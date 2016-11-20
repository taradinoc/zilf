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
