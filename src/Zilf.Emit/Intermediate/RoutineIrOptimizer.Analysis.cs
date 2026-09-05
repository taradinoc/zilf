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
using System.Diagnostics.CodeAnalysis;
using System.Linq;

namespace Zilf.Emit.Intermediate
{
    internal sealed partial class RoutineIrOptimizer
    {
        /// <summary>
        /// Iteratively propagates memory, stable, and routine-target identities through copy, phi, and
        /// address-arithmetic instructions, then resolves indirect calls whose targets are now known.
        /// </summary>
        /// <param name="routine">The routine to analyze.</param>
        private void PropagateMemoryIdentities(RoutineIr routine)
        {
            var changed = true;
            var propagationIterations = 0;
            while (changed)
            {
                if (propagationIterations++ > routine.Blocks.Count + 1)
                    throw new InvalidOperationException("Memory identity propagation did not converge.");
                changed = false;
                foreach (var instruction in routine.Blocks.SelectMany(block => block.Instructions))
                {
                    if (instruction.Result != null && TryInferMemoryIdentity(instruction) is { } inferred &&
                        instruction.Result.MemoryIdentity != inferred)
                    {
                        instruction.Result.MemoryIdentity = inferred;
                        changed = true;
                        Record("Pointer identities propagated");
                    }

                    if (instruction.Result != null && TryInferStableIdentity(instruction) is { } stableIdentity &&
                        !Equals(instruction.Result.StableIdentity, stableIdentity))
                    {
                        instruction.Result.StableIdentity = stableIdentity;
                        changed = true;
                        Record("Stable identities propagated");
                    }

                    if (instruction.Result != null &&
                        (TryInferRoutineTargets(instruction) ?? TryInferMemoryRoutineTargets(instruction)) is
                            { } routineTargets &&
                        !RoutineTargetsEqual(instruction.Result.RoutineTargets, routineTargets))
                    {
                        instruction.Result.RoutineTargets = routineTargets;
                        changed = true;
                        Record("Routine target sets propagated");
                    }

                    if (instruction.ReadIdentity == null && instruction.Opcode is IrOpcode.LoadByte or IrOpcode.LoadWord &&
                        TryGetAccessIdentity(instruction) is { } readIdentity)
                    {
                        instruction.ReadIdentity = readIdentity;
                        changed = true;
                        Record("Derived memory reads identified");
                    }

                    if (instruction.ReadIdentity == null && TryGetObjectReadIdentity(instruction) is { } objectIdentity)
                    {
                        instruction.ReadIdentity = objectIdentity;
                        changed = true;
                        Record("Exact object-member reads identified");
                    }
                }
            }


            foreach (var instruction in routine.Blocks.SelectMany(block => block.Instructions)
                .Where(instruction => instruction.Effect == IrEffect.Call && instruction.CallSummary == null &&
                    instruction.Operands.Count > 0 && instruction.Operands[0].RoutineTargets is { Count: > 0 }))
            {
                var summary = new IrRoutineEffectSummary { IsComplete = true };
                foreach (var target in instruction.Operands[0].RoutineTargets!)
                    summary.AddCallee(target, instruction.CallBindings);
                IrRoutineEffectSummary.Close([summary]);
                instruction.CallSummary = summary;
                Record("Indirect calls resolved");
            }
        }

        /// <summary>
        /// Infers the stable identity of an instruction's result from a copy or a phi whose operands all share
        /// the same identity.
        /// </summary>
        /// <param name="instruction">The instruction whose result should be analyzed.</param>
        /// <returns>The inferred stable identity, or <see langword="null"/> if one cannot be determined.</returns>
        private static object? TryInferStableIdentity(IrInstruction instruction)
        {
            if (instruction.Opcode == IrOpcode.Copy && instruction.Operands.Count == 1)
                return instruction.Operands[0].StableIdentity;
            if (instruction.Opcode != IrOpcode.Phi || instruction.Operands.Count == 0)
                return null;
            var identity = instruction.Operands[0].StableIdentity;
            return identity != null && instruction.Operands.Skip(1).All(operand =>
                Equals(operand.StableIdentity, identity)) ? identity : null;
        }

        /// <summary>
        /// Infers the set of possible routine targets for an instruction's result from a copy or a phi.
        /// </summary>
        /// <param name="instruction">The instruction whose result should be analyzed.</param>
        /// <returns>The inferred routine targets, or <see langword="null"/> if they cannot be determined.</returns>
        private static IReadOnlySet<IrRoutineEffectSummary>? TryInferRoutineTargets(IrInstruction instruction)
        {
            if (instruction.Opcode == IrOpcode.Copy && instruction.Operands.Count == 1)
                return instruction.Operands[0].RoutineTargets;
            if (instruction.Opcode != IrOpcode.Phi || instruction.Operands.Count == 0 ||
                instruction.Operands.Any(operand => operand.RoutineTargets == null))
                return null;
            var targets = instruction.Operands.SelectMany(operand => operand.RoutineTargets!).ToHashSet();
            return targets.Count <= 64 ? targets : null;
        }

        /// <summary>
        /// Infers the routine targets for a property or table load based on the loaded identity and the known
        /// property routine targets.
        /// </summary>
        /// <param name="instruction">The load instruction to analyze.</param>
        /// <returns>The inferred routine targets, or <see langword="null"/> if none apply.</returns>
        private IReadOnlySet<IrRoutineEffectSummary>? TryInferMemoryRoutineTargets(IrInstruction instruction)
        {
            if (instruction.Opcode == IrOpcode.LoadProperty && instruction.Operands.Count >= 2 &&
                GetStableIdentity(instruction.Operands[1]) is { } propertyKey)
            {
                if (GetStableIdentity(instruction.Operands[0]) is { } objectKey &&
                    propertyRoutineTargets.TryGetValue(new IrObjectMemberKey(objectKey, propertyKey), out var exact))
                    return exact;
                return propertyRoutineTargets.GetValueOrDefault(propertyKey);
            }
            if (instruction.Opcode is IrOpcode.LoadByte or IrOpcode.LoadWord && instruction.ReadIdentity is { } identity)
                return propertyRoutineTargets.GetValueOrDefault(identity) ??
                    propertyRoutineTargets.GetValueOrDefault(identity.Key);
            return null;
        }

        private static bool RoutineTargetsEqual(IReadOnlySet<IrRoutineEffectSummary>? left,
            IReadOnlySet<IrRoutineEffectSummary> right) => left != null && left.SetEquals(right);

        /// <summary>
        /// Computes the exact memory identity read by an object-tree, attribute, or property load whose object
        /// and member resolve to stable identities.
        /// </summary>
        /// <param name="instruction">The load instruction to analyze.</param>
        /// <returns>The read identity, or <see langword="null"/> if it cannot be determined.</returns>
        private static IrMemoryIdentity? TryGetObjectReadIdentity(IrInstruction instruction)
        {
            if (instruction.Operands.Count == 0 || GetStableIdentity(instruction.Operands[0]) is not { } objectKey)
                return null;
            if (instruction.Opcode is IrOpcode.LoadParent or IrOpcode.LoadChild or IrOpcode.LoadSibling)
                return new IrMemoryIdentity(IrMemoryRegion.ObjectTree, objectKey);
            var region = instruction.Opcode switch
            {
                IrOpcode.HasAttribute => IrMemoryRegion.Attributes,
                IrOpcode.LoadProperty or IrOpcode.LoadPropertyAddress or IrOpcode.LoadNextProperty =>
                    IrMemoryRegion.Properties,
                _ => IrMemoryRegion.None,
            };
            if (region == IrMemoryRegion.None || instruction.Operands.Count < 2 ||
                GetStableIdentity(instruction.Operands[1]) is not { } memberKey)
                return null;
            return new IrMemoryIdentity(region, new IrObjectMemberKey(objectKey, memberKey));
        }

        private static object? GetStableIdentity(IrValue value) => value.StableIdentity ?? value.Constant;

        /// <summary>
        /// Infers the table memory identity of an instruction's result from a copy, phi, or address arithmetic.
        /// </summary>
        /// <param name="instruction">The instruction whose result should be analyzed.</param>
        /// <returns>The inferred memory identity, or <see langword="null"/> if one cannot be determined.</returns>
        private static IrMemoryIdentity? TryInferMemoryIdentity(IrInstruction instruction)
        {
            if (instruction.Opcode == IrOpcode.Copy && instruction.Operands.Count == 1)
                return instruction.Operands[0].MemoryIdentity;
            if (instruction.Opcode == IrOpcode.Phi && instruction.Operands.Count > 0)
            {
                var identities = instruction.Operands.Select(operand => operand.MemoryIdentity).ToArray();
                if (identities.Any(identity => identity is not { Region: IrMemoryRegion.Tables }))
                    return null;
                var first = identities[0]!;
                if (identities.Skip(1).Any(identity => !Equals(identity!.Key, first.Key)))
                    return null;
                var mergedOffset = identities.All(identity => identity!.Offset == first.Offset) ? first.Offset : null;
                return first with { Offset = mergedOffset, Length = null };
            }
            if (instruction.Opcode is not (IrOpcode.Add or IrOpcode.Subtract) || instruction.Operands.Count != 2)
                return null;
            IrMemoryIdentity? identity;
            int delta;
            if (instruction.Operands[0].MemoryIdentity is { Region: IrMemoryRegion.Tables } left &&
                instruction.Operands[1].Constant is int right)
            {
                identity = left;
                delta = instruction.Opcode == IrOpcode.Add ? right : -right;
            }
            else if (instruction.Opcode == IrOpcode.Add && instruction.Operands[0].Constant is int leftConstant &&
                instruction.Operands[1].MemoryIdentity is { Region: IrMemoryRegion.Tables } rightIdentity)
            {
                identity = rightIdentity;
                delta = leftConstant;
            }
            else
                return null;
            if (identity.Offset == null)
                return identity with { Length = null };
            var offset = (long)identity.Offset.Value + delta;
            return offset is >= int.MinValue and <= int.MaxValue
                ? identity with { Offset = (int)offset, Length = null }
                : null;
        }

        /// <summary>
        /// Computes the exact memory identity read by a byte or word load from a table with a constant index.
        /// </summary>
        /// <param name="instruction">The load instruction to analyze.</param>
        /// <returns>The read identity, or <see langword="null"/> if it cannot be determined.</returns>
        private IrMemoryIdentity? TryGetAccessIdentity(IrInstruction instruction)
        {
            if (instruction.Operands.Count < 2 ||
                instruction.Operands[0].MemoryIdentity is not { Region: IrMemoryRegion.Tables } identity)
                return null;
            var width = instruction.Opcode == IrOpcode.LoadByte
                ? 1
                : numericSemantics == IrNumericSemantics.ZMachine16 ? 2 : 4;
            if (instruction.Operands[1].Constant is not int index)
                return identity with { Offset = null, Length = null };
            if (identity.Offset == null)
                return identity with { Length = null };
            var offset = (long)identity.Offset.Value + (long)index * width;
            return offset is >= int.MinValue and <= int.MaxValue
                ? identity with { Offset = (int)offset, Length = width }
                : null;
        }

        /// <summary>
        /// Performs store-to-load forwarding, replacing loads whose exact source was recently stored with the
        /// stored value.
        /// </summary>
        /// <param name="routine">The routine to transform.</param>
        private void ForwardStoredValues(RoutineIr routine)
        {
            routine.RebuildPredecessors();
            var input = routine.Blocks.ToDictionary(block => block,
                _ => new Dictionary<IrMemoryIdentity, IrValue>());
            var output = routine.Blocks.ToDictionary(block => block,
                _ => new Dictionary<IrMemoryIdentity, IrValue>());
            var changed = true;
            var forwardingIterations = 0;
            while (changed)
            {
                if (forwardingIterations++ > routine.Blocks.Count + 1)
                    throw new InvalidOperationException("Store-to-load analysis did not converge.");
                changed = false;
                foreach (var block in routine.Blocks)
                {
                    var incoming = MergeStoredValues(block.Predecessors.Select(predecessor => output[predecessor]));
                    if (!StoredValuesEqual(input[block], incoming))
                    {
                        input[block] = incoming;
                        changed = true;
                    }
                    var current = new Dictionary<IrMemoryIdentity, IrValue>(incoming);
                    foreach (var instruction in block.Instructions)
                    {
                        KillStoredValues(current, instruction);
                        if (instruction.WriteIdentity is { } identity && instruction.WrittenValue is { } value &&
                            IsReusableStoredValue(value))
                            current[identity] = value;
                    }
                    if (!StoredValuesEqual(output[block], current))
                    {
                        output[block] = current;
                        changed = true;
                    }
                }
            }

            var replacements = new Dictionary<IrValue, IrValue>();
            foreach (var block in routine.Blocks)
            {
                var current = new Dictionary<IrMemoryIdentity, IrValue>(input[block]);
                foreach (var instruction in block.Instructions)
                {
                    if (instruction.ReadIdentity is { } readIdentity && instruction.Result != null &&
                        current.TryGetValue(readIdentity, out var stored))
                    {
                        if (instruction.Payload is IrLoweringOperation { RequiredHome: true })
                            Record("Store-to-load rejected: required home");
                        else
                        {
                            replacements[instruction.Result] = stored;
                            instruction.Result.PhysicalHome = stored.PhysicalHome;
                            Record(stored.Constant != null
                                ? "Store-to-load forwarded constants"
                                : "Store-to-load forwarded values");
                        }
                    }
                    KillStoredValues(current, instruction);
                    if (instruction.WriteIdentity is { } writeIdentity && instruction.WrittenValue is { } value &&
                        IsReusableStoredValue(value))
                        current[writeIdentity] = value;
                }
            }
            ReplaceValues(routine, replacements);
        }

        private static bool IsReusableStoredValue(IrValue value) =>
            value.Constant != null || value.PhysicalHome is ILocalBuilder;

        /// <summary>
        /// Merges the stored-value maps of several predecessor blocks, keeping only the identities stored to the
        /// same value along every path.
        /// </summary>
        /// <param name="predecessors">The predecessor maps to merge.</param>
        /// <returns>The merged stored-value map.</returns>
        private static Dictionary<IrMemoryIdentity, IrValue> MergeStoredValues(
            IEnumerable<Dictionary<IrMemoryIdentity, IrValue>> predecessors)
        {
            using var enumerator = predecessors.GetEnumerator();
            if (!enumerator.MoveNext())
                return [];
            var result = new Dictionary<IrMemoryIdentity, IrValue>(enumerator.Current);
            while (enumerator.MoveNext())
            {
                foreach (var pair in result.ToArray())
                {
                    if (!enumerator.Current.TryGetValue(pair.Key, out var value) ||
                        !StoredValueEqual(value, pair.Value))
                        result.Remove(pair.Key);
                }
            }
            return result;
        }

        private static bool StoredValuesEqual(Dictionary<IrMemoryIdentity, IrValue> left,
            Dictionary<IrMemoryIdentity, IrValue> right) => left.Count == right.Count && left.All(pair =>
                right.TryGetValue(pair.Key, out var value) && StoredValueEqual(value, pair.Value));

        private static bool StoredValueEqual(IrValue left, IrValue right) => ReferenceEquals(left, right) ||
            left.Constant is int constant && right.Constant == constant;

        /// <summary>
        /// Removes stored values from the map that the given instruction may overwrite.
        /// </summary>
        /// <param name="values">The stored-value map to update.</param>
        /// <param name="instruction">The instruction whose writes invalidate entries.</param>
        private static void KillStoredValues(Dictionary<IrMemoryIdentity, IrValue> values, IrInstruction instruction)
        {
            if (instruction.Effect == IrEffect.Opaque)
            {
                values.Clear();
                return;
            }
            var unknown = GetUnknownWrittenRegions(instruction);
            var exact = GetWrittenIdentities(instruction).ToArray();
            var writtenHome = instruction.Payload is IrLoweringOperation
                { ResultHome: IVariable home, IsStackResult: false }
                ? home
                : null;
            foreach (var identity in values.Keys.Where(identity => (unknown & identity.Region) != 0 ||
                exact.Any(identity.MayAlias) || writtenHome != null &&
                ReferenceEquals(values[identity].PhysicalHome, writtenHome)).ToArray())
                values.Remove(identity);
        }

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

            public IReadOnlyDictionary<IrBlock, HashSet<IrBlock>> Dominators => dominators;

            /// <summary>
            /// Determines whether one block dominates another.
            /// </summary>
            /// <param name="dominator">The candidate dominator.</param>
            /// <param name="block">The block to check.</param>
            /// <returns><see langword="true"/> if <paramref name="dominator"/> dominates <paramref name="block"/>; otherwise, <see langword="false"/>.</returns>
            public bool Dominates(IrBlock dominator, IrBlock block) => dominators[block].Contains(dominator);

            /// <summary>
            /// Computes dominator sets and natural loops for the given routine.
            /// </summary>
            /// <param name="routine">The routine to analyze.</param>
            /// <returns>The computed control-flow analysis.</returns>
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

            /// <summary>
            /// Builds a version key describing the memory state an instruction depends on, combining region
            /// versions with exact identity versions.
            /// </summary>
            /// <param name="instruction">The instruction being numbered.</param>
            /// <param name="dependencies">The memory regions the instruction depends on.</param>
            /// <param name="exactDependencies">The exact identities the instruction depends on.</param>
            /// <returns>The version key string.</returns>
            public string GetKey(IrInstruction instruction, IrMemoryRegion dependencies,
                IEnumerable<IrMemoryIdentity> exactDependencies)
            {
                var regionKey = dependencies == IrMemoryRegion.None
                    ? ""
                    : string.Join(",", Regions.Where(region => (dependencies & region) != 0)
                        .Select(region => before[instruction][region]));
                var exactKey = exactBefore.TryGetValue(instruction, out var exactVersions)
                    ? string.Join(",", exactDependencies.OrderBy(identity => identity.GetHashCode())
                        .Select(identity => exactVersions.GetValueOrDefault(identity)))
                    : "";
                return $"{regionKey}|{exactKey}";
            }

            /// <summary>
            /// Assigns SSA-style version numbers to memory regions and exact identities, and computes the
            /// version state observed before each value-numberable instruction.
            /// </summary>
            /// <param name="routine">The routine to analyze.</param>
            /// <param name="stateDependencies">The region dependencies of each value.</param>
            /// <param name="identityDependencies">The exact identity dependencies of each value.</param>
            /// <returns>The computed memory version analysis.</returns>
            public static MemoryVersionAnalysis Create(RoutineIr routine,
                IReadOnlyDictionary<IrValue, IrMemoryRegion> stateDependencies,
                IReadOnlyDictionary<IrValue, HashSet<IrMemoryIdentity>> identityDependencies)
            {
                routine.RebuildPredecessors();
                var nextVersion = 1;
                var instructions = routine.Blocks.SelectMany(block => block.Instructions).ToArray();
                var valueNumberable = instructions.Where(instruction => instruction.Result != null &&
                    IsValueNumberable(instruction)).ToHashSet();
                var relevantRegions = valueNumberable.Aggregate(IrMemoryRegion.None, (regions, instruction) =>
                    regions | stateDependencies.GetValueOrDefault(instruction.Result!) |
                    (IsReadOnlyCall(instruction) ? instruction.CallSummary!.GetReadRegions() : IrMemoryRegion.None));
                var regions = Regions.Where(region => (relevantRegions & region) != 0).ToArray();
                var exactIdentities = valueNumberable.SelectMany(instruction =>
                        (identityDependencies.GetValueOrDefault(instruction.Result!) ?? []).Concat(
                            IsReadOnlyCall(instruction) ? instruction.CallSummary!.GetReadIdentities() : []))
                    .Distinct().ToArray();
                if (regions.Length == 0 && exactIdentities.Length == 0)
                    return new([], []);
                var initial = regions.ToDictionary(region => region, _ => nextVersion++);
                var unknownWritesByInstruction = instructions.ToDictionary(instruction => instruction,
                    GetUnknownWrittenRegions);
                var writtenIdentitiesByInstruction = instructions.ToDictionary(instruction => instruction,
                    instruction => GetWrittenIdentities(instruction).ToArray());
                var identitiesByRegion = exactIdentities.GroupBy(identity => identity.Region)
                    .ToDictionary(group => group.Key, group => group.ToArray());
                var affectedIdentitiesByInstruction = instructions.ToDictionary(instruction => instruction,
                    instruction => writtenIdentitiesByInstruction[instruction]
                        .SelectMany(written => identitiesByRegion.GetValueOrDefault(written.Region) ?? [])
                        .Distinct().Where(identity => writtenIdentitiesByInstruction[instruction]
                            .Any(identity.MayAlias)).ToArray());
                var exactInitial = exactIdentities.ToDictionary(identity => identity, _ => nextVersion++);
                var writeVersions = routine.Blocks.SelectMany(block => block.Instructions)
                    .SelectMany(instruction => regions.Where(region =>
                            (unknownWritesByInstruction[instruction] & region) != 0)
                        .Select(region => (instruction, region)))
                    .ToDictionary(pair => pair, _ => nextVersion++);
                var exactWriteVersions = routine.Blocks.SelectMany(block => block.Instructions)
                    .SelectMany(instruction => affectedIdentitiesByInstruction[instruction]
                        .Select(identity => (instruction, identity)))
                    .ToDictionary(pair => pair, _ => nextVersion++);
                var mergeVersions = routine.Blocks.SelectMany(block => regions.Select(region => (block, region)))
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

                var pendingBlocks = new Queue<IrBlock>(routine.Blocks);
                var queuedBlocks = routine.Blocks.ToHashSet();
                while (pendingBlocks.TryDequeue(out var block))
                {
                    queuedBlocks.Remove(block);
                    var nextIn = new Dictionary<IrMemoryRegion, int>();
                    foreach (var region in regions)
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
                        foreach (var region in regions.Where(region => (unknownWrites & region) != 0))
                        {
                            nextOut[region] = writeVersions[(instruction, region)];
                            foreach (var identity in exactIdentities.Where(identity => identity.Region == region))
                                nextExactOut[identity] = writeVersions[(instruction, region)];
                        }
                        foreach (var identity in affectedIdentitiesByInstruction[instruction])
                            nextExactOut[identity] = exactWriteVersions[(instruction, identity)];
                    }
                    if (!nextIn.SequenceEqual(incoming[block]))
                        incoming[block] = nextIn;
                    if (!nextExactIn.SequenceEqual(exactIncoming[block]))
                        exactIncoming[block] = nextExactIn;
                    var outgoingChanged = !nextOut.SequenceEqual(outgoing[block]) ||
                        !nextExactOut.SequenceEqual(exactOutgoing[block]);
                    if (!nextOut.SequenceEqual(outgoing[block]))
                        outgoing[block] = nextOut;
                    if (!nextExactOut.SequenceEqual(exactOutgoing[block]))
                        exactOutgoing[block] = nextExactOut;
                    if (!outgoingChanged)
                        continue;
                    foreach (var successor in RoutineIr.GetSuccessors(block))
                    {
                        if (queuedBlocks.Add(successor))
                            pendingBlocks.Enqueue(successor);
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
                        if (valueNumberable.Contains(instruction) &&
                            (stateDependencies.GetValueOrDefault(instruction.Result!) |
                                (IsReadOnlyCall(instruction)
                                    ? instruction.CallSummary!.GetReadRegions()
                                    : IrMemoryRegion.None)) is var dependencies &&
                            dependencies != IrMemoryRegion.None)
                            before[instruction] = regions.Where(region => (dependencies & region) != 0)
                                .ToDictionary(region => region, region => current[region]);
                        if (valueNumberable.Contains(instruction))
                        {
                            var identities = (identityDependencies.GetValueOrDefault(instruction.Result!) ?? []).Concat(
                                IsReadOnlyCall(instruction)
                                    ? instruction.CallSummary!.GetReadIdentities()
                                    : []).ToHashSet();
                            if (identities.Count != 0)
                                exactBefore[instruction] = identities.ToDictionary(identity => identity,
                                    identity => exactCurrent[identity]);
                        }
                        var unknownWrites = unknownWritesByInstruction[instruction];
                        foreach (var region in regions.Where(region => (unknownWrites & region) != 0))
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

        /// <summary>
        /// Performs global value numbering, eliminating redundant computations (or redundant read-only calls)
        /// by tracking available expressions along the dominator tree.
        /// </summary>
        /// <param name="routine">The routine to transform.</param>
        /// <param name="cfg">The control-flow analysis of the routine.</param>
        /// <param name="callsOnly">Whether to number only read-only calls.</param>
        private void GlobalValueNumbering(RoutineIr routine, CfgAnalysis cfg, bool callsOnly)
        {
            routine.RebuildPredecessors();
            var blocks = routine.Blocks.ToArray();
            var stateDependencies = FindStateDependencies(blocks);
            var unknownStateDependencies = FindUnknownStateDependencies(blocks);
            var identityDependencies = FindIdentityDependencies(blocks);
            var memoryVersions = MemoryVersionAnalysis.Create(routine, stateDependencies, identityDependencies);
            Record("Exact memory version merges", memoryVersions.ExactMergeCount);
            var liveHomesAfter = FindLiveHomesAfter(blocks);
            var instructionBlocks = blocks.SelectMany(block => block.Instructions.Select(instruction =>
                (instruction, block))).ToDictionary(pair => pair.instruction, pair => pair.block);
            var instructionIndices = blocks.SelectMany(block => block.Instructions.Select((instruction, index) =>
                (instruction, index))).ToDictionary(pair => pair.instruction, pair => pair.index);
            var definitions = instructionBlocks.Keys.Where(instruction => instruction.Result != null)
                .ToDictionary(instruction => instruction.Result!, instruction => instruction);
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
            var children = blocks.ToDictionary(block => block, _ => new List<IrBlock>());
            foreach (var block in blocks.Where(block => !ReferenceEquals(block, routine.Entry)))
            {
                var idom = cfg.Dominators[block].Where(candidate => !ReferenceEquals(candidate, block))
                    .OrderByDescending(candidate => cfg.Dominators[candidate].Count).FirstOrDefault();
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
                    if (instruction.WriteRegions != IrMemoryRegion.None ||
                        instruction.Effect is IrEffect.WriteMemory or IrEffect.Call or IrEffect.Opaque)
                    {
                        if (instruction.Effect == IrEffect.Opaque)
                            available.Clear();
                        else
                        {
                            var writtenRegions = GetUnknownWrittenRegions(instruction);
                            var writtenIdentities = GetWrittenIdentities(instruction).ToArray();
                            foreach (var pair in available.ToArray())
                            {
                                var availableSummary = pair.Value.Instruction.CallSummary;
                                var availableUnknownReads = IsReadOnlyCall(pair.Value.Instruction)
                                    ? availableSummary!.GetUnknownReadRegions()
                                    : IrMemoryRegion.None;
                                IEnumerable<IrMemoryIdentity> availableExactReads = IsReadOnlyCall(pair.Value.Instruction)
                                    ? availableSummary!.GetReadIdentities()
                                    : [];
                                var unknownInvalidation =
                                    ((unknownStateDependencies.GetValueOrDefault(pair.Value.Value) |
                                        availableUnknownReads) & writtenRegions) != 0 ||
                                    identityDependencies.GetValueOrDefault(pair.Value.Value)?.Any(identity =>
                                        (writtenRegions & identity.Region) != 0) == true ||
                                    availableExactReads.Any(identity => (writtenRegions & identity.Region) != 0);
                                var exactInvalidation =
                                    (identityDependencies.GetValueOrDefault(pair.Value.Value) ?? [])
                                    .Concat(availableExactReads)
                                    .Any(identity => writtenIdentities.Any(identity.MayAlias));
                                if (!unknownInvalidation && !exactInvalidation)
                                    continue;
                                available.Remove(pair.Key);
                                Record(unknownInvalidation
                                    ? "GVN invalidated: unknown memory"
                                    : "GVN invalidated: exact memory");
                                RecordMemoryRejection("GVN", pair.Value.Instruction.Opcode,
                                    unknownInvalidation ? writtenRegions : writtenIdentities.Aggregate(
                                        IrMemoryRegion.None, (regions, identity) => regions | identity.Region),
                                    GetBarrierKind(instruction, unknownInvalidation));
                            }
                        }
                    }
                    var resultHome = (instruction.Payload as IrLoweringOperation)?.ResultHome;
                    var isStackResult = instruction.Payload is IrLoweringOperation { IsStackResult: true };
                    (IrOpcode Opcode, string Operands)? currentKey = null;
                    if (instruction.Result != null && IsGvnCandidate(instruction))
                    {
                        Record("GVN candidates");
                        var currentIds = instruction.Operands.Select(OperandKey).ToArray();
                        if (IsCommutative(instruction.Opcode))
                            Array.Sort(currentIds);
                        var keyOpcode = instruction.Opcode;
                        if (TryGetConstantOffsetExpression(instruction.Result, definitions, out var addressBase,
                            out var addressOffset))
                        {
                            keyOpcode = IrOpcode.Add;
                            currentIds = [$"A{addressBase.Id}", $"O{addressOffset}"];
                        }
                        var dependencies = stateDependencies.GetValueOrDefault(instruction.Result);
                        var exactDependencies = identityDependencies.GetValueOrDefault(instruction.Result) ?? [];
                        if (IsReadOnlyCall(instruction))
                        {
                            dependencies |= instruction.CallSummary!.GetReadRegions();
                            exactDependencies = exactDependencies.Concat(
                                instruction.CallSummary.GetReadIdentities()).ToHashSet();
                        }
                        currentKey = (keyOpcode,
                            $"{string.Join(",", currentIds)}|{memoryVersions.GetKey(instruction, dependencies,
                                exactDependencies)}");
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
                    if (instruction.Result == null || !IsGvnCandidate(instruction))
                        continue;
                    var key = currentKey!.Value;
                    if (available.TryGetValue(key, out var prior) &&
                        CanReusePhysicalHome(prior.Instruction, instruction))
                    {
                        Record("GVN eliminated expressions");
                        replacements[instruction.Result] = Resolve(prior.Value);
                        instruction.Result.PhysicalHome = Resolve(prior.Value).PhysicalHome;
                        ReserveHomeThroughUse(prior.Instruction, instruction.Result);
                    }
                    else if (available.TryGetValue(key, out prior) &&
                        TryPromoteStackValue(prior.Instruction, instruction,
                            FindReusableTemporary(prior.Instruction, instruction)))
                    {
                        Record("GVN promoted stack results");
                        liveHomesAfter = FindLiveHomesAfter(blocks);
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

            bool IsGvnCandidate(IrInstruction instruction) =>
                callsOnly ? IsReadOnlyCall(instruction) : IsValueNumberable(instruction) && !IsReadOnlyCall(instruction);

            bool TryGetConstantOffsetExpression(IrValue? value,
                IReadOnlyDictionary<IrValue, IrInstruction> valueDefinitions, [NotNullWhen(true)] out IrValue? baseValue, out int offset)
            {
                baseValue = null!;
                offset = 0;
                if (value == null || !valueDefinitions.TryGetValue(value, out var definition) ||
                    definition.Opcode is not (IrOpcode.Add or IrOpcode.Subtract) || definition.Operands.Count != 2)
                    return false;
                IrValue nested;
                int delta;
                if (definition.Operands[1].Constant is int right)
                {
                    nested = definition.Operands[0];
                    delta = definition.Opcode == IrOpcode.Add ? right : -right;
                }
                else if (definition.Opcode == IrOpcode.Add && definition.Operands[0].Constant is int left)
                {
                    nested = definition.Operands[1];
                    delta = left;
                }
                else
                {
                    return false;
                }
                if (TryGetConstantOffsetExpression(nested, valueDefinitions, out var nestedBase, out var nestedOffset))
                {
                    baseValue = nestedBase;
                    offset = Normalize(nestedOffset + delta);
                }
                else
                {
                    baseValue = nested;
                    offset = Normalize(delta);
                }
                return true;
            }

            IVariable? FindReusableTemporary(IrInstruction definition, IrInstruction use)
            {
                if (!instructionBlocks.TryGetValue(definition, out var definitionBlock) ||
                    !instructionBlocks.ContainsKey(use))
                    return null;
                var live = liveHomesAfter.GetValueOrDefault(definition);
                var definitionIndex = instructionIndices[definition];
                return reusableTemporaries().FirstOrDefault(candidate =>
                    (live == null || !live.Contains(candidate)) &&
                    homeReservations.GetValueOrDefault((definitionBlock, candidate), -1) < definitionIndex &&
                    HomeSurvivesUntilUse(candidate, definition, definitionBlock, use));
            }

            bool HomeSurvivesUntilUse(IVariable home, IrInstruction definition, IrBlock definitionBlock,
                IrInstruction use)
            {
                var pending = new Stack<(IrBlock Block, int Index, bool Clobbered)>();
                var visited = new HashSet<(IrBlock Block, int Index, bool Clobbered)>();
                pending.Push((definitionBlock, instructionIndices[definition] + 1, false));
                var reachedUse = false;
                while (pending.Count > 0)
                {
                    var state = pending.Pop();
                    if (!visited.Add(state))
                        continue;
                    var clobbered = state.Clobbered;
                    var stoppedAtUse = false;
                    for (var i = state.Index; i < state.Block.Instructions.Count; i++)
                    {
                        var instruction = state.Block.Instructions[i];
                        if (ReferenceEquals(instruction, use))
                        {
                            reachedUse = true;
                            if (clobbered)
                                return false;
                            stoppedAtUse = true;
                            break;
                        }
                        if (ReferenceEquals(instruction, definition))
                            clobbered = false;
                        else if (instruction.Payload is IrLoweringOperation
                            { IsStackResult: false, ResultHome: not null } lowering &&
                            ReferenceEquals(lowering.ResultHome, home))
                            clobbered = true;
                    }
                    if (!stoppedAtUse)
                    {
                        foreach (var successor in RoutineIr.GetSuccessors(state.Block))
                            pending.Push((successor, 0, clobbered));
                    }
                }
                return reachedUse;
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

        /// <summary>
        /// Records debug-only rejection statistics for each memory region an optimization was blocked by.
        /// </summary>
        /// <param name="optimization">The name of the optimization.</param>
        /// <param name="opcode">The opcode of the rejected instruction.</param>
        /// <param name="regions">The memory regions that caused the rejection.</param>
        /// <param name="barrier">A description of the write barrier encountered.</param>
        private void RecordMemoryRejection(string optimization, IrOpcode opcode, IrMemoryRegion regions,
            string barrier)
        {
#if DEBUG
            foreach (var region in Enum.GetValues<IrMemoryRegion>().Where(region => region != IrMemoryRegion.None &&
                region != IrMemoryRegion.All && (regions & region) != 0))
                Record($"{optimization} rejected: {opcode}: {region}: {barrier}");
#endif
        }

        /// <summary>
        /// Returns a human-readable description of the write barrier an instruction represents.
        /// </summary>
        /// <param name="instruction">The instruction acting as a barrier.</param>
        /// <param name="unknown">Whether the barrier is an unknown write rather than an exact one.</param>
        /// <returns>The barrier description.</returns>
        private static string GetBarrierKind(IrInstruction instruction, bool unknown) => instruction.Effect switch
        {
            IrEffect.Opaque => "opaque operation",
            IrEffect.Call when instruction.CallSummary == null || !instruction.CallSummary.IsComplete =>
                "incomplete call",
            IrEffect.Call => "summarized call",
            _ => unknown ? "unknown write" : "exact write",
        };

        /// <summary>
        /// Attempts to rewrite the given instruction as a copy of a previously computed value.
        /// </summary>
        /// <param name="priorValue">The value to copy.</param>
        /// <param name="priorInstruction">The instruction that produced the value.</param>
        /// <param name="instruction">The instruction to rewrite.</param>
        /// <returns><see langword="true"/> if the instruction was rewritten; otherwise, <see langword="false"/>.</returns>
        private bool TryRewriteAsCopy(IrValue priorValue, IrInstruction priorInstruction, IrInstruction instruction)
        {
            if (emitCopy == null ||
                !costPolicy.ShouldRewriteAsCopy(instruction, priorValue) ||
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

        /// <summary>
        /// Computes, for each value, the memory regions its computation transitively depends on.
        /// </summary>
        /// <param name="blocks">The blocks to analyze.</param>
        /// <returns>A map from each value to its dependent regions.</returns>
        private static Dictionary<IrValue, IrMemoryRegion> FindStateDependencies(IEnumerable<IrBlock> blocks)
        {
            var instructions = blocks.SelectMany(block => block.Instructions).ToArray();
            var result = instructions.SelectMany(instruction => instruction.Operands)
                .Where(value => value.MutableExternal)
                .Distinct().ToDictionary(value => value, _ => IrMemoryRegion.Globals);
            var users = BuildUsers(instructions);
            var pending = new Queue<IrInstruction>(instructions.Where(instruction => instruction.Result != null));
            var queued = pending.ToHashSet();
            while (pending.TryDequeue(out var instruction))
            {
                queued.Remove(instruction);
                var dependencies = instruction.ReadRegions != IrMemoryRegion.None
                    ? instruction.ReadRegions
                    : GetReadRegions(instruction.Opcode);
                foreach (var operand in instruction.Operands)
                    dependencies |= result.GetValueOrDefault(operand);
                if (dependencies == result.GetValueOrDefault(instruction.Result!))
                    continue;
                result[instruction.Result!] = dependencies;
                if (!users.TryGetValue(instruction.Result!, out var consumers))
                    continue;
                foreach (var consumer in consumers)
                {
                    if (consumer.Result != null && queued.Add(consumer))
                        pending.Enqueue(consumer);
                }
            }
            return result;
        }

        /// <summary>
        /// Computes, for each value, the memory regions it depends on through reads with unknown identities.
        /// </summary>
        /// <param name="blocks">The blocks to analyze.</param>
        /// <returns>A map from each value to its unknown dependent regions.</returns>
        private static Dictionary<IrValue, IrMemoryRegion> FindUnknownStateDependencies(IEnumerable<IrBlock> blocks)
        {
            var instructions = blocks.SelectMany(block => block.Instructions).ToArray();
            var result = instructions.SelectMany(instruction => instruction.Operands)
                .Where(value => value.MutableExternal && value.MemoryIdentity == null)
                .Distinct().ToDictionary(value => value, _ => IrMemoryRegion.Globals);
            var users = BuildUsers(instructions);
            var pending = new Queue<IrInstruction>(instructions.Where(instruction => instruction.Result != null));
            var queued = pending.ToHashSet();
            while (pending.TryDequeue(out var instruction))
            {
                queued.Remove(instruction);
                var dependencies = instruction.ReadIdentity == null
                    ? instruction.ReadRegions != IrMemoryRegion.None
                        ? instruction.ReadRegions
                        : GetReadRegions(instruction.Opcode)
                    : IrMemoryRegion.None;
                foreach (var operand in instruction.Operands)
                    dependencies |= result.GetValueOrDefault(operand);
                if (dependencies == result.GetValueOrDefault(instruction.Result!))
                    continue;
                result[instruction.Result!] = dependencies;
                if (!users.TryGetValue(instruction.Result!, out var consumers))
                    continue;
                foreach (var consumer in consumers)
                {
                    if (consumer.Result != null && queued.Add(consumer))
                        pending.Enqueue(consumer);
                }
            }
            return result;
        }

        /// <summary>
        /// Computes, for each value, the set of exact memory identities its computation transitively depends on.
        /// </summary>
        /// <param name="blocks">The blocks to analyze.</param>
        /// <returns>A map from each value to its dependent identities.</returns>
        private static Dictionary<IrValue, HashSet<IrMemoryIdentity>> FindIdentityDependencies(
            IEnumerable<IrBlock> blocks)
        {
            var instructions = blocks.SelectMany(block => block.Instructions).ToArray();
            var result = instructions.SelectMany(instruction => instruction.Operands)
                .Where(value => value.MemoryIdentity != null).Distinct()
                .ToDictionary(value => value, value => new HashSet<IrMemoryIdentity> { value.MemoryIdentity! });
            var users = BuildUsers(instructions);
            var pending = new Queue<IrInstruction>(instructions.Where(instruction => instruction.Result != null));
            var queued = pending.ToHashSet();
            while (pending.TryDequeue(out var instruction))
            {
                queued.Remove(instruction);
                var dependencies = instruction.ReadIdentity == null
                    ? []
                    : new HashSet<IrMemoryIdentity> { instruction.ReadIdentity };
                foreach (var operand in instruction.Operands)
                {
                    if (result.TryGetValue(operand, out var operandDependencies))
                        dependencies.UnionWith(operandDependencies);
                }
                if (result.TryGetValue(instruction.Result!, out var existing) && existing.SetEquals(dependencies))
                    continue;
                result[instruction.Result!] = dependencies;
                if (!users.TryGetValue(instruction.Result!, out var consumers))
                    continue;
                foreach (var consumer in consumers)
                {
                    if (consumer.Result != null && queued.Add(consumer))
                        pending.Enqueue(consumer);
                }
            }
            return result;
        }

        /// <summary>
        /// Builds a map from each value to the instructions that use it.
        /// </summary>
        /// <param name="instructions">The instructions to index.</param>
        /// <returns>The use-definition map.</returns>
        private static Dictionary<IrValue, List<IrInstruction>> BuildUsers(IEnumerable<IrInstruction> instructions)
        {
            var result = new Dictionary<IrValue, List<IrInstruction>>();
            foreach (var instruction in instructions)
            {
                foreach (var operand in instruction.Operands)
                {
                    if (!result.TryGetValue(operand, out var users))
                        result[operand] = users = [];
                    users.Add(instruction);
                }
            }
            return result;
        }

        /// <summary>
        /// Returns the memory region read by an opcode, or <see cref="IrMemoryRegion.None"/> if the opcode does
        /// not read memory.
        /// </summary>
        /// <param name="opcode">The opcode to classify.</param>
        /// <returns>The read memory region.</returns>
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

        /// <summary>
        /// Attempts to promote a stack result computed by an earlier instruction into a temporary local so it can
        /// be reused by the current instruction.
        /// </summary>
        /// <param name="prior">The instruction that computed the stack result.</param>
        /// <param name="current">The instruction that wants to reuse it.</param>
        /// <param name="reusableTemporary">An already-available temporary to use, or <see langword="null"/>.</param>
        /// <returns><see langword="true"/> if the promotion succeeded; otherwise, <see langword="false"/>.</returns>
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

            if (!costPolicy.ShouldPromoteStackResult(prior, current, reusableTemporary != null))
            {
                Record("GVN rejected: unprofitable rewrite");
                return false;
            }

            var temporary = reusableTemporary ?? acquireTemporary();
            if (temporary == null)
                return false;

            priorLowering.ResultHome = temporary;
            priorLowering.IsStackResult = false;
            prior.Result!.PhysicalHome = temporary;
            current.Result!.PhysicalHome = temporary;
            return true;
        }

        /// <summary>
        /// Computes, for each instruction, the set of variable homes whose values are live immediately after it.
        /// </summary>
        /// <param name="blocks">The blocks to analyze.</param>
        /// <returns>A map from each instruction to its live homes.</returns>
        private static Dictionary<IrInstruction, HashSet<IVariable>> FindLiveHomesAfter(IEnumerable<IrBlock> blocks)
        {
            var blockArray = blocks.ToArray();
            var definitions = blockArray.SelectMany(block => block.Instructions)
                .Where(instruction => instruction.Result != null)
                .ToDictionary(instruction => instruction.Result!, instruction => instruction);
            var meaningfulValues = new HashSet<IrValue>();
            static IEnumerable<IrValue> TerminatorValues(IrBlock block) => block.Terminator switch
            {
                IrTerminator.Branch branch => [branch.Condition],
                IrTerminator.Return { Value: not null } ret => [ret.Value],
                _ => [],
            };
            var pendingValues = new Stack<IrValue>(blockArray.SelectMany(block => block.Instructions)
                .Where(instruction => instruction.Opcode != IrOpcode.Phi)
                .SelectMany(instruction => instruction.Operands).Concat(blockArray.SelectMany(TerminatorValues)));
            while (pendingValues.TryPop(out var value))
            {
                if (!meaningfulValues.Add(value) || !definitions.TryGetValue(value, out var definition))
                    continue;
                foreach (var operand in definition.Operands)
                    pendingValues.Push(operand);
            }
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
                    foreach (var successor in RoutineIr.GetSuccessors(block))
                    {
                        foreach (var phi in successor.Instructions.Where(instruction =>
                            instruction.Payload is IrPhi && instruction.Result != null &&
                            meaningfulValues.Contains(instruction.Result)))
                        {
                            if (((IrPhi)phi.Payload!).Incoming.TryGetValue(block, out var incoming) &&
                                incoming.PhysicalHome is IVariable incomingHome)
                                nextOut.Add(incomingHome);
                        }
                    }
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
                        if (instruction.Opcode == IrOpcode.Phi)
                        {
                            if (instruction.Result?.PhysicalHome is IVariable phiHome)
                                nextIn.Remove(phiHome);
                        }
                        else
                        {
                            foreach (var home in instruction.Operands.Select(operand => operand.PhysicalHome)
                                .OfType<IVariable>())
                                nextIn.Add(home);
                        }
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
                    if (instruction.Opcode == IrOpcode.Phi)
                    {
                        if (instruction.Result?.PhysicalHome is IVariable phiHome)
                            live.Remove(phiHome);
                    }
                    else
                    {
                        foreach (var home in instruction.Operands.Select(operand => operand.PhysicalHome)
                            .OfType<IVariable>())
                            live.Add(home);
                    }
                }
            }
            return result;
        }

        /// <summary>
        /// Determines whether the physical result home of a prior instruction can be reused for the current
        /// instruction.
        /// </summary>
        /// <param name="prior">The earlier instruction.</param>
        /// <param name="current">The later instruction.</param>
        /// <returns><see langword="true"/> if the home can be reused; otherwise, <see langword="false"/>.</returns>
        private static bool CanReusePhysicalHome(IrInstruction prior, IrInstruction current)
        {
            if (prior.Payload is not IrLoweringOperation priorLowering ||
                current.Payload is not IrLoweringOperation currentLowering)
                return true;
            if (priorLowering.IsStackResult)
                return false;
            if (currentLowering.IsStackResult)
                return priorLowering.ResultHome != null;
            if (IsMemoryRead(prior.Opcode) && prior.Opcode == current.Opcode)
                return priorLowering.ResultHome != null && currentLowering.ResultHome != null;
            return priorLowering.ResultHome != null &&
                ReferenceEquals(priorLowering.ResultHome, currentLowering.ResultHome);
        }

        /// <summary>
        /// Determines whether an instruction's result is eligible for value numbering.
        /// </summary>
        /// <param name="instruction">The instruction to check.</param>
        /// <returns><see langword="true"/> if the instruction can be value-numbered; otherwise, <see langword="false"/>.</returns>
        private static bool IsValueNumberable(IrInstruction instruction) =>
            IsReadOnlyCall(instruction) ||
            (instruction.IsPure || instruction.Effect == IrEffect.ReadMemory) && instruction.Opcode is
            IrOpcode.Add or IrOpcode.Subtract or IrOpcode.Multiply or IrOpcode.Divide or IrOpcode.Modulo or
            IrOpcode.BitwiseAnd or IrOpcode.BitwiseOr or IrOpcode.BitwiseNot or IrOpcode.Negate or
            IrOpcode.ShiftLeft or IrOpcode.ShiftRight or IrOpcode.Equal or IrOpcode.LessThan or
            IrOpcode.ArithmeticShift or IrOpcode.LogicalShift or
            IrOpcode.LessThanOrEqual or IrOpcode.GreaterThan or IrOpcode.GreaterThanOrEqual or IrOpcode.BitTest or
            IrOpcode.LoadByte or IrOpcode.LoadWord or IrOpcode.LoadProperty or IrOpcode.LoadPropertyAddress or
            IrOpcode.LoadNextProperty or IrOpcode.LoadPropertySize or IrOpcode.LoadParent or IrOpcode.LoadChild or
            IrOpcode.LoadSibling;

        /// <summary>
        /// Determines whether an instruction is a call whose effect summary shows it reads memory without
        /// writing and has no effects beyond control flow.
        /// </summary>
        /// <param name="instruction">The instruction to check.</param>
        /// <returns><see langword="true"/> if the instruction is a read-only call; otherwise, <see langword="false"/>.</returns>
        private static bool IsReadOnlyCall(IrInstruction instruction) =>
            instruction.Effect == IrEffect.Call && instruction.Result != null &&
            instruction.CallSummary is { IsComplete: true } summary &&
            summary.GetWrittenRegions() == IrMemoryRegion.None &&
            summary.GetUnknownWrittenRegions() == IrMemoryRegion.None &&
            summary.GetEffects().All(effect => effect == IrEffect.Control);

        private static bool IsMemoryRead(IrOpcode opcode) => opcode is IrOpcode.LoadByte or IrOpcode.LoadWord or
            IrOpcode.LoadProperty or IrOpcode.LoadPropertyAddress or IrOpcode.LoadNextProperty or
            IrOpcode.LoadPropertySize or IrOpcode.LoadParent or IrOpcode.LoadChild or IrOpcode.LoadSibling;

        private static bool IsCommutative(IrOpcode opcode) => opcode is IrOpcode.Add or IrOpcode.Multiply or
            IrOpcode.BitwiseAnd or IrOpcode.BitwiseOr or IrOpcode.Equal;

        private static string OperandKey(IrValue value) => value.Constant is int constant
            ? $"C{constant}"
            : $"V{value.Id}";

        /// <summary>
        /// Replaces all uses of the given values throughout the routine with their replacements.
        /// </summary>
        /// <param name="routine">The routine to transform.</param>
        /// <param name="replacements">The value replacement map.</param>
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
