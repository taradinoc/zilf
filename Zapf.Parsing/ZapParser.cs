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
using System.IO;
using JetBrains.Annotations;
using Zapf.Parsing.Diagnostics;
using Zapf.Parsing.Directives;
using Zapf.Parsing.Expressions;
using Zapf.Parsing.Instructions;

namespace Zapf.Parsing
{
    public class ZapParser : IDisposable
    {
        readonly IErrorSink sink;
        readonly IDictionary<string, KeyValuePair<ushort, ZOpAttribute>> opcodeDict;
        Tokenizer toks;
        int errorCount;

        public ZapParser(IErrorSink sink, [NotNull] IDictionary<string, KeyValuePair<ushort, ZOpAttribute>> opcodeDict)
        {
            this.sink = sink;
            this.opcodeDict = opcodeDict;

            directiveDict = new Dictionary<string, DirectiveParseHandler>
            {
                { ".ALIGN", ParseAlignDirective },
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
                { ".DEBUG-MAP", ParseDebugMapDirective },
                { ".DEBUG-OBJECT", ParseDebugObjectDirective },
                { ".DEBUG-PROP", ParseDebugPropDirective },
                { ".DEBUG-ROUTINE", ParseDebugRoutineDirective },
                { ".DEBUG-ROUTINE-END", ParseDebugRoutineEndDirective },

                { ".DEFSEG", IgnoreDirective },
                { ".ENDSEG", IgnoreDirective },
                { ".OPTIONS", IgnoreDirective },
                { ".PICFILE", IgnoreDirective },
                { ".SEGMENT", IgnoreDirective },

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
        public ParseResult Parse([NotNull] Stream stream, [NotNull] string filename)
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

        void ReportError(ISourceLine node, [NotNull] string message)
        {
            errorCount++;
            Errors.Serious(sink, node, message);
        }

        void ReportError(ISourceLine node, [NotNull] string format, [NotNull] params object[] args)
        {
            errorCount++;
            Errors.Serious(sink, node, format, args);
        }

        void ReportErrorAndSkipLine(ISourceLine node, [NotNull] string message)
        {
            ReportError(node, message);
            SkipLine();
        }

        void ReportErrorAndSkipLine(ISourceLine node, [NotNull] string format, [NotNull] params object[] args)
        {
            ReportError(node, format, args);
            SkipLine();
        }

        void ReportErrorAndSkipExpr(ISourceLine node, [NotNull] string message)
        {
            ReportError(node, message);
            SkipExpr();
        }

        void ReportErrorAndSkipExpr(ISourceLine node, [NotNull] string format, [NotNull] params object[] args)
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

        [CanBeNull]
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

        [CanBeNull]
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

        [NotNull]
        AsmExpr ParseExprOne()
        {
            return ParseExprOne(toks.NextToken());
        }

        [NotNull]
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
                    return new NumericLiteral(0);
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

        [NotNull]
        AsmExpr ParseExpr()
        {
            return ParseExpr(toks.NextToken());
        }

        [NotNull]
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

        [CanBeNull]
        AsmExpr TryParseExpr()
        {
            return CanStartExpr(toks.PeekToken().Type) ? ParseExpr() : null;
        }

        void MaybeSkipTypeFlag()
        {
            if (TryMatchComma())
            {
                MatchSymbol();
            }
        }

        AsmLine TryParseDirective(Token head)
        {
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

        [NotNull]
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

        [NotNull] delegate AsmLine DirectiveParseHandler(Token head);

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

        bool TryMatchColon()
        {
            if (toks.PeekToken().Type == TokenType.Colon)
            {
                toks.NextToken();
                return true;
            }

            return false;
        }

        void MatchColon()
        {
            if (!TryMatchColon())
            {
                ReportErrorAndSkipExpr(toks.PeekToken(), "expected ':'");
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

        [NotNull]
        AsmLine IgnoreDirective(Token head)
        {
            SkipLine();
            return new NullDirective();
        }

        [NotNull]
        AsmLine ParseAlignDirective(Token head)
        {
            var divisor = ParseExpr();
            MatchEndOfDirective();
            return new AlignDirective(divisor);
        }

        [NotNull]
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

        [NotNull]
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

        [NotNull]
        AsmLine ParseEndDirective(Token head)
        {
            MatchEndOfDirective();
            return new EndDirective();
        }

        [NotNull]
        AsmLine ParseEndiDirective(Token head)
        {
            MatchEndOfDirective();
            return new EndiDirective();
        }

        [NotNull]
        AsmLine ParseEndtDirective(Token head)
        {
            MatchEndOfDirective();
            return new EndtDirective();
        }

        [NotNull]
        AsmLine ParseFstrDirective(Token head)
        {
            var name = MatchSymbol();
            MatchComma();
            var text = MatchString();
            MatchEndOfDirective();
            return new FstrDirective(name, text);
        }

        [NotNull]
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

        [NotNull]
        AsmLine ParseGstrDirective(Token head)
        {
            var name = MatchSymbol();
            MatchComma();
            var text = MatchString();
            MatchEndOfDirective();
            return new GstrDirective(name, text);
        }

        [NotNull]
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

        [NotNull]
        AsmLine ParseInsertDirective(Token head)
        {
            var filename = MatchString();
            MatchEndOfDirective();
            return new InsertDirective(filename);
        }

        [NotNull]
        AsmLine ParseLangDirective(Token head)
        {
            var langId = ParseExpr();
            MatchComma();
            var escapeChar = ParseExpr();
            MatchEndOfDirective();
            return new LangDirective(langId, escapeChar);
        }

        [NotNull]
        AsmLine ParseLenDirective(Token head)
        {
            var text = MatchString();
            MatchEndOfDirective();
            return new LenDirective(text);
        }

        [NotNull]
        AsmLine ParseNewDirective(Token head)
        {
            var version = TryParseExpr();
            MatchEndOfDirective();
            return new NewDirective(version);
        }

        [NotNull]
        AsmLine ParseObjectDirective(Token head)
        {
            var name = MatchSymbol();
            MatchComma();
            var flags1 = ParseExpr();
            MatchComma();
            var flags2 = ParseExpr();
            MatchComma();
            // flags3 may be omitted, depending on version...
            var parentOrFlags3 = ParseExpr();    // parent or flags3
            MatchComma();
            var siblingOrParent = ParseExpr();    // sibling or parent
            MatchComma();
            var childOrSibling = ParseExpr();    // child or sibling
            MatchComma();
            var propTableOrChild = ParseExpr();    // proptable or child

            AsmExpr flags3, parent, sibling, child, propTable;
            if (TryMatchComma())
            {
                // flags3 provided
                flags3 = parentOrFlags3;
                parent = siblingOrParent;
                sibling = childOrSibling;
                child = propTableOrChild;
                propTable = ParseExpr();
            }
            else
            {
                // flags3 omitted
                flags3 = null;
                parent = parentOrFlags3;
                sibling = siblingOrParent;
                child = childOrSibling;
                propTable = propTableOrChild;
            }
            MatchEndOfDirective();
            return new ObjectDirective(name, flags1, flags2, flags3, parent, sibling, child, propTable);
        }

        [NotNull]
        AsmLine ParsePropDirective(Token head)
        {
            var size = ParseExpr();
            MatchComma();
            var prop = ParseExpr();
            MatchEndOfDirective();
            return new PropDirective(size, prop);
        }

        [NotNull]
        AsmLine ParseSoundDirective(Token head)
        {
            MatchEndOfDirective();
            return new SoundDirective();
        }

        [NotNull]
        AsmLine ParseStrDirective(Token head)
        {
            var text = MatchString();
            MatchEndOfDirective();
            return new StrDirective(text);
        }

        [NotNull]
        AsmLine ParseStrlDirective(Token head)
        {
            var text = MatchString();
            MatchEndOfDirective();
            return new StrlDirective(text);
        }

        [NotNull]
        AsmLine ParseTableDirective(Token head)
        {
            var size = TryParseExpr();
            MatchEndOfDirective();
            return new TableDirective(size);
        }

        [NotNull]
        AsmLine ParseTimeDirective(Token head)
        {
            MatchEndOfDirective();
            return new TimeDirective();
        }

        [NotNull]
        AsmLine ParseVocbegDirective(Token head)
        {
            var recordSize = ParseExpr();
            MatchComma();
            var keySize = ParseExpr();
            MatchEndOfDirective();
            return new VocbegDirective(recordSize, keySize);
        }

        [NotNull]
        AsmLine ParseVocendDirective(Token head)
        {
            MatchEndOfDirective();
            return new VocendDirective();
        }

        [NotNull]
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

        [NotNull]
        AsmLine ParseZwordDirective(Token head)
        {
            var text = MatchString();
            MatchEndOfDirective();
            return new ZwordDirective(text);
        }

        [NotNull]
        AsmLine ParseDebugActionDirective(Token head)
        {
            var number = ParseExpr();
            MatchComma();
            var name = MatchString();
            MatchEndOfDirective();
            return new DebugActionDirective(number, name);
        }

        [NotNull]
        AsmLine ParseDebugArrayDirective(Token head)
        {
            var number = ParseExpr();
            MatchComma();
            var name = MatchString();
            MatchEndOfDirective();
            return new DebugArrayDirective(number, name);
        }

        [NotNull]
        AsmLine ParseDebugAttrDirective(Token head)
        {
            var number = ParseExpr();
            MatchComma();
            var name = MatchString();
            MatchEndOfDirective();
            return new DebugAttrDirective(number, name);
        }

        [NotNull]
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

        [NotNull]
        AsmLine ParseDebugGlobalDirective(Token head)
        {
            var number = ParseExpr();
            MatchComma();
            var name = MatchString();
            MatchEndOfDirective();
            return new DebugGlobalDirective(number, name);
        }

        [NotNull]
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

        [NotNull]
        AsmLine ParseDebugMapDirective(Token head)
        {
            var key = MatchString();
            var value = TryMatchEquals() ? ParseExpr() : null;
            MatchEndOfDirective();
            return new DebugMapDirective(key, value);
        }

        [NotNull]
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

        [NotNull]
        AsmLine ParseDebugPropDirective(Token head)
        {
            var number = ParseExpr();
            MatchComma();
            var name = MatchString();
            MatchEndOfDirective();
            return new DebugPropDirective(number, name);
        }

        [NotNull]
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

        [NotNull]
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
