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
using System.Diagnostics.CodeAnalysis;
using JetBrains.Annotations;

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
            Name = name;
        }

        public string Name { get; }
    }

    abstract class TextDirective : Directive
    {
        protected TextDirective(string text)
        {
            Text = text;
        }

        public string Text { get; }
    }

    abstract class NameAndTextDirective : Directive
    {
        protected NameAndTextDirective(string name, string text)
        {
            Name = name;
            Text = text;
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
            InsertFileName = filename;
        }

        public string InsertFileName { get; }
    }

    sealed class NewDirective : Directive
    {
        public NewDirective(AsmExpr version)
        {
            Version = version;
        }

        public AsmExpr Version { get; }
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
            LanguageId = langId;
            EscapeChar = escapeChar;
        }

        public AsmExpr LanguageId { get; }
        public AsmExpr EscapeChar { get; }
    }

    sealed class ChrsetDirective : Directive
    {
        public ChrsetDirective(AsmExpr alphabetNum, [NotNull] IEnumerable<AsmExpr> characters)
        {
            CharsetNum = alphabetNum;
            Characters = new List<AsmExpr>(characters);
        }

        public AsmExpr CharsetNum { get; }
        public IList<AsmExpr> Characters { get; }
    }

    struct FunctLocal
    {
        public FunctLocal(string name, AsmExpr defaultValue)
        {
            Name = name;
            DefaultValue = defaultValue;
        }

        public readonly string Name;
        public readonly AsmExpr DefaultValue;
    }

    sealed class FunctDirective : NamedDirective
    {
        public FunctDirective(string name)
            : base(name)
        {
            Locals = new List<FunctLocal>();
        }

        public IList<FunctLocal> Locals { get; }
    }

    sealed class TableDirective : Directive
    {
        public TableDirective(AsmExpr size)
        {
            Size = size;
        }

        public AsmExpr Size { get; }
    }

    sealed class EndtDirective : Directive
    {
    }

    sealed class VocbegDirective : Directive
    {
        public VocbegDirective(AsmExpr recordSize, AsmExpr keySize)
        {
            RecordSize = recordSize;
            KeySize = keySize;
        }

        public AsmExpr RecordSize { get; }
        public AsmExpr KeySize { get; }
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
            Name = name;
        }

        public string Name { get; }

        public IList<AsmExpr> Operands { get; } = new List<AsmExpr>();

        public string StoreTarget { get; set; }

        public bool? BranchPolarity { get; set; }
        public string BranchTarget { get; set; }
    }

    sealed class BareSymbolLine : AsmLine
    {
        public BareSymbolLine(string text)
        {
            Text = text;
        }

        public string Text { get; }

        public int OperandCount { get; set; }
        public bool HasStore { get; set; }
        public bool HasBranch { get; set; }

        public bool UsedAsInstruction => OperandCount > 0 || HasStore || HasBranch;
    }

    sealed class LocalLabel : AsmLine
    {
        public LocalLabel(string name)
        {
            Name = name;
        }

        public string Name { get; }
    }

    sealed class GlobalLabel : AsmLine
    {
        public GlobalLabel(string name)
        {
            Name = name;
        }

        public string Name { get; }
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
            InitialValue = initialValue;
        }

        public AsmExpr InitialValue { get; }
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
            Size = size;
            Prop = prop;
        }

        public AsmExpr Size { get; }
        public AsmExpr Prop { get; }
    }

    sealed class EqualsDirective : Directive
    {
        public EqualsDirective(string left, AsmExpr right)
        {
            Left = left;
            Right = right;
        }

        public string Left { get; }
        public AsmExpr Right { get; }
    }

    abstract class DebugDirective : AsmLine
    {
    }

    abstract class NumberAndNameDebugDirective : DebugDirective
    {
        protected NumberAndNameDebugDirective(AsmExpr number, string name)
        {
            Number = number;
            Name = name;
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

    [SuppressMessage("ReSharper", "ClassNeverInstantiated.Global")]
    sealed class DebugClassDirective : DebugDirective
    {
        public DebugClassDirective(string name, AsmExpr startFile, AsmExpr startLine, AsmExpr startColumn, AsmExpr endFile, AsmExpr endLine, AsmExpr endColumn)
        {
            Name = name;
            StartFile = startFile;
            StartLine = startLine;
            StartColumn = startColumn;
            EndFile = endFile;
            EndLine = endLine;
            EndColumn = endColumn;
        }

        public string Name { get; }
        public AsmExpr StartFile { get; }
        public AsmExpr StartLine { get; }
        public AsmExpr StartColumn { get; }
        public AsmExpr EndFile { get; }
        public AsmExpr EndLine { get; }
        public AsmExpr EndColumn { get; }
    }

    [SuppressMessage("ReSharper", "ClassNeverInstantiated.Global")]
    sealed class DebugFakeActionDirective : NumberAndNameDebugDirective
    {
        public DebugFakeActionDirective(AsmExpr number, string name)
            : base(number, name) { }
    }

    sealed class DebugFileDirective : DebugDirective
    {
        public DebugFileDirective(AsmExpr number, string includeName, string actualName)
        {
            Number = number;
            IncludeName = includeName;
            ActualName = actualName;
        }

        public AsmExpr Number { get; }
        public string IncludeName { get; }
        public string ActualName { get; }
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
            TheFile = file;
            TheLine = line;
            TheColumn = column;
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

    [SuppressMessage("ReSharper", "ClassNeverInstantiated.Global")]
    sealed class DebugMapDirective : DebugDirective
    {
        public DebugMapDirective(string key, AsmExpr value)
        {
            Key = key;
            Value = value;
        }

        public string Key { get; }
        public AsmExpr Value { get; }
    }

    sealed class DebugObjectDirective : DebugDirective
    {
        public DebugObjectDirective(AsmExpr number, string name,
            AsmExpr startFile, AsmExpr startLine, AsmExpr startColumn,
            AsmExpr endFile, AsmExpr endLine, AsmExpr endColumn)
        {
            Number = number;
            Name = name;
            StartFile = startFile;
            StartLine = startLine;
            StartColumn = startColumn;
            EndFile = endFile;
            EndLine = endLine;
            EndColumn = endColumn;
        }

        public AsmExpr Number { get; }
        public string Name { get; }
        public AsmExpr StartFile { get; }
        public AsmExpr StartLine { get; }
        public AsmExpr StartColumn { get; }
        public AsmExpr EndFile { get; }
        public AsmExpr EndLine { get; }
        public AsmExpr EndColumn { get; }
    }

    sealed class DebugPropDirective : NumberAndNameDebugDirective
    {
        public DebugPropDirective(AsmExpr number, string name)
            : base(number, name) { }
    }

    sealed class DebugRoutineDirective : LineDebugDirective
    {
        public DebugRoutineDirective(AsmExpr file, AsmExpr line, AsmExpr column,
            string name, [NotNull] IEnumerable<string> locals)
            : base(file, line, column)
        {
            Name = name;
            Locals = new List<string>(locals);
        }

        public string Name { get; }
        public IList<string> Locals { get; }
    }

    sealed class DebugRoutineEndDirective : LineDebugDirective
    {
        public DebugRoutineEndDirective(AsmExpr file, AsmExpr line, AsmExpr column)
            : base(file, line, column) { }
    }

    abstract class AsmExpr : ISourceLine
    {
        public string SourceFile => null;
        public int LineNum => 0;
    }

    abstract class TextAsmExpr : AsmExpr
    {
        protected TextAsmExpr(string text)
        {
            Text = text;
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
            Left = left;
            Right = right;
        }

        public AsmExpr Left { get; }
        public AsmExpr Right { get; }
    }

    sealed class QuoteExpr : AsmExpr
    {
        public QuoteExpr(AsmExpr inner)
        {
            Inner = inner;
        }

        public AsmExpr Inner { get; }
    }
}
