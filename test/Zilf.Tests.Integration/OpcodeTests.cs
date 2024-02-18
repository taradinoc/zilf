/* Copyright 2010-2023 Tara McGrew
 * 
 * This file is part of ZILF.
 * 
 * ZILF is free software: you can redistribute it and/or modify it
 * under the terms of the GNU General Public License as published by
 * the Free Software Foundation, either version 3 of the License, or
 * (at your option) any later version.
 * 
 * ZILF is distributed in the hope that it will be useful, but
 * WITHOUT ANY WARRANTY; without even the implied warranty of
 * MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the GNU
 * General Public License for more details.
 * 
 * You should have received a copy of the GNU General Public License
 * along with ZILF.  If not, see <http://www.gnu.org/licenses/>.
 */

using System.Diagnostics.CodeAnalysis;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Zilf.Diagnostics;

namespace Zilf.Tests.Integration
{
    [TestClass, TestCategory("Compiler"), TestCategory("Arguments")]
    public class FormatArgCountTests
    {
        [TestMethod]
        public void TestExactly()
        {
            Assert.AreEqual("exactly 1 argument",
                ArgCountHelpers.FormatArgCount([
                    new ArgCountRange(1, 1),
                    new ArgCountRange(1, 1)
                ]));
        }

        [TestMethod]
        public void TestAlternatives()
        {
            Assert.AreEqual("1 or 2 arguments",
                ArgCountHelpers.FormatArgCount([
                    new ArgCountRange(1, 2)
                ]));

            Assert.AreEqual("1 or 2 arguments",
                ArgCountHelpers.FormatArgCount([
                    new ArgCountRange(1, 1),
                    new ArgCountRange(2, 2)
                ]));

            Assert.AreEqual("2 or 4 arguments",
                ArgCountHelpers.FormatArgCount([
                    new ArgCountRange(2, 2),
                    new ArgCountRange(4, 4)
                ]));

            Assert.AreEqual("0, 2, or 4 arguments",
                ArgCountHelpers.FormatArgCount([
                    new ArgCountRange(0, 0),
                    new ArgCountRange(2, 2),
                    new ArgCountRange(4, 4)
                ]));
        }

        [TestMethod]
        public void TestRange()
        {
            Assert.AreEqual("1 to 3 arguments",
                ArgCountHelpers.FormatArgCount([
                    new ArgCountRange(1, 3)
                ]));

            Assert.AreEqual("1 to 3 arguments",
                ArgCountHelpers.FormatArgCount([
                    new ArgCountRange(1, 2),
                    new ArgCountRange(3, 3)
                ]));

            Assert.AreEqual("1 to 3 arguments",
                ArgCountHelpers.FormatArgCount([
                    new ArgCountRange(1, 1),
                    new ArgCountRange(2, 2),
                    new ArgCountRange(3, 3)
                ]));
        }

        [TestMethod]
        public void TestUnlimited()
        {
            Assert.AreEqual("1 or more arguments",
                ArgCountHelpers.FormatArgCount([
                    new ArgCountRange(1, null)
                ]));

            Assert.AreEqual("1 or more arguments",
                ArgCountHelpers.FormatArgCount([
                    new ArgCountRange(1, 2),
                    new ArgCountRange(3, null)
                ]));
        }

        [TestMethod]
        public void TestDisjointRanges()
        {
            Assert.AreEqual("1, 2, or 4 arguments",
                ArgCountHelpers.FormatArgCount([
                    new ArgCountRange(1, 2),
                    new ArgCountRange(4, 4)
                ]));

            Assert.AreEqual("0, 1, 3, or 4 arguments",
                ArgCountHelpers.FormatArgCount([
                    new ArgCountRange(0, 1),
                    new ArgCountRange(3, 4)
                ]));

            Assert.AreEqual("0, 2, or more arguments",
                ArgCountHelpers.FormatArgCount([
                    new ArgCountRange(0, 0),
                    new ArgCountRange(2, null)
                ]));
        }
    }

    [TestClass, TestCategory("Compiler")]
    public class OpcodeTests : IntegrationTestClass
    {
        #region Z-Machine Opcodes

        [TestMethod]
        public async System.Threading.Tasks.Task TestADDAsync()
        {
            await AssertExpr("<+ 1 2>").GivesNumberAsync("3");
            await AssertExpr("<+ 1 -2>").GivesNumberAsync("-1");
            await AssertExpr("<+ 32767 1>").GivesNumberAsync("-32768");
            await AssertExpr("<+ -32768 -1>").GivesNumberAsync("32767");
            await AssertExpr("<+>").GivesNumberAsync("0");
            await AssertExpr("<+ 5>").GivesNumberAsync("5");
            await AssertExpr("<+ 1 2 3>").GivesNumberAsync("6");
            await AssertExpr("<+ 1 2 3 4>").GivesNumberAsync("10");
            await AssertExpr("<+ 1 2 3 4 5>").GivesNumberAsync("15");

            // alias
            await AssertExpr("<ADD 1 2>").GivesNumberAsync("3");
        }

        [TestMethod]
        public async System.Threading.Tasks.Task TestADD_RESTAsync()
        {
            // alias where 2nd operand defaults to 1
            await AssertExpr("<REST 1>").GivesNumberAsync("2");
            await AssertExpr("<REST 1 2>").GivesNumberAsync("3");
        }

        [TestMethod]
        public async System.Threading.Tasks.Task TestAPPLYAsync()
        {
            await AssertExpr("<APPLY 0>").GivesNumberAsync("0");
            await AssertExpr("<APPLY 0 1 2 3>").GivesNumberAsync("0");
            await AssertExpr("<APPLY 0 1 2 3 4 5 6 7>").InV5().GivesNumberAsync("0");

            await AssertRoutine("\"AUX\" X", "<SET X ,OTHER-ROUTINE> <APPLY .X 12>")
                .WithGlobal("<ROUTINE OTHER-ROUTINE (N) <* .N 2>>")
                .GivesNumberAsync("24");
        }

        [TestMethod]
        public async System.Threading.Tasks.Task TestAPPLY_ChoosesValueCallForPredAsync()
        {
            /* V5 has void-context and value-context versions of APPLY.
             * the void-context version is always true in predicate context,
             * so we need to prefer the value-context version. */

            await AssertRoutine("\"AUX\" X", "<SET X ,FALSE-ROUTINE> <COND (<APPLY .X> 123) (T 456)>")
                .InV5()
                .WithGlobal("<ROUTINE FALSE-ROUTINE () 0>")
                .GivesNumberAsync("456");
            await AssertRoutine("\"AUX\" X", "<SET X ,FALSE-ROUTINE> <COND (<NOT <APPLY .X>> 123) (T 456)>")
                .InV5()
                .WithGlobal("<ROUTINE FALSE-ROUTINE () 0>")
                .GivesNumberAsync("123");
        }

        [TestMethod]
        public async System.Threading.Tasks.Task TestAPPLY_ErrorAsync()
        {
            await AssertExpr("<APPLY>").DoesNotCompileAsync();
            await AssertExpr("<APPLY 0 1 2 3 4>").InV3().DoesNotCompileAsync();
            await AssertExpr("<APPLY 0 1 2 3 4 5 6 7 8>").InV5().DoesNotCompileAsync();
        }

        [TestMethod]
        public async System.Threading.Tasks.Task TestASHAsync()
        {
            // only exists in V5+
            await AssertExpr("<ASH 4 0>").InV5().GivesNumberAsync("4");
            await AssertExpr("<ASH 4 1>").InV5().GivesNumberAsync("8");
            await AssertExpr("<ASH 4 -2>").InV5().GivesNumberAsync("1");

            // alias
            await AssertExpr("<ASHIFT 4 0>").InV5().GivesNumberAsync("4");
        }

        [TestMethod]
        public async System.Threading.Tasks.Task TestASH_ErrorAsync()
        {
            await AssertExpr("<ASH 4 0>").InV3().DoesNotCompileAsync();
            await AssertExpr("<ASH 4 0>").InV4().DoesNotCompileAsync();

            await AssertExpr("<ASH>").InV5().DoesNotCompileAsync();
            await AssertExpr("<ASH 4>").InV5().DoesNotCompileAsync();
            await AssertExpr("<ASH 4 1 9>").InV5().DoesNotCompileAsync();
        }

        [TestMethod]
        public async System.Threading.Tasks.Task TestASSIGNED_PAsync()
        {
            await AssertRoutine("X", "<ASSIGNED? X>").InV5()
                .WhenCalledWith("999").GivesNumberAsync("1");
            await AssertRoutine("\"OPT\" X", "<ASSIGNED? X>").InV5()
                .WhenCalledWith("0").GivesNumberAsync("1");
            await AssertRoutine("\"OPT\" X", "<ASSIGNED? X>").InV5()
                .WhenCalledWith("").GivesNumberAsync("0");
        }

        [TestMethod]
        public async System.Threading.Tasks.Task TestASSIGNED_P_ErrorAsync()
        {
            await AssertRoutine("X", "<ASSIGNED? Y>").InV5().DoesNotCompileAsync();
            await AssertRoutine("X", "<ASSIGNED? 1>").InV5().DoesNotCompileAsync();
            await AssertRoutine("X", "<ASSIGNED?>").InV5().DoesNotCompileAsync();
            await AssertRoutine("X", "<ASSIGNED? X X>").InV5().DoesNotCompileAsync();
        }

        [TestMethod]
        public async System.Threading.Tasks.Task TestBANDAsync()
        {
            await AssertExpr("<BAND>").GivesNumberAsync("-1");
            await AssertExpr("<BAND 33>").GivesNumberAsync("33");
            await AssertExpr("<BAND 33 96>").GivesNumberAsync("32");
            await AssertExpr("<BAND 33 96 64>").GivesNumberAsync("0");

            // alias
            await AssertExpr("<ANDB 33 96>").GivesNumberAsync("32");
        }

        [TestMethod]
        public async System.Threading.Tasks.Task TestBCOMAsync()
        {
            await AssertExpr("<BCOM 32767>").GivesNumberAsync("-32768");

            // opcode changes in V5
            await AssertExpr("<BCOM 32767>").InV5().GivesNumberAsync("-32768");
        }

        [TestMethod]
        public async System.Threading.Tasks.Task TestBCOM_ErrorAsync()
        {
            await AssertExpr("<BCOM>").DoesNotCompileAsync();
            await AssertExpr("<BCOM 33 96>").DoesNotCompileAsync();
        }

        [TestMethod]
        public async System.Threading.Tasks.Task TestBORAsync()
        {
            await AssertExpr("<BOR>").GivesNumberAsync("0");
            await AssertExpr("<BOR 33>").GivesNumberAsync("33");
            await AssertExpr("<BOR 33 96>").GivesNumberAsync("97");
            await AssertExpr("<BOR 33 96 64>").GivesNumberAsync("97");

            // alias
            await AssertExpr("<ORB 33 96>").GivesNumberAsync("97");
        }

        [TestMethod]
        public async System.Threading.Tasks.Task TestBTSTAsync()
        {
            await AssertExpr("<BTST 64 64>").GivesNumberAsync("1");
            await AssertExpr("<BTST 64 63>").GivesNumberAsync("0");
            await AssertExpr("<BTST 97 33>").GivesNumberAsync("1");
        }

        [TestMethod]
        public async System.Threading.Tasks.Task TestBTST_ErrorAsync()
        {
            await AssertExpr("<BTST>").DoesNotCompileAsync();
            await AssertExpr("<BTST 97>").DoesNotCompileAsync();
            await AssertExpr("<BTST 97 31 29>").DoesNotCompileAsync();
        }

        [TestMethod]
        public async System.Threading.Tasks.Task TestBUFOUTAsync()
        {
            // only exists in V4+

            // we can't really test its side-effect here
            await AssertExpr("<BUFOUT 0>").InV4().GivesNumberAsync("1");
        }

        [TestMethod]
        public async System.Threading.Tasks.Task TestBUFOUT_ErrorAsync()
        {
            await AssertExpr("<BUFOUT 0>").InV3().DoesNotCompileAsync();

            await AssertExpr("<BUFOUT>").InV4().DoesNotCompileAsync();
            await AssertExpr("<BUFOUT 0 1>").InV4().DoesNotCompileAsync();
        }

        // CALL1 and CALL2 are not supported in ZIL

        [TestMethod]
        public async System.Threading.Tasks.Task TestCATCHAsync()
        {
            // only exists in V5+

            // the return value is unpredictable
            await AssertExpr("<CATCH>").InV5().CompilesAsync();
        }

        [TestMethod]
        public async System.Threading.Tasks.Task TestCATCH_ErrorAsync()
        {
            await AssertExpr("<CATCH>").InV3().DoesNotCompileAsync();
            await AssertExpr("<CATCH>").InV4().DoesNotCompileAsync();

            await AssertExpr("<CATCH 123>").InV5().DoesNotCompileAsync();
        }

        [TestMethod]
        public async System.Threading.Tasks.Task TestCHECKUAsync()
        {
            // only exists in V5+

            // only the lower 2 bits of the return value are defined
            await AssertExpr("<BAND 3 <CHECKU 65>>").InV5().GivesNumberAsync("3");
        }

        [TestMethod]
        public async System.Threading.Tasks.Task TestCHECKU_ErrorAsync()
        {
            await AssertExpr("<CHECKU 65>").InV3().DoesNotCompileAsync();
            await AssertExpr("<CHECKU 65>").InV4().DoesNotCompileAsync();

            await AssertExpr("<CHECKU>").InV5().DoesNotCompileAsync();
            await AssertExpr("<CHECKU 65 66>").InV5().DoesNotCompileAsync();
        }

        [TestMethod]
        public async System.Threading.Tasks.Task TestCLEARAsync()
        {
            // only exists in V4+

            // we can't really test its side-effect here
            await AssertExpr("<CLEAR 0>").InV4().GivesNumberAsync("1");
        }

        [TestMethod]
        public async System.Threading.Tasks.Task TestCLEAR_ErrorAsync()
        {
            await AssertExpr("<CLEAR 0>").InV3().DoesNotCompileAsync();

            await AssertExpr("<CLEAR>").InV4().DoesNotCompileAsync();
            await AssertExpr("<CLEAR 0 1>").InV4().DoesNotCompileAsync();
        }

        [TestMethod]
        public async System.Threading.Tasks.Task TestCOLORAsync()
        {
            // only exists in V5+

            // we can't really test its side-effect here
            await AssertExpr("<COLOR 5 5>").InV5().GivesNumberAsync("1");
        }

        [TestMethod]
        public void TestCOLOR_V6()
        {
            Assert.Inconclusive();

            // third argument is supported in V6+
/*
            await AssertExpr("<COLOR 5 5 1>").InV6().Compiles();
*/
        }

        [TestMethod]
        public async System.Threading.Tasks.Task TestCOLOR_ErrorAsync()
        {
            await AssertExpr("<COLOR 5 5>").InV3().DoesNotCompileAsync();
            await AssertExpr("<COLOR 5 5>").InV4().DoesNotCompileAsync();

            await AssertExpr("<COLOR 5 5 1>").InV5().DoesNotCompileAsync();

            await AssertExpr("<COLOR>").InV5().DoesNotCompileAsync();
            await AssertExpr("<COLOR 5>").InV5().DoesNotCompileAsync();
        }

        [TestMethod]
        public async System.Threading.Tasks.Task TestCOPYTAsync()
        {
            // only exists in V5+

            await AssertRoutine("", "<COPYT ,TABLE1 ,TABLE2 6> <GET ,TABLE2 2>")
                .InV5()
                .WithGlobal("<GLOBAL TABLE1 <TABLE 1 2 3>>")
                .WithGlobal("<GLOBAL TABLE2 <TABLE 0 0 0>>")
                .GivesNumberAsync("3");
        }

        [TestMethod]
        public async System.Threading.Tasks.Task TestCOPYT_ErrorAsync()
        {
            // only exists in V5+
            await AssertExpr("<COPYT 0 0 0>").InV3().DoesNotCompileAsync();
            await AssertExpr("<COPYT 0 0 0>").InV4().DoesNotCompileAsync();

            await AssertExpr("<COPYT>").InV5().DoesNotCompileAsync();
            await AssertExpr("<COPYT 0>").InV5().DoesNotCompileAsync();
            await AssertExpr("<COPYT 0 0>").InV5().DoesNotCompileAsync();
            await AssertExpr("<COPYT 0 0 0 0>").InV5().DoesNotCompileAsync();
        }

        [TestMethod]
        public async System.Threading.Tasks.Task TestCRLFAsync()
        {
            await AssertExpr("<CRLF>").OutputsAsync("\n");
        }

        [TestMethod]
        public async System.Threading.Tasks.Task TestCRLF_ErrorAsync()
        {
            await AssertExpr("<CRLF 1>").DoesNotCompileAsync();
        }

        [TestMethod]
        public async System.Threading.Tasks.Task TestCURGETAsync()
        {
            // only exists in V4+

            // needs a table
            await AssertExpr("<CURGET ,CURTABLE>")
                .InV4()
                .WithGlobal("<GLOBAL CURTABLE <TABLE 0 0>>")
                .CompilesAsync();
        }

        [TestMethod]
        public async System.Threading.Tasks.Task TestCURGET_ErrorAsync()
        {
            // only exists in V4+
            await AssertExpr("<CURGET 0>").InV3().DoesNotCompileAsync();

            await AssertExpr("<CURGET>").InV4().DoesNotCompileAsync();
            await AssertExpr("<CURGET 0 0>").InV4().DoesNotCompileAsync();
        }

        [TestMethod]
        public async System.Threading.Tasks.Task TestCURSETAsync()
        {
            // only exists in V4+

            // we can't really test its side-effect here
            await AssertExpr("<CURSET 1 1>").InV4().GivesNumberAsync("1");
        }

        [TestMethod]
        public async System.Threading.Tasks.Task TestCURSET_ErrorAsync()
        {
            // only exists in V4+
            await AssertExpr("<CURSET 1 1>").InV3().DoesNotCompileAsync();

            await AssertExpr("<CURSET>").InV4().DoesNotCompileAsync();
            await AssertExpr("<CURSET 1>").InV4().DoesNotCompileAsync();
            await AssertExpr("<CURSET 1 1 1>").InV4().DoesNotCompileAsync();
        }

        [TestMethod]
        public void TestDCLEAR_V6()
        {
            // only exists in V6+
            Assert.Inconclusive();
        }

        [TestMethod]
        public async System.Threading.Tasks.Task TestDECAsync()
        {
            await AssertRoutine("FOO", "<DEC FOO> .FOO").WhenCalledWith("200").GivesNumberAsync("199");
        }

        [TestMethod]
        public async System.Threading.Tasks.Task TestDEC_QuirksAsync()
        {
            await AssertRoutine("FOO", "<DEC .FOO> .FOO").WhenCalledWith("200").GivesNumberAsync("199");
            await AssertRoutine("", "<DEC ,FOO> ,FOO").WithGlobal("<GLOBAL FOO 5>").GivesNumberAsync("4");
        }

        [TestMethod]
        public async System.Threading.Tasks.Task TestDEC_ErrorAsync()
        {
            await AssertExpr("<DEC>").DoesNotCompileAsync();
            await AssertExpr("<DEC 1>").DoesNotCompileAsync();
            await AssertRoutine("FOO", "<DEC BAR>").DoesNotCompileAsync();
        }

        [TestMethod]
        public async System.Threading.Tasks.Task TestDIRINAsync()
        {
            await AssertExpr("<DIRIN 0>").GivesNumberAsync("1");
        }

        [TestMethod]
        public async System.Threading.Tasks.Task TestDIRIN_ErrorAsync()
        {
            await AssertExpr("<DIRIN>").DoesNotCompileAsync();
            await AssertExpr("<DIRIN 0 0>").DoesNotCompileAsync();
        }

        [TestMethod]
        public async System.Threading.Tasks.Task TestDIROUTAsync()
        {
            await AssertExpr("<DIROUT 1>").GivesNumberAsync("1");

            // output stream 3 needs a table
            await AssertRoutine("", "<DIROUT 3 ,OUTTABLE> <PRINTI \"A\"> <DIROUT -3> <GETB ,OUTTABLE 2>")
                .WithGlobal("<GLOBAL OUTTABLE <LTABLE (BYTE) 0 0 0 0 0 0 0 0>>")
                .GivesNumberAsync("65");
        }

        [TestMethod]
        public async System.Threading.Tasks.Task TestDIROUT_V6Async()
        {
            // third operand allowed in V6
            await AssertExpr("<DIROUT 3 0 0>").InV6().CompilesAsync();
        }

        [TestMethod]
        public async System.Threading.Tasks.Task TestDIROUT_ErrorAsync()
        {
            await AssertExpr("<DIROUT>").DoesNotCompileAsync();
            await AssertExpr("<DIROUT 3 0 0>").InV5().DoesNotCompileAsync();
        }

        [TestMethod]
        public void TestDISPLAY_V6()
        {
            // only exists in V6
            Assert.Inconclusive();
        }

        [TestMethod]
        public async System.Threading.Tasks.Task TestDIVAsync()
        {
            await AssertExpr("<DIV 360 90>").GivesNumberAsync("4");
            await AssertExpr("<DIV 100 -2>").GivesNumberAsync("-50");
            await AssertExpr("<DIV -100 -2>").GivesNumberAsync("50");
            await AssertExpr("<DIV -17 2>").GivesNumberAsync("-8");
            await AssertExpr("<DIV>").GivesNumberAsync("1");
            await AssertExpr("<DIV 1>").GivesNumberAsync("1");
            await AssertExpr("<DIV 2>").GivesNumberAsync("0");
            await AssertExpr("<DIV 1 1>").GivesNumberAsync("1");
            await AssertExpr("<DIV 1 1 1>").GivesNumberAsync("1");
        }

        [TestMethod]
        public async System.Threading.Tasks.Task TestDLESS_PAsync()
        {
            // V1 to V6
            await AssertRoutine("FOO", "<PRINTN <DLESS? FOO 100>> <CRLF> <PRINTN .FOO>")
                .WhenCalledWith("100").OutputsAsync("1\n99");
            await AssertRoutine("FOO", "<PRINTN <DLESS? FOO 100>> <CRLF> <PRINTN .FOO>")
                .WhenCalledWith("101").OutputsAsync("0\n100");
        }

        [TestMethod]
        public async System.Threading.Tasks.Task TestDLESS_P_ErrorAsync()
        {
            // V1 to V6
            await AssertExpr("<DLESS?>").DoesNotCompileAsync();
            await AssertRoutine("FOO", "<DLESS? FOO>").DoesNotCompileAsync();
            await AssertExpr("<DLESS? 11 22>").DoesNotCompileAsync();
            await AssertRoutine("FOO", "<DLESS? BAR 100>").DoesNotCompileAsync();
            await AssertRoutine("FOO BAR", "<DLESS? FOO BAR>").DoesNotCompileAsync();
        }

        [TestMethod]
        public async System.Threading.Tasks.Task TestEQUAL_PAsync()
        {
            await AssertExpr("<EQUAL? 1 1>").GivesNumberAsync("1");
            await AssertExpr("<EQUAL? 1 2>").GivesNumberAsync("0");
            await AssertExpr("<EQUAL? 1 2 1>").GivesNumberAsync("1");
            await AssertExpr("<EQUAL? 1 2 3 4>").GivesNumberAsync("0");
            await AssertExpr("<EQUAL? 1 2 3 4 5 6 7 8 9 0 1>").GivesNumberAsync("1");

            await AssertExpr("<COND (<EQUAL? 1 2 3 4 5 6 1> 99) (T 0)>").GivesNumberAsync("99");
            await AssertRoutine("X", "<COND (<EQUAL? <+ .X 1> 2 4 6 8> 99) (T 0)>")
                .WhenCalledWith("7")
                .GivesNumberAsync("99");

            // alias
            await AssertExpr("<=? 1 1>").GivesNumberAsync("1");
            await AssertExpr("<==? 1 1>").GivesNumberAsync("1");
        }

        [TestMethod]
        public async System.Threading.Tasks.Task TestEQUAL_P_ErrorAsync()
        {
            await AssertExpr("<EQUAL?>").DoesNotCompileAsync();
        }

        [TestMethod]
        public async System.Threading.Tasks.Task TestERASEAsync()
        {
            // only exists in V4+

            // we can't really test its side-effect here
            await AssertExpr("<ERASE 1>").InV4().GivesNumberAsync("1");
        }

        [TestMethod]
        public async System.Threading.Tasks.Task TestERASE_ErrorAsync()
        {
            // only exists in V4+
            await AssertExpr("<ERASE 1>").InV3().DoesNotCompileAsync();

            await AssertExpr("<ERASE>").InV4().DoesNotCompileAsync();
            await AssertExpr("<ERASE 1 2>").InV4().DoesNotCompileAsync();
        }

        [TestMethod]
        public async System.Threading.Tasks.Task TestFCLEARAsync()
        {
            await AssertExpr("<FCLEAR ,MYOBJECT ,FOOBIT>")
                .WithGlobal("<OBJECT MYOBJECT (FLAGS FOOBIT)>")
                .GivesNumberAsync("1");
        }

        [TestMethod]
        public async System.Threading.Tasks.Task TestFCLEAR_ErrorAsync()
        {
            await AssertExpr("<FCLEAR>").DoesNotCompileAsync();
            await AssertExpr("<FCLEAR 1>").DoesNotCompileAsync();
            await AssertExpr("<FCLEAR 1 2 3>").DoesNotCompileAsync();
        }

        [TestMethod]
        public async System.Threading.Tasks.Task TestFIRST_PAsync()
        {
            await AssertExpr("<FIRST? ,MYOBJECT>")
                .WithGlobal("<OBJECT MYOBJECT>")
                .GivesNumberAsync("0");
            await AssertExpr("<==? <FIRST? ,MYOBJECT> ,INNEROBJECT>")
                .WithGlobal("<OBJECT MYOBJECT>")
                .WithGlobal("<OBJECT INNEROBJECT (LOC MYOBJECT)>")
                .GivesNumberAsync("1");
            await AssertExpr("<COND (<FIRST? ,MYOBJECT> <PRINTI \"yes\">)>")
                .WithGlobal("<OBJECT MYOBJECT>")
                .WithGlobal("<OBJECT INNEROBJECT (LOC MYOBJECT)>")
                .OutputsAsync("yes");
            await AssertExpr("<COND (<FIRST? ,INNEROBJECT> <PRINTI \"yes\">) (T <PRINTI \"no\">)>")
                .WithGlobal("<OBJECT MYOBJECT>")
                .WithGlobal("<OBJECT INNEROBJECT (LOC MYOBJECT)>")
                .OutputsAsync("no");
        }

        [TestMethod]
        public async System.Threading.Tasks.Task TestFIRST_P_ErrorAsync()
        {
            await AssertExpr("<FIRST?>").DoesNotCompileAsync();
            await AssertExpr("<FIRST? 0 0>").DoesNotCompileAsync();
        }

        [TestMethod]
        public async System.Threading.Tasks.Task TestFONTAsync()
        {
            // only exists in V5+
            await AssertExpr("<FONT 1>").InV5().CompilesAsync();
        }

        [TestMethod]
        public async System.Threading.Tasks.Task TestFONT_ErrorAsync()
        {
            // only exists in V5+
            await AssertExpr("<FONT 1>").InV3().DoesNotCompileAsync();
            await AssertExpr("<FONT 1>").InV4().DoesNotCompileAsync();

            await AssertExpr("<FONT>").InV5().DoesNotCompileAsync();
            await AssertExpr("<FONT 1 2>").InV5().DoesNotCompileAsync();
        }

        [TestMethod]
        public async System.Threading.Tasks.Task TestFSETAsync()
        {
            await AssertExpr("<FSET ,MYOBJECT ,FOOBIT>")
                .WithGlobal("<OBJECT MYOBJECT (FLAGS FOOBIT)>")
                .GivesNumberAsync("1");
        }

        [TestMethod]
        public async System.Threading.Tasks.Task TestFSET_ErrorAsync()
        {
            await AssertExpr("<FSET>").DoesNotCompileAsync();
            await AssertExpr("<FSET 0>").DoesNotCompileAsync();
            await AssertExpr("<FSET 0 1 2>").DoesNotCompileAsync();
        }

        [TestMethod]
        public async System.Threading.Tasks.Task TestFSET_PAsync()
        {
            await AssertRoutine("", "<PRINTN <FSET? ,OBJECT1 FOOBIT>> <CRLF> <PRINTN <FSET? ,OBJECT2 FOOBIT>>")
                .WithGlobal("<OBJECT OBJECT1 (FLAGS FOOBIT)>")
                .WithGlobal("<OBJECT OBJECT2>")
                .OutputsAsync("1\n0");
        }

        [TestMethod]
        public async System.Threading.Tasks.Task TestFSET_P_ErrorAsync()
        {
            await AssertExpr("<FSET?>").DoesNotCompileAsync();
            await AssertExpr("<FSET? 0>").DoesNotCompileAsync();
            await AssertExpr("<FSET? 0 1 2>").DoesNotCompileAsync();
        }

        [TestMethod]
        public async System.Threading.Tasks.Task TestFSTACK_V6Async()
        {
            // only the V6 version is supported in ZIL
            await AssertRoutine("",
                "<PUSH 123> <PUSH 0> <PUSH 0> <PUSH 0> <FSTACK 3> <POP>")
                .InV6()
                .GivesNumberAsync("123");

            await AssertRoutine("",
                "<FSTACK 3 ,MY-STACK> <GET ,MY-STACK 0>")
                .WithGlobal("<GLOBAL MY-STACK <TABLE 0 4 3 2 1>>")
                .InV6()
                .GivesNumberAsync("3");
        }

        [TestMethod]
        public async System.Threading.Tasks.Task TestFSTACK_ErrorAsync()
        {
            // only the V6 version is supported in ZIL
            await AssertExpr("<FSTACK 0>").InV3().DoesNotCompileAsync();
            await AssertExpr("<FSTACK 0>").InV4().DoesNotCompileAsync();
            await AssertExpr("<FSTACK 0>").InV5().DoesNotCompileAsync();

            await AssertExpr("<FSTACK>").InV6().DoesNotCompileAsync();
            await AssertExpr("<FSTACK 0 0 0>").InV6().DoesNotCompileAsync();
        }

        [TestMethod]
        public async System.Threading.Tasks.Task TestGETAsync()
        {
            // V1 to V6
            // 2 to 2 operands
            await AssertExpr("<GET 0 0>").InV3().CompilesAsync();
        }

        [TestMethod]
        public async System.Threading.Tasks.Task TestGET_ErrorAsync()
        {
            // V1 to V6
            // 2 to 2 operands
            await AssertExpr("<GET>").InV3().DoesNotCompileAsync();
            await AssertExpr("<GET 0>").InV3().DoesNotCompileAsync();
            await AssertExpr("<GET 0 0 0>").InV3().DoesNotCompileAsync();
        }

        [TestMethod]
        public async System.Threading.Tasks.Task TestGETBAsync()
        {
            // V1 to V6
            // 2 to 2 operands
            await AssertExpr("<GETB 0 0>").InV3().GivesNumberAsync("3");
        }

        [TestMethod]
        public async System.Threading.Tasks.Task TestGETB_ErrorAsync()
        {
            // V1 to V6
            // 2 to 2 operands
            await AssertExpr("<GETB>").InV3().DoesNotCompileAsync();
            await AssertExpr("<GETB 0>").InV3().DoesNotCompileAsync();
            await AssertExpr("<GETB 0 0 0>").InV3().DoesNotCompileAsync();
        }

        [TestMethod]
        public async System.Threading.Tasks.Task TestGETPAsync()
        {
            // V1 to V6
            // 2 to 2 operands
            await AssertExpr("<GETP ,MYOBJECT ,P?MYPROP>")
                .WithGlobal("<OBJECT MYOBJECT (MYPROP 123)>")
                .GivesNumberAsync("123");
            await AssertExpr("<GETP ,OBJECT2 ,P?MYPROP>")
                .WithGlobal("<OBJECT OBJECT1 (MYPROP 1)>")
                .WithGlobal("<OBJECT OBJECT2>")
                .GivesNumberAsync("0");
        }

        [TestMethod]
        public async System.Threading.Tasks.Task TestGETP_ErrorAsync()
        {
            // V1 to V6
            // 2 to 2 operands
            await AssertExpr("<GETP>").InV3().DoesNotCompileAsync();
            await AssertExpr("<GETP 0>").InV3().DoesNotCompileAsync();
            await AssertExpr("<GETP 0 0 0>").InV3().DoesNotCompileAsync();
        }

        [TestMethod]
        public async System.Threading.Tasks.Task TestGETPTAsync()
        {
            // V1 to V6
            // 2 to 2 operands
            await AssertExpr("<GET <GETPT ,MYOBJECT ,P?MYPROP> 0>")
                .WithGlobal("<OBJECT MYOBJECT (MYPROP 123)>")
                .GivesNumberAsync("123");
            await AssertExpr("<GETPT ,OBJECT2 ,P?MYPROP>")
                .WithGlobal("<OBJECT OBJECT1 (MYPROP 1)>")
                .WithGlobal("<OBJECT OBJECT2>")
                .GivesNumberAsync("0");
        }

        [TestMethod]
        public async System.Threading.Tasks.Task TestGETPT_ErrorAsync()
        {
            // V1 to V6
            // 2 to 2 operands
            await AssertExpr("<GETPT>").InV3().DoesNotCompileAsync();
            await AssertExpr("<GETPT 0>").InV3().DoesNotCompileAsync();
            await AssertExpr("<GETPT 0 0 0>").InV3().DoesNotCompileAsync();
        }

        [TestMethod]
        public async System.Threading.Tasks.Task TestGEq_PAsync()
        {
            // V1 to V6
            // 2 to 2 operands
            await AssertExpr("<G=? -1 3>").InV3().GivesNumberAsync("0");
            await AssertExpr("<G=? 3 -1>").InV3().GivesNumberAsync("1");
            await AssertExpr("<G=? 37 37>").InV3().GivesNumberAsync("1");

            // alias
            await AssertExpr("<G? 3 -1>").InV3().GivesNumberAsync("1");
        }

        [TestMethod]
        public async System.Threading.Tasks.Task TestGEq_P_ErrorAsync()
        {
            // V1 to V6
            // 2 to 2 operands
            await AssertExpr("<G=?>").InV3().DoesNotCompileAsync();
            await AssertExpr("<G=? 0>").InV3().DoesNotCompileAsync();
            await AssertExpr("<G=? 0 0 0>").InV3().DoesNotCompileAsync();
        }

        [TestMethod]
        public async System.Threading.Tasks.Task TestGRTR_PAsync()
        {
            // V1 to V6
            // 2 to 2 operands
            await AssertExpr("<GRTR? -1 3>").InV3().GivesNumberAsync("0");
            await AssertExpr("<GRTR? 3 -1>").InV3().GivesNumberAsync("1");
            await AssertExpr("<GRTR? 37 37>").InV3().GivesNumberAsync("0");

            // alias
            await AssertExpr("<G? 3 -1>").InV3().GivesNumberAsync("1");
        }

        [TestMethod]
        public async System.Threading.Tasks.Task TestGRTR_P_ErrorAsync()
        {
            // V1 to V6
            // 2 to 2 operands
            await AssertExpr("<GRTR?>").InV3().DoesNotCompileAsync();
            await AssertExpr("<GRTR? 0>").InV3().DoesNotCompileAsync();
            await AssertExpr("<GRTR? 0 0 0>").InV3().DoesNotCompileAsync();
        }

        [TestMethod]
        public async System.Threading.Tasks.Task TestHLIGHTAsync()
        {
            // V4 to V6
            // 1 operand
            await AssertExpr("<HLIGHT 4>").InV4().CompilesAsync();
        }

        [TestMethod]
        public async System.Threading.Tasks.Task TestHLIGHT_ErrorAsync()
        {
            // only exists in V4+
            await AssertExpr("<HLIGHT>").InV3().DoesNotCompileAsync();

            // V4 to V6
            // 1 operand
            await AssertExpr("<HLIGHT>").InV4().DoesNotCompileAsync();
            await AssertExpr("<HLIGHT 0 0>").InV4().DoesNotCompileAsync();
        }

        // ICALL, ICALL1, and ICALL2 are not supported in ZIL

        [TestMethod]
        public async System.Threading.Tasks.Task TestIGRTR_PAsync()
        {
            // V1 to V6
            await AssertRoutine("FOO", "<PRINTN <IGRTR? FOO 100>> <CRLF> <PRINTN .FOO>")
                .WhenCalledWith("100").OutputsAsync("1\n101");
            await AssertRoutine("FOO", "<PRINTN <IGRTR? FOO 100>> <CRLF> <PRINTN .FOO>")
                .WhenCalledWith("99").OutputsAsync("0\n100");
        }

        [TestMethod]
        public async System.Threading.Tasks.Task TestIGRTR_P_ErrorAsync()
        {
            // V1 to V6
            await AssertExpr("<IGRTR?>").DoesNotCompileAsync();
            await AssertRoutine("FOO", "<IGRTR? FOO>").DoesNotCompileAsync();
            await AssertExpr("<IGRTR? 11 22>").DoesNotCompileAsync();
            await AssertRoutine("FOO", "<IGRTR? BAR 100>").DoesNotCompileAsync();
            await AssertRoutine("FOO BAR", "<IGRTR? FOO BAR>").DoesNotCompileAsync();
        }

        [TestMethod]
        public async System.Threading.Tasks.Task TestIN_PAsync()
        {
            // V1 to V6
            // 2 to 2 operands
            await AssertExpr("<COND (<IN? ,CAT ,HAT> 123) (T 456)>")
                .WithGlobal("<OBJECT HAT>")
                .WithGlobal("<OBJECT CAT (LOC HAT)>")
                .GivesNumberAsync("123");
            await AssertExpr("<COND (<IN? ,CAT ,HAT> 123) (T 456)>")
                .WithGlobal("<OBJECT HAT (LOC CAT)>")
                .WithGlobal("<OBJECT CAT>")
                .GivesNumberAsync("456");
        }

        [TestMethod]
        public async System.Threading.Tasks.Task TestIN_P_ErrorAsync()
        {
            // V1 to V6
            // 2 to 2 operands
            await AssertExpr("<IN?>").InV3().DoesNotCompileAsync();
            await AssertExpr("<IN? 0>").InV3().DoesNotCompileAsync();
            await AssertExpr("<IN? 0 0 0>").InV3().DoesNotCompileAsync();
        }

        [TestMethod]
        public async System.Threading.Tasks.Task TestINCAsync()
        {
            await AssertRoutine("FOO", "<INC FOO> .FOO").WhenCalledWith("200").GivesNumberAsync("201");
        }

        [TestMethod]
        public async System.Threading.Tasks.Task TestINC_QuirksAsync()
        {
            await AssertRoutine("FOO", "<INC .FOO> .FOO").WhenCalledWith("200").GivesNumberAsync("201");
            await AssertRoutine("", "<INC ,FOO> ,FOO").WithGlobal("<GLOBAL FOO 5>").GivesNumberAsync("6");
        }

        [TestMethod]
        public async System.Threading.Tasks.Task TestINC_ErrorAsync()
        {
            await AssertExpr("<INC>").DoesNotCompileAsync();
            await AssertExpr("<INC 1>").DoesNotCompileAsync();
            await AssertRoutine("FOO", "<INC BAR>").DoesNotCompileAsync();
        }

        [TestMethod]
        public async System.Threading.Tasks.Task TestINPUTAsync()
        {
            // V4 to V6
            // 1 to 3 operands
            await AssertExpr("<INPUT 1>")
                .InV4()
                .WithInput("A")
                .GivesNumberAsync("65");

            await AssertExpr("<INPUT 1 0>").InV4().CompilesAsync();
            await AssertExpr("<INPUT 1 0 0>").InV4().CompilesAsync();
        }

        [TestMethod]
        public async System.Threading.Tasks.Task TestINPUT_ErrorAsync()
        {
            // only exists in V4+
            await AssertExpr("<INPUT 1>").InV3().DoesNotCompileAsync();

            // V4 to V6
            // 0 to 4 operands
            await AssertExpr("<INPUT>").InV4().DoesNotCompileAsync();
            await AssertExpr("<INPUT 0 0 0 0>").InV4().DoesNotCompileAsync();
        }

        [TestMethod]
        public async System.Threading.Tasks.Task TestINTBL_PAsync()
        {
            // V4 to V6
            // 3 to 4 operands
            await AssertExpr("<COND (<INTBL? 3 ,MYTABLE 4> 123) (T 456)>")
                .InV4()
                .WithGlobal("<GLOBAL MYTABLE <TABLE 1 2 3 4>>")
                .GivesNumberAsync("123");
            await AssertExpr("<GET <INTBL? 3 ,MYTABLE 4> 0>")
                .InV4()
                .WithGlobal("<GLOBAL MYTABLE <TABLE 1 2 3 4>>")
                .GivesNumberAsync("3");
            await AssertExpr("<INTBL? 9 ,MYTABLE 4>")
                .InV4()
                .WithGlobal("<GLOBAL MYTABLE <TABLE 1 2 3 4>>")
                .GivesNumberAsync("0");

            // 4th operand is allowed in V5
            await AssertExpr("<GETB <INTBL? 10 ,MYTABLE 9 3> 0>")
                .InV5()
                .WithGlobal("<GLOBAL MYTABLE <TABLE (BYTE) 111 111 111 222 222 222 10 123 123>>")
                .GivesNumberAsync("10");
        }

        [TestMethod]
        public async System.Threading.Tasks.Task TestINTBL_P_ErrorAsync()
        {
            // only exists in V4+
            await AssertExpr("<INTBL? 0 0 0>").InV3().DoesNotCompileAsync();

            // V4 to V6
            // 3 to 4 operands
            await AssertExpr("<INTBL?>").InV4().DoesNotCompileAsync();
            await AssertExpr("<INTBL? 0>").InV4().DoesNotCompileAsync();
            await AssertExpr("<INTBL? 0 0>").InV4().DoesNotCompileAsync();

            // 4th operand is only allowed in V5
            await AssertExpr("<INTBL? 0 0 0 0>").InV4().DoesNotCompileAsync();
            await AssertExpr("<INTBL? 0 0 0 0 0>").InV5().DoesNotCompileAsync();
        }

        [TestMethod]
        public async System.Threading.Tasks.Task TestIRESTOREAsync()
        {
            // V5 to V6
            // 0 operands
            await AssertExpr("<IRESTORE>").InV5().CompilesAsync();
        }

        [TestMethod]
        public async System.Threading.Tasks.Task TestIRESTORE_ErrorAsync()
        {
            // only exists in V5+
            await AssertExpr("<IRESTORE>").InV4().DoesNotCompileAsync();

            // V5 to V6
            // 0 operands
            await AssertExpr("<IRESTORE 0>").InV5().DoesNotCompileAsync();
        }

        [TestMethod]
        public async System.Threading.Tasks.Task TestISAVEAsync()
        {
            // V5 to V6
            // 0 operands
            await AssertExpr("<ISAVE>").InV5().CompilesAsync();
        }

        [TestMethod]
        public async System.Threading.Tasks.Task TestISAVE_ErrorAsync()
        {
            // only exists in V5+
            await AssertExpr("<ISAVE>").InV4().DoesNotCompileAsync();

            // V5 to V6
            // 0 operands
            await AssertExpr("<ISAVE 0>").InV5().DoesNotCompileAsync();
        }

        // IXCALL and JUMP are not supported in ZIL

        [TestMethod]
        public async System.Threading.Tasks.Task TestLEq_PAsync()
        {
            // V1 to V6
            // 2 to 2 operands
            await AssertExpr("<L=? -1 3>").InV3().GivesNumberAsync("1");
            await AssertExpr("<L=? 3 -1>").InV3().GivesNumberAsync("0");
            await AssertExpr("<L=? 37 37>").InV3().GivesNumberAsync("1");

            // alias
            await AssertExpr("<L? 3 -1>").InV3().GivesNumberAsync("0");
        }

        [TestMethod]
        public async System.Threading.Tasks.Task TestLEq_P_ErrorAsync()
        {
            // V1 to V6
            // 2 to 2 operands
            await AssertExpr("<L=?>").InV3().DoesNotCompileAsync();
            await AssertExpr("<L=? 0>").InV3().DoesNotCompileAsync();
            await AssertExpr("<L=? 0 0 0>").InV3().DoesNotCompileAsync();
        }

        [TestMethod]
        public async System.Threading.Tasks.Task TestLESS_PAsync()
        {
            // V1 to V6
            // 2 to 2 operands
            await AssertExpr("<LESS? -1 3>").InV3().GivesNumberAsync("1");
            await AssertExpr("<LESS? 3 -1>").InV3().GivesNumberAsync("0");
            await AssertExpr("<LESS? 37 37>").InV3().GivesNumberAsync("0");

            // alias
            await AssertExpr("<L? 3 -1>").InV3().GivesNumberAsync("0");
        }

        [TestMethod]
        public async System.Threading.Tasks.Task TestLESS_P_ErrorAsync()
        {
            // V1 to V6
            // 2 to 2 operands
            await AssertExpr("<LESS?>").InV3().DoesNotCompileAsync();
            await AssertExpr("<LESS? 0>").InV3().DoesNotCompileAsync();
            await AssertExpr("<LESS? 0 0 0>").InV3().DoesNotCompileAsync();
        }

        [TestMethod]
        public async System.Threading.Tasks.Task TestLEXAsync()
        {
            // V5 to V6
            // 2 to 4 operands

            await AssertRoutine("", "<LEX ,TEXTBUF ,LEXBUF> <PRINTB <GET ,LEXBUF 1>>")
                .InV5()
                .WithGlobal("<GLOBAL TEXTBUF <TABLE (BYTE) 3 3 !\\c !\\a !\\t>>")
                .WithGlobal("<GLOBAL LEXBUF <ITABLE 1 (LEXV) 0 0 0>>")
                .WithGlobal("<OBJECT CAT (SYNONYM CAT)>")
                .OutputsAsync("cat");

            await AssertExpr("<LEX 0 0 0>").InV5().CompilesAsync();
            await AssertExpr("<LEX 0 0 0 0>").InV5().CompilesAsync();
        }

        [TestMethod]
        public async System.Threading.Tasks.Task TestLEX_ErrorAsync()
        {
            // only exists in V5+
            await AssertExpr("<LEX>").InV4().DoesNotCompileAsync();

            // V5 to V6
            // 2 to 4 operands
            await AssertExpr("<LEX>").InV5().DoesNotCompileAsync();
            await AssertExpr("<LEX 0>").InV5().DoesNotCompileAsync();
            await AssertExpr("<LEX 0 0 0 0 0>").InV5().DoesNotCompileAsync();
        }

        [TestMethod]
        public async System.Threading.Tasks.Task TestLOCAsync()
        {
            // V1 to V6
            // 1 to 1 operands
            await AssertExpr("<==? <LOC ,CAT> ,HAT>")
                .WithGlobal("<OBJECT CAT (LOC HAT)>")
                .WithGlobal("<OBJECT HAT>")
                .GivesNumberAsync("1");
            await AssertExpr("<LOC ,HAT>")
                .WithGlobal("<OBJECT CAT (LOC HAT)>")
                .WithGlobal("<OBJECT HAT>")
                .GivesNumberAsync("0");
        }

        [TestMethod]
        public async System.Threading.Tasks.Task TestLOC_ErrorAsync()
        {
            // V1 to V6
            // 1 to 1 operands
            await AssertExpr("<LOC>").InV3().DoesNotCompileAsync();
            await AssertExpr("<LOC 0 0>").InV3().DoesNotCompileAsync();
        }

        [TestMethod]
        public async System.Threading.Tasks.Task TestMARGIN_V6Async()
        {
            // V6 to V6
            // 2 to 3 operands
            await AssertExpr("<MARGIN 0 0>").InV6().CompilesAsync();
            await AssertExpr("<MARGIN 0 0 0>").InV6().CompilesAsync();
        }

        [TestMethod]
        public async System.Threading.Tasks.Task TestMARGIN_Error_V6Async()
        {
            // only exists in V6+
            await AssertExpr("<MARGIN>").InV5().DoesNotCompileAsync();

            // V6 to V6
            // 2 to 3 operands
            await AssertExpr("<MARGIN 0>").InV6().DoesNotCompileAsync();
            await AssertExpr("<MARGIN 0 0 0 0>").InV6().DoesNotCompileAsync();
        }

        [TestMethod]
        public async System.Threading.Tasks.Task TestMENU_V6Async()
        {
            // V6 to V6
            // 2 operands
            await AssertExpr("<MENU 0 0>").InV6().CompilesAsync();
        }

        [TestMethod]
        public async System.Threading.Tasks.Task TestMENU_Error_V6Async()
        {
            // only exists in V6+
            await AssertExpr("<MENU>").InV5().DoesNotCompileAsync();

            // V6 to V6
            // 2 operands
            await AssertExpr("<MENU>").InV6().DoesNotCompileAsync();
            await AssertExpr("<MENU 0>").InV6().DoesNotCompileAsync();
            await AssertExpr("<MENU 0 0 0>").InV6().DoesNotCompileAsync();
            await AssertExpr("<MENU 0 0 0 0>").InV6().DoesNotCompileAsync();
        }

        [TestMethod]
        public async System.Threading.Tasks.Task TestMODAsync()
        {
            // V1 to V6
            // 2 to 2 operands
            await AssertExpr("<MOD 15 4>").InV3().GivesNumberAsync("3");
            await AssertExpr("<MOD -15 4>").InV3().GivesNumberAsync("-3");
            await AssertExpr("<MOD -15 4>").InV3().GivesNumberAsync("-3");
            await AssertExpr("<MOD 15 -4>").InV3().GivesNumberAsync("3");
        }

        [TestMethod]
        public async System.Threading.Tasks.Task TestMOD_ErrorAsync()
        {
            // V1 to V6
            // 2 to 2 operands
            await AssertExpr("<MOD>").InV3().DoesNotCompileAsync();
            await AssertExpr("<MOD 0>").InV3().DoesNotCompileAsync();
            await AssertExpr("<MOD 0 0 0>").InV3().DoesNotCompileAsync();
        }

        [TestMethod]
        public void TestMOUSE_INFO_V6()
        {
            // only exists in V6+
            Assert.Inconclusive();

            // V6 to V6
            // 0 to 4 operands
/*
            await AssertExpr("<MOUSE-INFO>").InV6().Compiles();
            await AssertExpr("<MOUSE-INFO 0>").InV6().Compiles();
            await AssertExpr("<MOUSE-INFO 0 0>").InV6().Compiles();
            await AssertExpr("<MOUSE-INFO 0 0 0>").InV6().Compiles();
            await AssertExpr("<MOUSE-INFO 0 0 0 0>").InV6().Compiles();
            Assert.Inconclusive("This test was automatically generated.");
*/
        }

        [TestMethod]
        public async System.Threading.Tasks.Task TestMOUSE_INFO_Error_V6Async()
        {
            // only exists in V6+
            await AssertExpr("<MOUSE-INFO>").InV5().DoesNotCompileAsync();

            // V6 to V6
            // 0 to 4 operands
            await AssertExpr("<MOUSE-INFO 0 0 0 0 0>").InV6().DoesNotCompileAsync();
        }

        [TestMethod]
        public async System.Threading.Tasks.Task TestMOUSE_LIMIT_V6Async()
        {
            // V6 to V6
            // 1 operand
            await AssertExpr("<MOUSE-LIMIT 0>").InV6().CompilesAsync();
        }

        [TestMethod]
        public async System.Threading.Tasks.Task TestMOUSE_LIMIT_Error_V6Async()
        {
            // only exists in V6+
            await AssertExpr("<MOUSE-LIMIT>").InV5().DoesNotCompileAsync();

            // V6 to V6
            // 1 operand
            await AssertExpr("<MOUSE-LIMIT>").InV6().DoesNotCompileAsync();
            await AssertExpr("<MOUSE-LIMIT 0 0>").InV6().DoesNotCompileAsync();
            await AssertExpr("<MOUSE-LIMIT 0 0 0>").InV6().DoesNotCompileAsync();
            await AssertExpr("<MOUSE-LIMIT 0 0 0 0>").InV6().DoesNotCompileAsync();
            await AssertExpr("<MOUSE-LIMIT 0 0 0 0 0>").InV6().DoesNotCompileAsync();
        }

        [TestMethod]
        public async System.Threading.Tasks.Task TestMOVEAsync()
        {
            // V1 to V6
            // 2 to 2 operands
            await AssertRoutine("", "<MOVE ,CAT ,HAT> <IN? ,CAT ,HAT>")
                .WithGlobal("<OBJECT CAT>")
                .WithGlobal("<OBJECT HAT>")
                .GivesNumberAsync("1");
        }

        [TestMethod]
        public async System.Threading.Tasks.Task TestMOVE_ErrorAsync()
        {
            // V1 to V6
            // 2 to 2 operands
            await AssertExpr("<MOVE>").InV3().DoesNotCompileAsync();
            await AssertExpr("<MOVE 0>").InV3().DoesNotCompileAsync();
            await AssertExpr("<MOVE 0 0 0>").InV3().DoesNotCompileAsync();
        }

        [TestMethod]
        public async System.Threading.Tasks.Task TestMULAsync()
        {
            // V1 to V6
            // 2 to 2 operands
            await AssertExpr("<MUL 150 0>").InV3().GivesNumberAsync("0");
            await AssertExpr("<MUL 0 -6>").InV3().GivesNumberAsync("0");
            await AssertExpr("<MUL 150 3>").InV3().GivesNumberAsync("450");
            await AssertExpr("<MUL 150 -3>").InV3().GivesNumberAsync("-450");
            await AssertExpr("<MUL -15 4>").InV3().GivesNumberAsync("-60");
            await AssertExpr("<MUL -1 128>").InV3().GivesNumberAsync("-128");
            await AssertExpr("<MUL>").GivesNumberAsync("1");
            await AssertExpr("<MUL 5>").GivesNumberAsync("5");
            await AssertExpr("<MUL 1 2 3>").GivesNumberAsync("6");
            await AssertExpr("<MUL 1 2 3 4>").GivesNumberAsync("24");
            await AssertExpr("<MUL 1 2 3 4 -5>").GivesNumberAsync("-120");
        }

        [TestMethod]
        public async System.Threading.Tasks.Task TestNEXT_PAsync()
        {
            // V1 to V6
            // 1 to 1 operands
            await AssertRoutine("", "<MOVE ,RAT ,HAT> <==? <NEXT? ,RAT> ,CAT>")
                .WithGlobal("<OBJECT CAT (LOC HAT)>")
                .WithGlobal("<OBJECT HAT>")
                .WithGlobal("<OBJECT RAT>")
                .GivesNumberAsync("1");
            await AssertExpr("<NEXT? ,CAT>")
                .WithGlobal("<OBJECT CAT (LOC HAT)>")
                .WithGlobal("<OBJECT HAT>")
                .GivesNumberAsync("0");
        }

        [TestMethod]
        public async System.Threading.Tasks.Task TestNEXT_P_ErrorAsync()
        {
            // V1 to V6
            // 1 to 1 operands
            await AssertExpr("<NEXT?>").InV3().DoesNotCompileAsync();
            await AssertExpr("<NEXT? 0 0>").InV3().DoesNotCompileAsync();
        }

        [TestMethod]
        public async System.Threading.Tasks.Task TestNEXTPAsync()
        {
            // V1 to V6
            // 2 to 2 operands
            await AssertExpr("<==? <NEXTP ,MYOBJECT 0> ,P?FOO>")
                .WithGlobal("<OBJECT MYOBJECT (FOO 123) (BAR 456)>")
                .GivesNumberAsync("1");
            await AssertExpr("<==? <NEXTP ,MYOBJECT ,P?FOO> ,P?BAR>")
                .WithGlobal("<OBJECT MYOBJECT (FOO 123) (BAR 456)>")
                .GivesNumberAsync("1");
            await AssertExpr("<==? <NEXTP ,MYOBJECT ,P?BAR> 0>")
                .WithGlobal("<OBJECT MYOBJECT (FOO 123) (BAR 456)>")
                .GivesNumberAsync("1");
        }

        [TestMethod]
        public async System.Threading.Tasks.Task TestNEXTP_ErrorAsync()
        {
            // V1 to V6
            // 2 to 2 operands
            await AssertExpr("<NEXTP>").InV3().DoesNotCompileAsync();
            await AssertExpr("<NEXTP 0>").InV3().DoesNotCompileAsync();
            await AssertExpr("<NEXTP 0 0 0>").InV3().DoesNotCompileAsync();
        }

        // NOOP is not supported in ZIL

        [TestMethod]
        public async System.Threading.Tasks.Task TestNOTAsync()
        {
            await AssertExpr("<NOT 0>").GivesNumberAsync("1");
            await AssertExpr("<NOT 123>").GivesNumberAsync("0");

            await AssertExpr("<NOT ,FOO>")
                .WithGlobal("<GLOBAL FOO 0>")
                .GivesNumberAsync("1");
            await AssertExpr("<NOT ,FOO>")
                .WithGlobal("<GLOBAL FOO 123>")
                .GivesNumberAsync("0");

            await AssertRoutine("", "<COND (<NOT 0> <PRINTI \"hello\">) (T <PRINTI \"goodbye\">)>")
                .OutputsAsync("hello");
            await AssertRoutine("", "<COND (<NOT 123> <PRINTI \"hello\">) (T <PRINTI \"goodbye\">)>")
                .OutputsAsync("goodbye");

            await AssertRoutine("", "<COND (<NOT ,FOO> <PRINTI \"hello\">) (T <PRINTI \"goodbye\">)>")
                .WithGlobal("<GLOBAL FOO 0>")
                .OutputsAsync("hello");
            await AssertRoutine("", "<COND (<NOT ,FOO> <PRINTI \"hello\">) (T <PRINTI \"goodbye\">)>")
                .WithGlobal("<GLOBAL FOO 123>")
                .OutputsAsync("goodbye");
        }

        [TestMethod]
        public async System.Threading.Tasks.Task TestNOT_ErrorAsync()
        {
            await AssertExpr("<NOT>").DoesNotCompileAsync();
            await AssertExpr("<NOT 0 0>").DoesNotCompileAsync();

            await AssertExpr("<COND (<NOT>)>").DoesNotCompileAsync();
            await AssertExpr("<COND (<NOT 0 0>)>").DoesNotCompileAsync();
        }

        [TestMethod]
        public async System.Threading.Tasks.Task TestORIGINAL_PAsync()
        {
            // V5 to V6
            // 0 to 0 operands
            await AssertExpr("<ORIGINAL?>").InV5().GivesNumberAsync("1");
        }

        [TestMethod]
        public async System.Threading.Tasks.Task TestORIGINAL_P_ErrorAsync()
        {
            // only exists in V5+
            await AssertExpr("<ORIGINAL?>").InV4().DoesNotCompileAsync();

            // V5 to V6
            // 0 to 0 operands
            await AssertExpr("<ORIGINAL? 0>").InV5().DoesNotCompileAsync();
        }

        [TestMethod]
        public void TestPICINF_V6()
        {
            // only exists in V6+
            Assert.Inconclusive();

            // V6 to V6
            // 0 to 4 operands
/*
            await AssertExpr("<PICINF>").InV6().Compiles();
            await AssertExpr("<PICINF 0>").InV6().Compiles();
            await AssertExpr("<PICINF 0 0>").InV6().Compiles();
            await AssertExpr("<PICINF 0 0 0>").InV6().Compiles();
            await AssertExpr("<PICINF 0 0 0 0>").InV6().Compiles();
            Assert.Inconclusive("This test was automatically generated.");
*/
        }

        [TestMethod]
        public async System.Threading.Tasks.Task TestPICINF_Error_V6Async()
        {
            // only exists in V6+
            await AssertExpr("<PICINF>").InV5().DoesNotCompileAsync();

            // V6 to V6
            // 0 to 4 operands
            await AssertExpr("<PICINF 0 0 0 0 0>").InV6().DoesNotCompileAsync();
            Assert.Inconclusive("This test was automatically generated.");
        }

        [TestMethod]
        public void TestPICSET_V6()
        {
            // only exists in V6+
            Assert.Inconclusive();

            // V6 to V6
            // 0 to 4 operands
/*
            await AssertExpr("<PICSET>").InV6().Compiles();
            await AssertExpr("<PICSET 0>").InV6().Compiles();
            await AssertExpr("<PICSET 0 0>").InV6().Compiles();
            await AssertExpr("<PICSET 0 0 0>").InV6().Compiles();
            await AssertExpr("<PICSET 0 0 0 0>").InV6().Compiles();
            Assert.Inconclusive("This test was automatically generated.");
*/
        }

        [TestMethod]
        public async System.Threading.Tasks.Task TestPICSET_Error_V6Async()
        {
            // only exists in V6+
            await AssertExpr("<PICSET>").InV5().DoesNotCompileAsync();

            // V6 to V6
            // 0 to 4 operands
            await AssertExpr("<PICSET 0 0 0 0 0>").InV6().DoesNotCompileAsync();
            Assert.Inconclusive("This test was automatically generated.");
        }

        // only the V6 version of POP is supported in ZIL

        [TestMethod]
        public async System.Threading.Tasks.Task TestPOP_V6Async()
        {
            // V6 to V6
            // 0 to 1 operands
            await AssertRoutine("\"AUX\" X", "<PUSH 123> <SET X <POP>> .X")
                .InV6()
                .GivesNumberAsync("123");

            await AssertExpr("<POP ,MY-STACK>")
                .WithGlobal("<GLOBAL MY-STACK <TABLE 3 0 0 0 123>>")
                .InV6()
                .GivesNumberAsync("123");
        }

        [TestMethod]
        public async System.Threading.Tasks.Task TestPOP_ErrorAsync()
        {
            // only exists in V6+
            await AssertExpr("<POP>").InV5().DoesNotCompileAsync();

            // V6 to V6
            // 0 to 1 operands
            await AssertExpr("<POP 0 0>").InV6().DoesNotCompileAsync();
        }

        [TestMethod]
        public async System.Threading.Tasks.Task TestPRINTAsync()
        {
            // V1 to V6
            // 1 to 1 operands
            await AssertExpr("<PRINT ,MESSAGE>")
                .InV3()
                .WithGlobal("<GLOBAL MESSAGE \"hello\">")
                .OutputsAsync("hello");
        }

        [TestMethod]
        public async System.Threading.Tasks.Task TestPRINT_ErrorAsync()
        {
            // V1 to V6
            // 1 to 1 operands
            await AssertExpr("<PRINT>").InV3().DoesNotCompileAsync();
            await AssertExpr("<PRINT 0 0>").InV3().DoesNotCompileAsync();
        }

        [TestMethod]
        public async System.Threading.Tasks.Task TestPRINTBAsync()
        {
            // V1 to V6
            // 1 to 1 operands
            await AssertExpr("<PRINTB <GETP ,MYOBJECT ,P?SYNONYM>>")
                .InV3()
                .WithGlobal("<OBJECT MYOBJECT (SYNONYM HELLO)>")
                .OutputsAsync("hello");
        }

        [TestMethod]
        public async System.Threading.Tasks.Task TestPRINTB_ErrorAsync()
        {
            // V1 to V6
            // 1 to 1 operands
            await AssertExpr("<PRINTB>").InV3().DoesNotCompileAsync();
            await AssertExpr("<PRINTB 0 0>").InV3().DoesNotCompileAsync();
        }

        [TestMethod]
        public async System.Threading.Tasks.Task TestPRINTCAsync()
        {
            // V1 to V6
            // 1 operand
            await AssertExpr("<PRINTC 65>").InV3().OutputsAsync("A");
        }

        [TestMethod]
        public async System.Threading.Tasks.Task TestPRINTC_ErrorAsync()
        {
            // V1 to V6
            // 1 operand
            await AssertExpr("<PRINTC>").InV3().DoesNotCompileAsync();
            await AssertExpr("<PRINTC 65 0>").InV3().DoesNotCompileAsync();
        }

        [TestMethod]
        public async System.Threading.Tasks.Task TestPRINTDAsync()
        {
            // V1 to V6
            // 1 to 1 operands
            await AssertExpr("<PRINTD ,MYOBJECT>")
                .WithGlobal("<OBJECT MYOBJECT (DESC \"pocket fisherman\")>")
                .OutputsAsync("pocket fisherman");
        }

        [TestMethod]
        public async System.Threading.Tasks.Task TestPRINTD_ErrorAsync()
        {
            // V1 to V6
            // 1 to 1 operands
            await AssertExpr("<PRINTD>").InV3().DoesNotCompileAsync();
            await AssertExpr("<PRINTD 0 0>").InV3().DoesNotCompileAsync();
        }

        [TestMethod]
        public void TestPRINTF_V6()
        {
            // only exists in V6+
            Assert.Inconclusive();

            // V6 to V6
            // 0 to 4 operands
/*
            await AssertExpr("<PRINTF>").InV6().Compiles();
            await AssertExpr("<PRINTF 0>").InV6().Compiles();
            await AssertExpr("<PRINTF 0 0>").InV6().Compiles();
            await AssertExpr("<PRINTF 0 0 0>").InV6().Compiles();
            await AssertExpr("<PRINTF 0 0 0 0>").InV6().Compiles();
            Assert.Inconclusive("This test was automatically generated.");
*/
        }

        [TestMethod]
        public async System.Threading.Tasks.Task TestPRINTF_Error_V6Async()
        {
            // only exists in V6+
            await AssertExpr("<PRINTF>").InV5().DoesNotCompileAsync();

            // V6 to V6
            // 0 to 4 operands
            await AssertExpr("<PRINTF 0 0 0 0 0>").InV6().DoesNotCompileAsync();
            Assert.Inconclusive("This test was automatically generated.");
        }

        [TestMethod]
        public async System.Threading.Tasks.Task TestPRINTIAsync()
        {
            // V1 to V6
            await AssertExpr("<PRINTI \"hello|world\">").OutputsAsync("hello\nworld");
            await AssertExpr("<PRINTI \"foo||\r\n\r\n    BAR\">").OutputsAsync("foo\n\n     BAR");
        }

        [TestMethod]
        public async System.Threading.Tasks.Task TestPRINTI_ErrorAsync()
        {
            // V1 to V6
            await AssertExpr("<PRINTI>").DoesNotCompileAsync();
            await AssertExpr("<PRINTI \"foo\" \"bar\">").DoesNotCompileAsync();
            await AssertExpr("<PRINTI 123>").DoesNotCompileAsync();
        }

        [TestMethod]
        public async System.Threading.Tasks.Task TestPRINTNAsync()
        {
            // V1 to V6
            // 1 to 1 operands
            await AssertExpr("<PRINTN 0>").InV3().OutputsAsync("0");
            await AssertExpr("<PRINTN -12345>").InV3().OutputsAsync("-12345");
        }

        [TestMethod]
        public async System.Threading.Tasks.Task TestPRINTN_ErrorAsync()
        {
            // V1 to V6
            // 1 to 1 operands
            await AssertExpr("<PRINTN>").InV3().DoesNotCompileAsync();
            await AssertExpr("<PRINTN 0 0>").InV3().DoesNotCompileAsync();
        }

        [TestMethod]
        public async System.Threading.Tasks.Task TestPRINTRAsync()
        {
            // V1 to V6
            await AssertRoutine("", "<PRINTR \"hello|world\">").OutputsAsync("hello\nworld\n");
        }

        [TestMethod]
        public async System.Threading.Tasks.Task TestPRINTR_ErrorAsync()
        {
            // V1 to V6
            await AssertExpr("<PRINTR>").DoesNotCompileAsync();
            await AssertExpr("<PRINTR \"foo\" \"bar\">").DoesNotCompileAsync();
            await AssertExpr("<PRINTR 123>").DoesNotCompileAsync();
        }

        [TestMethod]
        public async System.Threading.Tasks.Task TestPRINTTAsync()
        {
            // V5 to V6
            // 2 to 4 operands
            await AssertExpr("<PRINTT ,MYTEXT 6>")
                .InV5()
                .WithGlobal("<GLOBAL MYTEXT <TABLE (STRING) \"hansprestige\">>")
                .OutputsAsync($"hanspr{System.Environment.NewLine}");

            await AssertExpr("<PRINTT ,MYTEXT 4 3>")
                .InV5()
                .WithGlobal("<GLOBAL MYTEXT <TABLE (STRING) \"hansprestige\">>")
                .OutputsAsync($"hans{System.Environment.NewLine}pres{System.Environment.NewLine}tige{System.Environment.NewLine}");

            await AssertExpr("<PRINTT ,MYTEXT 3 3 1>")
                .InV5()
                .WithGlobal("<GLOBAL MYTEXT <TABLE (STRING) \"hansprestige\">>")
                .OutputsAsync($"han{System.Environment.NewLine}pre{System.Environment.NewLine}tig{System.Environment.NewLine}");
        }

        [TestMethod]
        public async System.Threading.Tasks.Task TestPRINTT_ErrorAsync()
        {
            // only exists in V5+
            await AssertExpr("<PRINTT>").InV4().DoesNotCompileAsync();

            // V5 to V6
            // 2 to 4 operands
            await AssertExpr("<PRINTT>").InV5().DoesNotCompileAsync();
            await AssertExpr("<PRINTT 0>").InV5().DoesNotCompileAsync();
            await AssertExpr("<PRINTT 0 0 0 0 0>").InV5().DoesNotCompileAsync();
        }

        [TestMethod]
        public async System.Threading.Tasks.Task TestPRINTUAsync()
        {
            // V5 to V6
            // 1 operand
            await AssertExpr("<PRINTU 65>").InV5().CompilesAsync();
        }

        [TestMethod]
        public async System.Threading.Tasks.Task TestPRINTU_ErrorAsync()
        {
            // only exists in V5+
            await AssertExpr("<PRINTU>").InV4().DoesNotCompileAsync();

            // V5 to V6
            // 1 operand
            await AssertExpr("<PRINTU>").InV5().DoesNotCompileAsync();
            await AssertExpr("<PRINTU 0 0>").InV5().DoesNotCompileAsync();
        }

        [TestMethod]
        public async System.Threading.Tasks.Task TestPTSIZEAsync()
        {
            // V1 to V6
            // 1 to 1 operands
            await AssertExpr("<PTSIZE <GETPT ,MYOBJECT ,P?FOO>>")
                .WithGlobal("<OBJECT MYOBJECT (FOO 1 2 3)>")
                .GivesNumberAsync("6");
        }

        [TestMethod]
        public async System.Threading.Tasks.Task TestPTSIZE_ErrorAsync()
        {
            // V1 to V6
            // 1 to 1 operands
            await AssertExpr("<PTSIZE>").InV3().DoesNotCompileAsync();
            await AssertExpr("<PTSIZE 0 0>").InV3().DoesNotCompileAsync();
        }

        [TestMethod]
        public async System.Threading.Tasks.Task TestPUSHAsync()
        {
            // V1 to V6
            // 1 operand
            await AssertExpr("<PUSH 1234>").InV3().CompilesAsync();
        }

        [TestMethod]
        public async System.Threading.Tasks.Task TestPUSH_ErrorAsync()
        {
            // V1 to V6
            // 1 operand
            await AssertExpr("<PUSH>").InV3().DoesNotCompileAsync();
            await AssertExpr("<PUSH 0 0>").InV3().DoesNotCompileAsync();
        }

        [TestMethod]
        public async System.Threading.Tasks.Task TestPUTAsync()
        {
            // V1 to V6
            // 3 to 3 operands
            await AssertExpr("<PUT 0 0 0>").InV3().CompilesAsync();
        }

        [TestMethod]
        public async System.Threading.Tasks.Task TestPUT_ErrorAsync()
        {
            // V1 to V6
            // 3 to 3 operands
            await AssertExpr("<PUT 0 0>").InV3().DoesNotCompileAsync();
            await AssertExpr("<PUT 0 0 0 0>").InV3().DoesNotCompileAsync();
        }

        [TestMethod]
        public async System.Threading.Tasks.Task TestPUTBAsync()
        {
            // V1 to V6
            // 3 to 3 operands
            await AssertExpr("<PUTB 0 0 0>").InV3().CompilesAsync();
        }

        [TestMethod]
        public async System.Threading.Tasks.Task TestPUTB_ErrorAsync()
        {
            // V1 to V6
            // 3 to 3 operands
            await AssertExpr("<PUTB 0 0>").InV3().DoesNotCompileAsync();
            await AssertExpr("<PUTB 0 0 0 0>").InV3().DoesNotCompileAsync();
        }

        [TestMethod]
        public async System.Threading.Tasks.Task TestPUTPAsync()
        {
            // V1 to V6
            // 3 to 3 operands
            await AssertExpr("<PUTP 0 0 0>").InV3().CompilesAsync();
        }

        [TestMethod]
        public async System.Threading.Tasks.Task TestPUTP_ErrorAsync()
        {
            // V1 to V6
            // 3 to 3 operands
            await AssertExpr("<PUTP 0 0>").InV3().DoesNotCompileAsync();
            await AssertExpr("<PUTP 0 0 0 0>").InV3().DoesNotCompileAsync();
        }

        [TestMethod]
        public async System.Threading.Tasks.Task TestQUITAsync()
        {
            // V1 to V6
            // 0 to 0 operands
            await AssertExpr("<QUIT> <PRINTI \"foo\"> <CRLF>").InV3().OutputsAsync("");
        }

        [TestMethod]
        public async System.Threading.Tasks.Task TestQUIT_ErrorAsync()
        {
            // V1 to V6
            // 0 to 0 operands
            await AssertExpr("<QUIT 0>").InV3().DoesNotCompileAsync();
        }

        [TestMethod]
        public async System.Threading.Tasks.Task TestRANDOMAsync()
        {
            // V1 to V6
            // 1 to 1 operands
            await AssertExpr("<RANDOM 14>").InV3().CompilesAsync();
        }

        [TestMethod]
        public async System.Threading.Tasks.Task TestRANDOM_ErrorAsync()
        {
            // V1 to V6
            // 1 to 1 operands
            await AssertExpr("<RANDOM>").InV3().DoesNotCompileAsync();
            await AssertExpr("<RANDOM 0 0>").InV3().DoesNotCompileAsync();
        }

        [TestMethod]
        public async System.Threading.Tasks.Task TestREADAsync()
        {
            // V1 to V3
            // 2 operands
            // ,HERE must point to a valid object for status line purposes
            await AssertRoutine("", "<READ ,TEXTBUF ,LEXBUF> <PRINTC <GETB ,TEXTBUF 2>> <PRINTB <GET ,LEXBUF 1>>")
                .InV3()
                .WithGlobal("<GLOBAL TEXTBUF <ITABLE 50 (BYTE LENGTH) 0>>")
                .WithGlobal("<GLOBAL LEXBUF <ITABLE 1 (LEXV) 0 0 0>>")
                .WithGlobal("<OBJECT CAT (SYNONYM CAT)>")
                .WithGlobal("<GLOBAL HERE CAT>")
                .WithInput("cat")
                .OutputsAsync("acat");
            // V4
            // 2 to 4 operands
            await AssertExpr("<READ 0 0>").InV4().CompilesAsync();
            await AssertExpr("<READ 0 0 0>").InV4().CompilesAsync();
            await AssertExpr("<READ 0 0 0 0>").InV4().CompilesAsync();
            // V5 to V6
            // 1 to 4 operands
            await AssertRoutine("", "<PRINTN <READ ,TEXTBUF ,LEXBUF>> <PRINTC <GETB ,TEXTBUF 2>> <PRINTB <GET ,LEXBUF 1>>")
                .InV5()
                .WithGlobal("<GLOBAL TEXTBUF <ITABLE 50 (BYTE LENGTH) 0>>")
                .WithGlobal("<GLOBAL LEXBUF <ITABLE 1 (LEXV) 0 0 0>>")
                .WithGlobal("<OBJECT CAT (SYNONYM CAT)>")
                .WithInput("cat")
                .OutputsAsync("13ccat");
            await AssertExpr("<READ 0>").InV5().CompilesAsync();
            await AssertExpr("<READ 0 0 0>").InV5().CompilesAsync();
            await AssertExpr("<READ 0 0 0 0>").InV5().CompilesAsync();
        }

        [TestMethod]
        public async System.Threading.Tasks.Task TestREAD_ErrorAsync()
        {
            // V1 to V3
            // 2 operands
            await AssertExpr("<READ>").InV3().DoesNotCompileAsync();
            await AssertExpr("<READ 0>").InV3().DoesNotCompileAsync();
            await AssertExpr("<READ 0 0 0>").InV3().DoesNotCompileAsync();
            // V4
            // 2 to 4 operands
            await AssertExpr("<READ>").InV4().DoesNotCompileAsync();
            await AssertExpr("<READ 0>").InV4().DoesNotCompileAsync();
            await AssertExpr("<READ 0 0 0 0 0>").InV4().DoesNotCompileAsync();
            // V5 to V6
            // 1 to 4 operands
            await AssertExpr("<READ>").InV5().DoesNotCompileAsync();
            await AssertExpr("<READ 0 0 0 0 0>").InV5().DoesNotCompileAsync();
        }

        [TestMethod]
        public async System.Threading.Tasks.Task TestREMOVEAsync()
        {
            // V1 to V6
            // 1 to 1 operands
            await AssertRoutine("", "<REMOVE ,CAT> <LOC ,CAT>")
                .WithGlobal("<OBJECT CAT (LOC HAT)>")
                .WithGlobal("<OBJECT HAT>")
                .GivesNumberAsync("0");
        }

        [TestMethod]
        public async System.Threading.Tasks.Task TestREMOVE_ErrorAsync()
        {
            // V1 to V6
            // 1 to 1 operands
            await AssertExpr("<REMOVE>").InV3().DoesNotCompileAsync();
            await AssertExpr("<REMOVE 0 0>").InV3().DoesNotCompileAsync();
        }

        [TestMethod]
        public async System.Threading.Tasks.Task TestRESTARTAsync()
        {
            // V1 to V6
            // 0 to 0 operands
            await AssertExpr("<RESTART>").InV3().CompilesAsync();
        }

        [TestMethod]
        public async System.Threading.Tasks.Task TestRESTART_ErrorAsync()
        {
            // V1 to V6
            // 0 to 0 operands
            await AssertExpr("<RESTART 0>").InV3().DoesNotCompileAsync();
        }

        [TestMethod]
        public async System.Threading.Tasks.Task TestRESTOREAsync()
        {
            // V1 to V3
            // 0 to 0 operands
            await AssertExpr("<RESTORE>").InV3().CompilesAsync();
            // V4 to V4
            // 0 to 0 operands
            await AssertExpr("<RESTORE>").InV4().CompilesAsync();
            // V5 to V6
            // 0 or(!) 3 operands
            await AssertExpr("<RESTORE>").InV5().CompilesAsync();
            await AssertExpr("<RESTORE 0 0 0>").InV5().CompilesAsync();
        }

        [TestMethod]
        public async System.Threading.Tasks.Task TestRESTORE_ErrorAsync()
        {
            // V1 to V3
            // 0 to 0 operands
            await AssertExpr("<RESTORE 0>").InV3().DoesNotCompileAsync();
            // V4 to V4
            // 0 to 0 operands
            await AssertExpr("<RESTORE 0>").InV4().DoesNotCompileAsync();
            // V5 to V6
            // 0 or(!) 3 operands
            await AssertExpr("<RESTORE 0>").InV5().DoesNotCompileAsync();
            await AssertExpr("<RESTORE 0 0>").InV5().DoesNotCompileAsync();
            await AssertExpr("<RESTORE 0 0 0 0>").InV5().DoesNotCompileAsync();
        }

        [TestMethod]
        public async System.Threading.Tasks.Task TestRETURNAsync()
        {
            // NOTE: <RETURN> is more than just the Z-machine opcode. it also returns from <REPEAT>, and with no argument it returns true.

            // V1 to V6
            // 0 to 1 operands
            await AssertRoutine("", "<RETURN>").InV3().GivesNumberAsync("1");
            await AssertRoutine("", "<RETURN 41>").InV3().GivesNumberAsync("41");
        }

        [TestMethod]
        public async System.Threading.Tasks.Task TestRETURN_FromBlockAsync()
        {
            await AssertRoutine("", "<* 2 <PROG () <RETURN 41>>>").GivesNumberAsync("82");
            await AssertRoutine("", "<PROG () <RETURN>> 42").GivesNumberAsync("42");
        }

        [TestMethod]
        public async System.Threading.Tasks.Task TestRETURN_ErrorAsync()
        {
            // V1 to V6
            // 0 to 1 operands
            await AssertExpr("<RETURN 0 0>").InV3().DoesNotCompileAsync();
        }

        [TestMethod]
        public async System.Threading.Tasks.Task TestRFALSEAsync()
        {
            // V1 to V6
            // 0 to 0 operands
            await AssertRoutine("", "<RFALSE>").GivesNumberAsync("0");
        }

        [TestMethod]
        public async System.Threading.Tasks.Task TestRFALSE_ErrorAsync()
        {
            // V1 to V6
            // 0 to 0 operands
            await AssertExpr("<RFALSE 0>").InV3().DoesNotCompileAsync();
            await AssertExpr("<RFALSE 0 0>").InV3().DoesNotCompileAsync();
        }

        [TestMethod]
        public async System.Threading.Tasks.Task TestRSTACKAsync()
        {
            // V1 to V6
            // 0 to 0 operands
            await AssertRoutine("", "<PUSH 1234> <RSTACK>").GivesNumberAsync("1234");
        }

        [TestMethod]
        public async System.Threading.Tasks.Task TestRSTACK_ErrorAsync()
        {
            // V1 to V6
            // 0 to 0 operands
            await AssertExpr("<RSTACK 0>").InV3().DoesNotCompileAsync();
        }

        [TestMethod]
        public async System.Threading.Tasks.Task TestRTRUEAsync()
        {
            // V1 to V6
            // 0 to 0 operands
            await AssertRoutine("", "<RTRUE>").GivesNumberAsync("1");
        }

        [TestMethod]
        public async System.Threading.Tasks.Task TestRTRUE_ErrorAsync()
        {
            // V1 to V6
            // 0 to 0 operands
            await AssertExpr("<RTRUE 0>").InV3().DoesNotCompileAsync();
        }

        [TestMethod]
        public async System.Threading.Tasks.Task TestSAVEAsync()
        {
            // V1 to V3
            // 0 to 0 operands
            await AssertExpr("<SAVE>").InV3().CompilesAsync();
            // V4 to V4
            // 0 to 0 operands
            await AssertExpr("<SAVE>").InV4().CompilesAsync();
            // V5 to V6
            // 0 or(!) 3 operands
            await AssertExpr("<SAVE>").InV5().CompilesAsync();
            await AssertExpr("<SAVE 0 0 0>").InV5().CompilesAsync();
        }

        [TestMethod]
        public async System.Threading.Tasks.Task TestSAVE_ErrorAsync()
        {
            // V1 to V3
            // 0 to 0 operands
            await AssertExpr("<SAVE 0>").InV3().DoesNotCompileAsync();
            // V4 to V4
            // 0 to 0 operands
            await AssertExpr("<SAVE 0>").InV4().DoesNotCompileAsync();
            // V5 to V6
            // 0 or(!) 3 operands
            await AssertExpr("<SAVE 0>").InV5().DoesNotCompileAsync();
            await AssertExpr("<SAVE 0 0>").InV5().DoesNotCompileAsync();
            await AssertExpr("<SAVE 0 0 0 0>").InV5().DoesNotCompileAsync();
        }

        [TestMethod]
        public async System.Threading.Tasks.Task TestSCREENAsync()
        {
            // V3 to V6
            // 1 operand
            await AssertExpr("<SCREEN 0>").InV3().CompilesAsync();
        }

        [TestMethod]
        public async System.Threading.Tasks.Task TestSCREEN_ErrorAsync()
        {
            // V3 to V6
            // 1 operand
            await AssertExpr("<SCREEN>").InV3().DoesNotCompileAsync();
            await AssertExpr("<SCREEN 0 0>").InV3().DoesNotCompileAsync();
        }

        [TestMethod]
        public void TestSCROLL_V6()
        {
            // only exists in V6+
            Assert.Inconclusive();

            // V6 to V6
            // 0 to 4 operands
/*
            await AssertExpr("<SCROLL>").InV6().Compiles();
            await AssertExpr("<SCROLL 0>").InV6().Compiles();
            await AssertExpr("<SCROLL 0 0>").InV6().Compiles();
            await AssertExpr("<SCROLL 0 0 0>").InV6().Compiles();
            await AssertExpr("<SCROLL 0 0 0 0>").InV6().Compiles();
            Assert.Inconclusive("This test was automatically generated.");
*/
        }

        [TestMethod]
        public async System.Threading.Tasks.Task TestSCROLL_Error_V6Async()
        {
            // only exists in V6+
            await AssertExpr("<SCROLL>").InV5().DoesNotCompileAsync();

            // V6 to V6
            // 0 to 4 operands
            await AssertExpr("<SCROLL 0 0 0 0 0>").InV6().DoesNotCompileAsync();
            Assert.Inconclusive("This test was automatically generated.");
        }

        [TestMethod]
        public async System.Threading.Tasks.Task TestSETAsync()
        {
            // V1 to V6
            await AssertRoutine("\"AUX\" FOO", "<SET FOO 111> .FOO")
                .GivesNumberAsync("111");
            await AssertRoutine("", "<SET FOO 111> ,FOO")
                .WithGlobal("<GLOBAL FOO 0>")
                .GivesNumberAsync("111");

            // value version
            await AssertRoutine("\"AUX\" FOO", "<PRINTN <SET FOO 111>>")
                .OutputsAsync("111");

            // void version
            await AssertRoutine("\"AUX\" FOO", "<SET 1 111> <PRINTN .FOO>")
                .OutputsAsync("111");
            await AssertRoutine("\"AUX\" BAR", "<SET <ONE> <ONE-ELEVEN>> <PRINTN .BAR>")
                .WithGlobal("<ROUTINE ONE () <PRINTI \"ONE.\"> 1>")
                .WithGlobal("<ROUTINE ONE-ELEVEN () <PRINTI \"ONE-ELEVEN.\"> 111>")
                .OutputsAsync("ONE.ONE-ELEVEN.111");

            // alias: SETG
            await AssertRoutine("\"AUX\" FOO", "<SETG FOO 111> .FOO")
                .GivesNumberAsync("111");
            await AssertRoutine("", "<SETG FOO 111> ,FOO")
                .WithGlobal("<GLOBAL FOO 0>")
                .GivesNumberAsync("111");
        }

        [TestMethod]
        public async System.Threading.Tasks.Task TestSET_QuirksAsync()
        {
            /* SET and SETG have different VariableScopeQuirks behavior:
             * 
             * SETG treats a ,GVAL as its first argument as a variable name,
             * but treats an .LVAL as an expression: <SETG ,FOO 1> sets the global FOO,
             * whereas <SETG .FOO 1> sets the variable whose index is in .FOO.
             * 
             * Likewise, SET treats an .LVAL as a variable name but a ,GVAL as an
             * expression: <SET .FOO 1> sets the local FOO, and <SET ,FOO 1> sets the
             * variable whose index is in FOO. */

            // void context
            await AssertRoutine("\"AUX\" (FOO 16)", "<SET .FOO 123> <PRINTN .FOO> <CRLF> <PRINTN ,MYGLOBAL>")
                .WithGlobal("<GLOBAL MYGLOBAL 1>")
                .OutputsAsync("123\n1");

            await AssertRoutine("\"AUX\" (FOO 16)", "<SETG ,MYGLOBAL 123> <PRINTN .FOO> <CRLF> <PRINTN ,MYGLOBAL>")
                .WithGlobal("<GLOBAL MYGLOBAL 1>")
                .OutputsAsync("16\n123");

            await AssertRoutine("\"AUX\" (FOO 16)", "<SETG .FOO 123> <PRINTN .FOO> <CRLF> <PRINTN ,MYGLOBAL>")
                .WithGlobal("<GLOBAL MYGLOBAL 1>")
                .OutputsAsync("16\n123");

            await AssertRoutine("\"AUX\" (FOO 16)", "<SET ,MYGLOBAL 123> <PRINTN .FOO> <CRLF> <PRINTN ,MYGLOBAL>")
                .WithGlobal("<GLOBAL MYGLOBAL 1>")
                .OutputsAsync("123\n1");

            // value context (more limited)
            await AssertRoutine("\"AUX\" (FOO 16)", "<PRINTN <SET .FOO 123>> <CRLF> <PRINTN ,MYGLOBAL>")
                .WithGlobal("<GLOBAL MYGLOBAL 1>")
                .OutputsAsync("123\n1");

            await AssertRoutine("\"AUX\" (FOO 16)", "<PRINTN <SETG ,MYGLOBAL 123>> <CRLF> <PRINTN .FOO>")
                .WithGlobal("<GLOBAL MYGLOBAL 1>")
                .OutputsAsync("123\n16");
        }

        [TestMethod]
        public async System.Threading.Tasks.Task TestSET_ErrorAsync()
        {
            // V1 to V6
            await AssertExpr("<SET>").DoesNotCompileAsync();
            await AssertRoutine("X", "<SET X>").DoesNotCompileAsync();
            await AssertExpr("<SET 1 2>").DoesNotCompileAsync();
            await AssertRoutine("X", "<SET Y 1>").DoesNotCompileAsync();

            // if the first arg is a bare atom, it must be a variable
            await AssertRoutine("", "<SETG FOO 1> T")
                .WithGlobal("<CONSTANT FOO <TABLE 1 2 3>>")
                .DoesNotCompileAsync();
        }

        [TestMethod]
        public async System.Threading.Tasks.Task TestSHIFTAsync()
        {
            // V5 to V6
            // 2 to 2 operands
            await AssertExpr("<SHIFT 1 3>").InV5().GivesNumberAsync("8");
            await AssertExpr("<SHIFT 16 -3>").InV5().GivesNumberAsync("2");
            await AssertExpr("<SHIFT 1 16>").InV5().GivesNumberAsync("0");
            await AssertExpr("<SHIFT 1 15>").InV5().GivesNumberAsync("-32768");
            await AssertExpr("<SHIFT 16384 -14>").InV5().GivesNumberAsync("1");
        }

        [TestMethod]
        public async System.Threading.Tasks.Task TestSHIFT_ErrorAsync()
        {
            // only exists in V5+
            await AssertExpr("<SHIFT>").InV4().DoesNotCompileAsync();

            // V5 to V6
            // 2 to 2 operands
            await AssertExpr("<SHIFT 0>").InV5().DoesNotCompileAsync();
            await AssertExpr("<SHIFT 0 0 0>").InV5().DoesNotCompileAsync();
        }

        [TestMethod]
        public async System.Threading.Tasks.Task TestSOUNDAsync()
        {
            // V3 to V4
            // 1 to 3 operands
            await AssertExpr("<SOUND 0>").InV3().CompilesAsync();
            await AssertExpr("<SOUND 0 0>").InV3().CompilesAsync();
            await AssertExpr("<SOUND 0 0 0>").InV3().CompilesAsync();
            // V5 to V6
            // 1 to 4 operands
            await AssertExpr("<SOUND 0>").InV5().CompilesAsync();
            await AssertExpr("<SOUND 0 0>").InV5().CompilesAsync();
            await AssertExpr("<SOUND 0 0 0>").InV5().CompilesAsync();
            await AssertExpr("<SOUND 0 0 0 0>").InV5().CompilesAsync();
        }

        [TestMethod]
        public async System.Threading.Tasks.Task TestSOUND_ErrorAsync()
        {
            // V3 to V4
            // 1 to 3 operands
            await AssertExpr("<SOUND>").InV3().DoesNotCompileAsync();
            await AssertExpr("<SOUND 0 0 0 0>").InV3().DoesNotCompileAsync();
            // V5 to V6
            // 1 to 4 operands
            await AssertExpr("<SOUND>").InV5().DoesNotCompileAsync();
            await AssertExpr("<SOUND 0 0 0 0 0>").InV5().DoesNotCompileAsync();
        }

        [TestMethod]
        public async System.Threading.Tasks.Task TestSPLITAsync()
        {
            // V3 to V6
            // 1 to 1 operands
            await AssertExpr("<SPLIT 1>").InV3().CompilesAsync();
        }

        [TestMethod]
        public async System.Threading.Tasks.Task TestSPLIT_ErrorAsync()
        {
            // V3 to V6
            // 1 to 1 operands
            await AssertExpr("<SPLIT>").InV3().DoesNotCompileAsync();
            await AssertExpr("<SPLIT 0 0>").InV3().DoesNotCompileAsync();
        }

        [TestMethod]
        public async System.Threading.Tasks.Task TestSUBAsync()
        {
            await AssertExpr("<- 1 2>").GivesNumberAsync("-1");
            await AssertExpr("<- 1 -2>").GivesNumberAsync("3");
            await AssertExpr("<- -32768 1>").GivesNumberAsync("32767");
            await AssertExpr("<- 32767 -1>").GivesNumberAsync("-32768");

            // a single argument is unary negation
            await AssertExpr("<- 123>").GivesNumberAsync("-123");
            await AssertExpr("<- -200>").GivesNumberAsync("200");
            await AssertExpr("<- 0>").GivesNumberAsync("0");

            await AssertExpr("<->").GivesNumberAsync("0");
            await AssertExpr("<- 5>").GivesNumberAsync("-5");
            await AssertExpr("<- 1 2 3>").GivesNumberAsync("-4");
            await AssertExpr("<- 1 2 3 4>").GivesNumberAsync("-8");
            await AssertExpr("<- 1 2 3 4 5>").GivesNumberAsync("-13");

            // alias
            await AssertExpr("<SUB 1 2>").GivesNumberAsync("-1");
        }

        [TestMethod]
        public async System.Threading.Tasks.Task TestSUB_BACKAsync()
        {
            // alias where 2nd operand defaults to 1
            await AssertExpr("<BACK 1>").GivesNumberAsync("0");
            await AssertExpr("<BACK 1 2>").GivesNumberAsync("-1");
        }

        [TestMethod]
        public async System.Threading.Tasks.Task TestTHROWAsync()
        {
            // V5 to V6
            // 2 to 2 operands
            await AssertRoutine("\"AUX\" X", "<SET X <CATCH>> <THROWER .X> 123")
                .InV5()
                .WithGlobal("<ROUTINE THROWER (F) <THROW 456 .F>>")
                .GivesNumberAsync("456");
        }

        [TestMethod]
        public async System.Threading.Tasks.Task TestTHROW_ErrorAsync()
        {
            // only exists in V5+
            await AssertExpr("<THROW 0 0>").InV4().DoesNotCompileAsync();

            // V5 to V6
            // 2 to 2 operands
            await AssertExpr("<THROW>").InV5().DoesNotCompileAsync();
            await AssertExpr("<THROW 0>").InV5().DoesNotCompileAsync();
            await AssertExpr("<THROW 0 0 0>").InV5().DoesNotCompileAsync();
        }

        [TestMethod]
        public async System.Threading.Tasks.Task TestUSLAsync()
        {
            // V1 to V3
            // 0 to 0 operands
            await AssertExpr("<USL>").InV3().CompilesAsync();
        }

        [TestMethod]
        public async System.Threading.Tasks.Task TestUSL_ErrorAsync()
        {
            // V1 to V3
            // 0 to 0 operands
            await AssertExpr("<USL 0>").InV3().DoesNotCompileAsync();

            await AssertExpr("<USL>").InV4().DoesNotCompileAsync();
        }

        [TestMethod]
        public async System.Threading.Tasks.Task TestVALUEAsync()
        {
            // V1 to V6
            await AssertRoutine("\"AUX\" (X 123)", "<VALUE X>").GivesNumberAsync("123");
            await AssertExpr("<VALUE G>")
                .WithGlobal("<GLOBAL G 123>")
                .GivesNumberAsync("123");
            await AssertRoutine("", "<PUSH 1234> <VALUE 0>").GivesNumberAsync("1234");
        }

        [TestMethod]
        public async System.Threading.Tasks.Task TestVALUE_ErrorAsync()
        {
            // V1 to V6
            await AssertExpr("<VALUE>").DoesNotCompileAsync();
            await AssertExpr("<VALUE 0 0>").DoesNotCompileAsync();
            await AssertExpr("<VALUE ASDF>").DoesNotCompileAsync();
        }

        [TestMethod]
        public async System.Threading.Tasks.Task TestVERIFYAsync()
        {
            // V3 to V6
            // 0 to 0 operands
            await AssertExpr("<VERIFY>").InV3().GivesNumberAsync("1");
        }

        [TestMethod]
        public async System.Threading.Tasks.Task TestVERIFY_ErrorAsync()
        {
            // V3 to V6
            // 0 to 0 operands
            await AssertExpr("<VERIFY 0>").InV3().DoesNotCompileAsync();
        }

        [TestMethod]
        public void TestWINATTR_V6()
        {
            // only exists in V6+
            Assert.Inconclusive();

            // V6 to V6
            // 0 to 4 operands
/*
            await AssertExpr("<WINATTR>").InV6().Compiles();
            await AssertExpr("<WINATTR 0>").InV6().Compiles();
            await AssertExpr("<WINATTR 0 0>").InV6().Compiles();
            await AssertExpr("<WINATTR 0 0 0>").InV6().Compiles();
            await AssertExpr("<WINATTR 0 0 0 0>").InV6().Compiles();
            Assert.Inconclusive("This test was automatically generated.");
*/
        }

        [TestMethod]
        public async System.Threading.Tasks.Task TestWINATTR_Error_V6Async()
        {
            // only exists in V6+
            await AssertExpr("<WINATTR>").InV5().DoesNotCompileAsync();

            // V6 to V6
            // 0 to 4 operands
            await AssertExpr("<WINATTR 0 0 0 0 0>").InV6().DoesNotCompileAsync();
            Assert.Inconclusive("This test was automatically generated.");
        }

        [TestMethod]
        public void TestWINGET_V6()
        {
            // only exists in V6+
            Assert.Inconclusive();

            // V6 to V6
            // 0 to 4 operands
/*
            await AssertExpr("<WINGET>").InV6().Compiles();
            await AssertExpr("<WINGET 0>").InV6().Compiles();
            await AssertExpr("<WINGET 0 0>").InV6().Compiles();
            await AssertExpr("<WINGET 0 0 0>").InV6().Compiles();
            await AssertExpr("<WINGET 0 0 0 0>").InV6().Compiles();
            Assert.Inconclusive("This test was automatically generated.");
*/
        }

        [TestMethod]
        public async System.Threading.Tasks.Task TestWINGET_Error_V6Async()
        {
            // only exists in V6+
            await AssertExpr("<WINGET>").InV5().DoesNotCompileAsync();

            // V6 to V6
            // 0 to 4 operands
            await AssertExpr("<WINGET 0 0 0 0 0>").InV6().DoesNotCompileAsync();
            Assert.Inconclusive("This test was automatically generated.");
        }

        [TestMethod]
        public void TestWINPOS_V6()
        {
            // only exists in V6+
            Assert.Inconclusive();

            // V6 to V6
            // 0 to 4 operands
/*
            await AssertExpr("<WINPOS>").InV6().Compiles();
            await AssertExpr("<WINPOS 0>").InV6().Compiles();
            await AssertExpr("<WINPOS 0 0>").InV6().Compiles();
            await AssertExpr("<WINPOS 0 0 0>").InV6().Compiles();
            await AssertExpr("<WINPOS 0 0 0 0>").InV6().Compiles();
            Assert.Inconclusive("This test was automatically generated.");
*/
        }

        [TestMethod]
        public async System.Threading.Tasks.Task TestWINPOS_Error_V6Async()
        {
            // only exists in V6+
            await AssertExpr("<WINPOS>").InV5().DoesNotCompileAsync();

            // V6 to V6
            // 0 to 4 operands
            await AssertExpr("<WINPOS 0 0 0 0 0>").InV6().DoesNotCompileAsync();
            Assert.Inconclusive("This test was automatically generated.");
        }

        [TestMethod]
        public void TestWINPUT_V6()
        {
            // only exists in V6+
            Assert.Inconclusive();

            // V6 to V6
            // 0 to 4 operands
/*
            await AssertExpr("<WINPUT>").InV6().Compiles();
            await AssertExpr("<WINPUT 0>").InV6().Compiles();
            await AssertExpr("<WINPUT 0 0>").InV6().Compiles();
            await AssertExpr("<WINPUT 0 0 0>").InV6().Compiles();
            await AssertExpr("<WINPUT 0 0 0 0>").InV6().Compiles();
            Assert.Inconclusive("This test was automatically generated.");
*/
        }

        [TestMethod]
        public async System.Threading.Tasks.Task TestWINPUT_Error_V6Async()
        {
            // only exists in V6+
            await AssertExpr("<WINPUT>").InV5().DoesNotCompileAsync();

            // V6 to V6
            // 0 to 4 operands
            await AssertExpr("<WINPUT 0 0 0 0 0>").InV6().DoesNotCompileAsync();
            Assert.Inconclusive("This test was automatically generated.");
        }

        [TestMethod]
        public void TestWINSIZE_V6()
        {
            // only exists in V6+
            Assert.Inconclusive();

            // V6 to V6
            // 0 to 4 operands
/*
            await AssertExpr("<WINSIZE>").InV6().Compiles();
            await AssertExpr("<WINSIZE 0>").InV6().Compiles();
            await AssertExpr("<WINSIZE 0 0>").InV6().Compiles();
            await AssertExpr("<WINSIZE 0 0 0>").InV6().Compiles();
            await AssertExpr("<WINSIZE 0 0 0 0>").InV6().Compiles();
            Assert.Inconclusive("This test was automatically generated.");
*/
        }

        [TestMethod]
        public async System.Threading.Tasks.Task TestWINSIZE_Error_V6Async()
        {
            // only exists in V6+
            await AssertExpr("<WINSIZE>").InV5().DoesNotCompileAsync();

            // V6 to V6
            // 0 to 4 operands
            await AssertExpr("<WINSIZE 0 0 0 0 0>").InV6().DoesNotCompileAsync();
            Assert.Inconclusive("This test was automatically generated.");
        }

        // XCALL is not supported in ZIL

        [TestMethod]
        public async System.Threading.Tasks.Task TestXPUSH_V6Async()
        {
            // V6 to V6
            // 2 to 2 operands
            await AssertExpr("<XPUSH 0 0>").InV6().CompilesAsync();
        }

        [TestMethod]
        public async System.Threading.Tasks.Task TestXPUSH_Error_V6Async()
        {
            // only exists in V6+
            await AssertExpr("<XPUSH>").InV5().DoesNotCompileAsync();

            // V6 to V6
            // 2 to 2 operands
            await AssertExpr("<XPUSH>").InV6().DoesNotCompileAsync();
            await AssertExpr("<XPUSH 0>").InV6().DoesNotCompileAsync();
            await AssertExpr("<XPUSH 0 0 0>").InV6().DoesNotCompileAsync();
        }

        [TestMethod]
        public async System.Threading.Tasks.Task TestZERO_PAsync()
        {
            // V1 to V6
            // 1 to 1 operands
            await AssertExpr("<ZERO? 0>").InV3().GivesNumberAsync("1");
            await AssertExpr("<ZERO? -5>").InV3().GivesNumberAsync("0");

            // alias
            await AssertExpr("<0? 0>").InV3().GivesNumberAsync("1");
        }

        [TestMethod]
        public async System.Threading.Tasks.Task TestZERO_P_ErrorAsync()
        {
            // V1 to V6
            // 1 to 1 operands
            await AssertExpr("<ZERO?>").InV3().DoesNotCompileAsync();
            await AssertExpr("<ZERO? 0 0>").InV3().DoesNotCompileAsync();
        }

        [TestMethod]
        public async System.Threading.Tasks.Task TestZWSTRAsync()
        {
            // V5 to V6
            // 4 operands
            await AssertRoutine("", "<ZWSTR ,SRCBUF 5 0 ,DSTBUF> <PRINTB ,DSTBUF>")
                .InV5()
                .WithGlobal("<GLOBAL SRCBUF <TABLE (STRING) \"hello\">>")
                .WithGlobal("<GLOBAL DSTBUF <TABLE 0 0 0>>")
                .OutputsAsync("hello");
        }

        [TestMethod]
        public async System.Threading.Tasks.Task TestZWSTR_ErrorAsync()
        {
            // only exists in V5+
            await AssertExpr("<ZWSTR>").InV4().DoesNotCompileAsync();

            // V5 to V6
            // 4 operands
            await AssertExpr("<ZWSTR>").InV5().DoesNotCompileAsync();
            await AssertExpr("<ZWSTR 0>").InV5().DoesNotCompileAsync();
            await AssertExpr("<ZWSTR 0 0>").InV5().DoesNotCompileAsync();
            await AssertExpr("<ZWSTR 0 0 0>").InV5().DoesNotCompileAsync();
            await AssertExpr("<ZWSTR 0 0 0 0 0>").InV5().DoesNotCompileAsync();
        }

        #endregion

        #region Not Exactly Opcodes

        [TestMethod]
        public async System.Threading.Tasks.Task TestLOWCOREAsync()
        {
            await AssertRoutine("", "<LOWCORE FLAGS>")
                .GeneratesCodeMatchingAsync(@"^\s*GET 0,8 >STACK\s*$");
            await AssertRoutine("", "<LOWCORE FLAGS 123>")
                .GeneratesCodeMatchingAsync(@"^\s*PUT 0,8,123");
        }

        [TestMethod]
        public async System.Threading.Tasks.Task TestLOWCORE_ExtensionAsync()
        {
            await AssertRoutine("\"AUX\" X", "<SET X <LOWCORE MSLOCY>> <LOWCORE MSETBL 12345>")
                .InV5()
                .ImpliesAsync(
                    "<T? <LOWCORE EXTAB>>",
                    "<G=? <GET <LOWCORE EXTAB> 0> 2>");
        }

        [TestMethod]
        public async System.Threading.Tasks.Task TestLOWCORE_SubFieldAsync()
        {
            await AssertRoutine("\"AUX\" X", "<SET X <LOWCORE (ZVERSION 1)>>")
                .CompilesAsync();

            await AssertRoutine("", "<LOWCORE (FLAGS 1) 123>")
                .CompilesAsync();
        }

        [TestMethod]
        public async System.Threading.Tasks.Task TestXORBAsync()
        {
            await AssertRoutine("X", "<XORB .X -1>")
                .WhenCalledWith("12345")
                .GivesNumberAsync("-12346");

            await AssertRoutine("X", "<XORB -1 .X>")
                .WhenCalledWith("32767")
                .GivesNumberAsync("-32768");
        }

        #endregion
    }
}
