using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Zilf;

namespace ZilfTests.Interpreter
{
    internal static class TestHelpers
    {
        internal static ZilObject Evaluate(string expression)
        {
            var ctx = new Context();
            return Program.Evaluate(ctx, expression, true);
        }

        internal static void EvalAndAssert(string expression, ZilObject expected)
        {
            var actual = Evaluate(expression);
            if (!object.Equals(actual, expected))
                throw new AssertFailedException(string.Format("TestHelpers.EvalAndAssert failed. Expected:<{0}>. Actual:<{1}>. Expression was: {2}", expected, actual, expression));
        }

        internal static void EvalAndCatch<TException>(string expression)
            where TException : Exception
        {
            const string SFailure = "TestHelpers.EvalAndCatch failed. Expected:<{0}>. Actual:<{1}>. Expression was: {2}";

            bool caught = false;
            try
            {
                Evaluate(expression);
            }
            catch (TException)
            {
                caught = true;
            }
            catch (Exception ex)
            {
                throw new AssertFailedException(string.Format(SFailure, typeof(TException).FullName, ex.GetType().FullName, expression));
            }

            if (!caught)
                throw new AssertFailedException(string.Format(SFailure, typeof(TException).FullName, "(no exception)", expression));
        }
    }
}
