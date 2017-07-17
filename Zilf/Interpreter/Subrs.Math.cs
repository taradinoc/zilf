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
using System.Diagnostics.Contracts;
using System.Linq;
using Zilf.Interpreter.Values;
using Zilf.Language;
using Zilf.Diagnostics;
using JetBrains.Annotations;

namespace Zilf.Interpreter
{
    static partial class Subrs
    {
        [NotNull]
        static ZilObject PerformArithmetic(int init, [NotNull] Func<int, int, int> op, [NotNull] int[] args)
        {
            Contract.Requires(op != null);
            Contract.Requires(args != null);
            Contract.Ensures(Contract.Result<ZilObject>() != null);

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

        [NotNull]
        [Subr("+")]
        public static ZilObject Plus([NotNull] Context ctx, [NotNull] int[] args)
        {
            Contract.Requires(ctx != null);
            Contract.Requires(args != null);
            Contract.Ensures(Contract.Result<ZilObject>() != null);
            SubrContracts(ctx);

            return PerformArithmetic(0, (x, y) => x + y, args);
        }

        [NotNull]
        [Subr("-")]
        public static ZilObject Minus([NotNull] Context ctx, [NotNull] int[] args)
        {
            Contract.Requires(ctx != null);
            Contract.Requires(args != null);
            Contract.Ensures(Contract.Result<ZilObject>() != null);
            SubrContracts(ctx);

            return PerformArithmetic(0, (x, y) => x - y, args);
        }

        [NotNull]
        [Subr("*")]
        public static ZilObject Times([NotNull] Context ctx, [NotNull] int[] args)
        {
            Contract.Requires(ctx != null);
            Contract.Requires(args != null);
            Contract.Ensures(Contract.Result<ZilObject>() != null);
            SubrContracts(ctx);

            return PerformArithmetic(1, (x, y) => x * y, args);
        }

        /// <exception cref="InterpreterError">Division by zero.</exception>
        [NotNull]
        [Subr("/")]
        public static ZilObject Divide([NotNull] Context ctx, [NotNull] int[] args)
        {
            Contract.Requires(ctx != null);
            Contract.Requires(args != null);
            Contract.Ensures(Contract.Result<ZilObject>() != null);
            SubrContracts(ctx);

            try
            {
                return PerformArithmetic(1, (x, y) => x / y, args);
            }
            catch (DivideByZeroException ex)
            {
                throw new InterpreterError(InterpreterMessages.Division_By_Zero, ex);
            }
        }

        /// <exception cref="InterpreterError">Division by zero.</exception>
        [NotNull]
        [Subr]
        public static ZilObject MOD([NotNull] Context ctx, int a, int b)
        {
            Contract.Requires(ctx != null);
            Contract.Ensures(Contract.Result<ZilObject>() != null);
            SubrContracts(ctx);

            try
            {
                return new ZilFix(a % b);
            }
            catch (DivideByZeroException ex)
            {
                throw new InterpreterError(InterpreterMessages.Division_By_Zero, ex);
            }
        }

        [NotNull]
        [Subr]
        public static ZilObject LSH([NotNull] Context ctx, int a, int b)
        {
            Contract.Requires(ctx != null);
            Contract.Ensures(Contract.Result<ZilObject>() != null);
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

        [NotNull]
        [Subr]
        public static ZilObject ORB([NotNull] Context ctx, [NotNull] int[] args)
        {
            Contract.Requires(ctx != null);
            Contract.Requires(args != null);
            Contract.Ensures(Contract.Result<ZilObject>() != null);
            SubrContracts(ctx);

            return PerformArithmetic(0, (x, y) => x | y, args);
        }

        [NotNull]
        [Subr]
        public static ZilObject ANDB([NotNull] Context ctx, [NotNull] int[] args)
        {
            Contract.Requires(ctx != null);
            Contract.Requires(args != null);
            Contract.Ensures(Contract.Result<ZilObject>() != null);
            SubrContracts(ctx);

            return PerformArithmetic(-1, (x, y) => x & y, args);
        }

        [NotNull]
        [Subr]
        public static ZilObject XORB([NotNull] Context ctx, [NotNull] int[] args)
        {
            Contract.Requires(ctx != null);
            Contract.Requires(args != null);
            Contract.Ensures(Contract.Result<ZilObject>() != null);
            SubrContracts(ctx);

            return PerformArithmetic(0, (x, y) => x ^ y, args);
        }

        [CanBeNull]
        [Subr]
        public static ZilObject EQVB([NotNull] Context ctx, [NotNull] int[] args)
        {
            Contract.Requires(ctx != null);
            Contract.Requires(args != null);
            SubrContracts(ctx);

            return PerformArithmetic(-1, (x, y) => ~(x ^ y), args);
        }

        [CanBeNull]
        [Subr]
        public static ZilObject MIN([NotNull] Context ctx, [NotNull] [Required] int[] args)
        {
            Contract.Requires(ctx != null);
            Contract.Requires(args != null);
            SubrContracts(ctx);

            return new ZilFix(args.Min());
        }

        [NotNull]
        [Subr]
        public static ZilObject MAX([NotNull] Context ctx, [NotNull] [Required] int[] args)
        {
            Contract.Requires(ctx != null);
            Contract.Requires(args != null);
            Contract.Ensures(Contract.Result<ZilObject>() != null);

            SubrContracts(ctx);

            return new ZilFix(args.Max());
        }

        [NotNull]
        [Subr("OR?")]
        public static ZilObject OR_P([NotNull] Context ctx, [NotNull] [ItemNotNull] ZilObject[] args)
        {
            Contract.Requires(ctx != null);
            Contract.Requires(args != null);
            Contract.Ensures(Contract.Result<ZilObject>() != null);
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

        [NotNull]
        [Subr("AND?")]
        public static ZilObject AND_P([NotNull] Context ctx, [ItemNotNull] [NotNull] ZilObject[] args)
        {
            Contract.Requires(ctx != null);
            Contract.Requires(args != null);
            Contract.Ensures(Contract.Result<ZilObject>() != null);
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

        [NotNull]
        [Subr]
        public static ZilObject NOT([NotNull] Context ctx, [NotNull] ZilObject arg)
        {
            Contract.Requires(ctx != null);
            Contract.Requires(arg != null);
            Contract.Ensures(Contract.Result<ZilObject>() != null);
            SubrContracts(ctx);

            return arg.IsTrue ? ctx.FALSE : ctx.TRUE;
        }

        [NotNull]
        [Subr("=?")]
        public static ZilObject Eq_P([NotNull] Context ctx, [NotNull] ZilObject a, [NotNull] ZilObject b)
        {
            Contract.Requires(ctx != null);
            Contract.Requires(a != null);
            Contract.Requires(b != null);
            Contract.Ensures(Contract.Result<ZilObject>() != null);
            SubrContracts(ctx);

            return a.Equals(b) ? ctx.TRUE : ctx.FALSE;
        }

        [NotNull]
        [Subr("N=?")]
        public static ZilObject NEq_P([NotNull] Context ctx, [NotNull] ZilObject a, [NotNull] ZilObject b)
        {
            Contract.Requires(ctx != null);
            Contract.Requires(a != null);
            Contract.Requires(b != null);
            Contract.Ensures(Contract.Result<ZilObject>() != null);
            SubrContracts(ctx);

            return a.Equals(b) ? ctx.FALSE : ctx.TRUE;
        }

        [NotNull]
        [Subr("==?")]
        public static ZilObject Eeq_P([NotNull] Context ctx, [NotNull] ZilObject a, [NotNull] ZilObject b)
        {
            Contract.Requires(ctx != null);
            Contract.Requires(a != null);
            Contract.Requires(b != null);
            Contract.Ensures(Contract.Result<ZilObject>() != null);
            SubrContracts(ctx);

            bool equal;
            if (a is IStructure)
                equal = (a == b);
            else
                equal = a.Equals(b);

            return equal ? ctx.TRUE : ctx.FALSE;
        }

        [NotNull]
        [Subr("N==?")]
        public static ZilObject NEeq_P([NotNull] Context ctx, [NotNull] ZilObject a, [NotNull] ZilObject b)
        {
            Contract.Requires(ctx != null);
            Contract.Requires(a != null);
            Contract.Requires(b != null);
            Contract.Ensures(Contract.Result<ZilObject>() != null);
            SubrContracts(ctx);

            bool equal;
            if (a is IStructure)
                equal = (a == b);
            else
                equal = a.Equals(b);

            return equal ? ctx.FALSE : ctx.TRUE;
        }

        [NotNull]
        [Subr("L?")]
        public static ZilObject L_P([NotNull] Context ctx, int a, int b)
        {
            Contract.Requires(ctx != null);
            Contract.Ensures(Contract.Result<ZilObject>() != null);
            SubrContracts(ctx);

            return a < b ? ctx.TRUE : ctx.FALSE;
        }

        [NotNull]
        [Subr("L=?")]
        public static ZilObject LEq_P([NotNull] Context ctx, int a, int b)
        {
            Contract.Requires(ctx != null);
            Contract.Ensures(Contract.Result<ZilObject>() != null);
            SubrContracts(ctx);

            return a <= b ? ctx.TRUE : ctx.FALSE;
        }

        [NotNull]
        [Subr("G?")]
        public static ZilObject G_P([NotNull] Context ctx, int a, int b)
        {
            Contract.Requires(ctx != null);
            Contract.Ensures(Contract.Result<ZilObject>() != null);
            SubrContracts(ctx);

            return a > b ? ctx.TRUE : ctx.FALSE;
        }

        [NotNull]
        [Subr("G=?")]
        public static ZilObject GEq_P([NotNull] Context ctx, int a, int b)
        {
            Contract.Requires(ctx != null);
            Contract.Ensures(Contract.Result<ZilObject>() != null);
            SubrContracts(ctx);

            return a >= b ? ctx.TRUE : ctx.FALSE;
        }

        [NotNull]
        [Subr("0?")]
        public static ZilObject Zero_P([NotNull] Context ctx, [NotNull] ZilObject arg)
        {
            Contract.Requires(ctx != null);
            Contract.Requires(arg != null);
            Contract.Ensures(Contract.Result<ZilObject>() != null);
            SubrContracts(ctx);

            if (arg is ZilFix fix && fix.Value == 0)
                return ctx.TRUE;

            return ctx.FALSE;
        }

        [NotNull]
        [Subr("1?")]
        public static ZilObject One_P([NotNull] Context ctx, [NotNull] ZilObject arg)
        {
            Contract.Requires(ctx != null);
            Contract.Requires(arg != null);
            Contract.Ensures(Contract.Result<ZilObject>() != null);
            SubrContracts(ctx);

            if (arg is ZilFix fix && fix.Value == 1)
                return ctx.TRUE;

            return ctx.FALSE;
        }

    }
}
