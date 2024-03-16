/* Copyright 2010-2024 Tara McGrew
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

using Microsoft.CodeAnalysis.CSharp.Syntax;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;

namespace ZilfSourceGenerators
{
    public class EquatableArray<T>(ImmutableArray<T> array) : IEquatable<EquatableArray<T>>, IEnumerable<T>
        where T : IEquatable<T>
    {
        private readonly ImmutableArray<T> array = array;

        public EquatableArray(IEnumerable<T> items) : this(items.ToImmutableArray())
        {
        }

        public static implicit operator EquatableArray<T>(ImmutableArray<T> array) => new(array);

        public static implicit operator EquatableArray<T>(List<T> list) => new(list);

        public static implicit operator EquatableArray<T>(T[] array) => new(array);

        public static implicit operator ImmutableArray<T>(EquatableArray<T> equatable) => equatable.array;

        public ref readonly T this[int index] => ref array.ItemRef(index);

        public bool Equals(EquatableArray<T> other)
        {
            return array.SequenceEqual(other.array);
        }

        public static bool operator ==(EquatableArray<T> left, EquatableArray<T> right)
        {
            return left.Equals(right);
        }

        public static bool operator !=(EquatableArray<T> left, EquatableArray<T> right)
        {
            return !left.Equals(right);
        }

        public override bool Equals(object obj)
        {
            return obj is EquatableArray<T> other && Equals(other);
        }

        public override int GetHashCode()
        {
            HashCode hash = default;

            foreach (var item in array)
            {
                hash.Add(item);
            }

            return hash.ToHashCode();
        }

        public ImmutableArray<T>.Enumerator GetEnumerator()
        {
            return array.GetEnumerator();
        }

        IEnumerator<T> IEnumerable<T>.GetEnumerator()
        {
            return ((IEnumerable<T>)array).GetEnumerator();
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return ((IEnumerable)array).GetEnumerator();
        }
    }
}
