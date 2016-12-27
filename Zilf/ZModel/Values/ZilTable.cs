/* Copyright 2010-2016 Jesse McGrew
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
    /// <summary>
    /// Converts a table element from <see cref="ZilObject"/> to another type.
    /// </summary>
    /// <typeparam name="T">The destination type.</typeparam>
    /// <param name="tableElement">The original table element.</param>
    /// <param name="isWord"><b>true</b> if the table element is a word; <b>false</b> if it's a byte.</param>
    /// <returns>The converted element.</returns>
    delegate T TableToArrayElementConverter<T>(ZilObject tableElement, bool isWord);

    [BuiltinType(StdAtom.TABLE, PrimType.TABLE)]
    abstract class ZilTable : ZilObject, IProvideStructureForDeclCheck
    {
        public string Name { get; set; }

        public abstract TableFlags Flags { get; }
        public abstract int ElementCount { get; }

        public abstract ZilObject GetWord(Context ctx, int offset);
        public abstract ZilObject GetByte(Context ctx, int offset);
        public abstract void PutWord(Context ctx, int offset, ZilObject value);
        public abstract void PutByte(Context ctx, int offset, ZilObject value);

        public abstract void CopyTo<T>(T[] array, TableToArrayElementConverter<T> convert, T defaultFiller, Context ctx);
        protected abstract ZilTable AsNewTable();
        public abstract ZilTable OffsetByBytes(Context ctx, int bytesToSkip);

        protected abstract string ToString(Func<ZilObject, string> convert);

        public static ZilTable Create(int repetitions, ZilObject[] initializer, TableFlags flags, ZilObject[] pattern)
        {
            Contract.Requires(repetitions >= 0);
            Contract.Requires(repetitions > 0 || initializer == null || initializer.Length == 0);

            return new OriginalTable(repetitions, initializer, flags, pattern);
        }

        [ChtypeMethod]
        public static ZilTable FromTable(Context ctx, ZilTable other)
        {
            Contract.Requires(ctx != null);
            Contract.Requires(other != null);

            return other.AsNewTable();
        }

        public sealed override string ToString()
        {
            return ToString(zo => zo.ToString());
        }

        protected sealed override string ToStringContextImpl(Context ctx, bool friendly)
        {
            return ToString(zo => zo.ToStringContext(ctx, friendly));
        }

        public sealed override StdAtom StdTypeAtom => StdAtom.TABLE;

        public sealed override PrimType PrimType => PrimType.TABLE;

        public sealed override ZilObject GetPrimitive(Context ctx)
        {
            return this;
        }

        IStructure IProvideStructureForDeclCheck.GetStructureForDeclCheck(Context ctx)
        {
            var array = new ZilObject[ElementCount];
            CopyTo(array, (zo, isWord) => zo, ctx.FALSE, ctx);
            return new ZilVector(array);
        }

        [BuiltinAlternate(typeof(ZilTable))]
        sealed class OriginalTable : ZilTable
        {
            TableFlags flags;

            int repetitions;
            ZilObject[] initializer;
            ZilObject[] pattern;
            int[] elementToByteOffsets;

            public OriginalTable(int repetitions, ZilObject[] initializer, TableFlags flags, ZilObject[] pattern)
            {
                Contract.Requires(repetitions >= 0);
                Contract.Requires(repetitions > 0 || initializer == null || initializer.Length == 0);

                this.repetitions = repetitions;
                this.initializer = initializer?.Length > 0 ? initializer : null;
                this.flags = flags;
                this.pattern = pattern;
            }

            [ChtypeMethod]
            public OriginalTable(OriginalTable other)
            : this(other.repetitions,
                   (ZilObject[])other.initializer?.Clone(),
                   other.flags,
                   (ZilObject[])other.pattern?.Clone())
            {
                Contract.Requires(other != null);

                this.SourceLine = other.SourceLine;
            }

            [ContractInvariantMethod]
            void ObjectInvariant()
            {
                Contract.Invariant(repetitions >= 0);
                Contract.Invariant(repetitions > 0 || initializer == null);
                Contract.Invariant(initializer == null || initializer.Length > 0);
            }

            [Pure]
            bool HasLengthPrefix => (flags & (TableFlags.ByteLength | TableFlags.WordLength)) != 0;
            [Pure]
            int ElementCountWithoutLength => initializer == null ? repetitions : repetitions * initializer.Length;
            [Pure]
            public override int ElementCount => ElementCountWithoutLength + (HasLengthPrefix ? 1 : 0);

            public override TableFlags Flags
            {
                get { return flags; }
            }

            public ZilObject[] Pattern
            {
                get { return pattern; }
            }

            public override void CopyTo<T>(T[] array, TableToArrayElementConverter<T> convert, T defaultFiller, Context ctx)
            {
                int start;

                if (HasLengthPrefix)
                {
                    array[0] = convert(new ZilFix(ElementCountWithoutLength), (flags & TableFlags.ByteLength) == 0);
                    start = 1;
                }
                else
                {
                    start = 0;
                }

                if (initializer != null)
                {
                    int i = 0;

                    for (int r = 0; r < repetitions; r++)
                    {
                        for (int j = 0; j < initializer.Length; j++)
                        {
                            array[start + i] = convert(initializer[j], IsWord(ctx, i));
                            i++;
                        }
                    }
                }
                else
                {
                    for (int i = 0; i < repetitions; i++)
                        array[start + i] = defaultFiller;
                }
            }

            protected override string ToString(Func<ZilObject, string> convert)
            {
                var sb = new StringBuilder();

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

            /// <summary>
            /// Returns a value indicating whether the given element is a word rather than a byte.
            /// </summary>
            /// <param name="ctx">The context.</param>
            /// <param name="index">The element index, or -1 to check the length prefix.</param>
            /// <returns><b>true</b> if the element is a word, or <b>false</b> if it's a byte.</returns>
            bool IsWord(Context ctx, int index)
            {
                if (index == -1 && HasLengthPrefix)
                    return (flags & TableFlags.WordLength) != 0;

                if (index < 0 || index >= ElementCountWithoutLength)
                    throw new ArgumentOutOfRangeException(nameof(index));

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
                    switch (initializer[index % initializer.Length].StdTypeAtom)
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

            void ExpandInitializer(ZilObject defaultValue)
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

            void ExpandPattern(Context ctx, int index, bool insert)
            {
                if (pattern == null || pattern.Length <= index)
                {
                    var byteAtom = ctx.GetStdAtom(StdAtom.BYTE);
                    var wordAtom = ctx.GetStdAtom(StdAtom.WORD);

                    var length = ElementCountWithoutLength;
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

            /// <summary>
            /// Returns the index of the table element located at the given byte offset.
            /// </summary>
            /// <param name="ctx">The context.</param>
            /// <param name="offset">The byte offset.</param>
            /// <returns>-1 if the length prefix is at the given offset, or a 0-based index if a table element
            /// is at the given offset, or <b>null</b> if </returns>
            internal int? ByteOffsetToIndex(Context ctx, int offset)
            {
                Contract.Requires(ctx != null);
                Contract.Requires(offset >= 0);
                Contract.Ensures(Contract.Result<int?>() == null || (Contract.Result<int?>().Value >= -1 && Contract.Result<int?>().Value < ElementCountWithoutLength));
                Contract.Ensures(Contract.Result<int?>() >= 0 || HasLengthPrefix);

                // account for initial length markers
                if ((flags & TableFlags.ByteLength) != 0)
                {
                    if (offset == 0)
                        return -1;

                    offset--;
                }
                else if ((flags & TableFlags.WordLength) != 0)
                {
                    if (offset == 0)
                        return -1;
                    else if (offset == 1)
                        return null;

                    offset -= 2;
                }

                // initialize cache if necessary
                if (elementToByteOffsets == null)
                {
                    elementToByteOffsets = new int[ElementCountWithoutLength];
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

            public override ZilObject GetWord(Context ctx, int offset)
            {
                // convert word offset to byte offset
                offset *= 2;

                var index = ByteOffsetToIndex(ctx, offset);
                if (index == null)
                    throw new ArgumentException(string.Format("No element at offset {0}", offset));
                if (!IsWord(ctx, index.Value))
                    throw new ArgumentException(string.Format("Element at byte offset {0} is not a word", offset));

                if (index == -1)
                    return new ZilFix(ElementCountWithoutLength);

                if (initializer == null)
                    return null;

                return initializer[index.Value % initializer.Length];
            }

            public override void PutWord(Context ctx, int offset, ZilObject value)
            {
                // convert word offset to byte offset
                offset *= 2;

                var index = ByteOffsetToIndex(ctx, offset);
                if (index == null)
                    throw new ArgumentException(string.Format("No element at offset {0}", offset));

                if (index == -1)
                {
                    ExpandLengthPrefix(ctx);
                    index = 0;
                }

                if (!IsWord(ctx, index.Value))
                {
                    // we may be able to replace 2 bytes with a word
                    var index2 = ByteOffsetToIndex(ctx, offset + 1);
                    if (index2 == null || IsWord(ctx, index2.Value))
                        throw new ArgumentException(string.Format("Element at byte offset {0} is not a word", offset));

                    // remove one of the bytes from the initializer...
                    if (initializer == null || repetitions > 1)
                        ExpandInitializer(ctx.FALSE);

                    var newInitializer = new ZilObject[initializer.Length - 1];
                    Array.Copy(initializer, newInitializer, index.Value);
                    Array.Copy(initializer, index.Value + 2, newInitializer, index.Value + 1, initializer.Length - index.Value - 2);
                    initializer = newInitializer;

                    // ...and the pattern, if appropriate. then store the new value.
                    if (pattern != null)
                    {
                        ExpandPattern(ctx, index.Value, false);
                        var newPattern = new ZilObject[pattern.Length - 1];
                        Array.Copy(pattern, newPattern, index.Value);
                        Array.Copy(pattern, index.Value + 2, newPattern, index.Value + 1, pattern.Length - index.Value - 2);
                        pattern = newPattern;

                        initializer[index.Value] = value;
                        pattern[index.Value] = ctx.GetStdAtom(StdAtom.WORD);
                    }
                    else
                    {
                        initializer[index.Value] = new ZilWord(value);
                    }

                    elementToByteOffsets = null;
                }
                else
                {
                    if (initializer == null || repetitions > 1)
                        ExpandInitializer(ctx.FALSE);

                    if (initializer[index.Value] is ZilWord)
                    {
                        initializer[index.Value] = new ZilWord(value);
                    }
                    else
                    {
                        initializer[index.Value] = value;
                    }
                }
            }

            public override ZilObject GetByte(Context ctx, int offset)
            {
                var index = ByteOffsetToIndex(ctx, offset);
                if (index == null)
                    throw new ArgumentException(string.Format("No element at offset {0}", offset));
                if (IsWord(ctx, index.Value))
                    throw new ArgumentException(string.Format("Element at byte offset {0} is not a byte", offset));

                if (index == -1)
                    return new ZilFix((byte)ElementCountWithoutLength);

                if (initializer == null)
                    return null;

                return initializer[index.Value % initializer.Length];
            }

            public override void PutByte(Context ctx, int offset, ZilObject value)
            {
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

                if (index == -1)
                {
                    ExpandLengthPrefix(ctx);
                    index = 0;
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

            void ExpandLengthPrefix(Context ctx)
            {
                if (!HasLengthPrefix)
                    return;

                ExpandInitializer(ctx.FALSE);
                ExpandPattern(ctx, 0, true);

                // add length to beginning of initializer
                var countWithoutLength = ElementCountWithoutLength;

                var newInitializer = new ZilObject[countWithoutLength + 1];
                newInitializer[0] = new ZilFix(countWithoutLength);
                Array.Copy(initializer, 0, newInitializer, 1, countWithoutLength);

                initializer = newInitializer;

                // set width of the length element in pattern
                if ((flags & TableFlags.ByteLength) != 0)
                    pattern[0] = ctx.GetStdAtom(StdAtom.BYTE);
                else
                    pattern[0] = ctx.GetStdAtom(StdAtom.WORD);

                // clear length prefix flags
                flags &= ~(TableFlags.ByteLength | TableFlags.WordLength);
            }

            protected override ZilTable AsNewTable()
            {
                return new OriginalTable(
                    repetitions,
                    (ZilObject[])initializer?.Clone(),
                    flags,
                    (ZilObject[])pattern?.Clone());
            }

            public override ZilTable OffsetByBytes(Context ctx, int bytesToSkip)
            {
                return new OffsetTable(ctx, this, bytesToSkip);
            }
        }

        [BuiltinAlternate(typeof(ZilTable))]
        sealed class OffsetTable : ZilTable
        {
            readonly Context ctx;
            readonly OriginalTable orig;
            readonly int byteOffset;

            // This may unexpectedly change when items in orig before byteOffset change from bytes to words!
            int ElementOffset => (int)orig.ByteOffsetToIndex(ctx, byteOffset);

            public OffsetTable(Context ctx, OriginalTable orig, int byteOffset)
            {
                this.ctx = ctx;
                this.orig = orig;
                this.byteOffset = byteOffset;
            }

            public override int ElementCount => orig.ElementCount - ElementOffset;
            public override TableFlags Flags => orig.Flags & ~(TableFlags.ByteLength | TableFlags.WordLength);

            public override void CopyTo<T>(T[] array, TableToArrayElementConverter<T> convert, T defaultFiller, Context ctx)
            {
                var elemOffset = ElementOffset;
                var temp = new T[orig.ElementCount];
                orig.CopyTo(temp, convert, defaultFiller, ctx);
                Array.Copy(temp, elemOffset, array, 0, temp.Length - elemOffset);
            }

            protected override string ToString(Func<ZilObject, string> convert)
            {
                return string.Format("#TABLE ('OFFSET {0} {1})", byteOffset, orig.ToString(convert));
            }

            public override ZilObject GetWord(Context ctx, int offset)
            {
                return orig.GetWord(ctx, offset + this.byteOffset / 2);
            }

            public override ZilObject GetByte(Context ctx, int offset)
            {
                return orig.GetByte(ctx, offset + this.byteOffset);
            }

            public override void PutWord(Context ctx, int offset, ZilObject value)
            {
                orig.PutWord(ctx, offset + this.byteOffset / 2, value);
            }

            public override void PutByte(Context ctx, int offset, ZilObject value)
            {
                orig.PutByte(ctx, offset + this.byteOffset, value);
            }

            protected override ZilTable AsNewTable()
            {
                //XXX
                throw new NotImplementedException();
            }

            public override ZilTable OffsetByBytes(Context ctx, int bytesToSkip)
            {
                return new OffsetTable(ctx, orig, byteOffset + bytesToSkip);
            }
        }
    }
}