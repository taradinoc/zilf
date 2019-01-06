/* Copyright 2010-2018 Jesse McGrew
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

using JetBrains.Annotations;
using System;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Zilf.Diagnostics;

namespace Zilf.Tests.Integration
{
    public abstract class AbstractAssertionHelper<TThis>
        where TThis : AbstractAssertionHelper<TThis>
    {
        [NotNull]
        protected string versionDirective = "<VERSION ZIP>";
        [NotNull]
        protected readonly StringBuilder miscGlobals = new StringBuilder();
        [NotNull]
        protected readonly StringBuilder input = new StringBuilder();
        protected bool? expectWarnings;
        [CanBeNull]
        protected string[] warningCodes;
        protected bool wantCompileOutput;
        protected bool wantDebugInfo;

        [NotNull]
        public TThis InV3()
        {
            versionDirective = "<VERSION ZIP>";
            return (TThis)this;
        }

        [NotNull]
        public TThis InV4()
        {
            versionDirective = "<VERSION EZIP>";
            return (TThis)this;
        }

        [NotNull]
        public TThis InV5()
        {
            versionDirective = "<VERSION XZIP>";
            return (TThis)this;
        }

        [NotNull]
        public TThis InV6()
        {
            versionDirective = "<VERSION YZIP>";
            return (TThis)this;
        }

        [NotNull]
        public TThis InV7()
        {
            versionDirective = "<VERSION 7>";
            return (TThis)this;
        }

        [NotNull]
        public TThis InV8()
        {
            versionDirective = "<VERSION 8>";
            return (TThis)this;
        }

        [NotNull]
        public TThis WithVersionDirective([NotNull] string versionStr)
        {
            versionDirective = versionStr;
            return (TThis)this;
        }

        [NotNull]
        public TThis WithGlobal([NotNull] string code)
        {
            miscGlobals.AppendLine(code);
            return (TThis)this;
        }

        [NotNull]
        public TThis WithInput([NotNull] string line)
        {
            input.AppendLine(line);
            return (TThis)this;
        }

        [NotNull]
        public TThis WithWarnings()
        {
            expectWarnings = true;
            warningCodes = null;
            return (TThis)this;
        }

        [NotNull]
        public TThis WithWarnings(params string[] expectedWarningCodes)
        {
            expectWarnings = true;
            warningCodes = expectedWarningCodes;
            return (TThis)this;
        }

        [NotNull]
        public TThis WithoutWarnings()
        {
            expectWarnings = false;
            warningCodes = null;
            return (TThis)this;
        }

        [NotNull]
        public TThis CapturingCompileOutput()
        {
            wantCompileOutput = true;
            return (TThis)this;
        }

        [NotNull]
        public TThis WithDebugInfo()
        {
            wantDebugInfo = true;
            return (TThis)this;
        }

        [NotNull]
        protected virtual string GlobalCode()
        {
            var sb = new StringBuilder();
            sb.Append(versionDirective);

            sb.Append(miscGlobals);

            return sb.ToString();
        }

        protected void CheckWarnings(ZlrHelperRunResult res)
        {
            var warningCount = res.WarningCount;

            switch (expectWarnings)
            {
                case true:
                    Assert.AreNotEqual(0, warningCount, "Expected at least one warning.");

                    if (warningCodes != null)
                    {
                        foreach (var code in warningCodes)
                        {
                            if (res.Diagnostics.All(d => d.Code != code && d.SubDiagnostics.All(s => s.Code != code)))
                            {
                                Assert.Fail("Expected diagnostic with code '{0}'.", code);
                            }
                        }
                    }
                    break;

                case false:
                    Assert.AreEqual(0, warningCount, "Expected no warnings.");
                    break;
            }
        }
    }

    public sealed class EntryPointAssertionHelper : AbstractAssertionHelper<EntryPointAssertionHelper>
    {
        [NotNull]
        readonly string argSpec, body;

        public EntryPointAssertionHelper([NotNull] string argSpec, [NotNull] string body)
        {
            this.argSpec = argSpec;
            this.body = body;
        }

        public void DoesNotCompile()
        {
            var testCode = $"{GlobalCode()}\r\n" +
                           $"<ROUTINE GO ({argSpec})\r\n" +
                           $"\t{body}\r\n" +
                           "\t<QUIT>>";

            var result = ZlrHelper.Run(testCode, null, compileOnly: true, wantDebugInfo: wantDebugInfo);
            Assert.AreEqual(ZlrTestStatus.CompilationFailed, result.Status);

            CheckWarnings(result);
        }

        public void DoesNotThrow()
        {
            var testCode = $"{GlobalCode()}\r\n" +
                           $"<ROUTINE GO ({argSpec})\r\n" +
                           $"\t{body}\r\n" +
                           "\t<QUIT>>";

            ZlrHelperRunResult result;

            try
            {
                result = ZlrHelper.Run(testCode, null, compileOnly: true, wantDebugInfo: wantDebugInfo);
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
        [NotNull] protected abstract string Expression();

        public void GivesNumber([NotNull] string expectedValue)
        {
            var testCode = $"{GlobalCode()}\r\n" +
                           $"<ROUTINE GO () <PRINTN {Expression()}>>";

            ZlrHelper.RunAndAssert(testCode, input.ToString(), expectedValue, expectWarnings);
        }

        public void Outputs([NotNull] string expectedValue)
        {
            var testCode = $"{GlobalCode()}\r\n" +
                           $"<ROUTINE GO () {Expression()}>";

            ZlrHelper.RunAndAssert(testCode, input.ToString(), expectedValue, expectWarnings, wantCompileOutput);
        }

        public void Implies([ItemNotNull] [NotNull] params string[] conditions)
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

            ZlrHelper.RunAndAssert(testCode, input.ToString(), "PASS", expectWarnings);
        }

        public void DoesNotCompile([CanBeNull] Predicate<ZlrHelperRunResult> resultFilter = null,
            [CanBeNull] string message = null)
        {
            var testCode =
                $"{GlobalCode()}\r\n" +
                "<GLOBAL DUMMY?VAR <>>\r\n" +
                "<ROUTINE GO ()\r\n" +
                $"\t<SETG DUMMY?VAR {Expression()}>\r\n" +
                "\t<QUIT>>";

            var result = ZlrHelper.Run(testCode, null, compileOnly: true, wantDebugInfo: wantDebugInfo);
            Assert.AreEqual(ZlrTestStatus.CompilationFailed, result.Status);

            CheckWarnings(result);

            if (resultFilter != null)
            {
                Assert.IsTrue(resultFilter(result), message ?? "Result filter failed");
            }
        }

        public void DoesNotCompile(string diagnosticCode, [CanBeNull] Predicate<Diagnostic> diagFilter = null)
        {
            DoesNotCompile(res =>
                {
                    var diag = res.Diagnostics.FirstOrDefault(d => d.Code == diagnosticCode);
                    return diag != null && (diagFilter == null || diagFilter(diag));
                },
                $"Expected diagnostic {diagnosticCode} was not produced");
        }

        public void Compiles()
        {
            var testCode =
                $"{GlobalCode()}\r\n" +
                "<GLOBAL DUMMY?VAR <>>\r\n" +
                "<ROUTINE GO ()\r\n" +
                $"\t<SETG DUMMY?VAR {Expression()}>\r\n" +
                "\t<QUIT>>";

            var result = ZlrHelper.Run(testCode, null, compileOnly: true, wantDebugInfo: wantDebugInfo);
            Assert.IsTrue(result.Status > ZlrTestStatus.CompilationFailed,
                "Failed to compile");

            CheckWarnings(result);
        }

        [NotNull]
        public CodeMatchingResult GeneratesCodeMatching([NotNull] string pattern)
        {
            return GeneratesCodeMatching(CheckOutputMatches(pattern));
        }

        [NotNull]
        static Action<string> CheckOutputMatches(string pattern)
        {
            return output =>
                Assert.IsTrue(
                    Regex.IsMatch(output, pattern, RegexOptions.Singleline | RegexOptions.Multiline),
                    "Output did not match. Expected pattern: " + pattern);
        }

        [NotNull]
        public CodeMatchingResult GeneratesCodeNotMatching([NotNull] string pattern)
        {
            return GeneratesCodeMatching(CheckOutputDoesNotMatch(pattern));
        }

        [NotNull]
        static Action<string> CheckOutputDoesNotMatch(string pattern)
        {
            return output =>
                Assert.IsFalse(
                    Regex.IsMatch(output, pattern, RegexOptions.Singleline | RegexOptions.Multiline),
                    "Output should not have matched. Anti-pattern: " + pattern);
        }

        [NotNull]
        CodeMatchingResult GeneratesCodeMatching([NotNull] Action<string> checkGeneratedCode)
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
            });

            return new CodeMatchingResult(output);
        }

        public sealed class CodeMatchingResult
        {
            public string Output { get; }

            public CodeMatchingResult(string output)
            {
                this.Output = output;
            }

            [NotNull]
            public CodeMatchingResult AndMatching([NotNull] string pattern)
            {
                CheckOutputMatches(pattern)(Output);
                return this;
            }

            [NotNull]
            public CodeMatchingResult AndNotMatching([NotNull] string pattern)
            {
                CheckOutputDoesNotMatch(pattern)(Output);
                return this;
            }
        }
    }

    public sealed class ExprAssertionHelper : AbstractAssertionHelperWithEntryPoint<ExprAssertionHelper>
    {
        [NotNull]
        readonly string expression;

        public ExprAssertionHelper([NotNull] string expression)
        {
            this.expression = expression;
        }

        protected override string Expression()
        {
            return expression;
        }
    }

    public sealed class RoutineAssertionHelper : AbstractAssertionHelperWithEntryPoint<RoutineAssertionHelper>
    {
        readonly string argSpec, body;
        string arguments = "";

        const string RoutineName = "TEST?ROUTINE";

        public RoutineAssertionHelper([NotNull] string argSpec, [NotNull] string body)
        {
            this.argSpec = argSpec;
            this.body = body;
        }

        [NotNull]
        public RoutineAssertionHelper WhenCalledWith([NotNull] string testArguments)
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
        public GlobalsAssertionHelper([ItemNotNull] [NotNull] params string[] globals)
        {
            foreach (var g in globals)
                miscGlobals.AppendLine(g);
        }

        protected override string Expression()
        {
            return "<>";
        }
    }

    public sealed class RawAssertionHelper
    {
        [NotNull]
        readonly string code;

        public RawAssertionHelper([NotNull] string code)
        {
            this.code = code;
        }

        public void Outputs([NotNull] string expectedValue)
        {
            ZlrHelper.RunAndAssert(code, null, expectedValue);
        }
    }
}
