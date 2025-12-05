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
using System.Diagnostics.CodeAnalysis;
using System.IO;
using System.Linq;
using System.Text;

namespace Zilf.Emit.Glulx
{
    public sealed class GameBuilder : IGameBuilder
    {
        internal const string INDENT = "\t";

        internal static readonly NumericOperand ZERO = new(0);

        internal static readonly NumericOperand ONE = new(1);

        static readonly ConstantLiteralOperand VOCAB = new("VOCAB");

        internal RuntimeLib RuntimeLib { get; } = new();

        // all global names go in here
        readonly Dictionary<string, string> symbols = new(250);

        readonly List<ObjectBuilder> objects = new(100);
        readonly Dictionary<string, PropertyBuilder> props = new(32);
        readonly Dictionary<string, FlagBuilder> flags = new(32);
        readonly Dictionary<string, IOperand> constants = new(100);
        readonly HashSet<string> transparentConstants = new(100);
        readonly List<GlobalBuilder> globals = new(100);
        readonly List<TableBuilder> impureTables = new(10);
        readonly List<TableBuilder> pureTables = new(10);
        readonly List<(TableBuilder Table, string OriginalName)> tracedTables = new(10);
        readonly List<WordBuilder> vocabulary = new(100);
        readonly HashSet<char> siBreaks = new();
        readonly Dictionary<string, IOperand> stringPool = new(100);
        readonly Dictionary<int, NumericOperand> numberPool = new(50);

        readonly IGlulxStreamFactory streamFactory;
        readonly GlulxGameOptions options;

#if DEBUG
        readonly Dictionary<string, (int Applications, int InstructionsSaved)> peepholeStats = new(StringComparer.Ordinal);
#endif

        IRoutineBuilder? entryRoutine;
        IOperand? updateStatusLineHook;     // IRoutineBuilder or IGlobalBuilder
        ITableBuilder? terminatingCharsTable;

        Stream? stream;
        TextWriter writer;

        /// <exception cref="ArgumentException"><paramref name="gameOptions"/> is the wrong type for this target.</exception>
        public GameBuilder(IGlulxStreamFactory streamFactory, GlulxGameOptions? gameOptions = null)
        {
            this.streamFactory = streamFactory ?? throw new ArgumentNullException(nameof(streamFactory));
            this.options = gameOptions ?? new();

            stream = streamFactory.CreateMainStream();
            writer = new StreamWriter(stream);

            Begin();
        }

        public IDebugFileBuilder? DebugFile => null;

        public void Dispose()
        {
            writer?.Dispose();
            stream?.Dispose();
        }

        void Begin()
        {
            writer.WriteLine(INDENT + "; Main assembly file for {0}", streamFactory.GetMainFileName(true));
            writer.WriteLine();
            writer.WriteLine(INDENT + "section .text");
            writer.WriteLine();
        }

        public IGameOptions Options => options;

        /// <exception cref="ArgumentException">A symbol called <paramref name="name"/> is already defined.</exception>
        public IOperand DefineConstant(string name, IOperand value)
        {
            name = SanitizeSymbol(name);
            if (symbols.ContainsKey(name))
                throw new ArgumentException("Global symbol already defined: " + name, nameof(name));

            constants.Add(name, value);
            symbols.Add(name, "constant");

            switch (value)
            {
                case INumericOperand num:
                    return new NumericConstantOperand(name, num.Value);

                default:
                    // Glazer doesn't allow assigning labels to constants, so we make these into
                    // transparent constants that are replaced with their values at compile time
                    transparentConstants.Add(name);
                    return new TransparentConstantOperand(name, value.ToString() ?? "<bug>");
            };
        }

        internal bool TryGetNumericConstantValue(string name, out int value)
        {
            if (constants.TryGetValue(name, out var operand) && operand is INumericOperand numeric)
            {
                value = numeric.Value;
                return true;
            }

            value = default;
            return false;
        }

        /// <exception cref="ArgumentException">A symbol called <paramref name="name"/> is already defined.</exception>
        public IGlobalBuilder DefineGlobal(string name)
        {
            bool isUSL = name == "CURRENT-STATUS-LINE";

            name = SanitizeSymbol(name);
            if (symbols.ContainsKey(name))
                throw new ArgumentException("Global symbol already defined: " + name, nameof(name));

            var gb = new GlobalBuilder(name);
            globals.Add(gb);
            symbols.Add(name, "global");

            if (isUSL && updateStatusLineHook == null)
                updateStatusLineHook = gb;

            return gb;
        }

        /// <exception cref="ArgumentException">A symbol called <paramref name="name"/> is already defined.</exception>
        public ITableBuilder DefineTable(string? name, bool pure)
        {
            if (name == null)
                name = "T_" + Convert.ToString(pureTables.Count + impureTables.Count);
            else
                name = SanitizeSymbol(name);

            if (symbols.ContainsKey(name))
                throw new ArgumentException("Global symbol already defined: " + name, nameof(name));

            var tb = new TableBuilder(name);
            if (pure)
                pureTables.Add(tb);
            else
                impureTables.Add(tb);
            symbols.Add(name, "table");
            return tb;
        }

        /// <summary>
        /// Marks a table for write tracing. When tracing is enabled, PUT and PUTB operations
        /// that affect any traced table will emit debugging information at runtime.
        /// </summary>
        /// <param name="table">The table to trace.</param>
        /// <param name="originalName">The original name of the table as it appears in ZIL source code.</param>
        public void TraceTable(ITableBuilder table, string originalName)
        {
            if (table is TableBuilder tb && !tracedTables.Any(t => t.Table == tb))
            {
                tracedTables.Add((tb, originalName));
            }
        }

        /// <summary>
        /// Gets whether any tables are being traced for writes.
        /// </summary>
        public bool HasTracedTables => tracedTables.Count > 0;

        /// <exception cref="ArgumentException">A symbol called <paramref name="name"/> is already defined; or <paramref name="entryPoint"/> is <see langword="true"/> and an entry point routine is alrady defined.</exception>
        public IRoutineBuilder DefineRoutine(string name, bool entryPoint, bool cleanStack)
        {
            bool isUSL = name == "UPDATE-STATUS-LINE";

            name = SanitizeSymbol(name);
            if (symbols.ContainsKey(name))
                throw new ArgumentException("Global symbol already defined: " + name, nameof(name));

            if (entryPoint && entryRoutine != null)
                throw new ArgumentException("Entry routine already defined");

            var result = new RoutineBuilder(this, name, entryPoint);
            symbols.Add(name, "routine");

            if (entryPoint)
                entryRoutine = result;
            if (isUSL)
                updateStatusLineHook = result;

            return result;
        }

        /// <exception cref="ArgumentException">A symbol called <paramref name="name"/> is already defined.</exception>
        public IObjectBuilder DefineObject(string name)
        {
            name = SanitizeSymbol(name);
            if (symbols.ContainsKey(name))
                throw new ArgumentException("Global symbol already defined: " + name, nameof(name));

            var result = new ObjectBuilder(name);
            objects.Add(result);
            symbols.Add(name, "object");
            return result;
        }

        /// <exception cref="ArgumentException">A symbol called <paramref name="name"/> is already defined.</exception>
        public IPropertyBuilder DefineProperty(string name)
        {
            name = "P_" + SanitizeSymbol(name);
            if (symbols.ContainsKey(name))
                throw new ArgumentException("Global symbol already defined: " + name, nameof(name));

            int num = MaxProperties - props.Count;  // property numbers start at 1

            var result = new PropertyBuilder(name, num);
            props.Add(name, result);
            symbols.Add(name, "property");
            return result;
        }

        /// <exception cref="ArgumentException">A symbol called <paramref name="name"/> is already defined.</exception>
        public IFlagBuilder DefineFlag(string name)
        {
            name = SanitizeSymbol(name);
            if (symbols.ContainsKey(name))
                throw new ArgumentException("Global symbol already defined: " + name, nameof(name));

            int max = MaxFlags - 1;     // flag numbers start at 0
            int num = max - flags.Count;

            var result = new FlagBuilder(name, num);
            flags.Add(name, result);
            symbols.Add(name, "flag");
            return result;
        }

        /// <exception cref="ArgumentException">A symbol called <paramref name="word">W_WORD</paramref> is already defined.</exception>
        public IWordBuilder DefineVocabularyWord(string word)
        {
            string name = "W_" + SanitizeSymbol(word.ToUpperInvariant());
            if (symbols.ContainsKey(name))
                throw new ArgumentException("Global symbol already defined: " + name, nameof(word));

            var result = new WordBuilder(name, word.ToLowerInvariant());
            vocabulary.Add(result);
            symbols.Add(name, "word");
            return result;
        }

        public void RemoveVocabularyWord(string word)
        {
            string name = "W_" + SanitizeSymbol(word.ToUpperInvariant());
            if (symbols.Remove(name))
            {
                vocabulary.RemoveAll(wb => wb.Name == name);
            }
        }

        public ICollection<char> SelfInsertingBreaks => siBreaks;

        public static string SanitizeString(string text)
        {
            // escape '"' as '\"', newline as '\n'
            var sb = new StringBuilder(text);

            for (int i = sb.Length - 1; i >= 0; i--)
            {
                if (sb[i] == '"')
                {
                    sb.Insert(i, '\\');
                }
                else if (sb[i] == '\n')
                {
                    sb[i] = 'n';
                    sb.Insert(i, '\\');
                }
            }

            return sb.ToString();
        }

        public static string SanitizeSymbol(string symbol)
        {
            var sb = new StringBuilder(symbol.Length);

            foreach (char c in symbol)
            {
                switch (c)
                {
                    case '.':
                        sb.Append("_PERIOD_");
                        break;
                    case ',':
                        sb.Append("_COMMA_");
                        break;
                    case '"':
                        sb.Append("_QUOTE_");
                        break;
                    case '\'':
                        sb.Append("_APOSTROPHE_");
                        break;
                    case '_':
                        sb.Append("_UNDERSCORE_");
                        break;
                    case '-':
                        sb.Append("__");
                        break;
                    case '+':
                        sb.Append("_PLUS_");
                        break;
                    case '=':
                        sb.Append("_EQUAL_");
                        break;
                    case '?':
                        sb.Append("_Q_");
                        break;
                    case var _ when char.IsDigit(c) && sb.Length == 0:
                        sb.Append('_');
                        sb.Append(c);
                        break;
                    case var _ when char.IsLetterOrDigit(c):
                        sb.Append(c);
                        break;
                    default:
                        sb.Append('_');
                        sb.Append(((ushort)c).ToString("x4"));
                        break;
                }
            }

            return sb.ToString();
        }

        public INumericOperand MakeOperand(int value)
        {
            switch (value)
            {
                case 0:
                    return ZERO;

                case 1:
                    return ONE;

                default:
                    if (numberPool.TryGetValue(value, out var result) == false)
                    {
                        result = new NumericOperand(value);
                        numberPool.Add(value, result);
                    }
                    return result;
            }
        }

        public IOperand MakeOperand(string value)
        {
            if (stringPool.TryGetValue(value, out var result) == false)
            {
                result = new ConstantLiteralOperand("STR_" + stringPool.Count);
                stringPool.Add(value, result);
            }
            return result;
        }

        // arbitrary limits
        public int MaxPropertyLength => 65536;
        public int MaxProperties => 65535;
        // NOTE: keep MaxFlags in sync with the object structure constants and codegen
        public int MaxFlags => 56;     // number fits in a byte for syntax lines
        public int MaxCallArguments => 65536;

        public INumericOperand Zero => ZERO;
        public INumericOperand One => ONE;
        public IConstantOperand VocabularyTable => VOCAB;

        public bool IsGloballyDefined(string name, [NotNullWhen(true)] out string? type) => symbols.TryGetValue(name, out type);

        public void Finish()
        {
            // finish main file
#if DEBUG
            writer.WriteLine();
            WritePeepholeStats();
#endif

            // write data
            writer.WriteLine();
            writer.WriteLine(INDENT + "section .data");

            // assembly constants
            FinishSymbols();

            // impure data
            FinishGlobals();
            FinishObjects();
            FinishImpureTables();
            FinishTerminatingChars();

            // pure data
            writer.WriteLine();
            writer.WriteLine(INDENT + "section .text");
            writer.WriteLine();

            FinishSyntax();
            FinishPureTables();
            FinishTracedTablesMetadata();
            FinishHooks();

            writer.WriteLine();
            RuntimeLib.DefineUsed(writer);

            // end of resident memory

            // write strings
            writer.WriteLine();

            FinishStrings();
            FinishMetadata();

            // done
            writer.Close();
            stream?.Close();
        }

        void FinishSymbols()
        {
            writer.WriteLine();

            // flags
            if (flags.Count > 0)
                writer.WriteLine();
            foreach (var pair in from fp in flags
                                 orderby fp.Value.Number
                                 select fp)
            {
                writer.WriteLine(INDENT + "{0} = {1}", pair.Key, pair.Value.Number);
                writer.WriteLine(INDENT + "FX_{0} = {1}",
                    pair.Key,
                    1 << (15 - (pair.Value.Number % 16)));
            }

            // properties
            if (props.Count > 0)
                writer.WriteLine();
            foreach (var pair in from pp in props
                                 orderby pp.Value.Number
                                 select pp)
                writer.WriteLine(INDENT + "{0} = {1}", pair.Key, pair.Value.Number);

            // constants
            if (constants.Count > 0)
                writer.WriteLine();
            foreach (var pair in from cp in constants
                                 where !transparentConstants.Contains(cp.Key)
                                 orderby cp.Key
                                 select cp)
            {
                writer.WriteLine(INDENT + "{0} = {1}", pair.Key, pair.Value.StripIndirect());
            }

            // release number
            if (!constants.ContainsKey("RELEASEID"))
            {
                var value = constants.ContainsKey("ZORKID") ? "ZORKID" : "0";
                writer.WriteLine(INDENT + "RELEASEID = {0}", value);
            }
        }

        void FinishObjects()
        {
            writer.WriteLine();

            // property defaults
            writer.WriteLine("property_defaults:");

            var propNums = Enumerable.Range(1, MaxProperties);
            var propDefaults = from p in props
                               orderby p.Value.Number
                               select new {
                                   num = p.Value.Number,
                                   name = p.Key,
                                   def = p.Value.DefaultValue?.StripIndirect()
                               };

            foreach (var row in propDefaults)
            {
                if (row.name != null)
                    writer.WriteLine(INDENT + "; {0}", row.name);
                else
                    writer.WriteLine(INDENT + "; Unused property #{0}", row.num);

                writer.WriteLine(INDENT + "dd {0}", (object?)row.def ?? "0");
            }

            // object structures
            if (objects.Count > 0)
                writer.WriteLine();

            var numFlagBytes = (MaxFlags + 7) / 8;
            ObjectBuilder? previous = null;
            foreach (var ob in objects)
            {
                writer.WriteLine("{0}:", ob.SymbolicName);
                writer.WriteLine(INDENT + "db 0x70");   // type ID
                writer.Write(INDENT + "db");
                foreach (var flagByte in ob.GetFlagBytes(numFlagBytes))
                {
                    writer.Write(" 0x{0:X2}", flagByte);
                }
                writer.WriteLine();
                writer.WriteLine(INDENT + "dd {0}", previous?.ToString() ?? "0");
                writer.WriteLine(INDENT + "dd objdesc_{0} ptbl_{0}", ob.SymbolicName);
                writer.WriteLine(INDENT + "dd {0} {1} {2}",
                    (object?)ob.Parent ?? "0",
                    (object?)ob.Sibling ?? "0",
                    (object?)ob.Child ?? "0");

                previous = ob;
            }

            // hardware names and property tables
            foreach (var ob in objects)
            {
                // abbrevs?.AddText(ob.DescriptiveName);
                writer.WriteLine();
                writer.WriteLine("objdesc_{0}:", ob.SymbolicName);
                writer.WriteLine(INDENT + "huffstr \"{0}\"", ob.DescriptiveName);
                writer.WriteLine("ptbl_{0}:", ob.SymbolicName);
                ob.WriteProperties(writer);
            }
        }

        void FinishGlobals()
        {
            writer.WriteLine();
            writer.WriteLine(INDENT + "; Global variables");

            // global variables
            foreach (var gb in globals)
            {
                writer.WriteLine("{0}:", gb.Name);
                writer.WriteLine(INDENT + "dd {0}", (object?)gb.DefaultValue?.StripIndirect() ?? "0");
            }
        }

        void FinishImpureTables()
        {
            writer.WriteLine();
            writer.WriteLine(INDENT + "; Impure user tables");

            // impure user tables
            foreach (var tb in impureTables)
            {
                writer.WriteLine();
                writer.WriteLine("{0}: ; {1} bytes", tb.Name, tb.Size);
                tb.WriteTo(writer);
            }
        }

        void FinishTerminatingChars()
        {
            if (impureTables.Concat(pureTables).FirstOrDefault(tb => tb.Name == "TCHARS") is TableBuilder tcharsTable)
            {
                terminatingCharsTable = tcharsTable;

                writer.WriteLine();
                writer.WriteLine(INDENT + "section .bss");
                writer.WriteLine(INDENT + "terminating_chars_translations: resd {0}", tcharsTable.Size);
            }
        }

        void FinishSyntax()
        {
            // vocabulary table
            writer.WriteLine();
            writer.WriteLine("si_breaks:");

            if (siBreaks.Count > 255)
                throw new InvalidOperationException("Too many self-inserting breaks");

            writer.WriteLine(INDENT + "db {0}", siBreaks.Count);
            foreach (char c in siBreaks)
            {
                if ((byte)c != c)
                {
                    throw new InvalidOperationException(
                        $"Self-inserting break character out of range (${(ushort)c:x4})");
                }

                writer.WriteLine(INDENT + "db {0}", (byte)c);
            }

            writer.WriteLine();
            writer.WriteLine("; vocabulary");

            const int wordBytes = 10;        // TODO: configurable vocab resolution for Glulx

            writer.WriteLine(INDENT + "VOCAB_RESOLUTION = {0}", wordBytes);
            writer.WriteLine(INDENT + "VOCAB:");
            if (vocabulary.Count == 0)
            {
                writer.WriteLine("vocab_entry_length: dd (VOCAB_RESOLUTION + 1)");
                writer.WriteLine("vocab_entry_count: dd 0");
                writer.WriteLine("vocab_entries: dd 0");
            }
            else
            {
                int dataBytes = vocabulary[0].Size;

                writer.WriteLine("vocab_entry_length: dd (VOCAB_RESOLUTION + {0} + 1)", dataBytes);
                writer.WriteLine("vocab_entry_count: dd {0}", vocabulary.Count);
                writer.WriteLine("vocab_entries:");
                vocabulary.Sort((a, b) => string.CompareOrdinal(a.Word, b.Word));

                Span<byte> encodedWord = stackalloc byte[wordBytes * 4];

                foreach (var wb in vocabulary)
                {
                    writer.WriteLine("{0}:", wb.Name);
                    writer.WriteLine(INDENT + "db 0x60");

                    int count = Encoding.Latin1.GetBytes(wb.Word.ToLowerInvariant(), encodedWord);
                    for (int i = count; i < wordBytes; i++)
                        encodedWord[i] = 0;

                    for (int i = 0; i < wordBytes; i += 8)
                    {
                        writer.Write(INDENT + "db");

                        for (int j = i; j < Math.Min(wordBytes, i + 8); j++)
                        {
                            writer.Write(' ');
                            char c = (char)encodedWord[j];
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

                        writer.WriteLine();
                    }
                    wb.WriteTo(writer);
                }
            }
        }

        void FinishPureTables()
        {
            writer.WriteLine();
            writer.WriteLine(INDENT + "; Pure user tables");

            // pure user tables
            foreach (var tb in pureTables)
            {
                writer.WriteLine();
                writer.WriteLine("{0}: ; {1} bytes", tb.Name, tb.Size);
                tb.WriteTo(writer);
            }
        }

        void FinishHooks()
        {
            writer.WriteLine();
            writer.WriteLine("; Library hooks");

            // update_status_line_hook
            writer.WriteLine("update_status_line_hook:");
            writer.WriteLine(INDENT + "function");

            if (updateStatusLineHook is IRoutineBuilder)
            {
                writer.WriteLine(INDENT + $"callf {updateStatusLineHook}");
            }
            else if (updateStatusLineHook is IGlobalBuilder)
            {
                writer.WriteLine(INDENT + $"callfi {RuntimeLib.Use(nameof(RuntimeLib.check_call))} {updateStatusLineHook}");
            }

            writer.WriteLine(INDENT + "return");

            // terminating chars hooks
            writer.WriteLine();
            writer.WriteLine("init_terminating_chars_hook:");
            writer.WriteLine(INDENT + "function");

            if (terminatingCharsTable != null)
            {
                writer.WriteLine(INDENT + $"callfi {RuntimeLib.Use(nameof(RuntimeLib.init_terminating_chars))} {terminatingCharsTable}");
            }

            writer.WriteLine(INDENT + "return");

            writer.WriteLine();
            writer.WriteLine("convert_terminating_char_hook:");
            writer.WriteLine(INDENT + "function");
            writer.WriteLine(INDENT + "local ch");

            if (terminatingCharsTable != null)
            {
                writer.WriteLine(INDENT + $"callfi {RuntimeLib.Use(nameof(RuntimeLib.convert_terminating_char))} ch -> ch");
            }

            writer.WriteLine(INDENT + "return ch");
        }

        void FinishTracedTablesMetadata()
        {
            if (tracedTables.Count == 0)
                return;

            writer.WriteLine();
            writer.WriteLine("; Traced table write metadata");
            writer.WriteLine("_traced_tables:");
            writer.WriteLine(INDENT + "dd {0} ; count", tracedTables.Count);

            foreach (var (tb, originalName) in tracedTables)
            {
                // Each entry: address, size, name string address
                var nameLabel = $"_traced_name_{tb.Name}";
                writer.WriteLine(INDENT + "dd {0} ; table address", tb.Name);
                writer.WriteLine(INDENT + "dd {0} ; table size", tb.Size);
                writer.WriteLine(INDENT + "dd {0} ; table name", nameLabel);
            }

            // Define the name strings for table names (use original ZIL names)
            foreach (var (tb, originalName) in tracedTables)
            {
                var nameLabel = $"_traced_name_{tb.Name}";
                writer.WriteLine("{0}: huffstr \"{1}\"", nameLabel, originalName);
            }

            // Define the trace message strings
            writer.WriteLine("_trace_msg_word: huffstr \" word \"");
            writer.WriteLine("_trace_msg_byte: huffstr \" byte \"");
            writer.WriteLine("_trace_msg_unaligned: huffstr \" (+\"");
            writer.WriteLine("_trace_msg_bytes_suffix: huffstr \" bytes)\"");
            writer.WriteLine("_trace_msg_addr: huffstr \" (addr \"");
            writer.WriteLine("_trace_msg_dash: huffstr \"-\"");
            writer.WriteLine("_trace_msg_value: huffstr \"), value \"");
            writer.WriteLine("_trace_msg_newline: huffstr \"\\n\"");
            writer.WriteLine("_trace_msg_prefix: huffstr \"[TRACE] Write to \"");
        }

        void FinishStrings()
        {
            // strings
            writer.WriteLine();
            writer.WriteLine(INDENT + "; Strings");

            foreach (var (text, symbol) in stringPool.OrderBy(p => p.Key))
            {
                // abbrevs?.AddText(text);
                writer.WriteLine("{0}: huffstr \"{1}\"", symbol, SanitizeString(text));
            }
        }

        void FinishMetadata()
        {
            writer.WriteLine();
            writer.WriteLine(INDENT + "; Metadata");
            writer.WriteLine(INDENT + "db \"ZILF\"");
            writer.WriteLine("metadata_releaseid:");
            writer.WriteLine(INDENT + "dw RELEASEID");
            writer.WriteLine("metadata_serial:");
            writer.WriteLine(INDENT + "db \"{0}\"", DateTime.Now.ToString("yyMMdd"));
        }

        internal void WriteOutput(string str)
        {
            writer.WriteLine(str);
        }

#if DEBUG
        internal void RecordPeepholeStats(IEnumerable<PeepholeOptimizationStat> stats)
        {
            foreach (var stat in stats)
            {
                if (stat.Applications == 0)
                    continue;

                if (peepholeStats.TryGetValue(stat.Name, out var aggregate))
                {
                    peepholeStats[stat.Name] = (
                        aggregate.Applications + stat.Applications,
                        aggregate.InstructionsSaved + stat.InstructionsSaved);
                }
                else
                {
                    peepholeStats.Add(stat.Name, (stat.Applications, stat.InstructionsSaved));
                }
            }
        }

        void WritePeepholeStats()
        {
            if (peepholeStats.Count == 0)
                return;

            writer.WriteLine(INDENT + "; Peephole optimization statistics (debug build)");

            foreach (var entry in peepholeStats
                     .OrderByDescending(static e => e.Value.InstructionsSaved)
                     .ThenBy(static e => e.Key, StringComparer.Ordinal))
            {
                var name = entry.Key;
                var (applications, saved) = entry.Value;
                var instructionWord = saved == 1 ? "instruction" : "instructions";
                writer.WriteLine(INDENT + $";   {name}: applied {applications}x, saved {saved} {instructionWord}");
            }

            writer.WriteLine();
        }
#endif
    }
}
