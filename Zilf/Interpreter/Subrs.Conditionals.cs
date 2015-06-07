using System.Linq;
using Zilf.Interpreter.Values;
using Zilf.Language;

namespace Zilf.Interpreter
{
    static partial class Subrs
    {
        [FSubr]
        public static ZilObject COND(Context ctx, ZilObject[] args)
        {
            SubrContracts(ctx, args);

            if (args.Length < 1)
                throw new InterpreterError("COND", 1, 0);

            ZilObject result = null;

            foreach (ZilObject zo in args)
            {
                if (zo.GetTypeAtom(ctx).StdAtom == StdAtom.LIST)
                {
                    ZilList zl = (ZilList)zo;

                    if (zl.IsEmpty)
                        throw new InterpreterError("COND: lists must be non-empty");

                    result = zl.First.Eval(ctx);

                    if (result.IsTrue)
                    {
                        foreach (ZilObject inner in zl.Skip(1))
                            result = inner.Eval(ctx);

                        return result;
                    }
                }
                else
                    throw new InterpreterError("COND: args must be lists");
            }

            return result;
        }

        [FSubr]
        public static ZilObject OR(Context ctx, ZilObject[] args)
        {
            SubrContracts(ctx, args);

            ZilObject result = ctx.FALSE;

            foreach (ZilObject arg in args)
            {
                result = arg.Eval(ctx);
                if (result.IsTrue)
                    return result;
            }

            return result;
        }

        [FSubr]
        public static ZilObject AND(Context ctx, ZilObject[] args)
        {
            SubrContracts(ctx, args);

            ZilObject result = ctx.TRUE;

            foreach (ZilObject arg in args)
            {
                result = arg.Eval(ctx);
                if (!result.IsTrue)
                    return result;
            }

            return result;
        }
    }
}
