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

namespace Zilf.Interpreter
{
    class ArgSpec : IEnumerable<ArgItem>
    {
        // name of the function to which this spec belongs
        private readonly ZilAtom name;
        // reference to the "QUOTE" atom used for any quoted args
        private readonly ZilAtom quoteAtom;

        // "BIND"
        private readonly ZilAtom environmentAtom;

        // regular args, "OPT", and "AUX"
        private readonly ZilAtom[] argAtoms;
        private readonly ZilObject[] argDecls;
        private readonly bool[] argQuoted;
        private readonly ZilObject[] argDefaults;
        private readonly int optArgsStart, auxArgsStart;

        // "ARGS" or "TUPLE"
        private readonly ZilAtom varargsAtom;
        private readonly ZilObject varargsDecl;
        private readonly bool varargsQuoted;

        // "NAME"/"ACT"
        private readonly ZilAtom activationAtom;

        // "VALUE"
        private readonly ZilObject valueDecl;

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

            List<ZilAtom> argAtoms = new List<ZilAtom>();
            List<ZilObject> argDecls = new List<ZilObject>();
            List<bool> argQuoted = new List<bool>();
            List<ZilObject> argDefaults = new List<ZilObject>();

            const int OO_None = 0;
            const int OO_Varargs = 1;
            const int OO_Activation = 2;
            const int OO_Value = 3;
            const int OO_Environment = 4;

            int cur = 0;
            int oneOffMode = OO_None;

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
                                throw new InterpreterError("multiple \"OPT\" clauses");
                            if (auxArgsStart != -1)
                                throw new InterpreterError("\"OPT\" after \"AUX\"");
                            optArgsStart = cur;
                            continue;
                        case "AUX":
                        case "EXTRA":
                            if (auxArgsStart != -1)
                                throw new InterpreterError("multiple \"AUX\" clauses");
                            auxArgsStart = cur;
                            continue;
                        case "ARGS":
                        case "TUPLE":
                            if (varargsAtom != null)
                                throw new InterpreterError("multiple \"ARGS\" or \"TUPLE\" clauses");
                            oneOffMode = OO_Varargs;
                            varargsQuoted = (sep == "ARGS");
                            continue;
                        case "NAME":
                        case "ACT":
                            if (activationAtom != null)
                                throw new InterpreterError("multiple \"NAME\" clauses or activation atoms");
                            oneOffMode = OO_Activation;
                            continue;
                        case "BIND":
                            if (environmentAtom != null)
                                throw new InterpreterError("multiple \"BIND\" clauses");
                            oneOffMode = OO_Environment;
                            continue;
                        case "VALUE":
                            if (valueDecl != null)
                                throw new InterpreterError("multiple \"VALUE\" clauses");
                            oneOffMode = OO_Value;
                            continue;
                        default:
                            throw new InterpreterError("unexpected clause in arg spec: " + arg.ToString());
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
                                throw new InterpreterError("\"ARGS\" or \"TUPLE\" must be followed by an atom");
                            }
                        }

                        oneOffMode = OO_None;
                        continue;

                    case OO_Activation:
                        this.activationAtom = arg as ZilAtom;
                        if (this.activationAtom == null)
                            throw new InterpreterError("\"NAME\" or \"ACT\" must be followed by an atom");

                        oneOffMode = OO_None;
                        continue;

                    case OO_Environment:
                        environmentAtom = arg as ZilAtom;
                        if (environmentAtom == null)
                            throw new InterpreterError("\"BIND\" must be followed by an atom");

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
                    ZilList al = (ZilList)arg;

                    if (al.IsEmpty)
                        throw new InterpreterError("empty list in arg spec");

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
                    ZilForm af = (ZilForm)argName;
                    if (af.First is ZilAtom && ((ZilAtom)af.First).StdAtom == StdAtom.QUOTE &&
                        !af.Rest.IsEmpty)
                    {
                        quoted = true;
                        quoteAtom = (ZilAtom)af.First;
                        argName = af.Rest.First;
                    }
                    else
                        throw new InterpreterError("unexpected FORM in arg spec: " + argName.ToString());
                }

                // it'd better be an atom by now
                if (!(argName is ZilAtom))
                {
                    throw new InterpreterError("expected atom in arg spec but found " + argName.ToString());
                }

                argAtoms.Add((ZilAtom)argName);
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
                        throw new InterpreterError("unrecognized argument name in body DECL: " + atom);
                    }

                    if (prev != null)
                    {
                        throw new InterpreterError("conflicting DECLs for atom: " + atom);
                    }
                }
            }

            this.argAtoms = argAtoms.ToArray();
            this.argDecls = argDecls.ToArray();
            this.argQuoted = argQuoted.ToArray();
            this.argDefaults = argDefaults.ToArray();
        }

        [ContractInvariantMethod]
        private void ObjectInvariant()
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
            ArgSpec other = obj as ArgSpec;
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

        public ZilActivation BeginApply(Context ctx, ZilObject[] args, bool eval)
        {
            Contract.Requires(ctx != null);
            Contract.Requires(args != null);
            Contract.Requires(Contract.ForAll(args, a => a != null));

            if (eval)
            {
                // expand segments
                args = ZilObject.ExpandSegments(ctx, args).ToArray();
            }

            if (args.Length < optArgsStart || (args.Length > auxArgsStart && varargsAtom == null))
                throw new InterpreterError(
                    name == null ? "user-defined function" : name.ToString(),
                    optArgsStart, auxArgsStart);

            var outerEnv = ctx.LocalEnvironment;
            var innerEnv = ctx.PushEnvironment();

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

            return activation;
        }

        public void ValidateResult(Context ctx, ZilObject result)
        {
            ctx.MaybeCheckDecl(result, valueDecl, "return value of {0}", name);
        }

        public void EndApply(Context ctx)
        {
            Contract.Requires(ctx != null);

            ctx.PopEnvironment();
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