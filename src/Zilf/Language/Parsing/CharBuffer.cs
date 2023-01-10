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
        readonly IEnumerator<char> source;
        readonly Stack<char> heldChars = new(2);
        char? curChar;

        public CharBuffer(IEnumerable<char> source)
        {
            this.source = source.GetEnumerator();
        }

        public bool MoveNext()
        {
            if (heldChars.Count > 0)
            {
                curChar = heldChars.Pop();
                return true;
            }

            if (source.MoveNext())
            {
                curChar = source.Current;
                return true;
            }

            curChar = null;
            return false;
        }

        /// <exception cref="InvalidOperationException" accessor="get">No character to read</exception>
        public char Current => curChar ?? throw new InvalidOperationException("No character to read");

        public void PushBack(char ch) => heldChars.Push(ch);
    }
}