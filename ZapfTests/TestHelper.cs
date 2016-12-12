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

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using Zapf;

namespace ZapfTests
{
    internal static class TestHelper
    {
        public static bool Assemble(string code)
        {
            MemoryStream dummy;
            return Assemble(code, out dummy);
        }

        public static bool Assemble(string code, out MemoryStream storyFile)
        {
            const string InputFileName = "Input.zap";
            const string OutputFileName = "Output.z#";
            var inputFiles = new Dictionary<string, string>() {
                { InputFileName, code }
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
            if (assembler.Assemble(InputFileName, OutputFileName))
            {
                storyFile = (from pair in outputFiles
                             let ext = Path.GetExtension(pair.Key)
                             where ext.Length == 3 && ext.StartsWith(".z")
                             select pair.Value).Single();
                return true;
            }
            else
            {
                storyFile = null;
                return false;
            }
        }
    }
}
