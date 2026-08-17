/* Copyright 2010-2026 Tara McGrew
 *
 * This file is part of ZILF.
 */

using System;
using System.Collections.Generic;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;
using Zilf.Emit;
using Zilf.Emit.Intermediate;

namespace Zilf.Emit.Tests
{
    [TestClass]
    public sealed class RoutineIrTests
    {
        [TestMethod]
        public void Optimizer_Folds_Constants_And_Removes_Dead_Branch()
        {
            var routine = new RoutineIr();
            var whenTrue = routine.CreateBlock();
            var whenFalse = routine.CreateBlock();
            var dead = routine.Append(
                routine.Entry,
                IrOpcode.Add,
                [routine.CreateConstant(20), routine.CreateConstant(22)]);
            routine.Entry.Terminator = new IrTerminator.Branch(dead.Result!, whenTrue, whenFalse);
            whenTrue.Terminator = new IrTerminator.Return(routine.CreateConstant(1));
            whenFalse.Terminator = new IrTerminator.Return(routine.CreateConstant(0));

            new RoutineIrOptimizer().Optimize(routine);

            Assert.AreEqual(2, routine.Blocks.Count);
            Assert.AreEqual(0, routine.Entry.Instructions.Count);
            Assert.AreSame(whenTrue, ((IrTerminator.Jump)routine.Entry.Terminator).Target);
        }

        [TestMethod]
        public void SsaBuilder_Inserts_Phi_At_Diamond_Join()
        {
            var routine = new RoutineIr();
            var left = routine.CreateBlock();
            var right = routine.CreateBlock();
            var join = routine.CreateBlock();
            routine.Entry.Terminator = new IrTerminator.Branch(routine.CreateValue(), left, right);
            left.Terminator = new IrTerminator.Jump(join);
            right.Terminator = new IrTerminator.Jump(join);
            join.Terminator = new IrTerminator.Return(null);
            routine.RebuildPredecessors();

            var variable = new object();
            var builder = new SsaBuilder(routine);
            builder.SealBlock(routine.Entry);
            builder.WriteVariable(left, variable, routine.CreateConstant(1));
            builder.WriteVariable(right, variable, routine.CreateConstant(2));
            builder.SealBlock(left);
            builder.SealBlock(right);

            var value = builder.ReadVariable(join, variable);
            builder.SealBlock(join);

            Assert.AreEqual(1, join.Instructions.Count);
            Assert.AreEqual(IrOpcode.Phi, join.Instructions[0].Opcode);
            Assert.AreSame(value, join.Instructions[0].Result);
            Assert.AreEqual(2, join.Instructions[0].Operands.Count);
        }

        [TestMethod]
        public void Optimizer_Preserves_Effectful_Operations()
        {
            var routine = new RoutineIr();
            routine.Append(
                routine.Entry,
                IrOpcode.TargetOperation,
                [],
                IrEffect.InputOutput,
                payload: "print",
                hasResult: false);
            routine.Entry.Terminator = new IrTerminator.Return(null);

            new RoutineIrOptimizer().Optimize(routine);

            Assert.AreEqual(1, routine.Entry.Instructions.Count);
        }

        [TestMethod]
        public void Sccp_Uses_Only_Executable_Phi_Inputs()
        {
            var routine = new RoutineIr();
            var left = routine.CreateBlock();
            var right = routine.CreateBlock();
            var join = routine.CreateBlock();
            routine.Entry.Terminator = new IrTerminator.Branch(routine.CreateConstant(1), left, right);
            left.Terminator = new IrTerminator.Jump(join);
            right.Terminator = new IrTerminator.Jump(join);
            var phi = routine.Append(join, IrOpcode.Phi,
                [routine.CreateConstant(17), routine.CreateConstant(99)], payload: new[] { left, right });
            join.Terminator = new IrTerminator.Return(phi.Result);

            new RoutineIrOptimizer().Optimize(routine);

            Assert.IsFalse(routine.Blocks.Contains(right));
            Assert.AreEqual(17, ((IrTerminator.Return)join.Terminator).Value?.Constant);
        }

        [TestMethod]
        public void Sccp_Does_Not_Fold_Division_By_Zero()
        {
            var routine = new RoutineIr();
            var divide = routine.Append(routine.Entry, IrOpcode.Divide,
                [routine.CreateConstant(12), routine.CreateConstant(0)]);
            routine.Entry.Terminator = new IrTerminator.Return(divide.Result);

            new RoutineIrOptimizer().Optimize(routine);

            Assert.AreEqual(1, routine.Entry.Instructions.Count);
            Assert.AreSame(divide.Result, ((IrTerminator.Return)routine.Entry.Terminator).Value);
        }

        [TestMethod]
        public void Gvn_Removes_Dominated_Commutative_Expression()
        {
            var routine = new RoutineIr();
            var child = routine.CreateBlock();
            var left = routine.CreateValue();
            var right = routine.CreateValue();
            var first = routine.Append(routine.Entry, IrOpcode.Add, [left, right]);
            routine.Entry.Terminator = new IrTerminator.Jump(child);
            routine.Append(child, IrOpcode.Add, [right, left]);
            var duplicate = child.Instructions[0].Result;
            child.Terminator = new IrTerminator.Return(duplicate);

            new RoutineIrOptimizer().Optimize(routine);

            Assert.AreEqual(0, child.Instructions.Count);
            Assert.AreSame(first.Result, ((IrTerminator.Return)child.Terminator).Value);
        }

        [TestMethod]
        public void Gvn_Does_Not_Reuse_Value_From_Sibling_Block()
        {
            var routine = new RoutineIr();
            var leftBlock = routine.CreateBlock();
            var rightBlock = routine.CreateBlock();
            var value = routine.CreateValue();
            routine.Entry.Terminator = new IrTerminator.Branch(routine.CreateValue(), leftBlock, rightBlock);
            var left = routine.Append(leftBlock, IrOpcode.Negate, [value]);
            leftBlock.Terminator = new IrTerminator.Return(left.Result);
            var right = routine.Append(rightBlock, IrOpcode.Negate, [value]);
            rightBlock.Terminator = new IrTerminator.Return(right.Result);

            new RoutineIrOptimizer().Optimize(routine);

            Assert.AreEqual(1, leftBlock.Instructions.Count);
            Assert.AreEqual(1, rightBlock.Instructions.Count);
        }

        [DataTestMethod]
        [DataRow((int)IrNumericSemantics.ZMachine16, 32767, 1, -32768)]
        [DataRow((int)IrNumericSemantics.Glulx32, 32767, 1, 32768)]
        [DataRow((int)IrNumericSemantics.ZMachine16, 32768, 1, -32767)]
        [DataRow((int)IrNumericSemantics.Glulx32, 32768, 1, 32769)]
        public void ConstantFolding_Add_Uses_Target_Word_Size(
            int semantics,
            int left,
            int right,
            int expected)
        {
            AssertFoldedBinary((IrNumericSemantics)semantics, IrOpcode.Add, left, right, expected);
        }

        [DataTestMethod]
        [DataRow((int)IrNumericSemantics.ZMachine16, 256, 256, 0)]
        [DataRow((int)IrNumericSemantics.Glulx32, 256, 256, 65536)]
        [DataRow((int)IrNumericSemantics.ZMachine16, -32768, -1, -32768)]
        [DataRow((int)IrNumericSemantics.Glulx32, -32768, -1, 32768)]
        public void ConstantFolding_Multiply_Uses_Target_Word_Size(
            int semantics,
            int left,
            int right,
            int expected)
        {
            AssertFoldedBinary((IrNumericSemantics)semantics, IrOpcode.Multiply, left, right, expected);
        }

        [DataTestMethod]
        [DataRow((int)IrNumericSemantics.ZMachine16, 0x8000, -32768)]
        [DataRow((int)IrNumericSemantics.Glulx32, 0x8000, 32768)]
        [DataRow((int)IrNumericSemantics.ZMachine16, 0xffff, -1)]
        [DataRow((int)IrNumericSemantics.Glulx32, 0xffff, 65535)]
        public void Constants_Are_Interpreted_As_Target_Words(
            int semantics,
            int encodedValue,
            int expected)
        {
            AssertFoldedBinary((IrNumericSemantics)semantics, IrOpcode.Add, encodedValue, 0, expected);
        }

        [TestMethod]
        public void Verifier_Rejects_Block_Without_Terminator()
        {
            var routine = new RoutineIr();

            Assert.ThrowsException<InvalidOperationException>(routine.Verify);
        }

        [TestMethod]
        public void IrRoutineBuilder_Removes_Unreachable_Operations_When_Enabled()
        {
            var (builder, target, label) = CreateRecordingBuilder(optimize: true);

            builder.Branch(label);
            builder.EmitPrintNewLine();
            builder.MarkLabel(label);
            builder.EmitQuit();
            builder.Finish();

            target.Verify(t => t.Branch(label), Times.Once);
            target.Verify(t => t.EmitPrintNewLine(), Times.Never);
            target.Verify(t => t.MarkLabel(label), Times.Once);
            target.Verify(t => t.EmitQuit(), Times.Once);
            target.Verify(t => t.Finish(), Times.Once);
        }

        [TestMethod]
        public void IrRoutineBuilder_DisableOptimization_Preserves_Unreachable_Operations()
        {
            var (builder, target, label) = CreateRecordingBuilder(optimize: false);

            builder.Branch(label);
            builder.EmitPrintNewLine();
            builder.MarkLabel(label);
            builder.EmitQuit();
            builder.Finish();

            target.Verify(t => t.EmitPrintNewLine(), Times.Once);
        }

        [TestMethod]
        public void IrRoutineBuilder_Sccp_Folds_Production_Arithmetic()
        {
            var target = new Mock<IRoutineBuilder>();
            var local = new Mock<ILocalBuilder>();
            target.SetupGet(t => t.RoutineStart).Returns(Mock.Of<ILabel>());
            target.SetupGet(t => t.RTrue).Returns(Mock.Of<ILabel>());
            target.SetupGet(t => t.RFalse).Returns(Mock.Of<ILabel>());
            target.Setup(t => t.DefineLocal("TEMP")).Returns(local.Object);
            var operands = new Dictionary<int, INumericOperand>();
            INumericOperand MakeOperand(int value)
            {
                if (!operands.TryGetValue(value, out var operand))
                {
                    var mock = new Mock<INumericOperand>();
                    mock.SetupGet(item => item.Value).Returns(value);
                    operand = mock.Object;
                    operands.Add(value, operand);
                }
                return operand;
            }

            var builder = new IrRoutineBuilder(target.Object, IrNumericSemantics.ZMachine16, true, MakeOperand);
            var temp = builder.DefineLocal("TEMP");
            builder.EmitBinary(BinaryOp.Add, MakeOperand(20), MakeOperand(22), temp);
            builder.Return(temp);
            builder.Finish();

            target.Verify(t => t.EmitBinary(It.IsAny<BinaryOp>(), It.IsAny<IOperand>(), It.IsAny<IOperand>(),
                It.IsAny<IVariable>()), Times.Never);
            target.Verify(t => t.Return(operands[42]), Times.Once);
        }

        [TestMethod]
        public void IrRoutineBuilder_Gvn_Eliminates_Production_Arithmetic()
        {
            var target = new Mock<IRoutineBuilder>();
            var left = Mock.Of<ILocalBuilder>();
            var right = Mock.Of<ILocalBuilder>();
            var temp = Mock.Of<ILocalBuilder>();
            target.SetupGet(t => t.RoutineStart).Returns(Mock.Of<ILabel>());
            target.SetupGet(t => t.RTrue).Returns(Mock.Of<ILabel>());
            target.SetupGet(t => t.RFalse).Returns(Mock.Of<ILabel>());
            target.Setup(t => t.DefineRequiredParameter("LEFT")).Returns(left);
            target.Setup(t => t.DefineRequiredParameter("RIGHT")).Returns(right);
            target.Setup(t => t.DefineLocal("TEMP")).Returns(temp);

            var builder = new IrRoutineBuilder(target.Object, IrNumericSemantics.ZMachine16, true);
            var leftValue = builder.DefineRequiredParameter("LEFT");
            var rightValue = builder.DefineRequiredParameter("RIGHT");
            var result = builder.DefineLocal("TEMP");
            builder.EmitBinary(BinaryOp.Add, leftValue, rightValue, result);
            builder.EmitBinary(BinaryOp.Add, rightValue, leftValue, result);
            builder.Return(result);
            builder.Finish();

            target.Verify(t => t.EmitBinary(BinaryOp.Add, left, right, temp), Times.Once);
            target.Verify(t => t.EmitBinary(BinaryOp.Add, right, left, temp), Times.Never);
        }

        private static void AssertFoldedBinary(
            IrNumericSemantics semantics,
            IrOpcode opcode,
            int left,
            int right,
            int expected)
        {
            var routine = new RoutineIr();
            var instruction = routine.Append(
                routine.Entry,
                opcode,
                [routine.CreateConstant(left), routine.CreateConstant(right)]);
            routine.Entry.Terminator = new IrTerminator.Return(instruction.Result);

            new RoutineIrOptimizer(semantics).Optimize(routine);

            var result = ((IrTerminator.Return)routine.Entry.Terminator).Value;
            Assert.IsNotNull(result);
            Assert.AreEqual(expected, result.Constant);
        }

        private static (IrRoutineBuilder Builder, Mock<IRoutineBuilder> Target, ILabel Label)
            CreateRecordingBuilder(bool optimize)
        {
            var target = new Mock<IRoutineBuilder>();
            var routineStart = Mock.Of<ILabel>();
            var rtrue = Mock.Of<ILabel>();
            var rfalse = Mock.Of<ILabel>();
            var label = Mock.Of<ILabel>();
            target.SetupGet(t => t.RoutineStart).Returns(routineStart);
            target.SetupGet(t => t.RTrue).Returns(rtrue);
            target.SetupGet(t => t.RFalse).Returns(rfalse);

            return (new IrRoutineBuilder(target.Object, IrNumericSemantics.ZMachine16, optimize), target, label);
        }
    }
}
