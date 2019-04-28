/* Copyright 2010-2018 Jesse McGrew
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

namespace Dezapf.Tests
{
    /// <summary>
    /// Summary description for DezapfRoundTripTest
    /// </summary>
    [TestClass]
    public class DezapfRoundTripTest
    {
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

/*
        static int RunZapf(string code, out byte[] zcode)
        {
            string inputFile = Path.GetTempFileName();
            string outputFile = Path.GetTempFileName();
            try
            {
                File.WriteAllText(inputFile, code);
                int rc = Zapf.Program.Main(new[] { inputFile, outputFile });
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
*/

/*
        static int RunDezapf(byte[] zcode, out string code)
        {
            string inputFile = Path.GetTempFileName();
            TextWriter oldOut = Console.Out;
            try
            {
                File.WriteAllBytes(inputFile, zcode);
                MemoryStream mstr = new MemoryStream();
                StreamWriter wtr = new StreamWriter(mstr);
                Console.SetOut(wtr);
                Dezapf.Program.Main(new[] { inputFile });
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
*/

        /*[TestMethod]
        public void RoundTripTest_name_z3()
        {
            TestRoundTrip(Resources.name_z3);
        }

        [TestMethod]
        public void RoundTripTest_hello_z3()
        {
            TestRoundTrip(Resources.hello_z3);
        }*/

        [TestMethod]
        public void NoTest()
        {
            Assert.Inconclusive("DeZapf is not ready for end-to-end testing");
        }

/*
        void TestRoundTrip(byte[] zcode)
        {
            int rc;

            rc = RunDezapf(zcode, out var code);
            Assert.AreEqual(0, rc, "Dezapf signaled error");

            rc = RunZapf(code, out var actual);
            Assert.AreEqual(0, rc, "Zapf signaled error");

            CollectionAssert.AreEqual(zcode, actual);
        }
*/
    }
}
