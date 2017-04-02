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

using System.Collections.Generic;

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

    abstract class Directive : AsmLine
    {
    }

    abstract class NamedDirective : Directive
    {
        protected NamedDirective(string name)
        {
            this.Name = name;
        }

        public string Name { get; }
    }

    abstract class TextDirective : Directive
    {
        protected TextDirective(string text)
        {
            this.Text = text;
        }

        public string Text { get; }
    }

    abstract class NameAndTextDirective : Directive
    {
        protected NameAndTextDirective(string name, string text)
        {
            this.Name = name;
            this.Text = text;
        }

        public string Name { get; }
        public string Text { get; }
    }

    sealed class EndDirective : Directive
    {
    }

    sealed class EndiDirective : Directive
    {
    }

    sealed class InsertDirective : Directive
    {
        public InsertDirective(string filename)
        {
            this.InsertFileName = filename;
        }

        public string InsertFileName { get; set; }
    }

    sealed class NewDirective : Directive
    {
        public NewDirective(AsmExpr version)
        {
            this.Version = version;
        }

        public AsmExpr Version { get; set; }
    }

    sealed class TimeDirective : Directive
    {
    }

    sealed class SoundDirective : Directive
    {
    }

    sealed class LangDirective : Directive
    {
        public LangDirective(AsmExpr langId, AsmExpr escapeChar)
        {
            this.LanguageId = langId;
            this.EscapeChar = escapeChar;
        }

        public AsmExpr LanguageId { get; set; }
        public AsmExpr EscapeChar { get; set; }
    }

    sealed class ChrsetDirective : Directive
    {
        public ChrsetDirective(AsmExpr alphabetNum, IEnumerable<AsmExpr> characters)
        {
            this.CharsetNum = alphabetNum;
            this.Characters = new List<AsmExpr>(characters);
        }

        public AsmExpr CharsetNum { get; set; }
        public IList<AsmExpr> Characters { get; private set; }
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

    sealed class FunctDirective : NamedDirective
    {
        public FunctDirective(string name)
            : base(name)
        {
            this.Locals = new List<FunctLocal>();
        }

        public IList<FunctLocal> Locals { get; private set; }
    }

    sealed class TableDirective : Directive
    {
        public TableDirective(AsmExpr size)
        {
            this.Size = size;
        }

        public AsmExpr Size { get; private set; }
    }

    sealed class EndtDirective : Directive
    {
    }

    sealed class VocbegDirective : Directive
    {
        public VocbegDirective(AsmExpr recordSize, AsmExpr keySize)
        {
            this.RecordSize = recordSize;
            this.KeySize = keySize;
        }

        public AsmExpr RecordSize { get; private set; }
        public AsmExpr KeySize { get; private set; }
    }

    sealed class VocendDirective : Directive
    {
    }

    abstract class DataDirective : Directive
    {
        public IList<AsmExpr> Elements { get; } = new List<AsmExpr>();
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
        }

        public string Name { get; set; }

        public IList<AsmExpr> Operands { get; } = new List<AsmExpr>();

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

        public bool UsedAsInstruction => OperandCount > 0 || HasStore || HasBranch;
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

    sealed class FstrDirective : NameAndTextDirective
    {
        public FstrDirective(string name, string text)
            : base(name, text) { }
    }

    sealed class GstrDirective : NameAndTextDirective
    {
        public GstrDirective(string name, string text)
            : base(name, text) { }
    }

    sealed class StrDirective : TextDirective
    {
        public StrDirective(string text)
            : base(text) { }
    }

    sealed class LenDirective : TextDirective
    {
        public LenDirective(string text)
            : base(text) { }
    }

    sealed class StrlDirective : TextDirective
    {
        public StrlDirective(string text)
            : base(text) { }
    }

    sealed class ZwordDirective : TextDirective
    {
        public ZwordDirective(string text)
            : base(text) { }
    }

    sealed class GvarDirective : NamedDirective
    {
        public GvarDirective(string name, AsmExpr initialValue)
            : base(name)
        {
            this.InitialValue = initialValue;
        }

        public AsmExpr InitialValue { get; set; }
    }

    sealed class ObjectDirective : NamedDirective
    {
        public ObjectDirective(string name)
            : base(name) { }

        public AsmExpr Flags1 { get; set; }
        public AsmExpr Flags2 { get; set; }
        public AsmExpr Flags3 { get; set; }
        public AsmExpr Parent { get; set; }
        public AsmExpr Sibling { get; set; }
        public AsmExpr Child { get; set; }
        public AsmExpr PropTable { get; set; }
    }

    sealed class PropDirective : Directive
    {
        public PropDirective(AsmExpr size, AsmExpr prop)
        {
            this.Size = size;
            this.Prop = prop;
        }

        public AsmExpr Size { get; set; }
        public AsmExpr Prop { get; set; }
    }

    sealed class EqualsDirective : Directive
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

    abstract class NumberAndNameDebugDirective : DebugDirective
    {
        protected NumberAndNameDebugDirective(AsmExpr number, string name)
        {
            this.Number = number;
            this.Name = name;
        }

        public AsmExpr Number { get; }
        public string Name { get; }
    }

    sealed class DebugActionDirective : NumberAndNameDebugDirective
    {
        public DebugActionDirective(AsmExpr number, string name)
            : base(number, name) { }
    }

    sealed class DebugArrayDirective : NumberAndNameDebugDirective
    {
        public DebugArrayDirective(AsmExpr number, string name)
            : base(number, name) { }
    }

    sealed class DebugAttrDirective : NumberAndNameDebugDirective
    {
        public DebugAttrDirective(AsmExpr number, string name)
            : base(number, name) { }
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

    sealed class DebugFakeActionDirective : NumberAndNameDebugDirective
    {
        public DebugFakeActionDirective(AsmExpr number, string name)
            : base(number, name) { }
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

    sealed class DebugGlobalDirective : NumberAndNameDebugDirective
    {
        public DebugGlobalDirective(AsmExpr number, string name)
            : base(number, name) { }
    }

    abstract class LineDebugDirective : DebugDirective
    {
        protected LineDebugDirective(AsmExpr file, AsmExpr line, AsmExpr column)
        {
            this.TheFile = file;
            this.TheLine = line;
            this.TheColumn = column;
        }

        public AsmExpr TheFile { get; }
        public AsmExpr TheLine { get; }
        public AsmExpr TheColumn { get; }
    }

    sealed class DebugLineDirective : LineDebugDirective
    {
        public DebugLineDirective(AsmExpr file, AsmExpr line, AsmExpr column)
            : base(file, line, column) { }
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

    sealed class DebugPropDirective : NumberAndNameDebugDirective
    {
        public DebugPropDirective(AsmExpr number, string name)
            : base(number, name) { }
    }

    sealed class DebugRoutineDirective : LineDebugDirective
    {
        public DebugRoutineDirective(AsmExpr file, AsmExpr line, AsmExpr column,
            string name, IEnumerable<string> locals)
            : base(file, line, column)
        {
            this.Name = name;
            this.Locals = new List<string>(locals);
        }

        public string Name { get; set; }
        public IList<string> Locals { get; private set; }
    }

    sealed class DebugRoutineEndDirective : LineDebugDirective
    {
        public DebugRoutineEndDirective(AsmExpr file, AsmExpr line, AsmExpr column)
            : base(file, line, column) { }
    }

    abstract class AsmExpr : ISourceLine
    {
        public string SourceFile { get; set; }
        public int LineNum { get; set; }
    }

    abstract class TextAsmExpr : AsmExpr
    {
        protected TextAsmExpr(string text)
        {
            this.Text = text;
        }

        public string Text { get; }
    }

    sealed class NumericLiteral : TextAsmExpr
    {
        public NumericLiteral(string text)
            : base(text) { }
    }

    sealed class StringLiteral : TextAsmExpr
    {
        public StringLiteral(string text)
            : base(text) { }
    }

    sealed class SymbolExpr : TextAsmExpr
    {
        public SymbolExpr(string name)
            : base(name) { }
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
}
