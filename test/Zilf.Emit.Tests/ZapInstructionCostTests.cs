/* Copyright 2010-2026 Tara McGrew
 *
 * This file is part of ZILF.
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
