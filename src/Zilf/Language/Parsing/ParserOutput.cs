using System.Text;
using Zilf.Interpreter.Values;

namespace Zilf.Language.Parsing
{
    enum ParserOutputType
    {
        /// <summary>
        /// A valid <see cref="ZilObject"/> was parsed.
        /// </summary>
        Object,
        /// <summary>
        /// A valid <see cref="ZilObject"/> was parsed, with a comment prefix.
        /// </summary>
        Comment,
        /// <summary>
        /// A valid object could not be parsed.
        /// </summary>
        SyntaxError,
        /// <summary>
        /// There are no more characters to read.
        /// </summary>
        EndOfInput,
        /// <summary>
        /// A character was read (and pushed back) that may have terminated an outer structure.
        /// </summary>
        Terminator,
        /// <summary>
        /// A special object was parsed and evaluated, and there were no objects to insert in its place.
        /// </summary>
        /// <remarks>
        /// This happens whenever a %%macro is evaluated, or when a %macro returns #SPLICE (), or when
        /// the left side of a {...:SPLICE} template invocation evaluates to an empty structure.
        /// </remarks>
        EmptySplice,
    }

    struct ParserOutput
    {
        public ParserOutputType Type;
        public ZilObject Object;
        public ParserException Exception;

        public bool IsIgnorable => Type == ParserOutputType.Comment || Type == ParserOutputType.EmptySplice;

        public override string ToString()
        {
            var sb = new StringBuilder();

            sb.Append(Type);

            if (Object != null)
            {
                sb.Append(' ');
                sb.Append(Object);
            }

            if (Exception != null)
            {
                sb.Append(' ');
                sb.Append(Exception.GetType().Name);
                sb.Append("(\"");
                sb.Append(Exception.Message);
                sb.Append("\")");
            }

            return sb.ToString();
        }

        public static readonly ParserOutput EmptySplice =
            new()
            { Type = ParserOutputType.EmptySplice };

        public static readonly ParserOutput EndOfInput =
            new()
            { Type = ParserOutputType.EndOfInput };

        public static readonly ParserOutput Terminator =
            new()
            { Type = ParserOutputType.Terminator };

        public static ParserOutput FromObject(ZilObject zo)
        {
            return new ParserOutput
            {
                Type = ParserOutputType.Object,
                Object = zo
            };
        }

        public static ParserOutput FromComment(ZilObject zo)
        {
            return new ParserOutput
            {
                Type = ParserOutputType.Comment,
                Object = zo
            };
        }

        public static ParserOutput FromException(ParserException ex)
        {
            return new ParserOutput
            {
                Type = ParserOutputType.SyntaxError,
                Exception = ex
            };
        }
    }
}