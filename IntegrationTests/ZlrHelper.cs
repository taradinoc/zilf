/* Copyright 2010, 2015 Jesse McGrew
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
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using System.Collections.Generic;
using System.Diagnostics.Contracts;
using System.IO;
using System.Linq;
using System.Text;
using Zapf;
using Zilf.Compiler;
using ZLR.VM;

namespace IntegrationTests
{
    enum ZlrTestStatus
    {
        CompilationFailed,
        AssemblyFailed,
        ExecutionFailed,
        Finished,
    }

    struct ZlrHelperRunResult
    {
        public ZlrTestStatus Status;
        public string Output;
        public int WarningCount;
    }

    class ZlrHelper
    {
        public static void RunAndAssert(string code, string input, string expectedOutput, bool? expectWarnings = null)
        {
            Contract.Requires(code != null);
            Contract.Requires(expectedOutput != null);

            var helper = new ZlrHelper(code, input);
            Assert.IsTrue(helper.Compile(), "Failed to compile");
            if (expectWarnings != null)
            {
                Assert.AreEqual((bool)expectWarnings, helper.WarningCount != 0,
                    (bool)expectWarnings ? "Expected warnings" : "Expected no warnings");
            }
            Assert.IsTrue(helper.Assemble(), "Failed to assemble");
            string actualOutput = helper.Execute();
            Assert.AreEqual(expectedOutput, actualOutput, "Actual output differs from expected");
        }

        public static ZlrHelperRunResult Run(string code, string input, bool compileOnly = false)
        {
            Contract.Requires(code != null);

            var helper = new ZlrHelper(code, input);
            var result = new ZlrHelperRunResult();

            bool compiled = helper.Compile();
            result.WarningCount = helper.WarningCount;
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
            if (actualOutput == null)
            {
                result.Status = ZlrTestStatus.ExecutionFailed;
                return result;
            }

            result.Status = ZlrTestStatus.Finished;
            result.Output = actualOutput;
            return result;
        }

        private const string SZilFileName = "Input.zil";
        private const string SMainZapFileName = "Output.zap";
        private const string SStoryFileNameTemplate = "Output.z#";

        private string code;
        private string input;

        private Dictionary<string, MemoryStream> zilfOutputFiles;
        private List<string> zilfLogMessages;

        private MemoryStream zapfOutputFile;

        public int WarningCount { get; private set; }

        public ZlrHelper(string code, string input)
        {
            Contract.Requires(code != null);

            this.code = code;
            this.input = input;
        }

        private void PrintZilCode()
        {
            Console.Error.WriteLine("=== {0} ===", SZilFileName);
            Console.Error.WriteLine(this.code);
            Console.Error.WriteLine();
        }

        private void PrintZapCode()
        {
            PrintZapCode("Output.zap");
            PrintZapCode("Output_data.zap");
        }

        private void PrintZapCode(string filename)
        {
            var zapStream = zilfOutputFiles[filename];
            var zapCode = Encoding.UTF8.GetString(zapStream.ToArray());
            Console.Error.WriteLine("=== {0} ===", filename);
            Console.Error.WriteLine(zapCode);
            Console.Error.WriteLine();
        }

        public bool Compile()
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
            this.zilfOutputFiles = new Dictionary<string, MemoryStream>();
            this.zilfLogMessages = new List<string>();

            var compiler = new ZilfCompiler();
            compiler.OpeningFile += (sender, e) =>
            {
                if (e.FileName == SZilFileName)
                {
                    e.Stream = codeStream;
                    return;
                }

                MemoryStream mstr;
                if (zilfOutputFiles.TryGetValue(e.FileName, out mstr))
                {
                    e.Stream = mstr;
                    return;
                }

                e.Stream = zilfOutputFiles[e.FileName] = new MemoryStream();
            };
            compiler.CheckingFilePresence += (sender, e) =>
            {
                // XXX this isn't right...?
                e.Exists = zilfOutputFiles.ContainsKey(e.FileName);
            };

            //XXX need to intercept <INSERT_FILE> too

            // run compilation
            PrintZilCode();
            var result = compiler.Compile(SZilFileName, SMainZapFileName);
            this.WarningCount = result.WarningCount;
            if (result.Success)
            {
                PrintZapCode();
                return true;
            }
            else
            {
                Console.Error.WriteLine();
                return false;
            }
        }

        public string GetZapCode()
        {
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
            return assembler.Assemble(SMainZapFileName, SStoryFileNameTemplate);
        }

        public string Execute()
        {
            MemoryStream inputStream;
            if (input != null)
            {
                inputStream = new MemoryStream(Encoding.UTF8.GetBytes(input));
            }
            else
            {
                inputStream = new MemoryStream();
            }

            var io = new ReplayIO(inputStream);
            var gameStream = new MemoryStream(zapfOutputFile.ToArray(), false);
            var zmachine = new ZMachine(gameStream, io);
            zmachine.PredictableRandom = true;
            zmachine.ReadingCommandsFromFile = true;

            zmachine.Run();

            return io.CollectOutput();
        }
    }

    // TODO: merge this with ZlrHelper
    class FileBasedZlrHelper
    {
        private const string SStoryFileNameTemplate = "Output.z#";

        private string codeFile, zapFileName;
        private string[] includeDirs;
        private string inputFile;

        private Dictionary<string, MemoryStream> zilfOutputFiles;
        private List<string> zilfLogMessages;

        private MemoryStream zapfOutputFile;

        public FileBasedZlrHelper(string codeFile, string[] includeDirs, string inputFile)
        {
            Contract.Requires(!string.IsNullOrWhiteSpace(codeFile));
            Contract.Requires(includeDirs != null && Contract.ForAll(includeDirs, d => !string.IsNullOrWhiteSpace(d)));

            this.codeFile = codeFile;
            this.includeDirs = includeDirs;
            this.inputFile = inputFile;

            this.zapFileName = Path.ChangeExtension(Path.GetFileName(codeFile), ".zap");
        }

        public bool WantStatusLine { get; set; }

        public bool Compile()
        {
            var codeStreams = new Dictionary<string, Stream>();

            try
            {
                // initialize ZilfCompiler
                this.zilfOutputFiles = new Dictionary<string, MemoryStream>();
                this.zilfLogMessages = new List<string>();

                var compiler = new ZilfCompiler();
                compiler.OpeningFile += (sender, e) =>
                {
                    if (e.Writing)
                    {
                        MemoryStream mstr;
                        if (zilfOutputFiles.TryGetValue(e.FileName, out mstr))
                        {
                            e.Stream = mstr;
                            return;
                        }

                        e.Stream = zilfOutputFiles[e.FileName] = new MemoryStream();
                    }
                    else
                    {
                        Stream result;
                        if (codeStreams.TryGetValue(e.FileName, out result))
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
                var io = new ReplayIO(inputStream, this.WantStatusLine);

                try
                {
                    var gameStream = new MemoryStream(zapfOutputFile.ToArray(), false);
                    var zmachine = new ZMachine(gameStream, io);
                    zmachine.PredictableRandom = true;
                    zmachine.ReadingCommandsFromFile = true;

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
    }
}
