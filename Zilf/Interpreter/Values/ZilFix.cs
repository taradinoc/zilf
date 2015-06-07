using System.Diagnostics.Contracts;
using System.Linq;
using Zilf.Language;

namespace Zilf.Interpreter.Values
{
    [BuiltinType(StdAtom.FIX, PrimType.FIX)]
    class ZilFix : ZilObject, IApplicable
    {
        private readonly int value;

        public ZilFix(int value)
        {
            this.value = value;
        }

        [ChtypeMethod]
        public ZilFix(ZilFix other)
            : this(other.value)
        {
            Contract.Requires(other != null);
        }

        public int Value
        {
            get { return value; }
        }

        public override string ToString()
        {
            return value.ToString();
        }

        public override ZilAtom GetTypeAtom(Context ctx)
        {
            return ctx.GetStdAtom(StdAtom.FIX);
        }

        public override PrimType PrimType
        {
            get { return PrimType.FIX; }
        }

        public override ZilObject GetPrimitive(Context ctx)
        {
            return this;
        }

        public override bool Equals(object obj)
        {
            ZilFix other = obj as ZilFix;
            return other != null && other.value == this.value;
        }

        public override int GetHashCode()
        {
            return value.GetHashCode();
        }

        #region IApplicable Members

        public ZilObject Apply(Context ctx, ZilObject[] args)
        {
            return ApplyNoEval(ctx, EvalSequence(ctx, args).ToArray());
        }

        public ZilObject ApplyNoEval(Context ctx, ZilObject[] args)
        {
            if (args.Length == 1)
                return Subrs.NTH(ctx, new ZilObject[] { args[0], this });
            else if (args.Length == 2)
                return Subrs.PUT(ctx, new ZilObject[] { args[0], this, args[1] });
            else
                throw new InterpreterError("expected 1 or 2 args after a FIX");
        }

        #endregion
    }
}