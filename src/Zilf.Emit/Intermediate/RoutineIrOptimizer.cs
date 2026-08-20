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
        private readonly Func<IEnumerable<IVariable>> reusableTemporaries;
        private readonly Action<IVariable, IOperand>? emitCopy;
        private readonly Func<IrBlock, IrBlock>? createPreheader;
        private readonly Dictionary<string, int> statistics = new(StringComparer.Ordinal);

        public RoutineIrOptimizer(IrNumericSemantics numericSemantics = IrNumericSemantics.Glulx32,
            Func<IVariable?>? acquireTemporary = null, Action<IVariable, IOperand>? emitCopy = null,
            Func<IEnumerable<IVariable>>? reusableTemporaries = null,
            Func<IrBlock, IrBlock>? createPreheader = null)
        {
            this.numericSemantics = numericSemantics;
            this.acquireTemporary = acquireTemporary ?? (() => null);
            this.emitCopy = emitCopy;
            this.reusableTemporaries = reusableTemporaries ?? (() => []);
            this.createPreheader = createPreheader;
        }

        public void Optimize(RoutineIr routine)
        {
            statistics.Clear();
            routine.Verify();
            statistics["Input instructions"] = routine.Blocks.Sum(block => block.Instructions.Count);
            statistics["Input opaque operations"] = routine.Blocks.Sum(block => block.Instructions.Count(instruction =>
                instruction.Effect == IrEffect.Opaque));
            ProtectCopiesAcrossClobbers(routine);
            CoalesceMaterializationDestinations(routine);
            RemoveRedundantMaterializations(routine);
            ForwardCopyDestinations(routine);
            Record("SCCP constants", SparseConditionalConstantPropagation(routine));
            SimplifyControlFlow(routine);
            RemoveUnreachableBlocks(routine);
            CreateLoopPreheaders(routine);
            SimplifyInductionVariables(routine);
            LoopInvariantCodeMotion(routine);
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
            statistics["Output instructions"] = routine.Blocks.Sum(block => block.Instructions.Count);
            statistics["Output opaque operations"] = routine.Blocks.Sum(block => block.Instructions.Count(instruction =>
                instruction.Effect == IrEffect.Opaque));
        }

        public IEnumerable<IrOptimizationStat> GetStatistics() => statistics
            .OrderBy(pair => pair.Key, StringComparer.Ordinal)
            .Select(pair => new IrOptimizationStat(pair.Key, pair.Value));

        private void Record(string name, int count = 1)
        {
            if (count != 0)
                statistics[name] = statistics.GetValueOrDefault(name) + count;
        }

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

        private void CreateLoopPreheaders(RoutineIr routine)
        {
            if (createPreheader == null)
                return;
            foreach (var loop in CfgAnalysis.Create(routine).Loops.Where(loop => loop.Preheader == null)
                .OrderBy(loop => loop.Blocks.Count).ToArray())
            {
                var external = loop.Header.Predecessors.Where(predecessor => !loop.Blocks.Contains(predecessor))
                    .ToArray();
                if (external.Length == 0 || external.Any(predecessor => !CanRedirectEdge(predecessor, loop.Header)))
                {
                    Record("Preheaders rejected: unredirectable edge");
                    continue;
                }

                var preheader = createPreheader(loop.Header);
                foreach (var predecessor in external)
                    RedirectEdge(predecessor, loop.Header, preheader);
                foreach (var instruction in loop.Header.Instructions.Where(instruction =>
                    instruction.Opcode == IrOpcode.Phi && instruction.Payload is IrPhi))
                {
                    var phi = (IrPhi)instruction.Payload!;
                    var incoming = external.Select(predecessor => phi.Incoming[predecessor]).ToArray();
                    IrValue merged;
                    if (incoming.Skip(1).All(value => StateValuesEquivalent(value, incoming[0])))
                        merged = incoming[0];
                    else
                    {
                        var preheaderPhi = new IrInstruction(IrOpcode.Phi, routine.CreateValue(), [],
                            payload: new IrPhi(phi.Variable));
                        preheaderPhi.Result!.PhysicalHome = phi.Variable;
                        var payload = (IrPhi)preheaderPhi.Payload!;
                        foreach (var predecessor in external)
                            payload.Incoming[predecessor] = phi.Incoming[predecessor];
                        preheader.Instructions.Insert(0, preheaderPhi);
                        merged = preheaderPhi.Result;
                    }
                    foreach (var predecessor in external)
                        phi.Incoming.Remove(predecessor);
                    phi.Incoming[preheader] = merged;
                }
                routine.RebuildPredecessors();
                Record("Preheaders created");
            }
        }

        private static bool CanRedirectEdge(IrBlock predecessor, IrBlock target) => predecessor.Terminator switch
        {
            IrTerminator.Jump jump when ReferenceEquals(jump.Target, target) =>
                jump.Instruction == null || jump.EmitJump != null,
            IrTerminator.Branch branch when ReferenceEquals(branch.WhenTrue, target) ||
                ReferenceEquals(branch.WhenFalse, target) => branch.Instruction == null ||
                !ReferenceEquals(branch.ExplicitTarget, target),
            _ => false,
        };

        private static void RedirectEdge(IrBlock predecessor, IrBlock oldTarget, IrBlock newTarget)
        {
            predecessor.Terminator = predecessor.Terminator switch
            {
                IrTerminator.Jump jump when ReferenceEquals(jump.Target, oldTarget) =>
                    RedirectJump(jump, newTarget),
                IrTerminator.Branch branch => branch with
                {
                    WhenTrue = ReferenceEquals(branch.WhenTrue, oldTarget) ? newTarget : branch.WhenTrue,
                    WhenFalse = ReferenceEquals(branch.WhenFalse, oldTarget) ? newTarget : branch.WhenFalse,
                },
                _ => throw new InvalidOperationException($"Block {predecessor} has no edge to {oldTarget}."),
            };
        }

        private static IrTerminator.Jump RedirectJump(IrTerminator.Jump jump, IrBlock newTarget)
        {
            if (jump.Instruction?.Payload is IrLoweringOperation lowering && jump.EmitJump != null)
                jump.Instruction.Payload = new IrLoweringOperation(_ => jump.EmitJump(newTarget),
                    lowering.ResultHome, lowering.IsStackResult, lowering.EmitTo);
            return jump with { Target = newTarget };
        }

        private void SimplifyInductionVariables(RoutineIr routine)
        {
            var cfg = CfgAnalysis.Create(routine);
            var definitions = routine.Blocks.SelectMany(block => block.Instructions)
                .Where(instruction => instruction.Result != null).ToDictionary(instruction => instruction.Result!);
            var replacements = new Dictionary<IrValue, IrValue>();
            foreach (var loop in cfg.Loops.Where(loop => loop.Preheader != null && loop.Latches.Count == 1))
            {
                var latch = loop.Latches.Single();
                var inductions = new List<(IrInstruction Phi, IrInstruction Update, IrValue Initial, int Step)>();
                foreach (var phi in loop.Header.Instructions.Where(instruction =>
                    instruction.Opcode == IrOpcode.Phi && instruction.Payload is IrPhi))
                {
                    var payload = (IrPhi)phi.Payload!;
                    if (!payload.Incoming.TryGetValue(loop.Preheader!, out var initial) ||
                        !payload.Incoming.TryGetValue(latch, out var next) || !definitions.TryGetValue(next, out var update) ||
                        !TryGetInductionStep(phi.Result!, update, out var step) ||
                        update.Payload is IrLoweringOperation { RequiredHome: true })
                        continue;
                    inductions.Add((phi, update, initial, step));
                }

                foreach (var group in inductions.GroupBy(item => item.Step))
                {
                    var items = group.ToArray();
                    for (var index = 1; index < items.Length; index++)
                    {
                        var redundant = items[index];
                        var canonical = items.Take(index).FirstOrDefault(candidate =>
                            StateValuesEquivalent(candidate.Initial, redundant.Initial));
                        if (canonical.Phi == null)
                            continue;
                        replacements[redundant.Phi.Result!] = canonical.Phi.Result!;
                        replacements[redundant.Update.Result!] = canonical.Update.Result!;
                        loop.Header.Instructions.Remove(redundant.Phi);
                        foreach (var block in loop.Blocks)
                        {
                            for (var i = block.Instructions.Count - 1; i >= 0; i--)
                            {
                                if (ReferenceEquals(block.Instructions[i], redundant.Update) ||
                                    block.Instructions[i].Payload is IrLoweringOperation
                                    {
                                        IsMaterialization: true,
                                        ResultHome: not null,
                                    } materialization && ReferenceEquals(materialization.ResultHome,
                                        ((IrPhi)redundant.Phi.Payload!).Variable))
                                    block.Instructions.RemoveAt(i);
                            }
                        }
                        Record("Redundant induction variables removed");
                    }
                }
            }
            ReplaceValues(routine, replacements);
            routine.RebuildPredecessors();
        }

        private int NormalizeStep(int value) => Normalize(value);

        private static bool StateValuesEquivalent(IrValue left, IrValue right) => ReferenceEquals(left, right) ||
            left.Constant is int leftConstant && right.Constant == leftConstant;

        private bool TryGetInductionStep(IrValue phi, IrInstruction update, out int step)
        {
            step = 0;
            if (update.Opcode == IrOpcode.Add && update.Operands.Count == 2)
            {
                if (ReferenceEquals(update.Operands[0], phi) && update.Operands[1].Constant is int right)
                    step = NormalizeStep(right);
                else if (ReferenceEquals(update.Operands[1], phi) && update.Operands[0].Constant is int left)
                    step = NormalizeStep(left);
                else
                    return false;
                return true;
            }
            if (update.Opcode == IrOpcode.Subtract && update.Operands.Count == 2 &&
                ReferenceEquals(update.Operands[0], phi) && update.Operands[1].Constant is int subtrahend)
            {
                step = NormalizeStep(-subtrahend);
                return true;
            }
            return false;
        }

        private void LoopInvariantCodeMotion(RoutineIr routine)
        {
            var cfg = CfgAnalysis.Create(routine);
            Record("Loops detected", cfg.Loops.Count);
            if (cfg.Loops.Count == 0)
                return;

            var stateDependencies = FindStateDependencies(routine.Blocks);
            var definitions = routine.Blocks.SelectMany(block => block.Instructions.Select(instruction =>
                (instruction, block))).Where(pair => pair.instruction.Result != null)
                .ToDictionary(pair => pair.instruction.Result!, pair => pair);

            foreach (var loop in cfg.Loops.OrderBy(loop => loop.Blocks.Count))
            {
                if (loop.Preheader == null)
                {
                    Record("LICM rejected: no preheader");
                    continue;
                }

                var writtenRegions = loop.Blocks.SelectMany(block => block.Instructions)
                    .Aggregate(IrMemoryRegion.None, (regions, instruction) => regions | GetWrittenRegions(instruction));
                var clobberedHomes = loop.Blocks.SelectMany(block => block.Instructions)
                    .Select(instruction => (instruction.Payload as IrLoweringOperation)?.ResultHome)
                    .OfType<IVariable>().GroupBy(home => home).Where(group => group.Count() > 1)
                    .Select(group => group.Key).ToHashSet();
                var invariantValues = definitions.Where(pair => !loop.Blocks.Contains(pair.Value.block))
                    .Select(pair => pair.Key).ToHashSet();
                foreach (var value in loop.Blocks.SelectMany(block => block.Instructions)
                    .SelectMany(instruction => instruction.Operands).Where(value => !definitions.ContainsKey(value)))
                    invariantValues.Add(value);

                var changed = true;
                while (changed)
                {
                    changed = false;
                    foreach (var block in loop.Blocks.OrderBy(block => routine.Blocks.IndexOf(block)).ToArray())
                    {
                        for (var index = 0; index < block.Instructions.Count; index++)
                        {
                            var instruction = block.Instructions[index];
                            if (instruction.Result == null || invariantValues.Contains(instruction.Result) ||
                                instruction.Opcode == IrOpcode.Phi || !IsValueNumberable(instruction) ||
                                instruction.Operands.Any(operand => !invariantValues.Contains(operand)))
                                continue;

                            Record("LICM candidates");
                            if (instruction.Payload is not IrLoweringOperation
                                {
                                    ResultHome: IVariable home,
                                    IsStackResult: false,
                                    RequiredHome: false,
                                } || clobberedHomes.Contains(home))
                            {
                                Record("LICM rejected: physical availability");
                                continue;
                            }

                            var dependencies = stateDependencies.GetValueOrDefault(instruction.Result);
                            if ((dependencies & writtenRegions) != 0)
                            {
                                Record("LICM rejected: memory changed");
                                continue;
                            }
                            if (!IsSafeToSpeculate(instruction) &&
                                (!loop.ExitSources.All(exit => cfg.Dominates(block, exit)) ||
                                 !loop.Latches.All(latch => cfg.Dominates(block, latch))))
                            {
                                Record("LICM rejected: conditional execution");
                                continue;
                            }

                            block.Instructions.RemoveAt(index--);
                            loop.Preheader.Instructions.Add(instruction);
                            invariantValues.Add(instruction.Result);
                            Record("LICM hoisted instructions");
                            changed = true;
                        }
                    }
                }
            }
        }

        private static bool IsSafeToSpeculate(IrInstruction instruction) => instruction.IsPure && instruction.Opcode
            is not IrOpcode.Divide and not IrOpcode.Modulo and not IrOpcode.ShiftLeft and not IrOpcode.ShiftRight
            and not IrOpcode.ArithmeticShift and not IrOpcode.LogicalShift;

        private static IrMemoryRegion GetWrittenRegions(IrInstruction instruction) => instruction.Effect switch
        {
            IrEffect.Call => instruction.CallSummary?.GetWrittenRegions() ?? IrMemoryRegion.All,
            IrEffect.Opaque => IrMemoryRegion.All,
            IrEffect.WriteMemory when instruction.WriteRegions == IrMemoryRegion.None => IrMemoryRegion.All,
            IrEffect.WriteMemory => instruction.WriteRegions,
            _ => IrMemoryRegion.None,
        };

        private sealed class CfgAnalysis
        {
            private readonly IReadOnlyDictionary<IrBlock, HashSet<IrBlock>> dominators;

            private CfgAnalysis(IReadOnlyDictionary<IrBlock, HashSet<IrBlock>> dominators,
                IReadOnlyList<NaturalLoop> loops)
            {
                this.dominators = dominators;
                Loops = loops;
            }

            public IReadOnlyList<NaturalLoop> Loops { get; }

            public bool Dominates(IrBlock dominator, IrBlock block) => dominators[block].Contains(dominator);

            public static CfgAnalysis Create(RoutineIr routine)
            {
                routine.RebuildPredecessors();
                var blocks = routine.Blocks.ToArray();
                var all = blocks.ToHashSet();
                var dominators = blocks.ToDictionary(block => block, block => ReferenceEquals(block, routine.Entry)
                    ? new HashSet<IrBlock> { block }
                    : new HashSet<IrBlock>(all));
                var changed = true;
                while (changed)
                {
                    changed = false;
                    foreach (var block in blocks.Where(block => !ReferenceEquals(block, routine.Entry)))
                    {
                        var next = block.Predecessors.Count == 0
                            ? []
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

                var loops = new List<NaturalLoop>();
                foreach (var headerGroup in blocks.SelectMany(tail => RoutineIr.GetSuccessors(tail)
                    .Where(header => dominators[tail].Contains(header)).Select(header => (header, tail)))
                    .GroupBy(edge => edge.header))
                {
                    var header = headerGroup.Key;
                    var latches = headerGroup.Select(edge => edge.tail).ToHashSet();
                    var loopBlocks = new HashSet<IrBlock> { header };
                    var pending = new Stack<IrBlock>(latches);
                    while (pending.TryPop(out var block))
                    {
                        if (!loopBlocks.Add(block))
                            continue;
                        foreach (var predecessor in block.Predecessors)
                            pending.Push(predecessor);
                    }
                    var external = header.Predecessors.Where(predecessor => !loopBlocks.Contains(predecessor))
                        .ToArray();
                    var preheader = external.Length == 1 &&
                        RoutineIr.GetSuccessors(external[0]).Count() == 1 ? external[0] : null;
                    var exits = loopBlocks.Where(block => RoutineIr.GetSuccessors(block)
                        .Any(successor => !loopBlocks.Contains(successor))).ToHashSet();
                    loops.Add(new NaturalLoop(header, loopBlocks, latches, exits, preheader));
                }
                return new CfgAnalysis(dominators, loops);
            }
        }

        private sealed record NaturalLoop(IrBlock Header, HashSet<IrBlock> Blocks, HashSet<IrBlock> Latches,
            HashSet<IrBlock> ExitSources, IrBlock? Preheader);

        private sealed class MemoryVersionAnalysis
        {
            private static readonly IrMemoryRegion[] Regions =
            [
                IrMemoryRegion.Globals,
                IrMemoryRegion.Tables,
                IrMemoryRegion.Properties,
                IrMemoryRegion.ObjectTree,
                IrMemoryRegion.Attributes,
            ];

            private readonly Dictionary<IrInstruction, Dictionary<IrMemoryRegion, int>> before;

            private MemoryVersionAnalysis(Dictionary<IrInstruction, Dictionary<IrMemoryRegion, int>> before) =>
                this.before = before;

            public string GetKey(IrInstruction instruction, IrMemoryRegion dependencies) => string.Join(",",
                Regions.Where(region => (dependencies & region) != 0).Select(region => before[instruction][region]));

            public static MemoryVersionAnalysis Create(RoutineIr routine)
            {
                routine.RebuildPredecessors();
                var nextVersion = 1;
                var initial = Regions.ToDictionary(region => region, _ => nextVersion++);
                var writeVersions = routine.Blocks.SelectMany(block => block.Instructions)
                    .SelectMany(instruction => Regions.Where(region => (GetWrittenRegions(instruction) & region) != 0)
                        .Select(region => (instruction, region)))
                    .ToDictionary(pair => pair, _ => nextVersion++);
                var mergeVersions = routine.Blocks.SelectMany(block => Regions.Select(region => (block, region)))
                    .ToDictionary(pair => pair, _ => nextVersion++);
                var incoming = routine.Blocks.ToDictionary(block => block,
                    _ => new Dictionary<IrMemoryRegion, int>(initial));
                var outgoing = routine.Blocks.ToDictionary(block => block,
                    _ => new Dictionary<IrMemoryRegion, int>(initial));

                var changed = true;
                while (changed)
                {
                    changed = false;
                    foreach (var block in routine.Blocks)
                    {
                        var nextIn = new Dictionary<IrMemoryRegion, int>();
                        foreach (var region in Regions)
                        {
                            if (ReferenceEquals(block, routine.Entry) || block.Predecessors.Count == 0)
                                nextIn[region] = initial[region];
                            else
                            {
                                var versions = block.Predecessors.Select(predecessor => outgoing[predecessor][region])
                                    .Distinct().ToArray();
                                nextIn[region] = versions.Length == 1 ? versions[0] : mergeVersions[(block, region)];
                            }
                        }
                        var nextOut = new Dictionary<IrMemoryRegion, int>(nextIn);
                        foreach (var instruction in block.Instructions)
                            foreach (var region in Regions.Where(region =>
                                (GetWrittenRegions(instruction) & region) != 0))
                                nextOut[region] = writeVersions[(instruction, region)];
                        if (!nextIn.SequenceEqual(incoming[block]))
                        {
                            incoming[block] = nextIn;
                            changed = true;
                        }
                        if (!nextOut.SequenceEqual(outgoing[block]))
                        {
                            outgoing[block] = nextOut;
                            changed = true;
                        }
                    }
                }

                var before = new Dictionary<IrInstruction, Dictionary<IrMemoryRegion, int>>();
                foreach (var block in routine.Blocks)
                {
                    var current = new Dictionary<IrMemoryRegion, int>(incoming[block]);
                    foreach (var instruction in block.Instructions)
                    {
                        before[instruction] = new Dictionary<IrMemoryRegion, int>(current);
                        foreach (var region in Regions.Where(region =>
                            (GetWrittenRegions(instruction) & region) != 0))
                            current[region] = writeVersions[(instruction, region)];
                    }
                }
                return new MemoryVersionAnalysis(before);
            }
        }

        private void GlobalValueNumbering(RoutineIr routine)
        {
            routine.RebuildPredecessors();
            var blocks = routine.Blocks.ToArray();
            var stateDependencies = FindStateDependencies(blocks);
            var memoryVersions = MemoryVersionAnalysis.Create(routine);
            var liveHomesAfter = FindLiveHomesAfter(blocks);
            var instructionBlocks = blocks.SelectMany(block => block.Instructions.Select(instruction =>
                (instruction, block))).ToDictionary(pair => pair.instruction, pair => pair.block);
            var instructionIndices = blocks.SelectMany(block => block.Instructions.Select((instruction, index) =>
                (instruction, index))).ToDictionary(pair => pair.instruction, pair => pair.index);
            var lastUseIndices = new Dictionary<IrValue, int>();
            foreach (var block in blocks)
            {
                for (var i = 0; i < block.Instructions.Count; i++)
                {
                    foreach (var operand in block.Instructions[i].Operands)
                        lastUseIndices[operand] = Math.Max(lastUseIndices.GetValueOrDefault(operand, -1), i);
                }
                IEnumerable<IrValue> terminatorOperands = block.Terminator switch
                {
                    IrTerminator.Branch branch => [branch.Condition],
                    IrTerminator.Return { Value: not null } ret => [ret.Value],
                    _ => [],
                };
                foreach (var operand in terminatorOperands)
                    lastUseIndices[operand] = block.Instructions.Count;
            }
            var homeReservations = new Dictionary<(IrBlock Block, IVariable Home), int>();
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

            var visitedDominatorBlocks = new HashSet<IrBlock>();
            var pendingDominatorBlocks = new Stack<(IrBlock Block,
                Dictionary<(IrOpcode Opcode, string Operands), (IrValue Value, IrInstruction Instruction)>
                    Available)>();
            void Visit(IrBlock block,
                Dictionary<(IrOpcode Opcode, string Operands), (IrValue Value, IrInstruction Instruction)> available)
            {
                if (!visitedDominatorBlocks.Add(block))
                    return;
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
                    var isStackResult = instruction.Payload is IrLoweringOperation { IsStackResult: true };
                    (IrOpcode Opcode, string Operands)? currentKey = null;
                    if (instruction.Result != null && IsValueNumberable(instruction))
                    {
                        Record("GVN candidates");
                        var currentIds = instruction.Operands.Select(OperandKey).ToArray();
                        if (IsCommutative(instruction.Opcode))
                            Array.Sort(currentIds);
                        var dependencies = stateDependencies.GetValueOrDefault(instruction.Result);
                        currentKey = (instruction.Opcode,
                            $"{string.Join(",", currentIds)}|{memoryVersions.GetKey(instruction, dependencies)}");
                    }
                    if (resultHome != null && !isStackResult)
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
                        CanReusePhysicalHome(prior.Instruction, instruction))
                    {
                        Record("GVN eliminated expressions");
                        replacements[instruction.Result] = Resolve(prior.Value);
                        ReserveHomeThroughUse(prior.Instruction, instruction.Result);
                    }
                    else if (available.TryGetValue(key, out prior) &&
                        TryPromoteStackValue(prior.Instruction, instruction,
                            FindReusableTemporary(prior.Instruction, instruction)))
                    {
                        Record("GVN promoted stack results");
                        var promotedHome = (prior.Instruction.Payload as IrLoweringOperation)?.ResultHome;
                        if (promotedHome != null)
                        {
                            foreach (var staleKey in available
                                .Where(pair => !ReferenceEquals(pair.Value.Instruction, prior.Instruction) &&
                                    ReferenceEquals((pair.Value.Instruction.Payload as IrLoweringOperation)?.ResultHome,
                                        promotedHome))
                                .Select(pair => pair.Key).ToArray())
                                available.Remove(staleKey);
                        }
                        if (instruction.Payload is IrLoweringOperation { RequiredHome: true })
                        {
                            if (TryRewriteAsCopy(prior.Value, prior.Instruction, instruction))
                            {
                                available[key] = (instruction.Result, instruction);
                                ReserveHomeThroughUse(prior.Instruction, instruction.Result);
                            }
                        }
                        else
                        {
                            replacements[instruction.Result] = Resolve(prior.Value);
                            ReserveHomeThroughUse(prior.Instruction, instruction.Result);
                        }
                    }
                    else if (available.TryGetValue(key, out prior) &&
                        TryRewriteAsCopy(prior.Value, prior.Instruction, instruction))
                    {
                        Record("GVN rewrote expressions as copies");
                        available[key] = (instruction.Result, instruction);
                    }
                    else
                    {
                        if (available.ContainsKey(key))
                            Record("GVN rejected: physical availability");
                        available[key] = (instruction.Result, instruction);
                    }
                }
                block.Terminator = block.Terminator switch
                {
                    IrTerminator.Branch branch => branch with { Condition = Resolve(branch.Condition) },
                    IrTerminator.Return { Value: not null } ret => ret with { Value = Resolve(ret.Value) },
                    _ => block.Terminator,
                };
                foreach (var child in children[block].AsEnumerable().Reverse())
                    pendingDominatorBlocks.Push((child, available));
            }

            pendingDominatorBlocks.Push((routine.Entry, []));
            while (pendingDominatorBlocks.Count > 0)
            {
                var (block, available) = pendingDominatorBlocks.Pop();
                Visit(block, available);
            }
            ReplaceValues(routine, replacements);

            IVariable? FindReusableTemporary(IrInstruction definition, IrInstruction use)
            {
                if (!instructionBlocks.TryGetValue(definition, out var definitionBlock) ||
                    !instructionBlocks.TryGetValue(use, out var useBlock) ||
                    !ReferenceEquals(definitionBlock, useBlock))
                    return null;
                var live = liveHomesAfter.GetValueOrDefault(definition);
                var definitionIndex = instructionIndices[definition];
                return reusableTemporaries().FirstOrDefault(candidate =>
                    (live == null || !live.Contains(candidate)) &&
                    homeReservations.GetValueOrDefault((definitionBlock, candidate), -1) < definitionIndex);
            }

            void ReserveHomeThroughUse(IrInstruction definition, IrValue? replacedValue = null)
            {
                if (definition.Payload is not IrLoweringOperation { ResultHome: not null } lowering ||
                    !instructionBlocks.TryGetValue(definition, out var block))
                    return;
                var lastUse = lastUseIndices.GetValueOrDefault(definition.Result!, instructionIndices[definition]);
                if (replacedValue != null)
                    lastUse = Math.Max(lastUse, lastUseIndices.GetValueOrDefault(replacedValue, lastUse));
                var reservation = (block, lowering.ResultHome);
                homeReservations[reservation] = Math.Max(homeReservations.GetValueOrDefault(reservation, -1),
                    lastUse);
            }
        }

        private bool TryRewriteAsCopy(IrValue priorValue, IrInstruction priorInstruction, IrInstruction instruction)
        {
            if (emitCopy == null ||
                priorInstruction.Payload is IrLoweringOperation { IsStackResult: true } ||
                instruction.Payload is not IrLoweringOperation
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
            ((IrLoweringOperation)instruction.Payload).IsMaterialization = true;
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

        private bool TryPromoteStackValue(IrInstruction prior, IrInstruction current, IVariable? reusableTemporary)
        {
            if (prior.Payload is not IrLoweringOperation
                {
                    IsStackResult: true,
                    StackEscapes: false,
                    EmitTo: not null,
                } priorLowering ||
                current.Payload is not IrLoweringOperation currentLowering ||
                currentLowering.IsStackResult && currentLowering.StackEscapes)
                return false;

            var temporary = acquireTemporary() ?? reusableTemporary;
            if (temporary == null)
                return false;

            priorLowering.ResultHome = temporary;
            priorLowering.IsStackResult = false;
            prior.Result!.PhysicalHome = temporary;
            return true;
        }

        private static Dictionary<IrInstruction, HashSet<IVariable>> FindLiveHomesAfter(IEnumerable<IrBlock> blocks)
        {
            var blockArray = blocks.ToArray();
            var liveIn = blockArray.ToDictionary(block => block, _ => new HashSet<IVariable>());
            var liveOut = blockArray.ToDictionary(block => block, _ => new HashSet<IVariable>());
            var changed = true;
            while (changed)
            {
                changed = false;
                foreach (var block in blockArray.Reverse())
                {
                    var nextOut = RoutineIr.GetSuccessors(block)
                        .SelectMany(successor => liveIn[successor]).ToHashSet();
                    var nextIn = new HashSet<IVariable>(nextOut);
                    for (var i = block.Instructions.Count - 1; i >= 0; i--)
                    {
                        var instruction = block.Instructions[i];
                        if (instruction.Payload is IrLoweringOperation
                            {
                                ResultHome: not null,
                                IsStackResult: false,
                            } lowering)
                            nextIn.Remove(lowering.ResultHome);
                        foreach (var home in instruction.Operands.Select(operand => operand.PhysicalHome)
                            .OfType<IVariable>())
                            nextIn.Add(home);
                    }
                    if (!nextOut.SetEquals(liveOut[block]))
                    {
                        liveOut[block] = nextOut;
                        changed = true;
                    }
                    if (!nextIn.SetEquals(liveIn[block]))
                    {
                        liveIn[block] = nextIn;
                        changed = true;
                    }
                }
            }

            var result = new Dictionary<IrInstruction, HashSet<IVariable>>();
            foreach (var block in blockArray)
            {
                var live = new HashSet<IVariable>(liveOut[block]);
                for (var i = block.Instructions.Count - 1; i >= 0; i--)
                {
                    var instruction = block.Instructions[i];
                    result[instruction] = new HashSet<IVariable>(live);
                    if (instruction.Payload is IrLoweringOperation
                        {
                            ResultHome: not null,
                            IsStackResult: false,
                        } lowering)
                        live.Remove(lowering.ResultHome);
                    foreach (var home in instruction.Operands.Select(operand => operand.PhysicalHome)
                        .OfType<IVariable>())
                        live.Add(home);
                }
            }
            return result;
        }

        private static bool CanReusePhysicalHome(IrInstruction prior, IrInstruction current)
        {
            if (prior.Payload is not IrLoweringOperation priorLowering ||
                current.Payload is not IrLoweringOperation currentLowering)
                return true;
            if (priorLowering.IsStackResult)
                return false;
            if (currentLowering.IsStackResult)
                return !currentLowering.StackEscapes && priorLowering.ResultHome != null;
            if (IsMemoryRead(prior.Opcode) && prior.Opcode == current.Opcode)
                return priorLowering.ResultHome != null && currentLowering.ResultHome != null;
            return priorLowering.ResultHome != null &&
                ReferenceEquals(priorLowering.ResultHome, currentLowering.ResultHome);
        }

        private static bool IsValueNumberable(IrInstruction instruction) =>
            (instruction.Payload is not IrLoweringOperation { RequiredHome: true } ||
             IsMemoryRead(instruction.Opcode)) &&
            (instruction.IsPure || instruction.Effect == IrEffect.ReadMemory) && instruction.Opcode is
            IrOpcode.Add or IrOpcode.Subtract or IrOpcode.Multiply or IrOpcode.Divide or IrOpcode.Modulo or
            IrOpcode.BitwiseAnd or IrOpcode.BitwiseOr or IrOpcode.BitwiseNot or IrOpcode.Negate or
            IrOpcode.ShiftLeft or IrOpcode.ShiftRight or IrOpcode.Equal or IrOpcode.LessThan or
            IrOpcode.ArithmeticShift or IrOpcode.LogicalShift or
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
                        !instruction.Operands[0].MutableExternal && CanReplaceRequiredHome(instruction))
                    {
                        Record("Copy propagation");
                        replacements[instruction.Result] = instruction.Operands[0];
                        PreserveRequiredHome(instruction, instruction.Operands[0]);
                        continue;
                    }

                    if (CanReplaceRequiredHome(instruction) && TrySimplifyIdentity(instruction, out var replacement))
                    {
                        Record("Algebraic identities");
                        replacements[instruction.Result] = replacement;
                        PreserveRequiredHome(instruction, replacement);
                        continue;
                    }

                    if (CanReplaceRequiredHome(instruction) && TryFold(instruction, out var value))
                    {
                        Record("Constant folds");
                        var constant = routine.CreateConstant(value);
                        replacements[instruction.Result] = constant;
                        PreserveRequiredHome(instruction, constant);
                    }
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

        private void PreserveRequiredHome(IrInstruction instruction, IrValue replacement)
        {
            if (emitCopy == null || instruction.Payload is not IrLoweringOperation
                {
                    RequiredHome: true,
                    ResultHome: not null,
                } lowering)
                return;

            var resultHome = lowering.ResultHome;
            instruction.Opcode = IrOpcode.TargetOperation;
            instruction.Operands.Clear();
            instruction.Operands.Add(replacement);
            instruction.Payload = new IrLoweringOperation(
                operands => emitCopy(resultHome, operands[0]), resultHome)
            {
                IsMaterialization = true,
                RequiredHome = true,
            };
        }

        private bool CanReplaceRequiredHome(IrInstruction instruction) =>
            instruction.Payload is not IrLoweringOperation { RequiredHome: true } || emitCopy != null;

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
                IrOpcode.ArithmeticShift when right.Constant == 0 => left,
                IrOpcode.LogicalShift when right.Constant == 0 => left,
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
                    IrOpcode.ArithmeticShift when operands.Count == 2 && TryGetShift(operands[1], out var shift) =>
                        shift >= 0 ? operands[0] << shift : operands[0] >> -shift,
                    IrOpcode.LogicalShift when operands.Count == 2 &&
                        TryGetShift(operands[1], out var logicalShift) =>
                        LogicalShift(operands[0], logicalShift),
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

        private bool TryGetShift(int count, out int shift)
        {
            shift = count;
            return count != int.MinValue && IsValidShift(Math.Abs(count));
        }

        private int LogicalShift(int value, int shift) => numericSemantics switch
        {
            IrNumericSemantics.ZMachine16 => shift >= 0
                ? (ushort)value << shift
                : (ushort)value >> -shift,
            IrNumericSemantics.Glulx32 => shift >= 0
                ? unchecked((int)((uint)value << shift))
                : unchecked((int)((uint)value >> -shift)),
            _ => throw new InvalidOperationException($"Unknown numeric semantics: {numericSemantics}"),
        };

        private bool SimplifyControlFlow(RoutineIr routine)
        {
            var changed = false;
            foreach (var block in routine.Blocks)
            {
                if (block.Terminator is IrTerminator.Branch { Condition.Constant: int value } branch)
                {
                    var target = value != 0 ? branch.WhenTrue : branch.WhenFalse;
                    if (branch.Instruction != null && branch.EmitJump != null)
                    {
                        if (ReferenceEquals(target, branch.ExplicitTarget))
                        {
                            branch.Instruction.Opcode = IrOpcode.TargetOperation;
                            branch.Instruction.Operands.Clear();
                            branch.Instruction.Payload = new IrLoweringOperation(_ => branch.EmitJump(target));
                        }
                        else
                        {
                            block.Instructions.Remove(branch.Instruction);
                        }
                    }
                    block.Terminator = new IrTerminator.Jump(target, branch.EmitJump,
                        ReferenceEquals(target, branch.ExplicitTarget) ? branch.Instruction : null);
                    Record("Folded branches");
                    changed = true;
                }
                else if (block.Terminator is IrTerminator.Branch same && ReferenceEquals(same.WhenTrue, same.WhenFalse))
                {
                    if (same.Instruction != null)
                        block.Instructions.Remove(same.Instruction);
                    block.Terminator = new IrTerminator.Jump(same.WhenTrue);
                    Record("Folded branches");
                    changed = true;
                }
            }
            return changed;
        }

        private bool RemoveDeadInstructions(RoutineIr routine)
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
                        Record("Dead instructions removed");
                        changed = true;
                    }
                }
            }
            return changed;
        }

        private static bool IsRemovable(IrInstruction instruction) =>
            instruction.Payload is not IrLoweringOperation { RequiredHome: true } &&
            (instruction.IsPure || instruction.Effect == IrEffect.ReadMemory && IsMemoryRead(instruction.Opcode));

        private bool RemoveUnreachableBlocks(RoutineIr routine)
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
                    Record("Unreachable blocks removed");
                    removed = true;
                }
            }
            return removed;
        }
    }
}
