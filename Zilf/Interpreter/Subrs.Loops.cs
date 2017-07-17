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

using System.Diagnostics.Contracts;
using System.Linq;
using System.Runtime.InteropServices;
using Zilf.Interpreter.Values;
using Zilf.Language;
using Zilf.Diagnostics;
using JetBrains.Annotations;

namespace Zilf.Interpreter
{
    static partial class Subrs
    {
#pragma warning disable CS0649
        public static class BindingParams
        {
            [ZilStructuredParam(StdAtom.LIST)]
            public struct BindingList
            {
                public Binding[] Bindings;
            }

            [ZilSequenceParam]
            public struct Binding
            {
                [Either(typeof(AtomParams.AdeclOrAtom), typeof(BindingWithInitializer))]
                public object Content;

                public ZilAtom Atom
                {
                    get
                    {
                        if (Content is AtomParams.AdeclOrAtom aoa)
                            return aoa.Atom;

                        return ((BindingWithInitializer)Content).Name.Atom;
                    }
                }

                public ZilObject Decl
                {
                    get
                    {
                        if (Content is AtomParams.AdeclOrAtom aoa)
                            return aoa.Decl;

                        return ((BindingWithInitializer)Content).Name.Decl;
                    }
                }

                public ZilObject Initializer
                {
                    get
                    {
                        if (Content is BindingWithInitializer bwi)
                            return bwi.Initializer;

                        return null;
                    }
                }
            }

            [ZilStructuredParam(StdAtom.LIST)]
            public struct BindingWithInitializer
            {
                public AtomParams.AdeclOrAtom Name;
                public ZilObject Initializer;
            }
        }
#pragma warning restore CS0649

        [FSubr]
        public static ZilResult PROG([NotNull] Context ctx,
            [CanBeNull] [Optional] ZilAtom activationAtom,
            BindingParams.BindingList bindings,
            [CanBeNull] [Optional] ZilDecl bodyDecl,
            [NotNull] [Required] ZilObject[] body)
        {
            Contract.Requires(ctx != null);
            Contract.Requires(body != null);
            SubrContracts(ctx);

            return PerformProg(ctx, activationAtom, bindings, bodyDecl, body, "PROG", false, true);
        }

        [FSubr]
        public static ZilResult REPEAT([NotNull] Context ctx,
            [CanBeNull] [Optional] ZilAtom activationAtom,
            BindingParams.BindingList bindings,
            [CanBeNull] [Optional] ZilDecl bodyDecl,
            [NotNull] [Required] ZilObject[] body)
        {
            Contract.Requires(ctx != null);
            Contract.Requires(body != null);
            SubrContracts(ctx);

            return PerformProg(ctx, activationAtom, bindings, bodyDecl, body, "REPEAT", true, true);
        }

        [FSubr]
        public static ZilResult BIND([NotNull] Context ctx,
            [CanBeNull] [Optional] ZilAtom activationAtom,
            BindingParams.BindingList bindings,
            [CanBeNull] [Optional] ZilDecl bodyDecl,
            [ItemNotNull] [NotNull] [Required] ZilObject[] body)
        {
            Contract.Requires(ctx != null);
            Contract.Requires(body != null);
            SubrContracts(ctx);

            return PerformProg(ctx, activationAtom, bindings, bodyDecl, body, "BIND", false, false);
        }

        static ZilResult PerformProg([NotNull] [ProvidesContext] Context ctx, [CanBeNull] ZilAtom activationAtom,
            BindingParams.BindingList bindings, [CanBeNull] ZilDecl bodyDecl, [ItemNotNull] [NotNull] ZilObject[] body,
            [NotNull] string name, bool repeat, bool catchy)
        {
            Contract.Requires(ctx != null);
            Contract.Requires(body != null);
            SubrContracts(ctx);
            Contract.Requires(name != null);
            Contract.Requires(body != null && body.Length > 0);

            using (var activation = new ZilActivation(ctx.GetStdAtom(StdAtom.PROG)))
            {
                using (var innerEnv = ctx.PushEnvironment())
                {
                    if (activationAtom != null)
                    {
                        innerEnv.Rebind(activationAtom, activation);
                    }

                    var bodyAtomDecls = bodyDecl?.GetAtomDeclPairs().ToLookup(p => p.Key, p => p.Value);

                    foreach (var b in bindings.Bindings)
                    {
                        var atom = b.Atom;
                        var initializer = b.Initializer;

                        ZilObject value;

                        if (initializer != null)
                        {
                            var initResult = initializer.Eval(ctx);
                            if (initResult.ShouldPass(activation, ref initResult))
                                return initResult;
                            value = (ZilObject)initResult;
                        }
                        else
                        {
                            value = null;
                        }

                        var previousDecl = b.Decl;
                        var firstBodyDecl = bodyAtomDecls?[atom].FirstOrDefault();
                        if (firstBodyDecl != null && (previousDecl != null || bodyAtomDecls[atom].Skip(1).Any()))
                            throw new InterpreterError(InterpreterMessages._0_Conflicting_DECLs_For_Atom_1, name, atom);

                        var decl = previousDecl ?? firstBodyDecl;

                        if (value != null)
                            ctx.MaybeCheckDecl(initializer, value, decl, "LVAL of {0}", atom);

                        innerEnv.Rebind(atom, value, decl);
                    }

                    if (catchy)
                        innerEnv.Rebind(ctx.EnclosingProgActivationAtom, activation);

                    // evaluate body
                    ZilResult result = null;
                    bool again;
                    do
                    {
                        again = false;
                        foreach (var expr in body)
                        {
                            result = expr.Eval(ctx);

                            if (result.IsAgain(activation))
                            {
                                again = true;
                            }
                            else if (result.ShouldPass(activation, ref result))
                            {
                                return result;
                            }
                        }
                    } while (repeat || again);

                    Contract.Assert((ZilObject)result != null);

                    return result;
                }
            }
        }

        /// <exception cref="InterpreterError">No enclosing PROG/REPEAT.</exception>
        [Subr]
        public static ZilResult RETURN(Context ctx, ZilObject value = null, ZilActivation activation = null)
        {
            SubrContracts(ctx);

            if (value == null)
                value = ctx.TRUE;

            if (activation == null)
            {
                activation = ctx.GetEnclosingProgActivation();
                if (activation == null)
                    throw new InterpreterError(InterpreterMessages._0_No_Enclosing_PROGREPEAT, "RETURN");
            }

            return ZilResult.Return(activation, value);
        }

        /// <exception cref="InterpreterError">No enclosing PROG/REPEAT.</exception>
        [Subr]
        public static ZilResult AGAIN(Context ctx, ZilActivation activation = null)
        {
            SubrContracts(ctx);

            if (activation == null)
            {
                activation = ctx.GetEnclosingProgActivation();
                if (activation == null)
                    throw new InterpreterError(InterpreterMessages._0_No_Enclosing_PROGREPEAT, "AGAIN");
            }

            return ZilResult.Again(activation);
        }
    }
}
