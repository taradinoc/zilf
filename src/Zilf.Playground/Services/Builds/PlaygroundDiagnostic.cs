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

using Zilf.Diagnostics;

namespace Zilf.Playground.Services.Builds
{
    /// <summary>
    /// Minimal diagnostic info for displaying editor squiggles.
    /// Line numbers are 1-based. Columns are not provided by the compiler; consumers
    /// should highlight the full line or infer columns as needed.
    /// </summary>
    public sealed class PlaygroundDiagnostic
    {
        public string Path { get; init; } = string.Empty;
        public int Line { get; init; }
        public Severity Severity { get; init; }
        public string Code { get; init; } = string.Empty;
        public string Message { get; init; } = string.Empty;
    }
}
