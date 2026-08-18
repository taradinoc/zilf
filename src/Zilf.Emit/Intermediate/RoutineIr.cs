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

        public Action<IReadOnlyList<IOperand>, IVariable?>? EmitTo { get; }

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

    internal sealed class IrRoutineEffectSummary
    {
        private readonly List<IrRoutineEffectSummary> callees = [];

        public IrMemoryRegion DirectWrites { get; set; }

        public bool IsComplete { get; set; }

        public void AddCallee(IrRoutineEffectSummary callee) => callees.Add(callee);

        public IrMemoryRegion GetWrittenRegions() => GetWrittenRegions([]);

        private IrMemoryRegion GetWrittenRegions(HashSet<IrRoutineEffectSummary> active)
        {
            if (!IsComplete)
                return IrMemoryRegion.All;
            if (!active.Add(this))
                return DirectWrites;
            var result = DirectWrites;
            foreach (var callee in callees)
                result |= callee.GetWrittenRegions(active);
            active.Remove(this);
            return result;
        }
    }

    internal enum IrOpcode
    {
        Constant,
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
        Phi,
        TargetOperation,
    }

    internal sealed class IrValue
    {
        internal IrValue(int id, int? constant = null, bool mutableExternal = false)
        {
            Id = id;
            Constant = constant;
            MutableExternal = mutableExternal;
        }

        public int Id { get; }

        public int? Constant { get; }

        public bool MutableExternal { get; }

        public override string ToString() => Constant is int value ? value.ToString() : $"%{Id}";
    }

    internal sealed class IrInstruction
    {
        private readonly List<IrValue> operands;

        internal IrInstruction(IrOpcode opcode, IrValue? result, IEnumerable<IrValue> operands,
            IrEffect effect = IrEffect.None, object? payload = null, IrMemoryRegion readRegions = IrMemoryRegion.None,
            IrMemoryRegion writeRegions = IrMemoryRegion.None, IrRoutineEffectSummary? callSummary = null)
        {
            Opcode = opcode;
            Result = result;
            this.operands = [.. operands];
            Effect = effect;
            Payload = payload;
            ReadRegions = readRegions;
            WriteRegions = writeRegions;
            CallSummary = callSummary;
        }

        public IrOpcode Opcode { get; set; }

        public IrValue? Result { get; }

        public IList<IrValue> Operands => operands;

        public IrEffect Effect { get; }

        public object? Payload { get; set; }

        public IrMemoryRegion ReadRegions { get; }

        public IrMemoryRegion WriteRegions { get; }

        public IrRoutineEffectSummary? CallSummary { get; }

        public bool IsPure => Effect == IrEffect.None;

        public void ReplaceOperand(IrValue oldValue, IrValue newValue)
        {
            for (var i = 0; i < operands.Count; i++)
            {
                if (ReferenceEquals(operands[i], oldValue))
                    operands[i] = newValue;
            }
        }
    }

    internal abstract record IrTerminator
    {
        public sealed record Jump(IrBlock Target) : IrTerminator;

        public sealed record Branch(IrValue Condition, IrBlock WhenTrue, IrBlock WhenFalse) : IrTerminator;

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

        public IrValue CreateExternalValue(bool mutable) => new(nextValueId++, mutableExternal: mutable);

        public IrValue CreateConstant(int value) => new(nextValueId++, value);

        public IrInstruction Append(IrBlock block, IrOpcode opcode, IEnumerable<IrValue> operands,
            IrEffect effect = IrEffect.None, object? payload = null, bool hasResult = true,
            IrMemoryRegion readRegions = IrMemoryRegion.None, IrMemoryRegion writeRegions = IrMemoryRegion.None,
            IrRoutineEffectSummary? callSummary = null)
        {
            var instruction = new IrInstruction(opcode, hasResult ? CreateValue() : null, operands, effect, payload,
                readRegions, writeRegions, callSummary);
            block.Instructions.Add(instruction);
            return instruction;
        }

        public void RebuildPredecessors()
        {
            foreach (var block in blocks)
                block.ClearPredecessors();

            foreach (var block in blocks)
            {
                foreach (var successor in GetSuccessors(block))
                    successor.AddPredecessor(block);
            }
        }

        public static IEnumerable<IrBlock> GetSuccessors(IrBlock block) => block.Terminator switch
        {
            IrTerminator.Jump jump => [jump.Target],
            IrTerminator.Branch branch when ReferenceEquals(branch.WhenTrue, branch.WhenFalse) => [branch.WhenTrue],
            IrTerminator.Branch branch => [branch.WhenTrue, branch.WhenFalse],
            _ => [],
        };

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
