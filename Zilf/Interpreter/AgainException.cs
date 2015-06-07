
namespace Zilf.Interpreter
{
    /// <summary>
    /// Indicates that an inner block is repeating.
    /// </summary>
    class AgainException : ControlException
    {
        public AgainException()
            : base("AGAIN")
        {
        }
    }
}
