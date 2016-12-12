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
using System;

namespace Zilf.Compiler
{
    [Flags]
enum BlockFlags
    {
        None = 0,

        /// <summary>
        /// Indicates that the return label was used.
        /// </summary>
        Returned = 1,
        /// <summary>
        /// Indicates that the return label expects a result on the stack. (Otherwise, the
        /// result must be discarded before branching.)
        /// </summary>
        WantResult = 2,
        /// <summary>
        /// Indicates that &lt;RETURN&gt; and &lt;AGAIN&gt; should not act on this block
        /// unless explicitly given its activation atom.
        /// </summary>
        ExplicitOnly = 4,
    }
}
