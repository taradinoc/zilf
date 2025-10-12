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

using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CodeActions;
using Microsoft.CodeAnalysis.CodeFixes;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.CodeAnalysis.Formatting;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

// ReSharper disable once CheckNamespace
namespace ZilfAnalyzers.Test.Helpers
{
    /// <summary>
    /// Superclass of all Unit tests made for diagnostics with codefixes.
    /// Contains methods used to verify correctness of codefixes
    /// </summary>
    public abstract partial class CodeFixVerifier : DiagnosticVerifier
    {
        /// <summary>
        /// Returns the codefix being tested (C#) - to be implemented in non-abstract class
        /// </summary>
        /// <returns>The CodeFixProvider to be used for CSharp code</returns>
        protected abstract CodeFixProvider GetCSharpCodeFixProvider();

        /// <summary>
        /// Returns the codefix being tested (VB) - to be implemented in non-abstract class
        /// </summary>
        /// <returns>The CodeFixProvider to be used for VisualBasic code</returns>
        protected abstract CodeFixProvider GetBasicCodeFixProvider();

        /// <summary>
        /// Called to test a C# codefix when applied on the inputted string as a source
        /// </summary>
        /// <param name="oldSource">A class in the form of a string before the CodeFix was applied to it</param>
        /// <param name="newSource">A class in the form of a string after the CodeFix was applied to it</param>
        /// <param name="codeFixIndex">Index determining which codefix to apply if there are multiple</param>
        /// <param name="allowNewCompilerDiagnostics">A bool controlling whether or not the test will fail if the CodeFix introduces other warnings after being applied</param>
        protected Task VerifyCSharpFixAsync(string oldSource, string newSource, int? codeFixIndex = null, bool allowNewCompilerDiagnostics = false)
        {
            return VerifyFixAsync(LanguageNames.CSharp,
                    GetCSharpDiagnosticAnalyzer(),
                    GetCSharpCodeFixProvider(),
                    oldSource,
                    newSource,
                    codeFixIndex,
                    allowNewCompilerDiagnostics);
        }

        /// <summary>
        /// Called to test a VB codefix when applied on the inputted string as a source
        /// </summary>
        /// <param name="oldSource">A class in the form of a string before the CodeFix was applied to it</param>
        /// <param name="newSource">A class in the form of a string after the CodeFix was applied to it</param>
        /// <param name="codeFixIndex">Index determining which codefix to apply if there are multiple</param>
        /// <param name="allowNewCompilerDiagnostics">A bool controlling whether or not the test will fail if the CodeFix introduces other warnings after being applied</param>
        protected Task VerifyBasicFixAsync(string oldSource, string newSource, int? codeFixIndex = null, bool allowNewCompilerDiagnostics = false)
        {
            return VerifyFixAsync(LanguageNames.VisualBasic,
                    GetBasicDiagnosticAnalyzer(),
                    GetBasicCodeFixProvider(),
                    oldSource,
                    newSource,
                    codeFixIndex,
                    allowNewCompilerDiagnostics);
        }

        /// <summary>
        /// General verifier for codefixes.
        /// Creates a Document from the source string, then gets diagnostics on it and applies the relevant codefixes.
        /// Then gets the string after the codefix is applied and compares it with the expected result.
        /// Note: If any codefix causes new diagnostics to show up, the test fails unless allowNewCompilerDiagnostics is set to true.
        /// </summary>
        /// <param name="language">The language the source code is in</param>
        /// <param name="analyzer">The analyzer to be applied to the source code</param>
        /// <param name="codeFixProvider">The codefix to be applied to the code wherever the relevant Diagnostic is found</param>
        /// <param name="oldSource">A class in the form of a string before the CodeFix was applied to it</param>
        /// <param name="newSource">A class in the form of a string after the CodeFix was applied to it</param>
        /// <param name="codeFixIndex">Index determining which codefix to apply if there are multiple</param>
        /// <param name="allowNewCompilerDiagnostics">A bool controlling whether or not the test will fail if the CodeFix introduces other warnings after being applied</param>
        static async Task VerifyFixAsync(string language, DiagnosticAnalyzer analyzer, CodeFixProvider codeFixProvider, string oldSource, string newSource, int? codeFixIndex, bool allowNewCompilerDiagnostics)
        {
            var document = CreateDocument(oldSource, language);
            var analyzerDiagnostics = await GetSortedDiagnosticsFromDocumentsAsync(analyzer, [document]).ConfigureAwait(false);

            // only test the diagnostics the code fixer claims are fixable
            var fixableIds = codeFixProvider.FixableDiagnosticIds;
            analyzerDiagnostics = [.. analyzerDiagnostics.Where(d => fixableIds.Contains(d.Id))];

            var compilerDiagnostics = await GetCompilerDiagnosticsAsync(document).ConfigureAwait(false);
            var attempts = analyzerDiagnostics.Length;

            for (int i = 0; i < attempts; ++i)
            {
                var actions = new List<CodeAction>();
                var context = new CodeFixContext(document, analyzerDiagnostics[0], (a, _) => actions.Add(a), CancellationToken.None);
                await codeFixProvider.RegisterCodeFixesAsync(context).ConfigureAwait(false);

                if (actions.Count == 0)
                {
                    break;
                }

                if (codeFixIndex != null)
                {
                    document = await ApplyFixAsync(document, actions[(int)codeFixIndex]).ConfigureAwait(false);
                    break;
                }

                document = await ApplyFixAsync(document, actions[0]).ConfigureAwait(false);
                analyzerDiagnostics = await GetSortedDiagnosticsFromDocumentsAsync(analyzer, [document]).ConfigureAwait(false);
                analyzerDiagnostics = [.. analyzerDiagnostics.Where(d => fixableIds.Contains(d.Id))];

                var newCompilerDiagnostics = GetNewDiagnostics(compilerDiagnostics, await GetCompilerDiagnosticsAsync(document).ConfigureAwait(false));

                //check if applying the code fix introduced any new compiler diagnostics
                if (!allowNewCompilerDiagnostics && newCompilerDiagnostics.Any())
                {
                    // Format and get the compiler diagnostics again so that the locations make sense in the output
                    document = document.WithSyntaxRoot(Formatter.Format(
                        await document.GetSyntaxRootAsync().ConfigureAwait(false) ?? throw new NotSupportedException(),
                        Formatter.Annotation,
                        document.Project.Solution.Workspace));
                    newCompilerDiagnostics = GetNewDiagnostics(compilerDiagnostics, await GetCompilerDiagnosticsAsync(document).ConfigureAwait(false));

                    Assert.Fail(
                        "Fix introduced new compiler diagnostics:\r\n{0}\r\n\r\nNew document:\r\n{1}\r\n",
                        string.Join("\r\n", newCompilerDiagnostics.Select(d => d.ToString())),
                        (await document.GetSyntaxRootAsync().ConfigureAwait(false))?.ToFullString());
                }

                //check if there are analyzer diagnostics left after the code fix
                if (analyzerDiagnostics.Length == 0)
                {
                    break;
                }
            }

            //after applying all of the code fixes, compare the resulting string to the inputted one
            var actual = await GetStringFromDocumentAsync(document).ConfigureAwait(false);
            //Assert.AreEqual(newSource, actual);

            // normalize line endings
            actual = actual.Replace("\r\n", "\n");
            newSource = newSource.Replace("\r\n", "\n");

            actual.ShouldEqualWithDiff(newSource);
        }
    }
}