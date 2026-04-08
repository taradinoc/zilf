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
using System.Globalization;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text.RegularExpressions;

namespace Zilf.Emit.Cornerstone
{
    public sealed partial class GameBuilder
    {
        internal sealed partial class RoutineBuilder : IRoutineBuilder, global::Zilf.Emit.IProvideLowCoreEmulation
        {
            private static readonly Regex NumberedLocalOperandRegex = GetNumberedLocalOperandRegex();
            private static readonly Regex NumberedGlobalOperandRegex = GetNumberedGlobalOperandRegex();
            private readonly GameBuilder owner;
            private readonly List<NamedVariable> locals = [];
            private readonly List<string> lines = [];
            private readonly List<DirectRoutineCallSite> directCallSites = [];
            private readonly NamedVariable stackVariable = new("STACK", VariableKind.Stack, -1);

            private int nextLabelOrdinal;
            private int nextGeneratedLocalOrdinal;
            private int parameterCount;

            public RoutineBuilder(GameBuilder owner, string name, bool entryPoint, bool cleanStack)
            {
                this.owner = owner;
                Name = name;
                EntryPoint = entryPoint;
                CleanStack = cleanStack;

                if (entryPoint)
                    EmitConsoleStartup();
            }

            public string Name { get; }

            internal string AssignedModuleName { get; set; } = BaseModuleName;

            public bool EntryPoint { get; }

            public bool RequiresFarSelector { get; private set; }

            public void MarkRequiresFarSelector()
            {
                RequiresFarSelector = true;
            }

            public bool CleanStack { get; }

            public ILabel RTrue { get; } = new Label(-1);

            public ILabel RFalse { get; } = new Label(-2);

            public IVariable Stack => stackVariable;

            public bool UsesStackBasedCalls => true;

            public ILabel RoutineStart { get; } = new Label(0);

            public bool HasArgCount => false;

            public bool HasBranchSave => true;

            public bool HasStoreSave => true;

            public bool HasExtendedSave => true;

            public bool HasUndo => false;

            public ILocalBuilder DefineRequiredParameter(string name)
            {
                parameterCount++;
                return AddLocal(name);
            }

            public ILocalBuilder DefineOptionalParameter(string paramName)
            {
                parameterCount++;
                var local = AddLocal(paramName);
                local.DefaultValue = owner.Zero;
                return local;
            }

            public ILocalBuilder DefineLocal(string localName)
            {
                return AddLocal(localName);
            }

            public ILabel DefineLabel() => new Label(++nextLabelOrdinal);

            public void MarkLabel(ILabel label)
            {
                if (label is not Label concrete)
                    return;

                switch (concrete.Ordinal)
                {
                    case -1:
                        EmitReturnTrue();
                        return;

                    case -2:
                        lines.Add("    RFALSE");
                        return;

                    default:
                        lines.Add($"{FormatLabelName(concrete)}:");
                        return;
                }
            }

            public void Branch(ILabel label)
            {
                if (TryEmitSpecialBranchTarget(label))
                    return;

                lines.Add($"    JUMP {FormatBranchTarget(label)}");
            }

            public void Branch(Condition cond, IOperand? left, IOperand? right, ILabel label, bool polarity)
            {
                switch (cond)
                {
                    case Condition.Greater when left != null && right != null:
                        EmitOrderedComparison(
                            left,
                            right,
                            polarity ? "JUMPL" : "JUMPGE",
                            polarity ? "JUMPG" : "JUMPLE",
                            label);
                        return;

                    case Condition.Less when left != null && right != null:
                        EmitOrderedComparison(
                            left,
                            right,
                            polarity ? "JUMPG" : "JUMPLE",
                            polarity ? "JUMPL" : "JUMPGE",
                            label);
                        return;

                    case Condition.TestBits when left != null && right != null:
                        EmitTestBits(left, right, label, polarity);
                        return;

                    case Condition.IncCheck when left is NamedVariable incVariable && right != null:
                        // IncCheck tests "variable > threshold" → same comparison direction as Greater
                        EmitAdjustAndCompare(incVariable, right, add: true, label, polarity ? "JUMPL" : "JUMPGE");
                        return;

                    case Condition.DecCheck when left is NamedVariable decVariable && right != null:
                        // DecCheck tests "variable < threshold" → same comparison direction as Less
                        EmitAdjustAndCompare(decVariable, right, add: false, label, polarity ? "JUMPG" : "JUMPLE");
                        return;

                    case Condition.Inside when left != null && right != null:
                        EmitRuntimeCall(RuntimeLib.GetParent, [left], Stack);
                        BranchIfEqual(Stack, right, label, polarity);
                        return;

                    case Condition.TestAttr when left != null && right != null:
                        EmitRuntimeCall(RuntimeLib.TestAttribute, [left, right], Stack);
                        BranchIfZero(Stack, label, !polarity);
                        return;

                    case Condition.Verify:
                        if (polarity)
                            Branch(label);
                        return;

                    default:
                        ThrowNotSupported();
                        return;
                }
            }

            public void BranchIfZero(IOperand operand, ILabel label, bool polarity)
            {
                if (!IsStackOperand(operand))
                    EmitPushOperand(operand);

                EmitConditionalJump(polarity ? "JUMPZ" : "JUMPNZ", label);
            }

            public void BranchIfEqual(IOperand value, IOperand option1, ILabel label, bool polarity) =>
                EmitBranchIfEqual(value, [option1], label, polarity);

            public void BranchIfEqual(IOperand value, IOperand option1, IOperand option2, ILabel label, bool polarity) =>
                EmitBranchIfEqual(value, [option1, option2], label, polarity);

            public void BranchIfEqual(IOperand value, IOperand option1, IOperand option2, IOperand option3, ILabel label, bool polarity) =>
                EmitBranchIfEqual(value, [option1, option2, option3], label, polarity);

            public void Return(IOperand result)
            {
                if (!ReferenceEquals(result, Stack))
                    EmitPushOperand(result);

                lines.Add("    RETURN");
            }

            public void EmitRestart()
            {
            }

            public void EmitQuit()
            {
                // HALT 0x0000 keeps the last printed content on the screen and exits with a message
                // whereas HALT 0x0001 clears the screen and exits silently
                lines.Add("    HALT 0x0000");
            }

            public void EmitSave(ILabel label, bool polarity)
            {
                EmitPersistenceBranchFailure(label, polarity);
            }

            public void EmitRestore(ILabel label, bool polarity)
            {
                EmitPersistenceBranchFailure(label, polarity);
            }

            public void EmitSave(IVariable result)
            {
                EmitStore(result, owner.Zero);
            }

            public void EmitRestore(IVariable result)
            {
                EmitStore(result, owner.Zero);
            }

            public void EmitSave(IOperand table, IOperand size, IOperand name, IOperand? prompt, IVariable result)
            {
                EmitStore(result, owner.Zero);
            }

            public void EmitRestore(IOperand table, IOperand size, IOperand name, IOperand? prompt, IVariable result)
            {
                EmitStore(result, owner.Zero);
            }

            public void EmitScanTable(IOperand value, IOperand table, IOperand length, IOperand? form, IVariable result, ILabel label, bool polarity) => ThrowNotSupported();

            public void EmitGetChild(IOperand value, IVariable result, ILabel label, bool polarity)
            {
                EmitRuntimeCall(RuntimeLib.GetChild, [value], result);
                BranchIfZero(result, label, !polarity);
            }

            public void EmitGetSibling(IOperand value, IVariable result, ILabel label, bool polarity)
            {
                EmitRuntimeCall(RuntimeLib.GetSibling, [value], result);
                BranchIfZero(result, label, !polarity);
            }

            public void EmitNullary(NullaryOp op, IVariable? result)
            {
                switch (op)
                {
                    case NullaryOp.ShowStatus:
                        EmitRuntimeCall(RuntimeLib.DrawStatusLine, [], null);
                        if (result != null)
                            EmitStore(result, owner.Zero);

                        return;

                    default:
                        ThrowNotSupported();
                        return;
                }
            }

            public void EmitUnary(UnaryOp op, IOperand value, IVariable? result)
            {
                switch (op)
                {
                    case UnaryOp.LoadIndirect when value is IIndirectOperand indirect:
                        if (indirect.Variable is not NamedVariable resolvedIndirectVariable)
                        {
                            ThrowNotSupported();
                            return;
                        }

                        if (resolvedIndirectVariable.Kind == VariableKind.Stack)
                            lines.Add("    DUP");
                        else
                            EmitPushOperand(resolvedIndirectVariable);

                        ConsumeStackResult(result, discardIfUnused: true);
                        return;

                    case UnaryOp.LoadIndirect:
                        EmitRuntimeCall(RuntimeLib.LoadIndirectGlobal, [value], result);
                        return;

                    case UnaryOp.GetParent:
                        EmitRuntimeCall(RuntimeLib.GetParent, [value], result);
                        return;

                    case UnaryOp.GetPropSize:
                        EmitRuntimeCall(RuntimeLib.GetPropertySize, [value], result);
                        return;

                    case UnaryOp.RemoveObject:
                        EmitRuntimeCall(RuntimeLib.RemoveObject, [value], null);
                        return;

                    case UnaryOp.Random:
                        EmitRuntimeCall(RuntimeLib.Random, [value], result);
                        return;

                    case UnaryOp.DirectInput:
                        EmitRuntimeCall(RuntimeLib.DirectInput, [value], null);
                        return;

                    case UnaryOp.DirectOutput:
                        EmitRuntimeCall(RuntimeLib.DirectOutput, [value], null);
                        return;

                    case UnaryOp.OutputBuffer:
                    case UnaryOp.OutputStyle:
                    case UnaryOp.SplitWindow:
                    case UnaryOp.SelectWindow:
                    case UnaryOp.ClearWindow:
                    case UnaryOp.EraseLine:
                    case UnaryOp.GetCursor:
                    case UnaryOp.PictureTable:
                    case UnaryOp.MouseWindow:
                    case UnaryOp.ReadMouse:
                    case UnaryOp.PrintForm:
                    case UnaryOp.BufferScreen:
                        DiscardUnusedStackOperand(value);
                        return;

                    case UnaryOp.SetFont:
                    case UnaryOp.CheckUnicode:
                        DiscardUnusedStackOperand(value);
                        if (result != null)
                            EmitStore(result, owner.Zero);
                        return;
                }

                EmitPushOperand(value);

                switch (op)
                {
                    case UnaryOp.Not:
                        lines.Add("    NOT");
                        ConsumeStackResult(result, discardIfUnused: true);
                        return;

                    case UnaryOp.Neg:
                        lines.Add("    NEG");
                        ConsumeStackResult(result, discardIfUnused: true);
                        return;

                    default:
                        ThrowNotSupported();
                        return;
                }
            }

            public void EmitBinary(BinaryOp op, IOperand left, IOperand right, IVariable? result)
            {
                switch (op)
                {
                    case BinaryOp.GetWord:
                        EmitRuntimeCall(RuntimeLib.LoadWordAtBytePointer, [left, right], result);
                        return;

                    case BinaryOp.GetByte:
                        EmitRuntimeCall(RuntimeLib.LoadByteAtBytePointer, [left, right], result);
                        return;

                    case BinaryOp.MoveObject:
                        EmitRuntimeCall(RuntimeLib.MoveObject, [left, right], null);
                        return;

                    case BinaryOp.GetPropAddress:
                        EmitRuntimeCall(RuntimeLib.GetPropertyAddress, [left, right], result);
                        return;

                    case BinaryOp.GetProperty:
                        EmitRuntimeCall(RuntimeLib.GetProperty, [left, right], result);
                        return;

                    case BinaryOp.GetNextProp:
                        EmitRuntimeCall(RuntimeLib.GetNextProperty, [left, right], result);
                        return;

                    case BinaryOp.SetFlag:
                        EmitRuntimeCall(RuntimeLib.SetFlag, [left, right], null);
                        return;

                    case BinaryOp.ClearFlag:
                        EmitRuntimeCall(RuntimeLib.ClearFlag, [left, right], null);
                        return;
                }

                EmitPushOperand(left);
                EmitPushOperand(right);

                switch (op)
                {
                    case BinaryOp.Add:
                        lines.Add("    ADD");
                        break;

                    case BinaryOp.Sub:
                        lines.Add("    SUB");
                        break;

                    case BinaryOp.Mul:
                        lines.Add("    MUL");
                        break;

                    case BinaryOp.Div:
                        lines.Add("    DIV");
                        break;

                    case BinaryOp.Mod:
                        lines.Add("    MOD");
                        break;

                    case BinaryOp.And:
                        lines.Add("    AND");
                        break;

                    case BinaryOp.Or:
                        lines.Add("    OR");
                        break;

                    case BinaryOp.ArtShift:
                        lines.Add("    ASHIFT");
                        break;

                    case BinaryOp.LogShift:
                        lines.Add("    SHIFT");
                        break;

                    default:
                        ThrowNotSupported();
                        return;
                }

                ConsumeStackResult(result, discardIfUnused: true);
            }

            public void EmitTernary(TernaryOp op, IOperand left, IOperand center, IOperand right, IVariable? result)
            {
                switch (op)
                {
                    case TernaryOp.PutWord:
                        EmitRuntimeCall(RuntimeLib.StoreWordAtBytePointer, [left, center, right], null);
                        return;

                    case TernaryOp.PutByte:
                        EmitRuntimeCall(RuntimeLib.StoreByteAtBytePointer, [left, center, right], null);
                        return;

                    case TernaryOp.PutProperty:
                        EmitRuntimeCall(RuntimeLib.PutProperty, [left, center, right], null);
                        return;

                    default:
                        ThrowNotSupported();
                        return;
                }
            }

            public void EmitPrint(string text, bool crlfRtrue)
            {
                var objString = owner.RegisterObjString(text);
                EmitRuntimeCall(RuntimeLib.PrintObjDataString, [objString.Location], null);

                if (crlfRtrue)
                {
                    EmitPrintNewLine();
                    lines.Add("    PUSH1");
                    lines.Add("    RETURN");
                }
            }

            public void EmitPrint(PrintOp op, IOperand value)
            {
                switch (op)
                {
                    case PrintOp.Address:
                            EmitRuntimeCall(RuntimeLib.PrintVocabularyWord, [value], null);
                            return;

                    case PrintOp.PackedAddr:
                        EmitRuntimeCall(RuntimeLib.PrintObjDataString, [value], null);
                        return;

                    case PrintOp.Object:
                        EmitRuntimeCall(RuntimeLib.PrintObject, [value], null);
                        return;

                    case PrintOp.Character:
                        EmitRuntimeCall(RuntimeLib.BufferedPrintCharacter, [value], null);
                        return;

                    case PrintOp.Number:
                        EmitRuntimeCall(RuntimeLib.PrintNumber, [value], null);
                        return;

                    default:
                        ThrowNotSupported();
                        return;
                }
            }

            public void EmitPrintTable(IOperand table, IOperand width, IOperand? height, IOperand? skip) => ThrowNotSupported();

            public void EmitPrintNewLine()
            {
                EmitRuntimeCall(RuntimeLib.FlushOutputBuffer, [], null);
                EmitRuntimeCall(RuntimeLib.ConsoleAdvanceLine, [], null);
            }

            public void EmitRead(IOperand chrbuf, IOperand? lexbuf, IOperand? interval, IOperand? routine, IVariable? result)
            {
                // Timed input callbacks are not yet implemented for the Cornerstone backend.
                if (interval != null || routine != null)
                    ThrowNotSupported();

                EmitRuntimeCall(RuntimeLib.ReadLine, [chrbuf, lexbuf ?? owner.Zero], result);
            }

            public void EmitReadChar(IOperand? interval, IOperand? routine, IVariable result) => ThrowNotSupported();

            public void EmitPlaySound(IOperand number, IOperand? effect, IOperand? volume, IOperand? routine) => ThrowNotSupported();

            public void EmitEncodeText(IOperand src, IOperand length, IOperand srcOffset, IOperand dest) => ThrowNotSupported();

            public void EmitTokenize(IOperand text, IOperand parse, IOperand? dictionary, IOperand? flag) => ThrowNotSupported();

            public void EmitCall(IOperand routine, IOperand[] args, IVariable? result)
            {
                args = PrepareCallArguments(args);

                if (MatchesZeroCallee(routine))
                {
                    foreach (var arg in args)
                    {
                        if (IsStackOperand(arg))
                            lines.Add("    POP");
                    }

                    if (result != null)
                        EmitStore(result, owner.Zero);

                    return;
                }

                if (ReferenceEquals(routine, Stack))
                {
                    var spilledRoutine = AddGeneratedLocal("CALL_TARGET");
                    EmitStore(spilledRoutine, Stack);
                    routine = spilledRoutine;
                }

                foreach (var arg in args)
                    EmitPushOperand(arg);

                if (routine is RoutineBuilder directRoutine && args.Length <= 3)
                {
                    EmitDirectRoutineCall(directRoutine, args.Length);
                    ConsumeStackResult(result, discardIfUnused: CleanStack);
                    return;
                }

                var selectorLocal = AddGeneratedLocal("CALL_SELECTOR");
                var zeroSelector = DefineLabel();
                var done = DefineLabel();

                EmitPushOperand(routine);
                EmitStore(selectorLocal, Stack);
                BranchIfZero(selectorLocal, zeroSelector, true);
                EmitPushOperand(selectorLocal);
                lines.Add($"    CALLF {args.Length}");
                ConsumeStackResult(result, discardIfUnused: CleanStack);
                Branch(done);

                MarkLabel(zeroSelector);
                for (var i = 0; i < args.Length; i++)
                    lines.Add("    POP");

                if (result != null)
                    EmitStore(result, owner.Zero);

                MarkLabel(done);
            }

            public void EmitStore(IVariable dest, IOperand src)
            {
                if (dest is not NamedVariable variable)
                    throw new NotSupportedException("Cornerstone routine emission is not implemented for this construct yet.");

                if (variable.Kind == VariableKind.Stack)
                {
                    if (!ReferenceEquals(src, Stack))
                        EmitPushOperand(src);

                    return;
                }

                if (!ReferenceEquals(src, Stack))
                    EmitPushOperand(src);

                switch (variable.Kind)
                {
                    case VariableKind.Local:
                        lines.Add($"    PUTL {variable.Name}");
                        return;

                    case VariableKind.Global:
                        lines.Add($"    PUTG {variable.Name}");
                        return;

                    default:
                        ThrowNotSupported();
                        return;
                }
            }

            public bool TryEmitLowCoreRead(string field, IVariable resultStorage)
            {
                switch (field)
                {
                    case "FLAGS":
                        EmitRuntimeCall(RuntimeLib.GetLowCoreFlags, [], resultStorage);
                        return true;

                    case "RELEASEID":
                    case "ZORKID":
                        EmitRawLine($"    PUSHW {GameBuilder.MetadataReleaseIdLabel}");
                        EmitRawLine("    VLOADW_ 0x00");
                        EmitStore(resultStorage, Stack);
                        return true;

                    case "SCRH":
                        EmitRuntimeCall(RuntimeLib.GetScreenWidth, [], resultStorage);
                        return true;

                    case "SCRV":
                        EmitRuntimeCall(RuntimeLib.GetScreenHeight, [], resultStorage);
                        return true;

                    case "STDREV":
                        EmitStore(resultStorage, owner.MakeOperand(0x0101));
                        return true;

                    case "ZVERSION":
                        EmitStore(resultStorage, owner.MakeOperand((owner.EmulatedZMachineVersion << 8) | owner.EmulatedZVersionFlags));
                        return true;

                    default:
                        return false;
                }
            }

            public bool TryEmitLowCoreWrite(string field, IOperand newValue)
            {
                switch (field)
                {
                    case "FLAGS":
                        EmitRuntimeCall(RuntimeLib.SetLowCoreFlags, [newValue], null);
                        return true;

                    default:
                        return false;
                }
            }

            public bool TryEmitLowCoreGetTable(string field, IVariable resultStorage)
            {
                switch (field)
                {
                    case "SERIAL":
                        EmitStore(resultStorage, owner.MakeOperand(0x12));
                        return true;

                    default:
                        return false;
                }
            }

            public void EmitPopStack()
            {
                lines.Add("    POP");
            }

            public void EmitPushUserStack(IOperand value, IOperand stack, ILabel label, bool polarity) => ThrowNotSupported();

            public void Finish()
            {
            }

            public IConstantOperand Add(IConstantOperand other) => new CompositeOperand(this, other);

            public IEnumerable<string> GetLines()
            {
                if (directCallSites.Count == 0)
                    return lines;

                var rendered = lines.ToArray();
                foreach (var callSite in directCallSites)
                {
                    var opcode = string.Equals(AssignedModuleName, callSite.Target.AssignedModuleName, StringComparison.OrdinalIgnoreCase)
                        ? $"CALL{callSite.ArgumentCount}"
                        : $"CALLF{callSite.ArgumentCount}";
                    rendered[callSite.LineIndex] = $"    {opcode} {callSite.Target.Name}";
                }

                return rendered;
            }

            public IEnumerable<string> GetLocalDeclarationDirectives()
            {
                for (var i = 0; i < locals.Count; i++)
                {
                    var local = locals[i];
                    var defaultValue = local.DefaultValue;

                    if (defaultValue == null && i >= parameterCount)
                        defaultValue = owner.Zero;

                    if (defaultValue != null)
                        yield return $".local {local.Name}={FormatLocalInitializerOperand(defaultValue)}";
                    else
                        yield return $".local {local.Name}";
                }
            }

            private void EmitDirectRoutineCall(RoutineBuilder routine, int argumentCount)
            {
                directCallSites.Add(new DirectRoutineCallSite(lines.Count, routine, argumentCount));
                lines.Add($"    CALL{argumentCount} {routine.Name}");
            }

            private sealed record DirectRoutineCallSite(int LineIndex, RoutineBuilder Target, int ArgumentCount);

            internal void EmitRawLine(string line) => lines.Add(RewriteNamedSlotOperands(line));

            private NamedVariable AddLocal(string name)
            {
                if (locals.Any(local => string.Equals(local.Name, name, StringComparison.OrdinalIgnoreCase)))
                    throw new ArgumentException($"A local variable already exists by the name '{name}'.", nameof(name));

                var result = new NamedVariable(name, VariableKind.Local, locals.Count);
                locals.Add(result);
                return result;
            }

            private NamedVariable AddGeneratedLocal(string prefix)
            {
                string name;

                do
                    name = $"__{prefix}_{nextGeneratedLocalOrdinal++:D4}";
                while (locals.Any(local => string.Equals(local.Name, name, StringComparison.OrdinalIgnoreCase)));

                return AddLocal(name);
            }

            private void EmitConsoleStartup()
            {
                lines.Add("    PUSH0");
                lines.Add($"    PUTMG 0x{CursorRowSlot:X2}");
                lines.Add("    PUSH0");
                lines.Add($"    PUTMG 0x{CursorColumnSlot:X2}");
                // Activate color mode before clearing, so DISP uses the blue background
                lines.Add("    PUSH1");
                lines.Add($"    PUTMG 0x{DisplayModeSlot:X2}");
                // Refresh the computed display attribute so PRCHAR uses the new color base
                lines.Add("    PUSH0");
                lines.Add($"    PUTMG 0x{TextAttributeSlot:X2}");
                // Set scroll bottom before clearing so the scroll-down workaround works
                lines.Add($"    LOADMG 0x{ScreenHeightSlot:X2}");
                lines.Add($"    PUTMG 0x{ScrollBottomSlot:X2}");
                lines.Add("    DISP 0x05");
                // MME's DISP 0x05 misses the last screen row; scroll down 1 line to fill it
                lines.Add("    PUSH1");
                lines.Add("    XDISP 0x00");
                lines.Add(FormatImmediatePush(MainTextTopRow));
                lines.Add($"    PUTMG 0x{ScrollTopSlot:X2}");
                lines.Add($"    LOADMG 0x{ScreenHeightSlot:X2}");
                lines.Add($"    PUTMG 0x{CursorRowSlot:X2}");
                lines.Add("    PUSH0");
                lines.Add($"    PUTMG 0x{CursorColumnSlot:X2}");
                lines.Add($"    LOADMG 0x{ScreenHeightSlot:X2}");
                lines.Add($"    STOREG {owner.consoleRow.Name}");
                lines.Add("    PUSH0");
                lines.Add($"    STOREG {owner.consoleColumn.Name}");
            }

            private void EmitBranchIfEqual(IOperand value, IReadOnlyList<IOperand> options, ILabel label, bool polarity)
            {
                if (options.Count == 0)
                    throw new ArgumentException("At least one equality option is required.", nameof(options));

                if (polarity)
                {
                    for (var i = 0; i < options.Count; i++)
                        EmitSingleEqualityCompare(value, options[i], label, jumpOnEqual: true, preserveValue: i < options.Count - 1);

                    return;
                }

                if (options.Count == 1)
                {
                    EmitSingleEqualityCompare(value, options[0], label, jumpOnEqual: false, preserveValue: false);
                    return;
                }

                var matchedLabel = DefineLabel();
                for (var i = 0; i < options.Count - 1; i++)
                    EmitSingleEqualityCompare(value, options[i], matchedLabel, jumpOnEqual: true, preserveValue: true);

                EmitSingleEqualityCompare(value, options[^1], label, jumpOnEqual: false, preserveValue: false);
                MarkLabel(matchedLabel);
            }

            private void EmitSingleEqualityCompare(IOperand value, IOperand option, ILabel label, bool jumpOnEqual, bool preserveValue)
            {
                if (IsStackOperand(value))
                {
                    if (preserveValue)
                        lines.Add("    DUP");
                }
                else
                {
                    EmitPushOperand(value);
                }

                if (!IsStackOperand(option))
                    EmitPushOperand(option);

                EmitConditionalJump(jumpOnEqual ? "JUMPEQ" : "JUMPNE", label);
            }

            private void EmitOrderedComparison(IOperand left, IOperand right, string orderedOpcode, string reversedOpcode, ILabel label)
            {
                var leftOnStack = IsStackOperand(left);
                var rightOnStack = IsStackOperand(right);

                if (!leftOnStack && !rightOnStack)
                {
                    EmitPushOperand(left);
                    EmitPushOperand(right);
                    EmitConditionalJump(orderedOpcode, label);
                    return;
                }

                if (leftOnStack && rightOnStack)
                {
                    EmitConditionalJump(orderedOpcode, label);
                    return;
                }

                if (leftOnStack)
                {
                    EmitPushOperand(right);
                    EmitConditionalJump(orderedOpcode, label);
                    return;
                }

                EmitPushOperand(left);
                EmitConditionalJump(reversedOpcode, label);
            }

            private void EmitTestBits(IOperand left, IOperand right, ILabel label, bool polarity)
            {
                if (IsStackOperand(left))
                {
                    var spilledLeft = AddGeneratedLocal("TEST_BITS_LEFT");
                    EmitStore(spilledLeft, Stack);
                    left = spilledLeft;
                }

                if (IsStackOperand(right))
                {
                    lines.Add("    DUP");
                    EmitPushOperand(left);
                    lines.Add("    AND");
                    EmitConditionalJump(polarity ? "JUMPEQ" : "JUMPNE", label);
                    return;
                }

                EmitPushOperand(left);
                EmitPushOperand(right);
                lines.Add("    AND");
                EmitPushOperand(right);
                EmitConditionalJump(polarity ? "JUMPEQ" : "JUMPNE", label);
            }

            private void EmitAdjustAndCompare(NamedVariable variable, IOperand right, bool add, ILabel label, string opcode)
            {
                EmitPushOperand(variable);
                lines.Add("    PUSH1");
                lines.Add(add ? "    ADD" : "    SUB");
                ConsumeStackResult(variable, discardIfUnused: false);

                EmitOrderedComparison(variable, right, opcode, InvertOrderedComparisonOpcode(opcode), label);
            }

            private void EmitPersistenceBranchFailure(ILabel label, bool polarity)
            {
                if (!polarity)
                    Branch(label);
            }

            private void EmitConditionalJump(string opcode, ILabel label)
            {
                if (label is not Label concrete)
                    throw new NotSupportedException("Cornerstone routine emission is not implemented for this construct yet.");

                if (concrete.Ordinal >= 0)
                {
                    lines.Add($"    {opcode} {FormatLabelName(concrete)}");
                    return;
                }

                var skipLabel = DefineLabel();
                lines.Add($"    {InvertConditionalOpcode(opcode)} {FormatBranchTarget(skipLabel)}");
                EmitSpecialBranchTarget(concrete);
                MarkLabel(skipLabel);
            }

            private bool TryEmitSpecialBranchTarget(ILabel label)
            {
                if (label is not Label concrete || concrete.Ordinal >= 0)
                    return false;

                EmitSpecialBranchTarget(concrete);
                return true;
            }

            private void EmitSpecialBranchTarget(Label label)
            {
                switch (label.Ordinal)
                {
                    case -1:
                        EmitReturnTrue();
                        return;

                    case -2:
                        // Return zero, not Cornerstone's "FALSE" (0x8001).
                        lines.Add("    RZERO");
                        return;

                    default:
                        ThrowNotSupported();
                        return;
                }
            }

            private void EmitReturnTrue()
            {
                lines.Add("    PUSH1");
                lines.Add("    RETURN");
            }

            private static string FormatLabelName(Label label) => $"loc_{label.Ordinal:X4}";

            private static string FormatBranchTarget(ILabel label)
            {
                if (label is not Label concrete || concrete.Ordinal < 0)
                    throw new NotSupportedException("Cornerstone routine emission is not implemented for this construct yet.");

                return FormatLabelName(concrete);
            }

            private bool IsStackOperand(IOperand operand) => ReferenceEquals(operand, Stack);

            private static string InvertConditionalOpcode(string opcode)
            {
                return opcode switch
                {
                    "JUMPEQ" => "JUMPNE",
                    "JUMPGE" => "JUMPL",
                    "JUMPG" => "JUMPLE",
                    "JUMPLE" => "JUMPG",
                    "JUMPL" => "JUMPGE",
                    "JUMPNZ" => "JUMPZ",
                    "JUMPNE" => "JUMPEQ",
                    "JUMPZ" => "JUMPNZ",
                    _ => throw new NotSupportedException("Cornerstone routine emission is not implemented for this construct yet."),
                };
            }

            private static string InvertOrderedComparisonOpcode(string opcode)
            {
                return opcode switch
                {
                    "JUMPGE" => "JUMPL",
                    "JUMPG" => "JUMPLE",
                    "JUMPLE" => "JUMPG",
                    "JUMPL" => "JUMPGE",
                    _ => throw new NotSupportedException("Cornerstone routine emission is not implemented for this construct yet."),
                };
            }

            private void EmitPrintOperand(IOperand operand, string opcode)
            {
                EmitPushOperand(operand);
                lines.Add($"    {opcode}");
            }

            private void EmitAdvanceConsoleColumn(int amount)
            {
                if (amount == 0)
                    return;

                lines.Add($"    LOADG {owner.consoleColumn.Name}");
                lines.Add(FormatImmediatePush(amount));
                lines.Add("    ADD");
                lines.Add($"    STOREG {owner.consoleColumn.Name}");
            }

            private void EmitRuntimeCall(string routineId, IReadOnlyList<IOperand> args, IVariable? result)
            {
                var routineName = owner.RuntimeLib.Use(routineId);
                var runtimeRoutine = owner.FindRoutine(routineName);
                var returnsValue = owner.RuntimeLib.ReturnsValue(routineId);
                var preparedArgs = PrepareCallArguments(args);

                foreach (var arg in preparedArgs)
                    EmitPushOperand(arg);

                if (preparedArgs.Length <= 3 && runtimeRoutine != null)
                {
                    EmitDirectRoutineCall(runtimeRoutine, preparedArgs.Length);
                }
                else
                {
                    if (runtimeRoutine != null)
                        EmitPushOperand(runtimeRoutine);
                    else
                        lines.Add($"    PUSHW {routineName}");

                    lines.Add($"    CALLF {preparedArgs.Length}");
                }

                if (!returnsValue)
                {
                    if (result != null)
                        throw new InvalidOperationException($"Runtime routine '{routineId}' does not return a value.");

                    return;
                }

                ConsumeStackResult(result, discardIfUnused: result == null);
            }

            private IOperand[] PrepareCallArguments(IReadOnlyList<IOperand> args)
            {
                if (args.Count <= 1)
                    return args.ToArray();

                var prepared = args.ToArray();

                for (var i = 0; i < prepared.Length; i++)
                {
                    if (!IsStackOperand(prepared[i]))
                        continue;

                    var spilled = AddGeneratedLocal("CALL_ARG");
                    EmitStore(spilled, Stack);
                    prepared[i] = spilled;
                }

                return prepared;
            }

            private void ConsumeStackResult(IVariable? result, bool discardIfUnused)
            {
                if (result == null)
                {
                    if (discardIfUnused)
                        lines.Add("    POP");

                    return;
                }

                if (ReferenceEquals(result, Stack))
                    return;

                if (result is not NamedVariable variable)
                    throw new NotSupportedException("Cornerstone routine emission is not implemented for this construct yet.");

                switch (variable.Kind)
                {
                    case VariableKind.Local:
                        lines.Add($"    PUTL {variable.Name}");
                        return;

                    case VariableKind.Global:
                        lines.Add($"    PUTG {variable.Name}");
                        return;

                    default:
                        ThrowNotSupported();
                        return;
                }
            }

            private void EmitPushOperand(IOperand operand)
            {
                switch (operand)
                {
                    case NumericOperand numeric:
                        lines.Add(FormatImmediatePush(numeric.Value));
                        return;

                    case NamedConstant constant:
                        lines.Add($"    PUSHW {constant.Name}");
                        return;

                    case SymbolicOperand symbolic:
                        lines.Add($"    PUSHW {symbolic.Name}");
                        return;

                    case NamedVariable variable when variable.Kind == VariableKind.Stack:
                        return;

                    case NamedVariable variable when variable.Kind == VariableKind.Local:
                        lines.Add($"    PUSHL {variable.Name}");
                        return;

                    case NamedVariable variable when variable.Kind == VariableKind.Global:
                        lines.Add($"    LOADG {variable.Name}");
                        return;

                    case PropertyBuilder property:
                        lines.Add($"    PUSHW P?{property.Name}");
                        return;

                    case FlagBuilder flag:
                        lines.Add($"    PUSHW {flag.Name}");
                        return;

                    case ObjectBuilder obj:
                        lines.Add($"    PUSHW {obj.Name}");
                        return;

                    case RoutineBuilder routine:
                        routine.MarkRequiresFarSelector();
                        lines.Add($"    PUSHW {routine.Name}");
                        return;

                    case IIndirectOperand indirect when indirect.Variable is NamedVariable indirectVariable:
                        if (indirectVariable.Kind == VariableKind.Stack)
                        {
                            lines.Add("    DUP");
                            return;
                        }

                        EmitPushOperand(indirectVariable);
                        return;

                    case TableBuilder:
                    case CompositeOperand:
                        lines.Add($"    PUSHW {FormatConstantOperand(operand.StripIndirect())}");
                        return;

                    default:
                        ThrowNotSupported();
                        return;
                }
            }

            private void DiscardUnusedStackOperand(IOperand operand)
            {
                if (IsStackOperand(operand))
                    lines.Add("    POP");
            }

            private static bool MatchesZeroCallee(IOperand operand) => operand is NumericOperand { Value: 0 };

            private static string FormatSignedWord(int value)
            {
                if (value is >= 0 and <= 9)
                    return value.ToString(CultureInfo.InvariantCulture);

                if (value < 0)
                    return value.ToString(CultureInfo.InvariantCulture);

                return $"0x{unchecked((ushort)value):X4}";
            }

            private static string FormatImmediatePush(int value)
            {
                return value switch
                {
                    -8 => "    PUSHm8",
                    -1 => "    PUSHm1",
                    0 => "    PUSH0",
                    1 => "    PUSH1",
                    2 => "    PUSH2",
                    3 => "    PUSH3",
                    4 => "    PUSH4",
                    5 => "    PUSH5",
                    6 => "    PUSH6",
                    7 => "    PUSH7",
                    8 => "    PUSH8",
                    >= 0 and <= 0xFF => $"    PUSHB 0x{value:X2}",
                    _ => $"    PUSHW {FormatSignedWord(value)}",
                };
            }

            private string RewriteNamedSlotOperands(string line)
            {
                var localMatch = NumberedLocalOperandRegex.Match(line);
                if (localMatch.Success
                    && int.TryParse(localMatch.Groups[3].Value, NumberStyles.None, CultureInfo.InvariantCulture, out var localIndex)
                    && localIndex >= 0
                    && localIndex < locals.Count)
                {
                    return $"{localMatch.Groups[1].Value}{localMatch.Groups[2].Value} {locals[localIndex].Name}";
                }

                var globalMatch = NumberedGlobalOperandRegex.Match(line);
                if (globalMatch.Success
                    && int.TryParse(globalMatch.Groups[3].Value, NumberStyles.None, CultureInfo.InvariantCulture, out var globalIndex)
                    && globalIndex >= 0
                    && globalIndex < owner.globals.Count)
                {
                    return $"{globalMatch.Groups[1].Value}{globalMatch.Groups[2].Value} {owner.globals[globalIndex].Name}";
                }

                return line;
            }

            private static void ThrowNotSupported([CallerMemberName] string memberName = "") =>
                throw new NotSupportedException($"Cornerstone routine emission is not implemented for this construct yet ({memberName}).");

            [GeneratedRegex(@"^(\s*)(PUSHL|PUTL|STOREL|LOADL)\s+(\d+)\s*$", RegexOptions.Compiled)]
            private static partial Regex GetNumberedLocalOperandRegex();

            [GeneratedRegex(@"^(\s*)(LOADG|PUTG|STOREG)\s+(\d+)\s*$", RegexOptions.Compiled)]
            private static partial Regex GetNumberedGlobalOperandRegex();
        }
    }
}