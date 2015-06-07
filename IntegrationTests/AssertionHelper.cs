﻿using Microsoft.VisualStudio.TestTools.UnitTesting;
using System.Diagnostics.Contracts;
using System.Text;
using System.Text.RegularExpressions;

namespace IntegrationTests
{
    abstract class AbstractAssertionHelper<TThis>
        where TThis : AbstractAssertionHelper<TThis>
    {
        protected string versionDirective = "<VERSION ZIP>";
        protected StringBuilder miscGlobals = new StringBuilder();
        protected StringBuilder input = new StringBuilder();
        protected bool? expectWarnings = null;

        protected AbstractAssertionHelper()
        {
            Contract.Assume(this.GetType() == typeof(TThis));
        }

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
            expectWarnings = true;
            return (TThis)this;
        }

        protected virtual string GlobalCode()
        {
            var sb = new StringBuilder();
            sb.Append(versionDirective);

            sb.AppendLine("<CONSTANT RELEASEID 1>");

            sb.Append(miscGlobals);

            return sb.ToString();
        }

        protected abstract string Expression();

        public void GivesNumber(string expectedValue)
        {
            Contract.Requires(expectedValue != null);

            var testCode = string.Format(
                "{0}\r\n<ROUTINE GO () <PRINTN {1}>>",
                GlobalCode(),
                Expression());

            ZlrHelper.RunAndAssert(testCode, input.ToString(), expectedValue, expectWarnings);
        }

        public void Outputs(string expectedValue)
        {
            Contract.Requires(expectedValue != null);

            var testCode = string.Format(
                "{0}\r\n<ROUTINE GO () {1}>",
                GlobalCode(),
                Expression());

            ZlrHelper.RunAndAssert(testCode, input.ToString(), expectedValue, expectWarnings);
        }

        public void Implies(params string[] conditions)
        {
            Contract.Requires(conditions != null && conditions.Length > 0);
            Contract.Requires(Contract.ForAll(conditions, c => !string.IsNullOrWhiteSpace(c)));

            var sb = new StringBuilder();
            foreach (var c in conditions)
            {
                sb.AppendFormat("<COND ({0}) (T <INC FAILS> <PRINTI \"FAIL: {1}|\">)>\r\n",
                    c,
                    c.Replace("\\", "\\\\").Replace("\"", "\\\""));
            }

            var testCode = string.Format(
                "{0}\r\n<ROUTINE TEST-IMPLIES (\"AUX\" FAILS) {1} .FAILS>\r\n<ROUTINE GO () <OR <TEST-IMPLIES> <PRINTI \"PASS\">>>",
                GlobalCode(),
                sb);

            ZlrHelper.RunAndAssert(testCode, input.ToString(), "PASS", expectWarnings);
        }

        public void DoesNotCompile()
        {
            var testCode = string.Format(
                "{0}\r\n<GLOBAL DUMMY?VAR <>>\r\n<ROUTINE GO ()\r\n\t<SETG DUMMY?VAR {1}>\r\n\t<QUIT>>",
                GlobalCode(),
                Expression());

            var result = ZlrHelper.Run(testCode, null, compileOnly: true);
            Assert.AreEqual(ZlrTestStatus.CompilationFailed, result.Status);
            if (expectWarnings != null)
            {
                Assert.AreEqual((bool)expectWarnings, result.WarningCount != 0);
            }
        }

        public void Compiles()
        {
            var testCode = string.Format(
                "{0}\r\n<GLOBAL DUMMY?VAR <>>\r\n<ROUTINE GO ()\r\n\t<SETG DUMMY?VAR {1}>\r\n\t<QUIT>>",
                GlobalCode(),
                Expression());

            var result = ZlrHelper.Run(testCode, null, compileOnly: true);
            Assert.IsTrue(result.Status > ZlrTestStatus.CompilationFailed,
                "Failed to compile");
            if (expectWarnings != null)
            {
                if (expectWarnings.Value)
                {
                    Assert.AreNotEqual(0, result.WarningCount, "Expected at least one warning.");
                }
                else
                {
                    Assert.AreEqual(0, result.WarningCount, "Expected no warnings.");
                }
            }
        }

        public void GeneratesCodeMatching(string pattern)
        {
            Contract.Requires(pattern != null);

            var testCode = string.Format(
                "{0}\r\n<ROUTINE GO ()\r\n\t{1}\r\n\t<QUIT>>",
                GlobalCode(),
                Expression());

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

    sealed class ExprAssertionHelper : AbstractAssertionHelper<ExprAssertionHelper>
    {
        private readonly string expression;

        public ExprAssertionHelper(string expression)
        {
            Contract.Requires(!string.IsNullOrWhiteSpace(expression));

            this.expression = expression;
        }

        protected override string Expression()
        {
            return expression;
        }
    }

    sealed class RoutineAssertionHelper : AbstractAssertionHelper<RoutineAssertionHelper>
    {
        private readonly string argSpec, body;
        private string arguments = "";

        private const string RoutineName = "TEST?ROUTINE";

        public RoutineAssertionHelper(string argSpec, string body)
        {
            Contract.Requires(argSpec != null);
            Contract.Requires(!string.IsNullOrWhiteSpace(body));

            this.argSpec = argSpec;
            this.body = body;
        }

        public RoutineAssertionHelper WhenCalledWith(string arguments)
        {
            this.arguments = arguments;
            return this;
        }

        protected override string GlobalCode()
        {
            return string.Format("{0}<ROUTINE {1} ({2}) {3}>",
                base.GlobalCode(), RoutineName, argSpec, body);
        }

        protected override string Expression()
        {
            return string.Format("<{0} {1}>", RoutineName, arguments);
        }
    }

    sealed class GlobalsAssertionHelper : AbstractAssertionHelper<GlobalsAssertionHelper>
    {
        public GlobalsAssertionHelper(params string[] globals)
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
}
