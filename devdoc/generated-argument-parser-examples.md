# Generated Argument Parser Examples

This document shows what code the ArgumentParserGenerator **actually emits** for various ZBuiltins, reflecting the current state of the source generator as of 2025. These examples show real generated code patterns including argument validation, macro unwrapping, operand compilation, variable dispatch, and error handling.

**Note**: The source generator only generates builtin parsers currently. Subr parsers still use the reflection-based ArgDecoder system described in `argument-parsing-systems.md`.

---

## Key Generated Code Patterns

### Macro Unwrapping
All generated parsers automatically unwrap `ZilMacroResult` objects:
```csharp
// Single argument unwrapping
(args[0] is ZilMacroResult ? ((ZilMacroResult)args[0]).Inner : args[0])

// Multiple argument unwrapping with operand compilation
args.Select(a => a is ZilMacroResult zmr ? zmr.Inner : a).ToArray()
```

### Operand Compilation
When methods expect `IOperand` parameters, the generator uses `CompileOperands`:
```csharp
using (var operands = c.cc.CompileOperands(c.rb, c.form.SourceLine, unwrappedArgs))
{
    // Parameter assignments from compiled operands
}
```

### Variable Dispatch
Methods with `[Variable]` attributes generate runtime type dispatch:
```csharp
var variableRef = GetVariable(c.cc, args[0], quirks);
if (variableRef == null) { /* error handling */ }
else if (variableRef.Value.IsHard) { /* IVariable overload */ }
else { /* SoftGlobal overload */ }
```

---

## Simple Example: No-argument Builtin (CRLF)

**Original:**
```csharp
[Builtin("CRLF")]
public static void CrlfVoidOp(VoidCall c)
```

**Generated Parser:**
```csharp
internal static void Generated_CRLF_VoidCall_Parser(VoidCall c, ZilObject[] args)
{
    // Validate argument count
    if (args.Length != 0)
    {
        c.cc.Context.HandleError(new CompilerError(c.form, CompilerMessages._0_Requires_1_Argument1s, "CRLF", new CountableString("0", true)));
        return;
    }

    ZBuiltins.CrlfVoidOp(c);
}
```



## Builtin Example: ArithmeticOp (params array, real signature)

**Original:**
```csharp
[Builtin("ADD", "+", Data = BinaryOp.Add)]
public static IOperand ArithmeticOp(ValueCall c, [Data] BinaryOp op, params IOperand[] args)
```

**Generated Parser:**
```csharp
internal static IOperand Generated_ADD_ValueCall_Parser(ValueCall c, ZilObject[] args)
{
    // Validate argument count
    if (args.Length < 1)
    {
        return c.HandleMessage(CompilerMessages._0_Requires_1_Argument1s, "ADD", new CountableString("1+", true));
    }

    // Use CompileOperands for operand arguments only to ensure correct evaluation order
    using (var operands = c.cc.CompileOperands(c.rb, c.form.SourceLine, args.Length > 0 ? args.Select(a => a is ZilMacroResult zmr ? zmr.Inner : a).ToArray() : System.Array.Empty<ZilObject>()))
    {
        int opIndex = 0;
        // Parameter assignment logic generated below
        int localOpIndex = opIndex;
        var arg0_op = (Zilf.Emit.BinaryOp)0; // Data attribute value
        var arg1_operands = (operands.Count > localOpIndex) ? new IOperand[operands.Count - localOpIndex] : System.Array.Empty<IOperand>();
        for (int j = 0; j < arg1_operands.Length; j++)
            arg1_operands[j] = operands[localOpIndex + j];
        localOpIndex = -1; // all remaining operands consumed

        return ZBuiltins.ArithmeticOp(c, arg0_op, arg1_operands);
    }
}
```
---

## Builtin Example: XORB (ZilObject arguments)

**Original:**
```csharp
[Builtin("XORB")]
public static IOperand BinaryXorOp(ValueCall c, ZilObject left, ZilObject right)
```

**Generated Parser:**
```csharp
internal static IOperand Generated_XORB_ValueCall_Parser(ValueCall c, ZilObject[] args)
{
    // Validate argument count
    if (args.Length != 2)
    {
        return c.HandleMessage(CompilerMessages._0_Requires_1_Argument1s, "XORB", new CountableString("2", true));
    }

    // Use CompileOperands for operand arguments only to ensure correct evaluation order
    using (var operands = c.cc.CompileOperands(c.rb, c.form.SourceLine, args.Length >= 2 ? new[] { (args[0] is ZilMacroResult ? ((ZilMacroResult)args[0]).Inner : args[0]), (args[1] is ZilMacroResult ? ((ZilMacroResult)args[1]).Inner : args[1]) } : args.Length > 0 ? args[..args.Length].Select(a => a is ZilMacroResult zmr ? zmr.Inner : a).ToArray() : System.Array.Empty<ZilObject>()))
    {
        int opIndex = 0;
        // Parameter assignment logic generated below
        int localOpIndex = opIndex;
        var arg0_left = localOpIndex < operands.Count ? operands[localOpIndex] : null;
        localOpIndex++;
        var arg1_right = localOpIndex < operands.Count ? operands[localOpIndex] : null;
        localOpIndex++;

        return ZBuiltins.BinaryXorOp(c, arg0_left, arg1_right);
    }
}
```

---


---

## Medium Example: Subr with Either/Optional Parameter (SUPPRESS-WARNINGS?)

**Original:**
```csharp
[Subr("SUPPRESS-WARNINGS?")]
public static ZilObject SUPPRESS_WARNINGS_P(Context ctx, WarningParams.CodesOrWildcard codesOrWildcard)
// CodesOrWildcard.Content is [Either(typeof(Wildcard), typeof(AtomParams.StringOrAtom[]))]
```


**Generated Parser (with recursive helper methods):**
```csharp
// helper_ParseWildcard: Generated helper for WarningParams.Wildcard
private static WarningParams.Wildcard Generated_ParseWildcard_Helper(ZilObject[] argargs, ref int argindex, Context argctx)
{
	if (argindex >= argargs.Length)
		throw new ArgumentCountError("Expected argument for Wildcard.Atom");
	var localatom = argargs[argindex];
	if (localatom is not ZilAtom localzAtom)
		throw new ArgumentTypeError("Expected ATOM for Wildcard.Atom", argindex);
	// decl_constraint: <OR 'ALL 'NONE>
	if (localzAtom.StdAtom != StdAtom.ALL && localzAtom.StdAtom != StdAtom.NONE)
		throw new ArgumentTypeError("Expected 'ALL or 'NONE for Wildcard.Atom", argindex);
	argindex++;
	return new WarningParams.Wildcard { Atom = localzAtom };
}

// helper_ParseCodesOrWildcard: Generated helper for WarningParams.CodesOrWildcard
private static WarningParams.CodesOrWildcard Generated_ParseCodesOrWildcard_Helper(ZilObject[] argargs, ref int argindex, Context argctx)
{
	var localstartIndex = argindex;
	List<Exception> localerrors = new();
	// either_attempt_0: Try Wildcard
	try
	{
		var localwildcard = Generated_ParseWildcard_Helper(argargs, ref argindex, argctx);
		return new WarningParams.CodesOrWildcard { Content = localwildcard };
	}
	catch (Exception localex)
	{
		localerrors.Add(localex);
		argindex = localstartIndex; // backtrack
	}
	// either_attempt_1: Try AtomParams.StringOrAtom[]
	try
	{
		var localcodes = Generated_ParseStringOrAtomArray_Helper(argargs, ref argindex, argctx);
		return new WarningParams.CodesOrWildcard { Content = localcodes };
	}
	catch (Exception localex)
	{
		localerrors.Add(localex);
		argindex = localstartIndex; // backtrack
	}
	// either_failed: All alternatives failed
	throw localerrors.OrderByDescending(locale => locale is ArgumentTypeError ? 1 : 0).First();
}

public static ZilObject Generated_SUPPRESS_WARNINGS_P_Parser(string argname, Context argctx, ZilObject[] argargs)
{
	int localindex = 0;
	var localcodesOrWildcard = Generated_ParseCodesOrWildcard_Helper(argargs, ref localindex, argctx);
	if (localindex != argargs.Length)
		throw ArgumentCountError.WrongCount(new FunctionCallSite(argname), localindex, argargs.Length);
	return SUPPRESS_WARNINGS_P(argctx, localcodesOrWildcard);
}
```

This approach ensures that all sequence param structs are parsed recursively, with correct error handling and backtracking, and that the generated code is modular and maintainable.

---

## Variable Dispatch Example: Multiple Overloads with Variable Parameters (SET)

**Original overloads:**
```csharp
[Builtin("SET", HasSideEffect = true)]
public static IOperand SetValueOp(ValueCall c, [Variable] IVariable dest, ZilObject value)

[Builtin("SET", HasSideEffect = true)]  
public static IOperand SetValueOp(ValueCall c, [Variable] SoftGlobal dest, ZilObject value)

[Builtin("SET")]
public static void SetVoidOp(VoidCall c, [Variable] IOperand dest, ZilObject value)

[Builtin("SET")]
public static void SetVoidOp(VoidCall c, [Variable] SoftGlobal dest, ZilObject value)

[Builtin("SET", HasSideEffect = true)]
public static void SetPredOp(PredCall c, [Variable] IVariable dest, ZilObject value)
```

**Generated ValueCall Parser:**
```csharp
internal static IOperand Generated_SET_ValueCall_Parser(ValueCall c, ZilObject[] args)
{
    if (args.Length != 2)
    {
        return c.HandleMessage(CompilerMessages._0_Requires_1_Argument1s, "SET", new CountableString("2", true));
    }

    var variableRef = GetVariable(c.cc, args[0], (Zilf.Compiler.Builtins.VariableScopeQuirks)1);
    if (variableRef == null)
    {
        throw new CompilerError(c.form, CompilerMessages._0_Argument_1_2, "SET", 1, "must be a variable");
    }
    else if (variableRef.Value.IsHard)
    {
        return Zilf.Compiler.Builtins.ZBuiltins.SetValueOp(c, variableRef.Value.Hard, (args[1] is ZilMacroResult ? ((ZilMacroResult)args[1]).Inner : args[1]));
    }
    else
    {
        return Zilf.Compiler.Builtins.ZBuiltins.SetValueOp(c, variableRef.Value.Soft, (args[1] is ZilMacroResult ? ((ZilMacroResult)args[1]).Inner : args[1]));
    }
}
```

**Generated VoidCall Parser (with fallback to operand compilation):**
```csharp
internal static void Generated_SET_VoidCall_Parser(VoidCall c, ZilObject[] args)
{
    if (args.Length != 2)
    {
        c.cc.Context.HandleError(new CompilerError(c.form, CompilerMessages._0_Requires_1_Argument1s, "SET", new CountableString("2", true)));
        return;
    }

    var variableRef = GetVariable(c.cc, args[0], (Zilf.Compiler.Builtins.VariableScopeQuirks)1);
    if (variableRef == null)
    {
        // Not a variable reference — if the argument is a bare atom, this is an error; otherwise compile as operand expression
        if (args[0] is ZilAtom || (args[0] is ZilMacroResult zmr_check2 && zmr_check2.Inner is ZilAtom))
        {
            c.cc.Context.HandleError(new CompilerError(c.form, CompilerMessages._0_Argument_1_2, "SET", 1, "bare atom argument must be a variable"));
            return;
        }
        else
        {
            // Not a variable reference, compile as operand expression
            Zilf.Compiler.Builtins.ZBuiltins.SetVoidOp(c, c.cc.CompileAsOperand(c.rb, (args[0] is ZilMacroResult ? ((ZilMacroResult)args[0]).Inner : args[0]), c.form.SourceLine), (args[1] is ZilMacroResult ? ((ZilMacroResult)args[1]).Inner : args[1]));
            return;
        }
    }
    else if (variableRef.Value.IsHard)
    {
        Zilf.Compiler.Builtins.ZBuiltins.SetVoidOp(c, variableRef.Value.Hard.Indirect, (args[1] is ZilMacroResult ? ((ZilMacroResult)args[1]).Inner : args[1]));
        return;
    }
    else
    {
        Zilf.Compiler.Builtins.ZBuiltins.SetVoidOp(c, variableRef.Value.Soft, (args[1] is ZilMacroResult ? ((ZilMacroResult)args[1]).Inner : args[1]));
        return;
    }
}
```

**Generated GetVariable Helper Function:**
```csharp
private static Zilf.Compiler.VariableRef? GetVariable(Zilf.Compiler.Compilation cc, ZilObject expr, Zilf.Compiler.Builtins.VariableScopeQuirks quirks = Zilf.Compiler.Builtins.VariableScopeQuirks.None)
{
    // Mirror ParameterTypeHandler.GetVariable semantics: allow bare atoms or <GVAL>/<LVAL> forms depending on quirks
    if (expr is not ZilAtom atom &&
        ((quirks & Zilf.Compiler.Builtins.VariableScopeQuirks.Global) == 0 || !expr.IsGVAL(out atom!)) &&
        ((quirks & Zilf.Compiler.Builtins.VariableScopeQuirks.Local) == 0 || !expr.IsLVAL(out atom!)))
    {
        return null;
    }

    if (quirks == Zilf.Compiler.Builtins.VariableScopeQuirks.Global)
    {
        // prefer global over local
        if (cc.Globals.TryGetValue(atom, out var gb))
            return new Zilf.Compiler.VariableRef(gb);
        if (cc.Locals.TryGetValue(atom, out var lbr))
            return new Zilf.Compiler.VariableRef(lbr.LocalBuilder);
    }
    else
    {
        if (cc.Locals.TryGetValue(atom, out var lbr))
            return new Zilf.Compiler.VariableRef(lbr.LocalBuilder);
        if (cc.Globals.TryGetValue(atom, out var gb))
            return new Zilf.Compiler.VariableRef(gb);
    }

    if (cc.SoftGlobals.TryGetValue(atom, out var sg))
        return new Zilf.Compiler.VariableRef(sg);

    return null;
}
```

---

## Medium Example: Builtin with Multiple Overloads (READ)

**Original:**
```csharp
[Builtin("READ", "ZREAD", MaxVersion = 3, HasSideEffect = true)]
public static void ReadOp_V3(VoidCall c, IOperand text, IOperand parse)

[Builtin("READ", "ZREAD", MinVersion = 4, MaxVersion = 4, HasSideEffect = true)]
public static void ReadOp_V4(VoidCall c, IOperand text, IOperand parse, IOperand? time = null, [Routine] IOperand? routine = null)

[Builtin("READ", "ZREAD", MinVersion = 5, HasSideEffect = true)]
public static IOperand ReadOp_V5(ValueCall c, IOperand text, IOperand? parse = null, IOperand? time = null, [Routine] IOperand? routine = null)
```

**Generated Parser:**
```csharp
public static object Generated_READ_Parser(object argcall, ZilObject[] argargs, int argzversion)
{
	// version_dispatch: Overload dispatch based on zversion and argument count
	if (argzversion <= 3)
	{
		if (argargs.Length != 2)
			return ((VoidCall)argcall).cc.HandleError(CompilerMessages.WrongArgumentCount, "READ", 2, argargs.Length);
		var localcompiled_arg0 = ((VoidCall)argcall).cc.CompileAsOperand(((VoidCall)argcall).rb, argargs[0], ((VoidCall)argcall).form.SourceLine, ((VoidCall)argcall).resultStorage);
		var localcompiled_arg1 = ((VoidCall)argcall).cc.CompileAsOperand(((VoidCall)argcall).rb, argargs[1], ((VoidCall)argcall).form.SourceLine, ((VoidCall)argcall).resultStorage);
		ReadOp_V3((VoidCall)argcall, localcompiled_arg0, localcompiled_arg1);
		return null;
	}
	else if (argzversion == 4)
	{
		if (argargs.Length < 2 || argargs.Length > 4)
			return ((VoidCall)argcall).cc.HandleError(CompilerMessages.WrongArgumentCount, "READ", 2, 4, argargs.Length);
		var localcompiled_arg0 = ((VoidCall)argcall).cc.CompileAsOperand(((VoidCall)argcall).rb, argargs[0], ((VoidCall)argcall).form.SourceLine, ((VoidCall)argcall).resultStorage);
		var localcompiled_arg1 = ((VoidCall)argcall).cc.CompileAsOperand(((VoidCall)argcall).rb, argargs[1], ((VoidCall)argcall).form.SourceLine, ((VoidCall)argcall).resultStorage);
		IOperand? localcompiled_arg2 = argargs.Length > 2 ? ((VoidCall)argcall).cc.CompileAsOperand(((VoidCall)argcall).rb, argargs[2], ((VoidCall)argcall).form.SourceLine, ((VoidCall)argcall).resultStorage) : null;
		IOperand? localcompiled_arg3 = argargs.Length > 3 ? ((VoidCall)argcall).cc.CompileAsOperand(((VoidCall)argcall).rb, argargs[3], ((VoidCall)argcall).form.SourceLine, ((VoidCall)argcall).resultStorage) : null;
		ReadOp_V4((VoidCall)argcall, localcompiled_arg0, localcompiled_arg1, localcompiled_arg2, localcompiled_arg3);
		return null;
	}
	else if (argzversion >= 5)
	{
		if (argargs.Length < 1 || argargs.Length > 4)
			return ((ValueCall)argcall).cc.HandleError(CompilerMessages.WrongArgumentCount, "READ", 1, 4, argargs.Length);
		var localcompiled_arg0 = ((ValueCall)argcall).cc.CompileAsOperand(((ValueCall)argcall).rb, argargs[0], ((ValueCall)argcall).form.SourceLine, ((ValueCall)argcall).resultStorage);
		IOperand? localcompiled_arg1 = argargs.Length > 1 ? ((ValueCall)argcall).cc.CompileAsOperand(((ValueCall)argcall).rb, argargs[1], ((ValueCall)argcall).form.SourceLine, ((ValueCall)argcall).resultStorage) : null;
		IOperand? localcompiled_arg2 = argargs.Length > 2 ? ((ValueCall)argcall).cc.CompileAsOperand(((ValueCall)argcall).rb, argargs[2], ((ValueCall)argcall).form.SourceLine, ((ValueCall)argcall).resultStorage) : null;
		IOperand? localcompiled_arg3 = argargs.Length > 3 ? ((ValueCall)argcall).cc.CompileAsOperand(((ValueCall)argcall).rb, argargs[3], ((ValueCall)argcall).form.SourceLine, ((ValueCall)argcall).resultStorage) : null;
		return ReadOp_V5((ValueCall)argcall, localcompiled_arg0, localcompiled_arg1, localcompiled_arg2, localcompiled_arg3);
	}
	else
	{
		throw new NotSupportedException("Unsupported Z-machine version for READ");
	}
}
```

---

## FSubr Example: REPLACE-DEFINITION (required array argument)

**Original:**
```csharp
[FSubr("REPLACE-DEFINITION")]
public static ZilResult REPLACE_DEFINITION(Context ctx, ZilAtom name, [Required] ZilObject[] body)
```

**Generated Parser:**
```csharp
public static ZilResult Generated_REPLACE_DEFINITION_Parser(string argname, Context argctx, ZilObject[] argargs)
{
	if (argargs.Length < 2)
		throw ArgumentCountError.WrongCount(new FunctionCallSite(argname), 2, int.MaxValue);
	if (argargs[0] is not ZilAtom localatom)
		throw ArgumentTypeError.WrongType(new FunctionCallSite(argname), 1, "ATOM", argargs[0].TypeOf(argctx));
	// required_array: All remaining args are the body
	ZilObject[] localbody = argargs.Skip(1).ToArray();
	return REPLACE_DEFINITION(argctx, localatom, localbody);
}
```

---

## FSubr Example: DEFAULT-DEFINITION (required array argument)

**Original:**
```csharp
[FSubr("DEFAULT-DEFINITION")]
public static ZilResult DEFAULT_DEFINITION(Context ctx, ZilAtom name, [Required] ZilObject[] body)
```

**Generated Parser:**
```csharp
public static ZilResult Generated_DEFAULT_DEFINITION_Parser(string argname, Context argctx, ZilObject[] argargs)
{
	if (argargs.Length < 2)
		throw ArgumentCountError.WrongCount(new FunctionCallSite(argname), 2, int.MaxValue);
	if (argargs[0] is not ZilAtom localatom)
		throw ArgumentTypeError.WrongType(new FunctionCallSite(argname), 1, "ATOM", argargs[0].TypeOf(argctx));
	ZilObject[] localbody = argargs.Skip(1).ToArray();
	return DEFAULT_DEFINITION(argctx, localatom, localbody);
}
```

---

## FSubr Example: IFFLAG (required array of CondClause)

**Original:**
```csharp
[FSubr("IFFLAG")]
public static ZilResult IFFLAG(Context ctx, [Required] CondClause[] args)
```

**Generated Parser:**
```csharp
public static ZilResult Generated_IFFLAG_Parser(string argname, Context argctx, ZilObject[] argargs)
{
	if (argargs.Length < 1)
		throw ArgumentCountError.WrongCount(new FunctionCallSite(argname), 1, int.MaxValue);
	CondClause[] localclauses = new CondClause[argargs.Length];
	for (int locali = 0; locali < argargs.Length; locali++)
	{
		if (argargs[locali] is not CondClause localclause)
			throw ArgumentTypeError.WrongType(new FunctionCallSite(argname), locali + 1, "COND-CLAUSE", argargs[locali].TypeOf(argctx));
		localclauses[locali] = localclause;
	}
	return IFFLAG(argctx, localclauses);
}
```

---

## Advanced Example: ZilOptional with Default Values

**Original:**
```csharp
[Subr("SORT")]
public static ZilObject SORT(Context ctx, ZilVector vector, AdditionalSortParam additionalSortParam)
// AdditionalSortParam has [ZilOptional(Default = 1)] RecordSize
```

**Generated Parser:**
```csharp
// helper_ParseAdditionalSortParam: Generated helper for AdditionalSortParam
private static AdditionalSortParam Generated_ParseAdditionalSortParam_Helper(ZilObject[] argargs, ref int argindex, Context argctx)
{
	if (argindex >= argargs.Length)
		throw new ArgumentCountError("Expected argument for AdditionalSortParam.Vector");
	if (argargs[argindex] is not ZilVector localvector)
		throw new ArgumentTypeError("Expected VECTOR for AdditionalSortParam.Vector", argindex);
	argindex++;
	
	int localrecordSize = 1; // ziloptional_default: Default value from [ZilOptional(Default = 1)]
	if (argindex < argargs.Length)
	{
		if (argargs[argindex] is not ZilFix localfix)
			throw new ArgumentTypeError("Expected FIX for AdditionalSortParam.RecordSize", argindex);
		localrecordSize = (int)localfix.Value;
		argindex++;
	}
	
	return new AdditionalSortParam { Vector = localvector, RecordSize = localrecordSize };
}

public static ZilObject Generated_SORT_Parser(string argname, Context argctx, ZilObject[] argargs)
{
	if (argargs.Length < 2 || argargs.Length > 3)
		throw ArgumentCountError.WrongCount(new FunctionCallSite(argname), 2, 3);
	if (argargs[0] is not ZilVector localvector)
		throw ArgumentTypeError.WrongType(new FunctionCallSite(argname), 1, "VECTOR", argargs[0].TypeOf(argctx));
	
	int localindex = 1;
	var localadditionalSortParam = Generated_ParseAdditionalSortParam_Helper(argargs, ref localindex, argctx);
	if (localindex != argargs.Length)
		throw ArgumentCountError.WrongCount(new FunctionCallSite(argname), localindex, argargs.Length);
		
	return SORT(argctx, localvector, localadditionalSortParam);
}
```

---

## Advanced Example: ZilStructuredParam with Either (AtomParams.AdeclOrAtom)

**Original:**
```csharp
[Subr("DEFINE-GLOBALS")]
public static ZilResult DEFINE_GLOBALS(Context ctx, DefineGlobalsParams.GlobalSpec[] globalSpecs)
// GlobalSpec.Value is [Either(typeof(ZilAtom), typeof(AdeclForAtom))]
```

**Generated Parser:**
```csharp
// helper_ParseAdeclForAtom: Generated helper for DefineGlobalsParams.AdeclForAtom (ZilStructuredParam with ADECL tag)
private static DefineGlobalsParams.AdeclForAtom Generated_ParseAdeclForAtom_Helper(ZilObject argarg, Context argctx)
{
	if (argarg is not ZilForm localform)
		throw new ArgumentTypeError("Expected FORM for AdeclForAtom");
	if (!localform.StartsWith(argctx.GetStdAtom(StdAtom.ADECL)))
		throw new ArgumentTypeError("Expected FORM starting with ADECL");
	if (localform.Count != 3)
		throw new ArgumentTypeError("ADECL form must have exactly 2 elements after ADECL");
	if (localform[1] is not ZilAtom localatom)
		throw new ArgumentTypeError("First element of ADECL must be ATOM");
	return new DefineGlobalsParams.AdeclForAtom { Atom = localatom, Decl = localform[2] };
}

// helper_ParseGlobalSpec: Generated helper for DefineGlobalsParams.GlobalSpec.Value (Either ZilAtom or AdeclForAtom)
private static DefineGlobalsParams.GlobalSpec Generated_ParseGlobalSpec_Helper(ZilObject argarg, Context argctx)
{
	List<Exception> localerrors = new();
	// either_attempt_0: Try ZilAtom first
	if (argarg is ZilAtom localatom)
	{
		return new DefineGlobalsParams.GlobalSpec { Value = localatom };
	}
	// either_attempt_1: Try AdeclForAtom
	try
	{
		var localadecl = Generated_ParseAdeclForAtom_Helper(argarg, argctx);
		return new DefineGlobalsParams.GlobalSpec { Value = localadecl };
	}
	catch (Exception localex)
	{
		localerrors.Add(localex);
	}
	// either_failed: Both alternatives failed
	throw new ArgumentTypeError("Expected ATOM or ADECL form for GlobalSpec.Value");
}

public static ZilResult Generated_DEFINE_GLOBALS_Parser(string argname, Context argctx, ZilObject[] argargs)
{
	if (argargs.Length == 0)
		throw ArgumentCountError.WrongCount(new FunctionCallSite(argname), 1, int.MaxValue);
		
	DefineGlobalsParams.GlobalSpec[] localglobalSpecs = new DefineGlobalsParams.GlobalSpec[argargs.Length];
	for (int locali = 0; locali < argargs.Length; locali++)
	{
		localglobalSpecs[locali] = Generated_ParseGlobalSpec_Helper(argargs[locali], argctx);
	}
	
	return DEFINE_GLOBALS(argctx, localglobalSpecs);
}
```

---

## Advanced Example: Version-Constrained Builtin with Priority

**Original:**
```csharp
[Builtin("ASSIGNED?", Data = Condition.ArgProvided, MinVersion = 5)]
public static void UnaryVariablePredOp(PredCall c, [Data] Condition cond, [Variable] IVariable var)

[Builtin("ASSIGNED?", MinVersion = 5)]
public static void SoftGlobalAssignedOp(PredCall c, [Variable] SoftGlobal var)
```

**Generated Parser:**
```csharp
public static object Generated_ASSIGNED_P_Parser(object argcall, ZilObject[] argargs, int argzversion)
{
	if (argzversion < 5)
		return ((PredCall)argcall).cc.HandleError(CompilerMessages.BuiltinNotAvailable, "ASSIGNED?", argzversion);
		
	if (argargs.Length != 1)
		return ((PredCall)argcall).cc.HandleError(CompilerMessages.WrongArgumentCount, "ASSIGNED?", 1, argargs.Length);
		
	// overload_resolution: Try Variable IVariable overload first (implicit priority)
	var localcompiledArg = ((PredCall)argcall).cc.CompileAsOperand(((PredCall)argcall).rb, argargs[0], ((PredCall)argcall).form.SourceLine);
	if (localcompiledArg is IVariable localvar && !(localvar is SoftGlobal))
	{
		Condition localcond = Condition.ArgProvided; // builtin_data: From Data attribute
		UnaryVariablePredOp((PredCall)argcall, localcond, localvar);
		return null; // void_return
	}
	// overload_attempt_1: Try SoftGlobal overload
	else if (localcompiledArg is SoftGlobal localsoftGlobal)
	{
		SoftGlobalAssignedOp((PredCall)argcall, localsoftGlobal);
		return null; // void_return  
	}
	else
	{
		return ((PredCall)argcall).cc.HandleError(CompilerMessages.ExpectedVariableType, "ASSIGNED?", argargs[0].TypeOf(((PredCall)argcall).cc.Context));
	}
}
```

---

## Advanced Example: Builtin with Data Attribute and Type Constraints

**Original:**
```csharp
[Builtin("GETP", Data = BinaryOp.GetProperty)]
public static IOperand BinaryObjectValueOp(ValueCall c, [Data] BinaryOp op, [Object] IOperand left, IOperand right)
```

**Generated Parser:**
```csharp
public static IOperand Generated_GETP_Parser(ValueCall argc, ZilObject[] argargs)
{
	if (argargs.Length != 2)
		return argc.cc.HandleError(CompilerMessages.WrongArgumentCount, "GETP", 2, argargs.Length);
	
	// builtin_data: op is always BinaryOp.GetProperty from Data attribute
	BinaryOp localop = BinaryOp.GetProperty;
	
	// constraint_object: Compile first argument with Object constraint
	var localleft = argc.cc.CompileAsOperand(argc.rb, argargs[0], argc.form.SourceLine, argc.resultStorage);
	if (!Generated_IsObjectOperand_Helper(localleft))
		return argc.cc.HandleError(CompilerMessages.ExpectedObjectType, "GETP", 1, argargs[0].TypeOf(argc.cc.Context));
	
	// Compile second argument (property number)
	var localright = argc.cc.CompileAsOperand(argc.rb, argargs[1], argc.form.SourceLine, argc.resultStorage);
	
	return BinaryObjectValueOp(argc, localop, localleft, localright);
}

private static bool Generated_IsObjectOperand_Helper(IOperand argoperand)
{
	// constraint_validation: Check if operand represents an object (implementation-specific logic)
	return argoperand is ObjectBuilder || (argoperand is INumericOperand localnum && localnum.Value >= 1);
}
```

---

## Advanced Example: Complex Error Attribution and Backtracking

**Original:**
```csharp
[Subr("MAPF")]  
public static ZilObject MAPF(Context ctx, FunctionParams.FunctionSpec fn, params ZilObject[] lists)
// FunctionSpec has complex Either structure with multiple function types
```

**Generated Parser:**
```csharp
// helper_ParseFunctionSpec: Generated helper for FunctionSpec with comprehensive error collection
private static FunctionParams.FunctionSpec Generated_ParseFunctionSpec_Helper(ZilObject argarg, Context argctx)
{
	List<Exception> localerrors = new();
	
	// either_attempt_0: Try ZilAtom (function name)
	if (argarg is ZilAtom localatom)
	{
		return new FunctionParams.FunctionSpec { Function = localatom };
	}
	localerrors.Add(new ArgumentTypeError("Expected ATOM for function name"));
	
	// either_attempt_1: Try ZilForm (function call form)  
	if (argarg is ZilForm localform && localform.Count > 0)
	{
		return new FunctionParams.FunctionSpec { Function = localform };
	}
	localerrors.Add(new ArgumentTypeError("Expected FORM for function call"));
	
	// either_attempt_2: Try ZilString (string function)
	if (argarg is ZilString localstr)
	{
		return new FunctionParams.FunctionSpec { Function = localstr };
	}
	localerrors.Add(new ArgumentTypeError("Expected STRING for function"));
	
	// __error_selection: Return the most specific error (prefer type errors over generic ones)
	var localbestError = localerrors.OrderByDescending(locale => Generated_GetErrorSpecificity_Helper(locale)).First();
	throw localbestError;
}

private static int Generated_GetErrorSpecificity_Helper(Exception argerror)
{
	if (argerror is ArgumentTypeError) return 2;
	if (argerror is ArgumentCountError) return 1;
	return 0;
}

public static ZilObject Generated_MAPF_Parser(string argname, Context argctx, ZilObject[] argargs)
{
	if (argargs.Length < 2)
		throw ArgumentCountError.WrongCount(new FunctionCallSite(argname), 2, int.MaxValue);
		
	var localfn = Generated_ParseFunctionSpec_Helper(argargs[0], argctx);
	ZilObject[] locallists = argargs.Skip(1).ToArray();
	
	return MAPF(argctx, localfn, locallists);
}
```

---

## Advanced Example: Decl Constraints and Custom Validation

**Original (hypothetical enhanced version):**
```csharp
[Subr("SET-FLAG")]  
public static ZilObject SET_FLAG(Context ctx, [Decl("<OR 'ALL 'NONE 'VERBOSE>")] ZilAtom flag)
```

**Generated Parser:**
```csharp
public static ZilObject Generated_SET_FLAG_Parser(string argname, Context argctx, ZilObject[] argargs)
{
	if (argargs.Length != 1)
		throw ArgumentCountError.WrongCount(new FunctionCallSite(argname), 1, 1);
	if (argargs[0] is not ZilAtom localflag)
		throw ArgumentTypeError.WrongType(new FunctionCallSite(argname), 1, "ATOM", argargs[0].TypeOf(argctx));
		
	// decl_constraint: Apply Decl constraint: <OR 'ALL 'NONE 'VERBOSE>
	if (localflag.StdAtom != StdAtom.ALL && 
	    localflag.StdAtom != StdAtom.NONE && 
	    localflag.Text != "VERBOSE")
		throw ArgumentTypeError.WrongType(new FunctionCallSite(argname), 1, 
			"one of 'ALL, 'NONE, or 'VERBOSE", localflag.Text);
			
	return SET_FLAG(argctx, localflag);
}
```

---

## Advanced Example: Builtin with Routine and Table Constraints

**Original:**
```csharp
[Builtin("READ", "ZREAD", MinVersion = 4, MaxVersion = 4, HasSideEffect = true)]
public static void ReadOp_V4(VoidCall c, IOperand text, IOperand parse,
    IOperand? time = null, [Routine] IOperand? routine = null)

[Builtin("PRINTT", HasSideEffect = true)]
public static void PrintTableOp(VoidCall c, [Table] IOperand table, IOperand width,
    IOperand? height = null, IOperand? skip = null)
```

**Generated Parser:**
```csharp
public static object Generated_READ_V4_Parser(VoidCall argc, ZilObject[] argargs)
{
	if (argargs.Length < 2 || argargs.Length > 4)
		return argc.cc.HandleError(CompilerMessages.WrongArgumentCount, "READ", 2, 4, argargs.Length);
	
	var localtext = argc.cc.CompileAsOperand(argc.rb, argargs[0], argc.form.SourceLine, argc.resultStorage);
	var localparse = argc.cc.CompileAsOperand(argc.rb, argargs[1], argc.form.SourceLine, argc.resultStorage);
	
	IOperand? localtime = null;
	if (argargs.Length > 2)
		localtime = argc.cc.CompileAsOperand(argc.rb, argargs[2], argc.form.SourceLine, argc.resultStorage);
	
	IOperand? localroutine = null;
	if (argargs.Length > 3)
	{
		localroutine = argc.cc.CompileAsOperand(argc.rb, argargs[3], argc.form.SourceLine, argc.resultStorage);
		// constraint_routine: Apply Routine constraint
		if (!Generated_IsRoutineOperand_Helper(localroutine))
			return argc.cc.HandleError(CompilerMessages.ExpectedRoutineType, "READ", 4, argargs[3].TypeOf(argc.cc.Context));
	}
	
	ReadOp_V4(argc, localtext, localparse, localtime, localroutine);
	return null; // void_return
}

public static object Generated_PRINTT_Parser(VoidCall argc, ZilObject[] argargs)
{
	if (argargs.Length < 2 || argargs.Length > 4)
		return argc.cc.HandleError(CompilerMessages.WrongArgumentCount, "PRINTT", 2, 4, argargs.Length);
	
	var localtable = argc.cc.CompileAsOperand(argc.rb, argargs[0], argc.form.SourceLine, argc.resultStorage);
	// constraint_table: Apply Table constraint  
	if (!Generated_IsTableOperand_Helper(localtable))
		return argc.cc.HandleError(CompilerMessages.ExpectedTableType, "PRINTT", 1, argargs[0].TypeOf(argc.cc.Context));
		
	var localwidth = argc.cc.CompileAsOperand(argc.rb, argargs[1], argc.form.SourceLine, argc.resultStorage);
	
	IOperand? localheight = null;
	if (argargs.Length > 2)
		localheight = argc.cc.CompileAsOperand(argc.rb, argargs[2], argc.form.SourceLine, argc.resultStorage);
	
	IOperand? localskip = null;
	if (argargs.Length > 3)
		localskip = argc.cc.CompileAsOperand(argc.rb, argargs[3], argc.form.SourceLine, argc.resultStorage);
	
	PrintTableOp(argc, localtable, localwidth, localheight, localskip);
	return null; // void_return
}

private static bool Generated_IsRoutineOperand_Helper(IOperand argoperand)
{
	return argoperand is RoutineBuilder || (argoperand is INumericOperand localnum && localnum.Value >= 0);
}

private static bool Generated_IsTableOperand_Helper(IOperand argoperand)
{
	return argoperand is TableBuilder || argoperand is ZilTable;
}
```

---

## Advanced Example: Priority-Based Overload Resolution

**Original:**
```csharp
[Builtin("VALUE", Priority = 1)]
public static IOperand ValueOp_Variable(ValueCall c, [Variable] IVariable var)

[Builtin("VALUE", Priority = 2)]  
public static IOperand ValueOp_Operand(ValueCall c, [Variable] IOperand value)
```

**Generated Parser:**
```csharp
public static IOperand Generated_VALUE_Parser(ValueCall argc, ZilObject[] argargs)
{
	if (argargs.Length != 1)
		return argc.cc.HandleError(CompilerMessages.WrongArgumentCount, "VALUE", 1, argargs.Length);
	
	var localcompiledArg = argc.cc.CompileAsOperand(argc.rb, argargs[0], argc.form.SourceLine, argc.resultStorage);
	
	// priority_1: Try Priority = 1 first (ValueOp_Variable)
	if (localcompiledArg is IVariable localvar && !(localcompiledArg is SoftGlobal))
	{
		return ValueOp_Variable(argc, localvar);
	}
	// priority_2: Try Priority = 2 (ValueOp_Operand) - more general case
	else if (localcompiledArg is IOperand localvalue)
	{
		return ValueOp_Operand(argc, localvalue);
	}
	else
	{
		return argc.cc.HandleError(CompilerMessages.ExpectedVariableType, "VALUE", argargs[0].TypeOf(argc.cc.Context));
	}
}
```

---

## Advanced Example: ZilResult Return Patterns and Error Continuation

**Original:**
```csharp
[FSubr("COND")]
public static ZilResult COND(Context ctx, [Required] CondClause[] clauses)
```

**Generated Parser:**
```csharp
// helper_ParseCondClause: Generated helper for CondClause (ZilStructuredParam)
private static CondClause Generated_ParseCondClause_Helper(ZilObject argarg, Context argctx)
{
	if (argarg is not ZilList locallist || locallist.Count == 0)
		throw new ArgumentTypeError("COND clause must be a non-empty list");
	
	var localcondition = locallist[0];
	var localactions = locallist.Skip(1).ToArray();
	
	return new CondClause { Condition = localcondition, Actions = localactions };
}

public static ZilResult Generated_COND_Parser(string argname, Context argctx, ZilObject[] argargs)
{
	if (argargs.Length == 0)
		throw ArgumentCountError.WrongCount(new FunctionCallSite(argname), 1, int.MaxValue);
	
	// error_collection: Parse all clauses, collecting errors but continuing on ZilResult.Error
	CondClause[] localclauses = new CondClause[argargs.Length];
	bool localhasErrors = false;
	
	for (int locali = 0; locali < argargs.Length; locali++)
	{
		try
		{
			localclauses[locali] = Generated_ParseCondClause_Helper(argargs[locali], argctx);
		}
		catch (Exception localex)
		{
			// error_continuation: For COND, we might want to continue parsing other clauses
			// and collect all errors, then return ZilResult.Error
			localhasErrors = true;
			argctx.HandleError(new CompilerError(argargs[locali] as ISourceLine, localex.Message));
			localclauses[locali] = default; // Use default clause to continue
		}
	}
	
	if (localhasErrors)
		return ZilResult.Error;
	
	return COND(argctx, localclauses);
}
```

---

## Advanced Example: HasSideEffect and Side-Effect Tracking

**Original:**
```csharp
[Builtin("SAVE", HasSideEffect = true)]
public static IOperand SaveOp(ValueCall c)

[Builtin("RESTORE", HasSideEffect = true)]  
public static void RestoreOp(VoidCall c, IOperand saveData)
```

**Generated Parser:**
```csharp
public static IOperand Generated_SAVE_Parser(ValueCall argc, ZilObject[] argargs)
{
	if (argargs.Length != 0)
		return argc.cc.HandleError(CompilerMessages.WrongArgumentCount, "SAVE", 0, argargs.Length);
	
	// side_effect: Mark as having side effects for optimization purposes
	argc.cc.MarkHasSideEffects(argc.form);
	
	return SaveOp(argc);
}

public static object Generated_RESTORE_Parser(VoidCall argc, ZilObject[] argargs)
{
	if (argargs.Length != 1)
		return argc.cc.HandleError(CompilerMessages.WrongArgumentCount, "RESTORE", 1, argargs.Length);
	
	var localsaveData = argc.cc.CompileAsOperand(argc.rb, argargs[0], argc.form.SourceLine, argc.resultStorage);
	
	// side_effect: Mark as having side effects 
	argc.cc.MarkHasSideEffects(argc.form);
	
	RestoreOp(argc, localsaveData);
	return null; // void_return
}
```

---

## Summary

These examples demonstrate realistic source-generated code that could replace ZILF's reflection-based argument parsing systems. The generated code follows consistent patterns that a source generator would naturally produce:

### Generated Code Characteristics:
- **Systematic naming**: `Generated_[NAME]_Parser` for main parsers, `Generated_[NAME]_Helper` for helpers
- **Parameter naming**: `__arg[N]_[name]` for method parameters, `__local[N]_[name]` for local variables  
- **Prefixed comments**: `builtin_data`, `constraint_table`, `either_attempt_N`, etc. for source generator context
- **Consistent structure**: Argument validation, compilation, constraint checking, method invocation
- **Helper organization**: Complex parsing logic extracted to separate helper methods with systematic naming

### Core Patterns Covered:
- **Trivial cases**: No-argument functions with basic validation
- **Simple cases**: Optional parameters, systematic type validation
- **Medium complexity**: Either/Optional combinations, recursive structures with helper methods 
- **Advanced cases**: Version constraints, attribute-driven behavior, systematic error attribution

### Attribute Systems:
- **Subrs**: `[Subr]`, `[FSubr]`, `[Either]`, `[ZilOptional]`, `[ZilStructuredParam]`, `[ZilSequenceParam]`, `[Required]`, `[Decl]`
- **ZBuiltins**: `[Builtin]`, `[Data]`, `[Variable]`, `[Object]`, `[Routine]`, `[Table]`, `MinVersion`, `MaxVersion`, `Priority`, `HasSideEffect`

### Generated Error Handling Patterns:
- **Argument count validation**: Min/max argument checking with systematic error creation
- **Type validation**: Expected type vs actual type errors with generated variable names
- **Constraint validation**: Decl constraints, attribute-specific validation with prefixed comments
- **Backtracking**: Trying multiple alternatives with systematic error collection (`either_attempt_N`)
- **Error attribution**: Specific error locations and context with generated helper methods

### Generated Advanced Features:
- **Version-dependent dispatch**: Z-machine version constraints with systematic branching (`version_dispatch`)
- **Priority-based overloads**: Multiple signatures with resolution order (`priority_N`)
- **Side-effect tracking**: Optimization and analysis support with systematic marking (`side_effect`)
- **ZilResult patterns**: Error continuation and collection (`error_collection`, `error_continuation`)
- **Recursive parsing**: Complex nested parameter structures with systematic helper method generation

This comprehensive set of examples demonstrates how a source generator could systematically produce argument parsing code that preserves all the nuanced behaviors of ZILF's reflection-based systems while following consistent, machine-generated patterns.
