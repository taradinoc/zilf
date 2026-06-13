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
    /// Source generator for SUBR and FSUBR (interpreter command) argument parsers.
    /// Uses step-based tree generation inspired by ArgDecoder architecture.
    /// </summary>
    [Generator]
    public class SubrParserGenerator : IIncrementalGenerator
    {
        public void Initialize(IncrementalGeneratorInitializationContext context)
        {
            // Find methods with Subr or FSubr attributes
            var subrProvider = context.SyntaxProvider
                .ForAttributeWithMetadataName("Zilf.Interpreter.Subrs+SubrAttribute",
                    predicate: static (node, _) => node is MethodDeclarationSyntax,
                    transform: static (context, _) => GetSubrMethodInfo(context));
            var fsubrProvider = context.SyntaxProvider
                .ForAttributeWithMetadataName("Zilf.Interpreter.Subrs+FSubrAttribute",
                    predicate: static (node, _) => node is MethodDeclarationSyntax,
                    transform: static (context, _) => GetSubrMethodInfo(context));

            // Merge the two streams in a way compatible with older Roslyn incremental APIs.
            // This collects each provider once and combines the results into a single immutable array.
            var subrMethods = subrProvider.Collect()
                .Combine(fsubrProvider.Collect())
                .Select(static (t, _) => t.Left.AddRange(t.Right));

            // Generate parsers for Subrs
            var compilationAndSubrs = context.CompilationProvider.Combine(subrMethods);
            context.RegisterSourceOutput(compilationAndSubrs, static (spc, source) =>
            {
                var nonNullMethods = source.Right
                    .Where(static m => m != null)
                    .Select(static m => m!)
                    .ToImmutableArray();
                var debugLog = new List<string>();
                GenerateSubrParsers(spc, source.Left, nonNullMethods, debugLog);
                GenerateSubrSignatureMetadata(spc, source.Left, nonNullMethods);
            });
        }

        /// <summary>
        /// Generates signature metadata for all SUBR/FSUBR methods, similar to ZBuiltinParserGenerator.
        /// </summary>
        private static void GenerateSubrSignatureMetadata(SourceProductionContext context, Compilation compilation, ImmutableArray<SubrMethodInfo> methods)
        {
            if (methods.Length == 0) return;

            // Cache key type symbols for fast SymbolEqualityComparer checks.
            var applicableType = compilation.GetTypeByMetadataName("Zilf.Interpreter.IApplicable");
            var localEnvironmentType = compilation.GetTypeByMetadataName("Zilf.Interpreter.LocalEnvironment");
            var structureType = compilation.GetTypeByMetadataName("Zilf.Interpreter.IStructure");
            var zilObjectType = compilation.GetTypeByMetadataName("Zilf.Interpreter.Values.ZilObject");

            static bool IsInInterpreterNamespace(ITypeSymbol t)
            {
                var ns = t.ContainingNamespace;
                while (ns != null && !ns.IsGlobalNamespace)
                {
                    if (ns.Name == "Interpreter" && ns.ContainingNamespace?.Name == "Zilf")
                        return true;
                    ns = ns.ContainingNamespace;
                }
                return false;
            }

            static bool InheritsFromOrEquals(ITypeSymbol t, INamedTypeSymbol baseType)
            {
                // Walk base types for named types; for other symbols, just compare directly.
                if (t is INamedTypeSymbol named)
                {
                    for (INamedTypeSymbol? cur = named; cur != null; cur = cur.BaseType)
                    {
                        if (SymbolEqualityComparer.Default.Equals(cur, baseType))
                            return true;
                    }
                    return false;
                }
                return SymbolEqualityComparer.Default.Equals(t, baseType);
            }

            var sb = new IndentedStringBuilder();
            sb.AppendLine("/* This file is part of ZILF. Generated SUBR signature metadata - do not edit. */");
            sb.AppendLine();
            sb.AppendLine("#nullable enable");
            sb.AppendLine();
            sb.AppendLine("using System;");
            sb.AppendLine("using System.Collections.Generic;");
            sb.AppendLine("using Zilf.Interpreter;");
            sb.AppendLine("using Zilf.Language.Signatures;");
            sb.AppendLine("namespace Zilf.Interpreter");
            sb.AppendLine("{");
            sb.Indent();
            sb.AppendLine("internal static partial class GeneratedSubrSignatureMetadata");
            sb.AppendLine("{");
            sb.Indent();
            sb.AppendLine("internal static readonly IReadOnlyDictionary<string, ISignature[]> SubrSignatures = new Dictionary<string, ISignature[]>");
            sb.AppendLine("{");
            sb.Indent();

            // Group signatures by SUBR name
            var grouped = methods
                .SelectMany(m => m.AttributeInfos.Select(attr => (attr, m)))
                .GroupBy(x => x.attr.Name ?? "")
                .OrderBy(g => g.Key, StringComparer.OrdinalIgnoreCase);

            foreach (var group in grouped)
            {
                var name = group.Key;
                sb.AppendLine($"[\"{name}\"] = new ISignature[] {{");
                sb.Indent();
                foreach (var (attr, method) in group)
                {
                    var parameters = method.MethodSymbol.Parameters.Skip(1).ToArray(); // skip Context

                    // Extract XML documentation summaries
                    var methodSummary = XmlDocHelper.ExtractSummary(method.MethodSymbol);
                    var paramSummaries = XmlDocHelper.ExtractParamSummaries(method.MethodSymbol);

                    // Local helpers to inspect parameter types
                    bool IsZilObjectType(ITypeSymbol t)
                    {
                        if (structureType != null && SymbolEqualityComparer.Default.Equals(t, structureType))
                            return true;
                        if (zilObjectType != null && InheritsFromOrEquals(t, zilObjectType))
                            return true;

                        // Preserve prior behavior: treat most types in Zilf.Interpreter.* as “any object”.
                        return IsInInterpreterNamespace(t);
                    }

                    bool IsApplicableType(ITypeSymbol t)
                    {
                        if (applicableType != null && SymbolEqualityComparer.Default.Equals(t, applicableType))
                            return true;
                        return t.Name == "IApplicable" && IsInInterpreterNamespace(t);
                    }

                    static bool TryPrimTypeFor(ITypeSymbol t, out string primName)
                    {
                        primName = "";
                        return t.SpecialType switch
                        {
                            SpecialType.System_Int32 => (primName = "FIX") != null,
                            SpecialType.System_String => (primName = "STRING") != null,
                            _ => false,
                        };
                    }

                    bool TryStdAtomFor(ITypeSymbol t, out string atomName)
                    {
                        atomName = "";
                        if (localEnvironmentType != null && SymbolEqualityComparer.Default.Equals(t, localEnvironmentType))
                            return (atomName = "ENVIRONMENT") != null;
                        return t.Name == "LocalEnvironment" && IsInInterpreterNamespace(t) && (atomName = "ENVIRONMENT") != null;
                    }

                    // Build expressions for each parameter using SignatureBuilder helpers
                    var partExprs = new List<string>();
                    // Helper: prefer ParamDescAttribute on parameter, then on the parameter type (or array element type),
                    // then EitherAttribute.DefaultParamDesc (named argument), then fall back to the parameter name.
                    static string GetParamDescription(IParameterSymbol p)
                    {
                        static bool IsParamDescAttribute(INamedTypeSymbol cls) =>
                            cls.Name == "ParamDescAttribute" &&
                            cls.ContainingNamespace is { Name: "Interpreter", ContainingNamespace: { Name: "Zilf" } };

                        // 1) ParamDescAttribute on the parameter
                        foreach (var a in p.GetAttributes())
                        {
                            var cls = a.AttributeClass;
                            if (cls == null) continue;
                            if (IsParamDescAttribute(cls))
                            {
                                if (a.ConstructorArguments.Length > 0 && a.ConstructorArguments[0].Value is string s && !string.IsNullOrWhiteSpace(s))
                                    return s;
                            }
                        }

                        // 2) ParamDescAttribute on the parameter's type (or array element type)
                        var t = p.Type;
                        if (t is IArrayTypeSymbol arr)
                            t = arr.ElementType;

                        foreach (var a in t.GetAttributes())
                        {
                            var cls = a.AttributeClass;
                            if (cls == null) continue;
                            if (IsParamDescAttribute(cls))
                            {
                                if (a.ConstructorArguments.Length > 0 && a.ConstructorArguments[0].Value is string s && !string.IsNullOrWhiteSpace(s))
                                    return s;
                            }
                        }

                        // 3) EitherAttribute.DefaultParamDesc (named argument)
                        foreach (var a in p.GetAttributes())
                        {
                            var cls = a.AttributeClass;
                            if (cls == null) continue;
                            if (cls.Name == "EitherAttribute")
                            {
                                // look for named argument DefaultParamDesc
                                foreach (var kv in a.NamedArguments)
                                {
                                    if (kv.Key == "DefaultParamDesc" && kv.Value.Value is string s && !string.IsNullOrWhiteSpace(s))
                                        return s;
                                }
                            }
                        }

                        return p.Name ?? "arg";
                    }

                    foreach (var p in parameters)
                    {
                        var paramName = GetParamDescription(p);
                        // Start with a basic identifier part
                        var innerExpr = $"SignatureBuilder.Identifier(\"{paramName}\")";

                        // Determine type-based constraint expression
                        var type = p.Type;
                        string? constrainedInner = null;

                        // Arrays represent varargs in many cases; get element type if so
                        if (type is IArrayTypeSymbol arrType)
                        {
                            var elem = arrType.ElementType;
                            // Map element primitive/type
                            if (IsZilObjectType(elem))
                            {
                                // e.g., ZilObject[] -> no further constraint (AnyObject)
                                constrainedInner = innerExpr;
                            }
                            else if (IsApplicableType(elem))
                            {
                                constrainedInner = $"SignatureBuilder.Constrained({innerExpr}, Constraint.Applicable)";
                            }
                            else if (TryPrimTypeFor(elem, out var prim))
                            {
                                constrainedInner = $"SignatureBuilder.Constrained({innerExpr}, Constraint.OfPrimType(PrimType.{prim}))";
                            }
                            else if (TryStdAtomFor(elem, out var atomName))
                            {
                                constrainedInner = $"SignatureBuilder.Constrained({innerExpr}, Constraint.OfType(StdAtom.{atomName}))";
                            }
                            else
                            {
                                constrainedInner = innerExpr;
                            }
                        }
                        else
                        {
                            // Non-array parameter
                            if (IsZilObjectType(type))
                            {
                                constrainedInner = innerExpr;
                            }
                            else if (IsApplicableType(type))
                            {
                                constrainedInner = $"SignatureBuilder.Constrained({innerExpr}, Constraint.Applicable)";
                            }
                            else if (TryPrimTypeFor(type, out var prim))
                            {
                                constrainedInner = $"SignatureBuilder.Constrained({innerExpr}, Constraint.OfPrimType(PrimType.{prim}))";
                            }
                            else if (TryStdAtomFor(type, out var atomName2))
                            {
                                constrainedInner = $"SignatureBuilder.Constrained({innerExpr}, Constraint.OfType(StdAtom.{atomName2}))";
                            }
                            else
                            {
                                constrainedInner = innerExpr;
                            }
                        }

                        string paramExpr = constrainedInner;

                        if (p.IsParams)
                        {
                            // VarArgs around the inner constrained part
                            paramExpr = $"SignatureBuilder.VarArgs({paramExpr}, false)";
                        }
                        else if (p.IsOptional)
                        {
                            paramExpr = $"SignatureBuilder.Optional({paramExpr})";
                        }

                        // Add parameter summary if available
                        if (paramSummaries.TryGetValue(p.Name ?? "", out var paramSummary))
                        {
                            var escapedSummary = paramSummary.Replace("\"", "\\\"");
                            paramExpr = $"{paramExpr}.WithSummary(\"{escapedSummary}\")";
                        }

                        partExprs.Add(paramExpr);
                    }

                    // Emit an ISignaturePart[] initializer (with type-based constraints) and construct SubrSignature via factory
                    var escapedMethodSummary = methodSummary != null ? $"\"{methodSummary.Replace("\"", "\\\"")}\"" : "null";
                    sb.AppendLine("SubrSignature.FromGeneratedParts(new ISignaturePart[] {");
                    sb.Indent();
                    for (int i = 0; i < partExprs.Count; i++)
                    {
                        var comma = i < partExprs.Count - 1 ? "," : "";
                        sb.AppendLine(partExprs[i] + comma);
                    }
                    sb.Unindent();
                    sb.AppendLine($"}}, {escapedMethodSummary}),");
                }
                sb.Unindent();
                sb.AppendLine("},");
            }

            sb.Unindent();
            sb.AppendLine("};");
            sb.AppendLine();
            sb.Unindent();
            sb.AppendLine("}");
            sb.Unindent();
            sb.AppendLine("}");

            context.AddSource("GeneratedSubrSignatureMetadata.g.cs", SourceText.From(sb.ToString(), Encoding.UTF8));
        }

        private static SubrMethodInfo? GetSubrMethodInfo(GeneratorAttributeSyntaxContext context)
        {
            var method = (MethodDeclarationSyntax)context.TargetNode;

            if (context.TargetSymbol is not IMethodSymbol methodSymbol) return null;

            // Extract Subr/FSubr attribute information
            var subrAttrs = GetSubrAttributes(methodSymbol);
            if (subrAttrs.Length == 0) return null;

            // Extract MdlZilRedirect attribute information
            var mdlZilRedirect = GetMdlZilRedirectAttribute(methodSymbol);

            return new SubrMethodInfo
            {
                Method = method,
                MethodSymbol = methodSymbol,
                AttributeInfos = subrAttrs,
                MdlZilRedirect = mdlZilRedirect
            };
        }

        private static SubrAttributeInfo[] GetSubrAttributes(IMethodSymbol methodSymbol)
        {
            return [.. methodSymbol.GetAttributes()
                .Where(attr => attr.AttributeClass?.Name is "SubrAttribute" or "FSubrAttribute")
                .Select(attr =>
                {
                    var name = (attr.ConstructorArguments.FirstOrDefault().Value as string) ?? methodSymbol.Name;
                    var obList = attr.NamedArguments.FirstOrDefault(na => na.Key == "ObList").Value.Value as string ?? "";
                    var isFSubr = attr.AttributeClass?.Name == "FSubrAttribute";

                    return new SubrAttributeInfo
                    {
                        Name = name,
                        IsFSubr = isFSubr,
                        ObList = obList
                    };
                })];
        }

        private static MdlZilRedirectInfo? GetMdlZilRedirectAttribute(IMethodSymbol methodSymbol)
        {
            var redirectAttr = methodSymbol.GetAttributes().FirstOrDefault(attr =>
                attr.AttributeClass?.Name == "MdlZilRedirectAttribute");

            if (redirectAttr == null || redirectAttr.ConstructorArguments.Length < 2)
            {
                return null;
            }

            if (redirectAttr.ConstructorArguments[0].Value is INamedTypeSymbol typeSymbol &&
                redirectAttr.ConstructorArguments[1].Value is string targetMethod)
            {
                var topLevelOnly = redirectAttr.NamedArguments
                    .FirstOrDefault(na => na.Key == "TopLevelOnly").Value.Value as bool? ?? false;

                return new MdlZilRedirectInfo
                {
                    TargetType = typeSymbol.Name,
                    TargetMethod = targetMethod,
                    TopLevelOnly = topLevelOnly
                };
            }

            return null;
        }

        private static void GenerateSubrParsers(SourceProductionContext context, Compilation compilation, ImmutableArray<SubrMethodInfo> methods, List<string> debugLog)
        {
            if (methods.Length == 0) return;

            var sb = new IndentedStringBuilder();
            sb.AppendLine("/* This file is part of ZILF. Generated code - do not edit. */");
            sb.AppendLine();
            // Generated code uses nullable reference annotations; enable nullable context explicitly
            sb.AppendLine("#nullable enable"); // Enable nullable context
            sb.AppendLine();
            sb.AppendLine("using System;");
            sb.AppendLine("using System.Collections.Generic;");
            sb.AppendLine("using System.Linq;");
            sb.AppendLine("using Zilf.Interpreter;");
            sb.AppendLine("using Zilf.Interpreter.Values;");
            sb.AppendLine("using Zilf.Language;");
            sb.AppendLine("using Zilf.Common;");
            sb.AppendLine("using Zilf.Diagnostics;");
            sb.AppendLine();
            sb.AppendLine("namespace Zilf.Interpreter");

            sb.AppendLine("{");
            sb.Indent();
            sb.AppendLine("internal static partial class GeneratedSubrParsers");
            sb.AppendLine("{");
            sb.Indent();

            // Build a mapping from each method to a canonical parser name and its aliases
            var methodToParserName = new Dictionary<IMethodSymbol, string>(SymbolEqualityComparer.Default);
            var methodToAliases = new Dictionary<IMethodSymbol, List<SubrAttributeInfo>>(SymbolEqualityComparer.Default);

            foreach (var method in methods)
            {
                if (!methodToAliases.ContainsKey(method.MethodSymbol))
                {
                    methodToAliases[method.MethodSymbol] = new List<SubrAttributeInfo>();

                    // Use the first valid alias name to generate the canonical parser name
                    string? canonicalName = null;
                    foreach (var attrInfo in method.AttributeInfos)
                    {
                        var cleanName = attrInfo.Name?.Trim() ?? "";
                        if (!string.IsNullOrWhiteSpace(cleanName) && cleanName.Length > 0 && cleanName != "_")
                        {
                            var sanitizedName = SanitizeName(cleanName);
                            if (sanitizedName.Length > 0)
                            {
                                canonicalName = sanitizedName;
                                break;
                            }
                        }
                    }

                    if (!string.IsNullOrEmpty(canonicalName))
                    {
                        methodToParserName[method.MethodSymbol] = $"Generated_{canonicalName}_Parser";
                    }
                }

                // Add all attributes of this method to its alias list
                methodToAliases[method.MethodSymbol].AddRange(method.AttributeInfos);
            }

            // First pass: determine which parser names will be generated
            var parserNamesToGenerate = new HashSet<string>(methodToParserName.Values);

            var generatedParsers = new HashSet<string>();
            var skippedParsers = new HashSet<string>();

            // Clear global helpers for this generation run
            TreeBasedGenerator.ClearGlobalHelpers();

            // Second pass: generate one parser per unique method
            foreach (var method in methods)
            {
                if (methodToParserName.TryGetValue(method.MethodSymbol, out var parserName))
                {
                    // Use the first attribute as representative for parser generation
                    var firstAttr = method.AttributeInfos.FirstOrDefault();
                    if (firstAttr != null)
                    {
                        GenerateSubrParser(sb, method, firstAttr, parserName, generatedParsers, skippedParsers, parserNamesToGenerate, compilation, debugLog);
                    }
                }
            }

            // Generate parser dictionaries
            GenerateParserDictionaries(sb, methods, methodToParserName, methodToAliases, generatedParsers);

            sb.Unindent();
            sb.AppendLine("}");
            sb.Unindent();
            sb.AppendLine("}");

            // Dump debug log
            if (debugLog.Count > 0)
            {
                sb.AppendLine();
                foreach (var line in debugLog)
                {
                    sb.Append("// ");
                    sb.AppendLine(line);
                }
            }

            context.AddSource("GeneratedSubrParsers.g.cs", SourceText.From(sb.ToString(), Encoding.UTF8));
        }

        private static void GenerateSubrParser(IndentedStringBuilder sb, SubrMethodInfo method,
            SubrAttributeInfo attrInfo, string parserName, HashSet<string> generatedParsers, HashSet<string> skippedParsers, 
            HashSet<string> allParserNames, Compilation compilation, List<string> debugLog)
        {
            // Skip if already generated
            if (generatedParsers.Contains(parserName))
            {
                return;
            }

            // Skip if already processed (prevents duplicate TODO comments)
            if (skippedParsers.Contains(parserName))
            {
                return;
            }

            // For methods with MdlZilRedirect, if target parser exists, insert redirect clause at top of parser
            string? redirectClause = null;
            if (method.MdlZilRedirect != null)
            {
                var target = method.MdlZilRedirect;
                string targetParserName = $"Generated_{SanitizeName(target.TargetMethod)}_Parser";
                if (allParserNames.Contains(targetParserName))
                {
                    // Use 'context' to match the generated parser's parameter name
                    redirectClause = $"if ((context.CurrentFile.Flags & FileFlags.MdlZil) != 0" + (target.TopLevelOnly ? " && context.AtTopLevel" : "") + $")\n    return {targetParserName}(name, context, args);";
                }
                else
                {
                    sb.AppendLine($"// TODO: {parserName} - requires support for: MdlZilRedirect (target parser not yet generated)");
                    skippedParsers.Add(parserName);
                    return;
                }
            }

            // Get method parameters (skip Context which is always first)
            var parameters = method.MethodSymbol.Parameters.Skip(1).ToArray();

            // Only add to generatedParsers if we're actually going to generate the parser
            generatedParsers.Add(parserName);

            // Use TreeBasedGenerator approach
            try
            {
                var treeBuilder = new ParameterTreeBuilder(/*debugLog*/);
                var parameterTree = treeBuilder.BuildTree(parameters);
                var generator = new TreeBasedGenerator();
                // Pass the full SubrMethodInfo so the generator can access the original MethodDeclarationSyntax
                generator.GenerateParser(sb, method, parameterTree, parserName, redirectClause, compilation, debugLog);
            }
            catch (Exception ex)
            {
                sb.AppendLine($"// TODO: {parserName} - {ex.Message}");
                sb.AppendLine($"throw new NotImplementedException(\"Parser generation failed: {ex.Message}\");");
                sb.Unindent();
                sb.AppendLine("}");
                sb.AppendLine();
                skippedParsers.Add(parserName);
            }
        }

        private static void GenerateParserDictionaries(IndentedStringBuilder sb, ImmutableArray<SubrMethodInfo> methods,
            Dictionary<IMethodSymbol, string> methodToParserName, Dictionary<IMethodSymbol, List<SubrAttributeInfo>> methodToAliases,
            HashSet<string> generatedParsers)
        {
            sb.AppendLine();
            sb.AppendLine("// Dictionary of generated SUBR parsers");
            sb.AppendLine("internal static readonly Dictionary<string, SubrDelegate> SubrParsers = new()");
            sb.AppendLine("{");
            sb.Indent();

            var addedNames = new HashSet<string>();
            var lines = new List<string>();

            // For each unique method, add all its non-FSubr aliases to the dictionary
            foreach (var kvp in methodToParserName)
            {
                var methodSymbol = kvp.Key;
                var parserName = kvp.Value;

                if (!generatedParsers.Contains(parserName))
                    continue;

                if (methodToAliases.TryGetValue(methodSymbol, out var aliases))
                {
                    foreach (var attrInfo in aliases.Where(a => !a.IsFSubr))
                    {
                        var cleanName = attrInfo.Name?.Trim() ?? "";
                        if (!string.IsNullOrWhiteSpace(cleanName) && !addedNames.Contains(cleanName))
                        {
                            addedNames.Add(cleanName);

                            // Add parser to dictionary
                            var fullName = string.IsNullOrEmpty(attrInfo.ObList) ? cleanName : $"{cleanName}!-{attrInfo.ObList}";
                            lines.Add($"[\"{fullName}\"] = {parserName},");
                        }
                    }
                }
            }

            lines.Sort();

            foreach (var line in lines)
                sb.AppendLine(line);

            sb.Unindent();
            sb.AppendLine("};");

            sb.AppendLine();
            sb.AppendLine("// Dictionary of FSubr parsers (same signature as Subr)");
            sb.AppendLine("internal static readonly Dictionary<string, SubrDelegate> FSubrParsers = new()");
            sb.AppendLine("{");
            sb.Indent();

            addedNames.Clear();
            lines.Clear();

            // For each unique method, add all its FSubr aliases to the dictionary
            foreach (var kvp in methodToParserName)
            {
                var methodSymbol = kvp.Key;
                var parserName = kvp.Value;

                if (!generatedParsers.Contains(parserName))
                    continue;

                if (methodToAliases.TryGetValue(methodSymbol, out var aliases))
                {
                    foreach (var attrInfo in aliases.Where(a => a.IsFSubr))
                    {
                        var cleanName = attrInfo.Name?.Trim() ?? "";
                        if (!string.IsNullOrWhiteSpace(cleanName) && !addedNames.Contains(cleanName))
                        {
                            addedNames.Add(cleanName);

                            // Add parser to dictionary
                            var fullName = string.IsNullOrEmpty(attrInfo.ObList) ? cleanName : $"{cleanName}!-{attrInfo.ObList}";
                            lines.Add($"[\"{fullName}\"] = {parserName},");
                        }
                    }
                }
            }

            lines.Sort();

            foreach (var line in lines)
                sb.AppendLine(line);

            sb.Unindent();
            sb.AppendLine("};");
        }

        /// <summary>
        /// Converts a C# type name to its corresponding ZIL type name, if known, for use in error messages.
        /// </summary>
        /// <param name="symbolTypeName">The name of a type from a <see cref="ISymbol"/>.</param>
        /// <returns>
        /// The name of the corresponding ZIL type atom, or a descriptive string like "any value",
        /// or <see langword="null""/> for types with no corresponding ZIL type atom.
        /// </returns>
        internal static string? SymbolTypeToZil(string symbolTypeName) => symbolTypeName switch
        {
            "Char" => "CHARACTER",
            "IApplicable" => "applicable value",
            "Int32" => "FIX",
            "IStructure" => "structured value",
            "LocalEnvironment" => "ENVIRONMENT",
            "ObList" => "OBLIST",
            "String" => "STRING",
            "ZilActivation" => "ACTIVATION",
            "ZilAsoc" => "ASOC",
            "ZilAtom" => "ATOM",
            "ZilChannel" => "CHANNEL",
            "ZilChar" => "CHARACTER",
            "ZilDecl" => "DECL",
            "ZilEnvironment" => "ENVIRONMENT",
            "ZilFix" => "FIX",
            "ZilForm" => "FORM",
            "ZilList" => "LIST",
            "ZilListoidBase" => "PRIMTYPE LIST",
            "ZilObject" => "any value",
            "ZilOffset" => "OFFSET",
            "ZilString" => "STRING",
            "ZilVector" => "VECTOR",
            _ => null,
        };

        private static string BuildExpectedTypeDisplay(System.Collections.Generic.IEnumerable<string> expectedTypes, string fallback)
        {
            var normalized = expectedTypes
                .Where(type => !string.IsNullOrWhiteSpace(type))
                .Select(type => type.Trim())
                .ToList();

            if (normalized.Count == 0 && !string.IsNullOrWhiteSpace(fallback))
            {
                normalized.Add(fallback.Trim());
            }

            var distinct = new List<string>();
            var seen = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            foreach (var type in normalized)
            {
                if (seen.Add(type))
                {
                    distinct.Add(type);
                }
            }

            if (distinct.Count == 0)
            {
                return string.IsNullOrWhiteSpace(fallback) ? "structured" : fallback.Trim();
            }

            var ordered = distinct
                .OrderBy(type => type, StringComparer.OrdinalIgnoreCase)
                .ToList();

            if (ordered.Count == 1)
            {
                return ordered[0];
            }

            if (ordered.Count == 2)
            {
                return $"{ordered[0]} or {ordered[1]}";
            }

            return $"{string.Join(", ", ordered.Take(ordered.Count - 1))}, or {ordered[ordered.Count - 1]}";
        }

        private static string BuildExpectedTypeDisplayPreservingOrder(System.Collections.Generic.IEnumerable<string> expectedTypes, string fallback)
        {
            var normalized = expectedTypes
                .Where(type => !string.IsNullOrWhiteSpace(type))
                .Select(type => type.Trim())
                .ToList();

            if (normalized.Count == 0 && !string.IsNullOrWhiteSpace(fallback))
            {
                normalized.Add(fallback.Trim());
            }

            var distinct = new List<string>();
            var seen = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            foreach (var type in normalized)
            {
                if (seen.Add(type))
                {
                    distinct.Add(type);
                }
            }

            if (distinct.Count == 0)
            {
                return string.IsNullOrWhiteSpace(fallback) ? "structured" : fallback.Trim();
            }

            if (distinct.Count == 1)
            {
                return distinct[0];
            }

            if (distinct.Count == 2)
            {
                return $"{distinct[0]} or {distinct[1]}";
            }

            return $"{string.Join(", ", distinct.Take(distinct.Count - 1))}, or {distinct[distinct.Count - 1]}";
        }

        internal static string GetDefaultValueString(IParameterSymbol parameter)
        {
            if (parameter.HasExplicitDefaultValue)
            {
                if (parameter.ExplicitDefaultValue == null)
                {
                    // Check if this is a non-nullable reference type that accepts null as default
                    if (parameter.Type is INamedTypeSymbol explicitNullType && explicitNullType.IsReferenceType &&
                        explicitNullType.NullableAnnotation != NullableAnnotation.Annotated)
                    {
                        // Use null! for non-nullable reference types (like ZilObject)
                        return "null!";
                    }
                    return "null";
                }
                // Return a default expression that matches the parameter's declared type
                var pType = parameter.Type;
                if (parameter.ExplicitDefaultValue is string str)
                {
                    // If the parameter expects a ZilString, return ZilString.FromString, otherwise a C# string literal
                    if (pType.Name == "ZilString")
                        return $"ZilString.FromString(\"{str}\")";
                    return $"\"{str.Replace("\\", "\\\\").Replace("\"", "\\\"")}\"";
                }
                if (parameter.ExplicitDefaultValue is char c)
                {
                    return $"'{c}'";
                }
                if (parameter.ExplicitDefaultValue is bool b)
                {
                    if (pType.SpecialType == SpecialType.System_Boolean)
                        return b ? "true" : "false";
                    // For ZilObject-like boolean defaults, keep ctx.TRUE/ctx.FALSE
                    return b ? "ctx.TRUE" : "ctx.FALSE";
                }
                if (parameter.ExplicitDefaultValue is int i)
                {
                    if (pType.SpecialType == SpecialType.System_Int32)
                        return i.ToString();
                    if (pType.Name == "ZilFix")
                        return $"new ZilFix({i})";
                    // Fallback to numeric literal
                    return i.ToString();
                }
                return parameter.ExplicitDefaultValue.ToString() ?? "null";
            }

            // For parameters without explicit default values, provide reasonable defaults
            var parameterType = parameter.Type;

            // Handle nullable reference types
            if (parameterType is INamedTypeSymbol namedType && namedType.IsReferenceType)
            {
                var typeName = namedType.Name;
                // Special handling for common ZILF types with null defaults
                switch (typeName)
                {
                    case "ZilObject":
                    case "ZilAtom":
                    case "ZilString":
                    case "ZilChannel":
                    case "ZilActivation":
                    case "IStructure":
                        // Use null! for non-nullable reference types
                        return namedType.NullableAnnotation != NullableAnnotation.Annotated ? "null!" : "null";
                    default:
                        if (namedType.IsReferenceType)
                            return namedType.NullableAnnotation != NullableAnnotation.Annotated ? "null!" : "null";
                        break;
                }
            }

            // Handle value types
            if (parameterType.SpecialType == SpecialType.System_Boolean)
                return "false";
            if (parameterType.SpecialType == SpecialType.System_Int32)
                return "0";

            return "null";
        }

        private static string SanitizeName(string name)
        {
            if (string.IsNullOrEmpty(name))
            {
                return string.Empty;
            }

            if (name.StartsWith("-"))
            {
                // special case for leading dash
                name = "Minus" + name.Substring(1);
            }

            var sb = new StringBuilder(name.Length);
            foreach (char c in name)
            {
                switch (c)
                {
                    case '=' when sb.Length > 1 && sb[sb.Length - 2] == 'E' && sb[sb.Length - 1] == 'q':
                        sb.Length -= 2; // Remove previous "Eq"
                        sb.Append("Eeq");
                        break;
                    case '=':
                        sb.Append("Eq");
                        break;
                    case '+':
                        sb.Append("Plus");
                        break;
                    case '*':
                        sb.Append("Times");
                        break;
                    case '/':
                        sb.Append("Div");
                        break;
                    case '?':
                        sb.Append("_P");
                        break;
                    case '-':
                        sb.Append('_');
                        break;
                    case '<':
                        sb.Append("Lt");
                        break;
                    case '>':
                        sb.Append("Gt");
                        break;
                    case '!':
                        sb.Append("Bang");
                        break;
                    case '&':
                        sb.Append("And");
                        break;
                    case '|':
                        sb.Append("Or");
                        break;
                    case '%':
                        sb.Append("Mod");
                        break;
                    case '^':
                        sb.Append("Xor");
                        break;
                    case '#':
                        sb.Append("Hash");
                        break;
                    case '@':
                        sb.Append("At");
                        break;
                    case '$':
                        sb.Append("Dollar");
                        break;
                    case '~':
                        sb.Append("Tilde");
                        break;
                    case '`':
                        sb.Append("Tick");
                        break;
                    case '.':
                        sb.Append("Dot");
                        break;
                    case ',':
                        sb.Append("Comma");
                        break;
                    case ':':
                        sb.Append("Colon");
                        break;
                    case ';':
                        sb.Append("Semi");
                        break;
                    case '\'':
                        sb.Append("Quote");
                        break;
                    case '"':
                        sb.Append("Dquote");
                        break;
                    case '(':
                        sb.Append("Lparen");
                        break;
                    case ')':
                        sb.Append("Rparen");
                        break;
                    case '[':
                        sb.Append("Lbracket");
                        break;
                    case ']':
                        sb.Append("Rbracket");
                        break;
                    case '{':
                        sb.Append("Lbrace");
                        break;
                    case '}':
                        sb.Append("Rbrace");
                        break;
                    case ' ':
                        sb.Append('_');
                        break;
                    default:
                        sb.Append(c);
                        break;
                }
            }
            return sb.ToString();
        }

        private static string SanitizeIdentifierForVariableSuffix(string? name)
        {
            if (string.IsNullOrWhiteSpace(name))
            {
                return string.Empty;
            }

            var sb = new StringBuilder(name!.Length);
            foreach (char c in name)
            {
                if (char.IsLetterOrDigit(c) || c == '_')
                {
                    sb.Append(c);
                }
                else
                {
                    sb.Append('_');
                }
            }

            if (sb.Length == 0)
            {
                return string.Empty;
            }

            if (!char.IsLetter(sb[0]) && sb[0] != '_')
            {
                sb.Insert(0, '_');
            }

            return sb.ToString();
        }

        private static string GetResultVariableName(int parameterId, string? parameterName)
        {
            var suffix = SanitizeIdentifierForVariableSuffix(parameterName);
            if (string.IsNullOrEmpty(suffix))
            {
                suffix = $"arg{parameterId}";
            }

            return $"result_{parameterId}_{suffix}";
        }

        private static bool TryExtractResultVariableId(string varName, out int id)
        {
            id = 0;

            if (!varName.StartsWith("result_", StringComparison.Ordinal))
            {
                return false;
            }

            int idx = 7;
            while (idx < varName.Length && char.IsDigit(varName[idx]))
            {
                idx++;
            }

            if (idx == 7)
            {
                return false;
            }

            return int.TryParse(varName.Substring(7, idx - 7), out id);
        }

        // Data structures for SUBR method information
        public class SubrMethodInfo
        {
            public MethodDeclarationSyntax Method { get; set; } = null!;
            public IMethodSymbol MethodSymbol { get; set; } = null!;
            public SubrAttributeInfo[] AttributeInfos { get; set; } = null!;
            public MdlZilRedirectInfo? MdlZilRedirect { get; set; }
        }

        public class SubrAttributeInfo
        {
            public string Name { get; set; } = "";
            public bool IsFSubr { get; set; }
            public string ObList { get; set; } = "";
        }

        // Utility: remove obvious comment lines from an extracted source text block
        private static IEnumerable<string> FilterOutCommentLines(string text)
        {
            if (string.IsNullOrEmpty(text)) yield break;
            var lines = text.Replace("\r\n", "\n").Replace("\r", "\n").Split('\n');
            foreach (var raw in lines)
            {
                var lnTrim = raw.Trim();
                if (string.IsNullOrEmpty(lnTrim))
                    continue; // skip empty/whitespace-only lines to avoid spurious blank lines
                var ln = raw.TrimStart();
                if (ln.StartsWith("//")) continue;
                if (ln.StartsWith("/*")) continue;
                if (ln.StartsWith("*/")) continue;
                if (ln.StartsWith("///")) continue;
                if (ln.StartsWith("#")) continue; // skip preprocessor directives like #pragma
                if (ln.StartsWith("* ") || ln == "*") continue; // lines already in block comments
                yield return raw.TrimEnd();
            }
        }

        public class MdlZilRedirectInfo
        {
            public string TargetType { get; set; } = "";
            public string TargetMethod { get; set; } = "";
            public bool TopLevelOnly { get; set; }
        }

        #region Step-Based Parser Generation Architecture

        /// <summary>
        /// Base class for all parameter parsing nodes in the step-based tree approach.
        /// Each node knows how to generate parsing code for its specific parameter type.
        /// </summary>
        /// <remarks>
        /// This can represent either a method parameter or a field within a sequence/structured parameter.
        /// Either <see cref="Parameter"/> or <see cref="Field"/> will be set, but not both."/>
        /// </remarks>>
        public abstract class ParameterNode
        {
            public int ParameterId { get; set; }
            public string ParameterName { get; set; } = "";
            public bool IsOptional { get; set; }
            public ITypeSymbol TargetType { get; set; } = null!;
            public IParameterSymbol? Parameter { get; set; }
            public IFieldSymbol? Field { get; set; }
            public int ParameterIndex { get; set; }

            protected string GetResultVariableName() => SubrParserGenerator.GetResultVariableName(ParameterId, ParameterName);

            /// <summary>
            /// Step-based API: emit a local TryParse_{Depth} helper that accepts a ref ErrorRanker and attempts to parse
            /// and sets result_{Depth} on success. Default behavior delegates to the legacy
            /// GenerateCode implementation for incremental migration.
            /// </summary>
            public abstract void GenerateParsingStep(IndentedStringBuilder sb, GenerationContext ctx);

            /// <summary>
            /// Step-based API: emit parser-body invocation that calls the TryParse helper
            /// and applies defaults for optional parameters. Default behavior invokes a helper
            /// named TryParse_{ParameterId} helper which many generated parsing-steps create.
            /// </summary>
            public virtual void GenerateInvokeStep(IndentedStringBuilder sb, GenerationContext ctx)
            {
                var fnName = $"TryParse_{ParameterId}";
                if (IsOptional)
                {
                    sb.AppendLine($"if (!{fnName}({ctx.RankerArgument}))");
                    sb.AppendLine("{");
                    sb.Indent();
                    sb.AppendLine($"if ({ctx.ArgIndexVar} < {ctx.ArgsVar}.Length)");
                    sb.AppendLine("{");
                    sb.Indent();
                    if (Parameter != null && ctx.TrackOptionalMismatch)
                    {
                        sb.AppendLine($"optionalMismatch_{ParameterId} = true;");
                    }
                    var optionalExpectedDisplay = BuildExpectedTypeDisplay(GetErrorExpectedTypes(), GetExpectedTypeName());
                    var optionalExpectedEscaped = optionalExpectedDisplay.Replace("\"", "\\\"");
                    sb.AppendLine($"{ctx.RankerVar}.WrongType(0, {ctx.SiteVar}, {ctx.ArgIndexVar}, \"{optionalExpectedEscaped}\");");
                    sb.Unindent();
                    sb.AppendLine("}");
                    sb.AppendLine($"// Optional parameter '{ParameterName}' not matched, using default value");
                    sb.AppendLine($"{GetResultVariableName()} = {GetDefaultValueExpression()};");
                    sb.Unindent();
                    sb.AppendLine("}");
                }
                else
                {
                    sb.AppendLine($"{fnName}({ctx.RankerArgument});");
                }
            }

            public virtual string GetDescription() => $"{ParameterName} ({GetExpectedTypeName()})";

            public virtual string GetExpectedTypeName()
            {
                // TODO: move this into ArrayParameterNode?
                if (TargetType is IArrayTypeSymbol arrayType)
                {
                    var elementTypeName = arrayType.ElementType.Name;
                    return SubrParserGenerator.SymbolTypeToZil(elementTypeName) ?? elementTypeName;
                }

                // TODO: move this into CustomSequenceParameterNode
                if (TargetType != null &&
                    TargetType.GetAttributes().Any(a => a.AttributeClass?.Name == "ZilSequenceParamAttribute" || a.AttributeClass?.Name == "ZilSequenceParam"))
                {
                    var fields = TargetType.GetMembers().OfType<IFieldSymbol>()
                        .Where(f => f.DeclaredAccessibility == Accessibility.Public && !f.IsStatic)
                        .OrderBy(f => f.Locations.FirstOrDefault()?.SourceSpan.Start ?? 0)
                        .ToArray();

                    if (fields.Length > 0)
                    {
                        var firstFieldTypeName = fields[0].Type.Name;
                        return SubrParserGenerator.SymbolTypeToZil(firstFieldTypeName) ?? firstFieldTypeName;
                    }
                }

                var typeSymbol = TargetType;
                if (typeSymbol is INamedTypeSymbol namedType && namedType.IsGenericType && namedType.Name == "Nullable" && namedType.TypeArguments.Length == 1)
                {
                    var inner = namedType.TypeArguments[0];
                    var innerName = inner.Name;
                    return SubrParserGenerator.SymbolTypeToZil(innerName) ?? innerName;
                }

                var typeName = typeSymbol?.Name ?? "unknown";
                return SubrParserGenerator.SymbolTypeToZil(typeName) ?? typeName;
            }

            public virtual System.Collections.Generic.IEnumerable<string> GetErrorExpectedTypes()
            {
                yield return GetExpectedTypeName();
            }

            public virtual string GetCSharpTypeName() => TargetType?.ToDisplayString() ?? "object";

            protected virtual string GetDefaultValueExpression()
            {
                if (Parameter?.HasExplicitDefaultValue == true)
                    return SubrParserGenerator.GetDefaultValueString(Parameter);

                TypedConstant? optionalDefault = null;

                if (Parameter != null)
                {
                    var optionalAttr = Parameter.GetAttributes().FirstOrDefault(a =>
                        a.AttributeClass?.Name == "ZilOptionalAttribute" || a.AttributeClass?.Name == "ZilOptional");
                    if (optionalAttr != null)
                    {
                        foreach (var namedArg in optionalAttr.NamedArguments)
                        {
                            if (namedArg.Key == "Default")
                            {
                                optionalDefault = namedArg.Value;
                                break;
                            }
                        }
                    }
                }

                if (optionalDefault == null && Field != null)
                {
                    var optionalAttr = Field.GetAttributes().FirstOrDefault(a =>
                        a.AttributeClass?.Name == "ZilOptionalAttribute" || a.AttributeClass?.Name == "ZilOptional");
                    if (optionalAttr != null)
                    {
                        foreach (var namedArg in optionalAttr.NamedArguments)
                        {
                            if (namedArg.Key == "Default")
                            {
                                optionalDefault = namedArg.Value;
                                break;
                            }
                        }
                    }
                }

                if (optionalDefault is { } typedDefault)
                {
                    if (typedDefault.IsNull)
                        return "null";

                    return typedDefault.ToCSharpString();
                }

                var targetType = Parameter?.Type ?? Field?.Type ?? TargetType;

                if (targetType is IArrayTypeSymbol arrayType)
                {
                    var elementTypeName = arrayType.ElementType.ToDisplayString();
                    return $"Array.Empty<{elementTypeName}>()";
                }

                if (targetType?.IsReferenceType == true)
                    return "null!";

                return "default!";
            }

            /// <summary>
            /// Basic compatibility check expression used by optional parameter logic to decide
            /// if the current argument looks like it could match this parameter without
            /// running the full parsing logic. Returns a C# expression as string.
            /// </summary>
            public virtual string GenerateCompatibilityCheck(GenerationContext ctx)
            {
                var argVar = $"{ctx.ArgsVar}[{ctx.ArgIndexVar}]";
                var targetTypeName = GetPatternMatchTypeName();

                if (targetTypeName == "ZilObject") return "true";
                if (targetTypeName == "string") return $"{argVar} is Zilf.Interpreter.Values.ZilString";
                if (targetTypeName == "int") return $"{argVar} is Zilf.Interpreter.Values.ZilFix";
                if (targetTypeName == "bool") return "true";
                if (targetTypeName == "char") return $"{argVar} is Zilf.Interpreter.Values.ZilFix";

                // Common ZILF value types
                if (targetTypeName == "ZilAtom") return $"{argVar} is Zilf.Interpreter.Values.ZilAtom";
                if (targetTypeName == "ZilString") return $"{argVar} is Zilf.Interpreter.Values.ZilString";
                if (targetTypeName == "ZilFix") return $"{argVar} is Zilf.Interpreter.Values.ZilFix";
                if (targetTypeName == "ZilList") return $"{argVar} is Zilf.Interpreter.Values.ZilList";
                if (targetTypeName == "ZilForm") return $"{argVar} is Zilf.Interpreter.Values.ZilForm";
                if (targetTypeName == "LocalEnvironment") return $"{argVar} is Zilf.Interpreter.Values.ZilEnvironment";

                // Fallback: try to match the runtime type to the expected C# type
                return $"{argVar} is {targetTypeName}";
            }

            /// <summary>
            /// Get C# type name for pattern matching (strips nullable markers)
            /// </summary>
            protected virtual string GetPatternMatchTypeName()
            {
                var typeName = TargetType?.ToDisplayString() ?? "object";
                if (typeName.EndsWith("?"))
                    typeName = typeName.Substring(0, typeName.Length - 1);
                return typeName;
            }
        }

        /// <summary>
        /// Represents a LocalEnvironment parameter that may be provided in ZIL arguments.
        /// </summary>
        public class LocalEnvironmentParameterNode : ParameterNode
        {
            public override string GenerateCompatibilityCheck(GenerationContext ctx)
            {
                // LocalEnvironment accepts ZilEnvironment arguments but doesn't require them
                var argVar = $"{ctx.ArgsVar}[{ctx.ArgIndexVar}]";
                return $"{argVar} is Zilf.Interpreter.Values.ZilEnvironment";
            }

            public override void GenerateParsingStep(IndentedStringBuilder sb, GenerationContext ctx)
            {
                var fnName = $"TryParse_{ParameterId}";
                var resultVar = GetResultVariableName();

                sb.AppendLine($"// {nameof(LocalEnvironmentParameterNode)}: Match ENVIRONMENT or use current environment from context");
                sb.AppendLine($"bool {fnName}({ctx.RankerParameter})");
                sb.AppendLine("{");
                sb.Indent();

                // For optional parameters, absence of an argument should signal 'no match' so the
                // caller can apply the default (which for LocalEnvironment is context.LocalEnvironment).
                if (IsOptional)
                {
                    sb.AppendLine($"if ({ctx.ArgIndexVar} >= {ctx.ArgsVar}.Length)");
                    sb.AppendLine("{");
                    sb.Indent();
                    sb.AppendLine("return false;");
                    sb.Unindent();
                    sb.AppendLine("}");
                }
                else
                {
                    // Required: must have an argument
                    sb.AppendLine($"if ({ctx.ArgIndexVar} >= {ctx.ArgsVar}.Length)");
                    sb.AppendLine("{");
                    sb.Indent();
                    if (!string.IsNullOrEmpty(ctx.CallerArgIndexVar))
                    {
                        // Do not overwrite callerArgIndex here; outer helper needs the original start index
                        // so that remaining required count can be calculated correctly by the generated code.
                    }
                    // Record wrong-count into shared ranker and fail the helper; top-level parser will throw.
                    sb.AppendLine($"{ctx.RankerVar}.WrongCount({ctx.ArgIndexVar}, {ctx.SiteVar}, {ctx.Depth + 1}, null, false);");
                    sb.AppendLine("return false;");
                    sb.Unindent();
                    sb.AppendLine("}");
                }

                // Validate the provided argument is a ZilEnvironment and extract its LocalEnvironment
                sb.AppendLine($"var _arg = {ctx.ArgsVar}[{ctx.ArgIndexVar}];");
                sb.AppendLine($"if (_arg is Zilf.Interpreter.Values.ZilEnvironment _env)");
                sb.AppendLine("{");
                sb.Indent();
                sb.AppendLine($"{resultVar} = _env.LocalEnvironment;");
                sb.AppendLine($"{ctx.ArgIndexVar}++;");
                sb.AppendLine("return true;");
                sb.Unindent();
                sb.AppendLine("}");
                sb.AppendLine("else");
                sb.AppendLine("{");
                sb.Indent();
                // Report the type mismatch into the shared ranker and signal no match
                sb.AppendLine($"{ctx.RankerVar}.WrongType({ctx.ArgIndexVar}, {ctx.SiteVar}, {ctx.ArgIndexVar}, \"ENVIRONMENT\");");
                sb.AppendLine("return false;");
                sb.Unindent();
                sb.AppendLine("}");

                sb.Unindent();
                sb.AppendLine("}");
            }

            public override void GenerateInvokeStep(IndentedStringBuilder sb, GenerationContext ctx)
            {
                var fnName = $"TryParse_{ParameterId}";
                var resultVar = GetResultVariableName();

                if (IsOptional)
                {
                    sb.AppendLine($"if (!{fnName}({ctx.RankerArgument}))");
                    sb.AppendLine("{");
                    sb.Indent();
                    sb.AppendLine($"// Optional LocalEnvironment not provided -> use context.LocalEnvironment");
                    sb.AppendLine($"{resultVar} = context.LocalEnvironment;");
                    sb.Unindent();
                    sb.AppendLine("}");
                }
                else
                {
                    sb.AppendLine($"{fnName}({ctx.RankerArgument});");
                }
            }
        }

        /// <summary>
        /// Simple parameter node for basic type conversion and validation
        /// </summary>
        public class SimpleParameterNode : ParameterNode
        {
            public bool HasDeclConstraint { get; set; }
            public DeclConstraintType DeclConstraint { get; set; }
            public string? DeclPattern { get; set; }

            /// <summary>
            /// Get C# type name for variable declarations (preserves nullable markers)
            /// </summary>
            public override string GetCSharpTypeName()
            {
                var baseTypeName = TargetType?.ToDisplayString() ?? "object";

                // Only make reference types nullable when optional and could be null
                if (IsOptional && TargetType?.CanBeReferencedByName == true &&
                    !baseTypeName.EndsWith("?") &&
                    TargetType?.IsReferenceType == true)
                {
                    return baseTypeName + "?";
                }

                return baseTypeName;
            }
            private bool GenerateRequiredParameterLogic(IndentedStringBuilder sb, GenerationContext ctx, string resultVar)
            {
                var argVar = $"{ctx.ArgsVar}[{ctx.ArgIndexVar}]";

                // Generate simple type checking based on target type
                var targetTypeName = GetPatternMatchTypeName(); // Use pattern match type (strips nullable)

                if (targetTypeName == "ZilObject")
                {
                    // Accept any ZilObject
                    sb.AppendLine($"{resultVar} = {argVar};");
                }
                else if (targetTypeName == "string")
                {
                    // Handle string parameters - extract from ZilString.Text
                    sb.AppendLine($"if ({argVar} is Zilf.Interpreter.Values.ZilString zilStr{ParameterId})");
                    sb.AppendLine("{");
                    sb.Indent();
                    sb.AppendLine($"{resultVar} = zilStr{ParameterId}.Text;");
                    sb.Unindent();
                    sb.AppendLine("}");
                    sb.AppendLine("else");
                    sb.AppendLine("{");
                    sb.Indent();
                    sb.AppendLine("return false;");
                    sb.Unindent();
                    sb.AppendLine("}");
                }
                else if (targetTypeName == "int")
                {
                    // Handle int parameters - extract from ZilFix.Value
                    sb.AppendLine($"if ({argVar} is Zilf.Interpreter.Values.ZilFix zilFix{ParameterId})");
                    sb.AppendLine("{");
                    sb.Indent();
                    sb.AppendLine($"{resultVar} = zilFix{ParameterId}.Value;");
                    sb.Unindent();
                    sb.AppendLine("}");
                    sb.AppendLine("else");
                    sb.AppendLine("{");
                    sb.Indent();
                    sb.AppendLine("return false;");
                    sb.Unindent();
                    sb.AppendLine("}");
                }
                else if (targetTypeName == "bool")
                {
                    // Handle bool parameters - any ZilObject converts to bool
                    sb.AppendLine($"{resultVar} = {argVar}.IsTrue;");
                }
                else if (targetTypeName == "char")
                {
                    // Handle char parameters - extract from ZilFix.Value as char
                    sb.AppendLine($"if ({argVar} is Zilf.Interpreter.Values.ZilFix zilChar{ParameterId} && zilChar{ParameterId}.Value >= 0 && zilChar{ParameterId}.Value <= 65535)");
                    sb.AppendLine("{");
                    sb.Indent();
                    sb.AppendLine($"{resultVar} = (char)zilChar{ParameterId}.Value;");
                    sb.Unindent();
                    sb.AppendLine("}");
                    sb.AppendLine("else");
                    sb.AppendLine("{");
                    sb.Indent();
                    sb.AppendLine("return false;");
                    sb.Unindent();
                    sb.AppendLine("}");
                }
                else
                {
                    // Try to cast to specific ZilObject type
                    sb.AppendLine($"if ({argVar} is {targetTypeName} typed{ParameterId})");
                    sb.AppendLine("{");
                    sb.Indent();
                    sb.AppendLine($"{resultVar} = typed{ParameterId};");
                    sb.Unindent();
                    sb.AppendLine("}");
                    sb.AppendLine("else");
                    sb.AppendLine("{");
                    sb.Indent();
                    sb.AppendLine("return false;");
                    sb.Unindent();
                    sb.AppendLine("}");
                }

                // Add [Decl] constraint validation if present
                if (HasDeclConstraint && !string.IsNullOrEmpty(DeclPattern))
                {
                    sb.AppendLine();
                    sb.AppendLine($"// Validate [Decl] constraint: {DeclPattern}");
                    sb.AppendLine("{");
                    sb.Indent();
                    sb.AppendLine($"var decl_{ParameterId} = Zilf.Program.Parse(context, \"{DeclPattern?.Replace("\"", "\\\"")}\").Single();");
                    sb.AppendLine($"if (!Zilf.Language.Decl.Check(context, {argVar}, decl_{ParameterId}))");
                    sb.AppendLine("{");
                    sb.Indent();
                    sb.AppendLine("return false;");
                    sb.Unindent();
                    sb.AppendLine("}");
                    sb.Unindent();
                    sb.AppendLine("}");
                }

                sb.AppendLine($"{ctx.ArgIndexVar}++;");
                return true;
            }

            public override void GenerateParsingStep(IndentedStringBuilder sb, GenerationContext ctx)
            {
                // Emit a local parsing helper that attempts to parse this single parameter.
                // The helper returns true if it consumed an argument (or parsed successfully),
                // false only when the parameter is optional and no argument was provided.
                var fnName = $"TryParse_{ParameterId}";
                var typeName = GetCSharpTypeName();

                sb.AppendLine($"// {nameof(SimpleParameterNode)}: Match {GetExpectedTypeName()}");
                sb.AppendLine($"bool {fnName}({ctx.RankerParameter})");
                sb.AppendLine("{");
                sb.Indent();

                // Note: do NOT declare a new local result variable here; we rely on the outer variable declared in the parser body.

                // For any parameter, if no argument is available, it's a failure to parse.
                // The caller (InvokeStep) will decide if this is an error or means "use default".
                sb.AppendLine($"if ({ctx.ArgIndexVar} >= {ctx.ArgsVar}.Length)");
                sb.AppendLine("{");
                sb.Indent();
                sb.AppendLine("return false;");
                sb.Unindent();
                sb.AppendLine("}");

                // GenerateRequiredParameterLogic handles both type checking and conversion.
                // It returns a boolean, and the return false statements inside it handle any failures.
                GenerateRequiredParameterLogic(sb, ctx, GetResultVariableName());

                sb.AppendLine("return true;");

                sb.Unindent();
                sb.AppendLine("}");
            }

            public override void GenerateInvokeStep(IndentedStringBuilder sb, GenerationContext ctx)
            {
                var fnName = $"TryParse_{ParameterId}";

                if (IsOptional)
                {
                    var matchVar = $"matched_{ParameterId}";
                    var mismatchVar = $"optionalMismatch_{ParameterId}";

                    sb.AppendLine($"var {matchVar} = {fnName}({ctx.RankerArgument});");
                    sb.AppendLine($"if (!{matchVar})");
                    sb.AppendLine("{");
                    sb.Indent();

                    // Check if we have arguments available but parsing failed
                    sb.AppendLine($"if ({ctx.ArgIndexVar} < {ctx.ArgsVar}.Length)");
                    sb.AppendLine("{");
                    sb.Indent();

                    if (ctx.TrackOptionalMismatch)
                    {
                        sb.AppendLine($"{mismatchVar} = true;");
                    }

                    // Let the ranker throw any detailed error it may have recorded
                    sb.AppendLine($"{ctx.RankerVar}.ThrowIfError();");

                    sb.Unindent();
                    sb.AppendLine("}");

                    // No arguments available or no detailed error - use default
                    sb.AppendLine($"{GetResultVariableName()} = {GetDefaultValueExpression()};");
                    sb.Unindent();
                    sb.AppendLine("}");
                }
                else
                {
                    // Required: call helper and throw an appropriate error on failure.
                    sb.AppendLine($"if (!{fnName}({ctx.RankerArgument}))");
                    sb.AppendLine("{");
                    sb.Indent();
                    sb.AppendLine($"// Check if we failed due to lack of arguments or a type mismatch.");
                    sb.AppendLine($"if ({ctx.ArgIndexVar} >= {ctx.ArgsVar}.Length)");
                    sb.AppendLine("{");
                    sb.Indent();
                    if (!string.IsNullOrEmpty(ctx.CallerArgIndexVar))
                    {
                        // We're emitting into a helper/local parser: record wrong-count as "additional arguments required"
                        // Compute remaining required elements = expectedFields - (consumedWithinHelper)
                        // where consumedWithinHelper == (argIndex - callerArgIndex).
                        sb.AppendLine($"var _remaining_{ParameterId} = ({ctx.Depth + 1}) - ({ctx.ArgIndexVar} - {ctx.CallerArgIndexVar});");
                        sb.AppendLine($"{ctx.RankerVar}.WrongCount({ctx.ArgIndexVar}, {ctx.SiteVar}, _remaining_{ParameterId}, _remaining_{ParameterId} == 1 ? 1 : (int?)null, true);");
                        sb.AppendLine("return false;");
                    }
                    else
                    {
                        // At top-level: prefer any candidate error recorded by the shared ranker
                        // (e.g. inner structured-parameter helpers that require additional args).
                        sb.AppendLine($"{ctx.RankerVar}.ThrowIfError();");
                        sb.AppendLine($"throw ArgumentCountError.WrongCount({ctx.SiteVar}, {ctx.Depth + 1}, null);");
                    }
                    sb.Unindent();
                    sb.AppendLine("}");
                    sb.AppendLine("else");
                    sb.AppendLine("{");
                    sb.Indent();
                    // Build a canonical expected-type string and append decl pattern if present
                    var expectedTypeBase = BuildExpectedTypeDisplay(GetErrorExpectedTypes(), GetExpectedTypeName());
                    var expectedTypeName = HasDeclConstraint && !string.IsNullOrEmpty(DeclPattern)
                        ? expectedTypeBase + " matching " + DeclPattern?.Replace("\"", "\\\"")
                        : expectedTypeBase;
                    sb.AppendLine($"{ctx.RankerVar}.WrongType({ctx.ArgIndexVar}, {ctx.SiteVar}, {ctx.ArgIndexVar}, \"{expectedTypeName}\");");
                    if (!string.IsNullOrEmpty(ctx.CallerArgIndexVar))
                    {
                        // In helper context we must not throw - record and return false so caller can backtrack
                        sb.AppendLine("return false;");
                    }
                    else
                    {
                        // At top-level: if the ranker has recorded a candidate, throw it; otherwise throw a specific ArgumentTypeError
                        sb.AppendLine($"{ctx.RankerVar}.ThrowIfError();");
                        sb.AppendLine($"throw new ArgumentTypeError({ctx.SiteVar}, {ctx.ArgIndexVar}, \"{expectedTypeName}\");");
                    }
                    sb.Unindent();
                    sb.AppendLine("}");
                    sb.Unindent();
                    sb.AppendLine("}");
                }
            }
        }

        /// <summary>
        /// Array parameter node for consuming multiple arguments
        /// </summary>
        public class ArrayParameterNode : ParameterNode
        {
            public ParameterNode ElementNode { get; set; } = null!;
            public bool IsTrailingParams { get; set; }
            public bool IsRequired { get; set; }
            public bool HasDeclConstraint { get; set; }
            public string? DeclPattern { get; set; }

            private string GetElementExpectedTypeDisplay()
            {
                return BuildExpectedTypeDisplayPreservingOrder(
                    ElementNode.GetErrorExpectedTypes(),
                    ElementNode.GetExpectedTypeName());
            }

            public override System.Collections.Generic.IEnumerable<string> GetErrorExpectedTypes()
            {
                foreach (var type in ElementNode.GetErrorExpectedTypes())
                {
                    yield return type;
                }
            }

            public override void GenerateParsingStep(IndentedStringBuilder sb, GenerationContext ctx)
            {
                // Ensure the element's parsing step is emitted (use depth+1 for element helper/result)
                var elementCtx = ctx.WithDepth(ctx.Depth + 1);
                ElementNode.GenerateParsingStep(sb, elementCtx);

                var fnName = $"TryParse_{ParameterId}";
                var elementTypeName = ElementNode.GetCSharpTypeName();
                var arrayTypeName = GetCSharpTypeName();

                sb.AppendLine($"// {nameof(ArrayParameterNode)}: Match array of " + ElementNode.GetExpectedTypeName());
                sb.AppendLine($"bool {fnName}({ctx.RankerParameter})");
                sb.AppendLine("{");
                sb.Indent();

                if (IsTrailingParams)
                {
                    // Validate required arrays have at least one element
                    if (IsRequired)
                    {
                        sb.AppendLine($"if ({ctx.ArgIndexVar} >= {ctx.ArgsVar}.Length)");
                        sb.AppendLine("{");
                        sb.Indent();
                        sb.AppendLine($"throw ArgumentCountError.WrongCount({ctx.SiteVar}, 1, null);");
                        sb.Unindent();
                        sb.AppendLine("}");
                    }

                    sb.AppendLine($"var list = new System.Collections.Generic.List<{elementTypeName}>();");
                    sb.AppendLine($"while ({ctx.ArgIndexVar} < {ctx.ArgsVar}.Length)");
                    sb.AppendLine("{");
                    sb.Indent();

                    // To prevent infinite loops, we must check if the index advances.
                    sb.AppendLine($"var prevArgIndex = {ctx.ArgIndexVar};");

                    // Element parser might record type/count failures into the shared ranker.
                    // For a params array, a failing element simply ends the array; do not fail the whole SUBR here.
                    sb.AppendLine($"if (TryParse_{ElementNode.ParameterId}({ctx.RankerArgument}) && {ctx.ArgIndexVar} > prevArgIndex)");
                    sb.AppendLine("{");
                    sb.Indent();
                    sb.AppendLine($"list.Add({SubrParserGenerator.GetResultVariableName(ElementNode.ParameterId, ElementNode.ParameterName)});");
                    sb.Unindent();
                    sb.AppendLine("}");
                    sb.AppendLine("else");
                    sb.AppendLine("{");
                    sb.Indent();
                    sb.AppendLine("// Element didn't match - check if it's a type error");
                    sb.AppendLine("if (prevArgIndex < args.Length)");
                    sb.AppendLine("{");
                    sb.Indent();
                    sb.AppendLine($"// Element failed to parse - record type error");
                    var expectedTypeName = GetElementExpectedTypeDisplay();
                    sb.AppendLine($"{ctx.RankerVar}.WrongType(list.Count, {ctx.SiteVar}, prevArgIndex, \"{expectedTypeName}\");");
                    sb.Unindent();
                    sb.AppendLine("}");
                    sb.AppendLine("break; // element parser signalled no match -> end of params");
                    sb.Unindent();
                    sb.AppendLine("}");

                    // If the argument index hasn't changed, we're in an infinite loop.
                    sb.AppendLine($"if ({ctx.ArgIndexVar} == prevArgIndex) break;");

                    sb.Unindent();
                    sb.AppendLine("}");

                    sb.AppendLine($"{GetResultVariableName()} = list.ToArray();");

                    // Add [Decl] constraint validation if present
                    if (HasDeclConstraint && !string.IsNullOrEmpty(DeclPattern))
                    {
                        sb.AppendLine();
                        sb.AppendLine($"// Validate [Decl] constraint for array parameter: {DeclPattern}");
                        sb.AppendLine("{");
                        sb.Indent();
                        sb.AppendLine($"var decl_{ParameterId} = Zilf.Program.Parse(context, \"{DeclPattern?.Replace("\"", "\\\"")}\").Single();");
                        sb.AppendLine($"var argsList_{ParameterId} = new Zilf.Interpreter.Values.ZilList({GetResultVariableName()}.Cast<Zilf.Interpreter.Values.ZilObject>());");
                        sb.AppendLine($"if (!Zilf.Language.Decl.Check(context, argsList_{ParameterId}, decl_{ParameterId}))");
                        sb.AppendLine("{");
                        sb.Indent();
                        sb.AppendLine($"{ctx.RankerVar}.WrongType(0, {ctx.SiteVar}, 0, \"array arguments matching {DeclPattern?.Replace("\"", "\\\"")}\");");
                        sb.AppendLine("return false;");
                        sb.Unindent();
                        sb.AppendLine("}");
                        sb.Unindent();
                        sb.AppendLine("}");
                    }

                    sb.AppendLine("return true;");
                }
                else
                {
                    // Non-trailing array: stop before remaining required parameters
                    sb.AppendLine($"var list = new System.Collections.Generic.List<{elementTypeName}>();");
                    sb.AppendLine($"var minRequired_{ParameterId} = {ctx.GetRemainingRequiredParameterCount()};");
                    sb.AppendLine($"while ({ctx.ArgIndexVar} < {ctx.ArgsVar}.Length - minRequired_{ParameterId})");
                    sb.AppendLine("{");
                    sb.Indent();
                    sb.AppendLine($"var prevArgIndex = {ctx.ArgIndexVar};");
                    sb.AppendLine($"if (TryParse_{ElementNode.ParameterId}({ctx.RankerArgument}) && {ctx.ArgIndexVar} > prevArgIndex)");
                    sb.AppendLine("{");
                    sb.Indent();
                    sb.AppendLine($"list.Add({SubrParserGenerator.GetResultVariableName(ElementNode.ParameterId, ElementNode.ParameterName)});");
                    sb.Unindent();
                    sb.AppendLine("}");
                    sb.AppendLine("else");
                    sb.AppendLine("{");
                    sb.Indent();
                    sb.AppendLine("// Element didn't match - check if it's a type error");
                    sb.AppendLine("if (prevArgIndex < args.Length)");
                    sb.AppendLine("{");
                    sb.Indent();
                    sb.AppendLine($"// Element failed to parse - record type error");
                    var expectedTypeName = GetElementExpectedTypeDisplay();
                    sb.AppendLine($"{ctx.RankerVar}.WrongType(list.Count, {ctx.SiteVar}, prevArgIndex, \"{expectedTypeName}\");");
                    sb.Unindent();
                    sb.AppendLine("}");
                    sb.AppendLine("break; // element didn't match -> array parsing complete");
                    sb.Unindent();
                    sb.AppendLine("}");
                    sb.Unindent();
                    sb.AppendLine("}");

                    // Enforce required array minimum
                    if (IsRequired)
                    {
                        var additionalPrefixLiteral = ParameterIndex > 0 ? "true" : "false";
                        sb.AppendLine($"if (list.Count == 0)");
                        sb.AppendLine("{");
                        sb.Indent();
                        sb.AppendLine($"{ctx.RankerVar}.WrongCount(0, {ctx.SiteVar}, 1, null, {additionalPrefixLiteral});");
                        sb.AppendLine("return false;");
                        sb.Unindent();
                        sb.AppendLine("}");
                    }

                    sb.AppendLine($"{GetResultVariableName()} = list.ToArray();");

                    // Add [Decl] constraint validation if present
                    if (HasDeclConstraint && !string.IsNullOrEmpty(DeclPattern))
                    {
                        sb.AppendLine();
                        sb.AppendLine($"// Validate [Decl] constraint for array parameter: {DeclPattern}");
                        sb.AppendLine("{");
                        sb.Indent();
                        sb.AppendLine($"var decl_{ParameterId} = Zilf.Program.Parse(context, \"{DeclPattern?.Replace("\"", "\\\"")}\").Single();");
                        sb.AppendLine($"var argsList_{ParameterId} = new Zilf.Interpreter.Values.ZilList({GetResultVariableName()}.Cast<Zilf.Interpreter.Values.ZilObject>());");
                        sb.AppendLine($"if (!Zilf.Language.Decl.Check(context, argsList_{ParameterId}, decl_{ParameterId}))");
                        sb.AppendLine("{");
                        sb.Indent();
                        sb.AppendLine($"{ctx.RankerVar}.WrongType(0, {ctx.SiteVar}, 0, \"array arguments matching {DeclPattern?.Replace("\"", "\\\"")}\");");
                        sb.AppendLine("return false;");
                        sb.Unindent();
                        sb.AppendLine("}");
                        sb.Unindent();
                        sb.AppendLine("}");
                    }

                    sb.AppendLine("return true;");
                }

                sb.Unindent();
                sb.AppendLine("}");
            }

            public override void GenerateInvokeStep(IndentedStringBuilder sb, GenerationContext ctx)
            {
                var fnName = $"TryParse_{ParameterId}";
                var varName = GetResultVariableName();

                // Call the parsing helper; check the result so we can surface ranked errors
                sb.AppendLine($"if (!{fnName}({ctx.RankerArgument}))");
                sb.AppendLine("{");
                sb.Indent();
                if (IsOptional)
                {
                    sb.AppendLine($"// Optional array parameter not matched, using default value");
                    sb.AppendLine($"{varName} = {GetDefaultValueExpression()};");
                }
                else
                {
                    sb.AppendLine($"{ctx.RankerVar}.ThrowIfError();");
                    sb.AppendLine($"// No ranked error - fall back to argument count error for this parameter");
                    var additionalPrefixLiteral = ParameterIndex > 0 ? "true" : "false";
                    if (IsRequired)
                    {
                        sb.AppendLine($"throw ArgumentCountError.WrongCount({ctx.SiteVar}, 1, null, {additionalPrefixLiteral});");
                    }
                    else
                    {
                        sb.AppendLine($"throw ArgumentCountError.WrongCount({ctx.SiteVar}, 0, null);");
                    }
                }
                sb.Unindent();
                sb.AppendLine("}");
            }

            public override string GetExpectedTypeName()
            {
                var elementType = GetElementExpectedTypeDisplay();
                return IsTrailingParams ? $"{elementType}[]" : $"{elementType} array";
            }
        }

        /// <summary>
        /// Either parameter node for handling multiple type alternatives with backtracking
        /// </summary>
        public class EitherParameterNode : ParameterNode
        {
            public List<ParameterNode> Alternatives { get; set; } = [];

            public override string GetExpectedTypeName()
            {
                var types = string.Join(" or ", Alternatives.Select(a => a.GetExpectedTypeName()).Distinct());
                return types;
            }

            /// <summary>
            /// Like <see cref="GetExpectedTypeName"/> but refers to custom parameter types by their structure type name.
            /// </summary>
            private string GetHybridExpectedTypeName()
            {
                return string.Join(" or ", Alternatives.Select(a => a switch
                {
                    CustomParameterNode cust => cust.StructureType.Name,
                    _ => a.GetExpectedTypeName(),
                }).Distinct());
            }

            public override System.Collections.Generic.IEnumerable<string> GetErrorExpectedTypes()
            {
                var yielded = false;
                foreach (var alt in Alternatives)
                {
                    foreach (var type in alt.GetErrorExpectedTypes())
                    {
                        yielded = true;
                        yield return type;
                    }
                }

                if (!yielded)
                    yield return GetExpectedTypeName();
            }

            public override void GenerateParsingStep(IndentedStringBuilder sb, GenerationContext ctx)
            {
                // 1. First, ensure all alternative nodes have generated their own parsing steps.
                for (int i = 0; i < Alternatives.Count; i++)
                {
                    var altCtx = ctx.WithDepth(ctx.Depth + i + 1);
                    Alternatives[i].GenerateParsingStep(sb, altCtx);
                }

                var fnName = $"TryParse_{ParameterId}";
                var typeName = GetCSharpTypeName(); // This will be "object" or a shared base class
                var resultName = GetResultVariableName();

                // sb.AppendLine($"// EitherParameterNode.GenerateParsingStep: {fnName} of type {typeName}, storing into {resultName}, reporting as \"{GetExpectedTypeName()}\"");
                sb.AppendLine($"// {nameof(EitherParameterNode)}: Match {GetHybridExpectedTypeName()}");
                sb.AppendLine($"bool {fnName}({ctx.RankerParameter})");
                sb.AppendLine("{");
                sb.Indent();
                var bestFailureVar = $"bestFailure_{ParameterId}";
                var bestFailureFlag = $"bestFailureRecorded_{ParameterId}";
                sb.AppendLine($"ErrorRanker {bestFailureVar} = default;");
                sb.AppendLine($"bool {bestFailureFlag} = false;");
                var savedArgIndexVar = $"savedArgIndex_{ParameterId}";
                sb.AppendLine($"int {savedArgIndexVar} = argIndex;");
                var altRankerVar = $"altRanker_{ParameterId}";
                sb.AppendLine($"ErrorRanker {altRankerVar} = default;");

                // 2. Generate a chain of calls to the alternatives' parsing steps.
                for (int i = 0; i < Alternatives.Count; i++)
                {
                    var alt = Alternatives[i];
                    var altStepName = $"TryParse_{alt.ParameterId}";
                    var altVarName = SubrParserGenerator.GetResultVariableName(alt.ParameterId, alt.ParameterName);

                    // Attempt alternative and backtrack parser state on failure.
                    if (i > 0)
                    {
                        sb.AppendLine($"argIndex = {savedArgIndexVar};");
                    }
                    sb.AppendLine($"{altRankerVar} = ranker;");
                    sb.AppendLine($"if ({altStepName}(ref {altRankerVar}))");
                    sb.AppendLine("{");
                    sb.Indent();
                    sb.AppendLine($"ranker = {altRankerVar};");
                    sb.AppendLine($"{resultName} = {altVarName};");
                    sb.AppendLine("return true;");
                    sb.Unindent();
                    sb.AppendLine("}");
                    sb.AppendLine($"// failed -> try next alternative from original arg index");
                    sb.AppendLine($"{altRankerVar}.UpdateBestCandidate(ref {bestFailureFlag}, ref {bestFailureVar});");
                }

                // 3. If no alternative matched, handle the error.
                sb.AppendLine();
                sb.AppendLine($"{resultName} = default!;");
                sb.AppendLine("// No alternative matched -> signal failure; shared ranker contains best candidate");
                sb.AppendLine($"if ({bestFailureFlag})");
                sb.AppendLine("{");
                sb.Indent();
                sb.AppendLine($"ranker = {bestFailureVar};");
                sb.Unindent();
                sb.AppendLine("}");
                sb.AppendLine("return false;");

                sb.Unindent();
                sb.AppendLine("}");
            }

            public override void GenerateInvokeStep(IndentedStringBuilder sb, GenerationContext ctx)
            {
                var fnName = $"TryParse_{ParameterId}";

                if (IsOptional)
                {
                    sb.AppendLine($"if (!{fnName}({ctx.RankerArgument}))");
                    sb.AppendLine("{");
                    sb.Indent();
                    sb.AppendLine($"// Optional parameter '{ParameterName}' not matched, using default value");
                    sb.AppendLine($"{GetResultVariableName()} = {GetDefaultValueExpression()};");
                    sb.Unindent();
                    sb.AppendLine("}");
                }
                else
                {
                    var expectedTypeDisplay = BuildExpectedTypeDisplay(GetErrorExpectedTypes(), GetExpectedTypeName());
                    var expectedTypeEscaped = expectedTypeDisplay.Replace("\"", "\\\"");
                    var expectedTypeLength = expectedTypeEscaped.Length;

                    sb.AppendLine($"if (!{fnName}({ctx.RankerArgument}))");
                    sb.AppendLine("{");
                    sb.Indent();
                    sb.AppendLine($"// Check if we failed due to lack of arguments or a type mismatch.");
                    sb.AppendLine($"if ({ctx.ArgIndexVar} >= {ctx.ArgsVar}.Length)");
                    sb.AppendLine("{");
                    sb.Indent();
                    if (!string.IsNullOrEmpty(ctx.CallerArgIndexVar))
                    {
                        sb.AppendLine($"{ctx.CallerArgIndexVar} = {ctx.ArgIndexVar};");
                    }
                    sb.AppendLine($"throw ArgumentCountError.WrongCount({ctx.SiteVar}, {ctx.Depth + 1}, null);");
                    sb.Unindent();
                    sb.AppendLine("}");
                    sb.AppendLine("else");
                    sb.AppendLine("{");
                    sb.Indent();
                    sb.AppendLine($"var currentArgIsStructure = {ctx.ArgIndexVar} < {ctx.ArgsVar}.Length && {ctx.ArgsVar}[{ctx.ArgIndexVar}] is IStructure;");
                    sb.AppendLine($"if (!{ctx.RankerVar}.HasError)");
                    sb.AppendLine("{");
                    sb.Indent();
                    sb.AppendLine($"{ctx.RankerVar}.WrongType({ctx.ArgIndexVar}, {ctx.SiteVar}, {ctx.ArgIndexVar}, \"{expectedTypeEscaped}\");");
                    sb.Unindent();
                    sb.AppendLine("}");
                    sb.AppendLine($"else if ({ctx.RankerVar}.IsWrongType)");
                    sb.AppendLine("{");
                    sb.Indent();
                    sb.AppendLine($"var currentConstraint = {ctx.RankerVar}.CurrentConstraint;");
                    sb.AppendLine($"if (string.IsNullOrEmpty(currentConstraint) || currentConstraint.Length < {expectedTypeLength})");
                    sb.AppendLine("{");
                    sb.Indent();
                    sb.AppendLine("if (!currentArgIsStructure)");
                    sb.AppendLine("{");
                    sb.Indent();
                    sb.AppendLine($"{ctx.RankerVar}.WrongType({ctx.ArgIndexVar}, {ctx.SiteVar}, {ctx.ArgIndexVar}, \"{expectedTypeEscaped}\");");
                    sb.Unindent();
                    sb.AppendLine("}");
                    sb.Unindent();
                    sb.AppendLine("}");
                    sb.Unindent();
                    sb.AppendLine("}");
                    if (!string.IsNullOrEmpty(ctx.CallerArgIndexVar))
                    {
                        sb.AppendLine("return false;");
                    }
                    else
                    {
                        // At top-level prefer any ranked error; otherwise throw an ArgumentTypeError with expected type
                        sb.AppendLine($"{ctx.RankerVar}.ThrowIfError();");
                        sb.AppendLine($"throw new ArgumentTypeError({ctx.SiteVar}, {ctx.ArgIndexVar}, \"{expectedTypeEscaped}\");");
                    }
                    sb.Unindent();
                    sb.AppendLine("}");
                    sb.Unindent();
                    sb.AppendLine("}");
                }
            }
        }

        /// <summary>
        /// Generation context for parameter tree code generation
        /// </summary>
        public class GenerationContext
        {
            public string ArgIndexVar { get; set; } = "argIndex";
            public string ArgsVar { get; set; } = "args";
            public string SiteVar { get; set; } = "site";
            // Name of the variable to use for the ErrorRanker in generated code.
            // Defaults to "ranker" for top-level parsers. Helper methods can override this
            // if they introduce a differently named ranker variable.
            public string RankerVar { get; set; } = "ranker";
            public string MethodName { get; set; } = "";
            public int Depth { get; set; } = 0;
            public int ParameterCount { get; set; } = 0;
            // CallerArgIndexVar is set when the generator is creating code inside a
            // helper method (i.e. one of the emitted TryParse_* locals). In that
            // scenario nested helper invocations need a way to report their
            // failure progress back up to the enclosing helper. When non-null,
            // the generator emits code that assigns the caller's arg-index
            // variable (usually named `callerArgIndex`) before returning false
            // from a nested helper. This lets the outer helper preserve the
            // consumed-argument count and allow the shared ErrorRanker to be
            // consulted by a top-level parser.
            public string? CallerArgIndexVar { get; set; } = null;
            public ParameterNode[] PriorOptionalNodes { get; set; } = [];
            public bool TrackOptionalMismatch { get; set; }

            public string RankerParameter => $"ref ErrorRanker {RankerVar}";
            public string RankerArgument => $"ref {RankerVar}";

            public GenerationContext WithDepth(int newDepth) => new()
            {
                ArgIndexVar = ArgIndexVar,
                ArgsVar = ArgsVar,
                SiteVar = SiteVar,
                RankerVar = RankerVar,
                MethodName = MethodName,
                Depth = newDepth,
                ParameterCount = ParameterCount,
                CallerArgIndexVar = CallerArgIndexVar,
                PriorOptionalNodes = PriorOptionalNodes,
                TrackOptionalMismatch = TrackOptionalMismatch
            };

            public int GetRemainingRequiredParameterCount()
            {
                // This would be calculated by the tree builder based on following parameters
                // For now, return 0 as a placeholder
                return 0;
            }
        }

        /// <summary>
        /// Tree-based parser generator that builds parameter trees and generates code
        /// </summary>
        public class TreeBasedGenerator
        {
            private string? _redirectClause;
            private static readonly HashSet<string> _globalGeneratedHelpers = [];
            private Compilation? _compilation;

            public static void ClearGlobalHelpers()
            {
                _globalGeneratedHelpers.Clear();
            }

            public void GenerateParser(IndentedStringBuilder sb, SubrMethodInfo methodInfo,
                ParameterNode[] tree, string parserName, string? redirectClause, Compilation? compilation, List<string> debugLog)
            {
                _redirectClause = redirectClause;
                _compilation = compilation;

                // Generate helper methods for any ZilSequenceParam structures used in this parser
                GenerateHelperMethods(sb, tree, debugLog);

                GenerateParserMethod(sb, parserName, methodInfo.MethodSymbol, tree, methodInfo.Method);
            }

            private void GenerateParserMethod(IndentedStringBuilder sb, string parserName, IMethodSymbol method, ParameterNode[] tree, MethodDeclarationSyntax? originalMethodSyntax)
            {
                // Emit a descriptive comment block above the generated parser method. The block contains:
                //  - a pseudo-BNF style first paragraph describing the SUBR and its parameter shapes
                //  - a second paragraph showing a reconstructed C# method header (attributes + signature)
                //  - a third paragraph with the source file and line range where the original method is defined
                try
                {
                    // Determine SUBR name from attributes (fall back to method name)
                    var subrAttr = method.GetAttributes().FirstOrDefault(a => a.AttributeClass?.Name == "SubrAttribute" || a.AttributeClass?.Name == "FSubrAttribute");
                    var subrName = subrAttr?.ConstructorArguments.FirstOrDefault().Value as string ?? method.Name;

                    // Build a compact pseudo-BNF parameter description from the parsing tree
                    var paramTokens = new List<string>();
                    foreach (var node in tree)
                    {
                        switch (node)
                        {
                            case EitherParameterNode e:
                                var alts = e.Alternatives.Select(a => a.GetExpectedTypeName()).Distinct();
                                paramTokens.Add("{" + string.Join(" | ", alts) + "}");
                                break;
                            case CustomParameterNode c:
                                paramTokens.Add(c.StructureType?.Name ?? c.GetExpectedTypeName());
                                break;
                            case ArrayParameterNode arr:
                                // Represent arrays in a compact pseudo-BNF form like: [<element> ...]
                                var elemName = arr.ElementNode.GetExpectedTypeName();
                                paramTokens.Add("[" + elemName + " ...]");
                                break;
                            default:
                                paramTokens.Add(node.GetExpectedTypeName());
                                break;
                        }
                    }

                    var bnf = $"<{subrName} {string.Join(" ", paramTokens)}>".Trim();

                    // Build method header-like text. Prefer to use original method syntax text when available.
                    static string BuildAttributeText(AttributeData a)
                    {
                        try
                        {
                            var name = a.AttributeClass?.Name ?? "";
                            if (name.EndsWith("Attribute")) name = name.Substring(0, name.Length - 9);
                            var parts = new List<string>();
                            // constructor args
                            foreach (var ca in a.ConstructorArguments)
                            {
                                if (ca.Kind == Microsoft.CodeAnalysis.TypedConstantKind.Type && ca.Value is ITypeSymbol ts)
                                    parts.Add($"typeof({ts.ToDisplayString()})");
                                else if (ca.Value is string s)
                                    parts.Add($"\"{s.Replace("\\","\\\\").Replace("\"","\\\"")}\"");
                                else if (ca.Value != null)
                                    parts.Add(ca.ToCSharpString());
                            }
                            // named args
                            foreach (var named in a.NamedArguments)
                            {
                                var v = named.Value;
                                if (v.Kind == Microsoft.CodeAnalysis.TypedConstantKind.Type && v.Value is ITypeSymbol ts)
                                    parts.Add($"{named.Key} = typeof({ts.ToDisplayString()})");
                                else if (v.Value is string s)
                                    parts.Add($"{named.Key} = \"{s.Replace("\\","\\\\").Replace("\"","\\\"")}\"");
                                else
                                    parts.Add($"{named.Key} = {v.ToCSharpString()}");
                            }
                            return parts.Count == 0 ? $"[{name}]" : $"[{name}({string.Join(", ", parts)})]";
                        }
                        catch
                        {
                            return "[<attr>]";
                        }
                    }

                    var headerSb = new StringBuilder();
                    // Determine whether this is Subr or FSubr
                    var which = method.GetAttributes().Any(a => a.AttributeClass?.Name == "FSubrAttribute") ? "[FSubr]" : "[Subr]";
                    if (originalMethodSyntax != null)
                    {
                        // Use the original method attribute lists and signature (but NOT the body).
                        // Do NOT prepend a synthetic [Subr]/[FSubr] here since the original attribute list
                        // already contains the real attributes (avoids duplicate lines).
                        var attrsText = originalMethodSyntax.AttributeLists.ToFullString();
                        var signatureText = string.Concat(
                            originalMethodSyntax.Modifiers.ToFullString(),
                            originalMethodSyntax.ReturnType.ToFullString(), " ",
                            originalMethodSyntax.Identifier.ValueText,
                            originalMethodSyntax.TypeParameterList?.ToFullString() ?? "",
                            originalMethodSyntax.ParameterList.ToFullString(),
                            originalMethodSyntax.ConstraintClauses.ToFullString()
                        );

                        var combined = (attrsText + "\n" + signatureText).Trim();
                        // Escape any accidental comment terminators to avoid ending our generated comment block
                        combined = combined.Replace("*/", "*\\/");

                        headerSb.AppendLine(combined);
                    }
                    else
                    {
                        // Reconstruct signature (approximate)
                        var returnType = method.ReturnType.ToDisplayString();
                        // Prepend a synthetic [Subr]/[FSubr] marker for reconstructed headers
                        headerSb.AppendLine(which);
                        headerSb.Append($"public static {returnType} {method.Name}(Context ctx");
                        foreach (var p in method.Parameters.Skip(1))
                        {
                            var attrs = p.GetAttributes().Select(BuildAttributeText).Where(t => !string.IsNullOrEmpty(t)).ToArray();
                            if (attrs.Length > 0)
                                headerSb.AppendLine(",");
                            else
                                headerSb.AppendLine(",");
                            foreach (var at in attrs)
                            {
                                headerSb.Append("    ");
                                headerSb.Append(at);
                                headerSb.AppendLine();
                            }
                            var pType = p.Type.ToDisplayString();
                            headerSb.Append($"    {pType} {p.Name}");
                        }
                        headerSb.AppendLine(")");
                    }

                    // Get source location of the original method (prefer full method syntax range when available)
                    var srcLine = "(unknown source)";
                    try
                    {
                        if (originalMethodSyntax != null)
                        {
                            var syntaxTree = originalMethodSyntax.SyntaxTree;
                            var span = originalMethodSyntax.FullSpan;
                            var lineSpan = syntaxTree.GetLineSpan(span);
                            var path = lineSpan.Path;
                            var start = lineSpan.StartLinePosition.Line + 1;
                            var end = lineSpan.EndLinePosition.Line + 1;
                            srcLine = $"{System.IO.Path.GetFileName(path)}:{start}-{end}";
                        }
                        else
                        {
                            var loc = method.Locations.FirstOrDefault();
                            if (loc != null && loc.IsInSource)
                            {
                                var span = loc.GetLineSpan();
                                var path = span.Path;
                                var start = span.StartLinePosition.Line + 1;
                                var end = span.EndLinePosition.Line + 1;
                                srcLine = $"{System.IO.Path.GetFileName(path)}:{start}-{end}";
                            }
                        }
                    }
                    catch { /* ignore */ }

                    // Emit the comment block: include pseudo-BNF and the original method header (if available)
                    sb.AppendLine("/* " + bnf);
                    sb.AppendLine(" *");
                    // Only include the original method header text if we were able to retrieve it.
                    var headerText = headerSb.ToString().Trim();
                    if (!string.IsNullOrEmpty(headerText))
                    {
                        foreach (var ln in FilterOutCommentLines(headerText))
                        {
                            sb.AppendLine(" * " + ln);
                        }
                        sb.AppendLine(" *");
                    }
                    // Include source location if available
                    if (!string.Equals(srcLine, "(unknown source)", StringComparison.Ordinal))
                    {
                        sb.AppendLine($" * {srcLine}");
                    }
                    sb.AppendLine(" */");
                }
                catch (Exception ex)
                {
                    // On any failure while building comments, fall back to no comment but do not break generation
                    sb.AppendLine($"// (warning) failed to generate descriptive comment: {ex.Message}");
                }

                sb.AppendLine($"public static ZilResult {parserName}(string name, Context context, ZilObject[] args)");
                sb.AppendLine("{");
                sb.Indent();
                sb.AppendLine("var site = new FunctionCallSite(name);");

                var ctx = new GenerationContext
                {
                    MethodName = method.Name,
                    ParameterCount = tree.Length
                };

                // Emit redirect clause if present
                if (!string.IsNullOrEmpty(_redirectClause))
                {
                    foreach (var line in _redirectClause!.Split('\n'))
                        sb.AppendLine(line);
                }

                // Calculate argument count bounds
                var (minArgs, maxArgs) = CalculateArgumentBounds(tree);

                // Generate upfront argument count validation (like original ArgDecoder)
                sb.AppendLine("// Validate argument count bounds");
                bool doThrow = false;

                if (minArgs > 0)
                {
                    if (maxArgs.HasValue)
                    {
                        sb.AppendLine($"if (args.Length < {minArgs} || args.Length > {maxArgs.Value})");
                    }
                    else
                    {
                        sb.AppendLine($"if (args.Length < {minArgs})");
                    }

                    doThrow = true;
                }
                else if (maxArgs != null)
                {
                    sb.AppendLine($"if (args.Length > {maxArgs.Value})");
                    doThrow = true;
                }

                if (doThrow)
                {
                    sb.Indent();
                    var maxArgsStr = maxArgs?.ToString() ?? "null";
                    sb.AppendLine($"throw ArgumentCountError.WrongCount(site, {minArgs}, {maxArgsStr});");
                    sb.Unindent();
                }
                sb.AppendLine();

                // Only declare argIndex if we actually need to parse arguments
                if (tree.Length > 0)
                {
                    sb.AppendLine("int argIndex = 0;");
                    sb.AppendLine();

                    // ErrorRanker used by nested helpers to record best failures for either/alt choice
                    sb.AppendLine("ErrorRanker ranker = new(); // accumulate best failure reasons");
                    sb.AppendLine();
                }

                // Declare all result variables at the start - need to handle nested variables too
                var allVariables = new HashSet<string>();
                for (int i = 0; i < tree.Length; i++)
                {
                    allVariables.Add(GetResultVariableName(tree[i].ParameterId, tree[i].ParameterName));
                    CollectNestedVariables(tree[i], i, allVariables);
                }

                foreach (var varName in allVariables.OrderBy(v => v))
                {
                    var id = ExtractResultId(varName);
                    var node = GetNodeForVariable(tree, varName, id);
                    var typeName = node?.TargetType.ToDisplayString() ?? "Zilf.Interpreter.Values.ZilObject";
                    bool? isArray = node switch
                    {
                        CustomSequenceParameterNode seq => seq.IsArray,
                        CustomStructuredParameterNode stu => stu.IsArray,
                        _ => null
                    };
                    string? strucType = node switch
                    {
                        CustomSequenceParameterNode seq => seq.StructureType?.ToDisplayString(),
                        CustomStructuredParameterNode stu => stu.StructureType?.ToDisplayString(),
                        _ => null
                    };
                    // sb.AppendLine($"// GenerateParserMethod: variable {varName} with id {id} belongs to the {node?.GetType().Name} called {node?.ParameterName}, target type {node?.TargetType.ToDisplayString()}, struc type {strucType}, reporting as \"{node?.GetExpectedTypeName()}\", isArray={isArray}");
                    // Initialize to default to satisfy definite-assignment (parsing helpers or invoke steps will set actual values)
                    sb.AppendLine($"{typeName} {varName} = default!;");
                }
                sb.AppendLine();

                var optionalNodesToTrack = tree.Where(n => n.IsOptional).ToArray();
                var optionalTrackedIds = new HashSet<int>(optionalNodesToTrack.Select(n => n.ParameterId));

                if (optionalNodesToTrack.Length > 0)
                {
                    foreach (var optionalNode in optionalNodesToTrack)
                    {
                        sb.AppendLine($"bool optionalMismatch_{optionalNode.ParameterId} = false;");
                    }
                    sb.AppendLine();
                }

                // Generate parsing step local functions for each parameter node
                foreach (var (node, index) in tree.Select((n, i) => (n, i)))
                {
                    var nodeCtx = ctx.WithDepth(index);
                    // Ensure node's emitted calls use the correct ranker variable name for this scope
                    // Top-level parser uses the (possibly omitted) 'ranker' variable name.
                    nodeCtx.RankerVar = nodeCtx.RankerVar; // keep default "ranker"
                    node.GenerateParsingStep(sb, nodeCtx);
                    sb.AppendLine();
                }

                // Invoke parsing steps from the main parser body
                sb.AppendLine("// Invoke parsing steps");
                // argIndex was declared earlier (if needed); do not redeclare here
                foreach (var (node, index) in tree.Select((n, i) => (n, i)))
                {
                    sb.AppendLine($"// Extract parameter {index}: {node.GetDescription()}");
                    var nodeCtx = ctx.WithDepth(index);
                    nodeCtx.PriorOptionalNodes = [.. tree.Take(index).Where(n => optionalTrackedIds.Contains(n.ParameterId))];
                    nodeCtx.TrackOptionalMismatch = optionalTrackedIds.Contains(node.ParameterId);
                    node.GenerateInvokeStep(sb, nodeCtx);
                    sb.AppendLine();
                }

                if (optionalNodesToTrack.Length > 0)
                {
                    foreach (var optionalNode in optionalNodesToTrack)
                    {
                        sb.AppendLine($"_ = optionalMismatch_{optionalNode.ParameterId};");
                    }
                    sb.AppendLine();
                }

                // Generate method call
                GenerateMethodCall(sb, method, tree, optionalNodesToTrack);

                // Close the method
                sb.Unindent();
                sb.AppendLine("}");

            }

            /// <summary>
            /// Calculate the minimum and maximum number of arguments this method can accept
            /// </summary>
            private (int minArgs, int? maxArgs) CalculateArgumentBounds(ParameterNode[] tree)
            {
                int minArgs = 0;
                int? maxArgs = 0;

                foreach (var node in tree)
                {
                    var (nodeMin, nodeMax) = GetNodeArgumentBounds(node);

                    minArgs += nodeMin;

                    if (nodeMax == null || maxArgs == null)
                    {
                        maxArgs = null; // Unbounded
                    }
                    else
                    {
                        maxArgs += nodeMax.Value;
                    }
                }

                return (minArgs, maxArgs);
            }

            /// <summary>
            /// Get the minimum and maximum number of arguments a single parameter node can consume
            /// </summary>
            private (int min, int? max) GetNodeArgumentBounds(ParameterNode node)
            {
                switch (node)
                {
                    case LocalEnvironmentParameterNode localEnv:
                        // LocalEnvironment parameters DO consume ZIL arguments - they expect ZilEnvironment objects
                        if (localEnv.IsOptional)
                            return (0, 1); // Optional LocalEnvironment parameters consume 0 or 1 argument
                        else
                            return (1, 1); // Required LocalEnvironment parameters consume exactly 1 argument

                    case SimpleParameterNode simple:
                        // Check if this is a ZilSequenceParam type
                        if (simple.TargetType?.GetAttributes().Any(a =>
                            a.AttributeClass?.Name == "ZilSequenceParamAttribute" ||
                            a.AttributeClass?.Name == "ZilSequenceParam") == true)
                        {
                            // Count the number of fields in the structure type
                            if (simple.TargetType is INamedTypeSymbol structType)
                            {
                                var fields = structType.GetMembers().OfType<IFieldSymbol>()
                                    .Where(f => f.DeclaredAccessibility == Accessibility.Public && !f.IsStatic)
                                    .Count();
                                return simple.IsOptional ? (0, fields) : (fields, fields);
                            }
                        }

                        if (simple.IsOptional)
                            return (0, 1); // Optional parameters consume 0 or 1 argument
                        else
                            return (1, 1); // Required parameters consume exactly 1 argument

                    case ArrayParameterNode array:
                        if (array.IsRequired)
                        {
                            return (1, null);  // Required arrays consume 1 to unlimited arguments
                        }
                        else
                        {
                            return (0, null); // Optional arrays consume 0 to unlimited arguments
                        }

                    case EitherParameterNode either:
                                // Either parameters take the minimum of all alternatives for min,
                                // and maximum of all alternatives for max
                                int minAlts = int.MaxValue;
                                int? maxAlts = 0;

                                foreach (var alt in either.Alternatives)
                                {
                                    var (altMin, altMax) = GetNodeArgumentBounds(alt);
                                    if (altMin < minAlts)
                                        minAlts = altMin;

                                    if (altMax == null || maxAlts == null)
                                        maxAlts = null;
                                    else if (altMax > maxAlts)
                                        maxAlts = altMax;
                                }

                                var finalMin = minAlts == int.MaxValue ? 0 : minAlts;

                                // If the Either parameter itself is optional (has default value), minimum is 0
                                if (either.IsOptional)
                                    finalMin = 0;

                                return (finalMin, maxAlts);

                            case CustomSequenceParameterNode customSequence:
                                if (customSequence.IsArray)
                                {
                                    return (0, null); // Optional array consumes 0 to unlimited arguments
                                }

                                if (customSequence.StructureType is INamedTypeSymbol sequenceStructType)
                                {
                                    var fields = sequenceStructType.GetMembers().OfType<IFieldSymbol>()
                                        .Where(f => f.DeclaredAccessibility == Accessibility.Public && !f.IsStatic)
                                        .OrderBy(f => f.Locations.FirstOrDefault()?.SourceSpan.Start ?? 0)
                                        .ToArray();

                                    var treeBuilder = new ParameterTreeBuilder(/*debugLog*/);
                                    var fieldTree = treeBuilder.BuildTree(fields);
                                    var (sequenceMin, sequenceMax) = CalculateArgumentBounds(fieldTree);

                                    if (customSequence.IsOptional)
                                    {
                                        return (0, sequenceMax);
                                    }

                                    return (sequenceMin, sequenceMax);
                                }

                                // Fallback if we can't analyze the sequence type
                                return customSequence.IsOptional ? (0, 1) : (1, 1);

                            case CustomStructuredParameterNode customStructure:
                                if (customStructure.IsArray)
                                {
                                    return customStructure.IsRequired ? (1, null) : (0, null);
                                }
                                else
                                {
                                    return customStructure.IsOptional ? (0, 1) : (1, 1);
                                }

                            default:
                                // Unknown node type - provide reasonable fallback behavior
                                // Most unknown parameter types are likely simple parameters that consume 1 argument
                                return (1, 1);
                            }
                        }

            private void CollectNestedVariables(ParameterNode node, int baseIndex, HashSet<string> variables)
            {
                // TODO: this logic belongs in the nodes themselves
                switch (node)
                {
                    case ArrayParameterNode arrayNode:
                        // Array elements use result_{baseIndex + 1}
                        variables.Add(GetResultVariableName(arrayNode.ElementNode.ParameterId, arrayNode.ElementNode.ParameterName));
                        CollectNestedVariables(arrayNode.ElementNode, baseIndex + 1, variables);
                        break;

                    case EitherParameterNode eitherNode:
                        foreach (var (child, index) in eitherNode.Alternatives.Select((c, i) => (c, i)))
                        {
                            variables.Add(GetResultVariableName(child.ParameterId, child.ParameterName));
                            CollectNestedVariables(child, baseIndex + index + 1, variables);
                        }
                        break;
                }
            }

            private int ExtractResultId(string varName)
            {
                if (TryExtractResultVariableId(varName, out var id))
                {
                    return id;
                }
                return 0;
            }

            private ParameterNode? GetNodeForVariable(ParameterNode[] tree, string varName, int id)
            {
                foreach (var node in tree)
                {
                    if (node.ParameterId == id)
                        return node;
                }

                // For nested variables, try to find the node that would generate this variable
                // Look for array element nodes or sequence child nodes
                for (int i = 0; i < tree.Length; i++)
                {
                    var foundNode = FindNestedNodeForVariable(tree[i], varName);
                    if (foundNode != null)
                        return foundNode;
                }

                // If we can't find a specific node, return null
                return null;
            }

            private ParameterNode? FindNestedNodeForVariable(ParameterNode node, string varName)
            {
                if (varName == GetResultVariableName(node.ParameterId, node.ParameterName))
                    return node;

                if (node is ArrayParameterNode arrayNode)
                {
                    // Recursively check element node
                    return FindNestedNodeForVariable(arrayNode.ElementNode, varName);
                }
                else if (node is EitherParameterNode eitherNode)
                {
                    // Check all alternatives
                    foreach (var alt in eitherNode.Alternatives)
                    {
                        var foundNode = FindNestedNodeForVariable(alt, varName);
                        if (foundNode != null)
                            return foundNode;
                    }
                }

                return null;
            }

            private void GenerateMethodCall(IndentedStringBuilder sb, IMethodSymbol method, ParameterNode[] tree, ParameterNode[] optionalTrackedNodes)
            {
                var parameters = method.Parameters.Where(p => p.Type.Name != "Context").ToArray();
                var paramNames = new List<string>
                {
                    "context" // Always pass context first
                };

                for (int i = 0; i < parameters.Length; i++)
                {
                    var param = parameters[i];
                    var paramName = GetResultVariableName(tree[i].ParameterId, tree[i].ParameterName);

                    // If parameter is optional and reference type, add null-forgiving operator
                    // to handle cases where method signature uses = null! pattern
                    if (param.IsOptional && param.Type.IsReferenceType)
                    {
                        paramName += "!";
                    }

                    paramNames.Add(paramName);
                }

                // Add validation to ensure all arguments have been consumed
                if (tree.Length > 0)
                {
                    sb.AppendLine();
                    sb.AppendLine("// Validate that all arguments have been consumed");
                    sb.AppendLine("if (argIndex < args.Length)");
                    sb.AppendLine("{");
                    sb.Indent();
                    if (optionalTrackedNodes.Length == 1)
                    {
                        // Single optional type: emit inline if/throw, no list/hashset/formatting needed
                        var optNode = optionalTrackedNodes[0];
                        var expectedDisplay = BuildExpectedTypeDisplay(optNode.GetErrorExpectedTypes(), optNode.GetExpectedTypeName());
                        var expectedEscaped = expectedDisplay.Replace("\"", "\\\"");
                        sb.AppendLine("if (!ranker.HasError)");
                        sb.AppendLine("{");
                        sb.Indent();
                        sb.AppendLine($"if (optionalMismatch_{optNode.ParameterId})");
                        sb.Indent();
                        sb.AppendLine($"throw new ArgumentTypeError(site, argIndex, \"{expectedEscaped}\");");
                        sb.Unindent();
                        sb.Unindent();
                        sb.AppendLine("}");
                        sb.AppendLine("ranker.ThrowIfError();");
                        sb.AppendLine("throw ArgumentCountError.TooMany(site, argIndex, null);");
                    }
                    else if (optionalTrackedNodes.Length == 2)
                    {
                        // Two optional types: zero-allocation ListFormatter2
                        sb.AppendLine("if (!ranker.HasError)");
                        sb.AppendLine("{");
                        sb.Indent();
                        sb.AppendLine("var expected = new ListFormatter2();");
                        foreach (var optionalNode in optionalTrackedNodes)
                        {
                            var expectedDisplay = BuildExpectedTypeDisplay(optionalNode.GetErrorExpectedTypes(), optionalNode.GetExpectedTypeName());
                            var expectedEscaped = expectedDisplay.Replace("\"", "\\\"");
                            sb.AppendLine($"if (optionalMismatch_{optionalNode.ParameterId}) expected.Add(\"{expectedEscaped}\");");
                        }
                        sb.AppendLine("if (expected.Count > 0)");
                        sb.Indent();
                        sb.AppendLine("throw new ArgumentTypeError(site, argIndex, expected.ToString());");
                        sb.Unindent();
                        sb.Unindent();
                        sb.AppendLine("}");
                        sb.AppendLine("ranker.ThrowIfError();");
                        sb.AppendLine("throw ArgumentCountError.TooMany(site, argIndex, null);");
                    }
                    else if (optionalTrackedNodes.Length > 2)
                    {
                        // Three or more optional types: general ListFormatterN
                        sb.AppendLine("if (!ranker.HasError)");
                        sb.AppendLine("{");
                        sb.Indent();
                        sb.AppendLine("var expected = new ListFormatterN();");
                        foreach (var optionalNode in optionalTrackedNodes)
                        {
                            var expectedDisplay = BuildExpectedTypeDisplay(optionalNode.GetErrorExpectedTypes(), optionalNode.GetExpectedTypeName());
                            var expectedEscaped = expectedDisplay.Replace("\"", "\\\"");
                            sb.AppendLine($"if (optionalMismatch_{optionalNode.ParameterId}) expected.Add(\"{expectedEscaped}\");");
                        }
                        sb.AppendLine("if (expected.Count > 0)");
                        sb.Indent();
                        sb.AppendLine("throw new ArgumentTypeError(site, argIndex, expected.ToString());");
                        sb.Unindent();
                        sb.Unindent();
                        sb.AppendLine("}");
                        sb.AppendLine("ranker.ThrowIfError();");
                        sb.AppendLine("throw ArgumentCountError.TooMany(site, argIndex, null);");
                    }
                    else
                    {
                        // No optional nodes: simple case
                        sb.AppendLine("ranker.ThrowIfError();");
                        sb.AppendLine("throw ArgumentCountError.TooMany(site, argIndex, null);");
                    }
                    sb.Unindent();
                    sb.AppendLine("}");
                }

                var methodCall = $"{method.ContainingType.Name}.{method.Name}({string.Join(", ", paramNames)})";

                // Check if method returns void
                if (method.ReturnType.SpecialType == SpecialType.System_Void)
                {
                    sb.AppendLine($"{methodCall};");
                    sb.AppendLine("return context.FALSE;"); // Return #F for void methods
                }
                else
                {
                    // Handle nullable return types by using null-coalescing with FALSE
                    if (method.ReturnType.CanBeReferencedByName && method.ReturnType.NullableAnnotation == NullableAnnotation.Annotated)
                    {
                        sb.AppendLine($"return {methodCall} ?? context.FALSE;");
                    }
                    else
                    {
                        sb.AppendLine($"return {methodCall};");
                    }
                }
            }

            private void GenerateHelperMethods(IndentedStringBuilder sb, ParameterNode[] tree, List<string> debugLog)
            {
                // Find all Custom{Sequence,Structured}ParameterNode instances that need helper methods
                var needyNodes = new HashSet<(string name, ITypeSymbol type)>();
                CollectNodesNeedingHelpers(tree, needyNodes, debugLog);

                foreach (var (structTypeName, typeSymbol) in needyNodes)
                {
                    var helperName = $"Generated_Parse{structTypeName}_Helper";
                    if (_globalGeneratedHelpers.Contains(helperName))
                        continue; // Already generated

                    GenerateHelperMethod(sb, structTypeName, helperName, typeSymbol);
                    _globalGeneratedHelpers.Add(helperName);
                    //debugLog.Add($"GenerateHelperMethods: added {helperName}");
                }
            }

            private void CollectNodesNeedingHelpers(ParameterNode[] nodes, HashSet<(string name, ITypeSymbol type)> nodesSoFar, List<string> debugLog)
            {
                foreach (var node in nodes)
                {
                    if (node is CustomSequenceParameterNode seqNode)
                    {
                        nodesSoFar.Add((seqNode.StructureType.Name, seqNode.StructureType));
                        // Also collect helpers for any field types that need them
                        CollectNodesNeedingFieldHelpers(seqNode.StructureType, nodesSoFar, debugLog);
                    }
                    else if (node is CustomStructuredParameterNode structNode)
                    {
                        nodesSoFar.Add((structNode.StructureType.Name, structNode.StructureType));
                        // Also collect helpers for any field types that need them
                        CollectNodesNeedingFieldHelpers(structNode.StructureType, nodesSoFar, debugLog);
                    }
                    else if (node is ArrayParameterNode arrNode)
                    {
                        CollectNodesNeedingHelpers([arrNode.ElementNode], nodesSoFar, debugLog);
                    }
                    else if (node is EitherParameterNode eitherNode)
                    {
                        CollectNodesNeedingHelpers([.. eitherNode.Alternatives], nodesSoFar, debugLog);
                    }
                }
            }

            private static bool IsHelperExcludedType(ITypeSymbol typeSymbol)
            {
                var containingNamespace = typeSymbol.ContainingNamespace?.ToDisplayString();
                if (containingNamespace?.StartsWith("System") == true ||
                    containingNamespace?.StartsWith("Zilf.Interpreter.Values") == true)
                {
                    return true;
                }

                var attributes = typeSymbol.GetAttributes();
                if (attributes.Any(a => a.AttributeClass?.Name == "BuiltinTypeAttribute"))
                {
                    return true;
                }

                return false;
            }

            private void CollectNodesNeedingFieldHelpers(ITypeSymbol structType, HashSet<(string name, ITypeSymbol type)> nodesSoFar, List<string> debugLog)
            {
                // For each field in the structure, check if it needs a helper
                foreach (var member in structType.GetMembers().OfType<IFieldSymbol>())
                {
                    var fieldType = member.Type;

                    // Check for Either attribute first (before excluding types)
                    var eitherAttr = member.GetAttributes().FirstOrDefault(a =>
                        a.AttributeClass?.Name == "EitherAttribute" || a.AttributeClass?.Name == "Either");

                    if (eitherAttr != null)
                    {
                        // Check each Either alternative to see if it needs a helper
                        if (eitherAttr.ConstructorArguments.Length > 0 &&
                            eitherAttr.ConstructorArguments[0].Kind == Microsoft.CodeAnalysis.TypedConstantKind.Array)
                        {
                            var typeValues = eitherAttr.ConstructorArguments[0].Values;
                            foreach (var typeValue in typeValues)
                            {
                                if (typeValue.Value is ITypeSymbol eitherAltType)
                                {
                                    // Check if this alternative type needs a helper method
                                    var altNeedsHelper = eitherAltType.GetAttributes().Any(a =>
                                        a.AttributeClass?.Name == "ZilStructuredParamAttribute" ||
                                        a.AttributeClass?.Name == "ZilSequenceParamAttribute" ||
                                        a.AttributeClass?.Name == "ZilStructuredParam" ||
                                        a.AttributeClass?.Name == "ZilSequenceParam");

                                    if (altNeedsHelper)
                                    {
                                        var altTypeName = eitherAltType.Name;
                                        if (!nodesSoFar.Any(n => n.name == altTypeName))
                                        {
                                            nodesSoFar.Add((altTypeName, eitherAltType));
                                            // Recursively collect helpers for this alternative type's fields
                                            CollectNodesNeedingFieldHelpers(eitherAltType, nodesSoFar, debugLog);
                                        }
                                    }
                                }
                            }
                        }
                    }
                    else
                    {
                        // No Either attribute, check if this is a built-in type that should be excluded
                        if (IsHelperExcludedType(fieldType))
                        {
                            continue;
                        }

                        // Handle both direct field type and array element types
                        ITypeSymbol typeToCheck = fieldType;

                        // If it's an array, check the element type instead
                        if (fieldType is IArrayTypeSymbol arrayType)
                        {
                            typeToCheck = arrayType.ElementType;
                        }

                        // Check if this type needs a helper method (has ZilStructuredParam or ZilSequenceParam attribute)
                        var needsHelper = typeToCheck.GetAttributes().Any(a =>
                            a.AttributeClass?.Name == "ZilStructuredParamAttribute" ||
                            a.AttributeClass?.Name == "ZilSequenceParamAttribute" ||
                            a.AttributeClass?.Name == "ZilStructuredParam" ||
                            a.AttributeClass?.Name == "ZilSequenceParam");

                        if (needsHelper)
                        {
                            var fieldTypeName = typeToCheck.Name;
                            if (!nodesSoFar.Any(n => n.name == fieldTypeName))
                            {
                                nodesSoFar.Add((fieldTypeName, typeToCheck));
                                // Recursively collect helpers for this field type's fields
                                CollectNodesNeedingFieldHelpers(typeToCheck, nodesSoFar, debugLog);
                            }
                        }
                        else
                        {
                            // Also consider runtime ZIL types that may be referenced directly
                            // (e.g., ObList, IApplicable) - if they are declared in the Zilf.Interpreter
                            // namespace and appear as field types, they should be considered for
                            // helper generation only when annotated; otherwise pattern matching
                            // will be emitted. We still add them to the set to ensure consistent
                            // naming when helper generation is needed by other paths.
                            var simpleName = typeToCheck.Name;
                            if (!nodesSoFar.Any(n => n.name == simpleName))
                            {
                                // Do not add primitive/value types unnecessarily
                                if (!IsHelperExcludedType(typeToCheck))
                                {
                                    nodesSoFar.Add((simpleName, typeToCheck));
                                }
                            }
                        }
                    }
                }
            }

            // TODO: factor out common logic with GenerateParserMethod
            private void GenerateHelperMethod(IndentedStringBuilder sb, string structTypeName, string helperName, ITypeSymbol structTypeSymbol)
            {
                // Emit a descriptive comment block for the helper method describing the custom type
                try
                {
                    // Prefer to include the original source text for the struct definition when available.
                    var syntaxTextEmitted = false;
                    foreach (var declRef in structTypeSymbol.DeclaringSyntaxReferences)
                    {
                        try
                        {
                            var node = declRef.GetSyntax();
                            // If it's a struct declaration, retrieve the full text of the declaration
                            var declTree = node.SyntaxTree;
                            var span = node.FullSpan;
                            var text = declTree.GetText().ToString(span);
                            // Escape comment terminators, normalize CRLF, and prefix each line with ' * '
                            var safe = text.Replace("*/", "*\\/").Replace("\r\n", "\n").Replace("\r", "\n");
                            var lines = safe.Split(['\n'], StringSplitOptions.None).Where(l => l != null).ToArray();
                            sb.AppendLine("/*");
                            foreach (var l in FilterOutCommentLines(string.Join("\n", lines)))
                            {
                                sb.AppendLine(" * " + l);
                            }
                            // Add source file:line range for the struct declaration range
                            try
                            {
                                var lineSpan = declTree.GetLineSpan(span);
                                var path = lineSpan.Path;
                                var start = lineSpan.StartLinePosition.Line + 1;
                                var end = lineSpan.EndLinePosition.Line + 1;
                                sb.AppendLine($" * {System.IO.Path.GetFileName(path)}:{start}-{end}");
                            }
                            catch { }
                            sb.AppendLine(" */");
                            syntaxTextEmitted = true;
                            break;
                        }
                        catch
                        {
                            // fall back to reconstruction below
                        }
                    }

                    // If no original syntax was emitted, omit the helper comment entirely (user requested no fallback reconstruction)
                    if (!syntaxTextEmitted)
                    {
                        // do nothing; omit comment for this helper
                    }
                }
                catch
                {
                    // ignore comment generation failures
                }
                var fullTypeName = structTypeSymbol.ToDisplayString();
                var isSequence = structTypeSymbol.GetAttributes().Any(a => a.AttributeClass?.Name == "ZilSequenceParamAttribute");

                /* For structured helpers, we need to emit an inner and outer method. The outer method verifies the structure type,
                 * decomposes the structure into a ZilObject[], and calls the inner method to do the actual parsing. This way, the
                 * code generated for the inner method can refer to argIndex as usual.
                */
                if (!isSequence)
                {
                    // Outer method: verify type and extract fields
                    var expectedTypeConstant = structTypeSymbol.GetAttributes().FirstOrDefault(a =>
                        a.AttributeClass?.Name == "ZilStructuredParamAttribute" ||
                        a.AttributeClass?.Name == "ZilStructuredParam")?
                        .ConstructorArguments.FirstOrDefault();
                    var expectedTypeCSharp = expectedTypeConstant?.ToCSharpString() ?? "StdAtom.None /* unknown */";
                    var expectedTypeZil = expectedTypeCSharp.Substring(expectedTypeCSharp.LastIndexOf('.') + 1);

                    sb.AppendLine($"private static bool {helperName}(ZilObject[] args, ref int callerArgIndex, Context context, CallSite site, out {fullTypeName} result, ref ErrorRanker ranker)");
                    sb.AppendLine("{");
                    sb.Indent();
                    sb.AppendLine("result = default!;");
                    sb.AppendLine($"if (args.Length <= callerArgIndex)");
                    sb.AppendLine("{");
                    sb.Indent();
                    // Record a candidate wrong-count: the structured parameter itself is missing (expects exactly one structured arg)
                    sb.AppendLine("ranker.WrongCount(callerArgIndex, site, 1, 1, false);");
                    sb.AppendLine("return false; // not enough elements for this structured param");
                    sb.Unindent();
                    sb.AppendLine("}");
                    sb.AppendLine();
                    sb.AppendLine($"if (args[callerArgIndex].StdTypeAtom != {expectedTypeCSharp})");
                    sb.AppendLine("{");
                    sb.Indent();
                    // Record a candidate wrong-type for the structured argument at this index
                    sb.AppendLine($"if (!ranker.HasError) ranker.WrongType(callerArgIndex, site, callerArgIndex, \"{expectedTypeZil}\");");
                    sb.AppendLine("return false; // type mismatch for structured param");
                    sb.Unindent();
                    sb.AppendLine("}");
                    sb.AppendLine();
                    sb.AppendLine("int innerIndex = 0;");
                    sb.AppendLine("ZilObject[] innerArgs = ((IStructure)args[callerArgIndex]).ToArray();");
                    sb.AppendLine($"if (!{helperName}_Inner(innerArgs, ref innerIndex, context, site, out var _innerResult, ref ranker))");
                    sb.Indent();
                    sb.AppendLine("return false; // inner parse failed");
                    sb.Unindent();
                    sb.AppendLine("callerArgIndex++; ");
                    sb.AppendLine("result = _innerResult;");
                    sb.AppendLine("return true;");
                    sb.Unindent();
                    sb.AppendLine("}");
                    sb.AppendLine();
                    sb.AppendLine($"private static bool {helperName}_Inner(ZilObject[] args, ref int callerArgIndex, Context context, CallSite site, out {fullTypeName} result, ref ErrorRanker ranker)");
                }
                else
                {
                    sb.AppendLine($"private static bool {helperName}(ZilObject[] args, ref int callerArgIndex, Context context, CallSite site, out {fullTypeName} result, ref ErrorRanker ranker)");
                }


                sb.AppendLine("{");
                sb.Indent();
                sb.AppendLine("int argIndex = callerArgIndex;");
                sb.AppendLine();
                // Ensure out parameter 'result' is definitely assigned before any early return
                sb.AppendLine("result = default!;");
                sb.AppendLine();

                var fields = structTypeSymbol.GetMembers().OfType<IFieldSymbol>()
                    .Where(f => f.DeclaredAccessibility == Accessibility.Public && !f.IsStatic)
                    .OrderBy(f => f.Locations.FirstOrDefault()?.SourceSpan.Start ?? 0) // Order by source location to preserve declaration order
                    .ToArray();

                try
                {
                    var treeBuilder = new ParameterTreeBuilder(/*debugLog*/);
                    var tree = treeBuilder.BuildTree(fields);

                    // Use the shared ref ranker parameter directly so nested helpers update caller state.
                    sb.AppendLine();

                    var ctx = new GenerationContext
                    {
                        MethodName = helperName,
                        ParameterCount = tree.Length,
                        CallerArgIndexVar = "callerArgIndex"
                    };

                    // Calculate argument count bounds
                    var (minArgs, maxArgs) = CalculateArgumentBounds(tree);

                    // Generate upfront argument count validation
                    sb.AppendLine("// Validate element count bounds");
                    if (isSequence)
                    {
                        // Sequence types can't enforce maxArgs up front; individual fields handle count/type errors
                    }
                    else
                    {
                        bool doThrow = false;

                        if (minArgs > 0)
                        {
                            if (maxArgs.HasValue)
                            {
                                sb.AppendLine($"if (args.Length - argIndex < {minArgs} || args.Length - argIndex > {maxArgs.Value})");
                            }
                            else
                            {
                                sb.AppendLine($"if (args.Length - argIndex < {minArgs})");
                            }
                            sb.AppendLine("{");

                            doThrow = true;
                        }
                        else if (maxArgs != null)
                        {
                            sb.AppendLine($"if (args.Length - argIndex > {maxArgs.Value})");
                            sb.AppendLine("{");
                            doThrow = true;
                        }

                        if (doThrow)
                        {
                            sb.Indent();
                            var maxArgsStr = maxArgs?.ToString() ?? "null";
                            // Helper-level methods must not throw. Record the failure in the shared ranker and return false so callers may backtrack.
                            sb.AppendLine($"ranker.WrongCount(0, site, {minArgs}, {maxArgsStr}, false);");
                            sb.AppendLine("return false;");
                            sb.Unindent();
                            sb.AppendLine("}");
                        }
                    }
                    sb.AppendLine();

                    // Declare all result variables at the start - need to handle nested variables too
                    var allVariables = new HashSet<string>();
                    for (int i = 0; i < tree.Length; i++)
                    {
                        allVariables.Add(GetResultVariableName(tree[i].ParameterId, tree[i].ParameterName));
                        CollectNestedVariables(tree[i], i, allVariables);
                    }

                    foreach (var varName in allVariables.OrderBy(v => v))
                    {
                        var id = ExtractResultId(varName);
                        var node = GetNodeForVariable(tree, varName, id);
                        var typeName = node?.GetCSharpTypeName() ?? "Zilf.Interpreter.Values.ZilObject";
                        // sb.AppendLine($"// GenerateHelperMethod: variable {varName} with id {id} belongs to the {node?.GetType().Name} called {node?.ParameterName}, target type {node?.TargetType.ToDisplayString()}, C# type {node?.GetCSharpTypeName()}, reporting as \"{node?.GetExpectedTypeName()}\"");
                        // Initialize to default to satisfy definite-assignment (parsing helpers or invoke steps will set actual values)
                        sb.AppendLine($"{typeName} {varName} = default!;");
                    }
                    sb.AppendLine();

                    // Generate parsing step local functions for each field node
                    foreach (var (node, index) in tree.Select((n, i) => (n, i)))
                    {
                        // Nested local functions update the shared ranker ref parameter directly.
                        var nodeCtx = ctx.WithDepth(index);
                        node.GenerateParsingStep(sb, nodeCtx);
                        sb.AppendLine();
                    }

                    sb.AppendLine("// Invoke parsing steps");

                    foreach (var (node, index) in tree.Select((n, i) => (n, i)))
                    {
                        sb.AppendLine($"// Extract field {index}: {node.GetDescription()}");
                        var nodeCtx = ctx.WithDepth(index);
                        node.GenerateInvokeStep(sb, nodeCtx);
                        sb.AppendLine();
                    }

                    // Construct instance
                    // For non-sequence helpers the inner method has signature: bool _Inner(..., out T result)
                    // Use a differently-named local to avoid colliding with the out param name.
                    sb.AppendLine($"{fullTypeName} _constructed = new();");
                    foreach (var node in tree)
                    {
                        sb.AppendLine($"_constructed.{node.ParameterName} = {GetResultVariableName(node.ParameterId, node.ParameterName)};");
                    }
                    sb.AppendLine("result = _constructed;");
                    sb.AppendLine("callerArgIndex = argIndex;");
                    sb.AppendLine("return true;");
                }
                catch (Exception ex)
                {
                    sb.AppendLine($"// ERROR: Failed to generate parsing logic for {structTypeName}: {ex.Message}");
                    sb.AppendLine($"throw new InvalidOperationException(\"modular parser for {structTypeName} failed generation: {ex.Message}\");");
                }

                sb.Unindent();
                sb.AppendLine("}");
            }

            /// <summary>
            /// Recursively search for a type in an assembly
            /// </summary>
            private INamedTypeSymbol? FindTypeInAssembly(INamespaceSymbol namespaceSymbol, string typeName)
            {
                // Search types in current namespace
                foreach (var type in namespaceSymbol.GetTypeMembers())
                {
                    if (type.Name == typeName)
                        return type;
                }

                // Search in child namespaces
                foreach (var childNamespace in namespaceSymbol.GetNamespaceMembers())
                {
                    var found = FindTypeInAssembly(childNamespace, typeName);
                    if (found != null)
                        return found;
                }

                return null;
            }
        }

        /// <summary>
        /// Base class for custom parameter nodes that handle ZilStructuredParam and ZilSequenceParam structures
        /// </summary>
        public abstract class CustomParameterNode : ParameterNode
        {
            /// <summary>
            /// Gets or sets the type annotated with <c>ZilStructuredParamAttribute</c> or <c>ZilSequenceParamAttribute</c> which this node handles.
            /// </summary>
            public ITypeSymbol StructureType { get; set; } = null!;
            /// <summary>
            /// Gets or sets a value indicating whether this parameter represents an array of the <see cref="StructureType"/>, rather than a single value.
            /// </summary>
            public bool IsArray { get; set; }
            /// <summary>
            /// Gets or sets a value indicating whether this parameter is marked as nullable (i.e. with a <c>'?'</c> suffix in the declaration).
            /// </summary>
            public bool IsNullable { get; set; }

            protected string GetHybridStructureTypeName()
            {
                return SymbolTypeToZil(StructureType.ToDisplayString()) ?? StructureType.Name;
            }
        }

        /// <summary>
        /// Parameter node for handling ZilSequenceParam structures like AdditionalSortParam
        /// </summary>
        public sealed class CustomSequenceParameterNode : CustomParameterNode
        {
            /// <summary>
            /// Gets or sets a value indicating whether this sequence parameter is required (must parse at least one element).
            /// </summary>
            public bool IsRequired { get; set; }

            public override string GetExpectedTypeName()
            {
                if (StructureType != null)
                {
                    var fields = StructureType.GetMembers().OfType<IFieldSymbol>()
                        .Where(f => f.DeclaredAccessibility == Accessibility.Public && !f.IsStatic)
                        .OrderBy(f => f.Locations.FirstOrDefault()?.SourceSpan.Start ?? 0)
                        .ToArray();

                    if (fields.Length > 0)
                    {
                        var builder = new ParameterTreeBuilder(/*[]*/);
                        var nodes = builder.BuildTree(fields);
                        return BuildExpectedTypeDisplayPreservingOrder(
                            nodes.SelectMany(node => node.GetErrorExpectedTypes()),
                            GetHybridStructureTypeName());
                    }
                }

                return GetHybridStructureTypeName();
            }

            public override System.Collections.Generic.IEnumerable<string> GetErrorExpectedTypes()
            {
                if (StructureType != null)
                {
                    var fields = StructureType.GetMembers().OfType<IFieldSymbol>()
                        .Where(f => f.DeclaredAccessibility == Accessibility.Public && !f.IsStatic)
                        .OrderBy(f => f.Locations.FirstOrDefault()?.SourceSpan.Start ?? 0)
                        .ToArray();

                    if (fields.Length > 0)
                    {
                        var builder = new ParameterTreeBuilder(/*[]*/);
                        var nodes = builder.BuildTree(fields);
                        foreach (var node in nodes)
                        {
                            foreach (var type in node.GetErrorExpectedTypes())
                                yield return type;
                        }
                        yield break;
                    }
                }

                foreach (var type in base.GetErrorExpectedTypes())
                    yield return type;
            }

            public override void GenerateParsingStep(IndentedStringBuilder sb, GenerationContext ctx)
            {
                // Emit parsing helper for custom structure parameter
                // Ensure helper methods for the structure are already generated by TreeBasedGenerator.GenerateHelperMethods
                var fnName = $"TryParse_{ParameterId}";
                var resultVar = GetResultVariableName();
                var fullTypeName = StructureType.ToDisplayString();

                var isArrayStr = IsArray ? "array of " : "";
                sb.AppendLine($"// {nameof(CustomSequenceParameterNode)}: Match {isArrayStr}{GetHybridStructureTypeName()}");
                sb.AppendLine($"bool {fnName}({ctx.RankerParameter})");
                sb.AppendLine("{");
                sb.Indent();

                var helperName = $"Generated_Parse{StructureType.Name}_Helper";
                if (IsArray)
                {
                    // For arrays of structures, repeatedly call the generated helper until it fails
                    if (IsNullable)
                    {
                        // sb.AppendLine($"// CustomSequenceParameterNode: nullable array");
                        sb.AppendLine($"var list = new System.Collections.Generic.List<{fullTypeName}>();");
                        sb.AppendLine($"bool parsedAny_{ParameterId}_{ctx.Depth} = false;");
                        sb.AppendLine($"bool encounteredFailureBeforeAny_{ParameterId}_{ctx.Depth} = false;");
                        sb.AppendLine($"while ({ctx.ArgIndexVar} < {ctx.ArgsVar}.Length)");
                        sb.AppendLine("{");
                        sb.Indent();
                        sb.AppendLine($"int savedArgIndex_{ParameterId}_{ctx.Depth} = {ctx.ArgIndexVar};");
                        sb.AppendLine($"if (!{helperName}({ctx.ArgsVar}, ref {ctx.ArgIndexVar}, context, new StructuredArgumentCallSite(site, {ctx.ArgIndexVar}), out var item, ref {ctx.RankerVar}))");
                        sb.AppendLine("{");
                        sb.Indent();
                        sb.AppendLine($"{ctx.ArgIndexVar} = savedArgIndex_{ParameterId}_{ctx.Depth};");
                        sb.AppendLine($"if (!parsedAny_{ParameterId}_{ctx.Depth})");
                        sb.AppendLine("{");
                        sb.Indent();
                        sb.AppendLine($"encounteredFailureBeforeAny_{ParameterId}_{ctx.Depth} = true;");
                        sb.Unindent();
                        sb.AppendLine("}");
                        sb.AppendLine("break; // no more valid structures");
                        sb.Unindent();
                        sb.AppendLine("}");
                        sb.AppendLine($"if ({ctx.ArgIndexVar} == savedArgIndex_{ParameterId}_{ctx.Depth}) break; // no progress, avoid infinite loop");
                        sb.AppendLine("list.Add(item);");
                        sb.AppendLine($"parsedAny_{ParameterId}_{ctx.Depth} = true;");
                        sb.Unindent();
                        sb.AppendLine("}");
                        sb.AppendLine($"if (encounteredFailureBeforeAny_{ParameterId}_{ctx.Depth}) return false;");
                        sb.AppendLine($"{resultVar} = list.Count == 0 ? null : list.ToArray();");
                    }
                    else
                    {
                        // Required array: must parse at least one element
                        // sb.AppendLine($"// CustomSequenceParameterNode: required array");
                        sb.AppendLine($"var list = new System.Collections.Generic.List<{fullTypeName}>();");
                        sb.AppendLine($"bool parsedAny_{ParameterId}_{ctx.Depth} = false;");
                        sb.AppendLine($"bool encounteredFailureBeforeAny_{ParameterId}_{ctx.Depth} = false;");
                        sb.AppendLine($"while ({ctx.ArgIndexVar} < {ctx.ArgsVar}.Length)");
                        sb.AppendLine("{");
                        sb.Indent();
                        sb.AppendLine($"int savedArgIndex_{ParameterId}_{ctx.Depth} = {ctx.ArgIndexVar};");
                        sb.AppendLine($"if (!{helperName}({ctx.ArgsVar}, ref {ctx.ArgIndexVar}, context, new StructuredArgumentCallSite(site, {ctx.ArgIndexVar}), out var item, ref {ctx.RankerVar}))");
                        sb.AppendLine("{");
                        sb.Indent();
                        sb.AppendLine($"{ctx.ArgIndexVar} = savedArgIndex_{ParameterId}_{ctx.Depth};");
                        sb.AppendLine($"if (!parsedAny_{ParameterId}_{ctx.Depth})");
                        sb.AppendLine("{");
                        sb.Indent();
                        sb.AppendLine($"encounteredFailureBeforeAny_{ParameterId}_{ctx.Depth} = true;");
                        sb.Unindent();
                        sb.AppendLine("}");
                        sb.AppendLine("break; // stop parsing elements on first failure");
                        sb.Unindent();
                        sb.AppendLine("}");
                        sb.AppendLine($"if ({ctx.ArgIndexVar} == savedArgIndex_{ParameterId}_{ctx.Depth}) break; // no progress, avoid infinite loop");
                        sb.AppendLine("list.Add(item);");
                        sb.AppendLine($"parsedAny_{ParameterId}_{ctx.Depth} = true;");
                        sb.Unindent();
                        sb.AppendLine("}");
                        sb.AppendLine($"if (encounteredFailureBeforeAny_{ParameterId}_{ctx.Depth}) return false;");
                        if (IsRequired)
                        {
                            sb.AppendLine($"if (list.Count == 0) {{ {ctx.RankerVar}.WrongCount(0, {ctx.SiteVar}, 1, null, false); return false; }}");
                        }
                        sb.AppendLine($"{resultVar} = list.ToArray();");
                    }
                }
                else
                {
                    // Single structure: delegate to generated helper which will throw on error
                    var expectedTypeDisplay = BuildExpectedTypeDisplay(GetErrorExpectedTypes(), GetHybridStructureTypeName());
                    // sb.AppendLine($"// CustomSequenceParameterNode: single structure");
                    // Declare the typed temporary in the outer scope. We intentionally
                    // declare `_singleParsed` here (rather than using an inline
                    // `out var`) because the generator later emits a separate
                    // assignment of the parsed value to the `result_{id}` variable
                    // in a different scope. Declaring the temp in the outer scope
                    // keeps the variable in-scope for that assignment and avoids
                    // accidental CS0103 errors in the generated code.
                    sb.AppendLine($"{fullTypeName} _singleParsed = default!;");
                    var helperSiteExpr = ctx.CallerArgIndexVar != null
                        ? $"new StructuredArgumentCallSite({ctx.SiteVar}, {ctx.ArgIndexVar})"
                        : ctx.SiteVar;
                    sb.AppendLine($"if (!{helperName}({ctx.ArgsVar}, ref {ctx.ArgIndexVar}, context, {helperSiteExpr}, out _singleParsed, ref {ctx.RankerVar}))");
                    sb.AppendLine("{");
                    sb.Indent();
                    sb.AppendLine("return false;");
                    sb.Unindent();
                    sb.AppendLine("}");
                    sb.AppendLine();
                    sb.AppendLine($"{resultVar} = _singleParsed;");
                }

                sb.AppendLine("return true;");
                sb.Unindent();
                sb.AppendLine("}");
            }

            public override void GenerateInvokeStep(IndentedStringBuilder sb, GenerationContext ctx)
            {
                var fnName = $"TryParse_{ParameterId}";
                if (IsOptional)
                {
                    sb.AppendLine($"if (!{fnName}({ctx.RankerArgument}))");
                    sb.AppendLine("{");
                    sb.Indent();
                    sb.AppendLine($"// Optional sequence-structure parameter not matched, using default value");
                    sb.AppendLine($"{GetResultVariableName()} = {GetDefaultValueExpression()};");
                    sb.Unindent();
                    sb.AppendLine("}");
                }
                else
                {
                    var expectedTypeDisplay = BuildExpectedTypeDisplay(GetErrorExpectedTypes(), GetHybridStructureTypeName());
                    var expectedTypeEscaped = expectedTypeDisplay.Replace("\"", "\\\"");
                    if (!string.IsNullOrEmpty(ctx.CallerArgIndexVar))
                    {
                        sb.AppendLine($"if (!{fnName}({ctx.RankerArgument}))");
                        sb.AppendLine("{");
                        sb.Indent();
                        sb.AppendLine($"{ctx.CallerArgIndexVar} = {ctx.ArgIndexVar};");
                        sb.AppendLine("return false;");
                        sb.Unindent();
                        sb.AppendLine("}");
                    }
                    else
                    {
                        sb.AppendLine($"if (!{fnName}({ctx.RankerArgument}))");
                        sb.AppendLine("{");
                        sb.Indent();
                        sb.AppendLine($"{ctx.RankerVar}.ThrowIfError();");
                        sb.AppendLine($"throw new ArgumentTypeError({ctx.SiteVar}, {ctx.ArgIndexVar}, \"{expectedTypeEscaped}\");");
                        sb.Unindent();
                        sb.AppendLine("}");
                    }
                }
            }
        }

        /// <summary>
        /// Parameter node for handling ZilStructuredParam structures like CondClause
        /// This node mirrors the behavior of CustomSequenceParameterNode but for
        /// "structured" parameters (attribute: ZilStructuredParam).
        /// Implemented as a lightweight node that emits a parsing-step which
        /// delegates to generated helper methods (Generated_Parse{Type}_Helper).
        /// </summary>
        public sealed class CustomStructuredParameterNode : CustomParameterNode
        {
            /// <summary>
            /// Gets or sets a value indicating whether this parameter is marked as required.
            /// </summary>
            /// <remarks>
            /// Only applies when <see cref="CustomParameterNode.IsArray"/> is true. If true, at least one element must be parsed;
            /// otherwise, the parameter can be an empty array.
            /// </remarks>
            public bool IsRequired { get; set; }

            public override System.Collections.Generic.IEnumerable<string> GetErrorExpectedTypes()
            {
                if (StructureType != null)
                {
                    var structuredAttr = StructureType.GetAttributes().FirstOrDefault(a =>
                        a.AttributeClass?.Name == "ZilStructuredParamAttribute" ||
                        a.AttributeClass?.Name == "ZilStructuredParam");
                    if (structuredAttr != null && structuredAttr.ConstructorArguments.Length > 0)
                    {
                        var expectedTypeConstant = structuredAttr.ConstructorArguments[0];
                        if (!expectedTypeConstant.IsNull)
                        {
                            var expectedTypeCSharp = expectedTypeConstant.ToCSharpString();
                            if (!string.IsNullOrEmpty(expectedTypeCSharp))
                            {
                                var lastDot = expectedTypeCSharp.LastIndexOf('.');
                                var atomName = lastDot >= 0 ? expectedTypeCSharp.Substring(lastDot + 1) : expectedTypeCSharp;
                                yield return IsArray ? atomName + " array" : atomName;
                                yield break;
                            }
                        }
                    }
                }

                foreach (var type in base.GetErrorExpectedTypes())
                    yield return type;
            }

            public override void GenerateParsingStep(IndentedStringBuilder sb, GenerationContext ctx)
            {
                var fnName = $"TryParse_{ParameterId}";
                var resultVar = GetResultVariableName();
                var fullTypeName = StructureType.ToDisplayString();

                var isArrayStr = IsArray ? "array of " : "";
                sb.AppendLine($"// {nameof(CustomStructuredParameterNode)}: Match {isArrayStr}{GetHybridStructureTypeName()}");
                sb.AppendLine($"bool {fnName}({ctx.RankerParameter})");
                sb.AppendLine("{");
                sb.Indent();

                var helperName = $"Generated_Parse{StructureType.Name}_Helper";

                if (IsArray)
                {
                    // Array of structured params: repeatedly call helper until it fails
                    sb.AppendLine($"var list = new System.Collections.Generic.List<{fullTypeName}>();");
                    sb.AppendLine($"while ({ctx.ArgIndexVar} < {ctx.ArgsVar}.Length)");
                    sb.AppendLine("{");
                    sb.Indent();
                    sb.AppendLine($"int savedArgIndex_{ParameterId}_{ctx.Depth} = {ctx.ArgIndexVar};");
                    sb.AppendLine($"if (!{helperName}({ctx.ArgsVar}, ref {ctx.ArgIndexVar}, context, new StructuredArgumentCallSite(site, {ctx.ArgIndexVar}), out var item, ref {ctx.RankerVar}))");
                    sb.AppendLine("{");
                    sb.Indent();
                    sb.AppendLine($"{ctx.ArgIndexVar} = savedArgIndex_{ParameterId}_{ctx.Depth};");
                    sb.AppendLine("break;");
                    sb.Unindent();
                    sb.AppendLine("}");
                    sb.AppendLine($"if ({ctx.ArgIndexVar} == savedArgIndex_{ParameterId}_{ctx.Depth}) break; // no progress, avoid infinite loop");
                    sb.AppendLine("list.Add(item);");
                    sb.Unindent();
                    sb.AppendLine("}");
                        if (IsRequired)
                        {
                            sb.AppendLine($"if (list.Count == 0) {{ {ctx.RankerVar}.WrongCount(0, {ctx.SiteVar}, 1, null, false); return false; }}");
                            sb.AppendLine($"{resultVar} = list.ToArray();");
                        }
                    else if (IsNullable)
                    {
                        sb.AppendLine($"{resultVar} = list.Count == 0 ? null : list.ToArray();");
                    }
                    else
                    {
                        sb.AppendLine($"{resultVar} = list.ToArray();");
                    }
                }
                else
                {
                    // Single structured param: delegate to helper
                    var expectedTypeDisplay = BuildExpectedTypeDisplay(GetErrorExpectedTypes(), GetHybridStructureTypeName());
                    sb.AppendLine($"{fullTypeName} _singleParsed = default!;");
                    var helperSiteExpr = ctx.CallerArgIndexVar != null
                        ? $"new StructuredArgumentCallSite({ctx.SiteVar}, {ctx.ArgIndexVar})"
                        : ctx.SiteVar;
                    sb.AppendLine($"if (!{helperName}({ctx.ArgsVar}, ref {ctx.ArgIndexVar}, context, {helperSiteExpr}, out _singleParsed, ref {ctx.RankerVar}))");
                    sb.AppendLine("{");
                    sb.Indent();
                    sb.AppendLine("return false;");
                    sb.Unindent();
                    sb.AppendLine("}");
                    sb.AppendLine($"{resultVar} = _singleParsed;");
                }

                sb.AppendLine("return true;");
                sb.Unindent();
                sb.AppendLine("}");
            }

            public override void GenerateInvokeStep(IndentedStringBuilder sb, GenerationContext ctx)
            {
                var fnName = $"TryParse_{ParameterId}";
                var resultVar = GetResultVariableName();
                    if (IsOptional)
                    {
                        sb.AppendLine($"if (!{fnName}({ctx.RankerArgument}))");
                        sb.AppendLine("{");
                        sb.Indent();

                        // Check if we have arguments available but parsing failed
                        sb.AppendLine($"if ({ctx.ArgIndexVar} < {ctx.ArgsVar}.Length)");
                        sb.AppendLine("{");
                        sb.Indent();

                        // If no detailed error was recorded, try to probe for better error info
                        // TODO: This "probing" sure looks redundant, but it seems necessary for correct error messages. Figure out why.
                        sb.AppendLine($"if (!{ctx.RankerVar}.HasError && {ctx.ArgsVar}[{ctx.ArgIndexVar}] is IStructure)");
                        sb.AppendLine("{");
                        sb.Indent();
                        sb.AppendLine($"var probeIndex = {ctx.ArgIndexVar};");
                        sb.AppendLine("var probeRanker = new ErrorRanker();");
                        sb.AppendLine($"Generated_Parse{StructureType.Name}_Helper({ctx.ArgsVar}, ref probeIndex, context, {ctx.SiteVar}, out var _, ref probeRanker);");
                        sb.AppendLine("if (probeRanker.HasError)");
                        sb.AppendLine("{");
                        sb.Indent();
                        sb.AppendLine($"{ctx.RankerVar} = probeRanker;");
                        sb.Unindent();
                        sb.AppendLine("}");
                        sb.Unindent();
                        sb.AppendLine("}");

                        // Let the ranker throw any detailed structured parsing errors it may have recorded
                        sb.AppendLine($"{ctx.RankerVar}.ThrowIfError();");

                        sb.Unindent();
                        sb.AppendLine("}");

                        // No arguments available or no detailed error - use default
                        sb.AppendLine($"{resultVar} = {GetDefaultValueExpression()};");
                        sb.Unindent();
                        sb.AppendLine("}");
                    }
                    else
                    {
                        var expectedTypeDisplay = BuildExpectedTypeDisplay(GetErrorExpectedTypes(), GetHybridStructureTypeName());
                        var expectedTypeEscaped = expectedTypeDisplay.Replace("\"", "\\\"");
                        if (!string.IsNullOrEmpty(ctx.CallerArgIndexVar))
                        {
                            sb.AppendLine($"if (!{fnName}({ctx.RankerArgument}))");
                            sb.AppendLine("{");
                            sb.Indent();
                            sb.AppendLine($"{ctx.CallerArgIndexVar} = {ctx.ArgIndexVar};");
                            sb.AppendLine("return false;");
                            sb.Unindent();
                            sb.AppendLine("}");
                        }
                        else
                        {
                            sb.AppendLine($"if (!{fnName}({ctx.RankerArgument}))");
                            sb.AppendLine("{");
                            sb.Indent();

                            // Build combined expected type list from prior optional parameters
                            if (ctx.PriorOptionalNodes.Length > 0)
                            {
                                sb.AppendLine("var expectedTypes = new System.Collections.Generic.List<string>();");
                                sb.AppendLine("var hasPriorOptionalMismatch = false;");
                                // Include prior optional types only if they had a mismatch (failed to parse available argument)
                                foreach (var priorOpt in ctx.PriorOptionalNodes)
                                {
                                    var priorExpectedDisplay = BuildExpectedTypeDisplay(priorOpt.GetErrorExpectedTypes(), priorOpt.GetExpectedTypeName());
                                    var priorExpectedEscaped = priorExpectedDisplay.Replace("\"", "\\\"");
                                    sb.AppendLine($"if (optionalMismatch_{priorOpt.ParameterId})");
                                    sb.AppendLine("{");
                                    sb.Indent();
                                    sb.AppendLine("hasPriorOptionalMismatch = true;");
                                    sb.AppendLine($"expectedTypes.Add(\"{priorExpectedEscaped}\");");
                                    sb.Unindent();
                                    sb.AppendLine("}");
                                }
                                sb.AppendLine($"expectedTypes.Add(\"{expectedTypeEscaped}\");");
                                sb.AppendLine("var combinedExpected = expectedTypes.Count == 1 ? expectedTypes[0] : expectedTypes.Count == 2 ? expectedTypes[0] + \" or \" + expectedTypes[1] : string.Join(\", \", expectedTypes.GetRange(0, expectedTypes.Count - 1)) + \", or \" + expectedTypes[expectedTypes.Count - 1];");

                                // Try to get detailed error by probing the structure if it's a structure argument
                                // TODO: This "probing" sure looks redundant, but it seems necessary for correct error messages. Figure out why.
                                sb.AppendLine($"if ({ctx.ArgIndexVar} < {ctx.ArgsVar}.Length && {ctx.ArgsVar}[{ctx.ArgIndexVar}] is IStructure)");
                                sb.AppendLine("{");
                                sb.Indent();
                                sb.AppendLine($"var probeIndex = {ctx.ArgIndexVar};");
                                sb.AppendLine("var probeRanker = new ErrorRanker();");
                                sb.AppendLine($"if (!Generated_Parse{StructureType.Name}_Helper({ctx.ArgsVar}, ref probeIndex, context, {ctx.SiteVar}, out var _, ref probeRanker) && probeRanker.HasError)");
                                sb.AppendLine("{");
                                sb.Indent();
                                sb.AppendLine($"if (!hasPriorOptionalMismatch || !probeRanker.IsWrongType || probeRanker.CurrentConstraint != \"{expectedTypeEscaped}\" || probeRanker.CurrentIndex != {ctx.ArgIndexVar})");
                                sb.AppendLine("{");
                                sb.Indent();
                                sb.AppendLine("probeRanker.Throw();");
                                sb.Unindent();
                                sb.AppendLine("}");
                                sb.Unindent();
                                sb.AppendLine("}");
                                sb.Unindent();
                                sb.AppendLine("}");

                                sb.AppendLine($"{ctx.RankerVar}.WrongType({ctx.ArgIndexVar}, {ctx.SiteVar}, {ctx.ArgIndexVar}, combinedExpected);");
                                sb.AppendLine($"throw new ArgumentTypeError({ctx.SiteVar}, {ctx.ArgIndexVar}, combinedExpected);");
                            }
                            else
                            {
                                // Let the ranker throw any detailed errors it has, or fall back to generic error
                                sb.AppendLine($"{ctx.RankerVar}.ThrowIfError();");
                                sb.AppendLine($"{ctx.RankerVar}.WrongType({ctx.ArgIndexVar}, {ctx.SiteVar}, {ctx.ArgIndexVar}, \"{expectedTypeEscaped}\");");
                                sb.AppendLine($"throw new ArgumentTypeError({ctx.SiteVar}, {ctx.ArgIndexVar}, \"{expectedTypeEscaped}\");");
                            }

                            sb.Unindent();
                            sb.AppendLine("}");
                        }
                    }
            }
        }

        /// <summary>
        /// Parameter tree builder that analyzes method signatures and builds parsing trees
        /// </summary>
        public class ParameterTreeBuilder(/*List<string> debugLog*/)
        {
            private int _nextNodeId = 1;

            public ParameterNode[] BuildTree(IParameterSymbol[] parameters)
            {
                var nodes = new List<ParameterNode>();

                // Skip Context parameter
                var nonContextParams = parameters.Where(p => p.Type.Name != "Context").ToArray();

                for (int i = 0; i < nonContextParams.Length; i++)
                {
                    var param = nonContextParams[i];
                    var node = BuildNode(param, i);
                    nodes.Add(node);
                }

                return [.. nodes];
            }

            private ParameterNode BuildNode(IParameterSymbol parameter, int index)
            {
                var typeName = parameter.Type.Name;

                // Handle LocalEnvironment parameters - they're special
                if (typeName == "LocalEnvironment")
                {
                    return new LocalEnvironmentParameterNode
                    {
                        ParameterId = _nextNodeId++,
                        ParameterName = parameter.Name,
                        TargetType = parameter.Type,
                        Parameter = parameter,
                        ParameterIndex = index,
                        IsOptional = true // LocalEnvironment is implicitly optional
                    };
                }

                // Check for ZilSequenceParam structures
                var paramType = parameter.Type;
                var actualType = paramType;
                bool isArray = false, isNullable = false;

                // TODO: modularize array parsing - use ArrayParameterNode for all types instead of IsArray
                if (paramType is IArrayTypeSymbol arrayType)
                {
                    actualType = arrayType.ElementType;
                    isArray = true;

                    // Check if it's a nullable array (like AdditionalSortParam[]?)
                    isNullable = parameter.NullableAnnotation == NullableAnnotation.Annotated;
                }

                // Check if the type (or array element type) has ZilSequenceParamAttribute
                var hasZilSequenceParam = actualType.GetAttributes().Any(a =>
                    a.AttributeClass?.Name == "ZilSequenceParamAttribute" ||
                    a.AttributeClass?.Name == "ZilSequenceParam");

                if (hasZilSequenceParam)
                {
                    // debugLog.Add($"BuildNode(IParameterSymbol): {parameter.Name} is ZilSequenceParam, target type {parameter.Type.ToDisplayString()}, actual type {actualType.ToDisplayString()}, isArray={isArray}, isNullable={isNullable}");
                    return new CustomSequenceParameterNode
                    {
                        ParameterId = _nextNodeId++,
                        ParameterName = parameter.Name,
                        TargetType = parameter.Type,
                        Parameter = parameter,
                        ParameterIndex = index,
                        IsOptional = parameter.IsOptional,
                        StructureType = actualType,
                        IsArray = isArray,
                        IsNullable = isNullable
                            ,IsRequired = parameter.GetAttributes().Any(ad => ad.AttributeClass?.Name == "RequiredAttribute")
                    };
                }

                // Check if the type (or array element type) has ZilStructuredParamAttribute
                var hasZilStructuredParam = actualType.GetAttributes().Any(a =>
                    a.AttributeClass?.Name == "ZilStructuredParamAttribute" ||
                    a.AttributeClass?.Name == "ZilStructuredParam");

                if (hasZilStructuredParam)
                {
                    // debugLog.Add($"BuildNode(IParameterSymbol): {parameter.Name} is ZilStructuredParam, target type {parameter.Type.ToDisplayString()}, actual type {actualType.ToDisplayString()}, isArray={isArray}, isNullable={isNullable}");
                    return new CustomStructuredParameterNode
                    {
                        ParameterId = _nextNodeId++,
                        ParameterName = parameter.Name,
                        TargetType = parameter.Type,
                        Parameter = parameter,
                        ParameterIndex = index,
                        StructureType = actualType,
                        IsArray = isArray,
                        IsNullable = isNullable,
                        IsOptional = parameter.IsOptional,
                        IsRequired = parameter.GetAttributes().Any(ad => ad.AttributeClass?.Name == "RequiredAttribute")
                    };
                }

                // Check for [Either] attribute first, before handling arrays
                var eitherAttr = parameter.GetAttributes().FirstOrDefault(a =>
                    a.AttributeClass?.Name == "EitherAttribute" || a.AttributeClass?.Name == "Either");

                if (eitherAttr != null)
                {
                    // Handle Either parameters
                    var eitherNode = new EitherParameterNode
                    {
                        ParameterId = _nextNodeId++,
                        ParameterName = parameter.Name,
                        TargetType = actualType,
                        Parameter = parameter,
                        ParameterIndex = index,
                        IsOptional = parameter.IsOptional
                    };

                    // Extract the types from the Either attribute
                    if (eitherAttr.ConstructorArguments.Length > 0 &&
                        eitherAttr.ConstructorArguments[0].Kind == Microsoft.CodeAnalysis.TypedConstantKind.Array)
                    {
                        var typeValues = eitherAttr.ConstructorArguments[0].Values;
                        foreach (var typeValue in typeValues)
                        {
                            if (typeValue.Value is ITypeSymbol typeSymbol)
                            {
                                // Build an appropriate node for the alternative type (handles arrays, structured/sequence types)
                                var altNode = BuildNodeFromType(typeSymbol, parameter, index);
                                altNode.ParameterName = parameter.Name + "_alt";
                                altNode.IsOptional = false; // Individual alternatives are not optional within Either
                                eitherNode.Alternatives.Add(altNode);
                            }
                        }
                    }

                    if (isArray)
                    {
                        // debugLog.Add($"ParameterTreeBuilder: wrapping Either as ArrayParameterNode for parameter {parameter.Name} of type {parameter.Type.ToDisplayString()}");

                        // TODO: this should be handled by BuildNodeFromType...?
                        return new ArrayParameterNode
                        {
                            ParameterId = _nextNodeId++,
                            ParameterName = parameter.Name,
                            TargetType = parameter.Type,
                            Parameter = parameter,
                            ParameterIndex = index,
                            IsOptional = parameter.IsOptional,
                            IsTrailingParams = false,
                            IsRequired = false,
                            ElementNode = eitherNode,
                            HasDeclConstraint = false,
                            DeclPattern = null
                        };
                    }

                    return eitherNode;
                }

                // Handle array parameters
                if (parameter.Type is IArrayTypeSymbol arrType)
                {
                    var elementType = arrType.ElementType;
                    // Build a proper element node (may be structured/sequence/etc.)
                    var elementNode = BuildNodeFromType(elementType, parameter, index);
                    elementNode.ParameterName = $"{parameter.Name}_element";
                    elementNode.Parameter = parameter; // element node refers back to the parent parameter for defaults
                    elementNode.ParameterIndex = index;

                    // For array parameters, determine if required based on ZILF semantics
                    // Arrays are required only if they are params arrays OR have explicit [Required] attribute
                    var hasRequiredAttribute = parameter.GetAttributes().Any(a => a.AttributeClass?.Name == "RequiredAttribute");
                    var isArrayRequired = /*parameter.IsParams ||*/ hasRequiredAttribute;

                    // Extract [Decl] attribute if present on the array parameter
                    var arrayDeclAttr = parameter.GetAttributes().FirstOrDefault(a => a.AttributeClass?.Name == "DeclAttribute");
                    bool hasDeclConstraint = arrayDeclAttr != null;
                    string? declPattern = null;
                    if (arrayDeclAttr?.ConstructorArguments.Length > 0)
                    {
                        declPattern = arrayDeclAttr.ConstructorArguments[0].Value?.ToString();
                    }

                    return new ArrayParameterNode
                    {
                        ParameterId = _nextNodeId++,
                        ParameterName = parameter.Name,
                        TargetType = parameter.Type,
                        Parameter = parameter,
                        ParameterIndex = index,
                        IsOptional = parameter.IsOptional,
                        IsTrailingParams = parameter.IsParams,
                        IsRequired = isArrayRequired,
                        ElementNode = elementNode,
                        HasDeclConstraint = hasDeclConstraint,
                        DeclPattern = declPattern
                    };
                }



                // Handle simple parameters
                var simpleNode = new SimpleParameterNode
                {
                    ParameterId = _nextNodeId++,
                    ParameterName = parameter.Name,
                    TargetType = parameter.Type,
                    Parameter = parameter,
                    ParameterIndex = index,
                    IsOptional = parameter.IsOptional
                };

                // Check for [Decl] constraints
                var declAttr = parameter.GetAttributes().FirstOrDefault(a =>
                    a.AttributeClass?.Name == "DeclAttribute" || a.AttributeClass?.Name == "Decl");
                if (declAttr != null)
                {
                    simpleNode.HasDeclConstraint = true;
                    // Extract the pattern from the first constructor argument
                    if (declAttr.ConstructorArguments.Length > 0)
                    {
                        simpleNode.DeclPattern = declAttr.ConstructorArguments[0].Value?.ToString();
                    }
                    // For now, assume Local constraint - this can be enhanced to parse the actual constraint
                    simpleNode.DeclConstraint = DeclConstraintType.Local;
                }

                return simpleNode;
            }

            private ParameterNode BuildNodeFromType(ITypeSymbol typeSymbol, IParameterSymbol originalParameter, int index)
            {
                // Handle array types
                if (typeSymbol is IArrayTypeSymbol arrType)
                {
                    var elem = arrType.ElementType;
                    var elemNode = BuildNodeFromType(elem, originalParameter, index);
                    // Wrap into ArrayParameterNode
                    return new ArrayParameterNode
                    {
                        ParameterId = _nextNodeId++,
                        ParameterName = originalParameter.Name + "_array",
                        TargetType = typeSymbol,
                        Parameter = originalParameter,
                        ParameterIndex = index,
                        IsOptional = originalParameter.IsOptional,
                        IsTrailingParams = false,
                        IsRequired = false,
                        ElementNode = elemNode
                    };
                }

                // Check for ZilSequenceParam on the type
                var hasZilSequenceParam = typeSymbol.GetAttributes().Any(a =>
                    a.AttributeClass?.Name == "ZilSequenceParamAttribute" || a.AttributeClass?.Name == "ZilSequenceParam");
                if (hasZilSequenceParam)
                {
                    // debugLog.Add($"BuildNodeFromType(IParameterSymbol): {originalParameter.Name} is ZilSequenceParam, target+actual type {typeSymbol.ToDisplayString()}, isNullable={originalParameter.NullableAnnotation == NullableAnnotation.Annotated}");
                    return new CustomSequenceParameterNode
                    {
                        ParameterId = _nextNodeId++,
                        ParameterName = originalParameter.Name,
                        TargetType = typeSymbol,
                        Parameter = originalParameter,
                        ParameterIndex = index,
                        IsOptional = originalParameter.IsOptional,
                        StructureType = (INamedTypeSymbol)typeSymbol,
                        IsArray = false,
                        IsNullable = originalParameter.NullableAnnotation == NullableAnnotation.Annotated
                    };
                }

                // Check for ZilStructuredParam on the type
                var hasZilStructuredParam = typeSymbol.GetAttributes().Any(a =>
                    a.AttributeClass?.Name == "ZilStructuredParamAttribute" || a.AttributeClass?.Name == "ZilStructuredParam");
                if (hasZilStructuredParam)
                {
                    // debugLog.Add($"BuildNodeFromType(IParameterSymbol): {originalParameter.Name} is ZilStructuredParam, target+actual type {typeSymbol.ToDisplayString()}, isNullable={originalParameter.NullableAnnotation == NullableAnnotation.Annotated}");
                    return new CustomStructuredParameterNode
                    {
                        ParameterId = _nextNodeId++,
                        ParameterName = originalParameter.Name,
                        TargetType = typeSymbol,
                        Parameter = originalParameter,
                        ParameterIndex = index,
                        StructureType = (INamedTypeSymbol)typeSymbol,
                        IsArray = false,
                        IsNullable = originalParameter.NullableAnnotation == NullableAnnotation.Annotated,
                        IsOptional = originalParameter.IsOptional,
                        IsRequired = originalParameter.GetAttributes().Any(ad => ad.AttributeClass?.Name == "RequiredAttribute")
                    };
                }

                // Fallback: simple parameter node
                return new SimpleParameterNode
                {
                    ParameterId = _nextNodeId++,
                    ParameterName = originalParameter.Name,
                    TargetType = typeSymbol,
                    Parameter = originalParameter,
                    ParameterIndex = index,
                    IsOptional = originalParameter.IsOptional
                };
            }

            public ParameterNode[] BuildTree(IFieldSymbol[] fields)
            {
                var nodes = new List<ParameterNode>();

                for (int i = 0; i < fields.Length; i++)
                {
                    var field = fields[i];
                    var node = BuildNode(field, i);
                    nodes.Add(node);
                }

                return [.. nodes];
            }

            // TODO: combine this with the IParameterSymbol version above
            public ParameterNode BuildNode(IFieldSymbol field, int index)
            {
                var typeName = field.Type.Name;

                // Handle LocalEnvironment parameters - they're special
                if (typeName == "LocalEnvironment")
                {
                    return new LocalEnvironmentParameterNode
                    {
                        ParameterId = _nextNodeId++,
                        ParameterName = field.Name,
                        TargetType = field.Type,
                        Field = field,
                        ParameterIndex = index,
                        IsOptional = true // LocalEnvironment is implicitly optional
                    };
                }

                bool isOptional = field.GetAttributes().Any(a => a.AttributeClass?.Name == "ZilOptionalAttribute" || a.AttributeClass?.Name == "ZilOptional");

                // Check for ZilSequenceParam structures
                var paramType = field.Type;
                var actualType = paramType;
                bool isArray = false, isNullable = false;

                if (paramType is IArrayTypeSymbol arrayType)
                {
                    actualType = arrayType.ElementType;
                    isArray = true;

                    // Check if it's a nullable array (like AdditionalSortParam[]?)
                    isNullable = field.NullableAnnotation == NullableAnnotation.Annotated;
                }

                // Check if the type (or array element type) has ZilSequenceParamAttribute
                var hasZilSequenceParam = actualType.GetAttributes().Any(a =>
                    a.AttributeClass?.Name == "ZilSequenceParamAttribute" ||
                    a.AttributeClass?.Name == "ZilSequenceParam");

                if (hasZilSequenceParam)
                {
                    // debugLog.Add($"BuildNode(IFieldSymbol): {field.Name} is ZilSequenceParam, target type {field.Type.ToDisplayString()}, actual type {actualType.ToDisplayString()}, isArray={isArray}, isNullable={isNullable}");
                    return new CustomSequenceParameterNode
                    {
                        ParameterId = _nextNodeId++,
                        ParameterName = field.Name,
                        TargetType = field.Type,
                        Field = field,
                        ParameterIndex = index,
                        IsOptional = isOptional,
                        StructureType = actualType,
                        IsArray = isArray,
                        IsNullable = isNullable
                    };
                }

                // Check if the type (or array element type) has ZilStructuredParamAttribute
                var hasZilStructuredParam = actualType.GetAttributes().Any(a =>
                    a.AttributeClass?.Name == "ZilStructuredParamAttribute" ||
                    a.AttributeClass?.Name == "ZilStructuredParam");

                if (hasZilStructuredParam)
                {
                    // debugLog.Add($"BuildNode(IFieldSymbol): {field.Name} is ZilStructuredParam, target type {field.Type.ToDisplayString()}, actual type {actualType.ToDisplayString()}, isArray={isArray}, isNullable={isNullable}");
                    return new CustomStructuredParameterNode
                    {
                        ParameterId = _nextNodeId++,
                        ParameterName = field.Name,
                        TargetType = actualType,
                        Field = field,
                        ParameterIndex = index,
                        StructureType = actualType,
                        IsArray = isArray,
                        IsNullable = isNullable,
                        IsOptional = isOptional,
                        IsRequired = field.GetAttributes().Any(ad => ad.AttributeClass?.Name == "RequiredAttribute")
                    };
                }

                // Check for [Either] attribute first, before handling arrays
                var eitherAttr = field.GetAttributes().FirstOrDefault(a =>
                    a.AttributeClass?.Name == "EitherAttribute" || a.AttributeClass?.Name == "Either");

                if (eitherAttr != null)
                {
                    // Handle Either parameters
                    var eitherNode = new EitherParameterNode
                    {
                        ParameterId = _nextNodeId++,
                        ParameterName = field.Name,
                        TargetType = actualType,
                        Field = field,
                        ParameterIndex = index,
                        IsOptional = isOptional
                    };

                    // Extract the types from the Either attribute
                    if (eitherAttr.ConstructorArguments.Length > 0 &&
                        eitherAttr.ConstructorArguments[0].Kind == Microsoft.CodeAnalysis.TypedConstantKind.Array)
                    {
                        var typeValues = eitherAttr.ConstructorArguments[0].Values;
                        foreach (var typeValue in typeValues)
                        {
                            if (typeValue.Value is ITypeSymbol typeSymbol)
                            {
                                // Build an appropriate node for the alternative type (handles arrays, structured/sequence types)
                                var altNode = BuildNodeFromType(typeSymbol, field, index);
                                altNode.ParameterName = field.Name + "_alt";
                                altNode.IsOptional = false; // Individual alternatives are not optional within Either
                                eitherNode.Alternatives.Add(altNode);
                            }
                        }
                    }

                    if (isArray)
                    {
                        // debugLog.Add($"ParameterTreeBuilder: wrapping Either as ArrayParameterNode for field {field.Name} of type {field.Type.ToDisplayString()}");

                        // TODO: this should be handled by BuildNodeFromType...?
                        return new ArrayParameterNode
                        {
                            ParameterId = _nextNodeId++,
                            ParameterName = field.Name,
                            TargetType = field.Type,
                            Field = field,
                            ParameterIndex = index,
                            IsOptional = isOptional,
                            IsTrailingParams = false,
                            IsRequired = false,
                            ElementNode = eitherNode,
                            HasDeclConstraint = false,
                            DeclPattern = null
                        };
                    }

                    // debugLog.Add($"ParameterTreeBuilder: created EitherParameterNode for field {field.Name} of type {field.Type.ToDisplayString()}");
                    return eitherNode;
                }

                // Handle array parameters
                if (field.Type is IArrayTypeSymbol arrType)
                {
                    var elementType = arrType.ElementType;
                    // Build a proper element node (may be structured/sequence/etc.)
                    var elementNode = BuildNodeFromType(elementType, field, index);
                    elementNode.ParameterName = $"{field.Name}_element";
                    elementNode.Field = field; // element node refers back to the parent parameter for defaults
                    elementNode.ParameterIndex = index;

                    // For array parameters, determine if required based on ZILF semantics
                    // Arrays are required only if they are params arrays OR have explicit [Required] attribute
                    var hasRequiredAttribute = field.GetAttributes().Any(a => a.AttributeClass?.Name == "RequiredAttribute");
                    var isArrayRequired = /*parameter.IsParams ||*/ hasRequiredAttribute;

                    // Extract [Decl] attribute if present on the array parameter
                    var arrayDeclAttr = field.GetAttributes().FirstOrDefault(a => a.AttributeClass?.Name == "DeclAttribute");
                    bool hasDeclConstraint = arrayDeclAttr != null;
                    string? declPattern = null;
                    if (arrayDeclAttr?.ConstructorArguments.Length > 0)
                    {
                        declPattern = arrayDeclAttr.ConstructorArguments[0].Value?.ToString();
                    }

                    return new ArrayParameterNode
                    {
                        ParameterId = _nextNodeId++,
                        ParameterName = field.Name,
                        TargetType = field.Type,
                        Field = field,
                        ParameterIndex = index,
                        IsOptional = isOptional,
                        IsRequired = isArrayRequired,
                        ElementNode = elementNode,
                        HasDeclConstraint = hasDeclConstraint,
                        DeclPattern = declPattern
                    };
                }

                // Handle simple parameters
                var simpleNode = new SimpleParameterNode
                {
                    ParameterId = _nextNodeId++,
                    ParameterName = field.Name,
                    TargetType = field.Type,
                    Field = field,
                    ParameterIndex = index,
                    IsOptional = isOptional
                };

                // Check for [Decl] constraints
                var declAttr = field.GetAttributes().FirstOrDefault(a =>
                    a.AttributeClass?.Name == "DeclAttribute" || a.AttributeClass?.Name == "Decl");
                if (declAttr != null)
                {
                    simpleNode.HasDeclConstraint = true;
                    // Extract the pattern from the first constructor argument
                    if (declAttr.ConstructorArguments.Length > 0)
                    {
                        simpleNode.DeclPattern = declAttr.ConstructorArguments[0].Value?.ToString();
                    }
                    // For now, assume Local constraint - this can be enhanced to parse the actual constraint
                    simpleNode.DeclConstraint = DeclConstraintType.Local;
                }

                return simpleNode;
            }

            private ParameterNode BuildNodeFromType(ITypeSymbol typeSymbol, IFieldSymbol originalField, int index)
            {
                bool isOptional = originalField.GetAttributes().Any(a => a.AttributeClass?.Name == "ZilOptionalAttribute" || a.AttributeClass?.Name == "ZilOptional");

                // Handle array types
                if (typeSymbol is IArrayTypeSymbol arrType)
                {
                    var elem = arrType.ElementType;
                    var elemNode = BuildNodeFromType(elem, originalField, index);
                    // Wrap into ArrayParameterNode
                    return new ArrayParameterNode
                    {
                        ParameterId = _nextNodeId++,
                        ParameterName = originalField.Name + "_array",
                        TargetType = typeSymbol,
                        Field = originalField,
                        ParameterIndex = index,
                        IsOptional = isOptional,
                        IsTrailingParams = false,
                        IsRequired = false,
                        ElementNode = elemNode
                    };
                }

                // Check for ZilSequenceParam on the type
                var hasZilSequenceParam = typeSymbol.GetAttributes().Any(a =>
                    a.AttributeClass?.Name == "ZilSequenceParamAttribute" || a.AttributeClass?.Name == "ZilSequenceParam");
                if (hasZilSequenceParam)
                {
                    // debugLog.Add($"BuildNodeFromType(IFieldSymbol): {originalField.Name} is ZilSequenceParam, target+actual type {originalField.Type.ToDisplayString()}, isNullable={originalField.NullableAnnotation == NullableAnnotation.Annotated}");

                    return new CustomSequenceParameterNode
                    {
                        ParameterId = _nextNodeId++,
                        ParameterName = originalField.Name,
                        TargetType = typeSymbol,
                        Field = originalField,
                        ParameterIndex = index,
                        IsOptional = isOptional,
                        StructureType = (INamedTypeSymbol)typeSymbol,
                        IsArray = false,
                        IsNullable = originalField.NullableAnnotation == NullableAnnotation.Annotated
                    };
                }

                // Check for ZilStructuredParam on the type
                var hasZilStructuredParam = typeSymbol.GetAttributes().Any(a =>
                    a.AttributeClass?.Name == "ZilStructuredParamAttribute" || a.AttributeClass?.Name == "ZilStructuredParam");
                if (hasZilStructuredParam)
                {
                    // debugLog.Add($"BuildNodeFromType(IFieldSymbol): {originalField.Name} is ZilStructuredParam, target+actual type {originalField.Type.ToDisplayString()}, isNullable={originalField.NullableAnnotation == NullableAnnotation.Annotated}");

                    return new CustomStructuredParameterNode
                    {
                        ParameterId = _nextNodeId++,
                        ParameterName = originalField.Name,
                        TargetType = typeSymbol,
                        Field = originalField,
                        ParameterIndex = index,
                        StructureType = (INamedTypeSymbol)typeSymbol,
                        IsArray = false,
                        IsNullable = originalField.NullableAnnotation == NullableAnnotation.Annotated,
                        IsOptional = isOptional,
                        IsRequired = originalField.GetAttributes().Any(ad => ad.AttributeClass?.Name == "RequiredAttribute")
                    };
                }

                // Fallback: simple parameter node
                return new SimpleParameterNode
                {
                    ParameterId = _nextNodeId++,
                    ParameterName = originalField.Name,
                    TargetType = typeSymbol,
                    Field = originalField,
                    ParameterIndex = index,
                    IsOptional = isOptional
                };
            }
        }

        public enum DeclConstraintType
        {
            Local,
            Global,
            Either
        }

        #endregion
    }
}









