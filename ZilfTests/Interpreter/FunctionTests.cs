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
        public void TestDEFINE_Segments_Can_Be_Used_With_TUPLE_Parameters()
        {
            var ctx = new Context();

            var foo = ZilAtom.Parse("FOO", ctx);
            TestHelpers.EvalAndAssert(ctx, "<SET L '(1 2 3)> <DEFINE FOO (\"TUPLE\" A) .A>", foo);

            TestHelpers.EvalAndAssert(ctx, "<LIST !<FOO !.L>>",
                new ZilList(new ZilObject[] {
                    new ZilFix(1), new ZilFix(2), new ZilFix(3)
                }));
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

            // lists eval to new lists formed by evaluating each element
            var list = new ZilList(new ZilObject[] {
                new ZilFix(1),
                new ZilForm(new ZilObject[] {
                    ctx.GetStdAtom(StdAtom.Plus),
                    new ZilFix(1),
                    new ZilFix(1),
                }),
                new ZilFix(3),
            });
            var expected = new ZilList(new ZilObject[] {
                new ZilFix(1),
                new ZilFix(2),
                new ZilFix(3),
            });
            ctx.SetLocalVal(ctx.GetStdAtom(StdAtom.T), list);
            var actual = TestHelpers.Evaluate(ctx, "<EVAL .T>");
            Assert.AreEqual(expected, actual);

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
            TestHelpers.EvalAndAssert("<APPLY ,QUOTE 1>", new ZilFix(1));
            TestHelpers.EvalAndAssert("<APPLY <FUNCTION () 3>>", new ZilFix(3));
            TestHelpers.EvalAndAssert("<DEFMAC FOO () 3> <APPLY ,FOO>", new ZilFix(3));
            TestHelpers.EvalAndAssert("<APPLY 2 (100 <+ 199 1> 300)>", new ZilFix(200));

            // can't apply non-applicable types
            TestHelpers.EvalAndCatch<InterpreterError>("<APPLY +>");
            TestHelpers.EvalAndCatch<InterpreterError>("<APPLY \"hello\">");
            TestHelpers.EvalAndCatch<InterpreterError>("<APPLY (+ 1 2)>");
            TestHelpers.EvalAndCatch<InterpreterError>("<APPLY <>>");
            TestHelpers.EvalAndCatch<InterpreterError>("<APPLY '<+ 1 2>>");

            // must have at least 1 argument
            TestHelpers.EvalAndCatch<InterpreterError>("<APPLY>");
        }

        [TestMethod]
        public void TestMAPF()
        {
            TestHelpers.EvalAndAssert("<MAPF <> <FUNCTION (N) <* .N 2>> '(1 2 3)>",
                new ZilFix(6));

            TestHelpers.EvalAndAssert("<MAPF ,VECTOR <FUNCTION (N) <* .N 2>> '(1 2 3)>",
                new ZilVector(new ZilFix(2), new ZilFix(4), new ZilFix(6)));

            TestHelpers.EvalAndAssert("<MAPF ,VECTOR <FUNCTION (N M) <* .N .M>> '(1 10 100 1000) '(2 3 4)>",
                new ZilVector(new ZilFix(2), new ZilFix(30), new ZilFix(400)));
        }

        [TestMethod]
        public void TestMAPR()
        {
            var ctx = new Context();

            var atom = ZilAtom.Parse("FOO", ctx);
            ctx.SetLocalVal(atom, new ZilList(new ZilObject[] { 
                new ZilFix(1), new ZilFix(2), new ZilFix(3)
            }));

            var expectedItems = new ZilObject[] {
                new ZilFix(3), new ZilFix(6), new ZilFix(9),
            };

            TestHelpers.EvalAndAssert(ctx, "<MAPR ,VECTOR <FUNCTION (L) <1 .L <* 3 <1 .L>>>> .FOO>",
                new ZilVector(expectedItems));

            Assert.AreEqual(new ZilList(expectedItems), ctx.GetLocalVal(atom));
        }
    }
}
