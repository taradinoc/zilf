using System;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Zilf;
using System.Diagnostics.Contracts;

namespace IntegrationTests
{
    [TestClass]
    public class TellTests
    {
        private static RoutineAssertionHelper AssertRoutine(string argSpec, string body)
        {
            Contract.Requires(argSpec != null);
            Contract.Requires(!string.IsNullOrWhiteSpace(body));

            return new RoutineAssertionHelper(argSpec, body);
        }

        [TestMethod]
        public void Tell_Macro_Should_Be_Used_If_Defined()
        {
            AssertRoutine("", "<TELL 21>")
                .WithGlobal("<DEFMAC TELL ('X) <FORM PRINTN <* .X 2>>>")
                .Outputs("42");
            
        }

        [TestMethod]
        public void Tell_Builtin_Should_Support_Basic_Operations()
        {
            AssertRoutine("", "<TELL \"AB\" C 67 CR N 123 CRLF D ,OBJ>")
                .WithGlobal("<OBJECT OBJ (DESC \"obj\")>")
                .Outputs("ABC\n123\nobj");
        }

        [TestMethod]
        public void Tell_Builtin_Should_Support_New_Tokens()
        {
            AssertRoutine("", "<TELL DBL 21 CR>")
                .WithGlobal(
                    "<TELL-TOKENS " +
                    "  (CR CRLF)  <CRLF>" +
                    "  DBL *      <PRINT-DBL .X>>")
                .WithGlobal("<ROUTINE PRINT-DBL (X) <PRINTN <* 2 .X>>>")
                .Outputs("42\n");
        }

        [TestMethod]
        public void Tell_Builtin_Should_Translate_Strings()
        {
            AssertRoutine("", "<TELL \"foo|bar|\nbaz\nquux\">")
                .Outputs("foo\nbar\nbaz quux");
        }

        [TestMethod]
        public void CRLF_CHARACTER_Should_Affect_String_Translation()
        {
            AssertRoutine("", "<TELL \"foo^bar\">")
                .WithGlobal("<SETG CRLF-CHARACTER !\\^>")
                .Outputs("foo\nbar");
        }
    }
}
