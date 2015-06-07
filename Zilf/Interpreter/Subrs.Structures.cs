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
        public static ZilObject EMPTY_P(Context ctx, ZilObject[] args)
        {
            SubrContracts(ctx, args);

            if (args.Length != 1)
                throw new InterpreterError("EMPTY?", 1, 1);

            IStructure st = args[0] as IStructure;
            if (st == null)
                throw new InterpreterError("EMPTY?: arg must be a structure");

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
        public static ZilObject REST(Context ctx, ZilObject[] args)
        {
            SubrContracts(ctx, args);

            if (args.Length < 1 || args.Length > 2)
                throw new InterpreterError("REST", 1, 2);

            IStructure st = args[0] as IStructure;
            if (st == null)
                throw new InterpreterError("REST: first arg must be a structure");

            int skip = 1;
            if (args.Length == 2)
            {
                ZilFix fix = args[1] as ZilFix;
                if (fix == null)
                    throw new InterpreterError("REST: second arg must be a FIX");
                skip = fix.Value;
            }

            var result = (ZilObject)st.GetRest(skip);
            if (result == null)
                throw new InterpreterError("REST: not enough elements");
            return result;
        }

        [Subr]
        public static ZilObject NTH(Context ctx, ZilObject[] args)
        {
            SubrContracts(ctx, args);

            if (args.Length != 2)
                throw new InterpreterError("NTH", 2, 2);

            IStructure st = args[0] as IStructure;
            if (st == null)
                throw new InterpreterError("NTH: first arg must be a structure");

            ZilFix idx = args[1] as ZilFix;
            if (idx == null)
                throw new InterpreterError("NTH: second arg must be a FIX");

            ZilObject result = st[idx.Value - 1];
            if (result == null)
                throw new InterpreterError("reading past end of structure");

            return result;
        }

        [Subr]
        public static ZilObject PUT(Context ctx, ZilObject[] args)
        {
            SubrContracts(ctx, args);

            if (args.Length != 3)
                throw new InterpreterError("PUT", 3, 3);

            IStructure st = args[0] as IStructure;
            if (st == null)
                throw new InterpreterError("PUT: first arg must be a structure");

            ZilFix idx = args[1] as ZilFix;
            if (idx == null)
                throw new InterpreterError("PUT: second arg must be a FIX");

            st[idx.Value - 1] = args[2];
            return args[2];
        }

        [Subr]
        public static ZilObject LENGTH(Context ctx, ZilObject[] args)
        {
            SubrContracts(ctx, args);

            if (args.Length != 1)
                throw new InterpreterError("LENGTH", 1, 1);

            IStructure st = args[0] as IStructure;
            if (st == null)
                throw new InterpreterError("LENGTH: arg must be a structure");

            return new ZilFix(st.GetLength());
        }

        [Subr("LENGTH?")]
        public static ZilObject LENGTH_P(Context ctx, ZilObject[] args)
        {
            SubrContracts(ctx, args);

            if (args.Length != 2)
                throw new InterpreterError("LENGTH?", 2, 2);

            IStructure st = args[0] as IStructure;
            if (st == null)
                throw new InterpreterError("LENGTH?: first arg must be a structure");

            ZilFix limit = args[1] as ZilFix;
            if (limit == null)
                throw new InterpreterError("LENGTH?: second arg must be a FIX");

            int? length = st.GetLength(limit.Value);
            if (length == null)
                return ctx.FALSE;
            else
                return new ZilFix(length.Value);
        }

        [Subr]
        public static ZilObject PUTREST(Context ctx, ZilObject[] args)
        {
            SubrContracts(ctx, args);

            if (args.Length != 2)
                throw new InterpreterError("PUTREST", 2, 2);

            ZilList list = args[0] as ZilList;
            if (list == null || list.GetTypeAtom(ctx).StdAtom != StdAtom.LIST)
                throw new InterpreterError("PUTREST: first arg must be a list");

            ZilList newRest = args[1] as ZilList;
            if (newRest == null)
                throw new InterpreterError("PUTREST: second arg must be a list");

            // well, not *exactly* a list...
            if (newRest.GetTypeAtom(ctx).StdAtom == StdAtom.LIST)
                list.Rest = newRest;
            else
                list.Rest = new ZilList(newRest);

            return list;
        }

        [Subr]
        public static ZilObject SUBSTRUC(Context ctx, ZilObject[] args)
        {
            SubrContracts(ctx, args);

            if (args.Length < 1 || args.Length > 4)
                throw new InterpreterError("SUBSTRUC", 1, 4);

            IStructure from = args[0] as IStructure;
            if (from == null)
                throw new InterpreterError("SUBSTRUC: first arg must be a structure");

            int rest;
            if (args.Length >= 2)
            {
                var restFix = args[1] as ZilFix;
                if (restFix == null)
                    throw new InterpreterError("SUBSTRUC: second arg must be a FIX");
                rest = restFix.Value;
            }
            else
            {
                rest = 0;
            }

            int amount;
            if (args.Length >= 3)
            {
                var amountFix = args[2] as ZilFix;
                if (amountFix == null)
                    throw new InterpreterError("SUBSTRUC: third arg must be a FIX");
                amount = amountFix.Value;

                var max = from.GetLength(rest + amount);
                if (max != null && max.Value - rest < amount)
                    throw new InterpreterError(string.Format("SUBSTRUC: {0} element(s) requested but only {1} available", amount, max.Value - rest));
            }
            else
            {
                amount = from.GetLength() - rest;
            }

            if (amount < 0)
                throw new InterpreterError("SUBSTRUC: negative element count");

            var primitive = args[0].GetPrimitive(ctx);

            if (args.Length >= 4)
            {
                var dest = args[3];
                if (dest.PrimType != args[0].PrimType)
                    throw new InterpreterError("SUBSTRUC: fourth arg must have same primtype as first");

                int i;

                switch (dest.GetTypeAtom(ctx).StdAtom)
                {
                    case StdAtom.LIST:
                        var list = (ZilList)dest;
                        foreach (var item in ((ZilList)primitive).Skip(rest).Take(amount))
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
                            ((IStructure)dest)[i] = ((IStructure)primitive)[i + rest];
                        break;

                    case StdAtom.VECTOR:
                        var vector = (ZilVector)dest;
                        i = 0;
                        foreach (var item in ((ZilVector)primitive).Skip(rest).Take(amount))
                        {
                            if (i >= vector.GetLength())
                                throw new InterpreterError("SUBSTRUC: destination too short");

                            vector[i++] = item;
                        }
                        break;

                    default:
                        throw new InterpreterError("SUBSTRUC: destination type not supported: " + dest.GetTypeAtom(ctx));
                }

                return dest;
            }
            else
            {
                switch (args[0].PrimType)
                {
                    case PrimType.LIST:
                        return new ZilList(((ZilList)primitive).Skip(rest).Take(amount));

                    case PrimType.STRING:
                        return new ZilString(((ZilString)primitive).Text.Substring(rest, amount));

                    case PrimType.TABLE:
                        throw new InterpreterError("SUBSTRUC: primtype TABLE not supported");

                    case PrimType.VECTOR:
                        return new ZilVector(((ZilVector)primitive).Skip(rest).Take(amount).ToArray());

                    default:
                        throw new NotImplementedException("unexpected structure primitive");
                }
            }
        }

        [Subr]
        public static ZilObject MEMBER(Context ctx, ZilObject[] args)
        {
            SubrContracts(ctx, args);

            return PerformMember(ctx, args, "MEMBER", (a, b) => a.Equals(b));
        }

        [Subr]
        public static ZilObject MEMQ(Context ctx, ZilObject[] args)
        {
            SubrContracts(ctx, args);

            return PerformMember(ctx, args, "MEMQ", (a, b) =>
            {
                if (a is IStructure)
                    return (a == b);
                else
                    return a.Equals(b);
            });
        }

        private static ZilObject PerformMember(Context ctx, ZilObject[] args, string name,
            Func<ZilObject, ZilObject, bool> equality)
        {
            SubrContracts(ctx, args);
            Contract.Requires(name != null);
            Contract.Requires(equality != null);

            if (args.Length != 2)
                throw new InterpreterError(name, 2, 2);

            var needle = args[0];
            var haystack = args[1] as IStructure;

            if (haystack == null)
                throw new InterpreterError(name + ": second arg must be structured");

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
