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

using System.CommandLine;

namespace Zilf.Cli
{
    internal sealed record class ZilfCommandSpec(
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
        Option<bool> BuildGlulx16Option,
        Option<bool> BuildEnableAllWarningsOption,
        Option<bool> BuildWarningsAsErrorsOption,
        Option<string[]> BuildSuppressWarningsOption,
        Option<bool> BuildStopAfterCompileOption,
        Option<string[]> BuildZapfPassThroughOption,
        Option<string?> BuildIdeInfoOption,
        Option<string[]> BuildDefineFlagOption,
        Option<bool> BuildPublishOption,
        Option<string?> BuildPublishOutputOption,
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
}