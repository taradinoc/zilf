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
using System.Collections.Generic;

namespace Zilf.Language.Parsing
{
    sealed class CharBuffer
    {
        readonly record struct PositionedChar(char Value, int Line, int Column);

        readonly IEnumerator<char> source;
        readonly Stack<PositionedChar> heldChars = new(2);
        readonly Stack<PositionedChar> history = new(8);
        char? curChar;
        int curLine;
        int curColumn;

        int nextLine = 1;
        int nextColumn = 1;

        public CharBuffer(IEnumerable<char> source)
        {
            this.source = source.GetEnumerator();
        }

        public int Line => curLine;

        public int Column => curColumn;

        public bool MoveNext()
        {
            if (heldChars.Count > 0)
            {
                var pc = heldChars.Pop();
                curChar = pc.Value;
                curLine = pc.Line;
                curColumn = pc.Column;
                history.Push(pc);
                return true;
            }

            if (source.MoveNext())
            {
                var ch = source.Current;
                curChar = ch;
                curLine = nextLine;
                curColumn = nextColumn;
                history.Push(new PositionedChar(ch, curLine, curColumn));

                if (ch == '\n')
                {
                    nextLine++;
                    nextColumn = 1;
                }
                else
                {
                    nextColumn++;
                }
                return true;
            }

            curChar = null;
            return false;
        }

        /// <exception cref="InvalidOperationException" accessor="get">No character to read</exception>
        public char Current => curChar ?? throw new InvalidOperationException("No character to read");

        public void PushBack(char ch)
        {
            // Most callers push back the current character; preserve the last-known position.
            // If there is no current character yet, approximate using the next position.
            var line = curChar != null ? curLine : nextLine;
            var column = curChar != null ? curColumn : nextColumn;
            heldChars.Push(new PositionedChar(ch, line, column));
        }

        /// <summary>
        /// Pushes the current character back so that the next <see cref="MoveNext"/> returns it,
        /// and restores the previous character (if any) as <see cref="Current"/>.
        /// </summary>
        public bool UnreadCurrent()
        {
            if (curChar == null || history.Count == 0)
                return false;

            var current = history.Pop();
            heldChars.Push(current);

            if (history.Count > 0)
            {
                var previous = history.Peek();
                curChar = previous.Value;
                curLine = previous.Line;
                curColumn = previous.Column;
            }
            else
            {
                curChar = null;
                curLine = 0;
                curColumn = 0;
            }

            return true;
        }
    }
}