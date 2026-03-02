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

using System.Text;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Zapf.Parsing;
using Zapf.Parsing.Diagnostics;
using Zapf.Parsing.Instructions;

namespace Zapf.Tests
{
    [TestClass, TestCategory("Assembler")]
    public class LimitTests
    {
        [TestMethod]
        public void Too_Many_GVARs_Should_Cause_An_Error()
        {
            const string SCodeTemplate = @"
    .NEW 5

GLOBAL:: .TABLE
{0}
    .ENDT

    .FUNCT GO
START::
    QUIT

    .END";

            var tooManyGvars = new StringBuilder();

            for (int i = 0; i < 500; i++)
                tooManyGvars.AppendFormat("    .GVAR MY-GLOBAL-{0}={0}\n", i);

            var code = string.Format(SCodeTemplate, tooManyGvars);
            Assert.IsFalse(TestHelper.Assemble(code), "Should not compile.");
        }

        [TestMethod]
        public void Too_Many_OBJECTs_Should_Cause_An_Error()
        {
            const string SCodeTemplate = @"
OBJECT:: .TABLE
    .WORD 0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0
    .WORD 0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0
    .WORD 0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0
    .WORD 0,0,0,0,0,0,0,0,0,0,0,0,0,0,0
{0}
    .ENDT

VOCAB::
IMPURE::

    .FUNCT GO
START::
    QUIT

    .END";

            var tooManyObjects = new StringBuilder();

            for (int i = 0; i < 500; i++)
                tooManyObjects.AppendFormat("    .OBJECT MY-OBJECT-{0},0,0,0,0,0,0,0\n", i);

            var code = string.Format(SCodeTemplate, tooManyObjects);
            Assert.IsFalse(TestHelper.Assemble(code), "Should not compile.");
        }

        [TestMethod]
        public void Unicode_Minus_In_Number_Should_Report_Syntax_Error_Not_Throw()
        {
            const string prefix = @"
    .NEW 5

GLOBAL:: .TABLE
    .GVAR TEST-GLOBAL=";

            const string suffix = @"
    .ENDT

    .FUNCT GO
START::
    QUIT

    .END";

            var code = prefix + "\u22121" + suffix;

            Assert.IsFalse(TestHelper.Assemble(code), "Unicode minus should fail as a syntax error.");
        }

        [TestMethod]
        public void ASCII_Minus_In_Number_Should_Parse_Independently_Of_Current_Culture()
        {
            const string code = ".WORD -1\n";

            var customCulture = (CultureInfo)CultureInfo.GetCultureInfo("sv-SE").Clone();
            customCulture.NumberFormat.NegativeSign = "\u2212";

            using var scope = new CultureScope(customCulture);
            using var stream = new MemoryStream(Encoding.UTF8.GetBytes(code));

            var sink = new RecordingErrorSink();
            var parser = new ZapParser(
                sink,
                new Dictionary<string, KeyValuePair<ushort, ZOpAttribute>>(),
                (_, _) => new Dictionary<string, KeyValuePair<ushort, ZOpAttribute>>(),
                informMode: false);

            var result = parser.Parse(stream, "Input.zap");

            Assert.AreEqual(0, result.NumberOfSyntaxErrors, "ASCII minus should parse regardless of current culture.");
            Assert.AreEqual(0, sink.ErrorCount, "No parser errors should be reported for ASCII minus.");
        }

        sealed class CultureScope : IDisposable
        {
            readonly CultureInfo originalCulture;
            readonly CultureInfo originalUiCulture;

            public CultureScope(CultureInfo culture)
            {
                originalCulture = CultureInfo.CurrentCulture;
                originalUiCulture = CultureInfo.CurrentUICulture;
                CultureInfo.CurrentCulture = culture;
                CultureInfo.CurrentUICulture = culture;
            }

            public void Dispose()
            {
                CultureInfo.CurrentCulture = originalCulture;
                CultureInfo.CurrentUICulture = originalUiCulture;
            }
        }

        sealed class RecordingErrorSink : IErrorSink
        {
            public int ErrorCount { get; private set; }

            public void HandleSeriousError(SeriousError ser)
            {
                ErrorCount++;
            }

            public void HandleWarning(Warning warning)
            {
            }
        }
    }
}
