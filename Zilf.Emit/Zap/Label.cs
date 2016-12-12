namespace Zilf.Emit.Zap
{
    class Label : ILabel
    {
        readonly string symbol;

        public Label(string symbol)
        {
            this.symbol = symbol;
        }

        public override string ToString()
        {
            return symbol;
        }
    }
}