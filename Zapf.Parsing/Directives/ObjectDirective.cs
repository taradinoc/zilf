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

using JetBrains.Annotations;
using Zapf.Parsing.Expressions;

namespace Zapf.Parsing.Directives
{
    public sealed class ObjectDirective : NamedDirective
    {
        public ObjectDirective([NotNull] string name,
            [NotNull] AsmExpr flags1, [NotNull] AsmExpr flags2, [CanBeNull] AsmExpr flags3,
            [NotNull] AsmExpr parent, [NotNull] AsmExpr sibling, [NotNull] AsmExpr child,
            [NotNull] AsmExpr propTable)
            : base(name)
        {
            Flags1 = flags1;
            Flags2 = flags2;
            Flags3 = flags3;
            Parent = parent;
            Sibling = sibling;
            Child = child;
            PropTable = propTable;
        }

        [NotNull]
        public AsmExpr Flags1 { get; }
        [NotNull]
        public AsmExpr Flags2 { get; }
        [CanBeNull]
        public AsmExpr Flags3 { get; }
        [NotNull]
        public AsmExpr Parent { get; }
        [NotNull]
        public AsmExpr Sibling { get; }
        [NotNull]
        public AsmExpr Child { get; }
        [NotNull]
        public AsmExpr PropTable { get; }
    }
}