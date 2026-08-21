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
