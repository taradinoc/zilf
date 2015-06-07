using System.Diagnostics.Contracts;
using Zilf.Interpreter;

namespace Zilf.ZModel.Vocab
{
    class Synonym
    {
        public readonly Word OriginalWord;
        public readonly Word SynonymWord;

        public Synonym(Word original, Word synonym)
        {
            Contract.Requires(original != null);
            Contract.Requires(synonym != null);

            this.OriginalWord = original;
            this.SynonymWord = synonym;
        }

        [ContractInvariantMethod]
        private void ObjectInvariant()
        {
            Contract.Invariant(OriginalWord != null);
            Contract.Invariant(SynonymWord != null);
        }

        public virtual void Apply(Context ctx)
        {
            Contract.Requires(ctx != null);

            if ((OriginalWord.PartOfSpeech & PartOfSpeech.Adjective) != 0)
                SynonymWord.SetAdjective(ctx, OriginalWord.GetDefinition(PartOfSpeech.Adjective), OriginalWord.GetValue(PartOfSpeech.Adjective));

            if ((OriginalWord.PartOfSpeech & PartOfSpeech.Buzzword) != 0)
                SynonymWord.SetBuzzword(ctx, OriginalWord.GetDefinition(PartOfSpeech.Buzzword), OriginalWord.GetValue(PartOfSpeech.Buzzword));

            if ((OriginalWord.PartOfSpeech & PartOfSpeech.Direction) != 0)
                SynonymWord.SetDirection(ctx, OriginalWord.GetDefinition(PartOfSpeech.Direction), OriginalWord.GetValue(PartOfSpeech.Direction));

            if ((OriginalWord.PartOfSpeech & PartOfSpeech.Object) != 0)
                SynonymWord.SetObject(ctx, OriginalWord.GetDefinition(PartOfSpeech.Object));

            if ((OriginalWord.PartOfSpeech & PartOfSpeech.Preposition) != 0)
                SynonymWord.SetPreposition(ctx, OriginalWord.GetDefinition(PartOfSpeech.Preposition), OriginalWord.GetValue(PartOfSpeech.Preposition));

            if ((OriginalWord.PartOfSpeech & PartOfSpeech.Verb) != 0)
                SynonymWord.SetVerb(ctx, OriginalWord.GetDefinition(PartOfSpeech.Verb), OriginalWord.GetValue(PartOfSpeech.Verb));

            SynonymWord.MarkAsSynonym(OriginalWord.PartOfSpeech & ~PartOfSpeech.FirstMask);
        }
    }
}