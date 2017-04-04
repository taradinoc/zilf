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
            if (obj is IEnumerable<ZilObject> enumerable)
            {
                foreach (var child in enumerable)
                {
                    if (child is ZilForm form)
                        action(form);

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
            if (!(last is ZilForm form))
                return false;

            if (!(form.First is ZilAtom atom) ||
                (atom.StdAtom != StdAtom.SET && atom.StdAtom != StdAtom.SETG))
                return false;

            if (!(form.Rest?.Rest?.First is ZilFix fix) || fix.Value != 0)
                return false;

            return true;
        }

        public static bool IsNonVariableForm(this ZilObject zo)
        {
            if (zo == null)
                return false;

            if (!(zo is ZilForm form))
                return false;

            if (!(form.First is ZilAtom first))
                return true;

            return first.StdAtom != StdAtom.GVAL && first.StdAtom != StdAtom.LVAL;
        }

        public static bool IsVariableRef(this ZilObject expr)
        {
            Contract.Requires(expr != null);

            if (!(expr is ZilForm form))
                return false;

            if (!(form.First is ZilAtom atom))
                return false;

            if (!(form.Rest?.First is ZilAtom))
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

            if (!(expr is ZilForm form))
                return false;

            if (!(form.First is ZilAtom atom))
                return false;

            if (!(form.Rest?.First is ZilAtom))
                return false;

            return atom.StdAtom == StdAtom.LVAL || atom.StdAtom == StdAtom.SET;
        }

        public static bool IsGlobalVariableRef(this ZilObject expr)
        {
            Contract.Requires(expr != null);

            if (!(expr is ZilForm form))
                return false;

            if (!(form.First is ZilAtom atom))
                return false;

            if (!(form.Rest?.First is ZilAtom))
                return false;

            return atom.StdAtom == StdAtom.GVAL || atom.StdAtom == StdAtom.SETG;
        }

        public static bool ModifiesLocal(this ZilObject expr, ZilAtom localAtom)
        {
            if (!(expr is ZilList list))
                return false;

            if (list is ZilForm)
            {
                if (list.First is ZilAtom atom &&
                    (atom.StdAtom == StdAtom.SET || atom.StdAtom == StdAtom.SETG) &&
                    list.Rest?.First == localAtom)
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
            if (zo is ZilForm form && form.First is ZilAtom head)
            {
                switch (head.StdAtom)
                {
                    case StdAtom.AND:
                    case StdAtom.OR:
                    case StdAtom.NOT:
                        return form.Rest.All(a => a.IsPredicate(zversion));

                    default:
                        return ZBuiltins.IsBuiltinPredCall(head.Text, zversion, form.Rest.Count());
                }
            }

            return false;
        }
    }
}
