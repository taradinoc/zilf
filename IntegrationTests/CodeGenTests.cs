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
                .GeneratesCodeMatching(@"ADD X,Y >X\r\n\s*RETURN X");
        }

        [TestMethod]
        public void TestAddInVoidContextBecomesINC()
        {
            AssertRoutine("\"AUX\" X", "<SET X <+ .X 1>> .X")
                .GeneratesCodeMatching(@"INC 'X\r\n\s*RETURN X");
        }

        [TestMethod]
        public void TestAddInValueContextBecomesINC()
        {
            AssertRoutine("\"AUX\" X", "<SET X <+ .X 1>>")
                .GeneratesCodeMatching(@"INC 'X\r\n\s*RETURN X");
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
                .GeneratesCodeMatching("CALL WHATEVER >FOO");
        }

        [TestMethod]
        public void TestPrintiCrlfRtrueBecomesPRINTR()
        {
            AssertRoutine("", "<PRINTI \"hi\"> <CRLF> <RTRUE>")
                .GeneratesCodeMatching("PRINTR \"hi\"");
        }

        [TestMethod]
        public void TestAdjacentEqualsCombine()
        {
            AssertRoutine("\"AUX\" X", "<COND (<OR <=? .X 1> <=? .X 2>> <RTRUE>)>")
                .GeneratesCodeMatching(@"EQUAL\? X,1,2 /TRUE");
            AssertRoutine("\"AUX\" X", "<COND (<OR <EQUAL? .X 1 2> <EQUAL? .X 3 4>> <RTRUE>)>")
                .GeneratesCodeMatching(@"EQUAL\? X,1,2,3 /TRUE");
            AssertRoutine("\"AUX\" X", "<COND (<OR <EQUAL? .X 1 2 3> <=? .X 4> <EQUAL? .X 5 6>> <RTRUE>)>")
                .GeneratesCodeMatching(@"EQUAL\? X,1,2,3 /TRUE\r\n\s*EQUAL\? X,4,5,6 /TRUE");
        }

        [TestMethod]
        public void TestEqualZeroBecomesZERO_P()
        {
            AssertRoutine("\"AUX\" X", "<COND (<=? .X 0> <RTRUE>)>")
                .GeneratesCodeMatching(@"ZERO\? X /TRUE");
            AssertRoutine("\"AUX\" X", "<COND (<=? 0 .X> <RTRUE>)>")
                .GeneratesCodeMatching(@"ZERO\? X /TRUE");
        }

        [TestMethod]
        public void TestAdjacentEqualsCombineEvenIfZero()
        {
            AssertRoutine("\"AUX\" X", "<COND (<OR <=? .X 0> <=? .X 2>> <RTRUE>)>")
                .GeneratesCodeMatching(@"EQUAL\? X,0,2 /TRUE");
            AssertRoutine("\"AUX\" X", "<COND (<OR <=? .X 0> <=? .X 0>> <RTRUE>)>")
                .GeneratesCodeMatching(@"EQUAL\? X,0,0 /TRUE");
            AssertRoutine("\"AUX\" X", "<COND (<OR <EQUAL? .X 1 2> <=? .X 0> <EQUAL? .X 3 4>> <RTRUE>)>")
                .GeneratesCodeMatching(@"EQUAL\? X,1,2,0 /TRUE");
            AssertRoutine("\"AUX\" X", "<COND (<OR <EQUAL? .X 1 2> <EQUAL? .X 3 0>> <RTRUE>)>")
                .GeneratesCodeMatching(@"EQUAL\? X,1,2,3 /TRUE\r\n\s*ZERO\? X /TRUE");
        }

        [TestMethod]
        public void TestValuePredicateContext()
        {
            AssertRoutine("\"AUX\" X Y", "<COND (<NOT <SET X <FIRST? .Y>>> <RTRUE>)>")
                .GeneratesCodeMatching(@"FIRST\? Y >X \\TRUE");
            AssertRoutine("\"AUX\" X Y", "<COND (<NOT .Y> <SET X <>>) (T <SET X <FIRST? .Y>>)> <OR .X <RTRUE>>")
                .GeneratesCodeMatching(@"FIRST\? Y >X (?![/\\]TRUE)");
        }

        [TestMethod]
        public void TestValuePredicateContext_Calls()
        {
            AssertRoutine("\"AUX\" X", "<COND (<NOT <SET X <FOO>>> <RTRUE>)>")
                .WithGlobal("<ROUTINE FOO () <>>")
                .GeneratesCodeMatching(@"CALL FOO >X\r\n\s*ZERO\? X /TRUE");
        }

        [TestMethod]
        public void TestValuePredicateContext_Constants()
        {
            AssertRoutine("\"AUX\" X", "<COND (<NOT <SET X <>>> <RTRUE>)>")
                .GeneratesCodeMatching(@"SET 'X,0\r\n\s*RTRUE");
            AssertRoutine("\"AUX\" X", "<COND (<NOT <SET X 0>> <RTRUE>)>")
                .GeneratesCodeMatching(@"SET 'X,0\r\n\s*RTRUE");
            AssertRoutine("\"AUX\" X", "<COND (<NOT <SET X 100>> <RTRUE>)>")
                .GeneratesCodeMatching(@"SET 'X,100\r\n\s*RFALSE");
            AssertRoutine("\"AUX\" X", "<COND (<NOT <SET X T>> <RTRUE>)>")
                .GeneratesCodeMatching(@"SET 'X,1\r\n\s*RFALSE");
            AssertRoutine("\"AUX\" X", "<COND (<NOT <SET X \"blah\">> <RTRUE>)>")
                .GeneratesCodeMatching(@"SET 'X,STR\?\d+\r\n\s*RFALSE");
        }
    }
}
