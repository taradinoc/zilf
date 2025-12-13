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
using System.IO;

namespace Zilf.Emit.Glulx
{
    /// <summary>
    /// Glulx16 object builder: objects are identified by 1 or 2 byte indices and emit
    /// Z-machine style object records and property tables.
    /// </summary>
    sealed class ObjectBuilder16(string name, int number, int zVersion) : ConstantOperandBase, IObjectBuilder, INonzeroConstantOperand
    {
        readonly List<PropertyEntry> props = [];
        readonly List<FlagBuilder> flags = [];

        record PropertyEntry(PropertyBuilder Property, IOperand Value, PropertyKind Kind);

        enum PropertyKind { Byte, Word, Table }

        public int Number { get; } = number;
        public string SymbolicName => name;
        public string DescriptiveName { get; set; } = "";
        public IObjectBuilder? Parent { get; set; }
        public IObjectBuilder? Child { get; set; }
        public IObjectBuilder? Sibling { get; set; }

        public override string ToString() => Number.ToString();

        public void AddByteProperty(IPropertyBuilder prop, IOperand value) => props.Add(new((PropertyBuilder)prop, value, PropertyKind.Byte));

        public void AddWordProperty(IPropertyBuilder prop, IOperand value) => props.Add(new((PropertyBuilder)prop, value, PropertyKind.Word));

        public ITableBuilder AddComplexProperty(IPropertyBuilder prop)
        {
            var data = new TableBuilder16($"?{this}?CP?{prop}");
            props.Add(new((PropertyBuilder)prop, data, PropertyKind.Table));
            return data;
        }

        public void AddFlag(IFlagBuilder flag)
        {
            var fb = (FlagBuilder)flag;
            if (!flags.Contains(fb))
                flags.Add(fb);
        }

        internal void WriteObjectRecord(TextWriter writer, int attrBytes)
        {
            // Z-machine object record layout depends on version
            // V3: 32 attributes -> 4 bytes; parent/sibling/child are bytes; property ptr is word
            // V4/V5: 48 attributes -> 6 bytes; parent/sibling/child are words; property ptr is word
            foreach (var flagByte in GetFlagBytes(attrBytes))
                writer.Write(" 0x{0:X2}", flagByte);

            if (zVersion < 4)
            {
                writer.Write(" {0} {1} {2}",
                    (object?)Parent ?? 0,
                    (object?)Sibling ?? 0,
                    (object?)Child ?? 0);
            }
            else
            {
                writer.Write(" {0} {1} {2}",
                    (object?)Parent ?? 0,
                    (object?)Sibling ?? 0,
                    (object?)Child ?? 0);
            }
        }

        internal void WritePropertyTable(TextWriter writer)
        {
            // Header: first byte is the short-name length in bytes followed by the raw bytes
            var descText = DescriptiveName ?? string.Empty;
            writer.WriteLine(GameBuilder16.INDENT + "db {0}", descText.Length);
            if (descText.Length > 0)
            {
                writer.WriteLine(GameBuilder16.INDENT + "db \"{0}\"", GameBuilder.SanitizeString(descText));
            }

            props.Sort((a, b) => b.Property.Number.CompareTo(a.Property.Number));

            foreach (var pe in props)
            {
                switch (pe.Kind)
                {
                    case PropertyKind.Byte:
                        WritePropertyHeader(writer, pe.Property.Number, 1);
                        writer.WriteLine(GameBuilder16.INDENT + "db {0}", pe.Value.StripIndirect());
                        break;
                    case PropertyKind.Word:
                        WritePropertyHeader(writer, pe.Property.Number, 2);
                        writer.WriteLine(GameBuilder16.INDENT + "dw {0}", pe.Value.StripIndirect());
                        break;
                    default:
                        var tb = (TableBuilder16)pe.Value;
                        WritePropertyHeader(writer, pe.Property.Number, tb.Size);
                        tb.WriteTo(writer);
                        break;
                }
            }

            writer.WriteLine(GameBuilder16.INDENT + "db 0");
        }

        void WritePropertyHeader(TextWriter writer, int propNum, int length)
        {
            if (zVersion < 4)
            {
                // low 5 bits prop num, high 3 bits length-1
                byte header = (byte)((((length - 1) & 0x7) << 5) | (propNum & 0x1F));
                writer.WriteLine(GameBuilder16.INDENT + "db 0x{0:X2}   ; property {1}, length {2}", header, propNum, length);
            }
            else
            {
                if (length <= 2)
                {
                    byte header = (byte)(((length - 1) << 6) | (propNum & 0x3F));
                    writer.WriteLine(GameBuilder16.INDENT + "db 0x{0:X2}   ; property {1}, length {2}", header, propNum, length);
                }
                else
                {
                    writer.WriteLine(GameBuilder16.INDENT + "db 0x{0:X2}   ; property {1}", 0x80 | (propNum & 0x3F), propNum);
                    writer.WriteLine(GameBuilder16.INDENT + "db 0x{0:X2}   ; length {1}", 0x80 | (length == 64 ? 0 : length), length);
                }
            }
        }

        internal byte[] GetFlagBytes(int numBytes)
        {
            var bytes = new byte[numBytes];

            foreach (var flag in flags)
            {
                int flagNumber = flag.Number;
                int byteIndex = flagNumber / 8;
                int bitIndex = flagNumber % 8;

                if (byteIndex < numBytes)
                {
                    bytes[byteIndex] |= (byte)(1 << (7 - bitIndex));
                }
            }

            return bytes;
        }
    }
}
