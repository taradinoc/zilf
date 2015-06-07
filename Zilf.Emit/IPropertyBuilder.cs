namespace Zilf.Emit
{
    public interface IPropertyBuilder : IOperand
    {
        IOperand DefaultValue { get; set; }
    }
}