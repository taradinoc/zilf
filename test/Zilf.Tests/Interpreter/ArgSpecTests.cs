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

using System.Linq;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Zilf.Interpreter;
using Zilf.Interpreter.Values;
using Zilf.Language;

namespace Zilf.Tests.Interpreter
{
    [TestClass, TestCategory("Interpreter"), TestCategory("Arguments")]
    public class ArgSpecTests
    {
        [TestMethod]
        public void ActivationAtom_Becomes_NAME_Argument_In_ZilListBody()
        {
            var ctx = new Context();

            var spec = ArgSpec.Parse("test", ZilAtom.Parse("FOO", ctx), ZilAtom.Parse("ACT", ctx), []);

            TestHelpers.AssertStructurallyEqual(
                [
                    ZilString.FromString("NAME"),
                    ZilAtom.Parse("ACT", ctx)
                ],
                [.. spec.AsZilListBody()]);
        }

        [TestMethod]
        public void ARGS_Is_Included_In_ZilListBody()
        {
            var ctx = new Context();

            var spec = ArgSpec.Parse("test", ZilAtom.Parse("FOO", ctx), null, [ZilString.FromString("ARGS"), ZilAtom.Parse("A", ctx)]);

            TestHelpers.AssertStructurallyEqual(
                [
                    ZilString.FromString("ARGS"),
                    ZilAtom.Parse("A", ctx)
                ],
                [.. spec.AsZilListBody()]);
        }

        [TestMethod]
        public void TUPLE_Is_Included_In_ZilListBody()
        {
            var ctx = new Context();

            var spec = ArgSpec.Parse("test", ZilAtom.Parse("FOO", ctx), null, [ZilString.FromString("TUPLE"), ZilAtom.Parse("A", ctx)]);

            TestHelpers.AssertStructurallyEqual(
                [
                    ZilString.FromString("TUPLE"),
                    ZilAtom.Parse("A", ctx)
                ],
                [.. spec.AsZilListBody()]);
        }

        [TestMethod]
        public void ARGS_And_TUPLE_Can_Be_ADECLs()
        {
            var ctx = new Context();

            ArgSpec.Parse("test", ZilAtom.Parse("FOO", ctx), null,
            [
                ZilString.FromString("ARGS"),
                new ZilAdecl(
                    ZilAtom.Parse("A", ctx),
                    ctx.GetStdAtom(StdAtom.LIST))
            ]);

            ArgSpec.Parse("test", ZilAtom.Parse("FOO", ctx), null,
            [
                ZilString.FromString("TUPLE"),
                new ZilAdecl(
                    ZilAtom.Parse("A", ctx),
                    ctx.GetStdAtom(StdAtom.LIST))
            ]);
        }

        [TestMethod]
        public void AsZilListBody_Returns_ADECLs_For_Arguments_With_DECLs()
        {
            var ctx = new Context();

            var spec = ArgSpec.Parse("test", ZilAtom.Parse("FOO", ctx), null,
            [
                new ZilAdecl(
                    ZilAtom.Parse("A1", ctx),
                    ctx.GetStdAtom(StdAtom.FIX)),
                new ZilAdecl(
                    new ZilForm([
                        ctx.GetStdAtom(StdAtom.QUOTE),
                        ZilAtom.Parse("A2", ctx)
                    ]),
                    ctx.GetStdAtom(StdAtom.FORM)),
                ZilString.FromString("TUPLE"),
                new ZilAdecl(
                    ZilAtom.Parse("A3", ctx),
                    ctx.GetStdAtom(StdAtom.LIST))
            ]);

            TestHelpers.AssertStructurallyEqual(
                [
                    new ZilAdecl(
                        ZilAtom.Parse("A1", ctx),
                        ctx.GetStdAtom(StdAtom.FIX)),
                    new ZilAdecl(
                        new ZilForm([
                            ctx.GetStdAtom(StdAtom.QUOTE),
                            ZilAtom.Parse("A2", ctx)
                        ]),
                        ctx.GetStdAtom(StdAtom.FORM)),
                    ZilString.FromString("TUPLE"),
                    new ZilAdecl(
                        ZilAtom.Parse("A3", ctx),
                        ctx.GetStdAtom(StdAtom.LIST))
                ],
                [.. spec.AsZilListBody()]);
        }


        [TestMethod]
        public void VALUE_Is_Included_In_ZilListBody()
        {
            var ctx = new Context();

            var spec = ArgSpec.Parse("test", ZilAtom.Parse("FOO", ctx), null,
            [
                ZilString.FromString("AUX"),
                ZilAtom.Parse("X", ctx),
                ZilString.FromString("NAME"),
                ZilAtom.Parse("N", ctx),
                ZilString.FromString("VALUE"),
                new ZilForm(
                [
                    ctx.GetStdAtom(StdAtom.OR),
                    ctx.GetStdAtom(StdAtom.FIX),
                    ctx.GetStdAtom(StdAtom.FALSE)
                ])
            ]);

            TestHelpers.AssertStructurallyEqual(
                [
                    ZilString.FromString("AUX"),
                    ZilAtom.Parse("X", ctx),
                    ZilString.FromString("NAME"),
                    ZilAtom.Parse("N", ctx),
                    ZilString.FromString("VALUE"),
                    new ZilForm(
                    [
                        ctx.GetStdAtom(StdAtom.OR),
                        ctx.GetStdAtom(StdAtom.FIX),
                        ctx.GetStdAtom(StdAtom.FALSE)
                    ])
                ],
                [.. spec.AsZilListBody()]);
        }

        [TestMethod]
        public void BIND_Is_Included_In_ZilListBody()
        {
            var ctx = new Context();

            var args = Program.Parse(ctx, @"""BIND"" B X ""NAME"" N").ToArray();

            var spec = ArgSpec.Parse("test", ZilAtom.Parse("FOO", ctx), null, args);

            TestHelpers.AssertStructurallyEqual(args, [.. spec.AsZilListBody()]);
        }

        [TestMethod]
        public void Extra_Elements_In_Binding_Should_Throw()
        {
            var ctx = new Context();

            var args = Program.Parse(ctx, @"""OPT"" (X <> Y)").ToArray();

            Assert.ThrowsException<InterpreterError>(() =>
            {
                ArgSpec.Parse("test", ZilAtom.Parse("FOO", ctx), null, args);
            });
        }
    }
}
