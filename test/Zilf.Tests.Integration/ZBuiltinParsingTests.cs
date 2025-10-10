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
using System.Linq;

namespace Zilf.Tests.Integration
{
    [TestClass, TestCategory("Compiler"), TestCategory("Error Messages")]
    public class IsNearMatchTests : IntegrationTestClass
    {
        [TestMethod]
        public async Task Wrong_Argument_Count_Should_Give_Specific_Error()
        {
            // FIRST? expects exactly 1 argument, not 3
            await AssertExpr("<FIRST? 1 2 3>")
                .DoesNotCompileAsync(res => 
                {
                    var errorMessage = string.Join(" ", res.Diagnostics.Select(d => d.GetFormattedMessage()));
                    return errorMessage.Contains("exactly 1 argument");
                });
        }

        [TestMethod]
        public async Task Variable_Argument_Builtin_Should_Give_Specific_Range()
        {
            // SOUND with no arguments (should require 1 or more)
            await AssertExpr("<SOUND>")
                .DoesNotCompileAsync(res =>
                {
                    var errorMessage = string.Join(" ", res.Diagnostics.Select(d => d.GetFormattedMessage()));
                    // Should give specific error about argument count requirements
                    return !errorMessage.Contains("1 to 4 arguments");
                });
        }

        [TestMethod]
        public async Task GVAL_With_Form_Should_Give_Specific_Error()
        {
            await AssertExpr(",<FOO>")
                .WithGlobal("<ROUTINE FOO () 123>")
                .DoesNotCompileAsync("ZIL0113");
        }

        [TestMethod]
        public async Task LOWCORE_TABLE_With_String_Should_Give_Specific_Error()
        {
            await AssertRoutine("", "<LOWCORE-TABLE SERIAL \"BAR\" BAZ>")
                .WithGlobal("<ROUTINE BAZ (X) <>>")
                .DoesNotCompileAsync("ZIL0113");
        }
    }
}