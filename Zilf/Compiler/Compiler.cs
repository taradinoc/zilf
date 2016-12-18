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
using System.Diagnostics.CodeAnalysis;
using System.Diagnostics.Contracts;
using System.Linq;
using System.Text;
using Zilf.Compiler.Builtins;
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
    static class Compiler
    {
        public static void Compile(Context ctx, IGameBuilder gb)
        {
            var cc = new CompileCtx(ctx, gb, gb.DebugFile != null && ctx.WantDebugInfo);

            /* the various structures need to be defined in the right order so
             * that symbols like P?FOO, V?FOO, etc. are always defined before
             * they could possibly be used. */

            // builders for routines
            if (ctx.ZEnvironment.EntryRoutineName == null)
                ctx.ZEnvironment.EntryRoutineName = ctx.GetStdAtom(StdAtom.GO);

            foreach (ZilRoutine routine in ctx.ZEnvironment.Routines)
                cc.Routines.Add(routine.Name, gb.DefineRoutine(
                    routine.Name.ToString(),
                    routine.Name == ctx.ZEnvironment.EntryRoutineName,
                    (routine.Flags & RoutineFlags.CleanStack) != 0));

            // builders and constants for some properties
            foreach (ZilAtom dir in ctx.ZEnvironment.Directions)
                DefineProperty(cc, dir);

            // create a constant for the last explicitly defined direction
            if (ctx.ZEnvironment.LowDirection != null)
                cc.Constants.Add(ctx.GetStdAtom(StdAtom.LOW_DIRECTION),
                    cc.Properties[ctx.ZEnvironment.LowDirection]);

            // builders and constants for some more properties
            foreach (KeyValuePair<ZilAtom, ZilObject> pair in ctx.ZEnvironment.PropertyDefaults)
                DefineProperty(cc, pair.Key);

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

            if (highestFlags.Count >= cc.Game.MaxFlags)
                ctx.HandleError(new CompilerError(
                    CompilerMessages.Too_Many_0_1_Defined_Only_2_Allowed,
                    "flags requiring high numbers",
                    highestFlags.Count,
                    cc.Game.MaxFlags));

            foreach (var flag in highestFlags)
                DefineFlag(cc, flag);

            // builders for objects
            ZilModelObject lastObject = null;

            foreach (ZilModelObject obj in ctx.ZEnvironment.ObjectsInDefinitionOrder())
            {
                lastObject = obj;
                cc.Objects.Add(obj.Name, gb.DefineObject(obj.Name.ToString()));
                // builders for the rest of the properties and flags,
                // and vocabulary for names
                PreBuildObject(cc, obj);
            }

            // builders for tables
            ITableBuilder firstPureTable = null;
            Func<ZilTable, int> parserTablesFirst = t => (t.Flags & TableFlags.ParserTable) != 0 ? 1 : 2;
            foreach (ZilTable table in ctx.ZEnvironment.Tables.OrderBy(parserTablesFirst))
            {
                bool pure = (table.Flags & TableFlags.Pure) != 0;
                var builder = gb.DefineTable(table.Name, pure);
                cc.Tables.Add(table, builder);

                if (pure && firstPureTable == null)
                    firstPureTable = builder;
            }

            if (firstPureTable != null)
                cc.Constants.Add(ctx.GetStdAtom(StdAtom.PRSTBL), firstPureTable);

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
                cc.Game.RemoveVocabularyWord(duplicateWord.Atom.Text);
                vocabMerges.Add(duplicateWord, mainWord);
            });

            foreach (IWord word in ctx.ZEnvironment.Vocabulary.Values)
            {
                DefineWord(cc, word);
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
                cc.Vocabulary[dupWord] = cc.Vocabulary[mainWord];

                foreach (var prefix in wordConstantPrefixes)
                {
                    var mainAtom = ZilAtom.Parse(prefix + mainWord.Atom.Text, ctx);

                    IOperand value;
                    if (cc.Constants.TryGetValue(mainAtom, out value))
                    {
                        var dupAtom = ZilAtom.Parse(prefix + dupWord.Atom.Text, ctx);
                        cc.Constants[dupAtom] = value;
                    }
                }
            }

            // constants and builders for late syntax tables
            foreach (var name in ctx.ZEnvironment.VocabFormat.GetLateSyntaxTableNames())
            {
                var tb = cc.Game.DefineTable(name, true);
                var atom = ctx.RootObList[name];
                cc.Constants.Add(atom, tb);

                // this hack lets macros use it as a compile-time value, as long as they don't access its contents
                ctx.SetGlobalVal(atom, atom);
            }

            // early syntax tables
            var syntaxTables = BuildSyntaxTables(cc);
            foreach (var pair in syntaxTables)
                cc.Constants.Add(ctx.RootObList[pair.Key], pair.Value);

            // now that all the vocabulary is set up, copy values for synonyms
            foreach (Synonym syn in ctx.ZEnvironment.Synonyms)
                syn.Apply(ctx);

            // may as well do bit synonyms here too
            foreach (var pair in ctx.ZEnvironment.BitSynonyms)
                DefineFlagAlias(cc, pair.Key, pair.Value);

            // enforce limit on number of flags
            if (cc.UniqueFlags > cc.Game.MaxFlags)
                cc.Context.HandleError(new CompilerError(
                    CompilerMessages.Too_Many_0_1_Defined_Only_2_Allowed,
                    "flags",
                    cc.UniqueFlags,
                    cc.Game.MaxFlags));

            // FUNNY-GLOBALS?
            var reservedGlobals = ctx.ZEnvironment.VocabFormat.GetReservedGlobalNames();
            if (ctx.GetGlobalOption(StdAtom.DO_FUNNY_GLOBALS_P))
            {
                // this sets StorageType for all variables, and creates the table and global if needed
                DoFunnyGlobals(cc, reservedGlobals.Length);
            }
            else
            {
                foreach (var g in ctx.ZEnvironment.Globals)
                    g.StorageType = GlobalStorageType.Hard;

                if (ctx.ZEnvironment.Globals.Count > 240 - reservedGlobals.Length)
                    cc.Context.HandleError(new CompilerError(
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
                    value = cc.Objects[lastObject.Name];
                }
                else
                {
                    value = CompileConstant(cc, constant.Value);
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

                cc.Constants.Add(constant.Name, gb.DefineConstant(constant.Name.ToString(), value));
            }

            ITableBuilder longWordTable = null;
            if (ctx.GetCompilationFlagOption(StdAtom.LONG_WORDS))
            {
                longWordTable = cc.Game.DefineTable("LONG-WORD-TABLE", true);
                cc.Constants.Add(cc.Context.GetStdAtom(StdAtom.LONG_WORD_TABLE), longWordTable);
            }

            cc.Constants.Add(cc.Context.GetStdAtom(StdAtom.VOCAB), cc.Game.VocabularyTable);

            // builders and values for globals (which may refer to constants)
            IGlobalBuilder glb;
            foreach (ZilGlobal global in ctx.ZEnvironment.Globals)
            {
                if (global.StorageType == GlobalStorageType.Hard)
                {
                    glb = gb.DefineGlobal(global.Name.ToString());
                    glb.DefaultValue = GetGlobalDefaultValue(cc, global);
                    cc.Globals.Add(global.Name, glb);
                }
            }

            // implicitly defined globals
            // NOTE: the parameter to DoFunnyGlobals() above must match the number of globals implicitly defined here
            foreach (var name in reservedGlobals)
            {
                glb = cc.Game.DefineGlobal(name);
                cc.Globals.Add(ctx.RootObList[name], glb);
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
                        IPropertyBuilder pb = cc.Properties[pair.Key];
                        pb.DefaultValue = CompileConstant(cc, pair.Value);
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
                if (!cc.Routines.ContainsKey(routine.Name))
                    cc.Routines.Add(routine.Name, gb.DefineRoutine(
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
                IRoutineBuilder rb = cc.Routines[routine.Name];
                try
                {
                    using (DiagnosticContext.Push(routine.SourceLine))
                    {
                        BuildRoutine(cc, routine, gb, rb, entryPoint);
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
                IObjectBuilder ob = cc.Objects[obj.Name];
                try
                {
                    using (DiagnosticContext.Push(obj.SourceLine))
                    {
                        BuildObject(cc, obj, ob);
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
                CompileConstant = zo => CompileConstant(cc, zo),
                DirIndexToPropertyOperand = di => cc.Properties[ctx.ZEnvironment.Directions[di]]
            };

            var builtWords = new HashSet<IWordBuilder>();
            foreach (var pair in cc.Vocabulary)
            {
                IWord word = pair.Key;
                IWordBuilder wb = pair.Value;

                if (builtWords.Contains(wb))
                    continue;

                builtWords.Add(wb);

                cc.Context.ZEnvironment.VocabFormat.WriteToBuilder(word, wb, helpers);

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
                    longWordTable.AddShort(cc.Vocabulary[word]);
                    longWordTable.AddShort(cc.Game.MakeOperand(word.Atom.Text.ToLower()));
                }
            }

            BuildLateSyntaxTables(cc);

            // build tables
            foreach (KeyValuePair<ZilTable, ITableBuilder> pair in cc.Tables)
                BuildTable(cc, pair.Key, pair.Value);

            BuildHeaderExtensionTable(cc);

            gb.Finish();
        }

        static IOperand GetGlobalDefaultValue(CompileCtx cc, ZilGlobal global)
        {
            Contract.Requires(cc != null);
            Contract.Requires(global != null);

            IOperand result = null;

            if (global.Value != null)
            {
                try
                {
                    using (DiagnosticContext.Push(global.SourceLine))
                    {
                        result = CompileConstant(cc, global.Value);
                        if (result == null)
                            cc.Context.HandleError(new CompilerError(
                                global,
                                CompilerMessages.Nonconstant_Initializer_For_0_1_2,
                                "global",
                                global.Name,
                                global.Value.ToStringContext(cc.Context, false)));
                    }
                }
                catch (ZilError ex)
                {
                    cc.Context.HandleError(ex);
                }
            }

            return result;
        }

        static void DoFunnyGlobals(CompileCtx cc, int reservedGlobals)
        {
            Contract.Requires(cc != null);
            Contract.Requires(reservedGlobals >= 0);
            Contract.Ensures(Contract.ForAll(cc.Context.ZEnvironment.Globals, g => g.StorageType != GlobalStorageType.Any));

            // if all the globals fit into Z-machine globals, no need for a table
            int remaining = 240 - reservedGlobals;

            if (cc.Context.ZEnvironment.Globals.Count <= remaining)
            {
                foreach (var g in cc.Context.ZEnvironment.Globals)
                    g.StorageType = GlobalStorageType.Hard;

                return;
            }

            // reserve one slot for GLOBAL-VARS-TABLE
            remaining--;

            // in V3, the status line variables need to be Z-machine globals
            if (cc.Context.ZEnvironment.ZVersion < 4)
            {
                foreach (var g in cc.Context.ZEnvironment.Globals)
                {
                    switch (g.Name.StdAtom)
                    {
                        case StdAtom.HERE:
                        case StdAtom.SCORE:
                        case StdAtom.MOVES:
                            g.StorageType = GlobalStorageType.Hard;
                            break;
                    }
                }
            }

            // variables used as operands need to be Z-machine globals too
            var globalsByName = cc.Context.ZEnvironment.Globals.ToDictionary(g => g.Name);
            foreach (var r in cc.Context.ZEnvironment.Routines)
            {
                WalkRoutineForms(r, f =>
                {
                    var args = f.Rest;
                    if (args != null && !args.IsEmpty)
                    {
                        // skip the first argument to operations that operate on a variable
                        var firstAtom = f.First as ZilAtom;
                        if (firstAtom != null)
                        {
                            switch (firstAtom.StdAtom)
                            {
                                case StdAtom.SET:
                                case StdAtom.SETG:
                                case StdAtom.VALUE:
                                case StdAtom.GVAL:
                                case StdAtom.LVAL:
                                case StdAtom.INC:
                                case StdAtom.DEC:
                                case StdAtom.IGRTR_P:
                                case StdAtom.DLESS_P:
                                    args = args.Rest;
                                    break;
                            }
                        }

                        while (args != null && !args.IsEmpty)
                        {
                            var atom = args.First as ZilAtom;
                            ZilGlobal g;
                            if (atom != null && globalsByName.TryGetValue(atom, out g))
                            {
                                g.StorageType = GlobalStorageType.Hard;
                            }

                            args = args.Rest;
                        }
                    }
                });
            }

            // determine which others to keep in Z-machine globals
            var lookup = cc.Context.ZEnvironment.Globals.ToLookup(g => g.StorageType);

            var hardGlobals = new List<ZilGlobal>(remaining);
            if (lookup.Contains(GlobalStorageType.Hard))
            {
                hardGlobals.AddRange(lookup[GlobalStorageType.Hard]);

                if (hardGlobals.Count > remaining)
                    throw new CompilerError(
                        CompilerMessages.Too_Many_0_1_Defined_Only_2_Allowed,
                        "hard globals",
                        hardGlobals.Count,
                        remaining);
            }

            var softGlobals = new Queue<ZilGlobal>(cc.Context.ZEnvironment.Globals.Count - hardGlobals.Count);

            if (lookup.Contains(GlobalStorageType.Any))
                foreach (var g in lookup[GlobalStorageType.Any])
                    softGlobals.Enqueue(g);

            if (lookup.Contains(GlobalStorageType.Soft))
                foreach (var g in lookup[GlobalStorageType.Soft])
                    softGlobals.Enqueue(g);

            while (hardGlobals.Count < remaining && softGlobals.Count > 0)
                hardGlobals.Add(softGlobals.Dequeue());

            // assign final StorageTypes
            foreach (var g in hardGlobals)
                g.StorageType = GlobalStorageType.Hard;

            foreach (var g in softGlobals)
                g.StorageType = GlobalStorageType.Soft;

            // create SoftGlobals entries, fill table, and assign offsets
            int byteOffset = 0;
            var table = cc.Game.DefineTable("T?GLOBAL-VARS-TABLE", false);

            var tableGlobal = cc.Game.DefineGlobal("GLOBAL-VARS-TABLE");
            tableGlobal.DefaultValue = table;
            cc.Globals.Add(cc.Context.GetStdAtom(StdAtom.GLOBAL_VARS_TABLE), tableGlobal);
            cc.SoftGlobalsTable = tableGlobal;

            foreach (var g in softGlobals)
            {
                if (!g.IsWord)
                {
                    var entry = new SoftGlobal
                    {
                        IsWord = false,
                        Offset = byteOffset
                    };
                    cc.SoftGlobals.Add(g.Name, entry);

                    table.AddByte(GetGlobalDefaultValue(cc, g) ?? cc.Game.Zero);

                    byteOffset++;
                }
            }

            if (byteOffset % 2 != 0)
            {
                byteOffset++;
                table.AddByte(cc.Game.Zero);
            }

            foreach (var g in softGlobals)
            {
                if (g.IsWord)
                {
                    var entry = new SoftGlobal
                    {
                        IsWord = true,
                        Offset = byteOffset / 2
                    };
                    cc.SoftGlobals.Add(g.Name, entry);

                    table.AddShort(GetGlobalDefaultValue(cc, g) ?? cc.Game.Zero);
                    byteOffset += 2;
                }
            }
        }

        static void BuildHeaderExtensionTable(CompileCtx cc)
        {
            Contract.Requires(cc != null);

            var size = cc.Context.ZEnvironment.HeaderExtensionWords;
            if (size > 0)
            {
                var v5options = cc.Game.Options as Zilf.Emit.Zap.GameOptions.V5Plus;
                if (v5options != null)
                {
                    var extab = cc.Game.DefineTable("EXTAB", false);
                    extab.AddShort((short)size);
                    for (int i = 0; i < size; i++)
                        extab.AddShort(cc.Game.Zero);

                    v5options.HeaderExtensionTable = extab;
                }
                else
                {
                    throw new CompilerError(CompilerMessages.Header_Extensions_Not_Supported_For_This_Target);
                }
            }
        }

        static void WalkRoutineForms(ZilRoutine routine, Action<ZilForm> action)
        {
            Contract.Requires(routine != null);
            Contract.Requires(action != null);

            var children =
                routine.ArgSpec.Select(ai => ai.DefaultValue)
                .Concat(routine.Body);

            foreach (var form in children.OfType<ZilForm>())
            {
                action(form);
                WalkChildren(form, action);
            }
        }

        static void WalkChildren(ZilObject obj, Action<ZilForm> action)
        {
            var enumerable = obj as IEnumerable<ZilObject>;

            if (enumerable != null)
            {
                foreach (var child in enumerable)
                {
                    if (child is ZilForm)
                        action((ZilForm)child);

                    WalkChildren(child, action);
                }
            }
        }

        static IDictionary<string, ITableBuilder> BuildSyntaxTables(CompileCtx cc)
        {
            var dict = new Dictionary<string, ITableBuilder>();

            // TODO: encapsulate this in the VocabFormat classes
            if (cc.Context.GetGlobalOption(StdAtom.NEW_PARSER_P))
                BuildNewFormatSyntaxTables(cc, dict);
            else
                BuildOldFormatSyntaxTables(cc, dict);

            return dict;
        }

        static void BuildOldFormatSyntaxTables(CompileCtx cc, IDictionary<string, ITableBuilder> tables)
        {
            Contract.Requires(cc != null);

            // TODO: emit VTBL as the first impure table, followed by syntax lines, which is what ztools expects?
            var verbTable = cc.Game.DefineTable("VTBL", true);
            var actionTable = cc.Game.DefineTable("ATBL", true);
            var preactionTable = cc.Game.DefineTable("PATBL", true);

            tables.Add("VTBL", verbTable);
            tables.Add("ATBL", actionTable);
            tables.Add("PATBL", preactionTable);

            // compact syntaxes?
            var compact = cc.Context.GetGlobalOption(StdAtom.COMPACT_SYNTAXES_P);

            var vf = cc.Context.ZEnvironment.VocabFormat;

            // verb table
            var query = from s in cc.Context.ZEnvironment.Syntaxes
                        group s by s.Verb into g
                        orderby vf.GetVerbValue(g.Key) descending
                        select g;

            var actions = new Dictionary<ZilAtom, Action>();

            foreach (var verb in query)
            {
                int num = vf.GetVerbValue(verb.Key);

                // syntax table
                var stbl = cc.Game.DefineTable("ST?" + verb.Key.Atom, true);
                verbTable.AddShort(stbl);

                stbl.AddByte((byte)verb.Count());

                // make two passes over the syntax line definitions:
                // first in definition order to create/validate the Actions, second in reverse order to emit the syntax lines
                foreach (Syntax line in verb)
                {
                    ValidateAction(cc, actions, line);
                }

                foreach (Syntax line in verb.Reverse())
                {
                    Action act;
                    if (actions.TryGetValue(line.ActionName, out act) == false)
                    {
                        // this can happen if an exception (e.g. undefined action routine) stops us from adding the action during the first pass.
                        continue;
                    }

                    try
                    {
                        using (DiagnosticContext.Push(line.SourceLine))
                        {
                            if (compact)
                            {
                                if (line.Preposition1 != null)
                                {
                                    var pn = vf.GetPrepositionValue(line.Preposition1);
                                    stbl.AddByte((byte)((pn & 63) | (line.NumObjects << 6)));
                                }
                                else
                                {
                                    stbl.AddByte((byte)(line.NumObjects << 6));
                                }
                                stbl.AddByte(act.Constant);

                                if (line.NumObjects > 0)
                                {
                                    stbl.AddByte(GetFlag(cc, line.FindFlag1) ?? cc.Game.Zero);
                                    stbl.AddByte((byte)line.Options1);

                                    if (line.NumObjects > 1)
                                    {
                                        if (line.Preposition2 != null)
                                        {
                                            var pn = vf.GetPrepositionValue(line.Preposition2);
                                            stbl.AddByte((byte)(pn & 63));
                                        }
                                        else
                                        {
                                            stbl.AddByte(0);
                                        }

                                        stbl.AddByte(GetFlag(cc, line.FindFlag2) ?? cc.Game.Zero);
                                        stbl.AddByte((byte)line.Options2);
                                    }
                                }
                            }
                            else
                            {
                                stbl.AddByte((byte)line.NumObjects);
                                stbl.AddByte(GetPreposition(cc, line.Preposition1) ?? cc.Game.Zero);
                                stbl.AddByte(GetPreposition(cc, line.Preposition2) ?? cc.Game.Zero);
                                stbl.AddByte(GetFlag(cc, line.FindFlag1) ?? cc.Game.Zero);
                                stbl.AddByte(GetFlag(cc, line.FindFlag2) ?? cc.Game.Zero);
                                stbl.AddByte((byte)line.Options1);
                                stbl.AddByte((byte)line.Options2);
                                stbl.AddByte(act.Constant);
                            }
                        }
                    }
                    catch (ZilError ex)
                    {
                        cc.Context.HandleError(ex);
                    }
                }
            }

            // action and preaction table
            var actquery = from a in actions
                           orderby a.Value.Index
                           select a.Value;
            foreach (Action act in actquery)
            {
                actionTable.AddShort(act.Routine);
                preactionTable.AddShort(act.PreRoutine ?? cc.Game.Zero);
            }
        }

        static void BuildNewFormatSyntaxTables(CompileCtx cc, IDictionary<string, ITableBuilder> tables)
        {
            Contract.Requires(cc != null);

            var actionTable = cc.Game.DefineTable("ATBL", true);
            var preactionTable = cc.Game.DefineTable("PATBL", true);

            tables.Add("ATBL", actionTable);
            tables.Add("PATBL", preactionTable);

            var vf = cc.Context.ZEnvironment.VocabFormat;

            var query = from s in cc.Context.ZEnvironment.Syntaxes
                        group s by s.Verb into verbGrouping
                        let numObjLookup = verbGrouping.ToLookup(s => s.NumObjects)
                        select new
                        {
                            Word = verbGrouping.Key,
                            Nullary = numObjLookup[0].FirstOrDefault(),
                            Unary = numObjLookup[1].ToArray(),
                            Binary = numObjLookup[2].ToArray(),
                        };

            // syntax lines are emitted in definition order, so we can validate actions and emit syntax lines in one pass
            var actions = new Dictionary<ZilAtom, Action>();

            foreach (var verb in query)
            {
                // syntax table
                var name = "ACT?" + verb.Word.Atom;
                var acttbl = cc.Game.DefineTable(name, true);
                tables.Add(name, acttbl);

                // 0-object syntaxes
                if (verb.Nullary != null)
                {
                    var act = ValidateAction(cc, actions, verb.Nullary);
                    acttbl.AddShort(act != null ? act.Constant : cc.Game.Zero);
                }
                else
                {
                    acttbl.AddShort(-1);
                }

                // reserved word
                acttbl.AddShort(0);

                // 1-object syntaxes
                if (verb.Unary.Length > 0)
                {
                    var utbl = cc.Game.DefineTable(null, true);
                    utbl.AddShort((short)verb.Unary.Length);

                    foreach (var line in verb.Unary)
                    {
                        var act = ValidateAction(cc, actions, line);
                        utbl.AddShort(act.Constant);

                        utbl.AddShort(line.Preposition1 == null ? cc.Game.Zero : cc.Vocabulary[line.Preposition1]);
                        utbl.AddByte(GetFlag(cc, line.FindFlag1) ?? cc.Game.Zero);
                        utbl.AddByte((byte)line.Options1);
                    }

                    acttbl.AddShort(utbl);
                }
                else
                {
                    acttbl.AddShort(0);
                }

                // 2-object syntaxes
                if (verb.Binary.Length > 0)
                {
                    var btbl = cc.Game.DefineTable(null, true);
                    btbl.AddShort((short)verb.Binary.Length);

                    foreach (var line in verb.Binary)
                    {
                        var act = ValidateAction(cc, actions, line);
                        btbl.AddShort(act.Constant);

                        btbl.AddShort(line.Preposition1 == null ? cc.Game.Zero : cc.Vocabulary[line.Preposition1]);
                        btbl.AddByte(GetFlag(cc, line.FindFlag1) ?? cc.Game.Zero);
                        btbl.AddByte((byte)line.Options1);

                        btbl.AddShort(line.Preposition2 == null ? cc.Game.Zero : cc.Vocabulary[line.Preposition2]);
                        btbl.AddByte(GetFlag(cc, line.FindFlag2) ?? cc.Game.Zero);
                        btbl.AddByte((byte)line.Options2);
                    }

                    acttbl.AddShort(btbl);
                }
                else
                {
                    acttbl.AddShort(0);
                }
            }

            // action and preaction table
            var actquery = from a in actions
                           orderby a.Value.Index
                           select a.Value;
            foreach (Action act in actquery)
            {
                actionTable.AddShort(act.Routine);
                preactionTable.AddShort(act.PreRoutine ?? cc.Game.Zero);
            }
        }

        static void BuildLateSyntaxTables(CompileCtx cc)
        {
            Contract.Requires(cc != null);

            var helpers = new BuildLateSyntaxTablesHelpers
            {
                CompileConstant = zo => CompileConstant(cc, zo),
                GetGlobal = atom => cc.Globals[atom],
                Vocabulary = cc.Vocabulary
            };

            cc.Context.ZEnvironment.VocabFormat.BuildLateSyntaxTables(helpers);
        }

        static Action ValidateAction(CompileCtx cc, Dictionary<ZilAtom, Action> actions, Syntax line)
        {
            try
            {
                using (DiagnosticContext.Push(line.SourceLine))
                {
                    Action act;
                    if (actions.TryGetValue(line.ActionName, out act) == false)
                    {
                        IRoutineBuilder routine;
                        if (cc.Routines.TryGetValue(line.Action, out routine) == false)
                            throw new CompilerError(CompilerMessages.Undefined_0_1, "action routine", line.Action);

                        IRoutineBuilder preRoutine = null;
                        if (line.Preaction != null &&
                            cc.Routines.TryGetValue(line.Preaction, out preRoutine) == false)
                            throw new CompilerError(CompilerMessages.Undefined_0_1, "preaction routine", line.Preaction);

                        ZilAtom actionName = line.ActionName;
                        int index = cc.Context.ZEnvironment.NextAction++;
                        var number = cc.Game.MakeOperand(index);
                        var constant = cc.Game.DefineConstant(actionName.ToString(), number);
                        cc.Constants.Add(actionName, constant);
                        if (cc.WantDebugInfo)
                            cc.Game.DebugFile.MarkAction(constant, line.Action.ToString());

                        act = new Action(index, constant, routine, preRoutine, line.Action, line.Preaction);
                        actions.Add(actionName, act);
                    }
                    else
                    {
                        WarnIfActionRoutineDiffers(cc, line, "action routine", line.Action, act.RoutineName);
                        WarnIfActionRoutineDiffers(cc, line, "preaction routine", line.Preaction, act.PreRoutineName);
                    }

                    return act;
                }
            }
            catch (ZilError ex)
            {
                cc.Context.HandleError(ex);
                return null;
            }
        }

        static void WarnIfActionRoutineDiffers(CompileCtx cc, Syntax line,
            string description, ZilAtom thisRoutineName, ZilAtom lastRoutineName)
        {
            Contract.Requires(cc != null);
            Contract.Requires(line != null);
            Contract.Requires(description != null);

            if (thisRoutineName != lastRoutineName)
                cc.Context.HandleWarning(new CompilerError(line.SourceLine,
                    CompilerMessages._0_Mismatch_For_1_Using_2_As_Before,
                    description,
                    line.ActionName,
                    lastRoutineName != null ? lastRoutineName.ToString() : "no " + description));
        }

        static IFlagBuilder GetFlag(CompileCtx cc, ZilAtom flag)
        {
            Contract.Requires(cc != null);

            if (flag == null)
                return null;

            ZilAtom originalFlag;
            if (cc.Context.ZEnvironment.TryGetBitSynonym(flag, out originalFlag))
            {
                flag = originalFlag;
            }

            DefineFlag(cc, flag);
            return cc.Flags[flag];
        }

        static IOperand GetPreposition(CompileCtx cc, IWord word)
        {
            Contract.Requires(cc != null);
            Contract.Ensures(Contract.Result<IOperand>() != null || word == null);

            if (word == null)
                return null;

            string name = "PR?" + word.Atom.Text;
            var atom = ZilAtom.Parse(name, cc.Context);
            return cc.Constants[atom];
        }

        static void DefineProperty(CompileCtx cc, ZilAtom prop)
        {
            Contract.Requires(cc != null);
            Contract.Requires(prop != null);

            if (!cc.Properties.ContainsKey(prop))
            {
                // create property builder
                var pb = cc.Game.DefineProperty(prop.ToString());
                cc.Properties.Add(prop, pb);

                // create constant
                string propConstName = "P?" + prop;
                var propAtom = ZilAtom.Parse(propConstName, cc.Context);
                cc.Constants.Add(propAtom, pb);
            }
        }

        static void DefineFlag(CompileCtx cc, ZilAtom flag)
        {
            Contract.Requires(cc != null);
            Contract.Requires(flag != null);

            if (!cc.Flags.ContainsKey(flag))
            {
                // create flag builder
                var fb = cc.Game.DefineFlag(flag.ToString());
                cc.Flags.Add(flag, fb);
                cc.UniqueFlags++;

                // create constant
                cc.Constants.Add(flag, fb);
            }
        }

        static void DefineFlagAlias(CompileCtx cc, ZilAtom alias, ZilAtom original)
        {
            Contract.Requires(cc != null);
            Contract.Requires(alias != null);
            Contract.Requires(original != null);
            Contract.Ensures(cc.Constants.ContainsKey(alias));

            if (!cc.Flags.ContainsKey(alias))
            {
                var fb = cc.Flags[original];
                cc.Constants.Add(alias, fb);
            }
        }

        /// <summary>
        /// Defines the appropriate constants for a word (W?FOO, A?FOO, ACT?FOO, PREP?FOO),
        /// creating the IWordBuilder if needed.
        /// </summary>
        /// <param name="cc">The CompileCtx.</param>
        /// <param name="word">The Word.</param>
        static void DefineWord(CompileCtx cc, IWord word)
        {
            Contract.Requires(cc != null);
            Contract.Requires(word != null);
            Contract.Ensures(cc.Vocabulary.ContainsKey(word));

            string rawWord = word.Atom.Text;

            if (!cc.Vocabulary.ContainsKey(word))
            {
                var wAtom = ZilAtom.Parse("W?" + rawWord, cc.Context);
                IOperand constantValue;
                if (cc.Constants.TryGetValue(wAtom, out constantValue) == false)
                {
                    var wb = cc.Game.DefineVocabularyWord(rawWord);
                    cc.Vocabulary.Add(word, wb);
                    cc.Constants.Add(wAtom, wb);
                }
                else
                {
                    if (constantValue is IWordBuilder)
                    {
                        cc.Vocabulary.Add(word, (IWordBuilder)constantValue);
                    }
                    else
                    {
                        throw new CompilerError(CompilerMessages.Nonvocab_Constant_0_Conflicts_With_Vocab_Word_1, wAtom, word.Atom);
                    }
                }
            }

            foreach (var pair in cc.Context.ZEnvironment.VocabFormat.GetVocabConstants(word))
            {
                var atom = ZilAtom.Parse(pair.Key, cc.Context);
                if (!cc.Constants.ContainsKey(atom))
                    cc.Constants.Add(atom,
                        cc.Game.DefineConstant(pair.Key,
                            cc.Game.MakeOperand(pair.Value)));
            }
        }

        struct TableElementOperand
        {
            public readonly IOperand Operand;
            public readonly bool? IsWord;

            public TableElementOperand(IOperand operand, bool? isWord)
            {
                this.Operand = operand;
                this.IsWord = isWord;
            }
        }

        static void BuildTable(CompileCtx cc, ZilTable zt, ITableBuilder tb)
        {
            Contract.Requires(cc != null);
            Contract.Requires(zt != null);
            Contract.Requires(tb != null);

            if ((zt.Flags & TableFlags.Lexv) != 0)
            {
                IOperand[] values = new IOperand[zt.ElementCount];
                zt.CopyTo(values, (zo, isWord) => CompileConstant(cc, zo), cc.Game.Zero, cc.Context);

                tb.AddByte((byte)(zt.ElementCount / 3));
                tb.AddByte(0);

                for (int i = 0; i < values.Length; i++)
                    if (i % 3 == 0)
                        tb.AddShort(values[i]);
                    else
                        tb.AddByte(values[i]);
            }
            else
            {
                TableElementOperand?[] values = new TableElementOperand?[zt.ElementCount];
                TableToArrayElementConverter<TableElementOperand?> convertElement = (zo, isWord) =>
                {
                    // it's usually a constant value
                    var constVal = CompileConstant(cc, zo);
                    if (constVal != null)
                        return new TableElementOperand(constVal, isWord);

                    // but we'll also allow a global name if the global contains a table
                    IGlobalBuilder global;
                    if (zo is ZilAtom && cc.Globals.TryGetValue((ZilAtom)zo, out global) && global.DefaultValue is ITableBuilder)
                        return new TableElementOperand(global.DefaultValue, isWord);

                    return null;
                };
                var defaultFiller = new TableElementOperand(cc.Game.Zero, null);
                zt.CopyTo(values, convertElement, defaultFiller, cc.Context);

                for (int i = 0; i < values.Length; i++)
                {
                    if (values[i] == null)
                    {
                        var rawElements = new ZilObject[zt.ElementCount];
                        zt.CopyTo(rawElements, (zo, isWord) => zo, null, cc.Context);
                        cc.Context.HandleError(new CompilerError(
                            zt.SourceLine,
                            CompilerMessages.Nonconstant_Initializer_For_0_1_2,
                            "table element",
                            i,
                            rawElements[i]));
                        values[i] = defaultFiller;
                    }
                }

                bool defaultWord = (zt.Flags & TableFlags.Byte) == 0;

                for (int i = 0; i < values.Length; i++)
                {
                    if (values[i].Value.IsWord ?? defaultWord)
                    {
                        tb.AddShort(values[i].Value.Operand);
                    }
                    else
                    {
                        tb.AddByte(values[i].Value.Operand);
                    }
                }
            }
        }

        public static string TranslateString(string str, Context ctx)
        {
            Contract.Requires(ctx != null);

            var crlfChar = ctx.GetGlobalVal(ctx.GetStdAtom(StdAtom.CRLF_CHARACTER)) as ZilChar;
            return TranslateString(
                str,
                crlfChar == null ? '|' : crlfChar.Char,
                ctx.GetGlobalOption(StdAtom.PRESERVE_SPACES_P));
        }

        public static string TranslateString(string str, char crlfChar, bool preserveSpaces)
        {
            // strip CR/LF and ensure 1 space afterward, translate crlfChar to LF,
            // and collapse two spaces after '.' or crlfChar into one
            var sb = new StringBuilder(str);
            char? last = null;
            bool sawDotSpace = false;

            for (int i = 0; i < sb.Length; i++)
            {
                char c = sb[i];

                if (!preserveSpaces)
                {
                    if ((last == '.' || last == crlfChar) && c == ' ')
                    {
                        sawDotSpace = true;
                    }
                    else if (sawDotSpace && c == ' ')
                    {
                        sb.Remove(i--, 1);
                        sawDotSpace = false;
                        last = c;
                        continue;
                    }
                    else
                    {
                        sawDotSpace = false;
                    }
                }

                switch (c)
                {
                    case '\r':
                        sb.Remove(i--, 1);
                        continue;

                    case '\n':
                        if (last == crlfChar)
                            sb.Remove(i--, 1);
                        else
                            sb[i] = ' ';
                        break;

                    default:
                        if (c == crlfChar)
                            sb[i] = '\n';
                        break;
                }

                last = c;
            }

            return sb.ToString();
        }

        public static IOperand CompileConstant(CompileCtx cc, ZilObject expr)
        {
            Contract.Requires(cc != null);
            Contract.Requires(expr != null);

            ZilAtom atom;

            var exprTypeAtom = expr.GetTypeAtom(cc.Context);
            switch (exprTypeAtom.StdAtom)
            {
                case StdAtom.FIX:
                    return cc.Game.MakeOperand(((ZilFix)expr).Value);

                case StdAtom.BYTE:
                    return cc.Game.MakeOperand(((ZilFix)((ZilHash)expr).GetPrimitive(cc.Context)).Value);

                case StdAtom.WORD:
                    return CompileConstant(cc, ((ZilWord)expr).Value);

                case StdAtom.STRING:
                    return cc.Game.MakeOperand(TranslateString(((ZilString)expr).Text, cc.Context));

                case StdAtom.CHARACTER:
                    return cc.Game.MakeOperand((byte)((ZilChar)expr).Char);

                case StdAtom.ATOM:
                    IRoutineBuilder routine;
                    IObjectBuilder obj;
                    IOperand operand;
                    atom = (ZilAtom)expr;
                    if (atom.StdAtom == StdAtom.T)
                        return cc.Game.One;
                    if (cc.Routines.TryGetValue(atom, out routine))
                        return routine;
                    if (cc.Objects.TryGetValue(atom, out obj))
                        return obj;
                    if (cc.Constants.TryGetValue(atom, out operand))
                        return operand;
                    return null;

                case StdAtom.FALSE:
                    return cc.Game.Zero;

                case StdAtom.TABLE:
                    var table = (ZilTable)expr;
                    ITableBuilder tb;
                    if (!cc.Tables.TryGetValue(table, out tb))
                    {
                        Contract.Assert((table.Flags & TableFlags.TempTable) != 0);
                        tb = cc.Game.DefineTable(table.Name, true);
                        cc.Tables.Add(table, tb);
                    }
                    return tb;

                case StdAtom.CONSTANT:
                    return CompileConstant(cc, ((ZilConstant)expr).Value);

                case StdAtom.FORM:
                    var form = (ZilForm)expr;
                    if (!form.IsEmpty &&
                        form.First == cc.Context.GetStdAtom(StdAtom.GVAL) &&
                        !form.Rest.IsEmpty &&
                        form.Rest.First.GetTypeAtom(cc.Context).StdAtom == StdAtom.ATOM &&
                        form.Rest.Rest.IsEmpty)
                    {
                        return CompileConstant(cc, form.Rest.First);
                    }
                    return null;

                case StdAtom.VOC:
                    atom = ZilAtom.Parse("W?" + (ZilAtom)expr.GetPrimitive(cc.Context), cc.Context);
                    if (cc.Constants.TryGetValue(atom, out operand))
                        return operand;
                    return null;

                default:
                    var primitive = expr.GetPrimitive(cc.Context);
                    if (primitive != expr && primitive.GetTypeAtom(cc.Context) != exprTypeAtom)
                        return CompileConstant(cc, primitive);
                    return null;
            }
        }

        static ZilRoutine MaybeRewriteRoutine(Context ctx, ZilRoutine origRoutine)
        {
            const string SExpectedResultType = "a list (with an arg spec and body) or FALSE";

            var rewriter = ctx.GetProp(ctx.GetStdAtom(StdAtom.ROUTINE), ctx.GetStdAtom(StdAtom.REWRITER)).AsApplicable(ctx);

            if (rewriter != null)
            {
                var result = rewriter.ApplyNoEval(ctx, new ZilObject[] {
                    origRoutine.Name,
                    origRoutine.ArgSpec.ToZilList(),
                    new ZilList(origRoutine.Body)
                });

                switch (result.GetTypeAtom(ctx).StdAtom)
                {
                    case StdAtom.LIST:
                        var list = (ZilList)result;
                        ZilList args, body;
                        if (((IStructure)list).GetLength(1) <= 1 || (args = list.First as ZilList) == null ||
                            args.GetTypeAtom(ctx).StdAtom != StdAtom.LIST)
                        {
                            throw new InterpreterError(InterpreterMessages._0_1_Must_Return_2, "routine rewriter", SExpectedResultType);
                        }
                        body = list.Rest;
                        return new ZilRoutine(origRoutine.Name, null, args, body, origRoutine.Flags);

                    case StdAtom.FALSE:
                        break;

                    default:
                        throw new InterpreterError(InterpreterMessages._0_1_Must_Return_2, "routine rewriter", SExpectedResultType);
                }
            }

            return origRoutine;
        }

        static void BuildRoutine(CompileCtx cc, ZilRoutine routine,
            IGameBuilder gb, IRoutineBuilder rb, bool entryPoint)
        {
            Contract.Requires(cc != null);
            Contract.Requires(routine != null);
            Contract.Requires(gb != null);
            Contract.Requires(rb != null);

            // give the user a chance to rewrite the routine
            routine = MaybeRewriteRoutine(cc.Context, routine);

            // set up arguments and locals
            cc.Locals.Clear();
            cc.TempLocalNames.Clear();
            cc.SpareLocals.Clear();
            cc.OuterLocals.Clear();

            if (cc.Context.TraceRoutines)
                rb.EmitPrint("[" + routine.Name, false);

            foreach (ArgItem arg in routine.ArgSpec)
            {
                ILocalBuilder lb;

                switch (arg.Type)
                {
                    case ArgItem.ArgType.Required:
                        lb = rb.DefineRequiredParameter(arg.Atom.ToString());
                        if (cc.Context.TraceRoutines)
                        {
                            rb.EmitPrint(" " + arg.Atom + "=", false);
                            rb.EmitPrint(PrintOp.Number, lb);
                        }
                        break;
                    case ArgItem.ArgType.Optional:
                        lb = rb.DefineOptionalParameter(arg.Atom.ToString());
                        break;
                    case ArgItem.ArgType.Auxiliary:
                        lb = rb.DefineLocal(arg.Atom.ToString());
                        break;
                    default:
                        throw new NotImplementedException();
                }

                cc.Locals.Add(arg.Atom, lb);

                if (arg.DefaultValue != null)
                {
                    lb.DefaultValue = CompileConstant(cc, arg.DefaultValue);
                    if (lb.DefaultValue == null)
                    {
                        // not a constant
                        if (arg.Type == ArgItem.ArgType.Optional)
                        {
                            if (!rb.HasArgCount)
                                throw new CompilerError(routine.SourceLine, CompilerMessages.Optional_Args_With_Nonconstant_Defaults_Not_Supported_For_This_Target);

                            var nextLabel = rb.DefineLabel();
                            rb.Branch(Condition.ArgProvided, lb, null, nextLabel, true);
                            var val = CompileAsOperand(cc, rb, arg.DefaultValue, routine.SourceLine, lb);
                            if (val != lb)
                                rb.EmitStore(lb, val);
                            rb.MarkLabel(nextLabel);
                        }
                        else
                        {
                            var val = CompileAsOperand(cc, rb, arg.DefaultValue, routine.SourceLine, lb);
                            if (val != lb)
                                rb.EmitStore(lb, val);
                        }
                    }
                }
            }

            if (cc.Context.TraceRoutines)
                rb.EmitPrint("]\n", false);

            // define a block for the routine
            cc.Blocks.Clear();
            cc.Blocks.Push(new Block
            {
                Name = routine.ActivationAtom,
                AgainLabel = rb.RoutineStart,
                ReturnLabel = null,
                Flags = BlockFlags.None
            });

            // generate code for routine body
            int i = 1;
            foreach (ZilObject stmt in routine.Body)
            {
                // only want the result of the last statement
                // and we never want results in the entry routine, since it can't return
                CompileStmt(cc, rb, stmt, !entryPoint && i == routine.BodyLength);
                i++;
            }

            // the entry point has to quit instead of returning
            if (entryPoint)
                rb.EmitQuit();

            // clean up
            cc.Locals.Clear();
            cc.SpareLocals.Clear();
            cc.OuterLocals.Clear();

            Contract.Assume(cc.Blocks.Count == 1);
            cc.Blocks.Pop();
        }

        static void CompileStmt(CompileCtx cc, IRoutineBuilder rb, ZilObject stmt, bool wantResult)
        {
            var form = stmt as ZilForm;
            if (form == null)
            {
                if (wantResult)
                {
                    var value = CompileConstant(cc, stmt);
                    if (value == null)
                    {
                        var error = new CompilerError(stmt, CompilerMessages.Expressions_Of_This_Type_Cannot_Be_Compiled);
                        if (stmt.GetTypeAtom(cc.Context).StdAtom == StdAtom.LIST)
                            error = error.Combine(new CompilerError(CompilerMessages.Misplaced_Bracket_In_COND));
                        throw error;
                    }

                    rb.Return(value);
                }
                //else
                //{
                    // TODO: warning message when skipping non-forms inside a routine?
                //}
            }
            else
            {
                MarkSequencePoint(cc, rb, form);

                var result = CompileForm(cc, rb, form, wantResult, null);

                if (wantResult)
                    rb.Return(result);
            }
        }

        static void MarkSequencePoint(CompileCtx cc, IRoutineBuilder rb, ZilObject node)
        {
            Contract.Requires(cc != null);
            Contract.Requires(rb != null);
            Contract.Requires(node != null);

            if (cc.WantDebugInfo)
            {
                var fileSourceLine = node.SourceLine as FileSourceLine;
                if (fileSourceLine != null)
                {
                    cc.Game.DebugFile.MarkSequencePoint(rb,
                        new DebugLineRef(fileSourceLine.FileName, fileSourceLine.Line, 1));
                }
            }
        }

        /// <summary>
        /// Compiles a FORM.
        /// </summary>
        /// <param name="cc">The compilation context.</param>
        /// <param name="rb">The current routine.</param>
        /// <param name="form">The FORM to compile.</param>
        /// <param name="wantResult">true if a result must be produced;
        /// false if a result must not be produced.</param>
        /// <param name="resultStorage">A suggested (but not mandatory) storage location
        /// for the result, or null.</param>
        /// <returns><paramref name="resultStorage"/> if the suggested location was used
        /// for the result, or another operand if the suggested location was not used,
        /// or null if a result was not produced.</returns>
        internal static IOperand CompileForm(CompileCtx cc, IRoutineBuilder rb, ZilForm form,
            bool wantResult, IVariable resultStorage)
        {
            Contract.Requires(cc != null);
            Contract.Requires(rb != null);
            Contract.Requires(form != null);
            Contract.Ensures(Contract.Result<IOperand>() != null || !wantResult);

            ILabel label1, label2;
            IOperand operand;

            using (DiagnosticContext.Push(form.SourceLine))
            {
                // expand macro invocations
                ZilObject expanded;
                try
                {
                    using (DiagnosticContext.Push(form.SourceLine))
                    {
                        expanded = form.Expand(cc.Context);
                    }
                }
                catch (InterpreterError ex)
                {
                    cc.Context.HandleError(ex);
                    return cc.Game.Zero;
                }

                if (expanded is ZilForm)
                {
                    form = (ZilForm)expanded;
                }
                else if (expanded is ZilSplice && ((ZilSplice)expanded).PopSpliceableFlag())
                {
                    var src = form.SourceLine;
                    form = new ZilForm(Enumerable.Concat(
                        new ZilObject[] {
                            cc.Context.GetStdAtom(StdAtom.BIND),
                            new ZilList(null, null)
                        },
                        (ZilList)expanded.GetPrimitive(cc.Context)));
                    form.SourceLine = src;
                }
                else
                {
                    if (wantResult)
                        return CompileAsOperand(cc, rb, expanded, form.SourceLine, resultStorage);
                    return null;
                }

                var head = form.First as ZilAtom;
                if (head == null)
                {
                    cc.Context.HandleError(new CompilerError(form, CompilerMessages.FORM_Must_Start_With_An_Atom));
                    return wantResult ? cc.Game.Zero : null;
                }

                // built-in statements handled by ZBuiltins
                var zversion = cc.Context.ZEnvironment.ZVersion;
                var argCount = form.Rest.Count();

                if (wantResult)
                {
                    // prefer the value version, then value+predicate, predicate, void
                    if (ZBuiltins.IsBuiltinValueCall(head.Text, zversion, argCount))
                    {
                        return ZBuiltins.CompileValueCall(head.Text, cc, rb, form, resultStorage);
                    }
                    if (ZBuiltins.IsBuiltinValuePredCall(head.Text, zversion, argCount))
                    {
                        label1 = rb.DefineLabel();
                        resultStorage = resultStorage ?? rb.Stack;
                        ZBuiltins.CompileValuePredCall(head.Text, cc, rb, form, resultStorage, label1, true);
                        rb.MarkLabel(label1);
                        return resultStorage;
                    }
                    if (ZBuiltins.IsBuiltinPredCall(head.Text, zversion, argCount))
                    {
                        label1 = rb.DefineLabel();
                        label2 = rb.DefineLabel();
                        resultStorage = resultStorage ?? rb.Stack;
                        ZBuiltins.CompilePredCall(head.Text, cc, rb, form, label1, true);
                        rb.EmitStore(resultStorage, cc.Game.Zero);
                        rb.Branch(label2);
                        rb.MarkLabel(label1);
                        rb.EmitStore(resultStorage, cc.Game.One);
                        rb.MarkLabel(label2);
                        return resultStorage;
                    }
                    if (ZBuiltins.IsBuiltinVoidCall(head.Text, zversion, argCount))
                    {
                        ZBuiltins.CompileVoidCall(head.Text, cc, rb, form);
                        return cc.Game.One;
                    }
                }
                else
                {
                    // prefer the void version, then predicate, value, value+predicate
                    // (predicate saves a cleanup instruction)
                    if (ZBuiltins.IsBuiltinVoidCall(head.Text, zversion, argCount))
                    {
                        ZBuiltins.CompileVoidCall(head.Text, cc, rb, form);
                        return null;
                    }
                    if (ZBuiltins.IsBuiltinPredCall(head.Text, zversion, argCount))
                    {
                        var dummy = rb.DefineLabel();
                        ZBuiltins.CompilePredCall(head.Text, cc, rb, form, dummy, true);
                        rb.MarkLabel(dummy);
                        return null;
                    }
                    if (ZBuiltins.IsBuiltinValueCall(head.Text, zversion, argCount))
                    {
                        if (ZBuiltins.CompileValueCall(head.Text, cc, rb, form, null) == rb.Stack)
                            rb.EmitPopStack();
                        return null;
                    }
                    if (ZBuiltins.IsBuiltinValuePredCall(head.Text, zversion, argCount))
                    {
                        label1 = rb.DefineLabel();
                        ZBuiltins.CompileValuePredCall(head.Text, cc, rb, form, rb.Stack, label1, true);
                        rb.MarkLabel(label1);
                        rb.EmitPopStack();
                        return null;
                    }
                }

                // built-in statements handled specially
                ZilAtom atom;
                IGlobalBuilder global;
                SoftGlobal softGlobal;
                ILocalBuilder local;
                IObjectBuilder objbld;
                IRoutineBuilder routine;
                IVariable result;
                switch (head.StdAtom)
                {
                    case StdAtom.GVAL:
                        atom = form.Rest.First as ZilAtom;
                        if (atom == null)
                        {
                            cc.Context.HandleError(new CompilerError(form, CompilerMessages.Expected_An_Atom_After_0, "GVAL"));
                            return wantResult ? cc.Game.Zero : null;
                        }

                        // constant, global, object, or routine
                        if (cc.Constants.TryGetValue(atom, out operand))
                            return operand;
                        if (cc.Globals.TryGetValue(atom, out global))
                            return global;
                        if (cc.Objects.TryGetValue(atom, out objbld))
                            return objbld;
                        if (cc.Routines.TryGetValue(atom, out routine))
                            return routine;

                        // soft global
                        if (cc.SoftGlobals.TryGetValue(atom, out softGlobal))
                        {
                            if (wantResult)
                            {
                                resultStorage = resultStorage ?? rb.Stack;
                                rb.EmitBinary(
                                    softGlobal.IsWord ? BinaryOp.GetWord : BinaryOp.GetByte,
                                    cc.SoftGlobalsTable,
                                    cc.Game.MakeOperand(softGlobal.Offset),
                                    resultStorage);
                                return resultStorage;
                            }
                            return null;
                        }

                        // quirks: local
                        if (cc.Locals.TryGetValue(atom, out local))
                        {
                            cc.Context.HandleWarning(new CompilerError(
                                form,
                                CompilerMessages.No_Such_0_Variable_1_Using_The_2_Instead,
                                "global",
                                atom,
                                "local"));
                            return local;
                        }

                        // error
                        cc.Context.HandleError(new CompilerError(form, CompilerMessages.Undefined_0_1, "global or constant", atom));
                        return wantResult ? cc.Game.Zero : null;
                    case StdAtom.LVAL:
                        atom = form.Rest.First as ZilAtom;
                        if (atom == null)
                        {
                            cc.Context.HandleError(new CompilerError(form, CompilerMessages.Expected_An_Atom_After_0, "LVAL"));
                            return wantResult ? cc.Game.Zero : null;
                        }

                        // local
                        if (cc.Locals.TryGetValue(atom, out local))
                            return local;

                        // quirks: constant, global, object, or routine
                        if (cc.Constants.TryGetValue(atom, out operand))
                        {
                            cc.Context.HandleWarning(new CompilerError(
                                form,
                                CompilerMessages.No_Such_0_Variable_1_Using_The_2_Instead,
                                "local", 
                                atom,
                                "constant"));
                            return operand;
                        }
                        if (cc.Globals.TryGetValue(atom, out global))
                        {
                            cc.Context.HandleWarning(new CompilerError(
                                form,
                                CompilerMessages.No_Such_0_Variable_1_Using_The_2_Instead,
                                "local",
                                atom,
                                "global"));
                            return global;
                        }
                        if (cc.Objects.TryGetValue(atom, out objbld))
                        {
                            cc.Context.HandleWarning(new CompilerError(
                                form,
                                CompilerMessages.No_Such_0_Variable_1_Using_The_2_Instead, 
                                "local",
                                atom,
                                "object"));
                            return objbld;
                        }
                        if (cc.Routines.TryGetValue(atom, out routine))
                        {
                            cc.Context.HandleWarning(new CompilerError(
                                form,
                                CompilerMessages.No_Such_0_Variable_1_Using_The_2_Instead, 
                                "local",
                                atom,
                                "routine"));
                            return routine;
                        }

                        // error
                        cc.Context.HandleError(new CompilerError(form, CompilerMessages.Undefined_0_1, "local", atom));
                        return wantResult ? cc.Game.Zero : null;

                    case StdAtom.ITABLE:
                    case StdAtom.TABLE:
                    case StdAtom.PTABLE:
                    case StdAtom.LTABLE:
                    case StdAtom.PLTABLE:
                        return CompileImpromptuTable(cc, rb, form, wantResult, resultStorage);

                    case StdAtom.PROG:
                        return CompilePROG(cc, rb, form.Rest, form.SourceLine, wantResult, resultStorage, "PROG", false, true);
                    case StdAtom.REPEAT:
                        return CompilePROG(cc, rb, form.Rest, form.SourceLine, wantResult, resultStorage, "REPEAT", true, true);
                    case StdAtom.BIND:
                        return CompilePROG(cc, rb, form.Rest, form.SourceLine, wantResult, resultStorage, "BIND", false, false);

                    case StdAtom.DO:
                        return CompileDO(cc, rb, form.Rest, form.SourceLine, wantResult, resultStorage);
                    case StdAtom.MAP_CONTENTS:
                        return CompileMAP_CONTENTS(cc, rb, form.Rest, form.SourceLine, wantResult, resultStorage);
                    case StdAtom.MAP_DIRECTIONS:
                        return CompileMAP_DIRECTIONS(cc, rb, form.Rest, form.SourceLine, wantResult, resultStorage);

                    case StdAtom.COND:
                        return CompileCOND(cc, rb, form.Rest, form.SourceLine, wantResult, resultStorage);

                    case StdAtom.VERSION_P:
                        return CompileVERSION_P(cc, rb, form.Rest, form.SourceLine, wantResult, resultStorage);
                    case StdAtom.IFFLAG:
                        return CompileIFFLAG(cc, rb, form.Rest, form.SourceLine, wantResult, resultStorage);

                    case StdAtom.NOT:
                    case StdAtom.F_P:
                    case StdAtom.T_P:
                        if (form.Rest.First == null || (form.Rest.Rest != null && !form.Rest.Rest.IsEmpty))
                        {
                            cc.Context.HandleError(new CompilerError(
                                form,
                                CompilerMessages._0_Requires_1_Argument1s,
                                head,
                                new CountableString("exactly 1", false)));
                            return cc.Game.Zero;
                        }
                        resultStorage = resultStorage ?? rb.Stack;
                        label1 = rb.DefineLabel();
                        label2 = rb.DefineLabel();
                        CompileCondition(cc, rb, form.Rest.First, form.SourceLine, label1, head.StdAtom != StdAtom.T_P);
                        rb.EmitStore(resultStorage, cc.Game.One);
                        rb.Branch(label2);
                        rb.MarkLabel(label1);
                        rb.EmitStore(resultStorage, cc.Game.Zero);
                        rb.MarkLabel(label2);
                        return resultStorage;

                    case StdAtom.OR:
                    case StdAtom.AND:
                        return CompileBoolean(cc, rb, form.Rest, form.SourceLine, head.StdAtom == StdAtom.AND, wantResult, resultStorage);

                    case StdAtom.TELL:
                        return CompileTell(cc, rb, form);
                }

                // routine calls
                var obj = cc.Context.GetZVal(cc.Context.ZEnvironment.InternGlobalName(head));

                while (obj is ZilConstant)
                    obj = ((ZilConstant)obj).Value;

                if (obj is ZilRoutine)
                {
                    var rtn = (ZilRoutine)obj;

                    // check argument count
                    var args = form.Skip(1).ToArray();
                    if (args.Length < rtn.ArgSpec.MinArgCount ||
                        (rtn.ArgSpec.MaxArgCount != null && args.Length > rtn.ArgSpec.MaxArgCount))
                    {
                        cc.Context.HandleError(CompilerError.WrongArgCount(
                            rtn.Name.ToString(),
                            new ArgCountRange(rtn.ArgSpec.MinArgCount, rtn.ArgSpec.MaxArgCount)));
                        return wantResult ? cc.Game.Zero : null;
                    }

                    // compile routine call
                    result = wantResult ? (resultStorage ?? rb.Stack) : null;
                    using (Operands argOperands = Operands.Compile(cc, rb, form.SourceLine, args))
                    {
                        rb.EmitCall(cc.Routines[head], argOperands.ToArray(), result);
                    }
                    return result;
                }
                if (obj is ZilFalse)
                {
                    // this always returns 0. we can eliminate the call if none of the arguments have side effects.
                    var argsWithSideEffects = form.Skip(1).Where(zo => HasSideEffects(cc, zo)).ToArray();

                    if (argsWithSideEffects.Length > 0)
                    {
                        result = wantResult ? (resultStorage ?? rb.Stack) : null;
                        using (Operands argOperands = Operands.Compile(cc, rb, form.SourceLine, argsWithSideEffects))
                        {
                            var operands = argOperands.ToArray();
                            if (operands.Any(o => o == rb.Stack))
                                rb.EmitCall(cc.Game.Zero, operands.Where(o => o == rb.Stack).ToArray(), result);
                        }
                        return result;
                    }
                    return cc.Game.Zero;
                }

                // unrecognized
                CompilerError error;
                if (!ZBuiltins.IsNearMatchBuiltin(head.Text, zversion, argCount, out error))
                {
                    error = new CompilerError(CompilerMessages.Unrecognized_0_1, "routine or instruction", head);
                }
                cc.Context.HandleError(error);
                return wantResult ? cc.Game.Zero : null;
            }
        }

        static bool HasSideEffects(CompileCtx cc, ZilObject expr)
        {
            var form = expr as ZilForm;

            // only forms can have side effects
            if (form == null)
                return false;

            // malformed forms are errors anyway
            var head = form.First as ZilAtom;
            if (head == null)
                return false;

            // some instructions always have side effects
            var zversion = cc.Context.ZEnvironment.ZVersion;
            var argCount = form.Rest.Count();
            if (ZBuiltins.IsBuiltinWithSideEffects(head.Text, zversion, argCount))
                return true;

            // routines are presumed to have side effects
            if (cc.Routines.ContainsKey(head))
                return true;

            // other instructions could still have side effects if their arguments do
            foreach (ZilObject obj in form.Rest)
                if (HasSideEffects(cc, obj))
                    return true;

            return false;
        }

        public static IOperand CompileAsOperand(CompileCtx cc, IRoutineBuilder rb, ZilObject expr, ISourceLine src,
            IVariable suggestion = null)
        {
            Contract.Requires(cc != null);
            Contract.Requires(rb != null);
            Contract.Requires(expr != null);
            Contract.Requires(src != null);
            Contract.Ensures(Contract.Result<IOperand>() != null);

            var constant = CompileConstant(cc, expr);
            if (constant != null)
                return constant;

            switch (expr.GetTypeAtom(cc.Context).StdAtom)
            {
                case StdAtom.FORM:
                    return CompileForm(cc, rb, (ZilForm)expr, true, suggestion ?? rb.Stack);

                case StdAtom.ATOM:
                    var atom = (ZilAtom)expr;
                    if (cc.Globals.ContainsKey(atom))
                    {
                        cc.Context.HandleWarning(new CompilerError(expr.SourceLine ?? src,
                            CompilerMessages.Bare_Atom_0_Interpreted_As_Global_Variable_Index_Be_Sure_This_Is_Right, atom));
                        return cc.Globals[atom].Indirect;
                    }
                    if (cc.SoftGlobals.ContainsKey(atom))
                    {
                        cc.Context.HandleError(new CompilerError(
                            expr.SourceLine ?? src,
                            CompilerMessages.Soft_Variable_0_May_Not_Be_Used_Here,
                            atom));
                    }
                    else
                    {
                        cc.Context.HandleError(new CompilerError(
                            expr.SourceLine ?? src,
                            CompilerMessages.Bare_Atom_0_Used_As_Operand_Is_Not_A_Global_Variable,
                            atom));
                    }
                    return cc.Game.Zero;

                case StdAtom.ADECL:
                    // TODO: verify DECL
                    return CompileAsOperand(cc, rb, ((ZilAdecl)expr).First, src, suggestion ?? rb.Stack);

                default:
                    cc.Context.HandleError(new CompilerError(
                        expr.SourceLine ?? src,
                        CompilerMessages.Expected_A_FORM_ATOM_Or_ADECL_But_Found_0,
                        expr));
                    return cc.Game.Zero;
            }
        }

        /// <summary>
        /// Compiles an expression for its value, and then branches on whether the value is nonzero.
        /// </summary>
        /// <param name="cc">The compile context.</param>
        /// <param name="rb">The routine builder.</param>
        /// <param name="expr">The expression to compile.</param>
        /// <param name="resultStorage">The variable in which to store the value, or <b>null</b> to
        /// use a natural or temporary location. Must not be the stack.</param>
        /// <param name="label">The label to branch to.</param>
        /// <param name="polarity"><b>true</b> to branch when the expression's value is nonzero,
        /// or <b>false</b> to branch when it's zero.</param>
        /// <param name="tempVarProvider">A delegate that returns a temporary variable to use for
        /// the result. Will only be called when <paramref name="resultStorage"/> is <b>null</b> and
        /// the expression has no natural location.</param>
        /// <returns>The variable where the expression value was stored: always <paramref name="resultStorage"/> if
        /// it is non-null and the expression is valid. Otherwise, may be a constant, or the natural
        /// location of the expression, or a temporary variable from <paramref name="tempVarProvider"/>.</returns>
        internal static IOperand CompileAsOperandWithBranch(CompileCtx cc, IRoutineBuilder rb, ZilObject expr,
            IVariable resultStorage, ILabel label, bool polarity, Func<IVariable> tempVarProvider = null)
        {
            Contract.Requires(cc != null);
            Contract.Requires(rb != null);
            Contract.Requires(expr != null);
            Contract.Requires(label != null);
            Contract.Requires(resultStorage != rb.Stack);
            Contract.Requires(resultStorage != null || tempVarProvider != null);
            Contract.Ensures(Contract.Result<IOperand>() != null);

            expr = expr.Expand(cc.Context);
            StdAtom type = expr.GetTypeAtom(cc.Context).StdAtom;
            IOperand result = resultStorage;

            if (type == StdAtom.FALSE)
            {
                if (resultStorage == null)
                {
                    result = cc.Game.Zero;
                }
                else
                {
                    rb.EmitStore(resultStorage, cc.Game.Zero);
                }

                if (polarity == false)
                    rb.Branch(label);

                return result;
            }
            if (type == StdAtom.FIX)
            {
                var value = ((ZilFix)expr).Value;

                if (resultStorage == null)
                {
                    result = cc.Game.MakeOperand(value);
                }
                else
                {
                    rb.EmitStore(resultStorage, cc.Game.MakeOperand(value));
                }

                bool nonzero = value != 0;
                if (polarity == nonzero)
                    rb.Branch(label);

                return result;
            }
            if (type == StdAtom.ADECL)
            {
                // TODO: check DECL
                return CompileAsOperandWithBranch(cc, rb, ((ZilAdecl)expr).First, resultStorage, label, polarity, tempVarProvider);
            }
            if (type != StdAtom.FORM)
            {
                var value = CompileConstant(cc, expr);
                if (value == null)
                {
                    cc.Context.HandleError(new CompilerError(expr, CompilerMessages.Expressions_Of_This_Type_Cannot_Be_Compiled));
                }
                else
                {
                    if (resultStorage == null)
                    {
                        result = value;
                    }
                    else
                    {
                        rb.EmitStore(resultStorage, value);
                    }

                    if (polarity == true)
                        rb.Branch(label);
                }
                return result;
            }

            // it's a FORM
            var form = expr as ZilForm;
            var head = form.First as ZilAtom;

            if (head == null)
            {
                cc.Context.HandleError(new CompilerError(form, CompilerMessages.FORM_Must_Start_With_An_Atom));
                return cc.Game.Zero;
            }

            // check for standard built-ins
            // prefer the value+predicate version, then value, predicate, void
            var zversion = cc.Context.ZEnvironment.ZVersion;
            var argCount = form.Count() - 1;
            if (ZBuiltins.IsBuiltinValuePredCall(head.Text, zversion, argCount))
            {
                if (resultStorage == null)
                    resultStorage = tempVarProvider();

                ZBuiltins.CompileValuePredCall(head.Text, cc, rb, form, resultStorage, label, polarity);
                return resultStorage;
            }
            if (ZBuiltins.IsBuiltinValueCall(head.Text, zversion, argCount))
            {
                result = ZBuiltins.CompileValueCall(head.Text, cc, rb, form, resultStorage);
                if (resultStorage != null && resultStorage != result)
                {
                    rb.EmitStore(resultStorage, result);
                    result = resultStorage;
                }
                else if (resultStorage == null && result == rb.Stack)
                {
                    resultStorage = tempVarProvider();
                    rb.EmitStore(resultStorage, result);
                    result = resultStorage;
                }
                rb.BranchIfZero(result, label, !polarity);
                return result;
            }
            if (ZBuiltins.IsBuiltinPredCall(head.Text, zversion, argCount))
            {
                if (resultStorage == null)
                    resultStorage = tempVarProvider();

                var label1 = rb.DefineLabel();
                var label2 = rb.DefineLabel();
                ZBuiltins.CompilePredCall(head.Text, cc, rb, form, label1, true);
                rb.EmitStore(resultStorage, cc.Game.Zero);
                rb.Branch(polarity ? label2 : label);
                rb.MarkLabel(label1);
                rb.EmitStore(resultStorage, cc.Game.One);
                if (polarity)
                    rb.Branch(label);
                rb.MarkLabel(label2);
                return resultStorage;
            }
            if (ZBuiltins.IsBuiltinVoidCall(head.Text, zversion, argCount))
            {
                ZBuiltins.CompileVoidCall(head.Text, cc, rb, form);

                // void calls return true
                if (resultStorage == null)
                {
                    result = cc.Game.One;
                }
                else
                {
                    rb.EmitStore(resultStorage, cc.Game.One);
                }

                if (polarity == true)
                    rb.Branch(label);

                return result;
            }

            // for anything more complicated, treat it as a value
            result = CompileAsOperand(cc, rb, form, form.SourceLine, resultStorage);
            if (resultStorage != null && resultStorage != result)
            {
                rb.EmitStore(resultStorage, result);
                result = resultStorage;
            }
            else if (resultStorage == null && result == rb.Stack)
            {
                resultStorage = tempVarProvider();
                rb.EmitStore(resultStorage, result);
                result = resultStorage;
            }
            
            rb.BranchIfZero(result, label, !polarity);
            return result;
        }

        static void CompileCondition(CompileCtx cc, IRoutineBuilder rb, ZilObject expr,
            ISourceLine src, ILabel label, bool polarity)
        {
            Contract.Requires(cc != null);
            Contract.Requires(rb != null);
            Contract.Requires(expr != null);
            Contract.Requires(src != null);
            Contract.Requires(label != null);

            expr = expr.Expand(cc.Context);
            var typeAtom = expr.GetTypeAtom(cc.Context);
            StdAtom type = typeAtom.StdAtom;

            if (type == StdAtom.FALSE)
            {
                if (polarity == false)
                    rb.Branch(label);
                return;
            }
            if (type == StdAtom.ATOM)
            {
                var atom = (ZilAtom)expr;
                if (atom.StdAtom != StdAtom.T && atom.StdAtom != StdAtom.ELSE)
                {
                    // could be a missing , or . before variable name
                    var warning = new CompilerError(src, CompilerMessages.Bare_Atom_0_Treated_As_True_Here, expr);

                    if (cc.Locals.ContainsKey(atom) || cc.Globals.ContainsKey(atom))
                        warning = warning.Combine(new CompilerError(src, CompilerMessages.Did_You_Mean_The_Variable));

                    cc.Context.HandleWarning(warning);
                }

                if (polarity == true)
                    rb.Branch(label);
                return;
            }
            if (type == StdAtom.FIX)
            {
                bool nonzero = ((ZilFix)expr).Value != 0;
                if (polarity == nonzero)
                    rb.Branch(label);
                return;
            }
            if (type != StdAtom.FORM)
            {
                cc.Context.HandleError(new CompilerError(expr.SourceLine ?? src, CompilerMessages.Expressions_Of_This_Type_Cannot_Be_Compiled));
                return;
            }

            // it's a FORM
            var form = expr as ZilForm;
            var head = form.First as ZilAtom;

            if (head == null)
            {
                cc.Context.HandleError(new CompilerError(form, CompilerMessages.FORM_Must_Start_With_An_Atom));
                return;
            }

            // check for standard built-ins
            // prefer the predicate version, then value, value+predicate, void
            // (value+predicate is hard to clean up)
            var zversion = cc.Context.ZEnvironment.ZVersion;
            var argCount = form.Count() - 1;
            if (ZBuiltins.IsBuiltinPredCall(head.Text, zversion, argCount))
            {
                ZBuiltins.CompilePredCall(head.Text, cc, rb, form, label, polarity);
                return;
            }
            if (ZBuiltins.IsBuiltinValueCall(head.Text, zversion, argCount))
            {
                var result = ZBuiltins.CompileValueCall(head.Text, cc, rb, form, rb.Stack);
                var numericResult = result as INumericOperand;
                if (numericResult != null)
                {
                    if ((numericResult.Value != 0) == polarity)
                        rb.Branch(label);
                }
                else
                {
                    rb.BranchIfZero(result, label, !polarity);
                }
                return;
            }
            if (ZBuiltins.IsBuiltinValuePredCall(head.Text, zversion, argCount))
            {
                if (rb.CleanStack)
                {
                    /* wasting the branch and checking the result with ZERO? is more efficient
                     * than using the branch and having to clean the result off the stack */
                    var noBranch = rb.DefineLabel();
                    ZBuiltins.CompileValuePredCall(head.Text, cc, rb, form, rb.Stack, noBranch, true);
                    rb.MarkLabel(noBranch);
                    rb.BranchIfZero(rb.Stack, label, !polarity);
                }
                else
                {
                    ZBuiltins.CompileValuePredCall(head.Text, cc, rb, form, rb.Stack, label, polarity);
                }
                return;
            }
            if (ZBuiltins.IsBuiltinVoidCall(head.Text, zversion, argCount))
            {
                ZBuiltins.CompileVoidCall(head.Text, cc, rb, form);

                // void calls return true
                if (polarity == true)
                    rb.Branch(label);
                return;
            }

            // special cases
            IOperand op1;
            var args = form.Skip(1).ToArray();

            switch (head.StdAtom)
            {
                case StdAtom.NOT:
                case StdAtom.F_P:
                    polarity = !polarity;
                    goto case StdAtom.T_P;

                case StdAtom.T_P:
                    if (args.Length == 1)
                    {
                        CompileCondition(cc, rb, args[0], form.SourceLine, label, polarity);
                    }
                    else
                    {
                        cc.Context.HandleError(new CompilerError(
                            expr.SourceLine ?? src,
                            CompilerMessages._0_Requires_1_Argument1s,
                            head,
                            new CountableString("exactly 1", false)));
                    }
                    break;

                case StdAtom.OR:
                case StdAtom.AND:
                    CompileBoolean(cc, rb, args, form.SourceLine, head.StdAtom == StdAtom.AND, label, polarity);
                    break;

                default:
                    op1 = CompileAsOperand(cc, rb, form, form.SourceLine);
                    var numericResult = op1 as INumericOperand;
                    if (numericResult != null)
                    {
                        if ((numericResult.Value != 0) == polarity)
                            rb.Branch(label);
                    }
                    else
                    {
                        rb.BranchIfZero(op1, label, !polarity);
                    }
                    break;
            }
        }

        static void CompileBoolean(CompileCtx cc, IRoutineBuilder rb, ZilObject[] args,
            ISourceLine src, bool and, ILabel label, bool polarity)
        {
            Contract.Requires(cc != null);
            Contract.Requires(rb != null);
            Contract.Requires(args != null);
            Contract.Requires(src != null);
            Contract.Requires(label != null);

            if (args.Length == 0)
            {
                // <AND> is true, <OR> is false
                if (and == polarity)
                    rb.Branch(label);
            }
            else if (args.Length == 1)
            {
                CompileCondition(cc, rb, args[0], src, label, polarity);
            }
            else if (and == polarity)
            {
                // AND or NOR
                var failure = rb.DefineLabel();
                for (int i = 0; i < args.Length - 1; i++)
                    CompileCondition(cc, rb, args[i], src, failure, !and);

                /* Historical note: ZILCH considered <AND ... <SET X 0>> to be true,
                 * even though <SET X 0> is false. We emulate the bug by compiling the
                 * last element as a statement instead of a condition when it fits
                 * this pattern. */
                ZilObject last = args[args.Length - 1];
                if (and && IsSetToZeroForm(last))
                {
                    cc.Context.HandleWarning(new CompilerError(last.SourceLine, CompilerMessages.Treating_SET_To_0_As_True_Here));
                    CompileStmt(cc, rb, last, false);
                }
                else
                    CompileCondition(cc, rb, last, src, label, and);

                rb.MarkLabel(failure);
            }
            else
            {
                // NAND or OR
                for (int i = 0; i < args.Length - 1; i++)
                    CompileCondition(cc, rb, args[i], src, label, !and);

                /* Emulate the aforementioned ZILCH bug. */
                ZilObject last = args[args.Length - 1];
                if (and && IsSetToZeroForm(last))
                {
                    cc.Context.HandleWarning(new CompilerError(last.SourceLine, CompilerMessages.Treating_SET_To_0_As_True_Here));
                    CompileStmt(cc, rb, last, false);
                }
                else
                    CompileCondition(cc, rb, last, src, label, !and);
            }
        }

        static bool IsSetToZeroForm(ZilObject last)
        {
            var form = last as ZilForm;
            if (form == null)
                return false;

            var atom = form.First as ZilAtom;
            if (atom == null ||
                (atom.StdAtom != StdAtom.SET && atom.StdAtom != StdAtom.SETG))
                return false;

            ZilFix fix;
            if (form.Rest == null || form.Rest.Rest == null ||
                (fix = form.Rest.Rest.First as ZilFix) == null ||
                fix.Value != 0)
                return false;

            return true;
        }

        static IOperand CompileBoolean(CompileCtx cc, IRoutineBuilder rb, ZilList args,
            ISourceLine src, bool and, bool wantResult, IVariable resultStorage)
        {
            Contract.Requires(cc != null);
            Contract.Requires(rb != null);
            Contract.Requires(args != null);
            Contract.Requires(src != null);
            Contract.Ensures(Contract.Result<IOperand>() != null || !wantResult);

            if (args.IsEmpty)
                return and ? cc.Game.One : cc.Game.Zero;

            if (args.Rest.IsEmpty)
            {
                if (wantResult)
                    return CompileAsOperand(cc, rb, args.First, src, resultStorage);

                if (args.First is ZilForm)
                    return CompileForm(cc, rb, (ZilForm)args.First, wantResult, resultStorage);

                return cc.Game.Zero;
            }

            IOperand result;

            if (wantResult)
            {
                var tempAtom = ZilAtom.Parse("?TMP", cc.Context);
                var lastLabel = rb.DefineLabel();
                IVariable tempVar = null;

                if (resultStorage == null)
                    resultStorage = rb.Stack;

                Contract.Assert(resultStorage != null);

                IVariable nonStackResultStorage = (resultStorage != rb.Stack) ? resultStorage : null;
                Func<IVariable> tempVarProvider = () =>
                {
                    if (tempVar == null)
                    {
                        PushInnerLocal(cc, rb, tempAtom);
                        tempVar = cc.Locals[tempAtom];
                    }
                    return tempVar;
                };

                while (!args.Rest.IsEmpty)
                {
                    var nextLabel = rb.DefineLabel();

                    if (and)
                    {
                        // for AND we only need the result of the last expr; otherwise we only care about truth value
                        CompileCondition(cc, rb, args.First, src, nextLabel, true);
                        rb.EmitStore(resultStorage, cc.Game.Zero);
                    }
                    else
                    {
                        // for OR, if the value is true we want to return it; otherwise discard it and try the next expr
                        result = CompileAsOperandWithBranch(cc, rb, args.First, nonStackResultStorage, nextLabel, false, tempVarProvider);

                        if (result != resultStorage)
                            rb.EmitStore(resultStorage, result);
                    }

                    rb.Branch(lastLabel);
                    rb.MarkLabel(nextLabel);

                    args = args.Rest;
                }

                result = CompileAsOperand(cc, rb, args.First, src, resultStorage);
                if (result != resultStorage)
                    rb.EmitStore(resultStorage, result);

                rb.MarkLabel(lastLabel);

                if (tempVar != null)
                    PopInnerLocal(cc, tempAtom);

                return resultStorage;
            }
            else
            {
                var lastLabel = rb.DefineLabel();

                while (!args.Rest.IsEmpty)
                {
                    var nextLabel = rb.DefineLabel();

                    CompileCondition(cc, rb, args.First, src, nextLabel, and);

                    rb.Branch(lastLabel);
                    rb.MarkLabel(nextLabel);

                    args = args.Rest;
                }

                if (args.First is ZilForm)
                    CompileForm(cc, rb, (ZilForm)args.First, false, null);

                rb.MarkLabel(lastLabel);

                return cc.Game.Zero;
            }
        }

        static IOperand CompilePROG(CompileCtx cc, IRoutineBuilder rb, ZilList args,
#pragma warning disable RECS0154 // Parameter is never used
            ISourceLine src, bool wantResult, IVariable resultStorage, string name, bool repeat, bool catchy)
#pragma warning restore RECS0154 // Parameter is never used
        {
            Contract.Requires(cc != null);
            Contract.Requires(rb != null);
            Contract.Requires(src != null);

            // NOTE: resultStorage is unused here, because PROG's result could come from
            // a RETURN statement (and REPEAT's result can *only* come from RETURN).
            // thus we have to return the result on the stack, because RETURN doesn't have
            // the context needed to put its result in the right place.

            if (args == null || args.First == null)
            {
                throw new CompilerError(CompilerMessages._0_Argument_1_2, name, 1, "argument must be an activation atom or binding list");
            }

            var activationAtom = args.First as ZilAtom;
            if (activationAtom != null)
            {
                args = args.Rest;
            }

            if (args == null || args.First == null ||
                args.First.GetTypeAtom(cc.Context).StdAtom != StdAtom.LIST)
            {
                throw new CompilerError(CompilerMessages._0_Missing_Binding_List, name);
            }

            // add new locals, if any
            var innerLocals = new Queue<ZilAtom>();
            foreach (ZilObject obj in (ZilList)args.First)
            {
                ZilAtom atom;

                switch (obj.GetTypeAtom(cc.Context).StdAtom)
                {
                    case StdAtom.ATOM:
                        atom = (ZilAtom)obj;
                        innerLocals.Enqueue(atom);
                        PushInnerLocal(cc, rb, atom);
                        break;

                    case StdAtom.ADECL:
                        atom = ((ZilAdecl)obj).First as ZilAtom;
                        if (atom == null)
                            throw new CompilerError(CompilerMessages.Invalid_Atom_Binding);
                        innerLocals.Enqueue(atom);
                        PushInnerLocal(cc, rb, atom);
                        break;

                    case StdAtom.LIST:
                        var list = (ZilList)obj;
                        if (list.First == null || list.Rest == null ||
                            list.Rest.First == null || (list.Rest.Rest != null && list.Rest.Rest.First != null))
                        {
                            throw new CompilerError(CompilerMessages._0_Expected_1_Element1s_In_Binding_List, name, 2);
                        }
                        atom = list.First as ZilAtom;
                        if (atom == null)
                        {
                            var adecl = list.First as ZilAdecl;
                            if (adecl != null)
                                atom = adecl.First as ZilAtom;
                        }
                        ZilObject value = list.Rest.First;
                        if (atom == null)
                            throw new CompilerError(CompilerMessages.Invalid_Atom_Binding);
                        innerLocals.Enqueue(atom);
                        var lb = PushInnerLocal(cc, rb, atom);
                        var loc = CompileAsOperand(cc, rb, value, src, lb);
                        if (loc != lb)
                            rb.EmitStore(lb, loc);
                        break;

                    default:
                        throw new CompilerError(CompilerMessages.Elements_Of_Binding_List_Must_Be_Atoms_Or_Lists);
                }
            }

            var block = new Block
            {
                Name = activationAtom,
                AgainLabel = rb.DefineLabel(),
                ReturnLabel = rb.DefineLabel()
            };

            if (wantResult)
                block.Flags |= BlockFlags.WantResult;
            if (!catchy)
                block.Flags |= BlockFlags.ExplicitOnly;

            rb.MarkLabel(block.AgainLabel);
            cc.Blocks.Push(block);

            try
            {
                // generate code for prog body
                args = args.Rest as ZilList;
                var clauseResult = CompileClauseBody(cc, rb, args, wantResult, rb.Stack);

                if (repeat)
                    rb.Branch(block.AgainLabel);

                if ((block.Flags & BlockFlags.Returned) != 0)
                    rb.MarkLabel(block.ReturnLabel);

                return wantResult ? clauseResult : null;
            }
            finally
            {
                while (innerLocals.Count > 0)
                    PopInnerLocal(cc, innerLocals.Dequeue());

                cc.Blocks.Pop();
            }
        }

        public static ILocalBuilder PushInnerLocal(CompileCtx cc, IRoutineBuilder rb, ZilAtom atom)
        {
            Contract.Requires(cc != null);
            Contract.Requires(rb != null);
            Contract.Requires(atom != null);

            string name = atom.Text;

            ILocalBuilder prev;
            if (cc.Locals.TryGetValue(atom, out prev))
            {
                // save the old binding
                Stack<ILocalBuilder> stk;
                if (cc.OuterLocals.TryGetValue(atom, out stk) == false)
                {
                    stk = new Stack<ILocalBuilder>();
                    cc.OuterLocals.Add(atom, stk);
                }
                stk.Push(prev);
            }

            ILocalBuilder result;
            if (cc.SpareLocals.Count > 0)
            {
                // reuse a spare variable
                result = cc.SpareLocals.Pop();
            }
            else
            {
                // allocate a new variable with a unique name
                if (cc.Locals.ContainsKey(atom) || cc.TempLocalNames.Contains(atom))
                {
                    ZilAtom newAtom;
                    int num = 1;
                    do
                    {
                        name = atom.Text + "?" + num;
                        num++;
                        newAtom = ZilAtom.Parse(name, cc.Context);
                    } while (cc.Locals.ContainsKey(newAtom) || cc.TempLocalNames.Contains(newAtom));

                    cc.TempLocalNames.Add(newAtom);
                }
                else
                {
                    cc.TempLocalNames.Add(atom);
                }

                result = rb.DefineLocal(name);
            }

            cc.Locals[atom] = result;
            return result;
        }

        public static void PopInnerLocal(CompileCtx cc, ZilAtom atom)
        {
            Contract.Requires(cc != null);
            Contract.Requires(atom != null);

            cc.SpareLocals.Push(cc.Locals[atom]);

            Stack<ILocalBuilder> stk;
            if (cc.OuterLocals.TryGetValue(atom, out stk))
            {
                cc.Locals[atom] = stk.Pop();
                if (stk.Count == 0)
                    cc.OuterLocals.Remove(atom);
            }
            else
                cc.Locals.Remove(atom);
        }

        static bool IsNonVariableForm(ZilObject zo)
        {
            if (zo == null)
                return false;

            var form = zo as ZilForm;
            if (form == null)
                return false;

            var first = form.First as ZilAtom;
            if (first == null)
                return true;

            return first.StdAtom != StdAtom.GVAL && first.StdAtom != StdAtom.LVAL;
        }

        [SuppressMessage("Microsoft.Contracts", "TestAlwaysEvaluatingToAConstant", Justification = "block.Flags can be changed by other methods")]
        static IOperand CompileDO(CompileCtx cc, IRoutineBuilder rb, ZilList args, ISourceLine src,
            bool wantResult,
#pragma warning disable RECS0154 // Parameter is never used
            IVariable resultStorage)
#pragma warning restore RECS0154 // Parameter is never used
        {
            Contract.Requires(cc != null);
            Contract.Requires(rb != null);
            Contract.Requires(args != null);
            Contract.Requires(src != null);
            Contract.Ensures(Contract.Result<IOperand>() != null || !wantResult);

            // resultStorage is unused here for the same reason as in CompilePROG.

            // parse binding list
            if (args.First == null || args.First.GetTypeAtom(cc.Context).StdAtom != StdAtom.LIST)
            {
                throw new CompilerError(CompilerMessages.Expected_Binding_List_At_Start_Of_0, "DO");
            }

            var spec = (ZilList)args.First;
            var specLength = ((IStructure)spec).GetLength(4);
            if (specLength < 3 || specLength == null)
            {
                throw new CompilerError(
                    CompilerMessages._0_Expected_1_Element1s_In_Binding_List,
                    "DO",
                    new CountableString("3 or 4", true));
            }

            var atom = spec.First as ZilAtom;
            if (atom == null)
            {
                throw new CompilerError(CompilerMessages._0_1_Element_In_Binding_List_Must_Be_2, "DO", "first", "an atom");
            }

            var start = spec.Rest.First;
            var end = spec.Rest.Rest.First;

            // look for an end block
            var body = args.Rest;
            ZilList endStmts;
            if (body.First != null && body.First.GetTypeAtom(cc.Context).StdAtom == StdAtom.LIST)
            {
                endStmts = (ZilList)body.First;
                body = body.Rest;
            }
            else
            {
                endStmts = null;
            }

            // create block
            var block = new Block
            {
                AgainLabel = rb.DefineLabel(),
                ReturnLabel = rb.DefineLabel(),
                Flags = wantResult ? BlockFlags.WantResult : 0
            };

            cc.Blocks.Push(block);

            var exhaustedLabel = rb.DefineLabel();

            // initialize counter
            var counter = PushInnerLocal(cc, rb, atom);
            var operand = CompileAsOperand(cc, rb, start, src, counter);
            if (operand != counter)
                rb.EmitStore(counter, operand);

            rb.MarkLabel(block.AgainLabel);

            // test and branch before the body, if end is a (non-[GL]VAL) FORM
            bool testFirst;
            if (IsNonVariableForm(end))
            {
                CompileCondition(cc, rb, end, end.SourceLine, exhaustedLabel, true);
                testFirst = true;
            }
            else
            {
                testFirst = false;
            }

            // body
            while (body != null && !body.IsEmpty)
            {
                // ignore the results of all statements
                CompileStmt(cc, rb, body.First, false);
                body = body.Rest;
            }

            // increment
            bool down;
            if (specLength == 4)
            {
                var inc = spec.Rest.Rest.Rest.First;
                int incValue;

                if (inc is ZilFix && (incValue = ((ZilFix)inc).Value) < 0)
                {
                    rb.EmitBinary(BinaryOp.Sub, counter, cc.Game.MakeOperand(-incValue), counter);
                    down = true;
                }
                else if (IsNonVariableForm(inc))
                {
                    operand = CompileAsOperand(cc, rb, inc, src, counter);
                    if (operand != counter)
                        rb.EmitStore(counter, operand);
                    down = false;
                }
                else
                {
                    operand = CompileAsOperand(cc, rb, inc, src);
                    rb.EmitBinary(BinaryOp.Add, counter, operand, counter);
                    down = false;
                }
            }
            else
            {
                down = (start is ZilFix && end is ZilFix && ((ZilFix)end).Value < ((ZilFix)start).Value);
                rb.EmitBinary(down ? BinaryOp.Sub : BinaryOp.Add, counter, cc.Game.One, counter);
            }

            // test and branch after the body, if end is GVAL/LVAL or a constant
            if (!testFirst)
            {
                operand = CompileAsOperand(cc, rb, end, src);
                rb.Branch(down ? Condition.Less : Condition.Greater, counter, operand, block.AgainLabel, false);
            }
            else
            {
                rb.Branch(block.AgainLabel);
            }

            // exhausted label, end statements, provide a return value if we need one
            rb.MarkLabel(exhaustedLabel);

            while (endStmts != null && !endStmts.IsEmpty)
            {
                CompileStmt(cc, rb, endStmts.First, false);
                endStmts = endStmts.Rest;
            }

            if (wantResult)
                rb.EmitStore(rb.Stack, cc.Game.One);

            // clean up block and counter
            if ((block.Flags & BlockFlags.Returned) != 0)   // Code Contracts message is suppressed on this line (see attribute)
                rb.MarkLabel(block.ReturnLabel);

            PopInnerLocal(cc, atom);

            cc.Blocks.Pop();

            return wantResult ? rb.Stack : null;
        }

        static IOperand CompileMAP_CONTENTS(CompileCtx cc, IRoutineBuilder rb, ZilList args, ISourceLine src,
            bool wantResult,
#pragma warning disable RECS0154 // Parameter is never used
            IVariable resultStorage)
#pragma warning restore RECS0154 // Parameter is never used
        {
            Contract.Requires(cc != null);
            Contract.Requires(rb != null);
            Contract.Requires(args != null);
            Contract.Requires(src != null);
            Contract.Ensures(Contract.Result<IOperand>() != null || !wantResult);

            // parse binding list
            if (args.First == null || args.First.GetTypeAtom(cc.Context).StdAtom != StdAtom.LIST)
            {
                throw new CompilerError(CompilerMessages.Expected_Binding_List_At_Start_Of_0, "MAP-CONTENTS");
            }

            var spec = (ZilList)args.First;
            var specLength = ((IStructure)spec).GetLength(3);
            if (specLength < 2 || specLength == null)
            {
                throw new CompilerError(
                    CompilerMessages._0_Expected_1_Element1s_In_Binding_List,
                    "MAP-CONTENTS",
                    new CountableString("2 or 3", true));
            }

            var atom = spec.First as ZilAtom;
            if (atom == null)
            {
                throw new CompilerError(CompilerMessages._0_1_Element_In_Binding_List_Must_Be_2, "MAP-CONTENTS", "first", "an atom");
            }

            ZilAtom nextAtom;
            ZilObject container;
            if (specLength == 3)
            {
                nextAtom = spec.Rest.First as ZilAtom;
                if (nextAtom == null)
                {
                    throw new CompilerError(CompilerMessages._0_1_Element_In_Binding_List_Must_Be_2, "MAP-CONTENTS", "middle", "an atom");
                }

                container = spec.Rest.Rest.First;
            }
            else
            {
                nextAtom = null;
                container = spec.Rest.First;
            }
            Contract.Assume(container != null);

            // look for an end block
            var body = args.Rest;
            ZilList endStmts;
            if (body.First != null && body.First.GetTypeAtom(cc.Context).StdAtom == StdAtom.LIST)
            {
                endStmts = (ZilList)body.First;
                body = body.Rest;
            }
            else
            {
                endStmts = null;
            }

            // create block
            var block = new Block
            {
                AgainLabel = rb.DefineLabel(),
                ReturnLabel = rb.DefineLabel(),
                Flags = wantResult ? BlockFlags.WantResult : 0
            };

            cc.Blocks.Push(block);

            var exhaustedLabel = rb.DefineLabel();

            // initialize counter
            var counter = PushInnerLocal(cc, rb, atom);
            var operand = CompileAsOperand(cc, rb, container, src);
            rb.EmitGetChild(operand, counter, exhaustedLabel, false);

            rb.MarkLabel(block.AgainLabel);

            // loop over the objects using one or two variables
            if (nextAtom != null)
            {
                // initialize next
                var next = PushInnerLocal(cc, rb, nextAtom);
                var tempLabel = rb.DefineLabel();
                rb.EmitGetSibling(counter, next, tempLabel, true);
                rb.MarkLabel(tempLabel);

                // body
                while (body != null && !body.IsEmpty)
                {
                    // ignore the results of all statements
                    CompileStmt(cc, rb, body.First, false);
                    body = body.Rest;
                }

                // next object
                rb.EmitStore(counter, next);
                rb.BranchIfZero(counter, block.AgainLabel, false);

                // clean up next
                PopInnerLocal(cc, nextAtom);
            }
            else
            {
                // body
                while (body != null && !body.IsEmpty)
                {
                    // ignore the results of all statements
                    CompileStmt(cc, rb, body.First, false);
                    body = body.Rest;
                }

                // next object
                rb.EmitGetSibling(counter, counter, block.AgainLabel, true);
            }

            // exhausted label, end statements, provide a return value if we need one
            rb.MarkLabel(exhaustedLabel);

            while (endStmts != null && !endStmts.IsEmpty)
            {
                CompileStmt(cc, rb, endStmts.First, false);
                endStmts = endStmts.Rest;
            }

            if (wantResult)
                rb.EmitStore(rb.Stack, cc.Game.One);

            // clean up block and counter
            rb.MarkLabel(block.ReturnLabel);

            PopInnerLocal(cc, atom);

            cc.Blocks.Pop();

            return wantResult ? rb.Stack : null;
        }

        static IOperand CompileMAP_DIRECTIONS(CompileCtx cc, IRoutineBuilder rb, ZilList args, ISourceLine src,
            bool wantResult,
#pragma warning disable RECS0154 // Parameter is never used
            IVariable resultStorage)
#pragma warning restore RECS0154 // Parameter is never used
        {
            Contract.Requires(cc != null);
            Contract.Requires(rb != null);
            Contract.Requires(args != null);
            Contract.Requires(src != null);
            Contract.Ensures(Contract.Result<IOperand>() != null || !wantResult);

            // parse binding list
            if (args.First == null || args.First.GetTypeAtom(cc.Context).StdAtom != StdAtom.LIST)
            {
                throw new CompilerError(CompilerMessages.Expected_Binding_List_At_Start_Of_0, "MAP-DIRECTIONS");
            }

            var spec = (ZilList)args.First;
            var specLength = ((IStructure)spec).GetLength(3);
            if (specLength != 3)
            {
                throw new CompilerError(CompilerMessages._0_Expected_1_Element1s_In_Binding_List, "MAP-DIRECTIONS", 3);
            }

            var dirAtom = spec.First as ZilAtom;
            if (dirAtom == null)
            {
                throw new CompilerError(CompilerMessages._0_1_Element_In_Binding_List_Must_Be_2, "MAP-DIRECTIONS", "first", "an atom");
            }

            var ptAtom = spec.Rest.First as ZilAtom;
            if (ptAtom == null)
            {
                throw new CompilerError(CompilerMessages._0_1_Element_In_Binding_List_Must_Be_2, "MAP-DIRECTIONS", "middle", "an atom");
            }

            var room = spec.Rest.Rest.First;
            if (!room.IsLVAL() && !room.IsGVAL())
            {
                throw new CompilerError(CompilerMessages._0_1_Element_In_Binding_List_Must_Be_2, "MAP-DIRECTIONS", "last", "an LVAL or GVAL");
            }

            // look for an end block
            var body = args.Rest;
            ZilList endStmts;
            if (body.First != null && body.First.GetTypeAtom(cc.Context).StdAtom == StdAtom.LIST)
            {
                endStmts = (ZilList)body.First;
                body = body.Rest;
            }
            else
            {
                endStmts = null;
            }

            // create block
            var block = new Block
            {
                AgainLabel = rb.DefineLabel(),
                ReturnLabel = rb.DefineLabel(),
                Flags = wantResult ? BlockFlags.WantResult : 0
            };

            cc.Blocks.Push(block);

            var exhaustedLabel = rb.DefineLabel();

            // initialize counter
            var counter = PushInnerLocal(cc, rb, dirAtom);
            rb.EmitStore(counter, cc.Game.MakeOperand(cc.Game.MaxProperties + 1));

            rb.MarkLabel(block.AgainLabel);

            rb.Branch(Condition.DecCheck, counter,
                cc.Constants[cc.Context.GetStdAtom(StdAtom.LOW_DIRECTION)], exhaustedLabel, true);

            var propTable = PushInnerLocal(cc, rb, ptAtom);
            var roomOperand = CompileAsOperand(cc, rb, room, src);
            rb.EmitBinary(BinaryOp.GetPropAddress, roomOperand, counter, propTable);
            rb.BranchIfZero(propTable, block.AgainLabel, true);

            // body
            while (body != null && !body.IsEmpty)
            {
                // ignore the results of all statements
                CompileStmt(cc, rb, body.First, false);
                body = body.Rest;
            }

            // loop
            rb.Branch(block.AgainLabel);

            // end statements
            while (endStmts != null && !endStmts.IsEmpty)
            {
                CompileStmt(cc, rb, endStmts.First, false);
                endStmts = endStmts.Rest;
            }

            // exhausted label, provide a return value if we need one
            rb.MarkLabel(exhaustedLabel);
            if (wantResult)
                rb.EmitStore(rb.Stack, cc.Game.One);

            // clean up block and variables
            rb.MarkLabel(block.ReturnLabel);

            PopInnerLocal(cc, ptAtom);
            PopInnerLocal(cc, dirAtom);

            cc.Blocks.Pop();

            return wantResult ? rb.Stack : null;
        }

        static IOperand CompileCOND(CompileCtx cc, IRoutineBuilder rb, ZilList clauses,
            ISourceLine src, bool wantResult, IVariable resultStorage)
        {
            Contract.Requires(cc != null);
            Contract.Requires(rb != null);
            Contract.Requires(clauses != null);
            Contract.Requires(src != null);
            Contract.Ensures(Contract.Result<IOperand>() != null || !wantResult);

            var nextLabel = rb.DefineLabel();
            var endLabel = rb.DefineLabel();
            bool elsePart = false;

            if (resultStorage == null)
                resultStorage = rb.Stack;

            Contract.Assert(resultStorage != null);

            while (!clauses.IsEmpty)
            {
                var clause = clauses.First as ZilList;
                clauses = clauses.Rest as ZilList;

                if (clause is ZilForm)
                {
                    // a macro call returning a list or false
                    var newClause = clause.Expand(cc.Context);

                    if (newClause is ZilFalse)
                        continue;

                    clause = newClause as ZilList;
                }

                if (clause == null || clause.GetTypeAtom(cc.Context).StdAtom != StdAtom.LIST)
                    throw new CompilerError(CompilerMessages.All_Clauses_In_0_Must_Be_Lists, "COND");

                ZilObject condition = clause.First;

                // if condition is always true (i.e. not a FORM or a FALSE), this is the "else" part
                switch (condition.GetTypeAtom(cc.Context).StdAtom)
                {
                    case StdAtom.FORM:
                        // must be evaluated
                        MarkSequencePoint(cc, rb, condition);
                        CompileCondition(cc, rb, condition, condition.SourceLine, nextLabel, false);
                        break;

                    case StdAtom.FALSE:
                        // never true
                        // TODO: warning message? clause will never be evaluated
                        continue;

                    default:
                        // always true
                        // TODO: warn if not T or ELSE?
                        elsePart = true;
                        break;
                }

                // emit code for clause
                clause = clause.Rest as ZilList;
                var clauseResult = CompileClauseBody(cc, rb, clause, wantResult, resultStorage);
                if (wantResult && clauseResult != resultStorage)
                    rb.EmitStore(resultStorage, clauseResult);

                // jump to end
                if (!clauses.IsEmpty || (wantResult && !elsePart))
                    rb.Branch(endLabel);

                rb.MarkLabel(nextLabel);

                if (elsePart)
                {
                    if (!clauses.IsEmpty)
                    {
                        cc.Context.HandleWarning(new CompilerError(src, CompilerMessages._0_Clauses_After_Else_Part_Will_Never_Be_Evaluated, "COND"));
                    }

                    break;
                }

                nextLabel = rb.DefineLabel();
            }

            if (wantResult && !elsePart)
                rb.EmitStore(resultStorage, cc.Game.Zero);

            rb.MarkLabel(endLabel);
            return wantResult ? resultStorage : null;
        }

        static IOperand CompileClauseBody(CompileCtx cc, IRoutineBuilder rb, ZilList clause, bool wantResult, IVariable resultStorage)
        {
            Contract.Requires(cc != null);
            Contract.Requires(rb != null);
            Contract.Requires(clause != null);
            Contract.Requires(resultStorage != null || !wantResult);
            Contract.Ensures(Contract.Result<IOperand>() != null || !Contract.OldValue(wantResult));

            if (clause.IsEmpty)
                return cc.Game.One;

            do
            {
                // only want the result of the last statement (if any)
                bool wantThisResult = wantResult && clause.Rest.IsEmpty;
                var stmt = clause.First;
                if (stmt is ZilAdecl)
                    stmt = ((ZilAdecl)stmt).First;
                var form = stmt as ZilForm;
                IOperand result;
                if (form != null)
                {
                    MarkSequencePoint(cc, rb, form);

                    result = CompileForm(cc, rb, form,
                        wantThisResult,
                        wantThisResult ? resultStorage : null);
                    if (wantThisResult && result != resultStorage)
                        rb.EmitStore(resultStorage, result);
                }
                else if (wantThisResult)
                {
                    result = CompileConstant(cc, stmt);
                    if (result == null)
                        throw new CompilerError(stmt, CompilerMessages.Expressions_Of_This_Type_Cannot_Be_Compiled);

                    rb.EmitStore(resultStorage, result);
                }

                clause = clause.Rest as ZilList;
            } while (!clause.IsEmpty);

            return resultStorage;
        }

        static IOperand CompileVERSION_P(CompileCtx cc, IRoutineBuilder rb, ZilList clauses,
            ISourceLine src, bool wantResult, IVariable resultStorage)
        {
            Contract.Requires(cc != null);
            Contract.Requires(rb != null);
            Contract.Requires(clauses != null);
            Contract.Requires(src != null);
            Contract.Ensures(Contract.Result<IOperand>() != null || !wantResult);

            if (resultStorage == null)
                resultStorage = rb.Stack;

            Contract.Assert(resultStorage != null);

            while (!clauses.IsEmpty)
            {
                var clause = clauses.First as ZilList;
                clauses = clauses.Rest;

                if (clause == null || clause.GetTypeAtom(cc.Context).StdAtom != StdAtom.LIST)
                    throw new CompilerError(CompilerMessages.All_Clauses_In_0_Must_Be_Lists, "VERSION?");

                ZilObject condition = clause.First;

                // check version condition
                int condVersion;
                switch (condition.GetTypeAtom(cc.Context).StdAtom)
                {
                    case StdAtom.ATOM:
                        switch (((ZilAtom)condition).StdAtom)
                        {
                            case StdAtom.ZIP:
                                condVersion = 3;
                                break;
                            case StdAtom.EZIP:
                                condVersion = 4;
                                break;
                            case StdAtom.XZIP:
                                condVersion = 5;
                                break;
                            case StdAtom.YZIP:
                                condVersion = 6;
                                break;
                            case StdAtom.ELSE:
                            case StdAtom.T:
                                condVersion = 0;
                                break;
                            default:
                                throw new CompilerError(CompilerMessages.Unrecognized_Atom_In_VERSION_Must_Be_ZIP_EZIP_XZIP_YZIP_ELSET);
                        }
                        break;

                    case StdAtom.FIX:
                        condVersion = ((ZilFix)condition).Value;
                        if (condVersion < 3 || condVersion > 8)
                            throw new CompilerError(CompilerMessages.Version_Number_Out_Of_Range_Must_Be_38);
                        break;

                    default:
                        throw new CompilerError(CompilerMessages.Conditions_In_In_VERSION_Clauses_Must_Be_ATOMs);
                }

                // does this clause match?
                if (condVersion == cc.Context.ZEnvironment.ZVersion || condVersion == 0)
                {
                    // emit code for clause
                    clause = clause.Rest;
                    var clauseResult = CompileClauseBody(cc, rb, clause, wantResult, resultStorage);

                    if (condVersion == 0 && !clauses.IsEmpty)
                    {
                        cc.Context.HandleWarning(new CompilerError(src, CompilerMessages._0_Clauses_After_Else_Part_Will_Never_Be_Evaluated, "VERSION?"));
                    }

                    return wantResult ? clauseResult : null;
                }
            }

            // no matching clauses
            if (wantResult)
                rb.EmitStore(resultStorage, cc.Game.Zero);

            return wantResult ? resultStorage : null;
        }

        static IOperand CompileIFFLAG(CompileCtx cc, IRoutineBuilder rb, ZilList clauses,
            ISourceLine src, bool wantResult, IVariable resultStorage)
        {
            Contract.Requires(cc != null);
            Contract.Requires(rb != null);
            Contract.Requires(clauses != null);
            Contract.Requires(src != null);
            Contract.Ensures(Contract.Result<IOperand>() != null || !wantResult);

            if (resultStorage == null)
                resultStorage = rb.Stack;

            Contract.Assert(resultStorage != null);

            while (!clauses.IsEmpty)
            {
                var clause = clauses.First as ZilList;
                clauses = clauses.Rest as ZilList;

                if (clause == null || clause.GetTypeAtom(cc.Context).StdAtom != StdAtom.LIST)
                    throw new CompilerError(CompilerMessages.All_Clauses_In_0_Must_Be_Lists, "IFFLAG");

                ZilAtom atom;
                ZilString str;
                ZilForm form;
                ZilObject value;
                bool match, isElse = false;
                if (((atom = clause.First as ZilAtom) != null &&
                     (value = cc.Context.GetCompilationFlagValue(atom)) != null) ||
                    ((str = clause.First as ZilString) != null &&
                     (value = cc.Context.GetCompilationFlagValue(str.Text)) != null))
                {
                    // name of a defined compilation flag
                    match = value.IsTrue;
                }
                else if ((form = clause.First as ZilForm) != null)
                {
                    form = Subrs.SubstituteIfflagForm(cc.Context, form);
                    match = form.Eval(cc.Context).IsTrue;
                }
                else
                {
                    match = isElse = true;
                }

                // does this clause match?
                if (match)
                {
                    // emit code for clause
                    clause = clause.Rest;
                    var clauseResult = CompileClauseBody(cc, rb, clause, wantResult, resultStorage);

                    if (isElse && !clauses.IsEmpty)
                    {
                        cc.Context.HandleWarning(new CompilerError(src, CompilerMessages._0_Clauses_After_Else_Part_Will_Never_Be_Evaluated, "IFFLAG"));
                    }

                    return wantResult ? clauseResult : null;
                }
            }

            // no matching clauses
            if (wantResult)
                rb.EmitStore(resultStorage, cc.Game.Zero);

            return wantResult ? resultStorage : null;
        }

        static IOperand CompileImpromptuTable(CompileCtx cc, IRoutineBuilder rb, ZilForm form,
            bool wantResult,
#pragma warning disable RECS0154 // Parameter is never used
            IVariable resultStorage)
#pragma warning restore RECS0154 // Parameter is never used
        {
            Contract.Requires(cc != null);
            Contract.Requires(rb != null);
            Contract.Requires(form != null);

            var type = ((ZilAtom)form.First).StdAtom;
            var args = form.Rest;

            var table = (ZilTable)form.Eval(cc.Context);

            var tableBuilder = cc.Game.DefineTable(table.Name, (table.Flags & TableFlags.Pure) != 0);
            cc.Tables.Add(table, tableBuilder);
            return wantResult ? tableBuilder : null;
        }

        static IOperand CompileTell(CompileCtx cc, IRoutineBuilder rb, ZilForm form)
        {
            Contract.Requires(cc != null);
            Contract.Requires(rb != null);
            Contract.Requires(form != null);
            Contract.Requires(form.SourceLine != null);
            Contract.Ensures(Contract.Result<IOperand>() != null);

            var args = form.Rest.ToArray();

            int index = 0;
            while (index < args.Length)
            {
                // look for a matching pattern
                bool handled = false;
                foreach (var pattern in cc.Context.ZEnvironment.TellPatterns)
                {
                    var result = pattern.Match(args, index, cc.Context, form.SourceLine);
                    if (result.Matched)
                    {
                        CompileForm(cc, rb, result.Output, false, null);
                        index += pattern.Length;
                        handled = true;
                        break;
                    }
                }

                if (handled)
                    continue;

                // literal string -> PRINTI
                if (args[index] is ZilString)
                {
                    rb.EmitPrint(TranslateString(((ZilString)args[index]).Text, cc.Context), false);
                    index++;
                    continue;
                }

                // literal character -> PRINTC
                if (args[index] is ZilChar)
                {
                    rb.EmitPrint(PrintOp.Character, cc.Game.MakeOperand((int)((ZilChar)args[index]).Char));
                    index++;
                    continue;
                }

                // <QUOTE foo> -> <PRINTD ,foo>
                if (args[index] is ZilForm)
                {
                    var innerForm = (ZilForm)args[index];
                    if (innerForm.First is ZilAtom && ((ZilAtom)innerForm.First).StdAtom == StdAtom.QUOTE && innerForm.Rest != null)
                    {
                        var transformed = new ZilForm(new ZilObject[] {
                            cc.Context.GetStdAtom(StdAtom.GVAL),
                            innerForm.Rest.First
                        })
                        { SourceLine = form.SourceLine };
                        var obj = CompileAsOperand(cc, rb, transformed, innerForm.SourceLine);
                        rb.EmitPrint(PrintOp.Object, obj);
                        index++;
                        continue;
                    }
                }

                // P?foo expr -> <PRINT <GETP expr ,P?foo>>
                if (args[index] is ZilAtom && index + 1 < args.Length)
                {
                    var transformed = new ZilForm(new ZilObject[] {
                        cc.Context.GetStdAtom(StdAtom.PRINT),
                        new ZilForm(new ZilObject[] {
                            cc.Context.GetStdAtom(StdAtom.GETP),
                            args[index+1],
                            new ZilForm(new ZilObject[] {
                                cc.Context.GetStdAtom(StdAtom.GVAL),
                                args[index]
                            }) { SourceLine = form.SourceLine }
                        }) { SourceLine = form.SourceLine }
                    })
                    { SourceLine = form.SourceLine };
                    CompileForm(cc, rb, transformed, false, null);
                    index += 2;
                    continue;
                }

                // otherwise, treat it as a packed string
                var str = CompileAsOperand(cc, rb, args[index], args[index].SourceLine ?? form.SourceLine);
                rb.EmitPrint(PrintOp.PackedAddr, str);
                index++;
                continue;
            }

            return cc.Game.One;
        }

        /// <summary>
        /// Contains unique atoms used as special values in <see cref="PreBuildObject(CompileCtx, ZilModelObject)"/>.
        /// </summary>
        /// <remarks>
        /// Since DESC and IN (or LOC) can be used as property names when the property definition
        /// matches the direction pattern (most commonly seen with IN), these atoms are used to separately
        /// track whether the names have been used as properties and/or pseudo-properties.
        /// </remarks>
        static class PseudoPropertyAtoms
        {
            public static readonly ZilAtom Desc = new ZilAtom("?DESC?", null, StdAtom.None);
            public static readonly ZilAtom Location = new ZilAtom("?IN/LOC?", null, StdAtom.None);
        }

        static void PreBuildObject(CompileCtx cc, ZilModelObject model)
        {
            Contract.Requires(cc != null);
            Contract.Requires(model != null);

            var globalsByName = cc.Context.ZEnvironment.Globals.ToDictionary(g => g.Name);
            var propertiesSoFar = new HashSet<ZilAtom>();

            var preBuilders = new ComplexPropDef.ElementPreBuilders
            {
                CreateVocabWord = (atom, partOfSpeech, src) =>
                {
                    IWord word;

                    switch (partOfSpeech.StdAtom)
                    {
                        case StdAtom.ADJ:
                        case StdAtom.ADJECTIVE:
                            word = cc.Context.ZEnvironment.GetVocabAdjective(atom, src);
                            break;

                        case StdAtom.NOUN:
                        case StdAtom.OBJECT:
                            word = cc.Context.ZEnvironment.GetVocabNoun(atom, src);
                            break;

                        case StdAtom.BUZZ:
                            word = cc.Context.ZEnvironment.GetVocabBuzzword(atom, src);
                            break;

                        case StdAtom.PREP:
                            word = cc.Context.ZEnvironment.GetVocabPreposition(atom, src);
                            break;

                        case StdAtom.DIR:
                            word = cc.Context.ZEnvironment.GetVocabDirection(atom, src);
                            break;

                        case StdAtom.VERB:
                            word = cc.Context.ZEnvironment.GetVocabVerb(atom, src);
                            break;

                        default:
                            cc.Context.HandleError(new CompilerError(model, CompilerMessages.Unrecognized_0_1, "part of speech", partOfSpeech));
                            break;
                    }
                },

                ReserveGlobal = atom =>
                {
                    ZilGlobal g;
                    if (globalsByName.TryGetValue(atom, out g))
                        g.StorageType = GlobalStorageType.Hard;
                }
            };

            // for detecting implicitly defined directions
            var directionPattern = cc.Context.GetProp(
                cc.Context.GetStdAtom(StdAtom.DIRECTIONS), cc.Context.GetStdAtom(StdAtom.PROPSPEC)) as ComplexPropDef;

            // create property builders for all properties on this object as needed,
            // and set up P?FOO constants for them. also create vocabulary words for 
            // SYNONYM and ADJECTIVE property values, and constants for FLAGS values.
            foreach (ZilList prop in model.Properties)
            {
                using (DiagnosticContext.Push(prop.SourceLine))
                {
                    // the first element must be an atom identifying the property
                    var atom = prop.First as ZilAtom;
                    if (atom == null)
                    {
                        cc.Context.HandleError(new CompilerError(model, CompilerMessages.Property_Specification_Must_Start_With_An_Atom));
                        continue;
                    }

                    ZilAtom uniquePropertyName;

                    // exclude phony built-in properties
                    /* we also detect directions here, which are tricky for a few reasons:
                     * - they can be implicitly defined by a property spec that looks sufficiently direction-like
                     * - (IN ROOMS) is not a direction, even if IN is explicitly defined as a direction -- but (IN "string") is!
                     * - (FOO BAR) is not enough to implicitly define FOO as a direction, even if (DIR R:ROOM)
                     *   is a pattern for directions
                     */
                    bool phony;
                    bool? isSynonym = null;
                    Synonym synonym = null;
                    var definedDirection = cc.Context.ZEnvironment.Directions.Contains(atom);

                    if (prop.Rest != null && prop.Rest.Rest != null &&
                        (!prop.Rest.Rest.IsEmpty ||
                         (definedDirection && !(prop.Rest.First is ZilAtom))) &&
                        (definedDirection ||
                         (directionPattern != null && directionPattern.Matches(cc.Context, prop))))
                    {
                        // it's a direction
                        phony = false;

                        // could be a new implicitly defined direction
                        if (!cc.Context.ZEnvironment.Directions.Contains(atom))
                        {
                            synonym = cc.Context.ZEnvironment.Synonyms.FirstOrDefault(s => s.SynonymWord.Atom == atom);

                            if (synonym == null)
                            {
                                isSynonym = false;
                                cc.Context.ZEnvironment.Directions.Add(atom);
                                cc.Context.ZEnvironment.GetVocabDirection(atom, prop.SourceLine);
                                if (directionPattern != null)
                                    cc.Context.SetPropDef(atom, directionPattern);
                                uniquePropertyName = atom;
                            }
                            else
                            {
                                isSynonym = true;
                                uniquePropertyName = synonym.OriginalWord.Atom;
                            }
                        }
                        else
                        {
                            uniquePropertyName = atom;
                        }
                    }
                    else
                    {
                        switch (atom.StdAtom)
                        {
                            case StdAtom.DESC:
                                phony = true;
                                uniquePropertyName = PseudoPropertyAtoms.Desc;
                                break;
                            case StdAtom.IN:
                            case StdAtom.LOC:
                                phony = true;
                                uniquePropertyName = PseudoPropertyAtoms.Location;
                                break;
                            case StdAtom.FLAGS:
                                phony = true;
                                // multiple FLAGS definitions are OK
                                uniquePropertyName = null;
                                break;
                            default:
                                phony = false;
                                uniquePropertyName = atom;
                                break;
                        }
                    }

                    if (uniquePropertyName != null)
                    {
                        if (propertiesSoFar.Contains(uniquePropertyName))
                        {
                            cc.Context.HandleError(new CompilerError(
                                prop,
                                CompilerMessages.Duplicate_0_Definition_1,
                                phony ? "pseudo-property" : "property",
                                atom.ToStringContext(cc.Context, false)));
                        }
                        else
                        {
                            propertiesSoFar.Add(uniquePropertyName);
                        }
                    }

                    if (!phony && !cc.Properties.ContainsKey(atom))
                    {
                        if (isSynonym == null)
                        {
                            synonym = cc.Context.ZEnvironment.Synonyms.FirstOrDefault(s => s.SynonymWord.Atom == atom);
                            isSynonym = (synonym != null);
                        }

                        if ((bool)isSynonym)
                        {
                            IPropertyBuilder origPb;
                            var origAtom = synonym.OriginalWord.Atom;
                            if (cc.Properties.TryGetValue(origAtom, out origPb) == false)
                            {
                                DefineProperty(cc, origAtom);
                                origPb = cc.Properties[origAtom];
                            }
                            cc.Properties.Add(atom, origPb);

                            var pAtom = ZilAtom.Parse("P?" + atom, cc.Context);
                            cc.Constants.Add(pAtom, origPb);

                            var origSpec = cc.Context.GetProp(origAtom, cc.Context.GetStdAtom(StdAtom.PROPSPEC));
                            cc.Context.PutProp(atom, cc.Context.GetStdAtom(StdAtom.PROPSPEC), origSpec);
                        }
                        else
                        {
                            DefineProperty(cc, atom);
                        }
                    }

                    // check for a PROPSPEC
                    var propspec = cc.Context.GetProp(atom, cc.Context.GetStdAtom(StdAtom.PROPSPEC));
                    if (propspec != null)
                    {
                        var complexDef = propspec as ComplexPropDef;
                        if (complexDef != null)
                        {
                            // PROPDEF pattern
                            if (complexDef.Matches(cc.Context, prop))
                            {
                                complexDef.PreBuildProperty(cc.Context, prop, preBuilders);
                            }
                        }
                        else
                        {
                            // name of a custom property builder function
                            var form = new ZilForm(new ZilObject[] { propspec, prop }) { SourceLine = prop.SourceLine };
                            var specOutput = form.Eval(cc.Context);
                            ZilList propBody;
                            if (specOutput.GetTypeAtom(cc.Context).StdAtom != StdAtom.LIST ||
                                (propBody = ((ZilList)specOutput).Rest) == null || propBody.IsEmpty)
                            {
                                cc.Context.HandleError(new CompilerError(model, CompilerMessages.PROPSPEC_For_Property_0_Returned_A_Bad_Value_1, atom, specOutput));
                                continue;
                            }

                            // replace the property body with the propspec's output
                            prop.Rest = propBody;
                        }
                    }
                    else
                    {
                        switch (atom.StdAtom)
                        {
                            case StdAtom.SYNONYM:
                                foreach (ZilObject obj in prop.Rest)
                                {
                                    atom = obj as ZilAtom;
                                    if (atom == null)
                                        continue;

                                    try
                                    {
                                        var word = cc.Context.ZEnvironment.GetVocabNoun(atom, prop.SourceLine);
                                    }
                                    catch (ZilError ex)
                                    {
                                        cc.Context.HandleError(ex);
                                    }
                                }
                                break;

                            case StdAtom.ADJECTIVE:
                                foreach (ZilObject obj in prop.Rest)
                                {
                                    atom = obj as ZilAtom;
                                    if (atom == null)
                                        continue;

                                    try
                                    {
                                        var word = cc.Context.ZEnvironment.GetVocabAdjective(atom, prop.SourceLine);
                                    }
                                    catch (ZilError ex)
                                    {
                                        cc.Context.HandleError(ex);
                                    }
                                }
                                break;

                            case StdAtom.PSEUDO:
                                foreach (ZilObject obj in prop.Rest)
                                {
                                    var str = obj as ZilString;
                                    if (str == null)
                                        continue;

                                    try
                                    {
                                        var word = cc.Context.ZEnvironment.GetVocabNoun(ZilAtom.Parse(str.Text, cc.Context), prop.SourceLine);
                                    }
                                    catch (ZilError ex)
                                    {
                                        cc.Context.HandleError(ex);
                                    }
                                }
                                break;

                            case StdAtom.FLAGS:
                                foreach (ZilObject obj in prop.Rest)
                                {
                                    atom = obj as ZilAtom;
                                    if (atom == null)
                                        continue;

                                    try
                                    {
                                        ZilAtom original;
                                        if (cc.Context.ZEnvironment.TryGetBitSynonym(atom, out original))
                                        {
                                            DefineFlag(cc, original);
                                        }
                                        else
                                        {
                                            DefineFlag(cc, atom);
                                        }
                                    }
                                    catch (ZilError ex)
                                    {
                                        cc.Context.HandleError(ex);
                                    }
                                }
                                break;
                        }
                    }
                }
            }
        }

        static void BuildObject(CompileCtx cc, ZilModelObject model, IObjectBuilder ob)
        {
            Contract.Requires(cc != null);
            Contract.Requires(model != null);
            Contract.Requires(ob != null);

            var elementConverters = new ComplexPropDef.ElementConverters
            {
                CompileConstant = zo => CompileConstant(cc, zo),

                GetAdjectiveValue = (atom, src) =>
                {
                    var word = cc.Context.ZEnvironment.GetVocabAdjective(atom, src);
                    if (cc.Context.ZEnvironment.ZVersion == 3)
                    {
                        return cc.Constants[ZilAtom.Parse("A?" + word.Atom, cc.Context)];
                    }
                    return cc.Vocabulary[word];
                },

                GetGlobalNumber = atom => cc.Globals[atom],

                GetVocabWord = (atom, partOfSpeech, src) =>
                {
                    IWord word;

                    switch (partOfSpeech.StdAtom)
                    {
                        case StdAtom.ADJ:
                        case StdAtom.ADJECTIVE:
                            word = cc.Context.ZEnvironment.GetVocabAdjective(atom, src);
                            break;

                        case StdAtom.NOUN:
                        case StdAtom.OBJECT:
                            word = cc.Context.ZEnvironment.GetVocabNoun(atom, src);
                            break;

                        case StdAtom.BUZZ:
                            word = cc.Context.ZEnvironment.GetVocabBuzzword(atom, src);
                            break;

                        case StdAtom.PREP:
                            word = cc.Context.ZEnvironment.GetVocabPreposition(atom, src);
                            break;

                        case StdAtom.DIR:
                            word = cc.Context.ZEnvironment.GetVocabDirection(atom, src);
                            break;

                        case StdAtom.VERB:
                            word = cc.Context.ZEnvironment.GetVocabVerb(atom, src);
                            break;

                        default:
                            cc.Context.HandleError(new CompilerError(model, CompilerMessages.Unrecognized_0_1, "part of speech", partOfSpeech));
                            return cc.Game.Zero;
                    }

                    return cc.Vocabulary[word];
                }
            };

            foreach (ZilList prop in model.Properties)
            {
                IPropertyBuilder pb;
                ITableBuilder tb;
                int length = 0;

                bool noSpecialCases = false;

                // the first element must be an atom identifying the property
                var propName = prop.First as ZilAtom;
                ZilList propBody = prop.Rest;
                if (propName == null)
                {
                    cc.Context.HandleError(new CompilerError(model, CompilerMessages.Property_Specification_Must_Start_With_An_Atom));
                    continue;
                }

                // check for IN/LOC, which can take precedence over PROPSPEC
                ZilObject value = propBody.First;
                if (propName.StdAtom == StdAtom.LOC ||
                    (propName.StdAtom == StdAtom.IN && ((IStructure)propBody).GetLength(1) == 1) && value is ZilAtom)
                {
                    var valueAtom = value as ZilAtom;
                    if (valueAtom == null)
                    {
                        cc.Context.HandleError(new CompilerError(model, CompilerMessages.Value_For_0_Property_Must_Be_1, propName, "an atom"));
                        continue;
                    }
                    IObjectBuilder parent;
                    if (cc.Objects.TryGetValue(valueAtom, out parent) == false)
                    {
                        cc.Context.HandleError(new CompilerError(
                            model,
                            CompilerMessages.No_Such_Object_0,
                            valueAtom.ToString()));
                        continue;
                    }
                    ob.Parent = parent;
                    ob.Sibling = parent.Child;
                    parent.Child = ob;
                    continue;
                }

                // check for a PUTPROP giving a PROPDEF pattern or hand-coded property builder
                var propspec = cc.Context.GetProp(propName, cc.Context.GetStdAtom(StdAtom.PROPSPEC));
                if (propspec != null)
                {
                    var complexDef = propspec as ComplexPropDef;
                    if (complexDef != null)
                    {
                        // PROPDEF pattern
                        if (complexDef.Matches(cc.Context, prop))
                        {
                            tb = ob.AddComplexProperty(cc.Properties[propName]);
                            complexDef.BuildProperty(cc.Context, prop, tb, elementConverters);
                            continue;
                        }
                    }
                    else
                    {
                        // name of a custom property builder function
                        // PreBuildObject already called the function and replaced the property body
                        noSpecialCases = true;
                    }
                }

                // built-in property builder, so at least one value has to follow the atom (except for FLAGS)
                if (value == null)
                {
                    if (propName.StdAtom != StdAtom.FLAGS)
                        cc.Context.HandleError(new CompilerError(model, CompilerMessages.Property_Has_No_Value_0, propName.ToString()));
                    continue;
                }

                // check for special cases
                bool handled = false;
                if (!noSpecialCases)
                {
                    switch (propName.StdAtom)
                    {
                        case StdAtom.DESC:
                            handled = true;
                            if (value.GetTypeAtom(cc.Context).StdAtom != StdAtom.STRING)
                            {
                                cc.Context.HandleError(new CompilerError(model, CompilerMessages.Value_For_0_Property_Must_Be_1, propName, "a STRING"));
                                continue;
                            }
                            ob.DescriptiveName = value.ToStringContext(cc.Context, true);
                            continue;

                        case StdAtom.FLAGS:
                            handled = true;
                            foreach (ZilObject obj in propBody)
                            {
                                var atom = obj as ZilAtom;
                                if (atom == null)
                                {
                                    cc.Context.HandleError(new CompilerError(model, CompilerMessages.Values_For_0_Property_Must_Be_1, propName, "atoms"));
                                    break;
                                }

                                ZilAtom original;
                                if (cc.Context.ZEnvironment.TryGetBitSynonym(atom, out original))
                                    atom = original;

                                IFlagBuilder fb = cc.Flags[atom];
                                ob.AddFlag(fb);
                            }
                            continue;

                        case StdAtom.SYNONYM:
                            handled = true;
                            tb = ob.AddComplexProperty(cc.Properties[propName]);
                            foreach (ZilObject obj in propBody)
                            {
                                var atom = obj as ZilAtom;
                                if (atom == null)
                                {
                                    cc.Context.HandleError(new CompilerError(model, CompilerMessages.Values_For_0_Property_Must_Be_1, propName, "atoms"));
                                    break;
                                }

                                var word = cc.Context.ZEnvironment.GetVocabNoun(atom, prop.SourceLine);
                                IWordBuilder wb = cc.Vocabulary[word];
                                tb.AddShort(wb);
                                length += 2;
                            }
                            break;

                        case StdAtom.ADJECTIVE:
                            handled = true;
                            tb = ob.AddComplexProperty(cc.Properties[propName]);
                            foreach (ZilObject obj in propBody)
                            {
                                var atom = obj as ZilAtom;
                                if (atom == null)
                                {
                                    cc.Context.HandleError(new CompilerError(model, CompilerMessages.Values_For_0_Property_Must_Be_1, propName, "atoms"));
                                    break;
                                }

                                var word = cc.Context.ZEnvironment.GetVocabAdjective(atom, prop.SourceLine);
                                IWordBuilder wb = cc.Vocabulary[word];
                                if (cc.Context.ZEnvironment.ZVersion == 3)
                                {
                                    tb.AddByte(cc.Constants[ZilAtom.Parse("A?" + word.Atom, cc.Context)]);
                                    length++;
                                }
                                else
                                {
                                    tb.AddShort(wb);
                                    length += 2;
                                }
                            }
                            break;

                        case StdAtom.PSEUDO:
                            handled = true;
                            tb = ob.AddComplexProperty(cc.Properties[propName]);
                            foreach (ZilObject obj in propBody)
                            {
                                var str = obj as ZilString;

                                if (str != null)
                                {
                                    var word = cc.Context.ZEnvironment.GetVocabNoun(ZilAtom.Parse(str.Text, cc.Context), prop.SourceLine);
                                    IWordBuilder wb = cc.Vocabulary[word];
                                    tb.AddShort(wb);
                                }
                                else
                                {
                                    tb.AddShort(CompileConstant(cc, obj));
                                }
                                length += 2;
                            }
                            break;

                        case StdAtom.GLOBAL:
                            if (cc.Context.ZEnvironment.ZVersion == 3)
                            {
                                handled = true;
                                tb = ob.AddComplexProperty(cc.Properties[propName]);
                                foreach (ZilObject obj in propBody)
                                {
                                    var atom = obj as ZilAtom;
                                    if (atom == null)
                                    {
                                        cc.Context.HandleError(new CompilerError(model, CompilerMessages.Values_For_0_Property_Must_Be_1, propName, "atoms"));
                                        break;
                                    }

                                    IObjectBuilder ob2;
                                    if (cc.Objects.TryGetValue(atom, out ob2) == false)
                                    {
                                        cc.Context.HandleError(new CompilerError(model, CompilerMessages.No_Such_Object_0, atom));
                                        break;
                                    }

                                    tb.AddByte(ob2);
                                    length++;
                                }
                            }
                            break;
                    }
                }

                if (!handled)
                {
                    // nothing special, just one or more words
                    pb = cc.Properties[propName];
                    Contract.Assume(pb != null);
                    if (propBody.Rest.IsEmpty)
                    {
                        var word = CompileConstant(cc, value);
                        if (word == null)
                        {
                            cc.Context.HandleError(new CompilerError(
                                prop,
                                CompilerMessages.Nonconstant_Initializer_For_0_1_2,
                                "property",
                                propName,
                                value));
                            word = cc.Game.Zero;
                        }
                        ob.AddWordProperty(pb, word);
                        length = 2;
                    }
                    else
                    {
                        tb = ob.AddComplexProperty(pb);
                        foreach (ZilObject obj in propBody)
                        {
                            var word = CompileConstant(cc, obj);
                            if (word == null)
                            {
                                cc.Context.HandleError(new CompilerError(
                                    prop,
                                    CompilerMessages.Nonconstant_Initializer_For_0_1_2,
                                    "property",
                                    propName,
                                    obj));
                                word = cc.Game.Zero;
                            }
                            tb.AddShort(word);
                            length += 2;
                        }
                    }
                }

                // check property length
                if (length > cc.Game.MaxPropertyLength)
                    cc.Context.HandleError(new CompilerError(
                        prop,
                        CompilerMessages.Property_0_Is_Too_Long_Max_1_Byte1s,
                        propName.ToStringContext(cc.Context, true),
                        cc.Game.MaxPropertyLength));
            }

            //XXX debug line refs for objects
            if (cc.WantDebugInfo)
                cc.Game.DebugFile.MarkObject(ob, new DebugLineRef(), new DebugLineRef());
        }
    }
}
