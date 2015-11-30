namespace Zilf.Emit.Zap
{
    class NumericConstantOperand : INumericOperand
    {
        private readonly string literal;
        private readonly int value;

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