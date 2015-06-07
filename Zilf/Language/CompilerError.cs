using System.Diagnostics.Contracts;

namespace Zilf.Language
{
    class CompilerError : ZilError
    {
        public CompilerError(string message)
            : base(message)
        {
            Contract.Requires(message != null);
        }

        public CompilerError(string format, params object[] args)
            : base(string.Format(format, args))
        {
            Contract.Requires(format != null);
            Contract.Requires(args != null);
        }

        public CompilerError(ISourceLine src, string message)
            : base(src, message)
        {
            Contract.Requires(message != null);
        }

        public CompilerError(ISourceLine src, string func, int minArgs, int maxArgs)
            : base(src, func, minArgs, maxArgs)
        {
            Contract.Requires(func != null);
            Contract.Requires(minArgs >= 0);
            Contract.Requires(maxArgs >= 0);
            Contract.Requires(maxArgs == 0 || maxArgs >= minArgs);
        }
    }
}