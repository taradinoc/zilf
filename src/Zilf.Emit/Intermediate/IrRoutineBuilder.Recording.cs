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

        private void Record(Action action, IrMemoryRegion writeRegions = IrMemoryRegion.All,
            string? diagnosticName = null)
        {
            PreserveStackValues();
            FlushPromotedLocals();
            RecordRaw(action, writeRegions, diagnosticName);
            localValues.Clear();
            dirtyLocals.Clear();
        }

        private void RecordRaw(Action action, IrMemoryRegion writeRegions, string? diagnosticName = null)
        {
            if (diagnosticName != null)
                recordingStatistics[diagnosticName] = recordingStatistics.GetValueOrDefault(diagnosticName) + 1;
            effectSummary.DirectWrites |= writeRegions;
            routine.Append(current, IrOpcode.TargetOperation, [], IrEffect.Opaque, action, hasResult: false);
        }

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

        private void RecordEqualityBranch(IReadOnlyList<IOperand> operands, ILabel label, bool polarity,
            Action<IReadOnlyList<IOperand>> emit)
        {
            var values = operands.Select(GetValue).ToArray();
            PreserveStackValues();
            FlushPromotedLocals();
            var instruction = AppendLowering(IrOpcode.Equal, values, IrEffect.Control, emit);
            FinishConditional(label, instruction.Result!, polarity, instruction);
        }

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

        private IrInstruction AppendLowering(IrOpcode opcode, IReadOnlyList<IrValue> operands, IrEffect effect,
            Action<IReadOnlyList<IOperand>> emit, bool hasResult = true, IVariable? resultHome = null,
            Action<IReadOnlyList<IOperand>, IVariable?>? emitTo = null,
            IrMemoryRegion readRegions = IrMemoryRegion.None, IrMemoryRegion writeRegions = IrMemoryRegion.None,
            IrRoutineEffectSummary? callSummary = null, IrMemoryIdentity? readIdentity = null,
            IrMemoryIdentity? writeIdentity = null)
        {
            var instruction = routine.Append(current, opcode, operands, effect,
                new IrLoweringOperation(emit, resultHome, ReferenceEquals(resultHome, Stack), emitTo), hasResult,
                readRegions, writeRegions, callSummary, readIdentity, writeIdentity);
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

        private IrRoutineEffectSummary BindCallSummary(IrRoutineEffectSummary callee, IReadOnlyList<IOperand> args)
        {
            var summary = new IrRoutineEffectSummary { IsComplete = true };
            summary.AddCallee(callee, args.Select(GetMemoryBinding).ToArray());
            return summary;
        }

        private IrMemoryBinding? GetMemoryBinding(IOperand operand)
        {
            if (operand is IVariable variable && parameters.TryGetValue(variable, out var parameter))
                return new IrMemoryBinding(parameter);
            if (operand is IMemoryAddressOperand address &&
                address.TryGetMemoryAddress(out var allocation, out var offset))
                return new IrMemoryBinding(allocation, offset);
            if (operand is IConstantOperand)
                return new IrMemoryBinding(GetStableMemoryKey(operand));
            return null;
        }

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
                valueHomes[constant] = operand;
                return constant;
            }
            if (operand is IVariable variable && locals.Contains(variable))
            {
                if (localValues.TryGetValue(variable, out var value))
                    return value;
                var localValue = routine.CreateValue();
                localValue.PhysicalHome = variable;
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
            valueHomes[external] = operand;
            if (!ReferenceEquals(operand, Stack))
                externalValues[operand] = external;
            return external;
        }

        private static IrMemoryIdentity? TryGetMemoryIdentity(IOperand operand, IrMemoryRegion region) =>
            region == IrMemoryRegion.Globals && operand is IConstantOperand
                ? new IrMemoryIdentity(region, GetStableMemoryKey(operand))
                : null;

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

        private IrMemoryIdentity? TryGetTableMemoryIdentity(IOperand address, IOperand index, int scale,
            int length)
        {
            object allocation;
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

        private static IrMemoryIdentity? TryGetTableMemoryIdentity(IrValue address, IrValue index, int scale,
            int length)
        {
            if (address.MemoryIdentity is not { Region: IrMemoryRegion.Tables } identity)
                return null;
            if (index.Constant is not int numeric)
                return identity with { Offset = null, Length = null };
            var offset = (long)(identity.Offset ?? 0) + (long)numeric * scale;
            return offset is >= int.MinValue and <= int.MaxValue
                ? identity with { Offset = (int)offset, Length = length }
                : null;
        }

        private void SetDerivedAddressIdentity(IrInstruction instruction)
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
            var offset = (long)(identity.Offset ?? 0) + delta;
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
