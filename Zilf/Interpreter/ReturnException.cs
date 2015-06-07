using System.Diagnostics.Contracts;
using Zilf.Interpreter.Values;

namespace Zilf.Interpreter
{
    /// <summary>
    /// Indicates that an inner block is returning.
    /// </summary>
    class ReturnException : ControlException
    {
        private readonly ZilObject value;

        public ReturnException(ZilObject value)
            : base("RETURN")
        {
            Contract.Requires(value != null);

            this.value = value;
        }

        [ContractInvariantMethod]
        private void ObjectInvariant()
        {
            Contract.Invariant(value != null);
        }

        public ZilObject Value
        {
            get
            {
                Contract.Ensures(Contract.Result<ZilObject>() != null);
                return value;
            }
        }
    }
}
