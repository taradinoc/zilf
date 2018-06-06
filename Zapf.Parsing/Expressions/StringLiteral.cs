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

using System.Text;
using JetBrains.Annotations;

namespace Zapf.Parsing.Expressions
{
    public sealed class StringLiteral : TextAsmExpr
    {
        public StringLiteral([NotNull] string text)
            : base(text) { }

        public override string ToString()
        {
            // escape '"' as '""'
            var sb = new StringBuilder(Text.Length + 2);
            sb.Append('"');

            foreach (var c in Text)
            {
                if (c == '"')
                    sb.Append('"');

                sb.Append(c);
            }

            sb.Append('"');
            return sb.ToString();
        }

        public override bool Equals(object obj)
        {
            return obj is StringLiteral other && other.Text == Text;
        }

        public override int GetHashCode()
        {
            return Text.GetHashCode();
        }
    }
}