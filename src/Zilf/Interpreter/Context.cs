/* Copyright 2010-2018 Jesse McGrew
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
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Globalization;
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
using Zilf.Language.Parsing;

namespace Zilf.Interpreter
{
    delegate Stream OpenFileDelegate(string filename, bool writing);
    delegate bool FileExistsDelegate(string filename);

    delegate string PrintTypeDelegate(ZilObject zo);
    delegate ZilResult EvalTypeDelegate(ZilObject zo);
    delegate ZilResult ApplyTypeDelegate(ZilObject zo, ZilObject[] args);

    class ZValEventArgs : EventArgs
    {
        public ZilAtom Name { get; }
        public ZilObject? NewValue { get; }

        public ZValEventArgs(ZilAtom name, ZilObject? newValue)
        {
            Name = name;
            NewValue = newValue;
        }
    }

    [SuppressMessage("Microsoft.Design", "CA1001:TypesThatOwnDisposableFieldsShouldBeDisposable",
        Justification = nameof(LocalEnvironment) + " is only disposable as syntactic sugar and doesn't need to be disposed")]
    sealed class Context : IParserSite
    {
        delegate ZilObject ChtypeDelegate(Context ctx, ZilObject original);

        abstract class TypeMapEntry
        {
            public Type? BuiltinType { get; set; }
            public bool IsBuiltin => BuiltinType != null;
            public PrimType PrimType { get; set; }
            [System.Diagnostics.CodeAnalysis.NotNull]
            public ChtypeDelegate? ChtypeMethod { get; set; }

            public ZilObject? PrintType { get; set; }
            public PrintTypeDelegate? PrintTypeDelegate { get; set; }

            public ZilObject? EvalType { get; set; }
            public EvalTypeDelegate? EvalTypeDelegate { get; set; }

            public ZilObject? ApplyType { get; set; }
            public ApplyTypeDelegate? ApplyTypeDelegate { get; set; }
        }

        sealed class CustomTypeMapEntry : TypeMapEntry
        {
        }

        sealed class StaticTypeMapEntry : TypeMapEntry, IStaticTypeMapEntry
        {
            public TypeMapEntry Clone()
            {
                return (TypeMapEntry)MemberwiseClone();
            }
        }

        interface IStaticTypeMapEntry
        {
            Type? BuiltinType { get; }
            PrimType PrimType { get; }

            TypeMapEntry Clone();
        }

        public DiagnosticManager DiagnosticManager { get; }

        readonly ObList compilationFlagsObList, hooksObList;
        public ParserMacros ParserMacros { get; }

        readonly Stack<ZilObject> previousObPaths;
        LocalEnvironment localEnvironment;
        readonly Dictionary<ZilAtom, Binding> globalValues;
        readonly AssociationTable associations;
        readonly Dictionary<ZilAtom, TypeMapEntry> typeMap;
        readonly Dictionary<string, (SubrDelegate del, MethodInfo mi, bool isFSubr)> subrDelegates;

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

        public event EventHandler<ZValEventArgs>? ZValChanged;

        readonly ZilAtom[] stdAtoms;

        public Context()
            : this(false)
        {
        }

        public Context(bool ignoreCase)
        {
            this.IgnoreCase = ignoreCase;

            this.ParserMacros = new ParserMacros(this);

            this.DiagnosticManager = new DiagnosticManager();
            this.DiagnosticManager.TooManyErrors += (sender, args) =>
            {
                if (RunMode == RunMode.Compiler)
                {
                    throw new CompilerFatal(CompilerMessages.Too_Many_Errors);
                }
            };

            // so we can create FileSourceInfos for default PROPDEFs
            CurrentFile = new FileContext(this, "<internal>");

            // set up the ROOT oblist manually
            RootObList = new ObList(ignoreCase);
            stdAtoms = InitStdAtoms();
            associations = new AssociationTable();
            PutProp(RootObList, GetStdAtom(StdAtom.OBLIST), GetStdAtom(StdAtom.ROOT));
            PutProp(GetStdAtom(StdAtom.ROOT), GetStdAtom(StdAtom.OBLIST), RootObList);

            // now we can use MakeObList
            PackageObList = MakeObList(GetStdAtom(StdAtom.PACKAGE));
            compilationFlagsObList = MakeObList(GetStdAtom(StdAtom.COMPILATION_FLAGS));
            hooksObList = MakeObList(MakeObList(GetStdAtom(StdAtom.ZILF))["HOOKS"]);
            previousObPaths = new Stack<ZilObject>();
            localEnvironment = new LocalEnvironment(this);
            globalValues = new Dictionary<ZilAtom, Binding>();
            typeMap = new Dictionary<ZilAtom, TypeMapEntry>();
            subrDelegates = new Dictionary<string, (SubrDelegate, MethodInfo, bool)>();

            ZEnvironment = new ZEnvironment(this);

            IncludePaths = new List<string>();

            InitTypeMap();

            TRUE = GetStdAtom(StdAtom.T);
            FALSE = new ZilFalse(new ZilList(null, null));

            var interruptsObList = MakeObList(GetStdAtom(StdAtom.INTERRUPTS));
            EnclosingProgActivationAtom = interruptsObList["LPROG "];

            InitConstants();

            // initialize OBLIST path: only the root oblist for now
            var olatom = GetStdAtom(StdAtom.OBLIST);
            var olpath = new ZilList(new ZilObject[] { RootObList });
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

            // set up the user oblist
            var userObList = MakeObList(GetStdAtom(StdAtom.INITIAL));
            localEnvironment.Rebind(olatom, new ZilList(userObList, olpath));
        }

        public ObList RootObList { get; }

        public ObList PackageObList { get; }

        public bool CheckDecls { get; set; }

        public RunMode RunMode { get; set; }

        public bool IgnoreCase { get; }

        public List<string> IncludePaths { get; }

        public bool Quiet { get; set; }

        public bool TraceRoutines { get; set; }

        public bool WantDebugInfo { get; set; }

        public bool WarningsAsErrors
        {
            // ReSharper disable once UnusedMember.Global
            get => DiagnosticManager.WarningsAsErrors;
            set => DiagnosticManager.WarningsAsErrors = value;
        }

        public bool SuppressNoisyWarnings
        {
            // ReSharper disable once UnusedMember.Global
            get => DiagnosticManager.SuppressNoisyWarnings;
            set => DiagnosticManager.SuppressNoisyWarnings = value;
        }

        public int ErrorCount => DiagnosticManager.ErrorCount;

        public int WarningCount => DiagnosticManager.WarningCount;

        public int SuppressedWarningCount => DiagnosticManager.SuppressedWarningCount;

        public IReadOnlyCollection<Diagnostic> Diagnostics => DiagnosticManager.Diagnostics;

        public ZEnvironment ZEnvironment { get; }

        public FileContext CurrentFile { get; set; }

        public RoutineFlags NextRoutineFlags { get; set; }

        // TODO: merge AtTopLevel into Frame
        public bool AtTopLevel { get; set; }

        public OpenFileDelegate? InterceptOpenFile;
        public FileExistsDelegate? InterceptFileExists;

        public Stream OpenFile(string filename, bool writing)
        {
            var intercept = InterceptOpenFile;
            if (intercept != null)
                return intercept(filename, writing);

            return new FileStream(
                filename,
                writing ? FileMode.Create : FileMode.Open,
                writing ? FileAccess.ReadWrite : FileAccess.Read);
        }

        public bool FileExists(string filename)
        {
            return InterceptFileExists?.Invoke(filename) ?? File.Exists(filename);
        }

        ZilAtom[] InitStdAtoms()
        {
            var ids = (StdAtom[])Enum.GetValues(typeof(StdAtom));

            var max = ids[^1];
            var newStdAtoms = new ZilAtom[(int)max + 1];

            foreach (var sa in ids)
            {
                if (sa == StdAtom.None)
                    continue;

                var pname = Enum.GetName(typeof(StdAtom), sa);
                Debug.Assert(pname != null, nameof(pname) + " != null");

                var attrs = typeof(StdAtom).GetField(pname)!.GetCustomAttributes(
                    typeof(AtomAttribute), false);
                if (attrs.Length > 0)
                    pname = ((AtomAttribute)attrs[0]).Name;

                var atom = new ZilAtom(pname, RootObList, sa);
                RootObList[pname] = atom;
                newStdAtoms[(int)sa] = atom;
            }
            return newStdAtoms;
        }

        public ObList MakeObList(ZilAtom name)
        {
            var result = new ObList(IgnoreCase);

            var oblistAtom = GetStdAtom(StdAtom.OBLIST);
            PutProp(name, oblistAtom, result);
            PutProp(result, oblistAtom, name);

            return result;
        }

        void InitPackages()
        {
            var emptyPackageNames = new[] { "NEWSTRUC", "ZILCH", "ZIL", "READER-MACROS" };

            foreach (var name in emptyPackageNames)
            {
                Subrs.PACKAGE(this, name);
                Subrs.ENDPACKAGE(this);
            }

            SetGlobalVal(ZilAtom.Parse("ZILCH!-ZILCH!-PACKAGE", this), TRUE);
        }

        void InitSubrs()
        {
            var methods = typeof(Subrs).GetMethods(BindingFlags.Static | BindingFlags.Public);
            foreach (var mi in methods)
            {
                var attrs = mi.GetCustomAttributes<Subrs.SubrAttributeBase>(false).ToArray();
                if (attrs.Length == 0)
                    continue;

                var del = ArgDecoder.WrapMethod(mi, this);

                foreach (var attr in attrs)
                {
                    var baseName = attr.Name ?? mi.Name;
                    var name = string.IsNullOrEmpty(attr.ObList) ? baseName : $"{baseName}!-{attr.ObList}";

                    var isFSubr = attr is Subrs.FSubrAttribute;
                    subrDelegates.Add(name, (del, mi, isFSubr));

                    // these atoms need to be on the root oblist
                    var atom = ZilAtom.Parse(name + "!-", this);
                    SetGlobalVal(atom, isFSubr ? new ZilFSubr(baseName, del) : new ZilSubr(baseName, del));
                }
            }
        }

        public SubrDelegate? GetSubrDelegate(string name)
        {
            subrDelegates.TryGetValue(name, out var result);
            return result.del;
        }

        public IEnumerable<(string name, MethodInfo methodInfo, bool isFSubr)> GetSubrDefinitions()
        {
            return subrDelegates.Select(pair => (pair.Key, pair.Value.mi, pair.Value.isFSubr));
        }

        void InitConstants()
        {
            // compile-time constants
            SetGlobalVal(GetStdAtom(StdAtom.ZILCH), TRUE);
            SetGlobalVal(GetStdAtom(StdAtom.ZILF), TRUE);
            SetGlobalVal(GetStdAtom(StdAtom.ZIL_VERSION), ZilString.FromString(Program.GetBanner()));
            SetGlobalVal(GetStdAtom(StdAtom.PREDGEN), TRUE);
            SetGlobalVal(GetStdAtom(StdAtom.PLUS_MODE), ZEnvironment.ZVersion > 3 ? TRUE : FALSE);
            SetGlobalVal(GetStdAtom(StdAtom.SIBREAKS), ZilString.FromString(",.\""));

            // runtime constants
            AddZConstant(GetStdAtom(StdAtom.TRUE_VALUE), TRUE);
            AddZConstant(GetStdAtom(StdAtom.FALSE_VALUE), FALSE);
            AddZConstant(GetStdAtom(StdAtom.FATAL_VALUE), new ZilFix(2));

            // parts of speech
            var parts = new[]
            {
                ("P1?OBJECT", PartOfSpeech.None), // there is no ObjectFirst
                ("P1?VERB", PartOfSpeech.VerbFirst),
                ("P1?ADJECTIVE", PartOfSpeech.AdjectiveFirst),
                ("P1?DIRECTION", PartOfSpeech.DirectionFirst),

                ("PS?BUZZ-WORD", PartOfSpeech.Buzzword),
                ("PS?PREPOSITION", PartOfSpeech.Preposition),
                ("PS?DIRECTION", PartOfSpeech.Direction),
                ("PS?ADJECTIVE", PartOfSpeech.Adjective),
                ("PS?VERB", PartOfSpeech.Verb),
                ("PS?OBJECT", PartOfSpeech.Object)
            };

            foreach (var (name, value) in parts)
            {
                AddZConstant(RootObList[name], new ZilFix((int)value));
            }
        }

        public void SetDefaultConstants()
        {
            var defaults = new[] {
                (StdAtom.SERIAL, 0)
            };

            foreach (var (name, value) in defaults)
            {
                var atom = GetStdAtom(name);
                if (GetZVal(atom) != null)
                    continue;

                try
                {
                    AddZConstant(atom, new ZilFix(value));
                }
                catch (DeclCheckError)
                {
                    Debug.Fail("default constant value doesn't match decl");
                }
            }
        }

        /// <exception cref="DeclCheckError">value does not match the DECL for atom.</exception>
        public ZilConstant AddZConstant(ZilAtom atom, ZilObject value)
        {
            if (GetZVal(atom) != null)
                Redefine(atom);

            var constant = new ZilConstant(atom, value);
            ZEnvironment.Constants.Add(constant);
            SetZVal(atom, constant);
            SetGlobalVal(atom, value);
            ZEnvironment.InternGlobalName(atom);

            return constant;
        }

        /// <summary>
        /// Gets the specified standard atom.
        /// </summary>
        /// <param name="id">The identifier of the standard atom.</param>
        /// <returns>The atom.</returns>
        public ZilAtom GetStdAtom(StdAtom id) => stdAtoms[(int)id];

        public ZilObject? GetProp(ZilObject first, ZilObject second) => associations.GetProp(first, second);

        /// <summary>
        /// Sets or clears the value associated with a pair of objects.
        /// </summary>
        /// <param name="first">The first object in the pair.</param>
        /// <param name="second">The second object in the pair.</param>
        /// <param name="value">The value to be associated with the pair, or
        /// null to clear the association.</param>
        public void PutProp(ZilObject first, ZilObject second, ZilObject? value)
        {
            associations.PutProp(first, second, value);
        }

        /// <summary>
        /// Gets an array of <see cref="AsocResult"/> listing all active associations.
        /// </summary>
        /// <returns>The array.</returns>
        public AsocResult[] GetAllAssociations() => associations.ToArray();

        /// <summary>
        /// Gets the local value assigned to an atom.
        /// </summary>
        /// <param name="atom">The atom.</param>
        /// <returns>The local value, or null if no local value is assigned.</returns>
        public ZilObject? GetLocalVal(ZilAtom atom) => localEnvironment.GetLocalVal(atom);

        /// <summary>
        /// Sets or clears the local value assigned to an atom.
        /// </summary>
        /// <param name="atom">The atom.</param>
        /// <param name="value">The new local value, or null to clear the local value.</param>
        /// <exception cref="DeclCheckError">value does not match the existing DECL for atom.</exception>
        public void SetLocalVal(ZilAtom atom, ZilObject? value)
        {
            localEnvironment.SetLocalVal(atom, value);
        }

        public LocalEnvironment PushEnvironment()
        {
            var result = new LocalEnvironment(this, localEnvironment);
            localEnvironment = result;
            return result;
        }

        public void PopEnvironment()
        {
            localEnvironment = localEnvironment.Parent ??
                               throw new InvalidOperationException("no parent environment to restore");
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
        public T ExecuteInMacroEnvironment<T>(Func<T> func)
        {
            var oblistAtom = GetStdAtom(StdAtom.OBLIST);

            var rootEnvironment = localEnvironment;
            while (rootEnvironment.Parent?.IsLocalBound(oblistAtom) == true)
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

        public LocalEnvironment LocalEnvironment => localEnvironment;

        public Frame PushFrame(ZilForm callingForm)
        {
            var result = new CallFrame(this, callingForm);
            TopFrame = result;
            return result;
        }

        public Frame PushFrame(ISourceLine sourceLine, string? description = null)
        {
            var result = new NativeFrame(this, sourceLine, description);
            TopFrame = result;
            return result;
        }

        /// <exception cref="InvalidOperationException">This frame is not on top of the stack.</exception>
        public void PopFrame()
        {
            // ReSharper disable once JoinNullCheckWithUsage (workaround for Exceptional)
            if (TopFrame.Parent == null)
                throw new InvalidOperationException("no parent frame to restore");

            TopFrame = TopFrame.Parent;
        }

        public FileContext PushFileContext(string path)
        {
            var result = new FileContext(this, path);
            CurrentFile = result;
            return result;
        }

        /// <exception cref="InvalidOperationException">This file is not on top of the stack.</exception>
        public void PopFileContext()
        {
            // ReSharper disable once JoinNullCheckWithUsage (workaround for Exceptional)
            if (CurrentFile.Parent == null)
                throw new InvalidOperationException("no parent file to restore");

            CurrentFile = CurrentFile.Parent;
        }

        public ZilObject? GetGlobalVal(ZilAtom atom) =>
            globalValues.TryGetValue(atom, out var binding) ? binding.Value : null;

        public bool TryGetGlobalBinding(ZilAtom atom, [NotNullWhen(true)] out Binding? binding) =>
            globalValues.TryGetValue(atom, out binding);

        public Binding EnsureGlobalBinding(ZilAtom atom)
        {
            if (globalValues.TryGetValue(atom, out var result))
                return result;

            result = new Binding(null);
            globalValues.Add(atom, result);

            return result;
        }

        /// <summary>
        /// Gets all the globally bound atoms and their values.
        /// </summary>
        /// <returns>A sequence of key-value pairs.</returns>
        public IEnumerable<KeyValuePair<ZilAtom, Binding>> GetGlobalBindings() => globalValues;

        /// <summary>
        /// Sets or clears the global value assigned to an atom.
        /// </summary>
        /// <param name="atom">The atom.</param>
        /// <param name="value">The new global value, or null to clear the global value.</param>
        /// <exception cref="DeclCheckError"><paramref name="value"/> does not match the DECL for <paramref name="atom"/>.</exception>
        public void SetGlobalVal(ZilAtom atom, ZilObject? value)
        {
            if (value != null)
            {
                var binding = EnsureGlobalBinding(atom);
                MaybeCheckDecl(value, binding.Decl, "GVAL of {0}", atom);
                binding.Value = value;
            }
            else
            {
                if (TryGetGlobalBinding(atom, out var binding))
                    binding.Value = null;
            }
        }

        /// <summary>
        /// Optionally checks a value against a DECL, throwing if it fails.
        /// </summary>
        /// <param name="src">The source node.</param>
        /// <param name="value">The value to check.</param>
        /// <param name="pattern">The DECL to check <paramref name="value"/> against, or <see langword="null"/>.</param>
        /// <param name="usageFormat">A format string describing how <paramref name="value"/> will be used.</param>
        /// <param name="arg0">A parameter for <paramref name="usageFormat"/>.</param>
        /// <exception cref="DeclCheckError"><see cref="CheckDecls"/> is <see langword="true"/>, <paramref name="pattern"/> is non-null, and <paramref name="value"/> failed the check.</exception>
        public void MaybeCheckDecl(IProvideSourceLine src, ZilObject value, ZilObject? pattern,
            string usageFormat, object arg0)
        {
            if (pattern != null && CheckDecls && !Decl.Check(this, value, pattern))
                throw new DeclCheckError(src, this, value, pattern, usageFormat, arg0);
        }

        /// <summary>
        /// Optionally checks a value against a DECL, throwing if it fails.
        /// </summary>
        /// <param name="value">The value to check.</param>
        /// <param name="pattern">The DECL to check <paramref name="value"/> against, or <see langword="null"/>.</param>
        /// <param name="usageFormat">A format string describing how <paramref name="value"/> will be used.</param>
        /// <param name="arg0">A parameter for <paramref name="usageFormat"/>.</param>
        /// <exception cref="DeclCheckError"><see cref="CheckDecls"/> is <see langword="true"/>, <paramref name="pattern"/> is non-null, and <paramref name="value"/> failed the check.</exception>
        public void MaybeCheckDecl(ZilObject value, ZilObject? pattern, string usageFormat,
            object arg0)
        {
            if (pattern != null && CheckDecls && !Decl.Check(this, value, pattern))
                throw new DeclCheckError(this, value, pattern, usageFormat, arg0);
        }

        public ZilObject? GetZVal(ZilAtom atom) => GetProp(atom, GetStdAtom(StdAtom.ZVAL));

        /// <summary>
        /// Sets or clears the Z-code structure assigned to an atom.
        /// </summary>
        /// <param name="atom">The atom.</param>
        /// <param name="value">The new value, or null to clear the value.</param>
        /// <remarks>This is equivalent to &lt;PUTPROP atom ZVAL value&gt;
        /// but also raises the <see cref="ZValChanged"/> event.</remarks>
        public void SetZVal(ZilAtom atom, ZilObject value)
        {
            PutProp(atom, GetStdAtom(StdAtom.ZVAL), value);
            ZValChanged?.Invoke(this, new ZValEventArgs(atom, value));
        }

        public bool GetGlobalOption(StdAtom stdAtom) => GetGlobalVal(GetStdAtom(stdAtom))?.IsTrue == true;

        public bool AllowRedefine => GetLocalVal(GetStdAtom(StdAtom.REDEFINE))?.IsTrue == true;

        public void Redefine(ZilAtom atom)
        {
            ZEnvironment.InternedGlobalNames.Remove(atom);

            switch (GetZVal(atom))
            {
                case ZilGlobal glob:
                    ZEnvironment.Globals.Remove(glob);
                    break;
                case ZilRoutine rtn:
                    ZEnvironment.Routines.Remove(rtn);
                    break;
                case ZilModelObject zmo:
                    ZEnvironment.Objects.Remove(zmo);
                    break;
                case ZilConstant cnst:
                    ZEnvironment.Constants.Remove(cnst);
                    break;
            }
        }

        public void HandleError(ZilErrorBase ex) =>
            DiagnosticManager.Handle(ex.Diagnostic ?? throw new ArgumentException("Missing diagnostic", nameof(ex)));

        private static readonly string[] IncludeFileExtensions = { ".zil", ".mud", ".ZIL", ".MUD" };

        /// <exception cref="FileNotFoundException">The file wasn't found in any include path.</exception>
        public string FindIncludeFile(string name)
        {
            foreach (var path in IncludePaths)
            {
                foreach (var nameVariant in GetIncludeFileNameVariants(name))
                {
                    var combined = Path.Combine(path, nameVariant);

                    if (FileExists(combined))
                        return combined;
                }
            }

            throw new FileNotFoundException();
        }

        [SuppressMessage("Globalization", "CA1308:Normalize strings to uppercase",
            Justification = "This method looks for a lowercase filename when the source specifies an uppercase filename.")]
        private static IEnumerable<string> GetIncludeFileNameVariants(string name)
        {
            // try the name as-is
            yield return name;

            // try adding a .zil or .mud extension
            bool hasExtension = !string.IsNullOrEmpty(Path.GetExtension(name));

            if (!hasExtension)
            {
                foreach (var ext in IncludeFileExtensions)
                {
                    yield return Path.ChangeExtension(name, ext);
                }
            }

            // try lowercasing the name
            var lower = name.ToLowerInvariant();

            if (lower != name)
            {
                yield return lower;

                // try adding a .zil or .mud extension
                if (!hasExtension)
                {
                    foreach (var ext in IncludeFileExtensions)
                    {
                        yield return Path.ChangeExtension(lower, ext);
                    }
                }
            }
        }

        /// <summary>
        /// Adapts a MethodInfo, describing a function that takes a
        /// specific ZilObject type and returns a ZilObject, to ChtypeDelegate.
        /// </summary>
        static ChtypeDelegate AdaptChtypeMethod1<T>(MethodInfo mi)
            where T : ZilObject
        {
            var rawDel = Delegate.CreateDelegate(typeof(Func<T, ZilObject>), mi);
            var del = (Func<T, ZilObject>)rawDel;
            return (_, zo) => del((T)zo);
        }

        /// <summary>
        /// Adapts a MethodInfo, describing a function that takes a context and a
        /// specific ZilObject type and returns a ZilObject, to ChtypeDelegate.
        /// </summary>
        static ChtypeDelegate AdaptChtypeMethod2<T>(MethodInfo mi)
            where T : ZilObject
        {
            var rawDel = Delegate.CreateDelegate(typeof(Func<Context, T, ZilObject>), mi);
            var del = (Func<Context, T, ZilObject>)rawDel;
            return (ctx, zo) => del(ctx, (T)zo);
        }

        /// <summary>
        /// Adapts a ConstructorInfo, describing a constructor that creates a
        /// given specific ZilObject type, to ChtypeDelegate.
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="ci"></param>
        /// <returns></returns>
        static ChtypeDelegate AdaptChtypeCtor<T>(ConstructorInfo ci)
            where T : ZilObject
        {
            var param1 = Expression.Parameter(typeof(Context), "ctx");
            var param2 = Expression.Parameter(typeof(ZilObject), "primValue");
            var expr = Expression.Lambda<ChtypeDelegate>(
                Expression.New(ci, Expression.Convert(param2, typeof(T))),
                param1, param2);
            return expr.Compile();
        }

        static IReadOnlyDictionary<StdAtom, IStaticTypeMapEntry> StaticTypeMap => InitStaticTypeMap();

        [SuppressMessage("Microsoft.Naming", "CA2204:Literals should be spelled correctly", MessageId = "ChtypeMethod")]
        static Dictionary<StdAtom, IStaticTypeMapEntry> InitStaticTypeMap()
        {
            var result = new Dictionary<StdAtom, IStaticTypeMapEntry>();

            var query = from t in typeof(ZilObject).Assembly.GetTypes()
                        where typeof(ZilObject).IsAssignableFrom(t)
                        from a in t.GetCustomAttributes<BuiltinTypeAttribute>(false)
                        select new { Type = t, Attr = a };

            foreach (var r in query)
            {
                // look up chtype method
                Func<MethodInfo, ChtypeDelegate> adaptChtypeMethod1;
                Func<MethodInfo, ChtypeDelegate> adaptChtypeMethod2;
                Func<ConstructorInfo, ChtypeDelegate> adaptChtypeCtor;
                Type primitiveClassType;

                switch (r.Attr.PrimType)
                {
                    case PrimType.ATOM:
                        primitiveClassType = typeof(ZilAtom);
                        adaptChtypeMethod1 = AdaptChtypeMethod1<ZilAtom>;
                        adaptChtypeMethod2 = AdaptChtypeMethod2<ZilAtom>;
                        adaptChtypeCtor = AdaptChtypeCtor<ZilAtom>;
                        break;
                    case PrimType.FIX:
                        primitiveClassType = typeof(ZilFix);
                        adaptChtypeMethod1 = AdaptChtypeMethod1<ZilFix>;
                        adaptChtypeMethod2 = AdaptChtypeMethod2<ZilFix>;
                        adaptChtypeCtor = AdaptChtypeCtor<ZilFix>;
                        break;
                    case PrimType.LIST:
                        primitiveClassType = typeof(ZilListBase);
                        adaptChtypeMethod1 = AdaptChtypeMethod1<ZilList>;
                        adaptChtypeMethod2 = AdaptChtypeMethod2<ZilList>;
                        adaptChtypeCtor = AdaptChtypeCtor<ZilList>;
                        break;
                    case PrimType.STRING:
                        primitiveClassType = typeof(ZilString);
                        adaptChtypeMethod1 = AdaptChtypeMethod1<ZilString>;
                        adaptChtypeMethod2 = AdaptChtypeMethod2<ZilString>;
                        adaptChtypeCtor = AdaptChtypeCtor<ZilString>;
                        break;
                    case PrimType.TABLE:
                        primitiveClassType = typeof(ZilTable);
                        adaptChtypeMethod1 = AdaptChtypeMethod1<ZilTable>;
                        adaptChtypeMethod2 = AdaptChtypeMethod2<ZilTable>;
                        adaptChtypeCtor = AdaptChtypeCtor<ZilTable>;
                        break;
                    case PrimType.VECTOR:
                        primitiveClassType = typeof(ZilVector);
                        adaptChtypeMethod1 = AdaptChtypeMethod1<ZilVector>;
                        adaptChtypeMethod2 = AdaptChtypeMethod2<ZilVector>;
                        adaptChtypeCtor = AdaptChtypeCtor<ZilVector>;
                        break;
                    default:
                        throw UnhandledCaseException.FromEnum(r.Attr.PrimType);
                }

                ChtypeDelegate chtypeDelegate;

                var chtypeMiQuery =
                    from mi in r.Type.GetMethods(BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic)
                    let attrs = mi.GetCustomAttributes(typeof(ChtypeMethodAttribute), false)
                    where attrs.Length > 0
                    select mi;
                var chtypeMethod = chtypeMiQuery.SingleOrDefault();

                if (chtypeMethod != null)
                {
                    // adapt the static method
                    var foundParamTypes = chtypeMethod.GetParameters().Select(pi => pi.ParameterType).ToArray();

                    switch (foundParamTypes.Length)
                    {
                        case 1:
                            if (!foundParamTypes[0].IsAssignableFrom(primitiveClassType))
                            {
                                goto default;
                            }
                            chtypeDelegate = adaptChtypeMethod1(chtypeMethod);
                            break;

                        case 2:
                            if (foundParamTypes[0] != typeof(Context) ||
                                !foundParamTypes[1].IsAssignableFrom(primitiveClassType))
                            {
                                goto default;
                            }
                            chtypeDelegate = adaptChtypeMethod2(chtypeMethod);
                            break;

                        default:
                            throw new InvalidOperationException(
                                $"Wrong parameters for static ChtypeMethod {chtypeMethod.Name} on type {r.Type.Name}\n" +
                                $"Expected: ({primitiveClassType.Name}) or ({typeof(Context).Name}, {primitiveClassType.Namespace})\n" +
                                $"Actual: ({string.Join(", ", foundParamTypes.Select(t => t.Name))})");
                    }
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
                        var foundParamTypes = chtypeCtor.GetParameters().Select(pi => pi.ParameterType).ToArray();

                        // adapt the constructor
                        if (foundParamTypes.Length != 1 || !foundParamTypes[0].IsAssignableFrom(primitiveClassType))
                        {
                            throw new InvalidOperationException(
                                $"Wrong parameters for ChtypeMethod constructor on type {r.Type.Name}\n" +
                                $"Expected: ({primitiveClassType.Name})\n" +
                                $"Actual: ({string.Join(", ", foundParamTypes.Select(t => t.Name))})");
                        }

                        chtypeDelegate = adaptChtypeCtor(chtypeCtor);
                    }
                    else
                    {
                        throw new InvalidOperationException($"No ChtypeMethod found on type {r.Type.Name}");
                    }
                }

                var entry = new StaticTypeMapEntry
                {
                    BuiltinType = r.Type,
                    PrimType = r.Attr.PrimType,
                    ChtypeMethod = chtypeDelegate
                };

                result.Add(r.Attr.Name, entry);
            }

            return result;
        }

        void InitTypeMap()
        {
            foreach (var (atom, typeMapEntry) in StaticTypeMap)
            {
                typeMap.Add(GetStdAtom(atom), typeMapEntry.Clone());
            }

            // default custom types
            var defaultCustomTypes = new[]
            {
                (Name: "BYTE", PrimType: PrimType.FIX),
                (Name: "SEMI", PrimType: PrimType.STRING),
                (Name: "VOC", PrimType: PrimType.ATOM)
            };

            foreach (var (name, primType) in defaultCustomTypes)
            {
                // can't use ZilAtom.Parse here because the OBLIST path isn't set up
                RegisterType(RootObList[name], primType);
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
            ChtypeDelegate chtypeDelegate = primType switch
            {
                PrimType.LIST or PrimType.STRING or PrimType.VECTOR => (_, zo) => new ZilStructuredHash(atom, primType, (IStructure)zo),
                _ => (_, zo) => new ZilHash(atom, primType, zo),
            };
            var entry = new CustomTypeMapEntry
            {
                PrimType = primType,
                ChtypeMethod = chtypeDelegate
            };

            typeMap.Add(atom, entry);
        }

        public bool IsRegisteredType(ZilAtom atom) => typeMap.ContainsKey(atom);

        public IEnumerable<ZilAtom> RegisteredTypes => typeMap.Keys;

        public static bool IsStructuredType(StdAtom atom) =>
            StaticTypeMap.TryGetValue(atom, out var entry) &&
            typeof(IStructure).IsAssignableFrom(entry.BuiltinType);

        public static bool IsApplicableType(StdAtom atom) =>
            StaticTypeMap.TryGetValue(atom, out var entry) &&
            typeof(IApplicable).IsAssignableFrom(entry.BuiltinType);
        public PrimType GetTypePrim(ZilAtom type) => typeMap[type].PrimType;

        public static PrimType GetTypePrim(StdAtom type) => StaticTypeMap[type].PrimType;

        public enum SetTypeHandlerResult
        {
            OK,
            OtherTypeNotRegistered,
            OtherTypePrimDiffers,
            BadHandlerType,
        }

        public SetTypeHandlerResult SetPrintType(ZilAtom type, ZilObject handler)
        {
            return SetTypeHandler(type, handler,
                StdAtom.PRINT,
                e => e.PrintType,
                e => e.PrintTypeDelegate,
                (e, h) => e.PrintType = h,
                (e, d) => e.PrintTypeDelegate = d,
                (ctx, _, otherType) => zo => ctx.ChangeType(zo, otherType).ToStringContext(ctx, false, true),
                (ctx, t, applicable) => zo =>
                {
                    var stringChannel = new ZilStringChannel(FileAccess.Write);
                    var outchanAtom = ctx.GetStdAtom(StdAtom.OUTCHAN);

                    using var innerEnv = ctx.PushEnvironment();

                    using (ctx.PushFrame(SourceLines.Unknown, $"<PRINTTYPE for {t}>"))
                    {
                        innerEnv.Rebind(outchanAtom, stringChannel);
                        applicable.ApplyNoEval(ctx, new[] { zo });
                        return stringChannel.String;
                    }
                });
        }

        public SetTypeHandlerResult SetEvalType(ZilAtom type, ZilObject handler)
        {
            return SetTypeHandler(type, handler,
                StdAtom.EVAL,
                e => e.EvalType,
                e => e.EvalTypeDelegate,
                (e, h) => e.EvalType = h,
                (e, d) => e.EvalTypeDelegate = d,
                (ctx, t, otherType) => zo => ctx.ChangeType(zo, otherType).EvalAsOtherType(ctx, t),
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
            return SetTypeHandler(type, handler,
                StdAtom.APPLY,
                e => e.ApplyType,
                e => e.ApplyTypeDelegate,
                (e, h) => e.ApplyType = h,
                (e, d) => e.ApplyTypeDelegate = d,
                (ctx, _, otherType) => (zo, args) =>
                {
                    if (ctx.ChangeType(zo, otherType).IsApplicable(ctx, out var chtyped))
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
            Func<TypeMapEntry, ZilObject?> getHandler,
            Func<TypeMapEntry, TDelegate?> getDelegate,
            Action<TypeMapEntry, ZilObject?> setHandler,
            Action<TypeMapEntry, TDelegate?> setDelegate,
            Func<Context, ZilAtom, ZilAtom, TDelegate> makeDelegateFromOtherType,
            Func<Context, ZilAtom, IApplicable, TDelegate> makeDelegateFromApplicable)
            where TDelegate : Delegate
        {
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

            if (!handler.IsApplicable(this, out var applicableHandler))
                return SetTypeHandlerResult.BadHandlerType;

            // setting to <GVAL clearIndicator> means clearing
            if (handler.ExactlyEquals(GetGlobalVal(GetStdAtom(clearIndicator))))
            {
                setHandler(entry, null);
                setDelegate(entry, null);
            }
            else
            {
                setHandler(entry, handler);
                setDelegate(entry, makeDelegateFromApplicable(this, type, applicableHandler));
            }

            return SetTypeHandlerResult.OK;
        }

        public ZilObject? GetPrintType(ZilAtom type)
        {
            var entry = typeMap[type];
            return entry.PrintType;
        }

        public PrintTypeDelegate? GetPrintTypeDelegate(ZilAtom type)
        {
            var entry = typeMap[type];
            return entry.PrintTypeDelegate;
        }

        public ZilObject? GetEvalType(ZilAtom type)
        {
            var entry = typeMap[type];
            return entry.EvalType;
        }

        public EvalTypeDelegate? GetEvalTypeDelegate(ZilAtom type)
        {
            var entry = typeMap[type];
            return entry.EvalTypeDelegate;
        }

        public ZilObject? GetApplyType(ZilAtom type)
        {
            var entry = typeMap[type];
            return entry.ApplyType;
        }

        public ApplyTypeDelegate? GetApplyTypeDelegate(ZilAtom type)
        {
            var entry = typeMap[type];
            return entry.ApplyTypeDelegate;
        }

        /// <exception cref="InterpreterError">The type of <paramref name="value"/> is incompatible with <paramref name="newType"/>.</exception>
        public ZilObject ChangeType(ZilObject value, ZilAtom newType)
        {
            // DECL checking happens before anything else, so even chtype to the current type might fail!
            var decl = GetProp(newType, GetStdAtom(StdAtom.DECL));
            MaybeCheckDecl(value, decl, "CHTYPE to {0}", newType);

            // otherwise, chtype to the current type has no effect
            if (value.GetTypeAtom(this) == newType)
                return value;

            ZilObject result;

            // TODO: standardize special cases
            switch (newType.StdAtom)
            {
                /* hacky special cases for GVAL and LVAL:
                 * <CHTYPE FOO GVAL> gives '<GVAL FOO> rather than #GVAL FOO
                 * <CHTYPE '<GVAL FOO> ATOM> gives FOO
                 */
                case StdAtom.GVAL:
                case StdAtom.LVAL:
                    if (value.PrimType != PrimType.ATOM)
                        throw new InterpreterError(InterpreterMessages.CHTYPE_To_0_Requires_1, "GVAL or LVAL", "ATOM");

                    result = new ZilForm(new[] { newType, value.GetPrimitive(this) });
                    break;

                case StdAtom.ATOM when value.StdTypeAtom == StdAtom.FORM:
                    if (value.IsGVAL(out var atom) || value.IsLVAL(out atom))
                    {
                        // don't set SourceLine
                        return atom;
                    }

                    throw new InterpreterError(InterpreterMessages.CHTYPE_To_0_Requires_1, "ATOM", "ATOM, GVAL, or LVAL");

                // special case for TABLE: its primtype is TABLE, but VECTOR can be converted too
                case StdAtom.TABLE when value.PrimType == PrimType.VECTOR:
                    var vector = (ZilVector)value.GetPrimitive(this);
                    result = ZilTable.Create(1, vector.ToArray(), 0, null);
                    break;

                // look it up in the typemap
                case var _ when typeMap.TryGetValue(newType, out var entry):
                    if (value.PrimType != entry.PrimType)
                    {
                        throw new InterpreterError(
                            InterpreterMessages.CHTYPE_To_0_Requires_1, newType, entry.PrimType);
                    }

                    result = entry.ChtypeMethod(this, value.GetPrimitive(this));
                    break;

                // unknown type
                default:
                    throw new InterpreterError(InterpreterMessages.Unrecognized_0_1, "type", newType);
            }

            result.SourceLine ??= SourceLines.Chtyped;
            return result;
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

            ZEnvironment.TellPatterns.AddRange(TellPattern.Parse(Program.Parse(this, STellPatterns)));
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
            if (GetCompilationFlagValue(name) == null)
            {
                SetCompilationFlagValue(name, value);

                // define IF and IFN macros
                const string MacrosTemplate = @"
<DEFMAC IF-{0}!- (""ARGS"" A) <IFFLAG ({0} <COND (<LENGTH? .A 1> <1 .A>) (T <FORM BIND '() !.A>)>)>>
<DEFMAC IFN-{0}!- (""ARGS"" A) <IFFLAG ({0} <>) (T <COND (<LENGTH? .A 1> <1 .A>) (T <FORM BIND '() !.A>)>)>>";

                Program.Evaluate(this, string.Format(CultureInfo.InvariantCulture, MacrosTemplate, name.Text), true);
                // TODO: correct the source locations in the macros
            }
            else if (redefine)
            {
                SetCompilationFlagValue(name, value);
            }
        }

        public bool GetCompilationFlagOption(StdAtom stdAtom) =>
            GetCompilationFlagValue(GetStdAtom(stdAtom))?.IsTrue == true;

        public ZilObject? GetCompilationFlagValue(ZilAtom atom) => GetCompilationFlagValue(atom.Text);

        public ZilObject? GetCompilationFlagValue(string name)
        {
            var atom = compilationFlagsObList[name];
            return GetGlobalVal(atom);
        }

        void SetCompilationFlagValue(ZilAtom name, ZilObject? value)
        {
            name = compilationFlagsObList[name.Text];
            SetGlobalVal(name, value);
        }

        /// <summary>
        /// Initializes (or re-initializes) the default PROPDEFs.
        /// </summary>
        /// <remarks>
        /// The PROPDEFs are version-specific, so this should be called again after changing <see cref="ZModel.ZEnvironment"/>;
        /// </remarks>
        void InitPropDefs()
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

            InitPropDef(StdAtom.DIRECTIONS, ZEnvironment.ZVersion == 3 ? SDirectionsPropDef_V3 : SDirectionsPropDef_V4_Etc);
        }

        void InitPropDef(StdAtom propName, string def)
        {
            Program.Evaluate(this, "<BLOCK (<ROOT>)>");
            ZilVector vector;
            try
            {
                vector = (ZilVector)Program.Evaluate(this, def, true)!;
                Debug.Assert(vector != null, "empty initial PROPDEF");
            }
            finally
            {
                Program.Evaluate(this, "<ENDBLOCK>");
            }
            var pattern = ComplexPropDef.Parse(vector);
            SetPropDef(GetStdAtom(propName), pattern);
        }

        public void SetPropDef(ZilAtom propName, ComplexPropDef pattern)
        {
            foreach (var (name, value) in pattern.GetConstants(this))
            {
                AddZConstant(name, new ZilFix(value));
            }
            PutProp(propName, GetStdAtom(StdAtom.PROPSPEC), pattern);
        }

        public Stream OpenChannelStream(string path, FileAccess fileAccess)
        {
            if (TopFrame.SourceLine is FileSourceLine fileSourceLine)
            {
                var dir = Path.GetDirectoryName(fileSourceLine.FileName);
                Debug.Assert(dir != null);
                path = Path.Combine(dir, path);
            }

            var mode = fileAccess switch
            {
                FileAccess.ReadWrite => FileMode.OpenOrCreate,
                FileAccess.Write => FileMode.Create,
                _ => FileMode.Open
            };

            return new FileStream(path, mode, fileAccess);
        }

        /// <summary>
        /// Pushes a new LVAL for the atom OBLIST, to be used for looking up atoms.
        /// </summary>
        /// <param name="newObPath">A list to serve as the new LVAL of OBLIST.</param>
        public void PushObPath(ZilList newObPath)
        {
            var atom = GetStdAtom(StdAtom.OBLIST);
            var old = GetLocalVal(atom) ?? new ZilList(null, null);

            previousObPaths.Push(old);
            SetLocalVal(atom, newObPath);
        }

        // ReSharper disable ExceptionNotThrown
        /// <summary>
        /// Restores the previous LVAL of the atom OBLIST that existed before a corresponding
        /// call to <see cref="PushObPath"/>.
        /// </summary>
        /// <returns>The current LVAL of OBLIST, or <see langword="null"/> if it's currently unassigned.</returns>
        /// <exception cref="InvalidOperationException">There was no previous LVAL of OBLIST.</exception>
        public ZilObject? PopObPath()
        {
            var atom = GetStdAtom(StdAtom.OBLIST);
            var old = GetLocalVal(atom);

            // may throw InvalidOperationException
            var popped = previousObPaths.Pop();

            SetLocalVal(atom, popped);
            return old;
        }
        // ReSharper restore ExceptionNotThrown

        public ZilActivation? GetEnclosingProgActivation() => GetLocalVal(EnclosingProgActivationAtom) as ZilActivation;

        public ZilAtom EnclosingProgActivationAtom { get; }

        public ZilObject? RunHook(string name, params ZilObject[] args)
        {
            var hook = GetGlobalVal(hooksObList[name]);

            // ReSharper disable once PatternAlwaysOfType
            if (hook != null && hook.IsApplicable(this, out var applicable))
            {
                return (ZilObject)applicable.ApplyNoEval(this, args);
            }

            return null;
        }

        [SuppressMessage("ReSharper", "PatternAlwaysOfType")]
        public ReturnQuirkMode ReturnQuirkMode =>
            GetGlobalVal(GetStdAtom(StdAtom.DO_FUNNY_RETURN_P))?.IsTrue switch
            {
                true => ReturnQuirkMode.PreferRoutine,
                false => ReturnQuirkMode.PreferBlock,
                _ => ReturnQuirkMode.ByVersion
            };

        #region IParserSite

        string IParserSite.CurrentFilePath => CurrentFile.Path;

        ZilAtom IParserSite.ParseAtom(string text) => ZilAtom.Parse(text, this);

        ZilAtom IParserSite.GetTypeAtom(ZilObject zo) => zo.GetTypeAtom(this);

        ZilObject IParserSite.Evaluate(ZilObject zo) => (ZilObject)zo.Eval(this);

        SimplePrefixMacroHandler? IParserSite.GetPrefixMacro(char prefix) => ParserMacros.GetPrefixMacro(prefix);

        #endregion
    }
}
