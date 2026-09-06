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

using Microsoft.VisualStudio.TestTools.UnitTesting;
using Zilf.Emit.Zap;

namespace Zilf.Emit.Tests
{
    [TestClass]
    public sealed class ZapInstructionCostTests
    {
        [TestMethod]
        public void Small_Constants_And_Locals_Use_One_Byte()
        {
            var cost = ZapInstructionCost.Estimate(InliningOperationClass.Arithmetic,
                [InliningOperandClass.SmallConstant, InliningOperandClass.Local], true, false);

            Assert.AreEqual(new InliningCost(4, 1), cost);
        }

        [TestMethod]
        public void Unresolved_Constants_Use_Conservative_Large_Encoding()
        {
            var small = ZapInstructionCost.Estimate(InliningOperationClass.Copy,
                [InliningOperandClass.SmallConstant], true, false);
            var unresolved = ZapInstructionCost.Estimate(InliningOperationClass.Copy,
                [InliningOperandClass.UnresolvedConstant], true, false);

            Assert.AreEqual(small.Bytes + 1, unresolved.Bytes);
        }

        [TestMethod]
        public void Calls_Include_Type_Store_And_Branch_Bytes()
        {
            var plain = ZapInstructionCost.Estimate(InliningOperationClass.DirectCall,
                [InliningOperandClass.UnresolvedConstant, InliningOperandClass.Global], false, false);
            var storedAndBranched = ZapInstructionCost.Estimate(InliningOperationClass.DirectCall,
                [InliningOperandClass.UnresolvedConstant, InliningOperandClass.Global], true, true);

            Assert.AreEqual(plain.Bytes + 3, storedAndBranched.Bytes);
            Assert.AreEqual(4, storedAndBranched.Instructions);
        }

        [TestMethod]
        public void Large_Operand_Uses_Two_Encoded_Bytes()
        {
            var small = ZapInstructionCost.Estimate(InliningOperationClass.Arithmetic,
                [InliningOperandClass.SmallConstant, InliningOperandClass.Local], true, false);
            var large = ZapInstructionCost.Estimate(InliningOperationClass.Arithmetic,
                [InliningOperandClass.LargeConstant, InliningOperandClass.Local], true, false);

            Assert.AreEqual(small.Bytes + 1, large.Bytes);
        }
    }
}
