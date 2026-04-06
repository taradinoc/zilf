/* Copyright 2010-2026 Tara McGrew
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
using System.Linq;

namespace Zilf.Emit.Cornerstone
{
    internal sealed class PackedTextEncoding
    {
        public const int DirectSymbolCount = 29;
        public const int ShiftAlphabet1Symbol = 29;
        public const int ShiftAlphabet2Symbol = 30;
        public const int EscapeAsciiSymbol = 31;

        private const int SymbolsPerWord = 3;

        private readonly Dictionary<char, byte> alphabet0Codes;
        private readonly Dictionary<char, byte> alphabet1Codes;
        private readonly Dictionary<char, byte> alphabet2Codes;

        private PackedTextEncoding(byte[] alphabet0, byte[] alphabet1, byte[] alphabet2)
        {
            Alphabet0 = alphabet0;
            Alphabet1 = alphabet1;
            Alphabet2 = alphabet2;
            alphabet0Codes = BuildCodeMap(alphabet0);
            alphabet1Codes = BuildCodeMap(alphabet1);
            alphabet2Codes = BuildCodeMap(alphabet2);
        }

        public byte[] Alphabet0 { get; }

        public byte[] Alphabet1 { get; }

        public byte[] Alphabet2 { get; }

        public static PackedTextEncoding Create(IReadOnlyDictionary<char, long> histogram)
        {
            var rankedCharacters = histogram
                .Where(pair => pair.Key <= 0x7F)
                .OrderByDescending(pair => pair.Value)
                .ThenBy(pair => pair.Key)
                .Select(pair => (byte)pair.Key)
                .Distinct()
                .ToList();

            return new PackedTextEncoding(
                FillAlphabet(rankedCharacters.Take(DirectSymbolCount)),
                FillAlphabet(rankedCharacters.Skip(DirectSymbolCount).Take(DirectSymbolCount)),
                FillAlphabet(rankedCharacters.Skip(DirectSymbolCount * 2).Take(DirectSymbolCount)));
        }

        public byte[] Encode(string text)
        {
            List<byte> symbols = [];
            foreach (var ch in text)
            {
                if (alphabet0Codes.TryGetValue(ch, out var directCode))
                {
                    symbols.Add(directCode);
                    continue;
                }

                if (alphabet1Codes.TryGetValue(ch, out var alphabet1Code))
                {
                    symbols.Add(ShiftAlphabet1Symbol);
                    symbols.Add(alphabet1Code);
                    continue;
                }

                if (alphabet2Codes.TryGetValue(ch, out var alphabet2Code))
                {
                    symbols.Add(ShiftAlphabet2Symbol);
                    symbols.Add(alphabet2Code);
                    continue;
                }

                symbols.Add(EscapeAsciiSymbol);
                symbols.Add((byte)(ch >> 5));
                symbols.Add((byte)(ch & 0x1F));
            }

            List<byte> bytes = [];
            WriteWord(bytes, checked((ushort)symbols.Count));

            for (var index = 0; index < symbols.Count; index += SymbolsPerWord)
            {
                var first = symbols[index];
                var second = index + 1 < symbols.Count ? symbols[index + 1] : 0;
                var third = index + 2 < symbols.Count ? symbols[index + 2] : 0;
                var packedWord = (ushort)((first << 10) | (second << 5) | third);
                WriteWord(bytes, packedWord);
            }

            return bytes.ToArray();
        }

        private static byte[] FillAlphabet(IEnumerable<byte> rankedCharacters)
        {
            var alphabet = rankedCharacters.Take(DirectSymbolCount).ToList();
            while (alphabet.Count < DirectSymbolCount)
                alphabet.Add((byte)'?');

            return alphabet.ToArray();
        }

        private static Dictionary<char, byte> BuildCodeMap(IReadOnlyList<byte> alphabet)
        {
            Dictionary<char, byte> result = new();
            for (byte index = 0; index < alphabet.Count; index++)
            {
                var ch = (char)alphabet[index];
                if (!result.ContainsKey(ch))
                    result.Add(ch, index);
            }

            return result;
        }

        private static void WriteWord(List<byte> bytes, ushort value)
        {
            bytes.Add((byte)(value & 0xFF));
            bytes.Add((byte)(value >> 8));
        }
    }
}