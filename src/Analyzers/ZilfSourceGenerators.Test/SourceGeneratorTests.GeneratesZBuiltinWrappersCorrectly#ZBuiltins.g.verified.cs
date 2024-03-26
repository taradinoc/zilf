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
            restOfArgs = new IOperand[argsSpan.Length - 0];
            for (int i = 0, j = 0; i < argsSpan.Length; i++, j++)
            {
                // Convert argsSpan[i] -> restOfArgs[j]
                restOfArgs[j] = default;
            }
            throw new NotImplementedException();
        }

        private static IOperand Decode_AddOrSubtract(ValueCall c, string op, Span<ZilObject> argsSpan)
        {
            // Decode parameters for ZBuiltins.AddOrSubtract
            int arg1;
            int arg2;
            throw new NotImplementedException();
        }

        private static IOperand Decode_Option(VoidCall c, Span<ZilObject> argsSpan)
        {
            // Decode parameters for ZBuiltins.Option
            IOperand arg1;
            IOperand arg2;
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
