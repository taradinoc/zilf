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
using System.Linq;
using System.Text;

namespace Zilf
{
    class ZilRoutine : ZilObject, ISourceLine
    {
        private readonly ZilAtom name;
        private readonly ArgSpec argspec;
        private readonly ZilObject[] body;

        public ZilRoutine(ZilAtom name, IEnumerable<ZilObject> argspec, IEnumerable<ZilObject> body)
        {
            this.name = name;
            this.argspec = new ArgSpec(name, argspec);
            this.body = body.ToArray();
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

    class ZilTable : ZilObject, ISourceLine
    {
        private readonly string filename;
        private readonly int line;
        private readonly int elements;
        private readonly ZilObject[] initializer;
        private readonly TableFlags flags;

        public ZilTable(string filename, int line, int elements, ZilObject[] initializer, TableFlags flags)
        {
            this.filename = filename;
            this.line = line;
            this.elements = elements;
            this.initializer = initializer;
            this.flags = flags;
        }

        public string Name { get; set; }

        public int ElementCount
        {
            get { return elements; }
        }

        public TableFlags Flags
        {
            get { return flags; }
        }

        public string SourceInfo
        {
            get { return filename + ":" + line.ToString(); }
        }

        public void CopyTo<T>(T[] array, int start, int length,
            Func<ZilObject, T> convert, T defaultFiller)
        {
            if (initializer != null && initializer.Length > 0)
            {
                for (int i = 0; i < length; i++)
                    array[start + i] = convert(initializer[i % initializer.Length]);
            }
            else
            {
                for (int i = 0; i < length; i++)
                    array[start + i] = defaultFiller;
            }
        }

        private string ToString(Func<ZilObject, string> convert)
        {
            StringBuilder sb = new StringBuilder();

            sb.Append("#TABLE (");
            sb.Append(elements);
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
            result.Add(new ZilFix(elements));

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