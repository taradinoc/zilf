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

using System.Diagnostics;
using System.Linq;
using System.Runtime.InteropServices;
using Zilf.Interpreter.Values;
using Zilf.Language;
using Zilf.Diagnostics;

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

                public ZilObject? Decl
                {
                    get
                    {
                        if (Content is AtomParams.AdeclOrAtom aoa)
                            return aoa.Decl;

                        return ((BindingWithInitializer)Content).Name.Decl;
                    }
                }

                public ZilObject? Initializer
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

        /// <summary>
        /// Evaluates a sequence of expressions in a new activation context, with optional bindings.
        /// </summary>
        /// <param name="ctx"></param>
        /// <param name="activationAtom">An optional atom to bind the new activation to.</param>
        /// <param name="bindings">A list containing a sequence of bindings, each of which may have an optional initializer.</param>
        /// <param name="bodyDecl">An optional DECL describing the types of the bindings.</param>
        /// <param name="body">A sequence of expressions to evaluate within the new activation context.</param>
        /// <returns>The results of evaluating the last expression in the body.</returns>
        [FSubr]
        public static ZilResult PROG(Context ctx,
            [Optional] ZilAtom? activationAtom,
            BindingParams.BindingList bindings,
            [Optional] ZilDecl? bodyDecl,
            [Required] ZilObject[] body)
        {
            return PerformProg(ctx, activationAtom, bindings, bodyDecl, body, "PROG", false, true);
        }

        /// <summary>
        /// Repeatedly evaluates a sequence of expressions in a new activation context, with optional bindings, until explicitly exited.
        /// </summary>
        /// <param name="ctx"></param>
        /// <param name="activationAtom">An optional atom to bind the new activation to.</param>
        /// <param name="bindings">A list containing a sequence of bindings, each of which may have an optional initializer.</param>
        /// <param name="bodyDecl">An optional DECL describing the types of the bindings.</param>
        /// <param name="body">A sequence of expressions to evaluate within the new activation context.</param>
        /// <returns>The results of evaluating the last expression in the body.</returns>
        [FSubr]
        public static ZilResult REPEAT(Context ctx,
            [Optional] ZilAtom? activationAtom,
            BindingParams.BindingList bindings,
            [Optional] ZilDecl? bodyDecl,
            [Required] ZilObject[] body)
        {
            return PerformProg(ctx, activationAtom, bindings, bodyDecl, body, "REPEAT", true, true);
        }

        /// <summary>
        /// Evaluates a sequence of expressions, with optional bindings.
        /// </summary>
        /// <param name="ctx"></param>
        /// <param name="activationAtom">An optional atom to bind the new activation to. If omitted, no activation context is created.</param>
        /// <param name="bindings">A list containing a sequence of bindings, each of which may have an optional initializer.</param>
        /// <param name="bodyDecl">An optional DECL describing the types of the bindings.</param>
        /// <param name="body">A sequence of expressions to evaluate within the new activation context.</param>
        /// <returns>The results of evaluating the last expression in the body.</returns>
        [FSubr]
        public static ZilResult BIND(Context ctx,
            [Optional] ZilAtom? activationAtom,
            BindingParams.BindingList bindings,
            [Optional] ZilDecl? bodyDecl,
            [Required] ZilObject[] body)
        {
            return PerformProg(ctx, activationAtom, bindings, bodyDecl, body, "BIND", false, false);
        }

        static ZilResult PerformProg(Context ctx, ZilAtom? activationAtom,
            BindingParams.BindingList bindings, ZilDecl? bodyDecl, ZilObject[] body,
             string name, bool repeat, bool catchy)
        {
            using var activation = new ZilActivation(ctx.GetStdAtom(StdAtom.PROG));
            using var innerEnv = ctx.PushEnvironment();

            if (activationAtom != null)
            {
                innerEnv.Rebind(activationAtom, activation);
            }

            var bodyAtomDecls = bodyDecl?.GetAtomDeclPairs().ToLookup(p => p.Key, p => p.Value);

            foreach (var b in bindings.Bindings)
            {
                var atom = b.Atom;
                var initializer = b.Initializer;

                ZilObject? value;

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
                if (firstBodyDecl != null && (previousDecl != null || bodyAtomDecls![atom].Skip(1).Any()))
                    throw new InterpreterError(InterpreterMessages._0_Conflicting_DECLs_For_Atom_1, name, atom);

                var decl = previousDecl ?? firstBodyDecl;

                if (value != null)
                {
                    Debug.Assert(initializer != null);
                    ctx.MaybeCheckDecl(initializer, value, decl, "LVAL of {0}", atom);
                }

                innerEnv.Rebind(atom, value, decl);
            }

            if (catchy)
                innerEnv.Rebind(ctx.EnclosingProgActivationAtom, activation);

            // evaluate body
            ZilResult result = default;
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

            return result;
        }

        /// <summary>
        /// Causes a value to be returned from the specified activation context (PROG, REPEAT, or function application).
        /// </summary>
        /// <param name="ctx"></param>
        /// <param name="value">The value to return.</param>
        /// <param name="activation">The activation context from which to return. If omitted, defaults to the current enclosing PROG or REPEAT activation or function application.</param>
        /// <returns>Does not return normally.</returns>
        /// <exception cref="InterpreterError">No enclosing PROG/REPEAT.</exception>
        [Subr]
        public static ZilResult RETURN(Context ctx, ZilObject? value = null, ZilActivation? activation = null)
        {
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

        /// <summary>
        /// Causes the current PROG or REPEAT activation to restart from the beginning.
        /// </summary>
        /// <param name="ctx"></param>
        /// <param name="activation">The activation context to restart. If omitted, defaults to the current enclosing PROG or REPEAT activation or function application.</param>
        /// <returns>Does not return normally.</returns>
        /// <exception cref="InterpreterError">No enclosing PROG/REPEAT.</exception>
        [Subr]
        public static ZilResult AGAIN(Context ctx, ZilActivation? activation = null)
        {
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
