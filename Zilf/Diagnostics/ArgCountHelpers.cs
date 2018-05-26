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
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using JetBrains.Annotations;

namespace Zilf.Diagnostics
{
    struct CountableString
    {
        public readonly string Text;
        public readonly bool Plural;

        public CountableString([NotNull] string text, bool plural)
        {

            Text = text;
            Plural = plural;
        }

        public override string ToString()
        {
            return Text;
        }
    }

    static class ArgCountHelpers
    {
        public static IEnumerable<T> Collapse<T>([NotNull] IEnumerable<T> sequence,
            [NotNull] Func<T, T, bool> match, [NotNull] Func<T, T, T> combine)
        {
            //Contract.Ensures(Contract.Result<IEnumerable<T>>() != null);

            using (var tor = sequence.GetEnumerator())
            {
                if (!tor.MoveNext())
                    yield break;

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

        [NotNull]
        static string EnglishJoin([NotNull] IEnumerable<string> sequence, [NotNull] string conjunction)
        {

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

        [NotNull]
        public static string FormatArgCount([NotNull] IEnumerable<ArgCountRange> ranges)
        {

            FormatArgCount(ranges, out var cs);
            return string.Format(CultureInfo.CurrentCulture, "{0} argument{1}", cs.Text, cs.Plural ? "s" : "");
        }

        /// <exception cref="ArgumentException">No ranges provided</exception>
        public static void FormatArgCount([NotNull] IEnumerable<ArgCountRange> ranges, out CountableString result)
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
                throw new ArgumentException("No ranges provided", nameof(ranges));

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
                FormatArgCount(range, out result);
                return;
            }

            // disjoint ranges
            var unrolled = from r in collapsed
                            from n in Enumerable.Range(r.min, r.max - r.min + 1)
                            select n.ToString(CultureInfo.InvariantCulture);
            if (uncapped)
                unrolled = unrolled.Concat(Enumerable.Repeat("more", 1));

            result = new CountableString(EnglishJoin(unrolled, "or"), true);
        }

        public static void FormatArgCount(ArgCountRange range, out CountableString result)
        {
            // (1,_) uncapped => "1 or more arguments"
            if (range.MaxArgs == null)
            {
                result = new CountableString(string.Format(CultureInfo.InvariantCulture, "{0} or more", range.MinArgs), true);
                return;
            }

            // (1,1) => "exactly 1 argument"
            if (range.MaxArgs == range.MinArgs)
            {
                result = new CountableString(string.Format(CultureInfo.InvariantCulture, "exactly {0}", range.MinArgs), range.MinArgs != 1);
                return;
            }

            // (0,1) => "at most 1 argument"
            if (range.MinArgs == 0)
            {
                result = new CountableString(string.Format(CultureInfo.InvariantCulture, "at most {0}", range.MaxArgs), range.MaxArgs != 1);
                return;
            }

            // (1,2) => "1 or 2 arguments"
            if (range.MaxArgs == range.MinArgs + 1)
            {
                result = new CountableString(string.Format(CultureInfo.InvariantCulture, "{0} or {1}", range.MinArgs, range.MaxArgs), true);
                return;
            }

            // (1,3) => "1 to 3 arguments"
            result = new CountableString(string.Format(CultureInfo.InvariantCulture, "{0} to {1}", range.MinArgs, range.MaxArgs), true);
        }
    }
}
