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
using System.Diagnostics.CodeAnalysis;
using System.IO;
using System.Linq;
using System.Text;
using Zilf.Common;

namespace Zapf.Tests
{
    struct AssemblyTestInput
    {
        public string Code;

        public string[]? Args;

        public IDebugFileWriter? DebugWriter;
    }

    struct AssemblyTestOutput
    {
        public bool Success;

        public MemoryStream? StoryFile;

        public IDictionary<string, Symbol>? Symbols;
    }

    static class TestHelper
    {
        public static bool Assemble(string code) =>
            Assemble(new AssemblyTestInput { Code = code }).Success;

        public static bool Assemble(string code, [NotNullWhen(true)] out MemoryStream? storyFile)
        {
            var result = Assemble(new AssemblyTestInput { Code = code });
            storyFile = result.StoryFile;
            return result.Success;
        }

        public static bool Assemble(string code, string[] args, [NotNullWhen(true)] out MemoryStream? storyFile)
        {
            var result = Assemble(new AssemblyTestInput { Code = code, Args = args });
            storyFile = result.StoryFile;
            return result.Success;
        }

        public static bool Assemble(string code, IDebugFileWriter debugWriter,
            [NotNullWhen(true)] out IDictionary<string, Symbol>? symbols)
        {
            var result = Assemble(new AssemblyTestInput { Code = code, DebugWriter = debugWriter });
            symbols = result.Symbols;
            return result.Success;
        }

        public static AssemblyTestOutput Assemble(AssemblyTestInput input)
        {
            const string InputFileName = "Input.zap";
            const string OutputFileName = "Output.zcode";
            var fileSystem = InMemoryFileSystem.Of(InputFileName, input.Code);

            // initialize ZapfAssembler
            var assembler = new ZapfAssembler { FileSystem = fileSystem };

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

                    if (!Program.TryParseArgs(newArgs, e.Context))
                        throw new ArgumentException("Invalid args", nameof(input));

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
                    StoryFile = (MemoryStream)fileSystem.OpenForReading(OutputFileName),
                    Symbols = result.Context?.GlobalSymbols
                };
            }

            return new AssemblyTestOutput { Success = false };
        }
    }
}
