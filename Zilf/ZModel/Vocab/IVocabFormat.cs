using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Diagnostics.Contracts;
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
            Contract.Requires(zo != null);
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
            Contract.Requires(name != null);
            Contract.Ensures(Contract.Result<IGlobalBuilder>() != null);
            return GetGlobalDelegate(name);
        }

        [CanBeNull]
        public IOperand CompileConstant([NotNull] ZilObject zo)
        {
            Contract.Requires(zo != null);
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

    [ContractClass(typeof(IVocabFormatContracts))]
    interface IVocabFormat
    {
        [NotNull]
        IWord CreateWord([NotNull] ZilAtom text);
        void WriteToBuilder([NotNull] IWord word, [NotNull] IWordBuilder wb, WriteToBuilderHelpers helpers);

        [NotNull]
        string[] GetReservedGlobalNames();

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
    }

    [ContractClassFor(typeof(IVocabFormat))]
    [SuppressMessage("ReSharper", "AssignNullToNotNullAttribute")]
    abstract class IVocabFormatContracts : IVocabFormat
    {
        public void BuildLateSyntaxTables(BuildLateSyntaxTablesHelpers helpers)
        {
            Contract.Requires(helpers.IsValid);
        }

        public IWord CreateWord(ZilAtom text)
        {
            Contract.Requires(text != null);
            Contract.Ensures(Contract.Result<IWord>() != null);
            return null;
        }

        public byte GetAdjectiveValue(IWord word)
        {
            Contract.Requires(word != null);
            return 0;
        }

        public byte GetDirectionValue(IWord word)
        {
            Contract.Requires(word != null);
            return 0;
        }

        public string[] GetLateSyntaxTableNames()
        {
            Contract.Ensures(Contract.Result<string[]>() != null);
            return null;
        }

        public byte GetPrepositionValue(IWord word)
        {
            Contract.Requires(word != null);
            return 0;
        }

        public string[] GetReservedGlobalNames()
        {
            Contract.Ensures(Contract.Result<string[]>() != null);
            return null;
        }

        public byte GetVerbValue(IWord word)
        {
            Contract.Requires(word != null);
            return 0;
        }

        public IEnumerable<KeyValuePair<string, int>> GetVocabConstants(IWord word)
        {
            Contract.Requires(word != null);
            Contract.Ensures(Contract.Result<IEnumerable<KeyValuePair<string, int>>>() != null);
            throw new NotImplementedException();
        }

        public bool IsAdjective(IWord word)
        {
            Contract.Requires(word != null);
            return false;
        }

        public bool IsBuzzword(IWord word)
        {
            Contract.Requires(word != null);
            return false;
        }

        public bool IsDirection(IWord word)
        {
            Contract.Requires(word != null);
            return false;
        }

        public bool IsObject(IWord word)
        {
            Contract.Requires(word != null);
            return false;
        }

        public bool IsPreposition(IWord word)
        {
            Contract.Requires(word != null);
            return false;
        }

        public bool IsSynonym(IWord word)
        {
            Contract.Requires(word != null);
            return false;
        }

        public bool IsVerb(IWord word)
        {
            Contract.Requires(word != null);
            return false;
        }

        public void MakeAdjective(IWord word, ISourceLine location)
        {
            Contract.Requires(word != null);
        }

        public void MakeBuzzword(IWord word, ISourceLine location)
        {
            Contract.Requires(word != null);
        }

        public void MakeDirection(IWord word, ISourceLine location)
        {
            Contract.Requires(word != null);
        }

        public void MakeObject(IWord word, ISourceLine location)
        {
            Contract.Requires(word != null);
        }

        public void MakePreposition(IWord word, ISourceLine location)
        {
            Contract.Requires(word != null);
        }

        public void MakeSyntaxPreposition(IWord word, ISourceLine location)
        {
            Contract.Requires(word != null);
        }

        public void MakeSynonym(IWord synonym, IWord original)
        {
            Contract.Requires(synonym != null);
            Contract.Requires(original != null);
        }

        public void MakeSynonym(IWord synonym, IWord original, PartOfSpeech partOfSpeech)
        {
            Contract.Requires(synonym != null);
            Contract.Requires(original != null);
            Contract.Requires(
                partOfSpeech == PartOfSpeech.Adjective || partOfSpeech == PartOfSpeech.Direction ||
                partOfSpeech == PartOfSpeech.Preposition || partOfSpeech == PartOfSpeech.Verb);
        }

        public void MakeVerb(IWord word, ISourceLine location)
        {
            Contract.Requires(word != null);
        }

        public void MergeWords(IWord dest, IWord src)
        {
            Contract.Requires(dest != null);
            Contract.Requires(src != null);
        }

        public void WriteToBuilder(IWord word, IWordBuilder wb, WriteToBuilderHelpers helpers)
        {
            Contract.Requires(word != null);
            Contract.Requires(wb != null);
            Contract.Requires(helpers.IsValid);
        }
    }
}
