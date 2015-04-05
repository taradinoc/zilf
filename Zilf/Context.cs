/* Copyright 2010, 2012 Jesse McGrew
 * 
 * This file is part of ZILF.
 * 
 * ZILF is free software: you can redistribute it and/or modify it
 * under the terms of the GNU General Public License as published by
 * the Free Software Foundation, either version 3 of the License, or
 * (at your option) any later version.
 * 
 * ZILF is distributed in the hope that it will be useful, but
 * WITHOUT ANY WARRANTY; without even the implied warranty of
 * MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the GNU
 * General Public License for more details.
 * 
 * You should have received a copy of the GNU General Public License
 * along with ZILF.  If not, see <http://www.gnu.org/licenses/>.
 */

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Reflection;
using System.IO;
using System.Linq.Expressions;

namespace Zilf
{
    enum RunMode
    {
        Interactive,
        Expression,
        Interpreter,
        Compiler,
    }

    delegate Stream OpenFileDelegate(string filename, bool writing);
    delegate bool FileExistsDelegate(string filename);

    [Flags]
    enum RoutineFlags
    {
        None = 0,
        CleanStack = 1,
    }

    [Flags]
    enum FileFlags
    {
        None = 0,
        CleanStack = 1,
    }

    class Context
    {
        private class Binding
        {
            public ZilObject Value;
            public Binding Prev;

            public Binding(ZilObject value)
            {
                this.Value = value;
            }
        }

        private struct AssocPair : IEquatable<AssocPair>
        {
            public readonly ZilObject First;
            public readonly ZilObject Second;

            public AssocPair(ZilObject first, ZilObject second)
            {
                this.First = first;
                this.Second = second;
            }

            public override bool Equals(object obj)
            {
                if (obj is AssocPair)
                    return Equals((AssocPair)obj);
                else
                    return false;
            }

            public bool Equals(AssocPair other)
            {
                return First.Equals(other.First) && Second.Equals(other.Second);
            }

            public override int GetHashCode()
            {
                return First.GetHashCode() ^ Second.GetHashCode();
            }
        }

        private delegate ZilObject ChtypeDelegate(Context ctx, ZilObject original);

        private class TypeMapEntry
        {
            public PrimType PrimType;
            public ChtypeDelegate ChtypeMethod;
        }

        private RunMode runMode;
        private int errorCount, warningCount;
        private bool ignoreCase, quiet, traceRoutines, wantDebugInfo;
        private List<string> includePaths;
        private string curFile;
        private FileFlags curFileFlags;
        private ZilForm callingForm;

        private ObList rootObList;
        private Dictionary<ZilAtom, Binding> localValues;
        private readonly Dictionary<ZilAtom, ZilObject> globalValues;
        private readonly Dictionary<AssocPair, ZilObject> associations;
        private readonly Dictionary<ZilAtom, TypeMapEntry> typeMap;
        private readonly ZEnvironment zenv;

        private RoutineFlags nextRoutineFlags;

        /// <summary>
        /// Gets a value representing truth (the atom T).
        /// </summary>
        public readonly ZilObject TRUE;
        /// <summary>
        /// Gets a value representing falsehood (a FALSE object with an empty list).
        /// </summary>
        public readonly ZilObject FALSE;

        private ZilAtom[] stdAtoms;

        public Context()
            : this(false)
        {
        }

        public Context(bool ignoreCase)
        {
            this.ignoreCase = ignoreCase;

            rootObList = new ObList(ignoreCase);
            localValues = new Dictionary<ZilAtom, Binding>();
            globalValues = new Dictionary<ZilAtom, ZilObject>();
            associations = new Dictionary<AssocPair, ZilObject>();
            typeMap = new Dictionary<ZilAtom, TypeMapEntry>();

            zenv = new ZEnvironment(this);

            includePaths = new List<string>();

            InitStdAtoms();
            InitSubrs();
            InitTypeMap();

            TRUE = GetStdAtom(StdAtom.T);
            FALSE = new ZilFalse(new ZilList(null, null));

            InitConstants();

            // initialize OBLIST path
            ObList userObList = new ObList(ignoreCase);
            ZilList olpath = new ZilList(new ZilObject[] { userObList, rootObList });
            ZilAtom olatom = GetStdAtom(StdAtom.OBLIST);
            localValues[olatom] = new Binding(olpath);

            InitTellPatterns();
        }

        public ObList RootObList
        {
            get { return rootObList; }
        }

        public RunMode RunMode
        {
            get { return runMode; }
            set { runMode = value; }
        }

        public bool IgnoreCase
        {
            get { return ignoreCase; }
        }

        public List<string> IncludePaths
        {
            get { return includePaths; }
        }

        public bool Quiet
        {
            get { return quiet; }
            set { quiet = value; }
        }

        public bool TraceRoutines
        {
            get { return traceRoutines; }
            set { traceRoutines = value; }
        }

        public bool WantDebugInfo
        {
            get { return wantDebugInfo; }
            set { wantDebugInfo = value; }
        }

        public bool Compiling
        {
            get { return runMode == RunMode.Compiler; }
        }

        public IEnumerable<KeyValuePair<ZilAtom, ZilObject>> GlobalVals
        {
            get { return globalValues; }
        }

        public int ErrorCount
        {
            get { return errorCount; }
            set { errorCount = value; }
        }

        public int WarningCount
        {
            get { return warningCount; }
            set { warningCount = value; }
        }

        public ZEnvironment ZEnvironment
        {
            get { return zenv; }
        }

        public string CurrentFile
        {
            get { return curFile; }
            set { curFile = value; }
        }

        public FileFlags CurrentFileFlags
        {
            get { return curFileFlags; }
            set { curFileFlags = value; }
        }

        public RoutineFlags NextRoutineFlags
        {
            get { return nextRoutineFlags; }
            set { nextRoutineFlags = value; }
        }

        public ZilForm CallingForm
        {
            get { return callingForm; }
            set { callingForm = value; }
        }

        public OpenFileDelegate InterceptOpenFile;
        public FileExistsDelegate InterceptFileExists;

        public Stream OpenFile(string filename, bool writing)
        {
            var intercept = this.InterceptOpenFile;
            if (intercept != null)
                return intercept(filename, writing);

            return new FileStream(
                filename,
                writing ? FileMode.Create : FileMode.Open,
                writing ? FileAccess.ReadWrite : FileAccess.Read);
        }

        public bool FileExists(string filename)
        {
            var intercept = this.InterceptFileExists;
            if (intercept != null)
                return intercept(filename);

            return File.Exists(filename);
        }
        
        private void InitStdAtoms()
        {
            StdAtom[] ids = (StdAtom[])Enum.GetValues(typeof(StdAtom));

            StdAtom max = ids[ids.Length - 1];
            stdAtoms = new ZilAtom[(int)max + 1];

            foreach (StdAtom sa in ids)
                if (sa != StdAtom.None)
                {
                    string pname = Enum.GetName(typeof(StdAtom), sa);

                    object[] attrs = typeof(StdAtom).GetField(pname).GetCustomAttributes(
                        typeof(AtomAttribute), false);
                    if (attrs.Length > 0)
                        pname = ((AtomAttribute)attrs[0]).Name;

                    ZilAtom atom = new ZilAtom(pname, rootObList, sa);
                    rootObList[pname] = atom;
                    stdAtoms[(int)sa] = atom;
                }
        }

        private void InitSubrs()
        {
            MethodInfo[] methods = typeof(Subrs).GetMethods(BindingFlags.Static | BindingFlags.Public);
            foreach (MethodInfo mi in methods)
            {
                object[] attrs = mi.GetCustomAttributes(typeof(Subrs.SubrAttribute), false);
                if (attrs.Length == 0)
                    continue;

                Subrs.SubrDelegate del =
                    (Subrs.SubrDelegate)Delegate.CreateDelegate(typeof(Subrs.SubrDelegate), mi);

                foreach (Subrs.SubrAttribute attr in attrs)
                {
                    ZilSubr sub;
                    if (attr is Subrs.FSubrAttribute)
                        sub = new ZilFSubr(del);
                    else
                        sub = new ZilSubr(del);

                    string name = attr.Name ?? mi.Name;

                    // can't use ZilAtom.Parse here because the OBLIST path isn't set up
                    ZilAtom atom = rootObList[name];
                    globalValues.Add(atom, sub);
                }
            }
        }

        private void InitConstants()
        {
            // compile-time constants
            globalValues.Add(GetStdAtom(StdAtom.ZILCH), TRUE);
            globalValues.Add(GetStdAtom(StdAtom.ZILF), TRUE);
            globalValues.Add(GetStdAtom(StdAtom.ZIL_VERSION), new ZilString(Program.VERSION));
            globalValues.Add(GetStdAtom(StdAtom.PREDGEN), TRUE);
            globalValues.Add(GetStdAtom(StdAtom.PLUS_MODE), zenv.ZVersion > 3 ? TRUE : FALSE);

            // runtime constants
            AddZConstant(GetStdAtom(StdAtom.TRUE_VALUE), TRUE);
            AddZConstant(GetStdAtom(StdAtom.FALSE_VALUE), FALSE);

            // parts of speech
            var parts = new[] {
                new { N="P1?OBJECT", V=PartOfSpeech.None },     // there is no ObjectFirst
                new { N="P1?VERB", V=PartOfSpeech.VerbFirst },
                new { N="P1?ADJECTIVE", V=PartOfSpeech.AdjectiveFirst },
                new { N="P1?DIRECTION", V=PartOfSpeech.DirectionFirst },

                new { N="PS?BUZZ-WORD", V=PartOfSpeech.Buzzword },
                new { N="PS?PREPOSITION", V=PartOfSpeech.Preposition },
                new { N="PS?DIRECTION", V=PartOfSpeech.Direction },
                new { N="PS?ADJECTIVE", V=PartOfSpeech.Adjective },
                new { N="PS?VERB", V=PartOfSpeech.Verb },
                new { N="PS?OBJECT", V=PartOfSpeech.Object },
            };

            foreach (var i in parts)
            {
                ZilAtom atom = rootObList[i.N];
                AddZConstant(atom, new ZilFix((int)i.V));
            }
        }

        public void SetDefaultConstants()
        {
            var defaults = new[] {
                new { N=StdAtom.SERIAL, V=0 },

                new { N=StdAtom.REXIT, V=0 },
                new { N=StdAtom.UEXIT, V=1 },
                new { N=StdAtom.NEXIT, V=2 },
                new { N=StdAtom.FEXIT, V=3 },
                new { N=StdAtom.CEXIT, V=4 },
                new { N=StdAtom.DEXIT, V=5 },

                new { N=StdAtom.NEXITSTR, V=0 },
                new { N=StdAtom.FEXITFCN, V=0 },
                new { N=StdAtom.CEXITFLAG, V=1 },
                new { N=StdAtom.CEXITSTR, V=1 },
                new { N=StdAtom.DEXITOBJ, V=1 },
                new { N=StdAtom.DEXITSTR, V=1 },
            };

            foreach (var i in defaults)
            {
                var atom = GetStdAtom(i.N);
                if (GetZVal(atom) == null)
                    AddZConstant(atom, new ZilFix(i.V));
            }
        }

        private void AddZConstant(ZilAtom atom, ZilObject value)
        {
            ZilConstant constant = new ZilConstant(atom, value);
            zenv.Constants.Add(constant);
            globalValues.Add(atom, constant);
        }

        /// <summary>
        /// Copies the context and creates a new, empty local binding environment.
        /// </summary>
        /// <returns>The newly created context, which shares everything with
        /// this context except the local bindings. Only the OBLIST local is copied.</returns>
        public Context CloneWithNewLocals()
        {
            Context result = (Context)MemberwiseClone();
            result.localValues = new Dictionary<ZilAtom, Binding>();

            ZilAtom oblistAtom = GetStdAtom(StdAtom.OBLIST);
            Binding binding;
            if (localValues.TryGetValue(oblistAtom, out binding) == true)
            {
                while (binding.Prev != null)
                    binding = binding.Prev;
                result.localValues.Add(oblistAtom, binding);
            }

            return result;
        }

        /// <summary>
        /// Gets the specified standard atom.
        /// </summary>
        /// <param name="id">The identifier of the standard atom.</param>
        /// <returns>The atom.</returns>
        public ZilAtom GetStdAtom(StdAtom id)
        {
            return stdAtoms[(int)id];
        }

        /// <summary>
        /// Gets the value associated with a pair of objects.
        /// </summary>
        /// <param name="first">The first object in the pair.</param>
        /// <param name="second">The second object in the pair.</param>
        /// <returns>The associated value, or null if no value is associated with the pair.</returns>
        public ZilObject GetProp(ZilObject first, ZilObject second)
        {
            AssocPair pair = new AssocPair(first, second);
            ZilObject result;
            if (associations.TryGetValue(pair, out result))
                return result;

            return null;
        }

        /// <summary>
        /// Sets or clears the value associated with a pair of objects.
        /// </summary>
        /// <param name="first">The first object in the pair.</param>
        /// <param name="second">The second object in the pair.</param>
        /// <param name="value">The value to be associated with the pair, or
        /// null to clear the association.</param>
        public void PutProp(ZilObject first, ZilObject second, ZilObject value)
        {
            AssocPair pair = new AssocPair(first, second);
            if (value == null)
                associations.Remove(pair);
            else
                associations[pair] = value;
        }

        /// <summary>
        /// Gets the local value assigned to an atom.
        /// </summary>
        /// <param name="atom">The atom.</param>
        /// <returns>The local value, or null if no local value is assigned.</returns>
        public ZilObject GetLocalVal(ZilAtom atom)
        {
            Binding b;
            if (localValues.TryGetValue(atom, out b))
                return b.Value;
            else
                return null;
        }

        /// <summary>
        /// Sets or clears the local value assigned to an atom.
        /// </summary>
        /// <param name="atom">The atom.</param>
        /// <param name="value">The new local value, or null to clear the local value.</param>
        public void SetLocalVal(ZilAtom atom, ZilObject value)
        {
            Binding b;
            if (localValues.TryGetValue(atom, out b))
                b.Value = value;
            else if (value != null)
                localValues[atom] = new Binding(value);
        }

        /// <summary>
        /// Creates a new local binding for an atom.
        /// </summary>
        /// <param name="atom">The atom.</param>
        /// <param name="value">The new local value for the atom, or null.</param>
        public void PushLocalVal(ZilAtom atom, ZilObject value)
        {
            Binding newb = new Binding(value);
            localValues.TryGetValue(atom, out newb.Prev);
            localValues[atom] = newb;
        }

        /// <summary>
        /// Removes the most recent local binding for an atom.
        /// </summary>
        /// <param name="atom">The atom.</param>
        public void PopLocalVal(ZilAtom atom)
        {
            Binding b;
            if (localValues.TryGetValue(atom, out b))
            {
                if (b.Prev == null)
                    localValues.Remove(atom);
                else
                    localValues[atom] = b.Prev;
            }
        }

        /// <summary>
        /// Gets the global value assigned to an atom.
        /// </summary>
        /// <param name="atom">The atom.</param>
        /// <returns>The global value, or null if no global value is assigned.</returns>
        public ZilObject GetGlobalVal(ZilAtom atom)
        {
            ZilObject result;
            if (globalValues.TryGetValue(atom, out result))
                return result;
            else
                return null;
        }

        /// <summary>
        /// Sets or clears the global value assigned to an atom.
        /// </summary>
        /// <param name="atom">The atom.</param>
        /// <param name="value">The new global value, or null to clear the global value.</param>
        public void SetGlobalVal(ZilAtom atom, ZilObject value)
        {
            if (value == null)
                globalValues.Remove(atom);
            else
                globalValues[atom] = value;
        }

        /// <summary>
        /// Gets the Z-code structure assigned to an atom.
        /// </summary>
        /// <param name="atom">The atom.</param>
        /// <returns>The value, or null if no value is assigned.</returns>
        /// <remarks>This is equivalent to &lt;GETPROP atom ZVAL&gt;.</remarks>
        public ZilObject GetZVal(ZilAtom atom)
        {
            return GetProp(atom, GetStdAtom(StdAtom.ZVAL));
        }

        /// <summary>
        /// Sets or clears the Z-code structure assigned to an atom.
        /// </summary>
        /// <param name="atom">The atom.</param>
        /// <param name="value">The new value, or null to clear the value.</param>
        /// <remarks>This is equivalent to &lt;PUTPROP atom ZVAL value&gt;.</remarks>
        public void SetZVal(ZilAtom atom, ZilObject value)
        {
            PutProp(atom, GetStdAtom(StdAtom.ZVAL), value);
        }

        public bool AllowRedefine
        {
            get
            {
                ZilObject lval = GetLocalVal(GetStdAtom(StdAtom.REDEFINE));
                return lval != null && lval.IsTrue;
            }
        }

        public void Redefine(ZilAtom atom)
        {
            ZilObject obj = GetZVal(atom);

            if (obj is ZilGlobal)
                zenv.Globals.Remove((ZilGlobal)obj);
            else if (obj is ZilRoutine)
                zenv.Routines.Remove((ZilRoutine)obj);
            else if (obj is ZilModelObject)
                zenv.Objects.Remove((ZilModelObject)obj);
            else if (obj is ZilConstant)
                zenv.Constants.Remove((ZilConstant)obj);
        }

        public void HandleWarning(ISourceLine node, string message, bool compiler)
        {
            warningCount++;
            Console.Error.WriteLine("[warning] {0}{1}{2}",
                node == null ? "" : node.SourceInfo,
                node == null ? "" : ": ",
                message);
        }

        public void HandleError(ZilError ex)
        {
            errorCount++;
            Console.Error.WriteLine("[error] {0}{1}", ex.SourcePrefix, ex.Message);
        }

        public string FindIncludeFile(string name)
        {
            foreach (var path in includePaths)
            {
                var combined = Path.Combine(path, name);

                if (FileExists(combined))
                    return combined;

                combined = Path.ChangeExtension(combined, ".zil");
                if (FileExists(combined))
                    return combined;
            }

            throw new FileNotFoundException();
        }

        /// <summary>
        /// Adapts a MethodInfo, describing a function that takes a context and a
        /// specific ZilObject type and returns a ZilObject, to ChtypeDelegate.
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="mi"></param>
        /// <returns></returns>
        private static ChtypeDelegate AdaptChtypeMethod<T>(MethodInfo mi)
            where T : ZilObject
        {
            var rawDel = Delegate.CreateDelegate(typeof(Func<Context, T, ZilObject>), mi);
            var del = (Func<Context, T, ZilObject>)rawDel;
            return (ctx, zo) => del(ctx, (T)zo);
        }

        /// <summary>
        /// Adapts a ConstructorInfo, describing a constructor that creates a
        /// given a specific ZilObject type, to ChtypeDelegate.
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="ci"></param>
        /// <returns></returns>
        private static ChtypeDelegate AdaptChtypeCtor<T>(ConstructorInfo ci)
            where T : ZilObject
        {
            var param1 = Expression.Parameter(typeof(Context), "ctx");
            var param2 = Expression.Parameter(typeof(ZilObject), "primValue");
            var expr = Expression.Lambda<ChtypeDelegate>(
                Expression.New(ci, Expression.Convert(param2, typeof(T))),
                param1, param2);
            return expr.Compile();
        }

        private void InitTypeMap()
        {
            var query = from t in typeof(ZilObject).Assembly.GetTypes()
                        where !t.IsAbstract && typeof(ZilObject).IsAssignableFrom(t)
                        from BuiltinTypeAttribute a in t.GetCustomAttributes(typeof(BuiltinTypeAttribute), false)
                        select new { Type = t, Attr = a };

            foreach (var r in query)
            {
                // look up chtype method
                Type[] chtypeParamTypes = { typeof(Context), null };
                Func<MethodInfo, ChtypeDelegate> adaptChtypeMethod;
                Func<ConstructorInfo, ChtypeDelegate> adaptChtypeCtor;

                switch (r.Attr.PrimType)
                {
                    case PrimType.ATOM:
                        chtypeParamTypes[1] = typeof(ZilAtom);
                        adaptChtypeMethod = AdaptChtypeMethod<ZilAtom>;
                        adaptChtypeCtor = AdaptChtypeCtor<ZilAtom>;
                        break;
                    case PrimType.FIX:
                        chtypeParamTypes[1] = typeof(ZilFix);
                        adaptChtypeMethod = AdaptChtypeMethod<ZilFix>;
                        adaptChtypeCtor = AdaptChtypeCtor<ZilFix>;
                        break;
                    case PrimType.LIST:
                        chtypeParamTypes[1] = typeof(ZilList);
                        adaptChtypeMethod = AdaptChtypeMethod<ZilList>;
                        adaptChtypeCtor = AdaptChtypeCtor<ZilList>;
                        break;
                    case PrimType.STRING:
                        chtypeParamTypes[1] = typeof(ZilString);
                        adaptChtypeMethod = AdaptChtypeMethod<ZilString>;
                        adaptChtypeCtor = AdaptChtypeCtor<ZilString>;
                        break;
                    case PrimType.VECTOR:
                        chtypeParamTypes[1] = typeof(ZilVector);
                        adaptChtypeMethod = AdaptChtypeMethod<ZilVector>;
                        adaptChtypeCtor = AdaptChtypeCtor<ZilVector>;
                        break;
                    default:
                        throw new NotImplementedException("Unexpected primtype: " + r.Attr.PrimType);
                }

                ChtypeDelegate chtypeDelegate;

                var chtypeMiQuery = from mi in r.Type.GetMethods(BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic)
                                    let attrs = mi.GetCustomAttributes(typeof(ChtypeMethodAttribute), false)
                                    where attrs.Length > 0
                                    select mi;
                var chtypeMethod = chtypeMiQuery.SingleOrDefault();
                
                if (chtypeMethod != null)
                {
                    // adapt the static method
                    if (!chtypeMethod.GetParameters().Select(pi => pi.ParameterType).SequenceEqual(chtypeParamTypes))
                        throw new InvalidOperationException(
                            string.Format(
                                "Wrong parameters for static ChtypeMethod {0} on type {1}",
                                chtypeMethod.Name,
                                r.Type.Name));

                    chtypeDelegate = adaptChtypeMethod(chtypeMethod);
                }
                else
                {
                    var chtypeCiQuery = from ci in r.Type.GetConstructors()
                                        let attrs = ci.GetCustomAttributes(typeof(ChtypeMethodAttribute), false)
                                        where attrs.Length > 0
                                        select ci;
                    var chtypeCtor = chtypeCiQuery.SingleOrDefault();

                    if (chtypeCtor != null)
                    {
                        // adapt the constructor
                        if (!chtypeCtor.GetParameters().Select(pi => pi.ParameterType).SequenceEqual(chtypeParamTypes.Skip(1)))
                            throw new InvalidOperationException(
                                string.Format(
                                    "Wrong parameters for ChtypeMethod constructor on type {0}",
                                    r.Type.Name));

                        chtypeDelegate = adaptChtypeCtor(chtypeCtor);
                    }
                    else
                    {
                        throw new InvalidOperationException(
                            string.Format("No ChtypeMethod found on type {0}", r.Type.Name));
                    }
                }

                var entry = new TypeMapEntry()
                {
                    PrimType = r.Attr.PrimType,
                    ChtypeMethod = chtypeDelegate,
                };

                typeMap.Add(GetStdAtom(r.Attr.Name), entry);
            }

            // default custom types
            var defaultCustomTypes = new[] {
                new { Name = "BYTE", PrimType = PrimType.FIX },
                new { Name = "DECL", PrimType = PrimType.LIST },
            };

            foreach (var ct in defaultCustomTypes)
            {
                // can't use ZilAtom.Parse here because the OBLIST path isn't set up
                var atom = rootObList[ct.Name];
                var primType = ct.PrimType;

                ChtypeDelegate chtypeDelegate =
                    (ctx, zo) => new ZilHash(atom, primType, zo);

                var entry = new TypeMapEntry()
                {
                    PrimType = ct.PrimType,
                    ChtypeMethod = chtypeDelegate,
                };

                typeMap.Add(atom, entry);
            }
        }

        public ZilObject ChangeType(ZilObject value, ZilAtom newType)
        {
            // chtype to the current type has no effect
            if (value.GetTypeAtom(this) == newType)
                return value;

            /* hacky special cases for GVAL and LVAL:
             * <CHTYPE FOO GVAL> gives '<GVAL FOO> rather than #GVAL FOO
             */
            if (newType.StdAtom == StdAtom.GVAL || newType.StdAtom == StdAtom.LVAL)
            {
                if (value.PrimType != PrimType.ATOM)
                    throw new InterpreterError("CHTYPE to GVAL or LVAL requires ATOM");

                return new ZilForm(new ZilObject[] { newType, value.GetPrimitive(this) });
            }

            // look it up in the typemap
            TypeMapEntry entry;
            if (typeMap.TryGetValue(newType, out entry))
            {
                if (value.PrimType != entry.PrimType)
                    throw new InterpreterError(
                        string.Format("CHTYPE to {0} requires {1}", newType, entry.PrimType));

                return entry.ChtypeMethod(this, value.GetPrimitive(this));
            }

            // unknown type
            throw new InterpreterError(newType + " is not a registered type");
        }

        private void InitTellPatterns()
        {
            ZEnvironment.TellPatterns.AddRange(TellPattern.Parse(
                new ZilObject[] {
                    // (CR CRLF) <CRLF>
                    new ZilList(new ZilObject[] { GetStdAtom(StdAtom.CR), GetStdAtom(StdAtom.CRLF) }),
                    new ZilForm(new ZilObject[] { GetStdAtom(StdAtom.CRLF) }),
                    // D * <PRINTD .X>
                    GetStdAtom(StdAtom.D), GetStdAtom(StdAtom.Times),
                    new ZilForm(new ZilObject[] {
                        GetStdAtom(StdAtom.PRINTD),
                        new ZilForm(new ZilObject[] { GetStdAtom(StdAtom.LVAL), GetStdAtom(StdAtom.X) }),
                    }),
                    // N * <PRINTN .X>
                    GetStdAtom(StdAtom.N), GetStdAtom(StdAtom.Times),
                    new ZilForm(new ZilObject[] {
                        GetStdAtom(StdAtom.PRINTN),
                        new ZilForm(new ZilObject[] { GetStdAtom(StdAtom.LVAL), GetStdAtom(StdAtom.X) }),
                    }),
                    // C * <PRINTC .X>
                    GetStdAtom(StdAtom.C), GetStdAtom(StdAtom.Times),
                    new ZilForm(new ZilObject[] {
                        GetStdAtom(StdAtom.PRINTC),
                        new ZilForm(new ZilObject[] { GetStdAtom(StdAtom.LVAL), GetStdAtom(StdAtom.X) }),
                    }),
                },
                this));
        }
    }

    /// <summary>
    /// A base class for exceptions used to implement wacky interpreter flow control.
    /// </summary>
    abstract class ControlException : Exception
    {
        protected ControlException(string name)
            : base(name)
        {
        }
    }

    /// <summary>
    /// Indicates that an inner block is returning.
    /// </summary>
    class ReturnException : ControlException
    {
        private readonly ZilObject value;

        public ReturnException(ZilObject value)
            : base("RETURN")
        {
            this.value = value;
        }

        public ZilObject Value
        {
            get { return value; }
        }
    }

    /// <summary>
    /// Indicates that an inner block is repeating.
    /// </summary>
    class AgainException : ControlException
    {
        public AgainException()
            : base("AGAIN")
        {
        }
    }
}
