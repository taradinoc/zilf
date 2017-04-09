using System.Diagnostics.Contracts;

namespace Zilf.Emit
{
    [ContractClass(typeof(ITableBuilderContracts))]
    public interface ITableBuilder : IConstantOperand
    {
        void AddByte(byte value);
        void AddByte(IOperand value);
        void AddShort(short value);
        void AddShort(IOperand value);
    }

    [ContractClassFor(typeof(ITableBuilder))]
    abstract class ITableBuilderContracts : ITableBuilder
    {
        public abstract IConstantOperand Add(IConstantOperand other);

        public void AddByte(byte value)
        {
            throw new System.NotImplementedException();
        }

        public void AddByte(IOperand value)
        {
            Contract.Requires(value != null);
        }

        public void AddShort(short value)
        {
            throw new System.NotImplementedException();
        }

        public void AddShort(IOperand value)
        {
            Contract.Requires(value != null);
        }
    }
}