/* Copyright 2010-2023 Tara McGrew
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
using System;
using System.Collections.Generic;
using System.Linq;
using Zilf.Common.StringEncoding;
using Zilf.Common.StringEncoding.SuffixTrees;

namespace Zilf.Common.Tests
{
    [TestClass]
    [System.Diagnostics.CodeAnalysis.SuppressMessage("Performance", "CA1861:Avoid constant arrays as arguments", Justification = "Test methods are only called once")]
    [System.Diagnostics.CodeAnalysis.SuppressMessage("CodeQuality", "IDE0079:Remove unnecessary suppression", Justification = "The suppression above is necessary")]
    public class SuffixTreeTests
    {
        static readonly IReadOnlyDictionary<char, int> ScrabbleValues = new Dictionary<char, int>
        {
            ['a'] = 1,
            ['e'] = 1,
            ['i'] = 1,
            ['o'] = 1,
            ['u'] = 1,
            ['l'] = 1,
            ['n'] = 1,
            ['s'] = 1,
            ['t'] = 1,
            ['r'] = 1,
            ['d'] = 2,
            ['g'] = 2,
            ['b'] = 3,
            ['c'] = 3,
            ['m'] = 3,
            ['p'] = 3,
            ['f'] = 4,
            ['h'] = 4,
            ['v'] = 4,
            ['w'] = 4,
            ['y'] = 4,
            ['k'] = 5,
            ['j'] = 8,
            ['x'] = 8,
            ['q'] = 10,
            ['z'] = 10,
        };

        static readonly string[] Pangrams =
        [
            //                  11111111112222222222333333333344444444445555555555
            //        012345678901234567890123456789012345678901234567890123456789
            /*  0 */ "a quick fox jumps over the lazy brown dog",
            /*  1 */ "watch jeopardy, alex trebek's fun tv quiz game",
            /*  2 */ "few black taxis drive up major roads on quiet hazy nights",
            /*  3 */ "sphinx of black quartz, judge my vow",
            /*  4 */ "the five boxing wizards jump quickly",
            /*  5 */ "pack my box with five dozen liquor jugs",
            /*  6 */ "few quips galvanized the mock jury box",
            /*  7 */ "five quacking zephyrs jolt my wax bed",
            /*  8 */ "waxy and quivering jocks fumble the pizza",
            /*  9 */ "amazingly few discotheques provide jukeboxes",
            /* 10 */ "grumpy wizards make a toxic brew for the jovial queen",
            /* 11 */ "my girl wove six dozen plaid jackets before she quit",
            /* 12 */ "by jove, my quick study of lexicography won a prize",
            /* 13 */ "jackdaws love my big sphinx of quartz",
            /* 14 */ "a wizard's job is to vex chumps quickly in fog",
            /* 15 */ "the wizard quickly jinxed the gnomes before they vaporized",
            /* 16 */ "just keep examining every low bid quoted for zinc etchings",
            /* 17 */ "how razorback-jumping frogs can level six piqued gymnasts",
        ];

        [TestMethod]
        public void TestSuffixTree1()
        {
            var tree = new SuffixTree<int>();

            for (int i = 0; i < Pangrams.Length; i++)
            {
                tree.Add(Pangrams[i], i);
            }

            var boxResults = tree.Search("box").ToList();
            CollectionAssert.AreEquivalent(new[] { 4, 5, 6, 9 }, boxResults);

            var wizardResults = tree.Search("wizard").ToList();
            CollectionAssert.AreEquivalent(new[] { 4, 10, 14, 15 }, wizardResults);

            var waxResults = tree.Search("wax").ToList();
            CollectionAssert.AreEquivalent(new[] { 7, 8 }, waxResults);

            var theResults = tree.Search("the").ToList();
            CollectionAssert.AreEquivalent(new[] { 0, 4, 6, 8, 9, 10, 15 }, theResults);

            var theCount = tree.CountOccurrences("the");
            Assert.AreEqual(9, theCount);

            var theResultsWithDepths = tree.SearchWithDepth("the").ToList();
            var theResultsWithOffsets = theResultsWithDepths
                .Select(p => (p.data, Pangrams[p.data].Length - p.depth))
                .ToArray();
            CollectionAssert.AreEquivalent(
                new[]
                {
                    (0, 23),
                    (4, 0),
                    (6, 21),
                    (8, 32),
                    (9, 19),
                    (10, 37),
                    (15, 0), (15, 26), (15, 44),
                },
                theResultsWithOffsets);

            var ogResults = tree.Search("og").ToList();
            CollectionAssert.AreEquivalent(new[] { 0, 12, 14, 17 }, ogResults);
        }

        [TestMethod]
        public void TestOverlap()
        {
            var isc = new IndexedStringCollection
            {
                /* 0 */ "A man, a plan, a canal, Panama",
                /* 1 */ "Panama",
                /* 2 */ "A penny saved is a penny earned",
                /* 3 */ "A penny",
                /* 4 */ "penny",
                /* 5 */ "penny earned",
                /* 6 */ " is a ",
                /* 7 */ " is ",
                /* 8 */ " a",
                /* 9 */ "ama",
                /*10 */ "xyzzy",
                /*11 */ "xyzzy ama",
            };

            var m = isc.GetSuffixPrefixOverlaps();

            Assert.AreEqual(3, m[0, 9]);

            isc.RemoveOverlapping((a, b) => a.Length - b.Length);

            var trimmed = isc.ToList();

            CollectionAssert.AreEqual(new[]
            {
                "A man, a plan, a canal, Panama",
                "A penny saved is a penny earned",
                "xyzzy ama",
            }, trimmed);
        }

        static readonly string[] TwistyPassages = [
            "You are in a maze of twisty little passages, all alike.",
            "You are in a little maze of twisty passages, all alike.",
            "You are in a twisty maze of little passages, all alike.",
            "You are in a twisty little maze of passages, all alike.",
        ];

        [TestMethod]
        public void TestSearchableStringCollection()
        {
            int costFunction(ReadOnlySpan<char> s) => s.Sum(c => ScrabbleValues.GetValueOrDefault(char.ToLower(c)));

            int evaluationFunction(int cost, int occurrences, ReadOnlySpan<char> s) =>
                s.Length switch
                {
                    < 2 or > 15 => 0,
                    _ => cost * occurrences,
                };

            var ssc = new IndexedStringCollection(TwistyPassages);

            var items = ssc.FindBestSubstrings(10, costFunction, evaluationFunction);

            foreach (var i in items)
                Console.WriteLine("Top: [{0}] {1}", i.score, i.substring);

            var (cost, substring) = items.First();
            Console.WriteLine($"Max cost substring: '{substring}' ({cost} points)");

            ssc.Split(substring);

            (cost, substring) = ssc.FindBestSubstrings(1, costFunction, evaluationFunction).First();
            Console.WriteLine($"Next max cost substring: '{substring}' ({cost} points)");

            ssc.Split(substring);

            (cost, substring) = ssc.FindBestSubstrings(1, costFunction, evaluationFunction).First();
            Console.WriteLine($"Next max cost substring: '{substring}' ({cost} points)");

            ssc.Split(substring);
        }

        [TestMethod]
        public void TestSearchableStringCollection_NoPrefixOrSuffix()
        {
            var ssc = new IndexedStringCollection
            {
                "foo",
                "football"
            };

            Assert.AreEqual(2, ssc.CountOccurrences("foo"));
        }

        [TestMethod]
        public void TestSearchableStringCollection_NoSuffix()
        {
            var ssc = new IndexedStringCollection
            {
                "There",
                "herefore",
                "here"
            };

            Assert.AreEqual(3, ssc.CountOccurrences("here"));
        }

        [TestMethod]
        public void TestSearchableStringCollection_Overlapping()
        {
            const string Substring = "aaaaaaaaaa"; // 10 a's

            var ssc = new IndexedStringCollection
            {
                "aaaaaaaaaaaaaaaaaaaa"  // 20 a's
            };

            Assert.AreEqual(2, ssc.CountOccurrences(Substring));
        }
    }
}
