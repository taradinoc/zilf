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
                BinaryOp.ArtShift => IrOpcode.ArithmeticShift,
                BinaryOp.LogShift => IrOpcode.LogicalShift,
                _ => IrOpcode.TargetOperation,
            };
            return opcode != IrOpcode.TargetOperation;
        }

        private static bool TryMapMemoryRead(BinaryOp op, out IrOpcode opcode)
        {
            opcode = op switch
            {
                BinaryOp.GetByte => IrOpcode.LoadByte,
                BinaryOp.GetWord => IrOpcode.LoadWord,
                BinaryOp.GetProperty => IrOpcode.LoadProperty,
                BinaryOp.GetPropAddress => IrOpcode.LoadPropertyAddress,
                BinaryOp.GetNextProp => IrOpcode.LoadNextProperty,
                _ => IrOpcode.TargetOperation,
            };
            return opcode != IrOpcode.TargetOperation;
        }

        private static bool TryMapMemoryRead(UnaryOp op, out IrOpcode opcode)
        {
            opcode = op switch
            {
                UnaryOp.GetParent => IrOpcode.LoadParent,
                UnaryOp.GetChild => IrOpcode.LoadChild,
                UnaryOp.GetSibling => IrOpcode.LoadSibling,
                UnaryOp.GetPropSize => IrOpcode.LoadPropertySize,
                _ => IrOpcode.TargetOperation,
            };
            return opcode != IrOpcode.TargetOperation;
        }

        private static bool TryClassify(UnaryOp op, out IrEffect effect)
        {
            effect = op switch
            {
                UnaryOp.Random => IrEffect.Nondeterministic,
                UnaryOp.RemoveObject or UnaryOp.GetCursor or UnaryOp.ReadMouse or UnaryOp.DirectOutput =>
                    IrEffect.WriteMemory,
                UnaryOp.DirectInput or UnaryOp.OutputStyle or UnaryOp.OutputBuffer or
                    UnaryOp.SplitWindow or UnaryOp.SelectWindow or UnaryOp.ClearWindow or UnaryOp.EraseLine or
                    UnaryOp.SetFont or UnaryOp.CheckUnicode or UnaryOp.PictureTable or UnaryOp.MouseWindow or
                    UnaryOp.PrintForm or UnaryOp.BufferScreen => IrEffect.InputOutput,
                UnaryOp.PopUserStack or UnaryOp.FlushStack => IrEffect.Stack,
                _ => IrEffect.Opaque,
            };
            return effect != IrEffect.Opaque;
        }

        private static bool TryClassify(BinaryOp op, out IrEffect effect)
        {
            effect = op switch
            {
                BinaryOp.MoveObject or BinaryOp.SetFlag or BinaryOp.ClearFlag or BinaryOp.DirectOutput =>
                    IrEffect.WriteMemory,
                BinaryOp.SetCursor or BinaryOp.SetColor or BinaryOp.SetTrueColor or
                    BinaryOp.GetWindowProperty or BinaryOp.ScrollWindow or BinaryOp.SetFont => IrEffect.InputOutput,
                BinaryOp.FlushUserStack => IrEffect.Stack,
                _ => IrEffect.Opaque,
            };
            return effect != IrEffect.Opaque;
        }

        private static bool TryClassify(TernaryOp op, out IrEffect effect)
        {
            effect = op switch
            {
                TernaryOp.PutByte or TernaryOp.PutWord or TernaryOp.PutProperty or TernaryOp.CopyTable or
                    TernaryOp.DirectOutput =>
                    IrEffect.WriteMemory,
                TernaryOp.PutWindowProperty or TernaryOp.DrawPicture or TernaryOp.WindowStyle or
                    TernaryOp.MoveWindow or TernaryOp.WindowSize or TernaryOp.SetMargins or TernaryOp.SetCursor or
                    TernaryOp.ErasePicture or TernaryOp.SetColor or
                    TernaryOp.SetTrueColor => IrEffect.InputOutput,
                _ => IrEffect.Opaque,
            };
            return effect != IrEffect.Opaque;
        }

        private static IrMemoryRegion GetReadRegions(IrOpcode opcode) => opcode switch
        {
            IrOpcode.LoadByte or IrOpcode.LoadWord or IrOpcode.ScanTable => IrMemoryRegion.Tables,
            IrOpcode.LoadProperty or IrOpcode.LoadPropertyAddress or IrOpcode.LoadNextProperty or
                IrOpcode.LoadPropertySize => IrMemoryRegion.Properties,
            IrOpcode.LoadParent or IrOpcode.LoadChild or IrOpcode.LoadSibling or IrOpcode.Inside =>
                IrMemoryRegion.ObjectTree,
            IrOpcode.HasAttribute => IrMemoryRegion.Attributes,
            _ => IrMemoryRegion.None,
        };

        private static IrMemoryRegion GetWriteRegions(UnaryOp op) => op switch
        {
            UnaryOp.RemoveObject => IrMemoryRegion.ObjectTree,
            UnaryOp.GetCursor or UnaryOp.ReadMouse => IrMemoryRegion.Tables,
            UnaryOp.DirectOutput => IrMemoryRegion.All,
            _ => IrMemoryRegion.None,
        };

        private static IrMemoryRegion GetWriteRegions(BinaryOp op) => op switch
        {
            BinaryOp.MoveObject => IrMemoryRegion.ObjectTree,
            BinaryOp.SetFlag or BinaryOp.ClearFlag => IrMemoryRegion.Attributes,
            BinaryOp.DirectOutput => IrMemoryRegion.All,
            _ => IrMemoryRegion.None,
        };

        private static IrMemoryRegion GetWriteRegions(TernaryOp op) => op switch
        {
            TernaryOp.PutByte or TernaryOp.PutWord or TernaryOp.CopyTable => IrMemoryRegion.Tables,
            TernaryOp.PutProperty => IrMemoryRegion.Properties,
            TernaryOp.DirectOutput => IrMemoryRegion.All,
            _ => IrMemoryRegion.None,
        };

        private static bool TryMap(Condition condition, out IrOpcode opcode)
        {
            opcode = condition switch
            {
                Condition.Less => IrOpcode.LessThan,
                Condition.Greater => IrOpcode.GreaterThan,
                Condition.TestBits => IrOpcode.BitTest,
                Condition.Inside => IrOpcode.Inside,
                Condition.TestAttr => IrOpcode.HasAttribute,
                Condition.ArgProvided => IrOpcode.ArgumentProvided,
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
            PreserveStackValues();
            FlushPromotedLocals();
            AppendLowering(IrOpcode.TargetOperation, [], IrEffect.Control, _ => action(), hasResult: false);
            localValues.Clear();
            dirtyLocals.Clear();
            current.Terminator = new IrTerminator.Return(null);
            StartFallthrough();
        }

    }
}

