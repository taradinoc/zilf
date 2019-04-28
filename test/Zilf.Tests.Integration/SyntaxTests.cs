/* Copyright 2010-2018 Jesse McGrew
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
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Zilf.Tests.Integration
{
    [TestClass, TestCategory("Compiler"), TestCategory("Vocab")]
    public class SyntaxTests : IntegrationTestClass
    {
        [TestMethod]
        public void First_Preaction_Definition_Per_Action_Name_Should_Persist()
        {
            AssertRoutine("",
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
                .Outputs("1\n1\n1\n1\n1\n1\n");
        }

        [TestMethod]
        public void Syntax_Lines_Can_Define_Verb_Synonyms()
        {
            AssertRoutine("", "<DO (I 4 6) <PRINTN <=? <GETB ,W?TOSS .I> <GETB ,W?CHUCK .I>>>>")
                .WithGlobal("<ROUTINE V-TOSS () <>>")
                .WithGlobal("<SYNTAX TOSS (CHUCK) OBJECT AT OBJECT = V-TOSS>")
                .InV3()
                .Outputs("111");
        }

        [TestMethod]
        public void NEW_SFLAGS_Defines_New_Scope_Flags()
        {
            AssertGlobals(
                    @"<ROUTINE GET-OPTS1 (ACT ""AUX"" (ST <GET ,VERBS <- 255 .ACT>>)) <GETB .ST 6>>",
                    "<CONSTANT SEARCH-DO-TAKE 1>",
                    "<CONSTANT SEARCH-MUST-HAVE 2>",
                    "<CONSTANT SEARCH-MANY 4>",
                    "<CONSTANT SEARCH-STANDARD 8>",
                    "<CONSTANT SEARCH-OPTIONAL 16>",
                    "<CONSTANT SEARCH-ALL ,SEARCH-STANDARD>",
                    @"<SETG NEW-SFLAGS [""STANDARD"" ,SEARCH-STANDARD ""OPTIONAL"" ,SEARCH-OPTIONAL]>",
                    "<ROUTINE V-DUMMY () <>>",
                    "<SYNTAX FOO OBJECT (OPTIONAL) = V-DUMMY>",
                    "<SYNTAX BAR OBJECT (HAVE) = V-DUMMY>",
                    "<SYNTAX BAZ OBJECT (HAVE OPTIONAL) = V-DUMMY>")
                .Implies(
                    "<=? <GET-OPTS1 ,ACT?FOO> ,SEARCH-OPTIONAL>",
                    "<=? <GET-OPTS1 ,ACT?BAR> <+ ,SEARCH-STANDARD ,SEARCH-MUST-HAVE>>",
                    "<=? <GET-OPTS1 ,ACT?BAZ> <+ ,SEARCH-OPTIONAL ,SEARCH-MUST-HAVE>>");
        }

        [TestMethod]
        public void Late_Syntax_Tables_Can_Be_Referenced_From_Macros()
        {
            AssertRoutine("", "<PRINTN <FOO>>")
                .WithGlobal(@"<DEFMAC FOO () <FORM REST ,PRTBL 1>>")
                .Compiles();
        }

        [TestMethod]
        public void Old_Parser_Only_Allows_255_Verbs()
        {
            var globals = Enumerable.Range(0, 256)
                .Select(i => $"<SYNTAX VERB-{i} = V-FOO>")
                .ToArray();

            AssertGlobals(globals)
                .WithGlobal("<ROUTINE V-FOO () <>>")
                .DoesNotCompile("MDL0426", // too many {0}, only {1} allowed in this vocab format
                    d => d.GetFormattedMessage().Contains("verbs"));
        }

        [TestMethod]
        public void Old_Parser_Only_Allows_255_Actions()
        {
            var globals = Enumerable.Range(0, 256)
                .Select(i => $"<SYNTAX VERB-{i / 100} PREP-{i % 100} OBJECT = V-FOO-{i}> <ROUTINE V-FOO-{i} () <>>")
                .ToArray();

            AssertGlobals(globals)
                .DoesNotCompile("MDL0426", // too many {0}, only {1} allowed in this vocab format
                    d => d.GetFormattedMessage().Contains("actions"));
        }

        [TestMethod, TestCategory("NEW-PARSER?")]
        public void NEW_PARSER_P_Supports_More_Than_255_Verbs_And_Actions()
        {
            var globals = new List<string>(258) { VocabTests.SNewParserBootstrap };
            globals.AddRange(Enumerable.Range(0, 257).Select(i => $"<SYNTAX VERB-{i} = V-VERB-{i}> <ROUTINE V-VERB-{i} () <>>"));

            AssertGlobals(globals.ToArray()).GeneratesCodeMatching(@"V\?VERB-256=256");
        }

    }
}
