using System;
using System.IO;
using System.Linq;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Zilf.Common;
using Zilf.Compiler;
using Zilf.Ide;
using Zilf.Interpreter;
using Zilf.Interpreter.Values;
using Zilf.Language.Parsing;

namespace Zilf.Tests.Interpreter
{
    [TestClass]
    public sealed class IdeInfoTests
    {
        [TestMethod]
        public void IdeInfoReport_IncludesFilesMacrosFlagsAndDefinitions()
        {
            var mainPath = Path.GetFullPath("main.zil");
            var incPath = Path.Combine(Path.GetDirectoryName(mainPath)!, "inc.zil");

            var fs = new InMemoryFileSystem();
            fs.SetText(incPath, "<DEFINE FOO (A) .A>");
            fs.SetText(mainPath, "<INSERT-FILE \"inc.zil\">\n<DEFAULT-DEFINITION TESTSEC 1>\n<BIT-SYNONYM TESTFLAG TESTALIAS>\n<OBJECT THING (FLAGS TESTALIAS)>\n<ROUTINE GO () <RTRUE>>");

            var frontEnd = new FrontEnd
            {
                FileSystem = fs
            };
            frontEnd.IncludePaths.Add(Path.GetDirectoryName(mainPath)!);

            var ctx = new Context(ignoreCase: false)
            {
                RunMode = RunMode.Compiler
            };

            // Install a prefix macro so we can confirm enumeration.
            ctx.ParserMacros.MakePrefixMacro('^', (c, zo) => ParserOutput.FromObject(zo));

            var outputPath = Path.GetFullPath("out.zap");
            var feResult = frontEnd.Compile(ctx, mainPath, outputPath, wantDebugInfo: false);
            Assert.IsTrue(feResult.Success, "FrontEnd.Compile should succeed for this test input");

            var report = IdeInfoReport.Build(ctx, mainPath);

            var filesRead = report["filesRead"]!.AsArray().Select(n => (string)n!).ToArray();
            CollectionAssert.Contains(filesRead, mainPath);
            CollectionAssert.Contains(filesRead, incPath);

            var prefixMacros = report["prefixMacros"]!.AsArray().Select(n => (string)n!).ToArray();
            CollectionAssert.Contains(prefixMacros, "^");

            var flags = report["compilationFlags"]!.AsArray();
            Assert.IsTrue(flags.Count > 0);
            Assert.IsTrue(flags.Any(f => string.Equals((string?)f?["name"], "UNDO", StringComparison.OrdinalIgnoreCase)));

            var defaultDefs = report["defaultDefinitions"]!.AsArray();
            Assert.IsTrue(defaultDefs.Any(d => string.Equals((string?)d?["name"], "TESTSEC", StringComparison.OrdinalIgnoreCase)
                                               && string.Equals((string?)d?["status"], "default", StringComparison.OrdinalIgnoreCase)));

            var mdlCallables = report["callables"]!["mdl"]!.AsArray();
            Assert.IsTrue(mdlCallables.Any(c => string.Equals((string?)c?["name"], "FOO", StringComparison.OrdinalIgnoreCase)
                                                && string.Equals((string?)c?["kind"], "define", StringComparison.OrdinalIgnoreCase)));

            var zcodeFlags = report["entities"]!["flags"]!.AsArray();
            Assert.IsTrue(zcodeFlags.Any(f => string.Equals((string?)f?["name"], "TESTFLAG", StringComparison.OrdinalIgnoreCase)));

            var flagSynonyms = report["entities"]!["flagSynonyms"]!.AsArray();
            Assert.IsTrue(flagSynonyms.Any(s => string.Equals((string?)s?["alias"], "TESTALIAS", StringComparison.OrdinalIgnoreCase)
                                                && string.Equals((string?)s?["original"], "TESTFLAG", StringComparison.OrdinalIgnoreCase)));
        }
    }
}
