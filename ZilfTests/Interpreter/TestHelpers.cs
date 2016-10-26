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

        internal static void EvalAndCatch<TException>(Context ctx, string expression, Predicate<TException> predicate = null)
            where TException : Exception
        {
            const string SWrongException = "TestHelpers.EvalAndCatch failed. Expected exception:<{0}>. Actual exception:<{1}>. Expression was: {2}.\nOriginal stack trace:\n{3}";
            const string SNoException = "TestHelpers.EvalAndCatch failed. Expected exception:<{0}>. Actual: no exception, returned <{1}>. Expression was: {2}";
            const string SPredicateFailed = "TestHelpers.EvalAndCatch failed. Predicate returned false. Exception: {0}";

            bool caught = false;
            ZilObject result = null;
            try
            {
                result = Evaluate(ctx, expression);
            }
            catch (TException ex)
            {
                caught = true;

                if (predicate != null && !predicate(ex))
                    throw new AssertFailedException(
                        string.Format(SPredicateFailed, ex));
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
