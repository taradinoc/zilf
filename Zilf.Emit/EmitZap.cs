/* Copyright 2010, 2012 Jesse McGrew
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
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using Zilf.Emit;

namespace Zilf.Emit.Zap
{
    struct ZapCode
    {
        public string Text;
        public string DebugText;

        public override string ToString()
        {
            return Text;
        }
    }

    public interface IZapStreamFactory
    {
        Stream CreateMainStream();
        Stream CreateFrequentWordsStream();
        Stream CreateDataStream();
        Stream CreateStringStream();

        string GetMainFileName(bool withExt);
        string GetDataFileName(bool withExt);
        string GetFrequentWordsFileName(bool withExt);
        string GetStringFileName(bool withExt);

        bool FrequentWordsFileExists { get; }
    }

    public class GameOptions : IGameOptions
    {
        public sealed class V3 : GameOptions
        {
            public bool TimeStatusLine { get; set; }
            public bool SoundEffects { get; set; }
        }

        public sealed class V4 : GameOptions
        {
            public bool SoundEffects { get; set; }
        }

        public sealed class V5 : GameOptions
        {
            public bool DisplayOps { get; set; }
            public bool Undo { get; set; }
            public bool Mouse { get; set; }
            public bool Color { get; set; }
            public bool SoundEffects { get; set; }
        }
    }

    internal class DummyGameOptions : IGameOptions { }

    public class GameBuilder : IGameBuilder
    {
        private const string INDENT = "\t";

        internal static readonly LiteralOperand ZERO = new LiteralOperand("0");
        internal static readonly LiteralOperand ONE = new LiteralOperand("1");

        // all global names go in here
        private Dictionary<string, string> symbols = new Dictionary<string, string>(250);

        private List<RoutineBuilder> routines = new List<RoutineBuilder>(100);
        private List<ObjectBuilder> objects = new List<ObjectBuilder>(100);
        private Dictionary<string, PropertyBuilder> props = new Dictionary<string, PropertyBuilder>(32);
        private Dictionary<string, FlagBuilder> flags = new Dictionary<string, FlagBuilder>(32);
        private Dictionary<string, IOperand> constants = new Dictionary<string, IOperand>(100);
        private List<GlobalBuilder> globals = new List<GlobalBuilder>(100);
        private List<TableBuilder> impureTables = new List<TableBuilder>(10);
        private List<TableBuilder> pureTables = new List<TableBuilder>(10);
        private List<WordBuilder> vocabulary = new List<WordBuilder>(100);
        private HashSet<char> siBreaks = new HashSet<char>();
        private Dictionary<string, IOperand> stringPool = new Dictionary<string, IOperand>(100);
        private Dictionary<int, IOperand> numberPool = new Dictionary<int, IOperand>(50);

        private readonly IZapStreamFactory streamFactory;
        internal readonly int zversion;
        internal readonly DebugFileBuilder debug;
        private readonly GameOptions options;

        private Stream stream;
        private TextWriter writer;

        public GameBuilder(int zversion, string outFile, bool wantDebugInfo, GameOptions options = null)
            : this(zversion, new ZapStreamFactory(outFile), wantDebugInfo, options)
        {
        }

        public GameBuilder(int zversion, IZapStreamFactory streamFactory, bool wantDebugInfo, GameOptions options = null)
        {
            if (!IsSupportedZversion(zversion))
                throw new ArgumentOutOfRangeException("zversion", "Unsupported Z-machine version");
            if (streamFactory == null)
                throw new ArgumentNullException("streamFactory");

            this.zversion = zversion;
            this.streamFactory = streamFactory;

            var optionsType = GetOptionsTypeForZVersion(zversion);

            if (options != null)
            {
                const string SOptionsNotCompatible = "Options not compatible with this Z-machine version";

                if (optionsType.IsAssignableFrom(options.GetType()))
                {
                    this.options = options;
                }
                else
                {
                    throw new ArgumentException(SOptionsNotCompatible, "options");
                }
            }
            else
            {
                this.options = (GameOptions)Activator.CreateInstance(optionsType);
            }

            debug = wantDebugInfo ? new DebugFileBuilder() : null;

            stream = streamFactory.CreateMainStream();
            writer = new StreamWriter(stream);

            Begin();
        }

        private static Type GetOptionsTypeForZVersion(int zversion)
        {
            switch (zversion)
            {
                case 3:
                    return typeof(GameOptions.V3);

                default:
                    return typeof(GameOptions);
            }
        }

        private static bool IsSupportedZversion(int zversion)
        {
            return zversion >= 1 && zversion <= 8;
        }

        private void Begin()
        {
            if (zversion == 3)
            {
                var v3options = (GameOptions.V3)options;
                if (v3options.TimeStatusLine)
                {
                    writer.WriteLine(INDENT + ".TIME");
                }
                if (v3options.SoundEffects)
                {
                    writer.WriteLine(INDENT + ".SOUND");
                }
            }
            else if (zversion > 3)
            {
                writer.WriteLine(INDENT + ".NEW {0}", zversion);

                if (zversion == 4)
                {
                    var v4options = (GameOptions.V4)options;
                    if (v4options.SoundEffects)
                    {
                        writer.WriteLine(INDENT + ".SOUND");
                    }
                }
                else if (zversion > 4)
                {
                    // build the header
                    writer.WriteLine();
                    writer.WriteLine(INDENT + ".BYTE {0}", zversion);
                    writer.WriteLine(INDENT + ".BYTE FLAGS");
                    writer.WriteLine(INDENT + ".WORD RELEASEID");
                    writer.WriteLine(INDENT + ".WORD ENDLOD");
                    writer.WriteLine(INDENT + ".WORD START");
                    writer.WriteLine(INDENT + ".WORD VOCAB");
                    writer.WriteLine(INDENT + ".WORD OBJECT");
                    writer.WriteLine(INDENT + ".WORD GLOBAL");
                    writer.WriteLine(INDENT + ".WORD IMPURE");
                    writer.WriteLine(INDENT + ".WORD FLAGS2");
                    writer.WriteLine(INDENT + ".BYTE 0,0,0,0,0,0");     // serial
                    writer.WriteLine(INDENT + ".WORD WORDS");
                    writer.WriteLine(INDENT + ".WORD 0,0");             // length, checksum
                    // pad to 64 bytes
                    writer.WriteLine(INDENT + ".WORD 0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0");
                }
            }

            writer.WriteLine(INDENT + ".INSERT \"{0}\"", streamFactory.GetFrequentWordsFileName(false));
            writer.WriteLine(INDENT + ".INSERT \"{0}\"", streamFactory.GetDataFileName(false));
        }

        public IDebugFileBuilder DebugFile
        {
            get { return debug; }
        }

        public IGameOptions Options
        {
            get { return options; }
        }

        public IOperand DefineConstant(string name, IOperand value)
        {
            name = SanitizeSymbol(name);
            if (symbols.ContainsKey(name))
                throw new ArgumentException("Global symbol already defined: " + name, "name");

            constants.Add(name, value);
            symbols.Add(name, "constant");
            return new LiteralOperand(name);
        }

        public IGlobalBuilder DefineGlobal(string name)
        {
            name = SanitizeSymbol(name);
            if (symbols.ContainsKey(name))
                throw new ArgumentException("Global symbol already defined: " + name, "name");

            GlobalBuilder gb = new GlobalBuilder(name);
            globals.Add(gb);
            symbols.Add(name, "global");
            return gb;
        }

        public ITableBuilder DefineTable(string name, bool pure)
        {
            if (name == null)
                name = "T?" + Convert.ToString(pureTables.Count + impureTables.Count);

            if (symbols.ContainsKey(name))
                throw new ArgumentException("Global symbol already defined: " + name, "name");

            TableBuilder tb = new TableBuilder(name);
            if (pure)
                pureTables.Add(tb);
            else
                impureTables.Add(tb);
            symbols.Add(name, "table");
            return tb;
        }

        public IRoutineBuilder DefineRoutine(string name, bool entryPoint, bool cleanStack)
        {
            name = SanitizeSymbol(name);
            if (symbols.ContainsKey(name))
                throw new ArgumentException("Global symbol already defined: " + name, "name");

            RoutineBuilder result = new RoutineBuilder(this, name, entryPoint, cleanStack);
            routines.Add(result);
            symbols.Add(name, "routine");
            return result;
        }

        public IObjectBuilder DefineObject(string name)
        {
            name = SanitizeSymbol(name);
            if (symbols.ContainsKey(name))
                throw new ArgumentException("Global symbol already defined: " + name, "name");

            ObjectBuilder result = new ObjectBuilder(this, 1 + objects.Count, name);
            objects.Add(result);
            symbols.Add(name, "object");
            return result;
        }

        public IPropertyBuilder DefineProperty(string name)
        {
            name = "P?" + SanitizeSymbol(name);
            if (symbols.ContainsKey(name))
                throw new ArgumentException("Global symbol already defined: " + name, "name");

            int max = (zversion >= 4) ? 63 : 31;
            int num = max - props.Count;

            PropertyBuilder result = new PropertyBuilder(name, num);
            props.Add(name, result);
            symbols.Add(name, "property");
            return result;
        }

        public IFlagBuilder DefineFlag(string name)
        {
            name = SanitizeSymbol(name);
            if (symbols.ContainsKey(name))
                throw new ArgumentException("Global symbol already defined: " + name, "name");

            int max = (zversion >= 4) ? 47 : 31;
            int num = max - flags.Count;

            FlagBuilder result = new FlagBuilder(name, num);
            flags.Add(name, result);
            symbols.Add(name, "flag");
            return result;
        }

        public IWordBuilder DefineVocabularyWord(string word)
        {
            string name = "W?" + SanitizeSymbol(word.ToUpper());
            if (symbols.ContainsKey(name))
                throw new ArgumentException("Global symbol already defined: " + name, "word");

            WordBuilder result = new WordBuilder(name, word.ToLower());
            vocabulary.Add(result);
            symbols.Add(name, "word");
            return result;
        }

        public ICollection<char> SelfInsertingBreaks
        {
            get { return siBreaks; }
        }

        public static string SanitizeString(string text)
        {
            // escape '"' as '""'
            StringBuilder sb = new StringBuilder(text);

            for (int i = sb.Length - 1; i >= 0; i--)
                if (sb[i] == '"')
                    sb.Insert(i, '"');

            return sb.ToString();
        }

        public static string SanitizeSymbol(string symbol)
        {
            if (symbol == ".")
                return "PERIOD";
            else if (symbol == ",")
                return "COMMA";
            else if (symbol == "\"")
                return "QUOTE";
            else if (symbol == "'")
                return "APOSTROPHE";

            StringBuilder sb = new StringBuilder(symbol);

            for (int i = 0; i < sb.Length; i++)
            {
                char c = sb[i];
                if (char.IsLetterOrDigit(c) ||
                    c == '?' || c == '#' || c == '-')
                    continue;

                sb[i] = '$';
                sb.Insert(i + 1, ((ushort)c).ToString("x4"));
                i += 4;
            }

            return sb.ToString();
        }

        public IOperand MakeOperand(int value)
        {
            switch (value)
            {
                case 0:
                    return ZERO;

                case 1:
                    return ONE;

                default:
                    IOperand result;
                    if (numberPool.TryGetValue(value, out result) == false)
                    {
                        result = new NumericOperand(value);
                        numberPool.Add(value, result);
                    }
                    return result;
            }
        }

        public IOperand MakeOperand(string value)
        {
            IOperand result;
            if (stringPool.TryGetValue(value, out result) == false)
            {
                result = new LiteralOperand("STR?" + stringPool.Count);
                stringPool.Add(value, result);
            }
            return result;
        }

        public int MaxPropertyLength
        {
            get { return zversion > 3 ? 64 : 8; }
        }

        public int MaxProperties
        {
            get { return zversion > 3 ? 63 : 31; }
        }

        public int MaxFlags
        {
            get { return zversion > 3 ? 48 : 32; }
        }

        public int MaxCallArguments
        {
            get { return zversion > 3 ? 7 : 3; }
        }

        public IOperand Zero
        {
            get { return ZERO; }
        }

        public IOperand One
        {
            get { return ONE; }
        }

        public void Finish()
        {
            // finish main file
            writer.WriteLine();
            writer.WriteLine(INDENT + ".INSERT \"{0}\"", streamFactory.GetStringFileName(false));
            writer.WriteLine(INDENT + ".END");
            writer.Close();

            // write data file
            stream = streamFactory.CreateDataStream();
            writer = new StreamWriter(stream);

            writer.WriteLine(INDENT + "; Data to accompany {0}", streamFactory.GetMainFileName(true));

            // assembly constants
            FinishSymbols();

            // impure data
            FinishGlobals();
            FinishObjects();
            FinishImpureTables();

            // pure data
            writer.WriteLine();
            writer.WriteLine("IMPURE::");   // sic

            FinishSyntax();
            FinishPureTables();

            // end of resident memory
            writer.WriteLine();
            writer.WriteLine("ENDLOD::");

            // debug records
            if (debug != null)
            {
                foreach (var pair in from p in debug.Files
                                     orderby p.Value
                                     select p)
                    writer.WriteLine(INDENT + ".DEBUG-FILE {0},\"{1}\",\"{2}\"",
                        pair.Value,
                        Path.GetFileNameWithoutExtension(pair.Key),
                        pair.Key);
                foreach (string name in from f in flags.Keys orderby f select f)
                    writer.WriteLine(INDENT + ".DEBUG-ATTR {0},\"{0}\"", name);
                foreach (string name in from p in props.Keys orderby p select p)
                    writer.WriteLine(INDENT + ".DEBUG-PROP {0},\"{0}\"", name);
                foreach (string name in from g in globals orderby g.Name select g.Name)
                    writer.WriteLine(INDENT + ".DEBUG-GLOBAL {0},\"{0}\"", name);
                foreach (string name in from t in impureTables.Concat(pureTables)
                                        orderby t.Name
                                        select t.Name)
                    writer.WriteLine(INDENT + ".DEBUG-ARRAY {0},\"{0}\"", name);
                foreach (string line in debug.StoredLines)
                    writer.WriteLine(INDENT + line);
            }

            // end of data file
            writer.WriteLine(INDENT + ".ENDI");
            writer.Close();

            // write strings file
            stream = streamFactory.CreateStringStream();
            writer = new StreamWriter(stream);

            writer.WriteLine(INDENT + "; Strings to accompany {0}", streamFactory.GetMainFileName(true));

            FinishStrings();

            writer.WriteLine(INDENT + ".ENDI");
            writer.Close();

            // write frequent words file if necessary
            if (!streamFactory.FrequentWordsFileExists)
            {
                stream = streamFactory.CreateFrequentWordsStream();
                writer = new StreamWriter(stream);

                writer.WriteLine(INDENT + "; Dummy frequent words file for {0}", streamFactory.GetMainFileName(true));
                writer.WriteLine(INDENT + ".FSTR FSTR?DUMMY,\"\"");
                writer.WriteLine("WORDS::");
                for (int i = 0; i < 96; i++)
                    writer.WriteLine(INDENT + "FSTR?DUMMY");

                writer.WriteLine(INDENT + ".ENDI");
                writer.Close();
            }

            // done
            writer = null;
            stream = null;
        }

        private void FinishSymbols()
        {
            // XXX header flags
            writer.WriteLine();
            writer.WriteLine(INDENT + "FLAGS=0");

            ushort flags2 = 0;

            switch (zversion)
            {
                case 5:
                case 7:
                case 8:
                    var v5options = (GameOptions.V5)options;
                    if (v5options.DisplayOps)
                    {
                        flags2 |= 8;
                    }
                    if (v5options.Undo)
                    {
                        flags2 |= 16;
                    }
                    if (v5options.Mouse)
                    {
                        flags2 |= 32;
                    }
                    if (v5options.Color)
                    {
                        flags2 |= 64;
                    }
                    if (v5options.SoundEffects)
                    {
                        flags2 |= 128;
                    }
                    break;

                case 6:
                    //XXX
                    break;
            }

            writer.WriteLine(INDENT + "FLAGS2={0}", flags2);

            // flags
            if (flags.Count > 0)
                writer.WriteLine();
            foreach (var pair in from fp in flags
                                 orderby fp.Value.Number
                                 select fp)
            {
                writer.WriteLine(INDENT + "{0}={1}", pair.Key, pair.Value.Number);
                writer.WriteLine(INDENT + "FX?{0}={1}",
                    pair.Key,
                    1 << (15 - (pair.Value.Number % 16)));
            }

            // properties
            if (props.Count > 0)
                writer.WriteLine();
            foreach (var pair in from pp in props
                                 orderby pp.Value.Number
                                 select pp)
                writer.WriteLine(INDENT + "{0}={1}", pair.Key, pair.Value.Number);

            // constants
            if (constants.Count > 0)
                writer.WriteLine();
            foreach (var pair in from cp in constants
                                 orderby cp.Key
                                 select cp)
                writer.WriteLine(INDENT + "{0}={1}", pair.Key, pair.Value);
        }

        private void FinishObjects()
        {
            writer.WriteLine();
            writer.WriteLine("OBJECT:: .TABLE");

            // property defaults
            var propNums = Enumerable.Range(1, (zversion >= 4) ? 63 : 31);
            var propDefaults = from num in propNums
                               join p in this.props on num equals p.Value.Number into propGroup
                               from prop in propGroup.DefaultIfEmpty()
                               let name = prop.Key
                               let def = prop.Value == null ? null : prop.Value.DefaultValue
                               select new { num, name, def };

            foreach (var row in propDefaults)
            {
                if (row.name != null)
                    writer.WriteLine(INDENT + "; {0}", row.name);
                else
                    writer.WriteLine(INDENT + "; Unused property #{0}", row.num);

                writer.WriteLine(INDENT + ".WORD {0}", (object)row.def ?? "0");
            }

            // object structures
            if (objects.Count > 0)
                writer.WriteLine();

            foreach (ObjectBuilder ob in objects)
                writer.WriteLine(INDENT + ".OBJECT {0},{1},{2}{3},{4},{5},{6},{7}",
                    ob.SymbolicName,
                    ob.Flags1,
                    ob.Flags2,
                    (zversion < 4) ? "" : "," + ob.Flags3,
                    (object)ob.Parent ?? "0",
                    (object)ob.Sibling ?? "0",
                    (object)ob.Child ?? "0",
                    "?PTBL?" + ob.SymbolicName);

            writer.WriteLine(INDENT + ".ENDT");

            // property tables
            foreach (ObjectBuilder ob in objects)
            {
                writer.WriteLine();
                writer.WriteLine("?PTBL?{0}:: .TABLE", ob.SymbolicName);
                ob.WriteProperties(writer);
                writer.WriteLine(INDENT + ".ENDT");
            }
        }

        private void MoveGlobal(string name, int index)
        {
            int curIndex = globals.FindIndex(g => g.Name == name);
            if (curIndex >= 0 && curIndex != index)
            {
                GlobalBuilder gb = globals[curIndex];
                globals.RemoveAt(curIndex);
                globals.Insert(index, gb);
            }
        }

        private void FinishGlobals()
        {
            writer.WriteLine();
            writer.WriteLine("GLOBAL:: .TABLE");

            // V3 needs HERE, SCORE, and MOVES to be first
            if (zversion < 4)
            {
                MoveGlobal("HERE", 0);
                MoveGlobal("SCORE", 1);
                MoveGlobal("MOVES", 2);
            }

            // global variables
            foreach (GlobalBuilder gb in globals)
                writer.WriteLine(INDENT + ".GVAR {0}={1}", gb.Name, (object)gb.DefaultValue ?? "0");

            writer.WriteLine(INDENT + ".ENDT");
        }

        private void FinishImpureTables()
        {
            // impure user tables
            foreach (TableBuilder tb in impureTables)
            {
                writer.WriteLine();
                writer.WriteLine("{0}:: .TABLE {1}", tb.Name, tb.Size);
                tb.WriteTo(writer);
                writer.WriteLine(INDENT + ".ENDT");
            }
        }

        private void FinishSyntax()
        {
            // vocabulary table
            writer.WriteLine();
            writer.WriteLine("VOCAB:: .TABLE");

            if (siBreaks.Count > 255)
                throw new InvalidOperationException("Too many self-inserting breaks");

            writer.WriteLine(INDENT + ".BYTE {0}", siBreaks.Count);
            foreach (char c in siBreaks)
            {
                if ((byte)c != c)
                    throw new InvalidOperationException(string.Format(
                        "Self-inserting break character out of range (${0:x4})", (ushort)c));

                writer.WriteLine(INDENT + ".BYTE {0}", (byte)c);
            }

            if (vocabulary.Count == 0)
            {
                writer.WriteLine(INDENT + ".BYTE 7");
                writer.WriteLine(INDENT + ".WORD 0");
            }
            else
            {
                int zwordBytes = (zversion < 4) ? 4 : 6;
                int dataBytes = vocabulary[0].Size;

                writer.WriteLine(INDENT + ".BYTE {0}", zwordBytes + dataBytes);
                writer.WriteLine(INDENT + ".WORD {0}", vocabulary.Count);

                writer.WriteLine(INDENT + ".VOCBEG {0},{1}", zwordBytes + dataBytes, zwordBytes);
                vocabulary.Sort((a, b) => a.Word.CompareTo(b.Word));
                foreach (WordBuilder wb in vocabulary)
                {
                    writer.WriteLine("{0}:: .ZWORD \"{1}\"", wb.Name, GameBuilder.SanitizeString(wb.Word));
                    wb.WriteTo(writer);
                }
                writer.WriteLine(INDENT + ".VOCEND");
            }

            writer.WriteLine(INDENT + ".ENDT");
        }

        private void FinishPureTables()
        {
            // pure user tables
            foreach (TableBuilder tb in pureTables)
            {
                writer.WriteLine();
                writer.WriteLine("{0}:: .TABLE {1}", tb.Name, tb.Size);
                tb.WriteTo(writer);
                writer.WriteLine(INDENT + ".ENDT");
            }
        }

        private void FinishStrings()
        {
            // strings
            if (stringPool.Count > 0)
                writer.WriteLine();

            var query = from p in stringPool
                        orderby p.Key
                        select p;

            foreach (var pair in query)
                writer.WriteLine(INDENT + ".GSTR {0},\"{1}\"", pair.Value, SanitizeString(pair.Key));
        }

        internal void WriteOutput(string str)
        {
            writer.WriteLine(str);
        }

        internal void WriteOutput(MemoryStream mstr)
        {
            writer.Flush();
            stream.Write(mstr.GetBuffer(), 0, (int)mstr.Length);
        }
    }

    class NumericOperand : IOperand
    {
        private readonly int value;

        public NumericOperand(int value)
        {
            this.value = value;
        }

        public override string ToString()
        {
            return value.ToString();
        }
    }

    class LiteralOperand : IOperand
    {
        private readonly string literal;

        public LiteralOperand(string literal)
        {
            this.literal = literal;
        }

        public override string ToString()
        {
            return literal;
        }
    }

    class VariableOperand : LiteralOperand, IVariable
    {
        public VariableOperand(string literal)
            : base(literal)
        {
        }

        public IIndirectOperand Indirect
        {
            get { return new IndirectOperand(this); }
        }
    }

    class IndirectOperand : IIndirectOperand
    {
        public IndirectOperand(IVariable variable)
        {
            this.Variable = variable;
        }

        public IVariable Variable { get; private set; }

        public override string ToString()
        {
            return "'" + Variable.ToString();
        }
    }

    class Label : ILabel
    {
        private readonly string symbol;

        public Label(string symbol)
        {
            this.symbol = symbol;
        }

        public override string ToString()
        {
            return symbol;
        }
    }

    class RoutineBuilder : IRoutineBuilder
    {
        internal static readonly Label RTRUE = new Label("TRUE");
        internal static readonly Label RFALSE = new Label("FALSE");
        internal static readonly VariableOperand STACK = new VariableOperand("STACK");
        private const string INDENT = "\t";

        private readonly GameBuilder game;
        private readonly string name;
        private readonly bool entryPoint, cleanStack;

        internal DebugLineRef defnStart, defnEnd;

        private PeepholeBuffer<ZapCode> peep;
        private int nextLabel = 0;
        private string pendingDebugText;

        private List<LocalBuilder> requiredParams = new List<LocalBuilder>();
        private List<LocalBuilder> optionalParams = new List<LocalBuilder>();
        private List<LocalBuilder> locals = new List<LocalBuilder>();

        public RoutineBuilder(GameBuilder game, string name, bool entryPoint, bool cleanStack)
        {
            this.game = game;
            this.name = name;
            this.entryPoint = entryPoint;
            this.cleanStack = cleanStack;

            peep = new PeepholeBuffer<ZapCode>();
            peep.Combiner = new PeepholeCombiner(this);
        }

        public override string ToString()
        {
            return name;
        }

        public bool CleanStack
        {
            get { return cleanStack; }
        }

        public ILabel RTrue
        {
            get { return RTRUE; }
        }

        public ILabel RFalse
        {
            get { return RFALSE; }
        }

        public IVariable Stack
        {
            get { return STACK; }
        }

        private bool LocalExists(string name)
        {
            return requiredParams.Concat(optionalParams).Concat(locals).Any(lb => lb.Name == name);
        }

        public ILocalBuilder DefineRequiredParameter(string name)
        {
            if (entryPoint)
                throw new InvalidOperationException("Entry point may not have parameters");
            if (LocalExists(name))
                throw new ArgumentException("Local variable already exists: " + name, "name");

            LocalBuilder local = new LocalBuilder(name);
            requiredParams.Add(local);
            return local;
        }

        public ILocalBuilder DefineOptionalParameter(string name)
        {
            if (entryPoint)
                throw new InvalidOperationException("Entry point may not have parameters");
            if (LocalExists(name))
                throw new ArgumentException("Local variable already exists: " + name, "name");

            LocalBuilder local = new LocalBuilder(name);
            optionalParams.Add(local);
            return local;
        }

        public ILocalBuilder DefineLocal(string name)
        {
            if (entryPoint)
                throw new InvalidOperationException("Entry point may not have local variables");
            if (LocalExists(name))
                throw new ArgumentException("Local variable already exists: " + name, "name");

            LocalBuilder local = new LocalBuilder(name);
            locals.Add(local);
            return local;
        }

        public ILabel DefineLabel()
        {
            return new Label("?L" + (nextLabel++).ToString());
        }

        public void MarkLabel(ILabel label)
        {
            peep.MarkLabel(label);
        }

        private void AddLine(string code, ILabel target, PeepholeLineType type)
        {
            ZapCode zc;
            zc.Text = code;
            zc.DebugText = pendingDebugText;
            pendingDebugText = null;

            peep.AddLine(zc, target, type);
        }

        public void MarkSequencePoint(DebugLineRef lineRef)
        {
            if (game.debug != null)
                pendingDebugText = string.Format(
                    ".DEBUG-LINE {0},{1},{2}",
                    game.debug.GetFileNumber(lineRef.File),
                    lineRef.Line,
                    lineRef.Column);
        }

        public void Branch(ILabel label)
        {
            AddLine("JUMP", label, PeepholeLineType.BranchAlways);
        }

        public bool HasArgCount
        {
            get { return game.zversion >= 5; }
        }

        public void Branch(Condition cond, IOperand left, IOperand right, ILabel label, bool polarity)
        {
            string opcode;
            bool leftVar = false, nullary = false, unary = false;

            switch (cond)
            {
                case Condition.DecCheck:
                    opcode = "DLESS?";
                    leftVar = true;
                    break;
                case Condition.Greater:
                    opcode = "GRTR?";
                    break;
                case Condition.IncCheck:
                    opcode = "IGRTR?";
                    leftVar = true;
                    break;
                case Condition.Inside:
                    opcode = "IN?";
                    break;
                case Condition.Less:
                    opcode = "LESS?";
                    break;
                case Condition.TestAttr:
                    opcode = "FSET?";
                    break;
                case Condition.TestBits:
                    opcode = "BTST";
                    break;

                case Condition.ArgProvided:
                    opcode = "ASSIGNED?";
                    leftVar = true;
                    unary = true;
                    break;

                case Condition.Verify:
                    opcode = "VERIFY";
                    nullary = true;
                    break;
                case Condition.Original:
                    opcode = "ORIGINAL?";
                    nullary = true;
                    break;

                default:
                    throw new NotImplementedException();
            }

            if (leftVar && !(left is IVariable))
                throw new ArgumentException("This condition requires a variable", "left");

            if (nullary)
            {
                if (left != null || right != null)
                    throw new ArgumentException("Expected no operands for nullary condition");
            }
            else if (unary)
            {
                if (right != null)
                    throw new ArgumentException("Expected only one operand for unary condition", "right");
            }
            else
            {
                if (right == null)
                    throw new ArgumentException("Expected two operands for binary condition", "right");
            }

            AddLine(
                nullary ?
                    opcode :
                unary ?
                    string.Format("{0} {1}{2}", opcode, leftVar ? "'" : "", left) :
                    string.Format("{0} {1}{2},{3}", opcode, leftVar ? "'" : "", left, right),
                label,
                polarity ? PeepholeLineType.BranchPositive : PeepholeLineType.BranchNegative);
        }

        public void BranchIfZero(IOperand operand, ILabel label, bool polarity)
        {
            AddLine(
                "ZERO? " + operand.ToString(),
                label,
                polarity ? PeepholeLineType.BranchPositive : PeepholeLineType.BranchNegative);
        }

        public void BranchIfEqual(IOperand value, IOperand option1, ILabel label, bool polarity)
        {
            AddLine(
                string.Format("EQUAL? {0},{1}", value, option1),
                label,
                polarity ? PeepholeLineType.BranchPositive : PeepholeLineType.BranchNegative);
        }

        public void BranchIfEqual(IOperand value, IOperand option1, IOperand option2, ILabel label, bool polarity)
        {
            AddLine(
                string.Format("EQUAL? {0},{1},{2}", value, option1, option2),
                label,
                polarity ? PeepholeLineType.BranchPositive : PeepholeLineType.BranchNegative);
        }

        public void BranchIfEqual(IOperand value, IOperand option1, IOperand option2, IOperand option3, ILabel label, bool polarity)
        {
            AddLine(
                string.Format("EQUAL? {0},{1},{2},{3}", value, option1, option2, option3),
                label,
                polarity ? PeepholeLineType.BranchPositive : PeepholeLineType.BranchNegative);
        }

        public void Return(IOperand result)
        {
            if (result == GameBuilder.ONE)
                AddLine("RTRUE", RTRUE, PeepholeLineType.BranchAlways);
            else if (result == GameBuilder.ZERO)
                AddLine("RFALSE", RFALSE, PeepholeLineType.BranchAlways);
            else if (result == STACK)
                AddLine("RSTACK", null, PeepholeLineType.Terminator);
            else
                AddLine("RETURN " + result.ToString(), null, PeepholeLineType.Terminator);
        }

        public bool HasUndo
        {
            get { return game.zversion >= 5; }
        }

        public void EmitNullary(NullaryOp op, IVariable result)
        {
            string opcode;

            switch (op)
            {
                case NullaryOp.RestoreUndo:
                    opcode = "IRESTORE";
                    break;
                case NullaryOp.SaveUndo:
                    opcode = "ISAVE";
                    break;
                case NullaryOp.ShowStatus:
                    opcode = "USL";
                    break;
                case NullaryOp.Catch:
                    opcode = "CATCH";
                    break;
                default:
                    throw new NotImplementedException();
            }

            AddLine(
                string.Format("{0}{1}{2}",
                    opcode,
                    result == null ? "" : " >",
                    (object)result ?? ""),
                null,
                PeepholeLineType.Plain);
        }

        public void EmitUnary(UnaryOp op, IOperand value, IVariable result)
        {
            if (op == UnaryOp.Neg)
            {
                AddLine(
                    string.Format("SUB 0,{0}{1}{2}",
                        value,
                        result == null ? "" : " >",
                        (object)result ?? ""),
                    null,
                    PeepholeLineType.Plain);
                return;
            }

            string opcode;
            bool pred = false;

            switch (op)
            {
                case UnaryOp.Not:
                    opcode = "BCOM";
                    break;
                case UnaryOp.GetParent:
                    opcode = "LOC";
                    break;
                case UnaryOp.GetPropSize:
                    opcode = "PTSIZE";
                    break;
                case UnaryOp.LoadIndirect:
                    opcode = "VALUE";
                    break;
                case UnaryOp.Random:
                    opcode = "RANDOM";
                    break;
                case UnaryOp.GetChild:
                    opcode = "FIRST?";
                    pred = true;
                    break;
                case UnaryOp.GetSibling:
                    opcode = "NEXT?";
                    pred = true;
                    break;
                case UnaryOp.RemoveObject:
                    opcode = "REMOVE";
                    break;
                case UnaryOp.DirectInput:
                    opcode = "DIRIN";
                    break;
                case UnaryOp.DirectOutput:
                    opcode = "DIROUT";
                    break;
                case UnaryOp.OutputBuffer:
                    opcode = "BUFOUT";
                    break;
                case UnaryOp.OutputStyle:
                    opcode = "HLIGHT";
                    break;
                case UnaryOp.SplitWindow:
                    opcode = "SPLIT";
                    break;
                case UnaryOp.SelectWindow:
                    opcode = "SCREEN";
                    break;
                case UnaryOp.ClearWindow:
                    opcode = "CLEAR";
                    break;
                case UnaryOp.GetCursor:
                    opcode = "CURGET";
                    break;
                case UnaryOp.EraseLine:
                    opcode = "ERASE";
                    break;
                case UnaryOp.SetFont:
                    opcode = "FONT";
                    break;
                case UnaryOp.CheckUnicode:
                    opcode = "CHECKU";
                    break;
                default:
                    throw new NotImplementedException();
            }

            if (pred)
            {
                ILabel label = DefineLabel();

                AddLine(
                    string.Format("{0} {1}{2}{3}",
                        opcode,
                        value,
                        result == null ? "" : " >",
                        (object)result ?? ""),
                    label,
                    PeepholeLineType.BranchPositive);

                peep.MarkLabel(label);
            }
            else
            {
                AddLine(
                    string.Format("{0} {1}{2}{3}",
                        opcode,
                        value,
                        result == null ? "" : " >",
                        (object)result ?? ""),
                    null,
                    PeepholeLineType.Plain);
            }
        }

        public void EmitBinary(BinaryOp op, IOperand left, IOperand right, IVariable result)
        {
            // optimize special cases
            if (op == BinaryOp.Add &&
                ((left == game.One && right == result) || (right == game.One && left == result)))
            {
                AddLine("INC '" + result.ToString(), null, PeepholeLineType.Plain);
                return;
            }
            else if (op == BinaryOp.Sub && left == result && right == game.One)
            {
                AddLine("DEC '" + result.ToString(), null, PeepholeLineType.Plain);
                return;
            }
            else if (op == BinaryOp.StoreIndirect && right == Stack)
            {
                AddLine("POP " + left.ToString(), null, PeepholeLineType.Plain);
                return;
            }

            string opcode;

            switch (op)
            {
                case BinaryOp.Add:
                    opcode = "ADD";
                    break;
                case BinaryOp.And:
                    opcode = "BAND";
                    break;
                case BinaryOp.ArtShift:
                    opcode = "ASHIFT";
                    break;
                case BinaryOp.Div:
                    opcode = "DIV";
                    break;
                case BinaryOp.GetByte:
                    opcode = "GETB";
                    break;
                case BinaryOp.GetPropAddress:
                    opcode = "GETPT";
                    break;
                case BinaryOp.GetProperty:
                    opcode = "GETP";
                    break;
                case BinaryOp.GetNextProp:
                    opcode = "NEXTP";
                    break;
                case BinaryOp.GetWord:
                    opcode = "GET";
                    break;
                case BinaryOp.LogShift:
                    opcode = "SHIFT";
                    break;
                case BinaryOp.Mod:
                    opcode = "MOD";
                    break;
                case BinaryOp.Mul:
                    opcode = "MUL";
                    break;
                case BinaryOp.Or:
                    opcode = "BOR";
                    break;
                case BinaryOp.Sub:
                    opcode = "SUB";
                    break;
                case BinaryOp.MoveObject:
                    opcode = "MOVE";
                    break;
                case BinaryOp.SetFlag:
                    opcode = "FSET";
                    break;
                case BinaryOp.ClearFlag:
                    opcode = "FCLEAR";
                    break;
                case BinaryOp.DirectOutput:
                    opcode = "DIROUT";
                    break;
                case BinaryOp.SetCursor:
                    opcode = "CURSET";
                    break;
                case BinaryOp.SetColor:
                    opcode = "COLOR";
                    break;
                case BinaryOp.Throw:
                    opcode = "THROW";
                    break;
                case BinaryOp.StoreIndirect:
                    opcode = "SET";
                    break;
                default:
                    throw new NotImplementedException();
            }

            AddLine(
                string.Format("{0} {1},{2}{3}{4}",
                    opcode,
                    left,
                    right,
                    result == null ? "" : " >",
                    (object)result ?? ""),
                null,
                PeepholeLineType.Plain);
        }

        public void EmitTernary(TernaryOp op, IOperand left, IOperand center, IOperand right, IVariable result)
        {
            string opcode;

            switch (op)
            {
                case TernaryOp.PutByte:
                    opcode = "PUTB";
                    break;
                case TernaryOp.PutProperty:
                    opcode = "PUTP";
                    break;
                case TernaryOp.PutWord:
                    opcode = "PUT";
                    break;
                case TernaryOp.CopyTable:
                    opcode = "COPYT";
                    break;
                default:
                    throw new NotImplementedException();
            }

            AddLine(
                string.Format("{0} {1},{2},{3}{4}{5}",
                    opcode,
                    left,
                    center,
                    right,
                    result == null ? "" : " >",
                    (object)result ?? ""),
                null,
                PeepholeLineType.Plain);
        }

        public void EmitEncodeText(IOperand src, IOperand length, IOperand srcOffset, IOperand dest)
        {
            AddLine(
                string.Format("ZWSTR {0},{1},{2},{3}",
                    src,
                    length,
                    srcOffset,
                    dest),
                null,
                PeepholeLineType.Plain);
        }

        public void EmitTokenize(IOperand text, IOperand parse, IOperand dictionary, IOperand flag)
        {
            var sb = new StringBuilder("LEX ");
            sb.Append(text);
            sb.Append(',');
            sb.Append(parse);

            if (dictionary != null)
            {
                sb.Append(',');
                sb.Append(dictionary);

                if (flag != null)
                {
                    sb.Append(',');
                    sb.Append(flag);
                }
            }

            AddLine(sb.ToString(), null, PeepholeLineType.Plain);
        }

        public void EmitRestart()
        {
            AddLine("RESTART", null, PeepholeLineType.Terminator);
        }

        public void EmitQuit()
        {
            AddLine("QUIT", null, PeepholeLineType.Terminator);
        }

        public bool HasBranchSave
        {
            get { return game.zversion < 4; }
        }

        public void EmitSave(ILabel label, bool polarity)
        {
            AddLine("SAVE", label,
                polarity ? PeepholeLineType.BranchPositive : PeepholeLineType.BranchNegative);
        }

        public void EmitRestore(ILabel label, bool polarity)
        {
            AddLine("RESTORE", label,
                polarity ? PeepholeLineType.BranchPositive : PeepholeLineType.BranchNegative);
        }

        public bool HasStoreSave
        {
            get { return game.zversion >= 4; }
        }

        public void EmitSave(IVariable result)
        {
            AddLine("SAVE >" + result, null, PeepholeLineType.Plain);
        }

        public void EmitRestore(IVariable result)
        {
            AddLine("RESTORE >" + result, null, PeepholeLineType.Plain);
        }

        public bool HasExtendedSave
        {
            get { return game.zversion >= 5; }
        }

        public void EmitSave(IOperand table, IOperand size, IOperand name,
            IVariable result)
        {
            AddLine(
                string.Format("SAVE {0},{1},{2} >{3}",
                    table, size, name, result),
                null,
                PeepholeLineType.Plain);
        }

        public void EmitRestore(IOperand table, IOperand size, IOperand name,
            IVariable result)
        {
            AddLine(
                string.Format("RESTORE {0},{1},{2} >{3}",
                    table, size, name, result),
                null,
                PeepholeLineType.Plain);
        }

        public void EmitScanTable(IOperand value, IOperand table, IOperand length, IOperand form,
            IVariable result, ILabel label, bool polarity)
        {
            StringBuilder sb = new StringBuilder("INTBL? ");
            sb.Append(value);
            sb.Append(',');
            sb.Append(table);
            sb.Append(',');
            sb.Append(length);
            if (form != null)
            {
                sb.Append(',');
                sb.Append(form);
            }
            sb.Append(" >");
            sb.Append(result);

            AddLine(sb.ToString(), label,
                polarity ? PeepholeLineType.BranchPositive : PeepholeLineType.BranchNegative);
        }

        public void EmitGetChild(IOperand value, IVariable result, ILabel label, bool polarity)
        {
            AddLine(
                string.Format("FIRST? {0} >{1}", value, result),
                label,
                polarity ? PeepholeLineType.BranchPositive : PeepholeLineType.BranchNegative);
        }

        public void EmitGetSibling(IOperand value, IVariable result, ILabel label, bool polarity)
        {
            AddLine(
                string.Format("NEXT? {0} >{1}", value, result),
                label,
                polarity ? PeepholeLineType.BranchPositive : PeepholeLineType.BranchNegative);
        }

        public void EmitPrintNewLine()
        {
            AddLine("CRLF", null, PeepholeLineType.Plain);
        }

        public void EmitPrint(string text, bool crlfRtrue)
        {
            AddLine(
                string.Format("{0} \"{1}\"", crlfRtrue ? "PRINTR" : "PRINTI",
                    GameBuilder.SanitizeString(text)),
                null,
                crlfRtrue ? PeepholeLineType.HeavyTerminator : PeepholeLineType.Plain);
        }

        public void EmitPrint(PrintOp op, IOperand value)
        {
            string opcode;

            switch (op)
            {
                case PrintOp.Address:
                    opcode = "PRINTB";
                    break;
                case PrintOp.Character:
                    opcode = "PRINTC";
                    break;
                case PrintOp.Number:
                    opcode = "PRINTN";
                    break;
                case PrintOp.Object:
                    opcode = "PRINTD";
                    break;
                case PrintOp.PackedAddr:
                    opcode = "PRINT";
                    break;
                case PrintOp.Unicode:
                    opcode = "PRINTU";
                    break;
                default:
                    throw new NotImplementedException();
            }

            AddLine(opcode + " " + value.ToString(), null, PeepholeLineType.Plain);
        }

        public void EmitPrintTable(IOperand table, IOperand width, IOperand height, IOperand skip)
        {
            StringBuilder sb = new StringBuilder("PRINTT ");
            sb.Append(table);
            sb.Append(',');
            sb.Append(width);

            if (height != null)
            {
                sb.Append(',');
                sb.Append(height);

                if (skip != null)
                {
                    sb.Append(',');
                    sb.Append(skip);
                }
            }

            AddLine(sb.ToString(), null, PeepholeLineType.Plain);
        }

        public void EmitPlaySound(IOperand number, IOperand effect, IOperand volume, IOperand routine)
        {
            StringBuilder sb = new StringBuilder("SOUND ");
            sb.Append(number);

            if (effect != null)
            {
                sb.Append(',');
                sb.Append(effect);

                if (volume != null)
                {
                    sb.Append(',');
                    sb.Append(volume);

                    if (routine != null)
                    {
                        sb.Append(',');
                        sb.Append(routine);
                    }
                }
            }

            AddLine(sb.ToString(), null, PeepholeLineType.Plain);
        }

        public void EmitRead(IOperand chrbuf, IOperand lexbuf, IOperand interval, IOperand routine,
            IVariable result)
        {
            StringBuilder sb = new StringBuilder("READ ");
            sb.Append(chrbuf);

            if (lexbuf != null)
            {
                sb.Append(',');
                sb.Append(lexbuf);

                if (interval != null)
                {
                    sb.Append(',');
                    sb.Append(interval);

                    if (routine != null)
                    {
                        sb.Append(',');
                        sb.Append(routine);
                    }
                }
            }

            if (result != null)
            {
                sb.Append(" >");
                sb.Append(result);
            }

            AddLine(sb.ToString(), null, PeepholeLineType.Plain);
        }

        public void EmitReadChar(IOperand interval, IOperand routine, IVariable result)
        {
            StringBuilder sb = new StringBuilder("INPUT 1");

            if (interval != null)
            {
                sb.Append(',');
                sb.Append(interval);

                if (routine != null)
                {
                    sb.Append(',');
                    sb.Append(routine);
                }
            }

            if (result != null)
            {
                sb.Append(" >");
                sb.Append(result);
            }

            AddLine(sb.ToString(), null, PeepholeLineType.Plain);
        }

        public void EmitCall(IOperand routine, IOperand[] args, IVariable result)
        {
            /* V1-3: CALL (0-3, store)
             * V4: CALL1 (0, store), CALL2 (1, store), XCALL (0-7, store)
             * V5: ICALL1 (0), ICALL2 (1), ICALL (0-3), IXCALL (0-7) */

            if (args.Length > game.MaxCallArguments)
                throw new ArgumentException(
                    string.Format(
                    "Too many arguments in routine call: {0} supplied, {1} allowed",
                    args.Length,
                    game.MaxCallArguments));

            StringBuilder sb = new StringBuilder();

            if (game.zversion < 4)
            {
                // V1-3: use only CALL opcode (3 args max), pop result if not needed
                sb.Append("CALL ");
                sb.Append(routine);
                foreach (IOperand arg in args)
                {
                    sb.Append(',');
                    sb.Append(arg);
                }

                if (result != null)
                {
                    sb.Append(" >");
                    sb.Append(result);
                }

                AddLine(sb.ToString(), null, PeepholeLineType.Plain);

                if (result == null && cleanStack)
                    AddLine("FSTACK", null, PeepholeLineType.Plain);
            }
            else if (game.zversion == 4)
            {
                // V4: use CALL/CALL1/CALL2/XCALL opcodes, pop result if not needed
                string opcode;
                switch (args.Length)
                {
                    case 0:
                        opcode = "CALL1";
                        break;
                    case 1:
                        opcode = "CALL2";
                        break;
                    case 2:
                    case 3:
                        opcode = "CALL";
                        break;
                    default:
                        opcode = "XCALL";
                        break;
                }

                sb.Append(opcode);
                sb.Append(' ');
                sb.Append(routine);
                foreach (IOperand arg in args)
                {
                    sb.Append(',');
                    sb.Append(arg);
                }

                if (result != null)
                {
                    sb.Append(" >");
                    sb.Append(result);
                }

                AddLine(sb.ToString(), null, PeepholeLineType.Plain);

                if (result == null && cleanStack)
                    AddLine("FSTACK", null, PeepholeLineType.Plain);
            }
            else
            {
                // V5-V6: use CALL/CALL1/CALL2/XCALL if want result
                // use ICALL/ICALL1/ICALL2/IXCALL if not
                string opcode;
                if (result == null)
                {
                    switch (args.Length)
                    {
                        case 0:
                            opcode = "ICALL1";
                            break;
                        case 1:
                            opcode = "ICALL2";
                            break;
                        case 2:
                        case 3:
                            opcode = "ICALL";
                            break;
                        default:
                            opcode = "IXCALL";
                            break;
                    }
                }
                else
                {
                    switch (args.Length)
                    {
                        case 0:
                            opcode = "CALL1";
                            break;
                        case 1:
                            opcode = "CALL2";
                            break;
                        case 2:
                        case 3:
                            opcode = "CALL";
                            break;
                        default:
                            opcode = "XCALL";
                            break;
                    }
                }

                sb.Append(opcode);
                sb.Append(' ');
                sb.Append(routine);
                foreach (IOperand arg in args)
                {
                    sb.Append(',');
                    sb.Append(arg);
                }

                if (result != null)
                {
                    sb.Append(" >");
                    sb.Append(result);
                }

                AddLine(sb.ToString(), null, PeepholeLineType.Plain);
            }
        }

        public void EmitStore(IVariable dest, IOperand src)
        {
            if (dest != src)
            {
                if (dest == STACK)
                    AddLine("PUSH " + src.ToString(), null, PeepholeLineType.Plain);
                else if (src == STACK)
                    AddLine("POP '" + dest.ToString(), null, PeepholeLineType.Plain);
                else
                    AddLine(string.Format("SET '{0},{1}", dest, src), null, PeepholeLineType.Plain);
            }
        }

        public void EmitPopStack()
        {
            if (cleanStack)
            {
                if (game.zversion <= 4)
                {
                    AddLine("FSTACK", null, PeepholeLineType.Plain);
                }
                else if (game.zversion == 6)
                {
                    AddLine("FSTACK 1", null, PeepholeLineType.Plain);
                }
                else
                {
                    AddLine("ICALL2 0,STACK", null, PeepholeLineType.Plain);
                }
            }
        }

        public void Finish()
        {
            game.WriteOutput(string.Empty);

            StringBuilder sb = new StringBuilder();

            // write routine header
            if (game.debug != null)
            {
                sb.Append(INDENT);
                sb.Append(".DEBUG-ROUTINE ");
                sb.Append(game.debug.GetFileNumber(defnStart.File));
                sb.Append(',');
                sb.Append(defnStart.Line);
                sb.Append(',');
                sb.Append(defnStart.Column);
                sb.Append(",\"");
                sb.Append(name);
                sb.Append('"');
                foreach (LocalBuilder lb in requiredParams.Concat(optionalParams).Concat(locals))
                {
                    sb.Append(",\"");
                    sb.Append(lb.Name);
                    sb.Append('"');
                }
                sb.AppendLine();
            }

            sb.Append(INDENT);
            sb.Append(".FUNCT ");
            sb.Append(name);

            foreach (LocalBuilder lb in requiredParams)
            {
                sb.Append(',');
                sb.Append(lb.Name);
            }

            foreach (LocalBuilder lb in Enumerable.Concat(optionalParams, locals))
            {
                sb.Append(',');
                sb.Append(lb.Name);

                if (game.zversion < 5 && lb.DefaultValue != null)
                {
                    sb.Append('=');
                    sb.Append(lb.DefaultValue);
                }
            }

            game.WriteOutput(sb.ToString());

            if (entryPoint)
                game.WriteOutput("START::");

            // write values for optional params and locals for V5+
            if (game.zversion >= 5)
            {
                // TODO: skip if the default value is 0?

                foreach (LocalBuilder lb in optionalParams)
                    if (lb.DefaultValue != null)
                    {
                        ILabel nextLabel = DefineLabel();
                        game.WriteOutput(string.Format(INDENT + "ASSIGNED? '{0} /{1}", lb, nextLabel));
                        game.WriteOutput(string.Format(INDENT + "SET '{0},{1}", lb, lb.DefaultValue));
                        game.WriteOutput(nextLabel + ":");
                    }

                foreach (LocalBuilder lb in locals)
                    if (lb.DefaultValue != null)
                        game.WriteOutput(string.Format(INDENT + "SET '{0},{1}", lb, lb.DefaultValue));
            }

            // write routine body
            peep.Finish((label, code, dest, type) =>
            {
                if (code.DebugText != null)
                    game.WriteOutput(INDENT + code.DebugText);

                if (type == PeepholeLineType.BranchAlways)
                {
                    if (dest == RTRUE)
                    {
                        game.WriteOutput(INDENT + "RTRUE");
                        return;
                    }
                    if (dest == RFALSE)
                    {
                        game.WriteOutput(INDENT + "RFALSE");
                        return;
                    }
                }

                if (code.Text == "CRLF+RTRUE")
                {
                    game.WriteOutput(string.Format("{0}{1}{2}CRLF",
                        (object)label ?? "",
                        label == null ? "" : ":",
                        INDENT));

                    game.WriteOutput(INDENT + "RTRUE");
                    return;
                }

                sb.Length = 0;
                if (label != null)
                {
                    sb.Append(label);
                    sb.Append(':');
                }
                sb.Append(INDENT);
                sb.Append(code.Text);

                switch (type)
                {
                    case PeepholeLineType.BranchAlways:
                        sb.Append(' ');
                        sb.Append(dest);
                        break;
                    case PeepholeLineType.BranchPositive:
                        sb.Append(" /");
                        sb.Append(dest);
                        break;
                    case PeepholeLineType.BranchNegative:
                        sb.Append(" \\");
                        sb.Append(dest);
                        break;
                }

                game.WriteOutput(sb.ToString());
            });

            if (game.debug != null)
                game.WriteOutput(string.Format(
                    INDENT + ".DEBUG-ROUTINE-END {0},{1},{2}",
                    game.debug.GetFileNumber(defnEnd.File),
                    defnEnd.Line,
                    defnEnd.Column));
        }

        private class PeepholeCombiner : IPeepholeCombiner<ZapCode>
        {
            private readonly RoutineBuilder routineBuilder;

            public PeepholeCombiner(RoutineBuilder routineBuilder)
            {
                this.routineBuilder = routineBuilder;
            }

            private void BeginMatch(IEnumerable<CombinableLine<ZapCode>> lines)
            {
                enumerator = lines.GetEnumerator();
                matches = new List<CombinableLine<ZapCode>>();
            }

            private bool Match(params Predicate<CombinableLine<ZapCode>>[] criteria)
            {
                while (matches.Count < criteria.Length)
                {
                    if (enumerator.MoveNext() == false)
                        return false;

                    matches.Add(enumerator.Current);
                }

                for (int i = 0; i < criteria.Length; i++)
                {
                    if (criteria[i](matches[i]) == false)
                        return false;
                }

                return true;
            }

            private void EndMatch()
            {
                enumerator.Dispose();
                enumerator = null;

                matches = null;
            }

            private IEnumerator<CombinableLine<ZapCode>> enumerator;
            private List<CombinableLine<ZapCode>> matches;

            private CombinerResult<ZapCode> Combine1to1(string newText, PeepholeLineType? type = null, ILabel target = null)
            {
                return new CombinerResult<ZapCode>(
                    1,
                    new CombinableLine<ZapCode>[] {
                        new CombinableLine<ZapCode>(
                            matches[0].Label,
                            new ZapCode() {
                                Text = newText,
                                DebugText = matches[0].Code.DebugText,
                            },
                            target ?? matches[0].Target,
                            type ?? matches[0].Type),
                    });
            }

            private CombinerResult<ZapCode> Combine2to1(string newText, PeepholeLineType? type = null, ILabel target = null)
            {
                return new CombinerResult<ZapCode>(
                    2,
                    new CombinableLine<ZapCode>[] {
                        new CombinableLine<ZapCode>(
                            matches[0].Label,
                            new ZapCode() {
                                Text = newText,
                                DebugText = matches[0].Code.DebugText ?? matches[1].Code.DebugText,
                            },
                            target ?? matches[1].Target,
                            type ?? matches[1].Type),
                    });
            }

            private CombinerResult<ZapCode> Combine2to2(
                string newText1, string newText2,
                PeepholeLineType? type1 = null, PeepholeLineType? type2 = null,
                ILabel target1 = null, ILabel target2 = null)
            {
                return new CombinerResult<ZapCode>(
                    2,
                    new CombinableLine<ZapCode>[] {
                        new CombinableLine<ZapCode>(
                            matches[0].Label,
                            new ZapCode() {
                                Text = newText1,
                                DebugText = matches[0].Code.DebugText,
                            },
                            target1 ?? matches[0].Target,
                            type1 ?? matches[0].Type),
                        new CombinableLine<ZapCode>(
                            matches[1].Label,
                            new ZapCode() {
                                Text = newText2,
                                DebugText = matches[1].Code.DebugText,
                            },
                            target2 ?? matches[1].Target,
                            type2 ?? matches[1].Type),
                    });
            }

            private CombinerResult<ZapCode> Combine3to1(string newText, PeepholeLineType? type = null, ILabel target = null)
            {
                return new CombinerResult<ZapCode>(
                    3,
                    new CombinableLine<ZapCode>[] {
                        new CombinableLine<ZapCode>(
                            matches[0].Label,
                            new ZapCode() {
                                Text = newText,
                                DebugText = matches[0].Code.DebugText ?? matches[1].Code.DebugText ?? matches[2].Code.DebugText,
                            },
                            target ?? matches[2].Target,
                            type ?? matches[2].Type),
                    });
            }

            private static readonly Regex equalZeroRegex = new Regex(@"^EQUAL\? (?:(?<var>[^,]+),0|0,(?<var>[^,]+))$");

            public CombinerResult<ZapCode> Apply(IEnumerable<CombinableLine<ZapCode>> lines)
            {
                System.Text.RegularExpressions.Match rm = null;

                BeginMatch(lines);
                try
                {
                    if (Match(a => (rm = equalZeroRegex.Match(a.Code.Text)).Success))
                    {
                        // EQUAL? x,0 | EQUAL? 0,x => ZERO? x
                        return Combine1to1("ZERO? " + rm.Groups["var"]);
                    }

                    if (Match(a => a.Code.Text.StartsWith("PUSH "), b => b.Code.Text == "RSTACK"))
                    {
                        // PUSH + RSTACK => RFALSE/RTRUE/RETURN
                        switch (matches[0].Code.Text)
                        {
                            case "PUSH 0":
                                return Combine2to1("RFALSE", PeepholeLineType.BranchAlways, RoutineBuilder.RFALSE);
                            case "PUSH 1":
                                return Combine2to1("RTRUE", PeepholeLineType.BranchAlways, RoutineBuilder.RTRUE);
                            default:
                                return Combine2to1("RETURN " + matches[0].Code.Text.Substring(5));
                        }
                    }

                    if (Match(a => a.Code.Text.EndsWith(">STACK"), b => b.Code.Text.StartsWith("POP '")))
                    {
                        // >STACK + POP 'dest => >dest
                        var a = matches[0].Code.Text;
                        var b = matches[1].Code.Text;
                        return Combine2to1(a.Substring(0, a.Length - 5) + b.Substring(5));
                    }

                    if (Match(a => a.Code.Text.StartsWith("INC '"), b => b.Code.Text.StartsWith("GRTR? ")))
                    {
                        string str;
                        if ((str = matches[0].Code.Text.Substring(5)) != "STACK" &&
                            matches[1].Code.Text.StartsWith("GRTR? " + str))
                        {
                            // INC 'v + GRTR? v => IGRTR? 'v
                            return Combine2to1("IGRTR? '" + matches[1].Code.Text.Substring(6));
                        }
                    }

                    if (Match(a => a.Code.Text.StartsWith("DEC '"), b => b.Code.Text.StartsWith("LESS? ")))
                    {
                        string str;
                        if ((str = matches[0].Code.Text.Substring(5)) != "STACK" &&
                            matches[1].Code.Text.StartsWith("LESS? " + str))
                        {
                            // DEC 'v + LESS? v => DLESS? 'v
                            return Combine2to1("DLESS? '" + matches[1].Code.Text.Substring(6));
                        }
                    }

                    if (Match(a => (a.Code.Text.StartsWith("EQUAL? ") || a.Code.Text.StartsWith("ZERO? ")) && a.Type == PeepholeLineType.BranchPositive,
                              b => (b.Code.Text.StartsWith("EQUAL? ") || b.Code.Text.StartsWith("ZERO? ")) && b.Type == PeepholeLineType.BranchPositive))
                    {
                        if (matches[0].Target == matches[1].Target)
                        {
                            string[] aparts, bparts;

                            if (matches[0].Code.Text.StartsWith("ZERO? "))
                            {
                                aparts = new[] { matches[0].Code.Text.Substring(6), "0" };
                            }
                            else
                            {
                                aparts = matches[0].Code.Text.Substring(7).Split(',');
                            }

                            if (matches[1].Code.Text.StartsWith("ZERO? "))
                            {
                                bparts = new[] { matches[1].Code.Text.Substring(6), "0" };
                            }
                            else
                            {
                                bparts = matches[1].Code.Text.Substring(7).Split(',');
                            }

                            if (aparts[0] == bparts[0] && aparts.Length < 4)
                            {
                                var sb = new StringBuilder(matches[0].Code.Text.Length + matches[1].Code.Text.Length);

                                if (aparts.Length + bparts.Length <= 5)
                                {
                                    // EQUAL? v,a,b /L + EQUAL? v,c /L => EQUAL? v,a,b,c /L
                                    sb.Append("EQUAL? ");
                                    sb.Append(aparts[0]);
                                    for (int i = 1; i < aparts.Length; i++)
                                    {
                                        sb.Append(',');
                                        sb.Append(aparts[i]);
                                    }
                                    for (int i = 1; i < bparts.Length; i++)
                                    {
                                        sb.Append(',');
                                        sb.Append(bparts[i]);
                                    }
                                    return Combine2to1(sb.ToString());
                                }
                                else
                                {
                                    // EQUAL? v,a,b /L + EQUAL? v,c,d /L => EQUAL? v,a,b,c /L + EQUAL? v,d /L
                                    var allRhs = aparts.Skip(1).Concat(bparts.Skip(1));

                                    sb.Append("EQUAL? ");
                                    sb.Append(aparts[0]);
                                    foreach (var rhs in allRhs.Take(3))
                                    {
                                        sb.Append(',');
                                        sb.Append(rhs);
                                    }
                                    var first = sb.ToString();

                                    sb.Length = 0;
                                    sb.Append("EQUAL? ");
                                    sb.Append(aparts[0]);
                                    foreach (var rhs in allRhs.Skip(3))
                                    {
                                        sb.Append(',');
                                        sb.Append(rhs);
                                    }
                                    var second = sb.ToString();

                                    return Combine2to2(first, second);
                                }
                            }
                        }
                    }

                    if (Match(a => a.Code.Text == "CRLF", b => b.Code.Text == "RTRUE"))
                    {
                        // combine CRLF + RTRUE into a single terminator
                        // this can be pulled through a branch and thus allows more PRINTR transformations
                        return Combine2to1("CRLF+RTRUE", PeepholeLineType.Terminator);
                    }

                    if (Match(a => a.Code.Text.StartsWith("PRINTI "), b => b.Code.Text == "CRLF+RTRUE"))
                    {
                        // PRINTI + (CRLF + RTRUE) => PRINTR
                        return Combine2to1("PRINTR " + matches[0].Code.Text.Substring(7), PeepholeLineType.HeavyTerminator);
                    }

                    // no matches
                    return new CombinerResult<ZapCode>();
                }
                finally
                {
                    EndMatch();
                }
            }

            public ZapCode SynthesizeBranchAlways()
            {
                return new ZapCode() { Text = "JUMP" };
            }

            public bool AreIdentical(ZapCode a, ZapCode b)
            {
                return a.Text == b.Text;
            }

            public ZapCode MergeIdentical(ZapCode a, ZapCode b)
            {
                return new ZapCode()
                {
                    Text = a.Text,
                    DebugText = a.DebugText ?? b.DebugText,
                };
            }

            public SameTestResult AreSameTest(ZapCode a, ZapCode b)
            {
                // if the stack is involved, all bets are off
                if (a.Text.Contains("STACK") || b.Text.Contains("STACK"))
                    return SameTestResult.Unrelated;

                // if the instructions are identical, they must be the same test
                if (a.Text == b.Text)
                    return SameTestResult.SameTest;

                /* otherwise, they can be related if 'a' is a store+branch instruction
                 * and 'b' is ZERO? testing the result stored by 'a'. the z-machine's
                 * store+branch instructions all branch upon storing a nonzero value,
                 * so we always return OppositeTest in this case. */
                if (b.Text.StartsWith("ZERO? ") && a.Text.EndsWith(">" + b.Text.Substring(6)))
                    return SameTestResult.OppositeTest;

                return SameTestResult.Unrelated;
            }

            public ILabel NewLabel()
            {
                return routineBuilder.DefineLabel();
            }
        }
    }

    class LocalBuilder : ILocalBuilder
    {
        private readonly string name;
        private IOperand defaultValue;

        public LocalBuilder(string name)
        {
            this.name = name;
        }

        public IIndirectOperand Indirect
        {
            get { return new IndirectOperand(this); }
        }

        public IOperand DefaultValue
        {
            get { return defaultValue; }
            set { defaultValue = value; }
        }

        public string Name
        {
            get { return name; }
        }

        public override string ToString()
        {
            return name;
        }
    }

    class GlobalBuilder : IGlobalBuilder
    {
        private readonly string name;
        private IOperand defaultValue;

        public GlobalBuilder(string name)
        {
            this.name = name;
        }

        public IIndirectOperand Indirect
        {
            get { return new IndirectOperand(this); }
        }

        public IOperand DefaultValue
        {
            get { return defaultValue; }
            set { defaultValue = value; }
        }

        public string Name
        {
            get { return name; }
        }

        public override string ToString()
        {
            return name;
        }
    }

    class TableBuilder : ITableBuilder
    {
        private readonly string name;
        private readonly List<short> numericValues = new List<short>();
        private readonly List<IOperand> operandValues = new List<IOperand>();
        private readonly List<byte> types = new List<byte>();
        private int size = 0;

        private const byte T_NUM_BYTE = 0;
        private const byte T_NUM_WORD = 1;
        private const byte T_OP_BYTE = 2;
        private const byte T_OP_WORD = 3;

        protected const string INDENT = "\t";

        public TableBuilder(string name)
        {
            this.name = name;
        }

        public string Name
        {
            get { return name; }
        }

        public int Size
        {
            get { return size; }
        }

        public void AddByte(byte value)
        {
            types.Add(T_NUM_BYTE);
            numericValues.Add(value);
            size++;
        }

        public void AddByte(IOperand value)
        {
            if (value == null)
                throw new ArgumentNullException();

            types.Add(T_OP_BYTE);
            operandValues.Add(value);
            size++;
        }

        public void AddShort(short value)
        {
            types.Add(T_NUM_WORD);
            numericValues.Add(value);
            size += 2;
        }

        public void AddShort(IOperand value)
        {
            if (value == null)
                throw new ArgumentNullException();

            types.Add(T_OP_WORD);
            operandValues.Add(value);
            size += 2;
        }

        public override string ToString()
        {
            return name;
        }

        public void WriteTo(TextWriter writer)
        {
            bool wasWord = false;
            int lineCount = 0, ni = 0, oi = 0;
            for (int i = 0; i < types.Count; i++)
            {
                byte t = types[i];
                bool isWord = (t & 1) != 0;
                bool isOperand = (t & 2) != 0;

                if (lineCount == 0 || lineCount == 10 || isWord != wasWord)
                {
                    if (ni + oi != 0)
                        writer.WriteLine();
                    writer.Write(isWord ? INDENT + ".WORD " : INDENT + ".BYTE ");
                    lineCount = 0;
                }
                else
                    writer.Write(',');

                if (isOperand)
                    writer.Write(operandValues[oi++]);
                else
                    writer.Write(numericValues[ni++]);

                wasWord = isWord;
                lineCount++;
            }

            writer.WriteLine();
        }
    }

    class WordBuilder : TableBuilder, IWordBuilder
    {
        private readonly string word;

        public WordBuilder(string tableName, string word)
            : base(tableName)
        {
            this.word = word;
        }

        public string Word
        {
            get { return word; }
        }
    }

    class ObjectBuilder : IObjectBuilder
    {
        private const string INDENT = "\t";

        private struct PropertyEntry
        {
            public const byte BYTE = 0;
            public const byte WORD = 1;
            public const byte TABLE = 2;

            public PropertyBuilder Property;
            public IOperand Value;
            public byte Kind;

            public PropertyEntry(PropertyBuilder prop, IOperand value, byte kind)
            {
                this.Property = prop;
                this.Value = value;
                this.Kind = kind;
            }
        }

        private readonly GameBuilder game;
        private readonly int number;
        private readonly string name;
        private readonly List<PropertyEntry> props = new List<PropertyEntry>();
        private readonly List<FlagBuilder> flags = new List<FlagBuilder>();

        private string descriptiveName = "";
        private IObjectBuilder parent, child, sibling;

        public ObjectBuilder(GameBuilder game, int number, string name)
        {
            this.game = game;
            this.number = number;
            this.name = name;
        }

        public string SymbolicName
        {
            get { return name; }
        }

        public int Number
        {
            get { return number; }
        }

        public string DescriptiveName
        {
            get { return descriptiveName; }
            set { descriptiveName = value; }
        }

        public IObjectBuilder Parent
        {
            get { return parent; }
            set { parent = value; }
        }

        public IObjectBuilder Child
        {
            get { return child; }
            set { child = value; }
        }

        public IObjectBuilder Sibling
        {
            get { return sibling; }
            set { sibling = value; }
        }

        public string Flags1
        {
            get { return GetFlagsString(0); }
        }

        public string Flags2
        {
            get { return GetFlagsString(16); }
        }

        public string Flags3
        {
            get { return GetFlagsString(32); }
        }

        private string GetFlagsString(int start)
        {
            StringBuilder sb = new StringBuilder();

            foreach (FlagBuilder flag in flags)
            {
                int num = flag.Number;
                if (num >= start && num < start + 16)
                {
                    if (sb.Length > 0)
                        sb.Append('+');

                    sb.Append("FX?");
                    sb.Append(flag.ToString());
                }
            }

            if (sb.Length == 0)
                return "0";

            return sb.ToString();
        }

        public void AddByteProperty(IPropertyBuilder prop, IOperand value)
        {
            PropertyEntry pe = new PropertyEntry((PropertyBuilder)prop, value, PropertyEntry.BYTE);
            props.Add(pe);
        }

        public void AddWordProperty(IPropertyBuilder prop, IOperand value)
        {
            PropertyEntry pe = new PropertyEntry((PropertyBuilder)prop, value, PropertyEntry.WORD);
            props.Add(pe);
        }

        public ITableBuilder AddComplexProperty(IPropertyBuilder prop)
        {
            TableBuilder data = new TableBuilder(string.Format("?{0}?CP?{1}", this, prop));
            PropertyEntry pe = new PropertyEntry((PropertyBuilder)prop, data, PropertyEntry.TABLE);
            props.Add(pe);
            return data;
        }

        public void AddFlag(IFlagBuilder flag)
        {
            FlagBuilder fb = (FlagBuilder)flag;
            if (!flags.Contains(fb))
                flags.Add(fb);
        }

        internal void WriteProperties(TextWriter writer)
        {
            writer.WriteLine(INDENT + ".STRL \"{0}\"", GameBuilder.SanitizeString(descriptiveName));

            props.Sort((a, b) => b.Property.Number.CompareTo(a.Property.Number));

            for (int i = 0; i < props.Count; i++)
            {
                PropertyEntry pe = props[i];
                if (pe.Kind == PropertyEntry.BYTE)
                {
                    writer.WriteLine(INDENT + ".PROP 1,{0}", pe.Property);
                    writer.WriteLine(INDENT + ".BYTE {0}", pe.Value);
                }
                else if (pe.Kind == PropertyEntry.WORD)
                {
                    writer.WriteLine(INDENT + ".PROP 2,{0}", pe.Property);
                    writer.WriteLine(INDENT + ".WORD {0}", pe.Value);
                }
                else // TABLE
                {
                    TableBuilder tb = (TableBuilder)pe.Value;
                    writer.WriteLine(INDENT + ".PROP {0},{1}", tb.Size, pe.Property);
                    tb.WriteTo(writer);
                }
            }

            writer.WriteLine(INDENT + ".BYTE 0");
        }

        public override string ToString()
        {
            return name;
        }
    }

    class PropertyBuilder : IPropertyBuilder
    {
        private readonly string name;
        private readonly int number;
        private IOperand defaultValue;

        public PropertyBuilder(string name, int number)
        {
            this.name = name;
            this.number = number;
        }

        public int Number
        {
            get { return number; }
        }

        public IOperand DefaultValue
        {
            get { return defaultValue; }
            set { defaultValue = value; }
        }

        public override string ToString()
        {
            return name;
        }
    }

    class FlagBuilder : IFlagBuilder
    {
        private readonly string name;
        private readonly int number;

        public FlagBuilder(string name, int number)
        {
            this.name = name;
            this.number = number;
        }

        public int Number
        {
            get { return number; }
        }

        public override string ToString()
        {
            return name;
        }
    }

    class DebugFileBuilder : IDebugFileBuilder
    {
        private Dictionary<string, int> files = new Dictionary<string, int>();
        private List<string> storedLines = new List<string>();
        
        public int GetFileNumber(string filename)
        {
            if (filename == null)
                return 0;

            int result;
            if (files.TryGetValue(filename, out result) == false)
            {
                result = files.Count + 1;
                files[filename] = result;
            }
            return result;
        }

        public IEnumerable<string> StoredLines
        {
            get { return storedLines; }
        }

        public IDictionary<string, int> Files
        {
            get { return files; }
        }

        public void MarkAction(IOperand action, string name)
        {
            storedLines.Add(string.Format(
                ".DEBUG-ACTION {0},\"{1}\"",
                action,
                name));
        }

        public void MarkObject(IObjectBuilder obj, DebugLineRef start, DebugLineRef end)
        {
            storedLines.Add(string.Format(
                ".DEBUG-OBJECT {0},\"{0}\",{1},{2},{3},{4},{5},{6}",
                obj,
                GetFileNumber(start.File),
                start.Line,
                start.Column,
                GetFileNumber(end.File),
                end.Line,
                end.Column));
        }

        public void MarkRoutine(IRoutineBuilder routine, DebugLineRef start, DebugLineRef end)
        {
            ((RoutineBuilder)routine).defnStart = start;
            ((RoutineBuilder)routine).defnEnd = end;
        }

        public void MarkSequencePoint(IRoutineBuilder routine, DebugLineRef point)
        {
            ((RoutineBuilder)routine).MarkSequencePoint(point);
        }
    }

    public class ZapStreamFactory : IZapStreamFactory
    {
        private readonly string outFile;

        private const string FrequentWordsSuffix = "_freq";
        private const string DataSuffix = "_data";
        private const string StringSuffix = "_str";

        public ZapStreamFactory(string outFile)
        {
            this.outFile = outFile;
        }

        #region IZapStreamFactory Members

        public Stream CreateMainStream()
        {
            return new FileStream(outFile, FileMode.Create, FileAccess.Write);
        }

        public Stream CreateFrequentWordsStream()
        {
            return new FileStream(
                Path.GetFileNameWithoutExtension(outFile) + FrequentWordsSuffix + Path.GetExtension(outFile),
                FileMode.Create,
                FileAccess.Write);
        }

        public Stream CreateDataStream()
        {
            return new FileStream(
                Path.GetFileNameWithoutExtension(outFile) + DataSuffix + Path.GetExtension(outFile),
                FileMode.Create,
                FileAccess.Write);
        }

        public Stream CreateStringStream()
        {
            return new FileStream(
                Path.GetFileNameWithoutExtension(outFile) + StringSuffix + Path.GetExtension(outFile),
                FileMode.Create,
                FileAccess.Write);
        }

        public string GetMainFileName(bool withExt)
        {
            return withExt ? Path.GetFileName(outFile) : Path.GetFileNameWithoutExtension(outFile);
        }

        public string GetDataFileName(bool withExt)
        {
            var fn = Path.GetFileNameWithoutExtension(outFile) + DataSuffix;
            return withExt ? fn + Path.GetExtension(outFile) : fn;
        }

        public string GetFrequentWordsFileName(bool withExt)
        {
            var fn = Path.GetFileNameWithoutExtension(outFile) + FrequentWordsSuffix;
            return withExt ? fn + Path.GetExtension(outFile) : fn;
        }

        public string GetStringFileName(bool withExt)
        {
            var fn = Path.GetFileNameWithoutExtension(outFile) + StringSuffix;
            return withExt ? fn + Path.GetExtension(outFile) : fn;
        }

        public bool FrequentWordsFileExists
        {
            get
            {
                var fn = GetFrequentWordsFileName(true);
                return File.Exists(fn) || File.Exists(Path.ChangeExtension(fn, ".xzap"));
            }
        }

        #endregion
    }
}
