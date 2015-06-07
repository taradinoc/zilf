using Microsoft.VisualStudio.TestTools.UnitTesting;
using System.Diagnostics.Contracts;

namespace IntegrationTests
{
    [TestClass]
    public class QuirksTests
    {
        private static RoutineAssertionHelper AssertRoutine(string argSpec, string body)
        {
            Contract.Requires(argSpec != null);
            Contract.Requires(!string.IsNullOrWhiteSpace(body));

            return new RoutineAssertionHelper(argSpec, body);
        }

        [TestMethod]
        public void TestGVALWithLocal()
        {
            AssertRoutine("\"AUX\" (X 5)", "<FOO ,X>")
                .WithGlobal("<ROUTINE FOO (A) .A>")
                .WithWarnings()
                .GivesNumber("5");
        }

        [TestMethod]
        public void TestLVALWithGlobal()
        {
            AssertRoutine("", "<FOO .X>")
                .WithGlobal("<GLOBAL X 5>")
                .WithGlobal("<ROUTINE FOO (A) .A>")
                .WithWarnings()
                .GivesNumber("5");
        }
    }
}
