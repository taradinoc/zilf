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
    internal enum IrNumericSemantics
    {
        ZMachine16,
        Glulx32,
    }

    internal sealed class RoutineIrOptimizer
    {
        private readonly IrNumericSemantics numericSemantics;
        private readonly Func<IVariable?> acquireTemporary;
        private readonly Action<IVariable, IOperand>? emitCopy;

        public RoutineIrOptimizer(IrNumericSemantics numericSemantics = IrNumericSemantics.Glulx32,
            Func<IVariable?>? acquireTemporary = null, Action<IVariable, IOperand>? emitCopy = null)
        {
            this.numericSemantics = numericSemantics;
            this.acquireTemporary = acquireTemporary ?? (() => null);
            this.emitCopy = emitCopy;
        }

        public void Optimize(RoutineIr routine)
        {
            routine.Verify();
            ForwardCopyDestinations(routine);
            SparseConditionalConstantPropagation(routine);
            SimplifyControlFlow(routine);
            RemoveUnreachableBlocks(routine);
            GlobalValueNumbering(routine);
            var changed = true;
            while (changed)
            {
                changed = FoldConstantsAndCopies(routine);
                changed |= SimplifyControlFlow(routine);
                changed |= RemoveDeadInstructions(routine);
                changed |= RemoveUnreachableBlocks(routine);
            }
            routine.Verify();
        }

        private static void ForwardCopyDestinations(RoutineIr routine)
        {
            var useCounts = CountUses(routine);
            var replacements = new Dictionary<IrValue, IrValue>();
            foreach (var block in routine.Blocks)
            {
                for (var i = 1; i < block.Instructions.Count; i++)
                {
                    var copy = block.Instructions[i];
                    if (copy.Opcode != IrOpcode.Copy || copy.Result == null || copy.Operands.Count != 1 ||
                        copy.Payload is not IrLoweringOperation { ResultHome: not null } copyLowering)
                        continue;

                    var producer = block.Instructions[i - 1];
                    if (!ReferenceEquals(producer.Result, copy.Operands[0]) || producer.Result == null ||
                        useCounts.GetValueOrDefault(producer.Result) != 1 ||
                        producer.Payload is not IrLoweringOperation { EmitTo: not null } producerLowering)
                        continue;

                    producerLowering.ResultHome = copyLowering.ResultHome;
                    producerLowering.IsStackResult = copyLowering.IsStackResult;
                    replacements[copy.Result] = producer.Result;
                    block.Instructions.RemoveAt(i);
                    i--;
                }
            }
            ReplaceValues(routine, replacements);
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

        private void SparseConditionalConstantPropagation(RoutineIr routine)
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
            var executableEdges = new HashSet<(IrBlock From, IrBlock To)>();
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
                        if (instruction.Opcode == IrOpcode.Phi)
                        {
                            next = default;
                            var predecessors = instruction.Payload as IReadOnlyList<IrBlock>;
                            for (var i = 0; i < instruction.Operands.Count; i++)
                            {
                                if (predecessors != null &&
                                    (i >= predecessors.Count || !executableEdges.Contains((predecessors[i], block))))
                                    continue;
                                next = LatticeValue.Meet(next, State(instruction.Operands[i]));
                            }
                        }
                        else if (!instruction.IsPure || instruction.Opcode == IrOpcode.TargetOperation)
                        {
                            next = new(LatticeKind.Overdefined);
                        }
                        else
                        {
                            var operandStates = instruction.Operands.Select(State).ToArray();
                            if (operandStates.Any(state => state.Kind == LatticeKind.Overdefined))
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
                        if (executableEdges.Add((block, successor)))
                            changed = true;
                        if (executableBlocks.Add(successor))
                            changed = true;
                    }
                }
            }

            var replacements = states.Where(pair => pair.Value.Kind == LatticeKind.Constant)
                .ToDictionary(pair => pair.Key, pair => routine.CreateConstant(pair.Value.Constant));
            ReplaceValues(routine, replacements);
        }

        private void GlobalValueNumbering(RoutineIr routine)
        {
            routine.RebuildPredecessors();
            var blocks = routine.Blocks.ToArray();
            var stateDependencies = FindStateDependencies(blocks);
            var all = blocks.ToHashSet();
            var dominators = blocks.ToDictionary(block => block,
                block => ReferenceEquals(block, routine.Entry) ? new HashSet<IrBlock> { block } : new HashSet<IrBlock>(all));
            var changed = true;
            while (changed)
            {
                changed = false;
                foreach (var block in blocks.Where(block => !ReferenceEquals(block, routine.Entry)))
                {
                    var next = block.Predecessors.Count == 0
                        ? new HashSet<IrBlock>()
                        : new HashSet<IrBlock>(dominators[block.Predecessors[0]]);
                    foreach (var predecessor in block.Predecessors.Skip(1))
                        next.IntersectWith(dominators[predecessor]);
                    next.Add(block);
                    if (!next.SetEquals(dominators[block]))
                    {
                        dominators[block] = next;
                        changed = true;
                    }
                }
            }

            var children = blocks.ToDictionary(block => block, _ => new List<IrBlock>());
            foreach (var block in blocks.Where(block => !ReferenceEquals(block, routine.Entry)))
            {
                var idom = dominators[block].Where(candidate => !ReferenceEquals(candidate, block))
                    .OrderByDescending(candidate => dominators[candidate].Count).FirstOrDefault();
                if (idom != null)
                    children[idom].Add(block);
            }

            var replacements = new Dictionary<IrValue, IrValue>();
            IrValue Resolve(IrValue value)
            {
                while (replacements.TryGetValue(value, out var replacement))
                    value = replacement;
                return value;
            }

            void Visit(IrBlock block,
                Dictionary<(IrOpcode Opcode, string Operands), (IrValue Value, IrInstruction Instruction)> available)
            {
                available = new(available);
                foreach (var instruction in block.Instructions)
                {
                    for (var i = 0; i < instruction.Operands.Count; i++)
                        instruction.Operands[i] = Resolve(instruction.Operands[i]);
                    if (instruction.Effect is IrEffect.WriteMemory or IrEffect.Call or IrEffect.Opaque)
                    {
                        if (instruction.Effect == IrEffect.Opaque)
                            available.Clear();
                        else
                        {
                            var writtenRegions = instruction.Effect == IrEffect.Call
                                ? instruction.CallSummary?.GetWrittenRegions() ?? IrMemoryRegion.All
                                : instruction.WriteRegions == IrMemoryRegion.None
                                    ? IrMemoryRegion.All
                                    : instruction.WriteRegions;
                            foreach (var invalidKey in available
                                .Where(pair => (stateDependencies.GetValueOrDefault(pair.Value.Value) &
                                    writtenRegions) != 0)
                                .Select(pair => pair.Key).ToArray())
                                available.Remove(invalidKey);
                        }
                    }
                    var resultHome = (instruction.Payload as IrLoweringOperation)?.ResultHome;
                    (IrOpcode Opcode, string Operands)? currentKey = null;
                    if (instruction.Result != null && IsValueNumberable(instruction))
                    {
                        var currentIds = instruction.Operands.Select(OperandKey).ToArray();
                        if (IsCommutative(instruction.Opcode))
                            Array.Sort(currentIds);
                        currentKey = (instruction.Opcode, string.Join(",", currentIds));
                    }
                    if (resultHome != null)
                    {
                        foreach (var staleKey in available
                            .Where(pair => !currentKey.HasValue || pair.Key != currentKey.Value)
                            .Where(pair => ReferenceEquals(
                                (pair.Value.Instruction.Payload as IrLoweringOperation)?.ResultHome, resultHome))
                            .Select(pair => pair.Key).ToArray())
                            available.Remove(staleKey);
                    }
                    if (instruction.Result == null || !IsValueNumberable(instruction))
                        continue;
                    var key = currentKey!.Value;
                    if (available.TryGetValue(key, out var prior) &&
                        (CanReusePhysicalHome(prior.Instruction, instruction) ||
                         TryPromoteStackValue(prior.Instruction, instruction)))
                    {
                        replacements[instruction.Result] = Resolve(prior.Value);
                    }
                    else if (available.TryGetValue(key, out prior) && TryRewriteAsCopy(prior.Value, instruction))
                    {
                        available[key] = (instruction.Result, instruction);
                    }
                    else
                    {
                        available[key] = (instruction.Result, instruction);
                    }
                }
                block.Terminator = block.Terminator switch
                {
                    IrTerminator.Branch branch => branch with { Condition = Resolve(branch.Condition) },
                    IrTerminator.Return { Value: not null } ret => ret with { Value = Resolve(ret.Value) },
                    _ => block.Terminator,
                };
                foreach (var child in children[block])
                    Visit(child, available);
            }

            Visit(routine.Entry, []);
            ReplaceValues(routine, replacements);
        }

        private bool TryRewriteAsCopy(IrValue priorValue, IrInstruction instruction)
        {
            if (emitCopy == null || instruction.Payload is not IrLoweringOperation
                {
                    ResultHome: not null,
                    IsStackResult: false,
                } lowering || instruction.Operands.Count == 0)
                return false;

            var resultHome = lowering.ResultHome;
            instruction.Opcode = IrOpcode.TargetOperation;
            instruction.Operands.Clear();
            instruction.Operands.Add(priorValue);
            instruction.Payload = new IrLoweringOperation(
                operands => emitCopy(resultHome, operands[0]), resultHome);
            return true;
        }

        private static Dictionary<IrValue, IrMemoryRegion> FindStateDependencies(IEnumerable<IrBlock> blocks)
        {
            var instructions = blocks.SelectMany(block => block.Instructions).ToArray();
            var result = instructions.SelectMany(instruction => instruction.Operands)
                .Where(value => value.MutableExternal)
                .Distinct().ToDictionary(value => value, _ => IrMemoryRegion.Globals);
            var changed = true;
            while (changed)
            {
                changed = false;
                foreach (var instruction in instructions)
                {
                    if (instruction.Result == null)
                        continue;
                    var dependencies = instruction.ReadRegions != IrMemoryRegion.None
                        ? instruction.ReadRegions
                        : GetReadRegions(instruction.Opcode);
                    foreach (var operand in instruction.Operands)
                        dependencies |= result.GetValueOrDefault(operand);
                    if (dependencies != result.GetValueOrDefault(instruction.Result))
                    {
                        result[instruction.Result] = dependencies;
                        changed = true;
                    }
                }
            }
            return result;
        }

        private static IrMemoryRegion GetReadRegions(IrOpcode opcode) => opcode switch
        {
            IrOpcode.LoadByte or IrOpcode.LoadWord or IrOpcode.ScanTable => IrMemoryRegion.Tables,
            IrOpcode.LoadProperty or IrOpcode.LoadPropertyAddress or IrOpcode.LoadNextProperty or
                IrOpcode.LoadPropertySize => IrMemoryRegion.Properties,
            IrOpcode.LoadParent or IrOpcode.LoadChild or IrOpcode.LoadSibling or IrOpcode.Inside =>
                IrMemoryRegion.ObjectTree,
            IrOpcode.HasAttribute => IrMemoryRegion.Attributes,
            _ => IrMemoryRegion.None,
        };

        private bool TryPromoteStackValue(IrInstruction prior, IrInstruction current)
        {
            if (prior.Payload is not IrLoweringOperation
                {
                    IsStackResult: true,
                    StackEscapes: false,
                    EmitTo: not null,
                } priorLowering ||
                current.Payload is not IrLoweringOperation
                {
                    IsStackResult: true,
                    StackEscapes: false,
                })
                return false;

            var temporary = acquireTemporary();
            if (temporary == null)
                return false;

            priorLowering.ResultHome = temporary;
            priorLowering.IsStackResult = false;
            return true;
        }

        private static bool CanReusePhysicalHome(IrInstruction prior, IrInstruction current)
        {
            if (prior.Payload is not IrLoweringOperation priorLowering ||
                current.Payload is not IrLoweringOperation currentLowering)
                return true;
            if (priorLowering.IsStackResult || currentLowering.IsStackResult)
                return false;
            if (IsMemoryRead(prior.Opcode) && prior.Opcode == current.Opcode)
                return priorLowering.ResultHome != null && currentLowering.ResultHome != null;
            return priorLowering.ResultHome != null &&
                ReferenceEquals(priorLowering.ResultHome, currentLowering.ResultHome);
        }

        private static bool IsValueNumberable(IrInstruction instruction) =>
            (instruction.IsPure || instruction.Effect == IrEffect.ReadMemory) && instruction.Opcode is
            IrOpcode.Add or IrOpcode.Subtract or IrOpcode.Multiply or IrOpcode.Divide or IrOpcode.Modulo or
            IrOpcode.BitwiseAnd or IrOpcode.BitwiseOr or IrOpcode.BitwiseNot or IrOpcode.Negate or
            IrOpcode.ShiftLeft or IrOpcode.ShiftRight or IrOpcode.Equal or IrOpcode.LessThan or
            IrOpcode.LessThanOrEqual or IrOpcode.GreaterThan or IrOpcode.GreaterThanOrEqual or IrOpcode.BitTest or
            IrOpcode.LoadByte or IrOpcode.LoadWord or IrOpcode.LoadProperty or IrOpcode.LoadPropertyAddress or
            IrOpcode.LoadNextProperty or IrOpcode.LoadPropertySize or IrOpcode.LoadParent or IrOpcode.LoadChild or
            IrOpcode.LoadSibling;

        private static bool IsMemoryRead(IrOpcode opcode) => opcode is IrOpcode.LoadByte or IrOpcode.LoadWord or
            IrOpcode.LoadProperty or IrOpcode.LoadPropertyAddress or IrOpcode.LoadNextProperty or
            IrOpcode.LoadPropertySize or IrOpcode.LoadParent or IrOpcode.LoadChild or IrOpcode.LoadSibling;

        private static bool IsCommutative(IrOpcode opcode) => opcode is IrOpcode.Add or IrOpcode.Multiply or
            IrOpcode.BitwiseAnd or IrOpcode.BitwiseOr or IrOpcode.Equal;

        private static string OperandKey(IrValue value) => value.Constant is int constant
            ? $"C{constant}"
            : $"V{value.Id}";

        private static void ReplaceValues(RoutineIr routine, IReadOnlyDictionary<IrValue, IrValue> replacements)
        {
            IrValue Resolve(IrValue value)
            {
                while (replacements.TryGetValue(value, out var replacement))
                    value = replacement;
                return value;
            }
            foreach (var block in routine.Blocks)
            {
                foreach (var instruction in block.Instructions)
                    for (var i = 0; i < instruction.Operands.Count; i++)
                        instruction.Operands[i] = Resolve(instruction.Operands[i]);
                block.Terminator = block.Terminator switch
                {
                    IrTerminator.Branch branch => branch with { Condition = Resolve(branch.Condition) },
                    IrTerminator.Return { Value: not null } ret => ret with { Value = Resolve(ret.Value) },
                    _ => block.Terminator,
                };
            }
        }

        private bool FoldConstantsAndCopies(RoutineIr routine)
        {
            var replacements = new Dictionary<IrValue, IrValue>();
            var changed = false;

            IrValue Resolve(IrValue value)
            {
                while (replacements.TryGetValue(value, out var replacement))
                    value = replacement;
                return value;
            }

            foreach (var block in routine.Blocks)
            {
                foreach (var instruction in block.Instructions)
                {
                    for (var i = 0; i < instruction.Operands.Count; i++)
                    {
                        var resolved = Resolve(instruction.Operands[i]);
                        if (!ReferenceEquals(resolved, instruction.Operands[i]))
                        {
                            instruction.Operands[i] = resolved;
                            changed = true;
                        }
                    }

                    if (instruction.Result == null)
                        continue;

                    if (instruction.Opcode == IrOpcode.Copy && instruction.Operands.Count == 1 &&
                        !instruction.Operands[0].MutableExternal)
                    {
                        replacements[instruction.Result] = instruction.Operands[0];
                        continue;
                    }

                    if (instruction.Opcode == IrOpcode.Phi &&
                        instruction.Operands.Count > 0 &&
                        instruction.Operands.All(operand => ReferenceEquals(operand, instruction.Operands[0])))
                    {
                        replacements[instruction.Result] = instruction.Operands[0];
                        continue;
                    }

                    if (TrySimplifyIdentity(instruction, out var replacement))
                    {
                        replacements[instruction.Result] = replacement;
                        continue;
                    }

                    if (TryFold(instruction, out var value))
                        replacements[instruction.Result] = routine.CreateConstant(value);
                }

                block.Terminator = block.Terminator switch
                {
                    IrTerminator.Branch branch => branch with { Condition = Resolve(branch.Condition) },
                    IrTerminator.Return { Value: not null } ret => ret with { Value = Resolve(ret.Value) },
                    _ => block.Terminator,
                };
            }

            if (replacements.Count == 0)
                return changed;

            foreach (var block in routine.Blocks)
            {
                foreach (var instruction in block.Instructions)
                {
                    foreach (var replacement in replacements)
                        instruction.ReplaceOperand(replacement.Key, Resolve(replacement.Value));
                }
            }

            return true;
        }

        private bool TrySimplifyIdentity(IrInstruction instruction, out IrValue replacement)
        {
            replacement = null!;
            if (instruction.Operands.Count != 2)
                return false;

            var left = instruction.Operands[0];
            var right = instruction.Operands[1];
            var allBits = Normalize(-1);
            replacement = instruction.Opcode switch
            {
                IrOpcode.Add when left.Constant == 0 => right,
                IrOpcode.Add when right.Constant == 0 => left,
                IrOpcode.Subtract when right.Constant == 0 => left,
                IrOpcode.Multiply when left.Constant == 1 => right,
                IrOpcode.Multiply when right.Constant == 1 => left,
                IrOpcode.Divide when right.Constant == 1 => left,
                IrOpcode.BitwiseOr when left.Constant == 0 => right,
                IrOpcode.BitwiseOr when right.Constant == 0 => left,
                IrOpcode.BitwiseAnd when left.Constant == allBits => right,
                IrOpcode.BitwiseAnd when right.Constant == allBits => left,
                IrOpcode.ShiftLeft when right.Constant == 0 => left,
                IrOpcode.ShiftRight when right.Constant == 0 => left,
                _ => null!,
            };
            return replacement != null;
        }

        private bool TryFold(IrInstruction instruction, out int value)
        {
            value = 0;
            if (instruction.Operands.Any(static operand => operand.Constant == null))
                return false;

            var operands = instruction.Operands
                .Select(operand => Normalize(operand.Constant!.Value))
                .ToArray();
            return TryEvaluate(instruction.Opcode, operands, out value);
        }

        private bool TryEvaluate(IrOpcode opcode, IReadOnlyList<int> operands, out int value)
        {
            value = 0;
            try
            {
                var unnormalized = opcode switch
                {
                    IrOpcode.Add when operands.Count == 2 => unchecked(operands[0] + operands[1]),
                    IrOpcode.Subtract when operands.Count == 2 => unchecked(operands[0] - operands[1]),
                    IrOpcode.Multiply when operands.Count == 2 => unchecked(operands[0] * operands[1]),
                    IrOpcode.Divide when operands.Count == 2 && operands[1] != 0 => operands[0] / operands[1],
                    IrOpcode.Modulo when operands.Count == 2 && operands[1] != 0 => operands[0] % operands[1],
                    IrOpcode.BitwiseAnd when operands.Count == 2 => operands[0] & operands[1],
                    IrOpcode.BitwiseOr when operands.Count == 2 => operands[0] | operands[1],
                    IrOpcode.BitwiseNot when operands.Count == 1 => ~operands[0],
                    IrOpcode.Negate when operands.Count == 1 => unchecked(-operands[0]),
                    IrOpcode.ShiftLeft when operands.Count == 2 && IsValidShift(operands[1]) =>
                        operands[0] << operands[1],
                    IrOpcode.ShiftRight when operands.Count == 2 && IsValidShift(operands[1]) =>
                        operands[0] >> operands[1],
                    IrOpcode.Equal when operands.Count == 2 => operands[0] == operands[1] ? 1 : 0,
                    IrOpcode.LessThan when operands.Count == 2 => operands[0] < operands[1] ? 1 : 0,
                    IrOpcode.LessThanOrEqual when operands.Count == 2 => operands[0] <= operands[1] ? 1 : 0,
                    IrOpcode.GreaterThan when operands.Count == 2 => operands[0] > operands[1] ? 1 : 0,
                    IrOpcode.GreaterThanOrEqual when operands.Count == 2 => operands[0] >= operands[1] ? 1 : 0,
                    IrOpcode.BitTest when operands.Count == 2 => (operands[0] & operands[1]) == operands[1] ? 1 : 0,
                    _ => throw new InvalidOperationException(),
                };
                value = Normalize(unnormalized);
                return true;
            }
            catch (Exception ex) when (ex is InvalidOperationException or OverflowException)
            {
                return false;
            }
        }

        private int Normalize(int value) => numericSemantics switch
        {
            IrNumericSemantics.ZMachine16 => unchecked((short)value),
            IrNumericSemantics.Glulx32 => value,
            _ => throw new InvalidOperationException($"Unknown numeric semantics: {numericSemantics}"),
        };

        private bool IsValidShift(int count) => count >= 0 && count < (numericSemantics == IrNumericSemantics.ZMachine16 ? 16 : 32);

        private static bool SimplifyControlFlow(RoutineIr routine)
        {
            var changed = false;
            foreach (var block in routine.Blocks)
            {
                if (block.Terminator is IrTerminator.Branch { Condition.Constant: int value } branch)
                {
                    block.Terminator = new IrTerminator.Jump(value != 0 ? branch.WhenTrue : branch.WhenFalse);
                    changed = true;
                }
                else if (block.Terminator is IrTerminator.Branch same && ReferenceEquals(same.WhenTrue, same.WhenFalse))
                {
                    block.Terminator = new IrTerminator.Jump(same.WhenTrue);
                    changed = true;
                }
            }
            return changed;
        }

        private static bool RemoveDeadInstructions(RoutineIr routine)
        {
            var used = new HashSet<IrValue>();
            foreach (var block in routine.Blocks)
            {
                foreach (var instruction in block.Instructions)
                {
                    foreach (var operand in instruction.Operands)
                        used.Add(operand);
                }

                switch (block.Terminator)
                {
                    case IrTerminator.Branch branch:
                        used.Add(branch.Condition);
                        break;
                    case IrTerminator.Return { Value: not null } ret:
                        used.Add(ret.Value);
                        break;
                }
            }

            var changed = false;
            foreach (var block in routine.Blocks)
            {
                for (var i = block.Instructions.Count - 1; i >= 0; i--)
                {
                    var instruction = block.Instructions[i];
                    if (instruction.Result != null && IsRemovable(instruction) && !used.Contains(instruction.Result))
                    {
                        block.Instructions.RemoveAt(i);
                        changed = true;
                    }
                }
            }
            return changed;
        }

        private static bool IsRemovable(IrInstruction instruction) => instruction.IsPure ||
            instruction.Effect == IrEffect.ReadMemory && IsMemoryRead(instruction.Opcode);

        private static bool RemoveUnreachableBlocks(RoutineIr routine)
        {
            var reachable = new HashSet<IrBlock>();
            var work = new Stack<IrBlock>();
            work.Push(routine.Entry);
            while (work.TryPop(out var block))
            {
                if (!reachable.Add(block))
                    continue;
                foreach (var successor in RoutineIr.GetSuccessors(block))
                    work.Push(successor);
            }

            var removed = false;
            for (var i = routine.Blocks.Count - 1; i >= 0; i--)
            {
                if (!reachable.Contains(routine.Blocks[i]))
                {
                    routine.Blocks.RemoveAt(i);
                    removed = true;
                }
            }
            return removed;
        }
    }
}
