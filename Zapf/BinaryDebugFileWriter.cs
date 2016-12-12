/* Copyright 2010, 2015 Jesse McGrew
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
using System.Text;

namespace Zapf
{
    class BinaryDebugFileWriter : IDebugFileWriter
    {
        readonly Stream stream;
        ushort nextRoutineNumber;
        long routineStart = -1;
        int routinePoints = -1;

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

        void WriteDebugByte(byte b)
        {
            stream.WriteByte(b);
        }

        void WriteDebugWord(ushort w)
        {
            stream.WriteByte((byte)(w >> 8));
            stream.WriteByte((byte)w);
        }

        void WriteDebugAddress(int a)
        {
            stream.WriteByte((byte)(a >> 16));
            stream.WriteByte((byte)(a >> 8));
            stream.WriteByte((byte)a);
        }

        void WriteDebugLineRef(LineRef lineRef)
        {
            stream.WriteByte(lineRef.File);
            stream.WriteByte((byte)(lineRef.Line >> 8));
            stream.WriteByte((byte)lineRef.Line);
            stream.WriteByte(lineRef.Col);
        }

        void WriteDebugString(string s)
        {
            var bytes = Encoding.ASCII.GetBytes(s);
            stream.Write(bytes, 0, bytes.Length);
            stream.WriteByte(0);
        }

        #endregion
    }
}
