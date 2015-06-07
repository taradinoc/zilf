using Microsoft.VisualStudio.TestTools.UnitTesting;
using Zilf.Interpreter;
using Zilf.Interpreter.Values;

namespace ZilfTests.Interpreter
{
    [TestClass]
    public class AssocTests
    {
        [TestMethod]
        public void GetProp_Should_Return_Value_Stored_By_PutProp()
        {
            var ctx = new Context();

            TestHelpers.EvalAndAssert(ctx, "<PUTPROP FOO BAR 123>", ZilAtom.Parse("FOO", ctx));
            TestHelpers.EvalAndAssert(ctx, "<GETPROP FOO BAR>", new ZilFix(123));
        }

        [TestMethod]
        public void GetProp_Should_Eval_Arg3_For_Missing_Values()
        {
            var ctx = new Context();

            TestHelpers.EvalAndAssert(ctx, "<GETPROP FOO BAR '<+ 1 2>>", new ZilFix(3));
        }

        [TestMethod]
        public void GetProp_Should_Return_False_For_Missing_Values()
        {
            var ctx = new Context();

            TestHelpers.EvalAndAssert(ctx, "<GETPROP FOO BAR>", ctx.FALSE);
        }

        [TestMethod]
        public void PutProp_Should_Return_Old_Value_When_Clearing()
        {
            var ctx = new Context();

            TestHelpers.EvalAndAssert(ctx, "<PUTPROP FOO BAR 123>", ZilAtom.Parse("FOO", ctx));
            TestHelpers.EvalAndAssert(ctx, "<PUTPROP FOO BAR>", new ZilFix(123));
            TestHelpers.EvalAndAssert(ctx, "<GETPROP FOO BAR>", ctx.FALSE);
        }
    }
}
