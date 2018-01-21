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
using System.Linq;
using JetBrains.Annotations;

namespace Zilf.Language.Signatures
{
    sealed class AlternativesPart : SignaturePart
    {
        AlternativesPart([ItemNotNull] [NotNull] IReadOnlyList<SignaturePart> alternatives)
        {
            Alternatives = alternatives;
        }

        [ItemNotNull]
        [NotNull]
        public IReadOnlyList<SignaturePart> Alternatives { get; }

        public static SignaturePart From([NotNull] [ItemNotNull] IEnumerable<SignaturePart> parts)
        {
            var alts = parts.SelectMany(ExpandAlternatives).ToArray();

            switch (alts.Length)
            {
                case 0:
                    throw new ArgumentException("No alternatives provided");

                case 1:
                    return alts[0];

                default:
                    return new AlternativesPart(alts);
            }
        }

        [NotNull]
        static IEnumerable<SignaturePart> ExpandAlternatives(SignaturePart p)
        {
            return p is AlternativesPart ap ? ap.Alternatives : Enumerable.Repeat(p, 1);
        }

        public override void Accept(ISignatureVisitor visitor) => visitor.Visit(this);

        public override Constraint Constraint =>
            Alternatives.Aggregate(Constraint.Forbidden, (c, sp) => c.Or(sp.Constraint));

        public override int MinArgs => Alternatives.Min(p => p.MinArgs);

        public override int? MaxArgs =>
            Alternatives.Select(p => p.MaxArgs).Aggregate(NullableMax);

        static int? NullableMax(int? a, int? b)
        {
            if (a == null || b == null)
                return null;

            return Math.Max(a.Value, b.Value);
        }

        protected override IEnumerable<SignaturePart> GetChildren()
        {
            return Alternatives;
        }
    }
}