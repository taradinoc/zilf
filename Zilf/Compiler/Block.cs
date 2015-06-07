using Zilf.Emit;
using Zilf.Interpreter;
using Zilf.Interpreter.Values;

namespace Zilf.Compiler
{
    internal sealed class Block
    {
        /// <summary>
        /// The activation atom identifying the block, or null if it is unnamed.
        /// </summary>
        public ZilAtom Name;
        /// <summary>
        /// The label to which &lt;AGAIN&gt; should branch.
        /// </summary>
        public ILabel AgainLabel;
        /// <summary>
        /// The label to which &lt;RETURN&gt; should branch, or null if
        /// it should return from the routine.
        /// </summary>
        public ILabel ReturnLabel;
        /// <summary>
        /// The context flags for &lt;RETURN&gt;.
        /// </summary>
        public BlockFlags Flags;
    }
}
