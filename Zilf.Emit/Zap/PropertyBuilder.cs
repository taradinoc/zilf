namespace Zilf.Emit.Zap
{
    class PropertyBuilder : IPropertyBuilder
    {
        private readonly string name;
        private readonly int number;
        private IOperand defaultValue;

        public PropertyBuilder(string name, int number)
        {
            this.name = name;
            this.number = number;
        }

        public int Number
        {
            get { return number; }
        }

        public IOperand DefaultValue
        {
            get { return defaultValue; }
            set { defaultValue = value; }
        }

        public override string ToString()
        {
            return name;
        }
    }
}