namespace Zilf.Emit.Zap
{
    class LocalBuilder : ILocalBuilder
    {
        private readonly string name;
        private IOperand defaultValue;

        public LocalBuilder(string name)
        {
            this.name = name;
        }

        public IIndirectOperand Indirect
        {
            get { return new IndirectOperand(this); }
        }

        public IOperand DefaultValue
        {
            get { return defaultValue; }
            set { defaultValue = value; }
        }

        public string Name
        {
            get { return name; }
        }

        public override string ToString()
        {
            return name;
        }
    }
}