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
using System.Linq;
using System.Threading.Tasks;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Zilf.Tests.Integration
{
    [TestClass, TestCategory("Compiler"), TestCategory("Vocab")]
    public class SyntaxTests : IntegrationTestClass
    {
        [TestMethod]
        public async Task First_Preaction_Definition_Per_Action_Name_Should_Persist()
        {
            await AssertRoutine("",
                    @"<TELL N <=? <GET ,ACTIONS ,V?FOO> ,V-FOO> CR" +
                    @" N <=? <GET ,ACTIONS ,V?FOO-WITH> ,V-FOO> CR" +
                    @" N <=? <GET ,ACTIONS ,V?BAR> ,V-BAR> CR" +
                    @" N <=? <GET ,PREACTIONS ,V?FOO> ,PRE-FOO> CR" +
                    @" N <=? <GET ,PREACTIONS ,V?FOO-WITH> <>> CR" +
                    @" N <=? <GET ,PREACTIONS ,V?BAR> <>> CR>")
                .WithGlobal("<ROUTINE V-FOO () <>>")
                .WithGlobal("<ROUTINE V-BAR () <>>")
                .WithGlobal("<ROUTINE PRE-FOO () <>>")
                .WithGlobal("<ROUTINE PRE-FOO-2 () <>>")
                .WithGlobal("<ROUTINE PRE-BAR () <>>")
                .WithGlobal("<SYNTAX FOO = V-FOO PRE-FOO>")
                .WithGlobal("<SYNTAX FOO OBJECT = V-FOO PRE-FOO-2>")
                .WithGlobal("<SYNTAX FOO OBJECT AT OBJECT = V-FOO>")
                .WithGlobal("<SYNTAX FOO OBJECT WITH OBJECT = V-FOO <> FOO-WITH>")
                .WithGlobal("<SYNTAX BAR = V-BAR>")
                .WithGlobal("<SYNTAX BAR OBJECT = V-BAR PRE-BAR>")
                .WithWarnings("ZIL0208")
                .OutputsAsync("1\n1\n1\n1\n1\n1\n");
        }

        [TestMethod]
        public async Task Syntax_Lines_Can_Define_Verb_Synonyms()
        {
            await AssertRoutine("", "<DO (I 4 6) <PRINTN <=? <GETB ,W?TOSS .I> <GETB ,W?CHUCK .I>>>>")
                .WithGlobal("<ROUTINE V-TOSS () <>>")
                .WithGlobal("<SYNTAX TOSS (CHUCK) OBJECT AT OBJECT = V-TOSS>")
                .InV3()
                .OutputsAsync("111");
        }

        [TestMethod]
        public async Task NEW_SFLAGS_Defines_New_Scope_Flags()
        {
            await AssertGlobals(
                    @"<ROUTINE GET-OPTS1 (ACT ""AUX"" (ST <GET ,VERBS <- 255 .ACT>>)) <GETB .ST 6>>",
                    "<CONSTANT SEARCH-DO-TAKE 1>",
                    "<CONSTANT SEARCH-MUST-HAVE 2>",
                    "<CONSTANT SEARCH-MANY 4>",
                    "<CONSTANT SEARCH-STANDARD 8>",
                    "<CONSTANT SEARCH-OPTIONAL 16>",
                    "<CONSTANT SEARCH-ADDITIVE 32>",
                    "<CONSTANT SEARCH-ALL ,SEARCH-STANDARD>",
                    @"<SETG NEW-SFLAGS [""STANDARD"" ,SEARCH-STANDARD ""OPTIONAL"" ,SEARCH-OPTIONAL ""ADDITIVE"" (+ ,SEARCH-ADDITIVE)]>",
                    "<ROUTINE V-DUMMY () <>>",
                    "<SYNTAX FOO OBJECT (OPTIONAL) = V-DUMMY>",
                    "<SYNTAX BAR OBJECT (HAVE) = V-DUMMY>",
                    "<SYNTAX BAZ OBJECT (HAVE OPTIONAL) = V-DUMMY>",
                    "<SYNTAX QUUX OBJECT (HAVE ADDITIVE) = V-DUMMY>")
                .ImpliesAsync(
                    "<=? <GET-OPTS1 ,ACT?FOO> ,SEARCH-OPTIONAL>",
                    "<=? <GET-OPTS1 ,ACT?BAR> <+ ,SEARCH-STANDARD ,SEARCH-MUST-HAVE>>",
                    "<=? <GET-OPTS1 ,ACT?BAZ> <+ ,SEARCH-OPTIONAL ,SEARCH-MUST-HAVE>>",
                    "<=? <GET-OPTS1 ,ACT?QUUX> <+ ,SEARCH-STANDARD ,SEARCH-MUST-HAVE ,SEARCH-ADDITIVE>>");
        }

        [TestMethod]
        public async Task Late_Syntax_Tables_Can_Be_Referenced_From_Macros()
        {
            await AssertRoutine("", "<PRINTN <FOO>>")
                .WithGlobal(@"<DEFMAC FOO () <FORM REST ,PRTBL 1>>")
                .CompilesAsync();
        }

        [TestMethod]
        public async Task Old_Parser_Only_Allows_255_Verbs()
        {
            var globals = Enumerable.Range(0, 256)
                .Select(i => $"<SYNTAX VERB-{i} = V-FOO>")
                .ToArray();

            await AssertGlobals(globals)
                .WithGlobal("<ROUTINE V-FOO () <>>")
                .DoesNotCompileAsync("MDL0426", // too many {0}, only {1} allowed in this vocab format
                    d => d.GetFormattedMessage().Contains("verbs"));
        }

        [TestMethod]
        public async Task Old_Parser_Only_Allows_255_Actions()
        {
            var globals = Enumerable.Range(0, 256)
                .Select(i => $"<SYNTAX VERB-{i / 100} PREP-{i % 100} OBJECT = V-FOO-{i}> <ROUTINE V-FOO-{i} () <>>")
                .ToArray();

            await AssertGlobals(globals)
                .DoesNotCompileAsync("MDL0426", // too many {0}, only {1} allowed in this vocab format
                    d => d.GetFormattedMessage().Contains("actions"));
        }

        [TestMethod, TestCategory("NEW-PARSER?")]
        [TestCategory("Slow")]
        public async Task NEW_PARSER_P_Supports_More_Than_255_Verbs_And_Actions()
        {
            var globals = new List<string>(258) { VocabTests.SNewParserBootstrap };
            globals.AddRange(Enumerable.Range(0, 257).Select(i => $"<SYNTAX VERB-{i} = V-VERB-{i}> <ROUTINE V-VERB-{i} () <>>"));

            await AssertGlobals([.. globals]).GeneratesCodeMatchingAsync(@"V\?VERB-256=256");
        }

        [TestMethod]
        public async Task COMPACT_PREACTIONS_P_Should_Affect_Preaction_Table_Format()
        {
            await AssertGlobals(
                "<SETG COMPACT-PREACTIONS? T>",
                "<SYNTAX FEE = V-FEE>",
                "<SYNTAX FIE = V-FIE>",
                "<SYNTAX FOE = V-FOE>",
                "<SYNTAX FOO = V-FOO PRE-FOO>",
                "<ROUTINE V-FEE () <>>",
                "<ROUTINE V-FIE () <>>",
                "<ROUTINE V-FOE () <>>",
                "<ROUTINE V-FOO () <>>",
                "<ROUTINE PRE-FOO () <>>")
                .ImpliesAsync(
                    "<=? <GET ,PREACTIONS 0> ,V?FOO>",
                    "<=? <GET ,PREACTIONS 1> ,PRE-FOO>",
                    "<=? <GET ,PREACTIONS 2> -1>",
                    "<=? <GET ,PREACTIONS 3> 0>");
        }

        [TestMethod]
        public async Task REMOVE_SYNTAX_Should_Remove_Matching_Syntax_By_Verb()
        {
            await AssertGlobals(
                "<SYNTAX TAKE OBJECT = V-TAKE>",
                "<SYNTAX TAKE OBJECT FROM OBJECT = V-TAKE-FROM>",
                "<SYNTAX DROP OBJECT = V-DROP>",
                "<ROUTINE V-TAKE () <>>",
                "<ROUTINE V-TAKE-FROM () <>>",
                "<ROUTINE V-DROP () <>>",
                "<REMOVE-SYNTAX TAKE>")
                .GeneratesCodeNotMatchingAsync("V-TAKE");
        }

        [TestMethod]
        public async Task REMOVE_SYNTAX_Should_Remove_Matching_Syntax_By_Action()
        {
            await AssertGlobals(
                "<SYNTAX TAKE OBJECT = V-TAKE>",
                "<SYNTAX GRAB OBJECT = V-TAKE>",
                "<SYNTAX DROP OBJECT = V-DROP>",
                "<ROUTINE V-TAKE () <>>",
                "<ROUTINE V-DROP () <>>",
                "<REMOVE-SYNTAX * = V-TAKE>")
                .GeneratesCodeNotMatchingAsync("V-TAKE");
        }

        [TestMethod]
        public async Task REMOVE_SYNTAX_Should_Remove_Matching_Syntax_By_Preposition()
        {
            await AssertGlobals(
                "<SYNTAX PUT OBJECT IN OBJECT = V-PUT-IN>",
                "<SYNTAX PUT OBJECT ON OBJECT = V-PUT-ON>",
                "<SYNTAX TAKE OBJECT = V-TAKE>",
                "<ROUTINE V-PUT-IN () <>>",
                "<ROUTINE V-PUT-ON () <>>",
                "<ROUTINE V-TAKE () <>>",
                "<REMOVE-SYNTAX PUT OBJECT IN>")
                .GeneratesCodeMatchingAsync(code => code.Contains("V-PUT-ON") && !code.Contains("V-PUT-IN"));
        }

        [TestMethod]
        public async Task REMOVE_SYNTAX_Should_Match_Wildcard_Patterns()
        {
            await AssertGlobals(
                "<SYNTAX PUT OBJECT IN OBJECT = V-PUT-IN>",
                "<SYNTAX PUT OBJECT ON OBJECT = V-PUT-ON>",
                "<SYNTAX TAKE OBJECT = V-TAKE>",
                "<ROUTINE V-PUT-IN () <>>",
                "<ROUTINE V-PUT-ON () <>>",
                "<ROUTINE V-TAKE () <>>",
                "<REMOVE-SYNTAX PUT * * OBJECT>")
                .GeneratesCodeNotMatchingAsync("V-PUT");
        }

        [TestMethod]
        public async Task REMOVE_SYNTAX_Should_Match_FALSE_As_Nothing()
        {
            await AssertGlobals(
                "<SYNTAX TAKE INVENTORY OBJECT (FIND KLUDGEBIT) = V-INVENTORY>",
                "<SYNTAX TAKE OBJECT = V-YOINK>",
                "<SYNTAX TAKE = V-TAKE>",
                "<ROUTINE V-INVENTORY () <>>",
                "<ROUTINE V-TAKE () <>>",
                "<REMOVE-SYNTAX TAKE <>>")
                .GeneratesCodeMatchingAsync(code => code.Contains("V-INVENTORY") && !code.Contains("V-YOINK") && !code.Contains("V-TAKE"));

            await AssertGlobals(
                "<SYNTAX TAKE INVENTORY OBJECT (FIND KLUDGEBIT) = V-INVENTORY>",
                "<SYNTAX TAKE OBJECT = V-YOINK>",
                "<SYNTAX TAKE = V-TAKE>",
                "<ROUTINE V-INVENTORY () <>>",
                "<ROUTINE V-TAKE () <>>",
                "<REMOVE-SYNTAX TAKE <> * = *>")
                .GeneratesCodeMatchingAsync(code => code.Contains("V-INVENTORY") && !code.Contains("V-YOINK") && !code.Contains("V-TAKE"));

            await AssertGlobals(
                "<SYNTAX TAKE INVENTORY OBJECT (FIND KLUDGEBIT) = V-INVENTORY>",
                "<SYNTAX TAKE OBJECT = V-YOINK>",
                "<SYNTAX TAKE = V-TAKE>",
                "<ROUTINE V-INVENTORY () <>>",
                "<ROUTINE V-TAKE () <>>",
                "<REMOVE-SYNTAX TAKE <> OBJECT>")
                .GeneratesCodeMatchingAsync(code => code.Contains("V-INVENTORY") && !code.Contains("V-YOINK") && code.Contains("V-TAKE"));
        }

        [TestMethod]
        public async Task REMOVE_SYNTAX_Should_Maintain_Correct_Numbering()
        {
            await AssertGlobals(
                "<SYNTAX RESTART = V-FOO>",
                "<SYNTAX SAVE = V-BAR>",
                "<SYNTAX BRIEF = V-BAZ>",
                "<ROUTINE V-FOO () <>>",
                "<ROUTINE V-BAR () <>>",
                "<ROUTINE V-BAZ () <>>",
                "<REMOVE-SYNTAX * = V-BAR>")
                .ImpliesAsync(
                    "<==? <GETB ,W?SAVE 5> 0>",                     // SAVE should have no verb number (deleting the word entirely would be tricky...)
                    "<==? <GETB ,W?RESTART 5> ,ACT?RESTART>",       // RESTART's verb number should match the constant
                    "<==? <GETB <GET ,VERBS <- 255 ,ACT?RESTART>> 8> ,V?FOO>",      // verb table's entry for RESTART should point to the syntax table with an entry for V?FOO
                    "<==? <GET ,ACTIONS ,V?FOO> V-FOO>",            // action table entry for V?FOO should point to V-FOO
                    "<==? <GETB ,W?BRIEF 5> ,ACT?BRIEF>",
                    "<==? <GETB <GET ,VERBS <- 255 ,ACT?BRIEF>> 8> ,V?BAZ>",
                    "<==? <GET ,ACTIONS ,V?BAZ> V-BAZ>"
                );
        }

        [TestMethod]
        public async Task REMOVE_SYNTAX_Should_Handle_Multiple_Verb_Number_Gaps()
        {
            await AssertGlobals(
                "<SYNTAX V1 = V-FOO>",
                "<SYNTAX V2 = V-BAR>",
                "<SYNTAX V3 = V-BAZ>",
                "<SYNTAX V4 = V-QUUX>",
                "<ROUTINE V-FOO () <>>",
                "<ROUTINE V-BAR () <>>",
                "<ROUTINE V-BAZ () <>>",
                "<ROUTINE V-QUUX () <>>",
                "<REMOVE-SYNTAX * = V-BAR>",
                "<REMOVE-SYNTAX * = V-QUUX>")
                .ImpliesAsync(
                    "<==? <GETB ,W?V2 5> 0>",
                    "<==? <GETB ,W?V4 5> 0>",
                    "<==? <GETB ,W?V1 5> ,ACT?V1>",
                    "<==? <GETB <GET ,VERBS <- 255 ,ACT?V1>> 8> ,V?FOO>",
                    "<==? <GET ,ACTIONS ,V?FOO> V-FOO>",
                    "<==? <GETB ,W?V3 5> ,ACT?V3>",
                    "<==? <GETB <GET ,VERBS <- 255 ,ACT?V3>> 8> ,V?BAZ>",
                    "<==? <GET ,ACTIONS ,V?BAZ> V-BAZ>"
                );
        }

        [TestMethod]
        public async Task REMOVE_SYNTAX_Should_Clear_Verb_Synonyms_When_Last_Syntax_Removed()
        {
            await AssertGlobals(
                "<SYNTAX TOSS (CHUCK) = V-TOSS>",
                "<SYNTAX KEEP = V-KEEP>",
                "<ROUTINE V-TOSS () <>>",
                "<ROUTINE V-KEEP () <>>",
                "<REMOVE-SYNTAX TOSS>")
                .ImpliesAsync(
                    "<==? <GETB ,W?TOSS 5> 0>",
                    "<==? <GETB ,W?CHUCK 5> 0>",
                    "<==? <GETB ,W?KEEP 5> ,ACT?KEEP>",
                    "<==? <GETB <GET ,VERBS <- 255 ,ACT?KEEP>> 8> ,V?KEEP>",
                    "<==? <GET ,ACTIONS ,V?KEEP> V-KEEP>"
                );
        }

        [TestMethod, TestCategory("NEW-PARSER?")]
        public async Task REMOVE_SYNTAX_Should_Not_Leave_Stale_New_Parser_Verb_Data()
        {
            await AssertGlobals(
                VocabTests.SNewParserBootstrap,
                "<COMPILATION-FLAG WORD-FLAGS-IN-TABLE T>",
                "<COMPILATION-FLAG ONE-BYTE-PARTS-OF-SPEECH T>",
                "<ROUTINE V-SING () <>>",
                "<SYNTAX SING = V-SING>",
                "<REMOVE-SYNTAX SING>")
                .InV4()
                .GeneratesCodeNotMatchingAsync("ACT\\?SING");
        }

        [TestMethod]
        public async Task REMOVE_SYNTAX_Should_Clear_Prepositions_When_Last_Use_Removed()
        {
            await AssertGlobals(
                "<ROUTINE V-LOOK-THROUGH () <>>",
                "<ROUTINE V-LOOK-WITH () <>>",
                "<ROUTINE V-LOOK-UP () <>>",
                "<SYNTAX LOOK THROUGH OBJECT = V-LOOK-THROUGH>",
                "<SYNTAX LOOK WITH OBJECT = V-LOOK-WITH>",
                "<SYNTAX LOOK UP OBJECT = V-LOOK-UP>",
                "<REMOVE-SYNTAX LOOK THROUGH OBJECT>")
                .InV5()
                .ImpliesAsync(
                    "<==? <GET ,PREPOSITIONS 0> 2>",
                    "<==? <GET <INTBL? ,W?WITH <+ ,PREPOSITIONS 2> <GET ,PREPOSITIONS 0> *204*> 1> ,PR?WITH>",
                    "<==? <GET <INTBL? ,W?UP <+ ,PREPOSITIONS 2> <GET ,PREPOSITIONS 0> *204*> 1> ,PR?UP>",
                    "<NOT <INTBL? ,W?THROUGH <+ ,PREPOSITIONS 2> <GET ,PREPOSITIONS 0> *204*>>"
                );
        }

        [TestMethod]
        public async Task REMOVE_SYNTAX_Should_Clear_Preposition_Synonyms_In_Compact_Vocab()
        {
            await AssertGlobals(
                "<SETG COMPACT-VOCABULARY? T>",
                "<ROUTINE V-LOOK-THROUGH () <>>",
                "<ROUTINE V-LOOK-WITH () <>>",
                "<SYNTAX LOOK THROUGH OBJECT = V-LOOK-THROUGH>",
                "<SYNTAX LOOK WITH OBJECT = V-LOOK-WITH>",
                "<PREP-SYNONYM THROUGH THRU>",
                "<REMOVE-SYNTAX LOOK THROUGH OBJECT>")
                .InV5()
                .ImpliesAsync(
                    "<==? <GET ,PREPOSITIONS 0> 1>",
                    "<==? <GETB <INTBL? ,W?WITH <+ ,PREPOSITIONS 2> <GET ,PREPOSITIONS 0> *203*> 2> ,PR?WITH>",
                    "<NOT <INTBL? ,W?THROUGH <+ ,PREPOSITIONS 2> <GET ,PREPOSITIONS 0> *203*>>",
                    "<NOT <INTBL? ,W?THRU <+ ,PREPOSITIONS 2> <GET ,PREPOSITIONS 0> *203*>>"
                );
        }

        [TestMethod]
        public async Task REMOVE_SYNONYM_Should_Remove_Synonyms()
        {
            await AssertGlobals(
                "<SYNTAX TAKE OBJECT = V-TAKE>",
                "<SYNONYM TAKE GET GRAB>",
                "<REMOVE-SYNONYM GET>",
                "<SYNTAX GET OBJECT = V-GET>",
                "<ROUTINE V-TAKE () <>>",
                "<ROUTINE V-GET () <>>",
                "<ROUTINE GET-SYNTAXES (WORD) <GET ,VTBL <- 255 <GETB .WORD 5>>>>")
                .ImpliesAsync(
                    "<=? <GET-SYNTAXES ,W?TAKE> <GET-SYNTAXES ,W?GRAB>>",
                    "<N=? <GET-SYNTAXES ,W?TAKE> <GET-SYNTAXES ,W?GET>>");
        }
    }
}
