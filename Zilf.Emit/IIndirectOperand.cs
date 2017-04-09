using System.Diagnostics.Contracts;

namespace Zilf.Emit
{
    [ContractClass(typeof(IIndirectOperandContracts))]
    public interface IIndirectOperand : IConstantOperand
    {
        IVariable Variable { get; }
    }

    [ContractClassFor(typeof(IIndirectOperand))]
    abstract class IIndirectOperandContracts : IIndirectOperand
    {
        [ContractInvariantMethod]
        void ObjectInvariant()
        {
            Contract.Invariant(Variable != null);
        }

        public abstract IVariable Variable { get; }
        public abstract IConstantOperand Add(IConstantOperand other);
    }
}