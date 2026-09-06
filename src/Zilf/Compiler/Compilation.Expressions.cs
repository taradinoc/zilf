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
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using Zilf.Compiler.Builtins;
using Zilf.Diagnostics;
using Zilf.Emit;
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
        /// <exception cref="CompilerError">The syntax is incorrect, or an error occurred while compiling a subexpression.</exception>
        internal IOperand? CompileForm(IRoutineBuilder rb, ZilForm form, bool wantResult,
            IVariable? resultStorage)
        {
            using (DiagnosticContext.Push(form.SourceLine))
            {
                var unwrapped = form.Unwrap(Context);

                if (!ReferenceEquals(unwrapped, form))
                {
                    switch (unwrapped)
                    {
                        case ZilForm newForm:
                            form = newForm;
                            break;

                        default:
                            return wantResult ? CompileAsOperand(rb, unwrapped, form.SourceLine, resultStorage) : null;
                    }
                }

                if (form.First is not ZilAtom head)
                {
                    Context.HandleError(new CompilerError(form, CompilerMessages.FORM_Must_Start_With_An_Atom));
                    return wantResult ? Game.Zero : null;
                }

                // built-in statements handled by ZBuiltins
                var zversion = Context.ZEnvironment.ZVersion;
                var currentPlatform = ZBuiltins.GetCurrentBuiltinPlatform(Context.ZEnvironment.TargetPlatform);
                Debug.Assert(form.Rest != null);
                var argCount = form.Rest.Count();

                if (wantResult)
                {
                    // prefer the gb version, then gb+predicate, predicate, void
                    if (ZBuiltins.IsBuiltinValueCall(head.Text, zversion, argCount, currentPlatform))
                    {
                        return ZBuiltins.CompileValueCall(head.Text, this, rb, form, resultStorage);
                    }
                    if (ZBuiltins.IsBuiltinValuePredCall(head.Text, zversion, argCount, currentPlatform))
                    {
                        var label1 = rb.DefineLabel();
                        resultStorage ??= rb.Stack;
                        ZBuiltins.CompileValuePredCall(head.Text, this, rb, form, resultStorage, label1, true);
                        rb.MarkLabel(label1);
                        return resultStorage;
                    }
                    if (ZBuiltins.IsBuiltinPredCall(head.Text, zversion, argCount, currentPlatform))
                    {
                        var label1 = rb.DefineLabel();
                        var label2 = rb.DefineLabel();
                        resultStorage ??= rb.Stack;
                        ZBuiltins.CompilePredCall(head.Text, this, rb, form, label1, true);
                        rb.EmitStore(resultStorage, Game.Zero);
                        rb.Branch(label2);
                        rb.MarkLabel(label1);
                        rb.EmitStore(resultStorage, Game.One);
                        rb.MarkLabel(label2);
                        return resultStorage;
                    }
                    if (ZBuiltins.IsBuiltinVoidCall(head.Text, zversion, argCount, currentPlatform))
                    {
                        ZBuiltins.CompileVoidCall(head.Text, this, rb, form);
                        return Game.One;
                    }
                }
                else
                {
                    // prefer the void version, then predicate, gb, gb+predicate
                    // (predicate saves a cleanup instruction)
                    if (ZBuiltins.IsBuiltinVoidCall(head.Text, zversion, argCount, currentPlatform))
                    {
                        ZBuiltins.CompileVoidCall(head.Text, this, rb, form);
                        return null;
                    }
                    if (ZBuiltins.IsBuiltinPredCall(head.Text, zversion, argCount, currentPlatform))
                    {
                        var dummy = rb.DefineLabel();
                        ZBuiltins.CompilePredCall(head.Text, this, rb, form, dummy, true);
                        rb.MarkLabel(dummy);
                        return null;
                    }
                    if (ZBuiltins.IsBuiltinValueCall(head.Text, zversion, argCount, currentPlatform))
                    {
                        if (ZBuiltins.CompileValueCall(head.Text, this, rb, form, null) == rb.Stack)
                            rb.EmitPopStack();
                        return null;
                    }
                    if (ZBuiltins.IsBuiltinValuePredCall(head.Text, zversion, argCount, currentPlatform))
                    {
                        var label1 = rb.DefineLabel();
                        ZBuiltins.CompileValuePredCall(head.Text, this, rb, form, rb.Stack, label1, true);
                        rb.MarkLabel(label1);
                        rb.EmitPopStack();
                        return null;
                    }
                }

                // routine calls
                ZilAtom internedHead = Context.ZEnvironment.InternGlobalName(head);
                MarkGlobalAsRead(internedHead);
                var obj = Context.GetZVal(internedHead);

                while (obj is ZilConstant cnst)
                    obj = cnst.Value;

                switch (obj)
                {
                    case ZilRoutine rtn:
                        // check argument count
                        var args = form.Skip(1).ToArray();
                        if (args.Length < rtn.ArgSpec.MinArgCount ||
                            rtn.ArgSpec.MaxArgCount != null && args.Length > rtn.ArgSpec.MaxArgCount)
                        {
                            Context.HandleError(CompilerError.WrongArgCount(
                                rtn.Name?.ToString() ?? "<unnamed routine>",
                                new ArgCountRange(rtn.ArgSpec.MinArgCount, rtn.ArgSpec.MaxArgCount)));
                            return wantResult ? Game.Zero : null;
                        }

                        if (args.Length > Game.MaxCallArguments)
                        {
                            Context.HandleError(new CompilerError(
                                form,
                                CompilerMessages.Too_Many_Call_Arguments_Only_0_Allowed_In_V1,
                                Game.MaxCallArguments,
                                Context.ZEnvironment.ZVersion));
                            return wantResult ? Game.Zero : null;
                        }

                        if (rtn.Name != null)
                        {
                            ScheduleRoutineForCompilation(rtn.Name);
                        }

                        if (rtn.Name != null && TryCompileInlineCall(rb, rtn.Name, args, wantResult,
                            resultStorage, form.SourceLine, out var inlineResult))
                        {
                            return inlineResult;
                        }
                        if (rtn.Name != null)
                            RecordNonInlinedCall(rtn.Name);

                        // compile routine call
                        resultStorage = wantResult ? (resultStorage ?? rb.Stack) : null;
                        using (var argOperands = CompileOperands(rb, form.SourceLine, args))
                        {
                            rb.EmitCall(Routines[head], argOperands.AsArray(), resultStorage);
                        }
                        return resultStorage;

                    case ZilFalse _:
                        // this always returns 0. we can eliminate the call if none of the arguments have side effects.
                        var argsWithSideEffects = form.Skip(1).Where(HasSideEffects).ToArray();

                        if (argsWithSideEffects.Length <= 0)
                            return Game.Zero;

                        resultStorage = wantResult ? (resultStorage ?? rb.Stack) : null;
                        using (var argOperands = CompileOperands(rb, form.SourceLine, argsWithSideEffects))
                        {
                            var operands = argOperands.AsArray();
                            if (operands.Any(o => o == rb.Stack))
                                rb.EmitCall(Game.Zero, operands.Where(o => o == rb.Stack).ToArray(), resultStorage);
                        }
                        return resultStorage;

                    default:
                        // unrecognized
                        if (!ZBuiltins.IsNearMatchBuiltin(head.Text, zversion, argCount, currentPlatform, out var error))
                        {
                            error = new CompilerError(CompilerMessages.Unrecognized_0_1, "routine or instruction", head);
                        }
                        Context.HandleError(error);
                        return wantResult ? Game.Zero : null;
                }
            }
        }

        /// <exception cref="CompilerError">The syntax is incorrect, or an error occurred while compiling a subexpression.</exception>
        public IOperand CompileAsOperand(IRoutineBuilder rb, ZilObject expr, ISourceLine? src,
            IVariable? suggestion = null)
        {
            expr = expr.Unwrap(Context);

            if (CompileConstant(expr, AmbiguousConstantMode.Pessimistic) is { } constant)
                return constant;

            switch (expr.StdTypeAtom)
            {
                case StdAtom.FORM:
                    return CompileForm(rb, (ZilForm)expr, true, suggestion ?? rb.Stack)!;

                case StdAtom.ATOM:
                    var atom = (ZilAtom)expr;
                    if (Globals.TryGetValue(atom, out var gb))
                    {
                        Context.HandleError(new CompilerError(
                            src,
                            CompilerMessages.Bare_Atom_0_Interpreted_As_Global_Variable_Index,
                            atom));
                        MarkGlobalAsRead(atom);
                        return gb.Indirect;
                    }
                    if (SoftGlobals.ContainsKey(atom))
                    {
                        Context.HandleError(new CompilerError(
                            src,
                            CompilerMessages.Soft_Variable_0_May_Not_Be_Used_Here,
                            atom));
                    }
                    else
                    {
                        Context.HandleError(new CompilerError(
                            src,
                            CompilerMessages.Bare_Atom_0_Used_As_Operand_Is_Not_A_Global_Variable,
                            atom));
                    }
                    return Game.Zero;

                default:
                    Context.HandleError(new CompilerError(
                        expr.SourceLine ?? src,
                        CompilerMessages.Expressions_Of_Type_0_Cannot_Be_Compiled,
                        expr.GetTypeAtom(Context)));
                    return Game.Zero;
            }
        }

        /// <summary>
        /// Compiles an expression for its value, and then branches on whether the value is nonzero.
        /// </summary>
        /// <param name="rb">The routine builder.</param>
        /// <param name="expr">The expression to compile.</param>
        /// <param name="resultStorage">The variable in which to store the value, or <see langword="null"/> to
        /// use a natural or temporary location. Must not be the stack.</param>
        /// <param name="label">The label to branch to.</param>
        /// <param name="polarity"><see langword="true"/> to branch when the expression's value is nonzero,
        /// or <see langword="false"/> to branch when it's zero.</param>
        /// <param name="tempVarProvider">A delegate that returns a temporary variable to use for
        /// the result. Will only be called when <paramref name="resultStorage"/> is <see langword="null"/> and
        /// the expression has no natural location.</param>
        /// <returns>The variable where the expression value was stored: always <paramref name="resultStorage"/> if
        /// it is non-null and the expression is valid. Otherwise, may be a constant, or the natural
        /// location of the expression, or a temporary variable from <paramref name="tempVarProvider"/>.</returns>
        /// <exception cref="CompilerError">The syntax is incorrect, or an error occurred while compiling a subexpression.</exception>
        internal IOperand CompileAsOperandWithBranch(IRoutineBuilder rb, ZilObject expr,
            IVariable? resultStorage,
            ILabel label, bool polarity, Func<IVariable>? tempVarProvider = null)
        {
            expr = expr.Unwrap(Context);
            IOperand result = resultStorage!;

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

                case ZilForm { First: not ZilAtom }:
                    Context.HandleError(new CompilerError(expr, CompilerMessages.FORM_Must_Start_With_An_Atom));
                    return Game.Zero;

                case ZilForm { First: ZilAtom head } form:
                    // check for standard built-ins
                    // prefer the value+predicate version, then value, predicate, void
                    var zversion = Context.ZEnvironment.ZVersion;
                    var currentPlatform = ZBuiltins.GetCurrentBuiltinPlatform(Context.ZEnvironment.TargetPlatform);
                    var argCount = form.Count() - 1;
                    if (ZBuiltins.IsBuiltinValuePredCall(head.Text, zversion, argCount, currentPlatform))
                    {
                        if (resultStorage == null)
                        {
                            Debug.Assert(tempVarProvider != null);
                            resultStorage = tempVarProvider();
                        }

                        ZBuiltins.CompileValuePredCall(head.Text, this, rb, form, resultStorage, label, polarity);
                        return resultStorage;
                    }
                    if (ZBuiltins.IsBuiltinValueCall(head.Text, zversion, argCount, currentPlatform))
                    {
                        result = ZBuiltins.CompileValueCall(head.Text, this, rb, form, resultStorage);
                        if (resultStorage != null && resultStorage != result)
                        {
                            rb.EmitStore(resultStorage, result);
                            result = resultStorage;
                        }
                        else if (resultStorage == null && result == rb.Stack)
                        {
                            Debug.Assert(tempVarProvider != null);
                            resultStorage = tempVarProvider();
                            rb.EmitStore(resultStorage, result);
                            result = resultStorage;
                        }
                        rb.BranchIfZero(result, label, !polarity);
                        return result;
                    }
                    if (ZBuiltins.IsBuiltinPredCall(head.Text, zversion, argCount, currentPlatform))
                    {
                        if (resultStorage == null)
                        {
                            Debug.Assert(tempVarProvider != null);
                            resultStorage = tempVarProvider();
                        }

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
                    if (ZBuiltins.IsBuiltinVoidCall(head.Text, zversion, argCount, currentPlatform))
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

                        if (polarity)
                            rb.Branch(label);

                        return result;
                    }

                    // for anything more complicated, treat it as a gb
                    result = CompileAsOperand(rb, form, form.SourceLine, resultStorage);
                    if (resultStorage != null && resultStorage != result)
                    {
                        rb.EmitStore(resultStorage, result);
                        result = resultStorage;
                    }
                    else if (resultStorage == null && result == rb.Stack)
                    {
                        Debug.Assert(tempVarProvider != null);
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
                        Context.HandleError(new CompilerError(
                            expr,
                            CompilerMessages.Expressions_Of_Type_0_Cannot_Be_Compiled,
                            expr.GetTypeAtom(Context)));
                        return Game.Zero;
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

                        if (polarity)
                            rb.Branch(label);
                    }
                    return result;
            }
        }

        internal void CompileTell(IRoutineBuilder rb, ISourceLine src, ZilObject[] args)
        {
            int index = 0;
            while (index < args.Length)
            {
                // look for a matching pattern
                bool handled = false;
                foreach (var pattern in Context.ZEnvironment.TellPatterns)
                {
                    var result = pattern.Match(args, index, Context, src);
                    if (!result.Matched)
                        continue;

                    CompileForm(rb, result.Output!, false, null);
                    index += pattern.Length;
                    handled = true;
                    break;
                }

                if (handled)
                    continue;

                switch (args[index])
                {
                    // literal string -> PRINTI
                    case ZilString zstr:
                        var translated = TranslateString(zstr, Context);
                        if (Game is Zilf.Emit.Cornerstone.GameBuilder)
                            rb.EmitPrint(translated, crlfRtrue: false);
                        else
                            rb.EmitPrint(translated, false);
                        index++;
                        continue;

                    // literal character -> PRINTC
                    case ZilChar zch:
                        NoteZMachineCharUsage(zch, zch.SourceLine ?? src);
                        rb.EmitPrint(PrintOp.Character, Game.MakeOperand(zch.Char));
                        index++;
                        continue;

                    // <QUOTE foo> -> <PRINTD ,foo>
                    case ZilForm innerForm when innerForm.First is ZilAtom atom && atom.StdAtom == StdAtom.QUOTE &&
                                                innerForm.Rest != null && !innerForm.Rest.IsEmpty:
                        Debug.Assert(innerForm.Rest.First != null);
                        var newGval = Context.ChangeType(innerForm.Rest.First, Context.GetStdAtom(StdAtom.GVAL));
                        newGval.SourceLine = src;
                        var obj = CompileAsOperand(rb, newGval, innerForm.SourceLine);
                        rb.EmitPrint(PrintOp.Object, obj);
                        index++;
                        continue;

                    // P?foo expr -> <PRINT <GETP expr ,P?foo>>
                    case ZilAtom propAtom when index + 1 < args.Length:
                        if (!propAtom.Text.StartsWith("P?", StringComparison.Ordinal))
                        {
                            Context.HandleError(new CompilerError(
                                CompilerMessages.Bare_Atom_0_Is_Not_A_TELL_Token_Or_Property,
                                propAtom));
                            // only advance by one, because they probably meant these as two separate arguments
                            index++;
                            break;
                        }
                        var prop = CompileAsOperand(rb, propAtom, src);
                        var expr = CompileAsOperand(rb, args[index + 1], args[index + 1].SourceLine ?? src);
                        rb.EmitBinary(BinaryOp.GetProperty, expr, prop, rb.Stack);
                        rb.EmitPrint(PrintOp.PackedAddr, rb.Stack);
                        index += 2;
                        continue;

                    // otherwise, treat it as a packed string
                    default:
                        var str = CompileAsOperand(rb, args[index], args[index].SourceLine ?? src);
                        rb.EmitPrint(PrintOp.PackedAddr, str);
                        index++;
                        break;
                }
            }
        }

        [SuppressMessage("ReSharper", "ConvertIfStatementToReturnStatement")]
        public bool HasSideEffects(ZilObject expr)
        {
            // only forms can have side effects
            if (expr is not ZilForm form)
                return false;

            // malformed forms are errors anyway
            if (form.First is not ZilAtom head)
                return false;

            Debug.Assert(form.Rest != null);

            // some instructions always have side effects
            var zversion = Context.ZEnvironment.ZVersion;
            var argCount = form.Rest.Count();
            if (ZBuiltins.IsBuiltinWithSideEffects(head.Text, zversion, argCount))
                return true;

            // A call that will be inlined has the effects of its arguments and transplanted body, not the
            // conservative effects of an opaque routine call. This lets operand preservation see through pure
            // nested inline calls instead of allocating temporaries that become unnecessary after transplantation.
            if (_inlineRoutines != null && _inlineRoutines.TryGetValue(head, out var inlineRoutine) &&
                inlineRoutine.LegacyEligible && !Context.OptimizeForSize)
                return form.Rest.Any(HasSideEffects) || HasSideEffects(inlineRoutine.Body);

            // routines are presumed to have side effects
            if (Routines.ContainsKey(head))
                return true;

            // other instructions could still have side effects if their arguments do
            return form.Rest.Any(HasSideEffects);
        }
    }
}
