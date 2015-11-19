using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Zilf.Emit;
using Zilf.Interpreter.Values;
using Zilf.Language;

namespace Zilf.ZModel.Vocab
{
    interface IVocabFormat
    {
        IWord CreateWord(ZilAtom text);
        void WriteToBuilder(IWord word, IWordBuilder wb, Func<byte, IOperand> dirIndexToPropertyOperand);

        void MergeWords(IWord dest, IWord src);
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
}
