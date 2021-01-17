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