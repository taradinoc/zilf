/* Copyright 2010-2026 Tara McGrew
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
using System.CommandLine;
using System.Diagnostics.CodeAnalysis;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.Json;
using Zilf.Compiler;
using Zilf.Ide;
using Zilf.Interpreter;
using Zilf.Interpreter.Values;
using Zilf.Language;
using Zilf.ZModel.Values;

namespace Zilf.Cli
{
    internal sealed class BuildCommandHandler
    {
        private readonly ContextFactory contextFactory;
        private readonly IFrontEndFactory frontEndFactory;
        private readonly IExternalToolService externalToolService;
        private readonly IHostFileSystem hostFileSystem;

        public BuildCommandHandler(
            ContextFactory contextFactory,
            IFrontEndFactory frontEndFactory,
            IExternalToolService externalToolService,
            IHostFileSystem hostFileSystem)
        {
            this.contextFactory = contextFactory;
            this.frontEndFactory = frontEndFactory;
            this.externalToolService = externalToolService;
            this.hostFileSystem = hostFileSystem;
        }

        public int Execute(ParseResult parseResult, ZilfCommandSpec spec, string? inputFile)
        {
            inputFile = ResolveInputFile(inputFile);
            if (string.IsNullOrEmpty(inputFile))
                return 1;

            var ctx = contextFactory.Create(parseResult, spec, RunMode.Compiler, inputFile);
            var hasIdeInfo = parseResult.GetResult(spec.BuildIdeInfoOption) is not null;
            var ideInfoPath = parseResult.GetValue(spec.BuildIdeInfoOption);

            if (!ctx.Quiet && !hasIdeInfo)
            {
                Console.Write(Program.GetBanner());
                Console.Write(" built ");
                Console.WriteLine(Program.GetBuildTimestamp());
            }

            var output = parseResult.GetValue(spec.BuildOutputArgument);
            var stopAfter = parseResult.GetValue(spec.BuildStopAfterCompileOption);
            var publish = parseResult.GetValue(spec.BuildPublishOption);
            var publishOutput = parseResult.GetValue(spec.BuildPublishOutputOption);

            if (hasIdeInfo)
                stopAfter = true;

            var frontEnd = frontEndFactory.Create();
            FrontEndResult result;
            try
            {
                result = frontEnd.EvaluateSource(ctx, inputFile);
            }
            catch (FileNotFoundException ex)
            {
                Console.Error.WriteLine("file not found: " + ex.FileName);
                return 1;
            }
            catch (IOException ex)
            {
                Console.Error.WriteLine("I/O error: " + ex.Message);
                return 1;
            }

            string? finalAssemblerOutput = null;
            string? outFile = null;

            if (result.ErrorCount == 0)
            {
                var outputPaths = PathResolution.ResolveCompileOutputPaths(inputFile, output, stopAfter, ctx.IsGlulx);
                outFile = outputPaths.IntermediateFile;
                finalAssemblerOutput = outputPaths.FinalAssemblerOutput;

                try
                {
                    result = frontEnd.EmitCompiledGame(ctx, outFile, ctx.WantDebugInfo);
                }
                catch (IOException ex)
                {
                    Console.Error.WriteLine("I/O error: " + ex.Message);
                    return 1;
                }
            }

            if (hasIdeInfo)
            {
                var report = IdeInfoReport.Build(ctx, inputFile);
                var json = report.ToJsonString(new JsonSerializerOptions
                {
                    WriteIndented = false
                });

                if (string.IsNullOrEmpty(ideInfoPath) || ideInfoPath == "-")
                {
                    Console.Out.WriteLine(json);
                }
                else
                {
                    hostFileSystem.WriteAllText(ideInfoPath, json, Encoding.UTF8);
                }
            }

            WriteResultSummary(result);
            if (result.ErrorCount > 0)
                return 2;

            if (stopAfter)
            {
                if (!ctx.Blorb.IsEmpty)
                {
                    var blorbFile = PathResolution.ResolveBlorbOutputPath(
                        outFile!,
                        finalAssemblerOutput,
                        stopAfter: true,
                        ctx.IsGlulx,
                        ctx.ZEnvironment.ZVersion);
                    using var stream = hostFileSystem.OpenWrite(blorbFile);
                    ctx.Blorb.WriteTo(stream);
                }

                return 0;
            }

            var asmArgsExpanded = ExpandAssemblerArgs(parseResult.GetValue(spec.BuildZapfPassThroughOption));
            if (ctx.Quiet && !asmArgsExpanded.Any(a => a is "-q" or "--quiet"))
                asmArgsExpanded.Insert(0, "-q");

            if (ctx.IsGlulx)
            {
                var glazerExe = externalToolService.FindGlazerExecutable();
                if (glazerExe == null)
                {
                    Console.Error.WriteLine(
                        "Glazer not found next to ZILF (looked for glazer, Glazer, glazer.exe). Use -S to skip assembly.");
                    return 1;
                }

                var exit = externalToolService.RunGlazerProcess(glazerExe, outFile!, asmArgsExpanded, finalAssemblerOutput);
                if (exit != 0)
                    return exit;
            }
            else
            {
                var zapfExe = externalToolService.FindZapfExecutable();
                if (zapfExe == null)
                {
                    Console.Error.WriteLine(
                        "ZAPF not found next to ZILF (looked for zapf, Zapf, zapf.exe). Use -S to skip assembly.");
                    return 1;
                }

                var exit = externalToolService.RunZapfProcess(zapfExe, outFile!, asmArgsExpanded, finalAssemblerOutput);
                if (exit != 0)
                    return exit;
            }

            if (!ctx.Blorb.IsEmpty)
            {
                var storyFile = PathResolution.ResolveFinalStoryOutputPath(
                    outFile!,
                    finalAssemblerOutput,
                    ctx.IsGlulx,
                    ctx.ZEnvironment.ZVersion);
                if (!hostFileSystem.FileExists(storyFile))
                {
                    Console.Error.WriteLine($"Assembled story file not found: {storyFile}");
                    return 1;
                }

                ctx.Blorb.AddExecutable(hostFileSystem.ReadAllBytes(storyFile), ctx.IsGlulx);

                var blorbFile = PathResolution.ResolveBlorbOutputPath(
                    outFile!,
                    finalAssemblerOutput,
                    stopAfter: false,
                    ctx.IsGlulx,
                    ctx.ZEnvironment.ZVersion);
                using var stream = hostFileSystem.OpenWrite(blorbFile);
                ctx.Blorb.WriteTo(stream);

                Console.WriteLine($"Created {blorbFile}");
            }

            if (publish)
            {
                var storyFile = PathResolution.ResolveFinalStoryOutputPath(
                    outFile!,
                    finalAssemblerOutput,
                    ctx.IsGlulx,
                    ctx.ZEnvironment.ZVersion);
                var publishInputFile = !ctx.Blorb.IsEmpty
                    ? PathResolution.ResolveBlorbOutputPath(
                        outFile!,
                        finalAssemblerOutput,
                        stopAfter: false,
                        ctx.IsGlulx,
                        ctx.ZEnvironment.ZVersion)
                    : storyFile;

                if (!hostFileSystem.FileExists(publishInputFile))
                {
                    Console.Error.WriteLine($"Publish input file not found: {publishInputFile}");
                    return 1;
                }

                var zilfPubExe = externalToolService.FindZilfPubExecutable();
                if (zilfPubExe == null)
                {
                    Console.Error.WriteLine(
                        "ZilfPub not found next to ZILF (looked for zilfpub, ZilfPub, zilfpub.exe). Use build without --publish.");
                    return 1;
                }

                var publishArgs = BuildZilfPubArguments(ctx, inputFile, publishInputFile, publishOutput);
                var exit = externalToolService.RunZilfPubProcess(zilfPubExe, publishArgs);
                if (exit != 0)
                    return exit;
            }

            return 0;
        }

        private static List<string> BuildZilfPubArguments(
            Context ctx,
            string inputFile,
            string publishInputFile,
            string? publishOutputOption)
        {
            var args = new List<string>
            {
                publishInputFile,
                "--overwrite",
                "--output-dir",
                PathResolution.ResolvePublishOutputPath(publishInputFile, publishOutputOption),
                "--project-name",
                PathResolution.ResolvePublishProjectName(ctx, inputFile)
            };

            AddOptionIfPresent(args, "--author-name", TryGetGlobalString(ctx, StdAtom.PUBLISH_AUTHOR));
            AddOptionIfPresent(args, "--cover-art", TryGetGlobalString(ctx, StdAtom.PUBLISH_COVER_ART));
            AddOptionIfPresent(args, "--description", TryGetGlobalString(ctx, StdAtom.PUBLISH_DESCRIPTION));
            AddOptionIfPresent(args, "--theme", TryGetGlobalString(ctx, StdAtom.PUBLISH_THEME));

            if (ctx.GetGlobalVal(ctx.GetStdAtom(StdAtom.PUBLISH_EXTRAS)) is ZilList extras)
            {
                foreach (var item in extras)
                {
                    if (item is ZilList lst && lst.First is ZilString filename &&
                        lst.Rest?.First is ZilString caption && lst.Rest.Rest?.IsEmpty == true)
                    {
                        args.Add("--extra");
                        args.Add($"{filename.Text}={caption.Text}");
                    }
                }
            }

            if (ctx.GetGlobalVal(ctx.GetStdAtom(StdAtom.PUBLISH_SOURCE_P))?.IsTrue == true)
            {
                var sourceDir = Path.GetDirectoryName(Path.GetFullPath(inputFile));
                if (!string.IsNullOrWhiteSpace(sourceDir))
                {
                    args.Add("--source-dir");
                    args.Add(sourceDir);
                }
            }

            return args;
        }

        private static void AddOptionIfPresent(List<string> args, string optionName, string? value)
        {
            if (string.IsNullOrWhiteSpace(value))
                return;

            args.Add(optionName);
            args.Add(value);
        }

        private static string? TryGetGlobalString(Context ctx, StdAtom stdAtom)
        {
            var value = ctx.GetGlobalVal(ctx.GetStdAtom(stdAtom));

            return value switch
            {
                ZilString zstr => zstr.Text,
                ZilChar zchar => zchar.Char.ToString(),
                ZilAtom atom => atom.Text,
                ZilConstant { Value: ZilString zstr } => zstr.Text,
                ZilConstant { Value: ZilChar zchar } => zchar.Char.ToString(),
                ZilConstant { Value: ZilAtom atom } => atom.Text,
                _ => null
            };
        }

        private string? ResolveInputFile(string? inputFile)
        {
            if (!string.IsNullOrEmpty(inputFile))
                return inputFile;

            var currentDir = hostFileSystem.GetCurrentDirectory();
            var dirName = Path.GetFileName(currentDir);
            var candidateFile = Path.Combine(currentDir, dirName + ".zil");
            if (hostFileSystem.FileExists(candidateFile))
                return candidateFile;

            Console.Error.WriteLine($"Error: No input file specified and '{dirName}.zil' not found in current directory.");
            return null;
        }

        private static List<string> ExpandAssemblerArgs(string[]? asmArgsRaw)
        {
            var asmArgsExpanded = new List<string>();
            foreach (var token in asmArgsRaw ?? Array.Empty<string>())
            {
                if (string.IsNullOrWhiteSpace(token))
                    continue;

                foreach (var part in token.Split(',', StringSplitOptions.RemoveEmptyEntries))
                    asmArgsExpanded.Add(part.Trim());
            }

            return asmArgsExpanded;
        }

        private static void WriteResultSummary(FrontEndResult result)
        {
            if (result.WarningCount > 0)
            {
                Console.Error.Write("{0} warning{1}", result.WarningCount, result.WarningCount == 1 ? string.Empty : "s");
                if (result.SuppressedWarningCount > 0)
                    Console.Error.Write(" ({0} suppressed)", result.SuppressedWarningCount);
                Console.Error.WriteLine();
            }

            if (result.ErrorCount > 0)
            {
                Console.Error.WriteLine("{0} error{1}", result.ErrorCount, result.ErrorCount == 1 ? string.Empty : "s");
            }
        }
    }

    internal sealed class ExecCommandHandler
    {
        private readonly ContextFactory contextFactory;
        private readonly IFrontEndFactory frontEndFactory;

        public ExecCommandHandler(ContextFactory contextFactory, IFrontEndFactory frontEndFactory)
        {
            this.contextFactory = contextFactory;
            this.frontEndFactory = frontEndFactory;
        }

        public int Execute(ParseResult parseResult, ZilfCommandSpec spec, string? inputFile, string? expression)
        {
            if (!string.IsNullOrEmpty(expression))
            {
                var ctx = contextFactory.Create(parseResult, spec, RunMode.Expression, expression);
                WriteBanner(ctx);

                using (ctx.PushFileContext("<cmdline>"))
                {
                    Console.WriteLine(Program.Evaluate(ctx, expression));
                    return ctx.ErrorCount > 0 ? 2 : 0;
                }
            }

            var fileContext = contextFactory.Create(parseResult, spec, RunMode.Interpreter, inputFile);
            WriteBanner(fileContext);

            return WrapInFrontEnd(frontEnd => frontEnd.Interpret(fileContext, inputFile!));
        }

        private int WrapInFrontEnd(Func<FrontEnd, FrontEndResult> func)
        {
            var frontEnd = frontEndFactory.Create();
            try
            {
                var result = func(frontEnd);

                if (result.WarningCount > 0)
                {
                    Console.Error.Write("{0} warning{1}", result.WarningCount, result.WarningCount == 1 ? string.Empty : "s");
                    if (result.SuppressedWarningCount > 0)
                        Console.Error.Write(" ({0} suppressed)", result.SuppressedWarningCount);
                    Console.Error.WriteLine();
                }

                if (result.ErrorCount > 0)
                {
                    Console.Error.WriteLine("{0} error{1}", result.ErrorCount, result.ErrorCount == 1 ? string.Empty : "s");
                    return 2;
                }
            }
            catch (FileNotFoundException ex)
            {
                Console.Error.WriteLine("file not found: " + ex.FileName);
                return 1;
            }
            catch (IOException ex)
            {
                Console.Error.WriteLine("I/O error: " + ex.Message);
                return 1;
            }

            return 0;
        }

        private static void WriteBanner(Context ctx)
        {
            if (ctx.Quiet)
                return;

            Console.Write(Program.GetBanner());
            Console.Write(" built ");
            Console.WriteLine(Program.GetBuildTimestamp());
        }
    }

    internal sealed class ReplCommandHandler
    {
        private readonly ContextFactory contextFactory;

        public ReplCommandHandler(ContextFactory contextFactory)
        {
            this.contextFactory = contextFactory;
        }

        public int Execute(ParseResult parseResult, ZilfCommandSpec spec)
        {
            var ctx = contextFactory.Create(parseResult, spec, RunMode.Interactive, null);

            if (!ctx.Quiet)
            {
                Console.Write(Program.GetBanner());
                Console.Write(" built ");
                Console.WriteLine(Program.GetBuildTimestamp());
            }

            DoRepl(ctx);
            return 0;
        }

        [SuppressMessage("Microsoft.Design", "CA1031:DoNotCatchGeneralExceptionTypes",
            Justification = "This is a top-level loop that reports unhandled exceptions to the user.")]
        private static void DoRepl(Context ctx)
        {
            using (ctx.PushFileContext("<stdin>"))
            using (new Completer(ctx).Attach())
            {
                ReadLine.HistoryEnabled = true;

                var sb = new StringBuilder();
                int angles = 0;
                int rounds = 0;
                int squares = 0;
                int quotes = 0;

                while (true)
                {
                    try
                    {
                        bool continuing = sb.Length > 0;
                        var line = ReadLine.Read(continuing ? ">> " : "> ");
                        if (line == null)
                            break;

                        if (continuing)
                        {
                            if (line == ".")
                            {
                                // abort input
                                sb.Clear();
                                angles = rounds = squares = quotes = 0;
                                Console.WriteLine("Input aborted.");
                                continue;
                            }

                            sb.AppendLine();
                        }
                        else
                        {
                            if (line.Equals("help", StringComparison.OrdinalIgnoreCase) ||
                                line.Equals("exit", StringComparison.OrdinalIgnoreCase) ||
                                line.Equals("quit", StringComparison.OrdinalIgnoreCase))
                            {
                                Console.WriteLine("Type '<QUIT>' to exit.");
                                continue;
                            }
                        }

                        sb.Append(line);

                        int state = quotes > 0 ? 1 : 0;
                        foreach (var c in line)
                        {
                            switch (state)
                            {
                                case 0:
                                    switch (c)
                                    {
                                        case '<': angles++; break;
                                        case '>': angles--; break;
                                        case '(': rounds++; break;
                                        case ')': rounds--; break;
                                        case '[': squares++; break;
                                        case ']': squares--; break;
                                        case '"': quotes++; state = 1; break;
                                    }
                                    break;

                                case 1:
                                    switch (c)
                                    {
                                        case '"': quotes--; state = 0; break;
                                        case '\\': state = 2; break;
                                    }
                                    break;

                                case 2:
                                    state = 1;
                                    break;
                            }
                        }

                        if (angles == 0 && rounds == 0 && squares == 0 && quotes == 0)
                        {
                            var result = Program.Evaluate(ctx, sb.ToString());
                            if (result != null)
                            {
                                try
                                {
                                    Console.WriteLine(result.ToStringContext(ctx, false));
                                }
                                catch (InterpreterError ex)
                                {
                                    ctx.HandleError(ex);
                                }
                            }

                            sb.Length = 0;
                        }
                    }
                    catch (Exception ex)
                    {
                        Console.WriteLine(ex);
                        sb.Length = 0;
                    }
                }
            }
        }
    }

    internal sealed class NewProjectCommandHandler
    {
        private readonly ProjectTemplateService projectTemplateService;

        public NewProjectCommandHandler(ProjectTemplateService projectTemplateService)
        {
            this.projectTemplateService = projectTemplateService;
        }

        public int Execute(string? projectPath)
        {
            if (string.IsNullOrWhiteSpace(projectPath))
            {
                Console.Error.WriteLine("Error: Project name required for new mode.");
                return 1;
            }

            try
            {
                var outputFile = projectTemplateService.CreateProject(projectPath);
                Console.WriteLine($"Created {outputFile}");
                return 0;
            }
            catch (InvalidOperationException ex)
            {
                Console.Error.WriteLine(ex.Message);
                return 1;
            }
        }
    }
}