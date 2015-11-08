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
using Zilf.Language;
using Zilf.ZModel.Values;

namespace ZilfTests.Interpreter
{
    [TestClass]
    public class StructureTests
    {
        [TestMethod]
        public void TestMEMQ()
        {
            TestHelpers.EvalAndAssert("<MEMQ 5 '(3 4 5 6 7)>",
                new ZilList(new ZilObject[] {
                    new ZilFix(5),
                    new ZilFix(6),
                    new ZilFix(7),
                }));

            TestHelpers.EvalAndAssert("<MEMQ 5 '[3 4 5 6 7]>",
                new ZilVector(new ZilObject[] {
                            new ZilFix(5),
                            new ZilFix(6),
                            new ZilFix(7),
                        }));
        }

        [TestMethod]
        public void TestMEMBER()
        {
            TestHelpers.EvalAndAssert("<MEMBER '(5) '(3 4 (5) 6 7)>",
                new ZilList(new ZilObject[] {
                    new ZilList(new ZilObject[] { new ZilFix(5) }),
                    new ZilFix(6),
                    new ZilFix(7),
                }));

            TestHelpers.EvalAndAssert("<MEMBER '(5) '[3 4 (5) 6 7]>",
                new ZilVector(new ZilObject[] {
                            new ZilList(new ZilObject[] { new ZilFix(5) }),
                            new ZilFix(6),
                            new ZilFix(7),
                        }));
        }

        [TestMethod]
        public void TestILIST()
        {
            TestHelpers.EvalAndAssert("<ILIST 3 123>",
                new ZilList(new ZilObject[] {
                    new ZilFix(123),
                    new ZilFix(123),
                    new ZilFix(123),
                }));
        }

        [TestMethod]
        public void TestIVECTOR()
        {
            TestHelpers.EvalAndAssert("<IVECTOR 3 123>",
                new ZilVector(new ZilObject[] {
                    new ZilFix(123),
                    new ZilFix(123),
                    new ZilFix(123),
                }));
        }

        [TestMethod]
        public void TestDEFSTRUCT_NewObject()
        {
            var ctx = new Context();
            var pointAtom = ZilAtom.Parse("POINT", ctx);

            TestHelpers.EvalAndAssert(ctx, "<DEFSTRUCT POINT VECTOR (POINT-X FIX) (POINT-Y FIX)>",
                pointAtom);

            // construct new object
            TestHelpers.EvalAndAssert(ctx, "<MAKE-POINT 'POINT-X 123 'POINT-Y 456>",
                ZilHash.Parse(ctx, new ZilObject[] { pointAtom, new ZilVector(new ZilFix(123), new ZilFix(456)) }));
            TestHelpers.EvalAndAssert(ctx, "<POINT-Y #POINT [234 567]>",
                new ZilFix(567));
        }

        [TestMethod]
        public void TestDEFSTRUCT_ExistingObject()
        {
            var ctx = new Context();
            var pointAtom = ZilAtom.Parse("POINT", ctx);

            TestHelpers.EvalAndAssert(ctx, "<DEFSTRUCT POINT VECTOR (POINT-X FIX) (POINT-Y FIX)>",
                pointAtom);

            // put values into existing object
            var vector = new ZilVector(new ZilObject[] { ctx.FALSE, ctx.FALSE });
            ctx.SetLocalVal(ZilAtom.Parse("MY-VECTOR", ctx),
                ZilHash.Parse(ctx, new ZilObject[] { pointAtom, vector }));
            TestHelpers.EvalAndAssert(ctx, "<MAKE-POINT 'POINT .MY-VECTOR 'POINT-Y 999 'POINT-X 888>",
                ZilHash.Parse(ctx, new ZilObject[] { pointAtom, vector }));
            Assert.AreEqual(new ZilFix(888), vector[0]);
            Assert.AreEqual(new ZilFix(999), vector[1]);
        }

        [TestMethod]
        public void TestDEFSTRUCT_PerFieldOffsets()
        {
            var ctx = new Context();
            var rpointAtom = ZilAtom.Parse("RPOINT", ctx);

            // per-field offsets
            TestHelpers.EvalAndAssert(ctx, "<DEFSTRUCT RPOINT VECTOR (RPOINT-X FIX 'OFFSET 2) (RPOINT-Y FIX 'OFFSET 1)>",
                rpointAtom);
            TestHelpers.EvalAndAssert(ctx, "<MAKE-RPOINT 'RPOINT-X 123 'RPOINT-Y 456>",
               ZilHash.Parse(ctx, new ZilObject[] { rpointAtom, new ZilVector(new ZilFix(456), new ZilFix(123)) }));
            TestHelpers.EvalAndAssert(ctx, "<RPOINT-Y #RPOINT [234 567]>",
                new ZilFix(234));
        }

        [TestMethod]
        public void TestDEFSTRUCT_NOTYPE()
        {
            var ctx = new Context();

            TestHelpers.Evaluate(ctx, "<DEFSTRUCT POINT (VECTOR 'NOTYPE) (POINT-X FIX) (POINT-Y FIX)>");
            TestHelpers.EvalAndAssert(ctx, "<POINT-X [123 456]>", new ZilFix(123));
            TestHelpers.EvalAndCatch<InterpreterError>(ctx, "<CHTYPE [123 456] POINT>");
        }

        [TestMethod]
        public void TestDEFSTRUCT_Suppress_Constructor()
        {
            var ctx = new Context();

            TestHelpers.Evaluate(ctx, "<DEFSTRUCT POINT (VECTOR 'CONSTRUCTOR) (POINT-X FIX) (POINT-Y FIX)>");
            TestHelpers.EvalAndAssert(ctx, "<GASSIGNED? MAKE-POINT>", ctx.FALSE);
            TestHelpers.EvalAndAssert(ctx, "<POINT-X <CHTYPE [123 456] POINT>>", new ZilFix(123));
        }

        [TestMethod]
        public void TestDEFSTRUCT_InitArgs()
        {
            var ctx = new Context();

            TestHelpers.Evaluate(ctx, "<DEFSTRUCT POINT (TABLE ('INIT-ARGS (PURE))) (POINT-X FIX) (POINT-Y FIX)>");
            var point = TestHelpers.Evaluate(ctx, "<MAKE-POINT 'POINT-X 123 'POINT-Y 456>");

            var table = point.GetPrimitive(ctx);
            Assert.IsInstanceOfType(table, typeof(ZilTable));
            Assert.AreEqual(TableFlags.Pure, ((ZilTable)table).Flags);
        }

        [TestMethod]
        public void REST_Of_One_Character_String_Should_Be_Empty_String()
        {
            TestHelpers.EvalAndAssert("<REST \"x\">", new ZilString(""));
            TestHelpers.EvalAndAssert("<REST <REST \"xx\">>", new ZilString(""));
        }

        [TestMethod]
        public void ILIST_Should_Evaluate_Initializer_Each_Time()
        {
            TestHelpers.EvalAndAssert("<SET X 0> <ILIST 3 '<SET X <+ .X 1>>>",
                new ZilList(new ZilObject[] {
                    new ZilFix(1), new ZilFix(2), new ZilFix(3)
                }));
        }

        [TestMethod]
        public void IVECTOR_Should_Evaluate_Initializer_Each_Time()
        {
            TestHelpers.EvalAndAssert("<SET X 0> <IVECTOR 3 '<SET X <+ .X 1>>>",
                new ZilVector(new ZilObject[] {
                    new ZilFix(1), new ZilFix(2), new ZilFix(3)
                }));
        }

        [TestMethod]
        public void ISTRING_Should_Evaluate_Initializer_Each_Time()
        {
            TestHelpers.EvalAndAssert("<SET X 64> <ISTRING 3 '<ASCII <SET X <+ .X 1>>>>",
                new ZilString("ABC"));
        }
    }
}
