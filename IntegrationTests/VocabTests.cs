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

        [TestMethod]
        public void SIBREAKS_Should_Affect_Lexing()
        {
            AssertRoutine("",
                "<READ ,INBUF ,LEXBUF> " +
                "<TELL N <GETB ,LEXBUF 0> CR " +
                 "N <GETB ,LEXBUF 1> CR> " +
                 "<PRINTB <GET ,LEXBUF 1>> <CRLF> " +
                 "<PRINTB <GET ,LEXBUF 3>> <CRLF> " +
                 "<PRINTB <GET ,LEXBUF 5>> <CRLF> " +
                 "<PRINTB <GET ,LEXBUF 7>> <CRLF>")
                .WithGlobal("<SETG SIBREAKS \"'\">")
                .WithGlobal("<BUZZ GRANT S TOMB>")
                .WithGlobal("<GLOBAL LEXBUF <ITABLE 59 (LEXV) 0 #BYTE 0 #BYTE 0>>")
                .WithGlobal("<GLOBAL INBUF <ITABLE 80 (BYTE LENGTH) 0>>")
                .WithGlobal("<OBJECT DUMMY (DESC \"wuteva\")>")
                .WithGlobal("<GLOBAL HERE DUMMY> <GLOBAL SCORE 0> <GLOBAL MOVES 0>")
                .InV3()
                .WithInput("grant's tomb")
                .Outputs("59\n4\ngrant\n'\ns\ntomb\n");
        }
    }
}
