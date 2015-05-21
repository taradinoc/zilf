﻿using System;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System.Diagnostics.Contracts;
using System.Collections.Generic;
using System.Text;

namespace IntegrationTests
{
    [TestClass]
    public class VariableTests
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
        public void FUNNY_GLOBALS_Should_Allow_Lots_Of_Globals()
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

            AssertRoutine("", myRoutineBody.ToString())
                .WithGlobal(string.Join("\n", myGlobals))
                .Outputs(expectedOutput.ToString());
        }

        [TestMethod]
        public void FUNNY_GLOBALS_Should_Work_With_INC()
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

            AssertRoutine("", myRoutineBody.ToString())
                .WithGlobal(string.Join("\n", myGlobals))
                .Outputs(expectedOutput.ToString());
        }

        [TestMethod]
        public void FUNNY_GLOBALS_Should_Work_With_IGRTR_P()
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

            AssertRoutine("", myRoutineBody.ToString())
                .WithGlobal(string.Join("\n", myGlobals))
                .Outputs(expectedOutput.ToString());
        }

        [TestMethod]
        public void Special_Globals_Should_Always_Be_Hard_Globals()
        {
            const int NumGlobals = 500;

            var myGlobals = new List<string>();

            myGlobals.Add("<FUNNY-GLOBALS?>");

            for (int i = 1; i <= NumGlobals; i++)
                myGlobals.Add(string.Format("<GLOBAL MY-GLOBAL-{0} {0}>", i));

            myGlobals.Add("<GLOBAL HERE <>>");
            myGlobals.Add("<GLOBAL SCORE <>>");
            myGlobals.Add("<GLOBAL MOVES <>>");

            AssertRoutine("", "<>")
                .WithGlobal(string.Join("\n", myGlobals))
                .InV3()
                .GeneratesCodeMatching(@"\.GVAR HERE=.*\.GVAR SCORE=.*\.GVAR MOVES=");
        }

        [TestMethod]
        public void PROPDEF_Referenced_Globals_Should_Always_Be_Hard_Globals()
        {
            const int NumGlobals = 500;

            var myGlobals = new List<string>();

            myGlobals.Add("<FUNNY-GLOBALS?>");

            for (int i = 1; i <= NumGlobals; i++)
                myGlobals.Add(string.Format("<GLOBAL MY-GLOBAL-{0} {0}>", i));

            myGlobals.Add("<PROPDEF GLOB <> (GLOB REF G:GLOBAL = 1 <GLOBAL .G>)>");
            myGlobals.Add("<OBJECT FOO (GLOB REF MY-GLOBAL-400)>");

            AssertRoutine("", "<>")
                .WithGlobal(string.Join("\n", myGlobals))
                .GeneratesCodeMatching(@"\.GVAR MY-GLOBAL-400=");
        }

        [TestMethod]
        public void Parameter_Globals_Should_Always_Be_Hard_Globals()
        {
            const int NumGlobals = 500;

            var myGlobals = new List<string>();

            myGlobals.Add("<FUNNY-GLOBALS?>");

            for (int i = 1; i <= NumGlobals; i++)
                myGlobals.Add(string.Format("<GLOBAL MY-GLOBAL-{0} {0}>", i));

            myGlobals.Add("<ROUTINE PRINTGN (GN) <PRINTN .GN>>");

            AssertRoutine("", "<PRINTGN MY-GLOBAL-400>")
                .WithGlobal(string.Join("\n", myGlobals))
                .GeneratesCodeMatching(@"\.GVAR MY-GLOBAL-400=");
        }
    }
}