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

using System.Collections.Generic;

namespace Zilf.Common.StringEncoding
{
    public static class UnicodeTranslation
    {
        public static byte ToZscii(char c) => Table.TryGetValue(c, out byte b) ? b : (byte)c;

        public static readonly IReadOnlyDictionary<char, byte> Table =
            new Dictionary<char, byte>(69)
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
                { '¿', 223 }
            };
    }
}
