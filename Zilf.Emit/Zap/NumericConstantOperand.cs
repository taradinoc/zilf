namespace Zilf.Emit.Zap
{
    class NumericConstantOperand : ConstantOperandBase, INumericOperand
    {
        readonly string literal;
        readonly int value;

        public NumericConstantOperand(string literal, int value)
        {
            this.literal = literal;
            this.value = value;
        }

        public override string ToString()
        {
            return literal;
        }

        public int Value
        {
            get { return value; }
        }
    }
}