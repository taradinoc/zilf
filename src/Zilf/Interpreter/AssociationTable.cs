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

using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Threading;
using Zilf.Interpreter.Values;

namespace Zilf.Interpreter
{
    /// <summary>
    /// A table of three-way associations between ZilObjects.
    /// </summary>
    /// <remarks>
    /// In order to avoid keeping objects alive indefinitely, the table uses weak references to the objects.
    /// The table is implemented as a dictionary of dictionaries, with the outer dictionary keyed by the first object
    /// and the inner dictionaries keyed by the second object.
    /// The table also keeps track of the set of first and second objects that have associations in the table.
    /// </remarks>
    sealed class AssociationTable : IEnumerable<AsocResult>
    {
        private sealed record Entry(ZilObject Value, long Order);

        readonly ConditionalWeakTable<ZilObject, ConditionalWeakTable<ZilObject, Entry>> associations =
            new();
        readonly WeakCountingSet<ZilObject> firsts = new();
        readonly WeakCountingSet<ZilObject> seconds = new();

        long nextOrder;

        /// <summary>
        /// Gets the value associated with a pair of objects.
        /// </summary>
        /// <param name="first">The first object in the pair.</param>
        /// <param name="second">The second object in the pair.</param>
        /// <returns>The associated value, or null if no value is associated with the pair.</returns>
        [System.Diagnostics.Contracts.Pure]
        public ZilObject? GetProp(ZilObject first, ZilObject second)
        {
            if (associations.TryGetValue(first, out var innerTable) && innerTable.TryGetValue(second, out var result))
                return result.Value;

            return null;
        }

        /// <summary>
        /// Sets or clears the value associated with a pair of objects.
        /// </summary>
        /// <param name="first">The first object in the pair.</param>
        /// <param name="second">The second object in the pair.</param>
        /// <param name="value">The value to be associated with the pair, or
        /// null to clear the association.</param>
        public void PutProp(ZilObject first, ZilObject second, ZilObject? value)
        {
            if (value == null)
            {
                if (associations.TryGetValue(first, out var innerTable))
                {
                    firsts.Remove(first);

                    if (innerTable.TryGetValue(second, out _))
                    {
                        innerTable.Remove(second);
                        seconds.Remove(second);
                    }
                }
            }
            else
            {
                if (!associations.TryGetValue(first, out var innerTable))
                {
                    innerTable = new();
                    associations.Add(first, innerTable);
                    firsts.Add(first);
                }

                if (!innerTable.TryGetValue(second, out var entry))
                {
                    seconds.Add(second);
                    innerTable.Add(second, new Entry(value, Interlocked.Increment(ref nextOrder)));
                }
                else
                {
                    innerTable.AddOrUpdate(second, entry with { Value = value });
                }
            }
        }

        public IEnumerator<AsocResult> GetEnumerator()
        {
            //foreach (var first in firsts)
            //{
            //    if (associations.TryGetValue(first, out var innerTable))
            //    {
            //        foreach (var second in seconds)
            //        {
            //            if (innerTable.TryGetValue(second, out var value))
            //            {
            //                yield return new AsocResult { Item = first, Indicator = second, Value = value };
            //            }
            //        }
            //    }
            //}

            var query = from pair in associations
                        let item = pair.Key
                        from innerPair in pair.Value
                        let indicator = innerPair.Key
                        let entry = innerPair.Value
                        orderby entry.Order descending
                        select new AsocResult { Item = item, Indicator = indicator, Value = entry.Value };

            return query.GetEnumerator();
        }

        IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();
    }
}
