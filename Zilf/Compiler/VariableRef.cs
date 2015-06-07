using Zilf.Emit;

namespace Zilf.Compiler
{
    internal struct VariableRef
    {
        public readonly IVariable Hard;
        public readonly SoftGlobal Soft;

        public VariableRef(IVariable hard)
        {
            this.Hard = hard;
            this.Soft = null;
        }

        public VariableRef(SoftGlobal soft)
        {
            this.Soft = soft;
            this.Hard = null;
        }

        public bool IsHard
        {
            get { return Hard != null; }
        }
    }
}
