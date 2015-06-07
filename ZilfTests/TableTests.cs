using Microsoft.VisualStudio.TestTools.UnitTesting;
using Zilf;
using Zilf.Interpreter;
using Zilf.ZModel;
using Zilf.ZModel.Values;

namespace ZilfTests
{
    [TestClass]
    public class TableTests
    {
        [TestMethod]
        public void TestITABLE()
        {
            var ctx = new Context();
            var table = (ZilTable)Program.Evaluate(ctx, "<ITABLE 3 2 1 0>", true);

            Assert.IsNotNull(table);
            Assert.AreEqual(9, table.ElementCount);
        }

        [TestMethod]
        public void TestTABLE()
        {
            var ctx = new Context();
            var table = (ZilTable)Program.Evaluate(ctx, "<TABLE 3 2 1 0>", true);

            Assert.IsNotNull(table);
            Assert.AreEqual(4, table.ElementCount);
        }
    }
}
