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
using System.Diagnostics.Contracts;

namespace IntegrationTests
{
    [TestClass]
    public class SyntaxTests
    {
        private static RoutineAssertionHelper AssertRoutine(string argSpec, string body)
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
    }
}
