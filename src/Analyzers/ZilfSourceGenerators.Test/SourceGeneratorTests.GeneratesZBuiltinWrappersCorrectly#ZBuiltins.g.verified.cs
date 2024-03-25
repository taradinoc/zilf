//HintName: ZBuiltins.g.cs
#nullable enable
#pragma warning disable CS0168 // Variable is declared but never used
using System;
using Zilf.Emit;
using Zilf.Language;
using Zilf.Interpreter.Values;
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
            throw new NotImplementedException();
        }
        private static void Dispatch_NEq_P(PredCall c, Span<ZilObject> args)
        {
            // Dispatch N=?
            // Exposure { Name = N=?, MinVersion = 1, MaxVersion = 6, HasSideEffect = False, Priority = 1, Data =  }
            throw new NotImplementedException();
        }
        private static void Dispatch_NEeq_P(PredCall c, Span<ZilObject> args)
        {
            // Dispatch N==?
            // Exposure { Name = N==?, MinVersion = 1, MaxVersion = 6, HasSideEffect = False, Priority = 1, Data =  }
            throw new NotImplementedException();
        }
    }
}
