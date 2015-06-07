using System.Diagnostics.Contracts;

namespace Zilf.ZModel.Vocab
{
    class PrepSynonym : Synonym
    {
        public PrepSynonym(Word original, Word synonym)
            : base(original, synonym)
        {
            Contract.Requires(original != null);
            Contract.Requires(synonym != null);
        }
    }
}