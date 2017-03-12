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
using System.Collections.Generic;
using System.Diagnostics.Contracts;
using System.Linq;
using Zilf.Compiler.Builtins;
using Zilf.Interpreter.Values;
using Zilf.Language;
using Zilf.ZModel.Values;

namespace Zilf.Compiler
{
    static class CompilationExtensions
    {
        public static void WalkChildren(this ZilObject obj, Action<ZilForm> action)
        {
            var enumerable = obj as IEnumerable<ZilObject>;

            if (enumerable != null)
            {
                foreach (var child in enumerable)
                {
                    if (child is ZilForm)
                        action((ZilForm)child);

                    WalkChildren(child, action);
                }
            }
        }

        public static void WalkRoutineForms(this ZilRoutine routine, Action<ZilForm> action)
        {
            Contract.Requires(routine != null);
            Contract.Requires(action != null);

            var children =
                routine.ArgSpec.Select(ai => ai.DefaultValue)
                .Concat(routine.Body);

            foreach (var form in children.OfType<ZilForm>())
            {
                action(form);
                form.WalkChildren(action);
            }
        }

        public static bool IsSetToZeroForm(this ZilObject last)
        {
            var form = last as ZilForm;
            if (form == null)
                return false;

            var atom = form.First as ZilAtom;
            if (atom == null ||
                (atom.StdAtom != StdAtom.SET && atom.StdAtom != StdAtom.SETG))
                return false;

            ZilFix fix;
            if (form.Rest == null || form.Rest.Rest == null ||
                (fix = form.Rest.Rest.First as ZilFix) == null ||
                fix.Value != 0)
                return false;

            return true;
        }

        public static bool IsNonVariableForm(this ZilObject zo)
        {
            if (zo == null)
                return false;

            var form = zo as ZilForm;
            if (form == null)
                return false;

            var first = form.First as ZilAtom;
            if (first == null)
                return true;

            return first.StdAtom != StdAtom.GVAL && first.StdAtom != StdAtom.LVAL;
        }

        public static bool IsVariableRef(this ZilObject expr)
        {
            Contract.Requires(expr != null);

            var form = expr as ZilForm;
            if (form == null)
                return false;

            var atom = form.First as ZilAtom;
            if (atom == null)
                return false;

            if (form.Rest == null || !(form.Rest.First is ZilAtom))
                return false;

            switch (atom.StdAtom)
            {
                case StdAtom.LVAL:
                case StdAtom.GVAL:
                case StdAtom.SET:
                case StdAtom.SETG:
                    return true;

                default:
                    return false;
            }
        }

        public static bool IsLocalVariableRef(this ZilObject expr)
        {
            Contract.Requires(expr != null);

            var form = expr as ZilForm;
            if (form == null)
                return false;

            var atom = form.First as ZilAtom;
            if (atom == null)
                return false;

            if (form.Rest == null || !(form.Rest.First is ZilAtom))
                return false;

            return atom.StdAtom == StdAtom.LVAL || atom.StdAtom == StdAtom.SET;
        }

        public static bool IsGlobalVariableRef(this ZilObject expr)
        {
            Contract.Requires(expr != null);

            var form = expr as ZilForm;
            if (form == null)
                return false;

            var atom = form.First as ZilAtom;
            if (atom == null)
                return false;

            if (form.Rest == null || !(form.Rest.First is ZilAtom))
                return false;

            return atom.StdAtom == StdAtom.GVAL || atom.StdAtom == StdAtom.SETG;
        }

        public static bool ModifiesLocal(this ZilObject expr, ZilAtom localAtom)
        {
            var list = expr as ZilList;
            if (list == null)
                return false;

            if (list is ZilForm)
            {
                var atom = list.First as ZilAtom;
                if (atom != null &&
                    (atom.StdAtom == StdAtom.SET || atom.StdAtom == StdAtom.SETG) &&
                    list.Rest != null && list.Rest.First == localAtom)
                {
                    return true;
                }
            }

            foreach (ZilObject zo in list)
                if (ModifiesLocal(zo, localAtom))
                    return true;

            return false;
        }

        public static bool IsPredicate(this ZilObject zo, int zversion)
        {
            var form = zo as ZilForm;

            if (form == null)
                return false;

            var head = form.First as ZilAtom;

            switch (head?.StdAtom)
            {
                case StdAtom.AND:
                case StdAtom.OR:
                case StdAtom.NOT:
                    return form.Rest.All(a => a.IsPredicate(zversion));

                case null:
                    return false;

                default:
                    return ZBuiltins.IsBuiltinPredCall(head.Text, zversion, form.Rest.Count());
            }
        }
    }
}
