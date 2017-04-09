using System.Diagnostics.Contracts;

namespace Zilf.Emit
{
    [ContractClass(typeof(IObjectBuilderContracts))]
    public interface IObjectBuilder : IConstantOperand
    {
        string DescriptiveName { get; set; }
        IObjectBuilder Parent { get; set; }
        IObjectBuilder Child { get; set; }
        IObjectBuilder Sibling { get; set; }

        void AddByteProperty(IPropertyBuilder prop, IOperand value);
        void AddWordProperty(IPropertyBuilder prop, IOperand value);
        ITableBuilder AddComplexProperty(IPropertyBuilder prop);

        void AddFlag(IFlagBuilder flag);
    }

    [ContractClassFor(typeof(IObjectBuilder))]
    abstract class IObjectBuilderContracts : IObjectBuilder
    {
        public string DescriptiveName { get; set; }
        public IObjectBuilder Parent { get; set; }
        public IObjectBuilder Child { get; set; }
        public IObjectBuilder Sibling { get; set; }

        public void AddByteProperty(IPropertyBuilder prop, IOperand value)
        {
            Contract.Requires(prop != null);
            Contract.Requires(value != null);
        }

        public void AddWordProperty(IPropertyBuilder prop, IOperand value)
        {
            Contract.Requires(prop != null);
            Contract.Requires(value != null);
        }

        public ITableBuilder AddComplexProperty(IPropertyBuilder prop)
        {
            Contract.Requires(prop != null);
            Contract.Ensures(Contract.Result<ITableBuilder>() != null);
            return default(ITableBuilder);
        }

        public void AddFlag(IFlagBuilder flag)
        {
            Contract.Requires(flag != null);
        }

        public abstract IConstantOperand Add(IConstantOperand other);
    }
}