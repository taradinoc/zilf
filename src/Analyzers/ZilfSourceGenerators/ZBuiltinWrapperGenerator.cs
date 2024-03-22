/* Copyright 2010-2024 Tara McGrew
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

using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Text;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading;

namespace ZilfSourceGenerators
{
    [Generator]
    public class ZBuiltinWrapperGenerator : IIncrementalGenerator
    {
        /// <summary>
        /// Records information about a method that should be exposed to user code
        /// as a builtin Z-code operation under one or more names.
        /// </summary>
        /// <param name="CallType">The type of call the method should be exposed as.</param>
        /// <param name="HasData">Whether the method has a Data parameter.</param>
        /// <param name="MethodName">The name of the method.</param>
        /// <param name="Params">An array of <see cref="Param"/>
        /// describing the method's parameters and their attributes.</param>
        /// <param name="ReturnValue">A <see cref="ReturnValue"/> describing the method's
        /// return value and its attributes.</param>
        /// <param name="Targets">An array of <see cref="Exposure"/> detailing
        /// how the method should be exposed.</param>
        record BuiltinMethodInfo(
            CallType CallType,
            string MethodName,
            EquatableArray<Param> Params,
            ReturnValue ReturnValue,
            EquatableArray<Exposure> Targets)
        {
            public bool HasData => Params.Length > 0 && Params[0].Flags.HasFlag(ParamFlags.IsData);
        }

        enum CallType
        {
            ValueCall,
            VoidCall,
            PredCall,
            ValuePredCall
        }

        [Flags]
        enum ParamFlags
        {
            None = 0,
            IsData = 1,
            IsOptional = 2,
            IsVarArgs = 4,
            IsTable = 8,                    // TableAttribute
            IsObject = 16,                  // ObjectAttribute
            IsVariable = 32,                // VariableAttribute
            IsVariableQuirkLocal = 64,      // VariableScopeQuirks.Local
            IsVariableQuirkGlobal = 128,    // VariableScopeQuirks.Global
        }

        record Param(string Name, string FormalType, ParamFlags Flags);

        [Flags]
        enum ReturnFlags
        {
            None = 0,
            IsTable = 1,                    // TableAttribute
            IsObject = 2,                   // ObjectAttribute
        }

        record ReturnValue(string FormalType, ReturnFlags Flags);

        /// <summary>
        /// Records information about a particular exposure of a method as a Z-code builtin.
        /// </summary>
        record Exposure(string Name, int MinVersion, int MaxVersion, bool HasSideEffect, int Priority, object? Data);

        /// <summary>
        /// Records information about a generated method.
        /// </summary>
        /// <param name="Namespace">The namespace where the method will be emitted.</param>
        /// <param name="ClassName">The class where the method will be emitted.</param>
        /// <param name="MethodName">The name of the method.</param>
        /// <param name="Lines">The method body.</param>
        record GeneratedMethod(string Namespace, string ClassName, string MethodName, EquatableArray<string> Lines);

        public void Initialize(IncrementalGeneratorInitializationContext context)
        {
            // extract BuiltinMethodInfo from method declarations with a BuiltinAttribute
            IncrementalValuesProvider<BuiltinMethodInfo> builtinInfos = context.SyntaxProvider.ForAttributeWithMetadataName(
                "Zilf.Compiler.Builtins.BuiltinAttribute",
                predicate: IsBuiltinMethodSyntax,
                transform: ExtractBuiltinMethodInfo)
                .Where(static b => b is not null)!;

            // generate one argument decoder method for each BuiltinMethodInfo
            var argDecoders = builtinInfos.Select(EmitArgDecoder);

            // generate one dispatch method for each distinct exposed name
            var dispatchers = builtinInfos
                .SelectMany((bi, _) => bi.Targets.Select(t => (target: t, method: bi.MethodName, callType: bi.CallType)))
                .Collect()
                .SelectMany((rows, _) => from row in rows
                                         group row by (name: row.target.Name, row.callType) into g
                                         select (
                                            g.Key.name,
                                            g.Key.callType,
                                            targets: g.OrderBy(static r => r.target.Priority)
                                                .ThenBy(static r => r.method)
                                                .ThenByDescending(static r => r.target.MaxVersion)
                                                .Select(static r => r.target)
                                                .ToEquatableArray()
                                         ))
                .Select((r, ct) => EmitDispatchMethod(r.name, r.callType, r.targets, ct));

            // produce a source file containing the argument decoders, the dispatchers, and
            // a static method to register all the dispatchers and some metadata about each one
            context.RegisterSourceOutput(
                argDecoders.Collect().Combine(dispatchers.Collect()),
                (spc, inputs) =>
                {
                    var (argDecoders, dispatchers) = inputs;
                    var byNamespace = argDecoders.Concat(dispatchers).GroupBy(static m => m.Namespace);
                    var sb = new StringBuilder();

                    foreach (var g in byNamespace)
                    {
                        foreach (var m in g)
                        {
                            var code = $$"""
                            namespace {{g.Key}}
                            {
                                static partial class {{m.ClassName}}
                                {
                                    {{string.Join("\n        ", m.Lines)}}
                                }
                            }

                            """;
                            sb.Append(code);
                        }
                    }

                    var sourceText = SourceText.From(sb.ToString(), Encoding.UTF8);
                    spc.AddSource("ZBuiltins.g.cs", sourceText);
            });
        }

        private GeneratedMethod EmitArgDecoder(BuiltinMethodInfo info, CancellationToken token)
        {
            var decoderMethodName = $"Decode_{info.MethodName}";
            var lines = new IndentedWriter();

            var returnType = info.ReturnValue.FormalType;
            var callType = info.CallType;
            var dataParam = info.HasData ? $"{info.Params[0].FormalType} {info.Params[0].Name}, " : "";

            using (lines.Block($"private static {returnType} {decoderMethodName}({callType} c, {dataParam}Span<ZilObject> args)"))
            {
                //XXX
                lines.WriteLine($"// Decode parameters for {info.MethodName}");

                foreach (var p in info.Params)
                {
                    if (p.Flags.HasFlag(ParamFlags.IsData))
                    {
                        continue;
                    }

                    lines.WriteLine($"{p.FormalType} {p.Name};");

                    token.ThrowIfCancellationRequested();
                }

                lines.WriteLine("throw new NotImplementedException();");
            }

            return new GeneratedMethod("Zilf.Compiler.Builtins.Generated", "ZBuiltinDecoders", decoderMethodName, lines.GetLines());
        }

        private GeneratedMethod EmitDispatchMethod(string name, CallType callType, EquatableArray<Exposure> targets, CancellationToken token)
        {
            var dispatchMethodName = $"Dispatch_{SanitizeName(name)}";
            var lines = new IndentedWriter();

            var returnType = callType == CallType.ValueCall ? "IOperand" : "void";

            using (lines.Block($"private static {returnType} {dispatchMethodName}({callType} c, Span<ZilObject> args)"))
            {
                //XXX
                lines.WriteLine($"// Dispatch {name}");

                foreach (var target in targets)
                {
                    lines.WriteLine($"// {target}");

                    token.ThrowIfCancellationRequested();
                }

                lines.WriteLine("throw new NotImplementedException();");
            }

            return new GeneratedMethod("Zilf.Compiler.Builtins.Generated", "ZBuiltinDecoders", dispatchMethodName, lines.GetLines());
        }

        private static string SanitizeName(string name)
        {
            return name switch
            {
                "+" => "Plus",
                "-" => "Minus",
                "*" => "Times",
                "/" => "Divide",
                _ => name
                    .Replace("==", "Eeq")
                    .Replace("=", "Eq")
                    .Replace("?", "_P")
                    .Replace('-', '_'),
            };
        }

        private static bool IsBuiltinMethodSyntax(SyntaxNode syntaxNode, CancellationToken cancellationToken)
        {
            var methodDeclaration = syntaxNode.FirstAncestorOrSelf<MethodDeclarationSyntax>();
            if (methodDeclaration is null)
                return false;

            var classDeclaration = methodDeclaration.FirstAncestorOrSelf<ClassDeclarationSyntax>();
            if (classDeclaration is null)
                return false;

            return
                classDeclaration.Modifiers.Any(m => m.IsKind(SyntaxKind.StaticKeyword)) &&
                classDeclaration.Modifiers.Any(m => m.IsKind(SyntaxKind.PartialKeyword));
        }

        private static BuiltinMethodInfo? ExtractBuiltinMethodInfo(GeneratorAttributeSyntaxContext gsc, CancellationToken cancellationToken)
        {
            if (gsc.TargetSymbol is not IMethodSymbol methodSymbol)
                return null;

            var methodName = $"{methodSymbol.ContainingType?.ToDisplayString(SymbolDisplayFormat.MinimallyQualifiedFormat)}.{methodSymbol.Name}";
            var targets = new List<Exposure>();
            var parameters = new List<Param>();
            ReturnValue returnValue;

            // populate targets from BuiltinAttribute attributes
            foreach (var attr in gsc.Attributes)
            {
                // extract the attribute properties
                int minVersion = 1, maxVersion = 6, priority = 1;
                bool hasSideEffect = false;
                object? data = null;

                foreach (var namedArg in attr.NamedArguments)
                {
                    switch (namedArg.Key)
                    {
                        case "MinVersion":
                            minVersion = namedArg.Value.Value as int? ?? 999;
                            break;
                        case "MaxVersion":
                            maxVersion = namedArg.Value.Value as int? ?? -999;
                            break;
                        case "Priority":
                            priority = namedArg.Value.Value as int? ?? -999;
                            break;
                        case "HasSideEffect":
                            hasSideEffect = namedArg.Value.Value as bool? ?? false;
                            break;
                        case "Data":
                            data = namedArg.Value.Value;
                            break;
                    }
                }

                // the constructor arguments are the builtin's name, followed by any aliases
                // we don't distinguish between the name and the aliases, so just produce one Exposure for each argument
                foreach (var arg in attr.ConstructorArguments)
                {
                    if (arg is { Kind: TypedConstantKind.Primitive, Type.SpecialType: SpecialType.System_String, Value: string name })
                    {
                        targets.Add(new Exposure(name, minVersion, maxVersion, hasSideEffect, priority, data));
                    }
                }
            }

            // the first parameter is the call type: ValueCall, VoidCall, PredCall, or ValuePredCall
            CallType callType;

            if (methodSymbol.Parameters.Length == 0)
            {
                // error
                return null;
            }

            switch (methodSymbol.Parameters[0].Type.Name)
            {
                case "ValueCall":
                    callType = CallType.ValueCall;
                    break;
                case "VoidCall":
                    callType = CallType.VoidCall;
                    break;
                case "PredCall":
                    callType = CallType.PredCall;
                    break;
                case "ValuePredCall":
                    callType = CallType.ValuePredCall;
                    break;
                default:
                    // error
                    return null;
            }

            // populate parameters from remaining method parameters
            for (int i = 1; i < methodSymbol.Parameters.Length; i++)
            {
                var param = methodSymbol.Parameters[i];
                var flags = ParamFlags.None;

                // check for special parameter attributes
                foreach (var attr in param.GetAttributes())
                {
                    switch (attr.AttributeClass?.Name)
                    {
                        case "OptionalAttribute":
                            flags |= ParamFlags.IsOptional;
                            break;
                        case "VarArgsAttribute":
                            flags |= ParamFlags.IsVarArgs;
                            break;
                        case "TableAttribute":
                            flags |= ParamFlags.IsTable;
                            break;
                        case "ObjectAttribute":
                            flags |= ParamFlags.IsObject;
                            break;
                        case "VariableAttribute":
                            flags |= ParamFlags.IsVariable;
                            // check for a VariableScopeQuirks named parameter
                            if (attr.NamedArguments.FirstOrDefault(attr => attr.Key == "VariableScopeQuirks") is
                                { Value.Kind: TypedConstantKind.Enum, Value.Value: int quirks })
                            {
                                // NOTE: these have to stay in sync with the enum values in VariableScopeQuirks
                                if ((quirks & 1) != 0)
                                {
                                    flags |= ParamFlags.IsVariableQuirkLocal;
                                }
                                if ((quirks & 2) != 0)
                                {
                                    flags |= ParamFlags.IsVariableQuirkGlobal;
                                }
                            }
                            break;
                    }
                }

                parameters.Add(new Param(param.Name, param.Type.Name, flags));
            }

            // populate returnValue from method return type
            var returnFlags = ReturnFlags.None;

            foreach (var attr in methodSymbol.ReturnType.GetAttributes())
            {
                switch (attr.AttributeClass?.Name)
                {
                    case "TableAttribute":
                        returnFlags |= ReturnFlags.IsTable;
                        break;
                    case "ObjectAttribute":
                        returnFlags |= ReturnFlags.IsObject;
                        break;
                }
            }

            returnValue = new ReturnValue(methodSymbol.ReturnType.Name, returnFlags);

            return new BuiltinMethodInfo(callType, methodName, parameters.ToEquatableArray(), returnValue, targets.ToEquatableArray());
        }
    }
}
