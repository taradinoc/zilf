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

using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Diagnostics.Contracts;
using System.Linq;
using System.Text;
using Zilf.Compiler.Builtins;
using Zilf.Diagnostics;
using Zilf.Emit;
using Zilf.Interpreter;
using Zilf.Interpreter.Values;
using Zilf.Language;
using Zilf.ZModel;
using Zilf.ZModel.Values;
using Zilf.ZModel.Vocab;

namespace Zilf.Compiler
{
    partial class Compilation
    {
        public static string TranslateString(string str, Context ctx)
        {
            Contract.Requires(ctx != null);

            var crlfChar = ctx.GetGlobalVal(ctx.GetStdAtom(StdAtom.CRLF_CHARACTER)) as ZilChar;
            return TranslateString(
                str,
                crlfChar == null ? '|' : crlfChar.Char,
                ctx.GetGlobalOption(StdAtom.PRESERVE_SPACES_P));
        }

        public static string TranslateString(string str, char crlfChar, bool preserveSpaces)
        {
            // strip CR/LF and ensure 1 space afterward, translate crlfChar to LF,
            // and collapse two spaces after '.' or crlfChar into one
            var sb = new StringBuilder(str);
            char? last = null;
            bool sawDotSpace = false;

            for (int i = 0; i < sb.Length; i++)
            {
                char c = sb[i];

                if (!preserveSpaces)
                {
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
