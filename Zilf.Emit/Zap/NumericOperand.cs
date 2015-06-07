namespace Zilf.Emit.Zap
{
    class NumericOperand : IOperand
    {
        private readonly int value;

        public NumericOperand(int value)
        {
            this.value = value;
        }

        public override string ToString()
        {
            return value.ToString();
        }
    }
}