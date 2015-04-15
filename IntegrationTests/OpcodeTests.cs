using System;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System.Diagnostics.Contracts;
using Zilf;

namespace IntegrationTests
{
    [TestClass]
    public class FormatArgCountTests
    {
        [TestMethod]
        public void TestExactly()
        {
            Assert.AreEqual("exactly 1 argument",
                Compiler.FormatArgCount(new[] {
                    new Compiler.ArgCountRange(1, 1),
                    new Compiler.ArgCountRange(1, 1),
                }));
        }

        [TestMethod]
        public void TestAlternatives()
        {
            Assert.AreEqual("1 or 2 arguments",
                Compiler.FormatArgCount(new[] {
                    new Compiler.ArgCountRange(1, 2),
                }));

            Assert.AreEqual("1 or 2 arguments",
                Compiler.FormatArgCount(new[] {
                    new Compiler.ArgCountRange(1, 1),
                    new Compiler.ArgCountRange(2, 2),
                }));

            Assert.AreEqual("2 or 4 arguments",
                Compiler.FormatArgCount(new[] {
                    new Compiler.ArgCountRange(2, 2),
                    new Compiler.ArgCountRange(4, 4),
                }));

            Assert.AreEqual("0, 2, or 4 arguments",
                Compiler.FormatArgCount(new[] {
                    new Compiler.ArgCountRange(0, 0),
                    new Compiler.ArgCountRange(2, 2),
                    new Compiler.ArgCountRange(4, 4),
                }));
        }

        [TestMethod]
        public void TestRange()
        {
            Assert.AreEqual("1 to 3 arguments",
                Compiler.FormatArgCount(new[] {
                    new Compiler.ArgCountRange(1, 3),
                }));

            Assert.AreEqual("1 to 3 arguments",
                Compiler.FormatArgCount(new[] {
                    new Compiler.ArgCountRange(1, 2),
                    new Compiler.ArgCountRange(3, 3),
                }));

            Assert.AreEqual("1 to 3 arguments",
                Compiler.FormatArgCount(new[] {
                    new Compiler.ArgCountRange(1, 1),
                    new Compiler.ArgCountRange(2, 2),
                    new Compiler.ArgCountRange(3, 3),
                }));
        }

        [TestMethod]
        public void TestUnlimited()
        {
            Assert.AreEqual("1 or more arguments",
                Compiler.FormatArgCount(new[] {
                    new Compiler.ArgCountRange(1, null),
                }));

            Assert.AreEqual("1 or more arguments",
                Compiler.FormatArgCount(new[] {
                    new Compiler.ArgCountRange(1, 2),
                    new Compiler.ArgCountRange(3, null),
                }));
        }

        [TestMethod]
        public void TestDisjointRanges()
        {
            Assert.AreEqual("1, 2, or 4 arguments",
                Compiler.FormatArgCount(new[] {
                    new Compiler.ArgCountRange(1, 2),
                    new Compiler.ArgCountRange(4, 4),
                }));

            Assert.AreEqual("0, 1, 3, or 4 arguments",
                Compiler.FormatArgCount(new[] {
                    new Compiler.ArgCountRange(0, 1),
                    new Compiler.ArgCountRange(3, 4),
                }));

            Assert.AreEqual("0, 2, or more arguments",
                Compiler.FormatArgCount(new[] {
                    new Compiler.ArgCountRange(0, 0),
                    new Compiler.ArgCountRange(2, null),
                }));
        }
    }
    
    [TestClass]
    public class OpcodeTests
    {
        private static ExprAssertionHelper AssertExpr(string expression)
        {
            Contract.Requires(!string.IsNullOrWhiteSpace(expression));

            return new ExprAssertionHelper(expression);
        }

        private static RoutineAssertionHelper AssertRoutine(string argSpec, string body)
        {
            Contract.Requires(argSpec != null);
            Contract.Requires(!string.IsNullOrWhiteSpace(body));

            return new RoutineAssertionHelper(argSpec, body);
        }

        #region Z-Machine Opcodes

        [TestMethod]
        public void TestADD()
        {
            AssertExpr("<+ 1 2>").GivesNumber("3");
            AssertExpr("<+ 1 -2>").GivesNumber("-1");
            AssertExpr("<+ 32767 1>").GivesNumber("-32768");
            AssertExpr("<+ -32768 -1>").GivesNumber("32767");
            AssertExpr("<+>").GivesNumber("0");
            AssertExpr("<+ 5>").GivesNumber("5");
            AssertExpr("<+ 1 2 3>").GivesNumber("6");
            AssertExpr("<+ 1 2 3 4>").GivesNumber("10");
            AssertExpr("<+ 1 2 3 4 5>").GivesNumber("15");

            // alias
            AssertExpr("<ADD 1 2>").GivesNumber("3");
        }

        [TestMethod]
        public void TestADD_REST()
        {
            // alias where 2nd operand defaults to 1
            AssertExpr("<REST 1>").GivesNumber("2");
            AssertExpr("<REST 1 2>").GivesNumber("3");
        }

        [TestMethod]
        public void TestAPPLY()
        {
            AssertExpr("<APPLY 0>").GivesNumber("0");
            AssertExpr("<APPLY 0 1 2 3>").GivesNumber("0");
            AssertExpr("<APPLY 0 1 2 3 4 5 6 7>").InV5().GivesNumber("0");

            AssertRoutine("\"AUX\" X", "<SET X ,OTHER-ROUTINE> <APPLY .X 12>")
                .WithGlobal("<ROUTINE OTHER-ROUTINE (N) <* .N 2>>")
                .GivesNumber("24");
        }

        [TestMethod]
        public void TestAPPLY_ChoosesValueCallForPred()
        {
            /* V5 has void-context and value-context versions of APPLY.
             * the void-context version is always true in predicate context,
             * so we need to prefer the value-context version. */

            AssertRoutine("\"AUX\" X", "<SET X ,FALSE-ROUTINE> <COND (<APPLY .X> 123) (T 456)>")
                .InV5()
                .WithGlobal("<ROUTINE FALSE-ROUTINE () 0>")
                .GivesNumber("456");
            AssertRoutine("\"AUX\" X", "<SET X ,FALSE-ROUTINE> <COND (<NOT <APPLY .X>> 123) (T 456)>")
                .InV5()
                .WithGlobal("<ROUTINE FALSE-ROUTINE () 0>")
                .GivesNumber("123");
        }

        [TestMethod]
        public void TestAPPLY_Error()
        {
            AssertExpr("<APPLY>").DoesNotCompile();
            AssertExpr("<APPLY 0 1 2 3 4>").InV3().DoesNotCompile();
            AssertExpr("<APPLY 0 1 2 3 4 5 6 7 8>").InV5().DoesNotCompile();
        }

        [TestMethod]
        public void TestASH()
        {
            // only exists in V5+
            AssertExpr("<ASH 4 0>").InV5().GivesNumber("4");
            AssertExpr("<ASH 4 1>").InV5().GivesNumber("8");
            AssertExpr("<ASH 4 -2>").InV5().GivesNumber("1");

            // alias
            AssertExpr("<ASHIFT 4 0>").InV5().GivesNumber("4");
        }

        [TestMethod]
        public void TestASH_Error()
        {
            AssertExpr("<ASH 4 0>").InV3().DoesNotCompile();
            AssertExpr("<ASH 4 0>").InV4().DoesNotCompile();

            AssertExpr("<ASH>").InV5().DoesNotCompile();
            AssertExpr("<ASH 4>").InV5().DoesNotCompile();
            AssertExpr("<ASH 4 1 9>").InV5().DoesNotCompile();
        }

        [TestMethod]
        public void TestASSIGNED_P()
        {
            AssertRoutine("X", "<ASSIGNED? X>").InV5()
                .WhenCalledWith("999").GivesNumber("1");
            AssertRoutine("\"OPT\" X", "<ASSIGNED? X>").InV5()
                .WhenCalledWith("0").GivesNumber("1");
            AssertRoutine("\"OPT\" X", "<ASSIGNED? X>").InV5()
                .WhenCalledWith("").GivesNumber("0");
        }

        [TestMethod]
        public void TestASSIGNED_P_Error()
        {
            AssertRoutine("X", "<ASSIGNED? Y>").InV5().DoesNotCompile();
            AssertRoutine("X", "<ASSIGNED? 1>").InV5().DoesNotCompile();
            AssertRoutine("X", "<ASSIGNED?>").InV5().DoesNotCompile();
            AssertRoutine("X", "<ASSIGNED? X X>").InV5().DoesNotCompile();
        }

        [TestMethod]
        public void TestBAND()
        {
            AssertExpr("<BAND 33 96>").GivesNumber("32");

            // alias
            AssertExpr("<ANDB 33 96>").GivesNumber("32");
        }

        [TestMethod]
        public void TestBAND_Error()
        {
            AssertExpr("<BAND>").DoesNotCompile();
            AssertExpr("<BAND 33>").DoesNotCompile();
            AssertExpr("<BAND 33 96 64>").DoesNotCompile();
        }

        [TestMethod]
        public void TestBCOM()
        {
            AssertExpr("<BCOM 32767>").GivesNumber("-32768");

            // opcode changes in V5
            AssertExpr("<BCOM 32767>").InV5().GivesNumber("-32768");
        }

        [TestMethod]
        public void TestBCOM_Error()
        {
            AssertExpr("<BCOM>").DoesNotCompile();
            AssertExpr("<BCOM 33 96>").DoesNotCompile();
        }

        [TestMethod]
        public void TestBOR()
        {
            AssertExpr("<BOR 33 96>").GivesNumber("97");

            // alias
            AssertExpr("<ORB 33 96>").GivesNumber("97");
        }

        [TestMethod]
        public void TestBOR_Error()
        {
            AssertExpr("<BOR>").DoesNotCompile();
            AssertExpr("<BOR 33>").DoesNotCompile();
            AssertExpr("<BOR 33 96 64>").DoesNotCompile();
        }

        [TestMethod]
        public void TestBTST()
        {
            AssertExpr("<BTST 64 64>").GivesNumber("1");
            AssertExpr("<BTST 64 63>").GivesNumber("0");
            AssertExpr("<BTST 97 33>").GivesNumber("1");
        }

        [TestMethod]
        public void TestBTST_Error()
        {
            AssertExpr("<BTST>").DoesNotCompile();
            AssertExpr("<BTST 97>").DoesNotCompile();
            AssertExpr("<BTST 97 31 29>").DoesNotCompile();
        }

        [TestMethod]
        public void TestBUFOUT()
        {
            // only exists in V4+

            // we can't really test its side-effect here
            AssertExpr("<BUFOUT 0>").InV4().GivesNumber("1");
        }

        [TestMethod]
        public void TestBUFOUT_Error()
        {
            AssertExpr("<BUFOUT 0>").InV3().DoesNotCompile();

            AssertExpr("<BUFOUT>").InV4().DoesNotCompile();
            AssertExpr("<BUFOUT 0 1>").InV4().DoesNotCompile();
        }

        // CALL1 and CALL2 are not supported in ZIL

        [TestMethod]
        public void TestCATCH()
        {
            // only exists in V5+

            // the return value is unpredictable
            AssertExpr("<CATCH>").InV5().Compiles();
        }

        [TestMethod]
        public void TestCATCH_Error()
        {
            AssertExpr("<CATCH>").InV3().DoesNotCompile();
            AssertExpr("<CATCH>").InV4().DoesNotCompile();

            AssertExpr("<CATCH 123>").InV5().DoesNotCompile();
        }

        [TestMethod]
        public void TestCHECKU()
        {
            // only exists in V5+

            // only the lower 2 bits of the return value are defined
            AssertExpr("<BAND 3 <CHECKU 65>>").InV5().GivesNumber("3");
        }

        [TestMethod]
        public void TestCHECKU_Error()
        {
            AssertExpr("<CHECKU 65>").InV3().DoesNotCompile();
            AssertExpr("<CHECKU 65>").InV4().DoesNotCompile();

            AssertExpr("<CHECKU>").InV5().DoesNotCompile();
            AssertExpr("<CHECKU 65 66>").InV5().DoesNotCompile();
        }

        [TestMethod]
        public void TestCLEAR()
        {
            // only exists in V4+

            // we can't really test its side-effect here
            AssertExpr("<CLEAR 0>").InV4().GivesNumber("1");
        }

        [TestMethod]
        public void TestCLEAR_Error()
        {
            AssertExpr("<CLEAR 0>").InV3().DoesNotCompile();

            AssertExpr("<CLEAR>").InV4().DoesNotCompile();
            AssertExpr("<CLEAR 0 1>").InV4().DoesNotCompile();
        }

        [TestMethod]
        public void TestCOLOR()
        {
            // only exists in V5+

            // we can't really test its side-effect here
            AssertExpr("<COLOR 5 5>").InV5().GivesNumber("1");
        }

        [TestMethod]
        public void TestCOLOR_V6()
        {
            Assert.Inconclusive();

            // third argument is supported in V6+
            AssertExpr("<COLOR 5 5 1>").InV6().Compiles();
        }

        [TestMethod]
        public void TestCOLOR_Error()
        {
            AssertExpr("<COLOR 5 5>").InV3().DoesNotCompile();
            AssertExpr("<COLOR 5 5>").InV4().DoesNotCompile();

            AssertExpr("<COLOR 5 5 1>").InV5().DoesNotCompile();

            AssertExpr("<COLOR>").InV5().DoesNotCompile();
            AssertExpr("<COLOR 5>").InV5().DoesNotCompile();
        }

        [TestMethod]
        public void TestCOPYT()
        {
            // only exists in V5+

            AssertRoutine("", "<COPYT ,TABLE1 ,TABLE2 6> <GET ,TABLE2 2>")
                .InV5()
                .WithGlobal("<GLOBAL TABLE1 <TABLE 1 2 3>>")
                .WithGlobal("<GLOBAL TABLE2 <TABLE 0 0 0>>")
                .GivesNumber("3");
        }

        [TestMethod]
        public void TestCOPYT_Error()
        {
            // only exists in V5+
            AssertExpr("<COPYT 0 0 0>").InV3().DoesNotCompile();
            AssertExpr("<COPYT 0 0 0>").InV4().DoesNotCompile();

            AssertExpr("<COPYT>").InV5().DoesNotCompile();
            AssertExpr("<COPYT 0>").InV5().DoesNotCompile();
            AssertExpr("<COPYT 0 0>").InV5().DoesNotCompile();
            AssertExpr("<COPYT 0 0 0 0>").InV5().DoesNotCompile();
        }

        [TestMethod]
        public void TestCRLF()
        {
            AssertExpr("<CRLF>").Outputs("\n");
        }

        [TestMethod]
        public void TestCRLF_Error()
        {
            AssertExpr("<CRLF 1>").DoesNotCompile();
        }

        [TestMethod]
        public void TestCURGET()
        {
            // only exists in V4+

            // needs a table
            AssertExpr("<CURGET ,CURTABLE>")
                .InV4()
                .WithGlobal("<GLOBAL CURTABLE <TABLE 0 0>>")
                .Compiles();
        }

        [TestMethod]
        public void TestCURGET_Error()
        {
            // only exists in V4+
            AssertExpr("<CURGET 0>").InV3().DoesNotCompile();

            AssertExpr("<CURGET>").InV4().DoesNotCompile();
            AssertExpr("<CURGET 0 0>").InV4().DoesNotCompile();
        }

        [TestMethod]
        public void TestCURSET()
        {
            // only exists in V4+

            // we can't really test its side-effect here
            AssertExpr("<CURSET 1 1>").InV4().GivesNumber("1");
        }

        [TestMethod]
        public void TestCURSET_Error()
        {
            // only exists in V4+
            AssertExpr("<CURSET 1 1>").InV3().DoesNotCompile();

            AssertExpr("<CURSET>").InV4().DoesNotCompile();
            AssertExpr("<CURSET 1>").InV4().DoesNotCompile();
            AssertExpr("<CURSET 1 1 1>").InV4().DoesNotCompile();
        }

        [TestMethod]
        public void TestDCLEAR_V6()
        {
            // only exists in V6+
            Assert.Inconclusive();
        }

        [TestMethod]
        public void TestDEC()
        {
            AssertRoutine("FOO", "<DEC FOO> .FOO").WhenCalledWith("200").GivesNumber("199");
        }

        [TestMethod]
        public void TestDEC_Error()
        {
            AssertExpr("<DEC>").DoesNotCompile();
            AssertExpr("<DEC 1>").DoesNotCompile();
            AssertRoutine("FOO", "<DEC BAR>").DoesNotCompile();
        }

        [TestMethod]
        public void TestDIRIN()
        {
            AssertExpr("<DIRIN 0>").GivesNumber("1");
        }

        [TestMethod]
        public void TestDIRIN_Error()
        {
            AssertExpr("<DIRIN>").DoesNotCompile();
            AssertExpr("<DIRIN 0 0>").DoesNotCompile();
        }

        [TestMethod]
        public void TestDIROUT()
        {
            AssertExpr("<DIROUT 1>").GivesNumber("1");

            // output stream 3 needs a table
            AssertRoutine("", "<DIROUT 3 ,OUTTABLE> <PRINTI \"A\"> <DIROUT -3> <GETB ,OUTTABLE 2>")
                .WithGlobal("<GLOBAL OUTTABLE <LTABLE (BYTE) 0 0 0 0 0 0 0 0>>")
                .GivesNumber("65");
        }

        [TestMethod]
        public void TestDIROUT_V6()
        {
            Assert.Inconclusive();

            // third operand allowed in V6
            AssertExpr("<DIROUT 3 0 0>").InV6().Compiles();
        }

        [TestMethod]
        public void TestDIROUT_Error()
        {
            AssertExpr("<DIROUT>").DoesNotCompile();
            AssertExpr("<DIROUT 3 0 0>").InV5().DoesNotCompile();
        }

        [TestMethod]
        public void TestDISPLAY_V6()
        {
            // only exists in V6
            Assert.Inconclusive();
        }

        [TestMethod]
        public void TestDIV()
        {
            AssertExpr("<DIV 360 90>").GivesNumber("4");
            AssertExpr("<DIV 100 -2>").GivesNumber("-50");
            AssertExpr("<DIV -100 -2>").GivesNumber("50");
            AssertExpr("<DIV -17 2>").GivesNumber("-8");
            AssertExpr("<DIV>").GivesNumber("1");
            AssertExpr("<DIV 1>").GivesNumber("1");
            AssertExpr("<DIV 2>").GivesNumber("0");
            AssertExpr("<DIV 1 1>").GivesNumber("1");
            AssertExpr("<DIV 1 1 1>").GivesNumber("1");
        }

        [TestMethod]
        public void TestDLESS_P()
        {
            // V1 to V6
            AssertRoutine("FOO", "<PRINTN <DLESS? FOO 100>> <CRLF> <PRINTN .FOO>")
                .WhenCalledWith("100").Outputs("1\n99");
            AssertRoutine("FOO", "<PRINTN <DLESS? FOO 100>> <CRLF> <PRINTN .FOO>")
                .WhenCalledWith("101").Outputs("0\n100");
        }

        [TestMethod]
        public void TestDLESS_P_Error()
        {
            // V1 to V6
            AssertExpr("<DLESS?>").DoesNotCompile();
            AssertRoutine("FOO", "<DLESS? FOO>").DoesNotCompile();
            AssertExpr("<DLESS? 11 22>").DoesNotCompile();
            AssertRoutine("FOO", "<DLESS? BAR 100>").DoesNotCompile();
            AssertRoutine("FOO BAR", "<DLESS? FOO BAR>").DoesNotCompile();
        }

        [TestMethod]
        public void TestEQUAL_P()
        {
            AssertExpr("<EQUAL? 1 1>").GivesNumber("1");
            AssertExpr("<EQUAL? 1 2>").GivesNumber("0");
            AssertExpr("<EQUAL? 1 2 1>").GivesNumber("1");
            AssertExpr("<EQUAL? 1 2 3 4>").GivesNumber("0");
            AssertExpr("<EQUAL? 1 2 3 4 5 6 7 8 9 0 1>").GivesNumber("1");

            AssertExpr("<COND (<EQUAL? 1 2 3 4 5 6 1> 99) (T 0)>").GivesNumber("99");
            AssertRoutine("X", "<COND (<EQUAL? <+ .X 1> 2 4 6 8> 99) (T 0)>")
                .WhenCalledWith("7")
                .GivesNumber("99");

            // alias
            AssertExpr("<=? 1 1>").GivesNumber("1");
            AssertExpr("<==? 1 1>").GivesNumber("1");
        }

        [TestMethod]
        public void TestEQUAL_P_Error()
        {
            AssertExpr("<EQUAL?>").DoesNotCompile();
        }

        [TestMethod]
        public void TestERASE()
        {
            // only exists in V4+

            // we can't really test its side-effect here
            AssertExpr("<ERASE 1>").InV4().GivesNumber("1");
        }

        [TestMethod]
        public void TestERASE_Error()
        {
            // only exists in V4+
            AssertExpr("<ERASE 1>").InV3().DoesNotCompile();

            AssertExpr("<ERASE>").InV4().DoesNotCompile();
            AssertExpr("<ERASE 1 2>").InV4().DoesNotCompile();
        }

        [TestMethod]
        public void TestFCLEAR()
        {
            AssertExpr("<FCLEAR ,MYOBJECT ,FOOBIT>")
                .WithGlobal("<OBJECT MYOBJECT (FLAGS FOOBIT)>")
                .GivesNumber("1");
        }

        [TestMethod]
        public void TestFCLEAR_Error()
        {
            AssertExpr("<FCLEAR>").DoesNotCompile();
            AssertExpr("<FCLEAR 1>").DoesNotCompile();
            AssertExpr("<FCLEAR 1 2 3>").DoesNotCompile();
        }

        [TestMethod]
        public void TestFIRST_P()
        {
            AssertExpr("<FIRST? ,MYOBJECT>")
                .WithGlobal("<OBJECT MYOBJECT>")
                .GivesNumber("0");
            AssertExpr("<==? <FIRST? ,MYOBJECT> ,INNEROBJECT>")
                .WithGlobal("<OBJECT MYOBJECT>")
                .WithGlobal("<OBJECT INNEROBJECT (LOC MYOBJECT)>")
                .GivesNumber("1");
            AssertExpr("<COND (<FIRST? ,MYOBJECT> <PRINTI \"yes\">)>")
                .WithGlobal("<OBJECT MYOBJECT>")
                .WithGlobal("<OBJECT INNEROBJECT (LOC MYOBJECT)>")
                .Outputs("yes");
            AssertExpr("<COND (<FIRST? ,INNEROBJECT> <PRINTI \"yes\">) (T <PRINTI \"no\">)>")
                .WithGlobal("<OBJECT MYOBJECT>")
                .WithGlobal("<OBJECT INNEROBJECT (LOC MYOBJECT)>")
                .Outputs("no");
        }

        [TestMethod]
        public void TestFIRST_P_Error()
        {
            AssertExpr("<FIRST?>").DoesNotCompile();
            AssertExpr("<FIRST? 0 0>").DoesNotCompile();
        }

        [TestMethod]
        public void TestFONT()
        {
            // only exists in V5+
            AssertExpr("<FONT 1>").InV5().Compiles();
        }

        [TestMethod]
        public void TestFONT_Error()
        {
            // only exists in V5+
            AssertExpr("<FONT 1>").InV3().DoesNotCompile();
            AssertExpr("<FONT 1>").InV4().DoesNotCompile();

            AssertExpr("<FONT>").InV5().DoesNotCompile();
            AssertExpr("<FONT 1 2>").InV5().DoesNotCompile();
        }

        [TestMethod]
        public void TestFSET()
        {
            AssertExpr("<FSET ,MYOBJECT ,FOOBIT>")
                .WithGlobal("<OBJECT MYOBJECT (FLAGS FOOBIT)>")
                .GivesNumber("1");
        }

        [TestMethod]
        public void TestFSET_Error()
        {
            AssertExpr("<FSET>").DoesNotCompile();
            AssertExpr("<FSET 0>").DoesNotCompile();
            AssertExpr("<FSET 0 1 2>").DoesNotCompile();
        }

        [TestMethod]
        public void TestFSET_P()
        {
            AssertRoutine("", "<PRINTN <FSET? ,OBJECT1 FOOBIT>> <CRLF> <PRINTN <FSET? ,OBJECT2 FOOBIT>>")
                .WithGlobal("<OBJECT OBJECT1 (FLAGS FOOBIT)>")
                .WithGlobal("<OBJECT OBJECT2>")
                .Outputs("1\n0");
        }

        [TestMethod]
        public void TestFSET_P_Error()
        {
            AssertExpr("<FSET?>").DoesNotCompile();
            AssertExpr("<FSET? 0>").DoesNotCompile();
            AssertExpr("<FSET? 0 1 2>").DoesNotCompile();
        }

        [TestMethod]
        public void TestFSTACK_V6()
        {
            // only the V6 version is supported in ZIL
            Assert.Inconclusive(); 
            AssertExpr("<FSTACK 0>").InV6().Compiles();
            AssertExpr("<FSTACK 0 0>").InV6().Compiles();
        }

        [TestMethod]
        public void TestFSTACK_Error()
        {
            // only the V6 version is supported in ZIL
            AssertExpr("<FSTACK 0>").InV3().DoesNotCompile();
            AssertExpr("<FSTACK 0>").InV4().DoesNotCompile();
            AssertExpr("<FSTACK 0>").InV5().DoesNotCompile();

            AssertExpr("<FSTACK>").InV6().DoesNotCompile();
            AssertExpr("<FSTACK 0 0 0>").InV6().DoesNotCompile();
        }

        [TestMethod]
        public void TestGET()
        {
            // V1 to V6
            // 2 to 2 operands
            AssertExpr("<GET 0 0>").InV3().Compiles();
        }

        [TestMethod]
        public void TestGET_Error()
        {
            // V1 to V6
            // 2 to 2 operands
            AssertExpr("<GET>").InV3().DoesNotCompile();
            AssertExpr("<GET 0>").InV3().DoesNotCompile();
            AssertExpr("<GET 0 0 0>").InV3().DoesNotCompile();
        }

        [TestMethod]
        public void TestGETB()
        {
            // V1 to V6
            // 2 to 2 operands
            AssertExpr("<GETB 0 0>").InV3().GivesNumber("3");
        }

        [TestMethod]
        public void TestGETB_Error()
        {
            // V1 to V6
            // 2 to 2 operands
            AssertExpr("<GETB>").InV3().DoesNotCompile();
            AssertExpr("<GETB 0>").InV3().DoesNotCompile();
            AssertExpr("<GETB 0 0 0>").InV3().DoesNotCompile();
        }

        [TestMethod]
        public void TestGETP()
        {
            // V1 to V6
            // 2 to 2 operands
            AssertExpr("<GETP ,MYOBJECT ,P?MYPROP>")
                .WithGlobal("<OBJECT MYOBJECT (MYPROP 123)>")
                .GivesNumber("123");
            AssertExpr("<GETP ,OBJECT2 ,P?MYPROP>")
                .WithGlobal("<OBJECT OBJECT1 (MYPROP 1)>")
                .WithGlobal("<OBJECT OBJECT2>")
                .GivesNumber("0");
        }

        [TestMethod]
        public void TestGETP_Error()
        {
            // V1 to V6
            // 2 to 2 operands
            AssertExpr("<GETP>").InV3().DoesNotCompile();
            AssertExpr("<GETP 0>").InV3().DoesNotCompile();
            AssertExpr("<GETP 0 0 0>").InV3().DoesNotCompile();
        }

        [TestMethod]
        public void TestGETPT()
        {
            // V1 to V6
            // 2 to 2 operands
            AssertExpr("<GET <GETPT ,MYOBJECT ,P?MYPROP> 0>")
                .WithGlobal("<OBJECT MYOBJECT (MYPROP 123)>")
                .GivesNumber("123");
            AssertExpr("<GETPT ,OBJECT2 ,P?MYPROP>")
                .WithGlobal("<OBJECT OBJECT1 (MYPROP 1)>")
                .WithGlobal("<OBJECT OBJECT2>")
                .GivesNumber("0");
        }

        [TestMethod]
        public void TestGETPT_Error()
        {
            // V1 to V6
            // 2 to 2 operands
            AssertExpr("<GETPT>").InV3().DoesNotCompile();
            AssertExpr("<GETPT 0>").InV3().DoesNotCompile();
            AssertExpr("<GETPT 0 0 0>").InV3().DoesNotCompile();
        }

        [TestMethod]
        public void TestGEq_P()
        {
            // V1 to V6
            // 2 to 2 operands
            AssertExpr("<G=? -1 3>").InV3().GivesNumber("0");
            AssertExpr("<G=? 3 -1>").InV3().GivesNumber("1");
            AssertExpr("<G=? 37 37>").InV3().GivesNumber("1");

            // alias
            AssertExpr("<G? 3 -1>").InV3().GivesNumber("1");
        }

        [TestMethod]
        public void TestGEq_P_Error()
        {
            // V1 to V6
            // 2 to 2 operands
            AssertExpr("<G=?>").InV3().DoesNotCompile();
            AssertExpr("<G=? 0>").InV3().DoesNotCompile();
            AssertExpr("<G=? 0 0 0>").InV3().DoesNotCompile();
        }

        [TestMethod]
        public void TestGRTR_P()
        {
            // V1 to V6
            // 2 to 2 operands
            AssertExpr("<GRTR? -1 3>").InV3().GivesNumber("0");
            AssertExpr("<GRTR? 3 -1>").InV3().GivesNumber("1");
            AssertExpr("<GRTR? 37 37>").InV3().GivesNumber("0");

            // alias
            AssertExpr("<G? 3 -1>").InV3().GivesNumber("1");
        }

        [TestMethod]
        public void TestGRTR_P_Error()
        {
            // V1 to V6
            // 2 to 2 operands
            AssertExpr("<GRTR?>").InV3().DoesNotCompile();
            AssertExpr("<GRTR? 0>").InV3().DoesNotCompile();
            AssertExpr("<GRTR? 0 0 0>").InV3().DoesNotCompile();
        }

        [TestMethod]
        public void TestHLIGHT()
        {
            // V4 to V6
            // 1 operand
            AssertExpr("<HLIGHT 4>").InV4().Compiles();
        }

        [TestMethod]
        public void TestHLIGHT_Error()
        {
            // only exists in V4+
            AssertExpr("<HLIGHT>").InV3().DoesNotCompile();

            // V4 to V6
            // 1 operand
            AssertExpr("<HLIGHT>").InV4().DoesNotCompile();
            AssertExpr("<HLIGHT 0 0>").InV4().DoesNotCompile();
        }

        // ICALL, ICALL1, and ICALL2 are not supported in ZIL

        [TestMethod]
        public void TestIGRTR_P()
        {
            // V1 to V6
            AssertRoutine("FOO", "<PRINTN <IGRTR? FOO 100>> <CRLF> <PRINTN .FOO>")
                .WhenCalledWith("100").Outputs("1\n101");
            AssertRoutine("FOO", "<PRINTN <IGRTR? FOO 100>> <CRLF> <PRINTN .FOO>")
                .WhenCalledWith("99").Outputs("0\n100");
        }

        [TestMethod]
        public void TestIGRTR_P_Error()
        {
            // V1 to V6
            AssertExpr("<IGRTR?>").DoesNotCompile();
            AssertRoutine("FOO", "<IGRTR? FOO>").DoesNotCompile();
            AssertExpr("<IGRTR? 11 22>").DoesNotCompile();
            AssertRoutine("FOO", "<IGRTR? BAR 100>").DoesNotCompile();
            AssertRoutine("FOO BAR", "<IGRTR? FOO BAR>").DoesNotCompile();
        }

        [TestMethod]
        public void TestIN_P()
        {
            // V1 to V6
            // 2 to 2 operands
            AssertExpr("<COND (<IN? ,CAT ,HAT> 123) (T 456)>")
                .WithGlobal("<OBJECT HAT>")
                .WithGlobal("<OBJECT CAT (LOC HAT)>")
                .GivesNumber("123");
            AssertExpr("<COND (<IN? ,CAT ,HAT> 123) (T 456)>")
                .WithGlobal("<OBJECT HAT (LOC CAT)>")
                .WithGlobal("<OBJECT CAT>")
                .GivesNumber("456");
        }

        [TestMethod]
        public void TestIN_P_Error()
        {
            // V1 to V6
            // 2 to 2 operands
            AssertExpr("<IN?>").InV3().DoesNotCompile();
            AssertExpr("<IN? 0>").InV3().DoesNotCompile();
            AssertExpr("<IN? 0 0 0>").InV3().DoesNotCompile();
        }

        [TestMethod]
        public void TestINC()
        {
            AssertRoutine("FOO", "<INC FOO> .FOO").WhenCalledWith("200").GivesNumber("201");
        }

        [TestMethod]
        public void TestINC_Error()
        {
            AssertExpr("<INC>").DoesNotCompile();
            AssertExpr("<INC 1>").DoesNotCompile();
            AssertRoutine("FOO", "<INC BAR>").DoesNotCompile();
        }

        [TestMethod]
        public void TestINPUT()
        {
            // V4 to V6
            // 1 to 3 operands
            AssertExpr("<INPUT 1>")
                .InV4()
                .WithInput("A")
                .GivesNumber("65");

            AssertExpr("<INPUT 1 0>").InV4().Compiles();
            AssertExpr("<INPUT 1 0 0>").InV4().Compiles();
        }

        [TestMethod]
        public void TestINPUT_Error()
        {
            // only exists in V4+
            AssertExpr("<INPUT 1>").InV3().DoesNotCompile();

            // V4 to V6
            // 0 to 4 operands
            AssertExpr("<INPUT>").InV4().DoesNotCompile();
            AssertExpr("<INPUT 0 0 0 0>").InV4().DoesNotCompile();

            // first argument must be a literal 1
            AssertExpr("<INPUT 0>").InV4().DoesNotCompile();
            AssertExpr("<INPUT <+ 1 0>>").InV4().DoesNotCompile();
        }

        [TestMethod]
        public void TestINTBL_P()
        {
            // V4 to V6
            // 3 to 4 operands
            AssertExpr("<COND (<INTBL? 3 ,MYTABLE 4> 123) (T 456)>")
                .InV4()
                .WithGlobal("<GLOBAL MYTABLE <TABLE 1 2 3 4>>")
                .GivesNumber("123");
            AssertExpr("<GET <INTBL? 3 ,MYTABLE 4> 0>")
                .InV4()
                .WithGlobal("<GLOBAL MYTABLE <TABLE 1 2 3 4>>")
                .GivesNumber("3");
            AssertExpr("<INTBL? 9 ,MYTABLE 4>")
                .InV4()
                .WithGlobal("<GLOBAL MYTABLE <TABLE 1 2 3 4>>")
                .GivesNumber("0");

            // 4th operand is allowed in V5
            AssertExpr("<GETB <INTBL? 10 ,MYTABLE 9 3> 0>")
                .InV5()
                .WithGlobal("<GLOBAL MYTABLE <TABLE (BYTE) 111 111 111 222 222 222 10 123 123>>")
                .GivesNumber("10");
        }

        [TestMethod]
        public void TestINTBL_P_Error()
        {
            // only exists in V4+
            AssertExpr("<INTBL? 0 0 0>").InV3().DoesNotCompile();

            // V4 to V6
            // 3 to 4 operands
            AssertExpr("<INTBL?>").InV4().DoesNotCompile();
            AssertExpr("<INTBL? 0>").InV4().DoesNotCompile();
            AssertExpr("<INTBL? 0 0>").InV4().DoesNotCompile();

            // 4th operand is only allowed in V5
            AssertExpr("<INTBL? 0 0 0 0>").InV4().DoesNotCompile();
            AssertExpr("<INTBL? 0 0 0 0 0>").InV5().DoesNotCompile();
        }

        [TestMethod]
        public void TestIRESTORE()
        {
            // V5 to V6
            // 0 operands
            AssertExpr("<IRESTORE>").InV5().Compiles();
        }

        [TestMethod]
        public void TestIRESTORE_Error()
        {
            // only exists in V5+
            AssertExpr("<IRESTORE>").InV4().DoesNotCompile();

            // V5 to V6
            // 0 operands
            AssertExpr("<IRESTORE 0>").InV5().DoesNotCompile();
        }

        [TestMethod]
        public void TestISAVE()
        {
            // V5 to V6
            // 0 operands
            AssertExpr("<ISAVE>").InV5().Compiles();
        }

        [TestMethod]
        public void TestISAVE_Error()
        {
            // only exists in V5+
            AssertExpr("<ISAVE>").InV4().DoesNotCompile();

            // V5 to V6
            // 0 operands
            AssertExpr("<ISAVE 0>").InV5().DoesNotCompile();
        }

        // IXCALL and JUMP are not supported in ZIL

        [TestMethod]
        public void TestLEq_P()
        {
            // V1 to V6
            // 2 to 2 operands
            AssertExpr("<L=? -1 3>").InV3().GivesNumber("1");
            AssertExpr("<L=? 3 -1>").InV3().GivesNumber("0");
            AssertExpr("<L=? 37 37>").InV3().GivesNumber("1");

            // alias
            AssertExpr("<L? 3 -1>").InV3().GivesNumber("0");
        }

        [TestMethod]
        public void TestLEq_P_Error()
        {
            // V1 to V6
            // 2 to 2 operands
            AssertExpr("<L=?>").InV3().DoesNotCompile();
            AssertExpr("<L=? 0>").InV3().DoesNotCompile();
            AssertExpr("<L=? 0 0 0>").InV3().DoesNotCompile();
        }

        [TestMethod]
        public void TestLESS_P()
        {
            // V1 to V6
            // 2 to 2 operands
            AssertExpr("<LESS? -1 3>").InV3().GivesNumber("1");
            AssertExpr("<LESS? 3 -1>").InV3().GivesNumber("0");
            AssertExpr("<LESS? 37 37>").InV3().GivesNumber("0");

            // alias
            AssertExpr("<L? 3 -1>").InV3().GivesNumber("0");
        }

        [TestMethod]
        public void TestLESS_P_Error()
        {
            // V1 to V6
            // 2 to 2 operands
            AssertExpr("<LESS?>").InV3().DoesNotCompile();
            AssertExpr("<LESS? 0>").InV3().DoesNotCompile();
            AssertExpr("<LESS? 0 0 0>").InV3().DoesNotCompile();
        }

        [TestMethod]
        public void TestLEX()
        {
            // V5 to V6
            // 2 to 4 operands

            AssertRoutine("", "<LEX ,TEXTBUF ,LEXBUF> <PRINTB <GET ,LEXBUF 1>>")
                .InV5()
                .WithGlobal("<GLOBAL TEXTBUF <TABLE (BYTE) 3 3 !\\c !\\a !\\t>>")
                .WithGlobal("<GLOBAL LEXBUF <ITABLE 1 (LEXV) 0 0>>")
                .WithGlobal("<OBJECT CAT (SYNONYM CAT)>")
                .Outputs("cat");

            AssertExpr("<LEX 0 0 0>").InV5().Compiles();
            AssertExpr("<LEX 0 0 0 0>").InV5().Compiles();
        }

        [TestMethod]
        public void TestLEX_Error()
        {
            // only exists in V5+
            AssertExpr("<LEX>").InV4().DoesNotCompile();

            // V5 to V6
            // 2 to 4 operands
            AssertExpr("<LEX>").InV5().DoesNotCompile();
            AssertExpr("<LEX 0>").InV5().DoesNotCompile();
            AssertExpr("<LEX 0 0 0 0 0>").InV5().DoesNotCompile();
        }

        [TestMethod]
        public void TestLOC()
        {
            // V1 to V6
            // 1 to 1 operands
            AssertExpr("<==? <LOC ,CAT> ,HAT>")
                .WithGlobal("<OBJECT CAT (LOC HAT)>")
                .WithGlobal("<OBJECT HAT>")
                .GivesNumber("1");
            AssertExpr("<LOC ,HAT>")
                .WithGlobal("<OBJECT CAT (LOC HAT)>")
                .WithGlobal("<OBJECT HAT>")
                .GivesNumber("0");
        }

        [TestMethod]
        public void TestLOC_Error()
        {
            // V1 to V6
            // 1 to 1 operands
            AssertExpr("<LOC>").InV3().DoesNotCompile();
            AssertExpr("<LOC 0 0>").InV3().DoesNotCompile();
        }

        [TestMethod]
        public void TestMARGIN_V6()
        {
            // only exists in V6+
            Assert.Inconclusive();
            
            // V6 to V6
            // 0 to 4 operands
            AssertExpr("<MARGIN>").InV6().Compiles();
            AssertExpr("<MARGIN 0>").InV6().Compiles();
            AssertExpr("<MARGIN 0 0>").InV6().Compiles();
            AssertExpr("<MARGIN 0 0 0>").InV6().Compiles();
            AssertExpr("<MARGIN 0 0 0 0>").InV6().Compiles();
            Assert.Inconclusive("This test was automatically generated.");
        }

        [TestMethod]
        public void TestMARGIN_Error_V6()
        {
            // only exists in V6+
            AssertExpr("<MARGIN>").InV5().DoesNotCompile();

            // V6 to V6
            // 0 to 4 operands
            AssertExpr("<MARGIN 0 0 0 0 0>").InV6().DoesNotCompile();
            Assert.Inconclusive("This test was automatically generated.");
        }

        [TestMethod]
        public void TestMENU_V6()
        {
            // only exists in V6+
            Assert.Inconclusive();
            
            // V6 to V6
            // 0 to 4 operands
            AssertExpr("<MENU>").InV6().Compiles();
            AssertExpr("<MENU 0>").InV6().Compiles();
            AssertExpr("<MENU 0 0>").InV6().Compiles();
            AssertExpr("<MENU 0 0 0>").InV6().Compiles();
            AssertExpr("<MENU 0 0 0 0>").InV6().Compiles();
            Assert.Inconclusive("This test was automatically generated.");
        }

        [TestMethod]
        public void TestMENU_Error_V6()
        {
            // only exists in V6+
            AssertExpr("<MENU>").InV5().DoesNotCompile();

            // V6 to V6
            // 0 to 4 operands
            AssertExpr("<MENU 0 0 0 0 0>").InV6().DoesNotCompile();
            Assert.Inconclusive("This test was automatically generated.");
        }

        [TestMethod]
        public void TestMOD()
        {
            // V1 to V6
            // 2 to 2 operands
            AssertExpr("<MOD 15 4>").InV3().GivesNumber("3");
            AssertExpr("<MOD -15 4>").InV3().GivesNumber("-3");
            AssertExpr("<MOD -15 4>").InV3().GivesNumber("-3");
            AssertExpr("<MOD 15 -4>").InV3().GivesNumber("3");
        }

        [TestMethod]
        public void TestMOD_Error()
        {
            // V1 to V6
            // 2 to 2 operands
            AssertExpr("<MOD>").InV3().DoesNotCompile();
            AssertExpr("<MOD 0>").InV3().DoesNotCompile();
            AssertExpr("<MOD 0 0 0>").InV3().DoesNotCompile();
        }

        [TestMethod]
        public void TestMOUSE_INFO_V6()
        {
            // only exists in V6+
            Assert.Inconclusive();
            
            // V6 to V6
            // 0 to 4 operands
            AssertExpr("<MOUSE-INFO>").InV6().Compiles();
            AssertExpr("<MOUSE-INFO 0>").InV6().Compiles();
            AssertExpr("<MOUSE-INFO 0 0>").InV6().Compiles();
            AssertExpr("<MOUSE-INFO 0 0 0>").InV6().Compiles();
            AssertExpr("<MOUSE-INFO 0 0 0 0>").InV6().Compiles();
            Assert.Inconclusive("This test was automatically generated.");
        }

        [TestMethod]
        public void TestMOUSE_INFO_Error_V6()
        {
            // only exists in V6+
            AssertExpr("<MOUSE-INFO>").InV5().DoesNotCompile();

            // V6 to V6
            // 0 to 4 operands
            AssertExpr("<MOUSE-INFO 0 0 0 0 0>").InV6().DoesNotCompile();
            Assert.Inconclusive("This test was automatically generated.");
        }

        [TestMethod]
        public void TestMOUSE_LIMIT_V6()
        {
            // only exists in V6+
            Assert.Inconclusive();
            
            // V6 to V6
            // 0 to 4 operands
            AssertExpr("<MOUSE-LIMIT>").InV6().Compiles();
            AssertExpr("<MOUSE-LIMIT 0>").InV6().Compiles();
            AssertExpr("<MOUSE-LIMIT 0 0>").InV6().Compiles();
            AssertExpr("<MOUSE-LIMIT 0 0 0>").InV6().Compiles();
            AssertExpr("<MOUSE-LIMIT 0 0 0 0>").InV6().Compiles();
            Assert.Inconclusive("This test was automatically generated.");
        }

        [TestMethod]
        public void TestMOUSE_LIMIT_Error_V6()
        {
            // only exists in V6+
            AssertExpr("<MOUSE-LIMIT>").InV5().DoesNotCompile();

            // V6 to V6
            // 0 to 4 operands
            AssertExpr("<MOUSE-LIMIT 0 0 0 0 0>").InV6().DoesNotCompile();
            Assert.Inconclusive("This test was automatically generated.");
        }

        [TestMethod]
        public void TestMOVE()
        {
            // V1 to V6
            // 2 to 2 operands
            AssertRoutine("", "<MOVE ,CAT ,HAT> <IN? ,CAT ,HAT>")
                .WithGlobal("<OBJECT CAT>")
                .WithGlobal("<OBJECT HAT>")
                .GivesNumber("1");
        }

        [TestMethod]
        public void TestMOVE_Error()
        {
            // V1 to V6
            // 2 to 2 operands
            AssertExpr("<MOVE>").InV3().DoesNotCompile();
            AssertExpr("<MOVE 0>").InV3().DoesNotCompile();
            AssertExpr("<MOVE 0 0 0>").InV3().DoesNotCompile();
        }

        [TestMethod]
        public void TestMUL()
        {
            // V1 to V6
            // 2 to 2 operands
            AssertExpr("<MUL 150 0>").InV3().GivesNumber("0");
            AssertExpr("<MUL 0 -6>").InV3().GivesNumber("0");
            AssertExpr("<MUL 150 3>").InV3().GivesNumber("450");
            AssertExpr("<MUL 150 -3>").InV3().GivesNumber("-450");
            AssertExpr("<MUL -15 4>").InV3().GivesNumber("-60");
            AssertExpr("<MUL -1 128>").InV3().GivesNumber("-128");
            AssertExpr("<MUL>").GivesNumber("1");
            AssertExpr("<MUL 5>").GivesNumber("5");
            AssertExpr("<MUL 1 2 3>").GivesNumber("6");
            AssertExpr("<MUL 1 2 3 4>").GivesNumber("24");
            AssertExpr("<MUL 1 2 3 4 -5>").GivesNumber("-120");
        }

        [TestMethod]
        public void TestNEXT_P()
        {
            // V1 to V6
            // 1 to 1 operands
            AssertRoutine("", "<MOVE ,RAT ,HAT> <==? <NEXT? ,RAT> ,CAT>")
                .WithGlobal("<OBJECT CAT (LOC HAT)>")
                .WithGlobal("<OBJECT HAT>")
                .WithGlobal("<OBJECT RAT>")
                .GivesNumber("1");
            AssertExpr("<NEXT? ,CAT>")
                .WithGlobal("<OBJECT CAT (LOC HAT)>")
                .WithGlobal("<OBJECT HAT>")
                .GivesNumber("0");
        }

        [TestMethod]
        public void TestNEXT_P_Error()
        {
            // V1 to V6
            // 1 to 1 operands
            AssertExpr("<NEXT?>").InV3().DoesNotCompile();
            AssertExpr("<NEXT? 0 0>").InV3().DoesNotCompile();
        }

        [TestMethod]
        public void TestNEXTP()
        {
            // V1 to V6
            // 2 to 2 operands
            AssertExpr("<==? <NEXTP ,MYOBJECT 0> ,P?FOO>")
                .WithGlobal("<OBJECT MYOBJECT (FOO 123) (BAR 456)>")
                .GivesNumber("1");
            AssertExpr("<==? <NEXTP ,MYOBJECT ,P?FOO> ,P?BAR>")
                .WithGlobal("<OBJECT MYOBJECT (FOO 123) (BAR 456)>")
                .GivesNumber("1");
            AssertExpr("<==? <NEXTP ,MYOBJECT ,P?BAR> 0>")
                .WithGlobal("<OBJECT MYOBJECT (FOO 123) (BAR 456)>")
                .GivesNumber("1");
        }

        [TestMethod]
        public void TestNEXTP_Error()
        {
            // V1 to V6
            // 2 to 2 operands
            AssertExpr("<NEXTP>").InV3().DoesNotCompile();
            AssertExpr("<NEXTP 0>").InV3().DoesNotCompile();
            AssertExpr("<NEXTP 0 0 0>").InV3().DoesNotCompile();
        }

        // NOOP is not supported in ZIL

        [TestMethod]
        public void TestNOT()
        {
            AssertExpr("<NOT 0>").GivesNumber("1");
            AssertExpr("<NOT 123>").GivesNumber("0");

            AssertExpr("<NOT ,FOO>")
                .WithGlobal("<GLOBAL FOO 0>")
                .GivesNumber("1");
            AssertExpr("<NOT ,FOO>")
                .WithGlobal("<GLOBAL FOO 123>")
                .GivesNumber("0");

            AssertRoutine("", "<COND (<NOT 0> <PRINTI \"hello\">) (T <PRINTI \"goodbye\">)>")
                .Outputs("hello");
            AssertRoutine("", "<COND (<NOT 123> <PRINTI \"hello\">) (T <PRINTI \"goodbye\">)>")
                .Outputs("goodbye");

            AssertRoutine("", "<COND (<NOT ,FOO> <PRINTI \"hello\">) (T <PRINTI \"goodbye\">)>")
                .WithGlobal("<GLOBAL FOO 0>")
                .Outputs("hello");
            AssertRoutine("", "<COND (<NOT ,FOO> <PRINTI \"hello\">) (T <PRINTI \"goodbye\">)>")
                .WithGlobal("<GLOBAL FOO 123>")
                .Outputs("goodbye");
        }

        [TestMethod]
        public void TestNOT_Error()
        {
            AssertExpr("<NOT>").DoesNotCompile();
            AssertExpr("<NOT 0 0>").DoesNotCompile();
        }

        [TestMethod]
        public void TestORIGINAL_P()
        {
            // V5 to V6
            // 0 to 0 operands
            AssertExpr("<ORIGINAL?>").InV5().GivesNumber("1");
        }

        [TestMethod]
        public void TestORIGINAL_P_Error()
        {
            // only exists in V5+
            AssertExpr("<ORIGINAL?>").InV4().DoesNotCompile();

            // V5 to V6
            // 0 to 0 operands
            AssertExpr("<ORIGINAL? 0>").InV5().DoesNotCompile();
        }

        [TestMethod]
        public void TestPICINF_V6()
        {
            // only exists in V6+
            Assert.Inconclusive();
            
            // V6 to V6
            // 0 to 4 operands
            AssertExpr("<PICINF>").InV6().Compiles();
            AssertExpr("<PICINF 0>").InV6().Compiles();
            AssertExpr("<PICINF 0 0>").InV6().Compiles();
            AssertExpr("<PICINF 0 0 0>").InV6().Compiles();
            AssertExpr("<PICINF 0 0 0 0>").InV6().Compiles();
            Assert.Inconclusive("This test was automatically generated.");
        }

        [TestMethod]
        public void TestPICINF_Error_V6()
        {
            // only exists in V6+
            AssertExpr("<PICINF>").InV5().DoesNotCompile();

            // V6 to V6
            // 0 to 4 operands
            AssertExpr("<PICINF 0 0 0 0 0>").InV6().DoesNotCompile();
            Assert.Inconclusive("This test was automatically generated.");
        }

        [TestMethod]
        public void TestPICSET_V6()
        {
            // only exists in V6+
            Assert.Inconclusive();
            
            // V6 to V6
            // 0 to 4 operands
            AssertExpr("<PICSET>").InV6().Compiles();
            AssertExpr("<PICSET 0>").InV6().Compiles();
            AssertExpr("<PICSET 0 0>").InV6().Compiles();
            AssertExpr("<PICSET 0 0 0>").InV6().Compiles();
            AssertExpr("<PICSET 0 0 0 0>").InV6().Compiles();
            Assert.Inconclusive("This test was automatically generated.");
        }

        [TestMethod]
        public void TestPICSET_Error_V6()
        {
            // only exists in V6+
            AssertExpr("<PICSET>").InV5().DoesNotCompile();

            // V6 to V6
            // 0 to 4 operands
            AssertExpr("<PICSET 0 0 0 0 0>").InV6().DoesNotCompile();
            Assert.Inconclusive("This test was automatically generated.");
        }

        // only the V6 version of POP is supported in ZIL

        [TestMethod]
        public void TestPOP_V6()
        {
            // only exists in V6+
            Assert.Inconclusive();
            
            // V6 to V6
            // 0 to 1 operands
            AssertExpr("<POP>").InV6().Compiles();
            AssertExpr("<POP 0>").InV6().Compiles();
        }

        [TestMethod]
        public void TestPOP_Error()
        {
            // V6 to V6
            // 0 to 1 operands
            AssertExpr("<POP 0 0>").InV6().DoesNotCompile();
        }

        [TestMethod]
        public void TestPRINT()
        {
            // V1 to V6
            // 1 to 1 operands
            AssertExpr("<PRINT ,MESSAGE>")
                .InV3()
                .WithGlobal("<GLOBAL MESSAGE \"hello\">")
                .Outputs("hello");
        }

        [TestMethod]
        public void TestPRINT_Error()
        {
            // V1 to V6
            // 1 to 1 operands
            AssertExpr("<PRINT>").InV3().DoesNotCompile();
            AssertExpr("<PRINT 0 0>").InV3().DoesNotCompile();
        }

        [TestMethod]
        public void TestPRINTB()
        {
            // V1 to V6
            // 1 to 1 operands
            AssertExpr("<PRINTB <GETP ,MYOBJECT ,P?SYNONYM>>")
                .InV3()
                .WithGlobal("<OBJECT MYOBJECT (SYNONYM HELLO)>")
                .Outputs("hello");
        }

        [TestMethod]
        public void TestPRINTB_Error()
        {
            // V1 to V6
            // 1 to 1 operands
            AssertExpr("<PRINTB>").InV3().DoesNotCompile();
            AssertExpr("<PRINTB 0 0>").InV3().DoesNotCompile();
        }

        [TestMethod]
        public void TestPRINTC()
        {
            // V1 to V6
            // 1 operand
            AssertExpr("<PRINTC 65>").InV3().Outputs("A");
        }

        [TestMethod]
        public void TestPRINTC_Error()
        {
            // V1 to V6
            // 1 operand
            AssertExpr("<PRINTC>").InV3().DoesNotCompile();
            AssertExpr("<PRINTC 65 0>").InV3().DoesNotCompile();
        }

        [TestMethod]
        public void TestPRINTD()
        {
            // V1 to V6
            // 1 to 1 operands
            AssertExpr("<PRINTD ,MYOBJECT>")
                .WithGlobal("<OBJECT MYOBJECT (DESC \"pocket fisherman\")>")
                .Outputs("pocket fisherman");
        }

        [TestMethod]
        public void TestPRINTD_Error()
        {
            // V1 to V6
            // 1 to 1 operands
            AssertExpr("<PRINTD>").InV3().DoesNotCompile();
            AssertExpr("<PRINTD 0 0>").InV3().DoesNotCompile();
        }

        [TestMethod]
        public void TestPRINTF_V6()
        {
            // only exists in V6+
            Assert.Inconclusive();
            
            // V6 to V6
            // 0 to 4 operands
            AssertExpr("<PRINTF>").InV6().Compiles();
            AssertExpr("<PRINTF 0>").InV6().Compiles();
            AssertExpr("<PRINTF 0 0>").InV6().Compiles();
            AssertExpr("<PRINTF 0 0 0>").InV6().Compiles();
            AssertExpr("<PRINTF 0 0 0 0>").InV6().Compiles();
            Assert.Inconclusive("This test was automatically generated.");
        }

        [TestMethod]
        public void TestPRINTF_Error_V6()
        {
            // only exists in V6+
            AssertExpr("<PRINTF>").InV5().DoesNotCompile();

            // V6 to V6
            // 0 to 4 operands
            AssertExpr("<PRINTF 0 0 0 0 0>").InV6().DoesNotCompile();
            Assert.Inconclusive("This test was automatically generated.");
        }

        [TestMethod]
        public void TestPRINTI()
        {
            // V1 to V6
            AssertExpr("<PRINTI \"hello|world\">").Outputs("hello\nworld");
            AssertExpr("<PRINTI \"foo||\r\n\r\n    BAR\">").Outputs("foo\n\n     BAR");
        }

        [TestMethod]
        public void TestPRINTI_Error()
        {
            // V1 to V6
            AssertExpr("<PRINTI>").DoesNotCompile();
            AssertExpr("<PRINTI \"foo\" \"bar\">").DoesNotCompile();
            AssertExpr("<PRINTI 123>").DoesNotCompile();
        }

        [TestMethod]
        public void TestPRINTN()
        {
            // V1 to V6
            // 1 to 1 operands
            AssertExpr("<PRINTN 0>").InV3().Outputs("0");
            AssertExpr("<PRINTN -12345>").InV3().Outputs("-12345");
        }

        [TestMethod]
        public void TestPRINTN_Error()
        {
            // V1 to V6
            // 1 to 1 operands
            AssertExpr("<PRINTN>").InV3().DoesNotCompile();
            AssertExpr("<PRINTN 0 0>").InV3().DoesNotCompile();
        }

        [TestMethod]
        public void TestPRINTR()
        {
            // V1 to V6
            AssertRoutine("", "<PRINTR \"hello|world\">").Outputs("hello\nworld\n");
        }

        [TestMethod]
        public void TestPRINTR_Error()
        {
            // V1 to V6
            AssertExpr("<PRINTR>").DoesNotCompile();
            AssertExpr("<PRINTR \"foo\" \"bar\">").DoesNotCompile();
            AssertExpr("<PRINTR 123>").DoesNotCompile();
        }

        [TestMethod]
        public void TestPRINTT()
        {
            // V5 to V6
            // 2 to 4 operands
            AssertExpr("<PRINTT ,MYTEXT 6>")
                .InV5()
                .WithGlobal("<GLOBAL MYTEXT <TABLE (STRING) \"hansprestige\">>")
                .Outputs("hanspr\r\n");

            AssertExpr("<PRINTT ,MYTEXT 4 3>")
                .InV5()
                .WithGlobal("<GLOBAL MYTEXT <TABLE (STRING) \"hansprestige\">>")
                .Outputs("hans\r\npres\r\ntige\r\n");

            AssertExpr("<PRINTT ,MYTEXT 3 3 1>")
                .InV5()
                .WithGlobal("<GLOBAL MYTEXT <TABLE (STRING) \"hansprestige\">>")
                .Outputs("han\r\npre\r\ntig\r\n");
        }

        [TestMethod]
        public void TestPRINTT_Error()
        {
            // only exists in V5+
            AssertExpr("<PRINTT>").InV4().DoesNotCompile();

            // V5 to V6
            // 2 to 4 operands
            AssertExpr("<PRINTT>").InV5().DoesNotCompile();
            AssertExpr("<PRINTT 0>").InV5().DoesNotCompile();
            AssertExpr("<PRINTT 0 0 0 0 0>").InV5().DoesNotCompile();
        }

        [TestMethod]
        public void TestPRINTU()
        {
            // V5 to V6
            // 1 operand
            AssertExpr("<PRINTU 65>").InV5().Compiles();
        }

        [TestMethod]
        public void TestPRINTU_Error()
        {
            // only exists in V5+
            AssertExpr("<PRINTU>").InV4().DoesNotCompile();

            // V5 to V6
            // 1 operand
            AssertExpr("<PRINTU>").InV5().DoesNotCompile();
            AssertExpr("<PRINTU 0 0>").InV5().DoesNotCompile();
        }

        [TestMethod]
        public void TestPTSIZE()
        {
            // V1 to V6
            // 1 to 1 operands
            AssertExpr("<PTSIZE <GETPT ,MYOBJECT ,P?FOO>>")
                .WithGlobal("<OBJECT MYOBJECT (FOO 1 2 3)>")
                .GivesNumber("6");
        }

        [TestMethod]
        public void TestPTSIZE_Error()
        {
            // V1 to V6
            // 1 to 1 operands
            AssertExpr("<PTSIZE>").InV3().DoesNotCompile();
            AssertExpr("<PTSIZE 0 0>").InV3().DoesNotCompile();
        }

        [TestMethod]
        public void TestPUSH()
        {
            // V1 to V6
            // 1 operand
            AssertExpr("<PUSH 1234>").InV3().Compiles();
        }

        [TestMethod]
        public void TestPUSH_Error()
        {
            // V1 to V6
            // 1 operand
            AssertExpr("<PUSH>").InV3().DoesNotCompile();
            AssertExpr("<PUSH 0 0>").InV3().DoesNotCompile();
        }

        [TestMethod]
        public void TestPUT()
        {
            // V1 to V6
            // 3 to 3 operands
            AssertExpr("<PUT 0 0 0>").InV3().Compiles();
        }

        [TestMethod]
        public void TestPUT_Error()
        {
            // V1 to V6
            // 3 to 3 operands
            AssertExpr("<PUT 0 0>").InV3().DoesNotCompile();
            AssertExpr("<PUT 0 0 0 0>").InV3().DoesNotCompile();
        }

        [TestMethod]
        public void TestPUTB()
        {
            // V1 to V6
            // 3 to 3 operands
            AssertExpr("<PUTB 0 0 0>").InV3().Compiles();
        }

        [TestMethod]
        public void TestPUTB_Error()
        {
            // V1 to V6
            // 3 to 3 operands
            AssertExpr("<PUTB 0 0>").InV3().DoesNotCompile();
            AssertExpr("<PUTB 0 0 0 0>").InV3().DoesNotCompile();
        }

        [TestMethod]
        public void TestPUTP()
        {
            // V1 to V6
            // 3 to 3 operands
            AssertExpr("<PUTP 0 0 0>").InV3().Compiles();
        }

        [TestMethod]
        public void TestPUTP_Error()
        {
            // V1 to V6
            // 3 to 3 operands
            AssertExpr("<PUTP 0 0>").InV3().DoesNotCompile();
            AssertExpr("<PUTP 0 0 0 0>").InV3().DoesNotCompile();
        }

        [TestMethod]
        public void TestQUIT()
        {
            // V1 to V6
            // 0 to 0 operands
            AssertExpr("<QUIT> <PRINTI \"foo\"> <CRLF>").InV3().Outputs("");
        }

        [TestMethod]
        public void TestQUIT_Error()
        {
            // V1 to V6
            // 0 to 0 operands
            AssertExpr("<QUIT 0>").InV3().DoesNotCompile();
        }

        [TestMethod]
        public void TestRANDOM()
        {
            // V1 to V6
            // 1 to 1 operands
            AssertExpr("<RANDOM 14>").InV3().Compiles();
        }

        [TestMethod]
        public void TestRANDOM_Error()
        {
            // V1 to V6
            // 1 to 1 operands
            AssertExpr("<RANDOM>").InV3().DoesNotCompile();
            AssertExpr("<RANDOM 0 0>").InV3().DoesNotCompile();
        }

        [TestMethod]
        public void TestREAD()
        {
            // V1 to V3
            // 2 operands
            // ,HERE must point to a valid object for status line purposes
            AssertRoutine("", "<READ ,TEXTBUF ,LEXBUF> <PRINTC <GETB ,TEXTBUF 2>> <PRINTB <GET ,LEXBUF 1>>")
                .InV3()
                .WithGlobal("<GLOBAL TEXTBUF <ITABLE 50 (BYTE LENGTH) 0>>")
                .WithGlobal("<GLOBAL LEXBUF <ITABLE 1 (LEXV) 0 0>>")
                .WithGlobal("<OBJECT CAT (SYNONYM CAT)>")
                .WithGlobal("<GLOBAL HERE CAT>")
                .WithInput("cat")
                .Outputs("acat");
            // V4
            // 2 to 4 operands
            AssertExpr("<READ 0 0>").InV4().Compiles();
            AssertExpr("<READ 0 0 0>").InV4().Compiles();
            AssertExpr("<READ 0 0 0 0>").InV4().Compiles();
            // V5 to V6
            // 1 to 4 operands
            AssertRoutine("", "<PRINTN <READ ,TEXTBUF ,LEXBUF>> <PRINTC <GETB ,TEXTBUF 2>> <PRINTB <GET ,LEXBUF 1>>")
                .InV5()
                .WithGlobal("<GLOBAL TEXTBUF <ITABLE 50 (BYTE LENGTH) 0>>")
                .WithGlobal("<GLOBAL LEXBUF <ITABLE 1 (LEXV) 0 0>>")
                .WithGlobal("<OBJECT CAT (SYNONYM CAT)>")
                .WithInput("cat")
                .Outputs("13ccat");
            AssertExpr("<READ 0>").InV5().Compiles();
            AssertExpr("<READ 0 0 0>").InV5().Compiles();
            AssertExpr("<READ 0 0 0 0>").InV5().Compiles();
        }

        [TestMethod]
        public void TestREAD_Error()
        {
            // V1 to V3
            // 2 operands
            AssertExpr("<READ>").InV3().DoesNotCompile();
            AssertExpr("<READ 0>").InV3().DoesNotCompile();
            AssertExpr("<READ 0 0 0>").InV3().DoesNotCompile();
            // V4
            // 2 to 4 operands
            AssertExpr("<READ>").InV4().DoesNotCompile();
            AssertExpr("<READ 0>").InV4().DoesNotCompile();
            AssertExpr("<READ 0 0 0 0 0>").InV4().DoesNotCompile();
            // V5 to V6
            // 1 to 4 operands
            AssertExpr("<READ>").InV5().DoesNotCompile();
            AssertExpr("<READ 0 0 0 0 0>").InV5().DoesNotCompile();
        }

        [TestMethod]
        public void TestREMOVE()
        {
            // V1 to V6
            // 1 to 1 operands
            AssertRoutine("", "<REMOVE ,CAT> <LOC ,CAT>")
                .WithGlobal("<OBJECT CAT (LOC HAT)>")
                .WithGlobal("<OBJECT HAT>")
                .GivesNumber("0");
        }

        [TestMethod]
        public void TestREMOVE_Error()
        {
            // V1 to V6
            // 1 to 1 operands
            AssertExpr("<REMOVE>").InV3().DoesNotCompile();
            AssertExpr("<REMOVE 0 0>").InV3().DoesNotCompile();
        }

        [TestMethod]
        public void TestRESTART()
        {
            // V1 to V6
            // 0 to 0 operands
            AssertExpr("<RESTART>").InV3().Compiles();
        }

        [TestMethod]
        public void TestRESTART_Error()
        {
            // V1 to V6
            // 0 to 0 operands
            AssertExpr("<RESTART 0>").InV3().DoesNotCompile();
        }

        [TestMethod]
        public void TestRESTORE()
        {
            // V1 to V3
            // 0 to 0 operands
            AssertExpr("<RESTORE>").InV3().Compiles();
            // V4 to V4
            // 0 to 0 operands
            AssertExpr("<RESTORE>").InV4().Compiles();
            // V5 to V6
            // 0 or(!) 3 operands
            AssertExpr("<RESTORE>").InV5().Compiles();
            AssertExpr("<RESTORE 0 0 0>").InV5().Compiles();
        }

        [TestMethod]
        public void TestRESTORE_Error()
        {
            // V1 to V3
            // 0 to 0 operands
            AssertExpr("<RESTORE 0>").InV3().DoesNotCompile();
            // V4 to V4
            // 0 to 0 operands
            AssertExpr("<RESTORE 0>").InV4().DoesNotCompile();
            // V5 to V6
            // 0 or(!) 3 operands
            AssertExpr("<RESTORE 0>").InV5().DoesNotCompile();
            AssertExpr("<RESTORE 0 0>").InV5().DoesNotCompile();
            AssertExpr("<RESTORE 0 0 0 0>").InV5().DoesNotCompile();
        }

        [TestMethod]
        public void TestRETURN()
        {
            // NOTE: <RETURN> is more than just the Z-machine opcode. it also returns from <REPEAT>, and with no argument it returns true.

            // V1 to V6
            // 0 to 1 operands
            AssertRoutine("", "<RETURN>").InV3().GivesNumber("1");
            AssertRoutine("", "<RETURN 41>").InV3().GivesNumber("41");
        }

        [TestMethod]
        public void TestRETURN_FromBlock()
        {
            AssertRoutine("", "<* 2 <PROG () <RETURN 41>>>").GivesNumber("82");
            AssertRoutine("", "<PROG () <RETURN>> 42").GivesNumber("42");
        }

        [TestMethod]
        public void TestRETURN_Error()
        {
            // V1 to V6
            // 0 to 1 operands
            AssertExpr("<RETURN 0 0>").InV3().DoesNotCompile();
        }

        [TestMethod]
        public void TestRFALSE()
        {
            // V1 to V6
            // 0 to 0 operands
            AssertRoutine("", "<RFALSE>").GivesNumber("0");
        }

        [TestMethod]
        public void TestRFALSE_Error()
        {
            // V1 to V6
            // 0 to 0 operands
            AssertExpr("<RFALSE 0>").InV3().DoesNotCompile();
        }

        [TestMethod]
        public void TestRSTACK()
        {
            // V1 to V6
            // 0 to 0 operands
            AssertRoutine("", "<PUSH 1234> <RSTACK>").GivesNumber("1234");
        }

        [TestMethod]
        public void TestRSTACK_Error()
        {
            // V1 to V6
            // 0 to 0 operands
            AssertExpr("<RSTACK 0>").InV3().DoesNotCompile();
        }

        [TestMethod]
        public void TestRTRUE()
        {
            // V1 to V6
            // 0 to 0 operands
            AssertRoutine("", "<RTRUE>").GivesNumber("1");
        }

        [TestMethod]
        public void TestRTRUE_Error()
        {
            // V1 to V6
            // 0 to 0 operands
            AssertExpr("<RTRUE 0>").InV3().DoesNotCompile();
        }

        [TestMethod]
        public void TestSAVE()
        {
            // V1 to V3
            // 0 to 0 operands
            AssertExpr("<SAVE>").InV3().Compiles();
            // V4 to V4
            // 0 to 0 operands
            AssertExpr("<SAVE>").InV4().Compiles();
            // V5 to V6
            // 0 or(!) 3 operands
            AssertExpr("<SAVE>").InV5().Compiles();
            AssertExpr("<SAVE 0 0 0>").InV5().Compiles();
        }

        [TestMethod]
        public void TestSAVE_Error()
        {
            // V1 to V3
            // 0 to 0 operands
            AssertExpr("<SAVE 0>").InV3().DoesNotCompile();
            // V4 to V4
            // 0 to 0 operands
            AssertExpr("<SAVE 0>").InV4().DoesNotCompile();
            // V5 to V6
            // 0 or(!) 3 operands
            AssertExpr("<SAVE 0>").InV5().DoesNotCompile();
            AssertExpr("<SAVE 0 0>").InV5().DoesNotCompile();
            AssertExpr("<SAVE 0 0 0 0>").InV5().DoesNotCompile();
        }

        [TestMethod]
        public void TestSCREEN()
        {
            // V3 to V6
            // 1 operand
            AssertExpr("<SCREEN 0>").InV3().Compiles();
        }

        [TestMethod]
        public void TestSCREEN_Error()
        {
            // V3 to V6
            // 1 operand
            AssertExpr("<SCREEN>").InV3().DoesNotCompile();
            AssertExpr("<SCREEN 0 0>").InV3().DoesNotCompile();
        }

        [TestMethod]
        public void TestSCROLL_V6()
        {
            // only exists in V6+
            Assert.Inconclusive();
            
            // V6 to V6
            // 0 to 4 operands
            AssertExpr("<SCROLL>").InV6().Compiles();
            AssertExpr("<SCROLL 0>").InV6().Compiles();
            AssertExpr("<SCROLL 0 0>").InV6().Compiles();
            AssertExpr("<SCROLL 0 0 0>").InV6().Compiles();
            AssertExpr("<SCROLL 0 0 0 0>").InV6().Compiles();
            Assert.Inconclusive("This test was automatically generated.");
        }

        [TestMethod]
        public void TestSCROLL_Error_V6()
        {
            // only exists in V6+
            AssertExpr("<SCROLL>").InV5().DoesNotCompile();

            // V6 to V6
            // 0 to 4 operands
            AssertExpr("<SCROLL 0 0 0 0 0>").InV6().DoesNotCompile();
            Assert.Inconclusive("This test was automatically generated.");
        }

        [TestMethod]
        public void TestSET()
        {
            // V1 to V6
            AssertRoutine("\"AUX\" FOO", "<SET FOO 111> .FOO")
                .GivesNumber("111");
            AssertRoutine("", "<SET FOO 111> ,FOO")
                .WithGlobal("<GLOBAL FOO 0>")
                .GivesNumber("111");

            // value version
            AssertRoutine("\"AUX\" FOO", "<PRINTN <SET FOO 111>>")
                .Outputs("111");

            // void version
            AssertRoutine("\"AUX\" FOO", "<SET 1 111> <PRINTN .FOO>")
                .Outputs("111");
            AssertRoutine("\"AUX\" BAR", "<SET <ONE> <ONE-ELEVEN>> <PRINTN .BAR>")
                .WithGlobal("<ROUTINE ONE () <PRINTI \"ONE.\"> 1>")
                .WithGlobal("<ROUTINE ONE-ELEVEN () <PRINTI \"ONE-ELEVEN.\"> 111>")
                .Outputs("ONE.ONE-ELEVEN.111");

            // alias: SETG
            AssertRoutine("\"AUX\" FOO", "<SETG FOO 111> .FOO")
                .GivesNumber("111");
            AssertRoutine("", "<SETG FOO 111> ,FOO")
                .WithGlobal("<GLOBAL FOO 0>")
                .GivesNumber("111");
        }

        [TestMethod]
        public void TestSET_Quirks()
        {
            /* SET and SETG have different QuirksMode behavior:
             * 
             * SETG treats a ,GVAL as its first argument as a variable name,
             * but treats an .LVAL as an expression: <SETG ,FOO 1> sets the global FOO,
             * whereas <SETG .FOO 1> sets the variable whose index is in .FOO.
             * 
             * Likewise, SET treats an .LVAL as a variable name but a ,GVAL as an
             * expression: <SET .FOO 1> sets the local FOO, and <SET ,FOO 1> sets the
             * variable whose index is in FOO. */

            AssertRoutine("\"AUX\" (FOO 16)", "<SET .FOO 123> <PRINTN .FOO> <CRLF> <PRINTN ,MYGLOBAL>")
                .WithGlobal("<GLOBAL MYGLOBAL 1>")
                .Outputs("123\n1");

            AssertRoutine("\"AUX\" (FOO 16)", "<SETG ,MYGLOBAL 123> <PRINTN .FOO> <CRLF> <PRINTN ,MYGLOBAL>")
                .WithGlobal("<GLOBAL MYGLOBAL 1>")
                .Outputs("16\n123");

            AssertRoutine("\"AUX\" (FOO 16)", "<SETG .FOO 123> <PRINTN .FOO> <CRLF> <PRINTN ,MYGLOBAL>")
                .WithGlobal("<GLOBAL MYGLOBAL 1>")
                .Outputs("16\n123");

            AssertRoutine("\"AUX\" (FOO 16)", "<SET ,MYGLOBAL 123> <PRINTN .FOO> <CRLF> <PRINTN ,MYGLOBAL>")
                .WithGlobal("<GLOBAL MYGLOBAL 1>")
                .Outputs("123\n1");
        }

        [TestMethod]
        public void TestSET_Error()
        {
            // V1 to V6
            AssertExpr("<SET>").DoesNotCompile();
            AssertRoutine("X", "<SET X>").DoesNotCompile();
            AssertExpr("<SET 1 2>").DoesNotCompile();
            AssertRoutine("X", "<SET Y 1>").DoesNotCompile();
        }

        [TestMethod]
        public void TestSHIFT()
        {
            // V5 to V6
            // 2 to 2 operands
            AssertExpr("<SHIFT 1 3>").InV5().GivesNumber("8");
            AssertExpr("<SHIFT 16 -3>").InV5().GivesNumber("2");
            AssertExpr("<SHIFT 1 16>").InV5().GivesNumber("0");
            AssertExpr("<SHIFT 1 15>").InV5().GivesNumber("-32768");
            AssertExpr("<SHIFT 16384 -14>").InV5().GivesNumber("1");
        }

        [TestMethod]
        public void TestSHIFT_Error()
        {
            // only exists in V5+
            AssertExpr("<SHIFT>").InV4().DoesNotCompile();

            // V5 to V6
            // 2 to 2 operands
            AssertExpr("<SHIFT 0>").InV5().DoesNotCompile();
            AssertExpr("<SHIFT 0 0 0>").InV5().DoesNotCompile();
        }

        [TestMethod]
        public void TestSOUND()
        {
            // V3 to V4
            // 1 to 3 operands
            AssertExpr("<SOUND 0>").InV3().Compiles();
            AssertExpr("<SOUND 0 0>").InV3().Compiles();
            AssertExpr("<SOUND 0 0 0>").InV3().Compiles();
            // V5 to V6
            // 1 to 4 operands
            AssertExpr("<SOUND 0>").InV5().Compiles();
            AssertExpr("<SOUND 0 0>").InV5().Compiles();
            AssertExpr("<SOUND 0 0 0>").InV5().Compiles();
            AssertExpr("<SOUND 0 0 0 0>").InV5().Compiles();
        }

        [TestMethod]
        public void TestSOUND_Error()
        {
            // V3 to V4
            // 1 to 3 operands
            AssertExpr("<SOUND>").InV3().DoesNotCompile();
            AssertExpr("<SOUND 0 0 0 0>").InV3().DoesNotCompile();
            // V5 to V6
            // 1 to 4 operands
            AssertExpr("<SOUND>").InV5().DoesNotCompile();
            AssertExpr("<SOUND 0 0 0 0 0>").InV5().DoesNotCompile();
        }

        [TestMethod]
        public void TestSPLIT()
        {
            // V3 to V6
            // 1 to 1 operands
            AssertExpr("<SPLIT 1>").InV3().Compiles();
        }

        [TestMethod]
        public void TestSPLIT_Error()
        {
            // V3 to V6
            // 1 to 1 operands
            AssertExpr("<SPLIT>").InV3().DoesNotCompile();
            AssertExpr("<SPLIT 0 0>").InV3().DoesNotCompile();
        }

        [TestMethod]
        public void TestSUB()
        {
            AssertExpr("<- 1 2>").GivesNumber("-1");
            AssertExpr("<- 1 -2>").GivesNumber("3");
            AssertExpr("<- -32768 1>").GivesNumber("32767");
            AssertExpr("<- 32767 -1>").GivesNumber("-32768");

            // a single argument is unary negation
            AssertExpr("<- 123>").GivesNumber("-123");
            AssertExpr("<- -200>").GivesNumber("200");
            AssertExpr("<- 0>").GivesNumber("0");

            AssertExpr("<->").GivesNumber("0");
            AssertExpr("<- 5>").GivesNumber("-5");
            AssertExpr("<- 1 2 3>").GivesNumber("-4");
            AssertExpr("<- 1 2 3 4>").GivesNumber("-8");
            AssertExpr("<- 1 2 3 4 5>").GivesNumber("-13");

            // alias
            AssertExpr("<SUB 1 2>").GivesNumber("-1");
        }

        [TestMethod]
        public void TestSUB_BACK()
        {
            // alias where 2nd operand defaults to 1
            AssertExpr("<BACK 1>").GivesNumber("0");
            AssertExpr("<BACK 1 2>").GivesNumber("-1");
        }

        [TestMethod]
        public void TestTHROW()
        {
            // V5 to V6
            // 2 to 2 operands
            AssertRoutine("\"AUX\" X", "<SET X <CATCH>> <THROWER .X> 123")
                .InV5()
                .WithGlobal("<ROUTINE THROWER (F) <THROW 456 .F>>")
                .GivesNumber("456");
        }

        [TestMethod]
        public void TestTHROW_Error()
        {
            // only exists in V5+
            AssertExpr("<THROW 0 0>").InV4().DoesNotCompile();

            // V5 to V6
            // 2 to 2 operands
            AssertExpr("<THROW>").InV5().DoesNotCompile();
            AssertExpr("<THROW 0>").InV5().DoesNotCompile();
            AssertExpr("<THROW 0 0 0>").InV5().DoesNotCompile();
        }

        [TestMethod]
        public void TestUSL()
        {
            // V1 to V3
            // 0 to 0 operands
            AssertExpr("<USL>").InV3().Compiles();
        }

        [TestMethod]
        public void TestUSL_Error()
        {
            // V1 to V3
            // 0 to 0 operands
            AssertExpr("<USL 0>").InV3().DoesNotCompile();

            AssertExpr("<USL>").InV4().DoesNotCompile();
        }

        [TestMethod]
        public void TestVALUE()
        {
            // V1 to V6
            AssertRoutine("\"AUX\" (X 123)", "<VALUE X>").GivesNumber("123");
            AssertExpr("<VALUE G>")
                .WithGlobal("<GLOBAL G 123>")
                .GivesNumber("123");
            AssertRoutine("", "<PUSH 1234> <VALUE 0>").GivesNumber("1234");
        }

        [TestMethod]
        public void TestVALUE_Error()
        {
            // V1 to V6
            AssertExpr("<VALUE>").DoesNotCompile();
            AssertExpr("<VALUE 0 0>").DoesNotCompile();
            AssertExpr("<VALUE ASDF>").DoesNotCompile();
        }

        [TestMethod]
        public void TestVERIFY()
        {
            // V3 to V6
            // 0 to 0 operands
            AssertExpr("<VERIFY>").InV3().GivesNumber("1");
        }

        [TestMethod]
        public void TestVERIFY_Error()
        {
            // V3 to V6
            // 0 to 0 operands
            AssertExpr("<VERIFY 0>").InV3().DoesNotCompile();
        }

        [TestMethod]
        public void TestWINATTR_V6()
        {
            // only exists in V6+
            Assert.Inconclusive();
            
            // V6 to V6
            // 0 to 4 operands
            AssertExpr("<WINATTR>").InV6().Compiles();
            AssertExpr("<WINATTR 0>").InV6().Compiles();
            AssertExpr("<WINATTR 0 0>").InV6().Compiles();
            AssertExpr("<WINATTR 0 0 0>").InV6().Compiles();
            AssertExpr("<WINATTR 0 0 0 0>").InV6().Compiles();
            Assert.Inconclusive("This test was automatically generated.");
        }

        [TestMethod]
        public void TestWINATTR_Error_V6()
        {
            // only exists in V6+
            AssertExpr("<WINATTR>").InV5().DoesNotCompile();

            // V6 to V6
            // 0 to 4 operands
            AssertExpr("<WINATTR 0 0 0 0 0>").InV6().DoesNotCompile();
            Assert.Inconclusive("This test was automatically generated.");
        }

        [TestMethod]
        public void TestWINGET_V6()
        {
            // only exists in V6+
            Assert.Inconclusive();
            
            // V6 to V6
            // 0 to 4 operands
            AssertExpr("<WINGET>").InV6().Compiles();
            AssertExpr("<WINGET 0>").InV6().Compiles();
            AssertExpr("<WINGET 0 0>").InV6().Compiles();
            AssertExpr("<WINGET 0 0 0>").InV6().Compiles();
            AssertExpr("<WINGET 0 0 0 0>").InV6().Compiles();
            Assert.Inconclusive("This test was automatically generated.");
        }

        [TestMethod]
        public void TestWINGET_Error_V6()
        {
            // only exists in V6+
            AssertExpr("<WINGET>").InV5().DoesNotCompile();

            // V6 to V6
            // 0 to 4 operands
            AssertExpr("<WINGET 0 0 0 0 0>").InV6().DoesNotCompile();
            Assert.Inconclusive("This test was automatically generated.");
        }

        [TestMethod]
        public void TestWINPOS_V6()
        {
            // only exists in V6+
            Assert.Inconclusive();
            
            // V6 to V6
            // 0 to 4 operands
            AssertExpr("<WINPOS>").InV6().Compiles();
            AssertExpr("<WINPOS 0>").InV6().Compiles();
            AssertExpr("<WINPOS 0 0>").InV6().Compiles();
            AssertExpr("<WINPOS 0 0 0>").InV6().Compiles();
            AssertExpr("<WINPOS 0 0 0 0>").InV6().Compiles();
            Assert.Inconclusive("This test was automatically generated.");
        }

        [TestMethod]
        public void TestWINPOS_Error_V6()
        {
            // only exists in V6+
            AssertExpr("<WINPOS>").InV5().DoesNotCompile();

            // V6 to V6
            // 0 to 4 operands
            AssertExpr("<WINPOS 0 0 0 0 0>").InV6().DoesNotCompile();
            Assert.Inconclusive("This test was automatically generated.");
        }

        [TestMethod]
        public void TestWINPUT_V6()
        {
            // only exists in V6+
            Assert.Inconclusive();
            
            // V6 to V6
            // 0 to 4 operands
            AssertExpr("<WINPUT>").InV6().Compiles();
            AssertExpr("<WINPUT 0>").InV6().Compiles();
            AssertExpr("<WINPUT 0 0>").InV6().Compiles();
            AssertExpr("<WINPUT 0 0 0>").InV6().Compiles();
            AssertExpr("<WINPUT 0 0 0 0>").InV6().Compiles();
            Assert.Inconclusive("This test was automatically generated.");
        }

        [TestMethod]
        public void TestWINPUT_Error_V6()
        {
            // only exists in V6+
            AssertExpr("<WINPUT>").InV5().DoesNotCompile();

            // V6 to V6
            // 0 to 4 operands
            AssertExpr("<WINPUT 0 0 0 0 0>").InV6().DoesNotCompile();
            Assert.Inconclusive("This test was automatically generated.");
        }

        [TestMethod]
        public void TestWINSIZE_V6()
        {
            // only exists in V6+
            Assert.Inconclusive();
            
            // V6 to V6
            // 0 to 4 operands
            AssertExpr("<WINSIZE>").InV6().Compiles();
            AssertExpr("<WINSIZE 0>").InV6().Compiles();
            AssertExpr("<WINSIZE 0 0>").InV6().Compiles();
            AssertExpr("<WINSIZE 0 0 0>").InV6().Compiles();
            AssertExpr("<WINSIZE 0 0 0 0>").InV6().Compiles();
            Assert.Inconclusive("This test was automatically generated.");
        }

        [TestMethod]
        public void TestWINSIZE_Error_V6()
        {
            // only exists in V6+
            AssertExpr("<WINSIZE>").InV5().DoesNotCompile();

            // V6 to V6
            // 0 to 4 operands
            AssertExpr("<WINSIZE 0 0 0 0 0>").InV6().DoesNotCompile();
            Assert.Inconclusive("This test was automatically generated.");
        }

        // XCALL is not supported in ZIL

        [TestMethod]
        public void TestXPUSH_V6()
        {
            // only exists in V6+
            Assert.Inconclusive();
            
            // V6 to V6
            // 0 to 4 operands
            AssertExpr("<XPUSH>").InV6().Compiles();
            AssertExpr("<XPUSH 0>").InV6().Compiles();
            AssertExpr("<XPUSH 0 0>").InV6().Compiles();
            AssertExpr("<XPUSH 0 0 0>").InV6().Compiles();
            AssertExpr("<XPUSH 0 0 0 0>").InV6().Compiles();
            Assert.Inconclusive("This test was automatically generated.");
        }

        [TestMethod]
        public void TestXPUSH_Error_V6()
        {
            // only exists in V6+
            AssertExpr("<XPUSH>").InV5().DoesNotCompile();

            // V6 to V6
            // 0 to 4 operands
            AssertExpr("<XPUSH 0 0 0 0 0>").InV6().DoesNotCompile();
            Assert.Inconclusive("This test was automatically generated.");
        }

        [TestMethod]
        public void TestZERO_P()
        {
            // V1 to V6
            // 1 to 1 operands
            AssertExpr("<ZERO? 0>").InV3().GivesNumber("1");
            AssertExpr("<ZERO? -5>").InV3().GivesNumber("0");

            // alias
            AssertExpr("<0? 0>").InV3().GivesNumber("1");
        }

        [TestMethod]
        public void TestZERO_P_Error()
        {
            // V1 to V6
            // 1 to 1 operands
            AssertExpr("<ZERO?>").InV3().DoesNotCompile();
            AssertExpr("<ZERO? 0 0>").InV3().DoesNotCompile();
        }

        [TestMethod]
        public void TestZWSTR()
        {
            // V5 to V6
            // 4 operands
            AssertRoutine("", "<ZWSTR ,SRCBUF 5 1 ,DSTBUF> <PRINTB ,DSTBUF>")
                .InV5()
                .WithGlobal("<GLOBAL SRCBUF <TABLE (STRING) \"hello\">>")
                .WithGlobal("<GLOBAL DSTBUF <TABLE 0 0 0>>")
                .Outputs("hello");
        }

        [TestMethod]
        public void TestZWSTR_Error()
        {
            // only exists in V5+
            AssertExpr("<ZWSTR>").InV4().DoesNotCompile();

            // V5 to V6
            // 4 operands
            AssertExpr("<ZWSTR>").InV5().DoesNotCompile();
            AssertExpr("<ZWSTR 0>").InV5().DoesNotCompile();
            AssertExpr("<ZWSTR 0 0>").InV5().DoesNotCompile();
            AssertExpr("<ZWSTR 0 0 0>").InV5().DoesNotCompile();
            AssertExpr("<ZWSTR 0 0 0 0 0>").InV5().DoesNotCompile();
        }

        #endregion

        #region Not Exactly Opcodes

        [TestMethod]
        public void TestLOWCORE()
        {
            AssertRoutine("", "<LOWCORE FLAGS>")
                .GeneratesCodeMatching(@"^\s*GET 16,0 >STACK\s*$");
        }

        #endregion
    }
}
