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
using System.Linq;
using System.Text.RegularExpressions;
using Zilf.Interpreter;

namespace Zilf.Language.Signatures
{
    static partial class SignatureBuilder
    {
        #region Basics

        public static SignaturePart MaybeConvertDecl(ParamDescAttribute attr)
        {
            if (string.IsNullOrWhiteSpace(attr.Description) || attr.Description.Contains(' ', StringComparison.Ordinal))
                throw new ArgumentException($"Unexpected param description: {attr.Description}");

            return Identifier(attr.Description);
        }

        public static SignaturePart Quote(SignaturePart second) => QuotedPart.From(second);

        [GeneratedRegex("^<OR (?:('[^ <>]+)\\s*)+>$")]
        private static partial Regex GetOrDeclRegex();

        public static SignaturePart? MaybeConvertDecl(DeclAttribute decl) => MaybeConvertDecl(decl.Pattern);

        public static SignaturePart? MaybeConvertDecl(string pattern)
        {
            // TODO: this should parse the <OR...> instead of doing a hacky regex match

            if (pattern.StartsWith("'", StringComparison.Ordinal))
            {
                return LiteralPart.From(pattern[1..]);
            }

            var match = GetOrDeclRegex().Match(pattern);
            if (!match.Success)
                return null;

#if NET471
            var captures = match.Groups[1].Captures.Cast<Capture>();
#else
            var captures = match.Groups[1].Captures;
#endif
            var alts = captures.Select(c => MaybeConvertDecl(c.Value)).ToArray();
            if (alts.Length > 0 && alts.All(a => a != null))
            {
                return Alternatives(alts!);
            }

            return null;
        }

        public static SignaturePart VarArgs(SignaturePart inner, bool isRequired) =>
            VarArgsPart.From(inner, isRequired);

        public static SignaturePart Optional(SignaturePart inner) => OptionalPart.From(inner);

        public static SignaturePart List(IEnumerable<SignaturePart> parts, string? name = null)
        {
            var result = ListPart.From(parts);
            result.Name = name;
            return result;
        }

        public static SignaturePart Form(IEnumerable<SignaturePart> parts, string? name = null)
        {
            var result = FormPart.From(parts);
            result.Name = name;
            return result;
        }

        public static SignaturePart Identifier(string name)
        {
            return new AnyPart(name);
        }

        public static SignaturePart Adecl(SignaturePart left, SignaturePart right, string? name = null)
        {
            return new AdeclPart(left, right) { Name = name };
        }

        public static SignaturePart Alternatives(IEnumerable<SignaturePart> parts, string? name = null)
        {
            var result = AlternativesPart.From(parts);
            result.Name = name;
            return result;
        }

        public static SignaturePart Sequence(IEnumerable<SignaturePart> parts, string? name = null)
        {
            var result = SequencePart.From(parts);
            result.Name = name;
            return result;
        }

        public static SignaturePart Constrained(SignaturePart inner, Constraint constraint)
        {
            return ConstrainedPart.From(inner, constraint);
        }

        #endregion
    }
}
