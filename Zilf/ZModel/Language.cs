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

using System.Collections.Generic;
using JetBrains.Annotations;

namespace Zilf.ZModel
{
    sealed class Language
    {
        public int Id { get; }
        public string Charset0 { get; }
        public string Charset1 { get; }
        public string Charset2 { get; }
        [NotNull]
        public IReadOnlyDictionary<char, char> SpecialChars { get; }

        Language(int id, string charset0, string charset1, string charset2, [NotNull] params char[] specialChars)
        {
            Id = id;
            Charset0 = charset0;
            Charset1 = charset1;
            Charset2 = charset2;

            var specialCharDict = new Dictionary<char, char>(specialChars.Length / 2);
            for (int i = 0; i < specialChars.Length - 1; i += 2)
            {
                specialCharDict.Add(specialChars[i], specialChars[i + 1]);
            }

            SpecialChars = specialCharDict;
        }

        [NotNull]
        public static readonly Language Default = new Language(
            0,
            "abcdefghijklmnopqrstuvwxyz",
            "ABCDEFGHIJKLMNOPQRSTUVWXYZ",
            "0123456789.,!?_#'\"/\\-:()");

        [NotNull]
        public static readonly Language German = new Language(
            1,
            "abcdefghiklmnoprstuwzäöü.,",
            "ABCDEFGHIKLMNOPRSTUWZjqvxy",
            "0123456789!?'-:()JÄÖÜß«»",
            'a', 'ä',
            'o', 'ö',
            'u', 'ü',
            's', 'ß',
            'A', 'Ä',
            'O', 'Ö',
            'U', 'Ü',
            '<', '«',
            '>', '»');

        static readonly Dictionary<string, Language> allLanguages = new Dictionary<string, Language>
        {
            { "DEFAULT", Default },
            { "GERMAN", German }
        };

        [CanBeNull]
        [System.Diagnostics.Contracts.Pure]
        public static Language Get([NotNull] string name)
        {
            allLanguages.TryGetValue(name, out var result);
            return result;
        }
    }
}