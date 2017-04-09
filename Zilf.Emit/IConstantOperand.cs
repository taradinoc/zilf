using System.Diagnostics.Contracts;

namespace Zilf.Emit
{
    [ContractClass(typeof(IConstantOperandContracts))]
    public interface IConstantOperand : IOperand
    {
        IConstantOperand Add(IConstantOperand other);
    }

    [ContractClassFor(typeof(IVariable))]
    abstract class IConstantOperandContracts : IConstantOperand
    {
        public IConstantOperand Add(IConstantOperand other)
        {
            Contract.Requires(other != null);
            Contract.Ensures(Contract.Result<IConstantOperand>() != null);
            return default(IConstantOperand);
        }
    }
}