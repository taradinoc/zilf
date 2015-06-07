/* Copyright 2010, 2015 Jesse McGrew
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
using DiffLib;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using System.Diagnostics.Contracts;
using System.IO;
using System.Text.RegularExpressions;

namespace IntegrationTests
{
    [TestClass]
    public class FullProjectTests
    {
        private const string ProjectsDirName = "FullTestProjects";
        private const string LibraryDirName = "Library";

        private static string projectsDir, libraryDir;

        [ClassInitialize]
        public static void ClassInitialize(TestContext context)
        {
            projectsDir = libraryDir = null;

            // find a directory containing ProjectsDirName and LibraryDirName
            var dir = Directory.GetCurrentDirectory();

            do
            {
                if (Directory.Exists(Path.Combine(dir, ProjectsDirName)) &&
                    Directory.Exists(Path.Combine(dir, LibraryDirName)))
                {
                    projectsDir = Path.Combine(dir, ProjectsDirName);
                    libraryDir = Path.Combine(dir, LibraryDirName);
                    break;
                }

                dir = Directory.GetParent(dir).FullName;
            } while (dir != Path.GetPathRoot(dir));

            if (projectsDir == null)
                throw new IOException("Can't locate projects and library directories");
        }

        [TestMethod]
        [Timeout(30000)]
        public void TestProjects()
        {
            bool inconclusive = false;

            foreach (var dir in Directory.EnumerateDirectories(projectsDir, "*", SearchOption.AllDirectories))
            {
                var baseName = Path.GetFileName(dir);
                var mainZilFile = Path.Combine(dir, baseName + ".zil");
                if (!File.Exists(mainZilFile))
                    continue;

                Console.WriteLine("Testing {0}", dir);

                var outputFile = Path.Combine(dir, baseName + ".output.txt");
                var inputFile = Path.Combine(dir, baseName + ".input.txt");

                bool testExecution = File.Exists(outputFile) && File.Exists(inputFile);

                var helper = new FileBasedZlrHelper(
                    mainZilFile,
                    new string[] { dir, libraryDir },
                    inputFile);
                helper.WantStatusLine = true;

                Assert.IsTrue(helper.Compile(), "Failed to compile");
                Assert.IsTrue(helper.Assemble(), "Failed to assemble");

                if (testExecution)
                {
                    var actualOutput = helper.Execute();

                    var massagedActual = MassageText(actualOutput);
                    var massagedExpected = MassageText(File.ReadAllText(outputFile));
                    if (massagedActual != massagedExpected)
                    {
                        string[] expectedLines = SplitLines(massagedExpected);
                        string[] actualLines = SplitLines(massagedActual);

                        var diff = Diff.Calculate(expectedLines, actualLines);
                        int e = 0, a = 0;
                        foreach (var change in diff)
                        {
                            if (!change.Equal)
                            {
                                Console.WriteLine("=== At line {0}, {1} ===", e + 1, a + 1);

                                for (int k = e; k < e + change.Length1; k++)
                                {
                                    Console.WriteLine("-{0}", expectedLines[k]);
                                }

                                for (int m = a; m < a + change.Length2; m++)
                                {
                                    Contract.Assume(m >= 0);        // prevent spurious "Array access might be below lower bound"
                                    Console.WriteLine("+{0}", actualLines[m]);
                                }

                                Console.WriteLine();
                            }

                            e += change.Length1;
                            a += change.Length2;
                        }

                        Assert.Fail("Expected output not found (diff written to console)");
                    }
                }
                else
                {
                    Console.WriteLine("Expected input and/or output files missing.");
                    inconclusive = true;
                }
            }

            if (inconclusive)
            {
                Assert.Inconclusive("One or more projects had no expected input/output files.");
            }
        }

        private static Regex SerialNumberRegex = new Regex(@"(?<=Serial number )\d{6}", RegexOptions.IgnoreCase);

        private static string MassageText(string text)
        {
            Contract.Requires(text != null);
            Contract.Ensures(Contract.Result<string>() != null);

            return SerialNumberRegex.Replace(text, "######");
        }

        private static string[] SplitLines(string text)
        {
            Contract.Requires(text != null);
            Contract.Ensures(Contract.Result<string[]>() != null);

            var lines = text.Split('\n');
            for (int i = 0; i < lines.Length; i++)
            {
                if (lines[i].EndsWith("\r"))
                    lines[i] = lines[i].Substring(0, lines[i].Length - 1);
            }

            return lines;
        }
    }
}
