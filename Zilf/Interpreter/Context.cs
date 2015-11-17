/* Copyright 2010, 2015 Jesse McGrew
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
using System.Diagnostics.Contracts;
using System.IO;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;
using System.Runtime.CompilerServices;
using Zilf.Interpreter.Values;
using Zilf.Language;
using Zilf.ZModel;
using Zilf.ZModel.Values;
using Zilf.ZModel.Vocab;

namespace Zilf.Interpreter
{
    delegate Stream OpenFileDelegate(string filename, bool writing);
    delegate bool FileExistsDelegate(string filename);

    delegate string PrintTypeDelegate(ZilObject zo);

    class Context
    {
        private delegate ZilObject ChtypeDelegate(Context ctx, ZilObject original);

        private class TypeMapEntry
        {
            public bool IsBuiltin;
            public PrimType PrimType;
            public ChtypeDelegate ChtypeMethod;

            public ZilObject PrintType;
            public PrintTypeDelegate PrintTypeDelegate;
        }

        private RunMode runMode;
        private int errorCount, warningCount;
        private readonly bool ignoreCase;
        private bool quiet, traceRoutines, wantDebugInfo;
        private readonly List<string> includePaths;
        private string curFile;
        private FileFlags curFileFlags;
        private ZilForm callingForm;
        private bool atTopLevel;
        private Func<string, FileAccess, Stream> streamOpener;

        private readonly ObList rootObList, packageObList, compilationFlagsOblist;
        private readonly Stack<ZilObject> previousObPaths;
        private Dictionary<ZilAtom, Binding> localValues;
        private readonly Dictionary<ZilAtom, ZilObject> globalValues;
        private readonly ConditionalWeakTable<ZilObject, ConditionalWeakTable<ZilObject, ZilObject>> associations;
        private readonly Dictionary<ZilAtom, TypeMapEntry> typeMap;
        private readonly ZEnvironment zenv;

        private RoutineFlags nextRoutineFlags;

        /// <summary>
        /// Gets a value representing truth (the atom T).
        /// </summary>
        public ZilObject TRUE { get; private set; }
        /// <summary>
        /// Gets a value representing falsehood (a FALSE object with an empty list).
        /// </summary>
        public ZilObject FALSE { get; private set; }

        private ZilAtom[] stdAtoms;
        private readonly ZilAtom enclosingProgActivationAtom;

        public Context()
            : this(false)
        {
        }

        public Context(bool ignoreCase)
        {
            this.ignoreCase = ignoreCase;
            this.CurrentFile = "<internal>";        // so we can create FileSourceInfos for default PROPDEFs

            // set up the ROOT oblist manually
            rootObList = new ObList(ignoreCase);
            InitStdAtoms();
            associations = new ConditionalWeakTable<ZilObject, ConditionalWeakTable<ZilObject, ZilObject>>();
            PutProp(rootObList, GetStdAtom(StdAtom.OBLIST), GetStdAtom(StdAtom.ROOT));
            PutProp(GetStdAtom(StdAtom.ROOT), GetStdAtom(StdAtom.OBLIST), rootObList);

            // now we can use MakeObList
            packageObList = MakeObList(GetStdAtom(StdAtom.PACKAGE));
            compilationFlagsOblist = MakeObList(GetStdAtom(StdAtom.COMPILATION_FLAGS));
            previousObPaths = new Stack<ZilObject>();
            localValues = new Dictionary<ZilAtom, Binding>();
            globalValues = new Dictionary<ZilAtom, ZilObject>();
            typeMap = new Dictionary<ZilAtom, TypeMapEntry>();

            zenv = new ZEnvironment(this);

            includePaths = new List<string>();

            InitSubrs();
            InitTypeMap();

            TRUE = GetStdAtom(StdAtom.T);
            FALSE = new ZilFalse(new ZilList(null, null));

            var interruptsObList = MakeObList(GetStdAtom(StdAtom.INTERRUPTS));
            enclosingProgActivationAtom = interruptsObList["LPROG "];

            InitConstants();

            // initialize OBLIST path
            ObList userObList = MakeObList(GetStdAtom(StdAtom.INITIAL));
            ZilList olpath = new ZilList(new ZilObject[] { userObList, rootObList });
            ZilAtom olatom = GetStdAtom(StdAtom.OBLIST);
            localValues[olatom] = new Binding(olpath);

            var outchanAtom = GetStdAtom(StdAtom.OUTCHAN);
            var consoleOutChannel = new ZilConsoleChannel(FileAccess.Write);
            SetLocalVal(outchanAtom, consoleOutChannel);
            SetGlobalVal(outchanAtom, consoleOutChannel);

            InitTellPatterns();
            InitPropDefs();
            InitCompilationFlags();
            InitPackages();

            atTopLevel = true;
        }

        [ContractInvariantMethod]
        private void ObjectInvariant()
        {
            Contract.Invariant(errorCount >= 0);
            Contract.Invariant(warningCount >= 0);
            Contract.Invariant(includePaths != null);
            Contract.Invariant(rootObList != null);
            Contract.Invariant(packageObList != null);
            Contract.Invariant(localValues != null);
            Contract.Invariant(GlobalVals != null);
            Contract.Invariant(associations != null);
            Contract.Invariant(typeMap != null);
            Contract.Invariant(zenv != null);

            Contract.Invariant(TRUE != null);
            Contract.Invariant(FALSE != null);

            Contract.Invariant(RootObList != null);
            Contract.Invariant(PackageObList != null);
            Contract.Invariant(IncludePaths != null);
            Contract.Invariant(GlobalVals != null);

        }

        public ObList RootObList
        {
            get { return rootObList; }
        }

        public ObList PackageObList
        {
            get { return packageObList; }
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
            get
            {
                Contract.Ensures(Contract.Result<int>() >= 0);
                return errorCount;
            }
            set
            {
                Contract.Requires(value >= 0);
                errorCount = value;
            }
        }

        public int WarningCount
        {
            get
            {
                Contract.Ensures(Contract.Result<int>() >= 0);
                return warningCount;
            }
            set
            {
                Contract.Requires(value >= 0);
                warningCount = value;
            }
        }

        public ZEnvironment ZEnvironment
        {
            get
            {
                Contract.Ensures(Contract.Result<ZEnvironment>() != null);
                return zenv;
            }
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

        public bool AtTopLevel
        {
            get { return atTopLevel; }
            set { atTopLevel = value; }
        }

        public Func<string, FileAccess, Stream> StreamOpener
        {
            get { return streamOpener; }
            set { streamOpener = value; }
        }

        public OpenFileDelegate InterceptOpenFile;
        public FileExistsDelegate InterceptFileExists;

        public Stream OpenFile(string filename, bool writing)
        {
            Contract.Requires(filename != null);

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
            Contract.Requires(filename != null);

            var intercept = this.InterceptFileExists;
            if (intercept != null)
                return intercept(filename);

            return File.Exists(filename);
        }
        
        private void InitStdAtoms()
        {
            Contract.Ensures(stdAtoms != null);
            Contract.Ensures(stdAtoms.Length > 0);

            StdAtom[] ids = (StdAtom[])Enum.GetValues(typeof(StdAtom));
            Contract.Assume(ids.Length > 0);
            Contract.Assume(Contract.Exists(ids, i => i != StdAtom.None));

            StdAtom max = ids[ids.Length - 1];
            stdAtoms = new ZilAtom[(int)max + 1];

            foreach (StdAtom sa in ids)
            {
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

            Contract.Assume(stdAtoms.Length > 0);
        }

        public ObList MakeObList(ZilAtom name)
        {
            var result = new ObList(ignoreCase);

            var oblistAtom = GetStdAtom(StdAtom.OBLIST);
            PutProp(name, oblistAtom, result);
            PutProp(result, oblistAtom, name);

            return result;
        }

        private void InitPackages()
        {
            var emptyPackageNames = new[] { "NEWSTRUC", "ZILCH" };
            ZilObject[] args0 = new ZilObject[0], args1 = new ZilObject[1];

            foreach (var name in emptyPackageNames)
            {
                args1[0] = new ZilString(name);
                Subrs.PACKAGE(this, args1);
                Subrs.ENDPACKAGE(this, args0);
            }

            SetGlobalVal(ZilAtom.Parse("ZILCH!-ZILCH!-PACKAGE", this), TRUE);
        }

        private void InitSubrs()
        {
            Contract.Ensures(globalValues.Count > Contract.OldValue(globalValues.Count));

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
            Contract.Ensures(globalValues.Count > Contract.OldValue(globalValues.Count));

            // compile-time constants
            globalValues.Add(GetStdAtom(StdAtom.ZILCH), TRUE);
            globalValues.Add(GetStdAtom(StdAtom.ZILF), TRUE);
            globalValues.Add(GetStdAtom(StdAtom.ZIL_VERSION), new ZilString(Program.VERSION));
            globalValues.Add(GetStdAtom(StdAtom.PREDGEN), TRUE);
            globalValues.Add(GetStdAtom(StdAtom.PLUS_MODE), zenv.ZVersion > 3 ? TRUE : FALSE);
            globalValues.Add(GetStdAtom(StdAtom.SIBREAKS), new ZilString(",.\""));

            // runtime constants
            AddZConstant(GetStdAtom(StdAtom.TRUE_VALUE), TRUE);
            AddZConstant(GetStdAtom(StdAtom.FALSE_VALUE), FALSE);
            AddZConstant(GetStdAtom(StdAtom.FATAL_VALUE), new ZilFix(2));

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
            Contract.Ensures(globalValues.Count >= Contract.OldValue(globalValues.Count));

            var defaults = new[] {
                new { N=StdAtom.SERIAL, V=0 },
            };

            foreach (var i in defaults)
            {
                var atom = GetStdAtom(i.N);
                if (GetZVal(atom) == null)
                    AddZConstant(atom, new ZilFix(i.V));
            }
        }

        public ZilConstant AddZConstant(ZilAtom atom, ZilObject value)
        {
            Contract.Requires(atom != null);
            Contract.Requires(value != null);
            Contract.Ensures(globalValues.Count >= Contract.OldValue(globalValues.Count));
            Contract.Ensures(zenv.Constants.Count >= Contract.OldValue(zenv.Constants.Count));

            if (GetZVal(atom) != null)
                Redefine(atom);

            ZilConstant constant = new ZilConstant(atom, value);
            zenv.Constants.Add(constant);
            SetZVal(atom, constant);
            SetGlobalVal(atom, value);
            zenv.InternGlobalName(atom);

            return constant;
        }

        /// <summary>
        /// Copies the context and creates a new, empty local binding environment.
        /// </summary>
        /// <returns>The newly created context, which shares everything with
        /// this context except the local bindings. Only the OBLIST local is copied.</returns>
        public Context CloneWithNewLocals()
        {
            Contract.Ensures(Contract.Result<Context>() != null);
            Contract.Ensures(Contract.Result<Context>().localValues.Count == 1);

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
            Contract.Ensures(Contract.Result<ZilAtom>() != null);

            return stdAtoms[(int)id];
        }

        /// <summary>
        /// Gets the value associated with a pair of objects.
        /// </summary>
        /// <param name="first">The first object in the pair.</param>
        /// <param name="second">The second object in the pair.</param>
        /// <returns>The associated value, or null if no value is associated with the pair.</returns>
        [Pure]
        public ZilObject GetProp(ZilObject first, ZilObject second)
        {
            Contract.Requires(first != null);
            Contract.Requires(second != null);

            ConditionalWeakTable<ZilObject, ZilObject> innerTable;
            ZilObject result;
            if (associations.TryGetValue(first, out innerTable) && innerTable.TryGetValue(second, out result))
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
            Contract.Requires(first != null);
            Contract.Requires(second != null);

            ConditionalWeakTable<ZilObject, ZilObject> innerTable;
            if (value == null)
            {
                if (associations.TryGetValue(first, out innerTable))
                {
                    innerTable.Remove(second);
                }
            }
            else
            {
                innerTable = associations.GetOrCreateValue(first);
                innerTable.Remove(second);
                innerTable.Add(second, value);
            }
        }

        /// <summary>
        /// Gets the local value assigned to an atom.
        /// </summary>
        /// <param name="atom">The atom.</param>
        /// <returns>The local value, or null if no local value is assigned.</returns>
        [Pure]
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
            Contract.Requires(atom != null);
            Contract.Ensures(GetLocalVal(atom) == value);

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
            Contract.Requires(atom != null);
            Contract.Ensures(GetLocalVal(atom) == value);

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
            Contract.Requires(atom != null);

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
        [Pure]
        public ZilObject GetGlobalVal(ZilAtom atom)
        {
            Contract.Requires(atom != null);

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
            Contract.Requires(atom != null);
            Contract.Ensures(GetGlobalVal(atom) == value);

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
        [Pure]
        public ZilObject GetZVal(ZilAtom atom)
        {
            Contract.Requires(atom != null);

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
            Contract.Requires(atom != null);
            Contract.Ensures(GetZVal(atom) == value);

            PutProp(atom, GetStdAtom(StdAtom.ZVAL), value);
        }

        /// <summary>
        /// Gets a boolean value indicating whether a global option is enabled.
        /// </summary>
        /// <param name="stdAtom">The StdAtom identifying the option.</param>
        /// <returns><b>true</b> if the GVAL of the specified atom is assigned and true; otherwise <b>false</b>.</returns>
        [Pure]
        public bool GetGlobalOption(StdAtom stdAtom)
        {
            var value = GetGlobalVal(GetStdAtom(stdAtom));
            return value != null && value.IsTrue;
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
            Contract.Requires(atom != null);

            zenv.InternedGlobalNames.Remove(atom);

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
            Contract.Requires(message != null);
            Contract.Ensures(warningCount > 0);

            warningCount++;
            Console.Error.WriteLine("[warning] {0}{1}{2}",
                node == null ? "" : node.SourceInfo,
                node == null ? "" : ": ",
                message);
        }

        public void HandleError(ZilError ex)
        {
            Contract.Requires(ex != null);
            Contract.Ensures(errorCount > 0);

            errorCount++;
            Console.Error.WriteLine("[error] {0}{1}", ex.SourcePrefix, ex.Message);
        }

        public string FindIncludeFile(string name)
        {
            Contract.Requires(name != null);

            foreach (var path in includePaths)
            {
                var combined = Path.Combine(path, name);

                if (FileExists(combined))
                    return combined;

                combined = Path.ChangeExtension(combined, ".zil");
                if (FileExists(combined))
                    return combined;

                combined = Path.ChangeExtension(combined, ".mud");
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
            Contract.Requires(ci != null);
            Contract.Ensures(Contract.Result<ChtypeDelegate>() != null);

            var param1 = Expression.Parameter(typeof(Context), "ctx");
            var param2 = Expression.Parameter(typeof(ZilObject), "primValue");
            var expr = Expression.Lambda<ChtypeDelegate>(
                Expression.New(ci, Expression.Convert(param2, typeof(T))),
                param1, param2);
            return expr.Compile();
        }

        private void InitTypeMap()
        {
            Contract.Ensures(typeMap.Count > 0);

            var query = from t in typeof(ZilObject).Assembly.GetTypes()
                        where !t.IsAbstract && typeof(ZilObject).IsAssignableFrom(t)
                        from BuiltinTypeAttribute a in t.GetCustomAttributes(typeof(BuiltinTypeAttribute), false)
                        select new { Type = t, Attr = a };

            Type[] chtypeParamTypes = { typeof(Context), null };

            foreach (var r in query)
            {
                // look up chtype method
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
                    case PrimType.TABLE:
                        chtypeParamTypes[1] = typeof(ZilTable);
                        adaptChtypeMethod = AdaptChtypeMethod<ZilTable>;
                        adaptChtypeCtor = AdaptChtypeCtor<ZilTable>;
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
                    IsBuiltin = true,
                    PrimType = r.Attr.PrimType,
                    ChtypeMethod = chtypeDelegate,
                };

                typeMap.Add(GetStdAtom(r.Attr.Name), entry);
            }

            // default custom types
            var defaultCustomTypes = new[] {
                new { Name = "BYTE", PrimType = PrimType.FIX },
                new { Name = "DECL", PrimType = PrimType.LIST },
                new { Name = "SEMI", PrimType = PrimType.STRING },
                new { Name = "SPLICE", PrimType = PrimType.LIST },
                new { Name = "VOC", PrimType = PrimType.ATOM },
            };

            foreach (var ct in defaultCustomTypes)
            {
                // can't use ZilAtom.Parse here because the OBLIST path isn't set up
                var atom = rootObList[ct.Name];
                var primType = ct.PrimType;

                RegisterType(atom, primType);
            }
        }

        public void RegisterType(ZilAtom atom, PrimType primType)
        {
            Contract.Requires(atom != null);
            Contract.Ensures(typeMap.Count == Contract.OldValue(typeMap.Count) + 1);

            ChtypeDelegate chtypeDelegate;

            // use ZilStructuredHash for structured primtypes
            switch (primType)
            {
                case PrimType.LIST:
                case PrimType.STRING:
                case PrimType.TABLE:
                case PrimType.VECTOR:
                    chtypeDelegate = (ctx, zo) => new ZilStructuredHash(atom, primType, zo);
                    break;

                default:
                    chtypeDelegate = (ctx, zo) => new ZilHash(atom, primType, zo);
                    break;
            }

            var entry = new TypeMapEntry()
            {
                IsBuiltin = false,
                PrimType = primType,
                ChtypeMethod = chtypeDelegate,
            };

            typeMap.Add(atom, entry);
        }

        [Pure]
        public bool IsRegisteredType(ZilAtom atom)
        {
            Contract.Requires(atom != null);
            return typeMap.ContainsKey(atom);
        }

        public PrimType GetTypePrim(ZilAtom type)
        {
            Contract.Requires(type != null);
            return typeMap[type].PrimType;
        }

        public enum SetPrintTypeResult
        {
            OK,
            OtherTypeNotRegistered,
            OtherTypePrimDiffers,
            BadHandlerType,
        }

        public SetPrintTypeResult SetPrintType(ZilAtom type, ZilObject handler)
        {
            Contract.Requires(type != null);
            Contract.Requires(IsRegisteredType(type));
            Contract.Requires(handler != null);
            var entry = typeMap[type];

            if (handler is ZilAtom)
            {
                var otherType = (ZilAtom)handler;
                TypeMapEntry otherEntry;
                if (!typeMap.TryGetValue(otherType, out otherEntry))
                {
                    return SetPrintTypeResult.OtherTypeNotRegistered;
                }
                else if (otherEntry.PrimType != entry.PrimType)
                {
                    return SetPrintTypeResult.OtherTypePrimDiffers;
                }
                else if (otherEntry.PrintType != null)
                {
                    // cloning a type that has a printtype: copy its handler
                    entry.PrintType = otherEntry.PrintType;
                    entry.PrintTypeDelegate = otherEntry.PrintTypeDelegate;
                    return SetPrintTypeResult.OK;
                }
                else
                {
                    // cloning a type that has no printtype: use the default handler for builtin types, or clear for newtypes
                    if (otherEntry.IsBuiltin)
                    {
                        entry.PrintType = otherType;
                        entry.PrintTypeDelegate = zo =>
                        {
                            return ChangeType(zo, otherType).ToStringContext(this, false, true);
                        };
                    }
                    else
                    {
                        entry.PrintType = null;
                        entry.PrintTypeDelegate = null;
                    }

                    return SetPrintTypeResult.OK;
                }
            }
            else if (handler is IApplicable)
            {
                // setting to ,PRINT means clearing
                if (handler.Equals(GetGlobalVal(GetStdAtom(StdAtom.PRINT))))
                {
                    entry.PrintType = null;
                    entry.PrintTypeDelegate = null;
                }
                else
                {
                    entry.PrintType = handler;
                    entry.PrintTypeDelegate = zo =>
                    {
                        var stringChannel = new ZilStringChannel(FileAccess.Write);
                        var outchanAtom = GetStdAtom(StdAtom.OUTCHAN);
                        PushLocalVal(outchanAtom, stringChannel);
                        try
                        {
                            ((IApplicable)handler).Apply(this, new ZilObject[] { zo });
                            return stringChannel.String;
                        }
                        finally
                        {
                            PopLocalVal(outchanAtom);
                        }
                    };
                }

                return SetPrintTypeResult.OK;
            }
            else
            {
                return SetPrintTypeResult.BadHandlerType;
            }
        }

        public ZilObject GetPrintType(ZilAtom type)
        {
            Contract.Requires(type != null);
            Contract.Requires(IsRegisteredType(type));

            var entry = typeMap[type];
            return entry.PrintType;
        }

        public PrintTypeDelegate GetPrintTypeDelegate(ZilAtom type)
        {
            Contract.Requires(type != null);
            Contract.Requires(IsRegisteredType(type));

            var entry = typeMap[type];
            return entry.PrintTypeDelegate;
        }

        public ZilObject ChangeType(ZilObject value, ZilAtom newType)
        {
            Contract.Requires(value != null);
            Contract.Requires(newType != null);
            Contract.Ensures(Contract.Result<ZilObject>() != null);

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

                return new ZilForm(new ZilObject[] { newType, value.GetPrimitive(this) }) { SourceLine = SourceLines.Chtyped };
            }

            // special case for TABLE: its primtype is TABLE, but VECTOR can be converted too
            if (newType.StdAtom == StdAtom.TABLE && value.PrimType == PrimType.VECTOR)
            {
                var vector = (ZilVector)value.GetPrimitive(this);
                return new ZilTable(1, vector.ToArray(), 0, null);
            }

            // look it up in the typemap
            TypeMapEntry entry;
            if (typeMap.TryGetValue(newType, out entry))
            {
                if (value.PrimType != entry.PrimType)
                    throw new InterpreterError(
                        string.Format("CHTYPE to {0} requires {1}", newType, entry.PrimType));

                var result = entry.ChtypeMethod(this, value.GetPrimitive(this));
                Contract.Assume(result != null);
                return result;
            }

            // unknown type
            throw new InterpreterError(newType + " is not a registered type");
        }

        private void InitTellPatterns()
        {
            zenv.TellPatterns.AddRange(TellPattern.Parse(
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
                    // B * <PRINTB .X>
                    GetStdAtom(StdAtom.B), GetStdAtom(StdAtom.Times),
                    new ZilForm(new ZilObject[] {
                        GetStdAtom(StdAtom.PRINTB),
                        new ZilForm(new ZilObject[] { GetStdAtom(StdAtom.LVAL), GetStdAtom(StdAtom.X) }),
                    }),
                },
                this));
        }

        private void InitCompilationFlags()
        {
            Program.Evaluate(this, "<BLOCK (<ROOT>)>");
            try {
                DefineCompilationFlag(GetStdAtom(StdAtom.IN_ZILCH), TRUE);

                DefineCompilationFlag(GetStdAtom(StdAtom.COLOR), FALSE);
                DefineCompilationFlag(GetStdAtom(StdAtom.MOUSE), FALSE);
                DefineCompilationFlag(GetStdAtom(StdAtom.UNDO), FALSE);
                DefineCompilationFlag(GetStdAtom(StdAtom.DISPLAY), FALSE);
                DefineCompilationFlag(GetStdAtom(StdAtom.SOUND), FALSE);
                DefineCompilationFlag(GetStdAtom(StdAtom.MENU), FALSE);
            }
            finally
            {
                Program.Evaluate(this, "<ENDBLOCK>");
            }
        }

        public void DefineCompilationFlag(ZilAtom name, ZilObject value, bool redefine = false)
        {
            Contract.Requires(name != null);
            Contract.Requires(value != null);
            Contract.Ensures(GetCompilationFlagValue(name) != null);

            if (GetCompilationFlagValue(name) == null)
            {
                SetCompilationFlagValue(name, value);

                // define IF and IFN macros
                const string MacrosTemplate = @"
<DEFMAC IF-{0}!- (""ARGS"" A) <IFFLAG ({0} <FORM BIND '() !.A>)>>
<DEFMAC IFN-{0}!- (""ARGS"" A) <IFFLAG ({0} <>) (T <FORM BIND '() !.A>)>>";

                Program.Evaluate(this, string.Format(MacrosTemplate, name.Text), true);
                // TODO: correct the source locations in the macros
            }
            else if (redefine)
            {
                SetCompilationFlagValue(name, value);
            }
        }

        [Pure]
        public ZilObject GetCompilationFlagValue(ZilAtom atom)
        {
            return GetCompilationFlagValue(atom.Text);
        }

        [Pure]
        public ZilObject GetCompilationFlagValue(string name)
        {
            var atom = compilationFlagsOblist[name];
            return GetGlobalVal(atom);
        }

        private void SetCompilationFlagValue(ZilAtom name, ZilObject value)
        {
            name = compilationFlagsOblist[name.Text];
            SetGlobalVal(name, value);
        }

        /// <summary>
        /// Initializes (or re-initializes) the default PROPDEFs.
        /// </summary>
        /// <remarks>
        /// The PROPDEFs are version-specific, so this should be called again after changing <see cref="ZEnvironment.ZVersion"/>;
        /// </remarks>
        public void InitPropDefs()
        {
            const string SDirectionsPropDef_V3 = @"'[
(DIR TO R:ROOM =
    (UEXIT 1)
    (REXIT <ROOM .R>))
(DIR SORRY S:STRING =
    (NEXIT 2)
    (NEXITSTR <STRING .S>))
(DIR PER F:FCN =
    (FEXIT 3)
    (FEXITFCN <WORD .F>)
    <BYTE 0>)
(DIR TO R:ROOM IF G:GLOBAL ""OPT"" ELSE S:STRING =
    (CEXIT 4)
    (REXIT <ROOM .R>)
    (CEXITFLAG <GLOBAL .G>)
    (CEXITSTR <STRING .S>))
(DIR TO R:ROOM IF D:OBJECT IS OPEN ""OPT"" ELSE S:STRING =
    (DEXIT 5)
    (REXIT <ROOM .R>)
    (DEXITOBJ <OBJECT .D>)
    (DEXITSTR <STRING .S>)
    <BYTE 0>)
(DIR R:ROOM =
    (UEXIT 1)
    (REXIT <ROOM .R>))
(DIR S:STRING =
    (NEXIT 2)
    (NEXITSTR <STRING .S>))
]";

            const string SDirectionsPropDef_V4_Etc = @"'[
(DIR TO R:ROOM =
    (UEXIT 2)
    (REXIT <ROOM .R>))
(DIR SORRY S:STRING =
    (NEXIT 3)
    (NEXITSTR <STRING .S>)
    <BYTE 0>)
(DIR PER F:FCN =
    (FEXIT 4)
    (FEXITFCN <WORD .F>)
    <WORD 0>)
(DIR TO R:ROOM IF G:GLOBAL ""OPT"" ELSE S:STRING =
    (CEXIT 5)
    (REXIT <ROOM .R>)
    (CEXITSTR <STRING .S>)
    (CEXITFLAG <GLOBAL .G>))
(DIR TO R:ROOM IF D:OBJECT IS OPEN ""OPT"" ELSE S:STRING =
    (DEXIT 6)
    (REXIT <ROOM .R>)
    (DEXITOBJ <OBJECT .D>)
    (DEXITSTR <STRING .S>))
(DIR R:ROOM =
    (UEXIT 2)
    (REXIT <ROOM .R>))
(DIR S:STRING =
    (NEXIT 3)
    (NEXITSTR <STRING .S>)
    <BYTE 0>)
]";

            InitPropDef(StdAtom.DIRECTIONS, zenv.ZVersion == 3 ? SDirectionsPropDef_V3 : SDirectionsPropDef_V4_Etc);
        }

        private void InitPropDef(StdAtom propName, string def)
        {
            Contract.Requires(def != null);

            Program.Evaluate(this, "<BLOCK (<ROOT>)>");
            ZilVector vector;
            try
            {
                vector = (ZilVector)Program.Evaluate(this, def, true);
            }
            finally
            {
                Program.Evaluate(this, "<ENDBLOCK>");
            }
            var pattern = ComplexPropDef.Parse(vector, this);
            SetPropDef(GetStdAtom(propName), pattern);
        }

        public void SetPropDef(ZilAtom propName, ComplexPropDef pattern)
        {
            Contract.Requires(propName != null);
            Contract.Requires(pattern != null);

            foreach (var pair in pattern.GetConstants(this))
            {
                AddZConstant(pair.Key, new ZilFix(pair.Value));
            }
            PutProp(propName, GetStdAtom(StdAtom.PROPSPEC), pattern);
        }

        public Stream OpenChannelStream(string path, FileAccess fileAccess)
        {
            Contract.Requires(path != null);
            Contract.Ensures(Contract.Result<Stream>() != null);

            if (callingForm != null && callingForm.SourceLine is FileSourceLine)
                path = Path.Combine(Path.GetDirectoryName(((FileSourceLine)callingForm.SourceLine).FileName), path);

            if (streamOpener != null)
                return streamOpener(path, fileAccess);

            FileMode mode;
            switch (fileAccess)
            {
                case FileAccess.ReadWrite:
                    mode = FileMode.OpenOrCreate;
                    break;

                case FileAccess.Write:
                    mode = FileMode.Create;
                    break;

                case FileAccess.Read:
                default:
                    mode = FileMode.Open;
                    break;
            }

            return new FileStream(path, mode, fileAccess);
        }

        public void PushObPath(ZilList newObPath)
        {
            var atom = GetStdAtom(StdAtom.OBLIST);
            var old = GetLocalVal(atom);

            if (old == null)
            {
                old = new ZilList(null, null);
            }

            previousObPaths.Push(old);
            SetLocalVal(atom, newObPath);
        }

        public ZilObject PopObPath()
        {
            var atom = GetStdAtom(StdAtom.OBLIST);
            var old = GetLocalVal(atom);

            ZilObject popped;
            try
            {
                popped = previousObPaths.Pop();
            }
            catch (InvalidOperationException)
            {
                throw new InterpreterError("no previously pushed value for OBLIST");
            }

            SetLocalVal(atom, popped);
            return old;
        }

        public ZilActivation GetEnclosingProgActivation()
        {
            return GetLocalVal(enclosingProgActivationAtom) as ZilActivation;
        }

        public void PushEnclosingProgActivation(ZilActivation activation)
        {
            PushLocalVal(enclosingProgActivationAtom, activation);
        }

        public void PopEnclosingProgActivation()
        {
            PopLocalVal(enclosingProgActivationAtom);
        }
    }
}
