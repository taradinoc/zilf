namespace Zilf
{
    // This placeholder is complemented by a generated BuildInfo.g.cs at build time.
    // It ensures successful compilation in environments where code generation
    // hasn't produced the file yet (e.g., IDE design-time build).
    internal static partial class BuildInfo
    {
        // Zero ticks (MinValue) means "unknown"
        public static System.DateTime BuildTimestampUtc { get; private set; } = System.DateTime.MinValue;
    }
}
