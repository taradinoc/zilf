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
using System.Runtime.InteropServices;
using Zilf.Interpreter.Values;
using Zilf.Language;
using Zilf.Diagnostics;
using JetBrains.Annotations;

namespace Zilf.Interpreter
{
    static partial class Subrs
    {
        [NotNull]
        [FSubr]
        [MdlZilRedirect(typeof(Subrs), nameof(ROUTINE))]
        public static ZilObject DEFINE([NotNull] Context ctx, [NotNull] ZilAtom name,
            [CanBeNull] [Optional] ZilAtom activationAtom, [ItemNotNull] [NotNull] ZilList argList,
            [CanBeNull] [Optional] ZilDecl decl, [ItemNotNull] [NotNull] [Required] ZilObject[] body)
        {
            Contract.Requires(ctx != null);
            Contract.Requires(name != null);
            Contract.Requires(argList != null);
            Contract.Requires(body != null);
            Contract.Ensures(Contract.Result<ZilObject>() != null);
            SubrContracts(ctx);

            return PerformDefine(ctx, name, activationAtom, argList, decl, body, "DEFINE");
        }

        [NotNull]
        [FSubr]
        public static ZilObject DEFINE20([NotNull] Context ctx, [NotNull] ZilAtom name,
            [CanBeNull] [Optional] ZilAtom activationAtom, [NotNull] [ItemNotNull] ZilList argList,
            [CanBeNull] [Optional] ZilDecl decl, [ItemCanBeNull] [NotNull] [Required] ZilObject[] body)
        {
            Contract.Requires(ctx != null);
            Contract.Requires(name != null);
            Contract.Requires(argList != null);
            Contract.Requires(body != null);
            Contract.Ensures(Contract.Result<ZilObject>() != null);
            SubrContracts(ctx);

            return PerformDefine(ctx, name, activationAtom, argList, decl, body, "DEFINE20");
        }

        [NotNull]
        static ZilObject PerformDefine([NotNull] [ProvidesContext] Context ctx, [NotNull] ZilAtom name,
            [CanBeNull] ZilAtom activationAtom,
            ZilList argList, ZilDecl decl, [NotNull] ZilObject[] body, [NotNull] string subrName)
        {
            Contract.Requires(ctx != null);
            Contract.Requires(name != null);
            Contract.Requires(subrName != null);
            Contract.Ensures(Contract.Result<ZilObject>() != null);

            if (!ctx.AllowRedefine && ctx.GetGlobalVal(name) != null)
                throw new InterpreterError(InterpreterMessages._0_Already_Defined_1, subrName, name.ToStringContext(ctx, false));

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
        [NotNull]
        [FSubr]
        public static ZilObject DEFMAC([NotNull] Context ctx, [NotNull] ZilAtom name,
            [CanBeNull] [Optional] ZilAtom activationAtom, [ItemNotNull] [NotNull] ZilList argList,
            [CanBeNull] [Optional] ZilDecl decl, [NotNull] [Required] ZilObject[] body)
        {
            Contract.Requires(ctx != null);
            Contract.Requires(name != null);
            Contract.Requires(argList != null);
            Contract.Ensures(Contract.Result<ZilObject>() != null);
            SubrContracts(ctx);

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
            SubrContracts(ctx);

            return value;
        }

        [Subr]
        public static ZilResult EVAL([NotNull] Context ctx, [NotNull] ZilObject value, [NotNull] LocalEnvironment env)
        {
            Contract.Requires(ctx != null);
            Contract.Requires(value != null);
            Contract.Requires(env != null);
            SubrContracts(ctx);

            return value.Eval(ctx, env);
        }

#pragma warning disable RECS0154 // Parameter is never used
        [Subr("EVAL-IN-SEGMENT")]
        public static ZilResult EVAL_IN_SEGMENT([NotNull] Context ctx, ZilObject dummy1,
            [NotNull] ZilObject value, [CanBeNull] ZilObject dummy2 = null)
#pragma warning restore RECS0154 // Parameter is never used
        {
            Contract.Requires(ctx != null);
            Contract.Requires(value != null);
            SubrContracts(ctx);

            return value.Eval(ctx);
        }

        [Subr]
        public static ZilResult EXPAND([NotNull] Context ctx, [NotNull] ZilObject value)
        {
            Contract.Requires(ctx != null);
            Contract.Requires(value != null);
            SubrContracts(ctx);

            var result = value.Expand(ctx);
            if (result.ShouldPass())
                return result;

            if ((ZilObject)result == value)
                result = value.Eval(ctx);

            return result;
        }

        [Subr]
        public static ZilResult APPLY([NotNull] Context ctx, [NotNull] IApplicable ap, [NotNull] ZilObject[] args)
        {
            Contract.Requires(ctx != null);
            Contract.Requires(ap != null);
            SubrContracts(ctx);

            return ap.ApplyNoEval(ctx, args);
        }
    }
}
