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
    class ObjectBuilder(string name) : ConstantOperandBase, IObjectBuilder, INonzeroConstantOperand
    {
        struct PropertyEntry(PropertyBuilder prop, IOperand value, byte kind)
        {
            public const byte BYTE = 0;
            public const byte WORD = 1;
            public const byte TABLE = 2;

            public readonly PropertyBuilder Property = prop;
            public readonly IOperand Value = value;
            public readonly byte Kind = kind;
        }

        const string INDENT = GameBuilder.INDENT;

        readonly List<PropertyEntry> props = [];
        readonly List<FlagBuilder> flags = [];

        public string SymbolicName => name;

        public override string ToString() => name;

        public string DescriptiveName { get; set; } = "";
        public IObjectBuilder? Parent { get; set; }
        public IObjectBuilder? Child { get; set; }
        public IObjectBuilder? Sibling { get; set; }

        public void AddByteProperty(IPropertyBuilder prop, IOperand value)
        {
            var pe = new PropertyEntry((PropertyBuilder)prop, value, PropertyEntry.BYTE);
            props.Add(pe);
        }

        public void AddWordProperty(IPropertyBuilder prop, IOperand value)
        {
            var pe = new PropertyEntry((PropertyBuilder)prop, value, PropertyEntry.WORD);
            props.Add(pe);
        }

        public ITableBuilder AddComplexProperty(IPropertyBuilder prop)
        {
            var data = new TableBuilder($"?{this}?CP?{prop}");
            var pe = new PropertyEntry((PropertyBuilder)prop, data, PropertyEntry.TABLE);
            props.Add(pe);
            return data;
        }

        public void AddFlag(IFlagBuilder flag)
        {
            var fb = (FlagBuilder)flag;
            if (!flags.Contains(fb))
                flags.Add(fb);
        }

        public byte[] GetFlagBytes(int numBytes)
        {
            var bytes = new byte[numBytes];

            foreach (var flag in flags)
            {
                int flagNumber = flag.Number;
                int byteIndex = flagNumber / 8;
                int bitIndex = flagNumber % 8;

                if (byteIndex < numBytes)
                {
                    bytes[byteIndex] |= (byte)(1 << bitIndex);
                }
            }

            return bytes;
        }

        internal void WriteProperties(TextWriter writer)
        {
            props.Sort((a, b) => a.Property.Number.CompareTo(b.Property.Number));

            writer.WriteLine(INDENT + "dd {0}", props.Count);

            foreach (var pe in props)
            {
                switch (pe.Kind)
                {
                    case PropertyEntry.BYTE:
                    case PropertyEntry.WORD:
                        // byte properties are an illusion
                        writer.WriteLine(INDENT + "dw {0}", pe.Property);
                        writer.WriteLine(INDENT + "dw 1");
                        writer.WriteLine(INDENT + "dd _prop_data_{0}_{1}", SymbolicName, pe.Property.Name);
                        writer.WriteLine(INDENT + "dw 0");
                        break;
                    default:
                        var tb = (TableBuilder)pe.Value;
                        writer.WriteLine(INDENT + "dw {0}", pe.Property);
                        writer.WriteLine(INDENT + "dw {0}", (tb.Size + 3) / 4);
                        writer.WriteLine(INDENT + "dd _prop_data_{0}_{1}", SymbolicName, pe.Property.Name);
                        writer.WriteLine(INDENT + "dw 0");
                        break;
                }
            }

            foreach (var pe in props)
            {
                writer.WriteLine();

                switch (pe.Kind)
                {
                    case PropertyEntry.BYTE:
                    case PropertyEntry.WORD:
                        writer.WriteLine(INDENT + "dw 4   ; property length");
                        break;
                    default:
                        writer.WriteLine(INDENT + "dw {0}   ; property length", ((TableBuilder)pe.Value).Size);
                        break;
                }

                writer.WriteLine("_prop_data_{0}_{1}:", SymbolicName, pe.Property.Name);

                switch (pe.Kind)
                {
                    case PropertyEntry.BYTE:
                    case PropertyEntry.WORD:
                        writer.WriteLine(INDENT + "dd {0}", pe.Value);
                        break;
                    default:
                        ((TableBuilder)pe.Value).WriteTo(writer);
                        break;
                }
            }
        }
    }
}