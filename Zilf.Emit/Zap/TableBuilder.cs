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
using JetBrains.Annotations;

namespace Zilf.Emit.Zap
{
    class TableBuilder : ConstantOperandBase, ITableBuilder
    {
        readonly List<short> numericValues = new List<short>();
        readonly List<IOperand> operandValues = new List<IOperand>();
        readonly List<byte> types = new List<byte>();
        int size;

        const byte WORD_FLAG = 1;
        const byte OPERAND_FLAG = 2;

        const byte T_NUM_BYTE = 0;
        const byte T_NUM_WORD = WORD_FLAG;
        const byte T_OP_BYTE = OPERAND_FLAG;
        const byte T_OP_WORD = OPERAND_FLAG | WORD_FLAG;

        protected const string INDENT = "\t";

        public TableBuilder([NotNull] string name)
        {
            Name = name;
        }

        [NotNull]
        public string Name { get; }

        public int Size => size;

        public void AddByte(byte value)
        {
            types.Add(T_NUM_BYTE);
            numericValues.Add(value);
            size++;
        }

        public void AddByte(IOperand value)
        {
            types.Add(T_OP_BYTE);
            operandValues.Add(value);
            size++;
        }

        public void AddShort(short value)
        {
            types.Add(T_NUM_WORD);
            numericValues.Add(value);
            size += 2;
        }

        public void AddShort(IOperand value)
        {
            types.Add(T_OP_WORD);
            operandValues.Add(value);
            size += 2;
        }

        public override string ToString()
        {
            return Name;
        }

        public void WriteTo([NotNull] TextWriter writer)
        {
            bool wasWord = false;
            int lineCount = 0, ni = 0, oi = 0;
            foreach (byte t in types)
            {
                bool isWord = (t & WORD_FLAG) != 0;
                bool isOperand = (t & OPERAND_FLAG) != 0;

                if (lineCount == 0 || lineCount == 10 || isWord != wasWord)
                {
                    if (ni + oi != 0)
                        writer.WriteLine();
                    writer.Write(isWord ? INDENT + ".WORD " : INDENT + ".BYTE ");
                    lineCount = 0;
                }
                else
                    writer.Write(',');

                if (isOperand)
                    writer.Write(operandValues[oi++].StripIndirect());
                else
                    writer.Write(numericValues[ni++]);

                wasWord = isWord;
                lineCount++;
            }

            writer.WriteLine();
        }
    }
}