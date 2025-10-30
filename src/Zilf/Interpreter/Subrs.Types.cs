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

using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using Zilf.Interpreter.Values;
using Zilf.Language;
using Zilf.Diagnostics;
using Zilf.Common;

namespace Zilf.Interpreter
{
    [SuppressMessage("ReSharper", "InconsistentNaming")]
    static partial class Subrs
    {
        [Subr]
        public static ZilObject TYPE(Context ctx, ZilObject value)
        {
            return value.GetTypeAtom(ctx);
        }

        [Subr("TYPE?")]
        public static ZilObject TYPE_P(Context ctx, ZilObject value, [Required] ZilAtom[] types)
        {
            var type = value.GetTypeAtom(ctx);

            foreach (var candidate in types)
            {
                if (candidate == type)
                    return candidate;

                // Special case for LVAL/GVAL
                if (candidate.StdAtom == StdAtom.LVAL && value.IsLVAL(out _) ||
                    candidate.StdAtom == StdAtom.GVAL && value.IsGVAL(out _))
                    return candidate;
            }

            return ctx.FALSE;
        }

        static StdAtom PrimTypeToType(PrimType pt)
        {
            return pt switch
            {
                PrimType.ATOM => StdAtom.ATOM,
                PrimType.FIX => StdAtom.FIX,
                PrimType.LIST => StdAtom.LIST,
                PrimType.STRING => StdAtom.STRING,
                PrimType.TABLE => StdAtom.TABLE,
                PrimType.VECTOR => StdAtom.VECTOR,
                _ => throw UnhandledCaseException.FromEnum(pt, "primtype")
            };
        }

        [Subr]
        public static ZilObject PRIMTYPE(Context ctx, ZilObject value)
        {
            return ctx.GetStdAtom(PrimTypeToType(value.PrimType));
        }

        /// <exception cref="InterpreterError"><paramref name="type"/> is not a registered type.</exception>
        [Subr]
        public static ZilObject TYPEPRIM(Context ctx, ZilAtom type)
        {
            if (!ctx.IsRegisteredType(type))
                throw new InterpreterError(InterpreterMessages._0_Unrecognized_1_2, "TYPEPRIM", "type", type.ToStringContext(ctx, false));

            return ctx.GetStdAtom(PrimTypeToType(ctx.GetTypePrim(type)));
        }

        [Subr]
        public static ZilObject CHTYPE(Context ctx, ZilObject value, ZilAtom atom)
        {
            return ctx.ChangeType(value, atom);
        }

        /// <exception cref="InterpreterError"><paramref name="name"/> is already a registered type, or <paramref name="primtypeAtom"/> is not.</exception>
        [Subr]
        public static ZilObject NEWTYPE(Context ctx, ZilAtom name, ZilAtom primtypeAtom,
             ZilObject? decl = null)
        {
            if (ctx.IsRegisteredType(name))
                throw new InterpreterError(InterpreterMessages._0_Already_Defined_1, "NEWTYPE", name.ToStringContext(ctx, false));

            PrimType primtype;
            if (ctx.IsRegisteredType(primtypeAtom))
                primtype = ctx.GetTypePrim(primtypeAtom);
            else
                throw new InterpreterError(InterpreterMessages._0_Unrecognized_1_2, "NEWTYPE", "primtype", primtypeAtom.ToStringContext(ctx, false));

            ctx.RegisterType(name, primtype);
            ctx.PutProp(name, ctx.GetStdAtom(StdAtom.DECL), decl);
            return name;
        }

        [Subr]
        public static ZilObject ALLTYPES(Context ctx)
        {
            return new ZilVector(ctx.RegisteredTypes.ToArray<ZilObject>());
        }

        [Subr("VALID-TYPE?")]
        public static ZilObject VALID_TYPE_P(Context ctx, ZilAtom atom)
        {
            return ctx.IsRegisteredType(atom) ? atom : ctx.FALSE;
        }

        [Subr]
        public static ZilObject PRINTTYPE(Context ctx, ZilAtom atom,
             [Decl("<OR ATOM APPLICABLE>")] ZilObject? handler = null)
        {
            return PerformTypeHandler(ctx, atom, handler,
                "PRINTTYPE",
                (c, a) => c.GetPrintType(a),
                (c, a, h) => c.SetPrintType(a, h));
        }

        [Subr]
        public static ZilObject EVALTYPE(Context ctx, ZilAtom atom,
             [Decl("<OR ATOM APPLICABLE>")] ZilObject? handler = null)
        {
            return PerformTypeHandler(ctx, atom, handler,
                "EVALTYPE",
                (c, a) => c.GetEvalType(a),
                (c, a, h) => c.SetEvalType(a, h));
        }

        [Subr]
        public static ZilObject APPLYTYPE(Context ctx, ZilAtom atom,
             [Decl("<OR ATOM APPLICABLE>")] ZilObject? handler = null)
        {
            return PerformTypeHandler(ctx, atom, handler,
                "APPLYTYPE",
                (c, a) => c.GetApplyType(a),
                (c, a, h) => c.SetApplyType(a, h));
        }

        static ZilObject PerformTypeHandler(Context ctx, ZilAtom atom, ZilObject? handler,
            string name,
            Func<Context, ZilAtom, ZilObject?> getter,
            Func<Context, ZilAtom, ZilObject, Context.SetTypeHandlerResult> setter)
        {
            if (!ctx.IsRegisteredType(atom))
                throw new InterpreterError(InterpreterMessages._0_Unrecognized_1_2, name, "type", atom.ToStringContext(ctx, false));

            if (handler == null)
            {
                return getter(ctx, atom) ?? ctx.FALSE;
            }

            var result = setter(ctx, atom, handler);
            return result switch
            {
                Context.SetTypeHandlerResult.OK =>
                    atom,
                Context.SetTypeHandlerResult.BadHandlerType =>
                    // the caller should check the handler type, but just in case...
                    throw new InterpreterError(InterpreterMessages._0_Must_Be_1, "handler", "atom or applicable value"),
                Context.SetTypeHandlerResult.OtherTypeNotRegistered =>
                    throw new InterpreterError(
                        InterpreterMessages._0_Unrecognized_1_2,
                        name,
                        "type",
                        handler.ToStringContext(ctx, false)),
                Context.SetTypeHandlerResult.OtherTypePrimDiffers =>
                    throw new InterpreterError(
                        InterpreterMessages._0_Primtypes_Of_1_And_2_Differ,
                        name,
                        atom.ToStringContext(ctx, false),
                        handler.ToStringContext(ctx, false)),
                _ =>
                    throw UnhandledCaseException.FromEnum(result)
            };
        }

        [Subr("MAKE-GVAL")]
        public static ZilObject MAKE_GVAL(Context ctx, ZilObject arg)
        {
            return new ZilForm(new[] { ctx.GetStdAtom(StdAtom.GVAL), arg }) { SourceLine = SourceLines.MakeGval };
        }

        [Subr("APPLICABLE?")]
        public static ZilObject APPLICABLE_P(Context ctx, ZilObject arg)
        {
            return arg.IsApplicable(ctx) ? ctx.TRUE : ctx.FALSE;
        }

        [Subr("STRUCTURED?")]
        public static ZilObject STRUCTURED_P(Context ctx, ZilObject arg)
        {
            return (arg is IStructure) ? ctx.TRUE : ctx.FALSE;
        }

        [Subr("LEGAL?")]
        public static ZilObject LEGAL_P(Context ctx, ZilObject arg)
        {
            // non-evanescent values are always legal
            return (arg as IEvanescent)?.IsLegal == false ? ctx.FALSE : ctx.TRUE;
        }

        [Subr]
        public static ZilObject FORM(Context ctx, ZilObject[] args)
        {
            return new ZilForm(args) { SourceLine = ctx.TopFrame.SourceLine };
        }

        [Subr]
        public static ZilObject LIST(Context ctx, ZilObject[] args)
        {
            return new ZilList(args);
        }

        [Subr]
        [Subr("TUPLE")]
        public static ZilObject VECTOR(Context ctx, ZilObject[] args)
        {
            return new ZilVector(args);
        }

        /// <exception cref="InterpreterError"><paramref name="count"/> is negative.</exception>
        [Subr]
        public static ZilResult ILIST(Context ctx, int count, ZilObject? init = null)
        {
            if (count < 0)
                throw new InterpreterError(
                    InterpreterMessages._0_Expected_1,
                    "ILIST: arg 1",
                    "a non-negative FIX");

            var contents = new List<ZilObject>(count);
            for (int i = 0; i < count; i++)
            {
                if (init != null)
                {
                    var zr = init.Eval(ctx);
                    if (zr.ShouldPass())
                        return zr;
                    contents.Add((ZilObject)zr);
                }
                else
                    contents.Add(ctx.FALSE);
            }

            return new ZilList(contents);
        }

        /// <exception cref="InterpreterError"><paramref name="count"/> is negative.</exception>
        [Subr]
        public static ZilResult IVECTOR(Context ctx, int count, ZilObject? init = null)
        {
            if (count < 0)
                throw new InterpreterError(
                    InterpreterMessages._0_Expected_1,
                    "IVECTOR: arg 1",
                    "a non-negative FIX");

            var contents = new List<ZilObject>(count);
            for (int i = 0; i < count; i++)
            {
                if (init != null)
                {
                    var zr = init.Eval(ctx);
                    if (zr.ShouldPass())
                        return zr;

                    contents.Add((ZilObject)zr);
                }
                else
                    contents.Add(ctx.FALSE);
            }

            return new ZilVector(contents.ToArray());
        }

        [Subr]
        public static ZilObject BYTE(Context ctx, ZilObject arg)
        {
            return ctx.ChangeType(arg, ctx.GetStdAtom(StdAtom.BYTE));
        }

        [Subr]
        public static ZilObject CONS(Context ctx, ZilObject first, ZilList rest)
        {
            return new ZilList(
                first,
                rest is ZilList restList ? restList : new ZilList(rest));
        }

        [FSubr]
        public static ZilObject FUNCTION(Context ctx, [Optional] ZilAtom? activationAtom,
             ZilList argList, [Optional] ZilDecl? decl, [Required] ZilObject[] body)
        {
            return new ZilFunction("FUNCTION", null, activationAtom, argList, decl, body);
        }

        [Subr]
        public static ZilObject STRING(Context ctx,
             [Either(typeof(ZilString), typeof(ZilChar))] ZilObject[] args)
        {
            var sb = new StringBuilder();

            foreach (var arg in args)
            {
                sb.Append(arg.ToStringContext(ctx, true));
            }

            return ZilString.FromString(sb.ToString());
        }

        /// <exception cref="InterpreterError"><paramref name="count"/> is negative.</exception>
        [Subr]
        public static ZilResult ISTRING(Context ctx, int count, ZilObject? init = null)
        {
            if (count < 0)
                throw new InterpreterError(
                    InterpreterMessages._0_Expected_1,
                    "ISTRING: arg 1",
                    "a non-negative FIX");

            var contents = new List<char>(count);
            for (int i = 0; i < count; i++)
            {
                if (init != null)
                {
                    var initResult = init.Eval(ctx);
                    if (initResult.ShouldPass())
                        return initResult;

                    if ((ZilObject)initResult is not ZilChar ch)
                        throw new InterpreterError(InterpreterMessages._0_Iterated_Values_Must_Be_CHARACTERs, "ISTRING");
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
            if (arg is ZilChar ch)
                return new ZilFix(ch.Char);

            return new ZilChar((char)((ZilFix)arg).Value);
        }

    }
}
