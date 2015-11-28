using System;
using System.Collections.Generic;
using System.Diagnostics.Contracts;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Zilf.Emit;
using Zilf.Interpreter.Values;
using Zilf.Language;

namespace Zilf.ZModel.Vocab
{
    struct WriteToBuilderHelpers
    {
        public Func<byte, IOperand> DirIndexToPropertyOperand;
        public Func<ZilObject, IOperand> CompileConstant;

        public bool IsValid
        {
            [Pure]
            get
            {
                return DirIndexToPropertyOperand != null && CompileConstant != null;
            }
        }
    }

    struct BuildLateSyntaxTablesHelpers
    {
        public IDictionary<IWord, IWordBuilder> Vocabulary;
        public Func<ZilObject, IOperand> CompileConstant;
        public Func<ZilAtom, IGlobalBuilder> GetGlobal;

        public bool IsValid
        {
            [Pure]
            get
            {
                return Vocabulary != null && CompileConstant != null && GetGlobal != null;
            }
        }
    }

    [ContractClass(typeof(IVocabFormatContracts))]
    interface IVocabFormat
    {
        IWord CreateWord(ZilAtom text);
        void WriteToBuilder(IWord word, IWordBuilder wb, WriteToBuilderHelpers helpers);
        string[] GetReservedGlobalNames();
        string[] GetLateSyntaxTableNames();
        void BuildLateSyntaxTables(BuildLateSyntaxTablesHelpers helpers);

        void MergeWords(IWord dest, IWord src);
        void MakeSynonym(IWord synonym, IWord original);
        bool IsSynonym(IWord word);

        void MakePreposition(IWord word, ISourceLine location);
        void MakeAdjective(IWord word, ISourceLine location);
        void MakeObject(IWord word, ISourceLine location);
        void MakeBuzzword(IWord word, ISourceLine location);
        void MakeVerb(IWord word, ISourceLine location);
        void MakeDirection(IWord word, ISourceLine location);

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
    }

    [ContractClassFor(typeof(IVocabFormat))]
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

        public void MakeSynonym(IWord synonym, IWord original)
        {
            Contract.Requires(synonym != null);
            Contract.Requires(original != null);
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
