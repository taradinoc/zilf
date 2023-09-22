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
using System.Text;
using System.Threading.Tasks;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Zilf.Tests.Integration
{
    [TestClass, TestCategory("Compiler")]
    public class VariableTests : IntegrationTestClass
    {
        [TestMethod]
        public async Task FUNNY_GLOBALS_Should_Allow_Lots_Of_Globals()
        {
            const int NumGlobals = 500;

            var myGlobals = new List<string>();
            var myRoutineBody = new StringBuilder();
            var expectedOutput = new StringBuilder();

            myGlobals.Add("<FUNNY-GLOBALS?>");

            for (int i = 1; i <= NumGlobals; i++)
            {
                myGlobals.Add(string.Format("<GLOBAL MY-GLOBAL-{0} {0}>", i));

                myRoutineBody.AppendFormat("<SETG MY-GLOBAL-{0} <+ ,MY-GLOBAL-{0} 1000>> <PRINTN ,MY-GLOBAL-{0}> <CRLF>\n", i);

                expectedOutput.Append(i + 1000);
                expectedOutput.Append('\n');
            }

            await AssertRoutine("", myRoutineBody.ToString())
                .WithGlobal(string.Join("\n", myGlobals))
                .OutputsAsync(expectedOutput.ToString());
        }

        [TestMethod]
        public async Task FUNNY_GLOBALS_Should_Work_With_INC()
        {
            const int NumGlobals = 500;

            var myGlobals = new List<string>();
            var myRoutineBody = new StringBuilder();
            var expectedOutput = new StringBuilder();

            myGlobals.Add("<FUNNY-GLOBALS?>");

            for (int i = 1; i <= NumGlobals; i++)
            {
                myGlobals.Add(string.Format("<GLOBAL MY-GLOBAL-{0} {0}>", i));

                myRoutineBody.AppendFormat("<INC MY-GLOBAL-{0}> <PRINTN <INC MY-GLOBAL-{0}>> <CRLF>\n", i);

                expectedOutput.Append(i + 2);
                expectedOutput.Append('\n');
            }

            await AssertRoutine("", myRoutineBody.ToString())
                .WithGlobal(string.Join("\n", myGlobals))
                .OutputsAsync(expectedOutput.ToString());
        }

        [TestMethod]
        [TestCategory("Slow")]
        public async Task FUNNY_GLOBALS_Should_Work_With_IGRTR_P()
        {
            const int NumGlobals = 500;

            var myGlobals = new List<string>();
            var myRoutineBody = new StringBuilder();
            var expectedOutput = new StringBuilder();

            myGlobals.Add("<FUNNY-GLOBALS?>");

            for (int i = 1; i <= NumGlobals; i++)
            {
                myGlobals.Add(string.Format("<GLOBAL MY-GLOBAL-{0} {0}>", i));

                myRoutineBody.AppendFormat("<COND (<IGRTR? MY-GLOBAL-{0} 400> <PRINTN ,MY-GLOBAL-{0}> <CRLF>)>\n", i);

                if (i >= 400)
                {
                    expectedOutput.Append(i + 1);
                    expectedOutput.Append('\n');
                }
            }

            await AssertRoutine("", myRoutineBody.ToString())
                .WithGlobal(string.Join("\n", myGlobals))
                .OutputsAsync(expectedOutput.ToString());
        }

        [TestMethod]
        public async Task Assigned_FUNNY_GLOBALS_Should_Work_In_Value_Context()
        {
            const int NumGlobals = 500;

            var myGlobals = new List<string> { "<FUNNY-GLOBALS?>" };

            for (int i = 1; i <= NumGlobals; i++)
                myGlobals.Add($"<GLOBAL MY-GLOBAL-{i} {i}>");

            myGlobals.Add("<GLOBAL VARIABLE 4>");

            await AssertRoutine("", @"<COND (<==? <SETG VARIABLE <- ,VARIABLE 1>> 3> <TELL ""Three."" CR>)>")
                .WithGlobal(string.Join("\n", myGlobals))
                .OutputsAsync("Three.\n");
        }

        [TestMethod]
        public async Task Special_Globals_Should_Always_Be_Hard_Globals()
        {
            const int NumGlobals = 500;

            var myGlobals = new List<string> { "<FUNNY-GLOBALS?>" };

            for (int i = 1; i <= NumGlobals; i++)
                myGlobals.Add(string.Format("<GLOBAL MY-GLOBAL-{0} {0}>", i));

            myGlobals.Add("<GLOBAL HERE <>>");
            myGlobals.Add("<GLOBAL SCORE <>>");
            myGlobals.Add("<GLOBAL MOVES <>>");

            await AssertRoutine("", "<>")
                .WithGlobal(string.Join("\n", myGlobals))
                .InV3()
                .GeneratesCodeMatchingAsync(@"\.GVAR HERE=.*\.GVAR SCORE=.*\.GVAR MOVES=");
        }

        [TestMethod]
        public async Task PROPDEF_Referenced_Globals_Should_Always_Be_Hard_Globals()
        {
            const int NumGlobals = 500;

            var myGlobals = new List<string> { "<FUNNY-GLOBALS?>" };

            for (int i = 1; i <= NumGlobals; i++)
                myGlobals.Add(string.Format("<GLOBAL MY-GLOBAL-{0} {0}>", i));

            myGlobals.Add("<PROPDEF GLOB <> (GLOB REF G:GLOBAL = 1 <GLOBAL .G>)>");
            myGlobals.Add("<OBJECT FOO (GLOB REF MY-GLOBAL-400)>");

            await AssertRoutine("", "<>")
                .WithGlobal(string.Join("\n", myGlobals))
                .GeneratesCodeMatchingAsync(@"\.GVAR MY-GLOBAL-400=");
        }

        [TestMethod]
        public async Task Parameter_Globals_Should_Always_Be_Hard_Globals()
        {
            const int NumGlobals = 500;

            var myGlobals = new List<string> { "<FUNNY-GLOBALS?>" };

            for (int i = 1; i <= NumGlobals; i++)
                myGlobals.Add(string.Format("<GLOBAL MY-GLOBAL-{0} {0}>", i));

            myGlobals.Add(@"<ROUTINE PRINTGN (GN) <PRINTN .GN>>");

            await AssertRoutine("", @"<PRINTGN MY-GLOBAL-400>")
                .WithGlobal(string.Join("\n", myGlobals))
                .WithWarnings("ZIL0200" /* bare atom as global index */)
                .GeneratesCodeMatchingAsync(@"\.GVAR MY-GLOBAL-400=");
        }

        [TestMethod]
        public async Task DEFINE_GLOBALS_Should_Work()
        {
            await AssertRoutine("",
                "<PRINTN <MY-WORD>> <CRLF> " +
                "<PRINTN <MY-BYTE>> <CRLF> " +
                "<MY-WORD 12345> " +
                "<MY-BYTE 67> " +
                "<PRINTN <MY-WORD>> <CRLF> " +
                "<PRINTN <MY-BYTE>> <CRLF> ")
                .WithGlobal("<DEFINE-GLOBALS TEST-GLOBALS (MY-WORD 32767) (MY-BYTE BYTE 255) (HAS-ADECL:FIX 0)>")
                .OutputsAsync("32767\n255\n12345\n67\n");
        }

        [TestMethod]
        public async Task GLOBAL_And_CONSTANT_Should_Work_With_ADECLs()
        {
            await AssertRoutine("", "<>")
                .WithGlobal("<GLOBAL FOO:FIX 12>")
                .WithGlobal("<CONSTANT BAR:FIX 34>")
                .CompilesAsync();
        }

        [TestMethod]
        public async Task Global_Can_Be_Initialized_To_A_Global_Index_With_Warning()
        {
            await AssertRoutine("", "<PRINTN ,BAR>")
                .WithGlobal("<GLOBAL GLOBAL-16 <>>")
                .WithGlobal("<GLOBAL GLOBAL-17 <>>")
                .WithGlobal("<GLOBAL FOO <>>")
                .WithGlobal("<GLOBAL BAR FOO>")
                .WithWarnings()
                .OutputsAsync("18");
        }

        [TestMethod]
        public async Task Locals_Can_Have_The_Same_Names_As_Globals()
        {
            // global can be accessed with SETG and GVAL
            await AssertRoutine("", "<BUMP-IT 111> ,FOO")
                .WithGlobal("<GLOBAL FOO 123>")
                .WithGlobal("<ROUTINE BUMP-IT (FOO) <SETG FOO <+ ,FOO .FOO>>>")
                .GivesNumberAsync("234");

            // PROG local shadows ROUTINE local
            await AssertRoutine("", "<BUMP-IT 111> ,FOO")
                .WithGlobal("<GLOBAL FOO 123>")
                .WithGlobal("<ROUTINE BUMP-IT (FOO) <PROG ((FOO 1000)) <SETG FOO <+ ,FOO .FOO>>>>")
                .GivesNumberAsync("1123");
        }

        [TestMethod]
        public async Task Unused_Locals_Should_Warn()
        {
            const string SWarningCode = "ZIL0210";

            // unreferenced, uninitialized routine local => warn
            await AssertRoutine(@"""AUX"" X", @"<>")
                .WithWarnings(SWarningCode)
                .CompilesAsync();

            // add a read => OK
            await AssertRoutine(@"""AUX"" X", @".X")
                .WithoutWarnings()
                .CompilesAsync();

            // unreferenced routine local, initialized to routine call => OK
            await AssertRoutine(@"""AUX"" (X <FOO>)", @"<>")
                .WithGlobal(@"<ROUTINE FOO () <TELL ""hi""> 123>")
                .WithoutWarnings()
                .CompilesAsync();

            // unreferenced, uninitialized BIND local => warn
            await AssertRoutine("", @"<BIND (X) <>>")
                .WithWarnings(SWarningCode)
                .CompilesAsync();

            // add a read => OK
            await AssertRoutine("", @"<BIND (X) .X>")
                .WithoutWarnings()
                .CompilesAsync();

            // unreferenced BIND local, initialized to routine call => OK
            await AssertRoutine("", @"<BIND ((X <FOO>)) <>>")
                .WithGlobal(@"<ROUTINE FOO () <TELL ""hi""> 123>")
                .WithoutWarnings()
                .CompilesAsync();
        }

    }
}
