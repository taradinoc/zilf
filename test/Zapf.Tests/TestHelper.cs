/* Copyright 2010-2018 Jesse McGrew
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
using JetBrains.Annotations;

namespace Zapf.Tests
{
    struct AssemblyTestInput
    {
        [NotNull]
        public string Code;

        [CanBeNull, ItemNotNull]
        public string[] Args;

        [CanBeNull]
        public IDebugFileWriter DebugWriter;
    }

    struct AssemblyTestOutput
    {
        public bool Success;

        [NotNull]
        public MemoryStream StoryFile;

        [NotNull]
        public IDictionary<string, Symbol> Symbols;
    }

    static class TestHelper
    {
        public static bool Assemble([NotNull] string code) =>
            Assemble(new AssemblyTestInput { Code = code }).Success;

        [ContractAnnotation("=> false, storyFile: null; => true, storyFile: notnull")]
        public static bool Assemble([NotNull] string code, [CanBeNull] out MemoryStream storyFile)
        {
            var result = Assemble(new AssemblyTestInput { Code = code });
            storyFile = result.StoryFile;
            return result.Success;
        }

        [ContractAnnotation("=> false, storyFile: null; => true, storyFile: notnull")]
        public static bool Assemble([NotNull] string code, [NotNull, ItemNotNull] string[] args,
            [CanBeNull] out MemoryStream storyFile)
        {
            var result = Assemble(new AssemblyTestInput { Code = code, Args = args });
            storyFile = result.StoryFile;
            return result.Success;
        }

        [ContractAnnotation("=> false, symbols: null; => true, symbols: notnull")]
        public static bool Assemble([NotNull] string code, [NotNull] IDebugFileWriter debugWriter,
            [CanBeNull] out IDictionary<string, Symbol> symbols)
        {
            var result = Assemble(new AssemblyTestInput { Code = code, DebugWriter = debugWriter });
            symbols = result.Symbols;
            return result.Success;
        }

        public static AssemblyTestOutput Assemble(AssemblyTestInput input)
        {
            const string InputFileName = "Input.zap";
            const string OutputFileName = "Output.z#";
            var inputFiles = new Dictionary<string, string>
            {
                { InputFileName, input.Code }
            };
            var outputFiles = new Dictionary<string, MemoryStream>();

            // initialize ZapfAssembler
            var assembler = new ZapfAssembler();
            assembler.OpeningFile += (sender, e) =>
            {
                if (e.Writing)
                {
                    var mstr = new MemoryStream();
                    e.Stream = mstr;
                    outputFiles.Add(e.FileName, mstr);
                }
                else if (inputFiles.ContainsKey(e.FileName))
                {
                    var buffer = Encoding.UTF8.GetBytes(inputFiles[e.FileName]);
                    e.Stream = new MemoryStream(buffer, false);
                }
                else
                {
                    throw new InvalidOperationException("No such input file: " + e.FileName);
                }
            };
            assembler.CheckingFilePresence += (sender, e) =>
            {
                e.Exists = inputFiles.ContainsKey(e.FileName);
            };

            if (input.Args != null)
            {
                var args = input.Args;

                assembler.InitializingContext += (sender, e) =>
                {
                    var inFile = e.Context.InFile;
                    var outFile = e.Context.OutFile;

                    var newArgs = args.ToList();

                    if (inFile != null)
                    {
                        newArgs.Add(inFile);

                        if (outFile != null)
                            newArgs.Add(outFile);
                    }

                    e.Context = Program.ParseArgs(newArgs) ?? throw new ArgumentException("Invalid args", nameof(args));

                    e.Context.InFile = inFile;
                    e.Context.OutFile = outFile;
                };
            }

            if (input.DebugWriter != null)
            {
                var wtr = input.DebugWriter;

                assembler.InitializingContext += (sender, e) =>
                {
                    e.Context.InterceptGetDebugWriter = _ => wtr;
                };
            }

            // run assembly
            var result = assembler.Assemble(InputFileName, OutputFileName);
            if (result.Success)
            {
                return new AssemblyTestOutput
                {
                    Success = true,

                    StoryFile = (from pair in outputFiles
                                 let ext = Path.GetExtension(pair.Key)
                                 where ext.Length == 3 && ext.StartsWith(".z")
                                 select pair.Value).Single(),

                    Symbols = result.Context.GlobalSymbols
                };
            }

            return new AssemblyTestOutput { Success = false };
        }
    }
}
