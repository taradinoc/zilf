using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using Zilf;
using Zilf.Interpreter;
using Zilf.Interpreter.Values;

namespace ZilfTests.Interpreter
{
    internal static class TestHelpers
    {
        internal static ZilObject Evaluate(string expression)
        {
            return Evaluate(null, expression);
        }

        internal static ZilObject Evaluate(Context ctx, string expression)
        {
            if (ctx == null)
                ctx = new Context();

            return Program.Evaluate(ctx, expression, true);
        }

        internal static void EvalAndAssert(string expression, ZilObject expected)
        {
            EvalAndAssert(null, expression, expected);
        }

        internal static void EvalAndAssert(Context ctx, string expression, ZilObject expected)
        {
            var actual = Evaluate(ctx, expression);
            if (!object.Equals(actual, expected))
                throw new AssertFailedException(string.Format("TestHelpers.EvalAndAssert failed. Expected:<{0}>. Actual:<{1}>. Expression was: {2}", expected, actual, expression));
        }

        internal static void EvalAndCatch<TException>(string expression)
            where TException : Exception
        {
            EvalAndCatch<TException>(null, expression);
        }

        internal static void EvalAndCatch<TException>(Context ctx, string expression)
            where TException : Exception
        {
            const string SWrongException = "TestHelpers.EvalAndCatch failed. Expected exception:<{0}>. Actual exception:<{1}>. Expression was: {2}.\nOriginal stack trace:\n{3}";
            const string SNoException = "TestHelpers.EvalAndCatch failed. Expected exception:<{0}>. Actual: no exception, returned <{1}>. Expression was: {2}";

            bool caught = false;
            ZilObject result = null;
            try
            {
                result = Evaluate(ctx, expression);
            }
            catch (TException)
            {
                caught = true;
            }
            catch (Exception ex)
            {
                throw new AssertFailedException(string.Format(SWrongException,
                    typeof(TException).FullName,
                    ex.GetType().FullName,
                    expression,
                    ex.StackTrace), ex);
            }

            if (!caught)
                throw new AssertFailedException(string.Format(SNoException,
                    typeof(TException).FullName,
                    result,
                    expression));
        }
    }
}
