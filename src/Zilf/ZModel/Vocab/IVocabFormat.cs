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

using System.Collections.Generic;
using Zilf.Emit;
using Zilf.Interpreter.Values;
using Zilf.Language;

namespace Zilf.ZModel.Vocab
{
    delegate IOperand DirIndexToPropertyOperandDelegate(byte dirIndex);

    delegate IOperand? CompileConstantDelegate(ZilObject zo);

    struct WriteToBuilderHelpers
    {
        public DirIndexToPropertyOperandDelegate DirIndexToPropertyOperandDelegate;
        public CompileConstantDelegate CompileConstantDelegate;

        public IOperand DirIndexToPropertyOperand(byte dirIndex) => DirIndexToPropertyOperandDelegate(dirIndex);

        public IOperand? CompileConstant(ZilObject zo) => CompileConstantDelegate(zo);
    }

    delegate IGlobalBuilder GetGlobalDelegate(ZilAtom name);

    struct BuildLateSyntaxTablesHelpers
    {
        public IDictionary<IWord, IWordBuilder> Vocabulary;
        public CompileConstantDelegate CompileConstantDelegate;
        public GetGlobalDelegate GetGlobalDelegate;

        public IGlobalBuilder GetGlobal(ZilAtom name) => GetGlobalDelegate(name);

        public IOperand? CompileConstant(ZilObject zo) => CompileConstantDelegate(zo);
    }

    interface IVocabFormat
    {
        IWord CreateWord(ZilAtom text);
        void WriteToBuilder(IWord word, IWordBuilder wb, WriteToBuilderHelpers helpers);

        string[] GetReservedGlobalNames();

        string[] GetLateSyntaxTableNames();

        void BuildLateSyntaxTables(BuildLateSyntaxTablesHelpers helpers);

        void MergeWords(IWord dest, IWord src);
        void MakeSynonym(IWord synonym, IWord original);
        void MakeSynonym(IWord synonymWord, IWord originalWord, PartOfSpeech partOfSpeech);
        bool IsSynonym(IWord word);

        void MakePreposition(IWord word, ISourceLine location);
        void MakeAdjective(IWord word, ISourceLine location);
        void MakeObject(IWord word, ISourceLine location);
        void MakeBuzzword(IWord word, ISourceLine location);
        void MakeVerb(IWord word, ISourceLine location);
        void MakeDirection(IWord word, ISourceLine location);

        // Used for prepositions defined in syntax lines, as opposed to ones defined with VOC.
        void MakeSyntaxPreposition(IWord word, ISourceLine location);

        bool IsPreposition(IWord word);
        bool IsAdjective(IWord word);
        bool IsObject(IWord word);
        bool IsBuzzword(IWord word);
        bool IsVerb(IWord word);
        bool IsDirection(IWord word);

        IEnumerable<KeyValuePair<string, int>> GetVocabConstants(IWord word);

        byte GetPrepositionValue(IWord word);
        byte GetAdjectiveValue(IWord word);
        byte GetVerbValue(IWord word);
        byte GetDirectionValue(IWord word);

        int MaxActionCount { get; }
    }
}
