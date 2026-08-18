/* Copyright 2010-2026 Tara McGrew
 *
 * This file is part of ZILF.
 */

using System;
using System.Collections.Generic;
using System.Linq;
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
        public void Optimizer_Folds_Algebraic_Identity_With_Unknown_Value()
        {
            var routine = new RoutineIr();
            var value = routine.CreateValue();
            var add = routine.Append(routine.Entry, IrOpcode.Add, [value, routine.CreateConstant(0)]);
            routine.Entry.Terminator = new IrTerminator.Return(add.Result);

            new RoutineIrOptimizer(IrNumericSemantics.ZMachine16).Optimize(routine);

            Assert.AreSame(value, ((IrTerminator.Return)routine.Entry.Terminator).Value);
            Assert.IsFalse(routine.Entry.Instructions.Any(instruction => instruction.Opcode == IrOpcode.Add));
        }

        [TestMethod]
        public void Optimizer_Eliminates_Dominated_Memory_Read_With_Same_Home()
        {
            var routine = new RoutineIr();
            var table = routine.CreateValue();
            var index = routine.CreateValue();
            var home = Mock.Of<IVariable>();
            var first = routine.Append(routine.Entry, IrOpcode.LoadByte, [table, index], IrEffect.ReadMemory,
                new IrLoweringOperation(_ => { }, home));
            var second = routine.Append(routine.Entry, IrOpcode.LoadByte, [table, index], IrEffect.ReadMemory,
                new IrLoweringOperation(_ => { }, home));
            routine.Entry.Terminator = new IrTerminator.Return(second.Result);

            new RoutineIrOptimizer().Optimize(routine);

            Assert.AreSame(first.Result, ((IrTerminator.Return)routine.Entry.Terminator).Value);
            Assert.AreEqual(1, routine.Entry.Instructions.Count(instruction => instruction.Opcode == IrOpcode.LoadByte));
        }

        [TestMethod]
        public void Optimizer_Does_Not_Eliminate_Memory_Read_Across_Write_Barrier()
        {
            var routine = new RoutineIr();
            var table = routine.CreateValue();
            var index = routine.CreateValue();
            var home = Mock.Of<IVariable>();
            var first = routine.Append(routine.Entry, IrOpcode.LoadWord, [table, index], IrEffect.ReadMemory,
                new IrLoweringOperation(_ => { }, home));
            routine.Append(routine.Entry, IrOpcode.TargetOperation, [first.Result!], IrEffect.WriteMemory,
                hasResult: false);
            var second = routine.Append(routine.Entry, IrOpcode.LoadWord, [table, index], IrEffect.ReadMemory,
                new IrLoweringOperation(_ => { }, home));
            routine.Entry.Terminator = new IrTerminator.Return(second.Result);

            new RoutineIrOptimizer().Optimize(routine);

            Assert.AreEqual(2, routine.Entry.Instructions.Count(instruction => instruction.Opcode == IrOpcode.LoadWord));
        }

        [TestMethod]
        public void Optimizer_Eliminates_Memory_Read_Across_Ordered_Io()
        {
            var routine = new RoutineIr();
            var table = routine.CreateValue();
            var index = routine.CreateValue();
            var home = Mock.Of<IVariable>();
            var first = routine.Append(routine.Entry, IrOpcode.LoadByte, [table, index], IrEffect.ReadMemory,
                new IrLoweringOperation(_ => { }, home));
            routine.Append(routine.Entry, IrOpcode.TargetOperation, [first.Result!], IrEffect.InputOutput,
                hasResult: false);
            var second = routine.Append(routine.Entry, IrOpcode.LoadByte, [table, index], IrEffect.ReadMemory,
                new IrLoweringOperation(_ => { }, home));
            routine.Entry.Terminator = new IrTerminator.Return(second.Result);

            new RoutineIrOptimizer().Optimize(routine);

            Assert.AreEqual(1, routine.Entry.Instructions.Count(instruction => instruction.Opcode == IrOpcode.LoadByte));
        }

        [TestMethod]
        public void Optimizer_Eliminates_Memory_Read_Across_Nondeterministic_Operation()
        {
            var routine = new RoutineIr();
            var table = routine.CreateValue();
            var index = routine.CreateValue();
            var home = Mock.Of<IVariable>();
            var first = routine.Append(routine.Entry, IrOpcode.LoadByte, [table, index], IrEffect.ReadMemory,
                new IrLoweringOperation(_ => { }, home));
            routine.Append(routine.Entry, IrOpcode.TargetOperation, [], IrEffect.Nondeterministic,
                hasResult: false);
            var second = routine.Append(routine.Entry, IrOpcode.LoadByte, [table, index], IrEffect.ReadMemory,
                new IrLoweringOperation(_ => { }, home));
            routine.Entry.Terminator = new IrTerminator.Return(second.Result);

            new RoutineIrOptimizer().Optimize(routine);

            Assert.AreSame(first.Result, ((IrTerminator.Return)routine.Entry.Terminator).Value);
            Assert.AreEqual(1, routine.Entry.Instructions.Count(instruction => instruction.Opcode == IrOpcode.LoadByte));
        }

        [TestMethod]
        public void Optimizer_Eliminates_Property_Read_Across_Control_Flow_Markers()
        {
            var routine = new RoutineIr();
            var next = routine.CreateBlock();
            var obj = routine.CreateValue();
            var property = routine.CreateValue();
            var home = Mock.Of<IVariable>();
            var first = routine.Append(routine.Entry, IrOpcode.LoadProperty, [obj, property], IrEffect.ReadMemory,
                new IrLoweringOperation(_ => { }, home));
            routine.Append(routine.Entry, IrOpcode.TargetOperation, [], IrEffect.Control, hasResult: false);
            routine.Entry.Terminator = new IrTerminator.Jump(next);
            routine.Append(next, IrOpcode.TargetOperation, [], IrEffect.Control, hasResult: false);
            var second = routine.Append(next, IrOpcode.LoadProperty, [obj, property], IrEffect.ReadMemory,
                new IrLoweringOperation(_ => { }, home));
            next.Terminator = new IrTerminator.Return(second.Result);

            new RoutineIrOptimizer().Optimize(routine);

            Assert.AreSame(first.Result, ((IrTerminator.Return)next.Terminator).Value);
            Assert.AreEqual(1, routine.Blocks.SelectMany(block => block.Instructions)
                .Count(instruction => instruction.Opcode == IrOpcode.LoadProperty));
        }

        [TestMethod]
        public void Gvn_Preserves_Local_Expression_Across_Call()
        {
            var routine = new RoutineIr();
            var left = routine.CreateValue();
            var right = routine.CreateValue();
            var home = Mock.Of<IVariable>();
            var first = routine.Append(routine.Entry, IrOpcode.Add, [left, right], payload:
                new IrLoweringOperation(_ => { }, home));
            routine.Append(routine.Entry, IrOpcode.TargetOperation, [], IrEffect.Call, hasResult: false);
            var second = routine.Append(routine.Entry, IrOpcode.Add, [left, right], payload:
                new IrLoweringOperation(_ => { }, home));
            routine.Entry.Terminator = new IrTerminator.Return(second.Result);

            new RoutineIrOptimizer().Optimize(routine);

            Assert.AreSame(first.Result, ((IrTerminator.Return)routine.Entry.Terminator).Value);
            Assert.AreEqual(1, routine.Entry.Instructions.Count(instruction => instruction.Opcode == IrOpcode.Add));
        }

        [TestMethod]
        public void Gvn_Rewrites_Duplicate_With_Different_Home_As_Copy()
        {
            var routine = new RoutineIr();
            var left = routine.CreateValue();
            var right = routine.CreateValue();
            var firstHome = Mock.Of<IVariable>();
            var secondHome = Mock.Of<IVariable>();
            var first = routine.Append(routine.Entry, IrOpcode.Add, [left, right], payload:
                new IrLoweringOperation(_ => { }, firstHome));
            var second = routine.Append(routine.Entry, IrOpcode.Add, [left, right], payload:
                new IrLoweringOperation(_ => { }, secondHome));
            routine.Entry.Terminator = new IrTerminator.Return(second.Result);
            var copies = new List<(IVariable Destination, IOperand Source)>();

            new RoutineIrOptimizer(emitCopy: (destination, source) => copies.Add((destination, source)))
                .Optimize(routine);
            var rewritten = routine.Entry.Instructions.Single(instruction => ReferenceEquals(instruction.Result,
                second.Result));
            ((IrLoweringOperation)rewritten.Payload!).Replay([firstHome]);

            Assert.AreEqual(IrOpcode.TargetOperation, rewritten.Opcode);
            Assert.AreSame(first.Result, rewritten.Operands[0]);
            Assert.AreEqual((secondHome, firstHome), copies.Single());
        }

        [TestMethod]
        public void Optimizer_Forwards_Sole_Result_Into_Adjacent_Copy_Destination()
        {
            var routine = new RoutineIr();
            var left = routine.CreateValue();
            var right = routine.CreateValue();
            var stack = Mock.Of<IVariable>();
            var local = Mock.Of<IVariable>();
            var add = routine.Append(routine.Entry, IrOpcode.Add, [left, right], payload:
                new IrLoweringOperation(_ => { }, stack, isStackResult: true, (_, _) => { }));
            var copy = routine.Append(routine.Entry, IrOpcode.Copy, [add.Result!], payload:
                new IrLoweringOperation(_ => { }, local));
            routine.Entry.Terminator = new IrTerminator.Return(copy.Result);

            new RoutineIrOptimizer().Optimize(routine);

            Assert.AreEqual(1, routine.Entry.Instructions.Count);
            Assert.AreEqual(IrOpcode.Add, routine.Entry.Instructions[0].Opcode);
            Assert.AreSame(local, ((IrLoweringOperation)routine.Entry.Instructions[0].Payload!).ResultHome);
            Assert.AreSame(add.Result, ((IrTerminator.Return)routine.Entry.Terminator).Value);
        }

        [TestMethod]
        public void Gvn_Invalidates_Global_Expression_Across_Call()
        {
            var routine = new RoutineIr();
            var global = routine.CreateExternalValue(mutable: true);
            var one = routine.CreateConstant(1);
            var home = Mock.Of<IVariable>();
            var first = routine.Append(routine.Entry, IrOpcode.Add, [global, one], payload:
                new IrLoweringOperation(_ => { }, home));
            routine.Append(routine.Entry, IrOpcode.TargetOperation, [first.Result!], IrEffect.InputOutput,
                hasResult: false);
            routine.Append(routine.Entry, IrOpcode.TargetOperation, [], IrEffect.Call, hasResult: false);
            var second = routine.Append(routine.Entry, IrOpcode.Add, [global, one], payload:
                new IrLoweringOperation(_ => { }, home));
            routine.Entry.Terminator = new IrTerminator.Return(second.Result);

            new RoutineIrOptimizer().Optimize(routine);

            Assert.AreEqual(2, routine.Entry.Instructions.Count(instruction => instruction.Opcode == IrOpcode.Add));
        }

        [TestMethod]
        public void Gvn_Invalidates_Expressions_Transitively_Derived_From_Global_Across_Call()
        {
            var routine = new RoutineIr();
            var global = routine.CreateExternalValue(mutable: true);
            var one = routine.CreateConstant(1);
            var two = routine.CreateConstant(2);
            var firstHome = Mock.Of<IVariable>();
            var secondHome = Mock.Of<IVariable>();
            var baseValue = routine.Append(routine.Entry, IrOpcode.Add, [global, one], payload:
                new IrLoweringOperation(_ => { }, firstHome));
            var first = routine.Append(routine.Entry, IrOpcode.Multiply, [baseValue.Result!, two], payload:
                new IrLoweringOperation(_ => { }, secondHome));
            routine.Append(routine.Entry, IrOpcode.TargetOperation, [first.Result!], IrEffect.InputOutput,
                hasResult: false);
            routine.Append(routine.Entry, IrOpcode.TargetOperation, [], IrEffect.Call, hasResult: false);
            var second = routine.Append(routine.Entry, IrOpcode.Multiply, [baseValue.Result!, two], payload:
                new IrLoweringOperation(_ => { }, secondHome));
            routine.Entry.Terminator = new IrTerminator.Return(second.Result);

            new RoutineIrOptimizer().Optimize(routine);

            Assert.AreEqual(2,
                routine.Entry.Instructions.Count(instruction => instruction.Opcode == IrOpcode.Multiply));
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

        [TestMethod]
        public void IrRoutineBuilder_Preserves_Original_Named_Constant_Operand()
        {
            var target = new Mock<IRoutineBuilder>();
            var namedConstant = new Mock<INumericOperand>();
            namedConstant.SetupGet(operand => operand.Value).Returns(42);
            target.SetupGet(t => t.RoutineStart).Returns(Mock.Of<ILabel>());
            target.SetupGet(t => t.RTrue).Returns(Mock.Of<ILabel>());
            target.SetupGet(t => t.RFalse).Returns(Mock.Of<ILabel>());

            var builder = new IrRoutineBuilder(target.Object, IrNumericSemantics.ZMachine16, true);
            builder.Return(namedConstant.Object);
            builder.Finish();

            target.Verify(t => t.Return(namedConstant.Object), Times.Once);
        }

        [TestMethod]
        public void IrRoutineBuilder_Propagates_Stored_Compiler_Temporary_Through_Arithmetic()
        {
            var target = new Mock<IRoutineBuilder>();
            var local = Mock.Of<ILocalBuilder>();
            target.SetupGet(t => t.RoutineStart).Returns(Mock.Of<ILabel>());
            target.SetupGet(t => t.RTrue).Returns(Mock.Of<ILabel>());
            target.SetupGet(t => t.RFalse).Returns(Mock.Of<ILabel>());
            target.Setup(t => t.DefineLocal("?TMP")).Returns(local);
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
            var temp = builder.DefineLocal("?TMP");
            builder.EmitStore(temp, MakeOperand(20));
            builder.EmitBinary(BinaryOp.Add, temp, MakeOperand(22), temp);
            builder.Return(temp);
            builder.Finish();

            target.Verify(t => t.EmitStore(It.IsAny<IVariable>(), It.IsAny<IOperand>()), Times.Never);
            target.Verify(t => t.EmitBinary(It.IsAny<BinaryOp>(), It.IsAny<IOperand>(), It.IsAny<IOperand>(),
                It.IsAny<IVariable>()), Times.Never);
            target.Verify(t => t.Return(operands[42]), Times.Once);
        }

        [TestMethod]
        public void IrRoutineBuilder_Materializes_Global_Snapshot_Before_Call()
        {
            var target = new Mock<IRoutineBuilder>();
            var temp = Mock.Of<ILocalBuilder>();
            var global = Mock.Of<IVariable>();
            var calledRoutine = Mock.Of<IOperand>();
            var emitted = new List<string>();
            target.SetupGet(t => t.RoutineStart).Returns(Mock.Of<ILabel>());
            target.SetupGet(t => t.RTrue).Returns(Mock.Of<ILabel>());
            target.SetupGet(t => t.RFalse).Returns(Mock.Of<ILabel>());
            target.Setup(t => t.DefineLocal("?TMP")).Returns(temp);
            target.Setup(t => t.EmitStore(temp, global)).Callback(() => emitted.Add("store"));
            target.Setup(t => t.EmitCall(calledRoutine, It.IsAny<IOperand[]>(), null))
                .Callback(() => emitted.Add("call"));
            target.Setup(t => t.Return(temp)).Callback(() => emitted.Add("return"));

            var builder = new IrRoutineBuilder(target.Object, IrNumericSemantics.ZMachine16, true);
            var local = builder.DefineLocal("?TMP");
            builder.EmitStore(local, global);
            builder.EmitCall(calledRoutine, [], null);
            builder.Return(local);
            builder.Finish();

            CollectionAssert.AreEqual(new[] { "store", "call", "return" }, emitted);
        }

        [TestMethod]
        public void IrRoutineBuilder_Folds_Stack_Result_Chain_Without_Changing_Stack_Depth()
        {
            var target = new Mock<IRoutineBuilder>();
            var stack = Mock.Of<IVariable>();
            target.SetupGet(t => t.RoutineStart).Returns(Mock.Of<ILabel>());
            target.SetupGet(t => t.RTrue).Returns(Mock.Of<ILabel>());
            target.SetupGet(t => t.RFalse).Returns(Mock.Of<ILabel>());
            target.SetupGet(t => t.Stack).Returns(stack);
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
            builder.EmitBinary(BinaryOp.Add, MakeOperand(20), MakeOperand(22), builder.Stack);
            builder.EmitUnary(UnaryOp.Neg, builder.Stack, builder.Stack);
            builder.Return(builder.Stack);
            builder.Finish();

            target.Verify(t => t.EmitBinary(It.IsAny<BinaryOp>(), It.IsAny<IOperand>(), It.IsAny<IOperand>(),
                It.IsAny<IVariable>()), Times.Never);
            target.Verify(t => t.EmitUnary(It.IsAny<UnaryOp>(), It.IsAny<IOperand>(), It.IsAny<IVariable>()), Times.Never);
            target.Verify(t => t.EmitStore(stack, It.IsAny<IOperand>()), Times.Never);
            target.Verify(t => t.Return(operands[-42]), Times.Once);
        }

        [TestMethod]
        public void Gvn_Preserves_Memory_Value_Across_Dominated_Io_Block()
        {
            var routine = new RoutineIr();
            var middle = routine.CreateBlock();
            var end = routine.CreateBlock();
            var home = Mock.Of<IVariable>();
            var address = routine.CreateValue();
            var index = routine.CreateConstant(0);
            var first = routine.Append(routine.Entry, IrOpcode.LoadByte, [address, index], IrEffect.ReadMemory,
                new IrLoweringOperation(_ => { }, home));
            routine.Entry.Terminator = new IrTerminator.Jump(middle);
            routine.Append(middle, IrOpcode.TargetOperation, [], IrEffect.InputOutput, () => { }, hasResult: false);
            middle.Terminator = new IrTerminator.Jump(end);
            var second = routine.Append(end, IrOpcode.LoadByte, [address, index], IrEffect.ReadMemory,
                new IrLoweringOperation(_ => { }, home));
            end.Terminator = new IrTerminator.Return(second.Result);

            new RoutineIrOptimizer(IrNumericSemantics.ZMachine16).Optimize(routine);

            Assert.AreEqual(1, routine.Blocks.SelectMany(block => block.Instructions)
                .Count(instruction => instruction.Opcode == IrOpcode.LoadByte));
            Assert.AreSame(first.Result, ((IrTerminator.Return)end.Terminator).Value);
        }

        [TestMethod]
        public void IrRoutineBuilder_Gvn_Promotes_Repeated_Stack_Expression_To_Local()
        {
            var target = new Mock<IRoutineBuilder>();
            var stack = Mock.Of<IVariable>();
            var index = Mock.Of<ILocalBuilder>();
            var firstResult = Mock.Of<ILocalBuilder>();
            var secondResult = Mock.Of<ILocalBuilder>();
            var irTemporary = Mock.Of<ILocalBuilder>();
            var firstTable = Mock.Of<IOperand>();
            var secondTable = Mock.Of<IOperand>();
            var one = new Mock<INumericOperand>();
            one.SetupGet(operand => operand.Value).Returns(1);
            target.SetupGet(t => t.RoutineStart).Returns(Mock.Of<ILabel>());
            target.SetupGet(t => t.RTrue).Returns(Mock.Of<ILabel>());
            target.SetupGet(t => t.RFalse).Returns(Mock.Of<ILabel>());
            target.SetupGet(t => t.Stack).Returns(stack);
            target.Setup(t => t.DefineRequiredParameter("INDEX")).Returns(index);
            target.Setup(t => t.DefineLocal("FIRST")).Returns(firstResult);
            target.Setup(t => t.DefineLocal("SECOND")).Returns(secondResult);
            target.Setup(t => t.DefineLocal("?IR0")).Returns(irTemporary);

            var builder = new IrRoutineBuilder(target.Object, IrNumericSemantics.ZMachine16, true);
            var indexValue = builder.DefineRequiredParameter("INDEX");
            var first = builder.DefineLocal("FIRST");
            var second = builder.DefineLocal("SECOND");
            builder.EmitBinary(BinaryOp.Sub, indexValue, one.Object, builder.Stack);
            builder.EmitBinary(BinaryOp.GetByte, firstTable, builder.Stack, first);
            builder.EmitPrint(PrintOp.Number, first);
            builder.EmitBinary(BinaryOp.Sub, indexValue, one.Object, builder.Stack);
            builder.EmitBinary(BinaryOp.GetByte, secondTable, builder.Stack, second);
            builder.Return(second);
            builder.Finish();

            target.Verify(t => t.EmitBinary(BinaryOp.Sub, index, one.Object, irTemporary), Times.Once);
            target.Verify(t => t.EmitBinary(BinaryOp.Sub, index, one.Object, stack), Times.Never);
            target.Verify(t => t.EmitBinary(BinaryOp.GetByte, firstTable, irTemporary, firstResult), Times.Once);
            target.Verify(t => t.EmitBinary(BinaryOp.GetByte, secondTable, irTemporary, secondResult), Times.Once);
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
