namespace Zilf.Emit.Zap
{
    class VariableOperand : LiteralOperand, IVariable
    {
        public VariableOperand(string literal)
            : base(literal)
        {
        }

        public IIndirectOperand Indirect
        {
            get { return new IndirectOperand(this); }
        }
    }
}