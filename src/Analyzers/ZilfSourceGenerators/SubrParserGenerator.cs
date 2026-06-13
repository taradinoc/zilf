/* Copyright 2010-2026 Tara McGrew
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
    public partial class SubrParserGenerator : IIncrementalGenerator
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

        /// <summary>
        /// Gets the expected type string for a parameter node, including any <c>[Decl]</c> constraint
        /// pattern if present on a <see cref="SimpleParameterNode"/>.
        /// </summary>
        private static string GetExpectedTypeString(ParameterNode node)
        {
            var display = BuildExpectedTypeDisplay(node.GetErrorExpectedTypes(), node.GetExpectedTypeName());
            if (node is SimpleParameterNode simple && simple.HasDeclConstraint && !string.IsNullOrEmpty(simple.DeclPattern))
                display = display + " matching " + simple.DeclPattern;
            return display;
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
    }
}









