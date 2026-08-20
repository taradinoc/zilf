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
        public void Ssa_Promotion_Inserts_Phi_At_Local_Join()
        {
            var routine = new RoutineIr();
            var left = routine.CreateBlock();
            var right = routine.CreateBlock();
            var join = routine.CreateBlock();
            var local = Mock.Of<IVariable>();
            var condition = routine.CreateExternalValue(mutable: false);
            routine.Entry.Terminator = new IrTerminator.Branch(condition, left, right);
            AppendMaterialization(routine, left, local, routine.CreateConstant(1));
            left.Terminator = new IrTerminator.Jump(join);
            AppendMaterialization(routine, right, local, routine.CreateConstant(2));
            right.Terminator = new IrTerminator.Jump(join);
            var localRead = routine.CreateExternalValue(mutable: true);
            localRead.PhysicalHome = local;
            var use = routine.Append(join, IrOpcode.Add, [localRead, routine.CreateConstant(3)]);
            join.Terminator = new IrTerminator.Return(use.Result);

            var count = routine.PromoteLocalsToSsa([local]);

            var phi = join.Instructions.Single(instruction => instruction.Opcode == IrOpcode.Phi);
            Assert.AreEqual(1, count);
            CollectionAssert.AreEqual(new int?[] { 1, 2 }, phi.Operands.Select(value => value.Constant).ToArray());
            Assert.AreSame(phi.Result, use.Operands[0]);
            routine.Verify();
        }

        [TestMethod]
        public void Ssa_Promotion_Connects_Loop_Carried_Local_To_Phi()
        {
            var routine = new RoutineIr();
            var header = routine.CreateBlock();
            var body = routine.CreateBlock();
            var exit = routine.CreateBlock();
            var local = Mock.Of<IVariable>();
            AppendMaterialization(routine, routine.Entry, local, routine.CreateConstant(0));
            routine.Entry.Terminator = new IrTerminator.Jump(header);
            var headerRead = routine.CreateExternalValue(mutable: true);
            headerRead.PhysicalHome = local;
            header.Terminator = new IrTerminator.Branch(routine.CreateExternalValue(mutable: false), body, exit);
            var increment = routine.Append(body, IrOpcode.Add, [headerRead, routine.CreateConstant(1)], payload:
                new IrLoweringOperation(_ => { }, local));
            AppendMaterialization(routine, body, local, increment.Result!);
            body.Terminator = new IrTerminator.Jump(header);
            exit.Terminator = new IrTerminator.Return(headerRead);

            routine.PromoteLocalsToSsa([local]);

            var phi = header.Instructions.Single(instruction => instruction.Opcode == IrOpcode.Phi);
            Assert.AreEqual(2, phi.Operands.Count);
            Assert.AreEqual(0, phi.Operands[0].Constant);
            Assert.AreSame(increment.Result, phi.Operands[1]);
            Assert.AreSame(phi.Result, increment.Operands[0]);
            Assert.AreSame(phi.Result, ((IrTerminator.Return)exit.Terminator).Value);
            routine.Verify();
        }

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
        public void Optimizer_Removes_Never_Taken_Recorded_Branch()
        {
            var routine = new RoutineIr();
            var target = routine.CreateBlock();
            var fallthrough = routine.CreateBlock();
            var branchInstruction = routine.Append(routine.Entry, IrOpcode.TargetOperation, [], IrEffect.Control,
                new IrLoweringOperation(_ => Assert.Fail("The branch should have been removed.")), hasResult: false);
            routine.Entry.Terminator = new IrTerminator.Branch(routine.CreateConstant(0), target, fallthrough,
                _ => { }, branchInstruction, target);
            target.Terminator = new IrTerminator.Return(null);
            fallthrough.Terminator = new IrTerminator.Return(null);

            new RoutineIrOptimizer().Optimize(routine);

            Assert.IsFalse(routine.Entry.Instructions.Contains(branchInstruction));
            Assert.AreSame(fallthrough, ((IrTerminator.Jump)routine.Entry.Terminator).Target);
        }

        [TestMethod]
        public void Optimizer_Rewrites_Always_Taken_Recorded_Branch_As_Jump()
        {
            var routine = new RoutineIr();
            var target = routine.CreateBlock();
            var fallthrough = routine.CreateBlock();
            IrBlock emittedTarget = null!;
            var branchInstruction = routine.Append(routine.Entry, IrOpcode.Equal, [], IrEffect.Control,
                new IrLoweringOperation(_ => Assert.Fail("The conditional branch should have been replaced.")));
            routine.Entry.Terminator = new IrTerminator.Branch(routine.CreateConstant(1), target, fallthrough,
                block => emittedTarget = block, branchInstruction, target);
            target.Terminator = new IrTerminator.Return(null);
            fallthrough.Terminator = new IrTerminator.Return(null);

            new RoutineIrOptimizer().Optimize(routine);
            ((IrLoweringOperation)branchInstruction.Payload!).Replay([]);

            Assert.AreSame(target, emittedTarget);
            Assert.AreEqual(IrOpcode.TargetOperation, branchInstruction.Opcode);
            Assert.AreEqual(0, branchInstruction.Operands.Count);
        }

        [TestMethod]
        public void Optimizer_Folds_Zero_Test_Of_Known_Nonzero_Symbol()
        {
            var routine = new RoutineIr();
            var target = routine.CreateBlock();
            var fallthrough = routine.CreateBlock();
            var symbol = routine.CreateExternalValue(mutable: false, knownNonzero: true);
            var comparison = routine.Append(routine.Entry, IrOpcode.Equal, [symbol, routine.CreateConstant(0)]);
            routine.Entry.Terminator = new IrTerminator.Branch(comparison.Result!, target, fallthrough);
            target.Terminator = new IrTerminator.Return(null);
            fallthrough.Terminator = new IrTerminator.Return(null);

            new RoutineIrOptimizer().Optimize(routine);

            Assert.AreSame(fallthrough, ((IrTerminator.Jump)routine.Entry.Terminator).Target);
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
        public void Optimizer_Reports_Applications_Rejections_And_Opaque_Counts()
        {
            var routine = new RoutineIr();
            var left = routine.CreateValue();
            var right = routine.CreateValue();
            var firstHome = Mock.Of<IVariable>();
            var secondHome = Mock.Of<IVariable>();
            routine.Append(routine.Entry, IrOpcode.TargetOperation, [], IrEffect.Opaque, hasResult: false);
            routine.Append(routine.Entry, IrOpcode.Add, [left, right], IrEffect.None,
                new IrLoweringOperation(_ => { }, firstHome));
            var duplicate = routine.Append(routine.Entry, IrOpcode.Add, [left, right], IrEffect.None,
                new IrLoweringOperation(_ => { }, secondHome));
            routine.Entry.Terminator = new IrTerminator.Return(duplicate.Result);
            var optimizer = new RoutineIrOptimizer();

            optimizer.Optimize(routine);

            var stats = optimizer.GetStatistics().ToDictionary(stat => stat.Name, stat => stat.Count);
            Assert.AreEqual(1, stats["Input opaque operations"]);
            Assert.AreEqual(1, stats["Output opaque operations"]);
            Assert.AreEqual(2, stats["GVN candidates"]);
            Assert.AreEqual(1, stats["GVN rejected: physical availability"]);
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
        public void Gvn_Does_Not_Eliminate_Required_Home_Definition()
        {
            var routine = new RoutineIr();
            var left = routine.CreateValue();
            var right = routine.CreateConstant(1);
            var firstHome = Mock.Of<IVariable>();
            var requiredHome = Mock.Of<IVariable>();
            routine.Append(routine.Entry, IrOpcode.Subtract, [left, right], payload:
                new IrLoweringOperation(_ => { }, firstHome, emitTo: (_, _) => { }));
            var requiredLowering = new IrLoweringOperation(_ => { }, requiredHome, emitTo: (_, _) => { })
            {
                RequiredHome = true,
            };
            var required = routine.Append(routine.Entry, IrOpcode.Subtract, [left, right], payload: requiredLowering);
            routine.Entry.Terminator = new IrTerminator.Return(required.Result);

            new RoutineIrOptimizer().Optimize(routine);

            Assert.AreEqual(IrOpcode.Subtract, required.Opcode);
            Assert.AreSame(requiredHome, requiredLowering.ResultHome);
        }

        [TestMethod]
        public void Gvn_Does_Not_Copy_Consumed_Stack_Result_Into_Local_Home()
        {
            var routine = new RoutineIr();
            var left = routine.CreateValue();
            var right = routine.CreateConstant(1);
            var stack = Mock.Of<IVariable>();
            var local = Mock.Of<IVariable>();
            routine.Append(routine.Entry, IrOpcode.Subtract, [left, right], payload:
                new IrLoweringOperation(_ => { }, stack, isStackResult: true, emitTo: (_, _) => { }));
            var second = routine.Append(routine.Entry, IrOpcode.Subtract, [left, right], payload:
                new IrLoweringOperation(_ => { }, local, emitTo: (_, _) => { }));
            routine.Entry.Terminator = new IrTerminator.Return(second.Result);

            new RoutineIrOptimizer(emitCopy: (_, _) => { }).Optimize(routine);

            Assert.AreEqual(IrOpcode.Subtract, second.Opcode);
        }

        [TestMethod]
        public void Gvn_Reuses_Promoted_Stack_Result_For_Later_Stack_Expressions()
        {
            var routine = new RoutineIr();
            var left = routine.CreateValue();
            var right = routine.CreateConstant(1);
            var stack = Mock.Of<IVariable>();
            var temporary = Mock.Of<IVariable>();
            var first = routine.Append(routine.Entry, IrOpcode.Subtract, [left, right], payload:
                new IrLoweringOperation(_ => { }, stack, isStackResult: true, emitTo: (_, _) => { }));
            var second = routine.Append(routine.Entry, IrOpcode.Subtract, [left, right], payload:
                new IrLoweringOperation(_ => { }, stack, isStackResult: true, emitTo: (_, _) => { }));
            var third = routine.Append(routine.Entry, IrOpcode.Subtract, [left, right], payload:
                new IrLoweringOperation(_ => { }, stack, isStackResult: true, emitTo: (_, _) => { }));
            routine.Entry.Terminator = new IrTerminator.Return(third.Result);

            new RoutineIrOptimizer(acquireTemporary: () => temporary).Optimize(routine);

            Assert.AreEqual(1, routine.Entry.Instructions.Count(instruction =>
                instruction.Opcode == IrOpcode.Subtract));
            Assert.AreSame(temporary, ((IrLoweringOperation)first.Payload!).ResultHome);
            Assert.AreSame(first.Result, ((IrTerminator.Return)routine.Entry.Terminator).Value);
            Assert.IsFalse(routine.Entry.Instructions.Any(instruction => ReferenceEquals(instruction.Result,
                second.Result) || ReferenceEquals(instruction.Result, third.Result)));
        }

        [TestMethod]
        public void Gvn_Preserves_Multiple_Virtual_Stack_Expressions_Until_Later_Uses()
        {
            var routine = new RoutineIr();
            var source = routine.CreateValue();
            var firstOffset = routine.CreateConstant(1);
            var secondOffset = routine.CreateConstant(2);
            var stack = Mock.Of<IVariable>();
            var firstTemporary = Mock.Of<IVariable>();
            var secondTemporary = Mock.Of<IVariable>();
            var temporaries = new Queue<IVariable>([firstTemporary, secondTemporary]);
            var first = routine.Append(routine.Entry, IrOpcode.LoadProperty, [source, firstOffset],
                IrEffect.ReadMemory,
                new IrLoweringOperation(_ => { }, stack, isStackResult: true, emitTo: (_, _) => { }),
                readRegions: IrMemoryRegion.Properties);
            var second = routine.Append(routine.Entry, IrOpcode.LoadProperty, [source, secondOffset],
                IrEffect.ReadMemory,
                new IrLoweringOperation(_ => { }, stack, isStackResult: true, emitTo: (_, _) => { }),
                readRegions: IrMemoryRegion.Properties);
            routine.Append(routine.Entry, IrOpcode.TargetOperation, [first.Result!, second.Result!],
                IrEffect.Control, hasResult: false);
            var firstDuplicate = routine.Append(routine.Entry, IrOpcode.LoadProperty, [source, firstOffset],
                IrEffect.ReadMemory,
                new IrLoweringOperation(_ => { }, stack, isStackResult: true, emitTo: (_, _) => { }),
                readRegions: IrMemoryRegion.Properties);
            var secondDuplicate = routine.Append(routine.Entry, IrOpcode.LoadProperty, [source, secondOffset],
                IrEffect.ReadMemory,
                new IrLoweringOperation(_ => { }, stack, isStackResult: true, emitTo: (_, _) => { }),
                readRegions: IrMemoryRegion.Properties);
            routine.Entry.Terminator = new IrTerminator.Return(secondDuplicate.Result);

            new RoutineIrOptimizer(acquireTemporary: () => temporaries.Dequeue()).Optimize(routine);

            Assert.AreEqual(2, routine.Entry.Instructions.Count(instruction =>
                instruction.Opcode == IrOpcode.LoadProperty));
            Assert.AreSame(firstTemporary, ((IrLoweringOperation)first.Payload!).ResultHome);
            Assert.AreSame(secondTemporary, ((IrLoweringOperation)second.Payload!).ResultHome);
            Assert.IsFalse(routine.Entry.Instructions.Any(instruction =>
                ReferenceEquals(instruction.Result, firstDuplicate.Result) ||
                ReferenceEquals(instruction.Result, secondDuplicate.Result)));
        }

        [TestMethod]
        public void Gvn_Invalidates_Old_Expression_When_Scavenged_Home_Is_Reused()
        {
            var routine = new RoutineIr();
            var left = routine.CreateValue();
            var firstRight = routine.CreateConstant(1);
            var secondRight = routine.CreateConstant(2);
            var stack = Mock.Of<IVariable>();
            var reusable = Mock.Of<IVariable>();
            IrInstruction Add(IrValue right) => routine.Append(routine.Entry, IrOpcode.Add, [left, right],
                payload: new IrLoweringOperation(_ => { }, stack, isStackResult: true, emitTo: (_, _) => { }));
            var first = Add(firstRight);
            routine.Append(routine.Entry, IrOpcode.TargetOperation, [first.Result!], IrEffect.Control,
                hasResult: false);
            var second = Add(secondRight);
            routine.Append(routine.Entry, IrOpcode.TargetOperation, [second.Result!], IrEffect.Control,
                hasResult: false);
            var firstDuplicate = Add(firstRight);
            routine.Append(routine.Entry, IrOpcode.TargetOperation, [firstDuplicate.Result!], IrEffect.Control,
                hasResult: false);
            var secondDuplicate = Add(secondRight);
            routine.Append(routine.Entry, IrOpcode.TargetOperation, [secondDuplicate.Result!], IrEffect.Control,
                hasResult: false);
            var finalFirst = Add(firstRight);
            routine.Entry.Terminator = new IrTerminator.Return(finalFirst.Result);

            new RoutineIrOptimizer(reusableTemporaries: () => [reusable]).Optimize(routine);

            Assert.AreEqual(3, routine.Entry.Instructions.Count(instruction => instruction.Opcode == IrOpcode.Add));
            Assert.IsTrue(routine.Entry.Instructions.Any(instruction => ReferenceEquals(instruction.Result,
                secondDuplicate.Result)));
        }

        [TestMethod]
        public void Gvn_Preserves_Dominating_Stack_Value_Across_Cfg_Edge()
        {
            var routine = new RoutineIr();
            var continuation = routine.CreateBlock();
            var source = routine.CreateValue();
            var offset = routine.CreateConstant(1);
            var condition = routine.CreateValue();
            var stack = Mock.Of<IVariable>();
            var local = Mock.Of<IVariable>();
            var temporary = Mock.Of<IVariable>();
            var first = routine.Append(routine.Entry, IrOpcode.LoadByte, [source, offset], IrEffect.ReadMemory,
                new IrLoweringOperation(_ => { }, stack, isStackResult: true, emitTo: (_, _) => { }),
                readRegions: IrMemoryRegion.Tables);
            routine.Entry.Terminator = new IrTerminator.Branch(condition, continuation, continuation);
            var second = routine.Append(continuation, IrOpcode.LoadByte, [source, offset], IrEffect.ReadMemory,
                new IrLoweringOperation(_ => { }, local, emitTo: (_, _) => { }),
                readRegions: IrMemoryRegion.Tables);
            continuation.Terminator = new IrTerminator.Return(second.Result);

            new RoutineIrOptimizer(acquireTemporary: () => temporary).Optimize(routine);

            Assert.AreEqual(1, routine.Blocks.SelectMany(block => block.Instructions)
                .Count(instruction => instruction.Opcode == IrOpcode.LoadByte));
            Assert.AreSame(temporary, ((IrLoweringOperation)first.Payload!).ResultHome);
            Assert.AreSame(first.Result, ((IrTerminator.Return)continuation.Terminator).Value);
        }

        [TestMethod]
        public void Gvn_Copies_Dominating_Memory_Value_Into_Required_Home_Across_Cfg_Edge()
        {
            var routine = new RoutineIr();
            var continuation = routine.CreateBlock();
            var source = routine.CreateValue();
            var offset = routine.CreateConstant(1);
            var condition = routine.CreateValue();
            var stack = Mock.Of<IVariable>();
            var local = Mock.Of<IVariable>();
            var temporary = Mock.Of<IVariable>();
            var first = routine.Append(routine.Entry, IrOpcode.LoadByte, [source, offset], IrEffect.ReadMemory,
                new IrLoweringOperation(_ => { }, stack, isStackResult: true, emitTo: (_, _) => { }),
                readRegions: IrMemoryRegion.Tables);
            routine.Entry.Terminator = new IrTerminator.Branch(condition, continuation, continuation);
            var requiredLowering = new IrLoweringOperation(_ => { }, local, emitTo: (_, _) => { })
            {
                RequiredHome = true,
            };
            var second = routine.Append(continuation, IrOpcode.LoadByte, [source, offset], IrEffect.ReadMemory,
                requiredLowering, readRegions: IrMemoryRegion.Tables);
            continuation.Terminator = new IrTerminator.Return(second.Result);
            var copies = new List<(IVariable Destination, IOperand Source)>();

            new RoutineIrOptimizer(acquireTemporary: () => temporary,
                emitCopy: (destination, value) => copies.Add((destination, value))).Optimize(routine);
            var copy = continuation.Instructions.Single(instruction => ReferenceEquals(instruction.Result,
                second.Result));
            ((IrLoweringOperation)copy.Payload!).Replay([temporary]);

            Assert.AreEqual(1, routine.Blocks.SelectMany(block => block.Instructions)
                .Count(instruction => instruction.Opcode == IrOpcode.LoadByte));
            Assert.AreEqual(IrOpcode.TargetOperation, copy.Opcode);
            Assert.AreSame(first.Result, copy.Operands[0]);
            Assert.AreSame(temporary, ((IrLoweringOperation)first.Payload!).ResultHome);
            Assert.AreEqual((local, temporary), copies.Single());
        }

        [TestMethod]
        public void Gvn_Does_Not_Preserve_Memory_Value_Across_Clobbering_Diamond_Path()
        {
            var routine = new RoutineIr();
            var left = routine.CreateBlock();
            var right = routine.CreateBlock();
            var join = routine.CreateBlock();
            var source = routine.CreateValue();
            var offset = routine.CreateConstant(1);
            var condition = routine.CreateValue();
            var home = Mock.Of<IVariable>();
            var first = routine.Append(routine.Entry, IrOpcode.LoadByte, [source, offset], IrEffect.ReadMemory,
                new IrLoweringOperation(_ => { }, home), readRegions: IrMemoryRegion.Tables);
            routine.Append(routine.Entry, IrOpcode.TargetOperation, [first.Result!], IrEffect.InputOutput,
                hasResult: false);
            routine.Entry.Terminator = new IrTerminator.Branch(condition, left, right);
            left.Instructions.Add(new IrInstruction(IrOpcode.TargetOperation, null, [], IrEffect.Call));
            left.Terminator = new IrTerminator.Jump(join);
            right.Terminator = new IrTerminator.Jump(join);
            var second = routine.Append(join, IrOpcode.LoadByte, [source, offset], IrEffect.ReadMemory,
                new IrLoweringOperation(_ => { }, home), readRegions: IrMemoryRegion.Tables);
            join.Terminator = new IrTerminator.Return(second.Result);

            new RoutineIrOptimizer().Optimize(routine);

            Assert.AreSame(second.Result, ((IrTerminator.Return)join.Terminator).Value);
            Assert.AreEqual(2, routine.Blocks.SelectMany(block => block.Instructions)
                .Count(instruction => instruction.Opcode == IrOpcode.LoadByte));
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
        public void Optimizer_Coalesces_Result_With_Nonadjacent_Copy_Destination()
        {
            var routine = new RoutineIr();
            var originalHome = Mock.Of<IVariable>();
            var destination = Mock.Of<IVariable>();
            var lowering = new IrLoweringOperation(_ => { }, originalHome, emitTo: (_, _) => { });
            var producer = routine.Append(routine.Entry, IrOpcode.Add,
                [routine.CreateValue(), routine.CreateValue()], payload: lowering);
            routine.Append(routine.Entry, IrOpcode.TargetOperation, [], IrEffect.InputOutput, hasResult: false);
            var copyLowering = new IrLoweringOperation(_ => { }, destination) { RequiredHome = true };
            var copy = routine.Append(routine.Entry, IrOpcode.Copy, [producer.Result!], payload: copyLowering);
            routine.Entry.Terminator = new IrTerminator.Return(copy.Result);

            new RoutineIrOptimizer().Optimize(routine);

            Assert.AreSame(destination, lowering.ResultHome);
            Assert.IsFalse(routine.Entry.Instructions.Any(instruction => ReferenceEquals(instruction.Result,
                copy.Result)));
        }

        [TestMethod]
        public void Optimizer_Coalesces_Result_With_End_Of_Block_Materialization()
        {
            var routine = new RoutineIr();
            var originalHome = Mock.Of<IVariable>();
            var destination = Mock.Of<IVariable>();
            var lowering = new IrLoweringOperation(_ => { }, originalHome, emitTo: (_, _) => { });
            var producer = routine.Append(routine.Entry, IrOpcode.Add,
                [routine.CreateValue(), routine.CreateValue()], payload: lowering);
            routine.Append(routine.Entry, IrOpcode.TargetOperation, [], IrEffect.InputOutput, hasResult: false);
            var materializationLowering = new IrLoweringOperation(_ => { }, destination)
            {
                IsMaterialization = true,
            };
            routine.Append(routine.Entry, IrOpcode.TargetOperation, [producer.Result!], IrEffect.Control,
                materializationLowering, hasResult: false);
            routine.Entry.Terminator = new IrTerminator.Return(null);

            new RoutineIrOptimizer().Optimize(routine);

            Assert.AreSame(destination, lowering.ResultHome);
            Assert.IsTrue(lowering.RequiredHome);
            Assert.IsFalse(routine.Entry.Instructions.Any(instruction =>
                ReferenceEquals(instruction.Payload, materializationLowering)));
        }

        [TestMethod]
        public void Optimizer_Does_Not_Coalesce_Across_Destination_Home_Use()
        {
            var routine = new RoutineIr();
            var originalHome = Mock.Of<IVariable>();
            var destination = Mock.Of<IVariable>();
            var destinationValue = routine.CreateValue();
            destinationValue.PhysicalHome = destination;
            var lowering = new IrLoweringOperation(_ => { }, originalHome, emitTo: (_, _) => { });
            var producer = routine.Append(routine.Entry, IrOpcode.Add,
                [routine.CreateValue(), routine.CreateValue()], payload: lowering);
            routine.Append(routine.Entry, IrOpcode.TargetOperation, [destinationValue], IrEffect.InputOutput,
                hasResult: false);
            var copyLowering = new IrLoweringOperation(_ => { }, destination) { RequiredHome = true };
            var copy = routine.Append(routine.Entry, IrOpcode.Copy, [producer.Result!], payload: copyLowering);
            routine.Entry.Terminator = new IrTerminator.Return(copy.Result);

            new RoutineIrOptimizer().Optimize(routine);

            Assert.AreSame(originalHome, lowering.ResultHome);
            Assert.IsTrue(routine.Entry.Instructions.Any(instruction => ReferenceEquals(instruction.Result,
                copy.Result)));
        }

        [TestMethod]
        public void Optimizer_Preserves_Copy_When_Source_Home_Is_Clobbered_Before_Use()
        {
            var routine = new RoutineIr();
            var sourceHome = Mock.Of<IVariable>();
            var snapshotHome = Mock.Of<IVariable>();
            var source = routine.CreateValue();
            source.PhysicalHome = sourceHome;
            var snapshot = routine.Append(routine.Entry, IrOpcode.Copy, [source], payload:
                new IrLoweringOperation(_ => { }, snapshotHome));
            routine.Append(routine.Entry, IrOpcode.TargetOperation, [], IrEffect.Call,
                new IrLoweringOperation(_ => { }, sourceHome), callSummary: new IrRoutineEffectSummary
                {
                    IsComplete = true,
                });
            routine.Entry.Terminator = new IrTerminator.Return(snapshot.Result);

            new RoutineIrOptimizer(emitCopy: (_, _) => { }).Optimize(routine);

            var preserved = routine.Entry.Instructions.Single(instruction =>
                ReferenceEquals(instruction.Result, snapshot.Result));
            Assert.IsTrue(((IrLoweringOperation)preserved.Payload!).RequiredHome);
            Assert.AreSame(snapshotHome, ((IrLoweringOperation)preserved.Payload!).ResultHome);
        }

        [TestMethod]
        public void Optimizer_Does_Not_Retarget_Producer_Whose_Original_Home_Is_Required()
        {
            var routine = new RoutineIr();
            var sourceHome = Mock.Of<IVariable>();
            var copyHome = Mock.Of<IVariable>();
            var producerLowering = new IrLoweringOperation(_ => { }, sourceHome, emitTo: (_, _) => { })
            {
                RequiredHome = true,
            };
            var producer = routine.Append(routine.Entry, IrOpcode.Add,
                [routine.CreateValue(), routine.CreateValue()], payload: producerLowering);
            var copyLowering = new IrLoweringOperation(_ => { }, copyHome)
            {
                RequiredHome = true,
            };
            var copy = routine.Append(routine.Entry, IrOpcode.Copy, [producer.Result!], payload: copyLowering);
            routine.Entry.Terminator = new IrTerminator.Return(copy.Result);

            new RoutineIrOptimizer().Optimize(routine);

            Assert.AreSame(sourceHome, producerLowering.ResultHome);
            Assert.IsTrue(routine.Entry.Instructions.Any(instruction => ReferenceEquals(instruction.Result,
                copy.Result)));
        }

        [TestMethod]
        public void Optimizer_Does_Not_Retarget_Call_Result()
        {
            var routine = new RoutineIr();
            var callHome = Mock.Of<IVariable>();
            var copyHome = Mock.Of<IVariable>();
            var callLowering = new IrLoweringOperation(_ => { }, callHome, emitTo: (_, _) => { });
            var call = routine.Append(routine.Entry, IrOpcode.TargetOperation, [], IrEffect.Call,
                callLowering, callSummary: new IrRoutineEffectSummary { IsComplete = true });
            var copy = routine.Append(routine.Entry, IrOpcode.Copy, [call.Result!], payload:
                new IrLoweringOperation(_ => { }, copyHome));
            routine.Entry.Terminator = new IrTerminator.Return(copy.Result);

            new RoutineIrOptimizer().Optimize(routine);

            Assert.AreSame(callHome, callLowering.ResultHome);
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
        public void Gvn_Preserves_Table_Read_Across_Unrelated_Property_Write()
        {
            var routine = new RoutineIr();
            var table = routine.CreateValue();
            var index = routine.CreateValue();
            var home = Mock.Of<IVariable>();
            var first = routine.Append(routine.Entry, IrOpcode.LoadByte, [table, index], IrEffect.ReadMemory,
                new IrLoweringOperation(_ => { }, home), readRegions: IrMemoryRegion.Tables);
            routine.Append(routine.Entry, IrOpcode.TargetOperation, [], IrEffect.WriteMemory, hasResult: false,
                writeRegions: IrMemoryRegion.Properties);
            var second = routine.Append(routine.Entry, IrOpcode.LoadByte, [table, index], IrEffect.ReadMemory,
                new IrLoweringOperation(_ => { }, home), readRegions: IrMemoryRegion.Tables);
            routine.Entry.Terminator = new IrTerminator.Return(second.Result);

            new RoutineIrOptimizer().Optimize(routine);

            Assert.AreSame(first.Result, ((IrTerminator.Return)routine.Entry.Terminator).Value);
            Assert.AreEqual(1,
                routine.Entry.Instructions.Count(instruction => instruction.Opcode == IrOpcode.LoadByte));
        }

        [TestMethod]
        public void Gvn_Invalidates_Table_Read_Across_Table_Write()
        {
            var routine = new RoutineIr();
            var table = routine.CreateValue();
            var index = routine.CreateValue();
            var home = Mock.Of<IVariable>();
            var first = routine.Append(routine.Entry, IrOpcode.LoadByte, [table, index], IrEffect.ReadMemory,
                new IrLoweringOperation(_ => { }, home), readRegions: IrMemoryRegion.Tables);
            routine.Append(routine.Entry, IrOpcode.TargetOperation, [first.Result!], IrEffect.InputOutput,
                hasResult: false);
            routine.Append(routine.Entry, IrOpcode.TargetOperation, [], IrEffect.WriteMemory, hasResult: false,
                writeRegions: IrMemoryRegion.Tables);
            var second = routine.Append(routine.Entry, IrOpcode.LoadByte, [table, index], IrEffect.ReadMemory,
                new IrLoweringOperation(_ => { }, home), readRegions: IrMemoryRegion.Tables);
            routine.Entry.Terminator = new IrTerminator.Return(second.Result);

            new RoutineIrOptimizer().Optimize(routine);

            Assert.AreEqual(2,
                routine.Entry.Instructions.Count(instruction => instruction.Opcode == IrOpcode.LoadByte));
        }

        [TestMethod]
        public void Routine_Effect_Summaries_Reach_A_Fixed_Point_Across_Recursion()
        {
            var first = new IrRoutineEffectSummary
            {
                DirectWrites = IrMemoryRegion.Globals,
                IsComplete = true,
            };
            var second = new IrRoutineEffectSummary
            {
                DirectWrites = IrMemoryRegion.Tables,
                IsComplete = true,
            };
            first.AddCallee(second);
            second.AddCallee(first);

            Assert.AreEqual(IrMemoryRegion.Globals | IrMemoryRegion.Tables, first.GetWrittenRegions());
            Assert.AreEqual(IrMemoryRegion.Globals | IrMemoryRegion.Tables, second.GetWrittenRegions());
        }

        [TestMethod]
        public void Gvn_Uses_Routine_Effect_Summary_At_Call()
        {
            var routine = new RoutineIr();
            var table = routine.CreateValue();
            var index = routine.CreateValue();
            var home = Mock.Of<IVariable>();
            var summary = new IrRoutineEffectSummary
            {
                DirectWrites = IrMemoryRegion.Properties,
                IsComplete = true,
            };
            var first = routine.Append(routine.Entry, IrOpcode.LoadByte, [table, index], IrEffect.ReadMemory,
                new IrLoweringOperation(_ => { }, home), readRegions: IrMemoryRegion.Tables);
            routine.Append(routine.Entry, IrOpcode.TargetOperation, [], IrEffect.Call, hasResult: false,
                callSummary: summary);
            var second = routine.Append(routine.Entry, IrOpcode.LoadByte, [table, index], IrEffect.ReadMemory,
                new IrLoweringOperation(_ => { }, home), readRegions: IrMemoryRegion.Tables);
            routine.Entry.Terminator = new IrTerminator.Return(second.Result);

            new RoutineIrOptimizer().Optimize(routine);

            Assert.AreSame(first.Result, ((IrTerminator.Return)routine.Entry.Terminator).Value);
            Assert.AreEqual(1,
                routine.Entry.Instructions.Count(instruction => instruction.Opcode == IrOpcode.LoadByte));
        }

        [TestMethod]
        public void Licm_Does_Not_Hoist_Global_Derived_Expression_Across_Global_Writing_Call()
        {
            var routine = new RoutineIr();
            var header = routine.CreateBlock();
            var body = routine.CreateBlock();
            var exit = routine.CreateBlock();
            var global = routine.CreateExternalValue(mutable: true);
            var home = Mock.Of<IVariable>();
            routine.Entry.Terminator = new IrTerminator.Jump(header);
            var expression = routine.Append(header, IrOpcode.Add, [global, routine.CreateConstant(2)], payload:
                new IrLoweringOperation(_ => { }, home));
            header.Terminator = new IrTerminator.Branch(routine.CreateValue(), body, exit);
            routine.Append(body, IrOpcode.TargetOperation, [], IrEffect.Call, hasResult: false);
            body.Terminator = new IrTerminator.Jump(header);
            exit.Terminator = new IrTerminator.Return(expression.Result);

            new RoutineIrOptimizer().Optimize(routine);

            Assert.IsTrue(header.Instructions.Contains(expression));
            Assert.IsFalse(routine.Entry.Instructions.Contains(expression));
        }

        [TestMethod]
        public void Licm_Creates_Preheader_When_External_Edges_Are_Redirectable()
        {
            var routine = new RoutineIr();
            var left = routine.CreateBlock();
            var right = routine.CreateBlock();
            var header = routine.CreateBlock();
            var body = routine.CreateBlock();
            var exit = routine.CreateBlock();
            var home = Mock.Of<IVariable>();
            routine.Entry.Terminator = new IrTerminator.Branch(routine.CreateValue(), left, right);
            left.Terminator = new IrTerminator.Jump(header);
            right.Terminator = new IrTerminator.Jump(header);
            var invariant = routine.Append(header, IrOpcode.Add,
                [routine.CreateValue(), routine.CreateValue()], payload: new IrLoweringOperation(_ => { }, home));
            header.Terminator = new IrTerminator.Branch(routine.CreateValue(), body, exit);
            body.Terminator = new IrTerminator.Jump(header);
            exit.Terminator = new IrTerminator.Return(invariant.Result);

            IrBlock created = null;
            var optimizer = new RoutineIrOptimizer(createPreheader: target =>
            {
                created = routine.CreateBlock();
                created.Terminator = new IrTerminator.Jump(target);
                return created;
            });
            optimizer.Optimize(routine);

            Assert.IsNotNull(created);
            Assert.AreSame(created, ((IrTerminator.Jump)left.Terminator).Target);
            Assert.AreSame(created, ((IrTerminator.Jump)right.Terminator).Target);
            Assert.IsTrue(created.Instructions.Contains(invariant));
            Assert.AreEqual(1, optimizer.GetStatistics().Single(stat => stat.Name == "Preheaders created").Count);
        }

        [TestMethod]
        public void Optimizer_Removes_Redundant_Basic_Induction_Variable()
        {
            var routine = new RoutineIr();
            var header = routine.CreateBlock();
            var body = routine.CreateBlock();
            var exit = routine.CreateBlock();
            var firstVariable = Mock.Of<IVariable>();
            var secondVariable = Mock.Of<IVariable>();
            routine.Entry.Terminator = new IrTerminator.Jump(header);
            var firstPhi = new IrInstruction(IrOpcode.Phi, routine.CreateValue(), [], payload:
                new IrPhi(firstVariable));
            var secondPhi = new IrInstruction(IrOpcode.Phi, routine.CreateValue(), [], payload:
                new IrPhi(secondVariable));
            firstPhi.Result!.PhysicalHome = firstVariable;
            secondPhi.Result!.PhysicalHome = secondVariable;
            header.Instructions.Add(firstPhi);
            header.Instructions.Add(secondPhi);
            header.Terminator = new IrTerminator.Branch(routine.CreateValue(), body, exit);
            var firstUpdate = routine.Append(body, IrOpcode.Add, [firstPhi.Result, routine.CreateConstant(1)], payload:
                new IrLoweringOperation(_ => { }, firstVariable));
            var secondUpdate = routine.Append(body, IrOpcode.Add, [secondPhi.Result, routine.CreateConstant(1)], payload:
                new IrLoweringOperation(_ => { }, secondVariable));
            ((IrPhi)firstPhi.Payload!).Incoming[routine.Entry] = routine.CreateConstant(0);
            ((IrPhi)firstPhi.Payload!).Incoming[body] = firstUpdate.Result!;
            ((IrPhi)secondPhi.Payload!).Incoming[routine.Entry] = routine.CreateConstant(0);
            ((IrPhi)secondPhi.Payload!).Incoming[body] = secondUpdate.Result!;
            body.Terminator = new IrTerminator.Jump(header);
            exit.Terminator = new IrTerminator.Return(secondPhi.Result);

            var optimizer = new RoutineIrOptimizer();
            optimizer.Optimize(routine);

            Assert.AreEqual(1, header.Instructions.Count(instruction => instruction.Opcode == IrOpcode.Phi));
            Assert.AreEqual(1, body.Instructions.Count(instruction => instruction.Opcode == IrOpcode.Add));
            Assert.AreSame(firstPhi.Result, ((IrTerminator.Return)exit.Terminator).Value);
            Assert.AreEqual(1, optimizer.GetStatistics().Single(stat =>
                stat.Name == "Redundant induction variables removed").Count);
        }

        [TestMethod]
        public void Licm_Hoists_Global_Derived_Expression_Across_Unrelated_Write()
        {
            var routine = new RoutineIr();
            var header = routine.CreateBlock();
            var body = routine.CreateBlock();
            var exit = routine.CreateBlock();
            var global = routine.CreateExternalValue(mutable: true);
            var home = Mock.Of<IVariable>();
            routine.Entry.Terminator = new IrTerminator.Jump(header);
            var expression = routine.Append(header, IrOpcode.Add, [global, routine.CreateConstant(2)], payload:
                new IrLoweringOperation(_ => { }, home));
            header.Terminator = new IrTerminator.Branch(routine.CreateValue(), body, exit);
            routine.Append(body, IrOpcode.TargetOperation, [], IrEffect.WriteMemory, hasResult: false,
                writeRegions: IrMemoryRegion.Properties);
            body.Terminator = new IrTerminator.Jump(header);
            exit.Terminator = new IrTerminator.Return(expression.Result);

            new RoutineIrOptimizer().Optimize(routine);

            Assert.IsTrue(routine.Entry.Instructions.Contains(expression));
            Assert.IsFalse(header.Instructions.Contains(expression));
        }

        [TestMethod]
        public void Licm_Does_Not_Hoist_Transitively_Global_Derived_Expression_Across_Call()
        {
            var routine = new RoutineIr();
            var header = routine.CreateBlock();
            var body = routine.CreateBlock();
            var exit = routine.CreateBlock();
            var global = routine.CreateExternalValue(mutable: true);
            var firstHome = Mock.Of<IVariable>();
            var secondHome = Mock.Of<IVariable>();
            routine.Entry.Terminator = new IrTerminator.Jump(header);
            var first = routine.Append(header, IrOpcode.Add, [global, routine.CreateConstant(1)], payload:
                new IrLoweringOperation(_ => { }, firstHome));
            var second = routine.Append(header, IrOpcode.Multiply, [first.Result!, routine.CreateConstant(2)], payload:
                new IrLoweringOperation(_ => { }, secondHome));
            header.Terminator = new IrTerminator.Branch(routine.CreateValue(), body, exit);
            routine.Append(body, IrOpcode.TargetOperation, [], IrEffect.Call, hasResult: false);
            body.Terminator = new IrTerminator.Jump(header);
            exit.Terminator = new IrTerminator.Return(second.Result);

            new RoutineIrOptimizer().Optimize(routine);

            Assert.IsTrue(header.Instructions.Contains(first));
            Assert.IsTrue(header.Instructions.Contains(second));
        }

        [TestMethod]
        public void Gvn_Memory_Versions_Differ_When_One_Join_Path_Writes()
        {
            var routine = new RoutineIr();
            var changed = routine.CreateBlock();
            var unchanged = routine.CreateBlock();
            var join = routine.CreateBlock();
            var table = routine.CreateValue();
            var index = routine.CreateValue();
            var firstHome = Mock.Of<IVariable>();
            var secondHome = Mock.Of<IVariable>();
            var first = routine.Append(routine.Entry, IrOpcode.LoadByte, [table, index], IrEffect.ReadMemory,
                new IrLoweringOperation(_ => { }, firstHome), readRegions: IrMemoryRegion.Tables);
            routine.Append(routine.Entry, IrOpcode.TargetOperation, [first.Result!], IrEffect.InputOutput,
                hasResult: false);
            routine.Entry.Terminator = new IrTerminator.Branch(routine.CreateValue(), changed, unchanged);
            changed.Instructions.Add(new IrInstruction(IrOpcode.TargetOperation, null, [], IrEffect.WriteMemory,
                writeRegions: IrMemoryRegion.Tables));
            changed.Terminator = new IrTerminator.Jump(join);
            unchanged.Terminator = new IrTerminator.Jump(join);
            var second = routine.Append(join, IrOpcode.LoadByte, [table, index], IrEffect.ReadMemory,
                new IrLoweringOperation(_ => { }, secondHome), readRegions: IrMemoryRegion.Tables);
            join.Terminator = new IrTerminator.Return(second.Result);

            new RoutineIrOptimizer().Optimize(routine);

            Assert.AreEqual(2, routine.Blocks.SelectMany(block => block.Instructions)
                .Count(instruction => instruction.Opcode == IrOpcode.LoadByte));
        }

        [TestMethod]
        public void Licm_Hoists_Invariant_Arithmetic_Into_Existing_Preheader()
        {
            var routine = new RoutineIr();
            var header = routine.CreateBlock();
            var body = routine.CreateBlock();
            var exit = routine.CreateBlock();
            var left = routine.CreateValue();
            var right = routine.CreateValue();
            var home = Mock.Of<IVariable>();
            routine.Entry.Terminator = new IrTerminator.Jump(header);
            var invariant = routine.Append(header, IrOpcode.Add, [left, right], payload:
                new IrLoweringOperation(_ => { }, home));
            header.Terminator = new IrTerminator.Branch(routine.CreateValue(), body, exit);
            body.Terminator = new IrTerminator.Jump(header);
            exit.Terminator = new IrTerminator.Return(invariant.Result);

            var optimizer = new RoutineIrOptimizer();
            optimizer.Optimize(routine);

            Assert.IsTrue(routine.Entry.Instructions.Contains(invariant));
            Assert.IsFalse(header.Instructions.Contains(invariant));
            Assert.AreEqual(1, optimizer.GetStatistics().Single(stat =>
                stat.Name == "LICM hoisted instructions").Count);
        }

        [TestMethod]
        public void Licm_Promotes_Invariant_Stack_Result_To_Compiler_Temporary()
        {
            var routine = new RoutineIr();
            var header = routine.CreateBlock();
            var body = routine.CreateBlock();
            var exit = routine.CreateBlock();
            var stack = Mock.Of<IVariable>();
            var temporary = Mock.Of<IVariable>();
            routine.Entry.Terminator = new IrTerminator.Jump(header);
            var lowering = new IrLoweringOperation(_ => { }, stack, isStackResult: true, emitTo: (_, _) => { });
            var invariant = routine.Append(header, IrOpcode.Add,
                [routine.CreateValue(), routine.CreateValue()], payload: lowering);
            header.Terminator = new IrTerminator.Branch(routine.CreateValue(), body, exit);
            body.Terminator = new IrTerminator.Jump(header);
            exit.Terminator = new IrTerminator.Return(invariant.Result);

            var optimizer = new RoutineIrOptimizer(acquireTemporary: () => temporary);
            optimizer.Optimize(routine);

            Assert.IsTrue(routine.Entry.Instructions.Contains(invariant));
            Assert.AreSame(temporary, lowering.ResultHome);
            Assert.IsFalse(lowering.IsStackResult);
            Assert.AreSame(temporary, invariant.Result.PhysicalHome);
            Assert.AreEqual(1, optimizer.GetStatistics().Single(stat =>
                stat.Name == "LICM promoted stack results").Count);
        }

        [TestMethod]
        public void Licm_Does_Not_Allocate_Temporary_When_Expression_Is_Already_In_Preheader()
        {
            var routine = new RoutineIr();
            var header = routine.CreateBlock();
            var body = routine.CreateBlock();
            var exit = routine.CreateBlock();
            var left = routine.CreateValue();
            var right = routine.CreateValue();
            var existingHome = Mock.Of<IVariable>();
            var stack = Mock.Of<IVariable>();
            var existing = routine.Append(routine.Entry, IrOpcode.Subtract, [left, right], payload:
                new IrLoweringOperation(_ => { }, existingHome));
            routine.Entry.Terminator = new IrTerminator.Jump(header);
            var duplicate = routine.Append(header, IrOpcode.Subtract, [left, right], payload:
                new IrLoweringOperation(_ => { }, stack, isStackResult: true, emitTo: (_, _) => { }));
            header.Terminator = new IrTerminator.Branch(routine.CreateValue(), body, exit);
            body.Terminator = new IrTerminator.Jump(header);
            exit.Terminator = new IrTerminator.Return(duplicate.Result);
            var allocations = 0;

            var optimizer = new RoutineIrOptimizer(acquireTemporary: () =>
            {
                allocations++;
                return Mock.Of<IVariable>();
            });
            optimizer.Optimize(routine);

            Assert.AreEqual(0, allocations);
            Assert.AreEqual(1, routine.Blocks.SelectMany(block => block.Instructions)
                .Count(instruction => instruction.Opcode == IrOpcode.Subtract));
            Assert.AreSame(existing.Result, ((IrTerminator.Return)exit.Terminator).Value);
            Assert.AreEqual(1, optimizer.GetStatistics().Single(stat =>
                stat.Name == "LICM deferred: already available").Count);
        }

        [TestMethod]
        public void Licm_Hoists_Unchanged_Table_Read_From_Loop_Header()
        {
            var routine = new RoutineIr();
            var header = routine.CreateBlock();
            var body = routine.CreateBlock();
            var exit = routine.CreateBlock();
            var table = routine.CreateValue();
            var index = routine.CreateValue();
            var home = Mock.Of<IVariable>();
            routine.Entry.Terminator = new IrTerminator.Jump(header);
            var invariant = routine.Append(header, IrOpcode.LoadByte, [table, index], IrEffect.ReadMemory,
                new IrLoweringOperation(_ => { }, home), readRegions: IrMemoryRegion.Tables);
            header.Terminator = new IrTerminator.Branch(routine.CreateValue(), body, exit);
            body.Terminator = new IrTerminator.Jump(header);
            exit.Terminator = new IrTerminator.Return(invariant.Result);

            new RoutineIrOptimizer().Optimize(routine);

            Assert.IsTrue(routine.Entry.Instructions.Contains(invariant));
            Assert.IsFalse(header.Instructions.Contains(invariant));
        }

        [TestMethod]
        public void Licm_Does_Not_Hoist_Table_Read_Across_Loop_Write()
        {
            var routine = new RoutineIr();
            var header = routine.CreateBlock();
            var body = routine.CreateBlock();
            var exit = routine.CreateBlock();
            var table = routine.CreateValue();
            var index = routine.CreateValue();
            var home = Mock.Of<IVariable>();
            routine.Entry.Terminator = new IrTerminator.Jump(header);
            var read = routine.Append(header, IrOpcode.LoadByte, [table, index], IrEffect.ReadMemory,
                new IrLoweringOperation(_ => { }, home), readRegions: IrMemoryRegion.Tables);
            header.Terminator = new IrTerminator.Branch(routine.CreateValue(), body, exit);
            routine.Append(body, IrOpcode.TargetOperation, [], IrEffect.WriteMemory, hasResult: false,
                writeRegions: IrMemoryRegion.Tables);
            body.Terminator = new IrTerminator.Jump(header);
            exit.Terminator = new IrTerminator.Return(read.Result);

            new RoutineIrOptimizer().Optimize(routine);

            Assert.IsTrue(header.Instructions.Contains(read));
            Assert.IsFalse(routine.Entry.Instructions.Contains(read));
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
        [DataRow((int)IrNumericSemantics.ZMachine16, (int)IrOpcode.ArithmeticShift, 0x8000, -1, -16384)]
        [DataRow((int)IrNumericSemantics.ZMachine16, (int)IrOpcode.LogicalShift, 0x8000, -1, 16384)]
        [DataRow((int)IrNumericSemantics.Glulx32, (int)IrOpcode.ArithmeticShift, int.MinValue, -1,
            -1073741824)]
        [DataRow((int)IrNumericSemantics.Glulx32, (int)IrOpcode.LogicalShift, int.MinValue, -1,
            1073741824)]
        [DataRow((int)IrNumericSemantics.ZMachine16, (int)IrOpcode.ArithmeticShift, 1, 15, -32768)]
        [DataRow((int)IrNumericSemantics.Glulx32, (int)IrOpcode.LogicalShift, 1, 31, int.MinValue)]
        public void ConstantFolding_Shifts_Use_Target_Semantics(
            int semantics,
            int opcode,
            int left,
            int right,
            int expected)
        {
            AssertFoldedBinary((IrNumericSemantics)semantics, (IrOpcode)opcode, left, right, expected);
        }

        [TestMethod]
        public void ConstantFolding_Declines_Invalid_Signed_Shift()
        {
            var routine = new RoutineIr();
            var instruction = routine.Append(routine.Entry, IrOpcode.ArithmeticShift,
                [routine.CreateConstant(1), routine.CreateConstant(16)]);
            routine.Entry.Terminator = new IrTerminator.Return(instruction.Result);

            new RoutineIrOptimizer(IrNumericSemantics.ZMachine16).Optimize(routine);

            Assert.IsTrue(routine.Entry.Instructions.Any(item => item.Opcode == IrOpcode.ArithmeticShift));
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
        public void IrRoutineBuilder_Sccp_Folds_Production_Logical_Shift()
        {
            var target = new Mock<IRoutineBuilder>();
            var local = Mock.Of<ILocalBuilder>();
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
            target.SetupGet(t => t.RoutineStart).Returns(Mock.Of<ILabel>());
            target.SetupGet(t => t.RTrue).Returns(Mock.Of<ILabel>());
            target.SetupGet(t => t.RFalse).Returns(Mock.Of<ILabel>());
            target.Setup(t => t.DefineLocal("TEMP")).Returns(local);

            var builder = new IrRoutineBuilder(target.Object, IrNumericSemantics.ZMachine16, true, MakeOperand);
            var temp = builder.DefineLocal("TEMP");
            builder.EmitBinary(BinaryOp.LogShift, MakeOperand(0x8000), MakeOperand(-1), temp);
            builder.Return(temp);
            builder.Finish();

            target.Verify(t => t.EmitBinary(It.IsAny<BinaryOp>(), It.IsAny<IOperand>(), It.IsAny<IOperand>(),
                It.IsAny<IVariable>()), Times.Never);
            target.Verify(t => t.Return(operands[16384]), Times.Once);
        }

        [TestMethod]
        public void IrRoutineBuilder_Reports_Unstructured_Binary_As_Opaque()
        {
            var target = new Mock<IRoutineBuilder>();
            var left = Mock.Of<IVariable>();
            var destination = Mock.Of<IVariable>();
            var one = new Mock<INumericOperand>();
            one.SetupGet(operand => operand.Value).Returns(1);
            target.SetupGet(t => t.RoutineStart).Returns(Mock.Of<ILabel>());
            target.SetupGet(t => t.RTrue).Returns(Mock.Of<ILabel>());
            target.SetupGet(t => t.RFalse).Returns(Mock.Of<ILabel>());
            var stats = new Dictionary<string, int>();

            var builder = new IrRoutineBuilder(target.Object, IrNumericSemantics.ZMachine16, true,
                recordOptimizationStats: values =>
                {
                    foreach (var stat in values)
                        stats[stat.Name] = stat.Count;
                });
            builder.EmitBinary(BinaryOp.Add, left, one.Object, destination);
            builder.Return(destination);
            builder.Finish();

            Assert.AreEqual(1, stats["Input opaque operations"]);
            Assert.AreEqual(1, stats["Recorded opaque: Binary.Add"]);
            target.Verify(t => t.EmitBinary(BinaryOp.Add, left, one.Object, destination), Times.Once);
            target.Verify(t => t.Return(destination), Times.Once);
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
        public void IrRoutineBuilder_Uses_Available_Local_For_Large_ZMachine_Constant()
        {
            var target = new Mock<IRoutineBuilder>();
            var first = Mock.Of<ILocalBuilder>();
            var second = Mock.Of<ILocalBuilder>();
            var large = new Mock<INumericOperand>();
            large.SetupGet(operand => operand.Value).Returns(500);
            target.SetupGet(t => t.RoutineStart).Returns(Mock.Of<ILabel>());
            target.SetupGet(t => t.RTrue).Returns(Mock.Of<ILabel>());
            target.SetupGet(t => t.RFalse).Returns(Mock.Of<ILabel>());
            target.Setup(t => t.DefineLocal("FIRST")).Returns(first);
            target.Setup(t => t.DefineLocal("SECOND")).Returns(second);

            var builder = new IrRoutineBuilder(target.Object, IrNumericSemantics.ZMachine16, true,
                preferConstantHome: operand => operand is not INumericOperand numeric || numeric.Value > 255);
            var firstLocal = builder.DefineLocal("FIRST");
            var secondLocal = builder.DefineLocal("SECOND");
            builder.EmitStore(firstLocal, large.Object);
            builder.EmitStore(secondLocal, firstLocal);
            builder.Finish();

            target.Verify(t => t.EmitStore(first, large.Object), Times.Once);
            target.Verify(t => t.EmitStore(second, first), Times.Once);
            target.Verify(t => t.EmitStore(second, large.Object), Times.Never);
        }

        [TestMethod]
        public void IrRoutineBuilder_Keeps_Small_ZMachine_Constant_Immediate()
        {
            var target = new Mock<IRoutineBuilder>();
            var first = Mock.Of<ILocalBuilder>();
            var second = Mock.Of<ILocalBuilder>();
            var small = new Mock<INumericOperand>();
            small.SetupGet(operand => operand.Value).Returns(50);
            target.SetupGet(t => t.RoutineStart).Returns(Mock.Of<ILabel>());
            target.SetupGet(t => t.RTrue).Returns(Mock.Of<ILabel>());
            target.SetupGet(t => t.RFalse).Returns(Mock.Of<ILabel>());
            target.Setup(t => t.DefineLocal("FIRST")).Returns(first);
            target.Setup(t => t.DefineLocal("SECOND")).Returns(second);

            var builder = new IrRoutineBuilder(target.Object, IrNumericSemantics.ZMachine16, true,
                preferConstantHome: operand => operand is not INumericOperand numeric || numeric.Value > 255);
            var firstLocal = builder.DefineLocal("FIRST");
            var secondLocal = builder.DefineLocal("SECOND");
            builder.EmitStore(firstLocal, small.Object);
            builder.EmitStore(secondLocal, firstLocal);
            builder.Finish();

            target.Verify(t => t.EmitStore(second, small.Object), Times.Once);
            target.Verify(t => t.EmitStore(second, first), Times.Never);
        }

        [TestMethod]
        public void IrRoutineBuilder_Uses_Available_Local_For_Symbolic_ZMachine_Constant()
        {
            var target = new Mock<IRoutineBuilder>();
            var first = Mock.Of<ILocalBuilder>();
            var second = Mock.Of<ILocalBuilder>();
            var symbolic = Mock.Of<IConstantOperand>();
            target.SetupGet(t => t.RoutineStart).Returns(Mock.Of<ILabel>());
            target.SetupGet(t => t.RTrue).Returns(Mock.Of<ILabel>());
            target.SetupGet(t => t.RFalse).Returns(Mock.Of<ILabel>());
            target.Setup(t => t.DefineLocal("FIRST")).Returns(first);
            target.Setup(t => t.DefineLocal("SECOND")).Returns(second);

            var builder = new IrRoutineBuilder(target.Object, IrNumericSemantics.ZMachine16, true,
                preferConstantHome: operand => operand is not INumericOperand numeric || numeric.Value > 255);
            var firstLocal = builder.DefineLocal("FIRST");
            var secondLocal = builder.DefineLocal("SECOND");
            builder.EmitStore(firstLocal, symbolic);
            builder.EmitStore(secondLocal, firstLocal);
            builder.Finish();

            target.Verify(t => t.EmitStore(first, symbolic), Times.Once);
            target.Verify(t => t.EmitStore(second, first), Times.Once);
            target.Verify(t => t.EmitStore(second, symbolic), Times.Never);
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
        public void IrRoutineBuilder_Preserves_Promoted_User_Local_Across_Call()
        {
            var target = new Mock<IRoutineBuilder>();
            var local = Mock.Of<ILocalBuilder>();
            var calledRoutine = Mock.Of<IOperand>();
            var constant = new Mock<INumericOperand>();
            constant.SetupGet(operand => operand.Value).Returns(42);
            target.SetupGet(t => t.RoutineStart).Returns(Mock.Of<ILabel>());
            target.SetupGet(t => t.RTrue).Returns(Mock.Of<ILabel>());
            target.SetupGet(t => t.RFalse).Returns(Mock.Of<ILabel>());
            target.Setup(t => t.DefineLocal("LOCAL")).Returns(local);

            var builder = new IrRoutineBuilder(target.Object, IrNumericSemantics.ZMachine16, true);
            var promoted = builder.DefineLocal("LOCAL");
            builder.EmitStore(promoted, constant.Object);
            builder.EmitCall(calledRoutine, [], null);
            builder.Return(promoted);
            builder.Finish();

            target.Verify(t => t.EmitStore(local, It.IsAny<IOperand>()), Times.Never);
            target.Verify(t => t.EmitCall(calledRoutine, It.IsAny<IOperand[]>(), null), Times.Once);
            target.Verify(t => t.Return(constant.Object), Times.Once);
        }

        [TestMethod]
        public void IrRoutineBuilder_Does_Not_Treat_Zero_Argument_Call_Result_As_Routine_Address()
        {
            var target = new Mock<IRoutineBuilder>();
            var first = Mock.Of<ILocalBuilder>();
            var second = Mock.Of<ILocalBuilder>();
            var calledRoutine = Mock.Of<IConstantOperand>();
            target.SetupGet(t => t.RoutineStart).Returns(Mock.Of<ILabel>());
            target.SetupGet(t => t.RTrue).Returns(Mock.Of<ILabel>());
            target.SetupGet(t => t.RFalse).Returns(Mock.Of<ILabel>());
            target.Setup(t => t.DefineLocal("FIRST")).Returns(first);
            target.Setup(t => t.DefineLocal("SECOND")).Returns(second);

            var builder = new IrRoutineBuilder(target.Object, IrNumericSemantics.ZMachine16, true,
                preferConstantHome: _ => true);
            var firstLocal = builder.DefineLocal("FIRST");
            var secondLocal = builder.DefineLocal("SECOND");
            builder.EmitCall(calledRoutine, [], firstLocal);
            builder.EmitCall(calledRoutine, [], secondLocal);
            builder.Finish();

            target.Verify(t => t.EmitCall(calledRoutine, It.IsAny<IOperand[]>(), first), Times.Once);
            target.Verify(t => t.EmitCall(calledRoutine, It.IsAny<IOperand[]>(), second), Times.Once);
        }

        [TestMethod]
        public void IrRoutineBuilder_Does_Not_Materialize_Dead_Local_Before_Global_Store()
        {
            var target = new Mock<IRoutineBuilder>();
            var local = Mock.Of<ILocalBuilder>();
            var global = Mock.Of<IVariable>();
            var constant = new Mock<INumericOperand>();
            constant.SetupGet(operand => operand.Value).Returns(9);
            target.SetupGet(t => t.RoutineStart).Returns(Mock.Of<ILabel>());
            target.SetupGet(t => t.RTrue).Returns(Mock.Of<ILabel>());
            target.SetupGet(t => t.RFalse).Returns(Mock.Of<ILabel>());
            target.Setup(t => t.DefineLocal("LOCAL")).Returns(local);

            var builder = new IrRoutineBuilder(target.Object, IrNumericSemantics.ZMachine16, true);
            var promoted = builder.DefineLocal("LOCAL");
            builder.EmitStore(promoted, constant.Object);
            builder.EmitStore(global, constant.Object);
            builder.Return(constant.Object);
            builder.Finish();

            target.Verify(t => t.EmitStore(local, It.IsAny<IOperand>()), Times.Never);
            target.Verify(t => t.EmitStore(global, constant.Object), Times.Once);
        }

        [TestMethod]
        public void IrRoutineBuilder_Reads_New_Global_Value_After_Store()
        {
            var target = new Mock<IRoutineBuilder>();
            var oldValue = Mock.Of<ILocalBuilder>();
            var newValue = Mock.Of<ILocalBuilder>();
            var global = Mock.Of<IVariable>();
            var calledRoutine = Mock.Of<IOperand>();
            target.SetupGet(t => t.RoutineStart).Returns(Mock.Of<ILabel>());
            target.SetupGet(t => t.RTrue).Returns(Mock.Of<ILabel>());
            target.SetupGet(t => t.RFalse).Returns(Mock.Of<ILabel>());
            target.Setup(t => t.DefineLocal("OLD")).Returns(oldValue);
            target.Setup(t => t.DefineLocal("NEW")).Returns(newValue);

            var builder = new IrRoutineBuilder(target.Object, IrNumericSemantics.ZMachine16, true);
            var oldLocal = builder.DefineLocal("OLD");
            var newLocal = builder.DefineLocal("NEW");
            builder.EmitStore(oldLocal, global);
            builder.EmitStore(global, newLocal);
            builder.EmitCall(calledRoutine, [oldLocal, global], null);
            builder.Finish();

            target.Verify(t => t.EmitStore(oldValue, global), Times.Once);
            target.Verify(t => t.EmitStore(global, newValue), Times.Once);
            target.Verify(t => t.EmitCall(calledRoutine,
                It.Is<IOperand[]>(operands => ReferenceEquals(operands[0], oldValue) &&
                    ReferenceEquals(operands[1], global)), null), Times.Once);
        }

        [TestMethod]
        public void IrRoutineBuilder_Reads_Global_Again_After_Unknown_Call()
        {
            var target = new Mock<IRoutineBuilder>();
            var oldValue = Mock.Of<ILocalBuilder>();
            var newValue = Mock.Of<ILocalBuilder>();
            var global = Mock.Of<IVariable>();
            var calledRoutine = Mock.Of<IOperand>();
            target.SetupGet(t => t.RoutineStart).Returns(Mock.Of<ILabel>());
            target.SetupGet(t => t.RTrue).Returns(Mock.Of<ILabel>());
            target.SetupGet(t => t.RFalse).Returns(Mock.Of<ILabel>());
            target.Setup(t => t.DefineLocal("OLD")).Returns(oldValue);
            target.Setup(t => t.DefineLocal("NEW")).Returns(newValue);

            var builder = new IrRoutineBuilder(target.Object, IrNumericSemantics.ZMachine16, true);
            var oldLocal = builder.DefineLocal("OLD");
            var newLocal = builder.DefineLocal("NEW");
            builder.EmitStore(oldLocal, global);
            builder.EmitCall(calledRoutine, [], null);
            builder.EmitStore(newLocal, global);
            builder.Finish();

            target.Verify(t => t.EmitStore(oldValue, global), Times.Once);
            target.Verify(t => t.EmitCall(calledRoutine, It.IsAny<IOperand[]>(), null), Times.Once);
            target.Verify(t => t.EmitStore(newValue, global), Times.Once);
            target.Verify(t => t.EmitStore(newValue, oldValue), Times.Never);
        }

        [TestMethod]
        public void IrRoutineBuilder_Forwards_Folded_Stack_Value_Into_Ordered_Operation()
        {
            var target = new Mock<IRoutineBuilder>();
            var stack = Mock.Of<IVariable>();
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
            target.SetupGet(t => t.Stack).Returns(stack);
            target.SetupGet(t => t.RoutineStart).Returns(Mock.Of<ILabel>());
            target.SetupGet(t => t.RTrue).Returns(Mock.Of<ILabel>());
            target.SetupGet(t => t.RFalse).Returns(Mock.Of<ILabel>());
            var emitted = new List<string>();
            target.Setup(t => t.EmitStore(stack, It.IsAny<IOperand>())).Callback(() => emitted.Add("push"));
            target.Setup(t => t.EmitPrint(PrintOp.Number, It.IsAny<IOperand>()))
                .Callback(() => emitted.Add("print"));

            var builder = new IrRoutineBuilder(target.Object, IrNumericSemantics.ZMachine16, true, MakeOperand);
            builder.EmitBinary(BinaryOp.Add, MakeOperand(60), MakeOperand(1), builder.Stack);
            builder.EmitPrint(PrintOp.Number, builder.Stack);
            builder.Finish();

            CollectionAssert.AreEqual(new[] { "print" }, emitted);
            target.Verify(t => t.EmitStore(stack, It.IsAny<IOperand>()), Times.Never);
            target.Verify(t => t.EmitPrint(PrintOp.Number, operands[61]), Times.Once);
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

        private static void AppendMaterialization(RoutineIr routine, IrBlock block, IVariable home, IrValue value)
        {
            var lowering = new IrLoweringOperation(_ => { }, home) { IsMaterialization = true };
            routine.Append(block, IrOpcode.TargetOperation, [value], IrEffect.Control, lowering, hasResult: false);
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
