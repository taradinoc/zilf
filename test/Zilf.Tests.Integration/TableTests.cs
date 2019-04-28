/* Copyright 2010-2018 Jesse McGrew
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

namespace Zilf.Tests.Integration
{
    [TestClass, TestCategory("Compiler")]
    public class TableTests : IntegrationTestClass
    {
        [TestMethod]
        public void BYTE_Elements_Should_Compile_As_Bytes()
        {
            AssertGlobals(
                "<GLOBAL TBL <TABLE 12345 #BYTE 123 #BYTE 45>>")
                .Implies(
                    "<==? <GET ,TBL 0> 12345>",
                    "<==? <GETB ,TBL 2> 123>",
                    "<==? <GETB ,TBL 3> 45>");
        }

        [TestMethod]
        public void WORD_Elements_Should_Compile_As_Words()
        {
            AssertGlobals(
                "<GLOBAL TBL <TABLE (BYTE) #WORD (12345) 123 45>>")
                .Implies(
                    "<==? <GET ,TBL 0> 12345>",
                    "<==? <GETB ,TBL 2> 123>",
                    "<==? <GETB ,TBL 3> 45>");
        }

        [TestMethod]
        public void ITABLE_Multi_Element_Initializers_Should_Repeat_N_Times()
        {
            AssertGlobals(
                "<GLOBAL TBL1 <ITABLE 2 1 2 3>>",
                "<GLOBAL TBL2 <ITABLE 3 9 8 7 6>>")
                .Implies(
                    "<==? <GET ,TBL1 0> 1>",
                    "<==? <GET ,TBL1 1> 2>",
                    "<==? <GET ,TBL1 2> 3>",
                    "<==? <GET ,TBL1 3> 1>",
                    "<==? <GET ,TBL1 4> 2>",
                    "<==? <GET ,TBL1 5> 3>");
        }

        [TestMethod]
        public void TABLE_PATTERN_Should_Affect_Element_Sizes()
        {
            AssertGlobals(
                "<GLOBAL TBL <TABLE (PATTERN (BYTE WORD BYTE BYTE [REST WORD])) 1 2 3 4 5 6>>")
                .Implies(
                    "<==? <GETB ,TBL 0> 1>",
                    "<==? <GET <REST ,TBL 1> 0> 2>",
                    "<==? <GETB ,TBL 3> 3>",
                    "<==? <GETB ,TBL 4> 4>",
                    "<==? <GET <REST ,TBL 5> 0> 5>",
                    "<==? <GET <REST ,TBL 5> 1> 6>");
        }

        [TestMethod]
        public void PURE_ITABLE_Should_Be_In_Pure_Memory()
        {
            AssertGlobals(
                "<GLOBAL TBL <ITABLE 10 (PURE)>>")
                .Implies(
                    "<G=? ,TBL <LOWCORE PURBOT>>");
        }

        [TestMethod]
        public void TABLE_Should_Be_Mutable_At_Compile_Time()
        {
            AssertGlobals(
                "<SETG MY-TBL <TABLE 0 <BYTE 0>>>",
                "<ZPUT ,MY-TBL 0 1>",
                "<PUTB ,MY-TBL 2 2>",
                "<GLOBAL TBL ,MY-TBL>")
                .Implies(
                    "<==? <GET ,TBL 0> 1>",
                    "<==? <GETB ,TBL 2> 2>");

            AssertGlobals(
                "<SETG MY-TBL <ITABLE 3 <>>>",
                "<ZPUT ,MY-TBL 1 1>",
                "<GLOBAL TBL ,MY-TBL>")
                .Implies(
                    "<==? <GET ,TBL 1> 1>");
        }

        [TestMethod]
        public void TABLE_Length_Words_Should_Be_Accessible_At_Compile_Time()
        {
            AssertGlobals(
                "<SETG MY-TBL <LTABLE 100 200 300 400>>",
                "<GLOBAL ORIG-LENGTH <ZGET ,MY-TBL 0>>",
                "<ZPUT ,MY-TBL 0 -1>",
                "<GLOBAL TBL ,MY-TBL>")
                .Implies(
                    "<==? ,ORIG-LENGTH 4>",
                    "<==? <GET ,TBL 0> -1>",
                    "<==? <GET ,TBL 4> 400>");
        }

        [TestMethod]
        public void TABLE_With_Adjacent_Bytes_Can_Be_Overwritten_With_Words()
        {
            // this doesn't change the length of the table (in bytes)
            AssertGlobals(
                "<SETG MY-TBL <TABLE (BYTE) 0 0 67 0>>",
                "<ZPUT ,MY-TBL 0 12345>",
                "<PUTB ,MY-TBL 3 89>",
                "<GLOBAL TBL ,MY-TBL>")
                .Implies(
                    "<==? <GET ,TBL 0> 12345>",
                    "<==? <GETB ,TBL 2> 67>",
                    "<==? <GETB ,TBL 3> 89>");
        }

        [TestMethod]
        public void TABLE_With_Words_Can_Be_Overwritten_With_Bytes()
        {
            // this also doesn't change the length of the table
            AssertGlobals(
                "<SETG MY-TBL <TABLE 12345 6789>>",
                "<PUTB ,MY-TBL 0 123>",
                "<PUTB ,MY-TBL 1 45>",
                "<GLOBAL TBL ,MY-TBL>")
                .Implies(
                    "<==? <GETB ,TBL 0> 123>",
                    "<==? <GETB ,TBL 1> 45>",
                    "<==? <GET ,TBL 1> 6789>");
        }

        [TestMethod]
        public void Round_Tripping_Table_Elements_Between_Bytes_And_Words_Preserves_Widths()
        {
            AssertGlobals(
                "<SETG MY-TBL <LTABLE 1 2 3>>",
                "<PUTB ,MY-TBL 2 100>",
                "<ZPUT ,MY-TBL 1 1>",
                "<GLOBAL TBL ,MY-TBL>")
                .Implies(
                    "<==? <GET ,TBL 0> 3>",
                    "<==? <GET ,TBL 1> 1>",
                    "<==? <GET ,TBL 2> 2>",
                    "<==? <GET ,TBL 3> 3>");

            AssertGlobals(
              "<SETG MY-TBL <LTABLE (BYTE) 1 2 3>>",
              "<ZPUT ,MY-TBL 1 2>",
              "<PUTB ,MY-TBL 2 2>",
              "<PUTB ,MY-TBL 3 3>",
              "<GLOBAL TBL ,MY-TBL>")
              .Implies(
                  "<==? <GETB ,TBL 0> 3>",
                  "<==? <GETB ,TBL 1> 1>",
                  "<==? <GETB ,TBL 2> 2>",
                  "<==? <GETB ,TBL 3> 3>");
        }

        [TestMethod]
        public void PARSER_TABLEs_Come_Before_Other_Pure_Tables()
        {
            AssertGlobals(
                "<CONSTANT PURE-TBL <TABLE (PURE) 1 2 3>>",
                "<CONSTANT PARSER-TBL <TABLE (PARSER-TABLE) 1 2 3>>",
                "<CONSTANT IMPURE-TBL <TABLE 1 2 3>>")
                .Implies(
                    "<L? ,IMPURE-TBL ,PARSER-TBL>",
                    "<L=? <LOWCORE PURBOT> ,PARSER-TBL>",
                    "<L? ,PARSER-TBL ,PURE-TBL>");
        }

        [TestMethod]
        public void PARSER_TABLEs_Start_At_PRSTBL()
        {
            AssertGlobals(
                "<CONSTANT PARSER-TBL <TABLE (PARSER-TABLE) 1 2 3>>")
                .Implies(
                    "<=? ,PARSER-TBL ,PRSTBL>");
        }

        [TestMethod]
        public void ZREST_Creates_A_Compile_Time_Offset_Table()
        {
            AssertGlobals(
                "<SETG MY-TBL <TABLE 100 200 300>>",
                "<GLOBAL TBL ,MY-TBL>",
                "<SETG RESTED <ZREST ,MY-TBL 2>>",
                "<CONSTANT RESTED-OLD-0 <ZGET ,RESTED 0>>",
                "<ZPUT ,RESTED 1 345>")
                .Implies(
                    "<=? ,RESTED-OLD-0 200>",
                    "<=? <GET ,TBL 2> 345>");
        }

        [TestMethod]
        public void ZREST_Works_With_2OP_Instruction()
        {
            AssertGlobals(
                "<CONSTANT PADDING-TBL <ITABLE 500>>",
                "<CONSTANT TBL <TABLE 100 200 300>>")
                .Implies(
                    "<=? <GET <ZREST ,TBL 4> 0> 300>");
        }
    }
}
