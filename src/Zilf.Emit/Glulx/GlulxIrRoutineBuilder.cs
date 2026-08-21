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
using Zilf.Emit.Intermediate;

namespace Zilf.Emit.Glulx
{
    internal class GlulxIrRoutineBuilder : IrRoutineBuilder, IProvideLowCoreEmulation,
        IProvideNoValuePredEmit, IProvideGlkFromStackEmit
    {
        protected readonly RoutineBuilder GlulxTarget;

        public GlulxIrRoutineBuilder(RoutineBuilder target, IrNumericSemantics numericSemantics, bool optimize,
            Func<int, INumericOperand> makeOperand, Action<IEnumerable<IrOptimizationStat>>? recordOptimizationStats,
            Action<IrRoutineBuilder>? deferFinalization = null)
            : base(target, numericSemantics, optimize, makeOperand,
                recordOptimizationStats: recordOptimizationStats, deferFinalization: deferFinalization)
        {
            GlulxTarget = target;
        }

        public bool TryEmitLowCoreRead(string field, IVariable resultStorage)
        {
            if (!GlulxTarget.SupportsLowCoreRead(field))
                return false;
            RecordEffectfulOperation([], _ => GlulxTarget.TryEmitLowCoreRead(field, resultStorage),
                IrEffect.ReadMemory, resultStorage,
                (_, home) => GlulxTarget.TryEmitLowCoreRead(field, home!));
            return true;
        }

        public bool TryEmitLowCoreWrite(string field, IOperand newValue)
        {
            if (!GlulxTarget.SupportsLowCoreWrite(field))
                return false;
            RecordOrderedOperation([newValue],
                operands => GlulxTarget.TryEmitLowCoreWrite(field, operands[0]), IrEffect.WriteMemory);
            return true;
        }

        public bool TryEmitLowCoreGetTable(string field, IVariable resultStorage)
        {
            if (!GlulxTarget.SupportsLowCoreGetTable(field))
                return false;
            RecordEffectfulOperation([], _ => GlulxTarget.TryEmitLowCoreGetTable(field, resultStorage),
                IrEffect.ReadMemory, resultStorage,
                (_, home) => GlulxTarget.TryEmitLowCoreGetTable(field, home!));
            return true;
        }

        public void EmitScanTable(IOperand value, IOperand table, IOperand length, IOperand? form, IVariable result)
        {
            var operands = form == null ? new[] { value, table, length } : [value, table, length, form];
            RecordEffectfulOperation(operands,
                resolved => GlulxTarget.EmitScanTable(resolved[0], resolved[1], resolved[2],
                    form == null ? null : resolved[3], result), IrEffect.ReadMemory, result,
                (resolved, home) => GlulxTarget.EmitScanTable(resolved[0], resolved[1], resolved[2],
                    form == null ? null : resolved[3], home!));
        }

        public void EmitGetChild(IOperand value, IVariable result) =>
            RecordEffectfulOperation([value], operands => GlulxTarget.EmitGetChild(operands[0], result),
                IrEffect.ReadMemory, result, (operands, home) => GlulxTarget.EmitGetChild(operands[0], home!));

        public void EmitGetSibling(IOperand value, IVariable result) =>
            RecordEffectfulOperation([value], operands => GlulxTarget.EmitGetSibling(operands[0], result),
                IrEffect.ReadMemory, result, (operands, home) => GlulxTarget.EmitGetSibling(operands[0], home!));

        public void EmitGlkFromStack(IOperand operation, int argCount, IVariable? resultStorage = null) =>
            RecordExtension(() => GlulxTarget.EmitGlkFromStack(operation, argCount, resultStorage));
    }

    internal sealed class Glulx16IrRoutineBuilder : GlulxIrRoutineBuilder, IProvideWideContext
    {
        private readonly IProvideWideContext wideTarget;
        private readonly Stack<IDisposable> loweringContexts = [];
        private int compilationDepth;

        public Glulx16IrRoutineBuilder(RoutineBuilder16 target, bool optimize, Func<int, INumericOperand> makeOperand,
            Action<IEnumerable<IrOptimizationStat>>? recordOptimizationStats,
            Action<IrRoutineBuilder>? deferFinalization = null)
            : base(target, IrNumericSemantics.ZMachine16, optimize, makeOperand, recordOptimizationStats,
                deferFinalization)
        {
            wideTarget = target;
        }

        public bool IsInWideContext => compilationDepth > 0;

        public IDisposable EnterWideContext()
        {
            compilationDepth++;
            RecordExtension(() => loweringContexts.Push(wideTarget.EnterWideContext()), IrEffect.Control);
            return new WideContext(this);
        }

        private sealed class WideContext(Glulx16IrRoutineBuilder owner) : IDisposable
        {
            private bool disposed;

            public void Dispose()
            {
                if (disposed)
                    return;
                disposed = true;
                owner.compilationDepth--;
                owner.RecordExtension(() => owner.loweringContexts.Pop().Dispose(), IrEffect.Control);
            }
        }
    }
}
