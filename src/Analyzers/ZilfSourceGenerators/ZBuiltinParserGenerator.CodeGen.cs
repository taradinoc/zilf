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
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;

namespace ZilfSourceGenerators
{
    public partial class ZBuiltinParserGenerator
    {
        private static void GenerateSingleOverloadBody(IndentedStringBuilder sb, OverloadInfo overload, string callType, string operationName, bool emitVersionGuard = true)
        {
            var method = overload.Method;
            var attr = overload.Attribute;
            var parameters = method.MethodSymbol.Parameters;

            if (emitVersionGuard && (attr.MinVersion.HasValue || attr.MaxVersion.HasValue))
            {
                // Emit a runtime guard for Z-machine version applicability. If this overload
                // does not apply to the current Z-machine version, report a helpful error
                // and return (matching prior reflection-based behavior). Return values must
                // match the parser method's return type: ValueCall and ValuePredCall return
                // an IOperand; PredCall/VoidCall return void.
                var minVerLiteral = (attr.MinVersion ?? 1).ToString(CultureInfo.InvariantCulture);
                var maxVerLiteral = attr.MaxVersion.HasValue
                    ? attr.MaxVersion.Value.ToString(CultureInfo.InvariantCulture)
                    : "null";
                var maxVerComment = attr.MaxVersion.HasValue
                    ? attr.MaxVersion.Value.ToString(CultureInfo.InvariantCulture)
                    : "infinity";
                sb.AppendLine($"// Version guard: applies to versions {minVerLiteral}..{maxVerComment}");
                sb.AppendLine($"if (!Zilf.ZModel.ZEnvironment.VersionMatches(c.cc.Context.ApparentZVersion, {minVerLiteral}, {maxVerLiteral}))");
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
                // Emit a CompilerError (ZIL0113) when an argument expected to be an atom is not an atom.
                sb.Append($"{indent}({argExpression} as ZilAtom ?? throw new CompilerError(c.form, CompilerMessages._0_Argument_1_2, \"{operationName}\", {argIndex}, \"must be an atom\"))");
            }
            else if (paramType.Name == "Block")
            {
                // Resolve an activation atom (LVAL) to a Block instance, mirroring ParameterTypeHandler.BlockHandler
                // Generated code will evaluate the argument expression, check IsLVAL and lookup in c.cc.Blocks
                sb.Append($"{indent}({argExpression} is ZilObject _blockArg && _blockArg.IsLVAL(out var __act) ? (c.cc.Blocks.FirstOrDefault(b => b.Name == __act) ?? throw new CompilerError(c.form, CompilerMessages._0_Argument_1_2, \"{operationName}\", {argIndex}, \"must be bound to a block\")) : throw new CompilerError(c.form, CompilerMessages._0_Argument_1_2, \"{operationName}\", {argIndex}, \"must be a local variable reference\"))");
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
                sb.Append($"{indent}({argExpression} is ZilString _zs ? _zs.Text : throw new CompilerError(c.form, CompilerMessages._0_Argument_1_2, \"{operationName}\", {argIndex}, \"must be a string\")");
            }
            else if (paramType.SpecialType == SpecialType.System_Boolean)
            {
                sb.Append($"{indent}({argExpression} is ZilAtom atom && atom == StdAtom.T)");
            }
            else if (paramType.SpecialType == SpecialType.System_Int32)
            {
                // Require a ZilFix for integer parameters; emit a CompilerError if not provided.
                sb.Append($"{indent}({argExpression} is ZilFix fix ? fix.Value : throw new CompilerError(c.form, CompilerMessages._0_Argument_1_2, \"{operationName}\", {argIndex}, \"must be a number\"))");
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
    }
}
