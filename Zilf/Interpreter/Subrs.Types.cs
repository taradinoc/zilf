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

using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using JetBrains.Annotations;
using Zilf.Interpreter.Values;
using Zilf.Language;
using Zilf.Diagnostics;
using Zilf.Common;

namespace Zilf.Interpreter
{
    [SuppressMessage("ReSharper", "InconsistentNaming")]
    static partial class Subrs
    {
        [NotNull]
        [Subr]
        public static ZilObject TYPE([NotNull] Context ctx, [NotNull] ZilObject value)
        {
            return value.GetTypeAtom(ctx);
        }

        [NotNull]
        [Subr("TYPE?")]
        public static ZilObject TYPE_P([NotNull] Context ctx, [NotNull] ZilObject value, [NotNull] [Required] ZilAtom[] types)
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
                    throw UnhandledCaseException.FromEnum(pt, "primtype");
            }
        }

        [NotNull]
        [Subr]
        public static ZilObject PRIMTYPE([NotNull] Context ctx, [NotNull] ZilObject value)
        {
            return ctx.GetStdAtom(PrimTypeToType(value.PrimType));
        }

        /// <exception cref="InterpreterError"><paramref name="type"/> is not a registered type.</exception>
        [NotNull]
        [Subr]
        public static ZilObject TYPEPRIM([NotNull] Context ctx, [NotNull] ZilAtom type)
        {
            if (!ctx.IsRegisteredType(type))
                throw new InterpreterError(InterpreterMessages._0_Unrecognized_1_2, "TYPEPRIM", "type", type.ToStringContext(ctx, false));

            return ctx.GetStdAtom(PrimTypeToType(ctx.GetTypePrim(type)));
        }

        [NotNull]
        [Subr]
        public static ZilObject CHTYPE([NotNull] Context ctx, [NotNull] ZilObject value, [NotNull] ZilAtom atom)
        {
            return ctx.ChangeType(value, atom);
        }

        /// <exception cref="InterpreterError"><paramref name="name"/> is already a registered type, or <paramref name="primtypeAtom"/> is not.</exception>
        [NotNull]
        [Subr]
        public static ZilObject NEWTYPE([NotNull] Context ctx, [NotNull] ZilAtom name, [NotNull] ZilAtom primtypeAtom,
            [CanBeNull] ZilObject decl = null)
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

        [NotNull]
        [Subr]
        public static ZilObject ALLTYPES([NotNull] Context ctx)
        {
            return new ZilVector(ctx.RegisteredTypes.ToArray<ZilObject>());
        }

        [NotNull]
        [Subr("VALID-TYPE?")]
        public static ZilObject VALID_TYPE_P([NotNull] Context ctx, [NotNull] ZilAtom atom)
        {
            return ctx.IsRegisteredType(atom) ? atom : ctx.FALSE;
        }

        [NotNull]
        [Subr]
        public static ZilObject PRINTTYPE([NotNull] Context ctx, [NotNull] ZilAtom atom,
            [CanBeNull] [Decl("<OR ATOM APPLICABLE>")] ZilObject handler = null)
        {
            return PerformTypeHandler(ctx, atom, handler,
                "PRINTTYPE",
                (c, a) => c.GetPrintType(a),
                (c, a, h) => c.SetPrintType(a, h));
        }

        [NotNull]
        [Subr]
        public static ZilObject EVALTYPE([NotNull] Context ctx, [NotNull] ZilAtom atom,
            [CanBeNull] [Decl("<OR ATOM APPLICABLE>")] ZilObject handler = null)
        {
            return PerformTypeHandler(ctx, atom, handler,
                "EVALTYPE",
                (c, a) => c.GetEvalType(a),
                (c, a, h) => c.SetEvalType(a, h));
        }

        [NotNull]
        [Subr]
        public static ZilObject APPLYTYPE([NotNull] Context ctx, [NotNull] ZilAtom atom,
            [CanBeNull] [Decl("<OR ATOM APPLICABLE>")] ZilObject handler = null)
        {
            return PerformTypeHandler(ctx, atom, handler,
                "APPLYTYPE",
                (c, a) => c.GetApplyType(a),
                (c, a, h) => c.SetApplyType(a, h));
        }

        [NotNull]
        static ZilObject PerformTypeHandler([NotNull] Context ctx, [NotNull] ZilAtom atom, [CanBeNull] ZilObject handler,
            string name,
            Func<Context, ZilAtom, ZilObject> getter,
            Func<Context, ZilAtom, ZilObject, Context.SetTypeHandlerResult> setter)
        {
            if (!ctx.IsRegisteredType(atom))
                throw new InterpreterError(InterpreterMessages._0_Unrecognized_1_2, name, "type", atom.ToStringContext(ctx, false));

            if (handler == null)
            {
                return getter(ctx, atom) ?? ctx.FALSE;
            }

            var result = setter(ctx, atom, handler);
            switch (result)
            {
                case Context.SetTypeHandlerResult.OK:
                    return atom;

                case Context.SetTypeHandlerResult.BadHandlerType:
                    // the caller should check the handler type, but just in case...
                    throw new InterpreterError(InterpreterMessages._0_Must_Be_1, "handler", "atom or applicable value");

                case Context.SetTypeHandlerResult.OtherTypeNotRegistered:
                    throw new InterpreterError(InterpreterMessages._0_Unrecognized_1_2, name, "type", handler.ToStringContext(ctx, false));

                case Context.SetTypeHandlerResult.OtherTypePrimDiffers:
                    throw new InterpreterError(
                        InterpreterMessages._0_Primtypes_Of_1_And_2_Differ, name, atom.ToStringContext(ctx, false), handler.ToStringContext(ctx, false));

                default:
                    throw UnhandledCaseException.FromEnum(result);
            }
        }

        [NotNull]
        [Subr("MAKE-GVAL")]
        public static ZilObject MAKE_GVAL([NotNull] Context ctx, ZilObject arg)
        {
            return new ZilForm(new[] { ctx.GetStdAtom(StdAtom.GVAL), arg }) { SourceLine = SourceLines.MakeGval };
        }

        [NotNull]
        [Subr("APPLICABLE?")]
        public static ZilObject APPLICABLE_P([NotNull] Context ctx, ZilObject arg)
        {
            return arg.IsApplicable(ctx) ? ctx.TRUE : ctx.FALSE;
        }

        [NotNull]
        [Subr("STRUCTURED?")]
        public static ZilObject STRUCTURED_P([NotNull] Context ctx, ZilObject arg)
        {
            return (arg is IStructure) ? ctx.TRUE : ctx.FALSE;
        }

        [NotNull]
        [Subr("LEGAL?")]
        public static ZilObject LEGAL_P([NotNull] Context ctx, ZilObject arg)
        {
            // non-evanescent values are always legal
            return (arg as IEvanescent)?.IsLegal == false ? ctx.FALSE : ctx.TRUE;
        }

        [NotNull]
        [Subr]
        public static ZilObject FORM([NotNull] Context ctx, [NotNull] [Required] ZilObject[] args)
        {
            return new ZilForm(args) { SourceLine = ctx.TopFrame.SourceLine };
        }

        [NotNull]
        [Subr]
        public static ZilObject LIST(Context ctx, [NotNull] ZilObject[] args)
        {
            return new ZilList(args);
        }

        [NotNull]
        [Subr]
        [Subr("TUPLE")]
        public static ZilObject VECTOR(Context ctx, [NotNull] ZilObject[] args)
        {
            return new ZilVector(args);
        }

        /// <exception cref="InterpreterError"><paramref name="count"/> is negative.</exception>
        [Subr]
        public static ZilResult ILIST(Context ctx, int count, [CanBeNull] ZilObject init = null)
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
        public static ZilResult IVECTOR(Context ctx, int count, [CanBeNull] ZilObject init = null)
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

        [NotNull]
        [Subr]
        public static ZilObject BYTE([NotNull] Context ctx, [NotNull] ZilObject arg)
        {
            return ctx.ChangeType(arg, ctx.GetStdAtom(StdAtom.BYTE));
        }

        [NotNull]
        [Subr]
        public static ZilObject CONS(Context ctx, ZilObject first, [NotNull] ZilListBase rest)
        {
            return new ZilList(
                first,
                rest is ZilList restList ? restList : new ZilList(rest));
        }

        [NotNull]
        [FSubr]
        public static ZilObject FUNCTION(Context ctx, [CanBeNull] [Optional] ZilAtom activationAtom,
            [NotNull] ZilList argList, [CanBeNull] [Optional] ZilDecl decl, [NotNull] [Required] ZilObject[] body)
        {
            return new ZilFunction("FUNCTION", null, activationAtom, argList, decl, body);
        }

        [NotNull]
        [Subr]
        public static ZilObject STRING(Context ctx,
            [NotNull] [Decl("<LIST [REST <OR STRING CHARACTER>]>")] ZilObject[] args)
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
        public static ZilResult ISTRING(Context ctx, int count, [CanBeNull] ZilObject init = null)
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

                    if (!((ZilObject)initResult is ZilChar ch))
                        throw new InterpreterError(InterpreterMessages._0_Iterated_Values_Must_Be_CHARACTERs, "ISTRING");
                    contents.Add(ch.Char);
                }
                else
                    contents.Add('\0');
            }

            return ZilString.FromString(new string(contents.ToArray()));
        }

        [NotNull]
        [Subr]
        public static ZilObject ASCII(Context ctx, [Decl("<OR CHARACTER FIX>")] ZilObject arg)
        {
            if (arg is ZilChar ch)
                return new ZilFix(ch.Char);

            return new ZilChar((char)((ZilFix)arg).Value);
        }

    }
}
