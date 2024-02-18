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

using DiffLib;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace Zilf.Tests.Integration
{
    [TestClass, TestCategory("Compiler"), TestCategory("Slow"), TestCategory("Library")]
    public partial class FullProjectTests
    {
        const string TestDirName = "test";
        const string ProjectsSubDirName = "FullTestProjects";
        const string LibraryDirName = "zillib";
        const int PerTestTimeoutMilliseconds = 60000;

        static string projectsDir = null!;
        static string libraryDir = null!;

        /// <exception cref="IOException">Can't locate projects and library directories</exception>
        [ClassInitialize]
        [MemberNotNull(nameof(projectsDir), nameof(libraryDir))]
        public static void ClassInitialize(TestContext _)
        {
            projectsDir = null!;
            libraryDir = null!;

            var projectsDirName = Path.Combine(TestDirName, ProjectsSubDirName);

            // find directories containing ProjectsDirName and LibraryDirName
            var dir = Directory.GetCurrentDirectory();

            do
            {
                if (projectsDir == null && Directory.Exists(Path.Combine(dir, projectsDirName)))
                    projectsDir = Path.Combine(dir, projectsDirName);

                if (libraryDir == null && Directory.Exists(Path.Combine(dir, LibraryDirName)))
                    libraryDir = Path.Combine(dir, LibraryDirName);

                if (projectsDir != null && libraryDir != null)
                    break;

                dir = Directory.GetParent(dir)?.FullName;
            } while (dir != null && dir != Path.GetPathRoot(dir));

            if (projectsDir == null || libraryDir == null)
                throw new IOException("Can't locate projects and library directories");
        }

        static IEnumerable<string[]> GetProjects()
        {
            return from dir in Directory.EnumerateDirectories(projectsDir, "*", SearchOption.AllDirectories)
                   let baseName = Path.GetFileName(dir)
                   let mainZilFile = Path.Combine(dir, baseName + ".zil")
                   where File.Exists(mainZilFile)
                   select new[] { baseName, dir, mainZilFile };
        }

        /// <exception cref="AssertInconclusiveException">Always thrown.</exception>
        [DataTestMethod]
        [DynamicData(nameof(GetProjects), DynamicDataSourceType.Method)]
        [Timeout(PerTestTimeoutMilliseconds)]
        [TestCategory("Slow")]
        public async Task TestProjectsAsync(string baseName, string dir, string mainZilFile)
        {
            Console.WriteLine("Testing {0}", dir);

            var outputFile = Path.Combine(dir, baseName + ".output.txt");
            var inputFile = Path.Combine(dir, baseName + ".input.txt");

            bool testExecution = File.Exists(outputFile) && File.Exists(inputFile);

            var helper = new FileBasedZlrHelper(mainZilFile,
                [dir, libraryDir], inputFile)
            {
                WantStatusLine = true
            };

            Assert.IsTrue(helper.Compile(), "Failed to compile");
            Assert.IsTrue(helper.Assemble(), "Failed to assemble");

            if (testExecution)
            {
                var actualOutput = await helper.ExecuteAsync();

                var massagedActual = MassageText(actualOutput);
                var massagedExpected = MassageText(await File.ReadAllTextAsync(outputFile));
                if (massagedActual != massagedExpected)
                {
                    var expectedLines = SplitLines(massagedExpected);
                    var actualLines = SplitLines(massagedActual);

                    var diff = Diff.CalculateSections(expectedLines, actualLines);
                    int e = 0, a = 0;
                    foreach (var change in diff)
                    {
                        if (!change.IsMatch)
                        {
                            Console.WriteLine("=== At line {0}, {1} ===", e + 1, a + 1);

                            for (int k = e; k < e + change.LengthInCollection1; k++)
                            {
                                Console.WriteLine("-{0}", expectedLines[k]);
                            }

                            for (int m = a; m < a + change.LengthInCollection2; m++)
                            {
                                Console.WriteLine("+{0}", actualLines[m]);
                            }

                            Console.WriteLine();
                        }

                        e += change.LengthInCollection1;
                        a += change.LengthInCollection2;
                    }

                    Assert.Fail("Expected output not found (diff written to console)");
                }
            }
            else
            {
                Assert.Inconclusive("Expected input and/or output files missing.");
            }
        }

        static readonly Regex SerialNumberRegex = GetSerialNumberRegex();

        static readonly Regex ZilfVersionRegex = GetZilfVersionRegex();

        static string MassageText(string text)
        {
            text = SerialNumberRegex.Replace(text, "######");
            text = ZilfVersionRegex.Replace(text, "ZILF #.# lib ##");
            return text;
        }

        static string[] SplitLines(string text)
        {
            var lines = text.Split('\n');
            for (int i = 0; i < lines.Length; i++)
            {
                if (lines[i].EndsWith('\r'))
                    lines[i] = lines[i][0..^1];
            }

            return lines;
        }

        [GeneratedRegex(@"ZILF [0-9.a-z]+ lib \S+")]
        private static partial Regex GetZilfVersionRegex();
        [GeneratedRegex(@"(?<=Serial number )\d{6}", RegexOptions.IgnoreCase, "en-US")]
        private static partial Regex GetSerialNumberRegex();
    }
}
