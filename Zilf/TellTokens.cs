using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Zilf
{
    interface ITellPatternMatchResult
    {
        bool Matched { get; }
        ZilForm Output { get; }
    }

    class TellPattern
    {
        private class MatchResult : ITellPatternMatchResult
        {
            public bool Matched { get; set; }
            public ZilForm Output { get; set; }

            public IList<ZilObject> Captures { get; private set; }

            public MatchResult()
            {
                Captures = new List<ZilObject>();
            }
        }

        private abstract class Token
        {
            public abstract bool Match(ZilObject input, MatchResult result);
        }

        private class AtomToken : Token
        {
            public IList<ZilAtom> Atoms { get; private set; }

            public AtomToken()
            {
                this.Atoms = new List<ZilAtom>();
            }

            public override bool Match(ZilObject input, MatchResult result)
            {
                foreach (var atom in this.Atoms)
                    if (input == atom)
                        return true;

                return false;
            }
        }

        private class AnyToken : Token
        {
            public override bool Match(ZilObject input, MatchResult result)
            {
                result.Captures.Add(input);
                return true;
            }
        }

        private class TypeToken : Token
        {
            public override bool Match(ZilObject input, MatchResult result)
            {
                throw new NotImplementedException();
            }
        }

        private class GvalToken : Token
        {
            public ZilAtom Atom { get; set; }

            public override bool Match(ZilObject input, MatchResult result)
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

        private readonly Token[] tokens;
        private readonly ZilForm outputForm;

        private TellPattern(Token[] tokens, ZilForm outputForm)
        {
            this.tokens = tokens;
            this.outputForm = outputForm;
        }

        public static IEnumerable<TellPattern> Parse(IEnumerable<ZilObject> spec, Context ctx)
        {
            var tokensSoFar = new List<Token>();

            foreach (var zo in spec)
            {
                ZilList list;
                ZilForm form;
                AtomToken atomToken;

                var type = zo.GetTypeAtom(ctx).StdAtom;
                switch (type)
                {
                    case StdAtom.LIST:
                        // one or more atoms to introduce the token
                        atomToken = new AtomToken();

                        list = (ZilList)zo;
                        if (list.First == null || !list.All(e => e is ZilAtom))
                            throw new InterpreterError(zo as ISourceLine, "lists in TELL token specs must contain atoms");
                        foreach (ZilAtom atom in list)
                            atomToken.Atoms.Add(atom);

                        if (tokensSoFar.Count != 0)
                            throw new InterpreterError("lists in TELL token specs must come at the beginning of a pattern");

                        tokensSoFar.Add(atomToken);
                        break;
                    case StdAtom.ATOM:
                        // * to capture any value, or any other atom to introduce the token
                        if (((ZilAtom)zo).StdAtom == StdAtom.Times)
                        {
                            tokensSoFar.Add(new AnyToken());
                        }
                        else
                        {
                            atomToken = new AtomToken();
                            atomToken.Atoms.Add((ZilAtom)zo);

                            if (tokensSoFar.Count != 0)
                                throw new InterpreterError("lists and atoms in TELL token specs must come at the beginning");

                            tokensSoFar.Add(atomToken);
                        }
                        break;

                    case StdAtom.ADECL:
                        // *:DECL to capture any value that matches the decl
                        //XXX
                        throw new NotImplementedException("ADECL in TELL token spec");

                    case StdAtom.FORM:
                        // <GVAL atom> to match an exact GVAL, or any other FORM to specify the pattern's output
                        form = (ZilForm)zo;
                        if (form.First is ZilAtom && ((ZilAtom)form.First).StdAtom == StdAtom.GVAL)
                        {
                            var atom = form.Rest.First as ZilAtom;
                            if (atom == null)
                                throw new InterpreterError(form, "malformed GVAL in TELL token spec");
                            tokensSoFar.Add(new GvalToken { Atom = atom });
                        }
                        else
                        {
                            // TODO: validate the number of capturing tokens vs. number of LVALs in the output FORM
                            var pattern = new TellPattern(tokensSoFar.ToArray(), form);
                            tokensSoFar.Clear();
                            yield return pattern;
                        }
                        break;

                    default:
                        throw new InterpreterError(zo as ISourceLine, "unexpected type in TELL token spec: " + zo.GetTypeAtom(ctx));
                }
            }

            if (tokensSoFar.Count != 0)
            {
                throw new InterpreterError("TELL token spec ends with an unterminated pattern");
            }
        }

        public int Length
        {
            get { return tokens.Length; }
        }

        public ITellPatternMatchResult Match(IList<ZilObject> input, int startIndex)
        {
            var result = new MatchResult();
            result.Matched = false;

            if (input.Count - startIndex < tokens.Length)
            {
                return result;
            }

            for (int i = 0; i < tokens.Length; i++)
            {
                if (!tokens[i].Match(input[startIndex + i], result))
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
            result.Output = new ZilForm(outputElements);

            return result;
        }
    }
}
