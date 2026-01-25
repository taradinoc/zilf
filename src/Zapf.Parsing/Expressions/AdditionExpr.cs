/* Copyright 2010-2023 Tara McGrew
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

namespace Zapf.Parsing.Expressions
{
    public sealed class AdditionExpr(AsmExpr left, AsmExpr right) : AsmExpr
    {
        public AsmExpr Left { get; } = left;
        public AsmExpr Right { get; } = right;

        public override string ToString()
        {
            return $"{Left}+{Right}";
        }

        public override bool Equals(object? obj)
        {
            return obj is AdditionExpr other &&
                   other.Left.Equals(Left) &&
                   other.Right.Equals(Right);
        }

        public override int GetHashCode()
        {
            return Left.GetHashCode() * 17 + Right.GetHashCode();
        }
    }
}