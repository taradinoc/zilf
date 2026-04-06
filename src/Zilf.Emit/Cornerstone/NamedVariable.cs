/* Copyright 2010-2026 Tara McGrew
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

namespace Zilf.Emit.Cornerstone
{
    public sealed partial class GameBuilder
    {
        internal sealed class NamedVariable(string name, VariableKind kind, int index) : IGlobalBuilder, ILocalBuilder
        {
            public string Name { get; } = name;

            public VariableKind Kind { get; } = kind;

            public int Index { get; set; } = index;

            public IOperand? DefaultValue { get; set; }

            public IIndirectOperand Indirect => new VariableIndirectOperand(this);
        }
    }
}