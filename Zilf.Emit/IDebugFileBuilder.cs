using System.Diagnostics.Contracts;

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
        void MarkAction(IOperand action, string name);
        /// <summary>
        /// Marks the bounds of an object definition.
        /// </summary>
        /// <param name="obj">The object being defined.</param>
        /// <param name="start">The position where the object definition begins.</param>
        /// <param name="end">The position where the object definition ends.</param>
        void MarkObject(IObjectBuilder obj, DebugLineRef start, DebugLineRef end);
        /// <summary>
        /// Marks the bounds of a routine definition.
        /// </summary>
        /// <param name="routine">The routine being defined.</param>
        /// <param name="start">The position where the routine definition begins.</param>
        /// <param name="end">The position where the routine definition ends.</param>
        void MarkRoutine(IRoutineBuilder routine, DebugLineRef start, DebugLineRef end);
        /// <summary>
        /// Marks a sequence point at the current position in a routine.
        /// </summary>
        /// <param name="routine">The routine being defined.</param>
        /// <param name="point">The position corresponding to the next
        /// instruction emitted.</param>
        void MarkSequencePoint(IRoutineBuilder routine, DebugLineRef point);
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