/* Copyright 2010-2018 Jesse McGrew
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
using System.Linq;
using System.Runtime.Serialization;
using System.Text;
using Zilf.Common;
using Zilf.Interpreter.Values;
using JetBrains.Annotations;

namespace Zilf.Language
{
    sealed class CharBuffer
    {
        [NotNull]
        readonly IEnumerator<char> source;
        [NotNull]
        readonly Stack<char> heldChars = new Stack<char>(2);
        char? curChar;

        public CharBuffer([NotNull] IEnumerable<char> source)
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

    [Serializable]
    public abstract class ParserException : Exception
    {
        protected ParserException(string message, Exception innerException)
            : base(message, innerException) { }

        protected ParserException([NotNull] SerializationInfo info, StreamingContext context)
            : base(info, context)
        {
        }
    }

    [Serializable]
    sealed class ExpectedButFound : ParserException
    {
        public ExpectedButFound(string expected, string actual, [CanBeNull] Exception innerException = null)
            : base($"expected {expected} but found {actual}", innerException) { }

        ExpectedButFound([NotNull] SerializationInfo info, StreamingContext context)
            : base(info, context)
        {
        }
    }

    [Serializable]
    sealed class ParsedNumberOverflowed : ParserException
    {
        const string DefaultRadix = "decimal";

        public ParsedNumberOverflowed(string number, string radix = DefaultRadix, [CanBeNull] Exception innerException = null)
            : base($"{radix} number '{number}' cannot be represented in 32 bits", innerException) { }

        public ParsedNumberOverflowed(string number, [CanBeNull] Exception innerException)
            : this(number, DefaultRadix, innerException) { }

        ParsedNumberOverflowed([NotNull] SerializationInfo info, StreamingContext context)
            : base(info, context)
        {
        }
    }
    interface IParserSite
    {
        [NotNull]
        ZilAtom ParseAtom([NotNull] string text);

        [NotNull]
        ZilAtom GetTypeAtom([NotNull] ZilObject zo);

        [NotNull]
        ZilObject ChangeType([NotNull] ZilObject zo, [NotNull] ZilAtom type);

        [NotNull]
        ZilObject Evaluate([NotNull] ZilObject zo);

        [CanBeNull]
        ZilObject GetGlobalVal([NotNull] ZilAtom atom);

        [NotNull]
        string CurrentFilePath { get; }

        [NotNull]
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
            new ParserOutput { Type = ParserOutputType.EmptySplice };

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

    // TODO: Non-evaluating parser mode, to return %MACROs, %%MACROs, and LINKs as-is, and keep inner comments
    sealed class Parser
    {
        readonly IParserSite site;
        readonly ISourceLine srcOverride;
        readonly ZilObject[] templateParams;
        readonly Queue<ZilObject> heldObjects = new Queue<ZilObject>();
        int line = 1;

        public Parser([NotNull] IParserSite site)
            : this(site, (ISourceLine)null, null)
        {
        }

        public Parser([NotNull] IParserSite site, params ZilObject[] templateParams)
            : this(site, null, templateParams)
        {
        }

        public Parser([NotNull] IParserSite site, ISourceLine srcOverride, params ZilObject[] templateParams)
        {

            this.site = site;
            this.srcOverride = srcOverride;
            this.templateParams = templateParams;
        }

        public int Line => line;

        [NotNull]
        public IEnumerable<ParserOutput> Parse([NotNull] IEnumerable<char> chars)
        {
            return Parse(new CharBuffer(chars));
        }

        [NotNull]
        IEnumerable<ParserOutput> Parse([NotNull] CharBuffer chars)
        {
            while (true)
            {
                var po = ParseOne(chars, out var src);

                switch (po.Type)
                {
                    case ParserOutputType.SyntaxError:
                    case ParserOutputType.EndOfInput:
                        yield return po;
                        yield break;

                    case ParserOutputType.Terminator:
                        yield return ParserOutput.FromException(new ExpectedButFound("object",
                            $"'{chars.Current.Rebang()}'"));
                        yield break;

                    default:
                        if (po.Object != null)
                            po.Object.SourceLine = srcOverride ?? src;

                        yield return po;
                        break;
                }
            }
        }

        ParserOutput ParseOne(CharBuffer chars, [NotNull] out ISourceLine sourceLine)
        {
            if (heldObjects.Count > 0)
            {
                sourceLine = SourceLines.Unknown;
                return ParserOutput.FromObject(heldObjects.Dequeue());
            }

            var po = ParseOneNonAdecl(chars, out sourceLine);

            if (po.Type != ParserOutputType.Object && po.Type != ParserOutputType.Comment)
            {
                System.Diagnostics.Debug.Assert(po.Object == null);
                return po;
            }

            System.Diagnostics.Debug.Assert(po.Object != null);

            if (!SkipWhitespace(chars))
                return po;

            var c = chars.Current;

            if (c != ':' && c != Bang.Colon)
            {
                chars.PushBack(c);
                return po;
            }

            ParserOutput po2;
            do
            {
                po2 = ParseOneNonAdecl(chars, out _);
                // TODO: store comment somewhere? (set SourceLine if so)
            } while (po2.IsIgnorable);

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
                    throw new ExpectedButFound("object after ':'", $"'{chars.Current.Rebang()}'");

                default:
                    throw new UnhandledCaseException("object after ':'");
            }
        }

        ParserOutput ParseOneNonAdecl(CharBuffer chars, [NotNull] out ISourceLine sourceLine)
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

                // '!' adds 128 to the next character (assuming it's below 128)
                if (c == '!')
                {
                    if (!chars.MoveNext())
                        throw new ExpectedButFound("character after '!'", "<EOF>");

                    c = chars.Current;

                    // two bangs in a row? preposterous.
                    if (c == '!')
                        throw new ExpectedButFound("character after '!'", "another '!'");

                    if (c < 128)
                        c += (char)128;
                }

                switch (c)
                {
                    case '(':
                        return ParserOutput.FromObject(
                            ParseCurrentStructure(
                                chars,
                                ')', Bang.RightParen,
                                zos => new ZilList(zos)));

                    case '<':
                        return ParserOutput.FromObject(
                            ParseCurrentStructure(
                                chars,
                                '>', Bang.RightAngle,
                                zos => zos.Count == 0 ? site.FALSE : new ZilForm(zos)));

                    case '[':
                        return ParserOutput.FromObject(
                            ParseCurrentStructure(
                                chars,
                                ']', Bang.RightBracket,
                                zos => new ZilVector(zos.ToArray())));

                    case Bang.LeftParen:
                        // !(foo!) is identical to (foo)
                        return ParserOutput.FromObject(
                            ParseCurrentStructure(
                                chars,
                                ')', Bang.RightParen,
                                zos => new ZilList(zos)));

                    case Bang.LeftAngle:
                        // !<foo!> is a segment
                        return ParserOutput.FromObject(
                            ParseCurrentStructure(
                                chars,
                                '>', Bang.RightAngle,
                                zos => new ZilSegment(new ZilForm(zos))));

                    case Bang.LeftBracket:
                        // ![foo!] is a uvector, but we alias it to vector
                        return ParserOutput.FromObject(
                            ParseCurrentStructure(
                                chars,
                                ']', Bang.RightBracket,
                                zos => new ZilVector(zos.ToArray())));

                    case Bang.Dot:
                    case Bang.Comma:
                    case Bang.SingleQuote:
                        // !.X is equivalent to !<LVAL X>, and so on
                        chars.PushBack((char)(c - 128));
                        return ParsePrefixed(chars, c,
                            zo => ParserOutput.FromObject(new ZilSegment(zo)));

                    case '{':
                    case Bang.LeftCurly:
                        return ParseCurrentStructure(
                            chars,
                            '}', Bang.RightCurly,
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
                                return heldObjects.Count == 0
                                    ? ParserOutput.EmptySplice
                                    : ParserOutput.FromObject(heldObjects.Dequeue());
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
                    case Bang.Percent:
                        bool drop = false;
                        if (chars.MoveNext())
                        {
                            c = chars.Current;
                            if (c == '%' || c == Bang.Percent)
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

                        if (po.Type != ParserOutputType.Object)
                            return po;

                        if (drop)
                            return ParserOutput.EmptySplice;

                        if (po.Object is ZilSplice splice)
                        {
                            foreach (var zo in splice)
                                heldObjects.Enqueue(zo);

                            return heldObjects.Count == 0
                                ? ParserOutput.EmptySplice
                                : ParserOutput.FromObject(heldObjects.Dequeue());
                        }

                        return po;

                    case '#':
                    case Bang.Hash:
                        return ParsePrefixed(
                            chars,
                            c,
                            zo =>
                            {
                                switch (zo)
                                {
                                    case ZilFix fix when fix.Value == 2:
                                        if (!SkipWhitespace(chars))
                                            throw new ExpectedButFound("binary number after '#2'", "<EOF>");

                                        var sb = new StringBuilder();

                                        bool run = true;
                                        do
                                        {
                                            var c2 = chars.Current;
                                            switch (c2)
                                            {
                                                case '0':
                                                case '1':
                                                    sb.Append(c2);
                                                    break;

                                                case var _ when c2.IsTerminator():
                                                    chars.PushBack(c2);
                                                    run = false;
                                                    break;

                                                default:
                                                    throw new ExpectedButFound("binary number after '#2'", $"{sb}{c2}");
                                            }
                                        } while (run && chars.MoveNext());

                                        try
                                        {
                                            return ParserOutput.FromObject(new ZilFix(Convert.ToInt32(sb.ToString(), 2)));
                                        }
                                        catch (OverflowException ex)
                                        {
                                            throw new ParsedNumberOverflowed(sb.ToString(), "binary", ex);
                                        }

                                    case ZilAtom atom:
                                        return ParsePrefixed(
                                            chars,
                                            atom.Text,
                                            zo2 => ParserOutput.FromObject(site.ChangeType(zo2, atom)));
                                }

                                throw new ExpectedButFound($"atom or '2' after '{c.Rebang()}'", site.GetTypeAtom(zo).ToString());
                            });

                    case ';':
                    case Bang.Semicolon:
                        return ParsePrefixed(chars, c, ParserOutput.FromComment);

                    case '"':
                        return ParserOutput.FromObject(ParseCurrentString(chars));

                    case var _ when c.IsTerminator():
                        chars.PushBack(c);
                        return ParserOutput.Terminator;

                    case Bang.Backslash:
                    case Bang.DoubleQuote:
                        if (chars.MoveNext())
                        {
                            return ParserOutput.FromObject(new ZilChar(chars.Current));
                        }
                        throw new ExpectedButFound("character after '!\\'", "<EOF>");

                    case var _ when c.IsNonAtomChar():
                        throw new ExpectedButFound("atom", $"'{c.Rebang()}'");

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

        bool SkipWhitespace([NotNull] CharBuffer chars)
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

                    case '!':
                        // skip bang whitespace
                        if (!chars.MoveNext())
                        {
                            throw new ExpectedButFound("character after '!'", "<EOF>");
                        }

                        c = chars.Current;
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
                                //chars.PushBack((char)(chars.Current | 128));
                                chars.PushBack('!');
                                chars.MoveNext();
                                chars.PushBack(c);
                                return true;
                        }

                    default:
                        return true;
                }
            }
        }

        [CanBeNull]
        ZilObject ParseCurrentAtomOrNumber([NotNull] CharBuffer chars)
        {
            var sb = new StringBuilder();

            bool run = true, backslash = false;
            int digits = 0, octalDigits = 0;

            do
            {
                var c = chars.Current;

                switch (c)
                {
                    case var _ when c.IsNonAtomChar():
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
                throw new UnhandledCaseException($"{nameof(ParseCurrentAtomOrNumber)} found nothing (current char: '{chars.Current}')");
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
                    catch (OverflowException ex)
                    {
                        throw new ParsedNumberOverflowed(sb.ToString(), ex);
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
                    catch (OverflowException ex)
                    {
                        throw new ParsedNumberOverflowed(sb.ToString(), "octal", ex);
                    }
                }
            }

            // must be an atom
            var atom = site.ParseAtom(sb.ToString());
            return atom is ZilLink ? site.GetGlobalVal(atom) : atom;
        }

        [NotNull]
        ZilString ParseCurrentString([NotNull] CharBuffer chars)
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

        [NotNull]
        static string KetWanted(char ket1, char? ket2)
        {
            return ket2 == null
                ? $"'{ket1.Rebang()}'"
                : $"'{ket1.Rebang()}' or '{((char)ket2).Rebang()}'";
        }

        [NotNull]
        T ParseCurrentStructure<T>([NotNull] CharBuffer chars, char ket1, char? ket2, [NotNull] Func<IList<ZilObject>, T> build)
        {
            var items = new List<ZilObject>();

            while (true)
            {
                var po = ParseOne(chars, out var src);

                switch (po.Type)
                {
                    case ParserOutputType.Comment:
                        // TODO: store comment somewhere? (set SourceLine if so)
                        break;

                    case ParserOutputType.EmptySplice:
                        // skip
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
                        if (c != ket1 && c != ket2)
                            throw new ExpectedButFound($"object or {KetWanted(ket1, ket2)}", $"'{c.Rebang()}'");

                        var result = build(items);

                        if (result is ISettableSourceLine asSettableSource)
                            asSettableSource.SourceLine = src;

                        return result;

                    default:
                        throw new UnhandledCaseException("parsed element type");
                }
            }
        }

        ParserOutput ParsePrefixed(CharBuffer chars, char prefix, Func<ZilObject, ParserOutput> convert)
        {
            return ParsePrefixed(chars, prefix.Rebang(), convert);
        }

        ParserOutput ParsePrefixed(CharBuffer chars, string prefix, Func<ZilObject, ParserOutput> convert)
        {
            ParserOutput po;
            ISourceLine src;

            do
            {
                po = ParseOneNonAdecl(chars, out src);
                // TODO: store comment somewhere?
            } while (po.IsIgnorable);

            switch (po.Type)
            {
                case ParserOutputType.Object:
                    po.Object.SourceLine = src;
                    return convert(po.Object);

                case ParserOutputType.EndOfInput:
                    throw new ExpectedButFound($"object after '{prefix}'", "<EOF>");

                case ParserOutputType.Terminator:
                    chars.MoveNext();
                    throw new ExpectedButFound($"object after '{prefix}'", $"'{chars.Current.Rebang()}'");

                case ParserOutputType.SyntaxError:
                    return po;

                default:
                    throw new UnhandledCaseException("after prefix");
            }
        }
    }

    static class CharExtensions
    {
        public static bool IsTerminator(this char c)
        {
            switch (c & ~128)
            {
                case ')':
                case ']':
                case '}':
                case '>':
                case ':':
                    return true;

                default:
                    return false;
            }
        }

        public static bool IsNonAtomChar(this char c)
        {
            switch (c & ~128)
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
                    return true;

                default:
                    return false;
            }
        }

        [NotNull]
        public static string Rebang(this char ch)
        {
            if (ch >= 128 && ch < 256)
                return "!" + (char)(ch - 128);

            return ch.ToString();
        }
    }
}
