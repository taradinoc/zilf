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
using Zilf.Interpreter.Values;

namespace Zilf.Interpreter
{
    internal struct AssocPair : IEquatable<AssocPair>
    {
        public readonly ZilObject First;
        public readonly ZilObject Second;

        public AssocPair(ZilObject first, ZilObject second)
        {
            this.First = first;
            this.Second = second;
        }

        public override bool Equals(object obj)
        {
            if (obj is AssocPair)
                return Equals((AssocPair)obj);
            else
                return false;
        }

        public bool Equals(AssocPair other)
        {
            return First.Equals(other.First) && Second.Equals(other.Second);
        }

        public override int GetHashCode()
        {
            return First.GetHashCode() ^ Second.GetHashCode();
        }
    }
}
