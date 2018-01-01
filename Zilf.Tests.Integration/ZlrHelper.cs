/* Copyright 2010-2017 Jesse McGrew
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

using JetBrains.Annotations;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Diagnostics.Contracts;
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
        public int ErrorCount;
        public IReadOnlyCollection<Diagnostic> Diagnostics;
    }

    sealed class ZlrHelper : IDisposable
    {
        public static void RunAndAssert([NotNull] string code, string input, [NotNull] string expectedOutput, bool? expectWarnings = null, bool wantCompileOutput = false)
        {
            Contract.Requires(code != null);
            Contract.Requires(expectedOutput != null);

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
            if (expectWarnings != null)
            {
                Assert.AreEqual((bool)expectWarnings, helper.WarningCount != 0,
                    (bool)expectWarnings ? "Expected warnings" : "Expected no warnings");
            }
            string actualOutput = compileOutput + helper.Execute();
            Assert.AreEqual(expectedOutput, actualOutput, "Actual output differs from expected");
        }

        public static ZlrHelperRunResult Run([NotNull] string code, string input, bool compileOnly = false)
        {
            Contract.Requires(code != null);

            var helper = new ZlrHelper(code, input);
            var result = new ZlrHelperRunResult();

            bool compiled = helper.Compile();
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

            string actualOutput = helper.Execute();

            result.Status = ZlrTestStatus.Finished;
            result.Output = actualOutput;
            return result;
        }

        const string SZilFileName = "Input.zil";
        const string SMainZapFileName = "Output.zap";
        const string SStoryFileNameTemplate = "Output.z#";

        [NotNull]
        readonly string code;
        [CanBeNull]
        readonly string input;

        [NotNull]
        readonly Dictionary<string, MemoryStream> zilfOutputFiles = new Dictionary<string, MemoryStream>();

        [CanBeNull]
        MemoryStream zapfOutputFile;

        public int ErrorCount { get; private set; }
        public int WarningCount { get; private set; }
        [CanBeNull]
        public IReadOnlyCollection<Diagnostic> Diagnostics { get; private set; }

        public ZlrHelper([NotNull] string code, [CanBeNull] string input)
        {
            Contract.Requires(code != null);

            this.code = code;
            this.input = input;
        }

        void PrintZilCode()
        {
            Console.Error.WriteLine("=== {0} ===", SZilFileName);
            Console.Error.WriteLine(code);
            Console.Error.WriteLine();
        }

        void PrintZapCode()
        {
            PrintZapCode("Output.zap");
            PrintZapCode("Output_data.zap");
        }

        void PrintZapCode([NotNull] string filename)
        {
            Contract.Requires(filename != null);
            var zapStream = zilfOutputFiles[filename];
            var zapCode = Encoding.UTF8.GetString(zapStream.ToArray());
            Console.Error.WriteLine("=== {0} ===", filename);
            Console.Error.WriteLine(zapCode);
            Console.Error.WriteLine();
        }

        public bool Compile()
        {
            return Compile(null);
        }

        bool Compile([CanBeNull] Action<FrontEnd> initializeFrontEnd)
        {
            // write code to a MemoryStream
            var codeStream = new MemoryStream();
            using (var wtr = new StreamWriter(codeStream, Encoding.UTF8, 512, true))
            {
                wtr.Write(code);
                wtr.Flush();
            }
            codeStream.Seek(0, SeekOrigin.Begin);

            // initialize ZilfCompiler
            zilfOutputFiles.Clear();

            var frontEnd = new FrontEnd();
            frontEnd.OpeningFile += (sender, e) =>
            {
                if (e.FileName == SZilFileName)
                {
                    e.Stream = codeStream;
                    return;
                }

                if (zilfOutputFiles.TryGetValue(e.FileName, out var mstr))
                {
                    e.Stream = mstr;
                    return;
                }

                e.Stream = zilfOutputFiles[e.FileName] = new MemoryStream();
            };
            frontEnd.CheckingFilePresence += (sender, e) =>
            {
                // XXX this isn't right...?
                e.Exists = zilfOutputFiles.ContainsKey(e.FileName);
            };

            initializeFrontEnd?.Invoke(frontEnd);

            //XXX need to intercept <INSERT_FILE> too

            // run compilation
            PrintZilCode();
            var result = frontEnd.Compile(SZilFileName, SMainZapFileName);
            ErrorCount = result.ErrorCount;
            WarningCount = result.WarningCount;
            Diagnostics = result.Diagnostics;
            if (result.Success)
            {
                PrintZapCode();
                return true;
            }

            Console.Error.WriteLine();
            return false;
        }

        public bool Compile([NotNull] out string compileOutput)
        {
            Contract.Requires(compileOutput != null);
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

        [NotNull]
        public string GetZapCode()
        {
            Contract.Ensures(Contract.Result<string>() != null);
            var sb = new StringBuilder();

            foreach (var stream in zilfOutputFiles.OrderBy(p => p.Key).Select(p => p.Value))
            {
                sb.Append(Encoding.UTF8.GetString(stream.ToArray()));
                sb.AppendLine();
            }

            return sb.ToString();
        }

        public bool Assemble()
        {
            // initialize ZapfAssembler
            var assembler = new ZapfAssembler();
            assembler.OpeningFile += (sender, e) =>
            {
                if (e.Writing)
                {
                    //XXX this could potentially be the debug file instead!

                    zapfOutputFile = new MemoryStream();
                    e.Stream = zapfOutputFile;
                }
                else if (zilfOutputFiles.ContainsKey(e.FileName))
                {
                    var buffer = zilfOutputFiles[e.FileName].ToArray();
                    e.Stream = new MemoryStream(buffer, false);
                }
                else
                {
                    throw new InvalidOperationException("No such ZILF output file: " + e.FileName);
                }
            };
            assembler.CheckingFilePresence += (sender, e) =>
            {
                e.Exists = zilfOutputFiles.ContainsKey(e.FileName);
            };

            // run assembly
            var success = assembler.Assemble(SMainZapFileName, SStoryFileNameTemplate, out _, out int warningCount);
            WarningCount += warningCount;
            return success;
        }

        [NotNull]
        string Execute()
        {
            Contract.Ensures(Contract.Result<string>() != null);
            Debug.Assert(zapfOutputFile != null);

            var inputStream = input != null ? new MemoryStream(Encoding.UTF8.GetBytes(input)) : new MemoryStream();

            var io = new ReplayIO(inputStream);
            var gameStream = new MemoryStream(zapfOutputFile.ToArray(), false);
            var zmachine = new ZMachine(gameStream, io)
            {
                PredictableRandom = true,
                ReadingCommandsFromFile = true
            };

            zmachine.Run();

            return io.CollectOutput();
        }

        public void Dispose()
        {
            zapfOutputFile?.Dispose();
        }
    }

    // TODO: merge this with ZlrHelper
    class FileBasedZlrHelper
    {
        const string SStoryFileNameTemplate = "Output.z#";

        [NotNull]
        readonly string codeFile;

        [NotNull]
        readonly string zapFileName;

        [NotNull]
        [ItemNotNull]
        readonly string[] includeDirs;

        [CanBeNull]
        readonly string inputFile;

        Dictionary<string, MemoryStream> zilfOutputFiles;

        MemoryStream zapfOutputFile;

        public FileBasedZlrHelper([NotNull] string codeFile, [ItemNotNull] [NotNull] string[] includeDirs, string inputFile)
        {
            Contract.Requires(!string.IsNullOrWhiteSpace(codeFile));
            Contract.Requires(includeDirs != null);
            Contract.Requires(Contract.ForAll(includeDirs, d => !string.IsNullOrWhiteSpace(d)));

            this.codeFile = codeFile;
            this.includeDirs = includeDirs;
            this.inputFile = inputFile;

            zapFileName = Path.ChangeExtension(Path.GetFileName(codeFile), ".zap");
        }

        public bool WantStatusLine { get; set; }

        public bool Compile()
        {
            var codeStreams = new Dictionary<string, Stream>();

            try
            {
                // initialize ZilfCompiler
                zilfOutputFiles = new Dictionary<string, MemoryStream>();

                var compiler = new FrontEnd();
                compiler.OpeningFile += (sender, e) =>
                {
                    if (e.Writing)
                    {
                        if (zilfOutputFiles.TryGetValue(e.FileName, out var mstr))
                        {
                            e.Stream = mstr;
                            return;
                        }

                        e.Stream = zilfOutputFiles[e.FileName] = new MemoryStream();
                    }
                    else
                    {
                        if (codeStreams.TryGetValue(e.FileName, out var result))
                        {
                            e.Stream = result;
                            return;
                        }

                        foreach (var idir in includeDirs)
                        {
                            var path = Path.Combine(idir, e.FileName);
                            if (File.Exists(path))
                            {
                                e.Stream = codeStreams[e.FileName] = new FileStream(path, FileMode.Open, FileAccess.Read);
                                return;
                            }
                        }
                    }
                };
                compiler.CheckingFilePresence += (sender, e) =>
                {
                    foreach (var idir in includeDirs)
                    {
                        if (File.Exists(Path.Combine(idir, e.FileName)))
                        {
                            e.Exists = true;
                            return;
                        }
                    }
                };
                foreach (var dir in includeDirs)
                    compiler.IncludePaths.Add(dir);

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
            finally
            {
                foreach (var stream in codeStreams.Values)
                    stream.Dispose();
            }
        }

        public bool Assemble()
        {
            var codeStreams = new Dictionary<string, Stream>();

            try
            {
                // initialize ZapfAssembler
                var assembler = new ZapfAssembler();
                assembler.OpeningFile += (sender, e) =>
                {
                    if (e.Writing)
                    {
                        //XXX this could potentially be the debug file instead!

                        zapfOutputFile = new MemoryStream();
                        e.Stream = zapfOutputFile;
                    }
                    else if (zilfOutputFiles.ContainsKey(e.FileName))
                    {
                        var buffer = zilfOutputFiles[e.FileName].ToArray();
                        e.Stream = new MemoryStream(buffer, false);
                    }
                    else
                    {
                        foreach (var idir in includeDirs)
                        {
                            var path = Path.Combine(idir, e.FileName);
                            if (File.Exists(path))
                            {
                                e.Stream = codeStreams[e.FileName] = new FileStream(path, FileMode.Open, FileAccess.Read);
                                return;
                            }
                        }

                        throw new InvalidOperationException("No such ZILF output file: " + e.FileName);
                    }
                };
                assembler.CheckingFilePresence += (sender, e) =>
                {
                    if (zilfOutputFiles.ContainsKey(e.FileName))
                    {
                        e.Exists = true;
                    }
                    else
                    {
                        foreach (var idir in includeDirs)
                        {
                            if (File.Exists(Path.Combine(idir, e.FileName)))
                            {
                                e.Exists = true;
                                return;
                            }
                        }

                        e.Exists = false;
                    }
                };

                // run assembly
                return assembler.Assemble(zapFileName, SStoryFileNameTemplate);
            }
            finally
            {
                foreach (var stream in codeStreams.Values)
                    stream.Dispose();
            }
        }

        /// <exception cref="Exception">Oh shit!</exception>
        public string Execute()
        {
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
                    var gameStream = new MemoryStream(zapfOutputFile.ToArray(), false);
                    var zmachine = new ZMachine(gameStream, io)
                    {
                        PredictableRandom = true,
                        ReadingCommandsFromFile = true
                    };

                    zmachine.Run();
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

        [ContractInvariantMethod]
        [SuppressMessage("Microsoft.Performance", "CA1822: MarkMembersAsStatic", Justification = "Required for code contracts.")]
        [Conditional("CONTRACTS_FULL")]
        private void ObjectInvariant()
        {
            Contract.Invariant(codeFile != null);
            Contract.Invariant(zapFileName != null);
            Contract.Invariant(includeDirs != null);
        }
    }
}
