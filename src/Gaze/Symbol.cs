/* Copyright 2010-2018 Tara McGrew
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

namespace Gaze
{
    public sealed class Symbol
    {
        /// <summary>
        /// The symbol's name in the source code.
        /// </summary>
        public readonly string? Name;
        /// <summary>
        /// The symbol's type.
        /// </summary>
        public SymbolType Type;
        /// <summary>
        /// The symbol's value, usually a numeric constant or an address.
        /// </summary>
        public int Value;
        /// <summary>
        /// Indicates whether the symbol has a value from a previous attempt,
        /// but has not yet been defined in the current attempt.
        /// </summary>
        public bool Phantom;

        public Symbol()
        {
        }

        public Symbol(int value)
        {
            Type = SymbolType.Constant;
            Value = value;
        }

        public Symbol(string? name, SymbolType type, int value)
        {
            Name = name;
            Type = type;
            Value = value;
        }
    }
}
