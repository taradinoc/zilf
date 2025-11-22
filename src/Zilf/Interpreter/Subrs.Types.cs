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
        /// <summary>
        /// Returns the type of the specified value.
        /// </summary>
        /// <param name="ctx"></param>
        /// <param name="value">The value to check.</param>
        /// <returns>The value's type atom.</returns>
        [Subr]
        public static ZilObject TYPE(Context ctx, ZilObject value)
        {
            return value.GetTypeAtom(ctx);
        }

        /// <summary>
        /// Checks whether a value's type is among a set of specified types.
        /// </summary>
        /// <param name="ctx"></param>
        /// <param name="value">The value to check.</param>
        /// <param name="types">The types to check against.</param>
        /// <returns>The value's type atom if it matches one in the set; otherwise, false.</returns>
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

        /// <summary>
        /// Returns a value's primitive type.
        /// </summary>
        /// <param name="ctx"></param>
        /// <param name="value">The value to check.</param>
        /// <returns>A primtype atom (ATOM, FIX, LIST, STRING, TABLE, or VECTOR).</returns>
        [Subr]
        public static ZilObject PRIMTYPE(Context ctx, ZilObject value)
        {
            return ctx.GetStdAtom(PrimTypeToType(value.PrimType));
        }

        /// <summary>
        /// Returns the primitive type of a registered type.
        /// </summary>
        /// <param name="ctx"></param>
        /// <param name="type">The type atom to check.</param>
        /// <returns>The type's primtype atom (ATOM, FIX, LIST, STRING, TABLE, or VECTOR).</returns>
        /// <exception cref="InterpreterError"><paramref name="type"/> is not a registered type.</exception>
        [Subr]
        public static ZilObject TYPEPRIM(Context ctx, ZilAtom type)
        {
            if (!ctx.IsRegisteredType(type))
                throw new InterpreterError(InterpreterMessages._0_Unrecognized_Type_1, "TYPEPRIM", type.ToStringContext(ctx, false));

            return ctx.GetStdAtom(PrimTypeToType(ctx.GetTypePrim(type)));
        }

        /// <summary>
        /// Changes the type of a value to the specified type.
        /// </summary>
        /// <param name="ctx"></param>
        /// <param name="value">The value to convert.</param>
        /// <param name="atom">The target type atom, which must share the same primtype as the value (with some exceptions).</param>
        /// <returns>The converted value.</returns>
        /// <exception cref="InterpreterError"><paramref name="value"/> cannot be converted to the specified type.</exception>
        [Subr]
        public static ZilObject CHTYPE(Context ctx, ZilObject value, ZilAtom atom)
        {
            return ctx.ChangeType(value, atom);
        }

        /// <summary>
        /// Registers a new type.
        /// </summary>
        /// <param name="ctx"></param>
        /// <param name="name">The new type atom.</param>
        /// <param name="primtypeAtom">The primtype that the new type will be based on (ATOM, FIX, LIST, STRING, TABLE, or VECTOR).</param>
        /// <param name="decl">Optionally, the DECL that values of the new type must match.</param>
        /// <returns>The new type atom.</returns>
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
                throw new InterpreterError(InterpreterMessages._0_Unrecognized_Primtype_1, "NEWTYPE", primtypeAtom.ToStringContext(ctx, false));

            ctx.RegisterType(name, primtype);
            ctx.PutProp(name, ctx.GetStdAtom(StdAtom.DECL), decl);
            return name;
        }

        /// <summary>
        /// Returns all registered types.
        /// </summary>
        /// <param name="ctx"></param>
        /// <returns>A vector containing every registered type atom.</returns>
        [Subr]
        public static ZilObject ALLTYPES(Context ctx)
        {
            return new ZilVector(ctx.RegisteredTypes.ToArray<ZilObject>());
        }

        /// <summary>
        /// Checks whether a registered type exists.
        /// </summary>
        /// <param name="ctx"></param>
        /// <param name="atom">The type atom to check.</param>
        /// <returns>The type atom if it is registered; otherwise, false.</returns>
        [Subr("VALID-TYPE?")]
        public static ZilObject VALID_TYPE_P(Context ctx, ZilAtom atom)
        {
            return ctx.IsRegisteredType(atom) ? atom : ctx.FALSE;
        }

        /// <summary>
        /// Gets or sets the PRINT handler for the specified type.
        /// </summary>
        /// <param name="ctx"></param>
        /// <param name="atom">The type atom whose PRINT handler will be set or retrieved.</param>
        /// <param name="handler">An applicable value to set as the new PRINT handler,
        /// or a type atom to copy another type's handler. If omitted, the current handler is returned.</param>
        /// <returns>The old or new PRINT handler.</returns>
        [Subr]
        public static ZilObject PRINTTYPE(Context ctx, ZilAtom atom,
             [Decl("<OR ATOM APPLICABLE>")] ZilObject? handler = null)
        {
            return PerformTypeHandler(ctx, atom, handler,
                "PRINTTYPE",
                (c, a) => c.GetPrintType(a),
                (c, a, h) => c.SetPrintType(a, h));
        }

        /// <summary>
        /// Gets or sets the EVAL handler for the specified type.
        /// </summary>
        /// <param name="ctx"></param>
        /// <param name="atom">The type atom whose EVAL handler will be set or retrieved.</param>
        /// <param name="handler">An applicable value to set as the new EVAL handler,
        /// or a type atom to copy another type's handler. If omitted, the current handler is returned.</param>
        /// <returns>The old or new EVAL handler.</returns>
        [Subr]
        public static ZilObject EVALTYPE(Context ctx, ZilAtom atom,
             [Decl("<OR ATOM APPLICABLE>")] ZilObject? handler = null)
        {
            return PerformTypeHandler(ctx, atom, handler,
                "EVALTYPE",
                (c, a) => c.GetEvalType(a),
                (c, a, h) => c.SetEvalType(a, h));
        }

        /// <summary>
        /// Gets or sets the APPLY handler for the specified type.
        /// </summary>
        /// <param name="ctx"></param>
        /// <param name="atom">The type atom whose APPLY handler will be set or retrieved.</param>
        /// <param name="handler">An applicable value to set as the new APPLY handler,
        /// or a type atom to copy another type's handler. If omitted, the current handler is returned.</param>
        /// <returns>The old or new APPLY handler.</returns>
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
                throw new InterpreterError(InterpreterMessages._0_Unrecognized_Type_1, name, atom.ToStringContext(ctx, false));

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
                        InterpreterMessages._0_Unrecognized_Type_1,
                        name,
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

        /// <summary>
        /// Creates a GVAL form wrapping the specified argument.
        /// </summary>
        /// <param name="ctx"></param>
        /// <param name="arg">The argument to wrap in a GVAL form (typically an atom).</param>
        /// <returns>The GVAL form.</returns>
        [Subr("MAKE-GVAL")]
        public static ZilObject MAKE_GVAL(Context ctx, ZilObject arg)
        {
            return new ZilForm(new[] { ctx.GetStdAtom(StdAtom.GVAL), arg }) { SourceLine = SourceLines.MakeGval };
        }

        /// <summary>
        /// Checks whether a value is applicable.
        /// </summary>
        /// <param name="ctx"></param>
        /// <param name="arg">The value to check.</param>
        /// <returns>True if the value is of an applicable type; otherwise, false.</returns>
        [Subr("APPLICABLE?")]
        public static ZilObject APPLICABLE_P(Context ctx, ZilObject arg)
        {
            return arg.IsApplicable(ctx) ? ctx.TRUE : ctx.FALSE;
        }

        /// <summary>
        /// Checks whether a value is structured.
        /// </summary>
        /// <param name="ctx"></param>
        /// <param name="arg">The value to check.</param>
        /// <returns>True if the value is of a structured type; otherwise, false.</returns>
        [Subr("STRUCTURED?")]
        public static ZilObject STRUCTURED_P(Context ctx, ZilObject arg)
        {
            return (arg is IStructure) ? ctx.TRUE : ctx.FALSE;
        }

        /// <summary>
        /// Checks whether a value is legal to use, i.e. whether it is not an ACTIVATION or ENVIRONMENT whose lifetime has ended.
        /// </summary>
        /// <param name="ctx"></param>
        /// <param name="arg">The value to check.</param>
        /// <returns>True if the value is legal to use; otherwise, false.</returns>
        [Subr("LEGAL?")]
        public static ZilObject LEGAL_P(Context ctx, ZilObject arg)
        {
            // non-evanescent values are always legal
            return (arg as IEvanescent)?.IsLegal == false ? ctx.FALSE : ctx.TRUE;
        }

        /// <summary>
        /// Creates a form containing the specified elements.
        /// </summary>
        /// <param name="ctx"></param>
        /// <param name="args">The elements of the new form.</param>
        /// <returns>The new form.</returns>
        [Subr]
        public static ZilObject FORM(Context ctx, ZilObject[] args)
        {
            return new ZilForm(args) { SourceLine = ctx.TopFrame.SourceLine };
        }

        /// <summary>
        /// Creates a list containing the specified elements.
        /// </summary>
        /// <param name="ctx"></param>
        /// <param name="args">The elements of the new list.</param>
        /// <returns>The new list.</returns>
        [Subr]
        public static ZilObject LIST(Context ctx, ZilObject[] args)
        {
            return new ZilList(args);
        }

        /// <summary>
        /// Creates a vector containing the specified elements.
        /// </summary>
        /// <param name="ctx"></param>
        /// <param name="args">The elements of the new vector.</param>
        /// <returns>The new vector.</returns>
        [Subr]
        [Subr("TUPLE")]
        public static ZilObject VECTOR(Context ctx, ZilObject[] args)
        {
            return new ZilVector(args);
        }

        /// <summary>
        /// Creates a list with the specified number of elements, optionally initializing them by evaluating an expression.
        /// </summary>
        /// <param name="ctx"></param>
        /// <param name="count">The number of desired elements.</param>
        /// <param name="init">The expression to evaluate repeatedly to initialize the list. If omitted, the list will be filled with FALSE.</param>
        /// <returns>The new list.</returns>
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

        /// <summary>
        /// Creates a vector with the specified number of elements, optionally initializing them by evaluating an expression.
        /// </summary>
        /// <param name="ctx"></param>
        /// <param name="count">The number of desired elements.</param>
        /// <param name="init">The expression to evaluate repeatedly to initialize the vector. If omitted, the vector will be filled with FALSE.</param>
        /// <returns>The new vector.</returns>
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

        /// <summary>
        /// Converts an integer to a BYTE.
        /// </summary>
        /// <param name="ctx"></param>
        /// <param name="arg">The integer to convert.</param>
        /// <returns>A BYTE value.</returns>
        [Subr]
        public static ZilObject BYTE(Context ctx, ZilObject arg)
        {
            return ctx.ChangeType(arg, ctx.GetStdAtom(StdAtom.BYTE));
        }

        /// <summary>
        /// Creates a list by prepending an element onto a tail list.
        /// </summary>
        /// <param name="ctx"></param>
        /// <param name="first">The first element of the new list.</param>
        /// <param name="rest">The tail of the new list, or false to create a 1-element list.</param>
        /// <returns>The new list.</returns>
        [Subr]
        public static ZilObject CONS(Context ctx, ZilObject first, ZilList rest)
        {
            return new ZilList(
                first,
                rest is ZilList restList ? restList : new ZilList(rest));
        }

        /// <summary>
        /// Creates a FUNCTION object.
        /// </summary>
        /// <param name="ctx"></param>
        /// <param name="activationAtom">An optional atom which will be assigned an activation when the macro is called.</param>
        /// <param name="argList">A list of arguments for the macro.</param>
        /// <param name="decl">An optional DECL specifying the types of the macro's arguments and return value.</param>
        /// <param name="body">A sequence of expressions forming the macro body.</param>
        /// <returns>The new FUNCTION object.</returns>
        [FSubr]
        public static ZilObject FUNCTION(Context ctx, [Optional] ZilAtom? activationAtom,
             ZilList argList, [Optional] ZilDecl? decl, [Required] ZilObject[] body)
        {
            return new ZilFunction("FUNCTION", null, activationAtom, argList, decl, body);
        }

        /// <summary>
        /// Concatenates its arguments into a string.
        /// </summary>
        /// <param name="ctx"></param>
        /// <param name="args">A sequence of strings and/or characters.</param>
        /// <returns>The concatenated string.</returns>
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

        /// <summary>
        /// Creates a string with the specified number of characters, optionally initializing them by evaluating an expression.
        /// </summary>
        /// <param name="ctx"></param>
        /// <param name="count">The number of desired characters.</param>
        /// <param name="init">The expression to evaluate repeatedly to initialize the string. If omitted, the string will be filled with ASCII 0.</param>
        /// <returns>The new string.</returns>
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

        /// <summary>
        /// Converts between a CHARACTER and its ASCII integer code.
        /// </summary>
        /// <param name="ctx"></param>
        /// <param name="arg">A character or an integer.</param>
        /// <returns>The corresponding integer or character.</returns>
        [Subr]
        public static ZilObject ASCII(Context ctx, [Decl("<OR CHARACTER FIX>")] ZilObject arg)
        {
            if (arg is ZilChar ch)
                return new ZilFix(ch.Char);

            return new ZilChar((char)((ZilFix)arg).Value);
        }

    }
}
