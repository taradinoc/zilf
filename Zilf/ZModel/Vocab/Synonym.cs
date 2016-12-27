/* Copyright 2010-2016 Jesse McGrew
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
        public readonly IWord OriginalWord;
        public readonly IWord SynonymWord;

        public Synonym(IWord original, IWord synonym)
        {
            Contract.Requires(original != null);
            Contract.Requires(synonym != null);

            this.OriginalWord = original;
            this.SynonymWord = synonym;
        }

        [ContractInvariantMethod]
        void ObjectInvariant()
        {
            Contract.Invariant(OriginalWord != null);
            Contract.Invariant(SynonymWord != null);
        }

        public virtual void Apply(Context ctx)
        {
            Contract.Requires(ctx != null);

            ctx.ZEnvironment.VocabFormat.MakeSynonym(SynonymWord, OriginalWord);
        }
    }
}