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

namespace Zilf.Language.Signatures
{
    sealed class SequencePart : SignaturePart
    {
        public IReadOnlyList<SignaturePart> Parts { get; }

        SequencePart(IReadOnlyList<SignaturePart> seqParts)
        {
            Parts = seqParts;
        }

        public static SignaturePart From(IEnumerable<SignaturePart> parts)
        {
            var seqParts = parts.SelectMany(ExpandSequenceParts).ToArray();

            return seqParts.Length switch
            {
                0 => throw new ArgumentException("No sequence provided"),
                1 => seqParts[0],
                _ => new SequencePart(seqParts),
            };
        }

        public static IEnumerable<SignaturePart> ExpandSequenceParts(SignaturePart p)
        {
            return p is SequencePart sp ? sp.Parts : Enumerable.Repeat(p, 1);
        }

        public override void Accept(ISignatureVisitor visitor) => visitor.Visit(this);

        public override Constraint Constraint
        {
            get
            {
                //XXX go through all Parts until we find a non-optional one, and OR their constraints together
                throw new NotImplementedException();
            }
        }

        public override int MinArgs => Parts.Sum(p => p.MinArgs);

        // can't use Sum here because it skips nulls
        public override int? MaxArgs => Parts.Select(p => p.MaxArgs).Aggregate((a, b) => a + b);

        protected override IEnumerable<SignaturePart> GetChildren()
        {
            return Parts;
        }
    }
}