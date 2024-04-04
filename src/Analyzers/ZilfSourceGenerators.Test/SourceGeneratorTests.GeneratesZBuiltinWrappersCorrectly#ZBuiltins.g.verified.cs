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
        private static void Decode_NegatedVarargsEqualityOp(PredCall c, Span<ZilObject> argsSpan)
        {
            // Decode parameters for ZBuiltins.NegatedVarargsEqualityOp
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
            ZBuiltins.NegatedVarargsEqualityOp(c, arg1, arg2, restOfArgs);
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

        private static void Dispatch_NEq_P(PredCall c, Span<ZilObject> args)
        {
            // Dispatch N=?
            // Exposure { Name = N=?, MinVersion = 1, MaxVersion = 6, HasSideEffect = False, Priority = 1, Data =  }
            // -> NegatedVarargsEqualityOp(IOperand arg1, IOperand arg2, IOperand[] restOfArgs)
            throw new NotImplementedException();
        }

        private static void Dispatch_NEeq_P(PredCall c, Span<ZilObject> args)
        {
            // Dispatch N==?
            // Exposure { Name = N==?, MinVersion = 1, MaxVersion = 6, HasSideEffect = False, Priority = 1, Data =  }
            // -> NegatedVarargsEqualityOp(IOperand arg1, IOperand arg2, IOperand[] restOfArgs)
            throw new NotImplementedException();
        }

        private static IOperand Dispatch_Plus(ValueCall c, Span<ZilObject> args)
        {
            // Dispatch +
            // Exposure { Name = +, MinVersion = 1, MaxVersion = 6, HasSideEffect = False, Priority = 1, Data = add }
            // -> AddOrSubtract(string op, int arg1, int arg2)
            throw new NotImplementedException();
        }

        private static IOperand Dispatch_Minus(ValueCall c, Span<ZilObject> args)
        {
            // Dispatch -
            // Exposure { Name = -, MinVersion = 1, MaxVersion = 6, HasSideEffect = False, Priority = 1, Data = sub }
            // -> AddOrSubtract(string op, int arg1, int arg2)
            throw new NotImplementedException();
        }

        private static void Dispatch_OPTION(VoidCall c, Span<ZilObject> args)
        {
            // Dispatch OPTION
            // Exposure { Name = OPTION, MinVersion = 1, MaxVersion = 6, HasSideEffect = False, Priority = 1, Data =  }
            // -> Option(IOperand arg1, IOperand arg2)
            throw new NotImplementedException();
        }
    }
}
#endif
