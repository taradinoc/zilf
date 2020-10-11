/* Copyright 2010-2018 Jesse McGrew
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
using JetBrains.Annotations;

namespace Zilf.Diagnostics
{
    sealed class DiagnosticManager
    {
        const int MaxErrorCount = 100;

        readonly List<Diagnostic> diagnostics = new List<Diagnostic>();

        readonly List<Diagnostic> suppressedDiagnostics = new List<Diagnostic>();

        readonly HashSet<string> suppressions = new HashSet<string>();

        bool suppressAllTheThings = false;

        public IReadOnlyCollection<Diagnostic> Diagnostics => diagnostics;
        public int ErrorCount => Diagnostics.Count(d => d.Severity == Severity.Error || d.Severity == Severity.Fatal);
        public int WarningCount => Diagnostics.Count(d => d.Severity == Severity.Warning);
        public int SuppressedWarningCount => suppressedDiagnostics.Count(d => d.Severity == Severity.Warning);

        public bool WarningsAsErrors { get; set; }
        public bool SuppressNoisyWarnings { get; set; }

        public IDiagnosticFormatter Formatter { get; }
        public TextWriter OutputWriter { get; }

        public event EventHandler? TooManyErrors;

        public DiagnosticManager(IDiagnosticFormatter? formatter = null, TextWriter? outputWriter = null)
        {
            Formatter = formatter ?? new DefaultDiagnosticFormatter();
            OutputWriter = outputWriter ?? Console.Error;
        }

        public void Suppress(string code)
        {
            if (!suppressAllTheThings)
                suppressions.Add(code);
        }

        public void Suppress(IEnumerable<string> codes)
        {
            if (!suppressAllTheThings)
                suppressions.UnionWith(codes);
        }

        public void SuppressAll()
        {
            suppressAllTheThings = true;
            suppressions.Clear();
        }

        public void SuppressNone()
        {
            suppressAllTheThings = false;
            suppressions.Clear();
        }

        public void Handle(Diagnostic diag)
        {
            if (WarningsAsErrors && diag.Severity == Severity.Warning)
            {
                diag = diag.WithSeverity(Severity.Error);
            }

            diagnostics.Add(diag);

            if (diag.Severity == Severity.Error && ErrorCount >= MaxErrorCount)
            {
                TooManyErrors?.Invoke(this, EventArgs.Empty);
            }

            if (IsSuppressed(diag))
            {
                suppressedDiagnostics.Add(diag);
            }
            else
            {
                OutputWriter.WriteLine(Formatter.Format(diag));
            }
        }

        private bool IsSuppressed(Diagnostic diag)
        {
            if (diag.Severity >= Severity.Error)
                return false;

            if (SuppressNoisyWarnings && diag.Severity == Severity.Warning && diag.Noisy)
                return true;

            return suppressAllTheThings || suppressions.Contains(diag.Code);
        }
    }
}
