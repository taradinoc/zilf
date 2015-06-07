using System.Diagnostics.Contracts;
using Zilf.Interpreter.Values;

namespace Zilf.Interpreter
{
    [ContractClassFor(typeof(IApplicable))]
    abstract class IApplicableContracts : IApplicable
    {
        public ZilObject Apply(Context ctx, ZilObject[] args)
        {
            Contract.Requires(ctx != null);
            Contract.Requires(args != null);
            Contract.Requires(Contract.ForAll(args, a => a != null));
            Contract.Ensures(Contract.Result<ZilObject>() != null);
            return default(ZilObject);
        }

        public ZilObject ApplyNoEval(Context ctx, ZilObject[] args)
        {
            Contract.Requires(ctx != null);
            Contract.Requires(args != null);
            Contract.Requires(Contract.ForAll(args, a => a != null));
            Contract.Ensures(Contract.Result<ZilObject>() != null);
            return default(ZilObject);
        }
    }
}