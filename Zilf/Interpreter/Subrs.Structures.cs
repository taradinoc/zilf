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
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Diagnostics.Contracts;
using System.Linq;
using System.Runtime.Serialization;
using Zilf.Interpreter.Values;
using Zilf.Language;
using Zilf.Diagnostics;
using Zilf.Common;
using JetBrains.Annotations;

namespace Zilf.Interpreter
{
    static partial class Subrs
    {
        [NotNull]
        [Subr("EMPTY?")]
        public static ZilObject EMPTY_P([NotNull] Context ctx, [NotNull] IStructure st)
        {
            Contract.Requires(ctx != null);
            Contract.Requires(st != null);
            Contract.Ensures(Contract.Result<ZilObject>() != null);
            SubrContracts(ctx);

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

        /// <exception cref="InterpreterError"><paramref name="st"/> has fewer than <paramref name="skip"/> elements.</exception>
        [NotNull]
        [Subr]
        public static ZilObject REST(Context ctx, [NotNull] IStructure st, int skip = 1)
        {
            Contract.Requires(st != null);
            Contract.Ensures(Contract.Result<ZilObject>() != null);
            SubrContracts(ctx);

            var result = (ZilObject)st.GetRest(skip);
            if (result == null)
                throw new InterpreterError(InterpreterMessages._0_Not_Enough_Elements, "REST");
            return result;
        }

        /// <exception cref="InterpreterError">The type of <paramref name="st"/> does not support this operation, or <paramref name="st"/> has not been RESTed at least <paramref name="skip"/> elements.</exception>
        [NotNull]
        [Subr]
        public static ZilObject BACK(Context ctx, [NotNull] IStructure st, int skip = 1)
        {
            Contract.Requires(st != null);
            Contract.Ensures(Contract.Result<ZilObject>() != null);
            SubrContracts(ctx);

            try
            {
                var result = (ZilObject)st.GetBack(skip);
                if (result == null)
                    throw new InterpreterError(InterpreterMessages._0_Not_Enough_Elements);
                return result;
            }
            catch (NotSupportedException)
            {
                throw new InterpreterError(InterpreterMessages._0_Not_Supported_By_Type, "BACK");
            }
        }

        /// <exception cref="InterpreterError">The type of <paramref name="st"/> does not support this operation.</exception>
        [NotNull]
        [Subr]
        public static ZilObject TOP(Context ctx, [NotNull] IStructure st)
        {
            Contract.Requires(st != null);
            Contract.Ensures(Contract.Result<ZilObject>() != null);
            SubrContracts(ctx);

            try
            {
                return (ZilObject)st.GetTop();
            }
            catch (NotSupportedException)
            {
                throw new InterpreterError(InterpreterMessages._0_Not_Supported_By_Type, "TOP");
            }
        }

        /// <exception cref="InterpreterError"><paramref name="beginning"/> or <paramref name="end"/> are negative, or the type of <paramref name="st"/> does not support this operation.</exception>
        [NotNull]
        [Subr]
        public static ZilObject GROW(Context ctx, [NotNull] IStructure st, int end, int beginning)
        {
            Contract.Requires(st != null);
            Contract.Ensures(Contract.Result<ZilObject>() != null);
            SubrContracts(ctx);

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

        /// <exception cref="InterpreterError"><paramref name="idx"/> is past the end of <paramref name="st"/>.</exception>
        [NotNull]
        [Subr]
        public static ZilObject NTH(Context ctx, [NotNull] IStructure st, int idx)
        {
            Contract.Requires(st != null);
            Contract.Ensures(Contract.Result<ZilObject>() != null);
            SubrContracts(ctx);

            ZilObject result = st[idx - 1];
            if (result == null)
                throw new InterpreterError(InterpreterMessages._0_Reading_Past_End_Of_Structure, "NTH");

            return result;
        }

        /// <exception cref="InterpreterError"><paramref name="idx"/> is past the end of <paramref name="st"/>, or <paramref name="st"/> is read-only.</exception>
        [NotNull]
        [Subr]
        public static ZilObject PUT(Context ctx, [NotNull] IStructure st, int idx, ZilObject newValue)
        {
            Contract.Requires(st != null);
            Contract.Ensures(Contract.Result<ZilObject>() != null);
            SubrContracts(ctx);

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

        [NotNull]
        [Subr]
        public static ZilObject OFFSET(Context ctx, int offset, [NotNull] ZilObject structurePattern, [CanBeNull] ZilObject valuePattern = null)
        {
            Contract.Requires(structurePattern != null);
            Contract.Ensures(Contract.Result<ZilObject>() != null);
            SubrContracts(ctx);

            return new ZilOffset(offset, structurePattern, valuePattern ?? ctx.GetStdAtom(StdAtom.ANY));
        }

        [NotNull]
        [Subr]
        public static ZilObject INDEX(Context ctx, [NotNull] ZilOffset offset)
        {
            Contract.Requires(offset != null);
            Contract.Ensures(Contract.Result<ZilObject>() != null);
            SubrContracts(ctx);

            return new ZilFix(offset.Index);
        }

        [NotNull]
        [Subr]
        public static ZilObject LENGTH(Context ctx, [NotNull] IStructure st)
        {
            Contract.Requires(st != null);
            Contract.Ensures(Contract.Result<ZilObject>() != null);
            SubrContracts(ctx);

            return new ZilFix(st.GetLength());
        }

        [NotNull]
        [Subr("LENGTH?")]
        public static ZilObject LENGTH_P(Context ctx, [NotNull] IStructure st, int limit)
        {
            Contract.Requires(st != null);
            Contract.Ensures(Contract.Result<ZilObject>() != null);
            SubrContracts(ctx);

            var length = st.GetLength(limit);
            return length != null ? new ZilFix((int)length) : ctx.FALSE;
        }

        /// <exception cref="InterpreterError"><paramref name="list"/> is empty.</exception>
        [NotNull]
        [Subr]
        public static ZilObject PUTREST(Context ctx, [NotNull] ZilListoidBase list, [NotNull] ZilListoidBase newRest)
        {
            Contract.Requires(list != null);
            Contract.Requires(newRest != null);
            Contract.Ensures(Contract.Result<ZilObject>() != null);
            SubrContracts(ctx);

            if (list.IsEmpty)
                throw new InterpreterError(InterpreterMessages._0_Writing_Past_End_Of_Structure, "PUTREST");

            if (newRest is ZilList newRestList)
            {
                list.Rest = newRestList;
            }
            else
            {
                list.Rest = new ZilList(newRest);
            }

            return list;
        }

        /// <exception cref="InterpreterError"><paramref name="amount"/> is negative, or <paramref name="from"/> or <paramref name="dest"/> are too short, or the types of <paramref name="from"/> and <paramref name="dest"/> are incompatible.</exception>
        [NotNull]
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Performance", "CA1800:DoNotCastUnnecessarily")]
        [Subr]
        public static ZilObject SUBSTRUC(Context ctx, [NotNull] IStructure from, int rest = 0, int? amount = null,
            [CanBeNull] IStructure dest = null)
        {
            Contract.Requires(from != null);
            Contract.Ensures(Contract.Result<ZilObject>() != null);
            SubrContracts(ctx);

            if (amount != null)
            {
                var max = from.GetLength(rest + (int)amount);
                if (max != null && max.Value - rest < amount)
                    throw new InterpreterError(
                        InterpreterMessages._0_1_Element1s_Requested_But_Only_2_Available,
                        "SUBSTRUC",
                        amount,
                        max.Value - rest);
            }
            else
            {
                amount = from.GetLength() - rest;
            }

            if (amount < 0)
                throw new InterpreterError(InterpreterMessages._0_Negative_Element_Count, "SUBSTRUC");

            var fromObj = (ZilObject)from;
            var destObj = (ZilObject)dest;
            var primitive = (IStructure)fromObj.GetPrimitive(ctx);

            if (destObj != null)
            {
                // modify an existing structure
                if (destObj.PrimType != fromObj.PrimType)
                    throw new InterpreterError(InterpreterMessages._0_Destination_Must_Have_Same_Primtype_As_Source, "SUBSTRUC");

                int i;

                switch (dest)
                {
                    case ZilList list:
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
                            str[i] = primitive[i + rest];
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
            switch (fromObj.PrimType)
            {
                case PrimType.LIST:
                    return new ZilList(primitive.Skip(rest).Take((int)amount));

                case PrimType.STRING:
                    return ZilString.FromString(((ZilString)primitive).Text.Substring(rest, (int)amount));

                case PrimType.TABLE:
                    throw new InterpreterError(InterpreterMessages._0_Primtype_TABLE_Not_Supported, "SUBSTRUC");

                case PrimType.VECTOR:
                    return new ZilVector(((ZilVector)primitive).Skip(rest).Take((int)amount).ToArray());

                default:
                    throw UnhandledCaseException.FromEnum(fromObj.PrimType, "structured primtype");
            }
        }

        [NotNull]
        [Subr]
        public static ZilObject MEMBER(Context ctx, [NotNull] ZilObject needle, [NotNull] IStructure haystack)
        {
            Contract.Requires(needle != null);
            Contract.Requires(haystack != null);
            Contract.Ensures(Contract.Result<ZilObject>() != null);
            SubrContracts(ctx);

            return PerformMember(ctx, needle, haystack, Equals);
        }

        [NotNull]
        [Subr]
        public static ZilObject MEMQ(Context ctx, [NotNull] ZilObject needle, [NotNull] IStructure haystack)
        {
            Contract.Requires(needle != null);
            Contract.Requires(haystack != null);
            Contract.Ensures(Contract.Result<ZilObject>() != null);
            SubrContracts(ctx);

            return PerformMember(ctx, needle, haystack, (a, b) =>
            {
                if (a is IStructure)
                    return (a == b);

                return a.Equals(b);
            });
        }

        [NotNull]
        static ZilObject PerformMember(Context ctx, [NotNull] ZilObject needle, [NotNull] IStructure haystack,
            [NotNull] Func<ZilObject, ZilObject, bool> equality)
        {
            Contract.Requires(needle != null);
            Contract.Requires(haystack != null);
            Contract.Ensures(Contract.Result<ZilObject>() != null);
            SubrContracts(ctx);
            Contract.Requires(equality != null);

            while (haystack != null && !haystack.IsEmpty)
            {
                if (equality(needle, haystack.GetFirst()))
                    return (ZilObject)haystack;

                haystack = haystack.GetRest(1);
            }

            return ctx.FALSE;
        }

        [ZilSequenceParam]
        public struct AdditionalSortParam
        {
            public ZilVector Vector;
            [ZilOptional(Default = 1)]
            public int RecordSize;
        }

        [Serializable]
        class SortAbortedException : Exception
        {
            public ZilResult ZilResult { get; }

            public SortAbortedException(ZilResult zilResult)
            {
                ZilResult = zilResult;
            }

            protected SortAbortedException(
                [NotNull] SerializationInfo info,
                StreamingContext context) : base(info, context)
            {
                Contract.Requires(info != null);
            }
        }

        [Subr]
        public static ZilResult SORT(Context ctx,
            [NotNull] [Decl("<OR FALSE APPLICABLE>")] ZilObject predicate,
            [NotNull] ZilVector vector, int recordSize = 1, int keyOffset = 0,
            [CanBeNull] AdditionalSortParam[] additionalSorts = null)
        {
            Contract.Requires(predicate != null);
            Contract.Requires(vector != null);
            SubrContracts(ctx);

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

            ZilObject KeySelector(int i) => vector[i * recordSize + keyOffset];
            Comparison<ZilObject> comparison;

            if (predicate.IsTrue)
            {
                // user-provided comparison
                var applicable = predicate.AsApplicable(ctx);
                Debug.Assert(applicable != null);

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

                    switch (a.PrimType)
                    {
                        case PrimType.ATOM:
                            return string.Compare(((ZilAtom)a).Text, ((ZilAtom)b).Text, StringComparison.Ordinal);

                        case PrimType.FIX:
                            return ((ZilFix)a).Value.CompareTo(((ZilFix)b).Value);

                        case PrimType.STRING:
                            return string.Compare(((ZilString)a).Text, ((ZilString)b).Text, StringComparison.Ordinal);

                        default:
                            throw new InterpreterError(InterpreterMessages._0_Key_Primtypes_Must_Be_ATOM_FIX_Or_STRING_To_Use_Default_Comparison, "SORT");
                    }
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
        }

        static void RearrangeVector([NotNull] ZilVector vector, int recordSize, [NotNull] int[] desiredIndexOrder)
        {
            Contract.Requires(vector != null);
            Contract.Requires(desiredIndexOrder != null);
            var output = new List<ZilObject>(vector.GetLength());

            foreach (var srcIndex in desiredIndexOrder)
            {
                for (int i = 0; i < recordSize; i++)
                {
                    output.Add(vector[srcIndex * recordSize + i]);
                }
            }

            for (int i = 0; i < output.Count; i++)
            {
                vector[i] = output[i];
            }
        }
    }
}
