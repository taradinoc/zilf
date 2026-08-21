/* Copyright 2010-2025 Tara McGrew
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

namespace Zilf.Emit.Glulx
{
    class SumOperand(IConstantOperand left, IConstantOperand right) : IConstantOperand, IMemoryAddressOperand
    {
        public IConstantOperand Left => left;

        public IConstantOperand Right => right;

        public override string ToString()
        {
            return $"({left} + {right})";
        }

        public IConstantOperand Add(IConstantOperand other)
        {
            return new SumOperand(this, other);
        }

        public bool TryGetMemoryAddress(out object allocation, out int offset)
        {
            if (left is IMemoryAddressOperand address && right is INumericOperand numeric &&
                address.TryGetMemoryAddress(out allocation, out offset))
            {
                return TryAddOffset(numeric.Value, ref offset);
            }
            if (right is IMemoryAddressOperand reverseAddress && left is INumericOperand reverseNumeric &&
                reverseAddress.TryGetMemoryAddress(out allocation, out offset))
            {
                return TryAddOffset(reverseNumeric.Value, ref offset);
            }
            allocation = null!;
            offset = 0;
            return false;
        }

        private static bool TryAddOffset(int value, ref int offset)
        {
            var result = (long)offset + value;
            if (result is < int.MinValue or > int.MaxValue)
                return false;
            offset = (int)result;
            return true;
        }
    }
}
