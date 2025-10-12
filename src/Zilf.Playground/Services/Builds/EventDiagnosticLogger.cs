/* Copyright 2010-2025 Tara McGrew
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

#nullable enable

using System;
using System.IO;
using Zilf.Diagnostics;

namespace Zilf.Playground.Services.Builds
{
    /// <summary>
    /// A diagnostic logger that fires events instead of writing to Console.
    /// </summary>
    internal sealed class EventDiagnosticLogger : IDiagnosticLogger
    {
        public event EventHandler<string>? DiagnosticLogged;

        public void Log(Diagnostic diagnostic)
        {
            ArgumentNullException.ThrowIfNull(diagnostic);

            var message = Format(diagnostic);
            DiagnosticLogged?.Invoke(this, message);

            const string SubDiagnosticIndent = "    ";

            foreach (var sd in diagnostic.SubDiagnostics)
            {
                var subMessage = SubDiagnosticIndent + Format(sd, diagnostic);
                DiagnosticLogged?.Invoke(this, subMessage);
            }

            if (diagnostic.StackTrace != null)
                DiagnosticLogged?.Invoke(this, diagnostic.StackTrace);
        }

        private static string FormatSeverity(Severity severity) => severity switch
        {
            Severity.Info => "info",
            Severity.Warning => "warning",
            Severity.Error => "error",
            Severity.Fatal => "fatal",
            _ => "unknown",
        };

        private static string Format(Diagnostic diagnostic, bool includeSourceInfo = true)
        {
            ArgumentNullException.ThrowIfNull(diagnostic);

            const string FormatWithSourceInfo = "[{0} {1}{2:0000}] {3}: {4}";
            const string FormatWithoutSourceInfo = "[{0} {1}{2:0000}] {4}";

            return string.Format(
                System.Globalization.CultureInfo.CurrentCulture,
                includeSourceInfo ? FormatWithSourceInfo : FormatWithoutSourceInfo,
                FormatSeverity(diagnostic.Severity),
                diagnostic.CodePrefix,
                diagnostic.CodeNumber,
                diagnostic.Location.SourceInfo,
                diagnostic.GetFormattedMessage());
        }

        private static string Format(Diagnostic diagnostic, Diagnostic parentDiagnostic)
        {
            ArgumentNullException.ThrowIfNull(diagnostic);
            ArgumentNullException.ThrowIfNull(parentDiagnostic);

            bool includeSourceInfo = !string.IsNullOrEmpty(diagnostic.Location.SourceInfo) &&
                diagnostic.Location != parentDiagnostic.Location &&
                diagnostic.Location.SourceInfo != parentDiagnostic.Location.SourceInfo;

            return Format(diagnostic, includeSourceInfo);
        }
    }
}
