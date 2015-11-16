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

using System.Collections.Generic;
using Zilf.Interpreter.Values;

namespace Zilf.ZModel
{
    internal sealed class AtomNameEqualityComparer : IEqualityComparer<ZilAtom>
    {
        private readonly bool ignoreCase;

        public AtomNameEqualityComparer(bool ignoreCase)
        {
            this.ignoreCase = ignoreCase;
        }

        public bool Equals(ZilAtom x, ZilAtom y)
        {
            if (x == y)
                return true;

            if (ignoreCase)
                return x.Text.ToUpper() == y.Text.ToUpper();
            else
                return x.Text == y.Text;
        }

        public int GetHashCode(ZilAtom obj)
        {
            if (ignoreCase)
                return obj.Text.ToUpper().GetHashCode();
            else
                return obj.Text.GetHashCode();
        }
    }
}
