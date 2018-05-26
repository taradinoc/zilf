/* Copyright 2010-2018 Jesse McGrew
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
using JetBrains.Annotations;

namespace Zilf.Interpreter
{
    class WeakCountingSet<T> : IEnumerable<T>
        where T : class
    {
        class Cell
        {
            public WeakReference<T> Ref;
            public int Count;
        }

        readonly Dictionary<int, List<Cell>> buckets = new Dictionary<int, List<Cell>>();

        public void Add([NotNull] T value)
        {

            var hashCode = value.GetHashCode();

            if (buckets.TryGetValue(hashCode, out var bucket))
            {
                foreach (var cell in bucket)
                {
                    if (cell.Ref.TryGetTarget(out T existingValue) && existingValue.Equals(value))
                    {
                        cell.Count++;
                        return;
                    }
                }
            }
            else
            {
                bucket = new List<Cell>();
                buckets.Add(hashCode, bucket);
            }

            bucket.Add(new Cell { Ref = new WeakReference<T>(value), Count = 1 });
        }

        public void Remove([NotNull] T value)
        {

            var hashCode = value.GetHashCode();

            if (buckets.TryGetValue(hashCode, out var bucket))
            {
                for (int i = 0; i < bucket.Count; i++)
                {
                    var cell = bucket[i];

                    if (cell.Ref.TryGetTarget(out T existingValue) && existingValue.Equals(value))
                    {
                        cell.Count--;

                        if (cell.Count <= 0)
                            bucket.RemoveAt(i);

                        return;
                    }
                }
            }
        }

        public IEnumerator<T> GetEnumerator()
        {
            foreach (var pair in buckets)
            {
                foreach (var cell in pair.Value)
                {
                    if (cell.Ref.TryGetTarget(out T value))
                        yield return value;
                }
            }
        }

        IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();
    }
}