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
using System.Collections.Generic;
using System.Diagnostics.Contracts;
using Zilf.Interpreter.Values;

namespace Zilf.Interpreter
{
    /// <summary>
    /// Provides methods common to structured types (LIST, STRING, possibly others).
    /// </summary>
    [ContractClass(typeof(IStructureContracts))]
    interface IStructure : IEnumerable<ZilObject>
    {
        /// <summary>
        /// Gets the first element of the structure.
        /// </summary>
        /// <returns>The first element.</returns>
        ZilObject GetFirst();
        /// <summary>
        /// Gets the remainder of the structure, after skipping the first few elements.
        /// </summary>
        /// <param name="skip">The number of elements to skip.</param>
        /// <returns>A structure containing the unskipped elements, or null if no elements are left.</returns>
        IStructure GetRest(int skip);
        /// <summary>
        /// Reverses <see cref="GetRest(int)"/>, returning a larger structure with some of the
        /// previously skipped elements included.
        /// </summary>
        /// <param name="skip">The number of skipped elements to include.</param>
        /// <returns>A structure containing the requested elements, or null if not enough elements
        /// have been skipped.</returns>
        /// <exception cref="System.NotSupportedException">The operation is not supported by this
        /// structure type.</exception>
        IStructure GetBack(int skip);
        /// <summary>
        /// Completely reverses <see cref="GetRest(int)"/>, returning all elements of the underlying
        /// structure.
        /// </summary>
        /// <returns>A structure.</returns>
        /// <exception cref="System.NotSupportedException">The operation is not supported by this
        /// structure type.</exception>
        IStructure GetTop();

        /// <summary>
        /// Increases the size of the structure by adding elements at either end.
        /// </summary>
        /// <param name="end">The number of elements to add at the end.</param>
        /// <param name="beginning">The number of elements to add at the beginning.</param>
        void Grow(int end, int beginning, ZilObject defaultValue);

        /// <summary>
        /// Determines whether the structure is empty.
        /// </summary>
        /// <returns>true if the structure has no elements; false if it has any elements.</returns>
        [Pure]
        bool IsEmpty();

        /// <summary>
        /// Gets or sets an element by its numeric index.
        /// </summary>
        /// <param name="index">The zero-based index of the element to access.</param>
        /// <returns>The element value, or null if the specified element is past the end of
        /// the structure.</returns>
        /// <exception cref="Zilf.Language.InterpreterError">
        /// An attempt was made to set an element past the end of the structure.
        /// </exception>
        ZilObject this[int index] { get; set; }

        /// <summary>
        /// Measures the length of the structure.
        /// </summary>
        /// <returns>The number of elements in the structure.</returns>
        /// <remarks>This method may loop indefinitely if the structure contains
        /// a reference to itself.</remarks>
        int GetLength();
        /// <summary>
        /// Measures the length of the structure, up to a specified maximum.
        /// </summary>
        /// <param name="limit">The maximum length to allow.</param>
        /// <returns>The number of elements in the structure, or null if the structure
        /// contains more than <paramref name="limit"/> elements.</returns>
        [Pure]
        int? GetLength(int limit);
    }
}