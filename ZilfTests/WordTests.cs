﻿using System;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Zilf;
using System.Collections.Generic;

namespace ZilfTests
{
    [TestClass]
    public class WordTests
    {
        [TestMethod]
        public void TestCtor()
        {
            var atom = new ZilAtom("FOO", new ObList(), StdAtom.None);

            var word = new Word(atom);
        }

        [TestMethod]
        [ExpectedException(typeof(ArgumentNullException))]
        public void Ctor_Should_Reject_Null_Atom()
        {
            var word = new Word(null);
        }

        /// <summary>
        /// Runs a test to ensure that a Word keeps the correct verb and preposition values after setting the parts
        /// of speech in some order.
        /// </summary>
        /// <param name="zversion">The Z-machine version to test.</param>
        /// <param name="newVoc">true to test with NEW-VOC? enabled, otherwise false.</param>
        /// <param name="setPartsOfSpeech">A delegate to set the parts of speech. It will be called with the parameters:
        /// <list type="*">
        /// <item>ctx (a Context),</item>
        /// <item>word (the vocabulary word to mutate),</item>
        /// <item>verbValue (the value to use when setting PartOfSpeech.Verb), and</item>
        /// <item>prepValue (the value to use when setting PartOfSpeech.Preposition).</item>
        /// </list></param>
        private void Test_Keep_VP_Values(int zversion, bool newVoc, Action<Context, Word, byte, byte> setPartsOfSpeech)
        {
            Context ctx;
            Word word;
            CreateWordInContext(zversion, newVoc, out ctx, out word);

            // set parts of speech
            const byte VERB_VALUE = 115, PREPOSITION_VALUE = 200;
            System.Diagnostics.Debug.Assert(VERB_VALUE != PREPOSITION_VALUE);

            setPartsOfSpeech(ctx, word, VERB_VALUE, PREPOSITION_VALUE);

            // verify values
            Assert.AreEqual(VERB_VALUE, word.GetValue(PartOfSpeech.Verb), "Verb value should remain set");
            Assert.AreEqual(PREPOSITION_VALUE, word.GetValue(PartOfSpeech.Preposition), "Preposition value should remain set");
        }

        /// <summary>
        /// Creates a new Context and adds a Word to it.
        /// </summary>
        /// <param name="zversion">The Z-machine version to test.</param>
        /// <param name="newVoc">true to test with NEW-VOC? enabled, otherwise false.</param>
        /// <param name="ctx">Returns the new Context.</param>
        /// <param name="word">Returns a new Word ("FOO") added to the context's ObList.</param>
        private static void CreateWordInContext(int zversion, bool newVoc, out Context ctx, out Word word)
        {
            // set up context
            ctx = new Context();
            ctx.ZEnvironment.ZVersion = zversion;

            if (newVoc)
                ctx.SetGlobalVal(ctx.GetStdAtom(StdAtom.NEW_VOC_P), ctx.TRUE);

            // create word
            var wordAtom = new ZilAtom("FOO", ctx.RootObList, StdAtom.None);
            word = new Word(wordAtom);
        }

        [TestMethod]
        public void V3_NewVoc_Verb_Prep_Object_Should_Keep_VP_Values()
        {
            Test_Keep_VP_Values(3, true, (ctx, word, verbValue, prepValue) =>
            {
                word.SetVerb(ctx, verbValue);
                word.SetPreposition(ctx, prepValue);
                word.SetObject(ctx);
            });
        }

        [TestMethod]
        public void V3_NewVoc_Object_Prep_Verb_Should_Keep_VP_Values()
        {
            Test_Keep_VP_Values(3, true, (ctx, word, verbValue, prepValue) =>
            {
                word.SetObject(ctx);
                word.SetPreposition(ctx, prepValue);
                word.SetVerb(ctx, verbValue);
            });
        }

        [TestMethod]
        public void V3_NewVoc_Prep_Object_Verb_Should_Keep_VP_Values()
        {
            Test_Keep_VP_Values(3, true, (ctx, word, verbValue, prepValue) =>
            {
                word.SetPreposition(ctx, prepValue);
                word.SetObject(ctx);
                word.SetVerb(ctx, verbValue);
            });
        }

        [TestMethod]
        [ExpectedException(typeof(CompilerError))]
        public void V3_OldVoc_Verb_Prep_Object_Should_Throw()
        {
            Context ctx;
            Word word;
            CreateWordInContext(3, false, out ctx, out word);

            word.SetVerb(ctx, 100);
            word.SetPreposition(ctx, 200);
            word.SetObject(ctx);
        }

        [TestMethod]
        [ExpectedException(typeof(CompilerError))]
        public void V4_OldVoc_Verb_Prep_Object_Should_Throw()
        {
            Context ctx;
            Word word;
            CreateWordInContext(4, false, out ctx, out word);

            word.SetVerb(ctx, 100);
            word.SetPreposition(ctx, 200);
            word.SetObject(ctx);
        }

        [TestMethod]
        public void V4_NewVoc_Verb_Prep_Object_Adj_Should_Keep_VP_Values()
        {
            Test_Keep_VP_Values(4, true, (ctx, word, verbValue, prepValue) =>
            {
                word.SetVerb(ctx, verbValue);
                word.SetPreposition(ctx, prepValue);
                word.SetObject(ctx);
                word.SetAdjective(ctx, 7);
            });
        }

        [TestMethod]
        public void V4_NewVoc_Verb_Adj_Prep_Object_Should_Keep_VP_Values()
        {
            Test_Keep_VP_Values(4, true, (ctx, word, verbValue, prepValue) =>
            {
                word.SetVerb(ctx, verbValue);
                word.SetAdjective(ctx, 7);
                word.SetPreposition(ctx, prepValue);
                word.SetObject(ctx);
            });
        }

        [TestMethod]
        [ExpectedException(typeof(CompilerError))]
        public void V3_NewVoc_Verb_Prep_Object_Adj_Should_Throw()
        {
            Context ctx;
            Word word;
            CreateWordInContext(3, true, out ctx, out word);

            word.SetVerb(ctx, 100);
            word.SetPreposition(ctx, 200);
            word.SetObject(ctx);
            word.SetAdjective(ctx, 250);
        }

        private struct WtwbTestCase
        {
            public int ZVersion;
            public bool NewVoc;

            public PartOfSpeech FirstPart;
            public byte FirstValue;
            public PartOfSpeech SecondPart;
            public byte SecondValue;
            public PartOfSpeech ThirdPart;
            public byte ThirdValue;

            public PartOfSpeech ExpectedPartOfSpeech;
            public byte ExpectedValue1;
            public byte ExpectedValue2;

            public WtwbTestCase(int zversion, bool newVoc,
                PartOfSpeech firstPart, byte firstValue,
                PartOfSpeech expectedPartOfSpeech, byte expectedValue1, byte expectedValue2)
                : this(zversion, newVoc,
                    firstPart, firstValue,
                    0, 0,
                    0, 0,
                    expectedPartOfSpeech, expectedValue1, expectedValue2)
            {
            }

            public WtwbTestCase(int zversion, bool newVoc,
                PartOfSpeech firstPart, byte firstValue,
                PartOfSpeech secondPart, byte secondValue,
                PartOfSpeech expectedPartOfSpeech, byte expectedValue1, byte expectedValue2)
                : this(zversion, newVoc,
                    firstPart, firstValue,
                    secondPart, secondValue,
                    0, 0,
                    expectedPartOfSpeech, expectedValue1, expectedValue2)
            {
            }

            public WtwbTestCase(int zversion, bool newVoc,
                PartOfSpeech firstPart, byte firstValue,
                PartOfSpeech secondPart, byte secondValue,
                PartOfSpeech thirdPart, byte thirdValue,
                PartOfSpeech expectedPartOfSpeech, byte expectedValue1, byte expectedValue2)
            {
                this.ZVersion = zversion;
                this.NewVoc = newVoc;

                this.FirstPart = firstPart;
                this.FirstValue = firstValue;
                this.SecondPart = secondPart;
                this.SecondValue = secondValue;
                this.ThirdPart = thirdPart;
                this.ThirdValue = thirdValue;

                this.ExpectedPartOfSpeech = expectedPartOfSpeech;
                this.ExpectedValue1 = expectedValue1;
                this.ExpectedValue2 = expectedValue2;
            }

            public override string ToString()
            {
 	            return string.Format("(V{0}-{1}, {2}={3}, {4}={5}, {6}={7})",
                    ZVersion, NewVoc ? "New" : "Old",
                    FirstPart, FirstValue,
                    SecondPart, SecondValue,
                    ThirdPart, ThirdValue);
            }
        }

        private class MockOperand : Zilf.Emit.IOperand
        {
            public byte Value;
        }

        private class MockWordBuilder : Zilf.Emit.IWordBuilder
        {
            public readonly List<byte> ActualBytes = new List<byte>(3);

            #region ITableBuilder Members

            public void AddByte(byte value)
            {
                ActualBytes.Add(value);
            }

            public void AddByte(Zilf.Emit.IOperand value)
            {
                Assert.IsInstanceOfType(value, typeof(MockOperand),
                    "IWordBuilder.AddByte should be called with an operand we gave it");

                ActualBytes.Add(((MockOperand)value).Value);
            }

            public void AddShort(short value)
            {
                Assert.Fail("IWordBuilder.AddShort shouldn't be called");
            }

            public void AddShort(Zilf.Emit.IOperand value)
            {
                Assert.Fail("IWordBuilder.AddShort shouldn't be called");
            }

            #endregion
        }

        [TestMethod]
        public void WriteToWordBuilder_Should_Produce_Expected_Output()
        {
            const int OBJPRESENT = 1, ADJNUM = 2, BUZZNUM = 3, DIRNUM = 4, PREPNUM = 5, VERBNUM = 6;

            WtwbTestCase[] testCases = {
                new WtwbTestCase(3, false,
                    PartOfSpeech.Object, OBJPRESENT,
                    PartOfSpeech.Object, OBJPRESENT, 0),
                new WtwbTestCase(3, true,
                    PartOfSpeech.Object, OBJPRESENT,
                    PartOfSpeech.Object, 0, 0),
                new WtwbTestCase(3, false,
                    PartOfSpeech.Object, OBJPRESENT,
                    PartOfSpeech.Verb, VERBNUM,
                    PartOfSpeech.Object | PartOfSpeech.Verb, OBJPRESENT, VERBNUM),
                new WtwbTestCase(3, false,
                    PartOfSpeech.Verb, VERBNUM,
                    PartOfSpeech.Object, OBJPRESENT,
                    PartOfSpeech.Object | PartOfSpeech.Verb | PartOfSpeech.VerbFirst, VERBNUM, OBJPRESENT),
                new WtwbTestCase(3, true,
                    PartOfSpeech.Object, OBJPRESENT,
                    PartOfSpeech.Verb, VERBNUM,
                    PartOfSpeech.Object | PartOfSpeech.Verb | PartOfSpeech.VerbFirst, VERBNUM, 0),
                new WtwbTestCase(3, true,
                    PartOfSpeech.Verb, VERBNUM,
                    PartOfSpeech.Object, OBJPRESENT,
                    PartOfSpeech.Object | PartOfSpeech.Verb | PartOfSpeech.VerbFirst, VERBNUM, 0),
                new WtwbTestCase(3, false,
                    PartOfSpeech.Direction, DIRNUM,
                    PartOfSpeech.Preposition, PREPNUM,
                    PartOfSpeech.Direction | PartOfSpeech.Preposition, PREPNUM, DIRNUM),
                new WtwbTestCase(4, true,
                    PartOfSpeech.Object, OBJPRESENT,
                    PartOfSpeech.Adjective, ADJNUM,
                    PartOfSpeech.Object | PartOfSpeech.Adjective, 0, 0),
                new WtwbTestCase(4, true,
                    PartOfSpeech.Adjective, ADJNUM,
                    PartOfSpeech.Object, OBJPRESENT,
                    PartOfSpeech.Object | PartOfSpeech.Adjective, 0, 0),
                new WtwbTestCase(3, true,
                    PartOfSpeech.Object, OBJPRESENT,
                    PartOfSpeech.Verb, VERBNUM,
                    PartOfSpeech.Adjective, ADJNUM,
                    PartOfSpeech.Object | PartOfSpeech.Verb | PartOfSpeech.Adjective | PartOfSpeech.VerbFirst, VERBNUM, ADJNUM),
                new WtwbTestCase(4, true,
                    PartOfSpeech.Verb, VERBNUM,
                    PartOfSpeech.Preposition, PREPNUM,
                    PartOfSpeech.Object, OBJPRESENT,
                    PartOfSpeech.Preposition | PartOfSpeech.Verb | PartOfSpeech.Object, PREPNUM, VERBNUM),
                new WtwbTestCase(3, false,
                    PartOfSpeech.Adjective, ADJNUM,
                    PartOfSpeech.Direction, DIRNUM,
                    PartOfSpeech.AdjectiveFirst | PartOfSpeech.Adjective | PartOfSpeech.Direction, ADJNUM, DIRNUM),
                new WtwbTestCase(3, false,
                    PartOfSpeech.Direction, DIRNUM,
                    PartOfSpeech.Adjective, ADJNUM,
                    PartOfSpeech.DirectionFirst | PartOfSpeech.Adjective | PartOfSpeech.Direction, DIRNUM, ADJNUM),
                new WtwbTestCase(3, false,
                    PartOfSpeech.Buzzword, BUZZNUM,
                    PartOfSpeech.Adjective, ADJNUM,
                    PartOfSpeech.Adjective | PartOfSpeech.Buzzword, BUZZNUM, ADJNUM),
                new WtwbTestCase(3, false,
                    PartOfSpeech.Adjective, ADJNUM,
                    PartOfSpeech.Buzzword, BUZZNUM,
                    PartOfSpeech.Adjective | PartOfSpeech.Buzzword, BUZZNUM, ADJNUM),
                new WtwbTestCase(3, false,
                    PartOfSpeech.Object, OBJPRESENT,
                    PartOfSpeech.Buzzword, BUZZNUM,
                    PartOfSpeech.Object | PartOfSpeech.Buzzword, BUZZNUM, OBJPRESENT),
                new WtwbTestCase(3, false,
                    PartOfSpeech.Direction, DIRNUM,
                    PartOfSpeech.Buzzword, BUZZNUM,
                    PartOfSpeech.Direction | PartOfSpeech.Buzzword, BUZZNUM, DIRNUM),
                new WtwbTestCase(3, false,
                    PartOfSpeech.Preposition, PREPNUM,
                    PartOfSpeech.Buzzword, BUZZNUM,
                    PartOfSpeech.Preposition | PartOfSpeech.Buzzword, PREPNUM, BUZZNUM),
                new WtwbTestCase(3, false,
                    PartOfSpeech.Verb, VERBNUM,
                    PartOfSpeech.Buzzword, BUZZNUM,
                    PartOfSpeech.Verb | PartOfSpeech.Buzzword, BUZZNUM, VERBNUM),
                new WtwbTestCase(4, true,
                    PartOfSpeech.Object, OBJPRESENT,
                    PartOfSpeech.Buzzword, BUZZNUM,
                    PartOfSpeech.Adjective, ADJNUM,
                    PartOfSpeech.Object | PartOfSpeech.Adjective | PartOfSpeech.Buzzword, BUZZNUM, 0),
            };

            foreach (var tc in testCases)
            {
                // set up word according to test case
                Context ctx;
                Word word;
                CreateWordInContext(tc.ZVersion, tc.NewVoc, out ctx, out word);

                // TODO: catch exceptions in this block to indicate which test failed

                for (int i = 0; i < 3; i++)
                {
                    var part = i == 0 ? tc.FirstPart : i == 1 ? tc.SecondPart : tc.ThirdPart;
                    var value = i == 0 ? tc.FirstValue : i == 1 ? tc.SecondValue : tc.ThirdValue;

                    switch (part)
                    {
                        case PartOfSpeech.Adjective:
                            word.SetAdjective(ctx, value);
                            break;
                        case PartOfSpeech.Buzzword:
                            word.SetBuzzword(ctx, value);
                            break;
                        case PartOfSpeech.Direction:
                            word.SetDirection(ctx, value);
                            break;
                        case PartOfSpeech.Object:
                            word.SetObject(ctx);
                            break;
                        case PartOfSpeech.Preposition:
                            word.SetPreposition(ctx, value);
                            break;
                        case PartOfSpeech.Verb:
                            word.SetVerb(ctx, value);
                            break;
                        case PartOfSpeech.None:
                            // nada
                            break;
                        default:
                            throw new NotImplementedException("BUG");
                    }
                }

                // write to wordbuilder
                var wb = new MockWordBuilder();
                word.WriteToBuilder(ctx, wb, dir => new MockOperand { Value = dir });

                // verify expected output
                Assert.AreEqual(3, wb.ActualBytes.Count, "Wrong number of bytes written for {0}", tc);

                if (wb.ActualBytes[0] != (byte)tc.ExpectedPartOfSpeech ||
                    wb.ActualBytes[1] != tc.ExpectedValue1 ||
                    wb.ActualBytes[2] != tc.ExpectedValue2)
                {
                    Assert.Fail("For {0}, expected to write ({1}; {2}, {3}) but got ({4}; {5}, {6})",
                        tc,
                        tc.ExpectedPartOfSpeech, tc.ExpectedValue1, tc.ExpectedValue2,
                        (PartOfSpeech)wb.ActualBytes[0], wb.ActualBytes[1], wb.ActualBytes[2]);
                }
            }
        }
    }
}