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

using System.Collections;
using System.Collections.Generic;

namespace Zilf.Interpreter.Values
{
    [BuiltinMeta]
    sealed class ZilStructuredHash : ZilHashBase<IStructure>, IStructure
    {
        public ZilStructuredHash(ZilAtom type, PrimType primType, IStructure primValue)
            : base(type, primType, primValue) { }

        public override ZilObject GetPrimitive(Context ctx) => (ZilObject)primValue;

        public IEnumerator<ZilObject> GetEnumerator() => primValue.GetEnumerator();
        IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();

        public ZilObject GetFirst() => primValue.GetFirst();
        public IStructure GetRest(int skip) => primValue.GetRest(skip);
        public IStructure GetBack(int skip) => primValue.GetBack(skip);
        public IStructure GetTop() => primValue.GetTop();

        public ZilObject this[int index]
        {
            get => primValue[index];
            set => primValue[index] = value;
        }

        public bool IsEmpty => primValue.IsEmpty;

        public int GetLength() => primValue.GetLength();
        public int? GetLength(int limit) => primValue.GetLength(limit);

        public void Grow(int end, int beginning, ZilObject defaultValue) =>
            primValue.Grow(end, beginning, defaultValue);
    }
}