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
                : value.Constant is int constant ? makeOperand(constant) : value.PhysicalHome;
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
            if (instruction.Opcode == IrOpcode.Copy ||
                instruction.Payload is IrLoweringOperation { IsMaterialization: true } &&
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

    }
}

