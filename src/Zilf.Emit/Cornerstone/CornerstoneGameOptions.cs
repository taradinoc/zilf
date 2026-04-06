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
    public class CornerstoneGameOptions : IGameOptions
    {
        /// <summary>
        /// Gets or sets the emulated Z-machine version used for header compatibility.
        /// </summary>
        public int ZMachineVersion { get; set; } = 3;

        /// <summary>
        /// Gets or sets a value indicating whether the status line should render SCORE and MOVES as time.
        /// </summary>
        public bool TimeStatusLine { get; set; }

        /// <summary>
        /// Gets or sets the maximum number of procedures emitted into a single Cornerstone module.
        /// </summary>
        /// <remarks>
        /// This doesn't vary at runtime, but it's configurable so that tests can override it.
        /// </remarks>
        public int MaxProceduresPerModule { get; set; } = byte.MaxValue;
    }
}