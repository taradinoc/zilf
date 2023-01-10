/* Copyright 2010-2023 Tara McGrew
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
using System.Linq;
using Newtonsoft.Json.Linq;
using Zilf.Interpreter;
using Zilf.Interpreter.Values;

namespace Zilf.Language.Signatures
{
    static class VisitingExtensions
    {
        public static T AcceptForValue<T>(this ISignaturePart part, SignatureVisitorWithValue<T> visitor)
            where T : class
        {
            var result = visitor.Run(part);
            System.Diagnostics.Debug.Assert(result != null);
            return result;
        }
    }

    abstract class SignatureVisitorWithValue<T> : ISignatureVisitor
        where T : class
    {
        T? result;

        internal T? Run(ISignaturePart part)
        {
            part.Accept(this);
            PostProcess(part, ref result);
            return result;
        }

        [SuppressMessage("ReSharper", "UnusedParameter.Global")]
        protected virtual void PostProcess(ISignaturePart part, ref T? pendingResult)
        {
            // by default, nada
        }

        protected abstract T Visit(AdeclPart part);
        protected abstract T Visit(AlternativesPart part);
        protected abstract T Visit(AnyPart part);
        protected abstract T Visit(ConstrainedPart part);
        protected abstract T Visit(FormPart part);
        protected abstract T Visit(ListPart part);
        protected abstract T Visit(LiteralPart part);
        protected abstract T Visit(OptionalPart part);
        protected abstract T Visit(QuotedPart part);
        protected abstract T Visit(SequencePart part);
        protected abstract T Visit(VarArgsPart part);

        #region ISignatureVisitor implementation

        void ISignatureVisitor.Visit(AdeclPart part) => result = Visit(part);
        void ISignatureVisitor.Visit(AlternativesPart part) => result = Visit(part);
        void ISignatureVisitor.Visit(AnyPart part) => result = Visit(part);
        void ISignatureVisitor.Visit(ConstrainedPart part) => result = Visit(part);
        void ISignatureVisitor.Visit(FormPart part) => result = Visit(part);
        void ISignatureVisitor.Visit(ListPart part) => result = Visit(part);
        void ISignatureVisitor.Visit(LiteralPart part) => result = Visit(part);
        void ISignatureVisitor.Visit(OptionalPart part) => result = Visit(part);
        void ISignatureVisitor.Visit(QuotedPart part) => result = Visit(part);
        void ISignatureVisitor.Visit(SequencePart part) => result = Visit(part);
        void ISignatureVisitor.Visit(VarArgsPart part) => result = Visit(part);

        #endregion
    }

    sealed class JsonDescriber : SignatureVisitorWithValue<JObject>
    {
        static readonly JsonDescriber Instance = new();

        JsonDescriber()
        {
        }

        public static JObject Describe(ISignature signature)
        {
            var parts = signature.Parts.Select(p => p.AcceptForValue(Instance));
            var result = new JObject
            {
                ["args"] = new JArray(parts),
                ["minArgs"] = signature.MinArgs,
            };
            if (signature.MaxArgs != null)
            {
                result["maxArgs"] = signature.MaxArgs;
            }

            // TODO: make visitor methods for this?
            switch (signature)
            {
                case SubrSignature _:
                    result["context"] = "mdl";
                    break;

                case ZBuiltinSignature bs:
                    result["context"] = "zcode";
                    result["minVersion"] = bs.MinVersion;
                    result["maxVersion"] = bs.MaxVersion;

                    if (bs.ReturnPart.Constraint != Constraint.AnyObject)
                    {
                        var retPart = bs.ReturnPart.AcceptForValue(Instance);
                        retPart.Remove("name");
                        result["returns"] = retPart;
                    }
                    break;

                default:
                    throw new ArgumentException($"Unexpected signature class: {signature.GetType()}");
            }

            return result;
        }

        protected override void PostProcess(ISignaturePart part, ref JObject? pendingResult)
        {
            if (pendingResult != null && part.Name != null)
            {
                pendingResult["name"] = part.Name;
            }
        }

        protected override JObject Visit(VarArgsPart part)
        {
            var result = new JObject { ["$rest"] = part.Inner.AcceptForValue(this) };
            if (part.Required)
            {
                result["required"] = true;
            }
            return result;
        }

        protected override JObject Visit(AdeclPart part)
        {
            return new JObject
            {
                ["type"] = "ADECL",
                ["elements"] = new JArray(part.Left.AcceptForValue(this), part.Right.AcceptForValue(this))
            };
        }

        protected override JObject Visit(AlternativesPart part)
        {
            return new JObject
            {
                ["$or"] = new JArray(part.Alternatives.Select(a => a.AcceptForValue(this)))
            };
        }

        protected override JObject Visit(AnyPart part)
        {
            return new JObject();
        }

        protected override JObject Visit(ConstrainedPart part)
        {
            var result = part.Inner.AcceptForValue(this);
            result["constraint"] = ConstraintDescriber.Describe(part.Constraint);
            return result;
        }

        protected override JObject Visit(FormPart part)
        {
            return new JObject
            {
                ["type"] = "FORM",
                ["elements"] = new JArray(part.Parts.Select(p => p.AcceptForValue(this)))
            };
        }

        protected override JObject Visit(ListPart part)
        {
            return new JObject
            {
                ["type"] = "LIST",
                ["elements"] = new JArray(part.Parts.Select(p => p.AcceptForValue(this)))
            };
        }

        protected override JObject Visit(LiteralPart part)
        {
            return new JObject { ["$literal"] = part.Text };
        }

        protected override JObject Visit(OptionalPart part)
        {
            return new JObject { ["$opt"] = part.Inner.AcceptForValue(this) };
        }

        protected override JObject Visit(QuotedPart part)
        {
            var result = part.Inner.AcceptForValue(this);
            result["eval"] = false;
            return result;
        }

        protected override JObject Visit(SequencePart part)
        {
            return new JObject
            {
                ["$seq"] = new JArray(part.Parts.Select(p => p.AcceptForValue(this)))
            };
        }

        class ConstraintDescriber : IConstraintVisitor
        {
            public static JObject? Describe(Constraint constraint)
            {
                var describer = new ConstraintDescriber();
                constraint.Accept(describer);
                return describer.result;
            }

            JObject? result;

            public void VisitAnyObjectConstraint()
            {
                result = null;
            }

            public void VisitApplicableConstraint()
            {
                result = new JObject { ["constraint"] = "applicable" };
            }

            public void VisitBooleanConstraint()
            {
                result = new JObject { ["constraint"] = "boolean" };
            }

            public void VisitConjunctionConstraint(IEnumerable<Constraint> parts)
            {
                result = new JObject { ["$and"] = new JArray(parts.Select(Describe)) };
            }

            public void VisitDeclConstraint(ZilObject pattern)
            {
                result = new JObject { ["constraint"] = "decl", ["decl"] = pattern.ToString() };
            }

            public void VisitDisjunctionConstraint(IEnumerable<Constraint> alts)
            {
                result = new JObject { ["$or"] = new JArray(alts.Select(Describe)) };
            }

            public void VisitForbiddenConstraint()
            {
                // shouldn't get here
                throw new InvalidOperationException("Forbidden constraint");
            }

            public void VisitPrimTypeConstraint(PrimType primType)
            {
                result = new JObject { ["constraint"] = "primtype", ["primype"] = primType.ToString() };
            }

            public void VisitStructuredConstraint()
            {
                result = new JObject { ["constraint"] = "structured" };
            }

            public void VisitTypeConstraint(StdAtom type)
            {
                result = new JObject { ["constraint"] = "type", ["type"] = type.ToString() };
            }
        }
    }
}
