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

using System.Diagnostics.Contracts;
using System.Text;
using Zilf.Interpreter;
using Zilf.Interpreter.Values;
using Zilf.Language;
using JetBrains.Annotations;

namespace Zilf.Compiler
{
    partial class Compilation
    {
        public enum StringSpacesMode
        {
            /// <summary>
            /// Two spaces after a period become one space. This is the default.
            /// </summary>
            CollapseAfterPeriod,
            /// <summary>
            /// Preserve spaces exactly as in the source code.
            /// </summary>
            Preserve,
            /// <summary>
            /// Two spaces after a period, question mark, or exclamation point become a sentence space (ZSCII 11). V6 only.
            /// </summary>
            CollapseWithSentenceSpace,
        }

        [NotNull]
        public static string TranslateString(string str, [NotNull] Context ctx)
        {
            Contract.Requires(ctx != null);

            var crlfChar = ctx.GetGlobalVal(ctx.GetStdAtom(StdAtom.CRLF_CHARACTER)) as ZilChar;
            return TranslateString(
                str,
                crlfChar?.Char ?? '|',
                GetSpacesMode(ctx));
        }

        static StringSpacesMode GetSpacesMode(Context ctx)
        {
            if (ctx.GetGlobalOption(StdAtom.PRESERVE_SPACES_P))
                return StringSpacesMode.Preserve;

            if ((ctx.CurrentFile.Flags & FileFlags.SentenceEnds) != 0)
                return StringSpacesMode.CollapseWithSentenceSpace;

            return StringSpacesMode.CollapseAfterPeriod;
        }

        const char SentenceSpaceChar = '\u000b';

        [NotNull]
        static string TranslateString(string str, char crlfChar, StringSpacesMode spacesMode)
        {
            // strip CR/LF and ensure 1 space afterward, translate crlfChar to LF,
            // and collapse two spaces after '.' or crlfChar into one
            var sb = new StringBuilder(str);
            char? last = null;
            bool sawDotSpace = false;

            for (int i = 0; i < sb.Length; i++)
            {
                char c = sb[i];

                switch (spacesMode)
                {
                    case StringSpacesMode.CollapseAfterPeriod:
                        if ((last == '.' || last == crlfChar) && c == ' ')
                        {
                            sawDotSpace = true;
                        }
                        else if (sawDotSpace && c == ' ')
                        {
                            sb.Remove(i--, 1);
                            sawDotSpace = false;
                            last = c;
                            continue;
                        }
                        else
                        {
                            sawDotSpace = false;
                        }
                        break;

                    case StringSpacesMode.CollapseWithSentenceSpace:
                        if ((last == '.' || last == '?' || last == '!') && c == ' ')
                        {
                            sawDotSpace = true;
                        }
                        else if (sawDotSpace && c == ' ')
                        {
                            sb.Remove(i--, 1);
                            sb[i] = SentenceSpaceChar;
                            sawDotSpace = false;
                            last = c;
                            continue;
                        }
                        else
                        {
                            sawDotSpace = false;
                        }
                        break;
                }

                switch (c)
                {
                    case '\r':
                        sb.Remove(i--, 1);
                        continue;

                    case '\n':
                        if (last == crlfChar)
                            sb.Remove(i--, 1);
                        else
                            sb[i] = ' ';
                        break;

                    default:
                        if (c == crlfChar)
                            sb[i] = '\n';
                        break;
                }

                last = c;
            }

            return sb.ToString();
        }
    }
}
