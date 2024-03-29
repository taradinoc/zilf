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
            IOperand arg1;
            IOperand arg2;
            IOperand[] restOfArgs;
            // Convert argsSpan[0] -> arg1 (IOperand)
            throw new NotImplementedException("unimplemented conversion from IOperand");
            // Convert argsSpan[1] -> arg2 (IOperand)
            throw new NotImplementedException("unimplemented conversion from IOperand");
            restOfArgs = new IOperand[argsSpan.Length - 2];
            for (int i = 2, j = 0; i < argsSpan.Length; i++, j++)
            {
                // Convert argsSpan[i] -> restOfArgs[j] (IOperand)
                throw new NotImplementedException("unimplemented conversion from IOperand");
            }
            throw new NotImplementedException();
        }

        private static IOperand Decode_AddOrSubtract(ValueCall c, string op, Span<ZilObject> argsSpan)
        {
            // Decode parameters for ZBuiltins.AddOrSubtract
            int arg1;
            int arg2;
            // Convert argsSpan[0] -> arg1 (int)
            if (argsSpan[0].StdTypeAtom != StdAtom.FIX
            {
                throw new ArgumentException("argument must be a FIX");
            }
            arg1 = ((ZilFix)argsSpan[0]).Value;
            // Convert argsSpan[1] -> arg2 (int)
            if (argsSpan[1].StdTypeAtom != StdAtom.FIX
            {
                throw new ArgumentException("argument must be a FIX");
            }
            arg2 = ((ZilFix)argsSpan[1]).Value;
            throw new NotImplementedException();
        }

        private static IOperand Decode_Option(VoidCall c, Span<ZilObject> argsSpan)
        {
            // Decode parameters for ZBuiltins.Option
            IOperand arg1;
            IOperand arg2;
            // Convert argsSpan[0] -> arg1 (IOperand)
            throw new NotImplementedException("unimplemented conversion from IOperand");
            if (argsSpan.Length > 1)
            {
                arg2 = null;
            }
            else
            {
                // Convert argsSpan[1] -> arg2 (IOperand)
                throw new NotImplementedException("unimplemented conversion from IOperand");
            }
            throw new NotImplementedException();
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
