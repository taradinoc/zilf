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

using Zapf.Parsing.Expressions;

namespace Zapf.Parsing.Directives
{
    public sealed class ObjectDirective(string name,
         AsmExpr flags1, AsmExpr flags2, AsmExpr? flags3,
         AsmExpr parent, AsmExpr sibling, AsmExpr child,
         AsmExpr propTable) : NamedDirective(name)
    {
        public AsmExpr Flags1 { get; } = flags1;
        public AsmExpr Flags2 { get; } = flags2;
        public AsmExpr? Flags3 { get; } = flags3;
        public AsmExpr Parent { get; } = parent;
        public AsmExpr Sibling { get; } = sibling;
        public AsmExpr Child { get; } = child;
        public AsmExpr PropTable { get; } = propTable;
    }
}