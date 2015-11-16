﻿/* Copyright 2010, 2015 Jesse McGrew
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
using System.Diagnostics.Contracts;
using System.IO;
using Zilf.Emit.Zap;
using Zilf.Interpreter;
using Zilf.Language;

namespace Zilf.Compiler
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

    public struct FrontEndResult
    {
        public bool Success;
        public int ErrorCount;
        public int WarningCount;
    }

    public sealed class FrontEnd
    {
        public FrontEnd()
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
            private readonly FrontEnd owner;
            private readonly string mainFile, fwordsFile, dataFile, stringFile;

            private const string FrequentWordsSuffix = "_freq";
            private const string DataSuffix = "_data";
            private const string StringSuffix = "_str";

            public ZapStreamFactory(FrontEnd owner, string mainFile)
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
                get { return owner.CheckFileExists(fwordsFile) || owner.CheckFileExists(Path.ChangeExtension(fwordsFile, ".xzap")); }
            }

            #endregion
        }

        public FrontEndResult Interpret(string inputFileName)
        {
            return Interpret(new Context(), inputFileName);
        }

        internal FrontEndResult Interpret(Context ctx, string inputFileName)
        {
            return InterpretOrCompile(ctx, inputFileName, null, false, false);
        }

        public FrontEndResult Compile(string inputFileName, string outputFileName, bool wantDebugInfo = false)
        {
            return Compile(new Context(), inputFileName, outputFileName, wantDebugInfo);
        }

        internal FrontEndResult Compile(Context ctx, string inputFileName, string outputFileName, bool wantDebugInfo = false)
        {
            return InterpretOrCompile(ctx, inputFileName, outputFileName, true, wantDebugInfo);
        }

        private FrontEndResult InterpretOrCompile(Context ctx, string inputFileName, string outputFileName, bool wantCompile, bool wantDebugInfo)
        {
            var result = new FrontEndResult();

            // open input file
            using (var inputStream = OpenFile(inputFileName, false))
            {
                // evaluate source text
                ICharStream charStream = new ANTLRInputStream(inputStream);

                ctx.CurrentFile = inputFileName;
                ctx.InterceptOpenFile = this.OpenFile;
                ctx.InterceptFileExists = this.CheckFileExists;
                ctx.IncludePaths.AddRange(this.IncludePaths);
                Zilf.Program.Evaluate(ctx, charStream);

                // compile, if there were no evaluation errors
                if (wantCompile && ctx.ErrorCount == 0)
                {
                    ctx.SetDefaultConstants();

                    try
                    {
                        var zversion = ctx.ZEnvironment.ZVersion;
                        var streamFactory = new ZapStreamFactory(this, outputFileName);
                        var options = MakeGameOptions(ctx);
                        var gameBuilder = new GameBuilder(zversion, streamFactory, wantDebugInfo, options);

                        Zilf.Compiler.Compiler.Compile(ctx, gameBuilder);
                    }
                    catch (ZilError ex)
                    {
                        ctx.HandleError(ex);
                    }
                }

                result.ErrorCount = ctx.ErrorCount;
                result.WarningCount = ctx.WarningCount;
                result.Success = (ctx.ErrorCount == 0);
                return result;
            }
        }

        internal static GameOptions MakeGameOptions(Context ctx)
        {
            Contract.Requires(ctx != null);

            var zenv = ctx.ZEnvironment;

            switch (zenv.ZVersion)
            {
                case 3:
                    return new Zilf.Emit.Zap.GameOptions.V3()
                    {
                        TimeStatusLine = zenv.TimeStatusLine,
                        SoundEffects = ctx.GetGlobalOption(StdAtom.USE_SOUND_P) || ctx.GetGlobalOption(StdAtom.SOUND_EFFECTS_P),
                    };

                case 4:
                    return new Zilf.Emit.Zap.GameOptions.V4()
                    {
                        SoundEffects = ctx.GetGlobalOption(StdAtom.USE_SOUND_P) || ctx.GetGlobalOption(StdAtom.SOUND_EFFECTS_P),
                    };

                case 5:
                case 6:
                case 7:
                case 8:
                    var defaultLang = ZModel.Language.Get("DEFAULT");
                    var doCharset =
                        zenv.Charset0 != defaultLang.Charset0 ||
                        zenv.Charset1 != defaultLang.Charset1 ||
                        zenv.Charset2 != defaultLang.Charset2;

                    var doLang = zenv.LanguageEscapeChar != null;

                    return new Zilf.Emit.Zap.GameOptions.V5()
                    {
                        DisplayOps = ctx.GetGlobalOption(StdAtom.DISPLAY_OPS_P),
                        Undo = ctx.GetGlobalOption(StdAtom.USE_UNDO_P),
                        Mouse = ctx.GetGlobalOption(StdAtom.USE_MOUSE_P),
                        Color = ctx.GetGlobalOption(StdAtom.USE_COLOR_P),
                        SoundEffects = ctx.GetGlobalOption(StdAtom.USE_SOUND_P) || ctx.GetGlobalOption(StdAtom.SOUND_EFFECTS_P),
                        Charset0 = doCharset ? zenv.Charset0 : null,
                        Charset1 = doCharset ? zenv.Charset1 : null,
                        Charset2 = doCharset ? zenv.Charset2 : null,
                        LanguageId = doLang ? zenv.Language.Id : 0,
                        LanguageEscapeChar = doLang ? zenv.LanguageEscapeChar : null,
                    };

                default:
                    return null;
            }
        }
    }
}