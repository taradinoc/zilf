/* Copyright 2010-2023 Tara McGrew
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
        /// <summary>
        /// Returns the name of the given atom as a string.
        /// </summary>
        /// <param name="ctx"></param>
        /// <param name="atom">The atom whose name is to be returned.</param>
        /// <returns>The atom's name.</returns>
        [Subr]
        [Subr("PNAME")]
        public static ZilObject SPNAME(Context ctx, ZilAtom atom)
        {
            return ZilString.FromString(atom.Text);
        }

        /// <summary>
        /// Parses <paramref name="text"/> as ZIL code, returning the first expression.
        /// </summary>
        /// <param name="ctx"></param>
        /// <param name="text">The code to parse.</param>
        /// <param name="radix">The base to use for parsing numbers. Must be 10 if specified.</param>
        /// <param name="lookupObList">The oblist to use for looking up atoms.</param>
        /// <returns>The first expression parsed.</returns>
        [Subr]
        public static ZilObject PARSE(Context ctx, string text, [Decl("'10")] int radix = 10,
            [Either(typeof(ObList), typeof(ZilList))] ZilObject lookupObList = null!)
        {
            return PerformParse(ctx, text, radix, lookupObList, "PARSE", true);
        }

        /// <summary>
        /// Parses <paramref name="text"/> as ZIL code, returning all expressions.
        /// </summary>
        /// <param name="ctx"></param>
        /// <param name="text">The code to parse.</param>
        /// <param name="radix">The base to use for parsing numbers. Must be 10 if specified.</param>
        /// <param name="lookupObList">The oblist to use for looking up atoms.</param>
        /// <returns>A list of all expressions parsed.</returns>
        [Subr]
        public static ZilObject LPARSE(Context ctx, string text, [Decl("'10")] int radix = 10,
            [Either(typeof(ObList), typeof(ZilList))] ZilObject lookupObList = null!)
        {
            return PerformParse(ctx, text, radix, lookupObList, "LPARSE", false);
        }

        static ZilObject PerformParse(Context ctx, string text, int radix, ZilObject lookupObList,
            string name, bool singleResult)
        {
            ArgumentOutOfRangeException.ThrowIfNotEqual(radix, 10);

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

        /// <summary>
        /// Returns a round-trippable string representation of a value.
        /// </summary>
        /// <param name="ctx"></param>
        /// <param name="arg">The object to unparse.</param>
        /// <returns>A string that can be parsed to recreate the value.</returns>
        [Subr]
        public static ZilObject UNPARSE(Context ctx, ZilObject arg)
        {
            // in MDL, this takes an optional second argument (radix), but we don't bother

            return ZilString.FromString(arg.ToStringContext(ctx, false));
        }

        /// <summary>
        /// Looks up an atom in an oblist by name.
        /// </summary>
        /// <param name="ctx"></param>
        /// <param name="str">The name of an atom to search for.</param>
        /// <param name="oblist">The oblist to search.</param>
        /// <returns>The atom with the specified name, or false if not found.</returns>
        [Subr]
        public static ZilObject LOOKUP(Context ctx, string str, ObList oblist)
        {
            return oblist.Contains(str) ? oblist[str] : ctx.FALSE;
        }

        /// <summary>
        /// Inserts an atom into an oblist.
        /// </summary>
        /// <param name="ctx"></param>
        /// <param name="stringOrAtom">The atom to insert, or the name of a new atom to create and insert.</param>
        /// <param name="oblist">The oblist in which to insert the atom.</param>
        /// <returns>The inserted atom.</returns>
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

        /// <summary>
        /// Removes an atom from its oblist, or from a specified oblist by name.
        /// </summary>
        /// <param name="ctx"></param>
        /// <param name="atomOrNameAndObList">The atom to remove, or the atom's name and the oblist from which to remove it.</param>
        /// <returns>The atom if it was found, or false if not.</returns>
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

        /// <summary>
        /// Creates a link from an atom to a value, such that the value will be substituted when the atom's name is read in.
        /// </summary>
        /// <param name="ctx"></param>
        /// <param name="value">The value to substitute.</param>
        /// <param name="str">The name of the atom to link.</param>
        /// <param name="oblist">The oblist in which to create the link.</param>
        /// <returns>The value to substitute.</returns>
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

        /// <summary>
        /// Creates an atom with the specified name, on no oblist.
        /// </summary>
        /// <param name="ctx"></param>
        /// <param name="pname">The name of the atom to create.</param>
        /// <returns>The newly created atom.</returns>
        [Subr]
        public static ZilObject ATOM(Context ctx, string pname)
        {
            return new ZilAtom(pname, null, StdAtom.NONE);
        }

        /// <summary>
        /// Returns the root oblist.
        /// </summary>
        /// <param name="ctx"></param>
        /// <returns>The root oblist.</returns>
        [Subr]
        public static ZilObject ROOT(Context ctx)
        {
            return ctx.RootObList;
        }

        /// <summary>
        /// Creates or returns the oblist with the given name.
        /// </summary>
        /// <param name="ctx"></param>
        /// <param name="name">The name of the oblist.</param>
        /// <returns>The oblist.</returns>
        [Subr]
        public static ZilObject MOBLIST(Context ctx, ZilAtom name)
        {
            return ctx.GetProp(name, ctx.GetStdAtom(StdAtom.OBLIST)) as ObList ?? ctx.MakeObList(name);
        }

        /// <summary>
        /// Returns true if the specified atom is on an oblist.
        /// </summary>
        /// <param name="ctx"></param>
        /// <param name="atom">The atom to check.</param>
        /// <returns>The oblist that the atom is on, or false if it isn't on an oblist.</returns>
        [Subr("OBLIST?")]
        public static ZilObject OBLIST_P(Context ctx, ZilAtom atom)
        {
            return atom.ObList ?? ctx.FALSE;
        }

        /// <summary>
        /// Pushes the current oblist path (.OBLIST) onto a stack and sets it to a new value.
        /// </summary>
        /// <param name="ctx"></param>
        /// <param name="list">The new oblist path.</param>
        /// <returns>The new oblist path.</returns>
        [Subr]
        public static ZilObject BLOCK(Context ctx, ZilList list)
        {
            ctx.PushObPath(list);
            return list;
        }

        /// <summary>
        /// Pops the oblist path stack, restoring the previous value of .OBLIST.
        /// </summary>
        /// <param name="ctx"></param>
        /// <returns>The value of .OBLIST before the pop.</returns>
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

        /// <summary>
        /// Sets the global value of an atom. (Or, in MDL-ZIL? mode, an alias for GLOBAL.)
        /// </summary>
        /// <param name="ctx"></param>
        /// <param name="atom">The atom to set the global value for.</param>
        /// <param name="value">The value to set.</param>
        /// <returns>The value that was set.</returns>
        [Subr]
        [MdlZilRedirect(typeof(Subrs), nameof(GLOBAL), TopLevelOnly = true)]
        public static ZilObject SETG(Context ctx, ZilAtom atom, ZilObject value)
        {
            ctx.SetGlobalVal(atom, value);
            return value;
        }

        /// <summary>
        /// Sets the global value of an atom. (Equivalent to SETG.)
        /// </summary>
        /// <param name="ctx"></param>
        /// <param name="atom">The atom to set the global value for.</param>
        /// <param name="value">The value to set.</param>
        /// <returns>The value that was set.</returns>
        [Subr]
        public static ZilObject SETG20(Context ctx, ZilAtom atom, ZilObject value)
        {
            ctx.SetGlobalVal(atom, value);
            return value;
        }

        /// <summary>
        /// Sets the local value of an atom.
        /// </summary>
        /// <param name="ctx"></param>
        /// <param name="atom">The atom to set the local value of.</param>
        /// <param name="value">The value to set.</param>
        /// <param name="env">The local environment to set the value in. If omitted, defaults to the current environment.</param>
        /// <returns>The value that was set.</returns>
        [Subr]
        public static ZilObject SET(Context ctx, ZilAtom atom, ZilObject value, LocalEnvironment env)
        {
            env.SetLocalVal(atom, value);
            return value;
        }

        /// <summary>
        /// Gets the global value of an atom.
        /// </summary>
        /// <param name="ctx"></param>
        /// <param name="atom">The atom to get the global value for.</param>
        /// <returns>The atom's global value.</returns>
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

        /// <summary>
        /// Returns true if the specified atom has a global value.
        /// </summary>
        /// <param name="ctx"></param>
        /// <param name="atom">The atom to check.</param>
        /// <returns>True if the atom has a global value; otherwise, false.</returns>
        [Subr("GASSIGNED?")]
        public static ZilObject GASSIGNED_P(Context ctx, ZilAtom atom)
        {
            return ctx.GetGlobalVal(atom) != null ? ctx.TRUE : ctx.FALSE;
        }

        /// <summary>
        /// Removes the global value of an atom.
        /// </summary>
        /// <param name="ctx"></param>
        /// <param name="atom">The atom to unassign.</param>
        /// <returns>The atom.</returns>
        [Subr]
        public static ZilObject GUNASSIGN(Context ctx, ZilAtom atom)
        {
            ctx.SetGlobalVal(atom, null);
            return atom;
        }

        /// <summary>
        /// Returns true if the specified atom has ever had a global value.
        /// </summary>
        /// <param name="ctx"></param>
        /// <param name="atom">The atom to check.</param>
        /// <returns>True if the atom has ever had a global value; otherwise, false.</returns>
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

        /// <summary>
        /// Sets DECL constraints on the global valuews of one or more atoms.
        /// </summary>
        /// <param name="ctx"></param>
        /// <param name="pairs">A sequence of pairs of lists of atoms and corresponding DECLs.</param>
        /// <returns>True.</returns>
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

        /// <summary>
        /// Checks whether a value matches a DECL pattern.
        /// </summary>
        /// <param name="ctx"></param>
        /// <param name="value">The value to check.</param>
        /// <param name="pattern">The DECL pattern to check the value against.</param>
        /// <returns>True if the value matches the pattern; otherwise, false.</returns>
        [Subr("DECL?")]
        public static ZilObject DECL_P(Context ctx, ZilObject value, ZilObject pattern) =>
            Decl.Check(ctx, value, pattern) ? ctx.TRUE : ctx.FALSE;

        /// <summary>
        /// Enables or disables DECL checking.
        /// </summary>
        /// <param name="ctx"></param>
        /// <param name="enable">True to enable DECL checking; false to disable.</param>
        /// <returns>True if DECL checking was previously enabled; otherwise, false.</returns>
        [Subr("DECL-CHECK")]
        public static ZilObject DECL_CHECK(Context ctx, bool enable)
        {
            var wasEnabled = ctx.CheckDecls;
            ctx.CheckDecls = enable;
            return wasEnabled ? ctx.TRUE : ctx.FALSE;
        }

        /// <summary>
        /// Gets the DECL pattern associated with an item.
        /// </summary>
        /// <param name="ctx"></param>
        /// <param name="item">The item to check (a type name or OFFSET).</param>
        /// <returns>The associated DECL pattern, or false if none exists.</returns>
        [Subr("GET-DECL")]
        public static ZilResult GET_DECL(Context ctx, ZilObject item)
        {
            if (item is ZilOffset offset)
                return offset.StructurePattern;

            return GETPROP(ctx, item, ctx.GetStdAtom(StdAtom.DECL));
        }

        /// <summary>
        /// Sets the DECL pattern associated with an item.
        /// </summary>
        /// <param name="ctx"></param>
        /// <param name="item">The item to set (a type name or OFFSET).</param>
        /// <param name="pattern">The DECL pattern to associate with the item.</param>
        /// <returns>The pattern.</returns>
        [Subr("PUT-DECL")]
        public static ZilObject PUT_DECL(Context ctx, ZilObject item, ZilObject pattern)
        {
            if (item is ZilOffset offset)
                return new ZilOffset(offset.Index, pattern, offset.ValuePattern);

            return PUTPROP(ctx, item, ctx.GetStdAtom(StdAtom.DECL), pattern);
        }

        /// <summary>
        /// Gets the local value of an atom.
        /// </summary>
        /// <param name="ctx"></param>
        /// <param name="atom">The atom to get the local value of.</param>
        /// <param name="env">The environment from which to get the local value. If omitted, defaults to the current environment.</param>
        /// <returns>The atom's local value.</returns>
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

        /// <summary>
        /// Removes the local value of an atom.
        /// </summary>
        /// <param name="ctx"></param>
        /// <param name="atom">The atom to remove the local value of.</param>
        /// <param name="env">The environment in which to remove the local value. If omitted, defaults to the current environment.</param>
        /// <returns>The atom.</returns>
        /// <exception cref="ArgumentNullException"><paramref name="env"/> is <see langword="null"/></exception>
        [Subr]
        public static ZilObject UNASSIGN(Context ctx, ZilAtom atom, LocalEnvironment env)
        {
            ArgumentNullException.ThrowIfNull(atom);
            ArgumentNullException.ThrowIfNull(env);

            env.SetLocalVal(atom, null);
            return atom;
        }

        /// <summary>
        /// Returns true if the specified atom has a local value.
        /// </summary>
        /// <param name="ctx"></param>
        /// <param name="atom">The atom to check.</param>
        /// <param name="env">The environment in which to check. If omitted, defaults to the current environment.</param>
        /// <returns>True if the atom has a local value; otherwise, false.</returns>
        [Subr("ASSIGNED?")]
        public static ZilObject ASSIGNED_P(Context ctx, ZilAtom atom, LocalEnvironment env) =>
            env.GetLocalVal(atom) != null ? ctx.TRUE : ctx.FALSE;

        /// <summary>
        /// Returns true if the specified atom has a local binding.
        /// </summary>
        /// <param name="ctx"></param>
        /// <param name="atom">The atom to check.</param>
        /// <param name="env">The environment in which to check. If omitted, defaults to the current environment.</param>
        /// <returns>True if the atom has a local binding in the specified environment or any parent environment; otherwise, false.</returns>
        [Subr("BOUND?")]
        public static ZilObject BOUND_P(Context ctx, ZilAtom atom, LocalEnvironment env) =>
            env.IsLocalBound(atom) ? ctx.TRUE : ctx.FALSE;

        /// <summary>
        /// Gets the local or global value of an atom.
        /// </summary>
        /// <param name="ctx"></param>
        /// <param name="atom">The atom to get the value of.</param>
        /// <param name="env">The environment in which to look for a local value. If omitted, defaults to the current environment.</param>
        /// <returns>The atom's local value, if present; otherwise, its global value.</returns>
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

        /// <summary>
        /// Gets the value associated with a pair of values (item and indicator).
        /// </summary>
        /// <param name="ctx"></param>
        /// <param name="item">The item (first element of the pair).</param>
        /// <param name="indicator">The indicator (second element of the pair).</param>
        /// <param name="defaultValue">The default value to return if the associated value is not found. If omitted, defaults to false.</param>
        /// <returns>The value associated with the item and indicator, or the default value if not found.</returns>
        [Subr]
        public static ZilResult GETPROP(Context ctx, ZilObject item, ZilObject indicator,
            ZilObject? defaultValue = null) =>
            ctx.GetProp(item, indicator) ?? defaultValue?.Eval(ctx) ?? ctx.FALSE;

        /// <summary>
        /// Sets or clears the value associated with a pair of values (item and indicator).
        /// </summary>
        /// <param name="ctx"></param>
        /// <param name="item">The item (first element of the pair).</param>
        /// <param name="indicator">The indicator (second element of the pair).</param>
        /// <param name="value">The value to associate with the item and indicator, or false to clear the association.</param>
        /// <returns>The newly associated value, or the previous associated value if cleared.</returns>
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

        /// <summary>
        /// Starts iterating through all global associations.
        /// </summary>
        /// <param name="ctx"></param>
        /// <returns>An ASOC pointing to the first association, or false if none exist.</returns>
        [Subr]
        public static ZilObject ASSOCIATIONS(Context ctx)
        {
            var results = ctx.GetAllAssociations();

            return results.Length > 0 ? new ZilAsoc(results, 0) : ctx.FALSE;
        }

        /// <summary>
        /// Continues iterating through all global associations.
        /// </summary>
        /// <param name="ctx"></param>
        /// <param name="asoc">The ASOC pointing to the previous association.</param>
        /// <returns>An ASOC pointing to the next association, or false if no more associations exist.</returns>
        [Subr]
        public static ZilObject NEXT(Context ctx, ZilAsoc asoc) => asoc.GetNext() ?? ctx.FALSE;

        /// <summary>
        /// Gets the item from the association pointed to by an ASOC.
        /// </summary>
        /// <param name="ctx"></param>
        /// <param name="asoc">The ASOC.</param>
        /// <returns>The item from the association.</returns>
        [Subr]
        public static ZilObject ITEM(Context ctx, ZilAsoc asoc) => asoc.Item;

        /// <summary>
        /// Gets the indicator from the association pointed to by an ASOC.
        /// </summary>
        /// <param name="ctx"></param>
        /// <param name="asoc">The ASOC.</param>
        /// <returns>The indicator from the association.</returns>
        [Subr]
        public static ZilObject INDICATOR(Context ctx, ZilAsoc asoc) => asoc.Indicator;

        /// <summary>
        /// Gets the value from the association pointed to by an ASOC.
        /// </summary>
        /// <param name="ctx"></param>
        /// <param name="asoc">The ASOC.</param>
        /// <returns>The value from the association.</returns>
        [Subr]
        public static ZilObject AVALUE(Context ctx, ZilAsoc asoc) => asoc.Value;
    }
}
