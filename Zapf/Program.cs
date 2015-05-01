/* Copyright 2010-2014 Jesse McGrew
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
using System.IO;
using Antlr.Runtime.Tree;

namespace Zapf
{
    class Program
    {
        // TODO: Blorb output

        public const string VERSION = "0.5";
        public const string BANNER = "ZAPF " + VERSION;
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
                Console.Error.WriteLine(BANNER);

            // TODO: move all of this logic into ZapfAssembler and use that instead

            // set up for the target version
            ctx.OpcodeDict = MakeOpcodeDict(ctx.ZVersion, ctx.InformMode);
            string stackName = ctx.InformMode ? "sp" : "STACK";
            ctx.GlobalSymbols.Add(stackName, new Symbol(stackName, SymbolType.Variable, 0));

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

                    var query = from s in ctx.GlobalSymbols.Values
                                where s.Type == SymbolType.Function || s.Type == SymbolType.Label ||
                                      s.Type == SymbolType.String
                                let addr = (s.Type == SymbolType.Label) ? s.Value : s.Value * ctx.PackingDivisor
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

        internal static Dictionary<string, KeyValuePair<ushort, ZOpAttribute>> MakeOpcodeDict(
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
                        result.GlobalSymbols["RELEASEID"] = new Symbol("RELEASEID", SymbolType.Constant, int.Parse(args[i]));
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

                    case "-dx":
                        result.XmlDebugMode = true;
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

            if (result.XmlDebugMode)
                result.DebugFile = Path.ChangeExtension(result.OutFile, ".dbg.xml");
            else
                result.DebugFile = Path.ChangeExtension(result.OutFile, ".dbg");

            return result;
        }

        private static void Usage()
        {
            Console.Error.WriteLine(BANNER);
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
  -ab                   also optimize abbreviations and print ZAPF code
  -dx                   use XML debug format");

        }

        internal static void Assemble(Context ctx)
        {
            // read in all source code
            List<AsmLine> file;
            ctx.PushFile(ctx.InFile);
            try
            {
                var roots = ReadRootsFromFile(ctx, ctx.InFile);
                file = new List<AsmLine>(ReadAllCode(ctx, roots));
            }
            catch (IOException ex)
            {
                Errors.ThrowFatal(ex.Message);
                return; // never gets here
            }

            // first pass: discover label addresses and header flags
            if (!ctx.Quiet)
                Console.Error.Write("Measuring");

            ctx.FinalPass = false;

            do
            {
                ctx.MeasureAgain = false;

                // write dummy header
                ctx.Position = 0;
                if (ctx.ZVersion < 5)
                    WriteHeader(ctx, false);

                for (int i = 0; i < file.Count; i++)
                    try
                    {
                        if (file[i] is EndDirective)
                            break;

                        PassOne(ctx, file[i], ref i);
                    }
                    catch (SeriousError ser)
                    {
                        ctx.HandleSeriousError(ser);
                    }

                ctx.CheckForUndefinedSymbols();

                if (ctx.Fixups.Count > 0)
                    ctx.MeasureAgain = true;

                ctx.ResetBetweenPasses();

                if (!ctx.Quiet && ctx.MeasureAgain && ctx.ErrorCount == 0)
                    Console.Error.Write('.');
            } while (ctx.MeasureAgain && ctx.ErrorCount == 0);

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
            if (ctx.ErrorCount > 0)
                return;

            // second pass: generate object code
            if (!ctx.Quiet)
                Console.Error.WriteLine("Assembling");

            ctx.FinalPass = true;

            for (int i = 0; i < file.Count; i++)
                try
                {
                    if (file[i] is EndDirective)
                        break;
                    else
                        PassTwo(ctx, file[i], ref i);
                }
                catch (SeriousError ser)
                {
                    ctx.HandleSeriousError(ser);
                }

            if (ctx.Fixups.Count > 0)
                Errors.Serious(ctx, "unresolved references after final pass");

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
        /// <param name="nodeIndex">The current node's index. The method may change this
        /// to rewind the source file.</param>
        private static void PassOne(Context ctx, AsmLine node, ref int nodeIndex)
        {
            if (node is NewDirective)
            {
                var versionExpr = ((NewDirective)node).Version;
                int version = versionExpr == null ? 4 : EvalExpr(ctx, versionExpr).Value;
                if (version < 3 || version > 8)
                    Errors.ThrowFatal("Only Z-machine versions 3-8 are supported");
                if (version != ctx.ZVersion)
                {
                    ctx.ZVersion = (byte)version;
                    ctx.OpcodeDict = MakeOpcodeDict(ctx.ZVersion, ctx.InformMode);
                    throw new RestartException();
                }
            }
            else if (node is TimeDirective)
            {
                if (ctx.ZVersion == 3)
                {
                    ctx.ZFlags |= 2;
                }
                else
                {
                    Errors.ThrowFatal(".TIME is only supported in Z-machine version 3");
                }
            }
            else if (node is SoundDirective)
            {
                if (ctx.ZVersion == 3)
                {
                    ctx.ZFlags2 |= 16;
                }
                else if (ctx.ZVersion == 4)
                {
                    ctx.ZFlags2 |= 128;
                }
                else
                {
                    Errors.ThrowFatal(".SOUND is only supported in Z-machine versions 3-4");
                }
            }
            else if (node is Instruction)
            {
                HandleInstruction(ctx, (Instruction)node);
            }
            else if (node is BareSymbolLine)
            {
                if (((BareSymbolLine)node).UsedAsInstruction)
                    Errors.Serious(ctx, node, "unrecognized opcode: " + ((BareSymbolLine)node).Text);
                else
                    HandleDirective(ctx, node, nodeIndex, false);
            }
            else if (node is LocalLabel || node is GlobalLabel)
            {
                HandleLabel(ctx, node, ref nodeIndex);
            }
            else
            {
                HandleDirective(ctx, node, nodeIndex, false);
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
            ctx.WriteWord(ctx.ZFlags2);
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
            if (ctx.GlobalSymbols.TryGetValue(name1, out sym) ||
                (name2 != null && ctx.GlobalSymbols.TryGetValue(name2, out sym)))
            {
                switch (sym.Type)
                {
                    case SymbolType.Label:
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
            if (ctx.GlobalSymbols.TryGetValue(name, out sym))
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
                        objTree.Value = objTable.Value + (ctx.ZVersion < 4 ? 31 : 63);
                        ctx.DebugFileMap[DEBF.ObjectsMapName] = objTree;
                    }
                }
                if (!ctx.DebugFileMap.ContainsKey(DEBF.PropsMapName))
                    ctx.DebugFileMap[DEBF.PropsMapName] = GetDebugMapValue(ctx, "OBJECT");
                if (!ctx.DebugFileMap.ContainsKey(DEBF.VocabMapName))
                    ctx.DebugFileMap[DEBF.VocabMapName] = GetDebugMapValue(ctx, "VOCAB");

                // write map
                ctx.DebugWriter.WriteMap(
                    ctx.DebugFileMap.Select(
                        p => new KeyValuePair<string, int>(p.Key, p.Value.Value)));

                // write header
                ctx.DebugWriter.WriteHeader(ctx.GetHeader());

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
        /// <param name="nodeIndex">The current node's index. The method may change this
        /// to rewind the source file.</param>
        private static void PassTwo(Context ctx, AsmLine node, ref int nodeIndex)
        {
            if (node is Instruction)
            {
                HandleInstruction(ctx, (Instruction)node);
            }
            else if (node is LocalLabel || node is GlobalLabel)
            {
                HandleLabel(ctx, node, ref nodeIndex);
            }
            else
            {
                HandleDirective(ctx, node, nodeIndex, true);
            }
        }

        private static IEnumerable<AsmLine> ReadAllCode(Context ctx, IEnumerable<AsmLine> roots)
        {
            foreach (var node in roots)
            {
                if (node is InsertDirective)
                {
                    string arg = ((InsertDirective)node).InsertFileName;
                    string insertedFile = ctx.FindInsertedFile(arg);

                    if (insertedFile == null)
                        Errors.ThrowFatal(node, "inserted file not found: " + arg, "root");

                    ctx.PushFile(insertedFile);
                    try
                    {
                        IEnumerable<AsmLine> insertedRoots = ReadRootsFromFile(ctx, insertedFile);
                        foreach (AsmLine insertedNode in ReadAllCode(ctx, insertedRoots))
                            if (insertedNode is EndiDirective || insertedNode is EndDirective)
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

        private static IEnumerable<AsmLine> ReadRootsFromFile(Context ctx, string path)
        {
            using (var stream = ctx.OpenFile(path, false))
            {
                var parser = new AntlrBasedParser(ctx.InformMode, ctx.OpcodeDict);
                var result = parser.Parse(stream, path);

                if (result.NumberOfSyntaxErrors > 0)
                    Errors.ThrowFatal("syntax error");

                return result.Lines;
            }
        }

        private static Symbol EvalExpr(Context ctx, AsmExpr node)
        {
            if (node is NumericLiteral)
            {
                return new Symbol(int.Parse(node.Text));
            }
            else if (node is SymbolExpr)
            {
                Symbol result;
                if (ctx.LocalSymbols.TryGetValue(node.Text, out result))
                    return result;
                if (ctx.GlobalSymbols.TryGetValue(node.Text, out result))
                    return result;
                if (ctx.FinalPass)
                    Errors.ThrowFatal(node, "undefined symbol: " + node.Text, "node");
                return new Symbol(null, SymbolType.Unknown, 0);
            }
            else if (node is AdditionExpr)
            {
                Symbol left = EvalExpr(ctx, ((AdditionExpr)node).Left);
                Symbol right = EvalExpr(ctx, ((AdditionExpr)node).Right);
                if (!ctx.FinalPass &&
                    (left.Type == SymbolType.Unknown || right.Type == SymbolType.Unknown))
                {
                    return new Symbol(null, SymbolType.Unknown, 0);
                }
                if (left.Type == SymbolType.Constant && right.Type == SymbolType.Constant)
                    return new Symbol(null, SymbolType.Constant, left.Value + right.Value);
                throw new NotImplementedException("Unimplemented symbol addition");
            }
            else
            {
                throw new NotImplementedException();
            }
        }

        private static void EvalOperand(Context ctx, AsmExpr node, out byte type, out ushort value, out Fixup fixup)
        {
            EvalOperand(ctx, node, out type, out value, out fixup, false);
        }

        private static void EvalOperand(Context ctx, AsmExpr node, out byte type, out ushort value, out Fixup fixup,
            bool allowLocalLabel)
        {
            fixup = null;

            bool apos = false;
            if (node is QuoteExpr)
            {
                apos = true;
                node = ((QuoteExpr)node).Inner;
            }

            if (node is NumericLiteral)
            {
                value = (ushort)int.Parse(node.Text);
                if (value < 256)
                    type = OPERAND_BYTE;
                else
                    type = OPERAND_WORD;
            }
            else if (node is SymbolExpr)
            {
                Symbol sym;
                if (ctx.LocalSymbols.TryGetValue(node.Text, out sym))
                {
                    if (sym.Type == SymbolType.Label)
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
                else if (ctx.GlobalSymbols.TryGetValue(node.Text, out sym))
                {
                    if (sym.Type == SymbolType.Variable)
                        type = OPERAND_VAR;
                    else if ((sym.Value & 0xff) == sym.Value)
                        type = OPERAND_BYTE;
                    else
                        type = OPERAND_WORD;
                    value = (ushort)sym.Value;
                }
                else if (ctx.FinalPass && !allowLocalLabel)
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
                    if (allowLocalLabel)
                        ctx.MarkUnknownBranch(node.Text);
                    else
                        fixup = new Fixup(node.Text);
                }
            }
            else
            {
                throw new NotImplementedException();
            }

            if (apos)
                type = OPERAND_BYTE;
        }

        private static bool IsLongConstant(Context ctx, AsmExpr node)
        {
            int value;

            if (node is NumericLiteral)
            {
                value = int.Parse(node.Text);
                return ((uint)value & 0xffffff00) != 0;
            }
            else if (node is SymbolExpr)
            {
                Symbol sym;
                if (ctx.LocalSymbols.TryGetValue(node.Text, out sym))
                {
                    // the only legal local symbol operand is a local variable
                    return false;
                }
                else if (ctx.GlobalSymbols.TryGetValue(node.Text, out sym))
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

            }
            else if (node is AdditionExpr)
            {
                return IsLongConstant(ctx, ((AdditionExpr)node).Left) && IsLongConstant(ctx, ((AdditionExpr)node).Right);
            }
            else if (node is StringLiteral || node is QuoteExpr)
            {
                return false;
            }
            else
            {
                throw new ArgumentException("Unexpected expr type: " + node.GetType().Name, "node");
            }
        }

        private static void HandleDirective(Context ctx, AsmLine node, int nodeIndex, bool assembling)
        {
            // local scope is terminated by any directive except .DEBUG_LINE (not counting labels)
            if (!(node is DebugLineDirective))
                ctx.EndReassemblyScope(nodeIndex);

            if (node is NewDirective || node is TimeDirective || node is SoundDirective)
            {
                // these are explicitly handled by PassOne
            }
            else if (node is FunctDirective)
            {
                BeginFunction(ctx, (FunctDirective)node, nodeIndex);
            }
            else if (node is TableDirective)
            {
                if (ctx.TableStart != null)
                    Errors.Warn(node, "starting new table before ending old table");
                ctx.TableStart = ctx.Position;
                ctx.TableSize = null;
                if (((TableDirective)node).Size != null)
                {
                    Symbol sym = EvalExpr(ctx, ((TableDirective)node).Size);
                    if (sym.Type != SymbolType.Constant)
                        Errors.Warn(node, "ignoring non-constant table size specifier");
                    else
                        ctx.TableSize = sym.Value;
                }
            }
            else if (node is EndtDirective)
            {
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
            }
            else if (node is VocbegDirective)
            {
                if (ctx.InVocab)
                {
                    Errors.Warn(node, "ignoring .VOCBEG inside another vocabulary block");
                }
                else
                {
                    Symbol sym1 = EvalExpr(ctx, ((VocbegDirective)node).RecordSize);
                    Symbol sym2 = EvalExpr(ctx, ((VocbegDirective)node).KeySize);
                    if (sym1.Type != SymbolType.Constant || sym2.Type != SymbolType.Constant)
                        Errors.Warn(node, "ignoring .VOCBEG with non-constant size specifiers");
                    else
                        ctx.EnterVocab(sym1.Value, sym2.Value);
                }
            }
            else if (node is VocendDirective)
            {
                if (!ctx.InVocab)
                    Errors.Warn(node, "ignoring .VOCEND outside of a vocabulary block");
                else
                    ctx.LeaveVocab();
            }
            else if (node is ByteDirective || node is WordDirective)
            {
                var elements = ((DataDirective)node).Elements;
                for (int i = 0; i < elements.Count; i++)
                {
                    Symbol sym = EvalExpr(ctx, elements[i]);
                    if (sym.Type == SymbolType.Unknown && ctx.FinalPass)
                    {
                        Errors.ThrowFatal(elements[i], "unrecognized symbol");
                    }
                    if (node is ByteDirective)
                    {
                        ctx.WriteByte((byte)sym.Value);
                    }
                    else
                    {
                        ctx.WriteWord((ushort)sym.Value);
                    }
                }
            }
            else if (node is FstrDirective)
            {
                AddAbbreviation(ctx, (FstrDirective)node);
            }
            else if (node is GstrDirective)
            {
                PackString(ctx, (GstrDirective)node);
            }
            else if (node is StrDirective)
            {
                ctx.WriteZString(((StrDirective)node).Text, false);
            }
            else if (node is StrlDirective)
            {
                ctx.WriteZString(((StrlDirective)node).Text, true);
            }
            else if (node is LenDirective)
            {
                ctx.WriteZStringLength(((LenDirective)node).Text);
            }
            else if (node is ZwordDirective)
            {
                ctx.WriteZWord(((ZwordDirective)node).Text);
            }
            else if (node is EqualsDirective)
            {
                Symbol rvalue = EvalExpr(ctx, ((EqualsDirective)node).Right);
                if (rvalue.Type == SymbolType.Unknown && ctx.FinalPass)
                {
                    Errors.ThrowFatal(((EqualsDirective)node).Right, "unrecognized symbol");
                }
                else
                {
                    ctx.GlobalSymbols[((EqualsDirective)node).Left] = rvalue;
                }
            }
            else if (node is GvarDirective)
            {
                ctx.AddGlobalVar(((GvarDirective)node).Name);
                if (((GvarDirective)node).InitialValue != null)
                {
                    Symbol sym = EvalExpr(ctx, ((GvarDirective)node).InitialValue);
                    if (sym.Type == SymbolType.Unknown && ctx.FinalPass)
                    {
                        Errors.ThrowFatal(((GvarDirective)node).InitialValue, "unrecognized symbol");
                    }
                    ctx.WriteWord((ushort)sym.Value);
                }
                else
                {
                    ctx.WriteWord(0);
                }
            }
            else if (node is ObjectDirective)
            {
                var od = (ObjectDirective)node;
                ctx.AddObject(od.Name);
                //XXX const string ObjectVersionError = "wrong .OBJECT syntax for this version";
                if (ctx.ZVersion < 4)
                {
                    // 2 flag words
                    ctx.WriteWord(EvalExpr(ctx, od.Flags1));
                    ctx.WriteWord(EvalExpr(ctx, od.Flags2));
                    // 3 object link bytes
                    ctx.WriteByte(EvalExpr(ctx, od.Parent));
                    ctx.WriteByte(EvalExpr(ctx, od.Sibling));
                    ctx.WriteByte(EvalExpr(ctx, od.Child));
                    // property table
                    ctx.WriteWord(EvalExpr(ctx, od.PropTable));
                }
                else
                {
                    // 3 flag words
                    ctx.WriteWord(EvalExpr(ctx, od.Flags1));
                    ctx.WriteWord(EvalExpr(ctx, od.Flags2));
                    ctx.WriteWord(EvalExpr(ctx, od.Flags3));
                    // 3 object link words
                    ctx.WriteWord(EvalExpr(ctx, od.Parent));
                    ctx.WriteWord(EvalExpr(ctx, od.Sibling));
                    ctx.WriteWord(EvalExpr(ctx, od.Child));
                    // property table
                    ctx.WriteWord(EvalExpr(ctx, od.PropTable));
                }
            }
            else if (node is PropDirective)
            {
                Symbol size = EvalExpr(ctx, ((PropDirective)node).Size);
                Symbol prop = EvalExpr(ctx, ((PropDirective)node).Prop);
                if (ctx.FinalPass &&
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
            }
            else if (node is DebugDirective)
            {
                if (assembling)
                {
                    if (!ctx.IsDebugFileOpen)
                        ctx.OpenDebugFile();

                    HandleDebugDirective(ctx, (DebugDirective)node);
                }
            }
            else
            {
                throw new NotImplementedException();
            }
        }

        private static void HandleDebugDirective(Context ctx, DebugDirective node)
        {
            if (ctx.DebugWriter.InRoutine &&
                !(node is DebugLineDirective || node is DebugRoutineEndDirective))
            {
                Errors.Serious(ctx, node, "debug directives other than .DEBUG-LINE not allowed inside routines");
                return;
            }

            Symbol sym1, sym2;
            if (node is DebugActionDirective)
            {
                ctx.DebugWriter.WriteAction(
                    (ushort)EvalExpr(ctx, ((DebugActionDirective)node).Number).Value,
                    ((DebugActionDirective)node).Name);
            }
            else if (node is DebugArrayDirective)
            {
                if (ctx.GlobalSymbols.TryGetValue("GLOBAL", out sym1) == false)
                {
                    Errors.Serious(ctx, node, "define GLOBAL before using .DEBUG-ARRAY");
                    return;
                }
                sym2 = EvalExpr(ctx, ((DebugArrayDirective)node).Number);
                ctx.DebugWriter.WriteArray(
                    (ushort)(sym2.Value - sym1.Value),
                    ((DebugArrayDirective)node).Name);
            }
            else if (node is DebugAttrDirective)
            {
                ctx.DebugWriter.WriteAttr(
                    (ushort)EvalExpr(ctx, ((DebugAttrDirective)node).Number).Value,
                    ((DebugAttrDirective)node).Name);
            }
            else if (node is DebugClassDirective)
            {
                ctx.DebugWriter.WriteClass(
                    ((DebugClassDirective)node).Name,
                    new LineRef(
                        (byte)EvalExpr(ctx, ((DebugClassDirective)node).StartFile).Value,
                        (ushort)EvalExpr(ctx, ((DebugClassDirective)node).StartLine).Value,
                        (byte)EvalExpr(ctx, ((DebugClassDirective)node).StartColumn).Value),
                    new LineRef(
                        (byte)EvalExpr(ctx, ((DebugClassDirective)node).EndFile).Value,
                        (ushort)EvalExpr(ctx, ((DebugClassDirective)node).EndLine).Value,
                        (byte)EvalExpr(ctx, ((DebugClassDirective)node).EndColumn).Value));
            }
            else if (node is DebugFakeActionDirective)
            {
                ctx.DebugWriter.WriteFakeAction(
                    (ushort)EvalExpr(ctx, ((DebugFakeActionDirective)node).Number).Value,
                    ((DebugFakeActionDirective)node).Name);
            }
            else if (node is DebugFileDirective)
            {
                ctx.DebugWriter.WriteFile(
                    (byte)EvalExpr(ctx, ((DebugFileDirective)node).Number).Value,
                    ((DebugFileDirective)node).IncludeName,
                    ((DebugFileDirective)node).ActualName);
            }
            else if (node is DebugGlobalDirective)
            {
                ctx.DebugWriter.WriteGlobal(
                    (byte)(EvalExpr(ctx, ((DebugGlobalDirective)node).Number).Value - 16),
                    ((DebugGlobalDirective)node).Name);
            }
            else if (node is DebugLineDirective)
            {
                if (!ctx.DebugWriter.InRoutine)
                {
                    Errors.Serious(ctx, node, ".DEBUG-LINE outside of .DEBUG-ROUTINE");
                }
                else
                {
                    ctx.DebugWriter.WriteLine(
                        new LineRef(
                            (byte)EvalExpr(ctx, ((DebugLineDirective)node).TheFile).Value,
                            (ushort)EvalExpr(ctx, ((DebugLineDirective)node).TheLine).Value,
                            (byte)EvalExpr(ctx, ((DebugLineDirective)node).TheColumn).Value),
                        ctx.Position);
                }
            }
            else if (node is DebugMapDirective)
            {
                ctx.DebugFileMap[((DebugMapDirective)node).Key] = EvalExpr(ctx, ((DebugMapDirective)node).Value);
            }
            else if (node is DebugObjectDirective)
            {
                ctx.DebugWriter.WriteObject(
                    (ushort)EvalExpr(ctx, ((DebugObjectDirective)node).Number).Value,
                    ((DebugObjectDirective)node).Name,
                    new LineRef(
                        (byte)EvalExpr(ctx, ((DebugObjectDirective)node).StartFile).Value,
                        (ushort)EvalExpr(ctx, ((DebugObjectDirective)node).StartLine).Value,
                        (byte)EvalExpr(ctx, ((DebugObjectDirective)node).StartColumn).Value),
                    new LineRef(
                        (byte)EvalExpr(ctx, ((DebugObjectDirective)node).EndFile).Value,
                        (ushort)EvalExpr(ctx, ((DebugObjectDirective)node).EndLine).Value,
                        (byte)EvalExpr(ctx, ((DebugObjectDirective)node).EndColumn).Value));
            }
            else if (node is DebugPropDirective)
            {
                ctx.DebugWriter.WriteProp(
                    (ushort)EvalExpr(ctx, ((DebugPropDirective)node).Number).Value,
                    ((DebugPropDirective)node).Name);
            }
            else if (node is DebugRoutineDirective)
            {
                AlignRoutine(ctx);
                ctx.DebugWriter.StartRoutine(
                    new LineRef(
                            (byte)EvalExpr(ctx, ((DebugRoutineDirective)node).TheFile).Value,
                            (ushort)EvalExpr(ctx, ((DebugRoutineDirective)node).TheLine).Value,
                            (byte)EvalExpr(ctx, ((DebugRoutineDirective)node).TheColumn).Value),
                    ctx.Position,
                    ((DebugRoutineDirective)node).Name,
                    ((DebugRoutineDirective)node).Locals);
            }
            else if (node is DebugRoutineEndDirective)
            {
                if (!ctx.DebugWriter.InRoutine)
                {
                    Errors.Serious(ctx, node, ".DEBUG-ROUTINE-END outside of .DEBUG-ROUTINE");
                }
                else
                {
                    ctx.DebugWriter.EndRoutine(
                        new LineRef(
                            (byte)EvalExpr(ctx, ((DebugRoutineEndDirective)node).TheFile).Value,
                            (ushort)EvalExpr(ctx, ((DebugRoutineEndDirective)node).TheLine).Value,
                            (byte)EvalExpr(ctx, ((DebugRoutineEndDirective)node).TheColumn).Value),
                        ctx.Position);
                }
            }
        }

        private static void BeginFunction(Context ctx, FunctDirective node, int nodeIndex)
        {
            List<string> localNames = new List<string>();
            List<ushort> localValues = new List<ushort>();
            bool gotDefaultValues = false;

            string name = node.Name;
            foreach (var local in node.Locals)
            {
                if (ctx.LocalSymbols.ContainsKey(local.Name))
                {
                    Errors.ThrowSerious(node, "duplicate local: {0}", local.Name);
                }

                localNames.Add(local.Name);
                ctx.LocalSymbols.Add(local.Name,
                    new Symbol(local.Name, SymbolType.Variable, localNames.Count));

                if (local.DefaultValue != null)
                {
                    gotDefaultValues = true;

                    Symbol defv = EvalExpr(ctx, local.DefaultValue);
                    localValues.Add((ushort)defv.Value);
                }
                else
                {
                    localValues.Add(0);
                }
            }

            if (localNames.Count > 15)
                Errors.ThrowSerious(node, "too many local variables");

            AlignRoutine(ctx);

            Symbol sym;
            int paddr = ctx.Position / ctx.PackingDivisor;
            if (ctx.GlobalSymbols.TryGetValue(name, out sym) == false)
            {
                sym = new Symbol(name, SymbolType.Function, paddr);
                ctx.GlobalSymbols.Add(name, sym);
            }
            else if (!sym.Phantom || sym.Type != SymbolType.Function)
            {
                Errors.ThrowSerious("function redefined: " + name);
            }
            else
            {
                sym.Phantom = false;
                if (sym.Value != paddr)
                {
                    sym.Value = paddr;
                    ctx.MeasureAgain = true;
                }
            }

            ctx.BeginReassemblyScope(nodeIndex, sym);

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

        private static void PackString(Context ctx, GstrDirective node)
        {
            string name = node.Name;

            AlignString(ctx);

            Symbol sym;
            int paddr = ctx.Position / ctx.PackingDivisor;
            if (ctx.GlobalSymbols.TryGetValue(name, out sym) == false)
            {
                sym = new Symbol(name, SymbolType.String, paddr);
                ctx.GlobalSymbols.Add(name, sym);
            }
            else if (!sym.Phantom || sym.Type != SymbolType.String)
            {
                Errors.ThrowSerious("string redefined: " + name);
            }
            else
            {
                sym.Phantom = false;
                if (sym.Value != paddr)
                {
                    sym.Value = paddr;
                    ctx.MeasureAgain = true;
                }
            }

            ctx.WriteZString(node.Text, false);
        }

        private static void AlignString(Context ctx)
        {
            while (ctx.Position % ctx.PackingDivisor != 0)
                ctx.WriteByte(0);
        }

        private static void AddAbbreviation(Context ctx, FstrDirective node)
        {
            if (ctx.StringEncoder.AbbrevsFrozen)
                Errors.ThrowSerious(node, "abbreviations must be defined before strings");

            string name = node.Name;

            if (ctx.Position % 2 != 0)
                ctx.WriteByte(0);

            ctx.GlobalSymbols[name] = new Symbol(name, SymbolType.Constant, ctx.Position / 2);

            string text = node.Text;
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
        private static Fixup[] tmpOperandFixups = new Fixup[8];

        private static void HandleInstruction(Context ctx, Instruction node)
        {
            var pair = ctx.OpcodeDict[node.Name];
            ushort opcode = pair.Key;
            ZOpAttribute attr = pair.Value;

            bool usesLongConstants = node.Operands.Any(o => IsLongConstant(ctx, o));

            // force a 2OP instruction to EXT mode if it uses long constants
            // or has more than 2 operands
            if (opcode < 128 && (usesLongConstants || node.Operands.Count > 2))
                opcode += 192;

            if (opcode < 128)
            {
                // 2OP
                if (node.Operands.Count != 2)
                    Errors.ThrowSerious(node, "expected 2 operands");

                byte b = (byte)opcode;
                EvalOperand(ctx, node.Operands[0], out tmpOperandTypes[0], out tmpOperandValues[0], out tmpOperandFixups[0]);
                EvalOperand(ctx, node.Operands[1], out tmpOperandTypes[1], out tmpOperandValues[1], out tmpOperandFixups[1]);
                System.Diagnostics.Debug.Assert(tmpOperandFixups[0] == null);
                System.Diagnostics.Debug.Assert(tmpOperandFixups[1] == null);
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
                if (node.Operands.Count != 1)
                    Errors.ThrowSerious(node, "expected 1 operand");

                byte b = (byte)opcode;
                EvalOperand(ctx, node.Operands[0], out tmpOperandTypes[0], out tmpOperandValues[0], out tmpOperandFixups[0],
                    (attr.Flags & ZOpFlags.Label) != 0);

                if ((attr.Flags & ZOpFlags.Label) != 0)
                {
                    // correct label offset (-3 for the opcode and operand, +2 for the normal jump bias)
                    tmpOperandValues[0]--;
                }

                b |= (byte)(tmpOperandTypes[0] << 4);

                ctx.WriteByte(b);
                if (tmpOperandFixups[0] != null)
                {
                    tmpOperandFixups[0].Location = ctx.Position;
                    ctx.Fixups.Add(tmpOperandFixups[0]);
                    tmpOperandFixups[0] = null;
                }
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
                    if (node.Operands.Count != 0)
                        Errors.ThrowSerious(node, "expected 0 operands");

                    ctx.WriteByte((byte)opcode);
                }
                else
                {
                    if (node.Operands.Count != 1)
                        Errors.ThrowSerious(node, "expected literal string as only operand");

                    ctx.WriteByte((byte)opcode);
                    ctx.WriteZString(node.Operands[0].Text, false);
                }
            }
            else
            {
                // EXT
                int maxArgs = ((attr.Flags & ZOpFlags.Extra) == 0) ? 4 : 8;

                if (node.Operands.Count > maxArgs)
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
                    if (i < node.Operands.Count)
                    {
                        EvalOperand(ctx, node.Operands[i], out t, out tmpOperandValues[i], out tmpOperandFixups[i]);
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
                for (int i = 0; i < node.Operands.Count; i++)
                {
                    if (tmpOperandFixups[i] != null)
                    {
                        tmpOperandFixups[i].Location = ctx.Position;
                        ctx.Fixups.Add(tmpOperandFixups[i]);
                        tmpOperandFixups[i] = null;
                    }

                    if (tmpOperandTypes[i] == OPERAND_WORD)
                        ctx.WriteWord(tmpOperandValues[i]);
                    else
                        ctx.WriteByte((byte)tmpOperandValues[i]);
                }
            }

            if ((attr.Flags & ZOpFlags.Store) != 0)
            {
                if (node.StoreTarget == null)
                {
                    // default to stack
                    ctx.WriteByte(0);
                }
                else
                {
                    Symbol sym;
                    if ((ctx.LocalSymbols.TryGetValue(node.StoreTarget, out sym) == false &&
                         ctx.GlobalSymbols.TryGetValue(node.StoreTarget, out sym) == false) ||
                        sym.Type != SymbolType.Variable)
                    {
                        Errors.ThrowSerious(node, "expected local or global variable as store target");
                    }

                    ctx.WriteByte((byte)sym.Value);
                }
            }

            if ((attr.Flags & ZOpFlags.Branch) != 0)
            {
                if (node.BranchTarget == null)
                {
                    Errors.ThrowSerious(node, "expected branch target");
                }
                else
                {
                    bool polarity = (bool)node.BranchPolarity, far = false;
                    int offset;
                    switch (node.BranchTarget)
                    {
                        case Instruction.BranchTrue:
                            offset = 1;
                            break;
                        case Instruction.BranchFalse:
                            offset = 0;
                            break;
                        default:
                            Symbol sym;
                            if (ctx.LocalSymbols.TryGetValue(node.BranchTarget, out sym))
                            {
                                offset = sym.Value - (ctx.Position + 1) + 2;
                                if (offset < 2 || offset > 63)
                                {
                                    far = true;
                                    offset--;
                                }
                            }
                            else
                            {
                                // not yet defined (we'll reassemble this later once it's defined)
                                offset = 2;
                                ctx.MarkUnknownBranch(node.BranchTarget);
                            }
                            break;
                    }

                    if (offset < -8192 || offset > 8191)
                        Errors.Serious(ctx, node, "branch target is too far away");

                    if (far)
                        ctx.WriteWord((ushort)((polarity ? 0x8000 : 0) | (offset & 0x3fff)));
                    else
                        ctx.WriteByte((byte)((polarity ? 0xc0 : 0x40) | (offset & 0x3f)));
                }
            }
        }

        private static void HandleLabel(Context ctx, AsmLine node, ref int nodeIndex)
        {
            if (node is GlobalLabel)
            {
                string name = ((GlobalLabel)node).Name;

                Symbol sym;
                if (ctx.GlobalSymbols.TryGetValue(name, out sym) == true)
                {
                    // we don't require it to be a phantom because a global label might be
                    // defined inside a routine, and reassembly could cause it to be defined twice
                    if (sym.Type != SymbolType.Label /*|| !sym.Phantom*/)
                        Errors.ThrowSerious(node, "redefining global label");

                    if (ctx.InVocab)
                    {
                        if (!ctx.AtVocabRecord)
                            Errors.ThrowSerious(node, "unaligned global label in vocab section");

                        sym.Value = ctx.Position;
                    }
                    else if (sym.Value != ctx.Position)
                    {
                        if (ctx.FinalPass)
                            Errors.ThrowFatal(node, "global label {0} seems to have moved: was {1}, now {2}",
                                name, sym.Value, ctx.Position);

                        ctx.MeasureAgain = true;
                        sym.Value = ctx.Position;
                    }

                    sym.Phantom = false;
                }
                else
                {
                    ctx.GlobalSymbols.Add(name, new Symbol(name, SymbolType.Label, ctx.Position));
                }
            }
            else
            {
                System.Diagnostics.Debug.Assert(node is LocalLabel);

                string name = ((LocalLabel)node).Name;

                if (!ctx.InReassemblyScope)
                    Errors.ThrowSerious(node, "local labels not allowed outside a function");

                Symbol sym;
                if (ctx.LocalSymbols.TryGetValue(name, out sym) == false)
                {
                    if (ctx.CausesReassembly(name))
                        nodeIndex = ctx.Reassemble(name) - 1;
                    else
                        ctx.LocalSymbols.Add(name, new Symbol(name, SymbolType.Label, ctx.Position));
                }
                else if (sym.Type == SymbolType.Label && sym.Phantom)
                {
                    if (sym.Value != ctx.Position)
                        nodeIndex = ctx.Reassemble(name) - 1;
                    else
                        sym.Phantom = false;
                }
                else
                {
                    Errors.ThrowSerious(node, "redefining local label");
                }
            }
        }
    }
}
