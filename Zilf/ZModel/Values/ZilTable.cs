/* Copyright 2010, 2015 Jesse McGrew
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
using System.Diagnostics.Contracts;
using System.Text;
using Zilf.Interpreter;
using Zilf.Interpreter.Values;
using Zilf.Language;

namespace Zilf.ZModel.Values
{
    [BuiltinType(StdAtom.TABLE, PrimType.TABLE)]
    class ZilTable : ZilObject
    {
        private readonly TableFlags flags;

        private int repetitions;
        private ZilObject[] initializer;
        private ZilObject[] pattern;
        private int[] elementToByteOffsets;

        public ZilTable(int repetitions, ZilObject[] initializer, TableFlags flags, ZilObject[] pattern)
        {
            Contract.Requires(repetitions >= 0);
            Contract.Requires(repetitions > 0 || initializer == null || initializer.Length == 0);

            this.repetitions = repetitions;
            this.initializer = (initializer != null && initializer.Length > 0) ? initializer : null;
            this.flags = flags;
            this.pattern = pattern;
        }

        [ChtypeMethod]
        public ZilTable(ZilTable other)
            : this(other.repetitions,
            other.initializer == null ? null : (ZilObject[])other.initializer.Clone(),
            other.flags,
            other.pattern == null ? null : (ZilObject[])other.pattern.Clone())
        {
            Contract.Requires(other != null);

            this.SourceLine = other.SourceLine;
        }

        [ContractInvariantMethod]
        private void ObjectInvariant()
        {
            Contract.Invariant(repetitions >= 0);
            Contract.Invariant(repetitions > 0 || initializer == null);
            Contract.Invariant(initializer == null || initializer.Length > 0);
        }

        public string Name { get; set; }

        public int ElementCount
        {
            // TODO: account for initial length markers?

            [Pure]
            get
            {
                Contract.Ensures(Contract.Result<int>() >= 0);

                if (initializer != null)
                {
                    return repetitions * initializer.Length;
                }
                else
                {
                    return repetitions;
                }
            }
        }

        public TableFlags Flags
        {
            get { return flags; }
        }

        public ZilObject[] Pattern
        {
            get { return pattern; }
        }

        public void CopyTo<T>(T[] array, Func<ZilObject, T> convert, T defaultFiller)
        {
            Contract.Requires(array != null);
            Contract.Requires(array.Length >= ElementCount);
            Contract.Requires(convert != null);

            if (initializer != null)
            {
                for (int i = 0; i < repetitions; i++)
                {
                    for (int j = 0; j < initializer.Length; j++)
                    {
                        array[i * initializer.Length + j] = convert(initializer[j]);
                    }
                }
            }
            else
            {
                for (int i = 0; i < repetitions; i++)
                    array[i] = defaultFiller;
            }
        }

        private string ToString(Func<ZilObject, string> convert)
        {
            StringBuilder sb = new StringBuilder();

            sb.Append("#TABLE (");
            sb.Append(repetitions);
            sb.Append(" (");

            int pos = sb.Length;

            if ((flags & TableFlags.Byte) != 0)
                sb.Append("BYTE ");
            if ((flags & TableFlags.ByteLength) != 0)
                sb.Append("BYTELENGTH ");
            if ((flags & TableFlags.Lexv) != 0)
                sb.Append("LEXV ");
            if ((flags & TableFlags.WordLength) != 0)
                sb.Append("WORDLENGTH ");
            if ((flags & TableFlags.Pure) != 0)
                sb.Append("PURE ");
            if ((flags & TableFlags.TempTable) != 0)
                sb.Append("TEMP-TABLE ");

            if (pattern != null)
            {
                sb.Append("PATTERN ");
                sb.Append(convert(new ZilList(pattern)));
                sb.Append(' ');
            }

            if (sb.Length > pos)
                sb.Length--;

            sb.Append(") (");
            if (initializer != null)
            {
                bool first = true;
                foreach (ZilObject obj in initializer)
                {
                    if (!first)
                        sb.Append(' ');

                    first = false;
                    sb.Append(convert(obj));
                }
            }
            sb.Append(')');

            sb.Append(')');
            return sb.ToString();
        }

        public override string ToString()
        {
            return ToString(zo => zo.ToString());
        }

        protected override string ToStringContextImpl(Context ctx, bool friendly)
        {
            return ToString(zo => zo.ToStringContext(ctx, friendly));
        }

        public override ZilAtom GetTypeAtom(Context ctx)
        {
            return ctx.GetStdAtom(StdAtom.TABLE);
        }

        public override PrimType PrimType
        {
            get { return PrimType.TABLE; }
        }

        public override ZilObject GetPrimitive(Context ctx)
        {
            return this;
        }

        private bool IsWord(Context ctx, int index)
        {
            // TODO: account for initial length markers?

            if (index < 0 || index >= ElementCount)
                throw new ArgumentOutOfRangeException("index");

            if ((flags & TableFlags.Lexv) != 0)
            {
                // word-byte-byte repeating
                return index % 3 == 0;
            }

            if (pattern != null && pattern.Length > 0)
            {
                ZilAtom atom;
                ZilVector rest;
                if (index >= pattern.Length - 1 && (rest = pattern[pattern.Length - 1] as ZilVector) != null)
                {
                    index -= pattern.Length - 1;
                    atom = rest[index % (rest.GetLength() - 1) + 1] as ZilAtom;
                    return !(atom != null && atom.StdAtom == StdAtom.BYTE);
                }
                else if ((atom = pattern[index % pattern.Length] as ZilAtom) != null)
                {
                    return atom.StdAtom != StdAtom.BYTE;
                }
                else
                {
                    throw new NotImplementedException("malformed pattern");
                }
            }

            if (initializer != null)
            {
                switch (initializer[index % initializer.Length].GetTypeAtom(ctx).StdAtom)
                {
                    case StdAtom.BYTE:
                        return false;

                    case StdAtom.WORD:
                        return true;

                    // no default, fall through
                }
            }

            return (flags & TableFlags.Byte) == 0;
        }

        private void ExpandInitializer(ZilObject defaultValue)
        {
            Contract.Requires(defaultValue != null);
            Contract.Ensures(repetitions >= 0 && repetitions <= 1);
            Contract.Ensures(initializer == null || initializer.Length > 0);
            Contract.Ensures(ElementCount == Contract.OldValue(ElementCount));

            if (repetitions == 0)
            {
                initializer = null;
            }
            else if (initializer == null)
            {
                initializer = new ZilObject[repetitions];
                for (int i = 0; i < repetitions; i++)
                    initializer[i] = defaultValue;
                repetitions = 1;
            }
            else
            {
                var newInitializer = new ZilObject[initializer.Length * repetitions];
                for (int i = 0; i < newInitializer.Length; i++)
                    newInitializer[i] = initializer[i % initializer.Length];
                initializer = newInitializer;
                repetitions = 1;
            }
        }

        private void ExpandPattern(Context ctx, int index, bool insert)
        {
            if (pattern == null || pattern.Length <= index)
            {
                var byteAtom = ctx.GetStdAtom(StdAtom.BYTE);
                var wordAtom = ctx.GetStdAtom(StdAtom.WORD);

                var length = ElementCount;
                if (insert)
                    length++;

                var newPattern = new ZilObject[length];

                for (int i = 0, j = 0; i < newPattern.Length; i++, j++)
                {
                    newPattern[i] = IsWord(ctx, j) ? wordAtom : byteAtom;

                    if (insert && i == index)
                    {
                        newPattern[i + 1] = newPattern[i];
                        i++;
                    }
                }

                pattern = newPattern;
            }
        }

        private int? ByteOffsetToIndex(Context ctx, int offset)
        {
            Contract.Requires(ctx != null);
            Contract.Requires(offset >= 0);
            Contract.Ensures(Contract.Result<int?>() == null || (Contract.Result<int?>().Value >= 0 && Contract.Result<int?>().Value < ElementCount));

            // TODO: account for initial length markers?

            // initialize cache if necessary
            if (elementToByteOffsets == null)
            {
                elementToByteOffsets = new int[ElementCount];
                for (int i = 0, nextOffset = 0; i < elementToByteOffsets.Length; i++)
                {
                    elementToByteOffsets[i] = nextOffset;

                    if (IsWord(ctx, i))
                        nextOffset += 2;
                    else
                        nextOffset++;
                }
            }

            // binary search to find the element
            var index = Array.BinarySearch(elementToByteOffsets, offset);
            if (index >= 0)
                return index;
            else
                return null;
        }

        public ZilObject GetWord(Context ctx, int offset)
        {
            Contract.Requires(ctx != null);
            Contract.Requires(offset >= 0);

            // convert word offset to byte offset
            offset *= 2;

            var index = ByteOffsetToIndex(ctx, offset);
            if (index == null)
                throw new ArgumentException(string.Format("No element at offset {0}", offset));
            if (!IsWord(ctx, index.Value))
                throw new ArgumentException(string.Format("Element at byte offset {0} is not a word", offset));

            if (initializer == null)
                return null;

            return initializer[index.Value % initializer.Length];
        }

        public void PutWord(Context ctx, int offset, ZilObject value)
        {
            Contract.Requires(ctx != null);
            Contract.Requires(offset >= 0);
            Contract.Requires(value != null);

            // convert word offset to byte offset
            offset *= 2;

            var index = ByteOffsetToIndex(ctx, offset);
            if (index == null)
                throw new ArgumentException(string.Format("No element at offset {0}", offset));

            if (!IsWord(ctx, index.Value))
            {
                // we may be able to replace 2 bytes with a word
                var index2 = ByteOffsetToIndex(ctx, offset + 1);
                if (index2 == null || IsWord(ctx, index2.Value))
                    throw new ArgumentException(string.Format("Element at byte offset {0} is not a word", offset));

                if (initializer == null || repetitions > 1)
                    ExpandInitializer(ctx.FALSE);

                var newInitializer = new ZilObject[initializer.Length - 1];
                Array.Copy(initializer, newInitializer, index.Value);
                Array.Copy(initializer, index.Value + 2, newInitializer, index.Value + 1, initializer.Length - index.Value - 2);
                initializer = newInitializer;
                elementToByteOffsets = null;

                initializer[index.Value] = new ZilWord(value);
            }
            else
            {
                if (initializer == null || repetitions > 1)
                    ExpandInitializer(ctx.FALSE);

                initializer[index.Value] = value;
            }
        }

        public ZilObject GetByte(Context ctx, int offset)
        {
            Contract.Requires(ctx != null);
            Contract.Requires(offset >= 0);

            var index = ByteOffsetToIndex(ctx, offset);
            if (index == null)
                throw new ArgumentException(string.Format("No element at offset {0}", offset));
            if (IsWord(ctx, index.Value))
                throw new ArgumentException(string.Format("Element at byte offset {0} is not a byte", offset));

            if (initializer == null)
                return null;

            return initializer[index.Value % initializer.Length];
        }

        public void PutByte(Context ctx, int offset, ZilObject value)
        {
            Contract.Requires(ctx != null);
            Contract.Requires(offset >= 0);
            Contract.Requires(value != null);

            if (initializer == null || repetitions > 1)
                ExpandInitializer(ctx.FALSE);

            var index = ByteOffsetToIndex(ctx, offset);
            bool second = false;
            if (index == null)
            {
                // might be the second byte of a word
                index = ByteOffsetToIndex(ctx, offset - 1);
                if (index != null && IsWord(ctx, index.Value))
                {
                    second = true;
                }
                else
                {
                    throw new ArgumentException(string.Format("No element at offset {0}", offset));
                }
            }

            if (IsWord(ctx, index.Value))
            {
                // split the word into 2 bytes
                var newInitializer = new ZilObject[initializer.Length + 1];
                Array.Copy(initializer, newInitializer, index.Value);
                Array.Copy(initializer, index.Value + 1, newInitializer, index.Value + 2, initializer.Length - index.Value - 1);
                initializer = newInitializer;

                if (pattern != null)
                    ExpandPattern(ctx, index.Value, true);

                elementToByteOffsets = null;

                var zeroByte = ctx.ChangeType(ZilFix.Zero, ctx.GetStdAtom(StdAtom.BYTE));

                if (second)
                {
                    initializer[index.Value] = zeroByte;
                    initializer[index.Value + 1] = value;

                    // remember the index we actually used
                    index++;
                }
                else
                {
                    initializer[index.Value] = value;
                    initializer[index.Value + 1] = zeroByte;
                }
            }
            else
            {
                initializer[index.Value] = value;
            }

            if (IsWord(ctx, index.Value))
            {
                ExpandPattern(ctx, index.Value, false);
                pattern[index.Value] = ctx.GetStdAtom(StdAtom.BYTE);
            }
        }
    }
}
