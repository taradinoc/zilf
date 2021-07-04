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
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using Zilf.Diagnostics;
using Zilf.Emit;
using Zilf.Interpreter.Values;
using Zilf.Language;
using Zilf.ZModel;
using Zilf.ZModel.Values;

namespace Zilf.Compiler
{
    partial class Compilation
    {
        IOperand? GetGlobalDefaultValue(ZilGlobal global)
        {
            if (global.Value == null)
                return null;

            IOperand? result = null;

            try
            {
                using (DiagnosticContext.Push(global.SourceLine))
                {
                    result = CompileConstant(global.Value, AmbiguousConstantMode.Optimistic);
                    if (result == null)
                    {
                        Context.HandleError(new CompilerError(
                            global,
                            CompilerMessages.Nonconstant_Initializer_For_0_1_2,
                            "global",
                            global.Name,
                            global.Value.ToStringContext(Context, false)));
                    }
                }
            }
            catch (ZilError ex)
            {
                Context.HandleError(ex);
            }

            return result;
        }

        /// <summary>
        /// Analyzes the set of defined global variables and, if there are too many to fit in Z-machine variables ("hard globals"),
        /// does the necessary planning and allocation to move some of them into a table ("soft globals").
        /// </summary>
        /// <remarks>
        /// <para>This method sets <see cref="ZilGlobal.StorageType"/> for all globals,
        /// whether or not it ends up moving any of them.</para>
        /// <para>If it does decide to move some globals, it allocates the <see cref="SoftGlobalsTable"/> (<c>T?GLOBAL-VARS-TABLE</c>),
        /// assigns offsets within the table, and fills the table with the initial values.</para>
        /// </remarks>
        /// <param name="reservedGlobals">The number of hard globals that are reserved for other purposes (i.e. parser tables).
        ///     Subtracting <paramref name="reservedGlobals"/> from 240 gives the number of hard globals that are actually available
        ///     for storing <see cref="ZEnvironment.Globals"/>.</param>
        /// <param name="globalInitializers"></param>
        /// <exception cref="CompilerError"></exception>
        void DoFunnyGlobals(int reservedGlobals, Queue<System.Action> globalInitializers)
        {
            // if all the globals fit into Z-machine globals, no need for a table
            int remaining = 240 - reservedGlobals;

            if (Context.ZEnvironment.Globals.Count <= remaining)
            {
                foreach (var g in Context.ZEnvironment.Globals)
                    g.StorageType = GlobalStorageType.Hard;

                return;
            }

            // reserve one slot for GLOBAL-VARS-TABLE
            remaining--;

            // in V3, the status line variables need to be Z-machine globals
            if (Context.ZEnvironment.ZVersion < 4)
            {
                foreach (var g in Context.ZEnvironment.Globals)
                {
                    switch (g.Name.StdAtom)
                    {
                        case StdAtom.HERE:
                        case StdAtom.SCORE:
                        case StdAtom.MOVES:
                            g.StorageType = GlobalStorageType.Hard;
                            break;
                    }
                }
            }

            // variables used as operands need to be Z-machine globals too
            var globalsByName = Context.ZEnvironment.Globals.ToDictionary(g => g.Name);
            foreach (var r in Context.ZEnvironment.Routines)
            {
                r.WalkRoutineForms(f =>
                {
                    var args = f.Rest;
                    if (args != null && !args.IsEmpty)
                    {
                        // skip the first argument to operations that operate on a variable
                        if (f.First is ZilAtom firstAtom)
                        {
                            switch (firstAtom.StdAtom)
                            {
                                case StdAtom.SET:
                                case StdAtom.SETG:
                                case StdAtom.VALUE:
                                case StdAtom.GVAL:
                                case StdAtom.LVAL:
                                case StdAtom.INC:
                                case StdAtom.DEC:
                                case StdAtom.IGRTR_P:
                                case StdAtom.DLESS_P:
                                    args = args.Rest;
                                    break;
                            }
                        }

                        while (args != null && !args.IsEmpty)
                        {
                            if (args.First is ZilAtom atom && globalsByName.TryGetValue(atom, out var g))
                            {
                                g.StorageType = GlobalStorageType.Hard;
                            }

                            args = args.Rest;
                        }
                    }
                });
            }

            // determine which others to keep in Z-machine globals
            var lookup = Context.ZEnvironment.Globals.ToLookup(g => g.StorageType);

            var hardGlobals = new List<ZilGlobal>(remaining);
            if (lookup.Contains(GlobalStorageType.Hard))
            {
                hardGlobals.AddRange(lookup[GlobalStorageType.Hard]);

                if (hardGlobals.Count > remaining)
                    throw new CompilerError(
                        CompilerMessages.Too_Many_0_1_Defined_Only_2_Allowed,
                        "hard globals",
                        hardGlobals.Count,
                        remaining);
            }

            var softGlobals = new Queue<ZilGlobal>(Context.ZEnvironment.Globals.Count - hardGlobals.Count);

            if (lookup.Contains(GlobalStorageType.Any))
                foreach (var g in lookup[GlobalStorageType.Any])
                    softGlobals.Enqueue(g);

            if (lookup.Contains(GlobalStorageType.Soft))
                foreach (var g in lookup[GlobalStorageType.Soft])
                    softGlobals.Enqueue(g);

            while (hardGlobals.Count < remaining && softGlobals.Count > 0)
                hardGlobals.Add(softGlobals.Dequeue());

            // assign final StorageTypes
            foreach (var g in hardGlobals)
                g.StorageType = GlobalStorageType.Hard;

            foreach (var g in softGlobals)
                g.StorageType = GlobalStorageType.Soft;

            // create SoftGlobals entries, fill table, and assign offsets
            int byteOffset = 0;
            var table = Game.DefineTable("T?GLOBAL-VARS-TABLE", false);

            var tableGlobal = Game.DefineGlobal("GLOBAL-VARS-TABLE");
            tableGlobal.DefaultValue = table;
            Globals.Add(Context.GetStdAtom(StdAtom.GLOBAL_VARS_TABLE), tableGlobal);
            SoftGlobalsTable = tableGlobal;

            foreach (var g in softGlobals)
            {
                if (!g.IsWord)
                {
                    var entry = new SoftGlobal
                    {
                        IsWord = false,
                        Offset = byteOffset
                    };
                    SoftGlobals.Add(g.Name, entry);

                    var gSave = g;
                    globalInitializers.Enqueue(() => table.AddByte(GetGlobalDefaultValue(gSave) ?? Game.Zero));

                    byteOffset++;
                }
            }

            if (byteOffset % 2 != 0)
            {
                byteOffset++;
                globalInitializers.Enqueue(() => table.AddByte(Game.Zero));
            }

            foreach (var g in softGlobals)
            {
                if (g.IsWord)
                {
                    var entry = new SoftGlobal
                    {
                        IsWord = true,
                        Offset = byteOffset / 2
                    };
                    SoftGlobals.Add(g.Name, entry);

                    var gSave = g;
                    globalInitializers.Enqueue(() => table.AddShort(GetGlobalDefaultValue(gSave) ?? Game.Zero));

                    byteOffset += 2;
                }
            }
        }

        /// <summary>
        /// Indicates whether to interpret ambiguous expressions as constants.
        /// </summary>
        /// <remarks>
        /// In particular, this indicates whether to interpret the name of a global variable
        /// as a constant equal to the variable's index.
        /// </remarks>
        public enum AmbiguousConstantMode
        {
            /// <summary>
            /// Try to interpret ambiguous expressions as constants.
            /// </summary>
            Optimistic,
            /// <summary>
            /// Don't try to interpret ambiguous expressions as constants.
            /// </summary>
            Pessimistic,
        }

        public IOperand? CompileConstant(ZilObject expr)
        {
            return CompileConstant(expr, AmbiguousConstantMode.Pessimistic);
        }

        // this method has a high complexity score because it has a big switch statement
        [SuppressMessage("ReSharper", "CyclomaticComplexity")]
        public IOperand? CompileConstant(ZilObject expr, AmbiguousConstantMode mode)
        {
            while (true)
            {
                switch (expr.Unwrap(Context))
                {
                    case ZilFix fix:
                        return Game.MakeOperand(fix.Value);

                    case ZilHash hash when hash.StdTypeAtom == StdAtom.BYTE && hash.GetPrimitive(Context) is ZilFix fix:
                        return Game.MakeOperand(fix.Value);

                    case ZilWord word:
                        return CompileConstant(word.Value);

                    case ZilString str:
                        return Game.MakeOperand(TranslateString(str, Context));

                    case ZilChar ch:
                        return Game.MakeOperand((byte)ch.Char);

                    case ZilAtom atom:
                        if (atom.StdAtom == StdAtom.T)
                            return Game.One;
                        if (Routines.TryGetValue(atom, out var routine))
                        {
                            MarkGlobalAsRead(atom);
                            return routine;
                        }
                        if (Objects.TryGetValue(atom, out var obj))
                        {
                            MarkGlobalAsRead(atom);
                            return obj;
                        }
                        if (Constants.TryGetValue(atom, out var operand))
                        {
                            MarkGlobalAsRead(atom);
                            return operand;
                        }

                        if (mode == AmbiguousConstantMode.Optimistic && Globals.TryGetValue(atom, out var global))
                        {
                            Context.HandleError(new CompilerError((ISourceLine?)null,
                                CompilerMessages.Bare_Atom_0_Interpreted_As_Global_Variable_Index,
                                atom));
                            MarkGlobalAsRead(atom);
                            return global;
                        }

                        return null;

                    case ZilFalse _:
                        return Game.Zero;

                    case ZilTable table:
                        if (Tables.TryGetValue(table, out var tb))
                            return tb;

                        tb = Game.DefineTable(table.Name, true);
                        Tables.Add(table, tb);
                        return tb;

                    case ZilConstant constant:
                        MarkGlobalAsRead(constant.Name);
                        return CompileConstant(constant.Value);

                    case ZilForm form:
                        if (form.IsGVAL(out var globalAtom))
                        {
                            expr = globalAtom;
                            mode = AmbiguousConstantMode.Pessimistic;
                            continue;
                        }

                        return null;

                    case ZilHash hash when hash.StdTypeAtom == StdAtom.VOC && hash.GetPrimitive(Context) is ZilAtom primAtom:
                        var wordAtom = ZilAtom.Parse("W?" + primAtom.Text, Context);
                        if (Constants.TryGetValue(wordAtom, out operand))
                        {
                            MarkGlobalAsRead(wordAtom);
                            return operand;
                        }

                        return null;

                    default:
                        var primitive = expr.GetPrimitive(Context);
                        if (primitive != expr && primitive.GetTypeAtom(Context) != expr.GetTypeAtom(Context))
                            return CompileConstant(primitive);

                        return null;
                }
            }
        }

        private void MarkGlobalAsRead(ZilAtom name)
        {
            ReadAccessedGlobalNames.Add(name);
        }

        private void WarnAboutUnusedGlobals()
        {
            var alreadyWarned = new HashSet<ZilAtom>();

            SuppressImplicitlyUsedNames();

            Check(
                CompilerMessages.Flag_0_Is_Defined_But_Never_Used,
                from flagName in Flags.Keys
                select (flagName, flagName, FlagDefinitions.GetValueOrDefault(flagName)));

            Check(
                CompilerMessages.Property_0_Is_Defined_But_Never_Used,
                from propName in Properties.Keys
                let propConst = ZilAtom.Parse("P?" + propName.Text, Context)
                select (propConst, propName, PropertyDefinitions.GetValueOrDefault(propName)));

            void SuppressImplicitlyUsedNames()
            {
                // TODO(ZILF-239): this works around the bug where PROPDEF DIRECTIONS incorrectly defines a DIRECTIONS property
                alreadyWarned.Add(Context.GetStdAtom(StdAtom.DIRECTIONS));

                // direction properties are implicitly used (written into the vocab table)
                foreach (var dir in Context.ZEnvironment.Directions)
                {
                    var dirPropConst = ZilAtom.Parse("P?" + dir.Text, Context);
                    alreadyWarned.Add(dirPropConst);
                }

                // FIND flags from syntax are implicitly used (written into the syntax table)
                foreach (var syn in Context.ZEnvironment.Syntaxes)
                {
                    if (syn.FindFlag1 != null)
                        alreadyWarned.Add(syn.FindFlag1);

                    if (syn.FindFlag2 != null)
                        alreadyWarned.Add(syn.FindFlag2);
                }

                /* If we warn about unused globals and routines in the future, this should be extended
                 * to suppress:
                 *   GO
                 *   ACTIONS
                 *   PREACTIONS
                 *   PREPOSITIONS
                 *   VERBS
                 *   
                 * ...and, for V3 only, the first 3 global variables defined.
                 */
            }

            void Check(int diagnosticCode, IEnumerable<(ZilAtom globalAtom, ZilAtom displayAtom, ISourceLine? src)> names)
            {
                foreach (var (globalAtom, displayAtom, src) in names.OrderBy(n => n.displayAtom.Text))
                {
                    if (!ReadAccessedGlobalNames.Contains(globalAtom) && alreadyWarned.Add(globalAtom))
                    {
                        Context.HandleError(new CompilerError(
                            src,
                            diagnosticCode,
                            displayAtom));
                    }
                }
            }
        }
    }
}
