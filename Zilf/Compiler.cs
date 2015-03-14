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
using Zilf.Emit;

namespace Zilf
{
    partial class Compiler
    {
        [Flags]
        private enum BlockReturnState
        {
            /// <summary>
            /// Indicates that the return label was used.
            /// </summary>
            Returned = 1,
            /// <summary>
            /// Indicates that the return label expects a result on the stack. (Otherwise, the
            /// result must be discarded before branching.)
            /// </summary>
            WantResult = 2,
        }

        private class CompileCtx
        {
            /// <summary>
            /// The ZIL context that resulted from loading the source code.
            /// </summary>
            public Context Context;
            /// <summary>
            /// The game being built.
            /// </summary>
            public IGameBuilder Game;
            /// <summary>
            /// True if debug information should be generated (i.e. if the user
            /// wants it and the game builder supports it).
            /// </summary>
            public bool WantDebugInfo;
            /// <summary>
            /// The label to which &lt;AGAIN&gt; should branch.
            /// </summary>
            public ILabel AgainLabel;
            /// <summary>
            /// The label to which &lt;RETURN&gt; should branch, or null if
            /// it should return from the routine.
            /// </summary>
            public ILabel ReturnLabel;
            /// <summary>
            /// The context flags for &lt;RETURN&gt;.
            /// </summary>
            public BlockReturnState ReturnState;

            public ITableBuilder VerbTable, ActionTable, PreactionTable, PrepositionTable;

            public Dictionary<ZilAtom, ILocalBuilder> Locals = new Dictionary<ZilAtom, ILocalBuilder>();
            public Stack<ILocalBuilder> SpareLocals = new Stack<ILocalBuilder>();
            public Dictionary<ZilAtom, Stack<ILocalBuilder>> OuterLocals = new Dictionary<ZilAtom, Stack<ILocalBuilder>>();

            public Dictionary<ZilAtom, IGlobalBuilder> Globals = new Dictionary<ZilAtom, IGlobalBuilder>();
            public Dictionary<ZilAtom, IOperand> Constants = new Dictionary<ZilAtom, IOperand>();
            public Dictionary<ZilAtom, IRoutineBuilder> Routines = new Dictionary<ZilAtom, IRoutineBuilder>();
            public Dictionary<ZilAtom, IObjectBuilder> Objects = new Dictionary<ZilAtom, IObjectBuilder>();
            public Dictionary<ZilTable, ITableBuilder> Tables = new Dictionary<ZilTable, ITableBuilder>();
            public Dictionary<Word, IWordBuilder> Vocabulary = new Dictionary<Word, IWordBuilder>();
            public Dictionary<ZilAtom, IPropertyBuilder> Properties = new Dictionary<ZilAtom, IPropertyBuilder>();
            public Dictionary<ZilAtom, IFlagBuilder> Flags = new Dictionary<ZilAtom, IFlagBuilder>();
        }

        public void Compile(Context ctx, IGameBuilder gb)
        {
            CompileCtx cc = new CompileCtx();
            cc.Context = ctx;
            cc.Game = gb;
            cc.WantDebugInfo = gb.DebugFile != null && ctx.WantDebugInfo;

            /* the various structures need to be defined in the right order so
             * that symbols like P?FOO, V?FOO, etc. are always defined before
             * they could possibly be used. */

            // builders for routines
            // TODO: respect CLEAN-STACK? in FILE-FLAGS and ROUTINE-FLAGS
            foreach (ZilRoutine routine in ctx.ZEnvironment.Routines)
                cc.Routines.Add(routine.Name, gb.DefineRoutine(
                    routine.Name.ToString(),
                    routine.Name.StdAtom == StdAtom.GO,
                    false));

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

            // builders for flags mentioned in syntax
            var syntaxRelevantFlags = (from syn in ctx.ZEnvironment.Syntaxes
                                       from flag in new[] { syn.FindFlag1, syn.FindFlag2 }
                                       where flag != null
                                       select flag).Distinct().ToList();

            if (syntaxRelevantFlags.Count >= cc.Game.MaxFlags)
                Errors.CompError(ctx, null, "too many flags mentioned in syntax lines");

            foreach (var flag in syntaxRelevantFlags)
                DefineFlag(cc, flag);

            // builders for objects
            foreach (ZilModelObject obj in ctx.ZEnvironment.ObjectsInDefinitionOrder())
            {
                cc.Objects.Add(obj.Name, gb.DefineObject(obj.Name.ToString()));
                // builders for the rest of the properties and flags,
                // and vocabulary for names
                PreBuildObject(cc, obj);
            }

            // builders for tables
            foreach (ZilTable table in ctx.ZEnvironment.Tables)
                cc.Tables.Add(table, gb.DefineTable(table.Name, (table.Flags & TableFlags.Pure) != 0));

            // vocabulary for punctuation
            DefinePunctWord(ctx, cc, ".", "PERIOD");
            DefinePunctWord(ctx, cc, ",", "COMMA");
            DefinePunctWord(ctx, cc, "\"", "QUOTE");

            // builders for vocabulary
            foreach (ZilAtom buzz in ctx.ZEnvironment.Buzzwords)
                ctx.ZEnvironment.GetVocabBuzzword(buzz);

            ctx.ZEnvironment.MergeVocabulary();

            foreach (Word word in ctx.ZEnvironment.Vocabulary.Values)
                DefineWord(cc, word);

            // tables for syntax
            BuildSyntaxTables(cc);

            // now that all the vocabulary is set up, copy values for synonyms
            foreach (Synonym syn in ctx.ZEnvironment.Synonyms)
                syn.Apply(ctx);

            // builders and values for constants (which may refer to vocabulary,
            // routines, tables, objects, properties, or flags)
            foreach (ZilConstant constant in ctx.ZEnvironment.Constants)
            {
                IOperand value = CompileConstant(cc, constant.Value);
                if (value == null)
                    Errors.CompError(ctx, constant, "invalid constant value");
                cc.Constants.Add(constant.Name, gb.DefineConstant(constant.Name.ToString(), value));
            }

            // builders and values for globals (which may refer to constants)
            IGlobalBuilder glb;
            foreach (ZilGlobal global in ctx.ZEnvironment.Globals)
            {
                glb = gb.DefineGlobal(global.Name.ToString());
                if (global.Value != null)
                {
                    try
                    {
                        glb.DefaultValue = CompileConstant(cc, global.Value);
                        if (glb.DefaultValue == null)
                            Errors.CompError(ctx, global, "default value must be constant");
                    }
                    catch (ZilError ex)
                    {
                        if (ex.SourceLine == null)
                            ex.SourceLine = global;
                        ctx.HandleError(ex);
                    }
                }
                cc.Globals.Add(global.Name, glb);
            }

            glb = cc.Game.DefineGlobal("PREPOSITIONS");
            glb.DefaultValue = cc.PrepositionTable;
            cc.Globals.Add(cc.Context.GetStdAtom(StdAtom.PREPOSITIONS), glb);

            glb = cc.Game.DefineGlobal("ACTIONS");
            glb.DefaultValue = cc.ActionTable;
            cc.Globals.Add(cc.Context.GetStdAtom(StdAtom.ACTIONS), glb);

            glb = cc.Game.DefineGlobal("PREACTIONS");
            glb.DefaultValue = cc.PreactionTable;
            cc.Globals.Add(cc.Context.GetStdAtom(StdAtom.PREACTIONS), glb);

            glb = cc.Game.DefineGlobal("VERBS");
            glb.DefaultValue = cc.VerbTable;
            cc.Globals.Add(cc.Context.GetStdAtom(StdAtom.VERBS), glb);

            // default values for properties
            foreach (KeyValuePair<ZilAtom, ZilObject> pair in ctx.ZEnvironment.PropertyDefaults)
            {
                try
                {
                    IPropertyBuilder pb = cc.Properties[pair.Key];
                    pb.DefaultValue = CompileConstant(cc, pair.Value);
                    if (pb.DefaultValue == null)
                        throw new CompilerError("default value must be constant");
                }
                catch (ZilError ex)
                {
                    if (ex.SourceLine == null)
                        ex.SourceLine = new StringSourceLine(
                            "property default for '" + pair.Key + "'");
                    ctx.HandleError(ex);
                }
            }

            // compile routines
            IRoutineBuilder mainRoutine = null;

            foreach (ZilRoutine routine in ctx.ZEnvironment.Routines)
            {
                bool entryPoint = (routine.Name.StdAtom == StdAtom.GO);
                IRoutineBuilder rb = cc.Routines[routine.Name];
                try
                {
                    BuildRoutine(cc, routine, gb, rb, entryPoint);
                }
                catch (ZilError ex)
                {
                    // could be a compiler error, or an interpreter error thrown by macro evaluation
                    if (ex.SourceLine == null)
                        ex.SourceLine = routine;
                    ctx.HandleError(ex);
                }
                rb.Finish();

                if (entryPoint)
                    mainRoutine = rb;
            }

            if (mainRoutine == null)
                throw new CompilerError("missing 'GO' routine");

            // build objects
            foreach (ZilModelObject obj in ctx.ZEnvironment.ObjectsInInsertionOrder())
            {
                IObjectBuilder ob = cc.Objects[obj.Name];
                try
                {
                    BuildObject(cc, obj, ob);
                }
                catch (ZilError ex)
                {
                    if (ex.SourceLine == null)
                        ex.SourceLine = obj;
                    ctx.HandleError(ex);
                }
            }

            // build vocabulary
            int numPreps = 0;

            var query = from p in cc.Vocabulary
                        select new { Word = p.Key, Builder = p.Value };
            foreach (var item in query)
            {
                Word word = item.Word;
                IWordBuilder wb = item.Builder;

                if ((word.PartOfSpeech & PartOfSpeech.Preposition) != 0)
                    numPreps++;

                Func<byte, IOperand> dirIndexToPropertyOperand = di => cc.Properties[ctx.ZEnvironment.Directions[di]];

                word.WriteToBuilder(ctx, wb, dirIndexToPropertyOperand);
            }

            // build preposition table
            cc.PrepositionTable.AddShort((short)numPreps);

            foreach (KeyValuePair<Word, IWordBuilder> pair in cc.Vocabulary)
            {
                if ((pair.Key.PartOfSpeech & PartOfSpeech.Preposition) != 0)
                {
                    string prname = "PR?" + pair.Key.Atom;
                    ZilAtom atom = ZilAtom.Parse(prname, ctx);
                    IOperand constant;

                    if (cc.Constants.TryGetValue(atom, out constant))
                    {
                        cc.PrepositionTable.AddShort(pair.Value);
                        cc.PrepositionTable.AddShort(cc.Constants[atom]);
                    }
                }
            }

            // build tables
            foreach (KeyValuePair<ZilTable, ITableBuilder> pair in cc.Tables)
                BuildTable(cc, pair.Key, pair.Value);

            gb.Finish();
        }

        private class Action
        {
            public readonly int Index;
            public readonly IOperand Constant;
            public readonly IRoutineBuilder Routine, PreRoutine;
            public readonly ZilAtom PreRoutineName;

            public Action(int index, IOperand constant, IRoutineBuilder routine, IRoutineBuilder preRoutine,
                ZilAtom preRoutineName)
            {
                this.Index = index;
                this.Constant = constant;
                this.Routine = routine;
                this.PreRoutine = preRoutine;
                this.PreRoutineName = preRoutineName;
            }
        }

        private void BuildSyntaxTables(CompileCtx cc)
        {
            cc.VerbTable = cc.Game.DefineTable("VTBL", true);
            cc.ActionTable = cc.Game.DefineTable("ATBL", true);
            cc.PreactionTable = cc.Game.DefineTable("PATBL", true);
            cc.PrepositionTable = cc.Game.DefineTable("PRTBL", true);

            // compact syntaxes?
            ZilAtom compactAtom = cc.Context.GetStdAtom(StdAtom.COMPACT_SYNTAXES_P);
            ZilObject compactObj = cc.Context.GetGlobalVal(compactAtom);
            bool compact = (compactObj != null && compactObj.IsTrue);

            // verb table
            var query = from s in cc.Context.ZEnvironment.Syntaxes
                        group s by s.Verb into g
                        orderby g.Key.GetValue(PartOfSpeech.Verb) descending
                        select g;

            Dictionary<ZilAtom, Action> actions = new Dictionary<ZilAtom, Action>();

            foreach (var verb in query)
            {
                int num = verb.Key.GetValue(PartOfSpeech.Verb);

                // syntax table
                ITableBuilder stbl = cc.Game.DefineTable("ST?" + verb.Key.Atom.ToString(), true);
                cc.VerbTable.AddShort(stbl);

                stbl.AddByte((byte)verb.Count());

                foreach (Syntax line in verb)
                {
                    try
                    {
                        Action act;
                        if (actions.TryGetValue(line.Action, out act) == false)
                        {
                            string name = line.Action.ToString();
                            if (name.ToUpper().StartsWith("V-"))
                                name = "V?" + name.Substring(2);
                            else
                                name = "V??" + name;

                            IRoutineBuilder routine;
                            if (cc.Routines.TryGetValue(line.Action, out routine) == false)
                                throw new CompilerError("undefined action routine: " + line.Action);

                            IRoutineBuilder preRoutine = null;
                            if (line.Preaction != null &&
                                cc.Routines.TryGetValue(line.Preaction, out preRoutine) == false)
                                throw new CompilerError("undefined preaction routine: " + line.Preaction);

                            ZilAtom atom = ZilAtom.Parse(name, cc.Context);
                            int index = cc.Context.ZEnvironment.NextAction++;
                            IOperand number = cc.Game.MakeOperand(index);
                            IOperand constant = cc.Game.DefineConstant(name, number);
                            cc.Constants.Add(atom, constant);
                            if (cc.WantDebugInfo)
                                cc.Game.DebugFile.MarkAction(constant, line.Action.ToString());

                            act = new Action(index, number, routine, preRoutine, line.Preaction);
                            actions.Add(line.Action, act);
                        }
                        else
                        {
                            IRoutineBuilder preRoutine;
                            bool match = true;

                            if (line.Preaction == null)
                                match = (act.PreRoutine == null);
                            else if (act.PreRoutine == null)
                                match = false;
                            else
                                match = (cc.Routines.TryGetValue(line.Preaction, out preRoutine) &&
                                    preRoutine == act.PreRoutine);
                            
                            if (!match)
                                Errors.CompWarning(cc.Context, line,
                                    "preaction mismatch for {0}: using {1} as before",
                                    line.Action,
                                    act.PreRoutineName.ToString() ?? "no preaction");
                        }

                        if (compact)
                        {
                            if (line.Preposition1 != null)
                            {
                                byte pn = line.Preposition1.GetValue(PartOfSpeech.Preposition);
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
                                        byte pn = line.Preposition2.GetValue(PartOfSpeech.Preposition);
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
                    catch (ZilError ex)
                    {
                        if (ex.SourceLine == null)
                            ex.SourceLine = line;
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
                cc.ActionTable.AddShort(act.Routine);
                cc.PreactionTable.AddShort(act.PreRoutine ?? cc.Game.Zero);
            }
        }

        private static IFlagBuilder GetFlag(CompileCtx cc, ZilAtom flag)
        {
            if (flag == null)
                return null;

            DefineFlag(cc, flag);
            return cc.Flags[flag];
        }

        private static IOperand GetPreposition(CompileCtx cc, Word word)
        {
            if (word == null)
                return null;

            string name = "PR?" + word.Atom.Text;
            ZilAtom atom = ZilAtom.Parse(name, cc.Context);
            return cc.Constants[atom];
        }

        private static void DefineProperty(CompileCtx cc, ZilAtom prop)
        {
            if (!cc.Properties.ContainsKey(prop))
            {
                // create property builder
                IPropertyBuilder pb = cc.Game.DefineProperty(prop.ToString());
                cc.Properties.Add(prop, pb);

                // create constant
                string propConstName = "P?" + prop.ToString();
                ZilAtom propAtom = ZilAtom.Parse(propConstName, cc.Context);
                cc.Constants.Add(propAtom, pb);
            }
        }

        private static void DefineFlag(CompileCtx cc, ZilAtom flag)
        {
            if (!cc.Flags.ContainsKey(flag))
            {
                // create flag builder
                IFlagBuilder fb = cc.Game.DefineFlag(flag.ToString());
                cc.Flags.Add(flag, fb);

                // create constant
                cc.Constants.Add(flag, fb);
            }
        }

        private static void DefinePunctWord(Context ctx, CompileCtx cc, string punct, string name)
        {
            ZilAtom atom = ZilAtom.Parse(punct, ctx);
            Word pword = new Word(atom);
            cc.Context.ZEnvironment.Vocabulary.Add(atom, pword);

            IWordBuilder pwb = cc.Game.DefineVocabularyWord(punct);
            cc.Vocabulary.Add(pword, pwb);
            cc.Constants.Add(ZilAtom.Parse("W?" + name, cc.Context), pwb);
        }
        
        private static void DefineWord(CompileCtx cc, Word word)
        {
            string rawWord = word.Atom.ToString();

            if (!cc.Vocabulary.ContainsKey(word))
            {
                IWordBuilder wb = cc.Game.DefineVocabularyWord(rawWord);
                cc.Vocabulary.Add(word, wb);
                cc.Constants.Add(ZilAtom.Parse("W?" + rawWord, cc.Context), wb);
            }

            // adjective numbers only exist in V3
            if (cc.Context.ZEnvironment.ZVersion == 3 &&
                (word.PartOfSpeech & PartOfSpeech.Adjective) != 0)
            {
                string adjConstant = "A?" + rawWord;
                ZilAtom adjAtom = ZilAtom.Parse(adjConstant, cc.Context);
                if (!cc.Constants.ContainsKey(adjAtom))
                    cc.Constants.Add(adjAtom,
                        cc.Game.DefineConstant(adjConstant,
                            cc.Game.MakeOperand(word.GetValue(PartOfSpeech.Adjective))));
            }

            if ((word.PartOfSpeech & PartOfSpeech.Verb) != 0)
            {
                string verbConstant = "ACT?" + rawWord;
                ZilAtom verbAtom = ZilAtom.Parse(verbConstant, cc.Context);
                if (!cc.Constants.ContainsKey(verbAtom))
                    cc.Constants.Add(verbAtom,
                        cc.Game.DefineConstant(verbConstant,
                            cc.Game.MakeOperand(word.GetValue(PartOfSpeech.Verb))));
            }

            if ((word.PartOfSpeech & PartOfSpeech.Preposition) != 0)
            {
                string prepConstant = "PR?" + rawWord;
                ZilAtom prepAtom = ZilAtom.Parse(prepConstant, cc.Context);
                if (!cc.Constants.ContainsKey(prepAtom))
                    cc.Constants.Add(prepAtom,
                        cc.Game.DefineConstant(prepConstant,
                            cc.Game.MakeOperand(word.GetValue(PartOfSpeech.Preposition))));
            }
        }

        private static void BuildTable(CompileCtx cc, ZilTable zt, ITableBuilder tb)
        {
            if ((zt.Flags & TableFlags.Lexv) != 0)
            {
                IOperand[] values = new IOperand[zt.ElementCount * 3];
                zt.CopyTo(values, 0, values.Length, zo => CompileConstant(cc, zo), cc.Game.Zero);

                tb.AddByte((byte)zt.ElementCount);
                tb.AddByte(0);

                for (int i = 0; i < values.Length; i++)
                    if (i % 3 == 0)
                        tb.AddShort(values[i]);
                    else
                        tb.AddByte(values[i]);
            }
            else
            {
                if ((zt.Flags & TableFlags.ByteLength) != 0)
                    tb.AddByte((byte)zt.ElementCount);
                else if ((zt.Flags & TableFlags.WordLength) != 0)
                    tb.AddShort((short)zt.ElementCount);

                IOperand[] values = new IOperand[zt.ElementCount];
                Func<ZilObject, IOperand> convertElement = zo =>
                {
                    // it's usually a constant value
                    var constVal = CompileConstant(cc, zo);
                    if (constVal != null)
                        return constVal;

                    // but we'll also allow a global name if the global contains a table
                    IGlobalBuilder global;
                    if (zo is ZilAtom && cc.Globals.TryGetValue((ZilAtom)zo, out global) && global.DefaultValue is ITableBuilder)
                        return global.DefaultValue;

                    return null;
                };
                zt.CopyTo(values, 0, values.Length, convertElement, cc.Game.Zero);

                for (int i = 0; i < values.Length; i++)
                    if (values[i] == null)
                    {
                        Errors.CompError(cc.Context, (ISourceLine)zt,
                            "non-constant in table initializer at element {0}", i);
                        values[i] = cc.Game.Zero;
                    }

                if ((zt.Flags & TableFlags.Byte) != 0)
                {
                    for (int i = 0; i < values.Length; i++)
                        tb.AddByte(values[i]);
                }
                else
                {
                    for (int i = 0; i < values.Length; i++)
                        tb.AddShort(values[i]);
                }
            }
        }

        private static string TranslateString(string str)
        {
            // strip CR/LF and ensure 1 space afterward; translate '|' to LF
            StringBuilder sb = new StringBuilder(str);
            char? last = null;

            for (int i = 0; i < sb.Length; i++)
            {
                char c = sb[i];

                switch (c)
                {
                    case '|':
                        sb[i] = '\n';
                        break;

                    case '\r':
                        sb.Remove(i--, 1);
                        continue;

                    case '\n':
                        if (last == '|')
                            sb.Remove(i--, 1);
                        else
                            sb[i] = ' ';
                        break;
                }

                last = c;
            }

            return sb.ToString();
        }

        private static IOperand CompileConstant(CompileCtx cc, ZilObject expr)
        {
            ZilAtom atom;

            switch (expr.GetTypeAtom(cc.Context).StdAtom)
            {
                case StdAtom.FIX:
                    return cc.Game.MakeOperand(((ZilFix)expr).Value);

                case StdAtom.BYTE:
                    return cc.Game.MakeOperand(((ZilFix)((ZilHash)expr).GetPrimitive(cc.Context)).Value);

                case StdAtom.STRING:
                    return cc.Game.MakeOperand(TranslateString(((ZilString)expr).Text));

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
                    return cc.Tables[(ZilTable)expr];

                case StdAtom.CONSTANT:
                    return CompileConstant(cc, ((ZilConstant)expr).Value);

                case StdAtom.FORM:
                    ZilForm form = (ZilForm)expr;
                    if (!form.IsEmpty &&
                        form.First == cc.Context.GetStdAtom(StdAtom.GVAL) &&
                        !form.Rest.IsEmpty &&
                        form.Rest.First.GetTypeAtom(cc.Context).StdAtom == StdAtom.ATOM &&
                        form.Rest.Rest.IsEmpty)
                    {
                        return CompileConstant(cc, form.Rest.First);
                    }
                    return null;

                default:
                    return null;
            }
        }

        private static void BuildRoutine(CompileCtx cc, ZilRoutine routine,
            IGameBuilder gb, IRoutineBuilder rb, bool entryPoint)
        {
            // set up arguments and locals
            cc.Locals.Clear();
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
                                throw new CompilerError(routine, "optional args with non-constant defaults not supported for this target");

                            ILabel nextLabel = rb.DefineLabel();
                            rb.Branch(Condition.ArgProvided, lb.Indirect, null, nextLabel, true);
                            IOperand val = CompileAsOperand(cc, rb, arg.DefaultValue, lb);
                            if (val != lb)
                                rb.EmitStore(lb, val);
                            rb.MarkLabel(nextLabel);
                        }
                        else
                        {
                            IOperand val = CompileAsOperand(cc, rb, arg.DefaultValue, lb);
                            if (val != lb)
                                rb.EmitStore(lb, val);
                        }
                    }
                }
            }

            if (cc.Context.TraceRoutines)
                rb.EmitPrint("]\n", false);

            // define standard labels
            cc.AgainLabel = rb.DefineLabel();
            rb.MarkLabel(cc.AgainLabel);

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
        }

        private static void CompileStmt(CompileCtx cc, IRoutineBuilder rb, ZilObject stmt, bool wantResult)
        {
            ZilForm form = stmt as ZilForm;
            if (form == null)
            {
                if (wantResult)
                {
                    IOperand value = CompileConstant(cc, stmt);
                    if (value == null)
                    {
                        if (stmt.GetTypeAtom(cc.Context).StdAtom == StdAtom.LIST)
                            throw new CompilerError("lists cannot be returned (misplaced bracket in COND?)");
                        else
                            throw new CompilerError("values of this type cannot be returned");
                    }

                    rb.Return(value);
                }
                else
                {
                    // TODO: warning message when skipping non-forms inside a routine?
                }
            }
            else
            {
                if (cc.WantDebugInfo)
                    cc.Game.DebugFile.MarkSequencePoint(rb,
                        new DebugLineRef(form.SourceFile, form.SourceLine, 1));
                
                IOperand result = CompileForm(cc, rb, form, wantResult, null);

                if (wantResult)
                    rb.Return(result);
            }
        }

        private class Operands : IDisposable
        {
            private readonly CompileCtx cc;
            private readonly IOperand[] values;
            private readonly bool[] temps;
            private readonly ZilAtom tempAtom;

            private Operands(CompileCtx cc, IOperand[] values, bool[] temps, ZilAtom tempAtom)
            {
                this.cc = cc;
                this.values = values;
                this.temps = temps;
                this.tempAtom = tempAtom;
            }

            public static Operands Compile(CompileCtx cc, IRoutineBuilder rb, params ZilObject[] exprs)
            {
                int length = exprs.Length;
                IOperand[] values = new IOperand[length];
                bool[] temps = new bool[length];
                ZilAtom tempAtom = ZilAtom.Parse("?TMP", cc.Context);

                // find the index of the last expr with side effects (or -1)
                int marker = -1;
                for (int i = length - 1; i >= 0; i--)
                {
                    if (HasSideEffects(cc, exprs[i]))
                    {
                        marker = i;
                        break;
                    }
                }

                /* Evaluate arguments up to and including the marker, left to right.
                 * Force the results into temp variables, except:
                 * - Constants
                 * - Local variables, if they aren't modified by any following argument
                 *   (i.e. no following argument includes SET[G] to the particular local)
                 * - Global variables, if they aren't potentially modified by any following
                 *   argument (i.e. no following arguments include routine calls or SET[G] to the
                 *   particular global)
                 * - The marker itself, if (1) its natural location is not the stack, or
                 *   (2) every following argument is a constant or variable.
                 */
                const string STempsNotAllowed = "expression needs temporary variables, not allowed here";
                for (int i = 0; i <= marker; i++)
                {
                    bool needTemp = false;

                    IOperand value = CompileConstant(cc, exprs[i]);
                    if (value == null)
                    {
                        // not a constant
                        value = CompileAsOperand(cc, rb, exprs[i]);

                        if (IsLocalVariableRef(exprs[i]))
                        {
                            needTemp = LocalIsLaterModified(exprs, i);
                        }
                        else if (IsGlobalVariableRef(exprs[i]))
                        {
                            needTemp = GlobalCouldBeLaterModified(cc, exprs, i);
                        }
                        else if (i == marker)
                        {
                            if (value == rb.Stack)
                            {
                                for (int j = i + 1; j < length; j++)
                                {
                                    if (CompileConstant(cc, exprs[j]) == null && !IsVariableRef(exprs[j]))
                                    {
                                        needTemp = true;
                                        break;
                                    }
                                }
                            }
                        }
                        else
                        {
                            needTemp = true;
                        }
                    }

                    if (!needTemp)
                    {
                        values[i] = value;
                    }
                    else
                    {
                        try
                        {
                            PushInnerLocal(cc, rb, tempAtom);
                        }
                        catch (InvalidOperationException)
                        {
                            throw new CompilerError(STempsNotAllowed);
                        }
                        values[i] = cc.Locals[tempAtom];
                        rb.EmitStore((IVariable)values[i], value);
                        temps[i] = true;
                    }
                }

                // evaluate the rest of the arguments right to left, leaving the results
                // in their natural locations.
                for (int i = length - 1; i > marker; i--)
                    values[i] = CompileAsOperand(cc, rb, exprs[i]);

                return new Operands(cc, values, temps, tempAtom);
            }

            private static bool LocalIsLaterModified(ZilObject[] exprs, int localIdx)
            {
                ZilForm form = exprs[localIdx] as ZilForm;
                if (form == null)
                    throw new ArgumentException("not a FORM");

                ZilAtom atom = form.First as ZilAtom;
                if (atom == null || atom.StdAtom != StdAtom.LVAL)
                    throw new ArgumentException("not an LVAL FORM");

                ZilAtom localAtom = form.Rest.First as ZilAtom;
                if (atom == null)
                    throw new ArgumentException("LVAL not followed by an atom");

                for (int i = localIdx + 1; i < exprs.Length; i++)
                    if (ModifiesLocal(exprs[i], localAtom))
                        return true;

                return false;
            }

            private static bool ModifiesLocal(ZilObject expr, ZilAtom localAtom)
            {
                ZilList list = expr as ZilList;
                if (list == null)
                    return false;

                if (list is ZilForm)
                {
                    ZilAtom atom = list.First as ZilAtom;
                    if (atom != null &&
                        (atom.StdAtom == StdAtom.SET || atom.StdAtom == StdAtom.SETG) &&
                        list.Rest != null && list.Rest.First == localAtom)
                    {
                        return true;
                    }
                }

                foreach (ZilObject zo in list)
                    if (ModifiesLocal(zo, localAtom))
                        return true;

                return false;
            }

            private static bool GlobalCouldBeLaterModified(CompileCtx cc, ZilObject[] exprs, int localIdx)
            {
                ZilForm form = exprs[localIdx] as ZilForm;
                if (form == null)
                    throw new ArgumentException("not a FORM");

                ZilAtom atom = form.First as ZilAtom;
                if (atom == null || atom.StdAtom != StdAtom.GVAL)
                    throw new ArgumentException("not a GVAL FORM");

                ZilAtom globalAtom = form.Rest.First as ZilAtom;
                if (atom == null)
                    throw new ArgumentException("GVAL not followed by an atom");

                for (int i = localIdx + 1; i < exprs.Length; i++)
                    if (CouldModifyGlobal(cc, exprs[i], globalAtom))
                        return true;

                return false;
            }

            private static bool CouldModifyGlobal(CompileCtx cc, ZilObject expr, ZilAtom globalAtom)
            {
                ZilList list = expr as ZilList;
                if (list == null)
                    return false;

                if (list is ZilForm)
                {
                    ZilAtom atom = list.First as ZilAtom;
                    if (atom != null &&
                        (atom.StdAtom == StdAtom.SET || atom.StdAtom == StdAtom.SETG) &&
                        list.Rest != null && list.Rest.First == globalAtom)
                    {
                        return true;
                    }
                    else if (cc.Routines.ContainsKey(atom))
                    {
                        return true;
                    }
                }

                foreach (ZilObject zo in list)
                    if (CouldModifyGlobal(cc, zo, globalAtom))
                        return true;

                return false;
            }

            private static bool IsVariableRef(ZilObject expr)
            {
                ZilForm form = expr as ZilForm;
                if (form == null)
                    return false;

                ZilAtom atom = form.First as ZilAtom;
                if (atom == null)
                    return false;

                return atom.StdAtom == StdAtom.GVAL || atom.StdAtom == StdAtom.LVAL;
            }

            private static bool IsLocalVariableRef(ZilObject expr)
            {
                ZilForm form = expr as ZilForm;
                if (form == null)
                    return false;

                ZilAtom atom = form.First as ZilAtom;
                if (atom == null)
                    return false;

                return atom.StdAtom == StdAtom.LVAL;
            }

            private static bool IsGlobalVariableRef(ZilObject expr)
            {
                ZilForm form = expr as ZilForm;
                if (form == null)
                    return false;

                ZilAtom atom = form.First as ZilAtom;
                if (atom == null)
                    return false;

                return atom.StdAtom == StdAtom.GVAL;
            }

            public void Dispose()
            {
                for (int i = 0; i < temps.Length; i++)
                    if (temps[i])
                        PopInnerLocal(cc, tempAtom);
            }

            public int Count
            {
                get { return values.Length; }
            }

            public IOperand this[int index]
            {
                get { return values[index]; }
            }

            public IOperand[] ToArray()
            {
                return values;
            }

            public IEnumerable<IOperand> Skip(int count)
            {
                for (int i = count; i < values.Length; i++)
                    yield return values[i];
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
        private static IOperand CompileForm(CompileCtx cc, IRoutineBuilder rb, ZilForm form,
            bool wantResult, IVariable resultStorage)
        {
            ILabel label1, label2;
            IOperand operand;

            try
            {
                // expand macro invocations
                ZilObject expanded = form.Expand(cc.Context);
                if (expanded is ZilForm)
                {
                    form = (ZilForm)expanded;
                }
                else
                {
                    if (wantResult)
                        return CompileAsOperand(cc, rb, expanded, resultStorage);
                    else
                        return null;
                }

                ZilAtom head = form.First as ZilAtom;
                if (head == null)
                {
                    Errors.CompError(cc.Context, form, "FORM inside a routine must start with an atom");
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
                    else if (ZBuiltins.IsBuiltinValuePredCall(head.Text, zversion, argCount))
                    {
                        label1 = rb.DefineLabel();
                        resultStorage = resultStorage ?? rb.Stack;
                        ZBuiltins.CompileValuePredCall(head.Text, cc, rb, form, resultStorage, label1, true);
                        rb.MarkLabel(label1);
                        return resultStorage;
                    }
                    else if (ZBuiltins.IsBuiltinPredCall(head.Text, zversion, argCount))
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
                    else if (ZBuiltins.IsBuiltinVoidCall(head.Text, zversion, argCount))
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
                    else if (ZBuiltins.IsBuiltinPredCall(head.Text, zversion, argCount))
                    {
                        ILabel dummy = rb.DefineLabel();
                        ZBuiltins.CompilePredCall(head.Text, cc, rb, form, dummy, true);
                        rb.MarkLabel(dummy);
                        return null;
                    }
                    else if (ZBuiltins.IsBuiltinValueCall(head.Text, zversion, argCount))
                    {
                        if (ZBuiltins.CompileValueCall(head.Text, cc, rb, form, null) == rb.Stack)
                            rb.EmitPopStack();
                        return null;
                    }
                    else if (ZBuiltins.IsBuiltinValuePredCall(head.Text, zversion, argCount))
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
                            Errors.CompError(cc.Context, form, "expected an atom after GVAL");
                            return wantResult ? cc.Game.Zero : null;
                        }
                        if (cc.Constants.TryGetValue(atom, out operand))
                            return operand;
                        if (cc.Globals.TryGetValue(atom, out global))
                            return global;
                        if (cc.Objects.TryGetValue(atom, out objbld))
                            return objbld;
                        if (cc.Routines.TryGetValue(atom, out routine))
                            return routine;
                        Errors.CompError(cc.Context, form, "undefined global or constant: {0}",
                            atom.ToStringContext(cc.Context, false));
                        return wantResult ? cc.Game.Zero : null;
                    case StdAtom.LVAL:
                        atom = form.Rest.First as ZilAtom;
                        if (atom == null)
                        {
                            Errors.CompError(cc.Context, form, "expected an atom after LVAL");
                            return wantResult ? cc.Game.Zero : null;
                        }
                        if (cc.Locals.TryGetValue(atom, out local))
                            return local;
                        Errors.CompError(cc.Context, form, "undefined local: {0}",
                            atom.ToStringContext(cc.Context, false));
                        return wantResult ? cc.Game.Zero : null;

                    case StdAtom.PROG:
                    case StdAtom.REPEAT:
                    case StdAtom.BIND:
                        return CompilePROG(cc, rb, form.Rest, wantResult, resultStorage,
                            head.StdAtom == StdAtom.REPEAT, head.StdAtom != StdAtom.BIND);

                    case StdAtom.COND:
                        return CompileCOND(cc, rb, form.Rest, wantResult, resultStorage);

                    case StdAtom.VERSION_P:
                        return CompileVERSION_P(cc, rb, form.Rest, wantResult, resultStorage);

                    case StdAtom.NOT:
                        if (form.Rest == null || form.Rest.First == null ||
                            (form.Rest.Rest != null && !form.Rest.Rest.IsEmpty))
                        {
                            Errors.CompError(cc.Context, form, "NOT requires exactly 1 argument");
                            return cc.Game.Zero;
                        }
                        resultStorage = resultStorage ?? rb.Stack;
                        label1 = rb.DefineLabel();
                        label2 = rb.DefineLabel();
                        CompileCondition(cc, rb, form.Rest.First, label1, true);
                        rb.EmitStore(resultStorage, cc.Game.One);
                        rb.Branch(label2);
                        rb.MarkLabel(label1);
                        rb.EmitStore(resultStorage, cc.Game.Zero);
                        rb.MarkLabel(label2);
                        return resultStorage;

                    case StdAtom.OR:
                    case StdAtom.AND:
                        return CompileBoolean(cc, rb, form.Rest, head.StdAtom == StdAtom.AND, wantResult, resultStorage);
                }

                // routine calls
                ZilObject obj = cc.Context.GetGlobalVal(head);
                if (obj is ZilRoutine)
                {
                    ZilRoutine rtn = (ZilRoutine)obj;

                    // check argument count
                    ZilObject[] args = form.Skip(1).ToArray();
                    if (args.Length < rtn.ArgSpec.MinArgCount ||
                        (rtn.ArgSpec.MaxArgCount > 0 && args.Length > rtn.ArgSpec.MaxArgCount))
                    {
                        Errors.CompError(cc.Context, form, ZilError.ArgCountMsg(
                            rtn.Name.ToString(),
                            rtn.ArgSpec.MinArgCount,
                            rtn.ArgSpec.MaxArgCount));
                        return wantResult ? cc.Game.Zero : null;
                    }

                    // compile routine call
                    result = wantResult ? (resultStorage ?? rb.Stack) : null;
                    using (Operands argOperands = Operands.Compile(cc, rb, args))
                    {
                        rb.EmitCall(cc.Routines[head], argOperands.ToArray(), result);
                    }
                    return result;
                }

                // unrecognized
                string msg;
                if (ZBuiltins.IsNearMatchBuiltin(head.Text, zversion, argCount, out msg))
                {
                    Errors.CompError(cc.Context, form, msg);
                }
                else
                {
                    Errors.CompError(cc.Context, form, "unrecognized routine or instruction: {0}",
                        head.ToStringContext(cc.Context, false));
                }
                return wantResult ? cc.Game.Zero : null;
            }
            catch (ZilError ex)
            {
                if (ex.SourceLine == null)
                    ex.SourceLine = form;
                throw;
            }
        }

        private static bool HasSideEffects(CompileCtx cc, ZilObject expr)
        {
            ZilForm form = expr as ZilForm;

            // only forms can have side effects
            if (form == null)
                return false;

            // malformed forms are errors anyway
            ZilAtom head = form.First as ZilAtom;
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

        private static IOperand CompileAsOperand(CompileCtx cc, IRoutineBuilder rb, ZilObject expr,
            IVariable suggestion = null)
        {
            IOperand constant = CompileConstant(cc, expr);
            if (constant != null)
                return constant;

            switch (expr.GetTypeAtom(cc.Context).StdAtom)
            {
                case StdAtom.FORM:
                    return CompileForm(cc, rb, (ZilForm)expr, true, suggestion ?? rb.Stack);

                case StdAtom.ATOM:
                    ZilAtom atom = (ZilAtom)expr;
                    if (cc.Globals.ContainsKey(atom))
                    {
                        Errors.CompWarning(cc.Context, expr as ISourceLine,
                            "bare atom '{0}' interpreted as global variable index; be sure this is right", atom);
                        return cc.Globals[atom].Indirect;
                    }
                    Errors.CompError(cc.Context, expr as ISourceLine,
                        "bare atom used as operand is not a global variable: {0}", atom);
                    return cc.Game.Zero;

                default:
                    throw new NotImplementedException();
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
        /// the result. Will only be called when <see cref="resultStorage"/> is <b>null</b> and
        /// the expression has no natural location.</param>
        /// <returns>The variable where the expression value was stored: always <see cref="resultStorage"/> if
        /// it is non-null and the expression is valid. Otherwise, may be a constant, or the natural
        /// location of the expression, or a temporary variable from <see cref="tempVarProvider"/>.</returns>
        private static IOperand CompileAsOperandWithBranch(CompileCtx cc, IRoutineBuilder rb, ZilObject expr,
            IVariable resultStorage, ILabel label, bool polarity, Func<IVariable> tempVarProvider = null)
        {
            System.Diagnostics.Debug.Assert(resultStorage != rb.Stack);
            System.Diagnostics.Debug.Assert(resultStorage != null || tempVarProvider != null);

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
            else if (type == StdAtom.FIX)
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
            else if (type != StdAtom.FORM)
            {
                var value = CompileConstant(cc, expr);
                if (value == null)
                {
                    Errors.CompError(cc.Context, expr as ZilForm, "unexpected expression in value+predicate context");
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
            ZilForm form = expr as ZilForm;
            ZilAtom head = form.First as ZilAtom;

            if (head == null)
            {
                Errors.CompError(cc.Context, form, "FORM must start with an atom");
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
            else if (ZBuiltins.IsBuiltinValueCall(head.Text, zversion, argCount))
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
            else if (ZBuiltins.IsBuiltinPredCall(head.Text, zversion, argCount))
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
            else if (ZBuiltins.IsBuiltinVoidCall(head.Text, zversion, argCount))
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
            result = CompileAsOperand(cc, rb, form, resultStorage);
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

        private static void CompileCondition(CompileCtx cc, IRoutineBuilder rb, ZilObject expr,
            ILabel label, bool polarity)
        {
            expr = expr.Expand(cc.Context);
            StdAtom type = expr.GetTypeAtom(cc.Context).StdAtom;

            if (type == StdAtom.FALSE)
            {
                if (polarity == false)
                    rb.Branch(label);
                return;
            }
            else if (type == StdAtom.FIX)
            {
                bool nonzero = ((ZilFix)expr).Value != 0;
                if (polarity == nonzero)
                    rb.Branch(label);
                return;
            }
            else if (type != StdAtom.FORM)
            {
                if (polarity == true)
                    rb.Branch(label);
                return;
            }

            // it's a FORM
            ZilForm form = expr as ZilForm;
            ZilAtom head = form.First as ZilAtom;

            if (head == null)
            {
                Errors.CompError(cc.Context, form, "FORM must start with an atom");
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
            else if (ZBuiltins.IsBuiltinValueCall(head.Text, zversion, argCount))
            {
                var result = ZBuiltins.CompileValueCall(head.Text, cc, rb, form, rb.Stack);
                rb.BranchIfZero(result, label, !polarity);
                return;
            }
            else if (ZBuiltins.IsBuiltinValuePredCall(head.Text, zversion, argCount))
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
            else if (ZBuiltins.IsBuiltinVoidCall(head.Text, zversion, argCount))
            {
                ZBuiltins.CompileVoidCall(head.Text, cc, rb, form);

                // void calls return true
                if (polarity == true)
                    rb.Branch(label);
                return;
            }

            // special cases
            IOperand op1;
            ZilObject[] args = form.Skip(1).ToArray();

            switch (head.StdAtom)
            {
                case StdAtom.NOT:
                    CompileCondition(cc, rb, args[0], label, !polarity);
                    break;

                case StdAtom.OR:
                case StdAtom.AND:
                    CompileBoolean(cc, rb, args, head.StdAtom == StdAtom.AND, label, polarity);
                    break;

                default:
                    op1 = CompileAsOperand(cc, rb, form);
                    rb.BranchIfZero(op1, label, !polarity);
                    break;
            }
        }

        private static void CompileBoolean(CompileCtx cc, IRoutineBuilder rb, ZilObject[] args, bool and,
            ILabel label, bool polarity)
        {
            if (args.Length == 0)
            {
                // <AND> is true, <OR> is false
                if (and == polarity)
                    rb.Branch(label);
            }
            else if (args.Length == 1)
            {
                CompileCondition(cc, rb, args[0], label, polarity);
            }
            else if (and == polarity)
            {
                // AND or NOR
                ILabel failure = rb.DefineLabel();
                for (int i = 0; i < args.Length - 1; i++)
                    CompileCondition(cc, rb, args[i], failure, !and);

                /* Historical note: ZILCH considered <AND ... <SET X 0>> to be true,
                 * even though <SET X 0> is false. We emulate the bug by compiling the
                 * last element as a statement instead of a condition when it fits
                 * this pattern. */
                ZilObject last = args[args.Length - 1];
                if (and && IsSetToZeroForm(last))
                {
                    Errors.CompWarning(cc.Context, (ISourceLine)last, "treating SET to 0 as true here");
                    CompileStmt(cc, rb, last, false);
                }
                else
                    CompileCondition(cc, rb, last, label, and);

                rb.MarkLabel(failure);
            }
            else
            {
                // NAND or OR
                for (int i = 0; i < args.Length - 1; i++)
                    CompileCondition(cc, rb, args[i], label, !and);

                /* Emulate the aforementioned ZILCH bug. */
                ZilObject last = args[args.Length - 1];
                if (and && IsSetToZeroForm(last))
                {
                    Errors.CompWarning(cc.Context, (ISourceLine)last, "treating SET to 0 as true here");
                    CompileStmt(cc, rb, last, false);
                }
                else
                    CompileCondition(cc, rb, last, label, !and);
            }
        }

        private static bool IsSetToZeroForm(ZilObject last)
        {
            ZilForm form = last as ZilForm;
            if (form == null)
                return false;

            ZilAtom atom = form.First as ZilAtom;
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

        private static IOperand CompileBoolean(CompileCtx cc, IRoutineBuilder rb, ZilList args,
            bool and, bool wantResult, IVariable resultStorage)
        {
            if (args.IsEmpty)
                return and ? cc.Game.One : cc.Game.Zero;

            if (args.Rest.IsEmpty)
            {
                if (wantResult)
                    return CompileAsOperand(cc, rb, args.First, resultStorage);

                if (args.First is ZilForm)
                    return CompileForm(cc, rb, (ZilForm)args.First, wantResult, resultStorage);

                return cc.Game.Zero;
            }

            IOperand result;

            if (wantResult)
            {
                ZilAtom tempAtom = ZilAtom.Parse("?TMP", cc.Context);
                ILabel lastLabel = rb.DefineLabel();
                IVariable tempVar = null;

                if (resultStorage == null)
                    resultStorage = rb.Stack;

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
                    ILabel nextLabel = rb.DefineLabel();

                    if (and)
                    {
                        // for AND we only need the result of the last expr; otherwise we only care about truth value
                        CompileCondition(cc, rb, args.First, nextLabel, true);
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

                result = CompileAsOperand(cc, rb, args.First, resultStorage);
                if (result != resultStorage)
                    rb.EmitStore(resultStorage, result);

                rb.MarkLabel(lastLabel);

                if (tempVar != null)
                    PopInnerLocal(cc, tempAtom);

                return resultStorage;
            }
            else
            {
                ILabel lastLabel = rb.DefineLabel();

                while (!args.Rest.IsEmpty)
                {
                    ILabel nextLabel = rb.DefineLabel();

                    CompileCondition(cc, rb, args.First, nextLabel, and);

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

        private static void CompilePRINTI(CompileCtx cc, IRoutineBuilder rb, ZilList args, bool ret)
        {
            if (args == null || !args.Rest.IsEmpty)
                throw new CompilerError("PRINTI/PRINTR: expected 1 arg");

            ZilObject value = args.First;

            if (value is ZilString)
                rb.EmitPrint(TranslateString(((ZilString)value).Text), ret);
            else
                throw new CompilerError("argument to PRINTI/PRINTR must be a string");
        }

        private static IOperand CompileREAD(CompileCtx cc, IRoutineBuilder rb, ZilList args,
            bool wantResult, IVariable resultStorage)
        {
            using (Operands operands = Operands.Compile(cc, rb, args.ToArray()))
            {
                IVariable result;
                if (cc.Context.ZEnvironment.ZVersion < 5)
                    result = null;
                else
                    result = wantResult ? (resultStorage ?? rb.Stack) : null;

                switch (operands.Count)
                {
                    case 1:
                        rb.EmitRead(operands[0], null, null, null, result);
                        break;
                    case 2:
                        rb.EmitRead(operands[0], operands[1], null, null, result);
                        break;
                    case 3:
                        rb.EmitRead(operands[0], operands[1], operands[2], null, result);
                        break;
                    case 4:
                        rb.EmitRead(operands[0], operands[1], operands[2], operands[3], result);
                        break;
                    default:
                        throw new CompilerError(null, "READ", 1, 4);
                }

                return wantResult ? (result ?? cc.Game.One) : null;
            }
        }

        private static IOperand CompileINPUT(CompileCtx cc, IRoutineBuilder rb, ZilList args,
            bool wantResult, IVariable resultStorage)
        {
            if (cc.Context.ZEnvironment.ZVersion < 4)
                throw new CompilerError("INPUT not supported in this version");

            using (Operands operands = Operands.Compile(cc, rb, args.Skip(1).ToArray()))
            {
                IVariable result = wantResult ? (resultStorage ?? rb.Stack) : null;

                if (!(args.First is ZilFix && ((ZilFix)args.First).Value == 1))
                    throw new CompilerError("INPUT: first arg must be 1");

                switch (operands.Count)
                {
                    case 0:
                        rb.EmitReadChar(null, null, result);
                        break;
                    case 1:
                        rb.EmitReadChar(operands[0], null, result);
                        break;
                    case 2:
                        rb.EmitReadChar(operands[0], operands[1], result);
                        break;
                    default:
                        throw new CompilerError(null, "INPUT", 1, 3);
                }

                return wantResult ? (result ?? cc.Game.One) : null;
            }
        }

        private static IOperand CompilePROG(CompileCtx cc, IRoutineBuilder rb, ZilList args,
            bool wantResult, IVariable resultStorage, bool repeat, bool catchy)
        {
            // NOTE: resultStorage is unused here, because PROG's result could come from
            // a RETURN statement (and REPEAT's result can *only* come from RETURN).
            // thus we have to return the result on the stack, because RETURN doesn't have
            // the context needed to put its result in the right place.

            if (args == null || args.First == null ||
                args.First.GetTypeAtom(cc.Context).StdAtom != StdAtom.LIST)
            {
                throw new CompilerError("expected binding list at start of PROG/REPEAT/BIND");
            }

            // add new locals, if any
            Queue<ZilAtom> innerLocals = new Queue<ZilAtom>();
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

                    case StdAtom.LIST:
                        ZilList list = (ZilList)obj;
                        if (list.First == null || list.Rest == null ||
                            list.Rest.First == null || (list.Rest.Rest != null && list.Rest.Rest.First != null))
                            throw new CompilerError("binding with value must be a 2-element list");
                        atom = list.First as ZilAtom;
                        ZilObject value = list.Rest.First;
                        if (atom == null || value == null)
                            throw new InterpreterError("invalid atom binding");
                        innerLocals.Enqueue(atom);
                        ILocalBuilder lb = PushInnerLocal(cc, rb, atom);
                        IOperand loc = CompileAsOperand(cc, rb, value, lb);
                        if (loc != lb)
                            rb.EmitStore(lb, loc);
                        break;

                    default:
                        throw new CompilerError("elements of binding list must be atoms or lists");
                }
            }

            ILabel oldAgain = null, oldReturn = null;
            BlockReturnState oldReturnState = 0;

            if (catchy)
            {
                oldAgain = cc.AgainLabel;
                oldReturn = cc.ReturnLabel;
                oldReturnState = cc.ReturnState;

                cc.AgainLabel = rb.DefineLabel();
                cc.ReturnLabel = rb.DefineLabel();
                cc.ReturnState = wantResult ? BlockReturnState.WantResult : 0;

                rb.MarkLabel(cc.AgainLabel);
            }

            try
            {
                // generate code for prog body
                args = args.Rest as ZilList;
                bool empty = (args.Rest == null);
                while (args != null && !args.IsEmpty)
                {
                    // only want the result of the last statement (if any)
                    bool wantThisResult = wantResult && !repeat && args.Rest.IsEmpty;
                    ZilForm form = args.First as ZilForm;
                    IOperand result;
                    if (form != null)
                    {
                        if (cc.WantDebugInfo)
                            cc.Game.DebugFile.MarkSequencePoint(rb,
                                new DebugLineRef(form.SourceFile, form.SourceLine, 1));

                        result = CompileForm(cc, rb, form,
                            wantThisResult,
                            wantThisResult ? rb.Stack : null);
                        if (wantThisResult && result != rb.Stack)
                            rb.EmitStore(rb.Stack, result);
                    }
                    else if (wantResult)
                    {
                        result = CompileConstant(cc, args.First);
                        if (result == null)
                            throw new CompilerError("unexpected value as statement");

                        rb.EmitStore(rb.Stack, result);
                    }

                    args = args.Rest as ZilList;
                }

                if (catchy)
                {
                    if (repeat)
                        rb.Branch(cc.AgainLabel);

                    if ((cc.ReturnState & BlockReturnState.Returned) != 0)
                        rb.MarkLabel(cc.ReturnLabel);
                }

                if (wantResult)
                {
                    // result is on the stack, unless the body was empty
                    if (!empty)
                        return rb.Stack;
                    else
                        return cc.Game.One;
                }
                else
                    return null;
            }
            finally
            {
                while (innerLocals.Count > 0)
                    PopInnerLocal(cc, innerLocals.Dequeue());

                if (catchy)
                {
                    cc.AgainLabel = oldAgain;
                    cc.ReturnLabel = oldReturn;
                    cc.ReturnState = oldReturnState;
                }
            }
        }

        private static ILocalBuilder PushInnerLocal(CompileCtx cc, IRoutineBuilder rb, ZilAtom atom)
        {
            string name = atom.Text;

            ILocalBuilder prev;
            if (cc.Locals.TryGetValue(atom, out prev))
            {
                Stack<ILocalBuilder> stk;
                if (cc.OuterLocals.TryGetValue(atom, out stk) == false)
                {
                    stk = new Stack<ILocalBuilder>();
                    cc.OuterLocals.Add(atom, stk);
                }
                stk.Push(prev);

                int num = 1;
                do
                {
                    name = atom.Text + "?" + num;
                    num++;
                } while (cc.Locals.ContainsKey(ZilAtom.Parse(name, cc.Context)));
            }

            ILocalBuilder cur;
            if (cc.SpareLocals.Count > 0)
                cur = cc.SpareLocals.Pop();
            else
                cur = rb.DefineLocal(name);

            cc.Locals[atom] = cur;
            return cur;
        }

        private static void PopInnerLocal(CompileCtx cc, ZilAtom atom)
        {
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

        private static IOperand CompileCOND(CompileCtx cc, IRoutineBuilder rb, ZilList clauses,
            bool wantResult, IVariable resultStorage)
        {
            ILabel nextLabel = rb.DefineLabel();
            ILabel endLabel = rb.DefineLabel();
            bool elsePart = false;

            if (resultStorage == null)
                resultStorage = rb.Stack;

            while (!clauses.IsEmpty)
            {
                ZilList clause = clauses.First as ZilList;
                clauses = clauses.Rest as ZilList;

                if (clause == null || clause.GetTypeAtom(cc.Context).StdAtom != StdAtom.LIST)
                    throw new CompilerError("all clauses in COND must be lists");

                ZilObject condition = clause.First;

                // if condition is always true (i.e. not a FORM or a FALSE), this is the "else" part
                switch (condition.GetTypeAtom(cc.Context).StdAtom)
                {
                    case StdAtom.FORM:
                        // must be evaluated
                        if (cc.WantDebugInfo)
                            cc.Game.DebugFile.MarkSequencePoint(rb,
                                new DebugLineRef(
                                    ((ZilForm)condition).SourceFile,
                                    ((ZilForm)condition).SourceLine, 1));
                        CompileCondition(cc, rb, condition, nextLabel, false);
                        break;

                    case StdAtom.FALSE:
                        // never true
                        //XXX warning message - clause will never be evaluated
                        continue;

                    default:
                        // always true
                        elsePart = true;
                        break;
                }

                // emit code for clause
                clause = clause.Rest as ZilList;
                if (clause.IsEmpty && wantResult)
                    rb.EmitStore(resultStorage, cc.Game.One);

                while (!clause.IsEmpty)
                {
                    // only want the result of the last statement (if any)
                    bool wantThisResult = wantResult && clause.Rest.IsEmpty;
                    ZilForm form = clause.First as ZilForm;
                    IOperand result;
                    if (form != null)
                    {
                        if (cc.WantDebugInfo)
                            cc.Game.DebugFile.MarkSequencePoint(rb,
                                new DebugLineRef(form.SourceFile, form.SourceLine, 1));
                        
                        result = CompileForm(cc, rb, form,
                            wantThisResult,
                            wantThisResult ? resultStorage : null);
                        if (wantThisResult && result != resultStorage)
                            rb.EmitStore(resultStorage, result);
                    }
                    else if (wantResult)
                    {
                        result = CompileConstant(cc, clause.First);
                        if (result == null)
                            throw new CompilerError("unexpected value as statement");

                        rb.EmitStore(resultStorage, result);
                    }

                    clause = clause.Rest as ZilList;
                }

                // jump to end
                if (!clauses.IsEmpty || (wantResult && !elsePart))
                    rb.Branch(endLabel);

                rb.MarkLabel(nextLabel);

                if (elsePart)
                {
                    if (!clauses.IsEmpty)
                    {
                        //XXX warning message - following clauses will never be evaluated
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

        private static IOperand CompileVERSION_P(CompileCtx cc, IRoutineBuilder rb, ZilList clauses,
            bool wantResult, IVariable resultStorage)
        {
            if (resultStorage == null)
                resultStorage = rb.Stack;

            while (!clauses.IsEmpty)
            {
                ZilList clause = clauses.First as ZilList;
                clauses = clauses.Rest as ZilList;

                if (clause == null || clause.GetTypeAtom(cc.Context).StdAtom != StdAtom.LIST)
                    throw new CompilerError("all clauses in VERSION? must be lists");

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
                                throw new CompilerError("unrecognized atom in VERSION? (must be ZIP, EZIP, XZIP, YZIP, ELSE/T)");
                        }
                        break;

                    case StdAtom.FIX:
                        condVersion = ((ZilFix)condition).Value;
                        if (condVersion < 3 || condVersion > 8)
                            throw new CompilerError("version number out of range (must be 3-6)");
                        break;

                    default:
                        throw new CompilerError("conditions in in VERSION? clauses must be ATOMs");
                }

                // does this clause match?
                if (condVersion == cc.Context.ZEnvironment.ZVersion || condVersion == 0)
                {
                    // emit code for clause
                    clause = clause.Rest as ZilList;
                    if (clause.IsEmpty && wantResult)
                        rb.EmitStore(resultStorage, cc.Game.One);

                    while (!clause.IsEmpty)
                    {
                        // only want the result of the last statement (if any)
                        bool wantThisResult = wantResult && clause.Rest.IsEmpty;
                        ZilForm form = clause.First as ZilForm;
                        IOperand result;
                        if (form != null)
                        {
                            if (cc.WantDebugInfo)
                                cc.Game.DebugFile.MarkSequencePoint(rb,
                                    new DebugLineRef(form.SourceFile, form.SourceLine, 1));

                            result = CompileForm(cc, rb, form,
                                wantThisResult,
                                wantThisResult ? resultStorage : null);
                            if (wantThisResult && result != resultStorage)
                                rb.EmitStore(resultStorage, result);
                        }
                        else if (wantResult)
                        {
                            result = CompileConstant(cc, clause.First);
                            if (result == null)
                                throw new CompilerError("unexpected value as statement");

                            rb.EmitStore(resultStorage, result);
                        }

                        clause = clause.Rest as ZilList;
                    }

                    if (condVersion == 0 && !clauses.IsEmpty)
                    {
                        //XXX warning message - following clauses will never be evaluated
                    }

                    return wantResult ? resultStorage : null;
                }
            }

            // no matching clauses
            if (wantResult)
                rb.EmitStore(resultStorage, cc.Game.Zero);

            return wantResult ? resultStorage : null;
        }

        private static void PreBuildObject(CompileCtx cc, ZilModelObject model)
        {
            try
            {
                // create property builders for all properties on this object as needed,
                // and set up P?FOO constants for them. also create vocabulary words for 
                // SYNONYM and ADJECTIVE property values, and constants for FLAGS values.
                foreach (ZilList prop in model.Properties)
                {
                    // check for a PROPDEF pattern
                    //XXX

                    // if no pattern, then the first element must be an atom identifying the property
                    ZilAtom atom = prop.First as ZilAtom;
                    if (atom == null)
                    {
                        Errors.CompError(cc.Context, model, "property specification must start with an atom");
                        continue;
                    }

                    // exclude phony built-in properties
                    bool phony;
                    if (IsExitToken(cc.Context, prop.Rest.First) ||
                        cc.Context.ZEnvironment.Directions.Contains(atom))
                    {
                        // it's a direction
                        phony = false;

                        // could be a new implicitly defined direction
                        if (!cc.Context.ZEnvironment.Directions.Contains(atom))
                        {
                            cc.Context.ZEnvironment.Directions.Add(atom);
                            cc.Context.ZEnvironment.GetVocabDirection(atom);
                        }
                    }
                    else
                    {
                        switch (atom.StdAtom)
                        {
                            case StdAtom.DESC:
                            case StdAtom.IN:
                            case StdAtom.LOC:
                            case StdAtom.FLAGS:
                                phony = true;
                                break;
                            default:
                                phony = false;
                                break;
                        }
                    }

                    if (!phony)
                        DefineProperty(cc, atom);

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
                                    DefineWord(cc, cc.Context.ZEnvironment.GetVocabNoun(atom));
                                }
                                catch (ZilError ex)
                                {
                                    if (ex.SourceLine == null)
                                        ex.SourceLine = model;
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
                                    DefineWord(cc, cc.Context.ZEnvironment.GetVocabAdjective(atom));
                                }
                                catch (ZilError ex)
                                {
                                    if (ex.SourceLine == null)
                                        ex.SourceLine = model;
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
                                    DefineFlag(cc, atom);
                                }
                                catch (ZilError ex)
                                {
                                    if (ex.SourceLine == null)
                                        ex.SourceLine = model;
                                    cc.Context.HandleError(ex);
                                }
                            }
                            break;
                    }
                }
            }
            catch (ZilError ex)
            {
                if (ex.SourceLine == null)
                    ex.SourceLine = model;
                throw;
            }
        }

        private static void BuildObject(CompileCtx cc, ZilModelObject model, IObjectBuilder ob)
        {
            foreach (ZilList prop in model.Properties)
            {
                IPropertyBuilder pb;
                ITableBuilder tb;
                int length = 0;

                // check for a PROPDEF pattern
                //XXX

                // if no pattern, then the first element must be an atom identifying the property
                ZilAtom atom = prop.First as ZilAtom;
                if (atom == null)
                {
                    Errors.CompError(cc.Context, model, "property specification must start with an atom");
                    continue;
                }

                // check for a PUTPROP giving a hand-coded property builder
                ZilObject propspec = cc.Context.GetProp(atom, cc.Context.GetStdAtom(StdAtom.PROPSPEC));
                if (propspec != null)
                {
                    //XXX
                    throw new NotImplementedException();
                }

                // built-in property builder, so at least one value has to follow the atom (except for FLAGS)
                ZilObject value = prop.Rest.First;
                if (value == null)
                {
                    if (atom.StdAtom != StdAtom.FLAGS)
                        Errors.CompError(cc.Context, model, "property has no value: " + atom.ToString());
                    continue;
                }

                // check for directions
                if (cc.Context.ZEnvironment.Directions.Contains(atom))
                {
                    pb = cc.Properties[atom];
                    ZilObject[] spec = prop.Rest.ToArray();
                    StdAtom[] pattern = new StdAtom[spec.Length];

                    for (int i = 0; i < spec.Length; i++)
                        if (spec[i] is ZilAtom)
                            pattern[i] = ((ZilAtom)spec[i]).StdAtom;

                    if (pattern.Length == 1)
                    {
                        if (spec[0].GetTypeAtom(cc.Context).StdAtom == StdAtom.STRING)
                        {
                            // NEXIT
                            BuildNEXIT(cc, ob, pb, spec[0]);
                            continue;
                        }
                    }
                    else if (pattern.Length == 2)
                    {
                        if (pattern[0] == StdAtom.TO)
                        {
                            // UEXIT
                            BuildUEXIT(cc, ob, pb, spec[1]);
                            continue;
                        }
                        else if (pattern[0] == StdAtom.PER)
                        {
                            // FEXIT
                            BuildFEXIT(cc, ob, pb, spec[1]);
                            continue;
                        }
                        else if (pattern[0] == StdAtom.SORRY)
                        {
                            // NEXIT
                            BuildNEXIT(cc, ob, pb, spec[1]);
                            continue;
                        }
                    }
                    else if (pattern.Length == 4)
                    {
                        if (pattern[0] == StdAtom.TO && pattern[2] == StdAtom.IF)
                        {
                            // CEXIT
                            BuildCEXIT(cc, ob, pb, spec[1], spec[3], null);
                            continue;
                        }
                    }
                    else if (pattern.Length == 6)
                    {
                        if (pattern[0] == StdAtom.TO && pattern[2] == StdAtom.IF &&
                            pattern[4] == StdAtom.IS && pattern[5] == StdAtom.OPEN)
                        {
                            // DEXIT
                            BuildDEXIT(cc, ob, pb, spec[1], spec[3], null);
                            continue;
                        }
                        else if (pattern[0] == StdAtom.TO && pattern[2] == StdAtom.IF &&
                            pattern[4] == StdAtom.ELSE)
                        {
                            // CEXIT
                            BuildCEXIT(cc, ob, pb, spec[1], spec[3], spec[5]);
                            continue;
                        }
                    }
                    else if (pattern.Length == 8)
                    {
                        if (pattern[0] == StdAtom.TO && pattern[2] == StdAtom.IF &&
                            pattern[4] == StdAtom.IS && pattern[5] == StdAtom.OPEN &&
                            pattern[6] == StdAtom.ELSE)
                        {
                            // DEXIT
                            BuildDEXIT(cc, ob, pb, spec[1], spec[3], spec[7]);
                            continue;
                        }
                    }
                }

                // check for special cases
                switch (atom.StdAtom)
                {
                    case StdAtom.DESC:
                        if (value.GetTypeAtom(cc.Context).StdAtom != StdAtom.STRING)
                        {
                            Errors.CompError(cc.Context, model, "value for DESC property must be a string");
                            continue;
                        }
                        ob.DescriptiveName = value.ToStringContext(cc.Context, true);
                        continue;

                    case StdAtom.IN:
                    case StdAtom.LOC:
                        atom = value as ZilAtom;
                        if (atom == null)
                        {
                            Errors.CompError(cc.Context, model, "value for IN/LOC property must be an atom");
                            continue;
                        }
                        IObjectBuilder parent;
                        if (cc.Objects.TryGetValue(atom, out parent) == false)
                        {
                            Errors.CompError(cc.Context, model,
                                "no such object for IN/LOC property: " + atom.ToString());
                            continue;
                        }
                        ob.Parent = parent;
                        ob.Sibling = parent.Child;
                        parent.Child = ob;
                        continue;

                    case StdAtom.FLAGS:
                        foreach (ZilObject obj in prop.Rest)
                        {
                            atom = obj as ZilAtom;
                            if (atom == null)
                            {
                                Errors.CompError(cc.Context, model, "values for FLAGS property must be atoms");
                                break;
                            }

                            IFlagBuilder fb = cc.Flags[atom];
                            ob.AddFlag(fb);
                        }
                        continue;

                    case StdAtom.SYNONYM:
                        tb = ob.AddComplexProperty(cc.Properties[atom]);
                        foreach (ZilObject obj in prop.Rest)
                        {
                            atom = obj as ZilAtom;
                            if (atom == null)
                            {
                                Errors.CompError(cc.Context, model, "values for SYNONYM property must be atoms");
                                break;
                            }

                            Word word = cc.Context.ZEnvironment.GetVocabNoun(atom);
                            IWordBuilder wb = cc.Vocabulary[word];
                            tb.AddShort(wb);
                            length += 2;
                        }
                        break;

                    case StdAtom.ADJECTIVE:
                        tb = ob.AddComplexProperty(cc.Properties[atom]);
                        foreach (ZilObject obj in prop.Rest)
                        {
                            atom = obj as ZilAtom;
                            if (atom == null)
                            {
                                Errors.CompError(cc.Context, model, "values for ADJECTIVE property must be atoms");
                                break;
                            }

                            Word word = cc.Context.ZEnvironment.GetVocabAdjective(atom);
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

                    case StdAtom.GLOBAL:
                        if (cc.Context.ZEnvironment.ZVersion == 3)
                        {
                            tb = ob.AddComplexProperty(cc.Properties[atom]);
                            foreach (ZilObject obj in prop.Rest)
                            {
                                atom = obj as ZilAtom;
                                if (atom == null)
                                {
                                    Errors.CompError(cc.Context, model, "values for GLOBAL property must be atoms");
                                    break;
                                }

                                IObjectBuilder ob2;
                                if (cc.Objects.TryGetValue(atom, out ob2) == false)
                                {
                                    Errors.CompError(cc.Context, model, "values for GLOBAL property must be object names");
                                    break;
                                }

                                tb.AddByte(ob2);
                                length++;
                            }
                            break;
                        }
                        else
                        {
                            goto default;
                        }

                    default:
                        // nothing special, just one or more words
                        pb = cc.Properties[atom];
                        if (prop.Rest.Rest.IsEmpty)
                        {
                            ob.AddWordProperty(pb, CompileConstant(cc, value));
                            length = 2;
                        }
                        else
                        {
                            tb = ob.AddComplexProperty(pb);
                            foreach (ZilObject obj in prop.Rest)
                            {
                                tb.AddShort(CompileConstant(cc, obj));
                                length += 2;
                            }
                        }
                        break;
                }

                // check property length
                if (length > cc.Game.MaxPropertyLength)
                    Errors.CompError(cc.Context, model, "property '{0}' is too long (max {1} bytes)",
                        prop.First.ToStringContext(cc.Context, true), cc.Game.MaxPropertyLength);
            }

            //XXX debug line refs for objects
            if (cc.WantDebugInfo)
                cc.Game.DebugFile.MarkObject(ob, new DebugLineRef(), new DebugLineRef());
        }

        private static void BuildCEXIT(CompileCtx cc, IObjectBuilder ob, IPropertyBuilder prop,
            ZilObject dest, ZilObject global, ZilObject message)
        {
            ZilAtom destAtom = dest as ZilAtom;
            IObjectBuilder destObject;
            if (destAtom == null || cc.Objects.TryGetValue(destAtom, out destObject) == false)
                throw new CompilerError("CEXIT must specify an object name");

            ZilAtom globalAtom = global as ZilAtom;
            IGlobalBuilder gb;
            if (globalAtom == null || cc.Globals.TryGetValue(globalAtom, out gb) == false)
                throw new CompilerError("CEXIT must specify a global variable name");

            if (message != null && message.GetTypeAtom(cc.Context).StdAtom != StdAtom.STRING)
                throw new CompilerError("CEXIT must specify a failure message or omit the ELSE part");

            string msgStr = (message == null) ? null :
                message.ToStringContext(cc.Context, true);

            ITableBuilder tb = ob.AddComplexProperty(prop);
            if (cc.Context.ZEnvironment.ZVersion < 4)
            {
                tb.AddByte(destObject);
                tb.AddByte(gb);
                if (msgStr == null)
                    tb.AddShort(0);
                else
                    tb.AddShort(cc.Game.MakeOperand(TranslateString(msgStr)));
            }
            else
            {
                tb.AddShort(destObject);
                if (msgStr == null)
                    tb.AddShort(0);
                else
                    tb.AddShort(cc.Game.MakeOperand(TranslateString(msgStr)));
                tb.AddByte(gb);
            }
        }

        private static void BuildDEXIT(CompileCtx cc, IObjectBuilder ob, IPropertyBuilder prop,
            ZilObject dest, ZilObject door, ZilObject message)
        {
            IObjectBuilder destObject, doorObject;
            ZilAtom atom = dest as ZilAtom;
            if (atom == null || cc.Objects.TryGetValue(atom, out destObject) == false)
                throw new CompilerError("DEXIT must supply a destination object name");

            atom = door as ZilAtom;
            if (atom == null || cc.Objects.TryGetValue(atom, out doorObject) == false)
                throw new CompilerError("DEXIT must supply a door object name");

            if (message != null && message.GetTypeAtom(cc.Context).StdAtom != StdAtom.STRING)
                throw new CompilerError("DEXIT must specify a failure message or omit the ELSE part");

            string msgStr = (message == null) ? null :
                message.ToStringContext(cc.Context, true);

            ITableBuilder tb = ob.AddComplexProperty(prop);
            if (cc.Context.ZEnvironment.ZVersion < 4)
            {
                tb.AddByte(destObject);
                tb.AddByte(doorObject);
                if (msgStr == null)
                    tb.AddShort(0);
                else
                    tb.AddShort(cc.Game.MakeOperand(TranslateString(msgStr)));
                tb.AddByte(0);
            }
            else
            {
                tb.AddShort(destObject);
                tb.AddShort(doorObject);
                if (msgStr == null)
                    tb.AddShort(0);
                else
                    tb.AddShort(cc.Game.MakeOperand(TranslateString(msgStr)));
            }
        }

        private static void BuildFEXIT(CompileCtx cc, IObjectBuilder ob, IPropertyBuilder prop, 
            ZilObject routine)
        {
            ZilAtom atom = routine as ZilAtom;
            IRoutineBuilder rtn;
            if (atom == null || cc.Routines.TryGetValue(atom, out rtn) == false)
                throw new CompilerError("FEXIT must supply a routine name");

            ITableBuilder tb = ob.AddComplexProperty(prop);
            if (cc.Context.ZEnvironment.ZVersion < 4)
            {
                tb.AddShort(rtn);
                tb.AddByte(0);
            }
            else
            {
                tb.AddShort(rtn);
                tb.AddShort(0);
            }
        }

        private static void BuildUEXIT(CompileCtx cc, IObjectBuilder ob, IPropertyBuilder prop, 
            ZilObject dest)
        {
            ZilAtom destAtom = dest as ZilAtom;
            IObjectBuilder destObject;
            if (destAtom == null || cc.Objects.TryGetValue(destAtom, out destObject) == false)
                throw new CompilerError("UEXIT must specify an object name");

            if (cc.Context.ZEnvironment.ZVersion < 4)
            {
                ob.AddByteProperty(prop, destObject);
            }
            else
            {
                ob.AddWordProperty(prop, destObject);
            }
        }

        private static void BuildNEXIT(CompileCtx cc, IObjectBuilder ob, IPropertyBuilder prop,
            ZilObject message)
        {
            if (message.GetTypeAtom(cc.Context).StdAtom != StdAtom.STRING)
                throw new CompilerError("NEXIT must specify a string");

            string msgStr = message.ToStringContext(cc.Context, true);
            IOperand msgOperand = cc.Game.MakeOperand(TranslateString(msgStr));

            if (cc.Context.ZEnvironment.ZVersion < 4)
            {
                ob.AddWordProperty(prop, msgOperand);
            }
            else
            {
                ITableBuilder tb = ob.AddComplexProperty(prop);
                tb.AddShort(msgOperand);
                tb.AddByte(0);
            }
        }

        private static bool IsExitToken(Context ctx, ZilObject value)
        {
            ZilAtom atom = value as ZilAtom;
            if (atom != null)
                switch (atom.StdAtom)
                {
                    case StdAtom.TO:
                    case StdAtom.PER:
                    case StdAtom.SORRY:
                        return true;
                }

            return false;
        }
    }
}
