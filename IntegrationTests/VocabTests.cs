/* Copyright 2010, 2015 Jesse McGrew
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
using System.Collections.Generic;
using System.Diagnostics.Contracts;
using Zilf.ZModel.Vocab;

namespace IntegrationTests
{
    [TestClass]
    public class VocabTests
    {
        private static RoutineAssertionHelper AssertRoutine(string argSpec, string body)
        {
            Contract.Requires(argSpec != null);
            Contract.Requires(!string.IsNullOrWhiteSpace(body));

            return new RoutineAssertionHelper(argSpec, body);
        }

        private static GlobalsAssertionHelper AssertGlobals(params string[] globals)
        {
            Contract.Requires(globals != null && globals.Length > 0);
            Contract.Requires(Contract.ForAll(globals, c => !string.IsNullOrWhiteSpace(c)));

            return new GlobalsAssertionHelper(globals);
        }

        [TestMethod]
        public void VOC_With_2nd_Arg_Atom_Should_Set_PartOfSpeech()
        {
            AssertRoutine("\"AUX\" (P <GET ,VOC-TABLE 0>)", "<GETB .P 4>")
                .WithGlobal("<GLOBAL VOC-TABLE <PTABLE <VOC \"XYZZY\" ADJ>>>")
                .InV3()
                .GivesNumber(((int)(PartOfSpeech.Adjective | PartOfSpeech.AdjectiveFirst)).ToString());
        }

        [TestMethod]
        public void VOC_With_2nd_Arg_False_Should_Not_Set_PartOfSpeech()
        {
            AssertRoutine("\"AUX\" (P <GET ,VOC-TABLE 0>)", "<GETB .P 4>")
                .WithGlobal("<GLOBAL VOC-TABLE <PTABLE <VOC \"XYZZY\" <>>>>")
                .InV3()
                .GivesNumber(((int)(PartOfSpeech.None)).ToString());
        }

        [TestMethod]
        public void VOC_With_2nd_Arg_Missing_Should_Not_Set_PartOfSpeech()
        {
            AssertRoutine("\"AUX\" (P <GET ,VOC-TABLE 0>)", "<GETB .P 4>")
                .WithGlobal("<GLOBAL VOC-TABLE <PTABLE <VOC \"XYZZY\">>>")
                .InV3()
                .GivesNumber(((int)(PartOfSpeech.None)).ToString());
        }

        [TestMethod]
        public void SIBREAKS_Should_Affect_Lexing()
        {
            AssertRoutine("",
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
                .Outputs("59\n4\ngrant\n'\ns\ntomb\n");
        }

        [TestMethod]
        public void TCHARS_Should_Affect_Header()
        {
            AssertGlobals(
                "<CONSTANT F12 144>",
                "<CONSTANT TCHARS <TABLE (BYTE) F12 0>>")
                .InV5()
                .Implies(
                    "<==? <LOWCORE TCHARS> ,TCHARS>",
                    "<==? <GETB ,TCHARS 0> 144>");
        }

        private static string[] PrepImplications(bool compact, params string[] wordAndIdConstantPairs)
        {
            Contract.Requires(wordAndIdConstantPairs != null && wordAndIdConstantPairs.Length % 2 == 0);

            const string SCompactTest =
                "<==? <GETB <INTBL? {0} <+ ,PREPOSITIONS 2> <GET ,PREPOSITIONS 0> *203*> 2> {1}>";
            const string SNonCompactTest =
                "<==? <GET <INTBL? {0} <+ ,PREPOSITIONS 2> <GET ,PREPOSITIONS 0> *204*> 1> {1}>";

            string testFormat = compact ? SCompactTest : SNonCompactTest;

            var result = new List<string>();
            result.Add(string.Format("<==? <GET ,PREPOSITIONS 0> {0}>", wordAndIdConstantPairs.Length / 2));

            for (int i = 0; i + 1 < wordAndIdConstantPairs.Length; i += 2)
            {
                var wordConstant = wordAndIdConstantPairs[i];
                var idConstant = wordAndIdConstantPairs[i + 1];

                result.Add(string.Format(testFormat, wordConstant, idConstant));
            }

            return result.ToArray();
        }

        [TestMethod]
        public void PREPOSITIONS_NonCompact_Should_Use_4_Byte_Entries_And_Not_List_Synonyms()
        {
            AssertGlobals(
                "<ROUTINE V-LOOK () <>>",
                "<ROUTINE V-PICK-UP-WITH () <>>",
                "<SYNTAX LOOK THROUGH OBJECT = V-LOOK>",
                "<PREP-SYNONYM THROUGH THRU>",
                "<SYNTAX PICK UP OBJECT WITH OBJECT = V-PICK-UP-WITH>")
                .InV5()
                .Implies(PrepImplications(
                    false,
                    "W?THROUGH", "PR?THROUGH",
                    "W?UP", "PR?UP",
                    "W?WITH", "PR?WITH"));
        }

        [TestMethod]
        public void PREPOSITIONS_Compact_Should_Use_3_Byte_Entries_And_List_Synonyms()
        {
            AssertGlobals(
                "<SETG COMPACT-VOCABULARY? T>",
                "<ROUTINE V-LOOK () <>>",
                "<ROUTINE V-PICK-UP-WITH () <>>",
                "<SYNTAX LOOK THROUGH OBJECT = V-LOOK>",
                "<PREP-SYNONYM THROUGH THRU>",
                "<SYNTAX PICK UP OBJECT WITH OBJECT = V-PICK-UP-WITH>")
                .InV5()
                .Implies(PrepImplications(
                    true,
                    "W?THROUGH", "PR?THROUGH",
                    "W?THRU", "PR?THROUGH",
                    "W?UP", "PR?UP",
                    "W?WITH", "PR?WITH"));
        }

        [TestMethod]
        public void LONG_WORDS_P_Should_Generate_LONG_WORD_TABLE()
        {
            AssertGlobals(
                "<LONG-WORDS?>",
                "<OBJECT FOO (SYNONYM HEMIDEMISEMIQUAVER)>")
                .Implies(
                    "<==? <GET ,LONG-WORD-TABLE 0> 1>",
                    "<==? <GET ,LONG-WORD-TABLE 1> ,W?HEMIDEMISEMIQUAVER>",
                    "<==? <GET ,LONG-WORD-TABLE 2> \"hemidemisemiquaver\">");
        }

        [TestMethod]
        public void LANGUAGE_Should_Affect_Lexing()
        {
            AssertRoutine("",
                "<READ ,INBUF ,LEXBUF> " +
                @"<==? <GET ,LEXBUF 1> ,W?AU\%SER>")
                .WithGlobal("<LANGUAGE GERMAN>")
                .WithGlobal(@"<BUZZ AU\%SER>")
                .WithGlobal("<GLOBAL LEXBUF <ITABLE 59 (LEXV) 0 #BYTE 0 #BYTE 0>>")
                .WithGlobal("<GLOBAL INBUF <ITABLE 80 (BYTE LENGTH) 0>>")
                .InV5()
                .WithInput("außer")
                .GivesNumber("1");
        }

        [TestMethod]
        public void Colliding_Words_Should_Be_Merged()
        {
            AssertGlobals(
                "<OBJECT FOO (SYNONYM HEMIDEMISEMIQUAVER)>",
                "<OBJECT BAR (SYNONYM HEMIDE)>",
                "<OBJECT BAZ (ADJECTIVE HEMIDEISH)>")
                .InV3()
                .Implies(
                    "<==? ,W?HEMIDEMISEMIQUAVER ,W?HEMIDE>",
                    "<==? ,W?HEMIDE ,W?HEMIDEISH>",
                    "<BTST <GETB ,W?HEMIDE 4> ,PS?OBJECT>",
                    "<BTST <GETB ,W?HEMIDE 4> ,PS?ADJECTIVE>");
        }
    }
}
