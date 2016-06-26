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
using Zilf.Interpreter;
using Zilf.Interpreter.Values;
using Zilf.Language;

namespace ZilfTests.Interpreter
{
    [TestClass]
    public class AtomTests
    {
        [TestMethod]
        public void TestSPNAME()
        {
            TestHelpers.EvalAndAssert("<SPNAME FOO>", ZilString.FromString("FOO"));
            TestHelpers.EvalAndAssert("<SPNAME +>", ZilString.FromString("+"));

            // must have 1 argument
            TestHelpers.EvalAndCatch<InterpreterError>("<SPNAME>");
            TestHelpers.EvalAndCatch<InterpreterError>("<SPNAME FOO BAR>");

            // argument must be an atom
            TestHelpers.EvalAndCatch<InterpreterError>("<SPNAME 5>");
            TestHelpers.EvalAndCatch<InterpreterError>("<SPNAME (1 2 3)>");
            TestHelpers.EvalAndCatch<InterpreterError>("<SPNAME \"hello\">");
        }

        [TestMethod]
        public void TestPARSE()
        {
            var ctx = new Context();

            var expected = new ZilAtom("FOO", ctx.RootObList, StdAtom.None);
            ctx.RootObList[expected.Text] = expected;

            var actual = TestHelpers.Evaluate(ctx, "<PARSE \"FOO\">");
            Assert.AreSame(expected, actual);

            expected = ctx.GetStdAtom(StdAtom.Plus);
            actual = TestHelpers.Evaluate(ctx, "<PARSE \"+\">");
            Assert.AreSame(expected, actual);

            expected = ctx.PackageObList["+"];
            actual = TestHelpers.Evaluate(ctx, "<PARSE \"+\" 10 <GETPROP PACKAGE OBLIST>>");
            Assert.AreSame(expected, actual);
            Assert.AreNotSame(ctx.GetStdAtom(StdAtom.Plus), actual);

            // must have 1 argument
            TestHelpers.EvalAndCatch<InterpreterError>("<PARSE>");
            TestHelpers.EvalAndCatch<InterpreterError>("<PARSE \"FOO\" \"BAR\">");

            // argument must be a string
            TestHelpers.EvalAndCatch<InterpreterError>("<PARSE 5>");
            TestHelpers.EvalAndCatch<InterpreterError>("<PARSE (\"FOO\")>");
        }

        [TestMethod]
        public void TestSETG()
        {
            var ctx = new Context();

            var expected = new ZilFix(123);
            TestHelpers.EvalAndAssert(ctx, "<SETG FOO 123>", expected);

            var stored = ctx.GetGlobalVal(ZilAtom.Parse("FOO", ctx));
            Assert.AreEqual(expected, stored);

            // must have 2 arguments
            TestHelpers.EvalAndCatch<InterpreterError>("<SETG>");
            TestHelpers.EvalAndCatch<InterpreterError>("<SETG FOO>");
            TestHelpers.EvalAndCatch<InterpreterError>("<SETG FOO 123 BAR>");

            // 2nd argument must be an atom
            TestHelpers.EvalAndCatch<InterpreterError>("<SETG \"FOO\" 5>");
        }

        [TestMethod]
        public void TestSET()
        {
            var ctx = new Context();

            var expected = new ZilFix(123);
            TestHelpers.EvalAndAssert(ctx, "<SET FOO 123>", expected);

            var stored = ctx.GetLocalVal(ZilAtom.Parse("FOO", ctx));
            Assert.AreEqual(expected, stored);

            // must have 2 arguments
            TestHelpers.EvalAndCatch<InterpreterError>("<SET>");
            TestHelpers.EvalAndCatch<InterpreterError>("<SET FOO>");
            TestHelpers.EvalAndCatch<InterpreterError>("<SET FOO 123 BAR>");

            // 2nd argument must be an atom
            TestHelpers.EvalAndCatch<InterpreterError>("<SET \"FOO\" 5>");
        }

        [TestMethod]
        public void TestGVAL()
        {
            var ctx = new Context();

            var expected = new ZilFix(123);
            ctx.SetGlobalVal(ZilAtom.Parse("FOO", ctx), expected);
            var actual = TestHelpers.Evaluate(ctx, "<GVAL FOO>");
            Assert.AreEqual(expected, actual);

            // fails when undefined
            TestHelpers.EvalAndCatch<InterpreterError>("<GVAL TESTING-TESTING-THIS-ATOM-HAS-NO-GVAL>");

            // must have 1 argument
            TestHelpers.EvalAndCatch<InterpreterError>("<GVAL>");
            TestHelpers.EvalAndCatch<InterpreterError>("<GVAL FOO BAR>");

            // argument must be an atom
            TestHelpers.EvalAndCatch<InterpreterError>("<GVAL \"FOO\">");
        }

        [TestMethod]
        public void TestLVAL()
        {
            var ctx = new Context();

            var expected = new ZilFix(123);
            ctx.SetLocalVal(ZilAtom.Parse("FOO", ctx), expected);
            var actual = TestHelpers.Evaluate(ctx, "<LVAL FOO>");
            Assert.AreEqual(expected, actual);

            // fails when undefined
            TestHelpers.EvalAndCatch<InterpreterError>("<LVAL TESTING-TESTING-THIS-ATOM-HAS-NO-LVAL>");

            // must have 1 argument
            TestHelpers.EvalAndCatch<InterpreterError>("<LVAL>");
            TestHelpers.EvalAndCatch<InterpreterError>("<LVAL FOO BAR>");

            // argument must be an atom
            TestHelpers.EvalAndCatch<InterpreterError>("<LVAL \"FOO\">");
        }

        [TestMethod]
        public void TestGASSIGNED_P()
        {
            var ctx = new Context();

            var whatever = new ZilFix(123);
            ctx.SetGlobalVal(ZilAtom.Parse("MY-TEST-GLOBAL", ctx), whatever);
            ctx.SetLocalVal(ZilAtom.Parse("MY-TEST-LOCAL", ctx), whatever);

            TestHelpers.EvalAndAssert(ctx, "<GASSIGNED? MY-TEST-GLOBAL>", ctx.TRUE);
            TestHelpers.EvalAndAssert(ctx, "<GASSIGNED? MY-TEST-LOCAL>", ctx.FALSE);
            TestHelpers.EvalAndAssert(ctx, "<GASSIGNED? THIS-ATOM-HAS-NO-GVAL-OR-LVAL>", ctx.FALSE);

            // must have 1 argument
            TestHelpers.EvalAndCatch<InterpreterError>("<GASSIGNED?>");
            TestHelpers.EvalAndCatch<InterpreterError>("<GASSIGNED? FOO BAR>");

            // argument must be an atom
            TestHelpers.EvalAndCatch<InterpreterError>("<GASSIGNED? \"FOO\">");
        }

        [TestMethod]
        public void TestASSIGNED_P()
        {
            var ctx = new Context();

            var whatever = new ZilFix(123);
            ctx.SetGlobalVal(ZilAtom.Parse("MY-TEST-GLOBAL", ctx), whatever);
            ctx.SetLocalVal(ZilAtom.Parse("MY-TEST-LOCAL", ctx), whatever);

            TestHelpers.EvalAndAssert(ctx, "<ASSIGNED? MY-TEST-LOCAL>", ctx.TRUE);
            TestHelpers.EvalAndAssert(ctx, "<ASSIGNED? MY-TEST-GLOBAL>", ctx.FALSE);
            TestHelpers.EvalAndAssert(ctx, "<ASSIGNED? THIS-ATOM-HAS-NO-GVAL-OR-LVAL>", ctx.FALSE);

            // must have 1 argument
            TestHelpers.EvalAndCatch<InterpreterError>("<ASSIGNED?>");
            TestHelpers.EvalAndCatch<InterpreterError>("<ASSIGNED? FOO BAR>");

            // argument must be an atom
            TestHelpers.EvalAndCatch<InterpreterError>("<ASSIGNED? \"FOO\">");
        }

        [TestMethod]
        public void TestGUNASSIGN()
        {
            var ctx = new Context();

            var foo = ZilAtom.Parse("FOO", ctx);
            ctx.SetGlobalVal(foo, new ZilFix(123));

            TestHelpers.Evaluate(ctx, "<GUNASSIGN FOO>");
            TestHelpers.EvalAndAssert(ctx, "<GASSIGNED? FOO>", ctx.FALSE);
            TestHelpers.EvalAndCatch<InterpreterError>("<GVAL FOO>");
        }

        [TestMethod]
        public void TestUNASSIGN()
        {
            var ctx = new Context();

            var foo = ZilAtom.Parse("FOO", ctx);
            ctx.SetLocalVal(foo, new ZilFix(123));

            TestHelpers.Evaluate(ctx, "<UNASSIGN FOO>");
            TestHelpers.EvalAndAssert(ctx, "<ASSIGNED? FOO>", ctx.FALSE);
            TestHelpers.EvalAndCatch<InterpreterError>("<LVAL FOO>");
        }

        [TestMethod]
        public void TestGDECL()
        {
            var ctx = new Context();

            TestHelpers.EvalAndAssert(ctx, "<GDECL (FOO) BAR>", ctx.FALSE);

            // must have an even number of arguments
            TestHelpers.EvalAndCatch<InterpreterError>(ctx, "<GDECL (FOO)>");
            TestHelpers.EvalAndCatch<InterpreterError>(ctx, "<GDECL (FOO) BAR (BAZ)>");

            // odd numbered arguments must be lists of atoms
            TestHelpers.EvalAndCatch<InterpreterError>(ctx, "<GDECL FOO BAR>");
            TestHelpers.EvalAndCatch<InterpreterError>(ctx, "<GDECL [FOO] BAR>");
        }

        [TestMethod]
        public void TestLOOKUP()
        {
            var ctx = new Context();
            var fooAtom = ctx.RootObList["FOO"];
            TestHelpers.EvalAndAssert(ctx, "<LOOKUP \"FOO\" <ROOT>>", fooAtom);
        }

        [TestMethod]
        public void TestINSERT()
        {
            TestHelpers.EvalAndAssert("<SPNAME <INSERT \"FOO\" <ROOT>>>", ZilString.FromString("FOO"));
        }

        [TestMethod]
        public void TestROOT()
        {
            var ctx = new Context();
            TestHelpers.EvalAndAssert(ctx, "<ROOT>", ctx.RootObList);
        }

        [TestMethod]
        public void All_Predefined_Atoms_Should_Be_On_ROOT_ObList()
        {
            var ctx = new Context();

            var offset = 0;

            foreach (var zo in (ZilList)ctx.GetLocalVal(ctx.GetStdAtom(StdAtom.OBLIST)))
            {
                var oblist = zo as ObList;
                if (oblist == null)
                    continue;

                var atomList = (ZilList)oblist.GetPrimitive(ctx);

                if (oblist != ctx.RootObList && !atomList.IsEmpty)
                    Assert.Fail("Expected non-root oblist at offset {0} to be empty, but found: {1}", offset, oblist.ToString());

                offset++;
            }
        }

        [TestMethod]
        public void Test_ObList_Trailers()
        {
            var ctx = new Context();
            ZilObject zo;

            zo = TestHelpers.Evaluate(ctx, "FOO");
            Assert.AreEqual("FOO", zo.ToStringContext(ctx, false));

            zo = TestHelpers.Evaluate(ctx, "FOO!-BAR!-");
            Assert.AreEqual("FOO!-BAR", zo.ToStringContext(ctx, false));

            // FOO!-BAR!-INITIAL shadows FOO!-BAR!-
            zo = TestHelpers.Evaluate(ctx, "FOO!-BAR!-INITIAL");
            Assert.AreEqual("FOO!-BAR", zo.ToStringContext(ctx, false));

            // now FOO!-BAR!- has a trailer
            zo = TestHelpers.Evaluate(ctx, "FOO!-BAR!-");
            Assert.AreEqual("FOO!-BAR!-", zo.ToStringContext(ctx, false));
        }
    }
}
