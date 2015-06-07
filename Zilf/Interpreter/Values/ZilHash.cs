using System;
using System.Diagnostics.Contracts;

namespace Zilf.Interpreter.Values
{
    class ZilHash : ZilObject
    {
        protected readonly ZilAtom type;
        protected readonly PrimType primtype;
        protected readonly ZilObject primvalue;

        internal ZilHash(ZilAtom type, PrimType primtype, ZilObject primvalue)
        {
            this.type = type;
            this.primtype = primtype;
            this.primvalue = primvalue;
        }

        public override bool Equals(object obj)
        {
            return (obj is ZilHash && ((ZilHash)obj).type == this.type &&
                    ((ZilHash)obj).primvalue.Equals(this.primvalue));
        }

        public override int GetHashCode()
        {
            return type.GetHashCode() ^ primvalue.GetHashCode();
        }

        public ZilAtom Type
        {
            get { return type; }
        }

        public static ZilObject Parse(Context ctx, ZilObject[] initializer)
        {
            Contract.Requires(ctx != null);
            Contract.Requires(initializer != null);

            if (initializer.Length != 2 || !(initializer[0] is ZilAtom) || initializer[1] == null)
                throw new ArgumentException("Expected 2 objects, the first a ZilAtom");

            ZilAtom type = (ZilAtom)initializer[0];
            ZilObject value = initializer[1];

            return ctx.ChangeType(value, type);
        }

        public override string ToString()
        {
            return "#" + type.ToString() + " " + primvalue.ToString();
        }

        public override string ToStringContext(Context ctx, bool friendly)
        {
            return "#" + type.ToStringContext(ctx, friendly) + " " + primvalue.ToStringContext(ctx, friendly);
        }

        public override ZilAtom GetTypeAtom(Context ctx)
        {
            return type;
        }

        public override PrimType PrimType
        {
            get { return primtype; }
        }

        public override ZilObject GetPrimitive(Context ctx)
        {
            return primvalue;
        }

        public override ZilObject Eval(Context ctx)
        {
            return this;
        }
    }
}