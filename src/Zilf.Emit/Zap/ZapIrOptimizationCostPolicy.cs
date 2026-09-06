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

using System.Linq;
using Zilf.Emit.Intermediate;

namespace Zilf.Emit.Zap
{
    internal sealed class ZapIrOptimizationCostPolicy : IIrOptimizationCostPolicy
    {
        public static ZapIrOptimizationCostPolicy Instance { get; } = new();

        public bool ShouldPromoteStackResult(IrInstruction instruction, IrInstruction repeatedInstruction,
            bool reusesExistingTemporary) => !IsInPlaceIncrementOrDecrement(repeatedInstruction) &&
            (reusesExistingTemporary || EstimateInstruction(instruction) >= 2);

        public bool ShouldRewriteAsCopy(IrInstruction instruction, IrValue source) =>
            !IsInPlaceIncrementOrDecrement(instruction) &&
            EstimateInstruction(instruction) > EstimateOperand(source) + 1;

        public bool ShouldHoist(IrInstruction instruction, bool requiresNewTemporary) =>
            !requiresNewTemporary || EstimateInstruction(instruction) > 2;

        public bool ShouldPlacePartialRedundancy(IrInstruction instruction, int insertedEdges) =>
            insertedEdges == 1 && EstimateInstruction(instruction) > 2;

        public bool ShouldCoalescePhi(int removedCopies, int addedCopies) => removedCopies > addedCopies;

        private static int EstimateInstruction(IrInstruction instruction) =>
            1 + instruction.Operands.Sum(EstimateOperand);

        private static bool IsInPlaceIncrementOrDecrement(IrInstruction instruction)
        {
            if (instruction.Payload is not IrLoweringOperation { ResultHome: not null } lowering ||
                instruction.Operands.Count != 2)
                return false;
            IrValue variable;
            if (instruction.Opcode == IrOpcode.Add && instruction.Operands[0].Constant == 1)
                variable = instruction.Operands[1];
            else if (instruction.Opcode is IrOpcode.Add or IrOpcode.Subtract && instruction.Operands[1].Constant == 1)
                variable = instruction.Operands[0];
            else
                return false;
            return ReferenceEquals(variable.PhysicalHome, lowering.ResultHome);
        }

        private static int EstimateOperand(IrValue value) => ZapInstructionCost.EstimateOperand(value.Constant switch
        {
            >= 0 and <= byte.MaxValue => InliningOperandClass.SmallConstant,
            not null => InliningOperandClass.LargeConstant,
            _ when value.PhysicalHome is ILocalBuilder => InliningOperandClass.Local,
            _ => InliningOperandClass.Global,
        });
    }
}
