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

using System.Linq;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Zilf.Interpreter;
using Zilf.Interpreter.Values;
using Zilf.Language;

// ReSharper disable InconsistentNaming

namespace Zilf.Tests.Interpreter
{
    [TestClass, TestCategory("Interpreter"), TestCategory("Parsing")]
    public class ParsingTests
    {
        [TestMethod]
        public void TestADECL()
        {
            var ctx = new Context();

            string[][] testCases = [
                                       ["FOO", "BAR"],
                                       ["(1 2 3)", "LIST"],
                                       ["BLAH", "<1 2 3>"],
                                       ["(1 2 3)", "<1 2 3>"],
                                       [".FOO", "BAR"],
                                       [",FOO", "BAR"],
                                       ["'FOO", "BAR"]
                                   ];

            foreach (var tc in testCases)
            {
                var first = tc[0];
                var second = tc[1];

                var firstValue = Program.Evaluate(ctx, "'" + first, true);
                var secondValue = Program.Evaluate(ctx, "'" + second, true);
                var combined = Program.Evaluate(ctx, $"<QUOTE {first}:{second}>", true);

                Assert.IsNotNull(combined);
                Assert.IsInstanceOfType<ZilAdecl>(combined);

                var adecl = (ZilAdecl)combined!;

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

        [TestMethod]
        public void TestSourceLineOverride()
        {
            var ctx = new Context();
            var src = new StringSourceLine("<test>");

            var result = Program.Parse(ctx, src, "<PRINT <GETP CR ,SPACE-TEXT>>").ToArray();

            Assert.AreEqual(1, result.Length);
            Assert.IsInstanceOfType<StringSourceLine>(result[0].SourceLine);
            Assert.AreEqual("<test>", result[0].SourceLine!.ToString());

            Assert.IsInstanceOfType<ZilForm>(result[0]);
            var form = (ZilForm)result[0];
            Assert.IsTrue(form.HasLength(2), "Expected form to have two elements");
            Assert.IsNotNull(form.First);
            Assert.IsInstanceOfType<StringSourceLine>(form.First.SourceLine);
            Assert.AreEqual("<test>", form.First.SourceLine!.ToString());

            Assert.IsNotNull(form.Rest);
            Assert.IsInstanceOfType<ZilForm>(form.Rest.First);
            var innerForm = (ZilForm)form.Rest.First;
            Assert.IsTrue(innerForm.HasLength(3), "Expected inner form to have three elements");
            Assert.IsNotNull(innerForm.Rest);
            Assert.IsNotNull(innerForm.Rest.First);
            Assert.IsInstanceOfType<StringSourceLine>(innerForm.Rest.First.SourceLine);
            Assert.AreEqual("<test>", innerForm.Rest.First.SourceLine!.ToString());
        }
    }
}
