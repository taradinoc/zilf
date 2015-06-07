using System;
using System.Diagnostics.Contracts;
using System.Linq;
using Zilf.Interpreter.Values;
using Zilf.Language;

namespace Zilf.Interpreter
{
    static partial class Subrs
    {
        private static ZilObject PerformArithmetic(int init, string name, Func<int, int, int> op,
            ZilObject[] args)
        {
            Contract.Requires(name != null);
            Contract.Requires(op != null);
            Contract.Requires(args != null);

            const string STypeError = "every arg must be a FIX";

            switch (args.Length)
            {
                case 0:
                    return new ZilFix(init);

                case 1:
                    if (!(args[0] is ZilFix))
                        throw new InterpreterError(name + ": " + STypeError);
                    else
                        return new ZilFix(op(init, ((ZilFix)args[0]).Value));

                default:
                    if (!(args[0] is ZilFix))
                        throw new InterpreterError(name + ": " + STypeError);

                    int result = ((ZilFix)args[0]).Value;

                    for (int i = 1; i < args.Length; i++)
                    {
                        if (!(args[i] is ZilFix))
                            throw new InterpreterError(name + ": " + STypeError);

                        result = op(result, ((ZilFix)args[i]).Value);
                    }

                    return new ZilFix(result);
            }
        }

        [Subr("+")]
        public static ZilObject Plus(Context ctx, ZilObject[] args)
        {
            SubrContracts(ctx, args);

            return PerformArithmetic(0, "+", (x, y) => x + y, args);
        }

        [Subr("-")]
        public static ZilObject Minus(Context ctx, ZilObject[] args)
        {
            SubrContracts(ctx, args);

            return PerformArithmetic(0, "-", (x, y) => x - y, args);
        }

        [Subr("*")]
        public static ZilObject Times(Context ctx, ZilObject[] args)
        {
            SubrContracts(ctx, args);

            return PerformArithmetic(1, "*", (x, y) => x * y, args);
        }

        [Subr("/")]
        public static ZilObject Divide(Context ctx, ZilObject[] args)
        {
            SubrContracts(ctx, args);

            try
            {
                return PerformArithmetic(1, "/", (x, y) => x / y, args);
            }
            catch (DivideByZeroException)
            {
                throw new InterpreterError("division by zero");
            }
        }

        [Subr]
        public static ZilObject MOD(Context ctx, ZilObject[] args)
        {
            SubrContracts(ctx, args);

            if (args.Length != 2)
                throw new InterpreterError("MOD", 2, 2);

            var a = args[0] as ZilFix;
            var b = args[1] as ZilFix;

            if (a == null || b == null)
                throw new InterpreterError("MOD: every arg must be a FIX");

            try
            {
                return new ZilFix(a.Value % b.Value);
            }
            catch (DivideByZeroException)
            {
                throw new InterpreterError("division by zero");
            }
        }

        [Subr]
        public static ZilObject LSH(Context ctx, ZilObject[] args)
        {
            SubrContracts(ctx, args);

            // "Logical shift", not left shift.
            // Positive shifts left, negative shifts right.
            
            if (args.Length != 2)
                throw new InterpreterError("LSH", 2, 2);

            var a = args[0] as ZilFix;
            var b = args[1] as ZilFix;

            if (a == null || b == null)
                throw new InterpreterError("LSH: every arg must be a FIX");

            int result;

            if (b.Value >= 0)
            {
                int count = b.Value % 256;
                result = count >= 32 ? 0 : a.Value << count;
            }
            else
            {
                int count = -b.Value % 256;
                result = count >= 32 ? 0 : (int)((uint)a.Value >> count);
            }

            return new ZilFix(result);
        }

        [Subr]
        public static ZilObject ORB(Context ctx, ZilObject[] args)
        {
            SubrContracts(ctx, args);

            return PerformArithmetic(0, "ORB", (x, y) => x | y, args);
        }

        [Subr]
        public static ZilObject ANDB(Context ctx, ZilObject[] args)
        {
            SubrContracts(ctx, args);

            return PerformArithmetic(-1, "ANDB", (x, y) => x & y, args);
        }

        [Subr]
        public static ZilObject XORB(Context ctx, ZilObject[] args)
        {
            SubrContracts(ctx, args);

            return PerformArithmetic(0, "XORB", (x, y) => x ^ y, args);
        }

        [Subr]
        public static ZilObject EQVB(Context ctx, ZilObject[] args)
        {
            SubrContracts(ctx, args);

            return PerformArithmetic(-1, "EQVB", (x, y) => ~(x ^ y), args);
        }

        [Subr]
        public static ZilObject MIN(Context ctx, ZilObject[] args)
        {
            SubrContracts(ctx, args);

            if (args.Length < 1)
                throw new InterpreterError("MIN", 1, 0);

            if (!args.All(zo => zo is ZilFix))
                throw new InterpreterError("MIN: all args must be FIXes");

            return args.OrderBy(zo => ((ZilFix)zo).Value).First();
        }

        [Subr]
        public static ZilObject MAX(Context ctx, ZilObject[] args)
        {
            SubrContracts(ctx, args);

            if (args.Length < 1)
                throw new InterpreterError("MAX", 1, 0);

            if (!args.All(zo => zo is ZilFix))
                throw new InterpreterError("MAX: all args must be FIXes");

            return args.OrderByDescending(zo => ((ZilFix)zo).Value).First();
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
        public static ZilObject NOT(Context ctx, ZilObject[] args)
        {
            SubrContracts(ctx, args);

            if (args.Length != 1)
                throw new InterpreterError("NOT", 1, 1);

            return args[0].IsTrue ? ctx.FALSE : ctx.TRUE;
        }

        [Subr("=?")]
        public static ZilObject Eq_P(Context ctx, ZilObject[] args)
        {
            SubrContracts(ctx, args);

            if (args.Length != 2)
                throw new InterpreterError("=?", 2, 2);

            return args[0].Equals(args[1]) ? ctx.TRUE : ctx.FALSE;
        }

        [Subr("N=?")]
        public static ZilObject NEq_P(Context ctx, ZilObject[] args)
        {
            SubrContracts(ctx, args);

            if (args.Length != 2)
                throw new InterpreterError("N=?", 2, 2);

            return args[0].Equals(args[1]) ? ctx.FALSE : ctx.TRUE;
        }

        [Subr("==?")]
        public static ZilObject Eeq_P(Context ctx, ZilObject[] args)
        {
            SubrContracts(ctx, args);

            if (args.Length != 2)
                throw new InterpreterError("==?", 2, 2);

            bool equal;
            if (args[0] is IStructure)
                equal = (args[0] == args[1]);
            else
                equal = args[0].Equals(args[1]);

            return equal ? ctx.TRUE : ctx.FALSE;
        }

        [Subr("N==?")]
        public static ZilObject NEeq_P(Context ctx, ZilObject[] args)
        {
            SubrContracts(ctx, args);

            if (args.Length != 2)
                throw new InterpreterError("N==?", 2, 2);

            bool equal;
            if (args[0] is IStructure)
                equal = (args[0] == args[1]);
            else
                equal = args[0].Equals(args[1]);

            return equal ? ctx.FALSE : ctx.TRUE;
        }

        private static ZilObject PerformComparison(Context ctx, string name, Func<int, int, bool> op, ZilObject[] args)
        {
            Contract.Requires(ctx != null);
            Contract.Requires(name != null);
            Contract.Requires(op != null);
            Contract.Requires(args != null && Contract.ForAll(args, a => a != null));

            const string STypeError = "every arg must be a FIX";

            if (args.Length != 2)
                throw new InterpreterError(name, 2, 2);

            if (!(args[0] is ZilFix && args[1] is ZilFix))
                throw new InterpreterError(name + ": " + STypeError);

            int value1 = ((ZilFix)args[0]).Value;
            int value2 = ((ZilFix)args[1]).Value;

            return op(value1, value2) ? ctx.TRUE : ctx.FALSE;
        }

        [Subr("L?")]
        public static ZilObject L_P(Context ctx, ZilObject[] args)
        {
            SubrContracts(ctx, args);

            return PerformComparison(ctx, "L?", (a, b) => a < b, args);
        }

        [Subr("L=?")]
        public static ZilObject LEq_P(Context ctx, ZilObject[] args)
        {
            SubrContracts(ctx, args);

            return PerformComparison(ctx, "L=?", (a, b) => a <= b, args);
        }

        [Subr("G?")]
        public static ZilObject G_P(Context ctx, ZilObject[] args)
        {
            SubrContracts(ctx, args);

            return PerformComparison(ctx, "G?", (a, b) => a > b, args);
        }

        [Subr("G=?")]
        public static ZilObject GEq_P(Context ctx, ZilObject[] args)
        {
            SubrContracts(ctx, args);

            return PerformComparison(ctx, "G=?", (a, b) => a >= b, args);
        }

        [Subr("0?")]
        public static ZilObject Zero_P(Context ctx, ZilObject[] args)
        {
            SubrContracts(ctx, args);

            if (args.Length != 1)
                throw new InterpreterError("0?", 1, 1);

            if (args[0] is ZilFix && ((ZilFix)args[0]).Value == 0)
                return ctx.TRUE;
            else
                return ctx.FALSE;
        }

        [Subr("1?")]
        public static ZilObject One_P(Context ctx, ZilObject[] args)
        {
            SubrContracts(ctx, args);

            if (args.Length != 1)
                throw new InterpreterError("1?", 1, 1);

            if (args[0] is ZilFix && ((ZilFix)args[0]).Value == 1)
                return ctx.TRUE;
            else
                return ctx.FALSE;
        }

    }
}
