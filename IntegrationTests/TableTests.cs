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

        [TestMethod]
        public void ITABLE_Multi_Element_Initializers_Should_Repeat_N_Times()
        {
            AssertGlobals(
                "<GLOBAL TBL1 <ITABLE 2 1 2 3>>",
                "<GLOBAL TBL2 <ITABLE 3 9 8 7 6>>")
                .Implies(
                    "<==? <GET ,TBL1 0> 1>",
                    "<==? <GET ,TBL1 1> 2>",
                    "<==? <GET ,TBL1 2> 3>",
                    "<==? <GET ,TBL1 3> 1>",
                    "<==? <GET ,TBL1 4> 2>",
                    "<==? <GET ,TBL1 5> 3>");
        }
    }
}
