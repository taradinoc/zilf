/* Copyright 2010-2026 Tara McGrew
 *
 * This file is part of ZILF.
 */

using System.Collections.Generic;

namespace Zilf.Emit
{
    /// <summary>
    /// Describes a target-independent operation for pre-IR inlining cost estimation.
    /// </summary>
    public enum InliningOperationClass
    {
        Arithmetic,
        Predicate,
        MemoryRead,
        MemoryWrite,
        Branch,
        DirectCall,
        Copy,
        Return,
    }

    /// <summary>
    /// Describes the target-relevant encoding class of an operand.
    /// </summary>
    public enum InliningOperandClass
    {
        SmallConstant,
        LargeConstant,
        UnresolvedConstant,
        Local,
        Global,
        Stack,
        Indirect,
    }

    /// <summary>
    /// Contains estimated encoded size and dynamic instruction cost.
    /// </summary>
    /// <param name="Bytes">The estimated encoded size in bytes.</param>
    /// <param name="Instructions">The estimated number of dynamically executed instructions.</param>
    public readonly record struct InliningCost(int Bytes, int Instructions)
    {
        /// <summary>
        /// Adds two costs.
        /// </summary>
        public static InliningCost operator +(InliningCost left, InliningCost right) =>
            new(left.Bytes + right.Bytes, left.Instructions + right.Instructions);
    }

    /// <summary>
    /// Supplies target-specific costs for compiler-level routine inlining decisions.
    /// </summary>
    public interface IInliningCostModel
    {
        /// <summary>
        /// Estimates one operation with the specified operands and result behavior.
        /// </summary>
        InliningCost EstimateOperation(InliningOperationClass operation,
            IReadOnlyList<InliningOperandClass> operands, bool storesResult, bool branches);
    }
}
