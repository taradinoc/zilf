using System;
using System.Collections.Generic;
using System.Diagnostics.Contracts;
using System.Linq;

namespace Zilf.Compiler
{
    internal static class Helpers
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

        private static string EnglishJoin(IEnumerable<string> sequence, string conjunction)
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

                // (1,_) uncapped => "1 or more arguments"
                if (uncapped)
                    return string.Format("{0} or more arguments", r.min);

                // (1,1) => "exactly 1 argument"
                if (r.max == r.min)
                    return string.Format("exactly {0} argument{1}",
                        r.min, r.min == 1 ? "" : "s");

                // (1,2) => "1 or 2 arguments"
                if (r.max == r.min + 1)
                    return string.Format("{0} or {1} arguments", r.min, r.max);

                // (1,3) => "1 to 3 arguments"
                return string.Format("{0} to {1} arguments", r.min, r.max);
            }
            else
            {
                // disjoint ranges
                var unrolled = from r in collapsed
                               from n in Enumerable.Range(r.min, r.max - r.min + 1)
                               select n.ToString();
                if (uncapped)
                    unrolled = unrolled.Concat(Enumerable.Repeat("more", 1));

                return EnglishJoin(unrolled, "or") + " arguments";
            }
        }
    }
}
