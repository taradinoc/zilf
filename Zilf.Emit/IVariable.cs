using System.Diagnostics.Contracts;

namespace Zilf.Emit
{
    [ContractClass(typeof(IVariableContracts))]
    public interface IVariable : IOperand
    {
        IIndirectOperand Indirect { get; }
    }

    [ContractClassFor(typeof(IVariable))]
    abstract class IVariableContracts : IVariable
    {
        [ContractInvariantMethod]
        void ObjectInvariant()
        {
            Contract.Invariant(Indirect != null);
        }

        public IIndirectOperand Indirect
        {
            get { throw new System.NotImplementedException(); }
        }
    }
}