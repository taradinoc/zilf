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
using System.Collections;
using System.Collections.Generic;

namespace Zilf.Common.StringEncoding.SuffixTrees
{
    /// <summary>
    /// Maintains a sorted collection of the top-scoring substrings, limited to a maximum number of results.
    /// </summary>
    /// <remarks>
    /// The leaderboard automatically keeps only the highest-scoring entries up to the specified maximum.
    /// When at capacity, entries with scores below the minimum score in the leaderboard are rejected.
    /// Ties are broken using a custom comparison function provided at construction time.
    /// </remarks>
    sealed class Leaderboard : IEnumerable<(int score, string substring)>
    {
        private readonly int maxResults;

        private readonly SortedList<(int score, string substring), string> winners;
        private int minScore = int.MinValue;

        /// <summary>
        /// A comparer that sorts entries by score (descending) and then by a custom tie-breaking function.
        /// </summary>
        private class TieBreakingComparer : IComparer<(int score, string str)>
        {
            private readonly Comparison<string> func;

            /// <summary>
            /// Initializes a new instance of the <see cref="TieBreakingComparer"/> class.
            /// </summary>
            /// <param name="func">The function to use for breaking ties between entries with equal scores.</param>
            public TieBreakingComparer(Comparison<string> func)
            {
                this.func = func;
            }

            /// <summary>
            /// Compares two entries by score (descending) and then by the tie-breaking function.
            /// </summary>
            /// <param name="x">The first entry to compare.</param>
            /// <param name="y">The second entry to compare.</param>
            /// <returns>A negative value if <paramref name="x"/> should come before <paramref name="y"/>,
            /// zero if they are equal, or a positive value if <paramref name="x"/> should come after <paramref name="y"/>.</returns>
            public int Compare((int score, string str) x, (int score, string str) y)
            {
                // descending sort by score...
                if (x.score != y.score)
                    return y.score - x.score;

                // then by tie-breaker
                return -func(x.str, y.str);
            }
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="Leaderboard"/> class.
        /// </summary>
        /// <param name="maxResults">The maximum number of results to maintain in the leaderboard.</param>
        /// <param name="tieBreaker">A function to use for breaking ties between entries with equal scores.</param>
        public Leaderboard(int maxResults, Comparison<string> tieBreaker)
        {
            winners = new SortedList<(int score, string substring), string>(new TieBreakingComparer(tieBreaker));
            this.maxResults = maxResults;
        }

        /// <summary>
        /// Attempts to add a new entry to the leaderboard.
        /// </summary>
        /// <param name="score">The score for the substring.</param>
        /// <param name="substring">The substring to add.</param>
        /// <returns><c>true</c> if the entry was added; <c>false</c> if it was rejected (score too low or duplicate entry).</returns>
        /// <remarks>
        /// If the leaderboard is at capacity and the new score is lower than the minimum score currently in the leaderboard,
        /// the entry is rejected. If the leaderboard is at capacity and the new score qualifies, the lowest-scoring entry
        /// is removed to make room. Duplicate entries (same score and substring) are also rejected.
        /// </remarks>
        public bool Add(int score, ReadOnlySpan<char> substring)
        {
            if (winners.Count == maxResults && score < minScore)
                return false;

            var str = new string(substring);
            var key = (score, str);

            if (winners.ContainsKey(key))
                return false;

            winners.Add(key, str);

            if (winners.Count > maxResults)
                winners.RemoveAt(winners.Count - 1);

            minScore = winners.Keys[^1].score;

            return true;
        }

        /// <summary>
        /// Returns an enumerator that iterates through the leaderboard entries in descending order by score.
        /// </summary>
        /// <returns>An enumerator for the leaderboard entries.</returns>
        public IEnumerator<(int score, string substring)> GetEnumerator() => winners.Keys.GetEnumerator();

        IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();
    }
}
