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
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Zilf.Common.StringEncoding
{
    public enum StringEncoderMode
    {
        Normal = 0,
        NoAbbreviations = 1,
    }

    public class StringEncoder
    {
        struct AbbrevEntry
        {
            public readonly Horspool Pattern;
            public readonly byte Number;

            public AbbrevEntry(string text, byte number)
            {
                Pattern = new Horspool(text);
                Number = number;
            }
        }

        class AbbrevComparer : IComparer<AbbrevEntry>
        {
            public int Compare(AbbrevEntry x, AbbrevEntry y)
            {
                int d = y.Pattern.Text.Length - x.Pattern.Text.Length;
                return d != 0 ? d : String.Compare(x.Pattern.Text, y.Pattern.Text, StringComparison.Ordinal);
            }
        }

        static readonly string[] DefaultCharset = {
            "abcdefghijklmnopqrstuvwxyz",
            "ABCDEFGHIJKLMNOPQRSTUVWXYZ",
            "0123456789.,!?_#'\"/\\-:()"   // 2 chars omitted at the beginning
        };
        readonly byte[][] charset;

        readonly List<AbbrevEntry> abbrevs = new List<AbbrevEntry>();
        static readonly AbbrevComparer abbrevLengthComparer = new AbbrevComparer();

        public StringEncoder()
        {
            // convert characters in DefaultCharset through unicode mapping and into bytes
            charset = DefaultCharset.Select(s => s.Select(UnicodeTranslation.ToZscii).ToArray()).ToArray();
        }

        public bool Frozen { get; private set; }

        /// <exception cref="InvalidOperationException">Too late to add abbreviations, or too many abbreviations.</exception>
        public void AddAbbreviation(string str)
        {
            if (Frozen)
                throw new InvalidOperationException("Too late to add abbreviations");
            if (abbrevs.Count >= 96)
                throw new InvalidOperationException("Too many abbreviations");

            var entry = new AbbrevEntry(str, (byte)abbrevs.Count);
            var idx = abbrevs.BinarySearch(entry, abbrevLengthComparer);
            abbrevs.Insert(idx < 0 ? ~idx : idx, entry);
        }

        public byte[] Encode(string str) =>
            Encode(str, null, StringEncoderMode.Normal);

        public byte[] Encode(string str, StringEncoderMode mode) =>
            Encode(str, null, mode);

        public byte[] Encode(string str, int? size, StringEncoderMode mode) =>
            Encode(str, size, mode, out _);

        public byte[] Encode(string str, int? size, StringEncoderMode mode, out int zchars)
        {
            var noAbbrevs = mode == StringEncoderMode.NoAbbreviations;

            if (!noAbbrevs)
                Frozen = true;

            var temp = new List<byte>();
            var sb = new StringBuilder(str);

            // temporarily replace abbreviated strings with Unicode private use characters (E000-E05F)
            if (!noAbbrevs)
            {
                for (int i = 0; i < abbrevs.Count; i++)
                {
                    var idx = abbrevs[i].Pattern.FindIn(sb);
                    while (idx >= 0)
                    {
                        sb.Remove(idx, abbrevs[i].Pattern.Text.Length - 1);
                        sb[idx] = (char)(0xE000 + abbrevs[i].Number);
                        idx = abbrevs[i].Pattern.FindIn(sb);
                    }
                }
            }

            for (int i = 0; i < sb.Length; i++)
            {
                char c = sb[i];
                if (c == ' ')
                {
                    temp.Add(0);
                }
                else if (c == '\n')
                {
                    temp.Add(5);
                    temp.Add(7);
                }
                else if (!noAbbrevs && c >= '\ue000' && c < '\ue060')
                {
                    int abbrNum = c - '\ue000';
                    temp.Add((byte)(1 + abbrNum / 32));
                    temp.Add((byte)(abbrNum % 32));
                }
                else
                {
                    if (UnicodeTranslation.Table.TryGetValue(c, out byte b) == false)
                    {
                        b = (byte)c;
                    }

                    int idx;
                    if ((idx = Array.IndexOf(charset[0], b)) >= 0)
                    {
                        temp.Add((byte)(idx + 6));
                    }
                    else if ((idx = Array.IndexOf(charset[1], b)) >= 0)
                    {
                        temp.Add(4);
                        temp.Add((byte)(idx + 6));
                    }
                    else if ((idx = Array.IndexOf(charset[2], b)) >= 0)
                    {
                        temp.Add(5);
                        temp.Add((byte)(idx + 8));
                    }
                    else
                    {
                        temp.Add(5);
                        temp.Add(6);
                        temp.Add((byte)((b >> 5) & 31));
                        temp.Add((byte)(b & 31));
                    }
                }
            }

            zchars = temp.Count;

            int resultSize;
            if (size == null)
            {
                if (temp.Count == 0)
                    temp.Add(5);

                while (temp.Count % 3 != 0)
                    temp.Add(5);

                resultSize = temp.Count * 2 / 3;
            }
            else
            {
                while (temp.Count < size)
                    temp.Add(5);

                resultSize = size.Value * 2 / 3;
            }

            var result = new byte[Math.Min(resultSize, temp.Count * 2 / 3)];

            for (int i = 0, t = 0; i < result.Length; )
            {
                // _aaaaabb bbbccccc
                byte a = temp[t++], b = temp[t++], c = temp[t++];
                result[i++] = (byte)(a << 2 | b >> 3);
                result[i++] = (byte)(b << 5 | c);
            }

            if (result.Length >= 2)
                result[result.Length - 2] |= 0x80;
            return result;
        }

        /// <exception cref="InvalidOperationException">Too late to change charset.</exception>
        public void SetCharset(int charsetNum, IEnumerable<byte> characters)
        {
            if (Frozen)
                throw new InvalidOperationException("Too late to change charset");

            var cs = new List<byte>(26);

            foreach (var b in characters)
                cs.Add(b);

            while (cs.Count < 26)
                cs.Insert(0, (byte)' ');

            if (charsetNum == 2)
                cs.RemoveRange(0, 2);

            charset[charsetNum] = cs.ToArray();
        }

        public static bool IsPrintable(byte zscii, int zversion)
        {
            switch (zscii)
            {
                case 9:
                case 11:
                    // only printable in V6
                    return zversion == 6;

                case 10:
                    // technically unprintable, but in encoded strings we translate it to a printable newline
                    return true;

                case 0:
                case 13:
                case var _ when zscii >= 32 && zscii <= 126:
                case var _ when zscii >= 155 && zscii <= 251:
                    // printable in all versions
                    return true;

                default:
                    // unprintable
                    return false;
            }
        }
    }
}
