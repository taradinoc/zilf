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

using System.Diagnostics;
using JetBrains.Annotations;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Zilf;
using Zilf.Interpreter;
using Zilf.Interpreter.Values;
using Zilf.ZModel;
using Zilf.ZModel.Vocab;
using System.Diagnostics.Contracts;
// ReSharper disable ExceptionNotDocumentedOptional

namespace ZilfTests
{
    [TestClass]
    public class SyntaxTests
    {
        [NotNull]
        static Syntax ParseSyntax([NotNull] Context ctx, [NotNull] string definition)
        {
            Contract.Requires(ctx != null);
            Contract.Requires(!string.IsNullOrWhiteSpace(definition));
            Contract.Ensures(Contract.Result<Syntax>() != null);

            var defn = (ZilList)Program.Evaluate(ctx, definition, true);
            Debug.Assert(defn != null);

            var syntax = Syntax.Parse(null, defn, ctx);
            return syntax;
        }

        [TestMethod]
        public void TestVerb()
        {
            var ctx = new Context();
            var syntax = ParseSyntax(ctx, "(ABIDE = V-ABIDE)");

            Assert.AreEqual("ABIDE", syntax.Verb.Atom.ToString());
            Assert.IsTrue(ctx.ZEnvironment.VocabFormat.IsVerb(syntax.Verb));

            Assert.AreEqual(0, syntax.NumObjects);
            Assert.IsNull(syntax.Preposition1);
            Assert.IsNull(syntax.FindFlag1);
            Assert.AreEqual(ScopeFlags.Original.Default, syntax.Options1);
            Assert.IsNull(syntax.Preposition2);
            Assert.IsNull(syntax.FindFlag2);
            Assert.AreEqual(ScopeFlags.Original.Default, syntax.Options2);

            Assert.AreEqual("V-ABIDE", syntax.Action.ToString());
            Assert.IsNull(syntax.Preaction);
            Assert.AreEqual("V?ABIDE", syntax.ActionName.ToString());
        }

        [TestMethod]
        public void TestVerbObject()
        {
            var ctx = new Context();
            var syntax = ParseSyntax(ctx, "(ABIDE OBJECT = V-ABIDE)");

            Assert.AreEqual("ABIDE", syntax.Verb.Atom.ToString());
            Assert.IsTrue(ctx.ZEnvironment.VocabFormat.IsVerb(syntax.Verb));

            Assert.AreEqual(1, syntax.NumObjects);
            Assert.IsNull(syntax.Preposition1);
            Assert.IsNull(syntax.FindFlag1);
            Assert.AreEqual(ScopeFlags.Original.Default, syntax.Options1);
            Assert.IsNull(syntax.Preposition2);
            Assert.IsNull(syntax.FindFlag2);
            Assert.AreEqual(ScopeFlags.Original.Default, syntax.Options2);

            Assert.AreEqual("V-ABIDE", syntax.Action.ToString());
            Assert.IsNull(syntax.Preaction);
            Assert.AreEqual("V?ABIDE", syntax.ActionName.ToString());
        }

        [TestMethod]
        public void TestVerbPrepObject()
        {
            var ctx = new Context();
            var syntax = ParseSyntax(ctx, "(ABIDE BY OBJECT = V-ABIDE)");

            Assert.AreEqual("ABIDE", syntax.Verb.Atom.ToString());
            Assert.IsTrue(ctx.ZEnvironment.VocabFormat.IsVerb(syntax.Verb));

            Assert.AreEqual(1, syntax.NumObjects);
            Assert.IsNotNull(syntax.Preposition1);
            Assert.AreEqual("BY", syntax.Preposition1.Atom.ToString());
            Assert.IsTrue(ctx.ZEnvironment.VocabFormat.IsPreposition(syntax.Preposition1));
            Assert.IsNull(syntax.FindFlag1);
            Assert.AreEqual(ScopeFlags.Original.Default, syntax.Options1);
            Assert.IsNull(syntax.Preposition2);
            Assert.IsNull(syntax.FindFlag2);
            Assert.AreEqual(ScopeFlags.Original.Default, syntax.Options2);

            Assert.AreEqual("V-ABIDE", syntax.Action.ToString());
            Assert.IsNull(syntax.Preaction);
            Assert.AreEqual("V?ABIDE", syntax.ActionName.ToString());
        }

        [TestMethod]
        public void TestVerbObjectPrepObject()
        {
            var ctx = new Context();
            var syntax = ParseSyntax(ctx, "(TREAT OBJECT LIKE OBJECT = V-TREEHORN)");

            Assert.AreEqual("TREAT", syntax.Verb.Atom.ToString());
            Assert.IsTrue(ctx.ZEnvironment.VocabFormat.IsVerb(syntax.Verb));

            Assert.AreEqual(2, syntax.NumObjects);
            Assert.IsNull(syntax.Preposition1);
            Assert.IsNull(syntax.FindFlag1);
            Assert.AreEqual(ScopeFlags.Original.Default, syntax.Options1);
            Assert.IsNotNull(syntax.Preposition2);
            Assert.AreEqual("LIKE", syntax.Preposition2.Atom.ToString());
            Assert.IsTrue(ctx.ZEnvironment.VocabFormat.IsPreposition(syntax.Preposition2));
            Assert.IsNull(syntax.FindFlag2);
            Assert.AreEqual(ScopeFlags.Original.Default, syntax.Options2);

            Assert.AreEqual("V-TREEHORN", syntax.Action.ToString());
            Assert.IsNull(syntax.Preaction);
            Assert.AreEqual("V?TREEHORN", syntax.ActionName.ToString());
        }

        [TestMethod]
        public void TestVerbPrepObjectPrepObject()
        {
            var ctx = new Context();
            var syntax = ParseSyntax(ctx, "(THROW OUT OBJECT FOR OBJECT = V-SWISS-WATCH)");

            Assert.AreEqual("THROW", syntax.Verb.Atom.ToString());
            Assert.IsTrue(ctx.ZEnvironment.VocabFormat.IsVerb(syntax.Verb));

            Assert.AreEqual(2, syntax.NumObjects);
            Assert.IsNotNull(syntax.Preposition1);
            Assert.AreEqual("OUT", syntax.Preposition1.Atom.ToString());
            Assert.IsTrue(ctx.ZEnvironment.VocabFormat.IsPreposition(syntax.Preposition1));
            Assert.IsNull(syntax.FindFlag1);
            Assert.AreEqual(ScopeFlags.Original.Default, syntax.Options1);
            Assert.IsNotNull(syntax.Preposition2);
            Assert.AreEqual("FOR", syntax.Preposition2.Atom.ToString());
            Assert.IsTrue(ctx.ZEnvironment.VocabFormat.IsPreposition(syntax.Preposition2));
            Assert.IsNull(syntax.FindFlag2);
            Assert.AreEqual(ScopeFlags.Original.Default, syntax.Options2);

            Assert.AreEqual("V-SWISS-WATCH", syntax.Action.ToString());
            Assert.IsNull(syntax.Preaction);
            Assert.AreEqual("V?SWISS-WATCH", syntax.ActionName.ToString());
        }

        [TestMethod]
        public void TestVerbPrepObjectPrepObjectFindbit()
        {
            var ctx = new Context();
            var syntax = ParseSyntax(ctx, "(THROW OUT OBJECT FOR OBJECT (FIND PHONEBOOKBIT) = V-SWISS-WATCH)");

            Assert.AreEqual("THROW", syntax.Verb.Atom.ToString());
            Assert.IsTrue(ctx.ZEnvironment.VocabFormat.IsVerb(syntax.Verb));

            Assert.AreEqual(2, syntax.NumObjects);
            Assert.IsNotNull(syntax.Preposition1);
            Assert.AreEqual("OUT", syntax.Preposition1.Atom.ToString());
            Assert.IsTrue(ctx.ZEnvironment.VocabFormat.IsPreposition(syntax.Preposition1));
            Assert.IsNull(syntax.FindFlag1);
            Assert.AreEqual(ScopeFlags.Original.Default, syntax.Options1);
            Assert.IsNotNull(syntax.Preposition2);
            Assert.AreEqual("FOR", syntax.Preposition2.Atom.ToString());
            Assert.IsTrue(ctx.ZEnvironment.VocabFormat.IsPreposition(syntax.Preposition2));
            Assert.IsNotNull(syntax.FindFlag2);
            Assert.AreEqual("PHONEBOOKBIT", syntax.FindFlag2.ToString());
            Assert.AreEqual(ScopeFlags.Original.Default, syntax.Options2);

            Assert.AreEqual("V-SWISS-WATCH", syntax.Action.ToString());
            Assert.IsNull(syntax.Preaction);
            Assert.AreEqual("V?SWISS-WATCH", syntax.ActionName.ToString());
        }

        [TestMethod]
        public void TestVerbPrepObjectFindbit()
        {
            var ctx = new Context();
            var syntax = ParseSyntax(ctx, "(LOOK AROUND OBJECT (FIND DUMMYBIT) = V-LOOK-AROUND)");

            Assert.AreEqual("LOOK", syntax.Verb.Atom.ToString());
            Assert.IsTrue(ctx.ZEnvironment.VocabFormat.IsVerb(syntax.Verb));

            Assert.AreEqual(1, syntax.NumObjects);
            Assert.IsNotNull(syntax.Preposition1);
            Assert.AreEqual("AROUND", syntax.Preposition1.Atom.ToString());
            Assert.IsTrue(ctx.ZEnvironment.VocabFormat.IsPreposition(syntax.Preposition1));
            Assert.IsNotNull(syntax.FindFlag1);
            Assert.AreEqual("DUMMYBIT", syntax.FindFlag1.ToString());
            Assert.AreEqual(ScopeFlags.Original.Default, syntax.Options1);
            Assert.IsNull(syntax.Preposition2);
            Assert.IsNull(syntax.FindFlag2);
            Assert.AreEqual(ScopeFlags.Original.Default, syntax.Options2);

            Assert.AreEqual("V-LOOK-AROUND", syntax.Action.ToString());
            Assert.IsNull(syntax.Preaction);
            Assert.AreEqual("V?LOOK-AROUND", syntax.ActionName.ToString());
        }

        [TestMethod]
        public void TestVerbPrepOmittedFindbit()
        {
            // if OBJECT is omitted, the preposition and FIND bits should be ignored and give a warning
            // TODO: add an assert to confirm the warning

            var ctx = new Context();
            var syntax = ParseSyntax(ctx, "(LOOK AROUND (FIND DUMMYBIT) = V-LOOK-AROUND)");

            Assert.AreEqual("LOOK", syntax.Verb.Atom.ToString());
            Assert.IsTrue(ctx.ZEnvironment.VocabFormat.IsVerb(syntax.Verb));

            Assert.AreEqual(0, syntax.NumObjects);
            Assert.IsNull(syntax.Preposition1);
            Assert.IsNull(syntax.FindFlag1);
            Assert.AreEqual(ScopeFlags.Original.Default, syntax.Options1);
            Assert.IsNull(syntax.Preposition2);
            Assert.IsNull(syntax.FindFlag2);
            Assert.AreEqual(ScopeFlags.Original.Default, syntax.Options2);

            Assert.AreEqual("V-LOOK-AROUND", syntax.Action.ToString());
            Assert.IsNull(syntax.Preaction);
            Assert.AreEqual("V?LOOK-AROUND", syntax.ActionName.ToString());
        }

        [TestMethod]
        public void TestCustomActionName()
        {
            var ctx = new Context();
            var syntax = ParseSyntax(ctx, "(LOOK BEHIND OBJECT = V-SEARCH <> LOOK-BEHIND)");

            Assert.AreEqual("LOOK", syntax.Verb.Atom.ToString());
            Assert.IsTrue(ctx.ZEnvironment.VocabFormat.IsVerb(syntax.Verb));

            Assert.AreEqual(1, syntax.NumObjects);
            Assert.IsNotNull(syntax.Preposition1);
            Assert.AreEqual("BEHIND", syntax.Preposition1.Atom.ToString());
            Assert.IsTrue(ctx.ZEnvironment.VocabFormat.IsPreposition(syntax.Preposition1));
            Assert.IsNull(syntax.FindFlag1);
            Assert.AreEqual(ScopeFlags.Original.Default, syntax.Options1);
            Assert.IsNull(syntax.Preposition2);
            Assert.IsNull(syntax.FindFlag2);
            Assert.AreEqual(ScopeFlags.Original.Default, syntax.Options2);

            Assert.AreEqual("V-SEARCH", syntax.Action.ToString());
            Assert.IsNull(syntax.Preaction);
            Assert.AreEqual("V?LOOK-BEHIND", syntax.ActionName.ToString());
        }

        [TestMethod]
        public void TestSynonyms()
        {
            var ctx = new Context();
            var syntax = ParseSyntax(ctx, "(LOOK (STARE GAZE) AT OBJECT = V-EXAMINE)");

            Assert.AreEqual("LOOK", syntax.Verb.Atom.ToString());
            Assert.IsTrue(ctx.ZEnvironment.VocabFormat.IsVerb(syntax.Verb));

            Assert.AreEqual(1, syntax.NumObjects);
            Assert.IsNotNull(syntax.Preposition1);
            Assert.AreEqual("AT", syntax.Preposition1.Atom.ToString());
            Assert.IsTrue(ctx.ZEnvironment.VocabFormat.IsPreposition(syntax.Preposition1));
            Assert.IsNull(syntax.FindFlag1);
            Assert.AreEqual(ScopeFlags.Original.Default, syntax.Options1);
            Assert.IsNull(syntax.Preposition2);
            Assert.IsNull(syntax.FindFlag2);
            Assert.AreEqual(ScopeFlags.Original.Default, syntax.Options2);

            Assert.AreEqual("V-EXAMINE", syntax.Action.ToString());
            Assert.IsNull(syntax.Preaction);
            Assert.AreEqual("V?EXAMINE", syntax.ActionName.ToString());

            Assert.AreEqual(2, syntax.Synonyms.Count);
            Assert.AreEqual(ZilAtom.Parse("STARE", ctx), syntax.Synonyms[0]);
            Assert.AreEqual(ZilAtom.Parse("GAZE", ctx), syntax.Synonyms[1]);
        }
    }
}
