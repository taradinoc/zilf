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
using System.Diagnostics;
using System.IO;
using System.Text;
using JetBrains.Annotations;
using Zapf.Parsing.Diagnostics;

namespace Zapf.Parsing
{
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
                LineNum = lineNum;
                SourceFile = sourceFile;
            }

            public int LineNum { get; }
            public string SourceFile { get; }
        }

        [NotNull]
        ISourceLine CurrentSourceLine => new BasicSourceLine(line, filename);

        public Tokenizer([NotNull] Stream stream, [NotNull] string filename)
        {

            rdr = new StreamReader(stream);
            this.filename = filename;
        }

        char? PeekChar()
        {
            return heldChar ?? (heldChar = NextChar());
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

        /// <exception cref="SeriousError">Syntax error.</exception>
        public Token PeekToken()
        {
            if (heldToken == null)
                heldToken = NextToken();

            return (Token)heldToken;
        }

        /// <exception cref="SeriousError">Syntax error.</exception>
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
                Filename = filename,
                Line = line
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
                    result.Type = c == null ? TokenType.EndOfFile : TokenType.EndOfLine;
                    break;

                default:
                    Debug.Assert(c != null);
                    if (CharTokens.TryGetValue((char)c, out var type))
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
                var c = NextChar();

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
                        Debug.Assert(c != null);
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
                case '%':
                case '!':
                    return true;

                default:
                    return char.IsLetterOrDigit(c);
            }
        }

        static bool CanContinueSymbol(char c)
        {
            switch (c)
            {
                case '\'':
                case '/':
                    return true;

                default:
                    return CanStartSymbol(c);
            }
        }

        public void Dispose()
        {
            try
            {
                rdr?.Dispose();
            }
            finally
            {
                rdr = null;
            }
        }
    }
}