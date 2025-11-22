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
using System.Linq;
using Zilf.Interpreter.Values;
using Zilf.Language;
using Zilf.Diagnostics;

namespace Zilf.Interpreter
{
    [System.Diagnostics.CodeAnalysis.SuppressMessage("Redundancy", "RCS1163:Unused parameter.", Justification = "Context parameter is required by the arg decoder.")]
    static partial class Subrs
    {
        static ZilFix PerformArithmetic(int init, Func<int, int, int> op, int[] args)
        {
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

        /// <summary>
        /// Adds together all of its integer arguments.
        /// </summary>
        /// <param name="ctx"></param>
        /// <param name="args">The integer arguments to add.</param>
        /// <returns>The sum of the arguments, or 0 if no arguments were provided.</returns>
        [Subr("+")]
        public static ZilObject Plus(Context ctx, int[] args)
        {
            return PerformArithmetic(0, (x, y) => x + y, args);
        }

        /// <summary>
        /// Subtracts all of its integer arguments from the first argument.
        /// </summary>
        /// <param name="ctx"></param>
        /// <param name="args">The integer arguments to subtract.</param>
        /// <returns>The result of subtracting all subsequent arguments from the first argument,
        /// or the first argument if only one was provided, or 0 if no arguments were provided.</returns>
        [Subr("-")]
        public static ZilObject Minus(Context ctx, int[] args)
        {
            return PerformArithmetic(0, (x, y) => x - y, args);
        }

        /// <summary>
        /// Multiplies together all of its integer arguments.
        /// </summary>
        /// <param name="ctx"></param>
        /// <param name="args">The integer arguments to multiply.</param>
        /// <returns>The product of the arguments, or the first argument if only one was provided,
        /// or 1 if no arguments were provided.</returns>
        [Subr("*")]
        public static ZilObject Times(Context ctx, int[] args)
        {
            return PerformArithmetic(1, (x, y) => x * y, args);
        }

        /// <summary>
        /// Divides the first integer argument by all subsequent arguments.
        /// </summary>
        /// <param name="ctx"></param>
        /// <param name="args">The integer arguments to divide.</param>
        /// <returns>The result of dividing the first argument by all subsequent arguments,
        /// or the first argument if only one was provided, or 1 if no arguments were provided.</returns>
        /// <exception cref="InterpreterError">Division by zero.</exception>
        [Subr("/")]
        public static ZilObject Divide(Context ctx, int[] args)
        {
            try
            {
                return PerformArithmetic(1, (x, y) => x / y, args);
            }
            catch (DivideByZeroException ex)
            {
                throw new InterpreterError(InterpreterMessages.Division_By_Zero, ex);
            }
        }

        /// <summary>
        /// Computes the modulus of the first integer argument by the second.
        /// </summary>
        /// <param name="ctx"></param>
        /// <param name="a">The dividend.</param>
        /// <param name="b">The divisor.</param>
        /// <returns>The remainder of dividing the first argument by the second, with the same sign as the first argument.</returns>
        /// <exception cref="InterpreterError">Division by zero.</exception>
        [Subr]
        public static ZilObject MOD(Context ctx, int a, int b)
        {
            try
            {
                return new ZilFix(a % b);
            }
            catch (DivideByZeroException ex)
            {
                throw new InterpreterError(InterpreterMessages.Division_By_Zero, ex);
            }
        }

        /// <summary>
        /// Performs a logical (unsigned) shift on an integer.
        /// </summary>
        /// <param name="ctx"></param>
        /// <param name="a">The integer to shift.</param>
        /// <param name="b">The number of bits to shift the integer left (positive) or right (negative).</param>
        /// <returns>The result of the logical shift, with zero fill.</returns>
        [Subr]
        public static ZilObject LSH(Context ctx, int a, int b)
        {
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

        /// <summary>
        /// Performs a bitwise OR on all of its integer arguments.
        /// </summary>
        /// <param name="ctx"></param>
        /// <param name="args">The integer arguments to OR together.</param>
        /// <returns>The result of the bitwise OR operation on all arguments.</returns>
        [Subr]
        public static ZilObject ORB(Context ctx, int[] args)
        {
            return PerformArithmetic(0, (x, y) => x | y, args);
        }

        /// <summary>
        /// Performs a bitwise AND on all of its integer arguments.
        /// </summary>
        /// <param name="ctx"></param>
        /// <param name="args">The integer arguments to AND together.</param>
        /// <returns>The result of the bitwise AND operation on all arguments.</returns>
        [Subr]
        public static ZilObject ANDB(Context ctx, int[] args)
        {
            return PerformArithmetic(-1, (x, y) => x & y, args);
        }

        /// <summary>
        /// Performs a bitwise XOR on all of its integer arguments.
        /// </summary>
        /// <param name="ctx"></param>
        /// <param name="args">The integer arguments to XOR together.</param>
        /// <returns>The result of the bitwise XOR operation on all arguments.</returns>
        [Subr]
        public static ZilObject XORB(Context ctx, int[] args)
        {
            return PerformArithmetic(0, (x, y) => x ^ y, args);
        }

        /// <summary>
        /// Performs a bitwise EQV (XNOR) on all of its integer arguments.
        /// </summary>
        /// <param name="ctx"></param>
        /// <param name="args">The integer arguments to EQV together.</param>
        /// <returns>The result of the bitwise EQV operation on all arguments,
        /// where an output bit of 1 indicates that the input bits are equal.</returns>
        [Subr]
        public static ZilObject? EQVB(Context ctx, int[] args)
        {
            return PerformArithmetic(-1, (x, y) => ~(x ^ y), args);
        }

        /// <summary>
        /// Returns the minimum of its integer arguments.
        /// </summary>
        /// <param name="ctx"></param>
        /// <param name="args">The integer arguments to compare.</param>
        /// <returns>The minimum value among the arguments, or the maximum FIX value if no arguments were provided.</returns>
        [Subr]
        public static ZilObject MIN(Context ctx, int[] args)
        {
            return new ZilFix(args.Length == 0 ? int.MaxValue : args.Min());
        }

        /// <summary>
        /// Returns the maximum of its integer arguments.
        /// </summary>
        /// <param name="ctx"></param>
        /// <param name="args">The integer arguments to compare.</param>
        /// <returns>The maximum value among the arguments, or the minimum FIX value if no arguments were provided.</returns>
        [Subr]
        public static ZilObject MAX(Context ctx, int[] args)
        {
            return new ZilFix(args.Length == 0 ? int.MinValue : args.Max());
        }

        /// <summary>
        /// Performs a non-short-circuiting logical OR on all of its arguments.
        /// </summary>
        /// <param name="ctx"></param>
        /// <param name="args">The arguments to OR together.</param>
        /// <returns>True if any of the arguments are true; otherwise, false.</returns>
        [Subr("OR?")]
        public static ZilObject OR_P(Context ctx, ZilObject[] args)
        {
            var result = ctx.FALSE;

            foreach (var arg in args)
            {
                result = arg;
                if (result.IsTrue)
                    return result;
            }

            return result;
        }

        /// <summary>
        /// Performs a non-short-circuiting logical AND on all of its arguments.
        /// </summary>
        /// <param name="ctx"></param>
        /// <param name="args">The arguments to AND together.</param>
        /// <returns>False if any of the arguments are false; otherwise, true.</returns>
        [Subr("AND?")]
        public static ZilObject AND_P(Context ctx, ZilObject[] args)
        {
            var result = ctx.TRUE;

            foreach (var arg in args)
            {
                result = arg;
                if (!result.IsTrue)
                    return result;
            }

            return result;
        }

        /// <summary>
        /// Returns the logical negation of its argument.
        /// </summary>
        /// <param name="ctx"></param>
        /// <param name="arg">The argument to negate.</param>
        /// <returns>True if the argument is false; otherwise, false.</returns>
        [Subr]
        public static ZilObject NOT(Context ctx, ZilObject arg)
        {
            return arg.IsTrue ? ctx.FALSE : ctx.TRUE;
        }

        /// <summary>
        /// Returns whether two values are "structurally equal", i.e., have the same type and meaning.
        /// </summary>
        /// <param name="ctx"></param>
        /// <param name="a">The first value to compare.</param>
        /// <param name="b">The second value to compare.</param>
        /// <returns>True if the two values are structurally equal; otherwise, false.</returns>
        [Subr("=?")]
        public static ZilObject Eq_P(Context ctx, ZilObject a, ZilObject b)
        {
            return a.StructurallyEquals(b) ? ctx.TRUE : ctx.FALSE;
        }

        /// <summary>
        /// Returns whether two values are not "structurally equal", i.e., have different types or meanings.
        /// </summary>
        /// <param name="ctx"></param>
        /// <param name="a">The first value to compare.</param>
        /// <param name="b">The second value to compare.</param>
        /// <returns>False if the two values are structurally equal; otherwise, true.</returns>
        [Subr("N=?")]
        public static ZilObject NEq_P(Context ctx, ZilObject a, ZilObject b)
        {
            return a.StructurallyEquals(b) ? ctx.FALSE : ctx.TRUE;
        }

        /// <summary>
        /// Returns whether two values are "exactly equal", i.e., are the same object or have the same primitive value.
        /// </summary>
        /// <param name="ctx"></param>
        /// <param name="a">The first value to compare.</param>
        /// <param name="b">The second value to compare.</param>
        /// <returns>True if the two values are exactly equal; otherwise, false.</returns>
        [Subr("==?")]
        public static ZilObject Eeq_P(Context ctx, ZilObject a, ZilObject b)
        {
            return a.ExactlyEquals(b) ? ctx.TRUE : ctx.FALSE;
        }

        /// <summary>
        /// Returns whether two values are not "exactly equal", i.e., are different objects or have different primitive values.
        /// </summary>
        /// <param name="ctx"></param>
        /// <param name="a">The first value to compare.</param>
        /// <param name="b">The second value to compare.</param>
        /// <returns>False if the two values are exactly equal; otherwise, true.</returns>
        [Subr("N==?")]
        public static ZilObject NEeq_P(Context ctx, ZilObject a, ZilObject b)
        {
            return a.ExactlyEquals(b) ? ctx.FALSE : ctx.TRUE;
        }

        /// <summary>
        /// Returns whether the first integer argument is less than the second.
        /// </summary>
        /// <param name="ctx"></param>
        /// <param name="a">The first integer to compare.</param>
        /// <param name="b">The second integer to compare.</param>
        /// <returns>True if the first argument is less than the second; otherwise, false.</returns>
        [Subr("L?")]
        public static ZilObject L_P(Context ctx, int a, int b)
        {
            return a < b ? ctx.TRUE : ctx.FALSE;
        }

        /// <summary>
        /// Returns whether the first integer argument is less than or equal to the second.
        /// </summary>
        /// <param name="ctx"></param>
        /// <param name="a">The first integer to compare.</param>
        /// <param name="b">The second integer to compare.</param>
        /// <returns>True if the first argument is less than or equal to the second; otherwise, false.</returns>
        [Subr("L=?")]
        public static ZilObject LEq_P(Context ctx, int a, int b)
        {
            return a <= b ? ctx.TRUE : ctx.FALSE;
        }

        /// <summary>
        /// Returns whether the first integer argument is greater than the second.
        /// </summary>
        /// <param name="ctx"></param>
        /// <param name="a">The first integer to compare.</param>
        /// <param name="b">The second integer to compare.</param>
        /// <returns>True if the first argument is greater than the second; otherwise, false.</returns>
        [Subr("G?")]
        public static ZilObject G_P(Context ctx, int a, int b)
        {
            return a > b ? ctx.TRUE : ctx.FALSE;
        }

        /// <summary>
        /// Returns whether the first integer argument is greater than or equal to the second.
        /// </summary>
        /// <param name="ctx"></param>
        /// <param name="a">The first integer to compare.</param>
        /// <param name="b">The second integer to compare.</param>
        /// <returns>True if the first argument is greater than or equal to the second; otherwise, false.</returns>
        [Subr("G=?")]
        public static ZilObject GEq_P(Context ctx, int a, int b)
        {
            return a >= b ? ctx.TRUE : ctx.FALSE;
        }

        /// <summary>
        /// Returns true if its argument is zero.
        /// </summary>
        /// <param name="ctx"></param>
        /// <param name="arg">The argument to check.</param>
        /// <returns>True if the argument is an integer equal to zero; otherwise, false.</returns>
        [Subr("0?")]
        public static ZilObject Zero_P(Context ctx, ZilObject arg)
        {
            if (arg is ZilFix fix && fix.Value == 0)
                return ctx.TRUE;

            return ctx.FALSE;
        }

        /// <summary>
        /// Returns true if its argument is one.
        /// </summary>
        /// <param name="ctx"></param>
        /// <param name="arg">The argument to check.</param>
        /// <returns>True if the argument is an integer equal to one; otherwise, false.</returns>
        [Subr("1?")]
        public static ZilObject One_P(Context ctx, ZilObject arg)
        {
            if (arg is ZilFix fix && fix.Value == 1)
                return ctx.TRUE;

            return ctx.FALSE;
        }

    }
}
