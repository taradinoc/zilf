using Microsoft.VisualStudio.TestTools.UnitTesting;
using Zilf.Language;

namespace ZilfTests.Interpreter
{
    [TestClass]
    public class FlowControlTests
    {
        [TestMethod]
        public void COND_Should_Reject_Empty_Clauses()
        {
            TestHelpers.EvalAndCatch<InterpreterError>("<COND ()>");
        }

        [TestMethod]
        public void VERSION_P_Should_Reject_Empty_Clauses()
        {
            TestHelpers.EvalAndCatch<InterpreterError>("<VERSION? ()>");
        }
    }
}
