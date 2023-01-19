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
using System.Runtime.Serialization;
using Zilf.Diagnostics;

namespace Zilf.Language
{
    [Serializable]
    sealed class CompilerFatal : ZilFatal<CompilerMessages>
    {
        public CompilerFatal(int code)
            : this(code, null)
        {
        }

        public CompilerFatal(int code, params object[]? messageArgs)
            : this(DiagnosticContext.Current.SourceLine, code, messageArgs)
        {
        }

        public CompilerFatal(ISourceLine sourceLine, int code)
            : this(sourceLine, code, null)
        {
        }

        public CompilerFatal(ISourceLine sourceLine, int code, params object[]? messageArgs)
            : base(MakeDiagnostic(sourceLine, code, messageArgs))
        {
        }

        public CompilerFatal(IProvideSourceLine sourceLine, int code)
            : this(sourceLine, code, null)
        {
        }

        public CompilerFatal(IProvideSourceLine node, int code, params object[]? messageArgs)
            : base(MakeDiagnostic(node.SourceLine, code, messageArgs))
        {
        }

        public CompilerFatal(Diagnostic diagnostic)
            : base(diagnostic)
        {
        }

        public CompilerFatal()
        {
        }

        public CompilerFatal(string message) : base(message)
        {
        }

        public CompilerFatal(string message, Exception innerException) : base(message, innerException)
        {
        }
    }
}