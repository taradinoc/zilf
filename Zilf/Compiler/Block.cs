/* Copyright 2010, 2015 Jesse McGrew
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
using Zilf.Emit;
using Zilf.Interpreter;
using Zilf.Interpreter.Values;

namespace Zilf.Compiler
{
    internal sealed class Block
    {
        /// <summary>
        /// The activation atom identifying the block, or null if it is unnamed.
        /// </summary>
        public ZilAtom Name;
        /// <summary>
        /// The label to which &lt;AGAIN&gt; should branch.
        /// </summary>
        public ILabel AgainLabel;
        /// <summary>
        /// The label to which &lt;RETURN&gt; should branch, or null if
        /// it should return from the routine.
        /// </summary>
        public ILabel ReturnLabel;
        /// <summary>
        /// The context flags for &lt;RETURN&gt;.
        /// </summary>
        public BlockFlags Flags;
    }
}
