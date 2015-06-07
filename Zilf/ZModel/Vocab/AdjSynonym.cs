using System.Diagnostics.Contracts;

namespace Zilf.ZModel.Vocab
{
    class AdjSynonym : Synonym
    {
        public AdjSynonym(Word original, Word synonym)
            : base(original, synonym)
        {
            Contract.Requires(original != null);
            Contract.Requires(synonym != null);
        }
    }
}