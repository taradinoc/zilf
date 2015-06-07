/* Copyright 2010, 2015 Jesse McGrew
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