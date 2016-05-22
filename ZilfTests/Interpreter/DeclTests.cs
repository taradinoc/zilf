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
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Zilf.Interpreter;

namespace ZilfTests.Interpreter
{
    [TestClass]
    public class DeclTests
    {
        [TestMethod]
        public void Test_Simple_DECL()
        {
            var ctx = new Context();
            TestHelpers.EvalAndAssert(ctx, "<DECL? 1 FIX>", ctx.TRUE);
            TestHelpers.EvalAndAssert(ctx, "<DECL? \"hi\" STRING>", ctx.TRUE);
            TestHelpers.EvalAndAssert(ctx, "<DECL? FOO STRING>", ctx.FALSE);
        }

        [TestMethod]
        public void Test_OR_DECL()
        {
            var ctx = new Context();
            TestHelpers.EvalAndAssert(ctx, "<DECL? 1 '<OR FIX FALSE>>", ctx.TRUE);
            TestHelpers.EvalAndAssert(ctx, "<DECL? \"hi\" '<OR VECTOR STRING>>", ctx.TRUE);
            TestHelpers.EvalAndAssert(ctx, "<DECL? FOO '<OR STRING FIX>>", ctx.FALSE);
        }

        [TestMethod]
        public void Test_Structure_DECL()
        {
            var ctx = new Context();
            TestHelpers.EvalAndAssert(ctx, "<DECL? '(1) '<LIST FIX>>", ctx.TRUE);
            TestHelpers.EvalAndAssert(ctx, "<DECL? '[\"hi\" 456 789 1011] '<VECTOR STRING FIX [REST FIX]>>", ctx.TRUE);
            TestHelpers.EvalAndAssert(ctx, "<DECL? '(FOO BAR) '<LIST STRING [REST FIX]>>", ctx.FALSE);
        }

        [TestMethod]
        public void Test_QUOTE_DECL()
        {
            var ctx = new Context();
            TestHelpers.EvalAndAssert(ctx, "<DECL? FOO ''FOO>", ctx.TRUE);
            TestHelpers.EvalAndAssert(ctx, "<DECL? FOO ''BAR>", ctx.FALSE);
            TestHelpers.EvalAndAssert(ctx, "<DECL? '<OR FIX FALSE> ''<OR FIX FALSE>>", ctx.TRUE);
            TestHelpers.EvalAndAssert(ctx, "<DECL? 123 ''<OR FIX FALSE>>", ctx.FALSE);
        }

        [TestMethod]
        public void Test_Segment_DECL()
        {
            var ctx = new Context();
            TestHelpers.EvalAndAssert(ctx, "<DECL? '(1 2 3) '<LIST FIX FIX>>", ctx.TRUE);
            TestHelpers.EvalAndAssert(ctx, "<DECL? '(1 2 3) '!<LIST FIX FIX>>", ctx.FALSE);
            TestHelpers.EvalAndAssert(ctx, "<DECL? '(1 2) '!<LIST FIX FIX>>", ctx.TRUE);

            TestHelpers.EvalAndAssert(ctx, "<DECL? '(1 2) '!<LIST [REST FIX FIX]>>", ctx.TRUE);
            TestHelpers.EvalAndAssert(ctx, "<DECL? '(1 2 3) '!<LIST [REST FIX FIX]>>", ctx.FALSE);
            TestHelpers.EvalAndAssert(ctx, "<DECL? '(1 2 3 4) '!<LIST [REST FIX FIX]>>", ctx.TRUE);
        }
    }
}
