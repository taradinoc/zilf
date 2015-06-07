using System.Diagnostics.Contracts;

namespace Zilf.ZModel.Vocab
{
    class VerbSynonym : Synonym
    {
        public VerbSynonym(Word original, Word synonym)
            : base(original, synonym)
        {
            Contract.Requires(original != null);
            Contract.Requires(synonym != null);
        }
    }
}