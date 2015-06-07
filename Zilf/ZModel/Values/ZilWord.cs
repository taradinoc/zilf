using System.Diagnostics.Contracts;
using Zilf.Interpreter;
using Zilf.Interpreter.Values;
using Zilf.Language;

namespace Zilf.ZModel.Values
{
    [BuiltinType(StdAtom.WORD, PrimType.LIST)]
    sealed class ZilWord : ZilObject
    {
        private readonly ZilObject value;

        public ZilWord(ZilObject value)
        {
            Contract.Requires(value != null);

            this.value = value;
        }

        [ChtypeMethod]
        public static ZilWord FromList(Context ctx, ZilList list)
        {
            Contract.Requires(ctx != null);
            Contract.Requires(list != null);
            Contract.Ensures(Contract.Result<ZilWord>() != null);

            if (list.First == null || list.Rest == null || !list.Rest.IsEmpty)
                throw new InterpreterError("list must have length 1");

            return new ZilWord(list.First);
        }

        public ZilObject Value
        {
            get
            {
                Contract.Ensures(Contract.Result<ZilObject>() != null);

                return value;
            }
        }

        public override string ToString()
        {
            return string.Format("#WORD ({0})", value);
        }

        public override string ToStringContext(Context ctx, bool friendly)
        {
            return string.Format("#WORD ({0})", value.ToStringContext(ctx, friendly));
        }

        public override ZilAtom GetTypeAtom(Context ctx)
        {
            return ctx.GetStdAtom(StdAtom.WORD);
        }

        public override PrimType PrimType
        {
            get { return PrimType.LIST; }
        }

        public override ZilObject GetPrimitive(Context ctx)
        {
            return new ZilList(value, new ZilList(null, null));
        }
    }
}