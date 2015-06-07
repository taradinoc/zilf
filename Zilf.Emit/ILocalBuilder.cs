namespace Zilf.Emit
{
    public interface ILocalBuilder : IVariable
    {
        IOperand DefaultValue { get; set; }
    }
}