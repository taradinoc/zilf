/* Copyright 2010, 2016 Jesse McGrew
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
    public class AtomTests
    {
        [TestMethod]
        public void TestSPNAME()
        {
            TestHelpers.EvalAndAssert("<SPNAME FOO>", ZilString.FromString("FOO"));
            TestHelpers.EvalAndAssert("<SPNAME +>", ZilString.FromString("+"));

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

            // atoms
            var expected = new ZilAtom("FOO", ctx.RootObList, StdAtom.None);
            ctx.RootObList[expected.Text] = expected;

            var actual = TestHelpers.Evaluate(ctx, "<PARSE \"FOO\">");
            Assert.AreSame(expected, actual);

            expected = ctx.GetStdAtom(StdAtom.Plus);
            actual = TestHelpers.Evaluate(ctx, "<PARSE \"+\">");
            Assert.AreSame(expected, actual);

            expected = ctx.PackageObList["+"];
            actual = TestHelpers.Evaluate(ctx, "<PARSE \"+\" 10 <GETPROP PACKAGE OBLIST>>");
            Assert.AreSame(expected, actual);
            Assert.AreNotSame(ctx.GetStdAtom(StdAtom.Plus), actual);

            // other expressions
            TestHelpers.EvalAndAssert(ctx, "<PARSE \"23\">", new ZilFix(23));
            TestHelpers.EvalAndAssert(ctx, "<PARSE \"(1 2 3)\">",
                new ZilList(new[] { new ZilFix(1), new ZilFix(2), new ZilFix(3) }));

            // READ macros
            TestHelpers.EvalAndAssert(ctx, "<PARSE \"%<+ 12 34>\">", new ZilFix(46));
            TestHelpers.EvalAndCatch<InterpreterError>(ctx, "<PARSE \"%<ERROR XYZZY>\">",
                ex => ex.Message.Contains("XYZZY"));

            // string must contain at least one expression
            TestHelpers.EvalAndCatch<InterpreterError>(ctx, "<PARSE \" \">");

            // with multiple expressions, only returns the first
            TestHelpers.EvalAndAssert(ctx, "<PARSE \"1 2 3\">", new ZilFix(1));

            // must have 1-3 arguments
            TestHelpers.EvalAndCatch<InterpreterError>(ctx, "<PARSE>");
            TestHelpers.EvalAndCatch<InterpreterError>(ctx, "<PARSE \"FOO\" <GETPROP PACKAGE OBLIST> 10 \"BAR\">");

            // argument must be a string
            TestHelpers.EvalAndCatch<InterpreterError>(ctx, "<PARSE 5>");
            TestHelpers.EvalAndCatch<InterpreterError>(ctx, "<PARSE (\"FOO\")>");
        }

        [TestMethod]
        public void TestLPARSE()
        {
            var ctx = new Context();

            // only one expression -> empty list
            TestHelpers.EvalAndAssert(ctx, "<LPARSE \" \">", new ZilList(null, null));

            // multiple expressions -> multiple results
            TestHelpers.EvalAndAssert(ctx, "<LPARSE \"1 FOO [3]\">",
                new ZilList(new ZilObject[] {
                    new ZilFix(1),
                    ZilAtom.Parse("FOO", ctx),
                    new ZilVector(new ZilFix(3)),
                }));

            // must have 1-3 arguments
            TestHelpers.EvalAndCatch<InterpreterError>(ctx, "<LPARSE>");
            TestHelpers.EvalAndCatch<InterpreterError>(ctx, "<LPARSE \"FOO\" <GETPROP PACKAGE OBLIST> 10 \"BAR\">");

            // argument must be a string
            TestHelpers.EvalAndCatch<InterpreterError>(ctx, "<LPARSE 5>");
            TestHelpers.EvalAndCatch<InterpreterError>(ctx, "<LPARSE (\"FOO\")>");
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

            // must have 2-3 arguments
            TestHelpers.EvalAndCatch<ArgumentCountError>("<SET>");
            TestHelpers.EvalAndCatch<ArgumentCountError>("<SET FOO>");
            TestHelpers.EvalAndCatch<ArgumentCountError>("<SET FOO BAR BAZ QUUX>");

            // 1st argument must be an atom
            TestHelpers.EvalAndCatch<ArgumentTypeError>("<SET \"FOO\" 5>");

            // 3rd argument must be an ENVIRONMENT
            TestHelpers.EvalAndCatch<ArgumentTypeError>("<SET FOO 123 BAR>");

            TestHelpers.EvalAndAssert(
                @"<DEFINE FOO (""AUX"" (X 123)) <BAR> <* .X 2>>" +
                @"<DEFINE BAR (""BIND"" ENV ""AUX"" (X 456)) <SET X 10 .ENV>>" +
                @"<FOO>",
                new ZilFix(20));
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

            // must have 1-2 arguments
            TestHelpers.EvalAndCatch<ArgumentCountError>("<LVAL>");
            TestHelpers.EvalAndCatch<ArgumentCountError>("<LVAL FOO BAR BAZ>");

            // 1st argument must be an atom
            TestHelpers.EvalAndCatch<InterpreterError>("<LVAL \"FOO\">");

            // 2nd argument must be an ENVIRONMENT
            TestHelpers.EvalAndCatch<ArgumentTypeError>("<LVAL FOO BAR>");

            TestHelpers.EvalAndAssert(
                @"<DEFINE FOO (""AUX"" (X 123)) <BAR>>" +
                @"<DEFINE BAR (""BIND"" ENV ""AUX"" (X 456)) <+ .X <LVAL X .ENV>>>" +
                @"<FOO>",
                new ZilFix(579));
        }

        [TestMethod]
        public void TestVALUE()
        {
            var ctx = new Context();
            var foo = ZilAtom.Parse("FOO", ctx);

            TestHelpers.EvalAndCatch<InterpreterError>(ctx, "<VALUE FOO>");

            ctx.SetGlobalVal(foo, new ZilFix(123));
            TestHelpers.EvalAndAssert(ctx, "<VALUE FOO>", new ZilFix(123));

            ctx.SetLocalVal(foo, new ZilFix(456));
            TestHelpers.EvalAndAssert(ctx, "<VALUE FOO>", new ZilFix(456));

            ctx.SetLocalVal(foo, null);
            TestHelpers.EvalAndAssert(ctx, "<VALUE FOO>", new ZilFix(123));

            ctx.SetGlobalVal(foo, null);
            TestHelpers.EvalAndCatch<InterpreterError>(ctx, "<VALUE FOO>");

            // must have 1-2 arguments
            TestHelpers.EvalAndCatch<ArgumentCountError>(ctx, "<VALUE>");
            TestHelpers.EvalAndCatch<ArgumentCountError>(ctx, "<VALUE FOO BAR BAZ>");

            // 1st argument must be an atom
            TestHelpers.EvalAndCatch<ArgumentTypeError>(ctx, "<VALUE 0>");

            // 2nd argument must be an ENVIRONMENT
            TestHelpers.EvalAndCatch<ArgumentTypeError>(ctx, "<VALUE FOO BAR>");

            TestHelpers.EvalAndAssert(
                @"<DEFINE FOO (""AUX"" (X 123)) <BAR>>" +
                @"<DEFINE BAR (""BIND"" ENV ""AUX"" (X 456)) <+ <VALUE X> <VALUE X .ENV>>>" +
                @"<FOO>",
                new ZilFix(579));
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
        public void TestGBOUND_P()
        {
            var ctx = new Context();

            var whatever = new ZilFix(123);
            ctx.SetGlobalVal(ZilAtom.Parse("MY-TEST-GLOBAL", ctx), whatever);
            ctx.SetLocalVal(ZilAtom.Parse("MY-TEST-LOCAL", ctx), whatever);

            TestHelpers.EvalAndAssert(ctx, "<GBOUND? MY-TEST-GLOBAL>", ctx.TRUE);
            TestHelpers.EvalAndAssert(ctx, "<GBOUND? MY-TEST-LOCAL>", ctx.FALSE);
            TestHelpers.EvalAndAssert(ctx, "<GBOUND? THIS-ATOM-HAS-NO-GVAL-OR-LVAL>", ctx.FALSE);

            TestHelpers.Evaluate(ctx, "<GUNASSIGN MY-TEST-GLOBAL>");
            TestHelpers.EvalAndAssert(ctx, "<GBOUND? MY-TEST-GLOBAL>", ctx.TRUE);

            TestHelpers.Evaluate(ctx, "<GDECL (ANOTHER-TEST-GLOBAL) ANY>");
            TestHelpers.EvalAndAssert(ctx, "<GBOUND? ANOTHER-TEST-GLOBAL>", ctx.TRUE);

            // TODO: test after GLOC

            // must have 1 argument
            TestHelpers.EvalAndCatch<InterpreterError>("<GBOUND?>");
            TestHelpers.EvalAndCatch<InterpreterError>("<GBOUND? FOO BAR>");

            // argument must be an atom
            TestHelpers.EvalAndCatch<InterpreterError>("<GBOUND? \"FOO\">");
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

            // must have 1-2 arguments
            TestHelpers.EvalAndCatch<ArgumentCountError>("<ASSIGNED?>");
            TestHelpers.EvalAndCatch<ArgumentCountError>("<ASSIGNED? FOO BAR BAZ>");

            // 1st argument must be an atom
            TestHelpers.EvalAndCatch<ArgumentTypeError>("<ASSIGNED? \"FOO\">");

            // 2nd argument must be an ENVIRONMENT
            TestHelpers.EvalAndCatch<ArgumentTypeError>("<ASSIGNED? FOO BAR>");

            TestHelpers.EvalAndAssert(ctx,
                @"<DEFINE FOO (""AUX"" (X 123)) <BAR>>" +
                @"<DEFINE BAR (""BIND"" ENV ""AUX"" X) (<ASSIGNED? X> <ASSIGNED? X .ENV>)>" +
                @"<FOO>",
                new ZilList(new[] { ctx.FALSE, ctx.TRUE }));
        }

        [TestMethod]
        public void TestBOUND_P()
        {
            var ctx = new Context();

            var whatever = new ZilFix(123);
            ctx.SetGlobalVal(ZilAtom.Parse("MY-TEST-GLOBAL", ctx), whatever);
            ctx.SetLocalVal(ZilAtom.Parse("MY-TEST-LOCAL", ctx), whatever);

            TestHelpers.EvalAndAssert(ctx, "<BOUND? MY-TEST-GLOBAL>", ctx.FALSE);
            TestHelpers.EvalAndAssert(ctx, "<BOUND? MY-TEST-LOCAL>", ctx.TRUE);
            TestHelpers.EvalAndAssert(ctx, "<BOUND? THIS-ATOM-HAS-NO-GVAL-OR-LVAL>", ctx.FALSE);

            TestHelpers.Evaluate(ctx, "<UNASSIGN MY-TEST-GLOBAL>");
            TestHelpers.EvalAndAssert(ctx, "<BOUND? MY-TEST-GLOBAL>", ctx.TRUE);

            TestHelpers.EvalAndAssert(ctx, "<PROG (FOO) <BOUND? FOO>>", ctx.TRUE);
            TestHelpers.EvalAndAssert(ctx, "<BOUND? FOO>", ctx.FALSE);

            // must have 1-2 arguments
            TestHelpers.EvalAndCatch<ArgumentCountError>("<BOUND?>");
            TestHelpers.EvalAndCatch<ArgumentCountError>("<BOUND? FOO BAR BAZ>");

            // 1st argument must be an atom
            TestHelpers.EvalAndCatch<InterpreterError>("<BOUND? \"FOO\">");

            // 2nd argument must be an ENVIRONMENT
            TestHelpers.EvalAndCatch<ArgumentTypeError>("<BOUND? FOO BAR>");

            TestHelpers.EvalAndAssert(ctx,
                @"<DEFINE FOO (""AUX"" (X 123)) <BAR>>" +
                @"<DEFINE BAR (""BIND"" ENV ""AUX"" (Y 456)) (<BOUND? X> <BOUND? X .ENV> <BOUND? Y> <BOUND? Y .ENV>)>" +
                @"<FOO>",
                new ZilList(new[] { ctx.TRUE, ctx.TRUE, ctx.TRUE, ctx.FALSE }));
        }

        [TestMethod]
        public void TestGUNASSIGN()
        {
            var ctx = new Context();

            var foo = ZilAtom.Parse("FOO", ctx);
            ctx.SetGlobalVal(foo, new ZilFix(123));

            TestHelpers.Evaluate(ctx, "<GUNASSIGN FOO>");
            TestHelpers.EvalAndAssert(ctx, "<GASSIGNED? FOO>", ctx.FALSE);
            TestHelpers.EvalAndCatch<InterpreterError>("<GVAL FOO>");
        }

        [TestMethod]
        public void TestUNASSIGN()
        {
            var ctx = new Context();

            var foo = ZilAtom.Parse("FOO", ctx);
            ctx.SetLocalVal(foo, new ZilFix(123));

            TestHelpers.Evaluate(ctx, "<UNASSIGN FOO>");
            TestHelpers.EvalAndAssert(ctx, "<ASSIGNED? FOO>", ctx.FALSE);
            TestHelpers.EvalAndCatch<InterpreterError>("<LVAL FOO>");

            // must have 1-2 arguments
            TestHelpers.EvalAndCatch<ArgumentCountError>("<UNASSIGN>");
            TestHelpers.EvalAndCatch<ArgumentCountError>("<UNASSIGN FOO BAR BAZ>");

            // 1st argument must be an atom
            TestHelpers.EvalAndCatch<ArgumentTypeError>("<UNASSIGN \"FOO\">");

            // 2nd argument must be an ENVIRONMENT
            TestHelpers.EvalAndCatch<ArgumentTypeError>("<UNASSIGN FOO BAR>");

            TestHelpers.EvalAndAssert(ctx,
               @"<DEFINE FOO (""AUX"" (X 123)) <BAR> <ASSIGNED? X>>" +
               @"<DEFINE BAR (""BIND"" ENV ""AUX"" (X 456)) <UNASSIGN X .ENV>>" +
               @"<FOO>",
               ctx.FALSE);
        }

        [TestMethod]
        public void TestGDECL()
        {
            var ctx = new Context();

            TestHelpers.Evaluate(ctx, "<GDECL (FOO BAR) FIX (BAZ) ANY>");

            TestHelpers.EvalAndAssert(ctx, "<SETG FOO 1>", new ZilFix(1));
            TestHelpers.EvalAndCatch<DeclCheckError>(ctx, "<SETG FOO NOT-A-FIX>");
            TestHelpers.EvalAndAssert(ctx, "<SETG FOO 5>", new ZilFix(5));
            TestHelpers.Evaluate(ctx, "<GUNASSIGN FOO>");

            TestHelpers.EvalAndAssert(ctx, "<GASSIGNED? BAR>", ctx.FALSE);
            TestHelpers.EvalAndCatch<DeclCheckError>(ctx, "<SETG BAR NOT-A-FIX>");

            TestHelpers.EvalAndAssert(ctx, "<SETG BAZ NOT-A-FIX>", ZilAtom.Parse("NOT-A-FIX", ctx));

            // must have an even number of arguments
            TestHelpers.EvalAndCatch<InterpreterError>(ctx, "<GDECL (FOO)>");
            TestHelpers.EvalAndCatch<InterpreterError>(ctx, "<GDECL (FOO) BAR (BAZ)>");

            // odd numbered arguments must be lists of atoms
            TestHelpers.EvalAndCatch<InterpreterError>(ctx, "<GDECL FOO BAR>");
            TestHelpers.EvalAndCatch<InterpreterError>(ctx, "<GDECL [FOO] BAR>");
        }

        [TestMethod]
        public void TestLOOKUP()
        {
            var ctx = new Context();
            var fooAtom = ctx.RootObList["FOO"];
            TestHelpers.EvalAndAssert(ctx, "<LOOKUP \"FOO\" <ROOT>>", fooAtom);
        }

        [TestMethod]
        public void TestINSERT()
        {
            TestHelpers.EvalAndAssert("<SPNAME <INSERT \"FOO\" <ROOT>>>", ZilString.FromString("FOO"));
        }

        [TestMethod]
        public void TestROOT()
        {
            var ctx = new Context();
            TestHelpers.EvalAndAssert(ctx, "<ROOT>", ctx.RootObList);
        }

        [TestMethod]
        public void All_Predefined_Atoms_Should_Be_On_ROOT_ObList()
        {
            var ctx = new Context();

            var offset = 0;

            foreach (var zo in (ZilList)ctx.GetLocalVal(ctx.GetStdAtom(StdAtom.OBLIST)))
            {
                var oblist = zo as ObList;
                if (oblist == null)
                    continue;

                var atomList = (ZilList)oblist.GetPrimitive(ctx);

                if (oblist != ctx.RootObList && !atomList.IsEmpty)
                    Assert.Fail("Expected non-root oblist at offset {0} to be empty, but found: {1}", offset, oblist.ToString());

                offset++;
            }
        }

        [TestMethod]
        public void Test_ObList_Trailers()
        {
            var ctx = new Context();
            ZilObject zo;

            zo = TestHelpers.Evaluate(ctx, "FOO");
            Assert.AreEqual("FOO", zo.ToStringContext(ctx, false));

            zo = TestHelpers.Evaluate(ctx, "FOO!-BAR!-");
            Assert.AreEqual("FOO!-BAR", zo.ToStringContext(ctx, false));

            // FOO!-BAR!-INITIAL shadows FOO!-BAR!-
            zo = TestHelpers.Evaluate(ctx, "FOO!-BAR!-INITIAL");
            Assert.AreEqual("FOO!-BAR", zo.ToStringContext(ctx, false));

            // now FOO!-BAR!- has a trailer
            zo = TestHelpers.Evaluate(ctx, "FOO!-BAR!-");
            Assert.AreEqual("FOO!-BAR!-", zo.ToStringContext(ctx, false));
        }

        [TestMethod]
        public void TestLINK()
        {
            var ctx = new Context();

            TestHelpers.Evaluate(ctx, "<SETG FOO 100>");
            TestHelpers.Evaluate(ctx, "<LINK '<+ 1 ,FOO> \"BAR\" <ROOT>>");

            TestHelpers.EvalAndAssert(ctx, "BAR", new ZilFix(101));
            TestHelpers.EvalAndAssert(ctx, "'BAR",
                new ZilForm(new ZilObject[] {
                    ctx.GetStdAtom(StdAtom.Plus),
                    new ZilFix(1),
                    new ZilForm(new[] { ctx.GetStdAtom(StdAtom.GVAL), ZilAtom.Parse("FOO", ctx) }),
                }));

            // can't replace existing link or atom
            TestHelpers.EvalAndCatch<InterpreterError>(ctx, "<LINK 0 \"BAR\" <ROOT>>");
            TestHelpers.EvalAndCatch<InterpreterError>(ctx, "<LINK 0 \"FOO\">");

            // but we can replace it in a different oblist
            TestHelpers.Evaluate(ctx, "<LINK 0 \"FOO\" <ROOT>>");
            TestHelpers.EvalAndAssert(ctx, "FOO!-", new ZilFix(0));
        }

        [TestMethod]
        public void TestATOM()
        {
            var ctx = new Context();
            var foo1 = TestHelpers.Evaluate(ctx, "FOO");
            var foo2 = TestHelpers.Evaluate(ctx, "<ATOM \"FOO\">");
            var foo3 = TestHelpers.Evaluate(ctx, "<ATOM \"FOO\">");

            Assert.AreNotEqual(foo1, foo2);
            Assert.AreNotEqual(foo1, foo3);
            Assert.AreNotEqual(foo2, foo3);

            Assert.IsNull(((ZilAtom)foo2).ObList);
            Assert.IsNull(((ZilAtom)foo3).ObList);

            Assert.AreEqual("FOO!-#FALSE ()", foo2.ToStringContext(ctx, false));
            Assert.AreEqual("FOO!-#FALSE ()", foo3.ToStringContext(ctx, false));

            TestHelpers.EvalAndCatch<ArgumentCountError>("<ATOM>");
            TestHelpers.EvalAndCatch<ArgumentTypeError>("<ATOM FOO>");
            TestHelpers.EvalAndCatch<ArgumentCountError>("<ATOM FOO BAR>");
        }
    }
}
