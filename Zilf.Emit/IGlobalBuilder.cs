namespace Zilf.Emit
{
    public interface IGlobalBuilder : IVariable
    {
        IOperand DefaultValue { get; set; }
    }
}