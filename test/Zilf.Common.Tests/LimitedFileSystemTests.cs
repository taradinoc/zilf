using System;
using System.IO;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Zilf.Common;

namespace Zilf.Common.Tests
{
    [TestClass]
    public class LimitedFileSystemTests
    {
        [TestMethod]
        public void LimitedFileSystem_WhenRestricted_ShouldOnlyExposeAllowedExtensions()
        {
            var underlying = new InMemoryFileSystem();
            var includeDir = Path.GetFullPath("include");

            underlying.SetText(Path.Combine(includeDir, "a.zil"), "<VERSION ZIP>");
            underlying.SetText(Path.Combine(includeDir, "b.mud"), "mud");
            underlying.SetText(Path.Combine(includeDir, "c.zap"), "; should be hidden");

            var fs = new LimitedFileSystem(underlying, [includeDir], ".zil", ".mud");

            Assert.IsTrue(fs.Exists("a.zil"));
            Assert.IsTrue(fs.Exists("b.mud"));
            Assert.IsFalse(fs.Exists("c.zap"));

            using var _ = fs.OpenForReading("a.zil");
            Assert.ThrowsException<FileNotFoundException>(() => fs.OpenForReading("c.zap"));
        }

        [TestMethod]
        public void LimitedFileSystem_WhenUnrestricted_ShouldExposeAnyExtension()
        {
            var underlying = new InMemoryFileSystem();
            var includeDir = Path.GetFullPath("include2");

            underlying.SetText(Path.Combine(includeDir, "c.zap"), "; visible when unrestricted");

            var fs = new LimitedFileSystem(underlying, [includeDir]);

            Assert.IsTrue(fs.Exists("c.zap"));
            using var _ = fs.OpenForReading("c.zap");
        }
    }
}
