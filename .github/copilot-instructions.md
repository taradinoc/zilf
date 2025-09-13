## ZILF Project – AI Assistant Working### 5. Testing Strategy & Conventions
- Framework: MSTest v3 (`Microsoft.NET.Test.Sdk`, `MSTest.TestFramework`); integration helpers in `test/Zilf.Tests.Integration/ZlrHelper.cs` show canonical compile→assemble→execute flow using in-memory FS.
- Use `InMemoryFileSystem` / `OverlayFileSystem` for deterministic tests; don't write to the real filesystem unless staging packaging scenarios.
- Analyzer and source generator behaviors are implicitly validated by normal builds (analyzers attached conditionally via `<Analyzer Condition="Exists(...)" ...>`). When adding new diagnostics, place IDs in `Analyzers/DiagnosticIds.cs` and create Analyzer + CodeFix pair following existing patterns.
- **Target Framework**: Projects target .NET 9 (`net9.0`) with C# 13 language features. Source generators remain on `netstandard2.0` for VS compatibility.es

Purpose: Enable an AI coding agent to be immediately productive in this repository by conveying architecture, workflows, and project‑specific conventions. Keep changes focused, incremental, and validated by tests.

### 1. High-Level Architecture
Components (see `src/`):
- `Zilf/` – Main executable front end: command‑line modes (compiler, interpreter, expression eval, interactive REPL). Entry: `src/Zilf/Program.cs` and pipeline coordinator `Compiler/FrontEnd.cs`.
- `Zilf.Common/` – Shared utilities, data structures, filesystem abstractions (e.g., `IFileSystem`, `PhysicalFileSystem`, in tests also `InMemoryFileSystem`). Use these instead of raw `System.IO` in cross‑component logic for testability.
- `Zilf.Emit/` – Z-machine emission layer (game builder, zap stream generation) consumed by `FrontEnd` when compiling.
- `Zapf.Parsing/` + `Zapf/` – Assembler for intermediate `.zap` / `.xzap` textual Z-code fragments into final story format (multi-part `.zap`, `_data.zap`, `_str.zap`, optional `_freq.zap`). `ZapfAssembler` orchestrates multi-pass assembly and restart logic.
- `Dezapf/` – Disassembler (currently minimal / limited tests). Not included in packaging (`Build.proj` excludes it from stage).
- `Analyzers/` – Roslyn analyzers & source generators specific to ZILF (e.g., `ZilfAnalyzers.csproj`, `ZilfSourceGenerators`). They are conditionally loaded as analyzers in consuming projects if the compiled DLL exists.
- `Zilf.Playground/` – Web/interactive playground that wraps `FrontEnd` (`Services/Builds/BuildService.cs`, `Services/Repl/ReplService.cs`). Useful reference for embedding patterns.
- `zillib/` & `sample/` – Bundled ZIL library include paths and example games; `sample/*.zil` used for manual or integration scenarios. `sample/advent` is a canonical test case.

Data / Control Flow (compile path): `FrontEnd.Compile` => evaluate ZIL (parsing + interpretation for definitions) => prepare `GameBuilder` + `ZapStreamFactory` => emit `.zap` segments => (optionally) external assembly (`ZapfAssembler`) to Z-machine story file (tests demonstrate chaining in `ZlrHelper`).

### 2. Build & Packaging Workflows
Fast local build (solution-wide): `dotnet build Zilf.sln`.
Core distribution packaging (multi-RID): `dotnet msbuild Build.proj -t:PackageAllRids -p:Configuration=Release` (targets only staged projects `Zilf` and `Zapf`; analyzers & dezapf excluded).
Stage only (no packaging): `dotnet msbuild Build.proj -t:Stage -p:Configuration=Release` → outputs under `Package/<Config>/Stage/<packageName>/` with executables + library + samples + zillib.
CI-specific properties are centralized in `Directory.Build.props` (version stamping, signing, warnings as errors, language version). Avoid duplicating these settings in individual `.csproj` files.

### 3. Runtime / Modes (CLI)
`zilf` modes (see `Program.BuildContext`):
- Compile: `zilf [-c] input.zil [output.zap]`
- Interpret (execute no output): `zilf -x input.zil`
- Expression: `zilf -e "<expr>"`
- REPL: `zilf -i`
Important switches: `-ip <dir>` (include path), `-tr` (trace routines), `-d` (debug info), `-ws code1,code2` (suppress diagnostics), `-we` (warnings as errors), `-w` (enable noisy warnings), case sensitivity `-cs/-ci`.
Include path auto-augmentation: `Program.AddImplicitIncludePaths` heuristically adds directory of input + nearby `zillib` (searches upward, ignoring test dirs); prefer not to reimplement—call existing logic.

### 4. Compilation Pipeline Nuances
- Evaluation (interpreting top-level forms) precedes emission; errors during evaluation abort emission (`FrontEnd.InterpretOrCompile`). Only proceed when `ctx.ErrorCount == 0`.
- Hooks: `ctx.RunHook("PRE-COMPILE")` then `ctx.SetDefaultConstants()` before building game image; if extending pipeline (e.g., extra transformation), insert after PRE-COMPILE but before `GameBuilder` instantiation.
- `ZapStreamFactory` names related segment files using suffixes `_data`, `_str`, and `_freq` (or `freq` w/o underscore) and will request frequent words generation if none exists. When adding new emission outputs, mirror this naming pattern.
- Game options are version-dependent (Z-machine version influences `GameOptions` subclass). Extend `MakeGameOptions` with additional Z-machine flags carefully—maintain existing switch structure.

### 5. Testing Strategy & Conventions
- Framework: MSTest v3 (`Microsoft.NET.Test.Sdk`, `MSTest.TestFramework`); integration helpers in `test/Zilf.Tests.Integration/ZlrHelper.cs` show canonical compile→assemble→execute flow using in-memory FS.
- Use `InMemoryFileSystem` / `OverlayFileSystem` for deterministic tests; don’t write to the real filesystem unless staging packaging scenarios.
- Analyzer and source generator behaviors are implicitly validated by normal builds (analyzers attached conditionally via `<Analyzer Condition="Exists(...)" ...>`). When adding new diagnostics, place IDs in `Analyzers/DiagnosticIds.cs` and create Analyzer + CodeFix pair following existing patterns.

### 6. FileSystem Abstraction
Prefer `IFileSystem` (see usages in `FrontEnd`, `ZapfAssembler`, tests). This enables multi-environment operation (playground, tests, CLI). New features should accept an `IFileSystem` rather than assuming physical disk.

### 7. Diagnostics & Logging
- `Context.DiagnosticManager` drives warnings/errors; suppression list applied from CLI `-ws`. To emit new diagnostics, use existing `Diagnostic` factories or follow analyzer patterns.
- Quiet mode suppresses banner/prompt only—do not gate diagnostic emission on `ctx.Quiet`.
- Diagnostic IDs and messages are centralized as integer constants in `src/Zilf/Diagnostics/InterpreterMessages.cs` (errors/warnings resulting from evaluating FORMs) and `src/Zilf/Diagnostics/CompilerMessages.cs` (errors/warnings from compilation; only raised when in "compile" mode), and grouped by category (e.g. interpreter messages 0300-0399 relate to structured values).
- When adding new diagnostic messages, ensure they have unique IDs, they're in the correct section and in order, they have an `[Error(...)]` attribute with a template message, and that their constant names follow the existing naming conventions (mirroring the template messages, with special characters replaced by underscores).

### 8. Source Generators / Analyzers
`src/Analyzers/ZilfAnalyzers.csproj` & `ZilfSourceGenerators` provide compile-time code analysis. They’re not packaged for runtime; ensure changes remain incremental and respect conditional inclusion path defined in consuming `.csproj` (`Exists('$(ZilfAnalyzersAssembly)')`).

### 9. Versioning / Metadata
Central version numbers: `CurrentVersion.props` → referenced by `Directory.Build.props` to create `InformationalVersion` (`DisplayVersion` logic chooses compressed form per release level). Avoid embedding versions directly in code; call `Program.GetVersion()` for display.

### 10. Packaging & Staging Targets
Custom MSBuild targets (`Build.proj`) define: `Stage`, `Package`, `PackageAllRids`, with multi-RID iteration using `RuntimeIdentifiers`. When adding a new executable project that should enter distributions, add it to `<StageProjects>` and ensure it supports the same RIDs.

### 11. Performance / Determinism
`Deterministic` is disabled (`false`) in several `.csproj` files; builds rely on runtime patch updates (`<TargetLatestRuntimePatch>true</TargetLatestRuntimePatch>`). Don’t assume stable binary hashes between builds. For perf-sensitive code (parsers, emission), keep allocations minimal and reuse spans/iterators (see patterns in tokenizer & parser code—consult those files before altering).

### 12. Safe Extension Points
Use events: `FrontEnd.InitializeContext` and `ZapfAssembler.InitializingContext` to inject context changes instead of modifying core constructors.
REPL integration contract: start via `FrontEnd.StartRepl()` (returns `IReplSession`). Avoid duplicating REPL loop logic from `Program.DoREPL`—use the interface.

### 13. Common Pitfalls
- Forgetting to add include paths leads to missing symbol diagnostics; replicate test helper pattern (add main file directory + library paths) rather than hardcoding.
- Creating `.zap` outputs directly without frequent words file triggers automatic generation; if providing a custom `_freq.zap`, ensure naming matches either `_freq` or `freq` suffix.
- Accessing `Context` global options: always use `ctx.GetGlobalOption(StdAtom.*)` rather than reading internal collections.

### 14. Typical Dev Commands (PowerShell)
```
dotnet restore Zilf.sln
dotnet build Zilf.sln -c Debug
dotnet test Zilf.sln -c Debug --logger "trx;LogFileName=test_results.trx"
dotnet msbuild Build.proj -t:PackageAllRids -p:Configuration=Release
```
**Note**: Projects now target .NET 9 with C# 13. Ensure .NET 9 SDK is installed.

### 15. When Adding New Features
1. Identify correct layer (interpretation vs emission vs assembly). If it affects Z-machine binary, likely belongs in `Zilf.Emit` or assembler; if syntax/semantic evaluation, in interpreter/compiler.
2. Expose through `Context` or `GameOptions` rather than adding ad-hoc globals.
3. Update tests using in-memory FS; prefer integration helper pattern for end-to-end.
4. New commands for use at **evaluation time** (i.e., intepreter commands) go in one of the categorized `src/Zilf/Interpreter/Subrs.<category>.cs` files and are marked with `[Subr(...)]` (or `[FSubr(...)]` for special forms). Arguments are automatically coerced based on method signature; see `src/Zilf/Interpreter/ArgDecoder.cs` for details.
5. When adding new interpreter commands, ensure they have corresponding tests in `test/Zilf.Tests/Interpreter` and follow existing naming and documentation conventions.
6. New commands for use at **compile time** (i.e., Z-code built-in routines) go in `src/Zilf/Compiler/Builtins/ZBuiltins.cs` and are marked with `[Builtin(...)]`. Arguments are also automatically coerced, using a different mechanism; see the `ValidateArguments`, `MakeBuiltinMethodParams`, and `CompileBuiltinCall` methods in that file, as well as `src/zilf/Compiler/Builtins/ParameterTypeHandler.cs` for details.
7. When adding new compile-time built-in routines, ensure they have corresponding tests in `test/Zilf.Tests.Integration` and follow existing naming and documentation conventions.

### 16. Minimal Example (Programmatic Compile)
Reference: pattern from `ZlrHelper`:
```csharp
var fe = new FrontEnd { FileSystem = new InMemoryFileSystem() };
fe.IncludePaths.Add("");
var result = fe.Compile("main.zil", "Output.zap", wantDebugInfo: false);
if (result.Success) new ZapfAssembler { FileSystem = fe.FileSystem }.Assemble("Output.zap", "Output.zcode");
```
