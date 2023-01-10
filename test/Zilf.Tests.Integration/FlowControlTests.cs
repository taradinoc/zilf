/* Copyright 2010-2023 Tara McGrew
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
using System.Threading.Tasks;

namespace Zilf.Tests.Integration
{
    [TestClass, TestCategory("Compiler"), TestCategory("Flow Control")]
    public class FlowControlTests : IntegrationTestClass
    {
        #region RETURN

        [TestMethod]
        public async Task RETURN_Without_Activation_Should_Return_From_Block()
        {
            await AssertRoutine("", "<FOO>")
                .WithGlobal("<ROUTINE FOO FOO-ACT (\"AUX\" X) <SET X <REPEAT () <RETURN 123>>> 456>")
                .GivesNumberAsync("456");
        }

        [TestMethod]
        public async Task RETURN_With_Activation_Should_Return_From_Routine()
        {
            await AssertRoutine("", "<FOO>")
                .WithGlobal("<ROUTINE FOO FOO-ACT (\"AUX\" X) <SET X <REPEAT () <RETURN 123 .FOO-ACT>>> 456>")
                .GivesNumberAsync("123");
        }

        [TestMethod]
        public async Task RETURN_With_Activation_Can_Return_From_Outer_Block()
        {
            await AssertRoutine("\"AUX\" X",
                    "<SET X <PROG OUTER () <PROG () <RETURN 123 .OUTER> 456> 789>> <PRINTN .X>")
                .OutputsAsync("123");
        }

        [TestMethod]
        public async Task RETURN_Inside_BIND_Should_Return_From_Outer_Block()
        {
            await AssertRoutine("", "<PROG () <+ 3 <PROG () <BIND () <RETURN 120>> 456>>>")
                .GivesNumberAsync("123");
        }

        [TestMethod]
        public async Task RETURN_With_Activation_In_Void_Context_Should_Not_Warn()
        {
            // activation + simple value => no warning
            await AssertRoutine("", "<PROG FOO () <RETURN <> .FOO> <QUIT>> 123")
                .WithoutWarnings()
                .GivesNumberAsync("123");

            // no activation + simple value => warning
            await AssertRoutine("", "<PROG () <RETURN <>> <QUIT>> 123")
                .WithWarnings()
                .GivesNumberAsync("123");

            // activation + other value => warning
            await AssertRoutine("", "<PROG FOO () <RETURN 9 .FOO> <QUIT>> 123")
                .WithWarnings()
                .GivesNumberAsync("123");
        }

        [TestMethod]
        public async Task RETURN_With_DO_FUNNY_RETURN_True_Or_High_Version_Should_Exit_Routine()
        {
            await AssertRoutine("\"AUX\" X", "<SET X <PROG () <RETURN 123>>> <* .X 2>")
                .WithGlobal("<SETG DO-FUNNY-RETURN? T>")
                .InV3()
                .GivesNumberAsync("123");

            await AssertRoutine("\"AUX\" X", "<SET X <PROG () <RETURN 123>>> <* .X 2>")
                .InV5()
                .GivesNumberAsync("123");
        }

        [TestMethod]
        public async Task RETURN_With_DO_FUNNY_RETURN_False_Or_Low_Version_Should_Exit_Block()
        {
            await AssertRoutine("\"AUX\" X", "<SET X <PROG () <RETURN 123>>> <* .X 2>")
                .WithGlobal("<SETG DO-FUNNY-RETURN? <>>")
                .InV5()
                .GivesNumberAsync("246");

            await AssertRoutine("\"AUX\" X", "<SET X <PROG () <RETURN 123>>> <* .X 2>")
                .InV3()
                .GivesNumberAsync("246");
        }

        #endregion

        #region AGAIN

        [TestMethod]
        public async Task AGAIN_Should_Reset_Local_Variable_Defaults()
        {
            // TODO: specify what AGAIN should do with local variables in V3-4

            await AssertRoutine("\"AUX\" (FOO 1)", "<COND (,GLOB <RETURN .FOO>) (T <INC GLOB> <SET FOO 99> <AGAIN>)>")
                .WithGlobal("<GLOBAL GLOB 0>")
                .InV5()
                .GivesNumberAsync("1");
        }

        [TestMethod]
        public async Task AGAIN_With_Activation_Should_Repeat_Routine()
        {
            await AssertRoutine("", "<FOO>")
                .WithGlobal("<GLOBAL BAR 0>")
                .WithGlobal("<ROUTINE FOO FOO-ACT () <PRINTI \"Top\"> <PROG () <PRINTN ,BAR> <COND (,BAR <RTRUE>)> <INC BAR> <AGAIN .FOO-ACT>>>")
                .OutputsAsync("Top0Top1");
        }

        [TestMethod]
        public async Task AGAIN_Without_Activation_Should_Repeat_Block()
        {
            await AssertRoutine("", "<FOO>")
                .WithGlobal("<GLOBAL BAR 0>")
                .WithGlobal("<ROUTINE FOO FOO-ACT () <PRINTI \"Top\"> <PROG () <PRINTN ,BAR> <COND (,BAR <RTRUE>)> <INC BAR> <AGAIN>>>")
                .OutputsAsync("Top01");
        }

        #endregion

        #region DO

        [TestMethod]
        public async Task TestDO_Up_Fixes()
        {
            await AssertRoutine("", "<DO (I 1 5) <PRINTN .I> <CRLF>>")
                .OutputsAsync("1\n2\n3\n4\n5\n");
        }

        [TestMethod]
        public async Task TestDO_Down_Fixes()
        {
            await AssertRoutine("", "<DO (I 5 1) <PRINTN .I> <CRLF>>")
                .OutputsAsync("5\n4\n3\n2\n1\n");
        }

        [TestMethod]
        public async Task TestDO_Up_Fixes_By2()
        {
            await AssertRoutine("", "<DO (I 1 5 2) <PRINTN .I> <CRLF>>")
                .OutputsAsync("1\n3\n5\n");
        }

        [TestMethod]
        public async Task TestDO_Down_Fixes_By2()
        {
            await AssertRoutine("", "<DO (I 5 1 -2) <PRINTN .I> <CRLF>>")
                .OutputsAsync("5\n3\n1\n");
        }

        [TestMethod]
        public async Task TestDO_Up_Fixes_ByN()
        {
            await AssertRoutine("\"AUX\" (N 2)", "<DO (I 1 5 .N) <PRINTN .I> <CRLF>>")
                .OutputsAsync("1\n3\n5\n");
        }

        [TestMethod]
        public async Task TestDO_Up_Fixes_CalculateInc()
        {
            await AssertRoutine("", "<DO (I 1 16 <* 2 .I>) <PRINTN .I> <CRLF>>")
                .OutputsAsync("1\n2\n4\n8\n16\n");
        }

        [TestMethod]
        public async Task TestDO_Up_Forms()
        {
            await AssertRoutine("", "<DO (I <FOO> <BAR .I>) <PRINTN .I> <CRLF>>")
                .WithGlobal("<ROUTINE FOO () <PRINTI \"FOO\"> <CRLF> 7>")
                .WithGlobal("<ROUTINE BAR (I) <PRINTI \"BAR\"> <CRLF> <G? .I 9>>")
                .OutputsAsync("FOO\nBAR\n7\nBAR\n8\nBAR\n9\nBAR\n");
        }

        [TestMethod]
        public async Task TestDO_Result()
        {
            await AssertRoutine("", "<DO (I 1 10) <>>")
                .GivesNumberAsync("1");
        }

        [TestMethod]
        public async Task TestDO_Result_RETURN()
        {
            await AssertRoutine("", "<DO (I 1 10) <COND (<==? .I 5> <RETURN <* .I 3>>)>>")
                .GivesNumberAsync("15");

            await AssertRoutine("\"AUX\" X", "<SET X <DO (I 1 10) <COND (<==? .I 5> <RETURN <* .I 3>>)>>> <* .X 10>")
                .GivesNumberAsync("150");
        }

        [TestMethod]
        public async Task TestDO_EndClause()
        {
            await AssertRoutine("",
                    @"<DO (I 1 4) (<TELL ""rock!"">)
                               <TELL N .I>
                               <COND (<G=? .I 3> <TELL "" o'clock"">)>
                               <TELL "", "">>")
                .OutputsAsync("1, 2, 3 o'clock, 4 o'clock, rock!");
        }

        [TestMethod]
        public async Task TestDO_EndClause_Misplaced()
        {
            await AssertRoutine("",
                    @"<DO (CNT 0 25 5)
                               <TELL N .CNT CR>
                               (END <TELL ""This message is never printed"">)>")
                .DoesNotCompileAsync();
        }

        [TestMethod]
        public async Task Unused_DO_Variables_Should_Not_Warn()
        {
            await AssertRoutine("", "<DO (I 1 10) <TELL \"spam\">>")
                .WithoutWarnings()
                .CompilesAsync();
        }

        #endregion

        #region MAP-CONTENTS

        [TestMethod]
        public async Task TestMAP_CONTENTS_Basic()
        {
            await AssertRoutine("", "<MAP-CONTENTS (F ,TABLE) <PRINTD .F> <CRLF>>")
                .WithGlobal("<OBJECT TABLE (DESC \"table\")>")
                .WithGlobal("<OBJECT APPLE (IN TABLE) (DESC \"apple\")>")
                .WithGlobal("<OBJECT CHERRY (IN TABLE) (DESC \"cherry\")>")
                .WithGlobal("<OBJECT BANANA (IN TABLE) (DESC \"banana\")>")
                .OutputsAsync("apple\nbanana\ncherry\n");
        }

        [TestMethod]
        public async Task TestMAP_CONTENTS_WithNext()
        {
            await AssertRoutine("", "<MAP-CONTENTS (F N ,TABLE) <REMOVE .F> <PRINTD .F> <PRINTI \", \"> <PRINTD? .N> <CRLF>>")
                .WithGlobal("<ROUTINE PRINTD? (OBJ) <COND (.OBJ <PRINTD .OBJ>) (ELSE <PRINTI \"nothing\">)>>")
                .WithGlobal("<OBJECT TABLE (DESC \"table\")>")
                .WithGlobal("<OBJECT APPLE (IN TABLE) (DESC \"apple\")>")
                .WithGlobal("<OBJECT CHERRY (IN TABLE) (DESC \"cherry\")>")
                .WithGlobal("<OBJECT BANANA (IN TABLE) (DESC \"banana\")>")
                .OutputsAsync("apple, banana\nbanana, cherry\ncherry, nothing\n");
        }

        [TestMethod]
        public async Task TestMAP_CONTENTS_WithEnd()
        {
            await AssertRoutine("\"AUX\" (SUM 0)", "<MAP-CONTENTS (F ,TABLE) (END <RETURN .SUM>) <SET SUM <+ .SUM <GETP .F ,P?PRICE>>>>")
                .WithGlobal("<OBJECT TABLE (DESC \"table\")>")
                .WithGlobal("<OBJECT APPLE (IN TABLE) (PRICE 1)>")
                .WithGlobal("<OBJECT CHERRY (IN TABLE) (PRICE 2)>")
                .WithGlobal("<OBJECT BANANA (IN TABLE) (PRICE 3)>")
                .GivesNumberAsync("6");
        }

        [TestMethod]
        public async Task TestMAP_CONTENTS_WithEnd_Empty()
        {
            await AssertRoutine("\"AUX\" (SUM 0)", "<MAP-CONTENTS (F ,TABLE) (END <RETURN 42>) <RFALSE>>")
                .WithGlobal("<OBJECT TABLE (DESC \"table\")>")
                .GivesNumberAsync("42");
        }

        [TestMethod]
        public async Task TestMAP_CONTENTS_WithNextAndEnd()
        {
            await AssertRoutine("\"AUX\" (SUM 0)", "<MAP-CONTENTS (F N ,TABLE) (END <RETURN .SUM>) <REMOVE .F> <SET SUM <+ .SUM <GETP .F ,P?PRICE>>>>")
                .WithGlobal("<OBJECT TABLE (DESC \"table\")>")
                .WithGlobal("<OBJECT APPLE (IN TABLE) (PRICE 1)>")
                .WithGlobal("<OBJECT CHERRY (IN TABLE) (PRICE 2)>")
                .WithGlobal("<OBJECT BANANA (IN TABLE) (PRICE 3)>")
                .GivesNumberAsync("6");
        }

        [TestMethod]
        public async Task TestMAP_CONTENTS_WithNextAndEnd_Empty()
        {
            await AssertRoutine("\"AUX\" (SUM 0)", "<MAP-CONTENTS (F N ,TABLE) (END <RETURN 42>) <RFALSE>>")
                .WithGlobal("<OBJECT TABLE (DESC \"table\")>")
                .GivesNumberAsync("42");
        }

        [TestMethod]
        public async Task Unused_MAP_CONTENTS_Variables_Should_Not_Warn()
        {
            await AssertRoutine("\"AUX\" CNT", "<MAP-CONTENTS (I ,STARTROOM) <SET CNT <+ .CNT 1>>>")
                .WithGlobal("<ROOM STARTROOM>")
                .WithGlobal("<OBJECT CHIMP (IN STARTROOM)>")
                .WithGlobal("<OBJECT CHAMP (IN STARTROOM)>")
                .WithGlobal("<OBJECT CHUMP (IN STARTROOM)>")
                .WithoutWarnings()
                .CompilesAsync();

            await AssertRoutine("", "<MAP-CONTENTS (I N ,STARTROOM) <REMOVE .I>>")
                .WithGlobal("<ROOM STARTROOM>")
                .WithGlobal("<OBJECT CHIMP (IN STARTROOM)>")
                .WithGlobal("<OBJECT CHAMP (IN STARTROOM)>")
                .WithGlobal("<OBJECT CHUMP (IN STARTROOM)>")
                .WithoutWarnings()
                .CompilesAsync();
        }

        #endregion

        #region MAP-DIRECTIONS

        [TestMethod]
        public async Task TestMAP_DIRECTIONS()
        {
            await AssertRoutine("", @"<MAP-DIRECTIONS (D P ,CENTER) <TELL N .D "" "" D <GETB .P ,REXIT> CR>>")
                .WithGlobal("<DIRECTIONS NORTH SOUTH EAST WEST>")
                .WithGlobal("<OBJECT CENTER (NORTH TO N-ROOM) (WEST TO W-ROOM)>")
                .WithGlobal("<OBJECT N-ROOM (DESC \"north room\")>")
                .WithGlobal("<OBJECT W-ROOM (DESC \"west room\")>")
                .InV3()
                .OutputsAsync("31 north room\n28 west room\n");
        }

        [TestMethod]
        public async Task TestMAP_DIRECTIONS_WithEnd()
        {
            await AssertRoutine("", @"<MAP-DIRECTIONS (D P ,CENTER) (END <TELL ""done"" CR>) <TELL N .D "" "" D <GETB .P ,REXIT> CR>>")
                .WithGlobal("<DIRECTIONS NORTH SOUTH EAST WEST>")
                .WithGlobal("<OBJECT CENTER (NORTH TO N-ROOM) (WEST TO W-ROOM)>")
                .WithGlobal("<OBJECT N-ROOM (DESC \"north room\")>")
                .WithGlobal("<OBJECT W-ROOM (DESC \"west room\")>")
                .InV3()
                .OutputsAsync("31 north room\n28 west room\ndone\n");
        }

        #endregion

        #region COND

        [TestMethod]
        public async Task COND_With_Parts_After_T_Should_Warn()
        {
            await AssertRoutine("", "<COND (<=? 0 1> <TELL \"nope\">) (T <TELL \"ok\">) (<=? 0 0> <TELL \"too late\">)>")
                .WithWarnings()
                .CompilesAsync();
        }

        [TestMethod]
        public async Task COND_With_False_Condition_From_Macro_Or_Constant_Should_Not_Warn()
        {
            await AssertRoutine("",
                    "<COND (<DO-IT?> <TELL \"do it\">) (,DO-OTHER? <TELL \"do other\">)>")
                .WithGlobal("<DEFMAC DO-IT? () <>>")
                .WithGlobal("<CONSTANT DO-OTHER? <>>")
                .WithoutWarnings()
                .CompilesAsync();

            // ... but should still warn if the condition was a literal
            await AssertRoutine("",
                    "<COND (<> <TELL \"done\">)>")
                .WithWarnings()
                .CompilesAsync();
        }

        [TestMethod]
        public async Task AND_In_Void_Context_With_Macro_At_End_Should_Work()
        {
            await AssertRoutine("",
                    "<AND <FOO> <BAR>> <RETURN>")
                .WithGlobal("<ROUTINE FOO () T>")
                .WithGlobal("<DEFMAC BAR () '<PRINTN 42>>")
                .OutputsAsync("42");
        }

        [TestMethod]
        public async Task COND_Should_Allow_Macro_Clauses()
        {
            await AssertRoutine("",
                    "<COND <LIVE-CONDITION> <DEAD-CONDITION> <IF-IN-ZILCH (<=? 2 2> <TELL \"2\">)> <IFN-IN-ZILCH (<=? 3 3> <TELL \"3\">)> (T <TELL \"end\">)>")
                .WithGlobal("<DEFMAC LIVE-CONDITION () '(<=? 0 1> <TELL \"nope\">)>")
                .WithGlobal("<DEFMAC DEAD-CONDITION () '<>>")
                .WithoutWarnings()
                .OutputsAsync("2");
        }

        [TestMethod]
        public async Task COND_Should_Reject_Non_Macro_Forms()
        {
            await AssertRoutine("\"AUX\" FOO",
                    "<COND <SET FOO 123> (<=? .FOO 123> <PRINTN 456>)>")
                .DoesNotCompileAsync("ZIL0100");
        }

        [TestMethod]
        public async Task Constants_In_COND_Clause_Should_Only_Be_Stored_If_At_End()
        {
            await AssertRoutine("\"AUX\" (A 0)",
                    "<SET A <COND (T 123 <PRINTN .A> 456)>>")
                .OutputsAsync("0");
        }

        #endregion

        #region BIND/PROG

        [TestMethod]
        public async Task BIND_Deferred_Return_Pattern_In_Void_Context_Should_Not_Use_A_Variable()
        {
            await AssertRoutine("", "<BIND (RESULT) <SET RESULT <FOO>> <PRINTN 1> .RESULT> <CRLF>")
                .WithGlobal("<ROUTINE FOO () 123>")
                .GeneratesCodeNotMatchingAsync(@"RESULT");
        }

        [TestMethod]
        public async Task PROG_Result_Should_Not_Be_Forced_Onto_Stack()
        {
            await AssertRoutine("\"AUX\" X", "<SET X <PROG () <COND (.X 1) (ELSE 2)>>>")
                .GeneratesCodeMatchingAsync("SET 'X,1");

            await AssertRoutine("\"AUX\" X", "<SET X <PROG () <RETURN <COND (.X 1) (ELSE 2)>>>>")
                .GeneratesCodeMatchingAsync("SET 'X,1");

            await AssertRoutine("\"AUX\" X", "<COND (<PROG () .X> T)>")
                .GeneratesCodeNotMatchingAsync(@"PUSH");
        }

        [TestMethod]
        public async Task REPEAT_Last_Expression_Should_Not_Clutter_Stack()
        {
            await AssertRoutine("", "<REPEAT () 123>")
                .GeneratesCodeNotMatchingAsync(@"PUSH");
        }

        [TestMethod]
        public async Task Unused_PROG_Variables_Should_Warn()
        {
            await AssertRoutine("", "<PROG (X) <TELL \"hi\">>")
                .WithWarnings("ZIL0210")
                .CompilesAsync();

            await AssertRoutine("", "<BIND (X) <TELL \"hi\">>")
                .WithWarnings("ZIL0210")
                .CompilesAsync();

            await AssertRoutine("", "<REPEAT (X) <TELL \"hi\">>")
                .WithWarnings("ZIL0210")
                .CompilesAsync();
        }

        #endregion

        #region VERSION?

        [TestMethod]
        public async Task VERSION_P_With_Parts_After_T_Should_Warn()
        {
            await AssertRoutine("",
                    @"<VERSION? (ZIP <TELL ""classic"">) (T <TELL ""extended"">) (XZIP <TELL ""too late"">)>")
                .InV5()
                .WithWarnings()
                .CompilesAsync();
        }

        #endregion

        #region Routines

        [TestMethod]
        public async Task Routine_With_Too_Many_Required_Arguments_For_Platform_Should_Not_Compile()
        {
            await AssertGlobals("<ROUTINE FOO (A B C D) <>>")
                .InV3()
                .DoesNotCompileAsync();

            await AssertGlobals("<ROUTINE FOO (A B C D) <>>")
                .InV5()
                .CompilesAsync();

            await AssertGlobals("<ROUTINE FOO (A B C D E F G H) <>>")
                .InV5()
                .DoesNotCompileAsync();
        }

        [TestMethod]
        public async Task Routine_With_Too_Many_Optional_Arguments_For_Platform_Should_Warn()
        {
            await AssertRoutine("\"OPT\" A B C D", "<>")
                .InV3()
                .WithWarnings("MDL0417")
                .CompilesAsync();

            await AssertRoutine("\"OPT\" A B C D", "<>")
                .InV5()
                .WithoutWarnings("MDL0417")
                .CompilesAsync();

            await AssertRoutine("\"OPT\" A B C D E F G H", "<>")
                .InV5()
                .WithWarnings("MDL0417")
                .CompilesAsync();
        }

        [TestMethod]
        public async Task Call_With_Too_Many_Arguments_Should_Not_Compile()
        {
            await AssertRoutine("", "<FOO 1 2 3>")
                .WithGlobal("<ROUTINE FOO () <>>")
                .DoesNotCompileAsync();

            await AssertRoutine("", "<FOO 1 2 3>")
                .WithGlobal("<ROUTINE FOO (X) <>>")
                .DoesNotCompileAsync();

            await AssertRoutine("", "<FOO 1 2 3>")
                .WithGlobal("<ROUTINE FOO (X Y Z) <>>")
                .CompilesAsync();
        }

        [TestMethod]
        public async Task APPLY_With_Too_Many_Arguments_For_Platform_Should_Not_Compile()
        {
            await AssertRoutine("", "<APPLY <> 1 2 3 4>")
                .InV3()
                .DoesNotCompileAsync();

            await AssertRoutine("", "<APPLY <> 1 2 3 4>")
                .InV5()
                .CompilesAsync();

            await AssertRoutine("", "<APPLY <> 1 2 3 4 5 6 7 8>")
                .InV5()
                .DoesNotCompileAsync();
        }

        [TestMethod]
        public async Task CONSTANT_FALSE_Can_Be_Called_Like_A_Routine()
        {
            await AssertRoutine("", "<FOO 1 2 3 <INC G>> ,G")
                .WithGlobal("<GLOBAL G 100>")
                .WithGlobal("<CONSTANT FOO <>>")
                .GivesNumberAsync("101");
        }

        #endregion

        #region GO routine (entry point)

        [TestMethod]
        public async Task GO_Routine_With_Locals_Should_Give_Error()
        {
            await AssertEntryPoint("X Y Z", @"<TELL ""hi"" CR>")
                .DoesNotCompileAsync();

            await AssertEntryPoint("\"OPT\" X Y Z", @"<TELL ""hi"" CR>")
                .DoesNotCompileAsync();

            await AssertEntryPoint("\"AUX\" X Y Z", @"<TELL ""hi"" CR>")
                .DoesNotCompileAsync();
        }

        [TestMethod]
        public async Task GO_Routine_With_Locals_In_V6_Should_Compile()
        {
            await AssertEntryPoint("\"AUX\" A", "<SET A 5>")
                .InV6()
                .CompilesAsync();

            await AssertEntryPoint("\"OPT\" A", "<SET A 5>")
                .InV6()
                .CompilesAsync();

            // entry point still can't have required variables
            await AssertEntryPoint("A", "<SET A 5>")
                .InV6()
                .DoesNotCompileAsync();
        }

        [TestMethod]
        public async Task GO_Routine_With_Locals_In_PROG_Should_Give_Error()
        {
            await AssertEntryPoint("", @"<PROG (X Y Z) <TELL ""hi"" CR>>")
                .DoesNotCompileAsync();
        }

        [TestMethod]
        public async Task GO_Routine_With_MultiEquals_Should_Not_Throw()
        {
            await AssertEntryPoint("", @"<COND (<=? <FOO> 1 2 3 4> <TELL ""equals"">)>")
                .WithGlobal("<ROUTINE FOO () 5>")
                .DoesNotThrowAsync();
        }

        [TestMethod]
        public async Task GO_Routine_With_SETG_Indirect_Involving_Stack_Should_Not_Throw()
        {
            await AssertEntryPoint("", @"<SETG <+ ,VARNUM 1> <* ,VARVAL 2>>")
                .WithGlobal(@"<GLOBAL VARNUM 16>")
                .WithGlobal(@"<GLOBAL VARVAL 100>")
                .DoesNotThrowAsync();
        }
        
        #endregion
    }
}
