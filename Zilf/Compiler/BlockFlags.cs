using System;

namespace Zilf.Compiler
{
    [Flags]
    internal enum BlockFlags
    {
        None = 0,

        /// <summary>
        /// Indicates that the return label was used.
        /// </summary>
        Returned = 1,
        /// <summary>
        /// Indicates that the return label expects a result on the stack. (Otherwise, the
        /// result must be discarded before branching.)
        /// </summary>
        WantResult = 2,
        /// <summary>
        /// Indicates that &lt;RETURN&gt; and &lt;AGAIN&gt; should not act on this block
        /// unless explicitly given its activation atom.
        /// </summary>
        ExplicitOnly = 4,
    }
}
