using System;
using System.Collections.Generic;
using System.Diagnostics.Contracts;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Zilf.Emit
{
    public interface INumericOperand : IOperand
    {
        int Value { get; }
    }
}
