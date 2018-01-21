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
using JetBrains.Annotations;
using Zilf.Compiler.Builtins;
using Zilf.Interpreter;

namespace Zilf.Language.Signatures
{
    class ZBuiltinSignature : ISignature
    {
        public IReadOnlyList<ISignaturePart> Parts { get; }
        public ISignaturePart ReturnPart { get; }
        public int MinArgs { get; }
        public int? MaxArgs { get; }
        public int MinVersion { get; }
        public int MaxVersion { get; }

        ZBuiltinSignature(int minArgs, int? maxArgs, int minVersion, int maxVersion,
            IReadOnlyList<ISignaturePart> parts, ISignaturePart returnPart)
        {
            MinArgs = minArgs;
            MaxArgs = maxArgs;
            MinVersion = minVersion;
            MaxVersion = maxVersion;
            Parts = parts;
            ReturnPart = returnPart;
        }

        [NotNull]
        public static ISignature FromBuiltinSpec([NotNull] BuiltinSpec spec)
        {
            var pis = spec.Method.GetParameters();

            var parts = pis
                .Skip(spec.Attr.Data == null ? 1 : 2)
                .Select(ConvertBuiltinParam)
                .ToArray();

            var returnPart = ConvertBuiltinParam(
                spec.Method.ReturnParameter ??
                throw new InvalidOperationException($"Missing {nameof(spec.Method.ReturnParameter)}"));

            return new ZBuiltinSignature(
                spec.MinArgs, spec.MaxArgs,
                spec.Attr.MinVersion, spec.Attr.MaxVersion,
                parts,
                returnPart);

            // helper
            SignaturePart ConvertBuiltinParam(ParameterInfo pi)
            {
                var type = pi.ParameterType;

                if (type == typeof(void))
                {
                    if (spec.CallType == typeof(VoidCall))
                    {
                        return LiteralPart.From("T");
                    }

                    var part = SignatureBuilder.Constrained(
                        SignatureBuilder.Identifier("$return"),
                        spec.CallType == typeof(PredCall) ? Constraint.Boolean : Constraint.AnyObject);

                    return ApplyParamAttributes(part, pi);
                }

                if (ParameterTypeHandler.Handlers.TryGetValue(type, out var handler))
                {
                    return ConvertWithHandler(handler, pi);
                }

                // ReSharper disable once PatternAlwaysOfType
                if (type.IsArray && type.GetElementType() is Type t &&
                    ParameterTypeHandler.Handlers.TryGetValue(t, out handler))
                {
                    var part = ConvertWithHandler(handler, pi);
                    return SignatureBuilder.VarArgs(part, false);
                }

                throw new InvalidOperationException("Unexpected builtin param type");
            }
        }

        [NotNull]
        static SignaturePart ConvertWithHandler([NotNull] ParameterTypeHandler handler,
            [NotNull] ParameterInfo pi)
        {
            return ApplyParamAttributes(handler.ToSignaturePart(pi), pi);
        }

        [NotNull]
        static SignaturePart ApplyParamAttributes([NotNull] SignaturePart part, [NotNull] ParameterInfo pi)
        {
            // TODO: also handle VariableAttribute?

            if (pi.IsDefined(typeof(TableAttribute), false))
            {
                part = SignatureBuilder.Constrained(part, Constraint.OfPrimType(PrimType.TABLE));
            }

            if (pi.IsDefined(typeof(ObjectAttribute), false))
            {
                part = SignatureBuilder.Constrained(part, Constraint.OfType(StdAtom.OBJECT));
            }

            if (pi.IsOptional)
            {
                part = SignatureBuilder.Optional(part);
            }

            return part;
        }
    }
}
