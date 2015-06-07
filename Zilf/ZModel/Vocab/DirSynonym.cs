using System.Diagnostics.Contracts;

namespace Zilf.ZModel.Vocab
{
    class DirSynonym : Synonym
    {
        public DirSynonym(Word original, Word synonym)
            : base(original, synonym)
        {
            Contract.Requires(original != null);
            Contract.Requires(synonym != null);
        }
    }
}