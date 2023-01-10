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
using System.Runtime.Serialization;

namespace Zilf.Language.Parsing
{
    [Serializable]
    public abstract class ParserException : Exception
    {
        protected ParserException(string message, Exception? innerException)
            : base(message, innerException) { }

        protected ParserException(SerializationInfo info, StreamingContext context)
            : base(info, context)
        {
        }

        public ParserException()
        {
        }

        public ParserException(string message) : base(message)
        {
        }
    }

    [Serializable]
    sealed class ParsedNumberOverflowed : ParserException
    {
        const string DefaultRadix = "decimal";

        public ParsedNumberOverflowed(string number, string radix = DefaultRadix, Exception? innerException = null)
            : base($"{radix} number '{number}' cannot be represented in 32 bits", innerException) { }

        public ParsedNumberOverflowed(string number, Exception? innerException)
            : this(number, DefaultRadix, innerException) { }

        ParsedNumberOverflowed(SerializationInfo info, StreamingContext context)
            : base(info, context)
        {
        }

        public ParsedNumberOverflowed()
        {
        }

        public ParsedNumberOverflowed(string message) : base(message)
        {
        }
    }

    [Serializable]
    sealed class ExpectedButFound : ParserException
    {
        public ExpectedButFound(string expected, string actual, Exception? innerException = null)
            : base($"expected {expected} but found {actual}", innerException) { }

        ExpectedButFound(SerializationInfo info, StreamingContext context)
            : base(info, context)
        {
        }

        public ExpectedButFound()
        {
        }

        public ExpectedButFound(string message) : base(message)
        {
        }

        public ExpectedButFound(string message, Exception innerException) : base(message, innerException)
        {
        }
    }
}