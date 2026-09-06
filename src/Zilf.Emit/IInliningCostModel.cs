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
