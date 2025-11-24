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
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using Gaze.Parsing.Diagnostics;
using Gaze.Parsing.Directives;
using Gaze.Parsing.Expressions;
using Gaze.Parsing.Instructions;

namespace Gaze.Parsing
{
    public sealed class GazeParser : IDisposable
    {
        readonly IErrorSink sink;
        IDictionary<string, (int opcode, GOpAttribute attr)> opcodeDict;
        Tokenizer? toks;
        int errorCount;

        public GazeParser(IErrorSink sink, IDictionary<string, (int opcode, GOpAttribute attr)> opcodeDict)
        {
            this.sink = sink;
            this.opcodeDict = opcodeDict;

            directiveDict = new Dictionary<string, DirectiveParseHandler>
            {
                { ".align", ParseAlignDirective },
                { ".byte", ParseByteDirective },
                // { ".CHRSET", ParseChrsetDirective },
                { ".end", ParseEndDirective },
                { ".endi", ParseEndiDirective },
                { ".endt", ParseEndtDirective },
                // { ".FSTR", ParseFstrDirective },
                { ".funct", ParseFunctDirective },
                { ".gstr", ParseGstrDirective },
                { ".gvar", ParseGvarDirective },
                { ".insert", ParseInsertDirective },
                // { ".LANG", ParseLangDirective },
                // { ".LEN", ParseLenDirective },
                // { ".FORM", ParseFormDirective },
                // { ".OPERAND", ParseOperandDirective },
                // { ".NEW", ParseNewDirective },
                // { ".OBJECT", ParseObjectDirective },
                // { ".PROP", ParsePropDirective },
                // { ".SOUND", ParseSoundDirective },
                { ".str", ParseStrDirective },
                // { ".STRL", parsestrldirective },
                { ".table", ParseTableDirective },
                // { ".TIME", ParseTimeDirective },
                // { ".VOCBEG", ParseVocbegDirective },
                // { ".VOCEND", ParseVocendDirective },
                { ".word", ParseWordDirective },
                // { ".ZWORD", ParseZwordDirective },

                { ".debug-action", ParseDebugActionDirective },
                { ".debug-array", ParseDebugArrayDirective },
                { ".debug-attr", ParseDebugAttrDirective },
                { ".debug-file", ParseDebugFileDirective },
                { ".debug-global", ParseDebugGlobalDirective },
                { ".debug-line", ParseDebugLineDirective },
                { ".debug-map", ParseDebugMapDirective },
                { ".debug-object", ParseDebugObjectDirective },
                { ".debug-prop", ParseDebugPropDirective },
                { ".debug-routine", ParseDebugRoutineDirective },
                { ".debug-routine-end", ParseDebugRoutineEndDirective },

                // { ".DEFSEG", IgnoreDirective },
                // { ".ENDSEG", IgnoreDirective },
                // { ".OPTIONS", IgnoreDirective },
                // { ".PICFILE", IgnoreDirective },
                // { ".SEGMENT", IgnoreDirective },

                // TODO: .TRUE and .FALSE?
            };
        }

        public void Dispose()
        {
            try
            {
                toks?.Dispose();
            }
            finally
            {
                toks = null;
            }
        }

        /// <exception cref="SeriousError">Syntax error.</exception>
        public ParseResult Parse(Stream stream, string filename)
        {
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
            Debug.Assert(toks != null);
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
            Debug.Assert(toks != null);

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

        AsmLine? TryParseInstruction(Token head)
        {
            Debug.Assert(toks != null);

            if (head.Type != TokenType.Symbol || !opcodeDict.ContainsKey(head.Text))
                return null;

            var result = new Instruction(head.Text);

            // parse operands
            while (true)
            {
                var type = toks.PeekToken().Type;

                switch (type)
                {
                    case TokenType.EndOfLine:
                        toks.NextToken();
                        return result;

                    case TokenType.EndOfFile:
                        return result;

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

        AsmLine? TryParseLabel(Token head)
        {
            Debug.Assert(toks != null);

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
            Debug.Assert(toks != null);

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

                case TokenType.Slash:
                    return new BranchOffsetExpr(ParseExprOne());

                default:
                    ReportErrorAndSkipExpr(head, "unexpected expr token {0}", head);
                    return new NumericLiteral(0);
            }
        }

        static bool CanStartExpr(TokenType type) => type is TokenType.Symbol or TokenType.Number or TokenType.String or TokenType.Apostrophe or TokenType.Slash;

        AsmExpr ParseExpr()
        {
            Debug.Assert(toks != null);

            return ParseExpr(toks.NextToken());
        }

        AsmExpr ParseExpr(Token head)
        {
            Debug.Assert(toks != null);

            var result = ParseExprOne(head);

            while (toks.PeekToken().Type == TokenType.Plus)
            {
                toks.NextToken();

                var right = ParseExprOne();
                result = new AdditionExpr(result, right);
            }

            return result;
        }

        AsmExpr? TryParseExpr()
        {
            Debug.Assert(toks != null);

            return CanStartExpr(toks.PeekToken().Type) ? ParseExpr() : null;
        }

        void MaybeSkipTypeFlag()
        {
            if (TryMatchComma())
            {
                MatchSymbol();
            }
        }

        AsmLine? TryParseDirective(Token head)
        {
            Debug.Assert(toks != null);

            if (head.Type == TokenType.Symbol)
            {
                if (toks.PeekToken().Type == TokenType.Equals)
                {
                    toks.NextToken();
                    var expr = ParseExpr();
                    MaybeSkipTypeFlag();
                    MatchEndOfDirective();
                    return new EqualsDirective(head.Text, expr);
                }

                if (directiveDict.TryGetValue(head.Text, out var handler))
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
            Debug.Assert(toks != null);

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
            Debug.Assert(toks != null);

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
            Debug.Assert(toks != null);

            if (toks.PeekToken().Type == TokenType.Comma)
            {
                toks.NextToken();
                return true;
            }

            return false;
        }

        void MatchComma()
        {
            Debug.Assert(toks != null);

            if (!TryMatchComma())
            {
                ReportErrorAndSkipExpr(toks.PeekToken(), "expected ','");
            }
        }

        bool TryMatchColon()
        {
            Debug.Assert(toks != null);

            if (toks.PeekToken().Type == TokenType.Colon)
            {
                toks.NextToken();
                return true;
            }

            return false;
        }

        void MatchColon()
        {
            Debug.Assert(toks != null);

            if (!TryMatchColon())
            {
                ReportErrorAndSkipExpr(toks.PeekToken(), "expected ':'");
            }
        }

        bool TryMatchEquals()
        {
            Debug.Assert(toks != null);

            if (toks.PeekToken().Type == TokenType.Equals)
            {
                toks.NextToken();
                return true;
            }

            return false;
        }

        string MatchSymbol()
        {
            Debug.Assert(toks != null);

            if (toks.PeekToken().Type == TokenType.Symbol)
                return toks.NextToken().Text;

            ReportErrorAndSkipExpr(toks.PeekToken(), "expected symbol");
            return "???";
        }

        string MatchString()
        {
            Debug.Assert(toks != null);

            if (toks.PeekToken().Type == TokenType.String)
                return toks.NextToken().Text;

            ReportErrorAndSkipExpr(toks.PeekToken(), "expected string");
            return "???";
        }

        AsmLine IgnoreDirective(Token head)
        {
            SkipLine();
            return new NullDirective();
        }

        AsmLine ParseFormDirective(Token head)
        {
            Debug.Assert(toks != null);

            var formToken = toks.NextToken();
            if (formToken.Type != TokenType.Symbol)
            {
                ReportErrorAndSkipLine(formToken, ".FORM expects a form specifier");
                return new NullDirective();
            }

            MatchEndOfDirective();
            return new FormDirective(formToken.Text);
        }

        AsmLine ParseOperandDirective(Token head)
        {
            Debug.Assert(toks != null);

            var index = ParseExpr();

            if (!TryMatchComma())
            {
                ReportErrorAndSkipLine(toks.PeekToken(), "expected ',' after operand index");
                return new NullDirective();
            }

            var modeToken = toks.NextToken();
            if (modeToken.Type != TokenType.Symbol)
            {
                ReportErrorAndSkipLine(modeToken, ".OPERAND expects an encoding specifier");
                return new NullDirective();
            }

            MatchEndOfDirective();
            return new OperandDirective(index, modeToken.Text);
        }

        AsmLine ParseAlignDirective(Token head)
        {
            var divisor = ParseExpr();
            MatchEndOfDirective();
            return new AlignDirective(divisor);
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
            if (TryMatchColon())
            {
                MatchSymbol();
                MatchColon();
                ParseExpr();
                MatchColon();
                ParseExpr();
            }
            while (TryMatchComma())
            {
                var localName = MatchSymbol();
                var localDefault = TryMatchEquals() ? ParseExpr() : null;
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
            AsmExpr? initialValue;
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

        AsmLine ParseDebugMapDirective(Token head)
        {
            var key = MatchString();
            var value = TryMatchEquals() ? ParseExpr() : null;
            MatchEndOfDirective();
            return new DebugMapDirective(key, value);
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
