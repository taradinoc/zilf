/* Copyright 2010-2025 Tara McGrew
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
using System.Diagnostics.CodeAnalysis;
using System.IO;
using System.Linq;
using System.Reflection;
using System.CommandLine;
using Zilf.Cli;
using Zilf.Common;
using Zilf.Diagnostics;
using Zilf.Interpreter;
using Zilf.Interpreter.Values;
using Zilf.Language;
using Zilf.Language.Parsing;

namespace Zilf
{
    static class Program
    {
        static readonly Lazy<ZilfCliServices> Services = new(CreateServices);
        static readonly Lazy<ZilfCommandSpec> CommandSpec = new(Services.Value.CommandSpecFactory.Create);

        internal static string GetVersion() =>
            typeof(Program).Assembly.GetCustomAttribute<AssemblyInformationalVersionAttribute>()
                ?.InformationalVersion ?? "<?.??>";

        internal static string GetBanner() => $"ZILF {GetVersion()}";

        internal static int Main(string[] args)
        {
            args = PreprocessArgs(args);

            var spec = CommandSpec.Value;
            var parseResult = spec.RootCommand.Parse(args);
            return parseResult.Invoke();
        }

        static ZilfCliServices CreateServices()
        {
            IHostFileSystem hostFileSystem = new HostFileSystem();
            IFrontEndFactory frontEndFactory = new FrontEndFactory();
            IExternalToolService externalToolService = new ExternalToolService();
            var contextFactory = new ContextFactory(hostFileSystem);
            var projectTemplateService = new ProjectTemplateService(hostFileSystem);
            var buildCommandHandler = new BuildCommandHandler(
                contextFactory,
                frontEndFactory,
                externalToolService,
                hostFileSystem);
            var replCommandHandler = new ReplCommandHandler(contextFactory);
            var execCommandHandler = new ExecCommandHandler(contextFactory, frontEndFactory);
            var newProjectCommandHandler = new NewProjectCommandHandler(projectTemplateService);
            var commandSpecFactory = new ZilfCommandSpecFactory(
                buildCommandHandler,
                replCommandHandler,
                execCommandHandler,
                newProjectCommandHandler);

            return new(
                hostFileSystem,
                frontEndFactory,
                externalToolService,
                contextFactory,
                projectTemplateService,
                buildCommandHandler,
                replCommandHandler,
                execCommandHandler,
                newProjectCommandHandler,
                commandSpecFactory);
        }

        static string[] PreprocessArgs(string[] args)
        {
            if (args.Length == 0)
                return args;

            var firstArg = args[0];

            // Alias "publish" to "build --publish"
            if (firstArg == "publish")
            {
                var newArgs = new string[args.Length + 1];
                newArgs[0] = "build";
                newArgs[1] = "--publish";
                Array.Copy(args, 1, newArgs, 2, args.Length - 1);
                return newArgs;
            }

            // Check if it's a known subcommand
            if (firstArg is "build" or "repl" or "exec" or "new")
                return args;

            // Check if it's a help or version flag
            if (firstArg is "-?" or "-h" or "--help" or "--version")
                return args;

            // Check if -e or --expr is present, route to exec
            if (args.Any(a => a is "-e" or "--expr"))
            {
                var newArgs = new string[args.Length + 1];
                newArgs[0] = "exec";
                Array.Copy(args, 0, newArgs, 1, args.Length);
                return newArgs;
            }

            // Otherwise, assume it's meant for the build command
            // Insert "build" at the beginning
            var newArgs2 = new string[args.Length + 1];
            newArgs2[0] = "build";
            Array.Copy(args, 0, newArgs2, 1, args.Length);
            return newArgs2;
        }

        internal sealed record CompileOutputPaths(string IntermediateFile, string? FinalAssemblerOutput);

        internal static CompileOutputPaths ResolveCompileOutputPaths(
            string inputFile,
            string? output,
            bool stopAfter,
            bool isGlulx)
        {
            return PathResolution.ResolveCompileOutputPaths(inputFile, output, stopAfter, isGlulx);
        }

        internal static string ResolveFinalStoryOutputPath(string assemblerInputFile, string? output, bool isGlulx, int zVersion)
        {
            return PathResolution.ResolveFinalStoryOutputPath(assemblerInputFile, output, isGlulx, zVersion);
        }

        internal static string ResolveBlorbOutputPath(string intermediateFile, string? output, bool stopAfter, bool isGlulx, int zVersion)
        {
            return PathResolution.ResolveBlorbOutputPath(intermediateFile, output, stopAfter, isGlulx, zVersion);
        }

        internal static string ResolvePublishOutputPath(string storyFilePath, string? publishOutputOption)
        {
            return PathResolution.ResolvePublishOutputPath(storyFilePath, publishOutputOption);
        }

        internal static string ResolvePublishProjectName(Context ctx, string inputFile)
        {
            return PathResolution.ResolvePublishProjectName(ctx, inputFile);
        }

        internal static string? GetBannerFirstLine(Context ctx, string? banner)
        {
            return PathResolution.GetBannerFirstLine(ctx, banner);
        }

        internal static Context BuildContextFromParseResult(ParseResult parseResult, RunMode mode, string? inFile)
        {
            return Services.Value.ContextFactory.Create(parseResult, CommandSpec.Value, mode, inFile);
        }

        internal static DateTime GetBuildTimestamp()
        {
            var ts = BuildInfo.BuildTimestampUtc;
            return ts.ToLocalTime();
        }

        internal static string GetProgramDirectory()
        {
            return PathResolution.GetProgramDirectory();
        }

        internal static string? FindNearbyDirectory(string startDir, IEnumerable<string> directoryNames)
        {
            return PathResolution.FindNearbyDirectory(startDir, directoryNames, Services.Value.HostFileSystem);
        }

        // TODO: move Parse somewhere more sensible
        /// <exception cref="InterpreterError">Syntax error.</exception>
        public static IEnumerable<ZilObject> Parse(Context ctx, IEnumerable<char> chars)
        {
            return Parse(ctx, null, chars, null);
        }

        /// <exception cref="InterpreterError">Syntax error.</exception>
        public static IEnumerable<ZilObject> Parse(Context ctx, IEnumerable<char> chars, params ZilObject[] templateParams)
        {
            return Parse(ctx, null, chars, templateParams);
        }

        /// <exception cref="InterpreterError">Syntax error.</exception>
        public static IEnumerable<ZilObject> Parse(Context ctx, ISourceLine? src, IEnumerable<char> chars, params ZilObject[]? templateParams)
        {
            var parser = new Parser(ctx, src, templateParams);

            foreach (var po in parser.Parse(chars))
            {
                if (po.IsIgnorable)
                    continue;

                switch (po.Type)
                {
                    case ParserOutputType.Object:
                        yield return po.Object;
                        break;

                    case ParserOutputType.EndOfInput:
                        yield break;

                    case ParserOutputType.SyntaxError:
                        throw new InterpreterError(
                            src ?? new FileSourceSpan(ctx.CurrentFile.Path, parser.Line, parser.Column, parser.Line, parser.Column),
                            InterpreterMessages.Syntax_Error_0, po.Exception.Message);

                    case ParserOutputType.Terminator:
                        throw new InterpreterError(
                            src ?? new FileSourceSpan(ctx.CurrentFile.Path, parser.Line, parser.Column, parser.Line, parser.Column),
                            InterpreterMessages.Syntax_Error_0, "misplaced terminator");

                    default:
                        throw new UnhandledCaseException("parser output type");
                }
            }
        }

        static IEnumerable<char> ReadAllChars(Stream stream)
        {
            using var rdr = new StreamReader(stream);

            int c;
            while ((c = rdr.Read()) >= 0)
            {
                yield return (char)c;
            }
        }

        // ReSharper disable once UnusedMethodReturnValue.Global
        public static ZilObject? Evaluate(Context ctx, Stream stream, bool wantExceptions = false)
        {
            return Evaluate(ctx, ReadAllChars(stream), wantExceptions);
        }

        /// <summary>
        /// Evaluates some code in a <see cref="Context"/>.
        /// </summary>
        /// <param name="ctx">The context in which to evaluate.</param>
        /// <param name="chars">The code to evaluate.</param>
        /// <param name="wantExceptions"><see langword="true"/> if the method should be allowed to throw
        /// <see cref="InterpreterError"/>, or <see langword="false"/> to catch it.</param>
        /// <returns>The result of evaluating the last object in the code; or <see langword="null"/> if either the code contained
        /// no objects, or <paramref name="wantExceptions"/> was <see langword="false"/> and an <see cref="InterpreterError"/> was caught.</returns>
        [SuppressMessage("Microsoft.Usage", "CA2202:Do not dispose objects multiple times")]
        public static ZilObject? Evaluate(Context ctx, IEnumerable<char> chars, bool wantExceptions = false)
        {
            try
            {
                var ztree = Parse(ctx, chars);

                ZilObject? result = null;
                bool first = true;
                foreach (var node in ztree)
                {
                    try
                    {
                        using (DiagnosticContext.Push(node.SourceLine))
                        {
                            if (first)
                            {
                                // V4 games can identify themselves this way instead of using <VERSION EZIP>
                                if (node is ZilString str &&
                                    str.Text.StartsWith("EXTENDED", StringComparison.Ordinal) &&
                                    ctx.ZEnvironment.ZVersion == 3)
                                {
                                    ctx.SetZVersion(4);
                                }

                                first = false;
                            }
                            result = (ZilObject)node.Eval(ctx);
                        }
                    }
                    catch (InterpreterError ex) when (wantExceptions == false)
                    {
                        ctx.HandleError(ex);
                    }
                }

                return result;
            }
            catch (InterpreterError ex) when (wantExceptions == false)
            {
                ctx.HandleError(ex);
                return null;
            }
        }
    }
}