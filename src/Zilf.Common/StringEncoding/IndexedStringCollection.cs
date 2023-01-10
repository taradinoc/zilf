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
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Runtime.InteropServices;
using Zilf.Common.StringEncoding.SuffixTrees;

namespace Zilf.Common.StringEncoding
{
    public sealed class IndexedStringCollection : ICollection<string>
    {
        private readonly List<string> strings;
        private SuffixTree<int>? suffixTree;
        private int maxStringLength;

        public int Count => ((ICollection<string>)strings).Count;

        public bool IsReadOnly => false;

        public IndexedStringCollection()
        {
            strings = new List<string>();
        }

        public IndexedStringCollection(IEnumerable<string> strings)
        {
            this.strings = strings switch
            {
                ICollection coll => new List<string>(coll.Count),
                ICollection<string> coll => new List<string>(coll.Count),
                _ => new List<string>(),
            };

            foreach (var s in strings)
            {
                this.strings.Add(s);
                maxStringLength = Math.Max(maxStringLength, s.Length);
            }
        }

        [MemberNotNull(nameof(suffixTree))]
        private SuffixTree<int> BuildSuffixTree()
        {
            if (suffixTree == null)
            {
                suffixTree = new SuffixTree<int>();
                for (int i = 0; i < strings.Count; i++)
                    suffixTree.Add(strings[i], i);
            }

            return suffixTree;
        }

        public void Add(string str)
        {
            suffixTree = null;
            strings.Add(str);
            maxStringLength = Math.Max(maxStringLength, str.Length);
        }

        public delegate int EdgeCostFunction(ReadOnlySpan<char> chars);
        public delegate int EvaluationFunction(int totalEdgeCost, int occurrences, ReadOnlySpan<char> chars);

        public int[,] GetSuffixPrefixOverlaps()
        {
            int k = strings.Count;
            var result = new int[k, k];
            var stacks = new Stack<int>[k];

            for (int i = 0; i < k; i++)
            {
                stacks[i] = new Stack<int>();
                stacks[i].Push(0);
            }

            var root = BuildSuffixTree().GetRoot();
            GetSuffixPrefixOverlapsRecursive(strings, result, stacks, root);
            return result;
        }

        private static void GetSuffixPrefixOverlapsRecursive(List<string> strings, int[,] result, Stack<int>[] stacks, INode<int> node)
        {
            foreach (var j in node.Data)
            {
                stacks[j].Push(node.Depth);
            }

            foreach (var j in node.Data)
            {
                if (node.Depth == strings[j].Length)
                {
                    for (int i = 0; i < strings.Count; i++)
                        result[i, j] = stacks[i].Peek();
                }
            }

            foreach (var e in node.Edges.Values)
            {
                GetSuffixPrefixOverlapsRecursive(strings, result, stacks, e.Dest);
            }

            foreach (var j in node.Data)
                stacks[j].Pop();
        }

        public bool RemoveOverlapping(Comparison<string> comparer)
        {
            var overlaps = GetSuffixPrefixOverlaps();

            int i = strings.Count - 1, j = i - 1;

            var horspools = new Horspool[strings.Count];

            for (int h = 0; h < strings.Count; h++)
                horspools[h] = new Horspool(strings[h]);

            var deletions = new HashSet<int>();

            bool Overlaps(int a, int b) =>
                overlaps[a, b] >= 1 ||
                overlaps[b, a] >= 1 ||
                horspools[a].FindIn(strings[b]) >= 0 ||
                horspools[b].FindIn(strings[a]) >= 0;

            while (i > 0)
            {
                if (!deletions.Contains(j) && Overlaps(i, j))
                {
                    var keepJ = comparer(strings[i], strings[j]) < 0;

                    if (keepJ)
                    {
                        deletions.Add(i);
                        i--;
                        j = i - 1;
                    }
                    else
                    {
                        deletions.Add(j);
                        j--;
                    }
                }
                else
                {
                    j--;
                }

                if (j < 0)
                {
                    do
                    {
                        i--;
                    } while (deletions.Contains(i));

                    j = i - 1;
                }
            }

            if (deletions.Count > 0)
            {
                foreach (var index in deletions.OrderByDescending(i => i))
                    strings.RemoveAt(index);

                suffixTree = null;
                return true;
            }

            return false;
        }

        public IEnumerable<(int score, string substring)> FindBestSubstrings(
            int maxResults,
            EdgeCostFunction edgeCostFunction,
            EvaluationFunction evaluationFunction)
        {
            return FindBestSubstrings(maxResults, edgeCostFunction, evaluationFunction, (a, b) => 0);
        }

        private List<char>? bestSubstringBuffer;

        public IEnumerable<(int score, string substring)> FindBestSubstrings(
            int maxResults,
            EdgeCostFunction edgeCostFunction,
            EvaluationFunction evaluationFunction,
            Comparison<string> tieBreaker)
        {
            var root = BuildSuffixTree().GetRoot();
            var stringSoFar = bestSubstringBuffer ??= new List<char>(maxStringLength);
            var leaderboard = new Leaderboard(maxResults, tieBreaker);
            FindBestSubstringsRecursive(edgeCostFunction, evaluationFunction, root, 0, stringSoFar, leaderboard);
            return leaderboard;
        }

        private static void FindBestSubstringsRecursive(
            EdgeCostFunction edgeCostFunction, EvaluationFunction evaluationFunction,
            INode<int> node, int costSoFar, List<char> stringSoFar,
            Leaderboard leaderboard)
        {
            int originalLength = stringSoFar.Count;

            if (originalLength > 0)
            {
                int bestScore = evaluationFunction(costSoFar, node.ResultCount, CollectionsMarshal.AsSpan(stringSoFar));
                leaderboard.Add(bestScore, CollectionsMarshal.AsSpan(stringSoFar));
            }

            foreach (var e in node.Edges.Values)
            {
                var label = e.Label.Span;

                foreach (var c in label)
                    stringSoFar.Add(c);

                var nextCost = costSoFar + edgeCostFunction(label);
                FindBestSubstringsRecursive(
                    edgeCostFunction,
                    evaluationFunction,
                    e.Dest,
                    nextCost,
                    stringSoFar,
                    leaderboard);

                stringSoFar.RemoveRange(originalLength, stringSoFar.Count - originalLength);
            }
        }

        public bool Split(string delimiter)
        {
            var splits = from r in BuildSuffixTree().SearchWithDepth(delimiter)
                         orderby r.data, r.depth
                         group r.depth by r.data;

            bool changed = false;

            foreach (var g in splits)
            {
                changed = true;

                var i = g.Key;
                var str = strings[i];

                foreach (var depth in g)
                {
                    var start = str.Length - depth;
                    var end = start + delimiter.Length;

                    // if this isn't the first split in this string, then strings[i] != str
                    if (end < strings[i].Length)
                    {
                        strings.Add(strings[i][end..]);
                    }

                    strings[i] = str[..start];
                }
            }

            if (!changed)
                return false;

            strings.RemoveAll(s => s.Length == 0);

            suffixTree = null;
            return true;
        }

        public int CountOccurrences(string word)
        {
            return BuildSuffixTree().CountOccurrences(word);
        }

        public void Clear()
        {
            strings.Clear();
            suffixTree = null;
        }

        public bool Contains(string item) => strings.Contains(item);

        public void CopyTo(string[] array, int arrayIndex) => strings.CopyTo(array, arrayIndex);

        public bool Remove(string item)
        {
            if (strings.Remove(item))
            {
                suffixTree = null;
                return true;
            }

            return false;
        }

        public IEnumerator<string> GetEnumerator() => strings.GetEnumerator();

        IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();
    }
}
