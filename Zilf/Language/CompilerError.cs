/* Copyright 2010, 2015 Jesse McGrew
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
using System.Diagnostics.Contracts;
using Zilf.Diagnostics;

namespace Zilf.Language
{
    class CompilerError : ZilError<CompilerMessages>
    {
        [Obsolete("Use a constructor that takes a diagnostic code.")]
        public CompilerError(string message)
            : base(message)
        {
            Contract.Requires(message != null);
        }

        [Obsolete("Use a constructor that takes a diagnostic code.")]
        public CompilerError(string format, params object[] args)
            : base(string.Format(format, args))
        {
        }

        public CompilerError(ISourceLine src, string message)
            : base(src, message)
        {
            Contract.Requires(message != null);
        }

        [Obsolete("Use a constructor that takes a diagnostic code.")]
        public CompilerError(string func, int minArgs, int maxArgs)
            : base(null, func, minArgs, maxArgs)
        {
            Contract.Requires(func != null);
            Contract.Requires(minArgs >= 0);
            Contract.Requires(maxArgs >= 0);
            Contract.Requires(maxArgs == 0 || maxArgs >= minArgs);
        }

        public CompilerError(int code)
            : this(code, null)
        {
        }

        public CompilerError(int code, params object[] messageArgs)
            : this(DiagnosticContext.Current.SourceLine, code, messageArgs)
        {
        }

        public CompilerError(ISourceLine sourceLine, int code)
            : this(sourceLine, code, null)
        {
        }

        public CompilerError(ISourceLine sourceLine, int code, params object[] messageArgs)
            : base(MakeDiagnostic(sourceLine, code, messageArgs))
        {
        }

        public CompilerError(IProvideSourceLine sourceLine, int code)
           : this(sourceLine, code, null)
        {
        }

        public CompilerError(IProvideSourceLine node, int code, params object[] messageArgs)
            : base(MakeDiagnostic(node.SourceLine, code, messageArgs))
        {
        }
    }
}