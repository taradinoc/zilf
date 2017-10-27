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

using System.Collections.Generic;
using System.Diagnostics.Contracts;
using System.Linq;
using JetBrains.Annotations;
using Zilf.Diagnostics;
using Zilf.Interpreter;
using Zilf.Interpreter.Values;
using Zilf.Language;

namespace Zilf.ZModel
{
    interface ITellPatternMatchResult
    {
        bool Matched { get; }
        ZilForm Output { get; }
    }

    class TellPattern
    {
        class MatchResult : ITellPatternMatchResult
        {
            public bool Matched { get; set; }
            public ZilForm Output { get; set; }

            public IList<ZilObject> Captures { get; }

            public MatchResult()
            {
                Captures = new List<ZilObject>();
            }
        }

        [ContractClass(typeof(TokenContract))]
        abstract class Token
        {
            public abstract bool Match([NotNull] Context ctx, [NotNull] ZilObject input, [NotNull] MatchResult result);
        }

        class AtomToken : Token
        {
            public IList<ZilAtom> Atoms { get; }

            public AtomToken()
            {
                Atoms = new List<ZilAtom>();
            }

            public override bool Match(Context ctx, ZilObject input, MatchResult result)
            {
                foreach (var atom in Atoms)
                    if (input == atom)
                        return true;

                return false;
            }
        }

        class AnyToken : Token
        {
            public override bool Match(Context ctx, ZilObject input, MatchResult result)
            {
                result.Captures.Add(input);
                return true;
            }
        }

        class DeclToken : Token
        {
            // ReSharper disable once MemberCanBePrivate.Local
            public ZilObject Pattern { get; set; }

            public override bool Match(Context ctx, ZilObject input, MatchResult result)
            {
                if (!Decl.Check(ctx, input, Pattern))
                    return false;

                result.Captures.Add(input);
                return true;
            }
        }

        class GvalToken : Token
        {
            [CanBeNull]
            // ReSharper disable once MemberCanBePrivate.Local
            public ZilAtom Atom { get; set; }

            public override bool Match(Context ctx, ZilObject input, MatchResult result)
            {
                return input is ZilForm form
                    && form.IsGVAL(out var inputAtom)
                    && inputAtom == Atom;
            }
        }

        readonly Token[] tokens;
        readonly ZilForm outputForm;

        TellPattern(Token[] tokens, ZilForm outputForm)
        {
            this.tokens = tokens;
            this.outputForm = outputForm;
        }

        /// <exception cref="InterpreterError">The pattern syntax is invalid.</exception>
        public static IEnumerable<TellPattern> Parse([NotNull] IEnumerable<ZilObject> spec)
        {
            Contract.Requires(spec != null);
            var tokensSoFar = new List<Token>();
            int capturesSoFar = 0;

            foreach (var zo in spec)
            {
                AtomToken atomToken;

                var type = zo.StdTypeAtom;
                switch (type)
                {
                    case StdAtom.LIST:
                        // one or more atoms to introduce the token
                        atomToken = new AtomToken();

                        var list = (ZilList)zo;
                        if (list.IsEmpty || !list.All(e => e is ZilAtom))
                        {
                            throw new InterpreterError(
                                zo.SourceLine,
                                InterpreterMessages._0_In_1_Must_Be_2,
                                "lists",
                                "TELL token specs",
                                "lists of atoms");
                        }

                        foreach (var o in list.Cast<ZilAtom>())
                            atomToken.Atoms.Add(o);

                        if (tokensSoFar.Count != 0)
                            throw new InterpreterError(InterpreterMessages.Lists_And_Atoms_In_TELL_Token_Specs_Must_Come_At_The_Beginning);

                        tokensSoFar.Add(atomToken);
                        break;

                    case StdAtom.ATOM:
                        // * to capture any value, or any other atom to introduce the token
                        if (((ZilAtom)zo).StdAtom == StdAtom.Times)
                        {
                            tokensSoFar.Add(new AnyToken());
                            capturesSoFar++;
                        }
                        else
                        {
                            atomToken = new AtomToken();
                            atomToken.Atoms.Add((ZilAtom)zo);

                            if (tokensSoFar.Count != 0)
                                throw new InterpreterError(InterpreterMessages.Lists_And_Atoms_In_TELL_Token_Specs_Must_Come_At_The_Beginning);

                            tokensSoFar.Add(atomToken);
                        }
                        break;

                    case StdAtom.ADECL:
                        // *:DECL to capture any value that matches the decl
                        var adecl = (ZilAdecl)zo;
                        if (!(adecl.First is ZilAtom adeclAtom) || adeclAtom.StdAtom != StdAtom.Times)
                            throw new InterpreterError(
                                InterpreterMessages._0_Must_Be_1,
                                "left side of ADECL in TELL token spec",
                                "'*'");
                        tokensSoFar.Add(new DeclToken { Pattern = adecl.Second });
                        capturesSoFar++;
                        break;

                    case StdAtom.FORM:
                        // <GVAL atom> to match an exact GVAL, or any other FORM to specify the pattern's output
                        var form = (ZilForm)zo;
                        if (form.IsGVAL(out var gvAtom))
                        {
                            tokensSoFar.Add(new GvalToken { Atom = gvAtom });
                        }
                        else
                        {
                            // validate the output FORM
                            int lvalCount = 0;
                            foreach (var elem in form)
                            {
                                if (elem.IsLVAL(out _))
                                {
                                    lvalCount++;
                                }
                                else if (!IsSimpleOutputElement(elem))
                                {
                                    throw new InterpreterError(
                                        form,
                                        InterpreterMessages.Unrecognized_0_1,
                                        "value in TELL output template",
                                        elem);
                                }
                            }

                            if (lvalCount != capturesSoFar)
                                throw new InterpreterError(
                                    form,
                                    InterpreterMessages.Expected_0_LVAL0s_In_TELL_Output_Template_But_Found_1,
                                    capturesSoFar,
                                    lvalCount);

                            var pattern = new TellPattern(tokensSoFar.ToArray(), form);
                            tokensSoFar.Clear();
                            capturesSoFar = 0;
                            yield return pattern;
                        }
                        break;

                    default:
                        throw new InterpreterError(
                            zo.SourceLine,
                            InterpreterMessages.Unrecognized_0_1,
                            "value in TELL token spec",
                            zo);
                }
            }

            if (tokensSoFar.Count != 0)
            {
                throw new InterpreterError(InterpreterMessages.TELL_Token_Spec_Ends_With_An_Unterminated_Pattern);
            }
        }

        public int Length => tokens.Length;

        [NotNull]
        public ITellPatternMatchResult Match([NotNull] IList<ZilObject> input, int startIndex, [NotNull] Context ctx, [NotNull] ISourceLine src)
        {
            Contract.Requires(input != null);
            Contract.Requires(startIndex >= 0 && startIndex < input.Count);
            Contract.Requires(ctx != null);
            Contract.Requires(src != null);
            Contract.Ensures(Contract.Result<ITellPatternMatchResult>() != null);

            var result = new MatchResult { Matched = false };

            if (input.Count - startIndex < tokens.Length)
            {
                return result;
            }

            for (int i = 0; i < tokens.Length; i++)
            {
                if (!tokens[i].Match(ctx, input[startIndex + i], result))
                {
                    return result;
                }
            }

            result.Matched = true;
            var outputElements = new List<ZilObject>();
            int nextCapture = 0;
            foreach (var element in outputForm)
            {
                if (element is ZilForm form)
                {
                    if (form.First is ZilAtom atom && atom.StdAtom == StdAtom.LVAL)
                    {
                        outputElements.Add(result.Captures[nextCapture++]);
                        continue;
                    }
                }

                outputElements.Add(element);
            }
            result.Output = new ZilForm(outputElements) { SourceLine = src };

            return result;
        }

        static bool IsSimpleOutputElement([NotNull] ZilObject obj)
        {
            Contract.Requires(obj != null);

            if (obj is ZilAtom || obj is ZilFix || obj is ZilString || obj is ZilFalse)
                return true;

            if (obj.IsLVAL(out _) || obj.IsGVAL(out _))
                return true;

            return false;
        }

        [ContractClassFor(typeof(Token))]
        abstract class TokenContract : Token
        {
            public override bool Match(Context ctx, ZilObject input, MatchResult result)
            {
                Contract.Requires(ctx != null);
                Contract.Requires(input != null);
                Contract.Requires(result != null);
                return default(bool);
            }
        }
    }
}
