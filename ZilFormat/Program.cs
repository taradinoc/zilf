/* Copyright 2010, 2012 Jesse McGrew
 * 
 * This file is part of ZilFormat.
 * 
 * ZilFormat is free software: you can redistribute it and/or modify
 * it under the terms of the GNU General Public License as published
 * by the Free Software Foundation, either version 3 of the License,
 * or (at your option) any later version.
 * 
 * ZilFormat is distributed in the hope that it will be useful, but
 * WITHOUT ANY WARRANTY; without even the implied warranty of
 * MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the GNU
 * General Public License for more details.
 * 
 * You should have received a copy of the GNU General Public License
 * along with ZilFormat.  If not, see <http://www.gnu.org/licenses/>.
 */

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using Antlr.Runtime;
using Zilf.Lexing;

namespace ZilFormat
{
    class Program
    {
        static void Main(string[] args)
        {
            ICharStream stream = new ANTLRFileStream(args[0]);
            ZilFormatter zform = new ZilFormatter();
            zform.Format(stream, Console.Out);
        }
    }

    class ZilFormatter
    {
        private enum Nesting
        {
            TopLevel,
            Form,
            List,
            Vector,
        }

        private readonly int maxWidth;

        public ZilFormatter()
            : this(80)
        {
        }

        public ZilFormatter(int maxWidth)
        {
            this.maxWidth = maxWidth;
        }

        public void Format(ICharStream inputStream, TextWriter output)
        {
            ZilLexer lexer = new ZilLexer(inputStream);
            Stack<Nesting> nesting = new Stack<Nesting>();
            int last = -1, width = 0;

            nesting.Push(Nesting.TopLevel);
            
            for (IToken token = lexer.NextToken(); token.Type >= 0; token = lexer.NextToken())
            {
                /*if (token.Type == ZilLexer.WS)
                    continue;*/

                //bool brokeLine = false;

                if (NeedBreakBetween(last, token.Type, nesting))
                {
                    output.WriteLine();
                    //brokeLine = true;

                    width = 0;
                    for (int i = 1; i < nesting.Count; i++)
                    {
                        output.Write("  ");
                        width += 2;
                    }
                }
                else if (NeedSpaceBetween(last, token.Type))
                {
                    output.Write(' ');
                    width++;
                }

                switch (token.Type)
                {
                    case ZilLexer.WS:
                        // ignore
                        continue;

                    case ZilLexer.ATOM:
                    case ZilLexer.NUM:
                    case ZilLexer.STRING:
                    case ZilLexer.CHAR:
                        break;

                    case ZilLexer.LANGLE:
                        nesting.Push(Nesting.Form);
                        break;
                    case ZilLexer.RANGLE:
                        if (nesting.Peek() == Nesting.Form)
                        {
                            nesting.Pop();
                        }
                        break;

                    case ZilLexer.LPAREN:
                        nesting.Push(Nesting.List);
                        break;
                    case ZilLexer.RPAREN:
                        if (nesting.Peek() == Nesting.List)
                        {
                            nesting.Pop();
                        }
                        break;

                    case ZilLexer.LSQUARE:
                        nesting.Push(Nesting.Vector);
                        break;
                    case ZilLexer.RSQUARE:
                        if (nesting.Peek() == Nesting.Vector)
                        {
                            nesting.Pop();
                        }
                        break;
                }

                /*if (!brokeLine && NeedBreakBetween(last, token.Type, nesting))
                {
                    output.WriteLine();
                    brokeLine = true;

                    width = 0;
                    for (int i = 1; i < nesting.Count; i++)
                    {
                        output.Write("  ");
                        width += 2;
                    }
                }*/

                output.Write(token.Text);
                width += token.Text.Length;
                last = token.Type;
            }
        }

        private static bool NeedBreakBetween(int lastToken, int token, Stack<Nesting> nesting)
        {
            if (IsOpenBracket(token))
                return false;

            if (IsCloseBracket(lastToken) && IsCloseBracket(token))
                return false;

            switch (nesting.Peek())
            {
                case Nesting.TopLevel:
                    return true;

                case Nesting.Form:
                    if (lastToken == ZilLexer.RANGLE || lastToken == ZilLexer.RPAREN)
                        return true;
                    break;

                case Nesting.List:
                    if (lastToken == ZilLexer.RANGLE)
                        return true;
                    break;
            }

            return false;
        }

        private static bool NeedSpaceBetween(int lastToken, int token)
        {
            if (lastToken == -1 || lastToken == ZilLexer.WS || token == ZilLexer.WS)
                return false;

            if (IsOpenBracket(lastToken))
                return false;

            if (IsCloseBracket(token))
                return false;

            switch (lastToken)
            {
                case ZilLexer.DOT:
                case ZilLexer.COMMA:
                case ZilLexer.APOS:
                case ZilLexer.BANG:
                case ZilLexer.SEMI:
                    return false;
            }

            return true;
        }

        private static bool IsOpenBracket(int token)
        {
            switch (token)
            {
                case ZilLexer.LANGLE:
                case ZilLexer.LPAREN:
                case ZilLexer.LSQUARE:
                    return true;
                default:
                    return false;
            }
        }

        private static bool IsCloseBracket(int token)
        {
            switch (token)
            {
                case ZilLexer.RANGLE:
                case ZilLexer.RPAREN:
                case ZilLexer.RSQUARE:
                    return true;
                default:
                    return false;
            }
        }
    }
}
