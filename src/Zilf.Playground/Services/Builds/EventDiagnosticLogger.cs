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
        public event EventHandler<PlaygroundDiagnostic>? DiagnosticFound;

        public void Log(Diagnostic diagnostic)
        {
            ArgumentNullException.ThrowIfNull(diagnostic);

            var message = Format(diagnostic);
            DiagnosticLogged?.Invoke(this, message);
            TryEmitStructured(diagnostic);

            const string SubDiagnosticIndent = "    ";

            foreach (var sd in diagnostic.SubDiagnostics)
            {
                var subMessage = SubDiagnosticIndent + Format(sd, diagnostic);
                DiagnosticLogged?.Invoke(this, subMessage);
                TryEmitStructured(sd);
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

        private void TryEmitStructured(Diagnostic diagnostic)
        {
            // Expect SourceInfo format like "path:line"; if parsing fails, skip emitting.
            var src = diagnostic.Location?.SourceInfo;
            if (string.IsNullOrEmpty(src))
                return;

            var (path, line) = ParseSourceInfo(src!);
            if (path == null || line <= 0)
                return;

            DiagnosticFound?.Invoke(this, new PlaygroundDiagnostic
            {
                Path = path,
                Line = line,
                Severity = diagnostic.Severity,
                Code = Diagnostic.FormatCode(diagnostic.CodePrefix, diagnostic.CodeNumber),
                Message = diagnostic.GetFormattedMessage()
            });
        }

        private static (string? path, int line) ParseSourceInfo(string sourceInfo)
        {
            // Expected most common format from FileSourceLine: "fileName:lineNumber"
            // Use last ':' to split (safe for file names that may contain ':')
            var idx = sourceInfo.LastIndexOf(':');
            if (idx <= 0 || idx >= sourceInfo.Length - 1)
                return (null, 0);

            var path = sourceInfo.Substring(0, idx);
            var tail = sourceInfo.Substring(idx + 1);
            if (int.TryParse(tail, out var line))
                return (path, line);

            return (null, 0);
        }
    }
}
