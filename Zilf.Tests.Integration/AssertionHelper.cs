/* Copyright 2010-2017 Jesse McGrew
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

extern alias JBA;
using System;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Diagnostics.Contracts;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Text.RegularExpressions;
using JetBrains.Annotations;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Zilf.Diagnostics;

namespace Zilf.Tests.Integration
{
    abstract class AbstractAssertionHelper<TThis>
        where TThis : AbstractAssertionHelper<TThis>
    {
        [JBA::JetBrains.Annotations.NotNullAttribute]
        protected string versionDirective = "<VERSION ZIP>";
        [JBA::JetBrains.Annotations.NotNullAttribute]
        protected readonly StringBuilder miscGlobals = new StringBuilder();
        [JBA::JetBrains.Annotations.NotNullAttribute]
        protected readonly StringBuilder input = new StringBuilder();
        protected bool? expectWarnings;
        protected bool wantCompileOutput;

        protected AbstractAssertionHelper()
        {
            Contract.Assume(GetType() == typeof(TThis));
        }

        [JBA::JetBrains.Annotations.NotNullAttribute]
        public TThis InV3()
        {
            versionDirective = "<VERSION ZIP>";
            return (TThis)this;
        }

        [JBA::JetBrains.Annotations.NotNullAttribute]
        public TThis InV4()
        {
            versionDirective = "<VERSION EZIP>";
            return (TThis)this;
        }

        [JBA::JetBrains.Annotations.NotNullAttribute]
        public TThis InV5()
        {
            versionDirective = "<VERSION XZIP>";
            return (TThis)this;
        }

        [JBA::JetBrains.Annotations.NotNullAttribute]
        public TThis InV6()
        {
            versionDirective = "<VERSION YZIP>";
            return (TThis)this;
        }

        [JBA::JetBrains.Annotations.NotNullAttribute]
        public TThis InV7()
        {
            versionDirective = "<VERSION 7>";
            return (TThis)this;
        }

        [JBA::JetBrains.Annotations.NotNullAttribute]
        public TThis InV8()
        {
            versionDirective = "<VERSION 8>";
            return (TThis)this;
        }

        [JBA::JetBrains.Annotations.NotNullAttribute]
        public TThis WithVersionDirective([JBA::JetBrains.Annotations.NotNullAttribute] string versionStr)
        {
            versionDirective = versionStr;
            return (TThis)this;
        }

        [JBA::JetBrains.Annotations.NotNullAttribute]
        public TThis WithGlobal([JBA::JetBrains.Annotations.NotNullAttribute] string code)
        {
            miscGlobals.AppendLine(code);
            return (TThis)this;
        }

        [JBA::JetBrains.Annotations.NotNullAttribute]
        public TThis WithInput([JBA::JetBrains.Annotations.NotNullAttribute] string line)
        {
            input.AppendLine(line);
            return (TThis)this;
        }

        [JBA::JetBrains.Annotations.NotNullAttribute]
        public TThis WithWarnings()
        {
            expectWarnings = true;
            return (TThis)this;
        }

        [JBA::JetBrains.Annotations.NotNullAttribute]
        public TThis WithoutWarnings()
        {
            expectWarnings = false;
            return (TThis)this;
        }

        [JBA::JetBrains.Annotations.NotNullAttribute]
        public TThis CapturingCompileOutput()
        {
            wantCompileOutput = true;
            return (TThis)this;
        }

        [JBA::JetBrains.Annotations.NotNullAttribute]
        protected virtual string GlobalCode()
        {
            var sb = new StringBuilder();
            sb.Append(versionDirective);

            sb.Append(miscGlobals);

            return sb.ToString();
        }
    }

    sealed class EntryPointAssertionHelper : AbstractAssertionHelper<EntryPointAssertionHelper>
    {
        [JBA::JetBrains.Annotations.NotNullAttribute]
        readonly string argSpec, body;

        public EntryPointAssertionHelper([JBA::JetBrains.Annotations.NotNullAttribute] string argSpec, [JBA::JetBrains.Annotations.NotNullAttribute] string body)
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

            var result = ZlrHelper.Run(testCode, null, compileOnly: true);
            Assert.AreEqual(ZlrTestStatus.CompilationFailed, result.Status);
            if (expectWarnings != null)
            {
                Assert.AreEqual((bool)expectWarnings, result.WarningCount != 0);
            }
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
                result = ZlrHelper.Run(testCode, null, compileOnly: true);
            }
            catch (Exception ex)
            {
                Assert.Fail("Expected no exception, but caught {0}", ex);

                // can't get here, but the compiler doesn't know that...
                // ReSharper knows, but we still can't remove the return

                // ReSharper disable once HeuristicUnreachableCode
                return;
            }

            if (expectWarnings != null)
            {
                Assert.AreEqual((bool)expectWarnings, result.WarningCount != 0);
            }
        }
    }

    abstract class AbstractAssertionHelperWithEntryPoint<TThis> : AbstractAssertionHelper<TThis>
        where TThis : AbstractAssertionHelperWithEntryPoint<TThis>
    {
        [JBA::JetBrains.Annotations.NotNullAttribute] protected abstract string Expression();

        [AssertionMethod]
        public void GivesNumber([JBA::JetBrains.Annotations.NotNullAttribute] string expectedValue)
        {
            Contract.Requires(expectedValue != null);

            var testCode = $"{GlobalCode()}\r\n" +
                           $"<ROUTINE GO () <PRINTN {Expression()}>>";

            ZlrHelper.RunAndAssert(testCode, input.ToString(), expectedValue, expectWarnings);
        }

        [AssertionMethod]
        public void Outputs([JBA::JetBrains.Annotations.NotNullAttribute] string expectedValue)
        {
            Contract.Requires(expectedValue != null);

            var testCode = $"{GlobalCode()}\r\n" +
                           $"<ROUTINE GO () {Expression()}>";

            ZlrHelper.RunAndAssert(testCode, input.ToString(), expectedValue, expectWarnings, wantCompileOutput);
        }

        [AssertionMethod]
        public void Implies([JBA::JetBrains.Annotations.ItemNotNullAttribute] [JBA::JetBrains.Annotations.NotNullAttribute] params string[] conditions)
        {
            Contract.Requires(conditions != null);
            Contract.Requires(conditions.Length > 0);
            Contract.Requires(Contract.ForAll(conditions, c => !string.IsNullOrWhiteSpace(c)));

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

        [AssertionMethod]
        public void DoesNotCompile([JBA::JetBrains.Annotations.CanBeNullAttribute] Predicate<ZlrHelperRunResult> resultFilter = null,
            [JBA::JetBrains.Annotations.CanBeNullAttribute] string message = null)
        {
            var testCode =
                $"{GlobalCode()}\r\n" +
                "<GLOBAL DUMMY?VAR <>>\r\n" +
                "<ROUTINE GO ()\r\n" +
                $"\t<SETG DUMMY?VAR {Expression()}>\r\n" +
                "\t<QUIT>>";

            var result = ZlrHelper.Run(testCode, null, compileOnly: true);
            Assert.AreEqual(ZlrTestStatus.CompilationFailed, result.Status);
            if (expectWarnings != null)
            {
                Assert.AreEqual((bool)expectWarnings, result.WarningCount != 0);
            }
            if (resultFilter != null)
            {
                Assert.IsTrue(resultFilter(result), message ?? "Result filter failed");
            }
        }

        [AssertionMethod]
        public void DoesNotCompile<TMessages>(int diagnosticCode, [JBA::JetBrains.Annotations.CanBeNullAttribute] Predicate<Diagnostic> diagFilter = null)
        {
            var attr = typeof(TMessages).GetCustomAttribute<MessageSetAttribute>();
            Debug.Assert(attr != null, "No " + nameof(MessageSetAttribute) + " on " + typeof(TMessages).FullName);

            var prefix = attr.Prefix;
            DoesNotCompile(res =>
                {
                    var diag = res.Diagnostics.FirstOrDefault(d => d.CodePrefix == prefix && d.Code == diagnosticCode);
                    return diag != null && (diagFilter == null || diagFilter(diag));
                },
                $"Expected diagnostic {attr.Prefix}{diagnosticCode:0000} was not produced");
        }

        [AssertionMethod]
        public void Compiles()
        {
            var testCode =
                $"{GlobalCode()}\r\n" +
                "<GLOBAL DUMMY?VAR <>>\r\n" +
                "<ROUTINE GO ()\r\n" +
                $"\t<SETG DUMMY?VAR {Expression()}>\r\n" +
                "\t<QUIT>>";

            var result = ZlrHelper.Run(testCode, null, compileOnly: true);
            Assert.IsTrue(result.Status > ZlrTestStatus.CompilationFailed,
                "Failed to compile");

            switch (expectWarnings)
            {
                case true:
                    Assert.AreNotEqual(0, result.WarningCount, "Expected at least one warning.");
                    break;

                case false:
                    Assert.AreEqual(0, result.WarningCount, "Expected no warnings.");
                    break;
            }
        }

        [AssertionMethod]
        public void GeneratesCodeMatching([JBA::JetBrains.Annotations.NotNullAttribute] string pattern)
        {
            Contract.Requires(pattern != null);

            var testCode = $"{GlobalCode()}\r\n" +
                           "<ROUTINE GO ()\r\n" +
                           $"\t{Expression()}\r\n" +
                           "\t<QUIT>>";

            var helper = new ZlrHelper(testCode, null);
            Assert.IsTrue(helper.Compile(), "Failed to compile");

            var output = helper.GetZapCode();
            Assert.IsTrue(Regex.IsMatch(output, pattern, RegexOptions.Singleline | RegexOptions.Multiline),
                "Output did not match. Expected pattern: " + pattern);

            if (expectWarnings != null)
            {
                Assert.AreEqual((bool)expectWarnings, helper.WarningCount != 0);
            }
        }
    }

    sealed class ExprAssertionHelper : AbstractAssertionHelperWithEntryPoint<ExprAssertionHelper>
    {
        [JBA::JetBrains.Annotations.NotNullAttribute]
        readonly string expression;

        public ExprAssertionHelper([JBA::JetBrains.Annotations.NotNullAttribute] string expression)
        {
            Contract.Requires(!string.IsNullOrWhiteSpace(expression));

            this.expression = expression;
        }

        protected override string Expression()
        {
            return expression;
        }
    }

    sealed class RoutineAssertionHelper : AbstractAssertionHelperWithEntryPoint<RoutineAssertionHelper>
    {
        readonly string argSpec, body;
        string arguments = "";

        const string RoutineName = "TEST?ROUTINE";

        public RoutineAssertionHelper([JBA::JetBrains.Annotations.NotNullAttribute] string argSpec, [JBA::JetBrains.Annotations.NotNullAttribute] string body)
        {
            Contract.Requires(argSpec != null);
            Contract.Requires(!string.IsNullOrWhiteSpace(body));

            this.argSpec = argSpec;
            this.body = body;
        }

        [JBA::JetBrains.Annotations.NotNullAttribute]
        public RoutineAssertionHelper WhenCalledWith([JBA::JetBrains.Annotations.NotNullAttribute] string testArguments)
        {
            Contract.Requires(testArguments != null);
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

    sealed class GlobalsAssertionHelper : AbstractAssertionHelperWithEntryPoint<GlobalsAssertionHelper>
    {
        public GlobalsAssertionHelper([JBA::JetBrains.Annotations.ItemNotNullAttribute] [JBA::JetBrains.Annotations.NotNullAttribute] params string[] globals)
        {
            Contract.Requires(globals != null && globals.Length > 0);
            Contract.Requires(Contract.ForAll(globals, c => !string.IsNullOrWhiteSpace(c)));

            foreach (var g in globals)
                miscGlobals.AppendLine(g);
        }

        protected override string Expression()
        {
            return "<>";
        }
    }

    sealed class RawAssertionHelper
    {
        [JBA::JetBrains.Annotations.NotNullAttribute]
        readonly string code;

        public RawAssertionHelper([JBA::JetBrains.Annotations.NotNullAttribute] string code)
        {
            Contract.Requires(code != null);
            this.code = code;
        }

        public void Outputs([JBA::JetBrains.Annotations.NotNullAttribute] string expectedValue)
        {
            Contract.Requires(expectedValue != null);
            ZlrHelper.RunAndAssert(code, null, expectedValue);
        }

        [ContractInvariantMethod]
        [SuppressMessage("Microsoft.Performance", "CA1822: MarkMembersAsStatic", Justification = "Required for code contracts.")]
        [Conditional("CONTRACTS_FULL")]
        void ObjectInvariant()
        {
            Contract.Invariant(code != null);
        }
    }
}
