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
        private readonly ZilAtom name;
        private readonly ZilAtom[] argAtoms;
        private readonly ZilObject[] argDecls;
        private readonly bool[] argQuoted;
        private readonly ZilObject[] argDefaults;
        private readonly int optArgsStart, auxArgsStart;
        private readonly ZilAtom varargsAtom;
        private readonly bool varargsQuoted;
        private readonly ZilAtom quoteAtom;

        public ArgSpec(ArgSpec prev, IEnumerable<ZilObject> argspec)
            : this(prev.name, argspec)
        {
            Contract.Requires(prev != null);
            Contract.Requires(argspec != null && Contract.ForAll(argspec, a => a != null));
        }

        public ArgSpec(ZilAtom name, IEnumerable<ZilObject> argspec)
        {
            Contract.Requires(argspec != null && Contract.ForAll(argspec, a => a != null));

            this.name = name;

            optArgsStart = -1;
            auxArgsStart = -1;

            List<ZilAtom> argAtoms = new List<ZilAtom>();
            List<ZilObject> argDecls = new List<ZilObject>();
            List<bool> argQuoted = new List<bool>();
            List<ZilObject> argDefaults = new List<ZilObject>();

            int cur = 0;
            bool gotVarargs = false;
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
                            gotVarargs = true;
                            varargsQuoted = (sep == "ARGS");
                            continue;
                        default:
                            throw new InterpreterError("unexpected clause in arg spec: " + arg.ToString());
                    }
                }

                if (gotVarargs)
                {
                    varargsAtom = arg as ZilAtom;
                    if (varargsAtom == null)
                        throw new InterpreterError("\"ARGS\" or \"TUPLE\" must be followed by an atom");

                    gotVarargs = false;
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
            Contract.Invariant(MaxArgCount == 0 || MaxArgCount >= MinArgCount);
        }

        public int MinArgCount
        {
            get { return optArgsStart; }
        }

        public int MaxArgCount
        {
            get { return (varargsAtom != null) ? 0 : auxArgsStart; }
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

        public string ToStringContext(Context ctx, bool friendly)
        {
            return this.ToString(zo => zo.ToStringContext(ctx, friendly));
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

        public void BeginApply(Context ctx, ZilObject[] args, bool eval)
        {
            Contract.Requires(ctx != null);
            Contract.Requires(args != null && Contract.ForAll(args, a => a != null));

            if (args.Length < optArgsStart || (args.Length > auxArgsStart && varargsAtom == null))
                throw new InterpreterError(
                    name == null ? "user-defined function" : name.ToString(),
                    optArgsStart, auxArgsStart);

            for (int i = 0; i < optArgsStart; i++)
            {
                ZilObject value = (!eval || argQuoted[i]) ? args[i] : args[i].Eval(ctx);
                ctx.PushLocalVal(argAtoms[i], value);
            }

            for (int i = optArgsStart; i < auxArgsStart; i++)
            {
                if (i < args.Length)
                {
                    ZilObject value = (!eval || argQuoted[i]) ? args[i] : args[i].Eval(ctx);
                    ctx.PushLocalVal(argAtoms[i], value);
                }
                else
                {
                    ctx.PushLocalVal(argAtoms[i], argDefaults[i] == null ? null : argDefaults[i].Eval(ctx));
                }
            }

            for (int i = auxArgsStart; i < argAtoms.Length; i++)
            {
                ctx.PushLocalVal(argAtoms[i], argDefaults[i] == null ? null : argDefaults[i].Eval(ctx));
            }

            if (varargsAtom != null)
            {
                var extras = args.Skip(auxArgsStart);
                if (eval && !varargsQuoted)
                    extras = ZilObject.EvalSequence(ctx, extras);
                ctx.PushLocalVal(varargsAtom, new ZilList(extras));
            }
        }

        public void EndApply(Context ctx)
        {
            Contract.Requires(ctx != null);

            foreach (ZilAtom atom in argAtoms)
                ctx.PopLocalVal(atom);
        }

        public ZilList ToZilList()
        {
            Contract.Ensures(Contract.Result<ZilList>() != null);

            return new ZilList(this.AsZilListBody());
        }

        public IEnumerable<ZilObject> AsZilListBody()
        {
            for (int i = 0; i < argAtoms.Length; i++)
            {
                // TODO: include "ARGS" or "TUPLE"
                // TODO: return ADECLs for args with decls
                if (i == auxArgsStart)
                    yield return new ZilString("AUX");
                else if (i == optArgsStart)
                    yield return new ZilString("OPT");

                ZilObject arg = argAtoms[i];

                if (argQuoted[i])
                {
                    arg = new ZilForm(new ZilObject[] { quoteAtom, arg });
                }

                if (argDefaults[i] != null)
                {
                    arg = new ZilList(arg,
                        new ZilList(argDefaults[i],
                            new ZilList(null, null)));
                }

                yield return arg;
            }
        }
    }
}