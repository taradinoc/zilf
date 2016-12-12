namespace Zilf.Emit.Zap
{
    class NumericOperand : INumericOperand
    {
        readonly int value;

        public NumericOperand(int value)
        {
            this.value = value;
        }

        public override string ToString()
        {
            return value.ToString();
        }

        public int Value
        {
            get { return value; }
        }
    }
}