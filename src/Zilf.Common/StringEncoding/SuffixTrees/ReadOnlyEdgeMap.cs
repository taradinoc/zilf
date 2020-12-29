/* Copyright 2010-2020 Jesse McGrew
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
    public struct ReadOnlyEdgeMap<T>
    {
        private readonly Dictionary<char, Edge<T>>? dict;

        internal ReadOnlyEdgeMap(Dictionary<char, Edge<T>>? dict)
        {
            this.dict = dict;
        }

        public int Count => dict == null ? 0 : dict.Count;

        public bool TryGetValue(char c, [NotNullWhen(true)] out IEdge<T>? edge)
        {
            if (dict == null)
            {
                edge = null;
                return false;
            }

            var found = dict.TryGetValue(c, out var e);
            edge = e;
            return found;
        }

        public bool ContainsKey(char key)
        {
            if (dict == null)
                return false;

            return dict.ContainsKey(key);
        }

        public IEdge<T> this[char c]
        {
            get
            {
                if (dict == null)
                    throw new KeyNotFoundException();
                return dict[c];
            }
        }

        public KeyCollection Keys => new KeyCollection(dict);

        public ValueCollection Values => new ValueCollection(dict);

        public IEnumerator<KeyValuePair<char, IEdge<T>>> GetEnumerator()
        {
            if (dict == null)
                return Enumerable.Empty<KeyValuePair<char, IEdge<T>>>().GetEnumerator();

            return dict.Select(p => new KeyValuePair<char, IEdge<T>>(p.Key, p.Value)).GetEnumerator();
        }

        #region Key/Value Collections

        public struct KeyCollection : IEnumerable<char>
        {
            private readonly Dictionary<char, Edge<T>>? dict;

            internal KeyCollection(Dictionary<char, Edge<T>>? dict)
            {
                this.dict = dict;
            }

            public Enumerator GetEnumerator()
            {
                if (dict == null)
                    return new Enumerator();

                return new Enumerator(dict.Keys.GetEnumerator());
            }

            IEnumerator<char> IEnumerable<char>.GetEnumerator() => GetEnumerator();

            IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();

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

        public struct ValueCollection : IEnumerable<IEdge<T>>
        {
            private readonly Dictionary<char, Edge<T>>? dict;

            internal ValueCollection(Dictionary<char, Edge<T>>? dict)
            {
                this.dict = dict;
            }

            public Enumerator GetEnumerator()
            {
                if (dict == null)
                    return new Enumerator();

                return new Enumerator(dict.Values.GetEnumerator());
            }

            IEnumerator<IEdge<T>> IEnumerable<IEdge<T>>.GetEnumerator() => GetEnumerator();

            IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();

            public struct Enumerator : IEnumerator<IEdge<T>>
            {
                private readonly bool hasValue;
                private Dictionary<char, Edge<T>>.ValueCollection.Enumerator tor;

                internal Enumerator(Dictionary<char, Edge<T>>.ValueCollection.Enumerator tor)
                {
                    hasValue = true;
                    this.tor = tor;
                }

                public IEdge<T> Current
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
