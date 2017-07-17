/* Copyright 2010, 2017 Jesse McGrew
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

using JetBrains.Annotations;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace ZapfTests
{
    [TestClass]
    public class HeaderTests
    {
        static void AssertWordAtOffset([NotNull] byte[] buffer, int offset, ushort expected)
        {
            var actual = (ushort)((buffer[offset] << 8) + buffer[offset + 1]);
            Assert.AreEqual(expected, actual, "Wrong word value at byte offset {0}.", offset);
        }

        [TestMethod]
        public void RELEASEID_Should_Set_Header_Release_In_V3()
        {
            const string SCode = @"
    RELEASEID=111

WORDS::
GLOBAL::
OBJECT::
VOCAB::
IMPURE::
ENDLOD::

    .FUNCT GO
START::
    QUIT

    .END";

            Assert.IsTrue(TestHelper.Assemble(SCode, out var mstr));
            var buffer = mstr.GetBuffer();
            AssertWordAtOffset(buffer, 2, 111);
        }

        [TestMethod]
        public void Option_Should_Work_Instead_Of_RELEASEID_In_V3()
        {
            const string SCode = @"
WORDS::
GLOBAL::
OBJECT::
VOCAB::
IMPURE::
ENDLOD::

    .FUNCT GO
START::
    QUIT

    .END";

            Assert.IsTrue(TestHelper.Assemble(SCode, new[] { "-r", "222" }, out var mstr));
            var buffer = mstr.GetBuffer();
            AssertWordAtOffset(buffer, 2, 222);
        }

        [TestMethod]
        public void Option_Should_Override_RELEASEID_In_V3()
        {
            const string SCode = @"
    RELEASEID=111

WORDS::
GLOBAL::
OBJECT::
VOCAB::
IMPURE::
ENDLOD::

    .FUNCT GO
START::
    QUIT

    .END";

            Assert.IsTrue(TestHelper.Assemble(SCode, new[] { "-r", "222" }, out var mstr));
            var buffer = mstr.GetBuffer();
            AssertWordAtOffset(buffer, 2, 222);
        }

        [TestMethod]
        public void Option_Should_Override_RELEASEID_In_V5()
        {
            const string SCode = @"
    .NEW 5

    RELEASEID=111

    ; 64 bytes for header
    .WORD 0,RELEASEID,0,0,0,0,0,0,0,0,0,0,0,0,0,0
    .WORD 0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0

    .END";

            Assert.IsTrue(TestHelper.Assemble(SCode, new[] { "-r", "222" }, out var mstr));
            var buffer = mstr.GetBuffer();
            AssertWordAtOffset(buffer, 2, 222);
        }
    }
}
