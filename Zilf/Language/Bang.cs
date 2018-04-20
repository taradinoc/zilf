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

namespace Zilf.Language
{
    /// <summary>
    /// Character constants for ASCII symbols with the 8th bit set,
    /// used to indicate that the symbol was prefixed with an exclamation point.
    /// </summary>
    static class Bang
    {
        public const char Backslash = (char)('\\' + 128);
        public const char Colon = (char)(':' + 128);
        public const char Comma = (char)(',' + 128);
        public const char Dot = (char)('.' + 128);
        public const char DoubleQuote = (char)('"' + 128);
        public const char Hash = (char)('#' + 128);
        public const char LeftAngle = (char)('<' + 128);
        public const char LeftBracket = (char)('[' + 128);
        public const char LeftCurly = (char)('{' + 128);
        public const char LeftParen = (char)('(' + 128);
        public const char Percent = (char)('%' + 128);
        public const char RightAngle = (char)('>' + 128);
        public const char RightBracket = (char)(']' + 128);
        public const char RightCurly = (char)('}' + 128);
        public const char RightParen = (char)(')' + 128);
        public const char Semicolon = (char)(';' + 128);
        public const char SingleQuote = (char)('\'' + 128);
    }
}