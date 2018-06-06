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

        [NotNull]
        readonly List<Diagnostic> diagnostics = new List<Diagnostic>();

        [NotNull]
        public IReadOnlyCollection<Diagnostic> Diagnostics => diagnostics;
        public int ErrorCount => Diagnostics.Count(d => d.Severity == Severity.Error || d.Severity == Severity.Fatal);
        public int WarningCount => Diagnostics.Count(d => d.Severity == Severity.Warning);

        public bool WarningsAsErrors { get; set; }

        [NotNull]
        public IDiagnosticFormatter Formatter { get; }
        [NotNull]
        public TextWriter OutputWriter { get; }

        public event EventHandler TooManyErrors;

        public DiagnosticManager([CanBeNull] IDiagnosticFormatter formatter = null, [CanBeNull] TextWriter outputWriter = null)
        {
            Formatter = formatter ?? new DefaultDiagnosticFormatter();
            OutputWriter = outputWriter ?? Console.Error;
        }

        public void Handle([NotNull] Diagnostic diag)
        {
            if (WarningsAsErrors && diag.Severity == Severity.Warning)
            {
                diag = diag.WithSeverity(Severity.Error);
            }

            diagnostics.Add(diag);

            if (diag.Severity == Severity.Error)
            {
                if (ErrorCount >= MaxErrorCount)
                {
                    TooManyErrors?.Invoke(this, EventArgs.Empty);
                }
            }

            OutputWriter.WriteLine(Formatter.Format(diag));
        }
    }
}
