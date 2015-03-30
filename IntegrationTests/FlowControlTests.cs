using System;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System.Diagnostics.Contracts;

namespace IntegrationTests
{
    [TestClass]
    public class FlowControlTests
    {
        private static RoutineAssertionHelper AssertRoutine(string argSpec, string body)
        {
            Contract.Requires(argSpec != null);
            Contract.Requires(!string.IsNullOrWhiteSpace(body));

            return new RoutineAssertionHelper(argSpec, body);
        }

        [TestMethod]
        public void TestDO_Up_Fixes()
        {
            AssertRoutine("", "<DO (I 1 5) <PRINTN .I> <CRLF>>")
                .Outputs("1\n2\n3\n4\n5\n");
        }

        [TestMethod]
        public void TestDO_Down_Fixes()
        {
            AssertRoutine("", "<DO (I 5 1) <PRINTN .I> <CRLF>>")
                .Outputs("5\n4\n3\n2\n1\n");
        }

        [TestMethod]
        public void TestDO_Up_Fixes_By2()
        {
            AssertRoutine("", "<DO (I 1 5 2) <PRINTN .I> <CRLF>>")
                .Outputs("1\n3\n5\n");
        }

        [TestMethod]
        public void TestDO_Down_Fixes_By2()
        {
            AssertRoutine("", "<DO (I 5 1 -2) <PRINTN .I> <CRLF>>")
                .Outputs("5\n3\n1\n");
        }

        [TestMethod]
        public void TestDO_Up_Fixes_ByN()
        {
            AssertRoutine("\"AUX\" (N 2)", "<DO (I 1 5 .N) <PRINTN .I> <CRLF>>")
                .Outputs("1\n3\n5\n");
        }

        [TestMethod]
        public void TestDO_Up_Fixes_CalculateInc()
        {
            AssertRoutine("", "<DO (I 1 16 <* 2 .I>) <PRINTN .I> <CRLF>>")
                .Outputs("1\n2\n4\n8\n16\n");
        }

        [TestMethod]
        public void TestDO_Up_Forms()
        {
            AssertRoutine("", "<DO (I <FOO> <BAR .I>) <PRINTN .I> <CRLF>>")
                .WithGlobal("<ROUTINE FOO () <PRINTI \"FOO\"> <CRLF> 7>")
                .WithGlobal("<ROUTINE BAR (I) <PRINTI \"BAR\"> <CRLF> <G? .I 9>>")
                .Outputs("FOO\nBAR\n7\nBAR\n8\nBAR\n9\nBAR\n");
        }
    }
}
