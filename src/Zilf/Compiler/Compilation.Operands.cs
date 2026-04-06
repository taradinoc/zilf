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
using System.Diagnostics;
using System.Linq;
using Zilf.Emit;
using Zilf.Interpreter.Values;
using Zilf.Language;

namespace Zilf.Compiler
{
    partial class Compilation
    {
        /// <exception cref="CompilerError">Local variables are not allowed here, or an error occurred while compiling a subexpression.</exception>
        public IOperands CompileOperands(IRoutineBuilder rb, ISourceLine src, params ZilObject[] exprs)
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
                        needTemp = value is not INumericOperand;
                    }
                }

                if (!needTemp)
                {
                    values[i] = value;
                }
                else
                {
                    PushInnerLocal(rb, tempAtom, LocalBindingType.CompilerTemporary, src);
                    values[i] = Locals[tempAtom].LocalBuilder;
                    rb.EmitStore((IVariable)values[i], value);
                    temps[i] = true;
                }
            }

            // evaluate the rest of the arguments right to left, leaving the results
            // in their natural locations.
            for (int i = length - 1; i > marker; i--)
            {
                values[i] = CompileAsOperand(rb, exprs[i], src);

                // On stack-based call targets (e.g. Cornerstone), all call arguments
                // are pushed onto the evaluation stack. If a complex subexpression
                // leaves its result on the stack and there are preceding operands
                // that will be pushed later (by EmitRuntimeCall), the left-to-right
                // push would place them out of order. Save the stack result to a
                // temp variable to prevent this.
                if (rb.UsesStackBasedCalls && i > 0 && values[i] == rb.Stack)
                {
                    try
                    {
                        PushInnerLocal(rb, tempAtom, LocalBindingType.CompilerTemporary, src);
                        values[i] = Locals[tempAtom].LocalBuilder;
                        rb.EmitStore((IVariable)values[i], rb.Stack);
                        temps[i] = true;
                    }
                    catch (CompilerError)
                    {
                        // Entry point routines cannot allocate temp locals.
                        // Leave the value on the stack and accept potential
                        // misordering — the entry point rarely uses complex
                        // table operations.
                    }
                }
            }

            return new Operands(this, values, temps, tempAtom);
        }

        /// <summary>
        /// Compiles a series of expressions, leaving all results on the stack in such a way that they can be popped
        /// left-to-right.
        /// </summary>
        /// <remarks>
        /// Evaluation is performed in a way that preserves left-to-right ordering up to and including the last expression
        /// that is detected to have side effects. The final stack order is such that the first expression's value is on
        /// top of the stack.
        /// </remarks>
        /// <exception cref="CompilerError">Local variables are not allowed here, or an error occurred while compiling a subexpression.</exception>
        public IOperands CompileOperandsToStack(IRoutineBuilder rb, ISourceLine src, params ZilObject[] exprs)
        {
            int length = exprs.Length;
            var values = new IOperand[length];
            var temps = new bool[length];
            var tempAtom = ZilAtom.Parse("?TMP", Context);

            for (int i = 0; i < length; i++)
                values[i] = rb.Stack;

            if (length == 0)
                return new Operands(this, values, temps, tempAtom);

            // Find the index of the last expr with side effects (or -1).
            int marker = -1;
            for (int i = length - 1; i >= 0; i--)
            {
                if (HasSideEffects(exprs[i]))
                {
                    marker = i;
                    break;
                }
            }

            // Values that must be preserved after being evaluated left-to-right.
            var saved = new IOperand[marker + 1];

            // Evaluate arguments up to and including the marker, left to right.
            // Since we ultimately need to push these values in reverse order, we preserve each computed value
            // somewhere safe (constant, unmodified variable, or temporary local).
            for (int i = 0; i <= marker; i++)
            {
                var value = CompileConstant(exprs[i], AmbiguousConstantMode.Pessimistic);
                if (value == null)
                    value = CompileAsOperand(rb, exprs[i], src);

                bool needTemp = value == rb.Stack;

                if (!needTemp)
                {
                    if (exprs[i].IsLocalVariableRef())
                    {
                        needTemp = LocalIsLaterModified(exprs, i);
                    }
                    else if (exprs[i].IsGlobalVariableRef())
                    {
                        needTemp = GlobalCouldBeLaterModified(exprs, i);
                    }
                }

                if (!needTemp)
                {
                    saved[i] = value;
                }
                else
                {
                    PushInnerLocal(rb, tempAtom, LocalBindingType.CompilerTemporary, src);
                    var tempLocal = Locals[tempAtom].LocalBuilder;
                    rb.EmitStore(tempLocal, value);
                    temps[i] = true;
                    saved[i] = tempLocal;
                }
            }

            // Evaluate the remaining arguments (which have no detected side effects) right-to-left, pushing each result
            // as we go. This produces the desired push order for the tail of the list.
            for (int i = length - 1; i > marker; i--)
            {
                var value = CompileConstant(exprs[i], AmbiguousConstantMode.Pessimistic);
                if (value == null)
                    value = CompileAsOperand(rb, exprs[i], src);

                if (value != rb.Stack)
                    rb.EmitStore(rb.Stack, value);
            }

            // Push the preserved values in reverse, so expr[0] ends up on top of the stack.
            for (int i = marker; i >= 0; i--)
                rb.EmitStore(rb.Stack, saved[i]);

            return new Operands(this, values, temps, tempAtom);
        }

        [System.Diagnostics.Contracts.Pure]
        static bool LocalIsLaterModified(ZilObject[] exprs, int localIdx)
        {
            if (exprs[localIdx] is not ZilForm form)
                throw new ArgumentException("not a FORM");

            if (form.First is not ZilAtom atom ||
                atom.StdAtom != StdAtom.LVAL && atom.StdAtom != StdAtom.SET)
            {
                throw new ArgumentException("not an LVAL/SET FORM");
            }

            Debug.Assert(form.Rest != null);

            if (form.Rest.First is not ZilAtom localAtom)
                throw new ArgumentException("LVAL/SET not followed by an atom");

            for (int i = localIdx + 1; i < exprs.Length; i++)
                if (exprs[i].ModifiesLocal(localAtom))
                    return true;

            return false;
        }

        bool GlobalCouldBeLaterModified(ZilObject[] exprs, int localIdx)
        {
            if (exprs[localIdx] is not ZilForm form)
                throw new ArgumentException("not a FORM");

            if (form.First is not ZilAtom atom ||
                (atom.StdAtom != StdAtom.GVAL && atom.StdAtom != StdAtom.SETG))
            {
                throw new ArgumentException("not a GVAL/SETG FORM");
            }

            Debug.Assert(form.Rest != null);

            if (form.Rest.First is not ZilAtom globalAtom)
                throw new ArgumentException("GVAL/SETG not followed by an atom");

            for (int i = localIdx + 1; i < exprs.Length; i++)
                if (CouldModifyGlobal(exprs[i], globalAtom))
                    return true;

            return false;
        }

        bool CouldModifyGlobal(ZilObject expr, ZilAtom globalAtom)
        {
            if (expr is not ZilListBase list)
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

        sealed class Operands : IOperands
        {
            readonly Compilation compilation;
            readonly IOperand[] values;
            readonly bool[] temps;
            readonly ZilAtom tempAtom;

            public Operands(Compilation compilation, IOperand[] values, bool[] temps, ZilAtom tempAtom)
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

            public int Count => values.Length;

            public IOperand this[int index] => values[index];

            public IOperand[] AsArray() => values;
        }
    }
}