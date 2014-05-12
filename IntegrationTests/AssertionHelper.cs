using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using System.Collections.Generic;
using System.Diagnostics.Contracts;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace IntegrationTests
{
    abstract class AbstractAssertionHelper<TThis>
        where TThis : AbstractAssertionHelper<TThis>
    {
        protected string zversion = "ZIP";
        protected StringBuilder miscGlobals = new StringBuilder();
        protected StringBuilder input = new StringBuilder();

        protected AbstractAssertionHelper()
        {
            System.Diagnostics.Debug.Assert(this.GetType() == typeof(TThis));
        }

        public TThis InV3()
        {
            zversion = "ZIP";
            return (TThis)this;
        }

        public TThis InV4()
        {
            zversion = "EZIP";
            return (TThis)this;
        }

        public TThis InV5()
        {
            zversion = "XZIP";
            return (TThis)this;
        }

        public TThis InV6()
        {
            zversion = "YZIP";
            return (TThis)this;
        }

        public TThis InV7()
        {
            zversion = "7";
            return (TThis)this;
        }

        public TThis InV8()
        {
            zversion = "8";
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

        protected virtual string GlobalCode()
        {
            var sb = new StringBuilder();
            sb.Append("<VERSION ");
            sb.Append(zversion);
            sb.AppendLine(">");

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

            ZlrHelper.RunAndAssert(testCode, input.ToString(), expectedValue);
        }

        public void Outputs(string expectedValue)
        {
            Contract.Requires(expectedValue != null);

            var testCode = string.Format(
                "{0}\r\n<ROUTINE GO () {1}>",
                GlobalCode(),
                Expression());

            ZlrHelper.RunAndAssert(testCode, input.ToString(), expectedValue);
        }

        public void DoesNotCompile()
        {
            var testCode = string.Format(
                "{0}\r\n<GLOBAL DUMMY?VAR <>>\r\n<ROUTINE GO ()\r\n\t<SETG DUMMY?VAR {1}>\r\n\t<QUIT>>",
                GlobalCode(),
                Expression());

            var result = ZlrHelper.Run(testCode, null, compileOnly: true);
            Assert.AreEqual(ZlrTestStatus.CompilationFailed, result.Status);
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
}
