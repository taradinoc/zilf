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

using System.Collections.Generic;
using System.Diagnostics;
using System.Diagnostics.Contracts;
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
            Contract.Requires(id >= 0);
            Contract.Requires(charset0 != null && charset0.Length == 26);
            Contract.Requires(charset1 != null && charset1.Length == 26);
            Contract.Requires(charset2 != null && charset2.Length == 24);
            Contract.Requires(specialChars != null);
            Contract.Requires(specialChars.Length % 2 == 0);

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

        [ContractInvariantMethod]
        [Conditional("CONTRACTS_FULL")]
        void ObjectInvariant()
        {
            Contract.Invariant(Id >= 0);
            Contract.Invariant(Charset0 != null && Charset0.Length == 26);
            Contract.Invariant(Charset1 != null && Charset1.Length == 26);
            Contract.Invariant(Charset2 != null && Charset2.Length == 24);
            Contract.Invariant(SpecialChars != null);
            Contract.Invariant(Default != null);
            Contract.Invariant(German != null);
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
            Contract.Requires(name != null);

            allLanguages.TryGetValue(name, out var result);
            return result;
        }
    }
}