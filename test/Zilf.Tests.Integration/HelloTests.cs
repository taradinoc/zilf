/* Copyright 2010-2023 Tara McGrew
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

namespace Zilf.Tests.Integration
{
    [TestClass, TestCategory("Compiler")]
    public class HelloTests : IntegrationTestClass
    {
        [DataTestMethod]
        [DataRow("ZIP", DisplayName = "V3")]
        [DataRow("EZIP", DisplayName = "V4")]
        [DataRow("XZIP", DisplayName = "V5")]
        [DataRow("YZIP", DisplayName = "V6")]
        [DataRow("7", DisplayName = "V7")]
        [DataRow("8", DisplayName = "V8")]
        public async System.Threading.Tasks.Task HelloWorldAsync(string zversion)
        {
            string code = $@"
<VERSION {zversion}>

<ROUTINE GREET (WHOM)
    <PRINTI ""Hello, "">
    <PRINT .WHOM>
    <PRINTC !\!>
    <CRLF>>

<ROUTINE GO ()
    <GREET ""world"">
    <QUIT>>";

            const string expectedOutput = "Hello, world!\n";
            await AssertRaw(code).OutputsAsync(expectedOutput);
        }
    }
}
