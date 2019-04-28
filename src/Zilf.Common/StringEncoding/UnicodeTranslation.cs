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

using System.Collections;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Diagnostics;
using System.Linq;
using System.Runtime.InteropServices;
using System.Runtime.InteropServices.ComTypes;

namespace Zilf.Common.StringEncoding
{
    public static class UnicodeTranslation
    {
        public static byte ToZscii(char c) => Table.TryGetValue(c, out byte b) ? b : (byte)c;

        public static readonly IReadOnlyDictionary<char, byte> Table = MakeDefaultUnicodeTable();

        private static IReadOnlyDictionary<char, byte> MakeDefaultUnicodeTable()
        {
            //                           1    1         1         1         1         2         2         2
            //                           5    6         7         8         9         0         1         2
            //                           567890123456789012345678901234567890123456789012345678901234567890123
            const string SExtraChars = @"äöüÄÖÜß»«ëïÿËÏáéíóúýÁÉÍÓÚÝàèìòùÀÈÌÒÙâêîôûÂÊÎÔÛåÅøØãñõÃÑÕæÆçÇþðÞÐ£œŒ¡¿";

            var result = new Dictionary<char, byte>(SExtraChars.Length);
            byte value = 155;

            foreach (var c in SExtraChars)
                result.Add(c, value++);

            Debug.Assert(value - 1 == 223);

            return result;
        }
    }
}
