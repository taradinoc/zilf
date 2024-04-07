//HintName: ZBuiltins.g.cs
#if ZILF_BUILTIN_WRAPPERS
#nullable enable
#pragma warning disable CS0168 // Variable is declared but never used

using System;
using Zilf.Emit;
using Zilf.Interpreter.Values;
using Zilf.Language;

namespace Zilf.Compiler.Builtins.Generated
{
    static partial class ZBuiltinDecoders
    {
        private static void Decode_NegatedVarargsEqualityOp_V1(PredCall c, Span<ZilObject> argsSpan)
        {
            // Decode parameters for ZBuiltins.NegatedVarargsEqualityOp_V1
            IOperand? arg1;
            IOperand? arg2;
            IOperand[] restOfArgs;
        
            // Operand parameter arg1 will be evaluated
        
            // Operand parameter arg2 will be evaluated
        
            // Varargs parameter restOfArgs will be evaluated
        
            // Evaluate operands
            using Operands temp_operands = c.cc.CompileOperands(c.rb, c.form.SourceLine, [argsSpan[0], argsSpan[1], .. argsSpan.Slice(2)]);
            arg1 = temp_operands[0];
            arg2 = temp_operands[1];
            restOfArgs = temp_operands.ToArray(2);
        
            // Call the implementation
            ZBuiltins.NegatedVarargsEqualityOp_V1(c, arg1, arg2, restOfArgs);
        }

        private static void Decode_NewNegatedVarargsEqualityOp_V3(PredCall c, Span<ZilObject> argsSpan)
        {
            // Decode parameters for ZBuiltins.NewNegatedVarargsEqualityOp_V3
            IOperand? arg1;
            IOperand? arg2;
            IOperand[] restOfArgs;
        
            // Operand parameter arg1 will be evaluated
        
            // Operand parameter arg2 will be evaluated
        
            // Varargs parameter restOfArgs will be evaluated
        
            // Evaluate operands
            using Operands temp_operands = c.cc.CompileOperands(c.rb, c.form.SourceLine, [argsSpan[0], argsSpan[1], .. argsSpan.Slice(2)]);
            arg1 = temp_operands[0];
            arg2 = temp_operands[1];
            restOfArgs = temp_operands.ToArray(2);
        
            // Call the implementation
            ZBuiltins.NewNegatedVarargsEqualityOp_V3(c, arg1, arg2, restOfArgs);
        }

        private static IOperand Decode_AddOrSubtract(ValueCall c, string op, Span<ZilObject> argsSpan)
        {
            // Decode parameters for ZBuiltins.AddOrSubtract
            int arg1;
            int arg2;
        
            // Convert parameter arg1
            if (argsSpan[0].StdTypeAtom != StdAtom.FIX)
            {
                throw new ArgumentException("argument must be a FIX");
            }
            arg1 = ((ZilFix)argsSpan[0]).Value;
        
            // Convert parameter arg2
            if (argsSpan[1].StdTypeAtom != StdAtom.FIX)
            {
                throw new ArgumentException("argument must be a FIX");
            }
            arg2 = ((ZilFix)argsSpan[1]).Value;
        
            // Call the implementation
            return ZBuiltins.AddOrSubtract(c, op, arg1, arg2);
        }

        private static IOperand Decode_ConcatString(ValueCall c, Span<ZilObject> argsSpan)
        {
            // Decode parameters for ZBuiltins.ConcatString
            string arg1;
            string arg2;
        
            // Convert parameter arg1
            if (argsSpan[0] is not ZilString temp_str_arg1)
            {
                throw new ArgumentException("argument must be a literal string");
            }
            arg1 = Compilation.TranslateString(temp_str_arg1, c.cc.Context);
        
            // Convert parameter arg2
            if (argsSpan[1] is not ZilString temp_str_arg2)
            {
                throw new ArgumentException("argument must be a literal string");
            }
            arg2 = Compilation.TranslateString(temp_str_arg2, c.cc.Context);
        
            // Call the implementation
            return ZBuiltins.ConcatString(c, arg1, arg2);
        }

        private static IOperand Decode_Option(VoidCall c, Span<ZilObject> argsSpan)
        {
            // Decode parameters for ZBuiltins.Option
            IOperand? arg1;
            IOperand? arg2;
        
            // Operand parameter arg1 will be evaluated
        
            // Stage optional parameter arg2 for evaluation, if present
            arg2 = null;
            Span<ZilObject> temp_span_arg2 = argsSpan.Length > 1 ? argsSpan.Slice(1, 1) : [];
        
            // Evaluate operands
            using Operands temp_operands = c.cc.CompileOperands(c.rb, c.form.SourceLine, [argsSpan[0], .. temp_span_arg2]);
            arg1 = temp_operands[0];
            arg2 ??= temp_operands[1];
        
            // Call the implementation
            return ZBuiltins.Option(c, arg1, arg2);
        }

        private static IOperand Decode_SetGlobal_IVariable(VoidCall c, Span<ZilObject> argsSpan)
        {
            // Decode parameters for ZBuiltins.SetGlobal
            IVariable? dest;
            ZilObject? value;
        
            // Convert parameter dest
            ZilAtom temp_atom_dest = argsSpan[0] as ZilAtom;
            if (temp_atom_dest is null && !argsSpan[0].IsGVAL(out temp_atom_dest))
            {
                throw new ArgumentException("argument must be a variable");
            }
            if (!c.cc.Locals.ContainsKey(temp_atom_dest) && !c.cc.Globals.ContainsKey(temp_atom_dest))
            {
                throw new ArgumentException("no such variable: " + temp_atom_dest);
            }
            dest = ParameterTypeHandler.GetVariable(c.cc, temp_atom_dest, VariableScopeQuirks.Global)!.Hard;
        
            // Convert parameter value
            value = argsSpan[1];
        
            // Call the implementation
            return ZBuiltins.SetGlobal(c, dest, value);
        }

        private static IOperand Decode_SetGlobal_SoftGlobal(VoidCall c, Span<ZilObject> argsSpan)
        {
            // Decode parameters for ZBuiltins.SetGlobal
            SoftGlobal dest;
            ZilObject? value;
        
            // Convert parameter dest
            ZilAtom temp_atom_dest = argsSpan[0] as ZilAtom;
            if (temp_atom_dest is null && !argsSpan[0].IsGVAL(out temp_atom_dest))
            {
                throw new ArgumentException("argument must be a variable");
            }
            if (!c.cc.SoftGlobals.ContainsKey(temp_atom_dest))
            {
                throw new ArgumentException("no such variable: " + temp_atom_dest);
            }
            dest = ParameterTypeHandler.GetVariable(c.cc, temp_atom_dest, VariableScopeQuirks.Global)!.Soft;
        
            // Convert parameter value
            value = argsSpan[1];
        
            // Call the implementation
            return ZBuiltins.SetGlobal(c, dest, value);
        }

        private static void Dispatch_NEq_P(PredCall c, Span<ZilObject> args)
        {
            // Dispatch N=?
            switch (args)
            {
                case [IOperand[]] when c.cc.Context.ZEnvironment.ZVersion is (>= 3 and <= 6):
                    // Exposure { Name = N=?, MinVersion = 3, MaxVersion = 6, HasSideEffect = False, Priority = 1, Data =  }
                    // -> Decode_NewNegatedVarargsEqualityOp_V3(IOperand arg1, IOperand arg2, IOperand[] restOfArgs)
                    return Zilf.Compiler.Builtins.Generated.ZBuiltinDecoders.Decode_NewNegatedVarargsEqualityOp_V3(c, args);
                case [IOperand[]] when c.cc.Context.ZEnvironment.ZVersion is (>= 1 and <= 2):
                    // Exposure { Name = N=?, MinVersion = 1, MaxVersion = 2, HasSideEffect = False, Priority = 1, Data =  }
                    // -> Decode_NegatedVarargsEqualityOp_V1(IOperand arg1, IOperand arg2, IOperand[] restOfArgs)
                    return Zilf.Compiler.Builtins.Generated.ZBuiltinDecoders.Decode_NegatedVarargsEqualityOp_V1(c, args);
            }
        }

        private static void Dispatch_NEeq_P(PredCall c, Span<ZilObject> args)
        {
            // Dispatch N==?
            switch (args)
            {
                case [IOperand[]] when c.cc.Context.ZEnvironment.ZVersion is (>= 1 and <= 2):
                    // Exposure { Name = N==?, MinVersion = 1, MaxVersion = 2, HasSideEffect = False, Priority = 1, Data =  }
                    // -> Decode_NegatedVarargsEqualityOp_V1(IOperand arg1, IOperand arg2, IOperand[] restOfArgs)
                    return Zilf.Compiler.Builtins.Generated.ZBuiltinDecoders.Decode_NegatedVarargsEqualityOp_V1(c, args);
            }
        }

        private static IOperand Dispatch_Plus(ValueCall c, Span<ZilObject> args)
        {
            // Dispatch +
            switch (args)
            {
                case [ZilFix]:
                    // Exposure { Name = +, MinVersion = 1, MaxVersion = 6, HasSideEffect = False, Priority = 1, Data = add }
                    // -> Decode_AddOrSubtract(string op, int arg1, int arg2)
                    return Zilf.Compiler.Builtins.Generated.ZBuiltinDecoders.Decode_AddOrSubtract(c, add, args);
                case [ZilString]:
                    // Exposure { Name = +, MinVersion = 1, MaxVersion = 6, HasSideEffect = False, Priority = 1, Data =  }
                    // -> Decode_ConcatString(string arg1, string arg2)
                    return Zilf.Compiler.Builtins.Generated.ZBuiltinDecoders.Decode_ConcatString(c, args);
            }
        }

        private static IOperand Dispatch_Minus(ValueCall c, Span<ZilObject> args)
        {
            // Dispatch -
            switch (args)
            {
                case [ZilFix]:
                    // Exposure { Name = -, MinVersion = 1, MaxVersion = 6, HasSideEffect = False, Priority = 1, Data = sub }
                    // -> Decode_AddOrSubtract(string op, int arg1, int arg2)
                    return Zilf.Compiler.Builtins.Generated.ZBuiltinDecoders.Decode_AddOrSubtract(c, sub, args);
            }
        }

        private static void Dispatch_OPTION(VoidCall c, Span<ZilObject> args)
        {
            // Dispatch OPTION
            switch (args)
            {
                case [ZilObject]:
                    // Exposure { Name = OPTION, MinVersion = 1, MaxVersion = 6, HasSideEffect = False, Priority = 1, Data =  }
                    // -> Decode_Option(IOperand arg1, IOperand arg2)
                    Zilf.Compiler.Builtins.Generated.ZBuiltinDecoders.Decode_Option(c, args);
                    return;
            }
        }

        private static void Dispatch_SETG(VoidCall c, Span<ZilObject> args)
        {
            // Dispatch SETG
            switch (args)
            {
                case [ZilObject]:
                    // Exposure { Name = SETG, MinVersion = 1, MaxVersion = 6, HasSideEffect = False, Priority = 1, Data =  }
                    // -> Decode_SetGlobal_SoftGlobal(SoftGlobal dest, ZilObject value)
                    Zilf.Compiler.Builtins.Generated.ZBuiltinDecoders.Decode_SetGlobal_SoftGlobal(c, args);
                    return;
                case [ZilObject]:
                    // Exposure { Name = SETG, MinVersion = 1, MaxVersion = 6, HasSideEffect = False, Priority = 1, Data =  }
                    // -> Decode_SetGlobal_IVariable(IVariable dest, ZilObject value)
                    Zilf.Compiler.Builtins.Generated.ZBuiltinDecoders.Decode_SetGlobal_IVariable(c, args);
                    return;
            }
        }
    }
}
#endif
