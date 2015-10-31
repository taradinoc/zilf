using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Zilf.Language;

namespace Zilf.Interpreter.Values
{
    [BuiltinType(StdAtom.ACTIVATION, PrimType.ATOM)]
    class ZilActivation : ZilObject
    {
        private readonly ZilAtom name;

        public ZilActivation(ZilAtom name)
        {
            this.name = name;
        }

        [ChtypeMethod]
        public static ZilActivation FromAtom(Context ctx, ZilAtom name)
        {
            throw new InterpreterError("CHTYPE to ACTIVATION not supported");
        }

        public override PrimType PrimType
        {
            get
            {
                return PrimType.ATOM;
            }
        }

        public override ZilObject GetPrimitive(Context ctx)
        {
            return name;
        }

        public override ZilAtom GetTypeAtom(Context ctx)
        {
            return ctx.GetStdAtom(StdAtom.ACTIVATION);
        }

        public override string ToString()
        {
            return string.Format("#ACTIVATION {0}", name.ToString());
        }

        public override string ToStringContext(Context ctx, bool friendly)
        {
            return string.Format("#ACTIVATION {0}", name.ToStringContext(ctx, friendly));
        }
    }
}
