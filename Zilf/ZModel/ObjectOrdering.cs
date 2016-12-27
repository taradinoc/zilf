/* Copyright 2010-2016 Jesse McGrew
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
    /// Specifies the order in which object numbers are assigned.
    /// </summary>
    enum ObjectOrdering
    {
        /// <summary>
        /// Reverse mention order.
        /// </summary>
        Default,
        /// <summary>
        /// Definition order, then mention order for objects with no definitions.
        /// </summary>
        Defined,
        /// <summary>
        /// Like <see cref="Defined"/>, but with all rooms having lower numbers than non-rooms.
        /// </summary>
        /// <remarks>
        /// "Rooms" include all objects declared with the ROOM FSUBR (instead of OBJECT), and
        /// all objects whose initial LOC is the object called ROOMS.
        /// </remarks>
        RoomsFirst,
        /// <summary>
        /// Like <see cref="RoomsFirst"/>, but with all local globals also having lower numbers
        /// than non-rooms and non-local-globals.
        /// </summary>
        /// <remarks>
        /// "Local globals" includes all objects whose initial LOC is the object called
        /// LOCAL-GLOBALS.
        /// </remarks>
        RoomsAndLocalGlobalsFirst,
        /// <summary>
        /// Like <see cref="Defined"/>, but with all non-rooms having lower numbers than rooms.
        /// </summary>
        RoomsLast,
    }
}