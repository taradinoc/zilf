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
                new ZilHash(ZilAtom.Parse("WACKY", ctx), PrimType.LIST, new ZilList(null, null)));

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
            // nothing can be coerced to ATOM
            TestHelpers.EvalAndCatch<InterpreterError>(ctx, "<CHTYPE .A-CHARACTER ATOM>");
            TestHelpers.EvalAndCatch<InterpreterError>(ctx, "<CHTYPE .A-FALSE ATOM>");
            TestHelpers.EvalAndCatch<InterpreterError>(ctx, "<CHTYPE .A-FIX ATOM>");
            TestHelpers.EvalAndCatch<InterpreterError>(ctx, "<CHTYPE .A-LIST ATOM>");
            TestHelpers.EvalAndCatch<InterpreterError>(ctx, "<CHTYPE .A-FORM ATOM>");
            TestHelpers.EvalAndCatch<InterpreterError>(ctx, "<CHTYPE .A-STRING ATOM>");
            TestHelpers.EvalAndCatch<InterpreterError>(ctx, "<CHTYPE .A-SUBR ATOM>");
            TestHelpers.EvalAndCatch<InterpreterError>(ctx, "<CHTYPE .A-FSUBR ATOM>");
            TestHelpers.EvalAndCatch<InterpreterError>(ctx, "<CHTYPE .A-FUNCTION ATOM>");
            TestHelpers.EvalAndCatch<InterpreterError>(ctx, "<CHTYPE .A-MACRO ATOM>");
            TestHelpers.EvalAndCatch<InterpreterError>(ctx, "<CHTYPE .A-SEGMENT ATOM>");
            TestHelpers.EvalAndCatch<InterpreterError>(ctx, "<CHTYPE .A-WACKY ATOM>");
        }

        [TestMethod]
        public void TestCHTYPE_to_CHARACTER()
        {
            // FIX can be coerced to CHARACTER
            TestHelpers.EvalAndAssert(ctx, "<CHTYPE .A-FIX CHARACTER>",
                new ZilChar((char)123));

            // other types can't
            TestHelpers.EvalAndCatch<InterpreterError>(ctx, "<CHTYPE .A-ATOM CHARACTER>");
            TestHelpers.EvalAndCatch<InterpreterError>(ctx, "<CHTYPE .A-FALSE CHARACTER>");
            TestHelpers.EvalAndCatch<InterpreterError>(ctx, "<CHTYPE .A-LIST CHARACTER>");
            TestHelpers.EvalAndCatch<InterpreterError>(ctx, "<CHTYPE .A-FORM CHARACTER>");
            TestHelpers.EvalAndCatch<InterpreterError>(ctx, "<CHTYPE .A-STRING CHARACTER>");
            TestHelpers.EvalAndCatch<InterpreterError>(ctx, "<CHTYPE .A-SUBR CHARACTER>");
            TestHelpers.EvalAndCatch<InterpreterError>(ctx, "<CHTYPE .A-FSUBR CHARACTER>");
            TestHelpers.EvalAndCatch<InterpreterError>(ctx, "<CHTYPE .A-FUNCTION CHARACTER>");
            TestHelpers.EvalAndCatch<InterpreterError>(ctx, "<CHTYPE .A-MACRO CHARACTER>");
            TestHelpers.EvalAndCatch<InterpreterError>(ctx, "<CHTYPE .A-SEGMENT CHARACTER>");
            TestHelpers.EvalAndCatch<InterpreterError>(ctx, "<CHTYPE .A-WACKY CHARACTER>");
        }

        [TestMethod]
        public void TestCHTYPE_to_FALSE()
        {
            // list-based types can be coerced to FALSE
            TestHelpers.EvalAndAssert(ctx, "<CHTYPE .A-LIST FALSE>",
                new ZilFalse(new ZilList(new ZilObject[] {
                    new ZilFix(1), new ZilFix(2), new ZilFix(3)
                })));
            TestHelpers.EvalAndAssert(ctx, "<CHTYPE .A-FORM FALSE>",
                new ZilFalse(new ZilList(new ZilObject[] {
                    ctx.GetStdAtom(StdAtom.Plus), new ZilFix(1), new ZilFix(2),
                })));
            TestHelpers.EvalAndAssert(ctx, "<CHTYPE .A-FUNCTION FALSE>",
                new ZilFalse(new ZilList(new ZilObject[] {
                    new ZilList(null, null),
                    new ZilFix(3),
                })));
            TestHelpers.EvalAndAssert(ctx, "<CHTYPE .A-MACRO FALSE>",
                new ZilFalse(new ZilList(new ZilFunction(
                    null,
                    new ZilObject[] { },
                    new ZilObject[] {
                        new ZilForm(new ZilObject[] {
                            ctx.GetStdAtom(StdAtom.FORM),
                            ctx.GetStdAtom(StdAtom.Plus),
                            new ZilFix(1),
                            new ZilFix(2),
                        }),
                    }),
                    new ZilList(null, null))));
            TestHelpers.EvalAndAssert(ctx, "<CHTYPE .A-SEGMENT FALSE>",
                new ZilFalse(new ZilList(new ZilObject[] {
                    ctx.GetStdAtom(StdAtom.LIST), new ZilFix(1), new ZilFix(2),
                })));
            TestHelpers.EvalAndAssert(ctx, "<CHTYPE .A-WACKY FALSE>",
                new ZilFalse(new ZilList(null, null)));

            TestHelpers.EvalAndCatch<InterpreterError>(ctx, "<CHTYPE .A-ATOM FALSE>");
            TestHelpers.EvalAndCatch<InterpreterError>(ctx, "<CHTYPE .A-CHARACTER FALSE>");
            TestHelpers.EvalAndCatch<InterpreterError>(ctx, "<CHTYPE .A-FIX FALSE>");
            TestHelpers.EvalAndCatch<InterpreterError>(ctx, "<CHTYPE .A-STRING FALSE>");
            TestHelpers.EvalAndCatch<InterpreterError>(ctx, "<CHTYPE .A-SUBR FALSE>");
            TestHelpers.EvalAndCatch<InterpreterError>(ctx, "<CHTYPE .A-FSUBR FALSE>");
        }

        [TestMethod]
        public void TestCHTYPE_to_FIX()
        {
            // CHARACTER can be coerced to FIX
            TestHelpers.EvalAndAssert(ctx, "<CHTYPE .A-CHARACTER FIX>", new ZilFix(67));

            // other types can't
            TestHelpers.EvalAndCatch<InterpreterError>(ctx, "<CHTYPE .A-ATOM FIX>");
            TestHelpers.EvalAndCatch<InterpreterError>(ctx, "<CHTYPE .A-FALSE FIX>");
            TestHelpers.EvalAndCatch<InterpreterError>(ctx, "<CHTYPE .A-LIST FIX>");
            TestHelpers.EvalAndCatch<InterpreterError>(ctx, "<CHTYPE .A-FORM FIX>");
            TestHelpers.EvalAndCatch<InterpreterError>(ctx, "<CHTYPE .A-STRING FIX>");
            TestHelpers.EvalAndCatch<InterpreterError>(ctx, "<CHTYPE .A-SUBR FIX>");
            TestHelpers.EvalAndCatch<InterpreterError>(ctx, "<CHTYPE .A-FSUBR FIX>");
            TestHelpers.EvalAndCatch<InterpreterError>(ctx, "<CHTYPE .A-FUNCTION FIX>");
            TestHelpers.EvalAndCatch<InterpreterError>(ctx, "<CHTYPE .A-MACRO FIX>");
            TestHelpers.EvalAndCatch<InterpreterError>(ctx, "<CHTYPE .A-SEGMENT FIX>");
            TestHelpers.EvalAndCatch<InterpreterError>(ctx, "<CHTYPE .A-WACKY FIX>");
        }

        [TestMethod]
        public void TestCHTYPE_to_LIST()
        {
            // list-based types can be coerced to LIST
            TestHelpers.EvalAndAssert(ctx, "<CHTYPE .A-FALSE LIST>",
                new ZilList(null, null));
            TestHelpers.EvalAndAssert(ctx, "<CHTYPE .A-FORM LIST>",
                new ZilList(new ZilObject[] {
                    ctx.GetStdAtom(StdAtom.Plus), new ZilFix(1), new ZilFix(2),
                }));
            TestHelpers.EvalAndAssert(ctx, "<CHTYPE .A-FUNCTION LIST>",
                new ZilList(new ZilObject[] {
                    new ZilList(null, null),
                    new ZilFix(3),
                }));
            TestHelpers.EvalAndAssert(ctx, "<CHTYPE .A-MACRO LIST>",
                new ZilList(new ZilFunction(
                    null,
                    new ZilObject[] { },
                    new ZilObject[] {
                        new ZilForm(new ZilObject[] {
                            ctx.GetStdAtom(StdAtom.FORM),
                            ctx.GetStdAtom(StdAtom.Plus),
                            new ZilFix(1),
                            new ZilFix(2),
                        }),
                    }),
                    new ZilList(null, null)));
            TestHelpers.EvalAndAssert(ctx, "<CHTYPE .A-SEGMENT LIST>",
                new ZilList(new ZilObject[] {
                    ctx.GetStdAtom(StdAtom.LIST), new ZilFix(1), new ZilFix(2),
                }));
            TestHelpers.EvalAndAssert(ctx, "<CHTYPE .A-WACKY LIST>",
                new ZilList(null, null));

            TestHelpers.EvalAndCatch<InterpreterError>(ctx, "<CHTYPE .A-ATOM LIST>");
            TestHelpers.EvalAndCatch<InterpreterError>(ctx, "<CHTYPE .A-CHARACTER LIST>");
            TestHelpers.EvalAndCatch<InterpreterError>(ctx, "<CHTYPE .A-FIX LIST>");
            TestHelpers.EvalAndCatch<InterpreterError>(ctx, "<CHTYPE .A-STRING LIST>");
            TestHelpers.EvalAndCatch<InterpreterError>(ctx, "<CHTYPE .A-SUBR LIST>");
            TestHelpers.EvalAndCatch<InterpreterError>(ctx, "<CHTYPE .A-FSUBR LIST>");
        }

        [TestMethod]
        public void TestCHTYPE_to_FORM()
        {
            // list-based types can be coerced to FORM
            TestHelpers.EvalAndAssert(ctx, "<CHTYPE .A-FALSE FORM>",
                new ZilForm(new ZilObject[] {}));
            TestHelpers.EvalAndAssert(ctx, "<CHTYPE .A-LIST FORM>",
                new ZilForm(new ZilObject[] {
                    new ZilFix(1), new ZilFix(2), new ZilFix(3),
                }));
            TestHelpers.EvalAndAssert(ctx, "<CHTYPE .A-FUNCTION FORM>",
                new ZilForm(new ZilObject[] {
                    new ZilList(null, null),
                    new ZilFix(3),
                }));
            TestHelpers.EvalAndAssert(ctx, "<CHTYPE .A-MACRO FORM>",
                new ZilForm(new ZilObject[] { new ZilFunction(
                    null,
                    new ZilObject[] { },
                    new ZilObject[] {
                        new ZilForm(new ZilObject[] {
                            ctx.GetStdAtom(StdAtom.FORM),
                            ctx.GetStdAtom(StdAtom.Plus),
                            new ZilFix(1),
                            new ZilFix(2),
                        }),
                    }),
                }));
            TestHelpers.EvalAndAssert(ctx, "<CHTYPE .A-SEGMENT FORM>",
                new ZilForm(new ZilObject[] {
                    ctx.GetStdAtom(StdAtom.LIST), new ZilFix(1), new ZilFix(2),
                }));
            TestHelpers.EvalAndAssert(ctx, "<CHTYPE .A-WACKY FORM>",
                new ZilForm(new ZilObject[] { }));

            TestHelpers.EvalAndCatch<InterpreterError>(ctx, "<CHTYPE .A-ATOM FORM>");
            TestHelpers.EvalAndCatch<InterpreterError>(ctx, "<CHTYPE .A-CHARACTER FORM>");
            TestHelpers.EvalAndCatch<InterpreterError>(ctx, "<CHTYPE .A-FIX FORM>");
            TestHelpers.EvalAndCatch<InterpreterError>(ctx, "<CHTYPE .A-STRING FORM>");
            TestHelpers.EvalAndCatch<InterpreterError>(ctx, "<CHTYPE .A-SUBR FORM>");
            TestHelpers.EvalAndCatch<InterpreterError>(ctx, "<CHTYPE .A-FSUBR FORM>");
        }

        [TestMethod]
        public void TestCHTYPE_to_STRING()
        {
            // string-based types can be coerced to STRING
            TestHelpers.EvalAndAssert(ctx, "<CHTYPE .A-SUBR STRING>",
                new ZilString("Plus"));
            TestHelpers.EvalAndAssert(ctx, "<CHTYPE .A-FSUBR STRING>",
                new ZilString("QUOTE"));

            TestHelpers.EvalAndCatch<InterpreterError>(ctx, "<CHTYPE .A-ATOM STRING>");
            TestHelpers.EvalAndCatch<InterpreterError>(ctx, "<CHTYPE .A-CHARACTER STRING>");
            TestHelpers.EvalAndCatch<InterpreterError>(ctx, "<CHTYPE .A-FIX STRING>");
            TestHelpers.EvalAndCatch<InterpreterError>(ctx, "<CHTYPE .A-FALSE STRING>");
            TestHelpers.EvalAndCatch<InterpreterError>(ctx, "<CHTYPE .A-LIST STRING>");
            TestHelpers.EvalAndCatch<InterpreterError>(ctx, "<CHTYPE .A-FORM STRING>");
            TestHelpers.EvalAndCatch<InterpreterError>(ctx, "<CHTYPE .A-FUNCTION STRING>");
            TestHelpers.EvalAndCatch<InterpreterError>(ctx, "<CHTYPE .A-MACRO STRING>");
            TestHelpers.EvalAndCatch<InterpreterError>(ctx, "<CHTYPE .A-SEGMENT STRING>");
            TestHelpers.EvalAndCatch<InterpreterError>(ctx, "<CHTYPE .A-WACKY STRING>");
        }

        [TestMethod]
        public void TestCHTYPE_to_SUBR()
        {
            // string-based types can be coerced to SUBR if they name an appropriate Subrs method
            TestHelpers.EvalAndAssert(ctx, "<CHTYPE \"Plus\" SUBR>",
                new ZilSubr(Subrs.Plus));
            TestHelpers.EvalAndAssert(ctx, "<CHTYPE .A-FSUBR SUBR>",
                new ZilSubr(Subrs.QUOTE));

            // arbitrary strings and non-matching methods can't
            TestHelpers.EvalAndCatch<InterpreterError>(ctx, "<CHTYPE \"\" SUBR>");
            TestHelpers.EvalAndCatch<InterpreterError>(ctx, "<CHTYPE \"foobarbaz\" SUBR>");
            TestHelpers.EvalAndCatch<InterpreterError>(ctx, "<CHTYPE \"PerformArithmetic\" SUBR>");

            TestHelpers.EvalAndCatch<InterpreterError>(ctx, "<CHTYPE .A-ATOM SUBR>");
            TestHelpers.EvalAndCatch<InterpreterError>(ctx, "<CHTYPE .A-CHARACTER SUBR>");
            TestHelpers.EvalAndCatch<InterpreterError>(ctx, "<CHTYPE .A-FIX SUBR>");
            TestHelpers.EvalAndCatch<InterpreterError>(ctx, "<CHTYPE .A-FALSE SUBR>");
            TestHelpers.EvalAndCatch<InterpreterError>(ctx, "<CHTYPE .A-LIST SUBR>");
            TestHelpers.EvalAndCatch<InterpreterError>(ctx, "<CHTYPE .A-FORM SUBR>");
            TestHelpers.EvalAndCatch<InterpreterError>(ctx, "<CHTYPE .A-FUNCTION SUBR>");
            TestHelpers.EvalAndCatch<InterpreterError>(ctx, "<CHTYPE .A-MACRO SUBR>");
            TestHelpers.EvalAndCatch<InterpreterError>(ctx, "<CHTYPE .A-SEGMENT SUBR>");
            TestHelpers.EvalAndCatch<InterpreterError>(ctx, "<CHTYPE .A-WACKY SUBR>");
        }

        [TestMethod]
        public void TestCHTYPE_to_FSUBR()
        {
            // string-based types can be coerced to FSUBR if they name an appropriate Subrs method
            TestHelpers.EvalAndAssert(ctx, "<CHTYPE \"DEFINE\" FSUBR>",
                new ZilFSubr(Subrs.DEFINE));
            TestHelpers.EvalAndAssert(ctx, "<CHTYPE .A-SUBR FSUBR>",
                new ZilFSubr(Subrs.Plus));

            // arbitrary strings and non-matching methods can't
            TestHelpers.EvalAndCatch<InterpreterError>(ctx, "<CHTYPE \"\" FSUBR>");
            TestHelpers.EvalAndCatch<InterpreterError>(ctx, "<CHTYPE \"foobarbaz\" FSUBR>");
            TestHelpers.EvalAndCatch<InterpreterError>(ctx, "<CHTYPE \"PerformArithmetic\" FSUBR>");

            TestHelpers.EvalAndCatch<InterpreterError>(ctx, "<CHTYPE .A-ATOM FSUBR>");
            TestHelpers.EvalAndCatch<InterpreterError>(ctx, "<CHTYPE .A-CHARACTER FSUBR>");
            TestHelpers.EvalAndCatch<InterpreterError>(ctx, "<CHTYPE .A-FIX FSUBR>");
            TestHelpers.EvalAndCatch<InterpreterError>(ctx, "<CHTYPE .A-FALSE FSUBR>");
            TestHelpers.EvalAndCatch<InterpreterError>(ctx, "<CHTYPE .A-LIST FSUBR>");
            TestHelpers.EvalAndCatch<InterpreterError>(ctx, "<CHTYPE .A-FORM FSUBR>");
            TestHelpers.EvalAndCatch<InterpreterError>(ctx, "<CHTYPE .A-FUNCTION FSUBR>");
            TestHelpers.EvalAndCatch<InterpreterError>(ctx, "<CHTYPE .A-MACRO FSUBR>");
            TestHelpers.EvalAndCatch<InterpreterError>(ctx, "<CHTYPE .A-SEGMENT FSUBR>");
            TestHelpers.EvalAndCatch<InterpreterError>(ctx, "<CHTYPE .A-WACKY FSUBR>");
        }

        [TestMethod]
        public void TestCHTYPE_to_FUNCTION()
        {
            // list-based types can be coerced to FUNCTION if they fit the pattern ((argspec) body)
            TestHelpers.EvalAndAssert(ctx, "<CHTYPE ((X) '<TYPE X>) FUNCTION>",
                new ZilFunction(
                    null,
                    new ZilObject[] { ZilAtom.Parse("X", ctx) },
                    new ZilObject[] {
                        new ZilForm(new ZilObject[] {
                            ZilAtom.Parse("TYPE", ctx),
                            ZilAtom.Parse("X", ctx),
                        }),
                    }
                ));

            // arbitrary lists and other values can't
            TestHelpers.EvalAndCatch<InterpreterError>(ctx, "<CHTYPE .A-ATOM FUNCTION>");
            TestHelpers.EvalAndCatch<InterpreterError>(ctx, "<CHTYPE .A-CHARACTER FUNCTION>");
            TestHelpers.EvalAndCatch<InterpreterError>(ctx, "<CHTYPE .A-FIX FUNCTION>");
            TestHelpers.EvalAndCatch<InterpreterError>(ctx, "<CHTYPE .A-FALSE FUNCTION>");
            TestHelpers.EvalAndCatch<InterpreterError>(ctx, "<CHTYPE .A-LIST FUNCTION>");
            TestHelpers.EvalAndCatch<InterpreterError>(ctx, "<CHTYPE .A-FORM FUNCTION>");
            TestHelpers.EvalAndCatch<InterpreterError>(ctx, "<CHTYPE .A-STRING FUNCTION>");
            TestHelpers.EvalAndCatch<InterpreterError>(ctx, "<CHTYPE .A-SUBR FUNCTION>");
            TestHelpers.EvalAndCatch<InterpreterError>(ctx, "<CHTYPE .A-FSUBR FUNCTION>");
            TestHelpers.EvalAndCatch<InterpreterError>(ctx, "<CHTYPE .A-MACRO FUNCTION>");
            TestHelpers.EvalAndCatch<InterpreterError>(ctx, "<CHTYPE .A-SEGMENT FUNCTION>");
            TestHelpers.EvalAndCatch<InterpreterError>(ctx, "<CHTYPE .A-WACKY FUNCTION>");
        }

        [TestMethod]
        public void TestCHTYPE_to_MACRO()
        {
            // list-based types can be coerced to MACRO if they fit the pattern (applicable)
            TestHelpers.EvalAndAssert(ctx, "<CHTYPE (.A-FUNCTION) MACRO>",
                new ZilEvalMacro(new ZilFunction(
                    ZilAtom.Parse("MYFUNC", ctx),
                    new ZilObject[] { },
                    new ZilObject[] { new ZilFix(3) })));
            TestHelpers.EvalAndAssert(ctx, "<CHTYPE '<#SUBR \"Plus\"> MACRO>",
                new ZilEvalMacro(new ZilSubr(Subrs.Plus)));

            // arbitrary lists and other values can't
            TestHelpers.EvalAndCatch<InterpreterError>(ctx, "<CHTYPE .A-ATOM MACRO>");
            TestHelpers.EvalAndCatch<InterpreterError>(ctx, "<CHTYPE .A-CHARACTER MACRO>");
            TestHelpers.EvalAndCatch<InterpreterError>(ctx, "<CHTYPE .A-FIX MACRO>");
            TestHelpers.EvalAndCatch<InterpreterError>(ctx, "<CHTYPE .A-FALSE MACRO>");
            TestHelpers.EvalAndCatch<InterpreterError>(ctx, "<CHTYPE .A-LIST MACRO>");
            TestHelpers.EvalAndCatch<InterpreterError>(ctx, "<CHTYPE .A-FORM MACRO>");
            TestHelpers.EvalAndCatch<InterpreterError>(ctx, "<CHTYPE .A-STRING MACRO>");
            TestHelpers.EvalAndCatch<InterpreterError>(ctx, "<CHTYPE .A-SUBR MACRO>");
            TestHelpers.EvalAndCatch<InterpreterError>(ctx, "<CHTYPE .A-FSUBR MACRO>");
            TestHelpers.EvalAndCatch<InterpreterError>(ctx, "<CHTYPE .A-FUNCTION MACRO>");
            TestHelpers.EvalAndCatch<InterpreterError>(ctx, "<CHTYPE .A-SEGMENT MACRO>");
            TestHelpers.EvalAndCatch<InterpreterError>(ctx, "<CHTYPE .A-WACKY MACRO>");
        }

        [TestMethod]
        public void TestCHTYPE_to_SEGMENT()
        {
            // list-based types can be coerced to SEGMENT
            TestHelpers.EvalAndAssert(ctx, "<CHTYPE .A-FALSE SEGMENT>",
                new ZilSegment(new ZilForm(new ZilObject[] { })));
            TestHelpers.EvalAndAssert(ctx, "<CHTYPE .A-LIST SEGMENT>",
                new ZilSegment(new ZilForm(new ZilObject[] {
                    new ZilFix(1), new ZilFix(2), new ZilFix(3),
                })));
            TestHelpers.EvalAndAssert(ctx, "<CHTYPE .A-FUNCTION SEGMENT>",
                new ZilSegment(new ZilForm(new ZilObject[] {
                    new ZilList(null, null),
                    new ZilFix(3),
                })));
            TestHelpers.EvalAndAssert(ctx, "<CHTYPE .A-MACRO SEGMENT>",
                new ZilSegment(new ZilForm(new ZilObject[] { new ZilFunction(
                    null,
                    new ZilObject[] { },
                    new ZilObject[] {
                        new ZilForm(new ZilObject[] {
                            ctx.GetStdAtom(StdAtom.FORM),
                            ctx.GetStdAtom(StdAtom.Plus),
                            new ZilFix(1),
                            new ZilFix(2),
                        }),
                    }),
                })));
            TestHelpers.EvalAndAssert(ctx, "<CHTYPE .A-FORM SEGMENT>",
                new ZilSegment(new ZilForm(new ZilObject[] {
                    ctx.GetStdAtom(StdAtom.Plus), new ZilFix(1), new ZilFix(2),
                })));
            TestHelpers.EvalAndAssert(ctx, "<CHTYPE .A-WACKY SEGMENT>",
                new ZilSegment(new ZilForm(new ZilObject[] { })));

            TestHelpers.EvalAndCatch<InterpreterError>(ctx, "<CHTYPE .A-ATOM SEGMENT>");
            TestHelpers.EvalAndCatch<InterpreterError>(ctx, "<CHTYPE .A-CHARACTER SEGMENT>");
            TestHelpers.EvalAndCatch<InterpreterError>(ctx, "<CHTYPE .A-FIX SEGMENT>");
            TestHelpers.EvalAndCatch<InterpreterError>(ctx, "<CHTYPE .A-STRING SEGMENT>");
            TestHelpers.EvalAndCatch<InterpreterError>(ctx, "<CHTYPE .A-SUBR SEGMENT>");
            TestHelpers.EvalAndCatch<InterpreterError>(ctx, "<CHTYPE .A-FSUBR SEGMENT>");
        }

        [TestMethod]
        public void TestCHTYPE_to_WACKY()
        {
            // since WACKY isn't a registered type, we can't change anything to it
            TestHelpers.EvalAndCatch<InterpreterError>(ctx, "<CHTYPE .A-ATOM WACKY>");
            TestHelpers.EvalAndCatch<InterpreterError>(ctx, "<CHTYPE .A-CHARACTER WACKY>");
            TestHelpers.EvalAndCatch<InterpreterError>(ctx, "<CHTYPE .A-FIX WACKY>");
            TestHelpers.EvalAndCatch<InterpreterError>(ctx, "<CHTYPE .A-FALSE WACKY>");
            TestHelpers.EvalAndCatch<InterpreterError>(ctx, "<CHTYPE .A-LIST WACKY>");
            TestHelpers.EvalAndCatch<InterpreterError>(ctx, "<CHTYPE .A-FORM WACKY>");
            TestHelpers.EvalAndCatch<InterpreterError>(ctx, "<CHTYPE .A-STRING WACKY>");
            TestHelpers.EvalAndCatch<InterpreterError>(ctx, "<CHTYPE .A-SUBR WACKY>");
            TestHelpers.EvalAndCatch<InterpreterError>(ctx, "<CHTYPE .A-FSUBR WACKY>");
            TestHelpers.EvalAndCatch<InterpreterError>(ctx, "<CHTYPE .A-FUNCTION WACKY>");
            TestHelpers.EvalAndCatch<InterpreterError>(ctx, "<CHTYPE .A-MACRO WACKY>");
            TestHelpers.EvalAndCatch<InterpreterError>(ctx, "<CHTYPE .A-SEGMENT WACKY>");
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

        [TestMethod]
        public void TestCustomType_BYTE()
        {
            TestHelpers.EvalAndAssert(ctx, "<TYPE #BYTE 255>", ctx.GetStdAtom(StdAtom.BYTE));
            TestHelpers.EvalAndAssert(ctx, "<CHTYPE #BYTE 255 FIX>", new ZilFix(255));

            TestHelpers.EvalAndCatch<InterpreterError>("#BYTE \"f\"");

            TestHelpers.EvalAndAssert(ctx, "<STRUCTURED? #BYTE 0>", ctx.FALSE);
            TestHelpers.EvalAndAssert(ctx, "<APPLICABLE? #BYTE 0>", ctx.FALSE);
        }

        [TestMethod]
        public void TestCustomType_DECL()
        {
            TestHelpers.EvalAndAssert(ctx, "<TYPE #DECL ((FOO) FIX)>", ZilAtom.Parse("DECL", ctx));
            TestHelpers.EvalAndAssert(ctx, "<CHTYPE #DECL ((FOO) FIX) LIST>",
                new ZilList(new ZilObject[] {
                    new ZilList(ZilAtom.Parse("FOO", ctx), new ZilList(null,null)),
                    ctx.GetStdAtom( StdAtom.FIX)
                }));

            TestHelpers.EvalAndCatch<InterpreterError>("#DECL BLAH");

            TestHelpers.EvalAndAssert(ctx, "<STRUCTURED? #DECL ((FOO) FIX)>", ctx.TRUE);
            TestHelpers.EvalAndAssert(ctx, "<APPLICABLE? #DECL ((FOO) FIX)>", ctx.FALSE);
        }

        [TestMethod]
        public void TestApplicableFIX()
        {
            var ctx = new Context();
            TestHelpers.EvalAndAssert(ctx, "<SET O <LIST <FORM + 1 2>>> <1 .O>",
                new ZilForm(new ZilObject[] { ctx.GetStdAtom(StdAtom.Plus), new ZilFix(1), new ZilFix(2) }));
        }
    }
}
