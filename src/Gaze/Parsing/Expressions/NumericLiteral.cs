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

using System;

namespace Gaze.Parsing.Expressions
{
    public sealed class NumericLiteral : TextAsmExpr
    {
        public NumericLiteral(string text)
            : base(text)
        {
            // Support hex numbers with $ prefix
            if (text.StartsWith("$"))
            {
                Value = Convert.ToInt32(text.Substring(1), 16);
            }
            else
            {
                Value = int.Parse(text);
            }
        }

        public NumericLiteral(int value)
            : base(value.ToString())
        {
            Value = value;
        }

        public int Value { get; }

        public override string ToString()
        {
            return Value.ToString();
        }

        public override bool Equals(object? obj)
        {
            return obj is NumericLiteral other && other.Value == Value;
        }

        public override int GetHashCode()
        {
            return Value.GetHashCode();
        }
    }
}