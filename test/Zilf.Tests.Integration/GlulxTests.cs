/* Copyright 2010-2025 Tara McGrew
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
using System.Threading.Tasks;

namespace Zilf.Tests.Integration
{
    [TestClass, TestCategory("Compiler")]
    public class GlulxTests : IntegrationTestClass
    {
        [TestMethod]
        public async Task HelloGlulxWorldAsync()
        {
            string code = $@"
<ROUTINE GREET (WHOM)
    <PRINTI ""Hello, "">
    <PRINT .WHOM>
    <PRINTC !\!>
    <CRLF>>

<ROUTINE GO ()
    <GREET ""Glulx world"">
    <QUIT>>";

            const string expectedOutput = "Hello, Glulx world!\n";
            await AssertRaw(code)
                .InGlulx()
                .OutputsAsync(expectedOutput);
        }

        [TestMethod]
        public async Task TestGlulxPropertyDefaultsAsync()
        {
            await AssertRoutine("", @"<TELL N <GETP ,OBJ ,P?FOO>>")
                .InGlulx()
                .WithGlobal("<PROPDEF FOO 123>")
                .WithGlobal("<OBJECT OBJ>")
                .OutputsAsync("123");
        }
    }
}