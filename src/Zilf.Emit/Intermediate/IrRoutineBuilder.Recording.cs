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
    internal partial class IrRoutineBuilder
    {
        protected void RecordExtension(Action action) => Record(action);

        protected void RecordExtension(Action action, IrEffect effect) =>
            RecordOrderedOperation([], _ => action(), effect);

        internal void RecordTargetAction(Action action) =>
            RecordOrderedOperation([], _ => action(), IrEffect.Control);

        /// <summary>
        /// Records an opaque extension operation, flushing any pending promoted local values and stack values
        /// first so the operation observes the correct machine state.
        /// </summary>
        /// <param name="action">The lowering action that emits the operation.</param>
        /// <param name="writeRegions">The memory regions the operation may write.</param>
        /// <param name="diagnosticName">An optional name under which to count this operation for diagnostics.</param>
        private void Record(Action action, IrMemoryRegion writeRegions = IrMemoryRegion.All,
            string? diagnosticName = null)
        {
            PreserveStackValues();
            FlushPromotedLocals();
            RecordRaw(action, writeRegions, diagnosticName);
            localValues.Clear();
            dirtyLocals.Clear();
        }

        /// <summary>
        /// Appends a raw opaque target operation to the routine without flushing pending state.
        /// </summary>
        /// <param name="action">The lowering action that emits the operation.</param>
        /// <param name="writeRegions">The memory regions the operation may write.</param>
        /// <param name="diagnosticName">An optional name under which to count this operation for diagnostics.</param>
        private void RecordRaw(Action action, IrMemoryRegion writeRegions, string? diagnosticName = null)
        {
            if (diagnosticName != null)
                recordingStatistics[diagnosticName] = recordingStatistics.GetValueOrDefault(diagnosticName) + 1;
            effectSummary.DirectWrites |= writeRegions;
            routine.Append(current, IrOpcode.TargetOperation, [], IrEffect.Opaque, action, hasResult: false);
        }

        /// <summary>
        /// Records a temporary operation whose result, if requested, is kept as a compiler temporary local.
        /// </summary>
        /// <param name="operands">The operation operands.</param>
        /// <param name="emit">The lowering action that emits the operation.</param>
        /// <param name="result">The temporary local to receive the result, or <see langword="null"/>.</param>
        /// <param name="diagnosticName">An optional name under which to count this operation for diagnostics.</param>
        private void RecordTemporaryOperation(IReadOnlyList<IOperand> operands,
            Action<IReadOnlyList<IOperand>> emit, IVariable? result = null, string? diagnosticName = null)
        {
            if (operands.Any(IsEffectBarrierOperand))
            {
                Record(() => emit(operands), IrMemoryRegion.None, diagnosticName);
                return;
            }

            FlushPromotedLocals(variable => !compilerTemporaries.Contains(variable));
            foreach (var variable in localValues.Keys.Where(variable => !compilerTemporaries.Contains(variable)).ToArray())
                localValues.Remove(variable);

            var values = operands.Select(GetValue).ToArray();
            var hasTemporaryResult = result != null && compilerTemporaries.Contains(result);
            var instruction = AppendLowering(IrOpcode.TargetOperation, values, IrEffect.Opaque, emit,
                hasResult: hasTemporaryResult, resultHome: hasTemporaryResult ? result : null);
            if (diagnosticName != null)
                recordingStatistics[diagnosticName] = recordingStatistics.GetValueOrDefault(diagnosticName) + 1;
            if (hasTemporaryResult)
            {
                localValues[result!] = instruction.Result!;
                valueHomes[instruction.Result!] = result!;
                dirtyLocals.Remove(result!);
            }
        }

        /// <summary>
        /// Records an operation whose operands are evaluated in source order.
        /// </summary>
        /// <param name="operands">The operation operands.</param>
        /// <param name="emit">The lowering action that emits the operation.</param>
        /// <param name="effect">The effect the operation has on the routine.</param>
        /// <param name="writeRegions">The memory regions the operation may write.</param>
        protected void RecordOrderedOperation(IReadOnlyList<IOperand> operands,
            Action<IReadOnlyList<IOperand>> emit, IrEffect effect,
            IrMemoryRegion writeRegions = IrMemoryRegion.None)
        {
            if (operands.Any(IsEffectBarrierOperand))
            {
                Record(() => emit(operands));
                return;
            }

            AppendLowering(IrOpcode.TargetOperation, operands.Select(GetValue).ToArray(), effect, emit,
                hasResult: false, writeRegions: writeRegions);
        }

        /// <summary>
        /// Records an operation that produces a result and may write memory, routing the result either to a
        /// local/stack home or through a separate emit callback.
        /// </summary>
        /// <param name="operands">The operation operands.</param>
        /// <param name="emit">The lowering action that emits the operation.</param>
        /// <param name="effect">The effect the operation has on the routine.</param>
        /// <param name="result">The variable to receive the result, or <see langword="null"/>.</param>
        /// <param name="emitTo">The lowering action used when the result is routed to a specific home.</param>
        /// <param name="writeRegions">The memory regions the operation may write.</param>
        /// <param name="writeIdentity">The memory identity written by the operation, or <see langword="null"/>.</param>
        /// <param name="writtenOperandIndex">The operand index that identifies the written value.</param>
        protected void RecordEffectfulOperation(IReadOnlyList<IOperand> operands,
            Action<IReadOnlyList<IOperand>> emit, IrEffect effect, IVariable? result,
            Action<IReadOnlyList<IOperand>, IVariable?> emitTo,
            IrMemoryRegion writeRegions = IrMemoryRegion.None, IrMemoryIdentity? writeIdentity = null,
            int? writtenOperandIndex = null)
        {
            if (result != null && (locals.Contains(result) || ReferenceEquals(result, Stack)) &&
                !operands.Any(IsEffectBarrierOperand))
            {
                var instruction = AppendLowering(IrOpcode.TargetOperation, operands.Select(GetValue).ToArray(), effect,
                    emit, resultHome: result, emitTo: emitTo, writeRegions: writeRegions,
                    writeIdentity: writeIdentity);
                if (writtenOperandIndex is int index)
                    instruction.WrittenValue = instruction.Operands[index];
                SetProducedValue(result, instruction.Result!);
                return;
            }

            if (writeIdentity == null)
                RecordOrderedOperation(operands, emit, effect, writeRegions);
            else
            {
                var instruction = AppendLowering(IrOpcode.TargetOperation, operands.Select(GetValue).ToArray(), effect,
                    emit, hasResult: false, writeRegions: writeRegions, writeIdentity: writeIdentity);
                if (writtenOperandIndex is int index)
                    instruction.WrittenValue = instruction.Operands[index];
            }
        }

        /// <summary>
        /// Records an operation with a mix of required and optional operands, reconstructing the original
        /// null-preserving operand list when lowering.
        /// </summary>
        /// <param name="operands">The operation operands, some of which may be <see langword="null"/>.</param>
        /// <param name="emit">The lowering action that emits the operation.</param>
        /// <param name="effect">The effect the operation has on the routine.</param>
        /// <param name="writeRegions">The memory regions the operation may write.</param>
        private void RecordEffectfulOptionalOperation(IReadOnlyList<IOperand?> operands,
            Action<IReadOnlyList<IOperand?>> emit, IrEffect effect,
            IrMemoryRegion writeRegions = IrMemoryRegion.None)
        {
            if (operands.Where(operand => operand != null).Any(operand => IsEffectBarrierOperand(operand!)))
            {
                Record(() => emit(operands));
                return;
            }

            var present = operands.Where(operand => operand != null).Cast<IOperand>().ToArray();
            var values = present.Select(GetValue).ToArray();
            AppendLowering(IrOpcode.TargetOperation, values, effect, resolved =>
            {
                var index = 0;
                emit(operands.Select(operand => operand == null ? null : resolved[index++]).ToArray());
            }, hasResult: false, writeRegions: writeRegions);
        }

        /// <summary>
        /// Records an equality comparison that conditionally branches to a label.
        /// </summary>
        /// <param name="operands">The operands to compare.</param>
        /// <param name="label">The branch target label.</param>
        /// <param name="polarity">Whether to branch when the comparison is true or false.</param>
        /// <param name="emit">The lowering action that emits the comparison.</param>
        private void RecordEqualityBranch(IReadOnlyList<IOperand> operands, ILabel label, bool polarity,
            Action<IReadOnlyList<IOperand>> emit)
        {
            var values = operands.Select(GetValue).ToArray();
            PreserveStackValues();
            FlushPromotedLocals();
            var instruction = AppendLowering(IrOpcode.Equal, values, IrEffect.Control, emit);
            FinishConditional(label, instruction.Result!, polarity, instruction);
        }

        /// <summary>
        /// Records a read operation whose result is both produced into a variable and used to conditionally
        /// branch to a label.
        /// </summary>
        /// <param name="opcode">The opcode of the read operation.</param>
        /// <param name="operands">The operation operands.</param>
        /// <param name="result">The variable to receive the result.</param>
        /// <param name="label">The branch target label.</param>
        /// <param name="polarity">Whether to branch when the result is true or false.</param>
        /// <param name="emit">The lowering action that emits the read.</param>
        /// <param name="emitTo">The lowering action used when routing the result to a specific home.</param>
        private void RecordReadBranch(IrOpcode opcode, IReadOnlyList<IOperand> operands, IVariable result,
            ILabel label, bool polarity, Action<IReadOnlyList<IOperand>> emit,
            Action<IReadOnlyList<IOperand>, IVariable?> emitTo)
        {
            var values = operands.Select(GetValue).ToArray();
            PreserveStackValues();
            FlushPromotedLocals();
            var instruction = AppendLowering(opcode, values, IrEffect.Control, emit, resultHome: result,
                emitTo: emitTo);
            SetProducedValue(result, instruction.Result!);
            dirtyLocals.Remove(result);
            FinishConditional(label, instruction.Result!, polarity, instruction);
        }

        private bool IsEffectBarrierOperand(IOperand operand) =>
            operand is IIndirectOperand { Variable: var variable } && locals.Contains(variable);

        /// <summary>
        /// Appends a lowering instruction to the routine and updates the effect summary with its read and
        /// write effects.
        /// </summary>
        /// <param name="opcode">The instruction opcode.</param>
        /// <param name="operands">The instruction operand values.</param>
        /// <param name="effect">The effect the instruction has on the routine.</param>
        /// <param name="emit">The lowering action that emits the instruction.</param>
        /// <param name="hasResult">Whether the instruction produces a result value.</param>
        /// <param name="resultHome">The variable the result is assigned to, or <see langword="null"/>.</param>
        /// <param name="emitTo">The lowering action used when routing the result to a specific home.</param>
        /// <param name="readRegions">The memory regions the instruction may read.</param>
        /// <param name="writeRegions">The memory regions the instruction may write.</param>
        /// <param name="callSummary">The effect summary of the called routine, for call instructions.</param>
        /// <param name="readIdentity">The memory identity read by the instruction, or <see langword="null"/>.</param>
        /// <param name="writeIdentity">The memory identity written by the instruction, or <see langword="null"/>.</param>
        /// <returns>The appended instruction.</returns>
        private IrInstruction AppendLowering(IrOpcode opcode, IReadOnlyList<IrValue> operands, IrEffect effect,
            Action<IReadOnlyList<IOperand>> emit, bool hasResult = true, IVariable? resultHome = null,
            Action<IReadOnlyList<IOperand>, IVariable?>? emitTo = null,
            IrMemoryRegion readRegions = IrMemoryRegion.None, IrMemoryRegion writeRegions = IrMemoryRegion.None,
            IrRoutineEffectSummary? callSummary = null, IrMemoryIdentity? readIdentity = null,
            IrMemoryIdentity? writeIdentity = null)
        {
            var instructionReadIdentity = readIdentity is { IsParameterRelative: true } ? null : readIdentity;
            var instructionWriteIdentity = writeIdentity is { IsParameterRelative: true } ? null : writeIdentity;
            var instruction = routine.Append(current, opcode, operands, effect,
                new IrLoweringOperation(emit, resultHome, ReferenceEquals(resultHome, Stack), emitTo), hasResult,
                readRegions, writeRegions, callSummary, instructionReadIdentity, instructionWriteIdentity);
            if (instruction.Result != null && resultHome != null)
                instruction.Result.PhysicalHome = resultHome;
            var writtenRegions = effect switch
            {
                IrEffect.WriteMemory when writeRegions == IrMemoryRegion.None => IrMemoryRegion.All,
                IrEffect.Call when callSummary == null => IrMemoryRegion.All,
                _ => writeRegions,
            };
            if (writtenRegions != IrMemoryRegion.None)
                effectSummary.AddWrite(writtenRegions, writeIdentity);
            if (readRegions != IrMemoryRegion.None)
                effectSummary.AddRead(readRegions, readIdentity);
            if (callSummary != null)
                effectSummary.AddCallee(callSummary);
            else if (effect == IrEffect.Call)
                effectSummary.AddEffect(IrEffect.Opaque);
            else if (effect is not (IrEffect.None or IrEffect.ReadMemory or IrEffect.WriteMemory))
                effectSummary.AddEffect(effect);
            if (instruction.Result != null)
                definitions[instruction.Result] = instruction;
            return instruction;
        }

        private ILocalBuilder TrackLocal(ILocalBuilder local)
        {
            locals.Add(local);
            return local;
        }

        private ILocalBuilder TrackParameter(ILocalBuilder parameter)
        {
            TrackLocal(parameter);
            parameters.Add(parameter, effectSummary.GetParameterKey(parameters.Count));
            return parameter;
        }

        /// <summary>
        /// Produces a complete effect summary for a call site by binding the callee's summary to the call
        /// arguments.
        /// </summary>
        /// <param name="callee">The callee's effect summary.</param>
        /// <param name="args">The call argument values.</param>
        /// <returns>The bound call-site summary.</returns>
        private IrRoutineEffectSummary BindCallSummary(IrRoutineEffectSummary callee,
            IReadOnlyList<IrValue> args)
        {
            var summary = new IrRoutineEffectSummary { IsComplete = true };
            summary.AddCallee(callee, args.Select(GetMemoryBinding).ToArray());
            return summary;
        }

        /// <summary>
        /// Resolves the memory binding for a value, describing which identity it aliases for call-effect
        /// tracking.
        /// </summary>
        /// <param name="value">The value to bind.</param>
        /// <returns>The memory binding, or <see langword="null"/> if none can be determined.</returns>
        private IrMemoryBinding? GetMemoryBinding(IrValue value)
        {
            if (value.MemoryIdentity is { } identity)
                return new IrMemoryBinding(identity.Key, identity.Offset ?? 0);
            if (value.PhysicalHome is IVariable variable && parameters.TryGetValue(variable, out var parameter))
                return new IrMemoryBinding(parameter);
            return value.PhysicalHome is IConstantOperand operand
                ? new IrMemoryBinding(GetStableMemoryKey(operand))
                : null;
        }

        /// <summary>
        /// Resolves an operand into an <see cref="IrValue"/>, reusing an existing value when the operand maps
        /// to a live stack, local, or external value and otherwise creating (and caching) a new one.
        /// </summary>
        /// <param name="operand">The operand to resolve.</param>
        /// <returns>The corresponding IR value.</returns>
        private IrValue GetValue(IOperand operand)
        {
            if (ReferenceEquals(operand, Stack) && stackValues.Count > 0)
            {
                var index = stackValues.Count - 1;
                var value = stackValues[index];
                stackValues.RemoveAt(index);
                return value;
            }
            if (operand is INumericOperand numeric)
            {
                var constant = routine.CreateConstant(numeric.Value);
                constant.PhysicalHome = operand;
                valueHomes[constant] = operand;
                return constant;
            }
            if (operand is IVariable variable && locals.Contains(variable))
            {
                if (localValues.TryGetValue(variable, out var value))
                    return value;
                var localValue = routine.CreateValue();
                localValue.PhysicalHome = variable;
                if (parameters.TryGetValue(variable, out var parameter))
                    localValue.StableIdentity = parameter;
                localValues[variable] = localValue;
                valueHomes[localValue] = variable;
                return localValue;
            }
            if (!ReferenceEquals(operand, Stack) && externalValues.TryGetValue(operand, out var existing))
                return existing;
            var mutable = operand is IVariable or IIndirectOperand;
            var external = routine.CreateExternalValue(mutable, operand is INonzeroConstantOperand,
                operand is IMemoryAddressOperand memoryAddress &&
                    memoryAddress.TryGetMemoryAddress(out var allocation, out var offset)
                    ? new IrMemoryIdentity(IrMemoryRegion.Tables, allocation, offset)
                    : mutable && operand is not IIndirectOperand
                        ? new IrMemoryIdentity(IrMemoryRegion.Globals, GetStableMemoryKey(operand))
                        : null);
            external.PhysicalHome = operand;
            if (operand is IConstantOperand)
                external.StableIdentity = GetStableMemoryKey(operand);
            if (operand is IrRoutineBuilder routineTarget)
                external.RoutineTargets = new HashSet<IrRoutineEffectSummary> { routineTarget.EffectSummary };
            valueHomes[external] = operand;
            if (!ReferenceEquals(operand, Stack))
                externalValues[operand] = external;
            return external;
        }

        private static IrMemoryIdentity? TryGetMemoryIdentity(IOperand operand, IrMemoryRegion region) =>
            region == IrMemoryRegion.Globals && operand is IConstantOperand
                ? new IrMemoryIdentity(region, GetStableMemoryKey(operand))
                : null;

        /// <summary>
        /// Computes a memory identity for an object member access when both the object and member resolve to
        /// stable keys.
        /// </summary>
        /// <param name="obj">The object operand.</param>
        /// <param name="member">The member operand.</param>
        /// <param name="region">The memory region being accessed.</param>
        /// <returns>The memory identity, or <see langword="null"/> if one cannot be determined.</returns>
        private IrMemoryIdentity? TryGetObjectMemberIdentity(IOperand obj, IOperand member,
            IrMemoryRegion region)
        {
            var objectKey = obj switch
            {
                IConstantOperand => GetStableMemoryKey(obj),
                IVariable variable when parameters.TryGetValue(variable, out var parameter) => parameter,
                _ => null,
            };
            return objectKey != null && member is IConstantOperand
                ? new IrMemoryIdentity(region,
                    new IrObjectMemberKey(objectKey, GetStableMemoryKey(member)))
                : null;
        }

        /// <summary>
        /// Computes a memory identity for a table access given an address operand and a constant or symbolic
        /// index.
        /// </summary>
        /// <param name="address">The table base address operand.</param>
        /// <param name="index">The index operand.</param>
        /// <param name="scale">The size in bytes of each table element.</param>
        /// <param name="length">The length in bytes of the access.</param>
        /// <returns>The memory identity, or <see langword="null"/> if one cannot be determined.</returns>
        private IrMemoryIdentity? TryGetTableMemoryIdentity(IOperand address, IOperand index, int scale,
            int length)
        {
            object? allocation;
            int baseOffset;
            if (address is IMemoryAddressOperand memoryAddress &&
                memoryAddress.TryGetMemoryAddress(out allocation, out baseOffset))
            {
            }
            else if (address is IVariable variable && parameters.TryGetValue(variable, out var parameter))
            {
                allocation = parameter;
                baseOffset = 0;
            }
            else
                return null;
            if (index is not INumericOperand numeric)
                return new IrMemoryIdentity(IrMemoryRegion.Tables, allocation);
            var offset = (long)baseOffset + (long)numeric.Value * scale;
            return offset is >= int.MinValue and <= int.MaxValue
                ? new IrMemoryIdentity(IrMemoryRegion.Tables, allocation, (int)offset, length)
                : null;
        }

        /// <summary>
        /// Refines an existing table memory identity with a constant index and length, or widens it when the
        /// index is not constant.
        /// </summary>
        /// <param name="address">The table base address value.</param>
        /// <param name="index">The index value.</param>
        /// <param name="scale">The size in bytes of each table element.</param>
        /// <param name="length">The length in bytes of the access.</param>
        /// <returns>The refined or widened memory identity, or <see langword="null"/> if the address is not a table.</returns>
        private static IrMemoryIdentity? TryGetTableMemoryIdentity(IrValue address, IrValue index, int scale,
            int length)
        {
            if (address.MemoryIdentity is not { Region: IrMemoryRegion.Tables } identity)
                return null;
            if (index.Constant is not int numeric)
                return identity with { Offset = null, Length = null };
            if (identity.Offset == null)
                return identity with { Length = null };
            var offset = (long)identity.Offset.Value + (long)numeric * scale;
            return offset is >= int.MinValue and <= int.MaxValue
                ? identity with { Offset = (int)offset, Length = length }
                : null;
        }

        /// <summary>
        /// Propagates a table address identity through an addition or subtraction instruction, so that a
        /// computed pointer keeps the identity of the address it was derived from.
        /// </summary>
        /// <param name="instruction">The add or subtract instruction to analyze.</param>
        private static void SetDerivedAddressIdentity(IrInstruction instruction)
        {
            if (instruction.Result == null || instruction.Opcode is not (IrOpcode.Add or IrOpcode.Subtract) ||
                instruction.Operands.Count != 2)
                return;
            IrValue address;
            int delta;
            if (instruction.Operands[1].Constant is int right &&
                instruction.Operands[0].MemoryIdentity is { Region: IrMemoryRegion.Tables })
            {
                address = instruction.Operands[0];
                delta = instruction.Opcode == IrOpcode.Add ? right : -right;
            }
            else if (instruction.Opcode == IrOpcode.Add && instruction.Operands[0].Constant is int left &&
                instruction.Operands[1].MemoryIdentity is { Region: IrMemoryRegion.Tables })
            {
                address = instruction.Operands[1];
                delta = left;
            }
            else
            {
                return;
            }
            var identity = address.MemoryIdentity!;
            if (identity.Offset == null)
            {
                instruction.Result.MemoryIdentity = identity with { Length = null };
                return;
            }
            var offset = (long)identity.Offset.Value + delta;
            if (offset is >= int.MinValue and <= int.MaxValue)
                instruction.Result.MemoryIdentity = identity with { Offset = (int)offset, Length = null };
        }

        private static object GetStableMemoryKey(IOperand operand) => operand switch
        {
            IGlobalBuilder => operand,
            INumericOperand numeric => numeric.Value,
            _ => operand.ToString()!,
        };

        private static IrMemoryIdentity? GetDestinationIdentity(IVariable destination) =>
            destination is IIndirectOperand
                ? null
                : new IrMemoryIdentity(IrMemoryRegion.Globals, GetStableMemoryKey(destination));

        private void SetLocalValue(IVariable variable, IrValue value)
        {
            localValues[variable] = value;
            dirtyLocals.Add(variable);
            valueHomes[value] = variable;
            value.PhysicalHome = variable;
        }

        private void InvalidateMutableExternalValues()
        {
            foreach (var pair in externalValues.Where(pair => pair.Value.MutableExternal).ToArray())
                externalValues.Remove(pair.Key);
        }

        /// <summary>
        /// Drops cached external values that may have been modified by a call, either all of them when the
        /// callee writes an unknown set of globals or only those whose identities the callee is known to write.
        /// </summary>
        /// <param name="summary">The callee's effect summary, or <see langword="null"/> to invalidate all globals.</param>
        private void InvalidateMutableExternalValues(IrRoutineEffectSummary? summary)
        {
            if (summary == null || (summary.GetWrittenRegions() & IrMemoryRegion.Globals) != 0)
            {
                InvalidateMutableExternalValues();
                return;
            }
            var written = summary.GetWrittenIdentities();
            foreach (var pair in externalValues.Where(pair => pair.Value.MutableExternal &&
                pair.Value.MemoryIdentity != null && written.Contains(pair.Value.MemoryIdentity)).ToArray())
                externalValues.Remove(pair.Key);
        }

        private void SetProducedValue(IVariable variable, IrValue value)
        {
            valueHomes[value] = variable;
            if (ReferenceEquals(variable, Stack))
                stackValues.Add(value);
            else
                SetLocalValue(variable, value);
        }

        /// <summary>
        /// Stores any live stack values into the machine stack so that a subsequent operation with unknown
        /// effects cannot corrupt them.
        /// </summary>
        private void PreserveStackValues()
        {
            if (stackValues.Count == 0)
                return;

            foreach (var value in stackValues)
            {
                if (definitions.TryGetValue(value, out var definition) &&
                    definition.Payload is IrLoweringOperation lowering)
                    lowering.StackEscapes = true;
            }

            AppendLowering(IrOpcode.TargetOperation, stackValues.ToArray(), IrEffect.Stack, operands =>
            {
                foreach (var operand in operands)
                {
                    if (!ReferenceEquals(operand, Stack))
                        target.EmitStore(Stack, operand);
                }
            }, hasResult: false);
            stackValues.Clear();
        }

        /// <summary>
        /// Materializes the current values of dirty promoted locals back to their local variable storage.
        /// </summary>
        /// <param name="predicate">An optional filter selecting which locals to flush.</param>
        private void FlushPromotedLocals(Func<IVariable, bool>? predicate = null)
        {
            foreach (var variable in dirtyLocals.Where(variable => predicate == null || predicate(variable)).ToArray())
            {
                var value = localValues[variable];
                var materialization = AppendLowering(IrOpcode.TargetOperation, [value], IrEffect.Control,
                    operands => target.EmitStore(variable, operands[0]), hasResult: false, resultHome: variable);
                ((IrLoweringOperation)materialization.Payload!).IsMaterialization = true;
                dirtyLocals.Remove(variable);
            }
        }

    }
}
