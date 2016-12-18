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

using System;
using System.Collections.Generic;
using System.Diagnostics.Contracts;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Zilf.Emit;
using Zilf.Interpreter;
using Zilf.Interpreter.Values;
using Zilf.Language;
using Zilf.ZModel.Values;
using Zilf.Diagnostics;

namespace Zilf.ZModel.Vocab.NewParser
{
    class NewParserVocabFormat : IVocabFormat
    {
        readonly Context ctx;
        readonly int adjClass, buzzClass, dirClass, objectClass, particleClass, prepClass, verbClass;

        byte nextAdjective = 255;

        public NewParserVocabFormat(Context ctx)
        {
            this.ctx = ctx;

            adjClass = TranslateType(ctx, ctx.GetStdAtom(StdAtom.TADJ)).Value;
            buzzClass = TranslateType(ctx, ctx.GetStdAtom(StdAtom.TBUZZ)).Value;
            dirClass = TranslateType(ctx, ctx.GetStdAtom(StdAtom.TDIR)).Value;
            objectClass = TranslateType(ctx, ctx.GetStdAtom(StdAtom.TOBJECT)).Value;
            particleClass = TranslateType(ctx, ctx.GetStdAtom(StdAtom.PARTICLE)).Value;
            prepClass = TranslateType(ctx, ctx.GetStdAtom(StdAtom.TPREP)).Value;
            verbClass = TranslateType(ctx, ctx.GetStdAtom(StdAtom.TVERB)).Value;

            if (AnyDuplicates(adjClass, buzzClass, dirClass, objectClass, prepClass, verbClass))
                //throw new InterpreterError(InterpreterMessages.GETCLASSIFICATION_Must_Return_Different_Values_For_ADJ_BUZZ_DIR_NOUN_PREP_And_VERB);
                throw new InterpreterError(
                    InterpreterMessages._0_1_Must_Return_2,
                    InterpreterMessages.NoFunction,
                    "GET-CLASSIFICATION",
                    "different values for ADJ, BUZZ, DIR, NOUN, PREP, and VERB");
        }

        static bool AnyDuplicates<T>(params T[] args)
        {
            var seen = new HashSet<T>();

            foreach (var e in args)
                if (!seen.Add(e))
                    return true;

            return false;
        }

        public string[] GetReservedGlobalNames()
        {
            return new[] { "ACTIONS", "PREACTIONS" };
        }

        public IWord CreateWord(ZilAtom atom)
        {
            var form = new ZilForm(new ZilObject[]
            {
                ctx.GetStdAtom(StdAtom.MAKE_VWORD),
                ZilString.FromString(atom.Text),
                ZilFix.Zero,
                ZilFix.Zero
            });
            var vword = form.Eval(ctx) as ZilHash;

            if (vword == null || vword.GetTypeAtom(ctx).StdAtom != StdAtom.VWORD)
                throw new InterpreterError(
                    InterpreterMessages._0_1_Must_Return_2,
                    InterpreterMessages.NoFunction,
                    "MAKE-VWORD",
                    "a VWORD");

            return new NewParserWord(ctx, atom, vword);
        }

        public byte GetAdjectiveValue(IWord word)
        {
            var nw = (NewParserWord)word;

            if (ctx.ZEnvironment.ZVersion >= 4)
                throw new InvalidOperationException("No adjective numbers in V4+");

            if (!nw.HasClass(adjClass))
                throw new InvalidOperationException(string.Format("Not an adjective (class={0}, adjClass={1})", nw.Classification, adjClass));

            var adjId = nw.AdjId;
            if (adjId is ZilFix)
                return (byte)((ZilFix)adjId).Value;

            throw new NotImplementedException("Unexpected AdjId value: " + adjId.ToStringContext(ctx, false));
        }

        public byte GetDirectionValue(IWord word)
        {
            throw new NotImplementedException();
        }

        public byte GetPrepositionValue(IWord word)
        {
            throw new NotImplementedException();
        }

        public byte GetVerbValue(IWord word)
        {
            throw new NotImplementedException();
        }

        public IEnumerable<KeyValuePair<string, int>> GetVocabConstants(IWord word)
        {
            if (ctx.ZEnvironment.ZVersion < 4 &&
                (((NewParserWord)word).Classification & adjClass) == adjClass)
            {
                yield return new KeyValuePair<string, int>("A?" + word.Atom, GetAdjectiveValue(word));
            }
        }

        public bool IsAdjective(IWord word)
        {
            return ((NewParserWord)word).HasClass(adjClass);
        }

        public bool IsBuzzword(IWord word)
        {
            throw new NotImplementedException();
        }

        public bool IsDirection(IWord word)
        {
            return ((NewParserWord)word).HasClass(dirClass);
        }

        public bool IsObject(IWord word)
        {
            return ((NewParserWord)word).HasClass(objectClass);
        }

        public bool IsPreposition(IWord word)
        {
            return ((NewParserWord)word).HasClass(prepClass);
        }

        public bool IsVerb(IWord word)
        {
            throw new NotImplementedException();
        }

        public void MakeAdjective(IWord word, ISourceLine location)
        {
            var nw = (NewParserWord)word;

            if (!nw.HasClass(adjClass))
            {
                ZilFix value;
                if (ctx.ZEnvironment.ZVersion < 4)
                {
                    if (nextAdjective == 0)
                        throw new InvalidOperationException("Too many adjectives");

                    value = new ZilFix(nextAdjective--);
                }
                else
                {
                    value = null;
                }

                NewAddWord(
                    nw.Atom,
                    ctx.GetStdAtom(StdAtom.TADJ),
                    value,
                    ZilFix.Zero);
            }
        }

        public void MakeBuzzword(IWord word, ISourceLine location)
        {
            var nw = (NewParserWord)word;

            if (!nw.HasClass(buzzClass))        // always true since this translates to 0
            {
                NewAddWord(
                    nw.Atom,
                    ctx.GetStdAtom(StdAtom.TBUZZ),
                    null,
                    ZilFix.Zero);
            }
        }

        public void MakeDirection(IWord word, ISourceLine location)
        {
            var nw = (NewParserWord)word;

            if (!nw.HasClass(dirClass))
            {
                var index = ctx.ZEnvironment.Directions.IndexOf(nw.Atom);
                if (index == -1)
                    throw new ArgumentException("Not a direction");

                NewAddWord(
                    nw.Atom,
                    ctx.GetStdAtom(StdAtom.TDIR),
                    ZilAtom.Parse("P?" + nw.Atom.Text, ctx),
                    ZilFix.Zero);
            }
        }

        public void MakeObject(IWord word, ISourceLine location)
        {
            var nw = (NewParserWord)word;

            if (!nw.HasClass(objectClass))
            {
                NewAddWord(
                    nw.Atom,
                    ctx.GetStdAtom(StdAtom.TOBJECT),
                    null,
                    ZilFix.Zero);
            }
        }

        public void MakePreposition(IWord word, ISourceLine location)
        {
            var nw = (NewParserWord)word;

            if (!nw.HasClass(prepClass))
            {
                NewAddWord(
                    nw.Atom,
                    ctx.GetStdAtom(StdAtom.TPREP),
                    null,
                    ZilFix.Zero);
            }
        }

        public void MakeSyntaxPreposition(IWord word, ISourceLine location)
        {
            var nw = (NewParserWord)word;

            if (!nw.HasClass(particleClass))
            {
                NewAddWord(
                    nw.Atom,
                    ctx.GetStdAtom(StdAtom.PARTICLE),
                    null,
                    ZilFix.Zero);
            }
        }

        public void MakeVerb(IWord word, ISourceLine location)
        {
            var nw = (NewParserWord)word;

            if (!nw.HasClass(verbClass))
            {
                var form = new ZilForm(new ZilObject[] { ctx.GetStdAtom(StdAtom.MAKE_VERB_DATA) });
                var verbData = form.Eval(ctx);

                nw.VerbStuff = verbData;
                ctx.PutProp(verbData, ctx.GetStdAtom(StdAtom.VERB_STUFF_ID), nw.Inner);

                NewAddWord(
                    nw.Atom,
                    ctx.GetStdAtom(StdAtom.TVERB),
                    verbData,
                    ZilFix.Zero);
            }
        }

        public void MergeWords(IWord dest, IWord src)
        {
            var ndest = (NewParserWord)dest;
            var nsrc = (NewParserWord)src;

            if ((ndest.Classification & 0x8000) != (nsrc.Classification & 0x8000))
            {
                throw new InterpreterError(InterpreterMessages.Incompatible_Classifications_Merging_Words_0_1__2_3, ndest.Atom, ndest.Classification, nsrc.Atom, nsrc.Classification);
            }

            if (ctx.ZEnvironment.ZVersion >= 4 &&
                ((ndest.Classification | nsrc.Classification) & (dirClass | verbClass)) == (dirClass | verbClass) &&
                (ndest.SemanticStuff != null || ndest.DirId != null) &&
                (nsrc.SemanticStuff != null || nsrc.DirId != null))
            {
                throw new InterpreterError(InterpreterMessages.Overloaded_Semantics_Merging_Words_0__1, dest.Atom, src.Atom);
            }

            ndest.Classification |= nsrc.Classification;

            if (nsrc.DirId != null)
                ndest.DirId = nsrc.DirId;

            if (nsrc.AdjId != null)
                ndest.AdjId = nsrc.AdjId;

            if (nsrc.SemanticStuff != null)
                ndest.SemanticStuff = nsrc.SemanticStuff;
        }

        public bool IsSynonym(IWord word)
        {
            throw new NotImplementedException();
        }

        public void MakeSynonym(IWord synonym, IWord original)
        {
            var nsyn = (NewParserWord)synonym;
            var norig = (NewParserWord)original;

            nsyn.Classification = 0;
            nsyn.Flags = 0;
            nsyn.SemanticStuff = ZilAtom.Parse("W?" + original.Atom, ctx);
        }

        public void MakeSynonym(IWord synonym, IWord original, PartOfSpeech partOfSpeech)
        {
            var nsyn = (NewParserWord)synonym;
            var norig = (NewParserWord)original;

            int classification;
            switch (partOfSpeech)
            {
                case PartOfSpeech.Adjective:
                    classification = adjClass;
                    break;
                case PartOfSpeech.Direction:
                    classification = dirClass;
                    break;
                case PartOfSpeech.Preposition:
                    classification = prepClass;
                    break;
                case PartOfSpeech.Verb:
                    classification = verbClass;
                    break;
                default:
                    // shouldn't get here due to contract
                    throw new NotImplementedException();
            }

            if (!norig.HasClass(classification))
                throw new InterpreterError(InterpreterMessages.Word_0_Is_Not_A_1, norig.Atom, partOfSpeech);

            var nclass = nsyn.Classification;
            var oclass = norig.Classification;
            if (nclass != 0 && oclass != 0 && (nclass & 0x8000) != (oclass & 0x8000))
            {
                throw new InterpreterError(InterpreterMessages.Incompatible_Classifications_Merging_Words_0_1__2_3, nsyn.Atom, nclass, norig.Atom, oclass);
            }

            nsyn.Classification = nclass | oclass;

            switch (partOfSpeech)
            {
                case PartOfSpeech.Adjective:
                    if (ctx.ZEnvironment.ZVersion == 3)
                        nsyn.AdjId = norig.AdjId;
                    break;
                case PartOfSpeech.Direction:
                    nsyn.DirId = norig.DirId;
                    break;
                case PartOfSpeech.Verb:
                    nsyn.VerbStuff = norig.VerbStuff;
                    break;
            }
        }

        public void WriteToBuilder(IWord word, IWordBuilder wb, WriteToBuilderHelpers helpers)
        {
            var zversion = ctx.ZEnvironment.ZVersion;
            var nw = (NewParserWord)word;
            bool needSemanticStuff = false;

            if (zversion >= 4)
            {
                if (nw.HasClass(dirClass))
                {
                    ConditionalAddByte(wb, word.Atom.Text, helpers.CompileConstant, nw.DirId);
                    wb.AddByte(0);
                }
                else if (!nw.HasClass(verbClass))
                {
                    ConditionalAddShort(wb, word.Atom.Text, helpers.CompileConstant, nw.SemanticStuff);
                }
                else
                {
                    needSemanticStuff = true;
                }
            }
            else
            {
                bool adj = nw.HasClass(adjClass), dir = nw.HasClass(dirClass);
                if (adj || dir)
                {
                    if (adj)
                        ConditionalAddByte(wb, word.Atom.Text, helpers.CompileConstant, nw.AdjId);
                    else
                        wb.AddByte(0);

                    if (dir)
                        ConditionalAddByte(wb, word.Atom.Text, helpers.CompileConstant, nw.DirId);
                    else
                        wb.AddByte(0);
                }
                else
                {
                    ConditionalAddShort(wb, word.Atom.Text, helpers.CompileConstant, nw.SemanticStuff);
                }
            }

            bool verbed = false;
            if (nw.HasClass(verbClass))
            {
                var verbStuff = nw.VerbStuff;
                ZilObject verbStuffId;

                if (IsVerbPointer(verbStuff))
                {
                    verbStuffId = verbStuff.GetPrimitive(ctx);
                    if (verbStuffId.GetTypeAtom(ctx).StdAtom == StdAtom.VWORD)
                        verbStuffId = NewParserWord.FromVword(ctx, (ZilHash)verbStuffId).Atom;

                    Contract.Assert(verbStuffId != null);
                    var actTableAtom = ZilAtom.Parse("ACT?" + ((ZilAtom)verbStuffId).Text, ctx);
                    wb.AddShort(helpers.CompileConstant(actTableAtom));
                    verbed = true;
                }
                else if (TryGetVerbStuffId(verbStuff, out verbStuffId))
                {
                    if (verbStuffId.GetTypeAtom(ctx).StdAtom == StdAtom.VWORD)
                        verbStuffId = NewParserWord.FromVword(ctx, (ZilHash)verbStuffId).Atom;

                    Contract.Assert(verbStuffId != null);
                    var actTableAtom = ZilAtom.Parse("ACT?" + ((ZilAtom)verbStuffId).Text, ctx);
                    wb.AddShort(helpers.CompileConstant(actTableAtom));
                    verbed = true;
                }
            }

            if (!verbed)
            {
                if (zversion == 3)
                {
                    wb.AddShort(0);
                }
                else if (needSemanticStuff)
                {
                    ConditionalAddShort(wb, word.Atom.Text, helpers.CompileConstant, nw.SemanticStuff);
                }
            }

            if (!ctx.GetCompilationFlagOption(StdAtom.WORD_FLAGS_IN_TABLE))
            {
                wb.AddShort((short)nw.Flags);
            }

            if (ctx.GetCompilationFlagOption(StdAtom.ONE_BYTE_PARTS_OF_SPEECH))
            {
                var lowByte = (byte)(nw.Classification & 0x7f);
                var highByte = (byte)((nw.Classification >> 7) & 0x7f);
                if (lowByte != 0 && highByte != 0)
                {
                    ctx.HandleWarning(new CompilerError(CompilerMessages.ONEBYTEPARTSOFSPEECH_Loses_Data_For_0, word.Atom.Text));
                }

                if (highByte != 0)
                {
                    wb.AddByte((byte)(highByte | 0x80));
                }
                else
                {
                    wb.AddByte(lowByte);
                }
            }
            else
            {
                wb.AddShort((short)nw.Classification);
            }
        }

        bool TryGetVerbStuffId(ZilObject verbStuff, out ZilObject verbStuffId)
        {
            Contract.Ensures(!Contract.Result<bool>() || Contract.ValueAtReturn(out verbStuffId) != null);

            if (verbStuff == null)
            {
                verbStuffId = null;
                return false;
            }

            verbStuffId = ctx.GetProp(verbStuff, ctx.GetStdAtom(StdAtom.VERB_STUFF_ID));
            return verbStuffId != null;
        }

        bool IsVerbPointer(ZilObject verbStuff)
        {
            return verbStuff != null && verbStuff.GetTypeAtom(ctx).StdAtom == StdAtom.VERB_POINTER;
        }

        void ConditionalAddShort(IWordBuilder wb, string word, Func<ZilObject, IOperand> compileConstant, ZilObject value)
        {
            if (value == null)
            {
                wb.AddShort(0);
            }
            else
            {
                var operand = compileConstant(value);
                if (operand == null)
                {
                    ctx.HandleError(new CompilerError(
                        CompilerMessages.Nonconstant_Initializer_For_0_1_2,
                        "vocab word",
                        word,
                        value.ToString()));
                    wb.AddShort(0);
                }
                else
                {
                    wb.AddShort(operand);
                }
            }
        }

        void ConditionalAddByte(IWordBuilder wb, string word, Func<ZilObject, IOperand> compileConstant, ZilObject value)
        {
            if (value == null)
            {
                wb.AddByte(0);
            }
            else
            {
                var operand = compileConstant(value);
                if (operand == null)
                {
                    ctx.HandleError(new CompilerError(
                        CompilerMessages.Nonconstant_Initializer_For_0_1_2,
                        "vocab word",
                        word,
                        value.ToString()));
                    wb.AddByte(0);
                }
                else
                {
                    wb.AddByte(operand);
                }
            }
        }

        internal ZilObject NewAddWord(ZilAtom name, ZilAtom type, ZilObject value, ZilFix flags)
        {
            Contract.Requires(name != null);
            Contract.Requires(flags != null);

            bool typeProvided;

            if (type == null)
            {
                typeProvided = false;
                type = ctx.GetStdAtom(StdAtom.TZERO);
            }
            else
            {
                typeProvided = true;
            }

            // find new CLASS by translating TYPE
            var classification = TranslateType(ctx, type);

            // create the word or merge into the existing one
            IWord iword;
            NewParserWord word;
            if (ctx.ZEnvironment.Vocabulary.TryGetValue(name, out iword) == false)
            {
                // create it by calling user-provided <MAKE-VWORD name class flags>
                var form = new ZilForm(new ZilObject[]
                {
                    ctx.GetStdAtom(StdAtom.MAKE_VWORD),
                    ZilString.FromString(name.Text),
                    classification,
                    flags
                });

                var vword = form.Eval(ctx);

                if (vword.GetTypeAtom(ctx).StdAtom != StdAtom.VWORD)
                    throw new InterpreterError(InterpreterMessages._0_1_Must_Return_2, "NEW-ADD-WORD", "MAKE-VWORD", "a VWORD");

                word = NewParserWord.FromVword(ctx, (ZilHash)vword);
                ctx.ZEnvironment.Vocabulary.Add(name, word);
            }
            else
            {
                word = (NewParserWord)iword;

                // if old and new CLASS differ in the high bit, error (word class conflict)
                if ((word.Classification & 0x8000) != (classification.Value & 0x8000))
                    throw new InterpreterError(InterpreterMessages._0_New_Classification_1_Is_Incompatible_With_Previous_2, "NEW-ADD-WORD", classification, word.Classification);

                // merge new CLASS into the word
                var combinedClassification = word.Classification | classification.Value;

                if (ctx.ZEnvironment.ZVersion >= 4)
                {
                    if (typeProvided &&
                        (combinedClassification & (dirClass | verbClass)) == (dirClass | verbClass) &&
                        (word.SemanticStuff != null || word.DirId != null) &&
                        value != null)
                    {
                        throw new InterpreterError(InterpreterMessages._0_Word_Would_Be_Overloaded, "NEW-ADD-WORD");
                    }
                }

                word.Classification = combinedClassification;

                // merge new FLAGS into the word
                word.Flags |= flags.Value;
            }

            // store flags
            if (flags.Value != 0)
            {
                var compFlag = ctx.GetCompilationFlagValue("WORD-FLAGS-IN-TABLE");
                if (compFlag != null && compFlag.IsTrue)
                {
                    // prepend .WORD .FLAGS to ,WORD-FLAGS-LIST
                    var wordFlagsList = ctx.GetGlobalVal(ctx.GetStdAtom(StdAtom.WORD_FLAGS_LIST));
                    if (wordFlagsList == null)
                    {
                        wordFlagsList = new ZilList(null, null);
                    }
                    else if (wordFlagsList.GetTypeAtom(ctx).StdAtom != StdAtom.LIST)
                    {
                        throw new InterpreterError(InterpreterMessages._0_GVAL_Of_WORDFLAGSLIST_Must_Be_A_List, "NEW-ADD-WORD");
                    }

                    wordFlagsList = new ZilList(word.Inner, new ZilList(flags, (ZilList)wordFlagsList));
                    ctx.SetGlobalVal(ctx.GetStdAtom(StdAtom.WORD_FLAGS_LIST), wordFlagsList);
                }
            }

            if (value != null)
            {
                if (classification.Value == adjClass)
                {
                    // store VALUE as word's ADJ-ID (V3) or SEMANTIC-STUFF (V4+)
                    if (ctx.ZEnvironment.ZVersion >= 4)
                        word.SemanticStuff = value;
                    else
                        word.AdjId = value;
                }
                else if (classification.Value == dirClass)
                {
                    // store VALUE as word's DIR-ID
                    word.DirId = value;
                }
                else
                {
                    // store VALUE as word's SEMANTIC-STUFF
                    word.SemanticStuff = value;
                }
            }

            return word.Atom;
        }

        static ZilFix TranslateType(Context ctx, ZilAtom type)
        {
            Contract.Requires(ctx != null);
            Contract.Requires(type != null);
            Contract.Ensures(Contract.Result<ZilFix>() != null);

            switch (type.StdAtom)
            {
                case StdAtom.TADJ:
                    type = ctx.GetStdAtom(StdAtom.ADJ);
                    break;
                case StdAtom.TOBJECT:
                    type = ctx.GetStdAtom(StdAtom.NOUN);
                    break;
                case StdAtom.TPREP:
                    type = ctx.GetStdAtom(StdAtom.PREP);
                    break;
                case StdAtom.TDIR:
                    type = ctx.GetStdAtom(StdAtom.DIR);
                    break;
                case StdAtom.TVERB:
                    type = ctx.GetStdAtom(StdAtom.VERB);
                    break;
            }

            ZilFix classification;

            switch (type.StdAtom)
            {
                case StdAtom.BUZZ:
                case StdAtom.TBUZZ:
                case StdAtom.TZERO:
                    classification = ZilFix.Zero;
                    break;

                default:
                    // call user-provided <GET-CLASSIFICATION type>
                    var form = new ZilForm(new ZilObject[]
                    {
                        ctx.GetStdAtom(StdAtom.GET_CLASSIFICATION),
                        type
                    });

                    classification = form.Eval(ctx) as ZilFix;

                    if (classification == null)
                        throw new InterpreterError(InterpreterMessages._0_1_Must_Return_2, "NEW-ADD-WORD", "GET-CLASSIFICATION", "a FIX");

                    break;
            }

            return classification;
        }

        public string[] GetLateSyntaxTableNames()
        {
            if (ctx.GetCompilationFlagOption(StdAtom.WORD_FLAGS_IN_TABLE))
                return new[] { "WORD-FLAG-TABLE" };
            return new string[0];
        }

        public void BuildLateSyntaxTables(BuildLateSyntaxTablesHelpers helpers)
        {
            var actionsTable = helpers.CompileConstant(ctx.GetStdAtom(StdAtom.ATBL)) as ITableBuilder;
            var preactionsTable = helpers.CompileConstant(ctx.GetStdAtom(StdAtom.PATBL)) as ITableBuilder;

            Contract.Assert(actionsTable != null);
            Contract.Assert(preactionsTable != null);

            helpers.GetGlobal(ctx.GetStdAtom(StdAtom.ACTIONS)).DefaultValue = actionsTable;
            helpers.GetGlobal(ctx.GetStdAtom(StdAtom.PREACTIONS)).DefaultValue = preactionsTable;

            // word flag table
            if (ctx.GetCompilationFlagOption(StdAtom.WORD_FLAGS_IN_TABLE))
            {
                var wordFlagsList = ctx.GetGlobalVal(ctx.GetStdAtom(StdAtom.WORD_FLAGS_LIST)) as ZilList;

                if (wordFlagsList == null)
                {
                    wordFlagsList = new ZilList(null, null);
                }
                else if (wordFlagsList.GetTypeAtom(ctx).StdAtom != StdAtom.LIST)
                {
                    throw new CompilerError(CompilerMessages.GVAL_Of_0_Must_Be_1, "WORD-FLAGS-LIST", "a list");
                }

                var wordFlagTable = helpers.CompileConstant(ctx.GetStdAtom(StdAtom.WORD_FLAG_TABLE)) as ITableBuilder;
                Contract.Assert(wordFlagTable != null);

                // WORD-FLAGS-LIST may contain duplicates: (W?FOO 96 W?BAR 1 W?FOO 32)
                // only the first appearance of each word will be kept
                var seen = new HashSet<ZilObject>();
                var filtered = new List<IOperand>();

                while (!wordFlagsList.IsEmpty)
                {
                    if (wordFlagsList.Rest.IsEmpty)
                        throw new CompilerError(CompilerMessages.WORDFLAGSLIST_Must_Have_An_Even_Number_Of_Elements);

                    var vword = wordFlagsList.First;
                    var flags = wordFlagsList.Rest.First;
                    wordFlagsList = wordFlagsList.Rest.Rest;

                    if (seen.Add(vword))
                    {
                        var nw = NewParserWord.FromVword(ctx, (ZilHash)vword);
                        var atom = nw.Atom;
                        var word = ctx.ZEnvironment.Vocabulary[atom];
                        var zword = helpers.Vocabulary[word];

                        filtered.Add(zword);
                        filtered.Add(helpers.CompileConstant(flags));
                    }
                }

                wordFlagTable.AddShort((short)filtered.Count);
                foreach (var operand in filtered)
                    wordFlagTable.AddShort(operand);
            }
        }
    }
}
