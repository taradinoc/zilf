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

using System.Collections.Generic;
using System.Linq;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;

namespace ZilfSourceGenerators;

partial class SubrParserGenerator
{
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
}
