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
    // TODO: split up this class/file
    partial class Compilation
    {
        public static void Compile(Context ctx, IGameBuilder gb)
        {
            var compilation = new Compilation(ctx, gb, gb.DebugFile != null && ctx.WantDebugInfo);
            compilation.Compile();
        }

        void Compile()
        {
            var ctx = Context;
            var gb = Game;

            /* the various structures need to be defined in the right order so
             * that symbols like P?FOO, V?FOO, etc. are always defined before
             * they could possibly be used. */

            // builders for routines
            if (ctx.ZEnvironment.EntryRoutineName == null)
                ctx.ZEnvironment.EntryRoutineName = ctx.GetStdAtom(StdAtom.GO);

            foreach (ZilRoutine routine in ctx.ZEnvironment.Routines)
                Routines.Add(routine.Name, gb.DefineRoutine(
                    routine.Name.ToString(),
                    routine.Name == ctx.ZEnvironment.EntryRoutineName,
                    (routine.Flags & RoutineFlags.CleanStack) != 0));

            // builders and constants for some properties
            foreach (ZilAtom dir in ctx.ZEnvironment.Directions)
                DefineProperty(dir);

            // create a constant for the last explicitly defined direction
            if (ctx.ZEnvironment.LowDirection != null)
                Constants.Add(ctx.GetStdAtom(StdAtom.LOW_DIRECTION),
                    Properties[ctx.ZEnvironment.LowDirection]);

            // builders and constants for some more properties
            foreach (KeyValuePair<ZilAtom, ZilObject> pair in ctx.ZEnvironment.PropertyDefaults)
                DefineProperty(pair.Key);

            // builders for flags that need to be numbered highest (explicitly listed or used in syntax)
            ZilAtom originalFlag;
            var highestFlags =
                ctx.ZEnvironment.FlagsOrderedLast
                    .Concat(
                        from syn in ctx.ZEnvironment.Syntaxes
                        from flag in new[] { syn.FindFlag1, syn.FindFlag2 }
                        where flag != null
                        select ctx.ZEnvironment.TryGetBitSynonym(flag, out originalFlag) ? originalFlag : flag)
                    .Distinct()
                    .ToList();

            if (highestFlags.Count >= Game.MaxFlags)
                ctx.HandleError(new CompilerError(
                    CompilerMessages.Too_Many_0_1_Defined_Only_2_Allowed,
                    "flags requiring high numbers",
                    highestFlags.Count,
                    Game.MaxFlags));

            foreach (var flag in highestFlags)
                DefineFlag(flag);

            // builders for objects
            ZilModelObject lastObject = null;

            foreach (ZilModelObject obj in ctx.ZEnvironment.ObjectsInDefinitionOrder())
            {
                lastObject = obj;
                Objects.Add(obj.Name, gb.DefineObject(obj.Name.ToString()));
                // builders for the rest of the properties and flags,
                // and vocabulary for names
                PreBuildObject(obj);
            }

            // builders for tables
            ITableBuilder firstPureTable = null;
            Func<ZilTable, int> parserTablesFirst = t => (t.Flags & TableFlags.ParserTable) != 0 ? 1 : 2;
            foreach (ZilTable table in ctx.ZEnvironment.Tables.OrderBy(parserTablesFirst))
            {
                bool pure = (table.Flags & TableFlags.Pure) != 0;
                var builder = gb.DefineTable(table.Name, pure);
                Tables.Add(table, builder);

                if (pure && firstPureTable == null)
                    firstPureTable = builder;
            }

            if (firstPureTable != null)
                Constants.Add(ctx.GetStdAtom(StdAtom.PRSTBL), firstPureTable);

            // self-inserting breaks
            var siBreaks = ctx.GetGlobalVal(ctx.GetStdAtom(StdAtom.SIBREAKS)) as ZilString;
            if (siBreaks != null)
            {
                gb.SelfInsertingBreaks.Clear();
                foreach (var c in siBreaks.Text)
                    gb.SelfInsertingBreaks.Add(c);
            }

            // builders for vocabulary
            // vocabulary for punctuation
            var punctWords = new Dictionary<string, string>
            {
                { "PERIOD", "." },
                { "COMMA", "," },
                { "QUOTE", "\"" },
                { "APOSTROPHE", "'" }
            };

            foreach (var symbol in punctWords.Values)
            {
                var symbolAtom = ZilAtom.Parse(symbol, ctx);
                ctx.ZEnvironment.GetVocab(symbolAtom);
            }

            foreach (var pair in ctx.ZEnvironment.Buzzwords)
            {
                ctx.ZEnvironment.GetVocabBuzzword(pair.Key, pair.Value);
            }

            var vocabMerges = new Dictionary<IWord, IWord>();
            ctx.ZEnvironment.MergeVocabulary((mainWord, duplicateWord) =>
            {
                Game.RemoveVocabularyWord(duplicateWord.Atom.Text);
                vocabMerges.Add(duplicateWord, mainWord);
            });

            foreach (IWord word in ctx.ZEnvironment.Vocabulary.Values)
            {
                DefineWord(word);
            }

            foreach (var pair in punctWords)
            {
                var nameAtom = ZilAtom.Parse(pair.Key, ctx);
                var symbolAtom = ZilAtom.Parse(pair.Value, ctx);

                IWord symbolWord;

                if (ctx.ZEnvironment.Vocabulary.TryGetValue(symbolAtom, out symbolWord) && 
                    !ctx.ZEnvironment.Vocabulary.ContainsKey(nameAtom))
                {
                    var nameWord = ctx.ZEnvironment.VocabFormat.CreateWord(nameAtom);
                    ctx.ZEnvironment.VocabFormat.MakeSynonym(nameWord, symbolWord);
                    vocabMerges.Add(nameWord, symbolWord);
                }
            }

            string[] wordConstantPrefixes = { "W?", "A?", "ACT?", "PR?" };

            foreach (var pair in vocabMerges)
            {
                IWord dupWord = pair.Key, mainWord = pair.Value;
                Vocabulary[dupWord] = Vocabulary[mainWord];

                foreach (var prefix in wordConstantPrefixes)
                {
                    var mainAtom = ZilAtom.Parse(prefix + mainWord.Atom.Text, ctx);

                    IOperand value;
                    if (Constants.TryGetValue(mainAtom, out value))
                    {
                        var dupAtom = ZilAtom.Parse(prefix + dupWord.Atom.Text, ctx);
                        Constants[dupAtom] = value;
                    }
                }
            }

            // constants and builders for late syntax tables
            foreach (var name in ctx.ZEnvironment.VocabFormat.GetLateSyntaxTableNames())
            {
                var tb = Game.DefineTable(name, true);
                var atom = ctx.RootObList[name];
                Constants.Add(atom, tb);

                // this hack lets macros use it as a compile-time value, as long as they don't access its contents
                ctx.SetGlobalVal(atom, atom);
            }

            // early syntax tables
            var syntaxTables = BuildSyntaxTables();
            foreach (var pair in syntaxTables)
                Constants.Add(ctx.RootObList[pair.Key], pair.Value);

            // now that all the vocabulary is set up, copy values for synonyms
            foreach (Synonym syn in ctx.ZEnvironment.Synonyms)
                syn.Apply(ctx);

            // may as well do bit synonyms here too
            foreach (var pair in ctx.ZEnvironment.BitSynonyms)
                DefineFlagAlias(pair.Key, pair.Value);

            // enforce limit on number of flags
            if (UniqueFlags > Game.MaxFlags)
                Context.HandleError(new CompilerError(
                    CompilerMessages.Too_Many_0_1_Defined_Only_2_Allowed,
                    "flags",
                    UniqueFlags,
                    Game.MaxFlags));

            // FUNNY-GLOBALS?
            var reservedGlobals = ctx.ZEnvironment.VocabFormat.GetReservedGlobalNames();
            if (ctx.GetGlobalOption(StdAtom.DO_FUNNY_GLOBALS_P))
            {
                // this sets StorageType for all variables, and creates the table and global if needed
                DoFunnyGlobals(reservedGlobals.Length);
            }
            else
            {
                foreach (var g in ctx.ZEnvironment.Globals)
                    g.StorageType = GlobalStorageType.Hard;

                if (ctx.ZEnvironment.Globals.Count > 240 - reservedGlobals.Length)
                    Context.HandleError(new CompilerError(
                        CompilerMessages.Too_Many_0_1_Defined_Only_2_Allowed,
                        "globals",
                        ctx.ZEnvironment.Globals.Count,
                        240 - reservedGlobals.Length));
            }

            // builders and values for constants (which may refer to vocabulary,
            // routines, tables, objects, properties, or flags)
            foreach (ZilConstant constant in ctx.ZEnvironment.Constants)
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
                    ctx.HandleError(new CompilerError(
                        constant,
                        CompilerMessages.Nonconstant_Initializer_For_0_1_2,
                        "constant",
                        constant.Name,
                        constant.Value.ToStringContext(ctx, false)));
                    value = gb.Zero;
                }

                Constants.Add(constant.Name, gb.DefineConstant(constant.Name.ToString(), value));
            }

            ITableBuilder longWordTable = null;
            if (ctx.GetCompilationFlagOption(StdAtom.LONG_WORDS))
            {
                longWordTable = Game.DefineTable("LONG-WORD-TABLE", true);
                Constants.Add(Context.GetStdAtom(StdAtom.LONG_WORD_TABLE), longWordTable);
            }

            Constants.Add(Context.GetStdAtom(StdAtom.VOCAB), Game.VocabularyTable);

            // builders and values for globals (which may refer to constants)
            IGlobalBuilder glb;
            foreach (ZilGlobal global in ctx.ZEnvironment.Globals)
            {
                if (global.StorageType == GlobalStorageType.Hard)
                {
                    glb = gb.DefineGlobal(global.Name.ToString());
                    glb.DefaultValue = GetGlobalDefaultValue(global);
                    Globals.Add(global.Name, glb);
                }
            }

            // implicitly defined globals
            // NOTE: the parameter to DoFunnyGlobals() above must match the number of globals implicitly defined here
            foreach (var name in reservedGlobals)
            {
                glb = Game.DefineGlobal(name);
                Globals.Add(ctx.RootObList[name], glb);
            }

            // default values for properties
            foreach (KeyValuePair<ZilAtom, ZilObject> pair in ctx.ZEnvironment.PropertyDefaults)
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
                                pair.Value.ToStringContext(ctx, false));
                    }
                }
                catch (ZilError ex)
                {
                    ctx.HandleError(ex);
                }
            }

            // builders for routines (again, in case any were added during compilation, e.g. by a PROPSPEC)
            foreach (ZilRoutine routine in ctx.ZEnvironment.Routines)
            {
                if (!Routines.ContainsKey(routine.Name))
                    Routines.Add(routine.Name, gb.DefineRoutine(
                        routine.Name.ToString(),
                        routine.Name == ctx.ZEnvironment.EntryRoutineName,
                        (routine.Flags & RoutineFlags.CleanStack) != 0));
            }

            // let macros know we're generating code now
            ctx.DefineCompilationFlag(ctx.GetStdAtom(StdAtom.IN_ZILCH), ctx.TRUE, true);

            // compile routines
            IRoutineBuilder mainRoutine = null;

            foreach (ZilRoutine routine in ctx.ZEnvironment.Routines)
            {
                bool entryPoint = routine.Name == ctx.ZEnvironment.EntryRoutineName;
                IRoutineBuilder rb = Routines[routine.Name];
                try
                {
                    using (DiagnosticContext.Push(routine.SourceLine))
                    {
                        BuildRoutine(routine, gb, rb, entryPoint);
                    }
                }
                catch (ZilError ex)
                {
                    // could be a compiler error, or an interpreter error thrown by macro evaluation
                    ctx.HandleError(ex);
                }
                rb.Finish();

                if (entryPoint)
                    mainRoutine = rb;
            }

            if (mainRoutine == null)
                throw new CompilerError(CompilerMessages.Missing_GO_Routine);

            // ...and we're done generating code
            ctx.DefineCompilationFlag(ctx.GetStdAtom(StdAtom.IN_ZILCH), ctx.FALSE, true);

            // build objects
            foreach (ZilModelObject obj in ctx.ZEnvironment.ObjectsInInsertionOrder())
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
                    ctx.HandleError(ex);
                }
            }

            // build vocabulary
            Queue<IWord> longWords = (longWordTable == null ? null : new Queue<IWord>());

            var helpers = new WriteToBuilderHelpers
            {
                CompileConstant = CompileConstant,
                DirIndexToPropertyOperand = di => Properties[ctx.ZEnvironment.Directions[di]]
            };

            var builtWords = new HashSet<IWordBuilder>();
            foreach (var pair in Vocabulary)
            {
                IWord word = pair.Key;
                IWordBuilder wb = pair.Value;

                if (builtWords.Contains(wb))
                    continue;

                builtWords.Add(wb);

                Context.ZEnvironment.VocabFormat.WriteToBuilder(word, wb, helpers);

                if (longWords != null && ctx.ZEnvironment.IsLongWord(word))
                {
                    longWords.Enqueue(word);
                }
            }

            if (longWords != null)
            {
                longWordTable.AddShort((short)longWords.Count);
                while (longWords.Count > 0)
                {
                    var word = longWords.Dequeue();
                    longWordTable.AddShort(Vocabulary[word]);
                    longWordTable.AddShort(Game.MakeOperand(word.Atom.Text.ToLowerInvariant()));
                }
            }

            BuildLateSyntaxTables();

            // build tables
            foreach (KeyValuePair<ZilTable, ITableBuilder> pair in Tables)
                BuildTable(pair.Key, pair.Value);

            BuildHeaderExtensionTable();

            gb.Finish();
        }
    }
}
