namespace Zilf.Emit
{
    public interface IPropertyBuilder : IConstantOperand
    {
        IOperand DefaultValue { get; set; }
    }
}