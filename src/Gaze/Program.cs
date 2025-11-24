/* Copyright 2010-2023 Tara McGrew
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
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using Gaze.Parsing;
using Zilf.Common.StringEncoding;
using Gaze.Parsing.Diagnostics;
using Gaze.Parsing.Directives;
using Gaze.Parsing.Expressions;
using Gaze.Parsing.Instructions;
using System.Diagnostics.CodeAnalysis;
using System.CommandLine;
using System.CommandLine.Parsing;
using CommandParseResult = System.CommandLine.ParseResult;

namespace Gaze
{
    public static class Program
    {
        // TODO: Blorb output

        static readonly Lazy<GazeCommandSpec> CommandSpec = new(CreateCommandSpec);

        public static int Main(string[] args)
        {
            var normalizedArgs = NormalizeArgs(args);
            var spec = CommandSpec.Value;
            var parseResult = spec.RootCommand.Parse(normalizedArgs);
            return parseResult.Invoke();
        }

        static GazeCommandSpec CreateCommandSpec()
        {
            var inputArgument = CreateInputArgument();
            var outputArgument = CreateOutputArgument();
            var abbreviateOption = CreateBoolOption("--abbreviate", "-A",
                "Also optimize abbreviations and print GAZE code.");
            var quietOption = CreateBoolOption("--quiet", "-q",
                "Quiet mode (no banner).");
            var listAddressesOption = CreateBoolOption("--list-addresses", "-L",
                "List global label addresses.");
            var releaseOption = CreateOption<short?>("--release", "-r",
                "Set release number.");
            var serialOption = CreateOption<string?>("--serial", "-s",
                "Set serial number.");
            var creatorOption = CreateOption<string?>("--creator", "-C",
                "Set creator version.");
            var clearCreatorOption = CreateBoolOption("--no-creator", "-N",
                "Clear creator version metadata.");

            var root = new RootCommand("Assemble ZAP source files into Glulx story files.")
            {
                TreatUnmatchedTokensAsErrors = true
            };

            root.Arguments.Add(inputArgument);
            root.Arguments.Add(outputArgument);
            root.Options.Add(abbreviateOption);
            root.Options.Add(quietOption);
            root.Options.Add(listAddressesOption);
            root.Options.Add(releaseOption);
            root.Options.Add(serialOption);
            root.Options.Add(creatorOption);
            root.Options.Add(clearCreatorOption);

            var spec = new GazeCommandSpec(
                root,
                inputArgument,
                outputArgument,
                abbreviateOption,
                quietOption,
                listAddressesOption,
                releaseOption,
                serialOption,
                creatorOption,
                clearCreatorOption);

            root.Validators.Add(commandResult =>
            {
                if (commandResult.GetResult(spec.CreatorOption) is not null &&
                    commandResult.GetResult(spec.ClearCreatorOption) is not null &&
                    commandResult.GetValue(spec.ClearCreatorOption))
                {
                    commandResult.AddError("Options -C/--creator and -N/--no-creator cannot be used together.");
                }
            });

            root.SetAction(parseResult =>
            {
                using var ctx = new Context();

                if (!TryPopulateContext(ctx, parseResult))
                {
                    return 1;
                }

                return RunAssembler(ctx);
            });

            return spec;
        }

        static Argument<string> CreateInputArgument()
        {
            return new Argument<string>("input")
            {
                Description = "Input ZAP source file.",
                HelpName = "input.zap"
            };
        }

        static Argument<string?> CreateOutputArgument()
        {
            return new Argument<string?>("output")
            {
                Description = "Optional output file path. Defaults to the input name with a .z# extension.",
                Arity = ArgumentArity.ZeroOrOne,
                HelpName = "output"
            };
        }

        static Option<bool> CreateBoolOption(string name, string alias, string description)
        {
            var option = new Option<bool>(name)
            {
                Description = description
            };
            option.Aliases.Add(alias);
            return option;
        }

        static Option<T> CreateOption<T>(string name, string alias, string description)
        {
            var option = new Option<T>(name)
            {
                Description = description
            };
            option.Aliases.Add(alias);
            option.Arity = ArgumentArity.ZeroOrOne;
            option.HelpName = "value";
            return option;
        }

        private sealed record class GazeCommandSpec(
            RootCommand RootCommand,
            Argument<string> InputArgument,
            Argument<string?> OutputArgument,
            Option<bool> AbbreviateOption,
            Option<bool> QuietOption,
            Option<bool> ListAddressesOption,
            Option<short?> ReleaseOption,
            Option<string?> SerialOption,
            Option<string?> CreatorOption,
            Option<bool> ClearCreatorOption);

        static string[] NormalizeArgs(IReadOnlyList<string> args)
        {
            var hasLegacyHelp = false;

            for (int i = 0; i < args.Count; i++)
            {
                if (args[i] is "-?" or "/?")
                {
                    hasLegacyHelp = true;
                    break;
                }
            }

            if (!hasLegacyHelp)
            {
                if (args is string[] array)
                    return array;

                return args.ToArray();
            }

            var normalized = new string[args.Count];
            for (int i = 0; i < args.Count; i++)
            {
                normalized[i] = args[i] is "-?" or "/?" ? "--help" : args[i];
            }

            return normalized;
        }

        static int RunAssembler(Context ctx)
        {
            // show banner
            if (!ctx.Quiet)
                Console.Error.WriteLine(GetBanner());

            // TODO: move all of this logic into GazeAssembler and use that instead

            // set up for the target version
            ctx.OpcodeDict = MakeOpcodeDict();
            ctx.Restart();

            try
            {
                // assemble the code
                try
                {
                    Assemble(ctx);
                }
                catch (FatalError fer)
                {
                    ctx.HandleFatalError(fer);
                    return 2;
                }

                // list label addresses
                PrintLabelAddresses(ctx);

                // find abbreviations
                FindAndPrintAbbreviations(ctx);

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

        internal static string GetVersion() =>
            typeof(Program).Assembly.GetCustomAttribute<AssemblyInformationalVersionAttribute>()
                ?.InformationalVersion ?? "<?.??>";

        internal static string GetBanner() => $"GAZE {GetVersion()}";

        static void FindAndPrintAbbreviations(Context ctx)
        {
            if (ctx.AbbreviateMode)
            {
                const int maxAbbrevs = 96;
                Console.Error.WriteLine("Finding up to {0} abbreviations...", maxAbbrevs);

                Console.WriteLine("        ; Frequent words file for {0}", Path.GetFileName(ctx.InFile));
                Console.WriteLine();
                int num = 1, totalSavings = 0;
                foreach (var r in ctx.AbbrevFinder.GetResults(maxAbbrevs))
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
                for (int i = num; i <= maxAbbrevs; i++)
                    Console.WriteLine("        FSTR?DUMMY");

                Console.WriteLine();
                Console.WriteLine("        .ENDI");

                Console.Error.WriteLine("Abbrevs would save {0} z-chars total (~{1} bytes)",
                    totalSavings, totalSavings * 2 / 3);
            }
        }

        static void PrintLabelAddresses(Context ctx)
        {
            if (ctx.ListAddresses)
            {
                Console.Error.WriteLine();

                var query = from s in ctx.GlobalSymbols.Values
                            let addr = s.Type switch
                            {
                                SymbolType.Label or SymbolType.Function or SymbolType.String => s.Value,
                                _ => (int?)null
                            }
                            where addr != null
                            orderby addr.Value
                            select new { s.Name, Address = addr.Value };

                Console.WriteLine("{0,-32} {1,-6} {2,-6}",
                    "Name",
                    "Addr",
                    "Length");

                // Remove accidental duplicate symbols (same name and address) that can occur
                // when inputs define the same symbol more than once across segments.
                var entries = query
                    .DistinctBy(e => (e.Name, e.Address))
                    .ToArray();

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
        }

        static string SanitizeString(string text)
        {
            var sb = new StringBuilder(text);

            for (int i = sb.Length - 1; i >= 0; i--)
                if (sb[i] == '"')
                    sb.Insert(i, '"');

            return sb.ToString();
        }

        internal static Dictionary<string, (int opcode, GOpAttribute attr)> MakeOpcodeDict()
        {
            var fields = typeof(Opcodes).GetFields(BindingFlags.Static | BindingFlags.Public);
            var result = new Dictionary<string, (int opcode, GOpAttribute attr)>(fields.Length);

            foreach (var fi in fields)
            {
                var attrs = fi.GetCustomAttributes(typeof(GOpAttribute), false);
                foreach (var attr in attrs.Cast<GOpAttribute>())
                {
                    result.Add(fi.Name, ((int)fi.GetValue(null)!, attr));
                    break;
                }
            }

            return result;
        }

        internal static bool TryParseArgs(IReadOnlyList<string> args, [NotNullWhen(true)] out Context? ctx)
        {
            var result = new Context();

            if (TryParseArgs(args, result))
            {
                ctx = result;
                return true;
            }

            result.Dispose();
            ctx = null;
            return false;
        }

        internal static bool TryParseArgs(IReadOnlyList<string> args, Context ctx)
        {
            var normalized = NormalizeArgs(args);

            if (normalized.Any(arg => arg is "--help" or "-h"))
                return false;

            var parseResult = CommandSpec.Value.RootCommand.Parse(normalized);

            if (parseResult.Errors.Count > 0)
                return false;

            return TryPopulateContext(ctx, parseResult);
        }

        static bool TryPopulateContext(Context ctx, CommandParseResult parseResult)
        {
            if (parseResult.Errors.Count > 0)
                return false;

            var spec = CommandSpec.Value;

            var input = parseResult.GetValue(spec.InputArgument);
            if (string.IsNullOrWhiteSpace(input))
                return false;

            ctx.InFile = input;

            var output = parseResult.GetValue(spec.OutputArgument);
            ctx.OutFile = string.IsNullOrEmpty(output)
                ? Path.ChangeExtension(ctx.InFile, ".z#")
                : output;

            if (parseResult.GetResult(spec.AbbreviateOption) is not null)
                ctx.AbbreviateMode = parseResult.GetValue(spec.AbbreviateOption);

            if (parseResult.GetResult(spec.QuietOption) is not null)
                ctx.Quiet = parseResult.GetValue(spec.QuietOption);

            if (parseResult.GetResult(spec.ListAddressesOption) is not null)
                ctx.ListAddresses = parseResult.GetValue(spec.ListAddressesOption);

            if (parseResult.GetResult(spec.ReleaseOption) is not null)
                ctx.Release = parseResult.GetValue(spec.ReleaseOption);

            if (parseResult.GetResult(spec.SerialOption) is not null)
                ctx.Serial = parseResult.GetValue(spec.SerialOption);

            if (parseResult.GetResult(spec.ClearCreatorOption) is not null &&
                parseResult.GetValue(spec.ClearCreatorOption))
            {
                ctx.Creator = null;
            }
            else if (parseResult.GetResult(spec.CreatorOption) is not null)
            {
                ctx.Creator = parseResult.GetValue(spec.CreatorOption);
            }

            ctx.DebugFile = Path.ChangeExtension(ctx.OutFile, ".dbg.xml");

            return true;
        }

        /// <exception cref="FatalError">An <see cref="IOException"/> occurred while reading the input file(s).</exception>
        internal static void Assemble(Context ctx)
        {
            List<AsmLine> file;
            Debug.Assert(ctx.InFile != null);
            ctx.PushFile(ctx.InFile);
            try
            {
                var roots = ReadRootsFromFile(ctx, ctx.InFile);
                file = new List<AsmLine>(ReadAllCode(ctx, roots));
            }
            catch (IOException ex)
            {
                Errors.ThrowFatal(ex.Message);
                // ReSharper disable once HeuristicUnreachableCode
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
                WriteHeader(ctx, false);

                for (int i = 0; i < file.Count; i++)
                {
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
                }

                ctx.CheckForUndefinedSymbols();

                if (ctx.Fixups.Count > 0)
                    ctx.MeasureAgain = true;

                // ctx.CheckLimits();
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
                try
                {
                    WriteHeader(ctx, true);
                }
                catch (SeriousError ser)
                {
                    ctx.HandleSeriousError(ser);
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
            {
                try
                {
                    if (file[i] is EndDirective)
                        break;

                    PassTwo(ctx, file[i], ref i);
                }
                catch (SeriousError ser)
                {
                    ctx.HandleSeriousError(ser);
                }
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
        /// </remarks>
        /// <param name="ctx">The current context.</param>
        /// <param name="node">The node to process.</param>
        /// <param name="nodeIndex">The current node's index. The method may change this
        /// to rewind the source file.</param>
        static void PassOne(Context ctx, AsmLine node, ref int nodeIndex)
        {
            switch (node)
            {
                case Instruction inst:
                    HandleInstruction(ctx, inst);
                    break;

                case BareSymbolLine bsl:
                    if (bsl.UsedAsInstruction)
                        Errors.Serious(ctx, node, "unrecognized opcode: " + bsl.Text);
                    else
                        HandleDirective(ctx, node, nodeIndex, false);
                    break;

                case LocalLabel:
                case GlobalLabel:
                    HandleLabel(ctx, node, ref nodeIndex);
                    break;

                default:
                    HandleDirective(ctx, node, nodeIndex, false);
                    break;
            }
        }

        static void WriteHeader(Context ctx, bool strict)
        {
            int stackRequired = 256;    //XXX
            int decodingTable = 0;      //XXX

            // Glulx header
            ctx.WriteWord(('G' << 24) + ('l' << 16) + ('u' << 8) + 'l');
            ctx.WriteWord(0x00030103);  // Glulx spec version 3.1.3
            ctx.WriteWord(ctx.GetHeaderValue("RAMSTART", strict));
            ctx.WriteWord(0);           // EXTSTART placeholder
            ctx.WriteWord(0);           // ENDMEM placeholder
            ctx.WriteWord(stackRequired);
            ctx.WriteWord(ctx.GetHeaderValue("GO", strict));
            ctx.WriteWord(decodingTable);
            ctx.WriteWord(0);           // checksum placeholder

            // Inform-style header extension
            ctx.WriteWord(0);           // creator placeholder
            ctx.WriteWord(0x0100);
            ctx.WriteWord(('0' << 24) + ('.' << 16) + ('0' << 8) + '0'); //XXX compiler frontend version
            ctx.WriteWord(('0' << 24) + ('.' << 16) + ('0' << 8) + '0'); //XXX compiler backend version
            ctx.WriteShort((ushort)ctx.GetHeaderValue("RELEASEID", "ZORKID", false));
            for (int i = 0; i < 6; i++) // serial number placeholder
                ctx.WriteByte(0);
        }

        static Symbol? GetDebugMapValue(Context ctx, string name)
        {
            ctx.GlobalSymbols.TryGetValue(name, out var sym);
            return sym;
        }

        static void FinalizeOutput(Context ctx)
        {
            const int MINSIZE = 512;

            // pad file to a minimum size (for the benefit of tools that assume one)
            while (ctx.Position < MINSIZE)
                ctx.WriteByte(0);

            // pad file to a multiple of 4 bytes
            while ((ctx.Position & 3) != 0)
                ctx.WriteByte(0);

            // calculate file length
            int length = ctx.Position;

            // calculate file checksum
            ctx.Position = 64;
            int checksum = 0;

            while (ctx.Position < length)
                checksum += ctx.ReadWord();

            // write release number into header (overriding RELEASEID if set)
            if (ctx.Release != null)
            {
                ctx.Position = 56;
                ctx.WriteShort((ushort)ctx.Release);
            }

            // write serial number into header
            if (ctx.Serial == null)
                ctx.Serial = DateTime.Now.ToString("yyMMdd");
            else if (ctx.Serial.Length != 6)
                ctx.Serial = ctx.Serial.PadRight(6)[..6];

            ctx.Position = 54;
            foreach (char c in ctx.Serial)
                ctx.WriteByte((byte)c);

            // write length and checksum into header
            ctx.Position = 12;
            ctx.WriteWord(length);      // EXTSTART
            ctx.WriteWord(length);      // ENDMEM
            ctx.Position = 32;
            ctx.WriteWord(checksum);

            // write creator ID
            if (ctx.Creator != null)
            {
                if (ctx.Creator.Length != 4)
                    ctx.Creator = ctx.Creator.PadRight(4)[..4];

                ctx.Position = 36;
                foreach (char c in ctx.Creator)
                    ctx.WriteByte((byte)c);
            }

            // done
            if (!ctx.Quiet)
                Console.Error.WriteLine("Wrote {0} bytes to {1}", length, ctx.OutFile);

            // finalize debug file
            if (ctx.IsDebugFileOpen)
            {
                Debug.Assert(ctx.DebugWriter != null);

                // finish map
                // void CopyDebugMapValue(string debfName, string symbolName)
                // {
                //     if (!ctx.DebugFileMap.ContainsKey(debfName) && GetDebugMapValue(ctx, symbolName) is Symbol sym)
                //         ctx.DebugFileMap[debfName] = sym;
                // }

                // CopyDebugMapValue(DEBF.AbbrevMapName, "WORDS");
                // CopyDebugMapValue(DEBF.GlobalsMapName, "GLOBAL");
                // CopyDebugMapValue(DEBF.PropsMapName, "OBJECT");
                // CopyDebugMapValue(DEBF.VocabMapName, "VOCAB");

                // if (!ctx.DebugFileMap.ContainsKey(DEBF.ObjectsMapName))
                // {
                //     var objTable = GetDebugMapValue(ctx, "OBJECT");
                //     if (objTable != null)
                //     {
                //         ctx.DebugFileMap[DEBF.ObjectsMapName] = new Symbol
                //         {
                //             Type = objTable.Type,
                //             Value = objTable.Value + (ctx.ZVersion < 4 ? 31 : 63)
                //         };
                //     }
                // }

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
        static void PassTwo(Context ctx, AsmLine node, ref int nodeIndex)
        {
            switch (node)
            {
                case Instruction inst:
                    HandleInstruction(ctx, inst);
                    break;

                case LocalLabel:
                case GlobalLabel:
                    HandleLabel(ctx, node, ref nodeIndex);
                    break;

                default:
                    HandleDirective(ctx, node, nodeIndex, true);
                    break;
            }
        }

        static IEnumerable<AsmLine> ReadAllCode(Context ctx, IEnumerable<AsmLine> roots)
        {
            foreach (var node in roots)
            {
                if (node is InsertDirective insert)
                {
                    string arg = insert.InsertFileName;
                    var insertedFile = ctx.FindInsertedFile(arg);

                    if (insertedFile == null)
                        Errors.ThrowFatal(node, "inserted file not found: {0}", arg);

                    ctx.PushFile(insertedFile);
                    try
                    {
                        var insertedRoots = ReadRootsFromFile(ctx, insertedFile);
                        foreach (var insertedNode in ReadAllCode(ctx, insertedRoots))
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

        static IEnumerable<AsmLine> ReadRootsFromFile(Context ctx, string path)
        {
            using var stream = ctx.FileSystem.OpenForReading(path);

            Debug.Assert(ctx.OpcodeDict != null);
            var parser = new GazeParser(ctx, ctx.OpcodeDict);
            var result = parser.Parse(stream, path);

            if (result.NumberOfSyntaxErrors > 0)
                Errors.ThrowFatal("syntax error");

            return result.Lines;
        }

        static Symbol EvalExpr(Context ctx, AsmExpr node)
        {
            switch (node)
            {
                case NumericLiteral num:
                    return new Symbol(num.Value);

                case SymbolExpr sym:
                    if (!ctx.LocalSymbols.TryGetValue(sym.Text, out var result) &&
                        !ctx.GlobalSymbols.TryGetValue(sym.Text, out result))
                    {
                        if (ctx.FinalPass)
                        {
                            Errors.ThrowFatal(node, "undefined symbol: {0}", sym.Text);
                        }
                        else
                        {
                            result = new Symbol(sym.Text, SymbolType.Unknown, 0);
                            ctx.GlobalSymbols.Add(sym.Text, result);
                        }
                    }

                    return result;

                case AdditionExpr add:
                    var left = EvalExpr(ctx, add.Left);
                    var right = EvalExpr(ctx, add.Right);
                    if (!ctx.FinalPass &&
                        (left.Type == SymbolType.Unknown || right.Type == SymbolType.Unknown))
                    {
                        return new Symbol(null, SymbolType.Unknown, 0);
                    }
                    if (Addable(left.Type) && Addable(right.Type))
                        return new Symbol(null, SymbolType.Constant, left.Value + right.Value);
                    throw new NotImplementedException($"Unimplemented symbol addition: {left.Type} + {right.Type}");

                    // we can add numbers and addresses
                    static bool Addable(SymbolType type) => type switch
                    {
                        SymbolType.Constant or SymbolType.Label or SymbolType.Function or SymbolType.String => true,
                        _ => false,
                    };

                default:
                    throw new NotImplementedException();
            }
        }

        static void EvalOperand(Context ctx, AsmExpr node, out byte type, out int value, out Fixup? fixup)
        {
            fixup = null;

            bool apos = false;
            if (node is QuoteExpr quote)
            {
                apos = true;
                node = quote.Inner;
            }

            // Handle branch offset expressions (forward slash prefix)
            if (node is BranchOffsetExpr branchOffset)
            {
                // Branch offsets are relative to the position after the operand (target - position + 2)
                var target = EvalExpr(ctx, branchOffset.Target);
                if (target.Type == SymbolType.Label)
                {
                    value = target.Value - ctx.Position + 2;
                    // Branch offsets can be short (2 bytes) or word (4 bytes)
                    if (value >= -32768 && value <= 32767)
                        type = OPERAND_CONSTANT_SHORT;
                    else
                        type = OPERAND_CONSTANT_WORD;
                }
                else if (target.Type == SymbolType.Unknown && !ctx.FinalPass)
                {
                    // Not defined yet - will be fixed up later
                    // For now, assume it will be a short offset
                    type = OPERAND_CONSTANT_SHORT;
                    value = 0;
                    if (branchOffset.Target is SymbolExpr se)
                        fixup = new Fixup(se.Text);
                }
                else
                {
                    Errors.Serious(ctx, node, "branch offset must reference a label");
                    type = OPERAND_CONSTANT_SHORT;
                    value = 0;
                }
                return;
            }
    
            if (node is SymbolExpr symExpr)
            {
                var text = symExpr.Text;
                
                // Check for special keywords
                if (text == "stack")
                {
                    type = OPERAND_STACK;
                    value = 0;
                    return;
                }
                
                if (ctx.LocalSymbols.TryGetValue(text, out var sym))
                {
                    if (sym.Type == SymbolType.Label)
                    {
                        // Local labels as absolute addresses
                        value = sym.Value;
                        if (value == 0)
                            type = OPERAND_ZERO;
                        else
                            type = OPERAND_CONSTANT_WORD;
                    }
                    else
                    {
                        Debug.Assert(sym.Type == SymbolType.Variable);
                        // Local variables use callframe addressing
                        value = (sym.Value - 1) * 4;  // only 32-bit locals are supported
                        if ((value & 0xff) == value)
                            type = OPERAND_CALLFRAME_BYTE;
                        else if ((value & 0xffff) == value)
                            type = OPERAND_CALLFRAME_SHORT;
                        else
                            type = OPERAND_CALLFRAME_WORD;
                    }
                }
                else if (ctx.GlobalSymbols.TryGetValue(text, out sym))
                {
                    if (sym.Type == SymbolType.Variable)
                    {
                        // Global variables use RAM addressing
                        if ((sym.Value & 0xff) == sym.Value)
                            type = OPERAND_RAM_BYTE;
                        else if ((sym.Value & 0xffff) == sym.Value)
                            type = OPERAND_RAM_SHORT;
                        else
                            type = OPERAND_RAM_WORD;
                    }
                    else if (sym.Value == 0)
                        type = OPERAND_ZERO;
                    else if ((sym.Value & 0xff) == sym.Value)
                        type = OPERAND_CONSTANT_BYTE;
                    else if ((sym.Value & 0xffff) == sym.Value)
                        type = OPERAND_CONSTANT_SHORT;
                    else
                        type = OPERAND_CONSTANT_WORD;
                    value = sym.Value;
                }
                else if (ctx.FinalPass)
                {
                    Errors.ThrowFatal(node, "undefined symbol: {0}", text);
                    // ReSharper disable once HeuristicUnreachableCode
                    type = 0;
                    value = 0;
                }
                else
                {
                    // Not defined yet - will be fixed up later
                    type = OPERAND_CONSTANT_BYTE;
                    value = 0;
                    fixup = new Fixup(text);
                }
            }
            else
            {
                var sym = EvalExpr(ctx, node);
                value = sym.Value;
                if (value == 0)
                    type = OPERAND_ZERO;
                else if ((value & 0xff) == value)
                    type = OPERAND_CONSTANT_BYTE;
                else if ((value & 0xffff) == value)
                    type = OPERAND_CONSTANT_SHORT;
                else
                    type = OPERAND_CONSTANT_WORD;
            }

            if (apos)
                type = OPERAND_CONSTANT_BYTE;
        }

        static bool IsLongConstant(Context ctx, AsmExpr node)
        {
            switch (node)
            {
                case NumericLiteral num:
                    return ((uint)num.Value & 0xffffff00) != 0;

                case SymbolExpr symExpr:
                    if (ctx.LocalSymbols.TryGetValue(symExpr.Text, out _))
                    {
                        // the only legal local symbol operand is a local variable
                        return false;
                    }
                    else if (ctx.GlobalSymbols.TryGetValue(symExpr.Text, out var sym))
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

                case AdditionExpr add:
                    if (IsLongConstant(ctx, add.Left) || IsLongConstant(ctx, add.Right))
                        return true;
                    var left = EvalExpr(ctx, add.Left);
                    var right = EvalExpr(ctx, add.Right);
                    return ((uint)(left.Value + right.Value) & 0xffffff00) != 0;

                case StringLiteral:
                case QuoteExpr:
                    return false;

                case BranchOffsetExpr:
                    // Branch offsets can be long
                    return true;

                default:
                    throw new ArgumentException("Unexpected expr type: " + node.GetType().Name, nameof(node));
            }
        }

        static void HandleDirective(Context ctx, AsmLine node, int nodeIndex, bool assembling)
        {
            // local scope is terminated by any directive except .DEBUG_LINE (not counting labels)
            // or directives that apply to the following instruction (.FORM/.OPERAND)
            if (node is not (DebugLineDirective or FormDirective or OperandDirective))
                ctx.EndReassemblyScope(nodeIndex);

            byte[] bytes;

            switch (node)
            {
                case NullDirective:
                    // nada
                    break;

                case FunctDirective functNode:
                    BeginFunction(ctx, functNode, nodeIndex);
                    break;

                case AlignDirective alignNode:
                    var divisor = EvalExpr(ctx, alignNode.Divisor);
                    if (divisor.Type != SymbolType.Constant)
                    {
                        Errors.ThrowSerious(alignNode.Divisor, "non-constant argument to .ALIGN");
                    }
                    AlignUnpacked(ctx, divisor.Value);
                    break;

                case TableDirective tableNode:
                    if (ctx.TableStart != null)
                    {
                        Errors.Warn(ctx, node, "starting new table before ending old table");
                    }
                    ctx.TableStart = ctx.Position;
                    ctx.TableSize = null;
                    if (tableNode.Size != null)
                    {
                        var sym = EvalExpr(ctx, tableNode.Size);
                        if (sym.Type != SymbolType.Constant)
                            Errors.Warn(ctx, node, "ignoring non-constant table size specifier");
                        else
                            ctx.TableSize = sym.Value;
                    }
                    break;

                case EndtDirective:
                    if (ctx.TableStart == null)
                    {
                        Errors.Warn(ctx, node, "ignoring .ENDT outside of a table definition");
                    }
                    else if (ctx.TableSize != null)
                    {
                        if (ctx.Position - ctx.TableStart.Value != ctx.TableSize.Value)
                            Errors.Warn(ctx, node, "incorrect table size: expected {0}, actual {1}",
                                ctx.TableSize.Value,
                                ctx.Position - ctx.TableStart.Value);
                    }
                    ctx.TableStart = null;
                    ctx.TableSize = null;
                    break;

                case DataDirective dataNode:
                    var elements = dataNode.Elements;
                    foreach (var expr in elements)
                    {
                        var sym = EvalExpr(ctx, expr);
                        if (sym.Type == SymbolType.Unknown && ctx.FinalPass)
                        {
                            Errors.ThrowFatal(expr, "unrecognized symbol: " + ((SymbolExpr)expr).Text);
                        }
                        if (node is ByteDirective)
                        {
                            if (sym.Value < -128 || sym.Value > 255)
                            {
                                Errors.Warn(ctx, node, "byte value out of range: " + sym.Value);
                            }

                            ctx.WriteByte((byte)sym.Value);
                        }
                        else
                        {
                            ctx.WriteWord(sym.Value);
                        }
                    }
                    break;

                case GstrDirective gstrNode:
                    // Write a string object (type prefix, null terminator)
                    // TODO: support more string types than E0
                    ctx.GlobalSymbols[gstrNode.Name] = new Symbol(gstrNode.Name, SymbolType.String, ctx.Position);
                    ctx.WriteByte(0xe0);
                    bytes = Encoding.Latin1.GetBytes(gstrNode.Text);
                    foreach (var b in bytes)
                    {
                        ctx.WriteByte(b);
                    }
                    ctx.WriteByte(0);
                    break;

                case StrDirective strNode:
                    // Write raw string bytes (no terminator)
                    bytes = Encoding.Latin1.GetBytes(strNode.Text);
                    foreach (var b in bytes)
                    {
                        ctx.WriteByte(b);
                    }
                    break;

                case EqualsDirective equalsNode:
                    var rvalue = EvalExpr(ctx, equalsNode.Right);
                    if (rvalue.Type == SymbolType.Unknown && ctx.FinalPass)
                    {
                        Errors.ThrowFatal(equalsNode.Right, "unrecognized symbol");
                    }
                    else
                    {
                        ctx.GlobalSymbols[equalsNode.Left] = rvalue;
                    }
                    break;

                case GvarDirective gvarNode:
                    ctx.AddGlobalVar(gvarNode.Name);
                    if (gvarNode.InitialValue != null)
                    {
                        var sym = EvalExpr(ctx, gvarNode.InitialValue);
                        if (sym.Type == SymbolType.Unknown && ctx.FinalPass)
                        {
                            Errors.ThrowFatal(gvarNode.InitialValue, "unrecognized symbol");
                        }
                        ctx.WriteWord((ushort)sym.Value);
                    }
                    else
                    {
                        ctx.WriteWord(0);
                    }
                    break;

                case DebugDirective debugNode:
                    if (assembling)
                    {
                        if (!ctx.IsDebugFileOpen)
                            ctx.OpenDebugFile();

                        HandleDebugDirective(ctx, debugNode);
                    }
                    break;

                default:
                    throw new NotImplementedException();
            }
        }

        static void HandleDebugDirective(Context ctx, DebugDirective node)
        {
            Debug.Assert(ctx.DebugWriter != null);

            if (ctx.DebugWriter.InRoutine &&
                !(node is DebugLineDirective || node is DebugRoutineEndDirective))
            {
                Errors.Serious(ctx, node, "debug directives other than .DEBUG-LINE not allowed inside routines");
                return;
            }

            LineRef MakeLineRef(AsmExpr file, AsmExpr line, AsmExpr column)
            {
                return new LineRef(
                    File: (byte)EvalExpr(ctx, file).Value,
                    Line: (ushort)EvalExpr(ctx, line).Value,
                    Col: (byte)EvalExpr(ctx, column).Value);
            }

            switch (node)
            {
                case DebugActionDirective dact:
                    ctx.DebugWriter.WriteAction(
                        (ushort)EvalExpr(ctx, dact.Number).Value,
                        dact.Name);
                    break;

                case DebugArrayDirective darr:
                    if (!ctx.GlobalSymbols.TryGetValue("GLOBAL", out var sym1))
                    {
                        Errors.Serious(ctx, node, "define GLOBAL before using .DEBUG-ARRAY");
                        return;
                    }
                    var sym2 = EvalExpr(ctx, darr.Number);
                    ctx.DebugWriter.WriteArray(
                        (ushort)(sym2.Value - sym1!.Value),
                        darr.Name);
                    break;

                case DebugAttrDirective dattr:
                    ctx.DebugWriter.WriteAttr(
                        (ushort)EvalExpr(ctx, dattr.Number).Value,
                        dattr.Name);
                    break;

                case DebugClassDirective dclass:
                    ctx.DebugWriter.WriteClass(
                        dclass.Name,
                        MakeLineRef(dclass.StartFile, dclass.StartLine, dclass.StartColumn),
                        MakeLineRef(dclass.EndFile, dclass.EndLine, dclass.EndColumn));
                    break;

                case DebugFakeActionDirective dfake:
                    ctx.DebugWriter.WriteFakeAction(
                        (ushort)EvalExpr(ctx, dfake.Number).Value,
                        dfake.Name);
                    break;

                case DebugFileDirective dfile:
                    ctx.DebugWriter.WriteFile(
                        (byte)EvalExpr(ctx, dfile.Number).Value,
                        dfile.IncludeName,
                        dfile.ActualName);
                    break;

                case DebugGlobalDirective dglob:
                    ctx.DebugWriter.WriteGlobal(
                        (byte)(EvalExpr(ctx, dglob.Number).Value - 16),
                        dglob.Name);
                    break;

                case DebugLineDirective dline:
                    if (!ctx.DebugWriter.InRoutine)
                    {
                        Errors.Serious(ctx, node, ".DEBUG-LINE outside of .DEBUG-ROUTINE");
                    }
                    else
                    {
                        ctx.DebugWriter.WriteLine(
                            MakeLineRef(dline.TheFile, dline.TheLine, dline.TheColumn),
                            ctx.Position);
                    }
                    break;

                case DebugMapDirective dmap:
                    ctx.DebugFileMap[dmap.Key] =
                        dmap.Value != null ? EvalExpr(ctx, dmap.Value) : new Symbol(ctx.Position);
                    break;

                case DebugObjectDirective dobj:
                    ctx.DebugWriter.WriteObject(
                        (ushort)EvalExpr(ctx, dobj.Number).Value,
                        ((DebugObjectDirective)node).Name,
                        MakeLineRef(dobj.StartFile, dobj.StartLine, dobj.StartColumn),
                        MakeLineRef(dobj.EndFile, dobj.EndLine, dobj.EndColumn));
                    break;

                case DebugPropDirective dprop:
                    ctx.DebugWriter.WriteProp(
                        (ushort)EvalExpr(ctx, dprop.Number).Value,
                        dprop.Name);
                    break;

                case DebugRoutineDirective drtn:
                    ctx.DebugWriter.StartRoutine(
                        MakeLineRef(drtn.TheFile, drtn.TheLine, drtn.TheColumn),
                        ctx.Position,
                        drtn.Name,
                        drtn.Locals);
                    break;

                case DebugRoutineEndDirective drend:
                    if (!ctx.DebugWriter.InRoutine)
                    {
                        Errors.Serious(ctx, node, ".DEBUG-ROUTINE-END outside of .DEBUG-ROUTINE");
                    }
                    else
                    {
                        ctx.DebugWriter.EndRoutine(
                            MakeLineRef(drend.TheFile, drend.TheLine, drend.TheColumn),
                            ctx.Position);
                    }
                    break;
            }
        }

        static void BeginFunction(Context ctx, FunctDirective node, int nodeIndex)
        {
            var localNames = new List<string>();

            string name = node.Name;
            foreach (var local in node.Locals)
            {
                if (ctx.LocalSymbols.ContainsKey(local.Name))
                {
                    Errors.ThrowSerious(node, "duplicate local: {0}", local.Name);
                }

                if (local.DefaultValue != null)
                {
                    throw new NotSupportedException("Local variable default values are not supported");
                }

                localNames.Add(local.Name);
                ctx.LocalSymbols.Add(local.Name,
                    new Symbol(local.Name, SymbolType.Variable, localNames.Count));
            }

            // if (localNames.Count > 15)
            //     Errors.ThrowSerious(node, "too many local variables");

            int addr = ctx.Position;
            if (!ctx.GlobalSymbols.TryGetValue(name, out var sym))
            {
                sym = new Symbol(name, SymbolType.Function, addr);
                ctx.GlobalSymbols.Add(name, sym);
            }
            else if (sym!.Type != SymbolType.Unknown && (!sym.Phantom || sym.Type != SymbolType.Function))
            {
                Errors.ThrowSerious("function redefined: " + name);
            }
            else
            {
                sym.Type = SymbolType.Function;
                sym.Phantom = false;
                if (sym.Value != addr)
                {
                    sym.Value = addr;
                    ctx.MeasureAgain = true;
                }
            }

            ctx.BeginReassemblyScope(nodeIndex, sym);

            ctx.WriteByte(0xc1);        // local-argument function

            int localCount = localNames.Count;
            while (localCount > 0)
            {
                int n = Math.Min(255, localCount);
                ctx.WriteByte(4);       // 32-bit locals
                ctx.WriteByte((byte)n);
                localCount -= n;
            }
            ctx.WriteByte(0);           // end of locals
            ctx.WriteByte(0);
        }

        static void AlignUnpacked(Context ctx, int divisor)
        {
            while (ctx.Position % divisor != 0)
                ctx.WriteByte(0);
        }

        const byte OPERAND_ZERO = 0;
        const byte OPERAND_CONSTANT_BYTE = 1;
        const byte OPERAND_CONSTANT_SHORT = 2;
        const byte OPERAND_CONSTANT_WORD = 3;
        // 4 is unused
        const byte OPERAND_MEMORY_BYTE = 5;
        const byte OPERAND_MEMORY_SHORT = 6;
        const byte OPERAND_MEMORY_WORD = 7;
        const byte OPERAND_STACK = 8;
        const byte OPERAND_CALLFRAME_BYTE = 9;
        const byte OPERAND_CALLFRAME_SHORT = 10;
        const byte OPERAND_CALLFRAME_WORD = 11;
        // 12 is unused
        const byte OPERAND_RAM_BYTE = 13;
        const byte OPERAND_RAM_SHORT = 14;
        const byte OPERAND_RAM_WORD = 15;

        static readonly byte[] tmpOperandTypes = new byte[8];
        static readonly int[] tmpOperandValues = new int[8];
        static readonly Fixup?[] tmpOperandFixups = new Fixup[8];

        static void HandleInstruction(Context ctx, Instruction node)
        {
            Debug.Assert(ctx.OpcodeDict != null);
            var (opcode, attr) = ctx.OpcodeDict[node.Name];
            int operandCount = node.Operands.Count;

            if (operandCount != attr.OperandCount)
            {
                Errors.Serious(ctx, node, "wrong number of operands for instruction {0}: expected {1}, got {2}",
                    node.Name, attr.OperandCount, operandCount);
                operandCount = Math.Min(operandCount, attr.OperandCount);
            }

            // opcode
            if (opcode <= 0x7f)
            {
                ctx.WriteByte((byte)opcode);
            }
            else if (opcode <= 0x3fff)
            {
                ctx.WriteByte((byte)((opcode >> 8) | 0x80));
                ctx.WriteByte((byte)(opcode & 0xff));
            }
            else
            {
                ctx.WriteByte((byte)((opcode >> 16) | 0xc0));
                ctx.WriteByte((byte)((opcode >> 8) & 0xff));
                ctx.WriteByte((byte)(opcode & 0xff));
            }

            // operand addressing modes
            byte modeByte = 0;
            for (int i = 0; i < operandCount; i++)
            {
                EvalOperand(ctx, node.Operands[i], out tmpOperandTypes[i], out tmpOperandValues[i], out tmpOperandFixups[i]);
                if (i % 2 == 0)
                {
                    modeByte = tmpOperandTypes[i];
                }
                else
                {
                    modeByte |= (byte)(tmpOperandTypes[i] << 4);
                    ctx.WriteByte(modeByte);
                    modeByte = 0;
                }
            }
            if (operandCount % 2 != 0)
            {
                ctx.WriteByte(modeByte);
            }

            // operands
            for (int i = 0; i < operandCount; i++)
            {
                if (tmpOperandFixups[i] != null)
                {
                    tmpOperandFixups[i]!.Location = ctx.Position;
                    ctx.Fixups.Add(tmpOperandFixups[i]!);
                    tmpOperandFixups[i] = null;
                }

                switch (tmpOperandTypes[i])
                {
                    case OPERAND_ZERO:
                    case OPERAND_STACK:
                        // zero bytes
                        break;

                    case OPERAND_CONSTANT_BYTE:
                    case OPERAND_MEMORY_BYTE:
                    case OPERAND_CALLFRAME_BYTE:
                    case OPERAND_RAM_BYTE:
                        // one byte
                        ctx.WriteByte((byte)tmpOperandValues[i]);
                        break;

                    case OPERAND_CONSTANT_SHORT:
                    case OPERAND_MEMORY_SHORT:
                    case OPERAND_CALLFRAME_SHORT:
                    case OPERAND_RAM_SHORT:
                        // two bytes
                        ctx.WriteShort((ushort)tmpOperandValues[i]);
                        break;

                    case OPERAND_CONSTANT_WORD:
                    case OPERAND_MEMORY_WORD:
                    case OPERAND_CALLFRAME_WORD:
                    case OPERAND_RAM_WORD:
                        // four bytes
                        ctx.WriteWord(tmpOperandValues[i]);
                        break;
                }
            }
        }

        static void HandleLabel(Context ctx, AsmLine node, ref int nodeIndex)
        {
            string name;

            switch (node)
            {
                case GlobalLabel globalNode:
                    name = globalNode.Name;
                    if (ctx.GlobalSymbols.TryGetValue(name, out var sym))
                    {
                        // we don't require it to be a phantom because a global label might be
                        // defined inside a routine, and reassembly could cause it to be defined twice
                        if (sym.Type != SymbolType.Label && sym.Type != SymbolType.Unknown /*|| !sym.Phantom*/)
                            Errors.ThrowSerious(node, "redefining global label");

                        if (sym.Value != ctx.Position)
                        {
                            var expected = sym.Value;

                            void CheckMismatch()
                            {
                                Debug.Assert(sym != null);

                                if (sym.Value == expected)
                                    return;

                                if (ctx.FinalPass)
                                    Errors.ThrowFatal(node, "global label {0} seems to have moved: was {1}, now {2}",
                                        name, expected, sym.Value);

                                ctx.MeasureAgain = true;
                            }

                            sym.Value = ctx.Position;

                            if (ctx.InReassemblyScope)
                            {
                                // don't panic yet, since the value might be correct after reassembly.
                                // just save the expected value and check it at the end of the reassembly scope.
                                ctx.DeferGlobalLabelStabilityCheck(sym, CheckMismatch);
                            }
                            else
                            {
                                CheckMismatch();
                            }
                        }

                        sym.Type = SymbolType.Label;
                        sym.Phantom = false;
                    }
                    else
                    {
                        ctx.GlobalSymbols.Add(name, new Symbol(name, SymbolType.Label, ctx.Position));
                    }
                    break;

                case LocalLabel localNode:
                    if (!ctx.InReassemblyScope)
                        Errors.ThrowSerious(node, "local labels not allowed outside a function");

                    name = localNode.Name;
                    if (!ctx.LocalSymbols.TryGetValue(name, out sym))
                    {
                        if (ctx.CausesReassembly(name))
                            nodeIndex = ctx.Reassemble(name) - 1;
                        else
                            ctx.LocalSymbols.Add(name, new Symbol(name, SymbolType.Label, ctx.Position));
                    }
                    else if (sym!.Type == SymbolType.Label && sym.Phantom)
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
                    break;

                default:
                    throw new NotImplementedException();
            }
        }
    }
}
