using System;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System.Diagnostics.Contracts;
using Zilf;

namespace IntegrationTests
{
    [TestClass]
    public class VocabTests
    {
        private static RoutineAssertionHelper AssertRoutine(string argSpec, string body)
        {
            Contract.Requires(argSpec != null);
            Contract.Requires(!string.IsNullOrWhiteSpace(body));

            return new RoutineAssertionHelper(argSpec, body);
        }

        [TestMethod]
        public void VOC_With_2nd_Arg_Atom_Should_Set_PartOfSpeech()
        {
            AssertRoutine("\"AUX\" (P <GET ,VOC-TABLE 0>)", "<GETB .P 4>")
                .WithGlobal("<GLOBAL VOC-TABLE <PTABLE <VOC \"XYZZY\" ADJ>>>")
                .InV3()
                .GivesNumber(((int)(PartOfSpeech.Adjective | PartOfSpeech.AdjectiveFirst)).ToString());
        }

        [TestMethod]
        public void VOC_With_2nd_Arg_False_Should_Not_Set_PartOfSpeech()
        {
            AssertRoutine("\"AUX\" (P <GET ,VOC-TABLE 0>)", "<GETB .P 4>")
                .WithGlobal("<GLOBAL VOC-TABLE <PTABLE <VOC \"XYZZY\" <>>>>")
                .InV3()
                .GivesNumber(((int)(PartOfSpeech.None)).ToString());
        }

        [TestMethod]
        public void VOC_With_2nd_Arg_Missing_Should_Not_Set_PartOfSpeech()
        {
            AssertRoutine("\"AUX\" (P <GET ,VOC-TABLE 0>)", "<GETB .P 4>")
                .WithGlobal("<GLOBAL VOC-TABLE <PTABLE <VOC \"XYZZY\">>>")
                .InV3()
                .GivesNumber(((int)(PartOfSpeech.None)).ToString());
        }
    }
}
