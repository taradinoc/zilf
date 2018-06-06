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
using System.Runtime.CompilerServices;
using Zilf.Interpreter.Values;

namespace Zilf.Interpreter
{
    class ReferenceEqualityComparer<T> : IEqualityComparer<T>
        where T : class
    {
        public static readonly IEqualityComparer<T> Instance = new ReferenceEqualityComparer<T>();

        ReferenceEqualityComparer()
        {
        }

        public bool Equals(T x, T y)
        {
            return ReferenceEquals(x, y);
        }

        public int GetHashCode(T obj)
        {
            return RuntimeHelpers.GetHashCode(obj);
        }
    }

    class StructuralEqualityComparer : IEqualityComparer<ZilObject>
    {
        public static readonly StructuralEqualityComparer Instance = new StructuralEqualityComparer();

        StructuralEqualityComparer()
        {
        }

        public bool Equals(ZilObject x, ZilObject y)
        {
            return x?.StructurallyEquals(y) ?? y == null;
        }

        public int GetHashCode(ZilObject obj)
        {
            if (obj == null)
                throw new ArgumentNullException(nameof(obj));

            return obj.ToString().GetHashCode();
        }
    }
}
