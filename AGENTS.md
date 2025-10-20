# ZILF Project — AI Assistant Working Guide

Purpose: Enable an AI coding agent to be immediately productive in this repository by conveying architecture, workflows, and project-specific conventions. Keep changes focused, incremental, and validated by tests.

## Key Concepts

### ZilObject vs IOperand in the Compiler

In the ZILF compiler, most argument parsers receive arguments as `ZilObject[]`, which represent unevaluated ZIL forms or constants. However, many built-in routines (such as arithmetic operations) require their arguments as `IOperand[]`, which are the compiled representations suitable for Z-machine code emission.

- **ZilObject**: Represents any ZIL value or form, including atoms, numbers, lists, and code expressions. Used as the universal argument type for parser entry points.
- **IOperand**: Represents a compiled operand (constant, variable, or temporary) that can be directly used in Z-machine instructions. Produced by compiling a `ZilObject` using methods like `CompileAsOperand`.


**When writing argument parsers for builtins (2025 best practices):**
- If the implementation method expects only `ZilObject` or `params ZilObject[]`, pass the arguments directly after validation and macro unwrapping. **Never use `CompileOperands` for these.**
- If the implementation method expects any `IOperand` or `params IOperand[]`, use `CompileOperands` for just the operand arguments (not all arguments). For `params IOperand[]`, slice the operands array safely.
- For mixed-parameter builtins, only pass operand arguments to `CompileOperands`; assign non-operand parameters directly from `args` with type conversion as needed.
- Always unwrap `ZilMacroResult` objects before compilation: `(args[i] is ZilMacroResult zmr ? zmr.Inner : args[i])`.
- For string parameters, enforce that only `ZilString` is accepted, and use `Compilation.TranslateString` for conversion.
- Match error handling patterns to the call type: `VoidCall`/`ValueCall` use `c.HandleMessage()`, `PredCall`/`ValuePredCall` use `c.cc.Context.HandleError()`.
- For variable parameters with `[Variable]` attributes, use `GetVariable()` helper and dispatch based on `IsHard`/`IsSoft` properties.
- Always validate generated code by examining the actual output, not just compilation success. Test with all edge cases, especially for params arrays and mixed-parameter builtins.
- **Side Effects**: Use `HasSideEffect = true` in `[Builtin]` attributes for builtins that modify state. The source generator automatically maintains the `HasSideEffects(string name)` method from these attributes.
- **No Runtime Reflection**: Never use reflection at runtime to inspect method signatures or attributes; all such analysis must be done at compile time in source generators.
- **No Special Cases**: NEVER hardcode the name of any custom sequence/structure parameter type, or the name of any SUBR/FSUBR/ZBuiltin, or any logic for parsing a specific custom type in the source generator. The generator MUST work generically, based on the definitions of those types.

**Example:**
```csharp
// Parser for an arithmetic builtin expecting IOperand[]
public static IOperand ADD_Generated(ValueCall c, ZilObject[] args)
{
    if (args.Length < 1)
        return c.cc.HandleError(CompilerMessages.WrongArgumentCount, "ADD", 1, args.Length);
    using (var operands = c.cc.CompileOperands(c.rb, c.form.SourceLine, args))
    {
        return ArithmeticOp(c, BinaryOp.Add, operands.ToArray());
    }
}
```

**Source Generator Implications**: The generator must analyze method signatures to determine which parameters are operands, and only use `CompileOperands` for those. For `params ZilObject[]`, never use `CompileOperands`—pass arguments directly. For `params IOperand[]`, slice the operands array safely. The generator automatically handles macro unwrapping for all arguments before compilation. Variable parameters with `[Variable]` attributes generate runtime dispatch code using the `GetVariable()` helper. The ArgumentParserGenerator now automatically generates side effects detection from `HasSideEffect = true` attributes, eliminating manual maintenance. Always update documentation when generator logic changes.

## 1. High-Level Architecture

Components (see `src/`):
- `Zilf/` — Main executable front end: command-line modes (compiler, interpreter, expression eval, interactive REPL). Entry: `src/Zilf/Program.cs` and pipeline coordinator `Compiler/FrontEnd.cs`.
- `Zilf.Common/` — Shared utilities, data structures, filesystem abstractions (e.g., `IFileSystem`, `PhysicalFileSystem`, in tests also `InMemoryFileSystem`). Use these instead of raw `System.IO` in cross-component logic for testability.
- `Zilf.Emit/` — Z-machine emission layer (game builder, zap stream generation) consumed by `FrontEnd` when compiling.
- `Zapf.Parsing/` + `Zapf/` — Assembler for intermediate `.zap` / `.xzap` textual Z-code fragments into final story format (multi-part `.zap`, `_data.zap`, `_str.zap`, optional `_freq.zap`). `ZapfAssembler` orchestrates multi-pass assembly and restart logic.
- `Dezapf/` — Disassembler (currently minimal / limited tests). Not included in packaging (`Build.proj` excludes it from stage).
- `Analyzers/` — Roslyn analyzers & source generators specific to ZILF (e.g., `ZilfAnalyzers.csproj`, `ZilfSourceGenerators`). They are conditionally loaded as analyzers in consuming projects if the compiled DLL exists.
- `Zilf.Playground/` — Web/interactive playground that wraps `FrontEnd` (`Services/Builds/BuildService.cs`, `Services/Repl/ReplService.cs`). Useful reference for embedding patterns.
- `zillib/` & `sample/` — Bundled ZIL library include paths and example games; `sample/*.zil` used for manual or integration scenarios. `sample/advent` is a canonical test case.

Data / Control Flow (compile path): `FrontEnd.Compile` => evaluate ZIL (parsing + interpretation for definitions) => prepare `GameBuilder` + `ZapStreamFactory` => emit `.zap` segments => (optionally) external assembly (`ZapfAssembler`) to Z-machine story file (tests demonstrate chaining in `ZlrHelper`).

## 2. Build & Packaging Workflows

Fast local build (solution-wide): `dotnet build Zilf.sln`.
Core distribution packaging (multi-RID): `tools\package-all.ps1` (targets only staged projects `Zilf` and `Zapf`; analyzers & dezapf excluded).
Stage only (no packaging): `dotnet msbuild Build.proj -t:Stage -p:Configuration=Release` → outputs under `Package/<Config>/Stage/<packageName>/` with executables + library + samples + zillib.
CI-specific properties are centralized in `Directory.Build.props` (version stamping, signing, warnings as errors, language version). Avoid duplicating these settings in individual `.csproj` files.

## 3. Runtime / Modes (CLI)

`zilf` modes (see `Program.BuildContext`):
- Compile: `zilf [-c] input.zil [output.zap]`
- Interpret (execute no output): `zilf -x input.zil`
- Expression: `zilf -e "<expr>"`
- REPL: `zilf -i`

Important switches: `-ip <dir>` (include path), `-tr` (trace routines), `-d` (debug info), `-ws code1,code2` (suppress diagnostics), `-we` (warnings as errors), `-w` (enable noisy warnings), case sensitivity `-cs/-ci`.
Include path auto-augmentation: `Program.AddImplicitIncludePaths` heuristically adds directory of input + nearby `zillib` (searches upward, ignoring test dirs); prefer not to reimplement—call existing logic.

## 4. Compilation Pipeline Nuances

- Evaluation (interpreting top-level forms) precedes emission; errors during evaluation abort emission (`FrontEnd.InterpretOrCompile`). Only proceed when `ctx.ErrorCount == 0`.
- Hooks: `ctx.RunHook("PRE-COMPILE")` then `ctx.SetDefaultConstants()` before building game image; if extending pipeline (e.g., extra transformation), insert after PRE-COMPILE but before `GameBuilder` instantiation.
- `ZapStreamFactory` names related segment files using suffixes `_data`, `_str`, and `_freq` (or `freq` w/o underscore) and will request frequent words generation if none exists. When adding new emission outputs, mirror this naming pattern.
- Game options are version-dependent (Z-machine version influences `GameOptions` subclass). Extend `MakeGameOptions` with additional Z-machine flags carefully—maintain existing switch structure.

## 5. Testing Strategy & Conventions

- Framework: MSTest v3 (`Microsoft.NET.Test.Sdk`, `MSTest.TestFramework`); integration helpers in `test/Zilf.Tests.Integration/ZlrHelper.cs` show canonical compile→assemble→execute flow using in-memory FS.
- Some of the integration tests take a long time to run, so long that the agent system will time out. Those tests are tagged with `[TestCategory("Slow")]`, so you should usually exclude them from running, especially while you're iterating on something. Only let the slow tests run when you're ready to do a full test pass.
- Use `InMemoryFileSystem` / `OverlayFileSystem` for deterministic tests; don't write to the real filesystem unless staging packaging scenarios.
- Analyzer and source generator behaviors are implicitly validated by normal builds (analyzers attached conditionally via `<Analyzer Condition="Exists(...)" ...>`). When adding new diagnostics, place IDs in `Analyzers/DiagnosticIds.cs` and create Analyzer + CodeFix pair following existing patterns.
- **Important**: Your solution to a problem must not break existing tests. Always run the fast test suite after making changes, and fix any failures before concluding that you're finished. It is unacceptable to fix one bug by creating another.

### How to Run Tests

- Start by running the fast tests only: in the workspace root, execute `dotnet test Zilf.sln -c Debug --filter "TestCategory!=Slow"`.
	- If needed, you may add additional parameters as needed (e.g., `--logger "console;verbosity=minimal"` or `--logger "trx;LogFileName=test_results.trx"`).
- **Optional**: If the fast tests pass, proceed to run the full test suite with `dotnet test Zilf.sln -c Debug`.
	- Note: the full test suite includes some slow tests, which may take a minute or more to complete. Only run the full test suite when you've finished work on a task and need to validate it. Don't run the full test suite while actively iterating on code changes.
- **Important**: Always run tests through `Zilf.sln`, not individual test projects. Individual test projects have dependency issues when run directly and require the full solution build. NEVER run an individual test project (e.g. `dotnet test test/Zilf.Tests/Zilf.Tests.csproj`) because it *will not work*.

### How to Test Source Generators

- Source generators are validated by normal builds and tests. If the regular test suite passes (see "How to Run Tests" above), the source generators are almost certainly functioning correctly.
- While actively iterating, you may quickly validate source generator changes by running `dotnet build Zilf.sln` to ensure the generators execute without errors. You may then inspect the generated files to see if your intended changes were applied correctly.

## 6. FileSystem Abstraction

Prefer `IFileSystem` (see usages in `FrontEnd`, `ZapfAssembler`, tests). This enables multi-environment operation (playground, tests, CLI). New features should accept an `IFileSystem` rather than assuming physical disk.

## 7. Diagnostics & Logging

- `Context.DiagnosticManager` drives warnings/errors; suppression list applied from CLI `-ws`. To emit new diagnostics, use existing `Diagnostic` factories or follow analyzer patterns.
- Quiet mode suppresses banner/prompt only—do not gate diagnostic emission on `ctx.Quiet`.
- Diagnostic IDs and messages are centralized as integer constants in `src/Zilf/Diagnostics/InterpreterMessages.cs` (errors/warnings resulting from evaluating FORMs) and `src/Zilf/Diagnostics/CompilerMessages.cs` (errors/warnings from compilation; only raised when in "compile" mode), and grouped by category (e.g. interpreter messages 0300-0399 relate to structured values).
- When adding new diagnostic messages, ensure they have unique IDs, they're in the correct section and in order, they have an `[Error(...)]` attribute with a template message, and that their constant names follow the existing naming conventions (mirroring the template messages, with special characters replaced by underscores).

## 8. Source Generators / Analyzers

`src/Analyzers/ZilfAnalyzers.csproj` & `ZilfSourceGenerators` provide compile-time code analysis. They're not packaged for runtime; ensure changes remain incremental and respect conditional inclusion path defined in consuming `.csproj` (`Exists('$(ZilfAnalyzersAssembly)')`).

**Key Lessons for ZILF Source Generators:**
- **Type Hierarchy Awareness**: When analyzing parameter types via reflection, always check specific derived types (e.g., `ZilAtom`) before general base types (e.g., `ZilObject`) to avoid inheritance classification issues.
- **[Data] Attribute Handling**: Parameters with `[Data]` attributes pass enum values as integers and require explicit enum casting in generated code: `(EnumType)intValue`.
- **Parameter vs Argument Separation**: Distinguish between method parameters (including `[Data]` attributes) and actual ZIL arguments when analyzing method signatures and counting expected arguments.
- **Call-Specific Error Handling**: Different ZILF call types require different error handling patterns:
  - `VoidCall`/`ValueCall`: Use `c.HandleMessage()`
  - `PredCall`/`ValuePredCall`: Use `c.cc.Context.HandleError()`
- **Generated Code Validation**: Always examine the actual generated source code, not just compilation success, to ensure correct type conversions and method calls.
- **Incremental Development**: Build and test source generators incrementally with small method subsets before attempting full generation to catch issues early.
- **Indentation**: When generating multi-line code blocks, use `IndentedStringBuilder` to manage indentation levels cleanly and avoid formatting issues. Do not hardcode indentation into strings.
- **Side Effects Generation**: The ArgumentParserGenerator automatically generates `HasSideEffects(string name)` method from `[Builtin(HasSideEffect = true)]` attributes. This eliminates manual side effect list maintenance and ensures accuracy for compiler optimizations.
- **Attribute-Driven Architecture**: Use attributes as the single source of truth for code generation. Collect all overloads of a method name to determine aggregate properties (like side effects).
- **No Runtime Reflection**: All method signature and attribute analysis must happen at compile time in source generators. Runtime reflection is prohibited for performance and trimming support.

For detailed implementation guidance, see `devdoc/argument-parsing-systems.md` "Implementation Lessons Learned" section and `devdoc/source-generator-side-effects.md` for side effects detection specifics.

Do not use reflection at runtime to inspect method signatures or attributes; all such analysis must be done at compile time in the source generator. Runtime reflection checks will not work correctly.

## 9. Versioning / Metadata

Central version numbers: `CurrentVersion.props` → referenced by `Directory.Build.props` to create `InformationalVersion` (`DisplayVersion` logic chooses compressed form per release level). Avoid embedding versions directly in code; call `Program.GetVersion()` for display.

## 10. Packaging & Staging Targets

Custom MSBuild targets (`Build.proj`) define: `Stage`, `Package`, `PackageAllRids`, with multi-RID iteration using `RuntimeIdentifiers`. When adding a new executable project that should enter distributions, add it to `<StageProjects>` and ensure it supports the same RIDs.

## 11. Performance / Determinism

`Deterministic` is disabled (`false`) in several `.csproj` files; builds rely on runtime patch updates (`<TargetLatestRuntimePatch>true</TargetLatestRuntimePatch>`). Don't assume stable binary hashes between builds. For perf-sensitive code (parsers, emission), keep allocations minimal and reuse spans/iterators (see patterns in tokenizer & parser code—consult those files before altering).

## 12. Safe Extension Points

Use events: `FrontEnd.InitializeContext` and `ZapfAssembler.InitializingContext` to inject context changes instead of modifying core constructors.
REPL integration contract: start via `FrontEnd.StartRepl()` (returns `IReplSession`). Avoid duplicating REPL loop logic from `Program.DoREPL`—use the interface.

## 13. Common Pitfalls

- Forgetting to add include paths leads to missing symbol diagnostics; replicate test helper pattern (add main file directory + library paths) rather than hardcoding.
- Creating `.zap` outputs directly without frequent words file triggers automatic generation; if providing a custom `_freq.zap`, ensure naming matches either `_freq` or `freq` suffix.
- Accessing `Context` global options: always use `ctx.GetGlobalOption(StdAtom.*)` rather than reading internal collections.

## 14. Typical Dev Commands (PowerShell)

```
dotnet restore Zilf.sln
dotnet build Zilf.sln -c Debug
dotnet test Zilf.sln -c Debug --logger "trx;LogFileName=test_results.trx"
tools\package-all.ps1
```
**Note**: Projects now target .NET 9 with C# 13. Ensure .NET 9 SDK is installed.

## 15. When Adding New Features

1. Identify correct layer (interpretation vs emission vs assembly). If it affects Z-machine binary, likely belongs in `Zilf.Emit` or assembler; if syntax/semantic evaluation, in interpreter/compiler.
2. Expose through `Context` or `GameOptions` rather than adding ad-hoc globals.
3. Update tests using in-memory FS; prefer integration helper pattern for end-to-end.
4. New commands for use at **evaluation time** (i.e., intepreter commands) go in one of the categorized `src/Zilf/Interpreter/Subrs.<category>.cs` files and are marked with `[Subr(...)]` (or `[FSubr(...)]` for special forms). Arguments are automatically coerced based on method signature; see `src/Zilf/Interpreter/ArgDecoder.cs` for details.
5. When adding new interpreter commands, ensure they have corresponding tests in `test/Zilf.Tests/Interpreter` and follow existing naming and documentation conventions.
6. New commands for use at **compile time** (i.e., Z-code built-in routines) go in `src/Zilf/Compiler/Builtins/ZBuiltins.cs` and are marked with `[Builtin(...)]`. Arguments are also automatically coerced, using a different mechanism; see the `ValidateArguments`, `MakeBuiltinMethodParams`, and `CompileBuiltinCall` methods in that file, as well as `src/zilf/Compiler/Builtins/ParameterTypeHandler.cs` for details.
7. When adding new compile-time built-in routines, ensure they have corresponding tests in `test/Zilf.Tests.Integration` and follow existing naming and documentation conventions.

## 16. Minimal Example (Programmatic Compile)

Reference: pattern from `ZlrHelper`:
```csharp
var fe = new FrontEnd { FileSystem = new InMemoryFileSystem() };
fe.IncludePaths.Add("");
var result = fe.Compile("main.zil", "Output.zap", wantDebugInfo: false);
if (result.Success) new ZapfAssembler { FileSystem = fe.FileSystem }.Assemble("Output.zap", "Output.zcode");
```

## 19. Coding Style & Conventions

- Projects target .NET 9 (`net9.0`) with C# 13 language features. Source generators remain on `netstandard2.0` for VS compatibility but can still use C# 13 language features.
- Use nullable reference types.
- Use expression-bodied members for simple property getters and methods.
- Prefer `var` when the type is obvious from the right side of the assignment.
- Use explicit access modifiers (`public`, `private`, etc.) on all members.
- Use `this.` only when necessary for disambiguation.
- Use pattern matching (`is`, `switch`) instead of `as` and null checks.
- Use string interpolation (`$"..."`) instead of `string.Format`.
- Use `nameof` instead of hard-coded parameter or property names.
- Use `using` declarations instead of `using` statements when possible.
- Use collection expressions (`[ ... ]`) instead of collection initializers (`new[] { ... }`).
- Use target-typed `new()` expressions.
- Use standard C# formatting, as seen in Visual Studio defaults.
- Use a maximum line length of 120 characters.
- Do not mix braces styles in an individual flow control statement; either use braces for all clauses or none.
- Comments on types and members should use XML documentation comments with appropriate tagged sections (`<summary>`, `<param>`, `<returns>`, `<exception>`, etc.). All public types and members that you add should have an XML doc comment with at least a `<summary>`.

## 20. The ZIL Language

ZIL is essentially a domain-specific extension of MDL. ZILF consists of an interpreter for a fairly large subset of MDL, with some additional constructs built in, plus a compiler for an embedded language which is similar to, but distinct from, MDL. The interpreter is not a full MDL implementation; it only supports the constructs needed for ZIL. The ZIL constructs such as `ROUTINE` and `OBJECT` build structures in the interpreter's context that are then used during compilation to generate Z-machine code and data structures.

MDL is not LISP, although it has some LISP-like syntax. It is a distinct language with its own semantics and constructs. MDL has a variety of data types besides lists, and notably, it distinguishes between lists and forms. Lists, written with parentheses, are merely data structures; forms, written with angle brackets, are code expressions that can be executed. Thus, evaluating `(+ 1 2)` will simply return the same list, but evaluating `<+ 1 2>` will perform the addition and return 3.

The embedded language implemented by the compiler (i.e. available inside a `ROUTINE`), is similar to but not the same as the language implemented by the interpreter (i.e. available outside a `ROUTINE`). The features of the embedded language are implemented in ZILF as methods in `ZBuiltins.cs` marked with the `[Builtin]` attribute, which emit assembly code to perform the operations. The features of the interpreted language are implemented in `Subrs.*.cs` files marked with the `[Subr]` or `[FSubr]` attribute, which perform the operations directly in C# code.

The interpreted language is dynamically typed, and all values which can be accessed by interpreted code are implemented as subclasses of `ZilObject`. The embedded language is untyped, and all values exist at runtime as 16-bit words; the compiler does some static typing to facilitate optimizations, but the Z-machine itself does not enforce types. The compiler represents values as `IOperand` instances, which translate directly to Z-machine instruction operands and can represent constants, local or global variables, or the stack.
