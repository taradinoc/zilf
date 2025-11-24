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
using System.IO;
using Gaze.Parsing;
using Gaze.Parsing.Diagnostics;
using Zilf.Common;

namespace Gaze
{
    public sealed class GazeAssembler
    {
        public IFileSystem FileSystem { get; set; } = PhysicalFileSystem.Instance;

        public event EventHandler<InitializingContextEventArgs>? InitializingContext;

        Context InitializeContext(string inputFileName, string? outputFileName)
        {
            var ctx = new Context
            {
                Quiet = true,
                InFile = inputFileName,
                OutFile = outputFileName,
                DebugFile = Path.ChangeExtension(outputFileName, ".dbg"),
                FileSystem = FileSystem
            };

            var handler = InitializingContext;
            if (handler != null)
            {
                var args = new InitializingContextEventArgs(ctx);
                handler(this, args);
            }

            return ctx;
        }

        public AssemblyResult Assemble(string inputFileName, string outputFileName)
        {
            using var ctx = InitializeContext(inputFileName, outputFileName);

            //XXX redirect log messages

            // set up OpcodeDict and GlobalSymbols for the target version
            ctx.OpcodeDict = Program.MakeOpcodeDict();
            ctx.Restart();

            // perform assembly
            try
            {
                try
                {
                    Program.Assemble(ctx);
                }
                catch (FatalError fer)
                {
                    ctx.HandleFatalError(fer);
                    return AssemblyResult.Failed;
                }

                //XXX find abbreviations?

                // done
                return new AssemblyResult(ctx.ErrorCount == 0, ctx);
            }
            finally
            {
                ctx.CloseOutput();
                ctx.CloseDebugFile();
            }
        }
    }
}
