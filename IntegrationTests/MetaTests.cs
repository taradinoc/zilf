/* Copyright 2010-2017 Jesse McGrew
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

extern alias JBA;
using System.Diagnostics.Contracts;
using JBA::JetBrains.Annotations;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace IntegrationTests
{
    [TestClass]
    public class MetaTests
    {
        [NotNull]
        static RoutineAssertionHelper AssertRoutine([NotNull] string argSpec, [NotNull] string body)
        {
            Contract.Requires(argSpec != null);
            Contract.Requires(!string.IsNullOrWhiteSpace(body));
            Contract.Ensures(Contract.Result<RoutineAssertionHelper>() != null);

            return new RoutineAssertionHelper(argSpec, body);
        }

        [NotNull]
        static GlobalsAssertionHelper AssertGlobals([ItemNotNull] [NotNull] params string[] globals)
        {
            Contract.Requires(globals != null);
            Contract.Requires(globals.Length > 0);
            Contract.Requires(Contract.ForAll(globals, c => !string.IsNullOrWhiteSpace(c)));
            Contract.Ensures(Contract.Result<GlobalsAssertionHelper>() != null);

            return new GlobalsAssertionHelper(globals);
        }

        [NotNull]
        static RawAssertionHelper AssertRaw([NotNull] string code)
        {
            Contract.Requires(!string.IsNullOrEmpty(code));
            Contract.Ensures(Contract.Result<RawAssertionHelper>() != null);

            return new RawAssertionHelper(code);
        }

        [TestMethod]
        public void TestIFFLAG()
        {
            AssertRoutine("", "<IFFLAG (FOO 123) (ELSE 456)>")
                .WithGlobal("<COMPILATION-FLAG FOO T>")
                .GivesNumber("123");

            AssertRoutine("", "<IFFLAG (FOO 123) (ELSE 456)>")
                .WithGlobal("<COMPILATION-FLAG FOO <>>")
                .GivesNumber("456");

            AssertRoutine("", "<IFFLAG (\"FOO\" 123) (ELSE 456)>")
                .WithGlobal("<COMPILATION-FLAG FOO <>>")
                .GivesNumber("456");
        }

        [TestMethod]
        public void Property_Names_Are_Shared_Across_Packages()
        {
            AssertGlobals(
                "<DEFINITIONS \"FOO\"> <OBJECT FOO-OBJ (MY-PROP 123)> <END-DEFINITIONS>",
                "<DEFINITIONS \"BAR\"> <OBJECT BAR-OBJ (MY-PROP 456)> <END-DEFINITIONS>",
                "<ROUTINE FOO () <GETP <> ,P?MY-PROP>>")
                .Compiles();
        }

        [TestMethod]
        public void Object_Names_Are_Shared_Across_Packages()
        {
            AssertGlobals(
                "<DEFINITIONS \"FOO\"> <OBJECT FOO-OBJ> <END-DEFINITIONS>",
                "<DEFINITIONS \"BAR\"> <OBJECT BAR-OBJ (LOC FOO-OBJ)> <END-DEFINITIONS>",
                "<ROUTINE FOO () <REMOVE ,FOO-OBJ>>")
                .WithoutWarnings()
                .Compiles();

            AssertGlobals(
                "<SET REDEFINE T>",
                "<DEFINITIONS \"FOO\"> <OBJECT MY-OBJ> <END-DEFINITIONS>",
                "<DEFINITIONS \"BAR\"> <OBJECT MY-OBJ> <END-DEFINITIONS>",
                "<DEFINITIONS \"BAZ\"> <OBJECT MY-OBJ> <END-DEFINITIONS>")
                .WithoutWarnings()
                .Compiles();
        }

        [TestMethod]
        public void Constant_Names_Are_Shared_Across_Packages()
        {
            AssertGlobals(
                "<DEFINITIONS \"FOO\"> <CONSTANT MY-CONST 1> <END-DEFINITIONS>",
                "<DEFINITIONS \"BAR\"> <CONSTANT MY-CONST 1> <END-DEFINITIONS>",
                "<ROUTINE FOO () <PRINT ,MY-CONST>>")
                .Compiles();

            AssertGlobals(
                "<DEFINITIONS \"FOO\"> <CONSTANT MY-CONST 1> <END-DEFINITIONS>",
                "<DEFINITIONS \"BAR\"> <CONSTANT MY-CONST 2> <END-DEFINITIONS>",
                "<ROUTINE FOO () <PRINT ,MY-CONST>>")
                .DoesNotCompile();
        }

        [TestMethod]
        public void Global_Names_Are_Shared_Across_Packages()
        {
            AssertGlobals(
                "<SET REDEFINE T>",
                "<DEFINITIONS \"FOO\"> <GLOBAL MY-GLOBAL <TABLE 1 2 3>> <END-DEFINITIONS>",
                "<DEFINITIONS \"BAR\"> <GLOBAL MY-GLOBAL <TABLE 1 2 3>> <END-DEFINITIONS>",
                "<ROUTINE FOO () <PRINT ,MY-GLOBAL>>")
                .Compiles();

            AssertGlobals(
                "<DEFINITIONS \"FOO\"> <GLOBAL MY-GLOBAL <TABLE 1 2 3>> <END-DEFINITIONS>",
                "<DEFINITIONS \"BAR\"> <GLOBAL MY-GLOBAL <TABLE 1 2 3>> <END-DEFINITIONS>",
                "<ROUTINE FOO () <PRINT ,MY-GLOBAL>>")
                .DoesNotCompile();
        }

        [TestMethod]
        public void Routine_Names_Are_Shared_Across_Packages()
        {
            AssertGlobals(
                "<DEFINITIONS \"FOO\"> <ROUTINE FOO () <BAR>> <END-DEFINITIONS>",
                "<ROUTINE BAR () <FOO>>")
                .Compiles();

            AssertGlobals(
                "<SET REDEFINE T>",
                "<DEFINITIONS \"FOO\"> <ROUTINE FOO () <BAR>> <END-DEFINITIONS>",
                "<DEFINITIONS \"BAR\"> <ROUTINE FOO () <BAR>> <END-DEFINITIONS>",
                "<ROUTINE BAR () <FOO>>")
                .Compiles();

            AssertGlobals(
                "<SET REDEFINE T>",
                "<DEFINITIONS \"FOO\"> <ROUTINE FOO () <BAR>> <END-DEFINITIONS>",
                "<DEFINITIONS \"BAR\"> <ROUTINE FOO () <BAR>> <END-DEFINITIONS>",
                "<DEFINITIONS \"BAZ\"> <ROUTINE FOO () <BAR>> <END-DEFINITIONS>",
                "<ROUTINE BAR () <FOO>>")
                .Compiles();

            AssertGlobals(
                "<SET REDEFINE T>",
                "<DEFINITIONS \"FOO\"> <ROUTINE FOO () <BAR>> <END-DEFINITIONS>",
                "<DEFINITIONS \"BAR\"> <ROUTINE FOO () <BAR>> <END-DEFINITIONS>",
                "<DEFINITIONS \"BAZ\"> <ROUTINE FOO () <BAR>> <END-DEFINITIONS>",
                "<DEFINITIONS \"QUUX\"> <ROUTINE FOO () <BAR>> <END-DEFINITIONS>",
                "<ROUTINE BAR () <FOO>>")
                .Compiles();

            AssertGlobals(
                "<DEFINITIONS \"FOO\"> <ROUTINE FOO () <BAR>> <END-DEFINITIONS>",
                "<DEFINITIONS \"BAR\"> <ROUTINE FOO () <BAR>> <END-DEFINITIONS>",
                "<ROUTINE BAR () <FOO>>")
                .DoesNotCompile();
        }

        [TestMethod]
        public void IN_ZILCH_Indicates_What_Macro_Expansions_Will_Be_Used_For()
        {
            AssertRoutine("", "<HELLO \"Z-machine\">")
                .WithGlobal(@"
<DEFMAC HELLO (WHENCE)
    <FORM BIND '()
        <FORM <IFFLAG (IN-ZILCH PRINTI) (T PRINC)>
              <STRING ""Hello from "" .WHENCE>>
        <FORM CRLF>>>")
                .WithGlobal("<HELLO \"MDL\">")
                .CapturingCompileOutput()
                .Outputs("Hello from MDL\nHello from Z-machine\n");
        }

        [TestMethod]
        public void ROUTINE_REWRITER_Can_Rewrite_Routines()
        {
            const string SMyRewriter = @"
<DEFINE MY-REWRITER (NAME ARGS BODY)
    <COND (<N==? .NAME GO>
           <SET BODY
              (<FORM TELL ""Arg: "" <FORM LVAL <1 .ARGS>> CR>
               <FORM BIND ((RES <FORM PROG '() !.BODY>)) <FORM TELL ""Return: "" N '.RES CR> '.RES>)>
           <LIST .ARGS !.BODY>)>>";

            AssertRoutine("NAME", "<TELL \"Hello, \" .NAME \".\" CR>")
                .WithGlobal(SMyRewriter)
                .WithGlobal("<SETG REWRITE-ROUTINE!-HOOKS!-ZILF ,MY-REWRITER>")
                .WhenCalledWith("\"world\"")
                .Outputs("Arg: world\nHello, world.\nReturn: 1\n");

            // TODO: make sure rewritten routine has the same routine flags as the original
        }

        [TestMethod]
        public void PRE_COMPILE_Hook_Can_Add_To_Compilation_Environment()
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

            AssertRoutine("", "<LIST-ROUTINES>")
                .WithGlobal(SMyHook)
                .WithGlobal("<SETG PRE-COMPILE!-HOOKS!-ZILF ,MY-PRE-COMPILE>")
                .Outputs("GO\nTEST?ROUTINE\n");
        }

        [DataTestMethod]
        [DataRow(3), DataRow(4), DataRow(5), DataRow(6), DataRow(7), DataRow(8)]
        public void RELEASEID_Is_Optional(int zversion)
        {
            string code =
                $"<VERSION {zversion}>\n" +
                "<ROUTINE GO () <PRINTN <GET 2 0>> <CRLF> <QUIT>>";

            AssertRaw(code).Outputs("0\n");
        }

        [TestMethod]
        public void Compilation_Stops_After_100_Errors()
        {
            var builder = AssertRoutine("", "T");

            for (var i = 1; i <= 150; i++)
            {
                builder = builder.WithGlobal($"<ROUTINE DUMMY-{i} () <THIS-IS-INVALID>>");
            }

            builder.DoesNotCompile(r => r.ErrorCount == 101);
        }

    }
}
