/* Copyright 2010-2018 Jesse McGrew
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

using JetBrains.Annotations;
using Zilf.Diagnostics;
using Zilf.Interpreter;
using Zilf.Interpreter.Values;
using Zilf.Language;

namespace Zilf.ZModel.Vocab.NewParser
{
    class NewParserWord : IWord
    {
        [NotNull]
        readonly Context ctx;

        public NewParserWord([NotNull] Context ctx, [NotNull] ZilAtom atom, [NotNull] ZilHash vword)
        {
            this.ctx = ctx;
            Atom = atom;
            Inner = vword;
        }

        [NotNull]
        public static NewParserWord FromVword([NotNull] Context ctx, [NotNull] ZilHash vword)
        {
            var form = new ZilForm(new ZilObject[]
            {
                ctx.GetStdAtom(StdAtom.WORD_LEXICAL_WORD),
                vword
            });
            if (!((ZilObject)form.Eval(ctx) is ZilString lexicalWord))
                throw new InterpreterError(
                    InterpreterMessages._0_1_Must_Return_2,
                    InterpreterMessages.NoFunction,
                    "WORD-LEXICAL-WORD",
                    "a string");

            var atom = ZilAtom.Parse(lexicalWord.Text, ctx);
            return new NewParserWord(ctx, atom, vword);
        }

        public ZilAtom Atom { get; }

        // TODO: change the type of NewParserWord.Inner? ZilHash isn't really a requirement
        [NotNull]
        public ZilHash Inner { get; }

        /* GetViaInner and SetViaInner disable DECL checking because user code may expect
         * property identifiers to be passed as FIXes instead of ATOMs. */
        ZilObject GetViaInner(StdAtom accessor)
        {
            var oldCheckDecls = ctx.CheckDecls;
            try
            {
                ctx.CheckDecls = false;
                var form = new ZilForm(new ZilObject[]
                {
                    ctx.GetStdAtom(accessor),
                    Inner
                });

                return (ZilObject)form.Eval(ctx);
            }
            finally
            {
                ctx.CheckDecls = oldCheckDecls;
            }
        }

        void SetViaInner(StdAtom accessor, ZilObject value)
        {
            var oldCheckDecls = ctx.CheckDecls;
            try
            {
                ctx.CheckDecls = false;
                var form = new ZilForm(new[]
                {
                    ctx.GetStdAtom(accessor),
                    Inner,
                    value
                });

                form.Eval(ctx);
            }
            finally
            {
                ctx.CheckDecls = oldCheckDecls;
            }
        }

        public ZilObject AdjId
        {
            get => GetViaInner(StdAtom.WORD_ADJ_ID);
            set => SetViaInner(StdAtom.WORD_ADJ_ID, value);
        }

        public int Classification
        {
            get => ((ZilFix)GetViaInner(StdAtom.WORD_CLASSIFICATION_NUMBER)).Value;
            set => SetViaInner(StdAtom.WORD_CLASSIFICATION_NUMBER, new ZilFix(value));
        }

        public ZilObject DirId
        {
            get => GetViaInner(StdAtom.WORD_DIR_ID);
            set => SetViaInner(StdAtom.WORD_DIR_ID, value);
        }

        public int Flags
        {
            get => ((ZilFix)GetViaInner(StdAtom.WORD_FLAGS)).Value;
            set => SetViaInner(StdAtom.WORD_FLAGS, new ZilFix(value));
        }

        public ZilObject SemanticStuff
        {
            get => GetViaInner(StdAtom.WORD_SEMANTIC_STUFF);
            set => SetViaInner(StdAtom.WORD_SEMANTIC_STUFF, value);
        }

        public ZilObject VerbStuff
        {
            get => GetViaInner(StdAtom.WORD_VERB_STUFF);
            set => SetViaInner(StdAtom.WORD_VERB_STUFF, value);
        }

        public bool HasClass(int queryClass)
        {
            return (Classification & queryClass) == queryClass;
        }
    }
}
