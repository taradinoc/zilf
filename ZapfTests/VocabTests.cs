using System;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System.IO;
using Zilf.Common.StringEncoding;

namespace ZapfTests
{
    [TestClass]
    public class VocabTests
    {
        [TestMethod]
        public void Pointers_To_Other_Words_Inside_Vocab_Data_Should_Be_Updated_When_Sorting()
        {
            const string SCode = @"
    .NEW 6

    ; 64 bytes for header
    .WORD 0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0
    .WORD 0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0

    .VOCBEG 8,6

W?ZEBRA::
    .ZWORD ""zebra""
    .WORD W?HORSE
W?HORSE::
    .ZWORD ""horse""
    .WORD W?DONKEY
W?DONKEY::
    .ZWORD ""donkey""
    .WORD W?ZEBRA
W?MULE::
    .ZWORD ""mule""
    .WORD W?MULE

    .VOCEND
    .END";

            MemoryStream mstr;
            Assert.IsTrue(TestHelper.Assemble(SCode, out mstr));

            var buffer = mstr.GetBuffer();

            /* After sorting:
             * donkey  firstEntry + entryLength*0   64  70
             * horse   firstEntry + entryLength*1   72  78
             * mule    firstEntry + entryLength*2   80  86
             * zebra   firstEntry + entryLength*3   88  94
             */
            const int firstEntryAddr = 64;
            const int entryLength = 8;
            const int zwordLength = 6;

            ushort donkeyAddr = firstEntryAddr + entryLength * 0;
            ushort horseAddr = firstEntryAddr + entryLength * 1;
            ushort muleAddr = firstEntryAddr + entryLength * 2;
            ushort zebraAddr = firstEntryAddr + entryLength * 3;

            var encoder = new StringEncoder();

            AssertZword(buffer, encoder, zwordLength, donkeyAddr, "donkey");
            AssertZword(buffer, encoder, zwordLength, horseAddr, "horse");
            AssertZword(buffer, encoder, zwordLength, muleAddr, "mule");
            AssertZword(buffer, encoder, zwordLength, zebraAddr, "zebra");

            AssertWord(buffer, donkeyAddr + zwordLength, zebraAddr);
            AssertWord(buffer, horseAddr + zwordLength, donkeyAddr);
            AssertWord(buffer, muleAddr + zwordLength, muleAddr);
            AssertWord(buffer, zebraAddr + zwordLength, horseAddr);
        }

        static void AssertWord(byte[] buffer, int address, ushort expected)
        {
            var actual = (ushort)((buffer[address] << 8) + buffer[address + 1]);
            Assert.AreEqual(expected, actual, "Data word differs at address {0}", address);
        }

        static void AssertZword(byte[] buffer, StringEncoder encoder, int zwordLength, int address, string expected)
        {
            var expectedBytes = encoder.Encode(expected, zwordLength * 3 / 2, StringEncoderMode.NoAbbreviations);
            System.Diagnostics.Debug.Assert(expectedBytes.Length == zwordLength);

            for (int i = 0; i < zwordLength; i++)
            {
                if (buffer[address + i] != expectedBytes[i])
                    Assert.Fail("Encoded word differs at position {0} (address {1}): expected {2}, found {3}",
                        i, address + i, expectedBytes[i], buffer[address + i]);
            }
        }
    }
}
