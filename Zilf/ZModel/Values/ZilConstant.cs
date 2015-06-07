using Zilf.Interpreter;
using Zilf.Interpreter.Values;
using Zilf.Language;

namespace Zilf.ZModel.Values
{
    class ZilConstant : ZilObject
    {
        private readonly ZilAtom name;
        private readonly ZilObject value;

        public ZilConstant(ZilAtom name, ZilObject value)
        {
            this.name = name;
            this.value = value;
        }

        public ZilAtom Name
        {
            get { return name; }
        }

        public ZilObject Value
        {
            get { return value; }
        }

        public override string ToString()
        {
            return "#CONSTANT (" + name.ToString() + " " + value.ToString() + ")";
        }

        public override string ToStringContext(Context ctx, bool friendly)
        {
            return "#CONSTANT (" + name.ToStringContext(ctx, friendly) +
            " " + value.ToStringContext(ctx, friendly) + ")";
        }

        public override ZilAtom GetTypeAtom(Context ctx)
        {
            return ctx.GetStdAtom(StdAtom.CONSTANT);
        }

        public override PrimType PrimType
        {
            get { return PrimType.LIST; }
        }

        public override ZilObject GetPrimitive(Context ctx)
        {
            return new ZilList(name,
            new ZilList(value,
            new ZilList(null, null)));
        }
    }
}