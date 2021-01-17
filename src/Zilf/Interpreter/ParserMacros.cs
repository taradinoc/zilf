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
            new Dictionary<char, SimplePrefixMacroHandler>();

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
