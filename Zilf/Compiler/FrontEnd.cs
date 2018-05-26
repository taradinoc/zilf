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
using System.Diagnostics;
using System.IO;
using Zilf.Emit.Zap;
using Zilf.Interpreter;
using Zilf.Language;
using JetBrains.Annotations;
using Zilf.Diagnostics;

namespace Zilf.Compiler
{
    public class OpeningFileEventArgs : EventArgs
    {
        public OpeningFileEventArgs(string fileName, bool writing)
        {
            FileName = fileName;
            Writing = writing;
        }

        public string FileName { get; }
        public bool Writing { get; }
        public Stream Stream { get; set; }
    }

    public class CheckingFilePresenceEventArgs : EventArgs
    {
        public CheckingFilePresenceEventArgs(string fileName)
        {
            FileName = fileName;
        }

        public string FileName { get; }
        public bool? Exists { get; set; }
    }

    class ContextEventArgs : EventArgs
    {
        public ContextEventArgs(Context ctx)
        {
            Context = ctx;
        }

        public Context Context { get; }
    }

    [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Performance", "CA1815:OverrideEqualsAndOperatorEqualsOnValueTypes")]
    public struct FrontEndResult
    {
        public bool Success { get; set; }
        public int ErrorCount { get; set; }
        public int WarningCount { get; set; }
        public IReadOnlyCollection<Diagnostic> Diagnostics { get; set; }
    }

    public sealed class FrontEnd
    {
        public FrontEnd()
        {
            IncludePaths = new List<string>();
        }
        
        public event EventHandler<OpeningFileEventArgs> OpeningFile;
        public event EventHandler<CheckingFilePresenceEventArgs> CheckingFilePresence;
        internal event EventHandler<ContextEventArgs> InitializeContext;

        public IList<string> IncludePaths { get; }

        [NotNull]
        Stream OpenFile(string path, bool writing)
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
                writing ? FileAccess.Write : FileAccess.Read);
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

        class ZapStreamFactory : IZapStreamFactory
        {
            [NotNull]
            readonly FrontEnd owner;

            [NotNull]
            readonly string mainFile;

            [NotNull]
            readonly string fwordsFile;

            [NotNull]
            readonly string dataFile;

            [NotNull]
            readonly string stringFile;

            const string FrequentWordsSuffix = "_freq";
            const string DataSuffix = "_data";
            const string StringSuffix = "_str";

            /// <exception cref="ArgumentException">mainFile is not a file name.</exception>
            public ZapStreamFactory([NotNull] FrontEnd owner, [NotNull] string mainFile)
            {
                this.owner = owner;
                this.mainFile = mainFile;

                var dir = Path.GetDirectoryName(mainFile);
                if (dir == null)
                    throw new ArgumentException("Must be a file name.", nameof(mainFile));

                var baseName = Path.GetFileNameWithoutExtension(mainFile);
                var ext = Path.GetExtension(mainFile);

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

            public bool FrequentWordsFileExists => owner.CheckFileExists(fwordsFile) ||
                                                   owner.CheckFileExists(Path.ChangeExtension(fwordsFile, ".xzap"));

            #endregion
        }

        [NotNull]
        Context NewContext(RunMode runMode, bool wantDebugInfo)
        {
            var result = new Context { RunMode = runMode, WantDebugInfo = wantDebugInfo };

            InitializeContext?.Invoke(this, new ContextEventArgs(result));

            return result;
        }

        internal FrontEndResult Interpret([NotNull] Context ctx, [NotNull] string inputFileName)
        {
            var f = InterpretOrCompile(ctx, inputFileName, null, false, false);
            return f;
        }

        public FrontEndResult Compile([NotNull] string inputFileName, [NotNull] string outputFileName, bool wantDebugInfo = false)
        {
            var ctx = NewContext(RunMode.Compiler, wantDebugInfo);
            return Compile(ctx, inputFileName, outputFileName, ctx.WantDebugInfo);
        }

        internal FrontEndResult Compile([NotNull] Context ctx, [NotNull] string inputFileName,
            [NotNull] string outputFileName, bool wantDebugInfo = false)
        {
            return InterpretOrCompile(ctx, inputFileName, outputFileName, true, wantDebugInfo);
        }

        // FIXME: not supported by R#, sadly...
        //[ContractAnnotation("wantCompile: true => outputFileName: notnull")]
        //[ContractAnnotation("wantCompile: false => outputFileName: null")]
        FrontEndResult InterpretOrCompile([NotNull] [ProvidesContext] Context ctx, [NotNull] string inputFileName,
            [CanBeNull] string outputFileName, bool wantCompile, bool wantDebugInfo)
        {
            var result = new FrontEndResult();

            Debug.Assert(!wantCompile || outputFileName != null);

            // open input file
            using (var inputStream = OpenFile(inputFileName, false))
            {
                // evaluate source text
                using (ctx.PushFileContext(inputFileName))
                {
                    ctx.InterceptOpenFile = OpenFile;
                    ctx.InterceptFileExists = CheckFileExists;
                    ctx.IncludePaths.AddRange(IncludePaths);
                    try
                    {
                        Program.Evaluate(ctx, inputStream);
                    }
                    catch (ZilErrorBase ex)
                    {
                        ctx.HandleError(ex);
                    }

                    // compile, if there were no evaluation errors
                    if (wantCompile && ctx.ErrorCount == 0)
                    {
                        ctx.RunHook("PRE-COMPILE");
                        ctx.SetDefaultConstants();

                        try
                        {
                            var zversion = ctx.ZEnvironment.ZVersion;
                            var streamFactory = new ZapStreamFactory(this, outputFileName);
                            var options = MakeGameOptions(ctx);

                            using (var gameBuilder = new GameBuilder(zversion, streamFactory, wantDebugInfo, options))
                            {
                                Compilation.Compile(ctx, gameBuilder);
                            }
                        }
                        catch (ZilErrorBase ex)     // catch fatals too
                        {
                            ctx.HandleError(ex);
                        }
                    }
                }

                result.ErrorCount = ctx.ErrorCount;
                result.WarningCount = ctx.WarningCount;
                result.Success = (ctx.ErrorCount == 0);
                result.Diagnostics = ctx.Diagnostics;
                return result;
            }
        }

        [NotNull]
        static GameOptions MakeGameOptions([NotNull] Context ctx)
        {

            var zenv = ctx.ZEnvironment;

            switch (zenv.ZVersion)
            {
                case 3:
                    return new GameOptions.V3
                    {
                        TimeStatusLine = zenv.TimeStatusLine,
                        SoundEffects = ctx.GetGlobalOption(StdAtom.USE_SOUND_P) ||
                                       ctx.GetGlobalOption(StdAtom.SOUND_EFFECTS_P)
                    };

                case 4:
                    return new GameOptions.V4
                    {
                        SoundEffects = ctx.GetGlobalOption(StdAtom.USE_SOUND_P) ||
                                       ctx.GetGlobalOption(StdAtom.SOUND_EFFECTS_P)
                    };

                case 5:
                case 7:
                case 8:
                    GameOptions.V5Plus v5Plus = new GameOptions.V5();
                    goto V5Plus;

                case 6:
                    v5Plus = new GameOptions.V6 { Menus = ctx.GetGlobalOption(StdAtom.USE_MENUS_P) };

                V5Plus:
                    var defaultLang = ZModel.Language.Default;
                    
                    var doCharset =
                        zenv.Charset0 != defaultLang.Charset0 ||
                        zenv.Charset1 != defaultLang.Charset1 ||
                        zenv.Charset2 != defaultLang.Charset2;

                    var doLang = zenv.LanguageEscapeChar != null;

                    v5Plus.DisplayOps = ctx.GetGlobalOption(StdAtom.DISPLAY_OPS_P);
                    v5Plus.Undo = ctx.GetGlobalOption(StdAtom.USE_UNDO_P);
                    v5Plus.Mouse = ctx.GetGlobalOption(StdAtom.USE_MOUSE_P);
                    v5Plus.Color = ctx.GetGlobalOption(StdAtom.USE_COLOR_P);
                    v5Plus.SoundEffects = ctx.GetGlobalOption(StdAtom.USE_SOUND_P) ||
                                            ctx.GetGlobalOption(StdAtom.SOUND_EFFECTS_P);

                    if (doCharset)
                    {
                        v5Plus.Charset0 = zenv.Charset0;
                        v5Plus.Charset1 = zenv.Charset1;
                        v5Plus.Charset2 = zenv.Charset2;
                    }

                    if (doLang)
                    {
                        v5Plus.LanguageId = zenv.Language.Id;
                        v5Plus.LanguageEscapeChar = zenv.LanguageEscapeChar;
                    }

                    return v5Plus;

                default:
                    throw new ArgumentException("Unsupported Z-machine version", nameof(ctx));
            }
        }
    }
}
