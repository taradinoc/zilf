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
using System.Collections.Generic;
using System.Diagnostics.Contracts;
using Zilf.Emit;
using Zilf.Interpreter;
using Zilf.Interpreter.Values;
using Zilf.Language;
using Zilf.Diagnostics;

namespace Zilf.Compiler
{
    class Operands : IDisposable
    {
        readonly CompileCtx cc;
        readonly IOperand[] values;
        readonly bool[] temps;
        readonly ZilAtom tempAtom;

        Operands(CompileCtx cc, IOperand[] values, bool[] temps, ZilAtom tempAtom)
        {
            Contract.Requires(cc != null);
            Contract.Requires(values != null);
            Contract.Requires(temps != null);
            Contract.Requires(tempAtom != null);
            Contract.Ensures(this.cc == cc);
            Contract.Ensures(this.values == values);
            Contract.Ensures(this.temps == temps);
            Contract.Ensures(this.tempAtom == tempAtom);

            this.cc = cc;
            this.values = values;
            this.temps = temps;
            this.tempAtom = tempAtom;
        }

        public static Operands Compile(CompileCtx cc, IRoutineBuilder rb, ISourceLine src, params ZilObject[] exprs)
        {
            Contract.Requires(cc != null);
            Contract.Requires(rb != null);
            Contract.Requires(src != null);
            Contract.Requires(exprs != null);

            int length = exprs.Length;
            IOperand[] values = new IOperand[length];
            bool[] temps = new bool[length];
            var tempAtom = ZilAtom.Parse("?TMP", cc.Context);

            // find the index of the last expr with side effects (or -1)
            int marker = -1;
            for (int i = length - 1; i >= 0; i--)
            {
                if (Expression.HasSideEffects(cc, exprs[i]))
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

                var value = Compiler.CompileConstant(cc, exprs[i]);
                if (value == null)
                {
                    // could still be a constant if it compiles to a constant operand
                    value = Compiler.CompileAsOperand(cc, rb, exprs[i], src);

                    if (IsLocalVariableRef(exprs[i]))
                    {
                        needTemp = LocalIsLaterModified(exprs, i);
                    }
                    else if (IsGlobalVariableRef(exprs[i]))
                    {
                        needTemp = GlobalCouldBeLaterModified(cc, exprs, i);
                    }
                    else if (i == marker)
                    {
                        if (value == rb.Stack)
                        {
                            for (int j = i + 1; j < length; j++)
                            {
                                if (Compiler.CompileConstant(cc, exprs[j]) == null && !IsVariableRef(exprs[j]))
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
                    try
                    {
                        Compiler.PushInnerLocal(cc, rb, tempAtom);
                    }
                    catch (InvalidOperationException)
                    {
                        throw new CompilerError(CompilerMessages.Expression_Needs_Temporary_Variables_Not_Allowed_Here);
                    }
                    values[i] = cc.Locals[tempAtom];
                    rb.EmitStore((IVariable)values[i], value);
                    temps[i] = true;
                }
            }

            // evaluate the rest of the arguments right to left, leaving the results
            // in their natural locations.
            for (int i = length - 1; i > marker; i--)
                values[i] = Compiler.CompileAsOperand(cc, rb, exprs[i], src);

            return new Operands(cc, values, temps, tempAtom);
        }

        static bool LocalIsLaterModified(ZilObject[] exprs, int localIdx)
        {
            Contract.Requires(exprs != null);
            Contract.Requires(exprs.Length > 0);
            Contract.Requires(localIdx >= 0);
            Contract.Requires(localIdx < exprs.Length);

            var form = exprs[localIdx] as ZilForm;
            if (form == null)
                throw new ArgumentException("not a FORM");

            var atom = form.First as ZilAtom;
            if (atom == null || (atom.StdAtom != StdAtom.LVAL && atom.StdAtom != StdAtom.SET))
                throw new ArgumentException("not an LVAL/SET FORM");

            var localAtom = form.Rest.First as ZilAtom;
            if (localAtom == null)
                throw new ArgumentException("LVAL/SET not followed by an atom");

            for (int i = localIdx + 1; i < exprs.Length; i++)
                if (ModifiesLocal(exprs[i], localAtom))
                    return true;

            return false;
        }

        static bool ModifiesLocal(ZilObject expr, ZilAtom localAtom)
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

        static bool GlobalCouldBeLaterModified(CompileCtx cc, ZilObject[] exprs, int localIdx)
        {
            Contract.Requires(cc != null);
            Contract.Requires(exprs != null);
            Contract.Requires(exprs.Length > 0);
            Contract.Requires(localIdx >= 0);
            Contract.Requires(localIdx < exprs.Length);

            var form = exprs[localIdx] as ZilForm;
            if (form == null)
                throw new ArgumentException("not a FORM");

            var atom = form.First as ZilAtom;
            if (atom == null || (atom.StdAtom != StdAtom.GVAL && atom.StdAtom != StdAtom.SETG))
                throw new ArgumentException("not a GVAL/SETG FORM");

            var globalAtom = form.Rest.First as ZilAtom;
            if (globalAtom == null)
                throw new ArgumentException("GVAL/SETG not followed by an atom");

            for (int i = localIdx + 1; i < exprs.Length; i++)
                if (CouldModifyGlobal(cc, exprs[i], globalAtom))
                    return true;

            return false;
        }

        static bool CouldModifyGlobal(CompileCtx cc, ZilObject expr, ZilAtom globalAtom)
        {
            Contract.Requires(cc != null);
            Contract.Requires(expr != null);
            Contract.Requires(globalAtom != null);

            var list = expr as ZilList;
            if (list == null)
                return false;

            if (list is ZilForm)
            {
                var atom = list.First as ZilAtom;
                if (atom != null &&
                    (atom.StdAtom == StdAtom.SET || atom.StdAtom == StdAtom.SETG) &&
                    list.Rest != null && list.Rest.First == globalAtom)
                {
                    return true;
                }
                else if (cc.Routines.ContainsKey(atom))
                {
                    return true;
                }
            }

            foreach (ZilObject zo in list)
                if (CouldModifyGlobal(cc, zo, globalAtom))
                    return true;

            return false;
        }

        static bool IsVariableRef(ZilObject expr)
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

        static bool IsLocalVariableRef(ZilObject expr)
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

        static bool IsGlobalVariableRef(ZilObject expr)
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

        public void Dispose()
        {
            for (int i = 0; i < temps.Length; i++)
                if (temps[i])
                    Compiler.PopInnerLocal(cc, tempAtom);
        }

        public int Count
        {
            [Pure]
            get
            {
                Contract.Ensures(Contract.Result<int>() >= 0);
                return values.Length;
            }
        }

        public IOperand this[int index]
        {
            get
            {
                Contract.Requires(index >= 0);
                Contract.Requires(index < Count);
                Contract.Ensures(Contract.Result<IOperand>() != null);
                return values[index];
            }
        }

        public IOperand[] ToArray()
        {
            Contract.Ensures(Contract.Result<IOperand[]>() != null);
            return values;
        }

        public IEnumerable<IOperand> Skip(int count)
        {
            Contract.Requires(count >= 0);
            //Contract.Ensures(Contract.Result<IEnumerable<IOperand>>() != null);

            for (int i = count; i < values.Length; i++)
                yield return values[i];
        }
    }
}
