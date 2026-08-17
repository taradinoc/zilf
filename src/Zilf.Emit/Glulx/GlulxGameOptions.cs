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
    public class GlulxGameOptions : IGameOptions
    {
        /// <summary>
        /// Enables Z-machine compatible 16-bit layout when targeting Glulx.
        /// </summary>
        public bool ZCompatibilityMode { get; init; }

        /// <summary>
        /// Z-machine version to emulate when <see cref="ZCompatibilityMode"/> is enabled.
        /// </summary>
        public int ZMachineVersion { get; init; } = 3;

        /// <summary>
        /// Enables the "time" status line when emulating V3.
        /// </summary>
        public bool TimeStatusLine { get; init; }

        /// <summary>
        /// Disables routine IR optimization while retaining normal IR construction and lowering.
        /// Intended for diagnostics and optimizer differential tests.
        /// </summary>
        public bool DisableIrOptimization { get; init; }
    }
}
