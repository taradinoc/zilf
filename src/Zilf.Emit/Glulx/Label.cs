/* Copyright 2010-2025 Tara McGrew
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

namespace Zilf.Emit.Glulx
{
    class Label(string name) : ILabel
    {
        /// <summary>
        /// Special label for "return true" (return 1). Branch to this to return 1.
        /// </summary>
        public static readonly Label RTRUE = new("rtrue");

        /// <summary>
        /// Special label for "return false" (return 0). Branch to this to return 0.
        /// </summary>
        public static readonly Label RFALSE = new("rfalse");

        public string Name => name;

        /// <summary>
        /// Returns true if this label is one of the special rtrue/rfalse pseudo-labels.
        /// </summary>
        public bool IsReturnLabel => ReferenceEquals(this, RTRUE) || ReferenceEquals(this, RFALSE);

        public override string ToString() => $".{name}";
    }
}
