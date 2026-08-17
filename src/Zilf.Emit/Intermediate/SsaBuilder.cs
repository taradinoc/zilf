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
    /// Constructs pruned SSA incrementally using sealed basic blocks.
    /// </summary>
    internal sealed class SsaBuilder
    {
        private readonly RoutineIr routine;
        private readonly Dictionary<(IrBlock Block, object Variable), IrValue> definitions = [];
        private readonly Dictionary<IrBlock, Dictionary<object, IrInstruction>> incompletePhis = [];
        private readonly HashSet<IrBlock> sealedBlocks = [];

        public SsaBuilder(RoutineIr routine) => this.routine = routine;

        public void WriteVariable(IrBlock block, object variable, IrValue value) => definitions[(block, variable)] = value;

        public IrValue ReadVariable(IrBlock block, object variable)
        {
            if (definitions.TryGetValue((block, variable), out var value))
                return value;

            return ReadVariableRecursive(block, variable);
        }

        public void SealBlock(IrBlock block)
        {
            if (!sealedBlocks.Add(block))
                return;

            if (!incompletePhis.Remove(block, out var phis))
                return;

            foreach (var pair in phis)
            {
                AddPhiOperands(block, pair.Key, pair.Value);
                TryRemoveTrivialPhi(pair.Value);
            }
        }

        private IrValue ReadVariableRecursive(IrBlock block, object variable)
        {
            IrValue value;
            if (!sealedBlocks.Contains(block))
            {
                var phi = CreatePhi(block);
                if (!incompletePhis.TryGetValue(block, out var phis))
                {
                    phis = [];
                    incompletePhis.Add(block, phis);
                }
                phis.Add(variable, phi);
                value = phi.Result!;
            }
            else if (block.Predecessors.Count == 0)
            {
                throw new InvalidOperationException($"Variable '{variable}' is read before it is defined in {block}.");
            }
            else if (block.Predecessors.Count == 1)
            {
                value = ReadVariable(block.Predecessors[0], variable);
            }
            else
            {
                var phi = CreatePhi(block);
                value = phi.Result!;
                definitions[(block, variable)] = value;
                AddPhiOperands(block, variable, phi);
                value = TryRemoveTrivialPhi(phi);
            }

            definitions[(block, variable)] = value;
            return value;
        }

        private IrInstruction CreatePhi(IrBlock block)
        {
            var phi = new IrInstruction(IrOpcode.Phi, routine.CreateValue(), [], payload: new List<IrBlock>());
            block.Instructions.Insert(0, phi);
            return phi;
        }

        private void AddPhiOperands(IrBlock block, object variable, IrInstruction phi)
        {
            var incomingBlocks = (List<IrBlock>)phi.Payload!;
            foreach (var predecessor in block.Predecessors)
            {
                incomingBlocks.Add(predecessor);
                phi.Operands.Add(ReadVariable(predecessor, variable));
            }
        }

        private IrValue TryRemoveTrivialPhi(IrInstruction phi)
        {
            var same = phi.Operands.FirstOrDefault(operand => !ReferenceEquals(operand, phi.Result));
            if (same == null || phi.Operands.Any(operand => !ReferenceEquals(operand, same) && !ReferenceEquals(operand, phi.Result)))
                return phi.Result!;

            foreach (var block in routine.Blocks)
            {
                foreach (var instruction in block.Instructions)
                    instruction.ReplaceOperand(phi.Result!, same);

                block.Terminator = block.Terminator switch
                {
                    IrTerminator.Branch branch when ReferenceEquals(branch.Condition, phi.Result) =>
                        branch with { Condition = same },
                    IrTerminator.Return ret when ReferenceEquals(ret.Value, phi.Result) => ret with { Value = same },
                    _ => block.Terminator,
                };
            }

            foreach (var block in routine.Blocks)
                block.Instructions.Remove(phi);

            var keys = definitions.Where(pair => ReferenceEquals(pair.Value, phi.Result)).Select(static pair => pair.Key).ToArray();
            foreach (var key in keys)
                definitions[key] = same;

            return same;
        }
    }
}
