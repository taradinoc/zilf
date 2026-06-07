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
using System.CommandLine;

namespace Zilf.Cli
{
    internal sealed class ZilfCommandSpecFactory
    {
        private readonly BuildCommandHandler buildCommandHandler;
        private readonly ReplCommandHandler replCommandHandler;
        private readonly ExecCommandHandler execCommandHandler;
        private readonly NewProjectCommandHandler newProjectCommandHandler;

        public ZilfCommandSpecFactory(
            BuildCommandHandler buildCommandHandler,
            ReplCommandHandler replCommandHandler,
            ExecCommandHandler execCommandHandler,
            NewProjectCommandHandler newProjectCommandHandler)
        {
            this.buildCommandHandler = buildCommandHandler;
            this.replCommandHandler = replCommandHandler;
            this.execCommandHandler = execCommandHandler;
            this.newProjectCommandHandler = newProjectCommandHandler;
        }

        public ZilfCommandSpec Create()
        {
            var root = new RootCommand("ZILF compiler and interpreter for ZIL (Zork Implementation Language).")
            {
                TreatUnmatchedTokensAsErrors = true
            };

            var buildCommand = new Command(
                "build",
                "(Default.) Compile ZIL source files into Z-machine assembly and optionally invoke ZAPF to produce a story file.");

            var buildInputArgument = new Argument<string?>("input")
            {
                Description = "Input ZIL source file. If not specified, looks for a .zil file matching the current directory name.",
                HelpName = "input.zil",
                Arity = ArgumentArity.ZeroOrOne
            };

            var buildOutputArgument = new Argument<string?>("output")
            {
                Description =
                    "Output file name. With -S, defaults to input name with .zap/.asm extension. Without -S, defaults to input name with .z#/.ulx extension.",
                Arity = ArgumentArity.ZeroOrOne,
                HelpName = "output.ext"
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

            var buildGlulx16Option = new Option<bool>("--glulx16")
            {
                Description = "Target Glulx with Z-machine-compatible 16-bit layout."
            };

            var buildCornerstoneOption = new Option<bool>("--cornerstone")
            {
                Description = "Target the Cornerstone MME VM (experimental)."
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

            var buildAllowFileWritesOption = new Option<bool>("--allow-file-writes")
            {
                Description = "Allow files to be opened for writing."
            };

            var buildStopAfterCompileOption = new Option<bool>("--stop-after-compile", "-S")
            {
                Description = "Stop after compilation; do not run ZAPF."
            };

            var buildZapfPassThroughOption = new Option<string[]>("--asm-options")
            {
                Description = "Pass comma-separated options through to the assembler. May be repeated.",
                AllowMultipleArgumentsPerToken = true,
                Arity = ArgumentArity.ZeroOrMore
            };

            var buildDefineFlagOption = new Option<string[]>("--define-flag", "-D")
            {
                Description = "Define compilation flag(s) (comma-separated) as T. May be repeated.",
                AllowMultipleArgumentsPerToken = false,
                Arity = ArgumentArity.OneOrMore
            };

            var buildIdeInfoOption = new Option<string?>("--ide-info")
            {
                Description = "Emit machine-readable workspace data to the provided file (or stdout). Implies --stop-after-compile.",
                Arity = ArgumentArity.ZeroOrOne
            };

            var buildPublishOption = new Option<bool>("--publish")
            {
                Description = "After a successful build, invoke ZilfPub to generate a publishable site."
            };

            var buildPublishOutputOption = new Option<string?>("--publish-output")
            {
                Description = "Output directory to pass to ZilfPub. Defaults to a 'publish' folder next to the story file.",
                Arity = ArgumentArity.ZeroOrOne
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
            buildCommand.Options.Add(buildGlulx16Option);
            buildCommand.Options.Add(buildCornerstoneOption);
            buildCommand.Options.Add(buildEnableAllWarningsOption);
            buildCommand.Options.Add(buildWarningsAsErrorsOption);
            buildCommand.Options.Add(buildSuppressWarningsOption);
            buildCommand.Options.Add(buildAllowFileWritesOption);
            buildCommand.Options.Add(buildStopAfterCompileOption);
            buildCommand.Options.Add(buildZapfPassThroughOption);
            buildCommand.Options.Add(buildIdeInfoOption);
            buildCommand.Options.Add(buildDefineFlagOption);
            buildCommand.Options.Add(buildPublishOption);
            buildCommand.Options.Add(buildPublishOutputOption);

            buildCommand.Validators.Add(commandResult =>
            {
                bool hasCaseSensitive = commandResult.GetResult(buildCaseSensitiveOption) is not null;
                bool hasCaseInsensitive = commandResult.GetResult(buildCaseInsensitiveOption) is not null;
                if (hasCaseSensitive && hasCaseInsensitive)
                {
                    commandResult.AddError("Options --case-sensitive and --case-insensitive cannot be used together.");
                }

                bool hasGlulx = commandResult.GetResult(buildGlulxOption) is not null;
                bool hasGlulx16 = commandResult.GetResult(buildGlulx16Option) is not null;
                bool hasCornerstone = commandResult.GetResult(buildCornerstoneOption) is not null;
                if ((hasGlulx && hasGlulx16) ||
                    (hasGlulx && hasCornerstone) ||
                    (hasGlulx16 && hasCornerstone))
                {
                    commandResult.AddError("Options --glulx, --glulx16, and --cornerstone are mutually exclusive.");
                }

                var wantsPublish = commandResult.GetResult(buildPublishOption) is not null;
                var stopAfterCompile = commandResult.GetResult(buildStopAfterCompileOption) is not null;
                var hasIdeInfo = commandResult.GetResult(buildIdeInfoOption) is not null;
                var hasPublishOutput = commandResult.GetResult(buildPublishOutputOption) is not null;

                if (wantsPublish && stopAfterCompile)
                {
                    commandResult.AddError("Options --publish and --stop-after-compile (-S) cannot be used together.");
                }

                if (wantsPublish && hasIdeInfo)
                {
                    commandResult.AddError(
                        "Option --publish cannot be used with --ide-info because --ide-info implies --stop-after-compile.");
                }

                if (hasPublishOutput && !wantsPublish)
                {
                    commandResult.AddError("Option --publish-output requires --publish.");
                }
            });

            root.Subcommands.Add(buildCommand);

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
            var replAllowFileWritesOption = new Option<bool>("--allow-file-writes")
            {
                Description = "Allow files to be opened for writing."
            };

            replCommand.Options.Add(replQuietOption);
            replCommand.Options.Add(replCaseSensitiveOption);
            replCommand.Options.Add(replCaseInsensitiveOption);
            replCommand.Options.Add(replIncludePathOption);
            replCommand.Options.Add(replAllowFileWritesOption);

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
            var execAllowFileWritesOption = new Option<bool>("--allow-file-writes")
            {
                Description = "Allow files to be opened for writing."
            };

            execCommand.Arguments.Add(execInputArgument);
            execCommand.Options.Add(execExprOption);
            execCommand.Options.Add(execQuietOption);
            execCommand.Options.Add(execCaseSensitiveOption);
            execCommand.Options.Add(execCaseInsensitiveOption);
            execCommand.Options.Add(execIncludePathOption);
            execCommand.Options.Add(execEnableAllWarningsOption);
            execCommand.Options.Add(execWarningsAsErrorsOption);
            execCommand.Options.Add(execSuppressWarningsOption);
            execCommand.Options.Add(execAllowFileWritesOption);

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

            var newCommand = new Command("new", "Create a new ZIL project from the empty sample template.");
            var newProjectArgument = new Argument<string>("name")
            {
                Description = "Name or path of the project directory to create.",
                HelpName = "project-name"
            };
            newCommand.Arguments.Add(newProjectArgument);
            root.Subcommands.Add(newCommand);

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
                buildGlulx16Option,
                buildCornerstoneOption,
                buildEnableAllWarningsOption,
                buildWarningsAsErrorsOption,
                buildSuppressWarningsOption,
                buildStopAfterCompileOption,
                buildZapfPassThroughOption,
                buildIdeInfoOption,
                buildDefineFlagOption,
                buildPublishOption,
                buildPublishOutputOption,
                buildAllowFileWritesOption,
                replCommand,
                replQuietOption,
                replCaseSensitiveOption,
                replCaseInsensitiveOption,
                replIncludePathOption,
                replAllowFileWritesOption,
                execCommand,
                execInputArgument,
                execExprOption,
                execQuietOption,
                execCaseSensitiveOption,
                execCaseInsensitiveOption,
                execIncludePathOption,
                execEnableAllWarningsOption,
                execWarningsAsErrorsOption,
                execSuppressWarningsOption,
                execAllowFileWritesOption);

            root.SetAction(_ =>
            {
                Console.Error.WriteLine("Error: A subcommand is required. Use 'zilf --help' for usage information.");
                return 1;
            });

            buildCommand.SetAction(parseResult =>
                buildCommandHandler.Execute(parseResult, spec, parseResult.GetValue(spec.BuildInputArgument)));
            replCommand.SetAction(parseResult => replCommandHandler.Execute(parseResult, spec));
            execCommand.SetAction(parseResult =>
                execCommandHandler.Execute(
                    parseResult,
                    spec,
                    parseResult.GetValue(spec.ExecInputArgument),
                    parseResult.GetValue(spec.ExecExprOption)));
            newCommand.SetAction(parseResult => newProjectCommandHandler.Execute(parseResult.GetValue(newProjectArgument)));

            return spec;
        }
    }
}