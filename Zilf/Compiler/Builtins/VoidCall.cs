using System.Diagnostics.Contracts;
using Zilf.Emit;
using Zilf.Interpreter;
using Zilf.Interpreter.Values;

namespace Zilf.Compiler.Builtins
{
    internal struct VoidCall
    {
        public CompileCtx cc { get; private set; }
        public IRoutineBuilder rb { get; private set; }
        public ZilForm form { get; private set; }

        public VoidCall(CompileCtx cc, IRoutineBuilder rb, ZilForm form)
            : this()
        {
            Contract.Requires(cc != null);
            Contract.Requires(rb != null);
            Contract.Requires(form != null);

            this.cc = cc;
            this.rb = rb;
            this.form = form;
        }

        [ContractInvariantMethod]
        private void ObjectInvariant()
        {
            Contract.Invariant(cc != null);
            Contract.Invariant(rb != null);
            Contract.Invariant(form != null);
        }
    }
}
