using System;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Zilf;

namespace ZilfTests
{
    [TestClass]
    public class SyntaxTests
    {
        [TestMethod]
        public void TestVerb()
        {
            var ctx = new Context();
            var defn = (ZilList)Program.Evaluate(ctx, "(ABIDE = V-ABIDE)", true);

            var syntax = Syntax.Parse(null, defn, ctx);

            Assert.AreEqual("ABIDE", syntax.Verb.Atom.ToString());
            Assert.AreEqual(PartOfSpeech.Verb, syntax.Verb.PartOfSpeech & PartOfSpeech.Verb);

            Assert.AreEqual(0, syntax.NumObjects);
            Assert.AreEqual(null, syntax.Preposition1);
            Assert.AreEqual(null, syntax.FindFlag1);
            Assert.AreEqual(ScopeFlags.Default, syntax.Options1);
            Assert.AreEqual(null, syntax.Preposition2);
            Assert.AreEqual(null, syntax.FindFlag2);
            Assert.AreEqual(ScopeFlags.Default, syntax.Options2);

            Assert.AreEqual("V-ABIDE", syntax.Action.ToString());
            Assert.AreEqual(null, syntax.Preaction);
        }

        [TestMethod]
        public void TestVerbObject()
        {
            var ctx = new Context();
            var defn = (ZilList)Program.Evaluate(ctx, "(ABIDE OBJECT = V-ABIDE)", true);

            var syntax = Syntax.Parse(null, defn, ctx);

            Assert.AreEqual("ABIDE", syntax.Verb.Atom.ToString());
            Assert.AreEqual(PartOfSpeech.Verb, syntax.Verb.PartOfSpeech & PartOfSpeech.Verb);

            Assert.AreEqual(1, syntax.NumObjects);
            Assert.AreEqual(null, syntax.Preposition1);
            Assert.AreEqual(null, syntax.FindFlag1);
            Assert.AreEqual(ScopeFlags.Default, syntax.Options1);
            Assert.AreEqual(null, syntax.Preposition2);
            Assert.AreEqual(null, syntax.FindFlag2);
            Assert.AreEqual(ScopeFlags.Default, syntax.Options2);

            Assert.AreEqual("V-ABIDE", syntax.Action.ToString());
            Assert.AreEqual(null, syntax.Preaction);
        }

        [TestMethod]
        public void TestVerbPrepObject()
        {
            var ctx = new Context();
            var defn = (ZilList)Program.Evaluate(ctx, "(ABIDE BY OBJECT = V-ABIDE)", true);

            var syntax = Syntax.Parse(null, defn, ctx);

            Assert.AreEqual("ABIDE", syntax.Verb.Atom.ToString());
            Assert.AreEqual(PartOfSpeech.Verb, syntax.Verb.PartOfSpeech & PartOfSpeech.Verb);

            Assert.AreEqual(1, syntax.NumObjects);
            Assert.AreEqual("BY", syntax.Preposition1.Atom.ToString());
            Assert.AreEqual(PartOfSpeech.Preposition, syntax.Preposition1.PartOfSpeech & PartOfSpeech.Preposition);
            Assert.AreEqual(null, syntax.FindFlag1);
            Assert.AreEqual(ScopeFlags.Default, syntax.Options1);
            Assert.AreEqual(null, syntax.Preposition2);
            Assert.AreEqual(null, syntax.FindFlag2);
            Assert.AreEqual(ScopeFlags.Default, syntax.Options2);

            Assert.AreEqual("V-ABIDE", syntax.Action.ToString());
            Assert.AreEqual(null, syntax.Preaction);
        }

        [TestMethod]
        public void TestVerbObjectPrepObject()
        {
            var ctx = new Context();
            var defn = (ZilList)Program.Evaluate(ctx, "(TREAT OBJECT LIKE OBJECT = V-TREEHORN)", true);

            var syntax = Syntax.Parse(null, defn, ctx);

            Assert.AreEqual("TREAT", syntax.Verb.Atom.ToString());
            Assert.AreEqual(PartOfSpeech.Verb, syntax.Verb.PartOfSpeech & PartOfSpeech.Verb);

            Assert.AreEqual(2, syntax.NumObjects);
            Assert.AreEqual(null, syntax.Preposition1);
            Assert.AreEqual(null, syntax.FindFlag1);
            Assert.AreEqual(ScopeFlags.Default, syntax.Options1);
            Assert.AreEqual("LIKE", syntax.Preposition2.Atom.ToString());
            Assert.AreEqual(PartOfSpeech.Preposition, syntax.Preposition2.PartOfSpeech & PartOfSpeech.Preposition);
            Assert.AreEqual(null, syntax.FindFlag2);
            Assert.AreEqual(ScopeFlags.Default, syntax.Options2);

            Assert.AreEqual("V-TREEHORN", syntax.Action.ToString());
            Assert.AreEqual(null, syntax.Preaction);
        }

        [TestMethod]
        public void TestVerbPrepObjectPrepObject()
        {
            var ctx = new Context();
            var defn = (ZilList)Program.Evaluate(ctx, "(THROW OUT OBJECT FOR OBJECT = V-SWISS-WATCH)", true);

            var syntax = Syntax.Parse(null, defn, ctx);

            Assert.AreEqual("THROW", syntax.Verb.Atom.ToString());
            Assert.AreEqual(PartOfSpeech.Verb, syntax.Verb.PartOfSpeech & PartOfSpeech.Verb);

            Assert.AreEqual(2, syntax.NumObjects);
            Assert.AreEqual("OUT", syntax.Preposition1.Atom.ToString());
            Assert.AreEqual(PartOfSpeech.Preposition, syntax.Preposition1.PartOfSpeech & PartOfSpeech.Preposition);
            Assert.AreEqual(null, syntax.FindFlag1);
            Assert.AreEqual(ScopeFlags.Default, syntax.Options1);
            Assert.AreEqual("FOR", syntax.Preposition2.Atom.ToString());
            Assert.AreEqual(PartOfSpeech.Preposition, syntax.Preposition2.PartOfSpeech & PartOfSpeech.Preposition);
            Assert.AreEqual(null, syntax.FindFlag2);
            Assert.AreEqual(ScopeFlags.Default, syntax.Options2);

            Assert.AreEqual("V-SWISS-WATCH", syntax.Action.ToString());
            Assert.AreEqual(null, syntax.Preaction);
        }

        [TestMethod]
        public void TestVerbPrepObjectPrepObjectFindbit()
        {
            var ctx = new Context();
            var defn = (ZilList)Program.Evaluate(ctx, "(THROW OUT OBJECT FOR OBJECT (FIND PHONEBOOKBIT) = V-SWISS-WATCH)", true);

            var syntax = Syntax.Parse(null, defn, ctx);

            Assert.AreEqual("THROW", syntax.Verb.Atom.ToString());
            Assert.AreEqual(PartOfSpeech.Verb, syntax.Verb.PartOfSpeech & PartOfSpeech.Verb);

            Assert.AreEqual(2, syntax.NumObjects);
            Assert.AreEqual("OUT", syntax.Preposition1.Atom.ToString());
            Assert.AreEqual(PartOfSpeech.Preposition, syntax.Preposition1.PartOfSpeech & PartOfSpeech.Preposition);
            Assert.AreEqual(null, syntax.FindFlag1);
            Assert.AreEqual(ScopeFlags.Default, syntax.Options1);
            Assert.AreEqual("FOR", syntax.Preposition2.Atom.ToString());
            Assert.AreEqual(PartOfSpeech.Preposition, syntax.Preposition2.PartOfSpeech & PartOfSpeech.Preposition);
            Assert.AreEqual("PHONEBOOKBIT", syntax.FindFlag2.ToString());
            Assert.AreEqual(ScopeFlags.Default, syntax.Options2);

            Assert.AreEqual("V-SWISS-WATCH", syntax.Action.ToString());
            Assert.AreEqual(null, syntax.Preaction);
        }

        [TestMethod]
        public void TestVerbPrepObjectFindbit()
        {
            var ctx = new Context();
            var defn = (ZilList)Program.Evaluate(ctx, "(LOOK AROUND OBJECT (FIND DUMMYBIT) = V-LOOK-AROUND)", true);

            var syntax = Syntax.Parse(null, defn, ctx);

            Assert.AreEqual("LOOK", syntax.Verb.Atom.ToString());
            Assert.AreEqual(PartOfSpeech.Verb, syntax.Verb.PartOfSpeech & PartOfSpeech.Verb);

            Assert.AreEqual(1, syntax.NumObjects);
            Assert.AreEqual("AROUND", syntax.Preposition1.Atom.ToString());
            Assert.AreEqual(PartOfSpeech.Preposition, syntax.Preposition1.PartOfSpeech & PartOfSpeech.Preposition);
            Assert.AreEqual("DUMMYBIT", syntax.FindFlag1.ToString());
            Assert.AreEqual(ScopeFlags.Default, syntax.Options1);
            Assert.AreEqual(null, syntax.Preposition2);
            Assert.AreEqual(null, syntax.FindFlag2);
            Assert.AreEqual(ScopeFlags.Default, syntax.Options2);

            Assert.AreEqual("V-LOOK-AROUND", syntax.Action.ToString());
            Assert.AreEqual(null, syntax.Preaction);
        }

        [TestMethod]
        public void TestVerbPrepOmittedFindbit()
        {
            // if OBJECT is omitted, the preposition and FIND bits should be ignored and give a warning
            // TODO: add an assert to confirm the warning

            var ctx = new Context();
            var defn = (ZilList)Program.Evaluate(ctx, "(LOOK AROUND (FIND DUMMYBIT) = V-LOOK-AROUND)", true);

            var syntax = Syntax.Parse(null, defn, ctx);

            Assert.AreEqual("LOOK", syntax.Verb.Atom.ToString());
            Assert.AreEqual(PartOfSpeech.Verb, syntax.Verb.PartOfSpeech & PartOfSpeech.Verb);

            Assert.AreEqual(0, syntax.NumObjects);
            Assert.AreEqual(null, syntax.Preposition1);
            Assert.AreEqual(null, syntax.FindFlag1);
            Assert.AreEqual(ScopeFlags.Default, syntax.Options1);
            Assert.AreEqual(null, syntax.Preposition2);
            Assert.AreEqual(null, syntax.FindFlag2);
            Assert.AreEqual(ScopeFlags.Default, syntax.Options2);

            Assert.AreEqual("V-LOOK-AROUND", syntax.Action.ToString());
            Assert.AreEqual(null, syntax.Preaction);
        }
    }
}
