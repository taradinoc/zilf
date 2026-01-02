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

using System;

namespace Zilf.Emit
{
    public interface IProvideWideContext
    {
        /// <summary>
        /// Begins an emit context that uses the native word size instead of
        /// truncating values to 16 bits.
        /// </summary>
        /// <returns>An object that, when disposed, will end the wide context.</returns>
        IDisposable EnterWideContext();

        bool IsInWideContext { get; }
    }
}
