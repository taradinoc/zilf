using System.Diagnostics.Contracts;
using System.Diagnostics;
using JetBrains.Annotations;

namespace Zilf.Emit.Zap
{
    class IndirectOperand : ConstantOperandBase, IIndirectOperand
    {
        public IndirectOperand(IVariable variable)
        {
            Variable = variable;
        }

        [ContractInvariantMethod]
        [Conditional("CONTRACTS_FULL")]
        void ObjectInvariant()
        {
            Contract.Invariant(Variable != null);
        }

        [NotNull]
        public IVariable Variable { get; }

        public override string ToString()
        {
            return "'" + Variable;
        }
    }
}