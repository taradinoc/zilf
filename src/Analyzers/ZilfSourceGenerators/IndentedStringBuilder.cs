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

using System;
using System.Linq;
using System.Text;

namespace ZilfSourceGenerators
{
    /// <summary>
    /// A StringBuilder wrapper that automatically handles indentation.
    /// </summary>
    /// <example>
    /// sb.Indent();
    /// sb.Append("if (");
    /// sb.Append(condition);
    /// sb.AppendLine(")");
    /// sb.AppendLine("{");
    /// sb.Indent();
    /// sb.AppendLine("return true;");
    /// sb.Unindent();
    /// sb.AppendLine("}");
    /// sb.Unindent();
    ///
    /// /* This produces:
    ///        if (condition)
    ///        {
    ///            return true;
    ///        }
    /// */
    /// </example>
    public class IndentedStringBuilder(int indentSize = 4, string indentChar = " ")
    {
        private readonly StringBuilder _sb = new();
        private int _indentLevel = 0;
        private readonly string _indentString = new(indentChar[0], indentSize);
        private bool _atLineStart = true;

        public void Indent(int levels = 1)
        {
            _indentLevel += levels;
        }

        public void Unindent(int levels = 1)
        {
            _indentLevel = Math.Max(0, _indentLevel - levels);
        }

        public void AppendLine(string line = "")
        {
            if (string.IsNullOrEmpty(line))
            {
                _sb.AppendLine();
            }
            else
            {
                // If we're at the start of a line, add indentation
                if (_atLineStart && _indentLevel > 0)
                {
                    // Environment.NewLine is banned for analyzers, so assume CRLF
                    _sb.EnsureCapacity(_sb.Length + _indentLevel * _indentString.Length + line.Length + 2);

                    for (int i = 0; i < _indentLevel; i++)
                    {
                        _sb.Append(_indentString);
                    }
                }
                _sb.AppendLine(line);
            }
            _atLineStart = true; // After AppendLine, we're at the start of a new line
        }

        public void Append(string text)
        {
            // If we're at the start of a line, add indentation
            if (_atLineStart && _indentLevel > 0)
            {
                var indent = string.Concat(Enumerable.Repeat(_indentString, _indentLevel));
                _sb.Append(indent);
            }
            _sb.Append(text);
            _atLineStart = false; // After Append, we're no longer at the start of a line
        }

        public override string ToString()
        {
            return _sb.ToString();
        }
    }
}