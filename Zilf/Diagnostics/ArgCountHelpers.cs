/* Copyright 2010, 2015 Jesse McGrew
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
using System.Diagnostics.Contracts;
using System.Linq;

namespace Zilf.Diagnostics
{
    static class ArgCountHelpers
    {
        public static IEnumerable<T> Collapse<T>(IEnumerable<T> sequence,
            Func<T, T, bool> match, Func<T, T, T> combine)
        {
            Contract.Requires(sequence != null);
            Contract.Requires(match != null);
            Contract.Requires(combine != null);
            //Contract.Ensures(Contract.Result<IEnumerable<T>>() != null);

            var tor = sequence.GetEnumerator();
            if (tor.MoveNext())
            {
                var last = tor.Current;

                while (tor.MoveNext())
                {
                    var current = tor.Current;
                    if (match(last, current))
                    {
                        last = combine(last, current);
                    }
                    else
                    {
                        yield return last;
                        last = current;
                    }
                }

                yield return last;
            }
        }

        static string EnglishJoin(IEnumerable<string> sequence, string conjunction)
        {
            Contract.Requires(sequence != null);
            Contract.Requires(conjunction != null);
            Contract.Ensures(Contract.Result<string>() != null);

            var items = sequence.ToArray();

            switch (items.Length)
            {
                case 0:
                    return "";
                case 1:
                    return items[0];
                case 2:
                    return items[0] + " " + conjunction + " " + items[1];
                default:
                    var last = items.Length - 1;
                    items[last] = conjunction + " " + items[last];
                    return string.Join(", ", items);
            }
        }

        public static string FormatArgCount(IEnumerable<ArgCountRange> ranges)
        {
            Contract.Requires(ranges != null);
            Contract.Ensures(!string.IsNullOrEmpty(Contract.Result<string>()));

            string countDescription, pluralSuffix;
            FormatArgCount(ranges, out countDescription, out pluralSuffix);
            return string.Format("{0} argument{1}", countDescription, pluralSuffix);
        }

        public static void FormatArgCount(IEnumerable<ArgCountRange> ranges,
            out string countDescription, out string pluralSuffix)
        {
            var allCounts = new List<int>();
            bool uncapped = false;
            foreach (var r in ranges)
            {
                if (r.MaxArgs == null)
                {
                    uncapped = true;
                    allCounts.Add(r.MinArgs);
                }
                else
                {
                    for (int i = r.MinArgs; i <= r.MaxArgs; i++)
                        allCounts.Add(i);
                }
            }

            if (allCounts.Count == 0)
                throw new ArgumentException("No ranges provided");

            allCounts.Sort();

            // (1,2), (2,4) => (1,4)
            var collapsed = Collapse(
                allCounts.Select(c => new { min = c, max = c }),
                (a, b) => b.min <= a.max + 1,
                (a, b) => new { a.min, b.max })
                .ToArray();

            if (collapsed.Length == 1)
            {
                var r = collapsed[0];
                var range = new ArgCountRange(r.min, uncapped ? (int?)null : r.max);
                FormatArgCount(range, out countDescription, out pluralSuffix);
                return;
            }

            // disjoint ranges
            var unrolled = from r in collapsed
                            from n in Enumerable.Range(r.min, r.max - r.min + 1)
                            select n.ToString();
            if (uncapped)
                unrolled = unrolled.Concat(Enumerable.Repeat("more", 1));

            countDescription = EnglishJoin(unrolled, "or");
            pluralSuffix = "s";
        }

        public static void FormatArgCount(ArgCountRange range, out string countDescription, out string pluralSuffix)
        {
            // (1,_) uncapped => "1 or more arguments"
            if (range.MaxArgs == null)
            {
                countDescription = string.Format("{0} or more", range.MinArgs);
                pluralSuffix = "s";
                return;
            }

            // (1,1) => "exactly 1 argument"
            if (range.MaxArgs == range.MinArgs)
            {
                countDescription = string.Format("exactly {0}", range.MinArgs);
                pluralSuffix = range.MinArgs == 1 ? "" : "s";
                return;
            }

            // (0,1) => "at most 1 argument"
            if (range.MinArgs == 0)
            {
                countDescription = string.Format("at most {0}", range.MaxArgs);
                pluralSuffix = range.MaxArgs == 1 ? "" : "s";
                return;
            }

            // (1,2) => "1 or 2 arguments"
            if (range.MaxArgs == range.MinArgs + 1)
            {
                countDescription = string.Format("{0} or {1}", range.MinArgs, range.MaxArgs);
                pluralSuffix = "s";
                return;
            }

            // (1,3) => "1 to 3 arguments"
            countDescription = string.Format("{0} to {1}", range.MinArgs, range.MaxArgs);
            pluralSuffix = "s";
            return;
        }
    }
}
