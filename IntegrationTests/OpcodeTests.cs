using System;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System.Diagnostics.Contracts;

namespace IntegrationTests
{
    [TestClass]
    public class OpcodeTests
    {
        private class ExprAssertionHelper
        {
            private readonly string expression;
            private string zversion = "ZIP";

            public ExprAssertionHelper(string expression)
            {
                Contract.Requires(!string.IsNullOrWhiteSpace(expression));

                this.expression = expression;
            }

            public ExprAssertionHelper InV3()
            {
                zversion = "ZIP";
                return this;
            }

            public ExprAssertionHelper InV4()
            {
                zversion = "EZIP";
                return this;
            }

            public ExprAssertionHelper InV5()
            {
                zversion = "XZIP";
                return this;
            }

            public ExprAssertionHelper InV6()
            {
                zversion = "YZIP";
                return this;
            }

            public ExprAssertionHelper InV7()
            {
                zversion = "7";
                return this;
            }

            public ExprAssertionHelper InV8()
            {
                zversion = "8";
                return this;
            }

            public void GivesNumber(string expectedValue)
            {
                Contract.Requires(expectedValue != null);

                const string SExprTestCode = @"<VERSION {0}> <CONSTANT RELEASEID 1> <ROUTINE GO () <PRINTN {1}>>";

                ZlrHelper.RunAndAssert(
                    string.Format(SExprTestCode, zversion, expression),
                    null,
                    expectedValue);
            }

            public void Outputs(string expectedValue)
            {
                Contract.Requires(expectedValue != null);

                const string SExprTestCode = @"<VERSION {0}> <CONSTANT RELEASEID 1> <ROUTINE GO () {1}>";

                ZlrHelper.RunAndAssert(
                    string.Format(SExprTestCode, zversion, expression),
                    null,
                    expectedValue);
            }

            public void DoesNotCompile()
            {
                const string SExprTestCode = @"<VERSION {0}> <CONSTANT RELEASEID 1> <GLOBAL X <>> <ROUTINE GO () <SETG X {1}> <QUIT>>";

                var result = ZlrHelper.Run(
                    string.Format(SExprTestCode, zversion, expression),
                    null,
                    compileOnly: true);

                Assert.AreEqual(ZlrTestStatus.CompilationFailed, result.Status);
            }

            public void Compiles()
            {
                const string SExprTestCode = @"<VERSION {0}> <CONSTANT RELEASEID 1> <GLOBAL X <>> <ROUTINE GO () <SETG X {1}> <QUIT>>";

                var result = ZlrHelper.Run(
                    string.Format(SExprTestCode, zversion, expression),
                    null,
                    compileOnly: true);

                Assert.IsTrue(result.Status > ZlrTestStatus.CompilationFailed, "Failed to compile");
            }
        }

        private ExprAssertionHelper AssertExpr(string expression)
        {
            Contract.Requires(!string.IsNullOrWhiteSpace(expression));

            return new ExprAssertionHelper(expression);
        }

        [TestMethod]
        public void TestADD()
        {
            AssertExpr("<+ 1 2>").GivesNumber("3");
            AssertExpr("<+ 1 -2>").GivesNumber("-1");
            AssertExpr("<+ 32767 1>").GivesNumber("-32768");
            AssertExpr("<+ -32768 -1>").GivesNumber("32767");

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
        public void TestADD_Error()
        {
            AssertExpr("<+>").DoesNotCompile();
            AssertExpr("<+ 1>").DoesNotCompile();
            AssertExpr("<+ 1 2 3>").DoesNotCompile();
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
            // needs a routine
            Assert.Inconclusive();
        }

        [TestMethod]
        public void TestASSIGNED_P_Error()
        {
            // needs a routine
            Assert.Inconclusive();
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

        [TestMethod]
        public void TestCALL()
        {
            // alias: APPLY
            Assert.Inconclusive();
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

            // needs a table
            Assert.Inconclusive();
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
            Assert.Inconclusive();
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
            // needs a variable
            Assert.Inconclusive();
        }

        [TestMethod]
        public void TestDEC_Error()
        {
            AssertExpr("<DEC>").DoesNotCompile();
            AssertExpr("<DEC 1 1>").DoesNotCompile();

            // should test with a nonexistent variable to
            Assert.Inconclusive();
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

            // needs a table
            Assert.Inconclusive();
            AssertExpr("<DIROUT 3 0>").GivesNumber("1");
        }

        [TestMethod]
        public void TestDIROUT_V6()
        {
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
        }

        [TestMethod]
        public void TestDIV_Error()
        {
            AssertExpr("<DIV>").DoesNotCompile();
            AssertExpr("<DIV 1>").DoesNotCompile();
            AssertExpr("<DIV 1 1 1>").DoesNotCompile();
        }

        [TestMethod]
        public void TestDLESS_P()
        {
            // needs a variable
            Assert.Inconclusive();
        }

        [TestMethod]
        public void TestDLESS_P_Error()
        {
            // needs a variable
            Assert.Inconclusive();
        }

        [TestMethod]
        public void TestEQUAL_P()
        {
            AssertExpr("<EQUAL? 1 1>").GivesNumber("1");
            AssertExpr("<EQUAL? 1 2>").GivesNumber("0");
            AssertExpr("<EQUAL? 1 2 1>").GivesNumber("1");
            AssertExpr("<EQUAL? 1 2 3 4>").GivesNumber("0");

            // alias
            AssertExpr("<=? 1 1>").GivesNumber("1");
            AssertExpr("<==? 1 1>").GivesNumber("1");
        }

        [TestMethod]
        public void TestEQUAL_P_Error()
        {
            AssertExpr("<EQUAL?>").DoesNotCompile();
            AssertExpr("<EQUAL? 1 2 3 4 5>").DoesNotCompile();
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
            // needs an object
            Assert.Inconclusive();
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
            // needs an object
            Assert.Inconclusive();
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
            // needs an object
            Assert.Inconclusive();
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
            // needs an object
            Assert.Inconclusive();
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
            // needs an object
            Assert.Inconclusive();
            // V1 to V6
            // 2 to 2 operands
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
            // needs an object
            Assert.Inconclusive();
            // V1 to V6
            // 2 to 2 operands
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
        public void TestGRTR_P_Cond()
        {
            // test as condition
            Assert.Inconclusive();
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
            // needs a variable
            Assert.Inconclusive();
            // V1 to V6
        }

        [TestMethod]
        public void TestIGRTR_P_Error()
        {
            // needs a variable
            Assert.Inconclusive();
            // V1 to V6
        }

        [TestMethod]
        public void TestIN_P()
        {
            // needs objects
            Assert.Inconclusive();
            // V1 to V6
            // 2 to 2 operands
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
            // needs a variable
            Assert.Inconclusive();
            // V1 to V6
        }

        [TestMethod]
        public void TestINC_Error()
        {
            // needs a variable
            Assert.Inconclusive();
            // V1 to V6
        }

        [TestMethod]
        public void TestINPUT()
        {
            // needs input
            Assert.Inconclusive();
            // V4 to V6
            // 1 to 3 operands
            AssertExpr("<INPUT 1>").InV4().Compiles();
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
        }

        [TestMethod]
        public void TestINTBL_P()
        {
            // needs a table
            Assert.Inconclusive();

            // V4 to V6
            // 3 to 4 operands
            AssertExpr("<INTBL? 0 0 0>").InV4().Compiles();

            // 4th operand is allowed in V5
            AssertExpr("<INTBL? 0 0 0 0>").InV5().Compiles();
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
        public void TestLESS_P_Cond()
        {
            // test as condition
            Assert.Inconclusive();
        }

        [TestMethod]
        public void TestLESS_P_Error()
        {
            // V1 to V6
            // 2 to 2 operands
            AssertExpr("<LESS?>").InV3().DoesNotCompile();
            AssertExpr("<LESS? 0>").InV3().DoesNotCompile();
            AssertExpr("<LESS? 0 0 0>").InV3().DoesNotCompile();
            Assert.Inconclusive("This test was automatically generated.");
        }

        [TestMethod]
        public void TestLEX()
        {
            // needs tables
            Assert.Inconclusive();

            // V5 to V6
            // 0 to 4 operands
            AssertExpr("<LEX>").InV5().Compiles();
            AssertExpr("<LEX 0>").InV5().Compiles();
            AssertExpr("<LEX 0 0>").InV5().Compiles();
            AssertExpr("<LEX 0 0 0>").InV5().Compiles();
            AssertExpr("<LEX 0 0 0 0>").InV5().Compiles();
            Assert.Inconclusive("This test was automatically generated.");
        }

        [TestMethod]
        public void TestLEX_Error()
        {
            // only exists in V5+
            AssertExpr("<LEX>").InV4().DoesNotCompile();

            // V5 to V6
            // 0 to 4 operands
            AssertExpr("<LEX 0 0 0 0 0>").InV5().DoesNotCompile();
            Assert.Inconclusive("This test was automatically generated.");
        }

        [TestMethod]
        public void TestLOC()
        {
            // needs an object
            Assert.Inconclusive();

            // V1 to V6
            // 1 to 1 operands
            AssertExpr("<LOC 0>").InV3().Compiles();
            Assert.Inconclusive("This test was automatically generated.");
        }

        [TestMethod]
        public void TestLOC_Error()
        {
            // V1 to V6
            // 1 to 1 operands
            AssertExpr("<LOC>").InV3().DoesNotCompile();
            AssertExpr("<LOC 0 0>").InV3().DoesNotCompile();
            Assert.Inconclusive("This test was automatically generated.");
        }

        [TestMethod]
        public void TestMARGIN_V6()
        {
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
        public void TestMARGIN_Error()
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
        public void TestMENU_Error()
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
        public void TestMOUSE_INFO_Error()
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
        public void TestMOUSE_LIMIT_Error()
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
            AssertExpr("<MOVE 0 0>").InV3().Compiles();
            Assert.Inconclusive("This test was automatically generated.");
        }

        [TestMethod]
        public void TestMOVE_Error()
        {
            // V1 to V6
            // 2 to 2 operands
            AssertExpr("<MOVE>").InV3().DoesNotCompile();
            AssertExpr("<MOVE 0>").InV3().DoesNotCompile();
            AssertExpr("<MOVE 0 0 0>").InV3().DoesNotCompile();
            Assert.Inconclusive("This test was automatically generated.");
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
        }

        [TestMethod]
        public void TestMUL_Error()
        {
            // V1 to V6
            // 2 to 2 operands
            AssertExpr("<MUL>").InV3().DoesNotCompile();
            AssertExpr("<MUL 0>").InV3().DoesNotCompile();
            AssertExpr("<MUL 0 0 0>").InV3().DoesNotCompile();
        }

        [TestMethod]
        public void TestNEXT_P()
        {
            // V1 to V6
            // 1 to 1 operands
            AssertExpr("<NEXT? 0>").InV3().Compiles();
            Assert.Inconclusive("This test was automatically generated.");
        }

        [TestMethod]
        public void TestNEXT_P_Error()
        {
            // V1 to V6
            // 1 to 1 operands
            AssertExpr("<NEXT?>").InV3().DoesNotCompile();
            AssertExpr("<NEXT? 0 0>").InV3().DoesNotCompile();
            Assert.Inconclusive("This test was automatically generated.");
        }

        [TestMethod]
        public void TestNEXTP()
        {
            // V1 to V6
            // 2 to 2 operands
            AssertExpr("<NEXTP 0 0>").InV3().Compiles();
            Assert.Inconclusive("This test was automatically generated.");
        }

        [TestMethod]
        public void TestNEXTP_Error()
        {
            // V1 to V6
            // 2 to 2 operands
            AssertExpr("<NEXTP>").InV3().DoesNotCompile();
            AssertExpr("<NEXTP 0>").InV3().DoesNotCompile();
            AssertExpr("<NEXTP 0 0 0>").InV3().DoesNotCompile();
            Assert.Inconclusive("This test was automatically generated.");
        }

        // NOOP is not supported in ZIL

        [TestMethod]
        public void TestORIGINAL_P()
        {
            // V5 to V6
            // 0 to 0 operands
            AssertExpr("<ORIGINAL?>").InV5().Compiles();
            Assert.Inconclusive("This test was automatically generated.");
        }

        [TestMethod]
        public void TestORIGINAL_P_Error()
        {
            // only exists in V5+
            AssertExpr("<ORIGINAL?>").InV4().DoesNotCompile();

            // V5 to V6
            // 0 to 0 operands
            AssertExpr("<ORIGINAL? 0>").InV5().DoesNotCompile();
            Assert.Inconclusive("This test was automatically generated.");
        }

        [TestMethod]
        public void TestPICINF_V6()
        {
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
        public void TestPICINF_Error()
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
        public void TestPICSET_Error()
        {
            // only exists in V6+
            AssertExpr("<PICSET>").InV5().DoesNotCompile();

            // V6 to V6
            // 0 to 4 operands
            AssertExpr("<PICSET 0 0 0 0 0>").InV6().DoesNotCompile();
            Assert.Inconclusive("This test was automatically generated.");
        }

        [TestMethod]
        public void TestPOP()
        {
            // V1 to V5
            // 1 to 1 operands

            // needs a variable
            Assert.Inconclusive();
            AssertExpr("<POP 1>").InV3().Compiles();
        }

        [TestMethod]
        public void TestPOP_V6()
        {
            // V6 to V6
            // 0 to 1 operands
            AssertExpr("<POP>").InV6().Compiles();
            AssertExpr("<POP 0>").InV6().Compiles();
        }

        [TestMethod]
        public void TestPOP_Error()
        {
            // V1 to V5
            // 1 to 1 operands
            AssertExpr("<POP>").InV3().DoesNotCompile();
            AssertExpr("<POP 0 0>").InV3().DoesNotCompile();
            // V6 to V6
            // 0 to 1 operands
            AssertExpr("<POP 0 0>").InV6().DoesNotCompile();
        }

        [TestMethod]
        public void TestPRINT()
        {
            // V1 to V6
            // 1 to 1 operands
            AssertExpr("<PRINT 0>").InV3().Compiles();
            Assert.Inconclusive("This test was automatically generated.");
        }

        [TestMethod]
        public void TestPRINT_Error()
        {
            // V1 to V6
            // 1 to 1 operands
            AssertExpr("<PRINT>").InV3().DoesNotCompile();
            AssertExpr("<PRINT 0 0>").InV3().DoesNotCompile();
            Assert.Inconclusive("This test was automatically generated.");
        }

        [TestMethod]
        public void TestPRINTB()
        {
            // V1 to V6
            // 1 to 1 operands
            AssertExpr("<PRINTB 0>").InV3().Compiles();
            Assert.Inconclusive("This test was automatically generated.");
        }

        [TestMethod]
        public void TestPRINTB_Error()
        {
            // V1 to V6
            // 1 to 1 operands
            AssertExpr("<PRINTB>").InV3().DoesNotCompile();
            AssertExpr("<PRINTB 0 0>").InV3().DoesNotCompile();
            Assert.Inconclusive("This test was automatically generated.");
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
            // needs an object
            Assert.Inconclusive();
            // V1 to V6
            // 1 to 1 operands
            AssertExpr("<PRINTD 0>").InV3().Compiles();
            Assert.Inconclusive("This test was automatically generated.");
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
        public void TestPRINTF_Error()
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
            Assert.Inconclusive("This test could not be automatically generated.");
        }

        [TestMethod]
        public void TestPRINTI_Error()
        {
            // V1 to V6
            Assert.Inconclusive("This test could not be automatically generated.");
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
            Assert.Inconclusive("This test could not be automatically generated.");
        }

        [TestMethod]
        public void TestPRINTR_Error()
        {
            // V1 to V6
            Assert.Inconclusive("This test could not be automatically generated.");
        }

        [TestMethod]
        public void TestPRINTT()
        {
            // V5 to V6
            // 0 to 4 operands
            AssertExpr("<PRINTT>").InV5().Compiles();
            AssertExpr("<PRINTT 0>").InV5().Compiles();
            AssertExpr("<PRINTT 0 0>").InV5().Compiles();
            AssertExpr("<PRINTT 0 0 0>").InV5().Compiles();
            AssertExpr("<PRINTT 0 0 0 0>").InV5().Compiles();
            Assert.Inconclusive("This test was automatically generated.");
        }

        [TestMethod]
        public void TestPRINTT_Error()
        {
            // only exists in V5+
            AssertExpr("<PRINTT>").InV4().DoesNotCompile();

            // V5 to V6
            // 0 to 4 operands
            AssertExpr("<PRINTT 0 0 0 0 0>").InV5().DoesNotCompile();
            Assert.Inconclusive("This test was automatically generated.");
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
            AssertExpr("<PTSIZE 0>").InV3().Compiles();
            Assert.Inconclusive("This test was automatically generated.");
        }

        [TestMethod]
        public void TestPTSIZE_Error()
        {
            // V1 to V6
            // 1 to 1 operands
            AssertExpr("<PTSIZE>").InV3().DoesNotCompile();
            AssertExpr("<PTSIZE 0 0>").InV3().DoesNotCompile();
            Assert.Inconclusive("This test was automatically generated.");
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
            AssertExpr("<READ 0 0>").InV3().Compiles();
            // V4
            // 2 to 4 operands
            AssertExpr("<READ 0 0>").InV4().Compiles();
            AssertExpr("<READ 0 0 0>").InV4().Compiles();
            AssertExpr("<READ 0 0 0 0>").InV4().Compiles();
            // V5 to V6
            // 1 to 4 operands
            AssertExpr("<READ 0>").InV5().Compiles();
            AssertExpr("<READ 0 0>").InV5().Compiles();
            AssertExpr("<READ 0 0 0>").InV5().Compiles();
            AssertExpr("<READ 0 0 0 0>").InV5().Compiles();

            // needs a table
            Assert.Inconclusive();
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
            AssertExpr("<REMOVE 0>").InV3().Compiles();
            Assert.Inconclusive("This test was automatically generated.");
        }

        [TestMethod]
        public void TestREMOVE_Error()
        {
            // V1 to V6
            // 1 to 1 operands
            AssertExpr("<REMOVE>").InV3().DoesNotCompile();
            AssertExpr("<REMOVE 0 0>").InV3().DoesNotCompile();
            Assert.Inconclusive("This test was automatically generated.");
        }

        [TestMethod]
        public void TestRESTART()
        {
            // V1 to V6
            // 0 to 0 operands
            AssertExpr("<RESTART>").InV3().Compiles();
            Assert.Inconclusive("This test was automatically generated.");
        }

        [TestMethod]
        public void TestRESTART_Error()
        {
            // V1 to V6
            // 0 to 0 operands
            AssertExpr("<RESTART 0>").InV3().DoesNotCompile();
            Assert.Inconclusive("This test was automatically generated.");
        }

        [TestMethod]
        public void TestRESTORE()
        {
            // V1 to V3
            // 0 to 0 operands
            AssertExpr("<RESTORE>").InV3().Compiles();
            Assert.Inconclusive("This test was automatically generated.");
            // V4 to V4
            // 0 to 0 operands
            AssertExpr("<RESTORE>").InV4().Compiles();
            Assert.Inconclusive("This test was automatically generated.");
            // V5 to V6
            // 0 to 4 operands
            AssertExpr("<RESTORE>").InV5().Compiles();
            AssertExpr("<RESTORE 0>").InV5().Compiles();
            AssertExpr("<RESTORE 0 0>").InV5().Compiles();
            AssertExpr("<RESTORE 0 0 0>").InV5().Compiles();
            AssertExpr("<RESTORE 0 0 0 0>").InV5().Compiles();
            Assert.Inconclusive("This test was automatically generated.");
        }

        [TestMethod]
        public void TestRESTORE_Error()
        {
            // V1 to V3
            // 0 to 0 operands
            AssertExpr("<RESTORE 0>").InV3().DoesNotCompile();
            Assert.Inconclusive("This test was automatically generated.");
            // V4 to V4
            // 0 to 0 operands
            AssertExpr("<RESTORE 0>").InV4().DoesNotCompile();
            Assert.Inconclusive("This test was automatically generated.");
            // V5 to V6
            // 0 to 4 operands
            AssertExpr("<RESTORE 0 0 0 0 0>").InV5().DoesNotCompile();
            Assert.Inconclusive("This test was automatically generated.");
        }

        [TestMethod]
        public void TestRETURN()
        {
            // V1 to V6
            // 1 to 1 operands
            AssertExpr("<RETURN 0>").InV3().Compiles();
            Assert.Inconclusive("This test was automatically generated.");
        }

        [TestMethod]
        public void TestRETURN_Error()
        {
            // V1 to V6
            // 1 to 1 operands
            AssertExpr("<RETURN>").InV3().DoesNotCompile();
            AssertExpr("<RETURN 0 0>").InV3().DoesNotCompile();
            Assert.Inconclusive("This test was automatically generated.");
        }

        [TestMethod]
        public void TestRFALSE()
        {
            // V1 to V6
            // 0 to 0 operands
            AssertExpr("<RFALSE>").InV3().Compiles();
            Assert.Inconclusive("This test was automatically generated.");
        }

        [TestMethod]
        public void TestRFALSE_Error()
        {
            // V1 to V6
            // 0 to 0 operands
            AssertExpr("<RFALSE 0>").InV3().DoesNotCompile();
            Assert.Inconclusive("This test was automatically generated.");
        }

        [TestMethod]
        public void TestRSTACK()
        {
            // V1 to V6
            // 0 to 0 operands
            AssertExpr("<RSTACK>").InV3().Compiles();
            Assert.Inconclusive("This test was automatically generated.");
        }

        [TestMethod]
        public void TestRSTACK_Error()
        {
            // V1 to V6
            // 0 to 0 operands
            AssertExpr("<RSTACK 0>").InV3().DoesNotCompile();
            Assert.Inconclusive("This test was automatically generated.");
        }

        [TestMethod]
        public void TestRTRUE()
        {
            // V1 to V6
            // 0 to 0 operands
            AssertExpr("<RTRUE>").InV3().Compiles();
            Assert.Inconclusive("This test was automatically generated.");
        }

        [TestMethod]
        public void TestRTRUE_Error()
        {
            // V1 to V6
            // 0 to 0 operands
            AssertExpr("<RTRUE 0>").InV3().DoesNotCompile();
            Assert.Inconclusive("This test was automatically generated.");
        }

        [TestMethod]
        public void TestSAVE()
        {
            // V1 to V3
            // 0 to 0 operands
            AssertExpr("<SAVE>").InV3().Compiles();
            Assert.Inconclusive("This test was automatically generated.");
            // V4 to V4
            // 0 to 0 operands
            AssertExpr("<SAVE>").InV4().Compiles();
            Assert.Inconclusive("This test was automatically generated.");
            // V5 to V6
            // 0 to 4 operands
            AssertExpr("<SAVE>").InV5().Compiles();
            AssertExpr("<SAVE 0>").InV5().Compiles();
            AssertExpr("<SAVE 0 0>").InV5().Compiles();
            AssertExpr("<SAVE 0 0 0>").InV5().Compiles();
            AssertExpr("<SAVE 0 0 0 0>").InV5().Compiles();
            Assert.Inconclusive("This test was automatically generated.");
        }

        [TestMethod]
        public void TestSAVE_Error()
        {
            // V1 to V3
            // 0 to 0 operands
            AssertExpr("<SAVE 0>").InV3().DoesNotCompile();
            Assert.Inconclusive("This test was automatically generated.");
            // V4 to V4
            // 0 to 0 operands
            AssertExpr("<SAVE 0>").InV4().DoesNotCompile();
            Assert.Inconclusive("This test was automatically generated.");
            // V5 to V6
            // 0 to 4 operands
            AssertExpr("<SAVE 0 0 0 0 0>").InV5().DoesNotCompile();
            Assert.Inconclusive("This test was automatically generated.");
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
            AssertExpr("<SCREEN 0>").InV3().DoesNotCompile();
            AssertExpr("<SCREEN 0 0>").InV3().DoesNotCompile();
        }

        [TestMethod]
        public void TestSCROLL_V6()
        {
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
        public void TestSCROLL_Error()
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
            Assert.Inconclusive("This test could not be automatically generated.");
        }

        [TestMethod]
        public void TestSET_Error()
        {
            // V1 to V6
            Assert.Inconclusive("This test could not be automatically generated.");
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
        public void TestSUB_Error()
        {
            AssertExpr("<->").DoesNotCompile();
            AssertExpr("<- 1 2 3>").DoesNotCompile();
        }

        [TestMethod]
        public void TestTHROW()
        {
            // V5 to V6
            // 2 to 2 operands
            AssertExpr("<THROW 0 0>").InV5().Compiles();
            Assert.Inconclusive("This test was automatically generated.");
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
            Assert.Inconclusive("This test was automatically generated.");
        }

        [TestMethod]
        public void TestUSL()
        {
            // V1 to V3
            // 0 to 0 operands
            AssertExpr("<USL>").InV3().Compiles();
            Assert.Inconclusive("This test was automatically generated.");
        }

        [TestMethod]
        public void TestUSL_Error()
        {
            // V1 to V3
            // 0 to 0 operands
            AssertExpr("<USL 0>").InV3().DoesNotCompile();
            Assert.Inconclusive("This test was automatically generated.");
        }

        [TestMethod]
        public void TestVALUE()
        {
            // V1 to V6
            Assert.Inconclusive("This test could not be automatically generated.");
        }

        [TestMethod]
        public void TestVALUE_Error()
        {
            // V1 to V6
            Assert.Inconclusive("This test could not be automatically generated.");
        }

        [TestMethod]
        public void TestVERIFY()
        {
            // V3 to V6
            // 0 to 0 operands
            AssertExpr("<VERIFY>").InV3().Compiles();
            Assert.Inconclusive("This test was automatically generated.");
        }

        [TestMethod]
        public void TestVERIFY_Error()
        {
            // V3 to V6
            // 0 to 0 operands
            AssertExpr("<VERIFY 0>").InV3().DoesNotCompile();
            Assert.Inconclusive("This test was automatically generated.");
        }

        [TestMethod]
        public void TestWINATTR_V6()
        {
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
        public void TestWINATTR_Error()
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
        public void TestWINGET_Error()
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
        public void TestWINPOS_Error()
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
        public void TestWINPUT_Error()
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
        public void TestWINSIZE_Error()
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
        public void TestXPUSH_Error()
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
            AssertExpr("<ZERO? 0>").InV3().Compiles();

            // alias
            AssertExpr("<0? 0>").InV3().Compiles();
            Assert.Inconclusive("This test was automatically generated.");
        }

        [TestMethod]
        public void TestZERO_P_Error()
        {
            // V1 to V6
            // 1 to 1 operands
            AssertExpr("<ZERO?>").InV3().DoesNotCompile();
            AssertExpr("<ZERO? 0 0>").InV3().DoesNotCompile();
            Assert.Inconclusive("This test was automatically generated.");
        }

        [TestMethod]
        public void TestZWSTR()
        {
            // V5 to V6
            // 4 operands
            AssertExpr("<ZWSTR 0 0 0 0>").InV5().Compiles();

            // needs a table
            Assert.Inconclusive("This test was automatically generated.");
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
    }
}
