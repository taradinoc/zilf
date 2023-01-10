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
using FastHashSet;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Collections;
using System.Linq;

namespace Zilf.Common.StringEncoding.SuffixTrees
{
    internal sealed class Node<T> : INode<T>
    {
        private FastHashSet<T>? data;
        private int depth = -1;
        private int leafCount = -1;
        private int resultCount = -1;
        private EdgeMap<T> edges = new();

        public Node<T>? Suffix { get; set; } = null;
        public ref EdgeMap<T> Edges => ref edges;
        [MemberNotNullWhen(true, nameof(edges))]
        public bool HasEdges => edges.Count > 0;

        IEnumerable<T> INode<T>.Data => data ?? Enumerable.Empty<T>();
        INode<T>? INode<T>.Suffix => Suffix;
        ReadOnlyEdgeMap<T> INode<T>.Edges => edges;

        public Node()
        {
        }

        public IEnumerable<T> GetData()
        {
            if (!HasEdges)
                return data ?? Enumerable.Empty<T>();

            if (data == null)
                return GetDataFromEdges();

            return data.Concat(GetDataFromEdges());
        }

        private IEnumerable<T> GetDataFromEdges()
        {
            return Edges.Values.SelectMany(e => e.Dest.GetData());
        }

        public IEnumerable<(T data, int depth)> GetDataWithDepth()
        {
            if (!HasEdges)
                return data?.Select(d => (d, depth)) ?? Enumerable.Empty<(T, int)>();

            if (data == null)
                return GetDataWithDepthFromEdges();

            return data.Select(d => (d, depth)).Concat(GetDataWithDepthFromEdges());
        }

        private IEnumerable<(T data, int depth)> GetDataWithDepthFromEdges()
        {
            return Edges.Values.SelectMany(e => e.Dest.GetDataWithDepth());
        }

        internal void AddRef(T value)
        {
            for (var node = this; node != null; node = node.Suffix)
            {
                if (node.data == null)
                    node.data = new FastHashSet<T>();

                if (!node.data.Add(value))
                    return;
            }
        }

        public void Annotate()
        {
            AnnotateRecursive(0);
        }

        private struct AnnotationResult
        {
            public int LeafCount { get; init; }
            public int ResultCount { get; init; }

            public int DatalessNodeCount { get; init; }
            public int NodeCount { get; init; }

            public override string ToString()
            {
                return $"{{Dataless={DatalessNodeCount}, Leaves={LeafCount}, Nodes={NodeCount}, Results={ResultCount}}}";
            }
        }

        private AnnotationResult AnnotateRecursive(int depth)
        {
            this.depth = depth;
            resultCount = data == null ? 0 : data.Count;

            int nodeCount = 1;
            int datalessNodeCount = resultCount == 0 ? 1 : 0;

            if (Edges.Count == 0)
            {
                // this is a leaf
                leafCount = 1;
            }
            else
            {
                leafCount = 0;

                foreach (var e in Edges.Values)
                {
                    var a = e.Dest.AnnotateRecursive(depth + e.Label.Length);

                    leafCount += a.LeafCount;
                    resultCount += a.ResultCount;
                    nodeCount += a.NodeCount;
                    datalessNodeCount += a.DatalessNodeCount;
                }
            }

            return new AnnotationResult
            {
                LeafCount = leafCount,
                ResultCount = resultCount,
                NodeCount = nodeCount,
                DatalessNodeCount = datalessNodeCount,
            };
        }

        private static int ThrowIfUnset(int value)
        {
            if (value == -1)
                throw new InvalidOperationException($"Call {nameof(Annotate)} first");

            return value;
        }

        public int LeafCount => ThrowIfUnset(leafCount);
        public int ResultCount => ThrowIfUnset(resultCount);
        public int Depth => ThrowIfUnset(depth);

        private sealed class EdgesWrapper : IReadOnlyDictionary<char, IEdge<T>>
        {
            private readonly Dictionary<char, Edge<T>> dict;

            public EdgesWrapper(Dictionary<char, Edge<T>> dict)
            {
                this.dict = dict;
            }

            public IEdge<T> this[char key] => dict[key];

            public IEnumerable<char> Keys => dict.Keys;

            public IEnumerable<IEdge<T>> Values => dict.Values;

            public int Count => dict.Count;

            public bool ContainsKey(char key) => dict.ContainsKey(key);

            public IEnumerator<KeyValuePair<char, IEdge<T>>> GetEnumerator() =>
                dict.Select(pair => new KeyValuePair<char, IEdge<T>>(pair.Key, pair.Value)).GetEnumerator();

            public bool TryGetValue(char key, [MaybeNullWhen(false)] out IEdge<T> value)
            {
                if (dict.TryGetValue(key, out var temp))
                {
                    value = temp;
                    return true;
                }

                value = null;
                return false;
            }

            IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();
        }
    }

    public interface INode<T>
    {
        IEnumerable<T> Data { get; }
        INode<T>? Suffix { get; }
        int LeafCount { get; }
        int ResultCount { get; }
        int Depth { get; }
        ReadOnlyEdgeMap<T> Edges { get; }
    }
}
