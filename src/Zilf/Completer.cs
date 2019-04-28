using System;
using Zilf.Interpreter;
using System.Collections.Generic;
using System.Linq;
using JetBrains.Annotations;
using Zilf.Interpreter.Values;
using Zilf.Language;

namespace Zilf
{
    class Completer : IAutoCompleteHandler, IDisposable
    {
        [NotNull]
        readonly Context ctx;

        bool attached;

        public Completer([NotNull] Context ctx)
        {
            this.ctx = ctx;
        }

        [NotNull]
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

        [NotNull, ItemNotNull]
        public string[] GetSuggestions([NotNull] string text, int pos)
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
                prefix = text.Substring(start, end - start);
            else
                prefix = "";


            return completions
                .Where(c => c.StartsWith(prefix, StringComparison.OrdinalIgnoreCase))
                .ToArray();
        }

        private readonly char[] delimiters = { '"', '<', '>', '(', ')', '[', ']', ';', ',', '.' };

        private (int start, int end) FindExpression([NotNull] string text, int pos)
        {
            var start = text.LastIndexOfAny(delimiters, Math.Min(pos, text.Length - 1));

            if (start < 0)
                start = 0;

            var end = text.IndexOfAny(delimiters, pos);

            if (end < 0)
                end = text.Length;

            return (start, end);
        }

        [NotNull, ItemNotNull]
        private IEnumerable<string> GetLvalNames()
        {
            return from a in ctx.LocalEnvironment.GetVisibleAtoms()
                   orderby a.Text
                   select a.Text;
        }

        [NotNull, ItemNotNull]
        private IEnumerable<string> GetGvalNames()
        {
            return GetGlobalNames((name, zo) => (IsFunction(zo) ? 2 : 0) +
                                                (name.Contains("!-") ? 1 : 0));
        }

        [NotNull, ItemNotNull]
        private IEnumerable<string> GetFunctionNames()
        {
            return GetGlobalNames((name, zo) =>
                IsFunction(zo)
                    ? (name.Contains("!-") ? 1 : 0)
                    : (int?)null);
        }

        static bool IsFunction([NotNull] ZilObject zo)
        {
            switch (zo.StdTypeAtom)
            {
                case StdAtom.FUNCTION:
                case StdAtom.SUBR:
                case StdAtom.FSUBR:
                case StdAtom.ROUTINE:
                case StdAtom.MACRO:
                    return true;

                default:
                    return false;
            }
        }

        [NotNull, ItemNotNull, LinqTunnel]
        IEnumerable<string> GetGlobalNames([NotNull] Func<string, ZilObject, int?> grouper)
        {
            return from b in ctx.GetGlobalBindings()
                   where b.Value.Value != null
                   let name = b.Key.ToStringContext(ctx, false, true)
                   let groupNum = grouper(name, b.Value.Value)
                   where groupNum != null
                   orderby groupNum, name
                   select name;
        }
    }
}