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
using System.Collections.Generic;
using System.IO;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Zilf.Cli;
using Zilf.Common;
using Zilf.Compiler;
using Zilf.Interpreter;

namespace Zilf.Tests.Cli
{
    [TestClass, TestCategory("CLI")]
    public class BuildCommandHandlerTests
    {
        [TestMethod]
        public void Cornerstone_Build_Uses_Chisel_For_Automatic_Assembly()
        {
            var mainPath = Path.GetFullPath("story.zil");
            var fs = new InMemoryFileSystem();
            fs.SetText(mainPath, MakeProgram());

            var hostFileSystem = new HostFileSystem();
            var contextFactory = new ContextFactory(hostFileSystem);
            var frontEndFactory = new TestFrontEndFactory(fs, mainPath);
            var externalToolService = new RecordingExternalToolService
            {
                ChiselExecutable = Path.Combine(Path.GetTempPath(), "chisel.exe")
            };

            var handler = new BuildCommandHandler(contextFactory, frontEndFactory, externalToolService, hostFileSystem);
            var spec = CreateCommandSpec(handler, contextFactory, frontEndFactory, hostFileSystem);
            var parseResult = ParseBuild(spec, "--cornerstone", mainPath);

            var exitCode = handler.Execute(parseResult, spec, parseResult.GetValue(spec.BuildInputArgument));

            Assert.AreEqual(0, exitCode);
            Assert.AreEqual(1, externalToolService.FindChiselExecutableCallCount);
            Assert.AreEqual(1, externalToolService.RunChiselProcessCallCount);
            Assert.AreEqual(0, externalToolService.FindGlazerExecutableCallCount);
            Assert.AreEqual(0, externalToolService.RunGlazerProcessCallCount);
            Assert.AreEqual(0, externalToolService.FindZapfExecutableCallCount);
            Assert.AreEqual(0, externalToolService.RunZapfProcessCallCount);
            Assert.AreEqual(Path.ChangeExtension(mainPath, ".cas"), externalToolService.LastAsmInputPath);
            CollectionAssert.AreEqual(Array.Empty<string>(), externalToolService.LastExtraArgs);
            Assert.IsNull(externalToolService.LastFinalOutput);
        }

        [TestMethod]
        public void Chisel_Arguments_Use_Assemble_Subcommand_And_Mme_Output()
        {
            var asmPath = Path.GetFullPath("story.cas");
            var outputPath = Path.GetFullPath("story.mme");

            var args = ExternalToolService.CreateChiselArguments(asmPath, ["--trace"], outputPath);

            CollectionAssert.AreEqual(
                new[] { "assemble", asmPath, "--mme", outputPath, "--trace" },
                (System.Collections.ICollection)args);
        }

        [TestMethod]
        public void Chisel_Arguments_Default_Output_To_Mme_Beside_Cas_File()
        {
            var asmPath = Path.GetFullPath("story.cas");

            var args = ExternalToolService.CreateChiselArguments(asmPath, Array.Empty<string>());

            CollectionAssert.AreEqual(
                new[] { "assemble", asmPath, "--mme", Path.ChangeExtension(asmPath, ".mme") },
                (System.Collections.ICollection)args);
        }

        [TestMethod]
        public void Optimization_Level_Switches_Select_Requested_Level_And_Default_To_One()
        {
            var hostFileSystem = new HostFileSystem();
            var contextFactory = new ContextFactory(hostFileSystem);
            var frontEndFactory = new TestFrontEndFactory(new InMemoryFileSystem(), Path.GetFullPath("story.zil"));
            var handler = new BuildCommandHandler(
                contextFactory, frontEndFactory, new RecordingExternalToolService(), hostFileSystem);
            var spec = CreateCommandSpec(handler, contextFactory, frontEndFactory, hostFileSystem);

            Assert.AreEqual(1, CreateContext().OptimizationLevel);
            Assert.AreEqual(0, CreateContext("-O0").OptimizationLevel);
            Assert.AreEqual(1, CreateContext("-O1").OptimizationLevel);
            Assert.AreEqual(2, CreateContext("-O2").OptimizationLevel);
            Assert.AreEqual(3, CreateContext("-O3").OptimizationLevel);
            var sizeContext = CreateContext("-Oz");
            Assert.AreEqual(1, sizeContext.OptimizationLevel);
            Assert.IsTrue(sizeContext.OptimizeForSize);

            Context CreateContext(params string[] args)
            {
                var parseResult = ParseBuild(spec, args);
                return contextFactory.Create(parseResult, spec, RunMode.Compiler, "story.zil");
            }
        }

        [TestMethod]
        public void Debug_And_Publish_Select_Default_Optimization_Levels_Unless_Explicitly_Overridden()
        {
            var hostFileSystem = new HostFileSystem();
            var contextFactory = new ContextFactory(hostFileSystem);
            var frontEndFactory = new TestFrontEndFactory(new InMemoryFileSystem(), Path.GetFullPath("story.zil"));
            var handler = new BuildCommandHandler(
                contextFactory, frontEndFactory, new RecordingExternalToolService(), hostFileSystem);
            var spec = CreateCommandSpec(handler, contextFactory, frontEndFactory, hostFileSystem);

            Assert.AreEqual(0, CreateContext("-d").OptimizationLevel);
            Assert.AreEqual(2, CreateContext("--publish").OptimizationLevel);
            Assert.AreEqual(0, CreateContext("-d", "--publish").OptimizationLevel);
            Assert.AreEqual(3, CreateContext("-d", "-O3").OptimizationLevel);
            Assert.AreEqual(2, CreateContext("-d", "-O2").OptimizationLevel);
            Assert.AreEqual(0, CreateContext("--publish", "-O0").OptimizationLevel);
            Assert.AreEqual(1, CreateContext("-d", "-O1").OptimizationLevel);
            var sizeContext = CreateContext("--publish", "-Oz");
            Assert.AreEqual(1, sizeContext.OptimizationLevel);
            Assert.IsTrue(sizeContext.OptimizeForSize);

            Context CreateContext(params string[] args)
            {
                var parseResult = ParseBuild(spec, args);
                return contextFactory.Create(parseResult, spec, RunMode.Compiler, "story.zil");
            }
        }

        [TestMethod]
        public void Optimization_Level_Switches_Are_Mutually_Exclusive()
        {
            var hostFileSystem = new HostFileSystem();
            var contextFactory = new ContextFactory(hostFileSystem);
            var frontEndFactory = new TestFrontEndFactory(new InMemoryFileSystem(), Path.GetFullPath("story.zil"));
            var handler = new BuildCommandHandler(
                contextFactory, frontEndFactory, new RecordingExternalToolService(), hostFileSystem);
            var spec = CreateCommandSpec(handler, contextFactory, frontEndFactory, hostFileSystem);

            var parseResult = spec.RootCommand.Parse(["build", "-O2", "-Oz"]);

            Assert.AreEqual(1, parseResult.Errors.Count);
            StringAssert.Contains(parseResult.Errors[0].Message, "mutually exclusive", StringComparison.Ordinal);
        }

        private static ZilfCommandSpec CreateCommandSpec(
            BuildCommandHandler handler,
            ContextFactory contextFactory,
            IFrontEndFactory frontEndFactory,
            IHostFileSystem hostFileSystem)
        {
            var factory = new ZilfCommandSpecFactory(
                handler,
                new ReplCommandHandler(contextFactory),
                new ExecCommandHandler(contextFactory, frontEndFactory),
                new NewProjectCommandHandler(new ProjectTemplateService(hostFileSystem)));

            return factory.Create();
        }

        private static ParseResult ParseBuild(ZilfCommandSpec spec, params string[] args)
        {
            var parseResult = spec.RootCommand.Parse(["build", .. args]);
            Assert.AreEqual(0, parseResult.Errors.Count, string.Join(Environment.NewLine, parseResult.Errors));
            return parseResult;
        }

        private static string MakeProgram()
        {
            return @"<VERSION ZIP>

<ROUTINE GO ()
    <PRINTI ""Hello, world!"">
    <CRLF>
    <QUIT>>";
        }

        private sealed class TestFrontEndFactory(InMemoryFileSystem fileSystem, string mainPath) : IFrontEndFactory
        {
            public FrontEnd Create()
            {
                var frontEnd = new FrontEnd
                {
                    FileSystem = fileSystem
                };
                frontEnd.IncludePaths.Add(Path.GetDirectoryName(mainPath)!);
                return frontEnd;
            }
        }

        private sealed class RecordingExternalToolService : IExternalToolService
        {
            public string? ChiselExecutable { get; set; }

            public int FindZapfExecutableCallCount { get; private set; }

            public int FindGlazerExecutableCallCount { get; private set; }

            public int FindChiselExecutableCallCount { get; private set; }

            public int RunZapfProcessCallCount { get; private set; }

            public int RunGlazerProcessCallCount { get; private set; }

            public int RunChiselProcessCallCount { get; private set; }

            public string? LastAsmInputPath { get; private set; }

            public string[] LastExtraArgs { get; private set; } = [];

            public string? LastFinalOutput { get; private set; }

            public string? FindZapfExecutable()
            {
                FindZapfExecutableCallCount++;
                return null;
            }

            public string? FindZilfPubExecutable() => null;

            public string? FindGlazerExecutable()
            {
                FindGlazerExecutableCallCount++;
                return null;
            }

            public string? FindChiselExecutable()
            {
                FindChiselExecutableCallCount++;
                return ChiselExecutable;
            }

            public int RunZapfProcess(string zapfPath, string zapInputPath, IReadOnlyList<string> extraArgs, string? finalOutput = null)
            {
                RunZapfProcessCallCount++;
                return 0;
            }

            public int RunGlazerProcess(string glazerPath, string asmInputPath, IReadOnlyList<string> extraArgs, string? finalOutput = null)
            {
                RunGlazerProcessCallCount++;
                return 0;
            }

            public int RunChiselProcess(string chiselPath, string asmInputPath, IReadOnlyList<string> extraArgs, string? finalOutput = null)
            {
                RunChiselProcessCallCount++;
                LastAsmInputPath = asmInputPath;
                LastExtraArgs = [.. extraArgs];
                LastFinalOutput = finalOutput;
                return 0;
            }

            public int RunZilfPubProcess(string zilfPubPath, IReadOnlyList<string> arguments)
            {
                return 0;
            }
        }
    }
}
