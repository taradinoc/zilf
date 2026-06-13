/* Copyright 2010-2025 Tara McGrew
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
using System.Collections.Immutable;
using System.Globalization;
using System.Linq;
using System.Text;

namespace ZilfSourceGenerators
{
    /// <summary>
    /// Source generator for ZBuiltin (compiler built-in) argument parsers.
    /// Handles all complexity levels for compiler built-in routines.
    /// </summary>
    [Generator]
    public partial class ZBuiltinParserGenerator : IIncrementalGenerator
    {
        public void Initialize(IncrementalGeneratorInitializationContext context)
        {
            // Find methods with Builtin attributes
            var builtinMethods = context.SyntaxProvider
                .ForAttributeWithMetadataName("Zilf.Compiler.Builtins.BuiltinAttribute",
                    predicate: static (node, _) => node is MethodDeclarationSyntax,
                    transform: static (context, _) => GetBuiltinMethodInfo(context))
                .Where(method => method != null);

            // Generate parsers for Builtins
            var compilationAndBuiltins = context.CompilationProvider.Combine(builtinMethods.Collect());
            context.RegisterSourceOutput(compilationAndBuiltins, static (spc, source) =>
            {
                var nonNullMethods = source.Right.Where(m => m != null).ToImmutableArray();
                GenerateBuiltinParsers(spc, nonNullMethods!);
            });
        }

        private static BuiltinMethodInfo? GetBuiltinMethodInfo(GeneratorAttributeSyntaxContext context)
        {
            var method = (MethodDeclarationSyntax)context.TargetNode;
            // var semanticModel = context.SemanticModel;

            // var methodSymbol = semanticModel.GetDeclaredSymbol(method);
            if (context.TargetSymbol is not IMethodSymbol methodSymbol) return null;

            // Extract Builtin attribute information
            var builtinAttrs = GetBuiltinAttributes(context.Attributes);
            if (builtinAttrs.Length == 0) return null;

            // Cache parameter classification (data vs argument) once per method.
            var argsBuilder = ImmutableArray.CreateBuilder<IParameterSymbol>();
            var dataBuilder = ImmutableArray.CreateBuilder<IParameterSymbol>();
            if (methodSymbol.Parameters.Length > 1)
            {
                for (int i = 1; i < methodSymbol.Parameters.Length; i++)
                {
                    var p = methodSymbol.Parameters[i];
                    bool isData = false;
                    foreach (var a in p.GetAttributes())
                    {
                        if (a.AttributeClass?.Name == "DataAttribute")
                        {
                            isData = true;
                            break;
                        }
                    }

                    if (isData)
                        dataBuilder.Add(p);
                    else
                        argsBuilder.Add(p);
                }
            }

            return new BuiltinMethodInfo
            {
                Method = method,
                MethodSymbol = methodSymbol,
                AttributeInfos = builtinAttrs,
                ArgumentParameters = argsBuilder.ToImmutable(),
                DataParameters = dataBuilder.ToImmutable(),
            };
        }

        private static BuiltinAttributeInfo[] GetBuiltinAttributes(ImmutableArray<AttributeData> attributes)
        {
            var builtins = new List<BuiltinAttributeInfo>();

            foreach (var attr in attributes)
            {
                // Extract all constructor arguments as names (e.g., "ADD", "+")
                var names = new List<string>();
                foreach (var arg in attr.ConstructorArguments)
                {
                    if (arg.Kind == TypedConstantKind.Array)
                    {
                        // Handle array of strings (shouldn't happen for builtin names but be safe)
                        foreach (var item in arg.Values)
                        {
                            if (item.Value is string str)
                                names.Add(str);
                        }
                    }
                    else if (arg.Value is string name)
                    {
                        names.Add(name);
                    }
                }

                foreach (var name in names)
                {
                    if (name == null) continue;

                    // Create a separate BuiltinAttributeInfo for each name
                    var info = new BuiltinAttributeInfo { Name = name };

                    foreach (var namedArg in attr.NamedArguments)
                    {
                        switch (namedArg.Key)
                        {
                            case "Data":
                                info.Data = namedArg.Value.Value?.ToString();
                                break;
                            case "MinVersion":
                                if (namedArg.Value.Value is int minVer)
                                    info.MinVersion = minVer;
                                break;
                            case "MaxVersion":
                                if (namedArg.Value.Value is int maxVer)
                                    info.MaxVersion = maxVer;
                                break;
                            case "HasSideEffect":
                                if (namedArg.Value.Value is bool hasSideEffect)
                                    info.HasSideEffect = hasSideEffect;
                                break;
                            case "Priority":
                                if (namedArg.Value.Value is int priority)
                                    info.Priority = priority;
                                break;
                            case "Summary":
                                if (namedArg.Value.Value is string summary)
                                    info.Summary = summary;
                                break;
                            case "Platform":
                                info.Platform = GetPlatformSetting(namedArg.Value);
                                break;
                        }
                    }

                    builtins.Add(info);
                }
            }

            return [.. builtins];
        }

        private static BuiltinPlatformSetting GetPlatformSetting(TypedConstant platformValue)
        {
            if (platformValue.Value == null)
            {
                return BuiltinPlatformSetting.Any;
            }

            try
            {
                var numericValue = Convert.ToInt32(platformValue.Value);
                return numericValue switch
                {
                    1 => BuiltinPlatformSetting.ZMachine,
                    2 => BuiltinPlatformSetting.Glulx,
                    3 => BuiltinPlatformSetting.Cornerstone,
                    _ => BuiltinPlatformSetting.Any,
                };
            }
            catch
            {
                return BuiltinPlatformSetting.Any;
            }
        }

        private static List<OverloadGroup> GroupOverloadsByBuiltinAndCallType(ImmutableArray<BuiltinMethodInfo> methods)
        {
            var groups = new List<OverloadGroup>();
            var groupsByKey = new Dictionary<string, OverloadGroup>();

            foreach (var method in methods.Where(m => m.AttributeInfos.Any(ai => !string.IsNullOrWhiteSpace(ai.Name))))
            {
                foreach (var attr in method.AttributeInfos.Where(ai => !string.IsNullOrWhiteSpace(ai.Name)))
                {
                    var callType = GetCallTypeForBuiltinParser(method.MethodSymbol);
                    var key = $"{attr.Name}_{callType}";

                    if (!groupsByKey.TryGetValue(key, out var group))
                    {
                        group = new OverloadGroup
                        {
                            BuiltinName = attr.Name,
                            CallType = callType,
                            Overloads = []
                        };
                        groupsByKey[key] = group;
                        groups.Add(group);
                    }

                    group.Overloads.Add(new OverloadInfo
                    {
                        Method = method,
                        Attribute = attr
                    });
                }
            }

            return groups;
        }

        private static string GetParameterTypeName(ITypeSymbol type)
        {
            return type.ToDisplayString();
        }

        private static string GetReturnTypeName(ITypeSymbol type)
        {
            if (type.SpecialType == SpecialType.System_Void)
                return "void";
            return type.ToDisplayString();
        }

        private static string GetDefaultValueString(IParameterSymbol parameter)
        {
            if (parameter.HasExplicitDefaultValue)
            {
                if (parameter.ExplicitDefaultValue == null)
                    return "null";
                if (parameter.ExplicitDefaultValue is string str)
                    return $"\"{str}\"";
                return parameter.ExplicitDefaultValue.ToString() ?? "null";
            }
            return "null";
        }

        private static string SanitizeName(string name)
        {
            // Use ZILF's naming convention as found in StdAtom.cs and Subrs.*.cs
            return name.Replace("==", "Eeq")
                      .Replace("=", "Eq")
                      .Replace("+", "Plus")
                      .Replace("*", "Times")
                      .Replace("/", "Div")
                      .Replace("?", "_P")
                      .Replace("-", "_")
                      .Replace("<", "_lt")
                      .Replace(">", "_gt")
                      .Replace("!", "_not")
                      .Replace("&", "_and")
                      .Replace("|", "_or")
                      .Replace("%", "_mod")
                      .Replace("^", "_xor")
                      .Replace("#", "_hash")
                      .Replace("@", "_at")
                      .Replace("$", "_dollar")
                      .Replace("~", "_tilde")
                      .Replace("`", "_tick")
                      .Replace(".", "_dot")
                      .Replace(",", "_comma")
                      .Replace(":", "_colon")
                      .Replace(";", "_semi")
                      .Replace("'", "_quote")
                      .Replace("\"", "_dquote")
                      .Replace("(", "_lparen")
                      .Replace(")", "_rparen")
                      .Replace("[", "_lbracket")
                      .Replace("]", "_rbracket")
                      .Replace("{", "_lbrace")
                      .Replace("}", "_rbrace")
                      .Replace(" ", "_space");
        }

        private static string GetDataValue(BuiltinAttributeInfo attr)
        {
            // For now, return a placeholder - this should be extracted from the Data property
            return attr.Data?.ToString() ?? "null";
        }

        private static string GetEnumValueName(ITypeSymbol enumType, object? value)
        {
            if (enumType.TypeKind != TypeKind.Enum || enumType is not INamedTypeSymbol namedType || value == null)
                return value?.ToString() ?? "null";

            // Convert value to the underlying integer type for comparison
            var intValue = Convert.ToInt32(value);

            // Look through the enum fields to find a match
            foreach (var member in namedType.GetMembers().OfType<IFieldSymbol>())
            {
                if (member.HasConstantValue && member.ConstantValue != null)
                {
                    var memberValue = Convert.ToInt32(member.ConstantValue);
                    if (memberValue == intValue)
                    {
                        // Return the fully qualified enum value name
                        return $"{enumType.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat)}.{member.Name}";
                    }
                }
            }

            // If no exact match found, fall back to integer cast (shouldn't happen with valid enum values)
            return $"({enumType.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat)}){value}";
        }

        private static void GenerateBuiltinParsers(SourceProductionContext context, ImmutableArray<BuiltinMethodInfo> methods)
        {
            if (methods.Length == 0)
                return;

            var sb = new IndentedStringBuilder();
            sb.AppendLine("/* This file is part of ZILF. Generated code - do not edit. */");
            sb.AppendLine();
            sb.AppendLine("#nullable disable");
            sb.AppendLine();
            sb.AppendLine("using System;");
            sb.AppendLine("using System.Collections.Generic;");
            sb.AppendLine("using System.Linq;");
            sb.AppendLine("using Zilf.Compiler.Builtins;");
            sb.AppendLine("using Zilf.Compiler;");
            sb.AppendLine("using Zilf.Emit;");
            sb.AppendLine("using Zilf.Interpreter.Values;");
            sb.AppendLine("using Zilf.Language;");
            sb.AppendLine("using Zilf.Diagnostics;");
            sb.AppendLine();
            sb.AppendLine("namespace Zilf.Compiler.Builtins");
            sb.AppendLine("{");
            sb.Indent();
            sb.AppendLine("internal static partial class GeneratedBuiltinParsers");
            sb.AppendLine("{");
            sb.Indent();

            // Group overloads by builtin name and call type for sophisticated dispatch
            var overloadGroups = GroupOverloadsByBuiltinAndCallType(methods);
            var generatedParsers = new HashSet<string>();

            foreach (var group in overloadGroups)
            {
                GenerateBuiltinParser(sb, group, generatedParsers);
            }

            // Generate separate typed dictionaries for each call type and receive C# metadata source
            var generatedMetadataCSharp = GenerateTypedParserDictionaries(sb, overloadGroups);

            // Add helper methods for variable resolution
            sb.AppendLine();
            sb.AppendLine("private static Zilf.Compiler.VariableRef? GetVariable(Zilf.Compiler.Compilation cc, ZilObject expr, Zilf.Compiler.Builtins.VariableScopeQuirks quirks = Zilf.Compiler.Builtins.VariableScopeQuirks.None)");
            sb.AppendLine("{");
            sb.Indent();
            sb.AppendLine("// Replicate ParameterTypeHandler.GetVariable: resolve bare atoms or <GVAL>/<LVAL> forms");
            sb.AppendLine("if (expr is not ZilAtom atom &&");
            sb.Indent();
            sb.AppendLine("((quirks & Zilf.Compiler.Builtins.VariableScopeQuirks.Global) == 0 || !expr.IsGVAL(out atom!)) &&");
            sb.AppendLine("((quirks & Zilf.Compiler.Builtins.VariableScopeQuirks.Local) == 0 || !expr.IsLVAL(out atom!)))");
            sb.Unindent();
            sb.AppendLine("{");
            sb.Indent();
            sb.AppendLine("return null;");
            sb.Unindent();
            sb.AppendLine("}");
            sb.AppendLine();
            sb.AppendLine("if (quirks == Zilf.Compiler.Builtins.VariableScopeQuirks.Global)");
            sb.AppendLine("{");
            sb.Indent();
            sb.AppendLine("// prefer global over local");
            sb.AppendLine("if (cc.Globals.TryGetValue(atom, out var gb))");
            sb.Indent();
            sb.AppendLine("return new Zilf.Compiler.VariableRef(gb);");
            sb.Unindent();
            sb.AppendLine("if (cc.Locals.TryGetValue(atom, out var lbr))");
            sb.Indent();
            sb.AppendLine("return new Zilf.Compiler.VariableRef(lbr.LocalBuilder);");
            sb.Unindent();
            sb.Unindent();
            sb.AppendLine("}");
            sb.AppendLine("else");
            sb.AppendLine("{");
            sb.Indent();
            sb.AppendLine("if (cc.Locals.TryGetValue(atom, out var lbr))");
            sb.Indent();
            sb.AppendLine("return new Zilf.Compiler.VariableRef(lbr.LocalBuilder);");
            sb.Unindent();
            sb.AppendLine("if (cc.Globals.TryGetValue(atom, out var gb))");
            sb.Indent();
            sb.AppendLine("return new Zilf.Compiler.VariableRef(gb);");
            sb.Unindent();
            sb.Unindent();
            sb.AppendLine("}");
            sb.AppendLine();
            sb.AppendLine("if (cc.SoftGlobals.TryGetValue(atom, out var sg))");
            sb.Indent();
            sb.AppendLine("return new Zilf.Compiler.VariableRef(sg);");
            sb.Unindent();
            sb.AppendLine();
            sb.AppendLine("return null;");
            sb.Unindent();
            sb.AppendLine("}");
            sb.AppendLine();

            // Generate side effects method
            GenerateHasSideEffectsMethod(sb, overloadGroups);

            sb.Unindent();
            sb.AppendLine("}");
            sb.Unindent();
            sb.AppendLine("}");

            context.AddSource("GeneratedBuiltinParsers.g.cs", SourceText.From(sb.ToString(), Encoding.UTF8));
            if (!string.IsNullOrEmpty(generatedMetadataCSharp))
            {
                // Add the strongly-typed C# metadata source produced by the generator.
                context.AddSource("GeneratedBuiltinMetadata.g.cs", SourceText.From(generatedMetadataCSharp, Encoding.UTF8));
            }
        }

        private static void GenerateBuiltinParser(IndentedStringBuilder sb, OverloadGroup group, HashSet<string> generatedParsers)
        {
            var cleanName = group.BuiltinName?.Trim() ?? "";
            var sanitizedName = SanitizeName(cleanName);
            var parserName = $"Generated_{sanitizedName}_{group.CallType}_Parser";

            // Skip if already generated or if name is invalid
            if (generatedParsers.Contains(parserName) ||
                string.IsNullOrWhiteSpace(cleanName) ||
                cleanName.Length == 0 ||
                cleanName == "_" ||
                sanitizedName.Length == 0)
            {
                return;
            }

            generatedParsers.Add(parserName);

            // Determine the return type based on the call type
            var returnType = GetReturnTypeForCallType(group.CallType);

            // Builtin parsers have different signatures based on the call type
            sb.AppendLine($"internal static {returnType} {parserName}({group.CallType} c, ZilObject[] args)");
            sb.AppendLine("{");
            sb.Indent();

            // Generate method body with multi-overload dispatch logic
            GenerateOverloadDispatchBody(sb, group);

            sb.Unindent();
            sb.AppendLine("}");
            sb.AppendLine();
        }

        private static string GetReturnTypeForCallType(string callType)
        {
            return callType switch
            {
                "VoidCall" => "void",
                "PredCall" => "void",
                "ValueCall" => "IOperand",
                "ValuePredCall" => "IOperand",
                _ => "IOperand" // default
            };
        }

        private static string GetReturnTypeForBuiltinParser(IMethodSymbol method)
        {
            var returnType = method.ReturnType;

            if (returnType.SpecialType == SpecialType.System_Void)
                return "void";

            // For most builtins, return IOperand
            return "IOperand";
        }

        private static string GetCallTypeForBuiltinParser(IMethodSymbol method)
        {
            // Check the first parameter type to determine the call type
            if (method.Parameters.Length > 0)
            {
                var firstParamType = method.Parameters[0].Type.Name;
                switch (firstParamType)
                {
                    case "ValueCall":
                        return "ValueCall";
                    case "VoidCall":
                        return "VoidCall";
                    case "PredCall":
                        return "PredCall";
                    case "ValuePredCall":
                        return "ValuePredCall";
                }
            }

            // Default to ValueCall
            return "ValueCall";
        }
    }
}

