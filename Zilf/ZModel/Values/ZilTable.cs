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
using System.Runtime.Serialization;
using System.Text;
using JetBrains.Annotations;
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
    public sealed class UnalignedTableReadException : Exception
    {
        public UnalignedTableReadException() { }

        UnalignedTableReadException([NotNull] SerializationInfo info, StreamingContext context)
            : base(info, context) { }
    }

    [BuiltinType(StdAtom.TABLE, PrimType.TABLE)]
    abstract class ZilTable : ZilObject, IProvideStructureForDeclCheck
    {
        [CanBeNull]
        public string Name { get; set; }

        public abstract TableFlags Flags { get; }
        public abstract int ElementCount { get; }
        public abstract int ByteCount { get; }

        [CanBeNull]
        public abstract ZilObject GetWord([NotNull] Context ctx, int offset);

        [CanBeNull]
        public abstract ZilObject GetByte([NotNull] Context ctx, int offset);

        public abstract void PutWord([NotNull] Context ctx, int offset, [NotNull] ZilObject value);
        public abstract void PutByte([NotNull] Context ctx, int offset, [NotNull] ZilObject value);

        public abstract void CopyTo<T>([NotNull] T[] array, [NotNull] TableToArrayElementConverter<T> convert,
            [CanBeNull] T defaultFiller, [NotNull] Context ctx);

        [NotNull]
        protected abstract ZilTable AsNewTable();
        [NotNull]
        public abstract ZilTable OffsetByBytes(int bytesToSkip);

        protected abstract string ToString([NotNull] Func<ZilObject, string> convert);

        [NotNull]
        public static ZilTable Create(int repetitions, [CanBeNull] ZilObject[] initializer, TableFlags flags,
            [CanBeNull] ZilObject[] pattern)
        {
            return new OriginalTable(repetitions, initializer, flags, pattern);
        }

        [NotNull]
        [ChtypeMethod]
        public static ZilTable FromTable([NotNull] Context ctx, [NotNull] ZilTable other)
        {
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

        [NotNull]
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
            int[] elementToByteOffsets;
            ZilObject[] pattern;

            public OriginalTable(int repetitions, [CanBeNull] ZilObject[] initializer, TableFlags flags,
                [CanBeNull] ZilObject[] pattern)
            {
                this.repetitions = repetitions;
                this.initializer = initializer?.Length > 0 ? initializer : null;
                this.flags = flags;
                this.pattern = pattern;
            }

            [System.Diagnostics.Contracts.Pure]
            bool HasLengthPrefix => (flags & (TableFlags.ByteLength | TableFlags.WordLength)) != 0;
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

            public override TableFlags Flags => flags;

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

            [NotNull]
            protected override string ToString(Func<ZilObject, string> convert)
            {
                var sb = new StringBuilder();

                var useItable =
                    repetitions != 1 ||
                    initializer == null ||
                    (flags & (TableFlags.ByteLength | TableFlags.Byte)) == TableFlags.ByteLength ||
                    (flags & (TableFlags.WordLength | TableFlags.Byte)) == (TableFlags.WordLength | TableFlags.Byte);

                if (useItable)
                {
                    sb.Append("%<ITABLE ");

                    if ((flags & TableFlags.ByteLength) != 0)
                    {
                        sb.Append("BYTE ");
                    }
                    else if ((flags & TableFlags.WordLength) != 0)
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

                if ((flags & TableFlags.Byte) != 0)
                    sb.Append("BYTE ");
                if (!useItable && HasLengthPrefix)
                    sb.Append("LENGTH ");
                if ((flags & TableFlags.Lexv) != 0)
                    sb.Append("LEXV ");
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
                    if (index >= pattern.Length - 1 && pattern[pattern.Length - 1] is ZilVector rest)
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

                return (flags & TableFlags.Byte) == 0;
            }

            void ExpandInitializer([NotNull] ZilObject defaultValue)
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

            void ExpandPattern([NotNull] Context ctx, int index, bool insert)
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

                    if (offset == 1)
                        return null;

                    offset -= 2;
                }

                // binary search to find the element
                var index = Array.BinarySearch(GetElementToByteOffsets(), offset);
                return index >= 0 ? index : (int?)null;
            }

            [NotNull]
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

            public override ZilObject GetWord(Context ctx, int offset)
            {
                return GetWordAtByte(offset * 2);
            }

            [CanBeNull]
            public ZilObject GetWordAtByte(int byteOffset)
            {
                // ReSharper disable once PatternAlwaysOfType
                if (!(ByteOffsetToIndex(byteOffset) is int index) || !IsWord(index))
                    throw new UnalignedTableReadException();

                return index == -1 ? new ZilFix(ElementCountWithoutLength) : initializer?[index % initializer.Length];
            }

            public override void PutWord(Context ctx, int offset, ZilObject value)
            {
                PutWordAtByte(ctx, offset * 2, value);
            }

            public void PutWordAtByte(Context ctx, int byteOffset, ZilObject value)
            {
                var index = ByteOffsetToIndex(byteOffset);

                switch (index)
                {
                    case null:
                        throw new ArgumentException($"No element at offset {byteOffset}");

                    case -1:
                        ExpandLengthPrefix(ctx);
                        index = 0;
                        break;
                }

                if (!IsWord(index.Value))
                {
                    // we may be able to replace 2 bytes with a word
                    var index2 = ByteOffsetToIndex(byteOffset + 1);
                    if (index2 == null || IsWord(index2.Value))
                        throw new ArgumentException($"Element at byte offset {byteOffset} is not a word");

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
                // ReSharper disable once PatternAlwaysOfType
                if (!(ByteOffsetToIndex(offset) is int index) || IsWord(index))
                    throw new UnalignedTableReadException();

                return index == -1 ? new ZilFix((byte)ElementCountWithoutLength) : initializer?[index % initializer.Length];
            }

            public override void PutByte(Context ctx, int offset, ZilObject value)
            {
                if (initializer == null || repetitions > 1)
                    ExpandInitializer(ctx.FALSE);

                var index = ByteOffsetToIndex(offset);
                bool second = false;

                switch (index)
                {
                    case null:
                        // might be the second byte of a word
                        index = ByteOffsetToIndex(offset - 1);
                        if (index != null && IsWord(index.Value))
                        {
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
                }

                if (IsWord(index.Value))
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

                if (IsWord(index.Value))
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

            public override ZilTable OffsetByBytes(int bytesToSkip)
            {
                return new OffsetTable(this, bytesToSkip);
            }
        }

        [BuiltinAlternate(typeof(ZilTable))]
        sealed class OffsetTable : ZilTable
        {
            [NotNull]
            readonly OriginalTable orig;
            readonly int byteOffset;

            /// <summary>
            /// This may unexpectedly change when items in orig before byteOffset change from bytes to words! 
            /// </summary>
            /// <exception cref="InvalidOperationException">
            /// This object's offset into the <see cref="ZilTable.OriginalTable"/> is no longer valid.
            /// </exception>
            // ReSharper disable once PossibleInvalidOperationException
            int ElementOffset => (int)orig.ByteOffsetToIndex(byteOffset);

            public OffsetTable([NotNull] OriginalTable orig, int byteOffset)
            {
                this.orig = orig;
                this.byteOffset = byteOffset;
            }

            public override int ElementCount => orig.ElementCount - ElementOffset;
            public override int ByteCount => orig.ByteCount - byteOffset;
            public override TableFlags Flags => orig.Flags & ~(TableFlags.ByteLength | TableFlags.WordLength);

            public override void CopyTo<T>(T[] array, TableToArrayElementConverter<T> convert, T defaultFiller, Context ctx)
            {
                var elemOffset = ElementOffset;
                var temp = new T[orig.ElementCount];
                orig.CopyTo(temp, convert, defaultFiller, ctx);
                Array.Copy(temp, elemOffset, array, 0, temp.Length - elemOffset);
            }

            [NotNull]
            protected override string ToString(Func<ZilObject, string> convert)
            {
                // strip initial '%' from original table representation
                var origStr = orig.ToString(convert).Substring(1);

                return $"%<ZREST {origStr} {byteOffset}>";
            }

            public override ZilObject GetWord(Context ctx, int offset)
            {
                return orig.GetWordAtByte(offset * 2 + byteOffset);
            }

            public override ZilObject GetByte(Context ctx, int offset)
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

            protected override ZilTable AsNewTable()
            {
                throw new NotImplementedException();
            }

            public override ZilTable OffsetByBytes(int bytesToSkip)
            {
                return new OffsetTable(orig, byteOffset + bytesToSkip);
            }
        }
    }
}