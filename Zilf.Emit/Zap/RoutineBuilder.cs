/* Copyright 2010-2018 Jesse McGrew
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
using System.Linq;
using System.Text;
using JetBrains.Annotations;
using Zapf.Parsing.Expressions;
using Zapf.Parsing.Instructions;
using Zilf.Common;

namespace Zilf.Emit.Zap
{
    class RoutineBuilder : ConstantOperandBase, IRoutineBuilder
    {
        internal static readonly Label RTRUE = new Label("TRUE");
        internal static readonly Label RFALSE = new Label("FALSE");
        internal static readonly VariableOperand STACK = new VariableOperand("STACK");
        const string INDENT = "\t";

        readonly GameBuilder game;
        readonly string name;
        readonly bool entryPoint;

        internal DebugLineRef defnStart, defnEnd;

        readonly PeepholeBuffer<ZapCode> peep;
        int nextLabelNum;
        string pendingDebugText;

        readonly List<LocalBuilder> requiredParams = new List<LocalBuilder>();
        readonly List<LocalBuilder> optionalParams = new List<LocalBuilder>();
        readonly List<LocalBuilder> locals = new List<LocalBuilder>();

        public RoutineBuilder(GameBuilder game, string name, bool entryPoint, bool cleanStack)
        {
            this.game = game;
            this.name = name;
            this.entryPoint = entryPoint;
            CleanStack = cleanStack;

            peep = new PeepholeBuffer<ZapCode>() { Combiner = new PeepholeCombiner(this) };
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
                throw new InvalidOperationException("Entry point may not have parameters");
            if (LocalExists(paramName))
                throw new ArgumentException("Local variable already exists: " + paramName, nameof(paramName));

            var local = new LocalBuilder(paramName);
            requiredParams.Add(local);
            return local;
        }

        /// <exception cref="InvalidOperationException">This is an entry point routine.</exception>
        /// <exception cref="ArgumentException">A local variable named <paramref name="paramName"/> is already defined.</exception>
        public ILocalBuilder DefineOptionalParameter(string paramName)
        {
            paramName = GameBuilder.SanitizeSymbol(paramName);

            if (entryPoint)
                throw new InvalidOperationException("Entry point may not have parameters");
            if (LocalExists(paramName))
                throw new ArgumentException("Local variable already exists: " + paramName, nameof(paramName));

            var local = new LocalBuilder(paramName);
            optionalParams.Add(local);
            return local;
        }

        /// <exception cref="InvalidOperationException">This is an entry point routine.</exception>
        /// <exception cref="ArgumentException">A local variable named <paramref name="localName"/> is already defined.</exception>
        public ILocalBuilder DefineLocal(string localName)
        {
            localName = GameBuilder.SanitizeSymbol(localName);

            if (entryPoint)
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

        void AddLine(Instruction code, ILabel target, PeepholeLineType type)
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
        public void Branch(Condition cond, IOperand left, IOperand right, ILabel label, bool polarity)
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

            if (leftVar && !(left is IVariable))
                throw new ArgumentException("This condition requires a variable", nameof(left));

            if (nullary)
            {
                if (left != null || right != null)
                    throw new ArgumentException("Expected no operands for nullary condition");
            }
            else if (unary)
            {
                if (right != null)
                    throw new ArgumentException("Expected only one operand for unary condition", nameof(right));
            }
            else
            {
                if (right == null)
                    throw new ArgumentException("Expected two operands for binary condition", nameof(right));
            }

            var instruction = new Instruction(opcode);
            if (unary)
            {
                instruction.Operands.Add(new QuoteExpr(left.ToAsmExpr()));
            }
            else if (!nullary)
            {
                Debug.Assert(left != null);
                var leftExpr = left.ToAsmExpr();
                instruction.Operands.Add(leftVar ? new QuoteExpr(leftExpr) : leftExpr);
                instruction.Operands.Add(right.ToAsmExpr());
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

        public void EmitNullary(NullaryOp op, IVariable result)
        {
            string opcode;

            switch (op)
            {
                case NullaryOp.RestoreUndo:
                    opcode = "IRESTORE";
                    break;
                case NullaryOp.SaveUndo:
                    opcode = "ISAVE";
                    break;
                case NullaryOp.ShowStatus:
                    opcode = "USL";
                    break;
                case NullaryOp.Catch:
                    opcode = "CATCH";
                    break;
                default:
                    throw UnhandledCaseException.FromEnum(op, "nullary operation");
            }

            AddLine(
                new Instruction(opcode) { StoreTarget = result?.ToString() }, 
                null,
                PeepholeLineType.Plain);
        }

        [NotNull]
        static string OptResult([CanBeNull] IVariable result)
        {
            if (result == null)
                return string.Empty;

            return " >" + result;
        }

        public void EmitUnary(UnaryOp op, IOperand value, IVariable result)
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

        public void EmitBinary(BinaryOp op, IOperand left, IOperand right, IVariable result)
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
                case BinaryOp.StoreIndirect when right == Stack && game.zversion != 6:
                    AddLine(new Instruction("POP", left.ToAsmExpr()), null, PeepholeLineType.Plain);
                    return;
            }

            string opcode;

            switch (op)
            {
                case BinaryOp.Add:
                    opcode = "ADD";
                    break;
                case BinaryOp.And:
                    opcode = "BAND";
                    break;
                case BinaryOp.ArtShift:
                    opcode = "ASHIFT";
                    break;
                case BinaryOp.Div:
                    opcode = "DIV";
                    break;
                case BinaryOp.GetByte:
                    opcode = "GETB";
                    break;
                case BinaryOp.GetPropAddress:
                    opcode = "GETPT";
                    break;
                case BinaryOp.GetProperty:
                    opcode = "GETP";
                    break;
                case BinaryOp.GetNextProp:
                    opcode = "NEXTP";
                    break;
                case BinaryOp.GetWord:
                    opcode = "GET";
                    break;
                case BinaryOp.LogShift:
                    opcode = "SHIFT";
                    break;
                case BinaryOp.Mod:
                    opcode = "MOD";
                    break;
                case BinaryOp.Mul:
                    opcode = "MUL";
                    break;
                case BinaryOp.Or:
                    opcode = "BOR";
                    break;
                case BinaryOp.Sub:
                    opcode = "SUB";
                    break;
                case BinaryOp.MoveObject:
                    opcode = "MOVE";
                    break;
                case BinaryOp.SetFlag:
                    opcode = "FSET";
                    break;
                case BinaryOp.ClearFlag:
                    opcode = "FCLEAR";
                    break;
                case BinaryOp.DirectOutput:
                    opcode = "DIROUT";
                    break;
                case BinaryOp.SetCursor:
                    opcode = "CURSET";
                    break;
                case BinaryOp.SetColor:
                    opcode = "COLOR";
                    break;
                case BinaryOp.Throw:
                    opcode = "THROW";
                    break;
                case BinaryOp.StoreIndirect:
                    opcode = "SET";
                    break;
                case BinaryOp.FlushUserStack:
                    opcode = "FSTACK";
                    break;
                case BinaryOp.GetWindowProperty:
                    opcode = "WINGET";
                    break;
                case BinaryOp.ScrollWindow:
                    opcode = "SCROLL";
                    break;
                default:
                    throw UnhandledCaseException.FromEnum(op, "binary operation");
            }

            AddLine(
                new Instruction(opcode, left.ToAsmExpr(), right.ToAsmExpr()) { StoreTarget = result?.ToString() },
                null,
                PeepholeLineType.Plain);
        }

        public void EmitTernary(TernaryOp op, IOperand left, IOperand center, IOperand right, IVariable result)
        {
            string opcode;

            switch (op)
            {
                case TernaryOp.PutByte:
                    opcode = "PUTB";
                    break;
                case TernaryOp.PutProperty:
                    opcode = "PUTP";
                    break;
                case TernaryOp.PutWord:
                    opcode = "PUT";
                    break;
                case TernaryOp.CopyTable:
                    opcode = "COPYT";
                    break;
                case TernaryOp.PutWindowProperty:
                    opcode = "WINPUT";
                    break;
                case TernaryOp.DrawPicture:
                    opcode = "DISPLAY";
                    break;
                case TernaryOp.WindowStyle:
                    opcode = "WINATTR";
                    break;
                case TernaryOp.MoveWindow:
                    opcode = "WINPOS";
                    break;
                case TernaryOp.WindowSize:
                    opcode = "WINSIZE";
                    break;
                case TernaryOp.SetMargins:
                    opcode = "MARGIN";
                    break;
                case TernaryOp.SetCursor:
                    opcode = "CURSET";
                    break;
                case TernaryOp.DirectOutput:
                    opcode = "DIROUT";
                    break;
                case TernaryOp.ErasePicture:
                    opcode = "DCLEAR";
                    break;
                default:
                    throw UnhandledCaseException.FromEnum(op, "ternary operation");
            }

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

        public void EmitTokenize(IOperand text, IOperand parse, IOperand dictionary, IOperand flag)
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
        public void EmitSave(IOperand table, IOperand size, IOperand filename,
            IVariable result)
        {
            AddLine(
                new Instruction("SAVE", table.ToAsmExpr(), size.ToAsmExpr(), filename.ToAsmExpr()) { StoreTarget = result.ToString() }, 
                null,
                PeepholeLineType.Plain);
        }

        public void EmitRestore(IOperand table, IOperand size, IOperand filename,
            IVariable result)
        {
            AddLine(
                new Instruction("RESTORE", table.ToAsmExpr(), size.ToAsmExpr(), filename.ToAsmExpr()) { StoreTarget = result.ToString() }, 
                null,
                PeepholeLineType.Plain);
        }

        public void EmitScanTable(IOperand value, IOperand table, IOperand length, IOperand form,
            IVariable result, ILabel label, bool polarity)
        {
            var sb = new StringBuilder("INTBL? ");
            sb.Append(value);
            sb.Append(',');
            sb.Append(table);
            sb.Append(',');
            sb.Append(length);
            if (form != null)
            {
                sb.Append(',');
                sb.Append(form);
            }
            sb.Append(" >");
            sb.Append(result);

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
            string opcode;

            switch (op)
            {
                case PrintOp.Address:
                    opcode = "PRINTB";
                    break;
                case PrintOp.Character:
                    opcode = "PRINTC";
                    break;
                case PrintOp.Number:
                    opcode = "PRINTN";
                    break;
                case PrintOp.Object:
                    opcode = "PRINTD";
                    break;
                case PrintOp.PackedAddr:
                    opcode = "PRINT";
                    break;
                case PrintOp.Unicode:
                    opcode = "PRINTU";
                    break;
                default:
                    throw UnhandledCaseException.FromEnum(op, "print operation");
            }

            AddLine(new Instruction(opcode, value.ToAsmExpr()), null, PeepholeLineType.Plain);
        }

        public void EmitPrintTable(IOperand table, IOperand width, IOperand height, IOperand skip)
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

        public void EmitPlaySound(IOperand number, IOperand effect, IOperand volume, IOperand routine)
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

        public void EmitRead(IOperand chrbuf, IOperand lexbuf, IOperand interval, IOperand routine,
            IVariable result)
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

        public void EmitReadChar(IOperand interval, IOperand routine, IVariable result)
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
        public void EmitCall(IOperand routine, IOperand[] args, IVariable result)
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
                // V4: use CALL/CALL1/CALL2/XCALL opcodes, pop result if not needed
                string opcode;
                switch (args.Length)
                {
                    case 0:
                        opcode = "CALL1";
                        break;
                    case 1:
                        opcode = "CALL2";
                        break;
                    case 2:
                    case 3:
                        opcode = "CALL";
                        break;
                    default:
                        opcode = "XCALL";
                        break;
                }

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
                    switch (args.Length)
                    {
                        case 0:
                            opcode = "ICALL1";
                            break;
                        case 1:
                            opcode = "ICALL2";
                            break;
                        case 2:
                        case 3:
                            opcode = "ICALL";
                            break;
                        default:
                            opcode = "IXCALL";
                            break;
                    }
                }
                else
                {
                    switch (args.Length)
                    {
                        case 0:
                            opcode = "CALL1";
                            break;
                        case 1:
                            opcode = "CALL2";
                            break;
                        case 2:
                        case 3:
                            opcode = "CALL";
                            break;
                        default:
                            opcode = "XCALL";
                            break;
                    }
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
            });

            if (game.debug != null)
                game.WriteOutput(
                    INDENT +
                    $".DEBUG-ROUTINE-END {game.debug.GetFileNumber(defnEnd.File)},{defnEnd.Line},{defnEnd.Column}");
        }

        class PeepholeCombiner : IPeepholeCombiner<ZapCode>
        {
            readonly RoutineBuilder routineBuilder;

            public PeepholeCombiner(RoutineBuilder routineBuilder)
            {
                this.routineBuilder = routineBuilder;
            }

            void BeginMatch([NotNull] IEnumerable<CombinableLine<ZapCode>> lines)
            {
                enumerator = lines.GetEnumerator();
                matches = new List<CombinableLine<ZapCode>>();
            }

            bool Match([ItemNotNull] [NotNull] [InstantHandle] params Predicate<CombinableLine<ZapCode>>[] criteria)
            {
                while (matches.Count < criteria.Length)
                {
                    if (enumerator.MoveNext() == false)
                        return false;

                    matches.Add(enumerator.Current);
                }
                
                return criteria.Zip(matches, (c, m) => c(m)).All(ok => ok);
            }

            void EndMatch()
            {
                enumerator.Dispose();
                enumerator = null;

                matches = null;
            }

            IEnumerator<CombinableLine<ZapCode>> enumerator;
            List<CombinableLine<ZapCode>> matches;

            CombinerResult<ZapCode> Combine1To1(Instruction newInstruction, PeepholeLineType? type = null, [CanBeNull] ILabel target = null)
            {
                return new CombinerResult<ZapCode>(
                    1,
                    new[] {
                        new CombinableLine<ZapCode>(
                            matches[0].Label,
                            new ZapCode {
                                Instruction = newInstruction,
                                DebugText = matches[0].Code.DebugText
                            },
                            target ?? matches[0].Target,
                            type ?? matches[0].Type)
                    });
            }

            CombinerResult<ZapCode> Combine2To1(Instruction newInstruction, PeepholeLineType? type = null, [CanBeNull] ILabel target = null)
            {
                return new CombinerResult<ZapCode>(
                    2,
                    new[] {
                        new CombinableLine<ZapCode>(
                            matches[0].Label,
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
                [CanBeNull] ILabel target1 = null, [CanBeNull] ILabel target2 = null)
            {
                return new CombinerResult<ZapCode>(
                    2,
                    new[] {
                        new CombinableLine<ZapCode>(
                            matches[0].Label,
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

            [ContractAnnotation("=> true, otherSide: notnull; => false, otherSide: null")]
            static bool IsEqualZero([NotNull] Instruction inst, out AsmExpr otherSide)
            {
                if (inst.Name == "EQUAL?" && inst.Operands.Count == 2)
                {
                    if (inst.Operands[0] is NumericLiteral num && num.Value == 0)
                    {
                        otherSide = inst.Operands[1];
                        return true;
                    }

                    num = inst.Operands[1] as NumericLiteral;
                    if (num != null && num.Value == 0)
                    {
                        otherSide = inst.Operands[0];
                        return true;
                    }
                }

                otherSide = null;
                return false;
            }

            [ContractAnnotation("=> true, dest: notnull, constant: notnull; => false, dest: null, constant: null")]
            static bool IsBANDConstantWithStack([NotNull] Instruction inst, out NumericLiteral constant, out string dest) =>
                IsCommutativeConstantWithStack("BAND", inst, out constant, out dest);

            [ContractAnnotation("=> true, dest: notnull, constant: notnull; => false, dest: null, constant: null")]
            static bool IsBORConstantWithStack([NotNull] Instruction inst, out NumericLiteral constant, out string dest) =>
                IsCommutativeConstantWithStack("BOR", inst, out constant, out dest);

            [ContractAnnotation("=> true, dest: notnull, constant: notnull; => false, dest: null, constant: null")]
            static bool IsCommutativeConstantWithStack(
                [NotNull] string instructionName, [NotNull] Instruction inst,
                out NumericLiteral constant, out string dest)
            {
                if (inst.Name == instructionName && inst.Operands.Count == 2)
                {
                    if (inst.Operands[0] is NumericLiteral num && inst.Operands[1].IsStack())
                    {
                        constant = num;
                        dest = inst.StoreTarget;
                        return true;
                    }

                    num = inst.Operands[1] as NumericLiteral;
                    if (num != null && inst.Operands[0].IsStack())
                    {
                        constant = num;
                        dest = inst.StoreTarget;
                        return true;
                    }
                }

                constant = null;
                dest = null;
                return false;
            }

            [ContractAnnotation(
                "=> true, variable: notnull, constant: notnull; => false, variable: null, constant: null")]
            static bool IsBANDConstantToStack([NotNull] Instruction inst, out AsmExpr variable,
                out NumericLiteral constant) =>
                IsCommutativeConstantToStack("BAND", inst, out variable, out constant);

            [ContractAnnotation(
                "=> true, variable: notnull, constant: notnull; => false, variable: null, constant: null")]
            static bool IsBORConstantToStack([NotNull] Instruction inst, out AsmExpr variable,
                out NumericLiteral constant) =>
                IsCommutativeConstantToStack("BOR", inst, out variable, out constant);

            [ContractAnnotation(
                "=> true, variable: notnull, constant: notnull; => false, variable: null, constant: null")]
            static bool IsCommutativeConstantToStack(
                [NotNull] string instructionName, [NotNull] Instruction inst,
                out AsmExpr variable, out NumericLiteral constant)
            {
                if (inst.Name == instructionName && inst.Operands.Count == 2 && inst.StoreTarget == "STACK")
                {
                    if (inst.Operands[0] is NumericLiteral num)
                    {
                        variable = inst.Operands[1];
                        constant = num;
                        return true;
                    }

                    num = inst.Operands[1] as NumericLiteral;
                    if (num != null)
                    {
                        variable = inst.Operands[0];
                        constant = num;
                        return true;
                    }
                }

                variable = constant = null;
                return false;
            }

            [ContractAnnotation("=> true, dest: notnull; => false, dest: null")]
            static bool IsPopToVariable([NotNull] Instruction inst, out string dest)
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
                AsmExpr expr1 = null;
                NumericLiteral const1 = null, const2 = null;
                string destStr = null;

                BeginMatch(lines);
                try
                {
                    if (Match(a => IsEqualZero(a.Code.Instruction, out expr1)))
                    {
                        // EQUAL? x,0 | EQUAL? 0,x => ZERO? x
                        return Combine1To1(new Instruction("ZERO?", expr1));
                    }

                    if (Match(a => a.Code.Instruction.Name=="JUMP" && (a.Target == RTRUE || a.Target==RFALSE)))
                    {
                        // JUMP to TRUE/FALSE => RTRUE/RFALSE
                        return Combine1To1(new Instruction(matches[0].Target == RTRUE ? "RTRUE" : "RFALSE"));
                    }

                    //if (Match(a => a.Code.Text.StartsWith("PUSH ", StringComparison.Ordinal), b => b.Code.Text == "RSTACK"))
                    if (Match(a => a.Code.Instruction.Name == "PUSH" && a.Code.Instruction.Operands.Count == 1,
                        b => b.Code.Instruction.Name == "RSTACK"))
                    {
                        // PUSH + RSTACK => RFALSE/RTRUE/RETURN
                        switch (matches[0].Code.Instruction.Operands[0])
                        {
                            case NumericLiteral lit when lit.Value == 0:
                                return Combine2To1(new Instruction("RFALSE"), PeepholeLineType.BranchAlways, RFALSE);
                            case NumericLiteral lit when lit.Value == 1:
                                return Combine2To1(new Instruction("RTRUE"), PeepholeLineType.BranchAlways, RTRUE);
                            default:
                                return Combine2To1(matches[0].Code.Instruction.WithName("RETURN"));
                        }
                    }

                    if (Match(a => a.Code.Instruction.StoreTarget == "STACK",
                        b => IsPopToVariable(b.Code.Instruction, out destStr)))
                    {
                        // >STACK + POP 'dest => >dest
                        return Combine2To1(matches[0].Code.Instruction.WithStoreTarget(destStr));
                    }

                    if (Match(a => a.Code.Instruction.Name == "PUSH",
                        b => IsPopToVariable(b.Code.Instruction, out destStr)))
                    {
                        // PUSH + POP 'dest => SET 'dest
                        return Combine2To1(new Instruction(
                            "SET",
                            new QuoteExpr(new SymbolExpr(destStr)),
                            matches[0].Code.Instruction.Operands[0]));
                    }

                    if (Match(
                        a => a.Code.Instruction.Name == "INC" && a.Code.Instruction.Operands[0].IsQuote(out expr1) &&
                             !expr1.IsStack(),
                        b => b.Code.Instruction.Name == "GRTR?" && b.Code.Instruction.Operands[0].Equals(expr1)))
                    {
                        // INC 'v + GRTR? v,w => IGRTR? 'v,w
                        return Combine2To1(new Instruction(
                            "IGRTR?",
                            matches[0].Code.Instruction.Operands[0],
                            matches[1].Code.Instruction.Operands[1]));
                    }

                    if (Match(
                        a => a.Code.Instruction.Name == "DEC" && a.Code.Instruction.Operands[0].IsQuote(out expr1) &&
                             !expr1.IsStack(),
                        b => b.Code.Instruction.Name == "LESS?" && b.Code.Instruction.Operands[0].Equals(expr1)))
                    {
                        // DEC 'v + LESS? v,w => DLESS? 'v,w
                        return Combine2To1(new Instruction(
                            "DLESS?",
                            matches[0].Code.Instruction.Operands[0],
                            matches[1].Code.Instruction.Operands[1]));
                    }

                    if (Match(
                        a => (a.Code.Instruction.Name == "EQUAL?" || a.Code.Instruction.Name == "ZERO?") &&
                             a.Type == PeepholeLineType.BranchPositive,
                        b => (b.Code.Instruction.Name == "EQUAL?" || b.Code.Instruction.Name == "ZERO?") &&
                             b.Type == PeepholeLineType.BranchPositive))
                    {
                        if (matches[0].Target == matches[1].Target)
                        {
                            IList<AsmExpr> GetParts(Instruction inst)
                            {
                                return inst.Name == "ZERO?"
                                    ? new[] { inst.Operands[0], new NumericLiteral(0) }
                                    : inst.Operands;
                            }

                            var aparts = GetParts(matches[0].Code.Instruction);
                            var bparts = GetParts(matches[1].Code.Instruction);

                            if (aparts[0].Equals(bparts[0]) && aparts.Count < 4)
                            {
                                if (aparts.Count + bparts.Count <= 5)
                                {
                                    // EQUAL? v,a,b /L + EQUAL? v,c /L => EQUAL? v,a,b,c /L
                                    return Combine2To1(new Instruction("EQUAL?", aparts.Concat(bparts.Skip(1))));
                                }
                                else
                                {
                                    // EQUAL? v,a,b /L + EQUAL? v,c,d /L => EQUAL? v,a,b,c /L + EQUAL? v,d /L
                                    var allRhs = aparts.Skip(1).Concat(bparts.Skip(1)).ToArray();

                                    var first = new Instruction("EQUAL?",
                                        Enumerable.Repeat(aparts[0], 1).Concat(allRhs.Take(3)));

                                    var second = new Instruction("EQUAL?",
                                        Enumerable.Repeat(aparts[0], 1).Concat(allRhs.Skip(3)));

                                    return Combine2To2(first, second);
                                }
                            }
                        }
                    }

                    //if (Match(a => a.Code.Text == "CRLF", b => b.Code.Text == "RTRUE"))
                    if (Match(a => a.Code.Instruction.Name == "CRLF", b => b.Code.Instruction.Name == "RTRUE"))
                    {
                        // combine CRLF + RTRUE into a single terminator
                        // this can be pulled through a branch and thus allows more PRINTR transformations
                        return Combine2To1(new Instruction("CRLF+RTRUE"), PeepholeLineType.Terminator);
                    }

                    //if (Match(a => a.Code.Text.StartsWith("PRINTI ", StringComparison.Ordinal), b => b.Code.Text == "CRLF+RTRUE"))
                    if (Match(a => a.Code.Instruction.Name == "PRINTI", b => b.Code.Instruction.Name=="CRLF+RTRUE"))
                    {
                        // PRINTI + (CRLF + RTRUE) => PRINTR
                        return Combine2To1(matches[0].Code.Instruction.WithName("PRINTR"), PeepholeLineType.HeavyTerminator);
                    }

                    // BAND v,c >STACK + ZERO? STACK /L =>
                    //     when c == 0:              simple branch
                    //     when c is a power of two: BTST v,c \L
                    if (Match(a => IsBANDConstantToStack(a.Code.Instruction, out expr1, out const1),
                        b => b.Code.Instruction.Name == "ZERO?" && b.Code.Instruction.Operands[0].IsStack()))
                    {
                        var constantValue = const1.Value;

                        if (constantValue == 0)
                        {
                            if (!expr1.IsStack())
                            {
                                return matches[1].Type == PeepholeLineType.BranchPositive
                                    ? Combine2To1(new Instruction("JUMP"), PeepholeLineType.BranchAlways, matches[1].Target)
                                    : Consume(2);
                            }
                        }
                        else if ((constantValue & (constantValue - 1)) == 0)
                        {
                            var oppositeType = matches[1].Type == PeepholeLineType.BranchPositive
                                ? PeepholeLineType.BranchNegative
                                : PeepholeLineType.BranchPositive;

                            return Combine2To1(new Instruction("BTST", expr1, const1), oppositeType);
                        }
                    }

                    // BAND v,c1 >STACK + BAND STACK,c2 >dest => BAND v,(c1&c2) >dest
                    if (Match(a => IsBANDConstantToStack(a.Code.Instruction, out expr1, out const1),
                        b => IsBANDConstantWithStack(b.Code.Instruction, out const2, out destStr)))
                    {
                        var combined = const1.Value & const2.Value;
                        return Combine2To1(
                            new Instruction("BAND", expr1, new NumericLiteral(combined)) { StoreTarget = destStr });
                    }

                    // BOR v,c1 >STACK + BOR STACK,c2 >dest => BOR v,(c1|c2) >dest
                    if (Match(a => IsBORConstantToStack(a.Code.Instruction, out expr1, out const1),
                        b => IsBORConstantWithStack(b.Code.Instruction, out const2, out destStr)))
                    {
                        var combined = const1.Value | const2.Value;
                        return Combine2To1(
                            new Instruction("BOR", expr1, new NumericLiteral(combined)) { StoreTarget = destStr });
                    }

                    // no matches
                    return new CombinerResult<ZapCode>();
                }
                finally
                {
                    EndMatch();
                }
            }

            public ZapCode SynthesizeBranchAlways()
            {
                return new ZapCode { Instruction = new Instruction("JUMP") };
            }

            [CanBeNull]
            private static string MergeDebugText([CanBeNull] string text1, [CanBeNull] string text2)
            {
                return
                    text1 == null ? text2
                    : text2 == null ? text1
                    : text1 == text2 ? text1
                    : $"{text1}\r\n{INDENT}{text2}";
            }

            public bool AreIdentical(ZapCode a, ZapCode b)
            {
                return a.Instruction.Equals(b.Instruction);
            }

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

            public ILabel NewLabel()
            {
                return routineBuilder.DefineLabel();
            }
        }
    }
}