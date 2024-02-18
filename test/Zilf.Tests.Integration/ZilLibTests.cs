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
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace Zilf.Tests.Integration
{
    [TestClass, TestCategory("Slow"), TestCategory("Library")]
    public partial class ZilLibTests
    {
        const string LibraryDirName = "zillib";
        const string TestsSubDirName = "tests";
        const int PerTestTimeoutMilliseconds = 60000;

        static string testsDir = null!;
        static string libraryDir = null!;

        /// <exception cref="IOException">Can't locate projects and library directories</exception>
        [ClassInitialize]
        [MemberNotNull(nameof(testsDir), nameof(libraryDir))]
        public static void ClassInitialize(TestContext _)
        {
            testsDir = null!;
            libraryDir = null!;

            var testsDirName = Path.Combine(LibraryDirName, TestsSubDirName);

            // find directories containing zillib/tests
            var dir = Directory.GetCurrentDirectory();

            do
            {
                if (testsDir == null && Directory.Exists(Path.Combine(dir, testsDirName)))
                {
                    testsDir = Path.Combine(dir, testsDirName);
                    libraryDir = Path.Combine(dir, LibraryDirName);
                    break;
                }

                dir = Directory.GetParent(dir)?.FullName;
            } while (dir != null && dir != Path.GetPathRoot(dir));

            if (testsDir == null || libraryDir == null)
                throw new IOException("Can't locate library and tests directories");
        }

        static IEnumerable<string[]> GetTestCaseNames()
        {
            return from f in Directory.EnumerateFiles(testsDir, "test-*.zil")
                   select new[] { Path.GetFileNameWithoutExtension(f) };
        }

        private static readonly Regex PassRegex = GetPassRegex();

        /// <exception cref="AssertInconclusiveException">Always thrown.</exception>
        [DataTestMethod]
        [DynamicData(nameof(GetTestCaseNames), DynamicDataSourceType.Method)]
        [Timeout(PerTestTimeoutMilliseconds)]
        [TestCategory("Slow")]
        public async Task TestLibraryCasesAsync(string testCaseName)
        {
            Console.WriteLine("Testing {0}", testCaseName);

            var mainZilFile = Path.Combine(testsDir, testCaseName + ".zil");

            var helper = new FileBasedZlrHelper(mainZilFile, [testsDir, libraryDir], null);

            Assert.IsTrue(helper.Compile(), "Failed to compile");
            Assert.IsTrue(helper.Assemble(), "Failed to assemble");

            var actualOutput = await helper.ExecuteAsync();

            if (!PassRegex.IsMatch(actualOutput))
            {
                Console.WriteLine(actualOutput);
                Assert.Fail("Test case failed (output written to console)");
            }
        }

        [GeneratedRegex("^PASS$", RegexOptions.Multiline)]
        private static partial Regex GetPassRegex();
    }
}
