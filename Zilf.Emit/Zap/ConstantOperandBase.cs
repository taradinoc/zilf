namespace Zilf.Emit.Zap
{
    abstract class ConstantOperandBase : IConstantOperand
    {
        public IConstantOperand Add(IConstantOperand other)
        {
            return new SumOperand(this, other);
        }
    }
}
