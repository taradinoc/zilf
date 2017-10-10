/* Copyright 2010-2017 Jesse McGrew
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
using JetBrains.Annotations;

namespace Zilf.Diagnostics
{
    sealed class DiagnosticManager
    {
        public int ErrorCount { get; private set; }
        public int? MaxErrorCount { get; set; } = 100;
        public int WarningCount { get; private set; }

        public IDiagnosticFormatter Formatter { get; }
        public TextWriter OutputWriter { get; }

        public event EventHandler TooManyErrors;

        public DiagnosticManager([CanBeNull] IDiagnosticFormatter formatter = null, [CanBeNull] TextWriter outputWriter = null)
        {
            Formatter = formatter ?? new DefaultDiagnosticFormatter();
            OutputWriter = outputWriter ?? Console.Error;
        }

        public void Handle([NotNull] Diagnostic diag)
        {
            switch (diag.Severity)
            {
                case Severity.Fatal:
                    ErrorCount++;
                    break;

                case Severity.Error:
                    ErrorCount++;
                    if (ErrorCount >= MaxErrorCount)
                    {
                        TooManyErrors?.Invoke(this, EventArgs.Empty);
                    }
                    break;

                case Severity.Warning:
                    WarningCount++;
                    break;
            }

            OutputWriter.WriteLine(Formatter.Format(diag));
        }
    }
}
