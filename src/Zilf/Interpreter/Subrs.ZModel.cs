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

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using Zilf.Interpreter.Values;
using Zilf.Language;
using Zilf.ZModel;
using Zilf.ZModel.Values;
using Zilf.ZModel.Vocab;
using Zilf.ZModel.Vocab.NewParser;
using Zilf.Diagnostics;
using Zilf.Common;
using System.Globalization;
using System.Diagnostics.CodeAnalysis;

namespace Zilf.Interpreter
{
    static partial class Subrs
    {
        #region Z-Code: Routines, Objects, Constants, Globals

        /// <exception cref="InterpreterError">Unrecognized flag.</exception>
        [Subr("ROUTINE-FLAGS")]
        public static ZilObject ROUTINE_FLAGS(Context ctx, ZilAtom[] flags)
        {
            var newFlags = RoutineFlags.None;

            foreach (var atom in flags)
            {
                newFlags |= atom.StdAtom switch
                {
                    StdAtom.CLEAN_STACK_P => RoutineFlags.CleanStack,
                    StdAtom.KEEP_P => RoutineFlags.Keep,
                    StdAtom.UNUSED_P => RoutineFlags.SuppressUnusedWarning,
                    _ => throw new InterpreterError(InterpreterMessages._0_Unrecognized_Routine_Flag_1, "ROUTINE-FLAGS", atom),
                };
            }

            ctx.NextRoutineFlags = newFlags;
            return ctx.TRUE;
        }

        /// <exception cref="InterpreterError"><paramref name="name"/> is already defined, or <paramref name="argList"/> defines too many required parameters for the Z-machine version.</exception>
        [FSubr]
        public static ZilObject ROUTINE(Context ctx, ZilAtom name,
             [Optional] ZilAtom? activationAtom, ZilList argList,
             [Required] ZilObject[] body)
        {
            var oldAtom = ctx.ZEnvironment.InternGlobalName(name);
            ZilObject? prev;
            if ((prev = ctx.GetZVal(oldAtom)) != null)
            {
                if (ctx.AllowRedefine)
                {
                    ctx.Redefine(oldAtom);
                    ctx.ZEnvironment.InternGlobalName(name);
                }
                else
                {
                    var exn = new InterpreterError(
                        InterpreterMessages._0_Already_Defined_1,
                        "ROUTINE",
                        oldAtom.ToStringContext(ctx, false));

                    if (prev.SourceLine != null)
                    {
                        exn = exn.Combine(new InterpreterError(
                            prev.SourceLine,
                            InterpreterMessages.Previous_Definition_Was_Here));
                    }
                    
                    throw exn;
                }
            }

            var flags = CombineFlags(ctx.CurrentFile.Flags, ctx.NextRoutineFlags);
            ctx.NextRoutineFlags = RoutineFlags.None;

            var rtn = new ZilRoutine(
                name,
                activationAtom,
                argList,
                body,
                flags);

            var maxArgsAllowed = ctx.ZEnvironment.ZVersion > 3 ? 7 : 3;
            if (rtn.ArgSpec.MinArgCount > maxArgsAllowed)
            {
                throw new InterpreterError(
                    InterpreterMessages._0_Too_Many_Routine_Arguments_Only_1_Allowed_In_V2, "ROUTINE", maxArgsAllowed, ctx.ZEnvironment.ZVersion);
            }

            if (rtn.ArgSpec.EnvironmentAtom != null || rtn.ArgSpec.VarargsAtom != null)
            {
                throw new InterpreterError(ctx.TopFrame.SourceLine,
                    InterpreterMessages._0_Routines_May_Not_Define_BIND_TUPLE_Or_ARGS_Arguments,
                    "ROUTINE");
            }

            if (rtn.ArgSpec.MaxArgCount > maxArgsAllowed)
            {
                var affectedArgCount = rtn.ArgSpec.MaxArgCount - maxArgsAllowed;
                ctx.HandleError(new InterpreterError(ctx.TopFrame.SourceLine,
                    InterpreterMessages._0_Only_1_Routine_Argument1s_Allowed_In_V2_So_Last_3_OPT_Argument3s_Will_Never_Be_Passed,
                    "ROUTINE",
                    maxArgsAllowed,
                    ctx.ZEnvironment.ZVersion,
                    affectedArgCount));
            }

            rtn.SourceLine = ctx.TopFrame.SourceLine;
            ctx.SetZVal(name, rtn);
            Debug.Assert(rtn.Name != null);
            ctx.ZEnvironment.Routines.Add(rtn);
            return name;
        }

        static RoutineFlags CombineFlags(FileFlags fileFlags, RoutineFlags routineFlags)
        {
            var result = routineFlags;

            if ((fileFlags & FileFlags.CleanStack) != 0)
                result |= RoutineFlags.CleanStack;

            if ((fileFlags & FileFlags.KeepRoutines) != 0)
                result |= RoutineFlags.Keep;

            if ((fileFlags & FileFlags.SuppressUnusedRoutineWarnings) != 0)
                result |= RoutineFlags.SuppressUnusedWarning;

            return result;
        }

#pragma warning disable CS0649
        public static class AtomParams
        {
            [ZilSequenceParam]
            [ParamDesc("atom-or-adecl")]
            public struct AdeclOrAtom
            {
                [Either(typeof(ZilAtom), typeof(AdeclForAtom))]
                public object Content;

                public ZilAtom Atom
                {
                    get
                    {
                        if (Content is ZilAtom atom)
                            return atom;

                        return ((AdeclForAtom)Content).Atom;
                    }
                }

                public ZilObject? Decl
                {
                    get
                    {
                        if (Content is AdeclForAtom afa)
                            return afa.Decl;

                        return null;
                    }
                }
            }

            [ZilStructuredParam(StdAtom.ADECL)]
            public struct AdeclForAtom
            {
                public ZilAtom Atom;
                public ZilObject Decl;
            }

            [ZilSequenceParam]
            [ParamDesc("atom-or-string")]
            public struct StringOrAtom
            {
                [Either(typeof(ZilAtom), typeof(string))]
                public object Content;

                public ZilAtom GetAtom(Context ctx)
                {
                    if (Content is ZilAtom atom)
                        return atom;

                    return ZilAtom.Parse((string)Content, ctx);
                }

                public override string ToString() => Content.ToString()!;
            }
        }
#pragma warning restore CS0649

        /// <exception cref="InterpreterError"><paramref name="name"/> is already defined.</exception>
        [FSubr]
        [FSubr("MSETG")]
        public static ZilResult CONSTANT(Context ctx,
            AtomParams.AdeclOrAtom name, ZilObject value)
        {
            var atom = name.Atom;
            var zr = value.Eval(ctx);
            if (zr.ShouldPass())
                return zr;
            value = (ZilObject)zr;

            var oldAtom = ctx.ZEnvironment.InternGlobalName(atom);
            var previous = ctx.GetZVal(oldAtom);
            if (previous != null)
            {
                if (ctx.AllowRedefine)
                {
                    ctx.Redefine(oldAtom);
                    ctx.ZEnvironment.InternGlobalName(atom);
                }
                else if (previous is ZilConstant cnst && cnst.Value.StructurallyEquals(value))
                {
                    // silently ignore duplicate constants as long as the values are equal
                    return previous;
                }
                else
                {
                    var exn = new InterpreterError(InterpreterMessages._0_Already_Defined_1,
                        "CONSTANT",
                        oldAtom.ToStringContext(ctx, false));

                    if (previous.SourceLine != null)
                    {
                        exn = exn.Combine(new InterpreterError(
                            previous.SourceLine,
                            InterpreterMessages.Previous_Definition_Was_Here));
                    }

                    throw exn;
                }
            }

            var result = ctx.AddZConstant(atom, value);
            result.SourceLine = ctx.TopFrame.SourceLine;
            return result;
        }

        /// <exception cref="InterpreterError"><paramref name="name"/> is already defined.</exception>
        [FSubr]
        public static ZilResult GLOBAL(
            Context ctx,
            AtomParams.AdeclOrAtom name,
            ZilObject defaultValue,
#pragma warning disable RECS0154 // Parameter is never used
            ZilObject? decl = null,
            ZilAtom? size = null)
#pragma warning restore RECS0154 // Parameter is never used
        {
            // typical form:  <GLOBAL atom-or-adecl default-value>
            // quirky form:   <GLOBAL atom-or-adecl default-value decl [size]>
            // TODO: use decl and size?

            var atom = name.Atom;

            var zr = defaultValue.Eval(ctx);
            if (zr.ShouldPass())
                return zr;

            defaultValue = (ZilObject)zr;

            var oldAtom = ctx.ZEnvironment.InternGlobalName(atom);
            var oldVal = ctx.GetZVal(oldAtom);
            if (oldVal != null)
            {
                if (!ctx.AllowRedefine)
                {
                    var exn = new InterpreterError(InterpreterMessages._0_Already_Defined_1,
                        "GLOBAL",
                        oldAtom.ToStringContext(ctx, false));

                    if (oldVal.SourceLine != null)
                    {
                        exn = exn.Combine(new InterpreterError(
                            oldVal.SourceLine,
                            InterpreterMessages.Previous_Definition_Was_Here));
                    }

                    throw exn;
                }

                if (oldVal is ZilGlobal glob && glob.Value is ZilTable tbl)
                {
                    // prevent errors about duplicate symbol T?GLOBAL-NAME
                    // TODO: undefine the table if it hasn't been referenced anywhere yet
                    tbl.Name = null;
                }

                ctx.Redefine(oldAtom);
                ctx.ZEnvironment.InternGlobalName(atom);
            }

            if (defaultValue is ZilTable table)
                table.Name = "T?" + atom.Text;

            var g = new ZilGlobal(atom, defaultValue) { SourceLine = ctx.TopFrame.SourceLine };
            ctx.SetZVal(atom, g);
            ctx.ZEnvironment.Globals.Add(g);
            return g;
        }

#pragma warning disable CS0649
        public static class DefineGlobalsParams
        {
            [ZilStructuredParam(StdAtom.LIST)]
            public struct GlobalSpec
            {
                public AtomParams.AdeclOrAtom Name;

                [ZilOptional, Decl("<OR 'BYTE 'WORD>")]
                public ZilAtom? Size;

                [ZilOptional]
                public ZilObject? Initializer;
            }
        }
#pragma warning restore CS0649

        [FSubr("DEFINE-GLOBALS")]
        public static ZilResult DEFINE_GLOBALS(
            Context ctx,
#pragma warning disable RECS0154 // Parameter is never used
            ZilAtom groupName,
#pragma warning restore RECS0154 // Parameter is never used
            DefineGlobalsParams.GlobalSpec[] args)
        {
            foreach (var spec in args)
            {
                var name = spec.Name.Atom;

                // create global and macros
                var globalAtom = ZilAtom.Parse("G?" + name.Text, ctx);
                ZilObject? initializer;
                if (spec.Initializer != null)
                {
                    var zr = spec.Initializer.Eval(ctx);
                    if (zr.ShouldPass())
                        return zr;
                    initializer = (ZilObject)zr;
                }
                else
                {
                    initializer = null;
                }
                var g = new ZilGlobal(globalAtom, initializer ?? ctx.FALSE) { SourceLine = ctx.TopFrame.SourceLine };
                if (spec.Size?.StdAtom == StdAtom.BYTE)
                {
                    g.IsWord = false;
                }
                ctx.SetZVal(globalAtom, g);
                ctx.ZEnvironment.Globals.Add(g);

                // TODO: correct the source locations in the macro
                // {0} = name
                // {1} = globalAtom
                const string SMacroTemplate = @"
<DEFMAC {0} (""OPT"" 'NV)
    <COND (<ASSIGNED? NV> <FORM SETG {1} .NV>)
          (T <CHTYPE {1} GVAL>)>>
";

                Program.Evaluate(ctx, string.Format(CultureInfo.InvariantCulture, SMacroTemplate, name, globalAtom), true);
            }

            // enable FUNNY-GLOBALS?
            ctx.SetGlobalVal(ctx.GetStdAtom(StdAtom.DO_FUNNY_GLOBALS_P), ctx.TRUE);

            return ctx.TRUE;
        }

        [Subr]
        public static ZilObject OBJECT(Context ctx, ZilAtom name,
             [Decl("<LIST [REST LIST]>")] ZilList[] props)
        {
            CheckObjectDefinitionForShadyQuotedAtoms(ctx);
            return PerformObject(ctx, name, props, false);
        }

        [Subr]
        public static ZilObject ROOM(Context ctx, ZilAtom name,
            [Decl("<LIST [REST LIST]>")] ZilList[] props)
        {
            CheckObjectDefinitionForShadyQuotedAtoms(ctx);
            return PerformObject(ctx, name, props, true);
        }

        private static void CheckObjectDefinitionForShadyQuotedAtoms(Context ctx)
        {
            // OBJECT and ROOM are SUBRs, so quotes will have already been evaluated before the call,
            // but we can use ctx.TopFrame to access the original calling form
            if (ctx.TopFrame is CallFrame { SourceLine: var sourceLine, CallingForm: var objectForm })
            {
                foreach (var elem in objectForm)
                {
                    // check property clauses for SYNONYM and ADJECTIVE
                    if (elem is ZilList { First: ZilAtom { StdAtom: StdAtom.SYNONYM or StdAtom.ADJECTIVE }, Rest: { } propValues })
                    {
                        ZilObject? prevWord = null;

                        foreach (var word in propValues)
                        {
                            switch (word)
                            {
                                // issue warning for each quoted atom-or-fix in the property value that
                                // follows another (optionally quoted) atom-or-fix
                                case ZilForm {
                                    First: ZilAtom { StdAtom: StdAtom.QUOTE },
                                    Rest: { First: { } quotedWord, Rest.IsEmpty: true }
                                } when prevWord != null && quotedWord is ZilAtom or ZilFix:
                                    ctx.HandleError(new InterpreterError(
                                        sourceLine,
                                        InterpreterMessages._0_1_Is_Parsed_As_Two_Separate_Words_0_And_1_Did_You_Mean_0_1,
                                        prevWord,
                                        quotedWord));
                                    prevWord = quotedWord;
                                    break;

                                case ZilAtom or ZilFix:
                                    prevWord = word;
                                    break;

                                default:
                                    prevWord = null;
                                    break;
                            }
                        }
                    }
                }
            }
        }

        static ZilModelObject PerformObject(Context ctx, ZilAtom atom, ZilList[] props, bool isRoom)
        {
            string name = isRoom ? "ROOM" : "OBJECT";

            var oldAtom = ctx.ZEnvironment.InternGlobalName(atom);
            ZilObject? prev;
            if ((prev = ctx.GetZVal(oldAtom)) != null)
            {
                if (!ctx.AllowRedefine)
                {
                    var exn = new InterpreterError(InterpreterMessages._0_Already_Defined_1,
                        name,
                        oldAtom.ToStringContext(ctx, false));

                    if (prev.SourceLine != null)
                    {
                        exn = exn.Combine(new InterpreterError(
                            prev.SourceLine,
                            InterpreterMessages.Previous_Definition_Was_Here));
                    }

                    throw exn;
                }

                ctx.Redefine(atom);
                ctx.ZEnvironment.InternGlobalName(atom);
            }

            var zmo = new ZilModelObject(atom, props, isRoom) { SourceLine = ctx.TopFrame.SourceLine };
            ctx.SetZVal(atom, zmo);
            ctx.ZEnvironment.Objects.Add(zmo);
            return zmo;
        }

        [FSubr]
        public static ZilResult PROPDEF(Context ctx, ZilAtom atom, ZilObject defaultValue, ZilObject[] spec)
        {
            if (ctx.ZEnvironment.PropertyDefaults.ContainsKey(atom))
                ctx.HandleError(new InterpreterError(InterpreterMessages.Overriding_Default_Value_For_Property_0, atom));

            var zr = defaultValue.Eval(ctx);
            if (zr.ShouldPass())
                return zr;

            /* Adding the property to PropertyDefaults will cause it to be emitted, defining a
             * constant and using up a property number, whether or not the property is ever
             * referenced anywhere else.
             *
             * We must do this even when the default value is zero, because zillib calls PROPDEF
             * for properties that are used by certain optional features, in order to ensure
             * that a game that doesn't use those features will still compile:
             *
             *   <PROPDEF TEXT-HELD <>>
             *
             * However, games may use PROPDEF to define syntax for all direction properties by
             * defining a complex property pattern for DIRECTIONS:
             *
             *   <PROPDEF DIRECTIONS <> (DIR TO R:ROOM = ...)>
             *
             * We must not emit a constant for DIRECTIONS, which isn't a real property.
             * Therefore, we treat DIRECTIONS as a special case, and we only add atom to
             * PropertyDefaults when:
             * 
             *   (1) atom is not DIRECTIONS,
             *   (2) defaultValue is not false, or
             *   (3) spec is empty.
             */
            var zo = (ZilObject)zr;
            if (atom.StdAtom != StdAtom.DIRECTIONS || zo.IsTrue || spec.Length <= 0)
                ctx.ZEnvironment.PropertyDefaults[atom] = zo;

            // complex property patterns
            if (spec.Length <= 0)
                return atom;

            var pattern = ComplexPropDef.Parse(spec);
            ctx.SetPropDef(atom, pattern);

            return atom;
        }

        [Subr]
        public static ZilObject ZSTART(Context ctx, ZilAtom atom)
        {
            ctx.ZEnvironment.EntryRoutineName = atom;
            return atom;
        }

        /// <exception cref="InterpreterError">One of the <paramref name="synonyms"/> is already defined.</exception>
        [Subr("BIT-SYNONYM")]
        public static ZilObject BIT_SYNONYM(Context ctx, ZilAtom first,
            [Required] ZilAtom[] synonyms)
        {
            if (ctx.ZEnvironment.TryGetBitSynonym(first, out var original))
                first = original;

            foreach (var synonym in synonyms)
            {
                if (ctx.GetZVal(synonym) != null)
                    throw new InterpreterError(InterpreterMessages._0_Already_Defined_1, "BIT-SYNONYM", synonym);

                ctx.ZEnvironment.AddBitSynonym(synonym, first);
            }

            return first;
        }

        #endregion

        #region Z-Code: Tables

        /// <exception cref="InterpreterError">The syntax is invalid, or <paramref name="count"/> is less than 1.</exception>
        [Subr]
        public static ZilObject ITABLE(Context ctx,
            [Optional] ZilAtom? specifier,
            int count,
            [Optional, Decl("<LIST [REST ATOM]>")] ZilList? flagList,
            ZilObject[] initializer)
        {
            // Syntax:
            //    <ITABLE [specifier] count [(flags...)] [init...]>
            // 'count' is a number of repetitions.
            // 'specifier' controls the length marker. BYTE specifier
            // makes the length marker a byte (but the table is still a
            // word table unless changed with a flag).
            // 'init' is a sequence of values to be repeated 'count' times.
            // values are compiled as words unless BYTE/LEXV flag is specified.

            TableFormat flags = 0;

            // optional specifier
            if (specifier != null)
            {
                switch (specifier.StdAtom)
                {
                    case StdAtom.NONE:
                        // no change
                        break;
                    case StdAtom.BYTE:
                        flags = TableFormat.ByteLength;
                        break;
                    case StdAtom.WORD:
                        flags = TableFormat.WordLength;
                        break;
                    default:
                        throw new InterpreterError(InterpreterMessages._0_Specifier_Must_Be_NONE_BYTE_Or_WORD, "ITABLE");
                }
            }

            // element count
            if (count < 1)
                throw new InterpreterError(InterpreterMessages._0_Invalid_Table_Size, "ITABLE");

            // optional flags
            if (flagList != null)
            {
                bool gotLength = false;

                foreach (var zo in flagList)
                {
                    var flag = (ZilAtom)zo;
                    switch (flag.StdAtom)
                    {
                        case StdAtom.BYTE:
                            flags |= TableFormat.Byte;
                            break;
                        case StdAtom.WORD:
                            flags &= ~TableFormat.Byte;
                            break;
                        case StdAtom.LENGTH:
                            gotLength = true;
                            break;
                        case StdAtom.LEXV:
                            flags |= TableFormat.Lexv;
                            break;
                        case StdAtom.PURE:
                            flags |= TableFormat.Pure;
                            break;
                        case StdAtom.PARSER_TABLE:
                            flags |= TableFormat.Pure | TableFormat.ParserTable;
                            break;
                        case StdAtom.TEMP_TABLE:
                            flags |= TableFormat.TempTable;
                            break;
                        default:
                            throw new InterpreterError(InterpreterMessages._0_Unrecognized_Table_Flag_1, "ITABLE", flag);
                    }
                }

                if (gotLength)
                {
                    if ((flags & TableFormat.Byte) != 0)
                        flags |= TableFormat.ByteLength;
                    else
                        flags |= TableFormat.WordLength;
                }
            }

            var elementCount = count * Math.Max(initializer.Length, 1);

            if ((flags & TableFormat.Lexv) != 0 && (elementCount % 3) != 0)
            {
                ctx.HandleError(new InterpreterError(
                    InterpreterMessages._0_LEXV_Table_Initializer_Is_Not_A_Multiple_Of_3_Elements,
                    "ITABLE"));
            }

            CheckForTableLengthPrefixOverflow(ctx, "ITABLE", flags, elementCount);

            var tab = ZilTable.Create(count, initializer.Length == 0 ? null : initializer, flags, null, ctx.ZWordSize);
            tab.SourceLine = ctx.TopFrame.SourceLine;
            if ((flags & TableFormat.TempTable) == 0)
                ctx.ZEnvironment.Tables.Add(tab);
            return tab;
        }

        static void CheckForTableLengthPrefixOverflow(Context ctx, string name, TableFormat flags, int elementCount)
        {
            int maxPrefixValue;
            string prefixType;

            if ((flags & TableFormat.ByteLength) != 0)
            {
                (maxPrefixValue, prefixType) = (byte.MaxValue, "byte");
            }
            else if ((flags & TableFormat.WordLength) != 0)
            {
                (maxPrefixValue, prefixType) = (ushort.MaxValue, "word");
            }
            else
            {
                return;
            }

            if (elementCount > maxPrefixValue)
            {
                ctx.HandleError(new InterpreterError(
                    InterpreterMessages._0_Length_Prefix_Overflow_Table_Element_Count_1_Cannot_Be_Stored_In_A_2,
                    name,
                    elementCount,
                    prefixType));
            }
        }

        static ZilTable PerformTable(Context ctx, ZilListoidBase? flagList, ZilObject[] values,
            bool pure, bool wantLength)
        {
            // syntax:
            //    <[P][L]TABLE [(flags...)] values...>

            var name = (pure, wantLength) switch
            {
                (true, true) => "PLTABLE",
                (true, false) => "PTABLE",
                (false, true) => "LTABLE",
                _ => "TABLE",
            };

            const int T_WORDS = 0;
            const int T_BYTES = 1;
            const int T_STRING = 2;
            int type = T_WORDS;
            ZilObject[]? pattern = null;
            bool tempTable = false;
            bool parserTable = false;

            if (flagList != null)
            {
                while (!flagList.IsEmpty)
                {
                    Debug.Assert(flagList.Rest != null);

                    if (flagList.First is not ZilAtom flag)
                        throw new InterpreterError(InterpreterMessages._0_Flags_Must_Be_Atoms, name);

                    switch (flag.StdAtom)
                    {
                        case StdAtom.LENGTH:
                            wantLength = true;
                            break;
                        case StdAtom.PURE:
                            pure = true;
                            break;
                        case StdAtom.BYTE:
                            type = T_BYTES;
                            break;
                        case StdAtom.STRING:
                            type = T_STRING;
                            break;
                        case StdAtom.KERNEL:
                            // nada
                            break;
                        case StdAtom.PARSER_TABLE:
                            pure = true;
                            parserTable = true;
                            break;
                        case StdAtom.TEMP_TABLE:
                            tempTable = true;
                            break;

                        case StdAtom.PATTERN:
                            flagList = flagList.Rest;
                            if (flagList.IsEmpty || flagList.First is not ZilList patternList)
                                throw new InterpreterError(InterpreterMessages._0_Expected_1_After_2, name, "a list", "PATTERN");
                            Debug.Assert(flagList.Rest != null);
                            pattern = patternList.ToArray();
                            ValidateTablePattern(name, pattern);
                            break;

                        case StdAtom.SEGMENT:
                            // ignore
                            flagList = flagList.Rest;
                            if (flagList.IsEmpty)
                                throw new InterpreterError(InterpreterMessages._0_Expected_1_After_2, name, "a value", "SEGMENT");
                            Debug.Assert(flagList.Rest != null);
                            break;

                        default:
                            throw new InterpreterError(InterpreterMessages._0_Unrecognized_Table_Flag_1, name, flag);
                    }

                    flagList = flagList.Rest;
                }
            }

            TableFormat flags = 0;
            if (pure)
                flags |= TableFormat.Pure;
            if (type == T_BYTES || type == T_STRING)
                flags |= TableFormat.Byte;
            if (wantLength)
            {
                if (type == T_BYTES || type == T_STRING)
                    flags |= TableFormat.ByteLength;
                else
                    flags |= TableFormat.WordLength;
            }
            if (tempTable)
                flags |= TableFormat.TempTable;
            if (parserTable)
                flags |= TableFormat.ParserTable;

            var newValues = new List<ZilObject>(values.Length);
            foreach (var val in values)
            {
                if (type == T_STRING && val.StdTypeAtom == StdAtom.STRING)
                {
                    var str = val.ToStringContext(ctx, true);
                    foreach (char c in str)
                        newValues.Add(new ZilFix(c));
                }
                else
                    newValues.Add(val);
            }

            CheckForTableLengthPrefixOverflow(ctx, name, flags, newValues.Count);

            var tab = ZilTable.Create(1, newValues.ToArray(), flags, pattern?.ToArray(), ctx.ZWordSize);
            tab.SourceLine = ctx.TopFrame.SourceLine;
            if (!tempTable)
                ctx.ZEnvironment.Tables.Add(tab);
            return tab;
        }

        static void ValidateTablePattern(string name, ZilObject[] pattern)
        {
            if (pattern.Length == 0)
                throw new InterpreterError(InterpreterMessages._0_PATTERN_Must_Not_Be_Empty, name);

            for (int i = 0; i < pattern.Length; i++)
            {
                if (IsByteOrWordAtom(pattern[i]))
                {
                    // OK
                    continue;
                }

                if (pattern[i] is ZilVector vector)
                {
                    if (i != pattern.Length - 1)
                        throw new InterpreterError(InterpreterMessages._0_Vector_May_Only_Appear_At_The_End_Of_A_PATTERN, name);

                    if (vector.GetLength() < 2)
                        throw new InterpreterError(
                            InterpreterMessages._0_In_1_Must_Have_2_Element2s,
                            "vector",
                            "PATTERN",
                            new CountableString("at least 2", true));

                    // first element must be REST
                    if (vector[0] is not ZilAtom atom || atom.StdAtom != StdAtom.REST)
                        throw new InterpreterError(InterpreterMessages.Element_0_Of_1_In_2_Must_Be_3, 1, "vector", "PATTERN", "REST");

                    // remaining elements must be BYTE or WORD
                    if (!vector.Skip(1).All(IsByteOrWordAtom))
                        throw new InterpreterError(InterpreterMessages._0_Following_Elements_Of_Vector_In_PATTERN_Must_Be_BYTE_Or_WORD, name);

                    // OK
                    continue;
                }

                throw new InterpreterError(InterpreterMessages._0_PATTERN_May_Only_Contain_BYTE_WORD_Or_A_REST_Vector, name);
            }
        }

        static bool IsByteOrWordAtom(ZilObject value)
        {
            return value is ZilAtom atom && (atom.StdAtom == StdAtom.BYTE || atom.StdAtom == StdAtom.WORD);
        }

        [Subr]
        public static ZilObject TABLE(Context ctx, [Optional] ZilList? flagList,
              ZilObject[] values)
        {
            return PerformTable(ctx, flagList, values, false, false);
        }

        [Subr]
        public static ZilObject LTABLE(Context ctx, [Optional] ZilList? flagList,
              ZilObject[] values)
        {
            return PerformTable(ctx, flagList, values, false, true);
        }

        [Subr]
        public static ZilObject PTABLE(Context ctx, [Optional] ZilList? flagList,
             ZilObject[] values)
        {
            return PerformTable(ctx, flagList, values, true, false);
        }

        [Subr]
        public static ZilObject PLTABLE(Context ctx, [Optional] ZilList? flagList,
             ZilObject[] values)
        {
            return PerformTable(ctx, flagList, values, true, true);
        }

        /// <exception cref="InterpreterError"><paramref name="index"/> is out of range, or the element at <paramref name="index"/> is not a word.</exception>
        [Subr]
        public static ZilObject ZGET(Context ctx, [Decl("<PRIMTYPE TABLE>")] ZilObject tableish, int index)
        {
            if (index < 0)
                throw new InterpreterError(InterpreterMessages._0_Negative_Element_Count, "ZGET");

            var table = (ZilTable)tableish.GetPrimitive(ctx);

            if (index * ctx.ZWordSize > table.ByteCount - ctx.ZWordSize)
                throw new InterpreterError(InterpreterMessages._0_Reading_Past_End_Of_Structure, "ZGET");

            try
            {
                return table.GetWord(ctx, index) ?? ctx.FALSE;
            }
            catch (UnalignedTableReadException)
            {
                throw new InterpreterError(
                    InterpreterMessages._0_Unaligned_Table_Read_Element_At_1_Offset_2_Is_Not_A_1,
                    "ZGET",
                    "word",
                    index);
            }
        }

        /// <exception cref="InterpreterError"><paramref name="index"/> is out of range.</exception>
        [Subr]
        public static ZilObject ZPUT(Context ctx, [Decl("<PRIMTYPE TABLE>")] ZilObject tableish, int index,
             ZilObject newValue)
        {
            if (index < 0)
                throw new InterpreterError(InterpreterMessages._0_Negative_Element_Count, "ZPUT");

            var table = (ZilTable)tableish.GetPrimitive(ctx);

            if (index * ctx.ZWordSize > table.ByteCount - ctx.ZWordSize)
                throw new InterpreterError(InterpreterMessages._0_Writing_Past_End_Of_Structure, "ZPUT");

            table.PutWord(ctx, index, newValue);
            return newValue;
        }

        [Subr]
        public static ZilObject GETB(Context ctx, [Decl("<PRIMTYPE TABLE>")] ZilObject tableish, int index)
        {
            if (index < 0)
                throw new InterpreterError(InterpreterMessages._0_Negative_Element_Count, "GETB");

            var table = (ZilTable)tableish.GetPrimitive(ctx);

            if (index >= table.ByteCount)
                throw new InterpreterError(InterpreterMessages._0_Reading_Past_End_Of_Structure, "GETB");

            try
            {
                return table.GetByte(ctx, index) ?? ctx.FALSE;
            }
            catch (UnalignedTableReadException)
            {
                throw new InterpreterError(
                    InterpreterMessages._0_Unaligned_Table_Read_Element_At_1_Offset_2_Is_Not_A_1,
                    "GETB",
                    "byte",
                    index);
            }
        }

        [Subr]
        public static ZilObject PUTB(Context ctx, [Decl("<PRIMTYPE TABLE>")] ZilObject tableish, int index,
             ZilObject newValue)
        {
            if (index < 0)
                throw new InterpreterError(InterpreterMessages._0_Negative_Element_Count, "PUTB");

            var table = (ZilTable)tableish.GetPrimitive(ctx);

            if (index >= table.ByteCount)
                throw new InterpreterError(InterpreterMessages._0_Writing_Past_End_Of_Structure, "PUTB");

            table.PutByte(ctx, index, newValue);
            return newValue;
        }

        [Subr]
        public static ZilObject ZREST(Context ctx, [Decl("<PRIMTYPE TABLE>")] ZilObject tableish, int bytes)
        {
            if (bytes < 0)
                throw new InterpreterError(InterpreterMessages._0_Negative_Element_Count, "ZREST");

            var table = (ZilTable)tableish.GetPrimitive(ctx);

            if (bytes > table.ByteCount)
                throw new InterpreterError(InterpreterMessages._0_Reading_Past_End_Of_Structure, "ZREST");

            return table.OffsetByBytes(bytes);
        }

        #endregion

        #region Z-Code: Version, Options, Capabilities

        [Subr]
        public static ZilObject VERSION(Context ctx,
             ZilObject versionExpr,
             [Decl("'TIME")] ZilAtom? time = null)
        {
            // silently ignore the change if we're already in Glulx mode
            if (ctx.IsGlulx)
                return new ZilFix(ZEnvironment.GLULX_ZVERSION);

            var newVersion = ParseZVersion("VERSION", versionExpr);

            if (newVersion == ZEnvironment.GLULX_ZVERSION)
            {
                throw new InterpreterError(
                    InterpreterMessages._0_Glulx_Output_Must_Be_Selected_On_The_Command_Line_With_Glulx,
                    "VERSION");
            }

            ctx.SetZVersion(newVersion);

            if (time != null)
            {
                if (ctx.ZEnvironment.ZVersion != 3)
                    throw new InterpreterError(InterpreterMessages._0_TIME_Is_Only_Meaningful_In_Version_3, "VERSION");

                ctx.ZEnvironment.TimeStatusLine = true;
            }

            return new ZilFix(newVersion);
        }

        static int ParseZVersion(string name, ZilObject expr)
        {
            int newVersion;
            switch (expr)
            {
                case ZilAtom atom:
                    string text = atom.Text;
                    goto HandleNamedVersion;

                case ZilString str:
                    text = str.Text;

                HandleNamedVersion:
                    newVersion = text.ToUpperInvariant() switch
                    {
                        "ZIP" => 3,
                        "EZIP" => 4,
                        "XZIP" => 5,
                        "YZIP" => 6,
                        "GLULX" => ZEnvironment.GLULX_ZVERSION,
                        _ => throw new InterpreterError(InterpreterMessages._0_Unrecognized_Version_Specifier_1,
                            name,
                            text).Combine(new InterpreterError(InterpreterMessages
                            .Recognized_Versions_Are_ZIP_EZIP_XZIP_YZIP_GLULX_And_Numbers_38))
                    };
                    break;

                case ZilFix fix:
                    newVersion = fix.Value;
                    if (newVersion < 3 || newVersion > 8)
                        throw new InterpreterError(InterpreterMessages._0_Unrecognized_Version_Specifier_1, name, newVersion)
                            .Combine(new InterpreterError(InterpreterMessages.Recognized_Versions_Are_ZIP_EZIP_XZIP_YZIP_GLULX_And_Numbers_38));
                    break;

                default:
                    throw new InterpreterError(InterpreterMessages._0_Unrecognized_Version_Specifier_1, name, expr)
                        .Combine(new InterpreterError(InterpreterMessages.Recognized_Versions_Are_ZIP_EZIP_XZIP_YZIP_GLULX_And_Numbers_38));
            }
            return newVersion;
        }

        [Subr("CHECK-VERSION?")]
        public static ZilObject CHECK_VERSION_P(Context ctx, ZilObject versionExpr)
        {
            var version = ParseZVersion("CHECK-VERSION?", versionExpr);
            return ctx.ZEnvironment.ZVersion == version ? ctx.TRUE : ctx.FALSE;
        }

        [FSubr("VERSION?")]
        public static ZilResult VERSION_P(Context ctx,
             CondClause[] clauses)
        {
            var tAtom = ctx.GetStdAtom(StdAtom.T);
            var elseAtom = ctx.GetStdAtom(StdAtom.ELSE);

            foreach (var clause in clauses)
            {
                if (clause.Condition == tAtom || clause.Condition == elseAtom ||
                    ParseZVersion("VERSION?", clause.Condition) == ctx.ZEnvironment.ZVersion)
                {
                    ZilResult result = clause.Condition;

                    foreach (var expr in clause.Body)
                    {
                        result = expr.Eval(ctx);
                        if (result.ShouldPass())
                            break;
                    }

                    return result;
                }
            }

            return ctx.FALSE;
        }

        [Subr("ORDER-OBJECTS?")]
        public static ZilObject ORDER_OBJECTS_P(Context ctx, ZilAtom atom)
        {
            switch (atom.StdAtom)
            {
                case StdAtom.DEFINED:
                    ctx.ZEnvironment.ObjectOrdering = ObjectOrdering.Defined;
                    return atom;
                case StdAtom.ROOMS_FIRST:
                    ctx.ZEnvironment.ObjectOrdering = ObjectOrdering.RoomsFirst;
                    return atom;
                case StdAtom.ROOMS_AND_LGS_FIRST:
                    ctx.ZEnvironment.ObjectOrdering = ObjectOrdering.RoomsAndLocalGlobalsFirst;
                    return atom;
                case StdAtom.ROOMS_LAST:
                    ctx.ZEnvironment.ObjectOrdering = ObjectOrdering.RoomsLast;
                    return atom;
            }

            throw new InterpreterError(
                InterpreterMessages._0_Expected_1,
                "ORDER-OBJECTS?: arg 1",
                "DEFINED, ROOMS-FIRST, ROOMS-AND-LGS-FIRST, or ROOMS-LAST");
        }

        [Subr("ORDER-TREE?")]
        public static ZilObject ORDER_TREE_P(Context ctx, ZilAtom atom)
        {
            switch (atom.StdAtom)
            {
                case StdAtom.REVERSE_DEFINED:
                    ctx.ZEnvironment.TreeOrdering = TreeOrdering.ReverseDefined;
                    return atom;
            }

            throw new InterpreterError(
                InterpreterMessages._0_Expected_1,
                "ORDER-TREE?: arg 1",
                "REVERSE-DEFINED");
        }

        [Subr("ORDER-FLAGS?")]
        public static ZilObject ORDER_FLAGS_P(Context ctx,
            [Decl("'LAST")] ZilAtom order,
            [Required] ZilAtom[] objects)
        {
            foreach (var atom in objects)
            {
                ctx.ZEnvironment.FlagsOrderedLast.Add(atom);
            }

            return order;
        }

        /// <exception cref="InterpreterError">Unrecognized option.</exception>
        [Subr("ZIP-OPTIONS")]
        public static ZilObject ZIP_OPTIONS(Context ctx, ZilAtom[] args)
        {
            foreach (var atom in args)
            {
                StdAtom flag;

                switch (atom.StdAtom)
                {
                    case StdAtom.COLOR:
                        flag = StdAtom.USE_COLOR_P;
                        break;

                    case StdAtom.MOUSE:
                        flag = StdAtom.USE_MOUSE_P;
                        break;

                    case StdAtom.UNDO:
                        flag = StdAtom.USE_UNDO_P;
                        break;

                    case StdAtom.DISPLAY:
                        flag = StdAtom.DISPLAY_OPS_P;
                        break;

                    case StdAtom.SOUND:
                        flag = StdAtom.USE_SOUND_P;
                        break;

                    case StdAtom.MENU:
                        flag = StdAtom.USE_MENUS_P;
                        break;

                    case StdAtom.BIG:
                        // ignore
                        continue;

                    default:
                        throw new InterpreterError(InterpreterMessages._0_Unrecognized_ZIP_Option_1, "ZIP-OPTIONS", atom);
                }

                ctx.DefineCompilationFlag(atom, ctx.TRUE, true);
                ctx.SetGlobalVal(ctx.GetStdAtom(flag), ctx.TRUE);
            }

            return ctx.TRUE;
        }

        [Subr("LONG-WORDS?")]
        public static ZilObject LONG_WORDS_P(Context ctx, bool enabled = true)
        {
            ctx.DefineCompilationFlag(ctx.GetStdAtom(StdAtom.LONG_WORDS),
                enabled ? ctx.TRUE : ctx.FALSE, true);
            return ctx.TRUE;
        }

        [Subr("FUNNY-GLOBALS?")]
        public static ZilObject FUNNY_GLOBALS_P(Context ctx, bool enabled = true)
        {
            ctx.SetGlobalVal(ctx.GetStdAtom(StdAtom.DO_FUNNY_GLOBALS_P),
                enabled ? ctx.TRUE : ctx.FALSE);
            return ctx.TRUE;
        }

        [Subr]
        public static ZilObject CHRSET(Context ctx, int alphabetNum,
             [Required, Decl("<LIST [REST <OR STRING CHARACTER FIX BYTE>]>")] ZilObject[] args)
        {
            if (alphabetNum < 0 || alphabetNum > 2)
                throw new InterpreterError(InterpreterMessages._0_Alphabet_Number_Must_Be_Between_0_And_2, "CHRSET");

            var sb = new StringBuilder(26);

            foreach (var item in args)
            {
                var primitive = item.GetPrimitive(ctx);
                switch (item.StdTypeAtom)
                {
                    case StdAtom.STRING:
                        sb.Append(((ZilString)primitive).Text);
                        break;

                    case StdAtom.CHARACTER:
                    case StdAtom.FIX:
                    case StdAtom.BYTE:
                        sb.Append((char)((ZilFix)primitive).Value);
                        break;

                    default:
                        throw UnhandledCaseException.FromEnum(item.StdTypeAtom, "CHRSET component type");
                }
            }

            var alphabetStr = sb.ToString();
            int requiredLen = (alphabetNum == 2) ? 24 : 26;
            if (alphabetStr.Length != requiredLen)
                throw new InterpreterError(
                    InterpreterMessages._0_Alphabet_1_Needs_2_Character2s,
                    "CHRSET",
                    alphabetNum,
                    requiredLen);

            switch (alphabetNum)
            {
                case 0:
                    ctx.ZEnvironment.Charset0 = alphabetStr;
                    break;

                case 1:
                    ctx.ZEnvironment.Charset1 = alphabetStr;
                    break;

                case 2:
                    ctx.ZEnvironment.Charset2 = alphabetStr;
                    break;
            }

            return ZilString.FromString(alphabetStr);
        }

        [Subr]
        public static ZilObject LANGUAGE(Context ctx, ZilAtom name, char escapeChar = '%', bool changeChrset = true)
        {
            var language = ZModel.Language.Get(name.Text) ??
                throw new InterpreterError(InterpreterMessages._0_Unrecognized_Language_1, "LANGUAGE", name.Text);

            // update language, escape char, and possibly charset
            ctx.ZEnvironment.Language = language;
            ctx.ZEnvironment.LanguageEscapeChar = escapeChar;

            if (changeChrset)
            {
                ctx.ZEnvironment.Charset0 = language.Charset0;
                ctx.ZEnvironment.Charset1 = language.Charset1;
                ctx.ZEnvironment.Charset2 = language.Charset2;
            }

            return name;
        }

        #endregion

        #region Z-Code: Vocabulary and Syntax

        [Subr]
        public static ZilObject DIRECTIONS(Context ctx, [Required] ZilAtom[] args)
        {
            // if a PROPSPEC is set for DIRECTIONS, it'll be copied to the new direction properties
            var propspecAtom = ctx.GetStdAtom(StdAtom.PROPSPEC);
            var propspec = ctx.GetProp(ctx.GetStdAtom(StdAtom.DIRECTIONS), propspecAtom);

            ctx.ZEnvironment.Directions.Clear();
            foreach (var arg in args)
            {
                ctx.ZEnvironment.Directions.Add(arg);
                ctx.ZEnvironment.GetVocabDirection(arg, ctx.TopFrame.SourceLine);
                ctx.ZEnvironment.LowDirection = arg;

                ctx.PutProp(arg, propspecAtom, propspec);
            }

            return ctx.TRUE;
        }

        [Subr]
        public static ZilObject BUZZ(Context ctx, [Required] ZilAtom[] args)
        {
            foreach (var arg in args)
                ctx.ZEnvironment.Buzzwords.Add(new KeyValuePair<ZilAtom, ISourceLine>(arg, ctx.TopFrame.SourceLine));

            return ctx.TRUE;
        }

        [Subr]
        public static ZilObject VOC(Context ctx, string text, [Decl("<OR FALSE ATOM>")] ZilObject? type = null)
        {
            var atom = ZilAtom.Parse(text, ctx);
            ctx.ZEnvironment.GetVocab(atom);

            if (type is ZilAtom typeAtom)
            {
                switch (typeAtom.StdAtom)
                {
                    case StdAtom.ADJ:
                    case StdAtom.ADJECTIVE:
                        ctx.ZEnvironment.GetVocabAdjective(atom, ctx.TopFrame.SourceLine);
                        break;

                    case StdAtom.NOUN:
                    case StdAtom.OBJECT:
                        ctx.ZEnvironment.GetVocabNoun(atom, ctx.TopFrame.SourceLine);
                        break;

                    case StdAtom.BUZZ:
                        ctx.ZEnvironment.GetVocabBuzzword(atom, ctx.TopFrame.SourceLine);
                        break;

                    case StdAtom.PREP:
                        ctx.ZEnvironment.GetVocabPreposition(atom, ctx.TopFrame.SourceLine);
                        break;

                    case StdAtom.DIR:
                        ctx.ZEnvironment.GetVocabDirection(atom, ctx.TopFrame.SourceLine);
                        break;

                    case StdAtom.VERB:
                        ctx.ZEnvironment.GetVocabVerb(atom, ctx.TopFrame.SourceLine);
                        break;

                    default:
                        throw new InterpreterError(InterpreterMessages._0_Unrecognized_Part_Of_Speech_1, "VOC", type);
                }
            }

            return ctx.ChangeType(atom, ctx.GetStdAtom(StdAtom.VOC));
        }

        /// <exception cref="ArgumentCountError"><paramref name="args"/> is too short.</exception>
        [Subr]
        public static ZilObject SYNTAX(Context ctx, ZilObject[] args)
        {
            if (args.Length < 3)
                throw ArgumentCountError.WrongCount(new FunctionCallSite("SYNTAX"), 3, null);

            var syntax = Syntax.Parse(ctx.TopFrame.SourceLine, args, ctx);
            ctx.ZEnvironment.Syntaxes.Add(syntax);

            if (syntax.Synonyms.Count > 0)
            {
                PerformSynonym(ctx, syntax.Verb.Atom, syntax.Synonyms.ToArray(), typeof(VerbSynonym));
            }

            return syntax.Verb.Atom;
        }

        [Subr("REMOVE-SYNTAX")]
        public static ZilObject REMOVE_SYNTAX(Context ctx, ZilObject[] args)
        {
            // <REMOVE-SYNTAX verb-pattern [[prep-pattern] object-pattern] [[prep-pattern] object-pattern] [= action-pattern]>
            //  where verb-pattern, prep-pattern, object-pattern, and action-pattern can be <>, *, or an appropriate atom
            //        <> matches nothing, * matches any atom or nothing
            //        omitted parts default to *
            // Example:
            //    <REMOVE-SYNTAX TAKE>             matches TAKE, TAKE INVENTORY, TAKE OBJECT FROM OBJECT
            //    <REMOVE-SYNTAX PUT * * OBJECT>   matches PUT OBJECT IN OBJECT, PUT OBJECT ON OBJECT
            //    <REMOVE-SYNTAX PUT OBJECT IN>    matches PUT OBJECT IN OBJECT
            //    <REMOVE-SYNTAX * = V-TAKE-FROM>  matches any verb syntax with action V-TAKE-FROM
            //    <REMOVE-SYNTAX *>                matches all syntaxes (!)

            var matcher = new SyntaxMatcher(args);
            ctx.ZEnvironment.Syntaxes.RemoveAll(s => matcher.Matches(s));
            return ctx.TRUE;
        }

        static ZilAtom PerformSynonym(Context ctx, ZilAtom original, ZilAtom[] synonyms, [DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicConstructors)] Type synonymType)
        {
            if (!ctx.ZEnvironment.Vocabulary.TryGetValue(original, out var oldWord))
            {
                oldWord = ctx.ZEnvironment.VocabFormat.CreateWord(original);
                ctx.ZEnvironment.Vocabulary.Add(original, oldWord);
            }

            var ctorArgs = new object[2];
            ctorArgs[0] = oldWord;

            foreach (var synonym in synonyms)
            {
                if (!ctx.ZEnvironment.Vocabulary.TryGetValue(synonym, out var newWord))
                {
                    newWord = ctx.ZEnvironment.VocabFormat.CreateWord(synonym);
                    ctx.ZEnvironment.Vocabulary.Add(synonym, newWord);
                }

                ctorArgs[1] = newWord;
                ctx.ZEnvironment.Synonyms.Add((Synonym)Activator.CreateInstance(
                    synonymType, ctorArgs)!);
            }

            return original;
        }

        [Subr]
        public static ZilObject SYNONYM(Context ctx, ZilAtom original, ZilAtom[] synonyms)
        {
            return PerformSynonym(ctx, original, synonyms, typeof(Synonym));
        }

        [Subr("VERB-SYNONYM")]
        public static ZilObject VERB_SYNONYM(Context ctx, ZilAtom original, ZilAtom[] synonyms)
        {
            return PerformSynonym(ctx, original, synonyms, typeof(VerbSynonym));
        }

        [Subr("PREP-SYNONYM")]
        public static ZilObject PREP_SYNONYM(Context ctx, ZilAtom original, ZilAtom[] synonyms)
        {
            return PerformSynonym(ctx, original, synonyms, typeof(PrepSynonym));
        }

        [Subr("ADJ-SYNONYM")]
        public static ZilObject ADJ_SYNONYM(Context ctx, ZilAtom original, ZilAtom[] synonyms)
        {
            return PerformSynonym(ctx, original, synonyms, typeof(AdjSynonym));
        }

        [Subr("DIR-SYNONYM")]
        public static ZilObject DIR_SYNONYM(Context ctx, ZilAtom original, ZilAtom[] synonyms)
        {
            return PerformSynonym(ctx, original, synonyms, typeof(DirSynonym));
        }

        [Subr("REMOVE-SYNONYM")]
        public static ZilObject REMOVE_SYNONYM(Context ctx, ZilAtom[] synonyms)
        {
            var synonymSet = new HashSet<ZilAtom>(synonyms);
            ctx.ZEnvironment.Synonyms.RemoveAll(
                s => synonymSet.Contains(s.OriginalWord.Atom) || synonymSet.Contains(s.SynonymWord.Atom));
            return ctx.TRUE;
        }

        #endregion

        #region Z-Code: Tell

        [FSubr("TELL-TOKENS")]
        public static ZilObject TELL_TOKENS(Context ctx, ZilObject[] args)
        {
            ctx.ZEnvironment.TellPatterns.Clear();
            return ADD_TELL_TOKENS(ctx, args);
        }

        [FSubr("ADD-TELL-TOKENS")]
        public static ZilObject ADD_TELL_TOKENS(Context ctx, ZilObject[] args)
        {
            ctx.ZEnvironment.TellPatterns.AddRange(TellPattern.Parse(args));
            return ctx.TRUE;
        }

        #endregion

        #region Z-Code: Version 6 Parser

        /// <exception cref="InterpreterError">NEW-PARSER? is not enabled.</exception>
        [Subr("ADD-WORD")]
        [Subr("NEW-ADD-WORD")]
        public static ZilObject NEW_ADD_WORD(Context ctx,
            AtomParams.StringOrAtom name,
            ZilAtom? type = null,
            ZilObject? value = null,
            ZilFix? flags = null)
        {
            if (!ctx.GetGlobalOption(StdAtom.NEW_PARSER_P))
                throw new InterpreterError(InterpreterMessages._0_Requires_NEWPARSER_Option, "NEW-ADD-WORD");

            var nameAtom = name.GetAtom(ctx);
            flags ??= ZilFix.Zero;
            return ((NewParserVocabFormat)ctx.ZEnvironment.VocabFormat).NewAddWord(nameAtom, type, value, flags);
        }

        #endregion
    }
}
