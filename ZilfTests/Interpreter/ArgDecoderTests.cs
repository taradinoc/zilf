/* Copyright 2010, 2016 Jesse McGrew
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
using System;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Zilf.Interpreter;
using Zilf.Interpreter.Values;
using System.Reflection;
using Zilf.Language;
using System.Runtime.InteropServices;

namespace ZilfTests.Interpreter
{
    [TestClass]
    public class ArgDecoderTests
    {
        private Context ctx;

        [TestInitialize]
        public void TestInitialize()
        {
            ctx = new Context();
        }

        private static MethodInfo GetMethod(string name)
        {
            return typeof(ArgDecoderTests).GetMethod(
                name,
                BindingFlags.Instance | BindingFlags.Static | BindingFlags.NonPublic);
        }

        [TestMethod]
        [ExpectedException(typeof(ArgumentNullException))]
        public void FromMethodInfo_Requires_NonNull_Argument()
        {
            var decoder = ArgDecoder.FromMethodInfo(null);
        }

        [TestMethod]
        [ExpectedException(typeof(ArgumentException))]
        public void FromMethodInfo_Requires_ZilObject_Return()
        {
            var methodInfo = GetMethod(nameof(Dummy_WrongReturn));

            var decoder = ArgDecoder.FromMethodInfo(methodInfo);
        }

        private void Dummy_WrongReturn(Context ctx)
        {
            // nada
        }

        [TestMethod]
        public void Test_ContextOnly()
        {
            var methodInfo = GetMethod(nameof(Dummy_ContextOnly));

            var decoder = ArgDecoder.FromMethodInfo(methodInfo);
            object[] actual = decoder.Decode("dummy", ctx, new ZilObject[] { });
            object[] expected = { ctx };

            CollectionAssert.AreEqual(expected, actual);
        }

        private ZilObject Dummy_ContextOnly(Context ctx)
        {
            return null;
        }

        [TestMethod]
        public void Test_ZilObjectArg()
        {
            var methodInfo = GetMethod(nameof(Dummy_ZilObjectArg));

            var arg = new ZilList(null, null);

            var decoder = ArgDecoder.FromMethodInfo(methodInfo);
            object[] actual = decoder.Decode("dummy", ctx, new ZilObject[] { arg });
            object[] expected = { ctx, arg };

            CollectionAssert.AreEqual(expected, actual);
        }


        private ZilObject Dummy_ZilObjectArg(Context ctx, ZilObject arg1)
        {
            return null;
        }

        [TestMethod]
        public void Test_ZilObjectArrayArg()
        {
            var methodInfo = GetMethod(nameof(Dummy_ZilObjectArrayArg));

            ZilObject[] args = { new ZilFix(5), ZilString.FromString("halloo") };

            var decoder = ArgDecoder.FromMethodInfo(methodInfo);
            object[] actual = decoder.Decode("dummy", ctx, args);
            object[] expected = { ctx, args };

            Assert.AreEqual(expected.Length, actual.Length);
            Assert.AreEqual(expected[0], actual[0]);
            Assert.IsInstanceOfType(actual[1], typeof(ZilObject[]));
            CollectionAssert.AreEqual((ZilObject[])expected[1], (ZilObject[])actual[1]);
        }

        private ZilObject Dummy_ZilObjectArrayArg(Context ctx, ZilObject[] args)
        {
            return null;
        }

        [TestMethod]
        [ExpectedException(typeof(ArgumentCountError))]
        public void Test_ZilObjectArrayArg_Required_Fail()
        {
            var methodInfo = GetMethod(nameof(Dummy_RequiredZilObjectArrayArg));

            ZilObject[] args = { };

            var decoder = ArgDecoder.FromMethodInfo(methodInfo);
            object[] actual = decoder.Decode("dummy", ctx, args);
        }

        private ZilObject Dummy_RequiredZilObjectArrayArg(Context ctx, [Required] ZilObject[] args)
        {
            return null;
        }

        [TestMethod]
        public void Test_IntArg()
        {
            var methodInfo = GetMethod(nameof(Dummy_IntArgs));

            ZilObject[] args = { new ZilFix(123), new ZilFix(456) };

            var decoder = ArgDecoder.FromMethodInfo(methodInfo);
            object[] actual = decoder.Decode("dummy", ctx, args);
            object[] expected = { ctx, 123, 456 };

            CollectionAssert.AreEqual(expected, actual);
        }

        [TestMethod]
        [ExpectedException(typeof(ArgumentCountError))]
        public void Test_TooFewArgs()
        {
            var methodInfo = GetMethod(nameof(Dummy_IntArgs));

            ZilObject[] args = { new ZilFix(123) };

            var decoder = ArgDecoder.FromMethodInfo(methodInfo);
            object[] actual = decoder.Decode("dummy", ctx, args);
        }

        [TestMethod]
        [ExpectedException(typeof(ArgumentCountError))]
        public void Test_TooManyArgs()
        {
            var methodInfo = GetMethod(nameof(Dummy_IntArgs));

            ZilObject[] args = { new ZilFix(123), new ZilFix(456), new ZilFix(789) };

            var decoder = ArgDecoder.FromMethodInfo(methodInfo);
            object[] actual = decoder.Decode("dummy", ctx, args);
        }

        private ZilObject Dummy_IntArgs(Context ctx, int foo, int bar)
        {
            return null;
        }

        [TestMethod]
        public void Test_IntArrayArg()
        {
            var methodInfo = GetMethod(nameof(Dummy_IntArrayArg));

            ZilObject[] args = { new ZilFix(123), new ZilFix(456) };

            var decoder = ArgDecoder.FromMethodInfo(methodInfo);
            object[] actual = decoder.Decode("dummy", ctx, args);
            object[] expected = { ctx, new int[] { 123, 456 } };

            Assert.AreEqual(2, actual.Length);
            Assert.AreEqual(expected[0], actual[0]);
            Assert.IsInstanceOfType(actual[1], typeof(int[]));
            CollectionAssert.AreEqual((int[])expected[1], (int[])actual[1]);
        }

        private ZilObject Dummy_IntArrayArg(Context ctx, int[] foo)
        {
            return null;
        }

        [TestMethod]
        public void Test_StringArg()
        {
            var methodInfo = GetMethod(nameof(Dummy_StringArgs));

            ZilObject[] args = { ZilString.FromString("hello"), ZilString.FromString("world") };

            var decoder = ArgDecoder.FromMethodInfo(methodInfo);
            object[] actual = decoder.Decode("dummy", ctx, args);
            object[] expected = { ctx, "hello", "world" };

            CollectionAssert.AreEqual(expected, actual);
        }

        private ZilObject Dummy_StringArgs(Context ctx, string foo, string bar)
        {
            return null;
        }

        [TestMethod]
        public void Test_StringArrayArg()
        {
            var methodInfo = GetMethod(nameof(Dummy_StringArrayArg));

            ZilObject[] args = { ZilString.FromString("hello") };

            var decoder = ArgDecoder.FromMethodInfo(methodInfo);
            object[] actual = decoder.Decode("dummy", ctx, args);
            object[] expected = { ctx, new string[] { "hello" } };

            Assert.AreEqual(2, actual.Length);
            Assert.AreEqual(expected[0], actual[0]);
            Assert.IsInstanceOfType(actual[1], typeof(string[]));
            CollectionAssert.AreEqual((string[])expected[1], (string[])actual[1]);
        }

        private ZilObject Dummy_StringArrayArg(Context ctx, string[] foo)
        {
            return null;
        }

        [TestMethod]
        public void Test_FormArg()
        {
            var methodInfo = GetMethod(nameof(Dummy_FormArg));

            ZilObject[] args = {
                new ZilForm(new ZilObject[] {
                    ctx.GetStdAtom(StdAtom.Plus),
                    new ZilFix(1),
                    new ZilFix(2),
                }),
            };

            var decoder = ArgDecoder.FromMethodInfo(methodInfo);
            object[] actual = decoder.Decode("dummy", ctx, args);
            object[] expected = { ctx, args[0] };

            CollectionAssert.AreEqual(expected, actual);
        }

        private ZilObject Dummy_FormArg(Context ctx, ZilForm form)
        {
            return null;
        }

        [TestMethod]
        public void Test_OptionalArg_EndOfArgs()
        {
            var methodInfo = GetMethod(nameof(Dummy_OptionalIntArg));

            ZilObject[] args = { };

            var decoder = ArgDecoder.FromMethodInfo(methodInfo);
            object[] actual = decoder.Decode("dummy", ctx, args);
            object[] expected = { ctx, 69105 };

            CollectionAssert.AreEqual(expected, actual);
        }

        private ZilObject Dummy_OptionalIntArg(Context ctx, int? foo = 69105)
        {
            return null;
        }

        [TestMethod]
        public void Test_OptionalArg_Omitted()
        {
            var methodInfo = GetMethod(nameof(Dummy_OptionalIntThenStringArg));

            ZilObject[] args = { ZilString.FromString("hello") };

            var decoder = ArgDecoder.FromMethodInfo(methodInfo);
            object[] actual = decoder.Decode("dummy", ctx, args);
            object[] expected = { ctx, 69105, "hello" };

            CollectionAssert.AreEqual(expected, actual);
        }

        [TestMethod]
        public void Test_OptionalArg_Provided()
        {
            var methodInfo = GetMethod(nameof(Dummy_OptionalIntThenStringArg));

            ZilObject[] args = { new ZilFix(42), ZilString.FromString("hello") };

            var decoder = ArgDecoder.FromMethodInfo(methodInfo);
            object[] actual = decoder.Decode("dummy", ctx, args);
            object[] expected = { ctx, 42, "hello" };

            CollectionAssert.AreEqual(expected, actual);
        }

        private ZilObject Dummy_OptionalIntThenStringArg(Context ctx, [Optional, DefaultParameterValue(69105)] int? foo, string bar)
        {
            return null;
        }

        [TestMethod]
        [ExpectedException(typeof(ArgumentCountError))]
        public void Test_OptionalArg_TooManyArgs()
        {
            var methodInfo = GetMethod(nameof(Dummy_OptionalIntArg));

            ZilObject[] args = { ZilString.FromString("not an int") };

            var decoder = ArgDecoder.FromMethodInfo(methodInfo);
            object[] actual = decoder.Decode("dummy", ctx, args);
        }

        [TestMethod]
        public void Test_DeclArg_Pass()
        {
            var methodInfo = GetMethod(nameof(Dummy_DeclArg));

            ZilObject[] args = { ctx.GetStdAtom(StdAtom.ZILF) };

            var decoder = ArgDecoder.FromMethodInfo(methodInfo);
            object[] actual = decoder.Decode("dummy", ctx, args);
            object[] expected = { ctx, args[0] };

            CollectionAssert.AreEqual(expected, actual);
        }

        [TestMethod]
        [ExpectedException(typeof(ArgumentTypeError))]
        public void Test_DeclArg_Fail()
        {
            var methodInfo = GetMethod(nameof(Dummy_DeclArg));

            ZilObject[] args = { ctx.GetStdAtom(StdAtom.ZILCH) };

            var decoder = ArgDecoder.FromMethodInfo(methodInfo);
            object[] actual = decoder.Decode("dummy", ctx, args);
        }

        private ZilObject Dummy_DeclArg(Context ctx, [Decl("'ZILF")] ZilAtom foo)
        {
            return null;
        }

        [TestMethod]
        public void Test_DeclArg_OptionalMiddle()
        {
            var methodInfo = GetMethod(nameof(Dummy_MultiOptionalDeclArgs));

            ZilObject[] args = { new ZilFix(2) };

            var decoder = ArgDecoder.FromMethodInfo(methodInfo);
            object[] actual = decoder.Decode("dummy", ctx, args);
            object[] expected = { ctx, 1, 2 };

            CollectionAssert.AreEqual(expected, actual);
        }

        private ZilObject Dummy_MultiOptionalDeclArgs(Context ctx, [Decl("'1")] int one = 1, [Decl("'2")] int two = 2)
        {
            return null;
        }

        [TestMethod]
        public void Test_DeclArg_VarArgs_Pass()
        {
            var methodInfo = GetMethod(nameof(Dummy_DeclVarArgs));

            ZilObject[] args = {
                new ZilFix(1),
                ZilAtom.Parse("MONEY", ctx),
                new ZilFix(2),
                ZilAtom.Parse("SHOW", ctx),
            };

            var decoder = ArgDecoder.FromMethodInfo(methodInfo);
            object[] actual = decoder.Decode("dummy", ctx, args);

            Assert.AreEqual(actual.Length, 2);
            Assert.AreEqual(actual[0], ctx);
            Assert.IsInstanceOfType(actual[1], typeof(ZilObject[]));
            CollectionAssert.AreEqual(args, (ZilObject[])actual[1]);
        }

        [TestMethod]
        [ExpectedException(typeof(ArgumentTypeError))]
        public void Test_DeclArg_VarArgs_Fail()
        {
            var methodInfo = GetMethod(nameof(Dummy_DeclVarArgs));

            ZilObject[] args = {
                new ZilFix(1),
                new ZilFix(2),
                ZilAtom.Parse("MONEY", ctx),
                ZilAtom.Parse("SHOW", ctx),
            };

            var decoder = ArgDecoder.FromMethodInfo(methodInfo);
            object[] actual = decoder.Decode("dummy", ctx, args);
        }

        private ZilObject Dummy_DeclVarArgs(Context ctx, [Decl("<LIST [REST FIX ATOM]>")] ZilObject[] args)
        {
            return null;
        }

        [TestMethod]
        public void Test_ApplicableArg()
        {
            var methodInfo = GetMethod(nameof(Dummy_ApplicableArg));

            ZilObject[] args = { new ZilFix(1), new ZilSubr("+", ctx.GetSubrDelegate("+")) };

            var decoder = ArgDecoder.FromMethodInfo(methodInfo);
            object[] actual = decoder.Decode("dummy", ctx, args);
            object[] expected = { ctx, new ZilFix(1), new ZilSubr("+", ctx.GetSubrDelegate("+")) };

            CollectionAssert.AreEqual(expected, actual);
        }

        private ZilObject Dummy_ApplicableArg(Context ctx, IApplicable ap1, IApplicable ap2)
        {
            return null;
        }

        [TestMethod]
        [ExpectedException(typeof(ArgumentTypeError))]
        public void Test_AtomArg_Fail()
        {
            var methodInfo = GetMethod(nameof(Dummy_AtomArg));

            ZilObject[] args = { new ZilFix(123) };

            var decoder = ArgDecoder.FromMethodInfo(methodInfo);
            object[] actual = decoder.Decode("dummy", ctx, args);
        }

        private ZilObject Dummy_AtomArg(Context ctx, ZilAtom foo)
        {
            return null;
        }

        [TestMethod]
        [ExpectedException(typeof(ArgumentTypeError))]
        public void Test_AtomArrayArg_Fail()
        {
            var methodInfo = GetMethod(nameof(Dummy_AtomArrayArg));

            ZilObject[] args = { new ZilFix(123) };

            var decoder = ArgDecoder.FromMethodInfo(methodInfo);
            object[] actual = decoder.Decode("dummy", ctx, args);
        }

        private ZilObject Dummy_AtomArrayArg(Context ctx, ZilAtom[] foo)
        {
            return null;
        }

        [TestMethod]
        public void Test_MdlZilRedirect()
        {
            ctx.CurrentFileFlags |= FileFlags.MdlZil;

            var methodInfo = GetMethod(nameof(Dummy_MdlZilRedirect_From));

            ZilObject[] args = { new ZilFix(123) };

            var del = ArgDecoder.WrapMethodAsSubrDelegate(methodInfo);
            var actual = del("dummy", ctx, args);
            var expected = new ZilFix(246);

            Assert.AreEqual(expected, actual);
        }

        [Subrs.MdlZilRedirect(typeof(ArgDecoderTests), nameof(Dummy_MdlZilRedirect_To))]
        private static ZilObject Dummy_MdlZilRedirect_From(Context ctx)
        {
            return ctx.FALSE;
        }

        private static ZilObject Dummy_MdlZilRedirect_To(Context ctx, int num)
        {
            return new ZilFix(num * 2);
        }

        [TestMethod]
        public void Test_PNAME_WrongArgType_Message()
        {
            const string SExpectedMessage = "PNAME: arg 1: expected TYPE ATOM";

            try
            {
                var ctx = new Context();
                Zilf.Program.Evaluate(ctx, "<PNAME 1>", true);
            }
            catch (ArgumentTypeError ex)
            {
                Assert.AreEqual(SExpectedMessage, ex.Message);
                return;
            }

            Assert.Fail($"Expected {nameof(ArgumentTypeError)}");
        }

        [TestMethod]
        public void Test_StructArg()
        {
            var methodInfo = GetMethod(nameof(Dummy_IntStringStructArg));

            ZilObject[] args = {
                new ZilList(new ZilObject[] {
                    new ZilFix(123), ZilString.FromString("hi")
                }),
            };

            var decoder = ArgDecoder.FromMethodInfo(methodInfo);
            object[] actual = decoder.Decode("dummy", ctx, args);
            object[] expected = { ctx, new IntStringStruct { arg1 = 123, arg2 = "hi" } };

            CollectionAssert.AreEqual(expected, actual);
        }

        [TestMethod]
        [ExpectedException(typeof(ArgumentCountError))]
        public void Test_StructArg_Fail_TooFewArgs()
        {
            var methodInfo = GetMethod(nameof(Dummy_IntStringStructArg));

            ZilObject[] args = {
                new ZilList(new ZilObject[] { new ZilFix(123) }),
            };

            var decoder = ArgDecoder.FromMethodInfo(methodInfo);
            object[] actual = decoder.Decode("dummy", ctx, args);
        }

        [TestMethod]
        [ExpectedException(typeof(ArgumentCountError))]
        public void Test_StructArg_Fail_TooManyArgs()
        {
            var methodInfo = GetMethod(nameof(Dummy_IntStringStructArg));

            ZilObject[] args = {
                new ZilList(new ZilObject[] {
                    new ZilFix(123),
                    ZilString.FromString("hi"),
                    ZilString.FromString("oops"),
                }),
            };

            var decoder = ArgDecoder.FromMethodInfo(methodInfo);
            object[] actual = decoder.Decode("dummy", ctx, args);
        }

        [TestMethod]
        public void Test_StructArg_Fail_WrongStructureType()
        {
            const string SExpectedMessage = "dummy: arg 1: expected TYPE LIST";

            var methodInfo = GetMethod(nameof(Dummy_IntStringStructArg));

            ZilObject[] args = {
                new ZilVector(new ZilFix(123), ZilString.FromString("hi")),
            };

            var decoder = ArgDecoder.FromMethodInfo(methodInfo);

            try
            {
                object[] actual = decoder.Decode("dummy", ctx, args);
            }
            catch (ArgumentTypeError ex)
            {
                Assert.AreEqual(SExpectedMessage, ex.Message);
                return;
            }

            Assert.Fail($"Expected {nameof(ArgumentTypeError)}");
        }

        [TestMethod]
        public void Test_StructArg_Fail_WrongElementType()
        {
            const string SExpectedMessage = "dummy: arg 1: element 2: expected TYPE STRING";

            var methodInfo = GetMethod(nameof(Dummy_IntStringStructArg));

            ZilObject[] args = {
                new ZilList(new ZilObject[] {
                    new ZilFix(123), ctx.FALSE,
                }),
            };

            var decoder = ArgDecoder.FromMethodInfo(methodInfo);

            try
            {
                object[] actual = decoder.Decode("dummy", ctx, args);
            }
            catch (ArgumentTypeError ex)
            {
                Assert.AreEqual(SExpectedMessage, ex.Message);
                return;
            }

            Assert.Fail($"Expected {nameof(ArgumentTypeError)}");
        }

        [ZilStructuredParam(StdAtom.LIST)]
        private struct IntStringStruct
        {
            public int arg1;
            public string arg2;
        }

        private ZilObject Dummy_IntStringStructArg(Context ctx, IntStringStruct foo)
        {
            return null;
        }

        [TestMethod]
        public void Test_StructArg_Array()
        {
            var methodInfo = GetMethod(nameof(Dummy_IntStringStructArrayArg));

            ZilObject[] args = {
                new ZilList(new ZilObject[] {
                    new ZilFix(1), ZilString.FromString("money")
                }),
                new ZilList(new ZilObject[] {
                    new ZilFix(2), ZilString.FromString("show")
                }),
                new ZilList(new ZilObject[] {
                    new ZilFix(3), ZilString.FromString("ready")
                }),
            };

            var decoder = ArgDecoder.FromMethodInfo(methodInfo);
            object[] actual = decoder.Decode("dummy", ctx, args);
            object[] expected = {
                ctx,
                new IntStringStruct[] {
                    new IntStringStruct { arg1 = 1, arg2 = "money" },
                    new IntStringStruct { arg1 = 2, arg2 = "show" },
                    new IntStringStruct { arg1 = 3, arg2 = "ready" },
                },
            };

            Assert.AreEqual(2, actual.Length);
            Assert.AreEqual(expected[0], actual[0]);
            Assert.IsInstanceOfType(actual[1], typeof(IntStringStruct[]));
            CollectionAssert.AreEqual((IntStringStruct[])expected[1], (IntStringStruct[])actual[1]);
        }

        private ZilObject Dummy_IntStringStructArrayArg(Context ctx, IntStringStruct[] foo)
        {
            return null;
        }

        [TestMethod]
        public void Test_StructArg_Nested()
        {
            var methodInfo = GetMethod(nameof(Dummy_OuterStructArg));

            ZilObject[] args = {
                new ZilVector(
                    new ZilFix(123),
                    new ZilList(new ZilObject[]
                    {
                        new ZilFix(456),
                        ZilString.FromString("foo"),
                    })
                ),
            };

            var decoder = ArgDecoder.FromMethodInfo(methodInfo);
            object[] actual = decoder.Decode("dummy", ctx, args);
            object[] expected = {
                ctx,
                new OuterStruct
                {
                    arg1 = 123,
                    arg2 = new IntStringStruct
                    {
                        arg1 = 456,
                        arg2 = "foo",
                    },
                },
            };

            CollectionAssert.AreEqual(expected, actual);
        }

        [TestMethod]
        public void Test_StructArg_Nested_Fail_WrongStructureType()
        {
            const string SExpectedMessage = "dummy: arg 1: element 2: expected TYPE LIST";

            var methodInfo = GetMethod(nameof(Dummy_OuterStructArg));

            ZilObject[] args = {
                new ZilVector(
                    new ZilFix(123),
                    new ZilVector(
                        new ZilFix(456),
                        ZilString.FromString("foo")
                    )
                ),
            };

            var decoder = ArgDecoder.FromMethodInfo(methodInfo);

            try
            {
                object[] actual = decoder.Decode("dummy", ctx, args);
            }
            catch (ArgumentTypeError ex)
            {
                Assert.AreEqual(SExpectedMessage, ex.Message);
            }
        }

        [TestMethod]
        public void Test_StructArg_Nested_Fail_WrongElementType()
        {
            const string SExpectedMessage = "dummy: arg 1: element 2: element 1: expected TYPE FIX";

            var methodInfo = GetMethod(nameof(Dummy_OuterStructArg));

            ZilObject[] args = {
                new ZilVector(
                    new ZilFix(123),
                    new ZilList(new ZilObject[] {
                        ZilString.FromString("foo"),
                        ZilString.FromString("bar"),
                    })
                ),
            };

            var decoder = ArgDecoder.FromMethodInfo(methodInfo);

            try
            {
                object[] actual = decoder.Decode("dummy", ctx, args);
            }
            catch (ArgumentTypeError ex)
            {
                Assert.AreEqual(SExpectedMessage, ex.Message);
            }
        }

        [ZilStructuredParam(StdAtom.VECTOR)]
        private struct OuterStruct
        {
            public int arg1;
            public IntStringStruct arg2;
        }

        private ZilObject Dummy_OuterStructArg(Context ctx, OuterStruct foo)
        {
            return null;
        }

        [TestMethod]
        public void Test_StructArg_Optional()
        {
            var methodInfo = GetMethod(nameof(Dummy_OptionalStructArrayArg));

            ZilObject[] args = {
                new ZilVector(
                    ZilString.FromString("o'clock"),
                    new ZilFix(4),
                    ZilString.FromString("o'clock"))
            };

            var decoder = ArgDecoder.FromMethodInfo(methodInfo);
            object[] actual = decoder.Decode("dummy", ctx, args);
            object[] expected = {
                ctx,
                new OptionalStruct {
                    arg1 = 1,
                    arg2 = 2,
                    arg3 = 3,
                    arg4 = "o'clock",
                    arg5 = 4,
                    arg6 = "o'clock",
                    arg7 = "rock",
                },
            };

            CollectionAssert.AreEqual(expected, actual);
        }

        [ZilStructuredParam(StdAtom.VECTOR)]
        private struct OptionalStruct
        {
            [ZilOptional(Default = 1)]
            public int arg1;
            [ZilOptional(Default = 2)]
            public int arg2;
            [ZilOptional(Default = 3)]
            public int arg3;

            public string arg4;

            [ZilOptional(Default = 111)]
            public int arg5;
            [ZilOptional(Default = "blah")]
            public string arg6;

            [ZilOptional(Default = "rock")]
            public string arg7;
        }

        private ZilObject Dummy_OptionalStructArrayArg(Context ctx, OptionalStruct foo)
        {
            return null;
        }

        [TestMethod]
        public void Test_EitherArg_Int()
        {
            var methodInfo = GetMethod(nameof(Dummy_IntOrStringOrIntStringArg));

            ZilObject[] args = { new ZilFix(123) };

            var decoder = ArgDecoder.FromMethodInfo(methodInfo);
            object[] actual = decoder.Decode("dummy", ctx, args);
            object[] expected = { ctx, 123 };

            CollectionAssert.AreEqual(expected, actual);
        }

        [TestMethod]
        public void Test_EitherArg_String()
        {
            var methodInfo = GetMethod(nameof(Dummy_IntOrStringOrIntStringArg));

            ZilObject[] args = { ZilString.FromString("hi") };

            var decoder = ArgDecoder.FromMethodInfo(methodInfo);
            object[] actual = decoder.Decode("dummy", ctx, args);
            object[] expected = { ctx, "hi" };

            CollectionAssert.AreEqual(expected, actual);
        }

        [TestMethod]
        public void Test_EitherArg_IntString()
        {
            var methodInfo = GetMethod(nameof(Dummy_IntOrStringOrIntStringArg));

            ZilObject[] args = {
                new ZilList(new ZilObject[] { new ZilFix(23), ZilString.FromString("skidoo") })
            };

            var decoder = ArgDecoder.FromMethodInfo(methodInfo);
            object[] actual = decoder.Decode("dummy", ctx, args);
            object[] expected = {
                ctx,
                new IntStringStruct { arg1 = 23, arg2 = "skidoo" },
            };

            CollectionAssert.AreEqual(expected, actual);
        }

        private ZilObject Dummy_IntOrStringOrIntStringArg(Context ctx,
            [Either(typeof(int), typeof(string), typeof(IntStringStruct))] object foo)
        {
            return null;
        }

        [TestMethod]
        public void Test_EitherArg_Optional_Omitted()
        {
            var methodInfo = GetMethod(nameof(Dummy_OptionalIntOrStringThenAtomArg));

            ZilObject[] args = { ctx.TRUE };

            var decoder = ArgDecoder.FromMethodInfo(methodInfo);
            object[] actual = decoder.Decode("dummy", ctx, args);
            object[] expected = { ctx, null, ctx.TRUE };

            CollectionAssert.AreEqual(expected, actual);
        }

        [TestMethod]
        [ExpectedException(typeof(ArgumentTypeError))]
        public void Test_EitherArg_Optional_WrongType()
        {
            var methodInfo = GetMethod(nameof(Dummy_OptionalIntOrStringThenAtomArg));

            ZilObject[] args = { ctx.FALSE };

            var decoder = ArgDecoder.FromMethodInfo(methodInfo);
            object[] actual = decoder.Decode("dummy", ctx, args);
        }

        [TestMethod]
        public void Test_EitherArg_Optional_Provided()
        {
            var methodInfo = GetMethod(nameof(Dummy_OptionalIntOrStringThenAtomArg));

            ZilObject[] args = { new ZilFix(123), ctx.TRUE };

            var decoder = ArgDecoder.FromMethodInfo(methodInfo);
            object[] actual = decoder.Decode("dummy", ctx, args);
            object[] expected = { ctx, 123, ctx.TRUE };

            CollectionAssert.AreEqual(expected, actual);
        }

        private ZilObject Dummy_OptionalIntOrStringThenAtomArg(Context ctx,
            [Optional, Either(typeof(int), typeof(string))] object foo,
            ZilAtom bar)
        {
            return null;
        }

        [TestMethod]
        public void Test_SequenceArg()
        {
            var methodInfo = GetMethod(nameof(Dummy_IntStringSequenceArg));

            ZilObject[] args = { new ZilFix(1), ZilString.FromString("money") };

            var decoder = ArgDecoder.FromMethodInfo(methodInfo);
            object[] actual = decoder.Decode("dummy", ctx, args);
            object[] expected = { ctx, new IntStringSequence { arg1 = 1, arg2 = "money" } };

            CollectionAssert.AreEqual(expected, actual);
        }

        [ZilSequenceParam]
        private struct IntStringSequence
        {
            public int arg1;
            public string arg2;
        }

        private ZilObject Dummy_IntStringSequenceArg(Context ctx, IntStringSequence foo)
        {
            return null;
        }

        [TestMethod]
        public void Test_SequenceArrayArg()
        {
            var methodInfo = GetMethod(nameof(Dummy_IntStringSequenceArrayArg));

            ZilObject[] args = {
                new ZilFix(1), ZilString.FromString("money"),
                new ZilFix(2), ZilString.FromString("show"),
            };

            var decoder = ArgDecoder.FromMethodInfo(methodInfo);
            object[] actual = decoder.Decode("dummy", ctx, args);
            object[] expected = {
                ctx,
                new[] {
                    new IntStringSequence { arg1 = 1, arg2 = "money" },
                    new IntStringSequence { arg1 = 2, arg2 = "show" },
                },
            };

            Assert.AreEqual(2, actual.Length);
            Assert.AreEqual(expected[0], actual[0]);
            Assert.IsInstanceOfType(actual[1], typeof(IntStringSequence[]));
            CollectionAssert.AreEqual((IntStringSequence[])expected[1], (IntStringSequence[])actual[1]);
        }

        private ZilObject Dummy_IntStringSequenceArrayArg(Context ctx, IntStringSequence[] foo)
        {
            return null;
        }
    }
}
