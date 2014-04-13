using System;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Zilf;

namespace ZilfTests.Interpreter
{
    [TestClass]
    public class FunctionTests
    {
        [TestMethod]
        public void TestDEFINE()
        {
            var ctx = new Context();

            var expected = ZilAtom.Parse("FOO", ctx);
            TestHelpers.EvalAndAssert(ctx, "<DEFINE FOO (BAR) <> <> <>>", expected);

            var stored = ctx.GetGlobalVal(expected);
            Assert.IsInstanceOfType(stored, typeof(ZilFunction));

            // it's OK to redefine if .REDEFINE is true
            ctx.SetLocalVal(ctx.GetStdAtom(StdAtom.REDEFINE), ctx.TRUE);
            TestHelpers.EvalAndAssert(ctx, "<DEFINE FOO (REDEF1) <>>", expected);

            // ...but it's an error if false
            ctx.SetLocalVal(ctx.GetStdAtom(StdAtom.REDEFINE), null);
            TestHelpers.EvalAndCatch<InterpreterError>(ctx, "<DEFINE FOO (REDEF2) <>>");

            // must have at least 3 arguments
            TestHelpers.EvalAndCatch<InterpreterError>("<DEFINE>");
            TestHelpers.EvalAndCatch<InterpreterError>("<DEFINE FOO>");
            TestHelpers.EvalAndCatch<InterpreterError>("<DEFINE FOO (BAR)>");
        }

        [TestMethod]
        public void TestDEFMAC()
        {
            var ctx = new Context();

            var expected = ZilAtom.Parse("FOO", ctx);
            TestHelpers.EvalAndAssert(ctx, "<DEFMAC FOO (BAR) <> <> <>>", expected);

            var stored = ctx.GetGlobalVal(expected);
            Assert.IsInstanceOfType(stored, typeof(ZilEvalMacro));

            // it's OK to redefine if .REDEFINE is true
            ctx.SetLocalVal(ctx.GetStdAtom(StdAtom.REDEFINE), ctx.TRUE);
            TestHelpers.EvalAndAssert(ctx, "<DEFMAC FOO (REDEF1) <>>", expected);

            // ...but it's an error if false
            ctx.SetLocalVal(ctx.GetStdAtom(StdAtom.REDEFINE), null);
            TestHelpers.EvalAndCatch<InterpreterError>(ctx, "<DEFMAC FOO (REDEF2) <>>");

            // must have at least 3 arguments
            TestHelpers.EvalAndCatch<InterpreterError>("<DEFMAC>");
            TestHelpers.EvalAndCatch<InterpreterError>("<DEFMAC FOO>");
            TestHelpers.EvalAndCatch<InterpreterError>("<DEFMAC FOO (BAR)>");
        }

        [TestMethod]
        public void TestQUOTE()
        {
            TestHelpers.EvalAndAssert("<QUOTE 123>", new ZilFix(123));
            TestHelpers.EvalAndAssert("<QUOTE ()>", new ZilList(null, null));

            var ctx = new Context();
            TestHelpers.EvalAndAssert(ctx, "<QUOTE <+>>",
                new ZilForm(new ZilObject[] { ctx.GetStdAtom(StdAtom.Plus) }));

            // must have 1 argument
            TestHelpers.EvalAndCatch<InterpreterError>("<QUOTE>");
            TestHelpers.EvalAndCatch<InterpreterError>("<QUOTE FOO BAR>");
        }

        [TestMethod]
        public void TestEVAL()
        {
            // most values eval to themselves
            TestHelpers.EvalAndAssert("<EVAL 123>", new ZilFix(123));
            TestHelpers.EvalAndAssert("<EVAL \"hello\">", new ZilString("hello"));

            var ctx = new Context();
            TestHelpers.EvalAndAssert(ctx, "<EVAL +>", ctx.GetStdAtom(StdAtom.Plus));
            TestHelpers.EvalAndAssert(ctx, "<EVAL <>>", ctx.FALSE);

            // lists eval to copies of themselves
            var list = new ZilList(new ZilObject[] { new ZilFix(1), new ZilFix(2), new ZilFix(3) });
            ctx.SetLocalVal(ctx.GetStdAtom(StdAtom.T), list);
            var actual = TestHelpers.Evaluate(ctx, "<EVAL .T>");
            Assert.AreEqual(list, actual);
            Assert.AreNotSame(list, actual);

            // forms execute when evaluated
            var form = new ZilForm(new ZilObject[] { ctx.GetStdAtom(StdAtom.Plus), new ZilFix(1), new ZilFix(2) });
            ctx.SetLocalVal(ctx.GetStdAtom(StdAtom.T), form);
            TestHelpers.EvalAndAssert(ctx, "<EVAL .T>", new ZilFix(3));

            // must have 1 argument
            TestHelpers.EvalAndCatch<InterpreterError>("<EVAL>");
            TestHelpers.EvalAndCatch<InterpreterError>("<EVAL FOO BAR>");
        }

        [TestMethod]
        public void TestEXPAND()
        {
            // most values expand to themselves
            TestHelpers.EvalAndAssert("<EXPAND 123>", new ZilFix(123));
            TestHelpers.EvalAndAssert("<EXPAND \"hello\">", new ZilString("hello"));

            var ctx = new Context();
            TestHelpers.EvalAndAssert(ctx, "<EXPAND +>", ctx.GetStdAtom(StdAtom.Plus));
            TestHelpers.EvalAndAssert(ctx, "<EXPAND <>>", ctx.FALSE);

            // lists expand to themselves, not copies
            var list = new ZilList(new ZilObject[] { new ZilFix(1), new ZilFix(2), new ZilFix(3) });
            ctx.SetLocalVal(ctx.GetStdAtom(StdAtom.T), list);
            var actual = TestHelpers.Evaluate(ctx, "<EXPAND .T>");
            Assert.AreSame(list, actual);

            // forms execute when evaluated
            TestHelpers.Evaluate(ctx, "<DEFMAC FOO () <FORM BAR>>");
            var expected = new ZilForm(new ZilObject[] { ZilAtom.Parse("BAR", ctx) });
            TestHelpers.EvalAndAssert(ctx, "<EXPAND '<FOO>>", expected);

            // must have 1 argument
            TestHelpers.EvalAndCatch<InterpreterError>("<EXPAND>");
            TestHelpers.EvalAndCatch<InterpreterError>("<EXPAND FOO BAR>");
        }

        [TestMethod]
        public void TestAPPLY()
        {
            TestHelpers.EvalAndAssert("<APPLY ,+ 1 2>", new ZilFix(3));
            TestHelpers.EvalAndAssert("<APPLY <FUNCTION () 3>>", new ZilFix(3));
            TestHelpers.EvalAndAssert("<DEFMAC FOO () 3> <APPLY ,FOO>", new ZilFix(3));

            // can't apply non-applicable types
            TestHelpers.EvalAndCatch<InterpreterError>("<APPLY 1>");
            TestHelpers.EvalAndCatch<InterpreterError>("<APPLY +>");
            TestHelpers.EvalAndCatch<InterpreterError>("<APPLY \"hello\">");
            TestHelpers.EvalAndCatch<InterpreterError>("<APPLY (+ 1 2)>");
            TestHelpers.EvalAndCatch<InterpreterError>("<APPLY <>>");

            // must have at least 1 argument
            TestHelpers.EvalAndCatch<InterpreterError>("<APPLY>");
        }
    }
}
