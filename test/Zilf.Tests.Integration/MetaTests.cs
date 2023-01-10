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
using Zilf.Diagnostics;

namespace Zilf.Tests.Integration
{
    [TestClass, TestCategory("Compiler")]
    public class MetaTests : IntegrationTestClass
    {
        [TestMethod]
        public async Task TestIFFLAG()
        {
            await AssertRoutine("", "<IFFLAG (FOO 123) (ELSE 456)>")
                .WithGlobal("<COMPILATION-FLAG FOO T>")
                .GivesNumberAsync("123");

            await AssertRoutine("", "<IFFLAG (FOO 123) (ELSE 456)>")
                .WithGlobal("<COMPILATION-FLAG FOO <>>")
                .GivesNumberAsync("456");

            await AssertRoutine("", "<IFFLAG (\"FOO\" 123) (ELSE 456)>")
                .WithGlobal("<COMPILATION-FLAG FOO <>>")
                .GivesNumberAsync("456");
        }

        [TestMethod]
        public async Task Property_Names_Are_Shared_Across_Packages()
        {
            await AssertGlobals(
                "<DEFINITIONS \"FOO\"> <OBJECT FOO-OBJ (MY-PROP 123)> <END-DEFINITIONS>",
                "<DEFINITIONS \"BAR\"> <OBJECT BAR-OBJ (MY-PROP 456)> <END-DEFINITIONS>",
                "<ROUTINE FOO () <GETP <> ,P?MY-PROP>>")
                .CompilesAsync();
        }

        [TestMethod]
        public async Task Object_Names_Are_Shared_Across_Packages()
        {
            await AssertGlobals(
                "<DEFINITIONS \"FOO\"> <OBJECT FOO-OBJ> <END-DEFINITIONS>",
                "<DEFINITIONS \"BAR\"> <OBJECT BAR-OBJ (LOC FOO-OBJ)> <END-DEFINITIONS>",
                "<ROUTINE FOO () <REMOVE ,FOO-OBJ>>")
                .WithoutWarnings()
                .CompilesAsync();

            await AssertGlobals(
                "<SET REDEFINE T>",
                "<DEFINITIONS \"FOO\"> <OBJECT MY-OBJ> <END-DEFINITIONS>",
                "<DEFINITIONS \"BAR\"> <OBJECT MY-OBJ> <END-DEFINITIONS>",
                "<DEFINITIONS \"BAZ\"> <OBJECT MY-OBJ> <END-DEFINITIONS>")
                .WithoutWarnings()
                .CompilesAsync();
        }

        [TestMethod]
        public async Task Constant_Names_Are_Shared_Across_Packages()
        {
            await AssertGlobals(
                "<DEFINITIONS \"FOO\"> <CONSTANT MY-CONST 1> <END-DEFINITIONS>",
                "<DEFINITIONS \"BAR\"> <CONSTANT MY-CONST 1> <END-DEFINITIONS>",
                "<ROUTINE FOO () <PRINT ,MY-CONST>>")
                .CompilesAsync();

            await AssertGlobals(
                "<DEFINITIONS \"FOO\"> <CONSTANT MY-CONST 1> <END-DEFINITIONS>",
                "<DEFINITIONS \"BAR\"> <CONSTANT MY-CONST 2> <END-DEFINITIONS>",
                "<ROUTINE FOO () <PRINT ,MY-CONST>>")
                .DoesNotCompileAsync();
        }

        [TestMethod]
        public async Task Global_Names_Are_Shared_Across_Packages()
        {
            await AssertGlobals(
                "<SET REDEFINE T>",
                "<DEFINITIONS \"FOO\"> <GLOBAL MY-GLOBAL <TABLE 1 2 3>> <END-DEFINITIONS>",
                "<DEFINITIONS \"BAR\"> <GLOBAL MY-GLOBAL <TABLE 1 2 3>> <END-DEFINITIONS>",
                "<ROUTINE FOO () <PRINT ,MY-GLOBAL>>")
                .CompilesAsync();

            await AssertGlobals(
                "<DEFINITIONS \"FOO\"> <GLOBAL MY-GLOBAL <TABLE 1 2 3>> <END-DEFINITIONS>",
                "<DEFINITIONS \"BAR\"> <GLOBAL MY-GLOBAL <TABLE 1 2 3>> <END-DEFINITIONS>",
                "<ROUTINE FOO () <PRINT ,MY-GLOBAL>>")
                .DoesNotCompileAsync();
        }

        [TestMethod]
        public async Task Routine_Names_Are_Shared_Across_Packages()
        {
            await AssertGlobals(
                "<DEFINITIONS \"FOO\"> <ROUTINE FOO () <BAR>> <END-DEFINITIONS>",
                "<ROUTINE BAR () <FOO>>")
                .CompilesAsync();

            await AssertGlobals(
                "<SET REDEFINE T>",
                "<DEFINITIONS \"FOO\"> <ROUTINE FOO () <BAR>> <END-DEFINITIONS>",
                "<DEFINITIONS \"BAR\"> <ROUTINE FOO () <BAR>> <END-DEFINITIONS>",
                "<ROUTINE BAR () <FOO>>")
                .CompilesAsync();

            await AssertGlobals(
                "<SET REDEFINE T>",
                "<DEFINITIONS \"FOO\"> <ROUTINE FOO () <BAR>> <END-DEFINITIONS>",
                "<DEFINITIONS \"BAR\"> <ROUTINE FOO () <BAR>> <END-DEFINITIONS>",
                "<DEFINITIONS \"BAZ\"> <ROUTINE FOO () <BAR>> <END-DEFINITIONS>",
                "<ROUTINE BAR () <FOO>>")
                .CompilesAsync();

            await AssertGlobals(
                "<SET REDEFINE T>",
                "<DEFINITIONS \"FOO\"> <ROUTINE FOO () <BAR>> <END-DEFINITIONS>",
                "<DEFINITIONS \"BAR\"> <ROUTINE FOO () <BAR>> <END-DEFINITIONS>",
                "<DEFINITIONS \"BAZ\"> <ROUTINE FOO () <BAR>> <END-DEFINITIONS>",
                "<DEFINITIONS \"QUUX\"> <ROUTINE FOO () <BAR>> <END-DEFINITIONS>",
                "<ROUTINE BAR () <FOO>>")
                .CompilesAsync();

            await AssertGlobals(
                "<DEFINITIONS \"FOO\"> <ROUTINE FOO () <BAR>> <END-DEFINITIONS>",
                "<DEFINITIONS \"BAR\"> <ROUTINE FOO () <BAR>> <END-DEFINITIONS>",
                "<ROUTINE BAR () <FOO>>")
                .DoesNotCompileAsync();
        }

        [TestMethod]
        public async Task IN_ZILCH_Indicates_What_Macro_Expansions_Will_Be_Used_For()
        {
            await AssertRoutine("", "<HELLO \"Z-machine\">")
                .WithGlobal(@"
<DEFMAC HELLO (WHENCE)
    <FORM BIND '()
        <FORM <IFFLAG (IN-ZILCH PRINTI) (T PRINC)>
              <STRING ""Hello from "" .WHENCE>>
        <FORM CRLF>>>")
                .WithGlobal("<HELLO \"MDL\">")
                .CapturingCompileOutput()
                .OutputsAsync("Hello from MDL\nHello from Z-machine\n");
        }

        [TestMethod]
        public async Task ROUTINE_REWRITER_Can_Rewrite_Routines()
        {
            const string SMyRewriter = @"
<DEFINE MY-REWRITER (NAME ARGS BODY)
    <COND (<N==? .NAME GO>
           <SET BODY
              (<FORM TELL ""Arg: "" <FORM LVAL <1 .ARGS>> CR>
               <FORM BIND ((RES <FORM PROG '() !.BODY>)) <FORM TELL ""Return: "" N '.RES CR> '.RES>)>
           <LIST .ARGS !.BODY>)>>";

            await AssertRoutine("NAME", "<TELL \"Hello, \" .NAME \".\" CR>")
                .WithGlobal(SMyRewriter)
                .WithGlobal("<SETG REWRITE-ROUTINE!-HOOKS!-ZILF ,MY-REWRITER>")
                .WhenCalledWith("\"world\"")
                .OutputsAsync("Arg: world\nHello, world.\nReturn: 1\n");

            // TODO: make sure rewritten routine has the same routine flags as the original
        }

        [TestMethod]
        public async Task PRE_COMPILE_Hook_Can_Add_To_Compilation_Environment()
        {
            const string SMyHook = @"
<DEFINE MY-PRE-COMPILE (""AUX"" ROUTINES)
    <SET ROUTINES
        <PROG ((A <ASSOCIATIONS>))
            <MAPF ,VECTOR
                  <FUNCTION (""AUX"" (L <CHTYPE .A LIST>) ITEM IND VAL)
                      <OR .A <MAPSTOP>>
                      <SET ITEM <1 .L>>
	                  <SET IND <2 .L>>
	                  <SET VAL <3 .L>>
                      <SET A <NEXT .A>>
	                  <COND (<AND <TYPE? .ITEM ATOM>
			                      <==? .IND ZVAL>
			                      <TYPE? .VAL ROUTINE>>
	                         .ITEM)
	                        (ELSE <MAPRET>)>>>>>
    <EVAL <FORM ROUTINE LIST-ROUTINES '()
              !<MAPF ,LIST
                     <FUNCTION (A) <FORM TELL <SPNAME .A> CR>>
                     <SORT <> .ROUTINES>>>>>";

            await AssertRoutine("", "<LIST-ROUTINES>")
                .WithGlobal(SMyHook)
                .WithGlobal("<SETG PRE-COMPILE!-HOOKS!-ZILF ,MY-PRE-COMPILE>")
                .OutputsAsync("GO\nTEST?ROUTINE\n");
        }

        [DataTestMethod]
        [DataRow(3), DataRow(4), DataRow(5), DataRow(6), DataRow(7), DataRow(8)]
        public async Task RELEASEID_Is_Optional(int zversion)
        {
            string code =
                $"<VERSION {zversion}>\n" +
                "<ROUTINE GO () <PRINTN <GET 2 0>> <CRLF> <QUIT>>";

            await AssertRaw(code).OutputsAsync("0\n");
        }

        [TestMethod]
        public async Task Compilation_Stops_After_100_Errors()
        {
            var builder = AssertRoutine("", "T");

            for (var i = 1; i <= 150; i++)
            {
                builder = builder.WithGlobal($"<ROUTINE DUMMY-{i} () <THIS-IS-INVALID>>");
            }

            await builder.DoesNotCompileAsync(r => r.ErrorCount == 101);
        }

        [TestMethod]
        public async Task Warnings_Can_Be_Converted_To_Errors()
        {
            await AssertRoutine("", ".X")
                .WithGlobal("<GLOBAL X 5>")
                .WithWarnings()
                .GivesNumberAsync("5");

            await AssertRoutine("", ".X")
                .WithGlobal("<WARN-AS-ERROR? T>")
                .WithGlobal("<GLOBAL X 5>")
                .WithoutWarnings()
                .DoesNotCompileAsync("ZIL0204", // no such {0} variable '{1}', using the {2} instead
                    diag => diag.Severity == Severity.Error);
        }

        [TestMethod]
        public async Task Warnings_Can_Be_Suppressed()
        {
            await AssertRoutine("", ".X")
                .WithGlobal("<GLOBAL X 5>")
                .WithGlobal("<SUPPRESS-WARNINGS? \"ZIL0204\">")
                .WithoutUnsuppressedWarnings()
                .GivesNumberAsync("5");

            await AssertRoutine("", ".X")
                .WithGlobal("<GLOBAL X 5>")
                .WithGlobal("<SUPPRESS-WARNINGS? ALL>")
                .WithoutUnsuppressedWarnings()
                .GivesNumberAsync("5");

            await AssertRoutine("", ".X")
                .WithGlobal("<GLOBAL X 5>")
                .WithGlobal("<SUPPRESS-WARNINGS? \"ZIL0204\">")
                .WithGlobal("<SUPPRESS-WARNINGS? NONE>")
                .WithWarnings("ZIL0204")
                .GivesNumberAsync("5");
        }
    }
}
