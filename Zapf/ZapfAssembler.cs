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
using System.IO;
using JetBrains.Annotations;
using Zapf.Parsing;
using Zapf.Parsing.Diagnostics;

namespace Zapf
{
    class OpeningFileEventArgs : EventArgs
    {
        public OpeningFileEventArgs([NotNull] string filename, bool writing)
        {
            FileName = filename;
            Writing = writing;
        }

        [NotNull]
        public string FileName { get; }

        public bool Writing { get; }

        [CanBeNull]
        public Stream Stream { get; set; }
    }

    class CheckingFilePresenceEventArgs : EventArgs
    {
        public CheckingFilePresenceEventArgs([NotNull] string filename)
        {
            FileName = filename;
        }

        [NotNull]
        public string FileName { get; }

        [CanBeNull]
        public bool? Exists { get; set; }
    }

    class InitializingContextEventArgs : EventArgs
    {
        public InitializingContextEventArgs([NotNull] Context ctx)
        {
            Context = ctx;
        }

        [NotNull]
        public Context Context { get; set; }
    }

    struct AssemblyResult
    {
        public AssemblyResult(bool success, Context context)
        {
            Success = success;
            Context = context;
        }

        public static readonly AssemblyResult Failed = new AssemblyResult(false, null);

        public bool Success { get; }
        public Context Context { get; }
    }

    sealed class ZapfAssembler
    {
        public event EventHandler<OpeningFileEventArgs> OpeningFile;
        public event EventHandler<CheckingFilePresenceEventArgs> CheckingFilePresence;
        public event EventHandler<InitializingContextEventArgs> InitializingContext;

        [NotNull]
        Stream OpenFile([NotNull] string path, bool writing)
        {
            var handler = OpeningFile;
            if (handler != null)
            {
                var args = new OpeningFileEventArgs(path, writing);

                handler(this, args);

                if (args.Stream != null)
                    return args.Stream;
            }

            return new FileStream(
                path,
                writing ? FileMode.Create : FileMode.Open,
                writing ? FileAccess.ReadWrite : FileAccess.Read);
        }

        bool CheckFileExists(string path)
        {
            var handler = CheckingFilePresence;
            if (handler != null)
            {
                var args = new CheckingFilePresenceEventArgs(path);

                handler(this, args);

                if (args.Exists.HasValue)
                    return args.Exists.Value;
            }

            return File.Exists(path);
        }

        [NotNull]
        Context InitializeContext([NotNull] string inputFileName, [CanBeNull] string outputFileName)
        {

            var ctx = new Context
            {
                Quiet = true,
                InFile = inputFileName,
                OutFile = outputFileName,
                DebugFile = Path.ChangeExtension(outputFileName, ".dbg")
            };

            var handler = InitializingContext;
            if (handler != null)
            {
                var args = new InitializingContextEventArgs(ctx);
                handler(this, args);
                ctx = args.Context;
            }

            ctx.InterceptOpenFile = OpenFile;
            ctx.InterceptFileExists = CheckFileExists;

            return ctx;
        }

        public AssemblyResult Assemble(string inputFileName, string outputFileName)
        {
            var ctx = InitializeContext(inputFileName, outputFileName);

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
