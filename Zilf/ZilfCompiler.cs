﻿/* Copyright 2010-2014 Jesse McGrew
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

using Antlr.Runtime;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using Zilf.Emit.Zap;

namespace Zilf
{
    public class OpeningFileEventArgs : EventArgs
    {
        public OpeningFileEventArgs(string filename, bool writing)
        {
            this.FileName = filename;
            this.Writing = writing;
        }

        public string FileName { get; private set; }
        public bool Writing { get; private set; }
        public Stream Stream { get; set; }
    }

    public class CheckingFilePresenceEventArgs : EventArgs
    {
        public CheckingFilePresenceEventArgs(string filename)
        {
            this.FileName = filename;
        }

        public string FileName { get; private set; }
        public bool? Exists { get; set; }
    }

    public sealed class ZilfCompiler
    {
        public ZilfCompiler()
        {
            this.IncludePaths = new List<string>();
        }
        
        public event EventHandler<OpeningFileEventArgs> OpeningFile;
        public event EventHandler<CheckingFilePresenceEventArgs> CheckingFilePresence;

        public IList<string> IncludePaths { get; private set; }

        private Stream OpenFile(string path, bool writing)
        {
            var handler = this.OpeningFile;
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
                writing ? FileAccess.Write : FileAccess.Read);
        }

        private bool CheckFileExists(string path)
        {
            var handler = this.CheckingFilePresence;
            if (handler != null)
            {
                var args = new CheckingFilePresenceEventArgs(path);

                handler(this, args);

                if (args.Exists.HasValue)
                    return args.Exists.Value;
            }

            return File.Exists(path);
        }

        private class ZapStreamFactory : IZapStreamFactory
        {
            private readonly ZilfCompiler owner;
            private readonly string mainFile, fwordsFile, dataFile, stringFile;

            private const string FrequentWordsSuffix = "_freq";
            private const string DataSuffix = "_data";
            private const string StringSuffix = "_str";

            public ZapStreamFactory(ZilfCompiler owner, string mainFile)
            {
                this.owner = owner;
                this.mainFile = mainFile;

                var dir = Path.GetDirectoryName(mainFile);
                var baseName = Path.GetFileNameWithoutExtension(mainFile);
                var ext = Path.GetExtension(mainFile);

                mainFile = Path.Combine(dir, baseName + ext);
                fwordsFile = Path.Combine(dir, baseName + FrequentWordsSuffix + ext);
                dataFile = Path.Combine(dir, baseName + DataSuffix + ext);
                stringFile = Path.Combine(dir, baseName + StringSuffix + ext);
            }

            #region IZapStreamFactory Members

            public Stream CreateMainStream()
            {
                return owner.OpenFile(mainFile, true);
            }

            public Stream CreateFrequentWordsStream()
            {
                return owner.OpenFile(fwordsFile, true);
            }

            public Stream CreateDataStream()
            {
                return owner.OpenFile(dataFile, true);
            }

            public Stream CreateStringStream()
            {
                return owner.OpenFile(stringFile, true);
            }

            public string GetMainFileName(bool withExt)
            {
                var result = mainFile;
                if (!withExt)
                    result = Path.ChangeExtension(result, null);
                return result;
            }

            public string GetDataFileName(bool withExt)
            {
                var result = dataFile;
                if (!withExt)
                    result = Path.ChangeExtension(result, null);
                return result;
            }

            public string GetFrequentWordsFileName(bool withExt)
            {
                var result = fwordsFile;
                if (!withExt)
                    result = Path.ChangeExtension(result, null);
                return result;
            }

            public string GetStringFileName(bool withExt)
            {
                var result = stringFile;
                if (!withExt)
                    result = Path.ChangeExtension(result, null);
                return result;
            }

            public bool FrequentWordsFileExists
            {
                get { return owner.CheckFileExists(fwordsFile); }
            }

            #endregion
        }

        public bool Compile(string inputFileName, string outputFileName, bool wantDebugInfo = false)
        {
            // open input file
            using (var inputStream = OpenFile(inputFileName, false))
            {
                // evaluate source text
                ICharStream charStream = new ANTLRInputStream(inputStream);

                var ctx = new Context();

                ctx.CurrentFile = inputFileName;
                ctx.InterceptOpenFile = this.OpenFile;
                ctx.InterceptFileExists = this.CheckFileExists;
                ctx.IncludePaths.AddRange(this.IncludePaths);
                Zilf.Program.Evaluate(ctx, charStream);

                // check for evaluation errors
                if (ctx.ErrorCount > 0)
                {
                    //XXX log message
                    throw new NotImplementedException("evaluation failed");
                }

                // generate code
                ctx.SetDefaultConstants();

                Compiler c = new Compiler();
                try
                {
                    var zversion = ctx.ZEnvironment.ZVersion;
                    var streamFactory = new ZapStreamFactory(this, outputFileName);

                    c.Compile(ctx, new GameBuilder(zversion, streamFactory, wantDebugInfo));
                }
                catch (ZilError ex)
                {
                    ctx.HandleError(ex);
                }

                // check for compilation errors
                if (ctx.ErrorCount > 0)
                {
                    return false;
                }

                // done
                return true;
            }
        }
    }
}