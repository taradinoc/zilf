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
    }
}
