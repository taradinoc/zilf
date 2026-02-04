/* Copyright 2010-2025 Tara McGrew
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
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using Zilf.Diagnostics;
using Zilf.Emit;
using Zilf.Interpreter.Values;
using Zilf.Language;
using Zilf.ZModel;
using Zilf.ZModel.Vocab;

namespace Zilf.Compiler
{
    partial class Compilation
    {
        void BuildEarlySyntaxTables()
        {
            if (Context.ZEnvironment.Syntaxes.Any(s => s.IsTopic1 || s.IsTopic2) &&
                (Context.GetGlobalOption(StdAtom.NEW_PARSER_P) || Context.GetGlobalOption(StdAtom.COMPACT_SYNTAXES_P)))
            {
                Context.HandleError(new CompilerError(CompilerMessages.TOPIC_In_SYNTAX_Requires_Noncompact_Old_Parser_Syntax_Tables));
            }

            var dict = new Dictionary<string, ITableBuilder>();

            // TODO: encapsulate this in the VocabFormat classes
            if (Context.GetGlobalOption(StdAtom.NEW_PARSER_P))
                BuildNewFormatSyntaxTables(dict);
            else
                BuildOldFormatSyntaxTables(dict);

            foreach (var pair in dict)
                Constants.Add(Context.RootObList[pair.Key], pair.Value);
        }

        void BuildOldFormatSyntaxTables(Dictionary<string, ITableBuilder> tables)
        {
            // TODO: emit VTBL as the first impure table, followed by syntax lines, which is what ztools expects?
            var verbTable = Game.DefineTable("VTBL", true);
            var actionTable = Game.DefineTable("ATBL", true);
            var preactionTable = Game.DefineTable("PATBL", true);

            tables.Add("VTBL", verbTable);
            tables.Add("ATBL", actionTable);
            tables.Add("PATBL", preactionTable);

            // compact syntaxes?
            var compact = Context.GetGlobalOption(StdAtom.COMPACT_SYNTAXES_P);

            // compact preactions?
            var compactPreactions = Context.GetGlobalOption(StdAtom.COMPACT_PREACTIONS_P);

            var vf = Context.ZEnvironment.VocabFormat;

            // group by verb value rather than IWord instance, so synonyms that share
            // a verb number also share a syntax table.
            var query = from s in Context.ZEnvironment.Syntaxes
                        let verbValue = vf.GetVerbValue(s.Verb)
                        group s by verbValue into g
                        orderby g.Key descending
                        select g;

            var actions = new Dictionary<ZilAtom, Action>();

            var syntaxTablesByVerbValue = new Dictionary<int, ITableBuilder>();

            foreach (var verb in query)
            {
                if (verb.Key == 0)
                    continue;

                var verbWord = verb.First().Verb;

                // syntax table
                var stbl = Game.DefineTable("ST?" + verbWord.Atom, true);
                syntaxTablesByVerbValue[verb.Key] = stbl;

                stbl.AddByte((byte)verb.Count());

                // make two passes over the syntax line definitions:
                // first in definition order to create/validate the Actions, second in reverse order to emit the syntax lines
                foreach (var line in verb)
                {
                    ValidateAction(actions, line);
                }

                foreach (var line in verb.Reverse())
                {
                    if (!actions.TryGetValue(line.ActionName, out var act))
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
                                if (line.IsTopic1 || line.IsTopic2)
                                    throw new CompilerError(CompilerMessages.TOPIC_In_SYNTAX_Requires_Noncompact_Old_Parser_Syntax_Tables);

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
                                    stbl.AddByte((IOperand?)GetFlag(line.FindFlag1) ?? Game.Zero);
                                    stbl.AddByte(line.Options1);

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

                                        stbl.AddByte((IOperand?)GetFlag(line.FindFlag2) ?? Game.Zero);
                                        stbl.AddByte(line.Options2);
                                    }
                                }
                            }
                            else
                            {
                                /* ZILF 1.1 extended syntax line format
                                 * 8 bytes: nobj prep1 prep2 find1 find2 opts1 opts2 action
                                 *
                                 * nobj:
                                 *   bits 0-1: number of objects (0, 1, or 2; 3 is invalid)
                                 *   bit 2: 1 if object 1 is special
                                 *   bit 4: 1 if object 2 is special
                                 *   all other bits: reserved
                                 *
                                 * prep1/prep2: preposition number expected before this object, or zero if no prep
                                 *
                                 * if NOT special:
                                 *   find1/find2: FIND flag, or zero if no FIND (flag 0 cannot be a FIND flag)
                                 *   opts1/opts2: search options, meaning can vary per game (NEW-SFLAGS)
                                 *
                                 * if special:
                                 *   if find1/find2 is 0 and opts1/opts2 is 0: object is a TOPIC
                                 *   all other combinations: reserved
                                 */

                                var nobj = line.NumObjects |
                                           (line.IsTopic1 ? 4 : 0) |
                                           (line.IsTopic2 ? 16 : 0);

                                stbl.AddByte((byte)nobj);
                                stbl.AddByte(GetPreposition(line.Preposition1) ?? Game.Zero);
                                stbl.AddByte(GetPreposition(line.Preposition2) ?? Game.Zero);

                                stbl.AddByte(line.IsTopic1 ? Game.Zero : (IOperand?)GetFlag(line.FindFlag1) ?? Game.Zero);
                                stbl.AddByte(line.IsTopic2 ? Game.Zero : (IOperand?)GetFlag(line.FindFlag2) ?? Game.Zero);
                                stbl.AddByte(line.IsTopic1 ? (byte)0 : line.Options1);
                                stbl.AddByte(line.IsTopic2 ? (byte)0 : line.Options2);
                                stbl.AddByte(act.Constant);
                            }
                        }
                    }
                    catch (ZilError ex)
                    {
                        Context.HandleError(ex);
                    }
                }
            }

            // Emit VTBL with stable indexing based on verb values.
            // The old parser indexes VERBS as (255 - verbValue), so VTBL must have one
            // slot for every possible verb value.
            const int MaxVerbValue = 255;
            for (int verbValue = MaxVerbValue; verbValue >= 1; verbValue--)
            {
                if (syntaxTablesByVerbValue.TryGetValue(verbValue, out var stbl))
                    verbTable.AddWord(stbl);
                else
                    verbTable.AddWord(0);
            }

            // action and preaction table
            var actquery = from a in actions
                           orderby a.Value.Index
                           select a.Value;
            foreach (var act in actquery)
            {
                actionTable.AddWord(act.Routine);
                if (compactPreactions)
                {
                    if (act.PreRoutine != null)
                    {
                        preactionTable.AddWord(act.Constant);
                        preactionTable.AddWord(act.PreRoutine);
                    }
                }
                else
                {
                    preactionTable.AddWord((IOperand?)act.PreRoutine ?? Game.Zero);
                }
            }
            if (compactPreactions)
            {
                preactionTable.AddWord(-1);
                preactionTable.AddWord(0);
            }
        }

        void BuildNewFormatSyntaxTables(Dictionary<string, ITableBuilder> tables)
        {
            var actionTable = Game.DefineTable("ATBL", true);
            var preactionTable = Game.DefineTable("PATBL", true);

            tables.Add("ATBL", actionTable);
            tables.Add("PATBL", preactionTable);

            var query = from s in Context.ZEnvironment.Syntaxes
                        group s by s.Verb into verbGrouping
                        let numObjLookup = verbGrouping.ToLookup(s => s.NumObjects)
                        select new
                        {
                            Word = verbGrouping.Key,
                            Nullary = numObjLookup[0].FirstOrDefault(),
                            Unary = numObjLookup[1].ToArray(),
                            Binary = numObjLookup[2].ToArray()
                        };

            // syntax lines are emitted in definition order, so we can validate actions and emit syntax lines in one pass
            var actions = new Dictionary<ZilAtom, Action>();

            foreach (var verb in query)
            {
                // syntax table
                var name = "ACT?" + verb.Word.Atom.Text;
                var acttbl = Game.DefineTable(name, true);
                tables.Add(name, acttbl);

                // 0-object syntaxes
                if (verb.Nullary != null)
                {
                    var act = ValidateAction(actions, verb.Nullary);
                    acttbl.AddWord(act != null ? act.Constant : Game.Zero);
                }
                else
                {
                    acttbl.AddWord(-1);
                }

                // reserved word
                acttbl.AddWord(0);

                // 1-object syntaxes
                if (verb.Unary.Length > 0)
                {
                    var utbl = Game.DefineTable(null, true);
                    utbl.AddWord((short)verb.Unary.Length);

                    foreach (var line in verb.Unary)
                    {
                        var act = ValidateAction(actions, line);
                        utbl.AddWord(act?.Constant ?? Game.Zero);

                        utbl.AddWord(line.Preposition1 == null ? (IOperand)Game.Zero : Vocabulary[line.Preposition1]);
                        utbl.AddByte((IOperand?)GetFlag(line.FindFlag1) ?? Game.Zero);
                        utbl.AddByte(line.Options1);
                    }

                    acttbl.AddWord(utbl);
                }
                else
                {
                    acttbl.AddWord(0);
                }

                // 2-object syntaxes
                if (verb.Binary.Length > 0)
                {
                    var btbl = Game.DefineTable(null, true);
                    btbl.AddWord((short)verb.Binary.Length);

                    foreach (var line in verb.Binary)
                    {
                        var act = ValidateAction(actions, line);
                        btbl.AddWord(act?.Constant ?? Game.Zero);

                        btbl.AddWord(line.Preposition1 == null ? (IOperand)Game.Zero : Vocabulary[line.Preposition1]);
                        btbl.AddByte((IOperand?)GetFlag(line.FindFlag1) ?? Game.Zero);
                        btbl.AddByte(line.Options1);

                        btbl.AddWord(line.Preposition2 == null ? (IOperand)Game.Zero : Vocabulary[line.Preposition2]);
                        btbl.AddByte((IOperand?)GetFlag(line.FindFlag2) ?? Game.Zero);
                        btbl.AddByte(line.Options2);
                    }

                    acttbl.AddWord(btbl);
                }
                else
                {
                    acttbl.AddWord(0);
                }
            }

            // action and preaction table
            var actquery = from a in actions
                           orderby a.Value.Index
                           select a.Value;
            foreach (var act in actquery)
            {
                actionTable.AddWord(act.Routine);
                preactionTable.AddWord((IOperand?)act.PreRoutine ?? Game.Zero);
            }
        }

        void BuildLateSyntaxTables()
        {
            var helpers = new BuildLateSyntaxTablesHelpers
            {
                CompileConstantDelegate = CompileConstant,
                GetGlobalDelegate = atom => Globals[atom],
                Vocabulary = Vocabulary
            };

            Context.ZEnvironment.VocabFormat.BuildLateSyntaxTables(helpers);
        }

        Action? ValidateAction(Dictionary<ZilAtom, Action> actions, Syntax line)
        {
            try
            {
                using (DiagnosticContext.Push(line.SourceLine))
                {
                    if (!actions.TryGetValue(line.ActionName, out var act))
                    {
                        if (!Routines.TryGetValue(line.Action, out var routine))
                            throw new CompilerError(CompilerMessages.Undefined_0_1, "action routine", line.Action);

                        IRoutineBuilder? preRoutine = null;
                        if (line.Preaction != null &&
                            !Routines.TryGetValue(line.Preaction, out preRoutine))
                            throw new CompilerError(CompilerMessages.Undefined_0_1, "preaction routine", line.Preaction);

                        var actionName = line.ActionName;
                        int index = Context.ZEnvironment.NextAction++;

                        if (index >= Context.ZEnvironment.VocabFormat.MaxActionCount)
                            throw new InterpreterError(
                                InterpreterMessages.Too_Many_0_Only_1_Allowed_In_This_Vocab_Format,
                                "actions",
                                Context.ZEnvironment.VocabFormat.MaxActionCount);

                        var number = Game.MakeOperand(index);
                        var constant = Game.DefineConstant(actionName.Text, number);
                        Constants.Add(actionName, constant);
                        if (WantDebugInfo)
                        {
                            Debug.Assert(Game.DebugFile != null);
                            Game.DebugFile.MarkAction(constant, actionName.Text);
                        }

                        act = new Action(index, constant, routine, preRoutine, line.Action, line.Preaction);
                        actions.Add(actionName, act);
                    }
                    else
                    {
                        WarnIfActionRoutineDiffers(line, "action routine", line.Action, act.RoutineName);
                        WarnIfActionRoutineDiffers(line, "preaction routine", line.Preaction, act.PreRoutineName);
                    }

                    return act;
                }
            }
            catch (ZilError ex)
            {
                Context.HandleError(ex);
                return null;
            }
        }

        void WarnIfActionRoutineDiffers(Syntax line, string description,
            ZilAtom? thisRoutineName, ZilAtom? lastRoutineName)
        {
            if (thisRoutineName != lastRoutineName)
            {
                Context.HandleError(new CompilerError(line.SourceLine,
                    CompilerMessages._0_Mismatch_For_1_Using_2_As_Before,
                    description,
                    line.ActionName,
                    lastRoutineName?.ToString() ?? "no " + description));
            }
        }

        /// <summary>
        /// Defines the appropriate constants for a word (W?FOO, A?FOO, ACT?FOO, PREP?FOO),
        /// creating the IWordBuilder if needed.
        /// </summary>
        /// <param name="word">The Word.</param>
        /// 
        void DefineWord(IWord word)
        {
            string rawWord = word.Atom.Text;

#pragma warning disable CA1864 // Prefer the 'IDictionary.TryAdd(TKey, TValue)' method
            if (!Vocabulary.ContainsKey(word))
            {
                var wAtom = ZilAtom.Parse("W?" + rawWord, Context);
                if (!Constants.TryGetValue(wAtom, out var constantValue))
                {
                    var wb = Game.DefineVocabularyWord(rawWord);
                    Vocabulary.Add(word, wb);
                    Constants.Add(wAtom, wb);
                }
                else
                {
                    if (constantValue is IWordBuilder wb)
                    {
                        Vocabulary.Add(word, wb);
                    }
                    else
                    {
                        throw new CompilerError(CompilerMessages.Nonvocab_Constant_0_Conflicts_With_Vocab_Word_1, wAtom, word.Atom);
                    }
                }
            }
#pragma warning restore CA1864 // Prefer the 'IDictionary.TryAdd(TKey, TValue)' method

            foreach (var pair in Context.ZEnvironment.VocabFormat.GetVocabConstants(word))
            {
                var atom = ZilAtom.Parse(pair.Key, Context);
                if (!Constants.ContainsKey(atom))
                    Constants.Add(atom,
                        Game.DefineConstant(pair.Key,
                            Game.MakeOperand(pair.Value)));
            }
        }

        [return: NotNullIfNotNull(nameof(word))]
        IOperand? GetPreposition(IWord? word)
        {
            if (word == null)
                return null;

            string name = "PR?" + word.Atom.Text;
            var atom = ZilAtom.Parse(name, Context);
            return Constants[atom];
        }
    }
}
