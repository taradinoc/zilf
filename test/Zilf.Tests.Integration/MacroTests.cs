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
    [TestClass, TestCategory("Compiler"), TestCategory("Macros")]
    public class MacroTests : IntegrationTestClass
    {
        [TestMethod]
        public async Task SPLICEs_Should_Work_Inside_Routines()
        {
            // void context
            await AssertRoutine("", "<VARIOUS-THINGS> T")
                .WithGlobal("<DEFMAC VARIOUS-THINGS () <CHTYPE '(<TELL \"hello\"> <TELL CR> <TELL \"world\">) SPLICE>>")
                .OutputsAsync("hello\nworld");

            // value context
            await AssertRoutine("", "<VARIOUS-THINGS>")
                .WithGlobal("<DEFMAC VARIOUS-THINGS () <CHTYPE '(123 456) SPLICE>>")
                .GivesNumberAsync("456");

            // as builtin arguments
            await AssertRoutine("", "<+ <VARIOUS-THINGS>>")
                .WithGlobal("<DEFMAC VARIOUS-THINGS () <CHTYPE '(123 456) SPLICE>>")
                .GivesNumberAsync("579");

            await AssertRoutine("", "<TELL <VARIOUS-THINGS>>")
                .WithGlobal("<DEFMAC VARIOUS-THINGS () <CHTYPE '(N 12345) SPLICE>>")
                .OutputsAsync("12345");

            // as routine arguments
            await AssertRoutine("", "<ADD-EM <VARIOUS-THINGS>>")
                .WithGlobal("<DEFMAC VARIOUS-THINGS () <CHTYPE '(123 456) SPLICE>>")
                .WithGlobal("<ROUTINE ADD-EM (X Y) <+ .X .Y>>")
                .GivesNumberAsync("579");
        }

        [TestMethod]
        public async Task Macro_Call_With_Wrong_Argument_Count_Should_Raise_An_Error()
        {
            await AssertRoutine("\"AUX\" S", "<SET S <FOO A>>")
                .WithGlobal("<DEFMAC FOO ('X 'Y 'Z) <FORM TELL \"hello world\" CR>>")
                .DoesNotCompileAsync();
        }

        [TestMethod]
        public async Task Macros_Can_Define_Globals_Inside_Routines()
        {
            await AssertRoutine("", "<PRINTN <MAKE-GLOBAL 123>>")
                .WithGlobal("<DEFMAC MAKE-GLOBAL (N) <EVAL <FORM GLOBAL NEW-GLOBAL .N>> ',NEW-GLOBAL>")
                .OutputsAsync("123");
        }

        [TestMethod]
        public async Task Macros_Can_Be_Used_In_Local_Initializers()
        {
            await AssertRoutine("\"AUX\" (X <MY-VALUE>)", ".X")
                .WithGlobal("<DEFMAC MY-VALUE () 123>")
                .GivesNumberAsync("123");
        }

        [TestMethod]
        public async Task Macros_Returning_Constants_Can_Be_Used_As_Literal_Arguments()
        {
            await AssertExpr(@"<PRINTI <FOO>> <CRLF>")
                .WithGlobal(@"<DEFMAC FOO () ""hello world"">")
                .OutputsAsync("hello world\n");

            await AssertRoutine("", @"<LOWCORE-TABLE ZVERSION <FOO> PRINTN>")
                .WithGlobal(@"<DEFMAC FOO () 2>")
                .CompilesAsync();
        }

        [TestMethod, TestCategory("Reader Macros")]
        public async Task MAKE_PREFIX_MACRO_Should_Work()
        {
            await AssertExpr(@"<TELL B @HELLO "" "" B @WORLD CR>")
                .WithGlobal(@"<USE ""READER-MACROS"">")
                .WithGlobal(@"<MAKE-PREFIX-MACRO !\@ <FUNCTION (W:ATOM) <VOC <SPNAME .W> BUZZ>>>")
                .OutputsAsync("hello world\n");
        }
    }
}
