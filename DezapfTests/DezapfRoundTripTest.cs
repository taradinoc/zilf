using System;
using System.Text;
using System.Collections.Generic;
using System.Linq;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System.IO;

namespace DezapfTests
{
    /// <summary>
    /// Summary description for DezapfRoundTripTest
    /// </summary>
    [TestClass]
    public class DezapfRoundTripTest
    {
        public DezapfRoundTripTest()
        {
            //
            // TODO: Add constructor logic here
            //
        }

        private TestContext testContextInstance;

        /// <summary>
        ///Gets or sets the test context which provides
        ///information about and functionality for the current test run.
        ///</summary>
        public TestContext TestContext
        {
            get
            {
                return testContextInstance;
            }
            set
            {
                testContextInstance = value;
            }
        }

        #region Additional test attributes
        //
        // You can use the following additional attributes as you write your tests:
        //
        // Use ClassInitialize to run code before running the first test in the class
        // [ClassInitialize()]
        // public static void MyClassInitialize(TestContext testContext) { }
        //
        // Use ClassCleanup to run code after all tests in a class have run
        // [ClassCleanup()]
        // public static void MyClassCleanup() { }
        //
        // Use TestInitialize to run code before running each test 
        // [TestInitialize()]
        // public void MyTestInitialize() { }
        //
        // Use TestCleanup to run code after each test has run
        // [TestCleanup()]
        // public void MyTestCleanup() { }
        //
        #endregion

        private static int RunZapf(string code, out byte[] zcode)
        {
            string inputFile = Path.GetTempFileName();
            string outputFile = Path.GetTempFileName();
            try
            {
                File.WriteAllText(inputFile, code);
                int rc = Zapf.Program.Main(new string[] { inputFile, outputFile });
                if (rc == 0)
                    zcode = File.ReadAllBytes(outputFile);
                else
                    zcode = null;
                return rc;
            }
            finally
            {
                File.Delete(inputFile);
                File.Delete(outputFile);
            }
        }

        private static int RunDezapf(byte[] zcode, out string code)
        {
            string inputFile = Path.GetTempFileName();
            TextWriter oldOut = Console.Out;
            try
            {
                File.WriteAllBytes(inputFile, zcode);
                MemoryStream mstr = new MemoryStream();
                StreamWriter wtr = new StreamWriter(mstr);
                Console.SetOut(wtr);
                Dezapf.Program.Main(new string[] { inputFile });
                wtr.Flush();
                code = Encoding.Default.GetString(mstr.ToArray());
                return 0; //XXX
            }
            finally
            {
                File.Delete(inputFile);
                Console.SetOut(oldOut);
            }
        }

        [TestMethod]
        public void RoundTripTest_name_z3()
        {
            TestRoundTrip(Resources.name_z3);
        }

        [TestMethod]
        public void RoundTripTest_hello_z3()
        {
            TestRoundTrip(Resources.hello_z3);
        }

        private void TestRoundTrip(byte[] zcode)
        {
            Assert.Inconclusive("DeZapf is not ready for end-to-end testing");

            int rc;

            string code;
            rc = RunDezapf(zcode, out code);
            Assert.AreEqual(0, rc, "Dezapf signaled error");

            byte[] actual;
            rc = RunZapf(code, out actual);
            Assert.AreEqual(0, rc, "Zapf signaled error");

            CollectionAssert.AreEqual(zcode, actual);
        }
    }
}
