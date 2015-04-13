using System;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System.Diagnostics.Contracts;

namespace IntegrationTests
{
    [TestClass]
    public class TableTests
    {
        private static GlobalsAssertionHelper AssertGlobals(params string[] globals)
        {
            Contract.Requires(globals != null && globals.Length > 0);
            Contract.Requires(Contract.ForAll(globals, c => !string.IsNullOrWhiteSpace(c)));

            return new GlobalsAssertionHelper(globals);
        }

        [TestMethod]
        public void BYTE_Elements_Should_Compile_As_Bytes()
        {
            AssertGlobals(
                "<GLOBAL TBL <TABLE 12345 #BYTE 123 #BYTE 45>>")
                .Implies(
                    "<==? <GET ,TBL 0> 12345>",
                    "<==? <GETB ,TBL 2> 123>",
                    "<==? <GETB ,TBL 3> 45>");
        }
    }
}
