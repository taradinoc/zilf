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

using System.Runtime.InteropServices;
using Zilf.Interpreter.Values;
using Zilf.Language;
using Zilf.Diagnostics;

namespace Zilf.Interpreter
{
    static partial class Subrs
    {
        /// <summary>
        /// Defines a new function and sets it as the global value of an atom. (Or, in MDL-ZIL? mode, an alias for ROUTINE.)
        /// </summary>
        /// <param name="ctx"></param>
        /// <param name="name">The name of the new function.</param>
        /// <param name="activationAtom">An optional atom which will be assigned an activation when the function is called.</param>
        /// <param name="argList">A list of arguments for the function.</param>
        /// <param name="decl">An optional DECL specifying the types of the function's arguments and return value.</param>
        /// <param name="body">A sequence of expressions forming the function body.</param>
        /// <returns>The name of the new function.</returns>
        /// <exception cref="InterpreterError">A global named <paramref name="name"/> is already defined.</exception>
        [FSubr]
        [MdlZilRedirect(typeof(Subrs), nameof(ROUTINE))]
        public static ZilObject DEFINE(Context ctx, ZilAtom name,
            [Optional] ZilAtom? activationAtom, ZilList argList,
            [Optional] ZilDecl? decl, [Required] ZilObject[] body) =>
            PerformDefine(ctx, name, activationAtom, argList, decl, body, "DEFINE");

        /// <summary>
        /// Defines a new function and sets it as the global value of an atom. (Equivalent to DEFINE.)
        /// </summary>
        /// <param name="ctx"></param>
        /// <param name="name">The name of the new function.</param>
        /// <param name="activationAtom">An optional atom which will be assigned an activation when the function is called.</param>
        /// <param name="argList">A list of arguments for the function.</param>
        /// <param name="decl">An optional DECL specifying the types of the function's arguments and return value.</param>
        /// <param name="body">A sequence of expressions forming the function body.</param>
        /// <returns>The name of the new function.</returns>
        /// <exception cref="InterpreterError">A global named <paramref name="name"/> is already defined.</exception>
        [FSubr]
        public static ZilObject DEFINE20(Context ctx, ZilAtom name,
            [Optional] ZilAtom? activationAtom, ZilList argList,
            [Optional] ZilDecl? decl, [Required] ZilObject[] body)
        {
            return PerformDefine(ctx, name, activationAtom, argList, decl, body, "DEFINE20");
        }

        static ZilAtom PerformDefine(Context ctx, ZilAtom name,
            ZilAtom? activationAtom,
            ZilList argList, ZilDecl? decl, ZilObject[] body, string subrName)
        {
            ZilObject? prev;

            if (!ctx.AllowRedefine && (prev = ctx.GetGlobalVal(name)) != null)
            {
                var exn = new InterpreterError(InterpreterMessages._0_Already_Defined_1,
                    subrName,
                    name.ToStringContext(ctx, false));

                if (prev.SourceLine != null)
                {
                    exn = exn.Combine(new InterpreterError(
                        prev.SourceLine,
                        InterpreterMessages.Previous_Definition_Was_Here));
                }

                throw exn;
            }

            var func = new ZilFunction(
                subrName,
                name,
                activationAtom,
                argList,
                decl,
                body)
            {
                SourceLine = ctx.TopFrame.SourceLine
            };
            ctx.SetGlobalVal(name, func);
            return name;
        }

        /// <summary>
        /// Defines a new macro and sets it as the global value of an atom.
        /// </summary>
        /// <param name="ctx"></param>
        /// <param name="name">The name of the new macro.</param>
        /// <param name="activationAtom">An optional atom which will be assigned an activation when the macro is called.</param>
        /// <param name="argList">A list of arguments for the macro.</param>
        /// <param name="decl">An optional DECL specifying the types of the macro's arguments and return value.</param>
        /// <param name="body">A sequence of expressions forming the macro body.</param>
        /// <returns>The name of the new macro.</returns>
        /// <exception cref="InterpreterError">A global named <paramref name="name"/> is already defined.</exception>
        [FSubr]
        public static ZilObject DEFMAC(Context ctx, ZilAtom name,
             [Optional] ZilAtom? activationAtom, ZilList argList,
             [Optional] ZilDecl? decl, [Required] ZilObject[] body)
        {
            if (!ctx.AllowRedefine && ctx.GetGlobalVal(name) != null)
                throw new InterpreterError(InterpreterMessages._0_Already_Defined_1, "DEFMAC", name.ToStringContext(ctx, false));

            var func = new ZilFunction(
                "DEFMAC",
                name,
                activationAtom,
                argList,
                decl,
                body)
            {
                SourceLine = ctx.TopFrame.SourceLine
            };
            var macro = new ZilEvalMacro(func) { SourceLine = ctx.TopFrame.SourceLine };
            ctx.SetGlobalVal(name, macro);
            return name;
        }

        /// <summary>
        /// Returns an expression without evaluating it.
        /// </summary>
        /// <param name="ctx"></param>
        /// <param name="value">The value to return.</param>
        /// <returns>The value.</returns>
        [FSubr]
        public static ZilObject QUOTE(Context ctx, ZilObject value)
        {
            return value;
        }

        /// <summary>
        /// Evaluates an expression in a given environment.
        /// </summary>
        /// <param name="ctx"></param>
        /// <param name="value">The expression to evaluate.</param>
        /// <param name="env"></param>
        /// <returns>The result of evaluating the expression.</returns>
        [Subr]
        public static ZilResult EVAL(Context ctx, ZilObject value, LocalEnvironment env)
        {
            return value.Eval(ctx, env);
        }

        /// <summary>
        /// Evaluates an expression in the current environment.
        /// </summary>
        /// <param name="ctx"></param>
        /// <param name="_1">Ignored.</param>
        /// <param name="value">The expression to evaluate.</param>
        /// <param name="_2">Ignored.</param>
        /// <returns>The result of evaluating the expression.</returns>
        [Subr("EVAL-IN-SEGMENT")]
        public static ZilResult EVAL_IN_SEGMENT(Context ctx, [ParamDesc("dummy1")] ZilObject _1,
             ZilObject value, [ParamDesc("dummy2")] ZilObject? _2 = null)
        {
            return value.Eval(ctx);
        }

        /// <summary>
        /// Expands a macro invocation, or evaluates an expression in the current environment.
        /// </summary>
        /// <param name="ctx"></param>
        /// <param name="value">The expression to expand or evaluate.</param>
        /// <returns>The result of expanding or evaluating the expression.</returns>
        [Subr]
        public static ZilResult EXPAND(Context ctx, ZilObject value)
        {
            var result = value.Expand(ctx);
            if (result.ShouldPass())
                return result;

            if ((ZilObject)result == value)
                result = value.Eval(ctx);

            return result;
        }

        /// <summary>
        /// Applies a function or other applicable value to a set of arguments.
        /// </summary>
        /// <param name="ctx"></param>
        /// <param name="ap">The applicable value.</param>
        /// <param name="args">The arguments to apply the value to.</param>
        /// <returns>The result of the function application.</returns>
        [Subr]
        public static ZilResult APPLY(Context ctx, IApplicable ap, ZilObject[] args)
        {
            return ap.ApplyNoEval(ctx, args);
        }
    }
}
