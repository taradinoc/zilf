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

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;

namespace Zilf.Common.StringEncoding
{
    public sealed class AbbrevFinder
    {
        public struct Result
        {
            public readonly int Score, Count;
            public readonly string Text;

            public Result(int score, int count, string text)
            {
                Score = score;
                Count = count;
                Text = text;
            }
        }

        readonly List<string> allTexts = new();
        readonly StringEncoder encoder = new();

        /// <summary>
        /// Adds some text to the accumulator.
        /// </summary>
        /// <param name="text">The text to add.</param>
        public void AddText(string text)
        {
            allTexts.Add(text);
        }

        /// <summary>
        /// Gets the number of texts in the accumulator.
        /// </summary>
        public int Position => allTexts.Count;

        /// <summary>
        /// Rolls the accumulator back to a previous state.
        /// </summary>
        /// <param name="position">The number of texts to keep.</param>
        public void Rollback(int position)
        {
            if (position < allTexts.Count && position >= 0)
                allTexts.RemoveRange(position, allTexts.Count - position);
        }

        /// <summary>
        /// Returns a sequence of abbreviations and clears the previously added text.
        /// </summary>
        /// <param name="max">The maximum number of abbreviations to return.</param>
        /// <returns>A sequence of abbreviations, in descending order of overall savings.</returns>
        public IEnumerable<Result> GetResults(int max)
        {
#if DEBUG_ABBREV
            Console.Error.WriteLine("Indexing {0} strings", allTexts.Count);

            var outerStopw = new Stopwatch();
            outerStopw.Start();
#endif

            var isc = new IndexedStringCollection(allTexts);
            var charsetMap = encoder.GetCharsetMap();

            int CostFunction(ReadOnlySpan<char> s)
            {
                int result = 0;

                foreach (var c in s)
                {
                    result += charsetMap.GetValueOrDefault(c) switch
                    {
                        // characters in alphabet 0 cost one Z-char each
                        0 => 1,
                        // characters in alphabet 1 or 2 cost two Z-chars each
                        1 or 2 => 2,
                        // characters in no alphabet cost four Z-chars each
                        _ => 4,
                    };
                }

                return result;
            }

            int EvaluationFunction(int cost, int count, ReadOnlySpan<char> word)
            {
                var savings = cost - 2;
                return (count - 1) * savings - 2;
            }

            while (max > 0)
            {
#if DEBUG_ABBREV
                var stopw = new Stopwatch();
                Console.Error.WriteLine("Finding up to {0} abbreviations...", max);
                stopw.Start();
#endif

                int BATCH_SIZE = 1;
                var results = isc.FindBestSubstrings(BATCH_SIZE, CostFunction, EvaluationFunction).ToList();

#if DEBUG_ABBREV
                stopw.Stop();
                Console.Error.WriteLine("FindBestSubstrings returned {0} results in {1}", results.Count, stopw.Elapsed);
#endif

                if (results.Count == 0 || results[0].score < 1)
                    yield break;

                var scores = results.ToDictionary(r => r.substring, r => r.score);
                var indexedResults = new IndexedStringCollection(from p in scores where p.Value > 0 select p.Key);
                indexedResults.RemoveOverlapping((a, b) =>
                {
                    // keep the higher scoring one
                    var scoreDiff = scores[a] - scores[b];
                    if (scoreDiff != 0)
                        return scoreDiff;

                    // ...or the shorter one if the scores are equal
                    return b.Length - a.Length;
                });

                foreach (var abbrev in indexedResults)
                    yield return new Result(scores[abbrev], isc.CountOccurrences(abbrev), abbrev);

                foreach (var abbrev in indexedResults)
                    isc.Split(abbrev);

                max -= indexedResults.Count;

#if DEBUG_ABBREV
                stopw.Stop();
#endif
            }

#if DEBUG_ABBREV
            outerStopw.Stop();
            Console.Error.WriteLine("Abbreviation search finished in {0}", outerStopw.Elapsed);
#endif
        }
    }
}
