using System;
using System.Collections.Generic;
using System.Diagnostics.Contracts;
using System.Linq;
using Zilf.Interpreter.Values;
using Zilf.Language;

namespace Zilf.Interpreter
{
    static partial class Subrs
    {
        [FSubr]
        public static ZilObject DEFINE(Context ctx, ZilObject[] args)
        {
            SubrContracts(ctx, args);

            if ((ctx.CurrentFileFlags & FileFlags.MdlZil) != 0)
            {
                return ROUTINE(ctx, args);
            }
            else
            {
                return PerformDefine(ctx, args, "DEFINE");
            }
        }

        [FSubr]
        public static ZilObject DEFINE20(Context ctx, ZilObject[] args)
        {
            SubrContracts(ctx, args);

            return PerformDefine(ctx, args, "DEFINE20");
        }

        private static ZilObject PerformDefine(Context ctx, ZilObject[] args, string name)
        {
            SubrContracts(ctx, args);
            Contract.Requires(name != null);

            if (args.Length < 3)
                throw new InterpreterError(name, 3, 0);

            ZilAtom atom = args[0].Eval(ctx) as ZilAtom;
            if (atom == null)
                throw new InterpreterError(name + ": first arg must evaluate to an atom");
            if (!ctx.AllowRedefine && ctx.GetGlobalVal(atom) != null)
                throw new InterpreterError(name + ": already defined: " + atom.ToStringContext(ctx, false));

            if (args[1].GetTypeAtom(ctx).StdAtom != StdAtom.LIST)
                throw new InterpreterError(name + ": second arg must be a list");

            ZilFunction func = new ZilFunction(atom,
                (IEnumerable<ZilObject>)args[1],
                args.Skip(2));
            ctx.SetGlobalVal(atom, func);
            return atom;
        }

        [FSubr]
        public static ZilObject DEFMAC(Context ctx, ZilObject[] args)
        {
            SubrContracts(ctx, args);

            if (args.Length < 3)
                throw new InterpreterError("DEFMAC", 3, 0);

            ZilAtom atom = args[0].Eval(ctx) as ZilAtom;
            if (atom == null)
                throw new InterpreterError("DEFMAC: first arg must be an atom");
            if (!ctx.AllowRedefine && ctx.GetGlobalVal(atom) != null)
                throw new InterpreterError("DEFMAC: already defined: " + atom.ToStringContext(ctx, false));

            if (args[1].GetTypeAtom(ctx).StdAtom != StdAtom.LIST)
                throw new InterpreterError("DEFMAC: second arg must be a list");

            ZilFunction func = new ZilFunction(atom,
                (IEnumerable<ZilObject>)args[1],
                args.Skip(2));
            ZilEvalMacro macro = new ZilEvalMacro(func);
            ctx.SetGlobalVal(atom, macro);
            return atom;
        }

        [FSubr]
        public static ZilObject QUOTE(Context ctx, ZilObject[] args)
        {
            SubrContracts(ctx, args);

            if (args.Length != 1)
                throw new InterpreterError("QUOTE", 1, 1);

            return args[0];
        }

        [Subr]
        public static ZilObject EVAL(Context ctx, ZilObject[] args)
        {
            SubrContracts(ctx, args);

            if (args.Length != 1)
                throw new InterpreterError("EVAL", 1, 1);

            return args[0].Eval(ctx);
        }

        [Subr]
        public static ZilObject EXPAND(Context ctx, ZilObject[] args)
        {
            SubrContracts(ctx, args);

            if (args.Length != 1)
                throw new InterpreterError("EXPAND", 1, 1);

            return args[0].Expand(ctx);
        }

        [Subr]
        public static ZilObject APPLY(Context ctx, ZilObject[] args)
        {
            SubrContracts(ctx, args);

            if (args.Length == 0)
                throw new InterpreterError("APPLY", 1, 0);

            IApplicable ap = args[0] as IApplicable;
            if (ap == null)
                throw new InterpreterError("APPLY: first arg must be an applicable type");

            ZilObject[] newArgs = new ZilObject[args.Length - 1];
            Array.Copy(args, 1, newArgs, 0, args.Length - 1);
            Contract.Assume(Contract.ForAll(newArgs, a => a != null));
            return ap.ApplyNoEval(ctx, newArgs);
        }
    }
}
