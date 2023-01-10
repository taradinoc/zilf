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
using System.Globalization;
using System.IO;
using Zilf.Common;

namespace Zilf.Diagnostics
{
    public class DefaultDiagnosticLogger : IDiagnosticLogger
    {
        public TextWriter Writer { get; init; } = Console.Error;

        public DefaultDiagnosticLogger()
        {
        }

        public void Log(Diagnostic diagnostic)
        {
            if (diagnostic == null)
                throw new ArgumentNullException(nameof(diagnostic));

            Writer.WriteLine(Format(diagnostic));

            foreach (var sd in diagnostic.SubDiagnostics)
                Writer.WriteLine(Format(sd, diagnostic));

            if (diagnostic.StackTrace != null)
                Writer.WriteLine(diagnostic.StackTrace);
        }

        protected static string FormatSeverity(Severity severity) => severity switch
        {
            Severity.Info => "info",
            Severity.Warning => "warning",
            Severity.Error => "error",
            Severity.Fatal => "fatal",
            _ => throw UnhandledCaseException.FromEnum(severity),
        };

        protected static string Format(Diagnostic diagnostic, bool includeSourceInfo = true)
        {
            if (diagnostic == null)
                throw new ArgumentNullException(nameof(diagnostic));

            const string SFormatWithSourceInfo = "[{0} {1}{2:0000}] {3}: {4}";
            const string SFormatWithoutSourceInfo = "[{0} {1}{2:0000}] {4}";

            return string.Format(
                CultureInfo.CurrentCulture,
                includeSourceInfo ? SFormatWithSourceInfo : SFormatWithoutSourceInfo,
                FormatSeverity(diagnostic.Severity),
                diagnostic.CodePrefix,
                diagnostic.CodeNumber,
                diagnostic.Location.SourceInfo,
                diagnostic.GetFormattedMessage());
        }

        protected static string Format(Diagnostic diagnostic, Diagnostic parentDiagnostic)
        {
            if (diagnostic == null)
                throw new ArgumentNullException(nameof(diagnostic));

            if (parentDiagnostic == null)
                throw new ArgumentNullException(nameof(parentDiagnostic));

            bool includeSourceInfo = !string.IsNullOrEmpty(diagnostic.Location.SourceInfo) &&
                diagnostic.Location != parentDiagnostic.Location &&
                diagnostic.Location.SourceInfo != parentDiagnostic.Location.SourceInfo;

            return Format(diagnostic, includeSourceInfo);
        }
    }
}
