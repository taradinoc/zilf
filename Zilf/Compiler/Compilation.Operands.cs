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
using System.Diagnostics;
using System.Linq;
using Zilf.Emit;
using Zilf.Interpreter.Values;
using Zilf.Language;
using JetBrains.Annotations;

namespace Zilf.Compiler
{
    partial class Compilation
    {
        /// <exception cref="CompilerError">Local variables are not allowed here, or an error occurred while compiling a subexpression.</exception>
        [NotNull]
        public IOperands CompileOperands([NotNull] IRoutineBuilder rb, [NotNull] ISourceLine src, [NotNull] params ZilObject[] exprs)
        {
            int length = exprs.Length;
            var values = new IOperand[length];
            var temps = new bool[length];
            var tempAtom = ZilAtom.Parse("?TMP", Context);

            // find the index of the last expr with side effects (or -1)
            int marker = -1;
            for (int i = length - 1; i >= 0; i--)
            {
                if (HasSideEffects(exprs[i]))
                {
                    marker = i;
                    break;
                }
            }

            /* Evaluate arguments up to and including the marker, left to right.
             * Force the results into temp variables, except:
             * - Constants
             * - Local variables, if they aren't modified by any following argument
             *   (i.e. no following argument includes SET[G] to the particular local)
             * - Global variables, if they aren't potentially modified by any following
             *   argument (i.e. no following arguments include routine calls or SET[G] to the
             *   particular global)
             * - The marker itself, if (1) its natural location is not the stack, or
             *   (2) every following argument is a constant or variable.
             */
            for (int i = 0; i <= marker; i++)
            {
                bool needTemp = false;

                var value = CompileConstant(exprs[i], AmbiguousConstantMode.Pessimistic);
                if (value == null)
                {
                    // could still be a constant if it compiles to a constant operand
                    value = CompileAsOperand(rb, exprs[i], src);

                    if (exprs[i].IsLocalVariableRef())
                    {
                        needTemp = LocalIsLaterModified(exprs, i);
                    }
                    else if (exprs[i].IsGlobalVariableRef())
                    {
                        needTemp = GlobalCouldBeLaterModified(exprs, i);
                    }
                    else if (i == marker)
                    {
                        if (value == rb.Stack)
                        {
                            for (int j = i + 1; j < length; j++)
                            {
                                if (CompileConstant(exprs[j], AmbiguousConstantMode.Pessimistic) == null && !exprs[j].IsVariableRef())
                                {
                                    needTemp = true;
                                    break;
                                }
                            }
                        }
                    }
                    else
                    {
                        needTemp = !(value is INumericOperand);
                    }
                }

                if (!needTemp)
                {
                    values[i] = value;
                }
                else
                {
                    PushInnerLocal(rb, tempAtom, LocalBindingType.CompilerTemporary);
                    values[i] = Locals[tempAtom].LocalBuilder;
                    rb.EmitStore((IVariable)values[i], value);
                    temps[i] = true;
                }
            }

            // evaluate the rest of the arguments right to left, leaving the results
            // in their natural locations.
            for (int i = length - 1; i > marker; i--)
                values[i] = CompileAsOperand(rb, exprs[i], src);

            return new Operands(this, values, temps, tempAtom);
        }

        [System.Diagnostics.Contracts.Pure]
        static bool LocalIsLaterModified([ItemNotNull] [NotNull] ZilObject[] exprs, int localIdx)
        {
            if (!(exprs[localIdx] is ZilForm form))
                throw new ArgumentException("not a FORM");

            if (!(form.First is ZilAtom atom) ||
                atom.StdAtom != StdAtom.LVAL && atom.StdAtom != StdAtom.SET)
            {
                throw new ArgumentException("not an LVAL/SET FORM");
            }

            Debug.Assert(form.Rest != null);

            if (!(form.Rest.First is ZilAtom localAtom))
                throw new ArgumentException("LVAL/SET not followed by an atom");

            for (int i = localIdx + 1; i < exprs.Length; i++)
                if (exprs[i].ModifiesLocal(localAtom))
                    return true;

            return false;
        }

        bool GlobalCouldBeLaterModified([ItemNotNull] [NotNull] ZilObject[] exprs, int localIdx)
        {
            if (!(exprs[localIdx] is ZilForm form))
                throw new ArgumentException("not a FORM");

            if (!(form.First is ZilAtom atom) ||
                (atom.StdAtom != StdAtom.GVAL && atom.StdAtom != StdAtom.SETG))
            {
                throw new ArgumentException("not a GVAL/SETG FORM");
            }

            Debug.Assert(form.Rest != null);

            if (!(form.Rest.First is ZilAtom globalAtom))
                throw new ArgumentException("GVAL/SETG not followed by an atom");

            for (int i = localIdx + 1; i < exprs.Length; i++)
                if (CouldModifyGlobal(exprs[i], globalAtom))
                    return true;

            return false;
        }

        bool CouldModifyGlobal([NotNull] ZilObject expr, [NotNull] ZilAtom globalAtom)
        {
            if (!(expr is ZilListBase list))
                return false;

            if (list is ZilForm && list.First is ZilAtom atom)
            {
                if ((atom.StdAtom == StdAtom.SET || atom.StdAtom == StdAtom.SETG) &&
                    list.Rest?.First == globalAtom)
                {
                    return true;
                }

                if (Routines.ContainsKey(atom))
                {
                    return true;
                }
            }

            return list.Any(zo => CouldModifyGlobal(zo, globalAtom));
        }

        public interface IOperands : IDisposable
        {
            IOperand[] AsArray();
            int Count { get; }
            IOperand this[int index] { get; }
        }

        class Operands : IOperands
        {
            readonly Compilation compilation;
            readonly IOperand[] values;
            readonly bool[] temps;
            readonly ZilAtom tempAtom;

            public Operands([NotNull] Compilation compilation, [NotNull] IOperand[] values, [NotNull] bool[] temps, [NotNull] ZilAtom tempAtom)
            {
                this.compilation = compilation;
                this.values = values;
                this.temps = temps;
                this.tempAtom = tempAtom;
            }

            public void Dispose()
            {
                foreach (bool isTemp in temps)
                    if (isTemp)
                        compilation.PopInnerLocal(tempAtom);
            }

            public int Count
            {
                [System.Diagnostics.Contracts.Pure]
                get => values.Length;
            }

            public IOperand this[int index] => values[index];

            [NotNull]
            public IOperand[] AsArray() => values;
        }
    }
}