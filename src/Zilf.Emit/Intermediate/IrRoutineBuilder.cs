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
        private readonly RoutineIrOptimizer optimizer;
        private readonly bool optimize;
        private readonly Func<int, INumericOperand> makeOperand;
        private readonly Dictionary<ILabel, IrBlock> labelBlocks = [];
        private readonly HashSet<IVariable> locals = [];
        private readonly HashSet<IVariable> compilerTemporaries = [];
        private readonly Dictionary<IVariable, IrValue> localValues = [];
        private readonly HashSet<IVariable> dirtyLocals = [];
        private readonly Dictionary<IrValue, IOperand> valueHomes = [];
        private readonly List<IrBlock> layout = [];
        private IrBlock current;
        private bool finished;

        public IrRoutineBuilder(IRoutineBuilder target, IrNumericSemantics numericSemantics, bool optimize,
            Func<int, INumericOperand>? makeOperand = null)
        {
            this.target = target;
            this.optimize = optimize;
            this.makeOperand = makeOperand ?? (value => new DeferredNumericOperand(value));
            optimizer = new RoutineIrOptimizer(numericSemantics);
            current = routine.Entry;
            layout.Add(current);
            labelBlocks.Add(target.RoutineStart, current);

            var trueBlock = GetLabelBlock(target.RTrue);
            trueBlock.Terminator = new IrTerminator.Return(routine.CreateConstant(1));
            var falseBlock = GetLabelBlock(target.RFalse);
            falseBlock.Terminator = new IrTerminator.Return(routine.CreateConstant(0));
        }

        internal IRoutineBuilder Target => target;

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
            FlushPromotedLocals();
            var block = GetLabelBlock(label);
            if (!ReferenceEquals(current, block) && current.Terminator == null)
                current.Terminator = new IrTerminator.Jump(block);
            current = block;
            if (!layout.Contains(block))
                layout.Add(block);
            RecordRaw(() => target.MarkLabel(label));
            localValues.Clear();
            dirtyLocals.Clear();
        }

        public void Branch(ILabel label)
        {
            Record(() => target.Branch(label));
            current.Terminator = new IrTerminator.Jump(GetLabelBlock(label));
            StartFallthrough();
        }

        public void Branch(Condition cond, IOperand? left, IOperand? right, ILabel label, bool polarity)
        {
            Record(() => target.Branch(cond, left, right, label, polarity));
            FinishConditional(label);
        }

        public void BranchIfZero(IOperand operand, ILabel label, bool polarity)
        {
            Record(() => target.BranchIfZero(operand, label, polarity));
            FinishConditional(label);
        }

        public void BranchIfEqual(IOperand value, IOperand option1, ILabel label, bool polarity)
        {
            Record(() => target.BranchIfEqual(value, option1, label, polarity));
            FinishConditional(label);
        }

        public void BranchIfEqual(IOperand value, IOperand option1, IOperand option2, ILabel label, bool polarity)
        {
            Record(() => target.BranchIfEqual(value, option1, option2, label, polarity));
            FinishConditional(label);
        }

        public void BranchIfEqual(IOperand value, IOperand option1, IOperand option2, IOperand option3,
            ILabel label, bool polarity)
        {
            Record(() => target.BranchIfEqual(value, option1, option2, option3, label, polarity));
            FinishConditional(label);
        }

        public void Return(IOperand result)
        {
            var value = GetValue(result);
            AppendLowering(IrOpcode.TargetOperation, [value], IrEffect.Opaque,
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
            Record(() => target.EmitScanTable(value, table, length, form, result, label, polarity));
            FinishConditional(label);
        }

        public void EmitGetChild(IOperand value, IVariable result, ILabel label, bool polarity)
        {
            Record(() => target.EmitGetChild(value, result, label, polarity));
            FinishConditional(label);
        }

        public void EmitGetSibling(IOperand value, IVariable result, ILabel label, bool polarity)
        {
            Record(() => target.EmitGetSibling(value, result, label, polarity));
            FinishConditional(label);
        }

        public void EmitNullary(NullaryOp op, IVariable? result) => Record(() => target.EmitNullary(op, result));

        public void EmitUnary(UnaryOp op, IOperand value, IVariable? result)
        {
            if (result != null && locals.Contains(result) && TryMap(op, out var opcode))
            {
                var instruction = AppendLowering(opcode, [GetValue(value)], IrEffect.None,
                    operands => target.EmitUnary(op, operands[0], result), resultHome: result);
                SetLocalValue(result, instruction.Result!);
                return;
            }
            Record(() => target.EmitUnary(op, value, result));
        }

        public void EmitBinary(BinaryOp op, IOperand left, IOperand right, IVariable? result)
        {
            if (op == BinaryOp.StoreIndirect && left is IIndirectOperand { Variable: var variable } &&
                locals.Contains(variable))
            {
                Record(() => target.EmitBinary(op, left, right, result));
                return;
            }
            if (result != null && locals.Contains(result) && TryMap(op, out var opcode))
            {
                var instruction = AppendLowering(opcode, [GetValue(left), GetValue(right)], IrEffect.None,
                    operands => target.EmitBinary(op, operands[0], operands[1], result), resultHome: result);
                SetLocalValue(result, instruction.Result!);
                return;
            }
            Record(() => target.EmitBinary(op, left, right, result));
        }

        public void EmitTernary(TernaryOp op, IOperand left, IOperand center, IOperand right, IVariable? result) =>
            Record(() => target.EmitTernary(op, left, center, right, result));

        public void EmitPrint(string text, bool crlfRtrue)
        {
            Record(() => target.EmitPrint(text, crlfRtrue));
            if (crlfRtrue)
            {
                current.Terminator = new IrTerminator.Return(routine.CreateConstant(1));
                StartFallthrough();
            }
        }

        public void EmitPrint(PrintOp op, IOperand value) => Record(() => target.EmitPrint(op, value));

        public void EmitPrintTable(IOperand table, IOperand width, IOperand? height, IOperand? skip) =>
            Record(() => target.EmitPrintTable(table, width, height, skip));

        public void EmitPrintNewLine() => Record(target.EmitPrintNewLine);

        public void EmitRead(IOperand chrbuf, IOperand? lexbuf, IOperand? interval, IOperand? routineOperand,
            IVariable? result) => Record(() => target.EmitRead(chrbuf, lexbuf, interval, routineOperand, result));

        public void EmitReadChar(IOperand? interval, IOperand? routineOperand, IVariable result) =>
            Record(() => target.EmitReadChar(interval, routineOperand, result));

        public void EmitPlaySound(IOperand number, IOperand? effect, IOperand? volume, IOperand? routineOperand) =>
            Record(() => target.EmitPlaySound(number, effect, volume, routineOperand));

        public void EmitEncodeText(IOperand src, IOperand length, IOperand srcOffset, IOperand dest) =>
            Record(() => target.EmitEncodeText(src, length, srcOffset, dest));

        public void EmitTokenize(IOperand text, IOperand parse, IOperand? dictionary, IOperand? flag) =>
            Record(() => target.EmitTokenize(text, parse, dictionary, flag));

        public void EmitCall(IOperand routineOperand, IOperand[] args, IVariable? result)
        {
            var capturedArgs = (IOperand[])args.Clone();
            Record(() => target.EmitCall(routineOperand, capturedArgs, result));
        }

        public void EmitStore(IVariable dest, IOperand src)
        {
            if (compilerTemporaries.Contains(dest) && !ReferenceEquals(src, Stack))
            {
                var copy = AppendLowering(IrOpcode.Copy, [GetValue(src)], IrEffect.None, _ => { }, resultHome: dest);
                SetLocalValue(dest, copy.Result!);
                return;
            }
            Record(() => target.EmitStore(dest, src));
        }

        public void EmitPopStack() => Record(target.EmitPopStack);

        public void EmitPushUserStack(IOperand value, IOperand stack, ILabel label, bool polarity)
        {
            Record(() => target.EmitPushUserStack(value, stack, label, polarity));
            FinishConditional(label);
        }

        public void Finish()
        {
            if (finished)
                throw new InvalidOperationException("The routine has already been finished.");
            finished = true;

            foreach (var block in routine.Blocks)
                block.Terminator ??= new IrTerminator.Return(null);

            FlushPromotedLocals();

            if (optimize)
                optimizer.Optimize(routine);
            else
                routine.Verify();

            foreach (var block in layout)
            {
                if (!routine.Blocks.Contains(block))
                    continue;
                foreach (var instruction in block.Instructions)
                {
                    if (instruction.Payload is IrLoweringOperation lowering)
                        lowering.Emit(instruction.Operands.Select(ResolveOperand).ToArray());
                    else
                        ((Action)instruction.Payload!).Invoke();
                    if (instruction.Result != null && instruction.Payload is IrLoweringOperation &&
                        !valueHomes.ContainsKey(instruction.Result))
                        throw new InvalidOperationException($"No physical home was assigned to {instruction.Result}.");
                }
            }

            target.Finish();
        }

        public override string ToString() => target.ToString()!;

        protected void RecordExtension(Action action) => Record(action);

        internal void RecordTargetAction(Action action) => Record(action);

        private void Record(Action action)
        {
            FlushPromotedLocals();
            RecordRaw(action);
            localValues.Clear();
            dirtyLocals.Clear();
        }

        private void RecordRaw(Action action) => routine.Append(
            current,
            IrOpcode.TargetOperation,
            [],
            IrEffect.Opaque,
            action,
            hasResult: false);

        private IrInstruction AppendLowering(IrOpcode opcode, IReadOnlyList<IrValue> operands, IrEffect effect,
            Action<IReadOnlyList<IOperand>> emit, bool hasResult = true, IVariable? resultHome = null)
        {
            var instruction = routine.Append(current, opcode, operands, effect,
                new IrLoweringOperation(emit, resultHome), hasResult);
            return instruction;
        }

        private ILocalBuilder TrackLocal(ILocalBuilder local)
        {
            locals.Add(local);
            return local;
        }

        private IrValue GetValue(IOperand operand)
        {
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
                localValues[variable] = localValue;
                valueHomes[localValue] = variable;
                return localValue;
            }
            var external = routine.CreateValue();
            valueHomes[external] = operand;
            return external;
        }

        private void SetLocalValue(IVariable variable, IrValue value)
        {
            localValues[variable] = value;
            dirtyLocals.Add(variable);
            valueHomes[value] = variable;
        }

        private void FlushPromotedLocals()
        {
            foreach (var variable in dirtyLocals)
            {
                var value = localValues[variable];
                AppendLowering(IrOpcode.TargetOperation, [value], IrEffect.Opaque,
                    operands => target.EmitStore(variable, operands[0]), hasResult: false);
            }
            dirtyLocals.Clear();
        }

        private IOperand ResolveOperand(IrValue value)
        {
            if (valueHomes.TryGetValue(value, out var operand))
                return operand;
            if (value.Constant is int constant)
                return makeOperand(constant);
            throw new InvalidOperationException($"No physical operand is available for {value}.");
        }

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

        private sealed class DeferredNumericOperand(int value) : INumericOperand
        {
            public int Value { get; } = value;

            public IConstantOperand Add(IConstantOperand other) => throw new NotSupportedException();
        }

        private void RecordTerminator(Action action)
        {
            Record(action);
            current.Terminator = new IrTerminator.Return(null);
            StartFallthrough();
        }

        private void FinishConditional(ILabel label)
        {
            var fallthrough = CreateLayoutBlock();
            current.Terminator = new IrTerminator.Branch(routine.CreateValue(), GetLabelBlock(label), fallthrough);
            current = fallthrough;
        }

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
            return block;
        }
    }
}
