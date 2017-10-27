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
using System.Diagnostics.Contracts;
using System.Runtime.Serialization;
using JetBrains.Annotations;
using Zilf.Diagnostics;

namespace Zilf.Language
{
    [Serializable]
    class InterpreterError : ZilError<InterpreterMessages>
    {
        [Obsolete("Use a constructor that takes a diagnostic code.")]
        public InterpreterError([NotNull] string message)
            : base(message)
        {
            Contract.Requires(message != null);
        }

        [Obsolete("Use a constructor that takes a diagnostic code.")]
        public InterpreterError([NotNull] string message, Exception innerException)
            : base(message, innerException)
        {
            Contract.Requires(message != null);
        }

        [Obsolete("Use a constructor that takes a diagnostic code.")]
        public InterpreterError(ISourceLine src, [NotNull] string message)
            : base(src, message)
        {
            Contract.Requires(message != null);
        }

        [Obsolete("Use a constructor that takes a diagnostic code.")]
        public InterpreterError([NotNull] IProvideSourceLine node, [NotNull] string message)
            : base(node.SourceLine, message)
        {
            Contract.Requires(node != null);
        }

        public InterpreterError(int code)
            : this(code, null)
        {
        }

        public InterpreterError(int code, params object[] messageArgs)
            : this(DiagnosticContext.Current.SourceLine, code, messageArgs)
        {
        }

        public InterpreterError(ISourceLine sourceLine, int code)
            : this(sourceLine, code, null)
        {
        }

        public InterpreterError(ISourceLine sourceLine, int code, params object[] messageArgs)
            : base(MakeDiagnostic(sourceLine, code, messageArgs))
        {
        }

        public InterpreterError([NotNull] IProvideSourceLine sourceLine, int code)
           : this(sourceLine, code, null)
        {
        }

        public InterpreterError([NotNull] IProvideSourceLine node, int code, params object[] messageArgs)
            : base(MakeDiagnostic(node.SourceLine, code, messageArgs))
        {
        }

        public InterpreterError([NotNull] Diagnostic diagnostic)
            : base(diagnostic)
        {
        }

        protected InterpreterError([NotNull] SerializationInfo si, StreamingContext sc)
            : base(si, sc)
        {
        }
    }
}