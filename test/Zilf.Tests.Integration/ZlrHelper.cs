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
using System.IO;
using System.Linq;
using System.Text;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Zapf;
using Zilf.Compiler;
using Zilf.Diagnostics;
using Zilf.Interpreter.Values;
using ZLR.VM;
using Zilf.Language;
using System.Text.RegularExpressions;
using System.Diagnostics.CodeAnalysis;
using Zilf.Common;
using System.Threading.Tasks;

namespace Zilf.Tests.Integration
{
    public enum ZlrTestStatus
    {
        CompilationFailed,
        AssemblyFailed,
/*
        ExecutionFailed,        // execution failure means an exception in ZLR, and it bubbles up through the test
*/
        Finished
    }

    public struct ZlrHelperRunResult
    {
        public ZlrTestStatus Status;
        // ReSharper disable once NotAccessedField.Global
        public string Output;
        public int WarningCount;
        public int SuppressedWarningCount;
        public int ErrorCount;
        public IReadOnlyCollection<Diagnostic> Diagnostics;
    }

    sealed partial class ZlrHelper(string code, string? input)
    {
        public static async Task RunAndAssertAsync(string code, string? input, string expectedOutput,
            IEnumerable<(Predicate<ZlrHelperRunResult>, string message)>? warningChecks = null,
            bool wantCompileOutput = false)
        {
            var helper = new ZlrHelper(code, input);
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
            var helper = new ZlrHelper(code, input);
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
        const string SMainZapFileName = "Output.zap";
        const string SStoryFileName = "Output.zcode";

        readonly InMemoryFileSystem fileSystem = new();

        public int ErrorCount { get; private set; }
        public int WarningCount { get; private set; }
        public int SuppressedWarningCount { get; private set; }
        public IReadOnlyCollection<Diagnostic>? Diagnostics { get; private set; }    // includes suppressed

        private static readonly Regex _invalidXMLChars = GetInvalidXMLCharsRegex();

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

        void PrintZapCode()
        {
            PrintZapCode("Output.zap");
            PrintZapCode("Output_data.zap");
        }

        void PrintZapCode(string filename)
        {
            var zapCode = fileSystem.Exists(filename) ? fileSystem.GetText(filename) : "*** MISSING ***";
            Console.Error.WriteLine("=== {0} ===", filename);
            Console.Error.WriteLine(TransformInvalidXMLChars(zapCode));
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
            var result = frontEnd.Compile(SZilFileName, SMainZapFileName, wantDebugInfo);
            ErrorCount = result.ErrorCount;
            WarningCount = result.WarningCount;
            Diagnostics = result.Diagnostics;
            SuppressedWarningCount = result.SuppressedWarningCount;
            if (result.Success)
            {
                PrintZapCode();
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

        public string GetZapCode()
        {
            var sb = new StringBuilder();

            foreach (var content in from path in fileSystem.Paths
                                    where path.EndsWith(".zap") || path.EndsWith(".xzap")
                                    orderby path
                                    select fileSystem.GetText(path))
            {
                sb.Append(content);
                sb.AppendLine();
            }

            return sb.ToString();
        }

        public bool Assemble()
        {
            // initialize ZapfAssembler
            var assembler = new ZapfAssembler { FileSystem = fileSystem };

            // run assembly
            var result = assembler.Assemble(SMainZapFileName, SStoryFileName);
            WarningCount += result.Context?.WarningCount ?? 0;
            return result.Success;
        }

        async Task<string> ExecuteAsync()
        {
            var inputStream = input != null ? new MemoryStream(Encoding.UTF8.GetBytes(input)) : new MemoryStream();

            var io = new ReplayIO(inputStream);
            var gameStream = new MemoryStream(fileSystem.GetBytes(SStoryFileName), false);
            var zmachine = new ZMachine(gameStream, io) { PredictableRandom = true };
            await zmachine.SetReadingCommandsFromFileAsync(true);

            await zmachine.RunAsync();

            return io.CollectOutput();
        }

        [GeneratedRegex(@"(?<![\uD800-\uDBFF])[\uDC00-\uDFFF]|[\uD800-\uDBFF](?![\uDC00-\uDFFF])|[\x00-\x08\x0B\x0C\x0E-\x1F\x7F-\x9F\uFEFF\uFFFE\uFFFF]", RegexOptions.Compiled)]
        private static partial Regex GetInvalidXMLCharsRegex();
    }

    // TODO: merge this with ZlrHelper
    class FileBasedZlrHelper(string codeFile, string[] includeDirs, string? inputFile)
    {
        const string SStoryFileName = "Output.zcode";
        readonly string zapFileName = Path.ChangeExtension(Path.GetFileName(codeFile), ".zap");
        readonly InMemoryFileSystem fileSystem = new();

        public bool WantStatusLine { get; set; }

        public bool Compile()
        {
            // initialize ZilfCompiler
            var compiler = new FrontEnd
            {
                FileSystem = new OverlayFileSystem(fileSystem, new LimitedFileSystem(includeDirs))
            };

            compiler.IncludePaths.Add("");

            // run compilation
            if (compiler.Compile(Path.GetFileName(codeFile), zapFileName).Success)
            {
                return true;
            }
            else
            {
                Console.Error.WriteLine();
                return false;
            }
        }

        public bool Assemble()
        {
            // initialize ZapfAssembler
            var assembler = new ZapfAssembler { FileSystem = fileSystem };

            // run assembly
            return assembler.Assemble(zapFileName, SStoryFileName).Success;
        }

        /// <exception cref="Exception">Oh shit!</exception>
        public async Task<string> ExecuteAsync()
        {
            if (!fileSystem.Exists(SStoryFileName))
                throw new InvalidOperationException($"{nameof(Assemble)} must be called first");

            var zapfOutputFile = fileSystem.GetBytes(SStoryFileName);

            Stream inputStream;
            if (inputFile != null)
            {
                inputStream = new FileStream(inputFile, FileMode.Open, FileAccess.Read);
            }
            else
            {
                inputStream = new MemoryStream();
            }

            try
            {
                var io = new ReplayIO(inputStream, WantStatusLine);

                try
                {
                    var gameStream = new MemoryStream(zapfOutputFile, false);
                    var zmachine = new ZMachine(gameStream, io) { PredictableRandom = true };
                    await zmachine.SetReadingCommandsFromFileAsync(true);

                    await zmachine.RunAsync();
                }
                catch
                {
                    Console.WriteLine("Oh shit!");
                    Console.Write(io.CollectOutput());
                    throw;
                }

                return io.CollectOutput();
            }
            finally
            {
                inputStream.Dispose();
            }
        }
    }
}
