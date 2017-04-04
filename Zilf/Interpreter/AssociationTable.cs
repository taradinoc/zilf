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
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics.Contracts;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;
using Zilf.Interpreter.Values;

namespace Zilf.Interpreter
{
    class AssociationTable : IEnumerable<AsocResult>
    {
        readonly ConditionalWeakTable<ZilObject, ConditionalWeakTable<ZilObject, ZilObject>> associations =
            new ConditionalWeakTable<ZilObject, ConditionalWeakTable<ZilObject, ZilObject>>();
        readonly WeakCountingSet<ZilObject> firsts = new WeakCountingSet<ZilObject>();
        readonly WeakCountingSet<ZilObject> seconds = new WeakCountingSet<ZilObject>();

        /// <summary>
        /// Gets the value associated with a pair of objects.
        /// </summary>
        /// <param name="first">The first object in the pair.</param>
        /// <param name="second">The second object in the pair.</param>
        /// <returns>The associated value, or null if no value is associated with the pair.</returns>
        [Pure]
        public ZilObject GetProp(ZilObject first, ZilObject second)
        {
            Contract.Requires(first != null);
            Contract.Requires(second != null);

            if (associations.TryGetValue(first, out var innerTable) && innerTable.TryGetValue(second, out var result))
                return result;

            return null;
        }

        /// <summary>
        /// Sets or clears the value associated with a pair of objects.
        /// </summary>
        /// <param name="first">The first object in the pair.</param>
        /// <param name="second">The second object in the pair.</param>
        /// <param name="value">The value to be associated with the pair, or
        /// null to clear the association.</param>
        public void PutProp(ZilObject first, ZilObject second, ZilObject value)
        {
            Contract.Requires(first != null);
            Contract.Requires(second != null);

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
                    innerTable = new ConditionalWeakTable<ZilObject, ZilObject>();
                    associations.Add(first, innerTable);
                    firsts.Add(first);
                    seconds.Add(second);
                }
                else if (innerTable.TryGetValue(second, out _))
                {
                    innerTable.Remove(second);
                }
                else
                {
                    seconds.Add(second);
                }

                innerTable.Add(second, value);
            }
        }

        public AsocResult[] ToArray()
        {
            var result = new List<AsocResult>();

            foreach (var first in firsts)
            {
                if (associations.TryGetValue(first, out var innerTable))
                {
                    foreach (var second in seconds)
                    {
                        if (innerTable.TryGetValue(second, out var value))
                        {
                            result.Add(new AsocResult { Item = first, Indicator = second, Value = value });
                        }
                    }
                }
            }

            return result.ToArray();
        }

        public IEnumerator<AsocResult> GetEnumerator()
        {
            foreach (var first in firsts)
            {
                if (associations.TryGetValue(first, out var innerTable))
                {
                    foreach (var second in seconds)
                    {
                        if (innerTable.TryGetValue(second, out var value))
                        {
                            yield return new AsocResult { Item = first, Indicator = second, Value = value };
                        }
                    }
                }
            }
        }

        IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();
    }
}
