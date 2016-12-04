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
                [Either(typeof(BindingName), typeof(BindingWithInitializer))]
                public object Content;

                public ZilAtom Atom
                {
                    get
                    {
                        if (Content is BindingName)
                            return ((BindingName)Content).Atom;

                        return ((BindingWithInitializer)Content).Name.Atom;
                    }
                }

                public ZilObject Decl
                {
                    get
                    {
                        if (Content is BindingName)
                            return ((BindingName)Content).Decl;

                        return ((BindingWithInitializer)Content).Name.Decl;
                    }
                }

                public ZilObject Initializer
                {
                    get
                    {
                        if (Content is BindingWithInitializer)
                            return ((BindingWithInitializer)Content).Initializer;

                        return null;
                    }
                }
            }

            [ZilSequenceParam]
            public struct BindingName
            {
                [Either(typeof(ZilAtom), typeof(BindingAdecl))]
                public object Content;

                public ZilAtom Atom
                {
                    get
                    {
                        var atom = Content as ZilAtom;
                        if (atom != null)
                            return atom;

                        return ((BindingAdecl)Content).Atom;
                    }
                }

                public ZilObject Decl
                {
                    get
                    {
                        if (Content is BindingAdecl)
                            return ((BindingAdecl)Content).Decl;

                        return null;
                    }
                }
            }

            [ZilStructuredParam(StdAtom.ADECL)]
            public struct BindingAdecl
            {
                public ZilAtom Atom;
                public ZilObject Decl;
            }

            [ZilStructuredParam(StdAtom.LIST)]
            public struct BindingWithInitializer
            {
                public BindingName Name;
                public ZilObject Initializer;
            }
        }

        [FSubr]
        public static ZilObject PROG(Context ctx,
            [Optional] ZilAtom activationAtom,
            BindingParams.BindingList bindings,
            [Optional] ZilDecl bodyDecl,
            [Required] ZilObject[] body)
        {
            SubrContracts(ctx);

            return PerformProg(ctx, activationAtom, bindings, bodyDecl, body, "PROG", false, true);
        }

        [FSubr]
        public static ZilObject REPEAT(Context ctx,
            [Optional] ZilAtom activationAtom,
            BindingParams.BindingList bindings,
            [Optional] ZilDecl bodyDecl,
            [Required] ZilObject[] body)
        {
            SubrContracts(ctx);

            return PerformProg(ctx, activationAtom, bindings, bodyDecl, body, "REPEAT", true, true);
        }

        [FSubr]
        public static ZilObject BIND(Context ctx,
            [Optional] ZilAtom activationAtom,
            BindingParams.BindingList bindings,
            [Optional] ZilDecl bodyDecl,
            [Required] ZilObject[] body)
        {
            SubrContracts(ctx);

            return PerformProg(ctx, activationAtom, bindings, bodyDecl, body, "BIND", false, false);
        }

        private static ZilObject PerformProg(Context ctx, ZilAtom activationAtom,
            BindingParams.BindingList bindings, ZilDecl bodyDecl, ZilObject[] body,
            string name, bool repeat, bool catchy)
        {
            SubrContracts(ctx);
            Contract.Requires(name != null);
            Contract.Requires(body != null && body.Length > 0);

            using (var activation = new ZilActivation(ctx.GetStdAtom(StdAtom.PROG)))
            {
                // bind atoms
                Queue<ZilAtom> boundAtoms = new Queue<ZilAtom>();

                using (var innerEnv = ctx.PushEnvironment())
                {
                    if (activationAtom != null)
                    {
                        innerEnv.Rebind(activationAtom, activation);
                    }

                    var bodyAtomDecls = bodyDecl?.GetAtomDeclPairs().ToDictionary(p => p.Key, p => p.Value);

                    foreach (var b in bindings.Bindings)
                    {
                        var atom = b.Atom;
                        var initializer = b.Initializer;

                        var value = initializer?.Eval(ctx);

                        ZilObject decl1 = b.Decl, decl2 = null;
                        bodyAtomDecls?.TryGetValue(atom, out decl2);
                        if (decl1 != null && decl2 != null)
                            throw new InterpreterError(name + ": conflicting DECLs for atom: " + atom);

                        var decl = decl1 ?? decl2;

                        if (value != null)
                            ctx.MaybeCheckDecl(initializer, value, decl, "LVAL of {0}", atom);

                        innerEnv.Rebind(atom, value, decl);
                    }

                    if (catchy)
                        innerEnv.Rebind(ctx.EnclosingProgActivationAtom, activation);

                    // evaluate body
                    ZilObject result = null;
                    bool again;
                    do
                    {
                        again = false;
                        foreach (var expr in body)
                        {
                            try
                            {
                                result = expr.Eval(ctx);
                            }
                            catch (ReturnException ex) when (ex.Activation == activation)
                            {
                                return ex.Value;
                            }
                            catch (AgainException ex) when (ex.Activation == activation)
                            {
                                again = true;
                            }
                        }
                    } while (repeat || again);

                    Contract.Assert(result != null);

                    return result;
                }
            }
        }

        [Subr]
        public static ZilObject RETURN(Context ctx, ZilObject value = null, ZilActivation activation = null)
        {
            SubrContracts(ctx);

            if (value == null) {
                value = ctx.TRUE;
            }

            if (activation == null)
            {
                activation = ctx.GetEnclosingProgActivation();
                if (activation == null)
                    throw new InterpreterError(InterpreterMessages.FUNCNAME0_No_Enclosing_PROGREPEAT, "RETURN");
            }

            throw new ReturnException(activation, value);
        }

        [Subr]
        public static ZilObject AGAIN(Context ctx, ZilActivation activation = null)
        {
            SubrContracts(ctx);

            if (activation == null)
            {
                activation = ctx.GetEnclosingProgActivation();
                if (activation == null)
                    throw new InterpreterError(InterpreterMessages.FUNCNAME0_No_Enclosing_PROGREPEAT, "AGAIN");
            }

            throw new AgainException(activation);
        }
    }
}
