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
using Zilf.Interpreter;
using Zilf.Interpreter.Values;
using Zilf.Language;
using Zilf.Diagnostics;

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

            public IList<ZilObject> Captures { get; private set; }

            public MatchResult()
            {
                Captures = new List<ZilObject>();
            }
        }

        abstract class Token
        {
            public abstract bool Match(Context ctx, ZilObject input, MatchResult result);
        }

        class AtomToken : Token
        {
            public IList<ZilAtom> Atoms { get; private set; }

            public AtomToken()
            {
                this.Atoms = new List<ZilAtom>();
            }

            public override bool Match(Context ctx, ZilObject input, MatchResult result)
            {
                foreach (var atom in this.Atoms)
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
            public ZilObject Pattern { get; set; }

            public override bool Match(Context ctx, ZilObject input, MatchResult result)
            {
                if (Decl.Check(ctx, input, Pattern))
                {
                    result.Captures.Add(input);
                    return true;
                }

                return false;
            }
        }

        class GvalToken : Token
        {
            public ZilAtom Atom { get; set; }

            public override bool Match(Context ctx, ZilObject input, MatchResult result)
            {
                var form = input as ZilForm;
                if (form != null)
                {
                    if (form.First is ZilAtom && ((ZilAtom)form.First).StdAtom == StdAtom.GVAL)
                    {
                        return form.Rest.First == this.Atom;
                    }
                }

                return false;
            }
        }

        readonly Token[] tokens;
        readonly ZilForm outputForm;

        TellPattern(Token[] tokens, ZilForm outputForm)
        {
            this.tokens = tokens;
            this.outputForm = outputForm;
        }

        public static IEnumerable<TellPattern> Parse(IEnumerable<ZilObject> spec, Context ctx)
        {
            var tokensSoFar = new List<Token>();
            int capturesSoFar = 0;

            foreach (var zo in spec)
            {
                ZilList list;
                ZilForm form;
                ZilAdecl adecl;
                AtomToken atomToken;

                var type = zo.GetTypeAtom(ctx).StdAtom;
                switch (type)
                {
                    case StdAtom.LIST:
                        // one or more atoms to introduce the token
                        atomToken = new AtomToken();

                        list = (ZilList)zo;
                        if (list.First == null || !list.All(e => e is ZilAtom))
                            throw new InterpreterError(zo.SourceLine, InterpreterMessages.Lists_In_TELL_Token_Specs_Must_Contain_Atoms);
                        foreach (ZilAtom atom in list)
                            atomToken.Atoms.Add(atom);

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
                        adecl = (ZilAdecl)zo;
                        if (!(adecl.First is ZilAtom) || ((ZilAtom)adecl.First).StdAtom != StdAtom.Times)
                            throw new InterpreterError(InterpreterMessages.Left_Side_Of_ADECL_In_TELL_Token_Spec_Must_Be_star);
                        tokensSoFar.Add(new DeclToken { Pattern = adecl.Second });
                        capturesSoFar++;
                        break;

                    case StdAtom.FORM:
                        // <GVAL atom> to match an exact GVAL, or any other FORM to specify the pattern's output
                        form = (ZilForm)zo;
                        if (form.IsGVAL())
                        {
                            var atom = form.Rest.First as ZilAtom;
                            if (atom == null)
                                throw new InterpreterError(form.SourceLine, InterpreterMessages.Malformed_GVAL_In_TELL_Token_Spec);
                            tokensSoFar.Add(new GvalToken { Atom = atom });
                        }
                        else
                        {
                            // validate the output FORM
                            int lvalCount = 0;
                            foreach (var elem in form)
                            {
                                if (elem.IsLVAL())
                                {
                                    lvalCount++;
                                }
                                else if (!IsSimpleOutputElement(elem))
                                {
                                    throw new InterpreterError(form, InterpreterMessages.Value_Too_Fancy_For_TELL_Output_Template_0, elem.ToStringContext(ctx, false));
                                }
                            }

                            if (lvalCount != capturesSoFar)
                                throw new InterpreterError(form,
                                    InterpreterMessages.Expected_0_LVALs_In_TELL_Output_Template_But_Found_1, capturesSoFar, lvalCount);

                            var pattern = new TellPattern(tokensSoFar.ToArray(), form);
                            tokensSoFar.Clear();
                            capturesSoFar = 0;
                            yield return pattern;
                        }
                        break;

                    default:
                        throw new InterpreterError(zo.SourceLine, InterpreterMessages.Unexpected_Type_In_TELL_Token_Spec_0, zo.GetTypeAtom(ctx));
                }
            }

            if (tokensSoFar.Count != 0)
            {
                throw new InterpreterError(InterpreterMessages.TELL_Token_Spec_Ends_With_An_Unterminated_Pattern);
            }
        }

        public int Length
        {
            get { return tokens.Length; }
        }

        public ITellPatternMatchResult Match(IList<ZilObject> input, int startIndex, Context ctx, ISourceLine src)
        {
            Contract.Requires(input != null);
            Contract.Requires(startIndex >= 0 && startIndex < input.Count);
            Contract.Requires(ctx != null);
            Contract.Requires(src != null);
            Contract.Ensures(Contract.Result<ITellPatternMatchResult>() != null);

            var result = new MatchResult();
            result.Matched = false;

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
            foreach (var element in this.outputForm)
            {
                if (element is ZilForm)
                {
                    var first = ((ZilForm)element).First;
                    if (first is ZilAtom && ((ZilAtom)first).StdAtom == StdAtom.LVAL)
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

        static bool IsSimpleOutputElement(ZilObject obj)
        {
            Contract.Requires(obj != null);

            if (obj is ZilAtom || obj is ZilFix || obj is ZilString || obj is ZilFalse)
                return true;

            if (obj.IsLVAL() || obj.IsGVAL())
                return true;

            return false;
        }
    }
}
