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
using Microsoft.CodeAnalysis;

// ReSharper disable once CheckNamespace
namespace ZilfAnalyzers.Test.Helpers
{
    /// <summary>
    /// Location where the diagnostic appears, as determined by path, line number, and column number.
    /// </summary>
    public readonly struct DiagnosticResultLocation
    {
        /// <exception cref="ArgumentOutOfRangeException"><paramref name="line"/> or <paramref name="column"/> are out of range.</exception>
        public DiagnosticResultLocation(string path, int line, int column)
        {
            if (line < -1)
            {
                throw new ArgumentOutOfRangeException(nameof(line), "line must be >= -1");
            }

            if (column < -1)
            {
                throw new ArgumentOutOfRangeException(nameof(column), "column must be >= -1");
            }

            Path = path;
            Line = line;
            Column = column;
        }

        public string Path { get; }
        public int Line { get; }
        public int Column { get; }
    }

    /// <summary>
    /// Struct that stores information about a Diagnostic appearing in a source
    /// </summary>
    public struct DiagnosticResult
    {
        DiagnosticResultLocation[] locations;

        public DiagnosticResultLocation[] Locations
        {
            get => locations ??= Array.Empty<DiagnosticResultLocation>();

            set => locations = value;
        }

        public DiagnosticSeverity Severity { get; set; }

        public string Id { get; set; }

        public string Message { get; set; }

/*
        public string Path => Locations.Length > 0 ? Locations[0].Path : "";
*/

        public int Line => Locations.Length > 0 ? Locations[0].Line : -1;

        public int Column => Locations.Length > 0 ? Locations[0].Column : -1;
    }
}