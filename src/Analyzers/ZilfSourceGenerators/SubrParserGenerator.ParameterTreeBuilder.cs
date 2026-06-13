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

namespace ZilfSourceGenerators;

partial class SubrParserGenerator
{
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
                        ,
                    IsRequired = parameter.GetAttributes().Any(ad => ad.AttributeClass?.Name == "RequiredAttribute")
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
}
