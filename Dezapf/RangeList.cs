/* Copyright 2010-2017 Jesse McGrew
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

namespace Dezapf
{
    class RangeList<T> : IEnumerable<RangeList<T>.Range>
    {
        public struct Range : IComparable<Range>
        {
            public readonly int Start, Length;
            public readonly T Value;

            public Range(int start, int length, T value)
            {
                Start = start;
                Length = length;
                Value = value;
            }

            public int CompareTo(Range other)
            {
                return Start - other.Start;
            }

            public override string ToString()
            {
                return $"Range {Start} - {Start + Length - 1}";
            }
        }

        public struct Gap : IComparable<Gap>
        {
            public readonly int Start, Length;

            public Gap(int start, int length)
            {
                Start = start;
                Length = length;
            }

            public int CompareTo(Gap other)
            {
                return Start - other.Start;
            }

            public override string ToString()
            {
                return $"Gap {Start} - {Start + Length - 1}";
            }
        }

        readonly List<Range> list;

        public RangeList()
        {
            list = new List<Range>();
        }

        public void Add(Range range)
        {
            if (range.Length < 1)
                throw new ArgumentOutOfRangeException(nameof(range), "New range must have positive length");

            int idx = list.BinarySearch(range);

            if (idx < 0)
            {
                idx = ~idx;
                if (idx >= list.Count || range.Start + range.Length <= list[idx].Start)
                {
                    if (idx == 0 || list[idx - 1].Start + list[idx - 1].Length <= range.Start)
                    {
                        list.Insert(idx, range);
                        return;
                    }
                }
            }

            throw new ArgumentException("New range overlaps with existing range");
        }

        public void AddRange(int start, int length, T value)
        {
            Add(new Range(start, length, value));
        }

        public bool TryGetValue(int position, out T value)
        {
            if (list.Count > 0)
            {
                Range r = new Range(position, 0, default(T));
                int idx = list.BinarySearch(r);

                if (idx < 0)
                    idx = ~idx;

                if (idx < list.Count)
                {
                    r = list[idx];
                    if (position >= r.Start && position < r.Start + r.Length)
                    {
                        value = r.Value;
                        return true;
                    }

                    if (idx > 0)
                    {
                        r = list[idx - 1];
                        if (position >= r.Start && position < r.Start + r.Length)
                        {
                            value = r.Value;
                            return true;
                        }
                    }
                }
            }

            value = default(T);
            return false;
        }

        public bool Contains(int position) => TryGetValue(position, out _);

        public delegate bool RangeCombiner(int start1, int length1, T value1,
                                         int start2, int length2, T value2,
                                         out T newValue);

        public void Coalesce(int coalesceStart, int coalesceLength, RangeCombiner combine)
        {
            for (int i = 0; i < list.Count - 1; i++)
            {
                var r = list[i];
                var r2 = list[i + 1];

                if (r.Start < coalesceStart)
                    continue;

                if (r2.Start + r2.Length > coalesceStart + coalesceLength)
                    break;

                if (r.Start + r.Length == r2.Start)
                {
                    bool combined = combine(
                        r.Start, r.Length, r.Value,
                        r2.Start, r2.Length, r2.Value,
                        out T newValue);
                    if (combined)
                    {
                        list[i] = new Range(r.Start, r.Length + r2.Length, newValue);
                        list.RemoveAt(i + 1);
                        i--;
                    }
                }
            }
        }

        public IEnumerable<Gap> FindGaps(int windowStart, int windowLength)
        {
            int last = windowStart;

            foreach (var r in list)
            {
                if (r.Start < last)
                {
                    // before window
                }
                else if (r.Start == last)
                {
                    // flush with previous range
                    last += r.Length;
                }
                else if (r.Start >= windowStart + windowLength)
                {
                    // after window
                    break;
                }
                else
                {
                    // gap
                    yield return new Gap(last, r.Start - last);
                    last = r.Start + r.Length;
                }
            }

            if (last < windowStart + windowLength)
                yield return new Gap(last, windowStart + windowLength - last);
        }

        public IEnumerator<Range> GetEnumerator()
        {
            return list.GetEnumerator();
        }

        #region IEnumerable Members

        System.Collections.IEnumerator System.Collections.IEnumerable.GetEnumerator()
        {
            return GetEnumerator();
        }

        #endregion
    }
}
