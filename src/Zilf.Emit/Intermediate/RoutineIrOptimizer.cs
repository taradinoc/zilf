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
    internal enum IrNumericSemantics
    {
        ZMachine16,
        Glulx32,
    }

    internal sealed class RoutineIrOptimizer
    {
        private readonly IrNumericSemantics numericSemantics;

        public RoutineIrOptimizer(IrNumericSemantics numericSemantics = IrNumericSemantics.Glulx32)
        {
            this.numericSemantics = numericSemantics;
        }

        public void Optimize(RoutineIr routine)
        {
            routine.Verify();
            var changed = true;
            while (changed)
            {
                changed = FoldConstantsAndCopies(routine);
                changed |= SimplifyControlFlow(routine);
                changed |= RemoveDeadInstructions(routine);
                changed |= RemoveUnreachableBlocks(routine);
            }
            routine.Verify();
        }

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

                    if (instruction.Opcode == IrOpcode.Copy && instruction.Operands.Count == 1)
                    {
                        replacements[instruction.Result] = instruction.Operands[0];
                        continue;
                    }

                    if (instruction.Opcode == IrOpcode.Phi &&
                        instruction.Operands.Count > 0 &&
                        instruction.Operands.All(operand => ReferenceEquals(operand, instruction.Operands[0])))
                    {
                        replacements[instruction.Result] = instruction.Operands[0];
                        continue;
                    }

                    if (TryFold(instruction, out var value))
                        replacements[instruction.Result] = routine.CreateConstant(value);
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

        private bool TryFold(IrInstruction instruction, out int value)
        {
            value = 0;
            if (instruction.Operands.Any(static operand => operand.Constant == null))
                return false;

            var operands = instruction.Operands
                .Select(operand => Normalize(operand.Constant!.Value))
                .ToArray();
            try
            {
                var unnormalized = instruction.Opcode switch
                {
                    IrOpcode.Add when operands.Length == 2 => unchecked(operands[0] + operands[1]),
                    IrOpcode.Subtract when operands.Length == 2 => unchecked(operands[0] - operands[1]),
                    IrOpcode.Multiply when operands.Length == 2 => unchecked(operands[0] * operands[1]),
                    IrOpcode.Divide when operands.Length == 2 && operands[1] != 0 => operands[0] / operands[1],
                    IrOpcode.Modulo when operands.Length == 2 && operands[1] != 0 => operands[0] % operands[1],
                    IrOpcode.BitwiseAnd when operands.Length == 2 => operands[0] & operands[1],
                    IrOpcode.BitwiseOr when operands.Length == 2 => operands[0] | operands[1],
                    IrOpcode.BitwiseNot when operands.Length == 1 => ~operands[0],
                    IrOpcode.ShiftLeft when operands.Length == 2 && IsValidShift(operands[1]) =>
                        operands[0] << operands[1],
                    IrOpcode.ShiftRight when operands.Length == 2 && IsValidShift(operands[1]) =>
                        operands[0] >> operands[1],
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

        private static bool SimplifyControlFlow(RoutineIr routine)
        {
            var changed = false;
            foreach (var block in routine.Blocks)
            {
                if (block.Terminator is IrTerminator.Branch { Condition.Constant: int value } branch)
                {
                    block.Terminator = new IrTerminator.Jump(value != 0 ? branch.WhenTrue : branch.WhenFalse);
                    changed = true;
                }
                else if (block.Terminator is IrTerminator.Branch same && ReferenceEquals(same.WhenTrue, same.WhenFalse))
                {
                    block.Terminator = new IrTerminator.Jump(same.WhenTrue);
                    changed = true;
                }
            }
            return changed;
        }

        private static bool RemoveDeadInstructions(RoutineIr routine)
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
                    if (instruction.Result != null && instruction.IsPure && !used.Contains(instruction.Result))
                    {
                        block.Instructions.RemoveAt(i);
                        changed = true;
                    }
                }
            }
            return changed;
        }

        private static bool RemoveUnreachableBlocks(RoutineIr routine)
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
                    removed = true;
                }
            }
            return removed;
        }
    }
}
