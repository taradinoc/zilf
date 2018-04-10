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
using System.Linq;
using System.Text;
using Zilf.Interpreter.Values;
using Zilf.Language;
using Zilf.Diagnostics;
using JetBrains.Annotations;
using System.Diagnostics;
using Zilf.Common;

namespace Zilf.Interpreter
{
    class ArgSpec : IEnumerable<ArgItem>
    {
        // name of the function to which this spec belongs
        [CanBeNull]
        readonly ZilAtom name;
        // reference to the "QUOTE" atom used for any quoted args
        [CanBeNull]
        readonly ZilAtom quoteAtom;

        // "BIND"
        [CanBeNull]
        readonly ZilAtom environmentAtom;

        // regular args, "OPT", and "AUX"
        [NotNull]
        readonly ZilAtom[] argAtoms;
        [NotNull]
        readonly ZilObject[] argDecls;
        [NotNull]
        readonly bool[] argQuoted;
        [NotNull]
        readonly ZilObject[] argDefaults;
        readonly int optArgsStart, auxArgsStart;

        // "ARGS" or "TUPLE"
        [CanBeNull]
        readonly ZilAtom varargsAtom;
        [CanBeNull]
        readonly ZilObject varargsDecl;
        readonly bool varargsQuoted;

        // "NAME"/"ACT"
        [CanBeNull]
        readonly ZilAtom activationAtom;

        // "VALUE"
        [CanBeNull]
        readonly ZilObject valueDecl;

        ArgSpec([CanBeNull] ZilAtom name, [CanBeNull] ZilAtom activationAtom, int optArgsStart, int auxArgsStart,
            [CanBeNull] ZilAtom varargsAtom, bool varargsQuoted, [CanBeNull] ZilObject varargsDecl,
            [CanBeNull] ZilAtom environmentAtom, [CanBeNull] ZilObject valueDecl, [CanBeNull] ZilAtom quoteAtom,
            [NotNull] ZilAtom[] argAtoms,
            [NotNull] ZilObject[] argDecls, [NotNull] bool[] argQuoted, [NotNull] ZilObject[] argDefaults)
        {
            this.name = name;
            this.activationAtom = activationAtom;
            this.optArgsStart = optArgsStart;
            this.auxArgsStart = auxArgsStart;
            this.varargsAtom = varargsAtom;
            this.varargsQuoted = varargsQuoted;
            this.varargsDecl = varargsDecl;
            this.environmentAtom = environmentAtom;
            this.valueDecl = valueDecl;
            this.quoteAtom = quoteAtom;
            this.argAtoms = argAtoms;
            this.argDecls = argDecls;
            this.argQuoted = argQuoted;
            this.argDefaults = argDefaults;
        }

        /// <exception cref="InterpreterError">The argument specification is invalid.</exception>
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Performance", "CA1800:DoNotCastUnnecessarily")]
        [NotNull]
        public static ArgSpec Parse([NotNull] string caller, [CanBeNull] ZilAtom targetName, [CanBeNull] ZilAtom activationAtom,
            [NotNull] IEnumerable<ZilObject> argspec, [CanBeNull] ZilDecl bodyDecl = null)
        {
            Contract.Requires(caller != null);
            Contract.Requires(argspec != null && Contract.ForAll(argspec, a => a != null));
            Contract.Ensures(Contract.Result<ArgSpec>() != null);

            var optArgsStart = -1;
            var auxArgsStart = -1;

            ZilAtom varargsAtom = null, environmentAtom = null, quoteAtom = null;
            bool varargsQuoted = false;
            ZilObject varargsDecl = null, valueDecl = null;

            var argAtoms = new List<ZilAtom>();
            var argDecls = new List<ZilObject>();
            var argQuoted = new List<bool>();
            var argDefaults = new List<ZilObject>();

            const int OO_None = 0;
            const int OO_Varargs = 1;
            const int OO_Activation = 2;
            const int OO_Value = 3;
            const int OO_Environment = 4;

            int cur = 0;
            int oneOffMode = OO_None;
            ZilObject oneOffTag = null;

            foreach (var arg in argspec)
            {
                // check for arg clause separators: "OPT", "AUX", etc.
                if (arg is ZilString sep)
                {
                    switch (sep.Text)
                    {
                        case "OPT":
                        case "OPTIONAL":
                            if (optArgsStart != -1)
                                throw new InterpreterError(InterpreterMessages._0_Multiple_1_Clauses, caller, "\"OPT\"");
                            if (auxArgsStart != -1)
                                throw new InterpreterError(InterpreterMessages._0_OPT_After_AUX, caller);
                            optArgsStart = cur;
                            continue;
                        case "AUX":
                        case "EXTRA":
                            if (auxArgsStart != -1)
                                throw new InterpreterError(InterpreterMessages._0_Multiple_1_Clauses, caller, "\"AUX\"");
                            auxArgsStart = cur;
                            continue;
                        case "ARGS":
                            varargsQuoted = true;
                            goto case "TUPLE";
                        case "TUPLE":
                            if (varargsAtom != null)
                                throw new InterpreterError(InterpreterMessages._0_Multiple_1_Clauses, caller, "\"ARGS\" or \"TUPLE\"");
                            oneOffMode = OO_Varargs;
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
                                InterpreterMessages._0_Unrecognized_1_2,
                                caller,
                                "clause in arg spec",
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
                                throw new InterpreterError(InterpreterMessages._0_Expected_1_After_2, caller, "an atom", oneOffTag);
                        }

                        oneOffMode = OO_None;
                        continue;

                    case OO_Activation:
                        activationAtom = arg as ZilAtom;
                        if (activationAtom == null)
                            throw new InterpreterError(InterpreterMessages._0_Expected_1_After_2, caller, "an atom", oneOffTag);

                        oneOffMode = OO_None;
                        continue;

                    case OO_Environment:
                        environmentAtom = arg as ZilAtom;
                        if (environmentAtom == null)
                            throw new InterpreterError(InterpreterMessages._0_Expected_1_After_2, caller, "an atom", oneOffTag);

                        oneOffMode = OO_None;
                        continue;

                    case OO_Value:
                        valueDecl = arg;
                        oneOffMode = OO_None;
                        continue;
                }

                // it's a real arg
                cur++;

                bool quoted = false;
                ZilObject argName, argValue, argDecl;

                // could be an atom or a list: (atom defaultValue)
                if (arg is ZilList al && !(arg is ZilForm))
                {
                    if (al.IsEmpty)
                        throw new InterpreterError(InterpreterMessages._0_Empty_List_In_Arg_Spec, caller);

                    // TODO: report error if length != 2, or if in required args section
                    argName = al.First;
                    argValue = al.Rest.First;
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
                    if (af.First is ZilAtom head && head.StdAtom == StdAtom.QUOTE &&
                        !af.Rest.IsEmpty)
                    {
                        quoted = true;
                        quoteAtom = head;
                        argName = af.Rest.First;
                    }
                    else
                        throw new InterpreterError(InterpreterMessages._0_Unexpected_FORM_In_Arg_Spec_1, caller, argName.ToString());
                }

                // it'd better be an atom by now
                if (!(argName is ZilAtom argAtom))
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
                    ZilObject prev;

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
                    else if (argIndex.Contains(atom))
                    {
                        prev = null;

                        foreach (var i in argIndex[atom])
                        {
                            prev = prev ?? argDecls[i];
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
                argAtoms.ToArray(), argDecls.ToArray(), argQuoted.ToArray(), argDefaults.ToArray());
        }

        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Performance", "CA1822:MarkMembersAsStatic")]
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Performance", "CA1811:AvoidUncalledPrivateCode")]
        [ContractInvariantMethod]
        [Conditional("CONTRACTS_FULL")]
        void ObjectInvariant()
        {
            Contract.Invariant(argAtoms != null && Contract.ForAll(argAtoms, a => a != null));
            Contract.Invariant(argDecls != null && argDecls.Length == argAtoms.Length);
            Contract.Invariant(argDefaults != null && argDefaults.Length == argAtoms.Length);
            Contract.Invariant(argQuoted != null && argQuoted.Length == argAtoms.Length);
            Contract.Invariant(argDefaults != null && argDefaults.Length == argAtoms.Length);
            Contract.Invariant(optArgsStart >= 0 && optArgsStart <= argAtoms.Length);
            Contract.Invariant(auxArgsStart >= optArgsStart && auxArgsStart <= argAtoms.Length);
            Contract.Invariant(varargsAtom != null || !varargsQuoted);
            Contract.Invariant(MinArgCount >= 0);
            Contract.Invariant(MaxArgCount == null || MaxArgCount >= MinArgCount);
        }

        public int MinArgCount => optArgsStart;

        public int? MaxArgCount => varargsAtom != null ? null : (int?)auxArgsStart;

        public IEnumerator<ArgItem> GetEnumerator()
        {
            var type = ArgItem.ArgType.Required;

            for (int i = 0; i < argAtoms.Length; i++)
            {
                if (i == auxArgsStart)
                    type = ArgItem.ArgType.Auxiliary;
                else if (i == optArgsStart)
                    type = ArgItem.ArgType.Optional;

                yield return new ArgItem(argAtoms[i], argQuoted[i], argDefaults[i], type);
            }
        }

        System.Collections.IEnumerator System.Collections.IEnumerable.GetEnumerator()
        {
            return GetEnumerator();
        }

        [NotNull]
        public string ToString([NotNull] Func<ZilObject, string> convert)
        {
            Contract.Requires(convert != null);
            Contract.Ensures(Contract.Result<string>() != null);
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

        public override bool Equals(object obj)
        {
            if (!(obj is ArgSpec other))
                return false;

            int numArgs = argAtoms.Length;
            if (other.argAtoms.Length != numArgs ||
                other.optArgsStart != optArgsStart ||
                other.auxArgsStart != auxArgsStart ||
                other.varargsAtom != varargsAtom ||
                other.varargsQuoted != varargsQuoted)
                return false;

            for (int i = 0; i < numArgs; i++)
            {
                if (other.argAtoms[i] != argAtoms[i])
                    return false;
                if (other.argQuoted[i] != argQuoted[i])
                    return false;
                if (other.argDefaults[i] == null
                    ? argDefaults[i] != null
                    : !other.argDefaults[i].StructurallyEquals(argDefaults[i]))
                    return false;
            }

            return true;
        }

        public override int GetHashCode()
        {
            int result = (argAtoms.Length << 1) ^ (optArgsStart << 2) ^ (auxArgsStart << 3);

            if (varargsAtom != null)
                result ^= varargsAtom.GetHashCode();

            result ^= varargsQuoted.GetHashCode();

            for (int i = 0; i < argAtoms.Length; i++)
            {
                result ^= argAtoms[i].GetHashCode();

                if (argDecls[i] != null)
                    result ^= argDecls[i].GetHashCode();

                result ^= argQuoted[i].GetHashCode();

                if (argDefaults[i] != null)
                    result ^= argDefaults[i].GetHashCode();
            }

            return result;
        }

        public sealed class Application : IDisposable
        {
            public Context Context { get; }
            public ZilResult? EarlyResult { get; }
            public ZilActivation Activation { get; }
            public LocalEnvironment Environment { get; }
            public bool WasTopLevel { get; }

            public Application(Context ctx, ZilActivation act, LocalEnvironment env, bool wasTopLevel)
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
                ((IDisposable)Environment)?.Dispose();
                Context.AtTopLevel = WasTopLevel;
            }

            ~Application()
            {
                ((IDisposable)this).Dispose();
            }
        }

        class ArgEvaluator : IDisposable
        {
            readonly Context ctx;
            readonly LocalEnvironment env;
            readonly Action throwWrongCount;
            IEnumerator<ZilObject> enumerator;
            IEnumerator<ZilResult> expansion;

            public ArgEvaluator([NotNull] Context ctx, [NotNull] LocalEnvironment env, [NotNull] IEnumerable<ZilObject> rawArgs,
                [NotNull] Action throwWrongCount)
            {
                Contract.Requires(ctx != null);
                Contract.Requires(env != null);
                Contract.Requires(rawArgs != null);
                Contract.Requires(throwWrongCount != null);

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
            [ContractAnnotation("=> halt")]
            void DoThrowWrongCount()
            {
                throwWrongCount();
            }

            /// <exception cref="InterpreterError">Too few arguments were provided.</exception>
            [ContractAnnotation("=> src: notnull")]
            public ZilResult GetOne(bool eval, out IProvideSourceLine src)
            {
                Contract.Ensures(Contract.ValueAtReturn(out src) != null);

                var result = GetOneOptional(eval, out src);

                if (result != null)
                    return result.Value;

                DoThrowWrongCount();
                throw new UnreachableCodeException();
            }

            /// <exception cref="InterpreterError">The wrong number or types of arguments were provided.</exception>
            public ZilResult? GetOneOptional(bool eval, out IProvideSourceLine src)
            {
                Contract.Ensures(Contract.ValueAtReturn(out src) != null || Contract.Result<ZilObject>() == null);

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

                            src = (ZilObject)expansion.Current;
                            return expansion.Current;
                        }

                        expansion.Dispose();
                        expansion = null;
                    }

                    if (enumerator.MoveNext())
                    {
                        var result = enumerator.Current;
                        Contract.Assert(result != null);

                        if (!eval)
                        {
                            src = result;
                            return result;
                        }

                        if (result is IMayExpandBeforeEvaluation expandable && expandable.ShouldExpandBeforeEvaluation)
                        {
                            expansion = expandable.ExpandBeforeEvaluation(ctx, env).GetEnumerator();
                            continue;
                        }

                        src = result;
                        return result.Eval(ctx, env);
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
        [NotNull]
        [MustUseReturnValue]
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Reliability", "CA2000:Dispose objects before losing scope")]
        public Application BeginApply([NotNull] [ProvidesContext] Context ctx, [ItemNotNull] [NotNull] ZilObject[] args, bool eval)
        {
            Contract.Requires(ctx != null);
            Contract.Requires(args != null);
            Contract.Requires(Contract.ForAll(args, a => a != null));
            Contract.Ensures(Contract.Result<Application>() != null);

            var outerEnv = ctx.LocalEnvironment;
            var innerEnv = ctx.PushEnvironment();

            var wasTopLevel = ctx.AtTopLevel;
            ctx.AtTopLevel = false;

            void ThrowWrongCount() => throw ArgumentCountError.WrongCount(
                new FunctionCallSite(name?.ToString() ?? "user-defined function"), optArgsStart, auxArgsStart);

            try
            {
                using (var evaluator = new ArgEvaluator(ctx, outerEnv, args, ThrowWrongCount))
                {
                    if (environmentAtom != null)
                    {
                        innerEnv.Rebind(environmentAtom,
                            new ZilEnvironment(outerEnv, environmentAtom),
                            ctx.GetStdAtom(StdAtom.ENVIRONMENT));
                    }

                    IProvideSourceLine src;

                    for (int i = 0; i < optArgsStart; i++)
                    {
                        var zr = evaluator.GetOne(eval && !argQuoted[i], out src);
                        if (zr.ShouldPass())
                            return new Application(ctx, zr, wasTopLevel);
                        ctx.MaybeCheckDecl(src, (ZilObject)zr, argDecls[i], "argument {0}", argAtoms[i]);
                        innerEnv.Rebind(argAtoms[i], (ZilObject)zr, argDecls[i]);
                    }

                    for (int i = optArgsStart; i < auxArgsStart; i++)
                    {
                        var zr = evaluator.GetOneOptional(eval && !argQuoted[i], out src);
                        if (zr != null)
                        {
                            if (zr.Value.ShouldPass())
                                return new Application(ctx, zr.Value, wasTopLevel);

                            ctx.MaybeCheckDecl(src, (ZilObject)zr.Value, argDecls[i], "argument {0}", argAtoms[i]);
                            innerEnv.Rebind(argAtoms[i], (ZilObject)zr.Value, argDecls[i]);
                        }
                        else
                        {
                            var init = argDefaults[i]?.Eval(ctx);
                            if (init != null)
                            {
                                if (init.Value.ShouldPass())
                                    return new Application(ctx, init.Value, wasTopLevel);

                                ctx.MaybeCheckDecl(argDefaults[i], (ZilObject)init.Value, argDecls[i], "default for argument {0}", argAtoms[i]);
                            }
                            innerEnv.Rebind(argAtoms[i], init == null ? null : (ZilObject)init.Value, argDecls[i]);
                        }
                    }

                    for (int i = auxArgsStart; i < argAtoms.Length; i++)
                    {
                        var zr = argDefaults[i]?.Eval(ctx);
                        if (zr != null)
                        {
                            if (zr.Value.ShouldPass())
                                return new Application(ctx, zr.Value, wasTopLevel);

                            ctx.MaybeCheckDecl(argDefaults[i], (ZilObject)zr.Value, argDecls[i], "default for argument {0}", argAtoms[i]);
                        }
                        innerEnv.Rebind(argAtoms[i], zr == null ? null : (ZilObject)zr.Value, argDecls[i]);
                    }

                    if (varargsAtom != null)
                    {
                        var result = evaluator.GetRest(eval && !varargsQuoted).ToZilListResult(null);
                        if (result.ShouldPass())
                            return new Application(ctx, result, wasTopLevel);

                        var value = (ZilObject)result;
                        ctx.MaybeCheckDecl(value, varargsDecl, "argument {0}", varargsAtom);
                        innerEnv.Rebind(varargsAtom, value, varargsDecl);
                    }

                    evaluator.NoMoreArguments();
                }

                ZilActivation activation;
                if (activationAtom != null)
                {
                    activation = new ZilActivation(activationAtom);
                    innerEnv.Rebind(activationAtom, activation, ctx.GetStdAtom(StdAtom.ACTIVATION));
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
        public void ValidateResult([NotNull] [ProvidesContext] Context ctx, [NotNull] ZilObject result)
        {
            Contract.Requires(ctx != null);
            Contract.Requires(result != null);
            ctx.MaybeCheckDecl(result, valueDecl, "return value of {0}", (object)name ?? "user-defined function");
        }

        [NotNull]
        public ZilList ToZilList()
        {
            Contract.Ensures(Contract.Result<ZilList>() != null);

            return new ZilList(AsZilListBody());
        }

        [ItemNotNull]
        public IEnumerable<ZilObject> AsZilListBody()
        {
            bool emittedVarargs = false;

            if (environmentAtom != null)
            {
                yield return ZilString.FromString("BIND");
                yield return environmentAtom;
            }

            for (int i = 0; i < argAtoms.Length; i++)
            {
                if (i == auxArgsStart)
                {
                    if (varargsAtom != null)
                    {
                        yield return ZilString.FromString(varargsQuoted ? "ARGS" : "TUPLE");
                        if (varargsDecl == null)
                        {
                            yield return varargsAtom;
                        }
                        else
                        {
                            yield return new ZilAdecl(varargsAtom, varargsDecl);
                        }
                        emittedVarargs = true;
                    }

                    yield return ZilString.FromString("AUX");
                }
                else if (i == optArgsStart)
                {
                    yield return ZilString.FromString("OPT");
                }

                ZilObject arg = argAtoms[i];

                if (argQuoted[i])
                {
                    arg = new ZilForm(new[] { quoteAtom, arg });
                }

                if (argDecls[i] != null)
                {
                    arg = new ZilAdecl(arg, argDecls[i]);
                }

                if (argDefaults[i] != null)
                {
                    arg = new ZilList(arg,
                        new ZilList(argDefaults[i],
                            new ZilList(null, null)));
                }

                yield return arg;
            }

            if (varargsAtom != null && !emittedVarargs)
            {
                yield return ZilString.FromString(varargsQuoted ? "ARGS" : "TUPLE");
                if (varargsDecl == null)
                {
                    yield return varargsAtom;
                }
                else
                {
                    yield return new ZilAdecl(varargsAtom, varargsDecl);
                }
            }

            if (activationAtom != null)
            {
                yield return ZilString.FromString("NAME");
                yield return activationAtom;
            }

            if (valueDecl != null)
            {
                yield return ZilString.FromString("VALUE");
                yield return valueDecl;
            }
        }

        [CanBeNull]
        public ZilAtom Name => name;

        [CanBeNull]
        public ZilAtom ActivationAtom => activationAtom;

        [CanBeNull]
        public ZilAtom EnvironmentAtom => environmentAtom;

        [CanBeNull]
        public ZilAtom VarargsAtom => varargsAtom;
    }
}