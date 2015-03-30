using System;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Zilf;

namespace ZilfTests.Interpreter
{
    [TestClass]
    public class ArithmeticTests
    {
        [TestMethod]
        public void TestAddition()
        {
            // no numbers -> 0
            TestHelpers.EvalAndAssert("<+>", new ZilFix(0));

            // one number -> identity
            TestHelpers.EvalAndAssert("<+ 7>", new ZilFix(7));

            // two or more numbers -> sum
            TestHelpers.EvalAndAssert("<+ 1 2>", new ZilFix(3));
            TestHelpers.EvalAndAssert("<+ 1 2 3>", new ZilFix(6));
            TestHelpers.EvalAndAssert("<+ -6 -6 10 1 -2>", new ZilFix(-3));

            // arguments must be numbers
            TestHelpers.EvalAndCatch<InterpreterError>("<+ \"foo\" 1>");
            TestHelpers.EvalAndCatch<InterpreterError>("<+ 0 ATOM>");
            TestHelpers.EvalAndCatch<InterpreterError>("<+ (1 2 3)>");
        }

        [TestMethod]
        public void TestSubtraction()
        {
            // no numbers -> 0
            TestHelpers.EvalAndAssert("<->", new ZilFix(0));

            // one number -> negation
            TestHelpers.EvalAndAssert("<- 7>", new ZilFix(-7));

            // two or more numbers -> difference
            TestHelpers.EvalAndAssert("<- 1 2>", new ZilFix(-1));
            TestHelpers.EvalAndAssert("<- 1 2 3>", new ZilFix(-4));
            TestHelpers.EvalAndAssert("<- -6 -6 10 1 -2>", new ZilFix(-9));

            // arguments must be numbers
            TestHelpers.EvalAndCatch<InterpreterError>("<- \"foo\" 1>");
            TestHelpers.EvalAndCatch<InterpreterError>("<- 0 ATOM>");
            TestHelpers.EvalAndCatch<InterpreterError>("<- (1 2 3)>");
        }

        [TestMethod]
        public void TestMultiplication()
        {
            // no numbers -> 1
            TestHelpers.EvalAndAssert("<*>", new ZilFix(1));

            // one number -> identity
            TestHelpers.EvalAndAssert("<* 7>", new ZilFix(7));
            TestHelpers.EvalAndAssert("<* -7>", new ZilFix(-7));

            // two or more numbers -> product
            TestHelpers.EvalAndAssert("<* 1 2>", new ZilFix(2));
            TestHelpers.EvalAndAssert("<* 1 2 3>", new ZilFix(6));
            TestHelpers.EvalAndAssert("<* -6 -6 10 1 -2>", new ZilFix(-720));

            // arguments must be numbers
            TestHelpers.EvalAndCatch<InterpreterError>("<* \"foo\" 1>");
            TestHelpers.EvalAndCatch<InterpreterError>("<* 0 ATOM>");
            TestHelpers.EvalAndCatch<InterpreterError>("<* (1 2 3)>");
        }

        [TestMethod]
        public void TestDivision()
        {
            // no numbers -> 1
            TestHelpers.EvalAndAssert("</>", new ZilFix(1));

            // one number -> integer reciprocal
            TestHelpers.EvalAndAssert("</ 1>", new ZilFix(1));
            TestHelpers.EvalAndAssert("</ -1>", new ZilFix(-1));
            TestHelpers.EvalAndAssert("</ 2>", new ZilFix(0));
            TestHelpers.EvalAndAssert("</ -2>", new ZilFix(0));
            TestHelpers.EvalAndAssert("</ 7>", new ZilFix(0));

            // two or more numbers -> quotient
            TestHelpers.EvalAndAssert("</ 10 2>", new ZilFix(5));
            TestHelpers.EvalAndAssert("</ 100 2 5>", new ZilFix(10));
            TestHelpers.EvalAndAssert("</ 360 -4 3 15>", new ZilFix(-2));

            // division by zero error
            TestHelpers.EvalAndCatch<InterpreterError>("</ 0>");
            TestHelpers.EvalAndCatch<InterpreterError>("</ 0 0>");
            TestHelpers.EvalAndCatch<InterpreterError>("</ 1 0>");

            // arguments must be numbers
            TestHelpers.EvalAndCatch<InterpreterError>("</ \"foo\" 1>");
            TestHelpers.EvalAndCatch<InterpreterError>("</ 0 ATOM>");
            TestHelpers.EvalAndCatch<InterpreterError>("</ (1 2 3)>");
        }

        [TestMethod]
        public void TestLSH()
        {
            // zero -> no change
            TestHelpers.EvalAndAssert("<LSH 12345 0>", new ZilFix(12345));

            // positive offset -> left shift
            TestHelpers.EvalAndAssert("<LSH 1 1>", new ZilFix(2));
            TestHelpers.EvalAndAssert("<LSH 3 2>", new ZilFix(12));
            TestHelpers.EvalAndAssert("<LSH *20000000000* 5>", new ZilFix(0));

            // negative offset -> right shift
            TestHelpers.EvalAndAssert("<LSH 8 -3>", new ZilFix(1));
            TestHelpers.EvalAndAssert("<LSH 1 -1>", new ZilFix(0));
            TestHelpers.EvalAndAssert("<LSH *37777777777* -32>", new ZilFix(0));

            // must have exactly 2 arguments
            TestHelpers.EvalAndCatch<InterpreterError>("<LSH>");
            TestHelpers.EvalAndCatch<InterpreterError>("<LSH 1>");
            TestHelpers.EvalAndCatch<InterpreterError>("<LSH 1 2 3>");

            // arguments must be numbers
            TestHelpers.EvalAndCatch<InterpreterError>("<LSH \"foo\" 1>");
            TestHelpers.EvalAndCatch<InterpreterError>("<LSH 0 ATOM>");
            TestHelpers.EvalAndCatch<InterpreterError>("<LSH (1 2 3) (4 5 6)>");
        }

        [TestMethod]
        public void TestORB()
        {
            // no numbers -> 0
            TestHelpers.EvalAndAssert("<ORB>", new ZilFix(0));

            // one number -> identity
            TestHelpers.EvalAndAssert("<ORB 0>", new ZilFix(0));
            TestHelpers.EvalAndAssert("<ORB 1>", new ZilFix(1));

            // two or more numbers -> bitwise OR
            TestHelpers.EvalAndAssert("<ORB 0 16>", new ZilFix(16));
            TestHelpers.EvalAndAssert("<ORB 64 96>", new ZilFix(96));
            TestHelpers.EvalAndAssert("<ORB *05777777776* *32107654321*>", new ZilFix(-1));

            // arguments must be numbers
            TestHelpers.EvalAndCatch<InterpreterError>("<ORB \"foo\" 1>");
            TestHelpers.EvalAndCatch<InterpreterError>("<ORB 0 ATOM>");
            TestHelpers.EvalAndCatch<InterpreterError>("<ORB (1 2 3)>");
        }

        [TestMethod]
        public void TestANDB()
        {
            // no numbers -> all bits set
            TestHelpers.EvalAndAssert("<ANDB>", new ZilFix(-1));

            // one number -> identity
            TestHelpers.EvalAndAssert("<ANDB 0>", new ZilFix(0));
            TestHelpers.EvalAndAssert("<ANDB 1>", new ZilFix(1));

            // two or more numbers -> bitwise AND
            TestHelpers.EvalAndAssert("<ANDB 0 16>", new ZilFix(0));
            TestHelpers.EvalAndAssert("<ANDB 64 96>", new ZilFix(64));
            TestHelpers.EvalAndAssert("<ANDB *05777777776* *32107654321*>", new ZilFix(0x011f58d0));

            // arguments must be numbers
            TestHelpers.EvalAndCatch<InterpreterError>("<ANDB \"foo\" 1>");
            TestHelpers.EvalAndCatch<InterpreterError>("<ANDB 0 ATOM>");
            TestHelpers.EvalAndCatch<InterpreterError>("<ANDB (1 2 3)>");
        }

        [TestMethod]
        public void TestComparisons()
        {
            var ctx = new Context();

            TestHelpers.EvalAndAssert(ctx, "<L? 4 5>", ctx.TRUE);
            TestHelpers.EvalAndAssert(ctx, "<L? 4 4>", ctx.FALSE);
            TestHelpers.EvalAndAssert(ctx, "<L? 4 3>", ctx.FALSE);

            TestHelpers.EvalAndAssert(ctx, "<L=? 4 5>", ctx.TRUE);
            TestHelpers.EvalAndAssert(ctx, "<L=? 4 4>", ctx.TRUE);
            TestHelpers.EvalAndAssert(ctx, "<L=? 4 3>", ctx.FALSE);

            TestHelpers.EvalAndAssert(ctx, "<G? 4 5>", ctx.FALSE);
            TestHelpers.EvalAndAssert(ctx, "<G? 4 4>", ctx.FALSE);
            TestHelpers.EvalAndAssert(ctx, "<G? 4 3>", ctx.TRUE);

            TestHelpers.EvalAndAssert(ctx, "<G=? 4 5>", ctx.FALSE);
            TestHelpers.EvalAndAssert(ctx, "<G=? 4 4>", ctx.TRUE);
            TestHelpers.EvalAndAssert(ctx, "<G=? 4 3>", ctx.TRUE);
        }
    }
}
