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
using JetBrains.Annotations;
using Zilf.Interpreter;
using Zilf.Language;
using System.Diagnostics.Contracts;

namespace Zilf.Diagnostics
{
    sealed class DiagnosticContext
    {
        // TODO: this should be thread-local
        public static DiagnosticContext Current { get; private set; } = new DiagnosticContext();

        class Disposer : IDisposable
        {
            DiagnosticContext oldContext, newContext;

            public Disposer(DiagnosticContext oldContext, DiagnosticContext newContext)
            {
                this.oldContext = oldContext;
                this.newContext = newContext;
            }

            /// <exception cref="InvalidOperationException">This contract is no longer on top of the stack</exception>
            public void Dispose()
            {
                if (newContext == null)
                    return;

                if (Current == newContext)
                {
                    Current = oldContext;
                    newContext = oldContext = null;
                }
                else
                {
                    throw new InvalidOperationException("Unable to restore diagnostic context");
                }
            }
        }

        [NotNull]
        public static IDisposable Push([CanBeNull] ISourceLine sourceLine = null, [CanBeNull] Frame frame = null)
        {
            Contract.Ensures(Contract.Result<IDisposable>() != null);
            var oldContext = Current;

            var newContext = new DiagnosticContext(
                sourceLine ?? oldContext.SourceLine, frame ?? oldContext.Frame);

            var disposer = new Disposer(oldContext, newContext);

            Current = newContext;
            return disposer;
        }

        DiagnosticContext()
            : this(SourceLines.TopLevel, null)
        {
        }

        DiagnosticContext(ISourceLine sourceLine, Frame frame)
        {
            SourceLine = sourceLine;
            Frame = frame;
        }

        public ISourceLine SourceLine { get; }
        public Frame Frame { get; }
    }
}
