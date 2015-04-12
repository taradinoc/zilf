using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using Antlr.Runtime;
using Zapf.Lexing;
using Antlr.Runtime.Tree;

namespace Zapf.Parsing
{
    struct ParseResult
    {
        public IEnumerable<AsmLine> Lines;
        public int NumberOfSyntaxErrors;
    }

    abstract class AsmLine : ISourceLine
    {
        public string SourceFile { get; set; }
        public int LineNum { get; set; }
    }

    sealed class EndDirective : AsmLine
    {
    }

    sealed class EndiDirective : AsmLine
    {
    }

    sealed class InsertDirective : AsmLine
    {
        public InsertDirective(string filename)
        {
            this.InsertFileName = filename;
        }

        public string InsertFileName { get; set; }
    }

    sealed class NewDirective : AsmLine
    {
        public NewDirective(AsmExpr version)
        {
            this.Version = version;
        }

        public AsmExpr Version { get; set; }
    }

    sealed class TimeDirective : AsmLine
    {
        public TimeDirective()
        {
        }
    }

    struct FunctLocal
    {
        public FunctLocal(string name, AsmExpr defaultValue)
        {
            this.Name = name;
            this.DefaultValue = defaultValue;
        }

        public readonly string Name;
        public readonly AsmExpr DefaultValue;
    }

    sealed class FunctDirective : AsmLine
    {
        public FunctDirective()
        {
            this.Locals = new List<FunctLocal>();
        }

        public string Name { get; set; }
        public IList<FunctLocal> Locals { get; private set; }
    }

    sealed class TableDirective : AsmLine
    {
        public TableDirective(AsmExpr size)
        {
            this.Size = size;
        }

        public AsmExpr Size { get; private set; }
    }

    sealed class EndtDirective : AsmLine
    {
    }

    sealed class VocbegDirective : AsmLine
    {
        public VocbegDirective(AsmExpr recordSize, AsmExpr keySize)
        {
            this.RecordSize = recordSize;
            this.KeySize = keySize;
        }

        public AsmExpr RecordSize { get; private set; }
        public AsmExpr KeySize { get; private set; }
    }

    sealed class VocendDirective : AsmLine
    {
    }

    abstract class DataDirective : AsmLine
    {
        public DataDirective()
        {
            this.Elements = new List<AsmExpr>();
        }

        public IList<AsmExpr> Elements { get; private set; }
    }

    sealed class ByteDirective : DataDirective
    {
    }

    sealed class WordDirective : DataDirective
    {
    }

    sealed class Instruction : AsmLine
    {
        public const string BranchTrue = "TRUE";
        public const string BranchFalse = "FALSE";

        public Instruction(string name)
        {
            this.Name = name;

            this.Operands = new List<AsmExpr>();
        }

        public string Name { get; set; }

        public IList<AsmExpr> Operands { get; private set; }

        public string StoreTarget { get; set; }

        public bool? BranchPolarity { get; set; }
        public string BranchTarget { get; set; }
    }

    sealed class BareSymbolLine : AsmLine
    {
        public BareSymbolLine(string text)
        {
            this.Text = text;
        }

        public string Text { get; set; }

        public int OperandCount { get; set; }
        public bool HasStore { get; set; }
        public bool HasBranch { get; set; }

        public bool UsedAsInstruction
        {
            get { return OperandCount > 0 || HasStore || HasBranch; }
        }
    }

    sealed class LocalLabel : AsmLine
    {
        public LocalLabel(string name)
        {
            this.Name = name;
        }

        public string Name { get; set; }
    }

    sealed class GlobalLabel : AsmLine
    {
        public GlobalLabel(string name)
        {
            this.Name = name;
        }

        public string Name { get; set; }
    }

    sealed class FstrDirective : AsmLine
    {
        public FstrDirective(string name, string text)
        {
            this.Name = name;
            this.Text = text;
        }

        public string Name { get; set; }
        public string Text { get; set; }
    }

    sealed class GstrDirective : AsmLine
    {
        public GstrDirective(string name, string text)
        {
            this.Name = name;
            this.Text = text;
        }

        public string Name { get; set; }
        public string Text { get; set; }
    }

    sealed class StrDirective : AsmLine
    {
        public StrDirective(string text)
        {
            this.Text = text;
        }

        public string Text { get; set; }
    }

    sealed class LenDirective : AsmLine
    {
        public LenDirective(string text)
        {
            this.Text = text;
        }

        public string Text { get; set; }
    }

    sealed class StrlDirective : AsmLine
    {
        public StrlDirective(string text)
        {
            this.Text = text;
        }

        public string Text { get; set; }
    }

    sealed class ZwordDirective : AsmLine
    {
        public ZwordDirective(string text)
        {
            this.Text = text;
        }

        public string Text { get; set; }
    }

    sealed class GvarDirective : AsmLine
    {
        public GvarDirective(string name, AsmExpr initialValue)
        {
            this.Name = name;
            this.InitialValue = initialValue;
        }

        public string Name { get; set; }
        public AsmExpr InitialValue { get; set; }
    }

    sealed class ObjectDirective : AsmLine
    {
        public string Name { get; set; }
        public AsmExpr Flags1 { get; set; }
        public AsmExpr Flags2 { get; set; }
        public AsmExpr Flags3 { get; set; }
        public AsmExpr Parent { get; set; }
        public AsmExpr Sibling { get; set; }
        public AsmExpr Child { get; set; }
        public AsmExpr PropTable { get; set; }
    }

    sealed class PropDirective : AsmLine
    {
        public PropDirective(AsmExpr size, AsmExpr prop)
        {
            this.Size = size;
            this.Prop = prop;
        }

        public AsmExpr Size { get; set; }
        public AsmExpr Prop { get; set; }
    }

    sealed class EqualsDirective : AsmLine
    {
        public EqualsDirective(string left, AsmExpr right)
        {
            this.Left = left;
            this.Right = right;
        }

        public string Left { get; set; }
        public AsmExpr Right { get; set; }
    }

    abstract class DebugDirective : AsmLine
    {
    }

    sealed class DebugActionDirective : DebugDirective
    {
        public DebugActionDirective(AsmExpr number, string name)
        {
            this.Number = number;
            this.Name = name;
        }

        public AsmExpr Number { get; set; }
        public string Name { get; set; }
    }

    sealed class DebugArrayDirective : DebugDirective
    {
        public DebugArrayDirective(AsmExpr number, string name)
        {
            this.Number = number;
            this.Name = name;
        }

        public AsmExpr Number { get; set; }
        public string Name { get; set; }
    }

    sealed class DebugAttrDirective : DebugDirective
    {
        public DebugAttrDirective(AsmExpr number, string name)
        {
            this.Number = number;
            this.Name = name;
        }

        public AsmExpr Number { get; set; }
        public string Name { get; set; }
    }

    sealed class DebugClassDirective : DebugDirective
    {
        public string Name { get; set; }

        public AsmExpr StartFile { get; set; }

        public AsmExpr StartLine { get; set; }

        public AsmExpr StartColumn { get; set; }

        public AsmExpr EndFile { get; set; }

        public AsmExpr EndLine { get; set; }

        public AsmExpr EndColumn { get; set; }
    }

    sealed class DebugFakeActionDirective : DebugDirective
    {
        public AsmExpr Number { get; set; }

        public string Name { get; set; }
    }

    sealed class DebugFileDirective : DebugDirective
    {
        public DebugFileDirective(AsmExpr number, string includeName, string actualName)
        {
            this.Number = number;
            this.IncludeName = includeName;
            this.ActualName = actualName;
        }

        public AsmExpr Number { get; set; }
        public string IncludeName { get; set; }
        public string ActualName { get; set; }
    }

    sealed class DebugGlobalDirective : DebugDirective
    {
        public DebugGlobalDirective(AsmExpr number, string name)
        {
            this.Number = number;
            this.Name = name;
        }

        public AsmExpr Number { get; set; }
        public string Name { get; set; }
    }

    sealed class DebugLineDirective : DebugDirective
    {
        public DebugLineDirective(AsmExpr file, AsmExpr line, AsmExpr column)
        {
            this.TheFile = file;
            this.TheLine = line;
            this.TheColumn = column;
        }

        public AsmExpr TheFile { get; set; }
        public AsmExpr TheLine { get; set; }
        public AsmExpr TheColumn { get; set; }
    }

    sealed class DebugMapDirective : DebugDirective
    {
        public string Key { get; set; }

        public AsmExpr Value { get; set; }
    }

    sealed class DebugObjectDirective : DebugDirective
    {
        public DebugObjectDirective(AsmExpr number, string name,
            AsmExpr startFile, AsmExpr startLine, AsmExpr startColumn,
            AsmExpr endFile, AsmExpr endLine, AsmExpr endColumn)
        {
            this.Number = number;
            this.Name = name;
            this.StartFile = startFile;
            this.StartLine = startLine;
            this.StartColumn = startColumn;
            this.EndFile = endFile;
            this.EndLine = endLine;
            this.EndColumn = endColumn;
        }

        public AsmExpr Number { get; set; }
        public string Name { get; set; }
        public AsmExpr StartFile { get; set; }
        public AsmExpr StartLine { get; set; }
        public AsmExpr StartColumn { get; set; }
        public AsmExpr EndFile { get; set; }
        public AsmExpr EndLine { get; set; }
        public AsmExpr EndColumn { get; set; }
    }

    sealed class DebugPropDirective : DebugDirective
    {
        public DebugPropDirective(AsmExpr number, string name)
        {
            this.Number = number;
            this.Name = name;
        }

        public AsmExpr Number { get; set; }
        public string Name { get; set; }
    }

    sealed class DebugRoutineDirective : DebugDirective
    {
        public DebugRoutineDirective(AsmExpr file, AsmExpr line, AsmExpr column,
            string name, IEnumerable<string> locals)
        {
            this.TheFile = file;
            this.TheLine = line;
            this.TheColumn = column;
            this.Name = name;
            this.Locals = new List<string>(locals);
        }

        public string Name { get; set; }
        public IList<string> Locals { get; private set; }
        public AsmExpr TheFile { get; set; }
        public AsmExpr TheLine { get; set; }
        public AsmExpr TheColumn { get; set; }
    }

    sealed class DebugRoutineEndDirective : DebugDirective
    {
        public DebugRoutineEndDirective(AsmExpr file, AsmExpr line, AsmExpr column)
        {
            this.TheFile = file;
            this.TheLine = line;
            this.TheColumn = column;
        }

        public AsmExpr TheFile { get; set; }
        public AsmExpr TheLine { get; set; }
        public AsmExpr TheColumn { get; set; }
    }

    abstract class AsmExpr : ISourceLine
    {
        public string SourceFile { get; set; }
        public int LineNum { get; set; }

        public string Text { get; set; }
    }

    sealed class NumericLiteral : AsmExpr
    {
        public NumericLiteral(string text)
        {
            this.Text = text;
        }
    }

    sealed class StringLiteral : AsmExpr
    {
        public StringLiteral(string text)
        {
            this.Text = text;
        }
    }

    sealed class SymbolExpr : AsmExpr
    {
        public SymbolExpr(string name)
        {
            this.Text = name;
        }
    }

    sealed class AdditionExpr : AsmExpr
    {
        public AdditionExpr(AsmExpr left, AsmExpr right)
        {
            this.Left = left;
            this.Right = right;
        }

        public AsmExpr Left { get; set; }
        public AsmExpr Right { get; set; }
    }

    sealed class QuoteExpr : AsmExpr
    {
        public QuoteExpr(AsmExpr inner)
        {
            this.Inner = inner;
        }

        public AsmExpr Inner { get; set; }
    }

    class AntlrBasedParser
    {
        private readonly bool informMode;
        private readonly IDictionary<string, KeyValuePair<ushort, ZOpAttribute>> opcodeDict;

        public AntlrBasedParser(bool informMode, IDictionary<string, KeyValuePair<ushort, ZOpAttribute>> opcodeDict)
        {
            this.informMode = informMode;
            this.opcodeDict = opcodeDict;
        }

        public ParseResult Parse(Stream stream, string filename)
        {
            ICharStream charStream = new ANTLRInputStream(stream);
            ITokenSource lexer;

            if (informMode)
                lexer = new ZapInf(charStream) { OpcodeDict = (System.Collections.IDictionary)opcodeDict };
            else
                lexer = new ZapLexer(charStream) { OpcodeDict = (System.Collections.IDictionary)opcodeDict };

            ZapParser parser = new ZapParser(new CommonTokenStream(lexer));
            parser.InformMode = informMode;

            var fret = parser.file();

            return new ParseResult()
            {
                Lines = GetLines(fret.Tree, filename),
                NumberOfSyntaxErrors = parser.NumberOfSyntaxErrors,
            };
        }

        private static IEnumerable<ITree> GetRootNodes(object root)
        {
            ITree tree = (ITree)root;

            if (tree.Type == 0)
            {
                for (int i = 0; i < tree.ChildCount; i++)
                    yield return tree.GetChild(i);
            }
            else
            {
                yield return tree;
            }
        }

        private static Instruction MakeInstruction(ITree node)
        {
            var result = new Instruction(node.Text);

            for (int i = 0; i < node.ChildCount; i++)
            {
                ITree child = node.GetChild(i);
                switch (child.Type)
                {
                    case ZapParser.SLASH:
                    case ZapParser.BACKSLASH:
                        if (result.BranchTarget == null)
                        {
                            result.BranchTarget = child.GetChild(0).Text;
                            result.BranchPolarity = (child.Type == ZapParser.SLASH);
                        }
                        else
                            Errors.ThrowFatal(child, "multiple branch operands");
                        break;

                    case ZapParser.RANGLE:
                        if (result.StoreTarget == null)
                            result.StoreTarget = child.GetChild(0).Text;
                        else
                            Errors.ThrowFatal(child, "multiple store operands");
                        break;

                    default:
                        if (result.BranchTarget != null || result.StoreTarget != null)
                            Errors.ThrowFatal(child, "normal operand after branch/store operand");
                        var expr = ParseAsmExpr(child);
                        result.Operands.Add(expr);
                        break;
                }
            }

            return result;
        }

        private static BareSymbolLine MakeBareSymbolLine(ITree node)
        {
            var result = new BareSymbolLine(node.Text);

            for (int i = 0; i < node.ChildCount; i++)
            {
                ITree child = node.GetChild(i);
                switch (child.Type)
                {
                    case ZapParser.SLASH:
                    case ZapParser.BACKSLASH:
                        result.HasBranch = true;
                        break;

                    case ZapParser.RANGLE:
                        result.HasStore = true;
                        break;

                    default:
                        result.OperandCount++;
                        break;
                }
            }

            return result;
        }

        private static IEnumerable<AsmLine> GetLines(object root, string filename)
        {
            foreach (var node in GetRootNodes(root))
            {
                AsmLine line;

                switch (node.Type)
                {
                    case ZapParser.OPCODE:
                        line = MakeInstruction(node);
                        break;

                    case ZapParser.SYMBOL:
                        line = MakeBareSymbolLine(node);
                        break;

                    case ZapParser.LLABEL:
                        line = new LocalLabel(node.Text);
                        break;

                    case ZapParser.GLABEL:
                        line = new GlobalLabel(node.Text);
                        break;

                    case ZapParser.INSERT:
                        line = new InsertDirective(node.GetChild(0).Text);
                        break;

                    case ZapParser.ENDI:
                        line = new EndiDirective();
                        break;

                    case ZapParser.END:
                        line = new EndDirective();
                        break;

                    case ZapParser.NEW:
                        line = new NewDirective(node.ChildCount > 0 ? ParseAsmExpr(node.GetChild(0)) : null);
                        break;

                    case ZapParser.TIME:
                        line = new TimeDirective();
                        break;

                    case ZapParser.FUNCT:
                        line = MakeFunctDirective(node);
                        break;

                    case ZapParser.TABLE:
                        line = new TableDirective(
                            node.ChildCount >= 1 ? ParseAsmExpr(node.GetChild(0)) : null);
                        break;

                    case ZapParser.ENDT:
                        line = new EndtDirective();
                        break;

                    case ZapParser.VOCBEG:
                        line = new VocbegDirective(
                            ParseAsmExpr(node.GetChild(0)),
                            ParseAsmExpr(node.GetChild(1)));
                        break;

                    case ZapParser.VOCEND:
                        line = new VocendDirective();
                        break;

                    case ZapParser.BYTE:
                        line = new ByteDirective();
                        ParseDataElements((DataDirective)line, node);
                        break;

                    case ZapParser.WORD:
                        line = new WordDirective();
                        ParseDataElements((DataDirective)line, node);
                        break;

                    case ZapParser.FSTR:
                        line = new FstrDirective(node.GetChild(0).Text, node.GetChild(1).Text);
                        break;

                    case ZapParser.GSTR:
                        line = new GstrDirective(node.GetChild(0).Text, node.GetChild(1).Text);
                        break;

                    case ZapParser.STR:
                        line = new StrDirective(node.GetChild(0).Text);
                        break;

                    case ZapParser.STRL:
                        line = new StrlDirective(node.GetChild(0).Text);
                        break;

                    case ZapParser.LEN:
                        line = new LenDirective(node.GetChild(0).Text);
                        break;

                    case ZapParser.ZWORD:
                        line = new ZwordDirective(node.GetChild(0).Text);
                        break;

                    case ZapParser.EQUALS:
                        line = new EqualsDirective(node.GetChild(0).Text, ParseAsmExpr(node.GetChild(1)));
                        break;

                    case ZapParser.GVAR:
                        line = new GvarDirective(node.GetChild(0).Text, ParseAsmExpr(node.GetChild(1)));
                        break;

                    case ZapParser.OBJECT:
                        line = MakeObjectDirective(node);
                        break;

                    case ZapParser.PROP:
                        line = new PropDirective(ParseAsmExpr(node.GetChild(0)), ParseAsmExpr(node.GetChild(1)));
                        break;

                    case ZapParser.DEBUG_ACTION:
                        line = new DebugActionDirective(ParseAsmExpr(node.GetChild(0)), node.GetChild(1).Text);
                        break;

                    case ZapParser.DEBUG_ARRAY:
                        line = new DebugArrayDirective(ParseAsmExpr(node.GetChild(0)), node.GetChild(1).Text);
                        break;

                    case ZapParser.DEBUG_ATTR:
                        line = new DebugAttrDirective(ParseAsmExpr(node.GetChild(0)), node.GetChild(1).Text);
                        break;

                    case ZapParser.DEBUG_FILE:
                        line = new DebugFileDirective(ParseAsmExpr(node.GetChild(0)), node.GetChild(1).Text, node.GetChild(2).Text);
                        break;

                    case ZapParser.DEBUG_GLOBAL:
                        line = new DebugGlobalDirective(ParseAsmExpr(node.GetChild(0)), node.GetChild(1).Text);
                        break;

                    case ZapParser.DEBUG_LINE:
                        line = new DebugLineDirective(ParseAsmExpr(node.GetChild(0)), ParseAsmExpr(node.GetChild(1)), ParseAsmExpr(node.GetChild(2)));
                        break;

                    case ZapParser.DEBUG_OBJECT:
                        line = new DebugObjectDirective(
                            ParseAsmExpr(node.GetChild(0)), node.GetChild(1).Text,
                            ParseAsmExpr(node.GetChild(2)), ParseAsmExpr(node.GetChild(3)), ParseAsmExpr(node.GetChild(4)),
                            ParseAsmExpr(node.GetChild(5)), ParseAsmExpr(node.GetChild(6)), ParseAsmExpr(node.GetChild(7)));
                        break;

                    case ZapParser.DEBUG_PROP:
                        line = new DebugPropDirective(ParseAsmExpr(node.GetChild(0)), node.GetChild(1).Text);
                        break;

                    case ZapParser.DEBUG_ROUTINE:
                        line = new DebugRoutineDirective(
                            ParseAsmExpr(node.GetChild(0)), ParseAsmExpr(node.GetChild(1)), ParseAsmExpr(node.GetChild(2)),
                            node.GetChild(3).Text,
                            Enumerable.Range(4, node.ChildCount - 4).Select(i => node.GetChild(i).Text));
                        break;

                    case ZapParser.DEBUG_ROUTINE_END:
                        line = new DebugRoutineEndDirective(ParseAsmExpr(node.GetChild(0)), ParseAsmExpr(node.GetChild(1)), ParseAsmExpr(node.GetChild(2)));
                        break;

                    case ZapParser.DEBUG_CLASS:
                    case ZapParser.DEBUG_FAKE_ACTION:
                    case ZapParser.DEBUG_MAP:
                    default:
                        //XXX
                        throw new NotImplementedException();
                }

                line.SourceFile = filename;
                line.LineNum = node.Line;
                yield return line;
            }
        }

        private static AsmLine MakeObjectDirective(ITree node)
        {
            var result = new ObjectDirective();

            result.Name = node.GetChild(0).Text;
            result.Flags1 = ParseAsmExpr(node.GetChild(1));
            result.Flags2 = ParseAsmExpr(node.GetChild(2));

            switch (node.ChildCount)
            {
                case 7:
                    result.Parent = ParseAsmExpr(node.GetChild(3));
                    result.Sibling = ParseAsmExpr(node.GetChild(4));
                    result.Child = ParseAsmExpr(node.GetChild(5));
                    result.PropTable = ParseAsmExpr(node.GetChild(6));
                    break;

                case 8:
                    result.Flags3 = ParseAsmExpr(node.GetChild(3));
                    result.Parent = ParseAsmExpr(node.GetChild(4));
                    result.Sibling = ParseAsmExpr(node.GetChild(5));
                    result.Child = ParseAsmExpr(node.GetChild(6));
                    result.PropTable = ParseAsmExpr(node.GetChild(7));
                    break;

                default:
                    throw new NotImplementedException();
            }

            return result;
        }

        private static void ParseDataElements(DataDirective line, ITree node)
        {
            var elements = line.Elements;

            for (int i = 0; i < node.ChildCount; i++)
                elements.Add(ParseAsmExpr(node.GetChild(i)));
        }

        private static AsmLine MakeFunctDirective(ITree node)
        {
            var result = new FunctDirective();

            result.Name = node.GetChild(0).Text;

            for (int i = 1; i < node.ChildCount; i++)
            {
                var child = node.GetChild(i);

                ITree local, defaultValue;
                if (child.Type == ZapParser.EQUALS)
                {
                    local = child.GetChild(0);
                    defaultValue = child.GetChild(1);
                }
                else
                {
                    local = child;
                    defaultValue = null;
                }

                result.Locals.Add(new FunctLocal(
                    local.Text,
                    defaultValue == null ? null : ParseAsmExpr(defaultValue)));
            }

            return result;
        }

        private static AsmExpr ParseAsmExpr(ITree tree)
        {
            switch (tree.Type)
            {
                case ZapParser.PLUS:
                    return new AdditionExpr(ParseAsmExpr(tree.GetChild(0)), ParseAsmExpr(tree.GetChild(1)));

                case ZapParser.APOSTROPHE:
                    return new QuoteExpr(ParseAsmExpr(tree.GetChild(0)));

                case ZapParser.NUM:
                    return new NumericLiteral(tree.Text);

                case ZapParser.STRING:
                    return new StringLiteral(tree.Text);

                case ZapParser.SYMBOL:
                    return new SymbolExpr(tree.Text);

                default:
                    throw new NotImplementedException();
            }
        }
    }
}
