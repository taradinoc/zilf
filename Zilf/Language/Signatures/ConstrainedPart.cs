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
    class ConstrainedPart : SignaturePart
    {
        [NotNull]
        public SignaturePart Inner { get; }

        ConstrainedPart([NotNull] SignaturePart inner, [NotNull] Constraint constraint)
        {
            Inner = inner;
            Constraint = constraint;
        }

        [NotNull]
        public static SignaturePart From([NotNull] SignaturePart inner, [NotNull] Constraint constraint)
        {
            var newConstraint = inner.Constraint.And(constraint);
            return new ConstrainedPart(inner is ConstrainedPart icp ? icp.Inner : inner, newConstraint);
        }

        public override void Accept(ISignatureVisitor visitor) => visitor.Visit(this);

        public override Constraint Constraint { get; }

        public override int MinArgs => Inner.MinArgs;
        public override int? MaxArgs => Inner.MaxArgs;

        protected override IEnumerable<SignaturePart> GetChildren()
        {
            yield return Inner;
        }
    }
}
