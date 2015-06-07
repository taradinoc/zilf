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
using Zilf.Interpreter;
using Zilf.Interpreter.Values;

namespace ZilfTests.Interpreter
{
    [TestClass]
    public class AssocTests
    {
        [TestMethod]
        public void GetProp_Should_Return_Value_Stored_By_PutProp()
        {
            var ctx = new Context();

            TestHelpers.EvalAndAssert(ctx, "<PUTPROP FOO BAR 123>", ZilAtom.Parse("FOO", ctx));
            TestHelpers.EvalAndAssert(ctx, "<GETPROP FOO BAR>", new ZilFix(123));
        }

        [TestMethod]
        public void GetProp_Should_Eval_Arg3_For_Missing_Values()
        {
            var ctx = new Context();

            TestHelpers.EvalAndAssert(ctx, "<GETPROP FOO BAR '<+ 1 2>>", new ZilFix(3));
        }

        [TestMethod]
        public void GetProp_Should_Return_False_For_Missing_Values()
        {
            var ctx = new Context();

            TestHelpers.EvalAndAssert(ctx, "<GETPROP FOO BAR>", ctx.FALSE);
        }

        [TestMethod]
        public void PutProp_Should_Return_Old_Value_When_Clearing()
        {
            var ctx = new Context();

            TestHelpers.EvalAndAssert(ctx, "<PUTPROP FOO BAR 123>", ZilAtom.Parse("FOO", ctx));
            TestHelpers.EvalAndAssert(ctx, "<PUTPROP FOO BAR>", new ZilFix(123));
            TestHelpers.EvalAndAssert(ctx, "<GETPROP FOO BAR>", ctx.FALSE);
        }
    }
}
