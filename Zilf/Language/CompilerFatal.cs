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
    class CompilerFatal : ZilFatal<CompilerMessages>
    {
        public CompilerFatal(int code)
            : this(code, null)
        {
        }

        public CompilerFatal(int code, [ItemNotNull] params object[] messageArgs)
            : this(DiagnosticContext.Current.SourceLine, code, messageArgs)
        {
        }

        public CompilerFatal(ISourceLine sourceLine, int code)
            : this(sourceLine, code, null)
        {
        }

        public CompilerFatal(ISourceLine sourceLine, int code, [ItemNotNull] params object[] messageArgs)
            : base(MakeDiagnostic(sourceLine, code, messageArgs))
        {
        }

        public CompilerFatal([NotNull] IProvideSourceLine sourceLine, int code)
           : this(sourceLine, code, null)
        {
            Contract.Requires(sourceLine != null);
        }

        public CompilerFatal([NotNull] IProvideSourceLine node, int code, params object[] messageArgs)
            : base(MakeDiagnostic(node.SourceLine, code, messageArgs))
        {
            Contract.Requires(node != null);
        }

        [UsedImplicitly]
        public CompilerFatal([NotNull] Diagnostic diagnostic)
            : base(diagnostic)
        {
            Contract.Requires(diagnostic != null);
        }

        protected CompilerFatal([NotNull] SerializationInfo si, StreamingContext sc)
            : base(si, sc)
        {
            Contract.Requires(si != null);
        }
    }
}