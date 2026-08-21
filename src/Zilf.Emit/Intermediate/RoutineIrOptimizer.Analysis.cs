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
            private readonly Dictionary<IrInstruction, Dictionary<IrMemoryIdentity, int>> exactBefore;

            private MemoryVersionAnalysis(Dictionary<IrInstruction, Dictionary<IrMemoryRegion, int>> before,
                Dictionary<IrInstruction, Dictionary<IrMemoryIdentity, int>> exactBefore)
            {
                this.before = before;
                this.exactBefore = exactBefore;
            }

            public int ExactMergeCount { get; private init; }

            public string GetKey(IrInstruction instruction, IrMemoryRegion dependencies,
                IEnumerable<IrMemoryIdentity> exactDependencies)
            {
                var regionKey = string.Join(",", Regions.Where(region => (dependencies & region) != 0)
                    .Select(region => before[instruction][region]));
                var exactKey = string.Join(",", exactDependencies.OrderBy(identity => identity.GetHashCode())
                    .Select(identity => exactBefore[instruction].GetValueOrDefault(identity)));
                return $"{regionKey}|{exactKey}";
            }

            public static MemoryVersionAnalysis Create(RoutineIr routine)
            {
                routine.RebuildPredecessors();
                var nextVersion = 1;
                var initial = Regions.ToDictionary(region => region, _ => nextVersion++);
                var instructions = routine.Blocks.SelectMany(block => block.Instructions).ToArray();
                var exactIdentities = instructions.Select(instruction => instruction.ReadIdentity)
                    .OfType<IrMemoryIdentity>()
                    .Concat(instructions.SelectMany(instruction => instruction.Operands.Append(instruction.Result))
                        .Where(value => value?.MemoryIdentity != null).Select(value => value!.MemoryIdentity!))
                    .Distinct().ToArray();
                var unknownWritesByInstruction = instructions.ToDictionary(instruction => instruction,
                    GetUnknownWrittenRegions);
                var writtenIdentitiesByInstruction = instructions.ToDictionary(instruction => instruction,
                    instruction => GetWrittenIdentities(instruction).ToArray());
                var affectedIdentitiesByInstruction = instructions.ToDictionary(instruction => instruction,
                    instruction => exactIdentities.Where(identity => writtenIdentitiesByInstruction[instruction]
                        .Any(written => identity.MayAlias(written))).ToArray());
                var exactInitial = exactIdentities.ToDictionary(identity => identity, _ => nextVersion++);
                var writeVersions = routine.Blocks.SelectMany(block => block.Instructions)
                    .SelectMany(instruction => Regions.Where(region =>
                            (unknownWritesByInstruction[instruction] & region) != 0)
                        .Select(region => (instruction, region)))
                    .ToDictionary(pair => pair, _ => nextVersion++);
                var exactWriteVersions = routine.Blocks.SelectMany(block => block.Instructions)
                    .SelectMany(instruction => affectedIdentitiesByInstruction[instruction]
                        .Select(identity => (instruction, identity)))
                    .ToDictionary(pair => pair, _ => nextVersion++);
                var mergeVersions = routine.Blocks.SelectMany(block => Regions.Select(region => (block, region)))
                    .ToDictionary(pair => pair, _ => nextVersion++);
                var exactMergeVersions = routine.Blocks
                    .SelectMany(block => exactIdentities.Select(identity => (block, identity)))
                    .ToDictionary(pair => pair, _ => nextVersion++);
                var incoming = routine.Blocks.ToDictionary(block => block,
                    _ => new Dictionary<IrMemoryRegion, int>(initial));
                var outgoing = routine.Blocks.ToDictionary(block => block,
                    _ => new Dictionary<IrMemoryRegion, int>(initial));
                var exactIncoming = routine.Blocks.ToDictionary(block => block,
                    _ => new Dictionary<IrMemoryIdentity, int>(exactInitial));
                var exactOutgoing = routine.Blocks.ToDictionary(block => block,
                    _ => new Dictionary<IrMemoryIdentity, int>(exactInitial));

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
                        var nextExactIn = new Dictionary<IrMemoryIdentity, int>();
                        foreach (var identity in exactIdentities)
                        {
                            if (ReferenceEquals(block, routine.Entry) || block.Predecessors.Count == 0)
                                nextExactIn[identity] = exactInitial[identity];
                            else
                            {
                                var versions = block.Predecessors
                                    .Select(predecessor => exactOutgoing[predecessor][identity]).Distinct().ToArray();
                                nextExactIn[identity] = versions.Length == 1
                                    ? versions[0]
                                    : exactMergeVersions[(block, identity)];
                            }
                        }
                        var nextOut = new Dictionary<IrMemoryRegion, int>(nextIn);
                        var nextExactOut = new Dictionary<IrMemoryIdentity, int>(nextExactIn);
                        foreach (var instruction in block.Instructions)
                        {
                            var unknownWrites = unknownWritesByInstruction[instruction];
                            foreach (var region in Regions.Where(region => (unknownWrites & region) != 0))
                            {
                                nextOut[region] = writeVersions[(instruction, region)];
                                foreach (var identity in exactIdentities.Where(identity => identity.Region == region))
                                    nextExactOut[identity] = writeVersions[(instruction, region)];
                            }
                            foreach (var identity in affectedIdentitiesByInstruction[instruction])
                                nextExactOut[identity] = exactWriteVersions[(instruction, identity)];
                        }
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
                        if (!nextExactIn.SequenceEqual(exactIncoming[block]))
                        {
                            exactIncoming[block] = nextExactIn;
                            changed = true;
                        }
                        if (!nextExactOut.SequenceEqual(exactOutgoing[block]))
                        {
                            exactOutgoing[block] = nextExactOut;
                            changed = true;
                        }
                    }
                }

                var before = new Dictionary<IrInstruction, Dictionary<IrMemoryRegion, int>>();
                var exactBefore = new Dictionary<IrInstruction, Dictionary<IrMemoryIdentity, int>>();
                foreach (var block in routine.Blocks)
                {
                    var current = new Dictionary<IrMemoryRegion, int>(incoming[block]);
                    var exactCurrent = new Dictionary<IrMemoryIdentity, int>(exactIncoming[block]);
                    foreach (var instruction in block.Instructions)
                    {
                        before[instruction] = new Dictionary<IrMemoryRegion, int>(current);
                        exactBefore[instruction] = new Dictionary<IrMemoryIdentity, int>(exactCurrent);
                        var unknownWrites = unknownWritesByInstruction[instruction];
                        foreach (var region in Regions.Where(region => (unknownWrites & region) != 0))
                        {
                            current[region] = writeVersions[(instruction, region)];
                            foreach (var identity in exactIdentities.Where(identity => identity.Region == region))
                                exactCurrent[identity] = writeVersions[(instruction, region)];
                        }
                        foreach (var identity in affectedIdentitiesByInstruction[instruction])
                            exactCurrent[identity] = exactWriteVersions[(instruction, identity)];
                    }
                }
                return new MemoryVersionAnalysis(before, exactBefore)
                {
                    ExactMergeCount = routine.Blocks.Sum(block => exactIdentities.Count(identity =>
                        block.Predecessors.Select(predecessor => exactOutgoing[predecessor][identity]).Distinct()
                            .Skip(1).Any())),
                };
            }
        }

        private void GlobalValueNumbering(RoutineIr routine)
        {
            routine.RebuildPredecessors();
            var blocks = routine.Blocks.ToArray();
            var stateDependencies = FindStateDependencies(blocks);
            var unknownStateDependencies = FindUnknownStateDependencies(blocks);
            var identityDependencies = FindIdentityDependencies(blocks);
            var memoryVersions = MemoryVersionAnalysis.Create(routine);
            Record("Exact memory version merges", memoryVersions.ExactMergeCount);
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
                            var writtenRegions = GetUnknownWrittenRegions(instruction);
                            var writtenIdentities = GetWrittenIdentities(instruction).ToArray();
                            foreach (var pair in available.ToArray())
                            {
                                var unknownInvalidation =
                                    (unknownStateDependencies.GetValueOrDefault(pair.Value.Value) & writtenRegions) != 0 ||
                                    identityDependencies.GetValueOrDefault(pair.Value.Value)?.Any(identity =>
                                        (writtenRegions & identity.Region) != 0) == true;
                                var exactInvalidation = identityDependencies.GetValueOrDefault(pair.Value.Value)
                                    ?.Any(identity => writtenIdentities.Any(identity.MayAlias)) == true;
                                if (!unknownInvalidation && !exactInvalidation)
                                    continue;
                                available.Remove(pair.Key);
                                Record(unknownInvalidation
                                    ? "GVN invalidated: unknown memory"
                                    : "GVN invalidated: exact memory");
                            }
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
                            $"{string.Join(",", currentIds)}|{memoryVersions.GetKey(instruction, dependencies,
                                identityDependencies.GetValueOrDefault(instruction.Result) ?? [])}");
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

        private static Dictionary<IrValue, IrMemoryRegion> FindUnknownStateDependencies(IEnumerable<IrBlock> blocks)
        {
            var instructions = blocks.SelectMany(block => block.Instructions).ToArray();
            var result = instructions.SelectMany(instruction => instruction.Operands)
                .Where(value => value.MutableExternal && value.MemoryIdentity == null)
                .Distinct().ToDictionary(value => value, _ => IrMemoryRegion.Globals);
            var changed = true;
            while (changed)
            {
                changed = false;
                foreach (var instruction in instructions.Where(instruction => instruction.Result != null))
                {
                    var dependencies = instruction.ReadIdentity == null
                        ? instruction.ReadRegions != IrMemoryRegion.None
                            ? instruction.ReadRegions
                            : GetReadRegions(instruction.Opcode)
                        : IrMemoryRegion.None;
                    foreach (var operand in instruction.Operands)
                        dependencies |= result.GetValueOrDefault(operand);
                    if (dependencies != result.GetValueOrDefault(instruction.Result!))
                    {
                        result[instruction.Result!] = dependencies;
                        changed = true;
                    }
                }
            }
            return result;
        }

        private static Dictionary<IrValue, HashSet<IrMemoryIdentity>> FindIdentityDependencies(
            IEnumerable<IrBlock> blocks)
        {
            var instructions = blocks.SelectMany(block => block.Instructions).ToArray();
            var result = instructions.SelectMany(instruction => instruction.Operands)
                .Where(value => value.MemoryIdentity != null).Distinct()
                .ToDictionary(value => value, value => new HashSet<IrMemoryIdentity> { value.MemoryIdentity! });
            var changed = true;
            while (changed)
            {
                changed = false;
                foreach (var instruction in instructions.Where(instruction => instruction.Result != null))
                {
                    var dependencies = instruction.ReadIdentity == null
                        ? []
                        : new HashSet<IrMemoryIdentity> { instruction.ReadIdentity };
                    foreach (var operand in instruction.Operands)
                    {
                        if (result.TryGetValue(operand, out var operandDependencies))
                            dependencies.UnionWith(operandDependencies);
                    }
                    if (!result.TryGetValue(instruction.Result!, out var existing) ||
                        !existing.SetEquals(dependencies))
                    {
                        result[instruction.Result!] = dependencies;
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

    }
}

