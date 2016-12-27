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
using System.Diagnostics.Contracts;

namespace IntegrationTests
{
    [TestClass]
    public class FlowControlTests
    {
        static GlobalsAssertionHelper AssertGlobals(params string[] globals)
        {
            Contract.Requires(globals != null && globals.Length > 0);
            Contract.Requires(Contract.ForAll(globals, c => !string.IsNullOrWhiteSpace(c)));

            return new GlobalsAssertionHelper(globals);
        }

        static RoutineAssertionHelper AssertRoutine(string argSpec, string body)
        {
            Contract.Requires(argSpec != null);
            Contract.Requires(!string.IsNullOrWhiteSpace(body));

            return new RoutineAssertionHelper(argSpec, body);
        }

        static EntryPointAssertionHelper AssertEntryPoint(string argSpec, string body)
        {
            Contract.Requires(argSpec != null);
            Contract.Requires(!string.IsNullOrWhiteSpace(body));

            return new EntryPointAssertionHelper(argSpec, body);
        }

        #region RETURN

        [TestMethod]
        public void RETURN_Without_Activation_Should_Return_From_Block()
        {
            AssertRoutine("", "<FOO>")
                .WithGlobal("<ROUTINE FOO FOO-ACT (\"AUX\" X) <SET X <REPEAT () <RETURN 123>>> 456>")
                .GivesNumber("456");
        }

        [TestMethod]
        public void RETURN_With_Activation_Should_Return_From_Routine()
        {
            AssertRoutine("", "<FOO>")
                .WithGlobal("<ROUTINE FOO FOO-ACT (\"AUX\" X) <SET X <REPEAT () <RETURN 123 .FOO-ACT>>> 456>")
                .GivesNumber("123");
        }

        [TestMethod]
        public void RETURN_With_Activation_Can_Return_From_Outer_Block()
        {
            AssertRoutine("\"AUX\" X",
                "<SET X <PROG OUTER () <PROG () <RETURN 123 .OUTER> 456> 789>> <PRINTN .X>")
                .Outputs("123");
        }

        [TestMethod]
        public void RETURN_Inside_BIND_Should_Return_From_Outer_Block()
        {
            AssertRoutine("",
                "<PROG () <+ 3 <PROG () <BIND () <RETURN 120>> 456>>>")
                .GivesNumber("123");
        }

        [TestMethod]
        public void RETURN_With_Activation_In_Void_Context_Should_Not_Warn()
        {
            // activation + simple value => no warning
            AssertRoutine("",
                "<PROG FOO () <RETURN <> .FOO> <QUIT>> 123")
                .WithoutWarnings()
                .GivesNumber("123");

            // no activation + simple value => warning
            AssertRoutine("",
                "<PROG () <RETURN <>> <QUIT>> 123")
                .WithWarnings()
                .GivesNumber("123");

            // activation + other value => warning
            AssertRoutine("",
                "<PROG FOO () <RETURN 9 .FOO> <QUIT>> 123")
                .WithWarnings()
                .GivesNumber("123");
        }

        #endregion

        #region AGAIN

        [TestMethod]
        public void AGAIN_Should_Reset_Local_Variable_Defaults()
        {
            // TODO: specify what AGAIN should do with local variables in V3-4

            AssertRoutine("\"AUX\" (FOO 1)", "<COND (,GLOB <RETURN .FOO>) (T <INC GLOB> <SET FOO 99> <AGAIN>)>")
                .WithGlobal("<GLOBAL GLOB 0>")
                .InV5()
                .GivesNumber("1");
        }

        [TestMethod]
        public void AGAIN_With_Activation_Should_Repeat_Routine()
        {
            AssertRoutine("", "<FOO>")
                .WithGlobal("<GLOBAL BAR 0>")
                .WithGlobal("<ROUTINE FOO FOO-ACT () <PRINTI \"Top\"> <PROG () <PRINTN ,BAR> <COND (,BAR <RTRUE>)> <INC BAR> <AGAIN .FOO-ACT>>>")
                .Outputs("Top0Top1");
        }

        [TestMethod]
        public void AGAIN_Without_Activation_Should_Repeat_Block()
        {
            AssertRoutine("", "<FOO>")
                .WithGlobal("<GLOBAL BAR 0>")
                .WithGlobal("<ROUTINE FOO FOO-ACT () <PRINTI \"Top\"> <PROG () <PRINTN ,BAR> <COND (,BAR <RTRUE>)> <INC BAR> <AGAIN>>>")
                .Outputs("Top01");
        }

        #endregion

        #region DO

        [TestMethod]
        public void TestDO_Up_Fixes()
        {
            AssertRoutine("", "<DO (I 1 5) <PRINTN .I> <CRLF>>")
                .Outputs("1\n2\n3\n4\n5\n");
        }

        [TestMethod]
        public void TestDO_Down_Fixes()
        {
            AssertRoutine("", "<DO (I 5 1) <PRINTN .I> <CRLF>>")
                .Outputs("5\n4\n3\n2\n1\n");
        }

        [TestMethod]
        public void TestDO_Up_Fixes_By2()
        {
            AssertRoutine("", "<DO (I 1 5 2) <PRINTN .I> <CRLF>>")
                .Outputs("1\n3\n5\n");
        }

        [TestMethod]
        public void TestDO_Down_Fixes_By2()
        {
            AssertRoutine("", "<DO (I 5 1 -2) <PRINTN .I> <CRLF>>")
                .Outputs("5\n3\n1\n");
        }

        [TestMethod]
        public void TestDO_Up_Fixes_ByN()
        {
            AssertRoutine("\"AUX\" (N 2)", "<DO (I 1 5 .N) <PRINTN .I> <CRLF>>")
                .Outputs("1\n3\n5\n");
        }

        [TestMethod]
        public void TestDO_Up_Fixes_CalculateInc()
        {
            AssertRoutine("", "<DO (I 1 16 <* 2 .I>) <PRINTN .I> <CRLF>>")
                .Outputs("1\n2\n4\n8\n16\n");
        }

        [TestMethod]
        public void TestDO_Up_Forms()
        {
            AssertRoutine("", "<DO (I <FOO> <BAR .I>) <PRINTN .I> <CRLF>>")
                .WithGlobal("<ROUTINE FOO () <PRINTI \"FOO\"> <CRLF> 7>")
                .WithGlobal("<ROUTINE BAR (I) <PRINTI \"BAR\"> <CRLF> <G? .I 9>>")
                .Outputs("FOO\nBAR\n7\nBAR\n8\nBAR\n9\nBAR\n");
        }

        [TestMethod]
        public void TestDO_Result()
        {
            AssertRoutine("", "<DO (I 1 10) <>>")
                .GivesNumber("1");
        }

        [TestMethod]
        public void TestDO_Result_RETURN()
        {
            AssertRoutine("", "<DO (I 1 10) <COND (<==? .I 5> <RETURN <* .I 3>>)>>")
                .GivesNumber("15");
        }

        [TestMethod]
        public void TestDO_EndClause()
        {
            AssertRoutine("",
                "<DO (I 1 4) " +
                "  (<TELL \"rock!\">) " +
                "  <TELL N .I> " +
                "  <COND (<G=? .I 3> <TELL \" o'clock\">)> " +
                "  <TELL \", \">>")
                .Outputs("1, 2, 3 o'clock, 4 o'clock, rock!");
        }

        #endregion

        #region MAP-CONTENTS

        [TestMethod]
        public void TestMAP_CONTENTS_Basic()
        {
            AssertRoutine("", "<MAP-CONTENTS (F ,TABLE) <PRINTD .F> <CRLF>>")
                .WithGlobal("<OBJECT TABLE (DESC \"table\")>")
                .WithGlobal("<OBJECT APPLE (IN TABLE) (DESC \"apple\")>")
                .WithGlobal("<OBJECT CHERRY (IN TABLE) (DESC \"cherry\")>")
                .WithGlobal("<OBJECT BANANA (IN TABLE) (DESC \"banana\")>")
                .Outputs("apple\nbanana\ncherry\n");
        }

        [TestMethod]
        public void TestMAP_CONTENTS_WithNext()
        {
            AssertRoutine("", "<MAP-CONTENTS (F N ,TABLE) <REMOVE .F> <PRINTD .F> <PRINTI \", \"> <PRINTD? .N> <CRLF>>")
               .WithGlobal("<ROUTINE PRINTD? (OBJ) <COND (.OBJ <PRINTD .OBJ>) (ELSE <PRINTI \"nothing\">)>>")
               .WithGlobal("<OBJECT TABLE (DESC \"table\")>")
               .WithGlobal("<OBJECT APPLE (IN TABLE) (DESC \"apple\")>")
               .WithGlobal("<OBJECT CHERRY (IN TABLE) (DESC \"cherry\")>")
               .WithGlobal("<OBJECT BANANA (IN TABLE) (DESC \"banana\")>")
               .Outputs("apple, banana\nbanana, cherry\ncherry, nothing\n");
        }

        [TestMethod]
        public void TestMAP_CONTENTS_WithEnd()
        {
            AssertRoutine("\"AUX\" (SUM 0)", "<MAP-CONTENTS (F ,TABLE) (END <RETURN .SUM>) <SET SUM <+ .SUM <GETP .F ,P?PRICE>>>>")
                .WithGlobal("<OBJECT TABLE (DESC \"table\")>")
                .WithGlobal("<OBJECT APPLE (IN TABLE) (PRICE 1)>")
                .WithGlobal("<OBJECT CHERRY (IN TABLE) (PRICE 2)>")
                .WithGlobal("<OBJECT BANANA (IN TABLE) (PRICE 3)>")
                .GivesNumber("6");
        }

        [TestMethod]
        public void TestMAP_CONTENTS_WithEnd_Empty()
        {
            AssertRoutine("\"AUX\" (SUM 0)", "<MAP-CONTENTS (F ,TABLE) (END <RETURN 42>) <RFALSE>>")
                .WithGlobal("<OBJECT TABLE (DESC \"table\")>")
                .GivesNumber("42");
        }

        [TestMethod]
        public void TestMAP_CONTENTS_WithNextAndEnd()
        {
            AssertRoutine("\"AUX\" (SUM 0)", "<MAP-CONTENTS (F N ,TABLE) (END <RETURN .SUM>) <REMOVE .F> <SET SUM <+ .SUM <GETP .F ,P?PRICE>>>>")
                .WithGlobal("<OBJECT TABLE (DESC \"table\")>")
                .WithGlobal("<OBJECT APPLE (IN TABLE) (PRICE 1)>")
                .WithGlobal("<OBJECT CHERRY (IN TABLE) (PRICE 2)>")
                .WithGlobal("<OBJECT BANANA (IN TABLE) (PRICE 3)>")
                .GivesNumber("6");
        }

        [TestMethod]
        public void TestMAP_CONTENTS_WithNextAndEnd_Empty()
        {
            AssertRoutine("\"AUX\" (SUM 0)", "<MAP-CONTENTS (F N ,TABLE) (END <RETURN 42>) <RFALSE>>")
                .WithGlobal("<OBJECT TABLE (DESC \"table\")>")
                .GivesNumber("42");
        }

        #endregion

        #region MAP-DIRECTIONS

        [TestMethod]
        public void TestMAP_DIRECTIONS()
        {
            AssertRoutine("", "<MAP-DIRECTIONS (D P ,CENTER) <TELL N .D \" \" D <GETB .P ,REXIT> CR>>")
                .WithGlobal("<DIRECTIONS NORTH SOUTH EAST WEST>")
                .WithGlobal("<OBJECT CENTER (NORTH TO N-ROOM) (WEST TO W-ROOM)>")
                .WithGlobal("<OBJECT N-ROOM (DESC \"north room\")>")
                .WithGlobal("<OBJECT W-ROOM (DESC \"west room\")>")
                .InV3()
                .Outputs("31 north room\n28 west room\n");
        }

        #endregion

        #region COND

        [TestMethod]
        public void COND_With_Parts_After_T_Should_Warn()
        {
            AssertRoutine("",
                "<COND (<=? 0 1> <TELL \"nope\">) (T <TELL \"ok\">) (<=? 0 0> <TELL \"too late\">)>")
                .WithWarnings()
                .Compiles();
        }

        [TestMethod]
        public void COND_Should_Allow_Macro_Clauses()
        {
            AssertRoutine("",
                "<COND <LIVE-CONDITION> <DEAD-CONDITION> <IF-IN-ZILCH (<=? 2 2> <TELL \"2\">)> <IFN-IN-ZILCH (<=? 3 3> <TELL \"3\">)> (T <TELL \"end\">)>")
                .WithGlobal("<DEFMAC LIVE-CONDITION () '(<=? 0 1> <TELL \"nope\">)>")
                .WithGlobal("<DEFMAC DEAD-CONDITION () '<>>")
                .Outputs("2");
        }

        [TestMethod]
        public void Constants_In_COND_Clause_Should_Only_Be_Stored_If_At_End()
        {
            AssertRoutine("\"AUX\" (A 0)",
                "<SET A <COND (T 123 <PRINTN .A> 456)>>")
                .Outputs("0");
        }

        #endregion

        #region VERSION?

        [TestMethod]
        public void VERSION_P_With_Parts_After_T_Should_Warn()
        {
            AssertRoutine("",
                "<VERSION? (ZIP <TELL \"classic\">) (T <TELL \"extended\">) (XZIP <TELL \"too late\">)>")
                .InV5()
                .WithWarnings()
                .Compiles();
        }

        #endregion

        #region Routines

        [TestMethod]
        public void Routine_With_Too_Many_Required_Arguments_For_Platform_Should_Not_Compile()
        {
            AssertGlobals(
                "<ROUTINE FOO (A B C D) <>>")
                .InV3()
                .DoesNotCompile();

            AssertGlobals(
                "<ROUTINE FOO (A B C D) <>>")
                .InV5()
                .Compiles();

            AssertGlobals(
                "<ROUTINE FOO (A B C D E F G H) <>>")
                .InV5()
                .DoesNotCompile();
        }

        [TestMethod]
        public void Routine_With_Too_Many_Optional_Arguments_For_Platform_Should_Warn()
        {
            AssertRoutine("\"OPT\" A B C D", "<>")
                .InV3()
                .WithWarnings()
                .Compiles();

            AssertRoutine("\"OPT\" A B C D", "<>")
                .InV5()
                .WithoutWarnings()
                .Compiles();

            AssertRoutine("\"OPT\" A B C D E F G H", "<>")
                .InV5()
                .WithWarnings()
                .Compiles();
        }

        [TestMethod]
        public void Call_With_Too_Many_Arguments_Should_Not_Compile()
        {
            AssertRoutine("", "<FOO 1 2 3>")
                .WithGlobal("<ROUTINE FOO () <>>")
                .DoesNotCompile();

            AssertRoutine("", "<FOO 1 2 3>")
                .WithGlobal("<ROUTINE FOO (X) <>>")
                .DoesNotCompile();

            AssertRoutine("", "<FOO 1 2 3>")
                .WithGlobal("<ROUTINE FOO (X Y Z) <>>")
                .Compiles();
        }

        [TestMethod]
        public void APPLY_With_Too_Many_Arguments_For_Platform_Should_Not_Compile()
        {
            AssertRoutine("", "<APPLY <> 1 2 3 4>")
                .InV3()
                .DoesNotCompile();

            AssertRoutine("", "<APPLY <> 1 2 3 4>")
                .InV5()
                .Compiles();

            AssertRoutine("", "<APPLY <> 1 2 3 4 5 6 7 8>")
                .InV5()
                .DoesNotCompile();
        }

        [TestMethod]
        public void CONSTANT_FALSE_Can_Be_Called_Like_A_Routine()
        {
            AssertRoutine("", "<FOO 1 2 3 <INC G>> ,G")
                .WithGlobal("<GLOBAL G 100>")
                .WithGlobal("<CONSTANT FOO <>>")
                .GivesNumber("101");
        }

        #endregion

        #region GO routine (entry point)

        [TestMethod]
        public void GO_Routine_With_Locals_Should_Give_Error()
        {
            AssertEntryPoint("X Y Z", @"<TELL ""hi"" CR>")
                .DoesNotCompile();
        }

        [TestMethod]
        public void GO_Routine_With_Locals_In_PROG_Should_Give_Error()
        {
            AssertEntryPoint("", @"<PROG (X Y Z) <TELL ""hi"" CR>>")
                .DoesNotCompile();
        }

        [TestMethod]
        public void GO_Routine_With_MultiEquals_Should_Not_Throw()
        {
            AssertEntryPoint("", @"<COND (<=? <FOO> 1 2 3 4> <TELL ""equals"">)>")
                .WithGlobal("<ROUTINE FOO () 5>")
                .DoesNotThrow();
        }

        [TestMethod]
        public void GO_Routine_With_SETG_Indirect_Involving_Stack_Should_Not_Throw()
        {
            AssertEntryPoint("", @"<SETG <+ ,VARNUM 1> <* ,VARVAL 2>>")
                .WithGlobal("<GLOBAL VARNUM 16>")
                .WithGlobal("<GLOBAL VARVAL 100>")
                .DoesNotThrow();
        }
        
        #endregion
    }
}
