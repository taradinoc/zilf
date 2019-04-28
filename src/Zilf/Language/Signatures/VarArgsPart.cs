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
using JetBrains.Annotations;

namespace Zilf.Language.Signatures
{
    sealed class VarArgsPart : SignaturePart
    {
        [NotNull]
        public SignaturePart Inner { get; }

        public bool Required { get; }

        VarArgsPart([NotNull] SignaturePart inner, bool isRequired)
        {
            Inner = inner;
            Required = isRequired;
        }

        [NotNull]
        public static SignaturePart From([NotNull] SignaturePart inner, bool isRequired)
        {
            switch (inner)
            {
                case VarArgsPart vp:
                    inner = vp.Inner;
                    break;

                case OptionalPart op:
                    inner = op.Inner;
                    break;
            }
            
            return new VarArgsPart(inner, isRequired);
        }

        public override void Accept(ISignatureVisitor visitor) => visitor.Visit(this);

        public override Constraint Constraint => Inner.Constraint;

        public override int MinArgs => Required ? 1 : 0;
        public override int? MaxArgs => null;

        protected override IEnumerable<SignaturePart> GetChildren()
        {
            yield return Inner;
        }
    }
}