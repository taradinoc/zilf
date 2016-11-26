using System;
using System.Collections.Generic;
using System.Diagnostics.Contracts;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;

namespace Zilf.Emit.Zap
{
    class RoutineBuilder : IRoutineBuilder
    {
        internal static readonly Label RTRUE = new Label("TRUE");
        internal static readonly Label RFALSE = new Label("FALSE");
        internal static readonly VariableOperand STACK = new VariableOperand("STACK");
        private const string INDENT = "\t";

        private readonly GameBuilder game;
        private readonly string name;
        private readonly bool entryPoint, cleanStack;

        internal DebugLineRef defnStart, defnEnd;

        private readonly PeepholeBuffer<ZapCode> peep;
        private readonly ILabel routineStartLabel;
        private int nextLabel = 0;
        private string pendingDebugText;

        private readonly List<LocalBuilder> requiredParams = new List<LocalBuilder>();
        private readonly List<LocalBuilder> optionalParams = new List<LocalBuilder>();
        private readonly List<LocalBuilder> locals = new List<LocalBuilder>();

        public RoutineBuilder(GameBuilder game, string name, bool entryPoint, bool cleanStack)
        {
            this.game = game;
            this.name = name;
            this.entryPoint = entryPoint;
            this.cleanStack = cleanStack;

            peep = new PeepholeBuffer<ZapCode>();
            peep.Combiner = new PeepholeCombiner(this);

            this.routineStartLabel = DefineLabel();
        }

        public override string ToString()
        {
            return name;
        }

        public bool CleanStack
        {
            get { return cleanStack; }
        }

        public ILabel RTrue
        {
            get { return RTRUE; }
        }

        public ILabel RFalse
        {
            get { return RFALSE; }
        }

        public IVariable Stack
        {
            get { return STACK; }
        }

        private bool LocalExists(string name)
        {
            return requiredParams.Concat(optionalParams).Concat(locals).Any(lb => lb.Name == name);
        }

        public ILocalBuilder DefineRequiredParameter(string name)
        {
            name = GameBuilder.SanitizeSymbol(name);

            if (entryPoint)
                throw new InvalidOperationException("Entry point may not have parameters");
            if (LocalExists(name))
                throw new ArgumentException("Local variable already exists: " + name, "name");

            LocalBuilder local = new LocalBuilder(name);
            requiredParams.Add(local);
            return local;
        }

        public ILocalBuilder DefineOptionalParameter(string name)
        {
            name = GameBuilder.SanitizeSymbol(name);

            if (entryPoint)
                throw new InvalidOperationException("Entry point may not have parameters");
            if (LocalExists(name))
                throw new ArgumentException("Local variable already exists: " + name, "name");

            LocalBuilder local = new LocalBuilder(name);
            optionalParams.Add(local);
            return local;
        }

        public ILocalBuilder DefineLocal(string name)
        {
            name = GameBuilder.SanitizeSymbol(name);

            if (entryPoint)
                throw new InvalidOperationException("Entry point may not have local variables");
            if (LocalExists(name))
                throw new ArgumentException("Local variable already exists: " + name, "name");

            LocalBuilder local = new LocalBuilder(name);
            locals.Add(local);
            return local;
        }

        public ILabel RoutineStart
        {
            get { return routineStartLabel; }
        }

        public ILabel DefineLabel()
        {
            return new Label("?L" + (nextLabel++).ToString());
        }

        public void MarkLabel(ILabel label)
        {
            peep.MarkLabel(label);
        }

        private void AddLine(string code, ILabel target, PeepholeLineType type)
        {
            ZapCode zc;
            zc.Text = code;
            zc.DebugText = pendingDebugText;
            pendingDebugText = null;

            peep.AddLine(zc, target, type);
        }

        public void MarkSequencePoint(DebugLineRef lineRef)
        {
            if (game.debug != null)
                pendingDebugText = string.Format(
                    ".DEBUG-LINE {0},{1},{2}",
                    game.debug.GetFileNumber(lineRef.File),
                    lineRef.Line,
                    lineRef.Column);
        }

        public void Branch(ILabel label)
        {
            AddLine("JUMP", label, PeepholeLineType.BranchAlways);
        }

        public bool HasArgCount
        {
            get { return game.zversion >= 5; }
        }

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
                    throw new NotImplementedException();
            }

            if (leftVar && !(left is IVariable))
                throw new ArgumentException("This condition requires a variable", "left");

            if (nullary)
            {
                if (left != null || right != null)
                    throw new ArgumentException("Expected no operands for nullary condition");
            }
            else if (unary)
            {
                if (right != null)
                    throw new ArgumentException("Expected only one operand for unary condition", "right");
            }
            else
            {
                if (right == null)
                    throw new ArgumentException("Expected two operands for binary condition", "right");
            }

            Contract.Assert(leftVar || !unary);
            AddLine(
                nullary ?
                    opcode :
                    unary ?
                        string.Format("{0} '{1}", opcode, left) :       // see assert above
                        string.Format("{0} {1}{2},{3}", opcode, leftVar ? "'" : "", left, right),
                label,
                polarity ? PeepholeLineType.BranchPositive : PeepholeLineType.BranchNegative);
        }

        public void BranchIfZero(IOperand operand, ILabel label, bool polarity)
        {
            AddLine(
                "ZERO? " + operand.ToString(),
                label,
                polarity ? PeepholeLineType.BranchPositive : PeepholeLineType.BranchNegative);
        }

        public void BranchIfEqual(IOperand value, IOperand option1, ILabel label, bool polarity)
        {
            AddLine(
                string.Format("EQUAL? {0},{1}", value, option1),
                label,
                polarity ? PeepholeLineType.BranchPositive : PeepholeLineType.BranchNegative);
        }

        public void BranchIfEqual(IOperand value, IOperand option1, IOperand option2, ILabel label, bool polarity)
        {
            AddLine(
                string.Format("EQUAL? {0},{1},{2}", value, option1, option2),
                label,
                polarity ? PeepholeLineType.BranchPositive : PeepholeLineType.BranchNegative);
        }

        public void BranchIfEqual(IOperand value, IOperand option1, IOperand option2, IOperand option3, ILabel label, bool polarity)
        {
            AddLine(
                string.Format("EQUAL? {0},{1},{2},{3}", value, option1, option2, option3),
                label,
                polarity ? PeepholeLineType.BranchPositive : PeepholeLineType.BranchNegative);
        }

        public void Return(IOperand result)
        {
            if (result == GameBuilder.ONE)
                AddLine("RTRUE", RTRUE, PeepholeLineType.BranchAlways);
            else if (result == GameBuilder.ZERO)
                AddLine("RFALSE", RFALSE, PeepholeLineType.BranchAlways);
            else if (result == STACK)
                AddLine("RSTACK", null, PeepholeLineType.Terminator);
            else
                AddLine("RETURN " + result.ToString(), null, PeepholeLineType.Terminator);
        }

        public bool HasUndo
        {
            get { return game.zversion >= 5; }
        }

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
                    throw new NotImplementedException();
            }

            AddLine(
                string.Format("{0}{1}{2}",
                    opcode,
                    result == null ? "" : " >",
                    (object)result ?? ""),
                null,
                PeepholeLineType.Plain);
        }

        public void EmitUnary(UnaryOp op, IOperand value, IVariable result)
        {
            if (op == UnaryOp.Neg)
            {
                AddLine(
                    string.Format("SUB 0,{0}{1}{2}",
                        value,
                        result == null ? "" : " >",
                        (object)result ?? ""),
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
                default:
                    throw new NotImplementedException();
            }

            if (pred)
            {
                ILabel label = DefineLabel();

                AddLine(
                    string.Format("{0} {1}{2}{3}",
                        opcode,
                        value,
                        result == null ? "" : " >",
                        (object)result ?? ""),
                    label,
                    PeepholeLineType.BranchPositive);

                peep.MarkLabel(label);
            }
            else
            {
                AddLine(
                    string.Format("{0} {1}{2}{3}",
                        opcode,
                        value,
                        result == null ? "" : " >",
                        (object)result ?? ""),
                    null,
                    PeepholeLineType.Plain);
            }
        }

        public void EmitBinary(BinaryOp op, IOperand left, IOperand right, IVariable result)
        {
            // optimize special cases
            if (op == BinaryOp.Add &&
                ((left == game.One && right == result) || (right == game.One && left == result)))
            {
                AddLine("INC '" + result.ToString(), null, PeepholeLineType.Plain);
                return;
            }
            else if (op == BinaryOp.Sub && left == result && right == game.One)
            {
                AddLine("DEC '" + result.ToString(), null, PeepholeLineType.Plain);
                return;
            }
            else if (op == BinaryOp.StoreIndirect && right == Stack && game.zversion != 6)
            {
                AddLine("POP " + left.ToString(), null, PeepholeLineType.Plain);
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
                    throw new NotImplementedException();
            }

            AddLine(
                string.Format("{0} {1},{2}{3}{4}",
                    opcode,
                    left,
                    right,
                    result == null ? "" : " >",
                    (object)result ?? ""),
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
                    throw new NotImplementedException();
            }

            AddLine(
                string.Format("{0} {1},{2},{3}{4}{5}",
                    opcode,
                    left,
                    center,
                    right,
                    result == null ? "" : " >",
                    (object)result ?? ""),
                null,
                PeepholeLineType.Plain);
        }

        public void EmitEncodeText(IOperand src, IOperand length, IOperand srcOffset, IOperand dest)
        {
            AddLine(
                string.Format("ZWSTR {0},{1},{2},{3}",
                    src,
                    length,
                    srcOffset,
                    dest),
                null,
                PeepholeLineType.Plain);
        }

        public void EmitTokenize(IOperand text, IOperand parse, IOperand dictionary, IOperand flag)
        {
            var sb = new StringBuilder("LEX ");
            sb.Append(text);
            sb.Append(',');
            sb.Append(parse);

            if (dictionary != null)
            {
                sb.Append(',');
                sb.Append(dictionary);

                if (flag != null)
                {
                    sb.Append(',');
                    sb.Append(flag);
                }
            }

            AddLine(sb.ToString(), null, PeepholeLineType.Plain);
        }

        public void EmitRestart()
        {
            AddLine("RESTART", null, PeepholeLineType.Terminator);
        }

        public void EmitQuit()
        {
            AddLine("QUIT", null, PeepholeLineType.Terminator);
        }

        public bool HasBranchSave
        {
            get { return game.zversion < 4; }
        }

        public void EmitSave(ILabel label, bool polarity)
        {
            AddLine("SAVE", label,
                polarity ? PeepholeLineType.BranchPositive : PeepholeLineType.BranchNegative);
        }

        public void EmitRestore(ILabel label, bool polarity)
        {
            AddLine("RESTORE", label,
                polarity ? PeepholeLineType.BranchPositive : PeepholeLineType.BranchNegative);
        }

        public bool HasStoreSave
        {
            get { return game.zversion >= 4; }
        }

        public void EmitSave(IVariable result)
        {
            AddLine("SAVE >" + result, null, PeepholeLineType.Plain);
        }

        public void EmitRestore(IVariable result)
        {
            AddLine("RESTORE >" + result, null, PeepholeLineType.Plain);
        }

        public bool HasExtendedSave
        {
            get { return game.zversion >= 5; }
        }

        public void EmitSave(IOperand table, IOperand size, IOperand name,
            IVariable result)
        {
            AddLine(
                string.Format("SAVE {0},{1},{2} >{3}",
                    table, size, name, result),
                null,
                PeepholeLineType.Plain);
        }

        public void EmitRestore(IOperand table, IOperand size, IOperand name,
            IVariable result)
        {
            AddLine(
                string.Format("RESTORE {0},{1},{2} >{3}",
                    table, size, name, result),
                null,
                PeepholeLineType.Plain);
        }

        public void EmitScanTable(IOperand value, IOperand table, IOperand length, IOperand form,
            IVariable result, ILabel label, bool polarity)
        {
            StringBuilder sb = new StringBuilder("INTBL? ");
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

            AddLine(sb.ToString(), label,
                polarity ? PeepholeLineType.BranchPositive : PeepholeLineType.BranchNegative);
        }

        public void EmitGetChild(IOperand value, IVariable result, ILabel label, bool polarity)
        {
            AddLine(
                string.Format("FIRST? {0} >{1}", value, result),
                label,
                polarity ? PeepholeLineType.BranchPositive : PeepholeLineType.BranchNegative);
        }

        public void EmitGetSibling(IOperand value, IVariable result, ILabel label, bool polarity)
        {
            AddLine(
                string.Format("NEXT? {0} >{1}", value, result),
                label,
                polarity ? PeepholeLineType.BranchPositive : PeepholeLineType.BranchNegative);
        }

        public void EmitPrintNewLine()
        {
            AddLine("CRLF", null, PeepholeLineType.Plain);
        }

        public void EmitPrint(string text, bool crlfRtrue)
        {
            AddLine(
                string.Format("{0} \"{1}\"", crlfRtrue ? "PRINTR" : "PRINTI",
                    GameBuilder.SanitizeString(text)),
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
                    throw new NotImplementedException();
            }

            AddLine(opcode + " " + value.ToString(), null, PeepholeLineType.Plain);
        }

        public void EmitPrintTable(IOperand table, IOperand width, IOperand height, IOperand skip)
        {
            StringBuilder sb = new StringBuilder("PRINTT ");
            sb.Append(table);
            sb.Append(',');
            sb.Append(width);

            if (height != null)
            {
                sb.Append(',');
                sb.Append(height);

                if (skip != null)
                {
                    sb.Append(',');
                    sb.Append(skip);
                }
            }

            AddLine(sb.ToString(), null, PeepholeLineType.Plain);
        }

        public void EmitPlaySound(IOperand number, IOperand effect, IOperand volume, IOperand routine)
        {
            StringBuilder sb = new StringBuilder("SOUND ");
            sb.Append(number);

            if (effect != null)
            {
                sb.Append(',');
                sb.Append(effect);

                if (volume != null)
                {
                    sb.Append(',');
                    sb.Append(volume);

                    if (routine != null)
                    {
                        sb.Append(',');
                        sb.Append(routine);
                    }
                }
            }

            AddLine(sb.ToString(), null, PeepholeLineType.Plain);
        }

        public void EmitRead(IOperand chrbuf, IOperand lexbuf, IOperand interval, IOperand routine,
            IVariable result)
        {
            StringBuilder sb = new StringBuilder("READ ");
            sb.Append(chrbuf);

            if (lexbuf != null)
            {
                sb.Append(',');
                sb.Append(lexbuf);

                if (interval != null)
                {
                    sb.Append(',');
                    sb.Append(interval);

                    if (routine != null)
                    {
                        sb.Append(',');
                        sb.Append(routine);
                    }
                }
            }

            if (result != null)
            {
                sb.Append(" >");
                sb.Append(result);
            }

            AddLine(sb.ToString(), null, PeepholeLineType.Plain);
        }

        public void EmitReadChar(IOperand interval, IOperand routine, IVariable result)
        {
            StringBuilder sb = new StringBuilder("INPUT 1");

            if (interval != null)
            {
                sb.Append(',');
                sb.Append(interval);

                if (routine != null)
                {
                    sb.Append(',');
                    sb.Append(routine);
                }
            }

            sb.Append(" >");
            sb.Append(result);

            AddLine(sb.ToString(), null, PeepholeLineType.Plain);
        }

        public void EmitCall(IOperand routine, IOperand[] args, IVariable result)
        {
            /* V1-3: CALL (0-3, store)
             * V4: CALL1 (0, store), CALL2 (1, store), XCALL (0-7, store)
             * V5: ICALL1 (0), ICALL2 (1), ICALL (0-3), IXCALL (0-7) */

            if (args.Length > game.MaxCallArguments)
                throw new ArgumentException(
                    string.Format(
                        "Too many arguments in routine call: {0} supplied, {1} allowed",
                        args.Length,
                        game.MaxCallArguments));

            StringBuilder sb = new StringBuilder();

            if (game.zversion < 4)
            {
                // V1-3: use only CALL opcode (3 args max), pop result if not needed
                sb.Append("CALL ");
                sb.Append(routine);
                foreach (IOperand arg in args)
                {
                    sb.Append(',');
                    sb.Append(arg);
                }

                if (result != null)
                {
                    sb.Append(" >");
                    sb.Append(result);
                }

                AddLine(sb.ToString(), null, PeepholeLineType.Plain);

                if (result == null && cleanStack)
                    AddLine("FSTACK", null, PeepholeLineType.Plain);
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

                sb.Append(opcode);
                sb.Append(' ');
                sb.Append(routine);
                foreach (IOperand arg in args)
                {
                    sb.Append(',');
                    sb.Append(arg);
                }

                if (result != null)
                {
                    sb.Append(" >");
                    sb.Append(result);
                }

                AddLine(sb.ToString(), null, PeepholeLineType.Plain);

                if (result == null && cleanStack)
                    AddLine("FSTACK", null, PeepholeLineType.Plain);
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

                sb.Append(opcode);
                sb.Append(' ');
                sb.Append(routine);
                foreach (IOperand arg in args)
                {
                    sb.Append(',');
                    sb.Append(arg);
                }

                if (result != null)
                {
                    sb.Append(" >");
                    sb.Append(result);
                }

                AddLine(sb.ToString(), null, PeepholeLineType.Plain);
            }
        }

        public void EmitStore(IVariable dest, IOperand src)
        {
            if (dest != src)
            {
                if (dest == STACK)
                {
                    AddLine("PUSH " + src.ToString(), null, PeepholeLineType.Plain);
                }
                else if (src == STACK)
                {
                    if (game.zversion == 6)
                        AddLine("POP >" + dest.ToString(), null, PeepholeLineType.Plain);
                    else
                        AddLine("POP '" + dest.ToString(), null, PeepholeLineType.Plain);
                }
                else
                {
                    AddLine(string.Format("SET '{0},{1}", dest, src), null, PeepholeLineType.Plain);
                }
            }
        }

        public void EmitPopStack()
        {
            if (cleanStack)
            {
                if (game.zversion <= 4)
                {
                    AddLine("FSTACK", null, PeepholeLineType.Plain);
                }
                else if (game.zversion == 6)
                {
                    AddLine("FSTACK 1", null, PeepholeLineType.Plain);
                }
                else
                {
                    AddLine("ICALL2 0,STACK", null, PeepholeLineType.Plain);
                }
            }
        }

        public void EmitPushUserStack(IOperand value, IOperand stack, ILabel label, bool polarity)
        {
            AddLine($"XPUSH {value},{stack}", label,
                polarity ? PeepholeLineType.BranchPositive : PeepholeLineType.BranchNegative);
        }

        public void Finish()
        {
            game.WriteOutput(string.Empty);

            StringBuilder sb = new StringBuilder();

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
                foreach (LocalBuilder lb in requiredParams.Concat(optionalParams).Concat(locals))
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

            foreach (LocalBuilder lb in requiredParams)
            {
                sb.Append(',');
                sb.Append(lb.Name);
            }

            foreach (LocalBuilder lb in Enumerable.Concat(optionalParams, locals))
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

            if (entryPoint && game.zversion != 6 && game.zversion != 7)
            {
                game.WriteOutput("START::");
            }

            // write preamble
            var preamble = new PeepholeBuffer<ZapCode>();
            preamble.MarkLabel(routineStartLabel);

            // write values for optional params and locals for V5+
            if (game.zversion >= 5)
            {
                // TODO: skip if the default value is 0?

                foreach (LocalBuilder lb in optionalParams)
                {
                    if (lb.DefaultValue != null)
                    {
                        ILabel nextLabel = DefineLabel();

                        preamble.AddLine(
                            new ZapCode { Text = string.Format("ASSIGNED? '{0}", lb) },
                            nextLabel,
                            PeepholeLineType.BranchPositive);
                        preamble.AddLine(
                            new ZapCode { Text = string.Format("SET '{0},{1}", lb, lb.DefaultValue) },
                            null,
                            PeepholeLineType.Plain);
                        preamble.MarkLabel(nextLabel);
                    }
                }

                foreach (LocalBuilder lb in locals)
                    if (lb.DefaultValue != null)
                        preamble.AddLine(
                            new ZapCode { Text = string.Format("SET '{0},{1}", lb, lb.DefaultValue) },
                            null,
                            PeepholeLineType.Plain);
            }

            peep.InsertBufferFirst(preamble);

            // write routine body
            peep.Finish((label, code, dest, type) =>
            {
                if (code.DebugText != null)
                    game.WriteOutput(INDENT + code.DebugText);

                if (type == PeepholeLineType.BranchAlways)
                {
                    if (dest == RTRUE)
                    {
                        game.WriteOutput(INDENT + "RTRUE");
                        return;
                    }
                    if (dest == RFALSE)
                    {
                        game.WriteOutput(INDENT + "RFALSE");
                        return;
                    }
                }

                if (code.Text == "CRLF+RTRUE")
                {
                    game.WriteOutput(string.Format("{0}{1}{2}CRLF",
                        (object)label ?? "",
                        label == null ? "" : ":",
                        INDENT));

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
                sb.Append(code.Text);

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
                game.WriteOutput(string.Format(
                    INDENT + ".DEBUG-ROUTINE-END {0},{1},{2}",
                    game.debug.GetFileNumber(defnEnd.File),
                    defnEnd.Line,
                    defnEnd.Column));
        }

        private class PeepholeCombiner : IPeepholeCombiner<ZapCode>
        {
            private readonly RoutineBuilder routineBuilder;

            public PeepholeCombiner(RoutineBuilder routineBuilder)
            {
                this.routineBuilder = routineBuilder;
            }

            private void BeginMatch(IEnumerable<CombinableLine<ZapCode>> lines)
            {
                enumerator = lines.GetEnumerator();
                matches = new List<CombinableLine<ZapCode>>();
            }

            private bool Match(params Predicate<CombinableLine<ZapCode>>[] criteria)
            {
                while (matches.Count < criteria.Length)
                {
                    if (enumerator.MoveNext() == false)
                        return false;

                    matches.Add(enumerator.Current);
                }

                for (int i = 0; i < criteria.Length; i++)
                {
                    if (criteria[i](matches[i]) == false)
                        return false;
                }

                return true;
            }

            private void EndMatch()
            {
                enumerator.Dispose();
                enumerator = null;

                matches = null;
            }

            private IEnumerator<CombinableLine<ZapCode>> enumerator;
            private List<CombinableLine<ZapCode>> matches;

            private CombinerResult<ZapCode> Combine1to1(string newText, PeepholeLineType? type = null, ILabel target = null)
            {
                return new CombinerResult<ZapCode>(
                    1,
                    new CombinableLine<ZapCode>[] {
                        new CombinableLine<ZapCode>(
                            matches[0].Label,
                            new ZapCode() {
                                Text = newText,
                                DebugText = matches[0].Code.DebugText,
                            },
                            target ?? matches[0].Target,
                            type ?? matches[0].Type),
                    });
            }

            private CombinerResult<ZapCode> Combine2to1(string newText, PeepholeLineType? type = null, ILabel target = null)
            {
                return new CombinerResult<ZapCode>(
                    2,
                    new CombinableLine<ZapCode>[] {
                        new CombinableLine<ZapCode>(
                            matches[0].Label,
                            new ZapCode() {
                                Text = newText,
                                DebugText = matches[0].Code.DebugText ?? matches[1].Code.DebugText,
                            },
                            target ?? matches[1].Target,
                            type ?? matches[1].Type),
                    });
            }

            private CombinerResult<ZapCode> Combine2to2(
                string newText1, string newText2,
                PeepholeLineType? type1 = null, PeepholeLineType? type2 = null,
                ILabel target1 = null, ILabel target2 = null)
            {
                return new CombinerResult<ZapCode>(
                    2,
                    new CombinableLine<ZapCode>[] {
                        new CombinableLine<ZapCode>(
                            matches[0].Label,
                            new ZapCode() {
                                Text = newText1,
                                DebugText = matches[0].Code.DebugText,
                            },
                            target1 ?? matches[0].Target,
                            type1 ?? matches[0].Type),
                        new CombinableLine<ZapCode>(
                            matches[1].Label,
                            new ZapCode() {
                                Text = newText2,
                                DebugText = matches[1].Code.DebugText,
                            },
                            target2 ?? matches[1].Target,
                            type2 ?? matches[1].Type),
                    });
            }

            private CombinerResult<ZapCode> Combine3to1(string newText, PeepholeLineType? type = null, ILabel target = null)
            {
                return new CombinerResult<ZapCode>(
                    3,
                    new CombinableLine<ZapCode>[] {
                        new CombinableLine<ZapCode>(
                            matches[0].Label,
                            new ZapCode() {
                                Text = newText,
                                DebugText = matches[0].Code.DebugText ?? matches[1].Code.DebugText ?? matches[2].Code.DebugText,
                            },
                            target ?? matches[2].Target,
                            type ?? matches[2].Type),
                    });
            }

            private static CombinerResult<ZapCode> Consume(int numberOfLines)
            {
                return new CombinerResult<ZapCode>(numberOfLines, Enumerable.Empty<CombinableLine<ZapCode>>());
            }

            private static readonly Regex equalZeroRegex = new Regex(@"^EQUAL\? (?:(?<var>[^,]+),0|0,(?<var>[^,]+))$");
            private static readonly Regex bandConstantToStackRegex = new Regex(@"^BAND (?:(?<var>[^,]+),(?<const>-?\d+)|(?<const>-?\d+),(?<var>[^,]+)) >STACK$");
            private static readonly Regex bandConstantWithStackRegex = new Regex(@"^BAND (?:STACK,(?<const>-?\d+)|(?<const>-?\d+),STACK) >(?<dest>.*)$");
            private static readonly Regex borConstantToStackRegex = new Regex(@"^BOR (?:(?<var>[^,]+),(?<const>-?\d+)|(?<const>-?\d+),(?<var>[^,]+)) >STACK$");
            private static readonly Regex borConstantWithStackRegex = new Regex(@"^BOR (?:STACK,(?<const>-?\d+)|(?<const>-?\d+),STACK) >(?<dest>.*)$");
            private static readonly Regex popToVariableRegex = new Regex(@"^POP ['>]");

            public CombinerResult<ZapCode> Apply(IEnumerable<CombinableLine<ZapCode>> lines)
            {
                System.Text.RegularExpressions.Match rm = null, rm2 = null;

                BeginMatch(lines);
                try
                {
                    if (Match(a => (rm = equalZeroRegex.Match(a.Code.Text)).Success))
                    {
                        // EQUAL? x,0 | EQUAL? 0,x => ZERO? x
                        Contract.Assume(rm != null);
                        return Combine1to1("ZERO? " + rm.Groups["var"]);
                    }

                    if (Match(a => a.Code.Text == "JUMP" && (a.Target == RTRUE || a.Target == RFALSE)))
                    {
                        // JUMP to TRUE/FALSE => RTRUE/RFALSE
                        return Combine1to1(matches[0].Target == RTRUE ? "RTRUE" : "RFALSE");
                    }

                    if (Match(a => a.Code.Text.StartsWith("PUSH "), b => b.Code.Text == "RSTACK"))
                    {
                        // PUSH + RSTACK => RFALSE/RTRUE/RETURN
                        switch (matches[0].Code.Text)
                        {
                            case "PUSH 0":
                                return Combine2to1("RFALSE", PeepholeLineType.BranchAlways, RoutineBuilder.RFALSE);
                            case "PUSH 1":
                                return Combine2to1("RTRUE", PeepholeLineType.BranchAlways, RoutineBuilder.RTRUE);
                            default:
                                return Combine2to1("RETURN " + matches[0].Code.Text.Substring(5));
                        }
                    }

                    if (Match(a => a.Code.Text.EndsWith(">STACK"), b => popToVariableRegex.IsMatch(b.Code.Text)))
                    {
                        // >STACK + POP 'dest => >dest
                        var a = matches[0].Code.Text;
                        var b = matches[1].Code.Text;
                        return Combine2to1(a.Substring(0, a.Length - 5) + b.Substring(5));
                    }

                    if (Match(a => a.Code.Text.StartsWith("PUSH "), b => popToVariableRegex.IsMatch(b.Code.Text)))
                    {
                        // PUSH + POP 'dest => SET 'dest
                        var a = matches[0].Code.Text;
                        var b = matches[1].Code.Text;
                        return Combine2to1("SET '" + b.Substring(5) + "," + a.Substring(5));
                    }

                    if (Match(a => a.Code.Text.StartsWith("INC '"), b => b.Code.Text.StartsWith("GRTR? ")))
                    {
                        string str;
                        if ((str = matches[0].Code.Text.Substring(5)) != "STACK" &&
                            matches[1].Code.Text.StartsWith("GRTR? " + str))
                        {
                            // INC 'v + GRTR? v => IGRTR? 'v
                            return Combine2to1("IGRTR? '" + matches[1].Code.Text.Substring(6));
                        }
                    }

                    if (Match(a => a.Code.Text.StartsWith("DEC '"), b => b.Code.Text.StartsWith("LESS? ")))
                    {
                        string str;
                        if ((str = matches[0].Code.Text.Substring(5)) != "STACK" &&
                            matches[1].Code.Text.StartsWith("LESS? " + str))
                        {
                            // DEC 'v + LESS? v => DLESS? 'v
                            return Combine2to1("DLESS? '" + matches[1].Code.Text.Substring(6));
                        }
                    }

                    if (Match(a => (a.Code.Text.StartsWith("EQUAL? ") || a.Code.Text.StartsWith("ZERO? ")) && a.Type == PeepholeLineType.BranchPositive,
                        b => (b.Code.Text.StartsWith("EQUAL? ") || b.Code.Text.StartsWith("ZERO? ")) && b.Type == PeepholeLineType.BranchPositive))
                    {
                        if (matches[0].Target == matches[1].Target)
                        {
                            string[] aparts, bparts;

                            if (matches[0].Code.Text.StartsWith("ZERO? "))
                            {
                                aparts = new[] { matches[0].Code.Text.Substring(6), "0" };
                            }
                            else
                            {
                                aparts = matches[0].Code.Text.Substring(7).Split(',');
                            }

                            if (matches[1].Code.Text.StartsWith("ZERO? "))
                            {
                                bparts = new[] { matches[1].Code.Text.Substring(6), "0" };
                            }
                            else
                            {
                                bparts = matches[1].Code.Text.Substring(7).Split(',');
                            }

                            if (aparts[0] == bparts[0] && aparts.Length < 4)
                            {
                                var sb = new StringBuilder(matches[0].Code.Text.Length + matches[1].Code.Text.Length);

                                if (aparts.Length + bparts.Length <= 5)
                                {
                                    // EQUAL? v,a,b /L + EQUAL? v,c /L => EQUAL? v,a,b,c /L
                                    sb.Append("EQUAL? ");
                                    sb.Append(aparts[0]);
                                    for (int i = 1; i < aparts.Length; i++)
                                    {
                                        sb.Append(',');
                                        sb.Append(aparts[i]);
                                    }
                                    for (int i = 1; i < bparts.Length; i++)
                                    {
                                        sb.Append(',');
                                        sb.Append(bparts[i]);
                                    }
                                    return Combine2to1(sb.ToString());
                                }
                                else
                                {
                                    // EQUAL? v,a,b /L + EQUAL? v,c,d /L => EQUAL? v,a,b,c /L + EQUAL? v,d /L
                                    var allRhs = aparts.Skip(1).Concat(bparts.Skip(1));

                                    sb.Append("EQUAL? ");
                                    sb.Append(aparts[0]);
                                    foreach (var rhs in allRhs.Take(3))
                                    {
                                        sb.Append(',');
                                        sb.Append(rhs);
                                    }
                                    var first = sb.ToString();

                                    sb.Length = 0;
                                    sb.Append("EQUAL? ");
                                    sb.Append(aparts[0]);
                                    foreach (var rhs in allRhs.Skip(3))
                                    {
                                        sb.Append(',');
                                        sb.Append(rhs);
                                    }
                                    var second = sb.ToString();

                                    return Combine2to2(first, second);
                                }
                            }
                        }
                    }

                    if (Match(a => a.Code.Text == "CRLF", b => b.Code.Text == "RTRUE"))
                    {
                        // combine CRLF + RTRUE into a single terminator
                        // this can be pulled through a branch and thus allows more PRINTR transformations
                        return Combine2to1("CRLF+RTRUE", PeepholeLineType.Terminator);
                    }

                    if (Match(a => a.Code.Text.StartsWith("PRINTI "), b => b.Code.Text == "CRLF+RTRUE"))
                    {
                        // PRINTI + (CRLF + RTRUE) => PRINTR
                        return Combine2to1("PRINTR " + matches[0].Code.Text.Substring(7), PeepholeLineType.HeavyTerminator);
                    }

                    // BAND v,c >STACK + ZERO? STACK /L =>
                    //     when c == 0:              simple branch
                    //     when c is a power of two: BTST v,c \L
                    if (Match(a => (rm = bandConstantToStackRegex.Match(a.Code.Text)).Success,
                              b => b.Code.Text == "ZERO? STACK"))
                    {
                        var variable = rm.Groups["var"].Value;
                        Contract.Assume(variable != null);
                        var constantValue = int.Parse(rm.Groups["const"].Value);

                        if (constantValue == 0)
                        {
                            if (rm.Groups["var"].Value != "STACK")
                            {
                                if (matches[1].Type == PeepholeLineType.BranchPositive)
                                    return Combine2to1("JUMP", PeepholeLineType.BranchAlways, matches[1].Target);
                                else
                                    return Consume(2);
                            }
                        }
                        else if ((constantValue & (constantValue - 1)) == 0)
                        {
                            PeepholeLineType oppositeType;
                            if (matches[1].Type == PeepholeLineType.BranchPositive)
                                oppositeType = PeepholeLineType.BranchNegative;
                            else
                                oppositeType = PeepholeLineType.BranchPositive;

                            return Combine2to1("BTST " + variable + "," + constantValue, oppositeType);
                        }
                    }

                    // BAND v,c1 >STACK + BAND STACK,c2 >dest => BAND v,(c1&c2) >dest
                    if (Match(a => (rm = bandConstantToStackRegex.Match(a.Code.Text)).Success,
                              b => (rm2 = bandConstantWithStackRegex.Match(b.Code.Text)).Success))
                    {
                        var variable = rm.Groups["var"].Value;
                        var dest = rm2.Groups["dest"].Value;
                        Contract.Assume(variable != null);
                        Contract.Assume(dest != null);
                        var constant1 = int.Parse(rm.Groups["const"].Value);
                        var constant2 = int.Parse(rm2.Groups["const"].Value);
                        var combined = constant1 & constant2;
                        return Combine2to1("BAND " + variable + "," + combined + " >" + dest);
                    }

                    // BOR v,c1 >STACK + BOR STACK,c2 >dest => BOR v,(c1|c2) >dest
                    if (Match(a => (rm = borConstantToStackRegex.Match(a.Code.Text)).Success,
                              b => (rm2 = borConstantWithStackRegex.Match(b.Code.Text)).Success))
                    {
                        var variable = rm.Groups["var"].Value;
                        var dest = rm2.Groups["dest"].Value;
                        Contract.Assume(variable != null);
                        Contract.Assume(dest != null);
                        var constant1 = int.Parse(rm.Groups["const"].Value);
                        var constant2 = int.Parse(rm2.Groups["const"].Value);
                        var combined = constant1 | constant2;
                        return Combine2to1("BOR " + variable + "," + combined + " >" + dest);
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
                return new ZapCode() { Text = "JUMP" };
            }

            public bool AreIdentical(ZapCode a, ZapCode b)
            {
                return a.Text == b.Text;
            }

            public ZapCode MergeIdentical(ZapCode a, ZapCode b)
            {
                return new ZapCode()
                {
                    Text = a.Text,
                    DebugText = a.DebugText ?? b.DebugText,
                };
            }

            public SameTestResult AreSameTest(ZapCode a, ZapCode b)
            {
                // if the stack is involved, all bets are off
                if (a.Text.Contains("STACK") || b.Text.Contains("STACK"))
                    return SameTestResult.Unrelated;

                // if the instructions are identical, they must be the same test
                if (a.Text == b.Text)
                    return SameTestResult.SameTest;

                /* otherwise, they can be related if 'a' is a store+branch instruction
                 * and 'b' is ZERO? testing the result stored by 'a'. the z-machine's
                 * store+branch instructions all branch upon storing a nonzero value,
                 * so we always return OppositeTest in this case. */
                if (b.Text.StartsWith("ZERO? ") && a.Text.EndsWith(">" + b.Text.Substring(6)))
                    return SameTestResult.OppositeTest;

                return SameTestResult.Unrelated;
            }

            public ControlsConditionResult ControlsConditionalBranch(ZapCode a, ZapCode b)
            {
                /* if 'a' pushes a constant and 'b' is ZERO? testing the stack, the
                 * answer depends on the value of the constant. */
                int value;
                if (a.Text.StartsWith("PUSH ") && int.TryParse(a.Text.Substring(5), out value) &&
                    b.Text == "ZERO? STACK")
                {
                    if (value == 0)
                        return ControlsConditionResult.CausesBranchIfPositive;
                    else
                        return ControlsConditionResult.CausesNoOpIfPositive;
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