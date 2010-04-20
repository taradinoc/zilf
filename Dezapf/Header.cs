/* Copyright 2010 Jesse McGrew
 * 
 * This file is part of DeZAPF.
 * 
 * DeZAPF is free software: you can redistribute it and/or modify it
 * under the terms of the GNU General Public License as published by
 * the Free Software Foundation, either version 3 of the License, or
 * (at your option) any later version.
 * 
 * DeZAPF is distributed in the hope that it will be useful, but
 * WITHOUT ANY WARRANTY; without even the implied warranty of
 * MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the GNU
 * General Public License for more details.
 * 
 * You should have received a copy of the GNU General Public License
 * along with DeZAPF.  If not, see <http://www.gnu.org/licenses/>.
 */

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.IO;

namespace Dezapf
{
    [AttributeUsage(AttributeTargets.Field)]
    class SetByTerpAttribute : Attribute
    {
    }

    struct Header
    {
        // supplied in file...
        public byte ZVersion;
        public byte Flags1;
        public ushort Release;
        public ushort EndLod;
        public ushort Start;
        public ushort Vocab;
        public ushort Objects;
        public ushort Globals;
        public ushort Impure;
        public ushort Flags2;
        public byte[] Serial;
        public ushort Words;
        public ushort Length;
        public ushort Checksum;

        // filled in by interpreter...
        [SetByTerp]
        public byte TerpNum;
        [SetByTerp]
        public byte TerpVersion;
        [SetByTerp]
        public byte ScreenRows;
        [SetByTerp]
        public byte ScreenColumns;
        [SetByTerp]
        public ushort ScreenWidth;
        [SetByTerp]
        public ushort ScreenHeight;
        [SetByTerp]
        public byte FontWidth;
        [SetByTerp]
        public byte FontHeight;

        // supplied in file...
        public ushort RoutineOffset;
        public ushort StringOffset;

        // filled in by interpreter...
        [SetByTerp]
        public byte DefaultBG;
        [SetByTerp]
        public byte DefaultFG;

        // supplied in file...
        public ushort TCharsTable;

        // filled in by interpreter...
        [SetByTerp]
        public ushort BufferWidth;
        [SetByTerp]
        public ushort StandardVersion;

        // supplied in file...
        public ushort AlphabetTable;
        public ushort ExtensionTable;

        // filled in by interpreter...
        [SetByTerp]
        public byte[] Username;

        // supplied in file...
        public byte[] Creator;

        public Header(BinaryReader rdr)
        {
            ZVersion = rdr.ReadByte();      // 0
            Flags1 = rdr.ReadByte();        // 1
            Release = rdr.ReadZWord();      // 2
            EndLod = rdr.ReadZWord();       // 4
            Start = rdr.ReadZWord();        // 6
            Vocab = rdr.ReadZWord();        // 8
            Objects = rdr.ReadZWord();      // A
            Globals = rdr.ReadZWord();      // C
            Impure = rdr.ReadZWord();       // E
            Flags2 = rdr.ReadZWord();       // 10
            Serial = rdr.ReadBytes(6);      // 12
            Words = rdr.ReadZWord();        // 18
            Length = rdr.ReadZWord();       // 1A
            Checksum = rdr.ReadZWord();     // 1C
            TerpNum = rdr.ReadByte();       // 1D
            TerpVersion = rdr.ReadByte();   // 1E
            ScreenRows = rdr.ReadByte();    // 20
            ScreenColumns = rdr.ReadByte(); // 21
            ScreenWidth = rdr.ReadZWord();  // 22
            ScreenHeight = rdr.ReadZWord(); // 24
            FontWidth = rdr.ReadByte();     // 26
            FontHeight = rdr.ReadByte();    // 27
            RoutineOffset = rdr.ReadZWord();    // 28
            StringOffset = rdr.ReadZWord();     // 2A
            DefaultBG = rdr.ReadByte();         // 2C
            DefaultFG = rdr.ReadByte();         // 2D
            TCharsTable = rdr.ReadZWord();      // 2E
            BufferWidth = rdr.ReadZWord();      // 30
            StandardVersion = rdr.ReadZWord();  // 32
            AlphabetTable = rdr.ReadZWord();    // 34
            ExtensionTable = rdr.ReadZWord();   // 36
            Username = rdr.ReadBytes(4);        // 38
            Creator = rdr.ReadBytes(4);         // 3C
        }
    }

    class HeaderChunk : Chunk
    {
        private readonly Header hdr;

        public HeaderChunk(int pc, int length, Header hdr)
            : base(pc, length)
        {
            this.hdr = hdr;
        }

        public Header Header
        {
            get { return hdr; }
        }

        public override void WriteTo(TextWriter writer, Context ctx)
        {
            ctx.OutputStyle.FormatHeader(writer, ctx, this);
        }
    }
}
