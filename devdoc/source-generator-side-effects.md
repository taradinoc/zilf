# ZILF Source Generator Side Effects Detection

## Overview

ZILF's source generator automatically generates side effects detection for compiler builtins based on `HasSideEffect = true` attributes in `[Builtin]` declarations. This ensures accuracy for compiler optimization decisions.

## Architecture

### Generated Method
The `ArgumentParserGenerator` analyzes all builtin methods and generates:

```csharp
internal static bool HasSideEffects(string name)
{
    switch (name)
    {
        case "AGAIN":
        case "APPLY":
        case "BUFOUT":
        // ... all builtins with HasSideEffect = true
        case "ZWSTR":
            return true;
        default:
            return false;
    }
}
```

## Key Principles

### Builtin Name Determines Side Effects
- Side effects are determined solely by the builtin **name**, not by version or argument count
- If **any** overload of a builtin has `HasSideEffect = true`, the builtin is considered to have side effects
- This ensures safe compiler optimization - better to be conservative than miss a side effect

### Attribute-Driven Truth Source
- All side effect information comes from `[Builtin(HasSideEffect = true)]` attributes
- No manual maintenance required - adding new builtins with side effects automatically updates the generated method
- Eliminates synchronization issues between attributes and detection logic

### PredCall vs Side Effects
Side effects are defined from a ZIL/MDL perspective:
- **Branching is not a side effect** - predicates that return boolean values are pure operations for optimization purposes
- **State modification is a side effect** - operations that change memory, I/O, or global state must be detected
- Example: `GRTR?` (greater than) is a predicate that branches but has no side effects, while `SET` modifies state and has side effects

## Implementation Details

### Generator Logic
```csharp
private void GenerateHasSideEffectsMethod(IndentedStringBuilder sb, List<OverloadGroup> overloadGroups)
{
    var sideEffectBuiltins = new HashSet<string>();

    // Collect all builtin names where any overload has side effects
    foreach (var group in overloadGroups)
    {
        if (group.Overloads.Any(o => o.Attribute.HasSideEffect))
        {
            sideEffectBuiltins.Add(group.Name);
        }
    }

    sb.AppendLine("internal static bool HasSideEffects(string name)");
    sb.AppendLine("{");
    sb.Indent();
    sb.AppendLine("switch (name)");
    sb.AppendLine("{");
    sb.Indent();

    foreach (var name in sideEffectBuiltins.OrderBy(n => n))
    {
        sb.AppendLine($"case \"{name}\":");
    }

    sb.AppendLine("return true;");
    sb.AppendLine();
    sb.AppendLine("default:");
    sb.AppendLine("return false;");
    sb.Unindent();
    sb.AppendLine("}");
    sb.Unindent();
    sb.AppendLine("}");
}
```

### Side Effect Categories
Current builtins with `HasSideEffect = true`:

**Control Flow**: AGAIN, CALL, QUIT, RESTART, RESTORE, RETURN, RFALSE, RFATAL, RTRUE, THROW
**Memory Operations**: CLEAR, COPYT, DEC, FSET, INC, MOVE, POP, PUSH, PUT, PUTB, PUTP, REMOVE, SET, SETG, XPUSH
**I/O Operations**: BUFOUT, CRLF, DIRIN, DIROUT, INPUT, PRINT*, READ, ZBUFOUT, ZCRLF, ZPRINT*
**System Operations**: COLOR, CURGET, CURSET, FONT, HLIGHT, ISAVE, IRESTORE, LEX, LOWCORE*, MARGIN, RANDOM, SAVE, SCREEN, SOUND, SPLIT, USL, ZRANDOM, ZSAVE
**Display Operations**: DCLEAR, DISPLAY, ERASE, MENU, MOUSE-*, PICINF, PICSET, PRINTF, SCROLL, WINATTR, WINPOS, WINPUT, WINSIZE
