using System.Diagnostics.Contracts;

namespace Zilf.Language
{
    class SyntaxError : InterpreterError
    {
        public SyntaxError(string filename, int line, string message)
            : base(new FileSourceLine(filename, line), "syntax error: " + message)
        {
            Contract.Requires(filename != null);
            Contract.Requires(message != null);
        }
    }
}