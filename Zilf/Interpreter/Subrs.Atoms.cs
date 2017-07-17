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
using System.Diagnostics.CodeAnalysis;
using System.Diagnostics.Contracts;
using System.Linq;
using JetBrains.Annotations;
using Zilf.Interpreter.Values;
using Zilf.Language;
using Zilf.Diagnostics;

namespace Zilf.Interpreter
{
    [SuppressMessage("ReSharper", "InconsistentNaming")]
    static partial class Subrs
    {
        [NotNull]
        [Subr]
        [Subr("PNAME")]
        public static ZilObject SPNAME(Context ctx, [NotNull] ZilAtom atom)
        {
            Contract.Requires(atom != null);
            Contract.Ensures(Contract.Result<ZilObject>() != null);
            SubrContracts(ctx);
            return ZilString.FromString(atom.Text);
        }

        [Subr]
        public static ZilObject PARSE([NotNull] Context ctx, [NotNull] string text, [Decl("'10")] int radix = 10,
            [CanBeNull] [Either(typeof(ObList), typeof(ZilList))] ZilObject lookupObList = null)
        {
            Contract.Requires(ctx != null);
            Contract.Requires(text != null);
            SubrContracts(ctx);
            return PerformParse(ctx, text, radix, lookupObList, "PARSE", true);
        }

        [Subr]
        public static ZilObject LPARSE([NotNull] Context ctx, [NotNull] string text, [Decl("'10")] int radix = 10,
            [CanBeNull] [Either(typeof(ObList), typeof(ZilList))] ZilObject lookupObList = null)
        {
            Contract.Requires(ctx != null);
            Contract.Requires(text != null);
            SubrContracts(ctx);
            return PerformParse(ctx, text, radix, lookupObList, "LPARSE", false);
        }

        static ZilObject PerformParse([NotNull] [ProvidesContext] Context ctx, [NotNull] string text, int radix, ZilObject lookupObList,
            [NotNull] string name, bool singleResult)
        {
            Contract.Requires(ctx != null);
            Contract.Requires(text != null);
            Contract.Requires(name != null);

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

        [NotNull]
        [Subr]
        public static ZilObject UNPARSE([NotNull] Context ctx, [NotNull] ZilObject arg)
        {
            Contract.Requires(ctx != null);
            Contract.Requires(arg != null);
            Contract.Ensures(Contract.Result<ZilObject>() != null);
            SubrContracts(ctx);

            // in MDL, this takes an optional second argument (radix), but we don't bother

            return ZilString.FromString(arg.ToStringContext(ctx, false));
        }

        [Subr]
        public static ZilObject LOOKUP(Context ctx, string str, [NotNull] ObList oblist)
        {
            Contract.Requires(oblist != null);
            SubrContracts(ctx);
            return oblist.Contains(str) ? oblist[str] : ctx.FALSE;
        }

        /// <exception cref="InterpreterError"><paramref name="oblist"/> already contains an atom named <paramref name="stringOrAtom"/>, or <paramref name="stringOrAtom"/> is an atom that is already on a different OBLIST.</exception>
        [Subr]
        public static ZilObject INSERT(Context ctx,
            [Either(typeof(string), typeof(ZilAtom))] object stringOrAtom,
            [NotNull] ObList oblist)
        {
            Contract.Requires(oblist != null);
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

        [NotNull]
        [Subr]
        public static ZilObject REMOVE(Context ctx,
            [NotNull] [Either(typeof(ZilAtom), typeof(RemoveParams.PnameAndObList), DefaultParamDesc = "atom")] object atomOrNameAndObList)
        {
            Contract.Requires(atomOrNameAndObList != null);
            Contract.Ensures(Contract.Result<ZilObject>() != null);
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

        /// <exception cref="InterpreterError"><paramref name="oblist"/> already contains an atom named <paramref name="str"/>.</exception>
        [Subr]
        public static ZilObject LINK([NotNull] Context ctx, ZilObject value, string str, [NotNull] ObList oblist)
        {
            Contract.Requires(ctx != null);
            Contract.Requires(oblist != null);
            SubrContracts(ctx);

            if (oblist.Contains(str))
                throw new InterpreterError(InterpreterMessages._0_OBLIST_Already_Contains_An_Atom_Named_1, "LINK", str);

            var link = new ZilLink(str, oblist);
            oblist[str] = link;

            ctx.SetGlobalVal(link, value);
            return value;
        }

        [NotNull]
        [Subr]
        public static ZilObject ATOM(Context ctx, string pname)
        {
            Contract.Ensures(Contract.Result<ZilObject>() != null);
            SubrContracts(ctx);

            return new ZilAtom(pname, null, StdAtom.NONE);
        }

        [NotNull]
        [Subr]
        public static ZilObject ROOT([NotNull] Context ctx)
        {
            Contract.Requires(ctx != null);
            Contract.Ensures(Contract.Result<ZilObject>() != null);
            SubrContracts(ctx);

            return ctx.RootObList;
        }

        [NotNull]
        [Subr]
        public static ZilObject MOBLIST([NotNull] Context ctx, [NotNull] ZilAtom name)
        {
            Contract.Requires(ctx != null);
            Contract.Requires(name != null);
            Contract.Ensures(Contract.Result<ZilObject>() != null);
            SubrContracts(ctx);

            return ctx.GetProp(name, ctx.GetStdAtom(StdAtom.OBLIST)) as ObList ?? ctx.MakeObList(name);
        }

        [NotNull]
        [Subr("OBLIST?")]
        public static ZilObject OBLIST_P(Context ctx, [NotNull] ZilAtom atom)
        {
            Contract.Requires(atom != null);
            Contract.Ensures(Contract.Result<ZilObject>() != null);
            SubrContracts(ctx);

            return atom.ObList ?? ctx.FALSE;
        }

        [NotNull]
        [Subr]
        public static ZilObject BLOCK([NotNull] Context ctx, [NotNull] ZilList list)
        {
            Contract.Requires(ctx != null);
            Contract.Requires(list != null);
            Contract.Ensures(Contract.Result<ZilObject>() != null);
            SubrContracts(ctx);

            ctx.PushObPath(list);
            return list;
        }

        /// <exception cref="InterpreterError">ENDBLOCK is not allowed here.</exception>
        [NotNull]
        [Subr]
        public static ZilObject ENDBLOCK([NotNull] Context ctx)
        {
            Contract.Requires(ctx != null);
            Contract.Ensures(Contract.Result<ZilObject>() != null);
            SubrContracts(ctx);

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
        public static ZilObject SETG([NotNull] Context ctx, [NotNull] ZilAtom atom, ZilObject value)
        {
            Contract.Requires(ctx != null);
            Contract.Requires(atom != null);
            SubrContracts(ctx);

            ctx.SetGlobalVal(atom, value);
            return value;
        }

        [Subr]
        public static ZilObject SETG20([NotNull] Context ctx, [NotNull] ZilAtom atom, ZilObject value)
        {
            Contract.Requires(ctx != null);
            Contract.Requires(atom != null);
            SubrContracts(ctx);

            ctx.SetGlobalVal(atom, value);
            return value;
        }

        [Subr]
        public static ZilObject SET(Context ctx, [NotNull] ZilAtom atom, ZilObject value, [NotNull] LocalEnvironment env)
        {
            Contract.Requires(atom != null);
            Contract.Requires(env != null);
            SubrContracts(ctx);

            env.SetLocalVal(atom, value);
            return value;
        }

        /// <exception cref="InterpreterError"><paramref name="atom"/> has no global value.</exception>
        [NotNull]
        [Subr]
        public static ZilObject GVAL([NotNull] Context ctx, [NotNull] ZilAtom atom)
        {
            Contract.Requires(ctx != null);
            Contract.Requires(atom != null);
            Contract.Ensures(Contract.Result<ZilObject>() != null);
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

        [NotNull]
        [Subr("GASSIGNED?")]
        public static ZilObject GASSIGNED_P([NotNull] Context ctx, [NotNull] ZilAtom atom)
        {
            Contract.Requires(ctx != null);
            Contract.Requires(atom != null);
            Contract.Ensures(Contract.Result<ZilObject>() != null);
            SubrContracts(ctx);

            return ctx.GetGlobalVal(atom) != null ? ctx.TRUE : ctx.FALSE;
        }

        [NotNull]
        [Subr]
        public static ZilObject GUNASSIGN([NotNull] Context ctx, [NotNull] ZilAtom atom)
        {
            Contract.Requires(ctx != null);
            Contract.Requires(atom != null);
            Contract.Ensures(Contract.Result<ZilObject>() != null);
            SubrContracts(ctx);

            ctx.SetGlobalVal(atom, null);
            return atom;
        }

        [NotNull]
        [Subr("GBOUND?")]
        public static ZilObject GBOUND_P([NotNull] Context ctx, [NotNull] ZilAtom atom)
        {
            Contract.Requires(ctx != null);
            Contract.Requires(atom != null);
            Contract.Ensures(Contract.Result<ZilObject>() != null);
            SubrContracts(ctx);

            return ctx.GetGlobalBinding(atom, false) != null ? ctx.TRUE : ctx.FALSE;
        }

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

        [NotNull]
        [FSubr]
        public static ZilObject GDECL([NotNull] Context ctx, [NotNull] DeclParams.AtomsDeclSequence[] pairs)
        {
            Contract.Requires(ctx != null);
            Contract.Requires(pairs != null);
            Contract.Ensures(Contract.Result<ZilObject>() != null);
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

        [NotNull]
        [Subr("DECL?")]
        public static ZilObject DECL_P([NotNull] Context ctx, [NotNull] ZilObject value, [NotNull] ZilObject pattern)
        {
            Contract.Requires(ctx != null);
            Contract.Requires(value != null);
            Contract.Requires(pattern != null);
            Contract.Ensures(Contract.Result<ZilObject>() != null);
            SubrContracts(ctx);

            return Decl.Check(ctx, value, pattern) ? ctx.TRUE : ctx.FALSE;
        }

        [NotNull]
        [Subr("DECL-CHECK")]
        public static ZilObject DECL_CHECK([NotNull] Context ctx, bool enable)
        {
            Contract.Requires(ctx != null);
            Contract.Ensures(Contract.Result<ZilObject>() != null);
            SubrContracts(ctx);

            var wasEnabled = ctx.CheckDecls;
            ctx.CheckDecls = enable;
            return wasEnabled ? ctx.TRUE : ctx.FALSE;
        }

        [Subr("GET-DECL")]
        public static ZilResult GET_DECL(Context ctx, [NotNull] ZilObject item)
        {
            Contract.Requires(item != null);
            SubrContracts(ctx);

            if (item is ZilOffset offset)
                return offset.StructurePattern;

            return GETPROP(ctx, item, ctx.GetStdAtom(StdAtom.DECL));
        }

        [NotNull]
        [Subr("PUT-DECL")]
        public static ZilObject PUT_DECL(Context ctx, [NotNull] ZilObject item, ZilObject pattern)
        {
            Contract.Requires(item != null);
            Contract.Ensures(Contract.Result<ZilObject>() != null);
            SubrContracts(ctx);

            if (item is ZilOffset offset)
                return new ZilOffset(offset.Index, pattern, offset.ValuePattern);

            return PUTPROP(ctx, item, ctx.GetStdAtom(StdAtom.DECL), pattern);
        }

        /// <exception cref="InterpreterError"><paramref name="atom"/> has no local value in <paramref name="env"/>.</exception>
        [NotNull]
        [Subr]
        public static ZilObject LVAL(Context ctx, [NotNull] ZilAtom atom, [NotNull] LocalEnvironment env)
        {
            Contract.Requires(atom != null);
            Contract.Requires(env != null);
            Contract.Ensures(Contract.Result<ZilObject>() != null);
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

        /// <exception cref="ArgumentNullException"><paramref name="env"/> is <see langword="null"/></exception>
        [NotNull]
        [Subr]
        public static ZilObject UNASSIGN(Context ctx, [NotNull] ZilAtom atom, [NotNull] LocalEnvironment env)
        {
            Contract.Requires(atom != null);
            Contract.Requires(env != null);
            Contract.Ensures(Contract.Result<ZilObject>() != null);
            SubrContracts(ctx);

            if (atom == null)
                throw new ArgumentNullException(nameof(atom));
            if (env == null)
                throw new ArgumentNullException(nameof(env));

            env.SetLocalVal(atom, null);
            return atom;
        }

        [NotNull]
        [Subr("ASSIGNED?")]
        public static ZilObject ASSIGNED_P([NotNull] Context ctx, [NotNull] ZilAtom atom, [NotNull] LocalEnvironment env)
        {
            Contract.Requires(ctx != null);
            Contract.Requires(atom != null);
            Contract.Requires(env != null);
            Contract.Ensures(Contract.Result<ZilObject>() != null);
            SubrContracts(ctx);

            return env.GetLocalVal(atom) != null ? ctx.TRUE : ctx.FALSE;
        }

        [NotNull]
        [Subr("BOUND?")]
        public static ZilObject BOUND_P([NotNull] Context ctx, [NotNull] ZilAtom atom, [NotNull] LocalEnvironment env)
        {
            Contract.Requires(ctx != null);
            Contract.Requires(atom != null);
            Contract.Requires(env != null);
            Contract.Ensures(Contract.Result<ZilObject>() != null);
            SubrContracts(ctx);

            return env.IsLocalBound(atom) ? ctx.TRUE : ctx.FALSE;
        }

        /// <exception cref="InterpreterError"><paramref name="atom"/> has no local or global value in <paramref name="env"/>.</exception>
        [NotNull]
        [Subr]
        public static ZilObject VALUE(Context ctx, [NotNull] ZilAtom atom, [NotNull] LocalEnvironment env)
        {
            Contract.Requires(atom != null);
            Contract.Requires(env != null);
            Contract.Ensures(Contract.Result<ZilObject>() != null);
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
        public static ZilResult GETPROP([NotNull] Context ctx, [NotNull] ZilObject item, [NotNull] ZilObject indicator,
            [CanBeNull] ZilObject defaultValue = null)
        {
            Contract.Requires(ctx != null);
            Contract.Requires(item != null);
            Contract.Requires(indicator != null);
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

        [NotNull]
        [Subr]
        public static ZilObject PUTPROP([NotNull] Context ctx, [NotNull] ZilObject item, [NotNull] ZilObject indicator,
            [CanBeNull] ZilObject value = null)
        {
            Contract.Requires(ctx != null);
            Contract.Requires(item != null);
            Contract.Requires(indicator != null);
            Contract.Ensures(Contract.Result<ZilObject>() != null);
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

        [NotNull]
        [Subr]
        public static ZilObject ASSOCIATIONS([NotNull] Context ctx)
        {
            Contract.Requires(ctx != null);
            Contract.Ensures(Contract.Result<ZilObject>() != null);
            SubrContracts(ctx);

            var results = ctx.GetAllAssociations();

            if (results.Length > 0)
            {
                return new ZilAsoc(results, 0);
            }
            return ctx.FALSE;
        }

        [NotNull]
        [Subr]
        public static ZilObject NEXT(Context ctx, [NotNull] ZilAsoc asoc)
        {
            Contract.Requires(asoc != null);
            Contract.Ensures(Contract.Result<ZilObject>() != null);
            SubrContracts(ctx);

            return asoc.GetNext() ?? ctx.FALSE;
        }

        [Subr]
        public static ZilObject ITEM(Context ctx, [NotNull] ZilAsoc asoc)
        {
            Contract.Requires(asoc != null);
            SubrContracts(ctx);

            return asoc.Item;
        }

        [Subr]
        public static ZilObject INDICATOR(Context ctx, [NotNull] ZilAsoc asoc)
        {
            Contract.Requires(asoc != null);
            SubrContracts(ctx);

            return asoc.Indicator;
        }

        [Subr]
        public static ZilObject AVALUE(Context ctx, [NotNull] ZilAsoc asoc)
        {
            Contract.Requires(asoc != null);
            SubrContracts(ctx);

            return asoc.Value;
        }
    }
}
