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

        public GlulxIrRoutineBuilder(RoutineBuilder target, IrNumericSemantics numericSemantics, bool optimize)
            : base(target, numericSemantics, optimize)
        {
            GlulxTarget = target;
        }

        public bool TryEmitLowCoreRead(string field, IVariable resultStorage)
        {
            if (!GlulxTarget.SupportsLowCoreRead(field))
                return false;
            RecordExtension(() => GlulxTarget.TryEmitLowCoreRead(field, resultStorage));
            return true;
        }

        public bool TryEmitLowCoreWrite(string field, IOperand newValue)
        {
            if (!GlulxTarget.SupportsLowCoreWrite(field))
                return false;
            RecordExtension(() => GlulxTarget.TryEmitLowCoreWrite(field, newValue));
            return true;
        }

        public bool TryEmitLowCoreGetTable(string field, IVariable resultStorage)
        {
            if (!GlulxTarget.SupportsLowCoreGetTable(field))
                return false;
            RecordExtension(() => GlulxTarget.TryEmitLowCoreGetTable(field, resultStorage));
            return true;
        }

        public void EmitScanTable(IOperand value, IOperand table, IOperand length, IOperand? form, IVariable result) =>
            RecordExtension(() => GlulxTarget.EmitScanTable(value, table, length, form, result));

        public void EmitGetChild(IOperand value, IVariable result) =>
            RecordExtension(() => GlulxTarget.EmitGetChild(value, result));

        public void EmitGetSibling(IOperand value, IVariable result) =>
            RecordExtension(() => GlulxTarget.EmitGetSibling(value, result));

        public void EmitGlkFromStack(IOperand operation, int argCount, IVariable? resultStorage = null) =>
            RecordExtension(() => GlulxTarget.EmitGlkFromStack(operation, argCount, resultStorage));
    }

    internal sealed class Glulx16IrRoutineBuilder : GlulxIrRoutineBuilder, IProvideWideContext
    {
        private readonly IProvideWideContext wideTarget;
        private readonly Stack<IDisposable> loweringContexts = [];
        private int compilationDepth;

        public Glulx16IrRoutineBuilder(RoutineBuilder16 target, bool optimize)
            : base(target, IrNumericSemantics.ZMachine16, optimize)
        {
            wideTarget = target;
        }

        public bool IsInWideContext => compilationDepth > 0;

        public IDisposable EnterWideContext()
        {
            compilationDepth++;
            RecordExtension(() => loweringContexts.Push(wideTarget.EnterWideContext()));
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
                owner.RecordExtension(() => owner.loweringContexts.Pop().Dispose());
            }
        }
    }
}
