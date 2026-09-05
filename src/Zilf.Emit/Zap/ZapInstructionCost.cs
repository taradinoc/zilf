/* Copyright 2010-2026 Tara McGrew
 * 
 * This file is part of ZILF.
 * 
 * ZILF is free software: you can redistribute it and/or modify it
 * under the terms of the GNU General Public License as published by
 * the Free Software Foundation, either version 3 of the License, or
 * (at your option) any later version.
 * 
 * ZILF is distributed in the hope that it will be useful, but
 * WITHOUT ANY WARRANTY; without even the implied warranty of
 * MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the GNU
 * General Public License for more details.
 * 
 * You should have received a copy of the GNU General Public License
 * along with ZILF.  If not, see <http://www.gnu.org/licenses/>.
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
