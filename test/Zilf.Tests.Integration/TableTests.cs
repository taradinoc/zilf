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
    [TestClass, TestCategory("Compiler")]
    public class TableTests : IntegrationTestClass
    {
        [TestMethod]
        public async Task BYTE_Elements_Should_Compile_As_Bytes()
        {
            await AssertGlobals(
                "<GLOBAL TBL <TABLE 12345 #BYTE 123 #BYTE 45>>")
                .ImpliesAsync(
                    "<==? <GET ,TBL 0> 12345>",
                    "<==? <GETB ,TBL 2> 123>",
                    "<==? <GETB ,TBL 3> 45>");
        }

        [TestMethod]
        public async Task WORD_Elements_Should_Compile_As_Words()
        {
            await AssertGlobals(
                "<GLOBAL TBL <TABLE (BYTE) #WORD (12345) 123 45>>")
                .ImpliesAsync(
                    "<==? <GET ,TBL 0> 12345>",
                    "<==? <GETB ,TBL 2> 123>",
                    "<==? <GETB ,TBL 3> 45>");
        }

        [TestMethod]
        public async Task ITABLE_Multi_Element_Initializers_Should_Repeat_N_Times()
        {
            await AssertGlobals(
                "<GLOBAL TBL1 <ITABLE 2 1 2 3>>",
                "<GLOBAL TBL2 <ITABLE 3 9 8 7 6>>")
                .ImpliesAsync(
                    "<==? <GET ,TBL1 0> 1>",
                    "<==? <GET ,TBL1 1> 2>",
                    "<==? <GET ,TBL1 2> 3>",
                    "<==? <GET ,TBL1 3> 1>",
                    "<==? <GET ,TBL1 4> 2>",
                    "<==? <GET ,TBL1 5> 3>");
        }

        [TestMethod]
        public async Task ITABLE_LEXV_Should_Warn_If_Not_A_Multiple_Of_3_Elements()
        {
            await AssertGlobals(
                "<CONSTANT LEXBUF <ITABLE 1 (LEXV) 0 0>>")
                .WithWarnings("MDL0428")
                .CompilesAsync();

            await AssertGlobals(
                "<CONSTANT LEXBUF <ITABLE 1 (LEXV)>>")
                .WithWarnings("MDL0428")
                .CompilesAsync();

            await AssertGlobals(
                "<CONSTANT LEXBUF <ITABLE 3 (LEXV)>>")
                .WithoutWarnings()
                .CompilesAsync();
        }

        [TestMethod]
        public async Task TABLE_PATTERN_Should_Affect_Element_Sizes()
        {
            await AssertGlobals(
                "<GLOBAL TBL <TABLE (PATTERN (BYTE WORD BYTE BYTE [REST WORD])) 1 2 3 4 5 6>>")
                .ImpliesAsync(
                    "<==? <GETB ,TBL 0> 1>",
                    "<==? <GET <REST ,TBL 1> 0> 2>",
                    "<==? <GETB ,TBL 3> 3>",
                    "<==? <GETB ,TBL 4> 4>",
                    "<==? <GET <REST ,TBL 5> 0> 5>",
                    "<==? <GET <REST ,TBL 5> 1> 6>");
        }

        [TestMethod]
        public async Task PURE_ITABLE_Should_Be_In_Pure_Memory()
        {
            await AssertGlobals(
                "<GLOBAL TBL <ITABLE 10 (PURE)>>")
                .ImpliesAsync(
                    "<G=? ,TBL <LOWCORE PURBOT>>");
        }

        [TestMethod]
        public async Task TABLE_Should_Be_Mutable_At_Compile_Time()
        {
            await AssertGlobals(
                "<SETG MY-TBL <TABLE 0 <BYTE 0>>>",
                "<ZPUT ,MY-TBL 0 1>",
                "<PUTB ,MY-TBL 2 2>",
                "<GLOBAL TBL ,MY-TBL>")
                .ImpliesAsync(
                    "<==? <GET ,TBL 0> 1>",
                    "<==? <GETB ,TBL 2> 2>");

            await AssertGlobals(
                "<SETG MY-TBL <ITABLE 3 <>>>",
                "<ZPUT ,MY-TBL 1 1>",
                "<GLOBAL TBL ,MY-TBL>")
                .ImpliesAsync(
                    "<==? <GET ,TBL 1> 1>");
        }

        [TestMethod]
        public async Task TABLE_Length_Words_Should_Be_Accessible_At_Compile_Time()
        {
            await AssertGlobals(
                "<SETG MY-TBL <LTABLE 100 200 300 400>>",
                "<GLOBAL ORIG-LENGTH <ZGET ,MY-TBL 0>>",
                "<ZPUT ,MY-TBL 0 -1>",
                "<GLOBAL TBL ,MY-TBL>")
                .ImpliesAsync(
                    "<==? ,ORIG-LENGTH 4>",
                    "<==? <GET ,TBL 0> -1>",
                    "<==? <GET ,TBL 4> 400>");
        }

        [TestMethod]
        public async Task TABLE_With_Adjacent_Bytes_Can_Be_Overwritten_With_Words()
        {
            // this doesn't change the length of the table (in bytes)
            await AssertGlobals(
                "<SETG MY-TBL <TABLE (BYTE) 0 0 67 0>>",
                "<ZPUT ,MY-TBL 0 12345>",
                "<PUTB ,MY-TBL 3 89>",
                "<GLOBAL TBL ,MY-TBL>")
                .ImpliesAsync(
                    "<==? <GET ,TBL 0> 12345>",
                    "<==? <GETB ,TBL 2> 67>",
                    "<==? <GETB ,TBL 3> 89>");
        }

        [TestMethod]
        public async Task TABLE_With_Words_Can_Be_Overwritten_With_Bytes()
        {
            // this also doesn't change the length of the table
            await AssertGlobals(
                "<SETG MY-TBL <TABLE 12345 6789>>",
                "<PUTB ,MY-TBL 0 123>",
                "<PUTB ,MY-TBL 1 45>",
                "<GLOBAL TBL ,MY-TBL>")
                .ImpliesAsync(
                    "<==? <GETB ,TBL 0> 123>",
                    "<==? <GETB ,TBL 1> 45>",
                    "<==? <GET ,TBL 1> 6789>");
        }

        [TestMethod]
        public async Task Round_Tripping_Table_Elements_Between_Bytes_And_Words_Preserves_Widths()
        {
            await AssertGlobals(
                "<SETG MY-TBL <LTABLE 1 2 3>>",
                "<PUTB ,MY-TBL 2 100>",
                "<ZPUT ,MY-TBL 1 1>",
                "<GLOBAL TBL ,MY-TBL>")
                .ImpliesAsync(
                    "<==? <GET ,TBL 0> 3>",
                    "<==? <GET ,TBL 1> 1>",
                    "<==? <GET ,TBL 2> 2>",
                    "<==? <GET ,TBL 3> 3>");

            await AssertGlobals(
              "<SETG MY-TBL <LTABLE (BYTE) 1 2 3>>",
              "<ZPUT ,MY-TBL 1 2>",
              "<PUTB ,MY-TBL 2 2>",
              "<PUTB ,MY-TBL 3 3>",
              "<GLOBAL TBL ,MY-TBL>")
              .ImpliesAsync(
                  "<==? <GETB ,TBL 0> 3>",
                  "<==? <GETB ,TBL 1> 1>",
                  "<==? <GETB ,TBL 2> 2>",
                  "<==? <GETB ,TBL 3> 3>");
        }

        [TestMethod]
        public async Task PARSER_TABLEs_Come_Before_Other_Pure_Tables()
        {
            await AssertGlobals(
                "<CONSTANT PURE-TBL <TABLE (PURE) 1 2 3>>",
                "<CONSTANT PARSER-TBL <TABLE (PARSER-TABLE) 1 2 3>>",
                "<CONSTANT IMPURE-TBL <TABLE 1 2 3>>")
                .ImpliesAsync(
                    "<L? ,IMPURE-TBL ,PARSER-TBL>",
                    "<L=? <LOWCORE PURBOT> ,PARSER-TBL>",
                    "<L? ,PARSER-TBL ,PURE-TBL>");
        }

        [TestMethod]
        public async Task PARSER_TABLEs_Start_At_PRSTBL()
        {
            await AssertGlobals(
                "<CONSTANT PARSER-TBL <TABLE (PARSER-TABLE) 1 2 3>>")
                .ImpliesAsync(
                    "<=? ,PARSER-TBL ,PRSTBL>");
        }

        [TestMethod]
        public async Task ZREST_Creates_A_Compile_Time_Offset_Table()
        {
            await AssertGlobals(
                "<SETG MY-TBL <TABLE 100 200 300>>",
                "<GLOBAL TBL ,MY-TBL>",
                "<SETG RESTED <ZREST ,MY-TBL 2>>",
                "<CONSTANT RESTED-OLD-0 <ZGET ,RESTED 0>>",
                "<ZPUT ,RESTED 1 345>")
                .ImpliesAsync(
                    "<=? ,RESTED-OLD-0 200>",
                    "<=? <GET ,TBL 2> 345>");
        }

        [TestMethod]
        public async Task ZREST_Works_With_2OP_Instruction()
        {
            await AssertGlobals(
                "<CONSTANT PADDING-TBL <ITABLE 500>>",
                "<CONSTANT TBL <TABLE 100 200 300>>")
                .ImpliesAsync(
                    "<=? <GET <ZREST ,TBL 4> 0> 300>");
        }

        [TestMethod]
        public async Task Table_With_Length_Prefix_Should_Warn_If_Overflowing()
        {
            await AssertGlobals(
                "<CONSTANT FIELD <ITABLE BYTE 2500>>")
                .WithWarnings("MDL0430")
                .CompilesAsync();

            await AssertGlobals(
                "<CONSTANT FIELD <ITABLE WORD 70000>>")
                .WithWarnings("MDL0430")
                .CompilesAsync();

            const string SOver256 =
                "This string is longer than two hundred and fifty-six characters. " +
                "This string is longer than two hundred and fifty-six characters. " +
                "This string is longer than two hundred and fifty-six characters. " +
                "This string is longer than two hundred and fifty-six characters. " +
                "This string is longer than two hundred and fifty-six characters. " +
                "This string is longer than two hundred and fifty-six characters. ";

            await AssertGlobals(
                @$"<CONSTANT FIELD <TABLE (STRING LENGTH) ""{SOver256}"">>")
                .WithWarnings("MDL0430")
                .CompilesAsync();
        }

        [TestMethod]
        public async Task Table_Defined_In_Routine_Cannot_Reference_Locals()
        {
            await AssertRoutine(
                "\"AUX\" (X 123) Y",
                "<SET Y <LTABLE .X <* .X 2>>>")
                .DoesNotCompileAsync();
        }

        [TestMethod]
        public async Task Table_Defined_In_Routine_Can_Be_Initialized_With_Macro()
        {
            await AssertRoutine(
                "\"AUX\" (X 123) Y",
                "<SET Y <LTABLE <MYMACRO 123>>>")
                .WithGlobal("<DEFMAC MYMACRO (X) <FORM * .X 10>>")
                .CompilesAsync();
        }

        [TestMethod]
        public async Task Table_Defined_In_Routine_Can_Be_Initialized_With_Segment()
        {
            await AssertRoutine(
                "\"AUX\" X",
                "<SET X <LTABLE !,VALS>>")
                .WithGlobal("<SETG VALS '(1 2 3)>")
                .CompilesAsync();
        }

        [TestMethod]
        public async Task Table_Defined_In_Routine_Can_Be_Initialized_With_Splice()
        {
            await AssertRoutine(
                "\"AUX\" X",
                "<SET X <LTABLE <MYMACRO>>>")
                .WithGlobal("<DEFMAC MYMACRO () #SPLICE (1 2 3)>")
                .CompilesAsync();
        }

        [TestMethod]
        public async Task Table_Defined_At_Top_Level_Can_Be_Initialized_With_Macro()
        {
            await AssertRoutine(
                "",
                ",FOO")
                .WithGlobal("<DEFMAC MYMACRO (X) <FORM * .X 10>>")
                .WithGlobal("<GLOBAL FOO <LTABLE <MYMACRO 123>>>")
                .CompilesAsync();
        }
    }
}
