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

using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using System.Globalization;
using System.IO;

// ReSharper disable once CheckNamespace
namespace ZilfAnalyzers.Test.Helpers
{
    // adapted from https://gist.github.com/Haacked/1610603
    public static class DiffHelper
    {
        public static void ShouldEqualWithDiff(this string actualValue, string expectedValue)
        {
            ShouldEqualWithDiff(actualValue, expectedValue, DiffStyle.Full, Console.Out);
        }

        public static void ShouldEqualWithDiff(this string actualValue, string expectedValue, DiffStyle diffStyle)
        {
            ShouldEqualWithDiff(actualValue, expectedValue, diffStyle, Console.Out);
        }

        public static void ShouldEqualWithDiff(this string? actualValue, string? expectedValue, DiffStyle diffStyle, TextWriter output)
        {
            if (actualValue == null || expectedValue == null)
            {
                Assert.AreEqual(expectedValue, actualValue);
                return;
            }

            if (actualValue.Equals(expectedValue, StringComparison.Ordinal)) return;

            output.WriteLine("  Idx Expected  Actual");
            output.WriteLine("-------------------------");
            int maxLen = Math.Max(actualValue.Length, expectedValue.Length);
            int minLen = Math.Min(actualValue.Length, expectedValue.Length);
            for (int i = 0; i < maxLen; i++)
            {
                if (diffStyle != DiffStyle.Minimal || i >= minLen || actualValue[i] != expectedValue[i])
                {
                    output.WriteLine("{0} {1,-3} {2,-4} {3,-3}  {4,-4} {5,-3}",
                        i < minLen && actualValue[i] == expectedValue[i] ? " " : "*", // put a mark beside a differing row
                        i, // the index
                        i < expectedValue.Length ? ((int)expectedValue[i]).ToString() : "", // character decimal value
                        i < expectedValue.Length ? expectedValue[i].ToSafeString() : "", // character safe string
                        i < actualValue.Length ? ((int)actualValue[i]).ToString() : "", // character decimal value
                        i < actualValue.Length ? actualValue[i].ToSafeString() : "" // character safe string
                    );
                }
            }
            output.WriteLine();

            Assert.AreEqual(expectedValue, actualValue);
        }

        static string ToSafeString(this char c)
        {
            return c switch
            {
                '\r' => @"\r",
                '\n' => @"\n",
                '\t' => @"\t",
                '\a' => @"\a",
                '\v' => @"\v",
                '\f' => @"\f",
                _ => char.IsControl(c) || char.IsWhiteSpace(c)
                    ? $"\\u{(int)c:X};"
                    : c.ToString(CultureInfo.InvariantCulture),
            };
        }
    }

    public enum DiffStyle
    {
        Full,
        Minimal
    }
}
