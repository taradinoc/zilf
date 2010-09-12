/* Copyright 2010 Jesse McGrew
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
    class Context
    {
        public bool Quiet, InformMode, ListAddresses, AbbreviateMode;
        public string InFile, OutFile, DebugFile;
        public string Creator = "ZAPF", Serial;
        public byte ZVersion, ZFlags;

        public int ErrorCount;

        public Dictionary<string, KeyValuePair<ushort, ZOpAttribute>> OpcodeDict;
        public StringEncoder StringEncoder;
        public AbbrevFinder AbbrevFinder;
        public BranchOptimizer BranchOptimizer;

        public Dictionary<string, Symbol> Symbols;

        public Dictionary<string, Symbol> DebugFileMap;
        public ushort NextDebugRoutine;
        public int DebugRoutinePoints = -1, DebugRoutineStart = -1;

        public int CurrentPass = 1;

        public int? TableStart, TableSize;
        public bool InVocab;

        private Stream stream, debugStream;
        private Stream prevStream;
        private int position;
        private int globalVarCount, objectCount;
        private Stack<string> fileStack;
        private int vocabStart, vocabRecSize, vocabKeySize;

        private Symbol localScope;

        public Context()
        {
            StringEncoder = new StringEncoder();
            AbbrevFinder = new AbbrevFinder();
            BranchOptimizer = new BranchOptimizer();

            Symbols = new Dictionary<string, Symbol>(1000);
            DebugFileMap = new Dictionary<string, Symbol>();

            fileStack = new Stack<string>();

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
                if (CurrentPass > 1)
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
                if (CurrentPass > 1)
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
            if (CurrentPass == 1 && AbbreviateMode)
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
                foreach (Symbol sym in Symbols.Values)
                {
                    if (sym.Type == SymbolType.GlobalLabel && sym.Value >= vocabStart && sym.Value < position)
                    {
                        int offset = sym.Value - vocabStart;
                        if (offset % vocabRecSize == 0)
                            sym.SetValue(vocabStart + newIndex[offset / vocabRecSize] * vocabRecSize, CurrentPass);
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
            stream = new FileStream(OutFile, FileMode.Create, FileAccess.ReadWrite);
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
            debugStream = new FileStream(DebugFile, FileMode.Create, FileAccess.Write);
            // debug file header
            WriteDebugWord(0xDEBF);     // magic number
            WriteDebugWord(0);          // file format
            WriteDebugWord(2001);       // creator version
        }

        public void WriteDebugByte(byte b)
        {
            if (debugStream != null)
                debugStream.WriteByte(b);
        }

        public void WriteDebugWord(ushort w)
        {
            if (debugStream != null)
            {
                debugStream.WriteByte((byte)(w >> 8));
                debugStream.WriteByte((byte)w);
            }
        }

        public void WriteDebugAddress(int a)
        {
            if (debugStream != null)
            {
                debugStream.WriteByte((byte)(a >> 16));
                debugStream.WriteByte((byte)(a >> 8));
                debugStream.WriteByte((byte)a);
            }
        }

        public void WriteDebugLineRef(byte file, ushort line, byte col)
        {
            if (debugStream != null)
            {
                debugStream.WriteByte(file);
                debugStream.WriteByte((byte)(line >> 8));
                debugStream.WriteByte((byte)line);
                debugStream.WriteByte(col);
            }
        }

        public void WriteDebugString(string s)
        {
            if (debugStream != null)
            {
                byte[] bytes = Encoding.ASCII.GetBytes(s);
                debugStream.Write(bytes, 0, bytes.Length);
                debugStream.WriteByte(0);
            }
        }

        public void CloseDebugFile()
        {
            if (debugStream != null)
            {
                debugStream.WriteByte(DEBF.EOF_DBR);
                debugStream.Close();
                debugStream = null;
            }
        }

        public bool IsDebugFileOpen
        {
            get { return debugStream != null; }
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

        public int DebugPosition
        {
            get { return (int)debugStream.Position; }
            set { debugStream.Seek(value, SeekOrigin.Begin); }
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

        public void EnterLocalScope(Symbol symbol)
        {
            localScope = symbol;
        }

        public void LeaveLocalScope()
        {
            localScope = null;
        }

        public bool InLocalScope
        {
            get { return localScope != null; }
        }

        public bool TryGetLocalSymbol(string name, out Symbol result)
        {
            if (localScope == null)
            {
                result = null;
                return false;
            }

            return Symbols.TryGetValue(localScope.Name + " " + name, out result);
        }

        public void SetLocalSymbol(string name, Symbol value)
        {
            if (localScope == null)
                Errors.ThrowSerious("defining a local symbol outside a routine");

            Symbols[localScope.Name + " " + name] = value;
        }

        public void AddGlobalVar(string name)
        {
            int num = 16 + globalVarCount++;

            Symbol sym;
            if (Symbols.TryGetValue(name, out sym) == false)
            {
                sym = new Symbol(name, SymbolType.Variable, num, CurrentPass);
                Symbols.Add(name, sym);
            }
            else if (sym.Pass == CurrentPass)
                Errors.ThrowSerious("global redefined: " + name);
            else if (sym.Value != num)
                Errors.ThrowSerious("global {0} seems to have moved: was {1}, now {2}", name, sym.Value, num);
            else
                sym.Pass = CurrentPass;
        }

        public void AddObject(string name)
        {
            int num = 1 + objectCount++;

            Symbol sym;
            if (Symbols.TryGetValue(name, out sym) == false)
            {
                sym = new Symbol(name, SymbolType.Object, num, CurrentPass);
                Symbols.Add(name, sym);
            }
            else if (sym.Pass == CurrentPass)
                Errors.ThrowSerious("object redefined: " + name);
            else if (sym.Value != num)
                Errors.ThrowFatal("object {0} seems to have moved: was {1}, now {2}", name, sym.Value, num);
            else
                sym.Pass = CurrentPass;
        }

        public void ResetBetweenPasses()
        {
            StringEncoder = new StringEncoder();

            globalVarCount = 0;
            objectCount = 0;
        }

        public void HandleSeriousError(SeriousError ser)
        {
            ErrorCount++;

            if (ser.Node != null)
                Console.Error.Write("line {0}: ", ser.Node.Line);

            Console.Error.WriteLine("error: {0}", ser.Message);
        }

        public void HandleFatalError(FatalError fer)
        {
            if (fer.Node != null)
                Console.Error.Write("line {0}: ", fer.Node.Line);

            Console.Error.WriteLine("fatal error: {0}", fer.Message);
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
        /// The symbol is a global label (byte address).
        /// </summary>
        GlobalLabel,
        /// <summary>
        /// The symbol is a local label (byte address).
        /// </summary>
        LocalLabel,
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
        public int Value { get; private set; }
        /// <summary>
        /// The number of the pass where the symbol's value was last changed,
        /// or 0 if the symbol has never been assigned a value.
        /// </summary>
        public int Pass;

        public Symbol()
        {
        }

        public Symbol(string name)
        {
            this.Type = SymbolType.Unknown;
            this.Name = name;
        }

        public Symbol(int value, int pass)
        {
            this.Type = SymbolType.Constant;
            this.Value = value;
            this.Pass = pass;
        }

        public Symbol(string name, SymbolType type, int value, int pass)
        {
            this.Name = name;
            this.Type = type;
            this.Value = value;
            this.Pass = pass;
        }

        public void SetValue(int newValue, int pass)
        {
            this.Value = newValue;
            this.Pass = pass;
        }
    }
}
