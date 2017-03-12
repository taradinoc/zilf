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

using System.Collections.Generic;
using Zilf.Interpreter.Values;

namespace Zilf.ZModel
{
    sealed class AtomNameEqualityComparer : IEqualityComparer<ZilAtom>
    {
        readonly bool ignoreCase;

        public AtomNameEqualityComparer(bool ignoreCase)
        {
            this.ignoreCase = ignoreCase;
        }

        public bool Equals(ZilAtom x, ZilAtom y)
        {
            if (x == y)
                return true;

            if (x == null || y == null)
                return false;

            if (ignoreCase)
                return x.Text.ToUpperInvariant() == y.Text.ToUpperInvariant();

            return x.Text == y.Text;
        }

        public int GetHashCode(ZilAtom obj)
        {
            if (obj == null || obj.Text == null)
                return 0;

            if (ignoreCase)
                return obj.Text.ToUpperInvariant().GetHashCode();

            return obj.Text.GetHashCode();
        }
    }
}
