/* Copyright 2010-2023 Tara McGrew
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
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.IO;
using System.Linq;
using System.Text;
using Zapf.Parsing.Expressions;
using Zapf.Parsing.Instructions;
using Zilf.Common;
using Zilf.Emit;

namespace Zilf.Emit.Zap
{
    // TODO: make sure RoutineBuilder is guaranteed nonzero in V6/V7
    class RoutineBuilder : ConstantOperandBase, IRoutineBuilder, INonzeroConstantOperand, IProvideArcturusEmit
    {
        internal static readonly Label RTRUE = new("TRUE");
        internal static readonly Label RFALSE = new("FALSE");
        internal static readonly VariableOperand STACK = new("STACK");
        const char INDENT = '\t';

        readonly GameBuilder game;
        readonly string name;
        readonly bool entryPoint;

        internal DebugLineRef defnStart, defnEnd;

        readonly PeepholeBuffer<ZapCode> peep;
        int nextLabelNum;
        string? pendingDebugText;

        readonly List<LocalBuilder> requiredParams = new();
        readonly List<LocalBuilder> optionalParams = new();
        readonly List<LocalBuilder> locals = new();

        public RoutineBuilder(GameBuilder game, string name, bool entryPoint, bool cleanStack)
        {
            this.game = game;
            this.name = name;
            this.entryPoint = entryPoint;
            CleanStack = cleanStack;

            peep = new PeepholeBuffer<ZapCode>
            {
                Combiner = new PeepholeCombiner(game),
                LabelFactory = DefineLabel,
            };
            RoutineStart = DefineLabel();
        }

        public override string ToString()
        {
            return name;
        }

        public bool CleanStack { get; }
        public ILabel RTrue => RTRUE;
        public ILabel RFalse => RFALSE;
        public IVariable Stack => STACK;

        public bool UsesStackBasedCalls => false;

        bool LocalExists(string localName)
        {
            return requiredParams.Concat(optionalParams).Concat(locals).Any(lb => lb.Name == localName);
        }

        /// <exception cref="InvalidOperationException">This is an entry point routine.</exception>
        /// <exception cref="ArgumentException">A local variable named <paramref name="paramName"/> is already defined.</exception>
        public ILocalBuilder DefineRequiredParameter(string paramName)
        {
            paramName = GameBuilder.SanitizeSymbol(paramName);

            if (entryPoint)
                throw new InvalidOperationException("Entry point may not have required parameters");
            if (LocalExists(paramName))
                throw new ArgumentException("Local variable already exists: " + paramName, nameof(paramName));

            var local = new LocalBuilder(paramName);
            requiredParams.Add(local);
            return local;
        }

        /// <exception cref="InvalidOperationException">This is an entry point routine and the target is not V6.</exception>
        /// <exception cref="ArgumentException">A local variable named <paramref name="paramName"/> is already defined.</exception>
        public ILocalBuilder DefineOptionalParameter(string paramName)
        {
            paramName = GameBuilder.SanitizeSymbol(paramName);

            if (entryPoint && game.zversion != 6)
                throw new InvalidOperationException("Entry point may not have optional parameters");
            if (LocalExists(paramName))
                throw new ArgumentException("Local variable already exists: " + paramName, nameof(paramName));

            var local = new LocalBuilder(paramName);
            optionalParams.Add(local);
            return local;
        }

        /// <exception cref="InvalidOperationException">This is an entry point routine and the target is not V6.</exception>
        /// <exception cref="ArgumentException">A local variable named <paramref name="localName"/> is already defined.</exception>
        public ILocalBuilder DefineLocal(string localName)
        {
            localName = GameBuilder.SanitizeSymbol(localName);

            if (entryPoint && game.zversion != 6)
                throw new InvalidOperationException("Entry point may not have local variables");
            if (LocalExists(localName))
                throw new ArgumentException("Local variable already exists: " + localName, nameof(localName));

            var local = new LocalBuilder(localName);
            locals.Add(local);
            return local;
        }

        public ILabel RoutineStart { get; }

        public ILabel DefineLabel()
        {
            return new Label("?L" + nextLabelNum++);
        }

        public void MarkLabel(ILabel label)
        {
            peep.MarkLabel(label);
        }

        void AddLine(Instruction code, ILabel? target, PeepholeLineType type)
        {
            ZapCode zc;
            zc.Instruction = code;
            zc.DebugText = pendingDebugText;
            pendingDebugText = null;

            peep.AddLine(zc, target, type);
        }

        public void MarkSequencePoint(DebugLineRef lineRef)
        {
            if (game.debug != null)
                pendingDebugText =
                    $".DEBUG-LINE {game.debug.GetFileNumber(lineRef.File)},{lineRef.Line},{lineRef.Column}";
        }

        public void Branch(ILabel label)
        {
            AddLine(new Instruction("JUMP"), label, PeepholeLineType.BranchAlways);
        }

        public bool HasArgCount => game.zversion >= 5;

        /// <exception cref="ArgumentException">This condition requires a variable, but <paramref name="left"/> is not a variable.</exception>
        /// <exception cref="ArgumentException">The wrong number of operands were provided.</exception>
        public void Branch(Condition cond, IOperand? left, IOperand? right, ILabel label, bool polarity)
        {
            string opcode;
            bool leftVar = false, nullary = false, unary = false;

            switch (cond)
            {
                case Condition.DecCheck:
                    opcode = "DLESS?";
                    leftVar = true;
                    break;
                case Condition.Greater:
                    opcode = "GRTR?";
                    break;
                case Condition.IncCheck:
                    opcode = "IGRTR?";
                    leftVar = true;
                    break;
                case Condition.Inside:
                    opcode = "IN?";
                    break;
                case Condition.Less:
                    opcode = "LESS?";
                    break;
                case Condition.TestAttr:
                    opcode = "FSET?";
                    break;
                case Condition.TestBits:
                    opcode = "BTST";
                    break;
                case Condition.PictureData:
                    opcode = "PICINF";
                    break;
                case Condition.MakeMenu:
                    opcode = "MENU";
                    break;

                case Condition.ArgProvided:
                    opcode = "ASSIGNED?";
                    leftVar = true;
                    unary = true;
                    break;

                case Condition.Verify:
                    opcode = "VERIFY";
                    nullary = true;
                    break;
                case Condition.Original:
                    opcode = "ORIGINAL?";
                    nullary = true;
                    break;

                default:
                    throw UnhandledCaseException.FromEnum(cond, "conditional operation");
            }

            if (leftVar && left is not IVariable)
                throw new ArgumentException("This condition requires a variable", nameof(left));

            if (nullary)
            {
                if (left != null || right != null)
                    throw new ArgumentException("Expected no operands for nullary condition");
            }
            else if (unary)
            {
                if (right != null)
                    throw new ArgumentException("Expected one operand for unary condition", nameof(right));
            }
            else
            {
                if (left == null || right == null)
                    throw new ArgumentException("Expected two operands for binary condition", nameof(right));
            }

            var instruction = new Instruction(opcode);
            if (unary)
            {
                instruction.Operands.Add(new QuoteExpr(left!.ToAsmExpr()));
            }
            else if (!nullary)
            {
                Debug.Assert(left != null);
                var leftExpr = left.ToAsmExpr();
                instruction.Operands.Add(leftVar ? new QuoteExpr(leftExpr) : leftExpr);
                instruction.Operands.Add(right!.ToAsmExpr());
            }

            AddLine(
                instruction,
                label,
                polarity ? PeepholeLineType.BranchPositive : PeepholeLineType.BranchNegative);
        }

        public void BranchIfZero(IOperand operand, ILabel label, bool polarity)
        {
            AddLine(
                new Instruction("ZERO?", operand.ToAsmExpr()),
                label,
                polarity ? PeepholeLineType.BranchPositive : PeepholeLineType.BranchNegative);
        }

        public void BranchIfEqual(IOperand value, IOperand option1, ILabel label, bool polarity)
        {
            AddLine(
                new Instruction("EQUAL?", value.ToAsmExpr(), option1.ToAsmExpr()),
                label,
                polarity ? PeepholeLineType.BranchPositive : PeepholeLineType.BranchNegative);
        }

        public void BranchIfEqual(IOperand value, IOperand option1, IOperand option2, ILabel label, bool polarity)
        {
            AddLine(
                new Instruction("EQUAL?", value.ToAsmExpr(), option1.ToAsmExpr(), option2.ToAsmExpr()),
                label,
                polarity ? PeepholeLineType.BranchPositive : PeepholeLineType.BranchNegative);
        }

        public void BranchIfEqual(IOperand value, IOperand option1, IOperand option2, IOperand option3, ILabel label, bool polarity)
        {
            AddLine(
                new Instruction("EQUAL?", value.ToAsmExpr(), option1.ToAsmExpr(), option2.ToAsmExpr(), option3.ToAsmExpr()),
                label,
                polarity ? PeepholeLineType.BranchPositive : PeepholeLineType.BranchNegative);
        }

        public void Return(IOperand result)
        {
            if (result == GameBuilder.ONE)
                AddLine(new Instruction("RTRUE"), RTRUE, PeepholeLineType.BranchAlways);
            else if (result == GameBuilder.ZERO)
                AddLine(new Instruction("RFALSE"), RFALSE, PeepholeLineType.BranchAlways);
            else if (result == STACK)
                AddLine(new Instruction("RSTACK"), null, PeepholeLineType.Terminator);
            else
                AddLine(new Instruction("RETURN", result.ToAsmExpr()), null, PeepholeLineType.Terminator);
        }

        public bool HasUndo => game.zversion >= 5;

        public void EmitNullary(NullaryOp op, IVariable? result)
        {
            var opcode = op switch
            {
                NullaryOp.RestoreUndo => "IRESTORE",
                NullaryOp.SaveUndo => "ISAVE",
                NullaryOp.ShowStatus => "USL",
                NullaryOp.Catch => "CATCH",
                _ => throw UnhandledCaseException.FromEnum(op, "nullary operation")
            };

            AddLine(
                new Instruction(opcode) { StoreTarget = result?.ToString() },
                null,
                PeepholeLineType.Plain);
        }

        public void EmitUnary(UnaryOp op, IOperand value, IVariable? result)
        {
            if (op == UnaryOp.Neg)
            {
                AddLine(
                    new Instruction("SUB", new NumericLiteral(0), value.ToAsmExpr()) { StoreTarget = result?.ToString() },
                    null,
                    PeepholeLineType.Plain);
                return;
            }

            string opcode;
            bool pred = false;

            switch (op)
            {
                case UnaryOp.Not:
                    opcode = "BCOM";
                    break;
                case UnaryOp.GetParent:
                    opcode = "LOC";
                    break;
                case UnaryOp.GetPropSize:
                    opcode = "PTSIZE";
                    break;
                case UnaryOp.LoadIndirect:
                    opcode = "VALUE";
                    break;
                case UnaryOp.Random:
                    opcode = "RANDOM";
                    break;
                case UnaryOp.GetChild:
                    opcode = "FIRST?";
                    pred = true;
                    break;
                case UnaryOp.GetSibling:
                    opcode = "NEXT?";
                    pred = true;
                    break;
                case UnaryOp.RemoveObject:
                    opcode = "REMOVE";
                    break;
                case UnaryOp.DirectInput:
                    opcode = "DIRIN";
                    break;
                case UnaryOp.DirectOutput:
                    opcode = "DIROUT";
                    break;
                case UnaryOp.OutputBuffer:
                    opcode = "BUFOUT";
                    break;
                case UnaryOp.OutputStyle:
                    opcode = "HLIGHT";
                    break;
                case UnaryOp.SplitWindow:
                    opcode = "SPLIT";
                    break;
                case UnaryOp.SelectWindow:
                    opcode = "SCREEN";
                    break;
                case UnaryOp.ClearWindow:
                    opcode = "CLEAR";
                    break;
                case UnaryOp.GetCursor:
                    opcode = "CURGET";
                    break;
                case UnaryOp.EraseLine:
                    opcode = "ERASE";
                    break;
                case UnaryOp.SetFont:
                    opcode = "FONT";
                    break;
                case UnaryOp.CheckUnicode:
                    opcode = "CHECKU";
                    break;
                case UnaryOp.FlushStack:
                    opcode = "FSTACK";
                    break;
                case UnaryOp.PopUserStack:
                    opcode = "POP";
                    break;
                case UnaryOp.PictureTable:
                    opcode = "PICSET";
                    break;
                case UnaryOp.MouseWindow:
                    opcode = "MOUSE-LIMIT";
                    break;
                case UnaryOp.ReadMouse:
                    opcode = "MOUSE-INFO";
                    break;
                case UnaryOp.PrintForm:
                    opcode = "PRINTF";
                    break;
                case UnaryOp.BufferScreen:
                    opcode = "BUFSCR";
                    break;
                default:
                    throw UnhandledCaseException.FromEnum(op, "unary operation");
            }

            if (pred)
            {
                var label = DefineLabel();

                AddLine(
                    new Instruction(opcode, value.ToAsmExpr()) { StoreTarget = result?.ToString() },
                    label,
                    PeepholeLineType.BranchPositive);

                peep.MarkLabel(label);
            }
            else
            {
                AddLine(
                    new Instruction(opcode, value.ToAsmExpr()) { StoreTarget = result?.ToString() },
                    null,
                    PeepholeLineType.Plain);
            }
        }

        public void EmitBinary(BinaryOp op, IOperand left, IOperand right, IVariable? result)
        {
            switch (op)
            {
                // optimize special cases
                case BinaryOp.Add when left == game.One && right == result || right == game.One && left == result:
                    AddLine(new Instruction("INC", new QuoteExpr(result.ToAsmExpr())), null, PeepholeLineType.Plain);
                    return;
                case BinaryOp.Sub when left == result && right == game.One:
                    AddLine(new Instruction("DEC", new QuoteExpr(result.ToAsmExpr())), null, PeepholeLineType.Plain);
                    return;
                case BinaryOp.Add when left == game.Zero:
                    EmitStore(result!, right);
                    return;
                case BinaryOp.Add when right == game.Zero:
                    EmitStore(result!, left);
                    return;
                case BinaryOp.Sub when right == game.Zero:
                    EmitStore(result!, left);
                    return;
                case BinaryOp.Mul when left == game.One:
                    EmitStore(result!, right);
                    return;
                case BinaryOp.Mul when right == game.One:
                    EmitStore(result!, left);
                    return;
                case BinaryOp.Div when right == game.One:
                    EmitStore(result!, left);
                    return;
                case BinaryOp.StoreIndirect when right == Stack && game.zversion != 6:
                    AddLine(new Instruction("POP", left.ToAsmExpr()), null, PeepholeLineType.Plain);
                    return;
            }

            var opcode = op switch
            {
                BinaryOp.Add => "ADD",
                BinaryOp.And => "BAND",
                BinaryOp.ArtShift => "ASHIFT",
                BinaryOp.Div => "DIV",
                BinaryOp.GetByte => "GETB",
                BinaryOp.GetPropAddress => "GETPT",
                BinaryOp.GetProperty => "GETP",
                BinaryOp.GetNextProp => "NEXTP",
                BinaryOp.GetWord => "GET",
                BinaryOp.LogShift => "SHIFT",
                BinaryOp.Mod => "MOD",
                BinaryOp.Mul => "MUL",
                BinaryOp.Or => "BOR",
                BinaryOp.Sub => "SUB",
                BinaryOp.MoveObject => "MOVE",
                BinaryOp.SetFlag => "FSET",
                BinaryOp.ClearFlag => "FCLEAR",
                BinaryOp.DirectOutput => "DIROUT",
                BinaryOp.SetCursor => "CURSET",
                BinaryOp.SetColor => "COLOR",
                BinaryOp.SetTrueColor => "TCOLOR",
                BinaryOp.Throw => "THROW",
                BinaryOp.StoreIndirect => "SET",
                BinaryOp.FlushUserStack => "FSTACK",
                BinaryOp.GetWindowProperty => "WINGET",
                BinaryOp.ScrollWindow => "SCROLL",
                BinaryOp.SetFont => "FONT",
                _ => throw UnhandledCaseException.FromEnum(op, "binary operation")
            };

            AddLine(
                new Instruction(opcode, left.ToAsmExpr(), right.ToAsmExpr()) { StoreTarget = result?.ToString() },
                null,
                PeepholeLineType.Plain);
        }

        public void EmitTernary(TernaryOp op, IOperand left, IOperand center, IOperand right, IVariable? result)
        {
            var opcode = op switch
            {
                TernaryOp.PutByte => "PUTB",
                TernaryOp.PutProperty => "PUTP",
                TernaryOp.PutWord => "PUT",
                TernaryOp.CopyTable => "COPYT",
                TernaryOp.PutWindowProperty => "WINPUT",
                TernaryOp.DrawPicture => "DISPLAY",
                TernaryOp.WindowStyle => "WINATTR",
                TernaryOp.MoveWindow => "WINPOS",
                TernaryOp.WindowSize => "WINSIZE",
                TernaryOp.SetMargins => "MARGIN",
                TernaryOp.SetCursor => "CURSET",
                TernaryOp.DirectOutput => "DIROUT",
                TernaryOp.ErasePicture => "DCLEAR",
                TernaryOp.SetColor => "COLOR",
                TernaryOp.SetTrueColor => "TCOLOR",
                _ => throw UnhandledCaseException.FromEnum(op, "ternary operation")
            };

            AddLine(
                new Instruction(opcode, left.ToAsmExpr(), center.ToAsmExpr(), right.ToAsmExpr()) { StoreTarget = result?.ToString() },
                null,
                PeepholeLineType.Plain);
        }

        public void EmitEncodeText(IOperand src, IOperand length, IOperand srcOffset, IOperand dest)
        {
            AddLine(
                new Instruction("ZWSTR", src.ToAsmExpr(), length.ToAsmExpr(), srcOffset.ToAsmExpr(), dest.ToAsmExpr()),
                null,
                PeepholeLineType.Plain);
        }

        public void EmitTokenize(IOperand text, IOperand parse, IOperand? dictionary, IOperand? flag)
        {
            var inst = new Instruction("LEX", text.ToAsmExpr(), parse.ToAsmExpr());
            if (dictionary != null)
            {
                inst.Operands.Add(dictionary.ToAsmExpr());

                if (flag != null)
                    inst.Operands.Add(flag.ToAsmExpr());
            }

            AddLine(inst, null, PeepholeLineType.Plain);
        }

        public void EmitRestart()
        {
            AddLine(new Instruction("RESTART"), null, PeepholeLineType.Terminator);
        }

        public void EmitQuit()
        {
            AddLine(new Instruction("QUIT"), null, PeepholeLineType.Terminator);
        }

        public bool HasBranchSave => game.zversion < 4;

        public void EmitSave(ILabel label, bool polarity)
        {
            AddLine(new Instruction("SAVE"), label,
                polarity ? PeepholeLineType.BranchPositive : PeepholeLineType.BranchNegative);
        }

        public void EmitRestore(ILabel label, bool polarity)
        {
            AddLine(new Instruction("RESTORE"), label,
                polarity ? PeepholeLineType.BranchPositive : PeepholeLineType.BranchNegative);
        }

        public bool HasStoreSave => game.zversion >= 4;

        public void EmitSave(IVariable result)
        {
            AddLine(new Instruction("SAVE") { StoreTarget = result.ToString() }, null, PeepholeLineType.Plain);
        }

        public void EmitRestore(IVariable result)
        {
            AddLine(new Instruction("RESTORE") { StoreTarget = result.ToString() }, null, PeepholeLineType.Plain);
        }

        public bool HasExtendedSave => game.zversion >= 5;
        public void EmitSave(IOperand table, IOperand size, IOperand filename, IOperand? prompt,
            IVariable result)
        {
            var inst = prompt is not null
                ? new Instruction("SAVE", table.ToAsmExpr(), size.ToAsmExpr(), filename.ToAsmExpr(), prompt.ToAsmExpr())
                : new Instruction("SAVE", table.ToAsmExpr(), size.ToAsmExpr(), filename.ToAsmExpr());

            inst.StoreTarget = result.ToString();

            AddLine(inst, null, PeepholeLineType.Plain);
        }

        public void EmitRestore(IOperand table, IOperand size, IOperand filename, IOperand? prompt,
            IVariable result)
        {
            var inst = prompt is not null
                ? new Instruction("RESTORE", table.ToAsmExpr(), size.ToAsmExpr(), filename.ToAsmExpr(), prompt.ToAsmExpr())
                : new Instruction("RESTORE", table.ToAsmExpr(), size.ToAsmExpr(), filename.ToAsmExpr());

            inst.StoreTarget = result.ToString();

            AddLine(inst, null, PeepholeLineType.Plain);
        }

        public void EmitScanTable(IOperand value, IOperand table, IOperand length, IOperand? form,
            IVariable result, ILabel label, bool polarity)
        {
            var inst = new Instruction("INTBL?", value.ToAsmExpr(), table.ToAsmExpr(), length.ToAsmExpr())
            {
                StoreTarget = result.ToString()
            };
            if (form != null)
                inst.Operands.Add(form.ToAsmExpr());

            AddLine(inst, label,
                polarity ? PeepholeLineType.BranchPositive : PeepholeLineType.BranchNegative);
        }

        public void EmitGetChild(IOperand value, IVariable result, ILabel label, bool polarity)
        {
            AddLine(
                new Instruction("FIRST?", value.ToAsmExpr()) { StoreTarget = result.ToString() },
                label,
                polarity ? PeepholeLineType.BranchPositive : PeepholeLineType.BranchNegative);
        }

        public void EmitGetSibling(IOperand value, IVariable result, ILabel label, bool polarity)
        {
            AddLine(
                new Instruction("NEXT?", value.ToAsmExpr()) { StoreTarget = result.ToString() },
                label,
                polarity ? PeepholeLineType.BranchPositive : PeepholeLineType.BranchNegative);
        }

        public void EmitPrintNewLine()
        {
            AddLine(new Instruction("CRLF"), null, PeepholeLineType.Plain);
        }

        public void EmitPrint(string text, bool crlfRtrue)
        {
            var opcode = crlfRtrue ? "PRINTR" : "PRINTI";

            AddLine(
                new Instruction(opcode, new StringLiteral(text)),
                null,
                crlfRtrue ? PeepholeLineType.HeavyTerminator : PeepholeLineType.Plain);
        }

        public void EmitPrint(PrintOp op, IOperand value)
        {
            var opcode = op switch
            {
                PrintOp.Address => "PRINTB",
                PrintOp.Character => "PRINTC",
                PrintOp.Number => "PRINTN",
                PrintOp.Object => "PRINTD",
                PrintOp.PackedAddr => "PRINT",
                PrintOp.Unicode => "PRINTU",
                _ => throw UnhandledCaseException.FromEnum(op, "print operation")
            };

            AddLine(new Instruction(opcode, value.ToAsmExpr()), null, PeepholeLineType.Plain);
        }

        public void EmitPrintTable(IOperand table, IOperand width, IOperand? height, IOperand? skip)
        {
            var inst = new Instruction("PRINTT", table.ToAsmExpr(), width.ToAsmExpr());

            if (height != null)
            {
                inst.Operands.Add(height.ToAsmExpr());

                if (skip != null)
                    inst.Operands.Add(skip.ToAsmExpr());
            }

            AddLine(inst, null, PeepholeLineType.Plain);
        }

        public void EmitPlaySound(IOperand number, IOperand? effect, IOperand? volume, IOperand? routine)
        {
            var inst = new Instruction("SOUND", number.ToAsmExpr());

            if (effect != null)
            {
                inst.Operands.Add(effect.ToAsmExpr());

                if (volume != null)
                {
                    inst.Operands.Add(volume.ToAsmExpr());

                    if (routine != null)
                    {
                        inst.Operands.Add(routine.ToAsmExpr());
                    }
                }
            }

            AddLine(inst, null, PeepholeLineType.Plain);
        }

        public bool HasArcImage => game.zversion is >= 5 and not 6;

        public void EmitArcImage(IOperand imageId, IOperand mode)
        {
            var inst = new Instruction("ARCIMG", imageId.ToAsmExpr(), mode.ToAsmExpr());
            AddLine(inst, null, PeepholeLineType.Plain);
        }

        public void EmitRead(IOperand chrbuf, IOperand? lexbuf, IOperand? interval, IOperand? routine,
            IVariable? result)
        {
            var inst = new Instruction("READ", chrbuf.ToAsmExpr()) { StoreTarget = result?.ToString() };

            if (lexbuf != null)
            {
                inst.Operands.Add(lexbuf.ToAsmExpr());

                if (interval != null)
                {
                    inst.Operands.Add(interval.ToAsmExpr());

                    if (routine != null)
                    {
                        inst.Operands.Add(routine.ToAsmExpr());
                    }
                }
            }

            AddLine(inst, null, PeepholeLineType.Plain);
        }

        public void EmitReadChar(IOperand? interval, IOperand? routine, IVariable result)
        {
            var inst = new Instruction("INPUT", new NumericLiteral(1)) { StoreTarget = result.ToString() };

            if (interval != null)
            {
                inst.Operands.Add(interval.ToAsmExpr());

                if (routine != null)
                {
                    inst.Operands.Add(routine.ToAsmExpr());
                }
            }

            AddLine(inst, null, PeepholeLineType.Plain);
        }

        /// <exception cref="ArgumentException">Too many arguments were supplied for the Z-machine version.</exception>
        public void EmitCall(IOperand routine, IOperand[] args, IVariable? result)
        {
            /* V1-3: CALL (0-3, store)
             * V4: CALL1 (0, store), CALL2 (1, store), XCALL (0-7, store)
             * V5: ICALL1 (0), ICALL2 (1), ICALL (0-3), IXCALL (0-7) */

            if (args.Length > game.MaxCallArguments)
                throw new ArgumentException(
                    $"Too many arguments in routine call: {args.Length} supplied, {game.MaxCallArguments} allowed");

            if (game.zversion < 4)
            {
                // V1-3: use only CALL opcode (3 args max), pop result if not needed
                var inst = new Instruction("CALL", routine.ToAsmExpr()) { StoreTarget = result?.ToString() };
                foreach (var arg in args)
                    inst.Operands.Add(arg.ToAsmExpr());

                AddLine(inst, null, PeepholeLineType.Plain);

                if (result == null && CleanStack)
                    AddLine(new Instruction("FSTACK"), null, PeepholeLineType.Plain);
            }
            else if (game.zversion == 4)
            {
                string opcode = args.Length switch
                {
                    0 => "CALL1",
                    1 => "CALL2",
                    2 or 3 => "CALL",
                    _ => "XCALL",
                };
                var inst = new Instruction(opcode, routine.ToAsmExpr()) { StoreTarget = result?.ToString() };
                foreach (var arg in args)
                    inst.Operands.Add(arg.ToAsmExpr());

                AddLine(inst, null, PeepholeLineType.Plain);

                if (result == null && CleanStack)
                    AddLine(new Instruction("FSTACK"), null, PeepholeLineType.Plain);
            }
            else
            {
                // V5-V6: use CALL/CALL1/CALL2/XCALL if want result
                // use ICALL/ICALL1/ICALL2/IXCALL if not
                string opcode;
                if (result == null)
                {
                    opcode = args.Length switch
                    {
                        0 => "ICALL1",
                        1 => "ICALL2",
                        2 or 3 => "ICALL",
                        _ => "IXCALL",
                    };
                }
                else
                {
                    opcode = args.Length switch
                    {
                        0 => "CALL1",
                        1 => "CALL2",
                        2 or 3 => "CALL",
                        _ => "XCALL",
                    };
                }

                var inst = new Instruction(opcode, routine.ToAsmExpr()) { StoreTarget = result?.ToString() };
                foreach (var arg in args)
                    inst.Operands.Add(arg.ToAsmExpr());

                AddLine(inst, null, PeepholeLineType.Plain);
            }
        }

        public void EmitStore(IVariable dest, IOperand src)
        {
            if (dest != src)
            {
                if (dest == STACK)
                {
                    AddLine(new Instruction("PUSH", src.ToAsmExpr()), null, PeepholeLineType.Plain);
                }
                else if (src == STACK)
                {
                    AddLine(
                        game.zversion == 6 ? new Instruction("POP") { StoreTarget = dest.ToString() } : new Instruction("POP", new QuoteExpr(dest.ToAsmExpr())),
                        null,
                        PeepholeLineType.Plain);
                }
                else
                {
                    AddLine(new Instruction("SET", new QuoteExpr(dest.ToAsmExpr()), src.ToAsmExpr()), null, PeepholeLineType.Plain);
                }
            }
        }

        public void EmitPopStack()
        {
            if (!CleanStack)
                return;

            if (game.zversion <= 4)
            {
                AddLine(new Instruction("FSTACK"), null, PeepholeLineType.Plain);
            }
            else if (game.zversion == 6)
            {
                AddLine(new Instruction("FSTACK", new NumericLiteral(1)), null, PeepholeLineType.Plain);
            }
            else
            {
                AddLine(new Instruction("ICALL2", new NumericLiteral(0), STACK.ToAsmExpr()), null, PeepholeLineType.Plain);
            }
        }

        public void EmitPushUserStack(IOperand value, IOperand stack, ILabel label, bool polarity)
        {
            AddLine(
                new Instruction("XPUSH", value.ToAsmExpr(), stack.ToAsmExpr()),
                label,
                polarity ? PeepholeLineType.BranchPositive : PeepholeLineType.BranchNegative);
        }

        public void Finish()
        {
            game.WriteOutput(string.Empty);

            var sb = new StringBuilder();

            // write routine header
            if (game.debug != null)
            {
                sb.Append(INDENT);
                sb.Append(".DEBUG-ROUTINE ");
                sb.Append(game.debug.GetFileNumber(defnStart.File));
                sb.Append(',');
                sb.Append(defnStart.Line);
                sb.Append(',');
                sb.Append(defnStart.Column);
                sb.Append(",\"");
                sb.Append(name);
                sb.Append('"');
                foreach (var lb in requiredParams.Concat(optionalParams).Concat(locals))
                {
                    sb.Append(",\"");
                    sb.Append(lb.Name);
                    sb.Append('"');
                }
                sb.AppendLine();
            }

            sb.Append(INDENT);
            sb.Append(".FUNCT ");
            sb.Append(name);

            foreach (var lb in requiredParams)
            {
                sb.Append(',');
                sb.Append(lb.Name);
            }

            foreach (var lb in optionalParams.Concat(locals))
            {
                sb.Append(',');
                sb.Append(lb.Name);

                if (game.zversion < 5 && lb.DefaultValue != null)
                {
                    sb.Append('=');
                    sb.Append(lb.DefaultValue);
                }
            }

            game.WriteOutput(sb.ToString());

            if (entryPoint && game.zversion != 6)
            {
                game.WriteOutput("START::");
            }

            // write preamble
            var preamble = new PeepholeBuffer<ZapCode>();
            preamble.MarkLabel(RoutineStart);

            // write values for optional params and locals for V5+
            if (game.zversion >= 5)
            {
                foreach (var lb in optionalParams)
                {
                    var defaultValue = lb.DefaultValue;

                    if (defaultValue != null)
                    {
                        var nextLabel = DefineLabel();

                        preamble.AddLine(
                            new ZapCode { Instruction = new Instruction("ASSIGNED?", new QuoteExpr(lb.ToAsmExpr())) },
                            nextLabel,
                            PeepholeLineType.BranchPositive);
                        preamble.AddLine(
                            new ZapCode { Instruction = new Instruction("SET", new QuoteExpr(lb.ToAsmExpr()), defaultValue.ToAsmExpr()) },
                            null,
                            PeepholeLineType.Plain);
                        preamble.MarkLabel(nextLabel);
                    }
                }

                foreach (var lb in locals)
                {
                    var defaultValue = lb.DefaultValue;

                    if (defaultValue != null)
                    {
                        preamble.AddLine(
                            new ZapCode { Instruction = new Instruction("SET", new QuoteExpr(lb.ToAsmExpr()), defaultValue.ToAsmExpr()) },
                            null,
                            PeepholeLineType.Plain);
                    }
                }
            }

            peep.InsertBufferFirst(preamble);

            // write routine body
            peep.Finish((label, code, dest, type) =>
            {
                if (code.DebugText != null)
                    game.WriteOutput(INDENT + code.DebugText);

                switch (type)
                {
                    case PeepholeLineType.BranchAlways when dest == RTRUE:
                        game.WriteOutput(INDENT + "RTRUE");
                        return;
                    case PeepholeLineType.BranchAlways when dest == RFALSE:
                        game.WriteOutput(INDENT + "RFALSE");
                        return;
                }

                if (code.Instruction.Name == "CRLF+RTRUE")
                {
                    var labelPrefix = label == null ? "" : label + ":";
                    game.WriteOutput($"{labelPrefix}{INDENT}CRLF");

                    game.WriteOutput(INDENT + "RTRUE");
                    return;
                }

                sb.Length = 0;
                if (label != null)
                {
                    sb.Append(label);
                    sb.Append(':');
                }
                sb.Append(INDENT);
                sb.Append(code.Instruction);    // includes operands and store target

                switch (type)
                {
                    case PeepholeLineType.BranchAlways:
                        sb.Append(' ');
                        sb.Append(dest);
                        break;
                    case PeepholeLineType.BranchPositive:
                        sb.Append(" /");
                        sb.Append(dest);
                        break;
                    case PeepholeLineType.BranchNegative:
                        sb.Append(" \\");
                        sb.Append(dest);
                        break;
                }

                game.WriteOutput(sb.ToString());

                if (game.abbrevs != null && code.Instruction.HasStringOperand(out var str))
                    game.abbrevs.AddText(str);
            });

#if DEBUG
            game.RecordPeepholeStats(peep.GetOptimizationStats());
#endif

            if (game.debug != null)
                game.WriteOutput(
                    INDENT +
                    $".DEBUG-ROUTINE-END {game.debug.GetFileNumber(defnEnd.File)},{defnEnd.Line},{defnEnd.Column}");
        }

        class PeepholeCombiner : IPeepholeCombiner<ZapCode>, IPeepholeCombinerWithStats
        {
            private readonly GameBuilder gameBuilder;
            private readonly CombinerOptimizationDescriptor[] optimizationPipeline;
#if DEBUG
            private readonly OptimizationStats[] optimizationStats;
#endif

            private IEnumerator<CombinableLine<ZapCode>>? enumerator;
            private List<CombinableLine<ZapCode>>? matches;

            private readonly record struct CombinerOptimizationDescriptor(string Name, CombinerOptimization Step);

            private delegate bool CombinerOptimization(IEnumerable<CombinableLine<ZapCode>> lines, out CombinerResult<ZapCode> result);

#if DEBUG
            private class OptimizationStats
            {
                public OptimizationStats(string name) => Name = name;

                public string Name { get; }
                public int Applications { get; private set; }
                public int InstructionsSaved { get; private set; }

                public void Record(int instructionsSaved)
                {
                    Applications++;
                    InstructionsSaved += instructionsSaved;
                }

                public PeepholeOptimizationStat Snapshot() => new(Name, Applications, InstructionsSaved);
            }
#endif

            public PeepholeCombiner(GameBuilder gameBuilder)
            {
                this.gameBuilder = gameBuilder;
                optimizationPipeline = BuildOptimizationPipeline();
#if DEBUG
                optimizationStats = optimizationPipeline.Select(d => new OptimizationStats(d.Name)).ToArray();
#endif
            }

            void BeginMatch(IEnumerable<CombinableLine<ZapCode>> lines)
            {
                enumerator = lines.GetEnumerator();
                matches = new List<CombinableLine<ZapCode>>();
            }

            bool Match(params Predicate<CombinableLine<ZapCode>>[] criteria)
            {
                Debug.Assert(matches != null && enumerator != null);
                while (matches.Count < criteria.Length)
                {
                    if (!enumerator.MoveNext())
                        return false;

                    matches.Add(enumerator.Current);
                }

                return criteria.Zip(matches, (c, m) => c(m)).All(ok => ok);
            }

            void EndMatch()
            {
                enumerator?.Dispose();
                enumerator = null;

                matches = null;
            }

            CombinerResult<ZapCode> Combine1To1(Instruction newInstruction, PeepholeLineType? type = null, ILabel? target = null)
            {
                return new CombinerResult<ZapCode>(
                    1,
                    new[] {
                        new CombinableLine<ZapCode>(
                            matches![0].Label,
                            new ZapCode {
                                Instruction = newInstruction,
                                DebugText = matches[0].Code.DebugText
                            },
                            target ?? matches[0].Target,
                            type ?? matches[0].Type)
                    });
            }

            CombinerResult<ZapCode> Combine2To1(Instruction newInstruction, PeepholeLineType? type = null, ILabel? target = null)
            {
                return new CombinerResult<ZapCode>(
                    2,
                    new[] {
                        new CombinableLine<ZapCode>(
                            matches![0].Label,
                            new ZapCode {
                                Instruction = newInstruction,
                                DebugText = MergeDebugText(matches[0].Code.DebugText, matches[1].Code.DebugText),
                            },
                            target ?? matches[1].Target,
                            type ?? matches[1].Type)
                    });
            }

            CombinerResult<ZapCode> Combine2To2(
                Instruction newInstruction1, Instruction newInstruction2,
                PeepholeLineType? type1 = null, PeepholeLineType? type2 = null,
                ILabel? target1 = null, ILabel? target2 = null)
            {
                return new CombinerResult<ZapCode>(
                    2,
                    new[] {
                        new CombinableLine<ZapCode>(
                            matches![0].Label,
                            new ZapCode {
                                Instruction = newInstruction1,
                                DebugText = matches[0].Code.DebugText
                            },
                            target1 ?? matches[0].Target,
                            type1 ?? matches[0].Type),
                        new CombinableLine<ZapCode>(
                            matches[1].Label,
                            new ZapCode {
                                Instruction = newInstruction2,
                                DebugText = matches[1].Code.DebugText
                            },
                            target2 ?? matches[1].Target,
                            type2 ?? matches[1].Type)
                    });
            }

            static CombinerResult<ZapCode> Consume(int numberOfLines)
            {
                return new CombinerResult<ZapCode>(numberOfLines, Enumerable.Empty<CombinableLine<ZapCode>>());
            }

            static bool IsEqualZero(Instruction inst,
                [NotNullWhen(true)] out AsmExpr? otherSide)
            {
                if (inst.Name == "EQUAL?" && inst.Operands.Count == 2)
                {
                    switch (inst.Operands[0], inst.Operands[1])
                    {
                        case (NumericLiteral { Value: 0 }, var rhs):
                            otherSide = rhs;
                            return true;

                        case (var lhs, NumericLiteral { Value: 0 }):
                            otherSide = lhs;
                            return true;
                    }
                }

                otherSide = null;
                return false;
            }

            static bool IsSetOfKnownNonzero(Instruction instruction, out AsmExpr? destination)
            {
                destination = null;

                if (instruction.Name != "SET" || instruction.Operands.Count != 2)
                    return false;

                if (instruction.Operands[0] is not QuoteExpr quote)
                    return false;

                if (!IsKnownNonzeroValue(instruction.Operands[1]))
                    return false;

                destination = quote.Inner;
                return true;
            }

            static bool IsZeroTestOfOperand(Instruction instruction, AsmExpr operand)
            {
                return instruction.Name == "ZERO?" &&
                       instruction.Operands.Count == 1 &&
                       instruction.Operands[0].Equals(operand);
            }
            static bool IsIncOrDecOfVariable(Instruction instruction, string opcode,
                [NotNullWhen(true)] out AsmExpr? variable)
            {
                if (instruction.Name == opcode &&
                    instruction.Operands.Count == 1 &&
                    instruction.Operands[0].IsQuote(out variable))
                {
                    return true;
                }

                variable = null;
                return false;
            }

            static bool IsAddStackOne(Instruction instruction, [NotNullWhen(true)] out string? destination)
            {
                if (instruction.Name == "ADD" &&
                    instruction.Operands.Count == 2 &&
                    instruction.StoreTarget != null &&
                    ((instruction.Operands[0].IsStack() && instruction.Operands[1] is NumericLiteral { Value: 1 }) ||
                     (instruction.Operands[1].IsStack() && instruction.Operands[0] is NumericLiteral { Value: 1 })))
                {
                    destination = instruction.StoreTarget;
                    return true;
                }

                destination = null;
                return false;
            }

            static bool IsSubStackOne(Instruction instruction, [NotNullWhen(true)] out string? destination)
            {
                if (instruction.Name == "SUB" &&
                    instruction.Operands.Count == 2 &&
                    instruction.StoreTarget != null &&
                    instruction.Operands[0].IsStack() &&
                    instruction.Operands[1] is NumericLiteral { Value: 1 })
                {
                    destination = instruction.StoreTarget;
                    return true;
                }

                destination = null;
                return false;
            }

            static bool IsKnownNonzeroValue(AsmExpr expr)
            {
                return expr switch
                {
                    NumericLiteral literal => literal.Value != 0,
                    _ => AsmExprFacts.IsKnownNonzero(expr),
                };
            }

            private bool TryGetNumericValue(AsmExpr expr, out int value)
            {
                switch (expr)
                {
                    case NumericLiteral literal:
                        value = literal.Value;
                        return true;
                    case SymbolExpr symbol when gameBuilder.TryGetNumericConstantValue(symbol.Text, out var constant):
                        value = constant;
                        return true;
                    default:
                        value = default;
                        return false;
                }
            }

            bool IsBANDConstantWithStack(Instruction inst,
                out int constant, [NotNullWhen(true)] out string? dest) =>
                IsCommutativeConstantWithStack("BAND", inst, out constant, out dest);

            bool IsBORConstantWithStack(Instruction inst,
                out int constant, [NotNullWhen(true)] out string? dest) =>
                IsCommutativeConstantWithStack("BOR", inst, out constant, out dest);

            bool IsCommutativeConstantWithStack(
                string instructionName, Instruction inst,
                out int constant, [NotNullWhen(true)] out string? dest)
            {
                if (inst.Name == instructionName && inst.Operands.Count == 2)
                {
                    Debug.Assert(inst.StoreTarget != null);

                    switch (inst.Operands[0], inst.Operands[1])
                    {
                        case var tuple when TryGetNumericValue(tuple.Item1, out var firstValue) && tuple.Item2.IsStack():
                            constant = firstValue;
                            dest = inst.StoreTarget;
                            return true;

                        case var tuple when tuple.Item1.IsStack() && TryGetNumericValue(tuple.Item2, out var secondValue):
                            constant = secondValue;
                            dest = inst.StoreTarget;
                            return true;
                    }
                }

                constant = default;
                dest = null;
                return false;
            }

            bool IsBANDConstantToStack(Instruction inst,
                [NotNullWhen(true)] out AsmExpr? variable,
                out int constant) =>
                IsCommutativeConstantToStack("BAND", inst, out variable, out constant);

            bool IsBORConstantToStack(Instruction inst,
                [NotNullWhen(true)] out AsmExpr? variable,
                out int constant) =>
                IsCommutativeConstantToStack("BOR", inst, out variable, out constant);

            bool IsCommutativeConstantToStack(
                string instructionName, Instruction inst,
                [NotNullWhen(true)] out AsmExpr? variable, out int constant)
            {
                if (inst.Name == instructionName && inst.Operands.Count == 2 && inst.StoreTarget == "STACK")
                {
                    switch (inst.Operands[0], inst.Operands[1])
                    {
                        case var tuple when TryGetNumericValue(tuple.Item1, out var firstValue):
                            variable = tuple.Item2;
                            constant = firstValue;
                            return true;

                        case var tuple when TryGetNumericValue(tuple.Item2, out var secondValue):
                            variable = tuple.Item1;
                            constant = secondValue;
                            return true;
                    }
                }

                variable = null;
                constant = default;
                return false;
            }

            static bool IsPopToVariable(Instruction inst,
                [NotNullWhen(true)] out string? dest)
            {
                switch (inst.Name)
                {
                    case "POP" when inst.Operands.Count == 1 && inst.Operands[0] is QuoteExpr quote:
                        dest = quote.Inner.ToString();
                        return true;

                    case "POP" when inst.Operands.Count == 0 && inst.StoreTarget != null:
                        dest = inst.StoreTarget;
                        return true;

                    default:
                        dest = null;
                        return false;
                }
            }

            /// <inheritdoc />
            public CombinerResult<ZapCode> Apply(IEnumerable<CombinableLine<ZapCode>> lines)
            {
                for (int i = 0; i < optimizationPipeline.Length; i++)
                {
                    if (!optimizationPipeline[i].Step(lines, out var result))
                        continue;

#if DEBUG
                    var newLineCount = CountNewLines(result.NewLines);
                    var instructionsSaved = result.LinesConsumed - newLineCount;
                    optimizationStats[i].Record(instructionsSaved);
#endif
                    return result;
                }

                return new CombinerResult<ZapCode>();
            }

            private static int CountNewLines(IEnumerable<CombinableLine<ZapCode>> newLines)
            {
                if (newLines is ICollection<CombinableLine<ZapCode>> collection)
                    return collection.Count;

                var count = 0;
                using var enumerator = newLines.GetEnumerator();
                while (enumerator.MoveNext())
                    count++;

                return count;
            }

            // these names are lowercase to avoid confusing the codegen tests
            // that check for specific instructions
            private CombinerOptimizationDescriptor[] BuildOptimizationPipeline() =>
            [
                new("simplify zero test", TrySimplifyEqualZero),
                new("fold known nonzero branch", TryFoldKnownNonzeroBranch),
                new("remove redundant zero? after set", TryRemoveZeroAfterNonzeroSet),
                new("rewrite jump to boolean", TryRewriteJumpToBoolean),
                new("fold push/rstack pair", TrySimplifyPushRStack),
                new("eliminate stack pop pair", TryEliminateStackPopPair),
                new("replace push+pop with set", TryReplacePushPopWithSet),
                new("substitute pushed value", TrySubstitutePushedValue),
                new("forward stack result through inc/dec arithmetic", TryForwardStackResultThroughIncDecArithmetic),
                new("eliminate inc/dec pair", TryEliminateIncDecPair),
                new("rewrite stack inc/dec arithmetic to pop", TryRewriteStackIncDecArithmeticToPop),
                new("fold inc branch", TryFoldIncBranch),
                new("fold dec branch", TryFoldDecBranch),
                new("merge equal tests", TryMergeEqualTests),
                new("combine crlf+rtrue", TryCombineCrlfRtrue),
                new("upgrade printi", TryUpgradePrintI),
                new("simplify band zero branch", TrySimplifyBandZeroBranch),
                new("merge band constants", TryMergeBandConstants),
                new("merge bor constants", TryMergeBorConstants),
            ];

            bool TrySimplifyEqualZero(IEnumerable<CombinableLine<ZapCode>> lines, out CombinerResult<ZapCode> result)
            {
                BeginMatch(lines);
                try
                {
                    AsmExpr? expr = null;

                    if (Match(a => IsEqualZero(a.Code.Instruction, out expr)))
                    {
                        result = Combine1To1(new Instruction("ZERO?", expr!));
                        return true;
                    }

                    result = default;
                    return false;
                }
                finally
                {
                    EndMatch();
                }
            }

            bool TryRemoveZeroAfterNonzeroSet(IEnumerable<CombinableLine<ZapCode>> lines, out CombinerResult<ZapCode> result)
            {
                BeginMatch(lines);
                try
                {
                    AsmExpr? destination = null;

                    if (Match(
                        a => IsSetOfKnownNonzero(a.Code.Instruction, out destination),
                        b => destination != null && IsZeroTestOfOperand(b.Code.Instruction, destination)))
                    {
                        var zeroLine = matches![1];

                        if (zeroLine.Type == PeepholeLineType.BranchNegative && zeroLine.Target != null)
                        {
                            result = Combine2To2(
                                matches[0].Code.Instruction,
                                new Instruction("JUMP"),
                                matches[0].Type,
                                PeepholeLineType.BranchAlways,
                                matches[0].Target,
                                zeroLine.Target);
                        }
                        else
                        {
                            result = Combine2To1(
                                matches[0].Code.Instruction,
                                matches[0].Type,
                                matches[0].Target);
                        }

                        return true;
                    }

                    result = default;
                    return false;
                }
                finally
                {
                    EndMatch();
                }
            }

            bool TryFoldKnownNonzeroBranch(IEnumerable<CombinableLine<ZapCode>> lines,
                out CombinerResult<ZapCode> result)
            {
                BeginMatch(lines);
                try
                {
                    if (Match(line => line.Code.Instruction.Name == "ZERO?" &&
                        line.Code.Instruction.Operands.Count == 1 &&
                        IsKnownNonzeroValue(line.Code.Instruction.Operands[0]) &&
                        line.Type == PeepholeLineType.BranchNegative && line.Target != null))
                    {
                        result = Combine1To1(new Instruction("JUMP"), PeepholeLineType.BranchAlways,
                            matches![0].Target);
                        return true;
                    }

                    result = default;
                    return false;
                }
                finally
                {
                    EndMatch();
                }
            }

            bool TryRewriteJumpToBoolean(IEnumerable<CombinableLine<ZapCode>> lines, out CombinerResult<ZapCode> result)
            {
                BeginMatch(lines);
                try
                {
                    if (Match(a => a.Code.Instruction.Name == "JUMP" && (a.Target == RTRUE || a.Target == RFALSE)))
                    {
                        var name = matches![0].Target == RTRUE ? "RTRUE" : "RFALSE";
                        result = Combine1To1(new Instruction(name));
                        return true;
                    }

                    result = default;
                    return false;
                }
                finally
                {
                    EndMatch();
                }
            }

            bool TrySimplifyPushRStack(IEnumerable<CombinableLine<ZapCode>> lines, out CombinerResult<ZapCode> result)
            {
                BeginMatch(lines);
                try
                {
                    if (Match(a => a.Code.Instruction.Name == "PUSH" && a.Code.Instruction.Operands.Count == 1,
                        b => b.Code.Instruction.Name == "RSTACK"))
                    {
                        result = matches![0].Code.Instruction.Operands[0] switch
                        {
                            NumericLiteral { Value: 0 } => Combine2To1(
                                new Instruction("RFALSE"),
                                PeepholeLineType.BranchAlways,
                                RFALSE),
                            NumericLiteral { Value: 1 } => Combine2To1(
                                new Instruction("RTRUE"),
                                PeepholeLineType.BranchAlways,
                                RTRUE),
                            _ => Combine2To1(matches[0].Code.Instruction.WithName("RETURN"))
                        };
                        return true;
                    }

                    result = default;
                    return false;
                }
                finally
                {
                    EndMatch();
                }
            }

            bool TryEliminateStackPopPair(IEnumerable<CombinableLine<ZapCode>> lines, out CombinerResult<ZapCode> result)
            {
                BeginMatch(lines);
                try
                {
                    string? dest = null;

                    if (Match(a => a.Code.Instruction.StoreTarget == "STACK",
                        b => IsPopToVariable(b.Code.Instruction, out dest)))
                    {
                        result = Combine2To1(matches![0].Code.Instruction.WithStoreTarget(dest));
                        return true;
                    }

                    result = default;
                    return false;
                }
                finally
                {
                    EndMatch();
                }
            }

            bool TryReplacePushPopWithSet(IEnumerable<CombinableLine<ZapCode>> lines, out CombinerResult<ZapCode> result)
            {
                BeginMatch(lines);
                try
                {
                    string? dest = null;

                    if (Match(a => a.Code.Instruction.Name == "PUSH",
                        b => IsPopToVariable(b.Code.Instruction, out dest)))
                    {
                        result = Combine2To1(new Instruction(
                            "SET",
                            new QuoteExpr(new SymbolExpr(dest!)),
                            matches![0].Code.Instruction.Operands[0]));
                        return true;
                    }

                    result = default;
                    return false;
                }
                finally
                {
                    EndMatch();
                }
            }

            bool TrySubstitutePushedValue(IEnumerable<CombinableLine<ZapCode>> lines, out CombinerResult<ZapCode> result)
            {
                BeginMatch(lines);
                try
                {
                    if (Match(a => a.Code.Instruction.Name == "PUSH" && !a.Code.Instruction.Operands[0].IsStack(),
                        b => b.Code.Instruction.Operands.Any(o => o.IsStack())))
                    {
                        var operands = matches![1].Code.Instruction.Operands.ToArray();

                        for (var i = 0; i < operands.Length; i++)
                        {
                            if (!operands[i].IsStack())
                                continue;

                            operands[i] = matches[0].Code.Instruction.Operands[0];

                            var instruction = new Instruction(matches[1].Code.Instruction.Name, operands)
                            {
                                StoreTarget = matches[1].Code.Instruction.StoreTarget,
                                BranchPolarity = matches[1].Code.Instruction.BranchPolarity,
                                BranchTarget = matches[1].Code.Instruction.BranchTarget,
                            };

                            result = Combine2To1(instruction);
                            return true;
                        }
                    }

                    result = default;
                    return false;
                }
                finally
                {
                    EndMatch();
                }
            }

            bool TryEliminateIncDecPair(IEnumerable<CombinableLine<ZapCode>> lines, out CombinerResult<ZapCode> result)
            {
                BeginMatch(lines);
                try
                {
                    AsmExpr? variable = null;

                    if (Match(
                        a => IsIncOrDecOfVariable(a.Code.Instruction, "INC", out variable),
                        b => IsIncOrDecOfVariable(b.Code.Instruction, "DEC", out var secondVariable) &&
                             variable != null && variable.Equals(secondVariable)))
                    {
                        result = Consume(2);
                        return true;
                    }

                    variable = null;

                    if (Match(
                        a => IsIncOrDecOfVariable(a.Code.Instruction, "DEC", out variable),
                        b => IsIncOrDecOfVariable(b.Code.Instruction, "INC", out var secondVariable) &&
                             variable != null && variable.Equals(secondVariable)))
                    {
                        result = Consume(2);
                        return true;
                    }

                    result = default;
                    return false;
                }
                finally
                {
                    EndMatch();
                }
            }

            bool TryForwardStackResultThroughIncDecArithmetic(IEnumerable<CombinableLine<ZapCode>> lines,
                out CombinerResult<ZapCode> result)
            {
                BeginMatch(lines);
                try
                {
                    string? destination = null;

                    if (Match(
                        a => a.Code.Instruction.StoreTarget == "STACK",
                        b => IsAddStackOne(b.Code.Instruction, out destination)))
                    {
                        result = Combine2To2(
                            matches![0].Code.Instruction.WithStoreTarget(destination),
                            new Instruction("INC", new QuoteExpr(new SymbolExpr(destination!))));
                        return true;
                    }

                    destination = null;

                    if (Match(
                        a => a.Code.Instruction.StoreTarget == "STACK",
                        b => IsSubStackOne(b.Code.Instruction, out destination)))
                    {
                        result = Combine2To2(
                            matches![0].Code.Instruction.WithStoreTarget(destination),
                            new Instruction("DEC", new QuoteExpr(new SymbolExpr(destination!))));
                        return true;
                    }

                    result = default;
                    return false;
                }
                finally
                {
                    EndMatch();
                }
            }

            Instruction BuildPopInstruction(string destination)
            {
                return gameBuilder.zversion == 6
                    ? new Instruction("POP") { StoreTarget = destination }
                    : new Instruction("POP", new QuoteExpr(new SymbolExpr(destination)));
            }

            bool TryRewriteStackIncDecArithmeticToPop(IEnumerable<CombinableLine<ZapCode>> lines,
                out CombinerResult<ZapCode> result)
            {
                BeginMatch(lines);
                try
                {
                    AsmExpr? variable = null;
                    string? destination = null;

                    if (Match(
                        a => IsIncOrDecOfVariable(a.Code.Instruction, "DEC", out variable) && variable.IsStack(),
                        b => IsAddStackOne(b.Code.Instruction, out destination)))
                    {
                        result = Combine2To1(BuildPopInstruction(destination!));
                        return true;
                    }

                    variable = null;
                    destination = null;

                    if (Match(
                        a => IsIncOrDecOfVariable(a.Code.Instruction, "INC", out variable) && variable.IsStack(),
                        b => IsSubStackOne(b.Code.Instruction, out destination)))
                    {
                        result = Combine2To1(BuildPopInstruction(destination!));
                        return true;
                    }

                    result = default;
                    return false;
                }
                finally
                {
                    EndMatch();
                }
            }

            bool TryFoldIncBranch(IEnumerable<CombinableLine<ZapCode>> lines, out CombinerResult<ZapCode> result)
            {
                BeginMatch(lines);
                try
                {
                    AsmExpr? expr = null;

                    if (Match(
                        a => a.Code.Instruction.Name == "INC" && a.Code.Instruction.Operands[0].IsQuote(out expr) &&
                             !expr.IsStack(),
                        b => b.Code.Instruction.Name == "GRTR?" && b.Code.Instruction.Operands[0].Equals(expr!)))
                    {
                        result = Combine2To1(new Instruction(
                            "IGRTR?",
                            matches![0].Code.Instruction.Operands[0],
                            matches[1].Code.Instruction.Operands[1]));
                        return true;
                    }

                    result = default;
                    return false;
                }
                finally
                {
                    EndMatch();
                }
            }

            bool TryFoldDecBranch(IEnumerable<CombinableLine<ZapCode>> lines, out CombinerResult<ZapCode> result)
            {
                BeginMatch(lines);
                try
                {
                    AsmExpr? expr = null;

                    if (Match(
                        a => a.Code.Instruction.Name == "DEC" && a.Code.Instruction.Operands[0].IsQuote(out expr) &&
                             !expr.IsStack(),
                        b => b.Code.Instruction.Name == "LESS?" && b.Code.Instruction.Operands[0].Equals(expr!)))
                    {
                        result = Combine2To1(new Instruction(
                            "DLESS?",
                            matches![0].Code.Instruction.Operands[0],
                            matches[1].Code.Instruction.Operands[1]));
                        return true;
                    }

                    result = default;
                    return false;
                }
                finally
                {
                    EndMatch();
                }
            }

            bool TryMergeEqualTests(IEnumerable<CombinableLine<ZapCode>> lines, out CombinerResult<ZapCode> result)
            {
                BeginMatch(lines);
                try
                {
                    if (Match(
                        a => (a.Code.Instruction.Name == "EQUAL?" || a.Code.Instruction.Name == "ZERO?") &&
                             a.Type == PeepholeLineType.BranchPositive,
                        b => (b.Code.Instruction.Name == "EQUAL?" || b.Code.Instruction.Name == "ZERO?") &&
                             b.Type == PeepholeLineType.BranchPositive))
                    {
                        if (matches![0].Target == matches[1].Target)
                        {
                            static IList<AsmExpr> GetParts(Instruction inst) =>
                                inst.Name == "ZERO?"
                                    ? new[] { inst.Operands[0], new NumericLiteral(0) }
                                    : inst.Operands;

                            var aParts = GetParts(matches[0].Code.Instruction);
                            var bParts = GetParts(matches[1].Code.Instruction);

                            if (aParts[0].Equals(bParts[0]) && aParts.Count < 4)
                            {
                                if (aParts.Count + bParts.Count <= 5)
                                {
                                    result = Combine2To1(new Instruction("EQUAL?", aParts.Concat(bParts.Skip(1))));
                                    return true;
                                }

                                var allRhs = aParts.Skip(1).Concat(bParts.Skip(1)).ToArray();

                                var first = new Instruction("EQUAL?",
                                    Enumerable.Repeat(aParts[0], 1).Concat(allRhs.Take(3)));

                                var second = new Instruction("EQUAL?",
                                    Enumerable.Repeat(aParts[0], 1).Concat(allRhs.Skip(3)));

                                result = Combine2To2(first, second);
                                return true;
                            }
                        }
                    }

                    result = default;
                    return false;
                }
                finally
                {
                    EndMatch();
                }
            }

            bool TryCombineCrlfRtrue(IEnumerable<CombinableLine<ZapCode>> lines, out CombinerResult<ZapCode> result)
            {
                BeginMatch(lines);
                try
                {
                    if (Match(a => a.Code.Instruction.Name == "CRLF",
                        b => b.Code.Instruction.Name == "RTRUE"))
                    {
                        result = Combine2To1(new Instruction("CRLF+RTRUE"), PeepholeLineType.Terminator);
                        return true;
                    }

                    result = default;
                    return false;
                }
                finally
                {
                    EndMatch();
                }
            }

            bool TryUpgradePrintI(IEnumerable<CombinableLine<ZapCode>> lines, out CombinerResult<ZapCode> result)
            {
                BeginMatch(lines);
                try
                {
                    if (Match(a => a.Code.Instruction.Name == "PRINTI",
                        b => b.Code.Instruction.Name == "CRLF+RTRUE"))
                    {
                        result = Combine2To1(matches![0].Code.Instruction.WithName("PRINTR"), PeepholeLineType.HeavyTerminator);
                        return true;
                    }

                    result = default;
                    return false;
                }
                finally
                {
                    EndMatch();
                }
            }

            bool TrySimplifyBandZeroBranch(IEnumerable<CombinableLine<ZapCode>> lines, out CombinerResult<ZapCode> result)
            {
                BeginMatch(lines);
                try
                {
                    AsmExpr? expr = null;
                    var constant = 0;

                    if (Match(a => IsBANDConstantToStack(a.Code.Instruction, out expr, out constant),
                        b => b.Code.Instruction.Name == "ZERO?" && b.Code.Instruction.Operands[0].IsStack()))
                    {
                        var value = constant;

                        if (value == 0)
                        {
                            if (!expr!.IsStack())
                            {
                                result = matches![1].Type == PeepholeLineType.BranchPositive
                                    ? Combine2To1(new Instruction("JUMP"), PeepholeLineType.BranchAlways, matches[1].Target)
                                    : Consume(2);
                                return true;
                            }
                        }
                        else if ((value & (value - 1)) == 0)
                        {
                            // powers of two
                            var opposite = matches![1].Type == PeepholeLineType.BranchPositive
                                ? PeepholeLineType.BranchNegative
                                : PeepholeLineType.BranchPositive;

                            result = Combine2To1(new Instruction("BTST", expr!, new NumericLiteral(value)), opposite);
                            return true;
                        }
                    }

                    result = default;
                    return false;
                }
                finally
                {
                    EndMatch();
                }
            }

            bool TryMergeBandConstants(IEnumerable<CombinableLine<ZapCode>> lines, out CombinerResult<ZapCode> result)
            {
                BeginMatch(lines);
                try
                {
                    AsmExpr? expr = null;
                    var leftConst = 0;
                    var rightConst = 0;
                    string? dest = null;

                    if (Match(a => IsBANDConstantToStack(a.Code.Instruction, out expr, out leftConst),
                        b => IsBANDConstantWithStack(b.Code.Instruction, out rightConst, out dest)))
                    {
                        var combined = leftConst & rightConst;
                        result = Combine2To1(new Instruction("BAND", expr!, new NumericLiteral(combined))
                        {
                            StoreTarget = dest
                        });
                        return true;
                    }

                    result = default;
                    return false;
                }
                finally
                {
                    EndMatch();
                }
            }

            bool TryMergeBorConstants(IEnumerable<CombinableLine<ZapCode>> lines, out CombinerResult<ZapCode> result)
            {
                BeginMatch(lines);
                try
                {
                    AsmExpr? expr = null;
                    var leftConst = 0;
                    var rightConst = 0;
                    string? dest = null;

                    if (Match(a => IsBORConstantToStack(a.Code.Instruction, out expr, out leftConst),
                        b => IsBORConstantWithStack(b.Code.Instruction, out rightConst, out dest)))
                    {
                        var combined = leftConst | rightConst;
                        result = Combine2To1(new Instruction("BOR", expr!, new NumericLiteral(combined))
                        {
                            StoreTarget = dest
                        });
                        return true;
                    }

                    result = default;
                    return false;
                }
                finally
                {
                    EndMatch();
                }
            }

            public IEnumerable<PeepholeOptimizationStat> GetOptimizationStats()
            {
#if DEBUG
                return optimizationStats.Select(static s => s.Snapshot());
#else
                return Enumerable.Empty<PeepholeOptimizationStat>();
#endif
            }

            public ZapCode SynthesizeBranchAlways()
            {
                return new ZapCode { Instruction = new Instruction("JUMP") };
            }

            private static string? MergeDebugText(string? text1, string? text2)
            {
                return
                    text1 == null ? text2
                    : text2 == null ? text1
                    : text1 == text2 ? text1
                    : $"{text1}{Environment.NewLine}{INDENT}{text2}";
            }

            public bool AreIdentical(ZapCode a, ZapCode b) => a.Instruction.Equals(b.Instruction);

            public ZapCode MergeIdentical(ZapCode a, ZapCode b)
            {
                return new ZapCode
                {
                    Instruction = a.Instruction,
                    DebugText = MergeDebugText(a.DebugText, b.DebugText),
                };
            }

            public bool CanDuplicate(ZapCode c)
            {
                // don't duplicate instructions with debug info attached
                return c.DebugText == null;
            }

            public SameTestResult AreSameTest(ZapCode a, ZapCode b)
            {
                // if the stack is involved, all bets are off
                if (a.Instruction.Operands.Concat(b.Instruction.Operands).Any(o => o.IsStack()))
                    return SameTestResult.Unrelated;

                // if the instructions are identical, they must be the same test
                if (a.Instruction.Equals(b.Instruction))
                    return SameTestResult.SameTest;

                /* otherwise, they can be related if 'a' is a store+branch instruction
                 * and 'b' is ZERO? testing the result stored by 'a'. the z-machine's
                 * store+branch instructions all branch upon storing a nonzero value,
                 * so we always return OppositeTest in this case. */
                if (b.Instruction.Name == "ZERO?" &&
                    a.Instruction.StoreTarget == b.Instruction.Operands[0].ToString())
                    return SameTestResult.OppositeTest;

                return SameTestResult.Unrelated;
            }

            public ControlsConditionResult ControlsConditionalBranch(ZapCode a, ZapCode b)
            {
                /* if 'a' pushes a constant and 'b' is ZERO? testing the stack, the
                 * answer depends on the value of the constant. */
                if (a.Instruction.Name == "PUSH" &&
                    a.Instruction.Operands[0] is NumericLiteral num &&
                    b.Instruction.Name == "ZERO?" &&
                    b.Instruction.Operands[0].IsStack())
                {
                    return num.Value == 0
                        ? ControlsConditionResult.CausesBranchIfPositive
                        : ControlsConditionResult.CausesNoOpIfPositive;
                }

                return ControlsConditionResult.Unrelated;
            }

            public ControlsConditionResult DeterminesConditionOutcome(ZapCode a, ZapCode b)
            {
                /* If 'a' is SET 'VAR,constant and 'b' is ZERO? VAR, the answer depends on
                 * the constant value. Unlike ControlsConditionalBranch, the SET has a
                 * side effect that must be preserved. */
                if (a.Instruction.Name == "SET" &&
                    a.Instruction.Operands.Count == 2 &&
                    a.Instruction.Operands[0] is QuoteExpr quote &&
                    b.Instruction.Name == "ZERO?" &&
                    b.Instruction.Operands.Count == 1 &&
                    quote.Inner.Equals(b.Instruction.Operands[0]))
                {
                    // Check if the value being stored is a known constant
                    if (TryGetNumericValue(a.Instruction.Operands[1], out var value))
                    {
                        return value == 0
                            ? ControlsConditionResult.CausesBranchIfPositive
                            : ControlsConditionResult.CausesNoOpIfPositive;
                    }

                    // Also handle known nonzero values (like string constants, routines, etc.)
                    if (IsKnownNonzeroValue(a.Instruction.Operands[1]))
                    {
                        return ControlsConditionResult.CausesNoOpIfPositive;
                    }
                }

                return ControlsConditionResult.Unrelated;
            }

        }
    }
}
