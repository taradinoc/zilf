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
    sealed class Leaderboard : IEnumerable<(int score, string substring)>
    {
        private readonly int maxResults;
        private readonly Comparison<string> tieBreaker;

        private readonly SortedList<(int score, string substring), string> winners;
        private int minScore = int.MinValue;
        private int maxScore = int.MaxValue;

        private class TieBreakingComparer : IComparer<(int score, string str)>
        {
            private readonly Comparison<string> func;

            public TieBreakingComparer(Comparison<string> func)
            {
                this.func = func;
            }

            public int Compare((int score, string str) x, (int score, string str) y)
            {
                // descending sort by score...
                if (x.score != y.score)
                    return y.score - x.score;

                // then by tie-breaker
                return -func(x.str, y.str);
            }
        }

        public Leaderboard(int maxResults, Comparison<string> tieBreaker)
        {
            winners = new SortedList<(int score, string substring), string>(new TieBreakingComparer(tieBreaker));
            this.maxResults = maxResults;
            this.tieBreaker = tieBreaker;
        }

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

            maxScore = winners.Keys[0].score;
            minScore = winners.Keys[^1].score;

            return true;
        }

        public IEnumerator<(int score, string substring)> GetEnumerator() => winners.Keys.GetEnumerator();

        IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();
    }
}
