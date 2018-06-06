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

using System.Collections.Generic;
using System.IO;
using Zapf.Parsing.Instructions;

namespace Dezapf
{
    enum OperandType : byte
    {
        Word = 0,
        Byte = 1,
        Variable = 2,
/*
        Omitted = 3,
*/
    }

    enum BranchType : byte
    {
        None = 0xff,
        LongNegative = 0x00,
        ShortNegative = 0x40,
        LongPositive = 0x80,
        ShortPositive = 0xc0,
    }

    class Instruction : Chunk
    {
        ushort op;
        ushort[] operands;
        OperandType[] operandTypes;
        BranchType branchType;
        short branchOffset;
        byte? storeTarget;
        ushort[] encodedText;

        public ushort Op => op;
        public ushort[] Operands => (ushort[])operands.Clone();
        public OperandType[] OperandTypes => (OperandType[])operandTypes.Clone();
        public BranchType BranchType => branchType;
        public short BranchOffset => branchOffset;
        public byte? StoreTarget => storeTarget;

        public ushort[] EncodedText => (ushort[])encodedText?.Clone();

        Instruction(int pc, int length)
            : base(pc, length)
        {
        }

        public static Instruction Decode(Context ctx, BinaryReader rdr, int pc)
        {
            int length = 1;

            // read opcode number and look up opcode info
            ushort op = rdr.ReadByte();
            if (op == 190 && ctx.ZVersion >= 5)
            {
                op = (ushort)(256 + rdr.ReadByte());
                length++;
            }

            // canonicalize opcode number and decode operand types
            OperandType[] otypes;

            if (op < 128)
            {
                // long, 2OP
                otypes = new OperandType[2];

                if ((op & 0x40) == 0)
                    otypes[0] = OperandType.Byte;
                else
                    otypes[0] = OperandType.Variable;

                if ((op & 0x20) == 0)
                    otypes[1] = OperandType.Byte;
                else
                    otypes[1] = OperandType.Variable;

                op &= 0x1f;     // clear operand type bits
            }
            else if (op < 192)
            {
                // short
                if (op < 176)
                {
                    // 1OP
                    otypes = new[] { (OperandType)((op >> 4) & 3) };
                    op &= 0xcf;     // clear operand type bits
                }
                else
                {
                    // 0OP
                    otypes = new OperandType[] { };
                }
            }
            else
            {
                // variable
                if (op == 236 || op == 250)
                    otypes = new OperandType[8];
                else
                    otypes = new OperandType[4];

                if (op < 224)
                {
                    // 2OP
                    op &= 0x1f;      // translate to 2OP opcode number
                }
                // otherwise VAR (op < 256) or EXT

                for (int i = 0; 4 * i < otypes.Length; i++)
                {
                    byte b = rdr.ReadByte();
                    length++;
                    otypes[4 * i] = (OperandType)((b >> 6) & 3);
                    otypes[4 * i + 1] = (OperandType)((b >> 4) & 3);
                    otypes[4 * i + 2] = (OperandType)((b >> 2) & 3);
                    otypes[4 * i + 3] = (OperandType)(b & 3);
                }
            }

            // look up opcode info
            ZOpAttribute attr = ctx.GetOpcodeInfo(op);
            if (attr == null)
                return null;

            // read operand values
            ushort[] operands = new ushort[otypes.Length];

            for (int i = 0; i < otypes.Length; i++)
            {
                switch (otypes[i])
                {
                    case OperandType.Word:
                        operands[i] = rdr.ReadZWord();
                        length += 2;
                        break;
                    case OperandType.Byte:
                    case OperandType.Variable:
                        operands[i] = rdr.ReadByte();
                        length++;
                        break;
                }
            }

            // read store/branch targets
            byte? storeTarget = null;
            if ((attr.Flags & ZOpFlags.Store) != 0)
            {
                storeTarget = rdr.ReadByte();
                length++;
            }

            BranchType branchType;
            short branchOffset = 0;
            if ((attr.Flags & ZOpFlags.Branch) != 0)
            {
                byte b = rdr.ReadByte();
                length++;
                branchType = (BranchType)(b & 0xc0);

                switch (branchType)
                {
                    case BranchType.LongNegative:
                    case BranchType.LongPositive:
                        // signed 14-bit offset: last 6 bits of b followed by all 8 of the next byte
                        // we shift right by 2 to extend the sign from 14 to 16 bits
                        short s = (short)(((b & 0x3f) << 10) | (rdr.ReadByte() << 2));
                        length++;
                        branchOffset = (short)(s >> 2);
                        break;
                    case BranchType.ShortNegative:
                    case BranchType.ShortPositive:
                        // unsigned 6-bit offset: last 6 bits of b
                        branchOffset = (short)(b & 0x3f);
                        break;
                }
            }
            else
            {
                branchType = BranchType.None;
            }

            // read text
            ushort[] encodedText = null;
            if ((attr.Flags & ZOpFlags.String) != 0)
            {
                List<ushort> list = new List<ushort>();
                ushort w;
                do
                {
                    w = rdr.ReadZWord();
                    length += 2;
                    list.Add(w);
                } while ((w & 0x8000) == 0);

                encodedText = list.ToArray();
            }

            // done
            var result = new Instruction(pc, length)
            {
                branchOffset = branchOffset,
                branchType = branchType,
                encodedText = encodedText,
                op = op,
                operandTypes = otypes,
                operands = operands,
                storeTarget = storeTarget
            };
            return result;
        }

        public override void WriteTo(TextWriter writer, Context ctx)
        {
            ctx.OutputStyle.FormatInstruction(writer, ctx, this);
        }

        public override bool WantNewParagraph(Chunk previous)
        {
            return !(previous is Instruction);
        }
    }
}
