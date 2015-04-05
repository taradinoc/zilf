using System;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System.Diagnostics.Contracts;

namespace IntegrationTests
{
    [TestClass]
    public class SyntaxTests
    {
        private static RoutineAssertionHelper AssertRoutine(string argSpec, string body)
        {
            Contract.Requires(argSpec != null);
            Contract.Requires(!string.IsNullOrWhiteSpace(body));

            return new RoutineAssertionHelper(argSpec, body);
        }

        [TestMethod]
        public void First_Preaction_Definition_Per_Action_Name_Should_Persist()
        {
            AssertRoutine("", "<TELL " +
                    "N <=? <GET ,ACTIONS ,V?FOO> ,V-FOO> CR " +
                    "N <=? <GET ,ACTIONS ,V?FOO-WITH> ,V-FOO> CR " +
                    "N <=? <GET ,ACTIONS ,V?BAR> ,V-BAR> CR " +
                    "N <=? <GET ,PREACTIONS ,V?FOO> ,PRE-FOO> CR " +
                    "N <=? <GET ,PREACTIONS ,V?FOO-WITH> <>> CR " +
                    "N <=? <GET ,PREACTIONS ,V?BAR> <>> CR " +
                    ">")
                .WithGlobal("<ROUTINE V-FOO () <>>")
                .WithGlobal("<ROUTINE V-BAR () <>>")
                .WithGlobal("<ROUTINE PRE-FOO () <>>")
                .WithGlobal("<ROUTINE PRE-FOO-2 () <>>")
                .WithGlobal("<ROUTINE PRE-BAR () <>>")
                .WithGlobal("<SYNTAX FOO = V-FOO PRE-FOO>")
                .WithGlobal("<SYNTAX FOO OBJECT = V-FOO PRE-FOO-2>")
                .WithGlobal("<SYNTAX FOO OBJECT AT OBJECT = V-FOO>")
                .WithGlobal("<SYNTAX FOO OBJECT WITH OBJECT = V-FOO <> FOO-WITH>")
                .WithGlobal("<SYNTAX BAR = V-BAR>")
                .WithGlobal("<SYNTAX BAR OBJECT = V-BAR PRE-BAR>")
                .Outputs("1\n1\n1\n1\n1\n1\n");
        }
    }
}
