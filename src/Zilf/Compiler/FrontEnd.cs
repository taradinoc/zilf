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
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.IO;
using Zilf.Common;
using Zilf.Diagnostics;
using Zilf.Emit;
using Zilf.Emit.Cornerstone;
using Zilf.Emit.Glulx;
using Zilf.Emit.Zap;
using Zilf.Interpreter;
using Zilf.Interpreter.Values;
using Zilf.Language;
using Zilf.ZModel;

using ZapGameOptions = Zilf.Emit.Zap.GameOptions;

namespace Zilf.Compiler
{
    sealed class ContextEventArgs : EventArgs
    {
        public ContextEventArgs(Context ctx)
        {
            Context = ctx;
        }

        public Context Context { get; }
    }

    public record FrontEndResult(bool Success, int ErrorCount, int WarningCount, int SuppressedWarningCount,
        IReadOnlyCollection<Diagnostic> Diagnostics);

    public interface IReplSession
    {
        /// <summary>
        /// Parses and evaluates a series of expressions in the current REPL context.
        /// </summary>
        /// <param name="expression">The expression(s) to evaluate.</param>
        /// <returns>A string representation of the result of evaluating the last given expression,
        /// or <see langword="null"/> if either no expressions were given or an exception was caught.</returns>
        /// <remarks>If this method returns <see langword="null"/>, call <see cref="ReadDiagnostics"/>
        /// to read the error message.</remarks>
        string? Evaluate(string expression);

        /// <summary>
        /// Returns any diagnostic messages that have been logged.
        /// </summary>
        /// <returns>A string containing the text of any diagnostic messages that have been issued, or an empty
        /// string if none have been issued.</returns>
        string ReadDiagnostics();

        /// <summary>
        /// Gets or sets a value indicating whether the REPL session allows quitting.
        /// </summary>
        /// <remarks>
        /// In some contexts (such as the ZILF Playground), quitting the REPL
        /// session is not allowed, since it would terminate the hosting application.
        /// </remarks>
        bool Quittable { get; set; }
    }

    public sealed class FrontEnd
    {
        readonly Dictionary<Context, EvaluatedFileState> evaluatedFileStates = [];

        public IFileSystem FileSystem { get; init; } = PhysicalFileSystem.Instance;
        public IDiagnosticLogger Logger { get; init; } = new DefaultDiagnosticLogger();

        internal event EventHandler<ContextEventArgs>? InitializeContext;

        public IList<string> IncludePaths { get; } = new List<string>();

        readonly record struct EvaluatedFileState(string Path, FileFlags Flags, ZilList? DefStructDefaults);

        sealed class ZapStreamFactory : IZapStreamFactory
        {
            readonly FrontEnd owner;

            readonly string mainFile;

            readonly string fwordsFile;

            readonly string dataFile;

            readonly string stringFile;

            readonly bool isNonstandardExtension;

            const string FrequentWordsSuffix1 = "_freq";
            const string FrequentWordsSuffix2 = "freq";
            const string DataSuffix = "_data";
            const string StringSuffix = "_str";

            /// <exception cref="ArgumentException">mainFile is not a file name.</exception>
            public ZapStreamFactory(FrontEnd owner, string mainFile)
            {
                this.owner = owner;
                this.mainFile = mainFile;

                var dir = Path.GetDirectoryName(mainFile) ?? throw new ArgumentException("Must be a file name", nameof(mainFile));
                var baseName = Path.GetFileNameWithoutExtension(mainFile);
                var ext = Path.GetExtension(mainFile);

                isNonstandardExtension = !ext.Equals(".zap", StringComparison.OrdinalIgnoreCase);

                fwordsFile = IdentifyFrequentWordsFilePath(dir, baseName, ext);
                dataFile = Path.Combine(dir, baseName + DataSuffix + ext);
                stringFile = Path.Combine(dir, baseName + StringSuffix + ext);
            }

            private string IdentifyFrequentWordsFilePath(string dir, string baseName, string ext)
            {
                return
                    Try(FrequentWordsSuffix1, ext, out string defaultPath) ??
                    Try(FrequentWordsSuffix2, ext, out _) ??
                    Try(FrequentWordsSuffix1, @".xzap", out _) ??
                    Try(FrequentWordsSuffix2, @".xzap", out _) ??
                    defaultPath;

                string? Try(string suffix, string myExt, out string path)
                {
                    path = Path.Combine(dir, baseName + suffix + myExt);
                    return owner.FileSystem.Exists(path) ? path : null;
                }
            }

            #region IZapStreamFactory Members

            public Stream CreateMainStream() => owner.FileSystem.OpenForWriting(mainFile);

            public Stream CreateFrequentWordsStream() => owner.FileSystem.OpenForWriting(fwordsFile);

            public Stream CreateDataStream() => owner.FileSystem.OpenForWriting(dataFile);

            public Stream CreateStringStream() => owner.FileSystem.OpenForWriting(stringFile);

            public string GetMainFileName(bool forceWithExt)
            {
                var result = mainFile;
                return forceWithExt ? result : Path.ChangeExtension(result, null);
            }

            public string GetDataFileName(bool forceWithExt)
            {
                var result = dataFile;
                if (forceWithExt)
                    return result;

                // When forceWithExt is false, include extension if it's not .zap
                return isNonstandardExtension ? result : Path.ChangeExtension(result, null);
            }

            public string GetFrequentWordsFileName(bool forceWithExt)
            {
                var result = fwordsFile;
                if (forceWithExt)
                    return result;

                // When forceWithExt is false, include extension if it's not .zap
                return isNonstandardExtension ? result : Path.ChangeExtension(result, null);
            }

            public string GetStringFileName(bool forceWithExt)
            {
                var result = stringFile;
                if (forceWithExt)
                    return result;

                // When forceWithExt is false, include extension if it's not .zap
                return isNonstandardExtension ? result : Path.ChangeExtension(result, null);
            }

            public bool FrequentWordsFileExists => owner.FileSystem.Exists(fwordsFile);

            #endregion
        }

        sealed class GlulxStreamFactory : IGlulxStreamFactory
        {
            readonly FrontEnd owner;
            readonly string mainFile;

            /// <exception cref="ArgumentException">mainFile is not a file name.</exception>
            public GlulxStreamFactory(FrontEnd owner, string mainFile)
            {
                this.owner = owner;
                this.mainFile = mainFile;

                var dir = Path.GetDirectoryName(mainFile);
                if (dir == null)
                    throw new ArgumentException("Must be a file name.", nameof(mainFile));
            }

            public Stream CreateMainStream() => owner.FileSystem.OpenForWriting(mainFile);

            public string GetMainFileName(bool withExt)
            {
                var result = mainFile;
                return withExt ? result : Path.ChangeExtension(result, null);
            }
        }

        sealed class CornerstoneStreamFactory : ICornerstoneStreamFactory
        {
            readonly FrontEnd owner;
            readonly string mainFile;

            public CornerstoneStreamFactory(FrontEnd owner, string mainFile)
            {
                this.owner = owner;
                this.mainFile = mainFile;

                var dir = Path.GetDirectoryName(mainFile);
                if (dir == null)
                    throw new ArgumentException("Must be a file name.", nameof(mainFile));
            }

            public Stream CreateMainStream() => owner.FileSystem.OpenForWriting(mainFile);

            public string GetMainFileName(bool withExt)
            {
                var result = mainFile;
                return withExt ? result : Path.ChangeExtension(result, null);
            }
        }

        Context NewContext(RunMode runMode, bool wantDebugInfo)
        {
            var ignoreCase = runMode == RunMode.Interactive;
            var result = new Context(ignoreCase) { RunMode = runMode, WantDebugInfo = wantDebugInfo };
            result.DiagnosticManager.Logger = Logger;

            InitializeContext?.Invoke(this, new ContextEventArgs(result));

            return result;
        }

        internal FrontEndResult Interpret(Context ctx, string inputFileName) =>
            InterpretOrCompile(ctx, inputFileName, null, false, false);

        public FrontEndResult Compile(string inputFileName, string outputFileName, bool wantDebugInfo = false)
        {
            var ctx = NewContext(RunMode.Compiler, wantDebugInfo);
            return Compile(ctx, inputFileName, outputFileName, ctx.WantDebugInfo);
        }

        internal FrontEndResult Compile(Context ctx, string inputFileName, string outputFileName, bool wantDebugInfo)
        {
            EvaluateSource(ctx, inputFileName);

            if (ctx.ErrorCount == 0)
            {
                EmitCompiledGame(ctx, outputFileName, wantDebugInfo);
            }

            return MakeResult(ctx);
        }

        internal FrontEndResult EvaluateSource(Context ctx, string inputFileName)
        {
            EvaluateInput(ctx, inputFileName);
            return MakeResult(ctx);
        }

        internal FrontEndResult EmitCompiledGame(Context ctx, string outputFileName, bool wantDebugInfo)
        {
            EmitCompilation(ctx, outputFileName, wantDebugInfo);
            return MakeResult(ctx);
        }

        FrontEndResult InterpretOrCompile(Context ctx, string inputFileName,
             string? outputFileName, bool wantCompile, bool wantDebugInfo)
        {
            Debug.Assert(!wantCompile || outputFileName != null);

            EvaluateInput(ctx, inputFileName);

            if (wantCompile && ctx.ErrorCount == 0)
            {
                Debug.Assert(outputFileName != null);
                EmitCompilation(ctx, outputFileName, wantDebugInfo);
            }

            return MakeResult(ctx);
        }

        void EvaluateInput(Context ctx, string inputFileName)
        {
            // open input file
            using var inputStream = FileSystem.OpenForReading(inputFileName);

            // evaluate source text
            using (ctx.PushFileContext(Path.GetFullPath(inputFileName)))
            {
                ctx.FileSystem = FileSystem;
                ctx.IncludePaths.AddRange(IncludePaths);
                try
                {
                    Program.Evaluate(ctx, inputStream, wantSyntaxWarnings: true);
                }
                catch (ZilErrorBase ex)
                {
                    ctx.HandleError(ex);
                }

                evaluatedFileStates[ctx] = new(ctx.CurrentFile.Path, ctx.CurrentFile.Flags, ctx.CurrentFile.DefStructDefaults);
            }
        }

        void EmitCompilation(Context ctx, string outputFileName, bool wantDebugInfo)
        {
            if (!evaluatedFileStates.TryGetValue(ctx, out var fileState))
            {
                throw new InvalidOperationException("Source must be evaluated before compilation.");
            }

            using (ctx.PushFileContext(fileState.Path))
            {
                ctx.CurrentFile.Flags = fileState.Flags;
                ctx.CurrentFile.DefStructDefaults = fileState.DefStructDefaults;

                ctx.RunHook("PRE-COMPILE");
                ctx.SetDefaultConstants();

                try
                {
                    var gameOptions = MakeGameOptions(ctx);

                    if (ctx.IsCornerstone)
                    {
                        var streamFactory = new CornerstoneStreamFactory(this, outputFileName);
                        using var gameBuilder = new Emit.Cornerstone.GameBuilder(streamFactory, (CornerstoneGameOptions)gameOptions);
                        Compilation.Compile(ctx, gameBuilder);
                    }
                    else if (ctx.IsGlulx)
                    {
                        var streamFactory = new GlulxStreamFactory(this, outputFileName);
                        using var gameBuilder = ctx.ZEnvironment.TargetPlatform == TargetPlatform.Glulx16
                            ? new Emit.Glulx.GameBuilder16(streamFactory, (GlulxGameOptions)gameOptions)
                            : new Emit.Glulx.GameBuilder(streamFactory, (GlulxGameOptions)gameOptions);
                        Compilation.Compile(ctx, gameBuilder);
                    }
                    else
                    {
                        var zversion = ctx.ZEnvironment.ZVersion;
                        var streamFactory = new ZapStreamFactory(this, outputFileName);

                        var builderOptions = wantDebugInfo ? GameBuilderOptions.WantDebugInfo : GameBuilderOptions.None;
                        if (ctx.OptimizationLevel < 2 && !ctx.OptimizeForSize)
                            builderOptions |= GameBuilderOptions.DisableRoutineIr;
                        if (!streamFactory.FrequentWordsFileExists)
                            builderOptions |= GameBuilderOptions.WantFrequentWords;

                        using var gameBuilder = new Emit.Zap.GameBuilder(zversion, streamFactory, builderOptions, (ZapGameOptions)gameOptions);
                        Compilation.Compile(ctx, gameBuilder);
                    }
                }
                catch (ZilErrorBase ex)     // catch fatals too
                {
                    ctx.HandleError(ex);
                }
                catch (NotSupportedException ex) when (ctx.IsCornerstone)
                {
                    ctx.HandleError(new CompilerError(CompilerMessages.Cornerstone_Backend_Limitation_0, ex.Message));
                }
            }
        }

        static FrontEndResult MakeResult(Context ctx)
        {
            return new(
                Success: ctx.ErrorCount == 0,
                ErrorCount: ctx.ErrorCount,
                WarningCount: ctx.WarningCount,
                SuppressedWarningCount: ctx.SuppressedWarningCount,
                Diagnostics: ctx.Diagnostics
            );
        }

        static IGameOptions MakeGameOptions(Context ctx)
        {
            var zenv = ctx.ZEnvironment;

            if (zenv.TargetPlatform == TargetPlatform.Glulx16)
            {
                return new GlulxGameOptions
                {
                    ZCompatibilityMode = true,
                    ZMachineVersion = zenv.ZVersion,
                    TimeStatusLine = zenv.TimeStatusLine,
                    DisableRoutineIr = ctx.OptimizationLevel < 2 && !ctx.OptimizeForSize,
                };
            }

            if (zenv.TargetPlatform == TargetPlatform.Glulx32 || zenv.ZVersion == ZEnvironment.GLULX_ZVERSION)
            {
                return new GlulxGameOptions
                {
                    DisableRoutineIr = ctx.OptimizationLevel < 2 && !ctx.OptimizeForSize
                };
            }

            if (zenv.TargetPlatform == TargetPlatform.Cornerstone)
            {
                return new CornerstoneGameOptions
                {
                    ZMachineVersion = zenv.ZVersion,
                    TimeStatusLine = zenv.TimeStatusLine,
                };
            }

            switch (zenv.ZVersion)
            {
                case 3:
                    return new ZapGameOptions.V3
                    {
                        TimeStatusLine = zenv.TimeStatusLine,
                        SoundEffects = ctx.GetGlobalOption(StdAtom.USE_SOUND_P) ||
                                       ctx.GetGlobalOption(StdAtom.SOUND_EFFECTS_P)
                    };

                case 4:
                    return new ZapGameOptions.V4
                    {
                        SoundEffects = ctx.GetGlobalOption(StdAtom.USE_SOUND_P) ||
                                       ctx.GetGlobalOption(StdAtom.SOUND_EFFECTS_P)
                    };

                case 5:
                case 7:
                case 8:
                    ZapGameOptions.V5Plus v5Plus = new ZapGameOptions.V5();
                    goto V5Plus;

                case 6:
                    v5Plus = new ZapGameOptions.V6 { Menus = ctx.GetGlobalOption(StdAtom.USE_MENUS_P) };

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

        public IReplSession StartRepl()
        {
            var ctx = NewContext(RunMode.Interactive, false);
            return new ReplSession(ctx);
        }

        private sealed class ReplSession : IReplSession, IDisposable
        {
            private readonly Context ctx;
            private readonly MemoryStream diagnostics;
            private readonly StreamWriter diagnosticsWriter;
            private bool disposedValue;

            public ReplSession(Context ctx)
            {
                this.ctx = ctx;

                diagnostics = new MemoryStream();
                diagnosticsWriter = new StreamWriter(diagnostics, leaveOpen: true);

                ctx.DiagnosticManager.Logger = new DefaultDiagnosticLogger { Writer = diagnosticsWriter };
            }

            public string? Evaluate(string expression)
            {
                using (ctx.PushFileContext("<REPL session>"))
                {
                    var result = Program.Evaluate(ctx, expression, wantExceptions: false);
                    return result?.ToStringContext(ctx, friendly: false);
                }
            }

            public string ReadDiagnostics()
            {
                string result;

                diagnosticsWriter.Flush();
                diagnostics.Position = 0;
                using (var rdr = new StreamReader(diagnostics, leaveOpen: true))
                {
                    result = rdr.ReadToEnd();
                }

                diagnostics.SetLength(0);
                return result;
            }

            public bool Quittable
            {
                get => ctx.Quittable;
                set => ctx.Quittable = value;
            }

            private void Dispose(bool disposing)
            {
                if (!disposedValue)
                {
                    if (disposing)
                    {
                        diagnostics.Dispose();
                        diagnosticsWriter.Dispose();
                    }

                    disposedValue = true;
                }
            }

            public void Dispose()
            {
                // Do not change this code. Put cleanup code in 'Dispose(bool disposing)' method
                Dispose(disposing: true);
                GC.SuppressFinalize(this);
            }
        }
    }
}
