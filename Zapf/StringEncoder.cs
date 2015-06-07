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

        private static readonly string[] DefaultCharset = {
            "abcdefghijklmnopqrstuvwxyz",
            "ABCDEFGHIJKLMNOPQRSTUVWXYZ",
            "0123456789.,!?_#'\"/\\-:()",   // 2 chars omitted at the beginning
        };
        private byte[][] charset;

        private List<AbbrevEntry> abbrevs = new List<AbbrevEntry>();
        private static AbbrevComparer abbrevLengthComparer = new AbbrevComparer();
        private bool frozen;

        private readonly Dictionary<char, byte> unicodeTranslations;

        public StringEncoder()
        {
            // TODO: share unicode translation table with Zilf.Emit
            unicodeTranslations = new Dictionary<char, byte>(69)
            {
                { 'ä', 155 },
                { 'ö', 156 },
                { 'ü', 157 },
                { 'Ä', 158 },
                { 'Ö', 159 },
                { 'Ü', 160 },
                { 'ß', 161 },
                { '»', 162 },
                { '«', 163 },
                { 'ë', 164 },
                { 'ï', 165 },
                { 'ÿ', 166 },
                { 'Ë', 167 },
                { 'Ï', 168 },
                { 'á', 169 },
                { 'é', 170 },
                { 'í', 171 },
                { 'ó', 172 },
                { 'ú', 173 },
                { 'ý', 174 },
                { 'Á', 175 },
                { 'É', 176 },
                { 'Í', 177 },
                { 'Ó', 178 },
                { 'Ú', 179 },
                { 'Ý', 180 },
                { 'à', 181 },
                { 'è', 182 },
                { 'ì', 183 },
                { 'ò', 184 },
                { 'ù', 185 },
                { 'À', 186 },
                { 'È', 187 },
                { 'Ì', 188 },
                { 'Ò', 189 },
                { 'Ù', 190 },
                { 'â', 191 },
                { 'ê', 192 },
                { 'î', 193 },
                { 'ô', 194 },
                { 'û', 195 },
                { 'Â', 196 },
                { 'Ê', 197 },
                { 'Î', 198 },
                { 'Ô', 199 },
                { 'Û', 200 },
                { 'å', 201 },
                { 'Å', 202 },
                { 'ø', 203 },
                { 'Ø', 204 },
                { 'ã', 205 },
                { 'ñ', 206 },
                { 'õ', 207 },
                { 'Ã', 208 },
                { 'Ñ', 209 },
                { 'Õ', 210 },
                { 'æ', 211 },
                { 'Æ', 212 },
                { 'ç', 213 },
                { 'Ç', 214 },
                { 'þ', 215 },
                { 'ð', 216 },
                { 'Þ', 217 },
                { 'Ð', 218 },
                { '£', 219 },
                { 'œ', 220 },
                { 'Œ', 221 },
                { '¡', 222 },
                { '¿', 223 },
            };

            // convert characters in DefaultCharset through unicode mapping and into bytes
            charset = DefaultCharset.Select(
                s => s.Select(c =>
                {
                    byte b;
                    if (unicodeTranslations.TryGetValue(c, out b))
                    {
                        return b;
                    }
                    else
                    {
                        return (byte)c;
                    }
                }).ToArray()).ToArray();
        }

        public bool Frozen
        {
            get { return frozen; }
        }

        public void AddAbbreviation(string str)
        {
            if (frozen)
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
                frozen = true;

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
                    byte b;
                    if (unicodeTranslations.TryGetValue(c, out b) == false)
                    {
                        b = (byte)c;
                    }

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

        public void SetCharset(int charsetNum, IEnumerable<byte> characters)
        {
            if (frozen)
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
    }
}
