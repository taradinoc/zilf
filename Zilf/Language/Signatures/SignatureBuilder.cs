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
using System.Diagnostics.Contracts;
using System.Linq;
using System.Text.RegularExpressions;
using JetBrains.Annotations;
using Zilf.Interpreter;

namespace Zilf.Language.Signatures
{
    static class SignatureBuilder
    {
        #region Basics

        [NotNull]
        public static SignaturePart MaybeConvertDecl([NotNull] ParamDescAttribute attr)
        {
            if (string.IsNullOrWhiteSpace(attr.Description) || attr.Description.Contains(" "))
                throw new ArgumentException($"Unexpected param description: {attr.Description}");

            return Identifier(attr.Description);
        }

        [NotNull]
        public static SignaturePart Quote([NotNull] SignaturePart second)
        {
            return QuotedPart.From(second);
        }

        static readonly Regex orDeclRegex = new Regex(@"^<OR (?:('[^ <>]+)\s*)+>$");

        [CanBeNull]
        public static SignaturePart MaybeConvertDecl([NotNull] DeclAttribute decl)
        {
            Contract.Requires(decl != null);
            return MaybeConvertDecl(decl.Pattern);
        }

        [CanBeNull]
        public static SignaturePart MaybeConvertDecl([NotNull] string pattern)
        {
            Contract.Requires(pattern != null);

            // TODO: this should parse the <OR...> instead of doing a hacky regex match

            if (pattern.StartsWith("'", StringComparison.Ordinal))
            {
                return LiteralPart.From(pattern.Substring(1));
            }

            var match = orDeclRegex.Match(pattern);
            if (!match.Success)
                return null;

            var alts = match.Groups[1].Captures.Cast<Capture>().Select(c => MaybeConvertDecl(c.Value)).ToArray();
            if (alts.Length > 0 && alts.All(a => a != null))
            {
                return Alternatives(alts);
            }

            return null;
        }

        [NotNull]
        public static SignaturePart VarArgs([NotNull] SignaturePart inner, bool isRequired)
        {
            return VarArgsPart.From(inner, isRequired);
        }

        [NotNull]
        public static SignaturePart Optional([NotNull] SignaturePart inner)
        {
            return OptionalPart.From(inner);
        }

        [NotNull]
        public static SignaturePart List([ItemNotNull] [NotNull] IEnumerable<SignaturePart> parts, [CanBeNull] string name = null)
        {
            var result = ListPart.From(parts);
            result.Name = name;
            return result;
        }

        [NotNull]
        public static SignaturePart Form([ItemNotNull] [NotNull] IEnumerable<SignaturePart> parts, [CanBeNull] string name = null)
        {
            var result = FormPart.From(parts);
            result.Name = name;
            return result;
        }

        [NotNull]
        public static SignaturePart Identifier([NotNull] string name)
        {
            return new AnyPart(name);
        }

        [NotNull]
        public static SignaturePart Adecl([NotNull] SignaturePart left, [NotNull] SignaturePart right, [CanBeNull] string name = null)
        {
            return new AdeclPart(left, right) { Name = name };
        }

        [NotNull]
        public static SignaturePart Alternatives([ItemNotNull] [NotNull] IEnumerable<SignaturePart> parts, [CanBeNull] string name = null)
        {
            var result = AlternativesPart.From(parts);
            result.Name = name;
            return result;
        }

        [NotNull]
        public static SignaturePart Sequence([ItemNotNull] [NotNull] IEnumerable<SignaturePart> parts, [CanBeNull] string name = null)
        {
            var result = SequencePart.From(parts);
            result.Name = name;
            return result;
        }

        [NotNull]
        public static SignaturePart Constrained([NotNull] SignaturePart inner, [NotNull] Constraint constraint)
        {
            return ConstrainedPart.From(inner, constraint);
        }

        #endregion
    }
}
