namespace Zilf.Emit.Zap
{
    class LiteralOperand : IOperand
    {
        readonly string literal;

        public LiteralOperand(string literal)
        {
            this.literal = literal;
        }

        public override string ToString()
        {
            return literal;
        }
    }
}