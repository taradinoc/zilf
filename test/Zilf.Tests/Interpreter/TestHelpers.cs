/* Copyright 2010-2018 Jesse McGrew
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
using Zilf.Interpreter;
using Zilf.Interpreter.Values;

namespace Zilf.Tests.Interpreter
{
    internal static class TestHelpers
    {
        [NotNull]
        internal static ZilObject Evaluate([NotNull] string expression) => Evaluate(null, expression);

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
            if (!actual.StructurallyEquals(expected))
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

            ZilObject result;

            try
            {
                result = Evaluate(ctx, expression);
            }
            catch (TException ex) when (predicate == null || predicate(ex))
            {
                // expected exception type, predicate passed (or no predicate)
                return;
            }
            catch (TException ex)
            {
                // expected exception type, predicate failed
                throw new AssertFailedException(
                    string.Format(SPredicateFailed, ex));
            }
            catch (Exception ex)
            {
                // unexpected exception type
                throw new AssertFailedException(string.Format(SWrongException,
                    typeof(TException).FullName,
                    ex.GetType().FullName,
                    expression,
                    ex.StackTrace,
                    ex.Message), ex);
            }

            // no exception was thrown
            throw new AssertFailedException(string.Format(SNoException,
                typeof(TException).FullName,
                result,
                expression));
        }

        [AssertionMethod]
        internal static void AssertStructurallyEqual([CanBeNull] ZilObject expected, [CanBeNull] ZilObject actual, [CanBeNull] string message = null)
        {
            bool ok;
            if (expected == null || actual == null)
            {
                ok = ReferenceEquals(expected, actual);
            }
            else
            {
                ok = expected.StructurallyEquals(actual);
            }

            if (!ok)
            {
                message = message ?? $"{nameof(TestHelpers)}.{nameof(AssertStructurallyEqual)} failed";
                throw new AssertFailedException($"{message}. Expected:<{expected}>. Actual:<{actual}>.");
            }
        }

        [AssertionMethod]
        [StringFormatMethod("format")]
        internal static void AssertStructurallyEqual([CanBeNull] ZilObject expected, [CanBeNull] ZilObject actual, [NotNull] string format, [NotNull] params object[] args)
        {
            bool ok;
            if (expected == null || actual == null)
            {
                ok = ReferenceEquals(expected, actual);
            }
            else
            {
                ok = expected.StructurallyEquals(actual);
            }

            if (!ok)
            {
                var message = string.Format(format, args);
                throw new AssertFailedException($"{message}. Expected:<{expected}>. Actual:<{actual}>.");
            }
        }

        [AssertionMethod]
        public static void AssertNotStructurallyEqual([CanBeNull] ZilObject notExpected, [CanBeNull] ZilObject actual)
        {
            bool ok;
            if (notExpected == null || actual == null)
            {
                ok = !ReferenceEquals(notExpected, actual);
            }
            else
            {
                ok = !notExpected.StructurallyEquals(actual);
            }

            if (!ok)
            {
                throw new AssertFailedException(
                    $"{nameof(TestHelpers)}.{nameof(AssertNotStructurallyEqual)} failed. Not expected:<{notExpected}>. Actual:<{actual}>.");
            }
        }

        [AssertionMethod]
        public static void AssertStructurallyEqual([NotNull] ZilObject[] expected, [NotNull] ZilObject[] actual, [CanBeNull] string message = null)
        {
            message = message ?? $"{nameof(TestHelpers)}.{nameof(AssertStructurallyEqual)} failed";

            Assert.AreEqual(expected.Length, actual.Length, $"{message}. Array lengths differ");

            for (int i = 0; i < expected.Length; i++)
            {
                AssertStructurallyEqual(expected[i], actual[i], $"{message}. Arrays differ at position {0}", i);
            }
        }
    }
}
