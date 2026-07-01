using System;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Zilf.Common.StringEncoding;
using Zilf.Emit.Cornerstone;

namespace Zilf.Emit.Tests
{
    [TestClass, TestCategory("Compiler")]
    public class EmitCornerstoneTests
    {
        [TestMethod]
        public void Branches_Should_Emit_Expected_Jump_Mnemonics()
        {
            var streamFactory = new TestCornerstoneStreamFactory("branch-test.cas");

            using (var builder = new GameBuilder(streamFactory))
            {
                var routine = builder.DefineRoutine("CMP", entryPoint: false, cleanStack: false);
                var left = routine.DefineRequiredParameter("LEFT");
                var right = routine.DefineRequiredParameter("RIGHT");
                var label1 = routine.DefineLabel();
                var label2 = routine.DefineLabel();
                var label3 = routine.DefineLabel();
                var label4 = routine.DefineLabel();

                routine.Branch(Condition.Greater, left, right, label1, false);
                routine.MarkLabel(label1);
                routine.Branch(Condition.Less, left, right, label2, true);
                routine.MarkLabel(label2);
                routine.BranchIfZero(right, label3, false);
                routine.MarkLabel(label3);
                routine.BranchIfEqual(left, builder.MakeOperand(5), builder.MakeOperand(7), label4, false);
                routine.MarkLabel(label4);
                routine.Return(builder.One);

                builder.DefineRoutine("GO", entryPoint: true, cleanStack: false).EmitQuit();
            }

            var output = streamFactory.GetOutput();

            StringAssert.Contains(output, ".proc CMP");
            StringAssert.Contains(output, ".local LEFT");
            StringAssert.Contains(output, ".local RIGHT");
            StringAssert.Contains(output, "    JUMPGE loc_0001");
            StringAssert.Contains(output, "    JUMPG loc_0002");
            StringAssert.Contains(output, "    JUMPNZ loc_0003");
            StringAssert.Contains(output, "    JUMPEQ loc_0005");
            StringAssert.Contains(output, "    JUMPNE loc_0004");
        }

        [TestMethod]
        public void SpecialBranchTargets_Should_Emit_Return_Trampolines()
        {
            var streamFactory = new TestCornerstoneStreamFactory("special-branch-test.cas");

            using (var builder = new GameBuilder(streamFactory))
            {
                var routine = builder.DefineRoutine("PRED", entryPoint: false, cleanStack: false);
                var flag = routine.DefineRequiredParameter("FLAG");

                routine.BranchIfZero(flag, routine.RTrue, true);
                routine.Branch(routine.RFalse);

                builder.DefineRoutine("GO", entryPoint: true, cleanStack: false).EmitQuit();
            }

            var output = streamFactory.GetOutput();

            StringAssert.Contains(output, "    JUMPNZ loc_0001");
            StringAssert.Contains(output, "    PUSH1");
            StringAssert.Contains(output, "    RETURN");
            // RFalse uses PUSH0+RETURN (Z-machine false == 0) rather than RFALSE (Cornerstone FALSE == 0x8001)
            StringAssert.Contains(output, "    PUSH0");
        }

        [TestMethod]
        public void ComputedCalls_Should_Emit_Selector_After_Arguments()
        {
            var streamFactory = new TestCornerstoneStreamFactory("computed-call-test.cas");

            using (var builder = new GameBuilder(streamFactory))
            {
                builder.DefineRoutine("TARGET", entryPoint: false, cleanStack: false).Return(builder.One);

                var caller = builder.DefineRoutine("CALLER", entryPoint: false, cleanStack: false);
                var arg1 = caller.DefineRequiredParameter("ARG1");
                var arg2 = caller.DefineRequiredParameter("ARG2");
                var arg3 = caller.DefineRequiredParameter("ARG3");
                var arg4 = caller.DefineRequiredParameter("ARG4");
                var result = caller.DefineLocal("RESULT");

                caller.EmitCall((IOperand)builder.DefineRoutine("TARGET2", entryPoint: false, cleanStack: false), [arg1, arg2, arg3, arg4], result);
                caller.Return(result);

                builder.DefineRoutine("GO", entryPoint: true, cleanStack: false).EmitQuit();
            }

            var output = streamFactory.GetOutput();

            StringAssert.Contains(output, ".proc CALLER");
            StringAssert.Contains(output, ".local ARG1");
            StringAssert.Contains(output, ".local ARG2");
            StringAssert.Contains(output, ".local ARG3");
            StringAssert.Contains(output, ".local ARG4");
            StringAssert.Contains(output, ".local RESULT");
            StringAssert.Contains(output, ".local __CALL_SELECTOR_0000");
            StringAssert.Contains(output, "    PUSHL ARG1");
            StringAssert.Contains(output, "    PUSHL ARG2");
            StringAssert.Contains(output, "    PUSHL ARG3");
            StringAssert.Contains(output, "    PUSHL ARG4");
            StringAssert.Contains(output, "    PUSHW TARGET2");
            StringAssert.Contains(output, "    PUTL __CALL_SELECTOR_0000");
            StringAssert.Contains(output, "    PUSHL __CALL_SELECTOR_0000");
            StringAssert.Contains(output, "    JUMPZ loc_");
            StringAssert.Contains(output, "    CALLF 4");
        }

        [TestMethod]
        public void Verify_Should_Branch_As_Success()
        {
            var streamFactory = new TestCornerstoneStreamFactory("verify-test.cas");

            using (var builder = new GameBuilder(streamFactory))
            {
                var routine = builder.DefineRoutine("VERIFY", entryPoint: false, cleanStack: false);
                var ok = routine.DefineLabel();

                routine.Branch(Condition.Verify, null, null, ok, true);
                routine.Return(builder.Zero);
                routine.MarkLabel(ok);
                routine.Return(builder.One);

                builder.DefineRoutine("GO", entryPoint: true, cleanStack: false).EmitQuit();
            }

            var output = streamFactory.GetOutput();

            StringAssert.Contains(output, "    JUMP loc_0001");
        }

        [TestMethod]
        public void StackResidentComputedCalls_Should_Spill_Callee_And_Emit_Call()
        {
            var streamFactory = new TestCornerstoneStreamFactory("stack-call-test.cas");

            using (var builder = new GameBuilder(streamFactory))
            {
                var target = builder.DefineRoutine("TARGET", entryPoint: false, cleanStack: false);
                target.Return(builder.One);

                var caller = builder.DefineRoutine("CALLER", entryPoint: false, cleanStack: false);
                var value = caller.DefineRequiredParameter("VALUE");
                var result = caller.DefineLocal("RESULT");

                caller.EmitStore(caller.Stack, target);
                caller.EmitCall(caller.Stack, [value], result);
                caller.Return(result);

                builder.DefineRoutine("GO", entryPoint: true, cleanStack: false).EmitQuit();
            }

            var output = streamFactory.GetOutput();

            StringAssert.Contains(output, ".proc CALLER");
            StringAssert.Contains(output, ".local VALUE");
            StringAssert.Contains(output, ".local RESULT");
            StringAssert.Contains(output, ".local __CALL_TARGET_0000");
            StringAssert.Contains(output, ".local __CALL_SELECTOR_0001");
            StringAssert.Contains(output, "    PUSHW TARGET");
            StringAssert.Contains(output, "    PUTL __CALL_TARGET_0000");
            StringAssert.Contains(output, "    PUSHL VALUE");
            StringAssert.Contains(output, "    PUSHL __CALL_TARGET_0000");
            StringAssert.Contains(output, "    PUTL __CALL_SELECTOR_0001");
            StringAssert.Contains(output, "    PUSHL __CALL_SELECTOR_0001");
            StringAssert.Contains(output, "    CALLF 1");
        }

        [TestMethod]
        public void NonFinalStackCallArguments_Should_Be_Spilled_Before_Direct_Calls()
        {
            var streamFactory = new TestCornerstoneStreamFactory("stack-arg-call-test.cas");

            using (var builder = new GameBuilder(streamFactory))
            {
                var target = builder.DefineRoutine("TARGET", entryPoint: false, cleanStack: false);
                target.Return(builder.One);

                var caller = builder.DefineRoutine("CALLER", entryPoint: false, cleanStack: false);
                var value = caller.DefineRequiredParameter("VALUE");
                var result = caller.DefineLocal("RESULT");

                caller.EmitStore(caller.Stack, value);
                caller.EmitCall(target, [builder.Zero, caller.Stack, builder.One], result);
                caller.Return(result);

                builder.DefineRoutine("GO", entryPoint: true, cleanStack: false).EmitQuit();
            }

            var output = streamFactory.GetOutput();

            StringAssert.Contains(output, ".proc CALLER");
            StringAssert.Contains(output, ".local VALUE");
            StringAssert.Contains(output, ".local RESULT");
            StringAssert.Contains(output, ".local __CALL_ARG_0000");
            StringAssert.Contains(output, "    PUTL __CALL_ARG_0000");
            StringAssert.Contains(output, "    PUSH0");
            StringAssert.Contains(output, "    PUSHL __CALL_ARG_0000");
            StringAssert.Contains(output, "    PUSH1");
            StringAssert.Contains(output, "    CALL3 TARGET");
        }

        [TestMethod]
        public void NonFinalStackRuntimeCallArguments_Should_Be_Spilled()
        {
            var streamFactory = new TestCornerstoneStreamFactory("stack-arg-runtime-call-test.cas");

            using (var builder = new GameBuilder(streamFactory))
            {
                var table = builder.DefineTable("VALUES", pure: false);
                table.AddWord(10);
                table.AddWord(20);

                var caller = builder.DefineRoutine("CALLER", entryPoint: false, cleanStack: false);
                var result = caller.DefineLocal("RESULT");

                caller.EmitStore(caller.Stack, table);
                caller.EmitBinary(BinaryOp.GetWord, caller.Stack, builder.One, result);
                caller.Return(result);

                builder.DefineRoutine("GO", entryPoint: true, cleanStack: false).EmitQuit();
            }

            var output = streamFactory.GetOutput();

            StringAssert.Contains(output, ".proc CALLER");
            StringAssert.Contains(output, ".local RESULT");
            StringAssert.Contains(output, ".local __CALL_ARG_0000");
            StringAssert.Contains(output, "    PUTL __CALL_ARG_0000");
            StringAssert.Contains(output, "    PUSHL __CALL_ARG_0000");
            StringAssert.Contains(output, "    PUSH1");
            StringAssert.Contains(output, "    CALL2 __LoadWordAtBytePointer");
        }

        [TestMethod]
        public void FinalStackRuntimeCallArguments_Should_Be_Spilled()
        {
            var streamFactory = new TestCornerstoneStreamFactory("stack-final-runtime-call-test.cas");

            using (var builder = new GameBuilder(streamFactory))
            {
                var table = builder.DefineTable("VALUES", pure: false);
                table.AddWord(10);
                table.AddWord(20);

                var caller = builder.DefineRoutine("CALLER", entryPoint: false, cleanStack: false);
                var result = caller.DefineLocal("RESULT");

                caller.EmitStore(caller.Stack, builder.One);
                caller.EmitBinary(BinaryOp.GetWord, table, caller.Stack, result);
                caller.Return(result);

                builder.DefineRoutine("GO", entryPoint: true, cleanStack: false).EmitQuit();
            }

            var output = streamFactory.GetOutput();

            StringAssert.Contains(output, ".proc CALLER");
            StringAssert.Contains(output, ".local RESULT");
            StringAssert.Contains(output, ".local __CALL_ARG_0000");
            StringAssert.Contains(output, "    PUTL __CALL_ARG_0000");
            StringAssert.Contains(output, "    PUSHW byte:VALUES");
            StringAssert.Contains(output, "    PUSHL __CALL_ARG_0000");
            StringAssert.Contains(output, "    CALL2 __LoadWordAtBytePointer");
        }

        [TestMethod]
        public void EntryPointFinalStackRuntimeCallArguments_Should_Be_Spilled()
        {
            var streamFactory = new TestCornerstoneStreamFactory("entrypoint-stack-final-runtime-call-test.cas");

            using (var builder = new GameBuilder(streamFactory))
            {
                var table = builder.DefineTable("VALUES", pure: false);
                table.AddWord(10);
                table.AddWord(20);

                var entry = builder.DefineRoutine("GO", entryPoint: true, cleanStack: false);
                var result = entry.DefineLocal("RESULT");

                entry.EmitStore(entry.Stack, builder.One);
                entry.EmitBinary(BinaryOp.GetWord, table, entry.Stack, result);
                entry.EmitQuit();
            }

            var output = streamFactory.GetOutput();

            StringAssert.Contains(output, ".proc GO");
            StringAssert.Contains(output, ".local RESULT");
            StringAssert.Contains(output, ".local __CALL_ARG_0000");
            StringAssert.Contains(output, "    PUTL __CALL_ARG_0000");
            StringAssert.Contains(output, "    PUSHW byte:VALUES");
            StringAssert.Contains(output, "    PUSHL __CALL_ARG_0000");
            StringAssert.Contains(output, "    CALL2 __LoadWordAtBytePointer");
        }

        [TestMethod]
        public void ComputedCalls_Should_Return_Zero_When_Selector_Is_Zero()
        {
            var streamFactory = new TestCornerstoneStreamFactory("computed-call-zero-test.cas");

            using (var builder = new GameBuilder(streamFactory))
            {
                var caller = builder.DefineRoutine("CALLER", entryPoint: false, cleanStack: false);
                var value = caller.DefineRequiredParameter("VALUE");
                var result = caller.DefineLocal("RESULT");

                caller.EmitCall(builder.Zero, [value], result);
                caller.Return(result);

                builder.DefineRoutine("GO", entryPoint: true, cleanStack: false).EmitQuit();
            }

            var output = streamFactory.GetOutput();

            StringAssert.Contains(output, ".proc CALLER");
            StringAssert.Contains(output, ".local VALUE");
            StringAssert.Contains(output, ".local RESULT");
            StringAssert.Contains(output, "    PUSH0");
            StringAssert.Contains(output, "    PUTL RESULT");
            Assert.IsFalse(output.Contains("CALLF 1"));
        }

        [TestMethod]
        public void SingleModuleBuilds_Should_Export_Only_Entrypoints_And_FarSelectorRoutines()
        {
            var streamFactory = new TestCornerstoneStreamFactory("single-module-exports-test.cas");

            using (var builder = new GameBuilder(streamFactory))
            {
                var selected = builder.DefineRoutine("SELECTED", entryPoint: false, cleanStack: false);
                selected.Return(builder.One);

                var privateRoutine = builder.DefineRoutine("PRIVATE", entryPoint: false, cleanStack: false);
                privateRoutine.Return(builder.Zero);

                var table = builder.DefineTable("ROUTINES", pure: true);
                table.AddWord(selected);

                builder.DefineRoutine("GO", entryPoint: true, cleanStack: false).EmitQuit();
            }

            var output = streamFactory.GetOutput();

            StringAssert.Contains(output, ".module Main");
            StringAssert.Contains(output, ".export SELECTED, 0");
            StringAssert.Contains(output, ".export GO, 1");
            Assert.IsFalse(output.Contains(".export PRIVATE"));
        }

        [TestMethod]
        public void ProcedureLimit_Should_Split_Modules_And_Use_Far_Calls_Across_Modules()
        {
            var streamFactory = new TestCornerstoneStreamFactory("split-modules-test.cas");
            var options = new CornerstoneGameOptions { MaxProceduresPerModule = 2 };

            using (var builder = new GameBuilder(streamFactory, options))
            {
                var callee = builder.DefineRoutine("CALLEE", entryPoint: false, cleanStack: false);
                callee.Return(builder.One);

                builder.DefineRoutine("HELPER", entryPoint: false, cleanStack: false).Return(builder.Zero);

                var caller = builder.DefineRoutine("CALLER", entryPoint: true, cleanStack: false);
                caller.EmitCall(callee, [], null);
                caller.EmitQuit();
            }

            var output = streamFactory.GetOutput();

            StringAssert.Contains(output, ".module Main");
            StringAssert.Contains(output, ".module Main_2");
            StringAssert.Contains(output, ".export CALLEE, 0");
            StringAssert.Contains(output, ".export HELPER, 1");
            StringAssert.Contains(output, ".export CALLER, 0");
            StringAssert.Contains(output, "    CALLF0 CALLEE");
            StringAssert.Contains(output, ".entry Main_2.CALLER");
        }

        [TestMethod]
        public void LocalInitializers_Should_Emit_In_Local_Declarations()
        {
            var streamFactory = new TestCornerstoneStreamFactory("local-initializer-test.cas");

            using (var builder = new GameBuilder(streamFactory))
            {
                var routine = builder.DefineRoutine("INITED", entryPoint: false, cleanStack: false);
                var count = routine.DefineLocal("COUNT");
                count.DefaultValue = builder.MakeOperand(17);
                routine.Return(count);

                builder.DefineRoutine("GO", entryPoint: true, cleanStack: false).EmitQuit();
            }

            var output = streamFactory.GetOutput();

            StringAssert.Contains(output, ".proc INITED");
            StringAssert.Contains(output, ".local COUNT=0x0011");
            Assert.IsFalse(output.Contains(".init "));
        }

        [TestMethod]
        public void OptionalParameters_Should_Default_To_Zero_In_Local_Declarations()
        {
            var streamFactory = new TestCornerstoneStreamFactory("optional-parameter-init-test.cas");

            using (var builder = new GameBuilder(streamFactory))
            {
                var routine = builder.DefineRoutine("OPTIONAL", entryPoint: false, cleanStack: false);
                routine.DefineRequiredParameter("REQ");
                routine.DefineOptionalParameter("OPT");
                routine.Return(builder.Zero);

                builder.DefineRoutine("GO", entryPoint: true, cleanStack: false).EmitQuit();
            }

            var output = streamFactory.GetOutput();

            StringAssert.Contains(output, ".proc OPTIONAL");
            StringAssert.Contains(output, ".local REQ");
            StringAssert.Contains(output, ".local OPT=0x0000");
            Assert.IsFalse(output.Contains(".init "));
        }

        [TestMethod]
        public void LocalZeroInitializers_Should_Be_Emitted_In_Local_Declarations()
        {
            var streamFactory = new TestCornerstoneStreamFactory("local-zero-initializer-layout-test.cas");

            using (var builder = new GameBuilder(streamFactory))
            {
                var routine = builder.DefineRoutine("LOOPING", entryPoint: false, cleanStack: false);
                routine.DefineRequiredParameter("ARG");
                var count = routine.DefineLocal("COUNT");
                count.DefaultValue = builder.MakeOperand(5);
                var temp = routine.DefineLocal("TEMP");
                routine.Return(temp);

                builder.DefineRoutine("GO", entryPoint: true, cleanStack: false).EmitQuit();
            }

            var output = streamFactory.GetOutput();
            var procStart = output.IndexOf(".proc LOOPING", StringComparison.Ordinal);
            Assert.IsTrue(procStart >= 0, "Expected LOOPING procedure in output.");

            var procEnd = output.IndexOf(".endproc", procStart, StringComparison.Ordinal);
            Assert.IsTrue(procEnd > procStart, "Expected LOOPING procedure terminator in output.");

            var procText = output.Substring(procStart, procEnd - procStart);
            var countDeclIndex = procText.IndexOf(".local COUNT=0x0005", StringComparison.Ordinal);
            var tempDeclIndex = procText.IndexOf(".local TEMP=0x0000", StringComparison.Ordinal);
            var entryIndex = procText.IndexOf("loc_0000:", StringComparison.Ordinal);

            Assert.IsTrue(countDeclIndex >= 0, "Expected COUNT initializer in local declaration.");
            Assert.IsTrue(tempDeclIndex > countDeclIndex, "Expected TEMP zero initializer in local declaration.");
            Assert.IsTrue(entryIndex > tempDeclIndex, "Expected loc_0000 label after local declarations.");
            Assert.IsFalse(procText.Contains(".init "), "Did not expect separate .init directives.");
            Assert.IsFalse(procText.Contains("    PUTL TEMP"), "Did not expect synthesized zero-init code for TEMP.");
        }

        [TestMethod]
        public void StackResidentBitTests_Should_Spill_LeftOperand_And_Emit_Branch()
        {
            var streamFactory = new TestCornerstoneStreamFactory("stack-testbits-test.cas");

            using (var builder = new GameBuilder(streamFactory))
            {
                var routine = builder.DefineRoutine("CHECK", entryPoint: false, cleanStack: false);
                var flags = routine.DefineRequiredParameter("FLAGS");
                var matched = routine.DefineLabel();

                routine.EmitStore(routine.Stack, flags);
                routine.Branch(Condition.TestBits, routine.Stack, builder.MakeOperand(2), matched, true);
                routine.Return(builder.Zero);
                routine.MarkLabel(matched);
                routine.Return(builder.One);

                builder.DefineRoutine("GO", entryPoint: true, cleanStack: false).EmitQuit();
            }

            var output = streamFactory.GetOutput();

            StringAssert.Contains(output, ".proc CHECK");
            StringAssert.Contains(output, ".local FLAGS");
            StringAssert.Contains(output, ".local __TEST_BITS_LEFT_0000");
            StringAssert.Contains(output, "    PUSHL FLAGS");
            StringAssert.Contains(output, "    PUTL __TEST_BITS_LEFT_0000");
            StringAssert.Contains(output, "    PUSHL __TEST_BITS_LEFT_0000");
            StringAssert.Contains(output, "    PUSH2");
            StringAssert.Contains(output, "    AND");
            StringAssert.Contains(output, "    JUMPEQ loc_0001");
        }

        [TestMethod]
        public void DirectStreams_Should_Emit_Runtime_Helpers()
        {
            var streamFactory = new TestCornerstoneStreamFactory("stream-runtime-test.cas");

            using (var builder = new GameBuilder(streamFactory))
            {
                var routine = builder.DefineRoutine("STREAMS", entryPoint: false, cleanStack: false);
                var mode = routine.DefineRequiredParameter("MODE");

                routine.EmitUnary(UnaryOp.DirectInput, builder.MakeOperand(1), null);
                routine.EmitUnary(UnaryOp.DirectOutput, mode, null);
                routine.Return(builder.Zero);

                builder.DefineRoutine("GO", entryPoint: true, cleanStack: false).EmitQuit();
            }

            var output = streamFactory.GetOutput();

            StringAssert.Contains(output, ".proc STREAMS");
            StringAssert.Contains(output, ".local MODE");
            StringAssert.Contains(output, "    PUSH1");
            StringAssert.Contains(output, "    CALL1 __DirectInput");
            StringAssert.Contains(output, "    PUSH0");
            StringAssert.Contains(output, "    PUSHL MODE");
            StringAssert.Contains(output, "    CALL2 __DirectOutput");
            StringAssert.Contains(output, ".proc __DirectInput");
            StringAssert.Contains(output, ".proc __DirectOutput");
            StringAssert.Contains(output, "    OPEN 0x00");
            StringAssert.Contains(output, "    OPEN 0x01");

            var echoProcStart = output.IndexOf(".proc __EchoReadLineToCommandFile", StringComparison.Ordinal);
            Assert.IsTrue(echoProcStart >= 0, "Expected __EchoReadLineToCommandFile procedure in output.");

            var echoProcEnd = output.IndexOf(".endproc", echoProcStart, StringComparison.Ordinal);
            Assert.IsTrue(echoProcEnd > echoProcStart, "Expected __EchoReadLineToCommandFile terminator in output.");

            var echoProcText = output.Substring(echoProcStart, echoProcEnd - echoProcStart);
            var writeWordCount = echoProcText.Split("    PUSH1\n    WRITE", StringSplitOptions.None).Length - 1;
            Assert.AreEqual(3, writeWordCount, "Expected one-word writes for characters plus CR and LF terminators.");
            StringAssert.Contains(echoProcText, "echo_char_loop:");
            StringAssert.Contains(echoProcText, "    PUSH1\n    ADD\n    VLOADB");

            var transferBufferStart = output.IndexOf("__COMMAND_FILE_TRANSFER_BUFFER::", StringComparison.Ordinal);
            Assert.IsTrue(transferBufferStart >= 0, "Expected command file transfer buffer in output.");

            var transferBufferEnd = output.IndexOf("\n", transferBufferStart, StringComparison.Ordinal);
            Assert.IsTrue(transferBufferEnd > transferBufferStart, "Expected transfer buffer label line terminator.");

            var transferBufferText = output.Substring(transferBufferStart, Math.Min(output.Length - transferBufferStart, 80));
            StringAssert.Contains(transferBufferText, ".word 0x0000, 0x0000");
        }

        [TestMethod]
        public void PropertySize_Should_Emit_Runtime_Helper()
        {
            var streamFactory = new TestCornerstoneStreamFactory("property-size-test.cas");

            using (var builder = new GameBuilder(streamFactory))
            {
                var routine = builder.DefineRoutine("PSIZE", entryPoint: false, cleanStack: false);
                var propertyAddress = routine.DefineRequiredParameter("PROPERTY");
                var result = routine.DefineLocal("RESULT");

                routine.EmitUnary(UnaryOp.GetPropSize, propertyAddress, result);
                routine.Return(result);

                builder.DefineRoutine("GO", entryPoint: true, cleanStack: false).EmitQuit();
            }

            var output = streamFactory.GetOutput();

            StringAssert.Contains(output, ".proc __GetPropertySize");
            StringAssert.Contains(output, "    CALL1 __GetPropertySize");
            StringAssert.Contains(output, ".proc __LoadWordAtBytePointer");
            StringAssert.Contains(output, "    PUSH2");
            StringAssert.Contains(output, "    SUB");
            StringAssert.Contains(output, "    CALL2 __LoadWordAtBytePointer");
        }

        [TestMethod]
        public void PropertyHelpers_Should_Use_Word_Indexed_Record_Offsets()
        {
            var streamFactory = new TestCornerstoneStreamFactory("property-helper-offsets.cas");

            using (var builder = new GameBuilder(streamFactory))
            {
                var size = builder.DefineProperty("SIZE");
                var box = builder.DefineObject("BOX");
                box.AddWordProperty(size, builder.MakeOperand(7));

                var routine = builder.DefineRoutine("CHECK", entryPoint: false, cleanStack: false);
                var temp = routine.DefineLocal("TEMP");
                routine.EmitBinary(BinaryOp.GetProperty, box, size, temp);
                routine.Return(temp);

                builder.DefineRoutine("GO", entryPoint: true, cleanStack: false).EmitQuit();
            }

            var output = streamFactory.GetOutput();

            StringAssert.Contains(output, ".proc __GetPropertyAddress");
            StringAssert.Contains(output, "    PUSH2\n    ADD\n    PUSH2\n    MUL\n    RETURN");
            StringAssert.Contains(output, "    PUSH2\n    DIV");
        }

        [TestMethod]
        public void BitwiseNot_Should_Emit_Not_Opcode()
        {
            var streamFactory = new TestCornerstoneStreamFactory("not-test.cas");

            using (var builder = new GameBuilder(streamFactory))
            {
                var routine = builder.DefineRoutine("NOTTER", entryPoint: false, cleanStack: false);
                var value = routine.DefineRequiredParameter("VALUE");
                var result = routine.DefineLocal("RESULT");

                routine.EmitUnary(UnaryOp.Not, value, result);
                routine.Return(result);

                builder.DefineRoutine("GO", entryPoint: true, cleanStack: false).EmitQuit();
            }

            var output = streamFactory.GetOutput();

            StringAssert.Contains(output, "    PUSHL VALUE");
            StringAssert.Contains(output, "    NOT");
            StringAssert.Contains(output, "    PUTL RESULT");
        }

        [TestMethod]
        public void ObjectAndPropertyOps_Should_Emit_RuntimeHelpers_And_RamTables()
        {
            var streamFactory = new TestCornerstoneStreamFactory("object-property-test.cas");

            using (var builder = new GameBuilder(streamFactory))
            {
                var size = builder.DefineProperty("SIZE");
                size.DefaultValue = builder.MakeOperand(5);
                var open = builder.DefineFlag("OPEN");
                var room = builder.DefineObject("ROOM");
                var box = builder.DefineObject("BOX");
                room.Child = box;
                box.Parent = room;
                box.AddFlag(open);
                box.AddWordProperty(size, builder.MakeOperand(7));

                var routine = builder.DefineRoutine("CHECK", entryPoint: false, cleanStack: false);
                var temp = routine.DefineLocal("TEMP");
                var hasChild = routine.DefineLabel();
                var hasFlag = routine.DefineLabel();

                routine.EmitUnary(UnaryOp.GetParent, box, temp);
                ((IProvideNoValuePredEmit)routine).EmitGetChild(room, temp);
                routine.MarkLabel(hasChild);
                routine.Branch(Condition.TestAttr, box, open, hasFlag, true);
                routine.MarkLabel(hasFlag);
                routine.EmitBinary(BinaryOp.SetFlag, box, open, null);
                routine.EmitBinary(BinaryOp.ClearFlag, box, open, null);
                routine.EmitBinary(BinaryOp.GetProperty, box, size, temp);
                routine.EmitBinary(BinaryOp.GetPropAddress, box, size, temp);
                routine.EmitBinary(BinaryOp.GetNextProp, box, builder.Zero, temp);
                routine.EmitTernary(TernaryOp.PutProperty, box, size, builder.MakeOperand(9), null);
                routine.EmitBinary(BinaryOp.MoveObject, box, room, null);
                routine.EmitUnary(UnaryOp.RemoveObject, box, null);
                routine.Return(builder.One);

                builder.DefineRoutine("GO", entryPoint: true, cleanStack: false).EmitQuit();
            }

            var output = streamFactory.GetOutput();

            StringAssert.Contains(output, "__PROPERTY_DEFAULTS::");
            StringAssert.Contains(output, "__OBJECT_FLAGS_0002::");
            StringAssert.Contains(output, "__OBJECT_PROPERTIES_0002::");
            StringAssert.Contains(output, "__OBJECT_RECORDS::");
            StringAssert.Contains(output, ".proc __GetParent");
            StringAssert.Contains(output, ".proc __GetChild");
            StringAssert.Contains(output, ".proc __TestAttribute");
            StringAssert.Contains(output, ".proc __GetProperty");
            StringAssert.Contains(output, ".proc __SetFlag");
            StringAssert.Contains(output, ".proc __ClearFlag");
            StringAssert.Contains(output, ".proc __PutProperty");
            StringAssert.Contains(output, ".proc __LoadWordAtBytePointer");
            StringAssert.Contains(output, ".proc __StoreWordAtBytePointer");
            StringAssert.Contains(output, ".proc __RemoveObject");
            StringAssert.Contains(output, ".proc __MoveObject");
            StringAssert.Contains(output, "    CALL1 __GetParent");
            StringAssert.Contains(output, "    CALL1 __GetChild");
            StringAssert.Contains(output, "    CALL2 __TestAttribute");
            StringAssert.Contains(output, "    CALL2 __SetFlag");
            StringAssert.Contains(output, "    CALL2 __ClearFlag");
            StringAssert.Contains(output, "    CALL2 __GetProperty");
            StringAssert.Contains(output, "    CALL2 __GetPropertyAddress");
            StringAssert.Contains(output, "    CALL2 __GetNextProperty");
            StringAssert.Contains(output, "    CALL3 __PutProperty");
            StringAssert.Contains(output, "    CALL1 __RemoveObject");
            StringAssert.Contains(output, "    CALL2 __MoveObject");
        }

        [TestMethod]
        public void ByteSizedComplexProperties_Should_Emit_ByteLength_And_BytePayload()
        {
            var streamFactory = new TestCornerstoneStreamFactory("byte-property-test.cas");

            using (var builder = new GameBuilder(streamFactory))
            {
                var exits = builder.DefineProperty("EXITS");
                var room = builder.DefineObject("ROOM");
                var table = room.AddComplexProperty(exits);
                table.AddByte(1);
                table.AddByte(2);
                table.AddByte(3);

                builder.DefineRoutine("GO", entryPoint: true, cleanStack: false).EmitQuit();
            }

            var output = streamFactory.GetOutput();

            StringAssert.Contains(output, "__OBJECT_PROPERTIES_0001::");
            StringAssert.Contains(output, ".word 0x0001");
            StringAssert.Contains(output, ".word P?EXITS, 0x0003");
            StringAssert.Contains(output, ".byte 0x01, 0x02, 0x03");
        }

        [TestMethod]
        public void ByteTables_Should_Emit_Constant_Operands()
        {
            var streamFactory = new TestCornerstoneStreamFactory("byte-table-test.cas");

            using (var builder = new GameBuilder(streamFactory))
            {
                var flag = builder.DefineFlag("OPEN");
                var table = builder.DefineTable("BYTE_TABLE", pure: false);
                table.AddByte(flag);
                table.AddByte(0x7F);

                builder.DefineRoutine("GO", entryPoint: true, cleanStack: false).EmitQuit();
            }

            var output = streamFactory.GetOutput();

            StringAssert.Contains(output, "BYTE_TABLE::");
            StringAssert.Contains(output, ".byte OPEN, 0x7F");
        }

        [TestMethod]
        public void GlobalInitializers_Should_Resolve_Referenced_Global_Defaults()
        {
            var streamFactory = new TestCornerstoneStreamFactory("global-initializer-test.cas");

            using (var builder = new GameBuilder(streamFactory))
            {
                var target = builder.DefineGlobal("TARGET");
                target.DefaultValue = builder.MakeOperand(0x1234);

                var alias = builder.DefineGlobal("ALIAS");
                alias.DefaultValue = target;

                builder.DefineRoutine("GO", entryPoint: true, cleanStack: false).EmitQuit();
            }

            var output = streamFactory.GetOutput();

            StringAssert.Contains(output, ".global TARGET=0x1234");
            StringAssert.Contains(output, ".global ALIAS=0x1234");
        }

        [TestMethod]
        public void ConstantTables_Should_Emit_Global_Variable_Selectors()
        {
            var streamFactory = new TestCornerstoneStreamFactory("global-selector-test.cas");

            using (var builder = new GameBuilder(streamFactory))
            {
                var global = builder.DefineGlobal("TARGET");
                var table = builder.DefineTable("SELECTORS", pure: true);
                table.AddWord(global);

                builder.DefineRoutine("GO", entryPoint: true, cleanStack: false).EmitQuit();
            }

            var output = streamFactory.GetOutput();

            StringAssert.Contains(output, "SELECTORS::");
            StringAssert.Contains(output, ".word 0x001C");
        }

        [TestMethod]
        public void VocabularyWords_Should_Be_Emitted_As_Tables()
        {
            var streamFactory = new TestCornerstoneStreamFactory("vocab-word-test.cas");

            using (var builder = new GameBuilder(streamFactory))
            {
                var word = builder.DefineVocabularyWord("north");
                word.AddByte(0x12);
                word.AddWord(0x3456);

                builder.DefineRoutine("GO", entryPoint: true, cleanStack: false).EmitQuit();
            }

            var output = streamFactory.GetOutput();
            var prefix = EncodeVocabularyWord("north");

            StringAssert.Contains(output, "WORD_1::");
            StringAssert.Contains(output, ".word OBJSTR_0000");
            StringAssert.Contains(output, $".byte {FormatBytes(prefix.Skip(2).ToArray())}, 0x12");
            StringAssert.Contains(output, ".word 0x3456");
        }

        [TestMethod]
        public void AmbiguousDirectionPrepositionWords_Should_Emit_Direction_First()
        {
            var streamFactory = new TestCornerstoneStreamFactory("ambiguous-vocab-word-test.cas");

            using (var builder = new GameBuilder(streamFactory))
            {
                var word = builder.DefineVocabularyWord("in");
                word.AddByte(0x18);
                word.AddByte(0xFB);
                word.AddByte(0x0B);

                builder.DefineRoutine("GO", entryPoint: true, cleanStack: false).EmitQuit();
            }

            var output = streamFactory.GetOutput();
            var prefix = EncodeVocabularyWord("in");

            StringAssert.Contains(output, $".byte {FormatBytes(prefix.Skip(2).ToArray())}, 0x18, 0xFB, 0x0B");
        }

        [TestMethod]
        public void CompositeConstantOperands_Should_Emit_As_Pushw_Expressions()
        {
            var streamFactory = new TestCornerstoneStreamFactory("composite-operand-test.cas");

            using (var builder = new GameBuilder(streamFactory))
            {
                var table = builder.DefineTable("ROOMS", pure: true);
                table.AddWord(1);
                table.AddWord(2);

                var routine = builder.DefineRoutine("PICK", entryPoint: false, cleanStack: false);
                var temp = routine.DefineLocal("TEMP");
                routine.EmitStore(temp, table.Add(builder.MakeOperand(2)));
                routine.Return(temp);

                builder.DefineRoutine("GO", entryPoint: true, cleanStack: false).EmitQuit();
            }

            var output = streamFactory.GetOutput();

            StringAssert.Contains(output, "    PUSHW (byte:ROOMS + 0x0002)");
            StringAssert.Contains(output, "    PUTL TEMP");
        }

        [TestMethod]
        public void TableOps_Should_Emit_Vector_Loads_And_Stores()
        {
            var streamFactory = new TestCornerstoneStreamFactory("table-ops-test.cas");

            using (var builder = new GameBuilder(streamFactory))
            {
                var table = builder.DefineTable("VALUES", pure: false);
                table.AddWord(10);
                table.AddWord(20);

                var routine = builder.DefineRoutine("TABLEOPS", entryPoint: false, cleanStack: false);
                var wordResult = routine.DefineLocal("WORD");
                var byteResult = routine.DefineLocal("BYTE");
                var index = routine.DefineRequiredParameter("INDEX");

                routine.EmitBinary(BinaryOp.GetWord, table, builder.Zero, wordResult);
                routine.EmitBinary(BinaryOp.GetByte, table, index, byteResult);
                routine.EmitTernary(TernaryOp.PutWord, table, builder.Zero, builder.MakeOperand(99), null);
                routine.EmitTernary(TernaryOp.PutByte, table, index, builder.MakeOperand(7), null);
                routine.Return(wordResult);

                builder.DefineRoutine("GO", entryPoint: true, cleanStack: false).EmitQuit();
            }

            var output = streamFactory.GetOutput();

            StringAssert.Contains(output, "    PUSHW byte:VALUES");
            StringAssert.Contains(output, ".proc __LoadWordAtBytePointer");
            StringAssert.Contains(output, ".proc __LoadByteAtBytePointer");
            StringAssert.Contains(output, ".proc __StoreWordAtBytePointer");
            StringAssert.Contains(output, ".proc __StoreByteAtBytePointer");
            StringAssert.Contains(output, "    CALL2 __LoadWordAtBytePointer");
            StringAssert.Contains(output, "    CALL2 __LoadByteAtBytePointer");
            StringAssert.Contains(output, "    CALL3 __StoreWordAtBytePointer");
            StringAssert.Contains(output, "    CALL3 __StoreByteAtBytePointer");
        }

        [TestMethod]
        public void Random_Should_Emit_Runtime_Helper()
        {
            var streamFactory = new TestCornerstoneStreamFactory("random-test.cas");

            using (var builder = new GameBuilder(streamFactory))
            {
                var routine = builder.DefineRoutine("ROLL", entryPoint: false, cleanStack: false);
                var range = routine.DefineRequiredParameter("RANGE");
                var result = routine.DefineLocal("RESULT");

                routine.EmitUnary(UnaryOp.Random, range, result);
                routine.Return(result);

                builder.DefineRoutine("GO", entryPoint: true, cleanStack: false).EmitQuit();
            }

            var output = streamFactory.GetOutput();

            StringAssert.Contains(output, ".proc __Random");
            StringAssert.Contains(output, "    CALL1 __Random");
            StringAssert.Contains(output, "    PUSHW 0x6255");
            StringAssert.Contains(output, "    PUSHW 0x7FFF");
            StringAssert.Contains(output, "    AND");
            StringAssert.Contains(output, "    MOD");
        }


        [TestMethod]
        public void UnsupportedUnaryFallbacks_Should_NoOp_Or_Return_Zero()
        {
            var streamFactory = new TestCornerstoneStreamFactory("unary-fallback-test.cas");

            using (var builder = new GameBuilder(streamFactory))
            {
                var routine = builder.DefineRoutine("FALLBACKS", entryPoint: false, cleanStack: false);
                var value = routine.DefineRequiredParameter("VALUE");
                var result = routine.DefineLocal("RESULT");

                routine.EmitStore(routine.Stack, value);
                routine.EmitUnary(UnaryOp.OutputBuffer, routine.Stack, null);
                routine.EmitUnary(UnaryOp.SetFont, builder.MakeOperand(3), result);
                routine.Return(result);

                builder.DefineRoutine("GO", entryPoint: true, cleanStack: false).EmitQuit();
            }

            var output = streamFactory.GetOutput();

            StringAssert.Contains(output, "    PUSHL VALUE");
            StringAssert.Contains(output, "    POP");
            StringAssert.Contains(output, "    PUSH0");
            StringAssert.Contains(output, "    PUTL RESULT");
        }

        [TestMethod]
        public void ComputedIndirectLoads_Should_Use_Runtime_Helper_For_Global_Selectors()
        {
            var streamFactory = new TestCornerstoneStreamFactory("load-indirect-fallback-test.cas");

            using (var builder = new GameBuilder(streamFactory))
            {
                builder.DefineGlobal("FIRST_GLOBAL");
                builder.DefineGlobal("SECOND_GLOBAL");

                var routine = builder.DefineRoutine("LOADER", entryPoint: false, cleanStack: false);
                var selector = routine.DefineRequiredParameter("SELECTOR");
                var result = routine.DefineLocal("RESULT");

                routine.EmitStore(routine.Stack, selector);
                routine.EmitUnary(UnaryOp.LoadIndirect, routine.Stack, result);
                routine.Return(result);

                builder.DefineRoutine("GO", entryPoint: true, cleanStack: false).EmitQuit();
            }

            var output = streamFactory.GetOutput();

            StringAssert.Contains(output, "    PUSHL SELECTOR");
            StringAssert.Contains(output, "    CALL1 __LoadIndirectGlobal");
            StringAssert.Contains(output, "    PUTL RESULT");
            StringAssert.Contains(output, ".proc __LoadIndirectGlobal");
            StringAssert.Contains(output, "    PUSHW 0x0010");
            StringAssert.Contains(output, "    JUMPL load_global_missing");
            StringAssert.Contains(output, "    PUSHW 0x001D");
            StringAssert.Contains(output, "    JUMPG load_global_missing");
            StringAssert.Contains(output, "    LOADG FIRST_GLOBAL");
            StringAssert.Contains(output, "    LOADG SECOND_GLOBAL");
            Assert.IsFalse(output.Contains("    POP\n    PUSH0\n    PUTL RESULT", StringComparison.Ordinal));
        }

        [TestMethod]
        public void StringLiterals_Should_Emit_In_Obj_Trailer_Section()
        {
            var streamFactory = new TestCornerstoneStreamFactory("obj-string-data-test.cas");

            using (var builder = new GameBuilder(streamFactory))
            {
                var routine = builder.DefineRoutine("SHOW", entryPoint: false, cleanStack: false);
                routine.EmitPrint("hello", crlfRtrue: false);
                routine.Return(builder.Zero);

                builder.DefineRoutine("GO", entryPoint: true, cleanStack: false).EmitQuit();
            }

            var output = streamFactory.GetOutput();
            StringAssert.Contains(output, ".proc __PrintObjDataString");
            StringAssert.Contains(output, "    CALL1 __PrintObjDataString");
            StringAssert.Contains(output, ".objpacked OBJSTR_0000");
            StringAssert.Contains(output, "__TEXT_ALPHABET_0::");
        }

        [TestMethod]
        public void ObjectNameTable_Should_Store_Obj_Trailer_String_Descriptors()
        {
            var streamFactory = new TestCornerstoneStreamFactory("object-name-string-layout.cas");

            using (var builder = new GameBuilder(streamFactory))
            {
                var box = builder.DefineObject("BOX");
                box.DescriptiveName = "ornate box";

                builder.DefineRoutine("GO", entryPoint: true, cleanStack: false).EmitQuit();
            }

            var output = streamFactory.GetOutput();
            StringAssert.Contains(output, "__OBJECT_NAMES::");
            StringAssert.Contains(output, ".word OBJSTR_0000");
            StringAssert.Contains(output, ".objpacked OBJSTR_0000");
        }

        [TestMethod]
        public void AddressAndObjectPrints_Should_Emit_Supported_Cornerstone_Output()
        {
            var streamFactory = new TestCornerstoneStreamFactory("print-test.cas");

            using (var builder = new GameBuilder(streamFactory))
            {
                var box = builder.DefineObject("BOX");
                box.DescriptiveName = "ornate box";
                var message = builder.DefineVocabularyWord("hello");

                var routine = builder.DefineRoutine("SHOW", entryPoint: false, cleanStack: false);
                routine.EmitPrint(PrintOp.Address, message);
                routine.EmitPrint(PrintOp.Object, box);
                routine.Return(builder.Zero);

                builder.DefineRoutine("GO", entryPoint: true, cleanStack: false).EmitQuit();
            }

            var output = streamFactory.GetOutput();

            StringAssert.Contains(output, "__OBJECT_NAMES::");
            StringAssert.Contains(output, ".proc __PrintObject");
            StringAssert.Contains(output, ".proc __PrintObjDataString");
            StringAssert.Contains(output, ".proc __PrintVocabularyWord");
            StringAssert.Contains(output, "    PUSHL byteAddress\n    PUSHB 0x00\n    CALL2 __LoadWordAtBytePointer");
            StringAssert.Contains(output, "    UNPACK");
            StringAssert.Contains(output, "__TEXT_ALPHABET_0::");
            StringAssert.Contains(output, "restore_record_size_packed:\n    PUSHL savedRecordSize\n    PUTMG 0xCC");
            StringAssert.Contains(output, ".proc __BufferedPrintCharacter");
            StringAssert.Contains(output, "    CALL1 __PrintObject");
            StringAssert.Contains(output, "    CALL1 __PrintVocabularyWord");
            StringAssert.Contains(output, "    CALL1 __PrintObjDataString");
            StringAssert.Contains(output, "WORD_1::");
            StringAssert.Contains(output, ".word OBJSTR_");
            StringAssert.Contains(output, ".objpacked OBJSTR_");
            Assert.AreEqual(1, Regex.Matches(output, "\\bUNPACK\\b").Count);
        }

        [TestMethod]
        public void BufferedPrintCharacter_Should_Check_Available_Columns_Before_Wrapping()
        {
            var streamFactory = new TestCornerstoneStreamFactory("buffered-print-wrap-test.cas");

            using (var builder = new GameBuilder(streamFactory))
            {
                var routine = builder.DefineRoutine("PRINTZ", entryPoint: false, cleanStack: false);
                routine.EmitPrint("Z", crlfRtrue: false);
                routine.Return(builder.Zero);

                builder.DefineRoutine("GO", entryPoint: true, cleanStack: false).EmitQuit();
            }

            var output = streamFactory.GetOutput();

            Assert.IsTrue(
                Regex.IsMatch(
                    output,
                    "PUTL available\\n\\s+PUSHL available\\n\\s+PUSHL length\\n\\s+JUMPLE append_done"),
                output);
        }

        [TestMethod]
        public void PackedAddrPrint_Should_Emit_ObjData_Helper()
        {
            var streamFactory = new TestCornerstoneStreamFactory("print-packed-addr-test.cas");

            using (var builder = new GameBuilder(streamFactory))
            {
                var routine = builder.DefineRoutine("SHOW", entryPoint: false, cleanStack: false);
                routine.EmitPrint(PrintOp.PackedAddr, builder.MakeOperand(0x1234));
                routine.Return(builder.Zero);

                builder.DefineRoutine("GO", entryPoint: true, cleanStack: false).EmitQuit();
            }

            var output = streamFactory.GetOutput();

            StringAssert.Contains(output, ".proc __PrintObjDataString");
            StringAssert.Contains(output, "    PUSHW 0x1234\n    CALL1 __PrintObjDataString");
        }

        [TestMethod]
        public void ReadLine_Should_Emit_Tokenizer_And_Vocabulary_Tables()
        {
            var streamFactory = new TestCornerstoneStreamFactory("read-tokenize-test.cas");

            using (var builder = new GameBuilder(streamFactory))
            {
                builder.DefineVocabularyWord("look");
                builder.DefineVocabularyWord("quit");
                builder.SelfInsertingBreaks.Add('.');

                var charBuffer = builder.DefineGlobal("CHARBUF");
                var lexBuffer = builder.DefineGlobal("LEXBUF");
                var routine = builder.DefineRoutine("READCMD", entryPoint: false, cleanStack: false);
                routine.EmitRead(charBuffer, lexBuffer, null, null, null);
                routine.Return(builder.Zero);

                builder.DefineRoutine("GO", entryPoint: true, cleanStack: false).EmitQuit();
            }

            var output = streamFactory.GetOutput();

            StringAssert.Contains(output, "VOCAB_TABLE::");
            StringAssert.Contains(output, "__SI_BREAKS::");
            StringAssert.Contains(output, "__TOKEN_COMPARE_BUFFER::");
            StringAssert.Contains(output, ".proc __TokenizeLine");
            StringAssert.Contains(output, ".proc __LoadWordAtBytePointer");
            StringAssert.Contains(output, "    CALL2 __TokenizeLine");
            StringAssert.Contains(output, "    LOADVB2");
            StringAssert.Contains(output, "    PUTVB2");
            StringAssert.Contains(output, "    PUSH2\n    DIV\n    PUTL charBuffer");
            StringAssert.Contains(output, "    PUSH2\n    DIV\n    PUTL lexBuffer");
            StringAssert.Contains(output, "    PUSH1\n    LOADVB2\n    PUTL maxLength");
            StringAssert.Contains(output, "    PUSH2\n    PUSH0\n    PUTVB2");
            StringAssert.Contains(output, "    PUSH2\n    LOADVB2\n    PUTL textLength");
            StringAssert.Contains(output, "binary_search_vocabulary:");
            StringAssert.Contains(output, "token_compare_ready:");
            StringAssert.Contains(output, "search_lower_half:");
            StringAssert.Contains(output, "search_upper_half:");
            StringAssert.Contains(output, "    PUSHL vocabLow\n    PUSHL vocabHigh\n    ADD\n    PUSH2\n    DIV\n    PUTL vocabMid");
            StringAssert.Contains(output, "    PUSHW VOCAB_TABLE\n    PUSH1\n    ADD\n    PUSHL vocabMid\n    PUSH 5\n    MUL\n    ADD\n    PUTL entryPointer");
            StringAssert.Contains(output, "    PUSHL entryPointer\n    PUSHW __TOKEN_COMPARE_BUFFER\n    PUSHL entryChar\n    PUSH0\n    PUSHL compareLength\n    STRICMP");
            StringAssert.Contains(output, "    PUSH4\n    MUL\n    PUSH1\n    ADD\n    PUSHL wordLength\n    PUTVB2");
            StringAssert.Contains(output, "    PUSH4\n    MUL\n    PUSH2\n    ADD\n    PUSHL wordStart\n    PUSH2\n    ADD\n    PUTVB2");
            StringAssert.Contains(output, "    PUSH2\n    PUSHL wordCount\n    PUTVB2");
            StringAssert.Contains(output, "    PUSH1\n    PUSHL maxLength\n    PUTVB2");
            StringAssert.Contains(output, "    PUSH2\n    PUSHL length\n    PUTVB2");
            StringAssert.Contains(output, ".word 0x0002");
            StringAssert.Contains(output, "WORD_1::\n.word OBJSTR_0000\n.byte 0xC0, 0xA5");
            StringAssert.Contains(output, "__VOCAB_ENTRY_0001::");
            StringAssert.Contains(output, ".word byte:WORD_1");
            StringAssert.Contains(output, ".word 0x0004\n.byte 0x6C, 0x6F, 0x6F, 0x6B, 0x00, 0x00\n.word byte:WORD_1");
        }

        [TestMethod]
        public void VocabularyPointerTable_Should_Be_Sorted_By_Normalized_Word()
        {
            var streamFactory = new TestCornerstoneStreamFactory("sorted-vocab-table-test.cas");

            using (var builder = new GameBuilder(streamFactory))
            {
                builder.DefineVocabularyWord("zebra");
                builder.DefineVocabularyWord("apple");
                builder.DefineVocabularyWord("middle");

                builder.DefineRoutine("GO", entryPoint: true, cleanStack: false).EmitQuit();
            }

            var output = streamFactory.GetOutput();

            StringAssert.Contains(output, "VOCAB_TABLE::\n.word 0x0003");
            StringAssert.Contains(output, "__VOCAB_ENTRY_0001::\n.word 0x0005\n.byte 0x61, 0x70, 0x70, 0x6C, 0x65, 0x00\n.word byte:WORD_2");
            StringAssert.Contains(output, "__VOCAB_ENTRY_0002::\n.word 0x0006\n.byte 0x6D, 0x69, 0x64, 0x64, 0x6C, 0x65\n.word byte:WORD_3");
            StringAssert.Contains(output, "__VOCAB_ENTRY_0003::\n.word 0x0005\n.byte 0x7A, 0x65, 0x62, 0x72, 0x61, 0x00\n.word byte:WORD_1");
        }

        private static byte[] EncodeVocabularyWord(string word)
        {
            var encoder = new StringEncoder();
            return encoder.Encode(word.ToLowerInvariant(), 6, StringEncoderMode.NoAbbreviations);
        }

        private static string FormatBytes(byte[] bytes) => string.Join(", ", bytes.Select(value => $"0x{value:X2}"));

        [TestMethod]
        public void RestartSaveAndRestore_Should_Fall_Back_To_Failure_Semantics()
        {
            var streamFactory = new TestCornerstoneStreamFactory("persistence-test.cas");

            using (var builder = new GameBuilder(streamFactory))
            {
                var routine = builder.DefineRoutine("PERSIST", entryPoint: false, cleanStack: false);
                var saveResult = routine.DefineLocal("SAVE_RESULT");
                var restoreResult = routine.DefineLocal("RESTORE_RESULT");
                var failed = routine.DefineLabel();

                routine.EmitRestart();
                routine.EmitSave(saveResult);
                routine.EmitRestore(restoreResult);
                routine.EmitSave(failed, polarity: false);
                routine.Return(builder.Zero);
                routine.MarkLabel(failed);
                routine.Return(builder.One);

                builder.DefineRoutine("GO", entryPoint: true, cleanStack: false).EmitQuit();
            }

            var output = streamFactory.GetOutput();

            StringAssert.Contains(output, ".proc PERSIST");
            StringAssert.Contains(output, ".local SAVE_RESULT");
            StringAssert.Contains(output, ".local RESTORE_RESULT");
            StringAssert.Contains(output, "    PUSH0");
            StringAssert.Contains(output, "    PUTL SAVE_RESULT");
            StringAssert.Contains(output, "    PUTL RESTORE_RESULT");
            StringAssert.Contains(output, "    JUMP loc_0001");
        }

        [TestMethod]
        public void LowCoreInterface_Should_Emit_Cornerstone_Header_Compatibility_Helpers()
        {
            var streamFactory = new TestCornerstoneStreamFactory("lowcore-interface-test.cas");
            var options = new CornerstoneGameOptions { TimeStatusLine = true, ZMachineVersion = 3 };

            using (var builder = new GameBuilder(streamFactory, options))
            {
                var routine = builder.DefineRoutine("LOWCORE", entryPoint: false, cleanStack: false);
                var result = routine.DefineLocal("RESULT");
                var emulator = (IProvideLowCoreEmulation)routine;

                Assert.IsTrue(emulator.TryEmitLowCoreRead("FLAGS", result));
                Assert.IsTrue(emulator.TryEmitLowCoreRead("ZVERSION", result));
                Assert.IsTrue(emulator.TryEmitLowCoreRead("SCRV", result));
                Assert.IsTrue(emulator.TryEmitLowCoreGetTable("SERIAL", result));
                Assert.IsTrue(emulator.TryEmitLowCoreWrite("FLAGS", builder.One));

                routine.Return(result);
                builder.DefineRoutine("GO", entryPoint: true, cleanStack: false).EmitQuit();
            }

            var output = streamFactory.GetOutput();

            StringAssert.Contains(output, ".proc __GetLowCoreFlags");
            StringAssert.Contains(output, ".proc __SetLowCoreFlags");
            StringAssert.Contains(output, ".proc __GetScreenHeight");
            StringAssert.Contains(output, "    CALL0 __GetLowCoreFlags");
            StringAssert.Contains(output, "    CALL0 __GetScreenHeight");
            StringAssert.Contains(output, "    PUSHB 0x12");
            StringAssert.Contains(output, "    PUSHB 0x17");
            StringAssert.Contains(output, "    CALL1 __SetLowCoreFlags");
        }

        [TestMethod]
        public void LowCoreRuntimeTraps_Should_Route_Header_Loads_And_Stores_Through_Runtime_Helpers()
        {
            var streamFactory = new TestCornerstoneStreamFactory("lowcore-runtime-trap-test.cas");

            using (var builder = new GameBuilder(streamFactory))
            {
                var routine = builder.DefineRoutine("HEADERS", entryPoint: false, cleanStack: false);
                var result = routine.DefineLocal("RESULT");

                routine.EmitBinary(BinaryOp.GetByte, builder.Zero, builder.One, result);
                routine.EmitBinary(BinaryOp.GetWord, builder.Zero, builder.MakeOperand(8), result);
                routine.EmitTernary(TernaryOp.PutByte, builder.Zero, builder.MakeOperand(17), builder.One, null);
                routine.EmitTernary(TernaryOp.PutWord, builder.Zero, builder.MakeOperand(8), builder.One, null);
                routine.Return(result);

                builder.DefineRoutine("GO", entryPoint: true, cleanStack: false).EmitQuit();
            }

            var output = streamFactory.GetOutput();

            StringAssert.Contains(output, ".proc __GetHeaderByte");
            StringAssert.Contains(output, ".proc __GetHeaderWord");
            StringAssert.Contains(output, ".proc __PutHeaderByte");
            StringAssert.Contains(output, ".proc __PutHeaderWord");
            StringAssert.Contains(output, "    CALL1 __GetHeaderByte");
            StringAssert.Contains(output, "    CALL1 __GetHeaderWord");
            StringAssert.Contains(output, "    CALL2 __PutHeaderByte");
            StringAssert.Contains(output, "    CALL2 __PutHeaderWord");
        }

        private sealed class TestCornerstoneStreamFactory(string fileName) : ICornerstoneStreamFactory
        {
            private readonly MemoryStream stream = new();

            public Stream CreateMainStream()
            {
                stream.SetLength(0);
                stream.Position = 0;
                return stream;
            }

            public string GetMainFileName(bool withExt) => withExt ? fileName : Path.GetFileNameWithoutExtension(fileName);

            public string GetOutput() => Encoding.UTF8.GetString(stream.ToArray()).Replace("\r\n", "\n");
        }
    }
}