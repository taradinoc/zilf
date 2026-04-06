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
using System.Globalization;
using System.IO;
using System.Linq;
using Zilf.Interpreter;
using Zilf.Interpreter.Values;
using Zilf.Language;
using Zilf.ZModel;
using Zilf.ZModel.Values;

namespace Zilf.Cli
{
    internal static class PathResolution
    {
        public static Program.CompileOutputPaths ResolveCompileOutputPaths(
            string inputFile,
            string? output,
            bool stopAfter,
            TargetPlatform targetPlatform)
        {
            var intermediateExtension = GetIntermediateExtension(targetPlatform);

            if (string.IsNullOrEmpty(output))
            {
                return new(Path.ChangeExtension(inputFile, intermediateExtension), null);
            }

            var outputExt = Path.GetExtension(output);
            var isIntermediateExtension = outputExt.Equals(".zap", StringComparison.OrdinalIgnoreCase) ||
                                          outputExt.Equals(".asm", StringComparison.OrdinalIgnoreCase) ||
                                          outputExt.Equals(".cas", StringComparison.OrdinalIgnoreCase);

            if (stopAfter || isIntermediateExtension)
            {
                return new(output, null);
            }

            return new(Path.ChangeExtension(inputFile, intermediateExtension), output);
        }

        public static Program.CompileOutputPaths ResolveCompileOutputPaths(
            string inputFile,
            string? output,
            bool stopAfter,
            bool isGlulx)
        {
            return ResolveCompileOutputPaths(
                inputFile,
                output,
                stopAfter,
                isGlulx ? TargetPlatform.Glulx32 : TargetPlatform.ZMachine);
        }

        public static string ResolveFinalStoryOutputPath(
            string assemblerInputFile,
            string? output,
            TargetPlatform targetPlatform,
            int zVersion)
        {
            if (!string.IsNullOrEmpty(output))
                return output;

            return targetPlatform switch
            {
                TargetPlatform.Glulx32 or TargetPlatform.Glulx16 => Path.ChangeExtension(assemblerInputFile, ".ulx"),
                TargetPlatform.Cornerstone => Path.ChangeExtension(assemblerInputFile, ".mme"),
                _ => Path.ChangeExtension(assemblerInputFile, $".z{zVersion}"),
            };
        }

        public static string ResolveFinalStoryOutputPath(string assemblerInputFile, string? output, bool isGlulx, int zVersion)
        {
            return ResolveFinalStoryOutputPath(
                assemblerInputFile,
                output,
                isGlulx ? TargetPlatform.Glulx32 : TargetPlatform.ZMachine,
                zVersion);
        }

        public static string ResolveBlorbOutputPath(
            string intermediateFile,
            string? output,
            bool stopAfter,
            TargetPlatform targetPlatform,
            int zVersion)
        {
            var basePath = stopAfter
                ? intermediateFile
                : ResolveFinalStoryOutputPath(intermediateFile, output, targetPlatform, zVersion);

            return Path.ChangeExtension(
                basePath,
                stopAfter ? ".blorb" : targetPlatform is TargetPlatform.Glulx32 or TargetPlatform.Glulx16 ? ".gblorb" : ".zblorb");
        }

            public static string ResolveBlorbOutputPath(string intermediateFile, string? output, bool stopAfter, bool isGlulx, int zVersion)
            {
                return ResolveBlorbOutputPath(
                intermediateFile,
                output,
                stopAfter,
                isGlulx ? TargetPlatform.Glulx32 : TargetPlatform.ZMachine,
                zVersion);
            }

        private static string GetIntermediateExtension(TargetPlatform targetPlatform) => targetPlatform switch
        {
            TargetPlatform.Glulx32 or TargetPlatform.Glulx16 => ".asm",
            TargetPlatform.Cornerstone => ".cas",
            _ => ".zap",
        };

        public static string ResolvePublishOutputPath(string storyFilePath, string? publishOutputOption)
        {
            if (!string.IsNullOrWhiteSpace(publishOutputOption))
                return publishOutputOption;

            var storyDirectory = Path.GetDirectoryName(Path.GetFullPath(storyFilePath)) ?? Environment.CurrentDirectory;
            return Path.Combine(storyDirectory, "publish");
        }

        public static string ResolvePublishProjectName(Context ctx, string inputFile)
        {
            var publishTitle = TryGetGlobalString(ctx, StdAtom.PUBLISH_TITLE);
            if (!string.IsNullOrWhiteSpace(publishTitle))
                return publishTitle;

            var gameTitle = TryGetGlobalString(ctx, StdAtom.GAME_TITLE);
            if (!string.IsNullOrWhiteSpace(gameTitle))
                return gameTitle;

            var bannerValue = ctx.GetGlobalVal(ctx.GetStdAtom(StdAtom.GAME_BANNER));
            var bannerText = bannerValue is ZilString bannerString ? bannerString.Text : null;
            var bannerFirstLine = GetBannerFirstLine(ctx, bannerText);
            if (!string.IsNullOrWhiteSpace(bannerFirstLine))
                return bannerFirstLine;

            var baseName = Path.GetFileNameWithoutExtension(inputFile);
            if (string.IsNullOrWhiteSpace(baseName))
                return "Story";

            return char.ToUpper(baseName[0], CultureInfo.InvariantCulture) + baseName[1..];
        }

        public static string? GetBannerFirstLine(Context ctx, string? banner)
        {
            if (string.IsNullOrWhiteSpace(banner))
                return null;

            var crlfChar = (ctx.GetGlobalVal(ctx.GetStdAtom(StdAtom.CRLF_CHARACTER)) as ZilChar)?.Char ?? '|';
            var splitChars = new[] { '|', '\r', '\n', crlfChar };
            var firstLine = banner.Split(splitChars, StringSplitOptions.None)[0].Trim();
            return firstLine.Length > 0 ? firstLine : null;
        }

        public static void AddImplicitIncludePaths(
            List<string> includePaths,
            string? inFile,
            RunMode mode,
            IHostFileSystem hostFileSystem)
        {
            if (inFile != null && mode != RunMode.Expression && Path.GetDirectoryName(Path.GetFullPath(inFile)) is string dir)
            {
                includePaths.Insert(0, dir);
            }

            if (includePaths.Count == 0)
            {
                includePaths.Add(Environment.CurrentDirectory);
            }

            string[] libraryDirNames = ["Library", "library", "lib", "zillib"];

            var libraryDir = FindNearbyDirectory(GetProgramDirectory(), libraryDirNames, hostFileSystem);
            if (libraryDir != null)
            {
                foreach (var path in RecursiveLibraryIncludePaths(libraryDir, hostFileSystem))
                    includePaths.Add(path);
            }
        }

        public static string GetProgramDirectory()
        {
            return Path.GetDirectoryName(AppContext.BaseDirectory) ?? AppContext.BaseDirectory;
        }

        public static string? FindNearbyDirectory(string startDir, IEnumerable<string> directoryNames, IHostFileSystem hostFileSystem)
        {
            var strippables = new HashSet<string>(StringComparer.InvariantCultureIgnoreCase)
            {
                "bin",
                "debug",
                "release",
                "zilf",
                "src"
            };

            var currentDir = startDir;

            while (!string.IsNullOrEmpty(currentDir))
            {
                foreach (var directoryName in directoryNames)
                {
                    var candidate = Path.Combine(currentDir, directoryName);
                    if (hostFileSystem.DirectoryExists(candidate))
                        return candidate;
                }

                currentDir += Path.DirectorySeparatorChar;
                var pos = strippables.Max(strippable =>
                    currentDir.LastIndexOf(Path.DirectorySeparatorChar + strippable + Path.DirectorySeparatorChar,
                        StringComparison.InvariantCultureIgnoreCase));

                if (pos < 0)
                    break;

                currentDir = currentDir[..pos];
            }

            return null;
        }

        private static IEnumerable<string> RecursiveLibraryIncludePaths(string parent, IHostFileSystem hostFileSystem)
        {
            yield return parent;

            foreach (var subdir in hostFileSystem.EnumerateDirectories(parent))
            {
                var name = Path.GetFileName(subdir);
                if (Excluded(name))
                    continue;

                foreach (var result in RecursiveLibraryIncludePaths(subdir, hostFileSystem))
                    yield return result;
            }

            static bool Excluded(string name) =>
                name[0] is '.' or '_' || name.ToUpperInvariant() is "TEST" or "TESTS";
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
    }
}