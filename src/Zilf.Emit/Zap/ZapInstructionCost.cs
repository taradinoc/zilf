/* Copyright 2010-2026 Tara McGrew
 *
 * This file is part of ZILF.
 */

using System;
using System.Collections.Generic;
using System.Linq;

namespace Zilf.Emit.Zap
{
    internal static class ZapInstructionCost
    {
        public static InliningCost Estimate(InliningOperationClass operation,
            IReadOnlyList<InliningOperandClass> operands, bool storesResult, bool branches)
        {
            var operandBytes = operands.Sum(EstimateOperand);
            var typeBytes = operation == InliningOperationClass.DirectCall || operands.Count > 2
                ? Math.Max(1, (operands.Count + 3) / 4)
                : 0;
            var bytes = 1 + typeBytes + operandBytes + (storesResult ? 1 : 0) + (branches ? 2 : 0);
            // Calls carry interpreter dispatch, frame setup, and return overhead beyond one ordinary opcode.
            var instructions = operation == InliningOperationClass.DirectCall ? 4 : 1;
            return new InliningCost(bytes, instructions);
        }

        public static int EstimateOperand(InliningOperandClass operand) => operand switch
        {
            InliningOperandClass.SmallConstant or InliningOperandClass.Local or InliningOperandClass.Stack => 1,
            _ => 2,
        };
    }
}
