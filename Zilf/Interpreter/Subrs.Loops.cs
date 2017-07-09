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
using System.Collections.Generic;
using System.Diagnostics.Contracts;
using System.Linq;
using System.Runtime.InteropServices;
using Zilf.Interpreter.Values;
using Zilf.Language;
using Zilf.Diagnostics;

namespace Zilf.Interpreter
{
    static partial class Subrs
    {
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

        [FSubr]
        public static ZilResult PROG(Context ctx,
            [Optional] ZilAtom activationAtom,
            BindingParams.BindingList bindings,
            [Optional] ZilDecl bodyDecl,
            [Required] ZilObject[] body)
        {
            SubrContracts(ctx);

            return PerformProg(ctx, activationAtom, bindings, bodyDecl, body, "PROG", false, true);
        }

        [FSubr]
        public static ZilResult REPEAT(Context ctx,
            [Optional] ZilAtom activationAtom,
            BindingParams.BindingList bindings,
            [Optional] ZilDecl bodyDecl,
            [Required] ZilObject[] body)
        {
            SubrContracts(ctx);

            return PerformProg(ctx, activationAtom, bindings, bodyDecl, body, "REPEAT", true, true);
        }

        [FSubr]
        public static ZilResult BIND(Context ctx,
            [Optional] ZilAtom activationAtom,
            BindingParams.BindingList bindings,
            [Optional] ZilDecl bodyDecl,
            [Required] ZilObject[] body)
        {
            SubrContracts(ctx);

            return PerformProg(ctx, activationAtom, bindings, bodyDecl, body, "BIND", false, false);
        }

        static ZilResult PerformProg(Context ctx, ZilAtom activationAtom,
            BindingParams.BindingList bindings, ZilDecl bodyDecl, ZilObject[] body,
            string name, bool repeat, bool catchy)
        {
            SubrContracts(ctx);
            Contract.Requires(name != null);
            Contract.Requires(body != null && body.Length > 0);

            using (var activation = new ZilActivation(ctx.GetStdAtom(StdAtom.PROG)))
            {
                // bind atoms
                var boundAtoms = new Queue<ZilAtom>();

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
                        if (firstBodyDecl != null && (previousDecl != null || bodyAtomDecls?[atom].Skip(1).Any() == true))
                            throw new InterpreterError(InterpreterMessages._0_Conflicting_DECLs_For_Atom_1, name, atom);

                        var decl = previousDecl ?? firstBodyDecl;

                        if (value != null)
                            ctx.MaybeCheckDecl(initializer, (ZilObject)value, decl, "LVAL of {0}", atom);

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
