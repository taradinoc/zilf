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
using System.Collections;
using System.Collections.Generic;

namespace Zilf.Interpreter.Values
{
    class ZilStructuredHash : ZilHash, IStructure
    {
        public ZilStructuredHash(ZilAtom type, PrimType primtype, ZilObject primvalue)
            : base(type, primtype, primvalue)
        {
        }

        public override bool Equals(object obj)
        {
            return (obj is ZilStructuredHash && ((ZilStructuredHash)obj).type == this.type &&
                    ((ZilStructuredHash)obj).primvalue.Equals(this.primvalue));
        }

        public override int GetHashCode()
        {
            return type.GetHashCode() ^ primvalue.GetHashCode();
        }

        #region IStructure Members

        public ZilObject GetFirst()
        {
            return ((IStructure)primvalue).GetFirst();
        }

        public IStructure GetRest(int skip)
        {
            return ((IStructure)primvalue).GetRest(skip);
        }

        public bool IsEmpty()
        {
            return ((IStructure)primvalue).IsEmpty();
        }

        public ZilObject this[int index]
        {
            get
            {
                return ((IStructure)primvalue)[index];
            }
            set
            {
                ((IStructure)primvalue)[index] = value;
            }
        }

        public int GetLength()
        {
            return ((IStructure)primvalue).GetLength();
        }

        public int? GetLength(int limit)
        {
            return ((IStructure)primvalue).GetLength(limit);
        }

        #endregion

        public IEnumerator<ZilObject> GetEnumerator()
        {
            return ((IStructure)primvalue).GetEnumerator();
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return GetEnumerator();
        }
    }
}