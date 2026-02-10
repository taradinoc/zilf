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
    /// <summary>
    /// A collection of strings with efficient substring searching, overlap detection, and substring analysis capabilities.
    /// </summary>
    /// <remarks>
    /// This collection uses a suffix tree internally to provide fast substring searches and analysis operations.
    /// The suffix tree is built lazily and invalidated when the collection is modified.
    /// </remarks>
    public sealed class IndexedStringCollection : ICollection<string>
    {
        private readonly List<string> strings;
        private SuffixTree<int>? suffixTree;
        private int maxStringLength;

        /// <summary>
        /// Gets the number of strings in the collection.
        /// </summary>
        public int Count => ((ICollection<string>)strings).Count;

        /// <summary>
        /// Gets a value indicating whether the collection is read-only.
        /// </summary>
        public bool IsReadOnly => false;

        /// <summary>
        /// Initializes a new instance of the <see cref="IndexedStringCollection"/> class.
        /// </summary>
        public IndexedStringCollection()
        {
            strings = new List<string>();
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="IndexedStringCollection"/> class with the specified strings.
        /// </summary>
        /// <param name="strings">The initial strings to add to the collection.</param>
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

        /// <summary>
        /// Builds or returns the cached suffix tree for the collection.
        /// </summary>
        /// <returns>A suffix tree containing all strings in the collection.</returns>
        /// <remarks>
        /// The suffix tree is built lazily on first access and cached until the collection is modified.
        /// Each string is indexed by its position in the collection.
        /// </remarks>
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

        /// <summary>
        /// Adds a string to the collection.
        /// </summary>
        /// <param name="str">The string to add.</param>
        /// <remarks>
        /// Adding a string invalidates the cached suffix tree, which will be rebuilt on the next operation that requires it.
        /// </remarks>
        public void Add(string str)
        {
            suffixTree = null;
            strings.Add(str);
            maxStringLength = Math.Max(maxStringLength, str.Length);
        }

        /// <summary>
        /// Represents a function that computes the cost of an edge label.
        /// </summary>
        /// <param name="chars">The characters in the edge label.</param>
        /// <returns>The cost associated with the edge.</returns>
        public delegate int EdgeCostFunction(ReadOnlySpan<char> chars);

        /// <summary>
        /// Represents a function that evaluates the quality of a substring based on its cost and frequency.
        /// </summary>
        /// <param name="totalEdgeCost">The cumulative cost of all edges leading to this substring.</param>
        /// <param name="occurrences">The number of times this substring appears in the collection.</param>
        /// <param name="chars">The characters in the substring.</param>
        /// <returns>A score representing the quality of the substring (higher is better).</returns>
        public delegate int EvaluationFunction(int totalEdgeCost, int occurrences, ReadOnlySpan<char> chars);

        /// <summary>
        /// Computes a matrix of suffix-prefix overlaps between all pairs of strings in the collection.
        /// </summary>
        /// <returns>A 2D array where element [i,j] contains the length of the longest suffix of string i
        /// that is also a prefix of string j.</returns>
        /// <remarks>
        /// This is useful for finding optimal orderings or concatenations of strings to minimize total length.
        /// The algorithm uses the suffix tree to efficiently compute all overlaps in O(n) time where n is the
        /// total length of all strings.
        /// </remarks>
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

        /// <summary>
        /// Recursively traverses the suffix tree to compute suffix-prefix overlaps.
        /// </summary>
        /// <param name="strings">The list of strings in the collection.</param>
        /// <param name="result">The matrix to populate with overlap lengths.</param>
        /// <param name="stacks">A stack for each string tracking the current overlap depth.</param>
        /// <param name="node">The current node being visited.</param>
        /// <remarks>
        /// This method performs a depth-first traversal, maintaining a stack for each string to track
        /// the maximum overlap depth encountered on the path from the root.
        /// </remarks>
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

        /// <summary>
        /// Removes overlapping strings from the collection, keeping the preferred string from each overlapping pair.
        /// </summary>
        /// <param name="comparer">A comparison function to determine which of two overlapping strings to keep.
        /// Should return a negative value to keep the first string, positive to keep the second.</param>
        /// <returns><c>true</c> if any strings were removed; otherwise, <c>false</c>.</returns>
        /// <remarks>
        /// Two strings are considered overlapping if one is a suffix of the other, one is a prefix of the other,
        /// or one appears as a substring within the other. This method uses both the suffix tree and Boyer-Moore-Horspool
        /// string searching for efficient overlap detection. The suffix tree is invalidated if any strings are removed.
        /// </remarks>
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

        /// <summary>
        /// Finds the highest-scoring substrings in the collection according to custom cost and evaluation functions.
        /// </summary>
        /// <param name="maxResults">The maximum number of results to return.</param>
        /// <param name="edgeCostFunction">A function that computes the cost of an edge label.</param>
        /// <param name="evaluationFunction">A function that scores substrings based on cost and frequency.</param>
        /// <returns>An enumerable of tuples containing scores and substrings, ordered by score descending.</returns>
        /// <remarks>
        /// This overload uses a default tie-breaker that treats all tied strings as equal.
        /// </remarks>
        public IEnumerable<(int score, string substring)> FindBestSubstrings(
            int maxResults,
            EdgeCostFunction edgeCostFunction,
            EvaluationFunction evaluationFunction)
        {
            return FindBestSubstrings(maxResults, edgeCostFunction, evaluationFunction, (a, b) => 0);
        }

        private List<char>? bestSubstringBuffer;

        /// <summary>
        /// Finds the highest-scoring substrings in the collection according to custom cost and evaluation functions.
        /// </summary>
        /// <param name="maxResults">The maximum number of results to return.</param>
        /// <param name="edgeCostFunction">A function that computes the cost of an edge label.</param>
        /// <param name="evaluationFunction">A function that scores substrings based on cost and frequency.</param>
        /// <param name="tieBreaker">A comparison function to break ties between substrings with equal scores.</param>
        /// <returns>An enumerable of tuples containing scores and substrings, ordered by score descending.</returns>
        /// <remarks>
        /// This method traverses the suffix tree, evaluating every possible substring using the provided functions.
        /// The edge cost function typically represents the storage cost of a substring, while the evaluation function
        /// combines cost and frequency to determine overall value. A common use case is finding substrings that
        /// appear frequently enough to justify their storage cost for compression purposes.
        /// </remarks>
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

        /// <summary>
        /// Recursively traverses the suffix tree to find and evaluate substrings.
        /// </summary>
        /// <param name="edgeCostFunction">A function that computes the cost of an edge label.</param>
        /// <param name="evaluationFunction">A function that scores substrings based on cost and frequency.</param>
        /// <param name="node">The current node being visited.</param>
        /// <param name="costSoFar">The cumulative edge cost from the root to this node.</param>
        /// <param name="stringSoFar">The substring from the root to this node.</param>
        /// <param name="leaderboard">The leaderboard to which scored substrings are added.</param>
        /// <remarks>
        /// This method performs a depth-first traversal, evaluating the substring at each node and recursively
        /// processing child nodes. The string buffer is restored after processing each child to avoid allocations.
        /// Non-overlapping occurrence counts are computed at each node using a greedy left-to-right strategy,
        /// which correctly accounts for self-overlapping substrings.
        /// </remarks>
        private void FindBestSubstringsRecursive(
            EdgeCostFunction edgeCostFunction, EvaluationFunction evaluationFunction,
            INode<int> node, int costSoFar, List<char> stringSoFar,
            Leaderboard leaderboard)
        {
            int originalLength = stringSoFar.Count;

            if (originalLength > 0)
            {
                int nonOverlappingCount = ComputeNonOverlappingCount(node, originalLength);
                int bestScore = evaluationFunction(costSoFar, nonOverlappingCount, CollectionsMarshal.AsSpan(stringSoFar));
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

        /// <summary>
        /// Splits strings in the collection at every occurrence of a delimiter substring.
        /// </summary>
        /// <param name="delimiter">The delimiter substring to split on.</param>
        /// <returns><c>true</c> if any strings were split; otherwise, <c>false</c>.</returns>
        /// <remarks>
        /// This method uses the suffix tree to efficiently find all occurrences of the delimiter across all strings.
        /// After splitting, empty strings are removed from the collection, and the suffix tree is invalidated.
        /// If a string contains multiple occurrences of the delimiter, it will be split into multiple parts.
        /// </remarks>
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

        /// <summary>
        /// Counts the total number of occurrences of a substring across all strings in the collection.
        /// </summary>
        /// <param name="word">The substring to count.</param>
        /// <returns>The number of occurrences of the substring.</returns>
        /// <remarks>
        /// This method uses the suffix tree for efficient counting in O(m) time where m is the length of the word.
        /// Note that this counts all occurrences including overlapping ones. For non-overlapping counts,
        /// use <see cref="CountNonOverlappingOccurrences"/>.
        /// </remarks>
        public int CountOccurrences(string word)
        {
            return BuildSuffixTree().CountOccurrences(word);
        }

        /// <summary>
        /// Counts the number of non-overlapping occurrences of a substring across all strings in the collection.
        /// </summary>
        /// <param name="word">The substring to count.</param>
        /// <returns>The number of non-overlapping occurrences of the substring, computed using a greedy
        /// left-to-right strategy within each string.</returns>
        /// <remarks>
        /// Unlike <see cref="CountOccurrences"/>, this method accounts for self-overlapping substrings.
        /// For example, "AAA" occurs 3 times in "AAAAA" but can only be used once in a non-overlapping
        /// manner. This method uses a greedy left-to-right approach to maximize the count of non-overlapping
        /// occurrences within each string.
        /// </remarks>
        public int CountNonOverlappingOccurrences(string word)
        {
            var tree = BuildSuffixTree();
            var node = tree.SearchNode(word);
            if (node == null)
                return 0;

            return ComputeNonOverlappingCount(node, word.Length);
        }

        /// <summary>
        /// Computes the number of non-overlapping occurrences of a substring represented by a suffix tree node.
        /// </summary>
        /// <param name="node">The suffix tree node whose subtree contains all occurrences.</param>
        /// <param name="substringLength">The length of the substring being counted.</param>
        /// <returns>The number of non-overlapping occurrences.</returns>
        private int ComputeNonOverlappingCount(INode<int> node, int substringLength)
        {
            // Collect all occurrence positions grouped by source string
            var byString = new Dictionary<int, List<int>>();
            foreach (var (stringIndex, depth) in node.GetDataWithDepth())
            {
                if (!byString.TryGetValue(stringIndex, out var positions))
                    byString[stringIndex] = positions = [];
                positions.Add(strings[stringIndex].Length - depth);
            }

            int total = 0;
            foreach (var positions in byString.Values)
            {
                positions.Sort();
                int lastEnd = int.MinValue;
                foreach (var pos in positions)
                {
                    if (pos >= lastEnd)
                    {
                        total++;
                        lastEnd = pos + substringLength;
                    }
                }
            }

            return total;
        }

        /// <summary>
        /// Removes all strings from the collection.
        /// </summary>
        /// <remarks>
        /// This method also invalidates the cached suffix tree.
        /// </remarks>
        public void Clear()
        {
            strings.Clear();
            suffixTree = null;
        }

        /// <summary>
        /// Determines whether the collection contains a specific string.
        /// </summary>
        /// <param name="item">The string to locate.</param>
        /// <returns><c>true</c> if the string is found; otherwise, <c>false</c>.</returns>
        public bool Contains(string item) => strings.Contains(item);

        /// <summary>
        /// Copies the strings in the collection to an array, starting at a particular array index.
        /// </summary>
        /// <param name="array">The destination array.</param>
        /// <param name="arrayIndex">The zero-based index in the array at which copying begins.</param>
        public void CopyTo(string[] array, int arrayIndex) => strings.CopyTo(array, arrayIndex);

        /// <summary>
        /// Removes the first occurrence of a specific string from the collection.
        /// </summary>
        /// <param name="item">The string to remove.</param>
        /// <returns><c>true</c> if the string was removed; otherwise, <c>false</c>.</returns>
        /// <remarks>
        /// Removing a string invalidates the cached suffix tree.
        /// </remarks>
        public bool Remove(string item)
        {
            if (strings.Remove(item))
            {
                suffixTree = null;
                return true;
            }

            return false;
        }

        /// <summary>
        /// Returns an enumerator that iterates through the strings in the collection.
        /// </summary>
        /// <returns>An enumerator for the collection.</returns>
        public IEnumerator<string> GetEnumerator() => strings.GetEnumerator();

        IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();
    }
}
