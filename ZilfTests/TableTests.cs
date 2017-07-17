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
using Zilf;
using Zilf.Interpreter;
using Zilf.ZModel.Values;

namespace ZilfTests
{
    [TestClass]
    public class TableTests
    {
        [TestMethod]
        public void TestITABLE()
        {
            var ctx = new Context();
            var table = (ZilTable)Program.Evaluate(ctx, "<ITABLE 3 2 1 0>", true);

            Assert.IsNotNull(table);
            Assert.AreEqual(9, table.ElementCount);
        }

        [TestMethod]
        public void TestTABLE()
        {
            var ctx = new Context();
            var table = (ZilTable)Program.Evaluate(ctx, "<TABLE 3 2 1 0>", true);

            Assert.IsNotNull(table);
            Assert.AreEqual(4, table.ElementCount);
        }

        [TestMethod]
        public void TestLTABLE()
        {
            var ctx = new Context();
            var table = (ZilTable)Program.Evaluate(ctx, "<LTABLE 3 2 1 0>", true);

            Assert.IsNotNull(table);
            Assert.AreEqual(5, table.ElementCount);
        }
    }
}
