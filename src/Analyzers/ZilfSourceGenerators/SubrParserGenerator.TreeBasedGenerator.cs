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

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace ZilfSourceGenerators;

partial class SubrParserGenerator
{
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
                                parts.Add($"\"{s.Replace("\\", "\\\\").Replace("\"", "\\\"")}\"");
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
                                parts.Add($"{named.Key} = \"{s.Replace("\\", "\\\\").Replace("\"", "\\\"")}\"");
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
                    var expectedDisplay = GetExpectedTypeString(optNode);
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
                        var expectedDisplay = GetExpectedTypeString(optionalNode);
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
                        var expectedDisplay = GetExpectedTypeString(optionalNode);
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
}
