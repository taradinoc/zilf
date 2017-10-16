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
using JetBrains.Annotations;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Zilf;
using Zilf.Interpreter;
using Zilf.Interpreter.Values;

namespace ZilfTests.Interpreter
{
    internal static class TestHelpers
    {
        [CanBeNull]
        internal static ZilObject Evaluate([NotNull] string expression)
        {
            return Evaluate(null, expression);
        }

        [NotNull]
        internal static ZilObject Evaluate([CanBeNull] Context ctx, [NotNull] string expression)
        {
            if (ctx == null)
                ctx = new Context();

            return Program.Evaluate(ctx, expression, true) ?? throw new ArgumentException("Bad expression", nameof(expression));
        }

        internal static void EvalAndAssert([NotNull] string expression, [NotNull] ZilObject expected)
        {
            EvalAndAssert(null, expression, expected);
        }

        internal static void EvalAndAssert(Context ctx, [NotNull] string expression, [NotNull] ZilObject expected)
        {
            var actual = Evaluate(ctx, expression);
            if (!Equals(actual, expected))
                throw new AssertFailedException(
                    $"TestHelpers.EvalAndAssert failed. Expected:<{expected}>. Actual:<{actual}>. Expression was: {expression}");
        }

        internal static void EvalAndCatch<TException>([NotNull] string expression, [CanBeNull] Predicate<TException> predicate = null)
            where TException : Exception
        {
            EvalAndCatch(null, expression, predicate);
        }

        internal static void EvalAndCatch<TException>(Context ctx, [NotNull] string expression, [CanBeNull] Predicate<TException> predicate = null)
            where TException : Exception
        {
            const string SWrongException = "TestHelpers.EvalAndCatch failed. Expected exception:<{0}>. Actual exception:<{1}> ({4}). Expression was: {2}.\nOriginal stack trace:\n{3}";
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
                    ex.StackTrace,
                    ex.Message), ex);
            }

            if (!caught)
                throw new AssertFailedException(string.Format(SNoException,
                    typeof(TException).FullName,
                    result,
                    expression));
        }
    }
}
