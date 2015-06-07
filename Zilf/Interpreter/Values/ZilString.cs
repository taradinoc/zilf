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
using Zilf.Language;

namespace Zilf.Interpreter.Values
{
    [BuiltinType(StdAtom.STRING, PrimType.STRING)]
    class ZilString : ZilObject, IStructure
    {
        public string Text;

        public ZilString(string text)
        {
            this.Text = text;
        }

        [ChtypeMethod]
        public ZilString(ZilString other)
            : this(other.Text)
        {
            Contract.Requires(other != null);
        }

        public static ZilString Parse(string str)
        {
            Contract.Requires(str != null);
            Contract.Requires(str.Length >= 2);

            StringBuilder sb = new StringBuilder(str.Length - 2);

            for (int i = 1; i < str.Length - 1; i++)
            {
                char ch = str[i];
                switch (ch)
                {
                    case '\\':
                        sb.Append(str[++i]);
                        break;

                    default:
                        sb.Append(ch);
                        break;
                }
            }

            return new ZilString(sb.ToString());
        }

        public override string ToString()
        {
            return Quote(Text);
        }

        public static string Quote(string text)
        {
            Contract.Requires(text != null);
            Contract.Ensures(Contract.Result<string>() != null);

            StringBuilder sb = new StringBuilder(text.Length + 2);
            sb.Append('"');

            foreach (char c in text)
            {
                switch (c)
                {
                    case '"':
                        sb.Append("\\\"");
                        break;
                    case '\\':
                        sb.Append("\\\\");
                        break;
                    default:
                        sb.Append(c);
                        break;
                }
            }

            sb.Append('"');
            return sb.ToString();
        }

        public override string ToStringContext(Context ctx, bool friendly)
        {
            if (friendly)
                return Text;
            else
                return ToString();
        }

        public override bool Equals(object obj)
        {
            if (obj is ZilString)
                return ((ZilString)obj).Text.Equals(this.Text);

            if (obj is OffsetString)
                return obj.Equals(this);

            return false;
        }

        public override int GetHashCode()
        {
            return Text.GetHashCode();
        }

        public override ZilAtom GetTypeAtom(Context ctx)
        {
            return ctx.GetStdAtom(StdAtom.STRING);
        }

        public override PrimType PrimType
        {
            get { return PrimType.STRING; }
        }

        public override ZilObject GetPrimitive(Context ctx)
        {
            return this;
        }

        ZilObject IStructure.GetFirst()
        {
            if (Text.Length == 0)
                return null;
            else
                return new ZilChar(Text[0]);
        }

        IStructure IStructure.GetRest(int skip)
        {
            if (Text.Length < skip)
                return null;
            else
                return new OffsetString(this, skip);
        }

        bool IStructure.IsEmpty()
        {
            return Text.Length == 0;
        }

        ZilObject IStructure.this[int index]
        {
            get
            {
                if (index >= 0 && index < Text.Length)
                    return new ZilChar(Text[index]);
                else
                    return null;
            }
            set
            {
                ZilChar ch = value as ZilChar;
                if (ch == null)
                    throw new InterpreterError("elements of a string must be characters");
                if (index >= 0 && index < Text.Length)
                    Text = Text.Substring(0, index) + ch.Char +
                           Text.Substring(index + 1, Text.Length - index - 1);
                else
                    throw new InterpreterError("writing past end of string");
            }
        }

        int IStructure.GetLength()
        {
            return Text.Length;
        }

        int? IStructure.GetLength(int limit)
        {
            int length = Text.Length;
            if (length > limit)
                return null;
            else
                return length;
        }

        private class OffsetString : ZilObject, IStructure
        {
            private readonly ZilString orig;
            private readonly int offset;

            public OffsetString(ZilString orig, int offset)
            {
                this.orig = orig;
                this.offset = offset;
            }

            public override string ToString()
            {
                StringBuilder sb = new StringBuilder(orig.Text.Length - offset + 2);
                sb.Append('"');

                for (int i = offset; i < orig.Text.Length; i++)
                {
                    char c = orig.Text[i];
                    switch (c)
                    {
                        case '"':
                            sb.Append("\\\"");
                            break;
                        case '\\':
                            sb.Append("\\\\");
                            break;
                        default:
                            sb.Append(c);
                            break;
                    }
                }

                sb.Append('"');
                return sb.ToString();
            }

            public override string ToStringContext(Context ctx, bool friendly)
            {
                if (friendly)
                    return orig.Text.Substring(offset);
                else
                    return ToString();
            }

            public override bool Equals(object obj)
            {
                if (obj is OffsetString)
                {
                    OffsetString other = (OffsetString)obj;
                    if (other.orig == this.orig && other.offset == this.offset)
                        return true;

                    return other.orig.Text.Substring(other.offset).Equals(
                        this.orig.Text.Substring(this.offset));
                }

                if (obj is ZilString)
                    return orig.Text.Substring(offset).Equals(((ZilString)obj).Text);

                return false;
            }

            public override int GetHashCode()
            {
                return orig.Text.Substring(offset).GetHashCode();
            }

            public override ZilAtom GetTypeAtom(Context ctx)
            {
                return ctx.GetStdAtom(StdAtom.STRING);
            }

            public override PrimType PrimType
            {
                get { return PrimType.STRING; }
            }

            public override ZilObject GetPrimitive(Context ctx)
            {
                return new ZilString(orig.Text.Substring(offset));
            }

            ZilObject IStructure.GetFirst()
            {
                if (offset >= orig.Text.Length)
                    return null;
                else
                    return new ZilChar(orig.Text[offset]);
            }

            IStructure IStructure.GetRest(int skip)
            {
                if (offset > orig.Text.Length - skip)
                    return null;
                else
                    return new OffsetString(orig, offset + skip);
            }

            bool IStructure.IsEmpty()
            {
                return offset >= orig.Text.Length;
            }

            ZilObject IStructure.this[int index]
            {
                get
                {
                    index += offset;
                    if (index >= 0 && index < orig.Text.Length)
                        return new ZilChar(orig.Text[index]);
                    else
                        return null;
                }
                set
                {
                    ZilChar ch = value as ZilChar;
                    if (ch == null)
                        throw new InterpreterError("elements of a string must be characters");
                    index += offset;
                    if (index >= 0 && index < orig.Text.Length)
                        orig.Text = orig.Text.Substring(0, index) + ch.Char +
                                    orig.Text.Substring(index + 1, orig.Text.Length - index - 1);
                    else
                        throw new InterpreterError("writing past end of string");
                }
            }

            int IStructure.GetLength()
            {
                return Math.Max(orig.Text.Length - offset, 0);
            }

            int? IStructure.GetLength(int limit)
            {
                int length = Math.Max(orig.Text.Length - offset, 0);
                if (length > limit)
                    return null;
                else
                    return length;
            }
        }
    }
}