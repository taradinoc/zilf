/* Copyright 2010-2017 Jesse McGrew
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
using System.Diagnostics.Contracts;
using System.Linq;
using System.Runtime.Serialization;
using System.Text;
using Zilf.Common;
using Zilf.Interpreter.Values;

namespace Zilf.Language
{
    sealed class CharBuffer
    {
        readonly IEnumerator<char> source;
        char? heldChar, curChar;

        public CharBuffer(IEnumerable<char> source)
        {
            this.source = source.GetEnumerator();
        }

        public bool MoveNext()
        {
            if (heldChar != null)
            {
                curChar = heldChar;
                heldChar = null;
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

        public char Current
        {
            get
            {
                if (curChar != null)
                    return (char)curChar;

                throw new InvalidOperationException("No character to read");
            }
        }

        public void PushBack(char ch)
        {
            if (heldChar != null)
                throw new InvalidOperationException("A character is already held");

            heldChar = ch;
        }
    }

    [Serializable]
    public abstract class ParserException : Exception
    {
        protected ParserException(string message)
            : base(message) { }

        protected ParserException(SerializationInfo info, StreamingContext context)
            : base(info, context) { }
    }

    [Serializable]
    sealed class ExpectedButFound : ParserException
    {
        public ExpectedButFound(string expected, string actual)
            : base($"expected {expected} but found {actual}") { }

        private ExpectedButFound(SerializationInfo info, StreamingContext context)
            : base(info, context) { }
    }

    [Serializable]
    sealed class ParsedNumberOverflowed : ParserException
    {
        public ParsedNumberOverflowed(string number, string radix = "decimal")
            : base($"{radix} number '{number}' cannot be represented in 32 bits") { }

        private ParsedNumberOverflowed(SerializationInfo info, StreamingContext context)
            : base(info, context) { }
    }

    interface IParserSite
    {
        ZilAtom ParseAtom(string text);
        ZilAtom GetTypeAtom(ZilObject zo);
        ZilObject ChangeType(ZilObject zo, ZilAtom type);
        ZilObject Evaluate(ZilObject zo);
        ZilObject GetGlobalVal(ZilAtom atom);

        string CurrentFilePath { get; }
        ZilObject FALSE { get; }
    }

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
    }

    struct ParserOutput
    {
        public ParserOutputType Type;
        public ZilObject Object;
        public ParserException Exception;

        public static readonly ParserOutput EndOfInput =
            new ParserOutput { Type = ParserOutputType.EndOfInput };

        public static readonly ParserOutput Terminator =
            new ParserOutput { Type = ParserOutputType.Terminator };

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

    sealed class Parser
    {
        readonly IParserSite site;
        readonly ISourceLine srcOverride;
        readonly ZilObject[] templateParams;
        readonly Queue<ZilObject> heldObjects = new Queue<ZilObject>();
        int line = 1;

        const char BANG_BACKSLASH = (char)('\\' + 128);
        const char BANG_LBRACKET = (char)('[' + 128);
        const char BANG_RBRACKET = (char)(']' + 128);
        const char BANG_LANGLE = (char)('<' + 128);
        const char BANG_RANGLE = (char)('>' + 128);
        const char BANG_LPAREN = (char)('(' + 128);
        const char BANG_RPAREN = (char)(')' + 128);
        const char BANG_LCURLY = (char)('{' + 128);
        const char BANG_RCURLY = (char)('}' + 128);
        const char BANG_DOT = (char)('.' + 128);
        const char BANG_COMMA = (char)(',' + 128);
        const char BANG_SQUOTE = (char)('\'' + 128);
        const char BANG_DQUOTE = (char)('"' + 128);

        static string Rebang(char ch)
        {
            if (ch >= 128 && ch < 256)
                return "!" + (char)(ch - 128);

            return ch.ToString();
        }

        public Parser(IParserSite site)
            : this(site, (ISourceLine)null, null)
        {
            Contract.Requires(site != null);
        }

        public Parser(IParserSite site, params ZilObject[] templateParams)
            : this(site, null, templateParams)
        {
            Contract.Requires(site != null);
        }

        public Parser(IParserSite site, ISourceLine srcOverride, params ZilObject[] templateParams)
        {
            Contract.Requires(site != null);

            this.site = site;
            this.srcOverride = srcOverride;
            this.templateParams = templateParams;
        }

        public int Line => line;

        public IEnumerable<ParserOutput> Parse(IEnumerable<char> chars)
        {
            return Parse(new CharBuffer(chars));
        }

        IEnumerable<ParserOutput> Parse(CharBuffer chars)
        {
            while (true)
            {
                var po = ParseOne(chars, out ISourceLine src);

                switch (po.Type)
                {
                    case ParserOutputType.SyntaxError:
                    case ParserOutputType.EndOfInput:
                        yield return po;
                        yield break;

                    case ParserOutputType.Terminator:
                        yield return ParserOutput.FromException(new ExpectedButFound("object", $"'{Rebang(chars.Current)}'"));
                        yield break;
                }

                po.Object.SourceLine = srcOverride ?? src;
                yield return po;
            }
        }

        ParserOutput ParseOne(CharBuffer chars, out ISourceLine sourceLine)
        {
            if (heldObjects.Count > 0)
            {
                sourceLine = SourceLines.Unknown;
                return ParserOutput.FromObject(heldObjects.Dequeue());
            }

            var po = ParseOneNonAdecl(chars, out sourceLine);

            switch (po.Type)
            {
                case ParserOutputType.Object:
                case ParserOutputType.Comment:
                    if (SkipWhitespace(chars))
                    {
                        var c = chars.Current;
                        
                        if (c == ':')
                        {
                            ParserOutput po2;
                            do
                            {
                                po2 = ParseOneNonAdecl(chars, out _);
                            } while (po2.Type == ParserOutputType.Comment);

                            switch (po2.Type)
                            {
                                case ParserOutputType.EndOfInput:
                                    throw new ExpectedButFound("object after ':'", "<EOF>");

                                case ParserOutputType.Object:
                                    var adecl = new ZilAdecl(po.Object, po2.Object);
                                    return po.Type == ParserOutputType.Comment
                                        ? ParserOutput.FromComment(adecl)
                                        : ParserOutput.FromObject(adecl);

                                case ParserOutputType.SyntaxError:
                                    return po2;

                                case ParserOutputType.Terminator:
                                    chars.MoveNext();
                                    throw new ExpectedButFound("object after ':'", $"'{Rebang(chars.Current)}'");

                                default:
                                    throw new UnhandledCaseException("object after ':'");
                            }
                        }

                        chars.PushBack(c);
                    }
                    break;
            }

            return po;
        }

        ParserOutput ParseOneNonAdecl(CharBuffer chars, out ISourceLine sourceLine)
        {
            try
            {
                // handle whitespace
                if (!SkipWhitespace(chars))
                {
                    sourceLine = new FileSourceLine(site.CurrentFilePath, line);
                    return ParserOutput.EndOfInput;
                }

                sourceLine = new FileSourceLine(site.CurrentFilePath, line);
                var c = chars.Current;

                // '!' adds 128 to the next character
                if (c == '!')
                {
                    if (!chars.MoveNext())
                        throw new ExpectedButFound("character after '!'", "<EOF>");

                    c = (char)(chars.Current + 128);
                }

                switch (c)
                {
                    case '(':
                        return ParserOutput.FromObject(
                            ParseCurrentStructure(
                                chars,
                                ')',
                                zos => new ZilList(zos)));

                    case '<':
                        return ParserOutput.FromObject(
                            ParseCurrentStructure(
                                chars,
                                '>',
                                zos => zos.Count == 0 ? (ZilObject)site.FALSE : new ZilForm(zos)));

                    case '[':
                        return ParserOutput.FromObject(
                            ParseCurrentStructure(
                                chars,
                                ']',
                                zos => new ZilVector(zos.ToArray())));

                    case BANG_LPAREN:
                        // !(foo!) is identical to (foo)
                        return ParserOutput.FromObject(
                            ParseCurrentStructure(
                                chars,
                                ')',
                                BANG_RPAREN,
                                zos => new ZilList(zos)));

                    case BANG_LANGLE:
                        // !<foo!> is a segment
                        return ParserOutput.FromObject(
                            ParseCurrentStructure(
                                chars,
                                '>',
                                BANG_RANGLE,
                                zos => new ZilSegment(new ZilForm(zos))));

                    case BANG_LBRACKET:
                        // ![foo!] is a uvector, but we alias it to vector
                        return ParserOutput.FromObject(
                            ParseCurrentStructure(
                                chars,
                                ']',
                                BANG_RBRACKET,
                                zos => new ZilVector(zos.ToArray())));

                    case BANG_DOT:
                    case BANG_COMMA:
                    case BANG_SQUOTE:
                        // !.X is equivalent to !<LVAL X>, and so on
                        chars.PushBack((char)(c - 128));
                        return ParsePrefixed(chars, c,
                            zo => ParserOutput.FromObject(new ZilSegment(zo)));

                    case '{':
                    case BANG_LCURLY:
                        return ParseCurrentStructure(
                            chars,
                            '}',
                            BANG_RCURLY,
                            zos =>
                            {
                                var zarr = zos.ToArray();
                                if (zarr.Length != 1)
                                {
                                    throw new ExpectedButFound("1 object inside '{}'", zarr.Length.ToString());
                                }
                                foreach (var zo in ZilObject.ExpandTemplateToken(zarr[0], templateParams))
                                {
                                    heldObjects.Enqueue(zo);
                                }
                                if (heldObjects.Count == 0)
                                {
                                    return ParserOutput.FromComment(zarr[0]);
                                }
                                return ParserOutput.FromObject(heldObjects.Dequeue());
                            });

                    case '.':
                        return ParsePrefixed(chars, c,
                            zo => ParserOutput.FromObject(
                                new ZilForm(new[] { site.ParseAtom("LVAL"), zo })));

                    case ',':
                        return ParsePrefixed(chars, c,
                            zo => ParserOutput.FromObject(
                                new ZilForm(new[] { site.ParseAtom("GVAL"), zo })));

                    case '\'':
                        return ParsePrefixed(chars, c,
                            zo => ParserOutput.FromObject(
                                new ZilForm(new[] { site.ParseAtom("QUOTE"), zo })));

                    case '%':
                        bool drop = false;
                        if (chars.MoveNext())
                        {
                            c = chars.Current;
                            if (c == '%')
                            {
                                drop = true;
                            }
                            else
                            {
                                chars.PushBack(c);
                            }
                        }

                        var po = ParsePrefixed(chars, c,
                            zo => ParserOutput.FromObject(site.Evaluate(zo)));

                        if (po.Type == ParserOutputType.Object)
                        {
                            if (drop)
                            {
                                po.Type = ParserOutputType.Comment;
                            }
                            else if (po.Object is ZilSplice splice)
                            {
                                foreach (var zo in splice)
                                    heldObjects.Enqueue(zo);

                                if (heldObjects.Count > 0)
                                    return ParserOutput.FromObject(heldObjects.Dequeue());

                                po.Type = ParserOutputType.Comment;
                            }
                        }

                        return po;

                    case '#':
                        return ParsePrefixed(
                            chars,
                            '#',
                            zo =>
                            {
                                if (zo is ZilFix fix && fix.Value == 2)
                                {
                                    if (SkipWhitespace(chars))
                                    {
                                        var sb = new StringBuilder();

                                        bool run = true;
                                        do
                                        {
                                            c = chars.Current;
                                            switch (c)
                                            {
                                                case '0':
                                                case '1':
                                                    sb.Append(c);
                                                    break;

                                                case ')':
                                                case ']':
                                                case '}':
                                                case '>':
                                                case BANG_RANGLE:
                                                case BANG_RBRACKET:
                                                case BANG_RCURLY:
                                                case BANG_RPAREN:
                                                    chars.PushBack(c);
                                                    run = false;
                                                    break;

                                                default:
                                                    throw new ExpectedButFound("binary number after '#2'", $"{sb}{c}");
                                            }
                                        } while (run && chars.MoveNext());

                                        try
                                        {
                                            return ParserOutput.FromObject(new ZilFix(Convert.ToInt32(sb.ToString(), 2)));
                                        }
                                        catch (OverflowException)
                                        {
                                            throw new ParsedNumberOverflowed(sb.ToString(), "binary");
                                        }
                                    }

                                    throw new ExpectedButFound("binary number after '#2'", "<EOF>");
                                }

                                if (zo is ZilAtom atom)
                                {
                                    return ParsePrefixed(
                                        chars,
                                        atom.Text,
                                        zo2 => ParserOutput.FromObject(site.ChangeType(zo2, atom)));
                                }

                                throw new ExpectedButFound("atom or '2' after '#'", site.GetTypeAtom(zo).ToString());
                            });

                    case ';':
                        return ParsePrefixed(chars, c, ParserOutput.FromComment);

                    case '"':
                        return ParserOutput.FromObject(ParseCurrentString(chars));

                    case '>':
                    case ')':
                    case '}':
                    case ']':
                    case BANG_RANGLE:
                    case BANG_RBRACKET:
                    case BANG_RPAREN:
                    case ':':
                        chars.PushBack(c);
                        return ParserOutput.Terminator;

                    case BANG_BACKSLASH:
                    case BANG_DQUOTE:
                        if (chars.MoveNext())
                        {
                            return ParserOutput.FromObject(new ZilChar(chars.Current));
                        }
                        throw new ExpectedButFound("character after '!\\'", "<EOF>");

                    default:
                        return ParserOutput.FromObject(ParseCurrentAtomOrNumber(chars));
                }
            }
            catch (ParserException ex)
            {
                sourceLine = new FileSourceLine(site.CurrentFilePath, line);
                return ParserOutput.FromException(ex);
            }
        }

        bool SkipWhitespace(CharBuffer chars)
        {
            while (true)
            {
                if (!chars.MoveNext())
                    return false;

                var c = chars.Current;

                switch (c)
                {
                    case ' ':
                    case '\t':
                    case '\r':
                    case '\f':
                        // ignore whitespace
                        continue;

                    case '\n':
                        // count line breaks
                        line++;
                        continue;

                    default:
                        return true;
                }
            }
        }

        ZilObject ParseCurrentAtomOrNumber(CharBuffer chars)
        {
            var sb = new StringBuilder();

            bool run = true, backslash = false;
            int digits = 0, octalDigits = 0;

            do
            {
                var c = chars.Current;

                switch (c)
                {
                    case ' ':
                    case '\f':
                    case '\n':
                    case '\r':
                    case '\t':
                    case '<':
                    case '>':
                    case '(':
                    case ')':
                    case '{':
                    case '}':
                    case '[':
                    case ']':
                    case ':':
                    case ';':
                    case '"':
                    case '\'':
                    case ',':
                    case '%':
                    case '#':
                    case BANG_COMMA:
                    case BANG_DQUOTE:
                    case BANG_LANGLE:
                    case BANG_LBRACKET:
                    case BANG_LCURLY:
                    case BANG_LPAREN:
                    case BANG_RANGLE:
                    case BANG_RBRACKET:
                    case BANG_RCURLY:
                    case BANG_RPAREN:
                    case BANG_SQUOTE:
                        // can't be part of an atom
                        chars.PushBack(c);
                        run = false;
                        break;

                    case '\\':
                        backslash = true;
                        if (chars.MoveNext())
                        {
                            c = chars.Current;

                            if (c == '\n')
                                line++;

                            sb.Append(c);
                        }
                        else
                        {
                            throw new ExpectedButFound("character after '\\'", "<EOF>");
                        }
                        break;

                    case '!':
                        // keep !-, otherwise drop the exclamation point
                        if (chars.MoveNext())
                        {
                            c = chars.Current;

                            if (c == '-')
                            {
                                sb.Append("!-");
                            }
                            else
                            {
                                chars.PushBack(c);
                            }
                        }
                        else
                        {
                            throw new ExpectedButFound("character after '!'", "<EOF>");
                        }
                        break;

                    default:
                        // can be part of an atom
                        sb.Append(c);
                        if (char.IsDigit(c))
                        {
                            digits++;

                            if (c < '8')
                                octalDigits++;
                        }
                        break;
                }
            } while (run && chars.MoveNext());

            // see what it is
            var length = sb.Length;

            if (length == 0)
            {
                // nothing? shouldn't happen...
                throw new UnhandledCaseException($"{nameof(ParseCurrentAtomOrNumber)} found nothing");
            }

            // try it as a FIX if there were no backslashes
            if (!backslash)
            {
                if (digits > 0 &&
                    (length == digits || (length == digits + 1 && (sb[0] == '-' || sb[0] == '+'))))
                {
                    // decimal
                    try
                    {
                        return new ZilFix(Convert.ToInt32(sb.ToString()));
                    }
                    catch (OverflowException)
                    {
                        throw new ParsedNumberOverflowed(sb.ToString());
                    }
                }

                if (length > 2 && octalDigits == length - 2 && sb[0] == '*' && sb[length - 1] == '*')
                {
                    // octal
                    sb.Remove(0, 1);
                    sb.Length = length - 2;
                    try
                    {
                        return new ZilFix(Convert.ToInt32(sb.ToString(), 8));
                    }
                    catch (OverflowException)
                    {
                        throw new ParsedNumberOverflowed(sb.ToString(), "octal");
                    }
                }
            }

            // must be an atom
            var atom = site.ParseAtom(sb.ToString());
            if (atom is ZilLink)
            {
                return site.GetGlobalVal(atom);
            }
            return atom;
        }

        ZilString ParseCurrentString(CharBuffer chars)
        {
            var sb = new StringBuilder();

            while (chars.MoveNext())
            {
                var c = chars.Current;

                switch (c)
                {
                    case '"':
                        return ZilString.FromString(sb.ToString());

                    case '\\':
                        if (chars.MoveNext())
                        {
                            c = chars.Current;

                            if (c == '\n')
                                line++;

                            sb.Append(c);
                        }
                        else
                        {
                            throw new ExpectedButFound("character after '\\'", "<EOF>");
                        }
                        break;

                    case '\n':
                        line++;
                        goto default;

                    default:
                        sb.Append(c);
                        break;
                }
            }

            throw new ExpectedButFound("'\"'", "<EOF>");
        }

        static string KetWanted(char ket1, char? ket2)
        {
            if (ket2 == null)
                return $"'{Rebang(ket1)}'";

            return $"'{Rebang(ket1)}' or '{Rebang((char)ket2)}'";
        }

        T ParseCurrentStructure<T>(CharBuffer chars, char ket, Func<IList<ZilObject>, T> build)
        {
            return ParseCurrentStructure(chars, ket, null, build);
        }

        T ParseCurrentStructure<T>(CharBuffer chars, char ket1, char? ket2, Func<IList<ZilObject>, T> build)
        {
            var items = new List<ZilObject>();

            while (true)
            {
                var po = ParseOne(chars, out ISourceLine src);

                switch (po.Type)
                {
                    case ParserOutputType.Comment:
                        // TODO: store comment somewhere? (set SourceLine if so)
                        break;

                    case ParserOutputType.EndOfInput:
                        throw new ExpectedButFound($"object or {KetWanted(ket1, ket2)}", "<EOF>");

                    case ParserOutputType.SyntaxError:
                        throw po.Exception;

                    case ParserOutputType.Object:
                        po.Object.SourceLine = src;
                        items.Add(po.Object);
                        break;

                    case ParserOutputType.Terminator:
                        chars.MoveNext();
                        var c = chars.Current;
                        if (c == ket1 || c == ket2)
                        {
                            var result = build(items);

                            if (result is ISettableSourceLine asSettableSource)
                                asSettableSource.SourceLine = src;

                            return result;
                        }
                        throw new ExpectedButFound($"object or {KetWanted(ket1, ket2)}", $"'{Rebang(c)}'");

                    default:
                        throw new UnhandledCaseException("parsed element type");
                }
            }
        }

        ParserOutput ParsePrefixed(CharBuffer chars, char prefix, Func<ZilObject, ParserOutput> convert)
        {
            return ParsePrefixed(chars, Rebang(prefix), convert);
        }

        ParserOutput ParsePrefixed(CharBuffer chars, string prefix, Func<ZilObject, ParserOutput> convert)
        {
            ParserOutput po;
            ISourceLine src;

            do
            {
                po = ParseOneNonAdecl(chars, out src);
            } while (po.Type == ParserOutputType.Comment);

            switch (po.Type)
            {
                case ParserOutputType.Object:
                    po.Object.SourceLine = src;
                    return convert(po.Object);

                case ParserOutputType.EndOfInput:
                    throw new ExpectedButFound($"object after '{prefix}'", "<EOF>");

                case ParserOutputType.Terminator:
                    chars.MoveNext();
                    throw new ExpectedButFound($"object after '{prefix}'", $"'{Rebang(chars.Current)}'");

                case ParserOutputType.SyntaxError:
                    return po;

                default:
                    throw new UnhandledCaseException("after prefix");
            }
        }
    }
}
