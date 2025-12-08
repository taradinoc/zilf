/* Copyright 2010-2023 Tara McGrew
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

using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using Zilf.Diagnostics;
using Zilf.Emit;
using Zilf.Interpreter;
using Zilf.Interpreter.Values;
using Zilf.Language;
using Zilf.ZModel;
using Zilf.ZModel.Values;
using Zilf.ZModel.Vocab;

namespace Zilf.Compiler
{
    partial class Compilation
    {
        public static void Compile(Context ctx, IGameBuilder gb)
        {
            var compilation = new Compilation(ctx, gb, gb.DebugFile != null && ctx.WantDebugInfo);
            compilation.Compile();
        }

        void Compile()
        {
            /* Many of these compilation steps invoke the interpreter.
             * Interpreted code can observe changes in the global state during compilation,
             * and it may expect parts of that state to be set before it runs.
             * Thus, the relative order of these steps should be preserved.
             */

            PrepareEarlyRoutineBuilders();
            PreparePropertyBuilders();
            PrepareHighestFlagBuilders();
            PrepareObjectBuilders(out var lastObject);
            EnforcePropertyLimit();
            PrepareTableBuilders();

            PrepareSelfInsertingBreaks();
            PreparePunctuationWords(out var punctWords);
            PrepareBuzzWords();
            PlanVocabMerges(out var vocabMerges);
            DefineVocabWords();
            PreparePunctuationAliasesAndPlanMerges(punctWords, vocabMerges);
            PerformVocabMerges(vocabMerges);
            PrepareLateSyntaxTableBuilders();
            BuildEarlySyntaxTables();
            CopyVocabSynonymValues();

            PrepareFlagAliases();
            EnforceFlagLimit();

            var globalInitializers = new Queue<System.Action>(Context.ZEnvironment.Globals.Count + 10);

            PrepareAndCheckGlobalStorage(globalInitializers, out var reservedGlobals);
            PrepareConstantBuilders(lastObject);
            PrepareLongWordTableBuilder(out var longWordTable);
            PrepareVocabConstant();
            PrepareHardGlobalBuilders(globalInitializers);
            PrepareReservedGlobalBuilders(reservedGlobals);
            PrepareGlobalDefaults(globalInitializers);

            PreparePropertyDefaults();
            PrepareLateRoutineBuilders();

            EnterZilch();
            try
            {
                ExpandRoutineBodies();
                AnalyzeAndPlanRoutines();
                GenerateRoutineCode();
                WarnAboutUnusedRoutines();
            }
            finally
            {
                ExitZilch();
            }

            BuildObjects();

            BuildVocabWords(longWordTable, out var longWords);

            BuildLongWordTable(longWordTable, longWords);
            BuildLateSyntaxTables();
            BuildUserDefinedTables();
            BuildUnicodeTranslationTable();
            BuildHeaderExtensionTable();

            WarnAboutUnusedGlobals();

            Game.Finish();
        }

        void HandleZValChangedWhileCompilingRoutine(object? sender, ZValEventArgs e)
        {
            switch (e.NewValue)
            {
                case ZilGlobal g:
                    if (!Globals.ContainsKey(g.Name))
                    {
                        var glb = Game.DefineGlobal(g.Name.Text);
                        glb.DefaultValue = GetGlobalDefaultValue(g);
                        Globals.Add(g.Name, glb);
                    }
                    break;
            }
        }

        void BuildUserDefinedTables()
        {
            // build tables
            foreach (var pair in Tables)
            {
                BuildTable(pair.Key, pair.Value);
            }
        }

        void BuildUnicodeTranslationTable()
        {
            var zversion = Context.ZEnvironment.ZVersion;
            if (zversion < 5 || zversion == ZEnvironment.GLULX_ZVERSION)
                return;

            var usage = Context.ZEnvironment.UnicodeUsage;
            if (!usage.NeedsCustomTable || usage.Characters.Count == 0)
                return;

            unicodeTranslationTableOperand = Game.DefineUnicodeTranslationTable(usage.Characters);
            Context.ZEnvironment.EnsureMinimumHeaderExtension(4);
        }

        [System.Diagnostics.CodeAnalysis.SuppressMessage("Globalization", "CA1308:Normalize strings to uppercase", Justification = "Z-machine requirement.")]
        void BuildLongWordTable(ITableBuilder? longWordTable, Queue<IWord>? longWords)
        {
            if (longWords == null)
                return;

            Debug.Assert(longWordTable != null);

            longWordTable.AddWord((short)longWords.Count);
            while (longWords.Count > 0)
            {
                var word = longWords.Dequeue();
                var wb = Vocabulary[word];
                longWordTable.AddWord(wb);
                longWordTable.AddWord(Game.MakeOperand(word.Atom.Text.ToLowerInvariant()));
            }
        }

        void BuildVocabWords(ITableBuilder? longWordTable, out Queue<IWord>? longWords)
        {
            // build vocabulary
            longWords = longWordTable == null ? null : new Queue<IWord>();

            var helpers = new WriteToBuilderHelpers
            {
                CompileConstantDelegate = CompileConstant,
                DirIndexToPropertyOperandDelegate = di =>
                {
                    Debug.Assert(di < Context.ZEnvironment.Directions.Count);
                    var dir = Context.ZEnvironment.Directions[di];
                    return Properties[dir];
                }
            };

            var builtWords = new HashSet<IWordBuilder>();
            foreach (var pair in Vocabulary)
            {
                var word = pair.Key;
                var wb = pair.Value;

                if (builtWords.Contains(wb))
                    continue;

                builtWords.Add(wb);

                Context.ZEnvironment.VocabFormat.WriteToBuilder(word, wb, helpers);

                if (longWords != null && Context.ZEnvironment.IsLongWord(word))
                {
                    longWords.Enqueue(word);
                }
            }
        }

        void BuildObjects()
        {
            // build objects
            foreach (var obj in Context.ZEnvironment.ObjectsInInsertionOrder())
            {
                var ob = Objects[obj.Name];
                try
                {
                    using (DiagnosticContext.Push(obj.SourceLine))
                    {
                        BuildObject(obj, ob);
                    }
                }
                catch (ZilError ex)
                {
                    Context.HandleError(ex);
                }
            }
        }

        void ExitZilch()
        {
            // ...and we're done generating code
            Context.DefineCompilationFlag(Context.GetStdAtom(StdAtom.IN_ZILCH), Context.FALSE, true);
        }

        void ExpandRoutineBodies()
        {
            Context.ZValChanged += HandleZValChangedWhileCompilingRoutine;
            try
            {
                foreach (var routine in Context.ZEnvironment.Routines)
                {
                    routine.ExpandInPlace(Context);
                }
            }
            finally
            {
                Context.ZValChanged -= HandleZValChangedWhileCompilingRoutine;
            }
        }

        void GenerateRoutineCode()
        {
            var compiled = new HashSet<ZilAtom>(new AtomNameEqualityComparer(Context.IgnoreCase));
            IRoutineBuilder? mainRoutine = null;

            bool compiledNew;
            do
            {
                compiledNew = false;

                foreach (var routine in Context.ZEnvironment.Routines)
                {
                    if (routine.Name == null)
                        continue;

                    if (_routinesToCompile != null && !_routinesToCompile.Contains(routine.Name))
                    {
                        // skipped due to being unreferenced
                        continue;
                    }

                    if (!compiled.Add(routine.Name))
                        continue;

                    var entryPoint = routine.Name == Context.ZEnvironment.EntryRoutineName;
                    Debug.Assert(Routines.ContainsKey(routine.Name));
                    var rb = Routines[routine.Name];
                    try
                    {
                        using (DiagnosticContext.Push(routine.SourceLine))
                        {
                            BuildRoutine(routine, rb, entryPoint, Context.TraceRoutines);
                        }
                    }
                    catch (ZilError ex)
                    {
                        // could be a compiler error, or an interpreter error thrown by macro evaluation
                        Context.HandleError(ex);
                    }
                    rb.Finish();

                    if (entryPoint)
                        mainRoutine = rb;
                }

                if (_routinesToCompile != null && _operandReferencedRoutineNames != null)
                {
                    foreach (var referenced in _operandReferencedRoutineNames)
                    {
                        if (_routinesToCompile.Add(referenced))
                        {
                            _maybeUnusedRoutineNames?.Remove(referenced);
                            if (!compiled.Contains(referenced))
                            {
                                compiledNew = true;
                            }
                        }
                        else if (_routinesToCompile.Contains(referenced))
                        {
                            _maybeUnusedRoutineNames?.Remove(referenced);
                            if (!compiled.Contains(referenced))
                            {
                                compiledNew = true;
                            }
                        }
                    }
                    _operandReferencedRoutineNames.Clear();
                }
            }
            while (compiledNew);

            if (mainRoutine == null)
                throw new CompilerError(CompilerMessages.Missing_GO_Routine);
        }

        void ScheduleRoutineForCompilation(ZilAtom routineName)
        {
            if (_routinesToCompile == null)
                return;

            _operandReferencedRoutineNames ??= new HashSet<ZilAtom>(new AtomNameEqualityComparer(Context.IgnoreCase));
            _operandReferencedRoutineNames.Add(routineName);
        }

        void WarnAboutUnusedRoutines()
        {
            if (_maybeUnusedRoutineNames == null || _maybeUnusedRoutineNames.Count == 0)
                return;

            var comparer = new AtomNameEqualityComparer(Context.IgnoreCase);
            var entry = Context.ZEnvironment.EntryRoutineName;

            foreach (var name in _maybeUnusedRoutineNames)
            {
                if (entry != null && comparer.Equals(name, entry))
                    continue;

                if (_suppressUnusedRoutineWarnings != null && _suppressUnusedRoutineWarnings.Contains(name))
                    continue;

                if (_routineDefinitionsByName != null && _routineDefinitionsByName.TryGetValue(name, out var routine))
                {
                    Context.HandleError(new CompilerError(
                        routine.SourceLine,
                        CompilerMessages.Routine_0_Is_Defined_But_Never_Used,
                        name));
                }
            }
        }

        // Build a routine reference graph from expanded bodies and previously recorded constant/global references,
        // compute the reachable set, warn about unreferenced routines, and plan which to compile.
        void AnalyzeAndPlanRoutines()
        {
            // Build reverse map from routine builders to their atoms for quick identification of routine constants
            var routineByBuilder = Routines.ToDictionary(kvp => kvp.Value, kvp => kvp.Key);

            var comparer = new AtomNameEqualityComparer(Context.IgnoreCase);
            var allRoutineNames = new HashSet<ZilAtom>(Context.ZEnvironment.Routines.Select(r => r.Name!), comparer);
            var keepRoutines = new HashSet<ZilAtom>(comparer);
            var suppressUnusedWarnings = new HashSet<ZilAtom>(comparer);

            // Map routine -> direct routine references found in its body (calls or routine constants)
            var adjacency = new Dictionary<ZilAtom, HashSet<ZilAtom>>(comparer);
            var routineByName = new Dictionary<ZilAtom, ZilRoutine>(comparer);

            foreach (var r in Context.ZEnvironment.Routines)
            {
                if (r.Name == null)
                    continue;
                routineByName[r.Name] = r;

                if ((r.Flags & RoutineFlags.Keep) != 0)
                    keepRoutines.Add(r.Name);

                if ((r.Flags & RoutineFlags.SuppressUnusedWarning) != 0)
                    suppressUnusedWarnings.Add(r.Name);

                var refs = new HashSet<ZilAtom>(comparer);

                foreach (var bodyItem in r.Body)
                {
                    CollectRoutineConstants(bodyItem, refs, suppressGlobalReads: false);
                }

                // Walk all FORMs in the routine body to find calls and nested constants
                r.WalkRoutineForms(f =>
                {
                    // Detect routine calls via head resolution
                    if (f.First is ZilAtom head)
                    {
                        var interned = Context.ZEnvironment.InternGlobalName(head);
                        var obj = Context.GetZVal(interned);
                        while (obj is ZilConstant c)
                            obj = c.Value;
                        if (obj is ZilRoutine called && called.Name != null)
                        {
                            refs.Add(called.Name);
                        }
                        else if (allRoutineNames.Contains(interned))
                        {
                            refs.Add(interned);
                        }
                    }

                    // Recursively scan arguments for routine constants
                    if (f.Rest != null)
                    {
                        foreach (var arg in f.Rest)
                        {
                            CollectRoutineConstants(arg, refs, suppressGlobalReads: false);
                        }
                    }
                });

                adjacency[r.Name] = refs;
            }

            // Seed reachable set with entry routine and any routine referenced outside routine bodies
            var reachable = new HashSet<ZilAtom>(comparer);
            if (Context.ZEnvironment.EntryRoutineName != null)
                reachable.Add(Context.ZEnvironment.EntryRoutineName);

            foreach (var name in ReadAccessedGlobalNames)
            {
                if (allRoutineNames.Contains(name))
                    reachable.Add(name);
            }

            reachable.UnionWith(keepRoutines);

            // Seed with routines referenced by property defaults, object properties, or table contents.
            var dataReferencedRoutines = new HashSet<ZilAtom>(comparer);

            foreach (var pair in Context.ZEnvironment.PropertyDefaults)
            {
                CollectRoutineConstants(pair.Value, dataReferencedRoutines, suppressGlobalReads: true);
            }

            foreach (var obj in Context.ZEnvironment.Objects)
            {
                foreach (var prop in obj.Properties)
                {
                    if (prop.Rest is not { } propBody)
                        continue;

                    foreach (var element in propBody)
                    {
                        CollectRoutineConstants(element, dataReferencedRoutines, suppressGlobalReads: true);
                    }
                }
            }

            foreach (var table in Context.ZEnvironment.Tables)
            {
                var rawElements = new ZilObject?[table.ElementCount];
                table.CopyTo(rawElements, (zo, _) => zo, null, Context);

                foreach (var element in rawElements)
                {
                    if (element != null)
                    {
                        CollectRoutineConstants(element, dataReferencedRoutines, suppressGlobalReads: true);
                    }
                }
            }

            reachable.UnionWith(dataReferencedRoutines);

            // Also seed with routines referenced by syntax/verb tables (actions and preactions)
            // These are emitted into ATBL/PATBL and must not be pruned even if never called directly.
            foreach (var syn in Context.ZEnvironment.Syntaxes)
            {
                if (syn.Action != null && allRoutineNames.Contains(syn.Action))
                    reachable.Add(syn.Action);
                if (syn.Preaction != null && allRoutineNames.Contains(syn.Preaction))
                    reachable.Add(syn.Preaction);
            }

            // Traverse call graph to find all routines reachable from the seeds
            var stack = new Stack<ZilAtom>(reachable);
            while (stack.Count > 0)
            {
                var cur = stack.Pop();
                if (!adjacency.TryGetValue(cur, out var targets))
                    continue;
                foreach (var tgt in targets)
                {
                    if (allRoutineNames.Contains(tgt) && reachable.Add(tgt))
                        stack.Push(tgt);
                }
            }

            _routinesToCompile = reachable;
            _maybeUnusedRoutineNames = new HashSet<ZilAtom>(allRoutineNames, comparer);
            _maybeUnusedRoutineNames.ExceptWith(reachable);
            _suppressUnusedRoutineWarnings = suppressUnusedWarnings;
            _routineDefinitionsByName = routineByName;

            // Local function to collect routine constants appearing within any expression
            void CollectRoutineConstants(ZilObject expr, HashSet<ZilAtom> output, bool suppressGlobalReads)
            {
                var unwrapped = expr.Unwrap(Context);

                if (unwrapped is ZilMacroResult zmr)
                {
                    unwrapped = zmr.Inner;
                }

                if (unwrapped is ZilRoutine routine && routine.Name != null)
                {
                    output.Add(routine.Name);
                    return;
                }

                if (unwrapped is ZilAtom atom)
                {
                    var interned = Context.ZEnvironment.InternGlobalName(atom);
                    var zval = Context.GetZVal(interned);
                    while (zval is ZilConstant c)
                        zval = c.Value;
                    if (zval is ZilRoutine resolved && resolved.Name != null)
                    {
                        output.Add(resolved.Name);
                        return;
                    }

                    if (allRoutineNames.Contains(interned))
                    {
                        output.Add(interned);
                        return;
                    }
                }

                // Strings cannot be routine references, so skip them to avoid registering them as global strings
                if (unwrapped is ZilString)
                {
                    return;
                }

                HashSet<ZilAtom>? priorReads = null;
                if (suppressGlobalReads)
                {
                    priorReads = new HashSet<ZilAtom>(ReadAccessedGlobalNames, comparer);
                }

                // Try to interpret as a constant; if it's a routine operand, map back to its atom
                var op = CompileConstant(unwrapped, AmbiguousConstantMode.Pessimistic);
                if (op is IRoutineBuilder rb && routineByBuilder.TryGetValue(rb, out var routineAtom))
                {
                    output.Add(routineAtom);
                }

                if (suppressGlobalReads && priorReads != null)
                {
                    var newlyRead = new List<ZilAtom>();
                    foreach (var name in ReadAccessedGlobalNames)
                    {
                        if (!priorReads.Contains(name))
                            newlyRead.Add(name);
                    }

                    if (newlyRead.Count != 0)
                    {
                        foreach (var name in newlyRead)
                        {
                            ReadAccessedGlobalNames.Remove(name);
                        }
                    }
                }

                // Recurse into lists/forms
                if (unwrapped is ZilListBase list)
                {
                    foreach (var item in list)
                    {
                        CollectRoutineConstants(item, output, suppressGlobalReads);
                    }
                }
            }
        }

        void EnterZilch()
        {
            // let macros know we're generating code now
            Context.DefineCompilationFlag(Context.GetStdAtom(StdAtom.IN_ZILCH), Context.TRUE, true);
        }

        void PrepareLateRoutineBuilders()
        {
            // builders for routines (again, in case any were added during compilation, e.g. by a PROPSPEC)
            foreach (var routine in Context.ZEnvironment.Routines)
            {
                Debug.Assert(routine.Name != null);
                if (!Routines.ContainsKey(routine.Name))
                    Routines.Add(routine.Name, Game.DefineRoutine(
                        routine.Name.Text,
                        routine.Name == Context.ZEnvironment.EntryRoutineName,
                        (routine.Flags & RoutineFlags.CleanStack) != 0));
            }
        }

        void PreparePropertyDefaults()
        {
            // default values for properties
            foreach (var pair in Context.ZEnvironment.PropertyDefaults)
            {
                try
                {
                    using (DiagnosticContext.Push(
                        pair.Value.SourceLine ??
                        new StringSourceLine($"<property default for '{pair.Key}'>")))
                    {
                        var pb = Properties[pair.Key];

                        pb.DefaultValue = CompileConstant(pair.Value);

                        if (pb.DefaultValue == null)
                            throw new CompilerError(
                                CompilerMessages.Nonconstant_Initializer_For_0_1_2,
                                "property default",
                                pair.Key,
                                pair.Value.ToStringContext(Context, false));
                    }
                }
                catch (ZilError ex)
                {
                    Context.HandleError(ex);
                }
            }
        }

        void PrepareReservedGlobalBuilders(string[] reservedGlobals)
        {
            // implicitly defined globals
            // NOTE: the parameter to DoFunnyGlobals() above must match the number of globals implicitly defined here
            foreach (var name in reservedGlobals)
            {
                var glb = Game.DefineGlobal(name);
                var atom = Context.RootObList[name];
                Globals.Add(atom, glb);
            }
        }

        void PrepareHardGlobalBuilders(Queue<System.Action> globalInitializers)
        {
            // builders and values for globals (which may refer to constants)
            foreach (var global in Context.ZEnvironment.Globals)
            {
                if (global.StorageType == GlobalStorageType.Hard)
                {
                    var glb = Game.DefineGlobal(global.Name.Text);
                    Globals.Add(global.Name, glb);
                    var globalSave = global;
                    globalInitializers.Enqueue(() => glb.DefaultValue = GetGlobalDefaultValue(globalSave));
                }
            }
        }

        static void PrepareGlobalDefaults(Queue<System.Action> globalInitializers)
        {
            while (globalInitializers.Count > 0)
                globalInitializers.Dequeue()?.Invoke();
        }

        void PrepareVocabConstant()
        {
            Constants.Add(Context.GetStdAtom(StdAtom.VOCAB), Game.VocabularyTable);
        }

        void PrepareLongWordTableBuilder(out ITableBuilder? longWordTable)
        {
            if (Context.GetCompilationFlagOption(StdAtom.LONG_WORDS))
            {
                longWordTable = Game.DefineTable("LONG-WORD-TABLE", true);
                Constants.Add(Context.GetStdAtom(StdAtom.LONG_WORD_TABLE), longWordTable);
            }
            else
            {
                longWordTable = null;
            }
        }

        void PrepareConstantBuilders(ZilModelObject? lastObject)
        {
            // builders and values for constants (which may refer to vocabulary,
            // routines, tables, objects, properties, or flags)
            foreach (var constant in Context.ZEnvironment.Constants)
            {
                IOperand? value;
                if (constant.Name.StdAtom == StdAtom.LAST_OBJECT && lastObject != null)
                {
                    value = Objects[lastObject.Name];
                }
                else
                {
                    value = CompileConstant(constant.Value);
                }

                if (value == null)
                {
                    Context.HandleError(new CompilerError(
                        constant,
                        CompilerMessages.Nonconstant_Initializer_For_0_1_2,
                        "constant",
                        constant.Name,
                        constant.Value.ToStringContext(Context, false)));
                    value = Game.Zero;
                }

                // If the value is already a table builder (whose symbol was already defined
                // during PrepareTableBuilders), just add it to the Constants dictionary
                // without calling DefineConstant
                if (value is ITableBuilder tableBuilder)
                {
                    Constants.Add(constant.Name, tableBuilder);
                }
                else
                {
                    Constants.Add(constant.Name, Game.DefineConstant(constant.Name.Text, value));
                }
            }
        }

        void PrepareAndCheckGlobalStorage(Queue<System.Action> globalInitializers,
             out string[] reservedGlobals)
        {
            // FUNNY-GLOBALS?
            reservedGlobals = Context.ZEnvironment.VocabFormat.GetReservedGlobalNames();
            if (Context.GetGlobalOption(StdAtom.DO_FUNNY_GLOBALS_P))
            {
                // this sets StorageType for all variables, queues the soft globals' initializers,
                // and creates the soft globals table and variable if needed
                DoFunnyGlobals(reservedGlobals.Length, globalInitializers);
            }
            else
            {
                foreach (var g in Context.ZEnvironment.Globals)
                    g.StorageType = GlobalStorageType.Hard;

                if (Context.ZEnvironment.Globals.Count > 240 - reservedGlobals.Length)
                {
                    Context.HandleError(new CompilerError(
                        CompilerMessages.Too_Many_0_1_Defined_Only_2_Allowed,
                        "globals",
                        Context.ZEnvironment.Globals.Count,
                        240 - reservedGlobals.Length));
                }
            }
        }

        void EnforceFlagLimit()
        {
            // enforce limit on number of flags
            if (UniqueFlags > Game.MaxFlags)
            {
                var err = new CompilerError(
                    CompilerMessages.Too_Many_0_1_Defined_Only_2_Allowed,
                    "flags",
                    UniqueFlags,
                    Game.MaxFlags);

                // If the number of flags would be legal in V4+, attach an informational
                // subdiagnostic indicating that it's legal in other Z-machine versions
                // (V4+ supports up to 48 flags).
                if (UniqueFlags <= 48)
                {
                    var info = new CompilerError(CompilerMessages.This_Would_Be_Legal_In_Other_Zmachine_Versions_Eg_V0, 4);
                    err = err.Combine(info);
                }

                Context.HandleError(err);
            }
        }

        void EnforcePropertyLimit()
        {
            // enforce limit on number of properties
            if (Properties.Count > Game.MaxProperties)
            {
                var count = Properties.Count;
                var err = new CompilerError(
                    CompilerMessages.Too_Many_0_1_Defined_Only_2_Allowed,
                    "properties",
                    count,
                    Game.MaxProperties);

                // If the number of properties would be legal in V4+ (63 or less), attach info
                if (count <= 63)
                {
                    var info = new CompilerError(CompilerMessages.This_Would_Be_Legal_In_Other_Zmachine_Versions_Eg_V0, 4);
                    err = err.Combine(info);
                }

                Context.HandleError(err);
            }
        }

        void PrepareFlagAliases()
        {
            // may as well do bit synonyms here too
            foreach (var (alias, original) in Context.ZEnvironment.BitSynonyms)
            {
                DefineFlagAlias(alias, original);
            }
        }

        void CopyVocabSynonymValues()
        {
            // now that all the vocabulary is set up, copy values for synonyms
            foreach (var syn in Context.ZEnvironment.Synonyms)
                syn.Apply(Context);
        }

        void PrepareLateSyntaxTableBuilders()
        {
            // constants and builders for late syntax tables
            foreach (var name in Context.ZEnvironment.VocabFormat.GetLateSyntaxTableNames())
            {
                var tb = Game.DefineTable(name, true);
                var atom = Context.RootObList[name];
                Constants.Add(atom, tb);

                // this hack lets macros use it as a compile-time value, as long as they don't access its contents
                Context.SetGlobalVal(atom, atom);
            }
        }

        void PerformVocabMerges(Dictionary<IWord, IWord> vocabMerges)
        {
            string[] wordConstantPrefixes = { "W?", "A?", "ACT?", "PR?" };

            foreach (var (dupWord, mainWord) in vocabMerges)
            {
                Vocabulary[dupWord] = Vocabulary[mainWord];

                foreach (var prefix in wordConstantPrefixes)
                {
                    var mainAtom = ZilAtom.Parse(prefix + mainWord.Atom.Text, Context);

                    if (!Constants.TryGetValue(mainAtom, out var value))
                        continue;

                    var dupAtom = ZilAtom.Parse(prefix + dupWord.Atom.Text, Context);
                    Constants[dupAtom] = value;
                }
            }
        }

        void PreparePunctuationAliasesAndPlanMerges(Dictionary<string, string> punctWords,
            Dictionary<IWord, IWord> vocabMerges)
        {
            foreach (var (name, symbol) in punctWords)
            {
                var nameAtom = ZilAtom.Parse(name, Context);
                var symbolAtom = ZilAtom.Parse(symbol, Context);

                if (!Context.ZEnvironment.Vocabulary.TryGetValue(symbolAtom, out var symbolWord) ||
                    Context.ZEnvironment.Vocabulary.ContainsKey(nameAtom))
                {
                    continue;
                }

                var nameWord = Context.ZEnvironment.VocabFormat.CreateWord(nameAtom);
                Context.ZEnvironment.VocabFormat.MakeSynonym(nameWord, symbolWord);
                vocabMerges.Add(nameWord, symbolWord);
            }
        }

        void DefineVocabWords()
        {
            foreach (var word in Context.ZEnvironment.Vocabulary.Values)
            {
                DefineWord(word);
            }
        }

        void PlanVocabMerges(out Dictionary<IWord, IWord> vocabMerges)
        {
            var merges = new Dictionary<IWord, IWord>();
            Context.ZEnvironment.MergeVocabulary((mainWord, duplicateWord, blameV3) =>
            {
                var warning = new CompilerError(
                    CompilerMessages.Vocab_Collision_0_And_1_Are_Indistinguishable_And_Will_Be_Merged,
                    mainWord.Atom.Text.ToUpperInvariant(),
                    duplicateWord.Atom.Text.ToUpperInvariant());

                if (blameV3)
                    warning = warning.Combine(new CompilerError(CompilerMessages.They_Would_Be_Distinguishable_In_Zmachine_V4_Or_Above));

                Context.HandleError(warning);

                Game.RemoveVocabularyWord(duplicateWord.Atom.Text);
                merges.Add(duplicateWord, mainWord);
            });
            vocabMerges = merges;
        }

        void PrepareBuzzWords()
        {
            foreach (var pair in Context.ZEnvironment.Buzzwords)
            {
                Context.ZEnvironment.GetVocabBuzzword(pair.Key, pair.Value);
            }
        }

        void PreparePunctuationWords(out Dictionary<string, string> punctWords)
        {
            // vocabulary for punctuation
            punctWords = new Dictionary<string, string>
            {
                { "PERIOD", "." },
                { "COMMA", "," },
                { "QUOTE", "\"" },
                { "APOSTROPHE", "'" }
            };

            foreach (var symbol in punctWords.Values)
            {
                var symbolAtom = ZilAtom.Parse(symbol, Context);
                Context.ZEnvironment.GetVocab(symbolAtom);
            }
        }

        void PrepareSelfInsertingBreaks()
        {
            // self-inserting breaks
            if (Context.GetGlobalVal(Context.GetStdAtom(StdAtom.SIBREAKS)) is not ZilString siBreaks)
                return;

            Game.SelfInsertingBreaks.Clear();
            foreach (var c in siBreaks.Text)
                Game.SelfInsertingBreaks.Add(c);
        }

        void PrepareTableBuilders()
        {
            // builders for tables
            ITableBuilder? firstPureTable = null;

            static int ParserTablesFirst(ZilTable t)
            {
                return (t.Flags & TableFormat.ParserTable) != 0 ? 1 : 2;
            }

            // Check if we're compiling for Glulx and have traced tables
            var glulxGameBuilder = Game as Emit.Glulx.GameBuilder;
            var tracedTableNames = Context.ZEnvironment.TracedTableWrites;

            foreach (var table in Context.ZEnvironment.Tables.OrderBy(ParserTablesFirst))
            {
                var pure = (table.Flags & TableFormat.Pure) != 0;
                var builder = Game.DefineTable(table.Name, pure);
                Tables.Add(table, builder);

                // If this table is being traced and we're using Glulx, mark it for tracing
                if (glulxGameBuilder != null && table.Name != null)
                {
                    foreach (var tracedAtom in tracedTableNames)
                    {
                        // Table names can match directly, or with a "T?" prefix (for globals)
                        var tracedName = tracedAtom.Text;
                        if (string.Equals(tracedName, table.Name, System.StringComparison.OrdinalIgnoreCase) ||
                            string.Equals("T?" + tracedName, table.Name, System.StringComparison.OrdinalIgnoreCase))
                        {
                            glulxGameBuilder.TraceTable(builder, tracedName);
                            break;
                        }
                    }
                }

                if (pure && firstPureTable == null)
                    firstPureTable = builder;
            }

            if (firstPureTable != null)
            {
                Constants.Add(Context.GetStdAtom(StdAtom.PRSTBL), firstPureTable);
            }
        }

        void PrepareObjectBuilders(out ZilModelObject? lastObject)
        {
            // builders for objects
            lastObject = null;

            string? GetGlobalSymbolType(ZilAtom atom)
            {
                return Game.IsGloballyDefined(atom.Text, out var type) ? type : null;
            }

            foreach (var obj in Context.ZEnvironment.ObjectsInDefinitionOrder(GetGlobalSymbolType))
            {
                lastObject = obj;
                Objects.Add(obj.Name, Game.DefineObject(obj.Name.Text));
                // builders for the rest of the properties and flags,
                // and vocabulary for names
                PreBuildObject(obj);
            }
        }

        void PrepareHighestFlagBuilders()
        {
            // builders for flags that need to be numbered highest (explicitly listed or used in syntax)
            ZilAtom GetOriginal(ZilAtom flag)
            {
                return Context.ZEnvironment.TryGetBitSynonym(flag, out var orig) ? orig : flag;
            }

            var highestFlags =
                Context.ZEnvironment.FlagsOrderedLast
                    .Concat(
                        from syn in Context.ZEnvironment.Syntaxes
                        from flag in new[] { syn.FindFlag1, syn.FindFlag2 }
                        where flag != null
                        select GetOriginal(flag))
                    .Distinct()
                    .ToList();

            if (highestFlags.Count >= Game.MaxFlags)
            {
                Context.HandleError(new CompilerError(
                    CompilerMessages.Too_Many_0_1_Defined_Only_2_Allowed,
                    "flags requiring high numbers",
                    highestFlags.Count,
                    Game.MaxFlags));
            }

            foreach (var flag in highestFlags)
            {
                DefineFlag(flag);
            }
        }

        void PreparePropertyBuilders()
        {
            // builders and constants for some properties
            foreach (var dir in Context.ZEnvironment.Directions)
                DefineProperty(dir);

            // create a constant for the last explicitly defined direction
            var lowDir = Context.ZEnvironment.LowDirection;
            if (lowDir != null)
            {
                Constants.Add(Context.GetStdAtom(StdAtom.LOW_DIRECTION),
                    Properties[lowDir]);
            }

            // builders and constants for some more properties
            foreach (var pair in Context.ZEnvironment.PropertyDefaults)
            {
                DefineProperty(pair.Key);
            }
        }

        void PrepareEarlyRoutineBuilders()
        {
            // builders for routines
            if (Context.ZEnvironment.EntryRoutineName == null)
                Context.ZEnvironment.EntryRoutineName = Context.GetStdAtom(StdAtom.GO);

            foreach (var routine in Context.ZEnvironment.Routines)
            {
                Debug.Assert(routine.Name != null);

                Routines.Add(routine.Name, Game.DefineRoutine(
                    routine.Name.Text,
                    routine.Name == Context.ZEnvironment.EntryRoutineName,
                    (routine.Flags & RoutineFlags.CleanStack) != 0));
            }
        }
    }
}
