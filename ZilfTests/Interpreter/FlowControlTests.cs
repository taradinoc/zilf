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
using Zilf.Interpreter;
using Zilf.Interpreter.Values;
using Zilf.Language;

namespace ZilfTests.Interpreter
{
    [TestClass, TestCategory("Interpreter"), TestCategory("Flow Control")]
    public class FlowControlTests
    {
        [TestMethod]
        public void COND_Requires_At_Least_One_Clause()
        {
            TestHelpers.EvalAndCatch<InterpreterError>("<COND>",
                ex => ex.Message.Contains("1 or more args"));
        }

        [TestMethod]
        public void COND_Should_Reject_Empty_Clauses()
        {
            TestHelpers.EvalAndCatch<InterpreterError>("<COND ()>",
                ex => !ex.Message.Contains("1 or more args"));
        }

        [TestMethod]
        public void VERSION_P_Should_Reject_Empty_Clauses()
        {
            TestHelpers.EvalAndCatch<InterpreterError>("<VERSION? ()>");
        }

        [TestMethod]
        public void RETURN_And_AGAIN_Require_An_Activation()
        {
            var ctx = new Context();

            TestHelpers.Evaluate(ctx, "<DEFINE FOO1 () <RETURN 123>>");
            TestHelpers.EvalAndCatch<InterpreterError>(ctx, "<FOO1>");

            TestHelpers.Evaluate(ctx, "<DEFINE FOO2 (\"AUX\" (BLAH <>)) <COND (.BLAH <>) (T <SET BLAH T> <AGAIN>)>>");
            TestHelpers.EvalAndCatch<InterpreterError>(ctx, "<FOO2>");

            // these are OK with a PROG added
            TestHelpers.Evaluate(ctx, "<DEFINE FOO3 () <PROG () <RETURN 123>>>");
            TestHelpers.EvalAndAssert(ctx, "<FOO3>", new ZilFix(123));

            TestHelpers.Evaluate(ctx, "<DEFINE FOO4 (\"AUX\" (BLAH <>)) <PROG () <COND (.BLAH <>) (T <SET BLAH T> <AGAIN>)>>>");
            TestHelpers.EvalAndAssert(ctx, "<FOO4>", ctx.FALSE);

            // but not if the PROG is outside the function
            TestHelpers.EvalAndCatch<InterpreterError>(ctx, "<PROG () <FOO1>>");

            TestHelpers.EvalAndCatch<InterpreterError>(ctx, "<PROG () <FOO2>>");
        }

        [TestMethod]
        public void PROG_Can_Bind_An_Activation()
        {
            var ctx = new Context();

            TestHelpers.Evaluate(ctx, "<DEFINE FOO () <* 10 <PROG P-ACT () <* 2 <BAR .P-ACT>>>>>");
            TestHelpers.Evaluate(ctx, "<DEFINE BAR (A) <RETURN 123 .A>>");
            TestHelpers.EvalAndAssert(ctx, "<FOO>", new ZilFix(1230));
        }

        [TestMethod]
        public void PROG_Requires_A_Body()
        {
            TestHelpers.EvalAndCatch<ArgumentDecodingError>("<PROG ()>", ex => !ex.Message.Contains("???"));
            TestHelpers.EvalAndCatch<ArgumentDecodingError>("<PROG A ()>", ex => !ex.Message.Contains("???"));
            TestHelpers.EvalAndCatch<ArgumentDecodingError>("<PROG (A) #DECL ((A) FIX)>", ex => !ex.Message.Contains("???"));
        }

        [TestMethod]
        public void PROG_Sets_DECLs_From_ADECLs()
        {
            TestHelpers.EvalAndCatch<DeclCheckError>("<PROG ((A:FIX NOT-A-FIX)) <>>");
            TestHelpers.EvalAndCatch<DeclCheckError>("<PROG (A:FIX) <SET A NOT-A-FIX>>");
        }

        [TestMethod]
        public void PROG_Sets_DECLs_From_Body_DECLs()
        {
            TestHelpers.EvalAndCatch<DeclCheckError>("<PROG ((A NOT-A-FIX)) #DECL ((A) FIX) <>>");
            TestHelpers.EvalAndCatch<DeclCheckError>("<PROG (A) #DECL ((A) FIX) <SET A NOT-A-FIX>>");
        }

        [TestMethod]
        public void PROG_Rejects_Conflicting_DECLs()
        {
            TestHelpers.EvalAndCatch<InterpreterError>("<PROG (A:FIX) #DECL ((A) LIST) <>>");
            TestHelpers.EvalAndCatch<InterpreterError>("<PROG (A) #DECL ((A) FIX (A) LIST) <>>");
        }
    }
}
