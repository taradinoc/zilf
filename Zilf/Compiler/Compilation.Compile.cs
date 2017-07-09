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

            PrepareAndCheckGlobalStorage(out string[] reservedGlobals);
            PrepareConstantBuilders(lastObject);
            PrepareLongWordTableBuilder(out ITableBuilder longWordTable);
            PrepareVocabConstant();
            PrepareHardGlobalBuilders();
            PrepareReservedGlobalBuilders(reservedGlobals);
            PreparePropertyDefaults();
            PrepareLateRoutineBuilders();

            EnterZilch();
            GenerateRoutineCode();
            ExitZilch();

            BuildObjects();

            BuildVocabWords(longWordTable, out Queue<IWord> longWords);

            BuildLongWordTable(longWordTable, longWords);
            BuildLateSyntaxTables();
            BuildUserDefinedTables();
            BuildHeaderExtensionTable();

            Game.Finish();
        }

        private void BuildUserDefinedTables()
        {
            // build tables
            foreach (KeyValuePair<ZilTable, ITableBuilder> pair in Tables)
                BuildTable(pair.Key, pair.Value);
        }

        private void BuildLongWordTable(ITableBuilder longWordTable, Queue<IWord> longWords)
        {
            if (longWords != null)
            {
                longWordTable.AddShort((short)longWords.Count);
                while (longWords.Count > 0)
                {
                    var word = longWords.Dequeue();
                    longWordTable.AddShort(Vocabulary[word]);
                    longWordTable.AddShort(this.Game.MakeOperand(word.Atom.Text.ToLowerInvariant()));
                }
            }
        }

        private void BuildVocabWords(ITableBuilder longWordTable, out Queue<IWord> longWords)
        {
            // build vocabulary
            longWords = (longWordTable == null ? null : new Queue<IWord>());

            var helpers = new WriteToBuilderHelpers
            {
                CompileConstant = CompileConstant,
                DirIndexToPropertyOperand = di => Properties[Context.ZEnvironment.Directions[di]]
            };

            var builtWords = new HashSet<IWordBuilder>();
            foreach (var pair in Vocabulary)
            {
                IWord word = pair.Key;
                IWordBuilder wb = pair.Value;

                if (builtWords.Contains(wb))
                    continue;

                builtWords.Add(wb);

                this.Context.ZEnvironment.VocabFormat.WriteToBuilder(word, wb, helpers);

                if (longWords != null && Context.ZEnvironment.IsLongWord(word))
                {
                    longWords.Enqueue(word);
                }
            }
        }

        private void BuildObjects()
        {
            // build objects
            foreach (ZilModelObject obj in Context.ZEnvironment.ObjectsInInsertionOrder())
            {
                IObjectBuilder ob = Objects[obj.Name];
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

        private void ExitZilch()
        {
            // ...and we're done generating code
            Context.DefineCompilationFlag(Context.GetStdAtom(StdAtom.IN_ZILCH), Context.FALSE, true);
        }

        private void GenerateRoutineCode()
        {
            // compile routines
            IRoutineBuilder mainRoutine = null;

            foreach (ZilRoutine routine in Context.ZEnvironment.Routines)
            {
                bool entryPoint = routine.Name == Context.ZEnvironment.EntryRoutineName;
                IRoutineBuilder rb = Routines[routine.Name];
                try
                {
                    using (DiagnosticContext.Push(routine.SourceLine))
                    {
                        BuildRoutine(routine, rb, entryPoint);
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

        private void EnterZilch()
        {
            // let macros know we're generating code now
            Context.DefineCompilationFlag(Context.GetStdAtom(StdAtom.IN_ZILCH), Context.TRUE, true);
        }

        private void PrepareLateRoutineBuilders()
        {
            // builders for routines (again, in case any were added during compilation, e.g. by a PROPSPEC)
            foreach (ZilRoutine routine in Context.ZEnvironment.Routines)
            {
                if (!Routines.ContainsKey(routine.Name))
                    Routines.Add(routine.Name, Game.DefineRoutine(
                        routine.Name.Text,
                        routine.Name == Context.ZEnvironment.EntryRoutineName,
                        (routine.Flags & RoutineFlags.CleanStack) != 0));
            }
        }

        private void PreparePropertyDefaults()
        {
            // default values for properties
            foreach (KeyValuePair<ZilAtom, ZilObject> pair in Context.ZEnvironment.PropertyDefaults)
            {
                try
                {
                    using (DiagnosticContext.Push(
                        pair.Value.SourceLine ??
                        new StringSourceLine("property default for '" + pair.Key + "'")))
                    {
                        IPropertyBuilder pb = Properties[pair.Key];
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

        private void PrepareReservedGlobalBuilders(string[] reservedGlobals)
        {
            // implicitly defined globals
            // NOTE: the parameter to DoFunnyGlobals() above must match the number of globals implicitly defined here
            foreach (var name in reservedGlobals)
            {
                var glb = this.Game.DefineGlobal(name);
                Globals.Add(Context.RootObList[name], glb);
            }
        }

        private void PrepareHardGlobalBuilders()
        {
            // builders and values for globals (which may refer to constants)
            foreach (ZilGlobal global in Context.ZEnvironment.Globals)
            {
                if (global.StorageType == GlobalStorageType.Hard)
                {
                    var glb = Game.DefineGlobal(global.Name.Text);
                    glb.DefaultValue = GetGlobalDefaultValue(global);
                    Globals.Add(global.Name, glb);
                }
            }
        }

        private void PrepareVocabConstant()
        {
            Constants.Add(this.Context.GetStdAtom(StdAtom.VOCAB), this.Game.VocabularyTable);
        }

        private void PrepareLongWordTableBuilder(out ITableBuilder longWordTable)
        {
            if (Context.GetCompilationFlagOption(StdAtom.LONG_WORDS))
            {
                longWordTable = this.Game.DefineTable("LONG-WORD-TABLE", true);
                Constants.Add(this.Context.GetStdAtom(StdAtom.LONG_WORD_TABLE), longWordTable);
            }
            else
            {
                longWordTable = null;
            }
        }

        private void PrepareConstantBuilders(ZilModelObject lastObject)
        {
            // builders and values for constants (which may refer to vocabulary,
            // routines, tables, objects, properties, or flags)
            foreach (ZilConstant constant in Context.ZEnvironment.Constants)
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

        private void PrepareAndCheckGlobalStorage(out string[] reservedGlobals)
        {
            // FUNNY-GLOBALS?
            reservedGlobals = Context.ZEnvironment.VocabFormat.GetReservedGlobalNames();
            if (Context.GetGlobalOption(StdAtom.DO_FUNNY_GLOBALS_P))
            {
                // this sets StorageType for all variables, and creates the table and global if needed
                DoFunnyGlobals(reservedGlobals.Length);
            }
            else
            {
                foreach (var g in Context.ZEnvironment.Globals)
                    g.StorageType = GlobalStorageType.Hard;

                if (Context.ZEnvironment.Globals.Count > 240 - reservedGlobals.Length)
                    this.Context.HandleError(new CompilerError(
                        CompilerMessages.Too_Many_0_1_Defined_Only_2_Allowed,
                        "globals",
                        Context.ZEnvironment.Globals.Count,
                        240 - reservedGlobals.Length));
            }
        }

        private void EnforceFlagLimit()
        {
            // enforce limit on number of flags
            if (UniqueFlags > this.Game.MaxFlags)
                this.Context.HandleError(new CompilerError(
                    CompilerMessages.Too_Many_0_1_Defined_Only_2_Allowed,
                    "flags",
                    UniqueFlags,
                    this.Game.MaxFlags));
        }

        private void PrepareFlagAliases()
        {
            // may as well do bit synonyms here too
            foreach (var pair in Context.ZEnvironment.BitSynonyms)
                DefineFlagAlias(pair.Key, pair.Value);
        }

        private void CopyVocabSynonymValues()
        {
            // now that all the vocabulary is set up, copy values for synonyms
            foreach (Synonym syn in Context.ZEnvironment.Synonyms)
                syn.Apply(Context);
        }

        private void PrepareLateSyntaxTableBuilders()
        {
            // constants and builders for late syntax tables
            foreach (var name in Context.ZEnvironment.VocabFormat.GetLateSyntaxTableNames())
            {
                var tb = this.Game.DefineTable(name, true);
                var atom = Context.RootObList[name];
                Constants.Add(atom, tb);

                // this hack lets macros use it as a compile-time value, as long as they don't access its contents
                Context.SetGlobalVal(atom, atom);
            }
        }

        private void PerformVocabMerges(Dictionary<IWord, IWord> vocabMerges)
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

        private void PreparePunctuationAliasesAndPlanMerges(Dictionary<string, string> punctWords, Dictionary<IWord, IWord> vocabMerges)
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

        private void DefineVocabWords()
        {
            foreach (IWord word in Context.ZEnvironment.Vocabulary.Values)
            {
                DefineWord(word);
            }
        }

        private void PlanVocabMerges(out Dictionary<IWord, IWord> vocabMerges)
        {
            var merges = new Dictionary<IWord, IWord>();
            Context.ZEnvironment.MergeVocabulary((mainWord, duplicateWord) =>
            {
                this.Game.RemoveVocabularyWord(duplicateWord.Atom.Text);
                merges.Add(duplicateWord, mainWord);
            });
            vocabMerges = merges;
        }

        private void PrepareBuzzWords()
        {
            foreach (var pair in Context.ZEnvironment.Buzzwords)
            {
                Context.ZEnvironment.GetVocabBuzzword(pair.Key, pair.Value);
            }
        }

        private void PreparePunctuationWords(out Dictionary<string, string> punctWords)
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

        private void PrepareSelfInsertingBreaks()
        {
            // self-inserting breaks
            if (Context.GetGlobalVal(Context.GetStdAtom(StdAtom.SIBREAKS)) is ZilString siBreaks)
            {
                Game.SelfInsertingBreaks.Clear();
                foreach (var c in siBreaks.Text)
                    Game.SelfInsertingBreaks.Add(c);
            }
        }

        private void PrepareTableBuilders()
        {
            // builders for tables
            ITableBuilder firstPureTable = null;
            Func<ZilTable, int> parserTablesFirst = t => (t.Flags & TableFlags.ParserTable) != 0 ? 1 : 2;
            foreach (ZilTable table in Context.ZEnvironment.Tables.OrderBy(parserTablesFirst))
            {
                bool pure = (table.Flags & TableFlags.Pure) != 0;
                var builder = Game.DefineTable(table.Name, pure);
                Tables.Add(table, builder);

                if (pure && firstPureTable == null)
                    firstPureTable = builder;
            }

            if (firstPureTable != null)
                Constants.Add(Context.GetStdAtom(StdAtom.PRSTBL), firstPureTable);
        }

        private void PrepareObjectBuilders(out ZilModelObject lastObject)
        {
            // builders for objects
            lastObject = null;

            foreach (ZilModelObject obj in Context.ZEnvironment.ObjectsInDefinitionOrder())
            {
                lastObject = obj;
                Objects.Add(obj.Name, Game.DefineObject(obj.Name.Text));
                // builders for the rest of the properties and flags,
                // and vocabulary for names
                PreBuildObject(obj);
            }
        }

        private void PrepareHighestFlagBuilders()
        {
            // builders for flags that need to be numbered highest (explicitly listed or used in syntax)
            ZilAtom getOriginal(ZilAtom flag) =>
                Context.ZEnvironment.TryGetBitSynonym(flag, out var orig) ? orig : flag;
            var highestFlags =
                Context.ZEnvironment.FlagsOrderedLast
                    .Concat(
                        from syn in Context.ZEnvironment.Syntaxes
                        from flag in new[] { syn.FindFlag1, syn.FindFlag2 }
                        where flag != null
                        select getOriginal(flag))
                    .Distinct()
                    .ToList();

            if (highestFlags.Count >= this.Game.MaxFlags)
                Context.HandleError(new CompilerError(
                    CompilerMessages.Too_Many_0_1_Defined_Only_2_Allowed,
                    "flags requiring high numbers",
                    highestFlags.Count,
                    this.Game.MaxFlags));

            foreach (var flag in highestFlags)
                DefineFlag(flag);
        }

        private void PreparePropertyBuilders()
        {
            // builders and constants for some properties
            foreach (ZilAtom dir in Context.ZEnvironment.Directions)
                DefineProperty(dir);

            // create a constant for the last explicitly defined direction
            if (Context.ZEnvironment.LowDirection != null)
                Constants.Add(Context.GetStdAtom(StdAtom.LOW_DIRECTION),
                    Properties[Context.ZEnvironment.LowDirection]);

            // builders and constants for some more properties
            foreach (KeyValuePair<ZilAtom, ZilObject> pair in Context.ZEnvironment.PropertyDefaults)
                DefineProperty(pair.Key);
        }

        private void PrepareEarlyRoutineBuilders()
        {
            // builders for routines
            if (Context.ZEnvironment.EntryRoutineName == null)
                Context.ZEnvironment.EntryRoutineName = Context.GetStdAtom(StdAtom.GO);

            foreach (ZilRoutine routine in Context.ZEnvironment.Routines)
                Routines.Add(routine.Name, Game.DefineRoutine(
                    routine.Name.Text,
                    routine.Name == Context.ZEnvironment.EntryRoutineName,
                    (routine.Flags & RoutineFlags.CleanStack) != 0));
        }
    }
}
