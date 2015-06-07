using Zilf.Interpreter.Values;

namespace Zilf.Interpreter
{
    /// <summary>
    /// Indicates the primitive type of a ZilObject.
    /// </summary>
    enum PrimType
    {
        /// <summary>
        /// The primitive type is <see cref="ZilAtom"/>.
        /// </summary>
        ATOM,
        /// <summary>
        /// The primitive type is <see cref="ZilFix"/>.
        /// </summary>
        FIX,
        /// <summary>
        /// The primitive type is <see cref="ZilString"/>.
        /// </summary>
        STRING,
        /// <summary>
        /// The primitive type is <see cref="ZilList"/>.
        /// </summary>
        LIST,
        /// <summary>
        /// The primitive type is <see cref="ZilTable"/>.
        /// </summary>
        TABLE,
        /// <summary>
        /// The primitive type is <see cref="ZilVector"/>.
        /// </summary>
        VECTOR,
    }
}