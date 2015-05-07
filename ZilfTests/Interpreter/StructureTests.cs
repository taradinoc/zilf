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
    }
}
