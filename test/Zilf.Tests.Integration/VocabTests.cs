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

using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Zilf.ZModel.Vocab;

namespace Zilf.Tests.Integration
{
    [TestClass, TestCategory("Compiler"), TestCategory("Vocab")]
    public class VocabTests : IntegrationTestClass
    {
        [TestMethod]
        public async Task SIBREAKS_Should_Affect_Lexing()
        {
            await AssertRoutine("",
                "<READ ,INBUF ,LEXBUF> " +
                "<TELL N <GETB ,LEXBUF 0> CR " +
                 "N <GETB ,LEXBUF 1> CR> " +
                 "<PRINTB <GET ,LEXBUF 1>> <CRLF> " +
                 "<PRINTB <GET ,LEXBUF 3>> <CRLF> " +
                 "<PRINTB <GET ,LEXBUF 5>> <CRLF> " +
                 "<PRINTB <GET ,LEXBUF 7>> <CRLF>")
                .WithGlobal("<SETG SIBREAKS \"'\">")
                .WithGlobal("<BUZZ GRANT S TOMB>")
                .WithGlobal("<GLOBAL LEXBUF <ITABLE 59 (LEXV) 0 #BYTE 0 #BYTE 0>>")
                .WithGlobal("<GLOBAL INBUF <ITABLE 80 (BYTE LENGTH) 0>>")
                .WithGlobal("<OBJECT DUMMY (DESC \"wuteva\")>")
                .WithGlobal("<GLOBAL HERE DUMMY> <GLOBAL SCORE 0> <GLOBAL MOVES 0>")
                .InV3()
                .WithInput("grant's tomb")
                .OutputsAsync("59\n4\ngrant\n'\ns\ntomb\n");
        }

        [TestMethod]
        public async Task TCHARS_Should_Affect_Header()
        {
            await AssertGlobals(
                "<CONSTANT F12 144>",
                "<CONSTANT TCHARS <TABLE (BYTE) F12 0>>")
                .InV5()
                .ImpliesAsync(
                    "<==? <LOWCORE TCHARS> ,TCHARS>",
                    "<==? <GETB ,TCHARS 0> 144>");
        }

        static string[] PrepImplications(bool compact, params string[] wordAndIdConstantPairs)
        {
            const string SCompactTest =
                "<==? <GETB <INTBL? {0} <+ ,PREPOSITIONS 2> <GET ,PREPOSITIONS 0> *203*> 2> {1}>";
            const string SNonCompactTest =
                "<==? <GET <INTBL? {0} <+ ,PREPOSITIONS 2> <GET ,PREPOSITIONS 0> *204*> 1> {1}>";

            string testFormat = compact ? SCompactTest : SNonCompactTest;

            var result = new List<string>
            {
                $"<==? <GET ,PREPOSITIONS 0> {wordAndIdConstantPairs.Length / 2}>"
            };

            for (int i = 0; i + 1 < wordAndIdConstantPairs.Length; i += 2)
            {
                var wordConstant = wordAndIdConstantPairs[i];
                var idConstant = wordAndIdConstantPairs[i + 1];

                result.Add(string.Format(testFormat, wordConstant, idConstant));
            }

            return [.. result];
        }

        [TestMethod]
        public async Task PREPOSITIONS_NonCompact_Should_Use_4_Byte_Entries_And_Not_List_Synonyms()
        {
            await AssertGlobals(
                "<ROUTINE V-LOOK () <>>",
                "<ROUTINE V-PICK-UP-WITH () <>>",
                "<SYNTAX LOOK THROUGH OBJECT = V-LOOK>",
                "<PREP-SYNONYM THROUGH THRU>",
                "<SYNTAX PICK UP OBJECT WITH OBJECT = V-PICK-UP-WITH>")
                .InV5()
                .ImpliesAsync(PrepImplications(
                    false,
                    "W?THROUGH", "PR?THROUGH",
                    "W?UP", "PR?UP",
                    "W?WITH", "PR?WITH"));
        }

        [TestMethod]
        public async Task PREPOSITIONS_Compact_Should_Use_3_Byte_Entries_And_List_Synonyms()
        {
            await AssertGlobals(
                "<SETG COMPACT-VOCABULARY? T>",
                "<ROUTINE V-LOOK () <>>",
                "<ROUTINE V-PICK-UP-WITH () <>>",
                "<SYNTAX LOOK THROUGH OBJECT = V-LOOK>",
                "<PREP-SYNONYM THROUGH THRU>",
                "<SYNTAX PICK UP OBJECT WITH OBJECT = V-PICK-UP-WITH>")
                .InV5()
                .ImpliesAsync(PrepImplications(
                    true,
                    "W?THROUGH", "PR?THROUGH",
                    "W?THRU", "PR?THROUGH",
                    "W?UP", "PR?UP",
                    "W?WITH", "PR?WITH"));
        }

        [TestMethod]
        public async Task LONG_WORDS_P_Should_Generate_LONG_WORD_TABLE()
        {
            await AssertGlobals(
                "<LONG-WORDS?>",
                "<OBJECT FOO (SYNONYM HEMIDEMISEMIQUAVER)>")
                .ImpliesAsync(
                    "<==? <GET ,LONG-WORD-TABLE 0> 1>",
                    "<==? <GET ,LONG-WORD-TABLE 1> ,W?HEMIDEMISEMIQUAVER>",
                    "<==? <GET ,LONG-WORD-TABLE 2> \"hemidemisemiquaver\">");
        }

        [TestMethod]
        public async Task LANGUAGE_Should_Affect_Lexing()
        {
            await AssertRoutine("",
                "<READ ,INBUF ,LEXBUF> " +
                @"<==? <GET ,LEXBUF 1> ,W?AU\%SER>")
                .WithGlobal("<LANGUAGE GERMAN>")
                .WithGlobal(@"<BUZZ AU\%SER>")
                .WithGlobal("<GLOBAL LEXBUF <ITABLE 59 (LEXV) 0 #BYTE 0 #BYTE 0>>")
                .WithGlobal("<GLOBAL INBUF <ITABLE 80 (BYTE LENGTH) 0>>")
                .InV5()
                .WithInput("außer")
                .GivesNumberAsync("1");
        }

        [TestMethod]
        public async Task Punctuation_Symbol_Words_Should_Still_Work_When_Given_Definitions()
        {
            await AssertRoutine("",
                @"<TELL B <GETP ,FOO ,P?SYNONYM> %,SPACE B ,W?COMMA %,SPACE B ,W?\,>")
                .WithGlobal("<CONSTANT SPACE <ASCII 32>>")
                .WithGlobal(@"<OBJECT FOO (SYNONYM \,)>")
                .OutputsAsync(", , ,");
        }

        [TestMethod]
        public async Task Punctuation_Name_Words_Should_Split_From_Symbol_Words_When_Given_Definitions()
        {
            await AssertRoutine("",
                @"<TELL B <GETP ,FOO ,P?SYNONYM> %,SPACE B ,W?COMMA %,SPACE B ,W?\,>")
                .WithGlobal("<CONSTANT SPACE <ASCII 32>>")
                .WithGlobal(@"<OBJECT FOO (SYNONYM COMMA)>")
                .OutputsAsync("comma comma ,");
        }

        #region Old Parser

        [TestMethod]
        public async Task VOC_With_2nd_Arg_Atom_Should_Set_PartOfSpeech()
        {
            await AssertRoutine("\"AUX\" (P <GET ,VOC-TABLE 0>)", "<GETB .P 4>")
                .WithGlobal("<GLOBAL VOC-TABLE <PTABLE <VOC \"XYZZY\" ADJ>>>")
                .InV3()
                .GivesNumberAsync(((int)(PartOfSpeech.Adjective | PartOfSpeech.AdjectiveFirst)).ToString());
        }

        [TestMethod]
        public async Task VOC_With_2nd_Arg_False_Should_Not_Set_PartOfSpeech()
        {
            await AssertRoutine("\"AUX\" (P <GET ,VOC-TABLE 0>)", "<GETB .P 4>")
                .WithGlobal("<GLOBAL VOC-TABLE <PTABLE <VOC \"XYZZY\" <>>>>")
                .InV3()
                .GivesNumberAsync(((int)(PartOfSpeech.None)).ToString());
        }

        [TestMethod]
        public async Task VOC_With_2nd_Arg_Missing_Should_Not_Set_PartOfSpeech()
        {
            await AssertRoutine("\"AUX\" (P <GET ,VOC-TABLE 0>)", "<GETB .P 4>")
                .WithGlobal("<GLOBAL VOC-TABLE <PTABLE <VOC \"XYZZY\">>>")
                .InV3()
                .GivesNumberAsync(((int)(PartOfSpeech.None)).ToString());
        }

        [TestMethod]
        public async Task Colliding_Words_Should_Be_Merged()
        {
            await AssertGlobals(
                "<OBJECT FOO (SYNONYM HEMIDEMISEMIQUAVER)>",
                "<OBJECT BAR (SYNONYM HEMIDE)>",
                "<OBJECT BAZ (ADJECTIVE HEMIDEISH SAMPLED)>",
                "<ROUTINE V-SAMPLE () <>>",
                "<SYNTAX SAMPLE = V-SAMPLE>")
                .InV3()
                .WithWarnings("ZIL0310", "ZIL0311")
                .ImpliesAsync(
                    "<==? ,W?HEMIDEMISEMIQUAVER ,W?HEMIDE>",
                    "<==? ,W?HEMIDE ,W?HEMIDEISH>",
                    "<BTST <GETB ,W?HEMIDE 4> ,PS?OBJECT>",
                    "<BTST <GETB ,W?HEMIDE 4> ,PS?ADJECTIVE>",
                    "<==? ,W?SAMPLE ,W?SAMPLED>",
                    "<BTST <GETB ,W?SAMPLE 4> ,PS?VERB>",
                    "<BTST <GETB ,W?SAMPLE 4> ,PS?ADJECTIVE>");

            await AssertGlobals(
                "<OBJECT FOO (SYNONYM LONGWORDEVENINV4A)>",
                "<OBJECT BAR (SYNONYM LONGWORDEVENINV4B)>")
                .InV4()
                .WithWarnings("ZIL0310")
                .ImpliesAsync(
                    "<==? ,W?LONGWORDEVENINV4A ,W?LONGWORDEVENINV4B>");
        }

        [TestMethod]
        public async Task Adjective_Numbers_Of_Colliding_Words_Should_Be_Merged()
        {
            await AssertGlobals(
                "<OBJECT FOO (ADJECTIVE ABCDEFGHIJKL ABCDEF) (FOO 123)>",
                "<DEFINE FOO-PROP (L) <VOC \"ABCDEFGHI\" ADJ> .L>",
                "<PUTPROP FOO PROPSPEC FOO-PROP>")
                .InV3()
                .ImpliesAsync(
                    "<==? ,A?ABCDEFGHIJKL ,A?ABCDEF>",
                    "<==? ,A?ABCDEFGHI ,A?ABCDEF>");
        }

        #endregion

        #region New Parser

        internal const string SNewParserBootstrap = @"
<SETG NEW-PARSER? T>

<SETG CLASSIFICATIONS '(ADJ 1 BUZZ 2 DIR 4 NOUN 8 PREP 16 VERB 32 PARTICLE 64)>

<DEFINE GET-CLASSIFICATION (TYPE ""AUX"" P)
    <COND (<SET P <MEMQ .TYPE ,CLASSIFICATIONS>> <2 .P>)
          (T <ERROR NO-SUCH-WORD-TYPE!-ERRORS>)>>

<SET-DEFSTRUCT-FILE-DEFAULTS ('START-OFFSET 0) ('PUT ZPUT) ('NTH ZGET)>

<DEFSTRUCT VERB-DATA (TABLE ('INIT-ARGS (TEMP-TABLE)))
    (VERB-ZERO ANY -1)
    (VERB-RESERVED FALSE)
    (VERB-ONE <OR FALSE TABLE>)
    (VERB-TWO <OR FALSE TABLE>)>

<DEFSTRUCT VWORD (TABLE ('INIT-ARGS (TEMP-TABLE)))
    (WORD-LEXICAL-WORD ANY)
    (WORD-CLASSIFICATION-NUMBER FIX)
    (WORD-FLAGS FIX)
    (WORD-SEMANTIC-STUFF ANY)
    (WORD-VERB-STUFF ANY)
    (WORD-ADJ-ID ANY)
    (WORD-DIR-ID ANY)>
";

        [TestMethod, TestCategory("NEW-PARSER?")]
        public async Task Game_Without_Objects_Should_Compile_With_NEW_PARSER_P()
        {
            await AssertRoutine("", @"<PRINTR ""Hello, world!"">")
                .WithGlobal(SNewParserBootstrap)
                .OutputsAsync("Hello, world!\n");
        }

        [TestMethod, TestCategory("NEW-PARSER?")]
        public async Task NEW_PARSER_P_Should_Affect_Vocab_Word_Size()
        {
            await AssertRoutine("", "<GETB ,VOCAB <+ 1 <GETB ,VOCAB 0>>>")
                .WithGlobal(SNewParserBootstrap)
                .WithGlobal("<COMPILATION-FLAG WORD-FLAGS-IN-TABLE <>>")
                .WithGlobal("<COMPILATION-FLAG ONE-BYTE-PARTS-OF-SPEECH <>>")
                .InV3()
                .GivesNumberAsync("12");
        }

        [TestMethod, TestCategory("NEW-PARSER?")]
        public async Task NEW_PARSER_P_Verbs_Should_Have_Verb_Data()
        {
            await AssertGlobals(
                SNewParserBootstrap,
                "<COMPILATION-FLAG WORD-FLAGS-IN-TABLE T>",
                "<COMPILATION-FLAG ONE-BYTE-PARTS-OF-SPEECH T>",
                "<ROUTINE V-SING () <>>",
                "<SYNTAX SING = V-SING>")
                .InV4()
                .ImpliesAsync("<N=? <GET ,W?SING 3> 0>");
        }

        [TestMethod, TestCategory("NEW-PARSER?")]
        public async Task NEW_PARSER_P_Should_Affect_Syntax_Format()
        {
            await AssertGlobals(
                SNewParserBootstrap,
                "<COMPILATION-FLAG WORD-FLAGS-IN-TABLE T>",
                "<COMPILATION-FLAG ONE-BYTE-PARTS-OF-SPEECH T>",
                "<ROUTINE V-ATTACK () <>>",
                "<SYNTAX ATTACK OBJECT WITH OBJECT = V-ATTACK>")
                .InV4()
                .ImpliesAsync(
                    "<=? <GET <GET ,W?ATTACK 3> 0> -1>",
                    "<=? <GET <GET ,W?ATTACK 3> 1> 0>",
                    "<=? <GET <GET ,W?ATTACK 3> 2> 0>",
                    "<N=? <GET <GET ,W?ATTACK 3> 3> 0>");
        }

        [TestMethod]
        public async Task WORD_FLAG_TABLE_Should_List_Words_And_Flags()
        {
            await AssertGlobals(
                SNewParserBootstrap,
                "<NEW-ADD-WORD FOO TOBJECT <> 12345>")
                .ImpliesAsync(
                    "<=? <GET ,WORD-FLAG-TABLE 0> 2>",
                    "<=? <GET ,WORD-FLAG-TABLE 1> ,W?FOO>",
                    "<=? <GET ,WORD-FLAG-TABLE 2> 12345>");
        }

        [TestMethod, Timeout(5000)]
        public async Task WORD_FLAGS_LIST_With_Duplicates_Should_Compile()
        {
            await AssertGlobals(
                SNewParserBootstrap,
                "<COMPILATION-FLAG WORD-FLAGS-IN-TABLE T>",
                "<NEW-ADD-WORD FOO TBUZZ 123 456>",
                "<NEW-ADD-WORD BAR TBUZZ 234 567>",
                "<NEW-ADD-WORD FOO TADJ 345 678>")
                .InV6()
                .CompilesAsync();
        }

        [TestMethod, TestCategory("NEW-PARSER?")]
        public async Task NEW_PARSER_P_Synonyms_Should_Use_Pointers()
        {
            await AssertGlobals(
                SNewParserBootstrap,
                "<COMPILATION-FLAG WORD-FLAGS-IN-TABLE T>",
                "<COMPILATION-FLAG ONE-BYTE-PARTS-OF-SPEECH T>",
                "<NEW-ADD-WORD FOO TBUZZ>",
                "<SYNONYM FOO BAR>")
                .InV4()
                .ImpliesAsync(
                    "<=? <GET ,W?BAR 3> ,W?FOO>",
                    "<=? <GETB ,W?BAR 8> 0>");
        }

        [TestMethod]
        public async Task Synonym_Used_As_Preposition_Should_Copy_The_Preposition_Number()
        {
            await AssertGlobals(
                "<SYNONYM ON ONTO>",
                "<SYNTAX CLIMB ON OBJECT = V-CLIMB>",
                "<SYNTAX CLIMB ONTO OBJECT = V-CLIMB>",
                "<ROUTINE V-CLIMB () <>>")
                .InV3()
                .ImpliesAsync(
                    // original word ON should be a preposition = PR?ON
                    "<=? <GETB ,W?ON 4> ,PS?PREPOSITION>",
                    "<=? <GETB ,W?ON 5> ,PR?ON>",
                    "<=? <GETB ,W?ON 6> 0>",
                    // synonym ONTO should also be a preposition = PR?ON
                    "<=? <GETB ,W?ONTO 4> ,PS?PREPOSITION>",
                    "<=? <GETB ,W?ONTO 5> ,PR?ON>",
                    "<=? <GETB ,W?ONTO 6> 0>",
                    // preposition table should only list ON
                    "<=? <GET ,PREPOSITIONS 0> 1>",
                    "<=? <GET ,PREPOSITIONS 1> ,W?ON>",
                    "<=? <GET ,PREPOSITIONS 2> ,PR?ON>");
        }

        #endregion
    }
}
