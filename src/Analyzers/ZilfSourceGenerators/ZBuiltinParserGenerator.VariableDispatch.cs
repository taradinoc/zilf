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
using System.Collections.Generic;
using System.Linq;

namespace ZilfSourceGenerators
{
    public partial class ZBuiltinParserGenerator
    {
        private static bool HasVariableAttribute(IParameterSymbol param)
        {
            return param.GetAttributes().Any(a => a.AttributeClass?.Name == "VariableAttribute");
        }

        private static bool HasSoftGlobalParameter(OverloadInfo overload)
        {
            var argsParams = overload.Method.ArgumentParameters;
            return argsParams.Any(p => HasVariableAttribute(p) && p.Type.Name == "SoftGlobal");
        }

        private static bool HasIVariableParameter(OverloadInfo overload)
        {
            var argsParams = overload.Method.ArgumentParameters;
            return argsParams.Any(p => HasVariableAttribute(p) && IsIVariableType(p.Type));
        }

        private static bool HasIOperandParameter(OverloadInfo overload)
        {
            var argsParams = overload.Method.ArgumentParameters;
            // Consider IOperand parameters that have [Variable] attribute - these expect variable.Hard.Indirect
            return argsParams.Any(p => HasVariableAttribute(p) && IsIOperandType(p.Type));
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
            var firstParams = firstOverload.Method.ArgumentParameters;

            foreach (var otherOverload in overloads.Skip(1))
            {
                var otherParams = otherOverload.Method.ArgumentParameters;

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
                o.Method.ArgumentParameters.Any(p => IsIVariableType(p.Type)));

            // Look for IOperand with [Variable] overload
            var iOperandVariableOverload = group.Overloads.FirstOrDefault(o =>
                o.Method.ArgumentParameters.Any(p => IsIOperandType(p.Type) && HasVariableAttribute(p)));

            // Look for SoftGlobal overload
            var softGlobalOverload = group.Overloads.FirstOrDefault(o =>
                o.Method.ArgumentParameters.Any(p => p.Type.Name == "SoftGlobal"));

            // Look for plain IOperand overload (where the FIRST operand/variable-like parameter is plain IOperand without [Variable])
            var plainOperandOverload = group.Overloads.FirstOrDefault(o =>
            {
                var params_ = o.Method.ArgumentParameters;
                // Find first parameter that could be a variable or operand
                var firstVarOrOpParam = params_.FirstOrDefault(p =>
                    IsIVariableType(p.Type) || p.Type.Name == "SoftGlobal" || IsIOperandType(p.Type));
                // It's a plain operand overload if the first such parameter is IOperand without [Variable]
                return firstVarOrOpParam != null && IsIOperandType(firstVarOrOpParam.Type) && !HasVariableAttribute(firstVarOrOpParam);
            });

            // Determine what kind of dispatch we need
            if (iVariableOverload != null && softGlobalOverload != null && plainOperandOverload != null)
            {
                // Three-way dispatch: IVariable vs SoftGlobal vs Plain Operand
                GenerateThreeWayVariableDispatch(sb, group, iVariableOverload, softGlobalOverload, plainOperandOverload);
            }
            else if ((iVariableOverload != null || iOperandVariableOverload != null || softGlobalOverload != null) && plainOperandOverload != null)
            {
                // Variable/SoftGlobal vs Plain Operand - dispatch between variable reference and operand expression
                var variableOverload = iVariableOverload ?? iOperandVariableOverload ?? softGlobalOverload;
                GenerateVariableVsOperandDispatch(sb, group, variableOverload!, plainOperandOverload);
            }
            else if (iVariableOverload != null && iOperandVariableOverload != null)
            {
                // IVariable vs IOperand with [Variable] - both are hard variables, dispatch based on variable type
                GenerateIVariableVsIOperandDispatch(sb, group, iVariableOverload, iOperandVariableOverload);
            }
            else if ((iVariableOverload != null || iOperandVariableOverload != null) && softGlobalOverload != null)
            {
                // Variable vs SoftGlobal - dispatch based on IsHard vs Soft (no plain operand fallback)
                var variableOverload = iVariableOverload ?? iOperandVariableOverload;
                GenerateVariableVsSoftGlobalDispatch(sb, group, variableOverload!, softGlobalOverload);
            }
            else
            {
                // Fallback to simple dispatch if we can't identify the overloads properly
                GenerateSingleOverloadBody(sb, group.Overloads[0], group.CallType, group.BuiltinName ?? "");
            }
        }

        private static void GenerateThreeWayVariableDispatch(IndentedStringBuilder sb, OverloadGroup group, OverloadInfo iVariableOverload, OverloadInfo softGlobalOverload, OverloadInfo plainOperandOverload)
        {
            // Three-way dispatch for cases like SET: IVariable + SoftGlobal + IOperand fallback
            var varParams = iVariableOverload.Method.ArgumentParameters;
            var variableParamIndex = -1;
            string? variableParamQuirks = null;

            for (int i = 0; i < varParams.Length; i++)
            {
                if (IsIVariableType(varParams[i].Type))
                {
                    variableParamIndex = i;

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

            sb.AppendLine($"var variableRef = GetVariable(c.cc, args[{variableParamIndex}], {variableParamQuirks});");
            sb.AppendLine("if (variableRef != null)");
            sb.AppendLine("{");
            sb.Indent();
            sb.AppendLine("if (variableRef.Value.IsHard)");
            sb.AppendLine("{");
            sb.Indent();
            GenerateOverloadCall(sb, iVariableOverload, group.CallType, "variableRef.Value.Hard", variableParamIndex);
            sb.Unindent();
            sb.AppendLine("}");
            sb.AppendLine("else");
            sb.AppendLine("{");
            sb.Indent();
            GenerateOverloadCall(sb, softGlobalOverload, group.CallType, "variableRef.Value.Soft", variableParamIndex);
            sb.Unindent();
            sb.AppendLine("}");
            sb.Unindent();
            sb.AppendLine("}");
            sb.AppendLine("else");
            sb.AppendLine("{");
            sb.Indent();
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
            GenerateOverloadCallWithOperandCompilation(sb, plainOperandOverload, group.CallType, variableParamIndex);
            sb.Unindent();
            sb.AppendLine("}");
            sb.Unindent();
            sb.AppendLine("}");
        }

        private static void GenerateIVariableVsIOperandDispatch(IndentedStringBuilder sb, OverloadGroup group, OverloadInfo iVariableOverload, OverloadInfo iOperandVariableOverload)
        {
            var varParams = iVariableOverload.Method.ArgumentParameters;
            var variableParamIndex = -1;
            string? variableParamQuirks = null;

            for (int i = 0; i < varParams.Length; i++)
            {
                if (IsIVariableType(varParams[i].Type))
                {
                    variableParamIndex = i;

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

            sb.AppendLine($"var variableRef = GetVariable(c.cc, args[{variableParamIndex}], {variableParamQuirks});");
            sb.AppendLine("if (variableRef != null)");
            sb.AppendLine("{");
            sb.Indent();
            GenerateOverloadCall(sb, iVariableOverload, group.CallType, "variableRef.Value.Hard", variableParamIndex);
            sb.Unindent();
            sb.AppendLine("}");
            sb.AppendLine("else");
            sb.AppendLine("{");
            sb.Indent();
            sb.AppendLine($"if (args[{variableParamIndex}] is ZilAtom || (args[{variableParamIndex}] is ZilMacroResult zmr_check && zmr_check.Inner is ZilAtom))");
            sb.AppendLine("{");
            sb.Indent();
            GenerateErrorThrow(sb, group.BuiltinName ?? "", variableParamIndex + 1, "bare atom argument must be a variable", group.CallType);
            sb.Unindent();
            sb.AppendLine("}");
            sb.AppendLine("else");
            sb.AppendLine("{");
            sb.Indent();
            GenerateOverloadCallWithOperandCompilation(sb, iOperandVariableOverload, group.CallType, variableParamIndex);
            sb.Unindent();
            sb.AppendLine("}");
            sb.Unindent();
            sb.AppendLine("}");
        }

        private static void GenerateVariableVsSoftGlobalDispatch(IndentedStringBuilder sb, OverloadGroup group, OverloadInfo variableOverload, OverloadInfo softGlobalOverload)
        {
            var varParams = variableOverload.Method.ArgumentParameters;
            var variableParamIndex = -1;
            string? variableParamQuirks = null;

            for (int i = 0; i < varParams.Length; i++)
            {
                if (IsIVariableType(varParams[i].Type) || (IsIOperandType(varParams[i].Type) && HasVariableAttribute(varParams[i])))
                {
                    variableParamIndex = i;

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

            sb.AppendLine($"var variableRef = GetVariable(c.cc, args[{variableParamIndex}], {variableParamQuirks});");
            sb.AppendLine("if (variableRef == null)");
            sb.AppendLine("{");
            sb.Indent();

            var variableParams = variableOverload.Method.ArgumentParameters;
            var variableParam = variableParams[variableParamIndex];

            if (IsIOperandType(variableParam.Type) && HasVariableAttribute(variableParam))
            {
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

            if (IsIOperandType(variableParam.Type))
            {
                GenerateOverloadCall(sb, variableOverload, group.CallType, "variableRef.Value.Hard.Indirect", variableParamIndex);
            }
            else
            {
                GenerateOverloadCall(sb, variableOverload, group.CallType, "variableRef.Value.Hard", variableParamIndex);
            }
            sb.Unindent();
            sb.AppendLine("}");
            sb.AppendLine("else");
            sb.AppendLine("{");
            sb.Indent();
            GenerateOverloadCall(sb, softGlobalOverload, group.CallType, "variableRef.Value.Soft", variableParamIndex);
            sb.Unindent();
            sb.AppendLine("}");
        }

        private static void GenerateVariableVsOperandDispatch(IndentedStringBuilder sb, OverloadGroup group, OverloadInfo variableOverload, OverloadInfo operandOverload)
        {
            var varParams = variableOverload.Method.ArgumentParameters;
            var operandParams = operandOverload.Method.ArgumentParameters;
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

            sb.AppendLine($"var variableRef = GetVariable(c.cc, args[{variableParamIndex}], {variableParamQuirks});");
            sb.AppendLine("if (variableRef != null)");
            sb.AppendLine("{");
            sb.Indent();

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
            var parameters = overload.Method.ArgumentParameters;

            var callArgs = new List<string> { "c" };

            foreach (var param in overload.Method.DataParameters)
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

            for (int i = 0; i < parameters.Length; i++)
            {
                if (i == operandParamIndex)
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
                else
                {
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
            var parameters = overload.Method.ArgumentParameters;

            var callArgs = new List<string> { "c" };

            foreach (var param in overload.Method.DataParameters)
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

            for (int i = 0; i < parameters.Length; i++)
            {
                if (i == variableParamIndex)
                {
                    var paramType = parameters[i].Type;
                    if (IsIOperandType(paramType) && HasVariableAttribute(parameters[i]))
                    {
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
    }
}
