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