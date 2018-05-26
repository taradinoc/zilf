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

using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using JetBrains.Annotations;
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
        public static void Compile([NotNull] Context ctx, [NotNull] IGameBuilder gb)
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
            PrepareObjectBuilders(out ZilModelObject lastObject);
            PrepareTableBuilders();

            PrepareSelfInsertingBreaks();
            PreparePunctuationWords(out Dictionary<string, string> punctWords);
            PrepareBuzzWords();
            PlanVocabMerges(out Dictionary<IWord, IWord> vocabMerges);
            DefineVocabWords();
            PreparePunctuationAliasesAndPlanMerges(punctWords, vocabMerges);
            PerformVocabMerges(vocabMerges);
            PrepareLateSyntaxTableBuilders();
            BuildEarlySyntaxTables();
            CopyVocabSynonymValues();

            PrepareFlagAliases();
            EnforceFlagLimit();

            var globalInitializers = new Queue<System.Action>(Context.ZEnvironment.Globals.Count + 10);

            PrepareAndCheckGlobalStorage(globalInitializers, out string[] reservedGlobals);
            PrepareConstantBuilders(lastObject);
            PrepareLongWordTableBuilder(out ITableBuilder longWordTable);
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
                GenerateRoutineCode();
            }
            finally
            {
                ExitZilch();
            }

            BuildObjects();

            BuildVocabWords(longWordTable, out Queue<IWord> longWords);

            BuildLongWordTable(longWordTable, longWords);
            BuildLateSyntaxTables();
            BuildUserDefinedTables();
            BuildHeaderExtensionTable();

            Game.Finish();
        }

        void HandleZValChangedWhileCompilingRoutine([NotNull] object sender, [NotNull] ZValEventArgs e)
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

        [ContractAnnotation("longWords: notnull => longWordTable: notnull")]
        void BuildLongWordTable([CanBeNull] ITableBuilder longWordTable, [CanBeNull][ItemNotNull] Queue<IWord> longWords)
        {
            if (longWords == null)
                return;

            Debug.Assert(longWordTable != null);

            longWordTable.AddShort((short)longWords.Count);
            while (longWords.Count > 0)
            {
                var word = longWords.Dequeue();
                var wb = Vocabulary[word];
                longWordTable.AddShort(wb);
                longWordTable.AddShort(Game.MakeOperand(word.Atom.Text.ToLowerInvariant()));
            }
        }

        void BuildVocabWords([CanBeNull] ITableBuilder longWordTable, [CanBeNull] out Queue<IWord> longWords)
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
            // compile routines
            IRoutineBuilder mainRoutine = null;

            foreach (var routine in Context.ZEnvironment.Routines)
            {
                var entryPoint = routine.Name == Context.ZEnvironment.EntryRoutineName;
                Debug.Assert(routine.Name != null);
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

            if (mainRoutine == null)
                throw new CompilerError(CompilerMessages.Missing_GO_Routine);
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

        void PrepareReservedGlobalBuilders([ItemNotNull] [NotNull] string[] reservedGlobals)
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

        void PrepareHardGlobalBuilders([ItemNotNull] [NotNull] Queue<System.Action> globalInitializers)
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

        static void PrepareGlobalDefaults([ItemNotNull] [NotNull] Queue<System.Action> globalInitializers)
        {
            while (globalInitializers.Count > 0)
                globalInitializers.Dequeue()?.Invoke();
        }

        void PrepareVocabConstant()
        {
            Constants.Add(Context.GetStdAtom(StdAtom.VOCAB), Game.VocabularyTable);
        }

        void PrepareLongWordTableBuilder([CanBeNull] out ITableBuilder longWordTable)
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

        void PrepareConstantBuilders([CanBeNull] ZilModelObject lastObject)
        {
            // builders and values for constants (which may refer to vocabulary,
            // routines, tables, objects, properties, or flags)
            foreach (var constant in Context.ZEnvironment.Constants)
            {
                IOperand value;
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

                Constants.Add(constant.Name, Game.DefineConstant(constant.Name.Text, value));
            }
        }

        void PrepareAndCheckGlobalStorage([NotNull] [ItemNotNull] Queue<System.Action> globalInitializers,
            [ItemNotNull] [NotNull] out string[] reservedGlobals)
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
                    Context.HandleError(new CompilerError(
                        CompilerMessages.Too_Many_0_1_Defined_Only_2_Allowed,
                        "globals",
                        Context.ZEnvironment.Globals.Count,
                        240 - reservedGlobals.Length));
            }
        }

        void EnforceFlagLimit()
        {
            // enforce limit on number of flags
            if (UniqueFlags > Game.MaxFlags)
                Context.HandleError(new CompilerError(
                    CompilerMessages.Too_Many_0_1_Defined_Only_2_Allowed,
                    "flags",
                    UniqueFlags,
                    Game.MaxFlags));
        }

        void PrepareFlagAliases()
        {
            // may as well do bit synonyms here too
            foreach (var pair in Context.ZEnvironment.BitSynonyms)
            {
                DefineFlagAlias(pair.Key, pair.Value);
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

        void PerformVocabMerges([NotNull] Dictionary<IWord, IWord> vocabMerges)
        {
            string[] wordConstantPrefixes = { "W?", "A?", "ACT?", "PR?" };

            foreach (var pair in vocabMerges)
            {
                IWord dupWord = pair.Key, mainWord = pair.Value;
                Vocabulary[dupWord] = Vocabulary[mainWord];

                foreach (var prefix in wordConstantPrefixes)
                {
                    var mainAtom = ZilAtom.Parse(prefix + mainWord.Atom.Text, Context);

                    if (Constants.TryGetValue(mainAtom, out var value))
                    {
                        var dupAtom = ZilAtom.Parse(prefix + dupWord.Atom.Text, Context);
                        Constants[dupAtom] = value;
                    }
                }
            }
        }

        void PreparePunctuationAliasesAndPlanMerges([NotNull] Dictionary<string, string> punctWords,
            [NotNull] Dictionary<IWord, IWord> vocabMerges)
        {

            foreach (var pair in punctWords)
            {
                var nameAtom = ZilAtom.Parse(pair.Key, Context);
                var symbolAtom = ZilAtom.Parse(pair.Value, Context);

                if (Context.ZEnvironment.Vocabulary.TryGetValue(symbolAtom, out var symbolWord) &&
                    !Context.ZEnvironment.Vocabulary.ContainsKey(nameAtom))
                {
                    var nameWord = Context.ZEnvironment.VocabFormat.CreateWord(nameAtom);
                    Context.ZEnvironment.VocabFormat.MakeSynonym(nameWord, symbolWord);
                    vocabMerges.Add(nameWord, symbolWord);
                }
            }
        }

        void DefineVocabWords()
        {
            foreach (var word in Context.ZEnvironment.Vocabulary.Values)
            {
                DefineWord(word);
            }
        }

        void PlanVocabMerges([NotNull] out Dictionary<IWord, IWord> vocabMerges)
        {
            var merges = new Dictionary<IWord, IWord>();
            Context.ZEnvironment.MergeVocabulary((mainWord, duplicateWord) =>
            {
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

        void PreparePunctuationWords([NotNull] out Dictionary<string, string> punctWords)
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
            if (Context.GetGlobalVal(Context.GetStdAtom(StdAtom.SIBREAKS)) is ZilString siBreaks)
            {
                Game.SelfInsertingBreaks.Clear();
                foreach (var c in siBreaks.Text)
                    Game.SelfInsertingBreaks.Add(c);
            }
        }

        void PrepareTableBuilders()
        {
            // builders for tables
            ITableBuilder firstPureTable = null;
            int ParserTablesFirst(ZilTable t) => (t.Flags & TableFlags.ParserTable) != 0 ? 1 : 2;

            foreach (var table in Context.ZEnvironment.Tables.OrderBy(ParserTablesFirst))
            {
                var pure = (table.Flags & TableFlags.Pure) != 0;
                var builder = Game.DefineTable(table.Name, pure);
                Tables.Add(table, builder);

                if (pure && firstPureTable == null)
                    firstPureTable = builder;
            }

            if (firstPureTable != null)
            {
                Constants.Add(Context.GetStdAtom(StdAtom.PRSTBL), firstPureTable);
            }
        }

        void PrepareObjectBuilders([CanBeNull] out ZilModelObject lastObject)
        {
            // builders for objects
            lastObject = null;

            string GetGlobalSymbolType(ZilAtom atom) =>
                Game.IsGloballyDefined(atom.Text, out var type) ? type : null;

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
                Context.HandleError(new CompilerError(
                    CompilerMessages.Too_Many_0_1_Defined_Only_2_Allowed,
                    "flags requiring high numbers",
                    highestFlags.Count,
                    Game.MaxFlags));

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
