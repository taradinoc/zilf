/* Copyright 2010, 2015 Jesse McGrew
 * 
 * This file is part of ZILF.
 * 
 * ZILF is free software: you can redistribute it and/or modify it
 * under the terms of the GNU General Public License as published by
 * the Free Software Foundation, either version 3 of the License, or
 * (at your option) any later version.
 * 
 * ZILF is distributed in the hope that it will be useful, but
 * WITHOUT ANY WARRANTY; without even the implied warranty of
 * MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the GNU
 * General Public License for more details.
 * 
 * You should have received a copy of the GNU General Public License
 * along with ZILF.  If not, see <http://www.gnu.org/licenses/>.
 */
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
        public static ZilObject SPNAME(Context ctx, ZilAtom atom)
        {
            SubrContracts(ctx);
            return ZilString.FromString(atom.Text);
        }

        [Subr]
        public static ZilObject PARSE(Context ctx, ZilObject[] args)
        {
            SubrContracts(ctx, args);

            // in MDL, this parses an arbitrary expression, but parsing atoms is probably enough for ZIL
            // TODO: implement arbitrary expression parsing?

            if (args.Length < 1 || args.Length > 3)
                throw new InterpreterError("PARSE", 1, 3);

            if (args[0].GetTypeAtom(ctx).StdAtom != StdAtom.STRING)
                throw new InterpreterError("PARSE: arg must be a string");

            if (args.Length >= 2)
            {
                // we only pretend to implement radix
                var radix = args[1] as ZilFix;
                if (radix == null || radix.Value != 10)
                    throw new InterpreterError("PARSE: second arg must be 10");
            }

            ZilObject lookupObList;
            if (args.Length >= 3)
            {
                lookupObList = args[2];
                switch (lookupObList.GetTypeAtom(ctx).StdAtom)
                {
                    case StdAtom.OBLIST:
                        lookupObList = new ZilList(lookupObList, new ZilList(null, null));
                        break;

                    case StdAtom.LIST:
                        // OK
                        break;

                    default:
                        throw new InterpreterError("PARSE: third arg must be an oblist or list");
                }

                ctx.PushLocalVal(ctx.GetStdAtom(StdAtom.OBLIST), lookupObList);
            }
            else
            {
                lookupObList = null;
            }

            try
            {
                return ZilAtom.Parse(args[0].ToStringContext(ctx, true), ctx);
            }
            finally
            {
                if (lookupObList != null)
                    ctx.PopLocalVal(ctx.GetStdAtom(StdAtom.OBLIST));
            }
        }

        // TODO: implement LPARSE?

        [Subr]
        public static ZilObject UNPARSE(Context ctx, ZilObject arg)
        {
            SubrContracts(ctx);

            // in MDL, this takes an optional second argument (radix), but we don't bother

            return ZilString.FromString(arg.ToStringContext(ctx, false));
        }

        [Subr]
        public static ZilObject LOOKUP(Context ctx, string str, ObList oblist)
        {
            SubrContracts(ctx);
            return oblist.Contains(str) ? oblist[str] : ctx.FALSE;
        }

        [Subr]
        public static ZilObject INSERT(Context ctx, string str, ObList oblist)
        {
            SubrContracts(ctx);

            if (oblist.Contains(str))
                throw new InterpreterError(string.Format(
                    "INSERT: OBLIST already contains an atom named '{0}'", str));

            return oblist[str];
        }

        [Subr]
        public static ZilObject ROOT(Context ctx)
        {
            SubrContracts(ctx);

            return ctx.RootObList;
        }

        [Subr]
        public static ZilObject MOBLIST(Context ctx, ZilAtom name)
        {
            SubrContracts(ctx);

            ObList result = ctx.GetProp(name, ctx.GetStdAtom(StdAtom.OBLIST)) as ObList;
            if (result == null)
                result = ctx.MakeObList(name);

            return result;
        }

        [Subr("OBLIST?")]
        public static ZilObject OBLIST_P(Context ctx, ZilAtom atom)
        {
            SubrContracts(ctx);

            return atom.ObList ?? ctx.FALSE;
        }

        [Subr]
        public static ZilObject BLOCK(Context ctx, ZilList list)
        {
            SubrContracts(ctx);

            ctx.PushObPath(list);
            return list;
        }

        [Subr]
        public static ZilObject ENDBLOCK(Context ctx)
        {
            SubrContracts(ctx);

            return ctx.PopObPath();
        }

        // TODO: clean up arg handling for SETG
        [Subr]
        public static ZilObject SETG(Context ctx, ZilObject[] args)
        {
            SubrContracts(ctx, args);

            if (ctx.AtTopLevel && (ctx.CurrentFileFlags & FileFlags.MdlZil) != 0)
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
        public static ZilObject SET(Context ctx, ZilAtom atom, ZilObject value)
        {
            SubrContracts(ctx);

            ctx.SetLocalVal(atom, value);
            return value;
        }

        [Subr]
        public static ZilObject GVAL(Context ctx, ZilAtom atom)
        {
            SubrContracts(ctx);

            ZilObject result = ctx.GetGlobalVal(atom);
            if (result == null)
                throw new InterpreterError("atom has no global value: " +
                    atom.ToStringContext(ctx, false));

            return result;
        }

        [Subr("GASSIGNED?")]
        public static ZilObject GASSIGNED_P(Context ctx, ZilAtom atom)
        {
            SubrContracts(ctx);

            return ctx.GetGlobalVal(atom) != null ? ctx.TRUE : ctx.FALSE;
        }

        [FSubr]
        public static ZilObject GDECL(Context ctx, ZilObject[] args)
        {
            SubrContracts(ctx, args);

            // ignore global declarations
            return ctx.FALSE;
        }

        [Subr("DECL?")]
        public static ZilObject DECL_P(Context ctx, ZilObject value, ZilObject pattern)
        {
            SubrContracts(ctx);

            return Decl.Check(ctx, value, pattern) ? ctx.TRUE : ctx.FALSE;
        }

        [Subr("PUT-DECL")]
        public static ZilObject PUT_DECL(Context ctx, ZilObject item, ZilObject pattern)
        {
            SubrContracts(ctx);

            return PUTPROP(ctx, item, ctx.GetStdAtom(StdAtom.DECL), pattern);
        }

        [Subr]
        public static ZilObject LVAL(Context ctx, ZilAtom atom)
        {
            SubrContracts(ctx);

            ZilObject result = ctx.GetLocalVal(atom);
            if (result == null)
                throw new InterpreterError("atom has no local value: " +
                    atom.ToStringContext(ctx, false));

            return result;
        }

        [Subr("ASSIGNED?")]
        public static ZilObject ASSIGNED_P(Context ctx, ZilAtom atom)
        {
            SubrContracts(ctx);

            return ctx.GetLocalVal(atom) != null ? ctx.TRUE : ctx.FALSE;
        }

        [Subr]
        public static ZilObject GETPROP(Context ctx, ZilObject item, ZilObject indicator, ZilObject wtf = null)
        {
            SubrContracts(ctx);

            var result = ctx.GetProp(item, indicator);

            if (result != null)
            {
                return result;
            }
            else if (wtf != null)
            {
                return wtf.Eval(ctx);
            }
            else
            {
                return ctx.FALSE;
            }
        }

        [Subr]
        public static ZilObject PUTPROP(Context ctx, ZilObject item, ZilObject indicator, ZilObject value = null)
        {
            SubrContracts(ctx);

            if (value == null)
            {
                // clear, and return previous value or <>
                var result = ctx.GetProp(item, indicator);
                ctx.PutProp(item, indicator, null);
                return result ?? ctx.FALSE;
            }
            else
            {
                // set, and return first arg
                ctx.PutProp(item, indicator, value);
                return item;
            }
        }
    }
}
