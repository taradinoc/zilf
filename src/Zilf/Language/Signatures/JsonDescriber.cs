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

using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using JetBrains.Annotations;
using Newtonsoft.Json.Linq;
using Zilf.Interpreter;
using Zilf.Interpreter.Values;

namespace Zilf.Language.Signatures
{
    static class VisitingExtensions
    {
        public static T AcceptForValue<T>([NotNull] this ISignaturePart part, [NotNull] SignatureVisitorWithValue<T> visitor)
        {
            return visitor.Run(part);
        }
    }

    abstract class SignatureVisitorWithValue<T> : ISignatureVisitor
    {
        T result;

        internal T Run([NotNull] ISignaturePart part)
        {
            part.Accept(this);
            PostProcess(part, ref result);
            return result;
        }

        [SuppressMessage("ReSharper", "UnusedParameter.Global")]
        protected virtual void PostProcess([NotNull] ISignaturePart part, ref T pendingResult)
        {
            // by default, nada
        }

        protected abstract T Visit([NotNull] AdeclPart part);
        protected abstract T Visit([NotNull] AlternativesPart part);
        protected abstract T Visit([NotNull] AnyPart part);
        protected abstract T Visit([NotNull] ConstrainedPart part);
        protected abstract T Visit([NotNull] FormPart part);
        protected abstract T Visit([NotNull] ListPart part);
        protected abstract T Visit([NotNull] LiteralPart part);
        protected abstract T Visit([NotNull] OptionalPart part);
        protected abstract T Visit([NotNull] QuotedPart part);
        protected abstract T Visit([NotNull] SequencePart part);
        protected abstract T Visit([NotNull] VarArgsPart part);

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
        static readonly JsonDescriber Instance = new JsonDescriber();

        JsonDescriber()
        {
        }

        [NotNull]
        public static JObject Describe([NotNull] ISignature signature)
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

        protected override void PostProcess(ISignaturePart part, ref JObject pendingResult)
        {
            if (part.Name != null)
            {
                pendingResult["name"] = part.Name;
            }
        }

        [NotNull]
        protected override JObject Visit(VarArgsPart part)
        {
            var result = new JObject { ["$rest"] = part.Inner.AcceptForValue(this) };
            if (part.Required)
            {
                result["required"] = true;
            }
            return result;
        }

        [NotNull]
        protected override JObject Visit(AdeclPart part)
        {
            return new JObject
            {
                ["type"] = "ADECL",
                ["elements"] = new JArray(part.Left.AcceptForValue(this), part.Right.AcceptForValue(this))
            };
        }

        [NotNull]
        protected override JObject Visit(AlternativesPart part)
        {
            return new JObject
            {
                ["$or"] = new JArray(part.Alternatives.Select(a => a.AcceptForValue(this)))
            };
        }

        [NotNull]
        protected override JObject Visit(AnyPart part)
        {
            return new JObject();
        }

        [NotNull]
        protected override JObject Visit(ConstrainedPart part)
        {
            var result = part.Inner.AcceptForValue(this);
            result["constraint"] = ConstraintDescriber.Describe(part.Constraint);
            return result;
        }

        [NotNull]
        protected override JObject Visit(FormPart part)
        {
            return new JObject
            {
                ["type"] = "FORM",
                ["elements"] = new JArray(part.Parts.Select(p => p.AcceptForValue(this)))
            };
        }

        [NotNull]
        protected override JObject Visit(ListPart part)
        {
            return new JObject
            {
                ["type"] = "LIST",
                ["elements"] = new JArray(part.Parts.Select(p => p.AcceptForValue(this)))
            };
        }

        [NotNull]
        protected override JObject Visit(LiteralPart part)
        {
            return new JObject { ["$literal"] = part.Text };
        }

        [NotNull]
        protected override JObject Visit(OptionalPart part)
        {
            return new JObject { ["$opt"] = part.Inner.AcceptForValue(this) };
        }

        [NotNull]
        protected override JObject Visit(QuotedPart part)
        {
            var result = part.Inner.AcceptForValue(this);
            result["eval"] = false;
            return result;
        }

        [NotNull]
        protected override JObject Visit(SequencePart part)
        {
            return new JObject
            {
                ["$seq"] = new JArray(part.Parts.Select(p => p.AcceptForValue(this)))
            };
        }

        class ConstraintDescriber : IConstraintVisitor
        {
            public static JObject Describe([NotNull] Constraint constraint)
            {
                var describer = new ConstraintDescriber();
                constraint.Accept(describer);
                return describer.result;
            }

            JObject result;

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
