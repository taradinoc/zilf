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
using System.Linq;
using System.Text;
using JetBrains.Annotations;
using Zilf.Diagnostics;
using Zilf.Interpreter;
using Zilf.Interpreter.Values;
using Zilf.Language;
using Zilf.ZModel.Vocab;

namespace Zilf.ZModel
{
    class Syntax : IProvideSourceLine
    {
        public readonly int NumObjects;
        [NotNull]
        public readonly IWord Verb;
        [CanBeNull]
        public readonly IWord Preposition1;
        [CanBeNull]
        public readonly IWord Preposition2;
        public readonly byte Options1, Options2;
        [CanBeNull]
        public readonly ZilAtom FindFlag1, FindFlag2;
        [NotNull]
        public readonly ZilAtom Action;
        [CanBeNull]
        public readonly ZilAtom Preaction;
        [NotNull]
        public readonly ZilAtom ActionName;
        public readonly IList<ZilAtom> Synonyms;

        static readonly ZilAtom[] EmptySynonyms = new ZilAtom[0];

        public Syntax(ISourceLine src, [NotNull] IWord verb, int numObjects, [CanBeNull] IWord prep1, [CanBeNull] IWord prep2,
            byte options1, byte options2, [CanBeNull] ZilAtom findFlag1, [CanBeNull] ZilAtom findFlag2,
            [NotNull] ZilAtom action, [CanBeNull] ZilAtom preaction, [NotNull] ZilAtom actionName,
            [ItemNotNull] [CanBeNull] IEnumerable<ZilAtom> synonyms = null)
        {
            Contract.Requires(verb != null);
            Contract.Requires(numObjects >= 0 & numObjects <= 2);
            Contract.Requires(action != null);

            SourceLine = src;

            Verb = verb;
            NumObjects = numObjects;
            Preposition1 = prep1;
            Preposition2 = prep2;
            Options1 = options1;
            Options2 = options2;
            FindFlag1 = findFlag1;
            FindFlag2 = findFlag2;
            Action = action;
            Preaction = preaction;
            ActionName = actionName;

            if (synonyms == null)
                Synonyms = EmptySynonyms;
            else
                Synonyms = new List<ZilAtom>(synonyms).AsReadOnly();
        }

        /// <exception cref="InterpreterError">The syntax definition is invalid.</exception>
        [NotNull]
        public static Syntax Parse(ISourceLine src, [NotNull] IEnumerable<ZilObject> definition, [NotNull] Context ctx)
        {
            // TODO: refactor this method or convert to a builder class
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
                    if (obj is ZilAtom atom && atom.StdAtom != StdAtom.Eq)
                    {
                        verb = atom;
                    }
                    else
                    {
                        throw new InterpreterError(InterpreterMessages.Missing_0_In_1, "verb", "syntax definition");
                    }
                }
                else if (!rightSide)
                {
                    // left side:
                    //   [[prep] OBJECT [(FIND ...)] [(options...) ...] [[prep] OBJECT [(FIND ...)] [(options...)]]]
                    switch (obj)
                    {
                        case ZilAtom atom:
                            HandleLeftSideAtom(atom);
                            break;

                        case ZilList list:
                            HandleLeftSideList(list);
                            break;

                        default:
                            throw new InterpreterError(obj, InterpreterMessages.Unrecognized_0_1, "value in syntax definition", obj);
                    }
                }
                else
                {
                    HandleRightSide(obj);
                }
            }

            return ValidateAndBuild();

            // helpers...
            void HandleLeftSideList(ZilList list)
            {
                if (list.First is ZilAtom atom)
                {
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
                                ctx.HandleError(new InterpreterError(
                                    src,
                                    InterpreterMessages.Ignoring_List_Of_Flags_In_Syntax_Definition_With_No_Preceding_OBJECT));
                                break;

                            default:
                                if (syns != null)
                                    throw new InterpreterError(InterpreterMessages.Too_Many_0_In_Syntax_Definition, "synonym lists");

                                syns = list;
                                break;
                        }
                    }
                    else
                    {
                        if (atom.StdAtom == StdAtom.FIND)
                        {
                            if ((numObjects == 1 && find1 != null) || find2 != null)
                                throw new InterpreterError(InterpreterMessages.Too_Many_0_In_Syntax_Definition, "FIND lists");
                            if (numObjects == 1)
                                find1 = list;
                            else
                                find2 = list;
                        }
                        else
                        {
                            if (numObjects == 1)
                            {
                                bits1 = bits1 != null ? new ZilList(bits1.Concat(list)) : list;
                            }
                            else
                            {
                                bits2 = bits2 != null ? new ZilList(bits2.Concat(list)) : list;
                            }
                        }
                    }
                }
                else
                {
                    throw new InterpreterError(InterpreterMessages.Element_0_Of_1_In_2_Must_Be_3, 1, "list", "syntax definition", "an atom");
                }
            }

            void HandleLeftSideAtom(ZilAtom atom)
            {
                switch (atom.StdAtom)
                {
                    case StdAtom.OBJECT:
                        numObjects++;
                        if (numObjects > 2)
                            throw new InterpreterError(InterpreterMessages.Too_Many_0_In_Syntax_Definition, "OBJECTs");
                        break;

                    case StdAtom.Eq:
                        rightSide = true;
                        break;

                    default:
                        var numPreps = prep2 != null ? 2 : prep1 != null ? 1 : 0;
                        if (numPreps == 2 || numPreps > numObjects)
                        {
                            var error = new InterpreterError(InterpreterMessages.Too_Many_0_In_Syntax_Definition, "prepositions");

                            if (numObjects < 2)
                                error = error.Combine(new InterpreterError(InterpreterMessages.Did_You_Mean_To_Separate_Them_With_OBJECT));

                            throw error;
                        }
                        if (numObjects == 0)
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

            void HandleRightSide(ZilObject obj)
            {
                // right side:
                //   action [preaction [action-name]]
                if (obj is ZilAtom atom)
                {
                    if (atom.StdAtom == StdAtom.Eq)
                        throw new InterpreterError(InterpreterMessages.Too_Many_0_In_Syntax_Definition, "'='");
                }
                else if (obj is ZilFalse)
                {
                    atom = null;
                }
                else
                {
                    throw new InterpreterError(InterpreterMessages._0_In_1_Must_Be_2, "values after '='", "syntax definition", "FALSE or atoms");
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
                        throw new InterpreterError(InterpreterMessages.Too_Many_0_In_Syntax_Definition, "values after '='");
                }

                rhsCount++;
            }

            Syntax ValidateAndBuild()
            {
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

                var verbWord = ctx.ZEnvironment.GetVocabVerb(verb, src);
                var word1 = prep1 == null ? null : ctx.ZEnvironment.GetVocabSyntaxPreposition(prep1, src);
                var word2 = prep2 == null ? null : ctx.ZEnvironment.GetVocabSyntaxPreposition(prep2, src);
                var flags1 = ScopeFlags.Parse(bits1, ctx);
                var flags2 = ScopeFlags.Parse(bits2, ctx);
                var findFlag1 = ParseFindFlag(find1);
                var findFlag2 = ParseFindFlag(find2);
                IEnumerable<ZilAtom> synAtoms = null;

                if (syns != null)
                {
                    if (!syns.All(s => s is ZilAtom))
                        throw new InterpreterError(InterpreterMessages._0_In_1_Must_Be_2, "verb synonyms", "syntax definition", "atoms");

                    synAtoms = syns.Cast<ZilAtom>();
                }

                if (action == null)
                {
                    throw new InterpreterError(InterpreterMessages.Missing_0_In_1, "action routine", "syntax definition");
                }

                if (actionName == null)
                {
                    var sb = new StringBuilder(action.Text);
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
                    var actionNameStr = actionName.Text;
                    if (!actionNameStr.StartsWith("V?", StringComparison.Ordinal))
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
        }

        [CanBeNull]
        [ContractAnnotation("null => null; notnull => notnull")]
        static ZilAtom ParseFindFlag([CanBeNull] ZilList list)
        {
            if (list == null)
                return null;

            if (list.Rest?.First is ZilAtom atom && list.Rest.Rest?.IsEmpty == true)
                return atom;

            throw new InterpreterError(
                InterpreterMessages._0_Expected_1_After_2,
                "SYNTAX",
                "a single atom",
                "FIND");
        }

        public override string ToString()
        {
            var sb = new StringBuilder();

            // verb
            sb.Append(Verb.Atom);

            // object clauses
            var items = new[] {
                new { Prep = Preposition1, Find = FindFlag1, Opts = Options1 },
                new { Prep = Preposition2, Find = FindFlag2, Opts = Options2 }
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

        public ISourceLine SourceLine { get; }
    }
}