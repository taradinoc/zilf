using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Zapf
{
    struct LineRef
    {
        public LineRef(byte file, ushort line, byte col)
        {
            this.File = file;
            this.Line = line;
            this.Col = col;
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
            return obj is LineRef && ((LineRef)obj) == this;
        }

        public override int GetHashCode()
        {
            int result = File;
            result = result * 31 + Line;
            result = result * 31 + Col;
            return result;
        }
    }

    interface IDebugFileWriter
    {
        void Close();

        void StartRoutine(LineRef start, int address, string name, IEnumerable<string> locals);
        bool InRoutine { get; }
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
