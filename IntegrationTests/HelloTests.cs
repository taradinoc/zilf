using System;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace IntegrationTests
{
    [TestClass]
    public class HelloTests
    {
        [TestMethod]
        public void HelloWorld_V3()
        {
            const string code = @"
<VERSION ZIP>

<ROUTINE GO ()
    <PRINTI ""Hello, world!"">
    <CRLF>
    <QUIT>>";

            const string expectedOutput = "Hello, world!\n";
            ZlrHelper.RunAndAssert(code, null, expectedOutput);
        }

        [TestMethod]
        public void HelloWorld_V4()
        {
            const string code = @"
<VERSION EZIP>

<ROUTINE GO ()
    <PRINTI ""Hello, world!"">
    <CRLF>
    <QUIT>>";

            const string expectedOutput = "Hello, world!\n";
            ZlrHelper.RunAndAssert(code, null, expectedOutput);
        }

        [TestMethod]
        public void HelloWorld_V5()
        {
            const string code = @"
<VERSION XZIP>

<CONSTANT RELEASEID 1>

<ROUTINE GO ()
    <PRINTI ""Hello, world!"">
    <CRLF>
    <QUIT>>";

            const string expectedOutput = "Hello, world!\n";
            ZlrHelper.RunAndAssert(code, null, expectedOutput);
        }

        [TestMethod]
        public void HelloWorld_V8()
        {
            const string code = @"
<VERSION 8>

<CONSTANT RELEASEID 1>

<ROUTINE GO ()
    <PRINTI ""Hello, world!"">
    <CRLF>
    <QUIT>>";

            const string expectedOutput = "Hello, world!\n";
            ZlrHelper.RunAndAssert(code, null, expectedOutput);
        }
    }
}
