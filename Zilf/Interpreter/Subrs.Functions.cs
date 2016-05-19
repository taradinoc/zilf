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
using Zilf.Interpreter.Values;
using Zilf.Language;

namespace Zilf.Interpreter
{
    static partial class Subrs
    {
        // TODO: clean up arg handling for DEFINE
        [FSubr]
        public static ZilObject DEFINE(Context ctx, ZilObject[] args)
        {
            SubrContracts(ctx, args);

            if ((ctx.CurrentFileFlags & FileFlags.MdlZil) != 0)
            {
                return ROUTINE(ctx, args);
            }
            else
            {
                return PerformDefine(ctx, args, "DEFINE");
            }
        }

        [FSubr]
        public static ZilObject DEFINE20(Context ctx, ZilObject[] args)
        {
            SubrContracts(ctx, args);

            return PerformDefine(ctx, args, "DEFINE20");
        }

        // TODO: merge parsing code for DEFINE, DEFMAC, ROUTINE, and FUNCTION
        private static ZilObject PerformDefine(Context ctx, ZilObject[] args, string name)
        {
            SubrContracts(ctx, args);
            Contract.Requires(name != null);

            if (args.Length < 3)
                throw new InterpreterError(name, 3, 0);

            ZilAtom atom = args[0].Eval(ctx) as ZilAtom;
            if (atom == null)
                throw new InterpreterError(name + ": first arg must evaluate to an atom");
            if (!ctx.AllowRedefine && ctx.GetGlobalVal(atom) != null)
                throw new InterpreterError(name + ": already defined: " + atom.ToStringContext(ctx, false));

            ZilAtom activationAtom;
            ZilList argList;
            IEnumerable<ZilObject> body;

            if (args[1] is ZilAtom)
            {
                activationAtom = (ZilAtom)args[1];
                argList = args[2] as ZilList;
                if (argList == null || argList.GetTypeAtom(ctx).StdAtom != StdAtom.LIST)
                    throw new InterpreterError(name + ": third arg must be a list");
                body = args.Skip(3);

                if (args.Length < 4)
                    throw new InterpreterError(name + ": missing body");
            }
            else if (args[1].GetTypeAtom(ctx).StdAtom == StdAtom.LIST)
            {
                activationAtom = null;
                argList = (ZilList)args[1];
                body = args.Skip(2);
            }
            else
            {
                throw new InterpreterError(name + ": second arg must be an atom or list");
            }

            ZilFunction func = new ZilFunction(
                atom,
                activationAtom,
                argList,
                body);
            ctx.SetGlobalVal(atom, func);
            return atom;
        }

        [FSubr]
        public static ZilObject DEFMAC(Context ctx, ZilObject[] args)
        {
            SubrContracts(ctx, args);

            if (args.Length < 3)
                throw new InterpreterError("DEFMAC", 3, 0);

            ZilAtom atom = args[0].Eval(ctx) as ZilAtom;
            if (atom == null)
                throw new InterpreterError("DEFMAC: first arg must be an atom");
            if (!ctx.AllowRedefine && ctx.GetGlobalVal(atom) != null)
                throw new InterpreterError("DEFMAC: already defined: " + atom.ToStringContext(ctx, false));

            ZilAtom activationAtom;
            ZilList argList;
            IEnumerable<ZilObject> body;

            if (args[1] is ZilAtom)
            {
                activationAtom = (ZilAtom)args[1];
                argList = args[2] as ZilList;
                if (argList == null || argList.GetTypeAtom(ctx).StdAtom != StdAtom.LIST)
                    throw new InterpreterError("DEFMAC: third arg must be a list");
                body = args.Skip(3);

                if (args.Length < 4)
                    throw new InterpreterError("DEFMAC: missing body");
            }
            else if (args[1].GetTypeAtom(ctx).StdAtom == StdAtom.LIST)
            {
                activationAtom = null;
                argList = (ZilList)args[1];
                body = args.Skip(2);
            }
            else
            {
                throw new InterpreterError("DEFMAC: second arg must be an atom or list");
            }

            ZilFunction func = new ZilFunction(
                atom,
                activationAtom,
                argList,
                body);
            ZilEvalMacro macro = new ZilEvalMacro(func);
            macro.SourceLine = ctx.CallingForm.SourceLine;
            ctx.SetGlobalVal(atom, macro);
            return atom;
        }

        [FSubr]
        public static ZilObject QUOTE(Context ctx, ZilObject value)
        {
            SubrContracts(ctx);

            return value;
        }

        [Subr]
        public static ZilObject EVAL(Context ctx, ZilObject value)
        {
            SubrContracts(ctx);

            return value.Eval(ctx);
        }

        [Subr("EVAL-IN-SEGMENT")]
        public static ZilObject EVAL_IN_SEGMENT(Context ctx, ZilObject dummy1,
            ZilObject value, ZilObject dummy2 = null)
        {
            SubrContracts(ctx);

            return value.Eval(ctx);
        }

        [Subr]
        public static ZilObject EXPAND(Context ctx, ZilObject value)
        {
            SubrContracts(ctx);

            var result = value.Expand(ctx);

            if (result == value)
                result = value.Eval(ctx);

            return result;
        }

        [Subr]
        public static ZilObject APPLY(Context ctx, ZilObject[] args)
        {
            SubrContracts(ctx, args);

            if (args.Length == 0)
                throw new InterpreterError("APPLY", 1, 0);

            IApplicable ap = args[0] as IApplicable;
            if (ap == null)
                throw new InterpreterError("APPLY: first arg must be an applicable type");

            ZilObject[] newArgs = new ZilObject[args.Length - 1];
            Array.Copy(args, 1, newArgs, 0, args.Length - 1);
            Contract.Assume(Contract.ForAll(newArgs, a => a != null));
            return ap.ApplyNoEval(ctx, newArgs);
        }
    }
}
