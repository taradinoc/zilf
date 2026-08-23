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
    internal partial class IrRoutineBuilder : IRoutineBuilder, INonzeroConstantOperand
    {
        private readonly IRoutineBuilder target;
        private readonly RoutineIr routine = new();
        private readonly IrRoutineEffectSummary effectSummary = new();
        private readonly RoutineIrOptimizer optimizer;
        private readonly bool optimize;
        private readonly Func<IOperand, bool>? preferConstantHome;
        private readonly Action<IEnumerable<IrOptimizationStat>>? recordOptimizationStats;
        private readonly Func<int, INumericOperand> makeOperand;
        private readonly Action<IrRoutineBuilder>? deferFinalization;
        private readonly int wordSize;
        private readonly Dictionary<ILabel, IrBlock> labelBlocks = [];
        private readonly Dictionary<IrBlock, ILabel> blockLabels = [];
        private readonly HashSet<IVariable> locals = [];
        private readonly Dictionary<IVariable, IrParameterMemoryKey> parameters = [];
        private readonly HashSet<IVariable> compilerTemporaries = [];
        private readonly Dictionary<IVariable, IrValue> localValues = [];
        private readonly HashSet<IVariable> dirtyLocals = [];
        private readonly Dictionary<IrValue, IOperand> valueHomes = [];
        private readonly Dictionary<IrValue, IrInstruction> definitions = [];
        private readonly Dictionary<IOperand, IrValue> externalValues = [];
        private readonly Dictionary<string, int> recordingStatistics = new(StringComparer.Ordinal);
        private readonly List<IrValue> stackValues = [];
        private readonly List<IrBlock> layout = [];
        private IrBlock current;
        private bool finished;
        private bool finalized;
        private int nextIrTemporary;
        private int readOnlyPointerGlobals;

        public IrRoutineBuilder(IRoutineBuilder target, IrNumericSemantics numericSemantics, bool optimize,
            Func<int, INumericOperand>? makeOperand = null, Func<IOperand, bool>? preferConstantHome = null,
            Action<IEnumerable<IrOptimizationStat>>? recordOptimizationStats = null,
            Action<IrRoutineBuilder>? deferFinalization = null, IIrOptimizationCostPolicy? costPolicy = null)
        {
            this.target = target;
            this.optimize = optimize;
            this.preferConstantHome = preferConstantHome;
            this.recordOptimizationStats = recordOptimizationStats;
            this.makeOperand = makeOperand ?? (value => new DeferredNumericOperand(value));
            this.deferFinalization = deferFinalization;
            wordSize = numericSemantics == IrNumericSemantics.ZMachine16 ? 2 : 4;
            optimizer = new RoutineIrOptimizer(numericSemantics, () =>
            {
                if (numericSemantics == IrNumericSemantics.ZMachine16 && locals.Count >= 15)
                    return null;
                var temporary = TrackLocal(target.DefineLocal($"?IR{nextIrTemporary++}"));
                compilerTemporaries.Add(temporary);
                return temporary;
            }, (destination, value) => target.EmitStore(destination, value), () => compilerTemporaries,
                CreateOptimizerPreheader, costPolicy);
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

        internal void SetPropertyRoutineTargets(
            IReadOnlyDictionary<object, IReadOnlySet<IrRoutineEffectSummary>> targets) =>
            optimizer.SetPropertyRoutineTargets(targets);

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

        public ILocalBuilder DefineRequiredParameter(string name) => TrackParameter(target.DefineRequiredParameter(name));

        public ILocalBuilder DefineOptionalParameter(string paramName) =>
            TrackParameter(target.DefineOptionalParameter(paramName));

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
                var leftValue = GetValue(variable);
                PreserveStackValues();
                FlushPromotedLocals();
                var updateOpcode = cond == Condition.IncCheck ? IrOpcode.Add : IrOpcode.Subtract;
                var instruction = AppendLowering(updateOpcode, [leftValue, routine.CreateConstant(1)], IrEffect.Control,
                    _ => target.Branch(cond, variable, right, label, polarity), resultHome: variable);
                SetLocalValue(variable, instruction.Result!);
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
                    resolved => target.Branch(cond, resolved[0], right == null ? null : resolved[1], label, polarity),
                    readRegions: GetReadRegions(opcode),
                    readIdentity: cond == Condition.TestAttr && right != null
                        ? TryGetObjectMemberIdentity(left, right, IrMemoryRegion.Attributes)
                        : null);
                FinishConditional(label, instruction.Result!, polarity, instruction);
                return;
            }
            Record(() => target.Branch(cond, left, right, label, polarity), IrMemoryRegion.None,
                $"Branch.{cond}");
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
            Record(() => target.EmitSave(label, polarity), diagnosticName: "SaveBranch");
            FinishConditional(label);
        }

        public void EmitRestore(ILabel label, bool polarity)
        {
            Record(() => target.EmitRestore(label, polarity), diagnosticName: "RestoreBranch");
            FinishConditional(label);
        }

        public void EmitSave(IVariable result) =>
            Record(() => target.EmitSave(result), diagnosticName: "SaveResult");

        public void EmitRestore(IVariable result) =>
            Record(() => target.EmitRestore(result), diagnosticName: "RestoreResult");

        public void EmitSave(IOperand table, IOperand size, IOperand name, IOperand? prompt, IVariable result) =>
            Record(() => target.EmitSave(table, size, name, prompt, result), diagnosticName: "SaveTable");

        public void EmitRestore(IOperand table, IOperand size, IOperand name, IOperand? prompt, IVariable result) =>
            Record(() => target.EmitRestore(table, size, name, prompt, result), diagnosticName: "RestoreTable");

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
            Record(() => target.EmitScanTable(value, table, length, form, result, label, polarity),
                diagnosticName: "ScanTableUnpromotedResult");
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
            Record(() => target.EmitGetChild(value, result, label, polarity),
                diagnosticName: "GetChildUnpromotedResult");
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
            Record(() => target.EmitGetSibling(value, result, label, polarity),
                diagnosticName: "GetSiblingUnpromotedResult");
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
                Record(() => target.EmitNullary(op, result), diagnosticName: $"Nullary.{op}");
            else
                RecordEffectfulOperation([], _ => target.EmitNullary(op, result), effect, result,
                    (_, home) => target.EmitNullary(op, home));
        }

        public void EmitUnary(UnaryOp op, IOperand value, IVariable? result)
        {
            if (result != null && TryMap(op, out var opcode))
            {
                var promotedResult = locals.Contains(result) || ReferenceEquals(result, Stack);
                var writeIdentity = promotedResult ? null : GetDestinationIdentity(result);
                var instruction = AppendLowering(opcode, [GetValue(value)], IrEffect.None,
                    operands => target.EmitUnary(op, operands[0], result), resultHome: result,
                    emitTo: (operands, home) => target.EmitUnary(op, operands[0], home),
                    writeRegions: promotedResult ? IrMemoryRegion.None : IrMemoryRegion.Globals,
                    writeIdentity: writeIdentity);
                ((IrLoweringOperation)instruction.Payload!).RequiredHome = !promotedResult;
                if (!promotedResult)
                    instruction.WrittenValue = instruction.Result;
                if (promotedResult)
                    SetProducedValue(result, instruction.Result!);
                else
                {
                    valueHomes[instruction.Result!] = result;
                    InvalidateMutableExternalValues();
                }
                return;
            }
            if (result != null && TryMapMemoryRead(op, out opcode))
            {
                var promotedResult = locals.Contains(result) || ReferenceEquals(result, Stack);
                var writeIdentity = promotedResult ? null : GetDestinationIdentity(result);
                var instruction = AppendLowering(opcode, [GetValue(value)], IrEffect.ReadMemory,
                    operands => target.EmitUnary(op, operands[0], result), resultHome: result,
                    emitTo: (operands, home) => target.EmitUnary(op, operands[0], home),
                    readRegions: GetReadRegions(opcode),
                    writeRegions: promotedResult ? IrMemoryRegion.None : IrMemoryRegion.Globals,
                    readIdentity: TryGetMemoryIdentity(value, GetReadRegions(opcode)), writeIdentity: writeIdentity);
                ((IrLoweringOperation)instruction.Payload!).RequiredHome = !promotedResult;
                if (!promotedResult)
                    instruction.WrittenValue = instruction.Result;
                if (promotedResult)
                    SetProducedValue(result, instruction.Result!);
                else
                {
                    valueHomes[instruction.Result!] = result;
                    InvalidateMutableExternalValues();
                }
                return;
            }
            if (TryClassify(op, out var effect))
                RecordEffectfulOperation([value], operands => target.EmitUnary(op, operands[0], result), effect, result,
                    (operands, home) => target.EmitUnary(op, operands[0], home), GetWriteRegions(op));
            else
                RecordTemporaryOperation([value], operands => target.EmitUnary(op, operands[0], result), result,
                    $"Unary.{op}");
        }

        public void EmitBinary(BinaryOp op, IOperand left, IOperand right, IVariable? result)
        {
            if (op == BinaryOp.StoreIndirect && left is IIndirectOperand { Variable: var variable } &&
                locals.Contains(variable))
            {
                Record(() => target.EmitBinary(op, left, right, result), IrMemoryRegion.None,
                    $"Binary.{op}.IndirectLocal");
                return;
            }
            if (result != null && TryMapMemoryRead(op, out var loadOpcode))
            {
                var promotedResult = locals.Contains(result) || ReferenceEquals(result, Stack);
                var writeIdentity = promotedResult ? null : GetDestinationIdentity(result);
                var leftValue = GetValue(left);
                var rightValue = GetValue(right);
                var instruction = AppendLowering(loadOpcode, [leftValue, rightValue], IrEffect.ReadMemory,
                    operands => target.EmitBinary(op, operands[0], operands[1], result), resultHome: result,
                    emitTo: (operands, home) => target.EmitBinary(op, operands[0], operands[1], home),
                    readRegions: GetReadRegions(loadOpcode),
                    writeRegions: promotedResult ? IrMemoryRegion.None : IrMemoryRegion.Globals,
                    readIdentity: loadOpcode == IrOpcode.LoadProperty
                        ? TryGetObjectMemberIdentity(left, right, IrMemoryRegion.Properties)
                        : TryGetTableMemoryIdentity(left, right,
                            loadOpcode == IrOpcode.LoadByte ? 1 : wordSize,
                            loadOpcode == IrOpcode.LoadByte ? 1 : wordSize) ??
                            TryGetTableMemoryIdentity(leftValue, rightValue,
                                loadOpcode == IrOpcode.LoadByte ? 1 : wordSize,
                                loadOpcode == IrOpcode.LoadByte ? 1 : wordSize),
                    writeIdentity: writeIdentity);
                ((IrLoweringOperation)instruction.Payload!).RequiredHome = !promotedResult;
                if (!promotedResult)
                    instruction.WrittenValue = instruction.Result;
                if (promotedResult)
                    SetProducedValue(result, instruction.Result!);
                else
                {
                    valueHomes[instruction.Result!] = result;
                    InvalidateMutableExternalValues();
                }
                return;
            }
            if (result != null && TryMap(op, out var opcode))
            {
                var promotedResult = locals.Contains(result) || ReferenceEquals(result, Stack);
                var writeIdentity = promotedResult ? null : GetDestinationIdentity(result);
                var instruction = AppendLowering(opcode, [GetValue(left), GetValue(right)], IrEffect.None,
                    operands => target.EmitBinary(op, operands[0], operands[1], result), resultHome: result,
                    emitTo: (operands, home) => target.EmitBinary(op, operands[0], operands[1], home),
                    writeRegions: promotedResult ? IrMemoryRegion.None : IrMemoryRegion.Globals,
                    writeIdentity: writeIdentity);
                SetDerivedAddressIdentity(instruction);
                ((IrLoweringOperation)instruction.Payload!).RequiredHome = !promotedResult;
                if (!promotedResult)
                    instruction.WrittenValue = instruction.Result;
                if (promotedResult)
                    SetProducedValue(result, instruction.Result!);
                else
                {
                    valueHomes[instruction.Result!] = result;
                    InvalidateMutableExternalValues();
                }
                return;
            }
            if (TryClassify(op, out var effect))
            {
                RecordEffectfulOperation([left, right],
                    operands => target.EmitBinary(op, operands[0], operands[1], result), effect, result,
                    (operands, home) => target.EmitBinary(op, operands[0], operands[1], home), GetWriteRegions(op),
                    op is BinaryOp.SetFlag or BinaryOp.ClearFlag
                        ? TryGetObjectMemberIdentity(left, right, IrMemoryRegion.Attributes)
                        : null);
                return;
            }
            RecordTemporaryOperation([left, right],
                operands => target.EmitBinary(op, operands[0], operands[1], result), result, $"Binary.{op}");
        }

        public void EmitTernary(TernaryOp op, IOperand left, IOperand center, IOperand right, IVariable? result)
        {
            if (TryClassify(op, out var effect))
            {
                RecordEffectfulOperation([left, center, right],
                    operands => target.EmitTernary(op, operands[0], operands[1], operands[2], result), effect, result,
                    (operands, home) => target.EmitTernary(op, operands[0], operands[1], operands[2], home),
                    GetWriteRegions(op), op switch
                    {
                        TernaryOp.PutByte => TryGetTableMemoryIdentity(left, center, 1, 1),
                        TernaryOp.PutWord => TryGetTableMemoryIdentity(left, center, wordSize, wordSize),
                        TernaryOp.PutProperty =>
                            TryGetObjectMemberIdentity(left, center, IrMemoryRegion.Properties),
                        _ => TryGetMemoryIdentity(left, GetWriteRegions(op)),
                    }, op is TernaryOp.PutByte or TernaryOp.PutWord or TernaryOp.PutProperty ? 2 : null);
                return;
            }
            RecordTemporaryOperation([left, center, right],
                operands => target.EmitTernary(op, operands[0], operands[1], operands[2], result), result,
                $"Ternary.{op}");
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
            IVariable? result) => Record(() => target.EmitRead(chrbuf, lexbuf, interval, routineOperand, result),
                diagnosticName: "Read");

        public void EmitReadChar(IOperand? interval, IOperand? routineOperand, IVariable result) =>
            Record(() => target.EmitReadChar(interval, routineOperand, result), diagnosticName: "ReadChar");

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
            var calleeSummary = (routineOperand as IrRoutineBuilder)?.EffectSummary;
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
            var callSummary = calleeSummary == null ? null : BindCallSummary(calleeSummary, values.Skip(1).ToArray());
            PreserveStackValues();
            if (result != null && (locals.Contains(result) || ReferenceEquals(result, Stack)))
            {
                var instruction = AppendLowering(IrOpcode.TargetOperation, values, IrEffect.Call,
                    resolved => target.EmitCall(resolved[0], resolved.Skip(1).ToArray(), result), resultHome: result,
                    emitTo: (resolved, home) => target.EmitCall(resolved[0], resolved.Skip(1).ToArray(), home),
                    callSummary: callSummary);
                instruction.CallBindings = values.Skip(1).Select(GetMemoryBinding).ToArray();
                SetProducedValue(result, instruction.Result!);
                dirtyLocals.Remove(result);
            }
            else
            {
                var instruction = AppendLowering(IrOpcode.TargetOperation, values, IrEffect.Call,
                    resolved => target.EmitCall(resolved[0], resolved.Skip(1).ToArray(), result), hasResult: false,
                    callSummary: callSummary);
                instruction.CallBindings = values.Skip(1).Select(GetMemoryBinding).ToArray();
            }
            InvalidateMutableExternalValues(callSummary);
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
                    writeRegions: IrMemoryRegion.Globals,
                    writeIdentity: new IrMemoryIdentity(IrMemoryRegion.Globals, GetStableMemoryKey(dest)));
                externalValues.Remove(dest);
                return;
            }
            Record(() => target.EmitStore(dest, src), diagnosticName: "StoreIndirect");
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

    }
}
