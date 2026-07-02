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

        public readonly bool IsIgnorable => Type == ParserOutputType.Comment || Type == ParserOutputType.EmptySplice;

        public override readonly string ToString()
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