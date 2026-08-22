/* Copyright 2010-2026 Tara McGrew
 *
 * This file is part of ZILF.
 */

using System;
using System.Collections.Generic;

namespace Zilf.Emit.Zap
{
    internal static class ZapInstructionCost
    {
        public static InliningCost Estimate(InliningOperationClass operation,
            IReadOnlyList<InliningOperandClass> operands, bool storesResult, bool branches)
        {
            var operandBytes = 0;
            foreach (var operand in operands)
                operandBytes += EstimateOperand(operand);
            var typeBytes = EstimateTypeBytes(operation, operands);
            var bytes = 1 + typeBytes + operandBytes + (storesResult ? 1 : 0) + (branches ? 2 : 0);
            // Calls carry interpreter dispatch, frame setup, and return overhead beyond one ordinary opcode.
            var instructions = operation == InliningOperationClass.DirectCall ? 4 : 1;
            return new InliningCost(bytes, instructions);
        }

        private static int EstimateTypeBytes(InliningOperationClass operation,
            IReadOnlyList<InliningOperandClass> operands)
        {
            if (operation == InliningOperationClass.DirectCall)
            {
                return operands.Count > 4 ? 2 : 1;
            }
            return operands.Count > 2 ? (operands.Count + 3) / 4 : 0;
        }

        public static int EstimateOperand(InliningOperandClass operand) => operand switch
        {
            InliningOperandClass.SmallConstant or InliningOperandClass.Local or InliningOperandClass.Stack => 1,
            _ => 2,
        };
    }
}
