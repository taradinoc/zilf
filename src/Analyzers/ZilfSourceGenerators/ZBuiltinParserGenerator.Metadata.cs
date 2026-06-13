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

using System;
using System.Collections.Generic;
using System.Linq;

namespace ZilfSourceGenerators
{
    public partial class ZBuiltinParserGenerator
    {
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
                var platformCondition = info.Attribute.Platform switch
                {
                    BuiltinPlatformSetting.ZMachine => "currentPlatform == BuiltinPlatform.ZMachine",
                    BuiltinPlatformSetting.Glulx => "currentPlatform == BuiltinPlatform.Glulx",
                    BuiltinPlatformSetting.Cornerstone => "currentPlatform == BuiltinPlatform.Cornerstone",
                    _ => "true"
                };

                // Combine conditions for this overload
                var conditionParts = new List<string>();
                if (platformCondition != "true") conditionParts.Add(platformCondition);
                if (versionCondition != "true") conditionParts.Add(versionCondition);
                if (argCountCondition != "true") conditionParts.Add(argCountCondition);

                string combinedCondition;
                if (conditionParts.Count == 0)
                {
                    combinedCondition = "true";
                }
                else if (conditionParts.Count == 1)
                {
                    combinedCondition = conditionParts[0];
                }
                else
                {
                    combinedCondition = $"({string.Join(" && ", conditionParts)})";
                }

                conditions.Add(combinedCondition);
            }

            // Generate lambda that returns true if ANY overload matches
            if (conditions.Count == 0)
            {
                return "(zversion, argCount, currentPlatform) => false";
            }
            else if (conditions.Count == 1)
            {
                return $"(zversion, argCount, currentPlatform) => {conditions[0]}";
            }
            else
            {
                var combined = string.Join(" || ", conditions);
                return $"(zversion, argCount, currentPlatform) => ({combined})";
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
                sb.AppendLine("internal static readonly System.Collections.Generic.Dictionary<string, (System.Action<Zilf.Compiler.Builtins.VoidCall, Zilf.Interpreter.Values.ZilObject[]> Parser, System.Func<int, int, BuiltinPlatform, bool> SupportsCall)> VoidCallParsers = new() {");
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
                sb.AppendLine("internal static readonly System.Collections.Generic.Dictionary<string, (System.Func<Zilf.Compiler.Builtins.ValueCall, Zilf.Interpreter.Values.ZilObject[], Zilf.Emit.IOperand> Parser, System.Func<int, int, BuiltinPlatform, bool> SupportsCall)> ValueCallParsers = new() {");
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
                sb.AppendLine("internal static readonly System.Collections.Generic.Dictionary<string, (System.Action<Zilf.Compiler.Builtins.PredCall, Zilf.Interpreter.Values.ZilObject[]> Parser, System.Func<int, int, BuiltinPlatform, bool> SupportsCall)> PredCallParsers = new() {");
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
                sb.AppendLine("internal static readonly System.Collections.Generic.Dictionary<string, (System.Func<Zilf.Compiler.Builtins.ValuePredCall, Zilf.Interpreter.Values.ZilObject[], Zilf.Emit.IOperand> Parser, System.Func<int, int, BuiltinPlatform, bool> SupportsCall)> ValuePredCallParsers = new() {");
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
                    var argsParams = overload.Method.ArgumentParameters;
                    int requiredCount = 0;
                    int optionalCount = 0;
                    bool hasParams = false;
                    foreach (var p in argsParams)
                    {
                        if (p.IsParams)
                        {
                            hasParams = true;
                            continue;
                        }
                        if (p.IsOptional)
                            optionalCount++;
                        else
                            requiredCount++;
                    }
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
                        var methodSymbol = ov.Method.MethodSymbol;

                        // Extract XML documentation summaries from method.
                        // For builtins, prefer BuiltinAttribute.Summary over XML doc summary, since
                        // the same method often implements multiple operations (e.g., TernaryVoidOp
                        // implements DCLEAR, DIROUT, DISPLAY, etc. with different [Builtin] attributes).
                        // Parameter summaries always come from XML <param> elements.
                        var methodSummary = XmlDocHelper.ExtractSummary(methodSymbol);
                        var paramSummaries = XmlDocHelper.ExtractParamSummaries(methodSymbol);

                        var argsParams = ov.Method.ArgumentParameters;
                        int requiredCount = 0;
                        int optionalCount = 0;
                        bool hasParams = false;
                        foreach (var p in argsParams)
                        {
                            if (p.IsParams)
                            {
                                hasParams = true;
                                continue;
                            }
                            if (p.IsOptional)
                                optionalCount++;
                            else
                                requiredCount++;
                        }
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

                            // Add parameter summary if available
                            if (paramSummaries.TryGetValue(p.Name ?? "", out var paramSummary))
                            {
                                var escapedSummary = paramSummary.Replace("\"", "\\\"");
                                paramExpr = $"{paramExpr}.WithSummary(\"{escapedSummary}\")";
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
                        ob.AppendLine($"maxVersion: {maxVersionExpr},");

                        // Determine summary: prefer attribute Summary over XML doc summary
                        var chosenSummary = !string.IsNullOrWhiteSpace(ov.Attribute.Summary) ? ov.Attribute.Summary : methodSummary;
                        var escapedMethodSummary = chosenSummary != null ? $"\"{chosenSummary.Replace("\"", "\\\"")}\"" : "null";
                        var platformExpr = GetPlatformLiteral(ov.Attribute.Platform);
                        ob.AppendLine($"summary: {escapedMethodSummary},");
                        ob.AppendLine($"platform: {platformExpr}),");
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

        private static string GetPlatformLiteral(BuiltinPlatformSetting platform)
        {
            return platform switch
            {
                BuiltinPlatformSetting.ZMachine => "BuiltinPlatform.ZMachine",
                BuiltinPlatformSetting.Glulx => "BuiltinPlatform.Glulx",
                BuiltinPlatformSetting.Cornerstone => "BuiltinPlatform.Cornerstone",
                _ => "BuiltinPlatform.Any",
            };
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
}
