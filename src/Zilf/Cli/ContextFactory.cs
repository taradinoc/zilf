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
using Zilf.Interpreter;
using Zilf.Interpreter.Values;
using Zilf.ZModel;

namespace Zilf.Cli
{
    internal sealed class ContextFactory
    {
        private readonly IHostFileSystem hostFileSystem;

        public ContextFactory(IHostFileSystem hostFileSystem)
        {
            this.hostFileSystem = hostFileSystem;
        }

        public Context Create(ParseResult parseResult, ZilfCommandSpec spec, RunMode mode, string? inFile)
        {
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
            Option<bool> glulx16Option;
            Option<bool> cornerstoneOption;
            Option<string[]> defineFlagOption;
            Option<bool> allowFileWritesOption;

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
                glulx16Option = spec.BuildGlulx16Option;
                cornerstoneOption = spec.BuildCornerstoneOption;
                defineFlagOption = spec.BuildDefineFlagOption;
                allowFileWritesOption = spec.BuildAllowFileWritesOption;
            }
            else if (commandResult.Command == spec.ReplCommand)
            {
                quietOption = spec.ReplQuietOption;
                caseSensitiveOption = spec.ReplCaseSensitiveOption;
                caseInsensitiveOption = spec.ReplCaseInsensitiveOption;
                includePathOption = spec.ReplIncludePathOption;
                enableAllWarningsOption = default!;
                warningsAsErrorsOption = default!;
                suppressWarningsOption = default!;
                traceRoutinesOption = default!;
                debugInfoOption = default!;
                glulxOption = default!;
                glulx16Option = default!;
                cornerstoneOption = default!;
                defineFlagOption = default!;
                allowFileWritesOption = spec.ReplAllowFileWritesOption;
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
                traceRoutinesOption = default!;
                debugInfoOption = default!;
                glulxOption = default!;
                glulx16Option = default!;
                cornerstoneOption = default!;
                defineFlagOption = default!;
                allowFileWritesOption = spec.ExecAllowFileWritesOption;
            }
            else
            {
                throw new InvalidOperationException($"Unknown command: {commandResult.Command.Name}");
            }

            var quiet = parseResult.GetValue(quietOption);
            var hasCaseSensitive = parseResult.GetResult(caseSensitiveOption) is not null;
            var hasCaseInsensitive = parseResult.GetResult(caseInsensitiveOption) is not null;
            var allowFileWrites = parseResult.GetValue(allowFileWritesOption);

            var caseSensitive = hasCaseSensitive
                ? true
                : hasCaseInsensitive
                    ? false
                    : mode switch
                    {
                        RunMode.Expression or RunMode.Interactive => false,
                        _ => true,
                    };

            var traceRoutines = traceRoutinesOption != null && parseResult.GetValue(traceRoutinesOption);
            var debugInfo = debugInfoOption != null && parseResult.GetValue(debugInfoOption);
            var suppressNoisyWarnings = enableAllWarningsOption == null || !parseResult.GetValue(enableAllWarningsOption);
            var warningsAsErrors = warningsAsErrorsOption != null && parseResult.GetValue(warningsAsErrorsOption);

            var includePaths = parseResult.GetValue(includePathOption) ?? [];
            var suppressedCodes = suppressWarningsOption != null ? parseResult.GetValue(suppressWarningsOption) ?? [] : [];

            var ctx = new Context(!caseSensitive)
            {
                TraceRoutines = traceRoutines,
                WantDebugInfo = debugInfo,
                WarningsAsErrors = warningsAsErrors,
                SuppressNoisyWarnings = suppressNoisyWarnings,
                RunMode = mode,
                Quiet = quiet,
                AllowFileWrites = allowFileWrites
            };

            ctx.IncludePaths.AddRange(includePaths);
            PathResolution.AddImplicitIncludePaths(ctx.IncludePaths, inFile, mode, hostFileSystem);

            foreach (var codeList in suppressedCodes)
            {
                foreach (var code in codeList.Split(','))
                {
                    ctx.DiagnosticManager.Suppress(code.Trim());
                }
            }

            var useGlulx = glulxOption != null && parseResult.GetValue(glulxOption);
            var useGlulx16 = glulx16Option != null && parseResult.GetValue(glulx16Option);
            var useCornerstone = cornerstoneOption != null && parseResult.GetValue(cornerstoneOption);

            if ((useGlulx && useGlulx16) ||
                (useGlulx && useCornerstone) ||
                (useGlulx16 && useCornerstone))
            {
                throw new InvalidOperationException("Cannot select multiple backend targets.");
            }

            if (useGlulx)
            {
                ctx.SetTargetPlatform(TargetPlatform.Glulx32);
                ctx.SetZVersion(ZEnvironment.GLULX_ZVERSION);
            }
            else if (useGlulx16)
            {
                ctx.SetTargetPlatform(TargetPlatform.Glulx16);
                ctx.SetZVersion(ctx.ZEnvironment.ZVersion);
            }
            else if (useCornerstone)
            {
                ctx.SetTargetPlatform(TargetPlatform.Cornerstone);
                ctx.SetZVersion(ctx.ZEnvironment.ZVersion);
            }

            if (defineFlagOption != null)
            {
                var rawTokens = parseResult.GetValue(defineFlagOption) ?? [];
                foreach (var token in rawTokens)
                {
                    if (string.IsNullOrWhiteSpace(token))
                        continue;

                    foreach (var part in token.Split(',', StringSplitOptions.RemoveEmptyEntries))
                    {
                        var flagName = part.Trim();
                        if (flagName.Length == 0)
                            continue;

                        var atom = ZilAtom.Parse(flagName, ctx);
                        ctx.DefineCompilationFlag(atom, ctx.TRUE, true);
                    }
                }
            }

            return ctx;
        }
    }
}