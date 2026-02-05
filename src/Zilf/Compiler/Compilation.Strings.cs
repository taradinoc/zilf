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

using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Text;
using Zilf.Interpreter;
using Zilf.Interpreter.Values;
using Zilf.Language;
using Zilf.Common.StringEncoding;
using Zilf.Diagnostics;
using Zilf.ZModel;

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

        public static string TranslateString(ZilString zstr, Context ctx)
        {
            var crlfChar = ctx.GetGlobalVal(ctx.GetStdAtom(StdAtom.CRLF_CHARACTER)) as ZilChar;
            return TranslateString(
                zstr,
                ctx,
                crlfChar?.Char ?? '|',
                GetSpacesMode(ctx));
        }

        [SuppressMessage("ReSharper", "ConvertIfStatementToReturnStatement")]
        static StringSpacesMode GetSpacesMode(Context ctx)
        {
            if (ctx.GetGlobalOption(StdAtom.PRESERVE_SPACES_P))
                return StringSpacesMode.Preserve;

            if ((ctx.CurrentFile.Flags & FileFlags.SentenceEnds) != 0)
                return StringSpacesMode.CollapseWithSentenceSpace;

            return StringSpacesMode.CollapseAfterPeriod;
        }

        const char SentenceSpaceChar = '\u000b';

        static string TranslateString(ZilString zstr, Context ctx, char crlfChar, StringSpacesMode spacesMode)
        {
            // strip CR/LF and ensure 1 space afterward, translate crlfChar to LF,
            // and collapse two spaces after '.' or crlfChar into one
            var sb = new StringBuilder(zstr.Text);
            char? last = null;
            bool sawDotSpace = false;

            var zversion = ctx.ZEnvironment.ZVersion;
            var isGlulx = ctx.IsGlulx;
            var unicodeUsage = ctx.ZEnvironment.UnicodeUsage;

            static bool IsStandardZsciiChar(char c, out bool isDefaultExtra)
            {
                isDefaultExtra = UnicodeTranslation.Table.ContainsKey(c);

                return c == '\0' || c == '\r' || c == '\n' ||
                    (c >= 32 && c <= 126) || isDefaultExtra;
            }

            static string? DescribeChar(byte zscii)
            {
                return zscii switch
                {
                    8 => "backspace",
                    9 => "tab",
                    11 => "sentence space",
                    27 => "escape",
                    _ when zscii < 32 => ("^" + (char)(zscii + 64)),
                    _ when zscii < 127 => ("'" + (char)zscii + "'"),
                    _ => null
                };
            }

            for (int i = 0; i < sb.Length; i++)
            {
                char c = sb[i];
                if (!isGlulx && c != crlfChar)
                {
                    bool isControl = c < 32 && c != '\r' && c != '\n';

                    if (isControl)
                    {
                        var b = (byte)c;
                        if (!StringEncoder.IsPrintable(b, zversion))
                        {
                            var warning = new CompilerError(zstr,
                                CompilerMessages.ZSCII_0_1_Cannot_Safely_Be_Printed_In_Zmachine_Version_2,
                                b,
                                DescribeChar(b) ?? "<???>",
                                zversion);

                            ctx.HandleError(warning);
                        }
                    }
                    else
                    {
                        var isStandard = IsStandardZsciiChar(c, out bool isDefaultExtra);

                        if (!isStandard)
                        {
                            if (zversion <= 4)
                            {
                                ctx.HandleError(new CompilerError(
                                    zstr,
                                    CompilerMessages.Character_0_Is_Not_Part_Of_Standard_ZSCII_And_Cannot_Be_Printed_In_Zmachine_Version_1,
                                    c,
                                    zversion));
                            }
                            else
                            {
                                if (!unicodeUsage.TryRequireCustomChar(c, out var rejected))
                                {
                                    Debug.Assert(rejected.HasValue);
                                    ctx.HandleError(new CompilerError(
                                        zstr,
                                        CompilerMessages.No_Room_Left_In_Unicode_Translation_Table_For_Character_0,
                                        rejected!.Value));
                                }
                            }
                        }
                        else
                        {
                            if (isDefaultExtra)
                            {
                                if (!unicodeUsage.TryNoteDefaultChar(c, out var rejected))
                                {
                                    Debug.Assert(rejected.HasValue);
                                    ctx.HandleError(new CompilerError(
                                        zstr,
                                        CompilerMessages.No_Room_Left_In_Unicode_Translation_Table_For_Character_0,
                                        rejected!.Value));
                                    continue;
                                }
                            }

                            var b = UnicodeTranslation.ToZscii(c);
                            if (!StringEncoder.IsPrintable(b, zversion))
                            {
                                var warning = new CompilerError(zstr,
                                    CompilerMessages.ZSCII_0_1_Cannot_Safely_Be_Printed_In_Zmachine_Version_2,
                                    b,
                                    DescribeChar(b) ?? "<???>",
                                    zversion);

                                ctx.HandleError(warning);
                            }
                        }
                    }
                }

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
