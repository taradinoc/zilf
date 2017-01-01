/* Copyright 2010-2016 Jesse McGrew
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
using Zilf;
using Zilf.Interpreter;
using Zilf.Interpreter.Values;

namespace ZilfTests.Interpreter
{
    [TestClass]
    public class TemplateTests
    {
        Context ctx;

        [TestInitialize]
        public void Initialize()
        {
            ctx = new Context();
        }

        [TestMethod]
        public void Parse_Function_Should_Support_Templates()
        {
            var result = Program.Parse(ctx, "<* 2 {0}>", new ZilFix(512)).Single();

            Assert.IsInstanceOfType(result, typeof(ZilForm));

            var evald = result.Eval(ctx);

            Assert.AreEqual(new ZilFix(1024), evald);
        }

        [TestMethod]
        public void Templates_Can_Insert_Multiple_Values()
        {
            var result = Program.Parse(ctx, "<BIND () {1:SPLICE} <+ !'{0}>>",
                new ZilVector(new ZilFix(1), new ZilFix(20)),
                new ZilList(Program.Parse(ctx, @"<PRINT ""Hi!""> <CRLF> <SETG DONE T>"))
                ).Single();

            Assert.IsInstanceOfType(result, typeof(ZilForm));

            var evald = result.Eval(ctx);

            Assert.AreEqual(new ZilFix(21), evald);

            Assert.AreEqual(ctx.TRUE, ctx.GetGlobalVal(ZilAtom.Parse("DONE", ctx)));
        }
    }
}
