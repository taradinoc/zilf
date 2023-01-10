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

using System.Collections.Generic;

namespace Zilf.Language.Signatures
{
    sealed class OptionalPart : SignaturePart
    {
        public SignaturePart Inner { get; }

        OptionalPart(SignaturePart inner)
        {
            Inner = inner;
        }

        public static SignaturePart From(SignaturePart inner) => inner switch
        {
            VarArgsPart or OptionalPart => inner,
            _ => new OptionalPart(inner),
        };

        public override void Accept(ISignatureVisitor visitor) => visitor.Visit(this);

        public override Constraint Constraint => Inner.Constraint;

        protected override IEnumerable<SignaturePart> GetChildren()
        {
            yield return Inner;
        }

        public override int MinArgs => 0;
        public override int? MaxArgs => Inner.MaxArgs;
    }
}