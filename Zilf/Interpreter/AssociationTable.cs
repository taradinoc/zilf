/* Copyright 2010, 2016 Jesse McGrew
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
        private readonly ConditionalWeakTable<ZilObject, ConditionalWeakTable<ZilObject, ZilObject>> associations =
            new ConditionalWeakTable<ZilObject, ConditionalWeakTable<ZilObject, ZilObject>>();
        private readonly WeakCountingSet<ZilObject> firsts = new WeakCountingSet<ZilObject>();
        private readonly WeakCountingSet<ZilObject> seconds = new WeakCountingSet<ZilObject>();

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

            ConditionalWeakTable<ZilObject, ZilObject> innerTable;
            ZilObject result;
            if (associations.TryGetValue(first, out innerTable) && innerTable.TryGetValue(second, out result))
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

            ConditionalWeakTable<ZilObject, ZilObject> innerTable;
            ZilObject dummy;

            if (value == null)
            {
                if (associations.TryGetValue(first, out innerTable))
                {
                    firsts.Remove(first);

                    if (innerTable.TryGetValue(second, out dummy))
                    {
                        innerTable.Remove(second);
                        seconds.Remove(second);
                    }
                }
            }
            else
            {
                if (!associations.TryGetValue(first, out innerTable))
                {
                    innerTable = new ConditionalWeakTable<ZilObject, ZilObject>();
                    associations.Add(first, innerTable);
                    firsts.Add(first);
                    seconds.Add(second);
                }
                else if (innerTable.TryGetValue(second, out dummy))
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
                ConditionalWeakTable<ZilObject, ZilObject> innerTable;

                if (associations.TryGetValue(first, out innerTable))
                {
                    foreach (var second in seconds)
                    {
                        ZilObject value;

                        if (innerTable.TryGetValue(second, out value))
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
                ConditionalWeakTable<ZilObject, ZilObject> innerTable;

                if (associations.TryGetValue(first, out innerTable))
                {
                    foreach (var second in seconds)
                    {
                        ZilObject value;

                        if (innerTable.TryGetValue(second, out value))
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
