using System.Diagnostics.Contracts;

namespace Zilf.Emit
{
    [ContractClass(typeof(IIndirectOperandContracts))]
    public interface IIndirectOperand : IOperand
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

        public IVariable Variable
        {
            get { throw new System.NotImplementedException(); }
        }
    }
}