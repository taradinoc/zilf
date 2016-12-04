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
using System.Collections.Generic;
using System.Diagnostics.Contracts;
using System.Linq;
using System.Text;
using Zilf.Interpreter;
using Zilf.Interpreter.Values;
using Zilf.Language;
using Zilf.ZModel.Vocab;
using Zilf.Diagnostics;

namespace Zilf.ZModel
{
    class Syntax : IProvideSourceLine
    {
        public readonly int NumObjects;
        public readonly IWord Verb, Preposition1, Preposition2;
        public readonly byte Options1, Options2;
        public readonly ZilAtom FindFlag1, FindFlag2;
        public readonly ZilAtom Action, Preaction, ActionName;
        public readonly IList<ZilAtom> Synonyms;

        private static readonly ZilAtom[] EmptySynonyms = new ZilAtom[0];

        public Syntax(ISourceLine src, IWord verb, int numObjects, IWord prep1, IWord prep2,
            byte options1, byte options2, ZilAtom findFlag1, ZilAtom findFlag2,
            ZilAtom action, ZilAtom preaction, ZilAtom actionName,
            IEnumerable<ZilAtom> synonyms = null)
        {
            Contract.Requires(verb != null);
            Contract.Requires(numObjects >= 0 & numObjects <= 2);
            Contract.Requires(action != null);

            this.SourceLine = src;

            this.Verb = verb;
            this.NumObjects = numObjects;
            this.Preposition1 = prep1;
            this.Preposition2 = prep2;
            this.Options1 = options1;
            this.Options2 = options2;
            this.FindFlag1 = findFlag1;
            this.FindFlag2 = findFlag2;
            this.Action = action;
            this.Preaction = preaction;
            this.ActionName = actionName;

            if (synonyms == null)
                this.Synonyms = EmptySynonyms;
            else
                this.Synonyms = new List<ZilAtom>(synonyms).AsReadOnly();
        }

        public static Syntax Parse(ISourceLine src, IEnumerable<ZilObject> definition, Context ctx)
        {
            Contract.Requires(definition != null);
            Contract.Requires(ctx != null);
            Contract.Ensures(Contract.Result<Syntax>() != null);

            int numObjects = 0;
            ZilAtom verb = null, prep1 = null, prep2 = null;
            ZilAtom action = null, preaction = null, actionName = null;
            ZilList bits1 = null, find1 = null, bits2 = null, find2 = null, syns = null;
            bool rightSide = false;
            int rhsCount = 0;

            // main parsing
            foreach (ZilObject obj in definition)
            {
                if (verb == null)
                {
                    ZilAtom atom = obj as ZilAtom;
                    if (atom == null || atom.StdAtom == StdAtom.Eq)
                        throw new InterpreterError(InterpreterMessages.Missing_Verb_In_Syntax_Definition);

                    verb = atom;
                }
                else if (!rightSide)
                {
                    // left side:
                    //   [[prep] OBJECT [(FIND ...)] [(options...) ...] [[prep] OBJECT [(FIND ...)] [(options...)]]]
                    ZilAtom atom = obj as ZilAtom;
                    if (atom != null)
                    {
                        switch (atom.StdAtom)
                        {
                            case StdAtom.OBJECT:
                                numObjects++;
                                if (numObjects > 2)
                                    throw new InterpreterError(InterpreterMessages.Too_Many_OBJECT_In_Syntax_Definition);
                                break;

                            case StdAtom.Eq:
                                rightSide = true;
                                break;

                            default:
                                var numPreps = prep2 != null ? 2 : prep1 != null ? 1 : 0;
                                if (numPreps == 2 || numPreps > numObjects)
                                {
                                    if (numObjects < 2)
                                    {
                                        throw new InterpreterError(InterpreterMessages.Too_Many_Prepositions_In_Syntax_Definition_Try_Defining_Another_Object);
                                    }
                                    else
                                    {
                                        throw new InterpreterError(InterpreterMessages.Too_Many_Prepositions_In_Syntax_Definition);
                                    }
                                }
                                else if (numObjects == 0)
                                {
                                    prep1 = atom;
                                }
                                else
                                {
                                    prep2 = atom;
                                }
                                break;
                        }
                    }
                    else
                    {
                        ZilList list = obj as ZilList;
                        if (list != null && list.GetTypeAtom(ctx).StdAtom == StdAtom.LIST)
                        {
                            atom = list.First as ZilAtom;
                            if (atom == null)
                                throw new InterpreterError(InterpreterMessages.List_In_Syntax_Definition_Must_Start_With_An_Atom);

                            if (numObjects == 0)
                            {
                                // could be a list of synonyms, but could also be a mistake (scope/find flags in the wrong place)
                                switch (atom.StdAtom)
                                {
                                    case StdAtom.FIND:
                                    case StdAtom.TAKE:
                                    case StdAtom.HAVE:
                                    case StdAtom.MANY:
                                    case StdAtom.HELD:
                                    case StdAtom.CARRIED:
                                    case StdAtom.ON_GROUND:
                                    case StdAtom.IN_ROOM:
                                        Errors.TerpWarning(ctx, src, "ignoring list of flags in syntax definition with no preceding OBJECT");
                                        break;

                                    default:
                                        if (syns != null)
                                            throw new InterpreterError(InterpreterMessages.Too_Many_Synonym_Lists_In_Syntax_Definition);

                                        syns = list;
                                        break;
                                }
                            }
                            else
                            {
                                if (atom.StdAtom == StdAtom.FIND)
                                {
                                    if ((numObjects == 1 && find1 != null) || find2 != null)
                                        throw new InterpreterError(InterpreterMessages.Too_Many_FIND_Lists_In_Syntax_Definition);
                                    else if (numObjects == 1)
                                        find1 = list;
                                    else
                                        find2 = list;
                                }
                                else
                                {
                                    if (numObjects == 1)
                                    {
                                        if (bits1 != null)
                                            bits1 = new ZilList(Enumerable.Concat(bits1, list));
                                        else
                                            bits1 = list;
                                    }
                                    else
                                    {
                                        if (bits2 != null)
                                            bits2 = new ZilList(Enumerable.Concat(bits2, list));
                                        else
                                            bits2 = list;
                                    }
                                }
                            }
                        }
                        else
                            throw new InterpreterError(InterpreterMessages.Unexpected_Value_In_Syntax_Definition);
                    }
                }
                else
                {
                    // right side:
                    //   action [preaction [action-name]]
                    ZilAtom atom = obj as ZilAtom;
                    if (atom != null)
                    {
                        if (atom.StdAtom == StdAtom.Eq)
                            throw new InterpreterError(InterpreterMessages.Too_Many_EQ_In_Syntax_Definition);
                    }
                    else if (!(obj is ZilFalse))
                    {
                        throw new InterpreterError(InterpreterMessages.Values_After_EQ_Must_Be_FALSE_Or_Atoms);
                    }

                    switch (rhsCount)
                    {
                        case 0:
                            action = atom;
                            break;

                        case 1:
                            preaction = atom;
                            break;

                        case 2:
                            actionName = atom;
                            break;

                        default:
                            throw new InterpreterError(InterpreterMessages.Too_Many_Values_After_EQ_In_Syntax_Definition);
                    }

                    rhsCount++;
                }
            }

            // validation
            Contract.Assume(numObjects <= 2);
            if (numObjects < 1)
            {
                prep1 = null;
                find1 = null;
                bits1 = null;
            }
            if (numObjects < 2)
            {
                prep2 = null;
                find2 = null;
                bits2 = null;
            }

            IWord verbWord = ctx.ZEnvironment.GetVocabVerb(verb, src);
            IWord word1 = (prep1 == null) ? null : ctx.ZEnvironment.GetVocabSyntaxPreposition(prep1, src);
            IWord word2 = (prep2 == null) ? null : ctx.ZEnvironment.GetVocabSyntaxPreposition(prep2, src);
            byte flags1 = ScopeFlags.Parse(bits1, ctx);
            byte flags2 = ScopeFlags.Parse(bits2, ctx);
            ZilAtom findFlag1 = ParseFindFlag(find1);
            ZilAtom findFlag2 = ParseFindFlag(find2);
            IEnumerable<ZilAtom> synAtoms = null;

            if (syns != null)
            {
                if (!syns.All(s => s is ZilAtom))
                    throw new InterpreterError(InterpreterMessages.Verb_Synonyms_Must_Be_Atoms);

                synAtoms = syns.Cast<ZilAtom>();
            }

            if (action == null)
            {
                throw new InterpreterError(InterpreterMessages.Action_Routine_Must_Be_Specified);
            }

            if (actionName == null)
            {
                var sb = new StringBuilder(action.ToString());
                if (sb.Length > 2 && sb[0] == 'V' && sb[1] == '-')
                {
                    sb[1] = '?';
                }
                else
                {
                    sb.Insert(0, "V?");
                }

                actionName = ZilAtom.Parse(sb.ToString(), ctx);
            }
            else
            {
                var actionNameStr = actionName.ToString();
                if (!actionNameStr.StartsWith("V?"))
                {
                    actionName = ZilAtom.Parse("V?" + actionNameStr, ctx);
                }
            }

            return new Syntax(
            src,
            verbWord, numObjects,
            word1, word2, flags1, flags2, findFlag1, findFlag2,
            action, preaction, actionName, synAtoms);
        }

        private static ZilAtom ParseFindFlag(ZilList list)
        {
            if (list == null)
                return null;

            ZilAtom atom;
            if (list.IsEmpty || list.Rest.IsEmpty || !list.Rest.Rest.IsEmpty ||
            (atom = list.Rest.First as ZilAtom) == null)
                throw new InterpreterError(InterpreterMessages.FIND_Must_Be_Followed_By_A_Single_Atom);

            return atom;
        }

        public override string ToString()
        {
            StringBuilder sb = new StringBuilder();

            // verb
            sb.Append(Verb.Atom);

            // object clauses
            var items = new[] {
                new { Prep = Preposition1, Find = FindFlag1, Opts = Options1 },
                new { Prep = Preposition2, Find = FindFlag2, Opts = Options2 },
            };

            foreach (var item in items.Take(NumObjects))
            {
                if (item.Prep != null)
                {
                    sb.Append(' ');
                    sb.Append(item.Prep.Atom);
                }

                sb.Append(" OBJECT");

                if (item.Find != null)
                {
                    sb.Append(" (FIND ");
                    sb.Append(item.Find);
                    sb.Append(')');
                }

                // TODO: unparse scope flags
                sb.Append(" (");
                sb.Append(item.Opts);
                sb.Append(')');
            }

            // actions
            sb.Append(" = ");
            sb.Append(Action);
            if (Preaction != null)
            {
                sb.Append(' ');
                sb.Append(Preaction);
            }

            return sb.ToString();
        }

        public ISourceLine SourceLine { get; set; }
    }
}