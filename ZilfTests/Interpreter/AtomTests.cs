using System;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Zilf;

namespace ZilfTests.Interpreter
{
    [TestClass]
    public class AtomTests
    {
        [TestMethod]
        public void TestSPNAME()
        {
            TestHelpers.EvalAndAssert("<SPNAME FOO>", new ZilString("FOO"));
            TestHelpers.EvalAndAssert("<SPNAME +>", new ZilString("+"));

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
    }
}
