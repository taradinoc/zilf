using System;
using System.Diagnostics.Contracts;
using Zilf.Interpreter.Values;

namespace Zilf.Interpreter
{
    [ContractClassFor(typeof(IStructure))]
    abstract class IStructureContracts : IStructure
    {
        public ZilObject GetFirst()
        {
            throw new NotImplementedException();
        }

        public IStructure GetRest(int skip)
        {
            Contract.Requires(skip >= 0);
            return default(IStructure);
        }
        
        public bool IsEmpty()
        {
            throw new NotImplementedException();
        }

        public ZilObject this[int index]
        {
            get
            {
                throw new NotImplementedException();
            }
            set
            {
                throw new NotImplementedException();
            }
        }

        public int GetLength()
        {
            Contract.Ensures(Contract.Result<int>() > 0 || IsEmpty());
            return default(int);
        }

        public int? GetLength(int limit)
        {
            Contract.Ensures(Contract.Result<int?>() == null || Contract.Result<int?>().Value <= limit);
            return default(int?);
        }
    }
}