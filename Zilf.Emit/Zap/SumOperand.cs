using JetBrains.Annotations;
using System.Diagnostics.Contracts;

namespace Zilf.Emit.Zap
{
    class SumOperand : IConstantOperand
    {
        public SumOperand([NotNull] IConstantOperand left, [NotNull] IConstantOperand right)
        {
            Contract.Requires(left != null);
            Contract.Requires(right != null);

            Left = left;
            Right = right;
        }

        [NotNull]
        public IConstantOperand Left { get; }

        [NotNull]
        public IConstantOperand Right { get; }

        public override string ToString()
        {
            return $"{Left}+{Right}";
        }

        [NotNull]
        public IConstantOperand Add([NotNull] IConstantOperand other)
        {
            Contract.Requires(other != null);
            return new SumOperand(this, other);
        }
    }
}
