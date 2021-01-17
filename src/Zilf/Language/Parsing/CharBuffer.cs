using System;
using System.Collections.Generic;

namespace Zilf.Language.Parsing
{
    sealed class CharBuffer
    {
        readonly IEnumerator<char> source;
        readonly Stack<char> heldChars = new Stack<char>(2);
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