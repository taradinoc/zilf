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
using System.Diagnostics.CodeAnalysis;
using System.Runtime.Serialization;
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
    /// <param name="isWord"><see langword="true"/> if the table element is a word; <see langword="false"/> if it's a byte.</param>
    /// <returns>The converted element.</returns>
    delegate T TableToArrayElementConverter<out T>(ZilObject tableElement, bool isWord);

    /// <summary>
    /// Thrown when attempting to read a byte from a location in a <see cref="ZilTable"/>
    /// that contains a word, or vice versa.
    /// </summary>
    [Serializable]
    [SuppressMessage("ReSharper", "UnusedMember.Global")]
    public sealed class UnalignedTableReadException : Exception
    {
        public UnalignedTableReadException() { }

        UnalignedTableReadException(SerializationInfo info, StreamingContext context)
            : base(info, context) { }

        public UnalignedTableReadException(string message) : base(message)
        {
        }

        public UnalignedTableReadException(string message, Exception innerException) : base(message, innerException)
        {
        }
    }

    [BuiltinType(StdAtom.TABLE, PrimType.TABLE)]
    abstract class ZilTable : ZilObject, IProvideStructureForDeclCheck
    {
        public string? Name { get; set; }

        public abstract TableFormat Flags { get; }
        public abstract int ElementCount { get; }
        public abstract int ByteCount { get; }

        public abstract ZilObject? GetWord(Context ctx, int offset);

        public abstract ZilObject? GetByte(Context ctx, int offset);

        public abstract void PutWord(Context ctx, int offset, ZilObject value);
        public abstract void PutByte(Context ctx, int offset, ZilObject value);

        public abstract void CopyTo<T>(T[] array, TableToArrayElementConverter<T> convert,
            T defaultFiller, Context ctx);

        protected abstract ZilTable AsNewTable();

        public abstract ZilTable OffsetByBytes(int bytesToSkip);

        protected abstract string ToString(Func<ZilObject, string> convert);

        public static ZilTable Create(int repetitions, ZilObject[]? initializer, TableFormat flags,
            ZilObject[]? pattern)
        {
            return new OriginalTable(repetitions, initializer, flags, pattern);
        }

        [ChtypeMethod]
        public static ZilTable FromTable(ZilTable other) => other.AsNewTable();

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

        public sealed override ZilObject GetPrimitive(Context ctx) => this;

        IStructure IProvideStructureForDeclCheck.GetStructureForDeclCheck(Context ctx)
        {
            var array = new ZilObject[ElementCount];
            CopyTo(array, (zo, isWord) => zo, ctx.FALSE, ctx);
            return new ZilVector(array);
        }

        [BuiltinAlternate(typeof(ZilTable))]
        sealed class OriginalTable : ZilTable
        {
            TableFormat flags;

            int repetitions;
            ZilObject[]? initializer;
            int[]? elementToByteOffsets;
            ZilObject[]? pattern;

            public OriginalTable(int repetitions, ZilObject[]? initializer, TableFormat flags,
                ZilObject[]? pattern)
            {
                this.repetitions = repetitions;
                this.initializer = initializer?.Length > 0 ? initializer : null;
                this.flags = flags;
                this.pattern = pattern;
            }

            [System.Diagnostics.Contracts.Pure]
            bool HasLengthPrefix => (flags & (TableFormat.ByteLength | TableFormat.WordLength)) != 0;

            [System.Diagnostics.Contracts.Pure]
            int ElementCountWithoutLength => repetitions * initializer?.Length ?? repetitions;

            [System.Diagnostics.Contracts.Pure]
            public override int ElementCount => ElementCountWithoutLength + (HasLengthPrefix ? 1 : 0);

            public override int ByteCount
            {
                get
                {
                    int result;

                    if (HasLengthPrefix)
                    {
                        result = IsWord(-1) ? 2 : 1;
                    }
                    else
                    {
                        result = 0;
                    }

                    // initialize cache if needed
                    var elemOffsets = GetElementToByteOffsets();

                    if (elemOffsets.Length > 0)
                    {
                        var last = elemOffsets.Length - 1;
                        result += elemOffsets[last];
                        result += IsWord(last) ? 2 : 1;
                    }

                    return result;
                }
            }

            public override TableFormat Flags => flags;

            public override void CopyTo<T>(T[] array, TableToArrayElementConverter<T> convert, T defaultFiller, Context ctx)
            {
                int start;

                if (HasLengthPrefix)
                {
                    array[0] = convert(new ZilFix(ElementCountWithoutLength), (flags & TableFormat.ByteLength) == 0);
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
                        foreach (var initItem in initializer)
                        {
                            array[start + i] = convert(initItem, IsWord(i));
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

                var useItable =
                    repetitions != 1 ||
                    initializer == null ||
                    (flags & (TableFormat.ByteLength | TableFormat.Byte)) == TableFormat.ByteLength ||
                    (flags & (TableFormat.WordLength | TableFormat.Byte)) == (TableFormat.WordLength | TableFormat.Byte);

                if (useItable)
                {
                    sb.Append("%<ITABLE ");

                    if ((flags & TableFormat.ByteLength) != 0)
                    {
                        sb.Append("BYTE ");
                    }
                    else if ((flags & TableFormat.WordLength) != 0)
                    {
                        sb.Append("WORD ");
                    }

                    sb.Append(repetitions);
                    sb.Append(' ');
                }
                else
                {
                    sb.Append("%<TABLE ");
                }

                sb.Append('(');

                int pos = sb.Length;

                if ((flags & TableFormat.Byte) != 0)
                    sb.Append("BYTE ");
                if (!useItable && HasLengthPrefix)
                    sb.Append("LENGTH ");
                if ((flags & TableFormat.Lexv) != 0)
                    sb.Append("LEXV ");
                if ((flags & TableFormat.Pure) != 0)
                    sb.Append("PURE ");
                if ((flags & TableFormat.TempTable) != 0)
                    sb.Append("TEMP-TABLE ");

                if (pattern != null)
                {
                    sb.Append("PATTERN ");
                    sb.Append(convert(new ZilList(pattern)));
                    sb.Append(' ');
                }

                if (sb.Length > pos)
                    sb.Length--;

                sb.Append(')');

                if (initializer != null)
                {
                    foreach (var obj in initializer)
                    {
                        sb.Append(' ');
                        sb.Append(convert(obj));
                    }
                }

                sb.Append('>');
                return sb.ToString();
            }

            /// <summary>
            /// Returns a value indicating whether the given element is a word rather than a byte.
            /// </summary>
            /// <param name="index">The element index, or -1 to check the length prefix.</param>
            /// <returns><see langword="true"/> if the element is a word, or <see langword="false"/> if it's a byte.</returns>
            bool IsWord(int index)
            {
                if (index == -1 && HasLengthPrefix)
                    return (flags & TableFormat.WordLength) != 0;

                if (index < 0 || index >= ElementCountWithoutLength)
                    throw new ArgumentOutOfRangeException(nameof(index));

                if ((flags & TableFormat.Lexv) != 0)
                {
                    // word-byte-byte repeating
                    return index % 3 == 0;
                }

                if (pattern != null && pattern.Length > 0)
                {
                    if (index >= pattern.Length - 1 && pattern[^1] is ZilVector rest)
                    {
                        index -= pattern.Length - 1;
                        return !(rest[index % (rest.GetLength() - 1) + 1] is ZilAtom atom && atom.StdAtom == StdAtom.BYTE);
                    }

                    if (pattern[index % pattern.Length] is ZilAtom atom2)
                    {
                        return atom2.StdAtom != StdAtom.BYTE;
                    }

                    throw new InvalidOperationException("malformed pattern");
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

                return (flags & TableFormat.Byte) == 0;
            }

            void ExpandInitializer(ZilObject defaultValue)
            {
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
                if (pattern?.Length > index)
                    return;

                var byteAtom = ctx.GetStdAtom(StdAtom.BYTE);
                var wordAtom = ctx.GetStdAtom(StdAtom.WORD);

                var length = ElementCountWithoutLength;
                if (insert)
                    length++;

                var newPattern = new ZilObject[length];

                for (int i = 0, j = 0; i < newPattern.Length; i++, j++)
                {
                    newPattern[i] = IsWord(j) ? wordAtom : byteAtom;

                    if (insert && i == index)
                    {
                        newPattern[i + 1] = newPattern[i];
                        i++;
                    }
                }

                pattern = newPattern;
            }

            /// <summary>
            /// Returns the index of the table element located at the given byte offset.
            /// </summary>
            /// <param name="offset">The byte offset.</param>
            /// <returns>-1 if the length prefix is at the given offset, or a 0-based index if a table element
            /// is at the given offset, or <see langword="null"/> if the offset does not point to an element.</returns>
            internal int? ByteOffsetToIndex(int offset)
            {
                // account for initial length markers
                if ((flags & TableFormat.ByteLength) != 0)
                {
                    if (offset == 0)
                        return -1;

                    offset--;
                }
                else if ((flags & TableFormat.WordLength) != 0)
                {
                    if (offset == 0)
                        return -1;

                    if (offset == 1)
                        return null;

                    offset -= 2;
                }

                // binary search to find the element
                var index = Array.BinarySearch(GetElementToByteOffsets(), offset);
                return index >= 0 ? index : (int?)null;
            }

            private int[] GetElementToByteOffsets()
            {
                if (elementToByteOffsets == null)
                {
                    elementToByteOffsets = new int[ElementCountWithoutLength];

                    for (int i = 0, nextOffset = 0; i < elementToByteOffsets.Length; i++)
                    {
                        elementToByteOffsets[i] = nextOffset;

                        if (IsWord(i))
                            nextOffset += 2;
                        else
                            nextOffset++;
                    }
                }

                return elementToByteOffsets;
            }

            public override ZilObject? GetWord(Context ctx, int offset)
            {
                return GetWordAtByte(offset * 2);
            }

            public ZilObject? GetWordAtByte(int byteOffset)
            {
                // ReSharper disable once PatternAlwaysOfType
                if (ByteOffsetToIndex(byteOffset) is not int index || !IsWord(index))
                    throw new UnalignedTableReadException();

                return index == -1 ? new ZilFix(ElementCountWithoutLength) : initializer?[index % initializer.Length];
            }

            public override void PutWord(Context ctx, int offset, ZilObject value)
            {
                PutWordAtByte(ctx, offset * 2, value);
            }

            public void PutWordAtByte(Context ctx, int byteOffset, ZilObject value)
            {
                var index = ByteOffsetToIndex(byteOffset) ??
                            throw new ArgumentException($"No element at offset {byteOffset}");

                if (index == -1)
                {
                    ExpandLengthPrefix(ctx);
                    index = 0;
                }

                if (!IsWord(index))
                {
                    // we may be able to replace 2 bytes with a word
                    var index2 = ByteOffsetToIndex(byteOffset + 1);
                    if (index2 == null || IsWord(index2.Value))
                        throw new ArgumentException($"Element at byte offset {byteOffset} is not a word");

                    // remove one of the bytes from the initializer...
                    if (initializer == null || repetitions > 1)
                        ExpandInitializer(ctx.FALSE);

                    var newInitializer = new ZilObject[initializer!.Length - 1];
                    Array.Copy(initializer, newInitializer, index);
                    Array.Copy(initializer, index + 2, newInitializer, index + 1, initializer.Length - index - 2);
                    initializer = newInitializer;

                    // ...and the pattern, if appropriate. then store the new value.
                    if (pattern != null)
                    {
                        ExpandPattern(ctx, index, false);
                        var newPattern = new ZilObject[pattern.Length - 1];
                        Array.Copy(pattern, newPattern, index);
                        Array.Copy(pattern, index + 2, newPattern, index + 1, pattern.Length - index - 2);
                        pattern = newPattern;

                        initializer[index] = value;
                        pattern[index] = ctx.GetStdAtom(StdAtom.WORD);
                    }
                    else
                    {
                        initializer[index] = new ZilWord(value);
                    }

                    elementToByteOffsets = null;
                }
                else
                {
                    if (initializer == null || repetitions > 1)
                        ExpandInitializer(ctx.FALSE);

                    if (initializer![index] is ZilWord)
                    {
                        initializer[index] = new ZilWord(value);
                    }
                    else
                    {
                        initializer[index] = value;
                    }
                }
            }

            public override ZilObject? GetByte(Context ctx, int offset)
            {
                // ReSharper disable once PatternAlwaysOfType
                if (ByteOffsetToIndex(offset) is not int index || IsWord(index))
                    throw new UnalignedTableReadException();

                return index == -1 ? new ZilFix((byte)ElementCountWithoutLength) : initializer?[index % initializer.Length];
            }

            public override void PutByte(Context ctx, int offset, ZilObject value)
            {
                if (initializer == null || repetitions > 1)
                    ExpandInitializer(ctx.FALSE);

                int index;
                bool second = false;

                switch (ByteOffsetToIndex(offset))
                {
                    case null:
                        // might be the second byte of a word
                        var maybeIndex = ByteOffsetToIndex(offset - 1);
                        if (maybeIndex != null && IsWord(maybeIndex.Value))
                        {
                            index = (int)maybeIndex;
                            second = true;
                        }
                        else
                        {
                            throw new ArgumentException($"No element at offset {offset}");
                        }

                        break;

                    case -1:
                        ExpandLengthPrefix(ctx);
                        index = 0;
                        break;

                    case int i:
                        index = i;
                        break;
                }

                if (IsWord(index))
                {
                    // split the word into 2 bytes
                    var newInitializer = new ZilObject[initializer!.Length + 1];
                    Array.Copy(initializer, newInitializer, index);
                    Array.Copy(initializer, index + 1, newInitializer, index + 2, initializer.Length - index - 1);
                    initializer = newInitializer;

                    if (pattern != null)
                        ExpandPattern(ctx, index, true);

                    elementToByteOffsets = null;

                    var zeroByte = ctx.ChangeType(ZilFix.Zero, ctx.GetStdAtom(StdAtom.BYTE));

                    if (second)
                    {
                        initializer[index] = zeroByte;
                        initializer[index + 1] = value;

                        // remember the index we actually used
                        index++;
                    }
                    else
                    {
                        initializer[index] = value;
                        initializer[index + 1] = zeroByte;
                    }
                }
                else
                {
                    initializer![index] = value;
                }

                if (IsWord(index))
                {
                    ExpandPattern(ctx, index, false);
                    pattern![index] = ctx.GetStdAtom(StdAtom.BYTE);
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
                Array.Copy(initializer!, 0, newInitializer, 1, countWithoutLength);

                initializer = newInitializer;

                // set width of the length element in pattern
                if ((flags & TableFormat.ByteLength) != 0)
                    pattern![0] = ctx.GetStdAtom(StdAtom.BYTE);
                else
                    pattern![0] = ctx.GetStdAtom(StdAtom.WORD);

                // clear length prefix flags
                flags &= ~(TableFormat.ByteLength | TableFormat.WordLength);
            }

            protected override ZilTable AsNewTable() =>
                new OriginalTable(
                    repetitions,
                    (ZilObject[])initializer?.Clone()!,
                    flags,
                    (ZilObject[])pattern?.Clone()!);

            public override ZilTable OffsetByBytes(int bytesToSkip) => new OffsetTable(this, bytesToSkip);
        }

        [BuiltinAlternate(typeof(ZilTable))]
        sealed class OffsetTable : ZilTable
        {
            readonly OriginalTable orig;
            readonly int byteOffset;

            /// <summary>
            /// This may unexpectedly change when items in orig before byteOffset change from bytes to words! 
            /// </summary>
            /// <exception cref="InvalidOperationException">
            /// This object's offset into the <see cref="ZilTable.OriginalTable"/> is no longer valid.
            /// </exception>
            // ReSharper disable once PossibleInvalidOperationException
            int ElementOffset => orig.ByteOffsetToIndex(byteOffset) ?? throw new InvalidOperationException();

            public OffsetTable(OriginalTable orig, int byteOffset)
            {
                this.orig = orig;
                this.byteOffset = byteOffset;
            }

            public override int ElementCount => orig.ElementCount - ElementOffset;
            public override int ByteCount => orig.ByteCount - byteOffset;
            public override TableFormat Flags => orig.Flags & ~(TableFormat.ByteLength | TableFormat.WordLength);

            public override void CopyTo<T>(T[] array, TableToArrayElementConverter<T> convert, T defaultFiller, Context ctx)
            {
                var elemOffset = ElementOffset;
                var temp = new T[orig.ElementCount];
                orig.CopyTo(temp, convert, defaultFiller, ctx);
                Array.Copy(temp, elemOffset, array, 0, temp.Length - elemOffset);
            }

            protected override string ToString(Func<ZilObject, string> convert)
            {
                // strip initial '%' from original table representation
                var origStr = orig.ToString(convert)[1..];

                return $"%<ZREST {origStr} {byteOffset}>";
            }

            public override ZilObject? GetWord(Context ctx, int offset)
            {
                return orig.GetWordAtByte(offset * 2 + byteOffset);
            }

            public override ZilObject? GetByte(Context ctx, int offset)
            {
                return orig.GetByte(ctx, offset + byteOffset);
            }

            public override void PutWord(Context ctx, int offset, ZilObject value)
            {
                orig.PutWordAtByte(ctx, offset * 2 + byteOffset, value);
            }

            public override void PutByte(Context ctx, int offset, ZilObject value)
            {
                orig.PutByte(ctx, offset + byteOffset, value);
            }

            protected override ZilTable AsNewTable() => throw new NotImplementedException();

            public override ZilTable OffsetByBytes(int bytesToSkip) => new OffsetTable(orig, byteOffset + bytesToSkip);
        }
    }
}