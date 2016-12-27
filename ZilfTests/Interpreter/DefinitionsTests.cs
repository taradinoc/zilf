/* Copyright 2010-2016 Jesse McGrew
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
using Zilf.Interpreter.Values;

namespace ZilfTests.Interpreter
{
    [TestClass]
    public class DefinitionsTests
    {
        [TestMethod]
        public void Default_Definition_Should_Be_Used_When_Not_Replaced()
        {
            const string CODE = @"
<DEFAULT-DEFINITION FOO-ROUTINE
    <DEFINE FOO () 123>>

<FOO>";

            TestHelpers.EvalAndAssert(CODE, new ZilFix(123));
        }

        [TestMethod]
        public void Replacement_Should_Be_Used_When_Given()
        {
            const string CODE = @"
<REPLACE-DEFINITION FOO-ROUTINE
    <DEFINE FOO () 456>>

<DEFAULT-DEFINITION FOO-ROUTINE
    <DEFINE FOO () 123>>

<FOO>";

            TestHelpers.EvalAndAssert(CODE, new ZilFix(456));
        }

        [TestMethod]
        public void Delayed_Replacement_Should_Be_Evaluated_At_The_Right_Place()
        {
            const string CODE = @"
<DELAY-DEFINITION FOO-ROUTINE>

<SETG FOO-RESULT 789>

<REPLACE-DEFINITION FOO-ROUTINE
    <EVAL <FORM DEFINE FOO '() ,FOO-RESULT>>>

<SETG FOO-RESULT 123>

<DEFAULT-DEFINITION FOO-ROUTINE
    <EVAL <FORM DEFINE FOO '() ,FOO-RESULT>>>

<FOO>";

            TestHelpers.EvalAndAssert(CODE, new ZilFix(789));
        }

        [TestMethod]
        public void Definition_Block_Names_Are_Shared_Across_Packages()
        {
            const string CODE = @"
FOO!-

<PACKAGE ""FOO"">
<DELAY-DEFINITION FOO-ROUTINE>
<ENDPACKAGE>

<PACKAGE ""BAR"">
<DEFAULT-DEFINITION FOO-ROUTINE
    <DEFINE FOO () 123>>
<ENDPACKAGE>

<PACKAGE ""BAZ"">
<REPLACE-DEFINITION FOO-ROUTINE
    <DEFINE FOO () 456>>
<ENDPACKAGE>

<FOO>";

            TestHelpers.EvalAndAssert(CODE, new ZilFix(456));
        }
    }
}
