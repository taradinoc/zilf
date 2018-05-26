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
using System.Linq;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Text;
using JetBrains.Annotations;
using Zilf.Common;
using Zilf.Interpreter;
using Zilf.Interpreter.Values;

namespace Zilf.Language.Signatures
{
    partial class SubrSignature
    {
        static readonly object[] EmptyObjectArray = new object[0];

        [NotNull]
        public static ISignature FromMethodInfo([NotNull] MethodInfo methodInfo, bool isFSubr)
        {
            if (methodInfo == null)
                throw new ArgumentNullException(nameof(methodInfo));

            if (!typeof(ZilObject).IsAssignableFrom(methodInfo.ReturnType) &&
                !typeof(ZilResult).IsAssignableFrom(methodInfo.ReturnType))
                throw new ArgumentException("Method return type is not assignable to ZilObject or ZilResult");

            var parameters = methodInfo.GetParameters();

            if (parameters.Length < 1 || parameters[0].ParameterType != typeof(Context))
                throw new ArgumentException("First parameter type must be Context");

            var paramSigParts = methodInfo.GetParameters().Skip(1).Select(ConvertSubrParam);

            return new SubrSignature(paramSigParts.ToArray());
        }

        [NotNull]
        static SignaturePart ConvertSubrParam([NotNull] ParameterInfo pi)
        {
            var (isOptional, defaultValue) = CheckOptional(pi);
            return ConvertForSubr(
                pi.ParameterType,
                Hyphenate(pi.Name),
                pi.GetCustomAttributes(false),
                isOptional,
                defaultValue);
        }

        [NotNull]
        static SignaturePart ConvertForSubr(
            [NotNull] Type paramType,
            [NotNull] string name,
            // ReSharper disable once SuggestBaseTypeForParameter
            [NotNull] [ItemNotNull] object[] attrs,
            bool isOptional,
            // ReSharper disable once UnusedParameter.Local
            object defaultValue)
        {
            // [Either], [Required], and [Decl] go on the parameter
            var isRequired = attrs.OfType<RequiredAttribute>().Any();
            if (isRequired && isOptional)
            {
                throw new InvalidOperationException("A parameter can't be both required and optional");
            }

            var eitherAttr = attrs.OfType<EitherAttribute>().SingleOrDefault();
            var declAttr = attrs.OfType<DeclAttribute>().SingleOrDefault();

            // [ZilStructuredParam] and [ZilSequenceParam] go on the element type
            var (isArray, elementType) = CheckArray(paramType);
            var structAttr = elementType.GetCustomAttribute<ZilStructuredParamAttribute>(false);
            var seqAttr = elementType.GetCustomAttribute<ZilSequenceParamAttribute>(false);

            int attrCount =
                (eitherAttr != null ? 1 : 0) +
                (declAttr != null ? 1 : 0) +
                (structAttr != null ? 1 : 0) +
                (seqAttr != null ? 1 : 0);

            if (attrCount > 1)
            {
                throw new InvalidOperationException(
                    nameof(EitherAttribute) + ", " +
                    nameof(DeclAttribute) + ", " +
                    nameof(ZilStructuredParamAttribute) + ", or " +
                    nameof(ZilSequenceParamAttribute) + ": pick at most one");
            }

            SignaturePart elemPart = null;

            if (eitherAttr != null)
            {
                elemPart = ConvertEither(eitherAttr.Types, eitherAttr.DefaultParamDesc ?? name);
            }
            else if (declAttr != null)
            {
                elemPart = SignatureBuilder.MaybeConvertDecl(declAttr);
            }
            else if (structAttr != null)
            {
                elemPart = ConvertStruct(elementType, structAttr, name);
            }
            else if (seqAttr != null)
            {
                elemPart = ConvertSequence(elementType, name);
            }

            if (elemPart == null)
            {
                elemPart = SignatureBuilder.Identifier(name);

                if (elementType != typeof(ZilObject))
                {
                    elemPart = ConstrainByType(elemPart, elementType);
                }
            }

            if (isArray)
            {
                elemPart = SignatureBuilder.VarArgs(elemPart, isRequired);
            }

            if (isOptional)
            {
                //XXX use defaultValue somehow?
                elemPart = SignatureBuilder.Optional(elemPart);
            }

            return elemPart;
        }

        [NotNull]
        static SignaturePart ConvertEither([NotNull] [ItemNotNull] [InstantHandle] IEnumerable<Type> altTypes, [NotNull] string name)
        {
            var alts = from t in altTypes
                       select ConvertForSubr(t, name, EmptyObjectArray, false, null);
            return SignatureBuilder.Alternatives(alts, name);
        }

        [NotNull]
        static SignaturePart ConvertStruct([NotNull] Type structType, [NotNull] ZilStructuredParamAttribute attr, [NotNull] string name)
        {
            // TODO: cache the result
            var parts = ConvertFields(structType).ToList();

            switch (attr.TypeAtom)
            {
                case StdAtom.ADECL:
                    return SignatureBuilder.Adecl(parts[0], parts[1], name);

                case StdAtom.FORM:
                    return SignatureBuilder.Form(parts, name);

                case StdAtom.LIST:
                    return SignatureBuilder.List(parts, name);

                default:
                    throw new UnhandledCaseException(attr.TypeAtom.ToString());
            }
        }

        [NotNull]
        static SignaturePart ConvertSequence([NotNull] Type seqType, [NotNull] string name)
        {
            // TODO: cache the result?
            return SignatureBuilder.Sequence(ConvertFields(seqType), name);
        }

        [NotNull]
        [ItemNotNull]
        static IEnumerable<SignaturePart> ConvertFields([NotNull] Type structOrSeqType)
        {
            return structOrSeqType.GetFields()
                .OrderBy(f => Marshal.OffsetOf(structOrSeqType, f.Name).ToInt64())
                .Select(fi =>
                {
                    var (isOptional, defaultValue) = CheckOptional(fi);
                    return ConvertForSubr(
                        fi.FieldType,
                        Hyphenate(fi.Name),
                        fi.GetCustomAttributes(false),
                        isOptional,
                        defaultValue);
                });
        }

        static readonly IReadOnlyDictionary<Type, Constraint> StandardTypeConstraints = new Dictionary<Type, Constraint>
        {
            { typeof(IApplicable), Constraint.Applicable },
            { typeof(IStructure), Constraint.Structured },
            { typeof(int), Constraint.OfType(StdAtom.FIX) },
            { typeof(string), Constraint.OfType(StdAtom.STRING) },
            { typeof(char), Constraint.OfType(StdAtom.CHARACTER) },
            { typeof(bool), Constraint.Boolean }
        };

        [NotNull]
        static SignaturePart ConstrainByType([NotNull] SignaturePart part, [NotNull] Type elementType)
        {

            System.Diagnostics.Debug.Assert(!elementType.IsArray);

            // treat Nullable<Foo> as an optional Foo
            bool isOptional = false;
            var nullableUnderlyingType = Nullable.GetUnderlyingType(elementType);
            if (nullableUnderlyingType != null)
            {
                isOptional = true;
                elementType = nullableUnderlyingType;
            }

            // treat LocalEnvironment as an optional ZilEnvironment
            if (elementType == typeof(LocalEnvironment))
            {
                isOptional = true;
                elementType = typeof(ZilEnvironment);
            }

            // find an appropriate type or primtype constraint for the element type
            if (!StandardTypeConstraints.TryGetValue(elementType, out var constraint))
            {
                BuiltinPrimTypeAttribute primTypeAttr;
                if ((primTypeAttr = elementType.GetCustomAttribute<BuiltinPrimTypeAttribute>(false)) != null)
                {
                    constraint = Constraint.OfPrimType(primTypeAttr.PrimType);
                }
                else
                {
                    BuiltinTypeAttribute typeAttr;
                    if ((typeAttr = elementType.GetCustomAttribute<BuiltinTypeAttribute>(false)) != null)
                    {
                        constraint = Constraint.OfType(typeAttr.Name);
                    }
                    else
                    {
                        throw new UnhandledCaseException(elementType.Name);
                    }
                }
            }

            part = SignatureBuilder.Constrained(part, constraint);
            return isOptional ? SignatureBuilder.Optional(part) : part;
        }

        static (bool isOptional, object defaultValue) CheckOptional([NotNull] ParameterInfo pi)
        {
            var zilOptAttr = pi.GetCustomAttribute<ZilOptionalAttribute>(false);

            if (zilOptAttr == null)
                return (pi.IsOptional, pi.HasDefaultValue ? pi.DefaultValue : null);

            if (pi.IsOptional)
                throw new InvalidOperationException(
                    $"Expected {nameof(ZilOptionalAttribute)} or {nameof(pi.IsOptional)}, not both");

            return (true, zilOptAttr.Default);
        }

        static (bool isOptional, object defaultValue) CheckOptional([NotNull] MemberInfo fi)
        {
            var zilOptAttr = fi.GetCustomAttribute<ZilOptionalAttribute>(false);
            return (zilOptAttr != null, zilOptAttr?.Default);
        }

        static (bool isArray, Type elementOrNonArrayType) CheckArray([NotNull] Type maybeArrayType)
        {
            return maybeArrayType.IsArray
                ? (true, maybeArrayType.GetElementType())
                : (false, maybeArrayType);
        }

        [NotNull]
        static string Hyphenate([NotNull] string s)
        {
            var sb = new StringBuilder(s.Length);
            sb.Append(char.ToLowerInvariant(s[0]));

            for (int i = 1; i < s.Length; i++)
            {
                var c = s[i];
                if (char.IsUpper(c))
                {
                    sb.Append('-');
                    sb.Append(char.ToLowerInvariant(c));
                }
                else
                {
                    sb.Append(c);
                }
            }

            return sb.ToString();
        }
    }
}