/* Copyright 2010, 2012 Jesse McGrew
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
using System.Diagnostics.Contracts;
using System.Linq;
using System.Text;

namespace Zilf
{
    class ZilRoutine : ZilObject
    {
        private readonly ZilAtom name, activationAtom;
        private readonly ArgSpec argspec;
        private readonly ZilObject[] body;
        private readonly RoutineFlags flags;

        public ZilRoutine(ZilAtom name, ZilAtom activationAtom, IEnumerable<ZilObject> argspec, IEnumerable<ZilObject> body, RoutineFlags flags)
        {
            Contract.Requires(name != null);
            Contract.Requires(argspec != null);
            Contract.Requires(body != null);

            this.name = name;
            this.activationAtom = activationAtom;
            this.argspec = new ArgSpec(name, argspec);
            this.body = body.ToArray();
            this.flags = flags;
        }

        public ArgSpec ArgSpec
        {
            get { return argspec; }
        }

        public IEnumerable<ZilObject> Body
        {
            get { return body; }
        }

        public int BodyLength
        {
            get { return body.Length; }
        }

        public ZilAtom Name
        {
            get { return name; }
        }

        public ZilAtom ActivationAtom
        {
            get { return activationAtom; }
        }

        public RoutineFlags Flags
        {
            get { return flags; }
        }

        private string ToString(Func<ZilObject, string> convert)
        {
            StringBuilder sb = new StringBuilder();

            sb.Append("#ROUTINE (");
            sb.Append(argspec.ToString(convert));

            foreach (ZilObject expr in body)
            {
                sb.Append(' ');
                sb.Append(convert(expr));
            }

            sb.Append(')');
            return sb.ToString();
        }

        public override string ToString()
        {
            return ToString(zo => zo.ToString());
        }

        public override string ToStringContext(Context ctx, bool friendly)
        {
            return ToString(zo => zo.ToStringContext(ctx, friendly));
        }

        public override ZilAtom GetTypeAtom(Context ctx)
        {
            return ctx.GetStdAtom(StdAtom.ROUTINE);
        }

        public override PrimType PrimType
        {
            get { return Zilf.PrimType.LIST; }
        }

        public override ZilObject GetPrimitive(Context ctx)
        {
            var result = new List<ZilObject>(1 + body.Length);
            result.Add(argspec.ToZilList());
            result.AddRange(body);
            return new ZilList(result);
        }

        public override bool Equals(object obj)
        {
            ZilRoutine other = obj as ZilRoutine;
            if (other == null)
                return false;

            if (!other.argspec.Equals(this.argspec))
                return false;

            if (other.body.Length != this.body.Length)
                return false;

            for (int i = 0; i < body.Length; i++)
                if (!other.body[i].Equals(this.body[i]))
                    return false;

            return true;
        }

        public override int GetHashCode()
        {
            int result = argspec.GetHashCode();

            foreach (ZilObject obj in body)
                result ^= obj.GetHashCode();

            return result;
        }
    }

    class ZilConstant : ZilObject
    {
        private readonly ZilAtom name;
        private readonly ZilObject value;

        public ZilConstant(ZilAtom name, ZilObject value)
        {
            this.name = name;
            this.value = value;
        }

        public ZilAtom Name
        {
            get { return name; }
        }

        public ZilObject Value
        {
            get { return value; }
        }

        public override string ToString()
        {
            return "#CONSTANT (" + name.ToString() + " " + value.ToString() + ")";
        }

        public override string ToStringContext(Context ctx, bool friendly)
        {
            return "#CONSTANT (" + name.ToStringContext(ctx, friendly) +
                " " + value.ToStringContext(ctx, friendly) + ")";
        }

        public override ZilAtom GetTypeAtom(Context ctx)
        {
            return ctx.GetStdAtom(StdAtom.CONSTANT);
        }

        public override PrimType PrimType
        {
            get { return Zilf.PrimType.LIST; }
        }

        public override ZilObject GetPrimitive(Context ctx)
        {
            return new ZilList(name,
                new ZilList(value,
                    new ZilList(null, null)));
        }
    }

    enum GlobalStorageType
    {
        /// <summary>
        /// The global can be stored in a Z-machine global or a table.
        /// </summary>
        Any,
        /// <summary>
        /// The global is (or must be) stored in a Z-machine global.
        /// </summary>
        Hard,
        /// <summary>
        /// The global is stored in a table.
        /// </summary>
        Soft,
    }

    class ZilGlobal : ZilObject
    {
        private readonly ZilAtom name;
        private readonly ZilObject value;

        public ZilGlobal(ZilAtom name, ZilObject value, GlobalStorageType storageType = GlobalStorageType.Any)
        {
            this.name = name;
            this.value = value;
            this.StorageType = storageType;
            this.IsWord = true;
        }

        public ZilAtom Name
        {
            get { return name; }
        }

        public ZilObject Value
        {
            get { return value; }
        }

        public GlobalStorageType StorageType { get; set; }

        public bool IsWord { get; set; }

        public override string ToString()
        {
            return "#GLOBAL (" + name.ToString() + " " + value.ToString() + ")";
        }

        public override string ToStringContext(Context ctx, bool friendly)
        {
            return "#GLOBAL (" + name.ToStringContext(ctx, friendly) +
                " " + value.ToStringContext(ctx, friendly) + ")";
        }

        public override ZilAtom GetTypeAtom(Context ctx)
        {
            return ctx.GetStdAtom(StdAtom.GLOBAL);
        }

        public override PrimType PrimType
        {
            get { return Zilf.PrimType.LIST; }
        }

        public override ZilObject GetPrimitive(Context ctx)
        {
            return new ZilList(name,
                new ZilList(value,
                    new ZilList(null, null)));
        }
    }

    [Flags]
    public enum TableFlags
    {
        /// <summary>
        /// The table elements are bytes rather than words.
        /// </summary>
        Byte = 1,
        /// <summary>
        /// The table elements are 4-byte records rather than words, and
        /// the table is prefixed with an element count (byte) and a zero byte.
        /// </summary>
        Lexv = 2,
        /// <summary>
        /// The table is prefixed with an element count (byte).
        /// </summary>
        ByteLength = 4,
        /// <summary>
        /// The table is prefixed with an element count (word).
        /// </summary>
        WordLength = 8,
        /// <summary>
        /// The table is stored in pure (read-only) memory.
        /// </summary>
        Pure = 16,
    }

    [BuiltinType(StdAtom.TABLE, PrimType.TABLE)]
    class ZilTable : ZilObject
    {
        private readonly ZilObject[] pattern;
        private readonly TableFlags flags;

        private int repetitions;
        private ZilObject[] initializer;
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

        public override string ToStringContext(Context ctx, bool friendly)
        {
            return ToString(zo => zo.ToStringContext(ctx, friendly));
        }

        public override ZilAtom GetTypeAtom(Context ctx)
        {
            return ctx.GetStdAtom(StdAtom.TABLE);
        }

        public override PrimType PrimType
        {
            get { return Zilf.PrimType.TABLE; }
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

            var index = ByteOffsetToIndex(ctx, offset);
            if (index == null)
                throw new ArgumentException(string.Format("No element at offset {0}", offset));
            if (IsWord(ctx, index.Value))
                throw new ArgumentException(string.Format("Element at byte offset {0} is not a byte", offset));

            if (initializer == null || repetitions > 1)
                ExpandInitializer(ctx.FALSE);

            if (initializer[index.Value % initializer.Length].GetTypeAtom(ctx).StdAtom == StdAtom.BYTE)
            {
                value = ctx.ChangeType(value, ctx.GetStdAtom(StdAtom.BYTE));
            }

            initializer[index.Value] = value;
        }
    }

    class ZilModelObject : ZilObject
    {
        private readonly ZilAtom name;
        private readonly ZilList[] props;
        private readonly bool isRoom;

        public ZilModelObject(ZilAtom name, ZilList[] props, bool isRoom)
        {
            this.name = name;
            this.props = props;
            this.isRoom = isRoom;
        }

        public ZilAtom Name
        {
            get { return name; }
        }

        public ZilList[] Properties
        {
            get { return props; }
        }

        public bool IsRoom
        {
            get { return isRoom; }
        }

        public override string ToString()
        {
            return ToString(zo => zo.ToString());
        }

        public override string ToStringContext(Context ctx, bool friendly)
        {
            return ToString(zo => zo.ToStringContext(ctx, friendly));
        }

        private string ToString(Func<ZilObject, string> convert)
        {
            Contract.Requires(convert != null);
            Contract.Ensures(Contract.Result<string>() != null);

            StringBuilder sb = new StringBuilder("#OBJECT (");
            sb.Append(convert(name));

            foreach (ZilList p in props)
            {
                sb.Append(' ');
                sb.Append(convert(p));
            }

            sb.Append(')');
            return sb.ToString();
        }

        public override ZilAtom GetTypeAtom(Context ctx)
        {
            return ctx.GetStdAtom(StdAtom.OBJECT);
        }

        public override PrimType PrimType
        {
            get { return Zilf.PrimType.LIST; }
        }

        public override ZilObject GetPrimitive(Context ctx)
        {
            var result = new List<ZilObject>(1 + props.Length);
            result.Add(name);
            result.AddRange(props);
            return new ZilList(result);
        }
    }

    [BuiltinType(StdAtom.WORD, Zilf.PrimType.LIST)]
    sealed class ZilWord : ZilObject
    {
        private readonly ZilObject value;

        public ZilWord(ZilObject value)
        {
            Contract.Requires(value != null);

            this.value = value;
        }

        [ChtypeMethod]
        public static ZilWord FromList(Context ctx, ZilList list)
        {
            Contract.Requires(ctx != null);
            Contract.Requires(list != null);
            Contract.Ensures(Contract.Result<ZilWord>() != null);

            if (list.First == null || list.Rest == null || !list.Rest.IsEmpty)
                throw new InterpreterError("list must have length 1");

            return new ZilWord(list.First);
        }

        public ZilObject Value
        {
            get
            {
                Contract.Ensures(Contract.Result<ZilObject>() != null);

                return value;
            }
        }

        public override string ToString()
        {
            return string.Format("#WORD ({0})", value);
        }

        public override string ToStringContext(Context ctx, bool friendly)
        {
            return string.Format("#WORD ({0})", value.ToStringContext(ctx, friendly));
        }

        public override ZilAtom GetTypeAtom(Context ctx)
        {
            return ctx.GetStdAtom(StdAtom.WORD);
        }

        public override PrimType PrimType
        {
            get { return PrimType.LIST; }
        }

        public override ZilObject GetPrimitive(Context ctx)
        {
            return new ZilList(value, new ZilList(null, null));
        }
    }
}