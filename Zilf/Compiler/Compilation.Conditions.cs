/* Copyright 2010-2016 Jesse McGrew
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
using System.Diagnostics.CodeAnalysis;
using System.Diagnostics.Contracts;
using System.Linq;
using System.Text;
using Zilf.Compiler.Builtins;
using Zilf.Diagnostics;
using Zilf.Emit;
using Zilf.Interpreter;
using Zilf.Interpreter.Values;
using Zilf.Language;
using Zilf.ZModel;
using Zilf.ZModel.Values;
using Zilf.ZModel.Vocab;

namespace Zilf.Compiler
{
    partial class Compilation
    {
        void CompileCondition(IRoutineBuilder rb, ZilObject expr, ISourceLine src,
            ILabel label, bool polarity)
        {
            Contract.Requires(rb != null);
            Contract.Requires(expr != null);
            Contract.Requires(src != null);
            Contract.Requires(label != null);

            expr = expr.Expand(Context);
            StdAtom type = expr.StdTypeAtom;

            if (type == StdAtom.FALSE)
            {
                if (polarity == false)
                    rb.Branch(label);
                return;
            }
            if (type == StdAtom.ATOM)
            {
                var atom = (ZilAtom)expr;
                if (atom.StdAtom != StdAtom.T && atom.StdAtom != StdAtom.ELSE)
                {
                    // could be a missing , or . before variable name
                    var warning = new CompilerError(src, CompilerMessages.Bare_Atom_0_Treated_As_True_Here, expr);

                    if (Locals.ContainsKey(atom) || Globals.ContainsKey(atom))
                        warning = warning.Combine(new CompilerError(src, CompilerMessages.Did_You_Mean_The_Variable));

                    Context.HandleWarning(warning);
                }

                if (polarity == true)
                    rb.Branch(label);
                return;
            }
            if (type == StdAtom.FIX)
            {
                bool nonzero = ((ZilFix)expr).Value != 0;
                if (polarity == nonzero)
                    rb.Branch(label);
                return;
            }
            if (type != StdAtom.FORM)
            {
                Context.HandleError(new CompilerError(expr.SourceLine ?? src, CompilerMessages.Expressions_Of_This_Type_Cannot_Be_Compiled));
                return;
            }

            // it's a FORM
            var form = expr as ZilForm;
            var head = form.First as ZilAtom;

            if (head == null)
            {
                Context.HandleError(new CompilerError(form, CompilerMessages.FORM_Must_Start_With_An_Atom));
                return;
            }

            // check for standard built-ins
            // prefer the predicate version, then value, value+predicate, void
            // (value+predicate is hard to clean up)
            var zversion = Context.ZEnvironment.ZVersion;
            var argCount = form.Count() - 1;
            if (ZBuiltins.IsBuiltinPredCall(head.Text, zversion, argCount))
            {
                ZBuiltins.CompilePredCall(head.Text, this, rb, form, label, polarity);
                return;
            }
            if (ZBuiltins.IsBuiltinValueCall(head.Text, zversion, argCount))
            {
                var result = ZBuiltins.CompileValueCall(head.Text, this, rb, form, rb.Stack);
                var numericResult = result as INumericOperand;
                if (numericResult != null)
                {
                    if ((numericResult.Value != 0) == polarity)
                        rb.Branch(label);
                }
                else
                {
                    rb.BranchIfZero(result, label, !polarity);
                }
                return;
            }
            if (ZBuiltins.IsBuiltinValuePredCall(head.Text, zversion, argCount))
            {
                if (rb.CleanStack)
                {
                    /* wasting the branch and checking the result with ZERO? is more efficient
                     * than using the branch and having to clean the result off the stack */
                    var noBranch = rb.DefineLabel();
                    ZBuiltins.CompileValuePredCall(head.Text, this, rb, form, rb.Stack, noBranch, true);
                    rb.MarkLabel(noBranch);
                    rb.BranchIfZero(rb.Stack, label, !polarity);
                }
                else
                {
                    ZBuiltins.CompileValuePredCall(head.Text, this, rb, form, rb.Stack, label, polarity);
                }
                return;
            }
            if (ZBuiltins.IsBuiltinVoidCall(head.Text, zversion, argCount))
            {
                ZBuiltins.CompileVoidCall(head.Text, this, rb, form);

                // void calls return true
                if (polarity == true)
                    rb.Branch(label);
                return;
            }

            // special cases
            IOperand op1;
            var args = form.Skip(1).ToArray();

            switch (head.StdAtom)
            {
                case StdAtom.NOT:
                case StdAtom.F_P:
                    polarity = !polarity;
                    goto case StdAtom.T_P;

                case StdAtom.T_P:
                    if (args.Length == 1)
                    {
                        CompileCondition(rb, args[0], form.SourceLine, label, polarity);
                    }
                    else
                    {
                        Context.HandleError(new CompilerError(
                            expr.SourceLine ?? src,
                            CompilerMessages._0_Requires_1_Argument1s,
                            head,
                            new CountableString("exactly 1", false)));
                    }
                    break;

                case StdAtom.OR:
                case StdAtom.AND:
                    CompileBoolean(rb, args, form.SourceLine, head.StdAtom == StdAtom.AND, label, polarity);
                    break;

                default:
                    op1 = CompileAsOperand(rb, form, form.SourceLine);
                    var numericResult = op1 as INumericOperand;
                    if (numericResult != null)
                    {
                        if ((numericResult.Value != 0) == polarity)
                            rb.Branch(label);
                    }
                    else
                    {
                        rb.BranchIfZero(op1, label, !polarity);
                    }
                    break;
            }
        }

        void CompileBoolean(IRoutineBuilder rb, ZilObject[] args, ISourceLine src,
            bool and, ILabel label, bool polarity)
        {
            Contract.Requires(rb != null);
            Contract.Requires(args != null);
            Contract.Requires(src != null);
            Contract.Requires(label != null);

            if (args.Length == 0)
            {
                // <AND> is true, <OR> is false
                if (and == polarity)
                    rb.Branch(label);
            }
            else if (args.Length == 1)
            {
                CompileCondition(rb, args[0], src, label, polarity);
            }
            else if (and == polarity)
            {
                // AND or NOR
                var failure = rb.DefineLabel();
                for (int i = 0; i < args.Length - 1; i++)
                    CompileCondition(rb, args[i], src, failure, !and);

                /* Historical note: ZILCH considered <AND ... <SET X 0>> to be true,
                 * even though <SET X 0> is false. We emulate the bug by compiling the
                 * last element as a statement instead of a condition when it fits
                 * this pattern. */
                ZilObject last = args[args.Length - 1];
                if (and && last.IsSetToZeroForm())
                {
                    Context.HandleWarning(new CompilerError(last.SourceLine, CompilerMessages.Treating_SET_To_0_As_True_Here));
                    CompileStmt(rb, last, false);
                }
                else
                    CompileCondition(rb, last, src, label, and);

                rb.MarkLabel(failure);
            }
            else
            {
                // NAND or OR
                for (int i = 0; i < args.Length - 1; i++)
                    CompileCondition(rb, args[i], src, label, !and);

                /* Emulate the aforementioned ZILCH bug. */
                ZilObject last = args[args.Length - 1];
                if (and && last.IsSetToZeroForm())
                {
                    Context.HandleWarning(new CompilerError(last.SourceLine, CompilerMessages.Treating_SET_To_0_As_True_Here));
                    CompileStmt(rb, last, false);
                }
                else
                    CompileCondition(rb, last, src, label, !and);
            }
        }

        IOperand CompileBoolean(IRoutineBuilder rb, ZilList args, ISourceLine src,
            bool and, bool wantResult, IVariable resultStorage)
        {
            Contract.Requires(rb != null);
            Contract.Requires(args != null);
            Contract.Requires(src != null);
            Contract.Ensures(Contract.Result<IOperand>() != null || !wantResult);

            if (args.IsEmpty)
                return and ? Game.One : Game.Zero;

            if (args.Rest.IsEmpty)
            {
                if (wantResult)
                    return CompileAsOperand(rb, args.First, src, resultStorage);

                if (args.First is ZilForm)
                    return CompileForm(rb, (ZilForm)args.First, wantResult, resultStorage);

                return Game.Zero;
            }

            IOperand result;

            if (wantResult)
            {
                var tempAtom = ZilAtom.Parse("?TMP", Context);
                var lastLabel = rb.DefineLabel();
                IVariable tempVar = null;

                if (resultStorage == null)
                    resultStorage = rb.Stack;

                Contract.Assert(resultStorage != null);

                IVariable nonStackResultStorage = (resultStorage != rb.Stack) ? resultStorage : null;
                Func<IVariable> tempVarProvider = () =>
                {
                    if (tempVar == null)
                    {
                        PushInnerLocal(rb, tempAtom);
                        tempVar = Locals[tempAtom];
                    }
                    return tempVar;
                };

                while (!args.Rest.IsEmpty)
                {
                    var nextLabel = rb.DefineLabel();

                    if (and)
                    {
                        // for AND we only need the result of the last expr; otherwise we only care about truth value
                        CompileCondition(rb, args.First, src, nextLabel, true);
                        rb.EmitStore(resultStorage, Game.Zero);
                    }
                    else
                    {
                        // for OR, if the value is true we want to return it; otherwise discard it and try the next expr
                        result = CompileAsOperandWithBranch(rb, args.First, nonStackResultStorage, nextLabel, false, tempVarProvider);

                        if (result != resultStorage)
                            rb.EmitStore(resultStorage, result);
                    }

                    rb.Branch(lastLabel);
                    rb.MarkLabel(nextLabel);

                    args = args.Rest;
                }

                result = CompileAsOperand(rb, args.First, src, resultStorage);
                if (result != resultStorage)
                    rb.EmitStore(resultStorage, result);

                rb.MarkLabel(lastLabel);

                if (tempVar != null)
                    PopInnerLocal(tempAtom);

                return resultStorage;
            }
            else
            {
                var lastLabel = rb.DefineLabel();

                while (!args.Rest.IsEmpty)
                {
                    var nextLabel = rb.DefineLabel();

                    CompileCondition(rb, args.First, src, nextLabel, and);

                    rb.Branch(lastLabel);
                    rb.MarkLabel(nextLabel);

                    args = args.Rest;
                }

                if (args.First is ZilForm)
                    CompileForm(rb, (ZilForm)args.First, false, null);

                rb.MarkLabel(lastLabel);

                return Game.Zero;
            }
        }

        IOperand CompileCOND(IRoutineBuilder rb, ZilList clauses, ISourceLine src,
            bool wantResult, IVariable resultStorage)
        {
            Contract.Requires(rb != null);
            Contract.Requires(clauses != null);
            Contract.Requires(src != null);
            Contract.Ensures(Contract.Result<IOperand>() != null || !wantResult);

            var nextLabel = rb.DefineLabel();
            var endLabel = rb.DefineLabel();
            bool elsePart = false;

            if (resultStorage == null)
                resultStorage = rb.Stack;

            Contract.Assert(resultStorage != null);

            while (!clauses.IsEmpty)
            {
                var clause = clauses.First as ZilList;
                clauses = clauses.Rest as ZilList;

                if (clause is ZilForm)
                {
                    // a macro call returning a list or false
                    var newClause = clause.Expand(Context);

                    if (newClause is ZilFalse)
                        continue;

                    clause = newClause as ZilList;
                }

                if (clause == null || clause.StdTypeAtom != StdAtom.LIST)
                    throw new CompilerError(CompilerMessages.All_Clauses_In_0_Must_Be_Lists, "COND");

                ZilObject condition = clause.First;

                // if condition is always true (i.e. not a FORM or a FALSE), this is the "else" part
                switch (condition.StdTypeAtom)
                {
                    case StdAtom.FORM:
                        // must be evaluated
                        MarkSequencePoint(rb, condition);
                        CompileCondition(rb, condition, condition.SourceLine, nextLabel, false);
                        break;

                    case StdAtom.FALSE:
                        // never true
                        // TODO: warning message? clause will never be evaluated
                        continue;

                    default:
                        // always true
                        // TODO: warn if not T or ELSE?
                        elsePart = true;
                        break;
                }

                // emit code for clause
                clause = clause.Rest as ZilList;
                var clauseResult = CompileClauseBody(rb, clause, wantResult, resultStorage);
                if (wantResult && clauseResult != resultStorage)
                    rb.EmitStore(resultStorage, clauseResult);

                // jump to end
                if (!clauses.IsEmpty || (wantResult && !elsePart))
                    rb.Branch(endLabel);

                rb.MarkLabel(nextLabel);

                if (elsePart)
                {
                    if (!clauses.IsEmpty)
                    {
                        Context.HandleWarning(new CompilerError(src, CompilerMessages._0_Clauses_After_Else_Part_Will_Never_Be_Evaluated, "COND"));
                    }

                    break;
                }

                nextLabel = rb.DefineLabel();
            }

            if (wantResult && !elsePart)
                rb.EmitStore(resultStorage, Game.Zero);

            rb.MarkLabel(endLabel);
            return wantResult ? resultStorage : null;
        }

        IOperand CompileClauseBody(IRoutineBuilder rb, ZilList clause, bool wantResult, IVariable resultStorage)
        {
            Contract.Requires(rb != null);
            Contract.Requires(clause != null);
            Contract.Requires(resultStorage != null || !wantResult);
            Contract.Ensures(Contract.Result<IOperand>() != null || !Contract.OldValue(wantResult));

            if (clause.IsEmpty)
                return Game.One;

            do
            {
                // only want the result of the last statement (if any)
                bool wantThisResult = wantResult && clause.Rest.IsEmpty;
                var stmt = clause.First;
                if (stmt is ZilAdecl)
                    stmt = ((ZilAdecl)stmt).First;
                var form = stmt as ZilForm;
                IOperand result;
                if (form != null)
                {
                    MarkSequencePoint(rb, form);

                    result = CompileForm(rb, form, wantThisResult,
                        wantThisResult ? resultStorage : null);
                    if (wantThisResult && result != resultStorage)
                        rb.EmitStore(resultStorage, result);
                }
                else if (wantThisResult)
                {
                    result = CompileConstant(stmt);
                    if (result == null)
                        throw new CompilerError(stmt, CompilerMessages.Expressions_Of_This_Type_Cannot_Be_Compiled);

                    rb.EmitStore(resultStorage, result);
                }

                clause = clause.Rest as ZilList;
            } while (!clause.IsEmpty);

            return resultStorage;
        }

        IOperand CompileVERSION_P(IRoutineBuilder rb, ZilList clauses, ISourceLine src,
            bool wantResult, IVariable resultStorage)
        {
            Contract.Requires(rb != null);
            Contract.Requires(clauses != null);
            Contract.Requires(src != null);
            Contract.Ensures(Contract.Result<IOperand>() != null || !wantResult);

            if (resultStorage == null)
                resultStorage = rb.Stack;

            Contract.Assert(resultStorage != null);

            while (!clauses.IsEmpty)
            {
                var clause = clauses.First as ZilList;
                clauses = clauses.Rest;

                if (clause == null || clause.StdTypeAtom != StdAtom.LIST)
                    throw new CompilerError(CompilerMessages.All_Clauses_In_0_Must_Be_Lists, "VERSION?");

                ZilObject condition = clause.First;

                // check version condition
                int condVersion;
                switch (condition.StdTypeAtom)
                {
                    case StdAtom.ATOM:
                        switch (((ZilAtom)condition).StdAtom)
                        {
                            case StdAtom.ZIP:
                                condVersion = 3;
                                break;
                            case StdAtom.EZIP:
                                condVersion = 4;
                                break;
                            case StdAtom.XZIP:
                                condVersion = 5;
                                break;
                            case StdAtom.YZIP:
                                condVersion = 6;
                                break;
                            case StdAtom.ELSE:
                            case StdAtom.T:
                                condVersion = 0;
                                break;
                            default:
                                throw new CompilerError(CompilerMessages.Unrecognized_Atom_In_VERSION_Must_Be_ZIP_EZIP_XZIP_YZIP_ELSET);
                        }
                        break;

                    case StdAtom.FIX:
                        condVersion = ((ZilFix)condition).Value;
                        if (condVersion < 3 || condVersion > 8)
                            throw new CompilerError(CompilerMessages.Version_Number_Out_Of_Range_Must_Be_38);
                        break;

                    default:
                        throw new CompilerError(CompilerMessages.Conditions_In_In_VERSION_Clauses_Must_Be_Atoms);
                }

                // does this clause match?
                if (condVersion == Context.ZEnvironment.ZVersion || condVersion == 0)
                {
                    // emit code for clause
                    clause = clause.Rest;
                    var clauseResult = CompileClauseBody(rb, clause, wantResult, resultStorage);

                    if (condVersion == 0 && !clauses.IsEmpty)
                    {
                        Context.HandleWarning(new CompilerError(src, CompilerMessages._0_Clauses_After_Else_Part_Will_Never_Be_Evaluated, "VERSION?"));
                    }

                    return wantResult ? clauseResult : null;
                }
            }

            // no matching clauses
            if (wantResult)
                rb.EmitStore(resultStorage, Game.Zero);

            return wantResult ? resultStorage : null;
        }

        IOperand CompileIFFLAG(IRoutineBuilder rb, ZilList clauses, ISourceLine src,
            bool wantResult, IVariable resultStorage)
        {
            Contract.Requires(rb != null);
            Contract.Requires(clauses != null);
            Contract.Requires(src != null);
            Contract.Ensures(Contract.Result<IOperand>() != null || !wantResult);

            if (resultStorage == null)
                resultStorage = rb.Stack;

            Contract.Assert(resultStorage != null);

            while (!clauses.IsEmpty)
            {
                var clause = clauses.First as ZilList;
                clauses = clauses.Rest as ZilList;

                if (clause == null || clause.StdTypeAtom != StdAtom.LIST)
                    throw new CompilerError(CompilerMessages.All_Clauses_In_0_Must_Be_Lists, "IFFLAG");

                ZilAtom atom;
                ZilString str;
                ZilForm form;
                ZilObject value;
                bool match, isElse = false;
                if (((atom = clause.First as ZilAtom) != null &&
                     (value = Context.GetCompilationFlagValue(atom)) != null) ||
                    ((str = clause.First as ZilString) != null &&
                     (value = Context.GetCompilationFlagValue(str.Text)) != null))
                {
                    // name of a defined compilation flag
                    match = value.IsTrue;
                }
                else if ((form = clause.First as ZilForm) != null)
                {
                    form = Subrs.SubstituteIfflagForm(Context, form);
                    match = form.Eval(Context).IsTrue;
                }
                else
                {
                    match = isElse = true;
                }

                // does this clause match?
                if (match)
                {
                    // emit code for clause
                    clause = clause.Rest;
                    var clauseResult = CompileClauseBody(rb, clause, wantResult, resultStorage);

                    if (isElse && !clauses.IsEmpty)
                    {
                        Context.HandleWarning(new CompilerError(src, CompilerMessages._0_Clauses_After_Else_Part_Will_Never_Be_Evaluated, "IFFLAG"));
                    }

                    return wantResult ? clauseResult : null;
                }
            }

            // no matching clauses
            if (wantResult)
                rb.EmitStore(resultStorage, Game.Zero);

            return wantResult ? resultStorage : null;
        }
    }
}
