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
using System.Linq;
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
        public static ZilObject PARSE(Context ctx, string text, [Decl("'10")] int radix = 10,
            [Either(typeof(ObList), typeof(ZilList))] ZilObject lookupObList = null)
        {
            SubrContracts(ctx);
            return PerformParse(ctx, text, radix, lookupObList, "PARSE", true);
        }

        [Subr]
        public static ZilObject LPARSE(Context ctx, string text, [Decl("'10")] int radix = 10,
            [Either(typeof(ObList), typeof(ZilList))] ZilObject lookupObList = null)
        {
            SubrContracts(ctx);
            return PerformParse(ctx, text, radix, lookupObList, "LPARSE", false);
        }

        private static ZilObject PerformParse(Context ctx, string text, int radix, ZilObject lookupObList,
            string name, bool singleResult)
        {
            // we only pretend to implement radix. the decl and default should guarantee it's 10.
            Contract.Assert(radix == 10);

            using (var innerEnv = ctx.PushEnvironment())
            {
                if (lookupObList != null)
                {
                    if (lookupObList is ObList)
                        lookupObList = new ZilList(lookupObList, new ZilList(null, null));

                    innerEnv.Rebind(ctx.GetStdAtom(StdAtom.OBLIST), lookupObList);
                }

                var ztree = Program.Parse(ctx, text);        //XXX move into FrontEnd class
                if (singleResult)
                {
                    try
                    {
                        return ztree.First();
                    }
                    catch (InvalidOperationException ex)
                    {
                        throw new InterpreterError(name + ": no expressions found", ex);
                    }
                }
                else
                {
                    return new ZilList(ztree);
                }
            }
        }

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

        [Subr]
        [MdlZilRedirect(typeof(Subrs), nameof(GLOBAL), TopLevelOnly = true)]
        public static ZilObject SETG(Context ctx, ZilAtom atom, ZilObject value)
        {
            SubrContracts(ctx);

            ctx.SetGlobalVal(atom, value);
            return value;
        }

        [Subr]
        public static ZilObject SETG20(Context ctx, ZilAtom atom, ZilObject value)
        {
            SubrContracts(ctx);

            ctx.SetGlobalVal(atom, value);
            return value;
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

        [Subr]
        public static ZilObject GUNASSIGN(Context ctx, ZilAtom atom)
        {
            SubrContracts(ctx);

            ctx.SetGlobalVal(atom, null);
            return atom;
        }

        [Subr("GBOUND?")]
        public static ZilObject GBOUND_P(Context ctx, ZilAtom atom)
        {
            SubrContracts(ctx);

            return ctx.GetGlobalBinding(atom, false) != null ? ctx.TRUE : ctx.FALSE;
        }

        public static class DeclParams
        {
            [ZilSequenceParam]
            public struct AtomsDeclSequence
            {
                public AtomList Atoms;
                public ZilObject Decl;
            }

            [ZilStructuredParam(StdAtom.LIST)]
            public struct AtomList
            {
                public ZilAtom[] Atoms;
            }
        }

        [FSubr]
        public static ZilObject GDECL(Context ctx, DeclParams.AtomsDeclSequence[] pairs)
        {
            SubrContracts(ctx);

            foreach (var pair in pairs)
            {
                foreach (var atom in pair.Atoms.Atoms)
                {
                    var binding = ctx.GetGlobalBinding(atom, true);
                    binding.Decl = pair.Decl;
                }
            }

            return ctx.TRUE;
        }

        [Subr("DECL?")]
        public static ZilObject DECL_P(Context ctx, ZilObject value, ZilObject pattern)
        {
            SubrContracts(ctx);

            return Decl.Check(ctx, value, pattern) ? ctx.TRUE : ctx.FALSE;
        }

        [Subr("DECL-CHECK")]
        public static ZilObject DECL_CHECK(Context ctx, bool enable)
        {
            SubrContracts(ctx);

            var wasEnabled = ctx.CheckDecls;
            ctx.CheckDecls = enable;
            return wasEnabled ? ctx.TRUE : ctx.FALSE;
        }

        [Subr("GET-DECL")]
        public static ZilObject GET_DECL(Context ctx, ZilObject item)
        {
            SubrContracts(ctx);

            return GETPROP(ctx, item, ctx.GetStdAtom(StdAtom.DECL));
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

        [Subr]
        public static ZilObject UNASSIGN(Context ctx, ZilAtom atom)
        {
            SubrContracts(ctx);

            ctx.SetLocalVal(atom, null);
            return atom;
        }

        [Subr("ASSIGNED?")]
        public static ZilObject ASSIGNED_P(Context ctx, ZilAtom atom)
        {
            SubrContracts(ctx);

            return ctx.GetLocalVal(atom) != null ? ctx.TRUE : ctx.FALSE;
        }

        [Subr("BOUND?")]
        public static ZilObject BOUND_P(Context ctx, ZilAtom atom)
        {
            SubrContracts(ctx);

            return ctx.LocalEnvironment.IsLocalBound(atom) ? ctx.TRUE : ctx.FALSE;
        }

        [Subr]
        public static ZilObject VALUE(Context ctx, ZilAtom atom)
        {
            SubrContracts(ctx);

            var result = ctx.GetLocalVal(atom) ?? ctx.GetGlobalVal(atom);
            if (result == null)
                throw new InterpreterError("atom has no local or global value: " +
                    atom.ToStringContext(ctx, false));

            return result;
        }

        [Subr]
        public static ZilObject GETPROP(Context ctx, ZilObject item, ZilObject indicator, ZilObject defaultValue = null)
        {
            SubrContracts(ctx);

            var result = ctx.GetProp(item, indicator);

            if (result != null)
            {
                return result;
            }
            else if (defaultValue != null)
            {
                return defaultValue.Eval(ctx);
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
