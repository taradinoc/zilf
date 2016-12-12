/* Copyright 2010, 2015 Jesse McGrew
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

        public ArgSpec(ArgSpec prev, IEnumerable<ZilObject> argspec)
            : this(prev.name, null, argspec)
        {
            Contract.Requires(prev != null);
            Contract.Requires(argspec != null && Contract.ForAll(argspec, a => a != null));
        }

        public ArgSpec(ZilAtom name, ZilAtom activationAtom, IEnumerable<ZilObject> argspec, ZilDecl bodyDecl = null)
        {
            Contract.Requires(argspec != null && Contract.ForAll(argspec, a => a != null));

            this.name = name;
            this.activationAtom = activationAtom;

            optArgsStart = -1;
            auxArgsStart = -1;

            var newArgAtoms = new List<ZilAtom>();
            var newArgDecls = new List<ZilObject>();
            var newArgQuoted = new List<bool>();
            var newArgDefaults = new List<ZilObject>();

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
                if (arg is ZilString)
                {
                    string sep = ((ZilString)arg).Text;
                    switch (sep)
                    {
                        case "OPT":
                        case "OPTIONAL":
                            if (optArgsStart != -1)
                                throw new InterpreterError(InterpreterMessages.Multiple_0_Clauses, "\"OPT\"");
                            if (auxArgsStart != -1)
                                throw new InterpreterError(InterpreterMessages.OPT_After_AUX);
                            optArgsStart = cur;
                            continue;
                        case "AUX":
                        case "EXTRA":
                            if (auxArgsStart != -1)
                                throw new InterpreterError(InterpreterMessages.Multiple_0_Clauses, "\"AUX\"");
                            auxArgsStart = cur;
                            continue;
                        case "ARGS":
                        case "TUPLE":
                            if (varargsAtom != null)
                                throw new InterpreterError(InterpreterMessages.Multiple_0_Clauses, "\"ARGS\" or \"TUPLE\"");
                            oneOffMode = OO_Varargs;
                            oneOffTag = arg;
                            varargsQuoted = (sep == "ARGS");
                            continue;
                        case "NAME":
                        case "ACT":
                            if (activationAtom != null)
                                throw new InterpreterError(InterpreterMessages.Multiple_NAME_Clauses_Or_Activation_Atoms);
                            oneOffMode = OO_Activation;
                            oneOffTag = arg;
                            continue;
                        case "BIND":
                            if (environmentAtom != null)
                                throw new InterpreterError(InterpreterMessages.Multiple_0_Clauses, "\"BIND\"");
                            oneOffMode = OO_Environment;
                            oneOffTag = arg;
                            continue;
                        case "VALUE":
                            if (valueDecl != null)
                                throw new InterpreterError(InterpreterMessages.Multiple_0_Clauses, "\"VALUE\"");
                            oneOffMode = OO_Value;
                            oneOffTag = arg;
                            continue;
                        default:
                            throw new InterpreterError(InterpreterMessages.Unexpected_Clause_In_Arg_Spec_0, arg.ToString());
                    }
                }

                // handle one-offs
                switch (oneOffMode)
                {
                    case OO_Varargs:
                        varargsAtom = arg as ZilAtom;
                        if (varargsAtom == null)
                        {
                            var adecl = arg as ZilAdecl;
                            if (adecl != null)
                            {
                                varargsDecl = adecl.Second;
                                varargsAtom = (ZilAtom)adecl.First;
                            }
                            else
                            {
                                throw new InterpreterError(InterpreterMessages._0_Must_Be_Followed_By_An_Atom, oneOffTag);
                            }
                        }

                        oneOffMode = OO_None;
                        continue;

                    case OO_Activation:
                        this.activationAtom = arg as ZilAtom;
                        if (this.activationAtom == null)
                            throw new InterpreterError(InterpreterMessages._0_Must_Be_Followed_By_An_Atom, oneOffTag);

                        oneOffMode = OO_None;
                        continue;

                    case OO_Environment:
                        environmentAtom = arg as ZilAtom;
                        if (environmentAtom == null)
                            throw new InterpreterError(InterpreterMessages._0_Must_Be_Followed_By_An_Atom, oneOffTag);

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
                if (arg is ZilList && !(arg is ZilForm))
                {
                    var al = (ZilList)arg;

                    if (al.IsEmpty)
                        throw new InterpreterError(InterpreterMessages.Empty_List_In_Arg_Spec);

                    argName = al.First;
                    argValue = al.Rest.First;
                }
                else
                {
                    argName = arg;
                    argValue = null;
                }

                // could be an ADECL
                if (argName is ZilAdecl)
                {
                    var adecl = (ZilAdecl)argName;
                    argDecl = adecl.Second;
                    argName = adecl.First;
                }
                else
                {
                    argDecl = null;
                }

                // could be quoted
                if (argName is ZilForm)
                {
                    var af = (ZilForm)argName;
                    if (af.First is ZilAtom && ((ZilAtom)af.First).StdAtom == StdAtom.QUOTE &&
                        !af.Rest.IsEmpty)
                    {
                        quoted = true;
                        quoteAtom = (ZilAtom)af.First;
                        argName = af.Rest.First;
                    }
                    else
                        throw new InterpreterError(InterpreterMessages.Unexpected_FORM_In_Arg_Spec_0, argName.ToString());
                }

                // it'd better be an atom by now
                if (!(argName is ZilAtom))
                {
                    throw new InterpreterError(InterpreterMessages.Expected_Atom_In_Arg_Spec_But_Found_0, argName.ToString());
                }

                newArgAtoms.Add((ZilAtom)argName);
                newArgDecls.Add(argDecl);
                newArgDefaults.Add(argValue);
                newArgQuoted.Add(quoted);
            }

            if (auxArgsStart == -1)
                auxArgsStart = cur;
            if (optArgsStart == -1)
                optArgsStart = auxArgsStart;

            // process #DECL in body
            if (bodyDecl != null)
            {
                var argIndex = newArgAtoms.Select((atom, i) => new { atom, i }).ToLookup(p => p.atom, p => p.i);

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
                            prev = prev ?? newArgDecls[i];
                            newArgDecls[i] = decl;
                        }
                    }
                    else
                    {
                        throw new InterpreterError(InterpreterMessages.Unrecognized_Argument_Name_In_Body_DECL_0, atom);
                    }

                    if (prev != null)
                    {
                        throw new InterpreterError(InterpreterMessages.Conflicting_DECLs_For_Atom_0, atom);
                    }
                }
            }

            this.argAtoms = newArgAtoms.ToArray();
            this.argDecls = newArgDecls.ToArray();
            this.argQuoted = newArgQuoted.ToArray();
            this.argDefaults = newArgDefaults.ToArray();
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
            ArgItem.ArgType type = ArgItem.ArgType.Required;

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
            var other = obj as ArgSpec;
            if (other == null)
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

        /// <summary>
        /// Pushes a new local environment and binds argument values in preparation for a functino call.
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

            try
            {
                if (eval)
                {
                    // expand segments
                    args = ZilObject.ExpandSegments(ctx, args).ToArray();
                }

                if (args.Length < optArgsStart || (args.Length > auxArgsStart && varargsAtom == null))
                    throw ArgumentCountError.WrongCount(
                        new FunctionCallSite(name?.ToString() ?? "user-defined function"),
                        optArgsStart,
                        auxArgsStart);

                if (environmentAtom != null)
                {
                    innerEnv.Rebind(environmentAtom,
                        new ZilEnvironment(outerEnv, environmentAtom),
                        ctx.GetStdAtom(StdAtom.ENVIRONMENT));
                }

                for (int i = 0; i < optArgsStart; i++)
                {
                    var value = (!eval || argQuoted[i]) ? args[i] : args[i].Eval(ctx, outerEnv);
                    ctx.MaybeCheckDecl(args[i], value, argDecls[i], "argument {0}", argAtoms[i]);
                    innerEnv.Rebind(argAtoms[i], value, argDecls[i]);
                }

                for (int i = optArgsStart; i < auxArgsStart; i++)
                {
                    if (i < args.Length)
                    {
                        var value = (!eval || argQuoted[i]) ? args[i] : args[i].Eval(ctx, outerEnv);
                        ctx.MaybeCheckDecl(args[i], value, argDecls[i], "argument {0}", argAtoms[i]);
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
                    var extras = args.Skip(auxArgsStart);
                    if (eval && !varargsQuoted)
                        extras = ZilObject.EvalSequenceLeavingSegments(ctx, extras);
                    var value = new ZilList(extras);
                    ctx.MaybeCheckDecl(value, varargsDecl, "argument {0}", varargsAtom);
                    innerEnv.Rebind(varargsAtom, new ZilList(extras), varargsDecl);
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