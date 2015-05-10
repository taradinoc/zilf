using System;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System.Diagnostics.Contracts;

namespace IntegrationTests
{
    [TestClass]
    public class MacroTests
    {
        private static RoutineAssertionHelper AssertRoutine(string argSpec, string body)
        {
            Contract.Requires(argSpec != null);
            Contract.Requires(!string.IsNullOrWhiteSpace(body));

            return new RoutineAssertionHelper(argSpec, body);
        }

        [TestMethod]
        public void SPLICEs_Should_Work_Inside_Routines()
        {
            // void context
            AssertRoutine("", "<VARIOUS-THINGS> T")
                .WithGlobal("<DEFMAC VARIOUS-THINGS () <CHTYPE '(<TELL \"hello\"> <TELL CR> <TELL \"world\">) SPLICE>>")
                .Outputs("hello\nworld");

            // value context
            AssertRoutine("", "<VARIOUS-THINGS>")
                .WithGlobal("<DEFMAC VARIOUS-THINGS () <CHTYPE '(123 456) SPLICE>>")
                .GivesNumber("456");
        }
    }
}
