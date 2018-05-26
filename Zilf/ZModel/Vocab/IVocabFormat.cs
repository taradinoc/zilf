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
using Zilf.Emit;
using Zilf.Interpreter.Values;
using Zilf.Language;

namespace Zilf.ZModel.Vocab
{
    [CanBeNull]
    delegate IOperand DirIndexToPropertyOperandDelegate(byte dirIndex);

    [CanBeNull]
    delegate IOperand CompileConstantDelegate([NotNull] ZilObject zo);

    struct WriteToBuilderHelpers
    {
        public DirIndexToPropertyOperandDelegate DirIndexToPropertyOperandDelegate;
        public CompileConstantDelegate CompileConstantDelegate;

        [CanBeNull]
        public IOperand DirIndexToPropertyOperand(byte dirIndex)
        {
            return DirIndexToPropertyOperandDelegate(dirIndex);
        }

        [CanBeNull]
        public IOperand CompileConstant([NotNull] ZilObject zo)
        {
            return CompileConstantDelegate(zo);
        }

        public bool IsValid
        {
            [System.Diagnostics.Contracts.Pure]
            get
            {
                return DirIndexToPropertyOperandDelegate != null && CompileConstantDelegate != null;
            }
        }
    }

    [NotNull]
    delegate IGlobalBuilder GetGlobalDelegate([NotNull] ZilAtom name);

    struct BuildLateSyntaxTablesHelpers
    {
        public IDictionary<IWord, IWordBuilder> Vocabulary;
        public CompileConstantDelegate CompileConstantDelegate;
        public GetGlobalDelegate GetGlobalDelegate;

        [NotNull]
        public IGlobalBuilder GetGlobal([NotNull] ZilAtom name)
        {
            return GetGlobalDelegate(name);
        }

        [CanBeNull]
        public IOperand CompileConstant([NotNull] ZilObject zo)
        {
            return CompileConstantDelegate(zo);
        }

        public bool IsValid
        {
            [System.Diagnostics.Contracts.Pure]
            get
            {
                return Vocabulary != null && CompileConstantDelegate != null && GetGlobalDelegate != null;
            }
        }
    }
    [PublicAPI]
    interface IVocabFormat
    {
        [NotNull]
        IWord CreateWord([NotNull] ZilAtom text);
        void WriteToBuilder([NotNull] IWord word, [NotNull] IWordBuilder wb, WriteToBuilderHelpers helpers);

        [ItemNotNull]
        [NotNull]
        string[] GetReservedGlobalNames();

        [ItemNotNull]
        [NotNull]
        string[] GetLateSyntaxTableNames();

        void BuildLateSyntaxTables(BuildLateSyntaxTablesHelpers helpers);

        void MergeWords([NotNull] IWord dest, [NotNull] IWord src);
        void MakeSynonym([NotNull] IWord synonym, [NotNull] IWord original);
        void MakeSynonym([NotNull] IWord synonymWord, [NotNull] IWord originalWord, PartOfSpeech partOfSpeech);
        bool IsSynonym([NotNull] IWord word);

        void MakePreposition([NotNull] IWord word, [CanBeNull] ISourceLine location);
        void MakeAdjective([NotNull] IWord word, [CanBeNull] ISourceLine location);
        void MakeObject([NotNull] IWord word, [CanBeNull] ISourceLine location);
        void MakeBuzzword([NotNull] IWord word, [CanBeNull] ISourceLine location);
        void MakeVerb([NotNull] IWord word, [CanBeNull] ISourceLine location);
        void MakeDirection([NotNull] IWord word, [CanBeNull] ISourceLine location);

        // Used for prepositions defined in syntax lines, as opposed to ones defined with VOC.
        void MakeSyntaxPreposition([NotNull] IWord word, [CanBeNull] ISourceLine location);

        bool IsPreposition([NotNull] IWord word);
        bool IsAdjective([NotNull] IWord word);
        bool IsObject([NotNull] IWord word);
        bool IsBuzzword([NotNull] IWord word);
        bool IsVerb([NotNull] IWord word);
        bool IsDirection([NotNull] IWord word);

        [NotNull]
        IEnumerable<KeyValuePair<string, int>> GetVocabConstants([NotNull] IWord word);

        byte GetPrepositionValue([NotNull] IWord word);
        byte GetAdjectiveValue([NotNull] IWord word);
        byte GetVerbValue([NotNull] IWord word);
        byte GetDirectionValue([NotNull] IWord word);

        int MaxActionCount { get; }
    }
}
