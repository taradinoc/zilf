/* Copyright 2010-2016 Jesse McGrew
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
        public void TestDEFINE_With_Activation()
        {
            var ctx = new Context();

            // DEFINE with activation-atom syntax
            TestHelpers.Evaluate(ctx, @"
<DEFINE FOO FOO-ACT ()
    <PROG () <RETURN 123 .FOO-ACT>>
    456>");
            TestHelpers.EvalAndAssert(ctx, "<FOO>", new ZilFix(123));

            // DEFINE with "NAME" argument
            ctx = new Context();

            TestHelpers.Evaluate(ctx, @"
<DEFINE FOO (""NAME"" FOO-ACT)
    <PROG () <RETURN 123 .FOO-ACT>>
    456>");
            TestHelpers.EvalAndAssert(ctx, "<FOO>", new ZilFix(123));

            // FUNCTION with activation-atom syntax
            ctx = new Context();

            TestHelpers.Evaluate(ctx, @"
<SETG FOO
    <FUNCTION FOO-ACT ()
        <PROG () <RETURN 123 .FOO-ACT>>
        456>>");
            TestHelpers.EvalAndAssert(ctx, "<FOO>", new ZilFix(123));

            // FUNCTION with "NAME" argument
            ctx = new Context();

            TestHelpers.Evaluate(ctx, @"
<SETG FOO
    <FUNCTION (""NAME"" FOO-ACT)
        <PROG () <RETURN 123 .FOO-ACT>>
        456>>");
            TestHelpers.EvalAndAssert(ctx, "<FOO>", new ZilFix(123));

            // #FUNCTION with activation-atom syntax
            ctx = new Context();

            TestHelpers.Evaluate(ctx, @"
<SETG FOO
    #FUNCTION (FOO-ACT ()
        <PROG () <RETURN 123 .FOO-ACT>>
        456)>");
            TestHelpers.EvalAndAssert(ctx, "<FOO>", new ZilFix(123));

            // #FUNCTION with "NAME" argument
            ctx = new Context();

            TestHelpers.Evaluate(ctx, @"
<SETG FOO
    #FUNCTION ((""NAME"" FOO-ACT)
        <PROG () <RETURN 123 .FOO-ACT>>
        456)>");
            TestHelpers.EvalAndAssert(ctx, "<FOO>", new ZilFix(123));
        }

        [TestMethod]
        public void DEFINE_Requires_A_Body()
        {
            TestHelpers.EvalAndCatch<InterpreterError>("<DEFINE FOO ()>");
            TestHelpers.EvalAndCatch<InterpreterError>("<DEFINE FOO A ()>");
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
        public void TestDEFMAC_With_Activation()
        {
            var ctx = new Context();

            // DEFMAC with activation-atom syntax
            TestHelpers.Evaluate(ctx, @"
<DEFMAC FOO FOO-ACT ()
    <PROG () <RETURN 123 .FOO-ACT>>
    456>");
            TestHelpers.EvalAndAssert(ctx, "<FOO>", new ZilFix(123));

            // DEFMAC with "NAME" argument
            ctx = new Context();

            TestHelpers.Evaluate(ctx, @"
<DEFMAC FOO (""NAME"" FOO-ACT)
    <PROG () <RETURN 123 .FOO-ACT>>
    456>");
            TestHelpers.EvalAndAssert(ctx, "<FOO>", new ZilFix(123));

            // FUNCTION with activation-atom syntax
            ctx = new Context();

            TestHelpers.Evaluate(ctx, @"
<SETG FOO
    <FUNCTION FOO-ACT ()
        <PROG () <RETURN 123 .FOO-ACT>>
        456>>");
            TestHelpers.EvalAndAssert(ctx, "<FOO>", new ZilFix(123));

            // FUNCTION with "NAME" argument
            ctx = new Context();

            TestHelpers.Evaluate(ctx, @"
<SETG FOO
    <FUNCTION (""NAME"" FOO-ACT)
        <PROG () <RETURN 123 .FOO-ACT>>
        456>>");
            TestHelpers.EvalAndAssert(ctx, "<FOO>", new ZilFix(123));

            // #FUNCTION with activation-atom syntax
            ctx = new Context();

            TestHelpers.Evaluate(ctx, @"
<SETG FOO
    #FUNCTION (FOO-ACT ()
        <PROG () <RETURN 123 .FOO-ACT>>
        456)>");
            TestHelpers.EvalAndAssert(ctx, "<FOO>", new ZilFix(123));

            // #FUNCTION with "NAME" argument
            ctx = new Context();

            TestHelpers.Evaluate(ctx, @"
<SETG FOO
    #FUNCTION ((""NAME"" FOO-ACT)
        <PROG () <RETURN 123 .FOO-ACT>>
        456)>");
            TestHelpers.EvalAndAssert(ctx, "<FOO>", new ZilFix(123));
        }

        [TestMethod]
        public void DEFMAC_Requires_A_Body()
        {
            TestHelpers.EvalAndCatch<InterpreterError>("<DEFMAC FOO ()>");
            TestHelpers.EvalAndCatch<InterpreterError>("<DEFMAC FOO A ()>");
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
            TestHelpers.EvalAndAssert("<EVAL \"hello\">", ZilString.FromString("hello"));

            var ctx = new Context();
            TestHelpers.EvalAndAssert(ctx, "<EVAL +>", ctx.GetStdAtom(StdAtom.Plus));
            TestHelpers.EvalAndAssert(ctx, "<EVAL <>>", ctx.FALSE);

            // lists eval to new lists formed by evaluating each element
            var list = new ZilList(new ZilObject[] {
                new ZilFix(1),
                new ZilForm(new ZilObject[] {
                    ctx.GetStdAtom(StdAtom.Plus),
                    new ZilFix(1),
                    new ZilFix(1)
                }),
                new ZilFix(3)
            });
            var expected = new ZilList(new ZilObject[] {
                new ZilFix(1),
                new ZilFix(2),
                new ZilFix(3)
            });
            ctx.SetLocalVal(ctx.GetStdAtom(StdAtom.T), list);
            var actual = TestHelpers.Evaluate(ctx, "<EVAL .T>");
            Assert.AreEqual(expected, actual);

            // forms execute when evaluated
            var form = new ZilForm(new ZilObject[] { ctx.GetStdAtom(StdAtom.Plus), new ZilFix(1), new ZilFix(2) });
            ctx.SetLocalVal(ctx.GetStdAtom(StdAtom.T), form);
            TestHelpers.EvalAndAssert(ctx, "<EVAL .T>", new ZilFix(3));

            // must have 1-2 arguments
            TestHelpers.EvalAndCatch<ArgumentCountError>("<EVAL>");
            TestHelpers.EvalAndCatch<ArgumentCountError>("<EVAL FOO BAR BAZ>");

            // 2nd argument must be an ENVIRONMENT
            TestHelpers.EvalAndCatch<ArgumentTypeError>("<EVAL FOO BAR>");

            TestHelpers.Evaluate(ctx, "<SET A 0>");
            TestHelpers.Evaluate(ctx, "<DEFINE RIGHT (\"BIND\" E 'B \"AUX\" (A 1)) <EVAL .B .E>>");
            TestHelpers.EvalAndAssert(ctx, "<RIGHT .A>", new ZilFix(0));
        }

        [TestMethod]
        public void TestEXPAND()
        {
            // most values expand to themselves
            TestHelpers.EvalAndAssert("<EXPAND 123>", new ZilFix(123));
            TestHelpers.EvalAndAssert("<EXPAND \"hello\">", ZilString.FromString("hello"));

            var ctx = new Context();
            TestHelpers.EvalAndAssert(ctx, "<EXPAND +>", ctx.GetStdAtom(StdAtom.Plus));
            TestHelpers.EvalAndAssert(ctx, "<EXPAND <>>", ctx.FALSE);

            // lists expand to copies of themselves
            var list = new ZilList(new ZilObject[] { new ZilFix(1), new ZilFix(2), new ZilFix(3) });
            ctx.SetLocalVal(ctx.GetStdAtom(StdAtom.T), list);
            var actual = TestHelpers.Evaluate(ctx, "<EXPAND .T>");
            Assert.AreEqual(list, actual);
            Assert.AreNotSame(list, actual);

            // forms execute when evaluated
            TestHelpers.Evaluate(ctx, "<DEFMAC FOO () <FORM BAR>>");
            var expected = new ZilForm(new ZilObject[] { ZilAtom.Parse("BAR", ctx) });
            TestHelpers.EvalAndAssert(ctx, "<EXPAND '<FOO>>", expected);
            TestHelpers.EvalAndAssert(ctx, "<EXPAND <FORM ,FOO>>", expected);

            // if the form doesn't contain a macro, it still executes
            TestHelpers.Evaluate(ctx, "<DEFINE BAR () 123>");
            TestHelpers.EvalAndAssert(ctx, "<EXPAND '<BAR>>", new ZilFix(123));
            TestHelpers.EvalAndAssert(ctx, "<EXPAND <FORM ,BAR>>", new ZilFix(123));

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
                new ZilFix(3), new ZilFix(6), new ZilFix(9)
            };

            TestHelpers.EvalAndAssert(ctx, "<MAPR ,VECTOR <FUNCTION (L) <1 .L <* 3 <1 .L>>> FOO> .FOO>",
                new ZilVector(atom, atom, atom));

            Assert.AreEqual(new ZilList(expectedItems), ctx.GetLocalVal(atom));
        }

        [TestMethod]
        public void SEGMENT_In_FUNCTION_Call_Should_Be_Expanded_When_In_Calling_FORM()
        {
            var ctx = new Context();
            TestHelpers.Evaluate(ctx, "<DEFINE FOO (A B C) <+ .A .B .C>>");
            TestHelpers.Evaluate(ctx, "<SET L '(100 50)>");
            TestHelpers.EvalAndAssert(ctx, "<FOO 5 !.L>", new ZilFix(155));

            // it should be expanded even when the arg is quoted
            ctx = new Context();
            TestHelpers.Evaluate(ctx, "<DEFINE FOO ('A 'B 'C) <+ .A .B .C>>");
            TestHelpers.Evaluate(ctx, "<SET L '(100 50)>");
            TestHelpers.EvalAndAssert(ctx, "<FOO 5 !.L>", new ZilFix(155));

            // or with "ARGS"
            ctx = new Context();
            TestHelpers.Evaluate(ctx, "<DEFINE FOO (\"ARGS\" A) <+ !.A>>");
            TestHelpers.Evaluate(ctx, "<SET L '(100 50)>");
            TestHelpers.EvalAndAssert(ctx, "<FOO 5 !.L>", new ZilFix(155));

            // or with "TUPLE"
            ctx = new Context();
            TestHelpers.Evaluate(ctx, "<DEFINE FOO (\"TUPLE\" A) <+ !.A>>");
            TestHelpers.Evaluate(ctx, "<SET L '(100 50)>");
            TestHelpers.EvalAndAssert(ctx, "<FOO 5 !.L>", new ZilFix(155));
        }

        [TestMethod]
        public void SEGMENT_In_FUNCTION_Call_Should_Not_Be_Expanded_When_Not_In_Calling_FORM()
        {
            // shouldn't expand a second segment returned by expanding a first segment
            var ctx = new Context();
            TestHelpers.Evaluate(ctx, "<DEFINE FOO (\"TUPLE\" A) <1 .A>>");
            TestHelpers.Evaluate(ctx, "<SET L '(100 50)>");
            TestHelpers.Evaluate(ctx, "<SET X '(!.L)>");
            TestHelpers.EvalAndAssert(ctx, "<FOO !.X>",
                new ZilSegment(
                    new ZilForm(new[] {
                        ctx.GetStdAtom(StdAtom.LVAL), ZilAtom.Parse("L", ctx)
                    })
                ));

            // shouldn't expand a segment passed to a MAPF function from the input list
            ctx = new Context();
            TestHelpers.EvalAndAssert(ctx, "<MAPF ,STRING <FUNCTION (I) <UNPARSE .I>> '(!<FOO>)>",
                ZilString.FromString("!<FOO>"));
        }

        [TestMethod]
        public void FUNCTION_Parameter_Atoms_Should_Not_Be_Bound_While_Arguments_Are_Being_Evaluated()
        {
            var ctx = new Context();
            var foo = ZilAtom.Parse("FOO", ctx);
            var bar = ZilAtom.Parse("BAR", ctx);

            TestHelpers.Evaluate(ctx, "<DEFINE PAIR (A B) <LIST .A .B>>");
            TestHelpers.Evaluate(ctx, "<SET A BAR>");
            TestHelpers.EvalAndAssert(ctx, "<PAIR FOO .A>", new ZilList(new[] { foo, bar }));
        }

        [TestMethod]
        public void OPT_And_AUX_Parameter_Defaults_Can_Refer_To_Earlier_Values()
        {
            var ctx = new Context();
            var foo = ZilAtom.Parse("FOO", ctx);
            var bar = ZilAtom.Parse("BAR", ctx);

            TestHelpers.Evaluate(ctx, "<DEFINE PAIR2 (A \"OPT\" (B .A) \"AUX\" (C .B)) <LIST .A .B .C>>");
            TestHelpers.EvalAndAssert(ctx, "<PAIR2 FOO>", new ZilList(new[] { foo, foo, foo }));
            TestHelpers.EvalAndAssert(ctx, "<PAIR2 FOO BAR>", new ZilList(new[] { foo, bar, bar }));
        }

        [TestMethod]
        public void FUNCTION_ADECL_Parameters_Should_Set_Binding_DECLs()
        {
            var ctx = new Context();

            TestHelpers.Evaluate(ctx, "<DEFINE FOO (A:FIX \"OPT\" B:FIX \"AUX\" C:FIX) <SET A T>>");
            TestHelpers.EvalAndCatch<DeclCheckError>(ctx, "<FOO 1>");

            TestHelpers.Evaluate(ctx, "<DEFINE BAR (A:FIX \"OPT\" B:FIX \"AUX\" C:FIX) <SET A 0> <SET B T>>");
            TestHelpers.EvalAndCatch<DeclCheckError>(ctx, "<BAR 1>");

            TestHelpers.Evaluate(ctx, "<DEFINE BAZ (A:FIX \"OPT\" B:FIX \"AUX\" C:FIX) <SET B 0> <SET C T>>");
            TestHelpers.EvalAndCatch<DeclCheckError>(ctx, "<BAZ 1>");

            TestHelpers.Evaluate(ctx, "<DEFINE OK (A:FIX \"OPT\" B:FIX \"AUX\" C:FIX) <SET A 0> <SET B 0> <SET C 0>>");
            TestHelpers.Evaluate(ctx, "<OK 1>");
        }

        [TestMethod]
        public void FUNCTION_Body_DECLs_Should_Set_Binding_DECLs()
        {
            var ctx = new Context();

            TestHelpers.Evaluate(ctx, "<DEFINE FOO (A \"OPT\" B \"AUX\" C) #DECL ((A B C) FIX) <SET A T>>");
            TestHelpers.EvalAndCatch<DeclCheckError>(ctx, "<FOO 1>");

            TestHelpers.Evaluate(ctx, "<DEFINE BAR (A \"OPT\" B \"AUX\" C) #DECL ((A B C) FIX) <SET B T>>");
            TestHelpers.EvalAndCatch<DeclCheckError>(ctx, "<BAR 1>");

            TestHelpers.Evaluate(ctx, "<DEFINE BAZ (A \"OPT\" B \"AUX\" C) #DECL ((A B C) FIX) <SET C T>>");
            TestHelpers.EvalAndCatch<DeclCheckError>(ctx, "<BAZ 1>");

            TestHelpers.Evaluate(ctx, "<DEFINE OK (A \"OPT\" B \"AUX\" C) #DECL ((A B C) FIX) <SET A 0> <SET B 0> <SET C 0>>");
            TestHelpers.Evaluate(ctx, "<OK 1>");
        }

        [TestMethod]
        public void FUNCTION_VALUE_Clause_Should_Check_Return_Value()
        {
            var ctx = new Context();

            TestHelpers.Evaluate(ctx, "<DEFINE FOO (A:FIX B:FIX \"VALUE\" <LIST FIX FIX>) (.A .B)>");
            TestHelpers.EvalAndAssert(ctx, "<FOO 1 2>",
                new ZilList(new[] { new ZilFix(1), new ZilFix(2) }));
            TestHelpers.EvalAndCatch<DeclCheckError>(ctx, "<FOO 1 X>");

            // also applies to values returned via an activation
            TestHelpers.Evaluate(ctx, "<DEFINE BAR (A:FIX B:FIX \"VALUE\" <LIST FIX FIX> \"ACT\" ACT) <BAZ .ACT> (.A .B)>");
            TestHelpers.Evaluate(ctx, "<DEFINE BAZ (ACT) <RETURN NOT-A-LIST .ACT>>");
            TestHelpers.EvalAndCatch<DeclCheckError>(ctx, "<BAR 1 2>");

            // and #DECL syntax
            TestHelpers.Evaluate(ctx, "<DEFINE BAH (A B) #DECL ((A B) FIX (VALUE) <LIST FIX FIX>) (.A .B)>");
            TestHelpers.EvalAndAssert(ctx, "<BAH 1 2>",
                new ZilList(new[] { new ZilFix(1), new ZilFix(2) }));
            TestHelpers.EvalAndCatch<DeclCheckError>(ctx, "<BAH 1 X>");
        }

        [TestMethod]
        public void FUNCTION_Calls_Should_Check_Argument_DECLs()
        {
            var ctx = new Context();

            TestHelpers.Evaluate(ctx, "<DEFINE FOO (A:FIX \"OPT\" B:FIX) <>>");
            TestHelpers.EvalAndAssert(ctx, "<FOO 1>", ctx.FALSE);
            TestHelpers.EvalAndAssert(ctx, "<FOO 1 2>", ctx.FALSE);
            TestHelpers.EvalAndCatch<DeclCheckError>(ctx, "<FOO X>");
            TestHelpers.EvalAndCatch<DeclCheckError>(ctx, "<FOO 1 X>");
        }

        [TestMethod]
        public void FUNCTION_Default_Values_Should_Be_Checked()
        {
            var ctx = new Context();

            TestHelpers.Evaluate(ctx, "<DEFINE FOO (\"OPT\" (A:FIX NOT-A-FIX)) <>>");
            TestHelpers.EvalAndCatch<DeclCheckError>(ctx, "<FOO>");

            TestHelpers.Evaluate(ctx, "<DEFINE BAR (\"AUX\" (A:FIX NOT-A-FIX)) <>>");
            TestHelpers.EvalAndCatch<DeclCheckError>(ctx, "<BAR>");
        }

        [TestMethod]
        public void FUNCTION_Rejects_Conflicting_DECLs()
        {
            TestHelpers.EvalAndCatch<InterpreterError>("<DEFINE FOO (A:FIX) #DECL ((A) LIST) <>>");
        }

        [TestMethod]
        public void FUNCTION_With_DECL_Still_Needs_A_Body()
        {
            TestHelpers.EvalAndCatch<ArgumentCountError>("<FUNCTION (A) #DECL ((A) FIX)>");
            TestHelpers.EvalAndCatch<ArgumentCountError>("<DEFINE FOO (A) #DECL ((A) FIX)>");
            TestHelpers.EvalAndCatch<ArgumentCountError>("<DEFINE20 FOO (A) #DECL ((A) FIX)>");
            TestHelpers.EvalAndCatch<ArgumentCountError>("<DEFMAC FOO (A) #DECL ((A) FIX)>");
        }

        [TestMethod]
        public void FUNCTION_ACT_And_BIND_Parameters_Should_Set_Binding_DECLs()
        {
            TestHelpers.EvalAndCatch<DeclCheckError>("<APPLY <FUNCTION (\"ACT\" A) <SET A 1>>>");
            TestHelpers.EvalAndCatch<DeclCheckError>("<APPLY <FUNCTION A () <SET A 1>>>");

            TestHelpers.EvalAndCatch<DeclCheckError>("<APPLY <FUNCTION (\"BIND\" B) <SET B 1>>>");
        }

        [TestMethod]
        public void BIND_Argument_Allows_Safe_INC()
        {
            var ctx = new Context();

            TestHelpers.Evaluate(ctx,
                @"<DEFINE INC (""BIND"" OUTER ATM) 
                    <SET .ATM <+ 1 <LVAL .ATM .OUTER>> .OUTER>>");

            ctx.SetLocalVal(ZilAtom.Parse("ATM", ctx), new ZilFix(100));
            TestHelpers.EvalAndAssert(ctx, "<INC ATM>", new ZilFix(101));
        }
    }
}
