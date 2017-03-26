using System;
using System.Text;
using System.Collections.Generic;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Zilf.Language;
using Zilf.Interpreter;
using Zilf.Interpreter.Values;
using System.Linq;

namespace ZilfTests.Interpreter
{
    /// <summary>
    /// Tests for the Parser class
    /// </summary>
    [TestClass]
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
                ZilAtom result;
                if (!atoms.TryGetValue(text, out result))
                {
                    result = new ZilAtom(text, null, StdAtom.None);
                    atoms.Add(text, result);
                }
                return result;
            }

            public ZilAtom GetTypeAtom(ZilObject zo)
            {
                if (zo is ZilHash)
                    return ((ZilHash)zo).Type;

                System.Diagnostics.Debug.Assert(zo.StdTypeAtom != StdAtom.None);
                return ParseAtom(zo.StdTypeAtom.ToString());
            }

            public ZilObject ChangeType(ZilObject zo, ZilAtom type)
            {
                return new ZilHash(type, zo.PrimType, zo);
            }

            public ZilObject Evaluate(ZilObject zo)
            {
                var handler = OnEvaluate;
                if (handler != null)
                {
                    return handler(zo);
                }

                throw new InvalidOperationException($"{nameof(OnEvaluate)} not set");
            }

            public ZilObject GetGlobalVal(ZilAtom atom)
            {
                return null;
            }

            public void AddStdAtom(string name, StdAtom stdAtom)
            {
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
            Assert.AreEqual(new ZilFix(123), result[0].Object);
            Assert.AreEqual(ParserOutputType.EndOfInput, result[1].Type);

            result = parser.Parse("-123").ToArray();
            Assert.AreEqual(2, result.Length);
            Assert.AreEqual(ParserOutputType.Object, result[0].Type);
            Assert.AreEqual(new ZilFix(-123), result[0].Object);
            Assert.AreEqual(ParserOutputType.EndOfInput, result[1].Type);

            result = parser.Parse("+123").ToArray();
            Assert.AreEqual(2, result.Length);
            Assert.AreEqual(ParserOutputType.Object, result[0].Type);
            Assert.AreEqual(new ZilFix(123), result[0].Object);
            Assert.AreEqual(ParserOutputType.EndOfInput, result[1].Type);

            result = parser.Parse("*377*").ToArray();
            Assert.AreEqual(2, result.Length);
            Assert.AreEqual(ParserOutputType.Object, result[0].Type);
            Assert.AreEqual(new ZilFix(255), result[0].Object);
            Assert.AreEqual(ParserOutputType.EndOfInput, result[1].Type);

            result = parser.Parse("#2 1010").ToArray();
            Assert.AreEqual(2, result.Length);
            Assert.AreEqual(ParserOutputType.Object, result[0].Type);
            Assert.AreEqual(new ZilFix(10), result[0].Object);
            Assert.AreEqual(ParserOutputType.EndOfInput, result[1].Type);
        }

        [TestMethod]
        public void TestParsingComment()
        {
            var parser = new Parser(site);

            var result = parser.Parse(";123").ToArray();
            Assert.AreEqual(2, result.Length);
            Assert.AreEqual(ParserOutputType.Comment, result[0].Type);
            Assert.AreEqual(new ZilFix(123), result[0].Object);
            Assert.AreEqual(ParserOutputType.EndOfInput, result[1].Type);
        }

        [TestMethod]
        public void TestParsingString()
        {
            var parser = new Parser(site);

            var result = parser.Parse(@"""hello""").ToArray();
            Assert.AreEqual(2, result.Length);
            Assert.AreEqual(ParserOutputType.Object, result[0].Type);
            Assert.AreEqual(ZilString.FromString("hello"), result[0].Object);
            Assert.AreEqual(ParserOutputType.EndOfInput, result[1].Type);

            result = parser.Parse(@"""\""scare\"" quotes""").ToArray();
            Assert.AreEqual(2, result.Length);
            Assert.AreEqual(ParserOutputType.Object, result[0].Type);
            Assert.AreEqual(ZilString.FromString("\"scare\" quotes"), result[0].Object);
            Assert.AreEqual(ParserOutputType.EndOfInput, result[1].Type);
        }

        [TestMethod]
        public void TestParsingList()
        {
            var parser = new Parser(site);

            var result = parser.Parse("()").ToArray();
            Assert.AreEqual(2, result.Length);
            Assert.AreEqual(ParserOutputType.Object, result[0].Type);
            Assert.AreEqual(new ZilList(null, null), result[0].Object);
            Assert.AreEqual(ParserOutputType.EndOfInput, result[1].Type);

            result = parser.Parse("(1 2)").ToArray();
            Assert.AreEqual(2, result.Length);
            Assert.AreEqual(ParserOutputType.Object, result[0].Type);
            Assert.AreEqual(new ZilList(
                new ZilFix(1), new ZilList(
                    new ZilFix(2), new ZilList(null, null))),
                result[0].Object);
            Assert.AreEqual(ParserOutputType.EndOfInput, result[1].Type);

            result = parser.Parse("(() 1 ())").ToArray();
            Assert.AreEqual(2, result.Length);
            Assert.AreEqual(ParserOutputType.Object, result[0].Type);
            Assert.AreEqual(new ZilList(
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
            Assert.AreEqual(new ZilForm(new[] { new ZilFix(1), new ZilFix(2) }), result[0].Object);
            Assert.AreEqual(ParserOutputType.EndOfInput, result[1].Type);

            result = parser.Parse("<<> 1 <>>").ToArray();
            Assert.AreEqual(2, result.Length);
            Assert.AreEqual(ParserOutputType.Object, result[0].Type);
            Assert.AreEqual(
                new ZilForm(new ZilObject[] { site.FALSE, new ZilFix(1), site.FALSE }),
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
            Assert.AreEqual(new ZilVector(), result[0].Object);
            Assert.AreEqual(ParserOutputType.EndOfInput, result[1].Type);

            result = parser.Parse("[1 2]").ToArray();
            Assert.AreEqual(2, result.Length);
            Assert.AreEqual(ParserOutputType.Object, result[0].Type);
            Assert.AreEqual(new ZilVector(new ZilFix(1), new ZilFix(2)), result[0].Object);
            Assert.AreEqual(ParserOutputType.EndOfInput, result[1].Type);

            result = parser.Parse("[[] 1 []]").ToArray();
            Assert.AreEqual(2, result.Length);
            Assert.AreEqual(ParserOutputType.Object, result[0].Type);
            Assert.AreEqual(
                new ZilVector(new ZilVector(), new ZilFix(1), new ZilVector()),
                result[0].Object);
            Assert.AreEqual(ParserOutputType.EndOfInput, result[1].Type);

            // fake UVECTOR
            result = parser.Parse("![![!] 1 ![]!]").ToArray();
            Assert.AreEqual(2, result.Length);
            Assert.AreEqual(ParserOutputType.Object, result[0].Type);
            Assert.AreEqual(
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
            Assert.AreEqual(new ZilChar('A'), result[0].Object);
            Assert.AreEqual(ParserOutputType.EndOfInput, result[1].Type);

            result = parser.Parse("!\\ ").ToArray();
            Assert.AreEqual(2, result.Length);
            Assert.AreEqual(ParserOutputType.Object, result[0].Type);
            Assert.AreEqual(new ZilChar(' '), result[0].Object);
            Assert.AreEqual(ParserOutputType.EndOfInput, result[1].Type);
        }

        [TestMethod]
        public void TestParsingSegment()
        {
            var parser = new Parser(site);

            var result = parser.Parse("!<1>").ToArray();
            Assert.AreEqual(2, result.Length);
            Assert.AreEqual(ParserOutputType.Object, result[0].Type);
            Assert.AreEqual(new ZilSegment(new ZilForm(new[] { new ZilFix(1) })), result[0].Object);
            Assert.AreEqual(ParserOutputType.EndOfInput, result[1].Type);

            result = parser.Parse("!<1!>").ToArray();
            Assert.AreEqual(2, result.Length);
            Assert.AreEqual(ParserOutputType.Object, result[0].Type);
            Assert.AreEqual(new ZilSegment(new ZilForm(new[] { new ZilFix(1) })), result[0].Object);
            Assert.AreEqual(ParserOutputType.EndOfInput, result[1].Type);

            result = parser.Parse("!.A").ToArray();
            Assert.AreEqual(2, result.Length);
            Assert.AreEqual(ParserOutputType.Object, result[0].Type);
            Assert.AreEqual(
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
            Assert.AreEqual(
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
            Assert.AreEqual(
                new ZilList(new ZilFix(3), new ZilList(null, null)),
                result[0].Object);
            Assert.AreEqual(ParserOutputType.EndOfInput, result[1].Type);

            result = parser.Parse("(%%<+ 1 2>)").ToArray();
            Assert.AreEqual(2, result.Length);
            Assert.AreEqual(ParserOutputType.Object, result[0].Type);
            Assert.AreEqual(new ZilList(null, null), result[0].Object);
            Assert.AreEqual(ParserOutputType.EndOfInput, result[1].Type);

            site.OnEvaluate = _ => new ZilSplice(
                new ZilList(new ZilFix(1), new ZilList(new ZilFix(2), new ZilList(null, null))));
            result = parser.Parse("(%<CHTYPE '(1 2) SPLICE>)").ToArray();
            Assert.AreEqual(2, result.Length);
            Assert.AreEqual(ParserOutputType.Object, result[0].Type);
            Assert.AreEqual(
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
            Assert.AreEqual(new ZilAdecl(site.ParseAtom("A"), site.ParseAtom("B")), result[0].Object);
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
            Assert.AreEqual(new ZilFix(12345), result[0].Object);
            Assert.AreEqual(ParserOutputType.Object, result[1].Type);
            Assert.AreEqual(
                new ZilList(new ZilFix(1), new ZilList(new ZilFix(2), new ZilList(null, null))),
                result[1].Object);
            Assert.AreEqual(ParserOutputType.EndOfInput, result[2].Type);

            result = parser.Parse("{0} {1:SPLICE}").ToArray();
            Assert.AreEqual(4, result.Length);
            Assert.AreEqual(ParserOutputType.Object, result[0].Type);
            Assert.AreEqual(new ZilFix(12345), result[0].Object);
            Assert.AreEqual(ParserOutputType.Object, result[1].Type);
            Assert.AreEqual(new ZilFix(1), result[1].Object);
            Assert.AreEqual(ParserOutputType.Object, result[2].Type);
            Assert.AreEqual(new ZilFix(2), result[2].Object);
            Assert.AreEqual(ParserOutputType.EndOfInput, result[3].Type);
        }
    }
}
