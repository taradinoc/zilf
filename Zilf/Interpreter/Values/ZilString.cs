/* Copyright 2010-2017 Jesse McGrew
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
using System.Diagnostics.CodeAnalysis;
using System.Diagnostics.Contracts;
using System.Text;
using Zilf.Language;
using Zilf.Diagnostics;
using JetBrains.Annotations;

namespace Zilf.Interpreter.Values
{
    [BuiltinType(StdAtom.STRING, PrimType.STRING)]
    [ContractClass(typeof(ZilStringContracts))]
    abstract class ZilString : ZilObject, IStructure
    {
        [NotNull]
        public abstract string Text { get; set; }

        [NotNull]
        [ChtypeMethod]
        public static ZilString FromString([NotNull] Context ctx, [NotNull] ZilString other)
        {
            Contract.Requires(ctx != null);
            Contract.Requires(other != null);

            return new OriginalString(other.Text);
        }

        [NotNull]
        public static ZilString FromString([NotNull] string text)
        {
            return new OriginalString(text);
        }

        public sealed override string ToString()
        {
            return Quote(Text);
        }

        [NotNull]
        public static string Quote([NotNull] string text)
        {
            Contract.Requires(text != null);
            Contract.Ensures(Contract.Result<string>() != null);

            var sb = new StringBuilder(text.Length + 2);
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
            return friendly ? Text : ToString();
        }

        public override bool Equals(object obj)
        {
            return (obj as ZilString)?.Text.Equals(Text) ?? false;
        }

        public override int GetHashCode()
        {
            return Text.GetHashCode();
        }

        public sealed override StdAtom StdTypeAtom => StdAtom.STRING;

        public sealed override PrimType PrimType => PrimType.STRING;

        public abstract ZilObject this[int index] { get; set; }

        [NotNull]
        public sealed override ZilObject GetPrimitive(Context ctx)
        {
            return this;
        }

        public abstract ZilObject GetFirst();
        public abstract IStructure GetRest(int skip);
        public abstract IStructure GetBack(int skip);
        public abstract IStructure GetTop();
        public abstract bool IsEmpty { get; }
        public abstract int GetLength();
        public abstract int? GetLength(int limit);
        public abstract IEnumerator<ZilObject> GetEnumerator();

        public void Grow(int end, int beginning, ZilObject defaultValue)
        {
            throw new NotSupportedException();
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return GetEnumerator();
        }

        [BuiltinAlternate(typeof(ZilString))]
        sealed class OriginalString : ZilString
        {
            public OriginalString([NotNull] string text)
            {
                Text = text;
            }

            public override string Text { get; set; }

            public override ZilObject GetFirst()
            {
                return Text.Length > 0 ? new ZilChar(Text[0]) : null;
            }

            public override IStructure GetRest(int skip)
            {
                return skip <= Text.Length ? new OffsetString(this, skip) : null;
            }

            public override IStructure GetBack(int skip)
            {
                return skip == 0 ? this : null;
            }

            public override IStructure GetTop() => this;

            public override bool IsEmpty => Text.Length == 0;

            [CanBeNull]
            public override ZilObject this[int index]
            {
                get
                {
                    if (index >= 0 && index < Text.Length)
                        return new ZilChar(Text[index]);
                    return null;
                }
                set
                {
                    if (!(value is ZilChar ch))
                        throw new InterpreterError(InterpreterMessages._0_In_1_Must_Be_2, "elements", "a STRING", "CHARACTERs");
                    if (index >= 0 && index < Text.Length)
                        Text = Text.Substring(0, index) + ch.Char +
                               Text.Substring(index + 1, Text.Length - index - 1);
                    else
                        throw new ArgumentOutOfRangeException(nameof(index));
                }
            }

            public override int GetLength() => Text.Length;

            public override int? GetLength(int limit) =>
                Text.Length <= limit ? Text.Length : (int?)null;

            public override IEnumerator<ZilObject> GetEnumerator()
            {
                foreach (var c in Text)
                    yield return new ZilChar(c);
            }
        }

        [BuiltinAlternate(typeof(ZilString))]
        class OffsetString : ZilString
        {
            readonly OriginalString orig;
            readonly int offset;

            public OffsetString(OriginalString orig, int offset)
            {
                this.orig = orig;
                this.offset = offset;
            }

            public override string Text
            {
                get => orig.Text.Substring(offset);
                set => orig.Text = orig.Text.Substring(0, offset) + value;
            }

            public override bool Equals(object obj)
            {
                if (obj is OffsetString other && other.orig == orig && other.offset == offset)
                    return true;

                return base.Equals(obj);
            }

            // make the compiler happy
            public override int GetHashCode() => base.GetHashCode();

            public override ZilObject GetFirst()
            {
                return offset < orig.Text.Length ? new ZilChar(orig.Text[offset]) : null;
            }

            public override IStructure GetRest(int skip)
            {
                return offset <= orig.Text.Length - skip ? new OffsetString(orig, offset + skip) : null;
            }

            public override IStructure GetBack(int skip)
            {
                return offset >= skip ? new OffsetString(orig, offset - skip) : null;
            }

            public override IStructure GetTop() => orig;

            public override bool IsEmpty => offset >= orig.Text.Length;

            [CanBeNull]
            public override ZilObject this[int index]
            {
                get
                {
                    index += offset;
                    if (index >= 0 && index < orig.Text.Length)
                        return new ZilChar(orig.Text[index]);
                    return null;
                }
                set
                {
                    if (!(value is ZilChar ch))
                        throw new InterpreterError(InterpreterMessages._0_In_1_Must_Be_2, "elements", "a STRING", "CHARACTERs");

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
                        throw new ArgumentOutOfRangeException(nameof(index));
                    }
                }
            }

            public override int GetLength()
            {
                return Math.Max(orig.Text.Length - offset, 0);
            }

            public override int? GetLength(int limit)
            {
                var length = Math.Max(orig.Text.Length - offset, 0);
                return length <= limit ? length : (int?)null;
            }

            public override IEnumerator<ZilObject> GetEnumerator()
            {
                for (int i = offset; i < orig.Text.Length; i++)
                    yield return new ZilChar(orig.Text[i]);
            }
        }
    }

    [ContractClassFor(typeof(ZilString))]
    [SuppressMessage("ReSharper", "AssignNullToNotNullAttribute")]
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