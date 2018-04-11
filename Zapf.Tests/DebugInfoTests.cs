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
using NSubstitute;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Zapf.Tests
{
    [TestClass, TestCategory("Debug Info")]
    public class DebugInfoTests
    {
        #region Additional test attributes
        //
        // You can use the following additional attributes as you write your tests:
        //
        // Use ClassInitialize to run code before running the first test in the class
        // [ClassInitialize()]
        // public static void MyClassInitialize(TestContext testContext) { }
        //
        // Use ClassCleanup to run code after all tests in a class have run
        // [ClassCleanup()]
        // public static void MyClassCleanup() { }
        //
        // Use TestInitialize to run code before running each test 
        // [TestInitialize()]
        // public void MyTestInitialize() { }
        //
        // Use TestCleanup to run code after each test has run
        // [TestCleanup()]
        // public void MyTestCleanup() { }
        //
        #endregion

        [TestMethod]
        public void DEBUG_LINE_Addresses_Should_Be_Correct_Near_Long_Conditional_Branches()
        {
            const string SCode = @"
    RELEASEID=111

WORDS::
GLOBAL::
OBJECT::
VOCAB::
IMPURE::
ENDLOD::

    .DEBUG-ROUTINE 0,0,0,""GO""
    .FUNCT GO
START::
    .DEBUG-LINE 1,77,1
LINE77::	EQUAL? 0,1 \?AHEAD
	.DEBUG-LINE 1,78,1
LINE78::	EQUAL? 2,3 /FALSE
	.DEBUG-LINE 1,79,1
LINE79::	;PRINTI ""This is some long text. We need enough text to force the conditional branch on line 77 into long form.""
	PRINTI ""This is some long text. We need enough text to force the conditional branch on line 77 into long form.""
	PRINTI ""This is some long text. We need enough text to force the conditional branch on line 77 into long form.""
	PRINTI ""This is some long text. We need enough text to force the conditional branch on line 77 into long form.""
	PRINTI ""This is some long text. We need enough text to force the conditional branch on line 77 into long form.""
	PRINTI ""This is some long text. We need enough text to force the conditional branch on line 77 into long form.""
	PRINTI ""This is some long text. We need enough text to force the conditional branch on line 77 into long form.""
	PRINTI ""This is some long text. We need enough text to force the conditional branch on line 77 into long form.""
	PRINTI ""This is some long text. We need enough text to force the conditional branch on line 77 into long form.""
	PRINTI ""This is some long text. We need enough text to force the conditional branch on line 77 into long form.""
?AHEAD:	PRINTI ""Enough of that.""
    QUIT
    .DEBUG-ROUTINE-END 0,0,0

    .END";

            // we'll collect the addresses associated with each DEBUG-LINE
            var lineRefs = new Dictionary<int, int>();

            // mock the debug file writer...
            var writer = Substitute.For<IDebugFileWriter>();

            // when the assembler calls WriteLine, record Line => Address in the dict
            writer
                .When(x => x.WriteLine(Arg.Any<LineRef>(), Arg.Any<int>()))
                .Do(c =>
                {
                    var line = c.Arg<LineRef>().Line;
                    var address = c.Arg<int>();
                    lineRefs.Add(line, address);
                });

            // InRoutine returns true between StartRoutine and EndRoutine
            writer.InRoutine.Returns(false);
            writer
                .When(
                    x => x.StartRoutine(
                        Arg.Any<LineRef>(), Arg.Any<int>(), Arg.Any<string>(), Arg.Any<IEnumerable<string>>()))
                .Do(c => writer.InRoutine.Returns(true));
            writer
                .When(
                    x => x.EndRoutine(Arg.Any<LineRef>(), Arg.Any<int>()))
                .Do(c => writer.InRoutine.Returns(false));

            // RestartRoutine discards the lines
            writer
                .When(x => x.RestartRoutine())
                .Do(c => lineRefs.Clear());
            
            Assert.IsTrue(TestHelper.Assemble(SCode, writer, out var symbols));

            Assert.AreEqual(lineRefs[77], symbols["LINE77"].Value);
            Assert.AreEqual(lineRefs[78], symbols["LINE78"].Value);
            Assert.AreEqual(lineRefs[79], symbols["LINE79"].Value);
        }
    }
}
