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

using System;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Zilf.Interpreter;
using Zilf.Interpreter.Values;
using Zilf.Language;
using Zilf.ZModel;
using Zilf.ZModel.Values;

namespace Zilf.Tests.Interpreter
{
    [TestClass, TestCategory("Interpreter")]
    public class TableTests
    {
        [TestMethod]
        public void TestITABLE()
        {
            var ctx = new Context();
            var table = (ZilTable?)Program.Evaluate(ctx, "<ITABLE 3 2 1 0>", true);

            Assert.IsNotNull(table);
            Assert.AreEqual(9, table!.ElementCount);
        }

        [TestMethod]
        public void TestTABLE()
        {
            var ctx = new Context();
            var table = (ZilTable?)Program.Evaluate(ctx, "<TABLE 3 2 1 0>", true);

            Assert.IsNotNull(table);
            Assert.AreEqual(4, table!.ElementCount);
        }

        [TestMethod]
        public void TestLTABLE()
        {
            var ctx = new Context();
            var table = (ZilTable?)Program.Evaluate(ctx, "<LTABLE 3 2 1 0>", true);

            Assert.IsNotNull(table);
            Assert.AreEqual(5, table!.ElementCount);
        }

#region Bytey-Wordy for Z-Machine

        [TestMethod]
        public void ByteCount_BasicTables()
        {
            var ctx = new Context();

            // Word table: 4 words = 8 bytes
            var wordTable = (ZilTable)Program.Evaluate(ctx, "<TABLE 1 2 3 4>", true)!;
            Assert.AreEqual(4, wordTable.ElementCount);
            Assert.AreEqual(8, wordTable.ByteCount);

            // Byte table: 3 bytes = 3 bytes
            var byteTable = (ZilTable)Program.Evaluate(ctx, "<TABLE (BYTE) 5 6 7>", true)!;
            Assert.AreEqual(3, byteTable.ElementCount);
            Assert.AreEqual(3, byteTable.ByteCount);

            // LTABLE (word length): 1 word length + 3 words = 2 + 6 = 8 bytes
            var ltableWord = (ZilTable)Program.Evaluate(ctx, "<LTABLE 1 2 3>", true)!;
            Assert.AreEqual(4, ltableWord.ElementCount); // includes length element
            Assert.AreEqual(8, ltableWord.ByteCount);

            // LTABLE (byte length + bytes): 1 byte length + 3 bytes = 4 bytes
            var ltableByte = (ZilTable)Program.Evaluate(ctx, "<LTABLE (BYTE) 9 8 7>", true)!;
            Assert.AreEqual(4, ltableByte.ElementCount);
            Assert.AreEqual(4, ltableByte.ByteCount);
        }

        [TestMethod]
        public void Read_Word_And_Byte_Aligned_Unaligned()
        {
            var ctx = new Context();

            var wordTable = (ZilTable)Program.Evaluate(ctx, "<TABLE 10 20 30>", true)!;

            // Reading words at word offsets
            TestHelpers.AssertStructurallyEqual(new ZilFix(10), wordTable.GetWord(ctx, 0));
            TestHelpers.AssertStructurallyEqual(new ZilFix(20), wordTable.GetWord(ctx, 1));

            // Byte reads at word boundaries should be unaligned
            Assert.ThrowsException<UnalignedTableReadException>(() => wordTable.GetByte(ctx, 0));
            Assert.ThrowsException<UnalignedTableReadException>(() => wordTable.GetByte(ctx, 1)); // inside a word

            var byteTable = (ZilTable)Program.Evaluate(ctx, "<TABLE (BYTE) 5 6 7>", true)!;
            TestHelpers.AssertStructurallyEqual(new ZilFix(5), byteTable.GetByte(ctx, 0));
            TestHelpers.AssertStructurallyEqual(new ZilFix(7), byteTable.GetByte(ctx, 2));
            Assert.ThrowsException<UnalignedTableReadException>(() => byteTable.GetWord(ctx, 0));
        }

        [TestMethod]
        public void Read_LengthPrefix()
        {
            var ctx = new Context();

            // Word-length prefix: readable as a word at offset 0
            var ltableWord = (ZilTable)Program.Evaluate(ctx, "<LTABLE 1 2 3>", true)!;
            TestHelpers.AssertStructurallyEqual(new ZilFix(3), ltableWord.GetWord(ctx, 0));
            Assert.ThrowsException<UnalignedTableReadException>(() => ltableWord.GetByte(ctx, 0));

            // Byte-length prefix: readable as a byte at offset 0
            var ltableByte = (ZilTable)Program.Evaluate(ctx, "<LTABLE (BYTE) 9 8 7>", true)!;
            TestHelpers.AssertStructurallyEqual(new ZilFix(3), ltableByte.GetByte(ctx, 0));
            Assert.ThrowsException<UnalignedTableReadException>(() => ltableByte.GetWord(ctx, 0));
        }

        [TestMethod]
        public void PutByte_SplitsWord_FirstAndSecondByte()
        {
            var ctx = new Context();
            var table = (ZilTable)Program.Evaluate(ctx, "<TABLE 100 200 300>", true)!;

            var beforeBytes = table.ByteCount;

            // Write to first byte of first word (offset 0)
            table.PutByte(ctx, 0, new ZilFix(42));
            Assert.AreEqual(beforeBytes, table.ByteCount, "Splitting a word should keep total bytes constant");
            TestHelpers.AssertStructurallyEqual(new ZilFix(42), table.GetByte(ctx, 0));
            // second byte now zero by default (typed as BYTE)
            var zeroByte = ctx.ChangeType(new ZilFix(0), ctx.GetStdAtom(StdAtom.BYTE));
            TestHelpers.AssertStructurallyEqual(zeroByte, table.GetByte(ctx, 1));
            // Word read at offset 0 should now be unaligned
            Assert.ThrowsException<UnalignedTableReadException>(() => table.GetWord(ctx, 0));

            // Reset table and split by writing to second byte of first word (offset 1)
            table = (ZilTable)Program.Evaluate(ctx, "<TABLE 100 200 300>", true)!;
            beforeBytes = table.ByteCount;
            table.PutByte(ctx, 1, new ZilFix(255));
            Assert.AreEqual(beforeBytes, table.ByteCount);
            TestHelpers.AssertStructurallyEqual(zeroByte, table.GetByte(ctx, 0));
            TestHelpers.AssertStructurallyEqual(new ZilFix(255), table.GetByte(ctx, 1));
            Assert.ThrowsException<UnalignedTableReadException>(() => table.GetWord(ctx, 0));
        }

        [TestMethod]
        public void PutWord_ReplacesTwoBytes()
        {
            var ctx = new Context();
            var table = (ZilTable)Program.Evaluate(ctx, "<TABLE (BYTE) 10 20 30 40>", true)!;
            var bytesBefore = table.ByteCount;

            // Replace bytes at offsets 2 and 3 with a word at word-offset 1
            table.PutWord(ctx, 1, new ZilFix(999));
            Assert.AreEqual(bytesBefore, table.ByteCount, "Merging two bytes into a word keeps byte count same");

            // Word is readable at index 1 (stored as WORD typed value)
            var got = table.GetWord(ctx, 1);
            Assert.IsNotNull(got);
            Assert.IsInstanceOfType<ZilWord>(got);
            TestHelpers.AssertStructurallyEqual(new ZilFix(999), ((ZilWord)got!).Value);
            // Byte reads at offsets 2 and 3 are unaligned now
            Assert.ThrowsException<UnalignedTableReadException>(() => table.GetByte(ctx, 2));
            Assert.ThrowsException<UnalignedTableReadException>(() => table.GetByte(ctx, 3));
        }

        [TestMethod]
        public void LexvLayout_Reads()
        {
            var ctx = new Context();
            var table = (ZilTable)Program.Evaluate(ctx, "<ITABLE 1 (LEXV) 1 2 3>", true)!;

            // Layout: word, byte, byte -> total 4 bytes
            Assert.AreEqual(3, table.ElementCount);
            Assert.AreEqual(4, table.ByteCount);

            TestHelpers.AssertStructurallyEqual(new ZilFix(1), table.GetWord(ctx, 0));
            TestHelpers.AssertStructurallyEqual(new ZilFix(2), table.GetByte(ctx, 2));
            TestHelpers.AssertStructurallyEqual(new ZilFix(3), table.GetByte(ctx, 3));
            Assert.ThrowsException<UnalignedTableReadException>(() => table.GetByte(ctx, 1)); // middle of word
        }

        [TestMethod]
        public void Pattern_Repeating_WordByte()
        {
            var ctx = new Context();
            // Pattern WORD BYTE repeating across values
            var table = (ZilTable)Program.Evaluate(ctx, "<TABLE (PATTERN (WORD BYTE)) 11 22 33 44>", true)!;

            // W B W B -> bytes: 2 + 1 + 2 + 1 = 6
            Assert.AreEqual(4, table.ElementCount);
            Assert.AreEqual(6, table.ByteCount);

            TestHelpers.AssertStructurallyEqual(new ZilFix(11), table.GetWord(ctx, 0));
            TestHelpers.AssertStructurallyEqual(new ZilFix(22), table.GetByte(ctx, 2));
            // Next WORD begins at byte offset 3, so GetWord at offset 1 is unaligned
            Assert.ThrowsException<UnalignedTableReadException>(() => table.GetWord(ctx, 1));
            TestHelpers.AssertStructurallyEqual(new ZilFix(44), table.GetByte(ctx, 5));
        }

        [TestMethod]
        public void ZREST_Aligned_And_Misaligned()
        {
            var ctx = new Context();
            var table = (ZilTable)Program.Evaluate(ctx, "<TABLE 1 2 3>", true)!;

            // Aligned: skip one word (2 bytes)
            var restAligned = table.OffsetByBytes(2);
            TestHelpers.AssertStructurallyEqual(new ZilFix(2), restAligned.GetWord(ctx, 0));

            // Misaligned: skip one byte -> element metadata becomes invalid
            var restMisaligned = table.OffsetByBytes(1);
            // Accessing ElementCount on misaligned offset throws InvalidOperationException (per docs)
            Assert.ThrowsException<System.InvalidOperationException>(() => { var _ = restMisaligned.ElementCount; });
            // But direct unaligned read should also fail
            Assert.ThrowsException<UnalignedTableReadException>(() => restMisaligned.GetByte(ctx, 0));
        }

        [TestMethod]
        public void BoundsChecks_Read_Write()
        {
            var ctx = new Context();
            var table = (ZilTable)Program.Evaluate(ctx, "<TABLE (BYTE) 1 2 3>", true)!;

            // Read past end
            Assert.ThrowsException<UnalignedTableReadException>(() => table.GetByte(ctx, 3));
            // Write past end
            Assert.ThrowsException<ArgumentException>(() => table.PutByte(ctx, 3, new ZilFix(0)));

            var wtable = (ZilTable)Program.Evaluate(ctx, "<TABLE 1 2>", true)!;
            // ZGET uses word indices; index 1 is last valid, 2 is past end
            Assert.ThrowsException<UnalignedTableReadException>(() => wtable.GetWord(ctx, 2));
            Assert.ThrowsException<ArgumentException>(() => wtable.PutWord(ctx, 2, new ZilFix(7)));
        }

#endregion

#region Bytey-Wordy for Glulx

#pragma warning disable IDE0051 // keep tests grouped, even if similar names
        [TestMethod]
        public void ByteCount_BasicTables_Glulx()
        {
            var ctx = new Context();
            ctx.SetZVersion(ZEnvironment.GLULX_ZVERSION);

            var wordTable = (ZilTable)Program.Evaluate(ctx, "<TABLE 1 2 3 4>", true)!;
            Assert.AreEqual(4, wordTable.ElementCount);
            Assert.AreEqual(16, wordTable.ByteCount);

            var byteTable = (ZilTable)Program.Evaluate(ctx, "<TABLE (BYTE) 5 6 7>", true)!;
            Assert.AreEqual(3, byteTable.ElementCount);
            Assert.AreEqual(3, byteTable.ByteCount);

            var ltableWord = (ZilTable)Program.Evaluate(ctx, "<LTABLE 1 2 3>", true)!;
            Assert.AreEqual(4, ltableWord.ElementCount);
            Assert.AreEqual(16, ltableWord.ByteCount);

            var ltableByte = (ZilTable)Program.Evaluate(ctx, "<LTABLE (BYTE) 9 8 7>", true)!;
            Assert.AreEqual(4, ltableByte.ElementCount);
            Assert.AreEqual(4, ltableByte.ByteCount);
        }

        [TestMethod]
        public void Read_Word_And_Byte_Aligned_Unaligned_Glulx()
        {
            var ctx = new Context();
            ctx.SetZVersion(ZEnvironment.GLULX_ZVERSION);

            var wordTable = (ZilTable)Program.Evaluate(ctx, "<TABLE 10 20 30>", true)!;
            TestHelpers.AssertStructurallyEqual(new ZilFix(10), wordTable.GetWord(ctx, 0));
            TestHelpers.AssertStructurallyEqual(new ZilFix(20), wordTable.GetWord(ctx, 1));
            Assert.ThrowsException<UnalignedTableReadException>(() => wordTable.GetByte(ctx, 0));
            Assert.ThrowsException<UnalignedTableReadException>(() => wordTable.GetByte(ctx, 1));
            Assert.ThrowsException<UnalignedTableReadException>(() => wordTable.GetByte(ctx, 2));
            Assert.ThrowsException<UnalignedTableReadException>(() => wordTable.GetByte(ctx, 3));

            var byteTable = (ZilTable)Program.Evaluate(ctx, "<TABLE (BYTE) 5 6 7>", true)!;
            TestHelpers.AssertStructurallyEqual(new ZilFix(5), byteTable.GetByte(ctx, 0));
            TestHelpers.AssertStructurallyEqual(new ZilFix(7), byteTable.GetByte(ctx, 2));
            Assert.ThrowsException<UnalignedTableReadException>(() => byteTable.GetWord(ctx, 0));
        }

        [TestMethod]
        public void Read_LengthPrefix_Glulx()
        {
            var ctx = new Context();
            ctx.SetZVersion(ZEnvironment.GLULX_ZVERSION);

            var ltableWord = (ZilTable)Program.Evaluate(ctx, "<LTABLE 1 2 3>", true)!;
            TestHelpers.AssertStructurallyEqual(new ZilFix(3), ltableWord.GetWord(ctx, 0));
            Assert.ThrowsException<UnalignedTableReadException>(() => ltableWord.GetByte(ctx, 0));

            var ltableByte = (ZilTable)Program.Evaluate(ctx, "<LTABLE (BYTE) 9 8 7>", true)!;
            TestHelpers.AssertStructurallyEqual(new ZilFix(3), ltableByte.GetByte(ctx, 0));
            Assert.ThrowsException<UnalignedTableReadException>(() => ltableByte.GetWord(ctx, 0));
        }

        [TestMethod]
        public void PutByte_SplitsWord_First_And_Last_Byte_Glulx()
        {
            var ctx = new Context();
            ctx.SetZVersion(ZEnvironment.GLULX_ZVERSION);
            var table = (ZilTable)Program.Evaluate(ctx, "<TABLE 100 200 300>", true)!;

            var beforeBytes = table.ByteCount;
            table.PutByte(ctx, 0, new ZilFix(42));
            Assert.AreEqual(beforeBytes, table.ByteCount);
            TestHelpers.AssertStructurallyEqual(new ZilFix(42), table.GetByte(ctx, 0));
            var zeroByte = ctx.ChangeType(new ZilFix(0), ctx.GetStdAtom(StdAtom.BYTE));
            TestHelpers.AssertStructurallyEqual(zeroByte, table.GetByte(ctx, 1));
            TestHelpers.AssertStructurallyEqual(zeroByte, table.GetByte(ctx, 2));
            TestHelpers.AssertStructurallyEqual(zeroByte, table.GetByte(ctx, 3));
            Assert.ThrowsException<UnalignedTableReadException>(() => table.GetWord(ctx, 0));

            table = (ZilTable)Program.Evaluate(ctx, "<TABLE 100 200 300>", true)!;
            beforeBytes = table.ByteCount;
            table.PutByte(ctx, 3, new ZilFix(255));
            Assert.AreEqual(beforeBytes, table.ByteCount);
            TestHelpers.AssertStructurallyEqual(zeroByte, table.GetByte(ctx, 0));
            TestHelpers.AssertStructurallyEqual(zeroByte, table.GetByte(ctx, 1));
            TestHelpers.AssertStructurallyEqual(zeroByte, table.GetByte(ctx, 2));
            TestHelpers.AssertStructurallyEqual(new ZilFix(255), table.GetByte(ctx, 3));
            Assert.ThrowsException<UnalignedTableReadException>(() => table.GetWord(ctx, 0));
        }

        [TestMethod]
        public void PutWord_ReplacesFourBytes_Glulx()
        {
            var ctx = new Context();
            ctx.SetZVersion(ZEnvironment.GLULX_ZVERSION);
            var table = (ZilTable)Program.Evaluate(ctx, "<TABLE (BYTE) 10 20 30 40 50 60 70 80>", true)!;
            var bytesBefore = table.ByteCount;

            table.PutWord(ctx, 1, new ZilFix(999));
            Assert.AreEqual(bytesBefore, table.ByteCount);

            var got = table.GetWord(ctx, 1);
            Assert.IsNotNull(got);
            Assert.IsInstanceOfType<ZilWord>(got);
            TestHelpers.AssertStructurallyEqual(new ZilFix(999), ((ZilWord)got!).Value);

            Assert.ThrowsException<UnalignedTableReadException>(() => table.GetByte(ctx, 4));
            Assert.ThrowsException<UnalignedTableReadException>(() => table.GetByte(ctx, 5));
            Assert.ThrowsException<UnalignedTableReadException>(() => table.GetByte(ctx, 6));
            Assert.ThrowsException<UnalignedTableReadException>(() => table.GetByte(ctx, 7));

            table = (ZilTable)Program.Evaluate(ctx, "<TABLE () #BYTE 0 #BYTE 0 0 0 #BYTE 0>", true)!;
            table.PutWord(ctx, 1, new ZilFix(1234));
        }

        [TestMethod]
        public void Pattern_Repeating_WordByte_Glulx()
        {
            var ctx = new Context();
            ctx.SetZVersion(ZEnvironment.GLULX_ZVERSION);
            var table = (ZilTable)Program.Evaluate(ctx, "<TABLE (PATTERN (WORD BYTE)) 11 22 33 44>", true)!;

            Assert.AreEqual(4, table.ElementCount);
            Assert.AreEqual(10, table.ByteCount);

            TestHelpers.AssertStructurallyEqual(new ZilFix(11), table.GetWord(ctx, 0));
            TestHelpers.AssertStructurallyEqual(new ZilFix(22), table.GetByte(ctx, 4));
            Assert.ThrowsException<UnalignedTableReadException>(() => table.GetWord(ctx, 1));
            TestHelpers.AssertStructurallyEqual(new ZilFix(44), table.GetByte(ctx, 9));
        }

        [TestMethod]
        public void ZREST_Aligned_And_Misaligned_Glulx()
        {
            var ctx = new Context();
            ctx.SetZVersion(ZEnvironment.GLULX_ZVERSION);
            var table = (ZilTable)Program.Evaluate(ctx, "<TABLE 1 2 3>", true)!;

            var restAligned = table.OffsetByBytes(4);
            TestHelpers.AssertStructurallyEqual(new ZilFix(2), restAligned.GetWord(ctx, 0));

            var restMisaligned = table.OffsetByBytes(1);
            Assert.ThrowsException<System.InvalidOperationException>(() => { var _ = restMisaligned.ElementCount; });
            Assert.ThrowsException<UnalignedTableReadException>(() => restMisaligned.GetByte(ctx, 0));
        }

        [TestMethod]
        public void BoundsChecks_Read_Write_Glulx()
        {
            var ctx = new Context();
            ctx.SetZVersion(ZEnvironment.GLULX_ZVERSION);
            var table = (ZilTable)Program.Evaluate(ctx, "<TABLE (BYTE) 1 2 3>", true)!;

            Assert.ThrowsException<UnalignedTableReadException>(() => table.GetByte(ctx, 3));
            Assert.ThrowsException<ArgumentException>(() => table.PutByte(ctx, 3, new ZilFix(0)));

            var wtable = (ZilTable)Program.Evaluate(ctx, "<TABLE 1 2>", true)!;
            Assert.ThrowsException<UnalignedTableReadException>(() => wtable.GetWord(ctx, 2));
            Assert.ThrowsException<ArgumentException>(() => wtable.PutWord(ctx, 2, new ZilFix(7)));
        }
#pragma warning restore IDE0051

#endregion
    }
}
