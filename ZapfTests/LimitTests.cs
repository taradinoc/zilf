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
using System.Text;

namespace ZapfTests
{
    [TestClass]
    public class LimitTests
    {
        [TestMethod]
        public void Too_Many_GVARs_Should_Cause_An_Error()
        {
            const string SCodeTemplate = @"
    .NEW 5

GLOBAL:: .TABLE
{0}
    .ENDT

    .FUNCT GO
START::
    QUIT

    .END";

            var tooManyGvars = new StringBuilder();

            for (int i = 0; i < 500; i++)
                tooManyGvars.AppendFormat("    .GVAR MY-GLOBAL-{0}={0}\n", i);

            var code = string.Format(SCodeTemplate, tooManyGvars);
            Assert.IsFalse(TestHelper.Assemble(code), "Should not compile.");
        }

        [TestMethod]
        public void Too_Many_OBJECTs_Should_Cause_An_Error()
        {
            const string SCodeTemplate = @"
OBJECT:: .TABLE
    .WORD 0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0
    .WORD 0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0
    .WORD 0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0
    .WORD 0,0,0,0,0,0,0,0,0,0,0,0,0,0,0
{0}
    .ENDT

VOCAB::
IMPURE::

    .FUNCT GO
START::
    QUIT

    .END";

            var tooManyObjects = new StringBuilder();

            for (int i = 0; i < 500; i++)
                tooManyObjects.AppendFormat("    .OBJECT MY-OBJECT-{0},0,0,0,0,0,0,0\n", i);

            var code = string.Format(SCodeTemplate, tooManyObjects);
            Assert.IsFalse(TestHelper.Assemble(code), "Should not compile.");
        }
    }
}
