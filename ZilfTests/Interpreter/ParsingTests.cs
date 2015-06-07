using Microsoft.VisualStudio.TestTools.UnitTesting;
using Zilf;
using Zilf.Interpreter;
using Zilf.Interpreter.Values;

namespace ZilfTests.Interpreter
{
    [TestClass]
    public class ParsingTests
    {
        [TestMethod]
        public void TestADECL()
        {
            var ctx = new Context();

            string[][] testCases = {
                                       new[] { "FOO", "BAR" },
                                       new[] { "(1 2 3)", "LIST" },
                                       new[] { "BLAH", "<1 2 3>" },
                                       new[] { "(1 2 3)", "<1 2 3>" },
                                       new[] { ".FOO", "BAR" },
                                       new[] { ",FOO", "BAR" },
                                       new[] { "'FOO", "BAR" },
                                   };

            foreach (var tc in testCases)
            {
                var first = tc[0];
                var second = tc[1];

                var firstValue = Program.Evaluate(ctx, "'" + first, true);
                var secondValue = Program.Evaluate(ctx, "'" + second, true);
                var combined = Program.Evaluate(ctx, string.Format("<QUOTE {0}:{1}>", first, second), true);

                Assert.IsInstanceOfType(combined, typeof(ZilAdecl));

                var adecl = (ZilAdecl)combined;

                Assert.AreEqual(firstValue, adecl.First);
                Assert.AreEqual(secondValue, adecl.Second);
            }
        }
    }
}
