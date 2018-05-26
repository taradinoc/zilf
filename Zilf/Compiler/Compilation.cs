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

using System.Collections.Generic;
using JetBrains.Annotations;
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
        [ProvidesContext]
        [NotNull]
        public Context Context { get; }
        /// <summary>
        /// The game being built.
        /// </summary>
        [NotNull]
        public IGameBuilder Game { get; }
        /// <summary>
        /// True if debug information should be generated (i.e. if the user
        /// wants it and the game builder supports it).
        /// </summary>
        public bool WantDebugInfo { get; }

        Compilation([NotNull] Context ctx, [NotNull] IGameBuilder game, bool wantDebugInfo)
        {

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

        [NotNull]
        public readonly Dictionary<ZilAtom, ILocalBuilder> Locals = new Dictionary<ZilAtom, ILocalBuilder>();
        [NotNull]
        public readonly HashSet<ZilAtom> TempLocalNames = new HashSet<ZilAtom>();
        [NotNull]
        public readonly Stack<ILocalBuilder> SpareLocals = new Stack<ILocalBuilder>();
        [NotNull]
        public readonly Dictionary<ZilAtom, Stack<ILocalBuilder>> OuterLocals = new Dictionary<ZilAtom, Stack<ILocalBuilder>>();
        [NotNull]
        public readonly Stack<Block> Blocks = new Stack<Block>();

        [NotNull]
        public readonly Dictionary<ZilAtom, IGlobalBuilder> Globals;
        [NotNull]
        public readonly Dictionary<ZilAtom, IOperand> Constants;
        [NotNull]
        public readonly Dictionary<ZilAtom, IRoutineBuilder> Routines;
        [NotNull]
        public readonly Dictionary<ZilAtom, IObjectBuilder> Objects;
        [NotNull]
        public readonly Dictionary<ZilTable, ITableBuilder> Tables = new Dictionary<ZilTable, ITableBuilder>();
        [NotNull]
        public readonly Dictionary<IWord, IWordBuilder> Vocabulary = new Dictionary<IWord, IWordBuilder>();
        [NotNull]
        public readonly Dictionary<ZilAtom, IPropertyBuilder> Properties;
        [NotNull]
        public readonly Dictionary<ZilAtom, IFlagBuilder> Flags;

        [NotNull]
        public readonly Dictionary<ZilAtom, SoftGlobal> SoftGlobals;
        [CanBeNull]
        public IOperand SoftGlobalsTable;

        public int UniqueFlags { get; set; }
    }
}
