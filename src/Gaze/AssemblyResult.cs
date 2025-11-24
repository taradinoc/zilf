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

using System.Diagnostics.CodeAnalysis;

namespace Gaze
{
    public readonly struct AssemblyResult
    {
        public AssemblyResult(bool success, Context? context)
        {
            Success = success;
            Context = context;
        }

        public static readonly AssemblyResult Failed = new(false, null);

        [MemberNotNullWhen(true, nameof(Context))]
        public bool Success { get; }
        public Context? Context { get; }
    }
}
