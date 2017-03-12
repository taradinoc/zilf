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
using Zilf.Common;
using Zilf.Diagnostics;
using Zilf.Emit;
using Zilf.Interpreter;
using Zilf.Interpreter.Values;
using Zilf.Language;
using Zilf.ZModel.Values;

namespace Zilf.Compiler
{
    partial class Compilation
    {
        static ZilRoutine MaybeRewriteRoutine(Context ctx, ZilRoutine origRoutine)
        {
            const string SExpectedResultType = "a list (with an arg spec and body) or FALSE";

            var result = ctx.RunHook(
                "REWRITE-ROUTINE",
                origRoutine.Name,
                origRoutine.ArgSpec.ToZilList(),
                new ZilList(origRoutine.Body));

            if (result != null)
            {
                switch (result.StdTypeAtom)
                {
                    case StdAtom.LIST:
                        var list = (ZilList)result;
                        ZilList args, body;
                        if (((IStructure)list).GetLength(1) <= 1 || (args = list.First as ZilList) == null ||
                            args.StdTypeAtom != StdAtom.LIST)
                        {
                            throw new InterpreterError(InterpreterMessages._0_1_Must_Return_2, "routine rewriter", SExpectedResultType);
                        }
                        body = list.Rest;
                        return new ZilRoutine(origRoutine.Name, null, args, body, origRoutine.Flags);

                    case StdAtom.FALSE:
                        break;

                    default:
                        throw new InterpreterError(InterpreterMessages._0_1_Must_Return_2, "routine rewriter", SExpectedResultType);
                }
            }

            return origRoutine;
        }

        void BuildRoutine(ZilRoutine routine, IGameBuilder gb,
            IRoutineBuilder rb, bool entryPoint)
        {
            Contract.Requires(routine != null);
            Contract.Requires(gb != null);
            Contract.Requires(rb != null);

            // give the user a chance to rewrite the routine
            routine = MaybeRewriteRoutine(Context, routine);

            // set up arguments and locals
            Locals.Clear();
            TempLocalNames.Clear();
            SpareLocals.Clear();
            OuterLocals.Clear();

            if (Context.TraceRoutines)
                rb.EmitPrint("[" + routine.Name, false);

            foreach (ArgItem arg in routine.ArgSpec)
            {
                ILocalBuilder lb;

                switch (arg.Type)
                {
                    case ArgItem.ArgType.Required:
                        try
                        {
                            lb = rb.DefineRequiredParameter(arg.Atom.ToString());
                        }
                        catch (InvalidOperationException)
                        {
                            throw new CompilerError(CompilerMessages.Expression_Needs_Temporary_Variables_Not_Allowed_Here);
                        }
                        if (Context.TraceRoutines)
                        {
                            rb.EmitPrint(" " + arg.Atom + "=", false);
                            rb.EmitPrint(PrintOp.Number, lb);
                        }
                        break;
                    case ArgItem.ArgType.Optional:
                        lb = rb.DefineOptionalParameter(arg.Atom.ToString());
                        break;
                    case ArgItem.ArgType.Auxiliary:
                        lb = rb.DefineLocal(arg.Atom.ToString());
                        break;
                    default:
                        throw UnhandledCaseException.FromEnum(arg.Type);
                }

                Locals.Add(arg.Atom, lb);

                if (arg.DefaultValue != null)
                {
                    lb.DefaultValue = CompileConstant(arg.DefaultValue);
                    if (lb.DefaultValue == null)
                    {
                        // not a constant
                        if (arg.Type == ArgItem.ArgType.Optional)
                        {
                            if (!rb.HasArgCount)
                                throw new CompilerError(routine.SourceLine, CompilerMessages.Optional_Args_With_Nonconstant_Defaults_Not_Supported_For_This_Target);

                            var nextLabel = rb.DefineLabel();
                            rb.Branch(Condition.ArgProvided, lb, null, nextLabel, true);
                            var val = CompileAsOperand(rb, arg.DefaultValue, routine.SourceLine, lb);
                            if (val != lb)
                                rb.EmitStore(lb, val);
                            rb.MarkLabel(nextLabel);
                        }
                        else
                        {
                            var val = CompileAsOperand(rb, arg.DefaultValue, routine.SourceLine, lb);
                            if (val != lb)
                                rb.EmitStore(lb, val);
                        }
                    }
                }
            }

            if (Context.TraceRoutines)
                rb.EmitPrint("]\n", false);

            // define a block for the routine
            Blocks.Clear();
            Blocks.Push(new Block
            {
                Name = routine.ActivationAtom,
                AgainLabel = rb.RoutineStart,
                ReturnLabel = null,
                Flags = BlockFlags.None
            });

            // generate code for routine body
            int i = 1;
            foreach (ZilObject stmt in routine.Body)
            {
                // only want the result of the last statement
                // and we never want results in the entry routine, since it can't return
                CompileStmt(rb, stmt, !entryPoint && i == routine.BodyLength);
                i++;
            }

            // the entry point has to quit instead of returning
            if (entryPoint)
                rb.EmitQuit();

            // clean up
            Locals.Clear();
            SpareLocals.Clear();
            OuterLocals.Clear();

            Contract.Assume(Blocks.Count == 1);
            Blocks.Pop();
        }

        void CompileStmt(IRoutineBuilder rb, ZilObject stmt, bool wantResult)
        {
            var form = stmt as ZilForm;
            if (form == null)
            {
                if (wantResult)
                {
                    var value = CompileConstant(stmt);
                    if (value == null)
                    {
                        var error = new CompilerError(stmt, CompilerMessages.Expressions_Of_This_Type_Cannot_Be_Compiled);
                        if (stmt.StdTypeAtom == StdAtom.LIST)
                            error = error.Combine(new CompilerError(CompilerMessages.Misplaced_Bracket_In_COND));
                        throw error;
                    }

                    rb.Return(value);
                }
                //else
                //{
                // TODO: warning message when skipping non-forms inside a routine?
                //}
            }
            else
            {
                MarkSequencePoint(rb, form);

                var result = CompileForm(rb, form, wantResult, null);

                if (wantResult)
                    rb.Return(result);
            }
        }

        void MarkSequencePoint(IRoutineBuilder rb, ZilObject node)
        {
            Contract.Requires(rb != null);
            Contract.Requires(node != null);

            if (WantDebugInfo)
            {
                var fileSourceLine = node.SourceLine as FileSourceLine;
                if (fileSourceLine != null)
                {
                    Game.DebugFile.MarkSequencePoint(rb,
                        new DebugLineRef(fileSourceLine.FileName, fileSourceLine.Line, 1));
                }
            }
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="rb"></param>
        /// <param name="atom"></param>
        /// 
        /// <returns></returns>
        /// <exception cref="CompilerError">Local variables are not allowed here.</exception>
        public ILocalBuilder PushInnerLocal(IRoutineBuilder rb, ZilAtom atom)
        {
            Contract.Requires(rb != null);
            Contract.Requires(atom != null);

            string name = atom.Text;

            ILocalBuilder prev;
            if (Locals.TryGetValue(atom, out prev))
            {
                // save the old binding
                Stack<ILocalBuilder> stk;
                if (OuterLocals.TryGetValue(atom, out stk) == false)
                {
                    stk = new Stack<ILocalBuilder>();
                    OuterLocals.Add(atom, stk);
                }
                stk.Push(prev);
            }

            ILocalBuilder result;
            if (SpareLocals.Count > 0)
            {
                // reuse a spare variable
                result = SpareLocals.Pop();
            }
            else
            {
                // allocate a new variable with a unique name
                ZilAtom tempName;

                if (Locals.ContainsKey(atom) || TempLocalNames.Contains(atom))
                {
                    ZilAtom newAtom;
                    int num = 1;
                    do
                    {
                        name = atom.Text + "?" + num;
                        num++;
                        newAtom = ZilAtom.Parse(name, Context);
                    } while (Locals.ContainsKey(newAtom) || TempLocalNames.Contains(newAtom));

                    tempName = newAtom;
                }
                else
                {
                    tempName = atom;
                }

                try
                {
                    result = rb.DefineLocal(name);
                }
                catch (InvalidOperationException)
                {
                    throw new CompilerError(CompilerMessages.Expression_Needs_Temporary_Variables_Not_Allowed_Here);
                }

                TempLocalNames.Add(tempName);
            }

            Locals[atom] = result;
            return result;
        }

        public void PopInnerLocal(ZilAtom atom)
        {
            Contract.Requires(atom != null);

            SpareLocals.Push(Locals[atom]);

            Stack<ILocalBuilder> stk;
            if (OuterLocals.TryGetValue(atom, out stk))
            {
                Locals[atom] = stk.Pop();
                if (stk.Count == 0)
                    OuterLocals.Remove(atom);
            }
            else
                Locals.Remove(atom);
        }
    }
}
