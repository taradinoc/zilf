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
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Text;
using Zilf.Interpreter.Values;
using Zilf.Language;
using Zilf.Diagnostics;
using Zilf.Common;

namespace Zilf.Interpreter
{
    sealed class ArgSpec : IEnumerable<ArgItem>
    {
        // name of the function to which this spec belongs
        // reference to the "QUOTE" atom used for any quoted args
        readonly ZilAtom? quoteAtom;
        // "BIND"

        // "CALL"
        readonly ZilObject? callDecl;

        // regular args, "OPT", and "AUX"
        readonly ZilAtom[] argAtoms;
        readonly ZilObject?[] argDecls;
        readonly bool[] argQuoted;
        readonly ZilObject?[] argDefaults;
        readonly int auxArgsStart;

        // "ARGS" or "TUPLE"
        readonly ZilObject? varargsDecl;
        readonly bool varargsQuoted;

        // "NAME"/"ACT"

        // "VALUE"
        readonly ZilObject? valueDecl;

        ArgSpec(ZilAtom? name, ZilAtom? activationAtom, int optArgsStart, int auxArgsStart,
            ZilAtom? varargsAtom, bool varargsQuoted, ZilObject? varargsDecl,
            ZilAtom? environmentAtom, ZilObject? valueDecl, ZilAtom? quoteAtom,
            ZilAtom? callAtom, ZilObject? callDecl,
            ZilAtom[] argAtoms,
            ZilObject?[] argDecls, bool[] argQuoted, ZilObject?[] argDefaults)
        {
            this.Name = name;
            this.ActivationAtom = activationAtom;
            this.MinArgCount = optArgsStart;
            this.auxArgsStart = auxArgsStart;
            this.VarargsAtom = varargsAtom;
            this.varargsQuoted = varargsQuoted;
            this.varargsDecl = varargsDecl;
            this.EnvironmentAtom = environmentAtom;
            this.valueDecl = valueDecl;
            this.quoteAtom = quoteAtom;
            this.CallAtom = callAtom;
            this.callDecl = callDecl;
            this.argAtoms = argAtoms;
            this.argDecls = argDecls;
            this.argQuoted = argQuoted;
            this.argDefaults = argDefaults;
        }

        /// <exception cref="InterpreterError">The argument specification is invalid.</exception>
        [SuppressMessage("Microsoft.Performance", "CA1800:DoNotCastUnnecessarily")]
        public static ArgSpec Parse(string caller, ZilAtom? targetName, ZilAtom? activationAtom,
            IEnumerable<ZilObject> argspec, ZilDecl? bodyDecl = null)
        {
            var optArgsStart = -1;
            var auxArgsStart = -1;

            ZilAtom? varargsAtom = null, environmentAtom = null, quoteAtom = null;
            bool varargsQuoted = false;
            ZilObject? varargsDecl = null, valueDecl = null;

            ZilAtom? callAtom = null;
            ZilObject? callDecl = null;

            var argAtoms = new List<ZilAtom>();
            var argDecls = new List<ZilObject?>();
            var argQuoted = new List<bool>();
            var argDefaults = new List<ZilObject?>();

            const int OO_None = 0;
            const int OO_Varargs = 1;
            const int OO_Activation = 2;
            const int OO_Value = 3;
            const int OO_Environment = 4;
            const int OO_Call = 5;

            int cur = 0;
            int oneOffMode = OO_None;
            ZilObject? oneOffTag = null;

            foreach (var arg in argspec)
            {
                // check for arg clause separators: "OPT", "AUX", etc.
                if (arg is ZilString sep)
                {
                    switch (sep.Text)
                    {
                        case "OPT":
                        case "OPTIONAL":
                            if (callAtom != null)
                                throw new InterpreterError(InterpreterMessages._0_CALL_Clause_Must_Not_Be_Combined_With_Other_Argument_Bindings, caller);
                            if (optArgsStart != -1)
                                throw new InterpreterError(InterpreterMessages._0_Multiple_1_Clauses, caller, "\"OPT\"");
                            if (auxArgsStart != -1)
                                throw new InterpreterError(InterpreterMessages._0_OPT_After_AUX, caller);
                            optArgsStart = cur;
                            continue;
                        case "AUX":
                        case "EXTRA":
                            if (callAtom != null)
                                throw new InterpreterError(InterpreterMessages._0_CALL_Clause_Must_Not_Be_Combined_With_Other_Argument_Bindings, caller);
                            if (auxArgsStart != -1)
                                throw new InterpreterError(InterpreterMessages._0_Multiple_1_Clauses, caller, "\"AUX\"");
                            auxArgsStart = cur;
                            continue;
                        case "ARGS":
                            if (callAtom != null)
                                throw new InterpreterError(InterpreterMessages._0_CALL_Clause_Must_Not_Be_Combined_With_Other_Argument_Bindings, caller);
                            varargsQuoted = true;
                            goto case "TUPLE";
                        case "TUPLE":
                            if (callAtom != null)
                                throw new InterpreterError(InterpreterMessages._0_CALL_Clause_Must_Not_Be_Combined_With_Other_Argument_Bindings, caller);
                            if (varargsAtom != null)
                                throw new InterpreterError(InterpreterMessages._0_Multiple_1_Clauses, caller, "\"ARGS\" or \"TUPLE\"");
                            oneOffMode = OO_Varargs;
                            oneOffTag = arg;
                            continue;
                        case "CALL":
                            if (callAtom != null)
                                throw new InterpreterError(InterpreterMessages._0_Multiple_1_Clauses, caller, "\"CALL\"");
                            if (cur != 0 || optArgsStart != -1 || auxArgsStart != -1 || varargsAtom != null)
                                throw new InterpreterError(InterpreterMessages._0_CALL_Clause_Must_Not_Be_Combined_With_Other_Argument_Bindings, caller);
                            oneOffMode = OO_Call;
                            oneOffTag = arg;
                            continue;
                        case "NAME":
                        case "ACT":
                            if (activationAtom != null)
                                throw new InterpreterError(InterpreterMessages._0_Multiple_1_Clauses, caller, "\"NAME\" or activation atom");
                            oneOffMode = OO_Activation;
                            oneOffTag = arg;
                            continue;
                        case "BIND":
                            if (environmentAtom != null)
                                throw new InterpreterError(InterpreterMessages._0_Multiple_1_Clauses, caller, "\"BIND\"");
                            oneOffMode = OO_Environment;
                            oneOffTag = arg;
                            continue;
                        case "VALUE":
                            if (valueDecl != null)
                                throw new InterpreterError(InterpreterMessages._0_Multiple_1_Clauses, caller, "\"VALUE\"");
                            oneOffMode = OO_Value;
                            oneOffTag = arg;
                            continue;
                        default:
                            throw new InterpreterError(
                                InterpreterMessages._0_Unrecognized_Clause_In_Arg_Spec_1,
                                caller,
                                arg.ToString());
                    }
                }

                // handle one-offs
                switch (oneOffMode)
                {
                    case OO_Varargs:
                        switch (arg)
                        {
                            case ZilAtom atom:
                                varargsAtom = atom;
                                break;

                            case ZilAdecl adecl:
                                varargsDecl = adecl.Second;
                                varargsAtom = (ZilAtom)adecl.First;
                                break;

                            default:
                                throw new InterpreterError(InterpreterMessages._0_Expected_1_After_2, caller, "an atom", oneOffTag!);
                        }

                        oneOffMode = OO_None;
                        continue;

                    case OO_Activation:
                        activationAtom = arg as ZilAtom;
                        if (activationAtom == null)
                            throw new InterpreterError(InterpreterMessages._0_Expected_1_After_2, caller, "an atom", oneOffTag!);

                        oneOffMode = OO_None;
                        continue;

                    case OO_Environment:
                        environmentAtom = arg as ZilAtom;
                        if (environmentAtom == null)
                            throw new InterpreterError(InterpreterMessages._0_Expected_1_After_2, caller, "an atom", oneOffTag!);

                        oneOffMode = OO_None;
                        continue;

                    case OO_Value:
                        valueDecl = arg;
                        oneOffMode = OO_None;
                        continue;

                    case OO_Call:
                        switch (arg)
                        {
                            case ZilAtom atom:
                                callAtom = atom;
                                break;

                            case ZilAdecl adecl:
                                callDecl = adecl.Second;
                                callAtom = (ZilAtom)adecl.First;
                                break;

                            default:
                                throw new InterpreterError(InterpreterMessages._0_Expected_1_After_2, caller, "an atom", oneOffTag!);
                        }

                        oneOffMode = OO_None;
                        continue;
                }

                // it's a real arg
                if (callAtom != null)
                    throw new InterpreterError(InterpreterMessages._0_CALL_Clause_Must_Not_Be_Combined_With_Other_Argument_Bindings, caller);

                cur++;

                bool quoted = false;
                ZilObject argName;
                ZilObject? argValue, argDecl;

                // could be an atom or a list: (atom defaultValue)
                if (arg is ZilList al && arg is not ZilForm)
                {
                    if (al.IsEmpty)
                        throw new InterpreterError(InterpreterMessages._0_Empty_List_In_Arg_Spec, caller);

                    if (!al.HasLength(2) || (auxArgsStart == -1 && optArgsStart == -1))
                    {
                        throw new InterpreterError(InterpreterMessages._0_Must_Have_1_Element1s, "binding", 2);
                    }

                    argName = al.First!;
                    argValue = al.Rest!.First!;
                }
                else
                {
                    argName = arg;
                    argValue = null;
                }

                // could be an ADECL
                if (argName is ZilAdecl aad)
                {
                    argDecl = aad.Second;
                    argName = aad.First;
                }
                else
                {
                    argDecl = null;
                }

                // could be quoted
                if (argName is ZilForm af)
                {
                    if (af is (ZilAtom { StdAtom: StdAtom.QUOTE } head, ({ } name, _)))
                    {
                        quoted = true;
                        quoteAtom = head;
                        argName = name;
                    }
                    else
                        throw new InterpreterError(InterpreterMessages._0_Unexpected_FORM_In_Arg_Spec_1, caller, argName.ToString());
                }

                // it'd better be an atom by now
                if (argName is not ZilAtom argAtom)
                {
                    throw new InterpreterError(InterpreterMessages._0_Expected_Atom_In_Arg_Spec_But_Found_1, caller, argName.ToString());
                }

                argAtoms.Add(argAtom);
                argDecls.Add(argDecl);
                argDefaults.Add(argValue);
                argQuoted.Add(quoted);
            }

            if (auxArgsStart == -1)
                auxArgsStart = cur;
            if (optArgsStart == -1)
                optArgsStart = auxArgsStart;

            // process #DECL in body
            if (bodyDecl != null)
            {
                var argIndex = argAtoms.Select((atom, i) => new { atom, i }).ToLookup(p => p.atom, p => p.i);

                foreach (var pair in bodyDecl.GetAtomDeclPairs())
                {
                    var atom = pair.Key;
                    var decl = pair.Value;
                    ZilObject? prev;

                    if (atom.StdAtom == StdAtom.VALUE)
                    {
                        prev = valueDecl;
                        valueDecl = decl;
                    }
                    else if (atom == varargsAtom)
                    {
                        prev = varargsDecl;
                        varargsDecl = decl;
                    }
                    else if (atom == callAtom)
                    {
                        prev = callDecl;
                        callDecl = decl;
                    }
                    else if (argIndex.Contains(atom))
                    {
                        prev = null;

                        foreach (var i in argIndex[atom])
                        {
                            prev ??= argDecls[i];
                            argDecls[i] = decl;
                        }
                    }
                    else
                    {
                        throw new InterpreterError(InterpreterMessages._0_Unrecognized_Argument_Name_In_Body_DECL_1, caller, atom);
                    }

                    if (prev != null)
                    {
                        throw new InterpreterError(InterpreterMessages._0_Conflicting_DECLs_For_Atom_1, caller, atom);
                    }
                }
            }

            return new ArgSpec(targetName, activationAtom, optArgsStart, auxArgsStart,
                varargsAtom, varargsQuoted, varargsDecl, environmentAtom, valueDecl, quoteAtom,
                callAtom, callDecl,
                argAtoms.ToArray(), argDecls.ToArray(), argQuoted.ToArray(), argDefaults.ToArray());
        }

        public int MinArgCount { get; }

        public int? MaxArgCount => VarargsAtom != null ? null : (int?)auxArgsStart;

        public IEnumerator<ArgItem> GetEnumerator()
        {
            var type = ArgItem.ArgType.Required;

            for (int i = 0; i < argAtoms.Length; i++)
            {
                if (i == auxArgsStart)
                    type = ArgItem.ArgType.Auxiliary;
                else if (i == MinArgCount)
                    type = ArgItem.ArgType.Optional;

                yield return new ArgItem(argAtoms[i], argQuoted[i], argDefaults[i], type);
            }
        }

        System.Collections.IEnumerator System.Collections.IEnumerable.GetEnumerator()
        {
            return GetEnumerator();
        }

        public string ToString(Func<ZilObject, string> convert)
        {
            var sb = new StringBuilder();
            sb.Append('(');

            bool first = true;
            foreach (var item in AsZilListBody())
            {
                if (!first)
                    sb.Append(' ');

                first = false;

                sb.Append(convert(item));
            }

            sb.Append(')');
            return sb.ToString();
        }

        public override string ToString()
        {
            return ToString(zo => zo.ToString());
        }

        public override bool Equals(object? obj)
        {
            if (obj is not ArgSpec other)
                return false;

            int numArgs = argAtoms.Length;
            if (other.argAtoms.Length != numArgs ||
                other.MinArgCount != MinArgCount ||
                other.auxArgsStart != auxArgsStart ||
                other.VarargsAtom != VarargsAtom ||
                other.varargsQuoted != varargsQuoted ||
                other.CallAtom != CallAtom)
            {
                return false;
            }

            for (int i = 0; i < numArgs; i++)
            {
                if (other.argAtoms[i] != argAtoms[i])
                    return false;

                if (other.argQuoted[i] != argQuoted[i])
                    return false;

                if (other.argDefaults[i] == null
                    ? argDefaults[i] != null
                    : !other.argDefaults[i]!.StructurallyEquals(argDefaults[i]))
                {
                    return false;
                }
            }

            return true;
        }

        public override int GetHashCode()
        {
            int result = (argAtoms.Length << 1) ^ (MinArgCount << 2) ^ (auxArgsStart << 3);

            if (VarargsAtom != null)
                result ^= VarargsAtom.GetHashCode();

            if (CallAtom != null)
                result ^= CallAtom.GetHashCode();

            result ^= varargsQuoted.GetHashCode();

            for (int i = 0; i < argAtoms.Length; i++)
            {
                result ^= argAtoms[i].GetHashCode();

                if (argDecls[i] != null)
                    result ^= argDecls[i]!.GetHashCode();

                result ^= argQuoted[i].GetHashCode();

                if (argDefaults[i] != null)
                    result ^= argDefaults[i]!.GetHashCode();
            }

            if (callDecl != null)
                result ^= callDecl.GetHashCode();

            return result;
        }

        public sealed class Application : IDisposable
        {
            public Context Context { get; }
            public ZilResult? EarlyResult { get; }
            public ZilActivation? Activation { get; }
            public LocalEnvironment? Environment { get; }
            public bool WasTopLevel { get; }

            public Application(Context ctx, ZilActivation? act, LocalEnvironment? env, bool wasTopLevel)
            {
                Context = ctx;
                Activation = act;
                Environment = env;
                WasTopLevel = wasTopLevel;
            }

            public Application(Context ctx, ZilResult earlyResult, bool wasTopLevel)
            {
                Context = ctx;
                EarlyResult = earlyResult;
                WasTopLevel = wasTopLevel;
            }

            void IDisposable.Dispose()
            {
                GC.SuppressFinalize(this);

                Activation?.Dispose();
                ((IDisposable?)Environment)?.Dispose();
                Context.AtTopLevel = WasTopLevel;
            }

            ~Application()
            {
                ((IDisposable)this).Dispose();
            }
        }

        /// <summary>
        /// Evaluates the arguments to a function call.
        /// </summary>
        sealed class ArgEvaluator : IDisposable
        {
            readonly Context ctx;
            readonly LocalEnvironment env;
            readonly Action throwWrongCount;
            IEnumerator<ZilObject>? enumerator;
            IEnumerator<ZilResult>? expansion;

            public ArgEvaluator(Context ctx, LocalEnvironment env, IEnumerable<ZilObject> rawArgs,
                Action throwWrongCount)
            {
                this.ctx = ctx;
                this.env = env;
                this.throwWrongCount = throwWrongCount;

                enumerator = rawArgs.GetEnumerator();
            }

            public void Dispose()
            {
                try
                {
                    enumerator?.Dispose();
                    expansion?.Dispose();
                }
                finally
                {
                    enumerator = null;
                    expansion = null;
                }
            }

            /// <exception cref="InterpreterError">The wrong number of arguments were provided.</exception>
            [DoesNotReturn]
            void DoThrowWrongCount()
            {
                throwWrongCount();
                throw new UnreachableCodeException();
            }

            /// <exception cref="InterpreterError">Too few arguments were provided.</exception>
            public ZilResult GetOne(bool eval, out IProvideSourceLine src)
            {
                var result = GetOneOptional(eval, out var src2);

                if (result != null)
                {
                    src = src2!;
                    return result.Value;
                }

                DoThrowWrongCount();
                throw new UnreachableCodeException();
            }

            /// <exception cref="InterpreterError">The wrong number or types of arguments were provided.</exception>
            [return: NotNullIfNotNull(nameof(src))]
            public ZilResult? GetOneOptional(bool eval, out IProvideSourceLine? src)
            {
                while (true)
                {
                    if (enumerator == null)
                    {
                        src = null;
                        return null;
                    }

                    if (expansion != null)
                    {
                        if (expansion.MoveNext())
                        {
                            if (!eval)
                            {
                                DoThrowWrongCount();
                            }

                            var zr = expansion.Current;
                            if (zr.ShouldPass())
                            {
                                src = null;
                                return zr;
                            }

                            src = (ZilObject)zr;
                            return zr;
                        }

                        expansion.Dispose();
                        expansion = null;
                    }

                    if (enumerator.MoveNext())
                    {
                        var result = enumerator.Current;

                        if (!eval)
                        {
                            src = result;
                            return result;
                        }

                        if (result is IMayExpandBeforeEvaluation expandableBefore && expandableBefore.ShouldExpandBeforeEvaluation)
                        {
                            expansion = expandableBefore.ExpandBeforeEvaluation(ctx, env).GetEnumerator();
                            continue;
                        }

                        var zr = result.Eval(ctx, env);
                        if (zr.ShouldPass())
                        {
                            src = null;
                            return zr;
                        }

                        result = (ZilObject)zr;

                        if (result is IMayExpandAfterEvaluation expandableAfter && expandableAfter.ShouldExpandAfterEvaluation)
                        {
                            expansion = expandableAfter.ExpandAfterEvaluation().AsResultSequence().GetEnumerator();
                            continue;
                        }

                        src = result;
                        return result;
                    }

                    enumerator.Dispose();
                    enumerator = null;
                }
            }

            /// <exception cref="InterpreterError">The wrong number or types of arguments were provided.</exception>
            public IEnumerable<ZilResult> GetRest(bool eval)
            {
                ZilResult? zr;

                while ((zr = GetOneOptional(eval, out _)) != null)
                    yield return zr.Value;
            }

            /// <exception cref="InterpreterError">The wrong number or types of arguments were provided.</exception>
            public void NoMoreArguments()
            {
                if (GetOneOptional(false, out _) != null)
                    DoThrowWrongCount();
            }
        }

        /// <summary>
        /// Pushes a new local environment and binds argument values in preparation for a function call.
        /// </summary>
        /// <param name="ctx">The context.</param>
        /// <param name="args">The unevaluated arguments provided at the call site.</param>
        /// <param name="eval"><see langword="true"/> if any provided arguments corresponding to unquoted argument atoms should be evaluated.</param>
        /// <returns>An object containing the new activation and other data needed to restore state, which must be disposed to
        /// restore the state.</returns>
        /// <exception cref="InterpreterError">The wrong number or types of arguments were provided.</exception>
        /// <remarks>
        /// <para>This method may throw other exceptions if an error occurs while processing argument values.
        /// In the case of an exception, the new local environment will not be pushed.</para>
        /// <para>Make sure to call <see cref="IDisposable.Dispose"/> on the returned object!</para>
        /// </remarks>
        [SuppressMessage("Microsoft.Reliability", "CA2000:Dispose objects before losing scope")]
        public Application BeginApply(Context ctx, ZilObject[] args, bool eval)
        {
            var outerEnv = ctx.LocalEnvironment;
            var innerEnv = ctx.PushEnvironment();

            var wasTopLevel = ctx.AtTopLevel;
            ctx.AtTopLevel = false;

            void ThrowWrongCount()
            {
                throw ArgumentCountError.WrongCount(
                    new FunctionCallSite(Name?.ToString() ?? "user-defined function"),
                    MinArgCount,
                    VarargsAtom == null ? auxArgsStart : null);
            }

            try
            {
                using var evaluator = new ArgEvaluator(ctx, outerEnv, args, ThrowWrongCount);

                if (EnvironmentAtom != null)
                {
                    innerEnv.Rebind(EnvironmentAtom,
                        new ZilEnvironment(outerEnv, EnvironmentAtom),
                        ctx.GetStdAtom(StdAtom.ENVIRONMENT));
                }

                if (CallAtom != null)
                {
                    CallFrame? callFrame = null;
                    for (Frame? frame = ctx.TopFrame; frame != null; frame = frame.Parent)
                    {
                        if (frame is CallFrame cf)
                        {
                            callFrame = cf;
                            break;
                        }
                    }

                    if (callFrame != null)
                    {
                        var callingForm = callFrame.CallingForm;
                        ctx.MaybeCheckDecl(callingForm, callingForm, callDecl, "argument {0}", CallAtom);
                        innerEnv.Rebind(CallAtom, callingForm, callDecl);
                    }
                    else
                    {
                        // No calling FORM is available (e.g. native invocation). Bind as unassigned.
                        innerEnv.Rebind(CallAtom, null, callDecl);
                    }
                }

                IProvideSourceLine? src;

                for (int i = 0; i < MinArgCount; i++)
                {
                    var zr = evaluator.GetOne(eval && !argQuoted[i], out src);
                    if (zr.ShouldPass())
                        return new Application(ctx, zr, wasTopLevel);
                    ctx.MaybeCheckDecl(src, (ZilObject)zr, argDecls[i], "argument {0}", argAtoms[i]);
                    innerEnv.Rebind(argAtoms[i], (ZilObject)zr, argDecls[i]);
                }

                for (int i = MinArgCount; i < auxArgsStart; i++)
                {
                    var zr = evaluator.GetOneOptional(eval && !argQuoted[i], out src);
                    if (zr != null)
                    {
                        if (zr.Value.ShouldPass())
                            return new Application(ctx, zr.Value, wasTopLevel);

                        ctx.MaybeCheckDecl(src!, (ZilObject)zr.Value, argDecls[i], "argument {0}", argAtoms[i]);
                        innerEnv.Rebind(argAtoms[i], (ZilObject)zr.Value, argDecls[i]);
                    }
                    else
                    {
                        var init = argDefaults[i]?.Eval(ctx);
                        if (init != null)
                        {
                            if (init.Value.ShouldPass())
                                return new Application(ctx, init.Value, wasTopLevel);

                            ctx.MaybeCheckDecl(argDefaults[i]!, (ZilObject)init.Value, argDecls[i], "default for argument {0}", argAtoms[i]);
                        }
                        innerEnv.Rebind(argAtoms[i], init == null ? null : (ZilObject)init.Value, argDecls[i]);
                    }
                }

                if (VarargsAtom != null)
                {
                    var result = evaluator.GetRest(eval && !varargsQuoted).ToZilListResult(null);
                    if (result.ShouldPass())
                        return new Application(ctx, result, wasTopLevel);

                    var value = (ZilObject)result;
                    ctx.MaybeCheckDecl(value, varargsDecl, "argument {0}", VarargsAtom);
                    innerEnv.Rebind(VarargsAtom, value, varargsDecl);
                }

                for (int i = auxArgsStart; i < argAtoms.Length; i++)
                {
                    var zr = argDefaults[i]?.Eval(ctx);
                    if (zr != null)
                    {
                        if (zr.Value.ShouldPass())
                            return new Application(ctx, zr.Value, wasTopLevel);

                        ctx.MaybeCheckDecl(argDefaults[i]!, (ZilObject)zr.Value, argDecls[i], "default for argument {0}", argAtoms[i]);
                    }
                    innerEnv.Rebind(argAtoms[i], zr == null ? null : (ZilObject)zr.Value, argDecls[i]);
                }

                evaluator.NoMoreArguments();

                ZilActivation? activation;
                if (ActivationAtom != null)
                {
                    activation = new ZilActivation(ActivationAtom);
                    innerEnv.Rebind(ActivationAtom, activation, ctx.GetStdAtom(StdAtom.ACTIVATION));
                }
                else
                {
                    activation = null;
                }

                /* make an unassigned binding so RETURN and AGAIN won't use the
                 * activation of a PROG outside the newly entered function unless
                 * explicitly told to */
                innerEnv.Rebind(ctx.EnclosingProgActivationAtom);

                return new Application(ctx, activation, innerEnv, wasTopLevel);
            }
            catch (InterpreterError)
            {
                // this is a separate block to satisfy Exceptional...

                // pop the environment so the caller doesn't have to
                ctx.PopEnvironment();
                ctx.AtTopLevel = wasTopLevel;
                throw;
            }
            catch
            {
                // pop the environment so the caller doesn't have to
                ctx.PopEnvironment();
                ctx.AtTopLevel = wasTopLevel;
                // ReSharper disable once ExceptionNotDocumented
                throw;
            }
        }

        /// <exception cref="DeclCheckError"><paramref name="result"/> did not match the required pattern, and
        /// <paramref name="ctx"/>.<see cref="Context.CheckDecls"/> is <see langword="true"/>.</exception>
        public void ValidateResult(Context ctx, ZilObject result)
        {
            ctx.MaybeCheckDecl(result, valueDecl, "return value of {0}", (object?)Name ?? "user-defined function");
        }

        public ZilList ToZilList()
        {
            return new ZilList(AsZilListBody());
        }

        public IEnumerable<ZilObject> AsZilListBody()
        {
            bool emittedVarargs = false;

            if (EnvironmentAtom != null)
            {
                yield return ZilString.FromString("BIND");
                yield return EnvironmentAtom;
            }

            if (CallAtom != null)
            {
                yield return ZilString.FromString("CALL");
                if (callDecl == null)
                {
                    yield return CallAtom;
                }
                else
                {
                    yield return new ZilAdecl(CallAtom, callDecl);
                }
            }

            for (int i = 0; i < argAtoms.Length; i++)
            {
                if (i == auxArgsStart)
                {
                    if (VarargsAtom != null)
                    {
                        yield return ZilString.FromString(varargsQuoted ? "ARGS" : "TUPLE");
                        if (varargsDecl == null)
                        {
                            yield return VarargsAtom;
                        }
                        else
                        {
                            yield return new ZilAdecl(VarargsAtom, varargsDecl);
                        }
                        emittedVarargs = true;
                    }

                    yield return ZilString.FromString("AUX");
                }
                else if (i == MinArgCount)
                {
                    yield return ZilString.FromString("OPT");
                }

                ZilObject arg = argAtoms[i];

                if (argQuoted[i])
                {
                    arg = new ZilForm(new[] { quoteAtom!, arg });
                }

                if (argDecls[i] != null)
                {
                    arg = new ZilAdecl(arg, argDecls[i]!);
                }

                if (argDefaults[i] != null)
                {
                    arg = new ZilList(arg,
                        new ZilList(argDefaults[i],
                            new ZilList(null, null)));
                }

                yield return arg;
            }

            if (VarargsAtom != null && !emittedVarargs)
            {
                yield return ZilString.FromString(varargsQuoted ? "ARGS" : "TUPLE");
                if (varargsDecl == null)
                {
                    yield return VarargsAtom;
                }
                else
                {
                    yield return new ZilAdecl(VarargsAtom, varargsDecl);
                }
            }

            if (ActivationAtom != null)
            {
                yield return ZilString.FromString("NAME");
                yield return ActivationAtom;
            }

            if (valueDecl != null)
            {
                yield return ZilString.FromString("VALUE");
                yield return valueDecl;
            }
        }

        public ZilAtom? Name { get; }

        public ZilAtom? ActivationAtom { get; }

        public ZilAtom? EnvironmentAtom { get; }

        public ZilAtom? VarargsAtom { get; }

        public ZilAtom? CallAtom { get; }
    }
}