/* Copyright 2010-2018 Jesse McGrew
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
using Zilf.Interpreter;
using Zilf.Interpreter.Values;
using Zilf.ZModel.Values;

namespace Zilf.Tests.Interpreter
{
    [TestClass, TestCategory("Interpreter")]
    public class MdlZilTests
    {
        [TestMethod]
        public void MdlZil_SETG_Is_GLOBAL_At_Top_Level()
        {
            var ctx = new Context();

            TestHelpers.Evaluate(ctx, "<FILE-FLAGS MDL-ZIL?>");
            TestHelpers.Evaluate(ctx, "<SETG FOO 123>");

            var fooAtom = ZilAtom.Parse("FOO", ctx);

            Assert.IsInstanceOfType(ctx.GetZVal(fooAtom), typeof(ZilGlobal));
            Assert.IsNull(ctx.GetGlobalVal(fooAtom));
        }

        [TestMethod]
        public void MdlZil_SETG_Is_SETG_In_A_Function()
        {
            var ctx = new Context();

            TestHelpers.Evaluate(ctx, "<FILE-FLAGS MDL-ZIL?>");
            TestHelpers.Evaluate(ctx, "<DEFINE20 FUNC () <SETG FOO 123>>");
            TestHelpers.Evaluate(ctx, "<FUNC>");

            var fooAtom = ZilAtom.Parse("FOO", ctx);

            Assert.IsInstanceOfType(ctx.GetGlobalVal(fooAtom), typeof(ZilFix));
            Assert.IsNull(ctx.GetZVal(fooAtom));
        }
    }
}
