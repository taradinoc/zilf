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
        public void TestDEFSTRUCT_Bare_Constructor_Call()
        {
            var ctx = new Context();
            var pointAtom = ZilAtom.Parse("POINT", ctx);

            TestHelpers.Evaluate(ctx, "<DEFSTRUCT POINT VECTOR (POINT-X FIX) (POINT-Y FIX)>");

            TestHelpers.EvalAndAssert(ctx, "<MAKE-POINT>",
                ZilHash.Parse(ctx, new ZilObject[] { pointAtom, new ZilVector(new ZilFix(0), new ZilFix(0)) }));
        }

        [TestMethod]
        public void TestDEFSTRUCT_Positional_Constructor_Argument()
        {
            var ctx = new Context();

            TestHelpers.Evaluate(ctx, "<DEFSTRUCT FOO VECTOR (FOO-A ATOM) (FOO-B <OR FIX FALSE>)>");

            TestHelpers.EvalAndAssert(ctx, "<MAKE-FOO BAR>",
                ZilHash.Parse(ctx, new ZilObject[]
                {
                    ZilAtom.Parse("FOO", ctx),
                    new ZilVector(ZilAtom.Parse("BAR", ctx), ctx.FALSE),
                }));
        }

        [TestMethod]
        public void TestDEFSTRUCT_Custom_Boa_Constructor()
        {
            var ctx = new Context();

            TestHelpers.Evaluate(ctx, @"
<SETG NEXT-ID 0>
<DEFSTRUCT RGBA (VECTOR
                 'CONSTRUCTOR
                 ('CONSTRUCTOR MAKE-RGBA ('RED 'GREEN 'BLUE ""OPT"" ('ALPHA 255) ""AUX"" (RGBA-ID '<SETG NEXT-ID <+ ,NEXT-ID 1>>))))
    (RED FIX) (GREEN FIX) (BLUE FIX) (ALPHA FIX) (RGBA-ID FIX)>");

            TestHelpers.EvalAndCatch<InterpreterError>(ctx, "<MAKE-RGBA 127 127>");
            TestHelpers.EvalAndAssert(ctx, "<RED <MAKE-RGBA 10 20 30>>", new ZilFix(10));       // ID 1
            TestHelpers.EvalAndAssert(ctx, "<RGBA-ID <MAKE-RGBA 11 22 33>>", new ZilFix(2));
            TestHelpers.EvalAndAssert(ctx, "<ALPHA <MAKE-RGBA 11 22 33>>", new ZilFix(255));    // ID 3
            TestHelpers.EvalAndAssert(ctx, "<ALPHA <MAKE-RGBA 11 22 33 44>>", new ZilFix(44));  // ID 4
        }

        [TestMethod]
        public void TestDEFSTRUCT_Eval_Args()
        {
            var ctx = new Context();

            TestHelpers.Evaluate(ctx, "<DEFSTRUCT POINT VECTOR (POINT-X FIX) (POINT-Y <OR FIX FORM>)>");

            TestHelpers.EvalAndAssert(ctx, "<MAKE-POINT 'POINT-X <+ 1 2> 'POINT-Y '<+ 3 4>>",
                ZilHash.Parse(ctx, new ZilObject[]
                {
                    ZilAtom.Parse("POINT", ctx),
                    new ZilVector(
                        new ZilFix(3),
                        new ZilForm(new ZilObject[]
                        {
                            ctx.GetStdAtom(StdAtom.Plus),
                            new ZilFix(3),
                            new ZilFix(4),
                        })),
                }));
        }

        [TestMethod]
        public void TestDEFSTRUCT_Explicit_Default_Field_Values()
        {
            var ctx = new Context();

            TestHelpers.Evaluate(ctx, @"
<DEFSTRUCT POINT VECTOR (POINT-X FIX 123) (POINT-Y FIX 456) (POINT-ID FIX <ALLOCATE-ID>)>
<SETG NEXT-ID 1>
<DEFINE ALLOCATE-ID (""AUX"" (R ,NEXT-ID))
    <SETG NEXT-ID <+ ,NEXT-ID 1>>
    .R>");

            TestHelpers.EvalAndAssert(ctx, "<POINT-ID <MAKE-POINT>>", new ZilFix(1));
            TestHelpers.EvalAndAssert(ctx, "<POINT-ID <MAKE-POINT>>", new ZilFix(2));
            TestHelpers.EvalAndAssert(ctx, "<POINT-ID <MAKE-POINT 'POINT-ID 1001>>", new ZilFix(1001));
            TestHelpers.EvalAndAssert(ctx, "<POINT-ID <MAKE-POINT>>", new ZilFix(3));
            TestHelpers.EvalAndAssert(ctx, "<POINT-X <MAKE-POINT 'POINT-Y 0>>", new ZilFix(123));   // ID 4
            TestHelpers.EvalAndAssert(ctx, "<POINT-Y <MAKE-POINT 'POINT-Y 0>>", new ZilFix(0));     // ID 5

            TestHelpers.EvalAndAssert(ctx, "<POINT-ID <MAKE-POINT 11 22>>", new ZilFix(6));
            TestHelpers.EvalAndAssert(ctx, "<POINT-ID <MAKE-POINT 'POINT <MAKE-POINT 111 222 333> 'POINT-Y 200>>", new ZilFix(333));
            TestHelpers.EvalAndAssert(ctx, "<POINT-ID <MAKE-POINT>>", new ZilFix(7));
        }

        [TestMethod]
        public void TestDEFSTRUCT_Implicit_Default_Field_Values()
        {
            var ctx = new Context();

            TestHelpers.Evaluate(ctx, @"
<DEFSTRUCT FOO VECTOR (FOO-FIX FIX) (FOO-STRING STRING) (FOO-ATOM ATOM) (FOO-LIST LIST) (FOO-VECTOR VECTOR) (FOO-MULTI <OR LIST FALSE>)>");

            TestHelpers.EvalAndAssert(ctx, "<FOO-FIX <MAKE-FOO>>", new ZilFix(0));
            TestHelpers.EvalAndAssert(ctx, "<FOO-STRING <MAKE-FOO>>", new ZilString(""));
            TestHelpers.EvalAndAssert(ctx, "<FOO-ATOM <MAKE-FOO>>", ctx.GetStdAtom(StdAtom.SORRY));
            TestHelpers.EvalAndAssert(ctx, "<FOO-LIST <MAKE-FOO>>", new ZilList(null, null));
            TestHelpers.EvalAndAssert(ctx, "<FOO-VECTOR <MAKE-FOO>>", new ZilVector());
            TestHelpers.EvalAndAssert(ctx, "<FOO-MULTI <MAKE-FOO>>", ctx.FALSE);
        }

        // TODO: test 0-based field offsets
        // <DEFSTRUCT POINT (TABLE ('START-OFFSET 0) ('NTH ZGET) ('PUT ZPUT)) (POINT-X FIX 123) (POINT-Y FIX 456) (POINT-ID FIX <ALLOCATE-ID>)>

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
