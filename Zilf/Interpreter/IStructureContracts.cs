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
using Zilf.Interpreter.Values;

namespace Zilf.Interpreter
{
    [ContractClassFor(typeof(IStructure))]
    abstract class IStructureContracts : IStructure
    {
        public ZilObject GetFirst()
        {
            Contract.Ensures(Contract.Result<ZilObject>() != null || IsEmpty);
            return default(ZilObject);
        }

        public IStructure GetRest(int skip)
        {
            Contract.Requires(skip >= 0);
            return default(IStructure);
        }
        
        public IStructure GetBack(int skip)
        {
            Contract.Requires(skip >= 0);
            return default(IStructure);
        }

        public IStructure GetTop()
        {
            Contract.Ensures(Contract.Result<IStructure>() != null);
            return default(IStructure);
        }

        public void Grow(int end, int beginning, ZilObject defaultValue)
        {
            Contract.Requires(end >= 0);
            Contract.Requires(beginning >= 0);
        }

        [Pure]
        public abstract bool IsEmpty { get; }

        public ZilObject this[int index]
        {
            get
            {
                Contract.Requires(index >= 0);
                return default(ZilObject);
            }
            set
            {
                Contract.Requires(index >= 0);
                Contract.Requires(value != null);
            }
        }

        public int GetLength()
        {
            Contract.Ensures(Contract.Result<int>() > 0 || IsEmpty);
            return default(int);
        }

        public int? GetLength(int limit)
        {
            Contract.Requires(limit >= 0);
            Contract.Ensures(Contract.Result<int?>() == null || Contract.Result<int?>().Value <= limit);
            return default(int?);
        }

        public abstract IEnumerator<ZilObject> GetEnumerator();

        IEnumerator IEnumerable.GetEnumerator()
        {
            return GetEnumerator();
        }
    }
}