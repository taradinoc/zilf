/* Copyright 2010-2025 Tara McGrew
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
    [TestClass, TestCategory("Compiler"), TestCategory("Glulx16")]
    public class Glulx16Tests : IntegrationTestClass
    {
        [TestMethod]
        public async Task Read_Populates_Readbuf_And_Lexbuf_Like_V3()
        {
            await AssertRoutine("", "<READ ,INBUF ,LEXBUF>")
                .InGlulx16()
                .WithGlobal("<GLOBAL LEXBUF <ITABLE 21 (LEXV) 0 #BYTE 0 #BYTE 0>>")
                .WithGlobal("<GLOBAL INBUF <ITABLE 40 (BYTE LENGTH) 0>>")
                .WithGlobal("<BUZZ FOO BAR>")
                .WithInput("foo bar")
                .ImpliesAsync(
                    // TODO: figure out why character escapes don't work here
                    // "<==? <GETB ,INBUF 1> !\\f>",
                    // "<==? <GETB ,INBUF 7> !\\r>",
                    "<==? <GETB ,INBUF 1> 102>",
                    "<==? <GETB ,INBUF 7> 114>",
                    "<==? <GETB ,INBUF 8> 0>",
                    "<==? <GETB ,LEXBUF 1> 2>",
                    "<==? <GET ,LEXBUF 1> ,W?FOO>",
                    "<==? <GET ,LEXBUF 3> ,W?BAR>");
        }

        [TestMethod]
        public async Task Read_Unknown_Word_Leaves_Zero_Dictionary_Address()
        {
            await AssertRoutine("", "<READ ,INBUF ,LEXBUF>")
                .InGlulx16()
                .WithGlobal("<GLOBAL LEXBUF <ITABLE 21 (LEXV) 0 #BYTE 0 #BYTE 0>>")
                .WithGlobal("<GLOBAL INBUF <ITABLE 40 (BYTE LENGTH) 0>>")
                .WithGlobal("<BUZZ FOO>")
                .WithInput("quux")
                .ImpliesAsync(
                    "<==? <GETB ,LEXBUF 1> 1>",
                    "<==? <GET ,LEXBUF 1> 0>");
        }

        [TestMethod]
        public async Task Read_Stores_Length_And_Offset_With_FourByte_Stride()
        {
            await AssertRoutine("", "<READ ,INBUF ,LEXBUF>")
                .InGlulx16()
                .InV3()
                .WithGlobal("<GLOBAL LEXBUF <ITABLE 21 (LEXV) 0 #BYTE 0 #BYTE 0>>")
                .WithGlobal("<GLOBAL INBUF <ITABLE 40 (BYTE LENGTH) 0>>")
                .WithGlobal("<BUZZ FOO BAR>")
                .WithInput("foo bar")
                .ImpliesAsync(
                    "<==? <GETB ,LEXBUF 1> 2>",
                    "<==? <GETB ,LEXBUF 4> 3>",
                    "<==? <GETB ,LEXBUF 5> 1>",
                    "<==? <GETB ,LEXBUF 8> 3>",
                    "<==? <GETB ,LEXBUF 9> 5>");
        }

        [TestMethod]
        public async Task Constant_Folding_Arithmetic_Truncates_To_16_Bits()
        {
            await AssertRoutine("", "<TELL N <+ 65535 2>>")
                .InGlulx16()
                .OutputsAsync("1");
        }

        [TestMethod]
        public async Task VERSION_P_Does_Not_Indicate_Glulx()
        {
            await AssertRoutine("", "<TELL N ,WORD-SIZE>")
                .InGlulx16()
                .WithGlobal("<CONSTANT WORD-SIZE <VERSION? (GLULX 4) (T 2)>>")
                .OutputsAsync("2");
        }

        [TestMethod]
        public async Task Word_Tables_Have_Two_Byte_Words()
        {
            await AssertGlobals(
                "<CONSTANT TABLE1 <LTABLE 10 9 8 7>>"
            )
            .InGlulx16()
            .ImpliesAsync(
                "<==? <GETB ,TABLE1 0> 0>",
                "<==? <GETB ,TABLE1 1> 4>"
            );
        }

        [TestMethod]
        public async Task Properties_Can_Be_Read_And_Written()
        {
            const string SObjectDef = @"
                <PROPDEF FOO <> (FOO N:FIX = <BYTE .N>)>
                <OBJECT OBJ (FOO 123) (BAR 456) (BAZ 789 123 456 789)>
            ";

            // read with GETP
            await AssertRoutine("", @"
                <TELL N <GETP ,OBJ ,P?FOO> CR>            ;123
                <TELL N <PTSIZE <GETPT ,OBJ ,P?FOO>> CR>  ;1
                <TELL N <GETP ,OBJ ,P?BAR> CR>            ;456
                <TELL N <PTSIZE <GETPT ,OBJ ,P?BAR>> CR>  ;2
                <TELL N <PTSIZE <GETPT ,OBJ ,P?BAZ>> CR>  ;8
                ")
                .InGlulx16()
                .WithGlobal(SObjectDef)
                .OutputsAsync("123\n1\n456\n2\n8\n");

            // read with GETPT
            await AssertRoutine("", @"
                <TELL N <GETB <GETPT ,OBJ ,P?FOO> 0> CR>            ;123
                <TELL N <GET <GETPT ,OBJ ,P?BAR> 0> CR>             ;456
                <TELL N <GET <GETPT ,OBJ ,P?BAZ> 0> CR>             ;789
                ").WithGlobal(SObjectDef)
                .OutputsAsync("123\n456\n789\n");

            // write with PUTP
            await AssertRoutine("", @"
                <PUTP ,OBJ ,P?FOO 111>
                <TELL N <GETP ,OBJ ,P?FOO> CR>
                <PUTP ,OBJ ,P?BAR 222>
                <TELL N <GETP ,OBJ ,P?BAR> CR>
                ")
                .InGlulx16()
                .WithGlobal(SObjectDef)
                .OutputsAsync("111\n222\n");
        }

        [TestMethod]
        public async Task Direction_Constants_Are_Set_Like_V3()
        {
            await AssertRoutine("", "T")
                .InGlulx16()
                .ImpliesAsync(
                    "<==? ,UEXIT 1>",
                    "<==? ,NEXIT 2>",
                    "<==? ,FEXIT 3>",
                    "<==? ,CEXIT 4>",
                    "<==? ,DEXIT 5>"
                );
        }

        [TestMethod]
        public async Task Routines_Can_Be_Called_Indirectly()
        {
            await AssertRoutine("", "<SETG THE-ROUTINE MY-ROUTINE> <APPLY ,THE-ROUTINE>")
                .WithGlobal("<GLOBAL THE-ROUTINE <>>")
                .WithGlobal(@"<ROUTINE MY-ROUTINE () <TELL ""Boop"">>")
                .OutputsAsync("Boop");
        }
    }
}
