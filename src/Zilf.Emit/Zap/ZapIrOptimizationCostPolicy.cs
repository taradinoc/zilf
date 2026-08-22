/* Copyright 2010-2026 Tara McGrew
 *
 * This file is part of ZILF.
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
