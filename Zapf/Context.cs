/* Copyright 2010-2014 Jesse McGrew
 * 
 * This file is part of ZAPF.
 * 
 * ZAPF is free software: you can redistribute it and/or modify it
 * under the terms of the GNU General Public License as published by
 * the Free Software Foundation, either version 3 of the License, or
 * (at your option) any later version.
 * 
 * ZAPF is distributed in the hope that it will be useful, but
 * WITHOUT ANY WARRANTY; without even the implied warranty of
 * MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the GNU
 * General Public License for more details.
 * 
 * You should have received a copy of the GNU General Public License
 * along with ZAPF.  If not, see <http://www.gnu.org/licenses/>.
 */

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.IO;

namespace Zapf
{
    delegate Stream OpenFileDelegate(string filename, bool writing);
    delegate bool FileExistsDelegate(string filename);

    class Context
    {
        public bool Quiet, InformMode, ListAddresses, AbbreviateMode, XmlDebugMode;
        public string InFile, OutFile, DebugFile;
        public string Creator = "ZAPF", Serial;
        public byte ZVersion, ZFlags;

        public int ErrorCount;

        public Dictionary<string, KeyValuePair<ushort, ZOpAttribute>> OpcodeDict;
        public StringEncoder StringEncoder;
        public AbbrevFinder AbbrevFinder;

        public Dictionary<string, Symbol> LocalSymbols;
        public Dictionary<string, Symbol> GlobalSymbols;
        public List<Fixup> Fixups;

        public IDebugFileWriter DebugWriter;
        public Dictionary<string, Symbol> DebugFileMap;

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

        private Stream stream;
        private Stream prevStream;
        private int position;
        private int globalVarCount, objectCount;
        private Stack<string> fileStack;
        private int vocabStart, vocabRecSize, vocabKeySize;

        private int reassemblyNodeIndex = -1, reassemblyPosition = -1, reassemblyAbbrevPos = 0;
        private Symbol reassemblySymbol;
        private Dictionary<string, bool> reassemblyLabels;

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
        }

        public void WriteByte(byte b)
        {
            position++;

            if (stream != null)
                stream.WriteByte(b);
        }

        public void WriteByte(Symbol sym)
        {
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

        public void WriteWord(Symbol sym)
        {
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

        public byte ReadByte()
        {
            if (stream == null)
                throw new InvalidOperationException("Object file is closed");

            position++;

            return (byte)stream.ReadByte();
        }

        public void WriteZString(string str, bool withLength)
        {
            WriteZString(str, withLength, false);
        }

        public void WriteZString(string str, bool withLength, bool noAbbrevs)
        {
            byte[] zstr = StringEncoder.Encode(str, noAbbrevs);
            if (FinalPass && AbbreviateMode)
                AbbrevFinder.AddText(str);

            if (withLength)
                WriteByte((byte)(zstr.Length / 2));

            position += zstr.Length;
            if (stream != null)
                stream.Write(zstr, 0, zstr.Length);
        }

        public void WriteZStringLength(string str)
        {
            byte[] zstr = StringEncoder.Encode(str);
            WriteByte((byte)(zstr.Length / 2));
        }

        public int ZWordChars
        {
            get { return (ZVersion >= 4) ? 9 : 6; }
        }

        public void WriteZWord(string str)
        {
            byte[] zstr = StringEncoder.Encode(str, ZWordChars, true);
            position += zstr.Length;

            if (stream != null)
                stream.Write(zstr, 0, zstr.Length);
        }

        public bool AtVocabRecord
        {
            get { return InVocab && (position - vocabStart) % vocabRecSize == 0; }
        }

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

        public void LeaveVocab()
        {
            if (InVocab)
            {
                byte[] buffer = ((MemoryStream)stream).GetBuffer();
                int bufLen = (int)stream.Length;

                stream = prevStream;
                prevStream = null;

                // sort vocab words
                // we use an insertion sort because ZILF's vocab table is mostly sorted already
                int records = bufLen / vocabRecSize;
                byte[] temp = new byte[vocabRecSize];

                int[] newIndex = new int[records];
                newIndex[0] = 0;

                for (int i = 1; i < records; i++)
                {
                    if (VocabCompare(buffer, i - 1, i) > 0)
                    {
                        int home = VocabSearch(buffer, i - 1, i);
                        Array.Copy(buffer, i * vocabRecSize, temp, 0, vocabRecSize);
                        Array.Copy(buffer, home * vocabRecSize, buffer, (home + 1) * vocabRecSize,
                            (i - home) * vocabRecSize);
                        Array.Copy(temp, 0, buffer, home * vocabRecSize, vocabRecSize);

                        for (int j = 0; j < i; j++)
                            if (newIndex[j] >= home && newIndex[j] < i)
                                newIndex[j]++;
                        newIndex[i] = home;
                    }
                    else
                    {
                        newIndex[i] = i;
                    }
                }

                // update global labels that point to vocab words
                foreach (Symbol sym in GlobalSymbols.Values)
                {
                    if (sym.Type == SymbolType.Label && sym.Value >= vocabStart && sym.Value < position)
                    {
                        int offset = sym.Value - vocabStart;
                        if (offset % vocabRecSize == 0)
                            sym.Value = vocabStart + newIndex[offset / vocabRecSize] * vocabRecSize;
                    }
                }

                if (stream != null)
                    stream.Write(buffer, 0, bufLen);
            }

            InVocab = false;

            vocabStart = -1;
            vocabRecSize = 0;
            vocabKeySize = 0;
        }

        private int VocabSearch(byte[] buffer, int numRecs, int keyRec)
        {
            int start = 0, end = numRecs - 1;
            while (start <= end)
            {
                int mid = (start + end) / 2;
                int diff = VocabCompare(buffer, keyRec, mid);
                if (diff == 0)
                    return mid;
                else if (diff < 0)
                    end = mid - 1;
                else
                    start = mid + 1;
            }
            return start;
        }

        private int VocabCompare(byte[] buffer, int idx1, int idx2)
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

        public string CurrentFile
        {
            get { return fileStack.Peek(); }
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
            if (stream != null)
            {
                stream.Close();
                stream = null;
            }
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
            if (DebugWriter != null)
            {
                DebugWriter.Close();
                DebugWriter = null;
            }
        }

        public bool IsDebugFileOpen
        {
            get { return DebugWriter != null; }
        }

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
            get { return position; }
            set
            {
                position = value;

                if (stream != null)
                    stream.Seek(value, SeekOrigin.Begin);
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

        public void BeginReassemblyScope(int nodeIndex, Symbol symbol)
        {
            reassemblyLabels.Clear();
            reassemblyNodeIndex = nodeIndex;
            reassemblyPosition = position;
            reassemblyAbbrevPos = AbbrevFinder.Position;
            reassemblySymbol = symbol;
        }

        public bool CausesReassembly(string label)
        {
            return reassemblyLabels.ContainsKey(label);
        }

        public bool InReassemblyScope
        {
            get { return reassemblyPosition != -1; }
        }

        public void MarkUnknownBranch(string label)
        {
            reassemblyLabels[label] = true;
        }

        public int Reassemble(string curLabel)
        {
            // define the current label, which is the one causing us to reassemble
            Symbol sym;
            if (LocalSymbols.TryGetValue(curLabel, out sym) == true)
                sym.Value = position;
            else
                LocalSymbols.Add(curLabel, new Symbol(curLabel, SymbolType.Label, position));

            // save labels as phantoms, wipe all other local symbols
            Queue<string> goners = new Queue<string>();

            foreach (Symbol i in LocalSymbols.Values)
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

        public void AddGlobalVar(string name)
        {
            int num = 16 + globalVarCount++;

            Symbol sym;
            if (GlobalSymbols.TryGetValue(name, out sym) == false)
            {
                sym = new Symbol(name, SymbolType.Variable, num);
                GlobalSymbols.Add(name, sym);
            }
            else if (!sym.Phantom)
                Errors.ThrowSerious("global redefined: " + name);
            else if (sym.Value != num)
                Errors.ThrowSerious("global {0} seems to have moved: was {1}, now {2}", name, sym.Value, num);
            else
                sym.Phantom = false;
        }

        public void AddObject(string name)
        {
            int num = 1 + objectCount++;

            Symbol sym;
            if (GlobalSymbols.TryGetValue(name, out sym) == false)
            {
                sym = new Symbol(name, SymbolType.Object, num);
                GlobalSymbols.Add(name, sym);
            }
            else if (!sym.Phantom)
                Errors.ThrowSerious("object redefined: " + name);
            else if (sym.Value != num)
                Errors.ThrowFatal("object {0} seems to have moved: was {1}, now {2}", name, sym.Value, num);
            else
                sym.Phantom = false;
        }

        public void CheckForUndefinedSymbols()
        {
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

            foreach (Symbol sym in GlobalSymbols.Values)
                sym.Phantom = true;

            globalVarCount = 0;
            objectCount = 0;
        }

        public void HandleSeriousError(SeriousError ser)
        {
            ErrorCount++;

            if (ser.Node != null)
                Console.Error.Write("line {0}: ", ser.Node.LineNum);

            Console.Error.WriteLine("error: {0}", ser.Message);
        }

        public void HandleFatalError(FatalError fer)
        {
            if (fer.Node != null)
                Console.Error.Write("line {0}: ", fer.Node.LineNum);

            Console.Error.WriteLine("fatal error: {0}", fer.Message);
        }

        public Stream OpenFile(string filename, bool writing)
        {
            var intercept = this.InterceptOpenFile;
            if (intercept != null)
                return intercept(filename, writing);

            return new FileStream(
                filename,
                writing ? FileMode.Create : FileMode.Open,
                writing ? FileAccess.ReadWrite : FileAccess.Read);
        }

        public bool FileExists(string filename)
        {
            var intercept = this.InterceptFileExists;
            if (intercept != null)
                return intercept(filename);

            return File.Exists(filename);
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
        public string Name;
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
            this.Type = SymbolType.Constant;
            this.Value = value;
        }

        public Symbol(string name, SymbolType type, int value)
        {
            this.Name = name;
            this.Type = type;
            this.Value = value;
        }
    }

    class Fixup
    {
        private readonly string symbol;
        private int location;

        public Fixup(string symbol)
        {
            this.symbol = symbol;
        }

        public string Symbol
        {
            get { return symbol; }
        }

        public int Location
        {
            get { return location; }
            set { location = value; }
        }
    }
}
