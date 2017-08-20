/* Copyright 2010-2017 Jesse McGrew
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

<ROUTINE GO ()
    <PRINTI ""Hello, world!"">
    <CRLF>
    <QUIT>>";

            const string expectedOutput = "Hello, world!\n";
            ZlrHelper.RunAndAssert(code, null, expectedOutput);
        }

        [TestMethod]
        public void HelloWorld_V6()
        {
            const string code = @"
<VERSION 6>

<ROUTINE GO ()
    <PRINTI ""Hello, world!"">
    <CRLF>
    <QUIT>>";
            const string expectedOutput = "Hello, world!\n";
            ZlrHelper.RunAndAssert(code, null, expectedOutput);
        }

        [TestMethod]
        public void HelloWorld_V7()
        {
            const string code = @"
<VERSION 7>

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

<ROUTINE GO ()
    <PRINTI ""Hello, world!"">
    <CRLF>
    <QUIT>>";

            const string expectedOutput = "Hello, world!\n";
            ZlrHelper.RunAndAssert(code, null, expectedOutput);
        }
    }
}
