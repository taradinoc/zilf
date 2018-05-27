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
using System.Diagnostics;
using JetBrains.Annotations;

namespace Zilf.Interpreter.Values
{
    [BuiltinPrimType(PrimType.LIST)]
    abstract class ZilListoidBase : ZilObject, IStructure
    {
        [CanBeNull]
        public abstract ZilObject First { get; set; }
        [CanBeNull]
        public abstract ZilListoidBase Rest { get; set; }  // TODO: make this ZilListoidBase (or ZilListBase?) instead of ZilList

        public abstract bool IsEmpty { get; }

        public sealed override PrimType PrimType => PrimType.LIST;

        public abstract override ZilObject GetPrimitive(Context ctx);

        public abstract IEnumerator<ZilObject> GetEnumerator();

        System.Collections.IEnumerator System.Collections.IEnumerable.GetEnumerator() => GetEnumerator();

        public ZilObject GetFirst() => First;
        public abstract IStructure GetRest(int skip);
        public IStructure GetBack(int skip) => throw new NotSupportedException();
        public IStructure GetTop() => throw new NotSupportedException();
        public void Grow(int end, int beginning, ZilObject defaultValue) =>
            throw new NotSupportedException();

        [CanBeNull]
        public abstract ZilObject this[int index] { get; set; }
        public abstract int GetLength();
        public abstract int? GetLength(int limit);

        /// <summary>
        /// Enumerates the items of the list, yielding a final <see langword="null"/> instead of repeating if the list is recursive.
        /// </summary>
        /// <returns></returns>
        [ItemCanBeNull]
        [System.Diagnostics.Contracts.Pure]
        public IEnumerable<ZilObject> EnumerateNonRecursive()
        {
            var seen = new HashSet<ZilListoidBase>(ReferenceEqualityComparer<ZilListoidBase>.Instance);
            var list = this;

            while (!list.IsEmpty)
            {
                if (seen.Contains(list))
                {
                    yield return null;
                    yield break;
                }

                seen.Add(list);
                yield return list.First;
                list = list.Rest;
                Debug.Assert(list != null);
            }
        }

        [NotNull]
        public ZilList AsZilList()
        {
            if (this is ZilList list)
                return list;

            return new ZilList(First, Rest);
        }
    }
}