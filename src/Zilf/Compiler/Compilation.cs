/* Copyright 2010-2023 Tara McGrew
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
using Zilf.Emit;
using Zilf.Interpreter;
using Zilf.Interpreter.Values;
using Zilf.Language;
using Zilf.ZModel;
using Zilf.ZModel.Values;
using Zilf.ZModel.Vocab;

namespace Zilf.Compiler
{
    sealed partial class Compilation
    {
        /// <summary>
        /// The ZIL context that resulted from loading the source code.
        /// </summary>
        public Context Context { get; }
        /// <summary>
        /// The game being built.
        /// </summary>
        public IGameBuilder Game { get; }
        /// <summary>
        /// True if debug information should be generated (i.e. if the user
        /// wants it and the game builder supports it).
        /// </summary>
        public bool WantDebugInfo { get; }

        Compilation(Context ctx, IGameBuilder game, bool wantDebugInfo)
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
            PropertyDefinitions = new Dictionary<ZilAtom, ISourceLine?>(equalizer);
            Flags = new Dictionary<ZilAtom, IFlagBuilder>(equalizer);
            FlagDefinitions = new Dictionary<ZilAtom, ISourceLine?>(equalizer);
            SoftGlobals = new Dictionary<ZilAtom, SoftGlobal>(equalizer);
        }

        // TODO: helper class for managing local variables
        public readonly Dictionary<ZilAtom, LocalBindingRecord> Locals = new();
        public readonly List<LocalBindingRecord> AllLocalBindingRecords = new();
        public readonly HashSet<ZilAtom> TempLocalNames = new();
        public readonly Stack<ILocalBuilder> SpareLocals = new();
        public readonly Dictionary<ZilAtom, Stack<LocalBindingRecord>> OuterLocals = new();

        bool maxRoutineLocalsExceeded;

        public readonly Stack<Block> Blocks = new();

        public readonly Dictionary<ZilAtom, IGlobalBuilder> Globals;
        public readonly Dictionary<ZilAtom, IOperand> Constants;
        public readonly Dictionary<ZilAtom, IRoutineBuilder> Routines;
        public readonly Dictionary<ZilAtom, IObjectBuilder> Objects;
        public readonly Dictionary<ZilTable, ITableBuilder> Tables = new();
        public readonly Dictionary<IWord, IWordBuilder> Vocabulary = new();

        public readonly Dictionary<ZilAtom, IPropertyBuilder> Properties;
        public readonly Dictionary<ZilAtom, ISourceLine?> PropertyDefinitions;

        public readonly Dictionary<ZilAtom, IFlagBuilder> Flags;
        public readonly Dictionary<ZilAtom, ISourceLine?> FlagDefinitions;

        public readonly Dictionary<ZilAtom, SoftGlobal> SoftGlobals;
        public IOperand? SoftGlobalsTable;

        IOperand? unicodeTranslationTableOperand;

        public int UniqueFlags { get; set; }

        /// <summary>
        /// A set of atoms identifying the global values (constants, globals, routines, objects, flags, etc.) that are "read",
        /// i.e. used anywhere in the story file as an operand or initializer.
        /// </summary>
        public readonly HashSet<ZilAtom> ReadAccessedGlobalNames = new();

    // Set of routine names to compile after reachability analysis. If null, compile all.
    private HashSet<ZilAtom>? _routinesToCompile;
    private HashSet<ZilAtom>? _operandReferencedRoutineNames;
    private HashSet<ZilAtom>? _maybeUnusedRoutineNames;
    private HashSet<ZilAtom>? _suppressUnusedRoutineWarnings;
    private Dictionary<ZilAtom, ZilRoutine>? _routineDefinitionsByName;
    private Dictionary<ZilAtom, InlineRoutine>? _inlineRoutines;
    private Dictionary<ZilAtom, HashSet<ZilAtom>>? _wholeProgramInlineCallers;
    private HashSet<ZilAtom>? _wholeProgramInlineRemovalCredit;
    private readonly HashSet<ZilAtom> eliminatedInlineRoutines = new();
    private readonly Dictionary<ZilAtom, int> nonInlinedCallCounts = new();
    private readonly Dictionary<ZilAtom, Stack<IOperand>> inlineLocalBindings = new();
    private readonly HashSet<ZilAtom> activeInlineRoutines = new();
    private ZilAtom? compilingRoutineName;
    private bool compilingEntryPoint;
    private int inlineCallerGrowth;
    }
}
