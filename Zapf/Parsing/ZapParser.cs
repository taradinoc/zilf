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
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace Zapf.Parsing
{
    enum TokenType
    {
        Equals,
        Comma,
        Slash,
        Backslash,
        RAngle,
        Plus,
        Apostrophe,
        Colon,
        DColon,

        Number,
        Symbol,
        String,

        EndOfLine,
        EndOfFile,
    }

    struct Token : ISourceLine
    {
        public TokenType Type;
        public string Text;
        public int Line;
        public string Filename;

        string ISourceLine.SourceFile => Filename;
        int ISourceLine.LineNum => Line;

        public override string ToString()
        {
            var sb = new StringBuilder();
            sb.Append('{');
            sb.AppendFormat("Type={0}", Type);
            switch (Type)
            {
                case TokenType.Number:
                case TokenType.Symbol:
                case TokenType.String:
                    sb.AppendFormat(", Text=\"{0}\"", Text);
                    break;
            }
            sb.Append('}');
            return sb.ToString();
        }
    }

    class Tokenizer : IDisposable
    {
        static readonly Dictionary<char, TokenType> CharTokens = new Dictionary<char, TokenType>
        {
            { '=', TokenType.Equals },
            { ',', TokenType.Comma },
            { '/', TokenType.Slash },
            { '\\', TokenType.Backslash },
            { '>', TokenType.RAngle },
            { '+', TokenType.Plus },
            { '\'', TokenType.Apostrophe },
        };

        StreamReader rdr;
        readonly string filename;
        int line = 1;
        Token? heldToken;
        char? heldChar;

        struct BasicSourceLine : ISourceLine
        {
            public BasicSourceLine(int lineNum, string sourceFile)
            {
                this.LineNum = lineNum;
                this.SourceFile = sourceFile;
            }

            public int LineNum { get; }
            public string SourceFile { get; }
        }

        ISourceLine CurrentSourceLine => new BasicSourceLine(line, filename);

        public Tokenizer(Stream stream, string filename)
        {
            Contract.Requires(stream != null);
            Contract.Requires(filename != null);

            this.rdr = new StreamReader(stream);
            this.filename = filename;
        }

        char? PeekChar()
        {
            if (heldChar == null)
                heldChar = NextChar();

            return heldChar;
        }

        char? NextChar()
        {
            if (heldChar != null)
            {
                var temp = heldChar;
                heldChar = null;
                return temp;
            }

            var c = rdr.Read();
            return c < 0 ? null : (char?)c;
        }

        public Token PeekToken()
        {
            if (heldToken == null)
                heldToken = NextToken();

            return (Token)heldToken;
        }

        public Token NextToken()
        {
            if (heldToken != null)
            {
                var temp = (Token)heldToken;
                heldToken = null;
                return temp;
            }

            Token result = new Token
            {
                Filename = this.filename,
                Line = this.line
            };

            var c = PeekChar();

            while (c != null && char.IsWhiteSpace((char)c))
            {
                NextChar();

                if (c == '\n')
                {
                    line++;
                    result.Type = TokenType.EndOfLine;
                    return result;
                }

                c = PeekChar();
            }

            switch (c)
            {
                case null:
                    result.Type = TokenType.EndOfFile;
                    break;

                case ':':
                    NextChar();
                    if (PeekChar() == ':')
                    {
                        NextChar();
                        result.Type = TokenType.DColon;
                    }
                    else
                    {
                        result.Type = TokenType.Colon;
                    }
                    break;

                case '"':
                    ReadString(ref result);
                    break;

                case ';':
                    // disard comment
                    do
                    {
                        c = NextChar();
                    } while (c != null && c != '\n');
                    line++;
                    result.Type = (c == null) ? TokenType.EndOfFile : TokenType.EndOfLine;
                    break;

                default:
                    TokenType type;
                    if (CharTokens.TryGetValue((char)c, out type))
                    {
                        NextChar();
                        result.Type = type;
                    }
                    else if (CanStartSymbol((char)c))
                    {
                        ReadSymbolOrNum(ref result);
                    }
                    else
                    {
                        throw Errors.MakeSerious(CurrentSourceLine, "unexpected character '{0}'", c);
                    }
                    break;
            }

            return result;
        }

        void ReadSymbolOrNum(ref Token result)
        {
            var sb = new StringBuilder();
            char? c;
            int digits = 0;

            while ((c = PeekChar()) != null && CanContinueSymbol((char)c))
            {
                if (char.IsDigit((char)c))
                    digits++;

                sb.Append(NextChar());
            }

            var length = sb.Length;
            Contract.Assert(length > 0, "zero length symbol");

            result.Text = sb.ToString();

            if (length == digits || (length == digits + 1 && sb[0] == '-'))
            {
                // numeric
                result.Type = TokenType.Number;
            }
            else
            {
                result.Type = TokenType.Symbol;
            }
        }

        void ReadString(ref Token result)
        {
            var sb = new StringBuilder();

            // skip initial quote
            NextChar();

            while (true)
            {
                char? c = NextChar();

                switch (c)
                {
                    case null:
                        throw Errors.MakeSerious(CurrentSourceLine, "unterminated string");

                    case '"':
                        if (PeekChar() == '"')
                        {
                            // escape sequence
                            sb.Append('"');
                            NextChar();
                            break;
                        }

                        result.Type = TokenType.String;
                        result.Text = sb.ToString();
                        return;

                    case '\r':
                        // ignore
                        continue;

                    case '\n':
                        line++;
                        goto default;

                    default:
                        sb.Append((char)c);
                        break;
                }
            }
        }

        static bool CanStartSymbol(char c)
        {
            switch (c)
            {
                case '-':
                case '?':
                case '$':
                case '#':
                case '&':
                case '.':
                    return true;

                default:
                    return char.IsLetterOrDigit(c);
            }
        }

        static bool CanContinueSymbol(char c)
        {
            if (c == '\'')
                return true;

            return CanStartSymbol(c);
        }

        public void Dispose()
        {
            try
            {
                if (rdr != null)
                    rdr.Dispose();
            }
            finally
            {
                rdr = null;
            }
        }
    }

    class ZapParser : IDisposable
    {
        readonly IErrorSink sink;
        readonly IDictionary<string, KeyValuePair<ushort, ZOpAttribute>> opcodeDict;
        Tokenizer toks;
        int errorCount;

        public ZapParser(IErrorSink sink, IDictionary<string, KeyValuePair<ushort, ZOpAttribute>> opcodeDict)
        {
            Contract.Requires(opcodeDict != null);

            this.sink = sink;
            this.opcodeDict = opcodeDict;

            directiveDict = new Dictionary<string, DirectiveParseHandler>
            {
                { ".BYTE", ParseByteDirective },
                { ".CHRSET", ParseChrsetDirective },
                { ".END", ParseEndDirective },
                { ".ENDI", ParseEndiDirective },
                { ".ENDT", ParseEndtDirective },
                { ".FSTR", ParseFstrDirective },
                { ".FUNCT", ParseFunctDirective },
                { ".GSTR", ParseGstrDirective },
                { ".GVAR", ParseGvarDirective },
                { ".INSERT", ParseInsertDirective },
                { ".LANG", ParseLangDirective },
                { ".LEN", ParseLenDirective },
                { ".NEW", ParseNewDirective },
                { ".OBJECT", ParseObjectDirective },
                { ".PROP", ParsePropDirective },
                { ".SOUND", ParseSoundDirective },
                { ".STR", ParseStrDirective },
                { ".STRL", ParseStrlDirective },
                { ".TABLE", ParseTableDirective },
                { ".TIME", ParseTimeDirective },
                { ".VOCBEG", ParseVocbegDirective },
                { ".VOCEND", ParseVocendDirective },
                { ".WORD", ParseWordDirective },
                { ".ZWORD", ParseZwordDirective },

                { ".DEBUG-ACTION", ParseDebugActionDirective },
                { ".DEBUG-ARRAY", ParseDebugArrayDirective },
                { ".DEBUG-ATTR", ParseDebugAttrDirective },
                { ".DEBUG-FILE", ParseDebugFileDirective },
                { ".DEBUG-GLOBAL", ParseDebugGlobalDirective },
                { ".DEBUG-LINE", ParseDebugLineDirective },
                { ".DEBUG-OBJECT", ParseDebugObjectDirective },
                { ".DEBUG-PROP", ParseDebugPropDirective },
                { ".DEBUG-ROUTINE", ParseDebugRoutineDirective },
                { ".DEBUG-ROUTINE-END", ParseDebugRoutineEndDirective },

                // TODO: .TRUE and .FALSE?
            };
        }

        public void Dispose()
        {
            try
            {
                if (toks != null)
                    toks.Dispose();
            }
            finally
            {
                toks = null;
            }
        }

        public ParseResult Parse(Stream stream, string filename)
        {
            Contract.Requires(stream != null);
            Contract.Requires(filename != null);

            toks = new Tokenizer(stream, filename);
            var output = new List<AsmLine>();

            bool run = true;
            while (run)
            {
                var t = toks.NextToken();

                switch (t.Type)
                {
                    case TokenType.EndOfLine:
                        continue;

                    case TokenType.EndOfFile:
                        run = false;
                        continue;
                }

                var label = TryParseLabel(t);

                if (label != null)
                {
                    label.LineNum = t.Line;
                    label.SourceFile = t.Filename;
                    output.Add(label);

                    t = toks.NextToken();

                    switch (t.Type)
                    {
                        case TokenType.EndOfLine:
                        case TokenType.EndOfFile:
                            continue;
                    }
                }

                var parsed = TryParseInstruction(t);

                if (parsed == null)
                {
                    parsed = TryParseDirective(t);

                    if (parsed == null)
                    {
                        ReportErrorAndSkipLine(t, "unexpected token: {0}", t);
                        continue;
                    }
                }

                parsed.LineNum = t.Line;
                parsed.SourceFile = t.Filename;
                output.Add(parsed);
            }

            return new ParseResult
            {
                Lines = output,
                NumberOfSyntaxErrors = errorCount,
            };
        }

        void ReportError(ISourceLine node, string message)
        {
            errorCount++;
            Errors.Serious(sink, node, message);
        }

        void ReportError(ISourceLine node, string format, params object[] args)
        {
            errorCount++;
            Errors.Serious(sink, node, format, args);
        }

        void ReportErrorAndSkipLine(ISourceLine node, string message)
        {
            ReportError(node, message);
            SkipLine();
        }

        void ReportErrorAndSkipLine(ISourceLine node, string format, params object[] args)
        {
            ReportError(node, format, args);
            SkipLine();
        }

        void ReportErrorAndSkipExpr(ISourceLine node, string message)
        {
            ReportError(node, message);
            SkipExpr();
        }

        void ReportErrorAndSkipExpr(ISourceLine node, string format, params object[] args)
        {
            ReportError(node, format, args);
            SkipExpr();
        }

        void SkipLine()
        {
            while (true)
            {
                switch (toks.PeekToken().Type)
                {
                    case TokenType.EndOfFile:
                        return;

                    case TokenType.EndOfLine:
                        toks.NextToken();
                        return;

                    default:
                        toks.NextToken();
                        break;
                }
            }
        }

        void SkipExpr()
        {
            while (true)
            {
                switch (toks.PeekToken().Type)
                {
                    case TokenType.EndOfFile:
                    case TokenType.Slash:
                    case TokenType.Backslash:
                    case TokenType.RAngle:
                        return;

                    case TokenType.EndOfLine:
                    case TokenType.Comma:
                        toks.NextToken();
                        return;

                    default:
                        toks.NextToken();
                        break;
                }
            }
        }

        AsmLine TryParseInstruction(Token head)
        {
            if (head.Type == TokenType.Symbol && opcodeDict.ContainsKey(head.Text))
            {
                var result = new Instruction(head.Text);

                // parse operands
                while (true)
                {
                    Token t;
                    var type = toks.PeekToken().Type;

                    switch (type)
                    {
                        case TokenType.EndOfLine:
                            toks.NextToken();
                            return result;

                        case TokenType.EndOfFile:
                            return result;

                        case TokenType.Slash:
                        case TokenType.Backslash:
                            // branch target
                            var polarity = toks.NextToken().Type == TokenType.Slash;
                            t = toks.NextToken();
                            if (t.Type != TokenType.Symbol)
                            {
                                ReportErrorAndSkipLine(
                                    t,
                                    "expected label or 'TRUE' or 'FALSE' after '{0}'",
                                    polarity ? '/' : '\\');
                            }
                            else if (result.BranchPolarity != null)
                            {
                                ReportErrorAndSkipLine(t, "multiple branch targets");
                            }
                            else
                            {
                                result.BranchPolarity = polarity;
                                result.BranchTarget = t.Text;
                            }
                            break;

                        case TokenType.RAngle:
                            // store target
                            toks.NextToken();
                            t = toks.NextToken();
                            if (t.Type != TokenType.Symbol)
                            {
                                ReportErrorAndSkipLine(t, "expected variable or 'STACK' after '>'");
                            }
                            else if (result.StoreTarget != null)
                            {
                                ReportErrorAndSkipLine(t, "multiple store targets");
                            }
                            else
                            {
                                result.StoreTarget = t.Text;
                            }
                            break;

                        default:
                            if (CanStartExpr(type))
                            {
                                // regular operand
                                result.Operands.Add(ParseExpr());
                                switch (toks.PeekToken().Type)
                                {
                                    case TokenType.Comma:
                                        toks.NextToken();
                                        break;

                                    case TokenType.Slash:
                                    case TokenType.Backslash:
                                    case TokenType.RAngle:
                                    case TokenType.EndOfLine:
                                    case TokenType.EndOfFile:
                                        break;

                                    default:
                                        ReportErrorAndSkipLine(toks.PeekToken(), "expected ',' or target or EOL after operand");
                                        break;
                                }
                                break;
                            }

                            ReportErrorAndSkipLine(toks.PeekToken(), "unexpected token: {0}", toks.PeekToken());
                            break;
                    }
                }
            }

            return null;
        }

        AsmLine TryParseLabel(Token head)
        {
            if (head.Type == TokenType.Symbol)
            {
                switch (toks.PeekToken().Type)
                {
                    case TokenType.Colon:
                        toks.NextToken();
                        return new LocalLabel(head.Text);

                    case TokenType.DColon:
                        toks.NextToken();
                        return new GlobalLabel(head.Text);
                }
            }

            return null;
        }

        AsmExpr ParseExprOne()
        {
            return ParseExprOne(toks.NextToken());
        }

        AsmExpr ParseExprOne(Token head)
        {
            switch (head.Type)
            {
                case TokenType.Symbol:
                    return new SymbolExpr(head.Text);

                case TokenType.Number:
                    return new NumericLiteral(head.Text);

                case TokenType.String:
                    return new StringLiteral(head.Text);

                case TokenType.Apostrophe:
                    return new QuoteExpr(ParseExprOne());

                default:
                    ReportErrorAndSkipExpr(head, "unexpected expr token {0}", head);
                    return new NumericLiteral("0");
            }
        }

        static bool CanStartExpr(TokenType type)
        {
            switch (type)
            {
                case TokenType.Symbol:
                case TokenType.Number:
                case TokenType.String:
                case TokenType.Apostrophe:
                    return true;

                default:
                    return false;
            }
        }

        AsmExpr ParseExpr()
        {
            return ParseExpr(toks.NextToken());
        }

        AsmExpr ParseExpr(Token head)
        {
            var result = ParseExprOne(head);

            while (toks.PeekToken().Type == TokenType.Plus)
            {
                toks.NextToken();

                var right = ParseExprOne();
                result = new AdditionExpr(result, right);
            }

            return result;
        }

        AsmExpr TryParseExpr()
        {
            if (CanStartExpr(toks.PeekToken().Type))
                return ParseExpr();

            return null;
        }

        AsmExpr TryParseExpr(Token head)
        {
            if (CanStartExpr(head.Type))
                return ParseExpr(head);

            return null;
        }

        AsmLine TryParseDirective(Token head)
        {
            if (head.Type == TokenType.Symbol)
            {
                if (toks.PeekToken().Type == TokenType.Equals)
                {
                    toks.NextToken();
                    var expr = ParseExpr();
                    return new EqualsDirective(head.Text, expr);
                }

                if (directiveDict.TryGetValue(head.Text, out DirectiveParseHandler handler))
                {
                    return handler(head);
                }
            }

            if (CanStartExpr(head.Type))
            {
                switch (toks.PeekToken().Type)
                {
                    case TokenType.EndOfFile:
                    case TokenType.EndOfLine:
                    case TokenType.Comma:
                    case TokenType.Plus:
                        // data directive (.WORD keyword is optional)
                        break;

                    default:
                        return ParseUnrecognizedInstruction(head);
                }

                var result = new WordDirective();
                result.Elements.Add(ParseExpr(head));
                while (TryMatchComma())
                {
                    result.Elements.Add(ParseExpr());
                }
                MatchEndOfDirective();
                return result;
            }

            return null;
        }

        AsmLine ParseUnrecognizedInstruction(Token head)
        {
            var result = new BareSymbolLine(head.Text);

            bool betweenOperands = true;

            while (true)
            {
                switch (toks.PeekToken().Type)
                {
                    case TokenType.Comma:
                        betweenOperands = true;
                        break;

                    case TokenType.Slash:
                    case TokenType.Backslash:
                        result.HasBranch = true;
                        break;

                    case TokenType.RAngle:
                        result.HasStore = true;
                        break;

                    case TokenType.EndOfFile:
                        return result;

                    case TokenType.EndOfLine:
                        toks.NextToken();
                        return result;

                    default:
                        if (betweenOperands)
                        {
                            result.OperandCount++;
                            betweenOperands = false;
                        }
                        break;
                }

                toks.NextToken();
            }
        }

        #region Directive Handlers

        delegate AsmLine DirectiveParseHandler(Token head);

        readonly IReadOnlyDictionary<string, DirectiveParseHandler> directiveDict;

        void MatchEndOfDirective()
        {
            switch (toks.PeekToken().Type)
            {
                case TokenType.EndOfLine:
                    toks.NextToken();
                    break;

                case TokenType.EndOfFile:
                    break;

                default:
                    ReportErrorAndSkipLine(toks.PeekToken(), "expected EOL after directive");
                    break;
            }
        }

        bool TryMatchComma()
        {
            if (toks.PeekToken().Type == TokenType.Comma)
            {
                toks.NextToken();
                return true;
            }

            return false;
        }

        void MatchComma()
        {
            if (!TryMatchComma())
            {
                ReportErrorAndSkipExpr(toks.PeekToken(), "expected ','");
            }
        }

        bool TryMatchEquals()
        {
            if (toks.PeekToken().Type == TokenType.Equals)
            {
                toks.NextToken();
                return true;
            }

            return false;
        }

        string MatchSymbol()
        {
            if (toks.PeekToken().Type == TokenType.Symbol)
                return toks.NextToken().Text;

            ReportErrorAndSkipExpr(toks.PeekToken(), "expected symbol");
            return "???";
        }

        string MatchString()
        {
            if (toks.PeekToken().Type == TokenType.String)
                return toks.NextToken().Text;

            ReportErrorAndSkipExpr(toks.PeekToken(), "expected string");
            return "???";
        }

        AsmLine ParseByteDirective(Token head)
        {
            var result = new ByteDirective();
            do
            {
                result.Elements.Add(ParseExpr());
            } while (TryMatchComma());
            MatchEndOfDirective();
            return result;
        }

        AsmLine ParseChrsetDirective(Token head)
        {
            var alphabetNum = ParseExpr();
            var characters = new List<AsmExpr>();
            while (TryMatchComma())
            {
                characters.Add(ParseExpr());
            }
            MatchEndOfDirective();
            return new ChrsetDirective(alphabetNum, characters);
        }

        AsmLine ParseEndDirective(Token head)
        {
            MatchEndOfDirective();
            return new EndDirective();
        }

        AsmLine ParseEndiDirective(Token head)
        {
            MatchEndOfDirective();
            return new EndiDirective();
        }

        AsmLine ParseEndtDirective(Token head)
        {
            MatchEndOfDirective();
            return new EndtDirective();
        }

        AsmLine ParseFstrDirective(Token head)
        {
            var name = MatchSymbol();
            MatchComma();
            var text = MatchString();
            MatchEndOfDirective();
            return new FstrDirective(name, text);
        }

        AsmLine ParseFunctDirective(Token head)
        {
            var result = new FunctDirective(MatchSymbol());
            while (TryMatchComma())
            {
                var localName = MatchSymbol();
                AsmExpr localDefault;
                if (TryMatchEquals())
                {
                    localDefault = ParseExpr();
                }
                else
                {
                    localDefault = null;
                }
                result.Locals.Add(new FunctLocal(localName, localDefault));
            }
            MatchEndOfDirective();
            return result;
        }

        AsmLine ParseGstrDirective(Token head)
        {
            var name = MatchSymbol();
            MatchComma();
            var text = MatchString();
            MatchEndOfDirective();
            return new GstrDirective(name, text);
        }

        AsmLine ParseGvarDirective(Token head)
        {
            var name = MatchSymbol();
            AsmExpr initialValue;
            if (TryMatchEquals())
            {
                initialValue = ParseExpr();

                if (TryMatchComma())
                    MatchSymbol();  // ignore
            }
            else
            {
                initialValue = null;
            }
            MatchEndOfDirective();
            return new GvarDirective(name, initialValue);
        }

        AsmLine ParseInsertDirective(Token head)
        {
            var filename = MatchString();
            MatchEndOfDirective();
            return new InsertDirective(filename);
        }

        AsmLine ParseLangDirective(Token head)
        {
            var langId = ParseExpr();
            MatchComma();
            var escapeChar = ParseExpr();
            MatchEndOfDirective();
            return new LangDirective(langId, escapeChar);
        }

        AsmLine ParseLenDirective(Token head)
        {
            var text = MatchString();
            MatchEndOfDirective();
            return new LenDirective(text);
        }

        AsmLine ParseNewDirective(Token head)
        {
            var version = TryParseExpr();
            MatchEndOfDirective();
            return new NewDirective(version);
        }

        AsmLine ParseObjectDirective(Token head)
        {
            var result = new ObjectDirective(MatchSymbol());
            MatchComma();
            result.Flags1 = ParseExpr();
            MatchComma();
            result.Flags2 = ParseExpr();
            MatchComma();
            // flags3 may be omitted, depending on version...
            var misc1 = ParseExpr();    // parent or flags3
            MatchComma();
            var misc2 = ParseExpr();    // sibling or parent
            MatchComma();
            var misc3 = ParseExpr();    // child or sibling
            MatchComma();
            var misc4 = ParseExpr();    // proptable or child
            if (TryMatchComma())
            {
                // flags3 provided
                result.Flags3 = misc1;
                result.Parent = misc2;
                result.Sibling = misc3;
                result.Child = misc4;
                result.PropTable = ParseExpr();
            }
            else
            {
                // flags3 omitted
                result.Parent = misc1;
                result.Sibling = misc2;
                result.Child = misc3;
                result.PropTable = misc4;
            }
            MatchEndOfDirective();
            return result;
        }

        AsmLine ParsePropDirective(Token head)
        {
            var size = ParseExpr();
            MatchComma();
            var prop = ParseExpr();
            MatchEndOfDirective();
            return new PropDirective(size, prop);
        }

        AsmLine ParseSoundDirective(Token head)
        {
            MatchEndOfDirective();
            return new SoundDirective();
        }

        AsmLine ParseStrDirective(Token head)
        {
            var text = MatchString();
            MatchEndOfDirective();
            return new StrDirective(text);
        }

        AsmLine ParseStrlDirective(Token head)
        {
            var text = MatchString();
            MatchEndOfDirective();
            return new StrlDirective(text);
        }

        AsmLine ParseTableDirective(Token head)
        {
            var size = TryParseExpr();
            MatchEndOfDirective();
            return new TableDirective(size);
        }

        AsmLine ParseTimeDirective(Token head)
        {
            MatchEndOfDirective();
            return new TimeDirective();
        }

        AsmLine ParseVocbegDirective(Token head)
        {
            var recordSize = ParseExpr();
            MatchComma();
            var keySize = ParseExpr();
            MatchEndOfDirective();
            return new VocbegDirective(recordSize, keySize);
        }

        AsmLine ParseVocendDirective(Token head)
        {
            MatchEndOfDirective();
            return new VocendDirective();
        }

        AsmLine ParseWordDirective(Token head)
        {
            var result = new WordDirective();
            do
            {
                result.Elements.Add(ParseExpr());
            } while (TryMatchComma());
            MatchEndOfDirective();
            return result;
        }

        AsmLine ParseZwordDirective(Token head)
        {
            var text = MatchString();
            MatchEndOfDirective();
            return new ZwordDirective(text);
        }

        AsmLine ParseDebugActionDirective(Token head)
        {
            var number = ParseExpr();
            MatchComma();
            var name = MatchString();
            MatchEndOfDirective();
            return new DebugActionDirective(number, name);
        }

        AsmLine ParseDebugArrayDirective(Token head)
        {
            var number = ParseExpr();
            MatchComma();
            var name = MatchString();
            MatchEndOfDirective();
            return new DebugArrayDirective(number, name);
        }

        AsmLine ParseDebugAttrDirective(Token head)
        {
            var number = ParseExpr();
            MatchComma();
            var name = MatchString();
            MatchEndOfDirective();
            return new DebugAttrDirective(number, name);
        }

        AsmLine ParseDebugFileDirective(Token head)
        {
            var number = ParseExpr();
            MatchComma();
            var includeName = MatchString();
            MatchComma();
            var actualName = MatchString();
            MatchEndOfDirective();
            return new DebugFileDirective(number, includeName, actualName);
        }

        AsmLine ParseDebugGlobalDirective(Token head)
        {
            var number = ParseExpr();
            MatchComma();
            var name = MatchString();
            MatchEndOfDirective();
            return new DebugGlobalDirective(number, name);
        }

        AsmLine ParseDebugLineDirective(Token head)
        {
            var file = ParseExpr();
            MatchComma();
            var line = ParseExpr();
            MatchComma();
            var column = ParseExpr();
            MatchEndOfDirective();
            return new DebugLineDirective(file, line, column);
        }

        AsmLine ParseDebugObjectDirective(Token head)
        {
            var number = ParseExpr();
            MatchComma();
            var name = MatchString();
            MatchComma();
            var startFile = ParseExpr();
            MatchComma();
            var startLine = ParseExpr();
            MatchComma();
            var startColumn = ParseExpr();
            MatchComma();
            var endFile = ParseExpr();
            MatchComma();
            var endLine = ParseExpr();
            MatchComma();
            var endColumn = ParseExpr();
            MatchEndOfDirective();
            return new DebugObjectDirective(
                number, name,
                startFile, startLine, startColumn,
                endFile, endLine, endColumn);
        }

        AsmLine ParseDebugPropDirective(Token head)
        {
            var number = ParseExpr();
            MatchComma();
            var name = MatchString();
            MatchEndOfDirective();
            return new DebugPropDirective(number, name);
        }

        AsmLine ParseDebugRoutineDirective(Token head)
        {
            var file = ParseExpr();
            MatchComma();
            var line = ParseExpr();
            MatchComma();
            var column = ParseExpr();
            MatchComma();
            var name = MatchString();
            var locals = new List<string>();
            while (TryMatchComma())
            {
                locals.Add(MatchString());
            }
            MatchEndOfDirective();
            return new DebugRoutineDirective(file, line, column, name, locals);
        }

        AsmLine ParseDebugRoutineEndDirective(Token head)
        {
            var file = ParseExpr();
            MatchComma();
            var line = ParseExpr();
            MatchComma();
            var column = ParseExpr();
            MatchEndOfDirective();
            return new DebugRoutineEndDirective(file, line, column);
        }

        #endregion
    }
}
