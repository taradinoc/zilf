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
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using Zilf.Compiler.Builtins;
using Zilf.Interpreter.Values;
using Zilf.Language;
using Zilf.ZModel.Values;
using Zilf.Diagnostics;
using Zilf.Interpreter;

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
            return last is ZilForm form &&
                form.First is ZilAtom atom &&
                (atom.StdAtom == StdAtom.SET || atom.StdAtom == StdAtom.SETG) &&
                form.Rest?.Rest?.First is ZilFix fix &&
                fix.Value == 0;
        }

        public static bool IsNonVariableForm(this ZilObject zo)
        {
            return zo is ZilForm form &&
                form.First is ZilAtom first &&
                first.StdAtom != StdAtom.GVAL && first.StdAtom != StdAtom.LVAL;
        }

        public static bool IsVariableRef(this ZilObject expr)
        {
            if (expr is ZilForm form &&
                form.First is ZilAtom atom &&
                form.Rest?.First is ZilAtom)
            {
                switch (atom.StdAtom)
                {
                    case StdAtom.LVAL:
                    case StdAtom.GVAL:
                    case StdAtom.SET:
                    case StdAtom.SETG:
                        return true;
                }
            }

            return false;
        }

        public static bool IsLocalVariableRef(this ZilObject expr)
        {
            return expr is ZilForm form &&
                form.First is ZilAtom atom &&
                form.Rest?.First is ZilAtom &&
                (atom.StdAtom == StdAtom.LVAL || atom.StdAtom == StdAtom.SET);
        }

        public static bool IsGlobalVariableRef(this ZilObject expr)
        {
            return expr is ZilForm form &&
                form.First is ZilAtom atom &&
                form.Rest?.First is ZilAtom &&
                (atom.StdAtom == StdAtom.GVAL || atom.StdAtom == StdAtom.SETG);
        }

        public static bool ModifiesLocal(this ZilObject expr, ZilAtom localAtom)
        {
            if (expr is not ZilListBase list)
                return false;

            if (list is ZilForm &&
                list.First is ZilAtom atom &&
                (atom.StdAtom == StdAtom.SET || atom.StdAtom == StdAtom.SETG) &&
                list.Rest?.First == localAtom)
            {
                return true;
            }

            return list.Any(zo => ModifiesLocal(zo, localAtom));
        }

        public static bool IsPredicate(this ZilObject zo, int zversion)
        {
            if (zo is not ZilForm form || form.First is not ZilAtom head)
                return false;

            Debug.Assert(form.Rest != null);

            // ReSharper disable once SwitchStatementMissingSomeCases
            return head.StdAtom switch
            {
                StdAtom.AND or StdAtom.OR or StdAtom.NOT => form.Rest.All(a => a.IsPredicate(zversion)),
                _ => ZBuiltins.IsBuiltinPredCall(head.Text, zversion, form.Rest.Count()),
            };
        }

        /// <summary>
        /// Recursively expands macros and cracks ADECLs to prepare an expression for compilation.
        /// </summary>
        /// <param name="zo">The expression.</param>
        /// <param name="ctx">The context.</param>
        /// <returns>The unwrapped expression. If macro expansion resulted in a SPLICE, this will be a call to BIND.</returns>
        public static ZilObject Unwrap(this ZilObject zo, Context ctx)
        {
            var src = zo.SourceLine;

            using (DiagnosticContext.Push(src))
            {
                while (true)
                {
                    zo = (ZilObject)zo.Expand(ctx);

                    switch (zo)
                    {
                        case ZilForm form when form.IsEmpty:
                            return ctx.FALSE;

                        case ZilAdecl adecl:
                            // TODO: check DECL
                            zo = adecl.First;
                            break;

                        case IMayExpandAfterEvaluation expandAfter when expandAfter.ShouldExpandAfterEvaluation:
                            // TODO: don't use Parse here
                            zo = expandAfter.ExpandAfterEvaluation()
                                .FirstOrCombine(zos =>
                                    Program.Parse(ctx, src, "<BIND () {0:SPLICE}>", new ZilList(zos))
                                        .Single());
                            break;

                        case ZilMacroResult macroResult:
                            zo = macroResult.Inner;
                            break;

                        default:
                            return zo;
                    }
                }
            }
        }

        static IEnumerable<T> ReconstructSequence<T>(T first, IEnumerator<T> enumerator)
        {
            yield return first;

            do
            {
                var item = enumerator.Current;
                Debug.Assert(item != null, nameof(item) + " != null");
                yield return item;
            } while (enumerator.MoveNext());
        }

        public delegate T SequenceCombiner<T>(IEnumerable<T> sequence);

        public static T FirstOrCombine<T>(this IEnumerable<T> sequence, SequenceCombiner<T> combiner)
        {
            using var tor = sequence.GetEnumerator();

            if (!tor.MoveNext())
                throw new InvalidOperationException("No items in sequence");

            var first = tor.Current;
            Debug.Assert(first != null);

            return tor.MoveNext() ? combiner(ReconstructSequence(first, tor)) : first;
        }
    }
}
