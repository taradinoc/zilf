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
using System.Collections.ObjectModel;
using System.Linq;

namespace Zilf.Emit.Intermediate
{
    internal readonly record struct IrOptimizationStat(string Name, int Count);

    internal interface IIrOptimizationCostPolicy
    {
        bool ShouldPromoteStackResult(IrInstruction instruction, IrInstruction repeatedInstruction,
            bool reusesExistingTemporary);

        bool ShouldRewriteAsCopy(IrInstruction instruction, IrValue source);

        bool ShouldHoist(IrInstruction instruction, bool requiresNewTemporary);

        bool ShouldPlacePartialRedundancy(IrInstruction instruction, int insertedEdges);

        bool ShouldCoalescePhi(int removedCopies, int addedCopies);
    }

    internal sealed class NeutralIrOptimizationCostPolicy : IIrOptimizationCostPolicy
    {
        public static NeutralIrOptimizationCostPolicy Instance { get; } = new();

        public bool ShouldPromoteStackResult(IrInstruction instruction, IrInstruction repeatedInstruction,
            bool reusesExistingTemporary) => true;

        public bool ShouldRewriteAsCopy(IrInstruction instruction, IrValue source) => true;

        public bool ShouldHoist(IrInstruction instruction, bool requiresNewTemporary) => true;

        public bool ShouldPlacePartialRedundancy(IrInstruction instruction, int insertedEdges) => insertedEdges == 1;

        public bool ShouldCoalescePhi(int removedCopies, int addedCopies) => removedCopies > addedCopies;
    }

    internal sealed class IrLoweringOperation
    {
        public IrLoweringOperation(Action<IReadOnlyList<IOperand>> emit, IVariable? resultHome = null,
            bool isStackResult = false, Action<IReadOnlyList<IOperand>, IVariable?>? emitTo = null)
        {
            Emit = emit;
            ResultHome = resultHome;
            IsStackResult = isStackResult;
            EmitTo = emitTo;
        }

        public Action<IReadOnlyList<IOperand>> Emit { get; }

        public IVariable? ResultHome { get; set; }

        public bool IsStackResult { get; set; }

        public bool StackEscapes { get; set; }

        public bool RequiredHome { get; set; }

        public bool IsMaterialization { get; set; }

        public Action<IReadOnlyList<IOperand>, IVariable?>? EmitTo { get; }

        /// <summary>
        /// Re-emits this lowering operation with the given operands, routing through the emit-to callback when
        /// one is present.
        /// </summary>
        /// <param name="operands">The resolved operands for the operation.</param>
        public void Replay(IReadOnlyList<IOperand> operands)
        {
            if (EmitTo != null)
                EmitTo(operands, ResultHome);
            else
                Emit(operands);
        }
    }

    internal enum IrEffect
    {
        None,
        ReadMemory,
        WriteMemory,
        Call,
        InputOutput,
        Nondeterministic,
        Stack,
        Control,
        Opaque,
    }

    [Flags]
    internal enum IrMemoryRegion
    {
        None = 0,
        Globals = 1,
        Tables = 2,
        Properties = 4,
        ObjectTree = 8,
        Attributes = 16,
        All = Globals | Tables | Properties | ObjectTree | Attributes,
    }

    internal sealed record IrMemoryIdentity(IrMemoryRegion Region, object Key, int? Offset = null, int? Length = null)
    {
        public bool IsParameterRelative => Key is IrParameterMemoryKey ||
            Key is IrObjectMemberKey { Object: IrParameterMemoryKey };

        /// <summary>
        /// Determines whether this memory identity may overlap another.
        /// </summary>
        /// <param name="other">The identity to compare against.</param>
        /// <returns><see langword="true"/> if the two identities may refer to overlapping memory; otherwise, <see langword="false"/>.</returns>
        public bool MayAlias(IrMemoryIdentity other)
        {
            if (Region != other.Region || !Equals(Key, other.Key))
                return false;
            if (Offset == null || Length == null || other.Offset == null || other.Length == null)
                return true;
            return (long)Offset.Value < (long)other.Offset.Value + other.Length.Value &&
                (long)other.Offset.Value < (long)Offset.Value + Length.Value;
        }
    }

    internal readonly record struct IrObjectMemberKey(object Object, object Member);

    internal sealed record IrParameterMemoryKey(IrRoutineEffectSummary Owner, int Index);

    internal readonly record struct IrMemoryBinding(object Key, int Offset = 0);

    internal sealed record IrSummaryCall(IrRoutineEffectSummary Callee,
        IReadOnlyList<IrMemoryBinding?> Arguments);

    internal sealed class IrRoutineEffectSummary
    {
        private readonly List<IrSummaryCall> callees = [];
        private readonly HashSet<IrMemoryIdentity> directWriteIdentities = [];
        private readonly HashSet<IrMemoryIdentity> directReadIdentities = [];
        private readonly HashSet<IrEffect> directEffects = [];
        private IrMemoryRegion directWrites;
        private IrMemoryRegion directUnknownWrites;
        private IrMemoryRegion directReads;
        private IrMemoryRegion directUnknownReads;
        private IrMemoryRegion? closedWrites;
        private IrMemoryRegion? closedUnknownWrites;
        private IrMemoryRegion? closedReads;
        private IrMemoryRegion? closedUnknownReads;
        private HashSet<IrMemoryIdentity>? closedWriteIdentities;
        private HashSet<IrMemoryIdentity>? closedReadIdentities;
        private HashSet<IrEffect>? closedEffects;

        public IrMemoryRegion DirectWrites
        {
            get => directWrites;
            set
            {
                directWrites = value;
                directUnknownWrites |= value;
            }
        }

        public bool IsComplete { get; set; }

        public bool HasArgumentBindings => callees.Any(call => call.Arguments.Count > 0);

        private bool IsBoundView => directWrites == IrMemoryRegion.None && directReads == IrMemoryRegion.None &&
            directEffects.Count == 0 && callees.Count == 1 && callees[0].Arguments.Count > 0;

        /// <summary>
        /// Records a call to the given callee, unwrapping a bound view callee into its underlying callee when no
        /// arguments are supplied.
        /// </summary>
        /// <param name="callee">The callee's effect summary.</param>
        /// <param name="arguments">The memory bindings for the call arguments, or <see langword="null"/>.</param>
        public void AddCallee(IrRoutineEffectSummary callee, IReadOnlyList<IrMemoryBinding?>? arguments = null)
        {
            if (arguments == null && callee.IsBoundView)
                callees.Add(new IrSummaryCall(callee.callees[0].Callee, []));
            else
                callees.Add(new IrSummaryCall(callee, arguments ?? []));
        }

        /// <summary>
        /// Precomputes the closed effect sets for this summary when it is a bound view of a single call,
        /// binding the callee's identities to this view's arguments.
        /// </summary>
        public void CloseBoundView()
        {
            if (!IsBoundView)
                return;
            var call = callees[0];
            closedWrites = call.Callee.GetWrittenRegions();
            closedUnknownWrites = BindUnknownRegions(call.Callee.GetUnknownWrittenRegions(),
                call.Callee.GetWrittenIdentities(), call, false);
            closedReads = call.Callee.GetReadRegions();
            closedUnknownReads = BindUnknownRegions(call.Callee.GetUnknownReadRegions(),
                call.Callee.GetReadIdentities(), call, false);
            closedWriteIdentities = [.. BindIdentities(call.Callee.GetWrittenIdentities(), call, false)];
            closedReadIdentities = [.. BindIdentities(call.Callee.GetReadIdentities(), call, false)];
            closedEffects = [.. call.Callee.GetEffects()];
        }

        public IrParameterMemoryKey GetParameterKey(int index) => new(this, index);

        /// <summary>
        /// Records that this summary directly writes the given memory regions, either as an unknown write or as a
        /// write to a specific identity.
        /// </summary>
        /// <param name="regions">The memory regions written.</param>
        /// <param name="identity">The written identity, or <see langword="null"/> if the exact identity is unknown.</param>
        public void AddWrite(IrMemoryRegion regions, IrMemoryIdentity? identity = null)
        {
            directWrites |= regions;
            if (identity == null)
                directUnknownWrites |= regions;
            else
                directWriteIdentities.Add(identity);
        }

        /// <summary>
        /// Records that this summary directly reads the given memory regions, either as an unknown read or as a
        /// read from a specific identity.
        /// </summary>
        /// <param name="regions">The memory regions read.</param>
        /// <param name="identity">The read identity, or <see langword="null"/> if the exact identity is unknown.</param>
        public void AddRead(IrMemoryRegion regions, IrMemoryIdentity? identity = null)
        {
            directReads |= regions;
            if (identity == null)
                directUnknownReads |= regions;
            else
                directReadIdentities.Add(identity);
        }

        public void AddEffect(IrEffect effect) => directEffects.Add(effect);

        /// <summary>
        /// Returns the closed set of written memory regions, computing it on demand if it has not been closed.
        /// </summary>
        /// <returns>The union of the memory regions written by this summary and its callees.</returns>
        public IrMemoryRegion GetWrittenRegions() => closedWrites ?? GetWrittenRegions([]);

        /// <summary>
        /// Returns the closed set of unknown written memory regions, computing it on demand if it has not been
        /// closed.
        /// </summary>
        /// <returns>The union of the unknown written regions of this summary and its callees.</returns>
        public IrMemoryRegion GetUnknownWrittenRegions() => closedUnknownWrites ?? GetUnknownWrittenRegions([]);

        /// <summary>
        /// Returns the set of memory identities written by this summary and its callees.
        /// </summary>
        /// <returns>The written memory identities.</returns>
        public IReadOnlySet<IrMemoryIdentity> GetWrittenIdentities()
        {
            if (closedWriteIdentities != null)
                return closedWriteIdentities;
            var result = new HashSet<IrMemoryIdentity>();
            CollectWrittenIdentities(result, []);
            return result;
        }

        /// <summary>
        /// Returns the closed set of read memory regions, computing it on demand if it has not been closed.
        /// </summary>
        /// <returns>The union of the memory regions read by this summary and its callees.</returns>
        public IrMemoryRegion GetReadRegions() => closedReads ?? CollectRegions(summary => summary.directReads, []);

        /// <summary>
        /// Returns the closed set of unknown read memory regions, computing it on demand if it has not been
        /// closed.
        /// </summary>
        /// <returns>The union of the unknown read regions of this summary and its callees.</returns>
        public IrMemoryRegion GetUnknownReadRegions() =>
            closedUnknownReads ?? CollectRegions(summary => summary.directUnknownReads, []);

        /// <summary>
        /// Returns the set of memory identities read by this summary and its callees.
        /// </summary>
        /// <returns>The read memory identities.</returns>
        public IReadOnlySet<IrMemoryIdentity> GetReadIdentities()
        {
            if (closedReadIdentities != null)
                return closedReadIdentities;
            var result = new HashSet<IrMemoryIdentity>();
            CollectSet(result, summary => summary.directReadIdentities, []);
            return result;
        }

        /// <summary>
        /// Returns the set of effects produced by this summary and its callees.
        /// </summary>
        /// <returns>The produced effects.</returns>
        public IReadOnlySet<IrEffect> GetEffects()
        {
            if (closedEffects != null)
                return closedEffects;
            var result = new HashSet<IrEffect>();
            CollectSet(result, summary => summary.directEffects, []);
            return result;
        }

        /// <summary>
        /// Closes the effect summaries for the given routines by iteratively propagating each callee's effects
        /// into its callers until a fixed point is reached, widening any summary that grows too large or cycles.
        /// </summary>
        /// <param name="summaries">The summaries to close.</param>
        public static void Close(IEnumerable<IrRoutineEffectSummary> summaries)
        {
            var discovered = new HashSet<IrRoutineEffectSummary>();
            var pending = new Stack<IrRoutineEffectSummary>(summaries);
            while (pending.TryPop(out var summary))
            {
                if (!discovered.Add(summary))
                    continue;
                foreach (var call in summary.callees)
                    pending.Push(call.Callee);
            }
            var all = discovered.ToArray();
            foreach (var summary in all)
            {
                summary.closedWrites = summary.IsComplete ? summary.directWrites : IrMemoryRegion.All;
                summary.closedUnknownWrites = summary.IsComplete ? summary.directUnknownWrites : IrMemoryRegion.All;
                summary.closedReads = summary.IsComplete ? summary.directReads : IrMemoryRegion.All;
                summary.closedUnknownReads = summary.IsComplete ? summary.directUnknownReads : IrMemoryRegion.All;
                summary.closedWriteIdentities = [.. summary.directWriteIdentities];
                summary.closedReadIdentities = [.. summary.directReadIdentities];
                summary.closedEffects = [.. summary.directEffects];
            }
            var cyclicCalls = FindCyclicCalls(all);
            var widened = new HashSet<IrRoutineEffectSummary>();
            var changed = true;
            var iteration = 0;
            while (changed)
            {
                changed = false;
                foreach (var summary in all.Where(summary => summary.IsComplete))
                {
                    var summaryChanged = false;
                    foreach (var call in summary.callees)
                    {
                        var callee = call.Callee;
                        var cyclic = cyclicCalls.Contains(call);
                        summaryChanged |= Union(ref summary.closedWrites, callee.GetWrittenRegions());
                        summaryChanged |= Union(ref summary.closedUnknownWrites,
                            BindUnknownRegions(callee.GetUnknownWrittenRegions(), callee.GetWrittenIdentities(), call,
                                cyclic));
                        summaryChanged |= Union(ref summary.closedReads, callee.GetReadRegions());
                        summaryChanged |= Union(ref summary.closedUnknownReads,
                            BindUnknownRegions(callee.GetUnknownReadRegions(), callee.GetReadIdentities(), call,
                                cyclic));
                        if (!widened.Contains(summary))
                        {
                            summaryChanged |= Union(summary.closedWriteIdentities!,
                                BindIdentities(callee.GetWrittenIdentities(), call, cyclic));
                            summaryChanged |= Union(summary.closedReadIdentities!,
                                BindIdentities(callee.GetReadIdentities(), call, cyclic));
                        }
                        summaryChanged |= Union(summary.closedEffects!, callee.GetEffects());
                    }
                    changed |= summaryChanged;
                    if ((iteration >= 32 || summary.closedWriteIdentities!.Count +
                            summary.closedReadIdentities!.Count > 32) && summaryChanged && widened.Add(summary))
                    {
                        summary.closedUnknownWrites |= summary.closedWrites;
                        summary.closedUnknownReads |= summary.closedReads;
                        summary.closedWriteIdentities!.Clear();
                        summary.closedReadIdentities!.Clear();
                    }
                }
                iteration++;
            }
        }

        /// <summary>
        /// Finds the call edges that participate in a recursion cycle using Tarjan's strongly connected component
        /// algorithm.
        /// </summary>
        /// <param name="summaries">The summaries whose call graph should be analyzed.</param>
        /// <returns>The set of call edges that lie within a cycle.</returns>
        private static HashSet<IrSummaryCall> FindCyclicCalls(IEnumerable<IrRoutineEffectSummary> summaries)
        {
            var nextIndex = 0;
            var nextComponent = 0;
            var indices = new Dictionary<IrRoutineEffectSummary, int>();
            var lowLinks = new Dictionary<IrRoutineEffectSummary, int>();
            var components = new Dictionary<IrRoutineEffectSummary, int>();
            var stack = new Stack<IrRoutineEffectSummary>();
            var onStack = new HashSet<IrRoutineEffectSummary>();

            void Visit(IrRoutineEffectSummary summary)
            {
                indices[summary] = nextIndex;
                lowLinks[summary] = nextIndex++;
                stack.Push(summary);
                onStack.Add(summary);
                foreach (var call in summary.callees)
                {
                    var callee = call.Callee;
                    if (!indices.TryGetValue(callee, out int value))
                    {
                        Visit(callee);
                        lowLinks[summary] = Math.Min(lowLinks[summary], lowLinks[callee]);
                    }
                    else if (onStack.Contains(callee))
                        lowLinks[summary] = Math.Min(lowLinks[summary], value);
                }
                if (lowLinks[summary] != indices[summary])
                    return;
                IrRoutineEffectSummary member;
                do
                {
                    member = stack.Pop();
                    onStack.Remove(member);
                    components[member] = nextComponent;
                }
                while (!ReferenceEquals(member, summary));
                nextComponent++;
            }

            foreach (var summary in summaries)
            {
                if (!indices.ContainsKey(summary))
                    Visit(summary);
            }
            return summaries.SelectMany(summary => summary.callees.Select(call => (summary, call)))
                .Where(edge => components[edge.summary] == components[edge.call.Callee] &&
                    (!ReferenceEquals(edge.summary, edge.call.Callee) ||
                        edge.summary.callees.Any(call => ReferenceEquals(call.Callee, edge.summary))))
                .Select(edge => edge.call)
                .ToHashSet();
        }

        /// <summary>
        /// Binds a set of callee-relative memory identities to the arguments of a call site, dropping any that
        /// cannot be resolved.
        /// </summary>
        /// <param name="identities">The identities to bind.</param>
        /// <param name="call">The call whose arguments supply the bindings.</param>
        /// <param name="cyclic">Whether the call participates in a cycle.</param>
        /// <returns>The resolved identities.</returns>
        private static IrMemoryIdentity[] BindIdentities(IEnumerable<IrMemoryIdentity> identities,
            IrSummaryCall call, bool cyclic) => identities
                .Select(identity => BindIdentity(identity, call, cyclic)).OfType<IrMemoryIdentity>().ToArray();

        /// <summary>
        /// Widens a callee's unknown-region set by adding the regions of any parameter-relative identity that
        /// cannot be resolved to a concrete argument.
        /// </summary>
        /// <param name="unknown">The callee's unknown regions.</param>
        /// <param name="identities">The callee's concrete identities.</param>
        /// <param name="call">The call whose arguments supply the bindings.</param>
        /// <param name="cyclic">Whether the call participates in a cycle.</param>
        /// <returns>The widened unknown-region set.</returns>
        private static IrMemoryRegion BindUnknownRegions(IrMemoryRegion unknown,
            IEnumerable<IrMemoryIdentity> identities, IrSummaryCall call, bool cyclic)
        {
            foreach (var identity in identities)
            {
                if (ContainsParameter(identity.Key) && BindIdentity(identity, call, cyclic) == null)
                    unknown |= identity.Region;
            }
            return unknown;
        }

        /// <summary>
        /// Determines whether a memory identity key references a call parameter, either directly or through an
        /// object member key.
        /// </summary>
        /// <param name="key">The identity key to check.</param>
        /// <returns><see langword="true"/> if the key references a parameter; otherwise, <see langword="false"/>.</returns>
        private static bool ContainsParameter(object key) => key switch
        {
            IrParameterMemoryKey => true,
            IrObjectMemberKey member => ContainsParameter(member.Object),
            _ => false,
        };

        /// <summary>
        /// Binds a single callee-relative memory identity to a call site, replacing parameter keys with their
        /// argument bindings and widening offsets when the call is cyclic.
        /// </summary>
        /// <param name="identity">The identity to bind.</param>
        /// <param name="call">The call whose arguments supply the bindings.</param>
        /// <param name="cyclic">Whether the call participates in a cycle.</param>
        /// <returns>The bound identity, or <see langword="null"/> if the parameter cannot be resolved.</returns>
        private static IrMemoryIdentity? BindIdentity(IrMemoryIdentity identity, IrSummaryCall call,
            bool cyclic = false)
        {
            if (identity.Key is IrParameterMemoryKey parameter && ReferenceEquals(parameter.Owner, call.Callee))
            {
                if (parameter.Index >= call.Arguments.Count || call.Arguments[parameter.Index] is not { } binding)
                    return null;
                long? offset = identity.Offset == null ? null : (long)binding.Offset + identity.Offset.Value;
                var bound = offset is < int.MinValue or > int.MaxValue
                    ? null
                    : identity with { Key = binding.Key, Offset = (int?)offset };
                return cyclic && bound != null ? bound with { Offset = null, Length = null } : bound;
            }
            if (identity.Key is IrObjectMemberKey { Object: IrParameterMemoryKey memberParameter } member &&
                ReferenceEquals(memberParameter.Owner, call.Callee))
            {
                if (memberParameter.Index >= call.Arguments.Count ||
                    call.Arguments[memberParameter.Index] is not { } binding)
                    return null;
                return identity with { Key = member with { Object = binding.Key } };
            }
            return identity;
        }

        private static bool Union(ref IrMemoryRegion? target, IrMemoryRegion value)
        {
            var previous = target!.Value;
            target = previous | value;
            return target != previous;
        }

        private static bool Union<T>(HashSet<T> target, IEnumerable<T> values)
        {
            var previous = target.Count;
            target.UnionWith(values);
            return target.Count != previous;
        }

        /// <summary>
        /// Recursively collects a region value from this summary and all of its callees.
        /// </summary>
        /// <param name="selector">A function selecting the region value from each summary.</param>
        /// <param name="active">The set of summaries on the current recursion path.</param>
        /// <returns>The union of the selected regions.</returns>
        private IrMemoryRegion CollectRegions(Func<IrRoutineEffectSummary, IrMemoryRegion> selector,
            HashSet<IrRoutineEffectSummary> active)
        {
            if (!IsComplete)
                return IrMemoryRegion.All;
            if (!active.Add(this))
                return selector(this);
            var result = selector(this);
            foreach (var call in callees)
                result |= call.Callee.CollectRegions(selector, active);
            active.Remove(this);
            return result;
        }

        /// <summary>
        /// Recursively collects a set of items from this summary and all of its callees.
        /// </summary>
        /// <typeparam name="T">The type of the collected items.</typeparam>
        /// <param name="result">The set receiving the collected items.</param>
        /// <param name="selector">A function selecting the items from each summary.</param>
        /// <param name="active">The set of summaries on the current recursion path.</param>
        private void CollectSet<T>(HashSet<T> result, Func<IrRoutineEffectSummary, IEnumerable<T>> selector,
            HashSet<IrRoutineEffectSummary> active)
        {
            if (!IsComplete || !active.Add(this))
                return;
            result.UnionWith(selector(this));
            foreach (var call in callees)
                call.Callee.CollectSet(result, selector, active);
            active.Remove(this);
        }

        /// <summary>
        /// Recursively computes the written memory regions of this summary and its callees.
        /// </summary>
        /// <param name="active">The set of summaries on the current recursion path.</param>
        /// <returns>The union of the written regions.</returns>
        private IrMemoryRegion GetWrittenRegions(HashSet<IrRoutineEffectSummary> active)
        {
            if (!IsComplete)
                return IrMemoryRegion.All;
            if (!active.Add(this))
                return DirectWrites;
            var result = DirectWrites;
            foreach (var call in callees)
                result |= call.Callee.GetWrittenRegions(active);
            active.Remove(this);
            return result;
        }

        /// <summary>
        /// Recursively computes the unknown written memory regions of this summary and its callees.
        /// </summary>
        /// <param name="active">The set of summaries on the current recursion path.</param>
        /// <returns>The union of the unknown written regions.</returns>
        private IrMemoryRegion GetUnknownWrittenRegions(HashSet<IrRoutineEffectSummary> active)
        {
            if (!IsComplete)
                return IrMemoryRegion.All;
            if (!active.Add(this))
                return directUnknownWrites;
            var result = directUnknownWrites;
            foreach (var call in callees)
                result |= call.Callee.GetUnknownWrittenRegions(active);
            active.Remove(this);
            return result;
        }

        /// <summary>
        /// Recursively collects the written memory identities of this summary and its callees.
        /// </summary>
        /// <param name="result">The set receiving the collected identities.</param>
        /// <param name="active">The set of summaries on the current recursion path.</param>
        private void CollectWrittenIdentities(HashSet<IrMemoryIdentity> result,
            HashSet<IrRoutineEffectSummary> active)
        {
            if (!IsComplete || !active.Add(this))
                return;
            result.UnionWith(directWriteIdentities);
            foreach (var call in callees)
                call.Callee.CollectWrittenIdentities(result, active);
            active.Remove(this);
        }
    }

    internal enum IrOpcode
    {
        Constant,
        Phi,
        Copy,
        Add,
        Subtract,
        Multiply,
        Divide,
        Modulo,
        BitwiseAnd,
        BitwiseOr,
        BitwiseNot,
        Negate,
        ShiftLeft,
        ShiftRight,
        ArithmeticShift,
        LogicalShift,
        Equal,
        LessThan,
        LessThanOrEqual,
        GreaterThan,
        GreaterThanOrEqual,
        BitTest,
        Inside,
        HasAttribute,
        ArgumentProvided,
        LoadByte,
        LoadWord,
        LoadProperty,
        LoadPropertyAddress,
        LoadNextProperty,
        LoadPropertySize,
        LoadParent,
        LoadChild,
        LoadSibling,
        ScanTable,
        TargetOperation,
    }

    internal sealed class IrValue
    {
        internal IrValue(int id, int? constant = null, bool mutableExternal = false, bool knownNonzero = false,
            IrMemoryIdentity? memoryIdentity = null)
        {
            Id = id;
            Constant = constant;
            MutableExternal = mutableExternal;
            KnownNonzero = knownNonzero || constant is not null and not 0;
            MemoryIdentity = memoryIdentity;
        }

        public int Id { get; }

        public int? Constant { get; }

        public bool MutableExternal { get; }

        public bool KnownNonzero { get; }

        public IrMemoryIdentity? MemoryIdentity { get; set; }

        public object? StableIdentity { get; set; }

        public IReadOnlySet<IrRoutineEffectSummary>? RoutineTargets { get; set; }

        public IOperand? PhysicalHome { get; set; }

        public override string ToString() => Constant is int value ? value.ToString() : $"%{Id}";
    }

    internal sealed class IrInstruction
    {
        private readonly List<IrValue> operands;

        internal IrInstruction(IrOpcode opcode, IrValue? result, IEnumerable<IrValue> operands,
            IrEffect effect = IrEffect.None, object? payload = null, IrMemoryRegion readRegions = IrMemoryRegion.None,
            IrMemoryRegion writeRegions = IrMemoryRegion.None, IrRoutineEffectSummary? callSummary = null,
            IrMemoryIdentity? readIdentity = null, IrMemoryIdentity? writeIdentity = null)
        {
            Opcode = opcode;
            Result = result;
            this.operands = [.. operands];
            Effect = effect;
            Payload = payload;
            ReadRegions = readRegions;
            WriteRegions = writeRegions;
            CallSummary = callSummary;
            ReadIdentity = readIdentity;
            WriteIdentity = writeIdentity;
        }

        public IrOpcode Opcode { get; set; }

        public IrValue? Result { get; }

        public IList<IrValue> Operands => operands;

        public IrEffect Effect { get; }

        public object? Payload { get; set; }

        public IrMemoryRegion ReadRegions { get; }

        public IrMemoryRegion WriteRegions { get; }

        public IrRoutineEffectSummary? CallSummary { get; set; }

        public IReadOnlyList<IrMemoryBinding?>? CallBindings { get; set; }

        public IrMemoryIdentity? ReadIdentity { get; set; }

        public IrMemoryIdentity? WriteIdentity { get; set; }

        public IrValue? WrittenValue { get; set; }

        public bool IsPure => Effect == IrEffect.None;

        /// <summary>
        /// Replaces every occurrence of one operand value with another in this instruction.
        /// </summary>
        /// <param name="oldValue">The value to replace.</param>
        /// <param name="newValue">The replacement value.</param>
        public void ReplaceOperand(IrValue oldValue, IrValue newValue)
        {
            for (var i = 0; i < operands.Count; i++)
            {
                if (ReferenceEquals(operands[i], oldValue))
                    operands[i] = newValue;
            }
        }
    }

    internal sealed class IrPhi
    {
        public IrPhi(IVariable variable) => Variable = variable;

        public IVariable Variable { get; }

        public IDictionary<IrBlock, IrValue> Incoming { get; } = new Dictionary<IrBlock, IrValue>();
    }

    internal abstract record IrTerminator
    {
        public sealed record Jump(IrBlock Target, Action<IrBlock>? EmitJump = null,
            IrInstruction? Instruction = null) : IrTerminator;

        public sealed record Branch(IrValue Condition, IrBlock WhenTrue, IrBlock WhenFalse,
            Action<IrBlock>? EmitJump = null, IrInstruction? Instruction = null,
            IrBlock? ExplicitTarget = null) : IrTerminator;

        public sealed record Return(IrValue? Value) : IrTerminator;
    }

    internal sealed class IrBlock
    {
        private readonly List<IrInstruction> instructions = [];
        private readonly List<IrBlock> predecessors = [];

        internal IrBlock(int id) => Id = id;

        public int Id { get; }

        public IList<IrInstruction> Instructions => instructions;

        public IReadOnlyList<IrBlock> Predecessors => predecessors;

        public IrTerminator? Terminator { get; set; }

        /// <summary>
        /// Adds a predecessor block, ignoring duplicates.
        /// </summary>
        /// <param name="block">The predecessor to add.</param>
        internal void AddPredecessor(IrBlock block)
        {
            if (!predecessors.Contains(block))
                predecessors.Add(block);
        }

        internal void ClearPredecessors() => predecessors.Clear();

        public override string ToString() => $"B{Id}";
    }

    internal sealed class RoutineIr
    {
        private readonly List<IrBlock> blocks = [];
        private int nextBlockId;
        private int nextValueId;

        public RoutineIr()
        {
            Entry = CreateBlock();
        }

        public IrBlock Entry { get; }

        public IList<IrBlock> Blocks => blocks;

        public IrBlock CreateBlock()
        {
            var block = new IrBlock(nextBlockId++);
            blocks.Add(block);
            return block;
        }

        public IrValue CreateValue() => new(nextValueId++);

        public IrValue CreateExternalValue(bool mutable, bool knownNonzero = false,
            IrMemoryIdentity? memoryIdentity = null) =>
            new(nextValueId++, mutableExternal: mutable, knownNonzero: knownNonzero, memoryIdentity: memoryIdentity);

        public IrValue CreateConstant(int value) => new(nextValueId++, value);

        /// <summary>
        /// Appends a new instruction to the given block, creating a result value when requested.
        /// </summary>
        /// <param name="block">The block to append to.</param>
        /// <param name="opcode">The instruction opcode.</param>
        /// <param name="operands">The instruction operand values.</param>
        /// <param name="effect">The effect the instruction has on the routine.</param>
        /// <param name="payload">An optional payload describing how the instruction is lowered.</param>
        /// <param name="hasResult">Whether to allocate a result value for the instruction.</param>
        /// <param name="readRegions">The memory regions the instruction may read.</param>
        /// <param name="writeRegions">The memory regions the instruction may write.</param>
        /// <param name="callSummary">The effect summary of the called routine, for call instructions.</param>
        /// <param name="readIdentity">The memory identity read by the instruction, or <see langword="null"/>.</param>
        /// <param name="writeIdentity">The memory identity written by the instruction, or <see langword="null"/>.</param>
        /// <returns>The appended instruction.</returns>
        public IrInstruction Append(IrBlock block, IrOpcode opcode, IEnumerable<IrValue> operands,
            IrEffect effect = IrEffect.None, object? payload = null, bool hasResult = true,
            IrMemoryRegion readRegions = IrMemoryRegion.None, IrMemoryRegion writeRegions = IrMemoryRegion.None,
            IrRoutineEffectSummary? callSummary = null, IrMemoryIdentity? readIdentity = null,
            IrMemoryIdentity? writeIdentity = null)
        {
            var instruction = new IrInstruction(opcode, hasResult ? CreateValue() : null, operands, effect, payload,
                readRegions, writeRegions, callSummary, readIdentity, writeIdentity);
            block.Instructions.Add(instruction);
            return instruction;
        }

        /// <summary>
        /// Recomputes each block's predecessor list from the current terminators and refreshes the operand lists
        /// of phi instructions to match.
        /// </summary>
        public void RebuildPredecessors()
        {
            foreach (var block in blocks)
                block.ClearPredecessors();

            foreach (var block in blocks)
            {
                foreach (var successor in GetSuccessors(block))
                    successor.AddPredecessor(block);
            }

            foreach (var block in blocks)
            {
                foreach (var instruction in block.Instructions.Where(instruction =>
                    instruction.Opcode == IrOpcode.Phi && instruction.Payload is IrPhi))
                {
                    var phi = (IrPhi)instruction.Payload!;
                    instruction.Operands.Clear();
                    foreach (var predecessor in block.Predecessors)
                    {
                        if (phi.Incoming.TryGetValue(predecessor, out var value))
                            instruction.Operands.Add(value);
                    }
                }
            }
        }

        /// <summary>
        /// Promotes the given local variables to SSA form by inserting phi instructions where control flow joins
        /// and rewriting operand references to the current reaching value.
        /// </summary>
        /// <param name="variables">The locals to promote.</param>
        /// <returns>The number of phi instructions inserted.</returns>
        public int PromoteLocalsToSsa(IEnumerable<IVariable> variables)
        {
            RebuildPredecessors();
            var promotable = variables.ToHashSet();
            if (promotable.Count == 0)
                return 0;

            var definitions = blocks.SelectMany(block => block.Instructions)
                .Where(instruction => instruction.Result != null)
                .Select(instruction => instruction.Result!)
                .ToHashSet();
            var initialValues = new Dictionary<IVariable, IrValue>();
            foreach (var value in blocks.SelectMany(block => block.Instructions)
                .SelectMany(instruction => instruction.Operands).Where(value => !definitions.Contains(value)))
            {
                if (value.PhysicalHome is IVariable variable && promotable.Contains(variable))
                    initialValues.TryAdd(variable, value);
            }
            foreach (var variable in promotable.Where(variable => !initialValues.ContainsKey(variable)))
            {
                var value = CreateExternalValue(mutable: true);
                value.PhysicalHome = variable;
                initialValues.Add(variable, value);
            }

            var incoming = blocks.ToDictionary(block => block,
                _ => new Dictionary<IVariable, IrValue>(initialValues));
            var outgoing = blocks.ToDictionary(block => block,
                _ => new Dictionary<IVariable, IrValue>(initialValues));
            var phis = new Dictionary<(IrBlock Block, IVariable Variable), IrInstruction>();
            var pending = new Queue<IrBlock>(blocks);
            var queued = blocks.ToHashSet();
            while (pending.TryDequeue(out var block))
            {
                queued.Remove(block);
                var nextIncoming = new Dictionary<IVariable, IrValue>();
                foreach (var variable in promotable)
                {
                    var predecessorValues = block.Predecessors
                        .Select(predecessor => outgoing[predecessor][variable])
                        .Distinct()
                        .ToArray();
                    IrValue value;
                    if (ReferenceEquals(block, Entry) || predecessorValues.Length == 0)
                    {
                        value = initialValues[variable];
                    }
                    else if (predecessorValues.Length == 1)
                    {
                        value = predecessorValues[0];
                    }
                    else
                    {
                        var key = (block, variable);
                        if (!phis.TryGetValue(key, out var phi))
                        {
                            phi = new IrInstruction(IrOpcode.Phi, CreateValue(), [], payload: new IrPhi(variable));
                            phi.Result!.PhysicalHome = variable;
                            block.Instructions.Insert(0, phi);
                            phis.Add(key, phi);
                        }
                        value = phi.Result!;
                    }
                    nextIncoming[variable] = value;
                }

                var nextOutgoing = TransferLocalValues(block, nextIncoming, promotable);
                if (!LocalValuesEqual(incoming[block], nextIncoming))
                    incoming[block] = nextIncoming;
                if (!LocalValuesEqual(outgoing[block], nextOutgoing))
                {
                    outgoing[block] = nextOutgoing;
                    foreach (var successor in GetSuccessors(block))
                    {
                        if (queued.Add(successor))
                            pending.Enqueue(successor);
                    }
                }
            }

            foreach (var ((block, variable), phi) in phis)
            {
                phi.Operands.Clear();
                foreach (var predecessor in block.Predecessors)
                {
                    var value = outgoing[predecessor][variable];
                    ((IrPhi)phi.Payload!).Incoming[predecessor] = value;
                    phi.Operands.Add(value);
                }
            }

            foreach (var block in blocks)
            {
                var values = new Dictionary<IVariable, IrValue>(incoming[block]);
                foreach (var instruction in block.Instructions)
                {
                    if (instruction.Opcode != IrOpcode.Phi)
                    {
                        for (var i = 0; i < instruction.Operands.Count; i++)
                        {
                            var operand = instruction.Operands[i];
                            if (!definitions.Contains(operand) && operand.PhysicalHome is IVariable variable &&
                                promotable.Contains(variable))
                                instruction.Operands[i] = values[variable];
                        }
                    }
                    ApplyLocalDefinition(instruction, values, promotable);
                }
                block.Terminator = block.Terminator switch
                {
                    IrTerminator.Branch branch => branch with { Condition = RewriteLocal(branch.Condition, values,
                        definitions, promotable) },
                    IrTerminator.Return { Value: not null } ret => ret with { Value = RewriteLocal(ret.Value, values,
                        definitions, promotable) },
                    _ => block.Terminator,
                };
            }
            return phis.Count;
        }

        /// <summary>
        /// Computes the outgoing local-value map for a block by applying the block's definitions to the incoming
        /// map.
        /// </summary>
        /// <param name="block">The block to process.</param>
        /// <param name="incoming">The incoming local-value map.</param>
        /// <param name="promotable">The set of locals being promoted.</param>
        /// <returns>The outgoing local-value map.</returns>
        private static Dictionary<IVariable, IrValue> TransferLocalValues(IrBlock block,
            IReadOnlyDictionary<IVariable, IrValue> incoming, IReadOnlySet<IVariable> promotable)
        {
            var result = new Dictionary<IVariable, IrValue>(incoming);
            foreach (var instruction in block.Instructions)
                ApplyLocalDefinition(instruction, result, promotable);
            return result;
        }

        /// <summary>
        /// Updates a local-value map with the value produced by a single instruction, if the instruction defines
        /// one of the promoted locals.
        /// </summary>
        /// <param name="instruction">The instruction to inspect.</param>
        /// <param name="values">The local-value map to update.</param>
        /// <param name="promotable">The set of locals being promoted.</param>
        private static void ApplyLocalDefinition(IrInstruction instruction, IDictionary<IVariable, IrValue> values,
            IReadOnlySet<IVariable> promotable)
        {
            if (instruction.Opcode == IrOpcode.Phi && instruction.Payload is IrPhi phi)
            {
                values[phi.Variable] = instruction.Result!;
                return;
            }
            if (instruction.Payload is not IrLoweringOperation { ResultHome: IVariable variable } lowering ||
                !promotable.Contains(variable))
                return;
            if (instruction.Result != null)
                values[variable] = instruction.Result;
            else if (lowering.IsMaterialization && instruction.Operands.Count == 1)
                values[variable] = instruction.Operands[0];
        }

        /// <summary>
        /// Rewrites a reference to a promoted local into its current SSA value, leaving other values unchanged.
        /// </summary>
        /// <param name="value">The value to rewrite.</param>
        /// <param name="values">The current local-value map.</param>
        /// <param name="definitions">The set of values that are defined by instructions.</param>
        /// <param name="promotable">The set of locals being promoted.</param>
        /// <returns>The rewritten value.</returns>
        private static IrValue RewriteLocal(IrValue value, IReadOnlyDictionary<IVariable, IrValue> values,
            IReadOnlySet<IrValue> definitions, IReadOnlySet<IVariable> promotable) =>
            !definitions.Contains(value) && value.PhysicalHome is IVariable variable && promotable.Contains(variable)
                ? values[variable]
                : value;

        private static bool LocalValuesEqual(IReadOnlyDictionary<IVariable, IrValue> left,
            IReadOnlyDictionary<IVariable, IrValue> right) =>
            left.Count == right.Count && left.All(pair => right.TryGetValue(pair.Key, out var value) &&
                ReferenceEquals(pair.Value, value));

        /// <summary>
        /// Returns the successor blocks of a block based on its terminator.
        /// </summary>
        /// <param name="block">The block whose successors are requested.</param>
        /// <returns>The successor blocks.</returns>
        public static IEnumerable<IrBlock> GetSuccessors(IrBlock block) => block.Terminator switch
        {
            IrTerminator.Jump jump => [jump.Target],
            IrTerminator.Branch branch when ReferenceEquals(branch.WhenTrue, branch.WhenFalse) => [branch.WhenTrue],
            IrTerminator.Branch branch => [branch.WhenTrue, branch.WhenFalse],
            _ => [],
        };

        /// <summary>
        /// Validates the structure of the routine, throwing if any invariant is violated.
        /// </summary>
        /// <exception cref="InvalidOperationException">Thrown when the routine is malformed.</exception>
        public void Verify()
        {
            if (!blocks.Contains(Entry))
                throw new InvalidOperationException("The entry block is not part of the routine.");

            RebuildPredecessors();
            var defined = new HashSet<IrValue>();
            foreach (var block in blocks)
            {
                if (block.Terminator == null)
                    throw new InvalidOperationException($"Block {block} has no terminator.");

                foreach (var instruction in block.Instructions)
                {
                    if (instruction.Opcode == IrOpcode.Phi &&
                        (instruction.Payload is not IrPhi || instruction.Operands.Count != block.Predecessors.Count))
                        throw new InvalidOperationException($"Phi in {block} does not match its predecessors.");
                    if (instruction.Result != null && !defined.Add(instruction.Result))
                        throw new InvalidOperationException($"Value {instruction.Result} is defined more than once.");
                }

                foreach (var successor in GetSuccessors(block))
                {
                    if (!blocks.Contains(successor))
                        throw new InvalidOperationException($"Block {block} targets a block outside the routine.");
                }
            }
        }
    }
}
