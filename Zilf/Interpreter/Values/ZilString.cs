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
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics.Contracts;
using System.Text;
using Zilf.Language;

namespace Zilf.Interpreter.Values
{
    [BuiltinType(StdAtom.STRING, PrimType.STRING)]
    [ContractClass(typeof(ZilStringContracts))]
    abstract class ZilString : ZilObject, IStructure
    {
        public abstract string Text { get; set; }

        [ChtypeMethod]
        public static ZilString FromString(Context ctx, ZilString other)
        {
            Contract.Requires(other != null);

            return new OriginalString(other.Text);
        }

        public static ZilString FromString(string text)
        {
            return new OriginalString(text);
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

            return new OriginalString(sb.ToString());
        }

        public sealed override string ToString()
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

        protected sealed override string ToStringContextImpl(Context ctx, bool friendly)
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

            return false;
        }

        public override int GetHashCode()
        {
            return Text.GetHashCode();
        }

        public sealed override ZilAtom GetTypeAtom(Context ctx)
        {
            return ctx.GetStdAtom(StdAtom.STRING);
        }

        public sealed override PrimType PrimType
        {
            get { return PrimType.STRING; }
        }

        public abstract ZilObject this[int index] { get; set; }

        public sealed override ZilObject GetPrimitive(Context ctx)
        {
            return this;
        }

        public abstract ZilObject GetFirst();
        public abstract IStructure GetRest(int skip);
        public abstract bool IsEmpty();
        public abstract int GetLength();
        public abstract int? GetLength(int limit);
        public abstract IEnumerator<ZilObject> GetEnumerator();

        IEnumerator IEnumerable.GetEnumerator()
        {
            return GetEnumerator();
        }

        [BuiltinAlternate(typeof(ZilString))]
        private sealed class OriginalString : ZilString
        {
            private string text;

            public OriginalString(string text)
            {
                this.text = text;
            }

            public override string Text
            {
                get { return text; }
                set { text = value; }
            }

            public override ZilObject GetFirst()
            {
                if (Text.Length == 0)
                    return null;
                else
                    return new ZilChar(Text[0]);
            }

            public override IStructure GetRest(int skip)
            {
                if (Text.Length < skip)
                    return null;
                else
                    return new OffsetString(this, skip);
            }

            public override bool IsEmpty()
            {
                return Text.Length == 0;
            }

            public override ZilObject this[int index]
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

            public override int GetLength()
            {
                return Text.Length;
            }

            public override int? GetLength(int limit)
            {
                int length = Text.Length;
                if (length > limit)
                    return null;
                else
                    return length;
            }

            public override IEnumerator<ZilObject> GetEnumerator()
            {
                foreach (var c in Text)
                    yield return new ZilChar(c);
            }
        }

        [BuiltinAlternate(typeof(ZilString))]
        private class OffsetString : ZilString
        {
            private readonly OriginalString orig;
            private readonly int offset;

            public OffsetString(OriginalString orig, int offset)
            {
                this.orig = orig;
                this.offset = offset;
            }

            public override string Text
            {
                get
                {
                    return orig.Text.Substring(offset);
                }
                set
                {
                    orig.Text = orig.Text.Substring(0, offset) + value;
                }
            }

            public override bool Equals(object obj)
            {
                var other = obj as OffsetString;
                if (other != null && other.orig == this.orig && other.offset == this.offset)
                    return true;

                return base.Equals(obj);
            }

            public override int GetHashCode()
            {
                // make the compiler happy
                return base.GetHashCode();
            }

            public override ZilObject GetFirst()
            {
                if (offset >= orig.Text.Length)
                    return null;
                else
                    return new ZilChar(orig.Text[offset]);
            }

            public override IStructure GetRest(int skip)
            {
                if (offset > orig.Text.Length - skip)
                    return null;
                else
                    return new OffsetString(orig, offset + skip);
            }

            public override bool IsEmpty()
            {
                return offset >= orig.Text.Length;
            }

            public override ZilObject this[int index]
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
                    {
                        orig.Text =
                            orig.Text.Substring(0, index) +
                            ch.Char +
                            orig.Text.Substring(index + 1, orig.Text.Length - index - 1);
                    }
                    else
                    {
                        throw new InterpreterError("writing past end of string");
                    }
                }
            }

            public override int GetLength()
            {
                return Math.Max(orig.Text.Length - offset, 0);
            }

            public override int? GetLength(int limit)
            {
                int length = Math.Max(orig.Text.Length - offset, 0);
                if (length > limit)
                    return null;
                else
                    return length;
            }

            public override IEnumerator<ZilObject> GetEnumerator()
            {
                for (int i = offset; i < orig.Text.Length; i++)
                    yield return new ZilChar(orig.Text[i]);
            }
        }
    }

    [ContractClassFor(typeof(ZilString))]
    abstract class ZilStringContracts : ZilString
    {
        public override string Text
        {
            get
            {
                Contract.Ensures(Contract.Result<string>() != null);
                return default(string);
            }

            set
            {
                Contract.Requires(value != null);
                Contract.Requires(value.Length == Text.Length);
                Contract.Ensures(Text == value);
            }
        }
    }
}