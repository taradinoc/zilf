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
