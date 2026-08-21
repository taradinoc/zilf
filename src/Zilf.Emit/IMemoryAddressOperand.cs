namespace Zilf.Emit
{
    /// <summary>
    /// Supplies internal allocation identity for constant story-memory addresses.
    /// </summary>
    internal interface IMemoryAddressOperand
    {
        bool TryGetMemoryAddress(out object allocation, out int offset);
    }
}
