/* Copyright 2010, 2016 Jesse McGrew
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
using System.Linq;
using System.Reflection;
using Zilf.Emit;
using Zilf.Interpreter;
using Zilf.Interpreter.Values;
using Zilf.Language;
using Zilf.ZModel;
using Zilf.Diagnostics;

namespace Zilf.Compiler.Builtins
{

    static class ZBuiltins
    {
        static ILookup<string, BuiltinSpec> builtins;

        static ZBuiltins()
        {
            var query = from mi in typeof(ZBuiltins).GetMethods(BindingFlags.Public | BindingFlags.Static)
                        from BuiltinAttribute a in mi.GetCustomAttributes(typeof(BuiltinAttribute), false)
                        from name in a.Names
                        select new { Name = name, Attr = a, Method = mi };

            builtins = query.ToLookup(r => r.Name, r => new BuiltinSpec(r.Attr, r.Method));
        }

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

        delegate void InvalidArgumentDelegate(int index, string message);


        static IList<BuiltinArg> ValidateArguments(
            CompileCtx cc, BuiltinSpec spec, ParameterInfo[] builtinParamInfos,
            ZilObject[] args, InvalidArgumentDelegate error)
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

                var arg = args[i];
                var pi = builtinParamInfos[j];

                if (pi.ParameterType == typeof(IVariable) || pi.ParameterType == typeof(SoftGlobal))
                {
                    // arg must be an atom, or <GVAL atom> or <LVAL atom> in quirks mode
                    var atom = arg as ZilAtom;
                    QuirksMode quirks = QuirksMode.None;
                    if (atom == null)
                    {
                        var attr = pi.GetCustomAttributes(typeof(VariableAttribute), false).Cast<VariableAttribute>().Single();
                        quirks = attr.QuirksMode;
                        if (quirks != QuirksMode.None && arg is ZilForm)
                        {
                            var form = (ZilForm)arg;
                            var fatom = form.First as ZilAtom;
                            if (fatom != null &&
                                (((quirks & QuirksMode.Global) != 0 && fatom.StdAtom == StdAtom.GVAL) ||
                                 ((quirks & QuirksMode.Local) != 0 && fatom.StdAtom == StdAtom.LVAL)) &&
                                form.Rest.First is ZilAtom &&
                                form.Rest.Rest.IsEmpty)
                            {
                                atom = (ZilAtom)form.Rest.First;
                            }
                        }
                    }

                    if (atom == null)
                    {
                        error(i, "argument must be a variable");
                        result.Add(new BuiltinArg(BuiltinArgType.Operand, null));
                    }
                    else if (pi.ParameterType == typeof(IVariable))
                    {
                        if (!cc.Locals.ContainsKey(atom) && !cc.Globals.ContainsKey(atom))
                            error(i, "no such variable: " + atom);

                        var variableRef = GetVariable(cc, arg, quirks);
                        result.Add(new BuiltinArg(BuiltinArgType.Operand, variableRef == null ? null : variableRef.Value.Hard));
                    }
                    else // if (pi.ParameterType == typeof(SoftGlobal))
                    {
                        if (!cc.SoftGlobals.ContainsKey(atom))
                            error(i, "no such variable: " + atom);

                        var variableRef = GetVariable(cc, arg, quirks);
                        result.Add(new BuiltinArg(BuiltinArgType.Operand, variableRef == null ? null : variableRef.Value.Soft));
                    }
                }
                else if (pi.ParameterType == typeof(string))
                {
                    // arg must be a string
                    var zstr = arg as ZilString;
                    if (zstr == null)
                    {
                        error(i, "argument must be a literal string");
                        result.Add(new BuiltinArg(BuiltinArgType.Operand, null));
                    }
                    else
                    {
                        result.Add(new BuiltinArg(BuiltinArgType.Operand, Compiler.TranslateString(zstr.Text, cc.Context)));
                    }
                }
                else if (pi.ParameterType == typeof(IOperand))
                {
                    // if marked with [Variable], allow a variable reference and forbid a non-variable bare atom
                    var varAttr = (VariableAttribute)pi.GetCustomAttributes(typeof(VariableAttribute), false).SingleOrDefault();
                    VariableRef? variable;
                    if (varAttr != null)
                    {
                        if ((variable = GetVariable(cc, arg, varAttr.QuirksMode)) != null)
                        {
                            if (!variable.Value.IsHard)
                            {
                                error(i, "soft variable may not be used here");
                                result.Add(new BuiltinArg(BuiltinArgType.Operand, null));
                            }
                            else
                            {
                                result.Add(new BuiltinArg(BuiltinArgType.Operand, variable.Value.Hard.Indirect));
                            }
                        }
                        else if (arg is ZilAtom)
                        {
                            error(i, "bare atom argument must be a variable name");
                            result.Add(new BuiltinArg(BuiltinArgType.Operand, null));
                        }
                        else
                        {
                            result.Add(new BuiltinArg(BuiltinArgType.NeedsEval, arg));
                        }
                    }
                    else
                    {
                        result.Add(new BuiltinArg(BuiltinArgType.NeedsEval, arg));
                    }
                }
                else if (pi.ParameterType == typeof(IOperand[]))
                {
                    // this absorbs the rest of the args
                    while (i < args.Length)
                    {
                        result.Add(new BuiltinArg(BuiltinArgType.NeedsEval, args[i++]));
                    }
                    break;
                }
                else if (pi.ParameterType == typeof(ZilObject))
                {
                    result.Add(new BuiltinArg(BuiltinArgType.Operand, arg));
                }
                else if (pi.ParameterType == typeof(ZilAtom))
                {
                    if (arg.GetTypeAtom(cc.Context).StdAtom != StdAtom.ATOM)
                        error(i, "argument must be an atom");

                    result.Add(new BuiltinArg(BuiltinArgType.Operand, arg));
                }
                else if (pi.ParameterType == typeof(int))
                {
                    if (arg.GetTypeAtom(cc.Context).StdAtom != StdAtom.FIX)
                        error(i, "argument must be a FIX");

                    result.Add(new BuiltinArg(BuiltinArgType.Operand, ((ZilFix)arg).Value));
                }
                else if (pi.ParameterType == typeof(ZilObject[]))
                {
                    // this absorbs the rest of the args
                    while (i < args.Length)
                    {
                        result.Add(new BuiltinArg(BuiltinArgType.Operand, args[i++]));
                    }
                    break;
                }
                else if (pi.ParameterType == typeof(Block))
                {
                    // arg must be an LVAL reference
                    if (arg.IsLVAL())
                    {
                        var atom = (ZilAtom)((ZilForm)arg).Rest.First;
                        var block = cc.Blocks.FirstOrDefault(b => b.Name == atom);
                        if (block == null)
                        {
                            error(i, "argument must be bound to a block");
                        }

                        result.Add(new BuiltinArg(BuiltinArgType.Operand, block));
                    }
                    else
                    {
                        error(i, "argument must be a local variable reference");
                        result.Add(new BuiltinArg(BuiltinArgType.Operand, null));
                    }
                }
                else
                {
                    throw new NotImplementedException("Unexpected parameter type");
                }
            }

            return result;
        }



        static VariableRef? GetVariable(CompileCtx cc, ZilObject expr, QuirksMode quirks = QuirksMode.None)
        {
            var atom = expr as ZilAtom;

            if (atom == null)
            {
                if (quirks == QuirksMode.None)
                    return null;

                var form = expr as ZilForm;
                if (form == null || !(form.First is ZilAtom))
                    return null;

                switch (((ZilAtom)form.First).StdAtom)
                {
                    case StdAtom.GVAL:
                        if ((quirks & QuirksMode.Global) == 0)
                            return null;
                        break;
                    case StdAtom.LVAL:
                        if ((quirks & QuirksMode.Local) == 0)
                            return null;
                        break;
                }

                if (form.Rest == null || !(form.Rest.First is ZilAtom))
                    return null;

                atom = (ZilAtom)form.Rest.First;
            }

            ILocalBuilder lb;
            IGlobalBuilder gb;
            SoftGlobal sg;

            if (cc.Locals.TryGetValue(atom, out lb))
                return new VariableRef(lb);
            if (cc.Globals.TryGetValue(atom, out gb))
                return new VariableRef(gb);
            if (cc.SoftGlobals.TryGetValue(atom, out sg))
                return new VariableRef(sg);

            return null;
        }

        static List<object> MakeBuiltinMethodParams(
            CompileCtx cc, BuiltinSpec spec,
            ParameterInfo[] builtinParamInfos, object call,
            IList<BuiltinArg> args)
        {
            Contract.Requires(cc != null);
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

            var result = new List<object>(builtinParamInfos.Length);

            // call
            result.Add(call);

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
                    if (j >= args.Count)
                    {
                        result.Add(new IOperand[0]);
                    }
                    else
                    {
                        result.Add(args.Skip(j).Select(a => (IOperand)a.Value).ToArray());
                    }
                }
                else if (pi.ParameterType == typeof(ZilObject[]))
                {
                    // add all remaining values as a param array
                    if (j >= args.Count)
                    {
                        result.Add(new ZilObject[0]);
                    }
                    else
                    {
                        result.Add(args.Skip(j).Select(a => (ZilObject)a.Value).ToArray());
                    }
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

        static object CompileBuiltinCall<TCall>(string name, CompileCtx cc, IRoutineBuilder rb, ZilForm form, TCall call)
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
            using (var operands = Operands.Compile(cc, rb, form.SourceLine, needEvalExprs))
            {
                // update validatedArgs with the evaluated operands
                for (int i = 0; i < operands.Count; i++)
                {
                    var oidx = needEval[i].oidx;
                    validatedArgs[oidx] = new BuiltinArg(BuiltinArgType.Operand, operands[i]);
                }

                // call the spec method to generate code for the builtin
                var builtinParams = MakeBuiltinMethodParams(cc, spec, builtinParamInfos, call, validatedArgs);
                try
                {
                    return spec.Method.Invoke(null, builtinParams.ToArray());
                }
                catch (TargetInvocationException ex) when (ex.InnerException is ZilError)
                {
                    throw ex.InnerException;
                }
            }
        }

        public static IOperand CompileValueCall(string name, CompileCtx cc, IRoutineBuilder rb, ZilForm form, IVariable resultStorage)
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

        public static void CompileVoidCall(string name, CompileCtx cc, IRoutineBuilder rb, ZilForm form)
        {
            Contract.Requires(name != null);
            Contract.Requires(cc != null);
            Contract.Requires(rb != null);
            Contract.Requires(form != null);

            CompileBuiltinCall(name, cc, rb, form, new VoidCall(cc, rb, form));
        }

        public static void CompilePredCall(string name, CompileCtx cc, IRoutineBuilder rb, ZilForm form, ILabel label, bool polarity)
        {
            Contract.Requires(name != null);
            Contract.Requires(cc != null);
            Contract.Requires(rb != null);
            Contract.Requires(form != null);
            Contract.Requires(label != null);

            CompileBuiltinCall(name, cc, rb, form, new PredCall(cc, rb, form, label, polarity));
        }

        public static void CompileValuePredCall(string name, CompileCtx cc, IRoutineBuilder rb, ZilForm form, IVariable resultStorage, ILabel label, bool polarity)
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

        [Builtin("EQUAL?", "=?", "==?")]
        public static void VarargsEqualityOp(
            PredCall c, IOperand arg1, IOperand arg2,
            params IOperand[] restOfArgs)
        {
            Contract.Requires(arg1 != null);
            Contract.Requires(arg2 != null);
            Contract.Requires(restOfArgs != null);

            if (arg1 is INumericOperand)
            {
                var value = ((INumericOperand)arg1).Value;

                if ((arg2 is INumericOperand && ((INumericOperand)arg2).Value == value) ||
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

                if (arg2 is INumericOperand &&
                    ((INumericOperand)arg2).Value != value &&
                    restOfArgs.All(arg => arg is INumericOperand && ((INumericOperand)arg).Value != value))
                {
                    // we know it's not equal, and there are no stack operands, so branch accordingly
                    if (!c.polarity)
                        c.rb.Branch(c.label);

                    return;
                }

                // we can't simplify the branch, but we can still skip testing all the constants
                if (arg2 is INumericOperand || restOfArgs.Any(arg => arg is INumericOperand))
                {
                    var queue = new Queue<IOperand>(restOfArgs.Length);

                    if (!(arg2 is INumericOperand))
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
                ILocalBuilder tempLocal = null;
                ZilAtom tempAtom = null;
                if (arg1 == c.rb.Stack)
                {
                    tempAtom = ZilAtom.Parse("?TMP", c.cc.Context);
                    tempLocal = Compiler.PushInnerLocal(c.cc, c.rb, tempAtom);
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
                    Compiler.PopInnerLocal(c.cc, tempAtom);
            }
        }

        [Builtin("N=?", "N==?")]
        public static void NegatedVarargsEqualityOp(
            PredCall c, IOperand arg1, IOperand arg2,
            params IOperand[] restOfArgs)
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
            IOperand left, IOperand center, IOperand right)
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
            IOperand left, IOperand center, IOperand right = null)
        {
            Contract.Requires(left != null);
            Contract.Requires(center != null);

            c.rb.EmitTernary(op, left, center, right ?? c.cc.Game.Zero, null);
        }

        [Builtin("PUT", "ZPUT", Data = TernaryOp.PutWord, HasSideEffect = true)]
        [Builtin("PUTB", Data = TernaryOp.PutByte, HasSideEffect = true)]
        public static void TernaryTableVoidOp(
            VoidCall c, [Data] TernaryOp op,
            [Table] IOperand left, IOperand center, IOperand right)
        {
            Contract.Requires(left != null);
            Contract.Requires(center != null);
            Contract.Requires(right != null);

            c.rb.EmitTernary(op, left, center, right, null);
        }

        [Builtin("PUTP", Data = TernaryOp.PutProperty, HasSideEffect = true)]
        public static void TernaryObjectVoidOp(
            VoidCall c, [Data] TernaryOp op,
            [Object] IOperand left, IOperand center, IOperand right)
        {
            Contract.Requires(left != null);
            Contract.Requires(center != null);
            Contract.Requires(right != null);

            c.rb.EmitTernary(op, left, center, right, null);
        }

        [Builtin("COPYT", Data = TernaryOp.CopyTable, HasSideEffect = true, MinVersion = 5)]
        public static void TernaryTableTableVoidOp(
            VoidCall c, [Data] TernaryOp op,
            [Table] IOperand left, [Table] IOperand center, IOperand right)
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
        public static IOperand BinaryValueOp(
            ValueCall c, [Data] BinaryOp op, IOperand left, IOperand right)
        {
            Contract.Requires(left != null);
            Contract.Requires(right != null);
            Contract.Ensures(Contract.Result<IOperand>() != null);

            var nleft = left as INumericOperand;
            var nright = right as INumericOperand;
            if (nleft != null && nright != null)
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

        [Builtin("XORB")]
        public static IOperand BinaryXorOp(ValueCall c, ZilObject left, ZilObject right)
        {
            Contract.Requires(left != null);
            Contract.Requires(right != null);
            Contract.Ensures(Contract.Result<IOperand>() != null);

            ZilObject value;
            if (left is ZilFix && ((ZilFix)left).Value == -1)
            {
                value = right;
            }
            else if (right is ZilFix && ((ZilFix)right).Value == -1)
            {
                value = left;
            }
            else
            {
                return c.HandleMessage(CompilerMessages._0_One_Operand_Must_Be_1, "XORB");
            }

            var storage = Compiler.CompileAsOperand(c.cc, c.rb, value, c.form.SourceLine, c.resultStorage);

            if (storage is INumericOperand)
            {
                return c.cc.Game.MakeOperand((short)(~((INumericOperand)storage).Value));
            }
            c.rb.EmitUnary(UnaryOp.Not, storage, c.resultStorage);
            return c.resultStorage;
        }

        [Builtin("ADD", "+", Data = BinaryOp.Add)]
        [Builtin("SUB", "-", Data = BinaryOp.Sub)]
        [Builtin("MUL", "*", Data = BinaryOp.Mul)]
        [Builtin("DIV", "/", Data = BinaryOp.Div)]
        [Builtin("BAND", "ANDB", Data = BinaryOp.And)]
        [Builtin("BOR", "ORB", Data = BinaryOp.Or)]
        public static IOperand ArithmeticOp(
            ValueCall c, [Data] BinaryOp op, params IOperand[] args)
        {
            Contract.Requires(args != null);

            if (args.Length > 0)
            {
                short foldedInit;
                Func<short, short, short> foldedOp;

                switch (op)
                {
                    case BinaryOp.Add:
                        foldedOp = (a, b) => (short)(a + b);
                        foldedInit = 0;
                        break;
                    case BinaryOp.Sub:
                        foldedOp = (a, b) => (short)(a - b);
                        foldedInit = 0;
                        break;
                    case BinaryOp.Mul:
                        foldedOp = (a, b) => (short)(a * b);
                        foldedInit = 1;
                        break;
                    case BinaryOp.Div:
                        foldedOp = (a, b) => (short)(a / b);
                        foldedInit = 1;
                        break;
                    case BinaryOp.And:
                        foldedOp = (a, b) => (short)(a & b);
                        foldedInit = -1;
                        break;
                    case BinaryOp.Or:
                        foldedOp = (a, b) => (short)(a | b);
                        foldedInit = 0;
                        break;
                    default:
                        throw new NotImplementedException();
                }

                var folded = FoldConstantArithmetic(c.cc, foldedInit, foldedOp, args);
                if (folded != null)
                    return folded;
            }

            IOperand init;
            switch (op)
            {
                case BinaryOp.Mul:
                case BinaryOp.Div:
                    init = c.cc.Game.One;
                    break;

                case BinaryOp.And:
                    init = c.cc.Game.MakeOperand(-1);
                    break;

                default:
                    init = c.cc.Game.Zero;
                    break;
            }

            switch (args.Length)
            {
                case 0:
                    return init;

                case 1:
                    switch (op)
                    {
                        case BinaryOp.Add:
                        case BinaryOp.Mul:
                        case BinaryOp.And:
                        case BinaryOp.Or:
                            return args[0];

                        case BinaryOp.Sub:
                            c.rb.EmitUnary(UnaryOp.Neg, args[0], c.resultStorage);
                            return c.resultStorage;

                        default:
                            c.rb.EmitBinary(op, init, args[0], c.resultStorage);
                            return c.resultStorage;
                    }

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

        static IOperand FoldConstantArithmetic(CompileCtx cc, short init, Func<short, short, short> op, IOperand[] args)
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
        public static void BinaryAndPredOp(PredCall c, IOperand left, IOperand right)
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

        // TODO: REST with a constant table argument should produce a constant operand (<REST MYTABLE 2> -> "MYTABLE+2")
        [Builtin("REST", "ZREST", Data = BinaryOp.Add)]
        [Builtin("BACK", "ZBACK", Data = BinaryOp.Sub)]
        public static IOperand RestOrBackOp(
            ValueCall c, [Data] BinaryOp op, IOperand left, IOperand right = null)
        {
            Contract.Requires(left != null);
            Contract.Ensures(Contract.Result<IOperand>() != null);

            return ArithmeticOp(c, op, left, right ?? c.cc.Game.One);
        }

        [Builtin("CURSET", Data = BinaryOp.SetCursor, MinVersion = 4, MaxVersion = 5, HasSideEffect = true)]
        [Builtin("COLOR", Data = BinaryOp.SetColor, MinVersion = 5, HasSideEffect = true)]
        [Builtin("DIROUT", Data = BinaryOp.DirectOutput, HasSideEffect = true)]
        [Builtin("THROW", Data = BinaryOp.Throw, MinVersion = 5, HasSideEffect = true)]
        [Builtin("SCROLL", Data = BinaryOp.ScrollWindow, MinVersion = 6, HasSideEffect = true)]
        public static void BinaryVoidOp(
            VoidCall c, [Data] BinaryOp op, IOperand left, IOperand right)
        {
            Contract.Requires(left != null);
            Contract.Requires(right != null);

            c.rb.EmitBinary(op, left, right, null);
        }

        [Builtin("CURSET", MinVersion = 6, HasSideEffect = true)]
        public static void CursetVoidOp(VoidCall c, IOperand line, IOperand column = null, IOperand window = null)
        {
            Contract.Requires(line != null);

            if (window != null)
                c.rb.EmitTernary(TernaryOp.SetCursor, line, column, window, null);
            else
                c.rb.EmitBinary(BinaryOp.SetCursor, line, column ?? c.cc.Game.Zero, null);
        }


        [Builtin("GRTR?", "G?", Data = Condition.Greater)]
        [Builtin("LESS?", "L?", Data = Condition.Less)]
        [Builtin("BTST", Data = Condition.TestBits)]
        public static void BinaryPredOp(
            PredCall c, [Data] Condition cond, IOperand left, IOperand right)
        {
            Contract.Requires(left != null);
            Contract.Requires(right != null);

            var nleft = left as INumericOperand;
            var nright = right as INumericOperand;
            if (nleft != null && nright != null)
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
                        throw new NotImplementedException();
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
            PredCall c, IOperand menuId, [Table] IOperand table)
        {
            Contract.Requires(menuId != null);
            Contract.Requires(table != null);

            c.rb.Branch(Condition.MakeMenu, menuId, table, c.label, c.polarity);
        }

        [Builtin("L=?", Data = Condition.Greater)]
        [Builtin("G=?", Data = Condition.Less)]
        public static void NegatedBinaryPredOp(
            PredCall c, [Data] Condition cond, IOperand left, IOperand right)
        {
            Contract.Requires(left != null);
            Contract.Requires(right != null);

            BinaryPredOp(new PredCall(c.cc, c.rb, c.form, c.label, !c.polarity), cond, left, right);
        }

        [Builtin("DLESS?", Data = Condition.DecCheck, HasSideEffect = true)]
        [Builtin("IGRTR?", Data = Condition.IncCheck, HasSideEffect = true)]
        public static void BinaryVariablePredOp(
            PredCall c, [Data] Condition cond, [Variable(QuirksMode = QuirksMode.Both)] IVariable left, IOperand right)
        {
            Contract.Requires(left != null);
            Contract.Requires(right != null);

            c.rb.Branch(cond, left, right, c.label, c.polarity);
        }

        [Builtin("PICINF", MinVersion = 6, HasSideEffect = true)]
        public static void PicinfPredOp(PredCall c, IOperand left, [Table] IOperand right)
        {
            Contract.Requires(left != null);
            Contract.Requires(right != null);

            c.rb.Branch(Condition.PictureData, left, right, c.label, c.polarity);
        }

        [Builtin("DLESS?", Data = Condition.Less, HasSideEffect = true)]
        [Builtin("IGRTR?", Data = Condition.Greater, HasSideEffect = true)]
        public static void BinaryVariablePredOp(
            PredCall c, [Data] Condition cond, [Variable] SoftGlobal left, IOperand right)
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
        public static IOperand BinaryObjectValueOp(
            ValueCall c, [Data] BinaryOp op, [Object] IOperand left, IOperand right)
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
            VoidCall c, [Data] BinaryOp op, [Object] IOperand left, IOperand right)
        {
            Contract.Requires(left != null);
            Contract.Requires(right != null);

            c.rb.EmitBinary(op, left, right, null);
        }

        [Builtin("FSET?", Data = Condition.TestAttr)]
        public static void BinaryObjectPredOp(
            PredCall c, [Data] Condition cond, [Object] IOperand left, IOperand right)
        {
            Contract.Requires(left != null);
            Contract.Requires(right != null);

            c.rb.Branch(cond, left, right, c.label, c.polarity);
        }

        [Builtin("IN?", Data = Condition.Inside)]
        public static void BinaryObjectObjectPredOp(
            PredCall c, [Data] Condition cond, [Object] IOperand left, [Object] IOperand right)
        {
            Contract.Requires(left != null);
            Contract.Requires(right != null);

            c.rb.Branch(cond, left, right, c.label, c.polarity);
        }

        [Builtin("MOVE", Data = BinaryOp.MoveObject, HasSideEffect = true)]
        public static void BinaryObjectObjectVoidOp(
            VoidCall c, [Data] BinaryOp op, [Object] IOperand left, [Object] IOperand right)
        {
            Contract.Requires(left != null);
            Contract.Requires(right != null);

            c.rb.EmitBinary(op, left, right, null);
        }

        [Builtin("GETPT", Data = BinaryOp.GetPropAddress)]
        [return: Table]
        public static IOperand BinaryObjectToTableValueOp(
            ValueCall c, [Data] BinaryOp op, [Object] IOperand left, IOperand right)
        {
            Contract.Requires(left != null);
            Contract.Requires(right != null);
            Contract.Ensures(Contract.Result<IOperand>() != null);

            c.rb.EmitBinary(op, left, right, c.resultStorage);
            return c.resultStorage;
        }

        [Builtin("GET", "NTH", "ZGET", Data = BinaryOp.GetWord)]
        [Builtin("GETB", Data = BinaryOp.GetByte)]
        public static IOperand BinaryTableValueOp(
            ValueCall c, [Data] BinaryOp op, [Table] IOperand left, IOperand right)
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
        public static IOperand UnaryValueOp(
            ValueCall c, [Data] UnaryOp op, IOperand value)
        {
            Contract.Requires(value != null);
            Contract.Ensures(Contract.Result<IOperand>() != null);

            if (op == UnaryOp.Not && value is INumericOperand)
            {
                return c.cc.Game.MakeOperand((short)(~((INumericOperand)value).Value));
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
            VoidCall c, [Data] UnaryOp op, IOperand value)
        {
            Contract.Requires(value != null);

            c.rb.EmitUnary(op, value, null);
        }

        [Builtin("ZERO?", "0?")]
        public static void ZeroPredOp(PredCall c, IOperand value)
        {
            Contract.Requires(value != null);

            if (value is INumericOperand)
            {
                if ((((INumericOperand)value).Value == 0) == c.polarity)
                    c.rb.Branch(c.label);
            }
            else
            {
                c.rb.BranchIfZero(value, c.label, c.polarity);
            }
        }

        [Builtin("1?")]
        public static void OnePredOp(PredCall c, IOperand value)
        {
            Contract.Requires(value != null);

            if (value is INumericOperand)
            {
                if ((((INumericOperand)value).Value == 1) == c.polarity)
                    c.rb.Branch(c.label);
            }
            else
            {
                c.rb.BranchIfEqual(value, c.cc.Game.One, c.label, c.polarity);
            }
        }

        [Builtin("LOC", Data = UnaryOp.GetParent)]
        public static IOperand UnaryObjectValueOp(
            ValueCall c, [Data] UnaryOp op, [Object] IOperand obj)
        {
            Contract.Requires(obj != null);
            Contract.Ensures(Contract.Result<IOperand>() != null);

            c.rb.EmitUnary(op, obj, c.resultStorage);
            return c.resultStorage;
        }

        [Builtin("FIRST?", Data = false)]
        [Builtin("NEXT?", Data = true)]
        public static void UnaryObjectValuePredOp(
            ValuePredCall c, [Data] bool sibling, [Object] IOperand obj)
        {
            Contract.Requires(obj != null);

            if (sibling)
                c.rb.EmitGetSibling(obj, c.resultStorage, c.label, c.polarity);
            else
                c.rb.EmitGetChild(obj, c.resultStorage, c.label, c.polarity);
        }

        [Builtin("PTSIZE", Data = UnaryOp.GetPropSize)]
        public static IOperand UnaryTableValueOp(
            ValueCall c, [Data] UnaryOp op, [Table] IOperand value)
        {
            Contract.Requires(value != null);
            Contract.Ensures(Contract.Result<IOperand>() != null);

            c.rb.EmitUnary(op, value, c.resultStorage);
            return c.resultStorage;
        }

        [Builtin("REMOVE", "ZREMOVE", Data = UnaryOp.RemoveObject, HasSideEffect = true)]
        public static void UnaryObjectVoidOp(
            VoidCall c, [Data] UnaryOp op, [Object] IOperand value)
        {
            Contract.Requires(value != null);

            c.rb.EmitUnary(op, value, null);
        }

        [Builtin("ASSIGNED?", Data = Condition.ArgProvided, MinVersion = 5)]
        public static void UnaryVariablePredOp(
            PredCall c, [Data] Condition cond, [Variable] IVariable var)
        {
            Contract.Requires(var != null);

            c.rb.Branch(cond, var, null, c.label, c.polarity);
        }

        [Builtin("ASSIGNED?", MinVersion = 5)]
        public static void SoftGlobalAssignedOp(PredCall c, [Variable] SoftGlobal var)
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
            VoidCall c, [Data] UnaryOp op, [Table] IOperand value)
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
            VoidCall c, [Data] PrintOp op, IOperand value)
        {
            Contract.Requires(value != null);

            c.rb.EmitPrint(op, value);
        }

        [Builtin("PRINTT", HasSideEffect = true)]
        public static void PrintTableOp(
            VoidCall c, [Table] IOperand table, IOperand width,
            IOperand height = null, IOperand skip = null)
        {
            Contract.Requires(table != null);
            Contract.Requires(width != null);
            Contract.Requires(height != null || skip == null);

            c.rb.EmitPrintTable(table, width, height, skip);
        }

        [Builtin("PRINTI", Data = false, HasSideEffect = true)]
        [Builtin("PRINTR", Data = true, HasSideEffect = true)]
        public static void UnaryPrintStringOp(
            VoidCall c, [Data] bool crlfRtrue, string text)
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

        [Builtin("SET", HasSideEffect = true)]
        public static IOperand SetValueOp(
            ValueCall c, [Variable(QuirksMode = QuirksMode.Local)] IVariable dest, ZilObject value)
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

            var storage = Compiler.CompileAsOperand(c.cc, c.rb, value, c.form.SourceLine, dest);
            if (storage != dest)
                c.rb.EmitStore(dest, storage);
            return dest;
        }

        [Builtin("SET", HasSideEffect = true)]
        public static IOperand SetValueOp(
            ValueCall c, [Variable(QuirksMode = QuirksMode.Local)] SoftGlobal dest, ZilObject value)
        {
            Contract.Requires(dest != null);
            Contract.Requires(value != null);
            Contract.Ensures(Contract.Result<IOperand>() != null);

            var storage = Compiler.CompileAsOperand(c.cc, c.rb, value, c.form.SourceLine, c.rb.Stack);

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

        [Builtin("SETG", HasSideEffect = true)]
        public static IOperand SetgValueOp(
            ValueCall c, [Variable(QuirksMode = QuirksMode.Global)] IVariable dest, ZilObject value)
        {
            Contract.Requires(dest != null);
            Contract.Requires(value != null);
            Contract.Ensures(Contract.Result<IOperand>() != null);

            return SetValueOp(c, dest, value);
        }

        [Builtin("SETG", HasSideEffect = true)]
        public static IOperand SetgValueOp(
            ValueCall c, [Variable(QuirksMode = QuirksMode.Global)] SoftGlobal dest, ZilObject value)
        {
            Contract.Requires(dest != null);
            Contract.Requires(value != null);
            Contract.Ensures(Contract.Result<IOperand>() != null);

            return SetValueOp(c, dest, value);
        }

        [Builtin("SET")]
        public static void SetVoidOp(
            VoidCall c, [Variable(QuirksMode = QuirksMode.Local)] IOperand dest, ZilObject value)
        {
            Contract.Requires(dest != null);
            Contract.Requires(value != null);

            // in void context, we don't need to return the newly set value, so we
            // can support <SET <fancy-expression> value>.

            if (dest is IIndirectOperand)
            {
                var destVar = ((IIndirectOperand)dest).Variable;
                var storage = Compiler.CompileAsOperand(c.cc, c.rb, value, c.form.SourceLine, destVar);
                if (storage != destVar)
                    c.rb.EmitStore(destVar, storage);
            }
            else
            {
                using (var operands = Operands.Compile(c.cc, c.rb, c.form.SourceLine, value))
                {
                    if (dest == c.rb.Stack && operands[0] == c.rb.Stack)
                    {
                        var tempAtom = ZilAtom.Parse("?TMP", c.cc.Context);
                        Compiler.PushInnerLocal(c.cc, c.rb, tempAtom);
                        try
                        {
                            var tempLocal = c.cc.Locals[tempAtom];
                            c.rb.EmitStore(tempLocal, operands[0]);
                            c.rb.EmitBinary(BinaryOp.StoreIndirect, dest, tempLocal, null);
                        }
                        finally
                        {
                            Compiler.PopInnerLocal(c.cc, tempAtom);
                        }
                    }
                    else
                    {
                        c.rb.EmitBinary(BinaryOp.StoreIndirect, dest, operands[0], null);
                    }
                }
            }
        }

        [Builtin("SET")]
        public static void SetVoidOp(
            VoidCall c, [Variable(QuirksMode = QuirksMode.Local)] SoftGlobal dest, ZilObject value)
        {
            Contract.Requires(dest != null);
            Contract.Requires(value != null);

            Contract.Assume(c.cc.SoftGlobalsTable != null);

            c.rb.EmitTernary(
                dest.IsWord ? TernaryOp.PutWord : TernaryOp.PutByte,
                c.cc.SoftGlobalsTable,
                c.cc.Game.MakeOperand(dest.Offset),
                Compiler.CompileAsOperand(c.cc, c.rb, value, c.form.SourceLine),
                null);
        }

        [Builtin("SETG")]
        public static void SetgVoidOp(
            VoidCall c, [Variable(QuirksMode = QuirksMode.Global)] IOperand dest, ZilObject value)
        {
            Contract.Requires(dest != null);
            Contract.Requires(value != null);

            SetVoidOp(c, dest, value);
        }

        [Builtin("SETG")]
        public static void SetgVoidOp(
            VoidCall c, [Variable(QuirksMode = QuirksMode.Global)] SoftGlobal dest, ZilObject value)
        {
            Contract.Requires(dest != null);
            Contract.Requires(value != null);

            SetVoidOp(c, dest, value);
        }

        [Builtin("SET", HasSideEffect = true)]
        public static void SetPredOp(
            PredCall c, [Variable(QuirksMode = QuirksMode.Local)] IVariable dest, ZilObject value)
        {
            Contract.Requires(dest != null);
            Contract.Requires(value != null);

            // see note in SetValueOp regarding dest being IVariable
            Compiler.CompileAsOperandWithBranch(c.cc, c.rb, value, dest, c.label, c.polarity);
        }

        [Builtin("SETG", HasSideEffect = true)]
        public static void SetgPredOp(
            PredCall c, [Variable(QuirksMode = QuirksMode.Global)] IVariable dest, ZilObject value)
        {
            Contract.Requires(dest != null);
            Contract.Requires(value != null);

            SetPredOp(c, dest, value);
        }

        [Builtin("INC", Data = BinaryOp.Add, HasSideEffect = true)]
        [Builtin("DEC", Data = BinaryOp.Sub, HasSideEffect = true)]
        public static IOperand IncValueOp(ValueCall c, [Data] BinaryOp op,
            [Variable(QuirksMode = QuirksMode.Both)] IVariable victim)
        {
            Contract.Requires(victim != null);
            Contract.Ensures(Contract.Result<IOperand>() != null);

            c.rb.EmitBinary(op, victim, c.cc.Game.One, victim);
            return victim;
        }

        [Builtin("INC", Data = BinaryOp.Add, HasSideEffect = true)]
        [Builtin("DEC", Data = BinaryOp.Sub, HasSideEffect = true)]
        public static IOperand IncValueOp(ValueCall c, [Data] BinaryOp op,
            [Variable(QuirksMode = QuirksMode.Both)] SoftGlobal victim)
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
            [Variable(QuirksMode = QuirksMode.Both)] IVariable victim)
        {
            Contract.Requires(victim != null);

            c.rb.EmitBinary(op, victim, c.cc.Game.One, victim);
        }

        [Builtin("INC", Data = BinaryOp.Add, HasSideEffect = true)]
        [Builtin("DEC", Data = BinaryOp.Sub, HasSideEffect = true)]
        public static void IncVoidOp(VoidCall c, [Data] BinaryOp op,
            [Variable(QuirksMode = QuirksMode.Both)] SoftGlobal victim)
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
        public static void PushVoidOp(VoidCall c, IOperand value)
        {
            Contract.Requires(value != null);

            c.rb.EmitStore(c.rb.Stack, value);
        }

        [Builtin("XPUSH", MinVersion = 6, HasSideEffect = true)]
        public static void XpushPredOp(PredCall c, IOperand value, IOperand stack)
        {
            Contract.Requires(value != null);
            Contract.Requires(stack != null);

            c.rb.EmitPushUserStack(value, stack, c.label, c.polarity);
        }

        [Builtin("POP", MinVersion = 6, HasSideEffect = true)]
        public static IOperand PopValueOp(ValueCall c, IOperand stack = null)
        {
            if (stack == null)
                c.rb.EmitStore(c.resultStorage, c.rb.Stack);
            else
                c.rb.EmitUnary(UnaryOp.PopUserStack, stack, c.resultStorage);

            return c.resultStorage;
        }

        [Builtin("FSTACK", MinVersion = 6, HasSideEffect = true)]
        public static void FstackVoidOp(VoidCall c, IOperand count, IOperand stack = null)
        {
            if (stack == null)
                c.rb.EmitUnary(UnaryOp.FlushStack, count, null);
            else
                c.rb.EmitBinary(BinaryOp.FlushUserStack, count, stack, null);
        }

        // TODO: support the IVariable, SoftGlobal, and IOperand versions side by side? that way we can skip emitting an instruction for <VALUE VARNAME>
        /*[Builtin("VALUE")]
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
        }*/

        [Builtin("VALUE")]
        public static IOperand ValueOp_Operand(ValueCall c, [Variable] IOperand value)
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
                (what == 2) ? c.cc.Game.MakeOperand(2) :
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
        public static void ReadOp_V3(VoidCall c, IOperand text, IOperand parse)
        {
            Contract.Requires(text != null);

            c.rb.EmitRead(text, parse, null, null, null);
        }

        [Builtin("READ", "ZREAD", MinVersion = 4, MaxVersion = 4, HasSideEffect = true)]
        public static void ReadOp_V4(VoidCall c, IOperand text, IOperand parse,
            IOperand time = null, [Routine] IOperand routine = null)
        {
            Contract.Requires(text != null);
            Contract.Requires(parse != null);
            Contract.Requires(time != null || routine == null);

            c.rb.EmitRead(text, parse, time, routine, null);
        }

        [Builtin("READ", "ZREAD", MinVersion = 5, HasSideEffect = true)]
        public static IOperand ReadOp_V5(ValueCall c, IOperand text,
            IOperand parse = null, IOperand time = null,
            [Routine] IOperand routine = null)
        {
            Contract.Requires(text != null);
            Contract.Requires(parse != null || time == null);
            Contract.Requires(time != null || routine == null);
            Contract.Ensures(Contract.Result<IOperand>() != null);

            c.rb.EmitRead(text, parse, time, routine, c.resultStorage);
            return c.resultStorage;
        }

        [Builtin("INPUT", MinVersion = 4, HasSideEffect = true)]
        public static IOperand InputOp(ValueCall c, IOperand dummy,
            IOperand interval = null, [Routine] IOperand routine = null)
        {
            Contract.Requires(dummy != null);

            if (c.form.Rest.First is ZilFix && ((ZilFix)c.form.Rest.First).Value != 1)
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
        public static void SoundOp_V3(VoidCall c, IOperand number,
            IOperand effect = null, IOperand volume = null)
        {
            Contract.Requires(number != null);
            Contract.Requires(effect != null || volume == null);

            c.rb.EmitPlaySound(number, effect, volume, null);
        }

        [Builtin("SOUND", MinVersion = 5, HasSideEffect = true)]
        public static void SoundOp_V5(VoidCall c, IOperand number,
            IOperand effect = null, IOperand volume = null,
            [Routine] IOperand routine = null)
        {
            Contract.Requires(number != null);
            Contract.Requires(effect != null || volume == null);
            Contract.Requires(volume != null || routine == null);

            c.rb.EmitPlaySound(number, effect, volume, null);
        }

        #endregion

        #region Vocab Opcodes

        [Builtin("ZWSTR", MinVersion = 5, HasSideEffect = true)]
        public static void EncodeTextOp(VoidCall c,
            [Table] IOperand src, IOperand length,
            IOperand srcOffset, [Table] IOperand dest)
        {
            Contract.Requires(src != null);
            Contract.Requires(length != null);
            Contract.Requires(srcOffset != null);
            Contract.Requires(dest != null);

            c.rb.EmitEncodeText(src, length, srcOffset, dest);
        }

        [Builtin("LEX", MinVersion = 5, HasSideEffect = true)]
        public static void LexOp(VoidCall c,
            [Table] IOperand text, [Table] IOperand parse,
            [Table] IOperand dictionary = null, IOperand flag = null)
        {
            Contract.Requires(text != null);
            Contract.Requires(parse != null);
            Contract.Requires(dictionary != null || flag == null);

            c.rb.EmitTokenize(text, parse, dictionary, flag);
        }

        #endregion

        #region Save/Restore Opcodes

        [Builtin("RESTORE", "ZRESTORE", MaxVersion = 3, HasSideEffect = true)]
        public static void RestoreOp_V3(PredCall c)
        {
            if (c.rb.HasBranchSave)
            {
                c.rb.EmitRestore(c.label, c.polarity);
            }
            else
            {
                throw new NotImplementedException("RestoreOp_V3 without HasBranchSave");
            }
        }

        [Builtin("RESTORE", "ZRESTORE", MinVersion = 4, HasSideEffect = true)]
        public static IOperand RestoreOp_V4(ValueCall c)
        {
            if (c.rb.HasStoreSave)
            {
                c.rb.EmitRestore(c.resultStorage);
                return c.resultStorage;
            }
            throw new NotImplementedException("RestoreOp_V4 without HasStoreSave");
        }

        [Builtin("RESTORE", "ZRESTORE", MinVersion = 5, HasSideEffect = true)]
        public static IOperand RestoreOp_V5(ValueCall c, [Table] IOperand table,
            IOperand bytes, [Table] IOperand name)
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
            throw new NotImplementedException("RestoreOp_V5 without HasExtendedSave");
        }

        [Builtin("SAVE", "ZSAVE", MaxVersion = 3, HasSideEffect = true)]
        public static void SaveOp_V3(PredCall c)
        {
            if (c.rb.HasBranchSave)
            {
                c.rb.EmitSave(c.label, c.polarity);
            }
            else
            {
                throw new NotImplementedException("SaveOp_V3 without HasBranchSave");
            }
        }

        [Builtin("SAVE", "ZSAVE", MinVersion = 4, HasSideEffect = true)]
        public static IOperand SaveOp_V4(ValueCall c)
        {
            if (c.rb.HasStoreSave)
            {
                c.rb.EmitSave(c.resultStorage);
                return c.resultStorage;
            }
            throw new NotImplementedException("SaveOp_V4 without HasStoreSave");
        }

        [Builtin("SAVE", "ZSAVE", MinVersion = 5, HasSideEffect = true)]
        public static IOperand SaveOp_V5(ValueCall c, [Table] IOperand table,
            IOperand bytes, [Table] IOperand name)
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
            throw new NotImplementedException("SaveOp_V5 without HasExtendedSave");
        }

        #endregion

        #region Routine Opcodes/Builtins

        [Builtin("RETURN", HasSideEffect = true)]
        public static void ReturnOp(VoidCall c, IOperand value = null, Block block = null)
        {
            if (block == null)
            {
                block = c.cc.Blocks.First(b => (b.Flags & BlockFlags.ExplicitOnly) == 0);
            }

            if (block.ReturnLabel == null)
            {
                // return from routine
                c.rb.Return(value ?? c.cc.Game.One);
            }
            else
            {
                // return from enclosing PROG/REPEAT
                if ((block.Flags & BlockFlags.WantResult) != 0)
                {
                    if (value == null)
                        c.rb.EmitStore(c.rb.Stack, c.cc.Game.One);
                    else if (value != c.rb.Stack)
                        c.rb.EmitStore(c.rb.Stack, value);
                }
                else if (value != null)
                {
                    if (value == c.rb.Stack)
                        c.rb.EmitPopStack();

                    c.HandleMessage(CompilerMessages.RETURN_Value_Ignored_Block_Is_In_Void_Context);
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
        public static IOperand CallValueOp(ValueCall c,
            [Routine] IOperand routine, params IOperand[] args)
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
            [Routine] IOperand routine, params IOperand[] args)
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
            IOperand value, [Table] IOperand table, IOperand length)
        {
            Contract.Requires(value != null);
            Contract.Requires(table != null);
            Contract.Requires(length != null);

            c.rb.EmitScanTable(value, table, length, null, c.resultStorage, c.label, c.polarity);
        }

        [Builtin("INTBL?", MinVersion = 5)]
        [return: Table]
        public static void IntblValuePredOp_V5(ValuePredCall c,
            IOperand value, [Table] IOperand table, IOperand length, IOperand form = null)
        {
            Contract.Requires(value != null);
            Contract.Requires(table != null);
            Contract.Requires(length != null);

            c.rb.EmitScanTable(value, table, length, form, c.resultStorage, c.label, c.polarity);
        }

        static bool TryGetLowCoreField(string name, Context ctx, ISourceLine src, ZilObject fieldSpec, bool writing,
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

            var atom = fieldSpec as ZilAtom;
            if (atom != null)
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

            var list = fieldSpec as ZilList;
            if (list != null && list.GetTypeAtom(ctx).StdAtom == StdAtom.LIST)
            {
                if (((IStructure)list).GetLength(2) != 2)
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

                var fix = list.Rest.First as ZilFix;
                if (fix == null || fix.Value < 0 || fix.Value > 1)
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
        public static IOperand LowCoreReadOp(ValueCall c, ZilObject fieldSpec)
        {
            Contract.Requires(fieldSpec != null);
            Contract.Ensures(Contract.Result<IOperand>() != null);

            int offset, minVersion;
            LowCoreFlags flags;

            if (!TryGetLowCoreField("LOWCORE", c.cc.Context, c.form.SourceLine, fieldSpec, false, out offset, out flags, out minVersion))
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
        public static void LowCoreWriteOp(VoidCall c, ZilObject fieldSpec, IOperand newValue)
        {
            Contract.Requires(fieldSpec != null);
            Contract.Requires(newValue != null);

            int offset, minVersion;
            LowCoreFlags flags;

            if (!TryGetLowCoreField("LOWCORE", c.cc.Context, c.form.SourceLine, fieldSpec, true, out offset, out flags, out minVersion))
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

        [Builtin("LOWCORE-TABLE", HasSideEffect = true)]
        public static void LowCoreTableOp(VoidCall c, ZilObject fieldSpec, int length, ZilAtom handler)
        {
            Contract.Requires(fieldSpec != null);
            Contract.Requires(handler != null);

            int offset, minVersion;
            LowCoreFlags flags;

            if (!TryGetLowCoreField("LOWCORE-TABLE", c.cc.Context, c.form.SourceLine, fieldSpec, false, out offset, out flags, out minVersion))
                return;

            if ((flags & LowCoreFlags.Byte) == 0)
            {
                offset *= 2;
            }

            var tmpAtom = ZilAtom.Parse("?TMP", c.cc.Context);
            var lb = Compiler.PushInnerLocal(c.cc, c.rb, tmpAtom);
            try
            {
                c.rb.EmitStore(lb, c.cc.Game.MakeOperand((int)offset));

                var label = c.rb.DefineLabel();
                c.rb.MarkLabel(label);

                var form = new ZilForm(new ZilObject[] {
                        handler,
                        new ZilForm(new ZilObject[] {
                            c.cc.Context.GetStdAtom(StdAtom.GETB),
                            ZilFix.Zero,
                            new ZilForm(new ZilObject[] {
                                c.cc.Context.GetStdAtom(StdAtom.LVAL),
                                tmpAtom
                            })
                        })
                    });
                form.SourceLine = c.form.SourceLine;
                Compiler.CompileForm(c.cc, c.rb, form, false, null);

                c.rb.Branch(Condition.IncCheck, lb, c.cc.Game.MakeOperand((int)offset + length - 1), label, false);
            }
            finally
            {
                Compiler.PopInnerLocal(c.cc, tmpAtom);
            }
        }

        #endregion

#pragma warning disable RECS0154 // Parameter is never used
        [Builtin("CHTYPE")]
        public static IOperand ChtypeValueOp(ValueCall c, IOperand value, ZilAtom type)
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
