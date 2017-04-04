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

namespace Zilf.Interpreter
{
    class ArgSpec : IEnumerable<ArgItem>
    {
        // name of the function to which this spec belongs
        readonly ZilAtom name;
        // reference to the "QUOTE" atom used for any quoted args
        readonly ZilAtom quoteAtom;

        // "BIND"
        readonly ZilAtom environmentAtom;

        // regular args, "OPT", and "AUX"
        readonly ZilAtom[] argAtoms;
        readonly ZilObject[] argDecls;
        readonly bool[] argQuoted;
        readonly ZilObject[] argDefaults;
        readonly int optArgsStart, auxArgsStart;

        // "ARGS" or "TUPLE"
        readonly ZilAtom varargsAtom;
        readonly ZilObject varargsDecl;
        readonly bool varargsQuoted;

        // "NAME"/"ACT"
        readonly ZilAtom activationAtom;

        // "VALUE"
        readonly ZilObject valueDecl;

        ArgSpec(ZilAtom name, ZilAtom activationAtom, int optArgsStart, int auxArgsStart, ZilAtom varargsAtom, bool varargsQuoted, ZilObject varargsDecl,
            ZilAtom environmentAtom, ZilObject valueDecl, ZilAtom quoteAtom, ZilAtom[] argAtoms, ZilObject[] argDecls, bool[] argQuoted, ZilObject[] argDefaults)
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

        public static ArgSpec Parse(string caller, ArgSpec prev, IEnumerable<ZilObject> argspec)
        {
            Contract.Requires(caller != null);
            Contract.Requires(prev != null);
            Contract.Requires(argspec != null && Contract.ForAll(argspec, a => a != null));
            Contract.Ensures(Contract.Result<ArgSpec>() != null);

            return Parse(caller, prev.name, null, argspec);
        }

        public static ArgSpec Parse(string caller, ZilAtom targetName, ZilAtom activationAtom, IEnumerable<ZilObject> argspec, ZilDecl bodyDecl = null)
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

            foreach (ZilObject arg in argspec)
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

        [ContractInvariantMethod]
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

        public int MinArgCount
        {
            get { return optArgsStart; }
        }

        public int? MaxArgCount
        {
            get { return (varargsAtom != null) ? null : (int?)auxArgsStart; }
        }

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

        public string ToString(Func<ZilObject, string> convert)
        {
            var sb = new StringBuilder();
            sb.Append('(');

            bool first = true;
            foreach (var item in this.AsZilListBody())
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
            return this.ToString(zo => zo.ToString());
        }

        public override bool Equals(object obj)
        {
            if (!(obj is ArgSpec other))
                return false;

            int numArgs = this.argAtoms.Length;
            if (other.argAtoms.Length != numArgs ||
                other.optArgsStart != this.optArgsStart ||
                other.auxArgsStart != this.auxArgsStart ||
                other.varargsAtom != this.varargsAtom ||
                other.varargsQuoted != this.varargsQuoted)
                return false;

            for (int i = 0; i < numArgs; i++)
            {
                if (other.argAtoms[i] != this.argAtoms[i])
                    return false;
                if (other.argQuoted[i] != this.argQuoted[i])
                    return false;
                if (!object.Equals(other.argDefaults[i], this.argDefaults[i]))
                    return false;
            }

            return true;
        }

        public override int GetHashCode()
        {
            int result = (argAtoms.Length << 1) ^ (optArgsStart << 2) ^ (auxArgsStart << 3);
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

            void IDisposable.Dispose()
            {
                GC.SuppressFinalize(this);

                Activation?.Dispose();
                ((IDisposable)Environment).Dispose();
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
            IEnumerator<ZilObject> enumerator, expansion;

            public ArgEvaluator(Context ctx, LocalEnvironment env, IEnumerable<ZilObject> rawArgs, Action throwWrongCount)
            {
                Contract.Requires(ctx != null);
                Contract.Requires(rawArgs != null);

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
                    enumerator = expansion = null;
                }
            }

            public ZilObject GetOne(bool eval, out IProvideSourceLine src)
            {
                Contract.Ensures(Contract.Result<ZilObject>() != null);
                Contract.Ensures(Contract.ValueAtReturn(out src) != null);

                var result = GetOneOptional(eval, out src);

                if (result == null)
                {
                    throwWrongCount();
                    throw new InvalidOperationException();
                }

                return result;
            }

            public ZilObject GetOneOptional(bool eval, out IProvideSourceLine src)
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
                                throwWrongCount();
                                throw new InvalidOperationException();
                            }

                            src = expansion.Current;
                            return expansion.Current;
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

            public IEnumerable<ZilObject> GetRest(bool eval)
            {
                ZilObject zo;

                while ((zo = GetOneOptional(eval, out _)) != null)
                    yield return zo;
            }
        }

        /// <summary>
        /// Pushes a new local environment and binds argument values in preparation for a function call.
        /// </summary>
        /// <param name="ctx">The context.</param>
        /// <param name="args">The unevaluated arguments provided at the call site.</param>
        /// <param name="eval"><b>true</b> if any provided arguments corresponding to unquoted argument atoms should be evaluated.</param>
        /// <returns>An object containing the new activation and other data needed to restore state, which must be disposed to
        /// restore the state.</returns>
        /// <exception cref="InterpreterError">The wrong number or types of arguments were provided.</exception>
        /// <remarks>
        /// <para>This method may throw other exceptions if an error occurs while processing argument values.
        /// In the case of an exception, the new local environment will not be pushed.</para>
        /// <para>Make sure to call <see cref="IDisposable.Dispose"/> on the returned object!</para>
        /// </remarks>
        public Application BeginApply(Context ctx, ZilObject[] args, bool eval)
        {
            Contract.Requires(ctx != null);
            Contract.Requires(args != null);
            Contract.Requires(Contract.ForAll(args, a => a != null));

            var outerEnv = ctx.LocalEnvironment;
            var innerEnv = ctx.PushEnvironment();

            var wasTopLevel = ctx.AtTopLevel;
            ctx.AtTopLevel = false;

            Action throwWrongCount = () =>
            {
                throw ArgumentCountError.WrongCount(
                    new FunctionCallSite(name?.ToString() ?? "user-defined function"),
                    optArgsStart,
                    auxArgsStart);
            };

            try
            {
                using (var evaluator = new ArgEvaluator(ctx, outerEnv, args, throwWrongCount))
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
                        var value = evaluator.GetOne(eval && !argQuoted[i], out src);
                        ctx.MaybeCheckDecl(src, value, argDecls[i], "argument {0}", argAtoms[i]);
                        innerEnv.Rebind(argAtoms[i], value, argDecls[i]);
                    }

                    for (int i = optArgsStart; i < auxArgsStart; i++)
                    {
                        var value = evaluator.GetOneOptional(eval && !argQuoted[i], out src);
                        if (value != null)
                        {
                            ctx.MaybeCheckDecl(src, value, argDecls[i], "argument {0}", argAtoms[i]);
                            innerEnv.Rebind(argAtoms[i], value, argDecls[i]);
                        }
                        else
                        {
                            var init = argDefaults[i]?.Eval(ctx);
                            if (init != null)
                            {
                                ctx.MaybeCheckDecl(argDefaults[i], init, argDecls[i], "default for argument {0}", argAtoms[i]);
                            }
                            innerEnv.Rebind(argAtoms[i], init, argDecls[i]);
                        }
                    }

                    for (int i = auxArgsStart; i < argAtoms.Length; i++)
                    {
                        var init = argDefaults[i]?.Eval(ctx);
                        if (init != null)
                        {
                            ctx.MaybeCheckDecl(argDefaults[i], init, argDecls[i], "default for argument {0}", argAtoms[i]);
                        }
                        innerEnv.Rebind(argAtoms[i], argDefaults[i]?.Eval(ctx), argDecls[i]);
                    }

                    if (varargsAtom != null)
                    {
                        var value = new ZilList(evaluator.GetRest(eval && !varargsQuoted));
                        ctx.MaybeCheckDecl(value, varargsDecl, "argument {0}", varargsAtom);
                        innerEnv.Rebind(varargsAtom, value, varargsDecl);
                    }
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
                innerEnv.Rebind(ctx.EnclosingProgActivationAtom, null);

                return new Application(ctx, activation, innerEnv, wasTopLevel);
            }
            catch
            {
                // pop the environment so the caller doesn't have to
                ctx.PopEnvironment();
                ctx.AtTopLevel = wasTopLevel;
                throw;
            }
        }

        public void ValidateResult(Context ctx, ZilObject result)
        {
            ctx.MaybeCheckDecl(result, valueDecl, "return value of {0}", name);
        }

        public ZilList ToZilList()
        {
            Contract.Ensures(Contract.Result<ZilList>() != null);

            return new ZilList(this.AsZilListBody());
        }

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
                    arg = new ZilForm(new ZilObject[] { quoteAtom, arg });
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

        public ZilAtom ActivationAtom
        {
            get { return activationAtom; }
        }
    }
}