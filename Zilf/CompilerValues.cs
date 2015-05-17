﻿/* Copyright 2010, 2012 Jesse McGrew
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
using System.Linq;
using System.Text;

namespace Zilf
{
    class ZilRoutine : ZilObject, ISourceLine
    {
        private readonly ZilAtom name, activationAtom;
        private readonly ArgSpec argspec;
        private readonly ZilObject[] body;
        private readonly RoutineFlags flags;

        public ZilRoutine(ZilAtom name, ZilAtom activationAtom, IEnumerable<ZilObject> argspec, IEnumerable<ZilObject> body, RoutineFlags flags)
        {
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

        public string SourceInfo
        {
            get { return "routine '" + name + "'"; }
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

    class ZilConstant : ZilObject, ISourceLine
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

        public string SourceInfo
        {
            get { return "constant '" + name.ToString() + "'"; }
        }
    }

    class ZilGlobal : ZilObject, ISourceLine
    {
        private readonly ZilAtom name;
        private readonly ZilObject value;

        public ZilGlobal(ZilAtom name, ZilObject value)
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

        public string SourceInfo
        {
            get { return "global '" + name.ToString() + "'"; }
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

    [BuiltinType(StdAtom.TABLE, PrimType.LIST)]
    class ZilTable : ZilObject, ISourceLine
    {
        private readonly string filename;
        private readonly int line;
        private readonly ZilObject[] pattern;
        private readonly TableFlags flags;

        private int repetitions;
        private ZilObject[] initializer;
        private int[] elementToByteOffsets;

        public ZilTable(string filename, int line, int repetitions, ZilObject[] initializer, TableFlags flags, ZilObject[] pattern)
        {
            this.filename = filename;
            this.line = line;
            this.repetitions = repetitions;
            this.initializer = initializer;
            this.flags = flags;
            this.pattern = pattern;
        }

        public string Name { get; set; }

        public int ElementCount
        {
            // TODO: account for initial length markers?

            get
            {
                if (initializer != null && initializer.Length > 0)
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

        public string SourceInfo
        {
            get { return filename + ":" + line.ToString(); }
        }

        public void CopyTo<T>(T[] array, Func<ZilObject, T> convert, T defaultFiller)
        {
            if (initializer != null && initializer.Length > 0)
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
            get { return Zilf.PrimType.LIST; }
        }

        public override ZilObject GetPrimitive(Context ctx)
        {
            var result = new List<ZilObject>(3);

            // element count
            result.Add(new ZilFix(repetitions));

            // flags
            var flagList = new List<ZilObject>(4);
            if ((flags & TableFlags.Byte) != 0)
                flagList.Add(ctx.GetStdAtom(StdAtom.BYTE));
            if ((flags & TableFlags.ByteLength) != 0)
                flagList.Add(ctx.GetStdAtom(StdAtom.BYTELENGTH));
            if ((flags & TableFlags.Lexv) != 0)
                flagList.Add(ctx.GetStdAtom(StdAtom.LEXV));
            if ((flags & TableFlags.WordLength) != 0)
                flagList.Add(ctx.GetStdAtom(StdAtom.WORDLENGTH));
            if ((flags & TableFlags.Pure) != 0)
                flagList.Add(ctx.GetStdAtom(StdAtom.PURE));

            if (pattern != null)
            {
                flagList.Add(ctx.GetStdAtom(StdAtom.PATTERN));
                flagList.Add(new ZilList(pattern));
            }

            result.Add(new ZilList(flagList));

            // initializer
            if (initializer != null)
            {
                result.Add(new ZilList(initializer));
            }
            else
            {
                result.Add(new ZilList(null, null));
            }

            return new ZilList(result);
        }

        [ChtypeMethod]
        public static ZilTable FromList(Context ctx, ZilList list)
        {
            if (list.IsEmpty || list.Rest.IsEmpty || list.Rest.Rest.IsEmpty || !list.Rest.Rest.Rest.IsEmpty)
                throw new InterpreterError("list converted to TABLE must have 3 elements");

            var repetitions = list.First as ZilFix;
            if (repetitions == null)
                throw new InterpreterError("first element of TABLE must be a FIX");

            var flagList = list.Rest.First as ZilList;
            if (flagList == null || flagList.GetTypeAtom(ctx).StdAtom != StdAtom.LIST)
                throw new InterpreterError("second element of TABLE must be a list");

            TableFlags flags = 0;
            ZilObject[] pattern = null;

            while (!flagList.IsEmpty)
            {
                var atom = flagList.First as ZilAtom;
                if (atom == null)
                    throw new InterpreterError("flags in TABLE must be atoms");

                switch (atom.StdAtom)
                {
                    case StdAtom.BYTE:
                        flags |= TableFlags.Byte;
                        break;
                    case StdAtom.BYTELENGTH:
                        flags |= TableFlags.ByteLength;
                        break;
                    case StdAtom.LEXV:
                        flags |= TableFlags.Lexv;
                        break;
                    case StdAtom.WORDLENGTH:
                        flags |= TableFlags.WordLength;
                        break;
                    case StdAtom.PURE:
                        flags |= TableFlags.Pure;
                        break;

                    case StdAtom.PATTERN:
                        flagList = flagList.Rest;
                        ZilList patternList;
                        if (flagList.IsEmpty || (patternList = flagList.First as ZilList) == null ||
                            patternList.GetTypeAtom(ctx).StdAtom != StdAtom.LIST)
                        {
                            throw new InterpreterError("PATTERN must be followed by a list");
                        }
                        pattern = patternList.ToArray();
                        break;
                }

                flagList = flagList.Rest;
            }

            var initializerList = list.Rest.Rest.First as ZilList;
            if (initializerList == null || initializerList.GetTypeAtom(ctx).StdAtom != StdAtom.LIST)
                throw new InterpreterError("third element of TABLE must be a list");

            ZilObject[] initializer = initializerList.IsEmpty ? null : initializerList.ToArray();

            return new ZilTable(null, 0, repetitions.Value, initializer, flags, pattern);
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

            if (initializer != null && initializer.Length > 0 &&
                initializer[index % initializer.Length].GetTypeAtom(ctx).StdAtom == StdAtom.BYTE)
            {
                return false;
            }

            return (flags & TableFlags.Byte) == 0;
        }

        private void ExpandInitializer()
        {
            //XXX
            throw new NotImplementedException();
        }

        private int? ByteOffsetToIndex(Context ctx, int offset)
        {
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
            // convert word offset to byte offset
            offset *= 2;

            var index = ByteOffsetToIndex(ctx, offset);
            if (index == null)
                throw new ArgumentException(string.Format("No element at offset {0}", offset));
            if (!IsWord(ctx, index.Value))
                throw new ArgumentException(string.Format("Element at byte offset {0} is not a word", offset));

            if (initializer == null || repetitions > 1)
                ExpandInitializer();

            initializer[index.Value] = value;
        }

        public ZilObject GetByte(Context ctx, int offset)
        {
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
            var index = ByteOffsetToIndex(ctx, offset);
            if (index == null)
                throw new ArgumentException(string.Format("No element at offset {0}", offset));
            if (IsWord(ctx, index.Value))
                throw new ArgumentException(string.Format("Element at byte offset {0} is not a byte", offset));

            if (initializer == null || repetitions > 1)
                ExpandInitializer();

            if (initializer[index.Value % initializer.Length].GetTypeAtom(ctx).StdAtom == StdAtom.BYTE)
            {
                value = ctx.ChangeType(value, ctx.GetStdAtom(StdAtom.BYTE));
            }

            initializer[index.Value] = value;
        }
    }

    class ZilModelObject : ZilObject, ISourceLine
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

        public string SourceInfo
        {
            get { return "object '" + name.ToString() + "'"; }
        }
    }
}