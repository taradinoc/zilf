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

    internal sealed partial class RoutineIrOptimizer
    {
        private readonly IrNumericSemantics numericSemantics;
        private readonly Func<IVariable?> acquireTemporary;
        private readonly Func<IEnumerable<IVariable>> reusableTemporaries;
        private readonly Action<IVariable, IOperand>? emitCopy;
        private readonly Func<IrBlock, IrBlock>? createPreheader;
        private readonly Dictionary<string, int> statistics = new(StringComparer.Ordinal);

        public RoutineIrOptimizer(IrNumericSemantics numericSemantics = IrNumericSemantics.Glulx32,
            Func<IVariable?>? acquireTemporary = null, Action<IVariable, IOperand>? emitCopy = null,
            Func<IEnumerable<IVariable>>? reusableTemporaries = null,
            Func<IrBlock, IrBlock>? createPreheader = null)
        {
            this.numericSemantics = numericSemantics;
            this.acquireTemporary = acquireTemporary ?? (() => null);
            this.emitCopy = emitCopy;
            this.reusableTemporaries = reusableTemporaries ?? (() => []);
            this.createPreheader = createPreheader;
        }

        public void Optimize(RoutineIr routine)
        {
            statistics.Clear();
            routine.Verify();
            statistics["Input instructions"] = routine.Blocks.Sum(block => block.Instructions.Count);
            statistics["Input opaque operations"] = routine.Blocks.Sum(block => block.Instructions.Count(instruction =>
                instruction.Effect == IrEffect.Opaque));
            Record("Exact memory locations", routine.Blocks.SelectMany(block => block.Instructions)
                .SelectMany(instruction => new[] { instruction.ReadIdentity, instruction.WriteIdentity })
                .OfType<IrMemoryIdentity>().Distinct().Count());
            Record("Calls with precise effects", routine.Blocks.SelectMany(block => block.Instructions)
                .Count(instruction => instruction.Effect == IrEffect.Call && instruction.CallSummary != null &&
                    instruction.CallSummary.GetUnknownWrittenRegions() != IrMemoryRegion.All));
            ProtectCopiesAcrossClobbers(routine);
            CoalesceMaterializationDestinations(routine);
            RemoveRedundantMaterializations(routine);
            ForwardCopyDestinations(routine);
            Record("SCCP constants", SparseConditionalConstantPropagation(routine));
            SimplifyControlFlow(routine);
            RemoveUnreachableBlocks(routine);
            CreateLoopPreheaders(routine);
            SimplifyInductionVariables(routine);
            LoopInvariantCodeMotion(routine);
            GlobalValueNumbering(routine);
            var changed = true;
            while (changed)
            {
                changed = FoldConstantsAndCopies(routine);
                changed |= SimplifyControlFlow(routine);
                changed |= RemoveDeadInstructions(routine);
                changed |= RemoveUnreachableBlocks(routine);
            }
            routine.Verify();
            statistics["Output instructions"] = routine.Blocks.Sum(block => block.Instructions.Count);
            statistics["Output opaque operations"] = routine.Blocks.Sum(block => block.Instructions.Count(instruction =>
                instruction.Effect == IrEffect.Opaque));
        }

        public IEnumerable<IrOptimizationStat> GetStatistics() => statistics
            .OrderBy(pair => pair.Key, StringComparer.Ordinal)
            .Select(pair => new IrOptimizationStat(pair.Key, pair.Value));

        private void Record(string name, int count = 1)
        {
            if (count != 0)
                statistics[name] = statistics.GetValueOrDefault(name) + count;
        }

    }
}

