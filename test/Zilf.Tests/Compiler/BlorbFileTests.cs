/* Copyright 2010-2026 Tara McGrew
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

using System.IO;
using System.Linq;
using System.Text;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Zilf.Blorb;

namespace Zilf.Tests.Compiler
{
    [TestClass, TestCategory("Compiler")]
    public class BlorbFileTests
    {
        [TestMethod]
        public void WriteTo_WithExecutableAndPicture_WritesExecAndPictEntries()
        {
            var blorb = new BlorbFile();
            var executable = new byte[] { 0x10, 0x20, 0x30 };
            var picture = new byte[] { 0x89, 0x50, 0x4e, 0x47, 0x0d };

            blorb.AddExecutable(executable, isGlulx: false);
            var pictureId = blorb.AddPicture(picture);

            using var stream = new MemoryStream();
            blorb.WriteTo(stream);

            var bytes = stream.ToArray();

            Assert.AreEqual(1, pictureId);
            Assert.AreEqual("FORM", ReadFourCc(bytes, 0));
            Assert.AreEqual(bytes.Length - 8, ReadInt32Be(bytes, 4));
            Assert.AreEqual("IFRS", ReadFourCc(bytes, 8));
            Assert.AreEqual("RIdx", ReadFourCc(bytes, 12));
            Assert.AreEqual(2, ReadInt32Be(bytes, 20));
            Assert.AreEqual("Exec", ReadFourCc(bytes, 24));
            Assert.AreEqual(0, ReadInt32Be(bytes, 28));
            Assert.AreEqual("Pict", ReadFourCc(bytes, 36));
            Assert.AreEqual(1, ReadInt32Be(bytes, 40));

            var executableOffset = ReadInt32Be(bytes, 32);
            var pictureOffset = ReadInt32Be(bytes, 44);

            Assert.AreEqual("ZCOD", ReadFourCc(bytes, executableOffset));
            Assert.AreEqual(executable.Length, ReadInt32Be(bytes, executableOffset + 4));
            CollectionAssert.AreEqual(executable, bytes.Skip(executableOffset + 8).Take(executable.Length).ToArray());

            Assert.AreEqual("PNG ", ReadFourCc(bytes, pictureOffset));
            Assert.AreEqual(picture.Length, ReadInt32Be(bytes, pictureOffset + 4));
            CollectionAssert.AreEqual(picture, bytes.Skip(pictureOffset + 8).Take(picture.Length).ToArray());
        }

        [TestMethod]
        public void WriteTo_WithGlulxExecutable_WritesGlulChunk()
        {
            var blorb = new BlorbFile();
            blorb.AddExecutable(new byte[] { 0xaa, 0xbb }, isGlulx: true);

            using var stream = new MemoryStream();
            blorb.WriteTo(stream);

            var bytes = stream.ToArray();
            var executableOffset = ReadInt32Be(bytes, 32);

            Assert.AreEqual(1, ReadInt32Be(bytes, 20));
            Assert.AreEqual("Exec", ReadFourCc(bytes, 24));
            Assert.AreEqual("GLUL", ReadFourCc(bytes, executableOffset));
        }

        static string ReadFourCc(byte[] bytes, int offset) => Encoding.ASCII.GetString(bytes, offset, 4);

        static int ReadInt32Be(byte[] bytes, int offset) =>
            (bytes[offset] << 24) |
            (bytes[offset + 1] << 16) |
            (bytes[offset + 2] << 8) |
            bytes[offset + 3];
    }
}