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
using System.Collections.Generic;
using System.Diagnostics;
using System.Diagnostics.Contracts;
using System.Linq;
using JetBrains.Annotations;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Zilf.Interpreter;
using Zilf.Interpreter.Values;
using Zilf.Language;

namespace Zilf.Tests.Interpreter
{
    /// <summary>
    /// Tests for the Parser class
    /// </summary>
    [TestClass, TestCategory("Interpreter"), TestCategory("Parsing")]
    public class ParserTests
    {
        class TestParserSite : IParserSite
        {
            readonly Dictionary<string, ZilAtom> atoms = new Dictionary<string, ZilAtom>();

            public Func<ZilObject, ZilObject> OnEvaluate { get; set; }

            public string CurrentFilePath => "sample.zil";

            public ZilObject FALSE { get; } = new ZilFalse(new ZilList(null, null));

            public ZilAtom ParseAtom(string text)
            {
                if (atoms.TryGetValue(text, out var result))
                    return result;

                result = new ZilAtom(text, null, StdAtom.None);
                atoms.Add(text, result);
                return result;
            }

            public ZilAtom GetTypeAtom(ZilObject zo)
            {
                if (zo is ZilHash hash)
                    return hash.Type;

                Debug.Assert(zo.StdTypeAtom != StdAtom.None);
                return ParseAtom(zo.StdTypeAtom.ToString());
            }

            public ZilObject ChangeType(ZilObject zo, ZilAtom type)
            {
                return new ZilHash(type, zo.PrimType, zo);
            }

            /// <exception cref="InvalidOperationException"><see cref="OnEvaluate"/> is not set.</exception>
            public ZilObject Evaluate(ZilObject zo)
            {
                var handler = OnEvaluate ?? throw new InvalidOperationException($"{nameof(OnEvaluate)} not set");
                return handler(zo);
            }

            public ZilObject GetGlobalVal(ZilAtom atom)
            {
                return null;
            }

            public void AddStdAtom([NotNull] string name, StdAtom stdAtom)
            {
                Contract.Requires(name != null);
                atoms.Add(name, new ZilAtom(name, null, stdAtom));
            }
        }

        TestParserSite site;

        [TestInitialize]
        public void Initialize()
        {
            site = new TestParserSite();
        }

        [TestMethod]
        public void TestParsingNumber()
        {
            var parser = new Parser(site);

            var result = parser.Parse("123").ToArray();
            Assert.AreEqual(2, result.Length);
            Assert.AreEqual(ParserOutputType.Object, result[0].Type);
            TestHelpers.AssertStructurallyEqual(new ZilFix(123), result[0].Object);
            Assert.AreEqual(ParserOutputType.EndOfInput, result[1].Type);

            result = parser.Parse("-123").ToArray();
            Assert.AreEqual(2, result.Length);
            Assert.AreEqual(ParserOutputType.Object, result[0].Type);
            TestHelpers.AssertStructurallyEqual(new ZilFix(-123), result[0].Object);
            Assert.AreEqual(ParserOutputType.EndOfInput, result[1].Type);

            result = parser.Parse("+123").ToArray();
            Assert.AreEqual(2, result.Length);
            Assert.AreEqual(ParserOutputType.Object, result[0].Type);
            TestHelpers.AssertStructurallyEqual(new ZilFix(123), result[0].Object);
            Assert.AreEqual(ParserOutputType.EndOfInput, result[1].Type);

            result = parser.Parse("*377*").ToArray();
            Assert.AreEqual(2, result.Length);
            Assert.AreEqual(ParserOutputType.Object, result[0].Type);
            TestHelpers.AssertStructurallyEqual(new ZilFix(255), result[0].Object);
            Assert.AreEqual(ParserOutputType.EndOfInput, result[1].Type);

            result = parser.Parse("#2 1010").ToArray();
            Assert.AreEqual(2, result.Length);
            Assert.AreEqual(ParserOutputType.Object, result[0].Type);
            TestHelpers.AssertStructurallyEqual(new ZilFix(10), result[0].Object);
            Assert.AreEqual(ParserOutputType.EndOfInput, result[1].Type);
        }

        [TestMethod]
        public void TestParsingComment()
        {
            var parser = new Parser(site);

            var result = parser.Parse(";123").ToArray();
            Assert.AreEqual(2, result.Length);
            Assert.AreEqual(ParserOutputType.Comment, result[0].Type);
            TestHelpers.AssertStructurallyEqual(new ZilFix(123), result[0].Object);
            Assert.AreEqual(ParserOutputType.EndOfInput, result[1].Type);
        }

        [TestMethod]
        public void TestParsingString()
        {
            var parser = new Parser(site);

            var result = parser.Parse(@"""hello""").ToArray();
            Assert.AreEqual(2, result.Length);
            Assert.AreEqual(ParserOutputType.Object, result[0].Type);
            TestHelpers.AssertStructurallyEqual(ZilString.FromString("hello"), result[0].Object);
            Assert.AreEqual(ParserOutputType.EndOfInput, result[1].Type);

            result = parser.Parse(@"""\""scare\"" quotes""").ToArray();
            Assert.AreEqual(2, result.Length);
            Assert.AreEqual(ParserOutputType.Object, result[0].Type);
            TestHelpers.AssertStructurallyEqual(ZilString.FromString("\"scare\" quotes"), result[0].Object);
            Assert.AreEqual(ParserOutputType.EndOfInput, result[1].Type);
        }

        [TestMethod]
        public void TestParsingList()
        {
            var parser = new Parser(site);

            var result = parser.Parse("()").ToArray();
            Assert.AreEqual(2, result.Length);
            Assert.AreEqual(ParserOutputType.Object, result[0].Type);
            TestHelpers.AssertStructurallyEqual(new ZilList(null, null), result[0].Object);
            Assert.AreEqual(ParserOutputType.EndOfInput, result[1].Type);

            result = parser.Parse("(1 2)").ToArray();
            Assert.AreEqual(2, result.Length);
            Assert.AreEqual(ParserOutputType.Object, result[0].Type);
            TestHelpers.AssertStructurallyEqual(new ZilList(
                new ZilFix(1), new ZilList(
                    new ZilFix(2), new ZilList(null, null))),
                result[0].Object);
            Assert.AreEqual(ParserOutputType.EndOfInput, result[1].Type);

            result = parser.Parse("(() 1 ())").ToArray();
            Assert.AreEqual(2, result.Length);
            Assert.AreEqual(ParserOutputType.Object, result[0].Type);
            TestHelpers.AssertStructurallyEqual(new ZilList(
                new ZilList(null, null), new ZilList(
                    new ZilFix(1), new ZilList(
                        new ZilList(null, null), new ZilList(null, null)))),
                result[0].Object);
            Assert.AreEqual(ParserOutputType.EndOfInput, result[1].Type);
        }

        [TestMethod]
        public void TestParsingForm()
        {
            var parser = new Parser(site);

            var result = parser.Parse("<>").ToArray();
            Assert.AreEqual(2, result.Length);
            Assert.AreEqual(ParserOutputType.Object, result[0].Type);
            Assert.AreSame(site.FALSE, result[0].Object);
            Assert.AreEqual(ParserOutputType.EndOfInput, result[1].Type);

            result = parser.Parse("<1 2>").ToArray();
            Assert.AreEqual(2, result.Length);
            Assert.AreEqual(ParserOutputType.Object, result[0].Type);
            TestHelpers.AssertStructurallyEqual(new ZilForm(new[] { new ZilFix(1), new ZilFix(2) }), result[0].Object);
            Assert.AreEqual(ParserOutputType.EndOfInput, result[1].Type);

            result = parser.Parse("<<> 1 <>>").ToArray();
            Assert.AreEqual(2, result.Length);
            Assert.AreEqual(ParserOutputType.Object, result[0].Type);
            TestHelpers.AssertStructurallyEqual(
                new ZilForm(new[] { site.FALSE, new ZilFix(1), site.FALSE }),
                result[0].Object);
            Assert.AreEqual(ParserOutputType.EndOfInput, result[1].Type);
        }

        [TestMethod]
        public void TestParsingVector()
        {
            var parser = new Parser(site);

            var result = parser.Parse("[]").ToArray();
            Assert.AreEqual(2, result.Length);
            Assert.AreEqual(ParserOutputType.Object, result[0].Type);
            TestHelpers.AssertStructurallyEqual(new ZilVector(), result[0].Object);
            Assert.AreEqual(ParserOutputType.EndOfInput, result[1].Type);

            result = parser.Parse("[1 2]").ToArray();
            Assert.AreEqual(2, result.Length);
            Assert.AreEqual(ParserOutputType.Object, result[0].Type);
            TestHelpers.AssertStructurallyEqual(new ZilVector(new ZilFix(1), new ZilFix(2)), result[0].Object);
            Assert.AreEqual(ParserOutputType.EndOfInput, result[1].Type);

            result = parser.Parse("[[] 1 []]").ToArray();
            Assert.AreEqual(2, result.Length);
            Assert.AreEqual(ParserOutputType.Object, result[0].Type);
            TestHelpers.AssertStructurallyEqual(
                new ZilVector(new ZilVector(), new ZilFix(1), new ZilVector()),
                result[0].Object);
            Assert.AreEqual(ParserOutputType.EndOfInput, result[1].Type);

            // fake UVECTOR
            result = parser.Parse("![![!] 1 ![]!]").ToArray();
            Assert.AreEqual(2, result.Length);
            Assert.AreEqual(ParserOutputType.Object, result[0].Type);
            TestHelpers.AssertStructurallyEqual(
                new ZilVector(new ZilVector(), new ZilFix(1), new ZilVector()),
                result[0].Object);
            Assert.AreEqual(ParserOutputType.EndOfInput, result[1].Type);
        }

        [TestMethod]
        public void TestParsingCharacter()
        {
            var parser = new Parser(site);

            var result = parser.Parse("!\\A").ToArray();
            Assert.AreEqual(2, result.Length);
            Assert.AreEqual(ParserOutputType.Object, result[0].Type);
            TestHelpers.AssertStructurallyEqual(new ZilChar('A'), result[0].Object);
            Assert.AreEqual(ParserOutputType.EndOfInput, result[1].Type);

            result = parser.Parse("!\\ ").ToArray();
            Assert.AreEqual(2, result.Length);
            Assert.AreEqual(ParserOutputType.Object, result[0].Type);
            TestHelpers.AssertStructurallyEqual(new ZilChar(' '), result[0].Object);
            Assert.AreEqual(ParserOutputType.EndOfInput, result[1].Type);
        }

        [TestMethod]
        public void TestParsingSegment()
        {
            var parser = new Parser(site);

            var result = parser.Parse("!<1>").ToArray();
            Assert.AreEqual(2, result.Length);
            Assert.AreEqual(ParserOutputType.Object, result[0].Type);
            TestHelpers.AssertStructurallyEqual(new ZilSegment(new ZilForm(new[] { new ZilFix(1) })), result[0].Object);
            Assert.AreEqual(ParserOutputType.EndOfInput, result[1].Type);

            result = parser.Parse("!<1!>").ToArray();
            Assert.AreEqual(2, result.Length);
            Assert.AreEqual(ParserOutputType.Object, result[0].Type);
            TestHelpers.AssertStructurallyEqual(new ZilSegment(new ZilForm(new[] { new ZilFix(1) })), result[0].Object);
            Assert.AreEqual(ParserOutputType.EndOfInput, result[1].Type);

            result = parser.Parse("!.A").ToArray();
            Assert.AreEqual(2, result.Length);
            Assert.AreEqual(ParserOutputType.Object, result[0].Type);
            TestHelpers.AssertStructurallyEqual(
                new ZilSegment(new ZilForm(new[] { site.ParseAtom("LVAL"), site.ParseAtom("A") })),
                result[0].Object);
            Assert.AreEqual(ParserOutputType.EndOfInput, result[1].Type);
        }

        [TestMethod]
        public void TestParsingHash()
        {
            var parser = new Parser(site);

            var result = parser.Parse("#FOO [1]").ToArray();
            Assert.AreEqual(2, result.Length);
            Assert.AreEqual(ParserOutputType.Object, result[0].Type);
            TestHelpers.AssertStructurallyEqual(
                new ZilHash(site.ParseAtom("FOO"), PrimType.ATOM, new ZilVector(new ZilFix(1))),
                result[0].Object);
            Assert.AreEqual(ParserOutputType.EndOfInput, result[1].Type);
        }

        [TestMethod]
        public void TestParsingReadMacro()
        {
            var parser = new Parser(site);

            site.OnEvaluate = _ => new ZilFix(3);
            var result = parser.Parse("(%<+ 1 2>)").ToArray();
            Assert.AreEqual(2, result.Length);
            Assert.AreEqual(ParserOutputType.Object, result[0].Type);
            TestHelpers.AssertStructurallyEqual(
                new ZilList(new ZilFix(3), new ZilList(null, null)),
                result[0].Object);
            Assert.AreEqual(ParserOutputType.EndOfInput, result[1].Type);

            result = parser.Parse("(%%<+ 1 2>)").ToArray();
            Assert.AreEqual(2, result.Length);
            Assert.AreEqual(ParserOutputType.Object, result[0].Type);
            TestHelpers.AssertStructurallyEqual(new ZilList(null, null), result[0].Object);
            Assert.AreEqual(ParserOutputType.EndOfInput, result[1].Type);

            site.OnEvaluate = _ => new ZilSplice(
                new ZilList(new ZilFix(1), new ZilList(new ZilFix(2), new ZilList(null, null))));
            result = parser.Parse("(%<CHTYPE '(1 2) SPLICE>)").ToArray();
            Assert.AreEqual(2, result.Length);
            Assert.AreEqual(ParserOutputType.Object, result[0].Type);
            TestHelpers.AssertStructurallyEqual(
                new ZilList(new ZilFix(1), new ZilList(new ZilFix(2), new ZilList(null, null))),
                result[0].Object);
            Assert.AreEqual(ParserOutputType.EndOfInput, result[1].Type);
        }

        [TestMethod]
        public void TestParsingAdecl()
        {
            var parser = new Parser(site);

            var result = parser.Parse("A:B").ToArray();
            Assert.AreEqual(2, result.Length);
            Assert.AreEqual(ParserOutputType.Object, result[0].Type);
            TestHelpers.AssertStructurallyEqual(new ZilAdecl(site.ParseAtom("A"), site.ParseAtom("B")), result[0].Object);
            Assert.AreEqual(ParserOutputType.EndOfInput, result[1].Type);

            result = parser.Parse(";A:B").ToArray();
            Assert.AreEqual(2, result.Length);
            Assert.AreEqual(ParserOutputType.Comment, result[0].Type);
            TestHelpers.AssertStructurallyEqual(new ZilAdecl(site.ParseAtom("A"), site.ParseAtom("B")), result[0].Object);
            Assert.AreEqual(ParserOutputType.EndOfInput, result[1].Type);
        }

        [TestMethod]
        public void TestParsingTemplate()
        {
            site.AddStdAtom("SPLICE", StdAtom.SPLICE);

            var parser = new Parser(
                site,
                new ZilFix(12345),
                new ZilList(new ZilFix(1), new ZilList(new ZilFix(2), new ZilList(null, null))));

            var result = parser.Parse("{0} {1}").ToArray();
            Assert.AreEqual(3, result.Length);
            Assert.AreEqual(ParserOutputType.Object, result[0].Type);
            TestHelpers.AssertStructurallyEqual(new ZilFix(12345), result[0].Object);
            Assert.AreEqual(ParserOutputType.Object, result[1].Type);
            TestHelpers.AssertStructurallyEqual(
                new ZilList(new ZilFix(1), new ZilList(new ZilFix(2), new ZilList(null, null))),
                result[1].Object);
            Assert.AreEqual(ParserOutputType.EndOfInput, result[2].Type);

            result = parser.Parse("{0} {1:SPLICE}").ToArray();
            Assert.AreEqual(4, result.Length);
            Assert.AreEqual(ParserOutputType.Object, result[0].Type);
            TestHelpers.AssertStructurallyEqual(new ZilFix(12345), result[0].Object);
            Assert.AreEqual(ParserOutputType.Object, result[1].Type);
            TestHelpers.AssertStructurallyEqual(new ZilFix(1), result[1].Object);
            Assert.AreEqual(ParserOutputType.Object, result[2].Type);
            TestHelpers.AssertStructurallyEqual(new ZilFix(2), result[2].Object);
            Assert.AreEqual(ParserOutputType.EndOfInput, result[3].Type);
        }

        [DataTestMethod]
        [DataRow("!;bang-comment", ParserOutputType.Comment)]
        [DataRow("!!<>", ParserOutputType.SyntaxError)]
        [DataRow("(! !)", ParserOutputType.Object)]
        [DataRow("(!)", ParserOutputType.Object)]
        [DataRow("<!>", ParserOutputType.Object)]
        [DataRow(@"!!\A", ParserOutputType.SyntaxError)]
        [DataRow("!%FOO", ParserOutputType.Object)]
        [DataRow("!%%FOO", ParserOutputType.EmptySplice)]
        [DataRow("!#2 1010", ParserOutputType.Object)]
        [DataRow("!'FOO", ParserOutputType.Object)]
        public void TestParsingUglyStructures([NotNull] string input, [NotNull] object boxedExpectedType)
        {
            site.OnEvaluate = _ => new ZilFix(3);

            var expectedType = (ParserOutputType)boxedExpectedType;
            var parser = new Parser(site);
            var results = parser.Parse(input).ToArray();
            var resultStr = string.Join(", ", results.Select(r => r.ToString()));

            if (expectedType == ParserOutputType.SyntaxError)
            {
                Assert.AreEqual(1, results.Length, "Expected 1 result but found [{0}]", resultStr);
                Assert.AreEqual(expectedType, results[0].Type, "Expected result to be {0} but found {1}",
                    expectedType, resultStr);
            }
            else
            {
                Assert.AreEqual(2, results.Length, "Expected 2 results but found [{0}]", resultStr);
                Assert.AreEqual(expectedType, results[0].Type, "Expected first result to be {0} but found {1}",
                    expectedType, resultStr);
                Assert.AreEqual(ParserOutputType.EndOfInput, results[1].Type,
                    "Expected second result to be {0} but found {1}", ParserOutput.EndOfInput, resultStr);
            }
        }
    }
}
