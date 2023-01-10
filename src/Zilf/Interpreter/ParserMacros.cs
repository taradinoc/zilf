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

using System;
using System.Collections.Generic;
using System.Diagnostics.Contracts;
using System.Text;
using Zilf.Interpreter.Values;
using Zilf.Language.Parsing;

namespace Zilf.Interpreter
{
    delegate ParserOutput SimplePrefixMacroHandlerWithContext(Context ctx, ZilObject zo);

    class ParserMacros
    {
        readonly Dictionary<char, SimplePrefixMacroHandler> prefixMacros =
            new();

        readonly Context ctx;

        public ParserMacros(Context ctx)
        {
            this.ctx = ctx;
        }

        public void MakePrefixMacro(char prefix, SimplePrefixMacroHandlerWithContext? convert)
        {
            switch (prefix)
            {
                case '.':
                case Bang.Dot:
                case '!':
                case '\\':
                case Bang.Backslash:
                case var _ when prefix.IsNonAtomChar():
                    throw new ArgumentException($"Reserved character: '{prefix}'", nameof(prefix));
            }

            if (convert != null)
            {
                var closureCtx = ctx;
                prefixMacros[prefix] = zo => convert(closureCtx, zo);
            }
            else
            {
                prefixMacros.Remove(prefix);
            }
        }

        [Pure]
        public SimplePrefixMacroHandler? GetPrefixMacro(char prefix)
        {
            prefixMacros.TryGetValue(prefix, out var result);
            return result;
        }
    }
}
