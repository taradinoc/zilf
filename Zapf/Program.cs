/* Copyright 2010 Jesse McGrew
 * 
 * This file is part of ZAPF.
 * 
 * ZAPF is free software: you can redistribute it and/or modify it
 * under the terms of the GNU General Public License as published by
 * the Free Software Foundation, either version 3 of the License, or
 * (at your option) any later version.
 * 
 * ZAPF is distributed in the hope that it will be useful, but
 * WITHOUT ANY WARRANTY; without even the implied warranty of
 * MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the GNU
 * General Public License for more details.
 * 
 * You should have received a copy of the GNU General Public License
 * along with ZAPF.  If not, see <http://www.gnu.org/licenses/>.
 */

using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;

using Zapf.Lexing;
using Zapf.Parsing;
using Antlr.Runtime;
using System.IO;
using Antlr.Runtime.Tree;

namespace Zapf
{
    class Program
    {
        // TODO: Blorb output

        public const string VERSION = "ZAPF 0.3";
        public const byte DEFAULT_ZVERSION = 3;

        public static int Main(string[] args)
        {
            // parse command line
            Context ctx = ParseArgs(args);
            if (ctx == null)
            {
                Usage();
                return 1;
            }

            // show banner
            if (!ctx.Quiet)
                Console.Error.WriteLine(VERSION);

            // set up for the target version
            ctx.OpcodeDict = MakeOpcodeDict(ctx.ZVersion, ctx.InformMode);
            string stackName = ctx.InformMode ? "sp" : "STACK";
            ctx.Symbols.Add(stackName, new Symbol(stackName, SymbolType.Variable, 0, ctx.CurrentPass));

            try
            {
                // assemble the code
                bool restart;
                do
                {
                    restart = false;
                    try
                    {
                        Assemble(ctx);
                    }
                    catch (RestartException)
                    {
                        if (!ctx.Quiet)
                            Console.Error.WriteLine("\nRestarting");

                        restart = true;
                    }
                    catch (FatalError fer)
                    {
                        ctx.HandleFatalError(fer);
                        return 2;
                    }
                } while (restart);

                // list label addresses
                if (ctx.ListAddresses)
                {
                    Console.Error.WriteLine();

                    // XXX incorrect addresses for V6/7
                    var query = from s in ctx.Symbols.Values
                                where s.Type == SymbolType.Function || s.Type == SymbolType.GlobalLabel ||
                                      s.Type == SymbolType.String
                                let addr = (s.Type == SymbolType.GlobalLabel) ? s.Value : s.Value * ctx.PackingDivisor
                                orderby addr
                                select new { Name = s.Name, Address = addr };

                    Console.WriteLine("{0,-32} {1,-6} {2,-6}",
                        "Name",
                        "Addr",
                        "Length");

                    var entries = query.ToArray();

                    for (int i = 0; i < entries.Length; i++)
                    {
                        var entry = entries[i];

                        object dist;
                        if (i < entries.Length - 1)
                            dist = entries[i + 1].Address - entries[i].Address;
                        else
                            dist = "to end";

                        Console.WriteLine("{0,-32} ${1:x5} {2,6}",
                            entry.Name,
                            entry.Address,
                            dist);
                    }
                }

                // find abbreviations
                if (ctx.AbbreviateMode)
                {
                    const int maxAbbrevs = 96;
                    Console.Error.WriteLine("Finding up to {0} abbreviations...", maxAbbrevs);

                    Console.WriteLine("        ; Frequent words file for {0}", Path.GetFileName(ctx.InFile));
                    Console.WriteLine();
                    int num = 1, totalSavings = 0;
                    foreach (AbbrevFinder.Result r in ctx.AbbrevFinder.GetResults(maxAbbrevs))
                    {
                        Console.WriteLine("        .FSTR FSTR?{0},\"{1}\"\t\t; {2}x, saved {3}",
                            num++, SanitizeString(r.Text), r.Count, r.Score);
                        totalSavings += r.Score;
                    }
                    if (num < maxAbbrevs)
                        Console.WriteLine("        .FSTR FSTR?DUMMY,\"\"");
                    Console.WriteLine("WORDS::");
                    for (int i = 1; i < num; i++)
                        Console.WriteLine("        FSTR?{0}", i);
                    for (int i = num; i < maxAbbrevs; i++)
                        Console.WriteLine("        FSTR?DUMMY");

                    Console.WriteLine();
                    Console.WriteLine("        .ENDI");

                    Console.Error.WriteLine("Abbrevs would save {0} z-chars total (~{1} bytes)",
                        totalSavings, totalSavings * 2 / 3);
                }

                // report success or failure
                if (ctx.ErrorCount > 0)
                {
                    if (!ctx.Quiet)
                    {
                        Console.Error.WriteLine();
                        Console.Error.WriteLine("Failed ({0} error{1})",
                            ctx.ErrorCount,
                            ctx.ErrorCount == 1 ? "" : "s");
                    }
                    return 2;
                }

                return 0;
            }
            finally
            {
                ctx.CloseOutput();
                ctx.CloseDebugFile();
            }
        }

        private static string SanitizeString(string text)
        {
            // escape '"' as '""'
            StringBuilder sb = new StringBuilder(text);

            for (int i = sb.Length - 1; i >= 0; i--)
                if (sb[i] == '"')
                    sb.Insert(i, '"');

            return sb.ToString();
        }

        private static Dictionary<string, KeyValuePair<ushort, ZOpAttribute>> MakeOpcodeDict(
            int zversion, bool inform)
        {
            FieldInfo[] fields = typeof(Opcodes).GetFields(BindingFlags.Static | BindingFlags.Public);
            var result = new Dictionary<string, KeyValuePair<ushort, ZOpAttribute>>(fields.Length);

            int effectiveVersion = zversion;

            if (zversion == 7)
                effectiveVersion = 6;
            else if (zversion == 8)
                effectiveVersion = 5;

            foreach (FieldInfo fi in fields)
            {
                object[] attrs = fi.GetCustomAttributes(typeof(ZOpAttribute), false);
                foreach (ZOpAttribute attr in attrs)
                    if (effectiveVersion >= attr.MinVer && effectiveVersion <= attr.MaxVer)
                    {
                        var pair = new KeyValuePair<ushort, ZOpAttribute>((ushort)fi.GetValue(null), attr);
                        result.Add(inform ? attr.InformName : attr.ClassicName, pair);
                        break;
                    }
            }

            return result;
        }

        private static Context ParseArgs(string[] args)
        {
            Context result = new Context();

            for (int i = 0; i < args.Length; i++)
            {
                switch (args[i])
                {
                    case "-ab":
                        result.AbbreviateMode = true;
                        break;

                    case "-q":
                        result.Quiet = true;
                        break;

                    case "-i":
                        result.InformMode = true;
                        break;

                    case "-la":
                        result.ListAddresses = true;
                        break;

                    case "-v":
                        if (++i == args.Length)
                            return null;
                        result.ZVersion = byte.Parse(args[i]);
                        break;

                    case "-r":
                        if (++i == args.Length)
                            return null;
                        result.Symbols["RELEASEID"] = new Symbol("RELEASEID", SymbolType.Constant, int.Parse(args[i]), 1);
                        break;

                    case "-s":
                        if (++i == args.Length)
                            return null;
                        result.Serial = args[i];
                        break;

                    case "-c":
                        if (++i == args.Length)
                            return null;
                        result.Creator = args[i];
                        break;

                    case "-c0":
                        result.Creator = null;
                        break;

                    case "-?":
                    case "--help":
                    case "/?":
                        return null;

                    default:
                        if (result.InFile == null)
                            result.InFile = args[i];
                        else if (result.OutFile == null)
                            result.OutFile = args[i];
                        else
                            return null;
                        break;
                }
            }

            // validate
            if (result.InFile == null)
                return null;

            if (result.OutFile == null)
                result.OutFile = Path.ChangeExtension(result.InFile, ".z#");

            result.DebugFile = Path.ChangeExtension(result.OutFile, ".dbg");

            return result;
        }

        private static void Usage()
        {
            Console.Error.WriteLine(VERSION);
            Console.Error.WriteLine(
@"Assemble: zapf [switches] <inFile.zap> [<outFile.z#>]

General switches:
  -i                    use Inform-like syntax
  -q                    quiet (no banner)
  -v #                  set default Z-machine version (.NEW overrides this)
  -la                   list global label addresses
  -r #                  set release number (RELEASEID overrides this)
  -s ######             set serial number
  -c ####               set creator version
  -ab                   also optimize abbreviations and print ZAPF code");

        }

        private static void Assemble(Context ctx)
        {
            // read in all source code
            List<ITree> file;
            ctx.PushFile(ctx.InFile);
            try
            {
                object root = ReadRootFromFile(ctx, ctx.InFile);
                file = new List<ITree>(ReadAllCode(ctx, root));
            }
            catch (IOException ex)
            {
                Errors.ThrowFatal(ex.Message);
                return; // never gets here
            }

            // first pass: discover label addresses and header flags
            if (!ctx.Quiet)
                Console.Error.Write("Measuring");

            ctx.CurrentPass = 1;

            // write dummy header
            ctx.Position = 0;
            if (ctx.ZVersion < 5)
                WriteHeader(ctx, false);

            for (int i = 0; i < file.Count; i++)
                try
                {
                    if (file[i].Type == ZapParser.END)
                        break;
                    else
                        PassOne(ctx, file[i]);
                }
                catch (SeriousError ser)
                {
                    ctx.HandleSeriousError(ser);
                }

            if (!ctx.Quiet)
                Console.Error.WriteLine();

            if (ctx.ErrorCount == 0)
            {
                // open output file and write mostly-real header
                ctx.OpenOutput();
                if (ctx.ZVersion < 5)
                {
                    try
                    {
                        WriteHeader(ctx, true);
                    }
                    catch (SeriousError ser)
                    {
                        ctx.HandleSeriousError(ser);
                    }
                }
            }

            // stop early if errors detected
            foreach (Symbol sym in ctx.Symbols.Values)
                if (sym.Pass == 0 || sym.Type == SymbolType.Unknown)
                    Errors.Serious(ctx, "undefined symbol: {0}", sym.Name);

            if (ctx.ErrorCount > 0)
                return;

            // second pass: generate object code
            ctx.ResetBetweenPasses();

            if (!ctx.Quiet)
                Console.Error.WriteLine("Assembling");

            ctx.CurrentPass = 2;
            IEnumerable<bool> longFormSequence = ctx.BranchOptimizer.Bake();
            using (IEnumerator<bool> longFormEnumerator = longFormSequence.GetEnumerator())
            {
                for (int i = 0; i < file.Count; i++)
                    try
                    {
                        if (file[i].Type == ZapParser.END)
                            break;
                        else
                            PassTwo(ctx, file[i], longFormEnumerator);
                    }
                    catch (SeriousError ser)
                    {
                        ctx.HandleSeriousError(ser);
                    }
            }

            // pad if necessary, finalize header length and checksum, close file
            try
            {
                FinalizeOutput(ctx);
            }
            catch (SeriousError ser)
            {
                ctx.HandleSeriousError(ser);
            }
        }

        /// <summary>
        /// Apply the first assembler pass to a node.
        /// </summary>
        /// <remarks>
        /// This pass discovers label addresses and header flags.
        /// It also handles the .NEW directive.
        /// </remarks>
        /// <param name="ctx">The current context.</param>
        /// <param name="node">The node to process.</param>
        private static void PassOne(Context ctx, ITree node)
        {
            switch (node.Type)
            {
                case ZapParser.NEW:
                    int version = (node.ChildCount == 0) ? 4 : EvalExpr(ctx, node.GetChild(0)).Value;
                    if (version < 3 || version > 8)
                        Errors.ThrowFatal("Only Z-machine versions 3-8 are supported");
                    if (version != ctx.ZVersion)
                    {
                        ctx.ZVersion = (byte)version;
                        ctx.OpcodeDict = MakeOpcodeDict(ctx.ZVersion, ctx.InformMode);
                        throw new RestartException();
                    }
                    break;

                case ZapParser.OPCODE:
                    HandleInstruction(ctx, node,
                        (address, possibleSavings, allowSpan, target) =>
                        {
                            ctx.BranchOptimizer.RecordSDI(address, possibleSavings, allowSpan, target);
                            return true;
                        });
                    break;

                case ZapParser.SYMBOL:
                    if (node.ChildCount != 0)
                        Errors.Serious(ctx, node, "unrecognized opcode: " + node.Text);
                    else
                        HandleDirective(ctx, node, false);
                    break;

                case ZapParser.LLABEL:
                case ZapParser.GLABEL:
                    HandleLabel(ctx, node, ctx.BranchOptimizer.RecordLabel);
                    break;

                default:
                    HandleDirective(ctx, node, false);
                    break;
            }
        }

        private static void WriteHeader(Context ctx, bool strict)
        {
            // Z-code version and flags 1 byte
            ctx.WriteByte(ctx.ZVersion);
            ctx.WriteByte(ctx.ZFlags);
            // release number
            ctx.WriteWord((ushort)GetHeaderValue(ctx, "RELEASEID", "ZORKID", false));
            // high memory mark (ENDLOD)
            int endlod = GetHeaderValue(ctx, "ENDLOD", strict);
            ctx.WriteWord((ushort)endlod);
            // initial program counter (START)
            int start = GetHeaderValue(ctx, "START", strict);
            ctx.WriteWord((ushort)start);
            // pointer to dictionary (VOCAB)
            ctx.WriteWord((ushort)GetHeaderValue(ctx, "VOCAB", strict));
            // pointer to objects (OBJECT)
            ctx.WriteWord((ushort)GetHeaderValue(ctx, "OBJECT", strict));
            // pointer to global variables (GLOBAL)
            ctx.WriteWord((ushort)GetHeaderValue(ctx, "GLOBAL", strict));
            // static memory mark (IMPURE - the memory *below* this point is impure)
            int impure = GetHeaderValue(ctx, "IMPURE", strict);
            ctx.WriteWord((ushort)impure);
            // flags 2 word
            ctx.WriteWord(0);   //XXX fill in flags2
            // serial number (6 bytes)
            ctx.WriteWord(0);   // filled in later
            ctx.WriteWord(0);
            ctx.WriteWord(0);
            // pointer to abbreviations (WORDS)
            ctx.WriteWord((ushort)GetHeaderValue(ctx, "WORDS", strict));
            // packed program length
            ctx.WriteWord(0);   // we'll fill this in later
            // checksum
            ctx.WriteWord(0);   // this too

            while (ctx.Position < 64)
                ctx.WriteByte(0);

            // validate header fields
            if (start > 65536)
                Errors.ThrowSerious("START must be in the first 64k (currently {0})", start);
            if (impure > 65536)
                Errors.ThrowSerious("IMPURE must be in the first 64k (currently {0})", impure);
            if (endlod < impure)
                Errors.ThrowSerious("ENDLOD must be after IMPURE");
        }

        private static int GetHeaderValue(Context ctx, string name, bool required)
        {
            return GetHeaderValue(ctx, name, null, required);
        }

        private static int GetHeaderValue(Context ctx, string name1, string name2, bool required)
        {
            Symbol sym;
            if (ctx.Symbols.TryGetValue(name1, out sym) ||
                (name2 != null && ctx.Symbols.TryGetValue(name2, out sym)))
            {
                switch (sym.Type)
                {
                    case SymbolType.GlobalLabel:
                    case SymbolType.Function:
                    case SymbolType.Constant:
                        return sym.Value;

                    default:
                        return 0;
                }
            }
            else
            {
                if (required)
                    Errors.Serious(ctx, "required global symbol '{0}' is missing", name1);
                return 0;
            }
        }

        private static Symbol GetDebugMapValue(Context ctx, string name)
        {
            Symbol sym;
            if (ctx.Symbols.TryGetValue(name, out sym))
                return sym;
            else
                return null;
        }

        private static void FinalizeOutput(Context ctx)
        {
            const int MINSIZE = 512;

            // pad file to a minimum size (for the benefit of tools that assume one)
            while (ctx.Position < MINSIZE)
                ctx.WriteByte(0);

            // pad file to a nice round number
            while (ctx.Position % ctx.HeaderLengthDivisor != 0)
                ctx.WriteByte(0);

            // calculate file length
            int length = ctx.Position;

            int maxLength = 0;
            switch (ctx.ZVersion)
            {
                case 1:
                case 2:
                case 3:
                    maxLength = 128;
                    break;
                case 4:
                case 5:
                    maxLength = 256;
                    break;
                case 7:
                    maxLength = 320;
                    break;
                case 6:
                case 8:
                    maxLength = 512;
                    break;
            }

            if (length > maxLength * 1024)
                Errors.ThrowSerious("file length of {0} exceeds V{1} maximum of {2}K",
                    length, ctx.ZVersion, maxLength);

            // calculate file checksum
            ctx.Position = 64;
            ushort checksum = 0;

            while (ctx.Position < length)
                checksum += ctx.ReadByte();

            // write Z-code version into header
            ctx.Position = 0;
            ctx.WriteByte(ctx.ZVersion);

            // write serial number into header
            if (ctx.Serial == null)
                ctx.Serial = DateTime.Now.ToString("yyMMdd");
            else if (ctx.Serial.Length != 6)
                ctx.Serial = ctx.Serial.PadRight(6).Substring(0, 6);

            ctx.Position = 0x12;
            foreach (char c in ctx.Serial)
                ctx.WriteByte((byte)c);

            // write length and checksum into header
            ctx.Position = 0x1A;
            ctx.WriteWord((ushort)(length / ctx.HeaderLengthDivisor));
            ctx.WriteWord(checksum);

            // write creator ID
            if (ctx.Creator != null)
            {
                if (ctx.Creator.Length != 4)
                    ctx.Creator = ctx.Creator.PadRight(4).Substring(0, 4);

                ctx.Position = 0x3C;
                foreach (char c in ctx.Creator)
                    ctx.WriteByte((byte)c);
            }

            // done
            if (!ctx.Quiet)
                Console.Error.WriteLine("Wrote {0} bytes to {1}", length, ctx.OutFile);

            // finalize debug file
            if (ctx.IsDebugFileOpen)
            {
                // finish map
                if (!ctx.DebugFileMap.ContainsKey(DEBF.AbbrevMapName))
                    ctx.DebugFileMap[DEBF.AbbrevMapName] = GetDebugMapValue(ctx, "WORDS");
                if (!ctx.DebugFileMap.ContainsKey(DEBF.GlobalsMapName))
                    ctx.DebugFileMap[DEBF.GlobalsMapName] = GetDebugMapValue(ctx, "GLOBAL");
                if (!ctx.DebugFileMap.ContainsKey(DEBF.ObjectsMapName))
                {
                    Symbol objTable = GetDebugMapValue(ctx, "OBJECT");
                    if (objTable != null)
                    {
                        Symbol objTree = new Symbol();
                        objTree.Type = objTable.Type;
                        objTree.SetValue(objTable.Value + (ctx.ZVersion < 4 ? 31 : 63), ctx.CurrentPass);
                        ctx.DebugFileMap[DEBF.ObjectsMapName] = objTree;
                    }
                }
                if (!ctx.DebugFileMap.ContainsKey(DEBF.PropsMapName))
                    ctx.DebugFileMap[DEBF.PropsMapName] = GetDebugMapValue(ctx, "OBJECT");
                if (!ctx.DebugFileMap.ContainsKey(DEBF.VocabMapName))
                    ctx.DebugFileMap[DEBF.VocabMapName] = GetDebugMapValue(ctx, "VOCAB");

                // write map
                ctx.WriteDebugByte(DEBF.MAP_DBR);
                foreach (var pair in ctx.DebugFileMap)
                {
                    ctx.WriteDebugString(pair.Key);
                    ctx.WriteDebugAddress(pair.Value.Value);
                }
                ctx.WriteDebugByte(0);

                // write header
                ctx.WriteDebugByte(DEBF.HEADER_DBR);
                ctx.Position = 0;
                for (int i = 0; i < 64; i++)
                    ctx.WriteDebugByte(ctx.ReadByte());

                // done
                ctx.CloseDebugFile();

                if (!ctx.Quiet)
                    Console.Error.WriteLine("Wrote debugging info to {0}", ctx.DebugFile);
            }

            ctx.CloseOutput();
        }

        /// <summary>
        /// Apply the second assembler pass to a node.
        /// </summary>
        /// <remarks>
        /// This pass generates code.
        /// </remarks>
        /// <param name="ctx">The current context.</param>
        /// <param name="node">The node to process.</param>
        /// <param name="longFormEnumerator">A sequence of booleans indicating which span-dependent
        /// instructions need to be assembled in long form.</param>
        private static void PassTwo(Context ctx, ITree node, IEnumerator<bool> longFormEnumerator)
        {
            switch (node.Type)
            {
                case ZapParser.OPCODE:
                    HandleInstruction(ctx, node,
                        (address, possibleSavings, allowSpan, target) =>
                        {
                            longFormEnumerator.MoveNext();
                            return longFormEnumerator.Current;
                        });
                    break;

                case ZapParser.LLABEL:
                case ZapParser.GLABEL:
                    HandleLabel(ctx, node, ctx.BranchOptimizer.RecordLabel);
                    break;

                default:
                    HandleDirective(ctx, node, true);
                    break;
            }
        }

        private static IEnumerable<ITree> ReadAllCode(Context ctx, object root)
        {
            foreach (ITree node in GetRootNodes(root))
            {
                if (node.Type == ZapParser.INSERT)
                {
                    string arg = node.GetChild(0).Text;
                    string insertedFile = FindInsertedFile(arg);

                    if (insertedFile == null)
                        Errors.ThrowFatal(node, "inserted file not found: " + arg, "root");

                    ctx.PushFile(insertedFile);
                    try
                    {
                        object insertedRoot = ReadRootFromFile(ctx, insertedFile);
                        foreach (ITree insertedNode in ReadAllCode(ctx, insertedRoot))
                            if (insertedNode.Type == ZapParser.ENDI || insertedNode.Type == ZapParser.END)
                                break;
                            else
                                yield return insertedNode;
                    }
                    finally
                    {
                        ctx.PopFile();
                    }
                }
                else
                {
                    yield return node;
                }
            }
        }

        private static object ReadRootFromFile(Context ctx, string path)
        {
            ICharStream charStream = new ANTLRFileStream(path);
            ITokenSource lexer;

            if (ctx.InformMode)
                lexer = new ZapInf(charStream) { OpcodeDict = ctx.OpcodeDict };
            else
                lexer = new ZapLexer(charStream) { OpcodeDict = ctx.OpcodeDict };

            ZapParser parser = new ZapParser(new CommonTokenStream(lexer));
            parser.InformMode = ctx.InformMode;
            ZapParser.file_return fret = parser.file();

            if (parser.NumberOfSyntaxErrors > 0)
                Errors.ThrowFatal("syntax error");

            return fret.Tree;
        }

        private static string FindInsertedFile(string name)
        {
            if (File.Exists(name))
                return name;

            string search = name + ".zap";
            if (File.Exists(search))
                return search;

            search = name + ".xzap";
            if (File.Exists(search))
                return search;

            return null;
        }

        private static IEnumerable<ITree> GetRootNodes(object root)
        {
            ITree tree = (ITree)root;

            if (tree.Type == 0)
            {
                for (int i = 0; i < tree.ChildCount; i++)
                    yield return tree.GetChild(i);
            }
            else
            {
                yield return tree;
            }
        }

        private static Symbol EvalExpr(Context ctx, ITree node)
        {
            switch (node.Type)
            {
                case ZapParser.NUM:
                    return new Symbol(int.Parse(node.Text), ctx.CurrentPass);

                case ZapParser.SYMBOL:
                    Symbol result;
                    if (ctx.Symbols.TryGetValue(node.Text, out result))
                        return result;
                    if (ctx.TryGetLocalSymbol(node.Text, out result))
                        return result;
                    if (ctx.CurrentPass > 1)
                        Errors.ThrowFatal(node, "undefined symbol: " + node.Text, "node");
                    return new Symbol(node.Text);

                case ZapParser.PLUS:
                    Symbol left = EvalExpr(ctx, node.GetChild(0));
                    Symbol right = EvalExpr(ctx, node.GetChild(1));
                    if (ctx.CurrentPass == 1 &&
                        (left.Type == SymbolType.Unknown || right.Type == SymbolType.Unknown))
                    {
                        return new Symbol(null);
                    }
                    if (left.Type == SymbolType.Constant && right.Type == SymbolType.Constant)
                        return new Symbol(null, SymbolType.Constant, left.Value + right.Value, ctx.CurrentPass);
                    throw new NotImplementedException("Unimplemented symbol addition");

                default:
                    throw new NotImplementedException();
            }
        }

        private static void EvalOperand(Context ctx, ITree node, out byte type, out ushort value)
        {
            EvalOperand(ctx, node, out type, out value, false);
        }

        private static void EvalOperand(Context ctx, ITree node, out byte type, out ushort value,
            bool allowLocalLabel)
        {
            bool apos = false;
            if (node.Type == ZapParser.APOSTROPHE)
            {
                apos = true;
                node = node.GetChild(0);
            }

            switch (node.Type)
            {
                case ZapParser.NUM:
                    value = (ushort)int.Parse(node.Text);
                    if (value < 256)
                        type = OPERAND_BYTE;
                    else
                        type = OPERAND_WORD;
                    break;

                case ZapParser.SYMBOL:
                    Symbol sym;
                    if (ctx.TryGetLocalSymbol(node.Text, out sym))
                    {
                        if (sym.Type == SymbolType.LocalLabel)
                        {
                            if (!allowLocalLabel)
                                Errors.Serious(ctx, node, "local label used as operand");

                            // note: returned value is relative to the current position,
                            // which is probably not where the value will be written.
                            // caller must correct it.
                            type = OPERAND_WORD;
                            value = (ushort)(sym.Value - ctx.Position);
                        }
                        else
                        {
                            System.Diagnostics.Debug.Assert(sym.Type == SymbolType.Variable);
                            type = OPERAND_VAR;
                            value = (byte)sym.Value;
                        }
                    }
                    else if (ctx.Symbols.TryGetValue(node.Text, out sym))
                    {
                        if (sym.Type == SymbolType.Variable)
                            type = OPERAND_VAR;
                        else if ((sym.Value & 0xff) == sym.Value)
                            type = OPERAND_BYTE;
                        else
                            type = OPERAND_WORD;
                        value = (ushort)sym.Value;
                    }
                    else if (ctx.CurrentPass > 1 && !allowLocalLabel)
                    {
                        Errors.ThrowFatal(node, "undefined symbol: {0}", node.Text);
                        type = 0;
                        value = 0;
                    }
                    else
                    {
                        // not defined yet
                        type = OPERAND_BYTE;
                        value = 0;
                        ctx.SetLocalSymbol(node.Text, new Symbol(node.Text));
                    }
                    break;

                default:
                    throw new NotImplementedException();
            }

            if (apos)
                type = OPERAND_BYTE;
        }

        private static bool IsLongConstant(Context ctx, ITree node)
        {
            int value;

            switch (node.Type)
            {
                case ZapParser.NUM:
                    value = int.Parse(node.Text);
                    return ((uint)value & 0xffffff00) != 0;

                case ZapParser.SYMBOL:
                    Symbol sym;
                    if (ctx.TryGetLocalSymbol(node.Text, out sym))
                    {
                        // the only legal local symbol operand is a local variable
                        return false;
                    }
                    else if (ctx.Symbols.TryGetValue(node.Text, out sym))
                    {
                        return sym.Value < 0 || sym.Value > 255;
                    }
                    else
                    {
                        // not defined yet, assume it's a faraway global label
                        // (could also be a local label if we're assembling a JUMP instruction,
                        // but that's assembled as a word anyway)
                        return true;
                    }

                case ZapParser.PLUS:
                    return IsLongConstant(ctx, node.GetChild(0)) && IsLongConstant(ctx, node.GetChild(1));

                case ZapParser.STRING:
                case ZapParser.APOSTROPHE:
                    return false;

                default:
                    throw new ArgumentException("Unexpected expr type: " + ZapParser.tokenNames[node.Type], "node");
            }
        }

        private static void HandleDirective(Context ctx, ITree node, bool assembling)
        {
            // local scope is terminated by any directive except .DEBUG_LINE (not counting labels)
            if (node.Type != ZapParser.DEBUG_LINE)
                ctx.LeaveLocalScope();

            switch (node.Type)
            {
                case ZapParser.NEW:
                    // this is explicitly handled by PassOne
                    break;

                case ZapParser.FUNCT:
                    BeginFunction(ctx, node);
                    break;

                case ZapParser.TABLE:
                    if (ctx.TableStart != null)
                        Errors.Warn(node, "starting new table before ending old table");
                    ctx.TableStart = ctx.Position;
                    ctx.TableSize = null;
                    if (node.ChildCount > 0)
                    {
                        Symbol sym = EvalExpr(ctx, node.GetChild(0));
                        if (sym.Type != SymbolType.Constant)
                            Errors.Warn(node, "ignoring non-constant table size specifier");
                        else
                            ctx.TableSize = sym.Value;
                    }
                    break;

                case ZapParser.ENDT:
                    if (ctx.TableStart == null)
                        Errors.Warn(node, "ignoring .ENDT outside of a table definition");
                    if (ctx.TableSize != null)
                    {
                        if (ctx.Position - ctx.TableStart.Value != ctx.TableSize.Value)
                            Errors.Warn(node, "incorrect table size: expected {0}, actual {1}",
                                ctx.TableSize.Value,
                                ctx.Position - ctx.TableStart.Value);
                    }
                    ctx.TableStart = null;
                    ctx.TableSize = null;
                    break;

                case ZapParser.VOCBEG:
                    if (ctx.InVocab)
                    {
                        Errors.Warn(node, "ignoring .VOCBEG inside another vocabulary block");
                    }
                    else
                    {
                        Symbol sym1 = EvalExpr(ctx, node.GetChild(0));
                        Symbol sym2 = EvalExpr(ctx, node.GetChild(1));
                        if (sym1.Type != SymbolType.Constant || sym2.Type != SymbolType.Constant)
                            Errors.Warn(node, "ignoring .VOCBEG with non-constant size specifiers");
                        else
                            ctx.EnterVocab(sym1.Value, sym2.Value);
                    }
                    break;

                case ZapParser.VOCEND:
                    if (!ctx.InVocab)
                        Errors.Warn(node, "ignoring .VOCEND outside of a vocabulary block");
                    else
                        ctx.LeaveVocab();
                    break;

                case ZapParser.BYTE:
                case ZapParser.WORD:
                    for (int i = 0; i < node.ChildCount; i++)
                    {
                        Symbol sym = EvalExpr(ctx, node.GetChild(i));
                        if (sym.Type == SymbolType.Unknown && ctx.CurrentPass > 1)
                        {
                            Errors.ThrowFatal(node.GetChild(i), "unrecognized symbol");
                        }
                        if (node.Type == ZapParser.BYTE)
                        {
                            ctx.WriteByte((byte)sym.Value);
                        }
                        else
                        {
                            ctx.WriteWord((ushort)sym.Value);
                        }
                    }
                    break;

                case ZapParser.FSTR:
                    AddAbbreviation(ctx, node);
                    break;

                case ZapParser.GSTR:
                    PackString(ctx, node);
                    break;

                case ZapParser.STR:
                    ctx.WriteZString(node.GetChild(0).Text, false);
                    break;

                case ZapParser.STRL:
                    ctx.WriteZString(node.GetChild(0).Text, true);
                    break;

                case ZapParser.LEN:
                    ctx.WriteZStringLength(node.GetChild(0).Text);
                    break;

                case ZapParser.ZWORD:
                    ctx.WriteZWord(node.GetChild(0).Text);
                    break;

                case ZapParser.EQUALS:
                    Symbol rvalue = EvalExpr(ctx, node.GetChild(1));
                    if (rvalue.Type == SymbolType.Unknown && ctx.CurrentPass > 1)
                    {
                        Errors.ThrowFatal(node.GetChild(1), "unrecognized symbol");
                    }
                    else
                    {
                        ctx.Symbols[node.GetChild(0).Text] = rvalue;
                    }
                    break;

                case ZapParser.GVAR:
                    ctx.AddGlobalVar(node.GetChild(0).Text);
                    if (node.ChildCount > 1)
                    {
                        Symbol sym = EvalExpr(ctx, node.GetChild(1));
                        if (sym.Type == SymbolType.Unknown && ctx.CurrentPass > 1)
                        {
                            Errors.ThrowFatal(node.GetChild(1), "unrecognized symbol");
                        }
                        ctx.WriteWord((ushort)sym.Value);
                    }
                    else
                    {
                        ctx.WriteWord(0);
                    }
                    break;

                case ZapParser.OBJECT:
                    ctx.AddObject(node.GetChild(0).Text);
                    const string ObjectVersionError = "wrong .OBJECT syntax for this version";
                    if (ctx.ZVersion < 4)
                    {
                        if (node.ChildCount != 7)
                        {
                            Errors.Serious(ctx, ObjectVersionError);
                            break;
                        }

                        // 2 flag words
                        ctx.WriteWord(EvalExpr(ctx, node.GetChild(1)));
                        ctx.WriteWord(EvalExpr(ctx, node.GetChild(2)));
                        // 3 object link bytes
                        ctx.WriteByte(EvalExpr(ctx, node.GetChild(3)));
                        ctx.WriteByte(EvalExpr(ctx, node.GetChild(4)));
                        ctx.WriteByte(EvalExpr(ctx, node.GetChild(5)));
                        // property table
                        ctx.WriteWord(EvalExpr(ctx, node.GetChild(6)));
                    }
                    else
                    {
                        if (node.ChildCount != 8)
                        {
                            Errors.Serious(ctx, ObjectVersionError);
                            break;
                        }

                        // 3 flag words
                        ctx.WriteWord(EvalExpr(ctx, node.GetChild(1)));
                        ctx.WriteWord(EvalExpr(ctx, node.GetChild(2)));
                        ctx.WriteWord(EvalExpr(ctx, node.GetChild(3)));
                        // 3 object link words
                        ctx.WriteWord(EvalExpr(ctx, node.GetChild(4)));
                        ctx.WriteWord(EvalExpr(ctx, node.GetChild(5)));
                        ctx.WriteWord(EvalExpr(ctx, node.GetChild(6)));
                        // property table
                        ctx.WriteWord(EvalExpr(ctx, node.GetChild(7)));
                    }
                    break;

                case ZapParser.PROP:
                    Symbol size = EvalExpr(ctx, node.GetChild(0));
                    Symbol prop = EvalExpr(ctx, node.GetChild(1));
                    if (ctx.CurrentPass > 1 &&
                        (size.Type != SymbolType.Constant || prop.Type != SymbolType.Constant))
                        Errors.Serious(ctx, node, "non-constant arguments to .PROP");
                    if (ctx.ZVersion < 4)
                    {
                        if (size.Value > 8)
                            Errors.Serious(ctx, node, "property too long (8 bytes max in V3)");
                        ctx.WriteByte((byte)(32 * (size.Value - 1) + prop.Value));
                    }
                    else if (size.Value > 2)
                    {
                        if (size.Value > 64)
                            Errors.Serious(ctx, node, "property too long (64 bytes max in V4+)");
                        ctx.WriteByte((byte)(prop.Value | 128));
                        ctx.WriteByte((byte)(size.Value | 128));
                    }
                    else
                    {
                        byte b = (byte)prop.Value;
                        if (size.Value == 2)
                            b |= 64;
                        ctx.WriteByte(b);
                    }
                    break;

                case ZapParser.DEBUG_ACTION:
                case ZapParser.DEBUG_ARRAY:
                case ZapParser.DEBUG_ATTR:
                case ZapParser.DEBUG_CLASS:
                case ZapParser.DEBUG_FAKE_ACTION:
                case ZapParser.DEBUG_FILE:
                case ZapParser.DEBUG_GLOBAL:
                case ZapParser.DEBUG_LINE:
                case ZapParser.DEBUG_MAP:
                case ZapParser.DEBUG_OBJECT:
                case ZapParser.DEBUG_PROP:
                case ZapParser.DEBUG_ROUTINE:
                case ZapParser.DEBUG_ROUTINE_END:
                    if (assembling)
                    {
                        if (!ctx.IsDebugFileOpen)
                            ctx.OpenDebugFile();

                        HandleDebugDirective(ctx, node);
                    }
                    break;

                default:
                    throw new NotImplementedException();
            }
        }

        private static void HandleDebugDirective(Context ctx, ITree node)
        {
            if (ctx.DebugRoutinePoints != -1 &&
                node.Type != ZapParser.DEBUG_LINE &&
                node.Type != ZapParser.DEBUG_ROUTINE_END)
            {
                Errors.Serious(ctx, node, "debug directives other than .DEBUG-LINE not allowed inside routines");
                return;
            }

            Symbol sym1, sym2;
            switch (node.Type)
            {
                case ZapParser.DEBUG_ACTION:
                    ctx.WriteDebugByte(DEBF.ACTION_DBR);
                    ctx.WriteDebugWord((ushort)EvalExpr(ctx, node.GetChild(0)).Value);
                    ctx.WriteDebugString(node.GetChild(1).Text);
                    break;
                case ZapParser.DEBUG_ARRAY:
                    if (ctx.Symbols.TryGetValue("GLOBAL", out sym1) == false)
                    {
                        Errors.Serious(ctx, node, "define GLOBAL before using .DEBUG-ARRAY");
                        return;
                    }
                    ctx.WriteDebugByte(DEBF.ARRAY_DBR);
                    sym2 = EvalExpr(ctx, node.GetChild(0));
                    ctx.WriteDebugWord((ushort)(sym2.Value - sym1.Value));
                    ctx.WriteDebugString(node.GetChild(1).Text);
                    break;
                case ZapParser.DEBUG_ATTR:
                    ctx.WriteDebugByte(DEBF.ATTR_DBR);
                    ctx.WriteDebugWord((ushort)EvalExpr(ctx, node.GetChild(0)).Value);
                    ctx.WriteDebugString(node.GetChild(1).Text);
                    break;
                case ZapParser.DEBUG_CLASS:
                    ctx.WriteDebugByte(DEBF.CLASS_DBR);
                    ctx.WriteDebugString(node.GetChild(0).Text);
                    ctx.WriteDebugLineRef(
                        (byte)EvalExpr(ctx, node.GetChild(1)).Value,
                        (ushort)EvalExpr(ctx, node.GetChild(2)).Value,
                        (byte)EvalExpr(ctx, node.GetChild(3)).Value);
                    ctx.WriteDebugLineRef(
                        (byte)EvalExpr(ctx, node.GetChild(4)).Value,
                        (ushort)EvalExpr(ctx, node.GetChild(5)).Value,
                        (byte)EvalExpr(ctx, node.GetChild(6)).Value);
                    break;
                case ZapParser.DEBUG_FAKE_ACTION:
                    ctx.WriteDebugByte(DEBF.FAKE_ACTION_DBR);
                    ctx.WriteDebugWord((ushort)EvalExpr(ctx, node.GetChild(0)).Value);
                    ctx.WriteDebugString(node.GetChild(1).Text);
                    break;
                case ZapParser.DEBUG_FILE:
                    ctx.WriteDebugByte(DEBF.FILE_DBR);
                    ctx.WriteDebugByte((byte)EvalExpr(ctx, node.GetChild(0)).Value);
                    ctx.WriteDebugString(node.GetChild(1).Text);
                    ctx.WriteDebugString(node.GetChild(2).Text);
                    break;
                case ZapParser.DEBUG_GLOBAL:
                    ctx.WriteDebugByte(DEBF.GLOBAL_DBR);
                    ctx.WriteDebugByte((byte)(EvalExpr(ctx, node.GetChild(0)).Value - 16));
                    ctx.WriteDebugString(node.GetChild(1).Text);
                    break;
                case ZapParser.DEBUG_LINE:
                    if (ctx.DebugRoutinePoints < 0)
                    {
                        Errors.Serious(ctx, node, ".DEBUG-LINE outside of .DEBUG-ROUTINE");
                    }
                    else
                    {
                        ctx.WriteDebugLineRef(
                            (byte)EvalExpr(ctx, node.GetChild(0)).Value,
                            (ushort)EvalExpr(ctx, node.GetChild(1)).Value,
                            (byte)EvalExpr(ctx, node.GetChild(2)).Value);
                        ctx.WriteDebugWord((ushort)(ctx.Position - ctx.DebugRoutineStart));
                        ctx.DebugRoutinePoints++;
                    }
                    break;
                case ZapParser.DEBUG_MAP:
                    ctx.DebugFileMap[node.GetChild(0).Text] = EvalExpr(ctx, node.GetChild(1));
                    break;
                case ZapParser.DEBUG_OBJECT:
                    ctx.WriteDebugByte(DEBF.OBJECT_DBR);
                    ctx.WriteDebugWord((ushort)EvalExpr(ctx, node.GetChild(0)).Value);
                    ctx.WriteDebugString(node.GetChild(1).Text);
                    ctx.WriteDebugLineRef(
                        (byte)EvalExpr(ctx, node.GetChild(2)).Value,
                        (ushort)EvalExpr(ctx, node.GetChild(3)).Value,
                        (byte)EvalExpr(ctx, node.GetChild(4)).Value);
                    ctx.WriteDebugLineRef(
                        (byte)EvalExpr(ctx, node.GetChild(5)).Value,
                        (ushort)EvalExpr(ctx, node.GetChild(6)).Value,
                        (byte)EvalExpr(ctx, node.GetChild(7)).Value);
                    break;
                case ZapParser.DEBUG_PROP:
                    ctx.WriteDebugByte(DEBF.PROP_DBR);
                    ctx.WriteDebugWord((ushort)EvalExpr(ctx, node.GetChild(0)).Value);
                    ctx.WriteDebugString(node.GetChild(1).Text);
                    break;
                case ZapParser.DEBUG_ROUTINE:
                    ctx.WriteDebugByte(DEBF.ROUTINE_DBR);
                    ctx.WriteDebugWord(ctx.NextDebugRoutine);
                    ctx.WriteDebugLineRef(
                        (byte)EvalExpr(ctx, node.GetChild(0)).Value,
                        (ushort)EvalExpr(ctx, node.GetChild(1)).Value,
                        (byte)EvalExpr(ctx, node.GetChild(2)).Value);
                    AlignRoutine(ctx);
                    ctx.WriteDebugAddress(ctx.Position);
                    ctx.WriteDebugString(node.GetChild(3).Text);
                    for (int i = 4; i < node.ChildCount; i++)
                        ctx.WriteDebugString(node.GetChild(i).Text);
                    ctx.WriteDebugByte(0);
                    // start LINEREF_DBR block
                    ctx.WriteDebugByte(DEBF.LINEREF_DBR);
                    ctx.WriteDebugWord(ctx.NextDebugRoutine);
                    ctx.WriteDebugWord(0);      // # sequence points, filled in later
                    ctx.DebugRoutinePoints = 0;
                    ctx.DebugRoutineStart = ctx.Position;
                    break;
                case ZapParser.DEBUG_ROUTINE_END:
                    // finish LINEREF_DBR block
                    if (ctx.DebugRoutinePoints < 0)
                    {
                        Errors.Serious(ctx, node, ".DEBUG-ROUTINE-END outside of .DEBUG-ROUTINE");
                    }
                    else
                    {
                        int curPosition = ctx.DebugPosition;
                        ctx.DebugPosition -= 2 + ctx.DebugRoutinePoints * 6;
                        ctx.WriteDebugWord((ushort)ctx.DebugRoutinePoints);
                        ctx.DebugPosition = curPosition;
                        ctx.DebugRoutinePoints = -1;
                        ctx.DebugRoutineStart = -1;
                    }
                    // write ROUTINE_END_DBR block
                    ctx.WriteDebugByte(DEBF.ROUTINE_END_DBR);
                    ctx.WriteDebugWord(ctx.NextDebugRoutine++);
                    ctx.WriteDebugLineRef(
                        (byte)EvalExpr(ctx, node.GetChild(0)).Value,
                        (ushort)EvalExpr(ctx, node.GetChild(1)).Value,
                        (byte)EvalExpr(ctx, node.GetChild(2)).Value);
                    ctx.WriteDebugAddress(ctx.Position);
                    break;
            }
        }

        private static void BeginFunction(Context ctx, ITree node)
        {
            List<string> localNames = new List<string>();
            List<ushort> localValues = new List<ushort>();
            bool gotDefaultValues = false;

            string name = node.GetChild(0).Text;
            if (node.ChildCount > 1)
            {
                localNames.Capacity = localValues.Capacity = node.ChildCount - 1;

                for (int i = 1; i < node.ChildCount; i++)
                {
                    ITree child = node.GetChild(i);
                    if (child.Type == ZapParser.EQUALS)
                    {
                        gotDefaultValues = true;
                        string lvname = child.GetChild(0).Text;
                        localNames.Add(lvname);
                        ctx.SetLocalSymbol(lvname,
                            new Symbol(lvname, SymbolType.Variable, localNames.Count, ctx.CurrentPass));

                        Symbol defv = EvalExpr(ctx, child.GetChild(1));
                        localValues.Add((ushort)defv.Value);
                    }
                    else if (child.Type == ZapParser.SYMBOL)
                    {
                        localNames.Add(child.Text);
                        ctx.SetLocalSymbol(child.Text,
                            new Symbol(child.Text, SymbolType.Variable, localNames.Count, ctx.CurrentPass));
                        localValues.Add(0);
                    }
                }
            }

            if (localNames.Count > 15)
                Errors.ThrowSerious(node, "too many local variables");

            AlignRoutine(ctx);

            Symbol sym;
            int paddr = ctx.Position / ctx.PackingDivisor;
            if (ctx.Symbols.TryGetValue(name, out sym) == false)
            {
                sym = new Symbol(name, SymbolType.Function, paddr, ctx.CurrentPass);
                ctx.Symbols.Add(name, sym);
            }
            else if (sym.Pass == ctx.CurrentPass || sym.Type != SymbolType.Function)
            {
                Errors.ThrowSerious("function redefined: " + name);
            }
            else
            {
                sym.SetValue(paddr, ctx.CurrentPass);
            }

            ctx.EnterLocalScope(sym);

            ctx.WriteByte((byte)localNames.Count);

            if (ctx.ZVersion < 5)
            {
                foreach (ushort val in localValues)
                    ctx.WriteWord(val);
            }
            else if (gotDefaultValues)
            {
                Errors.Warn(node, "ignoring default local variable values");
            }
        }

        private static void AlignRoutine(Context ctx)
        {
            while (ctx.Position % ctx.PackingDivisor != 0)
                ctx.WriteByte(0);
        }

        private static void PackString(Context ctx, ITree node)
        {
            string name = node.GetChild(0).Text;

            AlignString(ctx);

            Symbol sym;
            int paddr = ctx.Position / ctx.PackingDivisor;
            if (ctx.Symbols.TryGetValue(name, out sym) == false)
            {
                sym = new Symbol(name, SymbolType.String, paddr, ctx.CurrentPass);
                ctx.Symbols.Add(name, sym);
            }
            else if (sym.Pass == ctx.CurrentPass || sym.Type != SymbolType.String)
            {
                Errors.ThrowSerious("string redefined: " + name);
            }
            else
            {
                sym.SetValue(paddr, ctx.CurrentPass);
            }

            ctx.WriteZString(node.GetChild(1).Text, false);
        }

        private static void AlignString(Context ctx)
        {
            while (ctx.Position % ctx.PackingDivisor != 0)
                ctx.WriteByte(0);
        }

        private static void AddAbbreviation(Context ctx, ITree node)
        {
            if (ctx.StringEncoder.AbbrevsFrozen)
                Errors.ThrowSerious(node, "abbreviations must be defined before strings");

            string name = node.GetChild(0).Text;

            if (ctx.Position % 2 != 0)
                ctx.WriteByte(0);

            Symbol sym;
            if (ctx.Symbols.TryGetValue(name, out sym) == false)
            {
                sym = new Symbol(name);
                ctx.Symbols.Add(name, sym);
            }
            sym.Type = SymbolType.Constant;
            sym.SetValue(ctx.Position / 2, ctx.CurrentPass);

            string text = node.GetChild(1).Text;
            ctx.WriteZString(text, false, true);

            if (text.Length > 0)
                ctx.StringEncoder.AddAbbreviation(text);
        }

        private const byte OPERAND_WORD = 0;
        private const byte OPERAND_BYTE = 1;
        private const byte OPERAND_VAR = 2;
        private const byte OPERAND_OMITTED = 3;

        private static byte[] tmpOperandTypes = new byte[8];
        private static ushort[] tmpOperandValues = new ushort[8];

        private delegate bool BranchShortener(int address, int possibleSavings,
            Predicate<int> allowSpan, Symbol target);

        private static void HandleInstruction(Context ctx, ITree node,
            BranchShortener shortenBranch)
        {
            var pair = ctx.OpcodeDict[node.Text];
            ushort opcode = pair.Key;
            ZOpAttribute attr = pair.Value;

            List<ITree> operands = new List<ITree>();
            ITree store = null, branch = null;
            bool usesLongConstants = false;

            for (int i = 0; i < node.ChildCount; i++)
            {
                ITree child = node.GetChild(i);
                switch (child.Type)
                {
                    case ZapParser.SLASH:
                    case ZapParser.BACKSLASH:
                        if (branch == null)
                            branch = child;
                        else
                            Errors.ThrowFatal(child, "multiple branch operands");
                        break;

                    case ZapParser.RANGLE:
                        if (store == null)
                            store = child;
                        else
                            Errors.ThrowFatal(child, "multiple store operands");
                        break;

                    default:
                        if (branch != null || store != null)
                            Errors.ThrowFatal(child, "normal operand after branch/store operand");
                        operands.Add(child);
                        if (IsLongConstant(ctx, child))
                            usesLongConstants = true;
                        break;
                }
            }

            // force a 2OP instruction to EXT mode if it uses long constants
            // or has more than 2 operands
            if (opcode < 128 && (usesLongConstants || operands.Count > 2))
                opcode += 192;

            if (opcode < 128)
            {
                // 2OP
                if (operands.Count != 2)
                    Errors.ThrowSerious(node, "expected 2 operands");

                byte b = (byte)opcode;
                EvalOperand(ctx, operands[0], out tmpOperandTypes[0], out tmpOperandValues[0]);
                EvalOperand(ctx, operands[1], out tmpOperandTypes[1], out tmpOperandValues[1]);
                System.Diagnostics.Debug.Assert(tmpOperandTypes[0] != OPERAND_WORD);
                System.Diagnostics.Debug.Assert(tmpOperandTypes[1] != OPERAND_WORD);

                if (tmpOperandTypes[0] == OPERAND_VAR)
                    b |= 0x40;
                if (tmpOperandTypes[1] == OPERAND_VAR)
                    b |= 0x20;

                ctx.WriteByte(b);
                ctx.WriteByte((byte)tmpOperandValues[0]);
                ctx.WriteByte((byte)tmpOperandValues[1]);
            }
            else if (opcode < 176)
            {
                // 1OP
                if (operands.Count != 1)
                    Errors.ThrowSerious(node, "expected 1 operand");

                byte b = (byte)opcode;
                EvalOperand(ctx, operands[0], out tmpOperandTypes[0], out tmpOperandValues[0],
                    (attr.Flags & ZOpFlags.Label) != 0);

                if ((attr.Flags & ZOpFlags.Label) != 0)
                {
                    // correct label offset (-3 for the opcode and operand, +2 for the normal jump bias)
                    tmpOperandValues[0]--;

                    if (operands[0].Type == ZapParser.SYMBOL)
                    {
                        Symbol sym;
                        if (ctx.TryGetLocalSymbol(operands[0].Text, out sym) == false)
                            sym = ctx.Symbols[operands[0].Text];

                        if (shortenBranch(ctx.Position + 1, 1, n => (n >= 0 && n <= 255), sym))
                            tmpOperandTypes[0] = OPERAND_BYTE;
                        else
                            tmpOperandTypes[0] = OPERAND_WORD;
                    }
                }

                b |= (byte)(tmpOperandTypes[0] << 4);

                ctx.WriteByte(b);
                if (tmpOperandTypes[0] == OPERAND_WORD)
                    ctx.WriteWord(tmpOperandValues[0]);
                else
                    ctx.WriteByte((byte)tmpOperandValues[0]);
            }
            else if (opcode < 192)
            {
                // 0OP
                if ((attr.Flags & ZOpFlags.String) == 0)
                {
                    if (operands.Count != 0)
                        Errors.ThrowSerious(node, "expected 0 operands");

                    ctx.WriteByte((byte)opcode);
                }
                else
                {
                    if (operands.Count != 1)
                        Errors.ThrowSerious(node, "expected literal string as only operand");

                    ctx.WriteByte((byte)opcode);
                    ctx.WriteZString(operands[0].Text, false);
                }
            }
            else
            {
                // EXT
                int maxArgs = ((attr.Flags & ZOpFlags.Extra) == 0) ? 4 : 8;

                if (operands.Count > maxArgs)
                    Errors.ThrowSerious(node, "expected 0-{0} operands", maxArgs);

                if (opcode >= 256)
                {
                    ctx.WriteByte(190);
                    ctx.WriteByte((byte)(opcode - 256));
                }
                else
                    ctx.WriteByte((byte)opcode);

                // operand types
                byte typeByte = 0;
                for (int i = 0; i < maxArgs; i++)
                {
                    byte t;
                    if (i < operands.Count)
                    {
                        EvalOperand(ctx, operands[i], out t, out tmpOperandValues[i]);
                        tmpOperandTypes[i] = t;
                    }
                    else
                        t = OPERAND_OMITTED;

                    typeByte |= (byte)(t << 6 - i % 4 * 2);

                    if (i % 4 == 3)
                    {
                        ctx.WriteByte(typeByte);
                        typeByte = 0;
                    }
                }

                // operands
                for (int i = 0; i < operands.Count; i++)
                {
                    if (tmpOperandTypes[i] == OPERAND_WORD)
                        ctx.WriteWord(tmpOperandValues[i]);
                    else
                        ctx.WriteByte((byte)tmpOperandValues[i]);
                }
            }

            if ((attr.Flags & ZOpFlags.Store) != 0)
            {
                if (store == null)
                {
                    // default to stack
                    ctx.WriteByte(0);
                }
                else
                {
                    byte type;
                    ushort value;
                    EvalOperand(ctx, store.GetChild(0), out type, out value);

                    if (type != OPERAND_VAR)
                        Errors.ThrowSerious(store, "expected local or global variable as store target");
                    ctx.WriteByte((byte)value);
                }
            }

            if ((attr.Flags & ZOpFlags.Branch) != 0)
            {
                if (branch == null)
                {
                    Errors.ThrowSerious(node, "expected branch target");
                }
                else
                {
                    bool polarity = (branch.Type == ZapParser.SLASH);
                    bool shortForm;
                    int offset;
                    branch = branch.GetChild(0);
                    switch (branch.Type)
                    {
                        case ZapParser.TRUE:
                            offset = 1;
                            shortForm = true;
                            break;
                        case ZapParser.FALSE:
                            offset = 0;
                            shortForm = true;
                            break;
                        case ZapParser.SYMBOL:
                            Symbol sym;
                            if (ctx.TryGetLocalSymbol(branch.Text, out sym) == false)
                            {
                                sym = new Symbol(branch.Text, SymbolType.Unknown, 0, ctx.CurrentPass);
                                ctx.SetLocalSymbol(branch.Text, sym);
                            }
                            offset = sym.Value;
                            shortForm = shortenBranch(ctx.Position, 1, n => (n >= 0 && n <= 63), sym);
                            break;
                        default:
                            throw new NotImplementedException();
                    }

                    if (offset < -8192 || offset > 8191)
                        Errors.Serious(ctx, node, "branch target is too far away");

                    if (!shortForm)
                        ctx.WriteWord((ushort)((polarity ? 0x8000 : 0) | (offset & 0x3fff)));
                    else
                        ctx.WriteByte((byte)((polarity ? 0xc0 : 0x40) | (offset & 0x3f)));
                }
            }
        }

        private static void HandleLabel(Context ctx, ITree node, Action<Symbol> recordLabel)
        {
            Symbol sym;

            if (node.Type == ZapParser.GLABEL)
            {
                if (ctx.Symbols.TryGetValue(node.Text, out sym) == true)
                {
                    if (sym.Pass == ctx.CurrentPass || sym.Type != SymbolType.GlobalLabel)
                        Errors.ThrowSerious(node, "redefining global label");

                    if (ctx.InVocab && !ctx.AtVocabRecord)
                        Errors.ThrowSerious(node, "unaligned global label in vocab section");

                    sym.SetValue(ctx.Position, ctx.CurrentPass);
                }
                else
                {
                    sym = new Symbol(node.Text, SymbolType.GlobalLabel, ctx.Position, ctx.CurrentPass);
                    ctx.Symbols.Add(node.Text, sym);
                }

                recordLabel(sym);
            }
            else if (node.Type == ZapParser.LLABEL)
            {
                if (!ctx.InLocalScope)
                    Errors.ThrowSerious(node, "local labels not allowed outside a function");

                if (ctx.TryGetLocalSymbol(node.Text, out sym) == false)
                {
                    sym = new Symbol(node.Text, SymbolType.LocalLabel, ctx.Position, ctx.CurrentPass);
                    ctx.SetLocalSymbol(node.Text, sym);
                }
                else if (sym.Type == SymbolType.LocalLabel && sym.Pass < ctx.CurrentPass)
                {
                    sym.SetValue(ctx.Position, ctx.CurrentPass);
                }
                else
                {
                    Errors.ThrowSerious(node, "redefining local label");
                }

                recordLabel(sym);
            }
        }
    }
}
