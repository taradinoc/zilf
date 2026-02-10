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
    /// <summary>
    /// Represents a node in a suffix tree, containing edges to child nodes and associated data values.
    /// </summary>
    /// <typeparam name="T">The type of data associated with each string in the suffix tree.</typeparam>
    internal sealed class Node<T> : INode<T>
    {
        private FastHashSet<T>? data;
        private int depth = -1;
        private int leafCount = -1;
        private int resultCount = -1;
        private EdgeMap<T> edges = new();

        /// <summary>
        /// Gets or sets the suffix link, which points to the node representing the suffix of this node's path.
        /// </summary>
        /// <remarks>
        /// Suffix links are used in Ukkonen's algorithm for efficient suffix tree construction.
        /// </remarks>
        public Node<T>? Suffix { get; set; } = null;

        /// <summary>
        /// Gets a reference to the edge map for this node.
        /// </summary>
        public ref EdgeMap<T> Edges => ref edges;

        /// <summary>
        /// Gets a value indicating whether this node has any outgoing edges.
        /// </summary>
        [MemberNotNullWhen(true, nameof(edges))]
        public bool HasEdges => edges.Count > 0;

        IEnumerable<T> INode<T>.Data => data ?? Enumerable.Empty<T>();
        INode<T>? INode<T>.Suffix => Suffix;
        ReadOnlyEdgeMap<T> INode<T>.Edges => edges;

        /// <summary>
        /// Initializes a new instance of the <see cref="Node{T}"/> class.
        /// </summary>
        public Node()
        {
        }

        /// <summary>
        /// Gets all data values associated with this node and its descendants.
        /// </summary>
        /// <returns>An enumerable sequence of data values.</returns>
        /// <remarks>
        /// This method recursively collects data from all descendant nodes in the subtree.
        /// </remarks>
        public IEnumerable<T> GetData()
        {
            if (!HasEdges)
                return data ?? Enumerable.Empty<T>();

            if (data == null)
                return GetDataFromEdges();

            return data.Concat(GetDataFromEdges());
        }

        /// <summary>
        /// Gets all data values from child nodes by recursively traversing edges.
        /// </summary>
        /// <returns>An enumerable sequence of data values from descendant nodes.</returns>
        private IEnumerable<T> GetDataFromEdges()
        {
            return Edges.Values.SelectMany(e => e.Dest.GetData());
        }

        /// <summary>
        /// Gets all data values associated with this node and its descendants, along with their depths in the tree.
        /// </summary>
        /// <returns>An enumerable sequence of tuples containing data values and their corresponding depths.</returns>
        /// <remarks>
        /// The depth represents the number of characters from the root to the point where the data was added.
        /// This method requires that <see cref="Annotate"/> has been called first.
        /// </remarks>
        public IEnumerable<(T data, int depth)> GetDataWithDepth()
        {
            if (!HasEdges)
                return data?.Select(d => (d, depth)) ?? Enumerable.Empty<(T, int)>();

            if (data == null)
                return GetDataWithDepthFromEdges();

            return data.Select(d => (d, depth)).Concat(GetDataWithDepthFromEdges());
        }

        /// <summary>
        /// Gets all data values with depths from child nodes by recursively traversing edges.
        /// </summary>
        /// <returns>An enumerable sequence of tuples containing data values and depths from descendant nodes.</returns>
        private IEnumerable<(T data, int depth)> GetDataWithDepthFromEdges()
        {
            return Edges.Values.SelectMany(e => e.Dest.GetDataWithDepth());
        }

        /// <summary>
        /// Adds a data reference to this node and all nodes reachable via suffix links.
        /// </summary>
        /// <param name="value">The data value to add.</param>
        /// <remarks>
        /// This method follows the suffix link chain from this node back to the root, adding the value
        /// to each node along the way. This ensures that all suffixes of a string are properly annotated
        /// with the string's associated data.
        /// </remarks>
        internal void AddRef(T value)
        {
            for (var node = this; node != null; node = node.Suffix)
            {
                node.data ??= new FastHashSet<T>();

                if (!node.data.Add(value))
                    return;
            }
        }

        /// <summary>
        /// Annotates the entire subtree rooted at this node with computed metrics.
        /// </summary>
        /// <remarks>
        /// This method must be called before accessing <see cref="Depth"/>, <see cref="LeafCount"/>,
        /// or <see cref="ResultCount"/> properties. It performs a depth-first traversal to compute
        /// these metrics for the entire subtree.
        /// </remarks>
        public void Annotate()
        {
            AnnotateRecursive(0);
        }

        /// <summary>
        /// Contains the results of annotating a subtree.
        /// </summary>
        private readonly record struct AnnotationResult
        {
            /// <summary>
            /// Gets the number of leaf nodes in the subtree.
            /// </summary>
            /// <summary>
            /// Gets the number of leaf nodes in the subtree.
            /// </summary>
            public int LeafCount { get; init; }

            /// <summary>
            /// Gets the total number of distinct data values in the subtree.
            /// </summary>
            public int ResultCount { get; init; }

            /// <summary>
            /// Gets the number of nodes that have no associated data.
            /// </summary>
            public int DatalessNodeCount { get; init; }

            /// <summary>
            /// Gets the total number of nodes in the subtree.
            /// </summary>
            public int NodeCount { get; init; }

            /// <summary>
            /// Returns a string representation of the annotation results.
            /// </summary>
            /// <returns>A formatted string showing the metrics.</returns>
            public override string ToString()
            {
                return $"{{Dataless={DatalessNodeCount}, Leaves={LeafCount}, Nodes={NodeCount}, Results={ResultCount}}}";
            }
        }

        /// <summary>
        /// Recursively annotates this node and all its descendants with computed metrics.
        /// </summary>
        /// <param name="depth">The depth of this node in the tree (number of characters from the root).</param>
        /// <returns>Aggregated metrics for the entire subtree rooted at this node.</returns>
        /// <remarks>
        /// This method performs a post-order traversal, computing metrics for child nodes first,
        /// then aggregating them at the current node.
        /// </remarks>
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

        /// <summary>
        /// Throws an exception if the specified value indicates the node has not been annotated.
        /// </summary>
        /// <param name="value">The value to check (should be -1 if not yet annotated).</param>
        /// <returns>The input value if it is valid.</returns>
        /// <exception cref="InvalidOperationException">Thrown if the value is -1, indicating <see cref="Annotate"/> must be called first.</exception>
        private static int ThrowIfUnset(int value)
        {
            if (value == -1)
                throw new InvalidOperationException($"Call {nameof(Annotate)} first");

            return value;
        }

        /// <summary>
        /// Gets the number of leaf nodes in the subtree rooted at this node.
        /// </summary>
        /// <exception cref="InvalidOperationException">Thrown if <see cref="Annotate"/> has not been called.</exception>
        public int LeafCount => ThrowIfUnset(leafCount);

        /// <summary>
        /// Gets the total number of distinct data values in the subtree rooted at this node.
        /// </summary>
        /// <exception cref="InvalidOperationException">Thrown if <see cref="Annotate"/> has not been called.</exception>
        public int ResultCount => ThrowIfUnset(resultCount);

        /// <summary>
        /// Gets the depth of this node in the tree (number of characters from the root).
        /// </summary>
        /// <exception cref="InvalidOperationException">Thrown if <see cref="Annotate"/> has not been called.</exception>
        public int Depth => ThrowIfUnset(depth);

        /// <summary>
        /// Provides a read-only wrapper around an edge dictionary for the public interface.
        /// </summary>
        private sealed class EdgesWrapper : IReadOnlyDictionary<char, IEdge<T>>
        {
            private readonly Dictionary<char, Edge<T>> dict;

            /// <summary>
            /// Initializes a new instance of the <see cref="EdgesWrapper"/> class.
            /// </summary>
            /// <param name="dict">The underlying edge dictionary to wrap.</param>
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

    /// <summary>
    /// Represents a read-only view of a node in a suffix tree.
    /// </summary>
    /// <typeparam name="T">The type of data associated with each string in the suffix tree.</typeparam>
    public interface INode<T>
    {
        /// <summary>
        /// Gets the data values directly associated with this node (not including descendants).
        /// </summary>
        IEnumerable<T> Data { get; }

        /// <summary>
        /// Gets the suffix link, which points to the node representing the suffix of this node's path.
        /// </summary>
        INode<T>? Suffix { get; }

        /// <summary>
        /// Gets the number of leaf nodes in the subtree rooted at this node.
        /// </summary>
        int LeafCount { get; }

        /// <summary>
        /// Gets the total number of distinct data values in the subtree rooted at this node.
        /// </summary>
        int ResultCount { get; }

        /// <summary>
        /// Gets the depth of this node in the tree (number of characters from the root).
        /// </summary>
        int Depth { get; }

        /// <summary>
        /// Gets a read-only view of the edges emanating from this node.
        /// </summary>
        ReadOnlyEdgeMap<T> Edges { get; }

        /// <summary>
        /// Gets all data values associated with this node and its descendants, along with their depths in the tree.
        /// </summary>
        /// <returns>An enumerable sequence of tuples containing data values and their corresponding depths.</returns>
        /// <remarks>
        /// The depth represents the number of characters from the root to the point where the data was added.
        /// This method requires that the tree has been annotated first.
        /// </remarks>
        IEnumerable<(T data, int depth)> GetDataWithDepth();
    }
}
