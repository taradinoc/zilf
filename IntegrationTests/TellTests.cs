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
            AssertRoutine("", "<TELL DBL 21 CRLF WUTEVA \"hello\" GLOB WUTEVA 45 CR>")
                .WithGlobal(
                    "<TELL-TOKENS " +
                    "  (CR CRLF)        <CRLF>" +
                    "  DBL *            <PRINT-DBL .X>" +
                    "  WUTEVA *:STRING  <PRINTI .X>" +
                    "  WUTEVA *:FIX     <PRINTN .X>" +
                    "  GLOB             <PRINTN ,GLOB>>")
                .WithGlobal("<ROUTINE PRINT-DBL (X) <PRINTN <* 2 .X>>>")
                .WithGlobal("<GLOBAL GLOB 123>")
                .Outputs("42\nhello12345\n");
        }

        [TestMethod]
        public void Tell_Builtin_Should_Reject_Complex_Outputs()
        {
            AssertRoutine("", "<>")
                .WithGlobal("<TELL-TOKENS DBL * <PRINTN <* 2 .X>>>")
                .DoesNotCompile();
        }

        [TestMethod]
        public void Tell_Builtin_Should_Reject_Mismatched_Captures()
        {
            AssertRoutine("", "<>")
                .WithGlobal("<TELL-TOKENS DBL * <PRINT-DBL>>")
                .DoesNotCompile();

            AssertRoutine("", "<>")
                .WithGlobal("<TELL-TOKENS DBL * <PRINT-DBL .X .Y>>")
                .DoesNotCompile();
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

        [TestMethod]
        public void Two_Spaces_After_Period_Should_Collapse_By_Default()
        {
            AssertRoutine("", "<TELL \"Hi.  Hi.   Hi.|  Hi!  Hi?  \" CR>")
                .Outputs("Hi. Hi.  Hi.\n Hi!  Hi?  \n");
        }

        [TestMethod]
        public void Two_Spaces_After_Period_Should_Not_Collapse_With_PRESERVE_SPACES()
        {
            AssertRoutine("", "<TELL \"Hi.  Hi.   Hi.|  Hi!  Hi?  \" CR>")
                .WithGlobal("<SETG PRESERVE-SPACES? T>")
                .Outputs("Hi.  Hi.   Hi.\n  Hi!  Hi?  \n");
        }
    }
}
