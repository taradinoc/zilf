using System;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Zilf;

namespace ZilfTests.Interpreter
{
    [TestClass]
    public class TypeTests
    {
        private Context ctx;

        [TestInitialize]
        public void Initialize()
        {
            ctx = new Context();

            // monad types
            ctx.SetLocalVal(ZilAtom.Parse("A-ATOM", ctx), ZilAtom.Parse("FOO", ctx));
            ctx.SetLocalVal(ZilAtom.Parse("A-CHARACTER", ctx), new ZilChar('C'));
            ctx.SetLocalVal(ZilAtom.Parse("A-FALSE", ctx), ctx.FALSE);
            ctx.SetLocalVal(ZilAtom.Parse("A-FIX", ctx), new ZilFix(123));

            // structured types
            ctx.SetLocalVal(ZilAtom.Parse("A-LIST", ctx), new ZilList(new ZilObject[] {
                new ZilFix(1),
                new ZilFix(2),
                new ZilFix(3),
            }));
            ctx.SetLocalVal(ZilAtom.Parse("A-FORM", ctx), new ZilForm(new ZilObject[] {
                ctx.GetStdAtom(StdAtom.Plus),
                new ZilFix(1),
                new ZilFix(2),
            }));
            ctx.SetLocalVal(ZilAtom.Parse("A-STRING", ctx), new ZilString("hello"));
            ctx.SetLocalVal(ZilAtom.Parse("A-SUBR", ctx), new ZilSubr(Subrs.Plus));
            ctx.SetLocalVal(ZilAtom.Parse("A-FSUBR", ctx), new ZilFSubr(Subrs.QUOTE));
            ctx.SetLocalVal(ZilAtom.Parse("A-FUNCTION", ctx), new ZilFunction(
                ZilAtom.Parse("MYFUNC", ctx),
                new ZilObject[] { },
                new ZilObject[] { new ZilFix(3) }
            ));
            ctx.SetLocalVal(ZilAtom.Parse("A-MACRO", ctx), new ZilEvalMacro(
                new ZilFunction(
                    ZilAtom.Parse("MYMAC", ctx),
                    new ZilObject[] { },
                    new ZilObject[] {
                        new ZilForm(new ZilObject[] {
                            ctx.GetStdAtom(StdAtom.FORM),
                            ctx.GetStdAtom(StdAtom.Plus),
                            new ZilFix(1),
                            new ZilFix(2),
                        }),
                    }
                )
            ));

            // special types
            ctx.SetLocalVal(ZilAtom.Parse("A-SEGMENT", ctx), new ZilSegment(
                new ZilForm(new ZilObject[] {
                    ctx.GetStdAtom(StdAtom.LIST),
                    new ZilFix(1),
                    new ZilFix(2),
                }
            )));
            ctx.SetLocalVal(ZilAtom.Parse("A-WACKY", ctx),
                new ZilHash(ZilAtom.Parse("WACKY", ctx), new ZilList(null, null)));

            // TODO: test other ZilObject descendants: ObList, ZilRoutine, ZilConstant, ZilGlobal, ZilTable, ZilModelObject, OffsetString?
        }

        [TestCleanup]
        public void Cleanup()
        {
            ctx = null;
        }

        // ATOM, CHARACTER, FALSE, FIX
        // LIST, FORM, STRING, SUBR, FSUBR, FUNCTION, MACRO
        // SEGMENT, WACKY

        [TestMethod]
        public void TestTYPE()
        {
            TestHelpers.EvalAndAssert(ctx, "<TYPE .A-ATOM>", ctx.GetStdAtom(StdAtom.ATOM));
            TestHelpers.EvalAndAssert(ctx, "<TYPE .A-CHARACTER>", ctx.GetStdAtom(StdAtom.CHARACTER));
            TestHelpers.EvalAndAssert(ctx, "<TYPE .A-FALSE>", ctx.GetStdAtom(StdAtom.FALSE));
            TestHelpers.EvalAndAssert(ctx, "<TYPE .A-FIX>", ctx.GetStdAtom(StdAtom.FIX));
            TestHelpers.EvalAndAssert(ctx, "<TYPE .A-LIST>", ctx.GetStdAtom(StdAtom.LIST));
            TestHelpers.EvalAndAssert(ctx, "<TYPE .A-FORM>", ctx.GetStdAtom(StdAtom.FORM));
            TestHelpers.EvalAndAssert(ctx, "<TYPE .A-STRING>", ctx.GetStdAtom(StdAtom.STRING));
            TestHelpers.EvalAndAssert(ctx, "<TYPE .A-SUBR>", ctx.GetStdAtom(StdAtom.SUBR));
            TestHelpers.EvalAndAssert(ctx, "<TYPE .A-FSUBR>", ctx.GetStdAtom(StdAtom.FSUBR));
            TestHelpers.EvalAndAssert(ctx, "<TYPE .A-FUNCTION>", ctx.GetStdAtom(StdAtom.FUNCTION));
            TestHelpers.EvalAndAssert(ctx, "<TYPE .A-MACRO>", ctx.GetStdAtom(StdAtom.MACRO));
            TestHelpers.EvalAndAssert(ctx, "<TYPE .A-SEGMENT>", ctx.GetStdAtom(StdAtom.SEGMENT));
            TestHelpers.EvalAndAssert(ctx, "<TYPE .A-WACKY>", ZilAtom.Parse("WACKY", ctx));

            // must have 1 argument
            TestHelpers.EvalAndCatch<InterpreterError>("<TYPE>");
            TestHelpers.EvalAndCatch<InterpreterError>("<TYPE FOO BAR>");
        }

        [TestMethod]
        public void TestTYPE_P()
        {
            TestHelpers.EvalAndAssert(ctx, "<TYPE? .A-ATOM ATOM>", ctx.GetStdAtom(StdAtom.ATOM));
            TestHelpers.EvalAndAssert(ctx, "<TYPE? .A-ATOM STRING ATOM>", ctx.GetStdAtom(StdAtom.ATOM));
            TestHelpers.EvalAndAssert(ctx, "<TYPE? .A-LIST STRING ATOM>", ctx.FALSE);
            TestHelpers.EvalAndAssert(ctx, "<TYPE? .A-LIST LIST STRING ATOM>", ctx.GetStdAtom(StdAtom.LIST));

            // must have at least 2 arguments
            TestHelpers.EvalAndCatch<InterpreterError>("<TYPE?>");
            TestHelpers.EvalAndCatch<InterpreterError>("<TYPE? FOO>");
        }

        [TestMethod]
        public void TestCHTYPE()
        {
            // everything can be coerced to its own type
            string[] types = {
                "ATOM", "CHARACTER", "FALSE", "FIX", "LIST", "FORM", "STRING",
                "SUBR", "FSUBR", "FUNCTION", "MACRO", "SEGMENT", "WACKY",
            };

            foreach (var t in types)
            {
                TestHelpers.EvalAndAssert(
                    ctx,
                    string.Format("<CHTYPE .A-{0} {0}>", t),
                    ctx.GetLocalVal(ZilAtom.Parse("A-" + t, ctx)));
            }

            // specific type coercions are tested in other methods

            // must have 2 arguments
            TestHelpers.EvalAndCatch<InterpreterError>("<CHTYPE>");
            TestHelpers.EvalAndCatch<InterpreterError>("<CHTYPE 5>");
            TestHelpers.EvalAndCatch<InterpreterError>("<CHTYPE 5 FIX FIX>");
        }

        [TestMethod]
        public void TestCHTYPE_to_ATOM()
        {
            Assert.Inconclusive();
        }

        [TestMethod]
        public void TestCHTYPE_to_CHARACTER()
        {
            Assert.Inconclusive();
        }

        [TestMethod]
        public void TestCHTYPE_to_FALSE()
        {
            Assert.Inconclusive();
        }

        [TestMethod]
        public void TestCHTYPE_to_FIX()
        {
            Assert.Inconclusive();
        }

        [TestMethod]
        public void TestCHTYPE_to_LIST()
        {
            Assert.Inconclusive();
        }

        [TestMethod]
        public void TestCHTYPE_to_FORM()
        {
            Assert.Inconclusive();
        }

        [TestMethod]
        public void TestCHTYPE_to_STRING()
        {
            Assert.Inconclusive();
        }

        [TestMethod]
        public void TestCHTYPE_to_SUBR()
        {
            Assert.Inconclusive();
        }

        [TestMethod]
        public void TestCHTYPE_to_FSUBR()
        {
            Assert.Inconclusive();
        }

        [TestMethod]
        public void TestCHTYPE_to_FUNCTION()
        {
            Assert.Inconclusive();
        }

        [TestMethod]
        public void TestCHTYPE_to_MACRO()
        {
            Assert.Inconclusive();
        }

        [TestMethod]
        public void TestCHTYPE_to_SEGMENT()
        {
            Assert.Inconclusive();
        }

        [TestMethod]
        public void TestCHTYPE_to_WACKY()
        {
            Assert.Inconclusive();
        }

        [TestMethod]
        public void TestAPPLICABLE_P()
        {
            TestHelpers.EvalAndAssert(ctx, "<APPLICABLE? .A-FIX>", ctx.TRUE);
            TestHelpers.EvalAndAssert(ctx, "<APPLICABLE? .A-SUBR>", ctx.TRUE);
            TestHelpers.EvalAndAssert(ctx, "<APPLICABLE? .A-FSUBR>", ctx.TRUE);
            TestHelpers.EvalAndAssert(ctx, "<APPLICABLE? .A-FUNCTION>", ctx.TRUE);
            TestHelpers.EvalAndAssert(ctx, "<APPLICABLE? .A-MACRO>", ctx.TRUE);

            TestHelpers.EvalAndAssert(ctx, "<APPLICABLE? .A-ATOM>", ctx.FALSE);
            TestHelpers.EvalAndAssert(ctx, "<APPLICABLE? .A-CHARACTER>", ctx.FALSE);
            TestHelpers.EvalAndAssert(ctx, "<APPLICABLE? .A-FALSE>", ctx.FALSE);
            TestHelpers.EvalAndAssert(ctx, "<APPLICABLE? .A-LIST>", ctx.FALSE);
            TestHelpers.EvalAndAssert(ctx, "<APPLICABLE? .A-FORM>", ctx.FALSE);
            TestHelpers.EvalAndAssert(ctx, "<APPLICABLE? .A-STRING>", ctx.FALSE);
            TestHelpers.EvalAndAssert(ctx, "<APPLICABLE? .A-SEGMENT>", ctx.FALSE);
            TestHelpers.EvalAndAssert(ctx, "<APPLICABLE? .A-WACKY>", ctx.FALSE);

            // must have 1 argument
            TestHelpers.EvalAndCatch<InterpreterError>("<APPLICABLE?>");
            TestHelpers.EvalAndCatch<InterpreterError>("<APPLICABLE? FOO BAR>");
        }

        [TestMethod]
        public void TestSTRUCTURED_P()
        {
            TestHelpers.EvalAndAssert(ctx, "<STRUCTURED? .A-FALSE>", ctx.TRUE);
            TestHelpers.EvalAndAssert(ctx, "<STRUCTURED? .A-FUNCTION>", ctx.TRUE);
            TestHelpers.EvalAndAssert(ctx, "<STRUCTURED? .A-MACRO>", ctx.TRUE);
            TestHelpers.EvalAndAssert(ctx, "<STRUCTURED? .A-LIST>", ctx.TRUE);
            TestHelpers.EvalAndAssert(ctx, "<STRUCTURED? .A-FORM>", ctx.TRUE);
            TestHelpers.EvalAndAssert(ctx, "<STRUCTURED? .A-STRING>", ctx.TRUE);
            TestHelpers.EvalAndAssert(ctx, "<STRUCTURED? .A-SEGMENT>", ctx.TRUE);
            
            TestHelpers.EvalAndAssert(ctx, "<STRUCTURED? .A-FIX>", ctx.FALSE);
            TestHelpers.EvalAndAssert(ctx, "<STRUCTURED? .A-ATOM>", ctx.FALSE);
            TestHelpers.EvalAndAssert(ctx, "<STRUCTURED? .A-CHARACTER>", ctx.FALSE);
            TestHelpers.EvalAndAssert(ctx, "<STRUCTURED? .A-SUBR>", ctx.FALSE);
            TestHelpers.EvalAndAssert(ctx, "<STRUCTURED? .A-FSUBR>", ctx.FALSE);
            TestHelpers.EvalAndAssert(ctx, "<STRUCTURED? .A-WACKY>", ctx.FALSE);

            // must have 1 argument
            TestHelpers.EvalAndCatch<InterpreterError>("<STRUCTURED?>");
            TestHelpers.EvalAndCatch<InterpreterError>("<STRUCTURED? FOO BAR>");
        }

        [TestMethod]
        public void TestFORM()
        {
            TestHelpers.EvalAndAssert(ctx, "<FORM + 1 2>", new ZilForm(
                new ZilObject[] {
                    ctx.GetStdAtom(StdAtom.Plus),
                    new ZilFix(1),
                    new ZilFix(2),
                }
            ));

            // must have at least 1 argument
            TestHelpers.EvalAndCatch<InterpreterError>("<FORM>");
        }

        [TestMethod]
        public void TestLIST()
        {
            TestHelpers.EvalAndAssert("<LIST>", new ZilList(null, null));
            TestHelpers.EvalAndAssert("<LIST 1>", new ZilList(new ZilFix(1), new ZilList(null, null)));
            TestHelpers.EvalAndAssert("<LIST 1 2 3>", new ZilList(
                new ZilObject[] {
                    new ZilFix(1),
                    new ZilFix(2),
                    new ZilFix(3),
                }
            ));
        }

        [TestMethod]
        public void TestCONS()
        {
            TestHelpers.EvalAndAssert(ctx, "<CONS FOO (BAR)>",
                new ZilList(ZilAtom.Parse("FOO", ctx),
                    new ZilList(ZilAtom.Parse("BAR", ctx),
                        new ZilList(null, null))));
            TestHelpers.EvalAndAssert(ctx, "<CONS () ()>",
                new ZilList(new ZilList(null, null),
                    new ZilList(null, null)));

            // second argument can be a form, but the tail of the new list will still become a list
            TestHelpers.EvalAndAssert(ctx, "<CONS FOO '<BAR>>",
                new ZilList(ZilAtom.Parse("FOO", ctx),
                    new ZilList(ZilAtom.Parse("BAR", ctx),
                        new ZilList(null, null))));
            TestHelpers.EvalAndAssert(ctx, "<TYPE <REST <CONS FOO '<BAR>>>>",
                ctx.GetStdAtom(StdAtom.LIST));

            // second argument can't be another type
            TestHelpers.EvalAndCatch<InterpreterError>("<CONS FOO BAR>");
            TestHelpers.EvalAndCatch<InterpreterError>("<CONS () FOO>");

            // must have 2 arguments
            TestHelpers.EvalAndCatch<InterpreterError>("<CONS>");
            TestHelpers.EvalAndCatch<InterpreterError>("<CONS FOO>");
            TestHelpers.EvalAndCatch<InterpreterError>("<CONS FOO () BAR>");
        }

        [TestMethod]
        public void TestFUNCTION()
        {
            TestHelpers.EvalAndAssert("<FUNCTION () 5>", new ZilFunction(
                null,
                new ZilObject[] { },
                new ZilObject[] { new ZilFix(5) }
            ));

            // argument list must be valid
            TestHelpers.EvalAndCatch<InterpreterError>("<FUNCTION 1 2>");
            TestHelpers.EvalAndCatch<InterpreterError>("<FUNCTION (()) 123>");
            TestHelpers.EvalAndCatch<InterpreterError>("<FUNCTION (FOO 9) 123>");
            TestHelpers.EvalAndCatch<InterpreterError>("<FUNCTION ('9) 123>");

            // must have at least 2 arguments
            TestHelpers.EvalAndCatch<InterpreterError>("<FUNCTION>");
            TestHelpers.EvalAndCatch<InterpreterError>("<FUNCTION ()>");
        }

        [TestMethod]
        public void TestSTRING()
        {
            TestHelpers.EvalAndAssert("<STRING>", new ZilString(""));
            TestHelpers.EvalAndAssert("<STRING !\\A !\\B>", new ZilString("AB"));
            TestHelpers.EvalAndAssert("<STRING \"hello\">", new ZilString("hello"));
            TestHelpers.EvalAndAssert("<STRING \"hel\" \"lo\" !\\!>", new ZilString("hello!"));

            // arguments must be characters or strings
            TestHelpers.EvalAndCatch<InterpreterError>("<STRING 123>");
        }

        [TestMethod]
        public void TestASCII()
        {
            TestHelpers.EvalAndAssert("<ASCII !\\A>", new ZilFix(65));

            // argument must be a character
            TestHelpers.EvalAndCatch<InterpreterError>("<ASCII 65>");
            TestHelpers.EvalAndCatch<InterpreterError>("<ASCII \"A\">");

            // must have 1 argument
            TestHelpers.EvalAndCatch<InterpreterError>("<ASCII>");
            TestHelpers.EvalAndCatch<InterpreterError>("<ASCII !\\A !\\B>");
        }
    }
}
