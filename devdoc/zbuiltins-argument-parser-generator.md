# ZBuiltins Argument Parser Generator

## Overview

The ZBuiltins Argument Parser Generator is a Roslyn incremental source generator that replaces the reflection-based runtime argument parsing in ZILF's compiler built-in functions. It analyzes methods marked with `[Builtin]` attributes at compile time and generates strongly-typed argument parsers that eliminate the need for runtime reflection, enabling support for trimming scenarios.

The generator produces a single file `GeneratedBuiltinParsers.g.cs` containing:
- Individual parser methods for each builtin function and call type combination
- A static lookup dictionary mapping builtin names and call types to parser delegates
- Helper functions for common operations like variable resolution

## Architecture

### High-Level Flow

1. **Discovery**: Find all methods with `[Builtin]` attributes in the ZBuiltins class
2. **Analysis**: Analyze method signatures, parameters, and attributes to understand argument requirements
3. **Grouping**: Group overloads by builtin name and call type (ValueCall, VoidCall, PredCall, etc.)
4. **Generation**: Generate parser methods that handle argument validation, conversion, and dispatch
5. **Registration**: Generate lookup dictionaries for runtime parser resolution

### Key Components

```csharp
[Generator]
public class ArgumentParserGenerator : IIncrementalGenerator
{
    public void Initialize(IncrementalGeneratorInitializationContext context)
    {
        // Set up incremental pipeline for builtin method discovery and generation
    }
}
```

## Data Structures

### BuiltinMethodInfo

Represents a discovered builtin method with all its metadata:

```csharp
public record BuiltinMethodInfo
{
    public string MethodName { get; init; }
    public string BuiltinName { get; init; }         // From [Builtin("name")] attribute
    public string? CallType { get; init; }           // ValueCall, VoidCall, PredCall, etc.
    public List<ParameterInfo> Parameters { get; init; }
    public bool HasSideEffect { get; init; }         // From [Builtin] attribute
    public int MinVersion { get; init; }             // Z-machine version constraints
    public int MaxVersion { get; init; }
    public int Priority { get; init; }               // Overload resolution priority
}
```

### ParameterInfo

Detailed information about a method parameter:

```csharp
public record ParameterInfo
{
    public string Name { get; init; }
    public string TypeName { get; init; }
    public bool IsOptional { get; init; }
    public bool IsParams { get; init; }              // params arrays
    public bool IsVariable { get; init; }            // Has [Variable] attribute
    public bool IsRoutine { get; init; }             // Has [Routine] attribute  
    public bool IsData { get; init; }                // Has [Data] attribute
    public object? DataValue { get; init; }          // [Data] attribute value
    public VariableScopeQuirks ScopeQuirks { get; init; } // [Variable] scope constraints
}
```

### OverloadGroup

Groups method overloads by builtin name and call type:

```csharp
public record OverloadGroup
{
    public string BuiltinName { get; init; }
    public string CallType { get; init; }
    public List<BuiltinMethodInfo> Methods { get; init; }
    public bool HasVariableDispatch { get; init; }   // Needs variable resolution logic
    public bool HasVersionDispatch { get; init; }    // Needs Z-machine version dispatch
}
```

## Core Generation Methods

### Method Discovery: `GetBuiltinMethods`

Scans the ZBuiltins class for methods with `[Builtin]` attributes and extracts metadata:

```csharp
private static IEnumerable<BuiltinMethodInfo> GetBuiltinMethods(INamedTypeSymbol zBuiltinsClass)
{
    foreach (var method in zBuiltinsClass.GetMembers().OfType<IMethodSymbol>())
    {
        // Find [Builtin] attributes
        var builtinAttrs = method.GetAttributes()
            .Where(attr => attr.AttributeClass?.Name == "BuiltinAttribute");
        
        foreach (var attr in builtinAttrs)
        {
            // Extract builtin name, version constraints, priority, etc.
            // Analyze parameters for types, attributes, constraints
            // Determine call type from first parameter (ValueCall, VoidCall, etc.)
            yield return new BuiltinMethodInfo { /* ... */ };
        }
    }
}
```

### Parameter Analysis: `AnalyzeParameter`

Examines individual method parameters to extract type information and attributes:

```csharp
private static ParameterInfo AnalyzeParameter(IParameterSymbol parameter)
{
    var info = new ParameterInfo
    {
        Name = parameter.Name,
        TypeName = parameter.Type.ToDisplayString(),
        IsOptional = parameter.HasExplicitDefaultValue,
        IsParams = parameter.IsParams
    };
    
    // Check for special attributes
    foreach (var attr in parameter.GetAttributes())
    {
        switch (attr.AttributeClass?.Name)
        {
            case "VariableAttribute":
                info.IsVariable = true;
                info.ScopeQuirks = ExtractScopeQuirks(attr);
                break;
            case "RoutineAttribute":
                info.IsRoutine = true;
                break;
            case "DataAttribute":
                info.IsData = true;
                info.DataValue = attr.ConstructorArguments[0].Value;
                break;
        }
    }
    
    return info;
}
```

### Parser Generation: `GenerateParser`

Creates the main parser method for an overload group:

```csharp
private static void GenerateParser(IndentedStringBuilder sb, OverloadGroup group)
{
    // Method signature
    sb.AppendLine($"internal static {GetReturnType(group)} Generated_{SafeName(group.BuiltinName)}_{group.CallType}_Parser({group.CallType} c, ZilObject[] args)");
    sb.AppendLine("{");
    sb.Indent();
    
    if (group.Methods.Count == 1)
    {
        GenerateSimpleParser(sb, group.Methods[0]);
    }
    else if (group.HasVariableDispatch)
    {
        GenerateVariableDispatchParser(sb, group);
    }
    else if (group.HasVersionDispatch)
    {
        GenerateVersionDispatchParser(sb, group);
    }
    else
    {
        GenerateMultipleOverloadParser(sb, group);
    }
    
    sb.Unindent();
    sb.AppendLine("}");
}
```

### Argument Validation: `GenerateArgumentValidation`

Handles argument count validation with support for optional parameters and params arrays:

```csharp
private static void GenerateArgumentValidation(IndentedStringBuilder sb, ParameterInfo paramInfo, string builtinName, string returnType, string callType)
{
    if (paramInfo.HasParamsArray)
    {
        // Minimum argument count check for params arrays
        sb.AppendLine($"if (args.Length < {paramInfo.RequiredArgumentCount})");
        sb.AppendLine("{");
        sb.Indent();
        GenerateErrorReturn(sb, builtinName, $"{paramInfo.RequiredArgumentCount}+", returnType, callType);
        sb.Unindent();
        sb.AppendLine("}");
    }
    else if (paramInfo.HasOptionalParameters)
    {
        // Range check for optional parameters
        sb.AppendLine($"if (args.Length < {paramInfo.RequiredArgumentCount} || args.Length > {paramInfo.TotalArgumentCount})");
        // ... generate error handling
    }
    else
    {
        // Exact count check
        sb.AppendLine($"if (args.Length != {paramInfo.RequiredArgumentCount})");
        // ... generate error handling
    }
}
```

### Operand Compilation: `GenerateOperandCompilation`

Creates the `CompileOperands` call with macro unwrapping for arguments that need to be compiled to IOperand:

```csharp
private static void GenerateOperandCompilation(IndentedStringBuilder sb, List<ParameterInfo> parameters, string builtinName)
{
    // Identify which parameters need operand compilation
    var operandParams = parameters.Where(p => NeedsOperandCompilation(p)).ToList();
    
    if (operandParams.Any())
    {
        // Generate operand slice expression with macro unwrapping
        var operandSlice = GenerateOperandSliceExpression(parameters, operandParams);
        
        sb.AppendLine($"using (var operands = c.cc.CompileOperands(c.rb, c.form.SourceLine, {operandSlice}))");
        sb.AppendLine("{");
        sb.Indent();
        
        // Generate parameter assignment logic
        GenerateParameterAssignments(sb, parameters);
        
        // Generate the actual method call
        GenerateMethodCall(sb, builtinName, parameters);
        
        sb.Unindent();
        sb.AppendLine("}");
    }
    else
    {
        // Direct call without operand compilation
        GenerateMethodCall(sb, builtinName, parameters);
    }
}
```

### Variable Dispatch: `GenerateVariableDispatch`

Handles complex dispatch logic for builtins that support both variable references and operand expressions:

```csharp
private static void GenerateVariableDispatch(IndentedStringBuilder sb, OverloadGroup group)
{
    var variableParam = group.Methods[0].Parameters.First(p => p.IsVariable);
    var variableIndex = group.Methods[0].Parameters.IndexOf(variableParam);
    
    // Generate variable lookup
    sb.AppendLine($"var variableRef = GetVariable(c.cc, args[{variableIndex}], {GetScopeQuirks(variableParam)});");
    sb.AppendLine("if (variableRef == null)");
    sb.AppendLine("{");
    sb.Indent();
    
    // Handle non-variable case (operand fallback or error)
    GenerateNonVariableFallback(sb, group, variableIndex);
    
    sb.Unindent();
    sb.AppendLine("}");
    
    // Generate variable type dispatch (Hard vs Soft)
    GenerateVariableTypeDispatch(sb, group);
}
```

## Key Features

### Macro Unwrapping

The generator automatically handles `ZilMacroResult` unwrapping throughout the parsing pipeline:

```csharp
private static string GetUnwrappedArgExpression(string argExpr)
{
    return $"({argExpr} is ZilMacroResult ? (({typeof(ZilMacroResult).FullName}){argExpr}).Inner : {argExpr})";
}
```

This ensures that macro results are properly unwrapped before:
- Variable name resolution (`IsLVAL`, `IsGVAL` checks)
- Operand compilation (`CompileOperands`)
- Direct argument passing to builtin methods

### Variable Resolution

The generator includes a `GetVariable` helper function that replicates `ParameterTypeHandler.GetVariable` behavior:

```csharp
private static Zilf.Compiler.VariableRef? GetVariable(Compilation cc, ZilObject expr, VariableScopeQuirks quirks)
{
    // Allow bare atoms or <GVAL>/<LVAL> forms depending on quirks
    if (expr is not ZilAtom atom &&
        ((quirks & VariableScopeQuirks.Global) == 0 || !expr.IsGVAL(out atom!)) &&
        ((quirks & VariableScopeQuirks.Local) == 0 || !expr.IsLVAL(out atom!)))
    {
        return null;
    }
    
    // Variable resolution with scope preference based on quirks
    // ... lookup in cc.Locals, cc.Globals, cc.SoftGlobals
}
```

### Error Handling

The generator produces call-type-appropriate error handling:

- **ValueCall/VoidCall**: Use `c.HandleMessage()` for parser errors
- **PredCall/ValuePredCall**: Use `c.cc.Context.HandleError()` for compile errors
- **Bare Atom Validation**: Error when variable parameters receive invalid bare atoms

### Incremental Generation

The generator uses Roslyn's incremental generation pipeline to minimize regeneration:

```csharp
public void Initialize(IncrementalGeneratorInitializationContext context)
{
    var builtinMethods = context.SyntaxProvider
        .CreateSyntaxProvider(static (node, _) => node is ClassDeclarationSyntax,
                             static (context, _) => GetBuiltinMethods(context))
        .Where(method => method is not null)
        .Collect();

    context.RegisterSourceOutput(builtinMethods, 
        static (context, methods) => GenerateParsers(context, methods));
}
```

## Generated Code Structure

The final generated file contains:

1. **Individual Parser Methods**: One per builtin name + call type combination
2. **Lookup Dictionary**: Maps `(builtin_name, call_type)` to parser delegate
3. **Helper Functions**: `GetVariable` for variable resolution
4. **Using Statements**: All necessary namespace imports

Example structure:
```csharp
namespace Zilf.Compiler.Builtins
{
    internal static partial class GeneratedBuiltinParsers
    {
        // Individual parser methods
        internal static IOperand Generated_ADD_ValueCall_Parser(ValueCall c, ZilObject[] args) { /* ... */ }
        internal static void Generated_SET_VoidCall_Parser(VoidCall c, ZilObject[] args) { /* ... */ }
        
        // Lookup dictionary
        internal static readonly Dictionary<(string, Type), Delegate> ParserLookup = new()
        {
            { ("ADD", typeof(ValueCall)), (Func<ValueCall, ZilObject[], IOperand>)Generated_ADD_ValueCall_Parser },
            { ("SET", typeof(VoidCall)), (Action<VoidCall, ZilObject[]>)Generated_SET_VoidCall_Parser },
            // ...
        };
        
        // Helper functions
        private static VariableRef? GetVariable(Compilation cc, ZilObject expr, VariableScopeQuirks quirks) { /* ... */ }
    }
}
```

This architecture provides complete replacement of the reflection-based argument parsing while maintaining full compatibility with existing builtin method signatures and semantics.