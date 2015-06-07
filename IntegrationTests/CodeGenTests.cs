using Microsoft.VisualStudio.TestTools.UnitTesting;
using System.Diagnostics.Contracts;

namespace IntegrationTests
{
    [TestClass]
    public class CodeGenTests
    {
        private static RoutineAssertionHelper AssertRoutine(string argSpec, string body)
        {
            Contract.Requires(argSpec != null);
            Contract.Requires(!string.IsNullOrWhiteSpace(body));

            return new RoutineAssertionHelper(argSpec, body);
        }

        private static GlobalsAssertionHelper AssertGlobals(params string[] globals)
        {
            Contract.Requires(globals != null && globals.Length > 0);
            Contract.Requires(Contract.ForAll(globals, c => !string.IsNullOrWhiteSpace(c)));

            return new GlobalsAssertionHelper(globals);
        }

        [TestMethod]
        public void TestAddToVariable()
        {
            AssertRoutine("\"AUX\" X Y", "<SET X <+ .X .Y>>")
                .GeneratesCodeMatching(@"ADD X,Y >X\r\n\s*RETURN X");
        }

        [TestMethod]
        public void TestAddInVoidContextBecomesINC()
        {
            AssertRoutine("\"AUX\" X", "<SET X <+ .X 1>> .X")
                .GeneratesCodeMatching(@"INC 'X\r\n\s*RETURN X");
        }

        [TestMethod]
        public void TestAddInValueContextBecomesINC()
        {
            AssertRoutine("\"AUX\" X", "<SET X <+ .X 1>>")
                .GeneratesCodeMatching(@"INC 'X\r\n\s*RETURN X");
        }

        [TestMethod]
        public void TestSubtractInVoidContextThenLessBecomesDLESS()
        {
            AssertRoutine("\"AUX\" X", "<SET X <- .X 1>> <COND (<L? .X 0> <PRINTI \"blah\">)>")
                .GeneratesCodeMatching(@"DLESS\? 'X,0");
        }

        [TestMethod]
        public void TestSubtractInValueContextThenLessBecomesDLESS()
        {
            AssertRoutine("\"AUX\" X", "<COND (<L? <SET X <- .X 1>> 0> <PRINTI \"blah\">)>")
                .GeneratesCodeMatching(@"DLESS\? 'X,0");
        }

        [TestMethod]
        public void TestRoutineResultIntoVariable()
        {
            AssertRoutine("\"AUX\" FOO", "<SET FOO <WHATEVER>>")
                .WithGlobal("<ROUTINE WHATEVER () 123>")
                .InV3()
                .GeneratesCodeMatching("CALL WHATEVER >FOO");
        }

        [TestMethod]
        public void TestPrintiCrlfRtrueBecomesPRINTR()
        {
            AssertRoutine("", "<PRINTI \"hi\"> <CRLF> <RTRUE>")
                .GeneratesCodeMatching("PRINTR \"hi\"");
        }

        [TestMethod]
        public void TestAdjacentEqualsCombine()
        {
            AssertRoutine("\"AUX\" X", "<COND (<OR <=? .X 1> <=? .X 2>> <RTRUE>)>")
                .GeneratesCodeMatching(@"EQUAL\? X,1,2 /TRUE");
            AssertRoutine("\"AUX\" X", "<COND (<OR <EQUAL? .X 1 2> <EQUAL? .X 3 4>> <RTRUE>)>")
                .GeneratesCodeMatching(@"EQUAL\? X,1,2,3 /TRUE");
            AssertRoutine("\"AUX\" X", "<COND (<OR <EQUAL? .X 1 2 3> <=? .X 4> <EQUAL? .X 5 6>> <RTRUE>)>")
                .GeneratesCodeMatching(@"EQUAL\? X,1,2,3 /TRUE\r\n\s*EQUAL\? X,4,5,6 /TRUE");
        }

        [TestMethod]
        public void TestEqualZeroBecomesZERO_P()
        {
            AssertRoutine("\"AUX\" X", "<COND (<=? .X 0> <RTRUE>)>")
                .GeneratesCodeMatching(@"ZERO\? X /TRUE");
            AssertRoutine("\"AUX\" X", "<COND (<=? 0 .X> <RTRUE>)>")
                .GeneratesCodeMatching(@"ZERO\? X /TRUE");
        }

        [TestMethod]
        public void TestAdjacentEqualsCombineEvenIfZero()
        {
            AssertRoutine("\"AUX\" X", "<COND (<OR <=? .X 0> <=? .X 2>> <RTRUE>)>")
                .GeneratesCodeMatching(@"EQUAL\? X,0,2 /TRUE");
            AssertRoutine("\"AUX\" X", "<COND (<OR <=? .X 0> <=? .X 0>> <RTRUE>)>")
                .GeneratesCodeMatching(@"EQUAL\? X,0,0 /TRUE");
            AssertRoutine("\"AUX\" X", "<COND (<OR <EQUAL? .X 1 2> <=? .X 0> <EQUAL? .X 3 4>> <RTRUE>)>")
                .GeneratesCodeMatching(@"EQUAL\? X,1,2,0 /TRUE");
            AssertRoutine("\"AUX\" X", "<COND (<OR <EQUAL? .X 1 2> <EQUAL? .X 3 0>> <RTRUE>)>")
                .GeneratesCodeMatching(@"EQUAL\? X,1,2,3 /TRUE\r\n\s*ZERO\? X /TRUE");
        }

        [TestMethod]
        public void TestValuePredicateContext()
        {
            AssertRoutine("\"AUX\" X Y", "<COND (<NOT <SET X <FIRST? .Y>>> <RTRUE>)>")
                .GeneratesCodeMatching(@"FIRST\? Y >X \\TRUE");
            AssertRoutine("\"AUX\" X Y", "<COND (<NOT .Y> <SET X <>>) (T <SET X <FIRST? .Y>>)> <OR .X <RTRUE>>")
                .GeneratesCodeMatching(@"FIRST\? Y >X (?![/\\]TRUE)");
        }

        [TestMethod]
        public void TestValuePredicateContext_Calls()
        {
            AssertRoutine("\"AUX\" X", "<COND (<NOT <SET X <FOO>>> <RTRUE>)>")
                .WithGlobal("<ROUTINE FOO () <>>")
                .GeneratesCodeMatching(@"CALL FOO >X\r\n\s*ZERO\? X /TRUE");
        }

        [TestMethod]
        public void TestValuePredicateContext_Constants()
        {
            AssertRoutine("\"AUX\" X", "<COND (<NOT <SET X <>>> <RTRUE>)>")
                .GeneratesCodeMatching(@"SET 'X,0\r\n\s*RTRUE");
            AssertRoutine("\"AUX\" X", "<COND (<NOT <SET X 0>> <RTRUE>)>")
                .GeneratesCodeMatching(@"SET 'X,0\r\n\s*RTRUE");
            AssertRoutine("\"AUX\" X", "<COND (<NOT <SET X 100>> <RTRUE>)>")
                .GeneratesCodeMatching(@"SET 'X,100\r\n\s*RFALSE");
            AssertRoutine("\"AUX\" X", "<COND (<NOT <SET X T>> <RTRUE>)>")
                .GeneratesCodeMatching(@"SET 'X,1\r\n\s*RFALSE");
            AssertRoutine("\"AUX\" X", "<COND (<NOT <SET X \"blah\">> <RTRUE>)>")
                .GeneratesCodeMatching(@"SET 'X,STR\?\d+\r\n\s*RFALSE");
        }

        [TestMethod]
        public void TestMergeAdjacentTerminators()
        {
            AssertRoutine("OBJ \"AUX\" (CNT 0) X",
@"<COND (<SET X <FIRST? .OBJ>>
	<REPEAT ()
		<SET CNT <+ .CNT 1>>
		<COND (<NOT <SET X <NEXT? .X>>> <RETURN>)>>)>
.CNT").WhenCalledWith("<>")
      .GeneratesCodeMatching(@"NEXT\? X >X /\?L\d+\r\n\s*\?L\d+:\s*RETURN CNT\r\n\r\n");
        }

        [TestMethod]
        public void TestBranchToSameCondition()
        {
            AssertRoutine("\"AUX\" I P",
@"<OBJECTLOOP I ,HERE
    <COND (<AND <NOT <FSET? .I ,TOUCHBIT>> <SET P <GETP .I ,P?FDESC>>>
		   <PRINT .P> <CRLF>)>>")
                           .WithGlobal(@"<DEFMAC OBJECTLOOP ('VAR 'LOC ""ARGS"" BODY)
    <FORM REPEAT <LIST <LIST .VAR <FORM FIRST? .LOC>>>
        <FORM COND
            <LIST <FORM LVAL .VAR>
                !.BODY
                <FORM SET .VAR <FORM NEXT? <FORM LVAL .VAR>>>>
            '(ELSE <RETURN>)>>>")
                           .WithGlobal("<GLOBAL HERE <>>")
                           .WithGlobal("<GLOBAL TOUCHBIT <>>")
                           .WithGlobal("<GLOBAL P?FDESC <>>")
                           .GeneratesCodeMatching(@"\A(?:(?!ZERO I\?).)*PRINT P(?:(?!ZERO\? I).)*\Z");
        }

        [TestMethod]
        public void TestReturnOrWithPred()
        {
            AssertRoutine("\"AUX\" X", "<OR <EQUAL? .X 123> <FOO>>")
                .WithGlobal("<ROUTINE FOO () <>>")
                .GeneratesCodeMatching(@"\A(?:(?!PUSH|ZERO\?).)*\Z");
        }

        [TestMethod]
        public void TestSetOrWithPred()
        {
            AssertRoutine("\"AUX\" X Y", "<SET Y <OR <EQUAL? .X 123> <FOO>>>")
                .WithGlobal("<ROUTINE FOO () <>>")
                .GeneratesCodeMatching(@"\A(?:(?!ZERO\?).)*\Z");
        }

        [TestMethod]
        public void TestPrintrOverBranch()
        {
            AssertRoutine("\"AUX\" X", "<COND (.X <PRINTI \"foo\">) (T <PRINTI \"bar\">)> <CRLF> <RTRUE>")
                .GeneratesCodeMatching("PRINTR \"foo\".*PRINTR \"bar\"");
        }

        [TestMethod]
        public void TestSimpleAND_1()
        {
            AssertRoutine("\"AUX\" A", "<AND .A <FOO>>")
                .WithGlobal("<ROUTINE FOO () <>>")
                .GeneratesCodeMatching(@"\A(?:(?!\?TMP).)*\Z");
        }

        [TestMethod]
        public void TestSimpleAND_2()
        {
            AssertRoutine("\"AUX\" A", "<AND <OR <0? .A> <FOO>> <BAR>>")
                .WithGlobal("<ROUTINE FOO () <>>")
                .WithGlobal("<ROUTINE BAR () <>>")
                .GeneratesCodeMatching(@"\A(?:(?!\?TMP).)*\Z");
        }

        [TestMethod]
        public void TestSimpleOR_1()
        {
            AssertRoutine("\"AUX\" A", "<OR .A <FOO>>")
                .WithGlobal("<ROUTINE FOO () <>>")
                .GeneratesCodeMatching(@"\A(?:(?!\?TMP).)*\Z");
        }

        [TestMethod]
        public void TestSimpleOR_2()
        {
            AssertRoutine("\"AUX\" OBJ", "<OR <FIRST? .OBJ> <FOO>>")
                .WithGlobal("<ROUTINE FOO () <>>")
                .GeneratesCodeMatching(@"RETURN \?TMP.*RSTACK");
        }

        [TestMethod]
        public void TestSimpleOR_3()
        {
            AssertRoutine("\"AUX\" A", "<OR <SET A <FOO>> <BAR>>")
                .WithGlobal("<ROUTINE FOO () <>>")
                .WithGlobal("<ROUTINE BAR () <>>")
                .GeneratesCodeMatching(@"\A(?:(?!\?TMP).)*\Z");
        }

        [TestMethod]
        public void TestNestedTempVariables()
        {
            // this code should use 3 temp variables:
            // ?TMP is the value of GLOB before calling FOO
            // ?TMP?1 is the value returned by FOO
            // ?TMP?2 is the value of GLOB before calling BAR

            AssertRoutine("\"AUX\" X", @"<PUT ,GLOB <FOO> <+ .X <GET ,GLOB <BAR>>>>")
                .WithGlobal("<GLOBAL GLOB <>>")
                .WithGlobal("<ROUTINE FOO () <>>")
                .WithGlobal("<ROUTINE BAR () <>>")
                .GeneratesCodeMatching(@"\?TMP\?2");
        }

        [TestMethod]
        public void TestNestedBindVariables()
        {
            AssertRoutine("", @"<BIND (X)
                                  <SET X 0>
                                  <BIND (X)
                                    <SET X 1>
                                    <BIND (X)
                                      <SET X 2>>>>")
                .GeneratesCodeMatching(@"X\?2");
        }

        [TestMethod]
        public void TestTimeHeader_V3()
        {
            AssertRoutine("", "<>")
                .WithVersionDirective("<VERSION ZIP TIME>")
                .GeneratesCodeMatching(@"^\s*\.TIME\s*$");
        }

        [TestMethod]
        public void TestSoundHeader_V3()
        {
            AssertRoutine("", "<>")
                .WithGlobal("<SETG SOUND-EFFECTS? T>")
                .InV3()
                .GeneratesCodeMatching(@"^\s*\.SOUND\s*$");
        }

        [TestMethod]
        public void TestCleanStack_V3_NoClean()
        {
            AssertRoutine("", "<FOO> 456")
                .WithGlobal("<ROUTINE FOO () 123>")
                .InV3()
                .GeneratesCodeMatching(@"\A(?:(?!FSTACK).)*\Z");
        }

        [TestMethod]
        public void TestCleanStack_V3_Clean()
        {
            AssertRoutine("", "<FOO> 456")
                .WithGlobal("<FILE-FLAGS CLEAN-STACK?>")
                .WithGlobal("<ROUTINE FOO () 123>")
                .InV3()
                .GeneratesCodeMatching(@"FSTACK");
        }

        [TestMethod]
        public void TestCleanStack_V4_NoClean()
        {
            AssertRoutine("", "<FOO> 456")
                .WithGlobal("<ROUTINE FOO () 123>")
                .InV4()
                .GeneratesCodeMatching(@"\A(?:(?!FSTACK).)*\Z");
        }

        [TestMethod]
        public void TestCleanStack_V4_Clean()
        {
            AssertRoutine("", "<FOO> 456")
                .WithGlobal("<FILE-FLAGS CLEAN-STACK?>")
                .WithGlobal("<ROUTINE FOO () 123>")
                .InV4()
                .GeneratesCodeMatching(@"FSTACK");
        }

        [TestMethod]
        public void TestReuseTemp()
        {
            // the first G? allocates one temp var, then releases it afterward
            // the BIND consumes the same temp var and binds it to a new atom
            // the second G? allocates a new temp var, which must not collide with the first
            AssertRoutine("",
                "<COND (<G? <FOO> <BAR>> <RTRUE>)> " +
                "<BIND ((Z 0)) <COND (<G? <BAR> <FOO>> <RFALSE>)>>")
                .WithGlobal("<ROUTINE FOO () 123>")
                .WithGlobal("<ROUTINE BAR () 456>")
                .Compiles();
        }

        [TestMethod]
        public void TestNoTempForSet()
        {
            // this shouldn't use any temp vars, since the expressions are going into named variables
            AssertRoutine("\"AUX\" X Y",
                "<COND (<G? <SET X <FOO>> <SET Y <BAR>>> <RTRUE>)> " +
                "<BIND ((Z 0)) <COND (<G? <SET X <BAR>> <SET Y <FOO>>> <RFALSE>)>>")
                .WithGlobal("<ROUTINE FOO () 123>")
                .WithGlobal("<ROUTINE BAR () 456>")
                .GeneratesCodeMatching(@"\A(?:(?!\?TMP).)*\Z");

            // this one should, since X is modified in a subsequent arg
            AssertRoutine("\"AUX\" X Y",
                "<COND (<G? <SET X <FOO>> <SET Y <SET X <BAR>>>> <RTRUE>)> " +
                "<BIND ((Z 0)) <COND (<G? <SET X <BAR>> <SET Y <SET X <FOO>>>> <RFALSE>)>>")
                .WithGlobal("<ROUTINE FOO () 123>")
                .WithGlobal("<ROUTINE BAR () 456>")
                .GeneratesCodeMatching(@"\?TMP");
        }

        [TestMethod]
        public void CHRSET_Should_Generate_Directive()
        {
            AssertGlobals(
                "<CHRSET 0 \"zyxwvutsrqponmlkjihgfedcba\">")
                .InV5()
                .GeneratesCodeMatching(@"\.CHRSET 0,122,121,120,119,118,117,116,115,114,113,112,111,110,109,108,107,106,105,104,103,102,101,100,99,98,97");
        }
    }
}
