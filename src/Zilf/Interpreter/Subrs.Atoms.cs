/* Copyright 2010-2018 Jesse McGrew
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
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using Zilf.Common;
using Zilf.Interpreter.Values;
using Zilf.Language;
using Zilf.Diagnostics;

namespace Zilf.Interpreter
{
    [SuppressMessage("ReSharper", "InconsistentNaming")]
    static partial class Subrs
    {
        [Subr]
        [Subr("PNAME")]
        public static ZilObject SPNAME(Context ctx, ZilAtom atom)
        {
            return ZilString.FromString(atom.Text);
        }

        [Subr]
        public static ZilObject PARSE(Context ctx, string text, [Decl("'10")] int radix = 10,
            [Either(typeof(ObList), typeof(ZilList))] ZilObject lookupObList = null!)
        {
            return PerformParse(ctx, text, radix, lookupObList, "PARSE", true);
        }

        [Subr]
        public static ZilObject LPARSE(Context ctx, string text, [Decl("'10")] int radix = 10,
            [Either(typeof(ObList), typeof(ZilList))] ZilObject lookupObList = null!)
        {
            return PerformParse(ctx, text, radix, lookupObList, "LPARSE", false);
        }

        static ZilObject PerformParse(Context ctx, string text, int radix, ZilObject lookupObList,
            string name, bool singleResult)
        {
            if (radix != 10)
                throw new ArgumentOutOfRangeException(nameof(radix));

            using var innerEnv = ctx.PushEnvironment();

            if (lookupObList != null)
            {
                if (lookupObList is ObList)
                    lookupObList = new ZilList(lookupObList, new ZilList(null, null));

                innerEnv.Rebind(ctx.GetStdAtom(StdAtom.OBLIST), lookupObList);
            }

            var ztree = Program.Parse(ctx, text); // TODO: move into FrontEnd class
            if (!singleResult)
                return new ZilList(ztree);

            try
            {
                return ztree.First();
            }
            catch (InvalidOperationException ex)
            {
                throw new InterpreterError(InterpreterMessages._0_No_Expressions_Found, name, ex);
            }
        }

        [Subr]
        public static ZilObject UNPARSE(Context ctx, ZilObject arg)
        {
            // in MDL, this takes an optional second argument (radix), but we don't bother

            return ZilString.FromString(arg.ToStringContext(ctx, false));
        }

        [Subr]
        public static ZilObject LOOKUP(Context ctx, string str, ObList oblist)
        {
            return oblist.Contains(str) ? oblist[str] : ctx.FALSE;
        }

        /// <exception cref="InterpreterError"><paramref name="oblist"/> already contains an atom named <paramref name="stringOrAtom"/>, or <paramref name="stringOrAtom"/> is an atom that is already on a different OBLIST.</exception>
        [Subr]
        public static ZilObject INSERT(Context ctx,
            [Either(typeof(string), typeof(ZilAtom))] object stringOrAtom,
             ObList oblist)
        {
            switch (stringOrAtom)
            {
                case string str when oblist.Contains(str):
                    throw new InterpreterError(InterpreterMessages._0_OBLIST_Already_Contains_An_Atom_Named_1, "INSERT", str);

                case string str:
                    return oblist[str];

                case ZilAtom atom when atom.ObList != null:
                    throw new InterpreterError(InterpreterMessages._0_Atom_1_Is_Already_On_An_OBLIST, "INSERT",
                        atom.ToStringContext(ctx, false));

                case ZilAtom atom when oblist.Contains(atom.Text):
                    throw new InterpreterError(InterpreterMessages._0_OBLIST_Already_Contains_An_Atom_Named_1, "INSERT",
                        atom.Text);

                case ZilAtom atom:
                    atom.ObList = oblist;
                    return atom;
            }

            throw new UnreachableCodeException();
        }

#pragma warning disable CS0649
        public static class RemoveParams
        {
            [ZilSequenceParam]
            public struct PnameAndObList
            {
                public string Pname;
                public ObList ObList;
            }
        }
#pragma warning restore CS0649

        [Subr]
        public static ZilObject REMOVE(Context ctx,
             [Either(typeof(ZilAtom), typeof(RemoveParams.PnameAndObList), DefaultParamDesc = "atom")] object atomOrNameAndObList)
        {
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

        /// <exception cref="InterpreterError"><paramref name="oblist"/> already contains an atom named <paramref name="str"/>.</exception>
        [Subr]
        public static ZilObject LINK(Context ctx, ZilObject value, string str, ObList oblist)
        {
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
            return new ZilAtom(pname, null, StdAtom.NONE);
        }

        [Subr]
        public static ZilObject ROOT(Context ctx)
        {
            return ctx.RootObList;
        }

        [Subr]
        public static ZilObject MOBLIST(Context ctx, ZilAtom name)
        {
            return ctx.GetProp(name, ctx.GetStdAtom(StdAtom.OBLIST)) as ObList ?? ctx.MakeObList(name);
        }

        [Subr("OBLIST?")]
        public static ZilObject OBLIST_P(Context ctx, ZilAtom atom)
        {
            return atom.ObList ?? ctx.FALSE;
        }

        [Subr]
        public static ZilObject BLOCK(Context ctx, ZilList list)
        {
            ctx.PushObPath(list);
            return list;
        }

        /// <exception cref="InterpreterError">ENDBLOCK is not allowed here.</exception>
        [Subr]
        public static ZilObject ENDBLOCK(Context ctx)
        {
            try
            {
                return ctx.PopObPath() ?? ctx.FALSE;
            }
            catch (InvalidOperationException ex)
            {
                throw new InterpreterError(InterpreterMessages.Misplaced_0, "ENDBLOCK", ex);
            }
        }

        [Subr]
        [MdlZilRedirect(typeof(Subrs), nameof(GLOBAL), TopLevelOnly = true)]
        public static ZilObject SETG(Context ctx, ZilAtom atom, ZilObject value)
        {
            ctx.SetGlobalVal(atom, value);
            return value;
        }

        [Subr]
        public static ZilObject SETG20(Context ctx, ZilAtom atom, ZilObject value)
        {
            ctx.SetGlobalVal(atom, value);
            return value;
        }

        [Subr]
        public static ZilObject SET(Context ctx, ZilAtom atom, ZilObject value, LocalEnvironment env)
        {
            env.SetLocalVal(atom, value);
            return value;
        }

        /// <exception cref="InterpreterError"><paramref name="atom"/> has no global value.</exception>
        [Subr]
        public static ZilObject GVAL(Context ctx, ZilAtom atom)
        {
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
            return ctx.GetGlobalVal(atom) != null ? ctx.TRUE : ctx.FALSE;
        }

        [Subr]
        public static ZilObject GUNASSIGN(Context ctx, ZilAtom atom)
        {
            ctx.SetGlobalVal(atom, null);
            return atom;
        }

        [Subr("GBOUND?")]
        public static ZilObject GBOUND_P(Context ctx, ZilAtom atom) =>
            ctx.TryGetGlobalBinding(atom, out _) ? ctx.TRUE : ctx.FALSE;

#pragma warning disable CS0649
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
#pragma warning restore CS0649

        [FSubr]
        public static ZilObject GDECL(Context ctx, DeclParams.AtomsDeclSequence[] pairs)
        {
            foreach (var pair in pairs)
            {
                foreach (var atom in pair.Atoms.Atoms)
                {
                    var binding = ctx.EnsureGlobalBinding(atom);
                    binding.Decl = pair.Decl;
                }
            }

            return ctx.TRUE;
        }

        [Subr("DECL?")]
        public static ZilObject DECL_P(Context ctx, ZilObject value, ZilObject pattern) =>
            Decl.Check(ctx, value, pattern) ? ctx.TRUE : ctx.FALSE;

        [Subr("DECL-CHECK")]
        public static ZilObject DECL_CHECK(Context ctx, bool enable)
        {
            var wasEnabled = ctx.CheckDecls;
            ctx.CheckDecls = enable;
            return wasEnabled ? ctx.TRUE : ctx.FALSE;
        }

        [Subr("GET-DECL")]
        public static ZilResult GET_DECL(Context ctx, ZilObject item)
        {
            if (item is ZilOffset offset)
                return offset.StructurePattern;

            return GETPROP(ctx, item, ctx.GetStdAtom(StdAtom.DECL));
        }

        [Subr("PUT-DECL")]
        public static ZilObject PUT_DECL(Context ctx, ZilObject item, ZilObject pattern)
        {
            if (item is ZilOffset offset)
                return new ZilOffset(offset.Index, pattern, offset.ValuePattern);

            return PUTPROP(ctx, item, ctx.GetStdAtom(StdAtom.DECL), pattern);
        }

        /// <exception cref="InterpreterError"><paramref name="atom"/> has no local value in <paramref name="env"/>.</exception>
        [Subr]
        public static ZilObject LVAL(Context ctx, ZilAtom atom, LocalEnvironment env)
        {
            var result = env.GetLocalVal(atom);
            if (result == null)
            {
                throw new InterpreterError(
                    InterpreterMessages._0_Atom_1_Has_No_2_Value,
                    "LVAL",
                    atom.ToStringContext(ctx, false),
                    "local");
            }

            return result;
        }

        /// <exception cref="ArgumentNullException"><paramref name="env"/> is <see langword="null"/></exception>
        [Subr]
        public static ZilObject UNASSIGN(Context ctx, ZilAtom atom, LocalEnvironment env)
        {
            if (atom == null)
                throw new ArgumentNullException(nameof(atom));
            if (env == null)
                throw new ArgumentNullException(nameof(env));

            env.SetLocalVal(atom, null);
            return atom;
        }

        [Subr("ASSIGNED?")]
        public static ZilObject ASSIGNED_P(Context ctx, ZilAtom atom, LocalEnvironment env) =>
            env.GetLocalVal(atom) != null ? ctx.TRUE : ctx.FALSE;

        [Subr("BOUND?")]
        public static ZilObject BOUND_P(Context ctx, ZilAtom atom, LocalEnvironment env) =>
            env.IsLocalBound(atom) ? ctx.TRUE : ctx.FALSE;

        /// <exception cref="InterpreterError"><paramref name="atom"/> has no local or global value in <paramref name="env"/>.</exception>
        [Subr]
        public static ZilObject VALUE(Context ctx, ZilAtom atom, LocalEnvironment env)
        {
            var result = env.GetLocalVal(atom) ?? ctx.GetGlobalVal(atom);
            if (result == null)
            {
                throw new InterpreterError(
                    InterpreterMessages._0_Atom_1_Has_No_2_Value,
                    "VALUE",
                    atom.ToStringContext(ctx, false),
                    "local or global");
            }

            return result;
        }

        [Subr]
        public static ZilResult GETPROP(Context ctx, ZilObject item, ZilObject indicator,
            ZilObject? defaultValue = null) =>
            ctx.GetProp(item, indicator) ?? defaultValue?.Eval(ctx) ?? ctx.FALSE;

        [Subr]
        public static ZilObject PUTPROP(Context ctx, ZilObject item, ZilObject indicator,
            ZilObject? value = null)
        {
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
            var results = ctx.GetAllAssociations();

            return results.Length > 0 ? new ZilAsoc(results, 0) : ctx.FALSE;
        }

        [Subr]
        public static ZilObject NEXT(Context ctx, ZilAsoc asoc) => asoc.GetNext() ?? ctx.FALSE;

        [Subr]
        public static ZilObject ITEM(Context ctx, ZilAsoc asoc) => asoc.Item;

        [Subr]
        public static ZilObject INDICATOR(Context ctx, ZilAsoc asoc) => asoc.Indicator;

        [Subr]
        public static ZilObject AVALUE(Context ctx, ZilAsoc asoc) => asoc.Value;
    }
}
