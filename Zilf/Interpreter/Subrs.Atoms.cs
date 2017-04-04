/* Copyright 2010-2017 Jesse McGrew
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
using Zilf.Diagnostics;

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

        static ZilObject PerformParse(Context ctx, string text, int radix, ZilObject lookupObList,
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

                var ztree = Program.Parse(ctx, text);        // TODO: move into FrontEnd class
                if (singleResult)
                {
                    try
                    {
                        return ztree.First();
                    }
                    catch (InvalidOperationException ex)
                    {
                        throw new InterpreterError(InterpreterMessages._0_No_Expressions_Found, name, ex);
                    }
                }
                return new ZilList(ztree);
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
        public static ZilObject INSERT(Context ctx,
            [Either(typeof(string), typeof(ZilAtom))] object stringOrAtom,
            ObList oblist)
        {
            SubrContracts(ctx);

            if (stringOrAtom is string str)
            {
                if (oblist.Contains(str))
                    throw new InterpreterError(InterpreterMessages._0_OBLIST_Already_Contains_An_Atom_Named_1, "INSERT", str);

                return oblist[str];
            }

            var atom = (ZilAtom)stringOrAtom;

            if (atom.ObList != null)
                throw new InterpreterError(InterpreterMessages._0_Atom_1_Is_Already_On_An_OBLIST, "INSERT", atom.ToStringContext(ctx, false));

            if (oblist.Contains(atom.Text))
                throw new InterpreterError(InterpreterMessages._0_OBLIST_Already_Contains_An_Atom_Named_1, "INSERT", atom.Text);

            atom.ObList = oblist;
            return atom;
        }

        public static class RemoveParams
        {
            [ZilSequenceParam]
            public struct PnameAndObList
            {
                public string Pname;
                public ObList ObList;
            }
        }

        [Subr]
        public static ZilObject REMOVE(Context ctx,
            [Either(typeof(ZilAtom), typeof(RemoveParams.PnameAndObList), DefaultParamDesc = "atom")] object atomOrNameAndObList)
        {
            SubrContracts(ctx);

            if (atomOrNameAndObList is ZilAtom atom)
            {
                if (atom.ObList != null)
                {
                    atom.ObList = null;
                    return atom;
                }
                return ctx.FALSE;
            }

            var nameAndOblist = (RemoveParams.PnameAndObList)atomOrNameAndObList;
            var pname = nameAndOblist.Pname;
            var oblist = nameAndOblist.ObList;

            if (oblist.Contains(pname))
            {
                atom = oblist[pname];
                atom.ObList = null;
                return atom;
            }

            return ctx.FALSE;
        }

        [Subr]
        public static ZilObject LINK(Context ctx, ZilObject value, string str, ObList oblist)
        {
            SubrContracts(ctx);

            if (oblist.Contains(str))
                if (oblist.Contains(str))
                    throw new InterpreterError(InterpreterMessages._0_OBLIST_Already_Contains_An_Atom_Named_1, "LINK", str);

            var link = new ZilLink(str, oblist);
            oblist[str] = link;

            ctx.SetGlobalVal(link, value);
            return value;
        }

        [Subr]
        public static ZilObject ATOM(Context ctx, string pname)
        {
            SubrContracts(ctx);

            return new ZilAtom(pname, null, StdAtom.NONE);
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

            return ctx.GetProp(name, ctx.GetStdAtom(StdAtom.OBLIST)) as ObList ?? ctx.MakeObList(name);
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

            try
            {
                return ctx.PopObPath();
            }
            catch (InvalidOperationException)
            {
                throw new InterpreterError(InterpreterMessages.Misplaced_0, "ENDBLOCK");
            }
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
        public static ZilObject SET(Context ctx, ZilAtom atom, ZilObject value, LocalEnvironment env)
        {
            SubrContracts(ctx);

            env.SetLocalVal(atom, value);
            return value;
        }

        [Subr]
        public static ZilObject GVAL(Context ctx, ZilAtom atom)
        {
            SubrContracts(ctx);

            var result = ctx.GetGlobalVal(atom);
            if (result == null)
                throw new InterpreterError(
                    InterpreterMessages._0_Atom_1_Has_No_2_Value,
                    "GVAL",
                    atom.ToStringContext(ctx, false),
                    "global");

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

            if (item is ZilOffset offset)
                return offset.StructurePattern;

            return GETPROP(ctx, item, ctx.GetStdAtom(StdAtom.DECL));
        }

        [Subr("PUT-DECL")]
        public static ZilObject PUT_DECL(Context ctx, ZilObject item, ZilObject pattern)
        {
            SubrContracts(ctx);

            if (item is ZilOffset offset)
                return new ZilOffset(offset.Index, pattern, offset.ValuePattern);

            return PUTPROP(ctx, item, ctx.GetStdAtom(StdAtom.DECL), pattern);
        }

        [Subr]
        public static ZilObject LVAL(Context ctx, ZilAtom atom, LocalEnvironment env)
        {
            SubrContracts(ctx);

            var result = env.GetLocalVal(atom);
            if (result == null)
                throw new InterpreterError(
                    InterpreterMessages._0_Atom_1_Has_No_2_Value,
                    "LVAL",
                    atom.ToStringContext(ctx, false),
                    "local");

            return result;
        }

        [Subr]
        public static ZilObject UNASSIGN(Context ctx, ZilAtom atom, LocalEnvironment env)
        {
            SubrContracts(ctx);

            env.SetLocalVal(atom, null);
            return atom;
        }

        [Subr("ASSIGNED?")]
        public static ZilObject ASSIGNED_P(Context ctx, ZilAtom atom, LocalEnvironment env)
        {
            SubrContracts(ctx);

            return env.GetLocalVal(atom) != null ? ctx.TRUE : ctx.FALSE;
        }

        [Subr("BOUND?")]
        public static ZilObject BOUND_P(Context ctx, ZilAtom atom, LocalEnvironment env)
        {
            SubrContracts(ctx);

            return env.IsLocalBound(atom) ? ctx.TRUE : ctx.FALSE;
        }

        [Subr]
        public static ZilObject VALUE(Context ctx, ZilAtom atom, LocalEnvironment env)
        {
            SubrContracts(ctx);

            var result = env.GetLocalVal(atom) ?? ctx.GetGlobalVal(atom);
            if (result == null)
                throw new InterpreterError(
                    InterpreterMessages._0_Atom_1_Has_No_2_Value,
                    "VALUE",
                    atom.ToStringContext(ctx, false),
                    "local or global");

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
            if (defaultValue != null)
            {
                return defaultValue.Eval(ctx);
            }
            return ctx.FALSE;
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

            // set, and return first arg
            ctx.PutProp(item, indicator, value);
            return item;
        }

        [Subr]
        public static ZilObject ASSOCIATIONS(Context ctx)
        {
            SubrContracts(ctx);

            var results = ctx.GetAllAssociations();

            if (results.Length > 0)
            {
                return new ZilAsoc(results, 0);
            }
            return ctx.FALSE;
        }

        [Subr]
        public static ZilObject NEXT(Context ctx, ZilAsoc asoc)
        {
            SubrContracts(ctx);

            return asoc.GetNext() ?? ctx.FALSE;
        }

        [Subr]
        public static ZilObject ITEM(Context ctx, ZilAsoc asoc)
        {
            SubrContracts(ctx);

            return asoc.Item;
        }

        [Subr]
        public static ZilObject INDICATOR(Context ctx, ZilAsoc asoc)
        {
            SubrContracts(ctx);

            return asoc.Indicator;
        }

        [Subr]
        public static ZilObject AVALUE(Context ctx, ZilAsoc asoc)
        {
            SubrContracts(ctx);

            return asoc.Value;
        }
    }
}
