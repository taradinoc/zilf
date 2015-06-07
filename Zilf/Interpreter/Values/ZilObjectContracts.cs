using System.Diagnostics.Contracts;

namespace Zilf.Interpreter.Values
{
    [ContractClassFor(typeof(ZilObject))]
    abstract class ZilObjectContracts : ZilObject
    {
        public override ZilAtom GetTypeAtom(Context ctx)
        {
            Contract.Ensures(Contract.Result<ZilAtom>() != null);
            return default(ZilAtom);
        }

        public override ZilObject GetPrimitive(Context ctx)
        {
            Contract.Ensures(Contract.Result<ZilObject>() != null);
            return default(ZilObject);
        }
    }
}