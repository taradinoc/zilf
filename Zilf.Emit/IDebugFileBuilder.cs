/* Copyright 2010-2017 Jesse McGrew
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

using System.Diagnostics.Contracts;
using JetBrains.Annotations;

namespace Zilf.Emit
{
    [ContractClass(typeof(IDebugFileBuilderContracts))]
    public interface IDebugFileBuilder
    {
        /// <summary>
        /// Marks an operand as an action constant.
        /// </summary>
        /// <param name="action">The operand whose value identifies an action.</param>
        /// <param name="name">The name of the action.</param>
        void MarkAction([NotNull] IOperand action, [NotNull] string name);
        /// <summary>
        /// Marks the bounds of an object definition.
        /// </summary>
        /// <param name="obj">The object being defined.</param>
        /// <param name="start">The position where the object definition begins.</param>
        /// <param name="end">The position where the object definition ends.</param>
        void MarkObject([NotNull] IObjectBuilder obj, DebugLineRef start, DebugLineRef end);
        /// <summary>
        /// Marks the bounds of a routine definition.
        /// </summary>
        /// <param name="routine">The routine being defined.</param>
        /// <param name="start">The position where the routine definition begins.</param>
        /// <param name="end">The position where the routine definition ends.</param>
        void MarkRoutine([NotNull] IRoutineBuilder routine, DebugLineRef start, DebugLineRef end);
        /// <summary>
        /// Marks a sequence point at the current position in a routine.
        /// </summary>
        /// <param name="routine">The routine being defined.</param>
        /// <param name="point">The position corresponding to the next
        /// instruction emitted.</param>
        void MarkSequencePoint([NotNull] IRoutineBuilder routine, DebugLineRef point);
    }

    [ContractClassFor(typeof(IDebugFileBuilder))]
    abstract class IDebugFileBuilderContracts : IDebugFileBuilder
    {
        public void MarkAction(IOperand action, string name)
        {
            Contract.Requires(action != null);
            Contract.Requires(!string.IsNullOrWhiteSpace(name));
        }

        public void MarkObject(IObjectBuilder obj, DebugLineRef start, DebugLineRef end)
        {
            Contract.Requires(obj != null);
        }

        public void MarkRoutine(IRoutineBuilder routine, DebugLineRef start, DebugLineRef end)
        {
            Contract.Requires(routine != null);
        }

        public void MarkSequencePoint(IRoutineBuilder routine, DebugLineRef point)
        {
            Contract.Requires(routine != null);
        }
    }
}