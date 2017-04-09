namespace Zilf.Emit
{
    public interface INumericOperand : IConstantOperand
    {
        int Value { get; }
    }
}
