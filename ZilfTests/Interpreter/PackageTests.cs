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
using System;
using System.Text;
using System.Collections.Generic;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Zilf.Interpreter;
using Zilf.Interpreter.Values;
using Zilf.Language;
using System.IO;

namespace ZilfTests.Interpreter
{
    [TestClass]
    public class PackageTests
    {
        [TestMethod]
        public void Packages_Can_Be_Defined()
        {
            var ctx = new Context();

            TestHelpers.Evaluate(ctx, @"
<PACKAGE ""FOO"">
<ENDPACKAGE>");

            TestHelpers.EvalAndAssert(ctx, "<TYPE? <GETPROP FOO!-PACKAGE OBLIST> OBLIST>", ctx.GetStdAtom(StdAtom.OBLIST));
        }

        [TestMethod]
        public void Packages_Create_Internal_Lexical_Blocks()
        {
            var ctx = new Context();

            TestHelpers.Evaluate(ctx, @"
<PACKAGE ""FOO"">
<SETG ANSWER 42>
<ENDPACKAGE>");

            TestHelpers.EvalAndAssert(ctx, "<TYPE? <GETPROP IFOO!-FOO!-PACKAGE OBLIST> OBLIST>", ctx.GetStdAtom(StdAtom.OBLIST));

            TestHelpers.EvalAndAssert(ctx, "<GASSIGNED? ANSWER>", ctx.FALSE);
            TestHelpers.EvalAndAssert(ctx, "<GASSIGNED? ANSWER!-IFOO!-FOO!-PACKAGE>", ctx.TRUE);
            TestHelpers.EvalAndAssert(ctx, ",ANSWER!-IFOO!-FOO!-PACKAGE", new ZilFix(42));
        }

        [TestMethod]
        public void Packages_Can_Be_Additively_Defined()
        {
            var ctx = new Context();

            TestHelpers.Evaluate(ctx, @"
<PACKAGE ""FOO"">
<SETG ANSWER 42>
<ENDPACKAGE>

<PACKAGE ""FOO"">
<SETG DBL-ANSWER 84>
<ENDPACKAGE>");

            TestHelpers.EvalAndAssert(ctx, ",ANSWER!-IFOO!-FOO!-PACKAGE", new ZilFix(42));
            TestHelpers.EvalAndAssert(ctx, ",DBL-ANSWER!-IFOO!-FOO!-PACKAGE", new ZilFix(84));
        }

        [TestMethod]
        public void ENTRY_Puts_Atoms_In_External_Lexical_Blocks()
        {
            var ctx = new Context();

            TestHelpers.Evaluate(ctx, @"
<PACKAGE ""FOO"">
<ENTRY ANSWER>
<SETG ANSWER 42>
<ENDPACKAGE>");

            TestHelpers.EvalAndAssert(ctx, "<GASSIGNED? ANSWER>", ctx.FALSE);
            TestHelpers.EvalAndAssert(ctx, "<GASSIGNED? ANSWER!-FOO!-PACKAGE>", ctx.TRUE);
            TestHelpers.EvalAndAssert(ctx, "<GASSIGNED? ANSWER!-IFOO!-FOO!-PACKAGE>", ctx.FALSE);
            TestHelpers.EvalAndAssert(ctx, ",ANSWER!-FOO!-PACKAGE", new ZilFix(42));
        }


        [TestMethod]
        public void ENTRY_Is_Illegal_Outside_PACKAGE()
        {
            var ctx = new Context();
            TestHelpers.EvalAndCatch<InterpreterError>(ctx, "<ENTRY ANSWER>");
        }

        [TestMethod]
        public void USE_Imports_External_Lexical_Blocks()
        {
            var ctx = new Context();

            TestHelpers.Evaluate(ctx, @"
<PACKAGE ""FOO"">
<ENTRY ANSWER>
<SETG ANSWER 42>
<SETG SECRET 12345>
<ENDPACKAGE>
<USE ""FOO"">");

            TestHelpers.EvalAndAssert(ctx, "<GASSIGNED? ANSWER>", ctx.TRUE);
            TestHelpers.EvalAndAssert(ctx, "<GASSIGNED? SECRET>", ctx.FALSE);
        }

        [TestMethod]
        public void USE_Tries_To_Load_Unknown_Package()
        {
            var ctx = new Context();
            ctx.IncludePaths.Add("lib");

            const string FileToIntercept = "FOO.zil";

            ctx.InterceptFileExists = path => Path.GetFileName(path) == FileToIntercept;
            ctx.InterceptOpenFile = (path, writing) =>
            {
                if (Path.GetFileName(path) == FileToIntercept)
                {
                    const string fileContent = @"
<PACKAGE ""FOO"">
<ENTRY ANSWER>
<SETG ANSWER 42>
<ENDPACKAGE>";
                    return new MemoryStream(Encoding.ASCII.GetBytes(fileContent));
                }

                return null;
            };

            TestHelpers.EvalAndAssert(ctx, @"<USE ""FOO""> ,ANSWER", new ZilFix(42));
        }

        [TestMethod]
        public void USE_Fails_If_Unknown_Package_Does_Not_Exist()
        {
            var ctx = new Context();
            ctx.IncludePaths.Add("lib");
            ctx.InterceptFileExists = path => false;
            ctx.InterceptOpenFile = (path, writing) => null;

            TestHelpers.EvalAndCatch<InterpreterError>(ctx, @"<USE ""FOO"">");
        }

        [TestMethod]
        public void USE_Knows_About_Certain_Packages_By_Default()
        {
            var ctx = new Context();
            ctx.IncludePaths.Add("lib");
            ctx.InterceptFileExists = path => false;
            ctx.InterceptOpenFile = (path, writing) => null;

            TestHelpers.EvalAndAssert(ctx, @"<USE ""NEWSTRUC"">", ctx.TRUE);
            TestHelpers.EvalAndAssert(ctx, "<GASSIGNED? ZILCH!-PACKAGE>", ctx.TRUE);
        }

        [TestMethod]
        public void DEFINITIONS_Creates_A_Package_Where_All_Atoms_Are_Exported()
        {
            var ctx = new Context();

            TestHelpers.Evaluate(ctx, @"
<DEFINITIONS ""FOO"">
<SETG ANSWER 42>
<END-DEFINITIONS>");

            TestHelpers.EvalAndAssert(ctx, "<TYPE? <GETPROP FOO!-PACKAGE OBLIST> OBLIST>", ctx.GetStdAtom(StdAtom.OBLIST));

            TestHelpers.EvalAndAssert(ctx, "<GASSIGNED? ANSWER>", ctx.FALSE);
            TestHelpers.EvalAndAssert(ctx, "<GASSIGNED? ANSWER!-FOO!-PACKAGE>", ctx.TRUE);
            TestHelpers.EvalAndAssert(ctx, ",ANSWER!-FOO!-PACKAGE", new ZilFix(42));

            // reset context because we polluted the local oblist with ANSWER
            ctx = new Context();

            TestHelpers.Evaluate(ctx, @"
<DEFINITIONS ""FOO"">
<SETG ANSWER 42>
<END-DEFINITIONS>");

            TestHelpers.EvalAndAssert(ctx, @"<INCLUDE ""FOO""> ,ANSWER", new ZilFix(42));
        }

        [TestMethod]
        public void ENTRY_Is_Illegal_Inside_DEFINITIONS()
        {
            var ctx = new Context();
            TestHelpers.Evaluate(ctx, @"<DEFINITIONS ""FOO"">");

            TestHelpers.EvalAndCatch<InterpreterError>(ctx, "<ENTRY ANSWER>");
        }

        [TestMethod]
        public void USE_Rejects_DEFINITIONS_Packages()
        {
            var ctx = new Context();
            TestHelpers.Evaluate(ctx, @"
<DEFINITIONS ""FOO"">
<END-DEFINITIONS>");

            TestHelpers.EvalAndCatch<InterpreterError>(ctx, @"<USE ""FOO"">");
        }

        [TestMethod]
        public void INCLUDE_Rejects_PACKAGE_Packages()
        {
            var ctx = new Context();
            TestHelpers.Evaluate(ctx, @"
<PACKAGE ""FOO"">
<ENDPACKAGE>");

            TestHelpers.EvalAndCatch<InterpreterError>(ctx, @"<INCLUDE ""FOO"">");
        }

        [TestMethod]
        public void RENTRY_Puts_Atoms_On_Root_ObList()
        {
            var ctx = new Context();
            TestHelpers.Evaluate(ctx, @"
<PACKAGE ""FOO"">
<RENTRY BLAH>");

            TestHelpers.EvalAndAssert(ctx, "<==? BLAH BLAH!->", ctx.TRUE);
        }

        [TestMethod]
        public void ENTRY_Ignores_Atoms_Already_On_External_ObList()
        {
            var ctx = new Context();
            TestHelpers.Evaluate(ctx, @"
<PACKAGE ""FOO"">
BLAH!-FOO!-PACKAGE
<ENTRY BLAH>");

            TestHelpers.EvalAndAssert(ctx, "<==? BLAH BLAH!-FOO!-PACKAGE>", ctx.TRUE);
        }

        [TestMethod]
        public void RENTRY_Ignores_Atoms_Already_On_Root_ObList()
        {
            var ctx = new Context();
            TestHelpers.Evaluate(ctx, @"
BLAH!-
<PACKAGE ""FOO"">
<RENTRY BLAH>");

            TestHelpers.EvalAndAssert(ctx, "<==? BLAH BLAH!->", ctx.TRUE);
        }
    }
}
