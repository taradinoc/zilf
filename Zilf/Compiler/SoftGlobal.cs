
namespace Zilf.Compiler
{
    internal sealed class SoftGlobal
    {
        /// <summary>
        /// True if the global is a word; false if it's a byte.
        /// </summary>
        public bool IsWord;
        /// <summary>
        /// The word index (if <see cref="IsWord"/> is true) or byte index (otherwise)
        /// of the global, relative to <see cref="CompileCtx.SoftGlobalsTable"/>.
        /// </summary>
        public int Offset;
    }
}
