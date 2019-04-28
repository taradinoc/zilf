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
using System.Diagnostics;
using JetBrains.Annotations;
using Zilf.Interpreter;
using Zilf.Language;

namespace Zilf.Diagnostics
{
    sealed class DiagnosticContext
    {
        [NotNull]
        public static DiagnosticContext Current
        {
            get => threadLocalCurrent ?? (threadLocalCurrent = new DiagnosticContext());
            private set => threadLocalCurrent = value;
        }

        [ThreadStatic]
        static DiagnosticContext threadLocalCurrent;

        class Disposer : IDisposable
        {
            [CanBeNull]
            DiagnosticContext oldContext;
            [CanBeNull]
            DiagnosticContext newContext;

            public Disposer([NotNull] DiagnosticContext oldContext, [NotNull] DiagnosticContext newContext)
            {
                this.oldContext = oldContext;
                this.newContext = newContext;
            }

            /// <inheritdoc />
            /// <exception cref="T:System.InvalidOperationException">This contract is no longer on top of the stack</exception>
            public void Dispose()
            {
                if (newContext == null)
                    return;

                if (Current == newContext)
                {
                    Debug.Assert(oldContext != null, nameof(oldContext) + " != null");
                    Current = oldContext;
                    newContext = oldContext = null;
                }
                else
                {
                    throw new InvalidOperationException("Unable to restore diagnostic context");
                }
            }
        }

        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Reliability", "CA2000:Dispose objects before losing scope")]
        [NotNull]
        public static IDisposable Push([CanBeNull] ISourceLine sourceLine = null, [CanBeNull] Frame frame = null)
        {
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

        DiagnosticContext([NotNull] ISourceLine sourceLine, [CanBeNull] Frame frame)
        {
            SourceLine = sourceLine;
            Frame = frame;
        }

        [NotNull]
        public ISourceLine SourceLine { get; }
        [CanBeNull]
        public Frame Frame { get; }
    }
}
