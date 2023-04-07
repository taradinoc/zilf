using System.ComponentModel;

namespace System.Runtime.CompilerServices
{
    /// <summary>
    /// A marker class required by the compiler to use <c>init</c> or <c>record</c>.
    /// </summary>
    [EditorBrowsable(EditorBrowsableState.Never)]
    internal static class IsExternalInit { }
}
