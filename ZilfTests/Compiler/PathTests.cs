﻿/* Copyright 2010, 2015 Jesse McGrew
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
using Zilf.Compiler;
using System.IO;
using System.Collections;
using System.Collections.Generic;
using System.Text;

namespace ZilfTests
{
    [TestClass]
    public class PathTests
    {
        private class PathTestHelper
        {
            private readonly Dictionary<string, string> inputs = new Dictionary<string, string>();
            private readonly Dictionary<string, MemoryStream> outputs = new Dictionary<string, MemoryStream>();

            public ICollection OutputFilePaths
            {
                get { return outputs.Keys; }
            }

            public void SetInputFile(string path, string content)
            {
                inputs[path] = content;
            }

            public void Compile(string mainZilFile)
            {
                var compiler = new FrontEnd();

                compiler.CheckingFilePresence += (sender, e) =>
                {
                    e.Exists = inputs.ContainsKey(e.FileName);
                };

                compiler.OpeningFile += (sender, e) =>
                {
                    if (e.Writing)
                    {
                        e.Stream = outputs[e.FileName] = new MemoryStream();
                    }
                    else
                    {
                        string content;
                        if (inputs.TryGetValue(e.FileName, out content))
                        {
                            var result = new MemoryStream();
                            using (var wtr = new StreamWriter(result, Encoding.UTF8, 512, true))
                            {
                                wtr.Write(content);
                                wtr.Flush();
                            }
                            result.Position = 0;
                            e.Stream = result;
                        }
                    }
                };

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
                Path.Combine("src", "foo_str.zap"),
            };

            CollectionAssert.AreEquivalent(expected, helper.OutputFilePaths);
        }
    }
}