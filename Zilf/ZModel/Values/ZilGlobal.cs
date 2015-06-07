using Zilf.Interpreter;
using Zilf.Interpreter.Values;
using Zilf.Language;

namespace Zilf.ZModel.Values
{
    class ZilGlobal : ZilObject
    {
        private readonly ZilAtom name;
        private readonly ZilObject value;

        public ZilGlobal(ZilAtom name, ZilObject value, GlobalStorageType storageType = GlobalStorageType.Any)
        {
            this.name = name;
            this.value = value;
            this.StorageType = storageType;
            this.IsWord = true;
        }

        public ZilAtom Name
        {
            get { return name; }
        }

        public ZilObject Value
        {
            get { return value; }
        }

        public GlobalStorageType StorageType { get; set; }

        public bool IsWord { get; set; }

        public override string ToString()
        {
            return "#GLOBAL (" + name.ToString() + " " + value.ToString() + ")";
        }

        public override string ToStringContext(Context ctx, bool friendly)
        {
            return "#GLOBAL (" + name.ToStringContext(ctx, friendly) +
            " " + value.ToStringContext(ctx, friendly) + ")";
        }

        public override ZilAtom GetTypeAtom(Context ctx)
        {
            return ctx.GetStdAtom(StdAtom.GLOBAL);
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