/* Copyright 2010-2017 Jesse McGrew
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
using System.Diagnostics.Contracts;
using System.IO;
using JetBrains.Annotations;

namespace Zapf
{
    class OpeningFileEventArgs : EventArgs
    {
        public OpeningFileEventArgs([NotNull] string filename, bool writing)
        {
            Contract.Requires(filename != null);
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
            Contract.Requires(filename != null);
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
            Contract.Requires(ctx != null);
            Context = ctx;
        }

        [NotNull]
        public Context Context { get; set; }
    }

    sealed class ZapfAssembler
    {
        public event EventHandler<OpeningFileEventArgs> OpeningFile;
        public event EventHandler<CheckingFilePresenceEventArgs> CheckingFilePresence;
        public event EventHandler<InitializingContextEventArgs> InitializingContext;

        [NotNull]
        Stream OpenFile(string path, bool writing)
        {
            Contract.Ensures(Contract.Result<Stream>() != null);
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
            Contract.Requires(inputFileName != null);
            Contract.Ensures(Contract.Result<Context>() != null);

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

        public bool Assemble(string inputFileName, string outputFileName)
        {
            return Assemble(inputFileName, outputFileName, out _, out _);
        }

        public bool Assemble(string inputFileName, string outputFileName, out int errorCount, out int warningCount)
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
                        return false;
                    }
                } while (restart);

                //XXX find abbreviations?

                // done
                return ctx.ErrorCount == 0;
            }
            finally
            {
                errorCount = ctx.ErrorCount;
                warningCount = ctx.WarningCount;

                ctx.CloseOutput();
                ctx.CloseDebugFile();
            }
        }
    }
}
