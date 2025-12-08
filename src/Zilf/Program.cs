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
using System.Linq;
using System.Reflection;
using System.Text;
using System.CommandLine;
using System.CommandLine.Parsing;
using Zilf.Common;
using Zilf.Compiler;
using Zilf.Diagnostics;
using Zilf.Interpreter;
using Zilf.Interpreter.Values;
using Zilf.Language;
using Zilf.Language.Parsing;
using CommandParseResult = System.CommandLine.ParseResult;
using Zilf.ZModel;

namespace Zilf
{
    static class Program
    {
        static readonly Lazy<ZilfCommandSpec> CommandSpec = new(CreateCommandSpec);

        internal static string GetVersion() =>
            typeof(Program).Assembly.GetCustomAttribute<AssemblyInformationalVersionAttribute>()
                ?.InformationalVersion ?? "<?.??>";

        internal static string GetBanner() => $"ZILF {GetVersion()}";

        internal static int Main(string[] args)
        {
            // If no subcommand is specified and the first argument looks like a file or option,
            // assume "build" subcommand for backward compatibility
            args = PreprocessArgs(args);

            var spec = CommandSpec.Value;
            var parseResult = spec.RootCommand.Parse(args);
            return parseResult.Invoke();
        }

        static string[] PreprocessArgs(string[] args)
        {
            if (args.Length == 0)
                return args;

            var firstArg = args[0];

            // Check if it's a known subcommand
            if (firstArg is "build" or "repl" or "exec")
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

        static ZilfCommandSpec CreateCommandSpec()
        {
            var root = new RootCommand("ZILF compiler and interpreter for ZIL (Zork Implementation Language).")
            {
                TreatUnmatchedTokensAsErrors = true
            };

            // Build subcommand (also serves as default when no subcommand given)
            var buildCommand = new Command("build", "(Default.) Compile ZIL source files into Z-machine assembly and optionally invoke ZAPF to produce a story file.");

            var buildInputArgument = new Argument<string?>("input")
            {
                Description = "Input ZIL source file. If not specified, looks for a .zil file matching the current directory name.",
                HelpName = "input.zil",
                Arity = ArgumentArity.ZeroOrOne
            };

            var buildOutputArgument = new Argument<string?>("output")
            {
                Description = "Output ZAP file (defaults to input name with .zap extension).",
                Arity = ArgumentArity.ZeroOrOne,
                HelpName = "output.zap"
            };

            var buildQuietOption = new Option<bool>("--quiet", "-q")
            {
                Description = "Quiet mode: suppress banner and prompts."
            };

            var buildCaseSensitiveOption = new Option<bool?>("--case-sensitive")
            {
                Description = "Enable case-sensitive parsing.",
                Arity = ArgumentArity.Zero
            };
            buildCaseSensitiveOption.Aliases.Add("--cs");

            var buildCaseInsensitiveOption = new Option<bool?>("--case-insensitive")
            {
                Description = "Enable case-insensitive parsing.",
                Arity = ArgumentArity.Zero
            };
            buildCaseInsensitiveOption.Aliases.Add("--ci");

            var buildIncludePathOption = new Option<string[]>("--include-path", "-I")
            {
                Description = "Add directory to include path (may be repeated).",
                AllowMultipleArgumentsPerToken = false,
                Arity = ArgumentArity.ZeroOrMore
            };

            var buildTraceRoutinesOption = new Option<bool>("--trace", "-t")
            {
                Description = "Trace routine calls at runtime."
            };

            var buildDebugInfoOption = new Option<bool>("--debug", "-d")
            {
                Description = "Include debug information in output."
            };

            var buildGlulxOption = new Option<bool>("--glulx", "-g")
            {
                Description = "Target Glulx VM instead of Z-machine (experimental)."
            };

            var buildEnableAllWarningsOption = new Option<bool>("--warn-all", "-W")
            {
                Description = "Enable all warnings (even noisy ones)."
            };

            var buildWarningsAsErrorsOption = new Option<bool>("--warn-error", "-Werror")
            {
                Description = "Treat warnings as errors."
            };

            var buildSuppressWarningsOption = new Option<string[]>("--warn-suppress")
            {
                Description = "Suppress specific warning codes (comma-separated).",
                AllowMultipleArgumentsPerToken = true,
                Arity = ArgumentArity.ZeroOrMore
            };
            buildSuppressWarningsOption.Aliases.Add("-Wno");

            var buildStopAfterCompileOption = new Option<bool>("--stop-after-compile", "-S")
            {
                Description = "Stop after compilation; do not run ZAPF."
            };

            var buildZapfPassThroughOption = new Option<string[]>("--asm-options")
            {
                Description = "Pass comma-separated options through to the assembler (ZAPF). May be repeated.",
                AllowMultipleArgumentsPerToken = true,
                Arity = ArgumentArity.ZeroOrMore
            };

            buildCommand.Arguments.Add(buildInputArgument);
            buildCommand.Arguments.Add(buildOutputArgument);
            buildCommand.Options.Add(buildQuietOption);
            buildCommand.Options.Add(buildCaseSensitiveOption);
            buildCommand.Options.Add(buildCaseInsensitiveOption);
            buildCommand.Options.Add(buildIncludePathOption);
            buildCommand.Options.Add(buildTraceRoutinesOption);
            buildCommand.Options.Add(buildDebugInfoOption);
            buildCommand.Options.Add(buildGlulxOption);
            buildCommand.Options.Add(buildEnableAllWarningsOption);
            buildCommand.Options.Add(buildWarningsAsErrorsOption);
            buildCommand.Options.Add(buildSuppressWarningsOption);
            buildCommand.Options.Add(buildStopAfterCompileOption);
            buildCommand.Options.Add(buildZapfPassThroughOption);

            buildCommand.Validators.Add(commandResult =>
            {
                bool hasCaseSensitive = commandResult.GetResult(buildCaseSensitiveOption) is not null;
                bool hasCaseInsensitive = commandResult.GetResult(buildCaseInsensitiveOption) is not null;

                if (hasCaseSensitive && hasCaseInsensitive)
                {
                    commandResult.AddError("Options --case-sensitive and --case-insensitive cannot be used together.");
                }
            });

            root.Subcommands.Add(buildCommand);

            // REPL subcommand
            var replCommand = new Command("repl", "Start an interactive read-eval-print loop.");

            var replQuietOption = new Option<bool>("--quiet", "-q")
            {
                Description = "Quiet mode: suppress banner and prompts."
            };
            var replCaseSensitiveOption = new Option<bool?>("--case-sensitive")
            {
                Description = "Enable case-sensitive parsing.",
                Arity = ArgumentArity.Zero
            };
            replCaseSensitiveOption.Aliases.Add("--cs");
            var replCaseInsensitiveOption = new Option<bool?>("--case-insensitive")
            {
                Description = "Enable case-insensitive parsing.",
                Arity = ArgumentArity.Zero
            };
            replCaseInsensitiveOption.Aliases.Add("--ci");
            var replIncludePathOption = new Option<string[]>("--include-path", "-I")
            {
                Description = "Add directory to include path (may be repeated).",
                AllowMultipleArgumentsPerToken = false,
                Arity = ArgumentArity.ZeroOrMore
            };

            replCommand.Options.Add(replQuietOption);
            replCommand.Options.Add(replCaseSensitiveOption);
            replCommand.Options.Add(replCaseInsensitiveOption);
            replCommand.Options.Add(replIncludePathOption);

            replCommand.Validators.Add(commandResult =>
            {
                bool hasCaseSensitive = commandResult.GetResult(replCaseSensitiveOption) is not null;
                bool hasCaseInsensitive = commandResult.GetResult(replCaseInsensitiveOption) is not null;

                if (hasCaseSensitive && hasCaseInsensitive)
                {
                    commandResult.AddError("Options --case-sensitive and --case-insensitive cannot be used together.");
                }
            });

            root.Subcommands.Add(replCommand);

            // Exec subcommand
            var execCommand = new Command("exec", "Execute a ZIL file or expression without generating output.");

            var execInputArgument = new Argument<string?>("input")
            {
                Description = "Input ZIL source file to execute.",
                HelpName = "input.zil",
                Arity = ArgumentArity.ZeroOrOne
            };

            var execExprOption = new Option<string?>("--expr", "-e")
            {
                Description = "Evaluate a ZIL expression instead of executing a file.",
                Arity = ArgumentArity.ZeroOrOne
            };

            var execQuietOption = new Option<bool>("--quiet", "-q")
            {
                Description = "Quiet mode: suppress banner and prompts."
            };
            var execCaseSensitiveOption = new Option<bool?>("--case-sensitive")
            {
                Description = "Enable case-sensitive parsing.",
                Arity = ArgumentArity.Zero
            };
            execCaseSensitiveOption.Aliases.Add("--cs");
            var execCaseInsensitiveOption = new Option<bool?>("--case-insensitive")
            {
                Description = "Enable case-insensitive parsing.",
                Arity = ArgumentArity.Zero
            };
            execCaseInsensitiveOption.Aliases.Add("--ci");
            var execIncludePathOption = new Option<string[]>("--include-path", "-I")
            {
                Description = "Add directory to include path (may be repeated).",
                AllowMultipleArgumentsPerToken = false,
                Arity = ArgumentArity.ZeroOrMore
            };
            var execEnableAllWarningsOption = new Option<bool>("--warn-all", "-W")
            {
                Description = "Enable all warnings (even noisy ones)."
            };
            var execWarningsAsErrorsOption = new Option<bool>("--warn-error", "-Werror")
            {
                Description = "Treat warnings as errors."
            };
            var execSuppressWarningsOption = new Option<string[]>("--warn-suppress")
            {
                Description = "Suppress specific warning codes (comma-separated).",
                AllowMultipleArgumentsPerToken = true,
                Arity = ArgumentArity.ZeroOrMore
            };
            execSuppressWarningsOption.Aliases.Add("-Wno");

            execCommand.Arguments.Add(execInputArgument);
            execCommand.Options.Add(execExprOption);
            execCommand.Options.Add(execQuietOption);
            execCommand.Options.Add(execCaseSensitiveOption);
            execCommand.Options.Add(execCaseInsensitiveOption);
            execCommand.Options.Add(execIncludePathOption);
            execCommand.Options.Add(execEnableAllWarningsOption);
            execCommand.Options.Add(execWarningsAsErrorsOption);
            execCommand.Options.Add(execSuppressWarningsOption);

            execCommand.Validators.Add(commandResult =>
            {
                bool hasCaseSensitive = commandResult.GetResult(execCaseSensitiveOption) is not null;
                bool hasCaseInsensitive = commandResult.GetResult(execCaseInsensitiveOption) is not null;

                if (hasCaseSensitive && hasCaseInsensitive)
                {
                    commandResult.AddError("Options --case-sensitive and --case-insensitive cannot be used together.");
                }

                var inputFile = commandResult.GetValue(execInputArgument);
                var expression = commandResult.GetValue(execExprOption);

                if (string.IsNullOrEmpty(inputFile) && string.IsNullOrEmpty(expression))
                {
                    commandResult.AddError("Either an input file or an expression (--expr/-e) must be provided.");
                }
                else if (!string.IsNullOrEmpty(inputFile) && !string.IsNullOrEmpty(expression))
                {
                    commandResult.AddError("Cannot specify both an input file and an expression (--expr/-e).");
                }
            });

            root.Subcommands.Add(execCommand);

            var spec = new ZilfCommandSpec(
                root,
                buildCommand,
                buildInputArgument,
                buildOutputArgument,
                buildQuietOption,
                buildCaseSensitiveOption,
                buildCaseInsensitiveOption,
                buildIncludePathOption,
                buildTraceRoutinesOption,
                buildDebugInfoOption,
                buildGlulxOption,
                buildEnableAllWarningsOption,
                buildWarningsAsErrorsOption,
                buildSuppressWarningsOption,
                buildStopAfterCompileOption,
                buildZapfPassThroughOption,
                replCommand,
                replQuietOption,
                replCaseSensitiveOption,
                replCaseInsensitiveOption,
                replIncludePathOption,
                execCommand,
                execInputArgument,
                execExprOption,
                execQuietOption,
                execCaseSensitiveOption,
                execCaseInsensitiveOption,
                execIncludePathOption,
                execEnableAllWarningsOption,
                execWarningsAsErrorsOption,
                execSuppressWarningsOption);

            // Set up handler - root with no subcommand shows help
            root.SetAction(_ =>
            {
                Console.Error.WriteLine("Error: A subcommand is required. Use 'zilf --help' for usage information.");
                return 1;
            });

            buildCommand.SetAction(parseResult => 
            {
                var inputFile = parseResult.GetValue(spec.BuildInputArgument);
                
                // If no input file specified, try to find one based on current directory name
                if (string.IsNullOrEmpty(inputFile))
                {
                    var currentDir = Directory.GetCurrentDirectory();
                    var dirName = Path.GetFileName(currentDir);
                    var candidateFile = Path.Combine(currentDir, dirName + ".zil");
                    
                    if (File.Exists(candidateFile))
                    {
                        inputFile = candidateFile;
                    }
                    else
                    {
                        Console.Error.WriteLine($"Error: No input file specified and '{dirName}.zil' not found in current directory.");
                        return 1;
                    }
                }
                
                return ExecuteCompileMode(parseResult, inputFile);
            });
            replCommand.SetAction(ExecuteReplMode);
            execCommand.SetAction(parseResult => ExecuteExecMode(parseResult, parseResult.GetValue(spec.ExecInputArgument), parseResult.GetValue(spec.ExecExprOption)));

            return spec;
        }

        private sealed record class ZilfCommandSpec(
            RootCommand RootCommand,
            Command BuildCommand,
            Argument<string?> BuildInputArgument,
            Argument<string?> BuildOutputArgument,
            Option<bool> BuildQuietOption,
            Option<bool?> BuildCaseSensitiveOption,
            Option<bool?> BuildCaseInsensitiveOption,
            Option<string[]> BuildIncludePathOption,
            Option<bool> BuildTraceRoutinesOption,
            Option<bool> BuildDebugInfoOption,
            Option<bool> BuildGlulxOption,
            Option<bool> BuildEnableAllWarningsOption,
            Option<bool> BuildWarningsAsErrorsOption,
            Option<string[]> BuildSuppressWarningsOption,
            Option<bool> BuildStopAfterCompileOption,
            Option<string[]> BuildZapfPassThroughOption,
            Command ReplCommand,
            Option<bool> ReplQuietOption,
            Option<bool?> ReplCaseSensitiveOption,
            Option<bool?> ReplCaseInsensitiveOption,
            Option<string[]> ReplIncludePathOption,
            Command ExecCommand,
            Argument<string?> ExecInputArgument,
            Option<string?> ExecExprOption,
            Option<bool> ExecQuietOption,
            Option<bool?> ExecCaseSensitiveOption,
            Option<bool?> ExecCaseInsensitiveOption,
            Option<string[]> ExecIncludePathOption,
            Option<bool> ExecEnableAllWarningsOption,
            Option<bool> ExecWarningsAsErrorsOption,
            Option<string[]> ExecSuppressWarningsOption);

        static int ExecuteCompileMode(CommandParseResult parseResult, string? inputFile)
        {
            if (string.IsNullOrEmpty(inputFile))
            {
                Console.Error.WriteLine("Error: Input file required for compile mode.");
                return 1;
            }

            var spec = CommandSpec.Value;
            var ctx = BuildContextFromParseResult(parseResult, RunMode.Compiler, inputFile);

            if (ctx == null)
                return 1;

            if (!ctx.Quiet)
            {
                Console.Write(GetBanner());
                Console.Write(" built ");
                Console.WriteLine(GetBuildTimestamp());
            }

            // Determine which argument/option set to use based on command
            Argument<string?> outputArgument = spec.BuildOutputArgument;
            Option<bool> stopAfterCompileOption = spec.BuildStopAfterCompileOption;
            Option<string[]> zapfPassThroughOption = spec.BuildZapfPassThroughOption;

            // TODO: reorder front-end processing so <VERSION GLULX> can affect the output file extension
            var output = parseResult.GetValue(outputArgument);
            var outFile = string.IsNullOrEmpty(output) ? Path.ChangeExtension(inputFile, ctx.IsGlulx ? ".asm" : ".zap") : output;

            // Perform compilation, then optionally invoke ZAPF
            var frontEnd = new FrontEnd();
            FrontEndResult result;
            try
            {
                result = frontEnd.Compile(ctx, inputFile, outFile, ctx.WantDebugInfo);
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

            if (result.WarningCount > 0)
            {
                Console.Error.Write("{0} warning{1}",
                    result.WarningCount,
                    result.WarningCount == 1 ? "" : "s");

                if (result.SuppressedWarningCount > 0)
                {
                    Console.Error.Write(
                        " ({0} suppressed)",
                        result.SuppressedWarningCount);
                }

                Console.Error.WriteLine();
            }

            if (result.ErrorCount > 0)
            {
                Console.Error.WriteLine("{0} error{1}",
                    result.ErrorCount,
                    result.ErrorCount == 1 ? "" : "s");
                return 2;
            }

            // If requested, stop after compile
            var stopAfter = parseResult.GetValue(stopAfterCompileOption);

            if (stopAfter)
            {
                return 0;
            }

            // Prepare assembler arguments (used by both ZAPF and Glazer)
            var asmArgsRaw = parseResult.GetValue(zapfPassThroughOption) ?? Array.Empty<string>();
            var asmArgsExpanded = new List<string>();
            foreach (var token in asmArgsRaw)
            {
                if (string.IsNullOrWhiteSpace(token)) continue;
                foreach (var part in token.Split(',', StringSplitOptions.RemoveEmptyEntries))
                    asmArgsExpanded.Add(part.Trim());
            }

            // Propagate quiet to assembler if the user asked for quiet
            if (ctx.Quiet && !asmArgsExpanded.Any(a => a is "-q" or "--quiet"))
                asmArgsExpanded.Insert(0, "-q");

            if (ctx.IsGlulx)
            {
                // Invoke Glazer for Glulx
                var glazerExe = FindGlazerExecutable();
                if (glazerExe == null)
                {
                    Console.Error.WriteLine("Glazer not found next to ZILF (looked for glazer, Glazer, glazer.exe). Use -S to skip assembly.");
                    return 1;
                }

                var exit = RunGlazerProcess(glazerExe, outFile!, asmArgsExpanded);
                return exit;
            }
            else
            {
                // Invoke Zapf for Z-machine
                var zapfExe = FindZapfExecutable();
                if (zapfExe == null)
                {
                    Console.Error.WriteLine("ZAPF not found next to ZILF (looked for zapf, Zapf, zapf.exe). Use -S to skip assembly.");
                    return 1;
                }

                var exit = RunZapfProcess(zapfExe, outFile!, asmArgsExpanded);
                return exit;
            }
        }

        private static string? FindZapfExecutable()
        {
            var baseDir = AppContext.BaseDirectory;
            var candidates = new[] { "zapf", "Zapf", "zapf.exe", "Zapf.exe" };
            foreach (var name in candidates)
            {
                var full = Path.Combine(baseDir, name);
                if (File.Exists(full))
                    return full;
            }
            return null;
        }

        private static string? FindGlazerExecutable()
        {
            var baseDir = AppContext.BaseDirectory;
            var candidates = new[] { "glazer", "Glazer", "glazer.exe", "Glazer.exe" };
            foreach (var name in candidates)
            {
                var full = Path.Combine(baseDir, name);
                if (File.Exists(full))
                    return full;
            }
            return null;
        }

        private static int RunZapfProcess(string zapfPath, string zapInputPath, List<string> extraArgs)
        {
            using var proc = new Process();
            proc.StartInfo = new ProcessStartInfo
            {
                FileName = zapfPath,
                UseShellExecute = false,
                RedirectStandardOutput = false,
                RedirectStandardError = false,
                CreateNoWindow = false,
            };

            // Arguments: [extraArgs...] <input.zap>
            foreach (var a in extraArgs)
                proc.StartInfo.ArgumentList.Add(a);
            proc.StartInfo.ArgumentList.Add(zapInputPath);

            try
            {
                proc.Start();
                proc.WaitForExit();
                return proc.ExitCode;
            }
            catch (System.ComponentModel.Win32Exception ex)
            {
                Console.Error.WriteLine($"Failed to launch ZAPF: {ex.Message}");
                return 1;
            }
            catch (FileNotFoundException ex)
            {
                Console.Error.WriteLine($"ZAPF not found: {ex.FileName}");
                return 1;
            }
            catch (InvalidOperationException ex)
            {
                Console.Error.WriteLine($"Failed to run ZAPF: {ex.Message}");
                return 1;
            }
        }

        private static int RunGlazerProcess(string glazerPath, string asmInputPath, List<string> extraArgs)
        {
            using var proc = new Process();
            proc.StartInfo = new ProcessStartInfo
            {
                FileName = glazerPath,
                UseShellExecute = false,
                RedirectStandardOutput = false,
                RedirectStandardError = false,
                CreateNoWindow = false,
            };

            // Arguments: [extraArgs...] <input.asm>
            foreach (var a in extraArgs)
                proc.StartInfo.ArgumentList.Add(a);
            proc.StartInfo.ArgumentList.Add(asmInputPath);

            try
            {
                proc.Start();
                proc.WaitForExit();
                return proc.ExitCode;
            }
            catch (System.ComponentModel.Win32Exception ex)
            {
                Console.Error.WriteLine($"Failed to launch Glazer: {ex.Message}");
                return 1;
            }
            catch (FileNotFoundException ex)
            {
                Console.Error.WriteLine($"Glazer not found: {ex.FileName}");
                return 1;
            }
            catch (InvalidOperationException ex)
            {
                Console.Error.WriteLine($"Failed to run Glazer: {ex.Message}");
                return 1;
            }
        }

        static int ExecuteReplMode(CommandParseResult parseResult)
        {
            var ctx = BuildContextFromParseResult(parseResult, RunMode.Interactive, null);

            if (ctx == null)
                return 1;

            if (!ctx.Quiet)
            {
                Console.Write(GetBanner());
                Console.Write(" built ");
                Console.WriteLine(GetBuildTimestamp());
            }

            DoREPL(ctx);
            return 0;
        }

        static int ExecuteExecMode(CommandParseResult parseResult, string? inputFile, string? expression)
        {
            // Determine mode based on what was provided
            if (!string.IsNullOrEmpty(expression))
            {
                // Expression mode
                var ctx = BuildContextFromParseResult(parseResult, RunMode.Expression, expression);

                if (ctx == null)
                    return 1;

                if (!ctx.Quiet)
                {
                    Console.Write(GetBanner());
                    Console.Write(" built ");
                    Console.WriteLine(GetBuildTimestamp());
                }

                using (ctx.PushFileContext("<cmdline>"))
                {
                    Console.WriteLine(Evaluate(ctx, expression));
                    if (ctx.ErrorCount > 0)
                        return 2;
                }

                return 0;
            }
            else
            {
                // File mode
                var ctx = BuildContextFromParseResult(parseResult, RunMode.Interpreter, inputFile);

                if (ctx == null)
                    return 1;

                if (!ctx.Quiet)
                {
                    Console.Write(GetBanner());
                    Console.Write(" built ");
                    Console.WriteLine(GetBuildTimestamp());
                }

                return WrapInFrontEnd(frontEnd => frontEnd.Interpret(ctx, inputFile!));
            }
        }

        static int WrapInFrontEnd(Func<FrontEnd, FrontEndResult> func)
        {
            var frontEnd = new FrontEnd();
            try
            {
                var result = func(frontEnd);

                if (result.WarningCount > 0)
                {
                    Console.Error.Write("{0} warning{1}",
                        result.WarningCount,
                        result.WarningCount == 1 ? "" : "s");

                    if (result.SuppressedWarningCount > 0)
                    {
                        Console.Error.Write(
                            " ({0} suppressed)",
                            result.SuppressedWarningCount);
                    }

                    Console.Error.WriteLine();
                }

                if (result.ErrorCount > 0)
                {
                    Console.Error.WriteLine("{0} error{1}",
                        result.ErrorCount,
                        result.ErrorCount == 1 ? "" : "s");
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

        static Context? BuildContextFromParseResult(CommandParseResult parseResult, RunMode mode, string? inFile)
        {
            var spec = CommandSpec.Value;

            // Determine which set of options to use based on the command
            Option<bool> quietOption;
            Option<bool?> caseSensitiveOption;
            Option<bool?> caseInsensitiveOption;
            Option<string[]> includePathOption;
            Option<bool> enableAllWarningsOption;
            Option<bool> warningsAsErrorsOption;
            Option<string[]> suppressWarningsOption;
            Option<bool> traceRoutinesOption;
            Option<bool> debugInfoOption;
            Option<bool> glulxOption;

            var commandResult = parseResult.CommandResult;
            if (commandResult.Command == spec.BuildCommand)
            {
                quietOption = spec.BuildQuietOption;
                caseSensitiveOption = spec.BuildCaseSensitiveOption;
                caseInsensitiveOption = spec.BuildCaseInsensitiveOption;
                includePathOption = spec.BuildIncludePathOption;
                enableAllWarningsOption = spec.BuildEnableAllWarningsOption;
                warningsAsErrorsOption = spec.BuildWarningsAsErrorsOption;
                suppressWarningsOption = spec.BuildSuppressWarningsOption;
                traceRoutinesOption = spec.BuildTraceRoutinesOption;
                debugInfoOption = spec.BuildDebugInfoOption;
                glulxOption = spec.BuildGlulxOption;
            }
            else if (commandResult.Command == spec.ReplCommand)
            {
                quietOption = spec.ReplQuietOption;
                caseSensitiveOption = spec.ReplCaseSensitiveOption;
                caseInsensitiveOption = spec.ReplCaseInsensitiveOption;
                includePathOption = spec.ReplIncludePathOption;
                enableAllWarningsOption = default!; // Not available in REPL
                warningsAsErrorsOption = default!; // Not available in REPL
                suppressWarningsOption = default!; // Not available in REPL
                traceRoutinesOption = default!; // Not available in REPL
                debugInfoOption = default!; // Not available in REPL
                glulxOption = default!; // Not available in REPL
            }
            else if (commandResult.Command == spec.ExecCommand)
            {
                quietOption = spec.ExecQuietOption;
                caseSensitiveOption = spec.ExecCaseSensitiveOption;
                caseInsensitiveOption = spec.ExecCaseInsensitiveOption;
                includePathOption = spec.ExecIncludePathOption;
                enableAllWarningsOption = spec.ExecEnableAllWarningsOption;
                warningsAsErrorsOption = spec.ExecWarningsAsErrorsOption;
                suppressWarningsOption = spec.ExecSuppressWarningsOption;
                traceRoutinesOption = default!; // Not available in Exec
                debugInfoOption = default!; // Not available in Exec
                glulxOption = default!; // Not available in Exec
            }
            else
            {
                throw new InvalidOperationException($"Unknown command: {commandResult.Command.Name}");
            }

            var quiet = parseResult.GetValue(quietOption);
            var hasCaseSensitive = parseResult.GetResult(caseSensitiveOption) is not null;
            var hasCaseInsensitive = parseResult.GetResult(caseInsensitiveOption) is not null;

            bool caseSensitive;
            if (hasCaseSensitive)
            {
                caseSensitive = true;
            }
            else if (hasCaseInsensitive)
            {
                caseSensitive = false;
            }
            else
            {
                // Default based on mode
                caseSensitive = mode switch
                {
                    RunMode.Expression or RunMode.Interactive => false,
                    _ => true,
                };
            }

            var traceRoutines = traceRoutinesOption != null ? parseResult.GetValue(traceRoutinesOption) : false;
            var debugInfo = debugInfoOption != null ? parseResult.GetValue(debugInfoOption) : false;
            var suppressNoisyWarnings = enableAllWarningsOption != null ? !parseResult.GetValue(enableAllWarningsOption) : true;
            var warningsAsErrors = warningsAsErrorsOption != null ? parseResult.GetValue(warningsAsErrorsOption) : false;

            var includePaths = parseResult.GetValue(includePathOption) ?? [];
            var suppressedCodes = suppressWarningsOption != null ? parseResult.GetValue(suppressWarningsOption) ?? [] : [];

            var ctx = new Context(!caseSensitive)
            {
                TraceRoutines = traceRoutines,
                WantDebugInfo = debugInfo,
                WarningsAsErrors = warningsAsErrors,
                SuppressNoisyWarnings = suppressNoisyWarnings,
                RunMode = mode,
                Quiet = quiet
            };

            ctx.IncludePaths.AddRange(includePaths);
            AddImplicitIncludePaths(ctx.IncludePaths, inFile, mode);

            foreach (var codeList in suppressedCodes)
            {
                foreach (var code in codeList.Split(','))
                {
                    ctx.DiagnosticManager.Suppress(code.Trim());
                }
            }

            var useGlulx = glulxOption != null && parseResult.GetValue(glulxOption);
            if (useGlulx)
            {
                ctx.SetZVersion(ZEnvironment.GLULX_ZVERSION);
            }

            return ctx;
        }

        private static DateTime GetBuildTimestamp()
        {
            var ts = BuildInfo.BuildTimestampUtc;
            return ts.ToLocalTime();
        }

        [SuppressMessage("Microsoft.Design", "CA1031:DoNotCatchGeneralExceptionTypes",
            Justification = "This is a top-level loop that reports unhandled exceptions to the user.")]
        static void DoREPL(Context ctx)
        {
            using (ctx.PushFileContext("<stdin>"))
            using (new Completer(ctx).Attach())
            {
                ReadLine.HistoryEnabled = true;

                var sb = new StringBuilder();

                int angles = 0, rounds = 0, squares = 0, quotes = 0;

                while (true)
                {
                    try
                    {
                        var line = ReadLine.Read(sb.Length == 0 ? "> " : ">> ");

                        if (line == null)
                            break;

                        if (sb.Length > 0)
                            sb.AppendLine();
                        sb.Append(line);

                        int state = (quotes > 0) ? 1 : 0;
                        foreach (char c in line)
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
                            var result = Evaluate(ctx, sb.ToString());
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
                    // ReSharper disable once CatchAllClause
                    catch (Exception ex)
                    {
                        Console.WriteLine(ex);
                        sb.Length = 0;
                    }
                }
            }
        }

        static void AddImplicitIncludePaths(List<string> includePaths, string? inFile, RunMode mode)
        {
            if (inFile != null && mode != RunMode.Expression && Path.GetDirectoryName(Path.GetFullPath(inFile)) is string dir)
            {
                includePaths.Insert(0, dir);
            }

            if (includePaths.Count == 0)
            {
                includePaths.Add(Environment.CurrentDirectory);
            }

            // look for a "library" directory somewhere near zilf.exe
            var strippables = new HashSet<string> { "bin", "debug", "release", "zilf", "src" };
            string[] libraryDirNames = { "Library", "library", "lib", "zillib" };

            var zilfDir = Path.GetDirectoryName(System.AppContext.BaseDirectory);
            Debug.Assert(zilfDir != null);

            while (true)
            {
                bool found = false;

                foreach (var n in libraryDirNames)
                {
                    var candidate = Path.Combine(zilfDir, n);

                    if (Directory.Exists(candidate))
                    {
                        found = true;
                        foreach (var path in RecursiveLibraryIncludePaths(candidate))
                            includePaths.Add(path);
                        break;
                    }
                }

                if (!found)
                {
                    zilfDir += Path.DirectorySeparatorChar;
                    var pos = strippables.Max(strippable =>
                        zilfDir.LastIndexOf(Path.DirectorySeparatorChar + strippable + Path.DirectorySeparatorChar,
                            StringComparison.InvariantCultureIgnoreCase));

                    if (pos >= 0)
                    {
                        // remove part after split point and keep looking
                        zilfDir = zilfDir[..pos];

                        if (!string.IsNullOrEmpty(zilfDir))
                            continue;
                    }
                }

                break;
            }

            static IEnumerable<string> RecursiveLibraryIncludePaths(string parent)
            {
                var first = Enumerable.Repeat(parent, 1);

                static bool Excluded(string name) => name[0] is '.' or '_' || name.ToUpperInvariant() is "TEST" or "TESTS";

                var rest = from subdir in Directory.EnumerateDirectories(parent)
                           let name = Path.GetFileName(subdir)
                           where !Excluded(name)
                           from result in RecursiveLibraryIncludePaths(subdir)
                           select result;

                return first.Concat(rest);
            }
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
                            src ?? new FileSourceLine(ctx.CurrentFile.Path, parser.Line),
                            InterpreterMessages.Syntax_Error_0, po.Exception.Message);

                    case ParserOutputType.Terminator:
                        throw new InterpreterError(
                            src ?? new FileSourceLine(ctx.CurrentFile.Path, parser.Line),
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