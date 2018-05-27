/* Copyright 2010-2018 Jesse McGrew
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

using System.Diagnostics;
using JetBrains.Annotations;
using Zilf.Interpreter.Values;

namespace Zilf.Interpreter
{
    static class StructureExtensions
    {
        [System.Diagnostics.Contracts.Pure]
        public static bool HasLength([NotNull] this IStructure structure, int length) =>
            structure.GetLength(length) == length;

        [System.Diagnostics.Contracts.Pure]
        public static bool HasLength([NotNull] this IStructure structure, int min, int max)
        {
            Debug.Assert(min >= 0 && min < max);
            var len = structure.GetLength(max);
            return len != null && len >= min;
        }

        [System.Diagnostics.Contracts.Pure]
        public static bool HasLengthAtMost([NotNull] this IStructure structure, int max) =>
            structure.GetLength(max) != null;

        [System.Diagnostics.Contracts.Pure]
        public static bool HasLengthAtLeast([NotNull] this IStructure structure, int min)
        {
            var len = structure.GetLength(min);
            return len == null || len >= min;
        }

        #region Matches

        /// <summary>
        /// Tests whether a structure has the specified number and types of elements,
        /// and extracts the typed elements if so.
        /// </summary>
        /// <typeparam name="T1">The expected type of the first element.</typeparam>
        /// <param name="structure">The structure to test.</param>
        /// <param name="obj1">The first element, or <see langword="null"/> if the match failed.</param>
        /// <returns><see langword="true"/> if the structure had the specified number and types of elements,
        /// or <see langword="false"/> otherwise.</returns>
        [ContractAnnotation("=> true, obj1: notnull")]
        [ContractAnnotation("=> false, obj1: null")]
        public static bool Matches<T1>([NotNull] this IStructure structure, [CanBeNull] out T1 obj1)
            where T1 : ZilObject
        {
            if (structure.HasLength(1) && structure.GetFirst() is T1 elem1)
            {
                obj1 = elem1;
                return true;
            }

            obj1 = default(T1);
            return false;
        }

        /// <summary>
        /// Tests whether a structure has the specified number and types of elements,
        /// and extracts the typed elements if so.
        /// </summary>
        /// <typeparam name="T1">The expected type of the first element.</typeparam>
        /// <typeparam name="T2">The expected type of the second element.</typeparam>
        /// <param name="structure">The structure to test.</param>
        /// <param name="obj1">The first element, or <see langword="null"/> if the match failed.</param>
        /// <param name="obj2">The second element, or <see langword="null"/> if the match failed.</param>
        /// <returns><see langword="true"/> if the structure had the specified number and types of elements,
        /// or <see langword="false"/> otherwise.</returns>
        [ContractAnnotation("=> true, obj1: notnull, obj2: notnull")]
        [ContractAnnotation("=> false, obj1: null, obj2: null")]
        public static bool Matches<T1, T2>([NotNull] this IStructure structure, [CanBeNull] out T1 obj1, [CanBeNull] out T2 obj2)
            where T1 : ZilObject
            where T2 : ZilObject
        {
            if (structure.HasLength(2) && structure[0] is T1 elem1 && structure[1] is T2 elem2)
            {
                obj1 = elem1;
                obj2 = elem2;
                return true;
            }

            obj1 = default(T1);
            obj2 = default(T2);
            return false;
        }

        /// <summary>
        /// Tests whether a structure has the specified number and types of elements,
        /// and extracts the typed elements if so.
        /// </summary>
        /// <typeparam name="T1">The expected type of the first element.</typeparam>
        /// <typeparam name="T2">The expected type of the second element.</typeparam>
        /// <typeparam name="T3">The expected type of the third element.</typeparam>
        /// <param name="structure">The structure to test.</param>
        /// <param name="obj1">The first element, or <see langword="null"/> if the match failed.</param>
        /// <param name="obj2">The second element, or <see langword="null"/> if the match failed.</param>
        /// <param name="obj3">The third element, or <see langword="null"/> if the match failed.</param>
        /// <returns><see langword="true"/> if the structure had the specified number and types of elements,
        /// or <see langword="false"/> otherwise.</returns>
        [ContractAnnotation("=> true, obj1: notnull, obj2: notnull, obj3: notnull")]
        [ContractAnnotation("=> false, obj1: null, obj2: null, obj3: null")]
        public static bool Matches<T1, T2, T3>([NotNull] this IStructure structure, [CanBeNull] out T1 obj1, [CanBeNull] out T2 obj2,
            [CanBeNull] out T3 obj3)
            where T1 : ZilObject
            where T2 : ZilObject
            where T3 : ZilObject
        {
            if (structure.HasLength(3) && structure[0] is T1 elem1 && structure[1] is T2 elem2 && structure[2] is T3 elem3)
            {
                obj1 = elem1;
                obj2 = elem2;
                obj3 = elem3;
                return true;
            }

            obj1 = default(T1);
            obj2 = default(T2);
            obj3 = default(T3);
            return false;
        }

        /// <summary>
        /// Tests whether a structure has the specified number and types of elements,
        /// and extracts the typed elements if so.
        /// </summary>
        /// <typeparam name="T1">The expected type of the first element.</typeparam>
        /// <typeparam name="T2">The expected type of the second element.</typeparam>
        /// <typeparam name="T3">The expected type of the third element.</typeparam>
        /// <typeparam name="T4">The expected type of the fourth element.</typeparam>
        /// <param name="structure">The structure to test.</param>
        /// <param name="obj1">The first element, or <see langword="null"/> if the match failed.</param>
        /// <param name="obj2">The second element, or <see langword="null"/> if the match failed.</param>
        /// <param name="obj3">The third element, or <see langword="null"/> if the match failed.</param>
        /// <param name="obj4">The fourth element, or <see langword="null"/> if the match failed.</param>
        /// <returns><see langword="true"/> if the structure had the specified number and types of elements,
        /// or <see langword="false"/> otherwise.</returns>
        [ContractAnnotation("=> true, obj1: notnull, obj2: notnull, obj3: notnull, obj4: notnull")]
        [ContractAnnotation("=> false, obj1: null, obj2: null, obj3: null, obj4: null")]
        public static bool Matches<T1, T2, T3, T4>([NotNull] this IStructure structure, [CanBeNull] out T1 obj1, [CanBeNull] out T2 obj2,
            [CanBeNull] out T3 obj3, [CanBeNull] out T4 obj4)
            where T1 : ZilObject
            where T2 : ZilObject
            where T3 : ZilObject
            where T4 : ZilObject
        {
            if (structure.HasLength(4) && structure[0] is T1 elem1 && structure[1] is T2 elem2 && structure[2] is T3 elem3 &&
                structure[3] is T4 elem4)
            {
                obj1 = elem1;
                obj2 = elem2;
                obj3 = elem3;
                obj4 = elem4;
                return true;
            }

            obj1 = default(T1);
            obj2 = default(T2);
            obj3 = default(T3);
            obj4 = default(T4);
            return false;
        }

        #endregion

        #region StartsWith

        /// <summary>
        /// Tests whether a structure has the specified minimum number and types of elements,
        /// and extracts the typed elements if so.
        /// </summary>
        /// <typeparam name="T1">The expected type of the first element.</typeparam>
        /// <param name="structure">The structure to test.</param>
        /// <param name="obj1">The first element, or <see langword="null"/> if the match failed.</param>
        /// <returns><see langword="true"/> if the structure had the specified minimum number and types of elements,
        /// or <see langword="false"/> otherwise.</returns>
        [ContractAnnotation("=> true, obj1: notnull")]
        [ContractAnnotation("=> false, obj1: null")]
        public static bool StartsWith<T1>([NotNull] this IStructure structure, [CanBeNull] out T1 obj1)
            where T1 : ZilObject
        {
            if (structure.HasLengthAtLeast(1) && structure.GetFirst() is T1 elem1)
            {
                obj1 = elem1;
                return true;
            }

            obj1 = default(T1);
            return false;
        }

        /// <summary>
        /// Tests whether a structure has the specified minimum number and types of elements,
        /// and extracts the typed elements if so.
        /// </summary>
        /// <typeparam name="T1">The expected type of the first element.</typeparam>
        /// <typeparam name="T2">The expected type of the second element.</typeparam>
        /// <param name="structure">The structure to test.</param>
        /// <param name="obj1">The first element, or <see langword="null"/> if the match failed.</param>
        /// <param name="obj2">The second element, or <see langword="null"/> if the match failed.</param>
        /// <returns><see langword="true"/> if the structure had the specified minimum number and types of elements,
        /// or <see langword="false"/> otherwise.</returns>
        [ContractAnnotation("=> true, obj1: notnull, obj2: notnull")]
        [ContractAnnotation("=> false, obj1: null, obj2: null")]
        public static bool StartsWith<T1, T2>([NotNull] this IStructure structure, [CanBeNull] out T1 obj1, [CanBeNull] out T2 obj2)
            where T1 : ZilObject
            where T2 : ZilObject
        {
            if (structure.HasLengthAtLeast(2) && structure[0] is T1 elem1 && structure[1] is T2 elem2)
            {
                obj1 = elem1;
                obj2 = elem2;
                return true;
            }

            obj1 = default(T1);
            obj2 = default(T2);
            return false;
        }

        /// <summary>
        /// Tests whether a structure has the specified minimum number and types of elements,
        /// and extracts the typed elements if so.
        /// </summary>
        /// <typeparam name="T1">The expected type of the first element.</typeparam>
        /// <typeparam name="T2">The expected type of the second element.</typeparam>
        /// <typeparam name="T3">The expected type of the third element.</typeparam>
        /// <param name="structure">The structure to test.</param>
        /// <param name="obj1">The first element, or <see langword="null"/> if the match failed.</param>
        /// <param name="obj2">The second element, or <see langword="null"/> if the match failed.</param>
        /// <param name="obj3">The third element, or <see langword="null"/> if the match failed.</param>
        /// <returns><see langword="true"/> if the structure had the specified minimum number and types of elements,
        /// or <see langword="false"/> otherwise.</returns>
        [ContractAnnotation("=> true, obj1: notnull, obj2: notnull, obj3: notnull")]
        [ContractAnnotation("=> false, obj1: null, obj2: null, obj3: null")]
        public static bool StartsWith<T1, T2, T3>([NotNull] this IStructure structure, [CanBeNull] out T1 obj1, [CanBeNull] out T2 obj2,
            [CanBeNull] out T3 obj3)
            where T1 : ZilObject
            where T2 : ZilObject
            where T3 : ZilObject
        {
            if (structure.HasLengthAtLeast(3) && structure[0] is T1 elem1 && structure[1] is T2 elem2 && structure[2] is T3 elem3)
            {
                obj1 = elem1;
                obj2 = elem2;
                obj3 = elem3;
                return true;
            }

            obj1 = default(T1);
            obj2 = default(T2);
            obj3 = default(T3);
            return false;
        }

        /// <summary>
        /// Tests whether a structure has the specified minimum number and types of elements,
        /// and extracts the typed elements if so.
        /// </summary>
        /// <typeparam name="T1">The expected type of the first element.</typeparam>
        /// <typeparam name="T2">The expected type of the second element.</typeparam>
        /// <typeparam name="T3">The expected type of the third element.</typeparam>
        /// <typeparam name="T4">The expected type of the fourth element.</typeparam>
        /// <param name="structure">The structure to test.</param>
        /// <param name="obj1">The first element, or <see langword="null"/> if the match failed.</param>
        /// <param name="obj2">The second element, or <see langword="null"/> if the match failed.</param>
        /// <param name="obj3">The third element, or <see langword="null"/> if the match failed.</param>
        /// <param name="obj4">The fourth element, or <see langword="null"/> if the match failed.</param>
        /// <returns><see langword="true"/> if the structure had the specified minimum number and types of elements,
        /// or <see langword="false"/> otherwise.</returns>
        [ContractAnnotation("=> true, obj1: notnull, obj2: notnull, obj3: notnull, obj4: notnull")]
        [ContractAnnotation("=> false, obj1: null, obj2: null, obj3: null, obj4: null")]
        public static bool StartsWith<T1, T2, T3, T4>([NotNull] this IStructure structure, [CanBeNull] out T1 obj1, [CanBeNull] out T2 obj2,
            [CanBeNull] out T3 obj3, [CanBeNull] out T4 obj4)
            where T1 : ZilObject
            where T2 : ZilObject
            where T3 : ZilObject
            where T4 : ZilObject
        {
            if (structure.HasLengthAtLeast(4) && structure[0] is T1 elem1 && structure[1] is T2 elem2 && structure[2] is T3 elem3 &&
                structure[3] is T4 elem4)
            {
                obj1 = elem1;
                obj2 = elem2;
                obj3 = elem3;
                obj4 = elem4;
                return true;
            }

            obj1 = default(T1);
            obj2 = default(T2);
            obj3 = default(T3);
            obj4 = default(T4);
            return false;
        }

        #endregion
    }
}
