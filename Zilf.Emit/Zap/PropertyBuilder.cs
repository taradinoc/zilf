namespace Zilf.Emit.Zap
{
    class PropertyBuilder : IPropertyBuilder
    {
        readonly string name;
        readonly int number;
        IOperand defaultValue;

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