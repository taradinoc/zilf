/* Copyright 2010-2016 Jesse McGrew
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

using System.Collections.Generic;
using System.Text;

namespace Zilf.Common.StringEncoding
{
    /// <summary>
    /// Implements the Boyer-Moore-Horspool algorithm for abbreviation searches.
    /// </summary>
    /// <remarks>http://en.wikipedia.org/wiki/Boyer-Moore-Horspool_algorithm</remarks>
    public class Horspool
    {
        readonly string needle;
        readonly CharMap badCharSkip;

        public Horspool(string needle)
        {
            this.needle = needle;

            int nlen = needle.Length;
            badCharSkip = new CharMap(nlen);

            int last = nlen - 1;
            for (int i = 0; i < last; i++)
                badCharSkip[needle[i]] = last - i;
        }

        public string Text
        {
            get { return needle; }
        }

        public int FindIn(string haystack)
        {
            return FindIn(haystack, 0);
        }

        public int FindIn(string haystack, int startIndex)
        {
            int hlen = haystack.Length - startIndex;
            int hstart = startIndex;
            int nlen = needle.Length;
            int last = nlen - 1;

            while (hlen >= nlen)
            {
                for (int i = last; haystack[hstart + i] == needle[i]; i--)
                    if (i == 0)
                        return hstart;

                int skip = badCharSkip[haystack[hstart + last]];
                hlen -= skip;
                hstart += skip;
            }

            return -1;
        }

        public int FindIn(StringBuilder haystack)
        {
            return FindIn(haystack, 0);
        }

        public int FindIn(StringBuilder haystack, int startIndex)
        {
            int hlen = haystack.Length - startIndex;
            int hstart = startIndex;
            int nlen = needle.Length;
            int last = nlen - 1;

            while (hlen >= nlen)
            {
                for (int i = last; haystack[hstart + i] == needle[i]; i--)
                    if (i == 0)
                        return hstart;

                int skip = badCharSkip[haystack[hstart + last]];
                hlen -= skip;
                hstart += skip;
            }

            return -1;
        }

        class CharMap
        {
            readonly int[] small = new int[256];
            readonly int defaultValue;
            Dictionary<char, int> big;

            public CharMap(int defaultValue)
            {
                this.defaultValue = defaultValue;

                for (int i = 0; i < 256; i++)
                    small[i] = defaultValue;
            }

            public int DefaultValue
            {
                get { return defaultValue; }
            }

            public int this[char c]
            {
                get
                {
                    if (c < (char)256)
                        return small[(int)c];

                    int result;
                    if (big == null || big.TryGetValue(c, out result) == false)
                        return defaultValue;
                    else
                        return result;
                }
                set
                {
                    if (c < (char)256)
                    {
                        small[(int)c] = value;
                    }
                    else
                    {
                        if (big == null)
                            big = new Dictionary<char, int>();

                        big[c] = value;
                    }
                }
            }
        }
    }
}
