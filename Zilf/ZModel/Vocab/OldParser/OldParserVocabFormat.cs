/* Copyright 2010-2017 Jesse McGrew
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
using System.Diagnostics.Contracts;
using System.Linq;
using Zilf.Diagnostics;
using Zilf.Emit;
using Zilf.Interpreter;
using Zilf.Interpreter.Values;
using Zilf.Language;

namespace Zilf.ZModel.Vocab.OldParser
{
    class OldParserVocabFormat : IVocabFormat
    {
        readonly Context ctx;

        byte nextPreposition = 255, nextAdjective = 255, nextBuzzword = 255, nextVerb = 255;

        public OldParserVocabFormat(Context ctx)
        {
            this.ctx = ctx;
        }

        public IWord CreateWord(ZilAtom text) => new OldParserWord(text);

        public void MergeWords(IWord dest, IWord src)
        {
            ((OldParserWord)dest).Merge(ctx, (OldParserWord)src);
        }

        void MakePart(IWord word, ISourceLine location, PartOfSpeech part, string description,
            Func<OldParserWord, Action<Context, ISourceLine, byte>> getSetter, ref byte next, bool onlyNumberedInV3 = false)
        {
            var result = (OldParserWord)word;

            if ((result.PartOfSpeech & part) != 0)
                return;

            byte value;

            if (!onlyNumberedInV3 || ctx.ZEnvironment.ZVersion == 3)
            {
                if (next == 0)
                    throw new InterpreterError(
                        InterpreterMessages.Too_Many_0_Only_1_Allowed_In_This_Vocab_Format,
                        description,
                        255);

                value = next--;
            }
            else
            {
                value = 0;
            }

            getSetter(result)(ctx, location, value);
        }

        public void MakePreposition(IWord word, ISourceLine location) => MakePart(word, location,
            PartOfSpeech.Preposition, "prepositions", w => w.SetPreposition, ref nextPreposition);

        public void MakeSyntaxPreposition(IWord word, ISourceLine location) => MakePreposition(word, location);

        public void MakeAdjective(IWord word, ISourceLine location) => MakePart(word, location,
            PartOfSpeech.Adjective, "adjectives", w => w.SetAdjective, ref nextAdjective, onlyNumberedInV3: true);

        public void MakeObject(IWord result, ISourceLine location)
        {
            ((OldParserWord)result).SetObject(ctx, location);
        }

        public void MakeBuzzword(IWord word, ISourceLine location) => MakePart(word, location,
            PartOfSpeech.Buzzword, "buzzwords", w => w.SetBuzzword, ref nextBuzzword);

        public void MakeVerb(IWord word, ISourceLine location) => MakePart(word, location,
            PartOfSpeech.Verb, "verbs", w => w.SetVerb, ref nextVerb);

        public void MakeDirection(IWord word, ISourceLine location)
        {
            var result = (OldParserWord)word;

            if ((result.PartOfSpeech & PartOfSpeech.Direction) == 0)
            {
                var index = ctx.ZEnvironment.Directions.IndexOf(result.Atom);
                if (index == -1)
                    throw new ArgumentException("Not a direction");

                result.SetDirection(ctx, location, (byte)index);
            }
        }

        public IEnumerable<KeyValuePair<string, int>> GetVocabConstants(IWord word)
        {
            var opw = (OldParserWord)word;
            var rawWord = opw.Atom.Text;

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

        public void WriteToBuilder(IWord word, IWordBuilder wb, WriteToBuilderHelpers helpers)
        {
            ((OldParserWord)word).WriteToBuilder(ctx, wb, helpers.DirIndexToPropertyOperand);
        }

        static bool CheckPart(IWord word, PartOfSpeech part)
        {
            return (((OldParserWord)word).PartOfSpeech & part) != 0;
        }

        public bool IsPreposition(IWord word) => CheckPart(word, PartOfSpeech.Preposition);
        public bool IsAdjective(IWord word) => CheckPart(word, PartOfSpeech.Adjective);
        public bool IsObject(IWord word) => CheckPart(word, PartOfSpeech.Object);
        public bool IsBuzzword(IWord word) => CheckPart(word, PartOfSpeech.Buzzword);
        public bool IsVerb(IWord word) => CheckPart(word, PartOfSpeech.Verb);
        public bool IsDirection(IWord word) => CheckPart(word, PartOfSpeech.Direction);

        public bool IsSynonym(IWord word) => ((OldParserWord)word).SynonymTypes != PartOfSpeech.None;

        public void MakeSynonym(IWord synonym, IWord original) => MergeWords(synonym, original);
        public void MakeSynonym(IWord synonym, IWord original, PartOfSpeech partOfSpeech) =>
            MakeSynonym(synonym, original);

        static byte GetPart(IWord word, PartOfSpeech part)
        {
            return ((OldParserWord)word).GetValue(part);
        }

        public byte GetPrepositionValue(IWord word) => GetPart(word, PartOfSpeech.Preposition);
        public byte GetAdjectiveValue(IWord word) => GetPart(word, PartOfSpeech.Adjective);
        public byte GetVerbValue(IWord word) => GetPart(word, PartOfSpeech.Verb);
        public byte GetDirectionValue(IWord word) => GetPart(word, PartOfSpeech.Direction);

        public string[] GetReservedGlobalNames()
        {
            return new[] { "PREPOSITIONS", "ACTIONS", "PREACTIONS", "VERBS" };
        }

        public string[] GetLateSyntaxTableNames()
        {
            return new[] { "PRTBL" };
        }

        public int MaxActionCount => 255;

        public void BuildLateSyntaxTables(BuildLateSyntaxTablesHelpers helpers)
        {
            var prepositionsTable = (ITableBuilder)helpers.CompileConstant(ctx.GetStdAtom(StdAtom.PRTBL));
            var actionsTable = (ITableBuilder)helpers.CompileConstant(ctx.GetStdAtom(StdAtom.ATBL));
            var preactionsTable = (ITableBuilder)helpers.CompileConstant(ctx.GetStdAtom(StdAtom.PATBL));
            var verbsTable = (ITableBuilder)helpers.CompileConstant(ctx.GetStdAtom(StdAtom.VTBL));

            Contract.Assert(prepositionsTable != null);
            Contract.Assert(actionsTable != null);
            Contract.Assert(preactionsTable != null);
            Contract.Assert(verbsTable != null);

            helpers.GetGlobal(ctx.GetStdAtom(StdAtom.PREPOSITIONS)).DefaultValue = prepositionsTable;
            helpers.GetGlobal(ctx.GetStdAtom(StdAtom.ACTIONS)).DefaultValue = actionsTable;
            helpers.GetGlobal(ctx.GetStdAtom(StdAtom.PREACTIONS)).DefaultValue = preactionsTable;
            helpers.GetGlobal(ctx.GetStdAtom(StdAtom.VERBS)).DefaultValue = verbsTable;

            // preposition table
            var compactVocab = ctx.GetGlobalOption(StdAtom.COMPACT_VOCABULARY_P);

            // map all relevant preposition word builders to the preposition ID constants
            var query = from pair in helpers.Vocabulary
                        let word = pair.Key
                        where IsPreposition(word) && (compactVocab || !IsSynonym(word))
                        let builder = pair.Value
                        let prAtom = ZilAtom.Parse("PR?" + word.Atom.Text, ctx)
                        let prConstant = helpers.CompileConstant(prAtom)
                        let prepValue = GetPrepositionValue(word)
                        group new { builder, prConstant } by prepValue into g
                        let constant = g.First(w => w.prConstant != null).prConstant
                        from prep in g
                        select new { prep.builder, constant };
            var prepositions = query.ToArray();

            // build the table
            prepositionsTable.AddShort((short)prepositions.Length);

            foreach (var p in prepositions)
            {
                prepositionsTable.AddShort(p.builder);

                if (compactVocab)
                    prepositionsTable.AddByte(p.constant);
                else
                    prepositionsTable.AddShort(p.constant);
            }
        }
    }
}
