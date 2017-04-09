namespace Zilf.Emit.Zap
{
    class FlagBuilder : ConstantOperandBase, IFlagBuilder
    {
        readonly string name;
        readonly int number;

        public FlagBuilder(string name, int number)
        {
            this.name = name;
            this.number = number;
        }

        public int Number
        {
            get { return number; }
        }

        public override string ToString()
        {
            return name;
        }
    }
}