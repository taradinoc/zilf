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
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Text;
using Zilf.Language;
using Zilf.Diagnostics;
// ReSharper disable StringLiteralTypo

namespace Zilf.Interpreter.Values
{
    [BuiltinType(StdAtom.STRING, PrimType.STRING)]
    abstract class ZilString : ZilObject, IStructure
    {
        public abstract string Text { get; set; }

        [ChtypeMethod]
        public static ZilString FromString(ZilString other) => new OriginalString(other.Text);

        public static ZilString FromString(string text) => new OriginalString(text);

        public sealed override string ToString() => Quote(Text);

        public static string Quote(string text)
        {
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

        protected sealed override string ToStringContextImpl(Context ctx, bool friendly) =>
            friendly ? Text : ToString();

        public override bool ExactlyEquals(ZilObject? obj) => (obj as ZilString)?.Text.Equals(Text, StringComparison.Ordinal) ?? false;

        public override int GetHashCode() => Text.GetHashCode(StringComparison.Ordinal);

        public sealed override StdAtom StdTypeAtom => StdAtom.STRING;

        public sealed override PrimType PrimType => PrimType.STRING;

        [DisallowNull]
        public abstract ZilObject? this[int index] { get; set; }

        public sealed override ZilObject GetPrimitive(Context ctx) => this;

        public abstract ZilObject? GetFirst();
        public abstract IStructure? GetRest(int skip);
        public abstract IStructure? GetBack(int skip);
        public abstract IStructure GetTop();
        public abstract bool IsEmpty { get; }
        public abstract int GetLength();
        public abstract int? GetLength(int limit);
        public abstract IEnumerator<ZilObject> GetEnumerator();

        public void Grow(int end, int beginning, ZilObject defaultValue)
        {
            throw new NotSupportedException();
        }

        IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();

        [BuiltinAlternate(typeof(ZilString))]
        sealed class OriginalString : ZilString
        {
            public OriginalString(string text)
            {
                Text = text;
            }

            public override string Text { get; set; }

            public override ZilObject? GetFirst()
            {
                return Text.Length > 0 ? new ZilChar(Text[0]) : null;
            }

            public override IStructure? GetRest(int skip)
            {
                return skip <= Text.Length ? new OffsetString(this, skip) : null;
            }

            public override IStructure? GetBack(int skip)
            {
                return skip == 0 ? this : null;
            }

            public override IStructure GetTop() => this;

            public override bool IsEmpty => Text.Length == 0;

            [DisallowNull]
            public override ZilObject? this[int index]
            {
                get
                {
                    if (index >= 0 && index < Text.Length)
                        return new ZilChar(Text[index]);
                    return null;
                }
                set
                {
                    if (value is not ZilChar ch)
                    {
                        throw new InterpreterError(InterpreterMessages._0_In_1_Must_Be_2,
                            "elements",
                            "a STRING",
                            "CHARACTERs");
                    }

                    if (index < 0 || index >= Text.Length)
                        throw new ArgumentOutOfRangeException(nameof(index));

                    Text = Text[..index] + ch.Char +
                           Text.Substring(index + 1);
                }
            }

            public override int GetLength() => Text.Length;

            public override int? GetLength(int limit) =>
                Text.Length <= limit ? Text.Length : (int?)null;

            public override IEnumerator<ZilObject> GetEnumerator()
            {
                return Text.Select(c => new ZilChar(c)).Cast<ZilObject>().GetEnumerator();
            }
        }

        [BuiltinAlternate(typeof(ZilString))]
        sealed class OffsetString : ZilString
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
                get => orig.Text[offset..];
                set => orig.Text = string.Concat(orig.Text.AsSpan(0, offset), value);
            }

            public override bool StructurallyEquals(ZilObject? obj)
            {
                if (obj is OffsetString other && ReferenceEquals(other.orig, orig) && other.offset == offset)
                    return true;

                return base.StructurallyEquals(obj);
            }

            public override ZilObject? GetFirst() => offset < orig.Text.Length ? new ZilChar(orig.Text[offset]) : null;

            public override IStructure? GetRest(int skip) =>
                offset <= orig.Text.Length - skip ? new OffsetString(orig, offset + skip) : null;

            public override IStructure? GetBack(int skip) =>
                offset >= skip ? new OffsetString(orig, offset - skip) : null;

            public override IStructure GetTop() => orig;

            public override bool IsEmpty => offset >= orig.Text.Length;

            [DisallowNull]
            public override ZilObject? this[int index]
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
                    if (value is not ZilChar ch)
                        throw new InterpreterError(InterpreterMessages._0_In_1_Must_Be_2, "elements", "a STRING", "CHARACTERs");

                    index += offset;

                    if (index >= 0 && index < orig.Text.Length)
                    {
                        orig.Text =
                            orig.Text[..index] +
                            ch.Char +
                            orig.Text.Substring(index + 1);
                    }
                    else
                    {
                        throw new ArgumentOutOfRangeException(nameof(index));
                    }
                }
            }

            public override int GetLength() => Math.Max(orig.Text.Length - offset, 0);

            public override int? GetLength(int limit)
            {
                var length = Math.Max(orig.Text.Length - offset, 0);
                return length <= limit ? length : (int?)null;
            }

            public override IEnumerator<ZilObject> GetEnumerator()
            {
                return orig.Text.Skip(offset).Select(c => new ZilChar(c)).Cast<ZilObject>().GetEnumerator();
            }
        }
    }
}