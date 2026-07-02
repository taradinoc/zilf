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
using Zilf.Diagnostics;

namespace Zilf.Language.Parsing
{
    public abstract class ParserException : Exception
    {
        protected ParserException(string message, Exception? innerException)
            : base(message, innerException) { }

        protected ParserException()
        {
        }

        protected ParserException(string message) : base(message)
        {
        }
    }

    sealed class ParsedNumberOverflowed : ParserException
    {
        const string DefaultRadix = "decimal";

        public ParsedNumberOverflowed(string number, string radix = DefaultRadix, Exception? innerException = null)
            : base($"{radix} number '{number}' cannot be represented in 32 bits", innerException) { }

        public ParsedNumberOverflowed(string number, Exception? innerException)
            : this(number, DefaultRadix, innerException) { }

        public ParsedNumberOverflowed()
        {
        }

        public ParsedNumberOverflowed(string message) : base(message)
        {
        }
    }

    class ExpectedButFound : ParserException
    {
        public ExpectedButFound(string expected, string actual)
            : base($"expected {expected} but found {actual}") { }

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

    sealed class MismatchedTerminator(string expected, string actual) : ExpectedButFound(expected, actual)
    {
        // Explicitly store it in a field to avoid CS9107
        private readonly string actual = actual;

        public InterpreterError ToWarning(ISourceLine src)
        {
            return new InterpreterError(
                src,
                InterpreterMessages.Ignoring_Mismatched_Terminator_0,
                actual);
        }
    }
}