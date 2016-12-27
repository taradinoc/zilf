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
using Zilf.Interpreter.Values;

namespace Zilf.Interpreter
{
    /// <summary>
    /// Indicates the primitive type of a ZilObject.
    /// </summary>
    enum PrimType
    {
        /// <summary>
        /// The primitive type is <see cref="ZilAtom"/>.
        /// </summary>
        ATOM,
        /// <summary>
        /// The primitive type is <see cref="ZilFix"/>.
        /// </summary>
        FIX,
        /// <summary>
        /// The primitive type is <see cref="ZilString"/>.
        /// </summary>
        STRING,
        /// <summary>
        /// The primitive type is <see cref="ZilList"/>.
        /// </summary>
        LIST,
        /// <summary>
        /// The primitive type is <see cref="ZModel.Values.ZilTable"/>.
        /// </summary>
        TABLE,
        /// <summary>
        /// The primitive type is <see cref="ZilVector"/>.
        /// </summary>
        VECTOR,
    }
}