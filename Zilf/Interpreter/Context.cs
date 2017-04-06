/* Copyright 2010-2017 Jesse McGrew
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
using Zilf.Common;
using Zilf.Diagnostics;
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
    delegate ZilObject EvalTypeDelegate(ZilObject zo);
    delegate ZilObject ApplyTypeDelegate(ZilObject zo, ZilObject[] args);

    [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Design", "CA1001:TypesThatOwnDisposableFieldsShouldBeDisposable",
        Justification = nameof(LocalEnvironment) + " is only disposable as syntactic sugar and doesn't need to be disposed")]
    sealed class Context : IParserSite
    {
        delegate ZilObject ChtypeDelegate(Context ctx, ZilObject original);

        class TypeMapEntry
        {
            public Type BuiltinType;
            public bool IsBuiltin => BuiltinType != null;
            public PrimType PrimType;
            public ChtypeDelegate ChtypeMethod;

            public ZilObject PrintType;
            public PrintTypeDelegate PrintTypeDelegate;

            public ZilObject EvalType;
            public EvalTypeDelegate EvalTypeDelegate;

            public ZilObject ApplyType;
            public ApplyTypeDelegate ApplyTypeDelegate;
        }

        int errorCount, warningCount;
        readonly IDiagnosticFormatter diagnosticFormatter = new DefaultDiagnosticFormatter();

        readonly bool ignoreCase;
        readonly List<string> includePaths;

        readonly ObList rootObList, packageObList, compilationFlagsObList, hooksObList;
        readonly Stack<ZilObject> previousObPaths;
        LocalEnvironment localEnvironment;
        readonly Dictionary<ZilAtom, Binding> globalValues;
        readonly AssociationTable associations;
        readonly Dictionary<ZilAtom, TypeMapEntry> typeMap;
        readonly Dictionary<string, SubrDelegate> subrDelegates;
        readonly ZEnvironment zenv;

        /// <summary>
        /// Gets a value representing truth (the atom T).
        /// </summary>
        public ZilObject TRUE { get; }
        /// <summary>
        /// Gets a value representing falsehood (a FALSE object with an empty list).
        /// </summary>
        public ZilObject FALSE { get; }

        /// <summary>
        /// Gets the top <see cref="Frame"/>, representing the innermost function call.
        /// </summary>
        public Frame TopFrame { get; private set; }

        ZilAtom[] stdAtoms;
        readonly ZilAtom enclosingProgActivationAtom;

        public Context()
            : this(false)
        {
        }

        public Context(bool ignoreCase)
        {
            this.ignoreCase = ignoreCase;

            // so we can create FileSourceInfos for default PROPDEFs
            CurrentFile = new FileContext(this, "<internal>");

            // set up the ROOT oblist manually
            rootObList = new ObList(ignoreCase);
            InitStdAtoms();
            associations = new AssociationTable();
            PutProp(rootObList, GetStdAtom(StdAtom.OBLIST), GetStdAtom(StdAtom.ROOT));
            PutProp(GetStdAtom(StdAtom.ROOT), GetStdAtom(StdAtom.OBLIST), rootObList);

            // now we can use MakeObList
            packageObList = MakeObList(GetStdAtom(StdAtom.PACKAGE));
            compilationFlagsObList = MakeObList(GetStdAtom(StdAtom.COMPILATION_FLAGS));
            hooksObList = MakeObList(MakeObList(GetStdAtom(StdAtom.ZILF))["HOOKS"]);
            previousObPaths = new Stack<ZilObject>();
            localEnvironment = new LocalEnvironment(this);
            globalValues = new Dictionary<ZilAtom, Binding>();
            typeMap = new Dictionary<ZilAtom, TypeMapEntry>();
            subrDelegates = new Dictionary<string, SubrDelegate>();

            zenv = new ZEnvironment(this);

            includePaths = new List<string>();

            InitTypeMap();

            TRUE = GetStdAtom(StdAtom.T);
            FALSE = new ZilFalse(new ZilList(null, null));

            var interruptsObList = MakeObList(GetStdAtom(StdAtom.INTERRUPTS));
            enclosingProgActivationAtom = interruptsObList["LPROG "];

            InitConstants();

            // initialize OBLIST path
            var userObList = MakeObList(GetStdAtom(StdAtom.INITIAL));
            var olpath = new ZilList(new ZilObject[] { userObList, rootObList });
            var olatom = GetStdAtom(StdAtom.OBLIST);
            localEnvironment.Rebind(olatom, olpath);

            // InitSubrs uses ArgDecoder, which parses DECLs and needs the OBLIST path
            InitSubrs();

            var outchanAtom = GetStdAtom(StdAtom.OUTCHAN);
            var consoleOutChannel = new ZilConsoleChannel(FileAccess.Write);
            SetLocalVal(outchanAtom, consoleOutChannel);
            SetGlobalVal(outchanAtom, consoleOutChannel);

            AtTopLevel = true;
            TopFrame = new NativeFrame(this, SourceLines.TopLevel);
            CheckDecls = true;

            InitTellPatterns();
            InitPropDefs();
            InitCompilationFlags();
            InitPackages();
        }

        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Performance", "CA1822:MarkMembersAsStatic")]
        [ContractInvariantMethod]
        void ObjectInvariant()
        {
            Contract.Invariant(errorCount >= 0);
            Contract.Invariant(warningCount >= 0);
            Contract.Invariant(includePaths != null);
            Contract.Invariant(rootObList != null);
            Contract.Invariant(packageObList != null);
            Contract.Invariant(localEnvironment != null);
            Contract.Invariant(associations != null);
            Contract.Invariant(typeMap != null);
            Contract.Invariant(zenv != null);

            Contract.Invariant(TRUE != null);
            Contract.Invariant(FALSE != null);

            Contract.Invariant(RootObList != null);
            Contract.Invariant(PackageObList != null);
            Contract.Invariant(IncludePaths != null);

        }

        public ObList RootObList
        {
            get { return rootObList; }
        }

        public ObList PackageObList
        {
            get { return packageObList; }
        }

        public bool CheckDecls { get; set; }

        public RunMode RunMode { get; set; }

        public bool IgnoreCase
        {
            get { return ignoreCase; }
        }

        public List<string> IncludePaths
        {
            get { return includePaths; }
        }

        public bool Quiet { get; set; }

        public bool TraceRoutines { get; set; }

        public bool WantDebugInfo { get; set; }

        public bool Compiling
        {
            get { return RunMode == RunMode.Compiler; }
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

        public FileContext CurrentFile { get; set; }

        public RoutineFlags NextRoutineFlags { get; set; }

        // TODO: merge AtTopLevel into Frame
        public bool AtTopLevel { get; set; }

        public Func<string, FileAccess, Stream> StreamOpener { get; set; }

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

        void InitStdAtoms()
        {
            Contract.Ensures(stdAtoms != null);
            Contract.Ensures(stdAtoms.Length > 0);

            var ids = (StdAtom[])Enum.GetValues(typeof(StdAtom));
            Contract.Assume(ids.Length > 0);
            Contract.Assume(Contract.Exists(ids, i => i != StdAtom.None));

            StdAtom max = ids[ids.Length - 1];
            stdAtoms = new ZilAtom[(int)max + 1];

            foreach (StdAtom sa in ids)
            {
                if (sa != StdAtom.None)
                {
                    var pname = Enum.GetName(typeof(StdAtom), sa);

                    var attrs = typeof(StdAtom).GetField(pname).GetCustomAttributes(
                        typeof(AtomAttribute), false);
                    if (attrs.Length > 0)
                        pname = ((AtomAttribute)attrs[0]).Name;

                    var atom = new ZilAtom(pname, rootObList, sa);
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

        void InitPackages()
        {
            var emptyPackageNames = new[] { "NEWSTRUC", "ZILCH", "ZIL" };

            foreach (var name in emptyPackageNames)
            {
                Subrs.PACKAGE(this, name);
                Subrs.ENDPACKAGE(this);
            }

            SetGlobalVal(ZilAtom.Parse("ZILCH!-ZILCH!-PACKAGE", this), TRUE);
        }

        void InitSubrs()
        {
            Contract.Ensures(globalValues.Count > Contract.OldValue(globalValues.Count));

            var descAtom = GetStdAtom(StdAtom.DESC);

            var methods = typeof(Subrs).GetMethods(BindingFlags.Static | BindingFlags.Public);
            foreach (MethodInfo mi in methods)
            {
                var attrs = mi.GetCustomAttributes(typeof(Subrs.SubrAttribute), false);
                if (attrs.Length == 0)
                    continue;

                var del = ArgDecoder.WrapMethodAsSubrDelegate(mi, this, out var desc);

                foreach (Subrs.SubrAttribute attr in attrs)
                {
                    string name = attr.Name ?? mi.Name;

                    subrDelegates.Add(name, del);

                    ZilSubr sub;
                    if (attr is Subrs.FSubrAttribute)
                        sub = new ZilFSubr(name, del);
                    else
                        sub = new ZilSubr(name, del);

                    // these need to be on the root oblist
                    ZilAtom atom = rootObList[name];
                    SetGlobalVal(atom, sub);

                    PutProp(sub, descAtom, ZilString.FromString(desc));
                }
            }
        }

        public SubrDelegate GetSubrDelegate(string name)
        {
            subrDelegates.TryGetValue(name, out var result);
            return result;
        }

        void InitConstants()
        {
            Contract.Ensures(globalValues.Count > Contract.OldValue(globalValues.Count));

            // compile-time constants
            SetGlobalVal(GetStdAtom(StdAtom.ZILCH), TRUE);
            SetGlobalVal(GetStdAtom(StdAtom.ZILF), TRUE);
            SetGlobalVal(GetStdAtom(StdAtom.ZIL_VERSION), ZilString.FromString(Program.VERSION));
            SetGlobalVal(GetStdAtom(StdAtom.PREDGEN), TRUE);
            SetGlobalVal(GetStdAtom(StdAtom.PLUS_MODE), zenv.ZVersion > 3 ? TRUE : FALSE);
            SetGlobalVal(GetStdAtom(StdAtom.SIBREAKS), ZilString.FromString(",.\""));

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
                new { N="PS?OBJECT", V=PartOfSpeech.Object }
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
                new { N=StdAtom.SERIAL, V=0 }
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

            var constant = new ZilConstant(atom, value);
            zenv.Constants.Add(constant);
            SetZVal(atom, constant);
            SetGlobalVal(atom, value);
            zenv.InternGlobalName(atom);

            return constant;
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

            return associations.GetProp(first, second);
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

            associations.PutProp(first, second, value);
        }

        /// <summary>
        /// Gets an array of <see cref="AsocResult"/> listing all active associations.
        /// </summary>
        /// <returns>The array.</returns>
        public AsocResult[] GetAllAssociations()
        {
            Contract.Ensures(Contract.Result<AsocResult[]>() != null);

            return associations.ToArray();
        }

        /// <summary>
        /// Gets the local value assigned to an atom.
        /// </summary>
        /// <param name="atom">The atom.</param>
        /// <returns>The local value, or null if no local value is assigned.</returns>
        [Pure]
        public ZilObject GetLocalVal(ZilAtom atom)
        {
            return localEnvironment.GetLocalVal(atom);
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

            localEnvironment.SetLocalVal(atom, value);
        }

        public LocalEnvironment PushEnvironment()
        {
            Contract.Ensures(Contract.Result<LocalEnvironment>() != null);

            var result = new LocalEnvironment(this, localEnvironment);
            localEnvironment = result;
            return result;
        }

        public void PopEnvironment()
        {
            if (localEnvironment.Parent == null)
                throw new InvalidOperationException("no parent environment to restore");

            localEnvironment = localEnvironment.Parent;
        }

        public T ExecuteInEnvironment<T>(LocalEnvironment tempEnvironment, Func<T> func)
        {
            var prev = localEnvironment;
            try
            {
                localEnvironment = tempEnvironment;
                return func();
            }
            finally
            {
                localEnvironment = prev;
            }
        }

        /// <summary>
        /// Executes a delegate in a fresh local environment containing
        /// only the OBLIST value from the current environment.
        /// </summary>
        /// <typeparam name="T">The return type of the delegate.</typeparam>
        /// <param name="func">The delegate to execute.</param>
        /// <returns>The value returned by the delegate.</returns>
        /// <remarks>The MDL documentation refers to the macro expansion environment as a
        /// "top level environment", but for the purposes of MDL-ZIL?, the environment is
        /// not considered "top level" (i.e. SUBR names are not redirected).</remarks>
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Reliability", "CA2000:Dispose objects before losing scope")]
        public T ExecuteInMacroEnvironment<T>(Func<T> func)
        {
            var oblistAtom = GetStdAtom(StdAtom.OBLIST);

            var rootEnvironment = localEnvironment;
            while (rootEnvironment.Parent != null && rootEnvironment.Parent.IsLocalBound(oblistAtom))
                rootEnvironment = rootEnvironment.Parent;

            var macroEnv = new LocalEnvironment(this);
            macroEnv.SetLocalVal(oblistAtom, rootEnvironment.GetLocalVal(oblistAtom));

            var wasTopLevel = AtTopLevel;
            try
            {
                AtTopLevel = false;
                return ExecuteInEnvironment(macroEnv, func);
            }
            finally
            {
                AtTopLevel = wasTopLevel;
            }
        }

        public LocalEnvironment LocalEnvironment
        {
            get { return localEnvironment; }
        }

        public Frame PushFrame(ZilForm callingForm)
        {
            Contract.Requires(callingForm != null);
            Contract.Ensures(Contract.Result<Frame>() != null);

            var result = new CallFrame(this, callingForm);
            TopFrame = result;
            return result;
        }

        public Frame PushFrame(ISourceLine sourceLine, string description = null)
        {
            Contract.Requires(sourceLine != null);
            Contract.Ensures(Contract.Result<Frame>() != null);

            var result = new NativeFrame(this, sourceLine, description);
            TopFrame = result;
            return result;
        }

        public void PopFrame()
        {
            if (TopFrame.Parent == null)
                throw new InvalidOperationException("no parent frame to restore");

            TopFrame = TopFrame.Parent;
        }

        public FileContext PushFileContext(string path)
        {
            Contract.Requires(!string.IsNullOrWhiteSpace(path));
            Contract.Ensures(Contract.Result<FileContext>() != null);

            var result = new FileContext(this, path);
            CurrentFile = result;
            return result;
        }

        public void PopFileContext()
        {
            if (CurrentFile.Parent == null)
                throw new InvalidOperationException("no parent file to restore");

            CurrentFile = CurrentFile.Parent;
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

            if (globalValues.TryGetValue(atom, out var binding))
                return binding.Value;

            return null;
        }

        /// <summary>
        /// Gets the global binding for an atom, optionally creating one if necessary.
        /// </summary>
        /// <param name="atom">The atom.</param>
        /// <param name="create"><b>true</b> to create the binding if it doesn't already exist.</param>
        /// <returns>The binding, or <b>null</b> if it doesn't exist and <paramref name="create"/> is <b>false</b>.</returns>
        public Binding GetGlobalBinding(ZilAtom atom, bool create)
        {
            Contract.Requires(atom != null);
            Contract.Ensures(Contract.Result<Binding>() != null || create == false);

            globalValues.TryGetValue(atom, out var binding);

            if (binding == null && create)
            {
                binding = new Binding(null);
                globalValues.Add(atom, binding);
            }

            return binding;
        }

        /// <summary>
        /// Sets or clears the global value assigned to an atom.
        /// </summary>
        /// <param name="atom">The atom.</param>
        /// <param name="value">The new global value, or null to clear the global value.</param>
        /// <exception cref="DeclCheckError"><paramref name="value"/> does not match the DECL
        /// for <paramref name="atom"/>.</exception>
        public void SetGlobalVal(ZilAtom atom, ZilObject value)
        {
            Contract.Requires(atom != null);
            Contract.Ensures(GetGlobalVal(atom) == value);

            if (value != null)
            {
                var binding = GetGlobalBinding(atom, true);
                MaybeCheckDecl(value, binding.Decl, "GVAL of {0}", atom);
                binding.Value = value;
            }
            else
            {
                var binding = GetGlobalBinding(atom, false);

                if (binding != null)
                    binding.Value = null;
            }
        }

        /// <summary>
        /// Optionally checks a value against a DECL, throwing if it fails.
        /// </summary>
        /// <param name="src">The source node.</param>
        /// <param name="value">The value to check.</param>
        /// <param name="pattern">The DECL to check <paramref name="value"/> against, or <b>null</b>.</param>
        /// <param name="usageFormat">A format string describing how <paramref name="value"/> will be used.</param>
        /// <param name="arg0">A parameter for <paramref name="usageFormat"/>.</param>
        /// <exception cref="DeclCheckError"><see cref="CheckDecls"/> is <b>true</b>, <paramref name="pattern"/> is non-null,
        /// and <paramref name="value"/> failed the check.</exception>
        public void MaybeCheckDecl(IProvideSourceLine src, ZilObject value, ZilObject pattern, string usageFormat, object arg0)
        {
            if (pattern != null && CheckDecls && !Decl.Check(this, value, pattern))
                throw new DeclCheckError(src, this, value, pattern, usageFormat, arg0);
        }

        /// <summary>
        /// Optionally checks a value against a DECL, throwing if it fails.
        /// </summary>
        /// <param name="value">The value to check.</param>
        /// <param name="pattern">The DECL to check <paramref name="value"/> against, or <b>null</b>.</param>
        /// <param name="usageFormat">A format string describing how <paramref name="value"/> will be used.</param>
        /// <param name="arg0">A parameter for <paramref name="usageFormat"/>.</param>
        /// <exception cref="DeclCheckError"><see cref="CheckDecls"/> is <b>true</b>, <paramref name="pattern"/> is non-null,
        /// and <paramref name="value"/> failed the check.</exception>
        public void MaybeCheckDecl(ZilObject value, ZilObject pattern, string usageFormat, object arg0)
        {
            if (pattern != null && CheckDecls && !Decl.Check(this, value, pattern))
                throw new DeclCheckError(this, value, pattern, usageFormat, arg0);
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
                var lval = GetLocalVal(GetStdAtom(StdAtom.REDEFINE));
                return lval != null && lval.IsTrue;
            }
        }

        public void Redefine(ZilAtom atom)
        {
            Contract.Requires(atom != null);

            zenv.InternedGlobalNames.Remove(atom);

            var obj = GetZVal(atom);

            switch (obj)
            {
                case ZilGlobal glob:
                    zenv.Globals.Remove(glob);
                    break;
                case ZilRoutine rtn:
                    zenv.Routines.Remove(rtn);
                    break;
                case ZilModelObject zmo:
                    zenv.Objects.Remove(zmo);
                    break;
                case ZilConstant cnst:
                    zenv.Constants.Remove(cnst);
                    break;
            }
        }

        [Obsolete("Use the overload that takes a " + nameof(ZilError) + " instead.")]
        public void HandleWarning(ISourceLine node, string message)
        {
            Contract.Requires(message != null);
            Contract.Ensures(warningCount > 0);

            warningCount++;
            Console.Error.WriteLine("[warning] {0}{1}{2}",
                node == null ? "" : node.SourceInfo,
                node == null ? "" : ": ",
                message);
        }

        public void HandleWarning(ZilError ex)
        {
            Contract.Requires(ex != null);
            Contract.Ensures(warningCount > 0);

            HandleDiagnostic(ex.Diagnostic);
        }

        public void HandleError(ZilError ex)
        {
            Contract.Requires(ex != null);
            Contract.Ensures(errorCount > 0);

            HandleDiagnostic(ex.Diagnostic);
        }

        public void HandleDiagnostic(Diagnostic diag)
        {
            Contract.Requires(diag != null);
            Contract.Ensures(errorCount >= Contract.OldValue(errorCount));
            Contract.Ensures(warningCount >= Contract.OldValue(warningCount));

            switch (diag.Severity)
            {
                case Severity.Error:
                    errorCount++;
                    break;

                case Severity.Warning:
                    warningCount++;
                    break;
            }

            Console.Error.WriteLine(diagnosticFormatter.Format(diag));
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
        static ChtypeDelegate AdaptChtypeMethod<T>(MethodInfo mi)
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
        static ChtypeDelegate AdaptChtypeCtor<T>(ConstructorInfo ci)
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

        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Naming", "CA2204:Literals should be spelled correctly", MessageId = "ChtypeMethod")]
        void InitTypeMap()
        {
            Contract.Ensures(typeMap.Count > 0);

            var query = from t in typeof(ZilObject).Assembly.GetTypes()
                        where typeof(ZilObject).IsAssignableFrom(t)
                        from a in t.GetCustomAttributes<BuiltinTypeAttribute>(false)
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
                        chtypeParamTypes[1] = typeof(ZilListBase);
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
                        throw UnhandledCaseException.FromEnum(r.Attr.PrimType);
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
                                "Wrong parameters for static ChtypeMethod {0} on type {1}\nExpected: ({2})\nActual: ({3})",
                                chtypeMethod.Name,
                                r.Type.Name,
                                string.Join(", ", chtypeParamTypes.Select(t => t.Name)),
                                string.Join(", ", chtypeMethod.GetParameters().Select(pi => pi.ParameterType.Name))));

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

                var entry = new TypeMapEntry
                {
                    BuiltinType = r.Type,
                    PrimType = r.Attr.PrimType,
                    ChtypeMethod = chtypeDelegate
                };

                typeMap.Add(GetStdAtom(r.Attr.Name), entry);
            }

            // default custom types
            var defaultCustomTypes = new[] {
                new { Name = "BYTE", PrimType = PrimType.FIX },
                new { Name = "SEMI", PrimType = PrimType.STRING },
                new { Name = "VOC", PrimType = PrimType.ATOM }
            };

            foreach (var ct in defaultCustomTypes)
            {
                // can't use ZilAtom.Parse here because the OBLIST path isn't set up
                var atom = rootObList[ct.Name];
                var primType = ct.PrimType;

                RegisterType(atom, primType);
            }
        }

        public void SetZVersion(int newVersion)
        {
            ZEnvironment.ZVersion = newVersion;
            SetGlobalVal(GetStdAtom(StdAtom.PLUS_MODE), newVersion > 3 ? TRUE : FALSE);
            InitPropDefs();
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

            var entry = new TypeMapEntry
            {
                PrimType = primType,
                ChtypeMethod = chtypeDelegate
            };

            typeMap.Add(atom, entry);
        }

        [Pure]
        public bool IsRegisteredType(ZilAtom atom)
        {
            Contract.Requires(atom != null);
            return typeMap.ContainsKey(atom);
        }

        public IEnumerable<ZilAtom> RegisteredTypes
        {
            get { return typeMap.Keys; }
        }

        [Pure]
        public bool IsStructuredType(ZilAtom atom)
        {
            Contract.Requires(atom != null);

            if (typeMap.TryGetValue(atom, out var entry))
            {
                if (entry.IsBuiltin && typeof(IApplicable).IsAssignableFrom(entry.BuiltinType))
                    return true;
            }

            return false;
        }

        [Pure]
        public bool IsApplicableType(ZilAtom atom)
        {
            Contract.Requires(atom != null);

            if (typeMap.TryGetValue(atom, out var entry))
            {
                if (entry.ApplyTypeDelegate != null)
                    return true;

                if (entry.IsBuiltin && typeof(IApplicable).IsAssignableFrom(entry.BuiltinType))
                    return true;
            }

            return false;
        }

        /// <summary>
        /// Gets the <see cref="PrimType"/> of a type atom.
        /// </summary>
        /// <param name="type">The name of a built-in type or a NEWTYPE.</param>
        /// <returns>The <see cref="PrimType"/> of the given type.</returns>
        [Pure]
        public PrimType GetTypePrim(ZilAtom type)
        {
            Contract.Requires(type != null);
            return typeMap[type].PrimType;
        }

        public enum SetTypeHandlerResult
        {
            OK,
            OtherTypeNotRegistered,
            OtherTypePrimDiffers,
            BadHandlerType,
        }

        public SetTypeHandlerResult SetPrintType(ZilAtom type, ZilObject handler)
        {
            Contract.Requires(type != null);
            Contract.Requires(IsRegisteredType(type));
            Contract.Requires(handler != null);

            return SetTypeHandler(type, handler,
                StdAtom.PRINT,
                e => e.PrintType,
                e => e.PrintTypeDelegate,
                (e, h) => e.PrintType = h,
                (e, d) => e.PrintTypeDelegate = d,
                (ctx, t, otherType) => zo =>
                {
                    return ctx.ChangeType(zo, otherType).ToStringContext(ctx, false, true);
                },
                (ctx, t, applicable) => zo =>
                {
                    var stringChannel = new ZilStringChannel(FileAccess.Write);
                    var outchanAtom = ctx.GetStdAtom(StdAtom.OUTCHAN);
                    using (var innerEnv = ctx.PushEnvironment())
                    using (ctx.PushFrame(SourceLines.Unknown, $"<PRINTTYPE for {t}>"))
                    {
                        innerEnv.Rebind(outchanAtom, stringChannel);
                        applicable.ApplyNoEval(ctx, new ZilObject[] { zo });
                        return stringChannel.String;
                    }
                });
        }

        public SetTypeHandlerResult SetEvalType(ZilAtom type, ZilObject handler)
        {
            Contract.Requires(type != null);
            Contract.Requires(IsRegisteredType(type));
            Contract.Requires(handler != null);

            return SetTypeHandler(type, handler,
                StdAtom.EVAL,
                e => e.EvalType,
                e => e.EvalTypeDelegate,
                (e, h) => e.EvalType = h,
                (e, d) => e.EvalTypeDelegate = d,
                (ctx, t, otherType) => zo =>
                {
                    return ctx.ChangeType(zo, otherType).EvalAsOtherType(ctx, t);
                },
                (ctx, t, applicable) => zo =>
                {
                    using (ctx.PushFrame(SourceLines.Unknown, $"<EVALTYPE for {t}>"))
                    {
                        return applicable.ApplyNoEval(ctx, new[] { zo });
                    }
                });
        }

        public SetTypeHandlerResult SetApplyType(ZilAtom type, ZilObject handler)
        {
            Contract.Requires(type != null);
            Contract.Requires(IsRegisteredType(type));
            Contract.Requires(handler != null);

            return SetTypeHandler(type, handler,
                StdAtom.APPLY,
                e => e.ApplyType,
                e => e.ApplyTypeDelegate,
                (e, h) => e.ApplyType = h,
                (e, d) => e.ApplyTypeDelegate = d,
                (ctx, t, otherType) => (zo, args) =>
                {
                    var chtyped = ctx.ChangeType(zo, otherType).AsApplicable(ctx);
                    if (chtyped != null)
                        return chtyped.ApplyNoEval(ctx, args);
                    throw new InterpreterError(InterpreterMessages.CHTYPE_To_0_Did_Not_Produce_An_Applicable_Object, otherType);
                },
                (ctx, t, applicable) => (zo, args) =>
                {
                    var innerArgs = new ZilObject[args.Length + 1];
                    innerArgs[0] = zo;
                    Array.Copy(args, 0, innerArgs, 1, args.Length);
                    using (ctx.PushFrame(SourceLines.Unknown, $"<EVALTYPE for {t}>"))
                    {
                        return applicable.ApplyNoEval(ctx, innerArgs);
                    }
                });
        }

        SetTypeHandlerResult SetTypeHandler<TDelegate>(ZilAtom type, ZilObject handler,
            StdAtom clearIndicator,
            Func<TypeMapEntry, ZilObject> getHandler,
            Func<TypeMapEntry, TDelegate> getDelegate,
            Action<TypeMapEntry, ZilObject> setHandler,
            Action<TypeMapEntry, TDelegate> setDelegate,
            Func<Context, ZilAtom, ZilAtom, TDelegate> makeDelegateFromOtherType,
            Func<Context, ZilAtom, IApplicable, TDelegate> makeDelegateFromApplicable)
            where TDelegate : class
        {
            Contract.Requires(typeof(TDelegate).IsSubclassOf(typeof(Delegate)));
            Contract.Requires(type != null);
            Contract.Requires(IsRegisteredType(type));
            Contract.Requires(handler != null);

            var entry = typeMap[type];

            if (handler is ZilAtom otherType)
            {
                if (!typeMap.TryGetValue(otherType, out var otherEntry))
                {
                    return SetTypeHandlerResult.OtherTypeNotRegistered;
                }
                if (otherEntry.PrimType != entry.PrimType)
                {
                    return SetTypeHandlerResult.OtherTypePrimDiffers;
                }
                if (getHandler(otherEntry) != null)
                {
                    // cloning a type that has a handler: copy its handler
                    setHandler(entry, getHandler(otherEntry));
                    setDelegate(entry, getDelegate(otherEntry));
                    return SetTypeHandlerResult.OK;
                }
                // cloning a type that has no handler: use the default handler for builtin types, or clear for newtypes
                if (otherEntry.IsBuiltin)
                {
                    setHandler(entry, otherType);
                    setDelegate(entry, makeDelegateFromOtherType(this, type, otherType));
                }
                else
                {
                    setHandler(entry, null);
                    setDelegate(entry, null);
                }

                return SetTypeHandlerResult.OK;
            }
            if (handler.IsApplicable(this))
            {
                // setting to <GVAL clearIndicator> means clearing
                if (handler.Equals(GetGlobalVal(GetStdAtom(clearIndicator))))
                {
                    setHandler(entry, null);
                    setDelegate(entry, null);
                }
                else
                {
                    setHandler(entry, handler);
                    setDelegate(entry, makeDelegateFromApplicable(this, type, handler.AsApplicable(this)));
                }

                return SetTypeHandlerResult.OK;
            }
            return SetTypeHandlerResult.BadHandlerType;
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

        public ZilObject GetEvalType(ZilAtom type)
        {
            Contract.Requires(type != null);
            Contract.Requires(IsRegisteredType(type));

            var entry = typeMap[type];
            return entry.EvalType;
        }

        public EvalTypeDelegate GetEvalTypeDelegate(ZilAtom type)
        {
            Contract.Requires(type != null);
            Contract.Requires(IsRegisteredType(type));

            var entry = typeMap[type];
            return entry.EvalTypeDelegate;
        }

        public ZilObject GetApplyType(ZilAtom type)
        {
            Contract.Requires(type != null);
            Contract.Requires(IsRegisteredType(type));

            var entry = typeMap[type];
            return entry.ApplyType;
        }

        public ApplyTypeDelegate GetApplyTypeDelegate(ZilAtom type)
        {
            Contract.Requires(type != null);
            Contract.Requires(IsRegisteredType(type));

            var entry = typeMap[type];
            return entry.ApplyTypeDelegate;
        }

        public ZilObject ChangeType(ZilObject value, ZilAtom newType)
        {
            Contract.Requires(value != null);
            Contract.Requires(newType != null);
            Contract.Ensures(Contract.Result<ZilObject>() != null);

            // DECL checking happens before anything else, so even chtype to the current type might fail!
            var decl = GetProp(newType, GetStdAtom(StdAtom.DECL));
            MaybeCheckDecl(value, decl, "CHTYPE to {0}", newType);

            // otherwise, chtype to the current type has no effect
            if (value.GetTypeAtom(this) == newType)
                return value;

            // TODO: standardize special cases

            /* hacky special cases for GVAL and LVAL:
             * <CHTYPE FOO GVAL> gives '<GVAL FOO> rather than #GVAL FOO
             */
            if (newType.StdAtom == StdAtom.GVAL || newType.StdAtom == StdAtom.LVAL)
            {
                if (value.PrimType != PrimType.ATOM)
                    throw new InterpreterError(InterpreterMessages.CHTYPE_To_0_Requires_1, "GVAL or LVAL", "ATOM");

                return new ZilForm(new ZilObject[] { newType, value.GetPrimitive(this) }) { SourceLine = SourceLines.Chtyped };
            }

            // special case for TABLE: its primtype is TABLE, but VECTOR can be converted too
            if (newType.StdAtom == StdAtom.TABLE && value.PrimType == PrimType.VECTOR)
            {
                var vector = (ZilVector)value.GetPrimitive(this);
                return ZilTable.Create(1, vector.ToArray(), 0, null);
            }

            // look it up in the typemap
            if (typeMap.TryGetValue(newType, out var entry))
            {
                if (value.PrimType != entry.PrimType)
                    throw new InterpreterError(
                        InterpreterMessages.CHTYPE_To_0_Requires_1, newType, entry.PrimType);

                var result = entry.ChtypeMethod(this, value.GetPrimitive(this));
                Contract.Assume(result != null);
                return result;
            }

            // unknown type
            throw new InterpreterError(InterpreterMessages.Unrecognized_0_1, "type", newType);
        }

        void InitTellPatterns()
        {
            const string STellPatterns = @"
(CR CRLF) <CRLF>
D * <PRINTD .X>
N * <PRINTN .X>
C * <PRINTC .X>
B * <PRINTB .X>
";

            zenv.TellPatterns.AddRange(TellPattern.Parse(Program.Parse(this, STellPatterns)));
        }

        void InitCompilationFlags()
        {
            Program.Evaluate(this, "<BLOCK (<ROOT>)>");
            try
            {
                DefineCompilationFlag(GetStdAtom(StdAtom.IN_ZILCH), FALSE);

                DefineCompilationFlag(GetStdAtom(StdAtom.COLOR), FALSE);
                DefineCompilationFlag(GetStdAtom(StdAtom.MOUSE), FALSE);
                DefineCompilationFlag(GetStdAtom(StdAtom.UNDO), FALSE);
                DefineCompilationFlag(GetStdAtom(StdAtom.DISPLAY), FALSE);
                DefineCompilationFlag(GetStdAtom(StdAtom.SOUND), FALSE);
                DefineCompilationFlag(GetStdAtom(StdAtom.MENU), FALSE);

                DefineCompilationFlag(GetStdAtom(StdAtom.LONG_WORDS), FALSE);
                DefineCompilationFlag(GetStdAtom(StdAtom.WORD_FLAGS_IN_TABLE), TRUE);
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
<DEFMAC IF-{0}!- (""ARGS"" A) <IFFLAG ({0} <COND (<LENGTH? .A 1> <1 .A>) (T <FORM BIND '() !.A>)>)>>
<DEFMAC IFN-{0}!- (""ARGS"" A) <IFFLAG ({0} <>) (T <COND (<LENGTH? .A 1> <1 .A>) (T <FORM BIND '() !.A>)>)>>";

                Program.Evaluate(this, string.Format(MacrosTemplate, name.Text), true);
                // TODO: correct the source locations in the macros
            }
            else if (redefine)
            {
                SetCompilationFlagValue(name, value);
            }
        }

        [Pure]
        public bool GetCompilationFlagOption(StdAtom stdAtom)
        {
            var value = GetCompilationFlagValue(GetStdAtom(stdAtom));
            return value != null && value.IsTrue;
        }

        [Pure]
        public ZilObject GetCompilationFlagValue(ZilAtom atom)
        {
            return GetCompilationFlagValue(atom.Text);
        }

        [Pure]
        public ZilObject GetCompilationFlagValue(string name)
        {
            var atom = compilationFlagsObList[name];
            return GetGlobalVal(atom);
        }

        void SetCompilationFlagValue(ZilAtom name, ZilObject value)
        {
            name = compilationFlagsObList[name.Text];
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

        void InitPropDef(StdAtom propName, string def)
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

            if (TopFrame.SourceLine is FileSourceLine fileSourceLine)
                path = Path.Combine(Path.GetDirectoryName(fileSourceLine.FileName), path);

            if (StreamOpener != null)
                return StreamOpener(path, fileAccess);

            FileMode mode;
            switch (fileAccess)
            {
                case FileAccess.ReadWrite:
                    mode = FileMode.OpenOrCreate;
                    break;

                case FileAccess.Write:
                    mode = FileMode.Create;
                    break;

                default:
                    mode = FileMode.Open;
                    break;
            }

            return new FileStream(path, mode, fileAccess);
        }

        /// <summary>
        /// Pushes a new LVAL for the atom OBLIST, to be used for looking up atoms.
        /// </summary>
        /// <param name="newObPath">A list to serve as the new LVAL of OBLIST.</param>
        public void PushObPath(ZilList newObPath)
        {
            Contract.Requires(newObPath != null);

            var atom = GetStdAtom(StdAtom.OBLIST);
            var old = GetLocalVal(atom);

            if (old == null)
            {
                old = new ZilList(null, null);
            }

            previousObPaths.Push(old);
            SetLocalVal(atom, newObPath);
        }

        /// <summary>
        /// Restores the previous LVAL of the atom OBLIST that existed before a corresponding
        /// call to <see cref="PushObPath"/>.
        /// </summary>
        /// <returns>The current LVAL of OBLIST, or <b>null</b> if it's currently unassigned.</returns>
        /// <exception cref="InvalidOperationException">There was no previous LVAL of OBLIST.</exception>
        public ZilObject PopObPath()
        {
            Contract.Ensures(Contract.Result<ZilObject>() != null);

            var atom = GetStdAtom(StdAtom.OBLIST);
            var old = GetLocalVal(atom);

            // may throw InvalidOperationException
            var popped = previousObPaths.Pop();

            SetLocalVal(atom, popped);
            return old;
        }

        public ZilActivation GetEnclosingProgActivation()
        {
            return GetLocalVal(enclosingProgActivationAtom) as ZilActivation;
        }

        public ZilAtom EnclosingProgActivationAtom
        {
            get { return enclosingProgActivationAtom; }
        }

        public ZilObject RunHook(string name, params ZilObject[] args)
        {
            var hook = GetGlobalVal(hooksObList[name]);

            if (hook != null && hook.IsApplicable(this))
            {
                var applicable = hook.AsApplicable(this);
                return applicable.ApplyNoEval(this, args);
            }

            return null;
        }

        #region IParserSite

        string IParserSite.CurrentFilePath => CurrentFile.Path;

        ZilAtom IParserSite.ParseAtom(string text) => ZilAtom.Parse(text, this);

        ZilAtom IParserSite.GetTypeAtom(ZilObject zo) => zo.GetTypeAtom(this);

        ZilObject IParserSite.Evaluate(ZilObject zo) => zo.Eval(this);

        #endregion
    }
}
