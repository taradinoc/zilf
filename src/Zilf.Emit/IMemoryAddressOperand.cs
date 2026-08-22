using System.Diagnostics.CodeAnalysis;

namespace Zilf.Emit
{
    /// <summary>
    /// Supplies internal allocation identity for constant story-memory addresses.
    /// </summary>
internal interface IMemoryAddressOperand
{
    bool TryGetMemoryAddress([NotNullWhen(true)] out object? allocation, out int offset);
}

internal static class MemoryAddressOperand
{
    public static bool TryGetSumAddress(IConstantOperand left, IConstantOperand right,
        [NotNullWhen(true)] out object? allocation, out int offset)
    {
        if (left is IMemoryAddressOperand address && right is INumericOperand numeric &&
            address.TryGetMemoryAddress(out allocation, out offset))
            return TryAddOffset(numeric.Value, ref offset);
        if (right is IMemoryAddressOperand reverseAddress && left is INumericOperand reverseNumeric &&
            reverseAddress.TryGetMemoryAddress(out allocation, out offset))
            return TryAddOffset(reverseNumeric.Value, ref offset);
        allocation = null!;
        offset = 0;
        return false;
    }

    private static bool TryAddOffset(int value, ref int offset)
    {
        var result = (long)offset + value;
        if (result is < int.MinValue or > int.MaxValue)
            return false;
        offset = (int)result;
        return true;
    }
}
}
