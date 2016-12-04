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
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using Zilf.Interpreter.Values;
using Zilf.Language;
using Zilf.Diagnostics;

namespace Zilf.Interpreter
{
    static partial class Subrs
    {
        [Subr]
        public static ZilObject TYPE(Context ctx, ZilObject value)
        {
            SubrContracts(ctx);

            return value.GetTypeAtom(ctx);
        }

        [Subr("TYPE?")]
        public static ZilObject TYPE_P(Context ctx, ZilObject value, [Required] ZilAtom[] types)
        {
            SubrContracts(ctx);

            var type = value.GetTypeAtom(ctx);
            return types.FirstOrDefault(a => a == type) ?? ctx.FALSE;
        }

        private static StdAtom PrimTypeToType(PrimType pt)
        {
            switch (pt)
            {
                case PrimType.ATOM:
                    return StdAtom.ATOM;
                case PrimType.FIX:
                    return StdAtom.FIX;
                case PrimType.LIST:
                    return StdAtom.LIST;
                case PrimType.STRING:
                    return StdAtom.STRING;
                case PrimType.TABLE:
                    return StdAtom.TABLE;
                case PrimType.VECTOR:
                    return StdAtom.VECTOR;
                default:
                    throw new NotImplementedException("unknown primtype " + pt);
            }
        }

        [Subr]
        public static ZilObject PRIMTYPE(Context ctx, ZilObject value)
        {
            SubrContracts(ctx);

            return ctx.GetStdAtom(PrimTypeToType(value.PrimType));
        }

        [Subr]
        public static ZilObject TYPEPRIM(Context ctx, ZilAtom type)
        {
            SubrContracts(ctx);

            if (!ctx.IsRegisteredType(type))
                throw new InterpreterError("TYPEPRIM: not a registered type: " + type.ToStringContext(ctx, false));

            return ctx.GetStdAtom(PrimTypeToType(ctx.GetTypePrim(type)));
        }

        [Subr]
        public static ZilObject CHTYPE(Context ctx, ZilObject value, ZilAtom atom)
        {
            SubrContracts(ctx);

            return ctx.ChangeType(value, atom);
        }

        [Subr]
        public static ZilObject NEWTYPE(Context ctx, ZilAtom name, ZilAtom primtypeAtom, ZilObject decl = null)
        {
            SubrContracts(ctx);

            if (ctx.IsRegisteredType(name))
                throw new InterpreterError("NEWTYPE: already registered: " + name.ToStringContext(ctx, false));

            PrimType primtype;
            if (ctx.IsRegisteredType(primtypeAtom))
                primtype = ctx.GetTypePrim(primtypeAtom);
            else
                throw new InterpreterError("NEWTYPE: unrecognized primtype: " + primtypeAtom.ToStringContext(ctx, false));

            ctx.RegisterType(name, primtype);
            ctx.PutProp(name, ctx.GetStdAtom(StdAtom.DECL), decl);
            return name;
        }

        [Subr]
        public static ZilObject ALLTYPES(Context ctx)
        {
            SubrContracts(ctx);

            return new ZilVector(ctx.RegisteredTypes.ToArray());
        }

        [Subr("VALID-TYPE?")]
        public static ZilObject VALID_TYPE_P(Context ctx, ZilAtom atom)
        {
            SubrContracts(ctx);

            return ctx.IsRegisteredType(atom) ? atom : ctx.FALSE;
        }

        [Subr]
        public static ZilObject PRINTTYPE(Context ctx, ZilAtom atom,
            [Decl("<OR ATOM APPLICABLE>")] ZilObject handler = null)
        {
            SubrContracts(ctx);

            return PerformTypeHandler(ctx, atom, handler,
                "PRINTTYPE",
                (c, a) => c.GetPrintType(a),
                (c, a, h) => c.SetPrintType(a, h));
        }

        [Subr]
        public static ZilObject EVALTYPE(Context ctx, ZilAtom atom,
            [Decl("<OR ATOM APPLICABLE>")] ZilObject handler = null)
        {
            SubrContracts(ctx);

            return PerformTypeHandler(ctx, atom, handler,
                "EVALTYPE",
                (c, a) => c.GetEvalType(a),
                (c, a, h) => c.SetEvalType(a, h));
        }

        [Subr]
        public static ZilObject APPLYTYPE(Context ctx, ZilAtom atom,
            [Decl("<OR ATOM APPLICABLE>")] ZilObject handler = null)
        {
            SubrContracts(ctx);

            return PerformTypeHandler(ctx, atom, handler,
                "APPLYTYPE",
                (c, a) => c.GetApplyType(a),
                (c, a, h) => c.SetApplyType(a, h));
        }

        private static ZilObject PerformTypeHandler(Context ctx, ZilAtom atom, ZilObject handler,
            string name,
            Func<Context, ZilAtom, ZilObject> getter,
            Func<Context, ZilAtom, ZilObject, Context.SetTypeHandlerResult> setter)
        {
            if (!ctx.IsRegisteredType(atom))
                throw new InterpreterError($"{name}: not a registered type: {atom.ToStringContext(ctx, false)}");

            if (handler == null)
            {
                return getter(ctx, atom) ?? ctx.FALSE;
            }

            switch (setter(ctx, atom, handler))
            {
                case Context.SetTypeHandlerResult.OK:
                    return atom;

                case Context.SetTypeHandlerResult.BadHandlerType:
                    throw new InterpreterError($"{name}: second arg must be an atom or applicable");

                case Context.SetTypeHandlerResult.OtherTypeNotRegistered:
                    throw new InterpreterError($"{name}: not a registered type: {handler.ToStringContext(ctx, false)}");

                case Context.SetTypeHandlerResult.OtherTypePrimDiffers:
                    throw new InterpreterError(
                        $"{name}: primtypes of {atom.ToStringContext(ctx, false)} and {handler.ToStringContext(ctx, false)} differ");

                default:
                    throw new NotImplementedException();
            }
        }

        [Subr("MAKE-GVAL")]
        public static ZilObject MAKE_GVAL(Context ctx, ZilObject arg)
        {
            SubrContracts(ctx);

            return new ZilForm(new ZilObject[] { ctx.GetStdAtom(StdAtom.GVAL), arg }) { SourceLine = SourceLines.MakeGval };
        }

        [Subr("APPLICABLE?")]
        public static ZilObject APPLICABLE_P(Context ctx, ZilObject arg)
        {
            SubrContracts(ctx);

            return arg.IsApplicable(ctx) ? ctx.TRUE : ctx.FALSE;
        }

        [Subr("STRUCTURED?")]
        public static ZilObject STRUCTURED_P(Context ctx, ZilObject arg)
        {
            SubrContracts(ctx);

            return (arg is IStructure) ? ctx.TRUE : ctx.FALSE;
        }

        [Subr("LEGAL?")]
        public static ZilObject LEGAL_P(Context ctx, ZilObject arg)
        {
            SubrContracts(ctx);

            // non-evanescent values are always legal
            return (arg as IEvanescent)?.IsLegal == false ? ctx.FALSE : ctx.TRUE;
        }

        [Subr]
        public static ZilObject FORM(Context ctx, ZilObject[] args)
        {
            SubrContracts(ctx, args);

            if (args.Length == 0)
                throw new InterpreterError("FORM", 1, 0);

            var result = new ZilForm(args);
            result.SourceLine = ctx.TopFrame.SourceLine;
            return result;
        }

        [Subr]
        public static ZilObject LIST(Context ctx, ZilObject[] args)
        {
            SubrContracts(ctx, args);

            return new ZilList(args);
        }

        [Subr]
        [Subr("TUPLE")]
        public static ZilObject VECTOR(Context ctx, ZilObject[] args)
        {
            SubrContracts(ctx, args);

            return new ZilVector(args);
        }
        
        [Subr]
        public static ZilObject ILIST(Context ctx, int count, ZilObject init = null)
        {
            SubrContracts(ctx);

            if (count < 0)
                throw new InterpreterError(InterpreterMessages.FUNCNAME0_First_Arg_Must_Be_A_Nonnegative_FIX, "ILIST");

            var contents = new List<ZilObject>(count);
            for (int i = 0; i < count; i++)
            {
                if (init != null)
                    contents.Add(init.Eval(ctx));
                else
                    contents.Add(ctx.FALSE);
            }

            return new ZilList(contents);
        }

        [Subr]
        public static ZilObject IVECTOR(Context ctx, int count, ZilObject init=null )
        {
            SubrContracts(ctx);

            if (count < 0)
                throw new InterpreterError(InterpreterMessages.FUNCNAME0_First_Arg_Must_Be_A_Nonnegative_FIX, "IVECTOR");

            var contents = new List<ZilObject>(count);
            for (int i = 0; i < count; i++)
            {
                if (init != null)
                    contents.Add(init.Eval(ctx));
                else
                    contents.Add(ctx.FALSE);
            }

            return new ZilVector(contents.ToArray());
        }

        [Subr]
        public static ZilObject BYTE(Context ctx, ZilObject arg)
        {
            SubrContracts(ctx);

            return ctx.ChangeType(arg, ctx.GetStdAtom(StdAtom.BYTE));
        }

        [Subr]
        public static ZilObject CONS(Context ctx, ZilObject first, ZilList rest)
        {
            SubrContracts(ctx);

            if (rest.GetTypeAtom(ctx).StdAtom != StdAtom.LIST)
                rest = new ZilList(rest);

            return new ZilList(first, rest);
        }

        [FSubr]
        public static ZilObject FUNCTION(Context ctx, [Optional] ZilAtom activationAtom,
            ZilList argList, [Optional] ZilDecl decl, [Required] ZilObject[] body)
        {
            SubrContracts(ctx);

            return new ZilFunction(null, activationAtom, argList, decl, body);
        }

        [Subr]
        public static ZilObject STRING(Context ctx,
            [Decl("<LIST [REST <OR STRING CHARACTER>]>")] ZilObject[] args)
        {
            SubrContracts(ctx, args);

            StringBuilder sb = new StringBuilder();

            foreach (ZilObject arg in args)
            {
                switch (arg.GetTypeAtom(ctx).StdAtom)
                {
                    case StdAtom.STRING:
                    case StdAtom.CHARACTER:
                        sb.Append(arg.ToStringContext(ctx, true));
                        break;

                    default:
                        // shouldn't get here
                        throw new NotImplementedException();
                }
            }

            return ZilString.FromString(sb.ToString());
        }

        [Subr]
        public static ZilObject ISTRING(Context ctx, int count, ZilObject init = null)
        {
            SubrContracts(ctx);

            if (count < 0)
                throw new InterpreterError(InterpreterMessages.FUNCNAME0_First_Arg_Must_Be_A_Nonnegative_FIX, "ISTRING");

            var contents = new List<char>(count);
            for (int i = 0; i < count; i++)
            {
                if (init != null)
                {
                    var ch = init.Eval(ctx) as ZilChar;
                    if (ch == null)
                        throw new InterpreterError(InterpreterMessages.ISTRING_Iterated_Values_Must_Be_CHARACTERs);
                    contents.Add(ch.Char);
                }
                else
                    contents.Add('\0');
            }

            return ZilString.FromString(new string(contents.ToArray()));
        }

        [Subr]
        public static ZilObject ASCII(Context ctx, [Decl("<OR CHARACTER FIX>")] ZilObject arg)
        {
            SubrContracts(ctx);

            ZilChar ch = arg as ZilChar;
            if (ch != null)
                return new ZilFix(ch.Char);

            return new ZilChar((char)((ZilFix)arg).Value);
        }

    }
}
