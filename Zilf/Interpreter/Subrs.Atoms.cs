using System;
using System.Diagnostics.Contracts;
using Zilf.Interpreter.Values;
using Zilf.Language;

namespace Zilf.Interpreter
{
    static partial class Subrs
    {
        [Subr]
        [Subr("PNAME")]
        public static ZilObject SPNAME(Context ctx, ZilObject[] args)
        {
            SubrContracts(ctx, args);

            if (args.Length != 1)
                throw new InterpreterError("SPNAME", 1, 1);

            ZilAtom atom = args[0] as ZilAtom;
            if (atom == null)
                throw new InterpreterError("SPNAME: arg must be an atom");

            return new ZilString(atom.Text);
        }

        [Subr]
        public static ZilObject PARSE(Context ctx, ZilObject[] args)
        {
            SubrContracts(ctx, args);

            // in MDL, this parses an arbitrary expression, but parsing atoms is probably enough for ZIL

            if (args.Length != 1)
                throw new InterpreterError("PARSE", 1, 1);

            if (args[0].GetTypeAtom(ctx).StdAtom != StdAtom.STRING)
                throw new InterpreterError("PARSE: arg must be a string");

            return ZilAtom.Parse(args[0].ToStringContext(ctx, true), ctx);
        }

        [Subr]
        public static ZilObject LOOKUP(Context ctx, ZilObject[] args)
        {
            SubrContracts(ctx, args);

            if (args.Length != 2)
                throw new InterpreterError("LOOKUP", 2, 2);

            var str = args[0] as ZilString;
            if (str == null)
                throw new InterpreterError("LOOKUP: first arg must be a string");

            var oblist = args[1] as ObList;
            if (oblist == null)
                throw new InterpreterError("LOOKUP: second arg must be an OBLIST");

            return oblist.Contains(str.Text) ? oblist[str.Text] : ctx.FALSE;
        }

        [Subr]
        public static ZilObject INSERT(Context ctx, ZilObject[] args)
        {
            SubrContracts(ctx, args);

            // TODO: support 1-argument form of INSERT?
            if (args.Length != 2)
                throw new InterpreterError("INSERT", 2, 2);

            var str = args[0] as ZilString;
            if (str == null)
                throw new InterpreterError("INSERT: first arg must be a string");

            var oblist = args[1] as ObList;
            if (oblist == null)
                throw new InterpreterError("INSERT: second arg must be an OBLIST");

            if (oblist.Contains(str.Text))
                throw new InterpreterError(string.Format("INSERT: OBLIST already contains an atom named '{0}'", str.Text));

            return oblist[str.Text];
        }

        [Subr]
        public static ZilObject ROOT(Context ctx, ZilObject[] args)
        {
            SubrContracts(ctx, args);

            if (args.Length != 0)
                throw new InterpreterError("ROOT", 0, 0);

            return ctx.RootObList;
        }

        [Subr]
        public static ZilObject SETG(Context ctx, ZilObject[] args)
        {
            SubrContracts(ctx, args);

            if ((ctx.CurrentFileFlags & FileFlags.MdlZil) != 0)
            {
                return GLOBAL(ctx, args);
            }
            else
            {
                return PerformSetg(ctx, args, "SETG");
            }
        }

        [Subr]
        public static ZilObject SETG20(Context ctx, ZilObject[] args)
        {
            SubrContracts(ctx, args);

            return PerformSetg(ctx, args, "SETG20");
        }

        private static ZilObject PerformSetg(Context ctx, ZilObject[] args, string name)
        {
            SubrContracts(ctx, args);
            Contract.Requires(name != null);

            if (args.Length != 2)
                throw new InterpreterError(name, 2, 2);

            if (!(args[0] is ZilAtom))
                throw new InterpreterError(name + ": first arg must be an atom");

            if (args[1] == null)
                throw new ArgumentNullException();

            ctx.SetGlobalVal((ZilAtom)args[0], args[1]);
            return args[1];
        }

        [Subr]
        public static ZilObject SET(Context ctx, ZilObject[] args)
        {
            SubrContracts(ctx, args);

            if (args.Length != 2)
                throw new InterpreterError("SET", 2, 2);

            if (!(args[0] is ZilAtom))
                throw new InterpreterError("SET: first arg must be an atom");

            if (args[1] == null)
                throw new ArgumentNullException();

            ctx.SetLocalVal((ZilAtom)args[0], args[1]);
            return args[1];
        }

        [Subr]
        public static ZilObject GVAL(Context ctx, ZilObject[] args)
        {
            SubrContracts(ctx, args);

            if (args.Length != 1)
                throw new InterpreterError("GVAL", 1, 1);

            if (!(args[0] is ZilAtom))
                throw new InterpreterError("GVAL: arg must be an atom");

            ZilObject result = ctx.GetGlobalVal((ZilAtom)args[0]);
            if (result == null)
                throw new InterpreterError("atom has no global value: " +
                    args[0].ToStringContext(ctx, false));

            return result;
        }

        [Subr("GASSIGNED?")]
        public static ZilObject GASSIGNED_P(Context ctx, ZilObject[] args)
        {
            SubrContracts(ctx, args);

            if (args.Length != 1)
                throw new InterpreterError("GASSIGNED?", 1, 1);

            ZilAtom atom = args[0] as ZilAtom;
            if (atom == null)
                throw new InterpreterError("GASSIGNED?: arg must be an atom");

            return ctx.GetGlobalVal(atom) != null ? ctx.TRUE : ctx.FALSE;
        }

        [FSubr]
        public static ZilObject GDECL(Context ctx, ZilObject[] args)
        {
            SubrContracts(ctx, args);

            // ignore global declarations
            return ctx.FALSE;
        }

        [Subr]
        public static ZilObject LVAL(Context ctx, ZilObject[] args)
        {
            SubrContracts(ctx, args);

            if (args.Length != 1)
                throw new InterpreterError("LVAL", 1, 1);

            if (!(args[0] is ZilAtom))
                throw new InterpreterError("LVAL: arg must be an atom");

            ZilObject result = ctx.GetLocalVal((ZilAtom)args[0]);
            if (result == null)
                throw new InterpreterError("atom has no local value: " +
                    args[0].ToStringContext(ctx, false));

            return result;
        }

        [Subr("ASSIGNED?")]
        public static ZilObject ASSIGNED_P(Context ctx, ZilObject[] args)
        {
            SubrContracts(ctx, args);

            if (args.Length != 1)
                throw new InterpreterError("ASSIGNED?", 1, 1);

            ZilAtom atom = args[0] as ZilAtom;
            if (atom == null)
                throw new InterpreterError("ASSIGNED?: arg must be an atom");

            return ctx.GetLocalVal(atom) != null ? ctx.TRUE : ctx.FALSE;
        }

        [Subr]
        public static ZilObject GETPROP(Context ctx, ZilObject[] args)
        {
            SubrContracts(ctx, args);

            if (args.Length < 2 || args.Length > 3)
                throw new InterpreterError("GETPROP", 2, 3);

            var result = ctx.GetProp(args[0], args[1]);

            if (result != null)
            {
                return result;
            }
            else if (args.Length > 2)
            {
                return args[2].Eval(ctx);
            }
            else
            {
                return ctx.FALSE;
            }
        }

        [Subr]
        public static ZilObject PUTPROP(Context ctx, ZilObject[] args)
        {
            SubrContracts(ctx, args);

            if (args.Length < 2 || args.Length > 3)
                throw new InterpreterError("PUTPROP", 2, 3);

            if (args.Length == 2)
            {
                // clear, and return previous value or <>
                var result = ctx.GetProp(args[0], args[1]);
                ctx.PutProp(args[0], args[1], null);
                return result ?? ctx.FALSE;
            }
            else
            {
                // set, and return first arg
                ctx.PutProp(args[0], args[1], args[2]);
                return args[0];
            }
        }
    }
}
