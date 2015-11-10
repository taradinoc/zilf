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
            throw new NotImplementedException();
        }

        public IStructure GetRest(int skip)
        {
            Contract.Requires(skip >= 0);
            return default(IStructure);
        }
        
        public bool IsEmpty()
        {
            throw new NotImplementedException();
        }

        public ZilObject this[int index]
        {
            get
            {
                throw new NotImplementedException();
            }
            set
            {
                throw new NotImplementedException();
            }
        }

        public int GetLength()
        {
            Contract.Ensures(Contract.Result<int>() > 0 || IsEmpty());
            return default(int);
        }

        public int? GetLength(int limit)
        {
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