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

using Microsoft.VisualStudio.TestTools.UnitTesting;
using System.Diagnostics.Contracts;

namespace IntegrationTests
{
    [TestClass]
    public class SyntaxTests
    {
        static GlobalsAssertionHelper AssertGlobals(params string[] globals)
        {
            Contract.Requires(globals != null && globals.Length > 0);
            Contract.Requires(Contract.ForAll(globals, c => !string.IsNullOrWhiteSpace(c)));

            return new GlobalsAssertionHelper(globals);
        }

        static RoutineAssertionHelper AssertRoutine(string argSpec, string body)
        {
            Contract.Requires(argSpec != null);
            Contract.Requires(!string.IsNullOrWhiteSpace(body));

            return new RoutineAssertionHelper(argSpec, body);
        }

        [TestMethod]
        public void First_Preaction_Definition_Per_Action_Name_Should_Persist()
        {
            AssertRoutine("", "<TELL " +
                    "N <=? <GET ,ACTIONS ,V?FOO> ,V-FOO> CR " +
                    "N <=? <GET ,ACTIONS ,V?FOO-WITH> ,V-FOO> CR " +
                    "N <=? <GET ,ACTIONS ,V?BAR> ,V-BAR> CR " +
                    "N <=? <GET ,PREACTIONS ,V?FOO> ,PRE-FOO> CR " +
                    "N <=? <GET ,PREACTIONS ,V?FOO-WITH> <>> CR " +
                    "N <=? <GET ,PREACTIONS ,V?BAR> <>> CR " +
                    ">")
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
                .WithGlobal("<DEFMAC FOO () <FORM REST ,PRTBL 1>>")
                .Compiles();
        }
    }
}
