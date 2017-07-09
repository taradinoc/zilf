/* Copyright 2010-2017 Jesse McGrew
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
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System.IO;
using System.Text;
using Zilf.Interpreter;
using Zilf.Interpreter.Values;
using Zilf.Language;
using Zilf.ZModel.Values;

namespace ZilfTests.Interpreter
{
    [TestClass]
    public class StructureTests
    {
        [TestMethod]
        public void TestMEMQ()
        {
            TestHelpers.EvalAndAssert("<MEMQ 5 '(3 4 5 6 7)>",
                new ZilList(new ZilObject[] {
                    new ZilFix(5),
                    new ZilFix(6),
                    new ZilFix(7)
                }));

            TestHelpers.EvalAndAssert("<MEMQ 5 '[3 4 5 6 7]>",
                new ZilVector(new ZilObject[] {
                            new ZilFix(5),
                            new ZilFix(6),
                            new ZilFix(7)
                        }));
        }

        [TestMethod]
        public void TestMEMBER()
        {
            TestHelpers.EvalAndAssert("<MEMBER '(5) '(3 4 (5) 6 7)>",
                new ZilList(new ZilObject[] {
                    new ZilList(new ZilObject[] { new ZilFix(5) }),
                    new ZilFix(6),
                    new ZilFix(7)
                }));

            TestHelpers.EvalAndAssert("<MEMBER '(5) '[3 4 (5) 6 7]>",
                new ZilVector(new ZilObject[] {
                            new ZilList(new ZilObject[] { new ZilFix(5) }),
                            new ZilFix(6),
                            new ZilFix(7)
                        }));
        }

        [TestMethod]
        public void TestILIST()
        {
            TestHelpers.EvalAndAssert("<ILIST 3 123>",
                new ZilList(new ZilObject[] {
                    new ZilFix(123),
                    new ZilFix(123),
                    new ZilFix(123)
                }));
        }

        [TestMethod]
        public void TestIVECTOR()
        {
            TestHelpers.EvalAndAssert("<IVECTOR 3 123>",
                new ZilVector(new ZilObject[] {
                    new ZilFix(123),
                    new ZilFix(123),
                    new ZilFix(123)
                }));
        }

        [TestMethod]
        public void TestDEFSTRUCT_NewObject()
        {
            var ctx = new Context();
            var pointAtom = ZilAtom.Parse("POINT", ctx);

            TestHelpers.EvalAndAssert(ctx, "<DEFSTRUCT POINT VECTOR (POINT-X FIX) (POINT-Y FIX)>",
                pointAtom);

            // construct new object
            TestHelpers.EvalAndAssert(ctx, "<MAKE-POINT 'POINT-X 123 'POINT-Y 456>",
                new ZilStructuredHash(pointAtom, PrimType.VECTOR, new ZilVector(new ZilFix(123), new ZilFix(456))));
            TestHelpers.EvalAndAssert(ctx, "<POINT-Y #POINT [234 567]>",
                new ZilFix(567));
        }

        [TestMethod]
        public void TestDEFSTRUCT_ExistingObject()
        {
            var ctx = new Context();
            var pointAtom = ZilAtom.Parse("POINT", ctx);

            TestHelpers.EvalAndAssert(ctx, "<DEFSTRUCT POINT VECTOR (POINT-X FIX 0) (POINT-Y FIX 0) (POINT-Z FIX 0) (POINT-W FIX 'NONE)>",
                pointAtom);

            // put values into existing object, setting any omitted fields to default values (unless the default is NONE!)
            var vector = new ZilVector(new ZilObject[] { new ZilFix(123), new ZilFix(456), new ZilFix(789), new ZilFix(1011) });
            ctx.SetLocalVal(ZilAtom.Parse("MY-VECTOR", ctx),
                new ZilStructuredHash(pointAtom, PrimType.VECTOR, vector));
            TestHelpers.EvalAndAssert(ctx, "<MAKE-POINT 'POINT .MY-VECTOR 'POINT-Y 999 'POINT-X 888>",
                new ZilStructuredHash(pointAtom, PrimType.VECTOR, vector));
            Assert.AreEqual(new ZilFix(888), vector[0]);
            Assert.AreEqual(new ZilFix(999), vector[1]);
            Assert.AreEqual(new ZilFix(0), vector[2]);
            Assert.AreEqual(new ZilFix(1011), vector[3]);
        }

        [TestMethod]
        public void TestDEFSTRUCT_PerFieldOffsets()
        {
            var ctx = new Context();
            var rpointAtom = ZilAtom.Parse("RPOINT", ctx);

            // per-field offsets
            TestHelpers.EvalAndAssert(ctx, "<DEFSTRUCT RPOINT VECTOR (RPOINT-X FIX 'OFFSET 2) (RPOINT-Y FIX 'OFFSET 1)>",
                rpointAtom);
            TestHelpers.EvalAndAssert(ctx, "<MAKE-RPOINT 'RPOINT-X 123 'RPOINT-Y 456>",
                new ZilStructuredHash(rpointAtom, PrimType.VECTOR, new ZilVector(new ZilFix(456), new ZilFix(123))));
            TestHelpers.EvalAndAssert(ctx, "<RPOINT-Y #RPOINT [234 567]>",
                new ZilFix(234));
        }

        [TestMethod]
        public void TestDEFSTRUCT_PerFieldOffsets_Mixed()
        {
            var ctx = new Context();
            var rpointAtom = ZilAtom.Parse("RPOINT", ctx);

            // RPOINT-FOO's auto-assigned offset should be 1; the fields with per-field offsets don't affect the auto-offset counter
            TestHelpers.EvalAndAssert(ctx, "<DEFSTRUCT RPOINT (VECTOR 'CONSTRUCTOR) (RPOINT-X FIX 'OFFSET 2) (RPOINT-Y FIX 'OFFSET 1) (RPOINT-FOO FIX)>",
                rpointAtom);
            TestHelpers.EvalAndAssert(ctx, "<RPOINT-FOO #RPOINT [234 567 8]>",
                new ZilFix(234));
        }

        [TestMethod]
        public void TestDEFSTRUCT_NOTYPE()
        {
            var ctx = new Context();

            TestHelpers.Evaluate(ctx, "<DEFSTRUCT POINT (VECTOR 'NOTYPE) (POINT-X FIX) (POINT-Y FIX)>");
            TestHelpers.EvalAndAssert(ctx, "<POINT-X [123 456]>", new ZilFix(123));
            TestHelpers.EvalAndCatch<InterpreterError>(ctx, "<CHTYPE [123 456] POINT>");
        }

        [TestMethod]
        public void TestDEFSTRUCT_NODECL()
        {
            var ctx = new Context();

            TestHelpers.Evaluate(ctx, "<DEFSTRUCT POINT (VECTOR 'NODECL) (POINT-X FIX) (POINT-Y FIX)>");
            TestHelpers.Evaluate(ctx, "<CHTYPE [1 2 3 4 SHOE] POINT>");

            TestHelpers.Evaluate(ctx, "<SET PT <MAKE-POINT 100 200>>");
            TestHelpers.Evaluate(ctx, "<POINT-X .PT FOO>");
            TestHelpers.EvalAndAssert(ctx, "<POINT-X .PT>", ZilAtom.Parse("FOO", ctx));
        }

        [TestMethod]
        public void TestDEFSTRUCT_Without_NODECL()
        {
            var ctx = new Context();

            TestHelpers.Evaluate(ctx, "<DEFSTRUCT POINT (VECTOR) (POINT-X FIX) (POINT-Y FIX)>");
            TestHelpers.EvalAndCatch<DeclCheckError>(ctx, "<CHTYPE [1 2 3 4 SHOE] POINT>");

            TestHelpers.EvalAndCatch<DeclCheckError>(ctx, "<SET PT <MAKE-POINT A B>>");
            TestHelpers.Evaluate(ctx, "<SET PT <MAKE-POINT 100 200>>");
            TestHelpers.EvalAndCatch<DeclCheckError>(ctx, "<POINT-X .PT FOO>");
            TestHelpers.Evaluate(ctx, "<POINT-X .PT 99>");
        }

        [TestMethod]
        public void TestDEFSTRUCT_Suppress_Constructor()
        {
            var ctx = new Context();

            TestHelpers.Evaluate(ctx, "<DEFSTRUCT POINT (VECTOR 'CONSTRUCTOR) (POINT-X FIX) (POINT-Y FIX)>");
            TestHelpers.EvalAndAssert(ctx, "<GASSIGNED? MAKE-POINT>", ctx.FALSE);
            TestHelpers.EvalAndAssert(ctx, "<POINT-X <CHTYPE [123 456] POINT>>", new ZilFix(123));
        }

        [TestMethod]
        public void TestDEFSTRUCT_InitArgs()
        {
            var ctx = new Context();

            TestHelpers.Evaluate(ctx, "<DEFSTRUCT POINT (TABLE ('INIT-ARGS (PURE))) (POINT-X FIX) (POINT-Y FIX)>");
            var point = TestHelpers.Evaluate(ctx, "<MAKE-POINT 'POINT-X 123 'POINT-Y 456>");

            var table = point.GetPrimitive(ctx);
            Assert.IsInstanceOfType(table, typeof(ZilTable));
            Assert.AreEqual(TableFlags.Pure, ((ZilTable)table).Flags);
        }

        [TestMethod]
        public void TestDEFSTRUCT_Bare_Constructor_Call()
        {
            var ctx = new Context();
            var pointAtom = ZilAtom.Parse("POINT", ctx);

            TestHelpers.Evaluate(ctx, "<DEFSTRUCT POINT VECTOR (POINT-X FIX) (POINT-Y FIX)>");

            TestHelpers.EvalAndAssert(ctx, "<MAKE-POINT>",
                new ZilStructuredHash(pointAtom, PrimType.VECTOR, new ZilVector(new ZilFix(0), new ZilFix(0))));
        }

        [TestMethod]
        public void TestDEFSTRUCT_Positional_Constructor_Argument()
        {
            var ctx = new Context();

            TestHelpers.Evaluate(ctx, "<DEFSTRUCT FOO VECTOR (FOO-A ATOM) (FOO-B <OR FIX FALSE>)>");

            TestHelpers.EvalAndAssert(ctx, "<MAKE-FOO BAR>",
                new ZilStructuredHash(
                    ZilAtom.Parse("FOO", ctx),
                    PrimType.VECTOR,
                    new ZilVector(ZilAtom.Parse("BAR", ctx), ctx.FALSE)));
        }

        [TestMethod]
        public void TestDEFSTRUCT_Custom_Boa_Constructor()
        {
            var ctx = new Context();

            TestHelpers.Evaluate(ctx, @"
<SETG NEXT-ID 0>
<DEFSTRUCT RGBA (VECTOR
                 'CONSTRUCTOR
                 ('CONSTRUCTOR MAKE-RGBA ('RED 'GREEN 'BLUE ""OPT"" ('ALPHA 255) ""AUX"" (RGBA-ID '<SETG NEXT-ID <+ ,NEXT-ID 1>>))))
    (RED FIX) (GREEN FIX) (BLUE FIX) (ALPHA FIX) (RGBA-ID FIX)>");

            TestHelpers.EvalAndCatch<InterpreterError>(ctx, "<MAKE-RGBA 127 127>");
            TestHelpers.EvalAndAssert(ctx, "<RED <MAKE-RGBA 10 20 30>>", new ZilFix(10));       // ID 1
            TestHelpers.EvalAndAssert(ctx, "<RGBA-ID <MAKE-RGBA 11 22 33>>", new ZilFix(2));
            TestHelpers.EvalAndAssert(ctx, "<ALPHA <MAKE-RGBA 11 22 33>>", new ZilFix(255));    // ID 3
            TestHelpers.EvalAndAssert(ctx, "<ALPHA <MAKE-RGBA 11 22 33 44>>", new ZilFix(44));  // ID 4
        }

        [TestMethod]
        public void TestDEFSTRUCT_Eval_Args()
        {
            var ctx = new Context();

            TestHelpers.Evaluate(ctx, "<DEFSTRUCT POINT VECTOR (POINT-X FIX) (POINT-Y <OR FIX FORM>)>");

            TestHelpers.EvalAndAssert(ctx, "<MAKE-POINT 'POINT-X <+ 1 2> 'POINT-Y '<+ 3 4>>",
                new ZilStructuredHash(
                    ZilAtom.Parse("POINT", ctx),
                    PrimType.VECTOR,
                    new ZilVector(
                        new ZilFix(3),
                        new ZilForm(new ZilObject[]
                        {
                            ctx.GetStdAtom(StdAtom.Plus),
                            new ZilFix(3),
                            new ZilFix(4)
                        }))));
        }

        [TestMethod]
        public void TestDEFSTRUCT_Explicit_Default_Field_Values()
        {
            var ctx = new Context();

            TestHelpers.Evaluate(ctx, @"
<DEFSTRUCT POINT VECTOR (POINT-X FIX 123) (POINT-Y FIX 456) (POINT-ID FIX <ALLOCATE-ID>)>
<SETG NEXT-ID 1>
<DEFINE ALLOCATE-ID (""AUX"" (R ,NEXT-ID))
    <SETG NEXT-ID <+ ,NEXT-ID 1>>
    .R>");

            TestHelpers.EvalAndAssert(ctx, "<POINT-ID <MAKE-POINT>>", new ZilFix(1));
            TestHelpers.EvalAndAssert(ctx, "<POINT-ID <MAKE-POINT>>", new ZilFix(2));
            TestHelpers.EvalAndAssert(ctx, "<POINT-ID <MAKE-POINT 'POINT-ID 1001>>", new ZilFix(1001));
            TestHelpers.EvalAndAssert(ctx, "<POINT-ID <MAKE-POINT>>", new ZilFix(3));
            TestHelpers.EvalAndAssert(ctx, "<POINT-X <MAKE-POINT 'POINT-Y 0>>", new ZilFix(123));   // ID 4
            TestHelpers.EvalAndAssert(ctx, "<POINT-Y <MAKE-POINT 'POINT-Y 0>>", new ZilFix(0));     // ID 5

            TestHelpers.EvalAndAssert(ctx, "<POINT-ID <MAKE-POINT 11 22>>", new ZilFix(6));
            TestHelpers.EvalAndAssert(ctx, "<POINT-ID <MAKE-POINT 'POINT <MAKE-POINT 111 222 333> 'POINT-Y 200>>", new ZilFix(7));
            TestHelpers.EvalAndAssert(ctx, "<POINT-ID <MAKE-POINT>>", new ZilFix(8));
        }

        [TestMethod]
        public void TestDEFSTRUCT_Implicit_Default_Field_Values()
        {
            var ctx = new Context();

            TestHelpers.Evaluate(ctx, @"
<DEFSTRUCT FOO VECTOR (FOO-FIX FIX) (FOO-STRING STRING) (FOO-ATOM ATOM) (FOO-LIST LIST) (FOO-VECTOR VECTOR) (FOO-MULTI <OR LIST FALSE>)>");

            TestHelpers.EvalAndAssert(ctx, "<FOO-FIX <MAKE-FOO>>", new ZilFix(0));
            TestHelpers.EvalAndAssert(ctx, "<FOO-STRING <MAKE-FOO>>", ZilString.FromString(""));
            TestHelpers.EvalAndAssert(ctx, "<FOO-ATOM <MAKE-FOO>>", ctx.GetStdAtom(StdAtom.SORRY));
            TestHelpers.EvalAndAssert(ctx, "<FOO-LIST <MAKE-FOO>>", new ZilList(null, null));
            TestHelpers.EvalAndAssert(ctx, "<FOO-VECTOR <MAKE-FOO>>", new ZilVector());
            TestHelpers.EvalAndAssert(ctx, "<FOO-MULTI <MAKE-FOO>>", ctx.FALSE);
        }

        [TestMethod]
        public void TestDEFSTRUCT_Segment_Constructor_Argument()
        {
            var ctx = new Context();

            TestHelpers.Evaluate(ctx, "<DEFSTRUCT POINT VECTOR (POINT-X FIX) (POINT-Y FIX)>");
            TestHelpers.Evaluate(ctx, "<SET L '('POINT-X 123 'POINT-Y 456)>");

            TestHelpers.EvalAndCatch<InterpreterError>(ctx, "<POINT-X <MAKE-POINT !.L>>");
        }

        [TestMethod]
        public void TestSET_DEFSTRUCT_FILE_DEFAULTS()
        {
            var ctx = new Context();
            ctx.IncludePaths.Add("lib");

            const string FileToIntercept = "inner.zil";

            ctx.InterceptFileExists = path => Path.GetFileName(path) == FileToIntercept;
            ctx.InterceptOpenFile = (path, writing) =>
            {
                if (Path.GetFileName(path) == FileToIntercept)
                {
                    const string fileContent = @"
<SET-DEFSTRUCT-FILE-DEFAULTS ('NTH MY-NTH)>
<DEFINE MY-NTH (STRUC IDX) 12345>
<DEFSTRUCT INNER VECTOR (INNER-X FIX)>";
                    return new MemoryStream(Encoding.ASCII.GetBytes(fileContent));
                }

                return null;
            };

            TestHelpers.Evaluate(ctx, "<FLOAD \"inner\">");
            TestHelpers.EvalAndAssert(ctx, "<INNER-X <MAKE-INNER 'INNER-X 100>>", new ZilFix(12345));

            TestHelpers.Evaluate(ctx, "<DEFSTRUCT OUTER VECTOR (OUTER-X FIX)>");
            TestHelpers.EvalAndAssert(ctx, "<OUTER-X <MAKE-OUTER 'OUTER-X 100>>", new ZilFix(100));
        }

        // TODO: test 0-based field offsets
        // <DEFSTRUCT POINT (TABLE ('START-OFFSET 0) ('NTH ZGET) ('PUT ZPUT)) (POINT-X FIX 123) (POINT-Y FIX 456) (POINT-ID FIX <ALLOCATE-ID>)>

        [TestMethod]
        public void REST_Of_One_Character_String_Should_Be_Empty_String()
        {
            TestHelpers.EvalAndAssert("<REST \"x\">", ZilString.FromString(""));
            TestHelpers.EvalAndAssert("<REST <REST \"xx\">>", ZilString.FromString(""));
        }

        [TestMethod]
        public void ILIST_Should_Evaluate_Initializer_Each_Time()
        {
            TestHelpers.EvalAndAssert("<SET X 0> <ILIST 3 '<SET X <+ .X 1>>>",
                new ZilList(new ZilObject[] {
                    new ZilFix(1), new ZilFix(2), new ZilFix(3)
                }));
        }

        [TestMethod]
        public void IVECTOR_Should_Evaluate_Initializer_Each_Time()
        {
            TestHelpers.EvalAndAssert("<SET X 0> <IVECTOR 3 '<SET X <+ .X 1>>>",
                new ZilVector(new ZilObject[] {
                    new ZilFix(1), new ZilFix(2), new ZilFix(3)
                }));
        }

        [TestMethod]
        public void ISTRING_Should_Evaluate_Initializer_Each_Time()
        {
            TestHelpers.EvalAndAssert("<SET X 64> <ISTRING 3 '<ASCII <SET X <+ .X 1>>>>",
                ZilString.FromString("ABC"));
        }

        [TestMethod]
        public void REST_Of_String_Should_Be_A_ZilString_Instance()
        {
            // this isn't usually visible to the user code, but it's needed because of all the "if (foo is ZilString)" validations
            var rested = TestHelpers.Evaluate("<REST \"hello\">");
            Assert.IsInstanceOfType(rested, typeof(ZilString));
        }

        [TestMethod]
        public void ZREST_Of_Table_Should_Be_A_ZilTable_Instance()
        {
            var rested = TestHelpers.Evaluate("<ZREST <TABLE 1 2 3> 2>");
            Assert.IsInstanceOfType(rested, typeof(ZilTable));
        }

        [TestMethod]
        public void ZREST_Works_On_New_TABLE_Types()
        {
            var ctx = new Context();
            TestHelpers.Evaluate(ctx, "<NEWTYPE FOO TABLE>");
            var rested = TestHelpers.Evaluate(ctx, "<ZREST <CHTYPE <TABLE 1 2 3> FOO> 2>");
            Assert.IsInstanceOfType(rested, typeof(ZilTable));

            rested = TestHelpers.Evaluate(ctx, "<ZREST <CHTYPE <ZREST <TABLE 1 2 3> 2> FOO> 2>");
            Assert.IsInstanceOfType(rested, typeof(ZilTable));
        }

        [TestMethod]
        public void Can_Access_Unaligned_Word_After_ZREST()
        {
            TestHelpers.EvalAndAssert("<ZGET <ZREST <ITABLE BYTE 1 100 101 102> 1> 0>", new ZilFix(100));
            TestHelpers.EvalAndAssert("<ZPUT <ZREST <ITABLE BYTE 1 100 101 102> 1> 0 99>", new ZilFix(99));
        }

        [TestMethod]
        public void Access_Past_End_Of_Table_Fails()
        {
            TestHelpers.EvalAndCatch<InterpreterError>("<ZREST <TABLE 1> 100>");
            TestHelpers.EvalAndCatch<InterpreterError>("<ZGET <TABLE 1> 100>");
            TestHelpers.EvalAndCatch<InterpreterError>("<ZPUT <TABLE 1> 100 0>");
            TestHelpers.EvalAndCatch<InterpreterError>("<GETB <TABLE 1> 100>");
            TestHelpers.EvalAndCatch<InterpreterError>("<PUTB <TABLE 1> 100 0>");
        }

        [TestMethod]
        public void Unaligned_Access_To_Table_Fails()
        {
            TestHelpers.EvalAndCatch<InterpreterError>("<ZGET <TABLE (BYTE) 1 2 3> 0>");
            TestHelpers.EvalAndCatch<InterpreterError>("<GETB <TABLE 1 2 3> 0>");
            TestHelpers.EvalAndCatch<InterpreterError>("<GETB <TABLE 1 2 3> 1>");

            TestHelpers.EvalAndCatch<InterpreterError>("<ZGET <ZREST <TABLE (BYTE) 1 2 3> 1> 0>");
            TestHelpers.EvalAndCatch<InterpreterError>("<GETB <ZREST <TABLE 1 2 3> 2> 0>");
        }

        [TestMethod]
        public void SUBSTRUC_With_One_Argument_Returns_A_Primitive_Copy()
        {
            TestHelpers.EvalAndAssert("<SUBSTRUC '(1 2 3)>",
                new ZilList(new[] { new ZilFix(1), new ZilFix(2), new ZilFix(3) }));

            TestHelpers.EvalAndAssert("<SUBSTRUC <QUOTE 10:20>>",
                new ZilVector(new ZilFix(10), new ZilFix(20)));

            TestHelpers.EvalAndAssert("<SUBSTRUC \"Hello\">",
                ZilString.FromString("Hello"));
        }

        [TestMethod]
        public void SUBSTRUC_With_Two_Arguments_Returns_A_RESTed_Primitive_Copy()
        {
            TestHelpers.EvalAndAssert("<SUBSTRUC '(1 2 3) 2>",
                new ZilList(new[] { new ZilFix(3) }));

            TestHelpers.EvalAndAssert("<SUBSTRUC <QUOTE 10:20> 0>",
                new ZilVector(new ZilFix(10), new ZilFix(20)));

            TestHelpers.EvalAndAssert("<SUBSTRUC \"Hello\" 3>",
                ZilString.FromString("lo"));
        }

        [TestMethod]
        public void SUBSTRUC_With_Three_Arguments_Limits_Copying()
        {
            TestHelpers.EvalAndAssert("<SUBSTRUC '(1 2 3) 2 0>",
                new ZilList(null, null));

            TestHelpers.EvalAndAssert("<SUBSTRUC <QUOTE 10:20> 0 1>",
                new ZilVector(new ZilFix(10)));

            TestHelpers.EvalAndAssert("<SUBSTRUC \"Hello\" 1 3>",
                ZilString.FromString("ell"));
        }

        [TestMethod]
        public void SUBSTRUC_With_Four_Arguments_Copies_Into_An_Existing_Structure()
        {
            TestHelpers.EvalAndAssert("<SUBSTRUC '(1 2 3) 2 0 '(4 5 6)>",
                new ZilList(new[] { new ZilFix(4), new ZilFix(5), new ZilFix(6) }));

            TestHelpers.EvalAndAssert("<SUBSTRUC <QUOTE 10:20> 0 1 '[30 40]>",
                new ZilVector(new ZilFix(10), new ZilFix(40)));

            TestHelpers.EvalAndAssert("<SUBSTRUC \"Hello\" 1 3 \"Leeroy\">",
                ZilString.FromString("ellroy"));

            // TODO: should work with user-defined types too
            //var ctx = new Context();
            //TestHelpers.Evaluate(ctx, "<NEWTYPE ARRAY VECTOR>");
            //TestHelpers.EvalAndAssert(ctx, "<SUBSTRUC '[1 2 3] 2 1 #ARRAY [4 5 6]>",
            //    new ZilVector(new ZilFix(3), new ZilFix(5), new ZilFix(6)));
        }

        [TestMethod]
        public void PUT_Past_End_Of_LIST_Should_Throw()
        {
            TestHelpers.EvalAndCatch<InterpreterError>("<PUT '(1 2 3) 4 FOO>");
        }

        [TestMethod]
        public void TestBACK()
        {
            var ctx = new Context();
            TestHelpers.Evaluate(ctx, "<SET V <REST '[1 2 3 4] 3>>");
            TestHelpers.EvalAndAssert(ctx, "<BACK .V 2>",
                new ZilVector(new ZilFix(2), new ZilFix(3), new ZilFix(4)));

            TestHelpers.Evaluate(ctx, "<SET S <REST \"Hello world!\" 12>>");
            TestHelpers.EvalAndAssert(ctx, "<BACK .S 6>",
                ZilString.FromString("world!"));
        }

        [TestMethod]
        public void TestTOP()
        {
            var ctx = new Context();
            TestHelpers.Evaluate(ctx, "<SET V <REST '[1 2 3 4] 3>>");
            TestHelpers.EvalAndAssert(ctx, "<TOP .V>",
                new ZilVector(new ZilFix(1), new ZilFix(2), new ZilFix(3), new ZilFix(4)));

            TestHelpers.Evaluate(ctx, "<SET S <REST \"Hello world!\" 12>>");
            TestHelpers.EvalAndAssert(ctx, "<TOP .S>",
                ZilString.FromString("Hello world!"));
        }

        [TestMethod]
        public void TestGROW()
        {
            var ctx = new Context();
            TestHelpers.Evaluate(ctx, "<SET V '[1 2 3]>");
            TestHelpers.EvalAndAssert(ctx, "<LENGTH .V>", new ZilFix(3));

            // add elements
            TestHelpers.EvalAndAssert(ctx, "<SET G <GROW .V 1 2>>",
                new ZilVector(ctx.FALSE, ctx.FALSE, new ZilFix(1), new ZilFix(2), new ZilFix(3), ctx.FALSE));
            // confirm lengths
            TestHelpers.EvalAndAssert(ctx, "<LENGTH .V>", new ZilFix(4));
            TestHelpers.EvalAndAssert(ctx, "<LENGTH .G>", new ZilFix(6));
            // confirm elements are shared
            TestHelpers.EvalAndAssert(ctx, "<1 .V>", new ZilFix(1));
            TestHelpers.EvalAndAssert(ctx, "<3 .G>", new ZilFix(1));
            TestHelpers.Evaluate(ctx, "<1 .V 999>");
            TestHelpers.EvalAndAssert(ctx, "<3 .G>", new ZilFix(999));

            // test TOP, BACK, REST on affected vector
            TestHelpers.EvalAndAssert(ctx, "<1 <TOP .V>>", ctx.FALSE);
            TestHelpers.Evaluate(ctx, "<1 <TOP .V> 111>");
            TestHelpers.EvalAndAssert(ctx, "<1 <BACK .V 2>>", new ZilFix(111));
            TestHelpers.EvalAndAssert(ctx, "<1 <REST .G 2>>", new ZilFix(999));
            TestHelpers.EvalAndAssert(ctx, "<1 <REST <BACK .V 2> 2>>", new ZilFix(999));

            // can't remove elements
            TestHelpers.EvalAndCatch<InterpreterError>(ctx, "<SET V2 <GROW .G -1 -2>>");
        }

        [TestMethod]
        public void TestSORT()
        {
            // simple form
            TestHelpers.EvalAndAssert("<SORT <> '[1 9 3 5 6 2]>",
                new ZilVector(new ZilFix(1), new ZilFix(2), new ZilFix(3),
                              new ZilFix(5), new ZilFix(6), new ZilFix(9)));

            // multi-element records
            var ctx = new Context();
            var money = ZilAtom.Parse("MONEY", ctx);
            var show = ZilAtom.Parse("SHOW", ctx);
            var ready = ZilAtom.Parse("READY", ctx);
            var go = ZilAtom.Parse("GO", ctx);

            TestHelpers.Evaluate(ctx, "<SET V '[1 MONEY 2 SHOW 3 READY 4 GO]>");
            TestHelpers.EvalAndAssert(ctx, "<SORT <> .V 2 1>",
                new ZilVector(new ZilFix(4), go, new ZilFix(1), money,
                              new ZilFix(3), ready, new ZilFix(2), show));

            // vector length must be a multiple of record size
            TestHelpers.EvalAndCatch<InterpreterError>(ctx, "<SORT <> '[1 2 3 4] 3 1>");

            // record size must be positive
            TestHelpers.EvalAndCatch<InterpreterError>(ctx, "<SORT <> .V 0 1>");
            TestHelpers.EvalAndCatch<InterpreterError>(ctx, "<SORT <> .V -2 1>");

            // custom predicates, confirmation that .V is sorted in place, and early exit
            TestHelpers.Evaluate(ctx, "<SORT ,L? .V 2>");
            TestHelpers.EvalAndAssert(ctx, ".V",
                new ZilVector(new ZilFix(4), go, new ZilFix(3), ready,
                              new ZilFix(2), show, new ZilFix(1), money));
            TestHelpers.EvalAndAssert(
                ctx,
                "<PROG OUTER () <SORT <FUNCTION (A B) <RETURN 999 .OUTER>> .V 2>>",
                new ZilFix(999));
            TestHelpers.EvalAndAssert(ctx, ".V",
                new ZilVector(new ZilFix(4), go, new ZilFix(3), ready,
                    new ZilFix(2), show, new ZilFix(1), money));

            // multiple structures
            TestHelpers.EvalAndAssert(ctx, "<SORT <> '[2 1 4 3 6 5 8 7] 1 0 .V>",
                new ZilVector(new ZilFix(1), new ZilFix(2), new ZilFix(3), new ZilFix(4),
                              new ZilFix(5), new ZilFix(6), new ZilFix(7), new ZilFix(8)));
            TestHelpers.EvalAndAssert(ctx, ".V",
                new ZilVector(go, new ZilFix(4), ready, new ZilFix(3),
                              show, new ZilFix(2), money, new ZilFix(1)));

            // structures must have the same number of records
            TestHelpers.EvalAndCatch<InterpreterError>(ctx, "<SORT <> '[1 2 3] 1 0 '[1 2 3 4]>");
            TestHelpers.EvalAndCatch<InterpreterError>(ctx, "<SORT <> '[1 2 3 4] 1 0 '[1 2 3]>");

            // predicate must be applicable if not FALSE
            TestHelpers.EvalAndCatch<InterpreterError>(ctx, "<SORT FOO '[4 2 1 3]>");
            TestHelpers.EvalAndCatch<InterpreterError>(ctx, "<SORT '(1 2 3 4) '[4 2 1 3]>");
        }

        [TestMethod]
        public void TestOFFSET()
        {
            var ctx = new Context();

            TestHelpers.Evaluate(ctx, "<SET L1 '(1 MONEY)>");
            TestHelpers.Evaluate(ctx, "<SET L2 '(BOW WOW)>");
            TestHelpers.Evaluate(ctx, "<SET FIRST-FIX <OFFSET 1 '<LIST FIX> FIX>>");

            TestHelpers.EvalAndCatch<InterpreterError>(ctx, "<FIRST-FIX .L1 NOT-A-FIX>");
            TestHelpers.EvalAndCatch<InterpreterError>(ctx, "<FIRST-FIX .L2 0>");
            TestHelpers.EvalAndAssert(ctx, "<FIRST-FIX .L1>", new ZilFix(1));
            TestHelpers.Evaluate(ctx, "<FIRST-FIX .L1 0>");
            TestHelpers.EvalAndAssert(ctx, "<FIRST-FIX .L1>", new ZilFix(0));

            // INDEX returns the offset
            TestHelpers.EvalAndAssert(ctx, "<INDEX .FIRST-FIX>", new ZilFix(1));

            // GET-DECL returns the DECL portion
            TestHelpers.EvalAndAssert(ctx, "<GET-DECL .FIRST-FIX>",
                new ZilForm(new[] { ctx.GetStdAtom(StdAtom.LIST), ctx.GetStdAtom(StdAtom.FIX) }));

            // PUT-DECL returns a new offset with a different DECL...
            TestHelpers.EvalAndAssert(ctx, "<PUT-DECL .FIRST-FIX ANY>",
                new ZilOffset(1, ctx.GetStdAtom(StdAtom.ANY), ctx.GetStdAtom(StdAtom.FIX)));

            // ...without changing the original
            TestHelpers.EvalAndAssert(ctx, ".FIRST-FIX",
                new ZilOffset(
                    1,
                    new ZilForm(new[] { ctx.GetStdAtom(StdAtom.LIST), ctx.GetStdAtom(StdAtom.FIX) }),
                    ctx.GetStdAtom(StdAtom.FIX)));
        }

        [TestMethod]
        public void TestPUTREST()
        {
            var ctx = new Context();

            TestHelpers.EvalAndAssert(ctx, "<PUTREST '(1 2 3) '(A B)>",
                new ZilList(new ZilObject[] {
                    new ZilFix(1),
                    ZilAtom.Parse("A", ctx),
                    ZilAtom.Parse("B", ctx)
                }));

            TestHelpers.EvalAndAssert(ctx, "<PUTREST '<1 2 3> '(A B)>",
                new ZilForm(new ZilObject[] {
                    new ZilFix(1),
                    ZilAtom.Parse("A", ctx),
                    ZilAtom.Parse("B", ctx)
                }));

            TestHelpers.EvalAndCatch<InterpreterError>(ctx, "<PUTREST <ASSOCIATIONS> '()>",
                ex => !(ex is ArgumentDecodingError));

            TestHelpers.EvalAndCatch<ArgumentTypeError>("<PUTREST [1 2] [FOO]>");

            TestHelpers.EvalAndCatch<InterpreterError>("<PUTREST () (5)>");
        }

        [TestMethod]
        public void OBJECT_Clauses_Can_Be_Generated_By_READ_Macros()
        {
            TestHelpers.Evaluate(@"
<OBJECT FOO
    (DESC ""foo"")
    %<VERSION? (ZIP '(SYNONYM FOOV3)) (ELSE '(SYNONYM FOOV4))>>
");
        }
    }
}
