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
using System.Diagnostics.Contracts;
using System.Linq;
using Zilf.Interpreter.Values;
using Zilf.Language;

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
                throw new InterpreterError("REST: not enough elements");
            return result;
        }

        [Subr]
        public static ZilObject NTH(Context ctx, IStructure st, int idx)
        {
            SubrContracts(ctx);

            ZilObject result = st[idx - 1];
            if (result == null)
                throw new InterpreterError("NTH: reading past end of structure");

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
                throw new InterpreterError("PUT: writing past end of structure", ex);
            }

            return (ZilObject)st;
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

            int? length = st.GetLength(limit);
            if (length == null)
                return ctx.FALSE;
            else
                return new ZilFix(length.Value);
        }

        [Subr]
        public static ZilObject PUTREST(Context ctx, [Decl("LIST")] ZilList list, ZilList newRest)
        {
            SubrContracts(ctx);

            if (newRest.GetTypeAtom(ctx).StdAtom == StdAtom.LIST)
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
                    throw new InterpreterError(string.Format("SUBSTRUC: {0} element(s) requested but only {1} available", amount, max.Value - rest));
            }
            else
            {
                amount = from.GetLength() - rest;
            }

            if (amount < 0)
                throw new InterpreterError("SUBSTRUC: negative element count");

            var primitive = ((ZilObject)from).GetPrimitive(ctx);

            if (dest != null)
            {
                if (((ZilObject)dest).PrimType != ((ZilObject)from).PrimType)
                    throw new InterpreterError("SUBSTRUC: fourth arg must have same primtype as first");

                int i;

                switch (((ZilObject)dest).GetTypeAtom(ctx).StdAtom)
                {
                    case StdAtom.LIST:
                        var list = (ZilList)dest;
                        foreach (var item in ((ZilList)primitive).Skip(rest).Take((int)amount))
                        {
                            if (list.IsEmpty)
                                throw new InterpreterError("SUBSTRUC: destination too short");

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
                                throw new InterpreterError("SUBSTRUC: destination too short");

                            vector[i++] = item;
                        }
                        break;

                    default:
                        throw new InterpreterError("SUBSTRUC: destination type not supported: " + ((ZilObject)dest).GetTypeAtom(ctx));
                }

                return (ZilObject)dest;
            }
            else
            {
                switch (((ZilObject)from).PrimType)
                {
                    case PrimType.LIST:
                        return new ZilList(((ZilList)primitive).Skip(rest).Take((int)amount));

                    case PrimType.STRING:
                        return ZilString.FromString(((ZilString)primitive).Text.Substring(rest, (int)amount));

                    case PrimType.TABLE:
                        throw new InterpreterError("SUBSTRUC: primtype TABLE not supported");

                    case PrimType.VECTOR:
                        return new ZilVector(((ZilVector)primitive).Skip(rest).Take((int)amount).ToArray());

                    default:
                        throw new NotImplementedException("unexpected structure primitive");
                }
            }
        }

        [Subr]
        public static ZilObject MEMBER(Context ctx, ZilObject needle, IStructure haystack)
        {
            SubrContracts(ctx);

            return PerformMember(ctx, needle, haystack, (a, b) => a.Equals(b));
        }

        [Subr]
        public static ZilObject MEMQ(Context ctx, ZilObject needle, IStructure haystack)
        {
            SubrContracts(ctx);

            return PerformMember(ctx, needle, haystack, (a, b) =>
            {
                if (a is IStructure)
                    return (a == b);
                else
                    return a.Equals(b);
            });
        }

        private static ZilObject PerformMember(Context ctx, ZilObject needle, IStructure haystack,
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

    }
}
