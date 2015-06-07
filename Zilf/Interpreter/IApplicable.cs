using System.Diagnostics.Contracts;
using Zilf.Interpreter.Values;

namespace Zilf.Interpreter
{
    /// <summary>
    /// Provides a method to apply a <see cref="ZilObject"/>, such as a SUBR or FUNCTION,
    /// to a set of arguments.
    /// </summary>
    [ContractClass(typeof(IApplicableContracts))]
    interface IApplicable
    {
        /// <summary>
        /// Applies the object to the given arguments.
        /// </summary>
        /// <param name="ctx">The current context.</param>
        /// <param name="args">The unevaluated arguments.</param>
        /// <returns>The result of the application.</returns>
        /// <remarks>
        /// For FSUBRs, this is the same as <see cref="ApplyNoEval"/>.
        /// </remarks>
        ZilObject Apply(Context ctx, ZilObject[] args);

        /// <summary>
        /// Applies the object to the given arguments, which have already been evaluated.
        /// </summary>
        /// <param name="ctx">The current context.</param>
        /// <param name="args">The arguments.</param>
        /// <returns>The result of the application.</returns>
        ZilObject ApplyNoEval(Context ctx, ZilObject[] args);
    }
}