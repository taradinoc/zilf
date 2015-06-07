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
using System.Collections.Generic;
using System.Diagnostics.Contracts;
using Zilf.Emit;
using Zilf.Interpreter;
using Zilf.Interpreter.Values;
using Zilf.ZModel;
using Zilf.ZModel.Values;
using Zilf.ZModel.Vocab;

namespace Zilf.Compiler
{
    internal class CompileCtx
    {
        /// <summary>
        /// The ZIL context that resulted from loading the source code.
        /// </summary>
        public Context Context { get; private set; }
        /// <summary>
        /// The game being built.
        /// </summary>
        public IGameBuilder Game { get; private set; }
        /// <summary>
        /// True if debug information should be generated (i.e. if the user
        /// wants it and the game builder supports it).
        /// </summary>
        public bool WantDebugInfo { get; private set; }

        public CompileCtx(Context ctx, IGameBuilder game, bool wantDebugInfo)
        {
            Contract.Requires(ctx != null);
            Contract.Requires(game != null);

            this.Context = ctx;
            this.Game = game;
            this.WantDebugInfo = wantDebugInfo;
        }

        [ContractInvariantMethod]
        private void ObjectInvariant()
        {
            Contract.Invariant(Context != null);
            Contract.Invariant(Game != null);
        }

        public ITableBuilder VerbTable, ActionTable, PreactionTable, PrepositionTable;

        public readonly Dictionary<ZilAtom, ILocalBuilder> Locals = new Dictionary<ZilAtom, ILocalBuilder>();
        public readonly HashSet<ZilAtom> TempLocalNames = new HashSet<ZilAtom>();
        public readonly Stack<ILocalBuilder> SpareLocals = new Stack<ILocalBuilder>();
        public readonly Dictionary<ZilAtom, Stack<ILocalBuilder>> OuterLocals = new Dictionary<ZilAtom, Stack<ILocalBuilder>>();
        public readonly Stack<Block> Blocks = new Stack<Block>();

        public readonly Dictionary<ZilAtom, IGlobalBuilder> Globals = new Dictionary<ZilAtom, IGlobalBuilder>();
        public readonly Dictionary<ZilAtom, IOperand> Constants = new Dictionary<ZilAtom, IOperand>();
        public readonly Dictionary<ZilAtom, IRoutineBuilder> Routines = new Dictionary<ZilAtom, IRoutineBuilder>();
        public readonly Dictionary<ZilAtom, IObjectBuilder> Objects = new Dictionary<ZilAtom, IObjectBuilder>();
        public readonly Dictionary<ZilTable, ITableBuilder> Tables = new Dictionary<ZilTable, ITableBuilder>();
        public readonly Dictionary<Word, IWordBuilder> Vocabulary = new Dictionary<Word, IWordBuilder>();
        public readonly Dictionary<ZilAtom, IPropertyBuilder> Properties = new Dictionary<ZilAtom, IPropertyBuilder>();
        public readonly Dictionary<ZilAtom, IFlagBuilder> Flags = new Dictionary<ZilAtom, IFlagBuilder>();

        public readonly Dictionary<ZilAtom, SoftGlobal> SoftGlobals = new Dictionary<ZilAtom, SoftGlobal>();
        public IOperand SoftGlobalsTable;
    }
}
