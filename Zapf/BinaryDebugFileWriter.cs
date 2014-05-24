using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;

namespace Zapf
{
    class BinaryDebugFileWriter : IDebugFileWriter
    {
        private readonly Stream stream;
        private ushort nextRoutineNumber;
        private long routineStart = -1;
        private int routinePoints = -1;

        public BinaryDebugFileWriter(Stream debugStream)
        {
            this.stream = debugStream;

            WriteDebugWord(0xDEBF);     // magic number
            WriteDebugWord(0);          // file format
            WriteDebugWord(2001);       // creator version
        }

        public bool InRoutine
        {
            get { return routinePoints >= 0; }
        }

        public void Close()
        {
            stream.WriteByte(DEBF.EOF_DBR);
            stream.Close();
        }

        public void WriteMap(IEnumerable<KeyValuePair<string, int>> map)
        {
            WriteDebugByte(DEBF.MAP_DBR);
            foreach (var pair in map)
            {
                WriteDebugString(pair.Key);
                WriteDebugAddress(pair.Value);
            }
            WriteDebugByte(0);
        }

        public void WriteHeader(byte[] header)
        {
            WriteDebugByte(DEBF.HEADER_DBR);
            stream.Write(header, 0, 64);
        }

        public void WriteAction(ushort value, string name)
        {
            WriteDebugByte(DEBF.ACTION_DBR);
            WriteDebugWord(value);
            WriteDebugString(name);
        }

        public void WriteArray(ushort offsetFromGlobal, string name)
        {
            WriteDebugByte(DEBF.ARRAY_DBR);
            WriteDebugWord(offsetFromGlobal);
            WriteDebugString(name);
        }
        
        public void WriteAttr(ushort value, string name)
        {
            WriteDebugByte(DEBF.ATTR_DBR);
            WriteDebugWord(value);
            WriteDebugString(name);
        }

        public void WriteClass(string name, LineRef start, LineRef end)
        {
            WriteDebugByte(DEBF.CLASS_DBR);
            WriteDebugString(name);
            WriteDebugLineRef(start);
            WriteDebugLineRef(end);
        }

        public void WriteFakeAction(ushort value, string name)
        {
            WriteDebugByte(DEBF.FAKE_ACTION_DBR);
            WriteDebugWord(value);
            WriteDebugString(name);
        }

        public void WriteFile(byte number, string includeName, string actualName)
        {
            WriteDebugByte(DEBF.FILE_DBR);
            WriteDebugByte(number);
            WriteDebugString(includeName);
            WriteDebugString(actualName);
        }

        public void WriteGlobal(byte number, string name)
        {
            WriteDebugByte(DEBF.GLOBAL_DBR);
            WriteDebugByte(number);
            WriteDebugString(name);
        }

        public void WriteLine(LineRef loc, int address)
        {
            if (!InRoutine)
                throw new InvalidOperationException("WriteLine must be called inside a routine");

            WriteDebugLineRef(loc);
            WriteDebugWord((ushort)(address - routineStart));
            routinePoints++;
        }

        public void WriteObject(ushort number, string name, LineRef start, LineRef end)
        {
            WriteDebugByte(DEBF.OBJECT_DBR);
            WriteDebugWord(number);
            WriteDebugString(name);
            WriteDebugLineRef(start);
            WriteDebugLineRef(end);
        }

        public void WriteProp(ushort number, string name)
        {
            WriteDebugByte(DEBF.PROP_DBR);
            WriteDebugWord(number);
            WriteDebugString(name);
        }

        public void StartRoutine(LineRef start, int address, string name, IEnumerable<string> locals)
        {
            WriteDebugByte(DEBF.ROUTINE_DBR);
            WriteDebugWord(nextRoutineNumber);
            WriteDebugLineRef(start);
            WriteDebugAddress(address);
            WriteDebugString(name);
            foreach (var local in locals)
                WriteDebugString(local);
            WriteDebugByte(0);

            // start LINEREF_DBR block
            WriteDebugByte(DEBF.LINEREF_DBR);
            WriteDebugWord(nextRoutineNumber);
            WriteDebugWord(0);      // # sequence points, filled in later

            routinePoints = 0;
            routineStart = address;
        }

        public void EndRoutine(LineRef end, int address)
        {
            // finish LINEREF_DBR block
            var curPosition = stream.Position;
            stream.Position -= 2 + routinePoints * 6;
            WriteDebugWord((ushort)routinePoints);
            stream.Position = curPosition;
            routinePoints = -1;
            routineStart = -1;

            // write ROUTINE_END_DBR block
            WriteDebugByte(DEBF.ROUTINE_END_DBR);
            WriteDebugWord(nextRoutineNumber++);
            WriteDebugLineRef(end);
            WriteDebugAddress(address);
        }

        #region Write Primitives

        private void WriteDebugByte(byte b)
        {
            stream.WriteByte(b);
        }

        private void WriteDebugWord(ushort w)
        {
            stream.WriteByte((byte)(w >> 8));
            stream.WriteByte((byte)w);
        }

        private void WriteDebugAddress(int a)
        {
            stream.WriteByte((byte)(a >> 16));
            stream.WriteByte((byte)(a >> 8));
            stream.WriteByte((byte)a);
        }

        private void WriteDebugLineRef(LineRef lineRef)
        {
            stream.WriteByte(lineRef.File);
            stream.WriteByte((byte)(lineRef.Line >> 8));
            stream.WriteByte((byte)lineRef.Line);
            stream.WriteByte(lineRef.Col);
        }

        private void WriteDebugString(string s)
        {
            byte[] bytes = Encoding.ASCII.GetBytes(s);
            stream.Write(bytes, 0, bytes.Length);
            stream.WriteByte(0);
        }

        #endregion
    }
}
