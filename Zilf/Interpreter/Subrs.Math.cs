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
        private static ZilObject PerformArithmetic(int init, Func<int, int, int> op, int[] args)
        {
            Contract.Requires(op != null);
            Contract.Requires(args != null);

            switch (args.Length)
            {
                case 0:
                    return new ZilFix(init);

                case 1:
                    return new ZilFix(op(init, args[0]));

                default:
                    int result = args[0];

                    for (int i = 1; i < args.Length; i++)
                    {
                        result = op(result, args[i]);
                    }

                    return new ZilFix(result);
            }
        }

        [Subr("+")]
        public static ZilObject Plus(Context ctx, int[] args)
        {
            SubrContracts(ctx);

            return PerformArithmetic(0, (x, y) => x + y, args);
        }

        [Subr("-")]
        public static ZilObject Minus(Context ctx, int[] args)
        {
            SubrContracts(ctx);

            return PerformArithmetic(0, (x, y) => x - y, args);
        }

        [Subr("*")]
        public static ZilObject Times(Context ctx, int[] args)
        {
            SubrContracts(ctx);

            return PerformArithmetic(1, (x, y) => x * y, args);
        }

        [Subr("/")]
        public static ZilObject Divide(Context ctx, int[] args)
        {
            SubrContracts(ctx);

            try
            {
                return PerformArithmetic(1, (x, y) => x / y, args);
            }
            catch (DivideByZeroException)
            {
                throw new InterpreterError("division by zero");
            }
        }

        [Subr]
        public static ZilObject MOD(Context ctx, int a, int b)
        {
            SubrContracts(ctx);

            try
            {
                return new ZilFix(a % b);
            }
            catch (DivideByZeroException)
            {
                throw new InterpreterError("division by zero");
            }
        }

        [Subr]
        public static ZilObject LSH(Context ctx, int a, int b)
        {
            SubrContracts(ctx);

            // "Logical shift", not left shift.
            // Positive shifts left, negative shifts right.

            int result;

            if (b >= 0)
            {
                int count = b % 256;
                result = count >= 32 ? 0 : a << count;
            }
            else
            {
                int count = -b % 256;
                result = count >= 32 ? 0 : (int)((uint)a >> count);
            }

            return new ZilFix(result);
        }

        [Subr]
        public static ZilObject ORB(Context ctx, int[] args)
        {
            SubrContracts(ctx);

            return PerformArithmetic(0, (x, y) => x | y, args);
        }

        [Subr]
        public static ZilObject ANDB(Context ctx, int[] args)
        {
            SubrContracts(ctx);

            return PerformArithmetic(-1, (x, y) => x & y, args);
        }

        [Subr]
        public static ZilObject XORB(Context ctx, int[] args)
        {
            SubrContracts(ctx);

            return PerformArithmetic(0, (x, y) => x ^ y, args);
        }

        [Subr]
        public static ZilObject EQVB(Context ctx, int[] args)
        {
            SubrContracts(ctx);

            return PerformArithmetic(-1, (x, y) => ~(x ^ y), args);
        }

        [Subr]
        public static ZilObject MIN(Context ctx, [Required] int[] args)
        {
            SubrContracts(ctx);

            return new ZilFix(args.Min());
        }

        [Subr]
        public static ZilObject MAX(Context ctx, [Required] int[] args)
        {
            SubrContracts(ctx);

            return new ZilFix(args.Max());
        }


        [Subr("OR?")]
        public static ZilObject OR_P(Context ctx, ZilObject[] args)
        {
            SubrContracts(ctx, args);

            ZilObject result = ctx.FALSE;

            foreach (ZilObject arg in args)
            {
                result = arg;
                if (result.IsTrue)
                    return result;
            }

            return result;
        }

        [Subr("AND?")]
        public static ZilObject AND_P(Context ctx, ZilObject[] args)
        {
            SubrContracts(ctx, args);

            ZilObject result = ctx.TRUE;

            foreach (ZilObject arg in args)
            {
                result = arg;
                if (!result.IsTrue)
                    return result;
            }

            return result;
        }

        [Subr]
        public static ZilObject NOT(Context ctx, ZilObject arg)
        {
            SubrContracts(ctx);

            return arg.IsTrue ? ctx.FALSE : ctx.TRUE;
        }

        [Subr("=?")]
        public static ZilObject Eq_P(Context ctx, ZilObject a, ZilObject b)
        {
            SubrContracts(ctx);

            return a.Equals(b) ? ctx.TRUE : ctx.FALSE;
        }

        [Subr("N=?")]
        public static ZilObject NEq_P(Context ctx, ZilObject a, ZilObject b)
        {
            SubrContracts(ctx);

            return a.Equals(b) ? ctx.FALSE : ctx.TRUE;
        }

        [Subr("==?")]
        public static ZilObject Eeq_P(Context ctx, ZilObject a, ZilObject b)
        {
            SubrContracts(ctx);

            bool equal;
            if (a is IStructure)
                equal = (a == b);
            else
                equal = a.Equals(b);

            return equal ? ctx.TRUE : ctx.FALSE;
        }

        [Subr("N==?")]
        public static ZilObject NEeq_P(Context ctx, ZilObject a, ZilObject b)
        {
            SubrContracts(ctx);

            bool equal;
            if (a is IStructure)
                equal = (a == b);
            else
                equal = a.Equals(b);

            return equal ? ctx.FALSE : ctx.TRUE;
        }

        [Subr("L?")]
        public static ZilObject L_P(Context ctx, int a, int b)
        {
            SubrContracts(ctx);

            return a < b ? ctx.TRUE : ctx.FALSE;
        }

        [Subr("L=?")]
        public static ZilObject LEq_P(Context ctx, int a, int b)
        {
            SubrContracts(ctx);

            return a <= b ? ctx.TRUE : ctx.FALSE;
        }

        [Subr("G?")]
        public static ZilObject G_P(Context ctx, int a, int b)
        {
            SubrContracts(ctx);

            return a > b ? ctx.TRUE : ctx.FALSE;
        }

        [Subr("G=?")]
        public static ZilObject GEq_P(Context ctx, int a, int b)
        {
            SubrContracts(ctx);

            return a >= b ? ctx.TRUE : ctx.FALSE;
        }

        [Subr("0?")]
        public static ZilObject Zero_P(Context ctx, ZilObject arg)
        {
            SubrContracts(ctx);

            if (arg is ZilFix && ((ZilFix)arg).Value == 0)
                return ctx.TRUE;
            else
                return ctx.FALSE;
        }

        [Subr("1?")]
        public static ZilObject One_P(Context ctx, ZilObject arg)
        {
            SubrContracts(ctx);

            if (arg is ZilFix && ((ZilFix)arg).Value == 1)
                return ctx.TRUE;
            else
                return ctx.FALSE;
        }

    }
}
