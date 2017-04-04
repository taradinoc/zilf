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
using System.Text;
using System.Threading.Tasks;
using Zilf.Interpreter;
using Zilf.Interpreter.Values;
using Zilf.Language;
using Zilf.ZModel.Values;
using Zilf.Diagnostics;

namespace Zilf.ZModel.Vocab.NewParser
{
    class NewParserWord : IWord
    {
        readonly Context ctx;
        readonly ZilAtom atom;
        readonly ZilHash vword;

        public NewParserWord(Context ctx, ZilAtom atom, ZilHash vword)
        {
            this.ctx = ctx;
            this.atom = atom;
            this.vword = vword;
        }

        public static NewParserWord FromVword(Context ctx, ZilHash vword)
        {
            Contract.Requires(ctx != null);
            Contract.Requires(vword != null);
            Contract.Requires(vword.StdTypeAtom == StdAtom.VWORD);

            var form = new ZilForm(new ZilObject[]
            {
                ctx.GetStdAtom(StdAtom.WORD_LEXICAL_WORD),
                vword
            });
            if (!(form.Eval(ctx) is ZilString lexicalWord))
                throw new InterpreterError(
                    InterpreterMessages._0_1_Must_Return_2,
                    InterpreterMessages.NoFunction,
                    "WORD-LEXICAL-WORD",
                    "a string");

            var atom = ZilAtom.Parse(lexicalWord.Text, ctx);
            return new NewParserWord(ctx, atom, vword);
        }

        public ZilAtom Atom
        {
            get { return atom; }
        }

        public ZilHash Inner
        {
            get { return vword; }
        }

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
                    vword
                });

                return form.Eval(ctx);
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
                var form = new ZilForm(new ZilObject[]
                {
                    ctx.GetStdAtom(accessor),
                    vword,
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
            get { return GetViaInner(StdAtom.WORD_ADJ_ID); }
            set { SetViaInner(StdAtom.WORD_ADJ_ID, value); }
        }

        public int Classification
        {
            get { return ((ZilFix)GetViaInner(StdAtom.WORD_CLASSIFICATION_NUMBER)).Value; }
            set { SetViaInner(StdAtom.WORD_CLASSIFICATION_NUMBER, new ZilFix(value)); }
        }

        public ZilObject DirId
        {
            get { return GetViaInner(StdAtom.WORD_DIR_ID); }
            set { SetViaInner(StdAtom.WORD_DIR_ID, value); }
        }

        public int Flags
        {
            get { return ((ZilFix)GetViaInner(StdAtom.WORD_FLAGS)).Value; }
            set { SetViaInner(StdAtom.WORD_FLAGS, new ZilFix(value)); }
        }

        public ZilObject SemanticStuff
        {
            get { return GetViaInner(StdAtom.WORD_SEMANTIC_STUFF); }
            set { SetViaInner(StdAtom.WORD_SEMANTIC_STUFF, value); }
        }

        public ZilObject VerbStuff
        {
            get { return GetViaInner(StdAtom.WORD_VERB_STUFF); }
            set { SetViaInner(StdAtom.WORD_VERB_STUFF, value); }
        }

        public bool HasClass(int queryClass)
        {
            return (Classification & queryClass) == queryClass;
        }
    }
}
