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
using System.Diagnostics.Contracts;
using System.Linq;
using Zilf.Compiler.Builtins;
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
        /// <summary>
        /// Compiles a FORM.
        /// </summary>
        /// <param name="rb">The current routine.</param>
        /// <param name="form">The FORM to compile.</param>
        /// <param name="wantResult">true if a result must be produced;
        /// false if a result must not be produced.</param>
        /// <param name="resultStorage">A suggested (but not mandatory) storage location
        /// for the result, or null.</param>
        /// <returns><paramref name="resultStorage"/> if the suggested location was used
        /// for the result, or another operand if the suggested location was not used,
        /// or null if a result was not produced.</returns>
        internal IOperand CompileForm(IRoutineBuilder rb, ZilForm form, bool wantResult,
            IVariable resultStorage)
        {
            // TODO: split up this method

            Contract.Requires(rb != null);
            Contract.Requires(form != null);
            Contract.Ensures(Contract.Result<IOperand>() != null || !wantResult);

            IOperand operand;

            using (DiagnosticContext.Push(form.SourceLine))
            {
                // expand macro invocations
                ZilObject expanded;
                try
                {
                    using (DiagnosticContext.Push(form.SourceLine))
                    {
                        expanded = form.Expand(Context);
                    }
                }
                catch (InterpreterError ex)
                {
                    Context.HandleError(ex);
                    return Game.Zero;
                }

                switch (expanded)
                {
                    case ZilForm expandedForm:
                        form = expandedForm;
                        break;

                    case IMayExpandAfterEvaluation expAfter when (expAfter.ShouldExpandAfterEvaluation):
                        var src = form.SourceLine;
                        var reexpanded = expAfter.ExpandAfterEvaluation(Context, Context.LocalEnvironment);
                        form = (ZilForm)Program.Parse(Context, src, "<BIND () {0:SPLICE}>", new ZilList(reexpanded)).Single();
                        break;

                    default:
                        if (wantResult)
                            return CompileAsOperand(rb, expanded, form.SourceLine, resultStorage);
                        return null;
                }

                if (!(form.First is ZilAtom head))
                {
                    Context.HandleError(new CompilerError(form, CompilerMessages.FORM_Must_Start_With_An_Atom));
                    return wantResult ? Game.Zero : null;
                }

                // built-in statements handled by ZBuiltins
                var zversion = Context.ZEnvironment.ZVersion;
                var argCount = form.Rest.Count();

                if (wantResult)
                {
                    // prefer the value version, then value+predicate, predicate, void
                    if (ZBuiltins.IsBuiltinValueCall(head.Text, zversion, argCount))
                    {
                        return ZBuiltins.CompileValueCall(head.Text, this, rb, form, resultStorage);
                    }
                    if (ZBuiltins.IsBuiltinValuePredCall(head.Text, zversion, argCount))
                    {
                        var label1 = rb.DefineLabel();
                        resultStorage = resultStorage ?? rb.Stack;
                        ZBuiltins.CompileValuePredCall(head.Text, this, rb, form, resultStorage, label1, true);
                        rb.MarkLabel(label1);
                        return resultStorage;
                    }
                    if (ZBuiltins.IsBuiltinPredCall(head.Text, zversion, argCount))
                    {
                        var label1 = rb.DefineLabel();
                        var label2 = rb.DefineLabel();
                        resultStorage = resultStorage ?? rb.Stack;
                        ZBuiltins.CompilePredCall(head.Text, this, rb, form, label1, true);
                        rb.EmitStore(resultStorage, Game.Zero);
                        rb.Branch(label2);
                        rb.MarkLabel(label1);
                        rb.EmitStore(resultStorage, Game.One);
                        rb.MarkLabel(label2);
                        return resultStorage;
                    }
                    if (ZBuiltins.IsBuiltinVoidCall(head.Text, zversion, argCount))
                    {
                        ZBuiltins.CompileVoidCall(head.Text, this, rb, form);
                        return Game.One;
                    }
                }
                else
                {
                    // prefer the void version, then predicate, value, value+predicate
                    // (predicate saves a cleanup instruction)
                    if (ZBuiltins.IsBuiltinVoidCall(head.Text, zversion, argCount))
                    {
                        ZBuiltins.CompileVoidCall(head.Text, this, rb, form);
                        return null;
                    }
                    if (ZBuiltins.IsBuiltinPredCall(head.Text, zversion, argCount))
                    {
                        var dummy = rb.DefineLabel();
                        ZBuiltins.CompilePredCall(head.Text, this, rb, form, dummy, true);
                        rb.MarkLabel(dummy);
                        return null;
                    }
                    if (ZBuiltins.IsBuiltinValueCall(head.Text, zversion, argCount))
                    {
                        if (ZBuiltins.CompileValueCall(head.Text, this, rb, form, null) == rb.Stack)
                            rb.EmitPopStack();
                        return null;
                    }
                    if (ZBuiltins.IsBuiltinValuePredCall(head.Text, zversion, argCount))
                    {
                        var label1 = rb.DefineLabel();
                        ZBuiltins.CompileValuePredCall(head.Text, this, rb, form, rb.Stack, label1, true);
                        rb.MarkLabel(label1);
                        rb.EmitPopStack();
                        return null;
                    }
                }

                // built-in statements handled specially
                ZilAtom atom;
                IGlobalBuilder global;
                ILocalBuilder local;
                IObjectBuilder objbld;
                IRoutineBuilder routine;
                IVariable result;
                switch (head.StdAtom)
                {
                    case StdAtom.GVAL:
                        atom = form.Rest.First as ZilAtom;
                        if (atom == null)
                        {
                            Context.HandleError(new CompilerError(form, CompilerMessages.Expected_An_Atom_After_0, "GVAL"));
                            return wantResult ? Game.Zero : null;
                        }

                        // constant, global, object, or routine
                        if (Constants.TryGetValue(atom, out operand))
                            return operand;
                        if (Globals.TryGetValue(atom, out global))
                            return global;
                        if (Objects.TryGetValue(atom, out objbld))
                            return objbld;
                        if (Routines.TryGetValue(atom, out routine))
                            return routine;

                        // soft global
                        if (SoftGlobals.TryGetValue(atom, out var softGlobal))
                        {
                            if (wantResult)
                            {
                                resultStorage = resultStorage ?? rb.Stack;
                                rb.EmitBinary(
                                    softGlobal.IsWord ? BinaryOp.GetWord : BinaryOp.GetByte,
                                    SoftGlobalsTable,
                                    Game.MakeOperand(softGlobal.Offset),
                                    resultStorage);
                                return resultStorage;
                            }
                            return null;
                        }

                        // quirks: local
                        if (Locals.TryGetValue(atom, out local))
                        {
                            Context.HandleWarning(new CompilerError(
                                form,
                                CompilerMessages.No_Such_0_Variable_1_Using_The_2_Instead,
                                "global",
                                atom,
                                "local"));
                            return local;
                        }

                        // error
                        Context.HandleError(new CompilerError(form, CompilerMessages.Undefined_0_1, "global or constant", atom));
                        return wantResult ? Game.Zero : null;
                    case StdAtom.LVAL:
                        atom = form.Rest.First as ZilAtom;
                        if (atom == null)
                        {
                            Context.HandleError(new CompilerError(form, CompilerMessages.Expected_An_Atom_After_0, "LVAL"));
                            return wantResult ? Game.Zero : null;
                        }

                        // local
                        if (Locals.TryGetValue(atom, out local))
                            return local;

                        // quirks: constant, global, object, or routine
                        if (Constants.TryGetValue(atom, out operand))
                        {
                            Context.HandleWarning(new CompilerError(
                                form,
                                CompilerMessages.No_Such_0_Variable_1_Using_The_2_Instead,
                                "local",
                                atom,
                                "constant"));
                            return operand;
                        }
                        if (Globals.TryGetValue(atom, out global))
                        {
                            Context.HandleWarning(new CompilerError(
                                form,
                                CompilerMessages.No_Such_0_Variable_1_Using_The_2_Instead,
                                "local",
                                atom,
                                "global"));
                            return global;
                        }
                        if (Objects.TryGetValue(atom, out objbld))
                        {
                            Context.HandleWarning(new CompilerError(
                                form,
                                CompilerMessages.No_Such_0_Variable_1_Using_The_2_Instead,
                                "local",
                                atom,
                                "object"));
                            return objbld;
                        }
                        if (Routines.TryGetValue(atom, out routine))
                        {
                            Context.HandleWarning(new CompilerError(
                                form,
                                CompilerMessages.No_Such_0_Variable_1_Using_The_2_Instead,
                                "local",
                                atom,
                                "routine"));
                            return routine;
                        }

                        // error
                        Context.HandleError(new CompilerError(form, CompilerMessages.Undefined_0_1, "local", atom));
                        return wantResult ? Game.Zero : null;

                    case StdAtom.ITABLE:
                    case StdAtom.TABLE:
                    case StdAtom.PTABLE:
                    case StdAtom.LTABLE:
                    case StdAtom.PLTABLE:
                        operand = CompileImpromptuTable(form);
                        return wantResult ? operand : null;

                    case StdAtom.PROG:
                        return CompilePROG(rb, form.Rest, form.SourceLine, wantResult, resultStorage, "PROG", false, true);
                    case StdAtom.REPEAT:
                        return CompilePROG(rb, form.Rest, form.SourceLine, wantResult, resultStorage, "REPEAT", true, true);
                    case StdAtom.BIND:
                        return CompilePROG(rb, form.Rest, form.SourceLine, wantResult, resultStorage, "BIND", false, false);

                    case StdAtom.DO:
                        return CompileDO(rb, form.Rest, form.SourceLine, wantResult, resultStorage);
                    case StdAtom.MAP_CONTENTS:
                        return CompileMAP_CONTENTS(rb, form.Rest, form.SourceLine, wantResult, resultStorage);
                    case StdAtom.MAP_DIRECTIONS:
                        return CompileMAP_DIRECTIONS(rb, form.Rest, form.SourceLine, wantResult, resultStorage);

                    case StdAtom.COND:
                        return CompileCOND(rb, form.Rest, form.SourceLine, wantResult, resultStorage);

                    case StdAtom.VERSION_P:
                        return CompileVERSION_P(rb, form.Rest, form.SourceLine, wantResult, resultStorage);
                    case StdAtom.IFFLAG:
                        return CompileIFFLAG(rb, form.Rest, form.SourceLine, wantResult, resultStorage);

                    case StdAtom.NOT:
                    case StdAtom.F_P:
                    case StdAtom.T_P:
                        if (form.Rest.First == null || (form.Rest.Rest != null && !form.Rest.Rest.IsEmpty))
                        {
                            Context.HandleError(new CompilerError(
                                form,
                                CompilerMessages._0_Requires_1_Argument1s,
                                head,
                                new CountableString("exactly 1", false)));
                            return Game.Zero;
                        }
                        resultStorage = resultStorage ?? rb.Stack;
                        var label1 = rb.DefineLabel();
                        var label2 = rb.DefineLabel();
                        CompileCondition(rb, form.Rest.First, form.SourceLine, label1, head.StdAtom != StdAtom.T_P);
                        rb.EmitStore(resultStorage, Game.One);
                        rb.Branch(label2);
                        rb.MarkLabel(label1);
                        rb.EmitStore(resultStorage, Game.Zero);
                        rb.MarkLabel(label2);
                        return resultStorage;

                    case StdAtom.OR:
                    case StdAtom.AND:
                        return CompileBoolean(rb, form.Rest, form.SourceLine, head.StdAtom == StdAtom.AND, wantResult, resultStorage);

                    case StdAtom.TELL:
                        return CompileTell(rb, form);
                }

                // routine calls
                var obj = Context.GetZVal(Context.ZEnvironment.InternGlobalName(head));

                while (obj is ZilConstant cnst)
                    obj = cnst.Value;

                switch (obj)
                {
                    case ZilRoutine rtn:
                        // check argument count
                        var args = form.Skip(1).ToArray();
                        if (args.Length < rtn.ArgSpec.MinArgCount ||
                            (rtn.ArgSpec.MaxArgCount != null && args.Length > rtn.ArgSpec.MaxArgCount))
                        {
                            Context.HandleError(CompilerError.WrongArgCount(
                                rtn.Name.ToString(),
                                new ArgCountRange(rtn.ArgSpec.MinArgCount, rtn.ArgSpec.MaxArgCount)));
                            return wantResult ? Game.Zero : null;
                        }

                        // compile routine call
                        result = wantResult ? (resultStorage ?? rb.Stack) : null;
                        using (var argOperands = CompileOperands(rb, form.SourceLine, args))
                        {
                            rb.EmitCall(Routines[head], argOperands.AsArray(), result);
                        }
                        return result;

                    case ZilFalse _:
                        // this always returns 0. we can eliminate the call if none of the arguments have side effects.
                        var argsWithSideEffects = form.Skip(1).Where(zo => HasSideEffects(zo)).ToArray();

                        if (argsWithSideEffects.Length > 0)
                        {
                            result = wantResult ? (resultStorage ?? rb.Stack) : null;
                            using (var argOperands = CompileOperands(rb, form.SourceLine, argsWithSideEffects))
                            {
                                var operands = argOperands.AsArray();
                                if (operands.Any(o => o == rb.Stack))
                                    rb.EmitCall(Game.Zero, operands.Where(o => o == rb.Stack).ToArray(), result);
                            }
                            return result;
                        }
                        return Game.Zero;

                    default:
                        // unrecognized
                        if (!ZBuiltins.IsNearMatchBuiltin(head.Text, zversion, argCount, out var error))
                        {
                            error = new CompilerError(CompilerMessages.Unrecognized_0_1, "routine or instruction", head);
                        }
                        Context.HandleError(error);
                        return wantResult ? Game.Zero : null;
                }
            }
        }

        public IOperand CompileAsOperand(IRoutineBuilder rb, ZilObject expr, ISourceLine src, IVariable suggestion = null)
        {
            Contract.Requires(rb != null);
            Contract.Requires(expr != null);
            Contract.Requires(src != null);
            Contract.Ensures(Contract.Result<IOperand>() != null);

            var constant = CompileConstant(expr);
            if (constant != null)
                return constant;

            switch (expr.StdTypeAtom)
            {
                case StdAtom.FORM:
                    return CompileForm(rb, (ZilForm)expr, true, suggestion ?? rb.Stack);

                case StdAtom.ATOM:
                    var atom = (ZilAtom)expr;
                    if (Globals.ContainsKey(atom))
                    {
                        Context.HandleWarning(new CompilerError(expr.SourceLine ?? src,
                            CompilerMessages.Bare_Atom_0_Interpreted_As_Global_Variable_Index_Be_Sure_This_Is_Right, atom));
                        return Globals[atom].Indirect;
                    }
                    if (SoftGlobals.ContainsKey(atom))
                    {
                        Context.HandleError(new CompilerError(
                            expr.SourceLine ?? src,
                            CompilerMessages.Soft_Variable_0_May_Not_Be_Used_Here,
                            atom));
                    }
                    else
                    {
                        Context.HandleError(new CompilerError(
                            expr.SourceLine ?? src,
                            CompilerMessages.Bare_Atom_0_Used_As_Operand_Is_Not_A_Global_Variable,
                            atom));
                    }
                    return Game.Zero;

                case StdAtom.ADECL:
                    // TODO: verify DECL
                    return CompileAsOperand(rb, ((ZilAdecl)expr).First, src, suggestion ?? rb.Stack);

                default:
                    Context.HandleError(new CompilerError(
                        expr.SourceLine ?? src,
                        CompilerMessages.Expressions_Of_This_Type_Cannot_Be_Compiled));
                    return Game.Zero;
            }
        }

        /// <summary>
        /// Compiles an expression for its value, and then branches on whether the value is nonzero.
        /// </summary>
        /// <param name="rb">The routine builder.</param>
        /// <param name="expr">The expression to compile.</param>
        /// <param name="resultStorage">The variable in which to store the value, or <b>null</b> to
        /// use a natural or temporary location. Must not be the stack.</param>
        /// <param name="label">The label to branch to.</param>
        /// <param name="polarity"><b>true</b> to branch when the expression's value is nonzero,
        /// or <b>false</b> to branch when it's zero.</param>
        /// <param name="tempVarProvider">A delegate that returns a temporary variable to use for
        /// the result. Will only be called when <paramref name="resultStorage"/> is <b>null</b> and
        /// the expression has no natural location.</param>
        /// <returns>The variable where the expression value was stored: always <paramref name="resultStorage"/> if
        /// it is non-null and the expression is valid. Otherwise, may be a constant, or the natural
        /// location of the expression, or a temporary variable from <paramref name="tempVarProvider"/>.</returns>
        internal IOperand CompileAsOperandWithBranch(IRoutineBuilder rb, ZilObject expr, IVariable resultStorage,
            ILabel label, bool polarity, Func<IVariable> tempVarProvider = null)
        {
            Contract.Requires(rb != null);
            Contract.Requires(expr != null);
            Contract.Requires(label != null);
            Contract.Requires(resultStorage != rb.Stack);
            Contract.Requires(resultStorage != null || tempVarProvider != null);
            Contract.Ensures(Contract.Result<IOperand>() != null);

            expr = expr.Expand(Context);
            IOperand result = resultStorage;

            switch (expr)
            {
                case ZilFalse _:
                    if (resultStorage == null)
                    {
                        result = Game.Zero;
                    }
                    else
                    {
                        rb.EmitStore(resultStorage, Game.Zero);
                    }

                    if (polarity == false)
                        rb.Branch(label);

                    return result;

                case ZilFix fix:
                    if (resultStorage == null)
                    {
                        result = Game.MakeOperand(fix.Value);
                    }
                    else
                    {
                        rb.EmitStore(resultStorage, Game.MakeOperand(fix.Value));
                    }

                    bool nonzero = fix.Value != 0;
                    if (polarity == nonzero)
                        rb.Branch(label);

                    return result;

                case ZilAdecl adecl:
                    // TODO: check DECL
                    return CompileAsOperandWithBranch(rb, adecl.First, resultStorage, label, polarity, tempVarProvider);

                case ZilForm form:
                    if (!(form.First is ZilAtom head))
                    {
                        Context.HandleError(new CompilerError(form, CompilerMessages.FORM_Must_Start_With_An_Atom));
                        return Game.Zero;
                    }

                    // check for standard built-ins
                    // prefer the value+predicate version, then value, predicate, void
                    var zversion = Context.ZEnvironment.ZVersion;
                    var argCount = form.Count() - 1;
                    if (ZBuiltins.IsBuiltinValuePredCall(head.Text, zversion, argCount))
                    {
                        if (resultStorage == null)
                            resultStorage = tempVarProvider();

                        ZBuiltins.CompileValuePredCall(head.Text, this, rb, form, resultStorage, label, polarity);
                        return resultStorage;
                    }
                    if (ZBuiltins.IsBuiltinValueCall(head.Text, zversion, argCount))
                    {
                        result = ZBuiltins.CompileValueCall(head.Text, this, rb, form, resultStorage);
                        if (resultStorage != null && resultStorage != result)
                        {
                            rb.EmitStore(resultStorage, result);
                            result = resultStorage;
                        }
                        else if (resultStorage == null && result == rb.Stack)
                        {
                            resultStorage = tempVarProvider();
                            rb.EmitStore(resultStorage, result);
                            result = resultStorage;
                        }
                        rb.BranchIfZero(result, label, !polarity);
                        return result;
                    }
                    if (ZBuiltins.IsBuiltinPredCall(head.Text, zversion, argCount))
                    {
                        if (resultStorage == null)
                            resultStorage = tempVarProvider();

                        var label1 = rb.DefineLabel();
                        var label2 = rb.DefineLabel();
                        ZBuiltins.CompilePredCall(head.Text, this, rb, form, label1, true);
                        rb.EmitStore(resultStorage, Game.Zero);
                        rb.Branch(polarity ? label2 : label);
                        rb.MarkLabel(label1);
                        rb.EmitStore(resultStorage, Game.One);
                        if (polarity)
                            rb.Branch(label);
                        rb.MarkLabel(label2);
                        return resultStorage;
                    }
                    if (ZBuiltins.IsBuiltinVoidCall(head.Text, zversion, argCount))
                    {
                        ZBuiltins.CompileVoidCall(head.Text, this, rb, form);

                        // void calls return true
                        if (resultStorage == null)
                        {
                            result = Game.One;
                        }
                        else
                        {
                            rb.EmitStore(resultStorage, Game.One);
                        }

                        if (polarity == true)
                            rb.Branch(label);

                        return result;
                    }

                    // for anything more complicated, treat it as a value
                    result = CompileAsOperand(rb, form, form.SourceLine, resultStorage);
                    if (resultStorage != null && resultStorage != result)
                    {
                        rb.EmitStore(resultStorage, result);
                        result = resultStorage;
                    }
                    else if (resultStorage == null && result == rb.Stack)
                    {
                        resultStorage = tempVarProvider();
                        rb.EmitStore(resultStorage, result);
                        result = resultStorage;
                    }

                    rb.BranchIfZero(result, label, !polarity);
                    return result;

                default:
                    var constValue = CompileConstant(expr);
                    if (constValue == null)
                    {
                        Context.HandleError(new CompilerError(expr, CompilerMessages.Expressions_Of_This_Type_Cannot_Be_Compiled));
                    }
                    else
                    {
                        if (resultStorage == null)
                        {
                            result = constValue;
                        }
                        else
                        {
                            rb.EmitStore(resultStorage, constValue);
                        }

                        if (polarity == true)
                            rb.Branch(label);
                    }
                    return result;
            }
        }

        IOperand CompileTell(IRoutineBuilder rb, ZilForm form)
        {
            Contract.Requires(rb != null);
            Contract.Requires(form != null);
            Contract.Requires(form.SourceLine != null);
            Contract.Ensures(Contract.Result<IOperand>() != null);

            var args = form.Rest.ToArray();

            int index = 0;
            while (index < args.Length)
            {
                // look for a matching pattern
                bool handled = false;
                foreach (var pattern in Context.ZEnvironment.TellPatterns)
                {
                    var result = pattern.Match(args, index, Context, form.SourceLine);
                    if (result.Matched)
                    {
                        CompileForm(rb, result.Output, false, null);
                        index += pattern.Length;
                        handled = true;
                        break;
                    }
                }

                if (handled)
                    continue;

                // literal string -> PRINTI
                if (args[index] is ZilString zstr)
                {
                    rb.EmitPrint(TranslateString(zstr.Text, Context), false);
                    index++;
                    continue;
                }

                // literal character -> PRINTC
                if (args[index] is ZilChar zch)
                {
                    rb.EmitPrint(PrintOp.Character, Game.MakeOperand(zch.Char));
                    index++;
                    continue;
                }

                // <QUOTE foo> -> <PRINTD ,foo>
                if (args[index] is ZilForm innerForm)
                {
                    if (innerForm.First is ZilAtom atom && atom.StdAtom == StdAtom.QUOTE && innerForm.Rest != null)
                    {
                        var transformed = Context.ChangeType(innerForm.Rest.First, Context.GetStdAtom(StdAtom.GVAL));
                        transformed.SourceLine = form.SourceLine;
                        var obj = CompileAsOperand(rb, transformed, innerForm.SourceLine);
                        rb.EmitPrint(PrintOp.Object, obj);
                        index++;
                        continue;
                    }
                }

                // P?foo expr -> <PRINT <GETP expr ,P?foo>>
                if (args[index] is ZilAtom prop && index + 1 < args.Length)
                {
                    var transformed = (ZilForm)Program.Parse(Context, form.SourceLine,
                        "<PRINT <GETP {0} ,{1}>>", args[index + 1], prop)
                        .Single();
                    CompileForm(rb, transformed, false, null);
                    index += 2;
                    continue;
                }

                // otherwise, treat it as a packed string
                var str = CompileAsOperand(rb, args[index], args[index].SourceLine ?? form.SourceLine);
                rb.EmitPrint(PrintOp.PackedAddr, str);
                index++;
                continue;
            }

            return Game.One;
        }

        bool HasSideEffects(ZilObject expr)
        {
            // only forms can have side effects
            if (!(expr is ZilForm form))
                return false;

            // malformed forms are errors anyway
            if (!(form.First is ZilAtom head))
                return false;

            // some instructions always have side effects
            var zversion = Context.ZEnvironment.ZVersion;
            var argCount = form.Rest.Count();
            if (ZBuiltins.IsBuiltinWithSideEffects(head.Text, zversion, argCount))
                return true;

            // routines are presumed to have side effects
            if (Routines.ContainsKey(head))
                return true;

            // other instructions could still have side effects if their arguments do
            foreach (ZilObject obj in form.Rest)
                if (HasSideEffects(obj))
                    return true;

            return false;
        }
    }
}
