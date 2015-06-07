using System;

namespace Zilf.Interpreter
{
    /// <summary>
    /// A base class for exceptions used to implement wacky interpreter flow control.
    /// </summary>
    abstract class ControlException : Exception
    {
        protected ControlException(string name)
            : base(name)
        {
        }
    }
}
