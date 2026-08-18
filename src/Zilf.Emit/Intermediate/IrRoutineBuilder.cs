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
    /// <summary>
    /// Records the target-independent routine-builder protocol as a control-flow graph and lowers it into a target
    /// builder after verification and optimization.
    /// </summary>
    internal class IrRoutineBuilder : IRoutineBuilder, INonzeroConstantOperand
    {
        private readonly IRoutineBuilder target;
        private readonly RoutineIr routine = new();
        private readonly IrRoutineEffectSummary effectSummary = new();
        private readonly RoutineIrOptimizer optimizer;
        private readonly bool optimize;
        private readonly Func<IOperand, bool>? preferConstantHome;
        private readonly Func<int, INumericOperand> makeOperand;
        private readonly Dictionary<ILabel, IrBlock> labelBlocks = [];
        private readonly Dictionary<IrBlock, ILabel> blockLabels = [];
        private readonly HashSet<IVariable> locals = [];
        private readonly HashSet<IVariable> compilerTemporaries = [];
        private readonly Dictionary<IVariable, IrValue> localValues = [];
        private readonly HashSet<IVariable> dirtyLocals = [];
        private readonly Dictionary<IrValue, IOperand> valueHomes = [];
        private readonly Dictionary<IrValue, IrInstruction> definitions = [];
        private readonly Dictionary<IOperand, IrValue> externalValues = [];
        private readonly List<IrValue> stackValues = [];
        private readonly List<IrBlock> layout = [];
        private IrBlock current;
        private bool finished;
        private int nextIrTemporary;

        public IrRoutineBuilder(IRoutineBuilder target, IrNumericSemantics numericSemantics, bool optimize,
            Func<int, INumericOperand>? makeOperand = null, Func<IOperand, bool>? preferConstantHome = null)
        {
            this.target = target;
            this.optimize = optimize;
            this.preferConstantHome = preferConstantHome;
            this.makeOperand = makeOperand ?? (value => new DeferredNumericOperand(value));
            optimizer = new RoutineIrOptimizer(numericSemantics, () =>
            {
                if (numericSemantics == IrNumericSemantics.ZMachine16 && locals.Count >= 15)
                    return null;
                var temporary = TrackLocal(target.DefineLocal($"?IR{nextIrTemporary++}"));
                compilerTemporaries.Add(temporary);
                return temporary;
            }, (destination, value) => target.EmitStore(destination, value), () => compilerTemporaries);
            current = routine.Entry;
            layout.Add(current);
            labelBlocks.Add(target.RoutineStart, current);
            blockLabels.Add(current, target.RoutineStart);

            var trueBlock = GetLabelBlock(target.RTrue);
            trueBlock.Terminator = new IrTerminator.Return(routine.CreateConstant(1));
            var falseBlock = GetLabelBlock(target.RFalse);
            falseBlock.Terminator = new IrTerminator.Return(routine.CreateConstant(0));
        }

        internal IRoutineBuilder Target => target;

        internal IrRoutineEffectSummary EffectSummary => effectSummary;

        public bool CleanStack => target.CleanStack;

        public ILabel RTrue => target.RTrue;

        public ILabel RFalse => target.RFalse;

        public IVariable Stack => target.Stack;

        public bool UsesStackBasedCalls => target.UsesStackBasedCalls;

        public ILabel RoutineStart => target.RoutineStart;

        public bool HasArgCount => target.HasArgCount;

        public bool HasBranchSave => target.HasBranchSave;

        public bool HasStoreSave => target.HasStoreSave;

        public bool HasExtendedSave => target.HasExtendedSave;

        public bool HasUndo => target.HasUndo;

        public IConstantOperand Add(IConstantOperand other) =>
            target.Add(other is IrRoutineBuilder ir ? (IConstantOperand)ir.Target : other);

        public ILocalBuilder DefineRequiredParameter(string name) => TrackLocal(target.DefineRequiredParameter(name));

        public ILocalBuilder DefineOptionalParameter(string paramName) =>
            TrackLocal(target.DefineOptionalParameter(paramName));

        public ILocalBuilder DefineLocal(string localName)
        {
            var local = TrackLocal(target.DefineLocal(localName));
            if (localName.StartsWith("?TMP", StringComparison.Ordinal))
                compilerTemporaries.Add(local);
            return local;
        }

        public ILabel DefineLabel()
        {
            var label = target.DefineLabel();
            GetLabelBlock(label);
            return label;
        }

        public void MarkLabel(ILabel label)
        {
            PreserveStackValues();
            FlushPromotedLocals();
            var block = GetLabelBlock(label);
            if (!ReferenceEquals(current, block) && current.Terminator == null)
                current.Terminator = new IrTerminator.Jump(block);
            current = block;
            if (!layout.Contains(block))
                layout.Add(block);
            RecordOrderedOperation([], _ => target.MarkLabel(label), IrEffect.InputOutput);
            localValues.Clear();
            dirtyLocals.Clear();
        }

        public void Branch(ILabel label)
        {
            PreserveStackValues();
            FlushPromotedLocals();
            var instruction = AppendLowering(IrOpcode.TargetOperation, [], IrEffect.Control,
                _ => target.Branch(label), hasResult: false);
            current.Terminator = new IrTerminator.Jump(GetLabelBlock(label), EmitJump, instruction);
            StartFallthrough();
        }

        public void Branch(Condition cond, IOperand? left, IOperand? right, ILabel label, bool polarity)
        {
            if (cond is Condition.IncCheck or Condition.DecCheck && left is IVariable variable && right != null)
            {
                var rightValue = GetValue(right);
                PreserveStackValues();
                FlushPromotedLocals();
                AppendLowering(IrOpcode.TargetOperation, [rightValue], IrEffect.Control,
                    operands => target.Branch(cond, variable, operands[0], label, polarity), hasResult: false,
                    resultHome: variable);
                localValues.Remove(variable);
                dirtyLocals.Remove(variable);
                FinishConditional(label);
                return;
            }
            if (left != null && TryMap(cond, out var opcode) &&
                (right != null || cond == Condition.ArgProvided))
            {
                var operands = right == null ? new[] { left } : new[] { left, right };
                var values = operands.Select(GetValue).ToArray();
                PreserveStackValues();
                FlushPromotedLocals();
                var instruction = AppendLowering(opcode, values, IrEffect.Control,
                    resolved => target.Branch(cond, resolved[0], right == null ? null : resolved[1], label, polarity));
                FinishConditional(label, instruction.Result!, polarity, instruction);
                return;
            }
            Record(() => target.Branch(cond, left, right, label, polarity), IrMemoryRegion.None);
            FinishConditional(label);
        }

        public void BranchIfZero(IOperand operand, ILabel label, bool polarity)
        {
            var value = GetValue(operand);
            PreserveStackValues();
            FlushPromotedLocals();
            var instruction = AppendLowering(IrOpcode.Equal, [value, routine.CreateConstant(0)],
                IrEffect.Control, resolved => target.BranchIfZero(resolved[0], label, polarity));
            FinishConditional(label, instruction.Result!, polarity, instruction);
        }

        public void BranchIfEqual(IOperand value, IOperand option1, ILabel label, bool polarity)
        {
            RecordEqualityBranch([value, option1], label, polarity,
                operands => target.BranchIfEqual(operands[0], operands[1], label, polarity));
        }

        public void BranchIfEqual(IOperand value, IOperand option1, IOperand option2, ILabel label, bool polarity)
        {
            RecordEqualityBranch([value, option1, option2], label, polarity,
                operands => target.BranchIfEqual(operands[0], operands[1], operands[2], label, polarity));
        }

        public void BranchIfEqual(IOperand value, IOperand option1, IOperand option2, IOperand option3,
            ILabel label, bool polarity)
        {
            RecordEqualityBranch([value, option1, option2, option3], label, polarity,
                operands => target.BranchIfEqual(operands[0], operands[1], operands[2], operands[3], label, polarity));
        }

        public void Return(IOperand result)
        {
            var value = GetValue(result);
            PreserveStackValues();
            AppendLowering(IrOpcode.TargetOperation, [value], IrEffect.Control,
                operands => target.Return(operands[0]), hasResult: false);
            current.Terminator = new IrTerminator.Return(value);
            dirtyLocals.Clear();
            StartFallthrough();
        }

        public void EmitRestart() => RecordTerminator(target.EmitRestart);

        public void EmitQuit() => RecordTerminator(target.EmitQuit);

        public void EmitSave(ILabel label, bool polarity)
        {
            Record(() => target.EmitSave(label, polarity));
            FinishConditional(label);
        }

        public void EmitRestore(ILabel label, bool polarity)
        {
            Record(() => target.EmitRestore(label, polarity));
            FinishConditional(label);
        }

        public void EmitSave(IVariable result) => Record(() => target.EmitSave(result));

        public void EmitRestore(IVariable result) => Record(() => target.EmitRestore(result));

        public void EmitSave(IOperand table, IOperand size, IOperand name, IOperand? prompt, IVariable result) =>
            Record(() => target.EmitSave(table, size, name, prompt, result));

        public void EmitRestore(IOperand table, IOperand size, IOperand name, IOperand? prompt, IVariable result) =>
            Record(() => target.EmitRestore(table, size, name, prompt, result));

        public void EmitScanTable(IOperand value, IOperand table, IOperand length, IOperand? form,
            IVariable result, ILabel label, bool polarity)
        {
            if (locals.Contains(result) || ReferenceEquals(result, Stack))
            {
                var operands = new List<IOperand> { value, table, length };
                if (form != null)
                    operands.Add(form);
                RecordReadBranch(IrOpcode.ScanTable, operands, result, label, polarity, resolved =>
                    target.EmitScanTable(resolved[0], resolved[1], resolved[2], form == null ? null : resolved[3],
                        result, label, polarity),
                    (resolved, home) => target.EmitScanTable(resolved[0], resolved[1], resolved[2],
                        form == null ? null : resolved[3], home!, label, polarity));
                return;
            }
            Record(() => target.EmitScanTable(value, table, length, form, result, label, polarity));
            FinishConditional(label);
        }

        public void EmitGetChild(IOperand value, IVariable result, ILabel label, bool polarity)
        {
            if (locals.Contains(result) || ReferenceEquals(result, Stack))
            {
                RecordReadBranch(IrOpcode.LoadChild, [value], result, label, polarity,
                    operands => target.EmitGetChild(operands[0], result, label, polarity),
                    (operands, home) => target.EmitGetChild(operands[0], home!, label, polarity));
                return;
            }
            Record(() => target.EmitGetChild(value, result, label, polarity));
            FinishConditional(label);
        }

        public void EmitGetSibling(IOperand value, IVariable result, ILabel label, bool polarity)
        {
            if (locals.Contains(result) || ReferenceEquals(result, Stack))
            {
                RecordReadBranch(IrOpcode.LoadSibling, [value], result, label, polarity,
                    operands => target.EmitGetSibling(operands[0], result, label, polarity),
                    (operands, home) => target.EmitGetSibling(operands[0], home!, label, polarity));
                return;
            }
            Record(() => target.EmitGetSibling(value, result, label, polarity));
            FinishConditional(label);
        }

        public void EmitNullary(NullaryOp op, IVariable? result)
        {
            var effect = op switch
            {
                NullaryOp.ShowStatus => IrEffect.InputOutput,
                NullaryOp.Catch => IrEffect.Nondeterministic,
                _ => IrEffect.Opaque,
            };
            if (effect == IrEffect.Opaque)
                Record(() => target.EmitNullary(op, result));
            else
                RecordEffectfulOperation([], _ => target.EmitNullary(op, result), effect, result,
                    (_, home) => target.EmitNullary(op, home));
        }

        public void EmitUnary(UnaryOp op, IOperand value, IVariable? result)
        {
            if (result != null && (locals.Contains(result) || ReferenceEquals(result, Stack)) &&
                TryMap(op, out var opcode))
            {
                var instruction = AppendLowering(opcode, [GetValue(value)], IrEffect.None,
                    operands => target.EmitUnary(op, operands[0], result), resultHome: result,
                    emitTo: (operands, home) => target.EmitUnary(op, operands[0], home));
                SetProducedValue(result, instruction.Result!);
                return;
            }
            if (result != null && (locals.Contains(result) || ReferenceEquals(result, Stack)) &&
                TryMapMemoryRead(op, out opcode))
            {
                var instruction = AppendLowering(opcode, [GetValue(value)], IrEffect.ReadMemory,
                    operands => target.EmitUnary(op, operands[0], result), resultHome: result,
                    emitTo: (operands, home) => target.EmitUnary(op, operands[0], home),
                    readRegions: GetReadRegions(opcode));
                SetProducedValue(result, instruction.Result!);
                return;
            }
            if (TryClassify(op, out var effect))
                RecordEffectfulOperation([value], operands => target.EmitUnary(op, operands[0], result), effect, result,
                    (operands, home) => target.EmitUnary(op, operands[0], home), GetWriteRegions(op));
            else
                RecordTemporaryOperation([value], operands => target.EmitUnary(op, operands[0], result), result);
        }

        public void EmitBinary(BinaryOp op, IOperand left, IOperand right, IVariable? result)
        {
            if (op == BinaryOp.StoreIndirect && left is IIndirectOperand { Variable: var variable } &&
                locals.Contains(variable))
            {
                Record(() => target.EmitBinary(op, left, right, result), IrMemoryRegion.None);
                return;
            }
            if (result != null && (locals.Contains(result) || ReferenceEquals(result, Stack)) &&
                TryMapMemoryRead(op, out var loadOpcode))
            {
                var instruction = AppendLowering(loadOpcode, [GetValue(left), GetValue(right)], IrEffect.ReadMemory,
                    operands => target.EmitBinary(op, operands[0], operands[1], result), resultHome: result,
                    emitTo: (operands, home) => target.EmitBinary(op, operands[0], operands[1], home),
                    readRegions: GetReadRegions(loadOpcode));
                SetProducedValue(result, instruction.Result!);
                return;
            }
            if (result != null && (locals.Contains(result) || ReferenceEquals(result, Stack)) &&
                TryMap(op, out var opcode))
            {
                var instruction = AppendLowering(opcode, [GetValue(left), GetValue(right)], IrEffect.None,
                    operands => target.EmitBinary(op, operands[0], operands[1], result), resultHome: result,
                    emitTo: (operands, home) => target.EmitBinary(op, operands[0], operands[1], home));
                SetProducedValue(result, instruction.Result!);
                return;
            }
            if (TryClassify(op, out var effect))
            {
                RecordEffectfulOperation([left, right],
                    operands => target.EmitBinary(op, operands[0], operands[1], result), effect, result,
                    (operands, home) => target.EmitBinary(op, operands[0], operands[1], home), GetWriteRegions(op));
                return;
            }
            RecordTemporaryOperation([left, right],
                operands => target.EmitBinary(op, operands[0], operands[1], result), result);
        }

        public void EmitTernary(TernaryOp op, IOperand left, IOperand center, IOperand right, IVariable? result)
        {
            if (TryClassify(op, out var effect))
            {
                RecordEffectfulOperation([left, center, right],
                    operands => target.EmitTernary(op, operands[0], operands[1], operands[2], result), effect, result,
                    (operands, home) => target.EmitTernary(op, operands[0], operands[1], operands[2], home),
                    GetWriteRegions(op));
                return;
            }
            RecordTemporaryOperation([left, center, right],
                operands => target.EmitTernary(op, operands[0], operands[1], operands[2], result), result);
        }

        public void EmitPrint(string text, bool crlfRtrue)
        {
            if (crlfRtrue)
                RecordOrderedOperation([], _ => target.EmitPrint(text, true), IrEffect.InputOutput);
            else
                RecordOrderedOperation([], _ => target.EmitPrint(text, false), IrEffect.InputOutput);
            if (crlfRtrue)
            {
                current.Terminator = new IrTerminator.Return(routine.CreateConstant(1));
                StartFallthrough();
            }
        }

        public void EmitPrint(PrintOp op, IOperand value) => RecordOrderedOperation([value],
            operands => target.EmitPrint(op, operands[0]), IrEffect.InputOutput);

        public void EmitPrintTable(IOperand table, IOperand width, IOperand? height, IOperand? skip)
        {
            var operands = new List<IOperand> { table, width };
            if (height != null)
                operands.Add(height);
            if (skip != null)
                operands.Add(skip);
            RecordOrderedOperation(operands, resolved =>
            {
                var index = 2;
                target.EmitPrintTable(resolved[0], resolved[1], height == null ? null : resolved[index++],
                    skip == null ? null : resolved[index]);
            }, IrEffect.InputOutput);
        }

        public void EmitPrintNewLine() => RecordOrderedOperation([], _ => target.EmitPrintNewLine(), IrEffect.InputOutput);

        public void EmitRead(IOperand chrbuf, IOperand? lexbuf, IOperand? interval, IOperand? routineOperand,
            IVariable? result) => Record(() => target.EmitRead(chrbuf, lexbuf, interval, routineOperand, result));

        public void EmitReadChar(IOperand? interval, IOperand? routineOperand, IVariable result) =>
            Record(() => target.EmitReadChar(interval, routineOperand, result));

        public void EmitPlaySound(IOperand number, IOperand? effect, IOperand? volume, IOperand? routineOperand) =>
            RecordEffectfulOptionalOperation([number, effect, volume, routineOperand],
                operands => target.EmitPlaySound(operands[0]!, operands[1], operands[2], operands[3]),
                routineOperand == null ? IrEffect.InputOutput : IrEffect.Call);

        public void EmitEncodeText(IOperand src, IOperand length, IOperand srcOffset, IOperand dest) =>
            RecordOrderedOperation([src, length, srcOffset, dest],
                operands => target.EmitEncodeText(operands[0], operands[1], operands[2], operands[3]),
                IrEffect.WriteMemory, IrMemoryRegion.Tables);

        public void EmitTokenize(IOperand text, IOperand parse, IOperand? dictionary, IOperand? flag) =>
            RecordEffectfulOptionalOperation([text, parse, dictionary, flag],
                operands => target.EmitTokenize(operands[0]!, operands[1]!, operands[2], operands[3]),
                IrEffect.WriteMemory, IrMemoryRegion.Tables);

        public void EmitCall(IOperand routineOperand, IOperand[] args, IVariable? result)
        {
            var callSummary = (routineOperand as IrRoutineBuilder)?.EffectSummary;
            var operands = new IOperand[args.Length + 1];
            operands[0] = routineOperand;
            Array.Copy(args, 0, operands, 1, args.Length);
            if (operands.Any(operand => operand is IIndirectOperand { Variable: var variable } &&
                locals.Contains(variable)))
            {
                var capturedArgs = (IOperand[])args.Clone();
                Record(() => target.EmitCall(routineOperand, capturedArgs, result));
                return;
            }

            var values = operands.Select(GetValue).ToArray();
            PreserveStackValues();
            if (result != null && (locals.Contains(result) || ReferenceEquals(result, Stack)))
            {
                var instruction = AppendLowering(IrOpcode.TargetOperation, values, IrEffect.Call,
                    resolved => target.EmitCall(resolved[0], resolved.Skip(1).ToArray(), result), resultHome: result,
                    emitTo: (resolved, home) => target.EmitCall(resolved[0], resolved.Skip(1).ToArray(), home),
                    callSummary: callSummary);
                SetProducedValue(result, instruction.Result!);
                dirtyLocals.Remove(result);
            }
            else
            {
                AppendLowering(IrOpcode.TargetOperation, values, IrEffect.Call,
                    resolved => target.EmitCall(resolved[0], resolved.Skip(1).ToArray(), result), hasResult: false,
                    callSummary: callSummary);
            }
        }

        public void EmitStore(IVariable dest, IOperand src)
        {
            if (locals.Contains(dest) && ReferenceEquals(src, Stack) && stackValues.Count > 0)
            {
                var index = stackValues.Count - 1;
                var value = stackValues[index];
                stackValues.RemoveAt(index);
                if (definitions.TryGetValue(value, out var definition) &&
                    definition.Payload is IrLoweringOperation { EmitTo: not null } lowering)
                {
                    lowering.ResultHome = dest;
                    lowering.IsStackResult = false;
                    SetLocalValue(dest, value);
                    dirtyLocals.Remove(dest);
                    AppendLowering(IrOpcode.TargetOperation, [value], IrEffect.InputOutput, _ => { },
                        hasResult: false);
                    return;
                }
                stackValues.Add(value);
            }
            if (locals.Contains(dest) && !ReferenceEquals(src, Stack))
            {
                var copy = AppendLowering(IrOpcode.Copy, [GetValue(src)], IrEffect.None,
                    operands => target.EmitStore(dest, operands[0]), resultHome: dest);
                SetLocalValue(dest, copy.Result!);
                return;
            }
            if (!locals.Contains(dest) && dest is not IIndirectOperand)
            {
                var value = GetValue(src);
                AppendLowering(IrOpcode.TargetOperation, [value], IrEffect.WriteMemory,
                    operands => target.EmitStore(dest, operands[0]), hasResult: false,
                    writeRegions: IrMemoryRegion.Globals);
                return;
            }
            Record(() => target.EmitStore(dest, src));
        }

        public void EmitPopStack()
        {
            PreserveStackValues();
            AppendLowering(IrOpcode.TargetOperation, [], IrEffect.Stack, _ => target.EmitPopStack(), hasResult: false);
        }

        public void EmitPushUserStack(IOperand value, IOperand stack, ILabel label, bool polarity)
        {
            var values = new[] { GetValue(value), GetValue(stack) };
            PreserveStackValues();
            FlushPromotedLocals();
            AppendLowering(IrOpcode.TargetOperation, values, IrEffect.WriteMemory,
                operands => target.EmitPushUserStack(operands[0], operands[1], label, polarity), hasResult: false,
                writeRegions: IrMemoryRegion.Tables);
            FinishConditional(label);
        }

        public void Finish()
        {
            if (finished)
                throw new InvalidOperationException("The routine has already been finished.");
            finished = true;

            foreach (var block in routine.Blocks)
                block.Terminator ??= new IrTerminator.Return(null);

            PreserveStackValues();
            FlushPromotedLocals();
            effectSummary.IsComplete = true;

            if (optimize)
                optimizer.Optimize(routine);
            else
                routine.Verify();

            var availableConstantHomes = preferConstantHome != null
                ? FindAvailableConstantHomes()
                : null;

            foreach (var block in layout)
            {
                if (!routine.Blocks.Contains(block))
                    continue;
                var blockConstantHomes = availableConstantHomes != null &&
                    availableConstantHomes.TryGetValue(block, out var incoming)
                    ? new Dictionary<IVariable, IrValue>(incoming)
                    : null;
                foreach (var instruction in block.Instructions)
                {
                    if (instruction.Payload is IrLoweringOperation lowering)
                    {
                        lowering.Replay(instruction.Operands
                            .Select(value => ResolveOperand(value, blockConstantHomes, lowering.ResultHome)).ToArray());
                        if (blockConstantHomes != null)
                            UpdateAvailableConstantHomes(blockConstantHomes, instruction);
                    }
                    else
                        ((Action)instruction.Payload!).Invoke();
                    if (instruction.Result != null && instruction.Payload is IrLoweringOperation
                        { ResultHome: not null } &&
                        !valueHomes.ContainsKey(instruction.Result))
                        throw new InvalidOperationException($"No physical home was assigned to {instruction.Result}.");
                }
            }

            target.Finish();
        }

        public override string ToString() => target.ToString()!;

        protected void RecordExtension(Action action) => Record(action);

        protected void RecordExtension(Action action, IrEffect effect) =>
            RecordOrderedOperation([], _ => action(), effect);

        internal void RecordTargetAction(Action action) =>
            RecordOrderedOperation([], _ => action(), IrEffect.Control);

        private void Record(Action action, IrMemoryRegion writeRegions = IrMemoryRegion.All)
        {
            PreserveStackValues();
            FlushPromotedLocals();
            RecordRaw(action, writeRegions);
            localValues.Clear();
            dirtyLocals.Clear();
        }

        private void RecordRaw(Action action, IrMemoryRegion writeRegions)
        {
            effectSummary.DirectWrites |= writeRegions;
            routine.Append(current, IrOpcode.TargetOperation, [], IrEffect.Opaque, action, hasResult: false);
        }

        private void RecordTemporaryOperation(IReadOnlyList<IOperand> operands,
            Action<IReadOnlyList<IOperand>> emit, IVariable? result = null)
        {
            if (operands.Any(IsEffectBarrierOperand))
            {
                Record(() => emit(operands), IrMemoryRegion.None);
                return;
            }

            FlushPromotedLocals(variable => !compilerTemporaries.Contains(variable));
            foreach (var variable in localValues.Keys.Where(variable => !compilerTemporaries.Contains(variable)).ToArray())
                localValues.Remove(variable);

            var values = operands.Select(GetValue).ToArray();
            var hasTemporaryResult = result != null && compilerTemporaries.Contains(result);
            var instruction = AppendLowering(IrOpcode.TargetOperation, values, IrEffect.Opaque, emit,
                hasResult: hasTemporaryResult, resultHome: hasTemporaryResult ? result : null);
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
            IrMemoryRegion writeRegions = IrMemoryRegion.None)
        {
            if (result != null && (locals.Contains(result) || ReferenceEquals(result, Stack)) &&
                !operands.Any(IsEffectBarrierOperand))
            {
                var instruction = AppendLowering(IrOpcode.TargetOperation, operands.Select(GetValue).ToArray(), effect,
                    emit, resultHome: result, emitTo: emitTo, writeRegions: writeRegions);
                SetProducedValue(result, instruction.Result!);
                return;
            }

            RecordOrderedOperation(operands, emit, effect, writeRegions);
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
            IrRoutineEffectSummary? callSummary = null)
        {
            var instruction = routine.Append(current, opcode, operands, effect,
                new IrLoweringOperation(emit, resultHome, ReferenceEquals(resultHome, Stack), emitTo), hasResult,
                readRegions, writeRegions, callSummary);
            if (instruction.Result != null && resultHome != null)
                instruction.Result.PhysicalHome = resultHome;
            effectSummary.DirectWrites |= effect switch
            {
                IrEffect.WriteMemory when writeRegions == IrMemoryRegion.None => IrMemoryRegion.All,
                IrEffect.Call when callSummary == null => IrMemoryRegion.All,
                _ => writeRegions,
            };
            if (callSummary != null)
                effectSummary.AddCallee(callSummary);
            if (instruction.Result != null)
                definitions[instruction.Result] = instruction;
            return instruction;
        }

        private ILocalBuilder TrackLocal(ILocalBuilder local)
        {
            locals.Add(local);
            return local;
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
            var external = routine.CreateExternalValue(operand is IVariable or IIndirectOperand,
                operand is INonzeroConstantOperand);
            external.PhysicalHome = operand;
            valueHomes[external] = operand;
            if (!ReferenceEquals(operand, Stack))
                externalValues[operand] = external;
            return external;
        }

        private void SetLocalValue(IVariable variable, IrValue value)
        {
            localValues[variable] = value;
            dirtyLocals.Add(variable);
            valueHomes[value] = variable;
            value.PhysicalHome = variable;
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

        private IOperand ResolveOperand(IrValue value)
            => ResolveOperand(value, null, null);

        private IOperand ResolveOperand(IrValue value, IReadOnlyDictionary<IVariable, IrValue>? constantHomes,
            IVariable? destination)
        {
            if (definitions.TryGetValue(value, out var definition) &&
                definition.Payload is IrLoweringOperation { ResultHome: not null } lowering)
                return lowering.ResultHome;
            var directOperand = valueHomes.TryGetValue(value, out var existingOperand)
                ? existingOperand
                : value.Constant is int constant ? makeOperand(constant) : null;
            if (directOperand != null)
            {
                if (constantHomes != null && preferConstantHome!(directOperand))
                {
                    var home = constantHomes.FirstOrDefault(pair => ValuesEquivalent(pair.Value, value) &&
                        !ReferenceEquals(pair.Key, destination)).Key;
                    if (home != null)
                        return home;
                }
                return directOperand;
            }
            throw new InvalidOperationException($"No physical operand is available for {value}.");
        }

        private Dictionary<IrBlock, Dictionary<IVariable, IrValue>> FindAvailableConstantHomes()
        {
            routine.RebuildPredecessors();
            var result = routine.Blocks.ToDictionary(block => block,
                _ => new Dictionary<IVariable, IrValue>());
            var outgoing = routine.Blocks.ToDictionary(block => block,
                _ => new Dictionary<IVariable, IrValue>());
            bool changed;
            do
            {
                changed = false;
                foreach (var block in routine.Blocks)
                {
                    var incoming = block == routine.Entry || block.Predecessors.Count == 0
                        ? []
                        : IntersectConstantHomes(block.Predecessors.Select(predecessor => outgoing[predecessor]));
                    var nextOutgoing = new Dictionary<IVariable, IrValue>(incoming);
                    foreach (var instruction in block.Instructions)
                        UpdateAvailableConstantHomes(nextOutgoing, instruction);
                    if (!ConstantHomesEqual(result[block], incoming))
                    {
                        result[block] = incoming;
                        changed = true;
                    }
                    if (!ConstantHomesEqual(outgoing[block], nextOutgoing))
                    {
                        outgoing[block] = nextOutgoing;
                        changed = true;
                    }
                }
            }
            while (changed);
            return result;
        }

        private static Dictionary<IVariable, IrValue> IntersectConstantHomes(
            IEnumerable<Dictionary<IVariable, IrValue>> predecessors)
        {
            using var enumerator = predecessors.GetEnumerator();
            if (!enumerator.MoveNext())
                return [];
            var result = new Dictionary<IVariable, IrValue>(enumerator.Current);
            while (enumerator.MoveNext())
            {
                foreach (var pair in result.ToArray())
                {
                    if (!enumerator.Current.TryGetValue(pair.Key, out var value) ||
                        !ValuesEquivalent(value, pair.Value))
                        result.Remove(pair.Key);
                }
            }
            return result;
        }

        private void UpdateAvailableConstantHomes(Dictionary<IVariable, IrValue> homes, IrInstruction instruction)
        {
            if (instruction.Payload is not IrLoweringOperation { ResultHome: IVariable destination } lowering ||
                lowering.IsStackResult || ReferenceEquals(destination, Stack))
                return;
            homes.Remove(destination);
            if (instruction.Opcode is IrOpcode.Copy or IrOpcode.TargetOperation &&
                instruction.Operands.Count == 1 && IsReusableConstant(instruction.Operands[0]))
                homes[destination] = instruction.Operands[0];
        }

        private static bool ConstantHomesEqual(IReadOnlyDictionary<IVariable, IrValue> left,
            IReadOnlyDictionary<IVariable, IrValue> right) =>
            left.Count == right.Count && left.All(pair => right.TryGetValue(pair.Key, out var value) &&
                ValuesEquivalent(value, pair.Value));

        private bool IsReusableConstant(IrValue value) => value.Constant != null ||
            !value.MutableExternal && valueHomes.TryGetValue(value, out var operand) && operand is IConstantOperand;

        private static bool ValuesEquivalent(IrValue left, IrValue right) => ReferenceEquals(left, right) ||
            left.Constant is int leftConstant && right.Constant == leftConstant;

        private static bool TryMap(UnaryOp op, out IrOpcode opcode)
        {
            opcode = op switch
            {
                UnaryOp.Neg => IrOpcode.Negate,
                UnaryOp.Not => IrOpcode.BitwiseNot,
                _ => IrOpcode.TargetOperation,
            };
            return opcode != IrOpcode.TargetOperation;
        }

        private static bool TryMap(BinaryOp op, out IrOpcode opcode)
        {
            opcode = op switch
            {
                BinaryOp.Add => IrOpcode.Add,
                BinaryOp.Sub => IrOpcode.Subtract,
                BinaryOp.Mul => IrOpcode.Multiply,
                BinaryOp.Div => IrOpcode.Divide,
                BinaryOp.Mod => IrOpcode.Modulo,
                BinaryOp.And => IrOpcode.BitwiseAnd,
                BinaryOp.Or => IrOpcode.BitwiseOr,
                _ => IrOpcode.TargetOperation,
            };
            return opcode != IrOpcode.TargetOperation;
        }

        private static bool TryMapMemoryRead(BinaryOp op, out IrOpcode opcode)
        {
            opcode = op switch
            {
                BinaryOp.GetByte => IrOpcode.LoadByte,
                BinaryOp.GetWord => IrOpcode.LoadWord,
                BinaryOp.GetProperty => IrOpcode.LoadProperty,
                BinaryOp.GetPropAddress => IrOpcode.LoadPropertyAddress,
                BinaryOp.GetNextProp => IrOpcode.LoadNextProperty,
                _ => IrOpcode.TargetOperation,
            };
            return opcode != IrOpcode.TargetOperation;
        }

        private static bool TryMapMemoryRead(UnaryOp op, out IrOpcode opcode)
        {
            opcode = op switch
            {
                UnaryOp.GetParent => IrOpcode.LoadParent,
                UnaryOp.GetChild => IrOpcode.LoadChild,
                UnaryOp.GetSibling => IrOpcode.LoadSibling,
                UnaryOp.GetPropSize => IrOpcode.LoadPropertySize,
                _ => IrOpcode.TargetOperation,
            };
            return opcode != IrOpcode.TargetOperation;
        }

        private static bool TryClassify(UnaryOp op, out IrEffect effect)
        {
            effect = op switch
            {
                UnaryOp.Random => IrEffect.Nondeterministic,
                UnaryOp.RemoveObject or UnaryOp.GetCursor or UnaryOp.ReadMouse or UnaryOp.DirectOutput =>
                    IrEffect.WriteMemory,
                UnaryOp.DirectInput or UnaryOp.OutputStyle or UnaryOp.OutputBuffer or
                    UnaryOp.SplitWindow or UnaryOp.SelectWindow or UnaryOp.ClearWindow or UnaryOp.EraseLine or
                    UnaryOp.SetFont or UnaryOp.CheckUnicode or UnaryOp.PictureTable or UnaryOp.MouseWindow or
                    UnaryOp.PrintForm or UnaryOp.BufferScreen => IrEffect.InputOutput,
                UnaryOp.PopUserStack or UnaryOp.FlushStack => IrEffect.Stack,
                _ => IrEffect.Opaque,
            };
            return effect != IrEffect.Opaque;
        }

        private static bool TryClassify(BinaryOp op, out IrEffect effect)
        {
            effect = op switch
            {
                BinaryOp.MoveObject or BinaryOp.SetFlag or BinaryOp.ClearFlag or BinaryOp.DirectOutput =>
                    IrEffect.WriteMemory,
                BinaryOp.SetCursor or BinaryOp.SetColor or BinaryOp.SetTrueColor or
                    BinaryOp.GetWindowProperty or BinaryOp.ScrollWindow or BinaryOp.SetFont => IrEffect.InputOutput,
                BinaryOp.FlushUserStack => IrEffect.Stack,
                _ => IrEffect.Opaque,
            };
            return effect != IrEffect.Opaque;
        }

        private static bool TryClassify(TernaryOp op, out IrEffect effect)
        {
            effect = op switch
            {
                TernaryOp.PutByte or TernaryOp.PutWord or TernaryOp.PutProperty or TernaryOp.CopyTable or
                    TernaryOp.DirectOutput =>
                    IrEffect.WriteMemory,
                TernaryOp.PutWindowProperty or TernaryOp.DrawPicture or TernaryOp.WindowStyle or
                    TernaryOp.MoveWindow or TernaryOp.WindowSize or TernaryOp.SetMargins or TernaryOp.SetCursor or
                    TernaryOp.ErasePicture or TernaryOp.SetColor or
                    TernaryOp.SetTrueColor => IrEffect.InputOutput,
                _ => IrEffect.Opaque,
            };
            return effect != IrEffect.Opaque;
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

        private static IrMemoryRegion GetWriteRegions(UnaryOp op) => op switch
        {
            UnaryOp.RemoveObject => IrMemoryRegion.ObjectTree,
            UnaryOp.GetCursor or UnaryOp.ReadMouse => IrMemoryRegion.Tables,
            UnaryOp.DirectOutput => IrMemoryRegion.All,
            _ => IrMemoryRegion.None,
        };

        private static IrMemoryRegion GetWriteRegions(BinaryOp op) => op switch
        {
            BinaryOp.MoveObject => IrMemoryRegion.ObjectTree,
            BinaryOp.SetFlag or BinaryOp.ClearFlag => IrMemoryRegion.Attributes,
            BinaryOp.DirectOutput => IrMemoryRegion.All,
            _ => IrMemoryRegion.None,
        };

        private static IrMemoryRegion GetWriteRegions(TernaryOp op) => op switch
        {
            TernaryOp.PutByte or TernaryOp.PutWord or TernaryOp.CopyTable => IrMemoryRegion.Tables,
            TernaryOp.PutProperty => IrMemoryRegion.Properties,
            TernaryOp.DirectOutput => IrMemoryRegion.All,
            _ => IrMemoryRegion.None,
        };

        private static bool TryMap(Condition condition, out IrOpcode opcode)
        {
            opcode = condition switch
            {
                Condition.Less => IrOpcode.LessThan,
                Condition.Greater => IrOpcode.GreaterThan,
                Condition.TestBits => IrOpcode.BitTest,
                Condition.Inside => IrOpcode.Inside,
                Condition.TestAttr => IrOpcode.HasAttribute,
                Condition.ArgProvided => IrOpcode.ArgumentProvided,
                _ => IrOpcode.TargetOperation,
            };
            return opcode != IrOpcode.TargetOperation;
        }

        private sealed class DeferredNumericOperand(int value) : INumericOperand
        {
            public int Value { get; } = value;

            public IConstantOperand Add(IConstantOperand other) => throw new NotSupportedException();
        }

        private void RecordTerminator(Action action)
        {
            PreserveStackValues();
            FlushPromotedLocals();
            AppendLowering(IrOpcode.TargetOperation, [], IrEffect.Control, _ => action(), hasResult: false);
            localValues.Clear();
            dirtyLocals.Clear();
            current.Terminator = new IrTerminator.Return(null);
            StartFallthrough();
        }

        private void FinishConditional(ILabel label)
        {
            var fallthrough = CreateLayoutBlock();
            current.Terminator = new IrTerminator.Branch(routine.CreateValue(), GetLabelBlock(label), fallthrough);
            current = fallthrough;
        }

        private void FinishConditional(ILabel label, IrValue condition, bool polarity, IrInstruction instruction)
        {
            var fallthrough = CreateLayoutBlock();
            var targetBlock = GetLabelBlock(label);
            current.Terminator = polarity
                ? new IrTerminator.Branch(condition, targetBlock, fallthrough, EmitJump, instruction, targetBlock)
                : new IrTerminator.Branch(condition, fallthrough, targetBlock, EmitJump, instruction, targetBlock);
            current = fallthrough;
        }

        private void EmitJump(IrBlock block) => target.Branch(blockLabels[block]);

        private void StartFallthrough()
        {
            current = CreateLayoutBlock();
            localValues.Clear();
            dirtyLocals.Clear();
        }

        private IrBlock CreateLayoutBlock()
        {
            var block = routine.CreateBlock();
            layout.Add(block);
            return block;
        }

        private IrBlock GetLabelBlock(ILabel label)
        {
            if (labelBlocks.TryGetValue(label, out var block))
                return block;
            block = routine.CreateBlock();
            labelBlocks.Add(label, block);
            blockLabels.Add(block, label);
            return block;
        }
    }
}
