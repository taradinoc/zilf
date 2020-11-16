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
using System.Diagnostics.CodeAnalysis;
using System.IO;
using Zapf.Parsing;
using Zapf.Parsing.Diagnostics;
using Zilf.Common;

namespace Zapf
{
    [Obsolete("Use " + nameof(IFileSystem) + " instead.")]
    class OpeningFileEventArgs : EventArgs
    {
        public OpeningFileEventArgs(string filename, bool writing)
        {
            FileName = filename;
            Writing = writing;
        }

        public string FileName { get; }

        public bool Writing { get; }

        public Stream? Stream { get; set; }
    }

    [Obsolete("Use " + nameof(IFileSystem) + " instead.")]
    class CheckingFilePresenceEventArgs : EventArgs
    {
        public CheckingFilePresenceEventArgs(string filename) => FileName = filename;

        public string FileName { get; }

        public bool? Exists { get; set; }
    }

    class InitializingContextEventArgs : EventArgs
    {
        public InitializingContextEventArgs(Context ctx) => Context = ctx;

        public Context Context { get; }
    }

    readonly struct AssemblyResult
    {
        public AssemblyResult(bool success, Context? context)
        {
            Success = success;
            Context = context;
        }

        public static readonly AssemblyResult Failed = new AssemblyResult(false, null);

        [MemberNotNullWhen(true, nameof(Context))]
        public bool Success { get; }
        public Context? Context { get; }
    }

    sealed class ZapfAssembler
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
            ctx.OpcodeDict = Program.MakeOpcodeDict(ctx.ZVersion, ctx.InformMode);
            ctx.Restart();

            // perform assembly
            try
            {
                bool restart;
                do
                {
                    restart = false;
                    try
                    {
                        Program.Assemble(ctx);
                    }
                    catch (RestartException)
                    {
                        ctx.Restart();
                        restart = true;
                    }
                    catch (FatalError fer)
                    {
                        ctx.HandleFatalError(fer);
                        return AssemblyResult.Failed;
                    }
                } while (restart);

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
