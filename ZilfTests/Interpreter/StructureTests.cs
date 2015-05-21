using System;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Zilf;

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
