/* Copyright 2010-2026 Tara McGrew
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
using System.Linq;

namespace Zilf.Emit.Cornerstone
{
    public sealed partial class GameBuilder
    {
        private sealed record TableEntry(IOperand Operand, bool IsByte);

        private class TableBuilder(string name, bool pure) : ITableBuilder
        {
            private readonly List<TableEntry> contents = [];

            public string Name { get; } = name;

            // Currently unused, since all addressable memory is writable
            public bool Pure { get; } = pure;

            internal IReadOnlyList<TableEntry> Contents => contents;

            public void AddByte(byte value) => contents.Add(new TableEntry(new NumericOperand(value), IsByte: true));

            public void AddByte(IOperand value)
            {
                MarkFarSelectorDependencies(value);
                contents.Add(new TableEntry(value, IsByte: true));
            }

            public void AddWord(int value) => contents.Add(new TableEntry(new NumericOperand(value), IsByte: false));

            public void AddWord(IOperand value)
            {
                MarkFarSelectorDependencies(value);
                contents.Add(new TableEntry(value, IsByte: false));
            }

            public void EmitRam(TextWriter writer)
            {
                writer.WriteLine($"{Name}::");

                if (contents.Count == 0)
                {
                    EmitWordDirectives(writer, ["0x0000"]);
                    return;
                }

                for (var index = 0; index < contents.Count;)
                {
                    bool emitBytes = contents[index].IsByte;
                    var chunk = new List<TableEntry>();
                    while (index < contents.Count && contents[index].IsByte == emitBytes)
                    {
                        chunk.Add(contents[index]);
                        index++;
                    }

                    if (emitBytes)
                        EmitByteDirectives(writer, chunk.Select(FormatByteOperand));
                    else
                        EmitWordDirectives(writer, chunk.Select(entry => FormatConstantOperand(entry.Operand)));
                }
            }

            internal static string FormatByteOperand(TableEntry entry)
            {
                var operand = entry.Operand.StripIndirect();

                return operand switch
                {
                    NumericOperand numeric when numeric.Value is >= 0 and <= 0xFF => $"0x{numeric.Value:X2}",
                    _ => FormatConstantOperand(operand),
                };
            }

            public IConstantOperand Add(IConstantOperand other) => new CompositeOperand(this, other);
        }
    }
}