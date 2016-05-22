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
            return typeof(ArgDecoderTests).GetMethod(name, BindingFlags.Instance | BindingFlags.NonPublic);
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
            Assert.IsInstanceOfType(expected[1], typeof(ZilObject[]));
            CollectionAssert.AreEqual((ZilObject[])expected[1], (ZilObject[])actual[1]);
        }

        private ZilObject Dummy_ZilObjectArrayArg(Context ctx, ZilObject[] args)
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
        public void Test_OptionalArg_AnotherType()
        {
            var methodInfo = GetMethod(nameof(Dummy_OptionalIntThenStringArg));

            ZilObject[] args = { ZilString.FromString("hello") };

            var decoder = ArgDecoder.FromMethodInfo(methodInfo);
            object[] actual = decoder.Decode("dummy", ctx, args);
            object[] expected = { ctx, 69105, "hello" };

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

        #region WrapMethodInfo

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
        private ZilObject Dummy_MdlZilRedirect_From(Context ctx)
        {
            return ctx.FALSE;
        }

        private static ZilObject Dummy_MdlZilRedirect_To(Context ctx, int num)
        {
            return new ZilFix(num * 2);
        }

        #endregion

        #region Messages

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

            Assert.Fail("Expected ArgumentTypeError");
        }

        #endregion
    }
}
