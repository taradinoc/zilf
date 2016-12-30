using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Zilf.Interpreter;
using Zilf.Language;

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

            public void Dispose()
            {
                if (newContext != null)
                {
                    if (DiagnosticContext.Current == newContext)
                    {
                        DiagnosticContext.Current = oldContext;
                        newContext = oldContext = null;
                    }
                    else
                    {
                        throw new InvalidOperationException("Unable to restore diagnostic context");
                    }
                }
            }
        }

        public static IDisposable Push(ISourceLine sourceLine = null, Frame frame = null)
        {
            var oldContext = DiagnosticContext.Current;

            var newContext = new DiagnosticContext(
                sourceLine ?? oldContext.SourceLine, frame ?? oldContext.Frame);

            var disposer = new Disposer(oldContext, newContext);

            DiagnosticContext.Current = newContext;
            return disposer;
        }

        DiagnosticContext()
            : this(SourceLines.TopLevel, null)
        {
        }

        DiagnosticContext(ISourceLine sourceLine, Frame frame)
        {
            this.SourceLine = sourceLine;
            this.Frame = frame;
        }

        public ISourceLine SourceLine { get; }
        public Frame Frame { get; }
    }
}
