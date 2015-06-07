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
using System.Diagnostics.Contracts;
using Zilf.Language;

namespace Zilf.Interpreter.Values
{
    [BuiltinType(StdAtom.FALSE, PrimType.LIST)]
    class ZilFalse : ZilObject, IStructure
    {
        private readonly ZilList value;

        [ChtypeMethod]
        public ZilFalse(ZilList value)
        {
            Contract.Requires(value != null);
            this.value = value;
        }

        [ContractInvariantMethod]
        private void ObjectInvariant()
        {
            Contract.Invariant(value != null);
        }

        public override string ToString()
        {
            return "#FALSE " + value.ToString();
        }

        public override ZilAtom GetTypeAtom(Context ctx)
        {
            return ctx.GetStdAtom(StdAtom.FALSE);
        }

        public override PrimType PrimType
        {
            get { return PrimType.LIST; }
        }

        public override ZilObject GetPrimitive(Context ctx)
        {
            return value;
        }

        public override bool IsTrue
        {
            get { return false; }
        }

        public override bool Equals(object obj)
        {
            ZilFalse other = obj as ZilFalse;
            return other != null && other.value.Equals(this.value);
        }

        public override int GetHashCode()
        {
            return value.GetHashCode();
        }

        #region IStructure Members

        public ZilObject GetFirst()
        {
            return value.First;
        }

        public IStructure GetRest(int skip)
        {
            return ((IStructure)value).GetRest(skip);
        }

        public bool IsEmpty()
        {
            return value.IsEmpty;
        }

        public ZilObject this[int index]
        {
            get
            {
                return ((IStructure)value)[index];
            }
            set
            {
                ((IStructure)value)[index] = value;
            }
        }

        public int GetLength()
        {
            return ((IStructure)value).GetLength();
        }

        public int? GetLength(int limit)
        {
            return ((IStructure)value).GetLength(limit);
        }

        #endregion
    }
}