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

using System;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Zilf.Interpreter;
using System.Collections.Generic;
using System.Linq;

namespace ZilfTests.Interpreter
{
    [TestClass]
    public class WeakCountingSetTests
    {
        [TestMethod]
        public void WeakMultiSet_Should_Count_Objects()
        {
            var set = new WeakCountingSet<object>();

            var foo = new object();
            var bar = new object();

            set.Add(foo);
            set.Add(foo);

            set.Add(bar);
            set.Add(bar);
            set.Remove(bar);
            set.Add(bar);
            set.Remove(bar);

            CollectionAssert.AreEquivalent(new[] { foo, bar }, set.ToArray());

            set.Remove(bar);

            CollectionAssert.AreEquivalent(new[] { foo }, set.ToArray());

            foo = bar = null;
            GC.Collect();

            CollectionAssert.AreEqual(new object[] { }, set.ToArray());
        }
    }
}
