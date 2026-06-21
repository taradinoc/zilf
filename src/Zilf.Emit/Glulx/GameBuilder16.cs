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
using System.ComponentModel.DataAnnotations.Schema;
using System.IO;
using System.Linq;

namespace Zilf.Emit.Glulx
{
    /// <summary>
    /// Glulx16-specific game builder.
    /// </summary>
    public sealed class GameBuilder16 : GameBuilder
    {
        readonly int zversion;
        readonly int attrBytes;
        readonly int entryLen;
        readonly int parentOffset;
        readonly int siblingOffset;
        readonly int childOffset;
        readonly int propPtrOffset;

        public GameBuilder16(IGlulxStreamFactory streamFactory, GlulxGameOptions? gameOptions = null)
            : base(streamFactory, gameOptions, CreateRuntimeLib(gameOptions))
        {
            zversion = GetZVersion(gameOptions);
            attrBytes = zversion < 4 ? 4 : 6;
            parentOffset = attrBytes;
            siblingOffset = parentOffset + (zversion < 4 ? 1 : 2);
            childOffset = siblingOffset + (zversion < 4 ? 1 : 2);
            propPtrOffset = childOffset + (zversion < 4 ? 1 : 2);
            entryLen = propPtrOffset + 2;
        }

        private static int GetZVersion(GlulxGameOptions? gameOptions) => gameOptions?.ZMachineVersion ?? 3;

        private static RuntimeLib CreateRuntimeLib(GlulxGameOptions? gameOptions)
        {
            var version = GetZVersion(gameOptions);
            // return version < 4 ? new RuntimeLib16V3() : new RuntimeLib16V4();
            return version switch
            {
                < 4 => new RuntimeLib16V3(),
                4 => new RuntimeLib16V4(),
                _ => new RuntimeLib16V5(),
            };
        }

        public override IOperand MakeOperand(string value)
        {
            if (stringPool.TryGetValue(value, out var result) == false)
            {
                result = new PackedStringOperand(stringPool.Count);
                stringPool.Add(value, result);
            }
            return result;
        }

        public override IObjectBuilder DefineObject(string name)
        {
            name = SanitizeSymbol(name);
            if (symbols.ContainsKey(name))
                throw new ArgumentException("Global symbol already defined: " + name, nameof(name));

            var number = objects.Count + 1;
            var result = new ObjectBuilder16(name, number, zversion);
            objects.Add(result);
            symbols.Add(name, "object");
            return result;
        }

        protected override IRoutineBuilder CreateRoutineBuilder(string name, bool entryPoint, bool cleanStack)
        {
            return new RoutineBuilder16(this, name, entryPoint);
        }

        protected override IGlobalBuilder CreateGlobalBuilder(string name)
        {
            return new GlobalBuilder16(name);
        }

        protected override ITableBuilder CreateTableBuilder(string name)
        {
            return new TableBuilder16(name);
        }

        protected override string RoutineSectionDirective => "section .data";
        protected override string DataSectionDirective => "section .data";
        protected override string BssSectionDirective => "section .data";
        protected override string TextSectionDirective => "section .data";
        protected override string RuntimeCodeSectionDirective => INDENT + "section .data";
        protected override bool PureTablesGoToData => true;
        protected override bool RoutinesBeforeData => false;
        protected override string EntryRoutineSectionDirective => "section .text";

        protected override void EmitFinalOutput()
        {
            if (stream == null)
                return;

            using var mainWriter = new StreamWriter(stream);

            // Header
            mainWriter.Write(headerWriter.ToString());
            mainWriter.WriteLine(INDENT + "PACKING_FACTOR = 8");

            if (EntryRoutineWriter.GetStringBuilder().Length > 0)
            {
                mainWriter.WriteLine(INDENT + EntryRoutineSectionDirective);
                mainWriter.Write(EntryRoutineWriter.ToString());
                mainWriter.WriteLine();

                if (AlignAfterEntry)
                {
                    mainWriter.WriteLine(INDENT + "align 256");
                    mainWriter.WriteLine();
                }
            }

            // Data (tables first)
            if (DataWriter.GetStringBuilder().Length > 0)
            {
                mainWriter.WriteLine(INDENT + DataSectionDirective);
                mainWriter.Write(DataWriter.ToString());
                mainWriter.WriteLine();
            }

            // Routines / code after data to keep tables at low addresses
            EmitRoutines(mainWriter);

            // BSS if any
            if (BssWriter.GetStringBuilder().Length > 0)
            {
                mainWriter.WriteLine(INDENT + BssSectionDirective);
                mainWriter.Write(BssWriter.ToString());
                mainWriter.WriteLine();
            }

            // Runtime, hooks, strings, metadata
            if (TextSegmentWriter.GetStringBuilder().Length > 0)
            {
                mainWriter.WriteLine(INDENT + TextSectionDirective);
                mainWriter.Write(TextSegmentWriter.ToString());
            }

            mainWriter.Flush();

            writer = null!;
            stream = null;
        }

        protected override void FinishObjects()
        {
            writer.WriteLine();

            writer.WriteLine(INDENT + "GL16_ATTR_BYTES = {0}", attrBytes);
            writer.WriteLine(INDENT + "GL16_OBJ_ENTRY_LEN = {0}", entryLen);
            writer.WriteLine(INDENT + "GL16_OBJ_PARENT_OFF = {0}", parentOffset);
            writer.WriteLine(INDENT + "GL16_OBJ_SIBLING_OFF = {0}", siblingOffset);
            writer.WriteLine(INDENT + "GL16_OBJ_CHILD_OFF = {0}", childOffset);
            writer.WriteLine(INDENT + "GL16_OBJ_PROP_OFF = {0}", propPtrOffset);
            writer.WriteLine(INDENT + "GL16_MAX_PROPERTIES = {0}", MaxProperties);

            // property defaults (Z format: MaxProperties entries)
            writer.WriteLine(INDENT + "; Property defaults");
            writer.WriteLine("property_defaults_table:");
            var propNums = Enumerable.Range(1, MaxProperties);
            var propDefaults = from num in propNums
                               join p in props on num equals p.Value.Number into propGroup
                               from prop in propGroup.DefaultIfEmpty()
                               let name = prop.Key
                               let def = prop.Value?.DefaultValue?.StripIndirect()
                               select new { num, name, def };

            foreach (var row in propDefaults)
            {
                if (row.name != null)
                    writer.WriteLine(INDENT + "; {0}", row.name);
                else
                    writer.WriteLine(INDENT + "; Unused property #{0}", row.num);

                writer.WriteLine(INDENT + "dw {0}", (object?)row.def ?? "0");
            }

            if (objects.Count > 0)
                writer.WriteLine();

            writer.WriteLine("object_table_base:");

            foreach (var ob in objects.OfType<ObjectBuilder16>())
            {
                writer.WriteLine("{0}:", ob.SymbolicName);
                writer.Write(INDENT + "db");
                foreach (var flagByte in ob.GetFlagBytes(attrBytes))
                    writer.Write(" 0x{0:X2}", flagByte);
                writer.WriteLine();

                if (zversion < 4)
                {
                    writer.WriteLine(INDENT + "db {0} {1} {2}", (object?)ob.Parent ?? 0, (object?)ob.Sibling ?? 0, (object?)ob.Child ?? 0);
                }
                else
                {
                    writer.WriteLine(INDENT + "dw {0} {1} {2}", (object?)ob.Parent ?? 0, (object?)ob.Sibling ?? 0, (object?)ob.Child ?? 0);
                }

                writer.WriteLine(INDENT + "dw ptbl_{0}", ob.Number);
            }

            foreach (var ob in objects.OfType<ObjectBuilder16>())
            {
                writer.WriteLine();
                writer.WriteLine("ptbl_{0}:", ob.Number);
                ob.WritePropertyTable(writer);
            }
        }

        protected override void FinishSyntax()
        {
            writer.WriteLine();
            writer.WriteLine("; vocabulary");

            var charResolution = zCompatVersion >= 4 ? 9 : 6;
            var encodedBytes = zCompatVersion >= 4 ? 6 : 4;
            var dataBytes = vocabulary.Count > 0 ? vocabulary[0].Size : 3;

            writer.WriteLine(INDENT + "VOCAB_RESOLUTION = {0}", charResolution);

            writer.WriteLine();
            writer.WriteLine("VOCAB:");
            writer.WriteLine("si_breaks:");

            if (siBreaks.Count > 255)
                throw new InvalidOperationException("Too many self-inserting breaks");

            writer.WriteLine(INDENT + "db {0}", siBreaks.Count);
            foreach (char c in siBreaks)
            {
                if ((byte)c != c)
                    throw new InvalidOperationException($"Self-inserting break character out of range (${(ushort)c:x4})");

                writer.WriteLine(INDENT + "db {0}", (byte)c);
            }

            if (vocabulary.Count == 0)
            {
                writer.WriteLine(INDENT + "db ({0} + {1})", encodedBytes, dataBytes);
                writer.WriteLine(INDENT + "dw 0");
                writer.WriteLine("vocab_entry_length: dd ({0} + {1})", encodedBytes, dataBytes);
                writer.WriteLine("vocab_entry_count: dd 0");
                writer.WriteLine("vocab_entries: dd 0");
                writer.WriteLine("vocab_string_table: dd 0");
            }
            else
            {
                writer.WriteLine(INDENT + "db ({0} + {1})", encodedBytes, dataBytes);
                writer.WriteLine(INDENT + "dw {0}", vocabulary.Count);
                writer.WriteLine("vocab_entries:");
                vocabulary.Sort((a, b) => string.CompareOrdinal(a.Word, b.Word));

                int stringIndex = 0;
                foreach (var wb in vocabulary)
                {
                    writer.WriteLine("{0}:", wb.Name);
                    writer.WriteLine(INDENT + "dd vocab_string_{0}", stringIndex);

                    for (int i = 4; i < encodedBytes; i++)
                        writer.WriteLine(INDENT + "db 0");

                    wb.WriteTo(writer);
                    stringIndex++;
                }

                writer.WriteLine("vocab_entry_length: dd ({0} + {1})", encodedBytes, dataBytes);
                writer.WriteLine("vocab_entry_count: dd {0}", vocabulary.Count);

                writer.WriteLine();
                writer.WriteLine("vocab_strings:");

                for (int i = 0; i < vocabulary.Count; i++)
                {
                    var wb = vocabulary[i];
                    var word = wb.Word.ToLowerInvariant();

                    writer.Write(INDENT + "vocab_string_{0}: db", i);
                    for (int j = 0; j < charResolution; j++)
                    {
                        writer.Write(' ');
                        if (j < word.Length)
                        {
                            char c = word[j];
                            if (c == '`')
                            {
                                writer.Write("`\\``");
                            }
                            else if (!char.IsControl(c))
                            {
                                writer.Write('`');
                                writer.Write(c);
                                writer.Write('`');
                            }
                            else
                            {
                                writer.Write((byte)c);
                            }
                        }
                        else
                        {
                            writer.Write('0');
                        }
                    }

                    writer.WriteLine();
                }
            }
        }

        protected override void FinishStrings()
        {
            // strings
            writer.WriteLine();
            writer.WriteLine(INDENT + "; Strings");

            foreach (var (text, symbol) in stringPool.OrderBy(p => p.Key))
            {
                // abbrevs?.AddText(text);
                writer.WriteLine("align PACKING_FACTOR");
                writer.WriteLine("{0}: huffstr \"{1}\"", ((PackedStringOperand)symbol).Name, SanitizeString(text));
            }
        }

        void MoveGlobal(string name, int index)
        {
            var curIndex = globals.FindIndex(g => g.Name == name);
            if (curIndex >= 0 && curIndex != index)
            {
                var gb = globals[curIndex];
                globals.RemoveAt(curIndex);
                globals.Insert(index, gb);
            }
        }

        protected override void FinishGlobals()
        {
            // V3 needs HERE, SCORE, and MOVES to be first
            if (zversion < 4)
            {
               MoveGlobal("HERE", 0);
               MoveGlobal("SCORE", 1);
               MoveGlobal("MOVES", 2);
            }

            for (int i = 0; i < globals.Count; i++)
                writer.WriteLine(GameBuilder16.INDENT + "global_{0}_num = {1}", globals[i].Name, i + 16);

            base.FinishGlobals();
        }
    }
}
