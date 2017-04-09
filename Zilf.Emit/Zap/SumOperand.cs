using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Zilf.Emit.Zap
{
    class SumOperand : IConstantOperand
    {
        public SumOperand(IConstantOperand left, IConstantOperand right)
        {
            this.Left = left;
            this.Right = right;
        }

        public IConstantOperand Left { get; }
        public IConstantOperand Right { get; }

        public override string ToString()
        {
            return Left.ToString() + "+" + Right.ToString();
        }

        public IConstantOperand Add(IConstantOperand other)
        {
            return new SumOperand(this, other);
        }
    }
}
