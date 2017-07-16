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
        public OpeningFileEventArgs(string filename, bool writing)
        {
            FileName = filename;
            Writing = writing;
        }

        public string FileName { get; }
        public bool Writing { get; }
        public Stream Stream { get; set; }
    }

    class CheckingFilePresenceEventArgs : EventArgs
    {
        public CheckingFilePresenceEventArgs(string filename)
        {
            FileName = filename;
        }

        public string FileName { get; }
        public bool? Exists { get; set; }
    }

    sealed class ZapfAssembler
    {
        public event EventHandler<OpeningFileEventArgs> OpeningFile;
        public event EventHandler<CheckingFilePresenceEventArgs> CheckingFilePresence;

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

        public bool Assemble(string inputFileName, string outputFileName)
        {
            return Assemble(inputFileName, outputFileName, out _, out _);
        }

        public bool Assemble(string inputFileName, string outputFileName, out int errorCount, out int warningCount)
        {
            // initialize context
            var ctx = new Context()
            {
                Quiet = true,
                InFile = inputFileName,
                OutFile = outputFileName,
                DebugFile = Path.ChangeExtension(outputFileName, ".dbg"),
                InterceptOpenFile = OpenFile,
                InterceptFileExists = CheckFileExists
            };

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
