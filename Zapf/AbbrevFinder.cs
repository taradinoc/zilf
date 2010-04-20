/* Copyright 2010 Jesse McGrew
 * 
 * This file is part of ZAPF.
 * 
 * ZAPF is free software: you can redistribute it and/or modify it
 * under the terms of the GNU General Public License as published by
 * the Free Software Foundation, either version 3 of the License, or
 * (at your option) any later version.
 * 
 * ZAPF is distributed in the hope that it will be useful, but
 * WITHOUT ANY WARRANTY; without even the implied warranty of
 * MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the GNU
 * General Public License for more details.
 * 
 * You should have received a copy of the GNU General Public License
 * along with ZAPF.  If not, see <http://www.gnu.org/licenses/>.
 */

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Zapf
{
    class AbbrevFinder
    {
        public struct Result
        {
            public readonly int Score, Count;
            public readonly string Text;

            public Result(int score, int count, string text)
            {
                this.Score = score;
                this.Count = count;
                this.Text = text;
            }
        }

        private readonly Dictionary<string, int> words = new Dictionary<string, int>();
        private readonly StringBuilder allText = new StringBuilder();
        private readonly StringEncoder encoder = new StringEncoder();

        /// <summary>
        /// Adds some text to the accumulator.
        /// </summary>
        /// <param name="text">The text to add.</param>
        public void AddText(string text)
        {
            allText.Append(text);
            allText.Append('\0');

            foreach (string word in FindWords(text))
                if (!words.ContainsKey(word))
                    words.Add(word, CountSavings(word));
        }

        /// <summary>
        /// Gets the number of characters in the accumulator.
        /// </summary>
        public int Position
        {
            get { return allText.Length; }
        }

        /// <summary>
        /// Rolls the accumulator back to a previous state.
        /// </summary>
        /// <param name="position">The number of characters to keep.</param>
        public void Rollback(int position)
        {
            if (position < allText.Length && position >= 0)
                allText.Length = position;
        }

        private readonly static char[] wordDelimiters = { ' ', '.', ',', ':', ';', '!', '?', '(', ')', '/' };

        private IEnumerable<string> FindWords(string text)
        {
            int wordStart = -1, wordEnd = -1;
            bool inWord = false;

            for (int i = 0; i < text.Length; i++)
            {
                char c = text[i];
                if (inWord)
                {
                    if (Array.IndexOf(wordDelimiters, c) >= 0)
                    {
                        inWord = false;
                        wordEnd = i;
                    }
                    else
                    {
                        continue;
                    }
                }
                else
                {
                    if (c == ' ')
                    {
                        continue;
                    }
                    else
                    {
                        inWord = true;
                        wordStart = i;
                        continue;
                    }
                }

                // found a word
                string word = text.Substring(wordStart, wordEnd - wordStart);
                bool prev = (wordStart > 0), next = (wordEnd < text.Length);

                yield return word;
                if (prev)
                    yield return text[wordStart - 1] + word;
                if (prev & next)
                    yield return text[wordStart - 1] + word + text[wordEnd];
                if (next)
                    yield return word + text[wordEnd];
            }
        }

        private int CountSavings(string word)
        {
            int zchars;
            encoder.Encode(word, 0, true, out zchars);
            return zchars - 2;
        }

        private int CountAppearances(Horspool pattern)
        {
            int count = 0, index = -1;
            while (true)
            {
                index = pattern.FindIn(allText, index + 1);
                if (index == -1)
                    break;
                count++;
            }
            return count;
        }

        /// <summary>
        /// Returns a sequence of abbreviations and clears the previously added text.
        /// </summary>
        /// <param name="max">The maximum number of abbreviations to return.</param>
        /// <returns>A sequence of abbreviations, in descending order of overall savings.</returns>
        public IEnumerable<Result> GetResults(int max)
        {
            try
            {
                if (max < 1)
                    yield break;

                var query =
                    from p in words
                    where p.Value > 0
                    let hp = new Horspool(p.Key)
                    let count = CountAppearances(hp)
                    where count > 1
                    let overallSavings = (count - 1) * p.Value - 2
                    where overallSavings > 0
                    orderby overallSavings descending
                    select new
                    {
                        Savings = overallSavings,
                        Count = count,
                        Pattern = hp
                    };

                int numResults = 0;
                while (numResults < max)
                {
                    using (var enumerator = query.GetEnumerator())
                    {
                        if (enumerator.MoveNext())
                        {
                            var r = enumerator.Current;
                            string word = r.Pattern.Text;
                            yield return new Result(r.Savings, r.Count, word);

                            numResults++;
                            if (numResults >= max)
                                yield break;

                            int idx;
                            while ((idx = r.Pattern.FindIn(allText)) >= 0)
                            {
                                allText.Remove(idx, word.Length);
                                allText.Insert(idx, '\0');
                            }

                            words.Remove(word);
                        }
                        else
                            break;
                    }
                }
            }
            finally
            {
                words.Clear();
                allText.Length = 0;
                allText.Capacity = 0;
            }
        }
    }
}
