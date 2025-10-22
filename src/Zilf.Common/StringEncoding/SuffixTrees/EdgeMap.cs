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

namespace Zilf.Common.StringEncoding.SuffixTrees
{
    /// <summary>
    /// A mutable map of edges keyed by the first character of their labels.
    /// </summary>
    /// <typeparam name="T">The type of data associated with each string in the suffix tree.</typeparam>
    /// <remarks>
    /// This structure provides efficient lookup of edges by character, with lazy initialization
    /// of the underlying dictionary to save memory for nodes with no edges.
    /// </remarks>
    struct EdgeMap<T>
    {
        private Dictionary<char, Edge<T>>? dict;

        /// <summary>
        /// Implicitly converts an <see cref="EdgeMap{T}"/> to a <see cref="ReadOnlyEdgeMap{T}"/>.
        /// </summary>
        /// <param name="self">The edge map to convert.</param>
        public static implicit operator ReadOnlyEdgeMap<T>(EdgeMap<T> self)
        {
            return new ReadOnlyEdgeMap<T>(self.dict);
        }

        /// <summary>
        /// Adds a new edge to the map.
        /// </summary>
        /// <param name="c">The character key (first character of the edge label).</param>
        /// <param name="edge">The edge to add.</param>
        /// <exception cref="ArgumentException">Thrown if an edge with the same key already exists.</exception>
        public void Add(char c, Edge<T> edge)
        {
            dict ??= new Dictionary<char, Edge<T>>();

            dict.Add(c, edge);
        }

        /// <summary>
        /// Gets the number of edges in the map.
        /// </summary>
        public int Count => dict == null ? 0 : dict.Count;

        /// <summary>
        /// Attempts to retrieve an edge by its character key.
        /// </summary>
        /// <param name="c">The character key to look up.</param>
        /// <param name="edge">When this method returns, contains the edge associated with the key, if found; otherwise, <c>null</c>.</param>
        /// <returns><c>true</c> if an edge was found; otherwise, <c>false</c>.</returns>
        public bool TryGetValue(char c, [NotNullWhen(true)] out Edge<T>? edge)
        {
            if (dict == null)
            {
                edge = null;
                return false;
            }

            return dict.TryGetValue(c, out edge);
        }

        /// <summary>
        /// Determines whether the map contains an edge with the specified character key.
        /// </summary>
        /// <param name="key">The character key to check.</param>
        /// <returns><c>true</c> if an edge with the key exists; otherwise, <c>false</c>.</returns>
        public bool ContainsKey(char key)
        {
            if (dict == null)
                return false;

            return dict.ContainsKey(key);
        }

        /// <summary>
        /// Gets or sets the edge associated with the specified character key.
        /// </summary>
        /// <param name="c">The character key.</param>
        /// <returns>The edge associated with the key.</returns>
        /// <exception cref="KeyNotFoundException">Thrown when getting an edge that doesn't exist.</exception>
        public Edge<T> this[char c]
        {
            get
            {
                if (dict == null)
                    throw new KeyNotFoundException();
                return dict[c];
            }
            set
            {
                dict ??= new Dictionary<char, Edge<T>>();

                dict[c] = value;
            }
        }

        /// <summary>
        /// Gets a collection of all character keys in the map.
        /// </summary>
        public KeyCollection Keys => new(dict);

        /// <summary>
        /// Gets a collection of all edges in the map.
        /// </summary>
        public ValueCollection Values => new(dict);

        /// <summary>
        /// Returns an enumerator that iterates through the edge map.
        /// </summary>
        /// <returns>An enumerator for the key-value pairs in the map.</returns>
        public IEnumerator<KeyValuePair<char, Edge<T>>> GetEnumerator()
        {
            if (dict == null)
                return Enumerable.Empty<KeyValuePair<char, Edge<T>>>().GetEnumerator();

            return dict.GetEnumerator();
        }

        #region Key/Value Collections

        /// <summary>
        /// Represents a collection of character keys in an edge map.
        /// </summary>
        public readonly struct KeyCollection : IEnumerable<char>
        {
            private readonly Dictionary<char, Edge<T>>? dict;

            /// <summary>
            /// Initializes a new instance of the <see cref="KeyCollection"/> struct.
            /// </summary>
            /// <param name="dict">The underlying dictionary, or <c>null</c> if the edge map is empty.</param>
            internal KeyCollection(Dictionary<char, Edge<T>>? dict)
            {
                this.dict = dict;
            }

            /// <summary>
            /// Returns an enumerator that iterates through the keys.
            /// </summary>
            /// <returns>An enumerator for the character keys.</returns>
            public Enumerator GetEnumerator()
            {
                if (dict == null)
                    return new Enumerator();

                return new Enumerator(dict.Keys.GetEnumerator());
            }

            IEnumerator<char> IEnumerable<char>.GetEnumerator() => GetEnumerator();

            IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();

            /// <summary>
            /// Enumerates the characters in a key collection.
            /// </summary>
            public struct Enumerator : IEnumerator<char>
            {
                private readonly bool hasValue;
                private Dictionary<char, Edge<T>>.KeyCollection.Enumerator tor;

                internal Enumerator(Dictionary<char, Edge<T>>.KeyCollection.Enumerator tor)
                {
                    hasValue = true;
                    this.tor = tor;
                }

                public char Current
                {
                    get
                    {
                        if (!hasValue)
                            throw new InvalidOperationException();

                        return tor.Current;
                    }
                }

                object IEnumerator.Current => Current;

                public void Dispose() => tor.Dispose();

                public bool MoveNext()
                {
                    if (!hasValue)
                        return false;

                    return tor.MoveNext();
                }

                public void Reset() => throw new NotSupportedException();
            }
        }

        /// <summary>
        /// Represents a collection of edges in an edge map.
        /// </summary>
        public readonly struct ValueCollection : IEnumerable<Edge<T>>
        {
            private readonly Dictionary<char, Edge<T>>? dict;

            /// <summary>
            /// Initializes a new instance of the <see cref="ValueCollection"/> struct.
            /// </summary>
            /// <param name="dict">The underlying dictionary, or <c>null</c> if the edge map is empty.</param>
            internal ValueCollection(Dictionary<char, Edge<T>>? dict)
            {
                this.dict = dict;
            }

            /// <summary>
            /// Returns an enumerator that iterates through the edges.
            /// </summary>
            /// <returns>An enumerator for the edges.</returns>
            public Enumerator GetEnumerator()
            {
                if (dict == null)
                    return new Enumerator();

                return new Enumerator(dict.Values.GetEnumerator());
            }

            IEnumerator<Edge<T>> IEnumerable<Edge<T>>.GetEnumerator() => GetEnumerator();

            IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();

            /// <summary>
            /// Enumerates the edges in a value collection.
            /// </summary>
            public struct Enumerator : IEnumerator<Edge<T>>
            {
                private readonly bool hasValue;
                private Dictionary<char, Edge<T>>.ValueCollection.Enumerator tor;

                internal Enumerator(Dictionary<char, Edge<T>>.ValueCollection.Enumerator tor)
                {
                    hasValue = true;
                    this.tor = tor;
                }

                public Edge<T> Current
                {
                    get
                    {
                        if (!hasValue)
                            throw new InvalidOperationException();

                        return tor.Current;
                    }
                }

                object IEnumerator.Current => Current;

                public void Dispose() => tor.Dispose();

                public bool MoveNext()
                {
                    if (!hasValue)
                        return false;

                    return tor.MoveNext();
                }

                public void Reset() => throw new NotSupportedException();
            }
        }

        #endregion
    }
}
