using Microsoft.VisualStudio.TestTools.UnitTesting;
using Zilf.Interpreter;
using Zilf.Interpreter.Values;

namespace ZilfTests.Interpreter
{
    [TestClass]
    public class DefinitionsTests
    {
        [TestMethod]
        public void Default_Definition_Should_Be_Used_When_Not_Replaced()
        {
            const string CODE = @"
<DEFAULT-DEFINITION FOO-ROUTINE
    <DEFINE FOO () 123>>

<FOO>";

            TestHelpers.EvalAndAssert(CODE, new ZilFix(123));
        }

        [TestMethod]
        public void Replacement_Should_Be_Used_When_Given()
        {
            const string CODE = @"
<REPLACE-DEFINITION FOO-ROUTINE
    <DEFINE FOO () 456>>

<DEFAULT-DEFINITION FOO-ROUTINE
    <DEFINE FOO () 123>>

<FOO>";

            TestHelpers.EvalAndAssert(CODE, new ZilFix(456));
        }

        [TestMethod]
        public void Delayed_Replacement_Should_Be_Evaluated_At_The_Right_Place()
        {
            const string CODE = @"
<DELAY-DEFINITION FOO-ROUTINE>

<SETG FOO-RESULT 789>

<REPLACE-DEFINITION FOO-ROUTINE
    <EVAL <FORM DEFINE FOO '() ,FOO-RESULT>>>

<SETG FOO-RESULT 123>

<DEFAULT-DEFINITION FOO-ROUTINE
    <EVAL <FORM DEFINE FOO '() ,FOO-RESULT>>>

<FOO>";

            TestHelpers.EvalAndAssert(CODE, new ZilFix(789));
        }
    }
}
