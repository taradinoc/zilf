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
    partial class Compilation
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

        Compilation(Context ctx, IGameBuilder game, bool wantDebugInfo)
        {
            Contract.Requires(ctx != null);
            Contract.Requires(game != null);

            Context = ctx;
            Game = game;
            WantDebugInfo = wantDebugInfo;

            var equalizer = new AtomNameEqualityComparer(ctx.IgnoreCase);

            Globals = new Dictionary<ZilAtom, IGlobalBuilder>(equalizer);
            Constants = new Dictionary<ZilAtom, IOperand>(equalizer);
            Routines = new Dictionary<ZilAtom, IRoutineBuilder>(equalizer);
            Objects = new Dictionary<ZilAtom, IObjectBuilder>(equalizer);
            Properties = new Dictionary<ZilAtom, IPropertyBuilder>(equalizer);
            Flags = new Dictionary<ZilAtom, IFlagBuilder>(equalizer);
            SoftGlobals = new Dictionary<ZilAtom, SoftGlobal>(equalizer);
        }

        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Performance", "CA1822:MarkMembersAsStatic")]
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Performance", "CA1811:AvoidUncalledPrivateCode")]
        [ContractInvariantMethod]
        void ObjectInvariant()
        {
            Contract.Invariant(Context != null);
            Contract.Invariant(Game != null);
        }

        public readonly Dictionary<ZilAtom, ILocalBuilder> Locals = new Dictionary<ZilAtom, ILocalBuilder>();
        public readonly HashSet<ZilAtom> TempLocalNames = new HashSet<ZilAtom>();
        public readonly Stack<ILocalBuilder> SpareLocals = new Stack<ILocalBuilder>();
        public readonly Dictionary<ZilAtom, Stack<ILocalBuilder>> OuterLocals = new Dictionary<ZilAtom, Stack<ILocalBuilder>>();
        public readonly Stack<Block> Blocks = new Stack<Block>();

        public readonly Dictionary<ZilAtom, IGlobalBuilder> Globals;
        public readonly Dictionary<ZilAtom, IOperand> Constants;
        public readonly Dictionary<ZilAtom, IRoutineBuilder> Routines;
        public readonly Dictionary<ZilAtom, IObjectBuilder> Objects;
        public readonly Dictionary<ZilTable, ITableBuilder> Tables = new Dictionary<ZilTable, ITableBuilder>();
        public readonly Dictionary<IWord, IWordBuilder> Vocabulary = new Dictionary<IWord, IWordBuilder>();
        public readonly Dictionary<ZilAtom, IPropertyBuilder> Properties;
        public readonly Dictionary<ZilAtom, IFlagBuilder> Flags;

        public readonly Dictionary<ZilAtom, SoftGlobal> SoftGlobals;
        public IOperand SoftGlobalsTable;

        public int UniqueFlags { get; set; }
    }
}
