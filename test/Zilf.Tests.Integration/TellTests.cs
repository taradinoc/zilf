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

using System.Threading.Tasks;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Zilf.Tests.Integration
{
    [TestClass, TestCategory("Compiler")]
    public class TellTests : IntegrationTestClass
    {
        [TestMethod]
        public async Task Tell_Macro_Should_Be_Used_If_Defined()
        {
            await AssertRoutine("", "<TELL 21>")
                .WithGlobal("<DEFMAC TELL ('X) <FORM PRINTN <* .X 2>>>")
                .OutputsAsync("42");
            
        }

        [TestMethod]
        public async Task Tell_Builtin_Should_Support_Basic_Operations()
        {
            await AssertRoutine("", "<TELL \"AB\" C 67 CR N 123 CRLF D ,OBJ>")
                .WithGlobal("<OBJECT OBJ (DESC \"obj\")>")
                .OutputsAsync("ABC\n123\nobj");
        }

        [TestMethod]
        public async Task Tell_Builtin_Should_Support_New_Tokens()
        {
            const string STokens = @"
<TELL-TOKENS
    (CR CRLF)        <CRLF>
    DBL *            <PRINT-DBL .X>
    DBL0             <PRINT-DBL <>>
    WUTEVA *:STRING  <PRINTI .X>
    WUTEVA *:FIX     <PRINTN .X>
    GLOB             <PRINTN ,GLOB>
    MAC1             <PRINT-MAC-1>
    MAC2             <PRINT-MAC-2>>

<ROUTINE PRINT-DBL (X) <PRINTN <* 2 .X>>>
<GLOBAL GLOB 123>
<DEFMAC PRINT-MAC-1 () '<PRINT ""macro"">>
<DEFMAC PRINT-MAC-2 () #SPLICE (<PRINT ""mac""> <PRINT ""ro"">)>";

            await AssertRoutine("", @"<TELL DBL 21 CRLF>")
                .WithGlobal(STokens)
                .OutputsAsync("42\n");

            await AssertRoutine("", @"<TELL DBL0>")
                .WithGlobal(STokens)
                .OutputsAsync("0");

            await AssertRoutine("", @"<TELL WUTEVA ""hello"">")
                .WithGlobal(STokens)
                .OutputsAsync("hello");

            await AssertRoutine("", @"<TELL GLOB WUTEVA 45 CR>")
                .WithGlobal(STokens)
                .OutputsAsync("12345\n");

            await AssertRoutine("", @"<TELL MAC1 MAC2>")
                .WithGlobal(STokens)
                .OutputsAsync("macromacro");
        }

        [TestMethod]
        public async Task Tell_Builtin_Should_Reject_Complex_Outputs()
        {
            await AssertRoutine("", "<>")
                .WithGlobal("<TELL-TOKENS DBL * <PRINTN <* 2 .X>>>")
                .DoesNotCompileAsync();
        }

        [TestMethod]
        public async Task Tell_Builtin_Should_Reject_Mismatched_Captures()
        {
            await AssertRoutine("", "<>")
                .WithGlobal("<TELL-TOKENS DBL * <PRINT-DBL>>")
                .DoesNotCompileAsync();

            await AssertRoutine("", "<>")
                .WithGlobal("<TELL-TOKENS DBL * <PRINT-DBL .X .Y>>")
                .DoesNotCompileAsync();
        }

        [TestMethod]
        public async Task Tell_Builtin_Should_Translate_Strings()
        {
            await AssertRoutine("", "<TELL \"foo|bar|\nbaz\nquux\">")
                .OutputsAsync("foo\nbar\nbaz quux");
        }

        [TestMethod]
        public async Task Tell_Builtin_Should_Support_Characters()
        {
            await AssertRoutine("", @"<TELL !\A !\B !\C>")
                .OutputsAsync("ABC");
        }

        [TestMethod]
        public async Task CR_In_String_Should_Be_Ignored()
        {
            await AssertRoutine("", "<TELL \"First line.\r\nSecond line.\r\nLast line.\">")
                .OutputsAsync("First line. Second line. Last line.");
        }

        [TestMethod]
        public async Task CRLF_CHARACTER_Should_Affect_String_Translation()
        {
            await AssertRoutine("", "<TELL \"foo^bar\">")
                .WithGlobal("<SETG CRLF-CHARACTER !\\^>")
                .OutputsAsync("foo\nbar");
        }

        [TestMethod]
        public async Task Two_Spaces_After_Period_Should_Collapse_By_Default()
        {
            await AssertRoutine("", "<TELL \"Hi.  Hi.   Hi.|  Hi!  Hi?  \" CR>")
                .OutputsAsync("Hi. Hi.  Hi.\n Hi!  Hi?  \n");
        }

        [TestMethod]
        public async Task Two_Spaces_After_Period_Should_Not_Collapse_With_PRESERVE_SPACES()
        {
            await AssertRoutine("", "<TELL \"Hi.  Hi.   Hi.|  Hi!  Hi?  \" CR>")
                .WithGlobal("<SETG PRESERVE-SPACES? T>")
                .OutputsAsync("Hi.  Hi.   Hi.\n  Hi!  Hi?  \n");
        }

        [TestMethod]
        public async Task Two_Spaces_After_Period_Bang_Or_Question_Should_Become_Sentence_Space_With_SENTENCE_ENDS()
        {
            // Note: a space followed by embedded newline will produce two spaces instead of collapsing.
            await AssertRoutine("", "<TELL \"Hi.  Hi.   Hi.|  Hi!  Hi?  Hi. \nHi.\" CR>")
                .InV6()
                .WithGlobal("<FILE-FLAGS SENTENCE-ENDS?>")
                .OutputsAsync("Hi.\u000bHi.\u000b Hi.\n  Hi!\u000bHi?\u000bHi.  Hi.\n");
        }

        [TestMethod]
        public async Task Unprintable_Characters_In_Strings_Should_Warn()
        {
            const string SCodeWithTab = "<TELL \"foo\tbar\" CR>";
            const string SCodeWithBackspace = "<TELL \"foo\x0008bar\" CR>";
            const string SCodeWithCtrlZ = "<TELL \"foo\x001abar\" CR>";

            // tab is legal in V6...
            await AssertRoutine("", SCodeWithTab)
                .InV6()
                .WithoutWarnings()
                .CompilesAsync();

            // ...but not in V5
            await AssertRoutine("", SCodeWithTab)
                .InV5()
                .WithWarnings("ZIL0410")
                .CompilesAsync();

            // backspace is never legal
            await AssertRoutine("", SCodeWithBackspace)
                .WithWarnings("ZIL0410")
                .CompilesAsync();

            // nor is ^Z
            await AssertRoutine("", SCodeWithCtrlZ)
                .WithWarnings("ZIL0410")
                .CompilesAsync();
        }


        [TestMethod]
        public async Task CHRSET_Should_Affect_Text_Decoding()
        {
            /*     1         2         3 
             * 67890123456789012345678901
             * zyxwvutsrqponmlkjihgfedcba
             * 
             *   z=6   i=23  l=20
             * 1 00110 10111 10100
             */
            await AssertRoutine("", @"<PRINTB ,MYTEXT>")
                .WithGlobal(@"<CHRSET 0 ""zyxwvutsrqponmlkjihgfedcba"">")
                .WithGlobal(@"<CONSTANT MYTEXT <TABLE #2 1001101011110100>>")
                .InV5()
                .OutputsAsync("zil");
        }

        [TestMethod]
        public async Task CHRSET_Should_Affect_Text_Encoding()
        {
            await AssertRoutine("",
                    @"<PRINT ,MYTEXT> <CRLF> " +
                    @"<PRINTN <- <GET <* 4 ,MYTEXT> 0> ,ENCODED-TEXT>>")
                .WithGlobal(@"<CHRSET 0 ""zyxwvutsrqponmlkjihgfedcba"">")
                .WithGlobal(@"<CONSTANT MYTEXT ""zil"">")
                .WithGlobal(@"<CONSTANT ENCODED-TEXT #2 1001101011110100>")
                .InV5()
                .OutputsAsync("zil\n0");
        }

        [TestMethod]
        public async Task LANGUAGE_Should_Affect_Text_Encoding()
        {
            await AssertRoutine("",
                    @"<TELL ""%>M%obeltr%agerf%u%se%<"">")
                .WithGlobal(@"<LANGUAGE GERMAN>")
                .InV5()
                .OutputsAsync(@"»Möbelträgerfüße«");
        }

        [TestMethod]
        public async Task LANGUAGE_Should_Affect_Vocabulary_Encoding()
        {
            await AssertRoutine("", @"<PRINTB ,W?\%A\%S>")
                .WithGlobal(@"<LANGUAGE GERMAN>")
                .WithGlobal(@"<OBJECT FOO (SYNONYM \%A\%S)>")
                .InV5()
                .OutputsAsync(@"äß");
        }
    }
}
