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

using System.IO;
using JetBrains.Annotations;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Zilf.Interpreter;
using Zilf.Interpreter.Values;
using Zilf.Language;

namespace Zilf.Tests.Interpreter
{
    [TestClass, TestCategory("Interpreter")]
    public class OutputTests
    {
        [NotNull]
        static ZilStringChannel MakeTestChannel([NotNull] Context ctx)
        {
            var channel = new ZilStringChannel(FileAccess.Write);
            ctx.SetLocalVal(ctx.GetStdAtom(StdAtom.OUTCHAN), channel);
            return channel;
        }

        [TestMethod]
        public void TestPRINC()
        {
            var ctx = new Context();
            var chan = MakeTestChannel(ctx);

            TestHelpers.Evaluate(ctx, @"<PRINC [!\H ""ello"" WORLD]>");

            Assert.AreEqual("[H ello WORLD]", chan.String);
        }

        [TestMethod]
        public void TestPRIN1()
        {
            var ctx = new Context();
            var chan = MakeTestChannel(ctx);

            TestHelpers.Evaluate(ctx, @"<PRIN1 [!\H ""ello"" WORLD]>");

            Assert.AreEqual(@"[!\H ""ello"" WORLD]", chan.String);
        }

        [TestMethod]
        public void TestPRINT_MANY()
        {
            var ctx = new Context();
            var chan = MakeTestChannel(ctx);

            TestHelpers.Evaluate(ctx, @"<PRINT-MANY .OUTCHAN PRINC ""Hello"" !\! PRMANY-CRLF>");
            TestHelpers.Evaluate(ctx, @"<PRINT-MANY .OUTCHAN PRIN1 ""string"" !\c PRMANY-CRLF>");

            Assert.AreEqual("Hello!\n\"string\"!\\c\n", chan.String);
        }
    }
}
