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

            if (deferFinalization != null)
            {
                routine.Verify();
                deferFinalization(this);
                return;
            }

            FinalizeRoutine();
        }

        internal void FinalizeRoutine()
        {
            if (!finished)
                throw new InvalidOperationException("The routine has not been finished.");
            if (finalized)
                throw new InvalidOperationException("The routine has already been finalized.");
            finalized = true;

            IEnumerable<IrOptimizationStat> optimizationStatistics;
            var phiCount = routine.PromoteLocalsToSsa(locals);
            if (optimize)
            {
                optimizer.Optimize(routine);
                optimizationStatistics = optimizer.GetStatistics().Concat(phiCount == 0
                    ? []
                    : [new IrOptimizationStat("SSA phi nodes", phiCount)]);
            }
            else
            {
                routine.Verify();
                optimizationStatistics = [];
            }
            recordOptimizationStats?.Invoke(optimizationStatistics.Concat(recordingStatistics.Select(pair =>
                new IrOptimizationStat($"Recorded opaque: {pair.Key}", pair.Value))).Concat(
                readOnlyPointerGlobals == 0 ? [] :
                [new IrOptimizationStat("Read-only table-pointer globals", readOnlyPointerGlobals)]));

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
                    if (instruction.Opcode == IrOpcode.Phi)
                        continue;
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

        internal static void FinalizeRoutines(IReadOnlyCollection<IrRoutineBuilder> routines)
        {
            IrRoutineEffectSummary.Close(routines.Select(routine => routine.effectSummary));
            var hasUnknownGlobalWrite = routines.Any(routine =>
                (routine.effectSummary.GetUnknownWrittenRegions() & IrMemoryRegion.Globals) != 0);
            var writtenGlobals = routines.SelectMany(routine => routine.effectSummary.GetWrittenIdentities())
                .Where(identity => identity.Region == IrMemoryRegion.Globals).Select(identity => identity.Key)
                .ToHashSet();
            var readOnlyGlobals = hasUnknownGlobalWrite
                ? []
                : routines.SelectMany(routine => routine.routine.Blocks).SelectMany(block => block.Instructions)
                    .SelectMany(instruction => instruction.Operands).Select(value => value.PhysicalHome)
                    .OfType<IGlobalBuilder>()
                    .Where(global => global.DefaultValue is IMemoryAddressOperand && !writtenGlobals.Contains(global))
                    .ToHashSet();
            foreach (var routine in routines)
            {
                routine.PrepareReadOnlyPointerGlobals(readOnlyGlobals);
                routine.FinalizeRoutine();
            }
        }

        private void PrepareReadOnlyPointerGlobals(IReadOnlySet<IGlobalBuilder> readOnlyGlobals)
        {
            var used = new HashSet<IGlobalBuilder>();
            foreach (var instruction in routine.Blocks.SelectMany(block => block.Instructions)
                .Where(instruction => instruction.Opcode is IrOpcode.LoadByte or IrOpcode.LoadWord &&
                    instruction.Operands.Count >= 2 && instruction.ReadIdentity == null))
            {
                if (instruction.Operands[0].PhysicalHome is not IGlobalBuilder global ||
                    !readOnlyGlobals.Contains(global) ||
                    global.DefaultValue is not IMemoryAddressOperand address ||
                    !address.TryGetMemoryAddress(out var allocation, out var baseOffset))
                    continue;
                var width = instruction.Opcode == IrOpcode.LoadByte ? 1 : wordSize;
                if (instruction.Operands[1].Constant is int index)
                {
                    var offset = (long)baseOffset + (long)index * width;
                    if (offset is < int.MinValue or > int.MaxValue)
                        continue;
                    instruction.ReadIdentity = new IrMemoryIdentity(IrMemoryRegion.Tables, allocation, (int)offset,
                        width);
                }
                else
                    instruction.ReadIdentity = new IrMemoryIdentity(IrMemoryRegion.Tables, allocation);
                used.Add(global);
            }
            readOnlyPointerGlobals = used.Count;
        }

        public override string ToString() => target.ToString()!;

    }
}

