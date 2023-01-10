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

using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Zilf.Common;
using Zilf.Compiler;

namespace Zilf.Tests.Compiler
{
    [TestClass, TestCategory("Compiler")]
    public class PathTests
    {
        class PathTestHelper
        {
            readonly InMemoryFileSystem fileSystem = new();
            readonly HashSet<string> inputPaths = new();

            public ICollection GetOutputFilePaths() => fileSystem.Paths.Except(inputPaths).ToList();

            public void SetInputFile(string path, string content)
            {
                fileSystem.SetText(path, content);
                inputPaths.Add(path);
            }

            public string GetOutputContent(string path) => fileSystem.GetText(path);

            public void Compile(string mainZilFile)
            {
                var compiler = new FrontEnd { FileSystem = fileSystem };
                var compilationResult = compiler.Compile(mainZilFile, Path.ChangeExtension(mainZilFile, ".zap"));

                Assert.IsTrue(compilationResult.Success, "Compilation failed");
            }
        }

        [TestMethod]
        public void ZAP_Files_Should_Go_To_Source_Directory()
        {
            var helper = new PathTestHelper();

            helper.SetInputFile(Path.Combine("src", "foo.zil"), @"
<VERSION ZIP>

<ROUTINE GO ()
    <PRINTI ""Hello, world!"">
    <CRLF>
    <QUIT>>
");

            helper.Compile(Path.Combine("src", "foo.zil"));

            var expected = new[]
            {
                Path.Combine("src", "foo.zap"),
                Path.Combine("src", "foo_data.zap"),
                Path.Combine("src", "foo_freq.zap"),
                Path.Combine("src", "foo_str.zap")
            };

            CollectionAssert.AreEquivalent(expected, helper.GetOutputFilePaths());
        }

        [TestMethod]
        public void Frequent_Words_File_Without_Underscore_Should_Be_Used_If_Present()
        {
            var helper = new PathTestHelper();

            helper.SetInputFile("foo.zil", @"
<VERSION ZIP>

<ROUTINE GO ()
    <PRINTI ""Hello, world!"">
    <CRLF>
    <QUIT>>
");

            helper.SetInputFile("foofreq.xzap", "; use me");

            helper.Compile("foo.zil");

            var expected = new[]
            {
                "foo.zap",
                "foo_data.zap",
                "foo_str.zap"
            };

            CollectionAssert.AreEquivalent(expected, helper.GetOutputFilePaths());

            Assert.IsTrue(helper.GetOutputContent("foo.zap")!.Contains(@"foofreq"));
        }
    }
}
