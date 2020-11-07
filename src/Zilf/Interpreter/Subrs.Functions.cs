/* Copyright 2010-2018 Jesse McGrew
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
        [FSubr]
        [MdlZilRedirect(typeof(Subrs), nameof(ROUTINE))]
        public static ZilObject DEFINE(Context ctx, ZilAtom name,
            [Optional] ZilAtom? activationAtom, ZilList argList,
            [Optional] ZilDecl? decl, [Required] ZilObject[] body) =>
            PerformDefine(ctx, name, activationAtom, argList, decl, body, "DEFINE");

        [FSubr]
        public static ZilObject DEFINE20(Context ctx, ZilAtom name,
            [Optional] ZilAtom? activationAtom, ZilList argList,
            [Optional] ZilDecl? decl, [Required] ZilObject[] body)
        {
            return PerformDefine(ctx, name, activationAtom, argList, decl, body, "DEFINE20");
        }

        static ZilObject PerformDefine(Context ctx, ZilAtom name,
            ZilAtom? activationAtom,
            ZilList argList, ZilDecl? decl, ZilObject[] body, string subrName)
        {
            if (!ctx.AllowRedefine && ctx.GetGlobalVal(name) != null)
                throw new InterpreterError(InterpreterMessages._0_Already_Defined_1,
                    subrName,
                    name.ToStringContext(ctx, false));

            var func = new ZilFunction(
                subrName,
                name,
                activationAtom,
                argList,
                decl,
                body);
            ctx.SetGlobalVal(name, func);
            return name;
        }

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
                body);
            var macro = new ZilEvalMacro(func) { SourceLine = ctx.TopFrame.SourceLine };
            ctx.SetGlobalVal(name, macro);
            return name;
        }

        [FSubr]
        public static ZilObject QUOTE(Context ctx, ZilObject value)
        {
            return value;
        }

        [Subr]
        public static ZilResult EVAL(Context ctx, ZilObject value, LocalEnvironment env)
        {
            return value.Eval(ctx, env);
        }

        [Subr("EVAL-IN-SEGMENT")]
        public static ZilResult EVAL_IN_SEGMENT(Context ctx, [ParamDesc("dummy1")] ZilObject _1,
             ZilObject value, [ParamDesc("dummy2")] ZilObject? _2 = null)
        {
            return value.Eval(ctx);
        }

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

        [Subr]
        public static ZilResult APPLY(Context ctx, IApplicable ap, ZilObject[] args)
        {
            return ap.ApplyNoEval(ctx, args);
        }
    }
}
