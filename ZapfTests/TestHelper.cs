using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using Zapf;

namespace ZapfTests
{
    internal static class TestHelper
    {
        public static bool Assemble(string code)
        {
            const string InputFileName = "Input.zap";
            const string OutputFileName = "Output.z#";
            var inputFiles = new Dictionary<string, string>() {
                { InputFileName, code },
            };
            var outputFiles = new Dictionary<string, MemoryStream>();

            // initialize ZapfAssembler
            var assembler = new ZapfAssembler();
            assembler.OpeningFile += (sender, e) =>
            {
                if (e.Writing)
                {
                    var mstr = new MemoryStream();
                    e.Stream = mstr;
                    outputFiles.Add(e.FileName, mstr);
                }
                else if (inputFiles.ContainsKey(e.FileName))
                {
                    var buffer = Encoding.UTF8.GetBytes(inputFiles[e.FileName]);
                    e.Stream = new MemoryStream(buffer, false);
                }
                else
                {
                    throw new InvalidOperationException("No such input file: " + e.FileName);
                }
            };
            assembler.CheckingFilePresence += (sender, e) =>
            {
                e.Exists = inputFiles.ContainsKey(e.FileName);
            };

            // run assembly
            return assembler.Assemble(InputFileName, OutputFileName);
        }
    }
}
