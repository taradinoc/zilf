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
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Reflection.Emit;
using Zilf.Common;
using Zilf.Diagnostics;
using Zilf.Emit;
using Zilf.Interpreter;
using Zilf.Interpreter.Values;
using Zilf.Language;
using Zilf.ZModel.Values;
using JetBrains.Annotations;

namespace Zilf.Compiler
{
    partial class Compilation
    {
        [NotNull]
        static ZilRoutine MaybeRewriteRoutine([NotNull] Context ctx, [NotNull] ZilRoutine origRoutine)
        {
            const string SExpectedResultType = "a list (with an arg spec and body) or FALSE";

            Debug.Assert(origRoutine.Name != null);

            var result = ctx.RunHook(
                "REWRITE-ROUTINE",
                origRoutine.Name,
                origRoutine.ArgSpec.ToZilList(),
                new ZilList(origRoutine.Body));

            switch (result)
            {
                case ZilList list when list.GetLength(1) == null && list.First is ZilList args:
                    Debug.Assert(list.Rest != null);
                    return new ZilRoutine(origRoutine.Name, null, args, list.Rest, origRoutine.Flags);

                case ZilFalse _:
                case null:
                    return origRoutine;

                default:
                    throw new InterpreterError(InterpreterMessages._0_1_Must_Return_2, "routine rewriter", SExpectedResultType);
            }
        }

        void ClearLocalsAndBlocks()
        {
            Locals.Clear();
            AllLocalBindingRecords.Clear();
            TempLocalNames.Clear();
            SpareLocals.Clear();
            OuterLocals.Clear();
        }

        void BuildRoutine([NotNull] ZilRoutine routine, [NotNull] IRoutineBuilder rb, bool entryPoint, bool traceRoutines)
        {
            // give the user a chance to rewrite the routine
            routine = MaybeRewriteRoutine(Context, routine);

            // set up arguments and locals
            ClearLocalsAndBlocks();

            if (Context.TraceRoutines)
                rb.EmitPrint("[" + routine.Name, false);

            DefineLocalsFromArgSpec();

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
            foreach (var stmt in routine.Body)
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
            WarnAboutUnusedLocals();
            ClearLocalsAndBlocks();

            // helpers
            void DefineLocalsFromArgSpec()
            {
                foreach (var arg in routine.ArgSpec)
                {
                    var originalArgName = arg.Atom;
                    var uniqueArgName = MakeUniqueVariableName(originalArgName);

                    if (uniqueArgName != originalArgName)
                    {
                        /* When a parameter has to be renamed because of a conflict, use TempLocalNames
                         * to reserve the new name so we don't collide with it later. For example:
                         * 
                         *   <GLOBAL FOO <>>
                         *   <ROUTINE BLAH (FOO)
                         *     <PROG ((FOO)) ...>>
                         * 
                         * We rename the local variable to FOO?1 to avoid shadowing the global.
                         * Now the temporary variable bound by the PROG has to be FOO?2.
                         * ZIL code only sees the name FOO: the local is shadowed inside the PROG,
                         * and the global can always be accessed with SETG and GVAL.
                         */
                        TempLocalNames.Add(uniqueArgName);
                    }

                    var lb = MakeLocalBuilder(arg, uniqueArgName.Text);

                    if (traceRoutines && arg.Type == ArgItem.ArgType.Required)
                    {
                        // TODO: print OPT parameters when tracing routine execution too
                        rb.EmitPrint(" " + originalArgName + "=", false);
                        rb.EmitPrint(PrintOp.Number, lb);
                    }

                    var lbr = new LocalBindingRecord(arg.Type.ToLocalBindingType(), routine.SourceLine, originalArgName.Text, lb);
                    Locals.Add(originalArgName, lbr);
                    AllLocalBindingRecords.Add(lbr);

                    SetOrEmitDefaultValue(lb, arg);
                }
            }

            ILocalBuilder MakeLocalBuilder(ArgItem arg, string uniqueArgName)
            {
                ILocalBuilder lb;

                switch (arg.Type)
                {
                    case ArgItem.ArgType.Required:
                        try
                        {
                            lb = rb.DefineRequiredParameter(uniqueArgName);
                        }
                        catch (InvalidOperationException)
                        {
                            throw new CompilerError(
                                CompilerMessages.Expression_Needs_Temporary_Variables_Not_Allowed_Here);
                        }
                        break;

                    case ArgItem.ArgType.Optional:
                        lb = rb.DefineOptionalParameter(uniqueArgName);
                        break;

                    case ArgItem.ArgType.Auxiliary:
                        lb = rb.DefineLocal(uniqueArgName);
                        break;

                    default:
                        throw UnhandledCaseException.FromEnum(arg.Type);
                }

                return lb;
            }

            void SetOrEmitDefaultValue(ILocalBuilder lb, ArgItem arg)
            {
                if (arg.DefaultValue == null)
                    return;

                Debug.Assert(arg.Type == ArgItem.ArgType.Optional || arg.Type == ArgItem.ArgType.Auxiliary);

                // setting any default value counts as a write
                MarkVariableAsWritten(lb);

                lb.DefaultValue = CompileConstant(arg.DefaultValue);
                if (lb.DefaultValue != null)
                    return;

                ILabel nextLabel = null;

                // ReSharper disable once SwitchStatementMissingSomeCases
                switch (arg.Type)
                {
                    case ArgItem.ArgType.Optional when !rb.HasArgCount:
                        // not a constant
                        throw new CompilerError(routine.SourceLine,
                            CompilerMessages.Optional_Args_With_Nonconstant_Defaults_Not_Supported_For_This_Target);

                    case ArgItem.ArgType.Optional:
                        nextLabel = rb.DefineLabel();
                        rb.Branch(Condition.ArgProvided, lb, null, nextLabel, true);
                        goto default;

                    default:
                        var val = CompileAsOperand(rb, arg.DefaultValue, routine.SourceLine, lb);
                        if (val != lb)
                            rb.EmitStore(lb, val);
                        break;
                }

                if (nextLabel != null)
                    rb.MarkLabel(nextLabel);
            }

            void WarnAboutUnusedLocals()
            {
                foreach (var lbr in AllLocalBindingRecords)
                {
                    if (lbr.IsEverRead || lbr.IsEverWritten)
                        continue;

                    if (lbr.Type == LocalBindingType.CompilerTemporary)
                        continue;

                    //XXX not sure about this
                    if (lbr.Type == LocalBindingType.RoutineRequired)
                        continue;

                    var warning = new CompilerError(
                        lbr.Definition,
                        CompilerMessages.Local_Variable_0_Is_Never_Used,
                        lbr.BoundName);

                    Context.HandleError(warning);
                }
            }
        }

        // TODO: replace CompileStmt with CompileForm and (in loops) CompileClauseBody
        void CompileStmt([NotNull] IRoutineBuilder rb, [NotNull] ZilObject stmt, bool wantResult)
        {
            stmt = stmt.Unwrap(Context);

            switch (stmt)
            {
                case ZilForm form:
                    MarkSequencePoint(rb, form);

                    var result = CompileForm(rb, form, wantResult, null);

                    if (wantResult)
                        rb.Return(result);
                    break;

                case ZilList _:
                    throw new CompilerError(stmt, CompilerMessages.Expressions_Of_This_Type_Cannot_Be_Compiled)
                        .Combine(new CompilerError(CompilerMessages.Misplaced_Bracket_In_COND_Or_Loop));

                default:
                    if (wantResult)
                    {
                        var value = CompileConstant(stmt);

                        if (value == null)
                        {
                            // TODO: show "expressions of this type cannot be compiled" warning even if wantResult is false?
                            throw new CompilerError(stmt, CompilerMessages.Expressions_Of_This_Type_Cannot_Be_Compiled);
                        }

                        rb.Return(value);
                    }
                    break;
            }
        }

        void MarkSequencePoint([NotNull] IRoutineBuilder rb, [NotNull] IProvideSourceLine node)
        {
            if (!WantDebugInfo || !(node.SourceLine is FileSourceLine fileSourceLine))
                return;

            Debug.Assert(Game.DebugFile != null);
            Game.DebugFile.MarkSequencePoint(rb,
                new DebugLineRef(fileSourceLine.FileName, fileSourceLine.Line, 1));
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="rb"></param>
        /// <param name="atom"></param>
        /// <param name="reason"></param>
        /// <param name="src"></param>
        /// <returns></returns>
        /// <exception cref="CompilerError">Local variables are not allowed here.</exception>
        [NotNull]
        public ILocalBuilder PushInnerLocal([NotNull] IRoutineBuilder rb, [NotNull] ZilAtom atom,
            LocalBindingType reason, ISourceLine src)
        {
            if (Locals.TryGetValue(atom, out var prev))
            {
                // save the old binding
                if (OuterLocals.TryGetValue(atom, out var stk) == false)
                {
                    stk = new Stack<LocalBindingRecord>();
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
                var tempName = MakeUniqueVariableName(atom);

                try
                {
                    result = rb.DefineLocal(tempName.Text);
                }
                catch (InvalidOperationException)
                {
                    throw new CompilerError(CompilerMessages.Expression_Needs_Temporary_Variables_Not_Allowed_Here);
                }

                TempLocalNames.Add(tempName);
            }

            var lbr = new LocalBindingRecord(reason, src, atom.Text, result);
            Locals[atom] = lbr;
            AllLocalBindingRecords.Add(lbr);
            return result;
        }

        [NotNull]
        ZilAtom MakeUniqueVariableName([NotNull] ZilAtom atom)
        {
            if (!VariableNameInUse(atom))
                return atom;

            ZilAtom newAtom;
            int num = 1;
            do
            {
                var name = atom.Text + "?" + num;
                num++;
                newAtom = ZilAtom.Parse(name, Context);
            } while (VariableNameInUse(newAtom));

            return newAtom;
        }

        bool VariableNameInUse([NotNull] ZilAtom atom)
        {
            return Locals.ContainsKey(atom) || TempLocalNames.Contains(atom) ||
                   Globals.ContainsKey(atom) || SoftGlobals.ContainsKey(atom) ||
                   Constants.ContainsKey(atom) || Objects.ContainsKey(atom) || Routines.ContainsKey(atom);
        }

        public void PopInnerLocal([NotNull] ZilAtom atom)
        {
            SpareLocals.Push(Locals[atom].LocalBuilder);

            if (OuterLocals.TryGetValue(atom, out var stk))
            {
                Locals[atom] = stk.Pop();
                if (stk.Count == 0)
                    OuterLocals.Remove(atom);
            }
            else
                Locals.Remove(atom);
        }

        void FindAndMarkVariable(IVariable var, Action<LocalBindingRecord> markAction)
        {
            if (!(var is ILocalBuilder))
                return;

            var lbr = Locals.Values.First(r => r.LocalBuilder == var);
            markAction(lbr);
        }

        public void MarkVariableAsRead([NotNull] LocalBindingRecord lbr) =>
            lbr.IsEverRead = true;

        public void MarkVariableAsRead(IVariable var) =>
            FindAndMarkVariable(var, MarkVariableAsRead);

        public void MarkVariableAsWritten([NotNull] LocalBindingRecord lbr) =>
            lbr.IsEverWritten = true;

        public void MarkVariableAsWritten(IVariable var) =>
            FindAndMarkVariable(var, MarkVariableAsWritten);

        public void MarkVariableAsReadAndWritten([NotNull] LocalBindingRecord lbr) =>
            lbr.IsEverRead = lbr.IsEverWritten = true;

        public void MarkVariableAsReadAndWritten(IVariable var) =>
            FindAndMarkVariable(var, MarkVariableAsReadAndWritten);
    }
}
