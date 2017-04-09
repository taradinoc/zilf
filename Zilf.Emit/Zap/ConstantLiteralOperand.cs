namespace Zilf.Emit.Zap
{
    class ConstantLiteralOperand : LiteralOperand, IConstantOperand
    {
        public ConstantLiteralOperand(string literal)
            : base(literal) { }

        public IConstantOperand Add(IConstantOperand other)
        {
            return new SumOperand(this, other);
        }
    }
}