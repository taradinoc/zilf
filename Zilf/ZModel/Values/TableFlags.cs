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

namespace Zilf.ZModel.Values
{
    [Flags]
    public enum TableFlags
    {
        /// <summary>
        /// The table elements are bytes rather than words.
        /// </summary>
        Byte = 1,
        /// <summary>
        /// The table elements are 4-byte records rather than words, and
        /// the table is prefixed with an element count (byte) and a zero byte.
        /// </summary>
        Lexv = 2,
        /// <summary>
        /// The table is prefixed with an element count (byte).
        /// </summary>
        ByteLength = 4,
        /// <summary>
        /// The table is prefixed with an element count (word).
        /// </summary>
        WordLength = 8,
        /// <summary>
        /// The table is stored in pure (read-only) memory.
        /// </summary>
        Pure = 16,
        /// <summary>
        /// The table only exists at compile time and is not written to the data file.
        /// </summary>
        TempTable = 32,
        /// <summary>
        /// The table is lower in memory than other pure tables.
        /// </summary>
        ParserTable = 64,
    }
}