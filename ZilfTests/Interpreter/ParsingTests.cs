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

using Microsoft.VisualStudio.TestTools.UnitTesting;
using Zilf;
using Zilf.Interpreter;
using Zilf.Interpreter.Values;
// ReSharper disable InconsistentNaming

namespace ZilfTests.Interpreter
{
    [TestClass]
    public class ParsingTests
    {
        [TestMethod]
        public void TestADECL()
        {
            var ctx = new Context();

            string[][] testCases = {
                                       new[] { "FOO", "BAR" },
                                       new[] { "(1 2 3)", "LIST" },
                                       new[] { "BLAH", "<1 2 3>" },
                                       new[] { "(1 2 3)", "<1 2 3>" },
                                       new[] { ".FOO", "BAR" },
                                       new[] { ",FOO", "BAR" },
                                       new[] { "'FOO", "BAR" }
                                   };

            foreach (var tc in testCases)
            {
                var first = tc[0];
                var second = tc[1];

                var firstValue = Program.Evaluate(ctx, "'" + first, true);
                var secondValue = Program.Evaluate(ctx, "'" + second, true);
                var combined = Program.Evaluate(ctx, $"<QUOTE {first}:{second}>", true);

                Assert.IsNotNull(combined);
                Assert.IsInstanceOfType(combined, typeof(ZilAdecl));

                var adecl = (ZilAdecl)combined;

                TestHelpers.AssertStructurallyEqual(firstValue, adecl.First);
                TestHelpers.AssertStructurallyEqual(secondValue, adecl.Second);
            }
        }

        [TestMethod]
        public void TestUNPARSE()
        {
            TestHelpers.EvalAndAssert("<UNPARSE 123>", ZilString.FromString("123"));

            TestHelpers.EvalAndAssert("<UNPARSE '(\"FOO\" [BAR])>", ZilString.FromString("(\"FOO\" [BAR])"));
        }

        [TestMethod]
        public void TestBLOCK()
        {
            var ctx = new Context();
            TestHelpers.EvalAndAssert(
                ctx,
                @"
XYZZY!-MY-OBLIST
<SETG FIRST!- FOO>
<BLOCK (<GETPROP MY-OBLIST OBLIST> <ROOT>)>
<SETG SECOND!- FOO>
<ENDBLOCK>
<=? ,FIRST!- ,SECOND!->",
                ctx.FALSE);

            TestHelpers.EvalAndAssert(ctx, "<=? ,SECOND!- FOO!-MY-OBLIST>", ctx.TRUE);
        }
    }
}
