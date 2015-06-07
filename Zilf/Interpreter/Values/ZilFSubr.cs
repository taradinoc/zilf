using System;
using System.Diagnostics.Contracts;
using System.Reflection;
using Zilf.Language;

namespace Zilf.Interpreter.Values
{
    [BuiltinType(StdAtom.FSUBR, PrimType.STRING)]
    class ZilFSubr : ZilSubr, IApplicable
    {
        public ZilFSubr(Subrs.SubrDelegate handler)
            : base(handler)
        {
        }

        [ChtypeMethod]
        public static new ZilFSubr FromString(Context ctx, ZilString str)
        {
            Contract.Requires(ctx != null);
            Contract.Requires(str != null);
            Contract.Ensures(Contract.Result<ZilFSubr>() != null);

            var name = str.ToStringContext(ctx, true);
            MethodInfo mi = typeof(Subrs).GetMethod(name, BindingFlags.Static | BindingFlags.Public);
            if (mi != null)
            {
                object[] attrs = mi.GetCustomAttributes(typeof(Subrs.SubrAttribute), false);
                if (attrs.Length == 1)
                {
                    Subrs.SubrDelegate del = (Subrs.SubrDelegate)Delegate.CreateDelegate(
                        typeof(Subrs.SubrDelegate), mi);

                    return new ZilFSubr(del);
                }
            }
            throw new InterpreterError("unrecognized FSUBR name: " + name);
        }

        public override string ToString()
        {
            return "#FSUBR \"" + handler.Method.Name + "\"";
        }

        public override ZilAtom GetTypeAtom(Context ctx)
        {
            return ctx.GetStdAtom(StdAtom.FSUBR);
        }

        public override ZilObject Apply(Context ctx, ZilObject[] args)
        {
            return ApplyNoEval(ctx, args);
        }
    }
}