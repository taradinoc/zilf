using System;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Zilf.Interpreter;
using Zilf.Interpreter.Values;
using Zilf.Language;
using System.Linq;

namespace ZilfTests.Interpreter
{
    [TestClass]
    public class ArgSpecTests
    {
        [TestMethod]
        public void ActivationAtom_Becomes_NAME_Argument_In_ZilListBody()
        {
            var ctx = new Context();

            var spec = new ArgSpec(ZilAtom.Parse("FOO", ctx), ZilAtom.Parse("ACT", ctx), new ZilObject[0]);

            CollectionAssert.AreEqual(
                new ZilObject[]
                {
                    new ZilString("NAME"),
                    ZilAtom.Parse("ACT", ctx),
                },
                spec.AsZilListBody().ToArray());
        }

        [TestMethod]
        public void ARGS_Is_Included_In_ZilListBody()
        {
            var ctx = new Context();

            var spec = new ArgSpec(ZilAtom.Parse("FOO", ctx), null, new ZilObject[] { new ZilString("ARGS"), ZilAtom.Parse("A", ctx) });

            CollectionAssert.AreEqual(
                new ZilObject[]
                {
                    new ZilString("ARGS"),
                    ZilAtom.Parse("A", ctx),
                },
                spec.AsZilListBody().ToArray());
        }

        [TestMethod]
        public void TUPLE_Is_Included_In_ZilListBody()
        {
            var ctx = new Context();

            var spec = new ArgSpec(ZilAtom.Parse("FOO", ctx), null, new ZilObject[] { new ZilString("TUPLE"), ZilAtom.Parse("A", ctx) });

            CollectionAssert.AreEqual(
                new ZilObject[]
                {
                    new ZilString("TUPLE"),
                    ZilAtom.Parse("A", ctx),
                },
                spec.AsZilListBody().ToArray());
        }

        [TestMethod]
        public void ARGS_And_TUPLE_Can_Be_ADECLs()
        {
            var ctx = new Context();

            var spec1 = new ArgSpec(ZilAtom.Parse("FOO", ctx), null, new ZilObject[]
            {
                new ZilString("ARGS"),
                new ZilAdecl(
                    ZilAtom.Parse("A", ctx),
                    ctx.GetStdAtom(StdAtom.LIST)),
            });

            var spec2 = new ArgSpec(ZilAtom.Parse("FOO", ctx), null, new ZilObject[]
            {
                new ZilString("TUPLE"),
                new ZilAdecl(
                    ZilAtom.Parse("A", ctx),
                    ctx.GetStdAtom(StdAtom.LIST)),
            });
        }

        [TestMethod]
        public void AsZilListBody_Returns_ADECLs_For_Arguments_With_DECLs()
        {
            var ctx = new Context();

            var spec = new ArgSpec(ZilAtom.Parse("FOO", ctx), null, new ZilObject[]
            {
                new ZilAdecl(
                    ZilAtom.Parse("A1", ctx),
                    ctx.GetStdAtom(StdAtom.FIX)),
                new ZilAdecl(
                    new ZilForm(new ZilObject[] {
                        ctx.GetStdAtom(StdAtom.QUOTE),
                        ZilAtom.Parse("A2", ctx),
                    }),
                    ctx.GetStdAtom(StdAtom.FORM)),
                new ZilString("TUPLE"),
                new ZilAdecl(
                    ZilAtom.Parse("A3", ctx),
                    ctx.GetStdAtom(StdAtom.LIST)),
            });

            CollectionAssert.AreEqual(
                new ZilObject[] {
                    new ZilAdecl(
                        ZilAtom.Parse("A1", ctx),
                        ctx.GetStdAtom(StdAtom.FIX)),
                    new ZilAdecl(
                        new ZilForm(new ZilObject[] {
                            ctx.GetStdAtom(StdAtom.QUOTE),
                            ZilAtom.Parse("A2", ctx),
                        }),
                        ctx.GetStdAtom(StdAtom.FORM)),
                    new ZilString("TUPLE"),
                    new ZilAdecl(
                        ZilAtom.Parse("A3", ctx),
                        ctx.GetStdAtom(StdAtom.LIST)),
                },
                spec.AsZilListBody().ToArray());
        }


        [TestMethod]
        public void VALUE_Is_Included_In_ZilListBody()
        {
            var ctx = new Context();

            var spec = new ArgSpec(ZilAtom.Parse("FOO", ctx), null, new ZilObject[]
            {
                new ZilString("AUX"),
                ZilAtom.Parse("X", ctx),
                new ZilString("NAME"),
                ZilAtom.Parse("N", ctx),
                new ZilString("VALUE"),
                new ZilForm(new ZilObject[]
                {
                    ctx.GetStdAtom(StdAtom.OR),
                    ctx.GetStdAtom(StdAtom.FIX),
                    ctx.GetStdAtom(StdAtom.FALSE),
                }),
            });

            CollectionAssert.AreEqual(
                new ZilObject[]
                {
                    new ZilString("AUX"),
                    ZilAtom.Parse("X", ctx),
                    new ZilString("NAME"),
                    ZilAtom.Parse("N", ctx),
                    new ZilString("VALUE"),
                    new ZilForm(new ZilObject[]
                    {
                        ctx.GetStdAtom(StdAtom.OR),
                        ctx.GetStdAtom(StdAtom.FIX),
                        ctx.GetStdAtom(StdAtom.FALSE),
                    }),
                },
                spec.AsZilListBody().ToArray());
        }
    }
}
