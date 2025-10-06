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
using System.Linq;
using System.Text;

namespace ZilfSourceGenerators
{
    /// <summary>
    /// Source generator for ZBuiltin (compiler built-in) argument parsers.
    /// Handles all complexity levels for compiler built-in routines.
    /// </summary>
    [Generator]
    public class ZBuiltinParserGenerator : IIncrementalGenerator
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

            return new BuiltinMethodInfo
            {
                Method = method,
                MethodSymbol = methodSymbol,
                AttributeInfos = builtinAttrs
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
                        }
                    }

                    builtins.Add(info);
                }
            }

            return [.. builtins];
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

        // TODO: include correct parameter names
        // TODO: don't add return parameter "T" to void calls
        private static string GenerateCapabilityChecker(OverloadGroup group)
        {
            // Create a lambda that checks if any overload supports the given version and argument count
            var conditions = new List<string>();

            foreach (var info in group.Overloads)
            {
                string? versionCondition;
                if (info.Attribute.MinVersion.HasValue && info.Attribute.MaxVersion.HasValue)
                {
                    versionCondition = $"(zversion >= {info.Attribute.MinVersion} && zversion <= {info.Attribute.MaxVersion})";
                }
                else if (info.Attribute.MinVersion.HasValue)
                {
                    versionCondition = $"zversion >= {info.Attribute.MinVersion}";
                }
                else if (info.Attribute.MaxVersion.HasValue)
                {
                    versionCondition = $"zversion <= {info.Attribute.MaxVersion}";
                }
                else
                {
                    versionCondition = "true"; // No version restriction
                }

                // Calculate argument count constraints using the same logic as argument validation
                var paramInfo = AnalyzeMethodParameters([.. info.Method.MethodSymbol.Parameters]);
                string? argCountCondition;
                if (paramInfo.HasParamsArray)
                {
                    // With params array, minimum is required count, no maximum
                    if (paramInfo.RequiredArgumentCount == 0)
                    {
                        argCountCondition = "true"; // Any number of arguments
                    }
                    else
                    {
                        argCountCondition = $"argCount >= {paramInfo.RequiredArgumentCount}";
                    }
                }
                else
                {
                    var minArgs = paramInfo.RequiredArgumentCount;
                    var maxArgs = paramInfo.RequiredArgumentCount + paramInfo.OptionalArgumentCount;

                    if (minArgs == maxArgs)
                    {
                        argCountCondition = $"argCount == {minArgs}";
                    }
                    else
                    {
                        argCountCondition = $"(argCount >= {minArgs} && argCount <= {maxArgs})";
                    }
                }

                // Combine version and argument conditions for this overload
                if (versionCondition == "true" && argCountCondition == "true")
                {
                    conditions.Add("true");
                }
                else if (versionCondition == "true")
                {
                    conditions.Add($"({argCountCondition})");
                }
                else if (argCountCondition == "true")
                {
                    conditions.Add($"({versionCondition})");
                }
                else
                {
                    conditions.Add($"({versionCondition} && {argCountCondition})");
                }
            }

            // Generate lambda that returns true if ANY overload matches
            if (conditions.Count == 0)
            {
                return "(zversion, argCount) => false";
            }
            else if (conditions.Count == 1)
            {
                return $"(zversion, argCount) => {conditions[0]}";
            }
            else
            {
                var combined = string.Join(" || ", conditions);
                return $"(zversion, argCount) => ({combined})";
            }
        }

        private static string GenerateTypedParserDictionaries(IndentedStringBuilder sb, List<OverloadGroup> overloadGroups)
        {
            // Group by call type
            var callTypeGroups = overloadGroups
                .Where(g => !string.IsNullOrWhiteSpace(g.BuiltinName?.Trim()) &&
                           !g.BuiltinName.Contains(' ') && !g.BuiltinName.Contains('\t'))
                .GroupBy(g => g.CallType)
                .ToList();

            sb.AppendLine();

            // Generate VoidCall dictionary with capability checking
            var voidCallGroup = callTypeGroups.FirstOrDefault(g => g.Key == "VoidCall");
            if (voidCallGroup != null)
            {
                sb.AppendLine("internal static readonly System.Collections.Generic.Dictionary<string, (System.Action<Zilf.Compiler.Builtins.VoidCall, Zilf.Interpreter.Values.ZilObject[]> Parser, System.Func<int, int, bool> SupportsCall)> VoidCallParsers = new() {");
                sb.Indent();
                foreach (var group in voidCallGroup)
                {
                    var cleanName = group.BuiltinName?.Trim() ?? "";
                    var sanitizedName = SanitizeName(cleanName);
                    var parserName = $"Generated_{sanitizedName}_{group.CallType}_Parser";
                    var capabilityChecker = GenerateCapabilityChecker(group);
                    sb.AppendLine($"{{ \"{cleanName}\", ({parserName}, {capabilityChecker}) }},");
                }
                sb.Unindent();
                sb.AppendLine("};");
                sb.AppendLine();
            }

            // Generate ValueCall dictionary with capability checking
            var valueCallGroup = callTypeGroups.FirstOrDefault(g => g.Key == "ValueCall");
            if (valueCallGroup != null)
            {
                sb.AppendLine("internal static readonly System.Collections.Generic.Dictionary<string, (System.Func<Zilf.Compiler.Builtins.ValueCall, Zilf.Interpreter.Values.ZilObject[], Zilf.Emit.IOperand> Parser, System.Func<int, int, bool> SupportsCall)> ValueCallParsers = new() {");
                sb.Indent();
                foreach (var group in valueCallGroup)
                {
                    var cleanName = group.BuiltinName?.Trim() ?? "";
                    var sanitizedName = SanitizeName(cleanName);
                    var parserName = $"Generated_{sanitizedName}_{group.CallType}_Parser";
                    var capabilityChecker = GenerateCapabilityChecker(group);
                    sb.AppendLine($"{{ \"{cleanName}\", ({parserName}, {capabilityChecker}) }},");
                }
                sb.Unindent();
                sb.AppendLine("};");
                sb.AppendLine();
            }

            // Generate PredCall dictionary with capability checking
            var predCallGroup = callTypeGroups.FirstOrDefault(g => g.Key == "PredCall");
            if (predCallGroup != null)
            {
                sb.AppendLine("internal static readonly System.Collections.Generic.Dictionary<string, (System.Action<Zilf.Compiler.Builtins.PredCall, Zilf.Interpreter.Values.ZilObject[]> Parser, System.Func<int, int, bool> SupportsCall)> PredCallParsers = new() {");
                sb.Indent();
                foreach (var group in predCallGroup)
                {
                    var cleanName = group.BuiltinName?.Trim() ?? "";
                    var sanitizedName = SanitizeName(cleanName);
                    var parserName = $"Generated_{sanitizedName}_{group.CallType}_Parser";
                    var capabilityChecker = GenerateCapabilityChecker(group);
                    sb.AppendLine($"{{ \"{cleanName}\", ({parserName}, {capabilityChecker}) }},");
                }
                sb.Unindent();
                sb.AppendLine("};");
                sb.AppendLine();
            }

            // Generate ValuePredCall dictionary with capability checking
            var valuePredCallGroup = callTypeGroups.FirstOrDefault(g => g.Key == "ValuePredCall");
            if (valuePredCallGroup != null)
            {
                sb.AppendLine("internal static readonly System.Collections.Generic.Dictionary<string, (System.Func<Zilf.Compiler.Builtins.ValuePredCall, Zilf.Interpreter.Values.ZilObject[], Zilf.Emit.IOperand> Parser, System.Func<int, int, bool> SupportsCall)> ValuePredCallParsers = new() {");
                sb.Indent();
                foreach (var group in valuePredCallGroup)
                {
                    var cleanName = group.BuiltinName?.Trim() ?? "";
                    var sanitizedName = SanitizeName(cleanName);
                    var parserName = $"Generated_{sanitizedName}_{group.CallType}_Parser";
                    var capabilityChecker = GenerateCapabilityChecker(group);
                    sb.AppendLine($"{{ \"{cleanName}\", ({parserName}, {capabilityChecker}) }},");
                }
                sb.Unindent();
                sb.AppendLine("};");
            }

            sb.AppendLine();

            // Emit a strongly-typed C# initializer for GeneratedBuiltinMetadata. This avoids
            // embedding raw JSON and preserves enum types in the generated code.
            var initByName = new System.Collections.Generic.SortedDictionary<string, System.Collections.Generic.List<string>>(System.StringComparer.Ordinal);
            foreach (var group in overloadGroups)
            {
                var cleanName = group.BuiltinName?.Trim() ?? "";
                if (string.IsNullOrWhiteSpace(cleanName)) continue;

                foreach (var overload in group.Overloads)
                {
                    var argsParams = overload.Method.MethodSymbol.Parameters.Skip(1).Where(p => !HasDataAttribute(p)).ToArray();
                    int requiredCount = argsParams.Count(p => !p.IsOptional && !p.IsParams);
                    int optionalCount = argsParams.Count(p => p.IsOptional && !p.IsParams);
                    bool hasParams = argsParams.Any(p => p.IsParams);
                    var minVersionExpr = overload.Attribute.MinVersion.HasValue ? overload.Attribute.MinVersion.Value.ToString() : "1";
                    var maxVersionExpr = overload.Attribute.MaxVersion.HasValue ? overload.Attribute.MaxVersion.Value.ToString() : "6";

                    string maxArgExpr = hasParams ? "(int?)null" : (requiredCount + optionalCount).ToString();

                    var entry = $"new BuiltinSignature(CallType.{group.CallType}, {requiredCount}, {maxArgExpr}, new ArgPart[0], {minVersionExpr}, {maxVersionExpr})";

                    if (!initByName.TryGetValue(cleanName, out var list))
                    {
                        list = [];
                        initByName[cleanName] = list;
                    }
                    list.Add(entry);
                }
            }

            var dictBuilder = new IndentedStringBuilder();
            dictBuilder.AppendLine("/* Auto-generated builtin metadata - do not edit. */");
            dictBuilder.AppendLine("#nullable disable");
            dictBuilder.AppendLine("using System;\nusing System.Collections.Generic;\nusing Zilf.Language.Signatures;\nusing Zilf.Interpreter;\nnamespace Zilf.Compiler.Builtins");
            dictBuilder.AppendLine("{");
            dictBuilder.Indent();
            dictBuilder.AppendLine("internal static partial class GeneratedBuiltinParsers");
            dictBuilder.AppendLine("{");
            dictBuilder.Indent();
            dictBuilder.AppendLine("// Dictionary mapping builtin name -> array of ISignature instances.\n        // These are constructed using ZBuiltinSignature.FromGeneratedMeta so we reuse the\n        // existing signature types in Zilf.Language.Signatures and avoid duplication.\n");
            dictBuilder.AppendLine("internal static readonly System.Collections.Generic.Dictionary<string, ISignature[]> GeneratedBuiltinMetadata = new() {");
            dictBuilder.Indent();
            dictBuilder.AppendLine("// name -> overloads");
            foreach (var kv in initByName)
            {
                var entries = string.Join(", ", kv.Value.Select(v =>
                {
                    // Each v currently looks like: new BuiltinSignature(CallType.X, min, max, new ArgPart[0], minV, maxV)
                    // We need to extract the pieces we already computed earlier. Simpler: recompute from overloadGroups.
                    return v;
                }));

                // We'll build entries by finding corresponding overloads in overloadGroups for this name
                var overloadEntries = new List<string>();
                foreach (var group in overloadGroups.Where(g => string.Equals(g.BuiltinName?.Trim(), kv.Key, StringComparison.Ordinal)))
                {
                    foreach (var ov in group.Overloads)
                    {
                        var argsParams = ov.Method.MethodSymbol.Parameters.Skip(1).Where(p => !HasDataAttribute(p)).ToArray();
                        int requiredCount = argsParams.Count(p => !p.IsOptional && !p.IsParams);
                        int optionalCount = argsParams.Count(p => p.IsOptional && !p.IsParams);
                        bool hasParams = argsParams.Any(p => p.IsParams);
                        var minVersionExpr = ov.Attribute.MinVersion.HasValue ? ov.Attribute.MinVersion.Value.ToString() : "1";
                        var maxVersionExpr = ov.Attribute.MaxVersion.HasValue ? ov.Attribute.MaxVersion.Value.ToString() : "6";

                        // Build ISignaturePart[] initializer using SignatureBuilder calls
                        // and format it as a multiline C# array using IndentedStringBuilder
                        var partExprs = new System.Collections.Generic.List<string>();
                        foreach (var p in argsParams)
                        {
                            var pType = p.Type;
                            // Use the actual parameter name from the method signature for the identifier
                            var paramName = p.Name ?? "arg";
                            string innerExpr;

                            // If parameter is annotated with [Table], emit TABLE constraint
                            var paramHasTable = p.GetAttributes().Any(a => a.AttributeClass?.Name == "TableAttribute" || a.AttributeClass?.Name == "Table");
                            if (paramHasTable)
                            {
                                innerExpr = $"SignatureBuilder.Constrained(SignatureBuilder.Identifier(\"{paramName}\"), Constraint.OfPrimType(PrimType.TABLE))";
                            }
                            else
                            {
                                var named = pType.Name;
                                innerExpr = named switch
                                {
                                    "Int32" or "Int16" or "Int64" or "Int" => $"SignatureBuilder.Constrained(SignatureBuilder.Identifier(\"{paramName}\"), Constraint.OfPrimType(PrimType.FIX))",
                                    "String" or "StringBuilder" => $"SignatureBuilder.Constrained(SignatureBuilder.Identifier(\"{paramName}\"), Constraint.OfPrimType(PrimType.STRING))",
                                    "ZilAtom" or "Atom" => $"SignatureBuilder.Constrained(SignatureBuilder.Identifier(\"{paramName}\"), Constraint.OfPrimType(PrimType.ATOM))",
                                    _ => $"SignatureBuilder.Identifier(\"{paramName}\")",
                                };
                            }

                            string paramExpr = innerExpr;
                            if (p.IsParams)
                            {
                                paramExpr = $"SignatureBuilder.VarArgs({innerExpr}, false)";
                            }
                            else if (p.IsOptional)
                            {
                                paramExpr = $"SignatureBuilder.Optional({innerExpr})";
                            }

                            partExprs.Add(paramExpr);
                        }

                        // Build the overload block using IndentedStringBuilder
                        var ob = new IndentedStringBuilder();
                        ob.AppendLine("ZBuiltinSignature.FromGeneratedParts(");
                        ob.Indent();

                        ob.AppendLine("[");
                        ob.Indent();
                        for (int pi = 0; pi < partExprs.Count; pi++)
                        {
                            var comma = pi < partExprs.Count - 1 ? "," : "";
                            ob.AppendLine(partExprs[pi] + comma);
                        }
                        ob.Unindent();
                        ob.AppendLine("],");

                        // Build return part expression
                        string returnExpr = "LiteralPart.From(\"T\")";
                        // Check if the method's return value is annotated with [Table]
                        var methodSymbol = ov.Method.MethodSymbol;
                        var returnHasTable = methodSymbol.GetReturnTypeAttributes().Any(a => a.AttributeClass?.Name == "TableAttribute" || a.AttributeClass?.Name == "Table");
                        if (returnHasTable)
                        {
                            returnExpr = "SignatureBuilder.Constrained(SignatureBuilder.Identifier(\"$return\"), Constraint.OfPrimType(PrimType.TABLE))";
                        }
                        else if (group.CallType == "ValueCall")
                        {
                            returnExpr = "SignatureBuilder.Constrained(SignatureBuilder.Identifier(\"$return\"), Constraint.AnyObject)";
                        }
                        else if (group.CallType == "PredCall")
                        {
                            returnExpr = "SignatureBuilder.Constrained(SignatureBuilder.Identifier(\"$return\"), Constraint.Boolean)";
                        }

                        ob.AppendLine(returnExpr + ",");
                        ob.AppendLine($"minArgs: {requiredCount},");
                        ob.AppendLine($"maxArgs: {(hasParams ? "null" : (requiredCount + optionalCount).ToString())},");
                        ob.AppendLine($"minVersion: {minVersionExpr},");
                        ob.AppendLine($"maxVersion: {maxVersionExpr}),");
                        ob.Unindent();

                        overloadEntries.Add(ob.ToString());
                    }
                }

                // Emit each overload as a separate, nicely-indented block inside the
                // C# 13 collection expression. Place the opening `[` on its own line,
                // then emit each overload block indented, followed by a trailing comma,
                // and finally the closing `]` on its own line.
                dictBuilder.AppendLine($"{{ \"{kv.Key}\", [");
                foreach (var ov in overloadEntries)
                {
                    // ov may contain multiple lines; indent each line by 18 spaces
                    var ovLines = ov.Replace("\r", "").Split('\n');
                    foreach (var ol in ovLines)
                    {
                        var trimmed = ol.TrimEnd();
                        // Skip empty lines produced by StringBuilder semantics
                        if (string.IsNullOrWhiteSpace(trimmed))
                            continue;
                        dictBuilder.Indent();
                        dictBuilder.AppendLine(trimmed);
                        dictBuilder.Unindent();
                    }
                    // overloadBuilder already includes the trailing comma on the final line
                }
                dictBuilder.AppendLine("] },");
            }
            dictBuilder.Unindent();
            dictBuilder.AppendLine("};");
            dictBuilder.Unindent();
            dictBuilder.AppendLine("}");
            dictBuilder.Unindent();
            dictBuilder.AppendLine("}");

            return dictBuilder.ToString();
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

        private static void GenerateOverloadDispatchBody(IndentedStringBuilder sb, OverloadGroup group)
        {
            // If there's only one overload, generate simple dispatch
            if (group.Overloads.Count == 1)
            {
                GenerateSingleOverloadBody(sb, group.Overloads[0], group.CallType, group.BuiltinName ?? "", emitVersionGuard: true);
                return;
            }

            // Multi-overload dispatch: analyze parameter types to determine dispatch strategy
            GenerateMultiOverloadDispatchBody(sb, group);
        }

        private static void GenerateSingleOverloadBody(IndentedStringBuilder sb, OverloadInfo overload, string callType, string operationName, bool emitVersionGuard = true)
        {
            var method = overload.Method;
            var attr = overload.Attribute;
            var parameters = method.MethodSymbol.Parameters;

            if (emitVersionGuard)
            {
                // Emit a runtime guard for Z-machine version applicability. If this overload
                // does not apply to the current Z-machine version, report a helpful error
                // and return (matching prior reflection-based behavior). Return values must
                // match the parser method's return type: ValueCall and ValuePredCall return
                // an IOperand; PredCall/VoidCall return void.
                var minVer = attr.MinVersion ?? 1;
                var maxVer = attr.MaxVersion ?? 6;
                sb.AppendLine($"// Version guard: applies to versions {minVer}..{maxVer}");
                sb.AppendLine($"if (!Zilf.ZModel.ZEnvironment.VersionMatches(c.cc.Context.ZEnvironment.ZVersion, {minVer}, {maxVer}))");
                sb.AppendLine("{");
                sb.Indent();
                if (callType == "ValueCall")
                {
                    sb.AppendLine($"return c.HandleMessage(CompilerMessages._0_Is_Not_Supported_In_This_Zmachine_Version, \"{operationName}\");");
                }
                else if (callType == "ValuePredCall")
                {
                    sb.AppendLine($"c.cc.Context.HandleError(new CompilerError(c.form, CompilerMessages._0_Is_Not_Supported_In_This_Zmachine_Version, \"{operationName}\"));");
                    sb.AppendLine("return c.cc.Game.Zero;");
                }
                else // PredCall or VoidCall
                {
                    sb.AppendLine($"c.cc.Context.HandleError(new CompilerError(c.form, CompilerMessages._0_Is_Not_Supported_In_This_Zmachine_Version, \"{operationName}\"));");
                    sb.AppendLine("return;");
                }
                sb.Unindent();
                sb.AppendLine("}");
            }


            // Skip the first parameter (call context) - it's handled by the parser signature
            var argsParameters = parameters.Skip(1).ToArray();
            var paramInfo = AnalyzeMethodParameters(argsParameters);

            // Generate argument count validation
            GenerateArgumentValidation(sb, paramInfo, attr.Name, GetReturnTypeForCallType(callType), callType);

            sb.AppendLine();

            int paramsOperandIndex = -1;
            for (int i = 0; i < paramInfo.ArgumentParameters.Count; i++)
            {
                var param = paramInfo.ArgumentParameters[i];
                if (param.IsParams && param.Type is IArrayTypeSymbol arrType && arrType.ElementType.Name == "IOperand")
                {
                    paramsOperandIndex = i;
                    break;
                }
            }
            bool isVarargs = paramInfo.ArgumentParameters.Any(p => p.IsParams);

            bool hasOperandParams = false;
            for (int i = 0; i < paramInfo.ArgumentParameters.Count; i++)
            {
                var param = paramInfo.ArgumentParameters[i];
                if (IsIOperandType(param.Type) || (param.IsParams && paramsOperandIndex == i))
                {
                    hasOperandParams = true;
                    break;
                }
            }

            if (hasOperandParams)
            {
                // Only pass the args that correspond to operand parameters to CompileOperands
                var operandArgsList = new List<string>();
                var operandArgIndices = new List<int>();
                for (int i = 0; i < paramInfo.ArgumentParameters.Count; i++)
                {
                    var param = paramInfo.ArgumentParameters[i];
                    if (IsIOperandType(param.Type) || (param.IsParams && paramsOperandIndex == i))
                    {
                        operandArgsList.Add(GetUnwrappedArgExpression(i));
                        operandArgIndices.Add(i);
                    }
                }
                // If there are no operand arguments, do not use CompileOperands
                if (operandArgsList.Count == 0)
                {
                    // All params are ZilObject or similar, assign directly
                    for (int i = 0; i < paramInfo.ArgumentParameters.Count; i++)
                    {
                        var param = paramInfo.ArgumentParameters[i];
                        var paramType = param.Type;
                        var paramName = $"arg{i}_{param.Name}";

                        if (param.IsParams)
                        {
                            // Handle params array by slicing the remaining arguments and unwrapping macro results
                            // Handle params array parameter - collect remaining arguments
                            sb.AppendLine($"// DEBUG: Handling params array for {param.Name}");
                            sb.AppendLine($"var {paramName} = args.Length > {i} ? args[{i}..].Select((arg, idx) => arg is ZilMacroResult zmr_slice ? zmr_slice.Inner : arg).ToArray() : System.Array.Empty<ZilObject>();");
                        }
                        else if (paramType.SpecialType == SpecialType.System_String)
                        {
                            sb.AppendLine($"if (!({GetUnwrappedArgExpression(i)} is ZilString _zs_{paramName}))");
                            sb.AppendLine("{");
                            sb.Indent();
                            sb.AppendLine("c.cc.Context.HandleError(new CompilerError(c.form, CompilerMessages.Argument_Must_Be_Literal_String));");
                            sb.AppendLine($"return {(callType == "ValueCall" ? "c.cc.Game.Zero" : "")};");
                            sb.Unindent();
                            sb.AppendLine("}");
                            sb.AppendLine($"var {paramName} = Compilation.TranslateString(_zs_{paramName}, c.cc.Context);");
                        }
                        else if (param.IsOptional)
                        {
                            sb.AppendLine($"var {paramName} = args.Length > {i} ? ");
                            GenerateParameterConversionExpression(sb, paramType, GetUnwrappedArgExpression(i), callType == "ValueCall", "", param, operationName, i + 1);
                            sb.AppendLine(" :");
                            sb.Indent();
                            sb.AppendLine($"{GetDefaultValueForType(param, paramType)};");
                            sb.Unindent();
                        }
                        else
                        {
                            sb.Append($"var {paramName} = ");
                            GenerateParameterConversionExpression(sb, paramType, GetUnwrappedArgExpression(i), callType == "ValueCall", "", param, operationName, i + 1);
                            sb.AppendLine(";");
                        }
                    }
                    sb.AppendLine();
                    // Generate method call
                    GenerateMethodCall(sb, method.MethodSymbol, paramInfo, attr, GetReturnTypeForCallType(callType));
                }
                else
                {
                    string operandArgsExpr;
                    if (isVarargs)
                    {
                        // Varargs: pass all args, but unwrap any ZilMacroResult elements before compiling
                        operandArgsExpr = "args.Length > 0 ? args.Select(a => a is ZilMacroResult zmr ? zmr.Inner : a).ToArray() : System.Array.Empty<ZilObject>()";
                    }
                    else if (operandArgsList.Count == 1 && operandArgIndices.Count == 1 && paramInfo.ArgumentParameters[operandArgIndices[0]].IsParams)
                    {
                        // Single params IOperand[]: pass all args from the start index, unwrapping macro results
                        int startIdx = operandArgIndices[0];
                        operandArgsExpr = $"args.Length > {startIdx} ? args[{startIdx}..args.Length].Select(a => a is ZilMacroResult zmr ? zmr.Inner : a).ToArray() : System.Array.Empty<ZilObject>()";
                    }
                    else if (operandArgsList.Count > 0)
                    {
                        // For fixed operands, only pass as many args as are present; when falling back to passing a slice, unwrap macros
                        operandArgsExpr = $"args.Length >= {operandArgsList.Count} ? new[] {{ {string.Join(", ", operandArgsList)} }} : args.Length > 0 ? args[..args.Length].Select(a => a is ZilMacroResult zmr ? zmr.Inner : a).ToArray() : System.Array.Empty<ZilObject>()";
                    }
                    else
                    {
                        operandArgsExpr = "System.Array.Empty<ZilObject>()";
                    }
                    // Compile operand arguments with macro unwrapping for correct evaluation order
                    sb.AppendLine($"using (var operands = c.cc.CompileOperands(c.rb, c.form.SourceLine, {operandArgsExpr}))");
                    sb.AppendLine("{");
                    sb.Indent();
                    sb.AppendLine("int opIndex = 0;");
                    sb.AppendLine("int localOpIndex = opIndex;");
                    for (int i = 0; i < paramInfo.ArgumentParameters.Count; i++)
                    {
                        var param = paramInfo.ArgumentParameters[i];
                        var paramType = param.Type;
                        var paramName = $"arg{i}_{param.Name}";
                        if (param.IsParams && paramsOperandIndex == i && paramType is IArrayTypeSymbol arrType && arrType.ElementType.Name == "IOperand")
                        {
                            sb.AppendLine($"var {paramName} = (operands.Count > localOpIndex) ? new IOperand[operands.Count - localOpIndex] : System.Array.Empty<IOperand>();");
                            sb.AppendLine($"for (int j = 0; j < {paramName}.Length; j++)");
                            sb.Indent();
                            sb.AppendLine($"{paramName}[j] = operands[localOpIndex + j];");
                            sb.Unindent();
                            sb.AppendLine("localOpIndex = -1; // Mark all remaining operands as consumed");
                        }
                        else if (IsIOperandType(paramType))
                        {
                            sb.AppendLine($"var {paramName} = localOpIndex < operands.Count ? operands[localOpIndex] : null;");
                            sb.AppendLine("localOpIndex++;");
                        }
                        else if (paramType.SpecialType == SpecialType.System_String)
                        {
                            sb.AppendLine($"var unwrapped_{paramName} = args.Length > {i} ? (args[{i}] is ZilMacroResult zmr{i} ? zmr{i}.Inner : args[{i}]) : null;");
                            sb.AppendLine($"if (!(unwrapped_{paramName} is ZilString _zs_{paramName}))");
                            sb.AppendLine("{");
                            sb.Indent();
                            sb.AppendLine("c.cc.Context.HandleError(new CompilerError(c.form, CompilerMessages.Argument_Must_Be_Literal_String));");
                            sb.AppendLine($"return {(callType == "ValueCall" ? "c.cc.Game.Zero" : "")};");
                            sb.Unindent();
                            sb.AppendLine("}");
                            sb.AppendLine($"var {paramName} = Compilation.TranslateString(_zs_{paramName}, c.cc.Context);");
                        }
                        else if (param.IsOptional)
                        {
                            sb.AppendLine($"var {paramName} = args.Length > {i} ? ");
                            GenerateParameterConversionExpression(sb, paramType, GetUnwrappedArgExpression(i), callType == "ValueCall", "", param, operationName, i + 1);
                            sb.AppendLine(" :");
                            sb.Indent();
                            sb.AppendLine($"{GetDefaultValueForType(param, paramType)};");
                            sb.Unindent();
                        }
                        else
                        {
                            sb.Append($"var {paramName} = ");
                            GenerateParameterConversionExpression(sb, paramType, GetUnwrappedArgExpression(i), callType == "ValueCall", "", param, operationName, i + 1);
                            sb.AppendLine(";");
                        }
                    }
                    sb.AppendLine();
                    // Generate method call inside the using block
                    GenerateMethodCall(sb, method.MethodSymbol, paramInfo, attr, GetReturnTypeForCallType(callType));
                    sb.Unindent();
                    sb.AppendLine("}");
                }
            }
            else
            {
                // For non-operand builtins, assign all parameters from args with conversion
                for (int i = 0; i < paramInfo.ArgumentParameters.Count; i++)
                {
                    var param = paramInfo.ArgumentParameters[i];
                    var paramType = param.Type;
                    var paramName = $"arg{i}_{param.Name}";

                    if (param.IsParams)
                    {
                        // Handle params array by slicing the remaining arguments and unwrapping macro results
                        sb.AppendLine($"var {paramName} = args.Length > {i} ? args[{i}..].Select((arg, idx) => arg is ZilMacroResult zmr_slice2 ? zmr_slice2.Inner : arg).ToArray() : System.Array.Empty<ZilObject>();");
                    }
                    else if (paramType.SpecialType == SpecialType.System_String)
                    {
                        sb.AppendLine($"if (!({GetUnwrappedArgExpression(i)} is ZilString _zs_{paramName}))");
                        sb.AppendLine("{");
                        sb.Indent();
                        sb.AppendLine("c.cc.Context.HandleError(new CompilerError(c.form, CompilerMessages.Argument_Must_Be_Literal_String));");
                        sb.AppendLine($"return {(callType == "ValueCall" ? "c.cc.Game.Zero" : "")};");
                        sb.Unindent();
                        sb.AppendLine("}");
                        sb.AppendLine($"var {paramName} = Compilation.TranslateString(_zs_{paramName}, c.cc.Context);");
                    }
                    else if (param.IsOptional)
                    {
                        sb.AppendLine($"var {paramName} = args.Length > {i} ? ");
                        GenerateParameterConversionExpression(sb, paramType, GetUnwrappedArgExpression(i), callType == "ValueCall", "", param, operationName, i + 1);
                        sb.AppendLine(" :");
                        sb.Indent();
                        sb.AppendLine($"{GetDefaultValueForType(param, paramType)};");
                        sb.Unindent();
                    }
                    else
                    {
                        sb.Append($"var {paramName} = ");
                        GenerateParameterConversionExpression(sb, paramType, GetUnwrappedArgExpression(i), callType == "ValueCall", "", param, operationName, i + 1);
                        sb.AppendLine(";");
                    }
                }
                sb.AppendLine();
                // Generate method call
                GenerateMethodCall(sb, method.MethodSymbol, paramInfo, attr, GetReturnTypeForCallType(callType));
            }
        }

        private static List<int> GetValidArgumentCounts(OverloadInfo overload)
        {
            var argsParams = overload.Method.MethodSymbol.Parameters.Skip(1).Where(p => !HasDataAttribute(p)).ToArray();
            var requiredCount = argsParams.Count(p => !p.IsOptional && !p.IsParams);
            var optionalCount = argsParams.Count(p => p.IsOptional);
            var hasParamsArray = argsParams.Any(p => p.IsParams);

            var validCounts = new List<int>();

            if (hasParamsArray)
            {
                // With params array, accept required count or more
                for (int i = requiredCount; i <= requiredCount + optionalCount + 10; i++) // Limit to reasonable range
                {
                    validCounts.Add(i);
                }
            }
            else
            {
                // Without params array, accept required count through required + optional
                for (int i = requiredCount; i <= requiredCount + optionalCount; i++)
                {
                    validCounts.Add(i);
                }
            }

            return validCounts;
        }

        private static void GenerateMacroUnwrapping(IndentedStringBuilder sb, ParameterInfo paramInfo)
        {
            // Generate unwrapping statements for all arguments
            sb.AppendLine("// Unwrap macro results to avoid variable name conflicts");
            for (int i = 0; i < paramInfo.ArgumentParameters.Count; i++)
            {
                sb.AppendLine($"var unwrapped_{i} = args.Length > {i} ? (args[{i}] is ZilMacroResult zmr{i} ? zmr{i}.Inner : args[{i}]) : null;");
            }
        }

        private static string GetUnwrappedArgExpression(int index)
        {
            // Use a simple ternary expression without declaring variables
            // This avoids variable declaration conflicts
            return $"(args[{index}] is ZilMacroResult ? ((ZilMacroResult)args[{index}]).Inner : args[{index}])";
        }

        private static void GenerateMultiOverloadDispatchBody(IndentedStringBuilder sb, OverloadGroup group)
        {
            // Analyze all overloads to understand dispatch requirements
            // Removed unused variable analysis for variable/operand/softglobal parameters

            // Group overloads by argument count to handle different arities
            // For overloads with optional parameters, expand to all valid argument counts
            var argumentCountToOverloads = new Dictionary<int, List<OverloadInfo>>();

            foreach (var overload in group.Overloads)
            {
                var validCounts = GetValidArgumentCounts(overload);
                foreach (var count in validCounts)
                {
                    if (!argumentCountToOverloads.ContainsKey(count))
                    {
                        argumentCountToOverloads[count] = [];
                    }
                    argumentCountToOverloads[count].Add(overload);
                }
            }

            var overloadsByArgCount = argumentCountToOverloads
                .Select(kvp => new ArgumentCountGroup { Key = kvp.Key, Overloads = kvp.Value })
                .OrderBy(x => x.Key)
                .ToList();

            if (overloadsByArgCount.Count == 1)
            {
                // All overloads have the same argument count - check if we need variable type dispatch
                var expectedArgCount = overloadsByArgCount[0].Key;
                var overloadsForThisCount = overloadsByArgCount[0].Overloads;

                // Generate argument count validation
                sb.AppendLine($"if (args.Length != {expectedArgCount})");
                sb.AppendLine("{");
                sb.Indent();
                GenerateErrorReturn(sb, group.BuiltinName, expectedArgCount.ToString(), GetReturnTypeForCallType(group.CallType), group.CallType);
                sb.Unindent();
                sb.AppendLine("}");
                sb.AppendLine();

                // Check if we have multiple overloads with variable parameters that need runtime dispatch
                if (overloadsForThisCount.Count > 1 && NeedsVariableTypeDispatch(overloadsForThisCount))
                {
                    var singleArgCountGroup = new OverloadGroup
                    {
                        BuiltinName = group.BuiltinName ?? "",
                        CallType = group.CallType,
                        Overloads = overloadsForThisCount
                    };
                    GenerateVariableTypeDispatch(sb, singleArgCountGroup);
                }
                else
                {
                    // Simple dispatch - just call the first overload
                    GenerateSingleOverloadBody(sb, overloadsForThisCount[0], group.CallType, group.BuiltinName ?? "", emitVersionGuard: true);
                }
            }
            else
            {
                // Multiple overloads with different argument counts - generate switch dispatch
                GenerateArgumentCountDispatch(sb, group, overloadsByArgCount);
            }
        }

        private static void GenerateArgumentCountDispatch(IndentedStringBuilder sb, OverloadGroup group, List<ArgumentCountGroup> overloadsByArgCount)
        {
            // Generate switch statement based on argument count
            sb.AppendLine("switch (args.Length)");
            sb.AppendLine("{");
            sb.Indent();

            foreach (var argCountGroup in overloadsByArgCount.OrderBy(g => g.Key))
            {
                var argCount = argCountGroup.Key;
                var overloadsForCount = argCountGroup.Overloads;

                sb.AppendLine($"case {argCount}:");
                sb.AppendLine("{");  // Add braces to create proper scope
                sb.Indent();

                if (overloadsForCount.Count == 1)
                {
                    // Single overload for this arg count
                    // Don't emit version guard in case statements - capability checker handles versions
                    GenerateSingleOverloadBody(sb, overloadsForCount[0], group.CallType, group.BuiltinName ?? "", emitVersionGuard: false);
                }
                else if (NeedsVariableTypeDispatch(overloadsForCount))
                {
                    // Multiple overloads that differ by variable types
                    var singleArgCountGroup = new OverloadGroup
                    {
                        BuiltinName = group.BuiltinName ?? "",
                        CallType = group.CallType,
                        Overloads = overloadsForCount
                    };
                    GenerateVariableTypeDispatch(sb, singleArgCountGroup);
                }
                else
                {
                    // Multiple overloads, use first one (may need more sophisticated dispatch in future)
                    // Don't emit version guard in case statements - capability checker handles versions
                    GenerateSingleOverloadBody(sb, overloadsForCount[0], group.CallType, group.BuiltinName ?? "", emitVersionGuard: false);
                }

                // Check if the method returns void to determine if we need a break
                var returnType = GetReturnTypeForCallType(group.CallType);
                if (returnType == "void")
                {
                    sb.AppendLine("return;");
                }
                else
                {
                    // For non-void methods, the return is handled in GenerateSingleOverloadBodyForArgCount
                    // so we don't need an explicit return or break here
                }

                sb.Unindent();
                sb.AppendLine("}");
                sb.AppendLine();
            }

            // Generate default case for invalid argument counts
            sb.AppendLine("default:");
            sb.Indent();
            var validArgCounts = overloadsByArgCount.Select(g => g.Key.ToString()).ToArray();
            var validCountsString = validArgCounts.Length == 1
                ? validArgCounts[0]
                : string.Join(" or ", validArgCounts);
            GenerateErrorReturn(sb, group.BuiltinName ?? "", validCountsString, GetReturnTypeForCallType(group.CallType), group.CallType);
            sb.Unindent();
            // Don't add break after return - it's unreachable

            sb.Unindent();
            sb.AppendLine("}");
        }

        private static bool HasVariableAttribute(IParameterSymbol param)
        {
            return param.GetAttributes().Any(a => a.AttributeClass?.Name == "VariableAttribute");
        }

        private static bool HasSoftGlobalParameter(OverloadInfo overload)
        {
            var argsParams = overload.Method.MethodSymbol.Parameters.Skip(1).Where(p => !HasDataAttribute(p)).ToArray();
            return argsParams.Any(p => HasVariableAttribute(p) && p.Type.Name == "SoftGlobal");
        }

        private static bool HasIVariableParameter(OverloadInfo overload)
        {
            var argsParams = overload.Method.MethodSymbol.Parameters.Skip(1).Where(p => !HasDataAttribute(p)).ToArray();
            return argsParams.Any(p => HasVariableAttribute(p) && IsIVariableType(p.Type));
        }

        private static bool HasIOperandParameter(OverloadInfo overload)
        {
            var argsParams = overload.Method.MethodSymbol.Parameters.Skip(1).Where(p => !HasDataAttribute(p)).ToArray();
            // Consider IOperand parameters that have [Variable] attribute - these expect variable.Hard.Indirect
            return argsParams.Any(p => HasVariableAttribute(p) && IsIOperandType(p.Type));
        }

        // Removed GetVariableQuirksArgument: referenced undefined variables and is not used.

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

        private static void GenerateBuiltinMethodBody(IndentedStringBuilder sb, BuiltinMethodInfo method, BuiltinAttributeInfo attr)
        {
            var methodSymbol = method.MethodSymbol;
            var parameters = methodSymbol.Parameters;

            // Skip the first parameter (call context) - it's handled by the parser signature
            var argsParameters = parameters.Skip(1).ToArray();

            var callType = GetCallTypeForBuiltinParser(methodSymbol);
            var returnType = GetReturnTypeForBuiltinParser(methodSymbol);

            // Analyze method signature to understand parameter structure
            var paramInfo = AnalyzeMethodParameters(argsParameters);

            // Generate argument count validation
            GenerateArgumentValidation(sb, paramInfo, attr.Name, returnType, callType);

            sb.AppendLine();

            // Generate method call
            GenerateMethodCall(sb, methodSymbol, paramInfo, attr, returnType);
        }

        private static bool HasDataAttribute(IParameterSymbol param)
        {
            return param.GetAttributes().Any(a => a.AttributeClass?.Name == "DataAttribute");
        }

        private static ParameterInfo AnalyzeMethodParameters(IParameterSymbol[] parameters)
        {
            var info = new ParameterInfo();

            // Most builtin methods have call context as first parameter, skip it
            int startIndex = 0;
            if (parameters.Length > 0)
            {
                var firstParamType = parameters[0].Type.Name;
                if (firstParamType is "ValueCall" or "VoidCall" or "PredCall" or "ValuePredCall")
                {
                    startIndex = 1; // Skip call context parameter
                }
            }

            for (int i = startIndex; i < parameters.Length; i++)
            {
                var param = parameters[i];

                if (HasDataAttribute(param))
                {
                    info.DataParameters.Add(param);
                }
                else
                {
                    info.ArgumentParameters.Add(param);

                    if (!param.IsOptional && !param.IsParams)
                        info.RequiredArgumentCount++;
                    if (param.IsOptional)
                        info.OptionalArgumentCount++;
                    if (param.IsParams)
                        info.HasParamsArray = true;
                }
            }

            return info;
        }

        private static void GenerateArgumentValidation(IndentedStringBuilder sb, ParameterInfo paramInfo, string builtinName, string returnType, string callType)
        {
            if (paramInfo.HasParamsArray)
            {
                sb.AppendLine($"if (args.Length < {paramInfo.RequiredArgumentCount})");
                sb.AppendLine("{");
                sb.Indent();
                GenerateErrorReturn(sb, builtinName, $"{paramInfo.RequiredArgumentCount}+", returnType, callType);
                sb.Unindent();
                sb.AppendLine("}");
            }
            else
            {
                var maxCount = paramInfo.RequiredArgumentCount + paramInfo.OptionalArgumentCount;
                if (paramInfo.RequiredArgumentCount == maxCount)
                {
                    sb.AppendLine($"if (args.Length != {paramInfo.RequiredArgumentCount})");
                    sb.AppendLine("{");
                    sb.Indent();
                    GenerateErrorReturn(sb, builtinName, paramInfo.RequiredArgumentCount.ToString(), returnType, callType);
                    sb.Unindent();
                    sb.AppendLine("}");
                }
                else
                {
                    sb.AppendLine($"if (args.Length < {paramInfo.RequiredArgumentCount} || args.Length > {maxCount})");
                    sb.AppendLine("{");
                    sb.Indent();
                    GenerateErrorReturn(sb, builtinName, $"{paramInfo.RequiredArgumentCount}-{maxCount}", returnType, callType);
                    sb.Unindent();
                    sb.AppendLine("}");
                }
            }
        }

        private static void GenerateMethodCall(IndentedStringBuilder sb, IMethodSymbol method, ParameterInfo paramInfo, BuiltinAttributeInfo attr, string returnType)
        {
            var methodCall = new StringBuilder();
            methodCall.Append($"ZBuiltins.{method.Name}(c");

            // Add parameters in the correct order: [Data] parameters first, then argument parameters
            int dataParamIndex = 0;
            int argParamIndex = 0;

            for (int i = 1; i < method.Parameters.Length; i++) // Skip first parameter (call context)
            {
                var param = method.Parameters[i];
                var hasDataAttr = param.GetAttributes().Any(attr => attr.AttributeClass?.Name == "DataAttribute");

                if (hasDataAttr)
                {
                    // Add data attribute value with proper type conversion
                    if (!string.IsNullOrEmpty(attr.Data))
                    {
                        if (param.Type.TypeKind == TypeKind.Enum)
                        {
                            // Use named enum value instead of integer cast
                            var enumValueName = GetEnumValueName(param.Type, attr.Data);
                            methodCall.Append($", {enumValueName}");
                        }
                        else if (param.Type.Name == "StdAtom")
                        {
                            // Handle StdAtom - use named enum value
                            var enumValueName = GetEnumValueName(param.Type, attr.Data);
                            methodCall.Append($", {enumValueName}");
                        }
                        else if (param.Type.SpecialType == SpecialType.System_Boolean)
                        {
                            // Handle boolean values - convert string to boolean literal
                            var boolValue = attr.Data?.ToLowerInvariant() == "true" || attr.Data == "True";
                            methodCall.Append($", {boolValue.ToString().ToLowerInvariant()}");
                        }
                        else
                        {
                            methodCall.Append($", {attr.Data}");
                        }
                    }
                    else
                    {
                        // Provide a default value for the data parameter based on its type
                        var defaultValue = GetDefaultDataValue(param.Type);
                        methodCall.Append($", {defaultValue}");
                    }
                    dataParamIndex++;
                }
                else
                {
                    // Add regular argument - use parameter name with index for clarity
                    var paramName = paramInfo.ArgumentParameters[argParamIndex].Name;
                    methodCall.Append($", arg{argParamIndex}_{paramName}");
                    argParamIndex++;
                }
            }

            methodCall.Append(")");

            if (returnType == "void")
            {
                sb.AppendLine($"{methodCall};");
            }
            else
            {
                // Check if this is a ValuePredCall with a void ZBuiltins method
                var firstParam = method.Parameters.FirstOrDefault();
                bool isValuePredCall = firstParam?.Type.Name == "ValuePredCall";
                bool methodReturnsVoid = method.ReturnType.SpecialType == SpecialType.System_Void;

                if (isValuePredCall && methodReturnsVoid)
                {
                    // ValuePredCall methods handle result storage internally and return void
                    // The parser should call the method, then return the result storage
                    sb.AppendLine($"{methodCall};");
                    sb.AppendLine("return c.resultStorage;");
                }
                else
                {
                    sb.AppendLine($"return {methodCall};");
                }
            }
        }

        private static void GenerateParameterConversionExpression(IndentedStringBuilder sb, ITypeSymbol paramType, string argExpression, bool hasResultStorage, string indent, IParameterSymbol? param = null, string operationName = "", int argIndex = 0)
        {
            string quirksArg = "Zilf.Compiler.Builtins.VariableScopeQuirks.None";
            if (param != null)
            {
                var variableAttr = param.GetAttributes().FirstOrDefault(a => a.AttributeClass?.Name == "VariableAttribute");
                if (variableAttr != null)
                {
                    var named = variableAttr.NamedArguments.FirstOrDefault(kv => kv.Key == "VariableScopeQuirks");
                    if (named.Value.Value != null && named.Value.Type != null)
                        quirksArg = GetEnumValueName(named.Value.Type, named.Value.Value);
                }
            }
            if (IsSpecialType(paramType, "ZilAtom"))
            {
                sb.Append($"{indent}({argExpression} as ZilAtom ?? throw new ArgumentException($\"Expected ZilAtom, got {{({argExpression})?.GetType().Name}}\"))");
            }
            else if (paramType.Name == "Block")
            {
                // Resolve an activation atom (LVAL) to a Block instance, mirroring ParameterTypeHandler.BlockHandler
                // Generated code will evaluate the argument expression, check IsLVAL and lookup in c.cc.Blocks
                sb.Append($"{indent}({argExpression} is ZilObject _blockArg && _blockArg.IsLVAL(out var __act) ? (c.cc.Blocks.FirstOrDefault(b => b.Name == __act) ?? throw new CompilerError(c.form, CompilerMessages._0_Argument_1_2, \"{operationName}\", {argIndex}, \"argument must be bound to a block\")) : throw new CompilerError(c.form, CompilerMessages._0_Argument_1_2, \"{operationName}\", {argIndex}, \"argument must be a local variable reference\"))");
            }
            else if (IsIVariableType(paramType))
            {
                // Variable parameters: use GetVariable and throw if null with proper form context
                sb.Append($"{indent}GetVariable(c.cc, {argExpression}, {quirksArg})?.Hard ?? throw new CompilerError(c.form, CompilerMessages._0_Argument_1_2, \"{operationName}\", {argIndex}, \"must be a variable\")");
            }
            else if (paramType.Name == "SoftGlobal")
            {
                // SoftGlobal parameters: use GetVariable and throw if null with proper form context
                sb.Append($"{indent}GetVariable(c.cc, {argExpression}, {quirksArg})?.Soft ?? throw new CompilerError(c.form, CompilerMessages._0_Argument_1_2, \"{operationName}\", {argIndex}, \"must be a variable\")");
            }
            else if (IsIOperandType(paramType))
            {
                if (hasResultStorage)
                {
                    sb.Append($"{indent}c.cc.CompileAsOperand(c.rb, {argExpression}, c.form.SourceLine, c.resultStorage)");
                }
                else
                {
                    sb.Append($"{indent}c.cc.CompileAsOperand(c.rb, {argExpression}, c.form.SourceLine)");
                }
            }
            else if (paramType.TypeKind == TypeKind.Enum)
            {
                // For enum types, they should come from [Data] attribute, not arguments
                sb.Append($"{indent}default({paramType.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat)})");
            }
            else if (paramType.SpecialType == SpecialType.System_String)
            {
                // If the argument is a ZilString, extract its raw Text to avoid double-quoting
                sb.Append($"{indent}({argExpression} is ZilString _zs ? _zs.Text : {argExpression}.ToString())");
            }
            else if (paramType.SpecialType == SpecialType.System_Boolean)
            {
                sb.Append($"{indent}({argExpression} is ZilAtom atom && atom == StdAtom.T)");
            }
            else if (paramType.SpecialType == SpecialType.System_Int32)
            {
                sb.Append($"{indent}({argExpression} is ZilFix fix ? fix.Value : 0)");
            }
            else if (IsZilObjectType(paramType))
            {
                sb.Append($"{indent}{argExpression}");
            }
            else
            {
                // For other types, provide a conversion placeholder
                sb.Append($"{indent}/* TODO: Convert {argExpression} to {paramType.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat)} */ default({paramType.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat)})");
            }
        }

        private static string GetDefaultValueForType(IParameterSymbol param, ITypeSymbol type)
        {
            if (param.HasExplicitDefaultValue)
            {
                if (param.ExplicitDefaultValue == null)
                    return "null";
                if (param.ExplicitDefaultValue is string str)
                    return $"\"{str}\"";
                return param.ExplicitDefaultValue.ToString() ?? "null";
            }

            // Provide sensible defaults for common types
            if (type.CanBeReferencedByName && type.IsReferenceType)
                return "null";
            if (type.SpecialType == SpecialType.System_Boolean)
                return "false";
            if (type.SpecialType == SpecialType.System_Int32)
                return "0";
            if (IsIOperandType(type))
                return "null";

            return "null";
        }

        private static string GetDefaultDataValue(ITypeSymbol type)
        {
            if (type.TypeKind == TypeKind.Enum)
            {
                // For enum types, use the first enum value or provide a cast from 0
                return $"({type.ToDisplayString()})0";
            }
            if (type.SpecialType == SpecialType.System_Boolean)
                return "false";
            if (type.SpecialType == SpecialType.System_Int32)
                return "0";

            return $"default({type.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat)})";
        }

        private static bool IsSpecialType(ITypeSymbol type, string typeName)
        {
            return type.Name == typeName;
        }

        private static string GetEnumValueExpression(ITypeSymbol? enumType, object enumValue)
        {
            if (enumType == null)
                return enumValue.ToString();

            var fullyQualifiedTypeName = enumType.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat);

            // For most common enums, we can generate the named constant directly
            // based on the integer value and known enum structures
            var enumName = enumType.Name;
            var intValue = System.Convert.ToInt32(enumValue);

            switch (enumName)
            {
                case "VariableScopeQuirks":
                    return intValue switch
                    {
                        0 => $"{fullyQualifiedTypeName}.None",
                        1 => $"{fullyQualifiedTypeName}.Local",
                        2 => $"{fullyQualifiedTypeName}.Global",
                        3 => $"{fullyQualifiedTypeName}.Both",
                        _ => $"({fullyQualifiedTypeName}){intValue}"
                    };
                default:
                    // For other enums, we can try to find the field with the matching constant value
                    // by examining the enum type's members
                    if (enumType is INamedTypeSymbol namedEnumType)
                    {
                        var matchingField = namedEnumType.GetMembers()
                            .OfType<IFieldSymbol>()
                            .FirstOrDefault(f => f.IsStatic && f.HasConstantValue &&
                                               f.ConstantValue != null &&
                                               System.Convert.ToInt32(f.ConstantValue) == intValue);

                        if (matchingField != null)
                        {
                            return $"{fullyQualifiedTypeName}.{matchingField.Name}";
                        }
                    }

                    // Fallback to casting if we can't resolve the name
                    return $"({fullyQualifiedTypeName}){intValue}";
            }
        }

        private static void GenerateSimpleArgumentValidation(IndentedStringBuilder sb, IParameterSymbol[] parameters, string builtinName, string returnType, string callType)
        {
            var requiredCount = parameters.Count(p => !p.IsOptional && !p.IsParams);
            var optionalCount = parameters.Count(p => p.IsOptional && !p.IsParams);
            var hasParamsArray = parameters.Any(p => p.IsParams);

            if (hasParamsArray)
            {
                sb.AppendLine($"if (args.Length < {requiredCount})");
                sb.AppendLine("{");
                sb.Indent();
                GenerateErrorReturn(sb, builtinName, $"{requiredCount}+", returnType, callType);
                sb.Unindent();
                sb.AppendLine("}");
            }
            else
            {
                var maxCount = requiredCount + optionalCount;
                if (requiredCount == maxCount)
                {
                    sb.AppendLine($"if (args.Length != {requiredCount})");
                    sb.AppendLine("{");
                    sb.Indent();
                    GenerateErrorReturn(sb, builtinName, requiredCount.ToString(), returnType, callType);
                    sb.Unindent();
                    sb.AppendLine("}");
                }
                else
                {
                    sb.AppendLine($"if (args.Length < {requiredCount} || args.Length > {maxCount})");
                    sb.AppendLine("{");
                    sb.Indent();
                    GenerateErrorReturn(sb, builtinName, $"{requiredCount}-{maxCount}", returnType, callType);
                    sb.Unindent();
                    sb.AppendLine("}");
                }
            }
        }

        private static void GenerateErrorReturn(IndentedStringBuilder sb, string builtinName, string expectedCount, string returnType, string callType)
        {
            // The second argument to CountableString should be true if the number is anything but 1
            bool isPlural = expectedCount != "1";

            if (returnType == "void")
            {
                // VoidCall and PredCall
                sb.AppendLine($"c.cc.Context.HandleError(new CompilerError(c.form, CompilerMessages._0_Requires_1_Argument1s, \"{builtinName}\", new CountableString(\"{expectedCount}\", {isPlural.ToString().ToLower()})));");
                sb.AppendLine("return;");
            }
            else if (callType == "ValuePredCall")
            {
                // ValuePredCall doesn't have HandleMessage - use cc.Context.HandleError and return Zero
                sb.AppendLine($"c.cc.Context.HandleError(new CompilerError(c.form, CompilerMessages._0_Requires_1_Argument1s, \"{builtinName}\", new CountableString(\"{expectedCount}\", {isPlural.ToString().ToLower()})));");
                sb.AppendLine("return c.cc.Game.Zero;");
            }
            else
            {
                // ValueCall - has HandleMessage
                sb.AppendLine($"return c.HandleMessage(CompilerMessages._0_Requires_1_Argument1s, \"{builtinName}\", new CountableString(\"{expectedCount}\", {isPlural.ToString().ToLower()}));");
            }
        }

        private static bool IsZilObjectType(ITypeSymbol type)
        {
            return type.Name == "ZilObject" || type.BaseType?.Name == "ZilObject";
        }

        private static bool IsIOperandType(ITypeSymbol type)
        {
            // Treat only explicit IOperand parameter types as IOperand.
            // Previously we also returned true for IVariable (which implements IOperand),
            // causing overloads that expect IVariable to be misclassified as IOperand overloads.
            return type.Name == "IOperand";
        }

        private static bool IsIVariableType(ITypeSymbol type)
        {
            return type.Name == "IVariable" ||
                   type.AllInterfaces.Any(i => i.Name == "IVariable");
        }

        private static bool NeedsVariableTypeDispatch(List<OverloadInfo> overloads)
        {
            // Check if we have multiple overloads where the only difference is IVariable vs SoftGlobal parameter types
            if (overloads.Count < 2) return false;

            // Look for overloads that have the same signature except for variable parameter types
            var firstOverload = overloads[0];
            var firstParams = firstOverload.Method.MethodSymbol.Parameters.Skip(1).Where(p => !HasDataAttribute(p)).ToArray();

            foreach (var otherOverload in overloads.Skip(1))
            {
                var otherParams = otherOverload.Method.MethodSymbol.Parameters.Skip(1).Where(p => !HasDataAttribute(p)).ToArray();

                if (firstParams.Length != otherParams.Length) continue;

                // Check if they differ only in variable type (IVariable vs SoftGlobal)
                bool onlyVariableTypeDifference = true;
                bool hasVariableTypeDifference = false;

                for (int i = 0; i < firstParams.Length; i++)
                {
                    var firstType = firstParams[i].Type;
                    var otherType = otherParams[i].Type;

                    if (SymbolEqualityComparer.Default.Equals(firstType, otherType))
                    {
                        continue; // Same type, no difference
                    }

                    // Check if this is a variable type difference (IVariable vs SoftGlobal, IOperand with [Variable] vs SoftGlobal, IVariable vs IOperand with [Variable], or Variable vs Plain IOperand)
                    bool firstIsVariable = IsIVariableType(firstType) || (IsIOperandType(firstType) && HasVariableAttribute(firstParams[i]));
                    bool firstIsSoftGlobal = firstType.Name == "SoftGlobal";
                    bool firstIsPlainOperand = IsIOperandType(firstType) && !HasVariableAttribute(firstParams[i]);
                    bool otherIsVariable = IsIVariableType(otherType) || (IsIOperandType(otherType) && HasVariableAttribute(otherParams[i]));
                    bool otherIsSoftGlobal = otherType.Name == "SoftGlobal";
                    bool otherIsPlainOperand = IsIOperandType(otherType) && !HasVariableAttribute(otherParams[i]);

                    bool isVariableDifference =
                        (firstIsVariable && otherIsSoftGlobal) ||
                        (firstIsSoftGlobal && otherIsVariable) ||
                        // Handle IVariable vs IOperand with [Variable] - both are variable types but need different handling
                        (IsIVariableType(firstType) && IsIOperandType(otherType) && HasVariableAttribute(otherParams[i])) ||
                        (IsIOperandType(firstType) && HasVariableAttribute(firstParams[i]) && IsIVariableType(otherType)) ||
                        // Handle Variable vs Plain Operand - variable reference vs operand expression
                        (firstIsVariable && otherIsPlainOperand) ||
                        (firstIsPlainOperand && otherIsVariable) ||
                        (firstIsSoftGlobal && otherIsPlainOperand) ||
                        (firstIsPlainOperand && otherIsSoftGlobal);

                    if (isVariableDifference)
                    {
                        hasVariableTypeDifference = true;
                    }
                    else
                    {
                        onlyVariableTypeDifference = false;
                        break; // Different types other than variable types
                    }
                }

                if (onlyVariableTypeDifference && hasVariableTypeDifference)
                {
                    return true;
                }
            }

            return false;
        }

        private static void GenerateVariableTypeDispatch(IndentedStringBuilder sb, OverloadGroup group)
        {
            // Handle overloads that differ by variable parameter types
            // Note: Use inline macro unwrapping to avoid variable conflicts

            // Look for IVariable overload
            var iVariableOverload = group.Overloads.FirstOrDefault(o =>
                o.Method.MethodSymbol.Parameters.Skip(1).Where(p => !HasDataAttribute(p))
                    .Any(p => IsIVariableType(p.Type)));

            // Look for IOperand with [Variable] overload
            var iOperandVariableOverload = group.Overloads.FirstOrDefault(o =>
                o.Method.MethodSymbol.Parameters.Skip(1).Where(p => !HasDataAttribute(p))
                    .Any(p => IsIOperandType(p.Type) && HasVariableAttribute(p)));

            // Look for SoftGlobal overload
            var softGlobalOverload = group.Overloads.FirstOrDefault(o =>
                o.Method.MethodSymbol.Parameters.Skip(1).Where(p => !HasDataAttribute(p))
                    .Any(p => p.Type.Name == "SoftGlobal"));

            // Look for plain IOperand overload (without [Variable])
            var plainOperandOverload = group.Overloads.FirstOrDefault(o =>
                o.Method.MethodSymbol.Parameters.Skip(1).Where(p => !HasDataAttribute(p))
                    .Any(p => IsIOperandType(p.Type) && !HasVariableAttribute(p)));

            // Determine what kind of dispatch we need
            if (iVariableOverload != null && iOperandVariableOverload != null)
            {
                // IVariable vs IOperand with [Variable] - both are hard variables, dispatch based on variable type
                GenerateIVariableVsIOperandDispatch(sb, group, iVariableOverload, iOperandVariableOverload);
            }
            else if ((iVariableOverload != null || iOperandVariableOverload != null) && softGlobalOverload != null)
            {
                // Variable vs SoftGlobal - dispatch based on IsHard vs Soft
                var variableOverload = iVariableOverload ?? iOperandVariableOverload;
                GenerateVariableVsSoftGlobalDispatch(sb, group, variableOverload!, softGlobalOverload);
            }
            else if ((iVariableOverload != null || iOperandVariableOverload != null || softGlobalOverload != null) && plainOperandOverload != null)
            {
                // Variable/SoftGlobal vs Plain Operand - dispatch between variable reference and operand expression
                var variableOverload = iVariableOverload ?? iOperandVariableOverload ?? softGlobalOverload;
                GenerateVariableVsOperandDispatch(sb, group, variableOverload!, plainOperandOverload);
            }
            else
            {
                // Fallback to simple dispatch if we can't identify the overloads properly
                GenerateSingleOverloadBody(sb, group.Overloads[0], group.CallType, group.BuiltinName ?? "");
            }
        }

        private static void GenerateIVariableVsIOperandDispatch(IndentedStringBuilder sb, OverloadGroup group, OverloadInfo iVariableOverload, OverloadInfo iOperandVariableOverload)
        {
            // Find the variable parameter index
            var varParams = iVariableOverload.Method.MethodSymbol.Parameters.Skip(1).Where(p => !HasDataAttribute(p)).ToArray();
            var variableParamIndex = -1;
            string? variableParamQuirks = null;

            for (int i = 0; i < varParams.Length; i++)
            {
                if (IsIVariableType(varParams[i].Type))
                {
                    variableParamIndex = i;

                    // Extract VariableScopeQuirks
                    var variableAttr = varParams[i].GetAttributes().FirstOrDefault(a => a.AttributeClass?.Name == "VariableAttribute");
                    if (variableAttr != null)
                    {
                        var named = variableAttr.NamedArguments.FirstOrDefault(kv => kv.Key == "VariableScopeQuirks");
                        if (named.Value.Value != null && named.Value.Type != null)
                            variableParamQuirks = GetEnumValueName(named.Value.Type, named.Value.Value);
                    }
                    break;
                }
            }

            if (variableParamIndex == -1)
            {
                GenerateSingleOverloadBody(sb, group.Overloads[0], group.CallType, group.BuiltinName ?? "");
                return;
            }

            variableParamQuirks ??= "Zilf.Compiler.Builtins.VariableScopeQuirks.None";

            // Generate runtime dispatch based on whether we have a variable or need to compile as operand
            sb.AppendLine($"var variableRef = GetVariable(c.cc, args[{variableParamIndex}], {variableParamQuirks});");
            sb.AppendLine("if (variableRef != null)");
            sb.AppendLine("{");
            sb.Indent();
            // Generate call to IVariable overload
            GenerateOverloadCall(sb, iVariableOverload, group.CallType, "variableRef.Value.Hard", variableParamIndex);
            sb.Unindent();
            sb.AppendLine("}");
            sb.AppendLine("else");
            sb.AppendLine("{");
            sb.Indent();
            // Not a variable reference — if the argument is a bare atom, this is an error; otherwise compile as operand expression
            sb.AppendLine($"if (args[{variableParamIndex}] is ZilAtom || (args[{variableParamIndex}] is ZilMacroResult zmr_check && zmr_check.Inner is ZilAtom))");
            sb.AppendLine("{");
            sb.Indent();
            GenerateErrorThrow(sb, group.BuiltinName ?? "", variableParamIndex + 1, "bare atom argument must be a variable", group.CallType);
            sb.Unindent();
            sb.AppendLine("}");
            sb.AppendLine("else");
            sb.AppendLine("{");
            sb.Indent();
            // Generate call to IOperand with [Variable] overload - compile the argument as operand
            GenerateOverloadCallWithOperandCompilation(sb, iOperandVariableOverload, group.CallType, variableParamIndex);
            sb.Unindent();
            sb.AppendLine("}");
            sb.Unindent();
            sb.AppendLine("}");
        }

        private static void GenerateVariableVsSoftGlobalDispatch(IndentedStringBuilder sb, OverloadGroup group, OverloadInfo variableOverload, OverloadInfo softGlobalOverload)
        {
            // Find the variable parameter index
            var varParams = variableOverload.Method.MethodSymbol.Parameters.Skip(1).Where(p => !HasDataAttribute(p)).ToArray();
            var variableParamIndex = -1;
            string? variableParamQuirks = null;

            for (int i = 0; i < varParams.Length; i++)
            {
                if (IsIVariableType(varParams[i].Type) || (IsIOperandType(varParams[i].Type) && HasVariableAttribute(varParams[i])))
                {
                    variableParamIndex = i;

                    // Extract VariableScopeQuirks
                    var variableAttr = varParams[i].GetAttributes().FirstOrDefault(a => a.AttributeClass?.Name == "VariableAttribute");
                    if (variableAttr != null)
                    {
                        var named = variableAttr.NamedArguments.FirstOrDefault(kv => kv.Key == "VariableScopeQuirks");
                        if (named.Value.Value != null && named.Value.Type != null)
                            variableParamQuirks = GetEnumValueName(named.Value.Type, named.Value.Value);
                    }
                    break;
                }
            }

            if (variableParamIndex == -1)
            {
                GenerateSingleOverloadBody(sb, group.Overloads[0], group.CallType, group.BuiltinName ?? "");
                return;
            }

            variableParamQuirks ??= "Zilf.Compiler.Builtins.VariableScopeQuirks.None";

            // Generate runtime dispatch based on variable type
            sb.AppendLine($"var variableRef = GetVariable(c.cc, args[{variableParamIndex}], {variableParamQuirks});");
            sb.AppendLine("if (variableRef == null)");
            sb.AppendLine("{");
            sb.Indent();

            // Check if the variable overload uses IOperand with [Variable] - if so, it supports operand expressions as fallback
            var variableParams = variableOverload.Method.MethodSymbol.Parameters.Skip(1).Where(p => !HasDataAttribute(p)).ToArray();
            var variableParam = variableParams[variableParamIndex];

            if (IsIOperandType(variableParam.Type) && HasVariableAttribute(variableParam))
            {
                // IOperand with [Variable] supports operand expressions when not a variable reference
                sb.AppendLine($"// Variable not found - check if bare atom (error) or expression (compile as operand)");
                sb.AppendLine($"if (args[{variableParamIndex}] is ZilAtom || (args[{variableParamIndex}] is ZilMacroResult zmr_check2 && zmr_check2.Inner is ZilAtom))");
                sb.AppendLine("{");
                sb.Indent();
                GenerateErrorThrow(sb, group.BuiltinName ?? "", variableParamIndex + 1, "bare atom argument must be a variable", group.CallType);
                sb.Unindent();
                sb.AppendLine("}");
                sb.AppendLine("else");
                sb.AppendLine("{");
                sb.Indent();
                sb.AppendLine("// Compile as operand expression");
                GenerateOverloadCallWithOperandCompilation(sb, variableOverload, group.CallType, variableParamIndex);
                sb.Unindent();
                sb.AppendLine("}");
            }
            else
            {
                GenerateErrorThrow(sb, group.BuiltinName ?? "", variableParamIndex + 1, "must be a variable", group.CallType);
            }

            sb.Unindent();
            sb.AppendLine("}");
            sb.AppendLine("else if (variableRef.Value.IsHard)");
            sb.AppendLine("{");
            sb.Indent();

            // Generate call to variable overload - use appropriate target based on parameter type
            if (IsIOperandType(variableParam.Type))
            {
                // For IOperand with [Variable], use .Indirect
                GenerateOverloadCall(sb, variableOverload, group.CallType, "variableRef.Value.Hard.Indirect", variableParamIndex);
            }
            else
            {
                // For IVariable, use the variable directly
                GenerateOverloadCall(sb, variableOverload, group.CallType, "variableRef.Value.Hard", variableParamIndex);
            }
            sb.Unindent();
            sb.AppendLine("}");
            sb.AppendLine("else");
            sb.AppendLine("{");
            sb.Indent();
            // Generate call to SoftGlobal overload
            GenerateOverloadCall(sb, softGlobalOverload, group.CallType, "variableRef.Value.Soft", variableParamIndex);
            sb.Unindent();
            sb.AppendLine("}");
        }

        private static void GenerateVariableVsOperandDispatch(IndentedStringBuilder sb, OverloadGroup group, OverloadInfo variableOverload, OverloadInfo operandOverload)
        {
            // Find the parameter that differs (variable vs operand)
            var varParams = variableOverload.Method.MethodSymbol.Parameters.Skip(1).Where(p => !HasDataAttribute(p)).ToArray();
            var operandParams = operandOverload.Method.MethodSymbol.Parameters.Skip(1).Where(p => !HasDataAttribute(p)).ToArray();
            var variableParamIndex = -1;
            string? variableParamQuirks = null;

            for (int i = 0; i < varParams.Length && i < operandParams.Length; i++)
            {
                var varType = varParams[i].Type;
                var operandType = operandParams[i].Type;

                bool varIsVariable = IsIVariableType(varType) || varType.Name == "SoftGlobal" || (IsIOperandType(varType) && HasVariableAttribute(varParams[i]));
                bool operandIsPlain = IsIOperandType(operandType) && !HasVariableAttribute(operandParams[i]);

                if (varIsVariable && operandIsPlain)
                {
                    variableParamIndex = i;

                    // Extract VariableScopeQuirks from variable parameter
                    var variableParam = varParams[i];
                    var variableAttr = variableParam.GetAttributes().FirstOrDefault(a => a.AttributeClass?.Name == "VariableAttribute");
                    if (variableAttr != null)
                    {
                        var named = variableAttr.NamedArguments.FirstOrDefault(kv => kv.Key == "VariableScopeQuirks");
                        if (named.Value.Value != null && named.Value.Type != null)
                            variableParamQuirks = GetEnumValueName(named.Value.Type, named.Value.Value);
                    }
                    break;
                }
            }

            if (variableParamIndex == -1)
            {
                GenerateSingleOverloadBody(sb, group.Overloads[0], group.CallType, group.BuiltinName ?? "");
                return;
            }

            variableParamQuirks ??= "Zilf.Compiler.Builtins.VariableScopeQuirks.None";

            // Generate runtime dispatch: try variable first, fallback to operand
            sb.AppendLine($"var variableRef = GetVariable(c.cc, args[{variableParamIndex}], {variableParamQuirks});");
            sb.AppendLine("if (variableRef != null)");
            sb.AppendLine("{");
            sb.Indent();

            // Check if variable overload uses IVariable, SoftGlobal, etc.
            var varParam = varParams[variableParamIndex];
            if (IsIVariableType(varParam.Type))
            {
                sb.AppendLine("if (variableRef.Value.IsHard)");
                sb.AppendLine("{");
                sb.Indent();
                GenerateOverloadCall(sb, variableOverload, group.CallType, "variableRef.Value.Hard", variableParamIndex);
                sb.Unindent();
                sb.AppendLine("}");
                sb.AppendLine("else");
                sb.AppendLine("{");
                sb.Indent();
                sb.AppendLine("// IVariable overload can't handle SoftGlobal, fallback to operand");
                GenerateOverloadCallWithOperandCompilation(sb, operandOverload, group.CallType, variableParamIndex);
                sb.Unindent();
                sb.AppendLine("}");
            }
            else if (varParam.Type.Name == "SoftGlobal")
            {
                sb.AppendLine("if (variableRef.Value.IsSoft)");
                sb.AppendLine("{");
                sb.Indent();
                GenerateOverloadCall(sb, variableOverload, group.CallType, "variableRef.Value.Soft", variableParamIndex);
                sb.Unindent();
                sb.AppendLine("}");
                sb.AppendLine("else");
                sb.AppendLine("{");
                sb.Indent();
                sb.AppendLine("// SoftGlobal overload can't handle IVariable, fallback to operand");
                GenerateOverloadCallWithOperandCompilation(sb, operandOverload, group.CallType, variableParamIndex);
                sb.Unindent();
                sb.AppendLine("}");
            }
            else
            {
                // Generic variable dispatch
                sb.AppendLine("if (variableRef.Value.IsHard)");
                sb.AppendLine("{");
                sb.Indent();
                GenerateOverloadCall(sb, variableOverload, group.CallType, "variableRef.Value.Hard", variableParamIndex);
                sb.Unindent();
                sb.AppendLine("}");
                sb.AppendLine("else");
                sb.AppendLine("{");
                sb.Indent();
                GenerateOverloadCall(sb, variableOverload, group.CallType, "variableRef.Value.Soft", variableParamIndex);
                sb.Unindent();
                sb.AppendLine("}");
            }

            sb.Unindent();
            sb.AppendLine("}");
            sb.AppendLine("else");
            sb.AppendLine("{");
            sb.Indent();
            sb.AppendLine($"// Not a variable reference — if the argument is a bare atom, this is an error; otherwise compile as operand expression");
            sb.AppendLine($"if (args[{variableParamIndex}] is ZilAtom || (args[{variableParamIndex}] is ZilMacroResult zmr_check3 && zmr_check3.Inner is ZilAtom))");
            sb.AppendLine("{");
            sb.Indent();
            GenerateErrorThrow(sb, group.BuiltinName ?? "", variableParamIndex + 1, "bare atom argument must be a variable", group.CallType);
            sb.Unindent();
            sb.AppendLine("}");
            sb.AppendLine("else");
            sb.AppendLine("{");
            sb.Indent();
            sb.AppendLine("// Not a variable reference, compile as operand expression");
            GenerateOverloadCallWithOperandCompilation(sb, operandOverload, group.CallType, variableParamIndex);
            sb.Unindent();
            sb.AppendLine("}");
            sb.Unindent();
            sb.AppendLine("}");
        }

        private static void GenerateOverloadCallWithOperandCompilation(IndentedStringBuilder sb, OverloadInfo overload, string callType, int operandParamIndex)
        {
            var method = overload.Method.MethodSymbol;
            var methodName = $"ZBuiltins.{method.Name}";
            var parameters = method.Parameters.Skip(1).Where(p => !HasDataAttribute(p)).ToArray();

            var callArgs = new List<string> { "c" };

            // Add data parameters (like BinaryOp enum values)
            foreach (var param in method.Parameters.Skip(1).Where(HasDataAttribute))
            {
                var dataAttr = param.GetAttributes().FirstOrDefault(a => a.AttributeClass?.Name == "DataAttribute");
                if (dataAttr != null && overload.Attribute.Data != null)
                {
                    if (param.Type.TypeKind == TypeKind.Enum)
                    {
                        var enumValue = GetEnumValueName(param.Type, overload.Attribute.Data);
                        callArgs.Add(enumValue);
                    }
                    else
                    {
                        callArgs.Add(overload.Attribute.Data);
                    }
                }
            }

            // Add argument parameters
            for (int i = 0; i < parameters.Length; i++)
            {
                if (i == operandParamIndex)
                {
                    // Compile the argument as an operand
                    if (callType == "ValueCall")
                    {
                        callArgs.Add($"c.cc.CompileAsOperand(c.rb, {GetUnwrappedArgExpression(i)}, c.form.SourceLine, c.resultStorage)");
                    }
                    else
                    {
                        callArgs.Add($"c.cc.CompileAsOperand(c.rb, {GetUnwrappedArgExpression(i)}, c.form.SourceLine)");
                    }
                }
                else
                {
                    // Handle other parameters
                    var paramType = parameters[i].Type;
                    if (IsIOperandType(paramType))
                    {
                        if (callType == "ValueCall")
                        {
                            callArgs.Add($"c.cc.CompileAsOperand(c.rb, {GetUnwrappedArgExpression(i)}, c.form.SourceLine, c.resultStorage)");
                        }
                        else
                        {
                            callArgs.Add($"c.cc.CompileAsOperand(c.rb, {GetUnwrappedArgExpression(i)}, c.form.SourceLine)");
                        }
                    }
                    else if (IsZilObjectType(paramType))
                    {
                        callArgs.Add(GetUnwrappedArgExpression(i));
                    }
                    else
                    {
                        // Handle other types as needed
                        callArgs.Add($"/* TODO: Convert args[{i}] to {paramType.Name} */ default({paramType.ToDisplayString()})");
                    }
                }
            }

            if (callType == "VoidCall" || callType == "PredCall")
            {
                sb.AppendLine($"{methodName}({string.Join(", ", callArgs)});");
                sb.AppendLine("return;");
            }
            else
            {
                sb.AppendLine($"return {methodName}({string.Join(", ", callArgs)});");
            }
        }

    private static void GenerateOverloadCall(IndentedStringBuilder sb, OverloadInfo overload, string callType, string variableExpression, int variableParamIndex)
        {
            var method = overload.Method.MethodSymbol;
            var methodName = method.ContainingType.ToDisplayString() + "." + method.Name;
            var parameters = method.Parameters.Skip(1).Where(p => !HasDataAttribute(p)).ToArray();

            var callArgs = new List<string> { "c" };

            // Add data parameters (like BinaryOp enum values)
            foreach (var param in method.Parameters.Skip(1).Where(HasDataAttribute))
            {
                var dataAttr = param.GetAttributes().FirstOrDefault(a => a.AttributeClass?.Name == "DataAttribute");
                if (dataAttr != null && overload.Attribute.Data != null)
                {
                    if (param.Type.TypeKind == TypeKind.Enum)
                    {
                        var enumValue = GetEnumValueName(param.Type, overload.Attribute.Data);
                        callArgs.Add(enumValue);
                    }
                    else
                    {
                        callArgs.Add(overload.Attribute.Data);
                    }
                }
            }

            // Add argument parameters
            for (int i = 0; i < parameters.Length; i++)
            {
                if (i == variableParamIndex)
                {
                    var paramType = parameters[i].Type;
                    // If the target parameter is IOperand with [Variable], we need to convert IVariable to IOperand using .Indirect
                    if (IsIOperandType(paramType) && HasVariableAttribute(parameters[i]))
                    {
                        // Convert IVariable to IOperand by getting its Indirect property
                        if (variableExpression == "variableRef.Value.Hard")
                        {
                            callArgs.Add("variableRef.Value.Hard.Indirect");
                        }
                        else
                        {
                            callArgs.Add(variableExpression);
                        }
                    }
                    else
                    {
                        callArgs.Add(variableExpression);
                    }
                }
                else
                {
                    // Generate conversion for other parameters
                    var paramType = parameters[i].Type;
                    if (IsIOperandType(paramType))
                    {
                        callArgs.Add($"c.cc.CompileAsOperand(c.rb, {GetUnwrappedArgExpression(i)}, c.form.SourceLine)");
                    }
                    else if (IsZilObjectType(paramType))
                    {
                        callArgs.Add(GetUnwrappedArgExpression(i));
                    }
                    else
                    {
                        // Handle other types as needed
                        callArgs.Add($"/* TODO: Convert {GetUnwrappedArgExpression(i)} to {paramType.Name} */ default({paramType.ToDisplayString()})");
                    }
                }
            }

            if (callType == "VoidCall" || callType == "PredCall")
            {
                sb.AppendLine($"{methodName}({string.Join(", ", callArgs)});");
                sb.AppendLine("return;");
            }
            else
            {
                sb.AppendLine($"return {methodName}({string.Join(", ", callArgs)});");
            }
        }

        private static void GenerateErrorThrow(IndentedStringBuilder sb, string operationName, int argIndex, string message, string callType)
        {
            if (callType == "VoidCall" || callType == "PredCall")
            {
                sb.AppendLine($"c.cc.Context.HandleError(new CompilerError(c.form, CompilerMessages._0_Argument_1_2, \"{operationName}\", {argIndex}, \"{message}\"));");
                sb.AppendLine("return;");
            }
            else
            {
                sb.AppendLine($"throw new CompilerError(c.form, CompilerMessages._0_Argument_1_2, \"{operationName}\", {argIndex}, \"{message}\");");
            }
        }

        private static void GenerateHasSideEffectsMethod(IndentedStringBuilder sb, List<OverloadGroup> overloadGroups)
        {
            sb.AppendLine("/// <summary>");
            sb.AppendLine("/// Checks if a builtin has side effects based on the HasSideEffect attribute.");
            sb.AppendLine("/// Generated from all [Builtin] attributes with HasSideEffect = true.");
            sb.AppendLine("/// </summary>");
            sb.AppendLine("internal static bool HasSideEffects(string name)");
            sb.AppendLine("{");
            sb.Indent();

            // Collect all unique builtin names that have side effects
            var sideEffectBuiltins = new HashSet<string>();
            foreach (var group in overloadGroups)
            {
                var cleanName = group.BuiltinName?.Trim() ?? "";
                if (string.IsNullOrWhiteSpace(cleanName)) continue;

                // Check if any overload in this group has side effects
                bool hasSideEffects = group.Overloads.Any(overload => overload.Attribute.HasSideEffect);
                if (hasSideEffects)
                {
                    sideEffectBuiltins.Add(cleanName);
                }
            }

            if (sideEffectBuiltins.Count > 0)
            {
                sb.AppendLine("switch (name)");
                sb.AppendLine("{");
                sb.Indent();

                // Sort the builtin names for consistent output
                var sortedBuiltins = sideEffectBuiltins.OrderBy(name => name).ToList();
                foreach (var builtin in sortedBuiltins)
                {
                    sb.AppendLine($"case \"{builtin}\":");
                }

                sb.Indent();
                sb.AppendLine("return true;");
                sb.Unindent();
                sb.AppendLine();
                sb.AppendLine("default:");
                sb.Indent();
                sb.AppendLine("return false;");
                sb.Unindent();
                sb.Unindent();
                sb.AppendLine("}");
            }
            else
            {
                sb.AppendLine("return false;");
            }

            sb.Unindent();
            sb.AppendLine("}");
        }
    }

    // Data structures for method information
    public class BuiltinMethodInfo
    {
        public MethodDeclarationSyntax Method { get; set; } = null!;
        public IMethodSymbol MethodSymbol { get; set; } = null!;
        public BuiltinAttributeInfo[] AttributeInfos { get; set; } = null!;
    }

    public class BuiltinAttributeInfo
    {
        public string Name { get; set; } = "";
        public string? Data { get; set; }
        public int? MinVersion { get; set; }
        public int? MaxVersion { get; set; }
        public bool HasSideEffect { get; set; }
        public int Priority { get; set; } = 1;
    }

    public class ParameterInfo
    {
        public List<IParameterSymbol> DataParameters { get; set; } = [];
        public List<IParameterSymbol> ArgumentParameters { get; set; } = [];
        public int RequiredArgumentCount { get; set; } = 0;
        public int OptionalArgumentCount { get; set; } = 0;
        public bool HasParamsArray { get; set; } = false;
    }

    public class ArgumentCountGroup
    {
        public int Key { get; set; }
        public List<OverloadInfo> Overloads { get; set; } = [];
    }

    public class OverloadGroup
    {
        public string BuiltinName { get; set; } = "";
        public string CallType { get; set; } = "";
        public List<OverloadInfo> Overloads { get; set; } = [];
    }

    public class OverloadInfo
    {
        public BuiltinMethodInfo Method { get; set; } = null!;
        public BuiltinAttributeInfo Attribute { get; set; } = null!;
    }
}

