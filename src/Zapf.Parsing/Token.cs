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
using Zapf.Parsing.Diagnostics;

namespace Zapf.Parsing
{
    struct Token : ISourceLine
    {
        public TokenType Type;
        public string Text;
        public int Line;
        public string Filename;

        string ISourceLine.SourceFile => Filename;
        int ISourceLine.LineNum => Line;

        public override string ToString()
        {
            var sb = new StringBuilder();
            sb.Append('{');
            sb.AppendFormat("Type={0}", Type);
            switch (Type)
            {
                case TokenType.Number:
                case TokenType.Symbol:
                case TokenType.String:
                    sb.AppendFormat(", Text=\"{0}\"", Text);
                    break;
            }
            sb.Append('}');
            return sb.ToString();
        }
    }
}