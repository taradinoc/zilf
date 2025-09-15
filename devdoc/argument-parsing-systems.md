# ZILF Argument Parsing Systems: Comprehensive Specification

This document provides a detailed specification of the two reflection-based argument parsing systems in ZILF, preparing for their replacement with source generation to enable trimming support.

## Table of Contents

1. [Overview](#overview)
2. [Subrs System (Interpreter Commands)](#subrs-system-interpreter-commands)
3. [ZBuiltins System (Compiler Built-ins)](#zbuiltins-system-compiler-built-ins)
4. [Comparison and Common Patterns](#comparison-and-common-patterns)
5. [Error Handling](#error-handling)
6. [Type System](#type-system)
7. [Reflection Dependencies](#reflection-dependencies)
8. [Source Generation Requirements](#source-generation-requirements)

## Overview

ZILF uses two distinct argument parsing systems, both heavily dependent on reflection:

1. **Subrs System**: Interpreter commands (runtime evaluation) using `ArgDecoder`
2. **ZBuiltins System**: Compiler built-in routines (compile-time code generation) using `ParameterTypeHandler`

Both systems share similar patterns but serve different purposes and contexts within the ZILF toolchain.

## Subrs System (Interpreter Commands)

The Subrs system handles interpreter commands that execute during ZIL evaluation. It uses the `ArgDecoder` class with reflection-based method wrapping.

### Discovery and Registration

#### Initialization Process
```csharp
// Context.InitSubrs() - Called from Context constructor
void InitSubrs()
{
    var methods = typeof(Subrs).GetMethods(BindingFlags.Static | BindingFlags.Public);
    foreach (var mi in methods)
    {
        var attrs = mi.GetCustomAttributes<Subrs.SubrAttributeBase>(false).ToArray();
        if (attrs.Length == 0)
            continue;

        var del = ArgDecoder.WrapMethod(mi, this);

        foreach (var attr in attrs)
        {
            var baseName = attr.Name ?? mi.Name;
            var name = string.IsNullOrEmpty(attr.ObList) ? baseName : $"{baseName}!-{attr.ObList}";

            var isFSubr = attr is Subrs.FSubrAttribute;
            subrDelegates.Add(name, (del, mi, isFSubr));

            // these atoms need to be on the root oblist
            var atom = ZilAtom.Parse(name + "!-", this);
            SetGlobalVal(atom, isFSubr ? new ZilFSubr(baseName, del) : new ZilSubr(baseName, del));
        }
    }
}
```

#### Attribute System
```csharp
// Base class for all Subr attributes
[AttributeUsage(AttributeTargets.Method, AllowMultiple = true)]
public abstract class SubrAttributeBase : Attribute
{
    protected SubrAttributeBase(string? name)
    {
        Name = name;
    }

    public string? Name { get; }       // Override method name
    public string? ObList { get; set; } // Namespace/package qualifier
}

// Regular subroutine - evaluates all arguments
public sealed class SubrAttribute : SubrAttributeBase
{
    public SubrAttribute() : base(null) { }
    public SubrAttribute(string name) : base(name) { }
}

// Special form - controls argument evaluation
public sealed class FSubrAttribute : SubrAttributeBase 
{
    public FSubrAttribute() : base(null) { }
    public FSubrAttribute(string name) : base(name) { }
}
```

### Method Wrapping and Invocation

#### ArgDecoder.WrapMethod Pattern
The `ArgDecoder.WrapMethod` method creates delegates that handle argument parsing and validation:

```csharp
public static SubrDelegate WrapMethod(MethodInfo mi, Context ctx)
{
    // Creates a delegate that:
    // 1. Validates argument count and types
    // 2. Performs type conversions using ArgDecoder.Decode
    // 3. Handles optional parameters and default values
    // 4. Invokes the target method via MethodInfo.Invoke
    // 5. Wraps return values in ZilResult
    
    // Pattern: return (name, context, args) => { /* decode + invoke */ };
}
```

#### Invocation Flow
1. **Discovery**: `typeof(Subrs).GetMethods()` finds all public static methods
2. **Filtering**: Only methods with `SubrAttribute` or `FSubrAttribute` are processed
3. **Wrapping**: `ArgDecoder.WrapMethod` creates type-safe delegates
4. **Registration**: Delegates stored in `Context.subrDelegates` dictionary
5. **Symbol Creation**: ZIL atoms created and bound to `ZilSubr`/`ZilFSubr` objects
6. **Runtime Invocation**: Calls go through delegate → reflection → target method

### Parameter Types and Constraints

#### Supported Parameter Types (30+ types)
```csharp
// Primitive types
Context ctx             // Always first parameter
ZilObject               // Any ZIL value
ZilAtom                 // Atoms only  
ZilFix                  // Fixed-point numbers
ZilString               // Strings
ZilList                 // Lists
bool, int, short        // .NET primitives

// Collection types  
ZilObject[]             // Variable arguments (params)
ZilAtom[]               // Multiple atoms
string[]                // Multiple strings

// Specialized types
IStructure              // Vectors, lists, strings
IApplicable            // Functions, macros, subroutines
LocalEnvironment        // Environment for EVAL
ZilListBase            // Lists or dotted pairs

// Custom parameter classes (in Subrs.*.cs)
AtomParams.StringOrAtom        // String or atom
DeclParams.AtomsDeclSequence   // Atoms with declarations
DefineGlobalsParams.GlobalSpec // Global variable specs
WarningParams.CodesOrWildcard  // Warning codes
```

#### Parameter Attributes
```csharp
[Optional]              // Parameter is optional (legacy, same as C# optional)
[Required]              // For array parameters - requires at least one element
[Decl("<type-spec>")]   // ZIL type constraint 
[ParamDesc("name")]     // Documentation/debugging info

// Extended ArgDecoder attributes
[ZilOptional]           // More flexible optional parameter with default values
[Either(typeof(ZilAtom), typeof(ZilString))] // Parameter can be one of multiple types
[ZilStructuredParam(StdAtom.LIST)]          // For custom parameter class structures
[ZilSequenceParam]      // Marks parameter classes that handle sequences
```

#### Advanced Parameter Attributes

**ZilOptionalAttribute**: More sophisticated optional parameter handling
```csharp
[AttributeUsage(AttributeTargets.Field | AttributeTargets.Parameter)]
sealed class ZilOptionalAttribute : Attribute
{
    public object? Default { get; set; }  // Custom default value
}

// Usage example
public static ZilObject SUBR(Context ctx, [ZilOptional(Default = 42)] int count)
```

**EitherAttribute**: Union type support for parameters
```csharp
[AttributeUsage(AttributeTargets.Field | AttributeTargets.Parameter)]
sealed class EitherAttribute : Attribute
{
    public EitherAttribute(params Type[] types)
    {
        Types = types;
    }
    
    public Type[] Types { get; }
    public string? DefaultParamDesc { get; set; }
}

// Usage example - parameter accepts either atom or string
[Either(typeof(ZilAtom), typeof(ZilString), DefaultParamDesc = "name")]
```

**ZilStructuredParamAttribute**: For complex parameter structures
```csharp
[AttributeUsage(AttributeTargets.Struct)]
sealed class ZilStructuredParamAttribute : Attribute
{
    public ZilStructuredParamAttribute(StdAtom typeAtom)
    {
        TypeAtom = typeAtom;
    }
    
    public StdAtom TypeAtom { get; }
}

// Marks parameter classes that represent structured ZIL types
[ZilStructuredParam(StdAtom.LIST)]
public struct ListParam { /* fields */ }
```

**ZilSequenceParamAttribute**: For sequence parameter handling
```csharp
[AttributeUsage(AttributeTargets.Struct)]
sealed class ZilSequenceParamAttribute : Attribute
{
}

// Used for parameter classes that handle variable-length sequences
[ZilSequenceParam]
public struct SequenceParam { /* array handling */ }
```

#### Type Constraint Examples
```csharp
[Subr("PUTPROP")]
public static ZilObject PUTPROP(Context ctx, ZilObject item, ZilObject indicator,
    ZilObject? value = null)

[Subr("PRINTTYPE")] 
public static ZilObject PRINTTYPE(Context ctx, ZilAtom atom,
    [Decl("<OR ATOM APPLICABLE>")] ZilObject? handler = null)

[FSubr]
public static ZilObject ROUTINE(Context ctx, ZilAtom name,
    [Optional] ZilAtom? activationAtom, ZilList argList,
    [Required] ZilObject[] body)

// Advanced parameter examples using new attributes
[Subr("EXAMPLE-EITHER")]
public static ZilObject EXAMPLE_EITHER(Context ctx,
    [Either(typeof(ZilAtom), typeof(ZilString), DefaultParamDesc = "name")] ZilObject nameParam)

[Subr("EXAMPLE-OPTIONAL")]
public static ZilObject EXAMPLE_OPTIONAL(Context ctx,
    [ZilOptional(Default = 10)] int count,
    [ParamDesc("output destination")] ZilObject? dest = null)
```

### Error Handling Patterns

#### Argument Validation Errors
- **Count Mismatch**: "Expected N arguments, got M"
- **Type Mismatch**: "Expected ATOM, got STRING" 
- **Null Reference**: "Required parameter cannot be null"
- **Constraint Violation**: Type doesn't match `[Decl]` specification

### Alternative Parsing, Backtracking, and Error Attribution

The Subrs system's `ArgDecoder` supports advanced argument matching with backtracking, especially for parameters marked with `[Either]` (union types) and optional parameters. When decoding arguments:

- **Either/Union Parameters:** For parameters with `[Either]`, `ArgDecoder` tries each alternative type in order. If one fails (e.g., type mismatch or constraint violation), it backtracks and tries the next. If none succeed, it reports the error from the most promising alternative (the one that got furthest before failing), ensuring the error message is as specific as possible.
- **Optional Parameters:** If an optional parameter does not match, `ArgDecoder` skips it and tries to match subsequent parameters. If later parameters also fail, it may attribute the error to the skipped optional, depending on which step was the last to "underachieve" (i.e., could have matched more arguments but didn't).
- **Backtracking and Error Collection:** As it tries alternatives, `ArgDecoder` collects exceptions (subclasses of `ArgumentDecodingError`) from each failed branch. If all alternatives fail, it rethrows the most informative error (usually the last one tried that got furthest), so the user sees the most relevant expected type(s) and argument position.
- **Error Attribution:** If there are extra arguments or ambiguous failures, `ArgDecoder` uses heuristics to decide which parameter to blame. For example, if an optional parameter was skipped and extra arguments remain, it may attribute the error to the skipped parameter or to the first unmatched argument, depending on which step was the last to "underachieve". This helps produce error messages like "Expected ATOM at argument 3" or "Too many arguments, check types of earlier arguments (e.g., argument 2)".

**Example:**
Suppose a signature is `<FOO fix [atom]>` and the call is `<FOO fix string>`. The decoder will try to match the optional `[atom]` parameter, fail (since `string` is not an atom), skip it, and then see that there are extra arguments. It will then attribute the error to the skipped optional, reporting that an ATOM was expected at that position.

This backtracking and error collection logic ensures that, even with complex signatures involving unions and optionals, the error messages are as helpful and precise as possible.

#### Exception Types
```csharp
InterpreterError        // ZIL evaluation errors
ArgumentException       // .NET parameter errors
InvalidOperationException // Context/state errors
```

### Return Value Handling

#### Return Types
```csharp
ZilObject              // Direct value return
ZilResult              // Success/failure with value
void                   // Converted to ctx.TRUE
```

#### ZilResult Usage
```csharp
// Success case
return new ZilResult(someValue);

// Failure/pass-through case  
var zr = value.Eval(ctx);
if (zr.ShouldPass())
    return zr;
```

## ZBuiltins System (Compiler Built-ins)

The ZBuiltins system handles built-in routines that generate Z-code during compilation. It uses `ParameterTypeHandler` with reflection-based lookup tables.

### Signature Matching and Error Reporting (No Backtracking)

Unlike Subrs, the ZBuiltins system does **not** perform backtracking or alternative parsing at runtime. Instead:

- **Signature Selection:** At invocation, the system selects the best-matching signature (method overload) based on argument count and types. If multiple signatures could match, it uses priorities and version constraints to disambiguate.
- **Error Reporting:** If no signature matches exactly, it reports a diagnostic for the first argument that fails to match, or for the overall signature if the count is wrong. It does not try alternative parses for individual parameters; instead, it matches the whole signature as a unit.
- **No Per-Parameter Backtracking:** There is no equivalent to Subrs' per-parameter backtracking or error collection. The error message is based on the best-matching signature, and typically reports the expected type(s) for the first mismatched argument.

This means ZBuiltins error messages are less nuanced in ambiguous cases, but the system is simpler and more predictable.

### Discovery and Registration

#### Static Initialization
```csharp
// ZBuiltins uses static initialization via reflection
static ZBuiltins()
{
    // Scan all methods with [Builtin] attribute
    foreach (var method in typeof(ZBuiltins).GetMethods(BindingFlags.Static | BindingFlags.Public))
    {
        var attr = method.GetCustomAttribute<BuiltinAttribute>();
        if (attr == null) continue;
        
        // Register in lookup tables by name
        // Create BuiltinSpec with method info and constraints
    }
}
```

#### Builtin Attribute
```csharp
[AttributeUsage(AttributeTargets.Method, AllowMultiple = true)]
public sealed class BuiltinAttribute : Attribute
{
    public BuiltinAttribute(string name) : this(name, null) { }
    public BuiltinAttribute(string name, params string[]? aliases)
    {
        this.name = name;
        this.aliases = aliases;
        MinVersion = 1;      // Z-machine version 1
        MaxVersion = 6;      // Z-machine version 6
        Priority = 1;        // Lower = higher priority for overload resolution
    }
    
    public IEnumerable<string> Names { get; }  // Primary name + aliases
    public object? Data { get; set; }          // Additional metadata
    public int MinVersion { get; set; }        // Minimum Z-machine version
    public int MaxVersion { get; set; }        // Maximum Z-machine version  
    public bool HasSideEffect { get; set; }    // Whether builtin has side effects
    public int Priority { get; set; }          // Overload resolution priority
}

// Usage examples
[Builtin("ADD")]                           // Simple builtin
[Builtin("PRINT", "PRINC")]               // With alias
[Builtin("SET", MinVersion = 4)]          // Version constraint
[Builtin("SAVE", HasSideEffect = true)]   // Side effect marker
```

### Method Discovery Pattern

```csharp
// ZBuiltins lookup is done via static dictionaries populated at startup
public static IEnumerable<string> GetBuiltinNames()
{
    // Returns all registered builtin names
}

public static IEnumerable<ZBuiltinSignature> GetBuiltinSignatures(string name)
{
    // Returns signature info for a specific builtin
}
```

### Parameter Processing

#### ParameterTypeHandler System
The `ParameterTypeHandler` class manages type-specific processing:

```csharp
abstract class ParameterTypeHandler
{
    public abstract bool CanHandle(Type type);
    public abstract ValidationResult Validate(/* args */);
    public abstract object? Process(/* args */);
}

// Specialized handlers for each parameter type
class AtomParameterHandler : ParameterTypeHandler { /* ... */ }
class FixParameterHandler : ParameterTypeHandler { /* ... */ }
class StringParameterHandler : ParameterTypeHandler { /* ... */ }
// ... 20+ more handlers
```

#### Type Handler Registration
```csharp
static Dictionary<Type, ParameterTypeHandler> handlers = new()
{
    [typeof(ZilAtom)] = new AtomParameterHandler(),
    [typeof(ZilFix)] = new FixParameterHandler(),
    [typeof(ZilString)] = new StringParameterHandler(),
    // ... full type mapping
};
```

### Invocation Process

#### Compilation Flow
1. **Name Resolution**: Look up builtin by name in static dictionary
2. **Signature Matching**: Find compatible signature for argument types
3. **Validation**: `ValidateArguments` checks count, types, constraints
4. **Parameter Processing**: Each `ParameterTypeHandler` processes its argument
5. **Method Invocation**: `MethodInfo.Invoke` calls the builtin method
6. **Code Generation**: Method returns `CompilerOutput` with Z-code

#### Example Builtin Method
```csharp
[Builtin("ADD", minVersion: ZVersion.ZV3)]
static CompilerOutput Add(CompilerState state, ZilAtom result, ZilFix left, ZilFix right)
{
    // Generate Z-code: @ADD left right -> result
    return state.EmitInstruction(Opcode.Add, left, right, result);
}
```

### Parameter Types (ZBuiltins)

#### Core Types
```csharp
CompilerState          // Always first - compilation context
ZilAtom               // Variable references, labels
ZilFix                // Integer constants  
ZilString             // String constants
ZilForm               // Code expressions
ZilList               // Argument lists
```

#### Specialized Types
```csharp
RoutineCall           // Function/routine calls
Label                 // Jump targets
LocalVariable         // Local vars in routines
GlobalVariable        // Global vars
PropertyReference     // Object properties
```

### Signature System

#### ZBuiltinSignature
```csharp
class ZBuiltinSignature : ISignature
{
    public string Name { get; }
    public ZVersion MinVersion { get; }
    public ZVersion MaxVersion { get; }
    public ParameterInfo[] Parameters { get; }
    public bool SupportsPrep { get; }
    
    // Validates if arguments match this signature
    public ValidationResult Validate(ZilObject[] args);
}
```

#### Overloading Support
Many builtins have multiple signatures:
```csharp
[Builtin("PRINT")]
static CompilerOutput Print(CompilerState state, ZilString text) { /* ... */ }

[Builtin("PRINT")] 
static CompilerOutput Print(CompilerState state, ZilAtom variable) { /* ... */ }

[Builtin("PRINT")]
static CompilerOutput Print(CompilerState state, ZilForm expression) { /* ... */ }
```

## Comparison and Common Patterns

### Shared Design Elements

#### Attribute-Driven Discovery
Both systems use custom attributes to mark eligible methods:
- **Subrs**: `[Subr]`, `[FSubr]` with optional name and oblist
- **ZBuiltins**: `[Builtin]` with name and version constraints

#### Reflection-Based Registration  
Both use `typeof(T).GetMethods()` to scan for attributed methods and build lookup tables.

#### Type-Safe Parameter Handling
Both systems validate argument types and perform conversions before method invocation.

#### MethodInfo.Invoke Usage
Both ultimately use reflection to invoke target methods after argument processing.

### Key Differences

| Aspect | Subrs System | ZBuiltins System |
|--------|--------------|------------------|
| **Purpose** | Runtime evaluation | Compile-time code generation |
| **Context** | Interpreter execution | Compiler processing |
| **First Parameter** | `Context ctx` | `CompilerState state` |
| **Return Types** | `ZilObject`, `ZilResult` | `CompilerOutput` |
| **Registration** | Instance method in Context | Static class initialization |
| **Argument Processing** | `ArgDecoder.Decode` | `ParameterTypeHandler.Process` |
| **Error Handling** | `InterpreterError` exceptions | `CompilerError` diagnostics |
| **Parameter Attributes** | `[Optional]`, `[Required]`, `[Decl]`, `[ZilOptional]`, `[Either]`, `[ParamDesc]` | Version/priority constraints (`MinVersion`, `MaxVersion`, `Priority`, `HasSideEffect`) |
| **Overloading** | Method name + attribute name | Multiple methods with same builtin name |

### Parameter Conversion Patterns

#### Subrs Conversion (ArgDecoder)
```csharp
// Centralized conversion in ArgDecoder.PrepareOne
switch (targetType)
{
    case Type t when t == typeof(ZilAtom):
        return ConvertToAtom(value, context);
    case Type t when t == typeof(ZilFix):
        return ConvertToFix(value, context);
    // ... handle all supported types
}
```

#### ZBuiltins Conversion (ParameterTypeHandler)
```csharp
// Distributed conversion via handler pattern
var handler = GetHandlerForType(parameterType);
var convertedValue = handler.Process(argument, compilerState);
```

## Error Handling

### Subrs Error Patterns

#### Validation Errors
```csharp
// Type mismatch
throw new InterpreterError(InterpreterMessages._0_Expected_1_2, 
    "SUBR-NAME", "parameter", "ATOM", value.GetTypeAtom(ctx));

// Argument count 
throw new InterpreterError(InterpreterMessages._0_Wrong_Number_Of_Arguments_1,
    "SUBR-NAME", $"expected {expected}, got {actual}");

// Constraint violation
throw new InterpreterError(InterpreterMessages._0_Value_1_Must_Be_2,
    "parameter", value.ToString(), constraintDescription);
```

#### Exception Propagation
```csharp
// ZilResult pattern for recoverable errors
public static ZilResult SOME_SUBR(Context ctx, ZilObject value)
{
    var zr = value.Eval(ctx);
    if (zr.ShouldPass())
        return zr;  // Propagate failure
    
    var evaluatedValue = (ZilObject)zr;
    // ... continue processing
}
```

### ZBuiltins Error Patterns

#### Compilation Errors
```csharp
// Type validation
if (!IsValidType(argument))
{
    state.HandleError(CompilerMessages.InvalidArgumentType, 
        builtinName, expectedType, actualType);
    return CompilerOutput.Error;
}

// Version compatibility
if (state.ZVersion < signature.MinVersion)
{
    state.HandleError(CompilerMessages.UnsupportedInVersion,
        builtinName, signature.MinVersion);
    return CompilerOutput.Error;
}
```

#### Diagnostic Collection
ZBuiltins collect errors in the compilation state rather than throwing exceptions, allowing batch processing of errors.

## Type System

### ZIL Type Hierarchy
Both systems work with the same ZIL type hierarchy:

```
ZilObject (abstract base)
├── ZilAtom
├── ZilFix  
├── ZilString
├── ZilForm
├── ZilList
├── ZilVector
├── ZilFunction
├── ZilMacro
├── ZilSubr/ZilFSubr
├── ZilConstant
├── ZilGlobal
└── ...
```

### Type Constraints

#### Subrs Type Constraints
```csharp
[Decl("<OR ATOM STRING>")]      // Union types
[Decl("<LIST [REST ATOM]>")]    // List of atoms
[Decl("<TUPLE FIX STRING>")]    // Fixed structure
[Decl("<VECTOR [REST ANY]>")]   // Vector of anything
```

#### ZBuiltins Type Constraints
- Version-based availability
- Context-sensitive validation (compile vs runtime)
- Z-machine instruction constraints

### Nullable Handling

#### Subrs Nullable Parameters
```csharp
public static ZilObject SUBR(Context ctx, ZilObject? optionalParam = null)
{
    if (optionalParam == null)
        return ctx.FALSE;
    // ...
}
```

#### ZBuiltins Optional Parameters
ZBuiltins typically use method overloading rather than nullable parameters for optional arguments.

## Reflection Dependencies

### Critical Reflection Usage

#### Method Discovery
```csharp
// Both systems rely on this pattern
typeof(T).GetMethods(BindingFlags.Static | BindingFlags.Public)
    .Where(m => m.GetCustomAttribute<AttributeType>() != null)
```

#### Attribute Reading
```csharp
// Subrs - Multiple attribute types
var attrs = method.GetCustomAttributes<SubrAttributeBase>(false);

// Parameter-level attributes in ArgDecoder
var declAttr = param.GetCustomAttribute<DeclAttribute>();
var optionalAttr = param.GetCustomAttribute<ZilOptionalAttribute>();
var eitherAttr = param.GetCustomAttribute<EitherAttribute>();
var requiredAttr = param.GetCustomAttribute<RequiredAttribute>();
var paramDescAttr = param.GetCustomAttribute<ParamDescAttribute>();

// Struct-level attributes for parameter classes
var structuredAttr = type.GetCustomAttribute<ZilStructuredParamAttribute>();
var sequenceAttr = type.GetCustomAttribute<ZilSequenceParamAttribute>();

// ZBuiltins  
var attr = method.GetCustomAttribute<BuiltinAttribute>();
```

#### Method Invocation
```csharp
// Both systems ultimately use
MethodInfo.Invoke(null, processedArguments)
```

#### Parameter Metadata
```csharp
// Both inspect parameter information
foreach (var param in method.GetParameters())
{
    var paramType = param.ParameterType;
    var attrs = param.GetCustomAttributes();
    var hasDefault = param.HasDefaultValue;
    // ...
}
```

### Reflection Elimination Challenges

#### Dynamic Type Conversion
Both systems perform extensive runtime type checking and conversion that must be code-generated.

#### Flexible Parameter Matching  
The systems support optional parameters, parameter arrays, and type coercion that complicates static generation.

#### Error Message Generation
Reflection-based error messages include actual method and parameter names that must be preserved.

## Source Generation Requirements

### Code Generation Targets

#### Subrs Replacement
```csharp
// Replace this pattern:
var del = ArgDecoder.WrapMethod(methodInfo, context);

// With generated code:  
SubrDelegate SUBR_NAME_Delegate = (name, ctx, args) =>
{
    // Generated validation and conversion code
    if (args.Length < minArgs || args.Length > maxArgs)
        throw new InterpreterError(/* specific message */);
        
    var param0 = ConvertToRequiredType(args[0], ctx);
    var param1 = args.Length > 1 ? ConvertToOptionalType(args[1], ctx) : defaultValue;
    
    // Direct method call (no reflection)
    var result = Subrs.ACTUAL_METHOD_NAME(ctx, param0, param1);
    return WrapResult(result);
};
```

#### ZBuiltins Replacement
```csharp
// Replace reflection-based lookup with generated dictionaries:
static Dictionary<string, BuiltinSpec> builtins = new()
{
    ["ADD"] = new GeneratedBuiltinSpec
    {
        Name = "ADD",
        Signatures = { /* generated signatures */ },
        Validator = GeneratedValidateADD,
        Invoker = GeneratedInvokeADD
    }
};

static ValidationResult GeneratedValidateADD(ZilObject[] args) { /* generated */ }
static CompilerOutput GeneratedInvokeADD(CompilerState state, ZilObject[] args) { /* generated */ }
```

**Note**: As of 2025, the ArgumentParserGenerator has been enhanced to generate a `HasSideEffects(string name)` method that automatically extracts side effect information from `[Builtin(HasSideEffect = true)]` attributes. This eliminates the need for manual side effect detection and ensures accuracy for compiler optimizations. See `devdoc/source-generator-side-effects.md` for details.

### Generation Strategy

#### Source Generator Architecture
1. **Discovery Phase**: Scan assemblies for attributed methods at compile time
2. **Analysis Phase**: Extract parameter types, constraints, and metadata
3. **Validation Phase**: Ensure all required information is available
4. **Generation Phase**: Emit type-safe wrapper code
5. **Registration Phase**: Create lookup tables and initialization code

### Preserving Backtracking and Error Attribution in Source Generation

To faithfully replace the reflection-based Subrs system, a source generator must:

- **Emit Alternative-Branching Code:** For `[Either]` and similar union parameters, generate code that tries each alternative in order, catching and collecting exceptions for each failed branch.
- **Support Optional Parameter Skipping:** Generate logic to skip optional parameters and propagate constraints forward, so that if a later argument fails, the error can be attributed to the skipped optional if appropriate.
- **Error Collection and Attribution:** Implement the same heuristics as `ArgDecoder` for collecting and rethrowing the most informative error, including tracking which parameter/step was the last to "underachieve" and using saved constraints for error messages.
- **Match Exception Types and Messages:** Ensure that the generated code throws the same exception types (`ArgumentTypeError`, `ArgumentCountError`) with the same messages and argument indices as the reflection-based system, so that error reporting remains just as precise and helpful.
- **Testing:** Include comprehensive tests for ambiguous and edge-case signatures (multiple `Either`, nested optionals, etc.) to ensure the generated code matches the legacy system's error reporting exactly.

For ZBuiltins, the source generator can use a simpler approach, as there is no per-parameter backtracking—just signature selection and error reporting for the first mismatch.

#### Preservation Requirements
- **Method Names**: For error messages and debugging
- **Parameter Names**: For diagnostic output
- **Type Information**: For runtime validation 
- **Constraint Data**: For `[Decl]` validation and `[Either]` union types
- **Optional Parameter Handling**: `[ZilOptional]` with custom defaults
- **Structured Parameter Info**: `[ZilStructuredParam]` and `[ZilSequenceParam]` metadata
- **Version Information**: For ZBuiltins compatibility (`MinVersion`, `MaxVersion`)
- **Priority Information**: For ZBuiltins overload resolution
- **Side Effect Markers**: For optimization decisions (`HasSideEffect`)

#### Template Patterns
```csharp
// Generated wrapper template
[GeneratedSubr("ORIGINAL-NAME")]
public static SubrDelegate SUBR_XXX_Generated()
{
    return (name, ctx, args) =>
    {
        // Generated argument validation
        // Generated type conversion  
        // Direct method call
        // Generated result wrapping
    };
}
```

### Migration Path

#### Phase 1: Parallel Implementation
- Keep existing reflection-based system
- Add source-generated alternatives
- Runtime flag to switch between systems
- Comprehensive test coverage

#### Phase 2: Validation
- Run both systems side-by-side
- Compare outputs for correctness
- Performance benchmarking
- Edge case testing

#### Phase 3: Replacement
- Default to source-generated system
- Remove reflection dependencies
- Update build process for trimming
- Documentation updates

### Testing Strategy

#### Compatibility Testing
- All existing Subr and ZBuiltin calls must work identically
- Error messages must match exactly
- Performance characteristics should be preserved or improved

#### Edge Case Coverage
- Optional parameter handling
- Parameter array processing
- Type constraint validation
- Error condition handling
- Version compatibility (ZBuiltins)

## Implementation Lessons Learned

### Generator Best Practices and Lessons Learned (2025)

#### 1. Operand vs Non-Operand Parameter Handling

**Rule:** Only use `CompileOperands` for parameters that are `IOperand` or `params IOperand[]`. For builtins with only `ZilObject[]` or `params ZilObject[]`, never use `CompileOperands`—pass arguments directly.

**Rationale:** This ensures correct evaluation order and stack semantics for operand-based builtins, while preserving unevaluated argument semantics for macros and special forms.

#### 2. Params Array Robustness

**Rule:** For `params IOperand[]`, always slice the operands array safely (never negative or out-of-bounds). For `params ZilObject[]`, use `args.Skip(i).ToArray()` where `i` is the params parameter index.

#### 3. Mixed-Parameter Builtins

**Rule:** For builtins with both operand and non-operand parameters, only pass operand arguments to `CompileOperands`. Non-operand parameters are assigned directly from `args` with type conversion as needed.

#### 4. Variable Assignment

**Rule:** Always emit variable assignments for all parameters, regardless of type. Assign from operands for `IOperand` parameters, and from `args` (with conversion) for others.

#### 5. Error Handling

**Rule:** Match error handling patterns to the call type: `VoidCall`/`ValueCall` use `c.HandleMessage()`, `PredCall`/`ValuePredCall` use `c.cc.Context.HandleError()`.

#### 6. String Parameter Enforcement

**Rule:** For string parameters, enforce that only `ZilString` is accepted, and use `Compilation.TranslateString` for conversion.

#### 7. Code Generation Validation

**Rule:** Always validate generated code by examining the actual output, not just compilation success. Test with all edge cases, especially for params arrays and mixed-parameter builtins.

#### 8. Documentation

**Rule:** Update this document and the AI assistant instructions whenever generator logic changes, to ensure future maintainers have up-to-date guidance.

---

### Critical Challenges Encountered

#### 1. Type Hierarchy Detection Issues

**Problem**: The type detection logic in `GenerateParameterConversionExpression` was checking `ZilObject` before more specific types like `ZilAtom`. Since `ZilAtom` inherits from `ZilObject`, all ZilAtom parameters were incorrectly processed as generic ZilObjects, leading to compilation errors.

**Solution**: Reorder type checking to examine specific types before general inheritance-based types:
```csharp
// Correct order - specific types first
if (IsZilAtomType(parameterType))
    // Handle ZilAtom specifically
else if (IsIVariableType(parameterType))  
    // Handle IVariable specifically
else if (IsZilObjectType(parameterType))
    // Handle generic ZilObject last
```

**Lesson**: In reflection-based type analysis, always check derived types before base types to avoid incorrect classification due to inheritance.

#### 2. [Data] Attribute Enum Casting

**Problem**: Methods with `[Data]` attributes pass enum values as integers, but the generated code was trying to pass them directly as integers to methods expecting strongly-typed enums, causing compilation errors.

**Solution**: Detect enum parameters and generate explicit casts:
```csharp
// For enum parameters from [Data] attributes
var enumParam = $"({parameterType.FullName}){dataValue}";
// Example: (Zilf.Emit.BinaryOp)4
```

**Lesson**: Source generators must handle all type conversions explicitly, including enum casting from integer values in attributes.

#### 3. Parameter vs Argument Separation

**Problem**: Confusion between method parameters (including `[Data]` attributes) and actual ZIL arguments led to incorrect argument counting and parameter matching.

**Solution**: Implement clear separation with `ParameterInfo` structure:
```csharp
public class ParameterInfo
{
    public ParameterInfo[] DataParameters { get; set; }    // [Data] attributes
    public ParameterInfo[] ArgumentParameters { get; set; } // ZIL arguments
    public int TotalArgumentCount { get; set; }            // Expected ZIL args
}
```

**Lesson**: Maintain clear conceptual separation between method signature analysis and runtime argument processing.

#### 4. Error Handling Pattern Differences

**Problem**: Different call types (VoidCall, ValueCall, PredCall, ValuePredCall) require different error handling mechanisms, but initial implementation used a single pattern.

**Solution**: Implement call-type-specific error handling:
```csharp
// VoidCall and ValueCall
c.HandleMessage(CompilerMessages.MessageId, args);

// PredCall and ValuePredCall  
c.cc.Context.HandleError(CompilerMessages.MessageId, args);
```

**Lesson**: Error handling patterns must match the specific call semantics of each method type.

#### 5. Variable Resolution Context

**Problem**: `IVariable` parameters require compilation context to resolve variables, but initial implementation didn't provide the necessary context methods.

**Solution**: Add helper method for variable resolution:
```csharp
private static string GetVariableFromContext(string argName)
{
    return $"GetVariable(c.cc.Context, {argName})";  // Simplified example
}
```

**Lesson**: Complex parameter types may require additional context or helper methods in generated code.

### Successful Implementation Patterns

#### 1. Comprehensive Method Signature Analysis
The `AnalyzeMethodParameters` approach successfully handled complex method signatures by:
- Separating `[Data]` parameters from ZIL arguments
- Counting expected arguments correctly including `params` arrays
- Preserving parameter order for correct method calls

#### 2. Type-Specific Conversion Generation
The `GenerateParameterConversionExpression` pattern successfully handled all ZILF type conversions by:
- Checking specific types before general types
- Generating appropriate cast expressions for each type
- Providing clear error messages for type mismatches

#### 3. Enum and Primitive Type Handling
Successful handling of both enum casting and primitive type conversion:
```csharp
// Enum from [Data] attribute
(Zilf.Emit.BinaryOp)4

// Primitive conversion with validation
(args[0] as ZilAtom ?? throw new ArgumentException($"Expected ZilAtom, got {args[0]?.GetType().Name}"))
```

### Performance and Maintainability Benefits

The source generation approach provides:
- **Compile-time validation**: Type mismatches caught at build time rather than runtime
- **Elimination of reflection**: Direct method calls instead of `MethodInfo.Invoke`
- **Clear generated code**: Easy to debug and understand the generated parsers
- **Trimming support**: No runtime reflection dependencies

### Recommendations for Future Work

1. **Start with comprehensive type analysis** before attempting code generation
2. **Handle type hierarchies carefully** by checking specific types before general ones
3. **Separate conceptual concerns** (parameters vs arguments, data vs runtime values)

---

## Future Subrs Parser Generator Design Considerations

### Lessons from ZBuiltins Generator Experience

The successful implementation of the ZBuiltins argument parser generator provides valuable insights for a future Subrs parser generator. However, the Subrs system presents unique challenges that require different approaches.

### Applicable Design Elements

#### 1. Core Generator Architecture
The incremental source generator pattern used for ZBuiltins can be directly applied to Subrs:
- Method discovery via `GetMethods()` and attribute scanning
- Parameter analysis with type and attribute extraction
- Code generation using `IndentedStringBuilder` patterns
- Registration via generated lookup dictionaries

#### 2. Metadata Extraction Patterns
The `AnalyzeParameter` and `BuiltinMethodInfo` patterns work well and can be adapted:
```csharp
// Similar structure, different attributes
public record SubrMethodInfo
{
    public string MethodName { get; init; }
    public string SubrName { get; init; }           // From [Subr("name")] attribute
    public bool IsFSubr { get; init; }              // [FSubr] vs [Subr]
    public string? ObList { get; init; }            // Namespace qualification
    public List<ParameterInfo> Parameters { get; init; }
}
```

#### 3. Type-Specific Conversion Generation
The parameter conversion logic can be reused but needs expansion for Subrs' more complex type system (30+ supported types vs ZBuiltins' simpler set).

#### 4. Error Handling Code Generation
The error handling patterns for generating appropriate exceptions with correct messages can be adapted, though Subrs uses `InterpreterError` instead of compiler errors.

### Major Differences Requiring New Design

#### 1. Backtracking and Alternative Parsing

**ZBuiltins Challenge**: Simple signature matching - one signature matches or fails.

**Subrs Challenge**: Complex backtracking for `[Either]` parameters and optional argument handling.

**Required Solution**: Generate state machine-like code that:
```csharp
// Generated backtracking code for [Either(typeof(ZilAtom), typeof(ZilString))]
var parseErrors = new List<Exception>();
try
{
    // Try ZilAtom conversion
    var atomResult = ConvertToAtom(args[i]);
    // Success - continue with this branch
}
catch (Exception ex1)
{
    parseErrors.Add(ex1);
    try
    {
        // Try ZilString conversion
        var stringResult = ConvertToString(args[i]);
        // Success - continue with this branch
    }
    catch (Exception ex2)
    {
        parseErrors.Add(ex2);
        // All alternatives failed - report most informative error
        throw SelectBestError(parseErrors);
    }
}
```

#### 2. Complex Parameter Classes

**ZBuiltins Challenge**: Simple parameter types (mostly primitives and ZILF types).

**Subrs Challenge**: Complex parameter classes with nested structures:
- `AtomParams.StringOrAtom` - union types within parameter classes
- `DeclParams.AtomsDeclSequence` - structured sequences with type constraints
- `WarningParams.CodesOrWildcard` - complex either/or scenarios

**Required Solution**: Recursive code generation for parameter class construction:
```csharp
// Generated code for complex parameter class
private static AtomParams.StringOrAtom GenerateStringOrAtom(ZilObject arg)
{
    if (arg is ZilAtom atom)
        return new AtomParams.StringOrAtom { Atom = atom };
    if (arg is ZilString str)
        return new AtomParams.StringOrAtom { String = str };
    throw new ArgumentTypeError($"Expected ATOM or STRING, got {arg.GetType().Name}");
}
```

#### 3. Context Sensitivity

**ZBuiltins Challenge**: Compilation context is relatively static.

**Subrs Challenge**: Runtime context affects argument interpretation:
- `LocalEnvironment` parameters change variable binding
- Dynamic type checking based on runtime state
- Context-dependent default values

#### 4. Error Attribution Complexity

**ZBuiltins Challenge**: Simple "first mismatch" error reporting.

**Subrs Challenge**: Sophisticated error attribution with backtracking:
- Which parameter to blame when multiple alternatives fail
- Error messages that reference skipped optional parameters
- Context-aware error descriptions

**Required Solution**: Generate error tracking and attribution logic:
```csharp
// Generated error attribution code
private static Exception SelectBestError(List<Exception> errors, int lastSuccessfulParam)
{
    // Implement ArgDecoder's error selection heuristics
    // Prefer type errors over count errors
    // Attribute to most promising alternative
    return errors.OrderBy(e => GetErrorPriority(e)).First();
}
```

### Recommended Implementation Strategy

#### Phase 1: Subset Implementation
Start with simple Subrs without complex features:
- No `[Either]` parameters
- No complex parameter classes
- No backtracking
- Simple type conversions only

This establishes the basic generator framework and tests the approach.

#### Phase 2: Backtracking Implementation
Add support for `[Either]` parameters and optional argument handling:
- Generate branching logic for alternative types
- Implement error collection and attribution
- Test against existing reflection-based behavior

#### Phase 3: Parameter Class Support
Add support for complex parameter classes:
- Analyze `[ZilStructuredParam]` and `[ZilSequenceParam]` attributes
- Generate recursive parsing logic
- Handle nested type constraints

#### Phase 4: Full Feature Parity
Complete implementation with all Subrs features:
- Context-sensitive parsing
- All parameter attributes
- Performance optimizations

### Generator Architecture Recommendations

#### Separate from ZBuiltins Generator
While sharing common utilities, the Subrs generator should be a separate class:

```csharp
[Generator]
public class SubrArgumentParserGenerator : IIncrementalGenerator
{
    // Can share utilities with ArgumentParserGenerator but needs different logic
}
```

**Rationale**: The complexity differences are substantial enough that trying to share the main generation logic would create more complexity than benefit.

#### Shared Utility Classes
Create shared utilities for common operations:
```csharp
// Shared between both generators
public static class GeneratorUtilities
{
    public static string GetTypeDisplayName(ITypeSymbol type) { /* ... */ }
    public static bool IsZilType(ITypeSymbol type) { /* ... */ }
    public static string GenerateTypeConversion(ITypeSymbol type, string expr) { /* ... */ }
}
```

#### Testing Strategy
Use the same approach as ZBuiltins generator:
- Generate side-by-side with reflection system
- Comprehensive compatibility testing
- Performance benchmarking
- Gradual migration path

### Complexity Assessment

**High Priority/Low Complexity**:
- Basic method discovery and registration
- Simple parameter conversion
- Error message generation

**High Priority/High Complexity**:
- Backtracking implementation for `[Either]`
- Error attribution matching ArgDecoder behavior
- Parameter class recursive parsing

**Lower Priority**:
- Performance optimizations beyond reflection elimination
- Advanced context sensitivity features

### Conclusion

The ZBuiltins generator success demonstrates that source generation is viable for ZILF's argument parsing. However, the Subrs generator requires significantly more sophisticated logic to handle backtracking, complex parameter types, and error attribution. The recommended approach is to build on the proven ZBuiltins patterns while implementing new logic specifically for Subrs' unique requirements.

A phased implementation starting with simple cases and gradually adding complexity will provide the best path to success while maintaining the precise error handling and argument matching behavior that existing ZILF code depends on.
4. **Implement error handling patterns** that match the target system's conventions
5. **Test incrementally** with small subsets before attempting full generation
6. **Validate generated code** by examining the actual output, not just compilation success

## Implementation Lessons Learned (2025)

The ZILF project has successfully implemented source generation for builtin argument parsing with the following key insights:

### Side Effects Detection (Completed)
- **Problem**: Manual maintenance of side effect lists was error-prone and inconsistent with `[Builtin]` attributes
- **Solution**: ArgumentParserGenerator now generates `HasSideEffects(string name)` method automatically from `HasSideEffect = true` attributes
- **Key Insight**: Builtin name alone determines side effects, not version or argument count combinations
- **Architecture**: Single source of truth in `[Builtin]` attributes, eliminated 120+ lines of manual code
- **Result**: 70+ builtins with side effects automatically detected, zero maintenance burden

### Source Generator Best Practices
- **Type Analysis**: Always check specific derived types before general base types when using reflection in generators
- **Attribute Collection**: Collect all overloads of a builtin name to determine aggregate properties (like side effects)
- **Code Generation**: Use `IndentedStringBuilder` for clean multi-line code generation with proper formatting
- **Incremental Development**: Build and test generators with small method subsets before attempting full generation
- **Validation**: Always examine actual generated code, not just compilation success, to ensure correctness

### Runtime Reflection Elimination
- **Policy**: No reflection at runtime - all method signature and attribute analysis must happen at compile time
- **Architecture**: Replace `MethodInfo.Invoke` patterns with direct method calls in generated code
- **Performance**: Generated switch statements are highly optimized compared to dictionary lookups or reflection
- **Maintainability**: Automatic code generation eliminates synchronization issues between attributes and runtime behavior

### Error Handling Conventions
Different builtin call types require specific error handling patterns:
- `VoidCall`/`ValueCall`: Use `c.HandleMessage()` for error reporting
- `PredCall`/`ValuePredCall`: Use `c.cc.Context.HandleError()` for error reporting
- Generated code must preserve the same exception types and messages as the original reflection-based system

### Future Source Generation Targets
The success with side effects detection provides a proven foundation for replacing:
- **Full ZBuiltins Argument Parsing**: Replace reflection-based `ParameterTypeHandler` system with generated parsers
- **Subrs System Migration**: More complex due to backtracking requirements for `[Either]` attributes and error attribution
- **Attribute-Driven Code Generation**: Expand to other areas where attributes drive runtime behavior

A phased implementation starting with simple cases and gradually adding complexity will provide the best path to success while maintaining the precise error handling and argument matching behavior that existing ZILF code depends on.