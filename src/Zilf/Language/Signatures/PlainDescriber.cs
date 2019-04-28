/* Copyright 2010-2018 Jesse McGrew
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
using System.Linq;
using System.Text;
using JetBrains.Annotations;
using Zilf.Interpreter;
using Zilf.Interpreter.Values;

namespace Zilf.Language.Signatures
{
    class PlainDescriber : ISignatureVisitor, IConstraintVisitor
    {
        [NotNull]
        readonly StringBuilder sb;

        PlainDescriber([NotNull] StringBuilder sb)
        {
            this.sb = sb;
        }

        [NotNull]
        public static string Describe([NotNull] ISignature signature)
        {
            var sb = new StringBuilder();
            var visitor = new PlainDescriber(sb);
            visitor.VisitWithDelimiter(signature.Parts, " ");
            return sb.ToString();
        }

        [NotNull]
        static string Describe([NotNull] ISignaturePart part)
        {
            var sb = new StringBuilder();
            var visitor = new PlainDescriber(sb);
            part.Accept(visitor);
            return sb.ToString();
        }

        [NotNull]
        static string Describe([NotNull] Constraint constraint)
        {
            var sb = new StringBuilder();
            var visitor = new PlainDescriber(sb);
            constraint.Accept(visitor);
            return sb.ToString();
        }

        void VisitWithDelimiter([ItemNotNull] [NotNull] IEnumerable<ISignaturePart> parts, [NotNull] string delimiter)
        {
            bool first = true;

            foreach (var i in parts)
            {
                if (first)
                {
                    first = false;
                }
                else
                {
                    sb.Append(delimiter);
                }

                i.Accept(this);
            }
        }

        public void Visit(AdeclPart part)
        {
            part.Left.Accept(this);
            sb.Append(':');
            part.Right.Accept(this);
        }

        public void Visit(AlternativesPart part)
        {
            sb.Append('{');
            VisitWithDelimiter(part.Alternatives, " | ");
            sb.Append('}');
        }

        public void Visit(AnyPart part)
        {
            sb.Append(part.Name);
        }

        public void Visit(ConstrainedPart part)
        {
            var left = Describe(part.Inner);
            sb.Append(left);

            var right = Describe(part.Constraint);
            if (right == left)
                return;

            sb.Append(':');
            sb.Append(right);
        }

        public void Visit(FormPart part)
        {
            if (part.Parts.Count == 2 && part.Parts[0].GetDescendants().Any(d => d.Name == "quote-atom"))
            {
                System.Diagnostics.Debugger.Break();
            }

            sb.Append('<');
            VisitWithDelimiter(part.Parts, " ");
            sb.Append('>');
        }

        public void Visit(ListPart part)
        {
            sb.Append('(');
            VisitWithDelimiter(part.Parts, " ");
            sb.Append(')');
        }

        public void Visit(LiteralPart part)
        {
            sb.Append(part.Text);
        }

        public void Visit(OptionalPart part)
        {
            sb.Append('[');
            part.Inner.Accept(this);
            sb.Append(']');
        }

        public void Visit(QuotedPart part)
        {
            sb.Append('\'');
            part.Inner.Accept(this);
        }

        public void Visit(SequencePart part)
        {
            VisitWithDelimiter(part.Parts, " ");
        }

        public void Visit(VarArgsPart part)
        {
            part.Inner.Accept(this);
            sb.Append(" ...");
        }

        public void VisitAnyObjectConstraint()
        {
            sb.Append("any");
        }

        public void VisitApplicableConstraint()
        {
            sb.Append("applicable");
        }

        public void VisitBooleanConstraint()
        {
            sb.Append("boolean");
        }

        public void VisitConjunctionConstraint(IEnumerable<Constraint> parts)
        {
            var query = parts.Select(Describe).Distinct().OrderBy(s => s);
            sb.Append(string.Join("-and-", query));
        }

        public void VisitDeclConstraint(ZilObject pattern)
        {
            sb.Append(pattern);
        }

        public void VisitDisjunctionConstraint(IEnumerable<Constraint> alts)
        {
            var query = alts.Select(Describe).Distinct().OrderBy(s => s);
            sb.Append(string.Join("-and-", query));
        }

        public void VisitForbiddenConstraint()
        {
            sb.Append(";NOPE");
        }

        public void VisitPrimTypeConstraint(PrimType primType)
        {
            sb.Append("primtype-");
            sb.Append(primType.ToString().ToLowerInvariant());
        }

        public void VisitStructuredConstraint()
        {
            sb.Append("structured");
        }

        public void VisitTypeConstraint(StdAtom type)
        {
            sb.Append(type.ToString().ToLowerInvariant());
        }
    }
}