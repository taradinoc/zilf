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
using System;

namespace Zapf.Tests
{
    [TestClass, TestCategory("Assembler")]
    public class HeaderTests
    {
        static void AssertWordAtOffset(ReadOnlySpan<byte> buffer, int offset, ushort expected)
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
            var buffer = mstr!.ToArray();
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

            Assert.IsTrue(TestHelper.Assemble(SCode, ["-r", "222"], out var mstr));
            var buffer = mstr!.ToArray();
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

            Assert.IsTrue(TestHelper.Assemble(SCode, ["-r", "222"], out var mstr));
            var buffer = mstr!.ToArray();
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

            Assert.IsTrue(TestHelper.Assemble(SCode, ["-r", "222"], out var mstr));
            var buffer = mstr!.ToArray();
            AssertWordAtOffset(buffer, 2, 222);
        }

    [TestMethod]
        public void START_Header_Should_Not_Be_Silently_Truncated_When_GO_Past_64k_In_V3()
        {
            // construct a large file with many .WORD entries to push the GO routine
            // beyond 64k so START would not fit in a 16-bit word.
            var sb = new System.Text.StringBuilder();
            sb.AppendLine("WORDS::");
            sb.AppendLine("GLOBAL::");
            sb.AppendLine("OBJECT::");
            sb.AppendLine("VOCAB::");
            sb.AppendLine("IMPURE::");
            sb.AppendLine("ENDLOD::");

            // emit enough words so that START ends up at or past 0x10000 (65536).
            // Note: a `.FUNCT` inserts at least one byte for the local count, so the
            // label may actually be at 65537 (one byte past 65536). We pick the
            // word count to ensure START >= 0x10000 in either case.
            int words = (65536 - 64) / 2; // 32736
            for (int i = 0; i < words; i++)
                sb.AppendLine("    .WORD 0");

            sb.AppendLine();
            sb.AppendLine("    .FUNCT GO");
            sb.AppendLine("START::");
            sb.AppendLine("    QUIT");
            sb.AppendLine();
            sb.AppendLine("    .END");

            var code = sb.ToString();

            // After the fix, assembly should fail because START does not fit in a
            // 16-bit word. Assert that assembly reports an error (Assemble returns false).
            Assert.IsFalse(TestHelper.Assemble(code, out _));
        }

        [TestMethod]
        public void START_Header_Should_Not_Be_Silently_Truncated_When_GO_Past_64k_In_V5()
        {
            var sb = new System.Text.StringBuilder();
            sb.AppendLine(".NEW 5");
            sb.AppendLine();
            sb.AppendLine("    ; 64 bytes for header");
            // place START as the 4th word in the header (word index 3)
            sb.AppendLine("    .WORD 0,RELEASEID,0,START,0,0,0,0,0,0,0,0,0,0,0,0");
            sb.AppendLine("    .WORD 0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0");

            // emit enough words so that START ends up at or past 0x10000 (65536).
            // Note: in V5 the header is manually laid out and `.FUNCT` still emits
            // the routine prologue byte(s). The label may therefore be at 65537.
            // We pick the word count to ensure START >= 0x10000 and trigger the
            // overflow check.
            int words = (65536 - 64) / 2; // 32736
            for (int i = 0; i < words; i++)
                sb.AppendLine("    .WORD 0");

            sb.AppendLine();
            sb.AppendLine("    .FUNCT GO");
            sb.AppendLine("START::");
            sb.AppendLine("    QUIT");
            sb.AppendLine();
            sb.AppendLine("    .END");

            var code = sb.ToString();

            // After the fix, assembly should fail because START/IMPURE do not fit
            // in a 16-bit word when the header is manually laid out in V5+.
            Assert.IsFalse(TestHelper.Assemble(code, out _));
        }
    }
}
