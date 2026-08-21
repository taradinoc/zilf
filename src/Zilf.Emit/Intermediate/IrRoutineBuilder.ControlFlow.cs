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

        private IrBlock CreateOptimizerPreheader(IrBlock header)
        {
            var label = target.DefineLabel();
            var block = routine.CreateBlock();
            labelBlocks.Add(label, block);
            blockLabels.Add(block, label);
            var headerIndex = layout.IndexOf(header);
            if (headerIndex < 0)
                throw new InvalidOperationException($"Cannot insert a preheader before non-layout block {header}.");
            layout.Insert(headerIndex, block);
            routine.Append(block, IrOpcode.TargetOperation, [], IrEffect.InputOutput,
                new IrLoweringOperation(_ => target.MarkLabel(label)), hasResult: false);
            block.Terminator = new IrTerminator.Jump(header);
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
