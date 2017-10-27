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

using System;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Zilf.Interpreter;
using Zilf.Interpreter.Values;
using Zilf.Language;

namespace Zilf.Tests.Interpreter
{
    [TestClass, TestCategory("Interpreter")]
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

        [TestMethod]
        public void Associations_Should_Not_Keep_Objects_Alive()
        {
            var ctx = new Context();

            var foo = ZilString.FromString("FOO");
            ctx.PutProp(foo, ctx.TRUE, ctx.TRUE);

            var weakRef = new WeakReference<ZilString>(foo);
            // ReSharper disable once RedundantAssignment
            foo = null;

            GC.Collect();

            Assert.IsFalse(weakRef.TryGetTarget(out foo), "Object was not garbage collected");
        }

        // TODO: test that <ASSOCIATIONS> doesn't keep every association alive... GC makes this hard to unit test but it seems to work correctly in release

        /*
        // TODO: interning for numeric values - otherwise every time we calculate or parse a number, it's a different instance
        [TestMethod]
        public void Associations_Should_Work_With_Separately_Calculated_Numbers()
        {
            var ctx = new Context();

            TestHelpers.Evaluate(ctx, "<PUTPROP 10 4 T>");
            TestHelpers.EvalAndAssert(ctx, "<GETPROP <* 5 2> <- 11 7>>", ctx.TRUE);
        }
        */

        [TestMethod]
        public void TestASSOCIATIONS_Et_Al()
        {
            var ctx = new Context();

            TestHelpers.EvalAndCatch<ArgumentCountError>(ctx, "<ASSOCIATIONS FOO>");

            // there should be some initial associations
            var zo = TestHelpers.Evaluate(ctx, "<ASSOCIATIONS>");
            Assert.IsInstanceOfType(zo, typeof(ZilAsoc));

            // clear all associations
            TestHelpers.Evaluate(ctx,
                @"<REPEAT ((A <ASSOCIATIONS>) N)
                    <OR .A <RETURN>>
                    <SET N <NEXT .A>>
                    <PUTPROP <ITEM .A> <INDICATOR .A>>
                    <SET A .N>>");

            // no more associations
            TestHelpers.EvalAndAssert(ctx, "<ASSOCIATIONS>", ctx.FALSE);

            // set one
            TestHelpers.Evaluate(ctx, "<PUTPROP FOO BAR BAZ>");

            // verify
            TestHelpers.EvalAndAssert(ctx, "<TYPE <SET A <ASSOCIATIONS>>>", ctx.GetStdAtom(StdAtom.ASOC));
            TestHelpers.EvalAndAssert(ctx, "<ITEM .A>", ZilAtom.Parse("FOO", ctx));
            TestHelpers.EvalAndAssert(ctx, "<INDICATOR .A>", ZilAtom.Parse("BAR", ctx));
            TestHelpers.EvalAndAssert(ctx, "<AVALUE .A>", ZilAtom.Parse("BAZ", ctx));
            TestHelpers.EvalAndAssert(ctx, "<NEXT .A>", ctx.FALSE);
        }
    }
}
