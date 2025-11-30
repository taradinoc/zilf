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
    /// <summary>
    /// Represents a single Glulx assembly instruction for peephole optimization.
    /// </summary>
    struct GlulxCode
    {
        /// <summary>
        /// The text of the instruction, without the leading indent.
        /// </summary>
        public string Text;

        /// <summary>
        /// The opcode name (e.g., "copy", "add", "jgt"), extracted for optimization purposes.
        /// </summary>
        public string? Opcode;

        /// <summary>
        /// The original line type when this instruction was created.
        /// This is used to detect when the peephole optimizer has inverted a branch,
        /// since in Glulx the branch polarity is encoded in the opcode itself (jz vs jnz, etc.)
        /// and we need to change the opcode when the polarity is inverted.
        /// </summary>
        public PeepholeLineType OriginalType;

        public override readonly string ToString() => Text;
    }
}
