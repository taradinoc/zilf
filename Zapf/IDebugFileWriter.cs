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

namespace Zapf
{
    public struct LineRef
    {
        public LineRef(byte file, ushort line, byte col)
        {
            File = file;
            Line = line;
            Col = col;
        }

        public readonly byte File;
        public readonly ushort Line;
        public readonly byte Col;

        public static bool operator ==(LineRef a, LineRef b)
        {
            return a.File == b.File &&
                a.Line == b.Line &&
                a.Col == b.Col;
        }

        public static bool operator !=(LineRef a, LineRef b)
        {
            return a.File != b.File ||
                a.Line != b.Line ||
                a.Col != b.Col;
        }

        public override bool Equals(object obj)
        {
            return obj is LineRef lineRef && lineRef == this;
        }

        public override int GetHashCode()
        {
            int result = File;
            result = result * 31 + Line;
            result = result * 31 + Col;
            return result;
        }
    }

    public interface IDebugFileWriter
    {
        void Close();

        void StartRoutine(LineRef start, int address, string name, IEnumerable<string> locals);
        bool InRoutine { get; }
        void RestartRoutine();
        void WriteLine(LineRef loc, int address);
        void EndRoutine(LineRef end, int address);

        void WriteAction(ushort number, string name);
        void WriteArray(ushort offsetFromGlobal, string name);
        void WriteAttr(ushort number, string name);
        void WriteClass(string name, LineRef start, LineRef end);
        void WriteFakeAction(ushort number, string name);
        void WriteFile(byte number, string includeName, string actualName);
        void WriteGlobal(byte number, string name);
        void WriteHeader(byte[] header);
        void WriteMap(IEnumerable<KeyValuePair<string, int>> map);
        void WriteObject(ushort number, string name, LineRef start, LineRef end);
        void WriteProp(ushort number, string name);
    }
}
