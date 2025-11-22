/* Copyright 2010-2023 Tara McGrew
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
using System.Collections.Generic;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using Zilf.Common;
using Zilf.Diagnostics;
using Zilf.Interpreter.Values;
using Zilf.Language;

namespace Zilf.Interpreter
{
    [SuppressMessage("Performance", "CA1801", Justification = "Subrs parameters are needed for validation, even if the values aren't used.")]
    [SuppressMessage("Style", "IDE0060:Remove unused parameter", Justification = "Subrs parameters are needed for validation, even if the values aren't used.")]
    static partial class Subrs
    {
        /// <summary>
        /// Returns true if the structure is empty.
        /// </summary>
        /// <param name="ctx"></param>
        /// <param name="st">A structured value.</param>
        /// <returns>True if the structure is empty; otherwise, false.</returns>
        [Subr("EMPTY?")]
        public static ZilObject EMPTY_P(Context ctx, IStructure st)
        {
            return st.IsEmpty ? ctx.TRUE : ctx.FALSE;
        }

        /*[Subr]
        public static ZilObject FIRST(Context ctx, ZilObject[] args)
        {
            if (args.Length != 1)
                throw new InterpreterError("FIRST", 1, 1);

            IStructure st = args[0] as IStructure;
            if (st == null)
                throw new InterpreterError("FIRST: arg must be a structure");

            return st.GetFirst();
        }*/

        /// <summary>
        /// Returns the suffix of a structure after skipping a specified number of elements.
        /// </summary>
        /// <param name="ctx"></param>
        /// <param name="st">A structured value.</param>
        /// <param name="skip">The number of elements to skip. If omitted, defaults to 1.</param>
        /// <returns>The suffix of the structure after skipping the specified number of elements.</returns>
        /// <exception cref="InterpreterError"><paramref name="st"/> has fewer than <paramref name="skip"/> elements.</exception>
        [Subr]
        public static ZilObject REST(Context ctx, IStructure st, int skip = 1)
        {
            var result = (ZilObject?)st.GetRest(skip);
            if (result == null)
                throw new InterpreterError(InterpreterMessages._0_Not_Enough_Elements, "REST");
            return result.GetPrimitive(ctx);
        }

        /// <summary>
        /// Partially reverses the effect of REST, returning a suffix of the original structure the specified number of elements before the specified suffix.
        /// </summary>
        /// <param name="ctx"></param>
        /// <param name="st">A structure suffix previously obtained from REST.</param>
        /// <param name="skip">The number of elements to move back. If omitted, defaults to 1.</param>
        /// <returns>A longer suffix of the original structure.</returns>
        /// <exception cref="InterpreterError">The type of <paramref name="st"/> does not support this operation, or <paramref name="st"/> has not been RESTed at least <paramref name="skip"/> elements.</exception>
        [Subr]
        public static ZilObject BACK(Context ctx, IStructure st, int skip = 1)
        {
            try
            {
                var result = (ZilObject?)st.GetBack(skip);
                if (result == null)
                    throw new InterpreterError(InterpreterMessages._0_Not_Enough_Elements);
                return result;
            }
            catch (NotSupportedException)
            {
                throw new InterpreterError(InterpreterMessages._0_Not_Supported_By_Type, "BACK");
            }
        }

        /// <summary>
        /// Reverses the effect of REST, returning the original structure.
        /// </summary>
        /// <param name="ctx"></param>
        /// <param name="st">A structure suffix previously obtained from REST.</param>
        /// <returns>The original structure.</returns>
        /// <exception cref="InterpreterError">The type of <paramref name="st"/> does not support this operation.</exception>
        [Subr]
        public static ZilObject TOP(Context ctx, IStructure st)
        {
            try
            {
                return (ZilObject)st.GetTop();
            }
            catch (NotSupportedException)
            {
                throw new InterpreterError(InterpreterMessages._0_Not_Supported_By_Type, "TOP");
            }
        }

        /// <summary>
        /// Expands a structure by adding the specified number of empty elements at the end and/or the beginning.
        /// </summary>
        /// <param name="ctx"></param>
        /// <param name="st">A structured value.</param>
        /// <param name="end">The number of elements to add at the end.</param>
        /// <param name="beginning">The number of elements to add at the beginning.</param>
        /// <returns>The expanded structure.</returns>
        /// <exception cref="InterpreterError"><paramref name="beginning"/> or <paramref name="end"/> are negative, or the type of <paramref name="st"/> does not support this operation.</exception>
        [Subr]
        public static ZilObject GROW(Context ctx, IStructure st, int end, int beginning)
        {
            if (end < 0 || beginning < 0)
            {
                throw new InterpreterError(InterpreterMessages._0_Sizes_Must_Be_Nonnegative, "GROW");
            }

            try
            {
                if (end > 0 || beginning > 0)
                {
                    st.Grow(end, beginning, ctx.FALSE);
                }

                return (ZilObject)st.GetTop();
            }
            catch (NotSupportedException)
            {
                throw new InterpreterError(InterpreterMessages._0_Not_Supported_By_Type, "GROW");
            }
        }

        /// <summary>
        /// Returns the element at the specified index in the structure.
        /// </summary>
        /// <param name="ctx"></param>
        /// <param name="st">A structured value.</param>
        /// <param name="idx">The 1-based index of the element to retrieve.</param>
        /// <returns>The element at the specified index.</returns>
        /// <exception cref="InterpreterError"><paramref name="idx"/> is past the end of <paramref name="st"/>.</exception>
        [Subr]
        public static ZilObject NTH(Context ctx, IStructure st, int idx)
        {
            var result = st[idx - 1];
            if (result == null)
                throw new InterpreterError(InterpreterMessages._0_Reading_Past_End_Of_Structure, "NTH");

            return result;
        }

        /// <summary>
        /// Sets the element at the specified index in the structure to a new value.
        /// </summary>
        /// <param name="ctx"></param>
        /// <param name="st">A structured value.</param>
        /// <param name="idx">The 1-based index of the element to set.</param>
        /// <param name="newValue">The new value to set at the specified index.</param>
        /// <returns>The updated structure.</returns>
        /// <exception cref="InterpreterError"><paramref name="idx"/> is past the end of <paramref name="st"/>, or <paramref name="st"/> is read-only.</exception>
        [Subr]
        public static ZilObject PUT(Context ctx, IStructure st, int idx, ZilObject newValue)
        {
            try
            {
                st[idx - 1] = newValue;
            }
            catch (ArgumentOutOfRangeException)
            {
                throw new InterpreterError(InterpreterMessages._0_Writing_Past_End_Of_Structure, "PUT");
            }
            catch (NotSupportedException)
            {
                throw new InterpreterError(InterpreterMessages._0_Element_1_Is_Read_Only, "PUT", idx);
            }

            return (ZilObject)st;
        }

        /// <summary>
        /// Creates a typed offset object, combining an index, a structure DECL, and optionally a value DECL.
        /// </summary>
        /// <param name="ctx"></param>
        /// <param name="offset">The 1-based index of the element to point to.</param>
        /// <param name="structurePattern">The DECL of the structure to which the new offset will apply.</param>
        /// <param name="valuePattern">Optionally, the DECL that values at the specified index must match.</param>
        /// <returns>The new offset object.</returns>
        [Subr]
        public static ZilObject OFFSET(Context ctx, int offset, ZilObject structurePattern, ZilObject? valuePattern = null)
        {
            return new ZilOffset(offset, structurePattern, valuePattern ?? ctx.GetStdAtom(StdAtom.ANY));
        }

        /// <summary>
        /// Returns the index contained in the specified offset object.
        /// </summary>
        /// <param name="ctx"></param>
        /// <param name="offset">An offset object.</param>
        /// <returns>The 1-based index of the element pointed to by the offset object.</returns>
        [Subr]
        public static ZilObject INDEX(Context ctx, ZilOffset offset)
        {
            return new ZilFix(offset.Index);
        }

        /// <summary>
        /// Returns the length of the specified structure, potentially looping indefinitely if the structure contains a reference to itself.
        /// </summary>
        /// <param name="ctx"></param>
        /// <param name="st">A structured value.</param>
        /// <returns>The number of elements in the structure.</returns>
        [Subr]
        public static ZilObject LENGTH(Context ctx, IStructure st)
        {
            return new ZilFix(st.GetLength());
        }

        /// <summary>
        /// Returns the length of the specified structure, up to a specified maximum. This is guaranteed to return, even if the structure contains a reference to itself.
        /// </summary>
        /// <param name="ctx"></param>
        /// <param name="st">A structured value.</param>
        /// <param name="limit">The maximum length to return.</param>
        /// <returns>The length of the structure if it is less than or equal to the limit; otherwise, false.</returns>
        [Subr("LENGTH?")]
        public static ZilObject LENGTH_P(Context ctx, IStructure st, int limit)
        {
            var length = st.GetLength(limit);
            return length != null ? new ZilFix((int)length) : ctx.FALSE;
        }

        /// <summary>
        /// Sets the tail pointer of a list cell.
        /// </summary>
        /// <param name="ctx"></param>
        /// <param name="list">A list.</param>
        /// <param name="newRest">The new tail of the list.</param>
        /// <returns>The modified list.</returns>
        /// <exception cref="InterpreterError"><paramref name="list"/> is empty.</exception>
        [Subr]
        public static ZilObject PUTREST(Context ctx, ZilListoidBase list, ZilListoidBase newRest)
        {
            if (list.IsEmpty)
                throw new InterpreterError(InterpreterMessages._0_Writing_Past_End_Of_Structure, "PUTREST");

            try
            {
                if (newRest is ZilList newRestList)
                {
                    list.Rest = newRestList;
                }
                else
                {
                    list.Rest = new ZilList(newRest);
                }
            }
            catch (NotSupportedException)
            {
                throw new InterpreterError(
                    InterpreterMessages._0_Element_1_Is_Read_Only,
                    "PUTREST",
                    "'REST'");
            }

            return list;
        }

        /// <summary>
        /// Extracts the specified number of elements from a structure, optionally copying them into a destination structure.
        /// </summary>
        /// <param name="ctx"></param>
        /// <param name="from">The source structure.</param>
        /// <param name="rest">The number of source elements to skip at the start.</param>
        /// <param name="amount">The number of source elements to extract.</param>
        /// <param name="dest">The destination structure to copy elements into. If omitted, a new structure will be created.</param>
        /// <returns>The specified destination structure, or a new structure of the same primtype as the source structure.</returns>
        /// <exception cref="InterpreterError"><paramref name="amount"/> is negative, or <paramref name="from"/> or <paramref name="dest"/> are too short, or the types of <paramref name="from"/> and <paramref name="dest"/> are incompatible.</exception>
        [SuppressMessage("Microsoft.Performance", "CA1800:DoNotCastUnnecessarily")]
        [Subr]
        public static ZilObject SUBSTRUC(Context ctx, IStructure from, int rest = 0, int? amount = null,
            IStructure? dest = null)
        {
            if (amount != null)
            {
                var max = from.GetLength(rest + (int)amount);
                if (max != null && max.Value - rest < amount)
                {
                    throw new InterpreterError(
                        InterpreterMessages._0_1_Element1s_Requested_But_Only_2_Available,
                        "SUBSTRUC",
                        amount,
                        max.Value - rest);
                }
            }
            else
            {
                amount = from.GetLength() - rest;
            }

            if (amount < 0)
                throw new InterpreterError(InterpreterMessages._0_Negative_Element_Count, "SUBSTRUC");

            var fromObj = (ZilObject)from;
            var destObj = (ZilObject?)dest;
            var primitive = (IStructure)fromObj.GetPrimitive(ctx);

            if (destObj != null)
            {
                // modify an existing structure
                if (destObj.PrimType != fromObj.PrimType)
                    throw new InterpreterError(InterpreterMessages._0_Destination_Must_Have_Same_Primtype_As_Source, "SUBSTRUC");

                int i;

                switch (dest)
                {
                    case ZilListoidBase list:
                        foreach (var item in primitive.Skip(rest).Take((int)amount))
                        {
                            if (list.IsEmpty)
                                throw new InterpreterError(InterpreterMessages._0_Destination_Too_Short, "SUBSTRUC");

                            Debug.Assert(list.Rest != null);

                            list.First = item;
                            list = list.Rest;
                        }
                        break;

                    case ZilString str:
                        // this is crazy inefficient, but works with ZilString and OffsetString
                        // TODO: method on ZilString to do this more efficiently?
                        for (i = 0; i < amount; i++)
                            str[i] = primitive[i + rest]!;
                        break;

                    case ZilVector vector:
                        i = 0;
                        foreach (var item in primitive.Skip(rest).Take((int)amount))
                        {
                            if (i >= vector.GetLength())
                                throw new InterpreterError(InterpreterMessages._0_Destination_Too_Short, "SUBSTRUC");

                            vector[i++] = item;
                        }
                        break;

                    default:
                        throw new InterpreterError(InterpreterMessages._0_Destination_Type_Not_Supported_1, "SUBSTRUC", destObj.GetTypeAtom(ctx));
                }

                return destObj;
            }

            // no destination, return a new structure
            return fromObj.PrimType switch
            {
                PrimType.LIST => (ZilObject)new ZilList(primitive.Skip(rest).Take((int)amount)),
                PrimType.STRING => ZilString.FromString(((ZilString)primitive).Text.Substring(rest, (int)amount)),
                PrimType.TABLE => throw new InterpreterError(InterpreterMessages._0_Primtype_TABLE_Not_Supported,
                    "SUBSTRUC"),
                PrimType.VECTOR => new ZilVector(((ZilVector)primitive).Skip(rest).Take((int)amount).ToArray()),
                _ => throw UnhandledCaseException.FromEnum(fromObj.PrimType, "structured primtype")
            };
        }

        /// <summary>
        /// Returns the first element in a structure that matches the specified needle, using structural equality.
        /// </summary>
        /// <param name="ctx"></param>
        /// <param name="needle">The value to search for.</param>
        /// <param name="haystack">The structure in which to search.</param>
        /// <returns>The suffix of the structure starting with the first matching element, or false if no match is found.</returns>
        [Subr]
        public static ZilObject MEMBER(Context ctx, ZilObject needle, IStructure haystack)
        {
            if (needle.PrimType == PrimType.STRING && ((ZilObject)haystack).PrimType == PrimType.STRING)
            {
                string n = ((ZilString)needle.GetPrimitive(ctx)).Text;
                if (n.Length > 0)
                {
                    string h = ((ZilString)((ZilObject)haystack).GetPrimitive(ctx)).Text;
                    int pos = h.IndexOf(n, StringComparison.Ordinal);
                    if (pos >= 0)
                    {
                        return (ZilObject?)haystack.GetRest(pos) ?? ctx.FALSE;
                    }
                }
            }

            return PerformMember(ctx, needle, haystack, (a, b) => a.StructurallyEquals(b));
        }

        /// <summary>
        /// Returns the first element in a structure that matches the specified needle, using exact equality.
        /// </summary>
        /// <param name="ctx"></param>
        /// <param name="needle">The value to search for.</param>
        /// <param name="haystack">The structure in which to search.</param>
        /// <returns>The suffix of the structure starting with the first matching element, or false if no match is found.</returns>
        [Subr]
        public static ZilObject MEMQ(Context ctx, ZilObject needle, IStructure haystack)
        {
            return PerformMember(ctx, needle, haystack, (a, b) => a.ExactlyEquals(b));
        }

        static ZilObject PerformMember(Context ctx, ZilObject needle, IStructure haystack,
            Func<ZilObject, ZilObject, bool> equality)
        {
            while (haystack?.IsEmpty == false)
            {
                if (equality(needle, haystack.GetFirst()!))
                    return (ZilObject)haystack;

                haystack = haystack.GetRest(1)!;
            }

            return ctx.FALSE;
        }

#pragma warning disable CS0649
        [ZilSequenceParam]
        public struct AdditionalSortParam
        {
            public ZilVector Vector;
            [ZilOptional(Default = 1)]
            public int RecordSize;
        }
#pragma warning restore CS0649

        sealed class SortAbortedException : Exception
        {
            public ZilResult ZilResult { get; }

            public SortAbortedException(ZilResult zilResult)
            {
                ZilResult = zilResult;
            }

            public SortAbortedException(string message, Exception innerException) : base(message, innerException)
            {
            }

            public SortAbortedException()
            {
            }

            public SortAbortedException(string message) : base(message)
            {
            }
        }

        /// <summary>
        /// Sorts a vector of records according to a specified key and optional comparison predicate.
        /// </summary>
        /// <param name="ctx"></param>
        /// <param name="predicate">An applicable value to use to check whether one element is greater than another, or false to use regular numeric or string comparison.</param>
        /// <param name="vector">The vector to sort.</param>
        /// <param name="recordSize">The number of elements in each record. If omitted, defaults to 1.</param>
        /// <param name="keyOffset">The 0-based position of the sort key within each record. If omitted, defaults to 0.</param>
        /// <param name="additionalSorts">Optional additional vectors to sort at the same time, each optionally followed by a record size.</param>
        /// <returns></returns>
        /// <exception cref="InterpreterError"></exception>
        /// <exception cref="UnhandledCaseException"></exception>
        /// <exception cref="SortAbortedException"></exception>
        [Subr]
        public static ZilResult SORT(Context ctx,
            [Decl("<OR FALSE APPLICABLE>")] ZilObject predicate,
            ZilVector vector, int recordSize = 1, int keyOffset = 0,
            AdditionalSortParam[]? additionalSorts = null)
        {
            if (keyOffset < 0 || keyOffset >= recordSize)
                throw new InterpreterError(InterpreterMessages._0_Expected_0__Key_Offset__Record_Size, "SORT");

            var vectorLength = vector.GetLength();
            int numRecords = Math.DivRem(vectorLength, recordSize, out var remainder);

            if (remainder != 0)
                throw new InterpreterError(InterpreterMessages._0_Vector_Length_Must_Be_A_Multiple_Of_Record_Size, "SORT");

            if (additionalSorts != null)
            {
                foreach (var asp in additionalSorts)
                {
                    if (asp.RecordSize < 1)
                        throw new InterpreterError(InterpreterMessages._0_Expected_0__Key_Offset__Record_Size, "SORT");

                    var len = asp.Vector.GetLength();
                    int recs = Math.DivRem(len, asp.RecordSize, out var rem);

                    if (rem != 0)
                        throw new InterpreterError(InterpreterMessages._0_Vector_Length_Must_Be_A_Multiple_Of_Record_Size, "SORT");

                    if (recs != numRecords)
                        throw new InterpreterError(InterpreterMessages._0_All_Vectors_Must_Have_The_Same_Number_Of_Records, "SORT");
                }
            }

            ZilObject KeySelector(int i)
            {
                return vector[i * recordSize + keyOffset]!;
            }

            Comparison<ZilObject> comparison;

            if (predicate.IsTrue)
            {
                // user-provided comparison
                if (!predicate.IsApplicable(ctx, out var applicable))
                    throw new UnhandledCaseException("non-false, non-applicable predicate");

                var args = new ZilObject[2];
                comparison = (a, b) =>
                {
                    // greater?
                    args[0] = a;
                    args[1] = b;

                    var zr = applicable.ApplyNoEval(ctx, args);
                    if (zr.ShouldPass())
                        throw new SortAbortedException(zr);

                    if (((ZilObject)zr).IsTrue)
                        return 1;

                    // less?
                    args[0] = b;
                    args[1] = a;

                    zr = applicable.ApplyNoEval(ctx, args);
                    if (zr.ShouldPass())
                        throw new SortAbortedException(zr);

                    if (((ZilObject)zr).IsTrue)
                        return -1;

                    // equal
                    return 0;
                };
            }
            else
            {
                // default comparison
                comparison = (a, b) =>
                {
                    if (a.GetTypeAtom(ctx) != b.GetTypeAtom(ctx))
                    {
                        throw new InterpreterError(InterpreterMessages._0_Keys_Must_Have_The_Same_Type_To_Use_Default_Comparison, "SORT");
                    }

                    a = a.GetPrimitive(ctx);
                    b = b.GetPrimitive(ctx);

                    return a.PrimType switch
                    {
                        PrimType.ATOM => string.CompareOrdinal(((ZilAtom)a).Text, ((ZilAtom)b).Text),
                        PrimType.FIX => ((ZilFix)a).Value.CompareTo(((ZilFix)b).Value),
                        PrimType.STRING => string.CompareOrdinal(((ZilString)a).Text, ((ZilString)b).Text),
                        _ => throw new InterpreterError(
                            InterpreterMessages._0_Key_Primtypes_Must_Be_ATOM_FIX_Or_STRING_To_Use_Default_Comparison,
                            "SORT"),
                    };
                };
            }

            try
            {
                // sort
                var sortedIndexes =
                    Enumerable.Range(0, numRecords)
                        .OrderBy(KeySelector, Comparer<ZilObject>.Create(comparison))
                        .ToArray();

                // write output
                RearrangeVector(vector, recordSize, sortedIndexes);

                if (additionalSorts != null)
                    foreach (var asp in additionalSorts)
                        RearrangeVector(asp.Vector, asp.RecordSize, sortedIndexes);

                return vector;
            }
            catch (SortAbortedException ex)
            {
                return ex.ZilResult;
            }
            catch (Exception wrapper) when (wrapper.InnerException is SortAbortedException ex)
            {
                return ex.ZilResult;
            }
        }

        static void RearrangeVector(ZilVector vector, int recordSize, int[] desiredIndexOrder)
        {
            int length = vector.GetLength();

            ArgumentOutOfRangeException.ThrowIfLessThan(recordSize, 1);

            var output = new List<ZilObject>(length);

            foreach (var srcIndex in desiredIndexOrder)
            {
                Debug.Assert(srcIndex >= 0 && (srcIndex + 1) * recordSize <= length);

                for (int i = 0; i < recordSize; i++)
                {
                    output.Add(vector[srcIndex * recordSize + i]!);
                }
            }

            for (int i = 0; i < output.Count; i++)
            {
                vector[i] = output[i];
            }
        }
    }
}
