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
using System.Linq;

namespace Zilf.Emit.Intermediate
{
    internal sealed partial class RoutineIrOptimizer
    {
        private static void ProtectCopiesAcrossClobbers(RoutineIr routine)
        {
            foreach (var block in routine.Blocks)
            {
                for (var i = 0; i < block.Instructions.Count; i++)
                {
                    var copy = block.Instructions[i];
                    if (copy.Opcode != IrOpcode.Copy || copy.Result == null || copy.Operands.Count != 1 ||
                        copy.Payload is not IrLoweringOperation copyLowering)
                        continue;
                    var sourceHome = GetPhysicalHome(copy.Operands[0]);
                    if (sourceHome == null)
                        continue;

                    var clobbered = false;
                    for (var j = i + 1; j < block.Instructions.Count; j++)
                    {
                        var instruction = block.Instructions[j];
                        if (clobbered && instruction.Operands.Any(operand => ReferenceEquals(operand, copy.Result)))
                        {
                            copyLowering.RequiredHome = true;
                            break;
                        }
                        if (instruction.Payload is IrLoweringOperation { ResultHome: not null } lowering &&
                            ReferenceEquals(lowering.ResultHome, sourceHome))
                            clobbered = true;
                    }
                    if (!copyLowering.RequiredHome && clobbered && block.Terminator switch
                        {
                            IrTerminator.Branch branch => ReferenceEquals(branch.Condition, copy.Result),
                            IrTerminator.Return ret => ReferenceEquals(ret.Value, copy.Result),
                            _ => false,
                        })
                        copyLowering.RequiredHome = true;
                }
            }
        }

        private static IOperand? GetPhysicalHome(IrValue value) => value.PhysicalHome;

        private static void RemoveRedundantMaterializations(RoutineIr routine)
        {
            var definitions = routine.Blocks.SelectMany(block => block.Instructions)
                .Where(instruction => instruction.Result != null)
                .ToDictionary(instruction => instruction.Result!);
            foreach (var block in routine.Blocks)
            {
                for (var i = block.Instructions.Count - 1; i >= 0; i--)
                {
                    var instruction = block.Instructions[i];
                    if (instruction.Operands.Count != 1 ||
                        instruction.Payload is not IrLoweringOperation
                        {
                            IsMaterialization: true,
                            ResultHome: not null,
                        } materialization ||
                        !definitions.TryGetValue(instruction.Operands[0], out var definition) ||
                        definition.Payload is not IrLoweringOperation { ResultHome: not null } producer ||
                        !ReferenceEquals(materialization.ResultHome, producer.ResultHome))
                        continue;

                    producer.RequiredHome = true;
                    block.Instructions.RemoveAt(i);
                }
            }
        }

        private static void CoalesceMaterializationDestinations(RoutineIr routine)
        {
            foreach (var block in routine.Blocks)
            {
                var definitions = block.Instructions.Select((instruction, index) => (instruction, index))
                    .Where(item => item.instruction.Result != null)
                    .ToDictionary(item => item.instruction.Result!, item => item);
                for (var i = block.Instructions.Count - 1; i >= 0; i--)
                {
                    var materialization = block.Instructions[i];
                    if (materialization.Operands.Count != 1 ||
                        materialization.Payload is not IrLoweringOperation
                        {
                            IsMaterialization: true,
                            ResultHome: not null,
                        } materializationLowering ||
                        !definitions.TryGetValue(materialization.Operands[0], out var definition) ||
                        definition.index >= i ||
                        definition.instruction.Effect == IrEffect.Call ||
                        definition.instruction.Payload is not IrLoweringOperation
                        {
                            EmitTo: not null,
                            RequiredHome: false,
                            IsStackResult: false,
                        } producerLowering ||
                        !CanKeepValueInHome(block, definition.index, i, materializationLowering.ResultHome))
                        continue;

                    producerLowering.ResultHome = materializationLowering.ResultHome;
                    producerLowering.RequiredHome = true;
                    definition.instruction.Result!.PhysicalHome = materializationLowering.ResultHome;
                    block.Instructions.RemoveAt(i);
                }
            }
        }

        private static void ForwardCopyDestinations(RoutineIr routine)
        {
            var useCounts = CountUses(routine);
            var replacements = new Dictionary<IrValue, IrValue>();
            foreach (var block in routine.Blocks)
            {
                var definitions = block.Instructions.Select((instruction, index) => (instruction, index))
                    .Where(item => item.instruction.Result != null)
                    .ToDictionary(item => item.instruction.Result!, item => item);
                for (var i = 0; i < block.Instructions.Count; i++)
                {
                    var copy = block.Instructions[i];
                    if (copy.Opcode != IrOpcode.Copy || copy.Result == null || copy.Operands.Count != 1 ||
                        copy.Payload is not IrLoweringOperation { ResultHome: not null } copyLowering)
                        continue;

                    if (!definitions.TryGetValue(copy.Operands[0], out var definition) || definition.index >= i)
                        continue;
                    var producer = definition.instruction;
                    if (producer.Result == null ||
                        producer.Effect == IrEffect.Call ||
                        useCounts.GetValueOrDefault(producer.Result) != 1 ||
                        producer.Payload is not IrLoweringOperation
                        {
                            EmitTo: not null,
                            RequiredHome: false,
                        } producerLowering)
                        continue;

                    if (!CanKeepValueInHome(block, definition.index, i, copyLowering.ResultHome))
                        continue;

                    producerLowering.ResultHome = copyLowering.ResultHome;
                    producer.Result.PhysicalHome = copyLowering.ResultHome;
                    producerLowering.IsStackResult = copyLowering.IsStackResult;
                    replacements[copy.Result] = producer.Result;
                    block.Instructions.RemoveAt(i);
                    definitions.Remove(copy.Result);
                    foreach (var value in definitions.Values.Where(value => value.index > i).ToArray())
                        definitions[value.instruction.Result!] = (value.instruction, value.index - 1);
                    i--;
                }
            }
            ReplaceValues(routine, replacements);
        }

        private static bool CanKeepValueInHome(IrBlock block, int definitionIndex, int copyIndex,
            IVariable destination)
        {
            for (var i = definitionIndex + 1; i < copyIndex; i++)
            {
                var instruction = block.Instructions[i];
                if (instruction.Effect == IrEffect.Opaque ||
                    instruction.Payload is IrLoweringOperation { ResultHome: not null } lowering &&
                    ReferenceEquals(lowering.ResultHome, destination))
                    return false;
                if (instruction.Operands.Any(operand => ReferenceEquals(GetPhysicalHome(operand), destination)))
                    return false;
            }
            return true;
        }

        private static Dictionary<IrValue, int> CountUses(RoutineIr routine)
        {
            var result = new Dictionary<IrValue, int>();
            void Add(IrValue value) => result[value] = result.GetValueOrDefault(value) + 1;
            foreach (var block in routine.Blocks)
            {
                foreach (var operand in block.Instructions.SelectMany(instruction => instruction.Operands))
                    Add(operand);
                switch (block.Terminator)
                {
                    case IrTerminator.Branch branch:
                        Add(branch.Condition);
                        break;
                    case IrTerminator.Return { Value: not null } ret:
                        Add(ret.Value);
                        break;
                }
            }
            return result;
        }

        private enum LatticeKind
        {
            Undefined,
            Constant,
            Overdefined,
        }

        private readonly record struct LatticeValue(LatticeKind Kind, int Constant = 0)
        {
            public static LatticeValue Meet(LatticeValue left, LatticeValue right)
            {
                if (left.Kind == LatticeKind.Undefined)
                    return right;
                if (right.Kind == LatticeKind.Undefined)
                    return left;
                if (left.Kind == LatticeKind.Constant && right.Kind == LatticeKind.Constant &&
                    left.Constant == right.Constant)
                    return left;
                return new(LatticeKind.Overdefined);
            }
        }

        private int SparseConditionalConstantPropagation(RoutineIr routine)
        {
            var states = new Dictionary<IrValue, LatticeValue>();
            var definitions = routine.Blocks.SelectMany(block => block.Instructions)
                .Where(instruction => instruction.Result != null)
                .Select(instruction => instruction.Result!)
                .ToHashSet();

            LatticeValue State(IrValue value)
            {
                if (value.Constant is int constant)
                    return new(LatticeKind.Constant, Normalize(constant));
                if (!definitions.Contains(value))
                    return new(LatticeKind.Overdefined);
                return states.GetValueOrDefault(value);
            }

            var executableBlocks = new HashSet<IrBlock> { routine.Entry };
            var changed = true;
            while (changed)
            {
                changed = false;
                foreach (var block in routine.Blocks.Where(executableBlocks.Contains))
                {
                    foreach (var instruction in block.Instructions)
                    {
                        if (instruction.Result == null)
                            continue;

                        LatticeValue next;
                        if (!instruction.IsPure || instruction.Opcode == IrOpcode.TargetOperation)
                        {
                            next = new(LatticeKind.Overdefined);
                        }
                        else
                        {
                            var operandStates = instruction.Operands.Select(State).ToArray();
                            if (TryEvaluateKnownFacts(instruction, out var knownResult))
                                next = new(LatticeKind.Constant, knownResult);
                            else if (operandStates.Any(state => state.Kind == LatticeKind.Overdefined))
                                next = new(LatticeKind.Overdefined);
                            else if (operandStates.Any(state => state.Kind == LatticeKind.Undefined))
                                next = default;
                            else if (TryEvaluate(instruction.Opcode,
                                operandStates.Select(state => state.Constant).ToArray(), out var result))
                                next = new(LatticeKind.Constant, result);
                            else
                                next = new(LatticeKind.Overdefined);
                        }

                        var old = states.GetValueOrDefault(instruction.Result);
                        var merged = LatticeValue.Meet(old, next);
                        if (merged != old)
                        {
                            states[instruction.Result] = merged;
                            changed = true;
                        }
                    }

                    IEnumerable<IrBlock> successors = block.Terminator switch
                    {
                        IrTerminator.Jump jump => [jump.Target],
                        IrTerminator.Branch branch when State(branch.Condition) is
                            { Kind: LatticeKind.Constant, Constant: var value } =>
                            [value != 0 ? branch.WhenTrue : branch.WhenFalse],
                        IrTerminator.Branch branch when State(branch.Condition).Kind == LatticeKind.Overdefined =>
                            [branch.WhenTrue, branch.WhenFalse],
                        _ => [],
                    };
                    foreach (var successor in successors)
                    {
                        if (executableBlocks.Add(successor))
                            changed = true;
                    }
                }
            }

            var replacements = states.Where(pair => pair.Value.Kind == LatticeKind.Constant)
                .ToDictionary(pair => pair.Key, pair => routine.CreateConstant(pair.Value.Constant));
            ReplaceValues(routine, replacements);
            return replacements.Count;
        }

        private static bool TryEvaluateKnownFacts(IrInstruction instruction, out int value)
        {
            value = 0;
            if (instruction.Opcode != IrOpcode.Equal || instruction.Operands.Count != 2)
                return false;
            var left = instruction.Operands[0];
            var right = instruction.Operands[1];
            if ((left.Constant == 0 && right.KnownNonzero) || (right.Constant == 0 && left.KnownNonzero))
                return true;
            return false;
        }

    }
}

