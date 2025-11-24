/* Copyright 2010-2025 Tara McGrew
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
using Zilf.Common.StringEncoding;
using Gaze.Parsing.Diagnostics;
using Gaze.Parsing.Instructions;
using Zilf.Common;

namespace Gaze
{
    public enum InstructionForm
    {
        TwoOp,
        Var,
    }

    public enum OperandEncoding
    {
        Byte,
        Word,
        Variable,
    }

    public delegate IDebugFileWriter GetDebugWriterDelegate(Stream stream);

    public sealed class Context : IErrorSink, IDisposable
    {
        public bool Quiet, ListAddresses, AbbreviateMode;
        public string? InFile, OutFile, DebugFile;
        public string? Creator = "GAZE";
        public string? Serial;
        public short? Release;

        public int ErrorCount, WarningCount;

        public Dictionary<string, (int opcode, GOpAttribute attr)>? OpcodeDict;

        public readonly AbbrevFinder AbbrevFinder;

        public readonly Dictionary<string, Symbol> LocalSymbols;

        public readonly Dictionary<string, Symbol> GlobalSymbols;

        public readonly List<Fixup> Fixups;

        public IDebugFileWriter? DebugWriter;

        public readonly Dictionary<string, Symbol> DebugFileMap;

        /// <summary>
        /// If true, a reference to an undefined global symbol is an error,
        /// and expensive statistics may be collected.
        /// </summary>
        public bool FinalPass;

        /// <summary>
        /// If true, another measuring pass is needed because labels have moved.
        /// </summary>
        public bool MeasureAgain;

        public int? TableStart, TableSize;

        public IFileSystem FileSystem { get; set; } = PhysicalFileSystem.Instance;

        public GetDebugWriterDelegate? InterceptGetDebugWriter;

        char? LanguageEscapeChar { get; set; }

        Dictionary<char, char> LanguageSpecialChars { get; }

        Stream? stream;
        Stream? prevStream;
        int position;
        int globalVarCount;

        readonly Stack<string> fileStack;

        /// <summary>
        /// The node index where the reassembly scope started, or -1 if none.
        /// </summary>
        int reassemblyNodeIndex = -1;
        /// <summary>
        /// The story file position where the reassembly scope started, or -1 if none.
        /// </summary>
        int reassemblyPosition = -1;
        /// <summary>
        /// The position of the <see cref="AbbrevFinder"/> at the beginning of the
        /// reassembly scope.
        /// </summary>
        int reassemblyAbbrevPos;
        /// <summary>
        /// The symbol of the function that owns the reassembly scope.
        /// </summary>
        Symbol? reassemblySymbol;

        /// <summary>
        /// The local labels that have been encountered in the current reassembly scope
        /// before their definitions.
        /// </summary>
        /// <remarks>
        /// When one of these labels is defined, we rewind to the beginning of the
        /// reassembly scope and start again using the new value.
        /// </remarks>
        readonly HashSet<string> reassemblyLabels;

        /// <summary>
        /// The global labels that have been encountered in the current reassembly scope
        /// with unexpected values, mapped to delegates that check the expected value and
        /// handle mismatches.
        /// </summary>
        /// <remarks>
        /// These are checked at the end of the reassembly scope.
        /// </remarks>
        readonly Dictionary<Symbol, Action> deferredGlobalLabelChecks;

        public Context()
        {
            AbbrevFinder = new AbbrevFinder();

            LocalSymbols = new Dictionary<string, Symbol>(25);
            GlobalSymbols = new Dictionary<string, Symbol>(200);
            Fixups = new List<Fixup>(200);
            DebugFileMap = [];

            fileStack = new Stack<string>();
            reassemblyLabels = [];
            deferredGlobalLabelChecks = [];

            LanguageSpecialChars = new Dictionary<char, char>();
        }

        public void Restart()
        {
            ErrorCount = WarningCount = 0;

            LocalSymbols.Clear();
            GlobalSymbols.Clear();
            Fixups.Clear();
            DebugFileMap.Clear();

            fileStack.Clear();
            reassemblyLabels.Clear();

            const string stackName = "stack";
            GlobalSymbols.Add(stackName, new Symbol(stackName, SymbolType.Variable, 0));

            LanguageEscapeChar = null;
            LanguageSpecialChars.Clear();
        }

        public void WriteByte(byte b)
        {
            position++;

            stream?.WriteByte(b);
        }

        /// <exception cref="SeriousError"><paramref name="sym"/> is undefined.</exception>
        public void WriteByte(Symbol sym)
        {
            switch (sym.Type)
            {
                case SymbolType.Unknown when FinalPass:
                    Errors.ThrowSerious("undefined symbol");
                    break;

                case SymbolType.Unknown:
                    WriteByte(0);
                    break;

                default:
                    WriteByte((byte)sym.Value);
                    break;
            }
        }

        public void WriteShort(ushort w)
        {
            position += 2;

            if (stream != null)
            {
                stream.WriteByte((byte)(w >> 8));
                stream.WriteByte((byte)w);
            }
        }

        public void WriteShort(Symbol sym)
        {
            switch (sym.Type)
            {
                case SymbolType.Unknown when FinalPass:
                    Errors.ThrowSerious("undefined symbol");
                    break;

                case SymbolType.Unknown:
                    WriteShort(0);
                    break;

                default:
                    WriteShort((ushort)sym.Value);
                    break;
            }
        }

        public void WriteWord(int w)
        {
            position += 4;

            if (stream != null)
            {
                stream.WriteByte((byte)((uint)w >> 24));
                stream.WriteByte((byte)((uint)w >> 16));
                stream.WriteByte((byte)((uint)w >> 8));
                stream.WriteByte((byte)w);
            }
        }

        /// <exception cref="SeriousError"><paramref name="sym"/> is undefined.</exception>
        public void WriteWord(Symbol sym)
        {
            switch (sym.Type)
            {
                case SymbolType.Unknown when FinalPass:
                    Errors.ThrowSerious("undefined symbol");
                    break;

                case SymbolType.Unknown:
                    WriteWord(0);
                    break;

                default:
                    WriteWord(sym.Value);
                    break;
            }
        }

        /// <exception cref="InvalidOperationException">The object file is closed.</exception>
        public byte ReadByte()
        {
            if (stream == null)
                throw new InvalidOperationException("Object file is closed");

            position++;

            return (byte)stream.ReadByte();
        }

        public int ReadWord()
        {
            if (stream == null)
                throw new InvalidOperationException("Object file is closed");

            position += 4;

            int b1 = stream.ReadByte();
            int b2 = stream.ReadByte();
            int b3 = stream.ReadByte();
            int b4 = stream.ReadByte();
            return (b1 << 24) | (b2 << 16) | (b3 << 8) | b4;
        }

        void MaybeProcessEscapeChars(ref string str)
        {
            // ReSharper disable once ConditionIsAlwaysTrueOrFalse      // false alarm!
            if (LanguageEscapeChar is not char escape || str.IndexOf((char)LanguageEscapeChar) < 0)
                return;

            var sb = new StringBuilder(str);

            for (int i = 0; i < sb.Length - 1; i++)
            {
                if (sb[i] != escape)
                    continue;

                var next = sb[i + 1];
                if (next == escape)
                {
                    // %% => %
                    sb.Remove(i, 1);
                }
                else
                {
                    // translate according to LanguageSpecialChars
                    if (!LanguageSpecialChars.TryGetValue(next, out var translation))
                        continue;

                    sb.Remove(i, 1);
                    sb[i] = translation;
                }
            }

            str = sb.ToString();
        }

        public void PushFile(string filename)
        {
            if (!Quiet)
                Console.Error.WriteLine("Reading {0}", filename);

            fileStack.Push(filename);
        }

        public void PopFile()
        {
            fileStack.Pop();
        }

        public void OpenOutput()
        {
            Debug.Assert(OutFile != null);

            position = 0;
            stream = FileSystem.OpenForWritingAndReading(OutFile);
        }

        public void CloseOutput()
        {
            stream?.Close();
            stream = null;
        }

        public void OpenDebugFile()
        {
            Debug.Assert(DebugFile != null);

            var debugStream = FileSystem.OpenForWriting(DebugFile);

            if (InterceptGetDebugWriter != null)
            {
                DebugWriter = InterceptGetDebugWriter(debugStream);

                if (DebugWriter != null)
                    return;
            }

            DebugWriter = new XmlDebugFileWriter(debugStream);
        }

        public void CloseDebugFile()
        {
            DebugWriter?.Close();
            DebugWriter = null;
        }

        public bool IsDebugFileOpen => DebugWriter != null;

        public byte[] GetHeader()
        {
            var op = Position;
            Position = 0;
            try
            {
                var result = new byte[64];

                for (int i = 0; i < 64; i++)
                    result[i] = ReadByte();

                return result;
            }
            finally
            {
                Position = op;
            }
        }

        public int Position
        {
            get => position;

            set
            {
                position = value;

                stream?.Seek(value, SeekOrigin.Begin);
            }
        }

        public void BeginReassemblyScope(int nodeIndex, Symbol symbol)
        {
            reassemblyLabels.Clear();
            deferredGlobalLabelChecks.Clear();
            reassemblyNodeIndex = nodeIndex;
            reassemblyPosition = position;
            reassemblyAbbrevPos = AbbrevFinder.Position;
            reassemblySymbol = symbol;
        }

        public bool CausesReassembly(string label)
        {
            return reassemblyLabels.Contains(label);
        }

        public bool InReassemblyScope => reassemblyPosition != -1;

        public void MarkUnknownBranch(string label)
        {
            reassemblyLabels.Add(label);
        }

        public void DeferGlobalLabelStabilityCheck(Symbol sym, Action checkMismatch)
        {
            deferredGlobalLabelChecks.TryAdd(sym, checkMismatch);
        }

        public int Reassemble(string curLabel)
        {
            if (LocalSymbols.TryGetValue(curLabel, out var sym))
                sym.Value = position;
            else
                LocalSymbols.Add(curLabel, new Symbol(curLabel, SymbolType.Label, position));

            // save labels as phantoms, wipe all other local symbols
            var goners = new Queue<string>();

            foreach (var i in LocalSymbols.Values)
            {
                if (i.Type == SymbolType.Label)
                {
                    i.Phantom = true;
                }
                else
                {
                    Debug.Assert(i.Name != null);
                    goners.Enqueue(i.Name);
                }
            }

            while (goners.Count > 0)
                LocalSymbols.Remove(goners.Dequeue());

            // make function symbol into a phantom
            reassemblySymbol!.Phantom = true;

            // clean up reassembly state and rewind to the beginning of the scope
            reassemblyLabels.Clear();
            deferredGlobalLabelChecks.Clear();
            Position = reassemblyPosition;
            AbbrevFinder.Rollback(reassemblyAbbrevPos);
            DebugWriter?.RestartRoutine();
            return reassemblyNodeIndex;
        }

        public void EndReassemblyScope(int nodeIndex)
        {
            if (nodeIndex != reassemblyNodeIndex)
            {
                reassemblyLabels.Clear();
                reassemblyNodeIndex = -1;
                reassemblyPosition = -1;
                reassemblyAbbrevPos = 0;
                reassemblySymbol = null;
                LocalSymbols.Clear();

                try
                {
                    foreach (var runDeferredCheck in deferredGlobalLabelChecks.Values)
                        runDeferredCheck();
                }
                finally
                {
                    deferredGlobalLabelChecks.Clear();
                }
            }
        }

        /// <exception cref="SeriousError">The global variable moved unexpectedly between passes.</exception>
        public void AddGlobalVar(string name)
        {
            int num = 16 + globalVarCount++;

            if (GlobalSymbols.TryGetValue(name, out var sym) == false)
            {
                sym = new Symbol(name, SymbolType.Variable, num);
                GlobalSymbols.Add(name, sym);
            }
            else if (sym!.Phantom && sym.Type == SymbolType.Variable)
            {
                if (sym.Value != num)
                {
                    Errors.ThrowSerious("global {0} seems to have moved: was {1}, now {2}", name, sym.Value, num);
                }

                sym.Phantom = false;
            }
            else if (sym.Type == SymbolType.Unknown)
            {
                sym.Type = SymbolType.Variable;
                sym.Value = num;
                MeasureAgain = true;
            }
            else
            {
                Errors.ThrowSerious("global redefined: " + name);
            }
        }

        public void CheckForUndefinedSymbols()
        {
            // define FLAGS and RELEASEID if needed
            void SetConstantDefault(string name, ushort value)
            {
                if (GlobalSymbols.TryGetValue(name, out var sym) && sym.Type == SymbolType.Unknown)
                {
                    sym.Type = SymbolType.Constant;
                    sym.Value = value;
                }
            }

            SetConstantDefault("FLAGS", 0);

            var releaseId = (ushort)GetHeaderValue("RELEASEID", "ZORKID", false);
            SetConstantDefault("RELEASEID", releaseId);
            SetConstantDefault("ZORKID", releaseId);

            // now look for any remaining undefined symbols
            var offenders = new HashSet<string>();

            foreach (var f in Fixups)
            {
                if (!GlobalSymbols.ContainsKey(f.Symbol) && !offenders.Contains(f.Symbol))
                {
                    Errors.Serious(this, "symbol is never defined: {0}", f.Symbol);
                    offenders.Add(f.Symbol);
                }
            }
        }

        public void ResetBetweenPasses()
        {
            Fixups.Clear();

            foreach (var sym in GlobalSymbols.Values)
                sym.Phantom = true;

            globalVarCount = 0;
        }

        public void HandleWarning(Warning warning)
        {
            WarningCount++;

            if (warning.Node != null)
                Console.Error.Write("{0}:{1}: ", warning.Node.SourceFile, warning.Node.LineNum);

            Console.Error.WriteLine("warning: {0}", warning.Message);
        }

        public void HandleSeriousError(SeriousError ser)
        {
            ErrorCount++;

            if (ser.Node != null)
                Console.Error.Write("{0}:{1}: ", ser.Node.SourceFile, ser.Node.LineNum);

            Console.Error.WriteLine("error: {0}", ser.Message);
        }

        public void HandleFatalError(FatalError fer)
        {
            ErrorCount++;

            if (fer.Node != null)
                Console.Error.Write("{0}:{1}: ", fer.Node.SourceFile, fer.Node.LineNum);

            Console.Error.WriteLine("fatal error: {0}", fer.Message);
        }

        public string? FindInsertedFile(string name)
        {
            if (FileSystem.Exists(name))
                return name;

            string search = name + ".zap";
            if (FileSystem.Exists(search))
                return search;

            search = name + ".xzap";
            if (FileSystem.Exists(search))
                return search;

            return null;
        }

        public void SetLanguage(int langId, int escapeChar)
        {
            LanguageEscapeChar = (char)escapeChar;

            LanguageSpecialChars.Clear();
            switch (langId)
            {
                case 1:
                    // German
                    LanguageSpecialChars.Add('a', 'ä');
                    LanguageSpecialChars.Add('o', 'ö');
                    LanguageSpecialChars.Add('u', 'ü');
                    LanguageSpecialChars.Add('s', 'ß');
                    LanguageSpecialChars.Add('A', 'Ä');
                    LanguageSpecialChars.Add('O', 'Ö');
                    LanguageSpecialChars.Add('U', 'Ü');
                    LanguageSpecialChars.Add('<', '«');
                    LanguageSpecialChars.Add('>', '»');
                    break;
            }
        }

        public void Dispose()
        {
            try
            {
                stream?.Dispose();
                prevStream?.Dispose();
            }
            finally
            {
                stream = null;
                prevStream = null;
            }
        }

        public int GetHeaderValue(string name, bool required) => GetHeaderValue(name, null, required);

        public int GetHeaderValue(string name1, string? name2, bool required)
        {
            if (GlobalSymbols.TryGetValue(name1, out var sym) ||
                (name2 != null && GlobalSymbols.TryGetValue(name2, out sym)))
            {
                return sym.Type switch
                {
                    SymbolType.Label or SymbolType.Function or SymbolType.Constant => sym.Value,
                    _ => 0,
                };
            }

            if (required)
                Errors.Serious(this, "required global symbol '{0}' is missing", name1);
            return 0;
        }
    }
}
