using System;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System.Diagnostics.Contracts;

namespace IntegrationTests
{
    [TestClass]
    public class CodeGenTests
    {
        private static RoutineAssertionHelper AssertRoutine(string argSpec, string body)
        {
            Contract.Requires(argSpec != null);
            Contract.Requires(!string.IsNullOrWhiteSpace(body));

            return new RoutineAssertionHelper(argSpec, body);
        }

        [TestMethod]
        public void TestAddToVariable()
        {
            AssertRoutine("\"AUX\" X Y", "<SET X <+ .X .Y>>")
                .GeneratesCodeMatching("ADD X,Y >X\r\n\\s*RETURN X");
        }

        [TestMethod]
        public void TestAddInVoidContextBecomesINC()
        {
            AssertRoutine("\"AUX\" X", "<SET X <+ .X 1>> .X")
                .GeneratesCodeMatching("INC 'X\r\n\\s*RETURN X");
        }

        [TestMethod]
        public void TestAddInValueContextBecomesINC()
        {
            AssertRoutine("\"AUX\" X", "<SET X <+ .X 1>>")
                .GeneratesCodeMatching("INC 'X\r\n\\s*RETURN X");
        }

        [TestMethod]
        public void TestSubtractInVoidContextThenLessBecomesDLESS()
        {
            AssertRoutine("\"AUX\" X", "<SET X <- .X 1>> <COND (<L? .X 0> <PRINTI \"blah\">)>")
                .GeneratesCodeMatching(@"DLESS\? 'X,0");
        }

        [TestMethod]
        public void TestSubtractInValueContextThenLessBecomesDLESS()
        {
            AssertRoutine("\"AUX\" X", "<COND (<L? <SET X <- .X 1>> 0> <PRINTI \"blah\">)>")
                .GeneratesCodeMatching(@"DLESS\? 'X,0");
        }

        [TestMethod]
        public void TestRoutineResultIntoVariable()
        {
            AssertRoutine("\"AUX\" FOO", "<SET FOO <WHATEVER>>")
                .WithGlobal("<ROUTINE WHATEVER () 123>")
                .InV3()
                .GeneratesCodeMatching(@"CALL WHATEVER >FOO");
        }
    }
}
