using System.Diagnostics.Contracts;
using Zilf.Emit;
using Zilf.Interpreter;
using Zilf.Interpreter.Values;

namespace Zilf.Compiler.Builtins
{
    struct ValueCall
    {
        public CompileCtx cc { get; private set; }
        public IRoutineBuilder rb { get; private set; }
        public ZilForm form { get; private set; }

        public IVariable resultStorage { get; private set; }

        public ValueCall(CompileCtx cc, IRoutineBuilder rb, ZilForm form, IVariable resultStorage)
            : this()
        {
            Contract.Requires(cc != null);
            Contract.Requires(rb != null);
            Contract.Requires(form != null);
            Contract.Requires(resultStorage != null);

            this.cc = cc;
            this.rb = rb;
            this.form = form;
            this.resultStorage = resultStorage;
        }

        [ContractInvariantMethod]
        private void ObjectInvariant()
        {
            Contract.Invariant(cc != null);
            Contract.Invariant(rb != null);
            Contract.Invariant(form != null);
            Contract.Invariant(resultStorage != null);
        }
    }
}