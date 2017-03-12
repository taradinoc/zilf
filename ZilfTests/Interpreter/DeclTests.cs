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

namespace ZilfTests.Interpreter
{
    [TestClass]
    public class DeclTests
    {
        Context ctx;

        [TestInitialize]
        public void TestInitialize()
        {
            ctx = new Context();
        }

        [TestMethod]
        public void Test_Simple_DECL()
        {
            TestHelpers.EvalAndAssert(ctx, "<DECL? 1 FIX>", ctx.TRUE);
            TestHelpers.EvalAndAssert(ctx, "<DECL? \"hi\" STRING>", ctx.TRUE);
            TestHelpers.EvalAndAssert(ctx, "<DECL? FOO STRING>", ctx.FALSE);
        }

        [TestMethod]
        public void Test_OR_DECL()
        {
            TestHelpers.EvalAndAssert(ctx, "<DECL? 1 '<OR FIX FALSE>>", ctx.TRUE);
            TestHelpers.EvalAndAssert(ctx, "<DECL? \"hi\" '<OR VECTOR STRING>>", ctx.TRUE);
            TestHelpers.EvalAndAssert(ctx, "<DECL? FOO '<OR STRING FIX>>", ctx.FALSE);
        }

        [TestMethod]
        public void Test_Structure_DECL()
        {
            TestHelpers.EvalAndAssert(ctx, "<DECL? '(1) '<LIST FIX>>", ctx.TRUE);
            TestHelpers.EvalAndAssert(ctx, "<DECL? '(1) '<LIST ATOM>>", ctx.FALSE);
            TestHelpers.EvalAndAssert(ctx, "<DECL? '<1> '<LIST FIX>>", ctx.FALSE);
            TestHelpers.EvalAndAssert(ctx, "<DECL? '<1> '<<OR FORM LIST> FIX>>", ctx.TRUE);
            TestHelpers.EvalAndAssert(ctx, "<DECL? '<1> '<<OR <PRIMTYPE LIST> <PRIMTYPE STRING>> FIX>>", ctx.TRUE);
            TestHelpers.EvalAndAssert(ctx, "<DECL? '(1) '<<PRIMTYPE LIST> FIX>>", ctx.TRUE);
            TestHelpers.EvalAndAssert(ctx, "<DECL? '<1> '<<PRIMTYPE LIST> FIX>>", ctx.TRUE);
        }

        [TestMethod]
        public void Test_NTH_DECL()
        {
            TestHelpers.EvalAndAssert(ctx, "<DECL? '[\"hi\" 456 789 1011] '<VECTOR STRING [4 FIX]>>", ctx.FALSE);
            TestHelpers.EvalAndAssert(ctx, "<DECL? '[\"hi\" 456 789 1011] '<VECTOR STRING [3 FIX]>>", ctx.TRUE);
            TestHelpers.EvalAndAssert(ctx, "<DECL? '[\"hi\" 456 789 1011] '<VECTOR [3 FIX]>>", ctx.FALSE);
            TestHelpers.EvalAndAssert(ctx, "<DECL? '[\"hi\" 456 789 1011] '<VECTOR STRING [2 FIX]>>", ctx.TRUE);
            TestHelpers.EvalAndAssert(ctx, "<DECL? '[\"hi\" 456 789 1011] '<VECTOR STRING [2 FIX] FIX>>", ctx.TRUE);
            TestHelpers.EvalAndAssert(ctx, "<DECL? '[\"hi\" 456 789 1011] '<VECTOR STRING [2 FIX] ATOM>>", ctx.FALSE);
            TestHelpers.EvalAndAssert(ctx, "<DECL? '(1 MONEY 2 SHOW 3 READY 4 GO) '<LIST [4 FIX ATOM]>>", ctx.TRUE);
            TestHelpers.EvalAndAssert(ctx, "<DECL? '(1 MONEY 2 SHOW 3 READY 4 GO) '<LIST [4 FIX]>>", ctx.FALSE);
            TestHelpers.EvalAndAssert(ctx, "<DECL? '(1 MONEY 2 SHOW 3 READY 4 GO) '<LIST [3 FIX ATOM] FIX ATOM>>", ctx.TRUE);
            TestHelpers.EvalAndAssert(ctx, "<DECL? '(1 MONEY 2 SHOW 3 READY 4 GO) '<LIST [3 FIX ATOM]>>", ctx.TRUE);
        }

        [TestMethod]
        public void Test_REST_DECL()
        {
            TestHelpers.EvalAndAssert(ctx, "<DECL? '[\"hi\" 456 789 1011] '<VECTOR STRING FIX [REST FIX]>>", ctx.TRUE);
            TestHelpers.EvalAndAssert(ctx, "<DECL? '(FOO BAR) '<LIST STRING [REST FIX]>>", ctx.FALSE);
            TestHelpers.EvalAndAssert(ctx, "<DECL? '(FOO BAR) '<LIST ATOM [REST FIX]>>", ctx.FALSE);
            TestHelpers.EvalAndAssert(ctx, "<DECL? '(FOO BAR) '<LIST ATOM ATOM [REST FIX]>>", ctx.TRUE);
        }

        [TestMethod]
        public void Test_OPT_DECL()
        {
            TestHelpers.EvalAndAssert(ctx, "<DECL? '(FOO BAR) '<LIST [OPT FIX FIX] [REST ATOM]>>", ctx.TRUE);
            TestHelpers.EvalAndAssert(ctx, "<DECL? '(1 FOO BAR) '<LIST [OPT FIX FIX] [REST ATOM]>>", ctx.TRUE);
            TestHelpers.EvalAndAssert(ctx, "<DECL? '(1 2 FOO BAR) '<LIST [OPT FIX] [REST ATOM]>>", ctx.FALSE);
            TestHelpers.EvalAndAssert(ctx, "<DECL? '(1 2 FOO BAR) '<LIST [OPT FIX FIX] [REST ATOM]>>", ctx.TRUE);
            TestHelpers.EvalAndAssert(ctx, "<DECL? '(1 2) '<LIST [OPT FIX FIX] [REST ATOM]>>", ctx.TRUE);
        }

        [TestMethod]
        public void Test_QUOTE_DECL()
        {
            TestHelpers.EvalAndAssert(ctx, "<DECL? FOO ''FOO>", ctx.TRUE);
            TestHelpers.EvalAndAssert(ctx, "<DECL? FOO ''BAR>", ctx.FALSE);
            TestHelpers.EvalAndAssert(ctx, "<DECL? '<OR FIX FALSE> ''<OR FIX FALSE>>", ctx.TRUE);
            TestHelpers.EvalAndAssert(ctx, "<DECL? 123 ''<OR FIX FALSE>>", ctx.FALSE);
        }

        [TestMethod]
        public void Test_Segment_DECL()
        {
            TestHelpers.EvalAndAssert(ctx, "<DECL? '(1 2 3) '<LIST FIX FIX>>", ctx.TRUE);
            TestHelpers.EvalAndAssert(ctx, "<DECL? '(1 2 3) '!<LIST FIX FIX>>", ctx.FALSE);
            TestHelpers.EvalAndAssert(ctx, "<DECL? '(1 2) '!<LIST FIX FIX>>", ctx.TRUE);

            TestHelpers.EvalAndAssert(ctx, "<DECL? '(1 2) '!<LIST [REST FIX FIX]>>", ctx.TRUE);
            TestHelpers.EvalAndAssert(ctx, "<DECL? '(1 2 3) '!<LIST [REST FIX FIX]>>", ctx.FALSE);
            TestHelpers.EvalAndAssert(ctx, "<DECL? '(1 2 3 4) '!<LIST [REST FIX FIX]>>", ctx.TRUE);
        }

        [TestMethod]
        public void Test_PUT_DECL()
        {
            // <PUT-DECL atom decl> establishes an alias for the decl
            TestHelpers.EvalAndCatch<InterpreterError>(ctx, "<DECL? T BOOLEAN>");
            TestHelpers.EvalAndAssert(ctx, "<PUT-DECL BOOLEAN '<OR ATOM FALSE>>", ZilAtom.Parse("BOOLEAN", ctx));
            TestHelpers.EvalAndAssert(ctx, "<DECL? T BOOLEAN>", ctx.TRUE);

            /// <see cref="StructureTests.TestOFFSET"/> tests GET-DECL for OFFSETs
        }

        [TestMethod]
        public void Test_GET_DECL()
        {
            // <GET-DECL atom> returns the alias definition, or false
            TestHelpers.EvalAndAssert(ctx, "<GET-DECL BOOLEAN>", ctx.FALSE);
            TestHelpers.Evaluate(ctx, "<PUT-DECL BOOLEAN '<OR ATOM FALSE>>");
            TestHelpers.EvalAndAssert(ctx, "<GET-DECL BOOLEAN>",
                new ZilForm(new[] {
                    ctx.GetStdAtom(StdAtom.OR),
                    ctx.GetStdAtom(StdAtom.ATOM),
                    ctx.GetStdAtom(StdAtom.FALSE)
                }));

            /// <see cref="StructureTests.TestOFFSET"/> tests GET-DECL for OFFSETs
        }

        [TestMethod]
        public void Test_DECL_CHECK()
        {
            // turn off decl checking, which should be on initially
            TestHelpers.EvalAndAssert(ctx, "<DECL-CHECK <>>", ctx.TRUE);

            TestHelpers.Evaluate(ctx, "<GDECL (FOO) FIX>");
            TestHelpers.EvalAndAssert(ctx, "<SETG FOO <>>", ctx.FALSE);

            // back on
            TestHelpers.EvalAndAssert(ctx, "<DECL-CHECK T>", ctx.FALSE);

            TestHelpers.EvalAndCatch<DeclCheckError>(ctx, "<SETG FOO <>>");
        }
    }
}
