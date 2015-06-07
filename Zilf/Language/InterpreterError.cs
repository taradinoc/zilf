using System;
using System.Diagnostics.Contracts;

namespace Zilf.Language
{
    class InterpreterError : ZilError
    {
        public InterpreterError(string message)
            : base(message)
        {
            Contract.Requires(message != null);
        }

        public InterpreterError(string message, Exception innerException)
            : base(message, innerException)
        {
            Contract.Requires(message != null);
        }

        public InterpreterError(ISourceLine src, string message)
            : base(src, message)
        {
            Contract.Requires(message != null);
        }

        public InterpreterError(IProvideSourceLine node, string message)
            : base(node.SourceLine, message)
        {
            Contract.Requires(node != null);
        }

        public InterpreterError(ISourceLine src, string func, int minArgs, int maxArgs)
            : base(src, func, minArgs, maxArgs)
        {
            Contract.Requires(func != null);
            Contract.Requires(minArgs >= 0);
            Contract.Requires(maxArgs >= 0);
            Contract.Requires(maxArgs == 0 || maxArgs >= minArgs);
        }

        public InterpreterError(IProvideSourceLine node, string func, int minArgs, int maxArgs)
            : base(node.SourceLine, func, minArgs, maxArgs)
        {
            Contract.Requires(node != null);
            Contract.Requires(func != null);
            Contract.Requires(minArgs >= 0);
            Contract.Requires(maxArgs >= 0);
            Contract.Requires(maxArgs == 0 || maxArgs >= minArgs);
        }

        public InterpreterError(string func, int minArgs, int maxArgs)
            : base(null, func, minArgs, maxArgs)
        {
            Contract.Requires(func != null);
            Contract.Requires(minArgs >= 0);
            Contract.Requires(maxArgs >= 0);
            Contract.Requires(maxArgs == 0 || maxArgs >= minArgs);
        }
    }
}