﻿/* Copyright 2010, 2015 Jesse McGrew
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

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Zilf.Emit;
using Zilf.Interpreter;
using Zilf.Interpreter.Values;
using Zilf.Language;

namespace Zilf.ZModel.Vocab.OldParser
{
    class OldParserVocabFormat : IVocabFormat
    {
        private readonly Context ctx;

        private byte NextPreposition = 255, NextAdjective = 255, NextBuzzword = 255, NextVerb = 255;

        public OldParserVocabFormat(Context ctx)
        {
            this.ctx = ctx;
        }

        public IWord CreateWord(ZilAtom text)
        {
            return new OldParserWord(text);
        }

        public void MergeWords(IWord dest, IWord src)
        {
            ((OldParserWord)dest).Merge(ctx, (OldParserWord)src);
        }

        public void MakePreposition(IWord word, ISourceLine location)
        {
            var result = (OldParserWord)word;

            if ((result.PartOfSpeech & PartOfSpeech.Preposition) == 0)
            {
                if (NextPreposition == 0)
                    throw new InvalidOperationException("Too many prepositions");

                result.SetPreposition(ctx, location, NextPreposition--);
            }

        }

        public void MakeAdjective(IWord word, ISourceLine location)
        {
            var result = (OldParserWord)word;

            if ((result.PartOfSpeech & PartOfSpeech.Adjective) == 0)
            {
                // adjective numbers only exist in V3
                if (ctx.ZEnvironment.ZVersion == 3)
                {
                    if (NextAdjective == 0)
                        throw new InvalidOperationException("Too many adjectives");

                    result.SetAdjective(ctx, location, NextAdjective--);
                }
                else
                {
                    result.SetAdjective(ctx, location, 0);
                }
            }
        }

        public void MakeObject(IWord result, ISourceLine location)
        {
            ((OldParserWord)result).SetObject(ctx, location);
        }

        public void MakeBuzzword(IWord word, ISourceLine location)
        {
            var result = (OldParserWord)word;

            if ((result.PartOfSpeech & PartOfSpeech.Buzzword) == 0)
            {
                if (NextBuzzword == 0)
                    throw new InvalidOperationException("Too many buzzwords");

                result.SetBuzzword(ctx, location, NextBuzzword--);
            }
        }

        public void MakeVerb(IWord word, ISourceLine location)
        {
            var result = (OldParserWord)word;

            if ((result.PartOfSpeech & PartOfSpeech.Verb) == 0)
            {
                if (NextVerb == 0)
                    throw new InvalidOperationException("Too many verbs");

                result.SetVerb(ctx, location, NextVerb--);
            }
        }

        public void MakeDirection(IWord word, ISourceLine location)
        {
            var result = (OldParserWord)word;

            if ((result.PartOfSpeech & PartOfSpeech.Direction) == 0)
            {
                int index = ctx.ZEnvironment.Directions.IndexOf(result.Atom);
                if (index == -1)
                    throw new ArgumentException("Not a direction");

                result.SetDirection(ctx, location, (byte)index);
            }
        }

        public IEnumerable<KeyValuePair<string, int>> GetVocabConstants(IWord word)
        {
            var opw = (OldParserWord)word;
            var rawWord = opw.Atom.ToString();

            // adjective numbers only exist in V3
            if (ctx.ZEnvironment.ZVersion == 3 &&
                (opw.PartOfSpeech & PartOfSpeech.Adjective) != 0)
            {
                yield return new KeyValuePair<string, int>("A?" + rawWord, opw.GetValue(PartOfSpeech.Adjective));
            }

            if ((opw.PartOfSpeech & PartOfSpeech.Verb) != 0)
            {
                yield return new KeyValuePair<string, int>("ACT?" + rawWord, opw.GetValue(PartOfSpeech.Verb));
            }

            if ((opw.PartOfSpeech & PartOfSpeech.Preposition) != 0)
            {
                yield return new KeyValuePair<string, int>("PR?" + rawWord, opw.GetValue(PartOfSpeech.Preposition));
            }
        }

        public void WriteToBuilder(IWord word, IWordBuilder wb, Func<byte, IOperand> dirIndexToPropertyOperand)
        {
            ((OldParserWord)word).WriteToBuilder(ctx, wb, dirIndexToPropertyOperand);
        }

        public bool IsPreposition(IWord word)
        {
            return (((OldParserWord)word).PartOfSpeech & PartOfSpeech.Preposition) != 0;
        }

        public bool IsAdjective(IWord word)
        {
            return (((OldParserWord)word).PartOfSpeech & PartOfSpeech.Adjective) != 0;
        }

        public bool IsObject(IWord word)
        {
            return (((OldParserWord)word).PartOfSpeech & PartOfSpeech.Object) != 0;
        }

        public bool IsBuzzword(IWord word)
        {
            return (((OldParserWord)word).PartOfSpeech & PartOfSpeech.Buzzword) != 0;
        }

        public bool IsVerb(IWord word)
        {
            return (((OldParserWord)word).PartOfSpeech & PartOfSpeech.Verb) != 0;
        }

        public bool IsDirection(IWord word)
        {
            return (((OldParserWord)word).PartOfSpeech & PartOfSpeech.Direction) != 0;
        }

        public bool IsSynonym(IWord word)
        {
            return ((OldParserWord)word).SynonymTypes != PartOfSpeech.None;
        }

        public byte GetPrepositionValue(IWord word)
        {
            return ((OldParserWord)word).GetValue(PartOfSpeech.Preposition);
        }

        public byte GetAdjectiveValue(IWord word)
        {
            return ((OldParserWord)word).GetValue(PartOfSpeech.Adjective);
        }

        public byte GetVerbValue(IWord word)
        {
            return ((OldParserWord)word).GetValue(PartOfSpeech.Verb);
        }

        public byte GetDirectionValue(IWord word)
        {
            return ((OldParserWord)word).GetValue(PartOfSpeech.Direction);
        }
    }
}