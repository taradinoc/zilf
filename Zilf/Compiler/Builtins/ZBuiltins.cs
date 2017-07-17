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
using System.Diagnostics;
using System.Diagnostics.Contracts;
using System.Linq;
using System.Reflection;
using Zilf.Emit;
using Zilf.Interpreter;
using Zilf.Interpreter.Values;
using Zilf.Language;
using Zilf.ZModel;
using Zilf.Diagnostics;
using Zilf.Common;
using JetBrains.Annotations;

namespace Zilf.Compiler.Builtins
{
    static class ZBuiltins
    {
        [NotNull]
        static readonly ILookup<string, BuiltinSpec> builtins =
            (from mi in typeof(ZBuiltins).GetMethods(BindingFlags.Public | BindingFlags.Static)
             from a in mi.GetCustomAttributes<BuiltinAttribute>()
             from name in a.Names
             select new { Name = name, Attr = a, Method = mi })
            .ToLookup(r => r.Name, r => new BuiltinSpec(r.Attr, r.Method));

        public static bool IsBuiltinValueCall(string name, int zversion, int argCount)
        {
            return builtins[name].Any(s => s.AppliesTo(zversion, argCount, typeof(ValueCall)));
        }

        public static bool IsBuiltinVoidCall(string name, int zversion, int argCount)
        {
            return builtins[name].Any(s => s.AppliesTo(zversion, argCount, typeof(VoidCall)));
        }

        public static bool IsBuiltinPredCall(string name, int zversion, int argCount)
        {
            return builtins[name].Any(s => s.AppliesTo(zversion, argCount, typeof(PredCall)));
        }

        public static bool IsBuiltinValuePredCall(string name, int zversion, int argCount)
        {
            return builtins[name].Any(s => s.AppliesTo(zversion, argCount, typeof(ValuePredCall)));
        }

        public static bool IsBuiltinWithSideEffects(string name, int zversion, int argCount)
        {
            // true if there's a void, value, or predicate version with side effects
            return builtins[name].Any(s => s.AppliesTo(zversion, argCount) && s.Attr.HasSideEffect);
        }

        public static bool IsNearMatchBuiltin(string name, int zversion, int argCount, out CompilerError error)
        {
            // is there a match with this zversion but any arg count?
            var wrongArgCount =
                builtins[name].Where(s => ZEnvironment.VersionMatches(
                    zversion, s.Attr.MinVersion, s.Attr.MaxVersion))
                .ToArray();
            if (wrongArgCount.Length > 0)
            {
                var counts = wrongArgCount.Select(s => new ArgCountRange(s.MinArgs, s.MaxArgs));

                // be a little more helpful if this arg count would work in another zversion
                var acceptableVersion = builtins[name]
                    .FirstOrDefault(s => argCount >= s.MinArgs && (s.MaxArgs == null || argCount <= s.MaxArgs))
                    ?.Attr.MinVersion;

                error = CompilerError.WrongArgCount(name, counts, acceptableVersion);
                return true;
            }

            // is there a match with any zversion?
            if (builtins.Contains(name))
            {
                error = new CompilerError(CompilerMessages._0_Is_Not_Supported_In_This_Zmachine_Version, name);
                return true;
            }

            // not a near match
            error = null;
            return false;
        }

        delegate void InvalidArgumentDelegate(int index, [NotNull] string message);

        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Globalization", "CA1305:SpecifyIFormatProvider", MessageId = "System.String.Format(System.String,System.Object,System.Object,System.Object)")]
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Performance", "CA1800:DoNotCastUnnecessarily")]
        [NotNull]
        static IList<BuiltinArg> ValidateArguments(
            [NotNull] Compilation cc, [NotNull] BuiltinSpec spec, [NotNull] ParameterInfo[] builtinParamInfos,
            [NotNull] ZilObject[] args, [NotNull] InvalidArgumentDelegate error)
        {
            Contract.Requires(cc != null);
            Contract.Requires(spec != null);
            Contract.Requires(builtinParamInfos != null);
            Contract.Requires(builtinParamInfos.Length >= 1);
            Contract.Requires(args != null);
            Contract.Requires(error != null);
            Contract.Ensures(Contract.Result<IList<BuiltinArg>>() != null);
            Contract.Ensures(Contract.Result<IList<BuiltinArg>>().Count == args.Length);

            // args may be short (for optional params)

            var result = new List<BuiltinArg>(args.Length);

            for (int i = 0, j = spec.Attr.Data == null ? 1 : 2; i < args.Length; i++, j++)
            {
                Contract.Assume(j < builtinParamInfos.Length);

                var pi = builtinParamInfos[j];

                void InnerError(string msg) => error(i, msg);

                if (ParameterTypeHandler.Handlers.TryGetValue(pi.ParameterType, out var handler))
                {
                    result.Add(handler.Process(cc, InnerError, args[i], pi));
                }
                else if (pi.ParameterType.IsArray &&
                    ParameterTypeHandler.Handlers.TryGetValue(pi.ParameterType.GetElementType(), out handler))
                {
                    // consume all remaining arguments
                    while (i < args.Length)
                    {
                        result.Add(handler.Process(cc, InnerError, args[i], pi));
                        i++;
                    }

                    break;
                }
                else
                {
                    throw new ArgumentException(
                        $"Unsupported type {pi.ParameterType} for parameter {j} ({pi.Name})",
                        nameof(builtinParamInfos));
                }
            }

            return result;
        }

        [NotNull]
        static List<object> MakeBuiltinMethodParams(
            [NotNull] BuiltinSpec spec, [ItemNotNull] [NotNull] ParameterInfo[] builtinParamInfos,
            [NotNull] object call, [NotNull] IList<BuiltinArg> args)
        {
            Contract.Requires(spec != null);
            Contract.Requires(builtinParamInfos != null);
            Contract.Requires(builtinParamInfos.Length >= 1);
            Contract.Requires(spec.Attr.Data == null || builtinParamInfos.Length >= 2);
            Contract.Requires(call != null);
            Contract.Requires(args != null);
            Contract.Requires(Contract.ForAll(args, a => a.Type == BuiltinArgType.Operand));
            Contract.Ensures(Contract.Result<List<object>>().Count == builtinParamInfos.Length);

            /* args.Length (plus call and data) may differ from builtinParamInfos.Length,
             * due to optional arguments and params arrays. */

            var result = new List<object>(builtinParamInfos.Length) { call };

            // data (optional)
            int i = 1;
            if (spec.Attr.Data != null)
            {
                result.Add(spec.Attr.Data);
                i++;
            }

            // operands
            for (int j = 0; i < builtinParamInfos.Length; i++, j++)
            {
                Contract.Assume(i < builtinParamInfos.Length);

                var pi = builtinParamInfos[i];

                if (pi.ParameterType == typeof(IOperand[]))
                {
                    // add all remaining operands as a param array
                    result.Add(j >= args.Count
                        ? new IOperand[0]
                        : args.Skip(j).Select(a => (IOperand)a.Value).ToArray());
                }
                else if (pi.ParameterType == typeof(ZilObject[]))
                {
                    // add all remaining values as a param array
                    result.Add(j >= args.Count
                        ? new ZilObject[0]
                        : args.Skip(j).Select(a => (ZilObject)a.Value).ToArray());
                }
                else if (j >= args.Count)
                {
                    result.Add(pi.DefaultValue);
                }
                else
                {
                    result.Add(args[j].Value);
                }
            }

            return result;
        }

        static object CompileBuiltinCall<TCall>([NotNull] string name, [NotNull] Compilation cc,
            [NotNull] IRoutineBuilder rb, [NotNull] ZilListoidBase form, TCall call)
            where TCall : struct
        {
            Contract.Requires(name != null);
            Contract.Requires(cc != null);
            Contract.Requires(rb != null);
            Contract.Requires(form != null);

            int zversion = cc.Context.ZEnvironment.ZVersion;
            var argList = form.Rest;
            var args = argList.ToArray();
            var candidateSpecs = builtins[name].Where(s => s.AppliesTo(zversion, args.Length, typeof(TCall))).ToArray();
            Contract.Assume(candidateSpecs.Length >= 1);

            // find the best matching spec, if there's more than one
            BuiltinSpec spec;
            if (candidateSpecs.Length > 1)
            {
                // choose the one with the fewest validation errors
                spec = candidateSpecs.OrderBy(s =>
                {
                    int errors = 0;
                    var pis = s.Method.GetParameters();
                    ValidateArguments(cc, s, pis, args, delegate { errors++; });
                    return errors;
                }).ThenBy(s => s.Attr.Priority).First();
            }
            else
            {
                spec = candidateSpecs[0];
            }
            var builtinParamInfos = spec.Method.GetParameters();

            // validate arguments
            bool valid = true;
            var validatedArgs = ValidateArguments(cc, spec, builtinParamInfos, args,
                (i, msg) =>
                {
                    cc.Context.HandleError(new CompilerError(form, CompilerMessages._0_Argument_1_2,
                        name, i + 1, msg));
                    valid = false;
                });

            if (!valid)
                return cc.Game.Zero;

            // extract the arguments that need evaluation, and remember their original indexes
            var needEval =
                validatedArgs
                .Select((a, oidx) => new { a, oidx })
                .Where(p => p.a.Type == BuiltinArgType.NeedsEval)
                .ToArray();
            var needEvalExprs = Array.ConvertAll(needEval, p => (ZilObject)p.a.Value);

            // generate code for arguments
            using (var operands = cc.CompileOperands(rb, form.SourceLine, needEvalExprs))
            {
                // update validatedArgs with the evaluated operands
                for (int i = 0; i < operands.Count; i++)
                {
                    var oidx = needEval[i].oidx;
                    validatedArgs[oidx] = new BuiltinArg(BuiltinArgType.Operand, operands[i]);
                }

                // call the spec method to generate code for the builtin
                var builtinParams = MakeBuiltinMethodParams(spec, builtinParamInfos, call, validatedArgs);
                try
                {
                    return spec.Method.Invoke(null, builtinParams.ToArray());
                }
                catch (TargetInvocationException ex) when (ex.InnerException is ZilError zex)
                {
                    throw zex;
                }
            }
        }

        [NotNull]
        public static IOperand CompileValueCall([NotNull] string name, [NotNull] Compilation cc, [NotNull] IRoutineBuilder rb, [NotNull] ZilForm form,
            [CanBeNull] IVariable resultStorage)
        {
            Contract.Requires(name != null);
            Contract.Requires(cc != null);
            Contract.Requires(rb != null);
            Contract.Requires(form != null);
            Contract.Ensures(Contract.Result<IOperand>() != null);

            // TODO: allow resultStorage to be passed as null to handlers that want it? are there any?
            return (IOperand)CompileBuiltinCall(name, cc, rb, form,
                new ValueCall(cc, rb, form, resultStorage ?? rb.Stack));
        }

        public static void CompileVoidCall([NotNull] string name, [NotNull] Compilation cc, [NotNull] IRoutineBuilder rb, [NotNull] ZilForm form)
        {
            Contract.Requires(name != null);
            Contract.Requires(cc != null);
            Contract.Requires(rb != null);
            Contract.Requires(form != null);

            CompileBuiltinCall(name, cc, rb, form, new VoidCall(cc, rb, form));
        }

        public static void CompilePredCall([NotNull] string name, [NotNull] Compilation cc, [NotNull] IRoutineBuilder rb, [NotNull] ZilForm form, [NotNull] ILabel label, bool polarity)
        {
            Contract.Requires(name != null);
            Contract.Requires(cc != null);
            Contract.Requires(rb != null);
            Contract.Requires(form != null);
            Contract.Requires(label != null);

            CompileBuiltinCall(name, cc, rb, form, new PredCall(cc, rb, form, label, polarity));
        }

        public static void CompileValuePredCall([NotNull] string name, [NotNull] Compilation cc, [NotNull] IRoutineBuilder rb, [NotNull] ZilForm form,
            [CanBeNull] IVariable resultStorage, [NotNull] ILabel label, bool polarity)
        {
            Contract.Requires(name != null);
            Contract.Requires(cc != null);
            Contract.Requires(rb != null);
            Contract.Requires(form != null);
            Contract.Requires(label != null);

            CompileBuiltinCall(name, cc, rb, form,
                new ValuePredCall(cc, rb, form, resultStorage ?? rb.Stack, label, polarity));
        }

        #region Equality Opcodes

        // TODO: simplify this method
        // TODO: add a way to tag builtins as needing local variable access, so we can give better errors when they're used from GO?
        /// <exception cref="CompilerError">Local variables are not allowed here.</exception>
        [Builtin("EQUAL?", "=?", "==?")]
        public static void VarargsEqualityOp(
            PredCall c, [NotNull] IOperand arg1, [NotNull] IOperand arg2,
            [NotNull] params IOperand[] restOfArgs)
        {
            Contract.Requires(arg1 != null);
            Contract.Requires(arg2 != null);
            Contract.Requires(restOfArgs != null);

            if (arg1 is INumericOperand num1)
            {
                var value = num1.Value;
                var num2 = arg2 as INumericOperand;

                if (num2?.Value == value ||
                    restOfArgs.OfType<INumericOperand>().Any(arg => arg.Value == value))
                {
                    // we know it's equal, so pop any stack operands and branch accordingly
                    if (arg2 == c.rb.Stack)
                        c.rb.EmitPopStack();

                    foreach (var arg in restOfArgs)
                        if (arg == c.rb.Stack)
                            c.rb.EmitPopStack();

                    if (c.polarity)
                        c.rb.Branch(c.label);

                    return;
                }

                if (num2 != null &&
                    num2.Value != value &&
                    restOfArgs.All(arg => arg is INumericOperand numArg && numArg.Value != value))
                {
                    // we know it's not equal, and there are no stack operands, so branch accordingly
                    if (!c.polarity)
                        c.rb.Branch(c.label);

                    return;
                }

                // we can't simplify the branch, but we can still skip testing all the constants
                if (num2 != null || restOfArgs.Any(arg => arg is INumericOperand))
                {
                    var queue = new Queue<IOperand>(restOfArgs.Length);

                    if (num2 == null)
                        queue.Enqueue(arg2);

                    foreach (var arg in restOfArgs)
                        if (!(arg is INumericOperand))
                            queue.Enqueue(arg);

                    arg2 = queue.Dequeue();
                    restOfArgs = queue.ToArray();
                }
            }

            if (restOfArgs.Length <= 2)
            {
                // TODO: there should really just be one BranchIfEqual with optional params
                switch (restOfArgs.Length)
                {
                    case 2:
                        c.rb.BranchIfEqual(arg1, arg2, restOfArgs[0], restOfArgs[1], c.label, c.polarity);
                        break;
                    case 1:
                        c.rb.BranchIfEqual(arg1, arg2, restOfArgs[0], c.label, c.polarity);
                        break;
                    default:
                        c.rb.BranchIfEqual(arg1, arg2, c.label, c.polarity);
                        break;
                }
            }
            else
            {
                ZilAtom tempAtom = null;
                if (arg1 == c.rb.Stack)
                {
                    tempAtom = ZilAtom.Parse("?TMP", c.cc.Context);
                    var tempLocal = c.cc.PushInnerLocal(c.rb, tempAtom);
                    c.rb.EmitStore(tempLocal, arg1);
                    arg1 = tempLocal;
                }

                var queue = new Queue<IOperand>(1 + restOfArgs.Length);
                queue.Enqueue(arg2);
                foreach (var arg in restOfArgs)
                    queue.Enqueue(arg);

                if (c.polarity)
                {
                    while (queue.Count > 0)
                    {
                        switch (queue.Count)
                        {
                            default:
                                c.rb.BranchIfEqual(arg1, queue.Dequeue(), queue.Dequeue(), queue.Dequeue(), c.label, true);
                                break;
                            case 2:
                                c.rb.BranchIfEqual(arg1, queue.Dequeue(), queue.Dequeue(), c.label, true);
                                break;
                            case 1:
                                c.rb.BranchIfEqual(arg1, queue.Dequeue(), c.label, true);
                                break;
                        }
                    }
                }
                else
                {
                    var skip = c.rb.DefineLabel();

                    while (queue.Count > 0)
                    {
                        // the last test (count <= 3) has false polarity and branches to the target label
                        // the earlier ones (count > 3) have true polarity and just skip the rest of the tests
                        switch (queue.Count)
                        {
                            case 3:
                                c.rb.BranchIfEqual(arg1, queue.Dequeue(), queue.Dequeue(), queue.Dequeue(), c.label, false);
                                break;
                            case 2:
                                c.rb.BranchIfEqual(arg1, queue.Dequeue(), queue.Dequeue(), c.label, false);
                                break;
                            case 1:
                                c.rb.BranchIfEqual(arg1, queue.Dequeue(), c.label, false);
                                break;
                            default:
                                c.rb.BranchIfEqual(arg1, queue.Dequeue(), queue.Dequeue(), queue.Dequeue(), skip, true);
                                break;
                        }
                    }

                    c.rb.MarkLabel(skip);
                }

                if (tempAtom != null)
                    c.cc.PopInnerLocal(tempAtom);
            }
        }

        /// <exception cref="CompilerError">Local variables are not allowed here.</exception>
        [Builtin("N=?", "N==?")]
        public static void NegatedVarargsEqualityOp(
            PredCall c, [NotNull] IOperand arg1, [NotNull] IOperand arg2,
            [NotNull] params IOperand[] restOfArgs)
        {
            Contract.Requires(arg1 != null);
            Contract.Requires(arg2 != null);
            Contract.Requires(restOfArgs != null);

            var innerCall = new PredCall(c.cc, c.rb, c.form, c.label, !c.polarity);
            VarargsEqualityOp(innerCall, arg1, arg2, restOfArgs);
        }

        #endregion

        #region Ternary Opcodes

        [Builtin("DCLEAR", Data = TernaryOp.ErasePicture, MinVersion = 6, HasSideEffect = true)]
        [Builtin("DIROUT", Data = TernaryOp.DirectOutput, MinVersion = 6, HasSideEffect = true)]
        [Builtin("DISPLAY", Data = TernaryOp.DrawPicture, MinVersion = 6, HasSideEffect = true)]
        [Builtin("WINPOS", Data = TernaryOp.MoveWindow, MinVersion = 6, HasSideEffect = true)]
        [Builtin("WINPUT", Data = TernaryOp.PutWindowProperty, MinVersion = 6, HasSideEffect = true)]
        [Builtin("WINSIZE", Data = TernaryOp.WindowSize, MinVersion = 6, HasSideEffect = true)]
        public static void TernaryVoidOp(
            VoidCall c, [Data] TernaryOp op,
            [NotNull] IOperand left, [NotNull] IOperand center, [NotNull] IOperand right)
        {
            Contract.Requires(left != null);
            Contract.Requires(center != null);
            Contract.Requires(right != null);

            c.rb.EmitTernary(op, left, center, right, null);
        }

        [Builtin("MARGIN", Data = TernaryOp.SetMargins, MinVersion = 6, HasSideEffect = true)]
        [Builtin("WINATTR", Data = TernaryOp.WindowStyle, MinVersion = 6, HasSideEffect = true)]
        public static void TernaryOptionalVoidOp(
            VoidCall c, [Data] TernaryOp op,
            [NotNull] IOperand left, [NotNull] IOperand center, [CanBeNull] IOperand right = null)
        {
            Contract.Requires(left != null);
            Contract.Requires(center != null);

            c.rb.EmitTernary(op, left, center, right ?? c.cc.Game.Zero, null);
        }

        [Builtin("PUT", "ZPUT", Data = TernaryOp.PutWord, HasSideEffect = true)]
        [Builtin("PUTB", Data = TernaryOp.PutByte, HasSideEffect = true)]
        public static void TernaryTableVoidOp(
            VoidCall c, [Data] TernaryOp op,
            [Table][NotNull] IOperand left, [NotNull] IOperand center, [NotNull] IOperand right)
        {
            Contract.Requires(left != null);
            Contract.Requires(center != null);
            Contract.Requires(right != null);

            c.rb.EmitTernary(op, left, center, right, null);
        }

        [Builtin("PUTP", Data = TernaryOp.PutProperty, HasSideEffect = true)]
        public static void TernaryObjectVoidOp(
            VoidCall c, [Data] TernaryOp op,
            [Object][NotNull] IOperand left, [NotNull] IOperand center, [NotNull] IOperand right)
        {
            Contract.Requires(left != null);
            Contract.Requires(center != null);
            Contract.Requires(right != null);

            c.rb.EmitTernary(op, left, center, right, null);
        }

        [Builtin("COPYT", Data = TernaryOp.CopyTable, HasSideEffect = true, MinVersion = 5)]
        public static void TernaryTableTableVoidOp(
            VoidCall c, [Data] TernaryOp op,
            [Table][NotNull] IOperand left, [Table][NotNull] IOperand center, [NotNull] IOperand right)
        {
            Contract.Requires(left != null);
            Contract.Requires(center != null);
            Contract.Requires(right != null);

            c.rb.EmitTernary(op, left, center, right, null);
        }

        #endregion

        #region Binary Opcodes

        [Builtin("MOD", Data = BinaryOp.Mod)]
        [Builtin("ASH", "ASHIFT", Data = BinaryOp.ArtShift, MinVersion = 5)]
        [Builtin("LSH", "SHIFT", Data = BinaryOp.LogShift, MinVersion = 5)]
        [Builtin("WINGET", Data = BinaryOp.GetWindowProperty, MinVersion = 6)]
        [NotNull]
        public static IOperand BinaryValueOp(
            ValueCall c, [Data] BinaryOp op, [NotNull] IOperand left, [NotNull] IOperand right)
        {
            Contract.Requires(left != null);
            Contract.Requires(right != null);
            Contract.Ensures(Contract.Result<IOperand>() != null);

            if (left is INumericOperand nleft && right is INumericOperand nright)
            {
                switch (op)
                {
                    case BinaryOp.Mod:
                        return c.cc.Game.MakeOperand((short)(nleft.Value % nright.Value));
                    case BinaryOp.ArtShift:
                        if (nright.Value < 0)
                            return c.cc.Game.MakeOperand((short)(nleft.Value >> -nright.Value));
                        return c.cc.Game.MakeOperand((short)(nleft.Value << nright.Value));
                    case BinaryOp.LogShift:
                        if (nright.Value < 0)
                            return c.cc.Game.MakeOperand((short)((ushort)nleft.Value >> -nright.Value));
                        return c.cc.Game.MakeOperand((short)((ushort)nleft.Value << nright.Value));
                }
            }

            c.rb.EmitBinary(op, left, right, c.resultStorage);
            return c.resultStorage;
        }

        /// <exception cref="CompilerError">The syntax is incorrect, or an error occurred while compiling a subexpression.</exception>
        [Builtin("XORB")]
        [NotNull]
        public static IOperand BinaryXorOp(ValueCall c, [NotNull] ZilObject left, [NotNull] ZilObject right)
        {
            Contract.Requires(left != null);
            Contract.Requires(right != null);
            Contract.Ensures(Contract.Result<IOperand>() != null);

            ZilObject value;
            if (left is ZilFix lf && lf.Value == -1)
            {
                value = right;
            }
            else if (right is ZilFix rf && rf.Value == -1)
            {
                value = left;
            }
            else
            {
                return c.HandleMessage(CompilerMessages._0_One_Operand_Must_Be_1, "XORB");
            }

            var storage = c.cc.CompileAsOperand(c.rb, value, c.form.SourceLine, c.resultStorage);

            if (storage is INumericOperand num)
            {
                return c.cc.Game.MakeOperand((short)(~num.Value));
            }
            c.rb.EmitUnary(UnaryOp.Not, storage, c.resultStorage);
            return c.resultStorage;
        }

        [NotNull]
        [Builtin("ADD", "+", Data = BinaryOp.Add)]
        [Builtin("SUB", "-", Data = BinaryOp.Sub)]
        [Builtin("MUL", "*", Data = BinaryOp.Mul)]
        [Builtin("DIV", "/", Data = BinaryOp.Div)]
        [Builtin("BAND", "ANDB", Data = BinaryOp.And)]
        [Builtin("BOR", "ORB", Data = BinaryOp.Or)]
        public static IOperand ArithmeticOp(
            ValueCall c, [Data] BinaryOp op, [ItemNotNull] [NotNull] params IOperand[] args)
        {
            Contract.Requires(args != null);
            Contract.Ensures(Contract.Result<IOperand>() != null);

            GetArithmeticInfo(op,
                out short initialValue,
                out Func<short, short, short> operation,
                out Func<ValueCall, IOperand, IOperand, IOperand> compileUnary);

            // can we evaluate the whole operation at compile time?
            if (args.Length > 0)
            {
                var folded = FoldConstantArithmetic(c.cc, initialValue, operation, args);
                if (folded != null)
                    return folded;
            }

            // nope, compile it
            switch (args.Length)
            {
                case 0:
                    return c.cc.Game.MakeOperand(initialValue);

                case 1:
                    return compileUnary(c, c.cc.Game.MakeOperand(initialValue), args[0]);

                case 2:
                    c.rb.EmitBinary(op, args[0], args[1], c.resultStorage);
                    return c.resultStorage;

                default:
                    c.rb.EmitBinary(op, args[0], args[1], c.rb.Stack);
                    for (int i = 2; i + 1 < args.Length; i++)
                    {
                        c.rb.EmitBinary(op, c.rb.Stack, args[i], c.rb.Stack);
                    }
                    c.rb.EmitBinary(op, c.rb.Stack, args[args.Length - 1], c.resultStorage);
                    return c.resultStorage;
            }
        }

        static void GetArithmeticInfo(BinaryOp op, out short initialValue,
            [NotNull] out Func<short, short, short> operation,
            [NotNull] out Func<ValueCall, IOperand, IOperand, IOperand> compileUnary)
        {
            Contract.Requires(operation != null);
            Contract.Requires(compileUnary != null);

            // a delegate implementing the actual arithmetic operation
            switch (op)
            {
                case BinaryOp.Add:
                    operation = (a, b) => (short)(a + b);
                    break;
                case BinaryOp.Sub:
                    operation = (a, b) => (short)(a - b);
                    break;
                case BinaryOp.Mul:
                    operation = (a, b) => (short)(a * b);
                    break;
                case BinaryOp.Div:
                    operation = (a, b) => (short)(a / b);
                    break;
                case BinaryOp.And:
                    operation = (a, b) => (short)(a & b);
                    break;
                case BinaryOp.Or:
                    operation = (a, b) => (short)(a | b);
                    break;
                default:
                    throw new ArgumentOutOfRangeException(nameof(op), op, null);
            }

            // the initial value, which is returned as-is if there are no args,
            // or possibly combined with the single arg if there's only one
            if (op == BinaryOp.Mul || op == BinaryOp.Div)
            {
                initialValue = 1;
            }
            else if (op == BinaryOp.And)
            {
                initialValue = -1;
            }
            else
            {
                initialValue = 0;
            }

            // another delegate describing how to combine the initial value
            // with the single arg in that case
            switch (op)
            {
                case BinaryOp.Add:
                case BinaryOp.Mul:
                case BinaryOp.And:
                case BinaryOp.Or:
                    // <+ X>, <* X>, <BAND X>, and <BOR X> all return X
                    compileUnary = (c, init, arg) => arg;
                    break;

                case BinaryOp.Sub:
                    // <- X> negates X
                    compileUnary = (c, init, arg) =>
                    {
                        c.rb.EmitUnary(UnaryOp.Neg, arg, c.resultStorage);
                        return c.resultStorage;
                    };
                    break;

                default:
                    // </ X> divides 1 by X
                    // presumably it sounded like a good idea at the time
                    compileUnary = (c, init, arg) =>
                    {
                        c.rb.EmitBinary(op, init, arg, c.resultStorage);
                        return c.resultStorage;
                    };
                    break;
            }
        }

        [CanBeNull]
        static IOperand FoldConstantArithmetic([NotNull] Compilation cc, short init, [NotNull] Func<short, short, short> op,
            [ItemNotNull] [NotNull] IOperand[] args)
        {
            Contract.Requires(cc != null);
            Contract.Requires(op != null);
            Contract.Requires(args != null);
            Contract.Requires(args.Length > 0);

            // make sure all args are constants
            foreach (var arg in args)
                if (!(arg is INumericOperand))
                    return null;

            if (args.Length == 1)
                return cc.Game.MakeOperand(op(init, (short)((INumericOperand)args[0]).Value));

            var value = (short)((INumericOperand)args[0]).Value;
            for (int i = 1; i < args.Length; i++)
                value = op(value, (short)((INumericOperand)args[i]).Value);

            return cc.Game.MakeOperand(value);
        }

        [Builtin("BAND", "ANDB")]
        public static void BinaryAndPredOp(PredCall c, [NotNull] IOperand left, [NotNull] IOperand right)
        {
            Contract.Requires(left != null);
            Contract.Requires(right != null);

            var nleft = left as INumericOperand;
            var nright = right as INumericOperand;

            // if both are constants, we can fully optimize
            if (nleft != null && nright != null)
            {
                var result = (short)(nleft.Value) & (short)(nright.Value);
                if ((result != 0) == c.polarity)
                    c.rb.Branch(c.label);

                return;
            }

            // if one is a constant power of two, we can use BTST
            if (nleft != null || nright != null)
            {
                IOperand variable;
                INumericOperand constant;

                if (nleft != null)
                {
                    constant = nleft;
                    variable = right;
                }
                else
                {
                    constant = nright;
                    variable = left;
                }

                if (constant.Value == 0)
                {
                    // always false
                    if (c.polarity == false)
                        c.rb.Branch(c.label);

                    return;
                }
                if ((constant.Value & (constant.Value - 1)) == 0)
                {
                    // power of two
                    c.rb.Branch(Condition.TestBits, variable, constant, c.label, c.polarity);
                    return;
                }
            }

            // otherwise use BAND and ZERO?
            c.rb.EmitBinary(BinaryOp.And, left, right, c.rb.Stack);
            c.rb.BranchIfZero(c.rb.Stack, c.label, !c.polarity);
        }

        [Builtin("REST", "ZREST")]
        [NotNull]
        public static IOperand RestOp(ValueCall c, [NotNull] IOperand left, [CanBeNull] IOperand right = null)
        {
            Contract.Requires(left != null);
            Contract.Ensures(Contract.Result<IOperand>() != null);

            // if left and right are constants, we can add them at assembly time
            if (left is IConstantOperand lconst)
            {
                if (right is IConstantOperand rconst)
                {
                    return lconst.Add(rconst);
                }
                else if (right == null)
                {
                    return lconst.Add(c.cc.Game.One);
                }
            }

            return ArithmeticOp(c, BinaryOp.Add, left, right ?? c.cc.Game.One);
        }

        [Builtin("BACK", "ZBACK")]
        [NotNull]
        public static IOperand BackOp(ValueCall c, [NotNull] IOperand left, [CanBeNull] IOperand right = null)
        {
            Contract.Requires(left != null);
            Contract.Ensures(Contract.Result<IOperand>() != null);

            return ArithmeticOp(c, BinaryOp.Sub, left, right ?? c.cc.Game.One);
        }

        [Builtin("CURSET", Data = BinaryOp.SetCursor, MinVersion = 4, MaxVersion = 5, HasSideEffect = true)]
        [Builtin("COLOR", Data = BinaryOp.SetColor, MinVersion = 5, HasSideEffect = true)]
        [Builtin("DIROUT", Data = BinaryOp.DirectOutput, HasSideEffect = true)]
        [Builtin("THROW", Data = BinaryOp.Throw, MinVersion = 5, HasSideEffect = true)]
        [Builtin("SCROLL", Data = BinaryOp.ScrollWindow, MinVersion = 6, HasSideEffect = true)]
        public static void BinaryVoidOp(
            VoidCall c, [Data] BinaryOp op, [NotNull] IOperand left, [NotNull] IOperand right)
        {
            Contract.Requires(left != null);
            Contract.Requires(right != null);

            c.rb.EmitBinary(op, left, right, null);
        }

        [Builtin("CURSET", MinVersion = 6, HasSideEffect = true)]
        public static void CursetVoidOp(VoidCall c, [NotNull] IOperand line, [CanBeNull] IOperand column = null,
            [CanBeNull] IOperand window = null)
        {
            Contract.Requires(line != null);

            if (window != null)
            {
                Debug.Assert(column != null);
                c.rb.EmitTernary(TernaryOp.SetCursor, line, column, window, null);
            }
            else
            {
                c.rb.EmitBinary(BinaryOp.SetCursor, line, column ?? c.cc.Game.Zero, null);
            }
        }

        [Builtin("GRTR?", "G?", Data = Condition.Greater)]
        [Builtin("LESS?", "L?", Data = Condition.Less)]
        [Builtin("BTST", Data = Condition.TestBits)]
        public static void BinaryPredOp(
            PredCall c, [Data] Condition cond, [NotNull] IOperand left, [NotNull] IOperand right)
        {
            Contract.Requires(left != null);
            Contract.Requires(right != null);

            if (left is INumericOperand nleft && right is INumericOperand nright)
            {
                bool branch;
                switch (cond)
                {
                    case Condition.Greater:
                        branch = nleft.Value > nright.Value;
                        break;
                    case Condition.Less:
                        branch = nleft.Value < nright.Value;
                        break;
                    case Condition.TestBits:
                        branch = (nleft.Value & nright.Value) == nright.Value;
                        break;
                    default:
                        throw UnhandledCaseException.FromEnum(cond);
                }

                if (branch == c.polarity)
                    c.rb.Branch(c.label);
            }
            else
            {
                c.rb.Branch(cond, left, right, c.label, c.polarity);
            }
        }

        [Builtin("MENU", MinVersion = 6, HasSideEffect = true)]
        public static void BinaryMenuOp(
            PredCall c, [NotNull] IOperand menuId, [Table][NotNull] IOperand table)
        {
            Contract.Requires(menuId != null);
            Contract.Requires(table != null);

            c.rb.Branch(Condition.MakeMenu, menuId, table, c.label, c.polarity);
        }

        [Builtin("L=?", Data = Condition.Greater)]
        [Builtin("G=?", Data = Condition.Less)]
        public static void NegatedBinaryPredOp(
            PredCall c, [Data] Condition cond, [NotNull] IOperand left, [NotNull] IOperand right)
        {
            Contract.Requires(left != null);
            Contract.Requires(right != null);

            BinaryPredOp(new PredCall(c.cc, c.rb, c.form, c.label, !c.polarity), cond, left, right);
        }

        [Builtin("DLESS?", Data = Condition.DecCheck, HasSideEffect = true)]
        [Builtin("IGRTR?", Data = Condition.IncCheck, HasSideEffect = true)]
        public static void BinaryVariablePredOp(
            PredCall c, [Data] Condition cond, [Variable(QuirksMode = QuirksMode.Both)][NotNull] IVariable left, [NotNull] IOperand right)
        {
            Contract.Requires(left != null);
            Contract.Requires(right != null);

            c.rb.Branch(cond, left, right, c.label, c.polarity);
        }

        [Builtin("PICINF", MinVersion = 6, HasSideEffect = true)]
        public static void PicinfPredOp(PredCall c, [NotNull] IOperand left, [Table][NotNull] IOperand right)
        {
            Contract.Requires(left != null);
            Contract.Requires(right != null);

            c.rb.Branch(Condition.PictureData, left, right, c.label, c.polarity);
        }

        [Builtin("DLESS?", Data = Condition.Less, HasSideEffect = true)]
        [Builtin("IGRTR?", Data = Condition.Greater, HasSideEffect = true)]
        public static void BinaryVariablePredOp(
            PredCall c, [Data] Condition cond, [Variable][NotNull] SoftGlobal left, [NotNull] IOperand right)
        {
            Contract.Requires(left != null);
            Contract.Requires(right != null);

            var offset = c.cc.Game.MakeOperand(left.Offset);

            // increment and leave the new value on the stack
            c.rb.EmitBinary(
                left.IsWord ? BinaryOp.GetWord : BinaryOp.GetByte,
                c.cc.SoftGlobalsTable,
                offset,
                c.rb.Stack);
            c.rb.EmitBinary(
                cond == Condition.Greater ? BinaryOp.Add : BinaryOp.Sub,
                c.rb.Stack,
                c.cc.Game.One,
                c.rb.Stack);
            c.rb.EmitUnary(
                UnaryOp.LoadIndirect,
                c.rb.Stack.Indirect,
                c.rb.Stack);
            c.rb.EmitTernary(
                left.IsWord ? TernaryOp.PutWord : TernaryOp.PutByte,
                c.cc.SoftGlobalsTable,
                offset,
                c.rb.Stack,
                null);

            // this works even if right == Stack, since the new value will be popped first
            c.rb.Branch(cond, c.rb.Stack, right, c.label, c.polarity);
        }

        [Builtin("GETP", Data = BinaryOp.GetProperty)]
        [Builtin("NEXTP", Data = BinaryOp.GetNextProp)]
        [NotNull]
        public static IOperand BinaryObjectValueOp(
            ValueCall c, [Data] BinaryOp op, [Object][NotNull] IOperand left, [NotNull] IOperand right)
        {
            Contract.Requires(left != null);
            Contract.Requires(right != null);
            Contract.Ensures(Contract.Result<IOperand>() != null);

            c.rb.EmitBinary(op, left, right, c.resultStorage);
            return c.resultStorage;
        }

        [Builtin("FSET", Data = BinaryOp.SetFlag, HasSideEffect = true)]
        [Builtin("FCLEAR", Data = BinaryOp.ClearFlag, HasSideEffect = true)]
        public static void BinaryObjectVoidOp(
            VoidCall c, [Data] BinaryOp op, [Object][NotNull] IOperand left, [NotNull] IOperand right)
        {
            Contract.Requires(left != null);
            Contract.Requires(right != null);

            c.rb.EmitBinary(op, left, right, null);
        }

        [Builtin("FSET?", Data = Condition.TestAttr)]
        public static void BinaryObjectPredOp(
            PredCall c, [Data] Condition cond, [Object][NotNull] IOperand left, [NotNull] IOperand right)
        {
            Contract.Requires(left != null);
            Contract.Requires(right != null);

            c.rb.Branch(cond, left, right, c.label, c.polarity);
        }

        [Builtin("IN?", Data = Condition.Inside)]
        public static void BinaryObjectObjectPredOp(
            PredCall c, [Data] Condition cond, [Object][NotNull] IOperand left, [Object][NotNull] IOperand right)
        {
            Contract.Requires(left != null);
            Contract.Requires(right != null);

            c.rb.Branch(cond, left, right, c.label, c.polarity);
        }

        [Builtin("MOVE", Data = BinaryOp.MoveObject, HasSideEffect = true)]
        public static void BinaryObjectObjectVoidOp(
            VoidCall c, [Data] BinaryOp op, [Object][NotNull] IOperand left, [Object][NotNull] IOperand right)
        {
            Contract.Requires(left != null);
            Contract.Requires(right != null);

            c.rb.EmitBinary(op, left, right, null);
        }

        [Builtin("GETPT", Data = BinaryOp.GetPropAddress)]
        [return: Table]
        [NotNull]
        public static IOperand BinaryObjectToTableValueOp(
            ValueCall c, [Data] BinaryOp op, [Object][NotNull] IOperand left, [NotNull] IOperand right)
        {
            Contract.Requires(left != null);
            Contract.Requires(right != null);
            Contract.Ensures(Contract.Result<IOperand>() != null);

            c.rb.EmitBinary(op, left, right, c.resultStorage);
            return c.resultStorage;
        }

        [Builtin("GET", "NTH", "ZGET", Data = BinaryOp.GetWord)]
        [Builtin("GETB", Data = BinaryOp.GetByte)]
        [NotNull]
        public static IOperand BinaryTableValueOp(
            ValueCall c, [Data] BinaryOp op, [Table][NotNull] IOperand left, [NotNull] IOperand right)
        {
            Contract.Requires(left != null);
            Contract.Requires(right != null);
            Contract.Ensures(Contract.Result<IOperand>() != null);

            c.rb.EmitBinary(op, left, right, c.resultStorage);
            return c.resultStorage;
        }

        #endregion

        #region Unary Opcodes

        [Builtin("BCOM", Data = UnaryOp.Not)]
        [Builtin("RANDOM", "ZRANDOM", Data = UnaryOp.Random, HasSideEffect = true)]
        [Builtin("FONT", Data = UnaryOp.SetFont, MinVersion = 5, HasSideEffect = true)]
        [Builtin("CHECKU", Data = UnaryOp.CheckUnicode, MinVersion = 5)]
        [NotNull]
        public static IOperand UnaryValueOp(
            ValueCall c, [Data] UnaryOp op, [NotNull] IOperand value)
        {
            Contract.Requires(value != null);
            Contract.Ensures(Contract.Result<IOperand>() != null);

            if (op == UnaryOp.Not && value is INumericOperand num)
            {
                return c.cc.Game.MakeOperand((short)(~num.Value));
            }

            c.rb.EmitUnary(op, value, c.resultStorage);
            return c.resultStorage;
        }

        [Builtin("DIRIN", Data = UnaryOp.DirectInput, HasSideEffect = true)]
        [Builtin("DIROUT", Data = UnaryOp.DirectOutput, HasSideEffect = true)]
        [Builtin("BUFOUT", "ZBUFOUT", Data = UnaryOp.OutputBuffer, MinVersion = 4, HasSideEffect = true)]
        [Builtin("HLIGHT", Data = UnaryOp.OutputStyle, HasSideEffect = true)]
        [Builtin("CLEAR", Data = UnaryOp.ClearWindow, MinVersion = 4, HasSideEffect = true)]
        [Builtin("SCREEN", Data = UnaryOp.SelectWindow, HasSideEffect = true)]
        [Builtin("SPLIT", Data = UnaryOp.SplitWindow, HasSideEffect = true)]
        [Builtin("ERASE", Data = UnaryOp.EraseLine, MinVersion = 4, HasSideEffect = true)]
        [Builtin("MOUSE-LIMIT", Data = UnaryOp.MouseWindow, MinVersion = 6, HasSideEffect = true)]
        public static void UnaryVoidOp(
            VoidCall c, [Data] UnaryOp op, [NotNull] IOperand value)
        {
            Contract.Requires(value != null);

            c.rb.EmitUnary(op, value, null);
        }

        [Builtin("ZERO?", "0?")]
        public static void ZeroPredOp(PredCall c, [NotNull] IOperand value)
        {
            Contract.Requires(value != null);

            if (value is INumericOperand num)
            {
                if ((num.Value == 0) == c.polarity)
                    c.rb.Branch(c.label);
            }
            else
            {
                c.rb.BranchIfZero(value, c.label, c.polarity);
            }
        }

        [Builtin("1?")]
        public static void OnePredOp(PredCall c, [NotNull] IOperand value)
        {
            Contract.Requires(value != null);

            if (value is INumericOperand num)
            {
                if ((num.Value == 1) == c.polarity)
                    c.rb.Branch(c.label);
            }
            else
            {
                c.rb.BranchIfEqual(value, c.cc.Game.One, c.label, c.polarity);
            }
        }

        [Builtin("LOC", Data = UnaryOp.GetParent)]
        [NotNull]
        public static IOperand UnaryObjectValueOp(
            ValueCall c, [Data] UnaryOp op, [Object][NotNull] IOperand obj)
        {
            Contract.Requires(obj != null);
            Contract.Ensures(Contract.Result<IOperand>() != null);

            c.rb.EmitUnary(op, obj, c.resultStorage);
            return c.resultStorage;
        }

        [Builtin("FIRST?", Data = false)]
        [Builtin("NEXT?", Data = true)]
        public static void UnaryObjectValuePredOp(
            ValuePredCall c, [Data] bool sibling, [Object][NotNull] IOperand obj)
        {
            Contract.Requires(obj != null);

            if (sibling)
                c.rb.EmitGetSibling(obj, c.resultStorage, c.label, c.polarity);
            else
                c.rb.EmitGetChild(obj, c.resultStorage, c.label, c.polarity);
        }

        [Builtin("PTSIZE", Data = UnaryOp.GetPropSize)]
        [NotNull]
        public static IOperand UnaryTableValueOp(
            ValueCall c, [Data] UnaryOp op, [Table][NotNull] IOperand value)
        {
            Contract.Requires(value != null);
            Contract.Ensures(Contract.Result<IOperand>() != null);

            c.rb.EmitUnary(op, value, c.resultStorage);
            return c.resultStorage;
        }

        [Builtin("REMOVE", "ZREMOVE", Data = UnaryOp.RemoveObject, HasSideEffect = true)]
        public static void UnaryObjectVoidOp(
            VoidCall c, [Data] UnaryOp op, [Object][NotNull] IOperand value)
        {
            Contract.Requires(value != null);

            c.rb.EmitUnary(op, value, null);
        }

        [Builtin("ASSIGNED?", Data = Condition.ArgProvided, MinVersion = 5)]
        public static void UnaryVariablePredOp(
            PredCall c, [Data] Condition cond, [Variable][NotNull] IVariable var)
        {
            Contract.Requires(var != null);

            c.rb.Branch(cond, var, null, c.label, c.polarity);
        }

        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Usage", "CA1801:ReviewUnusedParameters", MessageId = "var")]
        [Builtin("ASSIGNED?", MinVersion = 5)]
        public static void SoftGlobalAssignedOp(PredCall c, [Variable][NotNull] SoftGlobal var)
        {
            Contract.Requires(var != null);

            // globals are never "assigned" in this sense
            if (!c.polarity)
                c.rb.Branch(c.label);
        }

        [Builtin("CURGET", Data = UnaryOp.GetCursor, MinVersion = 4, HasSideEffect = true)]
        [Builtin("PICSET", Data = UnaryOp.PictureTable, MinVersion = 6, HasSideEffect = true)]
        [Builtin("MOUSE-INFO", Data = UnaryOp.ReadMouse, MinVersion = 6, HasSideEffect = true)]
        [Builtin("PRINTF", Data = UnaryOp.PrintForm, MinVersion = 6, HasSideEffect = true)]
        public static void UnaryTableVoidOp(
            VoidCall c, [Data] UnaryOp op, [Table][NotNull] IOperand value)
        {
            Contract.Requires(value != null);

            c.rb.EmitUnary(op, value, null);
        }

        #endregion

        #region Print Opcodes

        [Builtin("PRINT", "ZPRINT", Data = PrintOp.PackedAddr, HasSideEffect = true)]
        [Builtin("PRINTB", "ZPRINTB", Data = PrintOp.Address, HasSideEffect = true)]
        [Builtin("PRINTC", Data = PrintOp.Character, HasSideEffect = true)]
        [Builtin("PRINTD", Data = PrintOp.Object, HasSideEffect = true)]
        [Builtin("PRINTN", Data = PrintOp.Number, HasSideEffect = true)]
        [Builtin("PRINTU", Data = PrintOp.Unicode, HasSideEffect = true)]
        public static void UnaryPrintVoidOp(
            VoidCall c, [Data] PrintOp op, [NotNull] IOperand value)
        {
            Contract.Requires(value != null);

            c.rb.EmitPrint(op, value);
        }

        [Builtin("PRINTT", HasSideEffect = true)]
        public static void PrintTableOp(
            VoidCall c, [Table] [NotNull] IOperand table, [NotNull] IOperand width,
            [CanBeNull] IOperand height = null, [CanBeNull] IOperand skip = null)
        {
            Contract.Requires(table != null);
            Contract.Requires(width != null);
            Contract.Requires(height != null || skip == null);

            c.rb.EmitPrintTable(table, width, height, skip);
        }

        [Builtin("PRINTI", Data = false, HasSideEffect = true)]
        [Builtin("PRINTR", Data = true, HasSideEffect = true)]
        public static void UnaryPrintStringOp(
            VoidCall c, [Data] bool crlfRtrue, [NotNull] string text)
        {
            Contract.Requires(text != null);

            c.rb.EmitPrint(text, crlfRtrue);
        }

        [Builtin("CRLF", "ZCRLF", HasSideEffect = true)]
        public static void CrlfVoidOp(VoidCall c)
        {
            c.rb.EmitPrintNewLine();
        }

        #endregion

        #region Variable Opcodes

        /// <exception cref="CompilerError">The syntax is incorrect, or an error occurred while compiling a subexpression.</exception>
        [Builtin("SET", HasSideEffect = true)]
        [NotNull]
        public static IOperand SetValueOp(
            ValueCall c, [Variable(QuirksMode = QuirksMode.Local)] [NotNull] IVariable dest, [NotNull] ZilObject value)
        {
            Contract.Requires(dest != null);
            Contract.Requires(value != null);
            Contract.Ensures(Contract.Result<IOperand>() != null);

            // in value context, we need to be able to return the newly set value,
            // so dest is IVariable. this means <SET <fancy-expression> value> isn't
            // supported in value context.

            /* TODO: it could be supported if we wanted to get complicated:
             * <SET expr value> ->
             *   evaluate expr >TMP
             *   evaluate value >STACK
             *   PUSH 'STACK        ;duplicates top value
             *   SET TMP,STACK      ;performs indirect store
             *   return STACK       ;value is left on stack
             */

            var storage = c.cc.CompileAsOperand(c.rb, value, c.form.SourceLine, dest);
            if (storage != dest)
                c.rb.EmitStore(dest, storage);
            return dest;
        }

        /// <exception cref="CompilerError">The syntax is incorrect, or an error occurred while compiling a subexpression.</exception>
        [Builtin("SET", HasSideEffect = true)]
        [NotNull]
        public static IOperand SetValueOp(
            ValueCall c, [Variable(QuirksMode = QuirksMode.Local)] [NotNull] SoftGlobal dest, [NotNull] ZilObject value)
        {
            Contract.Requires(dest != null);
            Contract.Requires(value != null);
            Contract.Ensures(Contract.Result<IOperand>() != null);

            var storage = c.cc.CompileAsOperand(c.rb, value, c.form.SourceLine, c.rb.Stack);

            if (storage == c.rb.Stack)
            {
                // duplicate the value
                c.rb.EmitUnary(UnaryOp.LoadIndirect, c.rb.Stack, c.rb.Stack);
            }

            c.rb.EmitTernary(
                dest.IsWord ? TernaryOp.PutWord : TernaryOp.PutByte,
                c.cc.SoftGlobalsTable,
                c.cc.Game.MakeOperand(dest.Offset),
                storage,
                null);

            return storage;
        }

        /// <exception cref="CompilerError">The syntax is incorrect, or an error occurred while compiling a subexpression.</exception>
        [Builtin("SETG", HasSideEffect = true)]
        [NotNull]
        public static IOperand SetgValueOp(
            ValueCall c, [Variable(QuirksMode = QuirksMode.Global)][NotNull] IVariable dest, [NotNull] ZilObject value)
        {
            Contract.Requires(dest != null);
            Contract.Requires(value != null);
            Contract.Ensures(Contract.Result<IOperand>() != null);

            return SetValueOp(c, dest, value);
        }

        /// <exception cref="CompilerError">The syntax is incorrect, or an error occurred while compiling a subexpression.</exception>
        [Builtin("SETG", HasSideEffect = true)]
        [NotNull]
        public static IOperand SetgValueOp(
            ValueCall c, [Variable(QuirksMode = QuirksMode.Global)][NotNull] SoftGlobal dest, [NotNull] ZilObject value)
        {
            Contract.Requires(dest != null);
            Contract.Requires(value != null);
            Contract.Ensures(Contract.Result<IOperand>() != null);

            return SetValueOp(c, dest, value);
        }

        /// <exception cref="CompilerError">Local variables are not allowed here.</exception>
        [Builtin("SET")]
        public static void SetVoidOp(
            VoidCall c, [Variable(QuirksMode = QuirksMode.Local)][NotNull] IOperand dest, [NotNull] ZilObject value)
        {
            Contract.Requires(dest != null);
            Contract.Requires(value != null);

            // in void context, we don't need to return the newly set value, so we
            // can support <SET <fancy-expression> value>.

            if (dest is IIndirectOperand ind)
            {
                var destVar = ind.Variable;
                var storage = c.cc.CompileAsOperand(c.rb, value, c.form.SourceLine, destVar);
                if (storage != destVar)
                    c.rb.EmitStore(destVar, storage);
            }
            else
            {
                using (var operands = c.cc.CompileOperands(c.rb, c.form.SourceLine, value))
                {
                    if (dest == c.rb.Stack && operands[0] == c.rb.Stack)
                    {
                        var tempAtom = ZilAtom.Parse("?TMP", c.cc.Context);
                        c.cc.PushInnerLocal(c.rb, tempAtom);
                        try
                        {
                            var tempLocal = c.cc.Locals[tempAtom];
                            c.rb.EmitStore(tempLocal, operands[0]);
                            c.rb.EmitBinary(BinaryOp.StoreIndirect, dest, tempLocal, null);
                        }
                        finally
                        {
                            c.cc.PopInnerLocal(tempAtom);
                        }
                    }
                    else
                    {
                        c.rb.EmitBinary(BinaryOp.StoreIndirect, dest, operands[0], null);
                    }
                }
            }
        }

        /// <exception cref="CompilerError">The syntax is incorrect, or an error occurred while compiling a subexpression.</exception>
        [Builtin("SET")]
        public static void SetVoidOp(
            VoidCall c, [Variable(QuirksMode = QuirksMode.Local)][NotNull] SoftGlobal dest, [NotNull] ZilObject value)
        {
            Contract.Requires(dest != null);
            Contract.Requires(value != null);

            Contract.Assume(c.cc.SoftGlobalsTable != null);

            c.rb.EmitTernary(
                dest.IsWord ? TernaryOp.PutWord : TernaryOp.PutByte,
                c.cc.SoftGlobalsTable,
                c.cc.Game.MakeOperand(dest.Offset),
                c.cc.CompileAsOperand(c.rb, value, c.form.SourceLine),
                null);
        }

        /// <exception cref="CompilerError">Local variables are not allowed here.</exception>
        [Builtin("SETG")]
        public static void SetgVoidOp(
            VoidCall c, [Variable(QuirksMode = QuirksMode.Global)][NotNull] IOperand dest, [NotNull] ZilObject value)
        {
            Contract.Requires(dest != null);
            Contract.Requires(value != null);

            SetVoidOp(c, dest, value);
        }

        /// <exception cref="CompilerError">The syntax is incorrect, or an error occurred while compiling a subexpression.</exception>
        [Builtin("SETG")]
        public static void SetgVoidOp(
            VoidCall c, [Variable(QuirksMode = QuirksMode.Global)][NotNull] SoftGlobal dest, [NotNull] ZilObject value)
        {
            Contract.Requires(dest != null);
            Contract.Requires(value != null);

            SetVoidOp(c, dest, value);
        }

        /// <exception cref="CompilerError">The syntax is incorrect, or an error occurred while compiling a subexpression.</exception>
        [Builtin("SET", HasSideEffect = true)]
        public static void SetPredOp(
            PredCall c, [Variable(QuirksMode = QuirksMode.Local)][NotNull] IVariable dest, [NotNull] ZilObject value)
        {
            Contract.Requires(dest != null);
            Contract.Requires(value != null);

            // see note in SetValueOp regarding dest being IVariable
            c.cc.CompileAsOperandWithBranch(c.rb, value, dest, c.label, c.polarity);
        }

        /// <exception cref="CompilerError">The syntax is incorrect, or an error occurred while compiling a subexpression.</exception>
        [Builtin("SETG", HasSideEffect = true)]
        public static void SetgPredOp(
            PredCall c, [Variable(QuirksMode = QuirksMode.Global)][NotNull] IVariable dest, [NotNull] ZilObject value)
        {
            Contract.Requires(dest != null);
            Contract.Requires(value != null);

            SetPredOp(c, dest, value);
        }

        [Builtin("INC", Data = BinaryOp.Add, HasSideEffect = true)]
        [Builtin("DEC", Data = BinaryOp.Sub, HasSideEffect = true)]
        [NotNull]
        public static IOperand IncValueOp(ValueCall c, [Data] BinaryOp op,
            [Variable(QuirksMode = QuirksMode.Both)][NotNull] IVariable victim)
        {
            Contract.Requires(victim != null);
            Contract.Ensures(Contract.Result<IOperand>() != null);

            c.rb.EmitBinary(op, victim, c.cc.Game.One, victim);
            return victim;
        }

        [Builtin("INC", Data = BinaryOp.Add, HasSideEffect = true)]
        [Builtin("DEC", Data = BinaryOp.Sub, HasSideEffect = true)]
        [NotNull]
        public static IOperand IncValueOp(ValueCall c, [Data] BinaryOp op,
            [Variable(QuirksMode = QuirksMode.Both)][NotNull] SoftGlobal victim)
        {
            Contract.Requires(victim != null);
            Contract.Ensures(Contract.Result<IOperand>() != null);

            var offset = c.cc.Game.MakeOperand(victim.Offset);
            c.rb.EmitBinary(
                victim.IsWord ? BinaryOp.GetWord : BinaryOp.GetByte,
                c.cc.SoftGlobalsTable,
                offset,
                c.rb.Stack);
            c.rb.EmitBinary(op, c.rb.Stack, c.cc.Game.One, c.rb.Stack);
            c.rb.EmitUnary(UnaryOp.LoadIndirect, c.rb.Stack.Indirect, c.rb.Stack);
            c.rb.EmitTernary(
                victim.IsWord ? TernaryOp.PutWord : TernaryOp.PutByte,
                c.cc.SoftGlobalsTable,
                offset,
                c.rb.Stack,
                null);
            return c.rb.Stack;
        }

        [Builtin("INC", Data = BinaryOp.Add, HasSideEffect = true)]
        [Builtin("DEC", Data = BinaryOp.Sub, HasSideEffect = true)]
        public static void IncVoidOp(VoidCall c, [Data] BinaryOp op,
            [Variable(QuirksMode = QuirksMode.Both)][NotNull] IVariable victim)
        {
            Contract.Requires(victim != null);

            c.rb.EmitBinary(op, victim, c.cc.Game.One, victim);
        }

        [Builtin("INC", Data = BinaryOp.Add, HasSideEffect = true)]
        [Builtin("DEC", Data = BinaryOp.Sub, HasSideEffect = true)]
        public static void IncVoidOp(VoidCall c, [Data] BinaryOp op,
            [Variable(QuirksMode = QuirksMode.Both)][NotNull] SoftGlobal victim)
        {
            Contract.Requires(victim != null);

            var offset = c.cc.Game.MakeOperand(victim.Offset);
            c.rb.EmitBinary(
                victim.IsWord ? BinaryOp.GetWord : BinaryOp.GetByte,
                c.cc.SoftGlobalsTable,
                offset,
                c.rb.Stack);
            c.rb.EmitBinary(op, c.rb.Stack, c.cc.Game.One, c.rb.Stack);
            c.rb.EmitTernary(
                victim.IsWord ? TernaryOp.PutWord : TernaryOp.PutByte,
                c.cc.SoftGlobalsTable,
                offset,
                c.rb.Stack,
                null);
        }

        [Builtin("PUSH", HasSideEffect = true)]
        public static void PushVoidOp(VoidCall c, [NotNull] IOperand value)
        {
            Contract.Requires(value != null);

            c.rb.EmitStore(c.rb.Stack, value);
        }

        [Builtin("XPUSH", MinVersion = 6, HasSideEffect = true)]
        public static void XpushPredOp(PredCall c, [NotNull] IOperand value, [NotNull] IOperand stack)
        {
            Contract.Requires(value != null);
            Contract.Requires(stack != null);

            c.rb.EmitPushUserStack(value, stack, c.label, c.polarity);
        }

        [NotNull]
        [Builtin("POP", MinVersion = 6, HasSideEffect = true)]
        public static IOperand PopValueOp(ValueCall c, [CanBeNull] IOperand stack = null)
        {
            Contract.Ensures(Contract.Result<IOperand>() != null);
            if (stack == null)
                c.rb.EmitStore(c.resultStorage, c.rb.Stack);
            else
                c.rb.EmitUnary(UnaryOp.PopUserStack, stack, c.resultStorage);

            return c.resultStorage;
        }

        [Builtin("FSTACK", MinVersion = 6, HasSideEffect = true)]
        public static void FstackVoidOp(VoidCall c, [NotNull] IOperand count, [CanBeNull] IOperand stack = null)
        {
            Contract.Requires(count != null);
            if (stack == null)
                c.rb.EmitUnary(UnaryOp.FlushStack, count, null);
            else
                c.rb.EmitBinary(BinaryOp.FlushUserStack, count, stack, null);
        }

        [Builtin("VALUE", Priority = 1)]
        public static IOperand ValueOp_Variable(ValueCall c, [Variable] IVariable var)
        {
            if (var == c.rb.Stack)
            {
                c.rb.EmitUnary(UnaryOp.LoadIndirect, var.Indirect, c.resultStorage);
                return c.resultStorage;
            }
            else
            {
                return var;
            }
        }

        [Builtin("VALUE", Priority = 2)]
        [NotNull]
        public static IOperand ValueOp_Operand(ValueCall c, [Variable][NotNull] IOperand value)
        {
            Contract.Requires(value != null);
            Contract.Ensures(Contract.Result<IOperand>() != null);

            c.rb.EmitUnary(UnaryOp.LoadIndirect, value, c.resultStorage);
            return c.resultStorage;
        }

        #endregion

        #region Nullary Opcodes

        [Builtin("CATCH", Data = NullaryOp.Catch, MinVersion = 5)]
        [Builtin("ISAVE", Data = NullaryOp.SaveUndo, HasSideEffect = true, MinVersion = 5)]
        [Builtin("IRESTORE", Data = NullaryOp.RestoreUndo, HasSideEffect = true, MinVersion = 5)]
        [NotNull]
        public static IOperand NullaryValueOp(ValueCall c, [Data] NullaryOp op)
        {
            Contract.Ensures(Contract.Result<IOperand>() != null);

            c.rb.EmitNullary(op, c.resultStorage);
            return c.resultStorage;
        }

        [Builtin("ORIGINAL?", Data = Condition.Original, MinVersion = 5)]
        [Builtin("VERIFY", Data = Condition.Verify)]
        public static void NullaryPredOp(PredCall c, [Data] Condition cond)
        {
            c.rb.Branch(cond, null, null, c.label, c.polarity);
        }

        [Builtin("USL", Data = NullaryOp.ShowStatus, MinVersion = 3, MaxVersion = 3, HasSideEffect = true)]
        public static void NullaryVoidOp(VoidCall c, [Data] NullaryOp op)
        {
            c.rb.EmitNullary(op, null);
        }

        [Builtin("RTRUE", Data = 1, HasSideEffect = true)]
        [Builtin("RFALSE", Data = 0, HasSideEffect = true)]
        [Builtin("RFATAL", Data = 2, HasSideEffect = true)]
        [Builtin("RSTACK", Data = -1, HasSideEffect = true)]
        public static void NullaryReturnOp(VoidCall c, [Data] int what)
        {
            var operand =
                (what == 1) ? c.cc.Game.One :
                (what == 0) ? c.cc.Game.Zero :
                (what == 2) ? (IOperand)c.cc.Game.MakeOperand(2) :
                c.rb.Stack;

            c.rb.Return(operand);
        }

        [Builtin("RESTART", HasSideEffect = true)]
        public static void RestartOp(VoidCall c)
        {
            c.rb.EmitRestart();
        }

        [Builtin("QUIT", HasSideEffect = true)]
        public static void QuitOp(VoidCall c)
        {
            c.rb.EmitQuit();
        }

        #endregion

        #region Input Opcodes

        [Builtin("READ", "ZREAD", MaxVersion = 3, HasSideEffect = true)]
        public static void ReadOp_V3(VoidCall c, [NotNull] IOperand text, IOperand parse)
        {
            Contract.Requires(text != null);

            c.rb.EmitRead(text, parse, null, null, null);
        }

        [Builtin("READ", "ZREAD", MinVersion = 4, MaxVersion = 4, HasSideEffect = true)]
        public static void ReadOp_V4(VoidCall c, [NotNull] IOperand text, [NotNull] IOperand parse,
            [CanBeNull] IOperand time = null, [CanBeNull] [Routine] IOperand routine = null)
        {
            Contract.Requires(text != null);
            Contract.Requires(parse != null);
            Contract.Requires(time != null || routine == null);

            c.rb.EmitRead(text, parse, time, routine, null);
        }

        [Builtin("READ", "ZREAD", MinVersion = 5, HasSideEffect = true)]
        [NotNull]
        public static IOperand ReadOp_V5(ValueCall c, [NotNull] IOperand text,
            [CanBeNull] IOperand parse = null, [CanBeNull] IOperand time = null,
            [CanBeNull] [Routine] IOperand routine = null)
        {
            Contract.Requires(text != null);
            Contract.Requires(parse != null || time == null);
            Contract.Requires(time != null || routine == null);
            Contract.Ensures(Contract.Result<IOperand>() != null);

            c.rb.EmitRead(text, parse, time, routine, c.resultStorage);
            return c.resultStorage;
        }

        [NotNull]
        [Builtin("INPUT", MinVersion = 4, HasSideEffect = true)]
        public static IOperand InputOp(ValueCall c, [NotNull] IOperand dummy,
            [CanBeNull] IOperand interval = null, [CanBeNull] [Routine] IOperand routine = null)
        {
            Contract.Requires(dummy != null);
            Contract.Ensures(Contract.Result<IOperand>() != null);

            Debug.Assert(c.form.Rest != null);
            if (c.form.Rest.First is ZilFix fix && fix.Value != 1)
            {
                return c.HandleMessage(
                    CompilerMessages._0_Argument_1_2,
                    "INPUT",
                    1,
                    "argument must be '1'");
            }

            c.rb.EmitReadChar(interval, routine, c.resultStorage);

            if (dummy == c.rb.Stack)
                c.rb.EmitPopStack();

            return c.resultStorage;
        }

        #endregion

        #region Sound Opcodes

        [Builtin("SOUND", MaxVersion = 4, HasSideEffect = true)]
        public static void SoundOp_V3(VoidCall c, [NotNull] IOperand number,
            [CanBeNull] IOperand effect = null, [CanBeNull] IOperand volume = null)
        {
            Contract.Requires(number != null);
            Contract.Requires(effect != null || volume == null);

            c.rb.EmitPlaySound(number, effect, volume, null);
        }

        [Builtin("SOUND", MinVersion = 5, HasSideEffect = true)]
        public static void SoundOp_V5(VoidCall c, [NotNull] IOperand number,
            [CanBeNull] IOperand effect = null, [CanBeNull] IOperand volume = null,
            [CanBeNull] [Routine] IOperand routine = null)
        {
            Contract.Requires(number != null);
            Contract.Requires(effect != null || volume == null);
            Contract.Requires(volume != null || routine == null);

            c.rb.EmitPlaySound(number, effect, volume, routine);
        }

        #endregion

        #region Vocab Opcodes

        [Builtin("ZWSTR", MinVersion = 5, HasSideEffect = true)]
        public static void EncodeTextOp(VoidCall c,
            [Table][NotNull] IOperand src, [NotNull] IOperand length,
            [NotNull] IOperand srcOffset, [Table][NotNull] IOperand dest)
        {
            Contract.Requires(src != null);
            Contract.Requires(length != null);
            Contract.Requires(srcOffset != null);
            Contract.Requires(dest != null);

            c.rb.EmitEncodeText(src, length, srcOffset, dest);
        }

        [Builtin("LEX", MinVersion = 5, HasSideEffect = true)]
        public static void LexOp(VoidCall c,
            [Table][NotNull] IOperand text, [Table][NotNull] IOperand parse,
            [CanBeNull] [Table] IOperand dictionary = null, [CanBeNull] IOperand flag = null)
        {
            Contract.Requires(text != null);
            Contract.Requires(parse != null);
            Contract.Requires(dictionary != null || flag == null);

            c.rb.EmitTokenize(text, parse, dictionary, flag);
        }

        #endregion

        #region Save/Restore Opcodes

        /// <exception cref="NotSupportedException">Wrong Z-machine version for this form of the opcode.</exception>
        [Builtin("RESTORE", "ZRESTORE", MaxVersion = 3, HasSideEffect = true)]
        public static void RestoreOp_V3(PredCall c)
        {
            if (c.rb.HasBranchSave)
            {
                c.rb.EmitRestore(c.label, c.polarity);
            }
            else
            {
                throw new NotSupportedException($"{nameof(RestoreOp_V3)} without {nameof(c.rb.HasBranchSave)}");
            }
        }

        /// <exception cref="NotSupportedException">Wrong Z-machine version for this form of the opcode.</exception>
        [NotNull]
        [Builtin("RESTORE", "ZRESTORE", MinVersion = 4, HasSideEffect = true)]
        public static IOperand RestoreOp_V4(ValueCall c)
        {
            Contract.Ensures(Contract.Result<IOperand>() != null);
            if (c.rb.HasStoreSave)
            {
                c.rb.EmitRestore(c.resultStorage);
                return c.resultStorage;
            }
            throw new NotSupportedException($"{nameof(RestoreOp_V4)} without {nameof(c.rb.HasStoreSave)}");
        }

        /// <exception cref="NotSupportedException">Wrong Z-machine version for this form of the opcode.</exception>
        [Builtin("RESTORE", "ZRESTORE", MinVersion = 5, HasSideEffect = true)]
        [NotNull]
        public static IOperand RestoreOp_V5(ValueCall c, [Table][NotNull] IOperand table,
            [NotNull] IOperand bytes, [Table][NotNull] IOperand name)
        {
            Contract.Requires(table != null);
            Contract.Requires(bytes != null);
            Contract.Requires(name != null);
            Contract.Ensures(Contract.Result<IOperand>() != null);

            if (c.rb.HasExtendedSave)
            {
                c.rb.EmitRestore(table, bytes, name, c.resultStorage);
                return c.resultStorage;
            }
            throw new NotSupportedException($"{nameof(RestoreOp_V5)} without {nameof(c.rb.HasExtendedSave)}");
        }

        /// <exception cref="NotSupportedException">Wrong Z-machine version for this form of the opcode.</exception>
        [Builtin("SAVE", "ZSAVE", MaxVersion = 3, HasSideEffect = true)]
        public static void SaveOp_V3(PredCall c)
        {
            if (c.rb.HasBranchSave)
            {
                c.rb.EmitSave(c.label, c.polarity);
            }
            else
            {
                throw new NotSupportedException($"{nameof(SaveOp_V3)} without {nameof(c.rb.HasBranchSave)}");
            }
        }

        /// <exception cref="NotSupportedException">Wrong Z-machine version for this form of the opcode.</exception>
        [NotNull]
        [Builtin("SAVE", "ZSAVE", MinVersion = 4, HasSideEffect = true)]
        public static IOperand SaveOp_V4(ValueCall c)
        {
            Contract.Ensures(Contract.Result<IOperand>() != null);
            if (c.rb.HasStoreSave)
            {
                c.rb.EmitSave(c.resultStorage);
                return c.resultStorage;
            }
            throw new NotSupportedException($"{nameof(SaveOp_V4)} without {nameof(c.rb.HasStoreSave)}");
        }

        /// <exception cref="NotSupportedException">Wrong Z-machine version for this form of the opcode.</exception>
        [Builtin("SAVE", "ZSAVE", MinVersion = 5, HasSideEffect = true)]
        [NotNull]
        public static IOperand SaveOp_V5(ValueCall c, [Table][NotNull] IOperand table,
            [NotNull] IOperand bytes, [Table][NotNull] IOperand name)
        {
            Contract.Requires(table != null);
            Contract.Requires(bytes != null);
            Contract.Requires(name != null);
            Contract.Ensures(Contract.Result<IOperand>() != null);

            if (c.rb.HasExtendedSave)
            {
                c.rb.EmitSave(table, bytes, name, c.resultStorage);
                return c.resultStorage;
            }
            throw new NotSupportedException($"{nameof(SaveOp_V5)} without {nameof(c.rb.HasExtendedSave)}");
        }

        #endregion

        #region Routine Opcodes/Builtins

        /// <exception cref="CompilerError">The syntax is incorrect, or an error occurred while compiling a subexpression.</exception>
        [Builtin("RETURN", HasSideEffect = true)]
        public static void ReturnOp(VoidCall c, [CanBeNull] ZilObject expr = null, Block block = null)
        {
            var origBlock = block;

            if (block == null)
            {
                block = c.cc.Blocks.First(b => (b.Flags & BlockFlags.ExplicitOnly) == 0);
            }

            IOperand value;

            if (block.ReturnLabel == null)
            {
                // return from routine
                value = expr != null
                    ? c.cc.CompileAsOperand(c.rb, expr, c.form.SourceLine)
                    : c.cc.Game.One;
                c.rb.Return(value);
            }
            else
            {
                // return from enclosing PROG/REPEAT
                if ((block.Flags & BlockFlags.WantResult) != 0)
                {
                    var resultStorage = block.ResultStorage ?? c.rb.Stack;
                    if (expr == null)
                    {
                        c.rb.EmitStore(resultStorage, c.cc.Game.One);
                    }
                    else
                    {
                        value = c.cc.CompileAsOperand(
                            c.rb,
                            expr,
                            c.form.SourceLine,
                            resultStorage);
                        if (value != resultStorage)
                            c.rb.EmitStore(resultStorage, value);
                    }
                }
                else if (expr != null)
                {
                    value = c.cc.CompileAsOperand(c.rb, expr, c.form.SourceLine);
                    if (value == c.rb.Stack)
                        c.rb.EmitPopStack();

                    // warn that the value was ignored, unless this looks like a case where
                    // a dummy value was required in order to return from a named block
                    if (origBlock == null ||
                        (value != c.cc.Game.Zero && value != c.cc.Game.One))
                    {
                        c.HandleMessage(CompilerMessages.RETURN_Value_Ignored_Block_Is_In_Void_Context);
                    }
                }

                block.Flags |= BlockFlags.Returned;
                c.rb.Branch(block.ReturnLabel);
            }
        }

        [Builtin("AGAIN", HasSideEffect = true)]
        public static void AgainOp(VoidCall c, Block block = null)
        {
            if (block == null)
            {
                block = c.cc.Blocks.First(b => (b.Flags & BlockFlags.ExplicitOnly) == 0);
            }

            if (block.AgainLabel != null)
            {
                c.rb.Branch(block.AgainLabel);
            }
            else
            {
                c.HandleMessage(CompilerMessages.AGAIN_Requires_A_PROGREPEAT_Block_Or_Routine);
            }
        }

        [Builtin("APPLY", "CALL", "ZAPPLY", HasSideEffect = true)]
        [NotNull]
        public static IOperand CallValueOp(ValueCall c,
            [Routine][NotNull] IOperand routine, [NotNull] params IOperand[] args)
        {
            Contract.Requires(routine != null);
            Contract.Requires(args != null);
            Contract.Ensures(Contract.Result<IOperand>() != null);

            if (args.Length > c.cc.Game.MaxCallArguments)
            {
                return c.HandleMessage(
                    CompilerMessages.Too_Many_Call_Arguments_Only_0_Allowed_In_V1,
                    c.cc.Game.MaxCallArguments, c.cc.Context.ZEnvironment.ZVersion);
            }

            c.rb.EmitCall(routine, args, c.resultStorage);
            return c.resultStorage;
        }

        [Builtin("APPLY", "CALL", MinVersion = 5, HasSideEffect = true)]
        public static void CallVoidOp(VoidCall c,
            [Routine][NotNull] IOperand routine, [NotNull] params IOperand[] args)
        {
            Contract.Requires(routine != null);
            Contract.Requires(args != null);

            if (args.Length > c.cc.Game.MaxCallArguments)
            {
                c.HandleMessage(
                    CompilerMessages.Too_Many_Call_Arguments_Only_0_Allowed_In_V1,
                    c.cc.Game.MaxCallArguments, c.cc.Context.ZEnvironment.ZVersion);
                return;
            }

            c.rb.EmitCall(routine, args, null);
        }

        #endregion

        #region Table Opcodes/Builtins

        [Builtin("INTBL?", MinVersion = 4, MaxVersion = 4)]
        [return: Table]
        public static void IntblValuePredOp_V4(ValuePredCall c,
            [NotNull] IOperand value, [Table][NotNull] IOperand table, [NotNull] IOperand length)
        {
            Contract.Requires(value != null);
            Contract.Requires(table != null);
            Contract.Requires(length != null);

            c.rb.EmitScanTable(value, table, length, null, c.resultStorage, c.label, c.polarity);
        }

        [Builtin("INTBL?", MinVersion = 5)]
        [return: Table]
        public static void IntblValuePredOp_V5(ValuePredCall c,
            [NotNull] IOperand value, [Table][NotNull] IOperand table, [NotNull] IOperand length, [CanBeNull] IOperand form = null)
        {
            Contract.Requires(value != null);
            Contract.Requires(table != null);
            Contract.Requires(length != null);

            c.rb.EmitScanTable(value, table, length, form, c.resultStorage, c.label, c.polarity);
        }

        static bool TryGetLowCoreField([NotNull] string name, [NotNull] Context ctx, [NotNull] ISourceLine src, [NotNull] ZilObject fieldSpec, bool writing,
            out int offset, out LowCoreFlags flags, out int minVersion)
        {
            Contract.Requires(name != null);
            Contract.Requires(ctx != null);
            Contract.Requires(src != null);
            Contract.Requires(fieldSpec != null);
            Contract.Ensures(Contract.ValueAtReturn(out offset) >= 0);
            Contract.Ensures(Contract.ValueAtReturn(out minVersion) >= 0);
            Contract.Ensures(Contract.ValueAtReturn(out minVersion) >= 1 || Contract.Result<bool>() == false);

            offset = 0;
            flags = LowCoreFlags.None;
            minVersion = 0;

            if (fieldSpec is ZilAtom atom)
            {
                var field = LowCoreField.Get(atom);
                if (field == null)
                {
                    ctx.HandleError(new CompilerError(src, CompilerMessages.Unrecognized_0_1, "header field", atom));
                    return false;
                }
                if (!ctx.ZEnvironment.VersionMatches(field.MinVersion, field.MaxVersion))
                {
                    ctx.HandleError(new CompilerError(src, CompilerMessages._0_Field_1_Is_Not_Supported_In_This_Zmachine_Version, name, atom));
                    return false;
                }
                if (writing && (field.Flags & LowCoreFlags.Writable) == 0)
                {
                    ctx.HandleError(new CompilerError(src, CompilerMessages._0_Field_1_Is_Not_Writable, name, atom));
                    return false;
                }

                offset = field.Offset;
                flags = field.Flags;
                minVersion = field.MinVersion;
                return true;
            }

            if (fieldSpec is ZilList list)
            {
                if (list.GetLength(2) != 2)
                {
                    ctx.HandleError(new CompilerError(src, CompilerMessages._0_List_Must_Have_2_Elements, name));
                    return false;
                }

                atom = list.First as ZilAtom;
                if (atom == null)
                {
                    ctx.HandleError(new CompilerError(src, CompilerMessages._0_First_List_Element_Must_Be_An_Atom, name));
                    return false;
                }

                Debug.Assert(list.Rest != null);
                if (!(list.Rest.First is ZilFix fix) || fix.Value < 0 || fix.Value > 1)
                {
                    ctx.HandleError(new CompilerError(src, CompilerMessages._0_Second_List_Element_Must_Be_0_Or_1, name));
                    return false;
                }

                var field = LowCoreField.Get(atom);
                if (field == null)
                {
                    ctx.HandleError(new CompilerError(src, CompilerMessages.Unrecognized_0_1, "header field", atom));
                    return false;
                }
                if (!ctx.ZEnvironment.VersionMatches(field.MinVersion, field.MaxVersion))
                {
                    ctx.HandleError(new CompilerError(src, CompilerMessages._0_Field_1_Is_Not_Supported_In_This_Zmachine_Version, name, atom));
                    return false;
                }
                if ((field.Flags & LowCoreFlags.Byte) != 0)
                {
                    ctx.HandleError(new CompilerError(src, CompilerMessages._0_Field_1_Is_Not_A_Word_Field, name, atom));
                    return false;
                }
                if (writing && (field.Flags & LowCoreFlags.Writable) == 0)
                {
                    ctx.HandleError(new CompilerError(src, CompilerMessages._0_Field_1_Is_Not_Writable, name, atom));
                    return false;
                }

                offset = field.Offset * 2 + fix.Value;
                flags = field.Flags | LowCoreFlags.Byte;
                minVersion = field.MinVersion;
                return true;
            }

            ctx.HandleError(new CompilerError(src, CompilerMessages._0_Argument_1_2, name, 1, "argument must be an atom or list"));
            return false;
        }

        [Builtin("LOWCORE")]
        [NotNull]
        public static IOperand LowCoreReadOp(ValueCall c, [NotNull] ZilObject fieldSpec)
        {
            Contract.Requires(fieldSpec != null);
            Contract.Ensures(Contract.Result<IOperand>() != null);

            if (!TryGetLowCoreField("LOWCORE", c.cc.Context, c.form.SourceLine, fieldSpec, false, out var offset, out var flags, out _))
                return c.cc.Game.Zero;

            var binaryOp = ((flags & LowCoreFlags.Byte) != 0) ? BinaryOp.GetByte : BinaryOp.GetWord;
            if ((flags & LowCoreFlags.Extended) != 0)
            {
                var wordOffset = ((flags & LowCoreFlags.Byte) != 0) ? (offset + 1) / 2 : offset;
                c.cc.Context.ZEnvironment.EnsureMinimumHeaderExtension(wordOffset);
                c.rb.EmitBinary(BinaryOp.GetWord, c.cc.Game.Zero, c.cc.Game.MakeOperand(27 /* EXTAB */), c.rb.Stack);
                c.rb.EmitBinary(binaryOp, c.rb.Stack, c.cc.Game.MakeOperand(offset), c.resultStorage);
            }
            else
            {
                c.rb.EmitBinary(binaryOp, c.cc.Game.Zero, c.cc.Game.MakeOperand(offset), c.resultStorage);
            }

            return c.resultStorage;
        }

        [Builtin("LOWCORE", HasSideEffect = true)]
        public static void LowCoreWriteOp(VoidCall c, [NotNull] ZilObject fieldSpec, [NotNull] IOperand newValue)
        {
            Contract.Requires(fieldSpec != null);
            Contract.Requires(newValue != null);

            if (!TryGetLowCoreField("LOWCORE", c.cc.Context, c.form.SourceLine, fieldSpec, true, out var offset, out var flags, out _))
                return;

            var ternaryOp = ((flags & LowCoreFlags.Byte) != 0) ? TernaryOp.PutByte : TernaryOp.PutWord;
            if ((flags & LowCoreFlags.Extended) != 0)
            {
                var wordOffset = ((flags & LowCoreFlags.Byte) != 0) ? (offset + 1) / 2 : offset;
                c.cc.Context.ZEnvironment.EnsureMinimumHeaderExtension(wordOffset);
                c.rb.EmitBinary(BinaryOp.GetWord, c.cc.Game.Zero, c.cc.Game.MakeOperand(27 /* EXTAB */), c.rb.Stack);
                c.rb.EmitTernary(ternaryOp, c.rb.Stack, c.cc.Game.MakeOperand(offset), newValue, null);
            }
            else
            {
                c.rb.EmitTernary(ternaryOp, c.cc.Game.Zero, c.cc.Game.MakeOperand(offset), newValue, null);
            }
        }

        /// <exception cref="CompilerError">Local variables are not allowed here.</exception>
        [Builtin("LOWCORE-TABLE", HasSideEffect = true)]
        public static void LowCoreTableOp(VoidCall c, [NotNull] ZilObject fieldSpec, int length, [NotNull] ZilAtom handler)
        {
            Contract.Requires(fieldSpec != null);
            Contract.Requires(handler != null);

            if (!TryGetLowCoreField("LOWCORE-TABLE", c.cc.Context, c.form.SourceLine, fieldSpec, false, out var offset, out var flags, out _))
                return;

            if ((flags & LowCoreFlags.Byte) == 0)
            {
                offset *= 2;
            }

            var tmpAtom = ZilAtom.Parse("?TMP", c.cc.Context);
            var lb = c.cc.PushInnerLocal(c.rb, tmpAtom);
            try
            {
                c.rb.EmitStore(lb, c.cc.Game.MakeOperand(offset));

                var label = c.rb.DefineLabel();
                c.rb.MarkLabel(label);

                var form = (ZilForm)Program.Parse(c.cc.Context, c.form.SourceLine, "<{0} <GETB 0 .{1}>>", handler, tmpAtom).Single();
                c.cc.CompileForm(c.rb, form, false, null);

                c.rb.Branch(Condition.IncCheck, lb, c.cc.Game.MakeOperand(offset + length - 1), label, false);
            }
            finally
            {
                c.cc.PopInnerLocal(tmpAtom);
            }
        }

        #endregion

#pragma warning disable RECS0154 // Parameter is never used
        [Builtin("CHTYPE")]
        [NotNull]
        public static IOperand ChtypeValueOp(ValueCall c, [NotNull] IOperand value, [NotNull] ZilAtom type)
        {
            Contract.Requires(value != null);
            Contract.Requires(type != null);
            Contract.Ensures(Contract.Result<IOperand>() != null);

            // TODO: check type?
            return value;
        }
#pragma warning restore RECS0154 // Parameter is never used
    }
}
