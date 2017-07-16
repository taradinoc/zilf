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

        const byte T_NUM_BYTE = 0;
        const byte T_NUM_WORD = 1;
        const byte T_OP_BYTE = 2;
        const byte T_OP_WORD = 3;

        protected const string INDENT = "\t";

        public TableBuilder([NotNull] string name)
        {
            Name = name;
        }

        [NotNull]
        public string Name { get; }

        public int Size
        {
            get { return size; }
        }

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

        public void WriteTo(TextWriter writer)
        {
            bool wasWord = false;
            int lineCount = 0, ni = 0, oi = 0;
            for (int i = 0; i < types.Count; i++)
            {
                byte t = types[i];
                bool isWord = (t & 1) != 0;
                bool isOperand = (t & 2) != 0;

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
                    writer.Write(operandValues[oi++]);
                else
                    writer.Write(numericValues[ni++]);

                wasWord = isWord;
                lineCount++;
            }

            writer.WriteLine();
        }
    }
}