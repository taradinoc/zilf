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

using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using JetBrains.Annotations;
using Zilf.Common.StringEncoding;
using System.Diagnostics.Contracts;

namespace Zapf
{
    delegate Stream OpenFileDelegate(string filename, bool writing);
    delegate bool FileExistsDelegate(string filename);

    class Context : IErrorSink, IDisposable
    {
        public bool Quiet, InformMode, ListAddresses, AbbreviateMode, XmlDebugMode;
        public string InFile, OutFile, DebugFile;
        public string Creator = "ZAPF", Serial;
        public byte ZVersion, ZFlags;
        public ushort ZFlags2;
        public short? Release;

        public int FunctionsOffset, StringsOffset;

        public int ErrorCount, WarningCount;

        [CanBeNull]
        public Dictionary<string, KeyValuePair<ushort, ZOpAttribute>> OpcodeDict;

        [NotNull]
        public StringEncoder StringEncoder;

        [NotNull]
        public readonly AbbrevFinder AbbrevFinder;

        [NotNull]
        public readonly Dictionary<string, Symbol> LocalSymbols;

        [NotNull]
        public readonly Dictionary<string, Symbol> GlobalSymbols;

        [NotNull]
        public readonly List<Fixup> Fixups;

        [CanBeNull]
        public IDebugFileWriter DebugWriter;

        [NotNull]
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
        public bool InVocab;

        public OpenFileDelegate InterceptOpenFile;
        public FileExistsDelegate InterceptFileExists;

        char? LanguageEscapeChar { get; set; }

        [NotNull]
        IDictionary<char, char> LanguageSpecialChars { get; }

        Stream stream;
        Stream prevStream;
        int position;
        int globalVarCount, objectCount;

        [NotNull]
        readonly Stack<string> fileStack;

        int vocabStart, vocabRecSize, vocabKeySize;

        int reassemblyNodeIndex = -1, reassemblyPosition = -1, reassemblyAbbrevPos;
        Symbol reassemblySymbol;

        [NotNull]
        readonly Dictionary<string, bool> reassemblyLabels;

        public Context()
        {
            StringEncoder = new StringEncoder();
            AbbrevFinder = new AbbrevFinder();

            LocalSymbols = new Dictionary<string, Symbol>(25);
            GlobalSymbols = new Dictionary<string, Symbol>(200);
            Fixups = new List<Fixup>(200);
            DebugFileMap = new Dictionary<string, Symbol>();

            fileStack = new Stack<string>();
            reassemblyLabels = new Dictionary<string, bool>();

            ZVersion = Program.DEFAULT_ZVERSION;

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

            string stackName = InformMode ? "sp" : "STACK";
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
        public void WriteByte([NotNull] Symbol sym)
        {
            Contract.Requires(sym != null);
            if (sym.Type == SymbolType.Unknown)
            {
                if (FinalPass)
                    Errors.ThrowSerious("undefined symbol");
                else
                    WriteByte(0);
            }
            else
                WriteByte((byte)sym.Value);
        }

        public void WriteWord(ushort w)
        {
            position += 2;

            if (stream != null)
            {
                stream.WriteByte((byte)(w >> 8));
                stream.WriteByte((byte)w);
            }
        }

        /// <exception cref="SeriousError"><paramref name="sym"/> is undefined.</exception>
        public void WriteWord([NotNull] Symbol sym)
        {
            Contract.Requires(sym != null);
            if (sym.Type == SymbolType.Unknown)
            {
                if (FinalPass)
                    Errors.ThrowSerious("undefined symbol");
                else
                    WriteWord(0);
            }
            else
                WriteWord((ushort)sym.Value);
        }

        /// <exception cref="InvalidOperationException">The object file is closed.</exception>
        public byte ReadByte()
        {
            if (stream == null)
                throw new InvalidOperationException("Object file is closed");

            position++;

            return (byte)stream.ReadByte();
        }

        public void WriteZString([NotNull] string str, bool withLength)
        {
            Contract.Requires(str != null);
            WriteZString(str, withLength, StringEncoderMode.Normal);
        }

        public void WriteZString([NotNull] string str, bool withLength, StringEncoderMode mode)
        {
            Contract.Requires(str != null);
            MaybeProcessEscapeChars(ref str);

            var zstr = StringEncoder.Encode(str, mode);
            if (FinalPass && AbbreviateMode)
                AbbrevFinder.AddText(str);

            if (withLength)
                WriteByte((byte)(zstr.Length / 2));

            position += zstr.Length;
            stream?.Write(zstr, 0, zstr.Length);
        }

        void MaybeProcessEscapeChars([NotNull] ref string str)
        {
            Contract.Requires(str != null);

            // ReSharper disable once ConditionIsAlwaysTrueOrFalse      // false alarm!
            if (!(LanguageEscapeChar is char escape) || str.IndexOf((char)LanguageEscapeChar) < 0)
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

        public void WriteZStringLength(string str)
        {
            var zstr = StringEncoder.Encode(str);
            WriteByte((byte)(zstr.Length / 2));
        }

        public int ZWordChars => ZVersion >= 4 ? 9 : 6;

        public void WriteZWord(string str)
        {
            MaybeProcessEscapeChars(ref str);
            
            var zstr = StringEncoder.Encode(str, ZWordChars, StringEncoderMode.NoAbbreviations);
            position += zstr.Length;

            stream?.Write(zstr, 0, zstr.Length);
        }

        public bool AtVocabRecord => InVocab && (position - vocabStart) % vocabRecSize == 0;

        public void EnterVocab(int recSize, int keySize)
        {
            if (!InVocab)
            {
                prevStream = stream;
                stream = new MemoryStream();
            }

            InVocab = true;

            vocabStart = position;
            vocabRecSize = recSize;
            vocabKeySize = keySize;
        }

        public void LeaveVocab(ISourceLine src)
        {
            if (InVocab)
            {
                // restore stream
                var buffer = ((MemoryStream)stream).GetBuffer();
                var bufLen = (int)stream.Length;

                stream = prevStream;
                prevStream = null;
                InVocab = false;

                // sort vocab words
                // we use an insertion sort because ZILF's vocab table is mostly sorted already
                int records = bufLen / vocabRecSize;
                var temp = new byte[vocabRecSize];

                var newIndexes = new int[records];
                newIndexes[0] = 0;

                for (int i = 1; i < records; i++)
                {
                    if (VocabCompare(buffer, i - 1, i) > 0)
                    {
                        var home = VocabSearch(buffer, i - 1, i);
                        Array.Copy(buffer, i * vocabRecSize, temp, 0, vocabRecSize);
                        Array.Copy(buffer, home * vocabRecSize, buffer, (home + 1) * vocabRecSize,
                            (i - home) * vocabRecSize);
                        Array.Copy(temp, 0, buffer, home * vocabRecSize, vocabRecSize);

                        for (int j = 0; j < i; j++)
                            if (newIndexes[j] >= home && newIndexes[j] < i)
                                newIndexes[j]++;
                        newIndexes[i] = home;
                    }
                    else
                    {
                        newIndexes[i] = i;
                    }
                }

                // update global labels that point to vocab words
                foreach (var sym in GlobalSymbols.Values)
                {
                    if (sym.Type == SymbolType.Label && sym.Value >= vocabStart && sym.Value < position)
                    {
                        sym.Value = MapVocabAddress(sym.Value, newIndexes);
                    }
                }

                if (FinalPass)
                {
                    // check for collisions
                    for (int i = 1; i < records; i++)
                    {
                        if (VocabCompare(buffer, i - 1, i) == 0)
                        {
                            Errors.Warn(this, src, "vocab collision between {0} and {1}", VocabLabel(i - 1), VocabLabel(i));
                        }
                    }
                }

                stream?.Write(buffer, 0, bufLen);

                // apply fixups
                var vocabEnd = position;
                var goners = new HashSet<Fixup>();

                foreach (var f in Fixups)
                {
                    if (f.Location >= vocabStart && f.Location < vocabEnd)
                    {
                        if (GlobalSymbols.TryGetValue(f.Symbol, out var sym) &&
                            sym.Type == SymbolType.Label &&
                            sym.Value >= vocabStart && sym.Value < vocabEnd)
                        {
                            Position = MapVocabAddress(f.Location, newIndexes);
                            WriteWord((ushort)sym.Value);
                        }

                        goners.Add(f);
                    }
                }

                Fixups.RemoveAll(f => goners.Contains(f));
                Position = vocabEnd;
            }

            vocabStart = -1;
            vocabRecSize = 0;
            vocabKeySize = 0;
        }

        int MapVocabAddress(int oldAddress, [NotNull] int[] newIndexes)
        {
            Contract.Requires(newIndexes != null);
            var oldOffsetFromVocab = oldAddress - vocabStart;
            var oldIndex = oldOffsetFromVocab / vocabRecSize;
            var offsetWithinEntry = oldOffsetFromVocab % vocabRecSize;
            var newIndex = newIndexes[oldIndex];
            return vocabStart + (newIndex * vocabRecSize) + offsetWithinEntry;
        }

        int VocabSearch(byte[] buffer, int numRecs, int keyRec)
        {
            int start = 0, end = numRecs - 1;
            while (start <= end)
            {
                int mid = (start + end) / 2;
                var diff = VocabCompare(buffer, keyRec, mid);
                if (diff == 0)
                    return mid;

                if (diff < 0)
                    end = mid - 1;
                else
                    start = mid + 1;
            }
            return start;
        }

        int VocabCompare(byte[] buffer, int idx1, int idx2)
        {
            idx1 *= vocabRecSize;
            idx2 *= vocabRecSize;

            for (int i = 0; i < vocabKeySize; i++)
            {
                int diff = buffer[idx1 + i] - buffer[idx2 + i];
                if (diff != 0)
                    return diff;
            }

            return 0;
        }

        [CanBeNull]
        string VocabLabel(int index)
        {
            foreach (Symbol sym in GlobalSymbols.Values)
            {
                if (sym.Type == SymbolType.Label && sym.Value >= vocabStart && sym.Value < position)
                {
                    int offset = sym.Value - vocabStart;
                    if (offset % vocabRecSize == 0 && offset / vocabRecSize == index)
                        return sym.Name;
                }
            }

            return "index " + index;
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
            if (Path.GetExtension(OutFile) == ".z#")
                OutFile = Path.ChangeExtension(OutFile, ".z" + ZVersion);

            position = 0;
            stream = OpenFile(OutFile, true);
        }

        public void CloseOutput()
        {
            stream?.Close();
            stream = null;
        }

        public void OpenDebugFile()
        {
            var debugStream = OpenFile(DebugFile, true);
            if (XmlDebugMode)
            {
                DebugWriter = new XmlDebugFileWriter(debugStream);
            }
            else
            {
                DebugWriter = new BinaryDebugFileWriter(debugStream);
            }
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

        /// <exception cref="InvalidOperationException" accessor="set">A vocab section is currently being written.</exception>
        public int Position
        {
            get => position;

            set
            {
                position = value;

                if (stream != null)
                {
                    if (InVocab)
                        throw new InvalidOperationException("Cannot seek while inside vocab section");

                    stream.Seek(value, SeekOrigin.Begin);
                }
            }
        }

        public int PackingDivisor
        {
            get
            {
                switch (ZVersion)
                {
                    case 1:
                    case 2:
                    case 3:
                        return 2;
                    case 4:
                    case 5:
                    case 6:
                    case 7:
                        return 4;
                    case 8:
                        return 8;
                    default:
                        throw new NotImplementedException();
                }
            }
        }

        public int HeaderLengthDivisor
        {
            get
            {
                switch (ZVersion)
                {
                    case 1:
                    case 2:
                    case 3:
                        return 2;
                    case 4:
                    case 5:
                        return 4;
                    case 6:
                    case 7:
                    case 8:
                        return 8;
                    default:
                        throw new NotImplementedException();
                }
            }
        }

        public bool UsePackingOffsets => ZVersion == 6 || ZVersion == 7;

        public int PackingOffsetDivisor => 8;

        public void BeginReassemblyScope(int nodeIndex, Symbol symbol)
        {
            reassemblyLabels.Clear();
            reassemblyNodeIndex = nodeIndex;
            reassemblyPosition = position;
            reassemblyAbbrevPos = AbbrevFinder.Position;
            reassemblySymbol = symbol;
        }

        public bool CausesReassembly([NotNull] string label)
        {
            Contract.Requires(label != null);
            return reassemblyLabels.ContainsKey(label);
        }

        public bool InReassemblyScope => reassemblyPosition != -1;

        public void MarkUnknownBranch([NotNull] string label)
        {
            Contract.Requires(label != null);
            reassemblyLabels[label] = true;
        }

        public int Reassemble([NotNull] string curLabel)
        {
            // define the current label, which is the one causing us to reassemble
            Contract.Requires(curLabel != null);
            if (LocalSymbols.TryGetValue(curLabel, out var sym))
                sym.Value = position;
            else
                LocalSymbols.Add(curLabel, new Symbol(curLabel, SymbolType.Label, position));

            // save labels as phantoms, wipe all other local symbols
            var goners = new Queue<string>();

            foreach (var i in LocalSymbols.Values)
            {
                if (i.Type == SymbolType.Label)
                    i.Phantom = true;
                else
                    goners.Enqueue(i.Name);
            }

            while (goners.Count > 0)
                LocalSymbols.Remove(goners.Dequeue());

            // make function symbol into a phantom
            reassemblySymbol.Phantom = true;

            // clean up reassembly state and rewind to the beginning of the scope
            reassemblyLabels.Clear();
            Position = reassemblyPosition;
            AbbrevFinder.Rollback(reassemblyAbbrevPos);
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
            }
        }

        /// <exception cref="SeriousError">The global variable moved unexpectedly between passes.</exception>
        public void AddGlobalVar([NotNull] string name)
        {
            Contract.Requires(name != null);
            int num = 16 + globalVarCount++;

            if (GlobalSymbols.TryGetValue(name, out var sym) == false)
            {
                sym = new Symbol(name, SymbolType.Variable, num);
                GlobalSymbols.Add(name, sym);
            }
            else if (sym.Phantom && sym.Type == SymbolType.Variable)
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

        /// <exception cref="FatalError">The object moved unexpectedly between passes.</exception>
        /// <exception cref="SeriousError">The object was redefined.</exception>
        public void AddObject([NotNull] string name)
        {
            Contract.Requires(name != null);
            int num = 1 + objectCount++;

            if (GlobalSymbols.TryGetValue(name, out var sym) == false)
            {
                sym = new Symbol(name, SymbolType.Object, num);
                GlobalSymbols.Add(name, sym);
            }
            else if (sym.Phantom && sym.Type == SymbolType.Object)
            {
                if (sym.Value != num)
                {
                    Errors.ThrowFatal("object {0} seems to have moved: was {1}, now {2}", name, sym.Value, num);
                }

                sym.Phantom = false;
            }
            else if (sym.Type == SymbolType.Unknown)
            {
                sym.Type = SymbolType.Object;
                sym.Value = num;
                MeasureAgain = true;
            }
            else
            {
                Errors.ThrowSerious("object redefined: " + name);
            }
        }

        public void CheckLimits()
        {
            if (globalVarCount > 240)
                Errors.Serious(this, "too many global variables: {0} defined, only 240 allowed", globalVarCount);

            var maxObjects = (ZVersion == 3 ? 255 : 65535);
            if (objectCount > maxObjects)
                Errors.Serious(this, "too many objects: {0} defined, only {1} allowed", objectCount, maxObjects);
        }

        public void CheckForUndefinedSymbols()
        {
            // define FOFF and SOFF for V6-7
            if (UsePackingOffsets)
            {
                GlobalSymbols["FOFF"] = new Symbol("FOFF", SymbolType.Constant, FunctionsOffset / PackingOffsetDivisor);
                GlobalSymbols["SOFF"] = new Symbol("SOFF", SymbolType.Constant, StringsOffset / PackingOffsetDivisor);
            }

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
            StringEncoder = new StringEncoder();

            foreach (var sym in GlobalSymbols.Values)
                sym.Phantom = true;

            globalVarCount = 0;
            objectCount = 0;

            FunctionsOffset = 0;
            StringsOffset = 0;
        }

        public void HandleWarning([NotNull] Warning warning)
        {
            Contract.Requires(warning != null);
            WarningCount++;

            if (warning.Node != null)
                Console.Error.Write("{0}:{1}: ", warning.Node.SourceFile, warning.Node.LineNum);

            Console.Error.WriteLine("warning: {0}", warning.Message);
        }

        public void HandleSeriousError([NotNull] SeriousError ser)
        {
            Contract.Requires(ser != null);
            ErrorCount++;

            if (ser.Node != null)
                Console.Error.Write("{0}:{1}: ", ser.Node.SourceFile, ser.Node.LineNum);

            Console.Error.WriteLine("error: {0}", ser.Message);
        }

        public void HandleFatalError([NotNull] FatalError fer)
        {
            Contract.Requires(fer != null);
            ErrorCount++;

            if (fer.Node != null)
                Console.Error.Write("{0}:{1}: ", fer.Node.SourceFile, fer.Node.LineNum);

            Console.Error.WriteLine("fatal error: {0}", fer.Message);
        }

        public Stream OpenFile(string filename, bool writing)
        {
            var intercept = InterceptOpenFile;
            if (intercept != null)
                return intercept(filename, writing);

            return new FileStream(
                filename,
                writing ? FileMode.Create : FileMode.Open,
                writing ? FileAccess.ReadWrite : FileAccess.Read);
        }

        public bool FileExists(string filename)
        {
            var intercept = InterceptFileExists;
            return intercept?.Invoke(filename) ?? File.Exists(filename);
        }

        public string FindInsertedFile(string name)
        {
            if (FileExists(name))
                return name;

            string search = name + ".zap";
            if (FileExists(search))
                return search;

            search = name + ".xzap";
            if (FileExists(search))
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

        [ContractInvariantMethod]
        [SuppressMessage("Microsoft.Performance", "CA1822: MarkMembersAsStatic", Justification = "Required for code contracts.")]
        [Conditional("CONTRACTS_FULL")]
        void ObjectInvariant()
        {
            Contract.Invariant(StringEncoder != null);
            Contract.Invariant(AbbrevFinder != null);
            Contract.Invariant(LocalSymbols != null);
            Contract.Invariant(GlobalSymbols != null);
            Contract.Invariant(Fixups != null);
            Contract.Invariant(DebugFileMap != null);
            Contract.Invariant(fileStack != null);
            Contract.Invariant(reassemblyLabels != null);
        }
    }

    enum SymbolType
    {
        /// <summary>
        /// The symbol has not been defined.
        /// </summary>
        Unknown,
        /// <summary>
        /// The symbol is a numeric constant.
        /// </summary>
        Constant,
        /// <summary>
        /// The symbol is a local or global variable.
        /// </summary>
        Variable,
        /// <summary>
        /// The symbol is a local or global label (byte address).
        /// </summary>
        Label,
        /// <summary>
        /// The symbol is a packed function address.
        /// </summary>
        Function,
        /// <summary>
        /// The symbol is a packed string address.
        /// </summary>
        String,
        /// <summary>
        /// The symbol is an object number.
        /// </summary>
        Object,
    }

    class Symbol
    {
        /// <summary>
        /// The symbol's name in the source code.
        /// </summary>
        [CanBeNull]
        public readonly string Name;
        /// <summary>
        /// The symbol's type.
        /// </summary>
        public SymbolType Type;
        /// <summary>
        /// The symbol's value, usually a numeric constant or an address.
        /// </summary>
        public int Value;
        /// <summary>
        /// Indicates whether the symbol has a value from a previous attempt,
        /// but has not yet been defined in the current attempt.
        /// </summary>
        public bool Phantom;

        public Symbol()
        {
        }

        public Symbol(int value)
        {
            Type = SymbolType.Constant;
            Value = value;
        }

        public Symbol([CanBeNull] string name, SymbolType type, int value)
        {
            Name = name;
            Type = type;
            Value = value;
        }
    }

    class Fixup
    {
        public Fixup(string symbol)
        {
            Symbol = symbol;
        }

        public string Symbol { get; }

        public int Location { get; set; }
    }
}
