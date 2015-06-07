
namespace Zilf.Compiler.Builtins
{
    internal enum BuiltinArgType
    {
        /// <summary>
        /// An IOperand or other value ready to pass into the spec method.
        /// </summary>
        Operand,
        /// <summary>
        /// A ZilObject that must be evaluated before passing to the spec method.
        /// </summary>
        NeedsEval,
    }
}
