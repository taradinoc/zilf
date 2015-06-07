using System.Diagnostics.Contracts;
using Zilf.Emit;
using Zilf.Interpreter;
using Zilf.Interpreter.Values;

namespace Zilf.Compiler.Builtins
{
    struct PredCall
    {
        public CompileCtx cc { get; private set; }
        public IRoutineBuilder rb { get; private set; }
        public ZilForm form { get; private set; }

        public ILabel label { get; private set; }
        public bool polarity { get; private set; }

        public PredCall(CompileCtx cc, IRoutineBuilder rb, ZilForm form, ILabel label, bool polarity)
            : this()
        {
            Contract.Requires(cc != null);
            Contract.Requires(rb != null);
            Contract.Requires(form != null);
            Contract.Requires(label != null);

            this.cc = cc;
            this.rb = rb;
            this.form = form;
            this.label = label;
            this.polarity = polarity;
        }

        [ContractInvariantMethod]
        private void ObjectInvariant()
        {
            Contract.Invariant(cc != null);
            Contract.Invariant(rb != null);
            Contract.Invariant(form != null);
            Contract.Invariant(label != null);
        }
    }
}