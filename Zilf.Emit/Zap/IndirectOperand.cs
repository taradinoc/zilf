using System.Diagnostics.Contracts;

namespace Zilf.Emit.Zap
{
    class IndirectOperand : IIndirectOperand
    {
        public IndirectOperand(IVariable variable)
        {
            this.Variable = variable;
        }

        [ContractInvariantMethod]
        void ObjectInvariant()
        {
            Contract.Invariant(Variable != null);
        }

        public IVariable Variable { get; private set; }

        public override string ToString()
        {
            return "'" + Variable.ToString();
        }
    }
}