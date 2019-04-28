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
using System.Reflection;
using JetBrains.Annotations;

namespace Zilf.Language.Signatures
{
    abstract class SignaturePart : ISignaturePart
    {
        public ICustomAttributeProvider Source => null;
        public string Name { get; set; }

        [NotNull]
        public abstract Constraint Constraint { get; }
        public abstract int MinArgs { get; }
        public abstract int? MaxArgs { get; }
        public abstract void Accept(ISignatureVisitor visitor);

        [NotNull]
        [ItemNotNull]
        protected abstract IEnumerable<SignaturePart> GetChildren();

        [NotNull]
        [ItemNotNull]
        public IEnumerable<SignaturePart> GetDescendants()
        {
            return GetChildren().Concat(GetChildren().SelectMany(c => c.GetDescendants()));
        }

        IConstraint ISignaturePart.Constraint => Constraint;
    }
}
