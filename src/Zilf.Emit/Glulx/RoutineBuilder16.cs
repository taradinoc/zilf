/* Copyright 2010-2025 Tara McGrew
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
using System.Security.AccessControl;
using System.Text;

namespace Zilf.Emit.Glulx
{
    internal sealed class RoutineBuilder16 : RoutineBuilder
    {
        public RoutineBuilder16(GameBuilder gameBuilder, string name, bool entryPoint)
            : base(gameBuilder, name, entryPoint)
        {
        }

        public override string ToString() => $"({Name} / PACKING_FACTOR)";

        public override void EmitBinary(BinaryOp op, IOperand left, IOperand right, IVariable? result)
        {
            switch (op)
            {
                case BinaryOp.Add:
                    EmitBinary16(nameof(RuntimeLib16.add16), left, right, result);
                    return;
                case BinaryOp.Sub:
                    EmitBinary16(nameof(RuntimeLib16.sub16), left, right, result);
                    return;
                case BinaryOp.Mul:
                    EmitBinary16(nameof(RuntimeLib16.mul16), left, right, result);
                    return;
                case BinaryOp.Div:
                    EmitBinary16(nameof(RuntimeLib16.div16), left, right, result);
                    return;
                case BinaryOp.Mod:
                    EmitBinary16(nameof(RuntimeLib16.mod16), left, right, result);
                    return;
                case BinaryOp.And:
                    EmitBinary16(nameof(RuntimeLib16.band16), left, right, result);
                    return;
                case BinaryOp.Or:
                    EmitBinary16(nameof(RuntimeLib16.bor16), left, right, result);
                    return;
                case BinaryOp.GetWord when PotentialHeaderAccess(left):
                    EmitBinary16(nameof(RuntimeLib16.getword16), left, right, result);
                    return;
                case BinaryOp.GetByte when PotentialHeaderAccess(left):
                    EmitBinary16(nameof(RuntimeLib16.getbyte16), left, right, result);
                    return;
            }

            base.EmitBinary(op, left, right, result);
        }

        private static bool PotentialHeaderAccess(IOperand table)
        {
            if (table is not IConstantOperand)
                return true;

            if (table is INumericOperand { Value: < 64 })
                return true;

            // we don't expect games to access the header through negative offsets
            // or indexing a non-numeric constant
            return false;
        }

        public override void EmitUnary(UnaryOp op, IOperand value, IVariable? result)
        {
            switch (op)
            {
                case UnaryOp.Not:
                    EmitUnary16(nameof(RuntimeLib16.bcom16), value, result);
                    return;
                case UnaryOp.Neg:
                    EmitUnary16(nameof(RuntimeLib16.neg16), value, result);
                    return;
                case UnaryOp.LoadIndirect:
                    Emit($"sub {FormatLoad(value)} 1 -> push", "mul");
                    Emit($"aload global_variables pop -> push", "aload");
                    Emit($"bitand pop 0xFFFF -> {FormatStore(result!)}");
                    return;
            }

            base.EmitUnary(op, value, result);
        }

        public override void EmitTernary(TernaryOp op, IOperand left, IOperand center, IOperand right, IVariable? result)
        {
            switch (op)
            {
                case TernaryOp.PutWord:
                    EmitTernary16(nameof(RuntimeLib16.putword16), left, center, right);
                    return;
            }

            base.EmitTernary(op, left, center, right, result);
        }

        public override void EmitNullary(NullaryOp op, IVariable? result)
        {
            switch (op)
            {
                case NullaryOp.ShowStatus:
                    Emit("; TODO: draw status line");
                    return;
            }

            base.EmitNullary(op, result);
        }

        void EmitBinary16(string funcName, IOperand left, IOperand right, IVariable? result)
        {
            Emit($"callfii {gameBuilder.RuntimeLib.Use(funcName)} {FormatLoad(left)} {FormatLoad(right)} -> {FormatStore(result)}", "callfii");
        }

        void EmitUnary16(string funcName, IOperand value, IVariable? result)
        {
            Emit($"callfi {gameBuilder.RuntimeLib.Use(funcName)} {FormatLoad(value)} -> {FormatStore(result)}", "callfi");
        }

        void EmitTernary16(string funcName, IOperand first, IOperand second, IOperand third)
        {
            Emit($"callfiii {gameBuilder.RuntimeLib.Use(funcName)} {FormatLoad(first)} {FormatLoad(second)} {FormatLoad(third)}", "callfiii");
        }

        public override void Branch(Condition cond, IOperand? left, IOperand? right, ILabel label, bool polarity)
        {
            if (cond == Condition.Less || cond == Condition.Greater)
            {
                string opcode = cond switch
                {
                    Condition.Less => polarity ? "jlt" : "jge",
                    Condition.Greater => polarity ? "jgt" : "jle",
                    _ => throw new NotImplementedException($"Condition {cond} not implemented")
                };

                Emit($"callfii {gameBuilder.RuntimeLib.Use(nameof(RuntimeLib16.cmp16))} {FormatLoad(left!)} {FormatLoad(right!)} -> push", "callfii");
                AddLine($"{opcode} pop 0", opcode, label, PeepholeLineType.BranchPositive);
                return;
            }

            base.Branch(cond, left, right, label, polarity);
        }

        public override void EmitPrint(string text, bool crlfRtrue)
        {
            var strOperand = gameBuilder.MakeOperand(text);
            Emit($"callfi {gameBuilder.RuntimeLib.Use(nameof(RuntimeLib16.print_packed_string))} {FormatLoad(strOperand)}", "callfi");
            if (crlfRtrue)
            {
                Emit("streamchar 10", "streamchar");
                AddLine("return 1", "return", Label.RTRUE, PeepholeLineType.BranchAlways);
            }
        }

        public override void EmitPrint(PrintOp op, IOperand value)
        {
            switch (op)
            {
                case PrintOp.Number:
                    Emit($"sexs {FormatLoad(value)} -> push");
                    Emit($"streamnum pop", "streamnum");
                    return;
                case PrintOp.PackedAddr:
                    Emit($"callfi {gameBuilder.RuntimeLib.Use(nameof(RuntimeLib16.print_packed_string))} {FormatLoad(value)}", "callfi");
                    return;
            }

            base.EmitPrint(op, value);
        }

        protected override void WriteOptionalPreamble(StringBuilder sb)
        {
            sb.AppendLine("align PACKING_FACTOR");
        }

        protected override void WriteOptionalGlkSetup(PeepholeBuffer<GlulxCode> peep)
        {
            GlulxCode gc;
            gc.Text = $"callf {gameBuilder.RuntimeLib.Use(nameof(RuntimeLib16V3.init_status_line))}";
            gc.Opcode = "callf";
            gc.OriginalType = PeepholeLineType.Plain;

            peep.AddLine(gc, null, PeepholeLineType.Plain);
        }

        protected override string FormatDirectCall(IOperand routine)
        {
            return routine switch
            {
                RoutineBuilder16 r => r.Name,
                _ => base.FormatDirectCall(routine)
            };
        }

        public override bool TryEmitLowCoreGetTable(string field, IVariable resultStorage)
        {
            switch (field)
            {
                case "SERIAL":
                    // The serial data will be loaded through Glulx16's checked array load function,
                    // which masks addresses to 16 bits and traps access to the Z-machine header,
                    // so this is just the usual Z-machine location of the serial number.
                    Emit($"copy 18 -> {FormatStore(resultStorage)}", "copy");
                    return true;
            }

            return base.TryEmitLowCoreGetTable(field, resultStorage);
        }
    }
}
