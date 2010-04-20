/* Copyright 2010 Jesse McGrew
 * 
 * This file is part of ZAPF.
 * 
 * ZAPF is free software: you can redistribute it and/or modify it
 * under the terms of the GNU General Public License as published by
 * the Free Software Foundation, either version 3 of the License, or
 * (at your option) any later version.
 * 
 * ZAPF is distributed in the hope that it will be useful, but
 * WITHOUT ANY WARRANTY; without even the implied warranty of
 * MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the GNU
 * General Public License for more details.
 * 
 * You should have received a copy of the GNU General Public License
 * along with ZAPF.  If not, see <http://www.gnu.org/licenses/>.
 */

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Zapf
{
    class StringEncoder
    {
        private struct AbbrevEntry
        {
            public readonly Horspool Pattern;
            public readonly byte Number;

            public AbbrevEntry(string text, byte number)
            {
                this.Pattern = new Horspool(text);
                this.Number = number;
            }
        }

        private class AbbrevComparer : IComparer<AbbrevEntry>
        {
            public int Compare(AbbrevEntry x, AbbrevEntry y)
            {
                int d = y.Pattern.Text.Length - x.Pattern.Text.Length;
                if (d != 0)
                    return d;

                return x.Pattern.Text.CompareTo(y.Pattern.Text);
            }
        }

        private string charset0 = "abcdefghijklmnopqrstuvwxyz";
        private string charset1 = "ABCDEFGHIJKLMNOPQRSTUVWXYZ";
        private string charset2 = "\n0123456789.,!?_#'\"/\\-:()";   // 1 char omitted at the beginning

        private List<AbbrevEntry> abbrevs = new List<AbbrevEntry>();
        private static AbbrevComparer abbrevLengthComparer = new AbbrevComparer();
        private bool abbrevsFrozen;

        public bool AbbrevsFrozen
        {
            get { return abbrevsFrozen; }
        }

        public void AddAbbreviation(string str)
        {
            if (abbrevsFrozen)
                throw new InvalidOperationException("Too late to add abbreviations");
            if (abbrevs.Count >= 96)
                throw new InvalidOperationException("Too many abbreviations");

            AbbrevEntry entry = new AbbrevEntry(str, (byte)abbrevs.Count);
            int idx = abbrevs.BinarySearch(entry, abbrevLengthComparer);
            abbrevs.Insert(idx < 0 ? ~idx : idx, entry);
        }

        public byte[] Encode(string str)
        {
            return Encode(str, null, false);
        }

        public byte[] Encode(string str, bool noAbbrevs)
        {
            return Encode(str, null, noAbbrevs);
        }

        public byte[] Encode(string str, int? size)
        {
            return Encode(str, size, false);
        }

        public byte[] Encode(string str, int? size, bool noAbbrevs)
        {
            int dummy;
            return Encode(str, size, noAbbrevs, out dummy);
        }

        public byte[] Encode(string str, int? size, bool noAbbrevs, out int zchars)
        {
            if (!noAbbrevs)
                abbrevsFrozen = true;

            List<byte> temp = new List<byte>();
            StringBuilder sb = new StringBuilder(str);

            // temporarily replace abbreviated strings with Unicode private use characters (E000-E05F)
            if (!noAbbrevs)
            {
                for (int i = 0; i < abbrevs.Count; i++)
                {
                    int idx = abbrevs[i].Pattern.FindIn(sb);
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
                int idx;
                if (c == ' ')
                {
                    temp.Add(0);
                }
                else if (!noAbbrevs && c >= '\ue000' && c < '\ue060')
                {
                    int abbrNum = c - '\ue000';
                    temp.Add((byte)(1 + abbrNum / 32));
                    temp.Add((byte)(abbrNum % 32));
                }
                else if ((idx = charset0.IndexOf(c)) >= 0)
                {
                    temp.Add((byte)(idx + 6));
                }
                else if ((idx = charset1.IndexOf(c)) >= 0)
                {
                    temp.Add(4);
                    temp.Add((byte)(idx + 6));
                }
                else if ((idx = charset2.IndexOf(c)) >= 0)
                {
                    temp.Add(5);
                    temp.Add((byte)(idx + 7));
                }
                else
                {
                    temp.Add(5);
                    temp.Add(6);
                    temp.Add((byte)((c >> 5) & 31));
                    temp.Add((byte)(c & 31));
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

            byte[] result = new byte[Math.Min(resultSize, temp.Count * 2 / 3)];

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
    }
}
