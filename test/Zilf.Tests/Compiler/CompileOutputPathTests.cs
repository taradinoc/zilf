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
using System.IO;
using System.Linq;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Zilf.Common;
using Zilf.Compiler;
using Zilf.Diagnostics;
using Zilf.Interpreter;
using Zilf.Interpreter.Values;
using Zilf.ZModel;

namespace Zilf.Tests.Compiler
{
    [TestClass, TestCategory("Compiler")]
    public class CompileOutputPathTests
    {
        [TestMethod]
        public void Source_Selected_Glulx_Defaults_To_Asm_Output()
        {
            var mainPath = Path.GetFullPath("story.zil");
            var fs = new InMemoryFileSystem();
            fs.SetText(mainPath, MakeProgram("<VERSION GLULX>"));

            var frontEnd = CreateFrontEnd(fs, mainPath);
            var ctx = new Context(ignoreCase: false)
            {
                RunMode = RunMode.Compiler
            };

            var evalResult = frontEnd.EvaluateSource(ctx, mainPath);

            Assert.IsTrue(evalResult.Success, FormatDiagnostics(evalResult));
            Assert.IsTrue(ctx.IsGlulx);

            var outputPaths = Zilf.Program.ResolveCompileOutputPaths(mainPath, null, stopAfter: false, isGlulx: ctx.IsGlulx);
            Assert.AreEqual(Path.ChangeExtension(mainPath, ".asm"), outputPaths.IntermediateFile);
            Assert.IsNull(outputPaths.FinalAssemblerOutput);

            var emitResult = frontEnd.EmitCompiledGame(ctx, outputPaths.IntermediateFile, wantDebugInfo: false);
            Assert.IsTrue(emitResult.Success, FormatDiagnostics(emitResult));
            Assert.IsTrue(fs.Exists(outputPaths.IntermediateFile));
        }

        [TestMethod]
        public void ZMachine_Source_Defaults_To_Zap_Output()
        {
            var mainPath = Path.GetFullPath("story.zil");
            var fs = new InMemoryFileSystem();
            fs.SetText(mainPath, MakeProgram("<VERSION ZIP>"));

            var frontEnd = CreateFrontEnd(fs, mainPath);
            var ctx = new Context(ignoreCase: false)
            {
                RunMode = RunMode.Compiler
            };

            var evalResult = frontEnd.EvaluateSource(ctx, mainPath);

            Assert.IsTrue(evalResult.Success, FormatDiagnostics(evalResult));
            Assert.IsFalse(ctx.IsGlulx);

            var outputPaths = Zilf.Program.ResolveCompileOutputPaths(mainPath, null, stopAfter: false, isGlulx: ctx.IsGlulx);
            Assert.AreEqual(Path.ChangeExtension(mainPath, ".zap"), outputPaths.IntermediateFile);
            Assert.IsNull(outputPaths.FinalAssemblerOutput);
        }

        [TestMethod]
        public void Explicit_Output_Path_Is_Preserved()
        {
            var mainPath = Path.GetFullPath("story.zil");
            var explicitOutput = Path.GetFullPath("custom-output.z5");

            var outputPaths = Zilf.Program.ResolveCompileOutputPaths(mainPath, explicitOutput, stopAfter: false, isGlulx: true);

            Assert.AreEqual(Path.ChangeExtension(mainPath, ".asm"), outputPaths.IntermediateFile);
            Assert.AreEqual(explicitOutput, outputPaths.FinalAssemblerOutput);
        }

        [TestMethod]
        public void Default_ZMachine_Assembler_Output_Uses_Versioned_Extension()
        {
            var asmPath = Path.GetFullPath("story.zap");

            var outputPath = Zilf.Program.ResolveFinalStoryOutputPath(asmPath, null, isGlulx: false, zVersion: 5);

            Assert.AreEqual(Path.ChangeExtension(asmPath, ".z5"), outputPath);
        }

        [TestMethod]
        public void Default_Glulx_Assembler_Output_Uses_Ulx_Extension()
        {
            var asmPath = Path.GetFullPath("story.asm");

            var outputPath = Zilf.Program.ResolveFinalStoryOutputPath(asmPath, null, isGlulx: true, zVersion: 5);

            Assert.AreEqual(Path.ChangeExtension(asmPath, ".ulx"), outputPath);
        }

        [TestMethod]
        public void CompileOnly_Blorb_Output_Uses_Plain_Blorb_Extension()
        {
            var intermediatePath = Path.GetFullPath("story.asm");

            var blorbPath = Zilf.Program.ResolveBlorbOutputPath(
                intermediatePath,
                output: null,
                stopAfter: true,
                isGlulx: true,
                zVersion: 5);

            Assert.AreEqual(Path.ChangeExtension(intermediatePath, ".blorb"), blorbPath);
        }

        [TestMethod]
        public void FullBuild_Blorb_Output_Uses_Zblorb_Extension_For_ZMachine()
        {
            var intermediatePath = Path.GetFullPath("custom-output.zap");

            var blorbPath = Zilf.Program.ResolveBlorbOutputPath(
                intermediatePath,
                output: null,
                stopAfter: false,
                isGlulx: false,
                zVersion: 3);

            Assert.AreEqual(Path.ChangeExtension(intermediatePath, ".zblorb"), blorbPath);
        }

        [TestMethod]
        public void FullBuild_Blorb_Output_Uses_Gblorb_Extension_For_Glulx()
        {
            var intermediatePath = Path.GetFullPath("custom-output.asm");

            var blorbPath = Zilf.Program.ResolveBlorbOutputPath(
                intermediatePath,
                output: null,
                stopAfter: false,
                isGlulx: true,
                zVersion: 5);

            Assert.AreEqual(Path.ChangeExtension(intermediatePath, ".gblorb"), blorbPath);
        }

        [TestMethod]
        public void Publish_Output_Defaults_To_Publish_Subdirectory_Of_Story_Directory()
        {
            var storyPath = Path.GetFullPath(Path.Combine("build", "story.z3"));

            var outputPath = Zilf.Program.ResolvePublishOutputPath(storyPath, publishOutputOption: null);

            Assert.AreEqual(Path.Combine(Path.GetDirectoryName(storyPath)!, "publish"), outputPath);
        }

        [TestMethod]
        public void Publish_Output_Uses_Explicit_Option_When_Provided()
        {
            var storyPath = Path.GetFullPath("story.z3");
            var explicitPath = Path.GetFullPath(Path.Combine("out", "site"));

            var outputPath = Zilf.Program.ResolvePublishOutputPath(storyPath, explicitPath);

            Assert.AreEqual(explicitPath, outputPath);
        }

        [TestMethod]
        public void Publish_Project_Name_Prefers_Game_Title_Global()
        {
            var ctx = new Context(ignoreCase: false)
            {
                RunMode = RunMode.Compiler
            };
            var titleAtom = ZilAtom.Parse("GAME-TITLE", ctx);
            ctx.SetGlobalVal(titleAtom, ZilString.FromString("The Great Game"));

            var projectName = Zilf.Program.ResolvePublishProjectName(ctx, Path.GetFullPath("story.zil"));

            Assert.AreEqual("The Great Game", projectName);
        }

        [TestMethod]
        public void Publish_Project_Name_Uses_First_Banner_Line_When_Title_Missing()
        {
            var ctx = new Context(ignoreCase: false)
            {
                RunMode = RunMode.Compiler
            };

            var bannerAtom = ZilAtom.Parse("GAME-BANNER", ctx);
            ctx.SetGlobalVal(bannerAtom, ZilString.FromString("Banner Name|By Author"));

            var projectName = Zilf.Program.ResolvePublishProjectName(ctx, Path.GetFullPath("story.zil"));

            Assert.AreEqual("Banner Name", projectName);
        }

        [TestMethod]
        public void Publish_Project_Name_Uses_Banner_Custom_Crlf_Character()
        {
            var ctx = new Context(ignoreCase: false)
            {
                RunMode = RunMode.Compiler
            };

            var crlfAtom = ctx.GetStdAtom(Zilf.Language.StdAtom.CRLF_CHARACTER);
            ctx.SetGlobalVal(crlfAtom, new ZilChar('/'));

            var bannerAtom = ZilAtom.Parse("GAME-BANNER", ctx);
            ctx.SetGlobalVal(bannerAtom, ZilString.FromString("Alpha/Beta"));

            var projectName = Zilf.Program.ResolvePublishProjectName(ctx, Path.GetFullPath("story.zil"));

            Assert.AreEqual("Alpha", projectName);
        }

        [TestMethod]
        public void Publish_Project_Name_Falls_Back_To_Capitalized_Input_Base_Name()
        {
            var ctx = new Context(ignoreCase: false)
            {
                RunMode = RunMode.Compiler
            };

            var inputPath = Path.GetFullPath("advent.zil");

            var projectName = Zilf.Program.ResolvePublishProjectName(ctx, inputPath);

            Assert.AreEqual("Advent", projectName);
        }

        [TestMethod]
        public void Version_Glulx_Conflicts_With_Glulx16_Target()
        {
            var mainPath = Path.GetFullPath("story.zil");
            var fs = new InMemoryFileSystem();
            fs.SetText(mainPath, MakeProgram("<VERSION GLULX>"));

            var frontEnd = CreateFrontEnd(fs, mainPath);
            var ctx = new Context(ignoreCase: false)
            {
                RunMode = RunMode.Compiler
            };

            ctx.SetTargetPlatform(TargetPlatform.Glulx16);
            ctx.SetZVersion(ctx.ZEnvironment.ZVersion);

            var evalResult = frontEnd.EvaluateSource(ctx, mainPath);

            Assert.IsFalse(evalResult.Success);
            Assert.IsTrue(evalResult.Diagnostics.Any(d =>
                d.Severity == Severity.Error &&
                d.CodeNumber == InterpreterMessages._0_Glulx16_Cannot_Be_Combined_With_VERSION_GLULX));
        }

        static FrontEnd CreateFrontEnd(InMemoryFileSystem fs, string mainPath)
        {
            var frontEnd = new FrontEnd
            {
                FileSystem = fs
            };
            frontEnd.IncludePaths.Add(Path.GetDirectoryName(mainPath)!);
            return frontEnd;
        }

        static string MakeProgram(string versionDirective)
        {
            return $@"{versionDirective}

<ROUTINE GO ()
    <PRINTI ""Hello, world!"">
    <CRLF>
    <QUIT>>";
        }

        static string FormatDiagnostics(FrontEndResult result)
        {
            return string.Join(Environment.NewLine, result.Diagnostics.Select(d => d.ToString()));
        }
    }
}