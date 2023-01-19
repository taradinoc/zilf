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
using Zilf.Interpreter;
using System.Collections.Generic;
using System.Linq;
using Zilf.Interpreter.Values;
using Zilf.Language;

namespace Zilf
{
    sealed class Completer : IAutoCompleteHandler, IDisposable
    {
        readonly Context ctx;

        bool attached;

        public Completer(Context ctx)
        {
            this.ctx = ctx;
        }

        public Completer Attach()
        {
            ReadLine.AutoCompletionHandler = this;
            attached = true;
            return this;
        }

        public void Dispose()
        {
            if (attached)
            {
                attached = false;
                ReadLine.AutoCompletionHandler = null;
            }
        }

        public char[] Separators { get; set; } = { '<', '>', '[', ']', '(', ')', '{', '}', ':', ',', '.', '#', };

        public string[] GetSuggestions(string text, int pos)
        {
            // find a completable expression
            var (start, end) = FindExpression(text, pos);

            if (end <= start)
            {
                return Array.Empty<string>();
            }

            // find completions
            IEnumerable<string> completions;

            switch (text[start])
            {
                case '<':
                    completions = GetFunctionNames();
                    start++;
                    break;

                case ',':
                    completions = GetGvalNames();
                    start++;
                    break;

                case '.':
                    completions = GetLvalNames();
                    start++;
                    break;

                default:
                    return Array.Empty<string>();
            }

            string prefix;
            if (start >= 0 && end <= text.Length && start <= end)
                prefix = text[start..end];
            else
                prefix = "";

            return completions
                .Where(c => c.StartsWith(prefix, StringComparison.OrdinalIgnoreCase))
                .ToArray();
        }

        private readonly char[] delimiters = { '"', '<', '>', '(', ')', '[', ']', ';', ',', '.' };

        private (int start, int end) FindExpression(string text, int pos)
        {
            var start = text.LastIndexOfAny(delimiters, Math.Min(pos, text.Length - 1));

            if (start < 0)
                start = 0;

            var end = text.IndexOfAny(delimiters, pos);

            if (end < 0)
                end = text.Length;

            return (start, end);
        }

        private IEnumerable<string> GetLvalNames()
        {
            return from a in ctx.LocalEnvironment.GetVisibleAtoms()
                   orderby a.Text
                   select a.Text;
        }

        private IEnumerable<string> GetGvalNames()
        {
            return GetGlobalNames((name, zo) => (IsFunction(zo) ? 2 : 0) +
                                                (name.Contains("!-", StringComparison.Ordinal) ? 1 : 0));
        }

        private IEnumerable<string> GetFunctionNames()
        {
            return GetGlobalNames((name, zo) =>
                IsFunction(zo)
                    ? (name.Contains("!-", StringComparison.Ordinal) ? 1 : 0)
                    : (int?)null);
        }

        static bool IsFunction(ZilObject zo)
        {
            return zo.StdTypeAtom switch
            {
                StdAtom.FUNCTION or StdAtom.SUBR or StdAtom.FSUBR or StdAtom.ROUTINE or StdAtom.MACRO => true,
                _ => false,
            };
        }

        IEnumerable<string> GetGlobalNames(Func<string, ZilObject, int?> grouper) =>
            from b in ctx.GetGlobalBindings()
            where b.Value.Value != null
            let name = b.Key.ToStringContext(ctx, false, true)
            let groupNum = grouper(name, b.Value.Value!)
            where groupNum != null
            orderby groupNum, name
            select name;
    }
}