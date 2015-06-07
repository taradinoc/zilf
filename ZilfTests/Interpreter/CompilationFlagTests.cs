using Microsoft.VisualStudio.TestTools.UnitTesting;
using Zilf.Interpreter;
using Zilf.Interpreter.Values;
using Zilf.Language;

namespace ZilfTests.Interpreter
{
    [TestClass]
    public class CompilationFlagTests
    {
        [TestMethod]
        public void IN_ZILCH_Flag_Should_Be_Set_By_Default()
        {
            const string CODE1 = @"
<IFFLAG (IN-ZILCH <SETG FOO T>) (T <SETG FOO <>>)>
,FOO";

            var ctx = new Context();
            TestHelpers.EvalAndAssert(ctx, CODE1, ctx.TRUE);

            const string CODE2 = @"
<SETG BAR 123>
<IF-IN-ZILCH <SETG BAR T>>
<IFN-IN-ZILCH <SETG BAR <>>>
,BAR";

            TestHelpers.EvalAndAssert(ctx, CODE2, ctx.TRUE);
        }

        [TestMethod]
        public void IFFLAG_Should_Use_Default_When_Flag_Is_Off()
        {
            const string CODE = @"
<COMPILATION-FLAG MYFLAG <>>
<IFFLAG (MYFLAG <SETG FOO <>>) (T <SETG FOO T>)>
,FOO";

            var ctx = new Context();
            TestHelpers.EvalAndAssert(ctx, CODE, ctx.TRUE);
        }

        [TestMethod]
        public void COMPILATION_FLAG_DEFAULT_Should_Initialize_Value()
        {
            const string CODE = @"
<COMPILATION-FLAG-DEFAULT MYFLAG <>>
<IFFLAG (MYFLAG <SETG FOO <>>) (T <SETG FOO T>)>
,FOO";

            var ctx = new Context();
            TestHelpers.EvalAndAssert(ctx, CODE, ctx.TRUE);
        }

        [TestMethod]
        public void COMPILATION_FLAG_DEFAULT_Should_Not_Reinitialize_Value()
        {
            const string CODE = @"
<COMPILATION-FLAG MYFLAG T>
<COMPILATION-FLAG-DEFAULT MYFLAG <>>
<IFFLAG (MYFLAG <SETG FOO T>) (T <SETG FOO <>>)>
,FOO";

            var ctx = new Context();
            TestHelpers.EvalAndAssert(ctx, CODE, ctx.TRUE);
        }

        [TestMethod]
        public void COMPILATION_FLAG_VALUE_Should_Return_Value()
        {
            const string CODE = @"
<COMPILATION-FLAG MYFLAG 123>
<COMPILATION-FLAG-VALUE MYFLAG>";

            TestHelpers.EvalAndAssert(CODE, new ZilFix(123));
        }

        [TestMethod]
        public void COMPILATION_FLAG_VALUE_Should_Reject_Nonexistent_Flag()
        {
            const string CODE = "<COMPILATION-FLAG-VALUE ASDFGHJKL>";
            TestHelpers.EvalAndCatch<InterpreterError>(CODE);
        }
    }
}
