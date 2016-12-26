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
using System.Collections.Generic;
using System.Diagnostics.Contracts;
using System.Linq;
using Zilf.Interpreter.Values;
using Zilf.Language;
using Zilf.Diagnostics;

namespace Zilf.Interpreter
{
    static partial class Subrs
    {   
        [Subr("EMPTY?")]
        public static ZilObject EMPTY_P(Context ctx, IStructure st)
        {
            SubrContracts(ctx);

            return st.IsEmpty() ? ctx.TRUE : ctx.FALSE;
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

        [Subr]
        public static ZilObject REST(Context ctx, IStructure st, int skip = 1)
        {
            SubrContracts(ctx);

            var result = (ZilObject)st.GetRest(skip);
            if (result == null)
                throw new InterpreterError(InterpreterMessages._0_Not_Enough_Elements, "REST");
            return result;
        }

        [Subr]
        public static ZilObject BACK(Context ctx, IStructure st, int skip = 1)
        {
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

        [Subr]
        public static ZilObject TOP(Context ctx, IStructure st)
        {
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

        [Subr]
        public static ZilObject GROW(Context ctx, IStructure st, int end, int beginning)
        {
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

        [Subr]
        public static ZilObject NTH(Context ctx, IStructure st, int idx)
        {
            SubrContracts(ctx);

            ZilObject result = st[idx - 1];
            if (result == null)
                throw new InterpreterError(InterpreterMessages._0_Reading_Past_End_Of_Structure, "NTH");

            return result;
        }

        [Subr]
        public static ZilObject PUT(Context ctx, IStructure st, int idx, ZilObject newValue)
        {
            SubrContracts(ctx);

            try
            {
                st[idx - 1] = newValue;
            }
            catch (ArgumentOutOfRangeException ex)
            {
                throw new InterpreterError(InterpreterMessages._0_Writing_Past_End_Of_Structure, "PUT", ex);
            }

            return (ZilObject)st;
        }

        [Subr]
        public static ZilObject OFFSET(Context ctx, int offset, ZilObject structurePattern, ZilObject valuePattern = null)
        {
            SubrContracts(ctx);

            return new ZilOffset(offset, structurePattern, valuePattern ?? ctx.GetStdAtom(StdAtom.ANY));
        }

        [Subr]
        public static ZilObject INDEX(Context ctx, ZilOffset offset)
        {
            SubrContracts(ctx);

            return new ZilFix(offset.Index);
        }

        [Subr]
        public static ZilObject LENGTH(Context ctx, IStructure st)
        {
            SubrContracts(ctx);

            return new ZilFix(st.GetLength());
        }

        [Subr("LENGTH?")]
        public static ZilObject LENGTH_P(Context ctx, IStructure st, int limit)
        {
            SubrContracts(ctx);

            var length = st.GetLength(limit);
            return length != null ? new ZilFix((int)length) : ctx.FALSE;
        }

        [Subr]
        public static ZilObject PUTREST(Context ctx, [Decl("LIST")] ZilList list, ZilList newRest)
        {
            SubrContracts(ctx);

            if (newRest.StdTypeAtom == StdAtom.LIST)
                list.Rest = newRest;
            else
                list.Rest = new ZilList(newRest);

            return list;
        }

        [Subr]
        public static ZilObject SUBSTRUC(Context ctx, IStructure from, int rest = 0, int? amount = null, IStructure dest = null)
        {
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

            var primitive = ((ZilObject)from).GetPrimitive(ctx);

            if (dest != null)
            {
                // modify an existing structure
                if (((ZilObject)dest).PrimType != ((ZilObject)from).PrimType)
                    throw new InterpreterError(InterpreterMessages._0_Destination_Must_Have_Same_Primtype_As_Source, "SUBSTRUC");

                int i;

                switch (((ZilObject)dest).StdTypeAtom)
                {
                    case StdAtom.LIST:
                        var list = (ZilList)dest;
                        foreach (var item in ((ZilList)primitive).Skip(rest).Take((int)amount))
                        {
                            if (list.IsEmpty)
                                throw new InterpreterError(InterpreterMessages._0_Destination_Too_Short, "SUBSTRUC");

                            list.First = item;
                            list = list.Rest;
                        }
                        break;

                    case StdAtom.STRING:
                        // this is crazy inefficient, but works with ZilString and OffsetString
                        for (i = 0; i < amount; i++)
                            dest[i] = ((IStructure)primitive)[i + rest];
                        break;

                    case StdAtom.VECTOR:
                        var vector = (ZilVector)dest;
                        i = 0;
                        foreach (var item in ((ZilVector)primitive).Skip(rest).Take((int)amount))
                        {
                            if (i >= vector.GetLength())
                                throw new InterpreterError(InterpreterMessages._0_Destination_Too_Short, "SUBSTRUC");

                            vector[i++] = item;
                        }
                        break;

                    default:
                        throw new InterpreterError(InterpreterMessages._0_Destination_Type_Not_Supported_1, "SUBSTRUC", ((ZilObject)dest).GetTypeAtom(ctx));
                }

                return (ZilObject)dest;
            }

            // no destination, return a new structure
            switch (((ZilObject)from).PrimType)
            {
                case PrimType.LIST:
                    return new ZilList(((ZilList)primitive).Skip(rest).Take((int)amount));

                case PrimType.STRING:
                    return ZilString.FromString(((ZilString)primitive).Text.Substring(rest, (int)amount));

                case PrimType.TABLE:
                    throw new InterpreterError(InterpreterMessages._0_Primtype_TABLE_Not_Supported, "SUBSTRUC");

                case PrimType.VECTOR:
                    return new ZilVector(((ZilVector)primitive).Skip(rest).Take((int)amount).ToArray());

                default:
                    throw new NotImplementedException("unexpected structure primitive");
            }
        }

        [Subr]
        public static ZilObject MEMBER(Context ctx, ZilObject needle, IStructure haystack)
        {
            SubrContracts(ctx);

            return PerformMember(ctx, needle, haystack, object.Equals);
        }

        [Subr]
        public static ZilObject MEMQ(Context ctx, ZilObject needle, IStructure haystack)
        {
            SubrContracts(ctx);

            return PerformMember(ctx, needle, haystack, (a, b) =>
            {
                if (a is IStructure)
                    return (a == b);

                return a.Equals(b);
            });
        }

        static ZilObject PerformMember(Context ctx, ZilObject needle, IStructure haystack,
            Func<ZilObject, ZilObject, bool> equality)
        {
            SubrContracts(ctx);
            Contract.Requires(equality != null);

            while (haystack != null && !haystack.IsEmpty())
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

        [Subr]
        public static ZilObject SORT(Context ctx,
            [Decl("<OR FALSE APPLICABLE>")] ZilObject predicate,
            ZilVector vector, int recordSize = 1, int keyOffset = 0,
            AdditionalSortParam[] additionalSorts = null)
        {
            SubrContracts(ctx);

            if (keyOffset < 0 || keyOffset >= recordSize)
                throw new InterpreterError(InterpreterMessages._0_Expected_0__Key_Offset__Record_Size, "SORT");

            var vectorLength = vector.GetLength();
            int numRecords, remainder;
            numRecords = Math.DivRem(vectorLength, recordSize, out remainder);

            if (remainder != 0)
                throw new InterpreterError(InterpreterMessages._0_Vector_Length_Must_Be_A_Multiple_Of_Record_Size, "SORT");

            if (additionalSorts != null)
            {
                foreach (var asp in additionalSorts)
                {
                    if (asp.RecordSize < 1)
                        throw new InterpreterError(InterpreterMessages._0_Expected_0__Key_Offset__Record_Size, "SORT");

                    var len = asp.Vector.GetLength();
                    int recs, rem;
                    recs = Math.DivRem(len, asp.RecordSize, out rem);

                    if (rem != 0)
                        throw new InterpreterError(InterpreterMessages._0_Vector_Length_Must_Be_A_Multiple_Of_Record_Size, "SORT");

                    if (recs != numRecords)
                        throw new InterpreterError(InterpreterMessages._0_All_Vectors_Must_Have_The_Same_Number_Of_Records, "SORT");
                }
            }

            Func<int, ZilObject> keySelector = i => vector[i * recordSize + keyOffset];
            Comparison<ZilObject> comparison;

            if (predicate.IsTrue)
            {
                // user-provided comparison
                var applicable = predicate.AsApplicable(ctx);
                var args = new ZilObject[2];
                comparison = (a, b) =>
                {
                    // greater?
                    args[0] = a;
                    args[1] = b;

                    if (applicable.ApplyNoEval(ctx, args).IsTrue)
                        return 1;

                    // less?
                    args[0] = b;
                    args[1] = a;

                    if (applicable.ApplyNoEval(ctx, args).IsTrue)
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

            // sort
            var sortedIndexes =
                Enumerable.Range(0, numRecords)
                .OrderBy(keySelector, Comparer<ZilObject>.Create(comparison))
                .ToArray();

            // write output
            RearrangeVector(vector, recordSize, sortedIndexes);
            
            if (additionalSorts != null)
                foreach (var asp in additionalSorts)
                    RearrangeVector(asp.Vector, asp.RecordSize, sortedIndexes);

            return vector;
        }

        static void RearrangeVector(ZilVector vector, int recordSize, int[] desiredIndexOrder)
        {
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
