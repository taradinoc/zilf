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
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.IO;
using System.Runtime.InteropServices;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using FyreVM;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Zilf.Common;
using Zilf.Compiler;
using Zilf.Diagnostics;
using Zilf.Interpreter.Values;
using Zilf.Language;
using Zilf.ZModel;

namespace Zilf.Tests.Integration
{
    sealed partial class FyreHelper(string code, string? input)
    {
        public static async Task RunAndAssertAsync(string code, string? input, string expectedOutput,
            IEnumerable<(Predicate<ZlrHelperRunResult>, string message)>? warningChecks = null,
            bool wantCompileOutput = false)
        {
            var helper = new FyreHelper(code, input);
            bool compiled;
            string compileOutput;
            if (wantCompileOutput)
            {
                compiled = helper.Compile(out compileOutput);
            }
            else
            {
                compiled = helper.Compile();
                compileOutput = string.Empty;
            }
            Assert.IsTrue(compiled, "Failed to compile");
            Assert.IsTrue(helper.Assemble(), "Failed to assemble");
            if (warningChecks != null)
            {
                var result = new ZlrHelperRunResult
                {
                    Diagnostics = helper.Diagnostics,
                    ErrorCount = helper.ErrorCount,
                    WarningCount = helper.WarningCount,
                    Status = ZlrTestStatus.Finished,
                    SuppressedWarningCount = helper.SuppressedWarningCount,
                };
                foreach (var (check, message) in warningChecks)
                    if (!check(result))
                        Assert.Fail(message);
            }
            string actualOutput = compileOutput + await helper.ExecuteAsync();
            Assert.AreEqual(expectedOutput, actualOutput, "Actual output differs from expected");
        }

        public static async Task<ZlrHelperRunResult> RunAsync(string code, string? input, bool compileOnly = false, bool wantDebugInfo = false)
        {
            var helper = new FyreHelper(code, input);
            var result = new ZlrHelperRunResult();

            bool compiled = helper.Compile(wantDebugInfo);
            result.ErrorCount = helper.ErrorCount;
            result.WarningCount = helper.WarningCount;
            result.Diagnostics = helper.Diagnostics;
            if (!compiled)
            {
                result.Status = ZlrTestStatus.CompilationFailed;
                return result;
            }

            if (compileOnly)
            {
                result.Status = ZlrTestStatus.Finished;
                return result;
            }

            if (!helper.Assemble())
            {
                result.Status = ZlrTestStatus.AssemblyFailed;
                return result;
            }

            string actualOutput = await helper.ExecuteAsync();

            result.Status = ZlrTestStatus.Finished;
            result.Output = actualOutput;
            return result;
        }

        const string SZilFileName = "Input.zil";
        const string SMainZapFileName = "Output.asm";
        const string SStoryFileName = "Output.ulx";

        readonly InMemoryFileSystem fileSystem = new();

        public int ErrorCount { get; private set; }
        public int WarningCount { get; private set; }
        public int SuppressedWarningCount { get; private set; }
        public IReadOnlyCollection<Diagnostic>? Diagnostics { get; private set; }    // includes suppressed

        private static readonly Regex _invalidXMLChars = GetInvalidXMLCharsRegex();
        private static string? _glazerPath;

        static string FindGlazerPath()
        {
            if (_glazerPath != null)
                return _glazerPath;

            var currentDir = Directory.GetCurrentDirectory();
            var dir = new DirectoryInfo(currentDir);

            while (dir != null)
            {
                var thirdpartyDir = Path.Combine(dir.FullName, "thirdparty");
                if (Directory.Exists(thirdpartyDir))
                {
                    var glazerExe = RuntimeInformation.IsOSPlatform(OSPlatform.Windows)
                        ? Path.Combine(thirdpartyDir, "glazer", "glazer.exe")
                        : Path.Combine(thirdpartyDir, "glazer", "glazer");

                    if (File.Exists(glazerExe))
                    {
                        _glazerPath = glazerExe;
                        return _glazerPath;
                    }

                    throw new FileNotFoundException($"Glazer executable not found at: {glazerExe}");
                }

                dir = dir.Parent;
            }

            throw new DirectoryNotFoundException("Could not find thirdparty directory");
        }

        /// <summary>
        /// https://stackoverflow.com/questions/397250/unicode-regex-invalid-xml-characters/961504#961504
        /// </summary>
        static string TransformInvalidXMLChars(string text)
        {
            if (string.IsNullOrEmpty(text)) return "";
            return _invalidXMLChars.Replace(text, m => $"\uFFFD\\u{(int)m.Value[0]:X4}");
        }

        void PrintZilCode()
        {
            Console.Error.WriteLine("=== {0} ===", SZilFileName);
            Console.Error.WriteLine(TransformInvalidXMLChars(code));
            Console.Error.WriteLine();
        }

        void PrintAsmCode()
        {
            PrintAsmCode("Output.asm");
        }

        void PrintAsmCode(string filename)
        {
            var asmCode = fileSystem.Exists(filename) ? fileSystem.GetText(filename) : "*** MISSING ***";

            // elide everything between the first "; Definition set" and "; Strings"
            var defSetStart = asmCode.IndexOf("\t; Definition set", StringComparison.Ordinal);
            var stringsStart = asmCode.IndexOf("\t; Strings", StringComparison.Ordinal);
            if (defSetStart >= 0 && stringsStart > defSetStart)
            {
                asmCode = asmCode[..defSetStart]
                    + "\t; ...RTL elided for brevity...\n"
                    + asmCode[stringsStart..];
            }

            Console.Error.WriteLine("=== {0} ===", filename);
            Console.Error.WriteLine(TransformInvalidXMLChars(asmCode));
            Console.Error.WriteLine();
        }

        [MemberNotNull(nameof(Diagnostics))]
        public bool Compile(bool wantDebugInfo = false)
        {
            return Compile(null, wantDebugInfo);
        }

        [MemberNotNull(nameof(Diagnostics))]
        bool Compile(Action<FrontEnd>? initializeFrontEnd, bool wantDebugInfo = false)
        {
            fileSystem.Clear();
            fileSystem.SetText(SZilFileName, code);

            var frontEnd = new FrontEnd { FileSystem = fileSystem };

            initializeFrontEnd?.Invoke(frontEnd);

            // run compilation
            PrintZilCode();

            var ctx = new Interpreter.Context();
            ctx.ZEnvironment.ZVersion = ZEnvironment.GLULX_ZVERSION;

            var result = frontEnd.Compile(ctx, SZilFileName, SMainZapFileName, wantDebugInfo);
            ErrorCount = result.ErrorCount;
            WarningCount = result.WarningCount;
            Diagnostics = result.Diagnostics;
            SuppressedWarningCount = result.SuppressedWarningCount;
            if (result.Success)
            {
                PrintAsmCode();
                return true;
            }

            Console.Error.WriteLine();
            return false;
        }

        [MemberNotNull(nameof(Diagnostics))]
        public bool Compile(out string compileOutput)
        {
            var channel = new ZilStringChannel(FileAccess.Write);

            var compiled = Compile(fe =>
            {
                fe.InitializeContext += (sender, e) =>
                {
                    e.Context.SetLocalVal(e.Context.GetStdAtom(StdAtom.OUTCHAN), channel);
                };
            });

            compileOutput = channel.String;
            return compiled;
        }

        public bool Assemble()
        {
            // Use Glazer for Glulx assembly
            var glazerPath = FindGlazerPath();
            var tempDir = Path.Combine(Path.GetTempPath(), $"glazer_{Guid.NewGuid():N}");

            try
            {
                Directory.CreateDirectory(tempDir);

                // Copy files from in-memory filesystem to temp directory
                foreach (var path in fileSystem.Paths)
                {
                    var destPath = Path.Combine(tempDir, path);
                    var destDirPath = Path.GetDirectoryName(destPath);
                    if (destDirPath != null && !Directory.Exists(destDirPath))
                        Directory.CreateDirectory(destDirPath);

                    File.WriteAllBytes(destPath, fileSystem.GetBytes(path));
                }

                // Run Glazer
                var inputPath = Path.Combine(tempDir, SMainZapFileName);
                var outputPath = Path.Combine(tempDir, SStoryFileName);

                var startInfo = new ProcessStartInfo
                {
                    FileName = glazerPath,
                    Arguments = $"\"{inputPath}\" -o \"{outputPath}\"",
                    UseShellExecute = false,
                    RedirectStandardOutput = true,
                    RedirectStandardError = true,
                    CreateNoWindow = true,
                    WorkingDirectory = tempDir
                };

                using var process = Process.Start(startInfo) ?? throw new InvalidOperationException("Failed to start Glazer process");
                process.WaitForExit();

                var stdout = process.StandardOutput.ReadToEnd();
                var stderr = process.StandardError.ReadToEnd();

                if (!string.IsNullOrWhiteSpace(stdout))
                    Console.Error.WriteLine($"Glazer output: {stdout}");
                if (!string.IsNullOrWhiteSpace(stderr))
                    Console.Error.WriteLine($"Glazer errors: {stderr}");

                if (process.ExitCode != 0)
                {
                    Console.Error.WriteLine($"Glazer failed with exit code {process.ExitCode}");
                    return false;
                }

                // Copy output back to in-memory filesystem
                if (File.Exists(outputPath))
                {
                    fileSystem.SetBytes(SStoryFileName, File.ReadAllBytes(outputPath));
                    return true;
                }

                Console.Error.WriteLine($"Glazer did not produce output file: {outputPath}");
                return false;
            }
            finally
            {
                // Clean up temp directory
                if (Directory.Exists(tempDir))
                {
                    try
                    {
                        Directory.Delete(tempDir, recursive: true);
                    }
                    catch
                    {
                        // Ignore cleanup errors
                    }
                }
            }
        }

        async Task<string> ExecuteAsync()
        {
            // var inputStream = input != null ? new MemoryStream(Encoding.UTF8.GetBytes(input)) : new MemoryStream();

            // var io = new ReplayIO(inputStream);
            // var gameStream = new MemoryStream(fileSystem.GetBytes(SStoryFileName), false);
            // var zmachine = new ZMachine(gameStream, io) { PredictableRandom = true };
            // await zmachine.SetReadingCommandsFromFileAsync(true);

            // await zmachine.RunAsync();

            // return io.CollectOutput();

            var inputLines = input != null
                ? new Queue<string>(input.Split(new[] { "\r\n", "\n" }, StringSplitOptions.None))
                : new Queue<string>();

            var gameStream = new MemoryStream(fileSystem.GetBytes(SStoryFileName));
            var collectedOutput = new StringBuilder();

            var engine = new Engine(gameStream)
            {
                GlkMode = GlkMode.Wrapper
            };
            engine.LineWanted += (sender, e) =>
            {
                if (inputLines.Count > 0)
                {
                    e.Line = inputLines.Dequeue();
                }
                else
                {
                    e.Line = null;
                }
            };
            engine.OutputReady += (sender, e) =>
            {
                collectedOutput.Append(e.Package["MAIN"]);
            };

            engine.Run();
            return collectedOutput.ToString();
        }

        [GeneratedRegex(@"(?<![\uD800-\uDBFF])[\uDC00-\uDFFF]|[\uD800-\uDBFF](?![\uDC00-\uDFFF])|[\x00-\x08\x0B\x0C\x0E-\x1F\x7F-\x9F\uFEFF\uFFFE\uFFFF]", RegexOptions.Compiled)]
        private static partial Regex GetInvalidXMLCharsRegex();
    }
}
