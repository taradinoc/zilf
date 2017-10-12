/* Copyright 2010-2017 Jesse McGrew
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

namespace Zilf.ZModel
{
    /// <summary>
    /// Specifies the behavior of &lt;RETURN value&gt; inside a PROG/REPEAT block in a routine.
    /// </summary>
    /// <remarks>
    /// <para>MDL specifies that RETURN (with or without a value, and without an activation)
    /// returns from the innermost PROG/REPEAT. The only way to return from a function explicitly is
    /// to capture the function's activation and use the 3-argument form of RETURN.</para>
    /// <para>In ZIL, &lt;RETURN&gt; and &lt;RETURN value&gt; outside of a PROG/REPEAT always return
    /// from the routine, and &lt;RETURN&gt; (with no value) inside a PROG/REPEAT always returns
    /// from the block, but &lt;RETURN value&gt; inside a PROG/REPEAT is ambiguous.</para>
    /// <para>The 3-argument form of RETURN can always be used to resolve the ambiguity.</para>
    /// </remarks>
    enum ReturnQuirkMode
    {
        /// <summary>
        /// Equivalent to <see cref="PreferBlock"/> when targeting V4 or below,
        /// and <see cref="PreferRoutine"/> when targeting V5 or above.
        /// </summary>
        ByVersion,
        /// <summary>
        /// &lt;RETURN value&gt; always returns from the routine, even when inside a PROG/REPEAT.
        /// </summary>
        PreferRoutine,
        /// <summary>
        /// &lt;RETURN value&gt; inside a PROG/REPEAT block returns from the block.
        /// </summary>
        PreferBlock,
    }
}