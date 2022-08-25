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
    struct EdgeMap<T>
    {
        private Dictionary<char, Edge<T>>? dict;

        public static implicit operator ReadOnlyEdgeMap<T>(EdgeMap<T> self)
        {
            return new ReadOnlyEdgeMap<T>(self.dict);
        }

        public void Add(char c, Edge<T> edge)
        {
            dict ??= new Dictionary<char, Edge<T>>();

            dict.Add(c, edge);
        }

        public int Count => dict == null ? 0 : dict.Count;

        public bool TryGetValue(char c, [NotNullWhen(true)] out Edge<T>? edge)
        {
            if (dict == null)
            {
                edge = null;
                return false;
            }

            return dict.TryGetValue(c, out edge);
        }

        public bool ContainsKey(char key)
        {
            if (dict == null)
                return false;

            return dict.ContainsKey(key);
        }

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

        public KeyCollection Keys => new(dict);

        public ValueCollection Values => new(dict);

        public IEnumerator<KeyValuePair<char, Edge<T>>> GetEnumerator()
        {
            if (dict == null)
                return Enumerable.Empty<KeyValuePair<char, Edge<T>>>().GetEnumerator();

            return dict.GetEnumerator();
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

        public struct ValueCollection : IEnumerable<Edge<T>>
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

            IEnumerator<Edge<T>> IEnumerable<Edge<T>>.GetEnumerator() => GetEnumerator();

            IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();

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
