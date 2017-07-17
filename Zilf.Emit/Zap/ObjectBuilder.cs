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
using System.IO;
using System.Text;
using JetBrains.Annotations;
using System.Diagnostics.Contracts;

namespace Zilf.Emit.Zap
{
    class ObjectBuilder : ConstantOperandBase, IObjectBuilder
    {
        const string INDENT = "\t";

        struct PropertyEntry
        {
            public const byte BYTE = 0;
            public const byte WORD = 1;
            public const byte TABLE = 2;

            public readonly PropertyBuilder Property;
            public readonly IOperand Value;
            public readonly byte Kind;

            public PropertyEntry(PropertyBuilder prop, IOperand value, byte kind)
            {
                Property = prop;
                Value = value;
                Kind = kind;
            }
        }

        readonly List<PropertyEntry> props = new List<PropertyEntry>();
        readonly List<FlagBuilder> flags = new List<FlagBuilder>();

        public ObjectBuilder(string name)
        {
            SymbolicName = name;
        }

        public string SymbolicName { get; }

        [NotNull]
        public string DescriptiveName { get; set; } = "";

        [CanBeNull]
        public IObjectBuilder Parent { get; set; }

        [CanBeNull]
        public IObjectBuilder Child { get; set; }

        [CanBeNull]
        public IObjectBuilder Sibling { get; set; }

        [NotNull]
        public string Flags1 => GetFlagsString(0);

        [NotNull]
        public string Flags2 => GetFlagsString(16);

        [NotNull]
        public string Flags3 => GetFlagsString(32);

        [NotNull]
        string GetFlagsString(int start)
        {
            Contract.Ensures(Contract.Result<string>() != null);
            var sb = new StringBuilder();

            foreach (var flag in flags)
            {
                int num = flag.Number;
                if (num >= start && num < start + 16)
                {
                    if (sb.Length > 0)
                        sb.Append('+');

                    sb.Append("FX?");
                    sb.Append(flag);
                }
            }

            return sb.Length == 0 ? "0" : sb.ToString();
        }

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

        internal void WriteProperties(TextWriter writer)
        {
            writer.WriteLine(INDENT + ".STRL \"{0}\"", GameBuilder.SanitizeString(DescriptiveName));

            props.Sort((a, b) => b.Property.Number.CompareTo(a.Property.Number));

            foreach (var pe in props)
            {
                switch (pe.Kind)
                {
                    case PropertyEntry.BYTE:
                        writer.WriteLine(INDENT + ".PROP 1,{0}", pe.Property);
                        writer.WriteLine(INDENT + ".BYTE {0}", pe.Value);
                        break;
                    case PropertyEntry.WORD:
                        writer.WriteLine(INDENT + ".PROP 2,{0}", pe.Property);
                        writer.WriteLine(INDENT + ".WORD {0}", pe.Value);
                        break;
                    default:
                        var tb = (TableBuilder)pe.Value;
                        writer.WriteLine(INDENT + ".PROP {0},{1}", tb.Size, pe.Property);
                        tb.WriteTo(writer);
                        break;
                }
            }

            writer.WriteLine(INDENT + ".BYTE 0");
        }

        public override string ToString()
        {
            return SymbolicName;
        }
    }
}