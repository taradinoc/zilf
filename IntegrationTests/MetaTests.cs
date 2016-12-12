using System;
using System.Diagnostics.Contracts;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace IntegrationTests
{
    [TestClass]
    public class MetaTests
    {
        static RoutineAssertionHelper AssertRoutine(string argSpec, string body)
        {
            Contract.Requires(argSpec != null);
            Contract.Requires(!string.IsNullOrWhiteSpace(body));

            return new RoutineAssertionHelper(argSpec, body);
        }

        static GlobalsAssertionHelper AssertGlobals(params string[] globals)
        {
            Contract.Requires(globals != null && globals.Length > 0);
            Contract.Requires(Contract.ForAll(globals, c => !string.IsNullOrWhiteSpace(c)));

            return new GlobalsAssertionHelper(globals);
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
                .WithGlobal("<PUTPROP ROUTINE REWRITER ,MY-REWRITER>")
                .WhenCalledWith("\"world\"")
                .Outputs("Arg: world\nHello, world.\nReturn: 1\n");

            // TODO: make sure rewritten routine has the same routine flags as the original
        }
    }
}
