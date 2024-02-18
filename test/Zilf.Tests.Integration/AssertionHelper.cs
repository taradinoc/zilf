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
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Zilf.Diagnostics;

namespace Zilf.Tests.Integration
{
    public abstract class AbstractAssertionHelper<TThis>
        where TThis : AbstractAssertionHelper<TThis>
    {
        protected string versionDirective = "<VERSION ZIP>";
        protected readonly StringBuilder miscGlobals = new();
        protected readonly StringBuilder input = new();
        protected readonly List<(Predicate<ZlrHelperRunResult>, string message)> warningChecks = [];
        protected bool wantCompileOutput;
        protected bool wantDebugInfo;

        public TThis InV3()
        {
            versionDirective = "<VERSION ZIP>";
            return (TThis)this;
        }

        public TThis InV4()
        {
            versionDirective = "<VERSION EZIP>";
            return (TThis)this;
        }

        public TThis InV5()
        {
            versionDirective = "<VERSION XZIP>";
            return (TThis)this;
        }

        public TThis InV6()
        {
            versionDirective = "<VERSION YZIP>";
            return (TThis)this;
        }

        public TThis InV7()
        {
            versionDirective = "<VERSION 7>";
            return (TThis)this;
        }

        public TThis InV8()
        {
            versionDirective = "<VERSION 8>";
            return (TThis)this;
        }

        public TThis WithVersionDirective(string versionStr)
        {
            versionDirective = versionStr;
            return (TThis)this;
        }

        public TThis WithGlobal(string code)
        {
            miscGlobals.AppendLine(code);
            return (TThis)this;
        }

        public TThis WithInput(string line)
        {
            input.AppendLine(line);
            return (TThis)this;
        }

        public TThis WithWarnings()
        {
            warningChecks.Add((res => res.Diagnostics.Any(d => d.Severity == Severity.Warning),
                "Expected a nonzero number of warnings."));
            return (TThis)this;
        }

        private static bool DiagnosticCodeMatches(Diagnostic diag, string code)
        {
            return diag.Code == code || diag.SubDiagnostics.Any(d => DiagnosticCodeMatches(d, code));
        }

        public TThis WithWarnings(params string[] expectedWarningCodes)
        {
            foreach (var code in expectedWarningCodes)
            {
                warningChecks.Add((res => res.Diagnostics.Any(d => DiagnosticCodeMatches(d, code)),
                    $"Expected a diagnostic with code '{code}'."));
            }
            return (TThis)this;
        }

        public TThis WithoutWarnings()
        {
            warningChecks.Add((res => res.Diagnostics.All(d => d.Severity != Severity.Warning),
                "Expected no warnings."));
            return (TThis)this;
        }

        public TThis WithoutUnsuppressedWarnings()
        {
            warningChecks.Add(
                (res => res.Diagnostics.Count(d => d.Severity == Severity.Warning) == res.SuppressedWarningCount,
                "Expected all warnings to be suppressed."));
            return (TThis)this;
        }

        public TThis WithoutWarnings(params string[] unexpectedWarningCodes)
        {
            foreach (var code in unexpectedWarningCodes)
            {
                warningChecks.Add((res => !res.Diagnostics.Any(d => DiagnosticCodeMatches(d, code)),
                    $"Expected no diagnostic with code '{code}'."));
            }
            return (TThis)this;
        }

        public TThis IgnoringWarnings()
        {
            warningChecks.Clear();
            return (TThis)this;
        }

        public TThis CapturingCompileOutput()
        {
            wantCompileOutput = true;
            return (TThis)this;
        }

        public TThis WithDebugInfo()
        {
            wantDebugInfo = true;
            return (TThis)this;
        }

        protected virtual string GlobalCode()
        {
            var sb = new StringBuilder();
            sb.Append(versionDirective);

            sb.Append(miscGlobals);

            return sb.ToString();
        }

        protected void CheckWarnings(ZlrHelperRunResult res)
        {
            foreach (var (check, message) in warningChecks)
                if (!check(res))
                    Assert.Fail(message);
        }
    }

    public sealed class EntryPointAssertionHelper(string argSpec, string body) : AbstractAssertionHelper<EntryPointAssertionHelper>
    {
        public async Task CompilesAsync()
        {
            var testCode = $"{GlobalCode()}\r\n" +
                           $"<ROUTINE GO ({argSpec})\r\n" +
                           $"\t{body}\r\n" +
                           "\t<QUIT>>";

            var result = await ZlrHelper.RunAsync(testCode, null, compileOnly: true, wantDebugInfo: wantDebugInfo);
            Assert.AreEqual(ZlrTestStatus.Finished, result.Status);

            CheckWarnings(result);
        }

        public async Task DoesNotCompileAsync()
        {
            var testCode = $"{GlobalCode()}\r\n" +
                           $"<ROUTINE GO ({argSpec})\r\n" +
                           $"\t{body}\r\n" +
                           "\t<QUIT>>";

            var result = await ZlrHelper.RunAsync(testCode, null, compileOnly: true, wantDebugInfo: wantDebugInfo);
            Assert.AreEqual(ZlrTestStatus.CompilationFailed, result.Status);

            CheckWarnings(result);
        }

        public async Task DoesNotThrowAsync()
        {
            var testCode = $"{GlobalCode()}\r\n" +
                           $"<ROUTINE GO ({argSpec})\r\n" +
                           $"\t{body}\r\n" +
                           "\t<QUIT>>";

            ZlrHelperRunResult result;

            try
            {
                result = await ZlrHelper.RunAsync(testCode, null, compileOnly: true, wantDebugInfo: wantDebugInfo);
            }
            catch (Exception ex)
            {
                Assert.Fail("Expected no exception, but caught {0}", ex);

                // can't get here, but the compiler doesn't know that...
                // ReSharper knows, but we still can't remove the return

                // ReSharper disable once HeuristicUnreachableCode
                return;
            }

            CheckWarnings(result);
        }
    }

    public abstract class AbstractAssertionHelperWithEntryPoint<TThis> : AbstractAssertionHelper<TThis>
        where TThis : AbstractAssertionHelperWithEntryPoint<TThis>
    {
        protected abstract string Expression();

        public Task GivesNumberAsync(string expectedValue)
        {
            var testCode = $"{GlobalCode()}\r\n" +
                           $"<ROUTINE GO () <PRINTN {Expression()}>>";

            return ZlrHelper.RunAndAssertAsync(testCode, input.ToString(), expectedValue, warningChecks);
        }

        public Task OutputsAsync(string expectedValue)
        {
            var testCode = $"{GlobalCode()}\r\n" +
                           $"<ROUTINE GO () {Expression()}>";

            return ZlrHelper.RunAndAssertAsync(testCode, input.ToString(), expectedValue, warningChecks, wantCompileOutput);
        }

        public Task ImpliesAsync(params string[] conditions)
        {
            var sb = new StringBuilder();
            foreach (var c in conditions)
            {
                sb.AppendFormat(
                    "<COND ({0}) (T <INC FAILS> <PRINTI \"FAIL: {1}|\">)>\r\n",
                    c,
                    c.Replace("\\", "\\\\").Replace("\"", "\\\""));
            }

            var testCode =
                $"{GlobalCode()}\r\n" +
                $"<ROUTINE TEST-IMPLIES (\"AUX\" FAILS) {sb} .FAILS>\r\n" +
                "<ROUTINE GO () <OR <TEST-IMPLIES> <PRINTI \"PASS\">>>";

            return ZlrHelper.RunAndAssertAsync(testCode, input.ToString(), "PASS", warningChecks);
        }

        public async Task DoesNotCompileAsync(Predicate<ZlrHelperRunResult>? resultFilter = null,
            string? message = null)
        {
            var testCode =
                $"{GlobalCode()}\r\n" +
                "<GLOBAL DUMMY?VAR <>>\r\n" +
                "<ROUTINE GO ()\r\n" +
                $"\t<SETG DUMMY?VAR {Expression()}>\r\n" +
                "\t<QUIT>>";

            var result = await ZlrHelper.RunAsync(testCode, null, compileOnly: true, wantDebugInfo: wantDebugInfo);
            Assert.AreEqual(ZlrTestStatus.CompilationFailed, result.Status);

            CheckWarnings(result);

            if (resultFilter != null)
            {
                Assert.IsTrue(resultFilter(result), message ?? "Result filter failed");
            }
        }

        public Task DoesNotCompileAsync(string diagnosticCode, Predicate<Diagnostic>? diagFilter = null)
        {
            return DoesNotCompileAsync(res =>
                 {
                     var diag = res.Diagnostics.FirstOrDefault(d => d.Code == diagnosticCode);
                     return diag != null && (diagFilter == null || diagFilter(diag));
                 },
                $"Expected diagnostic {diagnosticCode} was not produced");
        }

        public async Task CompilesAsync()
        {
            var testCode =
                $"{GlobalCode()}\r\n" +
                "<GLOBAL DUMMY?VAR <>>\r\n" +
                "<ROUTINE GO ()\r\n" +
                $"\t<SETG DUMMY?VAR {Expression()}>\r\n" +
                "\t<QUIT>>";

            var result = await ZlrHelper.RunAsync(testCode, null, compileOnly: true, wantDebugInfo: wantDebugInfo);
            Assert.IsTrue(result.Status > ZlrTestStatus.CompilationFailed,
                "Failed to compile");

            CheckWarnings(result);
        }

        public Task<CodeMatchingResult> GeneratesCodeMatchingAsync([StringSyntax(StringSyntaxAttribute.Regex)] string pattern)
        {
            return GeneratesCodeMatchingAsync(output => CheckOutputMatches(output, pattern));
        }

        public static void CheckOutputMatches(string output, [StringSyntax(StringSyntaxAttribute.Regex)] string pattern)
        {
            Assert.IsTrue(
                Regex.IsMatch(output, pattern, RegexOptions.Singleline | RegexOptions.Multiline),
                "Output did not match. Expected pattern: " + pattern);
        }

        public Task<CodeMatchingResult> GeneratesCodeNotMatchingAsync([StringSyntax(StringSyntaxAttribute.Regex)] string pattern)
        {
            return GeneratesCodeMatchingAsync(output => CheckOutputDoesNotMatch(output, pattern));
        }

        public static void CheckOutputDoesNotMatch(string output, [StringSyntax(StringSyntaxAttribute.Regex)] string pattern)
        {
            Assert.IsFalse(
                Regex.IsMatch(output, pattern, RegexOptions.Singleline | RegexOptions.Multiline),
                "Output should not have matched. Anti-pattern: " + pattern);
        }

        Task<CodeMatchingResult> GeneratesCodeMatchingAsync(Action<string> checkGeneratedCode)
        {
            var testCode = $"{GlobalCode()}\r\n" +
                           "<ROUTINE GO ()\r\n" +
                           $"\t{Expression()}\r\n" +
                           "\t<QUIT>>";

            var helper = new ZlrHelper(testCode, null);
            Assert.IsTrue(helper.Compile(wantDebugInfo: wantDebugInfo), "Failed to compile");

            var output = helper.GetZapCode();
            checkGeneratedCode(output);

            CheckWarnings(new ZlrHelperRunResult
            {
                WarningCount = helper.WarningCount,
                Diagnostics = helper.Diagnostics,
                SuppressedWarningCount = helper.SuppressedWarningCount,
            });

            return Task.FromResult(new CodeMatchingResult(output));
        }

        public sealed class CodeMatchingResult(string output)
        {
            public string Output { get; } = output;
        }
    }

    public static class CodeMatchingResultTaskExtensions
    {
        public static async Task<AbstractAssertionHelperWithEntryPoint<T>.CodeMatchingResult> AndMatching<T>(
            this Task<AbstractAssertionHelperWithEntryPoint<T>.CodeMatchingResult> task, [StringSyntax(StringSyntaxAttribute.Regex)] string pattern)
            where T : AbstractAssertionHelperWithEntryPoint<T>
        {
            var result = await task;
            AbstractAssertionHelperWithEntryPoint<T>.CheckOutputMatches(result.Output, pattern);
            return result;
        }

        public static async Task<AbstractAssertionHelperWithEntryPoint<T>.CodeMatchingResult> AndNotMatching<T>(
            this Task<AbstractAssertionHelperWithEntryPoint<T>.CodeMatchingResult> task, [StringSyntax(StringSyntaxAttribute.Regex)] string pattern)
            where T : AbstractAssertionHelperWithEntryPoint<T>
        {
            var result = await task;
            AbstractAssertionHelperWithEntryPoint<T>.CheckOutputDoesNotMatch(result.Output, pattern);
            return result;
        }
    }

    public sealed class ExprAssertionHelper(string expression) : AbstractAssertionHelperWithEntryPoint<ExprAssertionHelper>
    {
        protected override string Expression() => expression;
    }

    public sealed class RoutineAssertionHelper(string argSpec, string body) : AbstractAssertionHelperWithEntryPoint<RoutineAssertionHelper>
    {
        string arguments = "";

        const string RoutineName = "TEST?ROUTINE";

        public RoutineAssertionHelper WhenCalledWith(string testArguments)
        {
            arguments = testArguments;
            return this;
        }

        protected override string GlobalCode()
        {
            return $"{base.GlobalCode()}<ROUTINE {RoutineName} ({argSpec}) {body}>";
        }

        protected override string Expression()
        {
            return $"<{RoutineName} {arguments}>";
        }
    }

    public sealed class GlobalsAssertionHelper : AbstractAssertionHelperWithEntryPoint<GlobalsAssertionHelper>
    {
        public GlobalsAssertionHelper(params string[] globals)
        {
            foreach (var g in globals)
                miscGlobals.AppendLine(g);
        }

        protected override string Expression()
        {
            return "<>";
        }
    }

    public sealed class RawAssertionHelper(string code)
    {
        public Task OutputsAsync(string expectedValue)
        {
            return ZlrHelper.RunAndAssertAsync(code, null, expectedValue);
        }
    }
}
