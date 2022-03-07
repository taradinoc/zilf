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
using System.Linq;

namespace Zilf.Common.StringEncoding.SuffixTrees
{
    /// <summary>
    /// Implements a Generalized Suffix Tree using Ukkonen's algorithm.
    /// </summary>
    /// <remarks>
    /// See also:
    /// <list type="bullet">
    /// <item>https://github.com/gmamaladze/trienet</item>
    /// <item>https://github.com/abahgat/suffixtree</item>
    /// <item>https://www.abahgat.com/project/suffix-tree/</item>
    /// <item>http://programmerspatch.blogspot.com/2013/02/ukkonens-suffix-tree-algorithm.html</item>
    /// <item>https://www.geeksforgeeks.org/suffix-tree-application-2-searching-all-patterns/</item>
    /// </list>
    /// </remarks>
    public sealed class SuffixTree<T>
    {
        private readonly Node<T> root;
        private Node<T> activeLeaf;
        private bool annotated;

        public SuffixTree()
        {
            activeLeaf = root = new Node<T>();
        }

        public IEnumerable<T> Search(string word)
        {
            var tmpNode = SearchNode(word);
            if (tmpNode == null)
                return Enumerable.Empty<T>();
            return tmpNode.GetData().Distinct();
        }

        public IEnumerable<(T data, int depth)> SearchWithDepth(string word)
        {
            Annotate();

            var tmpNode = SearchNode(word);
            if (tmpNode == null)
                return Enumerable.Empty<(T, int)>();
            return tmpNode.GetDataWithDepth().Distinct();
        }

        public int CountOccurrences(string word)
        {
            Annotate();

            var tmpNode = SearchNode(word);
            if (tmpNode == null)
                return 0;
            return tmpNode.ResultCount;
        }

        private void Annotate()
        {
            if (!annotated)
            {
                root.Annotate();
                annotated = true;
            }
        }

        public INode<T> GetRoot()
        {
            Annotate();
            return root;
        }

        private Node<T>? SearchNode(ReadOnlySpan<char> word)
        {
            var currentNode = root;

            for (int i = 0; i < word.Length; i++)
            {
                if (!currentNode.Edges.TryGetValue(word[i], out var currentEdge))
                    return null;

                var label = currentEdge.Label;
                int lenToMatch = Math.Min(word.Length - i, label.Length);

                if (!word.Slice(i, lenToMatch).Equals(label.Span[..lenToMatch], StringComparison.Ordinal))
                    return null;

                if (label.Length >= word.Length - i)
                    return currentEdge.Dest;

                currentNode = currentEdge.Dest;
                i += lenToMatch - 1;
            }

            return null;
        }

        public void Add(string key, T value)
        {
            annotated = false;

            activeLeaf = root;

            var keyMemory = key.AsMemory();
            var s = root;

            int start = 0, end = 0;
            for (int i = 0; i < keyMemory.Length; i++)
            {
                end++;

                var (updateLastNode, updateTrimmed) = Update(s, keyMemory[start..end], keyMemory[i..], value);
                var (canonizeS, canonizeTrimmed) = Canonize(updateLastNode, keyMemory[(start + updateTrimmed)..end]);
                s = canonizeS;
                start += updateTrimmed + canonizeTrimmed;
            }

            if (activeLeaf.Suffix == null && activeLeaf != root && activeLeaf != s)
                activeLeaf.Suffix = s;
        }

        private static (bool contained, Node<T> lastNode) TestAndSplit(Node<T> inputs, ReadOnlyMemory<char> stringPart,
            char t, ReadOnlyMemory<char> remainder, T value)
        {
            var (s, trimmed) = Canonize(inputs, stringPart);
            var str = stringPart[trimmed..];

            if (str.Length != 0)
            {
                var g = s.Edges[str.Span[0]];
                var label = g.Label;
                if (label.Length > str.Length && label.Span[str.Length] == t)
                    return (true, s);

                var newLabel = label[str.Length..];

                var r = new Node<T>();
                var newEdge = new Edge<T>(str, r);

                g.Label = newLabel;

                r.Edges.Add(newLabel.Span[0], g);
                s.Edges[str.Span[0]] = newEdge;

                return (false, r);
            }

            if (!s.Edges.TryGetValue(t, out var e))
                return (false, s);

            if (remainder.Span.Equals(e.Label.Span, StringComparison.Ordinal))
            {
                e.Dest.AddRef(value);
                return (true, s);
            }

            if (remainder.Span.StartsWith(e.Label.Span))
                return (true, s);

            if (e.Label.Span.StartsWith(remainder.Span))
            {
                var newNode = new Node<T>();
                newNode.AddRef(value);

                var newEdge = new Edge<T>(remainder, newNode);

                e.Label = e.Label[remainder.Length..];

                newNode.Edges.Add(e.Label.Span[0], e);

                s.Edges[t] = newEdge;

                return (false, s);
            }

            return (true, s);
        }

        private static (Node<T> lastNode, int leftTrimmedChars) Canonize(Node<T> s, ReadOnlyMemory<char> inputStr)
        {
            var trimmed = 0;

            if (inputStr.Length == 0)
                return (s, trimmed);

            var currentNode = s;
            var str = inputStr;
            var g = s.Edges[str.Span[0]];

            while (g != null && str.Span.StartsWith(g.Label.Span))
            {
                str = str[g.Label.Length..];
                trimmed += g.Label.Length;
                currentNode = g.Dest;
                if (str.Length > 0)
                    g = currentNode.Edges[str.Span[0]];
            }

            return (currentNode, trimmed);
        }

        private (Node<T> lastNode, int trimmed) Update(Node<T> inputNode, ReadOnlyMemory<char> stringPart, ReadOnlyMemory<char> rest, T value)
        {
            var trimmed = 0;

            var s = inputNode;
            var tempStr = stringPart;
            var newChar = stringPart.Span[^1];

            var oldRoot = root;

            var (endpoint, r) = TestAndSplit(s, tempStr[..^1], newChar, rest, value);

            Node<T> leaf;
            while (!endpoint)
            {
                if (r.Edges.TryGetValue(newChar, out var tempEdge))
                {
                    leaf = tempEdge.Dest;
                }
                else
                {
                    leaf = new Node<T>();
                    leaf.AddRef(value);
                    var newEdge = new Edge<T>(rest, leaf);
                    r.Edges.Add(newChar, newEdge);
                }

                if (activeLeaf != root)
                    activeLeaf.Suffix = leaf;

                activeLeaf = leaf;

                if (oldRoot != root)
                    oldRoot.Suffix = r;

                oldRoot = r;

                if (s.Suffix == null)
                {
                    tempStr = tempStr[1..];
                    trimmed++;
                }
                else
                {
                    var (canonizeLastNode, canonizeTrimmed) = Canonize(s.Suffix, SafeCutLastChar(tempStr));
                    s = canonizeLastNode;
                    tempStr = tempStr[canonizeTrimmed..];
                    trimmed += canonizeTrimmed;
                }

                (endpoint, r) = TestAndSplit(s, SafeCutLastChar(tempStr), newChar, rest, value);
            }

            if (oldRoot != root)
                oldRoot.Suffix = r;

            return (s, trimmed);
        }

        private static ReadOnlyMemory<char> SafeCutLastChar(ReadOnlyMemory<char> seq)
        {
            if (seq.Length == 0)
                return seq;

            return seq[..^1];
        }
    }

    public static class SpanExtensions
    {
        public static int Sum<T>(this ReadOnlySpan<T> span, Func<T, int> selector)
        {
            int result = 0;

            foreach (var i in span)
                result += selector(i);
            return result;
        }
    }
}
